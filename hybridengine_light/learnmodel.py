"""GUI 学习界面的**模型层** —— 课程发现 / 题目与答案读写 / 运行 / 检查。

## 为什么把逻辑放在这里、不写在界面里

界面的东西（控件、布局）**在无头环境里测不了**；
而"去哪找课程、答案路径怎么算、检查怎么调"这些**是能测的**。
把它们分出来，判据才能覆盖真正容易错的部分。

⚠ 这也是一条实测教训：我上一次修 "双击 exe 一闪而过" 时，
  判据只测了纯函数，结果漏掉了一条**只有打包后才走的路径**
  （`os` 没 import，因为 `and` 短路而从未执行）。
  ⇒ **能分成"可测的核心"就分出来。**

## 五条课程线怎么统一

它们的历史实现是**分开的**（Python 在 `run.py`，其余各有模块），
而且**只往屏幕上打印**，不返回字符串。这里做两件事把它们抹平：

  1. 用 `_截获输出()` 把打印内容收成字符串（界面要显示，不能直接印到终端）
  2. 统一成 `课` 这个 dataclass：编号 / 标题 / 轨道 / 源码 / 答题路径

⚠ 编译型四条线（C#/C++/Java/C）**要外部工具链**：
  本模块只负责"调 `dotnet run` 并收集输出"，**不假装能替学员装工具链**。
  装不上时给出**能照做的安装指引**（这是课程本来的做法，保留）。
"""
from __future__ import annotations

import contextlib
import io
import os
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

# ── 轨道定义 ─────────────────────────────────────────────────────────────
#: 轨道键 -> (显示名, 开箱即用?, 说明)
轨道 = {
    "python":   ("Python 基础", True,  "16 课，从变量到类 —— 零依赖，开箱即用"),
    # ⚠ 这两份是**完整演示程序**（没有 CHECK_，不能"检查答案"）——
    #   它们是"读代码"材料，不是练习。标签要说清楚，
    #   否则界面上会显示"小游戏 2 课"，看起来像 Python 只有 2 课。
    "games":    ("小游戏（读代码）", True, "两段完整代码，读懂它们就够了"),
    "ai":       ("人工智能入门", True,  "零依赖，算法全部自己实现"),
    "embedded": ("嵌入式/单片机", True, "单片机模拟器，不需要真板子"),
    "raspi":    ("树莓派",      True,  "树莓派模拟器，不需要真板子"),
    "enginecourse": ("引擎入门", True, "用组件拼游戏 —— 模块化编程"),
    "csharp":   ("C#",          False, "要 .NET SDK"),
    "cpp":      ("C++",         False, "要 .NET SDK + g++"),
    "java":     ("Java",        False, "要 .NET SDK + JDK"),
    "c":        ("C",           False, "要 .NET SDK + gcc"),
}


@dataclass
class 课:
    """一条课程线里的一课（界面只认这个）。"""
    轨道: str
    编号: str
    标题: str
    源文件: Path
    #: 学员的答题文件（可能还不存在 —— 第一次"检查"时才生成）
    答题文件: Path
    #: 编译型课程对应的 dotnet 工程；Python 系为 None
    工程: Path | None = None

    @property
    def 开箱即用(self) -> bool:
        return 轨道[self.轨道][1]

    @property
    def 自由练习(self) -> bool:
        """这一课是不是「自己试试」那个**空白页**（不是课程）。"""
        return self.编号 == 自由练习编号

    def 显示(self) -> str:
        #: ⚠ 自由练习**不显示编号** —— 它的编号是内部标记（`__自由__`），
        #:   吐到界面上就是 `__自由__  自由练习`，像没做完。
        if self.自由练习:
            return self.标题
        return f"{self.编号}  {self.标题}"


# ── 工具 ─────────────────────────────────────────────────────────────────

#: 「自己试试」那个空白页的编号。
#
#  ⚠ 它**不是课程** —— 这个编号不会出现在 `发现()` 的任何一条课程线里，
#    只是一个让界面认它的标记（见 `课.自由练习`）。
#    用 `"__自由__"` 而不是 `"00"` 之类：万一以后真有一课叫 00，
#    也不会撞上（撞上的后果是"自由练习"被当成第 00 课记进度）。
自由练习编号 = "__自由__"

#: 「自己试试」写到哪里。放在 `games/` 下 —— `gamekit.游戏说明()` 里
#: 给学员的指引就是"把它存成 games/我的游戏.py"，两边一致。
自由练习文件 = "games/我的游戏.py"


def 新建自由课() -> 课:
    """造一个「自己试试」用的**空课**（不是课程线里的一课）。

    ## 为什么要它

    作者原话：「**就是在自己尝试中来一个入口就行**」。

    主页原来只有两个入口（游戏编写 / 课程学习），两个都要求你先**选**点什么 ——
    而"我想随便写两句试试"这个最自然的动作**没有入口**。

    ## ⚠⚠ 两件必须守住的事

    **① 它不能污染课程体系的任何一处。** 具体：
       · `发现()` 不会返回它（它不是 `lessons/` 或任何课程目录里的文件）
       · 学习进度不记它（`进度.记()` 只被 `_标读完()` 调，而那个只在真课程上跑）
       · 轨道表不用为它加条目（借 `games` 那条 —— 它本来就有，
         而且 `_答案文件()` 对 `games` 返回 `k.答题文件`，正好是我们要的）

    **② 答题文件必须是"学员自己的文件"，不能是任何课程文件。** 具体：
       · `源文件` 和 `答题文件` **都指向同一个新文件** `games/我的游戏.py`
         （对自由练习来说"题目"就是"你的作品"）
       · ⚠ 绝对**不能**指向 `games/猜数字.py` 之类 —— 那会让"保存"覆盖成品。
    """
    根 = 项目根()
    #: ⚠ 源文件和答题文件**是同一个** —— 自由练习没有"题目"，
    #:   它就是学员自己的作品。指向两个不同文件反而会让"运行读哪个"变成坑。
    文件 = 根 / 自由练习文件
    return 课(
        轨道="games",
        编号=自由练习编号,
        标题="自由练习",
        源文件=文件,
        答题文件=文件,
    )

