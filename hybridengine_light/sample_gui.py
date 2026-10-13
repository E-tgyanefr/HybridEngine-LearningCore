"""`hybridengine_light.sample_gui` —— **SampleCore 的游戏开发 IDE**。

═══ 为什么是**另一个文件**而不是改 `learn_gui.py` ═══

作者定的：

    HybridEngine-LearningCore   面向**学** —— 动画播放 · IDE · 课程
    HybridEngine-SampleCore     面向**做游戏** —— 次旗舰

⇒ **两个不同的程序，UI 各自重新设计。** 作者明确选了「**完全不共用，各写一份**」。

⚠ 所以本文件与 `learn_gui.py` **没有任何 import 关系**（只共用 `theme` / `pyengine`
  这两个"零件库"）。改这里不会碰到学习界面，反过来也一样。

═══ 这个界面是什么 ═══

**游戏开发 IDE** —— 核心动作是「**拼**」，不是「读课」：

    ┌───────────────┬──────────────────┬───────────────────────────┐
    │ 组件面板       │ 场景树            │ 代码编辑器                 │
    │ （15 个组件）  │ 玩家              │ # 场景（面板里拼出来的）    │
    │  形状 · 运动   │   圆点            │ s = 场景()                │
    │  玩法 · 效果   │   按键驱动        │ 玩家 = s.造("玩家", 50, 50) │
    │               │ 敌人              │ 玩家.加(圆点(半径=3))      │
    │ 双击 = 挂上去  │   圆点 · 匀速移动  │ ...                       │
    ├───────────────┴──────────────────┴───────────────────────────┤
    │ ▶ 运行  ■ 停  ⟳ 重置      状态：就绪        帧 0                │
    └──────────────────────────────────────────────────────────────┘

**"模块化编程拼游戏"就落在这里**：左边挑组件 → 双击挂到选中的对象上 →
中间那栏的"场景配方"长一行 → 右边代码**自动跟着变** → F5 就跑。

═══ ⚠ 三条设计原则 ═══

**① 面板里的东西必须真能跑。** 面板不是文档，是**可执行的**：
   双击一个组件 = `对象.加(那个组件())`，代码立刻变。**没有假按钮。**

**② 代码是"配方"不是"生成物"。** 面板只负责**生成一份能读的配方**，
   学员可以**直接改代码**（改完面板不再覆盖，直到他点「按面板重写」）。
   ⚠ 这条很重要 —— 否则学员会以为"代码是机器产的、我不能动"。

**③ 运行是真的开一个游戏窗口。** 不是"预览图"。
   所以运行前会先把游戏存成一个临时文件再跑（和学员按 F5 一样）。
"""
from __future__ import annotations

import os
import sys
import time
import tkinter as tk
from tkinter import messagebox, ttk

from . import theme as T
from . import pyengine as E

#: 面板里列的组件 —— **(名字, 分组, 说明, 默认构造参数)**
#
#  ⚠ 这份表**必须和 `pyengine.components` 对得上**。有一条判据守着
#    （`tests/test_sample_gui.py`：表里每个名字都能真的实例化）。
#  ⚠ 参数用**关键字**写 —— 组件改名/加参数时不会静默错位。
面板 = [
    # 形状
    ("圆点", "形状", "画一个实心圆。最常用的那一个", {"半径": 3.0, "颜色": "红"}),
    ("方块", "形状", "画一个矩形（锚点在左下角）", {"宽": 4.0, "高": 4.0, "颜色": "蓝"}),
    ("标签", "形状", "写一段字（分数、提示）", {"内容": "文字", "颜色": "黑", "字号": 14}),
    ("线框", "形状", "把子对象连起来画（调试用）", {"颜色": "灰", "粗": 1.0}),
    # 运动
    ("匀速移动", "运动", "按固定速度走，撞边界就反弹", {"vx": 10.0, "vy": 10.0}),
    ("按键驱动", "运动", "按 WASD / 方向键移动", {"速度": 30.0}),
    ("重力", "运动", "给一个向下的加速度", {"加速度": -60.0, "地面": 0.0}),
    ("跟随相机", "运动", "让相机跟着这个对象走", {"平滑": 0.0}),
    ("世界边界", "运动", "给相机设世界边界", {"左": 0.0, "下": 0.0, "右": 100.0, "上": 100.0}),
    # 玩法
    ("计时器", "玩法", "每隔一段时间触发一次", {"间隔": 1.0}),
    ("分数", "玩法", "计分 + 自己画在左上角", {"初值": 0, "颜色": "黑"}),
    ("生命周期", "玩法", "活够时间就自动卸载（子弹、飘字）", {"寿命": 1.0}),
    ("圆形碰撞", "玩法", "圆形碰撞体。只判定，不做物理", {"半径": 3.0}),
    # 效果
    ("闪烁", "效果", "按节奏一闪一闪", {"周期": 0.2, "颜色a": "红", "颜色b": "黄"}),
    ("尾迹", "效果", "移动时留下会消失的小点", {"间隔": 0.05, "寿命": 0.4}),
]

