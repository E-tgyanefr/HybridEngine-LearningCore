"""行为指纹的判据 —— C++ 移植的**尺子**，外加"这尺子有刻度"的证明。

    python -m unittest tests.test_engine_fingerprint -v

## 这个文件守两件事，第二件比第一件重要

### ① 现 Python 版的行为没变
重跑一遍指纹生成，与落盘的 `_build/_engine_fingerprint.json` **逐条**比对。
任何行为变化（图元顺序、坐标末位、组件状态…）当场报红。

### ② ⚠⚠ **这把尺子本身有刻度**
这个仓**连续栽过两次假绿**：
  1. `报.count("✗")` 恒等于 0（那条线用的是 `√`/`×`）⇒ "零个不过"其实是"没检查"
  2. 改成数一组符号，却用**全串计数** ⇒ 把检查名字里的乘号 `×` 当成了"不过"

⇒ 所以这里有一整组**反面验证**（`C` 类）：**故意把指纹改坏，必须能被抓到**。
  改坏了却抓不到 ⇒ 判据是假绿 ⇒ 比"没有判据"更危险（因为它给人安全感）。

⚠⚠ 并且专门有一条：**指纹为空 / 图元总数为 0 ⇒ 判「没跑到」，不是"过了"。**

## ⚠ 为什么用一个子进程去重跑生成器
`_engine_fingerprint.py` 会**真开 tkinter 窗口**（量 `Tk后端` 提交了多少 item）。
而本仓实测过：**Tk 在"多模块共用一个解释器"时会硬崩解释器**
（退出码 2147483651，`run_tests.py` 开头就写着这件事，`test_gui_flow` 因此独占一个进程）。
⇒ 判据**不自己开窗**，而是 `subprocess` 起一个干净进程跑生成器、把 JSON 收回来。
   顺带还证明了一件事：**这份指纹是可复现的**（不是谁手工存下来的）。
"""
from __future__ import annotations

import copy
import json
import os
import subprocess
import sys
import unittest

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
_BUILD = os.path.join(LC, "_build")
for p in (LC, _BUILD):
    if p not in sys.path:
        sys.path.insert(0, p)

import _engine_fingerprint as F                                    # noqa: E402

指纹路径 = os.path.join(_BUILD, "_engine_fingerprint.json")
生成脚本 = os.path.join(_BUILD, "_engine_fingerprint.py")

_缓存: dict = {}


def 落盘指纹() -> dict:
    with open(指纹路径, encoding="utf-8") as f:
        return json.load(f)


def 重新生成() -> dict:
    """起一个**干净子进程**重跑生成器（Tk 在那边开，不在本进程里）。

    ⚠ 跑一次就缓存 —— 生成器要开真窗口，没必要跑很多遍。
    """
    if "值" not in _缓存:
        r = subprocess.run(
            [sys.executable, "-X", "utf8", 生成脚本, "--stdout"],
            cwd=LC, capture_output=True, timeout=300)
        出 = r.stdout.decode("utf-8", "replace")
        if r.returncode != 0 or not 出.strip().startswith("{"):
            raise AssertionError(
                "重跑指纹生成器失败（退出码 %s）：\n%s\n%s"
                % (r.returncode, 出[:800], r.stderr.decode("utf-8", "replace")[:800]))
        _缓存["值"] = json.loads(出)
    return _缓存["值"]


# ═══════════════════════ ① 指纹不是空的（**先过这一关**）═══════════════════