@contextlib.contextmanager
def _截获输出():
    """把 `print` 的内容收成字符串。

    ⚠ 为什么需要：各课程线的现成实现是**往屏幕打印**的（历史上是命令行工具）。
      界面要显示这些文字，不能让它直接印到终端去 ——
      而且 `sys.stdout` 在 GUI 里可能根本不存在（pythonw）。
    """
    缓 = io.StringIO()
    with contextlib.redirect_stdout(缓):
        yield 缓


def 项目根() -> Path:
    """课程内容根。⚠ 复用 `_paths.内容根()` —— 单一真值（打包后它指向 exe 旁边）。"""
    from ._paths import 内容根
    return 内容根()


def _第一行标题(p: Path) -> str:
    """从课程文件里取标题：第一行 `# 第 01 课：xxx` 或 `// 第 01 课：xxx`。"""
    try:
        for line in p.read_text(encoding="utf-8", errors="replace").split("\n")[:14]:
            s = line.strip().lstrip("#/ \t*-").strip()
            if s:
                return s[:70]
    except OSError:
        pass
    return p.stem


# ── 发现课程 ─────────────────────────────────────────────────────────────

#: 发现结果缓存（同一次运行内）。
#  ⚠ 为什么需要：编译型那四条线每条要跑一次 `dotnet run`（~1.5 秒），
#    而界面会在"选课""刷新"等场合反复问同一条线。
#    实测：不缓存的话启动一次要 6.4 秒。
_发现缓存: dict[str, list[课]] = {}
#: 缓存对应的**内容根**。
#  ⚠⚠ 实测踩到：缓存只按"轨道名"存，而内容根是会变的
#    （测试用临时目录、打包后指向 exe 旁边）。
#    ⇒ 换了根之后缓存仍返回**上一个根**的课，
#      而那个目录可能已被删除 ⇒ `FileNotFoundError`。
#    **缓存必须和它依赖的输入绑定** —— 否则它就不是缓存，是"记错了"。
_缓存根: str | None = None

#: 编译型轨道 —— 要跑 `dotnet run`，**慢**
编译型轨道 = ("csharp", "cpp", "java", "c")
#: 即时轨道 —— 纯读文件，实测 0.00 秒
即时轨道 = ("python", "games", "ai", "embedded", "raspi", "enginecourse")


def 清缓存() -> None:
    global _缓存根
    _发现缓存.clear()
    _缓存根 = None


def _缓存可用() -> bool:
    """当前根是否与缓存一致；不一致就清空重来。"""
    global _缓存根
    现在 = str(项目根())
    if _缓存根 != 现在:
        _发现缓存.clear()
        _缓存根 = 现在
        return False
    return True


def 发现(轨道键: str) -> list[课]:
    """列出某条课程线的所有课。找不到就返回空列表（**不抛异常**）。"""
    新鲜 = _缓存可用()
    if 新鲜 and 轨道键 in _发现缓存:
        return _发现缓存[轨道键]
    root = 项目根()
    if 轨道键 == "python":
        from . import run as R
        出 = []
        for p in R._lessons():
            出.append(课("python", p.stem[:2], R._title_of(p), p, R._答题路径(p)))
    elif 轨道键 == "enginecourse":
        出 = _发现引擎课程(root)
    elif 轨道键 in ("ai", "embedded", "raspi", "games"):
        出 = _发现Python系(轨道键, root)
    elif 轨道键 in 编译型轨道:
        出 = _发现编译型(轨道键, root)
    else:
        出 = []
    _发现缓存[轨道键] = 出
    return 出


def _发现引擎课程(root: Path) -> list[课]:
    """引擎课程线（`engine/` + `engine-answers/`）。

    ⚠ 它的答题文件在 `engine-answers/`，**不是**课程旁边 ——
      所以不能走 `_发现Python系` 那条路（那条把答案当成课程文件自己）。
    """
    from . import enginecourse as EC
    出 = []
    try:
        for p in EC._课程们(root):
            编号 = p.stem[:2]
            出.append(课("enginecourse", 编号, EC._标题(p), p,
                        root / EC.答案目录名 / p.name))
    except Exception:
        return []
    return 出


def _发现Python系(轨道键: str, root: Path) -> list[课]:
    """AI / 嵌入式 / 树莓派 / 小游戏 —— 它们的模块各有 `_课程们(root)`。"""
    if 轨道键 == "games":
        d = root / "games"
        if not d.is_dir():
            return []
        # ⚠ 小游戏是**给人读的成品**，没有"答题"这回事。
        #   但它同样**不能**把答题文件指成课程文件自己 ——
        #   一旦有人（或以后的代码）真把它当答题文件用，
        #   就会覆盖掉游戏源码。指向一个同名的新文件至少是安全的。
        #   （界面上游戏那条线本来也不给做题。）
        return [课("games", f.stem[:2], f.stem, f,
                   root / "games-answers" / f.name,
                   None) for f in sorted(d.glob("*.py"))]

    模块 = {"ai": "aicourse", "embedded": "embedded",
            "raspi": "raspi_course"}[轨道键]
    m = __import__(f"hybridengine_light.{模块}", fromlist=["x"])
    出 = []
    try:
        for p in m._课程们(root):
            编号 = p.stem[:2]
            # ⚠⚠ **答题文件必须在 `<线>-answers/` 下，不能是课程文件自己。**
            #
            #   原来这里写的是 `课(轨道键, 编号, 标题, p, p)` ——
            #   源文件和答题文件**指向同一个文件**。后果很严重：
            #   GUI 里打开这三条线，右边"答案"框里显示的其实是**课程本身**，
            #   学员一保存就把课程内容覆盖掉了（而且他不知道自己毁了什么）。
            #
            #   这是子代理实测发现的，我自己一直没看出来 ——
            #   因为 CLI（`python -m hybridengine_light AI 01`）走的是
            #   各模块自己的 `_检查()`，那条路一直是**对**的：
            #       aicourse.py:     答案目录 = root / "ai-answers"
            #       embedded.py:     答案目录 = root / "embedded-answers"
            #       raspi_course.py: 答案目录 = root / "raspi-answers"
            #   ⇒ 同一个概念在 CLI 和 GUI 里算出了两个答案，而**看起来都对**。
            #     **两处实现"看起来一样"不等于它们一致 —— 要么共用，
            #     要么就写一条判据把两边对比起来。**
            出.append(课(轨道键, 编号, m._标题(p), p,
                         root / (轨道键 + "-answers") / p.name))
    except Exception:
        # ⚠ 课程线坏了不该让整个界面打不开 —— 界面要能显示其它线
        return []
    return 出


