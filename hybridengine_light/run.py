"""hybridengine_light 课程运行器 —— `python -m hybridengine_light` 的入口。

════ 一条命令走完"看课程 → 写答案 → 检查" ════

    python -m hybridengine_light              列出所有课程
    python -m hybridengine_light 01           看第 1 课（读课程文件）
    python -m hybridengine_light 检查 01      检查第 1 课的答案
    python -m hybridengine_light 答案 01      看参考答案（先自己写，再看）
    python -m hybridengine_light 游戏         列出小游戏

════ 为什么用命令行而不是图形界面 ════

学编程本来就要学会用命令行 —— 这是**课程的一部分**，不是将就。
而且命令行没有任何依赖，装完 Python 就能用。
"""

from __future__ import annotations

import sys
import sys
from pathlib import Path

from ._paths import 内容根, 命令名




def _确保输出编码():
    """把输出流设成 UTF-8，并让编码错误降级成替换字符。

    ══ 为什么需要（实测）══
    中文 Windows 上，stdout **不是交互式终端**时 Python 用 GBK。
    报告里的 √ × 这些字符 GBK 里没有 → UnicodeEncodeError → **整份报告一个字都打不出来**。

        python -m hybridengine_light 检查 01 > out.txt     # 没兜底时：退出码 1，输出为空

    学生最常见的求助方式就是「把输出复制给我看」——
    如果重定向到文件就崩，这条路直接断掉。

    errors="replace" 是关键：宁可把个别字符显示成 ?，
    也不能因为一个字符让整份报告消失。
    """
    for 流名 in ("stdout", "stderr"):
        流 = getattr(sys, 流名, None)
        if 流 is None:
            continue
        try:
            流.reconfigure(encoding="utf-8", errors="replace")
        except Exception:
            # stdout 被别的库包过时没法 reconfigure —— 不是致命问题，
            # 下面的字符替换是第二道防线。
            pass


_确保输出编码()



__all__ = ["main"]

LESSONS_DIR = "lessons"
ANSWERS_DIR = "lessons/answers"      # 学生的工作文件（自动生成模板）
SOLUTIONS_DIR = "lessons/solutions"   # 参考答案（课程作者提供，只读）

# ⚠ 这两个目录必须分开。放一起的话，"参考答案"读到的会是学生写的文件 ——
#   学生写错了，参考答案就是错的。我实测撞上过：仓库里那份文件是 NameError 的代码，
#   而「答案 01」把它当标准答案打印了出来。
GAMES_DIR = "games"


def _root() -> Path:
    """项目根目录（本包所在目录的上一级）。"""
    return 内容根()


def _答题路径(课: Path) -> Path:
    """该课的答题文件路径（与「检查」用的是**同一个**真值）。

    ⚠ 必须共用：如果「编辑」打开的文件和「检查」读的文件不是同一个，
      学员会写到一个文件、检查另一个文件 —— 而那表现为"我明明写了却说过不了"，
      是最难查、最打击人的一类问题。
    """
    if _needs_multiple_files(课):
        return _root() / ANSWERS_DIR / 课.stem / "main.py"
    return _root() / ANSWERS_DIR / 课.name


def _lessons() -> list[Path]:
    d = _root() / LESSONS_DIR
    if not d.is_dir():
        return []
    # 只收 01_xxx.py 这种编号课程；跳过 answers 子目录
    return sorted(p for p in d.glob("[0-9][0-9]_*.py"))


def _find(id_or_name: str) -> Path | None:
    for p in _lessons():
        if p.stem.startswith(id_or_name):
            return p
    return None


def _cmd_list() -> int:
    ls = _lessons()
    print()
    print("════════════════════════════════════════════════════════")
    print("  HybridEngine-light 课程")
    print("════════════════════════════════════════════════════════")
    if not ls:
        print("  （还没有课程）")
        return 0
    for p in ls:
        # 用第一行文档字符串当标题
        title = _title_of(p)
        print(f"  {p.stem[:2]}   {title}")
    print()
    print(f"  看某一课：   {命令名()} 01")
    print(f"  检查答案：   {命令名()} 检查 01")
    print(f"  写代码：     {命令名()} 编辑 01        （内置编辑器，不用另开记事本）")
    print(f"  看参考答案： {命令名()} 答案 01")
    print(f"  小游戏：     {命令名()} 游戏")
    print(f"  嵌入式：     {命令名()} 嵌入式      （单片机模拟器，不需要真板子）")
    print(f"  树莓派：     {命令名()} 树莓派      （树莓派模拟器，不需要真板子）")
    print(f"  AI 入门：    {命令名()} AI          （零依赖，算法全部自己实现）")
    print(f"  考试：       {命令名()} 考试        （随机抽题，即时判分）")
    print(f"               {命令名()} 考试 --成绩  （历史与弱项）")
    print()
    return 0


