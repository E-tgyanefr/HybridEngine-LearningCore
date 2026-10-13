using System;
using Android.Util;
using Android.Webkit;
using Java.Interop;

namespace HybridEngineLight.Android
{
    /// <summary>
    /// 暴露给网页的桥：网页里 `桥.ping()` 就能调到 C#。
    ///
    /// ⚠⚠ 为什么换成这个方案 —— 真机实测走了三条路才通：
    ///
    ///   ① WebView 读 `file://…/index.html` + `ShouldInterceptRequest` 拦 `/api/`
    ///      ⇒ 界面能显示，但 `fetch` 永远失败：
    ///          TypeError: Failed to fetch
    ///        **WebView 默认禁止 fetch 访问 file://**（file 的 origin 是 null），
    ///        而且拦不到 file:// 的 fetch。这条路从根上不通。
    ///
    ///   ② 应用内起 `HttpListener` 当本地服务，页面走 `http://127.0.0.1:端口/`
    ///      ⇒ 先撞 `ERR_CLEARTEXT_NOT_PERMITTED`（安卓 9+ 禁明文 HTTP，加配置解决），
    ///        解决后**原生层崩溃**：
    ///          FORTIFY: pthread_mutex_lock called on a destroyed mutex
    ///        崩在请求真正被处理的那一刻（不是建的时候）。
    ///        ⇒ 这是 .NET 在安卓上 `HttpListener` 实现的问题，不是用法问题。
    ///          我试过把端口探测从 HttpListener 换成 TcpListener（避免反复建销），
    ///          崩的地址一模一样 ⇒ **不折腾它了**。
    ///
    ///   ③ `addJavascriptInterface`（**就是这个**）—— .NET Android 的原生桥。
    ///      不起 socket、不过 HTTP、不碰 file://。最省资源，也最少平台假设。
    ///
    /// ⚠ 代价：界面代码变成「调 `桥.xxx()`」而不是 `fetch(...)`，
    ///   于是**同一份界面不能在桌面浏览器里直接跑了**（那本来是方案②的好处）。
    ///   权衡下来：能在手机上跑起来 > 桌面浏览器里方便调试。
    ///   将来要接 `ctl.py` 协议，就在这个桥上再加一个 `ctl(命令)` 方法即可。
    ///
    /// ⚠ 方法必须是 `public` 且**返回 string/基本类型** ——
    ///   `[JavascriptInterface]` 只导出简单签名，返回自定义对象时 JS 拿不到东西。
    ///   ⇒ 所有返回值都序列化成 JSON 字符串。
    /// </summary>
    class JsBridge : Java.Lang.Object
    {
        const string 标签 = "HybridEngineLight";

        readonly string 素材根;

        /// <summary>
        /// ⚠⚠ **必须持有 Activity 本身**，不能靠
        /// `Application.Context as Activity` 去转 ——
        /// `Application.Context` 是**应用上下文**（`ContextWrapper`
        /// 包着 `Application`），它**不是** Activity，转出来是 `null`。
        ///
        /// 实测（通过 CDP 直接调桥）：桥返回
        ///     {"ok":false,"错":"拿不到 Activity"}
        /// 而界面本身完全正常 —— 所以这是**运行期**才暴露的问题，
        /// 编译期一点提示都没有。
        ///
        /// 为什么需要 Activity：`StartActivity`（分享面板）和
        /// `RunOnUiThread` 都**只能**从 Activity 上做。
        /// </summary>
        readonly global::Android.App.Activity 活动;

