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

from learncode.check import load_module, run_checks  # noqa: E402

ROOT = Path(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def _lesson(name: str):
    return load_module(ROOT / "lessons" / name, name=f"lesson_{name.replace('.', '_')}")


class Test第01课(unittest.TestCase):
    """变量课：句子必须跟着变量变，而不是写死。"""

    def _答案(self, 名字, 年龄, 打招呼):
        mod = type(sys)("ans01")
        mod.名字 = 名字
        mod.年龄 = 年龄
        mod.打招呼 = 打招呼
        return mod

    def test_正确解法全过(self):
        m = self._答案("小明", 18, lambda: f"你好，{m.名字}！我今年 {m.年龄} 岁。")
        rep = run_checks(_lesson("01_变量.py"), m)
        self.assertTrue(rep.all_ok, [f"{r.name}: {r.detail}" for r in rep.results if not r.ok])

    def test_写死的句子被指出(self):
        """返回的句子不跟着变量变 —— 这一课的核心检查点。"""
        m = self._答案("小明", 18, lambda: "你好，小明！我今年 18 岁。")
        rep = run_checks(_lesson("01_变量.py"), m)
        self.assertFalse(rep.all_ok)
        名字 = [r for r in rep.results if "用到变量" in r.name]
        self.assertTrue(名字, "应该有「描述真的用了变量」这条检查")
        self.assertFalse(名字[0].ok)
        self.assertIn("写死", 名字[0].detail + "写死")

    def test_年龄写死被指出(self):
        """**收紧后的核心测试**：句子跟着名字变、但年龄写死 —— 也必须被抓。

        ⚠ 审查发现的漏洞：原来的检查只替换「名字」，不替换「年龄」。
          于是 
eturn f"你好，{名字}！我今年 18 岁。" 能拿 6/6 满分 ——
          而课程明写「要真的用上面两个变量」。
        """
        m = self._答案("小明", 18, lambda: f"你好，{m.名字}！我今年 18 岁。")
        rep = run_checks(_lesson("01_变量.py"), m)
        self.assertFalse(rep.all_ok, "年龄写死的解法不该通过")
        项 = [r for r in rep.results if "用到变量" in r.name]
        self.assertTrue(项, "应该有「真的用了变量」这条检查")
        self.assertFalse(项[0].ok, "应该抓出年龄被写死")
        self.assertIn("年龄", 项[0].detail, "提示要指明是「年龄」没跟着变")

    def test_名字带了引号被指出(self):
        m = self._答案(18, 18, lambda: "x")
        rep = run_checks(_lesson("01_变量.py"), m)
        项 = [r for r in rep.results if r.name == "名字变量"]
        self.assertFalse(项[0].ok)
        self.assertIn("引号", 项[0].detail)

    def test_年龄是文字被指出(self):
        m = self._答案("小明", "18", lambda: "x")
        rep = run_checks(_lesson("01_变量.py"), m)
        项 = [r for r in rep.results if r.name == "年龄变量"]
        self.assertFalse(项[0].ok)
        self.assertIn("整数", 项[0].detail)


class Test第02课模块化(unittest.TestCase):
    """模块化课：必须在**两个文件**之间真的接上。"""

    def setUp(self):
        self.tmp = tempfile.mkdtemp(prefix="lc_mod_")
        self.dir = Path(self.tmp)

    def tearDown(self):
        shutil.rmtree(self.tmp, ignore_errors=True)

    def _写(self, mytools: str, main: str):
        (self.dir / "mytools.py").write_text(mytools, encoding="utf-8")
        (self.dir / "main.py").write_text(main, encoding="utf-8")
        if str(self.dir) not in sys.path:
            sys.path.insert(0, str(self.dir))
        # 每次都要清掉缓存的 mytools，否则上一次的会被复用
        sys.modules.pop("mytools", None)
        return load_module(self.dir / "main.py", name="ans02_main")

    好的模块 = (
        '单位 = "平方米"\n'
        "def 面积(长, 宽):\n    return 长 * 宽\n"
        "def 周长(长, 宽):\n    return 2 * (长 + 宽)\n"
        "def 是正方形(长, 宽):\n    return 长 == 宽\n"
    )

    def test_正确解法全过(self):
        main = (
            "import mytools\n"
            "def 描述(长, 宽):\n"
            "    a = mytools.面积(长, 宽)\n"
            "    c = mytools.周长(长, 宽)\n"
            "    return f'{长} × {宽} 的面积是 {a} {mytools.单位}，周长是 {c}'\n"
        )
        rep = run_checks(_lesson("02_模块化.py"), self._写(self.好的模块, main))
        self.assertTrue(rep.all_ok, [f"{r.name}: {r.detail}" for r in rep.results if not r.ok])

    def test_抄一份算法被指出(self):
        """**这一课最重要的测试**：输出全对，但没真的用模块。

        把算法抄一份到 main.py 里，结果一模一样 ——
        如果检查抓不出这个，那一课等于没上（学生抄一遍就"过"了）。
        """
        main = (
            "import mytools\n"
            "def 描述(长, 宽):\n"
            "    a = 长 * 宽\n"                       # ← 自己重写，没用 mytools
            "    c = 2 * (长 + 宽)\n"
            "    return f'{长} × {宽} 的面积是 {a} 平方米，周长是 {c}'\n"
        )
        rep = run_checks(_lesson("02_模块化.py"), self._写(self.好的模块, main))
        self.assertFalse(rep.all_ok, "抄一份的解法不该通过")
        项 = [r for r in rep.results if "用了mytools" in r.name]
        self.assertTrue(项, "应该有「描述真的用了mytools」这条检查")
        self.assertFalse(项[0].ok, "这条检查必须抓出没有真用模块的解法")
        # 提示要说清**哪些**没走模块（收紧之后改成逐个列出来了）
        self.assertIn("没有真的用 mytools", 项[0].detail)

    def test_只模块化一半被指出(self):
        """**收紧后的核心测试**：面积走模块、周长自己算 —— 也必须被抓。

        ⚠ 这是审查发现的一个漏洞：原来的检查只替换 mytools.面积 和 单位，
          不替换 周长。于是「面积走模块、周长手算 2*(长+宽)」能拿 9/9 满分 ——
          而课程明写「必须通过 mytools 去算，而不是自己重写一遍」。
          只验证一半，学生就只需要模块化一半。
        """
        main = (
            "import mytools\n"
            "def 描述(长, 宽):\n"
            "    a = mytools.面积(长, 宽)          # ← 这个走了模块\n"
            "    c = 2 * (长 + 宽)                 # ← 这个自己算的\n"
            "    return f'{长} × {宽} 的面积是 {a} {mytools.单位}，周长是 {c}'\n"
        )
        rep = run_checks(_lesson("02_模块化.py"), self._写(self.好的模块, main))
        self.assertFalse(rep.all_ok, "只模块化一半的解法不该通过")
        项 = [r for r in rep.results if "用了mytools" in r.name]
        self.assertTrue(项[0].ok is False, "应该抓出周长没走模块")
        self.assertIn("周长", 项[0].detail, "提示要指明是「周长」没走模块")

    def test_没import被指出(self):
        main = "def 描述(长, 宽):\n    return 'x'\n"
        rep = run_checks(_lesson("02_模块化.py"), self._写(self.好的模块, main))
        self.assertFalse(rep.all_ok)
        项 = [r for r in rep.results if "两个文件都在" in r.name]
        self.assertFalse(项[0].ok)
        self.assertIn("import mytools", 项[0].detail)

    def test_面积算错被指出(self):
        bad = self.好的模块.replace("return 长 * 宽", "return 长 + 宽")
        main = (
            "import mytools\n"
            "def 描述(长, 宽):\n"
            "    return f'{长} × {宽} 的面积是 {mytools.面积(长, 宽)} {mytools.单位}'\n"
        )
        rep = run_checks(_lesson("02_模块化.py"), self._写(bad, main))
        项 = [r for r in rep.results if r.name == "面积算得对"]
        self.assertFalse(项[0].ok)
        self.assertIn("算错了", 项[0].detail)

    def test_是正方形返回数字被指出(self):
        """返回 0/1 而不是 True/False —— 能用但类型不对，该指出。"""
        bad = self.好的模块.replace("return 长 == 宽", "return int(长 == 宽) if False else 长 - 宽")
        main = "import mytools\ndef 描述(长, 宽):\n    return ''\n"
        rep = run_checks(_lesson("02_模块化.py"), self._写(bad, main))
        项 = [r for r in rep.results if "是正方形" in r.name]
        self.assertFalse(项[0].ok)

    def test_单位写错被指出(self):
        bad = self.好的模块.replace('单位 = "平方米"', '单位 = "平米"')
        main = (
            "import mytools\n"
            "def 描述(长, 宽):\n"
            "    return f'{长} × {宽} 的面积是 {mytools.面积(长, 宽)} {mytools.单位}，"
            "周长是 {mytools.周长(长, 宽)}'\n"
        )
        rep = run_checks(_lesson("02_模块化.py"), self._写(bad, main))
        项 = [r for r in rep.results if r.name == "单位常量"]
        self.assertFalse(项[0].ok)


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
        """课程文件必须自己讲清要求 —— 学生是直接读这个文件的。"""
        for p in sorted((ROOT / "lessons").glob("[0-9][0-9]_*.py")):
            with self.subTest(课=p.stem):
                text = p.read_text(encoding="utf-8")
                self.assertIn("要做", text, f"{p.stem} 缺少「要做」段落")

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
