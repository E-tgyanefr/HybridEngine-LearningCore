"""跨平台自检 —— **在任何平台上都能跑**，退出码 0/1 明确。

════════════════════════════════════════════════════════════════════
  ⚠⚠ 这个脚本**证明不了**"在别的平台上能跑"
════════════════════════════════════════════════════════════════════

它检查的是**当前这台机器**。它能回答的问题是：

    「在这个平台上，这个项目**现在**是好的吗？」

它**不能**回答「Linux 上好不好」——那必须在 Linux 上跑一次。
（本机没有 WSL / Linux / macOS 发行版 ⇒ Linux、macOS 的构建与运行
  **一次都没验过**，见 `docs/跨平台.md` 的「实测」列。）

所以本脚本的两个用途：

  ① **CI / 本地门禁**：每个平台各跑一次，谁红谁的问题当场暴露
  ② **把"跨平台"变成可判据的**：静态扫描**硬编码 Windows 假设**，
     有一个**没登记在案的**新命中就红 —— 这样"平台假设"不会
     再悄悄溜进来（原来是"文档里说支持"）

════════════════════════════════════════════════════════════════════
  用法
════════════════════════════════════════════════════════════════════

    python _build/_check_platform.py             # 全部检查
    python _build/_check_platform.py -v          # 另外打印每条扫描命中
    python _build/_check_platform.py --无界面     # 服务器：允许没有 Tk / 显示

  退出码：0 = 本平台的检查项全过 · 1 = 有失败项

════════════════════════════════════════════════════════════════════
  ⚠ 判定口径（有意这样定的，不是漏了）
════════════════════════════════════════════════════════════════════

  · **没跑到 ≠ 通过**。所以"工具链不存在""bash 不存在"这类会**明说
    SKIP**，而不是静默当成绿（`_verify_c_neg.py` 那条教训）。
  · 只有**本平台能判**的东西才判失败。跨平台猜测一律降级为提示。
  · 静态扫描用的是**登记制**：清单里的命中算"已核·平台差异"，
    清单外的算失败 —— 这样它是**回归闸**，不是一次性报告。
"""
from __future__ import annotations

import io
import os
import re
import shutil
import subprocess
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
#: 允许没有界面（纯命令行服务器）——此时 Tk 缺失不算失败
允许无界面 = "--无界面" in sys.argv
详细 = "-v" in sys.argv or "--详细" in sys.argv

失败: list[str] = []
警告: list[str] = []
跳过: list[str] = []


def 节(标题: str) -> None:
    print()
    print("─" * 66)
    print("  " + 标题)
    print("─" * 66)


def 好(msg: str) -> None:
    print("  ✓ " + msg)


def 坏(msg: str) -> None:
    print("  ✗ " + msg)
    失败.append(msg)


def 警(msg: str) -> None:
    print("  ⚠ " + msg)
    警告.append(msg)


def 跳(msg: str) -> None:
    print("  ○ " + msg + "   ← **跳过，不算通过**")
    跳过.append(msg)


# ════════════════════════════════════════════════════════════════════
#  1 平台与解释器
# ════════════════════════════════════════════════════════════════════
节("1 平台与解释器")
import platform                                          # noqa: E402

是WIN = sys.platform.startswith("win")
是MAC = sys.platform == "darwin"
是LINUX = sys.platform.startswith("linux")
平台名 = {"win32": "Windows", "darwin": "macOS", "linux": "Linux"}.get(
    sys.platform, sys.platform)
print("  平台        : %s  (sys.platform = %r, os.name = %r)" % (平台名, sys.platform, os.name))
print("  机器        : %s / %s" % (platform.machine() or "?", platform.processor() or "?"))
print("  解释器      : %s" % sys.executable)
print("  Python      : %s" % sys.version.split()[0])
print("  冻结产物    : %s" % getattr(sys, "frozen", False))
print("  文件系统大小写敏感？: %s" % ("否（Windows/macOS 默认不敏感）" if (是WIN or 是MAC)
                                     else "是（Linux 通常敏感）"))
