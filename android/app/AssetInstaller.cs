using System;
using System.IO;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Util;

namespace HybridEngineLight.Android
{
    /// <summary>
    /// 把 APK 里的 assets 解到**应用内部存储**，只在首次启动做一次。
    ///
    /// ⚠⚠ 为什么必须解出来，不能直接从 assets 用：
    ///   ① assets 在 APK 里是**压缩**的，CPython 的 `import` 要 seek，
    ///      压缩流上做不到（除非把整个 APK 解压进内存 —— 那更糟）
    ///   ② CPython 要往 `__pycache__` **写**字节码，assets 只读
    ///   ③ 课程要生成答题文件（虽然答题文件单独走 `$HOME`，见下）
    ///
    /// ⚠⚠ 为什么先解到临时目录再改名（而不是边解边用）：
    ///   App 可能在解压到一半时被系统杀掉（切后台、内存紧张）。
    ///   那样会留下**半份** Python —— 症状是"有时候能跑，有时候 import 报错"，
    ///   极难查。改名（rename）是原子的：要么是完整的旧版，要么是完整的新版。
    ///   同目录内改名不涉及跨文件系统拷贝，代价可忽略。
    ///
    /// ⚠ 版本标记用 `版本号 + 素材指纹`：升级 APK 后素材会变，标记对不上就重解。
    ///   只比版本号不够 —— 开发期间同一个版本号会反复换素材（实测很容易忘）。
    /// </summary>
    static class AssetInstaller
    {
        const string 标签 = "HybridEngineLight";
        const string 标记名 = ".assets-ok";

        /// <summary>素材包在 assets 里的名字。</summary>
        const string 素材包 = "python.zip";

        /// <summary>Python 产物本身的代号（ChaquoPy 的 py3.12.7 第 1 版）。</summary>
        const string 素材代号 = "py312.7-chaq-1";

        /// <summary>
        /// 算出**这次 APK 里的素材指纹** —— 自动的，不用手改。
        ///
        /// ⚠⚠ 为什么必须自动：原来的实现是**只比一个手写的常量**
        ///   （`素材版本 = "py312.7-chaq-1"`）：
        ///
        ///       if (已有 == 素材版本) { 跳过解压; }
        ///
        ///   于是**只要那个常量没改，素材内容变了也不会重解** ——
        ///   开发期间我改了界面、装了新 APK，真机上还是**旧界面**，
        ///   而我盯着源码和生成物反复确认"明明改了"，
        ///   查了源码、生成物、构建日志，就是没想到设备上那份压根没换。
        ///   ⇒ 这一条其实早就记在 `docs/任务清单.md` 的待办里
        ///     （"`_assets-ok` 标记只比版本号，内容变了不会重解"），
        ///     直到它真的咬了我一次才修。
        ///
        /// 指纹取什么：
        ///   · `python.zip` 的**字节数** —— 素材一变基本就会变（够用且零成本）
        ///   · APK 的 **versionCode** —— 重建一次就变（兜住"长度恰好相同"）
        ///
        /// ⚠ 为什么不用文件哈希：算 6 MB 的 SHA 要几百毫秒，
        ///   而这段代码跑在**冷启动路径**上。字节数 + versionCode
        ///   在"开发/升级时一定会变"这个目标上已经足够。
        /// </summary>
        static string 算指纹(Context 上下文)
        {
            long 长度 = -1;
            try
            {
                using var fd = 上下文.Assets!.OpenFd(素材包);
                长度 = fd?.Length ?? -1;
            }
            catch (Exception e)
            {
                //: 打不开就退回"每次重解"（宁可慢，也不能拿旧素材糊弄）
                Log.Warn(标签, "读不到素材长度（" + e.GetType().Name + "），保守重解");
            }

            int 版本号 = 0;
            try
            {
                var 包 = 上下文.PackageManager?.GetPackageInfo(上下文.PackageName!, 0);
                //: ⚠ API 28+ 用 LongVersionCode，老的是 VersionCode
                版本号 = (int)(包?.LongVersionCode ?? 0);
            }
            catch (Exception e) { Log.Warn(标签, "读不到 versionCode：" + e.GetType().Name); }

            return 素材代号 + "+" + 长度 + "+v" + 版本号;
        }

