"""hybridengine_light.ctl —— `hybridengine_light-ctl`：与专业版**同一套协议**的控制接口。

═══════════════════════════════════════════════════════════════════════════
它是什么
═══════════════════════════════════════════════════════════════════════════

**一行一条 JSON 请求 → 一行一条响应**，stdio 上跑，`id` 必回显：

    → {"id":1,"cmd":"emu.ping"}
    ← {"id":1,"ok":true,"r":{"pong":true}}
    → {"id":2,"cmd":"lesson.list","args":{"line":"python"}}
    ← {"id":2,"ok":true,"r":{"lines":[...],"lessons":[...]}}
    → {"id":3,"cmd":"不存在"}
    ← {"id":3,"ok":false,"err":{"code":-1,"msg":"未知命令: 不存在"}}

协议规范见 `HybridEngine/docs/控制协议.md`（第三节把命令集按产品分层）。
专业版的参考实现是 `HybridEngine/tools/ms_ctl/main.cpp`，
Python 客户端是 `HybridEngine/tools/msctl.py`（`Ctl` 类）——
本文件**照那个形状**做，所以同一段客户端代码能驱动两个产品。

═══════════════════════════════════════════════════════════════════════════
三条硬约定（与专业版逐字相同）
═══════════════════════════════════════════════════════════════════════════

1. **不静默**：任何失败都必须 `ok:false` + 非空 `err.msg`
2. **`id` 必回显**：即使命令失败（连请求不是 JSON 时也回一条 `id:0`）
3. **一行一响应**：调用方可以同步读写，不需要流式分帧

⚠ 第四条约定的位置很微妙：**stdout 只跑协议，日志一律 stderr**。
   专业版是被引擎的日志污染过协议流才定下这条（见 main.cpp 的 fd 重定向）。
   HybridEngine-light 这边同样有风险 —— 学生的代码里一句 `print` 就能混进协议流。
   所以本文件用**同样的 fd 级做法**（见 `_协议通道`），而不是"指望没人 print"。

═══════════════════════════════════════════════════════════════════════════
为什么 `hybridengine_light-ctl` 是"套在既有 API 外的一层"，而不是新写的功能
═══════════════════════════════════════════════════════════════════════════

HybridEngine-light 的核心本来就是控制层：

    run.py        课程列表 / 看课 / 检查答案（CHECK_* 框架）
    check.py      run_checks() → Report(results, passed, total)
    exam.py       题库（48 题）、Progress 成绩历史、weak_topics()
    examrun.py    判分（真的执行学生的函数跑隐藏用例）+ 订正报告
    coach.py      离线助手：看代码 / 解释报错 / 提问引导

⇒ 所以本文件**不重新实现任何判定逻辑**，只做两件事：

    · 把它们的返回值变成**结构化的 JSON**（而不是只有 print 的文字）
    · 补上"一次进程、多次调用"的**会话**（考试会话、成绩累积）

⚠ 一处刻意的例外：`lesson.check` 复用 `run.py` 里那套**私有**的答题文件解析
   （`_lessons`/`_find`/`_load_answer`/`_title_of`）。理由：那套逻辑（单文件作业 /
   目录形态作业 / 首次自动生成模板 / answers 与 solutions 必须分开）是踩过坑的，
   抄一份必然分叉。复用它有个必要条件：**本文件不得被 `run.py` 反向 import**
   （否则 import 环）。目前没有，且不需要。

═══════════════════════════════════════════════════════════════════════════
零第三方依赖 / 不弹窗
═══════════════════════════════════════════════════════════════════════════

只用标准库（json/sys/os/io/contextlib/pathlib/importlib/traceback/random/time）。
**不 import tkinter** —— 画布那条线（`hybridengine_light.Stage`）会建窗口，
而后台作业约束是"不弹窗、不抢焦点"。所以 `code.run` 里跑学生代码时
用的是已经导入的东西，本模块自己绝不会去建窗口。
"""

from __future__ import annotations

import contextlib
import importlib.util
import io
import json
import os
import random
import sys
import time
import traceback
from pathlib import Path

__all__ = ["main", "run_stdio", "COMMANDS", "命令表", "协议版本"]


协议版本 = "1"

# 错误码。⚠ 专业版协议层只有 `-1`（它把 ABI 的错误码留在 `call` 里）。
#   这里为了**调用方能区分"命令不存在"和"命令存在但参数错"**多分了两档，
#   而 `-1` 的语义与专业版**逐字对齐**（未知命令）。
错误码 = {
    "未知命令": -1,      # 与专业版一致
    "参数错": -2,        # 本产品新增：命令存在，但参数不合法
    "内部错": -3,        # 本产品新增：命令自己崩了（含学生代码让检查没法进行）
}


# ══════════════════════════════════════════════════════════════════════════
#  协议通道（fd 级）—— ⚠ 这一节是"学生 print 不能污染协议"的唯一保证
# ══════════════════════════════════════════════════════════════════════════

# 协议响应用的 fd。非 None 时 `_写响应` 走 os.write，绕过 sys.stdout。
_协议fd: int | None = None


def _协议通道():
    """把 stdout 换成 stderr，并**保住原来的 fd 1 作为协议通道**。

    ══ 为什么需要（这不是预防性设计）══
    专业版实测踩到过：引擎往 stdout 打日志 ⇒ 日志混进协议 ⇒
    严格的 JSON 客户端直接 `JSONDecodeError`（见 main.cpp 的说明）。

    HybridEngine-light 这边的污染源更直白：**学生的代码里就有 print**。
    而 `code.run` / `lesson.check` 都要执行学生代码 ——
    "指望他们的代码不打印"是把协议的正确性押在学生身上。

    ══ 做法（与 main.cpp 逐字同构，只是 C 的 `_dup` 换成 `os.dup`）══
        1. `os.dup(1)` 保住原来的 fd 1 作**协议通道**
        2. `os.dup2(2, 1)` ⇒ 之后**任何**写 fd 1 的东西（含子进程继承的 fd 1）
           都进 stderr，而协议走那个 dup 出来的 fd

    ⚠ 为什么不用"把 sys.stdout 换成 stderr"这种 Python 层面的做法：
      那样只盖得住 Python 的 print，盖不住 `os.write(1, ...)`、
      `subprocess` 里继承的 fd 1、以及将来可能加的 C 扩展。
      **fd 级重定向能盖住全部来源** —— 这正是当初选它的理由。

    ⚠ 副作用（诚实记下来）：本进程**之后启动的子进程**，其 fd 1 也指向 stderr，
      不再指向调用方的 stdout 管道。所以：
        · 本模块自己**不 spawn 子进程**（`code.run` 是**进程内**执行）
        · `lesson.run`（要真跑学生脚本）会把 fd 1 **临时还原**（见 `_还原fd1`）
    """
    global _协议fd
    try:
        协议fd = os.dup(1)
    except OSError:
        协议fd = None
        return
    try:
        os.dup2(2, 1)
    except OSError:
        os.close(协议fd)
        _协议fd = None
        return
    _协议fd = 协议fd
    # ⚠ 不 reconfigure sys.stdout —— 它现在写的是 fd 1（= stderr），
    #   但调用方看到的是"stdout 拆成两个通道"，而 fd 级改动对子进程同样生效。
    #   这里只做一件小事：让 Python 侧的 print 立刻生效（行缓冲），
    #   免得日志攒在缓冲区里、在响应之后才冒出来，让人误以为对应关系错了。
    try:
        sys.stdout.reconfigure(line_buffering=True)
    except Exception:                                  # noqa: BLE001
        pass


@contextlib.contextmanager
def _还原fd1():
    """临时把 fd 1 还原成协议通道 —— 给"必须真起子进程"的命令用。

    只用于 `lesson.run`：那一课考的就是"你的脚本在命令行里跑起来是什么样"，
    所以它**必须**是子进程（进程内跑一次 setup() 就再也跑不了第二次）。
    """
    if _协议fd is None:
        yield
        return
    try:
        保存 = os.dup(1)
    except OSError:
        yield
        return
    os.dup2(_协议fd, 1)
    try:
        yield
    finally:
        os.dup2(保存, 1)
        os.close(保存)


