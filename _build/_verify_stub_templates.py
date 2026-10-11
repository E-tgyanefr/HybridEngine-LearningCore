# 实测：四条线的模板生成器**真的调一次**，量行数、量关键词、量讲解有没有丢、有没有放错地方。
#
# ⚠ 为什么不能只看代码：这轮已经出现过两次"看着对、其实补丁没生效"。
#   所以这份脚本做四件事：
#     1. 打印每条线第 1 课模板的**真实内容**（人眼确认它变长了、含讲解）
#     2. 量行数 / 关键词命中 / 讲解来源行数
#     3. 断言：生成出来的是**合法 Python**；讲解**一行都没丢**、且都在
#        「轮到你了」**前面**；「要做」整段都在「轮到你了」里、且只出现一次
#     4. 模拟**打包成 exe**（sys.frozen），确认提示里不会写死 `python`
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
sys.path.insert(0, LC)
os.chdir(LC)

from pathlib import Path                                          # noqa: E402
from hybridengine_light import _stub as S                         # noqa: E402
from hybridengine_light import aicourse, embedded, enginecourse   # noqa: E402
from hybridengine_light import raspi_course, run as R             # noqa: E402
from hybridengine_light._paths import 命令名                        # noqa: E402

关键 = ("先介绍", "轮到你了", "常见坑", "怎么自己测", "这一课")

# 线名, 课程目录, 取模板的函数, 这一课的「轮到你了」段前后用什么字界开
线们 = [
    ("python", "lessons", lambda p: R._stub_for(p), "轮到你了", "怎么自己测"),
    ("ai", "ai", aicourse._生成模板, "轮到你了", "怎么自己测"),
    ("embedded", "embedded", embedded._生成模板, "轮到你了", "怎么自己测"),
    ("raspi", "raspi", raspi_course._生成模板, "轮到你了", "怎么自己测"),
    ("engine", "engine", enginecourse._生成模板, "轮到你了", "怎么自己测"),
]


def 课们(目录: str) -> list[Path]:
    return sorted(Path(目录).glob("[0-9][0-9]_*.py"))


def _换(文: str) -> str:
    """把课程文件里的写死命令换成 `命令名()` —— 用来和模板正文对齐。"""
    return 文.replace("python -m hybridengine_light", 命令名())


def _行们(文: str) -> list[str]:
    return [l for l in 文.splitlines() if l.strip()]


def 检查(p: Path, 文: str, 自建: bool) -> list[str]:
    """返回问题列表（空 = 没问题）。"""
    错: list[str] = []
    try:
        compile(文, f"<模板 {p.name}>", "exec")          # 学员的文件是要被 import 的
    except SyntaxError as e:
        return [f"模板不是合法 Python：{e}"]

    if p.stem[:2] not in 文:
        错.append(f"模板里没有课号 {p.stem[:2]}")
    if 命令名() not in 文:
        错.append("模板里没有 `命令名()`")
    if not 自建:
        return 错

    for 标记 in S.分界标记:
        if 标记 in 文:
            错.append(f"把判分代码的标题也抄进来了：{标记}")

    # 段落切分：讲解段 / 轮到你了段
    a = 文.find("轮到你了")
    if a < 0:
        return 错 + ["没有「轮到你了」那一段"]
    b = 文.find("怎么自己测", a)
    if b < 0:
        return 错 + ["没有「怎么自己测」那一段"]
    轮到你了段, 讲解段 = 文[a:b], 文[:a]
    # ⚠ 用**整行**比对，不用子串：`x = x - 学习率 * …` 这种代码行在讲解里
    #   也有一份（缩进不同），子串比对会把正常情况误报成"抄了两遍"（实测）。
    讲解行集 = {l.rstrip() for l in 讲解段.splitlines()}
    任务行集 = {l.rstrip() for l in 轮到你了段.splitlines()}

    块 = S.开头讲解块(p)
    讲解, 任务 = S.拆任务(块)
    讲解 = _行们("\n".join(讲解[1:]))                     # 去掉标题行（它进了文档字符串第一行）

    # ① 讲解：一行都不能丢，而且必须都在「轮到你了」**前面**
    丢 = [l for l in 讲解 if _换(l).rstrip() not in 讲解行集]
    if 丢:
        错.append(f"讲解丢了/放错位置 {len(丢)}/{len(讲解)} 行，第一行：{丢[0][:50]!r}")

    # ② 「要做」：整段都要在「轮到你了」里，且不能同时留在讲解里（不许抄两遍）
    条目 = _行们("\n".join(任务))
    if not 条目:
        错.append("课程文件里找不到「要做」那一段")
    else:
        没搬 = [l for l in 条目 if _换(l).rstrip() not in 任务行集]
        if 没搬:
            错.append(f"「要做」有 {len(没搬)}/{len(条目)} 行没搬进「轮到你了」，"
                      f"第一行：{没搬[0][:50]!r}")
        重复 = [l for l in 条目[1:] if _换(l).rstrip() in 讲解行集]
        if 重复:
            错.append(f"「要做」有 {len(重复)} 行**同时**留在讲解里（抄了两遍）："
                      f"{重复[0][:40]!r}")
    return 错