print("  路径分隔符  : %r   编码: %s" % (os.sep, sys.getfilesystemencoding()))

# ════════════════════════════════════════════════════════════════════
#  2 Python 版本
# ════════════════════════════════════════════════════════════════════
节("2 Python 版本（代码要求的**最低**版本）")
#: 来源：`docs/Linux版说明.md` 的一键脚本要 3.9+；代码用 `list[str]` 之类
#: 的注解（`from __future__ import annotations` 兜住）与 `str.removeprefix`。
要求 = (3, 9)
if sys.version_info[:2] >= 要求:
    好("Python %s >= 要求的 %d.%d" % (sys.version.split()[0], *要求))
else:
    坏("Python %s **低于**要求的 %d.%d —— 这里会一路报语法/属性错"
       % (sys.version.split()[0], *要求))

# ════════════════════════════════════════════════════════════════════
#  3 包与全部子模块能否导入
# ════════════════════════════════════════════════════════════════════
节("3 `hybridengine_light` 及其全部子模块能否导入")
if LC not in sys.path:
    sys.path.insert(0, LC)


def _子模块() -> list[str]:
    """枚举包内所有模块（含子包），用**文件**枚举而不是手写清单。"""
    包 = os.path.join(LC, "hybridengine_light")
    出 = ["hybridengine_light"]
    for 根, 目录, 文件 in os.walk(包):
        目录[:] = [d for d in 目录 if d != "__pycache__"]
        for f in sorted(文件):
            if not f.endswith(".py"):
                continue
            相对 = os.path.relpath(os.path.join(根, f), LC)
            点 = 相对[:-3].replace(os.sep, ".").replace("/", ".")
            if 点.endswith(".__init__"):
                点 = 点[: -len(".__init__")]
            elif 点.endswith(".__main__"):
                continue
            出.append(点)
    return sorted(set(出))


模块 = _子模块()
import importlib                                          # noqa: E402

导入失败 = []
for m in 模块:
    try:
        importlib.import_module(m)
    except Exception as e:                               # noqa: BLE001
        导入失败.append((m, "%s: %s" % (type(e).__name__, e)))
if 导入失败:
    for m, e in 导入失败:
        坏("import %s -> %s" % (m, e))
else:
    好("全部 %d 个模块导入成功" % len(模块))
    if 详细:
        print("      " + " · ".join(模块))

# ════════════════════════════════════════════════════════════════════
#  4 tkinter（界面要它）
# ════════════════════════════════════════════════════════════════════
节("4 tkinter（图形界面的前置条件）")
try:
    import tkinter                                    # noqa: F401
    import tkinter.font as _tkfont                    # noqa: F401
    好("tkinter 可导入（Tk %s）" % tkinter.TkVersion)
    有显示 = True
    提示 = ""
    try:
        _r = tkinter.Tk()
        _r.withdraw()
        _r.destroy()
    except Exception as e:                            # noqa: BLE001
        有显示 = False
        提示 = "%s: %s" % (type(e).__name__, e)
    if 有显示:
        好("能建 Tk 窗口（有可用显示）")
    elif 允许无界面:
        跳("没有可用显示（%s）—— 已加 --无界面，只影响 GUI" % 提示)
    else:
        警("没有可用显示（%s）—— 命令行仍可用；纯服务器加 --无界面" % 提示)
except Exception as e:                                # noqa: BLE001
    if 允许无界面:
        跳("tkinter 不可用（%s）—— 已加 --无界面" % e)
    else:
        坏("tkinter 不可用（%s）—— 装法：Debian/Ubuntu `sudo apt install python3-tk`、"
           "Fedora `sudo dnf install python3-tkinter`" % e)

