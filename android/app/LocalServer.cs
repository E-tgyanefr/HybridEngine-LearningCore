using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Android.Util;

namespace HybridEngineLight.Android
{
    /// <summary>
    /// 应用内的本地 HTTP 服务：把界面发给 WebView，并接 `/api/` 请求。
    ///
    /// ⚠⚠ 为什么必须有它 —— 真机实测撞出来的硬约束：
    ///
    ///   我最初让 WebView 直接加载 `file:///…/web/index.html`，
    ///   然后指望用 `WebViewClient.ShouldInterceptRequest` 拦 `/api/` 请求。
    ///   界面**显示正常**，但自检永远报
    ///       TypeError: Failed to fetch
    ///
    ///   原因：**WebView 默认禁止 `fetch` 访问 `file://`**（CORS：file 源的
    ///   origin 是 `null`，跨源请求一律拒绝），而 `ShouldInterceptRequest`
    ///   也**拦不到 file:// 的 XHR/fetch**。这不是写法问题，是这条路走不通。
    ///
    ///   ⇒ 起一个真正的 HTTP 源：页面从 `http://127.0.0.1:<端口>/` 加载，
    ///     `fetch('./api/ping')` 就是同源请求，正常发出去，也能被我们接住。
    ///
    /// 附带的好处：**这一层就是将来接 `ctl.py` 协议的地方**。
    ///   `ctl.py` 本来就是"一行一条 JSON"的协议（见 hybridengine_light/ctl.py），
    ///   把它挂到 `/api/ctl` 上，桌面端和安卓端就能共用同一份界面代码。
    ///
    /// ⚠ 端口用 0（让系统分配空闲端口）—— 写死端口会和别的应用撞，
    ///   而撞了的症状是"有时候能开、有时候打不开"，极难查。
    /// ⚠ 只监听 `127.0.0.1`，**不监听 0.0.0.0** —— 否则同一 WiFi 下
    ///   别人能访问你的课程界面（虽然没什么秘密，但没必要开这个口子）。
    /// </summary>
    class LocalServer : IDisposable
    {
        const string 标签 = "HybridEngineLight";

        readonly string 素材根;
        readonly HttpListener 监听 = new();
        readonly Thread 线程;
        bool 停了;

        /// <summary>服务的根地址，形如 `http://127.0.0.1:41234/`。</summary>
        public string 地址 { get; private set; } = "";

        public LocalServer(string 素材根)
        {
            this.素材根 = 素材根;

            // ⚠ 端口 0 = 让内核挑一个空闲的。挑完从 `监听.Prefixes` 读回真实端口。
            //   （HttpListener 不允许端口 0 出现在 Prefixes 里 —— 要先试一个，
            //    撞了就换。见下面 挑选端口。）
            int 端口 = 挑选端口();
            监听.Prefixes.Add("http://127.0.0.1:" + 端口 + "/");
            监听.Start();
            地址 = "http://127.0.0.1:" + 端口 + "/";

            线程 = new Thread(循环) { IsBackground = true, Name = "he-local-http" };
            线程.Start();
            Log.Info(标签, "本地服务起来了：" + 地址);
        }

        /// <summary>找一个能用的端口（从 41000 往上试）。</summary>
        static int 挑选端口()
        {
            // ⚠⚠ 用 `TcpListener` 探端口，**不要**用 `HttpListener` 反复建销 ——
            //   真机实测：原来是「对每个端口都 new HttpListener → Start → Stop → Close」，
            //   连做 200 次，结果**原生层崩溃**：
            //       FORTIFY: pthread_mutex_lock called on a destroyed mutex
            //   那是 .NET 在安卓上的 HttpListener 实现反复 Dispose 踩到自己的互斥锁。
            //   ⇒ 探测用轻量的 TcpListener（只是 bind 一下看空不空），
            //     真正要用的那个 HttpListener **只建一次**。
            Exception? 第一个错 = null;
            for (int p = 41000; p < 41200; p++)
            {
                try
                {
                    var 探 = new System.Net.Sockets.TcpListener(IPAddress.Loopback, p);
                    探.Start();
                    探.Stop();              // 立刻放掉，好让真正的 HttpListener 绑上
                    return p;
                }
                catch (Exception e)
                {
                    第一个错 ??= e;
                }
            }
            throw new InvalidOperationException(
                "41000~41200 一个端口都用不了。第一个错误是：" +
                (第一个错 == null ? "（没有）" : 第一个错.GetType().Name + ": " + 第一个错.Message) +
                "\n⚠ 安卓上这通常是**缺 INTERNET 权限** —— 绑 socket 也需要它。");
        }

        void 循环()
        {
            while (!停了)
            {
                HttpListenerContext 上;
                try { 上 = 监听.GetContext(); }
                catch { break; }                    // Stop() 会让它抛，正常退出
                //: ⚠ 每个请求单独开线程 —— 调用 Python 可能是慢的（学员代码死循环），
                //  在同一个线程里串行处理会把界面卡住。
                ThreadPool.QueueUserWorkItem(_ => 处理(上));
            }
        }

