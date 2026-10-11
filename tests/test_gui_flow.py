"""界面全流程判据（`learn_gui.py` + `learnmodel.py`）。

## ⚠⚠ 最要紧的一条：必须用**真的** `mainloop()`

我第一版用 `while ...: root.update()` 轮询来"驱动"界面 —— 那**不是**真实环境：

    RuntimeError: main thread is not in main loop

Tk 的线程检查条件是"**主线程在不在 `mainloop()` 里**"。
· 真应用：`界面().mainloop()` ⇒ 主线程在 mainloop ⇒ 后台线程碰 Tcl 是**允许**的
  （Tk 会把调用转发到主线程）
· `update()` 轮询：主线程**不在** mainloop ⇒ 同样的调用就抛异常

⇒ 用 `update()` 轮询测出来的结论**不作数** —— 它会报出真应用里不会发生的错，
  也可能掩盖真应用里会发生的错。**测法必须和真实用法一致。**

所以这份用 `after()` 排好步骤，然后跑**真的** `mainloop()`。

## 测什么

  构造 → 跳过开场 → 选课 → 改代码 → 保存 → 检查（后台线程）→ 运行
  → 报告回到界面 → 结果徽标画出东西

⚠ **不测**：窗口长什么样、动画好不好看。那需要人眼。
"""
from __future__ import annotations

import os
import sys
import unittest

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, LC)


