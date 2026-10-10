"""GUI 学习界面的判据（`learnmodel.py` + `run.py::该开图形界面`）。

## ⚠ 这份测什么、不测什么（写在最前面）

  **测**：课程发现 · 答题路径 · 模板生成 · 运行/检查的输出收集
        · 「双击开 GUI 还是走命令行」的判据 · 界面能构造（有显示环境时）
  **不测**：窗口长什么样、按钮点下去看起来对不对。
        ⚠ 那需要人眼看。我**不假装测过**。

## 为什么值得为模型层写判据

因为上一次的教训：我修 "双击 exe 一闪而过" 时，判据只覆盖了纯函数，
结果漏掉一条**只有打包后才走的路径**（`os` 没 import，因 `and` 短路从未执行）。
⇒ **能把逻辑从界面里分出来的，就分出来；分出来就能测。**
"""
from __future__ import annotations

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from hybridengine_light import learnmodel as M            # noqa: E402
from hybridengine_light.run import 该开图形界面            # noqa: E402


class 课程发现测试(unittest.TestCase):
    def test_每条轨道都能发现且不抛(self):
        # 某条线坏了不该让界面整体打不开
        for 键 in M.轨道:
            cs = M.发现(键)
            self.assertIsInstance(cs, list, f"{键} 没返回列表")

    def test_开箱即用的几条线都有课(self):
        # 这四条不依赖任何外部工具链 —— 空的话就是真出了问题
        for 键 in ("python", "ai", "embedded", "raspi"):
            self.assertTrue(M.发现(键), f"{键} 一課都没有")

    def test_发现的课字段完整(self):
        for k in M.发现("python") + M.发现("ai"):
            self.assertTrue(k.编号, "编号为空")
            self.assertTrue(k.标题, "标题为空")
            self.assertTrue(str(k.源文件), "源文件为空")
            self.assertTrue(k.显示())

    def test_编号不重复(self):
        for 键 in ("python", "ai", "embedded", "raspi", "cpp", "c"):
            cs = M.发现(键)
            编号们 = [k.编号 for k in cs]
            self.assertEqual(len(编号们), len(set(编号们)), f"{键} 有重复编号：{编号们}")


class 回答路径测试(unittest.TestCase):
    def test_python线复用run的真值(self):
        # ⚠ 必须复用：自己拼一遍的话，学员会写到一个文件、检查另一个文件
        #   （表现为"我明明写了却说过不了"）
        from hybridengine_light import run as R
        cs = M.发现("python")
        self.assertTrue(cs)
        for k in cs:
            self.assertEqual(M._答案文件(k), R._答题路径(k.源文件))

    def test_答题文件在预期目录下(self):
        for k in M.发现("python"):
            self.assertIn("answers", str(k.答题文件))


class 环境体检测试(unittest.TestCase):
    def test_体检返回固定几项且不抛(self):
        出 = M.体检()
        self.assertGreaterEqual(len(出), 3)
        for 名, 好, 值 in 出:
            self.assertIsInstance(名, str)
            self.assertIsInstance(好, bool)
            self.assertIsInstance(值, str)

    def test_Python运行时必然就绪(self):
        d = {名: 好 for 名, 好, _ in M.体检()}
        self.assertTrue(d["Python 运行时"])