        public static string 安装(Context 上下文)
        {
            string 家 = 上下文.FilesDir!.AbsolutePath!;
            string 正式 = Path.Combine(家, "pyenv");
            string 标记 = Path.Combine(正式, 标记名);
            string 素材版本 = 算指纹(上下文);

            // ── 已经装好且指纹一致 ⇒ 直接返回（这是常态，零开销）──
            if (File.Exists(标记))
            {
                string 已有 = File.ReadAllText(标记).Trim();
                if (已有 == 素材版本)
                {
                    Log.Info(标签, "素材已就绪（" + 素材版本 + "），跳过解压");
                    return 正式;
                }
                Log.Info(标签, "素材指纹变了（" + 已有 + " → " + 素材版本 + "），重解");
            }

            // ── 解到临时目录 ──
            string 临时 = Path.Combine(家, "pyenv.tmp");
            if (Directory.Exists(临时))
            {
                try { Directory.Delete(临时, true); } catch { }
            }
            Directory.CreateDirectory(临时);

            var 秒表 = global::Android.OS.SystemClock.ElapsedRealtime();
            int 个 = 解zip(上下文, Path.Combine(临时, "python-stdlib"),
                          Path.Combine(临时, "lib-dynload"),
                          Path.Combine(临时, "python-app"),
                          Path.Combine(临时, "web"));
            long 毫秒 = global::Android.OS.SystemClock.ElapsedRealtime() - 秒表;
            Log.Info(标签, "解开 " + 个 + " 个文件，用时 " + 毫秒 + " ms");

            File.WriteAllText(Path.Combine(临时, 标记名), 素材版本);

            // ── 原子替换 ──
            //
            // ⚠ 先把旧的挪走再挪新的，最后删旧的 —— **不能先删旧的**：
            //   删了之后如果改名失败（磁盘满/权限），就两份都没了。
            string 旧 = Path.Combine(家, "pyenv.old");
            if (Directory.Exists(旧))
            {
                try { Directory.Delete(旧, true); } catch { }
            }
            if (Directory.Exists(正式))
            {
                try { Directory.Move(正式, 旧); }
                catch (Exception e) { Log.Warn(标签, "挪走旧素材失败：" + e.Message); }
            }
            Directory.Move(临时, 正式);
            if (Directory.Exists(旧))
            {
                try { Directory.Delete(旧, true); }
                catch (Exception e) { Log.Warn(标签, "删旧素材失败（不影响使用）：" + e.Message); }
            }
            return 正式;
        }

        /// <summary>
        /// 从 assets 里的 **一个 zip** 解出全部 Python 侧内容。
        ///
        /// ⚠⚠ 为什么是 zip 而不是逐个 asset（实测撞出来的硬约束）：
        ///   **AAPT2 处理不了非 ASCII 文件名**。课程文件名是中文的
        ///   （`01_线性回归.py` 等，667 个里 79 个含中文），而那些名字不能改
        ///   —— 那是学员在界面上看到的课名。逐个资产会直接构建失败：
        ///       error APT2098: failed to open file. .../01_线性回归.py
        ///
        ///   打进 zip 之后 APK 里只有一个 ASCII 名字（`python.zip`），
        ///   文件名的问题就不存在了。附带好处：解压比逐文件读 asset 快得多
        ///   （一次开流 + 顺序读，而不是 667 次 Open）。
        ///
        /// ⚠ 用 .NET 自带的 `ZipArchive` —— 不引第三方解压库。
        /// </summary>
        static int 解zip(Context 上下文, params string[] 目标们)
        {
            using var 流 = 上下文.Assets!.Open("python.zip", Access.Random);
            if (流 == null) throw new IOException("APK 里没有 assets/python.zip");
            using var 包 = new System.IO.Compression.ZipArchive(
                流, System.IO.Compression.ZipArchiveMode.Read);
            //: 目标目录由条目自身的顶层段决定；这里先把四个顶层目录建好
            string 基 = Path.GetDirectoryName(目标们[0])!;
            int n = 0;
            var 缓冲 = new byte[128 * 1024];
            foreach (var 条 in 包.Entries)
            {
                if (条.FullName.EndsWith("/", StringComparison.Ordinal)) continue;
                // ⚠ zip 里的路径一律是 `/`，Windows 上要换成平台分隔符
                string 相对 = 条.FullName.Replace('/', Path.DirectorySeparatorChar);
                string 出 = Path.Combine(基, 相对);
                Directory.CreateDirectory(Path.GetDirectoryName(出)!);
                using (var 入 = 条.Open())
                using (var 写 = File.Create(出))
                {
                    int r;
                    while ((r = 入.Read(缓冲, 0, 缓冲.Length)) > 0)
                        写.Write(缓冲, 0, r);
                }
                n++;
            }
            return n;
        }

        /// <summary>把一个 asset 子目录整个解到磁盘（旧路径，现在不用了）。</summary>
        static int 解目录(Context 上下文, string 资源目录, string 目标)
        {
            string[] 们;
            try { 们 = 上下文.Assets!.List(资源目录) ?? Array.Empty<string>(); }
            catch { return 0; }                       // 目录不存在（比如没有 web/）就当空
            int n = 0;
            Directory.CreateDirectory(目标);
            foreach (string 名 in 们)
            {
                // ⚠ asset 的 `List` 对**目录**返回不带尾斜杠的名字，要靠再 List 一次判断。
                //   直接开流会在目录上抛异常 —— 用 try 兜住，比判断更省事也更稳。
                string 子资源 = 资源目录 + "/" + 名;
                string 子目标 = Path.Combine(目标, 名);
                string[]? 孙 = null;
                try { 孙 = 上下文.Assets.List(子资源); } catch { }
                if (孙 != null && 孙.Length > 0)
                {
                    n += 解目录(上下文, 子资源, 子目标);
                    continue;
                }
                try
                {
                    using var 入 = 上下文.Assets.Open(子资源);
                    if (入 == null) continue;
                    using var 出 = File.Create(子目标);
                    入.CopyTo(出, 64 * 1024);
                    n++;
                }
                catch (Exception e)
                {
                    // 空目录会走到这里 —— 不当错误
                    Log.Debug(标签, "跳过 " + 子资源 + "：" + e.Message);
                }
            }
            return n;
        }
    }
}