# ════════════════════════════════════════════════════════════════════
#  5 内容根与课数（打包后最容易错位的地方）
# ════════════════════════════════════════════════════════════════════
节("5 内容根（`_paths.内容根()`）与课数")
try:
    from hybridengine_light import _paths
    根 = _paths.内容根()
    print("  内容根      : %s" % 根)
    print("  冻结        : %s" % _paths.冻结())
    print("  提示里的命令 : %s" % _paths.命令名())
    if not 根.is_dir():
        坏("内容根不存在：%s" % 根)
    else:
        缺 = [d for d in ("lessons", "docs") if not (根 / d).is_dir()]
        if 缺:
            坏("内容根下缺目录：%s" % "、".join(缺))
        else:
            好("内容根有效（阶段内容目录齐全）")
            print("      %s" % " · ".join(
                "%s=%d" % (n, len([f for f in os.listdir(根 / d)
                                   if f.endswith(ext) and re.match(r"^第?\d\d", f)]))
                for n, d, ext in (("Python", "lessons", ".py"),)))
except Exception as e:                                # noqa: BLE001
    坏("内容根检查失败：%s: %s" % (type(e).__name__, e))

# ════════════════════════════════════════════════════════════════════
#  6 编码（跨平台最容易"静默坏"的一类）
# ════════════════════════════════════════════════════════════════════
节("6 文件编码（源码必须 UTF-8）")
源码后缀 = {".py", ".sh", ".md", ".cs", ".java", ".c", ".cpp", ".h", ".hpp",
            ".json", ".js", ".html", ".css", ".csproj", ".spec", ".cmd", ".ps1",
            ".yml", ".yaml", ".xml"}
留档后缀 = {".txt"}
跳过目录 = {"__pycache__", "_build/dist", "_build/work", "_build/build",
            "_build/_android_cache", "obj", "bin"}
非utf8源码, 非utf8留档, 扫描数 = [], [], 0
for 根2, 目录, 文件 in os.walk(LC):
    目录[:] = [d for d in 目录 if d not in 跳过目录 and not d.startswith(".git")]
    相对根 = os.path.relpath(根2, LC).replace("\\", "/")
    if any(相对根.startswith(s) for s in 跳过目录):
        continue
    for f in 文件:
        后缀 = os.path.splitext(f)[1].lower()
        if f == ".gitignore":
            后缀 = ".json"        # 当成"必须 UTF-8 的文本"来查
        if 后缀 not in 源码后缀 and 后缀 not in 留档后缀:
            continue
        p = os.path.join(根2, f)
        try:
            if os.path.getsize(p) > 4 * 1024 * 1024:
                continue
            b = open(p, "rb").read()
        except OSError:
            continue
        扫描数 += 1
        相对 = os.path.join(相对根, f).replace("\\", "/")
        try:
            b.decode("utf-8")
        except UnicodeDecodeError as e:
            (非utf8源码 if 后缀 in 源码后缀 else 非utf8留档).append((相对, str(e)))
            if 后缀 in 源码后缀:
                坏("不是合法 UTF-8：%s（%s）" % (相对, e))
if not 非utf8源码:
    好("扫了 %d 个文本文件：**源码全是合法 UTF-8**" % 扫描数)
for 相对, e in 非utf8留档:
    警("命令输出留档不是 UTF-8（不是源码，仅提示）：%s —— %s" % (相对, e))
if 非utf8留档:
    print("      成因：PowerShell 5.1 的 `>` 重定向写的是 UTF-16LE（见环境坑第 2 条）。")

# ════════════════════════════════════════════════════════════════════
#  7 大小写冲突（Linux 上会**直接覆盖**）
# ════════════════════════════════════════════════════════════════════
节("7 只有大小写不同的路径（在 Linux/macOS 上会撞）")
撞 = []
for 根2, 目录, 文件 in os.walk(LC):
    目录[:] = [d for d in 目录 if d not in 跳过目录 and not d.startswith(".git")]
    相对根 = os.path.relpath(根2, LC).replace("\\", "/")
    if any(相对根.startswith(s) for s in 跳过目录):
        continue
    小写表: dict[str, list[str]] = {}
    for f in 文件:
        小写表.setdefault(f.lower(), []).append(f)
    for low, 名字 in 小写表.items():
        if len(名字) > 1:
            撞.append("%s : %s" % (相对根, sorted(名字)))
