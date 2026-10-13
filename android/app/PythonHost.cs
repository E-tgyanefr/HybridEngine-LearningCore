using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Android.Util;

namespace HybridEngineLight.Android
{
    /// <summary>
    /// 把 CPython 拉起来，并让 C# 能调它。
    ///
    /// ⚠⚠ 这一层是自己写的，**不是**用 Chaquopy 的 Java 运行时。
    ///   取舍：官方用法是 Chaquopy 的 Gradle 插件（它带一套 Java 桥），
    ///   但这台构建机**没有 Gradle**，而 .NET Android workload 自带完整构建链。
    ///   Chaquopy 的产物本身是裸 zip（`libpython3.12.so` + lib-dynload + stdlib），
    ///   直接取来用即可 —— 代价就是这里要自己按 CPython 的 C API 初始化。
    ///
    /// ⚠⚠ **性能上的关键决定：惰性初始化。**
    ///   `libpython3.12.so` 有 1.7 MB（压缩后）、标准库 490 多个 .pyc，
    ///   真加载起来要几百毫秒到一两秒。冷启动时**不该**付这个代价 ——
    ///   学员点开应用想先看到课程列表，而不是等一个白屏。
    ///   ⇒ `就绪` 标志 + 锁：第一次真要用 Python 才初始化，之后复用。
    ///
    /// ⚠ 初始化之后**不能**再改 `PYTHONHOME` / `sys.path` 的核心部分 ——
    ///   CPython 会把它们缓存进解释器状态。
    /// </summary>
    static class PythonHost
    {
        const string 标签 = "HybridEngineLight";

        // ── CPython 的 C API（只声明用得到的几个）─────────────────────────
        //
        // ⚠ 入口是 `Py_BytesMain` 而不是 `Py_Main`：
        //   `Py_Main` 收 wchar_t**（宽字符），跨平台编组很容易错；
        //   `Py_BytesMain` 收 char**（UTF-8），在安卓上更省事也更稳。
        [DllImport("python3.12", CallingConvention = CallingConvention.Cdecl)]
        static extern int Py_BytesMain(int argc, IntPtr argv);

