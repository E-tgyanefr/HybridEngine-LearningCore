# 把 HybridEngine-light 打包成 Windows onedir exe。
#
# ## 布局（学员看到的）
#   HybridEngine-light\
#     HybridEngine-light.exe     <- 双击这个
#     怎么用.txt                 <- 给学员看的说明（含"哪些课要额外装工具"）
#     lessons\ games\ ai\ ...    <- **课程内容，就在 exe 旁边**（看得见、改得动）
#     _internal\                 <- PyInstaller 的运行时（不用管它）
#
# ## 为什么课程在 exe 旁边而不是 _internal 里
#   包内定位原本是 `Path(__file__).resolve().parent.parent`（5 处），
#   打包后会落到 `_internal/` => 学员的答题文件藏在一个黑盒里。
#   => 加了 `hybridengine_light/_paths.py`（单一真值）：
#      环境变量 `HYBRIDENGINE_LIGHT_ROOT` 覆盖内容根。
#      入口脚本设它 => 课程能在 exe 旁边。
#   ⚠ 不设该环境变量时行为与改动前**逐位相同**（184 个用例验证过）。
import io
import os
import shutil
import subprocess
import sys

# ⚠ 不硬编码路径：脚本在 LearnCode\_build\ 里 ⇒ 上一级的上一级就是仓库根。
#   实测踩到：整理后仓库从 Project\LearnCode 变成了 Project\引擎\LearnCode，
#   而这里写死了旧路径 ⇒ 一构建就 FileNotFoundError。**路径要推导，不要写死。**
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BUILD = os.path.join(ROOT, "_build")
DIST = os.path.join(BUILD, "dist")
WORK = os.path.join(BUILD, "work")
APP = "HybridEngine-light"
OUTDIR = os.path.join(DIST, APP)

# ── ⚠ 平台自适应 ────────────────────────────────────────────────────────
#   原来这个脚本处处写死 Windows：产物名 `APP + ".exe"`、zip 名固定 `-win64.zip`、
#   用法说明里全是 `HybridEngine-light.exe`。于是它在 Linux 上**根本跑不了**
#   （exe 不存在 ⇒ 自检必失败），而 Linux 恰恰是这个项目最该支持的平台之一。
#
#   ⇒ 三个变量按平台定，其余代码一律引用它们，不再各写各的。
是WIN = sys.platform.startswith("win")
是MAC = sys.platform == "darwin"
#: 可执行文件的名字：Windows 带 .exe，别的平台不带
可执行名 = APP + ".exe" if 是WIN else APP
#: 发布包的后缀：和已有的 Windows 包（`-win64.zip`）保持一致的分辨度
if 是WIN:
    平台标记 = "win64"
    包后缀 = ".zip"
elif 是MAC:
    平台标记 = "macos"
    包后缀 = ".tar.gz"
else:
    #: ⚠ 用 `platform.machine()` 而不是写死 x86_64 ——
    #  ARM 机器上打出来的包只能在自己架构上跑，名字必须说清
    import platform as _pf
    平台标记 = "linux-" + (_pf.machine() or "unknown").lower()
    包后缀 = ".tar.gz"

# ── ⚠ 成品放哪：**引擎\发布\**（不是 _build\dist\）──────────────────────
#   2026-10 整理后，打包成品统一放在 `引擎\发布\`，那是给人拿的地方；
#   而 `_build\` 只是构建中间区（可随时删）。
#
# ⚠ 实现上仍然"先建到 _build\dist 再搬过去"，原因是 PyInstaller 的
#   `--noconfirm` 会**整个删掉 --distpath 目录** —— 直接把 --distpath 指向
#   发布目录的话，它会把里面别的东西（zip、说明）一起删掉。
#   所以：在临时区建好，再整体替换。
# Project\引擎\LearnCode -> Project\引擎  (ROOT 的上一级)
发布 = os.path.join(os.path.dirname(ROOT), "发布")
发布子 = os.path.join(发布, APP)

内容 = ["lessons", "games", "embedded", "raspi", "ai", "exams", "csharp", "docs",
        "engine", "engine-solutions"]
# ⚠ `engine-answers/`（学员的答题文件）**不发布** —— 否则新用户一开箱就看到别人的答案。