if 撞:
    for c in 撞:
        坏("大小写冲突 %s" % c)
else:
    好("没有只有大小写不同的路径")

# ════════════════════════════════════════════════════════════════════
#  8 平台相关代码静态扫描（**登记制 = 回归闸**）
# ════════════════════════════════════════════════════════════════════
节("8 硬编码 Windows 假设扫描（清单外的命中 = 失败）")
扫描规则 = [
    ("windll",       r"\bwindll\b"),
    ("msvcrt",       r"\bmsvcrt\b"),
    ("os.startfile", r"\bos\.startfile\b"),
    ("tasklist",     r"\btasklist\b|\btaskkill\b"),
    ("chcp",         r"\bchcp\b"),
    ("shell=True",   r"shell\s*=\s*True"),
    ("盘符路径",      r"['\"][A-Za-z]:[\\/]"),
    (".exe 字面量",   r"['\"][^'\"]*\.exe['\"]"),
    (".dll 字面量",   r"['\"][^'\"]*\.dll['\"]"),
    ("os.name==nt",  r"os\.name\s*==\s*['\"]nt['\"]"),
    #: ⚠ 只匹配 `environ["TEMP"]` 这种**下标**形式 ——
    #:   `environ.get("TEMP", "/tmp")` 是有兜底的安全写法，不该报。
    ("Windows 专有环境变量",
     r"environ\[['\"](?:TEMP|TMP|APPDATA|LOCALAPPDATA|USERPROFILE|COMSPEC|PATHEXT|windir|SystemRoot)['\"]\]"),
]
#: 登记表 —— 每一条都**核过**、都能说出为什么留着。键 = (相对路径, 规则名)。
已核: dict[tuple[str, str], str] = {
    ("_build/_cdp.py", ".exe 字面量"):
        "安卓 `adb.exe` —— 开发期调试工具，不参与产品运行路径",
    ("_build/_fix00j.py", "盘符路径"):
        "历史一次性修补脚本，里面的旧路径是**它要改的目标**，不再运行",
    ("_build/_fix_deadpaths.py", "盘符路径"):
        "同上：它的数据就是**待替换的旧硬编码路径**，写死是对的",
    ("_build/_shot.py", "windll"):
        "Windows 专用截图/取窗口工具（开发用）。非产品路径，Linux/macOS 用不了**也不影响**",
    ("_build/_shot.py", "Windows 专有环境变量"):
        "同上：截图工具本来就是 Windows 专用（`%TEMP%` 在这里不是缺陷）",
    ("_build/_verify_c.py", ".dll 字面量"):
        "`PathGen.dll` 是 .NET 生成物的名字 —— **各平台都叫 .dll**，不是 Windows 假设",
    ("_build/_verify_c.py", "盘符路径"):
        "C# 用例字符串里的 `F:\\_calc_\\` —— 是**喂给被测代码的测试数据**",
    ("_build/_verify_compiled.py", "盘符路径"):
        "`C:\\lc_tmpx` 是绕开「MinGW 写不进中文路径」的临时目录（Windows 验证脚本专用）",
    ("_build/_verify_cpp_negative.py", "盘符路径"):
        "同上：MinGW 中文路径 workaround，仅 Windows 验证脚本用",
    ("_build/_verify_stub_templates.py", ".exe 字面量"):
        "**故意的测试夹具** —— 它模拟 `sys.frozen` + 一个 Windows exe 路径，验 `命令名()`",
    ("_build/_verify_stub_templates.py", "盘符路径"):
        "同上：测试夹具里的模拟 exe 路径",
    ("_build/build_android.py", "盘符路径"):
        "安卓构建：AAPT2 不认中文路径 ⇒ 必须拷到 `C:\\lc_android`；另有写死的 JDK 路径。"
        "⚠ 该文件**不在本任务写入范围**（另有 owner），此处仅登记",
    ("_build/build_exe.py", ".exe 字面量"):
        "**本次新加的平台分支**：`可执行名 = APP + '.exe' if 是WIN else APP`",
    ("_build/build_exe.py", ".dll 字面量"):
        "**本次新加的平台分支**：运行库名按平台给（win=dll / mac=dylib / linux=so）",
    ("_build/build_exe.py", "tasklist"):
        "**本次新加的平台分支**：win 用 tasklist/taskkill，POSIX 用 pgrep",
    ("hybridengine_light/editor.py", "windll"):
        "**正确的跨平台写法**：`if os.name != 'nt': return True` + try/except 兜住",
    ("hybridengine_light/editor.py", "os.name==nt"):
        "**正确的跨平台写法**：nt 走 msvcrt、类 Unix 走 termios（两条路都实现了）",
    ("hybridengine_light/editor.py", "msvcrt"):
        "同上：Windows 分支。文档字符串里的提及也计在内",
    ("hybridengine_light/learn_gui.py", "os.startfile"):
        "**已经是按平台分流的**（win=startfile / mac=open / linux=xdg-open→gio）",
    ("hybridengine_light/learnmodel.py", ".exe 字面量"):
        "工具链**候选名列表**（`dotnet` 与 `dotnet.exe` 都试）—— 跨平台要的正是它",
    ("tests/test_gui_model.py", ".exe 字面量"):
        "**测试夹具**：monkey-patch `_paths.命令名()` 成 `HybridEngine-light.exe`，"
        "专门验「冻结后界面显示 exe 名」这条 —— 是**被测的输入**，不是平台假设。"
        "⚠ 该文件不在本任务写入范围（另有 owner），此处仅登记",
    # ── 下面三条：反面验证那两条线的**新增脚本**（task-6）──────────────
    #  ⚠ 命中来自两处：① 代码里真的调 `taskkill /F /T`；
    #                ② 注释里在解释**为什么必须这么杀**。
    ("_build/_probe_c_neg_explore.py", "tasklist"):
        "**C 线错答案探索器**（开发工具，只在本机跑）。`taskkill /F /T` 是**必须的**："
        "`subprocess.run(timeout=)` 只杀得掉 `dotnet run`，杀不掉它拉起的孙进程"
        "（`%TEMP%\\hybridengine_light_c\\prog.exe`）—— 孙进程攥着 stdout 管道不放，"
        "`communicate()` 连 timeout 都等不到尽头（实测卡 11 分钟）。不带进产品。",
    ("_build/_probe_cs_neg_explore.py", "tasklist"):
        "同上：**C# 线**探索器，同一套杀进程树的写法与注释。不带进产品。",
    ("_build/_verify_c_neg2.py", "tasklist"):
        "**C 线反面验证脚本**（补账本里缺的 24 课）。同样用 `Popen` + `taskkill /F /T`"
        "兜住「学员错答案死循环」。它是判据脚本，**不参与产品运行路径**；"
        "跑它本来就要 Windows 上的 dotnet + gcc（课程工具链）。",
    ("_build/_verify_cs_neg2.py", "tasklist"):
        "**C# 线反面验证脚本**（补账本里缺的 33 课）—— 和 `_verify_c_neg2.py` 同一形态、"
        "同一段杀进程树的兜底。判据脚本，不参与产品运行路径。",
}