def _title_of(p: Path) -> str:
    try:
        text = p.read_text(encoding="utf-8")
    except OSError:
        return p.stem
    for line in text.splitlines():
        line = line.strip()
        if line.startswith("#"):
            return line.lstrip("# ").strip()
    return p.stem


def _cmd_show(idf: str) -> int:
    p = _find(idf)
    if p is None:
        print(f"\n  没有找到「{idf}」这一课。用 `{命令名()}` 看列表。\n")
        return 1
    print()
    print(p.read_text(encoding="utf-8"))
    return 0


def _cmd_answer(idf: str) -> int:
    p = _find(idf)
    if p is None:
        print(f"\n  没有找到「{idf}」这一课。\n")
        return 1
    # 参考答案在 solutions/，**不是** answers/ —— 后者是学生自己的工作文件。
    # 读错目录会导致"学生写错了，参考答案也是错的"（我实测撞上过）。
    sol = _root() / SOLUTIONS_DIR / p.name
    sol_dir = _root() / SOLUTIONS_DIR / p.stem
    if sol.exists():
        print()
        print("── 参考答案 ─────────────────────────────────────────")
        print("  （先自己写一遍再看。抄一遍不如写错一遍学得多。）")
        print("─" * 56)
        print(sol.read_text(encoding="utf-8"))
        return 0
    if sol_dir.is_dir():
        print()
        print("── 参考答案 ─────────────────────────────────────────")
        print("  （这一课是多文件作业，下面是各个文件。）")
        print("─" * 56)
        for f in sorted(sol_dir.glob("*.py")):
            print(f"\n// ══ {f.name} ══\n")
            print(f.read_text(encoding="utf-8"))
        return 0
    print(f"\n  这一课还没有参考答案（应该是 {sol} 或 {sol_dir}/）。\n")
    return 1


def _cmd_edit(idf: str) -> int:
    """内置编辑器打开该课的答题文件（不存在就先建模板）。"""
    p = _find(idf)
    if p is None:
        print(f"\n  没有找到「{idf}」这一课。用 `{命令名()}` 看列表。\n")
        return 1
    文件 = _答题路径(p)
    if not 文件.exists():
        # ⚠ 先建模板再打开 —— 否则学员面对一个空文件，而"空白"是最劝退的起点
        if _needs_multiple_files(p):
            _make_multi_stub(p, 文件.parent)
        else:
            文件.parent.mkdir(parents=True, exist_ok=True)
            文件.write_text(_stub_for(p), encoding="utf-8")
        print(f"\n  已建好答题模板：{文件}")
    from .editor import 编辑
    return 编辑(str(文件))


def _cmd_check(idf: str) -> int:
    """检查答案：加载课程里的 CHECK_*，对学生写的答案跑一遍。"""
    from .check import load_module, run_checks, report
    from .explain import explain

    p = _find(idf)
    if p is None:
        print(f"\n  没有找到「{idf}」这一课。用 `{命令名()}` 看列表。\n")
        return 1

    answer = _load_answer(p, idf)
    if answer is None:
        return 1                       # 里面已经把原因讲清楚了

    try:
        lesson = load_module(p, name=f"lesson_{p.stem}")
    except Exception as e:                            # noqa: BLE001
        print("\n  课程文件本身有问题（这不是你的错）：")
        print(explain(e, filename=str(p)))
        return 1

    print(report(run_checks(lesson, answer), title=_title_of(p)))
    return 0