        /// <summary>
        /// 把学员写的代码**分享出去** —— 让他能从手机带到电脑。
        ///
        /// ⚠⚠ 为什么用「分享」而不是「复制到剪贴板」——
        ///   剪贴板那条路我在这个绑定上**卡了三次**（见下面的记录），
        ///   而分享不但绕开了它，对"手机 → 电脑"这个真实需求**还更合适**：
        ///     · 微信 / QQ / 邮件 / 笔记 / 蓝牙 —— 系统分享面板里现成的都能用
        ///     · 而剪贴板只在本机有效，跨设备还得再找地方粘一次
        ///
        /// 试过并失败的（都记下来，免得以后有人再走一遍）：
        ///   ① `Android.Content.ClipboardManager.FromContext(ctx)` 拿到的
        ///      其实是**旧的** `Android.Text.ClipboardManager`（两个类同名），
        ///      它没有 `SetPrimaryClip`。错误消息里的
        ///      `CS0266: Cannot implicitly convert type
        ///       'Android.Text.ClipboardManager' to …` **直接写出了真实类型**，
        ///      而我当时在盯着「成员不存在」看，没读那一行。
        ///   ② 换成 `GetSystemService(ClipboardService) as ClipboardManager`：
        ///      类型对了（错误消息不再说类型转换），但报
        ///      `ClipboardManager does not contain a definition for
        ///       'SetPrimaryClip'` —— 在这个 **ref 程序集**里它确实没有
        ///      （我把 `Mono.Android.xml` 里 `ClipboardManager` 的成员列出来过，
        ///       只有 Add/Remove/Clear/Has/PrimaryClip 那几项）。
        ///   ③ 试 `PrimaryClip` 属性赋值 —— 同样的转换问题。
        ///   ⇒ **用分享，别跟剪贴板较劲。**
        ///
        /// ⚠ 为什么要切到 UI 线程：见下面 `投递` 的注释。
        /// </summary>
        [JavascriptInterface]
        [Java.Interop.Export("分享")]
        public string 分享(string 文字, string 标题)
        {
            if (string.IsNullOrEmpty(文字)) return "{\"ok\":false,\"错\":\"没有内容\"}";
            try
            {
                //: ⚠ 这里**不再**从 `Application.Context` 转 Activity ——
                //:   那样拿到的是 null（见字段上的注释）。用构造时传进来的。
                投递(活动, () =>
                {
                    try
                    {
                        var 意 = new global::Android.Content.Intent(
                            global::Android.Content.Intent.ActionSend);
                        意.SetType("text/plain");
                        意.PutExtra(global::Android.Content.Intent.ExtraText, 文字);
                        意.PutExtra(global::Android.Content.Intent.ExtraSubject, 标题);
                        var 选 = global::Android.Content.Intent.CreateChooser(
                            意, "把这段代码发到…");
                        活动.StartActivity(选);
                        Log.Info(标签, "已打开分享面板（" + 文字.Length + " 字）");
                    }
                    catch (Exception e)
                    {
                        Log.Error(标签, "分享失败：" + e);
                    }
                });
                return "{\"ok\":true,\"字\":" + 文字.Length + "}";
            }
            catch (Exception e)
            {
                Log.Error(标签, "分享（外层）失败：" + e);
                return "{\"ok\":false,\"错\":\"" + e.GetType().Name + "\"}";
            }
        }

        /// <summary>
        /// 切到主线程再跑。
        ///
        /// ⚠⚠ 为什么所有碰安卓 UI/系统服务的东西都要走这里：
        ///   桥的方法是在 **WebView 的 JS 线程**上被调用的，那个线程
        ///   **没有 Looper**。直接在那里 `StartActivity` / 取剪贴板会抛
        ///       Java.Lang.RuntimeException: Can't create handler inside
        ///       thread that has not called Looper.prepare()
        ///
        /// ⚠ 但**不能**扔给线程池（`Task.Run`）—— 那会踩到另一个坑：
        ///   本项目的 `Py_Initialize()` 就是扔线程池之后原生崩溃的
        ///   （`FORTIFY: pthread_mutex_lock called on a destroyed mutex`）。
        ///   ⇒ `RunOnUiThread` 是**应用主线程**，是这里唯一对的选择。
        /// </summary>
        void 投递(global::Android.App.Activity 活动, Action 做)
        {
            活动.RunOnUiThread(new Java.Lang.Runnable(做));
        }

        //: ⚠⚠ Python 的初始化**必须在后台做**，不能占着 JS 线程。
        //
        //   真机实测：第一版让 `ping()` 同步去调 `Py_Initialize()`，
        //   结果**界面整个卡住** —— 课程卡没渲染、自检停在「正在检查…」。
        //   因为 `addJavascriptInterface` 的方法是在 **WebView 的 JS 线程**上跑的，
        //   同步阻塞它 = DOM 更新也停了。
        //
        //   ⇒ 拆成三步：`启动()`（立刻返回，后台去初始化）→ `就绪()`（轮询）
        //     → `ping()`（这时才是快的）。
        static volatile bool 起了;
        static volatile string? 起错;
        static readonly object 锁 = new();

        public JsBridge(global::Android.App.Activity 活动, string 素材根) : base()
        {
            this.活动 = 活动;
            this.素材根 = 素材根;
        }