源码文件 = []
for 子 in ("hybridengine_light", "_build", "tests"):
    for 根2, 目录, 文件 in os.walk(os.path.join(LC, 子)):
        目录[:] = [d for d in 目录 if d != "__pycache__"]
        for f in 文件:
            if f.endswith(".py"):
                p = os.path.join(根2, f)
                # ⚠ 跳过本文件自己 —— 上面那张 `扫描规则` 表和 `已核` 的
                #   **理由文字**里就写着这些模式，扫自己必然满屏假阳性。
                相对自 = os.path.relpath(p, LC).replace("\\", "/")
                if 相对自 == "_build/_check_platform.py":
                    continue
                源码文件.append(p)

命中: dict[tuple[str, str], list[tuple[int, str]]] = {}
for p in 源码文件:
    相对 = os.path.relpath(p, LC).replace("\\", "/")
    try:
        文本 = io.open(p, encoding="utf-8").read()
    except UnicodeDecodeError:
        continue
    for i, 行 in enumerate(文本.split("\n"), 1):
        s = 行.strip()
        if not s or s.startswith("#"):
            continue        # 整行注释不算（内联注释与文档字符串仍会计入 = 有意保守）
        for 名, pat in 扫描规则:
            if re.search(pat, s):
                命中.setdefault((相对, 名), []).append((i, s[:110]))

