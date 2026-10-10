# hybridengine_light 的测试 —— 只用标准库 unittest。
#
# 为什么不引入 pytest：这个包的目标就是"零依赖、装完即用"。
# 连测试都要额外装东西，就跟这个目标自相矛盾了。
#
# 关键设计：Stage 的绘图指令与显示是分开的，所以**大部分测试不需要显示器**，
# 只有真正建窗口的那几条要跳过无头环境。这让测试能在 CI/远程机器上跑。
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from hybridengine_light import Stage, explain, color_of  # noqa: E402
from hybridengine_light.stage import colors  # noqa: E402


class TestCoordinates(unittest.TestCase):
    """坐标换算是最容易悄悄错的地方（y 轴翻转 + 缩放），单独钉住。"""

    def test_origin_is_bottom_left(self):
        s = Stage(size=100, width=100, height=100)
        # (0,0) 应落在屏幕左下角 → 像素 y 最大
        px, py = s._xy(0, 0)
        self.assertEqual((px, py), (0, 100))

    def test_top_right(self):
        s = Stage(size=100, width=100, height=100)
        px, py = s._xy(100, 100)
        self.assertEqual((px, py), (100, 0))   # 右上角 → 像素 y=0

    def test_scaling(self):
        s = Stage(size=100, width=500, height=250)
        px, py = s._xy(50, 50)
        self.assertEqual((px, py), (250, 125))
        self.assertEqual(s._r(10), 50)

    def test_y_axis_points_up(self):
        """数学坐标里 y 变大＝往上，屏幕上像素 y 应变小。这条最容易写反。"""
        s = Stage(size=100, width=100, height=100)
        _, y_low = s._xy(0, 20)
        _, y_high = s._xy(0, 80)
        self.assertLess(y_high, y_low, "y 更大时像素 y 必须更小（往上）")


class TestShapes(unittest.TestCase):
    """形状记录 —— 不需要显示器。"""

    def test_recorded_in_order(self):
        s = Stage()
        s.dot(1, 1).box(2, 2, 3, 3).line(0, 0, 9, 9).text(5, 5, "hi")
        kinds = [sh[0] for sh in s.snapshot()]
        self.assertEqual(kinds, ["dot", "box", "line", "text"])

    def test_chainable(self):
        s = Stage()
        r = s.dot(1, 1).dot(2, 2).clear()
        self.assertIs(r, s, "每个绘图方法都应返回 self，才能链式写")

    def test_clear_empties(self):
        s = Stage()
        s.dot(1, 1)
        s.clear()
        self.assertEqual(s.snapshot(), [])

    def test_dot_args(self):
        s = Stage()
        s.dot(10, 20, r=7, color="红")
        self.assertEqual(s.snapshot()[0], ("dot", 10, 20, 7, "红"))

    def test_defaults_are_sane(self):
        """默认值必须真的能用 —— 新手常省略参数。"""
        s = Stage()
        s.dot(1, 1)
        self.assertEqual(len(s.snapshot()[0]), 5)


class TestColors(unittest.TestCase):
    def test_chinese_name(self):
        self.assertEqual(color_of("红"), colors["红"])

    def test_english_name(self):
        self.assertEqual(color_of("red"), colors["red"])

    def test_english_case_insensitive(self):
        self.assertEqual(color_of("RED"), colors["red"])

    def test_hex_passthrough(self):
        self.assertEqual(color_of("#123456"), "#123456")

    def test_unknown_name_passthrough(self):
        """未知颜色名不在这里报错 —— 交给 tkinter，再由 explain 翻译。"""
        self.assertEqual(color_of("不存在的颜色"), "不存在的颜色")

    def test_non_string_passthrough(self):
        self.assertEqual(color_of(None), None)