def _写响应(id: int, ok: bool, 体: dict | None = None, 错: str = "") -> None:
    """写一条响应。⚠ 一定是**一行**，且 id 一定回显。"""
    out: dict = {"id": id, "ok": bool(ok)}
    if ok:
        if 体:
            out["r"] = 体
    else:
        # ⚠ 不静默：失败必须有非空 err.msg。这里兜一层 ——
        #   即使调用方传了空字符串，也不会写出 `{"msg":""}`。
        out["err"] = {"code": _当前错误码, "msg": 错 or "命令失败（没有给出原因）"}
    # ⚠ `separators` 去掉 json.dumps 默认的空格 —— 与专业版手写的
    #   `{"id":1,"ok":true,...}` **逐字同形**。两种都是合法 JSON，
    #   但"同一段客户端代码驱动两个产品"时，肉眼 diff 两边更省事。
    行 = json.dumps(out, ensure_ascii=False, separators=(",", ":")) + "\n"
    _写行(行)


# `_写响应` 用的错误码 —— 由 `_分派` 在调用前设好。
_当前错误码 = 错误码["未知命令"]


def _写行(行: str) -> None:
    """把一行写到协议通道。⚠ 不用 print：响应的通道是 dup 出来的那个 fd。"""
    数据 = 行.encode("utf-8")
    if _协议fd is None:
        # 没拿到协议 fd（例如在测试里直接把 stdout 换成了 StringIO）——
        # 退回 sys.stdout。⚠ 也要绕开缓冲：调用方是**阻塞读一行**的，
        # 攒在缓冲区里就是死锁。
        try:
            sys.stdout.write(行)
            sys.stdout.flush()
        except Exception:                              # noqa: BLE001
            pass
        return
    with contextlib.suppress(OSError):
        # ⚠ 循环写全部：管道满时 os.write 可能只写一部分。
        视图 = memoryview(数据)
        while 视图:
            视图 = 视图[os.write(_协议fd, 视图):]


# ══════════════════════════════════════════════════════════════════════════
#  根目录 / 课程线
# ══════════════════════════════════════════════════════════════════════════

def 项目根() -> Path:
    """HybridEngine-light 项目根 = 本包所在目录的上一级。"""
    return Path(__file__).resolve().parent.parent


# 课程线表 —— **按实测的目录结构写**，不是按文档写。
#   line      : 协议里用的稳定标识（英文，客户端拿它做主键）
#   dir       : 课程目录（`01_xxx.py` 这种编号课程）
#   answers   : 学生答题文件目录（首次检查时自动生成模板）
#   solutions : 参考答案目录（课程作者提供，只读）
#   cmd       : 与之等价的命令行调用（客户端的"等价 CLI"契约）
课程线 = {
    "python": {
        "title": "Python",
        "dir": "lessons",
        "answers": "lessons/answers",
        "solutions": "lessons/solutions",
        "cmd": ["python", "-m", "hybridengine_light"],
    },
    "embedded": {
        "title": "嵌入式 / 单片机",
        "dir": "embedded",
        "answers": "embedded-answers",
        "solutions": "embedded-solutions",
        "cmd": ["python", "-m", "hybridengine_light", "嵌入式"],
    },
    "raspi": {
        "title": "树莓派",
        "dir": "raspi",
        "answers": "raspi-answers",
        "solutions": "raspi-solutions",
        "cmd": ["python", "-m", "hybridengine_light", "树莓派"],
    },
    "ai": {
        "title": "人工智能入门",
        "dir": "ai",
        "answers": "ai-answers",
        "solutions": "ai-solutions",
        "cmd": ["python", "-m", "hybridengine_light", "AI"],
    },
}

# 课程线的别名 —— 客户端写中文更顺手，而这几个词本来就是 CLI 里的写法。
线别名 = {
    "embedded": "embedded", "嵌入式": "embedded", "单片机": "embedded", "mcu": "embedded",
    "raspi": "raspi", "树莓派": "raspi", "rpi": "raspi", "pi": "raspi",
    "ai": "ai", "人工智能": "ai", "AI": "ai",
    "python": "python", "py": "python", "lessons": "python",
}


class 参数错(Exception):
    """参数不合法 —— 会被翻译成 `ok:false, err.code=-2`。"""


def _线(名: str | None) -> str:
    """把课程线名（中/英）规范成 `课程线` 的键。缺省 = python。"""
    if 名 is None or 名 == "":
        return "python"
    k = 线别名.get(str(名)) or 线别名.get(str(名).lower())
    if k is None:
        raise 参数错(f"没有「{名}」这条课程线。可用的：{', '.join(课程线)}")
    return k


def _课程们(line: str, root: Path | None = None) -> list[Path]:
    """某条线上的编号课程。⚠ 与各 `*_course.py` 的 `_课程们` 同一套 glob。"""
    d = (root or 项目根()) / 课程线[line]["dir"]
    if not d.is_dir():
        return []
    return sorted(p for p in d.glob("[0-9][0-9]_*.py"))


def _标题(p: Path) -> str:
    """课程标题 = 文件里第一个注释行（与各运行器的 `_标题` 一致）。"""
    try:
        for line in p.read_text(encoding="utf-8").splitlines():
            s = line.strip()
            if s.startswith("#"):
                return s.lstrip("# ").strip()
    except OSError:
        pass
    return p.stem


def _要做(p: Path) -> str:
    """抓课程文件里的「要做：」段落 —— 学生真正要动手的那几行。"""
    行: list[str] = []
    抓 = False
    try:
        文本 = p.read_text(encoding="utf-8")
    except OSError:
        return ""
    for line in 文本.splitlines():
        s = line.strip()
        if s.startswith("# 要做"):
            抓 = True
            continue
        if 抓:
            if s.startswith("#"):
                行.append(s.lstrip("# ").rstrip())
            elif not s:
                行.append("")
            else:
                break
    return "\n".join(行).strip()


def _找课(line: str, 编号: str) -> Path | None:
    """按编号（"01"）或名字前缀找课 —— 与 CLI 的 `_find` 同一套语义。"""
    for p in _课程们(line):
        if p.stem.startswith(str(编号)):
            return p
    return None


def _解课(参数: dict) -> tuple[str, Path]:
    """从 args 里解出 (课程线, 课程文件)。

    ⚠ 两种写法都收：
        {"line":"embedded","id":"01"}      显式
        {"id":"嵌入式 01"} / {"id":"ai 02"}  课程线写在 id 里（客户端更省事）
    缺省线是 python —— 与 `python -m hybridengine_light 01` 一致。
    """
    raw = 参数.get("id", 参数.get("lesson", 参数.get("课", "")))
    if raw is None or str(raw).strip() == "":
        raise 参数错("缺 id（课号，例如 \"01\"；也可以写 \"嵌入式 01\"）")
    文本 = str(raw).strip()

    line_参数 = 参数.get("line")
    编号 = 文本
    if line_参数 is None and " " in 文本:
        头, 尾 = 文本.split(None, 1)
        if 头 in 线别名:
            line_参数, 编号 = 头, 尾.strip()
    line = _线(line_参数)

    课 = _找课(line, 编号)
    if 课 is None:
        raise _没找到(line, 编号)
    return line, 课


def _没找到(line: str, 编号: str) -> 参数错:
    现有 = "、".join(p.stem for p in _课程们(line)) or "（这条线还没有课程）"
    return 参数错(f"{课程线[line]['title']} 这条线上没有「{编号}」这一课。现有：{现有}")


# ══════════════════════════════════════════════════════════════════════════
#  抓输出 —— 既有 API 是"print 型"的，这里把它们的输出收成字符串
# ══════════════════════════════════════════════════════════════════════════

@contextlib.contextmanager
def _抓输出():
    """把 print 收进一个 StringIO，供"同时返回结构化字段和原文"用。

    ⚠ 为什么要兜住 stdout：`check.report()` / `progress.*` 这些既有 API
      都是直接 print 的。抓下来的**原文**会放进 `r.cli`，
      让调用方能对照"命令行会看到什么" —— 而不是让那些输出漏进协议流。

    ⚠ `sys.stdout` 此刻指向 fd 1（已被重定向到 stderr），
      但**协议响应走的是 dup 出来的 fd**，与这里无关 —— 两条通道互不影响。
    """
    缓冲 = io.StringIO()
    旧 = sys.stdout
    sys.stdout = 缓冲
    try:
        yield 缓冲
    finally:
        sys.stdout = 旧