class A指纹非空自检(unittest.TestCase):
    """⚠⚠ 「没跑到」必须是**失败**，不是"过了" —— 这组是这个文件存在的理由之一。"""

    def test_自检函数对落盘指纹判通过(self):
        问题 = F.自检(落盘指纹())
        self.assertEqual(问题, [], "指纹自检没过：\n  " + "\n  ".join(问题))

    def test_场景数不为零(self):
        统 = 落盘指纹()["统计"]
        self.assertGreater(统["场景数"], 0,
                           "场景数是 0 —— **没跑到**，这不算通过")
        self.assertEqual(统["场景数"], len(落盘指纹()["内容"]))

    def test_图元总数不为零(self):
        统 = 落盘指纹()["统计"]
        self.assertGreater(统["图元总数"], 0,
                           "图元总数是 0 —— **没跑到**，这不算通过")

    def test_声明该有图元的场景全都真有图元(self):
        """⚠ 声明了"该有图元"却一条都没有 ⇒ 那个场景没跑到。"""
        内容 = 落盘指纹()["内容"]
        声明 = [k for k, v in 内容.items() if v.get("应有图元")]
        self.assertGreater(len(声明), 20, "声明该有图元的场景太少了（覆盖不够）")
        空 = [k for k in 声明 if 内容[k].get("图元数", 0) <= 0]
        self.assertEqual(空, [], "这些场景声明了该有图元却一条都没有：%s" % 空)

    def test_声明该有状态的场景全都真有状态(self):
        内容 = 落盘指纹()["内容"]
        声明 = [k for k, v in 内容.items() if v.get("应有状态")]
        空 = [k for k in 声明 if 内容[k].get("状态条目数", 0) <= 0]
        self.assertEqual(空, [], "这些场景声明了该有状态却是空的：%s" % 空)

    def test_钉了每帧图元数的场景逐帧都对(self):
        """空场景 / 裁剪 / 单对象这几个边界靠**精确值**才有意义。"""
        内容 = 落盘指纹()["内容"]
        有钉的 = [k for k, v in 内容.items() if "期望每帧图元数" in v]
        self.assertGreaterEqual(len(有钉的), 3, "没有一个场景把每帧图元数钉成精确值")
        for k in 有钉的:
            期 = 内容[k]["期望每帧图元数"]
            得 = 内容[k]["实测每帧图元数"]
            self.assertTrue(得, "%s 没有实测每帧图元数" % k)
            for n, x in enumerate(得):
                self.assertEqual(x, 期, "%s 第 %d 帧：%d ≠ 期望 %d" % (k, n, x, 期))

    def test_空场景确实只有清屏(self):
        """⚠ 最容易被"顺手跳过"的边界：一个对象都没有时，每帧**恰好 1 条**。"""
        条 = 落盘指纹()["内容"]["边界_空场景"]
        self.assertEqual(条["实测每帧图元数"], [1, 1, 1])
        self.assertEqual(条["帧"][0]["图元"], [["清屏", "白"]])

    def test_本机能开窗就必须有Tk数据(self):
        """⚠ "环境不支持"和"我们忘了测"是两件事 —— 这条把后者变成红灯。"""
        验 = 落盘指纹()
        if not 验["统计"]["Tk可用"]:
            self.skipTest("本机没有可用的显示环境（生成指纹时 Tk 就不可用）")
        tk = 验["内容"]["Tk后端_提交item数"]["Tk"]
        self.assertTrue(tk.get("可用"),
                        "生成指纹时 Tk 可用，但指纹里没有 Tk 数据 ⇒ 覆盖漏了一块")
        self.assertGreater(tk.get("提交item数", 0), 0)


# ═══════════════════════ ② 逐条比对（行为没变）═════════════════════════

