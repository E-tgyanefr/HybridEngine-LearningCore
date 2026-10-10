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
import shutil
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

# ── 轨道定义 ─────────────────────────────────────────────────────────────
#: 轨道键 -> (显示名, 开箱即用?, 说明)
轨道 = {
    "python":   ("Python 基础", True,  "零依赖，开箱即用"),
    "games":    ("小游戏",      True,  "用前面几课的知识写的，读得懂"),
    "ai":       ("人工智能入门", True,  "零依赖，算法全部自己实现"),
    "embedded": ("嵌入式/单片机", True, "单片机模拟器，不需要真板子"),
    "raspi":    ("树莓派",      True,  "树莓派模拟器，不需要真板子"),
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

    def 显示(self) -> str:
        return f"{self.编号}  {self.标题}"


# ── 工具 ─────────────────────────────────────────────────────────────────

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
即时轨道 = ("python", "games", "ai", "embedded", "raspi")


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
    elif 轨道键 in ("ai", "embedded", "raspi", "games"):
        出 = _发现Python系(轨道键, root)
    elif 轨道键 in 编译型轨道:
        出 = _发现编译型(轨道键, root)
    else:
        出 = []
    _发现缓存[轨道键] = 出
    return 出


def _发现Python系(轨道键: str, root: Path) -> list[课]:
    """AI / 嵌入式 / 树莓派 / 小游戏 —— 它们的模块各有 `_课程们(root)`。"""
    if 轨道键 == "games":
        d = root / "games"
        if not d.is_dir():
            return []
        return [课("games", f.stem[:2], f.stem, f, f) for f in sorted(d.glob("*.py"))]

    模块 = {"ai": "aicourse", "embedded": "embedded", "raspi": "raspi_course"}[轨道键]
    m = __import__(f"hybridengine_light.{模块}", fromlist=["x"])
    出 = []
    try:
        for p in m._课程们(root):
            编号 = p.stem[:2]
            出.append(课(轨道键, 编号, m._标题(p), p, p))
    except Exception:
        # ⚠ 课程线坏了不该让整个界面打不开 —— 界面要能显示其它线
        return []
    return 出


def _发现编译型(轨道键: str, root: Path) -> list[课]:
    """C# / C++ / Java / C —— 课程定义是 .cs，答题文件在 `<线>-answers/`。

    ⚠ 用**子进程**问课程程序要列表（`dotnet run ... ` 不带参数会列出课程）。
      不在这里重复实现它的 `[课程]` 特性扫描 —— 那是 C# 侧的事，
      在这里抄一遍必然两边不一致（这正是我加 C 课时踩到的坑）。
    """
    工程名 = {"csharp": "lessons", "cpp": "cpp-lessons",
              "java": "java-lessons", "c": "c-lessons"}[轨道键]
    工程 = root / "csharp" / 工程名
    if not (工程 / "Program.cs").is_file():
        return []
    答案名 = {"csharp": "csharp-answers", "cpp": "cpp-answers",
              "java": "java-answers", "c": "c-answers"}[轨道键]
    出 = []
    for 行 in _运行课程程序(工程, [])[1].split("\n"):
        s = 行.strip()
        # 形如：   01   第 01 课：输出 —— 让程序开口说话   （5 项检查）
        if len(s) > 6 and s[:2].isdigit() and "  " in s:
            编号 = s[:2]
            标题 = s[4:].split("（")[0].strip()
            if 标题:
                出.append(课(轨道键, 编号, 标题, 工程, root / 答案名, 工程))
    return 出


# ── 调用编译型课程程序 ───────────────────────────────────────────────────

def 有dotnet() -> bool:
    return shutil.which("dotnet") is not None


def _运行课程程序(工程: Path, 参数: list[str], 超时: int = 300) -> tuple[int, str]:
    """跑 `dotnet run --project <工程> <参数>`，返回 (退出码, 输出文字)。"""
    if not 有dotnet():
        return 127, (
            "没有找到 .NET SDK。\n\n"
            "这几门课程要把你写的代码**真的编译运行**才能判对错，所以需要它：\n\n"
            "  Windows  https://dotnet.microsoft.com/download\n"
            "           或：winget install Microsoft.DotNet.SDK.8\n\n"
            "装完**重开一个窗口**（让 PATH 生效）再试。\n"
            "（Python 那几门不需要它，现在就能用。）")
    try:
        r = subprocess.run(["dotnet", "run", "--project", str(工程), *参数],
                           capture_output=True, timeout=超时, cwd=str(工程.parent.parent))
        文 = (r.stdout + r.stderr).decode("utf-8", "replace")
        return r.returncode, 文
    except subprocess.TimeoutExpired:
        return 124, f"超时（超过 {超时} 秒）—— 是不是有死循环？"
    except OSError as e:
        return 1, f"启动 dotnet 失败：{e}"


# ── 读课程正文 / 答案 ────────────────────────────────────────────────────

def 看课(k: 课) -> str:
    """这一课的正文（要做什么）。"""
    if k.轨道 in ("csharp", "cpp", "java", "c") and k.工程:
        return _运行课程程序(k.工程, [k.编号])[1] or "（这一课没有正文）"
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
    if k.轨道 in ("ai", "embedded", "raspi", "games"):
        return k.源文件        # 这几条线的答题就是课程文件旁边/同目录
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
    答案名 = {"csharp": "csharp-answers", "cpp": "cpp-answers",
              "java": "java-answers", "c": "c-answers"}[k.轨道]
    # 兜底：按"编号_标题清理"猜一个（问不到时用）
    净 = "".join(c for c in k.标题 if c.isalnum()) or "作业"
    猜 = root / 答案名 / f"{k.编号}_{净}.c" if k.轨道 == "c" else root / 答案名 / f"{k.编号}_{净}.cs"
    _答题路径缓存[键] = 猜
    return 猜


def _确保模板(k: 课) -> None:
    """生成答题模板（第一次打开时）。"""
    文件 = _答案文件(k)
    if 文件.is_file():
        return
    文件.parent.mkdir(parents=True, exist_ok=True)
    if k.轨道 == "python":
        from . import run as R
        文件.write_text(R._stub_for(k.源文件), encoding="utf-8")
    elif k.轨道 in ("ai", "embedded", "raspi"):
        模块 = {"ai": "aicourse", "embedded": "embedded", "raspi": "raspi_course"}[k.轨道]
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
    if k.轨道 in ("ai", "embedded", "raspi"):
        模块 = {"ai": "aicourse", "embedded": "embedded", "raspi": "raspi_course"}[k.轨道]
        m = __import__(f"hybridengine_light.{模块}", fromlist=["x"])
        with _截获输出() as 缓:
            m._检查(项目根(), k.编号)
        return 缓.getvalue()
    return _跑Python答案(k)


def 参考答案(k: 课) -> str:
    """这一课的参考答案（**先自己写再看**）。"""
    if k.轨道 in ("csharp", "cpp", "java", "c") and k.工程:
        return _运行课程程序(k.工程, ["答案", k.编号])[1]
    if k.轨道 == "python":
        from . import run as R
        with _截获输出() as 缓:
            R._cmd_answer(k.编号)
        return 缓.getvalue()
    if k.轨道 in ("ai", "embedded", "raspi"):
        模块 = {"ai": "aicourse", "embedded": "embedded", "raspi": "raspi_course"}[k.轨道]
        m = __import__(f"hybridengine_light.{模块}", fromlist=["x"])
        with _截获输出() as 缓:
            m._答案(项目根(), k.编号)
        return 缓.getvalue()
    try:
        解 = k.源文件.parent / "solutions" / k.源文件.name
        return 解.read_text(encoding="utf-8", errors="replace") if 解.is_file() else "（这一课没有参考答案）"
    except OSError:
        return "（读不到参考答案）"


def 体检() -> list[tuple[str, bool, str]]:
    """环境自检：哪几条线现在能用、缺什么。界面首页显示这个。"""
    出 = [("Python 运行时", True, sys.version.split()[0]),
          (".NET SDK（C#/C++/Java/C 需要）", 有dotnet(), "已安装" if 有dotnet() else "未安装"),
          ("g++（C++ 需要）", shutil.which("g++") is not None, shutil.which("g++") or "未安装"),
          ("gcc（C 需要）", shutil.which("gcc") is not None, shutil.which("gcc") or "未安装"),
          ("javac（Java 需要）", shutil.which("javac") is not None, shutil.which("javac") or "未安装")]
    return 出