        [DllImport("python3.12", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr Py_GetVersion();

        /// <summary>
        /// ⚠⚠ **诊断用**：手工 dlopen 一个扩展模块，验证「扩展模块能不能
        /// 找到 libpython」。
        ///
        /// 假设：`Py_Initialize()` 会去加载 `lib-dynload` 下的扩展模块
        /// （`_io`、`_locale`、`math` 之类），而**那些 .so 依赖 libpython3.12.so**。
        /// 安卓的动态加载器默认**不像 Linux 那样把 `dlopen` 进来的符号
        /// 全局共享**，而 `lib-dynload/arm64-v8a/` 与 `lib/arm64-v8a/`
        /// 又是两个目录 —— 找不到符号就会在加载时炸，
        /// 而那正是 `Py_Initialize` 会走到的路径。
        ///
        /// ⇒ 手动加载一个扩展模块，看它能不能自己找到 libpython：
        ///   · 能 ⇒ 假设不成立，得另找
        ///   · 不能 ⇒ 假设成立 ⇒ 让 `lib-dynload` 下的 .so 能被解析到
        /// </summary>
        public static string 试扩展模块(string 素材根)
        {
            var 出 = new System.Text.StringBuilder();
            try
            {
                NativeLibrary.Load("libpython3.12.so");
                出.Append("libpython 已加载；");
            }
            catch (Exception e) { 出.Append("libpython 加载失败：" + e.Message + "；"); }

            string 目录 = Path.Combine(素材根, "lib-dynload", "arm64-v8a");
            string[] 试 = { "_io.cpython-312.so", "math.cpython-312.so",
                            "_locale.cpython-312.so", "binascii.cpython-312.so" };
            foreach (string 名 in 试)
            {
                string 全 = Path.Combine(目录, 名);
                if (!File.Exists(全)) { 出.Append(名 + "（不存在）；"); continue; }
                try
                {
                    NativeLibrary.Load(全);
                    出.Append(名 + " ✓；");
                }
                catch (Exception e)
                {
                    string m = e.Message;
                    出.Append(名 + " ✗ " + e.GetType().Name + ":" +
                              (m.Length > 70 ? m.Substring(0, 70) : m) + "；");
                }
            }
            Log.Info(标签, "扩展模块探测：" + 出);
            return 出.ToString();
        }

        /// <summary>
        /// ⚠⚠ **诊断用**：只调 `Py_Initialize()`，**什么都不设**。
        ///
        /// 真机实测已确认：dlopen 成功、`Py_GetVersion()` 返回了正确的
        /// "3.12.7 (main, May 7 2025) [Clang 18.0.3 …]" ——
        /// 说明 **.so 本身在这个设备上完全可用**。
        /// 崩的是 `启动()` 里紧接的那一段。
        ///
        /// 所以下一步要分离出到底是哪一句：
        ///   · 设那几个 `Environment.SetEnvironmentVariable`？
        ///   · 还是 `Py_Initialize()` 本身？
        /// 这个方法就是干这个的：**只调 Py_Initialize，别的一概不做**。
        /// </summary>
        public static string 试初始化()
        {
            try
            {
                Log.Info(标签, "A 调 Py_Initialize()（不设任何环境变量）…");
                Py_Initialize();
                Log.Info(标签, "B Py_Initialize 返回了");
                nint 起 = Py_IsInitialized();
                Log.Info(标签, "C Py_IsInitialized() = " + 起);
                return "OK：已初始化=" + 起 + "，版本=" + (版本() ?? "?");
            }
            catch (Exception e)
            {
                Log.Error(标签, "试初始化抛异常：" + e);
                return "失败：" + e.GetType().Name + ": " + e.Message;
            }
        }
        ///
        /// 为什么要单独这一步：真机上 `PythonHost.启动()` 一进去就原生崩溃
        ///     FORTIFY: pthread_mutex_lock called on a destroyed mutex
        /// 而换线程、不起 HTTP 服务、不碰 WebView 全都一样崩。
        /// 所以要先回答一个更基本的问题：
        ///     **光是"把 libpython 加载进来"这一步本身会不会崩？**
        ///   · 这一步就崩 ⇒ 问题在 .so 与这个设备/ABI 的兼容性上，
        ///     跟我们的 Python 代码无关（也说明这条路要换）
        ///   · 这一步不崩 ⇒ 崩在后面的 C API 调用，还有得查
        /// </summary>
        public static string 试加载()
        {
            try
            {
                Log.Info(标签, "① 准备 dlopen libpython3.12.so …");
                IntPtr h = NativeLibrary.Load("libpython3.12.so");
                Log.Info(标签, "② dlopen 成功，句柄 = 0x" + h.ToString("x"));
                IntPtr v = NativeLibrary.GetExport(h, "Py_GetVersion");
                Log.Info(标签, "③ 找到 Py_GetVersion = 0x" + v.ToString("x"));
                IntPtr s = Py_GetVersion();
                string 版 = Marshal.PtrToStringAnsi(s) ?? "（空）";
                Log.Info(标签, "④ Py_GetVersion() = " + 版);
                return "OK：" + 版;
            }
            catch (Exception e)
            {
                Log.Error(标签, "试加载失败：" + e);
                return "失败：" + e.GetType().Name + ": " + e.Message;
            }
        }

        [DllImport("python3.12", CallingConvention = CallingConvention.Cdecl)]
        static extern void Py_Initialize();

        // ⚠ 返回值写成 `nint`（IntPtr）而不是 `int`：
        //   C 的 `int` 在 64 位平台上按 nint 编组，直接声明成 int 在
        //   .NET 10 上会报 CS0266「无法把 nint 隐式转成 int」。
        //   用 nint 然后显式比较，跨平台都不会错。
        [DllImport("python3.12", CallingConvention = CallingConvention.Cdecl)]
        static extern nint PyRun_SimpleStringFlags(string code, IntPtr flags);

        [DllImport("python3.12", CallingConvention = CallingConvention.Cdecl)]
        static extern nint Py_IsInitialized();

        static readonly object 锁 = new object();
        static bool 就绪;
        static string? 家目录;

        public static bool 已就绪 => 就绪;
        public static string? 根 => 家目录;

        /// <summary>Python 版本串（没起来就返回 null）。</summary>
        public static string? 版本()
        {
            try
            {
                IntPtr p = Py_GetVersion();
                return p == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(p);
            }
            catch { return null; }
        }

        /// <summary>
        /// 初始化（幂等）。传入的是 `AssetInstaller` 解出来的那个目录。
        ///
        /// 环境变量必须在**第一次**调 CPython 之前设好：
        ///   PYTHONHOME  标准库在哪（我们解出来的 python-stdlib）
        ///   PYTHONPATH  我们的引擎和课程在哪
        ///   PYTHONDONTWRITEBYTECODE  不写 .pyc —— 见下面「为什么不写」
        /// </summary>
        public static void 启动(string 素材根) => 初始化(素材根, 旧路径: false);

        /// <summary>
        /// ⚠⚠ **这一版修掉了 5 轮都没找到的那个死因**（下面 `路径` 那几行）。
        ///
        /// 旧写法把 `PYTHONPATH` 只设成 `python-app`（我们自己的引擎），
        /// 而**标准库在 `python-stdlib`**。于是 `Py_Initialize()` 走到
        /// `init_fs_encoding`（要 `import encodings`）时 **sys.path 上根本没有它**：
        ///
        ///     Fatal Python error: init_fs_encoding: failed to get the Python codec
        ///     of the filesystem encoding
        ///     ModuleNotFoundError: No module named 'encodings'
        ///
        /// 而**这句报错是写给 stderr 的，安卓应用的 fd 2 指向 /dev/null**
        /// ⇒ 一行都看不到。看到的只有进程退出时 .NET 拆锁引发的
        /// `FORTIFY: pthread_mutex_lock called on a destroyed mutex` ——
        /// 那是**收尸的噪音**，被当成了死因，一查就是 5 轮。
        ///
        /// ⚠ 关键点：**`sys.path` 必须在 `Py_Initialize()` 之前就对** ——
        ///   初始化过程中（`init_fs_encoding`）就要 import `encodings`，
        ///   而 `PyRun_SimpleStringFlags` 是初始化**之后**才跑得上的，
        ///   那时候再 `sys.path.insert` 已经晚了。
        ///   ⇒ 只能靠 `PYTHONPATH` 环境变量（初始化前设，初始化时被 getpath 收进去）。
        ///
        /// ⚠ `dyn` 是真的 `lib-dynload/<abi>/` 那一层 —— 旧写法少了 `<abi>`，
        ///   扩展模块（`_io`/`math`/`_locale`）根本不在那个目录里。
        /// </summary>
        public static void 初始化(string 素材根, bool 旧路径, bool 本地 = true)
        {
            lock (锁)
            {
                if (就绪) return;
                var 秒表 = global::Android.OS.SystemClock.ElapsedRealtime();

                string std = Path.Combine(素材根, "python-stdlib");
                string app = Path.Combine(素材根, "python-app");
                string dyn根 = Path.Combine(素材根, "lib-dynload");
                string dyn = Path.Combine(dyn根, "arm64-v8a");
                if (!Directory.Exists(dyn)) dyn = dyn根;

                // ⚠ PYTHONHOME 指向**标准库的上一级**还是标准库本身？
                //   CPython 的约定是：PYTHONHOME 下应当有 `lib/python3.12/`。
                //   我们解出来的结构是扁平的一份（stdlib 内容直接在 python-stdlib 下），
                //   所以**不能**直接用 PYTHONHOME —— 会找不到 encodings。
                //   ⇒ 用 `PYTHONPATH` + 在代码里显式设 sys.path，更可控。
                //
                // ⚠⚠ 但"显式设 sys.path"只能管**初始化之后**；初始化**之内**
                //   就要 import `encodings`。所以 PYTHONPATH 里必须**已经有标准库**。
                string 路径 = 旧路径
                    ? app                                   // ← 旧行为（复现崩溃用，别删）
                    : string.Join(new string(Path.PathSeparator, 1), new[] { std, app, dyn });
                //: ⚠⚠ 用**原生 setenv**，不是 `Environment.SetEnvironmentVariable`。
                //   实测：托管那个在安卓上到不了原生 environ，
                //   而 CPython 的 getpath 走的是 `getenv` —— 于是
                //   PYTHONPATH 设了等于没设（它的诊断原文：`PYTHONPATH = (not set)`）。
                if (本地)
                {
                    Diag.本地设环境("PYTHONPATH", 路径);
                    Diag.本地设环境("PYTHONNOUSERSITE", "1");
                    Diag.本地设环境("PYTHONDONTWRITEBYTECODE", "1");
                    Diag.本地设环境("HOME", 素材根);
                    Diag.本地设环境("TMPDIR", Path.Combine(素材根, "tmp"));
                    //: ⚠⚠ **引擎靠这个找课程**。`_paths.内容根()` 优先读
                    //   `HYBRIDENGINE_LIGHT_ROOT`，没设就退回"包目录的上一级"。
                    //   而安卓上的布局是 `<素材根>/python-app/content/lessons/…`
                    //   —— 包目录的上一级是 `python-app`，那里**没有** `lessons/`。
                    //   ⇒ 不设这一条，`lesson.list` 会返回 0 课（而不是报错，
                    //     因为"目录不存在"和"课程为空"在它眼里一样）。
                    Diag.本地设环境("HYBRIDENGINE_LIGHT_ROOT",
                                     Path.Combine(app, "content"));
                }
                Environment.SetEnvironmentVariable("PYTHONPATH", 路径);
                Environment.SetEnvironmentVariable("PYTHONNOUSERSITE", "1");
                // ⚠ 不写字节码：写 __pycache__ 需要额外的文件操作，
                //   而我们的标准库**本来就是 .pyc**（stdlib-pyc.zip），
                //   自己那份源码只有 20 来个模块，启动时编译一次代价很小。
                //   好处是首次启动少写 600 个文件（省几百毫秒）。
                Environment.SetEnvironmentVariable("PYTHONDONTWRITEBYTECODE", "1");
                // ⚠ 安卓上 `os.getcwd()` 往往是 `/`（不可写）。
                //   课程要生成答题文件，所以把 HOME 指到应用私有目录。
                Environment.SetEnvironmentVariable("HOME", 素材根);
                Environment.SetEnvironmentVariable("TMPDIR", Path.Combine(素材根, "tmp"));
                Directory.CreateDirectory(Path.Combine(素材根, "tmp"));

                //: ⚠ 把**真正设进去的值**打出来 —— "设了"和"生效了"是两件事，
                //  而这个死因（encodings 找不到）恰恰只在"没生效"时出现。
                //  ⚠ 而且这里要读**原生**侧：托管侧读到的一直是"设好了"，
                //    原生侧才是 CPython 看得见的那一份。
                Log.Info(标签, "初始化(旧路径=" + 旧路径 + " 本地env=" + 本地 + ") PYTHONPATH=" + 路径);
                Log.Info(标签, "  托管读回：" + (Environment.GetEnvironmentVariable("PYTHONPATH") ?? "?")
                               + "\n  原生读回：" + Diag.本地读环境("PYTHONPATH") + "\n  dyn=" + dyn);
                Log.Info(标签, "准备调 Py_Initialize()…");
                Py_Initialize();
                Log.Info(标签, "Py_Initialize 返回了");
                if (Py_IsInitialized() == 0)
                    throw new InvalidOperationException("Py_Initialize 之后仍未初始化");

                // ── 把路径和我们的引擎接进去 ──
                //
                // ⚠ 这里用 `PyRun_SimpleStringFlags` 跑几行 Python，而不是
                //   去调 `PySys_SetPath`（那个的签名在 3.11+ 变了，容易踩）。
                //   启动只跑一次，字符串拼接的代价可以忽略。
                // ⚠ 路径里可能有反斜杠（Windows 上调试时）—— 统一换成正斜杠，
                //   避免 Python 字符串里的转义问题。
                string 脚本 = string.Join("\n", new[]
                {
                    "import sys, os",
                    "sys.path.insert(0, r'" + std.Replace('\\', '/') + "')",
                    "sys.path.insert(0, r'" + app.Replace('\\', '/') + "')",
                    "sys.path.insert(0, r'" + dyn.Replace('\\', '/') + "')",
                    // 安卓上 time.tzset 之类没有，但 stdlib 自己会降级，不用管
                    "os.environ.setdefault('PYTHONIOENCODING', 'utf-8')",
                    // 让 print 立刻出来（不然日志要等到缓冲满，调试时看不见）
                    "import io",
                    "try:",
                    "    sys.stdout.reconfigure(line_buffering=True)",
                    "    sys.stderr.reconfigure(line_buffering=True)",
                    "except Exception: pass",
                });
                if (PyRun_SimpleStringFlags(脚本, IntPtr.Zero) != 0)
                    throw new InvalidOperationException(
                        "设置 sys.path 失败 —— 多半是标准库不完整（encodings 缺了会这样）");

                家目录 = 素材根;
                就绪 = true;
                long 毫秒 = global::Android.OS.SystemClock.ElapsedRealtime() - 秒表;
                Log.Info(标签, "Python 起来了：" + 版本() + "，用时 " + 毫秒 + " ms");
            }
        }

        /// <summary>
        /// ⚠⚠ **跑学员代码并拿回 print 的内容** —— 判分的地基。
        ///
        /// 原实现只能回答"成没成功"（`PyRun_SimpleStringFlags` 的返回值），
        /// **拿不回 print 写过什么**。而判分、看输出、报错都要它。
        ///
        /// 做法：**不碰 C 侧的 `PySys_SetObject`**（那要自己实现一个 Python
        /// 可调用对象，坑多），而是让 Python 自己把 `sys.stdout/stderr`
        /// 换成 `io.StringIO`，跑完再写进两个文件，C# 读文件。
        ///
        /// ⚠ 学员代码用 **base64** 传进去，而不是拼进字符串字面量 ——
        ///   学员代码里引号、反斜杠、换行、中文什么都有，
        ///   拼字符串是**跨语言转义的经典事故点**（本仓在这上面栽过：
        ///   模板里 JS 的转义要穿两层、漏一层就整页白屏）。
        ///   base64 之后只剩 `A-Za-z0-9+/=`，怎么都不会破坏语法。
        /// </summary>
        public static (bool 成功, string 出, string 错) 跑并抓(string 源码, string 家)
        {
            if (!就绪) return (false, "", "Python 还没初始化");
            try
            {
                string 路出 = Path.Combine(家, "hel-跑-出.txt").Replace('\\', '/');
                string 路错 = Path.Combine(家, "hel-跑-错.txt").Replace('\\', '/');
                string b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(源码));
                string 脚本 = string.Join("\n", new[]
                {
                    "import base64, io, sys, traceback",
                    "_src = base64.b64decode('" + b64 + "').decode('utf-8')",
                    "_o, _e = io.StringIO(), io.StringIO()",
                    "_so, _se = sys.stdout, sys.stderr",
                    "sys.stdout, sys.stderr = _o, _e",
                    "try:",
                    "    exec(compile(_src, '<学员代码>', 'exec'), {'__name__': '__main__'})",
                    "except BaseException:",
                    "    traceback.print_exc()",
                    "finally:",
                    "    sys.stdout, sys.stderr = _so, _se",
                    "open(r'" + 路出 + "', 'w', encoding='utf-8').write(_o.getvalue())",
                    "open(r'" + 路错 + "', 'w', encoding='utf-8').write(_e.getvalue())",
                });
                nint r = PyRun_SimpleStringFlags(脚本, IntPtr.Zero);
                string 出 = File.Exists(路出) ? File.ReadAllText(路出) : "";
                string 错 = File.Exists(路错) ? File.ReadAllText(路错) : "";
                bool 好 = r == 0 && 错.Length == 0;
                Log.Info(标签, "跑并抓：r=" + r + " 出=" + 出.Length + " 字 错=" + 错.Length + " 字");
                return (好, 出, 错);
            }
            catch (Exception e)
            {
                return (false, "", e.GetType().Name + ": " + e.Message);
            }
        }

        /// <summary>
        /// ⚠⚠ **判分的地基：把一行 `ctl.py` 协议请求送进引擎，拿回一行 JSON 响应。**
        ///
        /// 这一层之前是空的（`桥.ctl` 只回一句"协议层还没接上"）。
        /// 接法刻意选"**进程内直接调 `ctl.处理一行`**"，而不是"起子进程跑 ctl"：
        ///   · 安卓上起不了自己的可执行文件（PEP 738 原文），子进程那条路根本不通
        ///   · `处理一行` 是纯函数式的一进一出，正好适合嵌进来
        ///   · 引擎那边的判定逻辑（`check.run_checks` / `examrun._grade_code`）
        ///     **一行都不用改** —— 换的是宿主，不是引擎
        ///
        /// ⚠ 请求用 **base64** 传（学员代码里引号/换行/中文都有），
        ///   响应写文件再由 C# 读回 —— 不走 stdout，因为学员代码自己会 print。
        /// </summary>
        public static string 调协议(string 请求, string 家)
        {
            if (!就绪) return "{\"ok\":false,\"err\":{\"code\":-1,\"msg\":\"Python 还没起来\"}}";
            try
            {
                string 路 = Path.Combine(家, "hel-协议.json").Replace('\\', '/');
                string b64 = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(请求 ?? ""));
                string 脚本 = string.Join("\n", new[]
                {
                    "import base64, json, traceback",
                    "请求 = base64.b64decode('" + b64 + "').decode('utf-8')",
                    "try:",
                    "    from hybridengine_light import ctl",
                    "    _id, _ok, _r, _msg, _code = ctl.处理一行(请求)",
                    "    出 = json.dumps({'id': _id, 'ok': bool(_ok), 'r': _r,",
                    "                     'err': None if _ok else {'code': _code, 'msg': _msg}},",
                    "                    ensure_ascii=False, default=str)",
                    "except BaseException:",
                    "    出 = json.dumps({'ok': False, 'err': {'code': -3,",
                    "                     'msg': traceback.format_exc()[-1500:]}}, ensure_ascii=False)",
                    "open(r'" + 路 + "', 'w', encoding='utf-8').write(出)",
                });
                PyRun_SimpleStringFlags(脚本, IntPtr.Zero);
                return File.Exists(路) ? File.ReadAllText(路) : "{\"ok\":false,\"err\":{\"msg\":\"没有响应文件\"}}";
            }
            catch (Exception e)
            {
                return "{\"ok\":false,\"err\":{\"msg\":\"" + e.GetType().Name + ": "
                       + e.Message.Replace("\"", "'") + "\"}}";
            }
        }