#: 课程特性（C# 侧用 `[课程]`，Java 侧用 `[Java课程]`）
_课程特性 = re.compile(r'\[\s*\w*课程\s*\(')


def _读CSharp字符串(文: str, i: int) -> tuple[str, int]:
    """从 `文[i] == '"'` 读一个 C# 字符串字面量，返回 (值, 下一位置)。

    ⚠⚠ 这里**不能**用 `"([^"]+)"` 这种正则 —— C# 的引号可以**转义**：

           [课程("07", "第 07 课：字符串 —— 'A' 和 \\"A\\" 不是一回事")]

       正则遇到第一个 `\\"` 就断了，C++ 第 07 课实际取到的是
           "第 07 课：字符串 —— 'A' 和 \\"
       —— 标题被截断，而**不会报错**，界面上就显示一个残缺的课名。
    """
    出: list[str] = []
    i += 1                                          # 跳过开头的 "
    while i < len(文):
        c = 文[i]
        if c == "\\" and i + 1 < len(文):
            n = 文[i + 1]
            出.append({"n": "\n", "t": "\t", "r": "\r",
                       '"': '"', "\\": "\\"}.get(n, n))
            i += 2
            continue
        if c == '"':
            return "".join(出), i + 1
        出.append(c)
        i += 1
    return "".join(出), i


def _解析课程文件(文: str) -> tuple[str, str] | None:
    """从一个 `.cs` 课程文件里取出 `[课程("01", "标题", "要做")]` 的前两项。"""
    m = _课程特性.search(文)
    if not m:
        return None
    i = m.end()
    值: list[str] = []
    for _ in range(2):                              # 只要 编号 和 标题
        while i < len(文) and 文[i] in " \t\r\n":
            i += 1
        if i >= len(文) or 文[i] != '"':
            return None
        s, i = _读CSharp字符串(文, i)
        值.append(s)
        while i < len(文) and 文[i] in " \t\r\n":
            i += 1
        if i < len(文) and 文[i] == ",":
            i += 1
    return (值[0], 值[1])


def _直接读课程表(工程: Path) -> list[tuple[str, str]]:
    """**直接读** `Lessons/*.cs` 解析课程表 —— 不启动 `dotnet`。

    ⚠ 为什么值得这么做（实测数据）：

        办法            4 条线合计      单条
        dotnet 子进程    2.86 秒        0.66 秒
        直接读文件       0.008 秒       0.002 秒

      `.NET` 那 0.66 秒几乎全是 **CLI 自身启动**，跟课程多少无关。
      代价是界面第一眼"编译型语言那里是空的/载入中…"—— 而 `dotnet` 慢的话
      要等两三秒，作者看到的就是这个「只有 Python，别的没有」。

    ⚠ 与 `dotnet run` 的结果**逐条核对过**，四条线完全一致
      （含 C++ 第 07 课标题里带转义引号那种；见 `_读CSharp字符串` 的注释）。
      取不到任何课就返回空 —— 调用方会退回子进程那条路。
    """
    d = 工程 / "Lessons"
    if not d.is_dir():
        return []
    出: list[tuple[str, str]] = []
    for f in sorted(d.glob("*.cs")):
        try:
            项 = _解析课程文件(f.read_text(encoding="utf-8"))
        except OSError:
            continue
        if 项:
            出.append(项)
    return 出


def _发现编译型(轨道键: str, root: Path) -> list[课]:
    """C# / C++ / Java / C —— 课程定义是 .cs，答题文件在 `<线>-answers/`。

    ⚠ **先直接读文件，读不到才退回子进程**：
      子进程（`dotnet run`）每条线要 0.66 秒，四条线近 3 秒，
      界面第一眼就是"载入中…"。直接读 0.002 秒。
      但子进程那条路**保留着**：万一以后课程文件换了组织形式，
      直接读拿不到，还能靠它兜底（宁可慢，不能空）。
    """
    # ⚠ C# 用的是 `cs-lessons`（8 课真课程）。
    #   原来指向 `lessons` —— 那是个**演示程序**（只有 Program.cs，
    #   没有 Lessons/ 目录）⇒ 界面里 C# 显示 0 课。
    #   作者反馈"python与c#的课程不全"时，C# 这边就是这个问题。
    工程名 = {"csharp": "cs-lessons", "cpp": "cpp-lessons",
              "java": "java-lessons", "c": "c-lessons"}[轨道键]
    工程 = root / "csharp" / 工程名
    # ⚠ 判据用 `Program.cs`：它是**运行器**，每门编译型课都有。
    #   不要用 Lessons/ 目录 —— 有的线的课程文件组织形式不同。
    if not (工程 / "Program.cs").is_file():
        return []
    # ⚠ 答案目录名要与各课课程库里的常量一致：
    #   CsCourse.答题目录 / CppCourse / JavaCourse / CCourse
    答案名 = {"csharp": "cs-answers", "cpp": "cpp-answers",
              "java": "java-answers", "c": "c-answers"}[轨道键]

    表 = _直接读课程表(工程)
    if not 表:
        # 直接读不到 ⇒ 退回子进程（慢但可靠）
        表 = []
        for 行 in _运行课程程序(工程, [])[1].split("\n"):
            s = 行.strip()
            # 形如：   01   第 01 课：输出 —— 让程序开口说话   （5 项检查）
            if len(s) > 6 and s[:2].isdigit() and "  " in s:
                标题 = s[4:].split("（")[0].strip()
                if 标题:
                    表.append((s[:2], 标题))
    return [课(轨道键, 编号, 标题, 工程, root / 答案名, 工程)
            for 编号, 标题 in 表]


# ── 调用编译型课程程序 ───────────────────────────────────────────────────

#: 工具名 -> 可执行文件候选名（Windows 上带 .exe）
_工具候选 = {
    "dotnet": ("dotnet", "dotnet.exe"),
    "g++": ("g++", "g++.exe", "clang++", "clang++.exe", "c++"),
    "gcc": ("gcc", "gcc.exe", "clang", "clang.exe"),
    "javac": ("javac", "javac.exe"),
    "java": ("java", "java.exe"),
}

