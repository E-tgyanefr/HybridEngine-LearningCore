"""缓存与"启动速度"的判据。

## 为什么给"缓存"单独写判据

因为缓存**必然**引入一类错：**记错了**。

实测踩到的：`_发现缓存` 只按"轨道名"存，而**内容根是会变的**
（测试用临时目录、打包后指向 exe 旁边）。
换了根之后缓存仍返回**上一个根**的课，而那个目录可能已被删除
⇒ `FileNotFoundError`，而且**只在"先跑 A 再跑 B"的顺序下才出现**
—— 单跑一个测试永远看不到。

⇒ 判据必须**显式地换根**跑，才能守住这条。

## 另半份：启动速度

启动慢是用户实测反馈（6.4 秒）。这里判"构造界面**不等**编译型课程"，
而不是判一个绝对秒数（机器快慢不同，绝对秒数会变成脆弱判据）。
"""
from __future__ import annotations

import os
import shutil
import sys
import tempfile
import unittest

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, LC)

from hybridengine_light import learnmodel as M                    # noqa: E402


class 缓存与根绑定测试(unittest.TestCase):
    """⚠ 这组测试的价值全在"两种根先后出现"上。"""

    def setUp(self):
        self.根们 = []

    def tearDown(self):
        for d in self.根们:
            shutil.rmtree(d, ignore_errors=True)
        os.environ.pop("HYBRIDENGINE_LIGHT_ROOT", None)

    def _造根(self, 编号们: list[str]) -> str:
        d = tempfile.mkdtemp(prefix="ms_cache_work_")
        self.根们.append(d)
        os.makedirs(os.path.join(d, "lessons", "solutions"), exist_ok=True)
        for i in 编号们:
            with open(os.path.join(d, "lessons", f"{i}_课.py"), "w", encoding="utf-8") as f:
                f.write(f"# 第 {i} 课：课{i}\n")
            with open(os.path.join(d, "lessons", "solutions", f"{i}_课.py"),
                      "w", encoding="utf-8") as f:
                f.write("x = 1\n")
        return d

    def test_换根之后不能返回旧根的课(self):
        """⚠ 这条就是实测踩到的那个 bug。"""
        a = self._造根(["01", "02"])
        os.environ["HYBRIDENGINE_LIGHT_ROOT"] = a
        ra = M.发现("python")
        self.assertEqual([k.编号 for k in ra], ["01", "02"])

        # 换一个**不同**的根
        b = self._造根(["07"])
        os.environ["HYBRIDENGINE_LIGHT_ROOT"] = b
        rb = M.发现("python")
        self.assertEqual([k.编号 for k in rb], ["07"],
                         "换根后仍返回旧根的课 —— 缓存没和根绑定")
        # 而且返回的文件必须真的存在（旧根可能已被删）
        for k in rb:
            self.assertTrue(k.源文件.is_file(), f"返回了不存在的文件：{k.源文件}")

    def test_旧根被删掉也不会炸(self):
        a = self._造根(["01"])
        os.environ["HYBRIDENGINE_LIGHT_ROOT"] = a
        M.发现("python")
        shutil.rmtree(a, ignore_errors=True)          # 模拟测试结束清场
        b = self._造根(["09"])
        os.environ["HYBRIDENGINE_LIGHT_ROOT"] = b
        try:
            rb = M.发现("python")
        except FileNotFoundError as e:
            self.fail(f"旧根被删后仍去读它：{e}")
        self.assertEqual([k.编号 for k in rb], ["09"])

    def test_同一个根下确实用了缓存(self):
        """反面判据：不缓存的话这个优化就白做了（编译型要 1.5 秒/条）。

        ⚠ 用"同一个列表对象"来判 —— 比掐时间稳。
        """
        a = self._造根(["01"])
        os.environ["HYBRIDENGINE_LIGHT_ROOT"] = a
        r1 = M.发现("python")
        r2 = M.发现("python")
        self.assertIs(r1, r2, "同根下没有复用缓存（编译型那几条会白跑 dotnet）")

    def test_清缓存后重新发现(self):
        a = self._造根(["01"])
        os.environ["HYBRIDENGINE_LIGHT_ROOT"] = a
        r1 = M.发现("python")
        M.清缓存()
        r2 = M.发现("python")
        self.assertIsNot(r1, r2, "清缓存没生效")


class 启动不等编译型测试(unittest.TestCase):
    """⚠ 用户实测反馈"打开太慢"（6.4 秒）⇒ 界面不能等编译型课程。"""

    @classmethod
    def setUpClass(cls):
        try:
            import tkinter
            r = tkinter.Tk()
            r.destroy()
        except Exception as e:
            raise unittest.SkipTest(f"没有显示环境：{e}")

    def setUp(self):
        self.根 = tempfile.mkdtemp(prefix="ms_faststart_work_")
        os.makedirs(os.path.join(self.根, "lessons", "solutions"), exist_ok=True)
        with open(os.path.join(self.根, "lessons", "01_课.py"), "w", encoding="utf-8") as f:
            f.write("# 第 01 课：课\n")
        os.environ["HYBRIDENGINE_LIGHT_ROOT"] = self.根

    def tearDown(self):
        shutil.rmtree(self.根, ignore_errors=True)
        os.environ.pop("HYBRIDENGINE_LIGHT_ROOT", None)

    def test_构造界面不阻塞在编译型上(self):
        """构造时**不许**调用编译型的发现（那会跑 dotnet run）。"""
        from hybridengine_light import learn_gui as G
        被问到的: list[str] = []
        真的 = M.发现

        def 记账(键):
            被问到的.append(键)
            return 真的(键)

        M.发现 = 记账
        try:
            g = G.界面()
            # ⚠ 课程树在「课程学习」里 —— 主页上没有它
            g.进课程学习()
        finally:
            M.发现 = 真的
        编译型被问 = [k for k in 被问到的 if k in M.编译型轨道]
        self.assertEqual(编译型被问, [],
                         f"构造界面时问了编译型课程线 {编译型被问} —— 那就是启动慢的原因")
        # 即时轨道要**在**构造时就填好（否则界面是空的）
        for 键 in M.即时轨道:
            if 真的(键):
                self.assertIn(键, 被问到的, f"即时轨道 {键} 没在构造时填")
        g.destroy()

    def test_构造后有待填的编译型占位(self):
        """编译型四条线应当**先占位**（显示"载入中…"），而不是不出现。"""
        from hybridengine_light import learn_gui as G
        g = G.界面()
        # ⚠ 课程树在「课程学习」里 —— 主页上没有它
        g.进课程学习()
        try:
            self.assertTrue(hasattr(g, "_待填父节点"))
            self.assertEqual(set(g._待填父节点), set(M.编译型轨道),
                             "编译型四条线没有全部占位")
            for 键, 父 in g._待填父节点.items():
                self.assertIn("载入中", g.树.item(父, "text"))
        finally:
            g.destroy()


if __name__ == "__main__":
    unittest.main()