print()
print("  ① 每条线第 1 课：模板行数 · 讲解行数 · 关键词命中")
print("  " + "─" * 90)
print("  %-9s %-9s %-11s %-9s %-9s %s"
      % ("课程线", "模板行数", "讲解来自", "关键词", "合法Python", "缺哪个关键词"))
全部问题: list[str] = []
统计 = {}
for 名, 目录, 生成, *_ in 线们:
    p = 课们(目录)[0]
    文 = 生成(p)
    命中 = [k for k in 关键 if k in 文]
    缺 = [k for k in 关键 if k not in 文]
    块 = S.开头讲解块(p)
    讲解, 任务 = S.拆任务(块)
    讲解行 = len(_行们("\n".join(讲解[1:])))
    错 = 检查(p, 文, 名 != "python")
    全部问题 += [f"{名}/{p.name}：{e}" for e in 错]
    统计[名] = (len(文.splitlines()), 讲解行, len(命中))
    print("  %-9s %6d 行  %6d 行    %d/%d      %-9s %s"
          % (名, len(文.splitlines()), 讲解行, len(命中), len(关键),
             "✅" if not 错 else "❌",
             "、".join(缺) or "（一个不缺）"))

print()
print("  ② 四条线的**全部**课程（19 课）跑同样的检查")
print("  " + "─" * 90)
for 名, 目录, 生成, *_ in 线们[1:]:
    for p in 课们(目录):
        文 = 生成(p)
        错 = 检查(p, 文, True)
        全部问题 += [f"{目录}/{p.name}：{e}" for e in 错]
        命中 = [k for k in 关键 if k in 文]
        print("  %-9s %-20s 模板 %4d 行  讲解 %4d 行  关键词 %d/%d  %s"
              % (名, p.name, len(文.splitlines()),
                 len(_行们("\n".join(S.拆任务(S.开头讲解块(p))[0][1:]))),
                 len(命中), len(关键), "✅" if not 错 else "❌ " + "；".join(错)))

print()
print("  ③ 模拟**打包成 exe**：提示里必须用 exe 名，不能写死 python")
print("  " + "─" * 90)
_真_frozen, _真_exe = getattr(sys, "frozen", False), sys.executable
try:
    sys.frozen = True                                  # type: ignore[attr-defined]
    sys.executable = r"C:\Users\学员\HybridEngine-light.exe"
    print(f"  命令名() = {命令名()}")
    for 名, 目录, 生成, *_ in 线们[1:]:
        p = 课们(目录)[0]
        文 = 生成(p)
        坏 = "python -m hybridengine_light" in 文
        if 坏:
            全部问题.append(f"{名}/{p.name}：exe 下提示里还写着 `python -m hybridengine_light`")
        print("  %-9s %-20s 写死 python：%-4s  exe 名出现 %d 次  %s"
              % (名, p.name, "是 ❌" if 坏 else "否 ✅",
                 文.count(命令名()), "✅" if not 坏 else "❌"))
finally:
    sys.executable = _真_exe
    if not _真_frozen:
        del sys.frozen                                 # type: ignore[attr-defined]