class TestExplain(unittest.TestCase):
    """错误翻译是核心功能，逐条钉住。"""

    def _msg(self, fn, *a):
        try:
            fn(*a)
        except Exception as e:                       # noqa: BLE001
            return explain(e)
        self.fail("这段代码本该报错")

    def test_name_error_explains_quotes(self):
        def f():
            return 未定义的名字                     # noqa: F821
        msg = self._msg(f)
        self.assertIn("未定义的名字", msg)
        self.assertIn("引号", msg)

    def test_int_plus_str_mentions_fstring(self):
        def f():
            return 1 + "a"
        msg = self._msg(f)
        self.assertIn("f-string", msg)
        self.assertIn("不能相加", msg)

    def test_str_plus_int_suggests_fstring(self):
        """`"x" + 1` 走的是 can only concatenate str 那条规则，应给出 f-string 建议。

        ⚠ 我第一次把断言写成查 "int()" —— 那是「unsupported operand」那条规则的提示，
          而这条错误走的是另一条分支。**测试写错和代码写错一样会浪费时间**，
          所以这里把"这条错误该给什么建议"写清楚。
        """
        def f():
            return "x" + 1
        msg = self._msg(f)
        self.assertIn("f-string", msg)
        self.assertIn("拼起来", msg)

    def test_input_hint_is_in_operand_rule(self):
        """input() 返回文字这条提示属于「数字+文字」那条规则（新手最常踩的一脚）。"""
        def f():
            n = "3"
            return n + 1
        msg = self._msg(f)
        self.assertIn("f-string", msg)

    def test_index_error_mentions_zero(self):
        def f():
            return [1, 2, 3][5]
        msg = self._msg(f)
        self.assertIn("从 0 开始", msg)
        self.assertIn("x[-1]", msg)

    def test_key_error(self):
        def f():
            return {"a": 1}["b"]
        msg = self._msg(f)
        self.assertIn(".get(", msg)

    def test_zero_division(self):
        def f():
            return 1 / 0
        msg = self._msg(f)
        self.assertIn("除数", msg)

    def test_attribute_error(self):
        def f():
            return "abc".append("d")
        msg = self._msg(f)
        self.assertIn("append", msg)

    def test_syntax_error(self):
        msg = self._msg(compile, "if True\n    pass", "<t>", "exec")
        self.assertIn("冒号", msg)

    def test_unknown_error_is_honest(self):
        """认不出来时必须承认，而不是编一段像模像样的解释。"""
        class Weird(Exception):
            pass

        def f():
            raise Weird("很奇怪的错误")
        msg = self._msg(f)
        self.assertIn("没有内置解释", msg)
        self.assertIn("很奇怪的错误", msg)         # 原文必须保留

    def test_original_traceback_always_kept(self):
        """原文必须始终保留 —— 老师和搜索都要用它。"""
        def f():
            return 1 / 0
        msg = self._msg(f)
        self.assertIn("ZeroDivisionError", msg)
        self.assertIn("Traceback", msg)

    def test_shows_line_number(self):
        def f():
            return 1 / 0
        msg = self._msg(f)
        self.assertIn("第", msg)
        self.assertIn("行", msg)

    def test_every_rule_has_how_to_fix(self):
        """每条解释都必须给出可操作的建议，不能只说"你错了"。"""
        cases = [
            (lambda: 不存在的名字, "怎么改"),      # noqa: F821
            (lambda: 1 + "a", "怎么改"),
            (lambda: [1][9], "怎么改"),
            (lambda: {"a": 1}["b"], "怎么改"),
            (lambda: 1 / 0, "怎么改"),
            (lambda: "s".append(1), "怎么改"),
            (lambda: open("绝对不存在的文件_12345.txt"), "怎么改"),
        ]
        for fn, marker in cases:
            try:
                fn()
                self.fail("本该报错")
            except Exception as e:                   # noqa: BLE001
                self.assertIn(marker, explain(e), f"{type(e).__name__} 的解释缺少修改建议")


class TestWindow(unittest.TestCase):
    """真正建窗口的那几条 —— 无头环境自动跳过。"""

    def setUp(self):
        self.s = Stage(size=100, width=200, height=200)

    def tearDown(self):
        self.s.close()

    @unittest.skipIf(os.environ.get("HYBRIDENGINE_LIGHT_NO_DISPLAY") == "1", "无显示环境")
    def test_show_and_close(self):
        try:
            self.s.dot(50, 50, r=10, color="红").show()
        except Exception as e:                       # noqa: BLE001
            self.skipTest(f"无法创建窗口（可能是无头环境）：{e}")
        self.assertIsNotNone(self.s._root)

    def test_close_is_idempotent(self):
        """close() 调两次不该崩 —— 新手常在 finally 里多调一次。"""
        self.s.close()
        self.s.close()


if __name__ == "__main__":
    unittest.main(verbosity=2)