_工具缓存: dict[str, str | None] = {}


def 便携工具目录() -> list[Path]:
    """可能放着**便携工具链**的目录（按优先级）。

    ⚠ 为什么要这个：`.NET SDK` 4GB、MinGW 914MB、JDK 373MB ——
      **装不进 11MB 的发布包**（物理限制，不是偷懒）。
      所以留一个"把工具链放这儿就自动认"的位置：

        <exe 旁边>\\tools\\         ← 打包产物旁边的 tools/
        <项目根>\\tools\\           ← 源码仓库里的 tools/

      放了就用，没放就退回 PATH 上的系统工具 —— **两边都不影响**。
      这样"能自带的"都自带了，只有 GB 级的必须外部安装。
    """
    出: list[Path] = []
    try:
        出.append(项目根() / "tools")
    except Exception:
        pass
    try:
        import sys as _sys
        if getattr(_sys, "frozen", False):
            出.append(Path(_sys.executable).resolve().parent / "tools")
    except Exception:
        pass
    return 出


def _找工具(名: str) -> str | None:
    """找一个工具：**先看便携目录，再退回 PATH**。返回可执行路径（None = 没有）。"""
    if 名 in _工具缓存:
        return _工具缓存[名]
    候选 = _工具候选.get(名, (名,))
    结果: str | None = None
    for 根 in 便携工具目录():
        if not 根.is_dir():
            continue
        for 子 in (None, "bin", 名):
            基准 = 根 if 子 is None else 根 / 子
            for c in 候选:
                p = 基准 / c
                if p.is_file():
                    # ⚠ 必须**带扩展名**才能被 CreateProcess 找到；
                    #   没有 .exe 的话优先补上。
                    结果 = str(p)
                    break
            if 结果:
                break
        if 结果:
            break
        # 再扫一层（tools/*/bin/<c>）—— 便携包目录名五花八门
        try:
            for 子目录 in 根.iterdir():
                if not 子目录.is_dir():
                    continue
                for c in 候选:
                    p = 子目录 / "bin" / c
                    if p.is_file():
                        结果 = str(p)
                        break
                if 结果:
                    break
        except OSError:
            pass
        if 结果:
            break
    if not 结果:
        结果 = shutil.which(名)
    _工具缓存[名] = 结果
    return 结果


def 有dotnet() -> bool:
    return _找工具("dotnet") is not None


def _运行课程程序(工程: Path, 参数: list[str], 超时: int = 300) -> tuple[int, str]:
    """跑 `dotnet run --project <工程> <参数>`，返回 (退出码, 输出文字)。

    ⚠ 优先用**预构建**（`--no-build`）：
      打包时已经构建过了 ⇒ 首次跑就是瞬间，而且**不需要联网**（不用 NuGet 还原）。
      没预构建过（比如从源码跑）就退回普通构建。
    """
    dotnet = _找工具("dotnet")
    if dotnet is None:
        return 127, (
            "没有找到 .NET SDK。\n\n"
            "这几门课程要把你写的代码**真的编译运行**才能判对错，所以需要它：\n\n"
            "  Windows  装 .NET SDK 8（推荐 winget）：\n"
            "             winget install Microsoft.DotNet.SDK.8\n"
            "           或者去 https://dotnet.microsoft.com/download 下\n\n"
            "  ⚠ 要的是 **SDK**，不是 Runtime —— Runtime 里没有编译器。\n\n"
            "装完**重开一个窗口**（让 PATH 生效）再试。\n"
            "（Python 那几条线不需要它，现在就能用。）\n\n"
            "💡 也可以把工具链放进 exe 旁边的 `tools\\` 目录（便携方式），程序会自动认。")

    已构建 = (工程 / "bin").is_dir()
    基础 = [dotnet, "run", "--project", str(工程)]
    if 已构建:
        基础.append("--no-build")
    try:
        r = subprocess.run([*基础, *参数], capture_output=True, timeout=超时,
                           cwd=str(工程.parent.parent))
        # ⚠ 预构建的 bin 与源码不匹配时 `--no-build` 会失败
        #   （比如改过课程文件）⇒ 退回普通构建，别让学员卡在一个假错误上。
        if r.returncode != 0 and 已构建:
            r = subprocess.run([dotnet, "run", "--project", str(工程), *参数],
                               capture_output=True, timeout=超时,
                               cwd=str(工程.parent.parent))
        文 = (r.stdout + r.stderr).decode("utf-8", "replace")
        return r.returncode, 文
    except subprocess.TimeoutExpired:
        return 124, f"超时（超过 {超时} 秒）—— 是不是有死循环？"
    except OSError as e:
        return 1, f"启动 dotnet 失败：{e}"


# ── 读课程正文 / 答案 ────────────────────────────────────────────────────

def _把命令行写对(文: str) -> str:
    """把"看课"正文里写死的 `python -m hybridengine_light …` 换成真正的命令名。

    ⚠⚠ 为什么需要这个（同一个坑的第二次）：
      **打包成 exe 之后没有 `python` 命令。** 课程正文里写
          python -m hybridengine_light 树莓派 检查 01
      学员照着敲会得到"不是内部或外部命令" —— 而错的其实是**课程**。

      项目里已经栽过一次（各课程线提示漏了 11 处），`_paths.命令名()` 就是
      为它准备的。这次是课程**正文**里的 21 处（ai 10 / embedded 4 /
      raspi 5 / engine 2）。

    ⚠ 为什么不直接改那 21 行课程文件：
      课程文件是**源码仓库里的文档**，从源码跑的时候 `python -m hybridengine_light`
      **本来就是对的**（开发者就是这么跑）。写死了 exe 的名字反而在源码环境下错。
      ⇒ 正确做法是**在显示的那一刻**按当前环境替换，而不是把正文写死成任一种。
        这和 `run._stub_for` 里用 `命令名()` 是同一个思路。
    """
    import re
    from ._paths import 命令名
    return re.sub(r"python\s+-m\s+hybridengine_light",
                  命令名().replace("\\", "\\\\"), 文)


def 看课(k: 课) -> str:
    """这一课的正文（要做什么）。"""
    return _把命令行写对(_看课原文(k))


