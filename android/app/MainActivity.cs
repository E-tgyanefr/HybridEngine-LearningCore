using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Android.Widget;

namespace HybridEngineLight.Android
{
    /// <summary>
    /// 主界面：一个 WebView + 底部状态栏。
    ///
    /// ⚠⚠ 为什么是 WebView 而不是原生控件：
    ///   桌面版界面是 Tkinter 写的，而 **Tkinter 在安卓上不存在**
    ///   （CPython 官方 PEP 738 明确列出：`tkinter` 需要自行构建 Tk）。
    ///   与其在安卓上重写一套原生界面（那要维护两份 UI），
    ///   不如把界面做成**网页** —— 引擎那侧一行不用改，
    ///   而且同一份界面在桌面浏览器里也能开（方便调试）。
    ///
    /// 性能取舍（这是本工程最花心思的地方）：
    ///   · WebView 的系统实现是各平台调优过的，比自带一套 GUI 框架省内存
    ///   · **Python 是惰性初始化的**（第一次真要跑代码时才 start）——
    ///     冷启动只画界面，不加载 6.4MB 的 libpython
    ///   · 素材解压只在首次启动做一次，之后走标记文件跳过
    /// </summary>
    [Activity(Label = "学编程",
              MainLauncher = true,
              Icon = "@mipmap/icon",
              ConfigurationChanges = ConfigChanges.Orientation
                                   | ConfigChanges.ScreenSize
                                   | ConfigChanges.KeyboardHidden,
              WindowSoftInputMode = SoftInput.AdjustResize)]
    public class MainActivity : Activity
    {
        const string 标签 = "HybridEngineLight";

        /// <summary>
        /// 调试期开关：打开 WebView 的远程调试（CDP）。
        ///
        /// ⚠⚠ **发布版必须是 false**（调试端口不该留在正式包里）。
        ///   2026-10-13 收口时已改回 false —— 真机验证全部做完之后才关的。
        ///   要再查安卓界面：把它改回 true、重新构建即可（下面这段注释
        ///   就是那次调试留下的方法记录）。
        ///
        /// ⚠ 关掉之后 `_build/_cdp.py` 就连不上了 —— 那是有意的：
        ///   它靠的是 `webview_devtools_remote_<pid>` 这个调试 socket。
        ///   发布包里**不留**它。要复现验证，走下面的 `--es hel` 实验入口
        ///   （那条不需要 WebView 调试，也不需要 CDP）。
        /// </summary>
        const bool 调试WebView = false;