class B指纹逐条比对(unittest.TestCase):
    def test_落盘指纹与重跑逐条相同(self):
        问题 = F.比对(落盘指纹(), 重新生成())
        self.assertEqual(
            问题, [],
            "现 Python 版的行为与落盘指纹**不一致**（前 %d 条）：\n  %s\n"
            "⇒ 要么行为真的变了，要么该重新生成指纹：\n"
            "     python _build/_engine_fingerprint.py" % (len(问题), "\n  ".join(问题)))

    def test_渲染哈希逐场景相同(self):
        """哈希是给 C++ 版一眼对的总账；它必须和逐条比对结论一致。"""
        落, 新 = 落盘指纹()["内容"], 重新生成()["内容"]
        坏 = [k for k in 落
              if 落[k].get("渲染哈希") != 新.get(k, {}).get("渲染哈希")]
        self.assertEqual(坏, [], "这些场景的渲染哈希对不上：%s" % 坏)

    def test_头里的版本号与代码一致(self):
        """⚠ 文件头那两个版本号，决定"是 C++ 版错了"还是"指纹该更新了"。

        红了先别怀疑行为 —— 多半只是版本号变了，重跑一次生成器即可：
            python _build/_engine_fingerprint.py
        """
        import hybridengine_light as H
        头 = 落盘指纹()["头"]
        self.assertEqual(头["指纹格式版本"], F.指纹格式)
        self.assertEqual(
            头["对应产品版本"], H.__version__,
            "指纹头写的是 v%s，而代码里是 v%s —— 版本变了就重新生成指纹"
            % (头["对应产品版本"], H.__version__))

    def test_确定性场景两次相同(self):
        条 = 落盘指纹()["内容"]["确定性_同场景跑两遍"]
        self.assertEqual(条["两次相同"], "真", "同一份代码跑两遍结果不同 ⇒ 引擎不确定")
        self.assertEqual(条["渲染哈希"], 条["第二次哈希"])

    def test_贪吃蛇同种子两次相同(self):
        """⚠ 这只覆盖"同种子 ⇒ 同结果"。**反面**（异种子）见下一条 —— 那条特意记录了
        "异种子也相同"是**设计**（`吃食物` 每次吃到会重新播种），不是假绿。"""
        条 = 落盘指纹()["内容"]["确定性_贪吃蛇同种子两遍"]
        self.assertEqual(条["同种子两次相同"], "真")
        self.assertEqual(条["渲染哈希"], 条["同种子第二次哈希"])

    def test_贪吃蛇异种子也相同是设计如此(self):
        """⚠⚠ 这条守的是一个**很容易被误当成 bug「修」掉**的行为。

        `snake.py` 的 `吃食物.每帧` 里有 `self.随机源.seed(self.吃到 * 7919)`
        —— **每次吃到都重新播种**，构造时的 `种子` 对食物位置**无效**。
        ⇒ 异种子跑出来**一模一样**。C++ 版必须**照样复现**，不许"顺手修好"。
        """
        条 = 落盘指纹()["内容"]["确定性_贪吃蛇同种子两遍"]
        self.assertEqual(
            条["⚠ 异种子也相同（设计如此，不是假绿）"], "真",
            "异种子行为变了 —— 若是有意让它依赖种子，那是**行为变更**，"
            "要同步更新指纹与 C++ 契约；若是无意，那是缺陷")

    def test_随机路径的输出被逐位钉死(self):
        """⚠ 这条才是"随机源真被走到"的正面证据（种子那个方向证不了）。

        强制连吃 3 次，每次食物的新坐标必须与记录**逐位相同**。
        路径没走到 ⇒ 坐标不变 ⇒ 这里必红。
        """
        帧们 = 落盘指纹()["内容"]["确定性_吃食物重定位"]["帧"]
        序 = [f["状态"]["吃到次数"][0] for f in 帧们]
        self.assertEqual(序, ["1", "1", "2", "2", "3", "3"],
                         "吃到次数的序列变了 ⇒ 随机路径的触发点变了")
        分 = [f["状态"]["分数"][0] for f in 帧们]
        self.assertEqual(分, ["10", "10", "20", "20", "30", "30"])
        #: 三次吃到的食物坐标必须互不相同（否则"重定位"根本没发生）
        坐标 = {tuple(f["状态"]["食物位置"]) for f in 帧们}
        self.assertEqual(len(坐标), 3,
                         "三次吃到的食物坐标有重复 ⇒ 重定位可能没真的发生")

    def test_Tk裁剪边界被逐点钉死(self):
        """⚠ 裁剪规则只钉"安全点"是钉不住的：必须钉在 **MARGIN 的边界上**。

        实测：MARGIN=10 虚拟单位、宽 200/size 100 ⇒ 换算比 2 ⇒ 合法屏幕区间
        **[-20, 220]**。所以 -20 真、-20.5 假、220 真、220.5 假。
        """
        验 = 落盘指纹()
        tk = 验["内容"]["Tk后端_提交item数"]["Tk"]
        if not tk.get("可用"):
            self.skipTest("生成指纹时 Tk 不可用")
        格 = {(float.fromhex(r[0]), float.fromhex(r[1])): r[2]
              for r in tk["在视野内网格"]}
        for sx in (-20.0, 220.0):
            self.assertEqual(格[(sx, 0.0)], "真", "sx=%s 应当在视野内" % sx)
        for sx in (-20.5, 220.5):
            self.assertEqual(格[(sx, 0.0)], "假", "sx=%s 应当被裁掉" % sx)
        #: 两轴必须**同时**在区间内
        self.assertEqual(格[(100.0, 221.0)], "假", "y 出界没被裁")
        self.assertEqual(格[(221.0, 0.0)], "假", "x 出界没被裁")
        #: 一条轴出界就整体为假（不是"任一轴在范围内就算"）
        self.assertEqual(格[(100.0, 220.0)], "真")
        self.assertEqual(格[(220.0, 0.0)], "真")

    def test_录制后端不裁剪_Tk后端裁剪(self):
        """两个后端的裁剪开关是**契约的一部分**（判据靠录制后端数画了几个圆）。"""
        内容 = 落盘指纹()["内容"]
        值 = 内容["裁剪_后端属性"]["值"]
        self.assertEqual(值["录制后端.允许裁剪"], "假")
        self.assertEqual(值["Tk后端.允许裁剪"], "真")
        self.assertEqual(内容["裁剪_录制后端不裁剪"]["实测每帧图元数"], [11, 11])

    def test_十五个原生组件都有覆盖(self):
        """⚠ 任务书要求"15 个组件每一个都单独跑一遍" —— 这条防止悄悄漏掉。"""
        名 = set(落盘指纹()["内容"])
        应有 = ["圆点", "方块", "标签", "线框",          # 视觉 4
                "匀速移动", "按键驱动", "重力",           # 运动 3
                "跟随相机", "世界边界",                   # 相机 2
                "计时器", "分数", "生命周期", "圆形碰撞", "闪烁", "尾迹"]  # 玩法 6
        self.assertEqual(len(应有), 15)
        缺 = [c for c in 应有 if ("组件_" + c) not in 名]
        self.assertEqual(缺, [], "这些原生组件没有单独跑过：%s" % 缺)