def _load_answer(p: Path, idf: str) -> object | None:
    """加载学生的答案。

    ══ 两种形态（为什么不是一种）══
    前几课的作业是「一个文件里写几个变量和函数」—— 单文件足够。
    但**模块化那一课**的作业本身就是「把代码拆成两个文件」，
    用单文件交就没法验证"两个文件之间能不能互相 import"，
    而那恰恰是那一课唯一要考的东西。

    所以：目录存在就用目录（模块化作业），否则用单文件（普通作业）。
    ══
    """
    from .check import load_module
    from .explain import explain

    dir_form = _root() / ANSWERS_DIR / p.stem      # lessons/answers/02_函数/
    file_form = _root() / ANSWERS_DIR / p.name     # lessons/answers/02_函数.py

    if dir_form.is_dir():
        # 目录形态：把目录里的 .py 都加载进来，汇成一个"答案对象"
        # （课程的 CHECK_ 既能按名字取函数，也能取到子模块）
        main_file = dir_form / "main.py"
        if not main_file.exists():
            # 目录在但缺 main.py —— 自动补模板，别让学生对着空目录发愣
            _make_multi_stub(p, dir_form)
            print()
            print("─" * 56)
            print(f"  这一课的作业是**两个文件**，我已经建好：{dir_form}")
            print("  打开 main.py 和 mytools.py，按里面的说明写，然后再跑一次这条命令。")
            print("─" * 56)
            print()
            return None
        try:
            # 让学生目录里的模块能互相 import
            if str(dir_form) not in sys.path:
                sys.path.insert(0, str(dir_form))
            return load_module(main_file, name=f"answer_{p.stem}")
        except Exception as e:                        # noqa: BLE001
            print(explain(e, filename=str(main_file)))
            return None

    if file_form.exists():
        try:
            return load_module(file_form, name=f"answer_{p.stem}")
        except Exception as e:                        # noqa: BLE001
            print(explain(e, filename=str(file_form)))
            return None

    # 都没有 → 生成模板
    file_form.parent.mkdir(parents=True, exist_ok=True)
    if _needs_multiple_files(p):
        dir_form.mkdir(parents=True, exist_ok=True)
        _make_multi_stub(p, dir_form)
        print()
        print("─" * 56)
        print(f"  这一课的作业是**两个文件**，已经帮你建好：{dir_form}")
        print("    main.py      主程序")
        print("    mytools.py   模块（要被 main 用 import 引进来）")
        print("  按里面的说明写，然后再跑一次这条命令。")
        print("─" * 56)
        print()
    else:
        file_form.write_text(_stub_for(p), encoding="utf-8")
        print()
        print("─" * 56)
        print(f"  给你建好了答题文件：{file_form}")
        print("─" * 56)
        print()
        # ⚠ 第一次建模板时**直接问要不要打开** —— 少一步，而且新手常常
        #   不知道"用什么打开"。但只在**交互终端**里问：脚本/测试里问会把流程卡住。
        if sys.stdin.isatty() and sys.stdout.isatty():
            try:
                ans = input("  现在打开内置编辑器写吗？[Y/n] ").strip().lower()
            except (EOFError, KeyboardInterrupt):
                ans = "n"
            if ans in ("", "y", "yes", "是"):
                from .editor import 编辑
                return 编辑(str(file_form)) or None
    return None


def _needs_multiple_files(lesson: Path) -> bool:
    """课程文件顶部写了 `# 需要多文件` 就是多文件作业。"""
    try:
        head = lesson.read_text(encoding="utf-8")[:600]
    except OSError:
        return False
    return "需要多文件" in head