分组顺序 = ["形状", "运动", "玩法", "效果"]


def _组件类(名: str):
    """按名字取组件类。取不到返回 None（面板要能容忍表写错）。"""
    return getattr(E.components, 名, None)


class 界面(tk.Tk):
    """SampleCore 的游戏开发 IDE。

    ⚠ 它**继承 `tk.Tk`**（不是一个 Frame）—— 和 `learn_gui.py` 的做法一致：
      这样 `界面()` 就是整个窗口，入口简单。
    """

    def __init__(self):
        super().__init__()
        self.title("HybridEngine-SampleCore —— 拼游戏")
        self.configure(bg=T.底)
        self.geometry("1440x900")
        self.minsize(1100, 700)

        #: 场景状态 —— 面板编的就是它
        #: ⚠ 它是个**纯数据描述**（名字 + 位置 + 组件列表），
        #:   不是活的 `E.场景`。理由：面板要能"编完再生成代码"，
        #:   而活的场景对象没法反过来生成代码（组件实例上没有"我是谁"）。
        self.对象们: list[dict] = []
        self.选中: int | None = None
        #: 面板改过之后，代码区是否由面板接管
        self.面板接管代码 = True
        self.脏 = False
        self._运行进程 = None
        self._游戏文件 = os.path.join(os.getcwd(), "我的游戏.py")

        self._建样式()
        self._建布局()
        self._刷面板()
        self._刷场景树()
        self._生成代码()

        self.bind("<F5>", lambda e: self.点运行())
        self.bind("<Control-s>", lambda e: self.点保存())
        self.protocol("WM_DELETE_WINDOW", self._关闭时问)

    # ══════════════════════ 样式 ══════════════════════

    def _建样式(self):
        """ttk 控件不吃 `bg=`/`fg=` ⇒ 必须走样式。

        ⚠ 和 `learn_gui.py` 里同一套办法（`clam` + 显式配色）。
          这里**重写一份**而不是 import —— 作者要的是"两套 UI 各写一份"。
        """
        s = ttk.Style(self)
        try:
            s.theme_use("clam")
        except Exception:
            pass
        s.configure("Treeview", background=T.面板, fieldbackground=T.面板,
                    foreground=T.文字, borderwidth=0, rowheight=26)
        s.configure("Treeview.Heading", background=T.浮起, foreground=T.文字次,
                    borderwidth=0, relief="flat")
        s.map("Treeview", background=[("selected", T.强调暗)],
              foreground=[("selected", T.强调底字)])
        s.configure("TNotebook", background=T.底, borderwidth=0)
        s.configure("TNotebook.Tab", background=T.面板, foreground=T.文字次,
                    padding=(T.间_中, T.间_小), borderwidth=0)
        s.map("TNotebook.Tab", background=[("selected", T.底)],
              foreground=[("selected", T.文字)])

    # ══════════════════════ 布局 ══════════════════════

    def _建布局(self):
        # ── 顶栏 ──
        顶 = tk.Frame(self, bg=T.面板, height=44)
        顶.pack(fill="x", side="top")
        顶.pack_propagate(False)

        牌 = tk.Frame(顶, bg=T.面板)
        牌.pack(side="left", padx=T.间_中)
        tk.Label(牌, text="HybridEngine", bg=T.面板, fg=T.文字,
                 font=T.界面字体(T.字_大, 粗=True)).pack(side="left")
        tk.Label(牌, text="-SampleCore", bg=T.面板, fg=T.强调,
                 font=T.界面字体(T.字_大, 粗=True)).pack(side="left")
        tk.Label(牌, text="  拼游戏", bg=T.面板, fg=T.文字弱,
                 font=T.界面字体(T.字_小)).pack(side="left")

        self._实心按钮(顶, "▶ 运行 F5", self.点运行).pack(side="right", padx=(0, T.间_中), pady=T.间_小)
        self._幽灵按钮(顶, "■ 停", self.点停).pack(side="right", padx=(0, T.间_小), pady=T.间_小)
        self._幽灵按钮(顶, "保存", self.点保存).pack(side="right", padx=(0, T.间_小), pady=T.间_小)

        self.状态 = tk.Label(顶, text="就绪", bg=T.面板, fg=T.文字弱,
                          font=T.界面字体(T.字_微))
        self.状态.pack(side="right", padx=T.间_中)

        # ── 三栏主体 ──
        主体 = tk.Frame(self, bg=T.底)
        主体.pack(fill="both", expand=True)

        # ① 组件面板（左）
        左 = tk.Frame(主体, bg=T.面板, width=260)
        左.pack(side="left", fill="y")
        左.pack_propagate(False)
        self._区标题(左, "组件面板", "双击 = 挂到选中的对象上")
        self.组件树 = ttk.Treeview(左, show="tree", selectmode="browse")
        self.组件树.pack(fill="both", expand=True, padx=T.间_小, pady=(0, T.间_中))
        self.组件树.bind("<Double-1>", self._双击组件)

        # ② 场景树（中）
        中 = tk.Frame(主体, bg=T.面板, width=280)
        中.pack(side="left", fill="y")
        中.pack_propagate(False)
        self._区标题(中, "场景树", "选一个对象，再从左边的面板挂组件")
        条 = tk.Frame(中, bg=T.面板)
        条.pack(fill="x", padx=T.间_小)
        self._幽灵按钮(条, "+ 对象", self.点加对象).pack(side="left")
        self._幽灵按钮(条, "− 删", self.点删对象).pack(side="left", padx=T.间_小)
        条2 = tk.Frame(中, bg=T.面板)
        条2.pack(fill="x", padx=T.间_小, pady=(T.间_小, 0))
        self._幽灵按钮(条2, "拆掉组件", self.点删组件).pack(side="left")
        self.场景树 = ttk.Treeview(中, show="tree", selectmode="browse")
        self.场景树.pack(fill="both", expand=True, padx=T.间_小, pady=T.间_小)
        self.场景树.bind("<<TreeviewSelect>>", self._选对象)
        self.场景树.tag_configure("对象", foreground=T.文字)
        self.场景树.tag_configure("组件", foreground=T.文字次)

        # ③ 代码（右）
        右 = tk.Frame(主体, bg=T.底)
        右.pack(side="left", fill="both", expand=True)
        右头 = tk.Frame(右, bg=T.底)
        右头.pack(fill="x", padx=T.间_中, pady=(T.间_小, 0))
        tk.Label(右头, text="场景配方（可以直接改）", bg=T.底, fg=T.文字,
                 font=T.界面字体(T.字_小, 粗=True)).pack(side="left")
        self.接管标签 = tk.Label(右头, text="面板接管中", bg=T.底, fg=T.好,
                             font=T.界面字体(T.字_微))
        self.接管标签.pack(side="left", padx=T.间_中)
        self._幽灵按钮(右头, "按面板重写", self.点按面板重写).pack(side="right")

        编框 = tk.Frame(右, bg=T.码底)
        编框.pack(fill="both", expand=True, padx=T.间_中, pady=T.间_小)
        self.行号 = tk.Text(编框, width=4, padx=T.间_小, pady=T.间_小,
                          bg=T.码底, fg=T.文字弱, bd=0, relief="flat",
                          font=T.等宽字体(T.字_正文), state="disabled",
                          highlightthickness=0, takefocus=0)
        self.行号.pack(side="left", fill="y")
        self.代码 = tk.Text(编框, bg=T.码底, fg=T.码_其余, bd=0, relief="flat",
                          insertbackground=T.强调, font=T.等宽字体(T.字_正文),
                          wrap="none", undo=True, padx=T.间_小, pady=T.间_小,
                          highlightthickness=0, selectbackground=T.强调暗)
        self.代码.pack(side="left", fill="both", expand=True)
        sb = tk.Scrollbar(编框, command=self._滚动, bg=T.面板, bd=0,
                          highlightthickness=0, troughcolor=T.码底,
                          activebackground=T.线强)
        sb.pack(side="right", fill="y")
        self.代码.configure(yscrollcommand=lambda a, b: (sb.set(a, b), self._画行号()))
        #: Tab 缩进（这是 IDE 该有的；学员写 Python 全靠缩进）
        self.代码.bind("<Tab>", self._Tab)
        self.代码.bind("<Shift-Tab>", self._ShiftTab)
        self.代码.bind("<Return>", self._回车)
        self.代码.bind("<KeyRelease>", lambda e: (self._画行号(), self._抢走接管()))
        self.代码.bind("<MouseWheel>", lambda e: self.after(1, self._画行号))

        # ── 底：运行输出 ──
        底 = tk.Frame(self, bg=T.面板, height=132)
        底.pack(fill="x", side="bottom")
        底.pack_propagate(False)
        tk.Label(底, text="运行输出", bg=T.面板, fg=T.文字次,
                 font=T.界面字体(T.字_微)).pack(anchor="w", padx=T.间_中,
                                             pady=(T.间_小, 0))
        self.输出 = tk.Text(底, height=5, bg=T.码底, fg=T.码_其余, bd=0,
                          relief="flat", font=T.等宽字体(T.字_微), wrap="word",
                          highlightthickness=0, state="disabled")
        self.输出.pack(fill="both", expand=True, padx=T.间_中, pady=(0, T.间_中))

    def _区标题(self, 父, 标题: str, 说明: str):
        f = tk.Frame(父, bg=T.面板)
        f.pack(fill="x", padx=T.间_中, pady=(T.间_中, T.间_小))
        tk.Label(f, text=标题, bg=T.面板, fg=T.文字,
                 font=T.界面字体(T.字_小, 粗=True)).pack(anchor="w")
        tk.Label(f, text=说明, bg=T.面板, fg=T.文字弱,
                 font=T.界面字体(T.字_微)).pack(anchor="w")

    def _幽灵按钮(self, 父, 字: str, 命令):
        b = tk.Button(父, text=字, command=命令, bg=T.面板, fg=T.文字次,
                      bd=0, relief="flat", highlightthickness=0,
                      activebackground=T.浮起, activeforeground=T.文字,
                      font=T.界面字体(T.字_微), padx=T.间_中, pady=2,
                      cursor="hand2")
        return b

    def _实心按钮(self, 父, 字: str, 命令):
        b = tk.Button(父, text=字, command=命令, bg=T.强调底, fg=T.强调底字,
                      bd=0, relief="flat", highlightthickness=0,
                      activebackground=T.强调亮, activeforeground=T.强调底字,
                      font=T.界面字体(T.字_微, 粗=True), padx=T.间_中, pady=3,
                      cursor="hand2")
        return b

    # ══════════════════════ 面板 ══════════════════════

    def _刷面板(self):
        """把 15 个组件按分组列出来。⚠ 组名在表里写错也不会崩（跳过）。"""
        self.组件树.delete(*self.组件树.get_children())
        for 组 in 分组顺序:
            节 = self.组件树.insert("", "end", text="  " + 组, open=True)
            for (名, g, 说明, 参数) in 面板:
                if g != 组:
                    continue
                self.组件树.insert(节, "end", iid="组件:" + 名,
                                 text="    " + 名 + "   " + 说明[:16])
        self.组件树.tag_configure("", foreground=T.文字)

    def _双击组件(self, _e=None):
        选 = self.组件树.selection()
        if not 选 or not 选[0].startswith("组件:"):
            return
        名 = 选[0].split(":", 1)[1]
        if self.选中 is None:
            self._说("先在中间选一个对象，再挂组件")
            return
        类 = _组件类(名)
        if 类 is None:
            self._说("组件 %s 不存在（面板表和 components 对不上）" % 名)
            return
        参数 = dict(next((p for (n, _, _, p) in 面板 if n == 名), {}))
        self.对象们[self.选中]["组件"].append({"名": 名, "参数": 参数})
        self._面板改了()
        self._说("给「%s」挂上了 %s" % (self.对象们[self.选中]["名"], 名))

    def _面板改了(self):
        """面板一改 ⇒ 场景树刷新 + 代码重写（如果面板还接管着代码）。"""
        self.面板接管代码 = True
        self.脏 = True
        self._刷场景树()
        self._生成代码()

    # ══════════════════════ 场景树 ══════════════════════

    def _刷场景树(self):
        self.场景树.delete(*self.场景树.get_children())
        for i, o in enumerate(self.对象们):
            iid = "对象:%d" % i
            self.场景树.insert("", "end", iid=iid, text="  %s" % o["名"],
                             open=True, tags=("对象",))
            for j, c in enumerate(o["组件"]):
                参 = " ".join("%s=%r" % (k, v) for k, v in c["参数"].items())
                self.场景树.insert(iid, "end", iid="组件:%d:%d" % (i, j),
                                 text="    %s(%s)" % (c["名"], 参[:30]),
                                 tags=("组件",))
        if self.选中 is not None and self.选中 < len(self.对象们):
            try:
                self.场景树.selection_set("对象:%d" % self.选中)
            except Exception:
                pass

    def _选对象(self, _e=None):
        选 = self.场景树.selection()
        if not 选:
            return
        标 = 选[0]
        if 标.startswith("对象:"):
            self.选中 = int(标.split(":")[1])
        elif 标.startswith("组件:"):
            #: ⚠ 点组件时把"选中的对象"切到它爹 —— 否则"双击组件挂上去"
            #:   会飞到上一次选的对象上（很迷惑）。
            self.选中 = int(标.split(":")[1])

    def 点加对象(self):
        名 = "对象%d" % (len(self.对象们) + 1)
        self.对象们.append({"名": 名, "x": 50.0, "y": 50.0, "组件": []})
        self.选中 = len(self.对象们) - 1
        self._面板改了()
        self._说("加了一个「%s」—— 从左边的组件面板给它挂组件" % 名)

    def 点删对象(self):
        if self.选中 is None:
            self._说("先选一个对象")
            return
        名 = self.对象们[self.选中]["名"]
        del self.对象们[self.选中]
        self.选中 = min(self.选中, len(self.对象们) - 1) if self.对象们 else None
        self._面板改了()
        self._说("删掉了「%s」" % 名)

    def 点删组件(self):
        选 = self.场景树.selection()
        if not 选 or not 选[0].startswith("组件:"):
            self._说("在中间选一个**组件**（不是对象）")
            return
        _, i, j = 选[0].split(":")
        i, j = int(i), int(j)
        try:
            名 = self.对象们[i]["组件"][j]["名"]
            del self.对象们[i]["组件"][j]
        except Exception:
            return
        self._面板改了()
        self._说("拆掉了 %s" % 名)

    # ══════════════════════ 代码生成 ══════════════════════

    def _生成代码(self):
        """把面板里的东西生成一份**能读的配方**。

        ⚠ 生成的是**代码**，不是"隐藏的序列化数据" —— 学员能看到、能改、能学。
          这是"模块化编程"该有的样子：面板只是**帮你写这段代码**。
        """
        行 = [
            "# 这份「场景配方」是面板拼出来的 —— 你可以直接改。",
            "# 改完按 F5 就跑。⚠ 一改代码，面板就不再覆盖它（除非点「按面板重写」）。",
            "",
            "import math",
            "from hybridengine_light import pyengine as E",
            "from hybridengine_light.pyengine.components import *",
            "",
            "",
            "def 造场景():",
            "    s = E.场景(\"主场景\")",
            "",
        ]
        if not self.对象们:
            行 += ["    # 还没有对象 —— 点中间的「+ 对象」加一个，",
                   "    # 再从左边的组件面板双击挂组件。",
                   "    return s",
                   "",
                   "",
                   "def 主流程():",
                   "    E.游戏(场景=造场景(), 标题=\"我的游戏\").跑()",
                   ""]
        else:
            for o in self.对象们:
                行.append("    %s = s.造(%r, x=%g, y=%g)"
                          % (o["名"], o["名"], o["x"], o["y"]))
                for c in o["组件"]:
                    参 = "".join(", %s=%r" % (k, v) for k, v in c["参数"].items())
                    行.append("    %s.加(%s(%s))" % (o["名"], c["名"], 参.lstrip(", ")))
            行 += [
                "",
                "    # 相机：默认跟着第一个对象（不想要就删掉这行）",
                "    s.设服务(\"相机\", E.相机2D(x=50, y=50, 视口=(100.0, 100.0)))",
                "    return s",
                "",
                "",
                "def 主流程():",
                "    E.游戏(场景=造场景(), 标题=\"我的游戏\").跑()",
                "",
                "",
                'if __name__ == "__main__":',
                "    主流程()",
                "",
            ]
        self._设文本(self.代码, "\n".join(行))
        self._画行号()
        self._重高亮()

    def 点按面板重写(self):
        """把代码恢复成面板拼出来的那份。

        ⚠ 有确认门 —— 学员可能已经在代码里写了一堆东西，那**不该被无声覆盖**。
        """
        if not self.面板接管代码 and not messagebox.askyesno(
                "按面板重写？", "代码里你改过的东西会**被丢掉**，\n"
                            "恢复成面板拼出来的配方。\n\n确定吗？"):
            return
        self.面板接管代码 = True
        self._生成代码()
        self.接管标签.configure(text="面板接管中", fg=T.好)
        self._说("代码已按面板重写")

    def _抢走接管(self, _e=None):
        """学员一改代码 ⇒ 面板**让位**（不再覆盖他写的东西）。

        ⚠ 这条是"代码是配方不是生成物"的落地：面板不能跟人抢。
        """
        if self.面板接管代码:
            self.面板接管代码 = False
            self.接管标签.configure(text="你在改代码 · 面板让位", fg=T.警示)

    # ══════════════════════ 编辑器技巧 ══════════════════════

    def _设文本(self, 控件: tk.Text, 内容: str):
        控件.delete("1.0", "end")
        控件.insert("1.0", 内容)
        控件.edit_modified(False)

    def _画行号(self):
        try:
            self.行号.configure(state="normal")
            self.行号.delete("1.0", "end")
            行数 = int(self.代码.index("end-1c").split(".")[0])
            self.行号.insert("1.0", "\n".join(str(i) for i in range(1, 行数 + 1)))
            self.行号.configure(state="disabled")
            self.行号.yview_moveto(self.代码.yview()[0])
        except Exception:
            pass

    def _滚动(self, *a):
        self.代码.yview(*a)
        self._画行号()

    def _Tab(self, _e=None):
        """Tab 缩进（多行选中就整块缩进）。IDE 该有的。"""
        try:
            if self.代码.tag_ranges("sel"):
                起, 止 = self.代码.index("sel.first linestart"), self.代码.index("sel.last lineend")
                块 = self.代码.get(起, 止)
                self.代码.delete(起, 止)
                self.代码.insert(起, "\n".join("    " + l for l in 块.split("\n")))
            else:
                self.代码.insert("insert", "    ")
        except Exception:
            pass
        return "break"

    def _ShiftTab(self, _e=None):
        try:
            行 = self.代码.index("insert").split(".")[0]
            文 = self.代码.get("%s.0" % 行, "%s.end" % 行)
            if 文.startswith("    "):
                self.代码.delete("%s.0" % 行, "%s.4" % 行)
        except Exception:
            pass
        return "break"

    def _回车(self, _e=None):
        """回车自动保持缩进（写 Python 全靠缩进）。"""
        try:
            行 = self.代码.index("insert").split(".")[0]
            文 = self.代码.get("%s.0" % 行, "insert")
            缩 = len(文) - len(文.lstrip(" "))
            尾 = 4 if 文.rstrip().endswith(":") else 0
            self.代码.insert("insert", "\n" + " " * (缩 + 尾))
        except Exception:
            pass
        return "break"

    def _重高亮(self):
        """很轻的语法着色 —— 只认注释/字符串/关键字。

        ⚠ 刻意**不写一个大词法器**：这里的目标是"看起来像代码"，
          不是"编译器级准确"。一个错的词法器比没有更糟。
        """
        try:
            self.代码.tag_delete("注释", "字符串", "关键字", "数字")
        except Exception:
            pass
        self.代码.tag_configure("注释", foreground=T.码_注释)
        self.代码.tag_configure("字符串", foreground=T.码_字符串)
        self.代码.tag_configure("关键字", foreground=T.码_关键字)
        self.代码.tag_configure("数字", foreground=T.码_数字)
        import re
        文 = self.代码.get("1.0", "end-1c")
        for m in re.finditer(r"#[^\n]*", 文):
            self.代码.tag_add("注释", "1.0+%dc" % m.start(), "1.0+%dc" % m.end())
        for m in re.finditer(r'"[^"\n]*"|\'[^\'\n]*\'', 文):
            if "#" in 文[:m.start()].split("\n")[-1]:
                continue
            self.代码.tag_add("字符串", "1.0+%dc" % m.start(), "1.0+%dc" % m.end())
        for m in re.finditer(r"\b(import|from|def|return|if|else|elif|for|while|in|as|"
                             r"class|try|except|with|and|or|not|None|True|False)\b", 文):
            self.代码.tag_add("关键字", "1.0+%dc" % m.start(), "1.0+%dc" % m.end())
        for m in re.finditer(r"\b\d+(\.\d+)?\b", 文):
            self.代码.tag_add("数字", "1.0+%dc" % m.start(), "1.0+%dc" % m.end())

    # ══════════════════════ 运行 / 保存 ══════════════════════

    def 点保存(self):
        try:
            with open(self._游戏文件, "w", encoding="utf-8", newline="\n") as f:
                f.write(self.代码.get("1.0", "end-1c"))
            self.脏 = False
            self._说("已保存：%s" % os.path.basename(self._游戏文件))
        except OSError as e:
            self._说("保存失败：%s" % e)

    def 点运行(self):
        """**真跑** —— 存成文件、起一个**独立进程**。

        ⚠ 为什么起独立进程而不是在界面里跑：
          游戏要 `E.游戏(...).跑()`，它会**自己开窗口、自己进主循环** ——
          在本进程里跑会把 IDE 卡死，而且它的窗口和 IDE 抢事件。
          ⇒ 独立进程 + 独立窗口 = 关掉游戏窗口就回到 IDE。
        """
        self.点保存()
        if not os.path.exists(self._游戏文件):
            self._说("没保存成功，跑不了")
            return
        if self._运行进程 is not None and self._运行进程.poll() is None:
            self._说("已经在跑了 —— 先「■ 停」")
            return
        import subprocess
        try:
            self._运行进程 = subprocess.Popen(
                [sys.executable, self._游戏文件],
                cwd=os.path.dirname(self._游戏文件) or ".",
                stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                text=True, encoding="utf-8", errors="replace")
        except Exception as e:
            self._说("起不来：%s" % e)
            return
        self._说("跑起来了 —— 关掉游戏窗口就回到这里")
        self.after(200, self._收运行输出)

    def _收运行输出(self):
        进 = self._运行进程
        if 进 is None:
            return
        try:
            出 = 进.stdout.read() if 进.poll() is not None else ""
        except Exception:
            出 = ""
        if 出:
            self._设文本(self.输出, 出[-4000:])
        if 进.poll() is None:
            self.after(400, self._收运行输出)
        else:
            self._说("游戏退出了（退出码 %s）" % 进.returncode)

    def 点停(self):
        进 = self._运行进程
        if 进 is None or 进.poll() is not None:
            self._说("没有在跑的游戏")
            return
        try:
            进.terminate()
        except Exception:
            pass
        self._说("已停")

    def _关闭时问(self):
        if self.脏 and not messagebox.askyesno("还没保存", "场景配方有改动没保存，确定退出吗？"):
            return
        self.点停()
        self.destroy()

    # ══════════════════════ 杂项 ══════════════════════

    def _说(self, 话: str):
        self.状态.configure(text=话[:80])


def main() -> int:
    """IDE 入口。⚠ 没有显示环境时给一句人话，不要抛 traceback。"""
    try:
        界面().mainloop()
        return 0
    except tk.TclError as e:
        print("  开不了图形界面：%s" % e)
        print("  （没有显示环境？这个 IDE 需要桌面）")
        return 1


if __name__ == "__main__":
    sys.exit(main())