# ═══════════════════════ ③ ⚠ 尺子本身要有刻度（反面验证）═══════════════

class C判据必须抓得到差异(unittest.TestCase):
    """⚠⚠ **这一组比上面两组都重要。**

    如果"改坏了抓不到"，那上面的全绿**什么都不能证明** ——
    而且比没有判据更危险，因为它给人安全感。
    （本仓的两次假绿事故都是这个形状。）
    """

    def test_改一个坐标的末位必须被抓到(self):
        """⚠ 浮点末位差一位 —— 这正是 C++ 移植最可能出、人眼最看不出的错。"""
        落 = 落盘指纹()
        坏 = copy.deepcopy(落)
        条 = 坏["内容"]["组件_圆点"]
        原 = 条["帧"][0]["图元"][1][1]          # 第 0 帧第 1 条图元的 x
        self.assertIsInstance(原, str)
        #: 把最后一位十六进制数字改掉（保证真的变了）
        末 = 原[-1]
        新 = (末 != "0") and "0" or "1"
        条["帧"][0]["图元"][1][1] = 原[:-1] + 新
        self.assertNotEqual(条["帧"][0]["图元"][1][1], 原)
        问题 = F.比对(坏, 落)
        self.assertTrue(问题, "坐标末位改了却**没抓到** ⇒ 判据是假绿")
        self.assertIn("组件_圆点", " ".join(问题))

    def test_删掉一个场景必须被抓到(self):
        坏 = copy.deepcopy(落盘指纹())
        坏["内容"].pop("组件_重力")
        问题 = F.比对(坏, 落盘指纹())
        self.assertTrue(问题, "删了一个场景却没抓到 ⇒ 判据是假绿")
        self.assertTrue(any("组件_重力" in p for p in 问题), 问题)

    def test_调换两个图元的顺序必须被抓到(self):
        """⚠ 顺序也是契约（C++ 侧遍历顺序不同 ⇒ 图元顺序就不同）。"""
        落 = 落盘指纹()
        坏 = copy.deepcopy(落)
        帧 = 坏["内容"]["组件_线框"]["帧"][0]["图元"]
        self.assertGreaterEqual(len(帧), 3, "这个场景的图元太少，换不出顺序")
        帧[1], 帧[2] = 帧[2], 帧[1]
        问题 = F.比对(坏, 落)
        self.assertTrue(问题, "图元顺序换了却没抓到 ⇒ 判据是假绿")

    def test_删掉一帧必须被抓到(self):
        坏 = copy.deepcopy(落盘指纹())
        坏["内容"]["组件_匀速移动"]["帧"].pop(3)
        问题 = F.比对(坏, 落盘指纹())
        self.assertTrue(问题, "少了一帧却没抓到 ⇒ 判据是假绿")

    # ── ⚠⚠ 空指纹 / 没跑到，必须是"失败"，不是"过了" ──────────────────
    def test_空指纹必须判没跑到(self):
        """⚠⚠ 这条直接对应本仓的两次假绿事故。

        "场景数 0 / 图元总数 0" 必须**返回非空问题清单**，
        否则一个什么都没跑到的脚本会**报绿**。
        """
        空 = {"头": {}, "统计": {"场景数": 0, "图元总数": 0}, "内容": {}}
        问题 = F.自检(空)
        self.assertTrue(问题, "空指纹竟然判「过了」⇒ 这就是假绿")
        self.assertTrue(any("没跑到" in p for p in 问题), 问题)

    def test_图元总数清零必须判没跑到(self):
        """场景都在、但**一条图元都没画出来**（渲染静默失效）也要判没跑到。"""
        坏 = copy.deepcopy(落盘指纹())
        坏["统计"]["图元总数"] = 0
        for v in 坏["内容"].values():
            if isinstance(v, dict) and v.get("应有图元"):
                v["图元数"] = 0
        问题 = F.自检(坏)
        self.assertTrue(问题, "图元总数清零竟然判「过了」⇒ 这就是假绿")

    def test_单个场景没画出东西也要判没跑到(self):
        """⚠ 大数不为零**掩盖不了**单个场景空掉 —— 这正是"全串计数"那个坑的形状。"""
        坏 = copy.deepcopy(落盘指纹())
        坏["内容"]["组件_圆点"]["图元数"] = 0
        问题 = F.自检(坏)
        self.assertTrue(问题, "单个场景空了却没抓到 ⇒ 又是「总量对了就放过」的假绿")
        self.assertTrue(any("组件_圆点" in p for p in 问题), 问题)

    def test_每帧图元数不符也要判没跑到(self):
        坏 = copy.deepcopy(落盘指纹())
        坏["内容"]["边界_空场景"]["实测每帧图元数"] = [1, 0, 1]
        问题 = F.自检(坏)
        self.assertTrue(问题, "空场景某一帧没画出清屏却没抓到")

    def test_没写统计一节也要判没跑到(self):
        """⚠ "没有统计"和"统计是空的"必须都不算通过 —— 否则删掉统计就能骗过判据。"""
        问题 = F.自检({"头": {}, "内容": {"x": {"应有图元": True, "图元数": 5}}})
        self.assertTrue(问题, "没有 `统计` 一节却判过了 ⇒ 删掉统计就能骗过判据")


if __name__ == "__main__":
    unittest.main(verbosity=2)
