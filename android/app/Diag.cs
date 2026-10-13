using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Android.Util;

namespace HybridEngineLight.Android
{
    /// <summary>
    /// 真机诊断工具（**只在调试期用**，界面不依赖它）。
    ///
    /// ⚠⚠ 为什么需要它 —— 这一层是**被逼出来的**，不是"顺手加的"：
    ///
    ///   前 5 轮排查只看得见一行 logcat：
    ///       F libc: FORTIFY: pthread_mutex_lock called on a destroyed mutex
    ///   而**看不到 CPython 自己说了什么**。原因是硬的：
    ///
    ///   · 安卓应用的 **fd 1 / fd 2 默认指向 /dev/null** ——
    ///     CPython 的 `Py_FatalError` 是往 stderr 写的，**写进去就没了**。
    ///   · 而 `Py_FatalError` 会 `abort()`/`exit()` 结束进程；
    ///     进程一退，.NET 运行时开始拆自己的锁，此时还在跑的
    ///     几个线程就会撞上"已经销毁的互斥量" ⇒ 才有那行 FORTIFY。
    ///
    ///   ⇒ 也就是说：**那行 FORTIFY 很可能是"收尸"时的噪音，不是死因。**
    ///     死因（Python 的报错原文）一直写在 /dev/null 里。
    ///
    ///   本文件做两件事：
    ///     ① `装重定向()` —— 把 fd 1/2 接到应用私有目录的文件上，
    ///        崩完**下一次启动**用 `读上次输出()` 捞回来（安卓上这是唯一
    ///        能拿到原生进程 stderr 的办法，设备没 root、应用非 debuggable）。
    ///     ② `地图()/谁()/符号()/线程()` —— 回答"那个地址属于哪个 .so"，
    ///        以及"libpython 到底被加载了几份"。
    ///
    /// ⚠ `log.redirect-stdio` 那条路**试过，不行**（实测）：
    ///     adb shell setprop log.redirect-stdio true
    ///     ⇒ Failed to set property 'log.redirect-stdio' to 'true'.
    ///   所以只能自己 dup2。
    /// </summary>
    static class Diag
    {
        const string 标签 = "HybridEngineLight";
        /// <summary>CPython 的 stderr 落到 logcat 时用的标签（好看出来）。</summary>
        public const string 标签错 = "python.stderr";
        public const string 标签出 = "python.stdout";

        // ── libc ─────────────────────────────────────────────────────────
        [DllImport("libc.so", CallingConvention = CallingConvention.Cdecl)]
        static extern int dup2(int 旧, int 新);