入口 = os.path.join(BUILD, "entry.py")
# ⚠ 入口**从受版本控制的模板复制**，不在脚本里拼字符串。
#   原来拼字符串有两个后果，都实测踩到了：
#     · 想给它加兜底错误处理，一改就被下次构建覆盖
#     · 它不是仓库文件，`git log` 里没有历史 ——
#       而出事时它恰恰是"程序能不能起来"的那个入口
模板 = os.path.join(BUILD, "entry_template.py")
if not os.path.isfile(模板):
    print("  ✗ 找不到入口模板：%s" % 模板)
    sys.exit(1)
shutil.copyfile(模板, 入口)
print("  0 入口已由模板生成（entry_template.py → entry.py）")

shutil.rmtree(DIST, ignore_errors=True)
shutil.rmtree(WORK, ignore_errors=True)
args = [sys.executable, "-m", "PyInstaller",
        "--noconfirm", "--clean", "--onedir", "--console",
        "--name", APP, "--distpath", DIST, "--workpath", WORK, "--specpath", BUILD,
        "--paths", ROOT, 入口]

# ⚠⚠ 动态 import 的模块必须**显式声明**，PyInstaller 的静态分析看不到。
#
#   实测踩到：`引擎 贪吃蛇` 在产物里报
#       ModuleNotFoundError: No module named 'hybridengine_light.pyengine'
#   因为 `cmd_engine` 里是 `__import__(模块路径, ...)` —— 字符串，静态分析不出来
#   （源码下能跑，打包后缺模块 —— **这类错只有打包后才现形**）。
#
#   凡是"用字符串拼出来的 import"都要在这里补一条。
隐藏导入 = [
    "hybridengine_light.pyengine",
    "hybridengine_light.pyengine.render",
    "hybridengine_light.pyengine.scene",
    "hybridengine_light.pyengine.engine",
    "hybridengine_light.pyengine.components",
    "hybridengine_light.pyengine.games",
    "hybridengine_light.pyengine.games.snake",
    # 引擎课程线（`引擎课程` 命令）—— 同样是动态加载
    "hybridengine_light.enginecourse",
]
for m in 隐藏导入:
    args += ["--hidden-import", m]

print("  1 建 exe...（含 %d 个显式隐藏导入）" % len(隐藏导入))
r = subprocess.run(args, cwd=ROOT, capture_output=True)
if r.returncode != 0:
    for line in (r.stdout + r.stderr).decode("utf-8", "replace").splitlines()[-20:]:
        print("   ", line)
    sys.exit(1)
exe = os.path.join(OUTDIR, 可执行名)
print("     exe: %s (%.0fKB)" % (os.path.isfile(exe), os.path.getsize(exe) / 1024 if os.path.isfile(exe) else 0))

print("  2 复制课程内容到 exe 旁边...")
for d in 内容:
    src = os.path.join(ROOT, d)
    if not os.path.isdir(src):
        print("     %s/ : 源不存在，跳过" % d)
        continue
    dst = os.path.join(OUTDIR, d)
    shutil.rmtree(dst, ignore_errors=True)
    # ⚠ 忽略 `bin`/`obj`（构建产物）—— 但**下面第 2b 步会把预构建的补回去**。
    shutil.copytree(src, dst, ignore=shutil.ignore_patterns(
        "__pycache__", "*.pyc", "bin", "obj", "answers", "*-answers"))
    n = sum(len(f) for _, _, f in os.walk(dst))
    print("     %s/ : %d 文件" % (d, n))