        void 处理(HttpListenerContext 上)
        {
            try
            {
                string 路 = 上.Request.Url?.AbsolutePath ?? "/";
                if (路.StartsWith("/api/", StringComparison.Ordinal))
                {
                    回JSON(上, 路由API(路));
                    return;
                }
                回文件(上, 路);
            }
            catch (Exception e)
            {
                Log.Error(标签, "处理请求失败：" + e);
                try { 回JSON(上, "{\"ok\":false,\"错\":" + 引号(e.Message) + "}"); } catch { }
            }
        }

        // ── /api/ ────────────────────────────────────────────────────────
        string 路由API(string 路)
        {
            if (路.StartsWith("/api/ping", StringComparison.Ordinal))
            {
                // ⚠ 这一步会**惰性初始化 Python**。冷启动时界面早就画好了，
                //   这里才付那几百毫秒 —— 学员先看到课程列表，而不是白屏。
                var 表 = global::Android.OS.SystemClock.ElapsedRealtime();
                PythonHost.启动(素材根);
                var (ok, 详情) = PythonHost.自检();
                long 毫秒 = global::Android.OS.SystemClock.ElapsedRealtime() - 表;
                return ok
                    ? "{\"ok\":true,\"版本\":" + 引号(详情) + ",\"毫秒\":" + 毫秒 + "}"
                    : "{\"ok\":false,\"错\":" + 引号(详情) + ",\"毫秒\":" + 毫秒 + "}";
            }

            if (路.StartsWith("/api/ctl", StringComparison.Ordinal))
            {
                PythonHost.启动(素材根);
                return "{\"ok\":false,\"err\":{\"code\":-1,\"msg\":" +
                       引号("协议层还没接上（下一步：把 ctl.py 的 JSON-lines 循环挂到这里）") + "}}";
            }

            return "{\"ok\":false,\"错\":" + 引号("没有这个接口：" + 路) + "}";
        }

        // ── 静态文件 ─────────────────────────────────────────────────────
        void 回文件(HttpListenerContext 上, string 路)
        {
            if (路 == "/" || 路.Length == 0) 路 = "/index.html";
            //: ⚠ 路径穿越防护：`..` 一律拒。不设这个的话
            //  `GET /../../../../data/...` 能读到应用私有目录外的文件。
            if (路.Contains("..", StringComparison.Ordinal))
            {
                回JSON(上, "{\"ok\":false,\"错\":\"路径不合法\"}");
                return;
            }
            string 全 = Path.GetFullPath(Path.Combine(素材根, "web", 路.TrimStart('/')));
            string 白名单根 = Path.GetFullPath(Path.Combine(素材根, "web"));
            if (!全.StartsWith(白名单根, StringComparison.Ordinal))
            {
                回JSON(上, "{\"ok\":false,\"错\":\"越界\"}");
                return;
            }
            if (!File.Exists(全))
            {
                上.Response.StatusCode = 404;
                回JSON(上, "{\"ok\":false,\"错\":" + 引号("没有这个文件：" + 路) + "}");
                return;
            }
            byte[] 体 = File.ReadAllBytes(全);
            上.Response.StatusCode = 200;
            上.Response.ContentType = 类型(全);
            上.Response.ContentLength64 = 体.Length;
            //: ⚠ 开发期别缓存 —— 否则改了页面重装看不到变化，会误以为没改对。
            上.Response.Headers["Cache-Control"] = "no-store";
            上.Response.OutputStream.Write(体, 0, 体.Length);
            上.Response.Close();
        }

        static string 类型(string 文件) => Path.GetExtension(文件).ToLowerInvariant() switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream",
        };

        static void 回JSON(HttpListenerContext 上, string json)
        {
            byte[] 体 = Encoding.UTF8.GetBytes(json);
            上.Response.StatusCode = 上.Response.StatusCode == 200 ? 200 : 上.Response.StatusCode;
            上.Response.ContentType = "application/json; charset=utf-8";
            上.Response.ContentLength64 = 体.Length;
            上.Response.Headers["Cache-Control"] = "no-store";
            上.Response.OutputStream.Write(体, 0, 体.Length);
            上.Response.Close();
        }

        /// <summary>JSON 字符串转义 —— 手写，不引序列化库（少一个依赖）。</summary>
        internal static string 引号(string s)
        {
            var b = new StringBuilder(s.Length + 2);
            b.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': b.Append("\\\""); break;
                    case '\\': b.Append("\\\\"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\t': b.Append("\\t"); break;
                    default:
                        if (c < 0x20) b.Append("\\u").Append(((int)c).ToString("x4"));
                        else b.Append(c);
                        break;
                }
            }
            b.Append('"');
            return b.ToString();
        }

        public void Dispose()
        {
            停了 = true;
            try { 监听.Stop(); } catch { }
            try { 监听.Close(); } catch { }
        }
    }
}
