using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Android.Util;
using Android.Webkit;

namespace HybridEngineLight.Android
{
    /// <summary>
    /// WebView ↔ Python 的桥。
    ///
    /// ⚠ 为什么走 <c>ShouldInterceptRequest</c>（拦请求）而不是
    ///   `addJavascriptInterface`（把 C# 对象暴露给 JS）：
    ///   · `addJavascriptInterface` 在 API 17 之前有远程代码执行漏洞，
    ///     现在虽然安全了，但它要求 JS 侧写 `window.桥.调用(...)` ——
    ///     那样界面代码就**绑死在安卓上**，桌面浏览器里跑不了。
    ///   · 拦请求之后，界面那边就是普通的 `fetch("/api/...")` ——
    ///     同一份界面在桌面浏览器里也能开（方便调试），
    ///     将来要换成真的 HTTP 服务端（`http.server`）也不用改前端。
    ///   ⇒ 多写几十行，换来"界面与平台解耦"。
    ///
    /// ⚠ 拦截回调在**后台线程**，不是 UI 线程 —— 这正好，
    ///   因为 Python 可能是慢的（学员代码死循环），在 UI 线程上会卡死界面。
    /// </summary>
    class WebBridge : WebViewClient
    {
        const string 标签 = "HybridEngineLight";

        readonly string 素材根;

        public WebBridge(string 素材根)
        {
            this.素材根 = 素材根;
        }

        public override WebResourceResponse? ShouldInterceptRequest(
            WebView? view, IWebResourceRequest? request)
        {
            string? url = request?.Url?.ToString();
            if (url == null || !url.Contains("/api/", StringComparison.Ordinal))
                return null;                       // 不是我们的接口，交给 WebView 自己处理

            string 路径 = url.Substring(url.IndexOf("/api/", StringComparison.Ordinal));
            string 体;
            try
            {
                体 = 路由(路径);
            }
            catch (Exception e)
            {
                Log.Error(标签, "接口 " + 路径 + " 抛了：" + e);
                体 = "{\"ok\":false,\"错\":" + 引号(e.GetType().Name + ": " + e.Message) + "}";
            }
            var 流 = new MemoryStream(Encoding.UTF8.GetBytes(体));
            return new WebResourceResponse("application/json", "utf-8", 200, "OK",
                new Dictionary<string, string> { { "Cache-Control", "no-store" } }, 流);
        }

        string 路由(string 路径)
        {
            if (路径.StartsWith("/api/ping", StringComparison.Ordinal))
            {
                // ⚠ 这一步会**惰性初始化 Python** —— 冷启动时界面早就画好了，
                //   这里才付那几百毫秒。如果放在 onCreate 里，用户会先看到白屏。
                var 表 = global::Android.OS.SystemClock.ElapsedRealtime();
                PythonHost.启动(素材根);
                var (ok, 详情) = PythonHost.自检();
                long 毫秒 = global::Android.OS.SystemClock.ElapsedRealtime() - 表;
                return ok
                    ? "{\"ok\":true,\"版本\":" + 引号(详情) + ",\"毫秒\":" + 毫秒 + "}"
                    : "{\"ok\":false,\"错\":" + 引号(详情) + ",\"毫秒\":" + 毫秒 + "}";
            }

            if (路径.StartsWith("/api/ctl", StringComparison.Ordinal))
            {
                //: 协议入口 —— 前端把 `{"id":1,"cmd":"lesson.list"}` 放进 POST 体。
                //  ⚠ 现在只回一句"还没接上"，好让前端能明确区分
                //    「Python 没起来」和「协议层没接」这两种情况。
                PythonHost.启动(素材根);
                return "{\"ok\":false,\"err\":{\"code\":-1,\"msg\":" +
                       引号("协议层还没接上（下一步：把 ctl.py 的 JSON-lines 循环搬进来）") + "}}";
            }

            return "{\"ok\":false,\"错\":" + 引号("没有这个接口：" + 路径) + "}";
        }

        /// <summary>JSON 字符串转义 —— 手写，不引序列化库（少一个依赖）。</summary>
        static string 引号(string s)
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
    }
}