# ── 2b 预构建四条编译型课程 ────────────────────────────────────────────
#
# ⚠⚠ 为什么要在**打包时**构建它们：
#
#   学员第一次跑 `c-lessons` 时，`dotnet run` 会**现场编译**：
#     · 慢（十几秒，让人以为卡死）
#     · **要联网**（NuGet 还原）—— 没网就直接失败，而学员根本不知道为什么
#
#   预构建之后：`--no-build` 直接跑，**首次就是瞬间**，也不需要联网。
#   代价只有约 1.5MB（四个工程各 ~0.4MB）。
#
#   ⚠ 这也是"能配的都配好"里**能配的那部分** —— 工具链（.NET SDK 4GB、
#     MinGW 914MB、JDK 373MB）装不进 11MB 的包，那是物理限制。
print("  2b 预构建四条编译型课程...")
编译型 = [
    # ⚠ C# 那条线原来指向 `lessons`（一个演示程序）—— 现在是真正的 `cs-lessons`。
    #   漏了它的话，界面里 C# 会因为首次构建慢而迟迟不出现。
    ("cs-lessons",   os.path.join("csharp", "cs-lessons", "CsLessons.csproj")),
    ("c-lessons",    os.path.join("csharp", "c-lessons", "CLessons.csproj")),
    ("cpp-lessons",  os.path.join("csharp", "cpp-lessons", "CppLessons.csproj")),
    ("java-lessons", os.path.join("csharp", "java-lessons", "JavaLessons.csproj")),
]
if shutil.which("dotnet") is None:
    print("     ⚠ 没有 dotnet —— 跳过预构建（产物里那几条线首次会现场编译）")
else:
    # ⚠ 必须在**资源目录**（ROOT/csharp/...）里构建，然后把 bin/obj 复制进产物。
    #   直接在产物目录构建的话，路径变了 obj 里的引用会失效。
    for 名, 相对 in 编译型:
        工程 = os.path.join(ROOT, 相对)
        if not os.path.isfile(工程):
            print("     %s: 找不到工程，跳过" % 名)
            continue
        # ⚠⚠ **明确指定 Debug**。
        #   不指定的话，源码目录里若残留了别的配置（比如我测"自包含发布"时
        #   留下的 `bin/Release/net8.0/win-x64/`，里面是一个 33MB 的单文件 exe），
        #   复制 bin/obj 时会把它们一起打进包 —— 那一次产物从 12MB 涨到 110MB。
        #   ⇒ 只发 Debug，并在复制时排除 Release。
        r = subprocess.run(["dotnet", "build", 工程, "-c", "Debug", "-v", "q", "--nologo"],
                           cwd=ROOT, capture_output=True, timeout=900)
        if r.returncode != 0:
            print("     %s: ✗ 构建失败 —— 产物里这条线首次会现场编译" % 名)
            for line in (r.stdout + r.stderr).decode("utf-8", "replace").splitlines()[-4:]:
                print("        " + line.strip()[:110])
            continue
        # 把 bin/obj 复制进产物（obj 也要 —— dotnet 判断"要不要重编"靠它）
        目录 = os.path.dirname(os.path.join(ROOT, 相对))
        变了 = 0
        for 子 in ("bin", "obj"):
            源 = os.path.join(目录, 子)
            if os.path.isdir(源):
                目的 = os.path.join(OUTDIR, os.path.dirname(相对), 子)
                shutil.rmtree(目的, ignore_errors=True)
                # ⚠ 排除 Release 与 pdb：只带上"能直接跑"的 Debug 产物。
                #   Release 里可能有自包含发布的 33MB 单文件 exe。
                shutil.copytree(源, 目的, ignore=shutil.ignore_patterns(
                    "*.pdb", "Release", "win-x64", "win-x86", "linux-x64"))
                变了 += sum(len(f) for _, _, f in os.walk(目的))
        print("     %s: ✓ 预构建好了（%d 文件）" % (名, 变了))

print("  3 写「怎么用.txt」...")

# ⚠⚠ 这份说明**按平台生成**，也是**课数的单一真值来源之一**。
#
#   原来它是写死的：全是 `HybridEngine-light.exe`，而且课程数还停在
#   「C++ 课程，8 课」「Java 课程，8 课」「C 课程，8 课」——
#   实际早就 41 课了。**学员拿到的是过时信息**，而没人会主动去核这个文件。
#
#   ⇒ 用法行按平台给（Linux 上是 `./HybridEngine-light`），
#     课数**从目录数出来**，不写死。
def _数课(目录: str, 后缀: str) -> int:
    p = os.path.join(ROOT, 目录)
    if not os.path.isdir(p):
        return 0
    import re as _re
    n = 0
    for f in os.listdir(p):
        if f.endswith(后缀) and _re.match(r"^第?\d\d", f):
            n += 1
    return n