        [DllImport("libc.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "open")]
        static extern int 开(string 路径, int 标志, int 模式);

        [DllImport("libc.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "close")]
        static extern int 关(int 句柄);

        [DllImport("libc.so", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr dlopen(string 名, int 标志);

        [DllImport("libc.so", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr dlsym(IntPtr 句柄, string 符号);

        [DllImport("libc.so", CallingConvention = CallingConvention.Cdecl)]
        static extern int dladdr(IntPtr 地址, out Dl信息 信息);

        [StructLayout(LayoutKind.Sequential)]
        struct Dl信息
        {
            public IntPtr 文件;      // dli_fname
            public IntPtr 基址;      // dli_fbase
            public IntPtr 符号;      // dli_sname
            public IntPtr 符号地址;  // dli_saddr
        }

        const int O_WRONLY = 1, O_CREAT = 64, O_TRUNC = 512;
        const int RTLD_LAZY = 1, RTLD_NOW = 2, RTLD_GLOBAL = 0x100;

        public const string 名错 = "hel-stderr.txt";
        public const string 名出 = "hel-stdout.txt";

        // ── ① 重定向 ─────────────────────────────────────────────────────

        /// <summary>
        /// 把本进程的 fd 1 / fd 2 接到 `<家>/hel-stdout.txt` / `hel-stderr.txt`。
        ///
        /// ⚠ 必须在**调用 CPython 之前**做：CPython 的 `Py_FatalError` 只往 stderr 写。
        /// ⚠ 用 `O_TRUNC` 每次清空 —— 这样文件里只可能有"这一次"的内容，不会有旧噪音。
        /// </summary>
        public static string 装重定向(string 家)
        {
            var 出 = new StringBuilder();
            try
            {
                string 路错 = Path.Combine(家, 名错);
                string 路出 = Path.Combine(家, 名出);
                int f2 = 开(路错, O_WRONLY | O_CREAT | O_TRUNC, 420);
                int f1 = 开(路出, O_WRONLY | O_CREAT | O_TRUNC, 420);
                出.Append("stderr fd=").Append(f2).Append("；stdout fd=").Append(f1);
                if (f2 >= 0) { dup2(f2, 2); 关(f2); }
                if (f1 >= 0) { dup2(f1, 1); 关(f1); }
                出.Append("；dup2 完成 → ").Append(家);
            }
            catch (Exception e)
            {
                出.Append("失败：").Append(e.GetType().Name).Append(": ").Append(e.Message);
            }
            Log.Info(标签, "装重定向：" + 出);
            return 出.ToString();
        }

        /// <summary>把上一次（或刚才）重定向下来的输出喂进 logcat。</summary>
        public static string 读输出(string 家, int 最多行 = 400)
        {
            var 出 = new StringBuilder();
            foreach (var (名, 标) in new[] { (名错, 标签错), (名出, 标签出) })
            {
                string 路 = Path.Combine(家, 名);
                try
                {
                    if (!File.Exists(路)) { Log.Info(标签, "（没有 " + 名 + "）"); continue; }
                    string 文 = File.ReadAllText(路, Encoding.UTF8);
                    Log.Info(标签, "=== " + 名 + "：" + 文.Length + " 字节 ===");
                    出.Append("【").Append(名).Append(" ").Append(文.Length).Append(" 字节】");
                    int n = 0;
                    foreach (string 行 in 文.Split('\n'))
                    {
                        if (n++ >= 最多行) { Log.Info(标, "…（截断，只显示前 " + 最多行 + " 行）"); break; }
                        string 净 = 行.TrimEnd('\r');
                        if (净.Length == 0) continue;
                        Log.Info(标, 净);
                        出.Append('\n').Append(净);
                    }
                }
                catch (Exception e)
                {
                    Log.Warn(标签, "读 " + 名 + " 失败：" + e.Message);
                }
            }
            return 出.ToString();
        }

        /// <summary>把某个文件的内容喂进 logcat（供 python.stdout 用）。</summary>
        public static string 读文件为(string 路, string 标, int 最多行 = 400)
        {
            var 出 = new StringBuilder();
            try
            {
                if (!File.Exists(路)) return "（没有 " + 路 + "）";
                string 文 = File.ReadAllText(路, Encoding.UTF8);
                int n = 0;
                foreach (string 行 in 文.Split('\n'))
                {
                    if (n++ >= 最多行) break;
                    string 净 = 行.TrimEnd('\r');
                    if (净.Length == 0) continue;
                    Log.Info(标, 净);
                    出.Append('\n').Append(净);
                }
            }
            catch (Exception e) { 出.Append("读失败：").Append(e.Message); }
            return 出.ToString();
        }

        // ── ② 地址归属 ───────────────────────────────────────────────────

        public static string 谁(ulong 址)
        {
            try
            {
                Dl信息 i;
                int r = dladdr((IntPtr)(long)址, out i);
                if (r == 0)
                    return string.Format("0x{0:x} → dladdr 说它不在任何已加载模块里（堆/栈？）", 址);
                string 文 = i.文件 == IntPtr.Zero ? "?" : (Marshal.PtrToStringAnsi(i.文件) ?? "?");
                string 号 = i.符号 == IntPtr.Zero
                    ? "（模块内无最近符号）" : (Marshal.PtrToStringAnsi(i.符号) ?? "?");
                return string.Format(
                    "0x{0:x} → {1}\n        模块基址 0x{2:x}，模块内偏移 0x{3:x}，最近符号 {4} +0x{5:x}",
                    址, 文, (long)i.基址, (long)址 - (long)i.基址, 号, (long)址 - (long)i.符号地址);
            }
            catch (Exception e) { return "dladdr 抛异常：" + e.GetType().Name + ": " + e.Message; }
        }

        /// <summary>参数形如 "0x711b60eaf8,0x711b611268"。</summary>
        public static string 谁们(string 参)
        {
            var 出 = new StringBuilder();
            foreach (string 段 in (参 ?? "").Split(','))
            {
                string s = 段.Trim();
                if (s.Length == 0) continue;
                try
                {
                    ulong a = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? Convert.ToUInt64(s.Substring(2), 16) : Convert.ToUInt64(s);
                    出.Append(谁(a)).Append('\n');
                }
                catch (Exception e) { 出.Append(s).Append(" 解析失败：").Append(e.Message).Append('\n'); }
            }
            return 出.ToString();
        }

        /// <summary>关键符号在哪 —— 顺便回答「libpython 加载了几份」。</summary>
        public static string 符号()
        {
            var 出 = new StringBuilder();
            IntPtr 全 = dlopen(null, RTLD_NOW);
            出.Append("dlopen(NULL) = 0x").Append(全.ToString("x")).Append('\n');

            IntPtr py = dlopen("libpython3.12.so", RTLD_NOW);
            出.Append("dlopen(libpython3.12.so, RTLD_NOW) = 0x").Append(py.ToString("x")).Append('\n');

            string[] 名 = { "Py_Initialize", "Py_IsInitialized", "Py_GetVersion",
                            "_PyRuntime", "_PyRuntimeState_Init", "PyThread_allocate_lock",
                            "Py_FatalError", "Py_GetPath", "Py_SetPath" };
            foreach (string n in 名)
            {
                IntPtr a = 句柄符号(py, n);
                IntPtr b = 句柄符号(全, n);
                出.Append(string.Format("  {0,-24} libpython=0x{1:x}  全局=0x{2:x}{3}\n",
                    n, (long)a, (long)b, a != IntPtr.Zero && b != IntPtr.Zero && a != b ? "  ⚠ 两处不同！" : ""));
            }
            //: 那个"被销毁的互斥量"到底是谁在用 —— 看 libc 里的锁函数归属
            IntPtr c = dlopen("libc.so", RTLD_NOW);
            foreach (string n in new[] { "pthread_mutex_lock", "pthread_mutex_destroy", "_exit", "abort" })
                出.Append(string.Format("  libc {0,-22} =0x{1:x}\n", n, (long)句柄符号(c, n)));

            foreach (string n in new[] { "mono_runtime_init", "mono_thread_attach", "monodroid_get_identity_hash_code" })
                出.Append(string.Format("  libmono {0,-22} =0x{1:x}\n", n, (long)句柄符号(全, n)));

            Log.Info(标签, "符号：\n" + 出);
            return 出.ToString();
        }

        static IntPtr 句柄符号(IntPtr 句柄, string 名)
        {
            try { return dlsym(句柄, 名); } catch { return IntPtr.Zero; }
        }

        // ── ③ /proc ──────────────────────────────────────────────────────

        /// <summary>把所有**文件映射**逐行写进 logcat —— 用来判断某个地址落在哪个 .so 里。</summary>
        public static string 地图()
        {
            var 出 = new StringBuilder();
            int 总 = 0, python份数 = 0;
            try
            {
                foreach (string 行 in File.ReadAllLines("/proc/self/maps"))
                {
                    总++;
                    if (行.Contains("libpython")) python份数++;
                    if (行.Contains("/")) Log.Info(标签, "map " + 行);
                }
            }
            catch (Exception e) { return "读 /proc/self/maps 失败：" + e.GetType().Name + ": " + e.Message; }
            出.Append("maps 共 ").Append(总).Append(" 行，其中含 libpython 的 ").Append(python份数).Append(" 行");
            Log.Info(标签, 出.ToString());
            return 出.ToString();
        }

        /// <summary>进程里现在有哪些线程（名字 + tid）—— 崩的是哪几个线程要靠它认。</summary>
        public static string 线程()
        {
            var 出 = new StringBuilder();
            try
            {
                foreach (string 行 in File.ReadAllLines("/proc/self/status"))
                    if (行.StartsWith("Threads") || 行.StartsWith("Name"))
                        出.Append("status ").Append(行).Append('\n');
                var 名们 = new List<string>();
                foreach (string 目 in Directory.GetDirectories("/proc/self/task"))
                {
                    try
                    {
                        string c = File.ReadAllText(Path.Combine(目, "comm")).Trim();
                        名们.Add(Path.GetFileName(目) + "=" + c);
                    }
                    catch { }
                }
                出.Append("线程 ").Append(名们.Count).Append(" 个：").Append(string.Join(" ", 名们));
            }
            catch (Exception e) { 出.Append("读 /proc/self/task 失败：").Append(e.Message); }
            Log.Info(标签, "线程：" + 出);
            return 出.ToString();
        }

        /// <summary>看环境变量有没有真的设进去（`setenv` 在安卓上不是理所当然）。</summary>
        public static string 环境()
        {
            var 出 = new StringBuilder();
            foreach (string n in new[] { "PYTHONPATH", "PYTHONHOME", "PYTHONNOUSERSITE",
                                         "PYTHONDONTWRITEBYTECODE", "HOME", "TMPDIR" })
                出.Append(n).Append('=').Append(Environment.GetEnvironmentVariable(n) ?? "（未设）").Append('\n');
            Log.Info(标签, "环境：\n" + 出);
            return 出.ToString();
        }

        /// <summary>手工 dlopen（可以指定 RTLD_GLOBAL）—— 用来单独验 RTLD 那条假设。</summary>
        public static string 手工加载(string 名, int 标志)
        {
            try
            {
                IntPtr h = dlopen(名, 标志);
                string 说 = string.Format("dlopen(\"{0}\", 0x{1:x}) = 0x{2:x}", 名, 标志, (long)h);
                Log.Info(标签, 说);
                return 说;
            }
            catch (Exception e) { return "dlopen 抛异常：" + e.GetType().Name + ": " + e.Message; }
        }

        public const int 标志_全局 = RTLD_NOW | RTLD_GLOBAL;
        public const int 标志_本地 = RTLD_NOW | RTLD_LAZY;

        // ── ④ 实验台 ─────────────────────────────────────────────────────
        //
        // ⚠⚠ 为什么要"一次只跑一个实验"：
        //   这是本仓踩过的第 23 条教训 —— **一次只动一个变量**。
        //   前 5 轮之所以烧掉，是因为每次都把"起 HTTP / 换线程 / 提前到 Activity"
        //   和"Py_Initialize 本身"搅在一起，崩了也分不清是谁。
        //   ⇒ 这里把每个假设做成**一个独立入口**，每次**冷启动一个进程**
        //     只跑一个，看它自己的 logcat。之后谁再说"我试过 X"都能复现。
        //
        // 用法（真机，CDP）：
        //     python _build/_cdp.py "桥.Dia('bare','')"
        //     python _build/_cdp.py "桥.Dia('who','0x711b60eaf8')"
        //     python _build/_cdp.py "桥.Dia('dump','')"      ← 上一次的 stderr
        public static string 跑实验(string 步, string 参, string 家, string 素材根)
        {
            Log.Info(标签, "━━ 实验：" + 步 + " 参=" + 参);

            switch ((步 ?? "").Trim())
            {
                // ── 只读观测（不改行为）──
                case "dump": return 读输出(家);
                case "maps": return 地图();
                case "sym": return 符号();
                case "who": return 谁们(参);
                case "threads": return 线程();
                case "env": return 环境();
                case "stderr": return 装重定向(家);

                // ── 一个个假设，各自冷启动一个进程 ──
                case "bare":
                    装重定向(家);
                    return "裸 Py_Initialize：" + PythonHost.试初始化();

                case "global":
                    装重定向(家);
                    {
                        string 加载;
                        try
                        {
                            Java.Lang.JavaSystem.LoadLibrary("python3.12");
                            //: 安卓的 System.loadLibrary 走 RTLD_GLOBAL
                            加载 = "JavaSystem.LoadLibrary(\"python3.12\") 成功";
                        }
                        catch (Exception e)
                        {
                            加载 = "JavaSystem.LoadLibrary 失败：" + e.GetType().Name
                                   + ": " + e.Message;
                        }
                        Log.Info(标签, 加载);
                        return 加载 + "\n" + PythonHost.试初始化();
                    }

                case "dlglobal":
                    装重定向(家);
                    return 手工加载("libpython3.12.so", 标志_全局) + "\n" + PythonHost.试初始化();

                case "dllocal":
                    装重定向(家);
                    return 手工加载("libpython3.12.so", 标志_本地) + "\n" + PythonHost.试初始化();

                case "old":
                    装重定向(家);
                    try { PythonHost.初始化(素材根, 旧路径: true); }
                    catch (Exception e) { return "初始化(旧)抛：" + e.GetType().Name + ": " + e.Message; }
                    return "初始化(旧)通了：" + (PythonHost.版本() ?? "?");

                case "fix":
                    装重定向(家);
                    try { PythonHost.初始化(素材根, 旧路径: false); }
                    catch (Exception e) { return "初始化(新)抛：" + e.GetType().Name + ": " + e.Message; }
                    return "初始化(新)通了：" + (PythonHost.版本() ?? "?");

                case "run":
                    装重定向(家);
                    try
                    {
                        PythonHost.初始化(素材根, 旧路径: false);
                        var (好, 出, 错) = PythonHost.跑并抓("print(1+2)", 家);
                        return string.Format("跑 print(1+2)：成功={0}\n【stdout】\n{1}\n【stderr】\n{2}",
                                             好, 出, 错);
                    }
                    catch (Exception e) { return "抛：" + e.GetType().Name + ": " + e.Message; }

                case "self":
                    装重定向(家);
                    try
                    {
                        PythonHost.初始化(素材根, 旧路径: false);
                        var (ok, 详情) = PythonHost.自检();
                        return "自检：" + (ok ? "过 " : "没过 ") + 详情;
                    }
                    catch (Exception e) { return "抛：" + e.GetType().Name + ": " + e.Message; }
            }
            return "（没有这个实验：" + 步 + "）可选：dump maps sym who threads env stderr "
                 + "bare global dlglobal dllocal old fix run self";
        }
    }
}