@contextlib.contextmanager
def _工作目录(d: Path | None):
    """临时切 cwd。

    ⚠ 为什么需要：学生答案真的会依赖相对路径（模块化那一课要
      `import mytools`、树莓派课程会写数据文件）。
      在命令行里他们是从项目根或作业目录跑的 —— 这里复现同一件事。
      （用 chdir 而不是 `sys.path` hack：chdir 对 `open()` 同样生效。）
    """
    if d is None:
        yield
        return
    旧 = Path.cwd()
    try:
        os.chdir(d)
    except OSError:
        yield
        return
    try:
        yield
    finally:
        with contextlib.suppress(OSError):
            os.chdir(旧)


# ══════════════════════════════════════════════════════════════════════════
#  考试会话 —— 协议层的"会话"就在这里（命令行版只能一口气考完）
# ══════════════════════════════════════════════════════════════════════════

_会话: dict[str, dict] = {}
_会话序 = 0


def _新会话() -> str:
    global _会话序
    _会话序 += 1
    return f"s{_会话序}-{random.randrange(0x1000, 0xFFFF):04x}"


def _取会话(参数: dict) -> dict:
    sid = str(参数.get("session", 参数.get("会话", "")))
    s = _会话.get(sid)
    if s is None:
        if not sid:
            raise 参数错("缺 session —— 先发 exam.start，它会返回一个 session")
        raise 参数错(f"没有这个考试会话：{sid}（会话只存在于当前进程里）")
    return s


def _题面(q, i: int) -> dict:
    """把一道题变成给客户端看的 JSON。

    ⚠⚠ **绝不能把答案发出去**：`answer`（选项下标）、`expect`（标准答案）、
       `tests`（隐藏用例）、`explain`（为什么）都会泄题。
       这条是考试系统成立的前提 —— 泄了题，判分再准也没意义。
       所以这里是**白名单**（逐字段挑），不是"复制一份再删几个键"。
    """
    面 = {
        "i": i,
        "id": q.id,
        "type": q.type,
        "topic": q.topic,
        "level": q.level,
        "q": q.q,
        "points": q.points,
    }
    if q.type == "choice":
        面["options"] = list(q.options)
    if q.type == "code":
        面["starter"] = q.starter
        面["tests"] = len(q.tests)          # 只给条数，不给内容
    return 面


def _判一题(q, 作答, 序号=None) -> tuple[bool, str, str]:
    """判一道题。返回 (是否对, 归一化后的作答, 说明)。

    ══ 判分规则与 `examrun.take_exam` **逐条一致**（不另立一套）══
      choice       : 选对才算对。`作答` = **0 起下标**或选项原文；
                     要按人看的序号（1 起）用 `序号`
      output / fill: `exam.normalize` 归一化后比较（空白、引号样式不扣分）
      code         : **真的执行**，跑隐藏用例（`examrun._grade_code`）
    """
    if q.type == "choice":
        n = len(q.options)
        if isinstance(作答, bool):
            raise 参数错("选择题的答案要写选项下标或选项原文，不能是 true/false")

        # ⚠⚠ 这一处踩过坑，值得写清楚：
        #   一开始我让 `answer` 同时兼容"0 起下标"和"1 起序号"，
        #   靠 `1 <= 值 <= n` 猜是哪一种 —— 结果 n=4 时传 3 被判成
        #   "第 3 个选项"（下标 2），而调用方说的是"下标 3"。
        #   **中间那一段永远有歧义，猜不了的区间就不要猜。**
        #   ⇒ 现在：answer 一律 **0 起下标**（与内部存储、与 UI 显示同一种）；
        #     要按人看的序号（1 起）就用 `choice` 字段，两者语义不重叠。
        if 序号 is not None:
            if isinstance(序号, bool) or not isinstance(序号, int):
                try:
                    序号 = int(str(序号).strip())
                except (TypeError, ValueError):
                    raise 参数错(f"choice 要是整数的选项序号（1 起），收到 {序号!r}") from None
            if not (1 <= 序号 <= n):
                raise 参数错(f"choice={序号} 超出范围（1..{n}）")
            given = str(序号 - 1)
        elif isinstance(作答, int):
            if not (0 <= 作答 < n):
                raise 参数错(f"选项下标 {作答} 超出范围（0..{n - 1}；"
                             f"要按人看的序号就传 choice，1..{n}）")
            given = str(作答)
        else:
            文字 = str(作答).strip()
            if 文字.isdigit() and 0 <= int(文字) < n:
                given = str(int(文字))
            else:
                候选 = [i for i, o in enumerate(q.options) if o.strip() == 文字]
                if not 候选:
                    raise 参数错(f"「{文字}」不是这道题的选项之一：{q.options}"
                                 f"（也可以传下标 0..{n - 1}，或 choice 序号 1..{n}）")
                given = str(候选[0])
        return given == str(q.answer), given, ""

    if q.type == "code":
        from .examrun import _grade_code
        src = str(作答 if 作答 is not None else "")
        ok, note = _grade_code(q, src)
        return ok, src, note

    # output / fill
    from .exam import normalize
    given = str(作答 if 作答 is not None else "")
    return normalize(given) == normalize(str(q.expect)), given, ""


def _成绩对象(会话: dict):
    """把会话累积成 `exam.Attempt` + `examrun.Answered` 列表。

    ⚠ 刻意复用 examrun 的 `Answered` / `Attempt`：这样 `render_report`
      能用，报告文字与命令行版**逐字相同**（客户端不必再实现一套排版）。
    """
    from .exam import Attempt
    from .examrun import Answered

    # ⚠ 只统计**已作答**的：没作答的按错计，那是在 `_exam_grade` 里显式说的，
    #   不能在这里悄悄算进分母（否则"答了 3 题"和"考了 10 题没答"分数一样）。
    明细 = []
    by_topic: dict[str, list[int]] = {}
    for i in sorted(会话["answers"]):
        ok, given, note = 会话["answers"][i]
        q = 会话["questions"][i]
        明细.append(Answered(q=q, given=given, ok=ok, note=note))
        桶 = by_topic.setdefault(q.topic, [0, 0])
        桶[1] += 1
        if ok:
            桶[0] += 1
    att = Attempt(
        ts=time.time(), mode=会话["mode"], total=len(明细),
        correct=sum(1 for a in 明细 if a.ok),
        seconds=time.time() - 会话["t0"],
        by_topic=by_topic,
        wrong_ids=[a.q.id for a in 明细 if not a.ok],
    )
    return att, 明细


# ══════════════════════════════════════════════════════════════════════════
#  命令实现
# ══════════════════════════════════════════════════════════════════════════
#  ⚠ 约定：每个函数收 `args`（dict），返回 dict（就是响应里的 `r`）。
#    失败**抛异常**：`参数错` → err.code=-2，其它异常 → err.code=-3（不静默）。

def _emu_ping(args: dict) -> dict:
    return {"pong": True, "pid": os.getpid()}


def _emu_info(args: dict) -> dict:
    """协议层命令 —— ⚠ `product` 是"同一段客户端代码驱动两个产品"的关键。

    专业版这里回 `{"product":"hybridengine","api":168}`。
    轻量版回 `{"product":"hybridengine_light", ...}` ⇒ 客户端靠这一个字段分流。
    """
    from . import __version__
    return {
        "product": "hybridengine_light",            # ⚠ 与专业版的 "hybridengine" 相对
        "version": __version__,            # ⚠ 文档明确要求的字段
        "protocol": 协议版本,
        "python": 系统_版本(),
        "root": _路径串(项目根()),
        "lines": list(课程线),
        "commands": len(COMMANDS),
        # 与专业版的 `api`（导出函数数）相对的"能力规模"
        "lessons": {ln: len(_课程们(ln)) for ln in 课程线},
    }


def 系统_版本() -> str:
    v = sys.version_info
    return f"{v.major}.{v.minor}.{v.micro}"


def _路径串(p: Path | str) -> str:
    """路径 → 字符串。

    ⚠ Windows 路径里的 `\\` 在 JSON 里是**非法转义** —— 专业版为此专门手写过
      转义（`capture.writeBmp` 的 path，实测报过 `Invalid \\escape: line 1 column 63`）。
      这里不再手写：所有响应都过 `json.dumps`，那才是通用解法。
      **所以本文件里任何外部字符串都不能用字符串拼接进 JSON。**
    """
    return str(p)


def _emu_quit(args: dict) -> dict:
    return {}


def _ctl_commands(args: dict) -> dict:
    """自省：本产品支持哪些命令。

    ⚠ 这条**必须存在**（协议文档 §3.2）：它让"这个产品支持什么"**可查询**，
      而不是靠文档 —— 文档会过期，这条不会。
      ⇒ 所以这里的列表**由 `COMMANDS` 表生成**，手写一份必然与实现分叉。
    """
    return {
        "commands": 命令名列表(),
        "product": "hybridengine_light",
        "detail": {名: {"summary": 元["summary"], "args": 元["args"]}
                   for 名, 元 in COMMANDS.items()},
    }