class 运行与检查测试(unittest.TestCase):
    """⚠ 这些会**真的写文件、真的跑子进程** —— 用临时目录，跑完清掉。"""

    def setUp(self):
        import tempfile
        self.临时 = tempfile.mkdtemp(prefix="ms_learn_gui_work_")
        os.environ["HYBRIDENGINE_LIGHT_ROOT"] = self.临时
        # 造一个最小可用的课程内容
        os.makedirs(os.path.join(self.临时, "lessons", "solutions"), exist_ok=True)
        课 = os.path.join(self.临时, "lessons", "01_测试课.py")
        with open(课, "w", encoding="utf-8") as f:
            f.write("# 第 01 课：测试课\n# 要做：打印 hello\n")
        with open(os.path.join(self.临时, "lessons", "solutions", "01_测试课.py"),
                  "w", encoding="utf-8") as f:
            f.write('print("hello")\n')
        self.课 = M.发现("python")[0]

    def tearDown(self):
        import shutil
        os.environ.pop("HYBRIDENGINE_LIGHT_ROOT", None)
        shutil.rmtree(self.临时, ignore_errors=True)

    def test_读答案会生成模板(self):
        self.assertFalse(self.课.答题文件.is_file())
        文 = M.读答案(self.课)
        self.assertTrue(self.课.答题文件.is_file(), "读答案应当自动生成模板")
        # ⚠ 判据的本意是"生成了一份**非空**的起步代码"，不是"含某个词"。
        #   我第一版断言含"要做" —— 而 `_stub_for` 给单文件课生成的是
        #   「第 01 课的答案 —— 在这里写你的代码」那种更简的模板。
        #   **断言具体措辞会把判据变成"文案快照"** —— 改文案就红，而功能没坏。
        self.assertTrue(文.strip(), "模板不该是空的")
        self.assertIn("01", 文, "模板里应该提到是哪一课")

    def test_保存然后读取一致(self):
        M.读答案(self.课)                      # 先生成
        语 = M.保存答案(self.课, 'print("看我的")\n')
        self.assertIn("已保存", 语)
        self.assertEqual(M.读答案(self.课), 'print("看我的")\n')

    def test_运行能收到子进程输出(self):
        # ⚠ 这是"界面真的能看到结果"的关键：输出要能被**收成字符串**
        M.读答案(self.课)
        M.保存答案(self.课, 'print("你好，世界")\n')
        出 = M.运行(self.课, 'print("你好，世界")\n')
        self.assertIn("你好，世界", 出)

    def test_运行能收到报错(self):
        # 报错也要显示出来（不能吞）—— 学员就是靠它学
        出 = M.运行(self.课, 'raise ValueError("故意炸")\n')
        self.assertIn("ValueError", 出)

    def test_运行不会因语法错而崩(self):
        出 = M.运行(self.课, 'print("没关引号\n')
        self.assertTrue(出.strip(), "语法错时也该有输出（报错本身）")

    def test_看课返回正文(self):
        文 = M.看课(self.课)
        self.assertIn("测试课", 文)

    def test_参考答案读得到(self):
        文 = M.参考答案(self.课)
        self.assertIn("hello", 文)

    def test_没有dotnet时给出人话指引(self):
        # ⚠ 这是编译型四条线的**唯一**失败模式：缺工具链。
        #   不能抛异常、不能什么都不说 —— 要给能照做的安装指引。
        import unittest.mock as mock
        with mock.patch.object(M, "有dotnet", return_value=False):
            码, 文 = M._运行课程程序(self.临时, [])
        self.assertEqual(码, 127)
        self.assertIn(".NET SDK", 文)
        self.assertIn("dotnet.microsoft.com", 文)


class 该开图形界面测试(unittest.TestCase):
    """（冻结, 参数个数, 可用GUI, 有PROMPT, 禁用GUI）-> 要不要开窗口。"""

    def test_双击exe开GUI(self):
        self.assertTrue(该开图形界面(True, 0, True, False))

    def test_带参数不开GUI(self):
        # ⚠ 带参数就是明确的命令行意图 —— 弹窗是骚扰
        self.assertFalse(该开图形界面(True, 1, True, False))

    def test_从cmd跑不开GUI(self):
        # ⚠ 最要紧的一条：不区分的话命令行用户会被弹窗
        self.assertFalse(该开图形界面(True, 0, True, True))

    def test_源码用法不开GUI(self):
        # 源码用法必经终端；`python -m ...` 不该弹窗
        self.assertFalse(该开图形界面(False, 0, True, False))

    def test_没有显示环境不开GUI(self):
        # CI / SSH / 无 X —— 不能去试，试了会崩
        self.assertFalse(该开图形界面(True, 0, False, False))

    def test_逃生门能强制命令行(self):
        self.assertFalse(该开图形界面(True, 0, True, False, 禁止图形界面=True))

    def test_五个输入都参与判定(self):
        # 反面判据：翻掉任意一项，结果都该变（说明它真的被读了）
        self.assertTrue(该开图形界面(True, 0, True, False))
        self.assertFalse(该开图形界面(False, 0, True, False), "冻结没被读")
        self.assertFalse(该开图形界面(True, 1, True, False), "参数个数没被读")
        self.assertFalse(该开图形界面(True, 0, False, False), "可用GUI没被读")
        self.assertFalse(该开图形界面(True, 0, True, True), "PROMPT没被读")
        self.assertFalse(该开图形界面(True, 0, True, False, True), "逃生门没被读")


if __name__ == "__main__":
    unittest.main()