        /// <summary>让 Python 开始初始化。**在调用线程上直接做**。
        ///
        /// ⚠⚠ 这里刻意**不用** `Task.Run` 挪到线程池 —— 真机实测：
        ///   用 `Task.Run` 把 `Py_Initialize()` 放到别的线程上，
        ///   应用会在约 70 ms 后**原生崩溃**：
        ///       FORTIFY: pthread_mutex_lock called on a destroyed mutex
        ///   CPython 的 C API 对线程有要求（初始化与调用应当同线程），
        ///   换个线程进去会踩到它自己的锁。
        ///
        /// ⚠ 代价：这一步会**阻塞 WebView 的 JS 线程**几百毫秒
        ///   （甚至几秒）—— 界面在这期间不动。
        ///   所以顺序是：**先让页面画出来**（页面是静态 HTML，
        ///   秒出），再来调这个方法；学员看到的是"正在检查…"而不是白屏。
        ///   等确认能起来之后，可以再研究怎么既不卡界面又不换线程
        ///   （例如在 Activity.onCreate 里就初始化，或者用
        ///    `WebView.post` 回到真正的主线程）。
        /// </summary>
        [JavascriptInterface]
        [Java.Interop.Export("启动")]
        public string 启动()
        {
            if (起了 || 起错 != null) return "{}";
            //: ⚠ 诊断期间：桥**不再触发** Python 初始化 ——
            //  初始化已经提前到 Activity 阶段（见 MainActivity 的 提前初始化PY）。
            //  这里再调一次只会重复走同一条崩路。
            if (!PythonHost.已就绪)
            {
                起错 = "Python 还没起来（初始化已提前到 Activity 阶段，看日志的 ★ 行）";
                return "{}";
            }
            try
            {
                PythonHost.启动(素材根);
                起了 = true;
                Log.Info(标签, "Python 就绪：" + PythonHost.版本());
            }
            catch (Exception e)
            {
                起错 = e.GetType().Name + ": " + e.Message;
                Log.Error(标签, "Python 起不来：" + e);
            }
            return "{}";
        }

        /// <summary>查一下好了没有。网页轮询这个。</summary>
        [JavascriptInterface]
        [Java.Interop.Export("就绪")]
        public string 就绪()
        {
            if (起错 != null)
                return "{\"ok\":false,\"错\":" + 引号(起错) + "}";
            return 起了
                ? "{\"ok\":true,\"版本\":" + 引号(PythonHost.版本() ?? "") + "}"
                : "{\"ok\":null}";
        }

        /// <summary>
        /// 自检：跑一段真 Python 代码确认能用。
        /// ⚠ 只在 `就绪()` 返回 ok 之后调 —— 这时初始化已完成，调用是快的。
        /// </summary>
        [JavascriptInterface]
        [Java.Interop.Export("ping")]
        public string Ping()
        {
            var 表 = global::Android.OS.SystemClock.ElapsedRealtime();
            try
            {
                if (!起了)
                {
                    //: 没就绪时**不要**在这里同步初始化（那正是卡界面的原因）
                    return "{\"ok\":false,\"错\":" + 引号("Python 还没就绪，请先轮询 就绪()") + "}";
                }
                var (ok, 详情) = PythonHost.自检();
                long 毫秒 = global::Android.OS.SystemClock.ElapsedRealtime() - 表;
                return ok
                    ? "{\"ok\":true,\"版本\":" + 引号(详情) + ",\"毫秒\":" + 毫秒 + "}"
                    : "{\"ok\":false,\"错\":" + 引号(详情) + ",\"毫秒\":" + 毫秒 + "}";
            }
            catch (Exception e)
            {
                Log.Error(标签, "自检失败：" + e);
                return "{\"ok\":false,\"错\":" + 引号(e.GetType().Name + ": " + e.Message) + "}";
            }
        }