def _lesson_lines(args: dict) -> dict:
    return {"lines": [
        {"line": ln, "title": 课程线[ln]["title"], "dir": 课程线[ln]["dir"],
         "lessons": len(_课程们(ln)), "cmd": 课程线[ln]["cmd"]}
        for ln in 课程线
    ]}


def _lessons(ln: str, root: Path) -> list[dict]:
    答案目录 = root / 课程线[ln]["answers"]
    出 = []
    for p in _课程们(ln, root):
        ans = 答案目录 / p.name
        if not ans.exists() and (答案目录 / p.stem).is_dir():
            ans = 答案目录 / p.stem
        出.append({
            "id": p.stem[:2],
            "stem": p.stem,
            "title": _标题(p),
            "path": _路径串(p),
            "answered": ans.exists(),
        })
    return 出


def _lesson_list(args: dict) -> dict:
    root = 项目根()
    line = args.get("line")
    if line is None or line == "":
        选 = list(课程线)
    else:
        选 = [_线(line)]
    出 = {"lines": _lesson_lines({})["lines"]}
    for ln in 选:
        出[ln] = _lessons(ln, root)
    if len(选) == 1:
        出["line"] = 选[0]
        出["lessons"] = 出[选[0]]
    return 出


def _lesson_show(args: dict) -> dict:
    line, p = _解课(args)
    return {
        "line": line,
        "id": p.stem[:2],
        "stem": p.stem,
        "title": _标题(p),
        "path": _路径串(p),
        "requires": _要做(p),                  # 「要做：」那一段 —— 学生的作业要求
        "text": p.read_text(encoding="utf-8"),  # 课程全文（课程就是给学生读的文件）
        "cmd": 课程线[line]["cmd"] + [p.stem[:2]],
    }


def _lesson_answer(args: dict) -> dict:
    line, p = _解课(args)
    root = 项目根()
    目录 = root / 课程线[line]["solutions"]
    单文件 = 目录 / p.name
    多文件 = 目录 / p.stem
    if 单文件.exists():
        return {"line": line, "id": p.stem[:2], "title": _标题(p),
                "kind": "single", "path": _路径串(单文件),
                "files": [{"name": 单文件.name, "text": 单文件.read_text(encoding="utf-8")}]}
    if 多文件.is_dir():
        return {"line": line, "id": p.stem[:2], "title": _标题(p),
                "kind": "multi", "path": _路径串(多文件),
                "files": [{"name": f.name, "text": f.read_text(encoding="utf-8")}
                          for f in sorted(多文件.glob("*.py"))]}
    raise 参数错(f"这一课还没有参考答案（找过 {_路径串(单文件)} 和 {_路径串(多文件)}）")


def _lesson_check(args: dict) -> dict:
    """检查学生的答案 —— 复用 `run.py` 那套答题文件解析（见模块头部的说明）。

    ⚠ **单文件线（嵌入式/树莓派/AI）与 Python 线的行为差别是故意的、也是既有事实**：
      python   : 第一次检查会**生成答题模板**并返回"请去写"（CLI 的 `_load_answer`）
      embedded / raspi / ai : 同样生成模板，然后返回"请去写"
      ⇒ 两条路都不静默：`ok:true` + `reason:"未作答"` + `templateCreated:true`，
        而不是把"还没写"伪装成"检查失败"。
    """
    from . import run as _run

    line, p = _解课(args)
    root = 项目根()
    课号 = p.stem[:2]

    if line == "python":
        with _抓输出() as 缓冲:
            ans = _run._load_answer(p, 课号)      # 模板生成/加载都在里面
        if ans is None:
            return {"line": line, "id": 课号, "title": _标题(p),
                    "ok": False, "reason": "未作答",
                    "templateCreated": True,
                    "cli": 缓冲.getvalue(),
                    "hint": f"去写 {课程线[line]['answers']}，然后重新发这条命令"}
        # ⚠ 多文件作业要能 `import mytools` —— CLI 是往 sys.path 里插作业目录。
        #   这里用 chdir 达到同样效果（也覆盖了 open() 相对路径）。
        作业目录 = root / 课程线[line]["answers"] / p.stem
        ctx = _工作目录(作业目录 if (作业目录 / "main.py").exists() else root)
    else:
        # 三条新线：答案目录是**平铺**的（embedded-answers/01_xxx.py）
        答案目录 = root / 课程线[line]["answers"]
        答案文件 = 答案目录 / p.name
        if not 答案文件.exists():
            模块 = {"embedded": "embedded", "raspi": "raspi_course", "ai": "aicourse"}[line]
            mod = importlib.import_module(f".{模块}", __package__)
            答案目录.mkdir(parents=True, exist_ok=True)
            答案文件.write_text(mod._生成模板(p), encoding="utf-8")
            return {"line": line, "id": 课号, "title": _标题(p),
                    "ok": False, "reason": "未作答", "templateCreated": True,
                    "path": _路径串(答案文件),
                    "hint": f"去写 {_路径串(答案文件)}，然后重新发这条命令"}
        with _抓输出() as 缓冲:
            模块 = {"embedded": "embedded", "raspi": "raspi_course", "ai": "aicourse"}[line]
            mod = importlib.import_module(f".{模块}", __package__)
            mod._检查(root, 课号)             # 只为拿它的"人话"输出，判定另做（见下）
        with _工作目录(None):
            return _跑检查(line, p, 答案文件, cli=缓冲.getvalue())

    with ctx:
        return _跑检查(line, p, None, 已加载=ans)


def _跑检查(line: str, p: Path, 答案文件: Path | None,
            cli: str = "", 已加载=None) -> dict:
    """加载课程 + 答案，跑 `check.run_checks`，给出**结构化结果**。"""
    from . import check as _check
    from .explain import explain

    try:
        课 = _check.load_module(p, name=f"ctl_lesson_{line}_{p.stem}")
    except Exception as e:                             # noqa: BLE001
        # 课程文件自己坏了 —— 这是**我们**的问题，不是学生的
        return {"line": line, "id": p.stem[:2], "title": _标题(p),
                "ok": False, "reason": "课程文件加载失败",
                "passed": 0, "total": 0, "results": [],
                "hint": explain(e, filename=str(p))}

    if 已加载 is None:
        # ⚠ 只有"答案文件"这条路才需要自己加载；python 线是 run.py 帮我们加载好的
        #   （它还要处理单文件/多文件两种形态，见 _lesson_check）。
        try:
            已加载 = _check.load_module(答案文件, name=f"ctl_ans_{line}_{p.stem}")
        except Exception as e:                          # noqa: BLE001
            # 学生的代码跑不起来 —— 这不是"检查没过"，是"答案加载不了"
            return {"line": line, "id": p.stem[:2], "title": _标题(p),
                    "ok": False, "reason": "答案加载失败",
                    "passed": 0, "total": 0, "results": [],
                    "hint": explain(e, filename=str(答案文件))}

    rep = _check.run_checks(课, 已加载)
    出 = {
        "line": line, "id": p.stem[:2], "title": _标题(p),
        "lesson": rep.lesson,
        "ok": rep.all_ok,
        "passed": rep.passed, "total": rep.total,
        "results": [{"name": r.name, "ok": r.ok, "detail": r.detail} for r in rep.results],
        "cmd": 课程线[line]["cmd"] + ["检查", p.stem[:2]],
    }
    if rep.error:
        出["reason"] = rep.error
    if cli:
        出["cli"] = cli
    if line in ("embedded", "raspi"):
        # ⚠ 这两条线的既有 API 在每条检查结果后都提醒"模拟器管不了电路" ——
        #   协议层也必须带上，否则"检查通过"会被读成"插上板子就能亮"。
        出["disclaimer"] = ("检查通过只说明**程序逻辑**对。"
                            "限流电阻、接线位置、正负极、I2C 地址冲突这些"
                            "**电路/硬件问题**，模拟器发现不了。")
    return 出


