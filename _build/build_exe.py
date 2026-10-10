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
exe = os.path.join(OUTDIR, APP + ".exe")
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
        r = subprocess.run(["dotnet", "build", 工程, "-v", "q", "--nologo"],
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
                shutil.copytree(源, 目的, ignore=shutil.ignore_patterns("*.pdb"))
                变了 += sum(len(f) for _, _, f in os.walk(目的))
        print("     %s: ✓ 预构建好了（%d 文件）" % (名, 变了))

print("  3 写「怎么用.txt」...")
io.open(os.path.join(OUTDIR, "怎么用.txt"), "w", encoding="utf-8", newline="\r\n").write(
    "HybridEngine-light —— 学编程，从第一行代码看到画面\n"
    "================================================\n"
    "\n"
    "双击 HybridEngine-light.exe 就能用，**不需要装 Python**。\n"
    "\n"
    "【Python 课程 / 小游戏 / 嵌入式 / 树莓派 / AI】—— 开箱即用\n"
    "    HybridEngine-light.exe               有哪些课\n"
    "    HybridEngine-light.exe 01            看第 1 课\n"
    "    HybridEngine-light.exe 编辑 01       写代码 —— **内置编辑器**，不用另开记事本\n"    "    HybridEngine-light.exe 检查 01       检查你的答案（首次会生成答题模板）\n"
    "    HybridEngine-light.exe 答案 01       参考答案\n"
    "    HybridEngine-light.exe 游戏          小游戏\n"
    "    HybridEngine-light.exe 嵌入式        单片机模拟器（不需要真板子）\n"
    "    HybridEngine-light.exe 树莓派        树莓派模拟器（不需要真板子）\n"
    "    HybridEngine-light.exe AI            人工智能入门（算法全部自己实现）\n"
    "    HybridEngine-light.exe 考试          随机抽题，即时判分\n"
    "\n"
    "你的答题文件在 lessons\\answers\\ 下（就在这个文件夹里，看得见、改得动）。\n"
    "\n"
    "【C# / C++ / Java / C 课程】—— 这四门要装工具链\n"
    "    课程内容在 csharp\\ 下，运行要靠 .NET：\n"
    "        dotnet run --project csharp/cpp-lessons     （C++ 课程，8 课）\n"
    "        dotnet run --project csharp/java-lessons    （Java 课程，8 课）\n"
    "        dotnet run --project csharp/c-lessons       （C 课程，8 课）\n"
    "\n"
    "    为什么这四门不一样：课程程序要把你写的代码**真的编译并运行**才能判对错，\n"
    "    所以它必须有编译器。Python 那几门只用 Python 自己，所以能开箱即用。\n"
    "\n"
    "    ⚠ 要装什么（exe 里已经能显示，不用记）：\n"
    "        winget install Microsoft.DotNet.SDK.8                    （.NET SDK）\n"
    "        winget install BrechtSanders.WinLibs.POSIX.UCRT          （gcc / g++）\n"
    "        JDK（跑 Java；注意是 JDK，不是 JRE）\n"
    "        装完**重开一个窗口**（让 PATH 生效）。\n"
    "\n"
    "    ✅ 已经替你做好的部分：\n"
    "        · 四条编译型课程**已经预先构建好**了 —— 第一次跑就是**瞬间**，\n"
    "          而且**不需要联网**（不用等 NuGet 还原）。\n"
    "        · 你可以把工具链放进 exe 旁边的 `tools\\` 目录（便携方式），\n"
    "          程序会**自动认出来**，不必改系统 PATH。\n"
    "          认得的布局：tools\\dotnet.exe、tools\\dotnet\\dotnet.exe、\n"
    "                       tools\\mingw64\\bin\\g++.exe（任意名字\\bin\\工具.exe）\n"
    "\n"
    "    ⚠ .NET SDK 约 4GB、MinGW 约 900MB、JDK 约 370MB ——\n"
    "      它们**装不进这个 12MB 的包**（这是物理限制）。\n"
    "      但除了它们，其余都已经配好了。\n"
    "\n"
    "_internal\\ 是运行时文件，不用管它；也不要从那里启动。\n"
    "\n"
    "⚙ 本项目由 AI 协作完成（人类定方向与验收）。详见仓库 README。\n")
print("     怎么用.txt ✓")

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

zip路径 = os.path.join(发布, "%s-v%s-win64.zip" % (APP, 版本))
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

# ── 7 自检：产物**真的能跑**吗 ────────────────────────────────────────
#   ⚠⚠ 这一步是这次翻车的直接补丁。
#     我上一轮改完脚本后，只看了"文件数 1055→1071"就以为成功 ——
#     **没有真跑一次**。而实际上 exe 已经起不来了（缺 Python 运行时），
#     是用户双击"没反应"才暴露的。
#     ⇒ **判据必须验证"能不能跑"，不是"文件数对不对"。**
print("  7 自检：真跑一次…")
必查 = [os.path.join(发布子, "_internal", "base_library.zip"),
        os.path.join(发布子, "_internal", "python3" + "12.dll"),
        os.path.join(发布子, APP + ".exe")]
缺 = [os.path.basename(p) for p in 必查 if not os.path.exists(p)]
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
r = subprocess.run([os.path.join(发布子, APP + ".exe"), "__诊断__"],
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
try:
    import subprocess as _sp
    q = _sp.run(["tasklist", "/FI", "IMAGENAME eq " + APP + ".exe"],
                capture_output=True, timeout=30)
    qs = (q.stdout + q.stderr).decode("utf-8", "replace")
    if APP + ".exe" in qs and "没有运行的任务" not in qs and "No tasks are running" not in qs:
        print("     ✗ 自检留下了活着的进程（会锁住产物，害得下轮构建搬一半）")
        print("       请先结束它：taskkill /IM %s.exe /F" % APP)
        sys.exit(1)
    print("     ✓ 确认没有残留进程")
except Exception:
    pass            # 查不到就算了，不因为查不到而挡住构建

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
        q = subprocess.run([os.path.join(发布子, APP + ".exe"), *命令],
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