未核 = {k: v for k, v in 命中.items() if k not in 已核}
print("  扫了 %d 个 .py；命中 %d 处，分布在 %d 个（文件,规则）组合"
      % (len(源码文件), sum(len(v) for v in 命中.values()), len(命中)))
if 详细:
    for (相对, 名), 位 in sorted(命中.items()):
        mark = "已核" if (相对, 名) in 已核 else "**未核**"
        print("    [%s] %s  %s" % (mark, 相对, 名))
        for i, s in 位[:2]:
            print("           L%-5d %s" % (i, s))
if 未核:
    for (相对, 名), 位 in sorted(未核.items()):
        坏("未登记的平台相关命中：%s 的「%s」×%d（首处 L%d：%s）"
           % (相对, 名, len(位), 位[0][0], 位[0][1]))
    print("      ⇒ 要么修掉，要么**核过之后登记进 `已核` 并写清理由** ——")
    print("        登记制就是为了让'新的平台假设'不会悄悄溜进来。")
else:
    好("全部 %d 个命中都已在登记表里（每条都有理由）" % len(命中))

# ════════════════════════════════════════════════════════════════════
#  9 工具链探测（四条编译型课程用）
# ════════════════════════════════════════════════════════════════════
节("9 工具链探测（编译型课程用；缺了只是那几条线用不了）")
工具 = [("dotnet", "C# / C / C++ / Java 课程线的**驱动器**"),
        ("g++", "C++ 课程"), ("gcc", "C 课程"),
        ("clang++", "C++ 课程（备选）"), ("clang", "C 课程（备选）"),
        ("javac", "Java 课程（要 **JDK**，JRE 里没有 javac）"),
        ("java", "Java 课程（运行）")]
在的, 不在的 = [], []
for 名, 用处 in 工具:
    路 = shutil.which(名)
    if 路:
        在的.append(名)
        print("  ✓ %-9s %s" % (名, 路))
    else:
        不在的.append(名)
        print("  ○ %-9s 没有 —— %s" % (名, 用处))
if 不在的:
    跳("缺 %d 个工具链：%s（安装说明见 docs/Linux版说明.md 第四节）"
       % (len(不在的), "、".join(不在的)))
else:
    好("全部工具链都在")

# ════════════════════════════════════════════════════════════════════
#  10 shell 脚本：语法 + 换行符
# ════════════════════════════════════════════════════════════════════
节("10 shell 脚本（Linux/macOS 的构建入口）")
脚本 = []
for 根2, 目录, 文件 in os.walk(os.path.join(LC, "_build")):
    目录[:] = [d for d in 目录 if d != "__pycache__"]
    for f in 文件:
        if f.endswith(".sh"):
            脚本.append(os.path.join(根2, f))
脚本.sort()