        /// <summary>
        /// ⚠⚠ **判一课的答案 —— 「完美移植」最后一块。**
        ///
        /// 走的是引擎**自己的**检查：`check.load_module` + `check.run_checks`
        /// （课程文件里那些 `CHECK_*` 函数），一行判定逻辑都没有重写。
        ///
        /// ⚠ 为什么不直接用 `ctl.lesson.check`：那条路的 python 线要
        ///   `from . import run`，而 **`run.py` 顶层引 tkinter**，
        ///   安卓上根本没有 tkinter（PEP 738）⇒ 那条路在手机上是死的。
        ///   所以这里照 `ctl._跑检查` 的形状走 `check`，
        ///   **只把"答案文件读写"这部分自己做了**（学员在界面上打的字
        ///   要落成答题文件才能被 `load_module` 加载）。
        ///
        /// ⚠ 答题文件写在 `<内容根>/lessons/answers/<课>.py` —— 那是**学员自己的
        ///   答题区**（桌面版同一位置），不是课程内容，改它不违反
        ///   「课程内容不许改」。
        /// </summary>
        public static string 判课(string 课号, string 代码, string 素材根, string 家)
        {
            if (!就绪) return "{\"ok\":false,\"err\":{\"msg\":\"Python 还没起来\"}}";
            try
            {
                string 路 = Path.Combine(家, "hel-判.json").Replace('\\', '/');
                string b64课 = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(课号 ?? ""));
                string b64码 = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(代码 ?? ""));
                string 脚本 = string.Join("\n", new[]
                {
                    "import base64, glob, json, os, traceback",
                    "课号 = base64.b64decode('" + b64课 + "').decode('utf-8').strip()",
                    "代码 = base64.b64decode('" + b64码 + "').decode('utf-8')",
                    "try:",
                    "    from pathlib import Path",
                    "    from hybridengine_light import check as _check",
                    "    from hybridengine_light._paths import 内容根",
                    "    root = 内容根()",
                    "    匹配 = sorted(glob.glob(str(root / 'lessons' / (课号 + '_*.py'))))",
                    "    if not 匹配:",
                    "        raise RuntimeError('找不到课程文件 lessons/' + 课号 + '_*.py（内容根=' + str(root) + '）')",
                    "    课文件 = Path(匹配[0])",
                    "    答目录 = root / 'lessons' / 'answers'",
                    "    答目录.mkdir(parents=True, exist_ok=True)",
                    "    答文件 = 答目录 / 课文件.name",
                    "    答文件.write_text(代码, encoding='utf-8')",
                    "    课 = _check.load_module(课文件, name='hel_课' + 课号)",
                    // ⚠ 多文件作业要能 `import 自己的模块` —— ctl 是 chdir 到作业目录
                    "    # （单文件作业则 chdir 到内容根，与 ctl._跑检查 一致）",
                    "    作业目录 = 答目录 / 课文件.stem",
                    "    旧 = os.getcwd()",
                    "    try:",
                    "        os.chdir(str(作业目录 if (作业目录 / 'main.py').exists() else root))",
                    "        已 = _check.load_module(答文件, name='hel_答' + 课号)",
                    "    finally:",
                    "        os.chdir(旧)",
                    "    rep = _check.run_checks(课, 已)",
                    "    出 = json.dumps({'ok': bool(rep.all_ok), 'passed': rep.passed,",
                    "                     'total': rep.total, 'error': rep.error,",
                    "                     'results': [{'name': r.name, 'ok': bool(r.ok),",
                    "                                  'detail': r.detail} for r in rep.results]},",
                    "                    ensure_ascii=False, default=str)",
                    "except BaseException:",
                    "    出 = json.dumps({'ok': False,",
                    "                     'err': {'msg': traceback.format_exc()[-1500:]}},",
                    "                    ensure_ascii=False)",
                    "open(r'" + 路 + "', 'w', encoding='utf-8').write(出)",
                });
                PyRun_SimpleStringFlags(脚本, IntPtr.Zero);
                string 果 = File.Exists(路) ? File.ReadAllText(路)
                                           : "{\"ok\":false,\"err\":{\"msg\":\"没有响应文件\"}}";
                Log.Info(标签, "判课 " + 课号 + " → " + (果.Length > 300 ? 果.Substring(0, 300) : 果));
                return 果;
            }
            catch (Exception e)
            {
                return "{\"ok\":false,\"err\":{\"msg\":\"" + e.GetType().Name + ": "
                       + e.Message.Replace("\"", "'") + "\"}}";
            }
        }

        /// <summary>
        /// 跑一段 Python 源码，返回 (成功, 输出)。
        ///
        /// ⚠ 现在只能拿到"成功与否" —— **拿不回 print 的内容**。
        ///   要拿回来得把 `sys.stdout` 换成我们自己实现的 writer
        ///   （C 侧 `PySys_SetObject` + 一个 Python 可调用对象），
        ///   那是下一步的事。当前这个页面只需要证明 Python 真的起来了。
        ///   ⇒ 已由上面的 `跑并抓` 补上（用 StringIO，不是 C 侧 writer）。
        ///
        /// ⚠ 必须在**后台线程**调 —— Python 代码可能是死循环（学员写的），
        ///   在 UI 线程上跑会把界面卡死，而那个症状看起来像"应用崩了"。
        /// </summary>
        public static (bool 成功, string 说明) 跑(string 源码)
        {
            if (!就绪) return (false, "Python 还没初始化");
            try
            {
                nint r = PyRun_SimpleStringFlags(源码, IntPtr.Zero);
                return (r == 0, r == 0 ? "ok" : "Python 返回非 0（异常已打到 stderr）");
            }
            catch (Exception e)
            {
                return (false, e.GetType().Name + ": " + e.Message);
            }
        }

        /// <summary>
        /// 自检：跑一句真代码，确认真能用。
        /// 这是"最小闭环"——先证明 Python 起来了，再往上搭课程界面。
        /// </summary>
        public static (bool ok, string 详情) 自检()
        {
            var 表 = global::Android.OS.SystemClock.ElapsedRealtime();
            try
            {
                nint r = PyRun_SimpleStringFlags(
                    "import sys, os\n" +
                    "assert sys.version_info[:2] == (3, 12), sys.version\n" +
                    "import json, re, math, random, collections, itertools\n" +
                    "assert json.loads(json.dumps({'a': [1, 2]}))['a'][1] == 2\n" +
                    "assert math.gcd(12, 18) == 6\n" +
                    "assert list(itertools.islice(itertools.count(), 3)) == [0, 1, 2]\n",
                    IntPtr.Zero);
                long 毫秒 = global::Android.OS.SystemClock.ElapsedRealtime() - 表;
                if (r != 0) return (false, "自检脚本返回非 0（看 logcat 的 python.stderr）");
                return (true, string.Format("Python {0}（{1} ms）", 版本(), 毫秒));
            }
            catch (Exception e)
            {
                return (false, e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