def _lesson_run(args: dict) -> dict:
    """**真起子进程**跑学生写好的脚本 —— "你的程序在命令行里是什么样"。

    ⚠ 与 `code.run` 的分工（两条都留着，因为用途不同）：
        code.run    : 进程内 exec 一段**源码**（快，用于"试一下这几行"）
        lesson.run  : 起子进程跑**已有文件**（慢，但就是学生按 F5 的那个东西）

    ⚠ 必须 `_还原fd1()`：否则子进程的 stdout 被继承过去、进了 stderr，
      调用方收到的 `stdout` 永远是空的（这是 fd 级重定向的代价，见 `_协议通道`）。
    """
    import subprocess

    超时 = _数(args, "timeout", 10.0, 最小=0.1, 最大=600.0)

    # ── 形态 A：直接给一个路径（"跑这个文件"）──
    if args.get("path"):
        f = Path(str(args["path"]))
        if not f.is_absolute():
            f = 项目根() / f
        if not f.exists():
            raise 参数错(f"没有这个文件：{_路径串(f)}")
        line, 课号 = "?", "?"
    else:
        # ── 形态 B：按课号找"你写好的答案"（找不到就明说，不要去跑空模板）──
        line, p = _解课(args)
        课号 = p.stem[:2]
        root = 项目根()
        答案目录 = root / 课程线[line]["answers"]
        if line == "python":
            候选 = [答案目录 / p.stem / "main.py", 答案目录 / p.name]
        else:
            候选 = [答案目录 / p.name]
        f = next((c for c in 候选 if c.exists()), None)
        if f is None:
            raise 参数错("这一课你还没写答案 —— 先写 "
                         + " 或 ".join(_路径串(c) for c in 候选)
                         + "（也可以直接用 args.path 跑任意脚本）")

    with _还原fd1():
        进程 = subprocess.run(
            [sys.executable, str(f)], cwd=str(f.parent),
            capture_output=True, text=True, encoding="utf-8", errors="replace",
            timeout=超时,
        )
    return {
        "line": line, "id": 课号, "path": _路径串(f),
        "exitCode": 进程.returncode,
        "stdout": 进程.stdout,
        "stderr": 进程.stderr,
        "ok": 进程.returncode == 0,
    }


def _code_run(args: dict) -> dict:
    """跑一段学生代码，把输出与报错**都**收回来（不弹窗、不建窗口）。

    ⚠ 进程内 `exec`，与 `examrun._grade_code` 同一个思路。
      好处：快，而且 stdout 被 StringIO 抓住 ⇒ 物理上不可能污染协议流。
      代价：**没有超时/杀不掉**（见 docs/控制协议.md 的"未做的部分"）。
    """
    from .explain import explain

    源码 = args.get("source", args.get("代码", args.get("src")))
    if 源码 is None:
        raise 参数错("缺 source（要跑的 Python 源码）")
    if not isinstance(源码, str):
        raise 参数错("source 必须是字符串")

    ns: dict = {"__name__": "__hybridengine_light_ctl__"}
    buf = io.StringIO()
    err = io.StringIO()
    出错 = None
    t0 = time.time()
    with _工作目录(项目根()):
        with contextlib.redirect_stdout(buf), contextlib.redirect_stderr(err):
            try:
                exec(compile(源码, "<hybridengine_light-ctl>", "exec"), ns)   # noqa: S102
            except BaseException as e:              # noqa: BLE001 —— SystemExit 也要收
                出错 = e
    用_秒 = time.time() - t0

    返回 = {
        "stdout": buf.getvalue(),
        "stderr": err.getvalue(),
        "ok": 出错 is None,
        "seconds": round(用_秒, 4),
    }
    if 出错 is not None:
        if isinstance(出错, SyntaxError):
            返回["error"] = f"{type(出错).__name__}: {出错.msg}（第 {出错.lineno} 行）"
        else:
            返回["error"] = f"{type(出错).__name__}: {出错}"
        # ⚠ 本产品的**核心卖点**就是"报错说人话" —— 协议层当然要带上翻译
        返回["explain"] = explain(出错)
        if not isinstance(出错, (SystemExit, KeyboardInterrupt)):
            返回["traceback"] = "".join(
                traceback.format_exception(type(出错), 出错, 出错.__traceback__))
    return 返回


def _题目池(args: dict, root: Path):
    from .exam import load_banks
    bank, metas = load_banks(root)
    if not bank:
        raise 参数错("题库是空的（exams/ 目录下没有题目）")
    return bank, metas


def _exam_list(args: dict) -> dict:
    from .exam import validate_bank
    root = 项目根()
    bank, metas = _题目池(args, root)
    主题: dict[str, dict] = {}
    for q in bank:
        桶 = 主题.setdefault(q.topic, {"topic": q.topic, "count": 0, "types": {}})
        桶["count"] += 1
        桶["types"][q.type] = 桶["types"].get(q.type, 0) + 1
    return {
        "total": len(bank),
        "topics": sorted(主题.values(), key=lambda d: d["topic"]),
        "banks": [{"name": m.name, "short": m.short, "level": m.level,
                   "source": m.source, "durationMin": m.duration_min} for m in metas],
        "exams": sorted({q.exam for q in bank if q.exam}),
        # ⚠ 题库是**数据**，写错了不会立刻暴露（exam.validate_bank 的注释里
        #   记着作者自己被一个引号坑过）。自省命令顺手把它跑一遍。
        "problems": validate_bank(bank),
    }


def _exam_start(args: dict) -> dict:
    """起一场考试会话，返回**不含答案**的题面。

    ⚠ `interactive_available()` 在 stdio 下是 False ⇒ 命令行版会**跳过** code 题
      （它要学生手打多行代码）。协议版不一样：作答是随 `exam.answer` 送进来的，
      所以 code 题**能考**（见 `code.answer`）。这是协议相对命令行的真实增益。
    """
    root = 项目根()
    bank, _ = _题目池(args, root)

    池 = bank
    if args.get("exam"):
        池 = [q for q in 池 if q.exam == args["exam"]]
    if args.get("topic"):
        t = str(args["topic"])
        池 = [q for q in 池 if t in q.topic]
        if not 池:
            现有 = "、".join(sorted({q.topic for q in bank}))
            raise 参数错(f"没有「{t}」这个主题。现有主题：{现有}")
    if not 池:
        raise 参数错(f"没有符合条件的题目（exam={args.get('exam')!r}）")

    n = int(_数(args, "n", 10, 最小=1, 最大=10000))
    n = min(n, len(池))
    if args.get("seed") is not None:
        # ⚠ 可复现的抽题 —— 端到端脚本要断言"判分对不对"，题必须每次一样
        random.Random(int(args["seed"])).shuffle(池)
        抽 = 池[:n]
    else:
        抽 = random.sample(池, n)

    sid = _新会话()
    _会话[sid] = {
        "questions": 抽, "answers": {}, "mode": str(args.get("mode") or "practice"),
        "t0": time.time(), "line": "python",
    }
    return {
        "session": sid,
        "total": len(抽),
        "mode": _会话[sid]["mode"],
        "seed": args.get("seed"),
        "questions": [_题面(q, i) for i, q in enumerate(抽)],
        "next": "把每题的答案发 exam.answer（args.session + args.i + args.answer），"
                "或一次发完 exam.answerAll，最后 exam.grade 出成绩与订正",
    }


def _答案项(会话: dict, 参数: dict):
    """从 args 里解出 (下标, 题目, 作答)。"""
    题 = 会话["questions"]
    if "i" in 参数 and 参数["i"] is not None:
        try:
            i = int(参数["i"])
        except (TypeError, ValueError):
            raise 参数错(f"i 要是整数（第几题，从 0 起），收到 {参数['i']!r}") from None
    elif 参数.get("id"):
        i = next((k for k, q in enumerate(题) if q.id == 参数["id"]), -1)
        if i < 0:
            raise 参数错(f"这场考试里没有题号 {参数['id']}")
    else:
        raise 参数错("缺 i（这是第几题，从 0 起）或 id（题号）")
    if not (0 <= i < len(题)):
        raise 参数错(f"i={i} 超出范围（0..{len(题) - 1}）")
    # ⚠ `choice`（1 起序号）可以**单独**给，此时不需要 answer 字段
    if 参数.get("choice") is not None:
        return i, 题[i], 参数.get("answer")
    if "answer" not in 参数:
        raise 参数错("缺 answer（选择题也可用 choice 传 1 起的选项序号）")
    return i, 题[i], 参数["answer"]


