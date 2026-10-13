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

    def test_答题文件绝不能是课程文件自己(self):
        """★ 这条是**防事故**的，不是防 bug 的。

        ⚠ 原来的 `_答案文件()` 对 ai / embedded / raspi 直接
          `return k.源文件` —— 答题文件和课程文件是**同一个文件**。
          后果：GUI 里"答案"框显示的是课程本身，学员一保存就把课程覆盖掉，
          而且他不知道自己毁了什么。

        ⚠ 为什么一直没被发现：CLI（`python -m hybridengine_light AI 01`）
          走的是各模块自己的 `_检查()`，那条路**一直是对的**。
          同一个概念在 CLI 和 GUI 里算出了两个答案，而**看起来都对**。

        ⇒ 所以判据要写成"**遍历所有课程线**"，而不是只测 Python 那条 ——
          只测一条的话，另外几条各错各的也照样绿。
        """
        坏 = []
        for 轨道 in M.轨道:
            try:
                课们 = M.发现(轨道)
            except Exception:                        # noqa: BLE001
                continue
            for k in 课们:
                try:
                    答 = M._答案文件(k)
                except Exception:                    # noqa: BLE001
                    continue
                if 答.resolve() == k.源文件.resolve():
                    坏.append(f"{轨道} {k.编号}：答题文件 == 课程文件（{答.name}）")
        self.assertEqual(坏, [], "以下课程的答题文件就是课程文件本身，"
                                 "学员保存时会覆盖课程：\n  " + "\n  ".join(坏))

    def test_答题文件都在对应线的目录下(self):
        """每条线的答题文件应该待在自己那条线的 answers 目录里。"""
        for 轨道 in M.轨道:
            try:
                课们 = M.发现(轨道)
            except Exception:                        # noqa: BLE001
                continue
            if not 课们:
                continue
            k = 课们[0]
            答 = str(M._答案文件(k)).replace("\\", "/")
            with self.subTest(轨道=轨道):
                self.assertTrue(
                    "answers" in 答 or "solutions" in 答,
                    f"{轨道} 的答题文件不在 answers 目录下：{答}")


class 看课命令行测试(unittest.TestCase):
    """★ 课程正文里的命令行必须跟着**当前环境**变。

    ⚠ 打包成 exe 之后没有 `python` 命令。课程正文（注释）里写死了
      `python -m hybridengine_light 树莓派 检查 01`，学员照着敲会得到
      "不是内部或外部命令" —— **错的其实是课程**。

    ⚠ 为什么不在课程文件里直接改掉：从源码跑的时候那个写法**本来就是对的**
      （开发者就是这么跑）。写死了 exe 名字反而在源码环境下错。
      ⇒ 正确做法是**在显示的那一刻**按环境替换。
    """

    def test_源码环境下命令行保持python写法(self):
        from hybridengine_light._paths import 命令名
        文 = "#   python -m hybridengine_light 树莓派 检查 01"
        出 = M._把命令行写对(文)
        self.assertIn(命令名(), 出)

    def test_exe环境下命令行不能出现python(self):
        """模拟 frozen，确认写死的 python 会被换掉。"""
        from hybridengine_light import _paths
        原 = _paths.命令名
        try:
            _paths.命令名 = lambda: "HybridEngine-light.exe"
            文 = ("#   python -m hybridengine_light 树莓派 检查 01\n"
                  "# 卡住了：python -m hybridengine_light AI 答案 02")
            出 = M._把命令行写对(文)
        finally:
            _paths.命令名 = 原
        self.assertNotIn("python -m", 出)
        self.assertIn("HybridEngine-light.exe", 出)

    def test_四条线的看课都不抛(self):
        for 轨道 in ("ai", "embedded", "raspi", "enginecourse"):
            课们 = M.发现(轨道)
            if not 课们:
                continue
            with self.subTest(轨道=轨道):
                self.assertTrue(M.看课(课们[0]).strip())