print()
print("  ④ 人眼核对：四条线第 1 课模板的**真实内容**")
for 名, 目录, 生成, *_ in 线们[1:]:
    p = 课们(目录)[0]
    文 = 生成(p).splitlines()
    print()
    print("  " + "═" * 88)
    print(f"  【{名}】{p.name}   →   {len(文)} 行（改之前是 "
          f"{'23' if 名 == 'ai' else '107' if 名 == 'embedded' else '161' if 名 == 'raspi' else '19'} 行）")
    print("  " + "═" * 88)
    for i, l in enumerate(文[:26], 1):
        print(f"  {i:3d}| {l}")
    print("  " + "·" * 66 + "   （中间是讲解，省略）")
    for i, l in enumerate(文[-20:], len(文) - 19):
        print(f"  {i:3d}| {l}")

print()
if 全部问题:
    print("  ❌ 有 %d 处问题：" % len(全部问题))
    for q in 全部问题:
        print("     · " + q)
    sys.exit(1)

print("  ✅ 第一~四部分全过：19 课模板都是合法 Python；讲解一行没丢、都在「轮到你了」前面；")
print("     「要做」整段搬到了末尾且没重复；exe 下提示用的是 exe 名。")

# ── ⑤ 端到端：真的按学员的路径走一遍 ────────────────────────────────────
# 第一次「检查」应当**建好答题文件**（内容 = 教程），第二次「检查」应当能
# 正常加载它、跑出一份"还没写"的报告 —— 而不是崩在导入上。
print()
print("  ⑤ 端到端：在**临时目录**里按学员的路径走一遍（不碰仓库里的答案文件）")
print("  " + "─" * 90)
import contextlib                                                # noqa: E402
import io                                                        # noqa: E402
import shutil                                                    # noqa: E402
import tempfile                                                  # noqa: E402
from hybridengine_light._paths import ROOT_ENV                   # noqa: E402

端到端 = [
    ("ai", "ai", aicourse._检查),
    ("embedded", "embedded", embedded._检查),
    ("raspi", "raspi", raspi_course._检查),
    ("engine", "engine", enginecourse._检查),
]
临时 = Path(tempfile.mkdtemp(prefix="stub_e2e_"))
_旧根 = os.environ.get(ROOT_ENV)
try:
    os.environ[ROOT_ENV] = str(临时)
    for 目录, 课程, 检查函数 in 端到端:
        p = 课们(目录)[0]
        (临时 / 目录).mkdir(parents=True, exist_ok=True)
        shutil.copy2(p, 临时 / 目录 / p.name)
        root = 临时
        缓 = io.StringIO()
        with contextlib.redirect_stdout(缓):
            码1 = 检查函数(root, p.stem[:2])          # 第一次：应当建模板
        答 = root / {"ai": "ai-answers", "embedded": "embedded-answers",
                     "raspi": "raspi-answers", "engine": "engine-answers"}[目录] / p.name
        建了 = 答.is_file()
        文 = 答.read_text(encoding="utf-8") if 建了 else ""
        缓2 = io.StringIO()
        with contextlib.redirect_stdout(缓2):
            码2 = 检查函数(root, p.stem[:2])          # 第二次：应当正常跑检查
        出2 = 缓2.getvalue()
        坏 = []
        if not 建了:
            坏.append("第一次「检查」没有建出答题文件")
        elif "这是第" not in 文 or "怎么自己测" not in 文:
            坏.append("建出来的答题文件不是教程")
        if "Traceback" in 出2 or "SyntaxError" in 出2:
            坏.append("第二次「检查」炸了")
        if "要做的文件里没有" in 出2 or "课程文件本身有问题" in 出2:
            坏.append("第二次「检查」读不到答题文件")
        全部问题 += [f"端到端 {目录}/{p.name}：{e}" for e in 坏]
        print("  %-9s 第一次检查→建模板 %s（%d 行）；第二次检查→%s 行报告  %s"
              % (目录, "✅" if 建了 else "❌", len(文.splitlines()),
                 len(出2.splitlines()), "✅" if not 坏 else "❌ " + "；".join(坏)))
finally:
    if _旧根 is None:
        os.environ.pop(ROOT_ENV, None)
    else:
        os.environ[ROOT_ENV] = _旧根
    shutil.rmtree(临时, ignore_errors=True)

print()
if 全部问题:
    print("  ❌ 有 %d 处问题：" % len(全部问题))
    for q in 全部问题:
        print("     · " + q)
    sys.exit(1)
print("  ✅ 全部通过（含端到端）。")
print()