def _exam_answer(args: dict) -> dict:
    会话 = _取会话(args)
    i, q, 作答 = _答案项(会话, args)
    ok, given, note = _判一题(q, 作答, args.get("choice"))
    会话["answers"][i] = (ok, given, note)
    出 = {"session": _会话键(会话), "i": i, "id": q.id, "ok": ok, "given": given}
    if note:
        出["note"] = note
    if not ok:
        # 答对了不给订正（那会把答案直接送出去）；答错了才给"为什么"。
        出["explain"] = q.explain
        if q.type == "choice":
            出["correct"] = q.options[q.answer]
        elif q.expect:
            出["correct"] = q.expect
    出["answered"] = len(会话["answers"])
    出["total"] = len(会话["questions"])
    return 出


def _会话键(会话: dict) -> str:
    for k, v in _会话.items():
        if v is 会话:
            return k
    return ""


def _exam_answerAll(args: dict) -> dict:
    会话 = _取会话(args)
    ans = args.get("answers")
    if not isinstance(ans, dict):
        raise 参数错('缺 answers（形如 {"0": "答", "1": 2, "3": {"answer": "代码"}}）')
    结果 = []
    for k, v in ans.items():
        子 = {"session": _会话键(会话), "i": int(k)}
        if isinstance(v, dict):
            子.update(v)
            子["i"] = int(k)
        else:
            子["answer"] = v
        try:
            结果.append(_exam_answer(子))
        except 参数错 as e:
            结果.append({"i": int(k), "ok": False, "err": str(e)})
    return {"session": _会话键(会话), "results": 结果,
            "answered": len(会话["answers"]), "total": len(会话["questions"])}


def _exam_report(args: dict) -> dict:
    会话 = _取会话(args)
    att, 明细 = _成绩对象(会话)
    return {"session": _会话键(会话), "total": len(会话["questions"]),
            "answered": len(会话["answers"]),
            "correct": att.correct,
            "percent": round(att.correct / att.total * 100, 1) if att.total else 0.0,
            "byTopic": {t: {"correct": c, "total": n} for t, (c, n) in att.by_topic.items()},
            "wrongIds": att.wrong_ids,
            "seconds": round(att.seconds, 1)}


def _exam_grade(args: dict) -> dict:
    """出成绩 + **逐题订正**。报告文字复用 `examrun.render_report`。

    ⚠ 为什么复用：那份排版（按主题条形图、订正段、"为什么"）是产品的一部分，
      协议层自己再排一遍必然与命令行版长得不一样 —— 而"同一段客户代码驱动
      两个产品"的前提之一就是**同一个产品的行为不因入口而变**。
    """
    from .exam import Progress
    from .examrun import render_report

    会话 = _取会话(args)
    未答 = len(会话["questions"]) - len(会话["answers"])
    出 = _exam_report(args)
    出["ok"] = 未答 == 0 and 出["correct"] == 出["total"]
    if 未答:
        出["note"] = f"还有 {未答} 题没作答（未作答按错计）"

    att, 明细 = _成绩对象(会话)
    标题 = 会话.get("title") or ("考试" if 会话["mode"] == "exam" else "练习")
    出["cli"] = render_report(att, 明细, title=标题)

    # ⚠ 记录成绩默认 **False**：它会写 exams/成绩记录.json（全机共享的一份历史）。
    #   "看一眼分数"不该顺手改掉别人的成绩历史 —— 要记就显式说。
    if _布尔(args, "record", False):
        p = Progress(项目根() / "exams" / "成绩记录.json")
        p.record(att)
        出["recorded"] = True
        出["attempts"] = len(p.attempts)
        出["weakTopics"] = [{"topic": t, "rate": round(r, 3), "n": n}
                            for t, r, n in p.weak_topics()]
    else:
        出["recorded"] = False
    return 出


def _progress_get(args: dict) -> dict:
    from .exam import Progress
    p = Progress(项目根() / "exams" / "成绩记录.json")
    最近 = int(_数(args, "limit", 10, 最小=1, 最大=100))
    return {
        "attempts": len(p.attempts),
        "recent": [
            {"ts": a.ts, "when": time.strftime("%Y-%m-%d %H:%M", time.localtime(a.ts)),
             "mode": a.mode, "correct": a.correct, "total": a.total,
             "percent": round(a.correct / a.total * 100, 1) if a.total else 0.0,
             "seconds": round(a.seconds, 1), "wrongIds": a.wrong_ids}
            for a in p.attempts[-最近:]
        ],
        "byTopic": {t: {"correct": c, "total": n} for t, (c, n) in p.topic_stats().items()},
        "weakTopics": [{"topic": t, "rate": round(r, 3), "n": n}
                       for t, r, n in p.weak_topics()],
        "path": _路径串(p.path),
        "exists": p.path.exists(),
    }


def _progress_reset(args: dict) -> dict:
    """清空成绩历史。

    ⚠ 这是**破坏性**的（成绩历史是全机共享的一份文件），
      所以要求显式 `{"yes":true}` —— 与协议"不静默"同一条精神：
      危险操作要能被调用方**看见**，而不是默默发生。
    """
    if not _布尔(args, "yes", False):
        raise 参数错('progress.reset 会清空成绩历史（exams/成绩记录.json）。确认请传 {"yes":true}')
    p = 项目根() / "exams" / "成绩记录.json"
    had = p.exists()
    n = 0
    if had:
        import json as _json
        with contextlib.suppress(Exception):
            n = len(_json.loads(p.read_text(encoding="utf-8")).get("attempts", []))
        p.unlink()
    return {"reset": True, "existed": had, "removed": n, "path": _路径串(p)}


def _coach_hint(args: dict) -> dict:
    """"卡住了" —— 给**下一步动作**，不给答案。

    ⚠ Coach 的定位写在它自己的 docstring 里：它不是语言模型，
      它的价值是"把问题拆小、教你观察现象"。
      所以这条命令返回的是**追问清单**，而不是一段像模像样的解答。
    """
    from .coach import Coach
    q = args.get("question", args.get("问题"))
    if q is None or str(q).strip() == "":
        raise 参数错("缺 question（把卡住的地方用一句话说清）")
    出 = {"advice": Coach().问我(str(q))}
    课 = args.get("lesson", args.get("课"))
    if 课:
        line, p = _解课({"id": 课, **({"line": args["line"]} if args.get("line") else {})})
        出["lesson"] = {"line": line, "id": p.stem[:2], "title": _标题(p), "requires": _要做(p)}
    return 出


def _coach_review(args: dict) -> dict:
    """读一段代码，指出**容易出问题的地方**（不判断对错）。"""
    from .coach import Coach
    src = args.get("source", args.get("代码", args.get("src")))
    if src is None:
        raise 参数错("缺 source（要看的 Python 源码）")
    return {"review": Coach().看代码(str(src), str(args.get("lesson") or ""))}


def _code_check(args: dict) -> dict:
    """按**隐藏用例**判一段学生代码（对应 `examrun._grade_code`）。

    ⚠ 与 `exam.answer` 的分工：这条是"直接给我判，不用开会话"，
      用于自动化投放作业；`exam.answer` 是"考试中的一个动作"。
    """
    from .exam import load_banks, normalize
    from .examrun import _grade_code

    bank, _ = _题目池(args, 项目根())
    q = None
    if args.get("id"):
        q = next((x for x in bank if x.id == args["id"]), None)
        if q is None:
            raise 参数错(f"题库里没有题号 {args['id']}")
        if q.type != "code":
            # choice / output / fill 也能判 —— 复用同一套归一化与选项校验
            if "answer" not in args:
                raise 参数错("缺 answer")
            ok, given, note = _判一题(q, args["answer"])
            return {"id": q.id, "type": q.type, "ok": ok, "given": given,
                    "note": note, "expected": q.expect or None}
    if "expected" in args:
        if "answer" not in args:
            raise 参数错("给了 expected 就要同时给 answer")
        return {"ok": normalize(str(args["answer"])) == normalize(str(args["expected"])),
                "given": str(args["answer"]), "expected": str(args["expected"])}
    if q is None:
        raise 参数错("缺 id（题库里的题号，例如 \"基础-001\"）或 answer+expected")
    if "answer" not in args:
        raise 参数错("缺 answer（学生写的代码）")
    ok, note = _grade_code(q, str(args["answer"]))
    return {"id": q.id, "type": "code", "ok": ok, "note": note,
            "tests": len(q.tests), "explain": q.explain}


# ══════════════════════════════════════════════════════════════════════════
#  参数小工具
# ══════════════════════════════════════════════════════════════════════════