class 每条线都能生成答题模板测试(unittest.TestCase):
    """★ 防"分派链漏了一条线"。

    ⚠⚠ 真实事故：`_确保模板()` 的分派链原本是
          if python: …
          elif 轨道 in ("ai", "embedded", "raspi"): …
       —— **漏了 enginecourse**。而 `enginecourse.py` 里明明有 `_生成模板`，
       只是没人调它。

       症状：引擎线的答题文件**永远不生成**，`读答案()` 返回空字符串，
       GUI 里"答案框一片空白"且**不报错**。最难查的那一类。

       ⇒ 判据写成"**遍历这一组所有轨道**、每一个都要能生成出非空模板"，
         而不是只测一两条 —— 只测一条的话，别的线各漏各的也照样绿。

    ⚠ 范围要和**机制的适用范围**一致：只测走同一个 `_确保模板` 分派链的那组。
      编译型（c/cpp/java/csharp）的模板是 **C# 课程程序**在第一次"检查"时
      生成的，走另一条路；`games` 是给人读的成品，**没有"答题"这回事**。
      我第一版把这两类也塞进来，于是判据报了假警 —— **一个漏洞只发生在
      "共用一条分派链"的那些线里**，判据就该收在那一组。
    """

    #: 走 `_确保模板` 分派链的课程线（bug 正是出在这条链上）
    这组 = ("python", "ai", "embedded", "raspi", "enginecourse")

    def test_每条线都能生成非空的答题模板(self):
        from hybridengine_light import learnmodel as M
        坏 = []
        for 轨道 in self.这组:
            self.assertIn(轨道, M.轨道, "轨道表里没有 %s" % 轨道)
            课们 = M.发现(轨道)
            if not 课们:
                坏.append("%s：一课都没发现" % 轨道)
                continue
            k = 课们[0]
            try:
                文 = M.读答案(k)
            except Exception as e:                            # noqa: BLE001
                坏.append("%s：读答案抛了 %s" % (轨道, e))
                continue
            n = len([x for x in 文.split("\n") if x.strip()])
            if n < 10:
                坏.append("%s：模板只有 %d 行非空（像没生成）" % (轨道, n))
        self.assertEqual(坏, [], "以下课程线生成不出像样的答题模板：\n  "
                                 + "\n  ".join(坏))


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
        # ⚠ mock 的层次要与实现一致：实现现在走 `_找工具("dotnet")`
        #   （它同时管便携目录与 PATH）。只 mock `有dotnet` 就不生效了 ——
        #   实测就是因此红的（`工程.parent.parent` 拿到字符串，报 TypeError）。
        import pathlib
        import unittest.mock as mock
        with mock.patch.object(M, "_找工具", return_value=None):
            码, 文 = M._运行课程程序(pathlib.Path(self.临时), [])
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