# 文本脚本的换行符：CRLF 会让 Linux 上的 shebang 变成 `#!/bin/sh\r` ⇒ 跑不起来
for p in 脚本:
    b = open(p, "rb").read()
    名 = os.path.relpath(p, LC).replace("\\", "/")
    if b.startswith(b"\xef\xbb\xbf"):
        坏("%s 有 UTF-8 BOM —— Linux 上 `#!/bin/sh` 前面多 BOM 会报 bad interpreter" % 名)
    elif b"\r\n" in b:
        坏("%s 是 CRLF 换行 —— Linux 上会报 `bad interpreter: /bin/sh^M`" % 名)
    else:
        好("%s 换行符 LF、无 BOM" % 名)


def _找bash() -> str | None:
    """找一个**真能跑**的 bash。

    ⚠ 本机 `shutil.which("bash")` 命中的是 `C:\\Windows\\system32\\bash.exe`
      —— 那是 **WSL 的启动器**，没装发行版时只会打印一句"未安装"。
      ⇒ 必须先试已知的 Git Bash，再退回 which，并且**真的跑一下 `--version`**。
    """
    for c in (r"C:\Program Files\Git\bin\bash.exe",
              r"C:\Program Files\Git\usr\bin\bash.exe",
              "/bin/bash", "/usr/bin/bash", "/bin/sh"):
        if os.path.exists(c):
            return c
    w = shutil.which("bash")
    if not w:
        return None
    try:
        r = subprocess.run([w, "--version"], capture_output=True, timeout=20)
        出 = (r.stdout + r.stderr).decode("utf-8", "replace")
        return w if "bash" in 出.lower() or "GNU" in 出 else None
    except Exception:                                    # noqa: BLE001
        return None


BASH = _找bash()
if not 脚本:
    跳("没找到 .sh 脚本")
elif BASH is None:
    跳("本机找不到可用的 bash —— `bash -n` 语法自检**没跑**")
else:
    print("  用 %s 做 `bash -n` 语法自检" % BASH)
    for p in 脚本:
        名 = os.path.relpath(p, LC).replace("\\", "/")
        try:
            r = subprocess.run([BASH, "-n", p], capture_output=True, timeout=60)
        except Exception as e:                           # noqa: BLE001
            坏("%s 语法自检跑不起来：%s" % (名, e))
            continue
        if r.returncode == 0:
            好("%s 语法通过（bash -n）" % 名)
        else:
            坏("%s 语法错误：%s" % (名, (r.stdout + r.stderr).decode("utf-8", "replace").strip()[:400]))
    print("  ⚠ 这只证明**语法**对；脚本在真实 Linux/macOS 上**从没运行过**（见 docs/跨平台.md）")

# ════════════════════════════════════════════════════════════════════
#  结论
# ════════════════════════════════════════════════════════════════════
节("结论")
print("  平台        : %s" % 平台名)
print("  通过 / 跳过 : %d 项跳过（跳过 = **没验**，不算过）" % len(跳过))
print("  警告        : %d" % len(警告))
print("  失败        : %d" % len(失败))
if 跳过:
    print()
    print("  ── 跳过（没验）的 ──")
    for s in 跳过:
        print("    ○ " + s)
if 警告:
    print()
    print("  ── 警告（不阻塞）──")
    for w in 警告:
        print("    ⚠ " + w)
print()
if 失败:
    print("  ── 失败 ──")
    for f in 失败:
        print("    ✗ " + f)
    print()
    print("  结论：**本平台自检不通过** ❌")
    sys.exit(1)

print("  结论：**本平台自检通过** ✅")
print()
print("  ⚠⚠ 这个结论**只对当前这台机器、当前这个平台**成立。")
print("      Linux / macOS 上的构建与运行**在本机一次都没跑过**")
print("      （没有 WSL、没有 Linux、没有 macOS）—— 见 docs/跨平台.md 的「实测」列。")
sys.exit(0)