def _数(args: dict, 键: str, 缺省, 最小=None, 最大=None):
    v = args.get(键)
    if v is None:
        return 缺省
    try:
        v = float(v)
    except (TypeError, ValueError):
        raise 参数错(f"{键} 要是数字，收到 {v!r}") from None
    if 最小 is not None and v < 最小:
        raise 参数错(f"{键} 不能小于 {最小}")
    if 最大 is not None and v > 最大:
        raise 参数错(f"{键} 不能大于 {最大}")
    return int(v) if float(v).is_integer() else v


def _布尔(args: dict, 键: str, 缺省: bool) -> bool:
    v = args.get(键)
    if v is None:
        return 缺省
    if isinstance(v, bool):
        return v
    if isinstance(v, (int, float)):
        return bool(v)
    if isinstance(v, str):
        s = v.strip().lower()
        if s in ("1", "true", "yes", "y", "on", "是", "真"):
            return True
        if s in ("0", "false", "no", "n", "off", "否", "假"):
            return False
    raise 参数错(f"{键} 要是 true/false，收到 {v!r}")


# ══════════════════════════════════════════════════════════════════════════
#  命令表（自省就从这里生成 —— `ctl.commands` 不会与实现脱节）
# ══════════════════════════════════════════════════════════════════════════

COMMANDS: dict[str, dict] = {
    # ── 协议层（与专业版逐字相同）──────────────────────────────────────
    "emu.ping": {
        "fn": _emu_ping,
        "summary": "活着吗（协议层。⚠ 响应含 pid，用于确认进程没被换掉）",
        "args": {},
    },
    "emu.info": {
        "fn": _emu_info,
        "summary": "产品自述。⚠ product 字段让同一段客户端代码区分两个产品",
        "args": {},
    },
    "ctl.commands": {
        "fn": _ctl_commands,
        "summary": "自省：本产品支持哪些命令（由命令表生成，不会与实现脱节）",
        "args": {},
    },
    "emu.quit": {
        "fn": _emu_quit,
        "summary": "结束会话并退出进程",
        "args": {},
    },
    # ── 课程（对应 hybridengine_light/run.py + check.py）────────────────────────
    "lesson.list": {
        "fn": _lesson_list,
        "summary": "课程线与课程列表（真读目录，answered 表示答题文件在不在）",
        "args": {"line": "课程线（python/embedded/raspi/ai，缺省=全部）"},
    },
    "lesson.show": {
        "fn": _lesson_show,
        "summary": "看某一课：标题 + 「要做」+ 课程全文",
        "args": {"id": "课号（\"01\"），也可写 \"嵌入式 01\"", "line": "课程线"},
    },
    "lesson.answer": {
        "fn": _lesson_answer,
        "summary": "参考答案（读 *-solutions/，⚠ 不是学生的 *-answers/）",
        "args": {"id": "课号", "line": "课程线"},
    },
    "lesson.check": {
        "fn": _lesson_check,
        "summary": "跑这一课的 CHECK_* 检查，给逐条结构化结果",
        "args": {"id": "课号", "line": "课程线"},
    },
    "lesson.run": {
        "fn": _lesson_run,
        "summary": "**真起子进程**跑你写好的答案脚本，收回 stdout/stderr/退出码",
        "args": {"id": "课号", "line": "课程线", "path": "或直接给一个脚本路径",
                 "timeout": "秒，缺省 10"},
    },
    # ── 代码（对应 run.py 的 _load_answer / check.run_checks 之外的裸跑）──
    "code.run": {
        "fn": _code_run,
        "summary": "跑一段 Python 源码：收 stdout/stderr，报错带**人话翻译**",
        "args": {"source": "要跑的源码"},
    },
    "code.check": {
        "fn": _code_check,
        "summary": "按隐藏用例判一段代码（对应 examrun._grade_code）",
        "args": {"id": "题库题号", "answer": "学生写的代码/答案",
                 "expected": "（不给 id 时）直接比对的正确答案"},
    },
    # ── 考试（对应 exam.py + examrun.py + cmd_exam.py）──────────────────
    "exam.list": {
        "fn": _exam_list,
        "summary": "题库概览：题数、主题、考级元数据，以及**题库自检结果**",
        "args": {},
    },
    "exam.start": {
        "fn": _exam_start,
        "summary": "起一场考试会话（⚠ 返回的题面**不含答案**；seed 可复现抽题）",
        "args": {"n": "题量，缺省 10", "topic": "只考某主题", "exam": "只考某门考级题库",
                 "mode": "practice/exam", "seed": "随机种子（可复现）"},
    },
    "exam.answer": {
        "fn": _exam_answer,
        "summary": "答一题并**立即判分**（答错才给「为什么」；答对不给，避免泄题）",
        "args": {"session": "会话 id", "i": "第几题（0 起）", "id": "或题号",
                 "answer": "choice=**0 起下标**或选项原文；output/fill=文字；code=源码",
                 "choice": "或按人看的序号（1 起）—— 与 answer 下标不重叠"},
    },
    "exam.answerAll": {
        "fn": _exam_answerAll,
        "summary": "一次交多题：{\"0\": \"答案\", \"1\": 2, ...}",
        "args": {"session": "会话 id", "answers": "下标 → 作答"},
    },
    "exam.report": {
        "fn": _exam_report,
        "summary": "当前得分与按主题的正确率（不写成绩历史）",
        "args": {"session": "会话 id"},
    },
    "exam.grade": {
        "fn": _exam_grade,
        "summary": "出成绩 + **逐题订正**（复用 examrun 的报告排版）",
        "args": {"session": "会话 id", "record": "true 才写成绩历史，缺省 false"},
    },
    # ── 进度（对应 exam.Progress）──────────────────────────────────────
    "progress.get": {
        "fn": _progress_get,
        "summary": "成绩历史与弱项（弱项 = 正确率<70% 且样本≥3）",
        "args": {"limit": "返回最近几次，缺省 10"},
    },
    "progress.reset": {
        "fn": _progress_reset,
        "summary": "⚠ 清空成绩历史（破坏性，必须显式 {\"yes\":true}）",
        "args": {"yes": "确认"},
    },
    # ── 助手（对应 coach.py）───────────────────────────────────────────
    "coach.hint": {
        "fn": _coach_hint,
        "summary": "卡住了？给**下一步动作与追问**，不给答案（Coach.问我）",
        "args": {"question": "把卡住的地方说清", "lesson": "可选课号"},
    },
    "coach.review": {
        "fn": _coach_review,
        "summary": "读一段代码指出常见坑（死循环/input 没转 int/…，不判断对错）",
        "args": {"source": "源码", "lesson": "可选课号（带课程语境）"},
    },
}

# `命令表` 是给中文读者的别名 —— 两个名字指向同一个 dict。
命令表 = COMMANDS

# 不用 `ctl.commands` 列出的别名（协议整洁：自省只报规范名）。
# ⚠ 为什么还要有：命令行那套是中文的（`python -m hybridengine_light 嵌入式 检查 01`），
#   中文调用方会顺手写 `课程.列表`。接了是零成本，不接是困惑。
别名: dict[str, str] = {
    "课.列表": "lesson.list", "课程.列表": "lesson.list",
    "课.看": "lesson.show", "课程.看": "lesson.show",
    "课.答案": "lesson.answer", "课程.答案": "lesson.answer",
    "课.检查": "lesson.check", "课程.检查": "lesson.check",
    "课.运行": "lesson.run", "课程.运行": "lesson.run",
    "代码.运行": "code.run", "代码.检查": "code.check",
    "题库.列表": "exam.list",
    "考试.开始": "exam.start", "考试.作答": "exam.answer",
    "考试.交卷": "exam.grade", "考试.成绩": "exam.report",
    "成绩.查看": "progress.get", "成绩.清空": "progress.reset",
    "助手.提示": "coach.hint", "助手.看代码": "coach.review",
}


def 命令名列表() -> list[str]:
    """规范命令名（不含别名）—— 与专业版的 `ctl.commands` 同一形状。"""
    return list(COMMANDS)


# ══════════════════════════════════════════════════════════════════════════
#  协议循环
# ══════════════════════════════════════════════════════════════════════════

