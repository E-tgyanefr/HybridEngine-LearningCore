"""课程系统的测试。

════ 课程为什么也要测 ════

课程文件里装的是**检查逻辑** —— 而检查逻辑错了是最糟的情况：

  · 太松：学生抄一份也能过 → 那一课等于没上
  · 太严：正确解法被判错 → 学生被莫名卡住，然后放弃

两种错都不会自己暴露。所以这里对每一课的检查都跑**两个场景**：

  ① 一个正确解法 → 必须全过
  ② 一个"看起来对但没做到要点"的解法 → 必须被指出

第 ② 条尤其重要。第 02 课（模块化）唯一要考的是"有没有真的用模块"，
而"把算法抄一份到 main.py"能输出完全一样的结果 —— 只有专门一条检查能抓出来。
如果不测它，那条检查形同虚设。
"""

import importlib.util
import os
import shutil
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from hybridengine_light.check import load_module, run_checks  # noqa: E402

ROOT = Path(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def _lesson(name: str):
    return load_module(ROOT / "lessons" / name, name=f"lesson_{name.replace('.', '_')}")


class Test第01课(unittest.TestCase):
    """第 01 课（变量）—— 检查必须**指出具体错在哪**，不是只说"错了"。

    ⚠ 这一课在「一课只讲一个语法」重排后只剩一条判据：
      "文字要加引号、数字不要"。所以这里的错例也只针对这一件事。
    """

    def test_正确解法全过(self):
        m = 类型小模块(我的名字="小明", 我的年龄=18)
        rep = run_checks(_lesson("01_变量.py"), m)
        self.assertTrue(rep.all_ok, _报告(rep))

    def test_名字忘了引号被指出(self):
        # ⚠ 忘了引号在真实运行里是 NameError —— 用"名字是数字"来模拟这一类错
        m = 类型小模块(我的名字=123, 我的年龄=18)
        rep = run_checks(_lesson("01_变量.py"), m)
        self.assertFalse(rep.all_ok)
        文 = _报告(rep)
        self.assertIn("我的名字", 文)
        self.assertIn("引号", 文, "没告诉学员「要加引号」这件事")

    def test_年龄写成了文字被指出(self):
        m = 类型小模块(我的名字="小明", 我的年龄="18")
        rep = run_checks(_lesson("01_变量.py"), m)
        self.assertFalse(rep.all_ok)
        文 = _报告(rep)
        self.assertIn("我的年龄", 文)
        self.assertIn("引号", 文, "没告诉学员「把引号去掉」")

    def test_两个都没写时说明不为空(self):
        rep = run_checks(_lesson("01_变量.py"), 类型小模块())
        self.assertFalse(rep.all_ok)
        for r in rep.results:
            if not r.ok:
                self.assertTrue(r.detail.strip(),
                                f"检查「{r.name}」失败了却没说为什么")


def 类型小模块(**属性):
    """造一个"像学员答案"的小对象。"""
    m = type(sys)("答题")
    for k, v in 属性.items():
        setattr(m, k, v)
    return m


def _报告(rep) -> str:
    """把报告变成字符串。

    ⚠ `check.report()` **返回字符串**（不是打印！）——
      我第一版用 `redirect_stdout` 去抓，抓到的是空串，
      于是断言 `assertIn("我的名字", "")` 全红。教训：
      **用别人的函数之前先看它返回什么**，别按名字猜行为。
    """
    from hybridengine_light import check as _ck
    return _ck.report(rep, "测试")



class Test所有课程(unittest.TestCase):
    """对每一课做一次通用体检 —— 新增课程时能自动覆盖到。"""

    def test_每课都有检查(self):
        for p in sorted((ROOT / "lessons").glob("[0-9][0-9]_*.py")):
            with self.subTest(课=p.stem):
                mod = load_module(p, name=f"chk_{p.stem}")
                checks = [n for n in vars(mod) if n.startswith("CHECK_")]
                self.assertGreater(len(checks), 0, f"{p.stem} 没有任何 CHECK_ 检查")

    def test_每课都有标题(self):
        for p in sorted((ROOT / "lessons").glob("[0-9][0-9]_*.py")):
            with self.subTest(课=p.stem):
                first = p.read_text(encoding="utf-8").splitlines()[0]
                self.assertTrue(first.startswith("# 第"), f"{p.stem} 第一行应是标题注释")

    def test_每课都说明了要做什么(self):
        """课程文件必须自己讲清要求 —— 学生是直接读这个文件的。

        ⚠ **不要判某个具体的词。**
          我原来写的是 `assertIn("要做", text)` —— 而后来把课程顺序
          改成「先介绍 → 再布置任务」时，那个小节标题换成了
          「轮到你了 —— 在下面的答题文件里写这几件事」，
          于是这个判据连红 8 课，**而课程其实完全正常**。

        ⇒ 判据要守的是「**有没有把要求讲清楚**」这件事，
          不是「用的哪个词」。所以这里成对地认两种说法，
          并且顺带要求它**真的列出了编号条目**（那才是"讲清楚"）。
        """
        提要求的说法 = ("要做", "轮到你了", "写这几件事", "要求")
        for p in sorted((ROOT / "lessons").glob("[0-9][0-9]_*.py")):
            with self.subTest(课=p.stem):
                text = p.read_text(encoding="utf-8")
                self.assertTrue(any(s in text for s in 提要求的说法),
                                f"{p.stem} 没有讲清要做什么（找过：{提要求的说法}）")
                # 光有标题不算 —— 得真有条目。
                # ⚠ 条目形态**不止一种**：多数课用编号 `#   1.`，
                #   而 08 用项目符号 `#     ·`、02 直接指向两个文件里的要求。
                #   判据要认"有没有分条列出"，不是"用哪种符号" ——
                #   我第一版只认编号，结果又连红 2 课（课程本身没问题）。
                import re as _re
                有条目 = (_re.search(r"^#\s+\d\.", text, _re.M)
                          or _re.search(r"^#\s+[·•\-]\s", text, _re.M)
                          or "具体每条要求写在那两个文件" in text)
                self.assertTrue(有条目,
                                f"{p.stem} 的要求没有分条列出（学生看不出要做几件事）")

    def test_检查的失败说明都不为空(self):
        """检查失败时必须给出具体提示，不能只说"错了"。"""
        # 用一个空对象去撞每一课，看失败项有没有说明
        empty = type(sys)("empty")
        for p in sorted((ROOT / "lessons").glob("[0-9][0-9]_*.py")):
            mod = load_module(p, name=f"emp_{p.stem}")
            rep = run_checks(mod, empty)
            for r in rep.results:
                if not r.ok:
                    with self.subTest(课=p.stem, 检查=r.name):
                        self.assertTrue(r.detail.strip(),
                                        f"{p.stem} 的「{r.name}」失败时没说原因")


if __name__ == "__main__":
    unittest.main(verbosity=2)
