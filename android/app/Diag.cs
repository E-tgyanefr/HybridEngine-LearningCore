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

        //: ⚠⚠ **必须走 libc 的 setenv** —— 实测 `.NET` 的
        //   `Environment.SetEnvironmentVariable` 在安卓上**到不了原生 environ**：
        //   托管侧读回来是设好的，而 libpython 的 `getenv("PYTHONPATH")` 是 NULL，
        //   CPython 的路径诊断原文就写着 `PYTHONPATH = (not set)`。
        //   ⇒ 环境变量设了等于没设 —— 这个坑本身就值得单独记一笔。
        [DllImport("libc.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "setenv")]
        static extern int 设原生环境(string 名, string 值, int 覆盖);

        [DllImport("libc.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "getenv")]
        static extern IntPtr 取原生环境(string 名);

        /// <summary>用 libc 设环境变量，并**从原生侧读回来**验证。</summary>
        public static string 本地设环境(string 名, string 值)
        {
            int r;
            try { r = 设原生环境(名, 值, 1); }
            catch (Exception e) { return "setenv(" + 名 + ") 抛：" + e.Message; }
            string 回 = 本地读环境(名);
            string 说 = string.Format("setenv({0}) 返回 {1}，原生 getenv 读回 {2}", 名, r, 回);
            Log.Info(标签, 说);
            return 说;
        }

        /// <summary>从原生侧读环境变量（托管侧读到的可能不是同一个）。</summary>
        public static string 本地读环境(string 名)
        {
            try
            {
                IntPtr p = 取原生环境(名);
                if (p == IntPtr.Zero) return "（null）";
                return Marshal.PtrToStringAnsi(p) ?? "（空）";
            }
            catch (Exception e) { return "getenv 抛：" + e.Message; }
        }

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
        public static string 装重定向(string 家, string 标记 = "")
        {
            var 出 = new StringBuilder();
            try
            {
                //: ⚠ 文件名带**实验标记** —— 否则分不清"这次的输出"和"上一次的残留"。
                //   我在这上面绕过一次：`fix` 实验崩了之后，下一次启动读到的
                //   其实是 `bare` 那次的旧文件（字节数一样），差点得出"修了也没用"。
                string 尾 = string.IsNullOrEmpty(标记) ? "" : "-" + 标记;
                string 路错 = Path.Combine(家, "hel" + 尾 + "-错.txt");
                string 路出 = Path.Combine(家, "hel" + 尾 + "-出.txt");
                int f2 = 开(路错, O_WRONLY | O_CREAT | O_TRUNC, 420);
                int f1 = 开(路出, O_WRONLY | O_CREAT | O_TRUNC, 420);
                出.Append("stderr fd=").Append(f2).Append("；stdout fd=").Append(f1);
                if (f2 >= 0) { dup2(f2, 2); 关(f2); }
                if (f1 >= 0) { dup2(f1, 1); 关(f1); }
                出.Append("；dup2 完成 → ").Append(路错);
            }
            catch (Exception e)
            {
                出.Append("失败：").Append(e.GetType().Name).Append(": ").Append(e.Message);
            }
            Log.Info(标签, "装重定向：" + 出);
            return 出.ToString();
        }

        /// <summary>把**所有**实验留下的输出喂进 logcat（每个实验一个文件，不会互相盖）。</summary>
        public static string 读输出(string 家, int 最多行 = 400)
        {
            var 出 = new StringBuilder();
            string[] 们;
            try { 们 = Directory.GetFiles(家, "hel*-*.txt"); }
            catch (Exception e) { return "列目录失败：" + e.Message; }
            Array.Sort(们);
            if (们.Length == 0) { Log.Info(标签, "（还没有任何实验输出文件）"); return "（无）"; }
            foreach (string 路 in 们)
            {
                string 名 = Path.GetFileName(路);
                string 标 = 名.Contains("错") ? 标签错 : 标签出;
                try
                {
                    var 信 = new FileInfo(路);
                    string 文 = File.ReadAllText(路, Encoding.UTF8);
                    Log.Info(标签, "=== " + 名 + "：" + 文.Length + " 字节，改于 "
                                   + 信.LastWriteTime.ToString("HH:mm:ss") + " ===");
                    if (文.Length == 0) continue;
                    出.Append("【").Append(名).Append(' ').Append(文.Length).Append(" 字节】");
                    int n = 0;
                    foreach (string 行 in 文.Split('\n'))
                    {
                        if (n++ >= 最多行) { Log.Info(标, "…（截断）"); break; }
                        string 净 = 行.TrimEnd('\r');
                        if (净.Length == 0) continue;
                        Log.Info(标, 净);
                        出.Append('\n').Append(净);
                    }
                    //: 上一轮的文件清掉，免得越攒越多、也免得又看成"这一次的"
                    try { File.Delete(路); } catch { }
                }
                catch (Exception e) { Log.Warn(标签, "读 " + 名 + " 失败：" + e.Message); }
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
        public static string 跑实验(string 步, string 参, string 家, string 素材根,
                                    global::Android.App.Activity? 活动 = null, string? src = null)
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

                // ── 一个个假设，各自冷启动一个进程 ──
                case "bare":
                    装重定向(家, 步);
                    return "裸 Py_Initialize：" + PythonHost.试初始化();

                case "global":
                    装重定向(家, 步);
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
                    装重定向(家, 步);
                    return 手工加载("libpython3.12.so", 标志_全局) + "\n" + PythonHost.试初始化();

                case "dllocal":
                    装重定向(家, 步);
                    return 手工加载("libpython3.12.so", 标志_本地) + "\n" + PythonHost.试初始化();

                case "old":
                    装重定向(家, 步);
                    try { PythonHost.初始化(素材根, 旧路径: true, 本地: false); }
                    catch (Exception e) { return "初始化(旧)抛：" + e.GetType().Name + ": " + e.Message; }
                    return "初始化(旧)通了：" + (PythonHost.版本() ?? "?");

                case "fix":
                    装重定向(家, 步);
                    try { PythonHost.初始化(素材根, 旧路径: false, 本地: false); }
                    catch (Exception e) { return "初始化(新)抛：" + e.GetType().Name + ": " + e.Message; }
                    return "初始化(新)通了：" + (PythonHost.版本() ?? "?");

                //: ⚠⚠ 关键对照：路径对了 + **用原生 setenv**
                case "fix2":
                    装重定向(家, 步);
                    try { PythonHost.初始化(素材根, 旧路径: false, 本地: true); }
                    catch (Exception e) { return "初始化(原生env)抛：" + e.GetType().Name + ": " + e.Message; }
                    return "初始化(原生env)通了：" + (PythonHost.版本() ?? "?");

                case "run":
                    装重定向(家, 步);
                    try
                    {
                        PythonHost.初始化(素材根, 旧路径: false, 本地: true);
                        //: ⚠⚠ **`参` 可以是 base64 源码 ⇒ 真机上跑任意 Python 片段。**
                        //:
                        //:   这条把实验台从"一次假设要重建一次 APK"变成
                        //:   "**一次 `am start` 验一个假设**" —— 构建+装机要 1~2 分钟，
                        //:   而探一个想法只需要 15 秒。
                        //:
                        //:   ⚠ 做法：`adb shell am start ... --es hel run --es 参 <base64>`
                        //:     不传 `参` 时保留原来的 `print(1+2)`（回归判据不变）。
                        string 源码 = "print(1+2)";
                        //: ⚠ `src` 这个 **ASCII 参数名**才是给 adb 用的 —— 
                        //:   实测 `--es 参 <汉字名>` 在 `adb shell` 里会**丢**
                        //:   （跑出来是 `跑（10 字符）`，也就是原来的 print(1+2)）。
                        //:   汉字参数名在 adb 的 shell 转义里不可靠。
                        string 传 = !string.IsNullOrWhiteSpace(src) ? src! : 参;
                        if (!string.IsNullOrWhiteSpace(传))
                        {
                            try
                            {
                                源码 = System.Text.Encoding.UTF8.GetString(
                                    Convert.FromBase64String(传.Trim()));
                            }
                            catch (Exception e)
                            {
                                return "参 不是合法 base64：" + e.GetType().Name + ": " + e.Message;
                            }
                        }
                        var (好, 出, 错) = PythonHost.跑并抓(源码, 家);
                        return string.Format("跑（{0} 字符）：成功={1}\n【stdout】\n{2}\n【stderr】\n{3}",
                                             源码.Length, 好, 出, 错);
                    }
                    catch (Exception e) { return "抛：" + e.GetType().Name + ": " + e.Message; }

                case "self":
                    装重定向(家, 步);
                    try
                    {
                        PythonHost.初始化(素材根, 旧路径: false, 本地: true);
                        var (ok, 详情) = PythonHost.自检();
                        return "自检：" + (ok ? "过 " : "没过 ") + 详情;
                    }
                    catch (Exception e) { return "抛：" + e.GetType().Name + ": " + e.Message; }

                case "路径":
                    装重定向(家, 步);
                    try
                    {
                        PythonHost.初始化(素材根, 旧路径: false, 本地: true);
                        var (_, 出, 错) = PythonHost.跑并抓(
                            "import sys, os\n"
                            + "print('executable', sys.executable)\n"
                            + "print('prefix', sys.prefix)\n"
                            + "print('HOME', os.environ.get('HOME'))\n"
                            + "print('PYTHONPATH', os.environ.get('PYTHONPATH'))\n"
                            + "print('ROOT', os.environ.get('HYBRIDENGINE_LIGHT_ROOT'))\n"
                            + "for p in sys.path: print('path', p)\n"
                            + "import math, json, re, collections\n"
                            + "print('扩展模块 OK', math.gcd(12, 18), json.dumps([1, 2]))\n", 家);
                        return "【stdout】\n" + 出 + "\n【stderr】\n" + 错;
                    }
                    catch (Exception e) { return "抛：" + e.GetType().Name + ": " + e.Message; }

                //: ⚠⚠ **判分实测**：错答案要 ✗、对答案要 ✓。
                //   用的是引擎**自己的**检查（`code.check` → `examrun._grade_code`
                //   跑隐藏用例），不是这里另写一套判定。
                //
                //: ⚠⚠ `桥判` 走的是**界面那条路**：`JsBridge.Pan`（网页点
                //   「判分」时调的**同一个方法**）。发布包关掉了 WebView 调试 ⇒
                //   点不了按钮，所以用它来证明"桥这个方法在发布包里也是通的"。
                case "桥判":
                    装重定向(家, 步);
                    try
                    {
                        if (活动 == null) return "（没有 Activity，跑不了桥实验）";
                        PythonHost.启动(素材根);
                        var 桥 = new JsBridge(活动, 素材根);
                        string 坏 = "print(\"工具链就绪\")\n";
                        string 好 = "print(\"工具链就绪\")\n"
                                  + "print(\"我准备好学 Python 了\")\n"
                                  + "print(\"第一步：写，第二步：跑\")\n";
                        string b1 = 桥.Pan("00", 坏);
                        string b2 = 桥.Pan("00", 好);
                        Log.Info(标签, "桥判 错答案：" + b1);
                        Log.Info(标签, "桥判 对答案：" + b2);
                        return "JsBridge.Pan 错答案 → " + b1 + "\nJsBridge.Pan 对答案 → " + b2;
                    }
                    catch (Exception e) { return "桥判抛：" + e.GetType().Name + ": " + e.Message; }

                 //: ⚠⚠ **看门狗在真机上还没生效**（19:44 与 19:47 两次实验都卡住）。
                 //:
                 //   ⇒ 这一项是**判别性**实验，专门回答"为什么"：
                 //     它跑一个**有界**循环（不会卡住），只问
                 //     `sys.settrace` 的 tracer 到底触发了没有。
                 //
                 //     三种可能，这一项能把它们分开：
                 //       ① `settrace` 在这个构建里根本不生效 ⇒ 触发次数 = 0
                 //       ② 触发次数 > 0 但很小 ⇒ 生效了，只是 `while True: pass`
                 //          那种紧循环看不到行事件
                 //       ③ 触发次数很大 ⇒ 生效正常 ⇒ 那卡住的原因在别处
                 case "jit":
                     装重定向(家, 步);
                     try
                     {
                         PythonHost.初始化(素材根, 旧路径: false, 本地: true);
                         //: ⚠ 这段**必须有界**：settrace 万一不生效，它也得返回。
                         //:   循环次数写死，不看时间。
                         string 探 = string.Join("\n", new[]
                         {
                             "import sys",
                             "痕迹 = [0]",
                             "def t(frame, event, arg):",
                             "    痕迹[0] += 1",
                             "    return t",
                             "sys.settrace(t)",
                             "for i in range(200000):",
                             "    pass",
                             "sys.settrace(None)",
                             "print('tracer 触发次数 =', 痕迹[0])",
                             "print('阈值 =', sys.gettrace() is None)",
                         });
                         string r = PythonHost.调协议(
                             "{\"id\":1,\"cmd\":\"code.run\",\"args\":{\"timeout\":20,"
                             + "\"source\":\"" + 转义(探) + "\"}}", 家);
                         Log.Info(标签, "jit 实验：" + r);
                         return r;
                     }
                     catch (Exception e) { return "jit 实验抛：" + e.GetType().Name + ": " + e.Message; }

                //: ⚠⚠ **`case "进度"` 我撤掉了** —— 它会让应用在**启动时原生崩溃**。
                //:
                //:   实测（真机 logcat）：
                //:       F libc: Fatal signal 6 (SIGABRT) in tid …(ridengine.light)
                //:       Process com.hybridengine.light has died: fg TOP
                //:       TombstoneParser: ParseTombstone success … tombstone_08
                //:
                //:   它做的事是：
                //:       Directory.GetFiles(家, ".学习进度.json", SearchOption.AllDirectories)
                //:   ⚠ 而 `家`（`files/`）下面有**几千个 Python 运行时文件**
                //:     （解压出来的 `pyenv/`）—— 递归全扫在 Activity 早期跑，太重。
                //:
                //:   ⇒ 教训两条：
                //:     ① **实验台跑在 Activity 早期时，别做重 IO**（递归扫目录尤其）
                //:     ② **"加了实验之后应用崩了"要第一时间怀疑实验本身** ——
                //:        我当时先怀疑了产品，白绕了几轮
                //: ⚠⚠ **`case "界面"`（用 EvaluateJavascript 问 DOM）我撤掉了 —— 它不稳。**
                //:
                //:   实测：跑它之后应用会崩（`DeadObjectException`，
                //:   原生栈仍在 `libmonodroid` 里）。⚠ 而我为它**重建+装机试了三次**，
                //:   每次 1~2 分钟 —— 而它想回答的那个问题
                //:   （进度条/副标题为什么不显示）**不值得用这么不稳的手段**。
                //:
                //:   ⇒ 换了一条**安全得多**的路：`index.html` 自带 `window.自检()`
                //:     和"标题日志"管道（`标题日志` 把 `document.title` 落 logcat）。
                //:     只要**触发一次**自检就够 —— 那是纯网页里的字符串拼接，
                //:     不跨语言边界、不碰 WebView 的线程模型。
                case "判":
                    装重定向(家, 步);
                    try
                    {
                        PythonHost.初始化(素材根, 旧路径: false, 本地: true);
                        var 报 = new StringBuilder();
                        //: ① 错答案（平方函数写成恒返回 0）
                        string 错答 = "def 平方(n):\n    return 0\n";
                        string r1 = PythonHost.调协议(
                            "{\"id\":1,\"cmd\":\"code.check\",\"args\":{\"id\":\"基础-050\","
                            + "\"answer\":\"" + 转义(错答) + "\"}}", 家);
                        报.Append("① 错答案 code.check 基础-050 → ").Append(r1).Append('\n');
                        //: ② 对答案
                        string 对答 = "def 平方(n):\n    return n * n\n";
                        string r2 = PythonHost.调协议(
                            "{\"id\":2,\"cmd\":\"code.check\",\"args\":{\"id\":\"基础-050\","
                            + "\"answer\":\"" + 转义(对答) + "\"}}", 家);
                        报.Append("② 对答案 code.check 基础-050 → ").Append(r2).Append('\n');
                        //: ③ 跑学员代码并收输出
                        string r3 = PythonHost.调协议(
                            "{\"id\":3,\"cmd\":\"code.run\",\"args\":{\"source\":\"print(1+2)\"}}", 家);
                        报.Append("③ code.run print(1+2) → ").Append(r3).Append('\n');
                        //: ④ 课程列表（证明引擎真的读到了课程，不是空目录）
                        string r4 = PythonHost.调协议(
                            "{\"id\":4,\"cmd\":\"lesson.list\",\"args\":{\"line\":\"python\"}}", 家);
                        报.Append("④ lesson.list python → ").Append(
                            r4.Length > 400 ? r4.Substring(0, 400) + "…" : r4).Append('\n');
                        //: ⑤ 题库
                        string r5 = PythonHost.调协议(
                            "{\"id\":5,\"cmd\":\"exam.list\",\"args\":{}}", 家);
                        报.Append("⑤ exam.list → ").Append(
                            r5.Length > 300 ? r5.Substring(0, 300) + "…" : r5).Append('\n');
                        Log.Info(标签, "判分结果：\n" + 报);
                        return 报.ToString();
                    }
                    catch (Exception e) { return "抛：" + e.GetType().Name + ": " + e.Message; }
            }
            //: ⚠⚠ **这张表必须和上面的 `case` 对得上。**
            //:
            //:   它原来是一行**手写的**字符串，只列到 `判` 为止 ——
            //:   于是我加了 `桥判` 和 `jit` 之后，跑 `hel 超时` 得到的是
            //:   「（没有这个实验：超时）可选：… 路径 判」，
            //:   **而我从那张表读出了"这个实验不存在"的结论 —— 那是错的**
            //:   （`jit` 明明在），白烧了一轮。
            //:
            //:   ⇒ 这张表**分开维护就一定会过时**。做法：把可选项和 case
            //:     放成一份数据（下面那个数组），报错时由它生成。
            //:     ⚠ 新增 case 时**同时加进这个数组** —— 它会在报错信息里出现。
            string[] 可选 = {
                "dump", "maps", "sym", "who", "threads", "env",
                "bare", "global", "dlglobal", "dllocal", "old", "fix", "fix2",
                "run", "self", "路径", "桥判", "jit", "判",
            };
            return "（没有这个实验：" + 步 + "）可选：" + string.Join(" ", 可选);
        }

        /// <summary>把字符串转义成 JSON 字面量的内容（不引外层引号）。</summary>
        static string 转义(string s)
        {
            var b = new StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': b.Append("\\\""); break;
                    case '\\': b.Append("\\\\"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\t': b.Append("\\t"); break;
                    default: b.Append(c); break;
                }
            }
            return b.ToString();
        }
    }
}