_课数 = {
    "Python": _数课("lessons", ".py"),
    "C++": _数课(os.path.join("csharp", "cpp-lessons", "Lessons"), ".cs"),
    "Java": _数课(os.path.join("csharp", "java-lessons", "Lessons"), ".cs"),
    "C": _数课(os.path.join("csharp", "c-lessons", "Lessons"), ".cs"),
    "C#": _数课(os.path.join("csharp", "cs-lessons", "Lessons"), ".cs"),
}

_程序 = 可执行名 if 是WIN else "./" + 可执行名   # 非 Windows 要 ./ 才跑得起来
_换行 = "\r\n" if 是WIN else "\n"                # Windows 记事本需要 CRLF
_答题目录 = "lessons\\answers\\" if 是WIN else "lessons/answers/"

io.open(os.path.join(OUTDIR, "怎么用.txt"), "w", encoding="utf-8", newline=_换行).write(
    "HybridEngine-light —— 学编程，从第一行代码看到画面\n"
    "================================================\n"
    "\n"
    + ("双击 %s 就能用，**不需要装 Python**。\n" % 可执行名 if 是WIN else
       "在终端里跑 %s 就能用，**不需要另外装 Python**。\n" % _程序) +
    "\n"
    "【Python 课程 / 小游戏 / 嵌入式 / 树莓派 / AI】—— 开箱即用\n"
    "    %-22s 有哪些课\n" % _程序 +
    "    %-22s 看第 1 课\n" % (_程序 + " 01") +
    "    %-22s 写代码 —— **内置编辑器**，不用另开记事本\n" % (_程序 + " 编辑 01") +
    "    %-22s 检查你的答案（首次会生成答题模板）\n" % (_程序 + " 检查 01") +
    "    %-22s 参考答案\n" % (_程序 + " 答案 01") +
    "    %-22s 小游戏 / 嵌入式 / 树莓派 / AI / 考试\n" % (_程序 + " 游戏") +
    "\n"
    "你的答题文件在 %s 下（就在这个文件夹里，看得见、改得动）。\n" % _答题目录 +
    "\n"
    "【C / C++ / Java / C# 课程】—— 这四门要装工具链\n"
    "    课程内容在 csharp/ 下，运行要靠 .NET：\n"
    "        dotnet run --project csharp/c-lessons       （C 课程，%d 课）\n" % _课数["C"] +
    "        dotnet run --project csharp/cpp-lessons     （C++ 课程，%d 课）\n" % _课数["C++"] +
    "        dotnet run --project csharp/java-lessons    （Java 课程，%d 课）\n" % _课数["Java"] +
    "        dotnet run --project csharp/cs-lessons      （C# 课程，%d 课）\n" % _课数["C#"] +
    "\n"
    "    为什么这四门不一样：课程程序要把你写的代码**真的编译并运行**才能判对错，\n"
    "    所以它必须有编译器。Python 那几门只用 Python 自己，所以能开箱即用。\n"
    "\n"
    + ("    ⚠ 要装什么（exe 里已经能显示，不用记）：\n"
       "        winget install Microsoft.DotNet.SDK.8                    （.NET SDK）\n"
       "        winget install BrechtSanders.WinLibs.POSIX.UCRT          （gcc / g++）\n"
       "        JDK（跑 Java；注意是 JDK，不是 JRE）\n"
       "        装完**重开一个窗口**（让 PATH 生效）。\n"
       if 是WIN else
       "    ⚠ 要装什么（程序里会把缺的列出来，不用记）：\n"
       "        sudo apt install dotnet-sdk-8.0 g++ default-jdk   （Debian / Ubuntu）\n"
       "        sudo dnf install dotnet-sdk-8.0 gcc-c++ java-17-openjdk-devel  （Fedora）\n"
       "        ⚠ Java 要的是 **JDK**，不是 JRE —— JRE 里没有 javac。\n"
       "        ⚠ 图形界面要 Tk：Debian/Ubuntu 上 sudo apt install python3-tk\n") +
    "\n"
    "    ✅ 已经替你做好的部分：\n"
    "        · 四条编译型课程已经预先构建好，第一次跑不用等 NuGet 还原。\n"
    "        · 你可以把工具链放进程序旁边的 `tools/` 目录（便携方式），\n"
    "          程序会**自动认出来**，不必改系统 PATH。\n"
    "\n"
    "_internal/ 是运行时文件，不用管它；也不要从那里启动。\n"
    "\n"
    "⚙ 本项目由 AI 协作完成（人类定方向与验收）。详见仓库 README。\n")