def _看课原文(k: 课) -> str:
    """看课的原始正文 —— 命令行还没被替换成当前环境的那一种。"""
    if k.轨道 in ("csharp", "cpp", "java", "c") and k.工程:
        return _运行课程程序(k.工程, [k.编号])[1] or "（这一课没有正文）"
    if k.轨道 == "enginecourse":
        from . import enginecourse as EC
        with _截获输出() as 缓:
            EC._看课程(项目根(), k.编号)
        return 缓.getvalue()
    if k.轨道 == "ai":
        from . import aicourse
        with _截获输出() as 缓:
            aicourse._看课程(项目根(), k.编号)
        return 缓.getvalue()
    if k.轨道 == "embedded":
        from . import embedded
        with _截获输出() as 缓:
            embedded._看课程(项目根(), k.编号)
        return 缓.getvalue()
    if k.轨道 == "raspi":
        from . import raspi_course
        with _截获输出() as 缓:
            raspi_course._看课程(项目根(), k.编号)
        return 缓.getvalue()
    try:
        return k.源文件.read_text(encoding="utf-8", errors="replace")
    except OSError as e:
        return f"（读不到这一课：{e}）"


def 读答案(k: 课) -> str:
    """学员当前的代码。不存在就生成模板（**与命令行同一条路径**）。"""
    文件 = _答案文件(k)
    if 文件.is_file():
        try:
            return 文件.read_text(encoding="utf-8", errors="replace")
        except OSError:
            return ""
    _确保模板(k)
    try:
        return 文件.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return ""


def _答案文件(k: 课) -> Path:
    """答题文件路径。

    ⚠ Python 线必须复用 `run._答题路径()` —— 它与「检查」用的是**同一个**真值。
      自己拼一遍的话，学员会写到一个文件、检查另一个文件
      （表现为"我明明写了却说过不了"，是最打击人的一类问题）。
    """
    if k.轨道 == "python":
        from . import run as R
        return R._答题路径(k.源文件)
    if k.轨道 == "enginecourse":
        return k.答题文件      # 已经算好了（engine-answers/ 下）
    if k.轨道 in ("ai", "embedded", "raspi", "games"):
        # ⚠⚠ 这里原来写的是 `return k.源文件` —— **把课程文件当成了答题文件**。
        #   后果：GUI 里"答案"框显示的是课程本身，学员一保存就覆盖课程。
        #   现在改成和 CLI 各模块一致的 `<线>-answers/<课程文件名>`
        #   （`_发现Python系` 已经把 k.答题文件 算对了，直接用它）。
        #
        #   ⚠ `games` 是个例外：小游戏是**给人读的成品**，没有答题这回事。
        #     但它同样不该返回课程文件 —— 真被当成答题文件时，
        #     返回一个同名的 answers 路径至少是"写到一个新文件"，
        #     不会破坏源码。界面上游戏那条线本来也不给做题。
        return k.答题文件
    if k.轨道 in ("csharp", "cpp", "java", "c") and k.答题文件:
        # 编译型的答题文件名由 C# 侧算（含标题清理规则）⇒ 问它要，别猜
        return _问答题路径(k)
    return k.答题文件


_答题路径缓存: dict[tuple[str, str], Path] = {}


def _问答题路径(k: 课) -> Path:
    """问 C# 课程程序要答题文件路径。

    ⚠ 为什么不能自己拼：文件名由标题清理规则决定（去除非字母数字），
      而标题里有中文标点、引号、全角符号 —— 我上一轮就在这里错了两次
      （猜的文件名与真实文件名不符 ⇒ 检查找不到文件）。
      ⇒ **问代码，别猜。**
    """
    键 = (k.轨道, k.编号)
    if 键 in _答题路径缓存:
        return _答题路径缓存[键]
    root = 项目根()
    答案名 = {"csharp": "cs-answers", "cpp": "cpp-answers",
              "java": "java-answers", "c": "c-answers"}[k.轨道]
    #: ⚠⚠⚠ 2026-10-14 修：这里**原来真的只是"猜"** ——
    #:   函数名叫「问答题路径」、注释写着「**问代码，别猜**」，
    #:   但代码是用**整个标题**拼的，而且后缀**写死 `.cs` / `.c`**：
    #:
    #:       净 = "".join(c for c in k.标题 if c.isalnum())
    #:       猜 = root / 答案名 / f"{k.编号}_{净}.cs"
    #:
    #:   而 C# 侧（`CppCourse.答案路径`，**权威**）算的是：
    #:       `{编号}_{清理(定义名(标题))}.cpp`
    #:       定义名 = 标题里 `：` 之后、`—` 之前那一段
    #:       清理   = 只留字母数字
    #:
    #:   ⇒ 两侧**永远对不上**：
    #:       C# 侧     `cpp-answers/05_函数.cpp`
    #:       Python 侧 `cpp-answers/05_第05课函数把计算打包想用几次想用几次.cs`
    #:
    #:   ⚠ 后果（实测，`_verify_reference_all.py`）：**`cpp` 线 41 课里 40 课
    #:     的参考答案"过不了自己的检查"** —— 因为
    #:     ① `保存答案` 写的文件，课程程序**根本不看**；
    #:     ② 课程程序检查的是**上次留下的 `05_函数.cpp`**（内容还是教学模板，
    #:        里面只有一句 `int 双倍(int n)` 声明）⇒ 报「没有 return」。
    #:   ⚠ 这也解释了 `cpp-answers/` 里 82 个文件的命名为什么和 C++ 侧对不上。
    #:
    #:   ⇒ 修法：**照抄 C# 的规则**（`定义名` = 冒号后、破折号前；`清理` = 只留字母数字），
    #:     后缀按线取（cpp→`.cpp`，csharp/java→`.cs`，c→`.c`）。
    #:   ⚠ 两条路（`_问答题路径` 与 C# 侧）**必须逐字一致** ——
    #:     不一致的代价就是"学员写了却说过不了"，是最打击人的一类问题。
    #: ⚠⚠⚠ **只有 `cpp` 那一层有 `定义名`** —— 我第一版把它错用到四条线上，
    #:   结果 `c` 线从「40 课过」变成「0 课过」。
    #:
    #:   实测三条课程库的规则**不一样**：
    #:
    #:       CCourse.cs:32    $"{编号}_{清理(def.标题)}.c"              ← 整个标题
    #:       CsCourse.cs:25   $"{编号}_{清理(def.标题)}.cs"             ← 整个标题
    #:       CppCourse.cs:121 $"{编号}_{清理(定义名(def.标题))}.cpp"    ← **多一层 定义名**
    #:
    #:   （`定义名` = 标题里 `：` 之后、`—` 之前那一段，所以 cpp 的
    #:     「第 05 课：函数 —— 把计算打包…」⇒ `函数` ⇒ `05_函数.cpp`）
    #:
    #:   ⇒ **Java 没有自己的课程库文件**（`HybridEngineLight/` 下只有
    #:     `CCourse / CppCourse / CsCourse` 三个）—— 它大概复用 `CsCourse`。
    #:     ⚠ **没查证**，先按 `CsCourse` 处理（后缀仍是 `.cs`）。
    #: ⚠⚠⚠ 2026-10-14 第二次修：**`java` 和 `cpp` 一样，也走 `定义名` + `.java`**。
    #:
    #:   我第一版按 `CsCourse` 处理 java（因为 `HybridEngineLight/` 下
    #:   只有 `CCourse / CppCourse / CsCourse` 三个课程库文件，**没找到 JavaCourse**）。
    #:   ⇒ 实测 `java-answers/` 里**两种命名并存**：
    #:
    #:      00_第00课跑通第一个程序把JDK装好.cs     ← Python 写的（错的）
    #:      00_跑通第一个程序.java                ← **Java 侧真正读的**
    #:
    #:   ⇒ 判据结果：`java` 线 41 课里 **40 课参考答案过不了自己的检查**。
    #:   ⚠ 这跟 `cpp` 是**同一个根因**（文件名两侧不一致），不是两个毛病。
    #:
    #:   ⇒ 规则汇总（实测确认的三条）：
    #:       cpp   → `定义名` + `.cpp`
    #:       java  → `定义名` + `.java`
    #:       c     → 整个标题 + `.c`
    #:       csharp→ 整个标题 + `.cs`
    #:   ⚠ `java` 的答题目录名也是**从盘上确认**的（`java-answers/`），
    #:     不是从 `答案名` 猜的 —— 见下面那条注释。
    走定义名 = k.轨道 in ("cpp", "java")
    主体 = _定义名(k.标题) if 走定义名 else _清理文件名(k.标题)
    _答题路径缓存[键] = root / 答案名 / (k.编号 + "_" + 主体 + _后缀(k.轨道))
    return _答题路径缓存[键]


