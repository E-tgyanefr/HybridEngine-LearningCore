"""内置编辑器的判据（`hybridengine_light/editor.py`）。

⚠ **这一份测什么、不测什么**（写在最前面，免得高估它）：

  测：编辑逻辑（插入/换行缩进/退格/删除/撤销/保存）+ Esc 两步确认
      + 光标边界 + `_画()` 能产出转义序列。
  **不测**：真终端下的全屏渲染与键盘读取。
      ⚠ 那需要 `sys.stdin.isatty()`，而测试跑在非交互环境里。
        我没有假装测过它 —— 那部分只有人工在终端里敲一遍才算数。
"""
from __future__ import annotations

import contextlib
import io
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from hybridengine_light.editor import 编辑器, 上, 下, 左, 右      # noqa: E402


class 编辑器测试(unittest.TestCase):
    def setUp(self):
        self.目录 = os.path.join(os.environ.get("TEMP", "/tmp"), "hel_editor_test")
        os.makedirs(self.目录, exist_ok=True)
        self.计数 = 0

    def 新建(self) -> 编辑器:
        self.计数 += 1
        p = os.path.join(self.目录, f"t{self.计数}.py")
        if os.path.isfile(p):
            os.remove(p)
        return 编辑器(p)

    # ── 打字与保存 ──
    def test_新文件与插入(self):
        e = self.新建()
        self.assertEqual(e.行, [""])
        for ch in "abc":
            e.插入(ch)
        self.assertEqual(e.行[0], "abc")
        self.assertEqual(e.x, 3)
        self.assertTrue(e.脏)

    def test_保存写盘且清除脏标记(self):
        e = self.新建()
        for ch in "abc":
            e.插入(ch)
        self.assertTrue(e.保存())
        with io.open(e.路径, encoding="utf-8") as f:
            self.assertEqual(f.read(), "abc\n")
        self.assertFalse(e.脏)

    def test_保存会建父目录(self):
        # ⚠ `open(..., "w")` 不会建父目录 —— 不建的话首次保存就失败，
        #   而学员看到的是"保存失败"，会以为编辑器坏了。
        p = os.path.join(self.目录, "深", "更深", "t.py")
        e = 编辑器(p)
        e.插入("x")
        self.assertTrue(e.保存())
        self.assertTrue(os.path.isfile(p))

    # ── 换行保持缩进（新手最常被这个绊）──
    def test_换行保持缩进(self):
        e = self.新建()
        for ch in "    x = 1":
            e.插入(ch)
        e.换行()
        self.assertEqual(e.y, 1)
        self.assertTrue(e.行[1].startswith("    "),
                        "新行应当带同样的缩进；不带的话学员写函数体会被打回行首")

    def test_空行换行不缩进(self):
        e = self.新建()
        e.换行()
        self.assertEqual(e.行[1], "")

    # ── 删除 ──
    def test_行首退格合并上一行(self):
        e = self.新建()
        e.插入("ab")
        e.换行()
        e.插入("cd")
        e.x = 0
        e.退格()
        self.assertEqual(e.行, ["abcd"])
        self.assertEqual(e.y, 0)
        self.assertEqual(e.x, 2)

    def test_行尾Delete合并下一行(self):
        e = self.新建()
        e.插入("ab")
        e.换行()
        e.插入("cd")
        e.y, e.x = 0, 2
        e.删除()
        self.assertEqual(e.行, ["abcd"])

    def test_撤销单步(self):
        e = self.新建()
        e.插入("hello")
        e.撤销()
        self.assertEqual(e.行[0], "")
        self.assertEqual(e.x, 0)

    # ── 光标边界：空文件里所有键都不该崩 ──
    def test_空文件所有边界键不崩(self):
        e = self.新建()
        for k in (左, 上, "BACKSPACE", "DELETE", "HOME", "END", "PGUP", "PGDN"):
            e.处理(k, 24)
        self.assertEqual(e.行, [""])

    def test_光标移动(self):
        e = self.新建()
        e.插入("abc")
        e.处理(左, 24)
        e.处理(左, 24)
        self.assertEqual(e.x, 1)
        e.处理("HOME", 24)
        self.assertEqual(e.x, 0)
        e.处理("END", 24)
        self.assertEqual(e.x, 3)

    def test_上下移到短行列被夹取(self):
        # ⚠ 不夹取的话 x 会越界，再打字就 IndexError 或写错位置
        e = self.新建()
        e.插入("abcdef")
        e.换行()
        e.插入("x")
        e.y, e.x = 0, 6
        e.处理(下, 24)
        self.assertEqual(e.y, 1)
        self.assertLessEqual(e.x, len(e.行[1]))

    def test_Tab是四个空格不是制表符(self):
        e = self.新建()
        e.处理("\t", 24)
        self.assertEqual(e.行[0], "    ")
        self.assertNotIn("\t", e.行[0])

    # ── Esc 两步确认（这一条最要紧：一次就退会丢代码）──
    def test_有未保存改动时Esc要按两次(self):
        e = self.新建()
        e.插入("重要代码")
        e.处理("ESC", 24)
        self.assertFalse(e.退出, "第 1 次 Esc 不该退出")
        self.assertIn("未保存", e.消息)
        self.assertEqual(e.行[0], "重要代码", "代码必须还在（不能假装已保存）")
        self.assertTrue(e.脏)
        e.处理("ESC", 24)
        self.assertTrue(e.退出)

    def test_没有改动时Esc一次就退(self):
        e = self.新建()
        e.处理("ESC", 24)
        self.assertTrue(e.退出)

    def test_Esc后按别的键确认作废(self):
        e = self.新建()
        e.插入("x")
        e.处理("ESC", 24)
        e.处理("a", 24)
        self.assertFalse(e._esc待确认, "按了别的键 ⇒ 之前的 Esc 提醒该作废")

    # ── 保存并运行（F5）──
    def test_F5设置保存并跑标记(self):
        e = self.新建()
        e.插入("print(1)")
        e.处理("F5", 24)
        self.assertTrue(e.退出)
        self.assertTrue(e.保存并跑)

    def test_CtrlC保存并退出(self):
        e = self.新建()
        e.插入("a")
        e.处理("CTRL_C", 24)
        self.assertTrue(e.退出)
        self.assertFalse(e.脏)
        self.assertTrue(os.path.isfile(e.路径))

    # ── `_画()` 能产出转义序列 ──
    #    ⚠ 这**不是**渲染测试：只证明"不会崩、序列形状对"。
    def test_画产出转义序列(self):
        e = self.新建()
        e.插入("print(1)")
        buf = io.StringIO()
        with contextlib.redirect_stdout(buf):
            e._画(80, 24)
        out = buf.getvalue()
        self.assertIn("\x1b[H", out)        # 回家（不清屏 ⇒ 不闪）
        self.assertIn("\x1b[K", out)        # 清到行尾
        self.assertIn("print(1)", out)
        self.assertIn("\x1b[", out)         # 光标定位

    def test_画在极小窗口下不崩(self):
        e = self.新建()
        e.插入("x")
        buf = io.StringIO()
        with contextlib.redirect_stdout(buf):
            e._画(20, 6)
        self.assertTrue(buf.getvalue())


if __name__ == "__main__":
    unittest.main()