print("     怎么用.txt ✓（课数：%s）"
      % " ".join("%s %d" % (k, v) for k, v in _课数.items()))

print("  4 核对...")
必查 = ["lessons", "games", "exams", "lessons/solutions", "怎么用.txt"]
好 = True
for k in 必查:
    p = os.path.join(OUTDIR, k.replace("/", os.sep))
    ok = os.path.exists(p)
    好 = 好 and ok
    print("     %s %s" % ("OK " if ok else "XX ", k))
tot = sum(len(f) for _, _, f in os.walk(OUTDIR))
print("  => 共 %d 个文件；%s" % (tot, "核对通过" if 好 else "有缺失"))
if not 好:
    sys.exit(1)

# ── 5 打包 zip（⚠ 必须在**搬走之前**做）──────────────────────────────
#   ⚠ 这里曾经留过一段旧的"搬到发布位置"，导致顺序变成
#     「先搬空 dist ⇒ 再打包 ⇒ FileNotFoundError」。
#     **是我 edit 时只替换了后半段。** 教训：改流程代码后要**通读一遍顺序**，
#     不能只看"新加的那段对不对"。
print("  5 打包 zip…（⚠ 必须在**搬走之前**做，见下）")
版本 = "0.0.0"
try:
    import re as _re
    _t = io.open(os.path.join(ROOT, "hybridengine_light", "__init__.py"), encoding="utf-8").read()
    _m = _re.search(r'__version__\s*=\s*"([^"]+)"', _t)
    if _m:
        版本 = _m.group(1)
except Exception as _e:
    print("     ⚠ 读不到 __version__（%s），zip 名用 0.0.0" % _e)

zip路径 = os.path.join(发布, "%s-v%s-%s%s" % (APP, 版本, 平台标记, 包后缀))
os.makedirs(发布, exist_ok=True)
if os.path.isfile(zip路径):
    os.remove(zip路径)
# ⚠⚠ 这里踩过一个大坑，写在最显眼处：
#   原来写成 make_archive(root_dir=发布, base_dir=APP) ——
#   而 `发布\HybridEngine-light` 就是**成品本身** ⇒ 等于把自己再包一层，
#   zip 里成了 `HybridEngine-light/HybridEngine-light/_internal/...`。
#   更糟的是下一轮构建时 `shutil.move(OUTDIR, 发布子)` 发现目标目录已存在，
#   就把**新产物移进了那个嵌套目录里**，外层只剩空壳（13 个文件的 _internal）
#   ⇒ **exe 起不来**（缺 base_library.zip / encodings）。
#
#   ⇒ 从 **PyInstaller 的 dist 目录**打包：它天然就是
#     `dist\HybridEngine-light\...`，包出来第一层正是 `HybridEngine-light/`。
#   ⇒ 而且必须在**搬走之前**打 —— 搬走之后 dist 就没了。
#
# ⚠ make_archive 的 `base_name` **不要带后缀**（它自己加），
#   而 `.tar.gz` 是个**双后缀** —— `zip路径[:-4]` 只砍得掉 `.zip`，
#   对 `x.tar.gz` 会砍成 `x.t`，于是打出 `x.t.tar.gz`（名字错但不会报错）。
#   ⇒ 按后缀分派：`.tar.gz` 砍 7 位，`.zip` 砍 4 位。
if 包后缀 == ".tar.gz":
    shutil.make_archive(zip路径[:-7], "gztar", DIST, APP)
else:
    shutil.make_archive(zip路径[:-4], "zip", DIST, APP)
print("     %s  (%.1fMB)  版本 %s" % (zip路径, os.path.getsize(zip路径) / 1024 / 1024, 版本))