        /// <summary>
        /// ⚠⚠ **协议入口 —— 判分走这里**（原来是"还没接上"的空壳）。
        ///
        /// 网页发一行 `ctl.py` 协议请求（JSON 文本），拿回一行 JSON 响应：
        ///
        ///     桥.ctl('{"id":1,"cmd":"code.check","args":{"id":"基础-050","answer":"..."}}')
        ///     → {"id":1,"ok":false,"err":{"code":-1,"msg":"…未通过…"}}
        ///
        /// 判定**不是在这里做的** —— 这里只把请求递给
        /// `hybridengine_light.ctl.处理一行`，用的是引擎自己那套
        /// `check.run_checks` / `examrun._grade_code`。宿主换成安卓，引擎一行没改。
        ///
        /// ⚠ 现在**界面还没接**（页面里没有调用它的按钮）—— 缺口记在
        ///   `docs/安卓版说明.md` 里：接上只需要在网页上加一个按钮 +
        ///   把答案文本塞进这条请求，服务端这一侧已经是真的了。
        /// </summary>
        [JavascriptInterface]
        [Java.Interop.Export("ctl")]
        public string Ctl(string 请求)
        {
            try
            {
                PythonHost.启动(素材根);
                return PythonHost.调协议(请求,
                    System.IO.Path.GetDirectoryName(素材根) ?? 素材根);
            }
            catch (Exception e)
            {
                return "{\"ok\":false,\"err\":{\"code\":-1,\"msg\":" +
                       引号(e.GetType().Name + ": " + e.Message) + "}}";
            }
        }

        /// <summary>
        /// ⚠⚠ **诊断台**（调试用，界面上没有入口）：`桥.Dia('bare','')`。
        ///
        /// 为什么要有它：前 5 轮排查每换一个假设**都要重新构建 + 装 APK + 重启**，
        /// 一轮几分钟，而且"上一次到底怎么试的"只能靠注释记。
        /// 有了这个入口，**每个假设是一个参数**，冷启动一个进程只跑一个：
        ///
        ///     python _build/_cdp.py "桥.Dia('bare','')"        # 只有 Py_Initialize
        ///     python _build/_cdp.py "桥.Dia('global','')"      # 先 RTLD_GLOBAL 再初始化
        ///     python _build/_cdp.py "桥.Dia('who','0x711b60eaf8')"
        ///     python _build/_cdp.py "桥.Dia('dump','')"        # 捞上一次的 stderr
        ///
        /// ⚠ 方法名和参数名**故意用 ASCII** —— 本项目通篇中文标识符，
        ///   而 Java 侧能调到的中文参数名会进 LLVM IR 把构建搞挂
        ///   （`error XA3006`，见 csproj 里 `AndroidEnableMarshalMethods` 那段）。
        ///   这个入口不值得再冒一次那个风险。
        /// </summary>
        [JavascriptInterface]
        [Java.Interop.Export("Dia")]
        public string Dia(string step, string arg)
        {
            try
            {
                string 家 = System.IO.Path.GetDirectoryName(素材根) ?? 素材根;
                return Diag.跑实验(step, arg, 家, 素材根);
            }
            catch (Exception e)
            {
                Log.Error(标签, "Dia 抛异常：" + e);
                return "Dia 抛异常：" + e.GetType().Name + ": " + e.Message;
            }
        }

        /// <summary>
        /// ⚠⚠ **判分入口（界面上的「判分」按钮走这里）。**
        ///
        /// 学员在手机上写的代码 → 落成答题文件 → 跑这一课的 `CHECK_*` → 逐条返回。
        ///
        /// 返回的 JSON 形如：
        ///     {"ok":true,"passed":3,"total":3,
        ///      "results":[{"name":"有函数 平方","ok":true,"detail":""}, …]}
        /// 错答案时 `ok` 是 false、`results` 里能看到**哪一条不过**。
        ///
        /// ⚠ 判定逻辑**一行都没写在 C# 里** —— 全是引擎的 `check.run_checks`。
        ///   宿主换成安卓以后引擎那边一个字没改，这就是"移植"而不是"重写"。
        ///
        /// ⚠ 方法名/参数名故意用 ASCII（见 `Dia` 的注释）。
        /// </summary>
        [JavascriptInterface]
        [Java.Interop.Export("Pan")]
        public string Pan(string id, string code)
        {
            try
            {
                PythonHost.启动(素材根);
                return PythonHost.判课(id ?? "", code ?? "", 素材根,
                    System.IO.Path.GetDirectoryName(素材根) ?? 素材根);
            }
            catch (Exception e)
            {
                Log.Error(标签, "判分失败：" + e);
                return "{\"ok\":false,\"err\":{\"msg\":" + 引号(e.GetType().Name + ": " + e.Message) + "}}";
            }
        }

        /// <summary>JSON 字符串转义 —— 手写，不引序列化库（少一个依赖）。</summary>
        internal static string 引号(string s)
        {
            if (s == null) return "\"\"";
            var b = new System.Text.StringBuilder(s.Length + 2);
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