def _后缀(轨道: str) -> str:
    """答题文件的扩展名。⚠ 与各课课程库里的 `答案路径` 一致。"""
    #: ⚠ `java` 是 **`.java`**（我第一版给的是 `.cs` —— 那是 `CsCourse` 的后缀，
    #:   而 java 线并不复用 `CsCourse`；实测盘上 Java 侧读的是 `.java` 文件）。
    return {"cpp": ".cpp", "csharp": ".cs", "java": ".java", "c": ".c"}[轨道]


def _定义名(标题: str) -> str:
    """从标题里取出「清爽的文件名主体」。

    ⚠ **逐字照抄 C# 的 `CppCourse.定义名`** —— 两侧不一致就会各写各的文件。

        标题形如「第 01 课：输出 —— 让程序开口说话」
        ⇒ 取冒号后、破折号前那一段 ⇒ 「输出」
    """
    i = 标题.find("：")
    s = 标题[i + 1:] if i > 0 else 标题
    j = s.find("—")
    if j > 0:
        s = s[:j]
    return _清理文件名(s.strip())


def _清理文件名(s: str) -> str:
    """只留字母和数字。⚠ 与 C# 的 `CppCourse.清理` 一致（`char.IsLetterOrDigit`）。"""
    净 = "".join(c for c in s if c.isalnum())
    return 净 or "作业"


def _确保模板(k: 课) -> None:
    """生成答题模板（第一次打开时）。"""
    文件 = _答案文件(k)
    if 文件.is_file():
        return
    文件.parent.mkdir(parents=True, exist_ok=True)
    if k.轨道 == "python":
        from . import run as R
        文件.write_text(R._stub_for(k.源文件), encoding="utf-8")
    elif k.轨道 in ("ai", "embedded", "raspi", "enginecourse"):
        # ⚠⚠ 这里原来只有 ("ai", "embedded", "raspi") —— **漏了 enginecourse**。
        #   而 `enginecourse.py` 里明明有 `_生成模板`，只是没人调它。
        #
        #   症状：引擎线的答题文件**永远不会生成**。
        #     目录 `engine-answers/` 存在却是空的；`读答案()` 走到
        #     "文件不存在 → _确保模板 → 再读" 这条路上，
        #     `_确保模板` 静默什么都不做，于是再读还是 OSError → **返回空字符串**。
        #   GUI 里就是"答案框一片空白"，而且**不报错** —— 最难查的那一类。
        #
        #   ⚠ 为什么之前一直没发现：`.` 这条线的答题文件在仓库里是**不存在的**
        #     （其他线的 -answers/ 里有一些旧的、能看出内容），
        #     所以"空"看起来像"还没生成"，而不是"生成坏了"。
        #   ⇒ 判据：**分派链要覆盖所有轨道**。加一条线时，除了 `轨道` 表，
        #     还要回来 grep 一遍所有 `k.轨道 in (...)` 的地方。
        模块 = {"ai": "aicourse", "embedded": "embedded",
                "raspi": "raspi_course", "enginecourse": "enginecourse"}[k.轨道]
        m = __import__(f"hybridengine_light.{模块}", fromlist=["x"])
        文件.write_text(m._生成模板(k.源文件), encoding="utf-8")


def 保存答案(k: 课, 代码: str) -> str:
    """写回答题文件。返回给学员看的一句话。"""
    文件 = _答案文件(k)
    文件.parent.mkdir(parents=True, exist_ok=True)
    try:
        文件.write_text(代码, encoding="utf-8", newline="\n")
    except OSError as e:
        return f"保存失败：{e}"
    return f"已保存：{文件.name}"


# ── 运行 / 检查 ──────────────────────────────────────────────────────────