# ── 6 搬到发布位置（`引擎\发布\`）────────────────────────────────────
#   ⚠ 用"建到临时区再整体替换"而不是直接把 --distpath 指过去：
#     PyInstaller 的 --noconfirm 会**整个删掉 distpath**，
#     里面有 zip 和说明的话会被一起删。
print("  6 搬到发布位置…")
已存在 = os.path.isdir(发布子)
if 已存在:
    # ⚠ 必须**整个删掉**再搬：`shutil.move` 在目标已存在时会**移进去**，
    #   于是成品变成"目录套目录"，外层只剩空壳 —— 实测就是这么坏的。
    shutil.rmtree(发布子, ignore_errors=True)
shutil.move(OUTDIR, 发布子)
print("     %s  %s" % (发布子, "（替换了旧的一份）" if 已存在 else "（新建）"))

# ── 6b 兜底：万一是"目录套目录"，就地拍平 ─────────────────────────────
#   ⚠⚠ 为什么要这一步：上面那句 `rmtree` 是**应该**能防住的，
#     但实测还是出现过一次嵌套（`发布子\HybridEngine-light\_internal\...`），
#     外层只剩一个 exe + 一个 13 文件的 `_internal` 空壳，
#     自检报「缺 base_library.zip」。
#     而我**没能复现出 rmtree 为什么没生效**（可能是删除时的瞬时占用），
#     所以不赌它 —— 搬完直接检查一次实际形状，是嵌套就拍平。
#
#   这段的真价值不在"修一次"，而在：**脚本自己能发现自己做错了**，
#   而不是把问题留给用户双击"没反应"。
嵌套 = os.path.join(发布子, APP)
if os.path.isdir(嵌套) and os.path.exists(os.path.join(嵌套, "_internal")):
    print("     ⚠ 检测到目录套目录，就地拍平…")
    # ⚠⚠ 先把**外层空壳**整个删掉 —— 它里面也有 `HybridEngine-light.exe`
    #   和 `_internal`，直接按名字往上传会撞名：
    #       FileExistsError: [WinError 183] 当文件已存在时，无法创建该文件
    #   实测就是这个错。空壳里的东西**本来就不完整**（那份 `_internal` 只有 13 个文件），
    #   没有任何值得保留的 —— 所以不是"合并"，是"丢掉壳、把真产物提上来"。
    存 = os.path.join(发布, "_拍平暂存")
    shutil.rmtree(存, ignore_errors=True)
    shutil.move(嵌套, 存)                     # 真产物先挪出去
    shutil.rmtree(发布子, ignore_errors=True)  # 连壳一起删
    os.makedirs(发布子, exist_ok=True)
    for 名 in os.listdir(存):
        shutil.move(os.path.join(存, 名), os.path.join(发布子, 名))
    shutil.rmtree(存, ignore_errors=True)
    print("     拍平完成")

# ── 7 自检：产物**真的能跑**吗 ────────────────────────────────────────
#   ⚠⚠ 这一步是这次翻车的直接补丁。
#     我上一轮改完脚本后，只看了"文件数 1055→1071"就以为成功 ——
#     **没有真跑一次**。而实际上 exe 已经起不来了（缺 Python 运行时），
#     是用户双击"没反应"才暴露的。
#     ⇒ **判据必须验证"能不能跑"，不是"文件数对不对"。**
print("  7 自检：真跑一次…")
# ⚠⚠ 运行库的名字**按平台不同**，而且原来这里写死了 `"python3" + "12.dll"`
#     和 `APP + ".exe"` —— 于是 Linux / macOS 上自检**必然报
#     「缺关键运行时文件」**，明明产物是好的。
#     这是**假失败**，比不检查更坏：它会让在 Linux 上打包的人以为构建坏了。
#     （同一个文件上面第 39~55 行已经做了平台自适应，这里当初漏了。）
#   ⇒ 版本从**当前解释器**推（不再写死 12），后缀按平台给，
#     再留一个 glob 兜底（各发行版的 .so 后缀不一定一样）。
_py = "%d.%d" % sys.version_info[:2]                    # 例如 3.12
if 是WIN:
    _运行库名 = "python%s.dll" % _py.replace(".", "")    # python312.dll
elif 是MAC:
    _运行库名 = "libpython%s.dylib" % _py                # libpython3.12.dylib