def _make_multi_stub(lesson: Path, d: Path):
    """生成多文件作业的模板。

    模板要能直接运行（虽然会失败），而不是一堆空文件 ——
    新手对着空文件会僵住，而对着一句 `raise NotImplementedError("在这里写")`
    至少知道该动哪里。
    """
    (d / "mytools.py").write_text(
        '"""这一课要你写的**模块**。\n\n'
        '模块就是「一个 .py 文件，里面装一些函数，可以被别的文件 import」。\n'
        '把工具函数放在这里，main.py 用 import 把它拿过去用。\n\n'
        '要做：\n'
        '  1. 写一个函数 面积(长, 宽)，返回长×宽\n'
        '  2. 写一个函数 周长(长, 宽)，返回 2×(长+宽)\n'
        '  3. 写一个函数 是正方形(长, 宽)，长宽相等返回 True，否则 False\n'
        '  4. 写一个常量 单位 = "平方米"\n\n'
        f'写完运行：  {命令名()} 检查 02\n'
        '"""\n\n'
        '# 在这个文件里写你的函数。\n'
        '# 提示：模块里不需要写 input / print —— 那些是主程序的事。\n\n\n'
        'def 面积(长, 宽):\n'
        '    raise NotImplementedError("在 mytools.py 里写这个函数")\n\n\n'
        'def 周长(长, 宽):\n'
        '    raise NotImplementedError("在 mytools.py 里写这个函数")\n\n\n'
        'def 是正方形(长, 宽):\n'
        '    raise NotImplementedError("在 mytools.py 里写这个函数")\n\n\n'
        '单位 = ""\n',
        encoding="utf-8")

    (d / "main.py").write_text(
        '"""主程序 —— 它要**用 import** 把 mytools 里的东西拿过来用。\n\n'
        '这一课唯一要考的就是这件事：**两个文件之间能不能接上**。\n'
        '所以你不能把函数直接写在这个文件里 —— 检查会去找 mytools。\n\n'
        '要做：\n'
        '  1. 用 import 把 mytools 引进来\n'
        '  2. 写一个函数 描述(长, 宽)，返回一句话，例如：\n'
        '       10 × 5 的面积是 50 平方米，周长是 30\n'
        '     格式要完全一致（用 mytools 里的函数算，别自己重写）\n\n'
        f'写完运行：  {命令名()} 检查 02\n'
        '"""\n\n'
        '# ① 在这里 import 你的模块。提示：import mytools\n\n\n'
        'def 描述(长, 宽):\n'
        '    raise NotImplementedError("在 main.py 里写这个函数，记得用 mytools 里的函数")\n\n\n'
        'if __name__ == "__main__":\n'
        '    print(描述(10, 5))\n',
        encoding="utf-8")


def _stub_for(lesson: Path) -> str:
    """给这一课生成答题模板：把课程文件里要求的函数/变量列成 TODO。"""
    text = lesson.read_text(encoding="utf-8")
    lines = [
        f'"""第 {lesson.stem[:2]} 课的答案 —— 在这里写你的代码。',
        "",
        f"改完运行：  {命令名()} 检查 " + lesson.stem[:2],
        f"卡住了：    {命令名()} 答案 " + lesson.stem[:2] + "   （先自己试过再看）",
        '"""',
        "",
    ]
    # 从课程文件里抓 "要做：" 段落，当作 TODO 抄进来
    grab = False
    for line in text.splitlines():
        s = line.strip()
        if s.startswith("# 要做"):
            grab = True
            continue
        if grab:
            if s.startswith("#") and not s.startswith("# 要做"):
                lines.append("# " + s.lstrip("# ").rstrip())
            elif not s:
                lines.append("#")
            else:
                break
    lines.append("")
    return "\n".join(lines)


def _cmd_games() -> int:
    d = _root() / GAMES_DIR
    gs = sorted(d.glob("*.py")) if d.is_dir() else []
    print()
    print("════════════════════════════════════════════════════════")
    print("  小游戏")
    print("════════════════════════════════════════════════════════")
    if not gs:
        print("  （还没有游戏）")
        return 0
    for g in gs:
        print(f"  {g.stem}")
    print()
    print("  直接运行： python games/猜数字.py")
    print("  看完游戏可以打开源码 —— 它们是用前面几课的知识写的，读得懂。")
    print()
    return 0


def main(argv: list[str] | None = None) -> int:
    argv = list(sys.argv[1:] if argv is None else argv)
    if not argv:
        return _cmd_list()
    cmd = argv[0]
    if cmd in ("检查", "check"):
        return _cmd_check(argv[1]) if len(argv) > 1 else _cmd_list()
    if cmd in ("答案", "answer"):
        return _cmd_answer(argv[1]) if len(argv) > 1 else _cmd_list()
    if cmd in ("编辑", "edit"):
        return _cmd_edit(argv[1]) if len(argv) > 1 else _cmd_list()
    if cmd in ("游戏", "games"):
        return _cmd_games()
    if cmd in ("树莓派", "raspi"):
        from .raspi_course import cmd_raspi
        return cmd_raspi(_root(), argv[1:])
    if cmd in ("嵌入式", "embedded"):
        from .embedded import cmd_embedded
        return cmd_embedded(_root(), argv[1:])
    if cmd in ("AI", "ai"):
        from .aicourse import cmd_ai
        return cmd_ai(_root(), argv[1:])
    if cmd in ("考试", "exam"):
        from .cmd_exam import cmd_exam
        return cmd_exam(_root(), argv[1:])
    if cmd in ("帮助", "help", "-h", "--help"):
        return _cmd_list()
    return _cmd_show(cmd)