class 便携工具链测试(unittest.TestCase):
    """「能配的都配好」的核心机制：**工具链放对位置就自动认**。

    ## 为什么值得一整套判据

    ⚠ `.NET SDK` 4GB、MinGW 914MB、JDK 373MB —— **装不进 11MB 的发布包**。
      所以只能做成"放这儿就认"。而"自动认"这件事**最容易 quietly 失效**：
      路径拼错、少个 .exe、扫描顺序不对 —— 表现都是"学员明明放了却不生效"，
      而那时他已经在怀疑自己了。

    ## 判据覆盖

      · 能扫到 `tools/<工具>.exe`
      · 能扫到 `tools/<工具>/bin/<工具>.exe`（便携包常见布局）
      · 能扫到 `tools/<任意名>/bin/<工具>.exe`（目录名五花八门）
      · **便携优先于 PATH**（放了的就该被用）
      · 找不到就退回 PATH
      · 体检里能反映出便携工具（否则自检在骗人）
    """

    def setUp(self):
        import tempfile
        self.根 = tempfile.mkdtemp(prefix="ms_tools_work_")
        self.旧env = os.environ.get("HYBRIDENGINE_LIGHT_ROOT")
        os.environ["HYBRIDENGINE_LIGHT_ROOT"] = self.根
        M.清缓存()                    # 缓存与根绑定，换根要清
        M._工具缓存.clear()           # ⚠ 工具缓存也要清，否则上一个用例的结果还在

    def tearDown(self):
        import shutil
        if self.旧env is None:
            os.environ.pop("HYBRIDENGINE_LIGHT_ROOT", None)
        else:
            os.environ["HYBRIDENGINE_LIGHT_ROOT"] = self.旧env
        M._工具缓存.clear()
        M.清缓存()
        shutil.rmtree(self.根, ignore_errors=True)

    def _放假工具(self, 相对: str) -> str:
        """造一个假的可执行文件，返回它的路径。

        ⚠⚠ **必须 `.resolve()`** —— 这条是三平台 CI 在 **macOS** 上抓出来的：

            AssertionError: '/private/var/folders/.../dotnet.exe'
                         != '/var/folders/.../dotnet.exe'

          macOS 上 `/var` 是 `/private/var` 的**符号链接**，而 `tempfile.mkdtemp()`
          给的是 `/var/...`；`M._找工具()` 内部会 `resolve()` ⇒ 返回 `/private/var/...`。
          **两边都没错，是"同一个目录的两种写法"** —— 拿未 resolve 的去比就永远不等。

        ⚠ 在 Windows 上 `resolve()` 是恒等的（本机路径没有软链）⇒ 加了不改变行为。
        ⚠ 这不是"为了让测试过而放宽判据"：比的是**同一个文件**，
          只是先归一化写法。**判据的强度没变**（还是 `==`，不是 `in` 或 `samefile` 之外的宽松比较）。
        """
        import pathlib
        p = (pathlib.Path(self.根) / 相对).resolve()
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text("#!/bin/sh\n", encoding="utf-8")
        return str(p)

    def test_扫到直接在tools下的工具(self):
        路径 = self._放假工具("tools/dotnet.exe")
        M._工具缓存.clear()
        self.assertEqual(M._找工具("dotnet"), 路径)

    def test_扫到tools里的工具目录(self):
        路径 = self._放假工具("tools/dotnet/dotnet.exe")
        M._工具缓存.clear()
        self.assertEqual(M._找工具("dotnet"), 路径)

    def test_扫到任意名字目录下的bin(self):
        # 便携包目录名五花八门（mingw64 / jdk-21 / dotnet-sdk-8 ...）
        路径 = self._放假工具("tools/mingw64/bin/g++.exe")
        M._工具缓存.clear()
        self.assertEqual(M._找工具("g++"), 路径)

    def test_便携优先于PATH(self):
        """⚠ 这条最要紧：放了的就该被用，而不是被系统 PATH 上的抢走。"""
        路径 = self._放假工具("tools/dotnet.exe")
        M._工具缓存.clear()
        找到 = M._找工具("dotnet")
        self.assertEqual(找到, 路径,
                         "便携目录里的工具没被优先使用 —— 那用户放了也白放")

    def test_没有便携就退回PATH(self):
        # 这个用例的临时根里没有任何 tools ⇒ 应当退回系统 PATH
        M._工具缓存.clear()
        找到 = M._找工具("dotnet")
        import shutil as _sh
        self.assertEqual(找到, _sh.which("dotnet"))

    def test_体检能反映便携工具(self):
        """⚠ 自检不能说谎：便携目录里的工具必须出现在体检里。"""
        self._放假工具("tools/javac.exe")
        M._工具缓存.clear()
        d = {名: (好, 值) for 名, 好, 值 in M.体检()}
        键 = [k for k in d if "javac" in k]
        self.assertTrue(键, "体检里没有 javac 这一项")
        好, 值 = d[键[0]]
        self.assertTrue(好, f"便携目录里有 javac，体检却说没有：{值}")
        self.assertIn("tools", 值.replace("\\", "/"), f"体检没指向便携路径：{值}")

    def test_体检给出缺了怎么装(self):
        """⚠ 只说"缺 .NET"等于把墙留给学员撞 —— 必须带上安装命令。"""
        import pathlib
        # 把便携目录指到一个空地方，并让 PATH 查不到 dotnet
        d = {名: (好, 值) for 名, 好, 值 in M.体检()}
        键 = [k for k in d if ".NET" in k]
        self.assertTrue(键)
        好, 值 = d[键[0]]
        if not 好:
            self.assertIn("install", 值, f"缺 .NET 时没给出安装方式：{值}")


if __name__ == "__main__":
    unittest.main()