else:
    _运行库名 = "libpython%s.so.1.0" % _py               # libpython3.12.so.1.0


def _找运行库(目录: str) -> str | None:
    """找解释器动态库。精确名不在就 glob 兜底（各平台后缀不同）。"""
    p = os.path.join(目录, _运行库名)
    if os.path.exists(p):
        return p
    import glob as _glob
    for 模式 in ("libpython%s.so*" % _py,
                 "libpython%s*.dylib" % _py,
                 "python%s*.dll" % _py.replace(".", "")):
        命中 = _glob.glob(os.path.join(目录, 模式))
        if 命中:
            return 命中[0]
    return None


必查 = [os.path.join(发布子, "_internal", "base_library.zip"),
        os.path.join(发布子, 可执行名)]
缺 = [os.path.basename(p) for p in 必查 if not os.path.exists(p)]
# ⚠⚠ 运行库找不到时，**只敢在 Windows 上判失败**。
#
#   理由：`python312.dll` 这个名字在 Windows 上是**实测**过的（本机构建产物里
#   确实叫这个）。而 macOS 上 PyInstaller 可能放 `libpython3.12.dylib`、
#   也可能整包成 `Python.framework` —— **本机没有 macOS，验不了**。
#   如果我在这里对所有平台一律 `sys.exit(1)`，就等于**凭空发明了一个
#   "macOS 上必然失败"**的假判据（和"假失败"同类，且会挡住真正能用的构建）。
#   ⇒ Windows：硬失败（保持原行为，已实测）；其他平台：**大声警告但不拦**，
#     让对方自己看一眼 `_internal/` 并把实际情况记下来。
_运行库 = _找运行库(os.path.join(发布子, "_internal"))
if _运行库 is None:
    if 是WIN:
        缺.append(_运行库名)
    else:
        print("     ⚠ 在 _internal/ 里没找到运行库（找的名字：%s，也试了 glob）" % _运行库名)
        print("        ⚠ **这不一定是坏的** —— 这个平台上的实际名字/布局本机没验过")
        print("          （macOS 可能打包成 Python.framework 而不是 libpython*.dylib）。")
        print("          请自己 `ls _internal/ | grep -i python` 看一眼，并把结果记下来。")
else:
    print("     ✓ 运行库：%s" % os.path.basename(_运行库))
if 缺:
    print("     ✗ 缺关键运行时文件：%s" % "、".join(缺))
    print("       ⇒ 这个产物**起不来**，不要发布。")
    sys.exit(1)
# ⚠⚠ 自检必须跑**命令行模式**（`HYBRIDENGINE_LIGHT_CLI=1`）。
#
#   我第一版没设这个变量，于是自检启动的 exe **开了 GUI —— 而 GUI 不会退出**。
#   那个进程锁着自己的 `libffi-8.dll` 和 `HybridEngine-light.exe`，
#   于是**下一轮构建的 `shutil.rmtree(发布子)` 失败**（WinError 5 拒绝访问），
#   产物只搬了一半 ⇒ **exe 起不来**。
#
#   ⇒ 我的自检机制**自己造成**了"双击没反应"。这一条是那次翻车的直接补丁：
#     自检进程必须**跑完就退**，绝对不能留下一个活着的窗口进程。
自检环境 = {**os.environ, "HYBRIDENGINE_LIGHT_CLI": "1"}
自检环境.pop("PROMPT", None)
r = subprocess.run([os.path.join(发布子, 可执行名), "__诊断__"],
                   capture_output=True, cwd=发布子, timeout=120, env=自检环境)
出 = (r.stdout + r.stderr).decode("utf-8", "replace")
if r.returncode != 0 or "frozen      = True" not in 出:
    print("     ✗ 自检失败（退出码 %s）：" % r.returncode)
    for line in 出.splitlines()[:12]:
        print("        " + line)
    sys.exit(1)
for line in 出.splitlines():
    if line.strip().startswith(("frozen", "内容根", "lessons 数", "Tk 版本")):
        print("       " + line.strip())
print("     ✓ 自检通过（exe 真的能跑，且进程已退出）")