class 界面全流程测试(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        try:
            import tkinter
            r = tkinter.Tk()
            r.destroy()
        except Exception as e:
            raise unittest.SkipTest(f"没有显示环境：{e}")

    def setUp(self):
        import tempfile
        self.临时 = tempfile.mkdtemp(prefix="ms_gui_flow_work_")
        os.environ["HYBRIDENGINE_LIGHT_ROOT"] = self.临时
        # 造最小课程内容（1 课，答案可判）
        os.makedirs(os.path.join(self.临时, "lessons", "solutions"), exist_ok=True)
        with open(os.path.join(self.临时, "lessons", "01_测试.py"), "w", encoding="utf-8") as f:
            f.write(
                "# 第 01 课：测试课\n# 要做：让 CHECK_数据 返回真\n\n"
                "def CHECK_数据(答案):\n"
                "    return getattr(答案, '值', None) == 42\n")
        with open(os.path.join(self.临时, "lessons", "solutions", "01_测试.py"),
                  "w", encoding="utf-8") as f:
            f.write("值 = 42\n")

    def tearDown(self):
        import shutil
        os.environ.pop("HYBRIDENGINE_LIGHT_ROOT", None)
        shutil.rmtree(self.临时, ignore_errors=True)

    def _跑流程(self, 代码: str) -> dict:
        """用**真 mainloop** 跑一遍界面流程，返回观察到的结果。"""
        from hybridengine_light.learn_gui import 界面
        观察: dict = {}
        # ⚠ 现在双击先进**主页**（两个入口），课程树要进了「课程学习」才存在。
        #   实测：不加这两行会 AttributeError: 'tkapp' object has no attribute '树'
        g = 界面()
        # ⚠⚠ 但**必须关掉后台载入**：`进课程学习` 会顺手调度
        #   `_后台载入编译型`（起 4 次 `dotnet run` 子进程，把结果塞进队列）。
        #   这个测试原来靠"自己排的 after 链条 + destroy"首尾闭死地跑完，
        #   后台消息一进来就把链条打乱 ⇒ **mainloop 永远不退出，表现为整组测试挂住**。
        #   实测：加 `进课程学习()` 之后这两条从 1.7 秒变成 60 秒不返回。
        #   ⇒ 测试不该依赖真子进程，这里直接把"补过了"标上，跳过调度。
        g._编译型补过 = True
        g.进课程学习()

        def 一步(序):
            if 序 == 0:
                # 跳过开场动画（模拟用户点了一下）
                g.event_generate("<Button-1>", x=5, y=5)
                g.after(220, 一步, 1)
            elif 序 == 1:
                观察["遮罩没了"] = not (hasattr(g, "遮罩") and g.遮罩.winfo_exists())
                顶 = g.树.get_children()
                观察["顶层节点数"] = len(顶)
                if not 顶:
                    观察["错"] = "课程树是空的"
                    g.destroy(); return

                # ⚠⚠ 树的层次变过：现在是 **分组 → 语言 → 课**。
                #   原来这里是"顶层就是语言、它的孩子就是课"，
                #   而改成分组之后 `顶[0]` 是「编程语言」这个**分组**，
                #   它的孩子是**语言**而不是课 ——
                #   `selection_set(子[0])` 于是选中了「Python 基础」，
                #   那是**点不出任何东西**的（语言节点没有课的内容）。
                #   症状：流程卡在序 1，mainloop 永不退出 ⇒ 整组测试挂住。
                #   ⇒ 递归找到**第一条真正的课**（课记在 `_树到课` 里）。
                def 第一课(父):
                    for i in g.树.get_children(父):
                        if i in g._树到课:
                            return i
                        r = 第一课(i)
                        if r:
                            return r
                    return None

                年级 = 第一课("")
                if not 年级:
                    观察["错"] = "找不到任何一课"
                    g.destroy(); return
                # 语言节点 = 它的父；课数 = 那个节点下的课数
                语言节点 = g.树.parent(年级)
                观察["首个语言节点"] = g.树.item(语言节点, "text").strip()
                观察["首线课数"] = len([x for x in g.树.get_children(语言节点)
                                       if x in g._树到课])
                g.树.selection_set(年级)
                g.after(320, 一步, 2)
            elif 序 == 2:
                观察["标题"] = g.编辑标题.cget("text").strip()
                观察["编辑器字数"] = len(g.代码.get("1.0", "end-1c"))
                观察["正文字数"] = len(g.正文.get("1.0", "end-1c"))
                g.代码.delete("1.0", "end")
                g.代码.insert("1.0", 代码)
                g.点保存()
                g.after(150, 一步, 3)
            elif 序 == 3:
                观察["保存后状态"] = g.状态.cget("text")
                g.点检查()
                g.after(250, 一步, 4)
            elif 序 == 4:
                # 等后台线程回来（最多 30 秒）
                if g._忙 and 观察.get("_等", 0) < 120:
                    观察["_等"] = 观察.get("_等", 0) + 1
                    g.after(250, 一步, 4)
                    return
                观察["报告"] = g.结果.get("1.0", "end-1c")
                观察["徽标件数"] = len(g.判定画布.find_all())
                g.点运行()
                g.after(250, 一步, 5)
            elif 序 == 5:
                if g._忙 and 观察.get("_等2", 0) < 120:
                    观察["_等2"] = 观察.get("_等2", 0) + 1
                    g.after(250, 一步, 5)
                    return
                观察["运行输出"] = g.结果.get("1.0", "end-1c")
                g.destroy()

        g.after(120, 一步, 0)
        g.mainloop()          # ⚠⚠ 真的 mainloop —— 见文件头说明
        return 观察

    def test_全流程_答案是错的(self):
        观察 = self._跑流程("值 = 1\n")
        self.assertNotIn("错", 观察, 观察.get("错"))
        self.assertTrue(观察["遮罩没了"], "开场动画没被跳过")
        self.assertGreaterEqual(观察["顶层节点数"], 1)
        self.assertIn("测试课", 观察["标题"])
        self.assertGreater(观察["编辑器字数"], 0, "编辑器没填内容")
        self.assertGreater(观察["正文字数"], 0, "正文页没填内容")
        self.assertIn("已保存", 观察["保存后状态"])
        报告 = 观察["报告"]
        self.assertGreater(len(报告), 40, f"报告太短，可能是空/报错：{报告!r}")
        self.assertNotIn("main thread is not in main loop", 报告)

    def test_全流程_答案是对的(self):
        观察 = self._跑流程("值 = 42\n")
        self.assertNotIn("错", 观察, 观察.get("错"))
        报告 = 观察["报告"]
        self.assertGreater(len(报告), 40, f"报告太短：{报告!r}")
        # 答案对 ⇒ 报告里应当有勾
        self.assertTrue(("√" in 报告) or ("✓" in 报告), f"对的答案没给出勾：{报告!r}")
        # 结果徽标应当画出东西（只在报告里有 ✓/✗ 时才弹）
        self.assertGreater(观察["徽标件数"], 0, "结果徽标没画出来")


def 找节点(树, 片段: str):
    """在整棵树里**递归**找第一个文字含 `片段` 的节点，返回 item id（找不到 None）。

    ⚠ 为什么需要它：树的层数会变（现在是 **分组 → 语言 → 课**）。
      判据里写死 `树.get_children()[0]` 这种"第几个"的写法，
      一改结构就红 —— 而**结构改动是正常的**，判据该认的是"有没有那一项"。
    """
    def 走(父):
        for i in 树.get_children(父):
            if 片段 in 树.item(i, "text"):
                return i
            r = 走(i)
            if r:
                return r
        return None
    return 走("")


def 全文字(树) -> str:
    """把整棵树的所有节点文字拼起来（用来查"有没有出现某个词"）。"""
    出 = []
    def 走(父):
        for i in 树.get_children(父):
            出.append(树.item(i, "text"))
            走(i)
    走("")
    return " ".join(出)


class 主页与语言列表测试(unittest.TestCase):
    """主页两个入口 + 「课程学习」直接把语言罗列出来（作者要求）。

    ⚠ 必须用**真 mainloop**：Tk 的线程检查条件是
      "主线程在不在 mainloop() 里"，用 update() 轮询会得到假错。

    ⚠ 这些判据守的是**导航结构**：
      双击之后是不是先看到两个入口、语言是不是第一层、
      Esc 能不能回主页、编译型有没有先占位。
      实测踩过：改完结构之后 `树` 要进了「课程学习」才存在，
      而旧测试直接 `g.树` ⇒ AttributeError。所以这里也顺带守住这一点。
    """

    def _走一遍(self, 做):
        from hybridengine_light import learn_gui as G
        g = G.界面()
        结果 = {}

        def 跑():
            try:
                做(g, 结果)
            finally:
                g.after(150, g.destroy)

        g.after(1300, 跑)          # 等开场动画（可跳过，但这里就等它）
        g.mainloop()
        return 结果

    def test_启动先进主页而不是学习界面(self):
        结果 = self._走一遍(lambda g, r: r.update(有主页=g.主页.winfo_exists() == 1))
        self.assertTrue(结果.get("有主页"), "启动后没有主页")
        # 主页上应当有**两个**入口按钮
        r = self._走一遍(lambda g, x: x.update(
            按钮数=sum(1 for w in g.主页.winfo_children()[0].winfo_children()
                      if isinstance(w, __import__("tkinter").Frame))))
        self.assertGreaterEqual(r.get("按钮数", 0), 1)

    def test_课程学习把语言与专题分开罗列(self):
        """⚠ 作者要求：**专题**（AI / 嵌入式 / 树莓派）要单独一类。

        把它们和 Python / C# 并列，学员会以为「树莓派」是一门语言。
        """
        def 做(g, r):
            g.进课程学习()
            g.update()
            r["顶层"] = [g.树.item(x, "text").strip() for x in g.树.get_children()]
            r["全部"] = 全文字(g.树)
            r["有树"] = hasattr(g, "树")
        结果 = self._走一遍(做)
        self.assertTrue(结果.get("有树"), "进了课程学习却没有课程树")
        顶层 = 结果["顶层"]
        self.assertTrue(any("编程语言" in x for x in 顶层),
                        f"没有「编程语言」分组：{顶层}")
        self.assertTrue(any("专题" in x for x in 顶层),
                        f"没有「专题」分组 —— AI/嵌入式/树莓派 没被归为一类：{顶层}")
        # 语言要真的在树里（不一定是第一层了）
        全部 = 结果["全部"]
        for 词 in ("Python", "C#", "C++", "Java"):
            self.assertIn(词, 全部, f"语言列表里没有 {词}")
        # 编译型要先占位（显示"载入中"），而不是不出现
        self.assertIn("载入中", 全部,
                      "编译型四条线没有先占位 —— 学员会以为界面上没有 C# / C++")

    def test_Python展开后是17课(self):
        """⚠ 是 **17** 课：01~16 加后来补的「第 00 课：跑通第一个程序」。

        改这个数之前先想清楚：课数是从 lessons/ 目录扫出来的真值，
        不是写死的配置。加了课就要改这里 —— 这是**有意的**摩擦，
        免得课悄悄丢了也没人发现。
        """
        def 做(g, r):
            g.进课程学习()
            g.update()
            节 = 找节点(g.树, "Python")
            r["课数"] = len(g.树.get_children(节)) if 节 else 0
        结果 = self._走一遍(做)
        self.assertEqual(结果.get("课数"), 17,
                         f"Python 应当是 17 课（第 00 课 + 01~16，一课只讲一个语法），"
                         f"实际 {结果.get('课数')}")

    def test_游戏编写直接展开(self):
        def 做(g, r):
            g.进游戏编写()
            g.update()
            顶 = g.树.get_children()
            r["组"] = [g.树.item(x, "text") for x in 顶]
            # 展开的可以是分组，也可以是分组下的那条线
            r["展开"] = [g.树.item(x, "text") for x in 顶 if g.树.item(x, "open")]
            for x in 顶:
                r["展开"] += [g.树.item(y, "text") for y in g.树.get_children(x)
                            if g.树.item(y, "open")]
        结果 = self._走一遍(做)
        self.assertTrue(结果.get("组"), "游戏编写里什么都没有")
        self.assertTrue(结果.get("展开"), "游戏编写的分组没有展开（还得再点一次）")

    def test_能回主页(self):
        def 做(g, r):
            g.进课程学习()
            g.update()
            r["学习界面在"] = g.学习界面 is not None and g.学习界面.winfo_exists() == 1
            g.回主页()
            g.update()
            r["主页在"] = g.主页.winfo_exists() == 1
            r["学习界面还在（只是藏起来）"] = g.学习界面.winfo_exists() == 1
        结果 = self._走一遍(做)
        self.assertTrue(结果.get("学习界面在"))
        self.assertTrue(结果.get("主页在"), "回不去主页")
        self.assertTrue(结果.get("学习界面还在（只是藏起来）"),
                        "回主页时把学习界面销毁了 —— 再进来会重跑一遍课程发现")


if __name__ == "__main__":
    unittest.main()