def 运行(k: 课, 代码: str) -> str:
    """跑学员的代码，返回输出文字。"""
    if k.轨道 in ("csharp", "cpp", "java", "c") and k.工程:
        保存答案(k, 代码)
        return _运行课程程序(k.工程, ["检查", k.编号])[1]
    if k.轨道 == "python":
        保存答案(k, 代码)
        return _跑Python答案(k)
    保存答案(k, 代码)
    return _跑Python答案(k)


def _跑Python答案(k: 课) -> str:
    """在**子进程**里跑学员的答案。

    ⚠ 必须开子进程：学员的代码可能有死循环、可能 `sys.exit()`、
      可能把 `sys.stdout` 弄坏 —— 在界面进程里跑会把整个界面搞死。
      子进程死了只是这一格显示报错。
    """
    文件 = _答案文件(k)
    目标 = 文件 / "main.py" if 文件.is_dir() else 文件
    if not 目标.is_file():
        return "（还没有答题文件）"
    cmd = [sys.executable, str(目标)]
    try:
        r = subprocess.run(cmd, capture_output=True, timeout=20,
                           cwd=str(目标.parent),
                           env={**os.environ, "PYTHONIOENCODING": "utf-8"})
        出 = (r.stdout + r.stderr).decode("utf-8", "replace")
        if r.returncode != 0:
            出 += f"\n\n（退出码 {r.returncode}）"
        return 出 or "（程序没有任何输出）"
    except subprocess.TimeoutExpired:
        return "运行超过 20 秒 —— 是不是有死循环？"
    except OSError as e:
        return f"启动失败：{e}"


def 检查(k: 课, 代码: str) -> str:
    """检查答案，返回给学员看的报告。"""
    if k.轨道 in ("csharp", "cpp", "java", "c") and k.工程:
        保存答案(k, 代码)
        return _运行课程程序(k.工程, ["检查", k.编号])[1]
    保存答案(k, 代码)
    if k.轨道 == "python":
        from . import run as R
        with _截获输出() as 缓:
            R._cmd_check(k.编号)
        return 缓.getvalue()
    if k.轨道 == "enginecourse":
        # ⚠ 引擎课程线有自己的模块（`enginecourse`），而且它的答题文件在
        #   `engine-answers/`（不是课程旁边）⇒ 不能并进下面那条 Python 系分支。
        from . import enginecourse as EC
        with _截获输出() as 缓:
            EC._检查(项目根(), k.编号)
        return 缓.getvalue()
    if k.轨道 in ("ai", "embedded", "raspi"):
        模块 = {"ai": "aicourse", "embedded": "embedded", "raspi": "raspi_course"}[k.轨道]
        m = __import__(f"hybridengine_light.{模块}", fromlist=["x"])
        with _截获输出() as 缓:
            m._检查(项目根(), k.编号)
        return 缓.getvalue()
    return _跑Python答案(k)



#: 参考答案里的**装饰框线**（课程程序的 `答案` 子命令在代码前面加的展示用抬头）。
#
#  ⚠⚠ **为什么必须剥掉它**（2026-10-14 实测）：
#
#     `c` / `csharp` 两条线的 `答案` 输出长这样：
#
#         ── 参考答案 ──────────────────────────────────
#           （先自己写一遍再看。抄一遍不如写错一遍学得多。）
#         ────────────────────────────────────────────────
#         static void 跑通()
#         {
#             Console.WriteLine("工具链就绪");
#         }
#
#     ⚠ 而 `参考答案()` 的返回值**同时被当作"可以直接提交的代码"用**
#       （`M.检查(课, M.参考答案(课))` —— 这正是"参考答案全过"该有的判据）。
#     ⇒ 那段**带框线的文字不是合法代码**，编译器直接报：
#
#         main.c:2:1: error: stray '\342' in program
#         （`\342` 就是 `─`（U+2500）的 UTF-8 首字节）
#
#     ⇒ 表现是「**参考答案自己过不了自己的检查**」：
#         c 第 00 课 4/5 · csharp 第 00 课 2/5（cpp / java 恰好没暴露）
#
#  ⚠ 而这个问题**一直存在**（我回退到 git HEAD 也是一样）——
#    因为 **"参考答案全过"这条判据在编译型四条线上从来没被跑过**。
#    几条线各有各的反面验证器（从课件源码里的错例表取代码），
#    那条路**绕过**了 `参考答案()` ⇒ 谁都没发现。
装饰框线 = "─"

def 剥装饰(文本: str) -> str:
    """把课程程序加在参考答案前面的**展示抬头**剥掉，只留代码。

    ⚠ 规则：**最后一个"整行都是框线"的行之后就是代码。**
      · 取**最后**一个而不取第一个 —— 抬头里可能有多条框线
      · ⚠ **只有真的找到框线行才剥**；找不到就**原样返回**
        （绝不误伤那些本来就没有抬头的答案）
      · 剥不出东西时（比如抬头后面空了）也**原样返回** ——
        宁可让判据报错，也不返回空串让下游静默失败
    """
    行 = 文本.split("\n")
    最后 = -1
    for i, l in enumerate(行):
        #: 去掉 \r 和空白后，整行只由框线字符组成，且至少两个 —— 这样不会把
        #: 代码里恰好以 `─` 开头的一行（注释里可能有）误判成抬头。
        s = l.strip().strip("\r").strip()
        if len(s) >= 2 and s.strip(装饰框线) == "":
            最后 = i
    if 最后 < 0:
        return 文本
    剩 = "\n".join(行[最后 + 1:])
    return 剩 if 剩.strip() else 文本



