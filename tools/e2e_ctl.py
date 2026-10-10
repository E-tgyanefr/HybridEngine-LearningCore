#!/usr/bin/env python3
"""hybridengine_light-ctl 的端到端测试 —— **可重复运行**，只用标准库。

═══════════════════════════════════════════════════════════════════════════
它验什么（对应 HybridEngine/docs/控制协议.md 的判据 + 本次任务的判据）
═══════════════════════════════════════════════════════════════════════════

  ① **起进程 → ping → info → 未知命令 → quit**
     未知命令必须 `ok:false` 且 `err.msg` **非空**（"不静默"）
  ② `ctl.commands` 列出的每一条命令**都能被调用**（逐个冒烟，不静默）
  ③ **一行一响应**：每条响应都是一行合法 JSON，`id` 逐条回显
  ④ **stdout 只跑协议**：请求里带 `print` 的学生代码不能污染协议流
     （专业版被引擎日志坑过，见 main.cpp 的 fd 重定向）
  ⑤ **跨产品判据**：同一段客户端代码（本文件的 `Ctl`）驱动**两个产品**，
     靠 `emu.info` 的 `product` 区分 —— 这是本任务的核心判据
  ⑥ 断言的**反面**：故意答错一题，必须判错 —— 否则"全对"什么也证明不了

═══════════════════════════════════════════════════════════════════════════
怎么用
═══════════════════════════════════════════════════════════════════════════

    python tools/e2e_ctl.py                  # 只测 hybridengine_light-ctl
    python tools/e2e_ctl.py --ms-ctl <路径>  # 顺带跑跨产品那条判据
    python tools/e2e_ctl.py --no-cross       # 明确不跑跨产品（会说明为什么）

⚠ 零弹窗：本脚本给子进程设 `HYBRIDENGINE_LIGHT_NO_DISPLAY=1`，
   而 `hybridengine_light-ctl` 自己也不 import tkinter —— 画布那条线不会被碰到。
⚠ stderr 一律 **DEVNULL**（与 professional 的 `tools/msctl.py` 同一个做法）：
   两个产品的日志都走 stderr，协议只走 stdout。
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import unittest
from pathlib import Path

根 = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(根))

默认_MS_CTL = 根.parent / "HybridEngine" / "build-ninja-release" / "tools" / "ms_ctl.exe"


# ══════════════════════════════════════════════════════════════════════════
#  客户端 —— ⚠ 这一段是判据⑤ 的主角：**它不关心对面是哪个产品**
# ══════════════════════════════════════════════════════════════════════════

class CtlError(RuntimeError):
    pass


class Ctl:
    """一个常驻控制进程的句柄（形状照 `HybridEngine/tools/msctl.py` 的 `Ctl`）。

    ⚠ 这是"同一段客户端代码"的实体：它只认协议（`id`/`cmd`/`ok`/`r`/`err`），
      **不认产品**。产品身份是运行期从 `emu.info` 读出来的。
    """

    def __init__(self, argv: list[str], cwd: Path | None = None, env_extra: dict | None = None):
        env = dict(os.environ)
        env["PYTHONIOENCODING"] = "utf-8"
        env["HYBRIDENGINE_LIGHT_NO_DISPLAY"] = "1"          # ⚠ 兜底：绝不让画布建窗口
        env.update(env_extra or {})
        self.argv = argv
        self.p = subprocess.Popen(
            argv, cwd=str(cwd or 根),
            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL,
            text=True, encoding="utf-8", env=env, bufsize=1)
        self._id = 0
        self.产品 = None

    # ── 上下文管理 ──
    def __enter__(self):
        return self

    def __exit__(self, *a):
        try:
            self.quit()
        except Exception:                              # noqa: BLE001
            pass
        try:
            self.p.stdin.close()
        except Exception:                              # noqa: BLE001
            pass
        try:
            self.p.wait(timeout=10)
        except Exception:                              # noqa: BLE001
            self.p.kill()

    # ── 协议 ──
    def 发(self, cmd: str, **args) -> dict:
        """发一条命令；失败**抛异常**（不静默 —— 与协议约定一致）。

        ⚠ 返回的是 `r` 本体；要错误对象用 `发原文`。
        """
        r = self.发原文(cmd, **args)
        if not r.get("ok"):
            raise CtlError(f"{cmd}: {r.get('err', {}).get('msg', '?')}")
        return r.get("r", {})

    def 发原文(self, cmd: str, **args) -> dict:
        """发一条命令，返回**整条响应**（含 ok/err）—— 测"该失败时失败了"要用它。"""
        self._id += 1
        rid = self._id
        req = {"id": rid, "cmd": cmd}
        if args:
            req["args"] = args
        self.p.stdin.write(json.dumps(req, ensure_ascii=False) + "\n")
        self.p.stdin.flush()
        line = self.p.stdout.readline()
        if not line:
            raise CtlError(f"进程提前退出（cmd={cmd}，exit={self.p.poll()}）")
        if not line.endswith("\n"):
            raise CtlError(f"响应不是一行（没有换行结尾）：{line!r}")
        r = json.loads(line)                           # ⚠ 解析失败 = 协议流被污染了
        if r.get("id") != rid:
            raise CtlError(f"id 没回显：发 {rid}，收到 {r.get('id')!r}")
        if not isinstance(r.get("ok"), bool):
            raise CtlError(f"响应缺 ok 布尔字段：{r!r}")
        if not r["ok"]:
            err = r.get("err") or {}
            if not isinstance(err.get("msg"), str) or not err["msg"].strip():
                raise CtlError("失败响应没有非空 err.msg（违反「不静默」）：" + repr(r))
        return r

    def quit(self) -> dict:
        return self.发原文("emu.quit")


def hybridengine_light_ctl() -> list[str]:
    """怎么起 hybridengine_light-ctl —— 两种方式都行，这里走**安装路径无关**的那个。"""
    return [sys.executable, "-m", "hybridengine_light.ctl"]


# ══════════════════════════════════════════════════════════════════════════
#  ① 协议层：起进程 → ping → info → 未知命令 → quit
# ══════════════════════════════════════════════════════════════════════════

class Test协议层(unittest.TestCase):

    def test_ping_info_未知命令_quit(self):
        with Ctl(hybridengine_light_ctl()) as c:
            ping = c.发("emu.ping")
            self.assertTrue(ping["pong"], "emu.ping 应该回 pong:true")
            self.assertIsInstance(ping.get("pid"), int, "ping 应带上 pid")

            info = c.发("emu.info")
            self.assertEqual(info["product"], "hybridengine_light")
            self.assertTrue(info.get("version"), "emu.info 必须给出 version")
            # ⚠ 协议文档 §3.1 明确把 product 定为"调用方区分两个产品"的依据
            self.assertNotEqual(info["product"], "hybridengine",
                                "两个产品的 product 必须不同，否则区分不了")

            # ── 未知命令：这两条断言就是判据本身 ──
            r = c.发原文("绝对不存在的命令.xyz")
            self.assertFalse(r["ok"], "未知命令必须 ok:false")
            self.assertIn("err", r)
            self.assertEqual(r["err"]["code"], -1, "未知命令的错误码应与专业版一致（-1）")
            self.assertTrue(str(r["err"]["msg"]).strip(), "⚠ 不静默：err.msg 必须非空")
            self.assertIn("未知命令", r["err"]["msg"])

            # 中文未知命令（本次任务的判据原文）
            r2 = c.发原文("不存在")
            self.assertFalse(r2["ok"])
            self.assertEqual(r2["err"]["msg"], "未知命令: 不存在")

            r3 = c.quit()
            self.assertTrue(r3["ok"], "emu.quit 必须 ok:true")

    def test_quit后进程真的退出(self):
        c = Ctl(hybridengine_light_ctl())
        try:
            c.quit()
            code = c.p.wait(timeout=10)
            self.assertEqual(code, 0, "正常 quit 应以退出码 0 结束")
        finally:
            with __import__("contextlib").suppress(Exception):
                c.p.kill()

    def test_坏请求不静默(self):
        """不是 JSON / 缺 cmd / args 不是对象 —— 三种都要有一个明确的错。"""
        with Ctl(hybridengine_light_ctl()) as c:
            for 坏 in ('这不是 JSON', '[1,2,3]', '{"id":9}', '{"id":9,"cmd":"emu.ping","args":5}'):
                c.p.stdin.write(坏 + "\n")
                c.p.stdin.flush()
                line = c.p.stdout.readline()
                self.assertTrue(line, f"对坏请求 {坏!r} 没有响应")
                r = json.loads(line)
                self.assertFalse(r["ok"], f"{坏!r} 应该失败")
                self.assertTrue(str(r["err"]["msg"]).strip(),
                                f"{坏!r} 的 err.msg 不能为空（不静默）")

    def test_空行被跳过而不是当成空请求(self):
        with Ctl(hybridengine_light_ctl()) as c:
            c.p.stdin.write("\n\n")
            c.p.stdin.flush()
            self.assertTrue(c.发("emu.ping")["pong"], "空行之后协议应该还在正常工作")


# ══════════════════════════════════════════════════════════════════════════
#  ② 自省：ctl.commands 列出的命令**都能被调用**
# ══════════════════════════════════════════════════════════════════════════

class Test自省(unittest.TestCase):

    def test_commands与实现不分叉(self):
        """⚠ 自省表的每一条都必须真在命令表里（不是文档抄来的）。"""
        from hybridengine_light import ctl
        with Ctl(hybridengine_light_ctl()) as c:
            r = c.发("ctl.commands")
            self.assertIn("commands", r)
            self.assertTrue(r["commands"], "ctl.commands 不能是空列表")
            for 名 in r["commands"]:
                self.assertIn(名, ctl.COMMANDS, f"{名} 列在自省里但不在命令表里")
            self.assertEqual(set(r["commands"]), set(ctl.COMMANDS),
                             "自省列表与命令表必须完全一致")
            # 协议层四条必须在（协议文档 §3）
            for 必须 in ("emu.ping", "emu.info", "emu.quit", "ctl.commands"):
                self.assertIn(必须, r["commands"])

    def test_每条命令都能被调用至少一次(self):
        """逐个冒烟 —— 判据原文："ctl.commands 列出的命令都能被调用"。

        ⚠ 冒烟用**真实参数**（否则只是测了"参数校验会不会拒绝"）。
        ⚠⚠ 顺序不是 `ctl.commands` 的顺序 —— `emu.quit` 在自省表里排第 4，
           照那个顺序冒烟，第一条就把进程关掉了，后面的命令全部
           "进程提前退出"（这个坑我自己踩了一次：第一次跑就报
           `进程提前退出（cmd=lesson.list）`）。
           所以：**先按自省顺序跑完所有其它命令，emu.quit 放最后**。
        """
        from hybridengine_light import ctl
        with Ctl(hybridengine_light_ctl()) as c:
            命令 = c.发("ctl.commands")["commands"]
            冒烟参数 = _冒烟参数(c)
            没冒烟 = [n for n in 命令 if n not in 冒烟参数]
            self.assertEqual(没冒烟, [], f"这些命令没有冒烟参数：{没冒烟}")

            顺序 = [n for n in 命令 if n != "emu.quit"] + (
                ["emu.quit"] if "emu.quit" in 命令 else [])
            结果 = {}
            for 名 in 顺序:
                r = c.发原文(名, **冒烟参数[名])
                结果[名] = r
                if not r["ok"]:
                    # ⚠ 允许"参数错"，但**必须**给出 msg（不静默），且不能是"未知命令"
                    err = r["err"]
                    self.assertTrue(str(err["msg"]).strip(), f"{名} 失败但 err.msg 为空")
                    self.assertNotIn("未知命令", err["msg"],
                                     f"{名} 在自省表里，却又被当成未知命令")
                    self.assertNotEqual(err["code"], ctl.错误码["内部错"],
                                        f"{名} 冒烟时内部错：{err['msg']}")
                    self.assertNotEqual(err["code"], ctl.错误码["未知命令"],
                                        f"{名} 冒烟时被当成未知命令")
            self.assertEqual(set(结果), set(命令))
            # emu.quit 之后进程必须已经退出（不是"回了 ok 但还赖着"）
            self.assertIsNotNone(c.p.wait(timeout=10), "emu.quit 之后进程没退出")


def _冒烟参数(c: Ctl) -> dict:
    """给每条命令准备一份**真实可用**的参数（冒烟不是测参数校验）。"""
    from hybridengine_light import ctl, exam

    # 真实题号（用于 code.check / exam.answer 的正例）
    bank, _ = exam.load_banks(根)
    选择题 = next(q for q in bank if q.type == "choice")

    # 一个考试会话（seed 固定 ⇒ 可复现）
    会话 = c.发("exam.start", n=3, seed=20240607)
    第一题 = 会话["questions"][0]
    该题的作答 = _查答案(bank, 第一题)

    会话2 = c.发("exam.start", n=3, seed=20240607)   # 给 answerAll 用，别污染会话1
    答2 = {str(i): _查答案(bank, q) for i, q in enumerate(会话2["questions"])}

    # 一个真跑得通的答案脚本（lesson.run 用）—— 优先用参考答案，它一定跑得通
    可跑的课 = _找可跑的课()

    参数 = {
        "emu.ping": {},
        "emu.info": {},
        "ctl.commands": {},
        "lesson.list": {"line": "ai"},
        "lesson.show": {"id": "ai 01"},
        "lesson.answer": {"id": "ai 01"},
        "lesson.check": {"id": "ai 01"},
        "code.run": {"source": "print('冒烟')\n1/0\n"},
        # ⚠ code.check 走 answer+expected 那条路（理由见 `_代码题的正确解`）
        "code.check": {"answer": "3", "expected": "3"},
        "exam.list": {},
        "exam.start": {"n": 2, "seed": 1},
        "exam.answer": {"session": 会话["session"], "i": 0, "answer": 该题的作答},
        "exam.answerAll": {"session": 会话2["session"], "answers": 答2},
        "exam.report": {"session": 会话["session"]},
        "exam.grade": {"session": 会话["session"]},      # record 缺省 false，不动成绩历史
        "progress.get": {"limit": 3},
        # ⚠ `progress.reset` 是破坏性的（会删掉成绩历史）。
        #   冒烟**故意不满足它的前置条件**（不给 yes）—— 这样它必须回"参数错"，
        #   而"回参数错"正好证明了两件事：
        #     ① 安全闸门真的在（不是文档里写写）
        #     ② 被拒时依然"不静默"（有非空 err.msg）
        #   ⇒ "能调用"不等于"必须成功"：对破坏性命令，**能被正确拒绝**才是对的行为。
        "progress.reset": {},
        "coach.hint": {"question": "我的循环停不下来"},
        "coach.review": {"source": "a = input()\nb = a + 1\n"},
    }
    if 可跑的课 is not None:
        # ⚠ 直接给 path 跑**参考答案**：学生目录里可能还是空模板，
        #   跑它会抛 NotImplementedError —— 那测的是"作业没写"，不是 lesson.run。
        参数["lesson.run"] = {"path": str(可跑的课), "timeout": 20}
    else:
        # ⚠ 不静默：宁可让测试**失败并说清原因**，也不能悄悄少冒烟一条
        参数["lesson.run"] = {"id": "01"}

    # ⚠ `emu.quit` 必须在**最后**：它之后进程就没了
    参数["emu.quit"] = {}
    顺序 = [n for n in ctl.COMMANDS if n != "emu.quit"] + ["emu.quit"]
    return {n: 参数[n] for n in 顺序}


def _代码题的正确解(q) -> str:
    """⚠ 本脚本**不倒推**代码题的正确解。

    理由："隐藏用例"是闭包（`lambda ns: ...`），从外面看不出它要什么；
    硬猜一个"看起来对"的实现，会让这条正例变成**假通过**
    （猜错了还被判对 = 测试在骗人）。

    ⇒ 所以 code.check 的正例走 `answer + expected` 那条路（见 `_冒烟参数`），
      而"真的执行学生函数跑隐藏用例"这条路，由 `exam.answer` 的 code 题
      与 `tests/test_exam.py` 覆盖。
    """
    return ""


def _查答案(bank, 题面: dict):
    """按题面（不含答案的那份）回查**标准答案** —— 测试可以看答案，客户端不行。

    ⚠ 选择题返回**下标**（与 `answer` 的"0 起下标"约定一致）。
      一开始我返回了选项原文，结果撞上 `_判一题` 旧版那套"1 起/0 起"的猜测，
      连带把一个真的歧义 bug 暴露出来了 —— 那 30 分钟没白花。
    """
    q = next(x for x in bank if x.id == 题面["id"])
    if q.type == "choice":
        return q.answer
    if q.type == "code":
        return q.starter or ""
    return q.expect


def _找可跑的课():
    """找一个"真的能跑通"的答案文件，给 lesson.run 冒烟用。

    ⚠ 优先看 `*-solutions/`（参考答案，一定存在且一定能跑），
      再看 `*-answers/`（学生目录，可能还是空模板 —— 跑起来会
      `NotImplementedError`，那不是 lesson.run 的问题，是"还没写作业"）。
    """
    from hybridengine_light import ctl
    for line, 元 in ctl.课程线.items():
        for p in ctl._课程们(line):
            候选 = []
            参考 = 根 / 元["solutions"]
            学生 = 根 / 元["answers"]
            if line == "python":
                候选 += [参考 / p.stem / "main.py", 参考 / p.name]
            else:
                候选 += [参考 / p.name]
            for f in 候选:
                if f.exists():
                    return f
            if line == "python":
                候选 = [学生 / p.stem / "main.py", 学生 / p.name]
            else:
                候选 = [学生 / p.name]
            for f in 候选:
                if f.exists() and "NotImplementedError" not in f.read_text(
                        encoding="utf-8", errors="replace"):
                    return f
    return None


# ══════════════════════════════════════════════════════════════════════════
#  ③④ 协议流的纯净性（专业版被这件事坑过）
# ══════════════════════════════════════════════════════════════════════════

class Test协议流纯净(unittest.TestCase):

    def test_学生代码的print不会污染协议流(self):
        """⚠ 这条对应专业版 main.cpp 里那段 fd 重定向。

        专业版是**引擎**在往 stdout 打日志；轻量版的污染源更直白：
        `code.run` 要跑学生代码，而学生代码里就有 print。
        如果那些 print 混进 stdout，下面第一句 `json.loads` 就会炸。
        """
        with Ctl(hybridengine_light_ctl()) as c:
            r = c.发("code.run", source="print('我是学生代码')\nprint('第二行')\n")
            self.assertIn("我是学生代码", r["stdout"], "stdout 抓取失效")
            # ⚠ 关键：紧接着的协议请求必须还是干净的 JSON 一行
            self.assertTrue(c.发("emu.ping")["pong"],
                            "学生代码的 print 之后协议流就被污染了")

    def test_报错也走协议不含原始print(self):
        with Ctl(hybridengine_light_ctl()) as c:
            r = c.发("code.run", source="print('先打印')\n1/0\n")
            self.assertFalse(r["ok"])
            self.assertIn("ZeroDivisionError", r["error"])
            self.assertIn("先打印", r["stdout"])
            self.assertIn("除数", r["explain"], "⚠ 报错必须带本产品的人话翻译")
            self.assertTrue(c.发("emu.ping")["pong"])


# ══════════════════════════════════════════════════════════════════════════
#  ⑥ 考试闭环：正面 + **反面**（能失败才算判据）
# ══════════════════════════════════════════════════════════════════════════

class Test考试闭环(unittest.TestCase):

    def test出题作答判分订正(self):
        from hybridengine_light import exam
        bank, _ = exam.load_banks(根)
        with Ctl(hybridengine_light_ctl()) as c:
            会话 = c.发("exam.start", n=6, seed=20240607, mode="practice")
            self.assertIn("session", 会话)
            self.assertEqual(len(会话["questions"]), 6)
            for q in 会话["questions"]:
                # ⚠ 泄题检查：题面里绝不能出现答案字段
                for 泄题 in ("answer", "expect", "tests", "explain", "options_answer"):
                    if 泄题 == "options":
                        continue
                    self.assertNotIn(泄题, q, f"题面泄露了 {泄题}：{q.get('id')}")
                self.assertIn("q", q)

            # ── 全部答对 ──
            for i, 题面 in enumerate(会话["questions"]):
                r = c.发("exam.answer", session=会话["session"], i=i,
                         answer=_查答案(bank, 题面))
                self.assertTrue(r["ok"], f"第 {i} 题（{题面['id']}）标准答案被判错了：{r}")
                self.assertNotIn("explain", r, "答对了不该给「为什么」（那会泄题）")
            成绩 = c.发("exam.grade", session=会话["session"])
            self.assertEqual(成绩["correct"], 成绩["total"])
            self.assertEqual(成绩["percent"], 100.0)
            self.assertTrue(成绩["ok"])
            self.assertFalse(成绩["recorded"], "缺省不该写成绩历史")
            self.assertIn("订正", 成绩["cli"])

    def test选择题下标的两种写法(self):
        """⚠ 这条是回归测试 —— 它对应的 bug **真的发生过**。

        初版 `_判一题` 让 `answer` 同时兼容"0 起下标"和"1 起序号"，
        靠 `1 <= 值 <= n` 猜是哪一种。n=4 时传 3 被判成"第 3 个选项"，
        而调用方说的是"下标 3" —— **中间的区间猜不了**，两边都觉得自己对。

        修法：`answer` 一律 0 起下标；要按人看的序号用 `choice`（1 起）。
        下面把两种写法都钉住，顺带钉住"越界要报错"。
        """
        from hybridengine_light import exam
        bank, _ = exam.load_banks(根)
        with Ctl(hybridengine_light_ctl()) as c:
            s = c.发("exam.start", n=8, seed=424242)
            选择题 = [(i, q) for i, q in enumerate(s["questions"]) if q["type"] == "choice"]
            self.assertTrue(选择题, "这场抽题里没有选择题 —— 换个 seed")
            for i, 题面 in 选择题:
                正确 = _查答案(bank, 题面)
                n = len(题面["options"])
                # ① answer = 0 起下标
                a = c.发("exam.answer", session=s["session"], i=i, answer=正确)
                self.assertTrue(a["ok"], f"{题面['id']} 用下标 {正确} 答对了却被判错")
                self.assertEqual(a["given"], str(正确))
                # ② choice = 1 起序号（与 ① 指向同一个选项）
                b = c.发("exam.answer", session=s["session"], i=i, choice=正确 + 1)
                self.assertTrue(b["ok"], f"{题面['id']} 用 choice={正确 + 1} 答对了却被判错")
                # ③ 选项原文
                d = c.发("exam.answer", session=s["session"], i=i,
                         answer=题面["options"][正确])
                self.assertTrue(d["ok"], f"{题面['id']} 用选项原文答对了却被判错")
                # ④ 越界必须报错（而不是被当成别的选项）
                bad = c.发原文("exam.answer", session=s["session"], i=i, answer=n)
                self.assertFalse(bad["ok"], f"{题面['id']} 传越界下标却没报错")
                self.assertIn("超出范围", bad["err"]["msg"])
                bad2 = c.发原文("exam.answer", session=s["session"], i=i, choice=n + 1)
                self.assertFalse(bad2["ok"], f"{题面['id']} 传越界序号却没报错")

    def test答错必须判错(self):
        """⚠ 判据必须能失败 —— "全对"本身什么也证明不了。"""
        from hybridengine_light import exam
        bank, _ = exam.load_banks(根)
        with Ctl(hybridengine_light_ctl()) as c:
            会话 = c.发("exam.start", n=6, seed=20240607, mode="practice")
            错了几题 = 0
            for i, 题面 in enumerate(会话["questions"]):
                正确 = _查答案(bank, 题面)
                故意错 = _一个错的答案(题面, 正确)
                if 故意错 is None:
                    continue
                r = c.发("exam.answer", session=会话["session"], i=i, answer=故意错)
                self.assertFalse(r["ok"], f"{题面['id']} 故意答错却被判对了")
                # 答错才给订正
                self.assertTrue(r.get("explain"), f"{题面['id']} 答错却没给「为什么」")
                错了几题 += 1
            self.assertGreater(错了几题, 0, "一道题都没能构造出错误答案 —— 这组断言没生效")
            成绩 = c.发("exam.grade", session=会话["session"])
            self.assertEqual(成绩["correct"], 0)
            self.assertEqual(成绩["percent"], 0.0)
            self.assertFalse(成绩["ok"])

    def test会话隔离且不存在时报错(self):
        with Ctl(hybridengine_light_ctl()) as c:
            a = c.发("exam.start", n=3, seed=1)
            b = c.发("exam.start", n=3, seed=1)
            self.assertNotEqual(a["session"], b["session"])
            r = c.发原文("exam.answer", session=a["session"], i=0, answer=0)
            self.assertTrue(r["ok"])
            # 另一个会话不该被串到
            self.assertEqual(len(c.发("exam.report", session=b["session"])["byTopic"]), 0)
            bad = c.发原文("exam.report", session="s999-ffff")
            self.assertFalse(bad["ok"])
            self.assertIn("没有这个考试会话", bad["err"]["msg"])

    def test未作答按错计且说明清楚(self):
        from hybridengine_light import exam
        bank, _ = exam.load_banks(根)
        with Ctl(hybridengine_light_ctl()) as c:
            会话 = c.发("exam.start", n=4, seed=99)
            r = c.发("exam.answer", session=会话["session"], i=0,
                     answer=_查答案(bank, 会话["questions"][0]))
            self.assertTrue(r["ok"])
            成绩 = c.发("exam.grade", session=会话["session"])
            self.assertEqual(成绩["answered"], 1)
            self.assertEqual(成绩["total"], 4)
            self.assertIn("没作答", 成绩["note"])


def _一个错的答案(题面: dict, 正确):
    """构造一个**保证错**的答案（"判据必须能失败"那条靠它）。

    ⚠ 每个答案都必须是**合法的**作答（否则测的是"参数校验拒绝了它"，
      而不是"判分判它错"）—— 两种失败原因混在一起，这条判据就废了。
    """
    if 题面["type"] == "choice":
        n = len(题面["options"])
        if n < 2:
            return None
        return (正确 + 1) % n                 # 下标，与 answer 的约定一致
    if 题面["type"] == "code":
        # 定义了东西、但绝不是它要的那个 —— 隐藏用例必然过不去
        return "def 完全不是这个函数名():\n    return None\n"
    return (str(正确) + "_错的") if 正确 is not None else "错的"


# ══════════════════════════════════════════════════════════════════════════
#  ⑤ 跨产品：**同一段客户端代码**驱动两个产品
# ══════════════════════════════════════════════════════════════════════════

def _跨产品测(ms_ctl: Path):
    """用同一个 `Ctl` 类驱动两个产品，靠 emu.info 的 product 区分。"""
    if not ms_ctl.exists():
        raise unittest.SkipTest(
            f"找不到专业版可执行文件 {ms_ctl} —— 跨产品判据没跑。"
            f"（本次**没有**构建任何东西，只读地找现成的 exe）")

    观察 = {}
    for 名, argv in (("hybridengine_light", hybridengine_light_ctl()), ("hybridengine", [str(ms_ctl)])):
        with Ctl(argv) as c:
            info = c.发("emu.info")
            命令 = c.发("ctl.commands")["commands"]
            未知 = c.发原文("绝对不存在的命令.xyz")
            观察[名] = {
                "product": info["product"],
                "commands": 命令,
                "unknownCode": 未知["err"]["code"],
                "unknownMsg": 未知["err"]["msg"],
                "ping": c.发("emu.ping")["pong"],
            }
    return 观察


class Test跨产品(unittest.TestCase):

    def test同一段客户端代码驱动两个产品(self):
        try:
            from hybridengine_light import ctl
            _ = ctl
        except Exception as e:                          # noqa: BLE001
            self.fail(f"import hybridengine_light.ctl 失败：{e}")

        ms = _ms_ctl_path()
        观察 = _跨产品测(ms)

        # ── ① 两个产品都说自己是活的 ──
        self.assertTrue(观察["hybridengine_light"]["ping"])
        self.assertTrue(观察["hybridengine"]["ping"])

        # ── ② ⚠ 判据本体：靠 product 区分 ──
        self.assertEqual(观察["hybridengine_light"]["product"], "hybridengine_light")
        self.assertEqual(观察["hybridengine"]["product"], "hybridengine")
        self.assertNotEqual(观察["hybridengine_light"]["product"], 观察["hybridengine"]["product"])

        # ── ③ 两个产品的自省都非空，且**命令集不同**（各自做各自的事）──
        甲 = set(观察["hybridengine_light"]["commands"])
        乙 = set(观察["hybridengine"]["commands"])
        self.assertTrue(甲 and 乙)
        self.assertNotEqual(甲, 乙, "两个产品的命令集不该一样")
        # 协议层那四条必须**两个产品都有** —— 这才是"同协议"的定义
        协议层 = {"emu.ping", "emu.info", "emu.quit", "ctl.commands"}
        self.assertTrue(协议层 <= 甲, f"轻量版缺协议层命令：{协议层 - 甲}")
        self.assertTrue(协议层 <= 乙, f"专业版缺协议层命令：{协议层 - 乙}")

        # ── ④ 未知命令的行为**逐字一致**（同一错误码、同一措辞形状）──
        self.assertEqual(观察["hybridengine_light"]["unknownCode"], 观察["hybridengine"]["unknownCode"])
        self.assertEqual(观察["hybridengine_light"]["unknownMsg"], 观察["hybridengine"]["unknownMsg"])
        self.assertEqual(观察["hybridengine_light"]["unknownMsg"], "未知命令: 绝对不存在的命令.xyz")


_MS_CTL: Path | None = None


def _ms_ctl_path() -> Path:
    return _MS_CTL if _MS_CTL is not None else 默认_MS_CTL


# ══════════════════════════════════════════════════════════════════════════
#  入口
# ══════════════════════════════════════════════════════════════════════════

def main(argv=None) -> int:
    global _MS_CTL
    ap = argparse.ArgumentParser(description="hybridengine_light-ctl 端到端测试")
    ap.add_argument("--ms-ctl", default=str(默认_MS_CTL),
                    help="专业版 ms_ctl 可执行文件（跨产品判据用）")
    ap.add_argument("--no-cross", action="store_true",
                    help="不跑跨产品判据（会明确报告「跳过」而不是假装通过）")
    ap.add_argument("-v", "--verbose", action="store_true")
    a = ap.parse_args(argv)
    _MS_CTL = Path(a.ms_ctl)

    loader = unittest.TestLoader()
    suite = unittest.TestSuite()
    组 = [Test协议层, Test自省, Test协议流纯净, Test考试闭环]
    if not a.no_cross:
        组.append(Test跨产品)
    else:
        print("⚠ --no-cross：跨产品判据**没有跑** —— 本次运行不构成跨产品证据。")
    for cls in 组:
        suite.addTests(loader.loadTestsFromTestCase(cls))

    结果 = unittest.TextTestRunner(verbosity=2 if a.verbose else 1).run(suite)
    return 0 if 结果.wasSuccessful() else 1


if __name__ == "__main__":
    sys.exit(main())