# ⚠ 再确认一次：自检**没有留下活着的进程**。
#   留一个就会锁住产物，害得下一轮构建搬一半 —— 正是上一轮的事故。
#
# ⚠⚠ 这一步原来**只写了 Windows 的 `tasklist`**。Linux/macOS 上没有这个
#   命令 ⇒ FileNotFoundError ⇒ 被下面的 `except` 吃掉 ⇒
#   **"查不到"被当成了"没问题"**（和"没跑到也算过"是同一类毛病）。
#   ⇒ 按平台分流，并且把"没有这个工具"**说出来**，不静默跳过。
try:
    import subprocess as _sp
    if 是WIN:
        查 = _sp.run(["tasklist", "/FI", "IMAGENAME eq " + 可执行名],
                     capture_output=True, timeout=30)
        出 = (查.stdout + 查.stderr).decode("utf-8", "replace")
        还在 = (可执行名 in 出
                and "没有运行的任务" not in 出
                and "No tasks are running" not in 出)
        结束提示 = "taskkill /IM %s /F" % 可执行名
    else:
        #: POSIX：按**产物全路径**匹配，免得误伤同名的别的进程
        查 = _sp.run(["pgrep", "-f", os.path.join(发布子, 可执行名)],
                     capture_output=True, timeout=30)
        还在 = 查.returncode == 0 and bool(查.stdout.strip())
        结束提示 = "pkill -f %s" % 可执行名
    if 还在:
        print("     ✗ 自检留下了活着的进程（会锁住产物，害得下轮构建搬一半）")
        print("       请先结束它：%s" % 结束提示)
        sys.exit(1)
    print("     ✓ 确认没有残留进程")
except FileNotFoundError:
    print("     ⚠ 本平台没有 tasklist / pgrep —— **跳过**残留进程检查（不算通过）")
except Exception as _e:
    # ⚠ 原来这里是 `pass`（"查不到就算了"）。但**静默**跳过会让"没跑到"
    #   和"查过了、没问题"长得一模一样 —— 正是第 22 条教训那一类。
    #   ⇒ 保留"不因此挡住构建"的意图，但**把跳过这件事说出来**。
    print("     ⚠ 残留进程检查本身出错了（%s: %s）—— **跳过**，不算通过"
          % (type(_e).__name__, _e))

# ⚠⚠ 第二步自检：**从产物里真跑一次需要动态 import 的命令**。
#
#   实测踩到：`引擎 贪吃蛇` 在产物里报 ModuleNotFoundError，
#   而第一步自检（`__诊断__`）**全绿** —— 因为那条路径不碰动态 import。
#   ⇒ "能启动"不等于"功能都在"。自检必须覆盖**会缺东西的那条路**。
print("  7b 自检：从产物里跑引擎命令…")
for 命令, 期望 in ((["引擎"], "贪吃蛇"),
                  (["引擎", "说明"], "组件"),
                  # ⚠ 引擎**课程**也要检 —— 它是另一条动态加载路径，
                  #   第一版自检只检了 `引擎`，而 `引擎课程` 是单独一个模块。
                  (["引擎课程"], "用组件拼游戏"),
                  (["引擎课程", "01"], "场景与对象")):
    try:
        q = subprocess.run([os.path.join(发布子, 可执行名), *命令],
                           capture_output=True, cwd=发布子, timeout=120, env=自检环境)
        qs = (q.stdout + q.stderr).decode("utf-8", "replace")
    except Exception as e:
        qs = f"（跑不起来：{e}）"
    if 期望 not in qs:
        print("     ✗ `%s` 的输出里没有「%s」：" % (" ".join(命令), 期望))
        for line in qs.splitlines()[:8]:
            print("        " + line)
        print("       ⇒ 多半是**动态 import 的模块没打进去**（见上面的 --hidden-import）")
        sys.exit(1)
    print("     ✓ `%s` 正常" % " ".join(命令))
print("     ✓ 动态 import 的模块都在")

# 清理临时区（dist 与 work 都是可再生的中间物）
shutil.rmtree(DIST, ignore_errors=True)
shutil.rmtree(WORK, ignore_errors=True)
print("     已清理 _build 中间物")
print("  => 成品在：%s" % 发布子)
sys.exit(0)