def _剥模块文档串(文本: str) -> str:
    """把**模块开头的那个 docstring** 删掉，只留可跑的代码。

    ⚠⚠ **为什么必须剥**（2026-10-14 实测）：

      python 线的参考答案长这样（`lessons/solutions/00_….py`）：

          '''参考答案 —— 第 00 课：跑通第一个程序。

          ⚠ 这个文件放在 `lessons/solutions/`，是**只读的课程作者提供**的答案。
            学生自己写的那份在 `lessons/answers/` …
          '''
          print("工具链就绪")
          print("我准备好学 Python 了")
          print("第一步：写，第二步：跑")

      ⚠ 那段 docstring 是**给读者看的说明**，里面有**全角括号**（`（` `）`）。
      而第 00 课有一条检查叫「**用了半角括号和引号**」——
      它扫**整个文件**，于是**参考答案踩了自己的检查**：

          × 用了半角括号和引号
              你的文件里有全角标点：全角左括号、全角右括号。

      ⇒ `M.检查(课, M.参考答案(课))` 对**每一课 python 都不过**。

    ⚠ 为什么这个 bug 活了这么久：判据脚本当时数的是 `✗`，
      而 python 线用的是 `√`/`×` ⇒ **恒等于 0** ⇒ 永远报"过"（假绿）。
      队友 `learning-verify` 查出来后，真相才露出来。

    ⚠ 只剥**模块开头那一个** docstring（AST 说了算），
      不碰函数/类里的、也不碰后面的字符串。
    """
    try:
        import ast as _ast

        树 = _ast.parse(文本)
    except SyntaxError:
        return 文本                      #: 解析不了就原样返回，别更糟
    if not 树.body:
        return 文本
    首 = 树.body[0]
    if not (isinstance(首, _ast.Expr) and isinstance(首.value, _ast.Constant)
            and isinstance(首.value.value, str)):
        return 文本                      #: 开头不是 docstring，什么都不做
    #: ⚠⚠ **只在 docstring 真的有害时才剥** —— 收紧条件，别误伤。
    #:
    #:   为什么收紧：我第一版无条件剥，结果把**答题模板**那条判据弄红了：
    #:
    #:       FAIL: test_每条线都能生成非空的答题模板
    #:             'python：模板只有 3 行非空（像没生成）'
    #:
    #:   ⚠ 而那一课是**该输出**的（它考的就是 print 输出）⇒ 不能一刀切。
    #:
    #:   判据：docstring 里含**全角标点**才剥。

    #:   · 全角标点会触发第 00 课那条「用了半角括号和引号」的检查
    #:     ⇒ 参考答案会**踩自己的检查**（这就是要剥它的原因）
    #:   · 纯 ASCII 的 docstring（比如某一课自己写的输出说明）**不剥**
    #:     —— 它可能就是那一课该输出的东西
    有害 = ("（", "）", "，", "。", "：", "；", "“", "”", "‘", "’", "《", "》", "—", "…")
    if not any(c in 首.value.value for c in 有害):
        return 文本
    行 = 文本.split("\n")
    #: docstring 的字面量跨 `lineno..end_lineno`（1-based）⇒ 整段切掉
    起 = 首.lineno - 1
    止 = getattr(首, "end_lineno", 首.lineno)      #: 切到它**含**的那一行
    剩 = 行[:起] + 行[止:]
    出 = "\n".join(剩).lstrip("\n")
    return 出 if 出.strip() else 文本


def 参考答案(k: 课) -> str:
    """这一课的参考答案（**先自己写再看**）。"""
    if k.轨道 in ("csharp", "cpp", "java", "c") and k.工程:
        #: ⚠⚠ **必须剥装饰** —— 见 `剥装饰` 上面那段：不剥的话
        #:   返回值不是合法代码，"参考答案全过"这条判据永远过不了。
        return 剥装饰(_运行课程程序(k.工程, ["答案", k.编号])[1] or "")
    if k.轨道 == "python":
        from . import run as R
        with _截获输出() as 缓:
            R._cmd_answer(k.编号)
        #: ⚠⚠ **补 剥装饰**（2026-10-14，队友 `learning-verify` 查出来的）：
        #:   原来只有编译型那条分支剥了 ⇒ python 线返回的答案**带着展示抬头**：
        #:
        #:       （空行）
        #:       ── 参考答案 ────────────────────────────
        #:         （先自己写一遍再看。抄一遍不如写错一遍学得多。）
        #:       ────────────────────────────────────────
        #:       """第 54 课参考答案 —— 最小生成树。"""
        #:
        #:   ⇒ `M.检查(课, M.参考答案(课))` 会把带 `─` 的文字写进答题文件
        #:     ⇒ **SyntaxError** ⇒ 检查全跑不了。
        #:   ⚠ 而它一直没被发现，因为**记号不匹配**盖住了（见下条）。
        #: ⚠ 先剥展示抬头，再剥**模块 docstring** ——
        #:   后者里有全角括号，会让第 00 课那条「用了半角括号和引号」判它不过。
        return _剥模块文档串(剥装饰(缓.getvalue()))
    if k.轨道 == "enginecourse":
        from . import enginecourse as EC
        with _截获输出() as 缓:
            EC._答案(项目根(), k.编号)
        return 剥装饰(缓.getvalue())   #: ⚠ 同 python 分支：不剥就是带抬头的展示文本
    if k.轨道 in ("ai", "embedded", "raspi"):
        模块 = {"ai": "aicourse", "embedded": "embedded", "raspi": "raspi_course"}[k.轨道]
        m = __import__(f"hybridengine_light.{模块}", fromlist=["x"])
        with _截获输出() as 缓:
            m._答案(项目根(), k.编号)
        return 剥装饰(缓.getvalue())   #: ⚠ 同上
    try:
        解 = k.源文件.parent / "solutions" / k.源文件.name
        return 解.read_text(encoding="utf-8", errors="replace") if 解.is_file() else "（这一课没有参考答案）"
    except OSError:
        return "（读不到参考答案）"


def 体检() -> list[tuple[str, bool, str]]:
    """环境自检：哪几条线现在能用、缺什么。界面首页显示这个。"""
    def 项(名, 工具, 缺了怎么装):
        p = _找工具(工具)
        return (名, p is not None, (p or "未安装") + ("" if p else "  ← " + 缺了怎么装))
    出 = [
        ("Python 运行时", True, sys.version.split()[0] + "（已内置，不需要装）"),
        项(".NET SDK（C# / C++ / Java / C 需要）", "dotnet",
           "winget install Microsoft.DotNet.SDK.8"),
        项("g++（C++ 需要）", "g++",
           "winget install BrechtSanders.WinLibs.POSIX.UCRT"),
        项("gcc（C 需要）", "gcc", "与 g++ 同一个包"),
        项("javac（Java 需要）", "javac", "装 JDK（不是 JRE）"),
    ]
    return 出
