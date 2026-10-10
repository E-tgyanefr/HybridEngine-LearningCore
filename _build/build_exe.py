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

内容 = ["lessons", "games", "embedded", "raspi", "ai", "exams", "csharp", "docs"]

入口 = os.path.join(BUILD, "entry.py")
io.open(入口, "w", encoding="utf-8", newline="").write(
    '"""打包入口 —— 与 `python -m hybridengine_light` 等价。\n'
    '\n'
    '⚠ 先设 `HYBRIDENGINE_LIGHT_ROOT` 指向 **exe 旁边**（不是 `_internal/`）——\n'
    '  这样学员的课程目录、答题文件都在看得见的地方。\n'
    '"""\n'
    'import os\n'
    'import sys\n'
    '\n'
    'if getattr(sys, "frozen", False):\n'
    '    _base = os.path.dirname(os.path.abspath(sys.executable))\n'
    '    os.environ.setdefault("HYBRIDENGINE_LIGHT_ROOT", _base)\n'
    '\n'
    'from hybridengine_light.run import main\n'
    '\n'
    'if __name__ == "__main__":\n'
    '    sys.exit(main())\n')

shutil.rmtree(DIST, ignore_errors=True)
shutil.rmtree(WORK, ignore_errors=True)
args = [sys.executable, "-m", "PyInstaller",
        "--noconfirm", "--clean", "--onedir", "--console",
        "--name", APP, "--distpath", DIST, "--workpath", WORK, "--specpath", BUILD,
        "--paths", ROOT, 入口]
print("  1 建 exe...")
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
    shutil.copytree(src, dst, ignore=shutil.ignore_patterns(
        "__pycache__", "*.pyc", "bin", "obj", "answers", "*-answers"))
    n = sum(len(f) for _, _, f in os.walk(dst))
    print("     %s/ : %d 文件" % (d, n))

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
    "【C# / C++ / Java / C 课程】—— 这四门**需要你自己装工具链**\n"
    "    这四门的课程内容在 csharp\\ 下，运行要靠 .NET：\n"
    "        dotnet run --project csharp/cpp-lessons     （C++ 课程，8 课）\n"
    "        dotnet run --project csharp/java-lessons    （Java 课程，8 课）\n"
    "        dotnet run --project csharp/c-lessons       （C 课程，8 课）\n"
    "    所以要装：.NET SDK（跑课程程序）+ 对应编译器（g++ / javac / gcc）。\n"
    "    这是语言本身的要求，不是这个 exe 能替你装的。\n"
    "\n"
    "为什么这四门不一样：课程程序要把你写的代码**真的编译并运行**才能检查对错，\n"
    "所以它必须有编译器。Python 那几门只用 Python 自己，所以能开箱即用。\n"
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

# ── 5 搬到发布位置（`引擎\发布\`）────────────────────────────────────
#   ⚠ 用"建到临时区再整体替换"而不是直接把 --distpath 指过去：
#     PyInstaller 的 --noconfirm 会**整个删掉 distpath**，
#     里面有 zip 和说明的话会被一起删。
print("  5 搬到发布位置...")
os.makedirs(发布, exist_ok=True)
已存在 = os.path.isdir(发布子)
if 已存在:
    # 只删成品目录本身，不碰同级的 zip / 说明
    shutil.rmtree(发布子, ignore_errors=True)
shutil.move(OUTDIR, 发布子)
print("     %s  %s" % (发布子, "（替换了旧的一份）" if 已存在 else "（新建）"))

# 顺手把 zip 也做出来（与 GitHub Release 上传的是同一份）
# ⚠ zip 名**带版本号**，而版本号从代码里读（`__version__` 是单一真值）——
#   实测踩到：原来固定叫 `HybridEngine-light-win64.zip`，
#   于是发布目录里躺着两个同名内容（一个带版本号、一个不带），
#   **分不清哪个是当前版**。版本号只写在一处，名字就跟着它走。
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
已有 = [f for f in os.listdir(发布) if f.startswith(APP + "-v") and f.endswith("-win64.zip")]
for f in 已有:
    # 旧版本的 zip 是历史留档，**不删**（GitHub 上那份也是历史）；
    #   但同版本号的旧文件要替换，避免"同名两份内容不同"。
    if f == os.path.basename(zip路径):
        os.remove(os.path.join(发布, f))
shutil.make_archive(zip路径[:-4], "zip", 发布, APP)
print("     %s  (%.1fMB)  版本 %s" % (zip路径, os.path.getsize(zip路径) / 1024 / 1024, 版本))

# 清理临时区（_build\dist 与 work 都是可再生的中间物）
shutil.rmtree(DIST, ignore_errors=True)
shutil.rmtree(WORK, ignore_errors=True)
print("     已清理 _build 中间物")
print("  => 成品在：%s" % 发布子)
sys.exit(0)
