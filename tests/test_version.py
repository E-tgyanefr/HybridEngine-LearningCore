"""版本号的判据 —— 守着**作者的要求**，以及"各处不许各写各的"。

## 为什么值得一整套判据

实测踩过两回，都是"没人守着就漂了"：

  ① 我发 Release 时**自己编了** `v1.0.0`，而代码里是 `0.1.0`
     ⇒ 作者的要求是「第一位保持 0」，我违反了
  ② README 里手写「当前 **0.1.4**」，而代码里已经是 `0.1.3`
     ⇒ **文案漂了而没人发现**（文案里的版本号没有任何东西守着）

⇒ 这两条现在都有判据。**"约定写在文档里"不等于"约定会被遵守"** ——
  能写成判据的就写成判据。
"""
from __future__ import annotations

import os
import re
import sys
import unittest

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, LC)

import hybridengine_light as H                                   # noqa: E402


class 版本号规则测试(unittest.TestCase):
    def test_第一位必须是0(self):
        """⚠ 作者明确要求：第一位保持 0，不要 1.0.0 及以上。

        `1.0.0` 的含义是「已稳定发布」—— 而这个项目还没到那一步。
        """
        主 = int(H.__version__.split(".")[0])
        self.assertEqual(
            主, 0,
            f"第一位是 {主}，但作者要求保持 0（版本号 {H.__version__}）。"
            "要升 1.0.0 必须先得到作者的明确同意。")

    def test_形如三段数字(self):
        self.assertRegex(H.__version__, r"^\d+\.\d+\.\d+$",
                         f"版本号应当形如 0.x.y，实际是 {H.__version__!r}")

    def test_不是占位版本号(self):
        # 0.0.0 通常意味着"忘了改"
        self.assertNotEqual(H.__version__, "0.0.0")

    def test_README里的版本与代码一致(self):
        """⚠ 这条防的是实测踩到的第 ② 类漂移。"""
        路 = os.path.join(LC, "README.md")
        with open(路, encoding="utf-8") as f:
            文 = f.read()
        m = re.search(r"当前 \*\*(\d+\.\d+\.\d+)\*\*", 文)
        self.assertIsNotNone(
            m, "README 里找不到「当前 **X.Y.Z**」那句 —— "
               "要么被删了，要么改了措辞（那这条判据就失效了）")
        self.assertEqual(
            m.group(1), H.__version__,
            f"README 写的是 {m.group(1)}，而代码里是 {H.__version__} —— "
            "文案漂了。改版本号时记得**两处一起改**。")

    def test_规则文档存在且覆盖作者要求(self):
        # 约定要写在代码旁边（写在对话里不算），而且要写明"第一位保持 0"
        路 = os.path.join(LC, "docs", "版本号规则.md")
        self.assertTrue(os.path.isfile(路), "docs/版本号规则.md 不见了")
        with open(路, encoding="utf-8") as f:
            文 = f.read()
        self.assertIn("第一位", 文, "规则文档里没写「第一位」这条要求")
        self.assertIn("0", 文)


if __name__ == "__main__":
    unittest.main()