        WebView? _网页;
        TextView? _状态;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // ── 界面（纯代码搭，省掉一个 .axml 资源和它的构建环节）──
            var 根 = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical,
            };
            _状态 = new TextView(this) { Text = "准备中…" };
            _状态.SetPadding(24, 16, 24, 16);
            _状态.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 12f);

            // ⚠⚠ **打开 WebView 的远程调试**（CDP）。
            //
            //   为什么必须显式调 API：我原来以为有个 MSBuild 属性
            //   （`<WebViewDebuggingEnabled>true</WebViewDebuggingEnabled>`），
            //   **加了没用** —— 全盘搜 `Microsoft.Android.Sdk.Windows` 的
            //   targets / props / cs，`WebViewDebugging` 这个词
            //   **一次都没出现** ⇒ 那个属性**根本不存在**，我加的是空操作。
            //   我还因此误判成"加了但 socket 没出现"，白花一轮。
            //
            //   ⇒ 只能显式调 `SetWebContentsDebuggingEnabled(true)`。
            //
            //   ⚠ 它的价值：真机上可以用
            //       adb forward tcp:9222 localabstract:webview_devtools_remote_<pid>
            //     + CDP 的 `Runtime.evaluate` **直接调 JS、读 DOM** ——
            //     而不用 `adb shell input tap X Y` 猜坐标
            //     （我为猜坐标烧了好几轮，还经常点不中）。
            //
            //   ⚠ 发布版应该关掉（调试端口不该留在正式包里）。
            if (调试WebView)
            {
                global::Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
                Log.Info(标签, "WebView 远程调试已打开（CDP）");
            }

            _网页 = new WebView(this);
            var 参数 = _网页.Settings!;
            参数.JavaScriptEnabled = true;          // 界面是网页，必须开
            参数.DomStorageEnabled = true;          // 记「上次看到第几课」
            参数.AllowFileAccess = true;
            参数.SetSupportZoom(false);             // 手机上乱缩放反而难用
            参数.BuiltInZoomControls = false;

            //: ⚠ 桥要在**拿到素材根之后**才能建（它要用那个路径去启动 Python）。
            //  所以真正 `SetWebViewClient` 的地方在 `初始化()` 里。
            根.AddView(_网页, new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent, 0, 1f));
            根.AddView(_状态, new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent,
                LinearLayout.LayoutParams.WrapContent));
            SetContentView(根);

            // ── 首次启动：把 assets 里的 Python 解开（只在第一次）──
            //
            // ⚠ 这一步放在后台线程，而且**先让界面显示出来** ——
            //   解压 ~26MB 要几秒，冷启动时卡在白屏上用户会以为坏了。
            _ = 初始化();
        }

        /// <summary>
        /// ⚠⚠ **诊断开关**：true = 在 Activity 阶段就把 Python 拉起来，
        /// 完全绕开 WebView 和 JS 桥。
        ///
        /// 为什么要它：真机上崩在
        ///     FORTIFY: pthread_mutex_lock called on a destroyed mutex
        /// 而**四次实验**（起 HttpListener / 不起 / Task.Run / 同线程）
        /// 崩在**同一个地址、同一时刻**，说明那三个候选都不是原因。
        /// 剩下的嫌疑是「JS 桥注入」或「WebView 加载 file:// 时的回调」。
        /// 把 Python 的初始化**提前到 WebView 加载之前**，就能把两者分开：
        ///   · 这里通了 ⇒ 问题在 WebView/桥那侧，libpython 是好的
        ///   · 这里也崩 ⇒ libpython 与这个 NDK/ABI 组合本身起不来
        /// </summary>
        // ⚠⚠ **这一段已经过时过一次，2026-10-13 重写了 —— 别再照旧结论走。**
        //
        //   旧结论（当时是对的）："手机版不需要 Python，所以这里是 false。"
        //   原因是那会儿 `Py_Initialize()` 一调就原生崩。
        //
        //   ⚠⚠ **那个结论已经不成立了**：崩溃的死因是
        //     `ModuleNotFoundError: No module named 'encodings'`
        //     （PYTHONPATH 里没带标准库 + `.NET` 的 SetEnvironmentVariable
        //      到不了原生 environ，见 `PythonHost.初始化` 的注释），
        //     修掉之后 Python 3.12 在真机上**起得来、跑得动、能判分**。
        //   ⇒ 所以：**手机版需要 Python**，判分就是它干的。
        //
        //   仍然保留 false 的理由变了 —— 不再是"起不来"，而是**惰性**：
        //     · 判分是第一个真要用 Python 的动作，那之前不必付这个代价
        //     · 一次 `Py_Initialize` 实测 20 ms，但引擎 import 更贵，
        //       冷启动路径上不值得为"可能不判分"先付掉
        //   ⇒ 打开它是"提前起来（Activity 阶段、主线程）"，
        //     留着是为了：万一以后要查"启动期就崩"这类问题，改回 true 就能复现。
        //
        //   ⚠ 真要复现崩溃路径，用下面的 `--es hel old` 实验入口更干净
        //     （它是独立进程、独立日志，不用改代码重构建）。
        const bool 提前初始化PY = false;

        async Task 初始化()
        {
            try
            {
                报状态("正在准备运行环境（只在第一次需要）…");
                string 目录 = await Task.Run(() => AssetInstaller.安装(this!));
                Log.Info(标签, "素材目录：" + 目录);

                // ⚠⚠ **把上一次运行留下的原生输出捞出来**（CPython 的报错原文）。
                //   安卓应用的 fd 1/2 指向 /dev/null，`Py_FatalError` 写的东西
                //   平时一行都看不到 —— 而它正是 5 轮排查一直缺的那句话。
                //   所以 `Diag.装重定向()` 把 fd 1/2 接到应用私有目录的文件上，
                //   崩了之后**下一次启动**在这里读回 logcat。
                //
                // ── ⚠ 实验入口（**走 adb Intent，不依赖 WebView/CDP**）──────────
                //
                //     adb shell am start -n com.hybridengine.light/crc…MainActivity \
                //         --es hel bare
                //
                // ⚠ 为什么要有这条路：CDP 那条路实测**不稳** —— WebView 的
                //   devtools socket 会突然"连得上但不回一个字节"，
                //   一个假设要重试好几次，白烧时间。Intent 是安卓自己的机制，
                //   每次都干净：`am force-stop` + `am start --es` 就一个进程一个实验。
                //
                // ⚠ 整段（含读输出）**只在带 `--es hel` 时才跑** ⇒
                //   正常启动零开销、也不会有调试残留文件被读。
                string 实验 = Intent?.GetStringExtra("hel") ?? "";
                if (实验.Length > 0)
                {
                    Diag.读输出(Path.GetDirectoryName(目录) ?? 目录);
                    string 步 = 实验, 参 = "";
                    int 竖 = 实验.IndexOf('|');
                    if (竖 >= 0) { 步 = 实验.Substring(0, 竖); 参 = 实验.Substring(竖 + 1); }
                    //: ⚠⚠ **要跑的 Python 源码走单独的 ASCII 参数名 `src`。**
                    //:
                    //:   实测：`adb shell am start ... --es 参 <base64>` 传不过来 ——
                    //:   汉字参数名在 adb 的 shell 转义里会丢（跑出来是默认的 print(1+2)，
                    //:   日志显示"跑（10 字符）"）。而 `--es hel run|...` 那条路
                    //:   靠的是 Intent 里的 `hel`，它本身是 ASCII，所以没事。
                    //:   ⇒ 大段 base64 走 `src`；小参数仍可以走 `hel` 里的 `|`。
                    string 源 = Intent?.GetStringExtra("src") ?? "";
                    报状态("实验：" + 步);
                    Log.Info(标签, "=== Intent 实验：" + 步 + " 参=" + 参
                                   + " src=" + (源.Length > 0 ? 源.Length + " 字符" : "无") + " ===");
                    string 果 = Diag.跑实验(步, 参, Path.GetDirectoryName(目录) ?? 目录, 目录, this, 源);
                    Log.Info(标签, "=== Intent 实验 " + 步 + " 结果：\n" + 果);
                    报状态("实验 " + 步 + "：" + (果.Length > 200 ? 果.Substring(0, 200) : 果));
                }

                if (提前初始化PY)
                {
                    //: ⚠ **在 UI 线程上**调（不是线程池）—— CPython 的 C API
                    //  对线程有要求，而这是应用主线程，最接近"正常进程"的形态。
                    //  ⚠ 会阻塞界面几百毫秒；这是诊断，先要能起来。
                    报状态("正在启动 Python…");
                    try
                    {
                        // ⚠ 诊断第一步：**只 dlopen**，不做任何 C API 调用。
                        //   先回答"光是加载 .so 会不会崩"这个更基本的问题。
                        string 试 = PythonHost.试加载();
                        Log.Info(标签, "★★ 试加载结果：" + 试);
                        报状态("试加载：" + 试);

                        // ⚠ 诊断第 1.5 步：先探扩展模块能不能找到 libpython。
                        //   Py_Initialize 会去加载 lib-dynload 下的扩展模块，
                        //   而那些 .so 依赖 libpython —— 安卓的动态加载器
                        //   默认不全局共享 dlopen 的符号，找不到就会炸在
                        //   Py_Initialize 里面。
                        string 试扩 = PythonHost.试扩展模块(目录);
                        Log.Info(标签, "★★ 扩展模块探测：" + 试扩);

                        // ⚠ 诊断第二步：**只调 Py_Initialize，不设环境变量**。
                        //   如果这一步能过，说明崩在"设环境变量"那几句；
                        //   如果这里就崩，说明 Py_Initialize 本身在这个设备上有问题。
                        string 试2 = PythonHost.试初始化();
                        Log.Info(标签, "★★ 试初始化结果：" + 试2);
                        报状态("试初始化：" + 试2);

                        PythonHost.启动(目录);
                        Log.Info(标签, "★ Activity 阶段 Python 起来了：" + PythonHost.版本());
                        var (ok, 详情) = PythonHost.自检();
                        Log.Info(标签, "★ Activity 阶段自检：" + (ok ? "过 " + 详情 : "没过 " + 详情));
                        报状态(ok ? "Python 就绪（" + 详情 + "）" : "Python 自检没过：" + 详情);
                    }
                    catch (Exception e)
                    {
                        Log.Error(标签, "★ Activity 阶段 Python 起不来：" + e);
                        报状态("Python 起不来：" + e.Message);
                    }
                }

                RunOnUiThread(() =>
                {
                    // ⚠⚠ 用 `addJavascriptInterface`，**不起 HTTP 服务**。
                    //
                    //   真机实测走了三条路才通（详见 JsBridge.cs 的注释）：
                    //     ① file:// + ShouldInterceptRequest  → fetch 被 WebView 禁掉
                    //     ② 本地 HttpListener                  → 原生层 mutex 崩溃
                    //     ③ addJavascriptInterface（这个）     → .NET Android 的原生桥
                    //   第③条不起 socket、不过 HTTP、不碰 file://，最少平台假设。
                    //
                    //   ⚠ 为了让它生效，页面必须从**磁盘**读（`file://`）——
                    //     `addJavascriptInterface` 对 file:// 页面是生效的
                    //     （被禁的是 fetch/XHR，不是 JS 接口本身）。
                    _网页?.AddJavascriptInterface(new JsBridge(this!, 目录), "桥");
                    // ⚠ 外链交给系统浏览器（WebView 里没法返回）
                    _网页?.SetWebViewClient(new 本页客户端());
                    // ⚠⚠ 把**网页标题打进日志**。这是真机调试的**眼睛**：
                    //   页面在自检模式（URL 带 `?自检=1`）下会把状态
                    //   （有几个课程行 / 进度条在不在 / localStorage 记了几课 /
                    //     有没有 JS 报错）写进 `document.title`，
                    //   宿主这一行就把它落到 logcat ⇒ `adb logcat` 直接读得到。
                    //   为什么值得加：加了它之前我在真机上**看不见 DOM**，
                    //   "进度条没显示"这种问题只能靠截图猜，绕了好几轮。
                    _网页?.SetWebChromeClient(new 标题日志());
                    string 首页 = "file://" + Path.Combine(目录, "web", "index.html")
                                       .Replace('\\', '/');
                    //: ⚠ 调试期在末尾加 `?自检=1`（页面每秒把 DOM 状态写进
                    //:   `document.title`，宿主落进 logcat）。**发布版去掉** —— 已去掉。
                    //:   要查界面就加回来（它只写标题，不改行为）。
                    Log.Info(标签, "加载界面：" + 首页);
                    _网页?.LoadUrl(首页);
                    报状态("就绪");
                });
            }
            catch (Exception e)
            {
                Log.Error(标签, "初始化失败：" + e);
                报状态("初始化失败：" + e.Message);
                RunOnUiThread(() => _网页?.LoadData(
                    "<h3>初始化失败</h3><pre>" +
                    global::Android.Text.TextUtils.HtmlEncode(e.ToString()) + "</pre>",
                    "text/html; charset=utf-8", "utf-8"));
            }
        }

        void 报状态(string s)
        {
            RunOnUiThread(() => { if (_状态 != null) _状态.Text = s; });
            Log.Info(标签, s);
        }

        /// <summary>
        /// ⚠⚠ 安卓返回键：**先让 WebView 回退历史**，没得退才退出应用。
        ///
        /// 试过并**失败**的三种（都真机实测过）：
        ///   ① 网页自己 `history.pushState` + `popstate` 回列表
        ///      ⇒ 按返回**直接退出应用**
        ///   ② `OnBackPressed` 里 `evaluateJavascript` 问网页，
        ///      用 `document.title` 当同步信箱
        ///      ⇒ **还是退出**（异步回调赶不上"这一帧退不退"的决定）
        ///   ③ 只在启动时 `history.replaceState` 一次
        ///      ⇒ 同样退出
        ///
        /// ⇒ 正路其实是 WebView 自带的：**`CanGoBack()` / `GoBack()`**。
        ///   网页每次进课程都 `pushState`，WebView 自己就知道有得退 ——
        ///   不用我在两门语言之间搭任何通道。
        ///   ⚠ 我绕了三轮才想到它，因为一开始就假设"历史得我自己管"。
        ///
        /// ⚠ 代价：在列表页（没有可退的历史）按返回会退出应用 —— 那是对的。
        /// </summary>
        /// <summary>
        /// ⚠⚠ 拦返回键 —— **用 `OnKeyDown`，不用 `OnBackPressed`**。
        ///
        /// 实测（真机，带日志）：`OnBackPressed` 重写了但**根本没被调用**
        /// （日志里一行都没有），按返回直接退出应用。
        /// 试过并失败的四种：
        ///   ① 网页自己 `history.pushState` + `popstate` 回列表      → 退出
        ///   ② `OnBackPressed` 里 `evaluateJavascript` 问网页        → 退出
        ///   ③ `OnBackPressed` 里 `_网页.CanGoBack()/GoBack()`       → 退出（没被调用）
        ///   ④ 只在启动时 `replaceState` 一次                        → 退出
        ///
        /// ⇒ `OnKeyDown` 是**更底层**的入口：按键事件一定先到这里，
        ///   不会被 `OnBackPressedDispatcher` 那套新机制绕过。
        ///   它也是安卓上拦返回键最老、最不会失效的写法。
        /// </summary>
        public override bool OnKeyDown(global::Android.Views.Keycode 键,
                                       global::Android.Views.KeyEvent? e)
        {
            if (键 == global::Android.Views.Keycode.Back)
            {
                bool 能退 = _网页 != null && _网页.CanGoBack();
                Log.Info(标签, "返回键：CanGoBack=" + 能退);
                if (能退)
                {
                    _网页!.GoBack();       // 回退一层历史（网页里 popstate 会回列表）
                    return true;           // 吃掉了，不退出
                }
                return base.OnKeyDown(键, e);   // 没得退 ⇒ 正常退出
            }
            return base.OnKeyDown(键, e);
        }

        public override void OnBackPressed()
        {
            bool 能退 = _网页 != null && _网页.CanGoBack();
            Log.Info(标签, "OnBackPressed：进来了，CanGoBack=" + 能退);
            if (能退) { _网页!.GoBack(); return; }
            base.OnBackPressed();
        }

        /// <summary>
        /// 把网页标题落进日志 —— 真机调试的"眼睛"。
        ///
        /// 页面在自检模式（URL 带 `?自检=1`）下会把 DOM 的真实状态写进
        /// `document.title`（几个课程行 / 进度条在不在 / localStorage 记了几课 /
        /// 有没有 JS 报错），这一行就把它写进 logcat。
        ///
        /// ⚠ 为什么值得加：加它之前我在真机上**看不见 DOM**，
        ///   "进度条没显示"这类问题只能靠截图猜 —— 我为此绕了好几轮，
        ///   而真正的原因（DOM 里到底是什么样）一直没拿到。
        /// </summary>
        class 标题日志 : global::Android.Webkit.WebChromeClient
        {
            public override void OnReceivedTitle(
                global::Android.Webkit.WebView? view, string? title)
            {
                base.OnReceivedTitle(view, title);
                if (!string.IsNullOrEmpty(title))
                    Log.Info(标签, "网页标题：" + title);
            }
        }

        /// <summary>把外链交给系统浏览器，别在应用内开（WebView 里没法返回）。</summary>
        class 本页客户端 : global::Android.Webkit.WebViewClient
        {
            public override bool ShouldOverrideUrlLoading(
                WebView? view, global::Android.Webkit.IWebResourceRequest? request)
            {
                string? u = request?.Url?.ToString();
                if (u != null && u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var i = new global::Android.Content.Intent(
                            global::Android.Content.Intent.ActionView,
                            global::Android.Net.Uri.Parse(u));
                        view?.Context?.StartActivity(i);
                    }
                    catch { /* 没有浏览器就算了 */ }
                    return true;
                }
                return false;
            }
        }
    }
}