def _分派(req) -> tuple[bool, dict | None, str, int]:
    """处理一条**已解析**的请求。返回 (ok, r, errMsg, errCode)。

    ⚠ 这个函数**不抛异常** —— 任何异常都在这里变成 `ok:false`。
      "不静默"这条约定落实的地方就是这里。
    """
    global _当前错误码
    if not isinstance(req, dict):
        _当前错误码 = 错误码["参数错"]
        return False, None, f"请求必须是 JSON 对象，收到 {type(req).__name__}", _当前错误码

    cmd = req.get("cmd")
    if cmd is None:
        _当前错误码 = 错误码["参数错"]
        return False, None, '缺 cmd 字段（形如 {"id":1,"cmd":"emu.ping"}）', _当前错误码
    if not isinstance(cmd, str) or not cmd.strip():
        _当前错误码 = 错误码["参数错"]
        return False, None, f"cmd 必须是非空字符串，收到 {cmd!r}", _当前错误码
    cmd = cmd.strip()

    args = req.get("args")
    if args is None:
        args = {}
    if not isinstance(args, dict):
        _当前错误码 = 错误码["参数错"]
        return False, None, f"args 必须是对象，收到 {type(args).__name__}", _当前错误码

    规范 = 别名.get(cmd, cmd)
    条目 = COMMANDS.get(规范)
    if 条目 is None:
        # ⚠ 与专业版逐字相同的措辞与错误码
        _当前错误码 = 错误码["未知命令"]
        return False, None, f"未知命令: {cmd}", _当前错误码

    try:
        r = 条目["fn"](args)
    except 参数错 as e:
        _当前错误码 = 错误码["参数错"]
        return False, None, str(e), _当前错误码
    except Exception as e:                              # noqa: BLE001
        # ⚠ 不静默：内部错也要说清楚是什么错、在哪
        _当前错误码 = 错误码["内部错"]
        位置 = traceback.extract_tb(e.__traceback__)
        哪 = f"（{位置[-1].filename.split(os.sep)[-1]}:{位置[-1].lineno}）" if 位置 else ""
        return False, None, f"{规范} 内部错：{type(e).__name__}: {e}{哪}", _当前错误码

    if not isinstance(r, dict):
        _当前错误码 = 错误码["内部错"]
        return False, None, f"{规范} 返回了非对象（{type(r).__name__}）—— 这是本产品的 bug", _当前错误码
    return True, r, "", 0


def 处理一行(行: str) -> tuple[int, bool, dict | None, str, int]:
    """处理一行请求文本。返回 (id, ok, r, errMsg, errCode)。

    ⚠ `id` 回显的兜底：请求不是 JSON / 不是对象时，专业版的 `id` 是 **0**
      （main.cpp 里 `long long id = 0;`）。这里保持一致。
    """
    try:
        req = json.loads(行)
    except ValueError as e:
        文本 = 行.strip()
        提示 = f"请求不是合法 JSON: {e}"
        if len(文本) > 60:
            提示 += f"（收到：{文本[:60]}…）"
        else:
            提示 += f"（收到：{文本}）"
        return 0, False, None, 提示, 错误码["参数错"]
    rid = req.get("id") if isinstance(req, dict) else None
    id_ = rid if isinstance(rid, int) and not isinstance(rid, bool) else (
        rid if isinstance(rid, str) else 0)
    ok, r, msg, code = _分派(req)
    return id_, ok, r, msg, code


def run_stdio(输入=None, 输出fd: int | None = None) -> int:
    """协议循环：一行请求 → 一行响应，直到 EOF 或 `emu.quit`。

    ⚠ 逐字节读而不是 `for line in stdin`：
       `for line in` 会**预读**（内部缓冲），而这里必须"收一行、回一行"，
       否则调用方按同步读写会死等（专业版同样用 `fgetc` 一个字符一个字符读）。
    """
    global _当前错误码
    if 输出fd is not None:
        global _协议fd
        _协议fd = 输出fd

    流入 = 输入 if 输入 is not None else sys.stdin
    二进制 = getattr(流入, "buffer", None)
    缓冲 = bytearray()
    while True:
        try:
            if 二进制 is not None:
                c = 二进制.read(1)
            else:
                c = 流入.read(1)
        except (OSError, ValueError):
            break
        if c in (b"", ""):
            break                                    # EOF
        if isinstance(c, bytes):
            if c != b"\n":
                缓冲 += c
                continue
        else:
            if c != "\n":
                缓冲 += c.encode("utf-8", "surrogateescape")
                continue
        行 = bytes(缓冲).decode("utf-8", "replace")
        缓冲.clear()
        if 行.strip() == "":
            continue                                 # 空行跳过（与专业版一致）
        try:
            id_, ok, r, msg, code = 处理一行(行)
        except Exception as e:                       # noqa: BLE001 —— 最后一道兜底
            id_, ok, r, msg, code = 0, False, None, f"协议循环内部错: {e}", 错误码["内部错"]
        _当前错误码 = code
        _写响应(id_, ok, r, msg)
        # ⚠ `emu.quit` 才退出。**先解析再判**，不用文本匹配 ——
        #   否则 `{"cmd":"code.run","args":{"source":"print('\"emu.quit\"')"}}`
        #   这种请求会让进程提前退出（"看起来能用、其实是个洞"的典型）。
        if ok and _是退出请求(行):
            return 0
    return 0


def _是退出请求(行: str) -> bool:
    try:
        req = json.loads(行)
    except ValueError:
        return False
    return isinstance(req, dict) and req.get("cmd") in ("emu.quit",)


# ══════════════════════════════════════════════════════════════════════════
#  命令行
# ══════════════════════════════════════════════════════════════════════════

用法 = """hybridengine_light-ctl —— HybridEngine-light 的控制接口（与专业版同一套协议）

    hybridengine_light-ctl [--stdio] [--product=<name>] [--commands] [--version]

默认走 **stdio**：一行一条 JSON 请求，一行一条响应。例如：

    {"id":1,"cmd":"emu.ping"}
    {"id":2,"cmd":"emu.info"}
    {"id":3,"cmd":"lesson.list","args":{"line":"ai"}}
    {"id":4,"cmd":"emu.quit"}

手工试：
    echo {"id":1,"cmd":"emu.info"} | python -m hybridengine_light.ctl
或（PowerShell）：
    '{"id":1,"cmd":"emu.info"}' | python -m hybridengine_light.ctl

⚠ 响应走 **stdout**，日志一律 stderr —— 所以 `2>nul` 不会影响协议。
⚠ 不会弹窗：本模块不 import tkinter。
"""


def main(argv: list[str] | None = None) -> int:
    argv = list(sys.argv[1:] if argv is None else argv)

    if any(a in ("-h", "--help", "帮助") for a in argv):
        sys.stdout.write(用法)
        return 0
    if any(a in ("--version", "-V") for a in argv):
        from . import __version__
        sys.stdout.write(f"hybridengine_light-ctl {__version__} (protocol {协议版本})\n")
        return 0

    # ⚠ 参数与专业版**同名同义**（见协议文档第四节）：
    #     --stdio（默认）  --product=<name>  --commands=<json 文件>
    #   `--commands` 在专业版是"从 JSON 载入命令描述"（数据驱动）。
    #   ⚠ 本产品**没有**复制那个设计，原因写在 docs/控制协议.md 的"未做的部分"：
    #     HybridEngine-light 的命令是**函数**（要判分、要读文件），不是"按签名调一个 C ABI"，
    #     所以数据驱动的收益拿不到，反而会把命令表与实现分叉。
    #     这里保留这个开关并**给出明确答复**，而不是假装支持。
    未知 = [a for a in argv if a.startswith("-") and a not in ("--stdio",)]
    for a in argv:
        if a.startswith("--product=") or a.startswith("--commands="):
            未知 = [x for x in 未知 if x != a]
    if 未知:
        sys.stderr.write(f"hybridengine_light-ctl: 未识别的选项：{' '.join(未知)}\n\n{用法}")
        return 2

    for a in argv:
        if a.startswith("--product="):
            名字 = a.split("=", 1)[1]
            if 名字 and 名字 not in ("hybridengine_light",):
                # ⚠ 不静默：说清这个开关在本产品里意味着什么
                sys.stderr.write(
                    f"hybridengine_light-ctl: --product={名字} 被忽略 —— "
                    f"本可执行文件的产品身份固定是 hybridengine_light"
                    f"（emu.info 的 product 字段就是它）。\n")
    for a in argv:
        if a.startswith("--commands="):
            路径 = a.split("=", 1)[1]
            sys.stderr.write(
                f"hybridengine_light-ctl: --commands={路径} 未支持 —— 本产品的命令是函数，"
                f"不经数据驱动；命令表在 hybridengine_light/ctl.py 的 COMMANDS，"
                f"用 ctl.commands 查询。\n")

    _协议通道()
    return run_stdio()


if __name__ == "__main__":
    sys.exit(main())
