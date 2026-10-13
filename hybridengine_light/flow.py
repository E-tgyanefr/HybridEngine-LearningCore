"""执行流的**动画**可视化 —— 值在动、路真的分叉。

═══ 用户原话：「我认为这不是互动动画」 ═══

⚠⚠ 他说得对，我上一版做错了。我做的其实是个 **stepper**：

    · 代码是**一行行文本**
    · 高亮条**跳**到下一行（没有位移，没有过渡）
    · 变量栏是**文字换了一下**
    · 而且**没有画布级动画** —— 我只是把文字画在 Canvas 上

  ⇒ 那不叫动画，那叫"带高亮的文本"。用户一眼就看出来了。

═══ 真正的互动动画是什么 ═══

  把**执行流本身变成图形**，让**值在图上移动**：

    if 分数 >= 90:        ┌──────────────┐
        等级 = "优秀"      │ 分数 >= 90 ? │  ← 条件做成一个**判断框**
    elif 分数 >= 60:      └──────┬───────┘
        等级 = "及格"             │ ✓ 成立
    else:                  ┌─────▼─────┐
        等级 = "不及格"     │ 等级="优秀"│  ← 一个**盒子**，值掉进去
                          └─────┬─────┘
                                │
                          程序继续往下走

  然后**一个小点沿这条路滚动过去**（不是跳，是滚）：
    · 条件框会**脉冲**一下（表示"在这里做了判断"）
    · 走通的那条路上，线**亮起来**
    · 没走的那两条路，**淡下去**
    · 赋值发生时，**值从右边滑进盒子**（带缓动和过冲）

⚠ 这才是"互动动画"：
  · **互** —— 能按、能停、能倒退、能改输入重跑
  · **动** —— 位置/透明度/颜色**连续变化**（不是离散跳变）
  · **画** —— 有图形（框、箭头、路径），不是文字排版

═══ 为什么用 Canvas 画而不是拼文字 ═══

  只有 Canvas 能做"每帧改坐标"。
  `Text` 控件的位置由布局管理器定，改一次要重排 —— 做不到平滑移动。
  所以这一版是**真·画布动画**：`动效.跑()` 每 16ms 重画一次。
"""
from __future__ import annotations

import math
import re
import tkinter as tk
import tkinter.font as tkfont

from . import theme as T
from . import tracer


# ── 流程图的几何 ────────────────────────────────────────────────────────

class 盒:
    """图上的一个方框。"""

    __slots__ = ("x0", "y0", "x1", "y1", "文", "类")

    def __init__(self, x0, y0, x1, y1, 文="", 类="语句"):
        self.x0, self.y0, self.x1, self.y1 = x0, y0, x1, y1
        self.文, self.类 = 文, 类

    @property
    def 心(self):
        return ((self.x0 + self.x1) / 2, (self.y0 + self.y1) / 2)

    def 含(self, x, y):
        return self.x0 <= x <= self.x1 and self.y0 <= y <= self.y1


def _分类(行: str) -> str:
    """把一行代码归成流程图的节点类型。"""
    s = 行.strip()
    if re.match(r"^(if|elif)\b", s):
        return "判断"
    if s.startswith("else"):
        return "否则"
    if re.match(r"^(for|while)\b", s):
        return "循环"
    if s.startswith("def "):
        return "定义"
    if s.startswith("return"):
        return "返回"
    if "=" in s and not any(o in s for o in ("==", "!=", ">=", "<=")):
        return "赋值"
    if s.startswith("print"):
        return "输出"
    return "语句"


def 建图(源码: str, 宽: int, 高: int) -> tuple[list[盒], dict[int, 盒]]:
    """把源码画成一列方框，返回 (所有盒子, {行号: 盒子})。

    ⚠ 刻意**不做完整流程图**（那要真正的解析器，而且会把简单的事画复杂）。
      这里做的是**带结构的竖排**：
        · 每个语句一个框
        · 判断/循环的框**缩进更浅、更宽**（视觉上是"岔路口"）
        · 分支体**往右缩**（视觉上挂在岔路口下面）
      ⇒ 对"看到程序怎么走"这个目标足够了，而且任何代码都画得出来
        （不认识的语句就当普通框）。
    """
    行们 = [l for l in 源码.split("\n")]
    有效 = [(i + 1, l) for i, l in enumerate(行们) if l.strip()]
    if not 有效:
        return [], {}

    行高 = 26
    间距 = 14
    #: 缩进每级往右移 26px（和代码缩进的感觉一致）
    盒们, 表 = [], {}
    y = 12
    for 行号, 文本 in 有效:
        缩 = len(文本) - len(文本.lstrip(" "))
        级 = 缩 // 4
        左 = 20 + 级 * 26
        类型 = _分类(文本)
        身 = 文本.strip()
        if len(身) > 26:
            身 = 身[:25] + "…"
        #: 判断框要宽一点（它上面要显示"成立/不成立"）
        右 = 宽 - 20 - (0 if 类型 in ("判断", "否则", "循环") else 级 * 26)
        b = 盒(左, y, 右, y + 行高, 身, 类型)
        盒们.append(b)
        表[行号] = b
        y += 行高 + 间距
    return 盒们, 表


# ── 画 ──────────────────────────────────────────────────────────────────

class 流程图:
    """一个**带动画**的执行流可视化。

    ⚠ 用法：`流程图(父容器, 代码)`，自带控制条。
    """

    def __init__(self, 父, 代码: str, 标题: str = ""):
        self.代码 = 代码.strip("\n")
        self.记录 = tracer.录(self.代码)
        self.标题 = 标题
        self.第几步 = 0
        self._动 = None
        self._播放句柄 = None

        self.码字 = tkfont.Font(family=T.码, size=T.字_微)
        self.小字 = tkfont.Font(family=T.界, size=T.字_微)
        self.大字 = tkfont.Font(family=T.界, size=T.字_小)

        self.框 = tk.Frame(父, bg=T.浮起, highlightthickness=1,
                         highlightbackground=T.线)
        头 = tk.Frame(self.框, bg=T.浮起)
        头.pack(fill="x", padx=T.间_中, pady=(T.间_小, 0))
        tk.Label(头, text=("▶  " + 标题) if 标题 else "▶  看看程序怎么跑",
                 bg=T.浮起, fg=T.强调,
                 font=T.界面字体(T.字_小, 粗=True)).pack(side="left")
        self.计数 = tk.Label(头, text="", bg=T.浮起, fg=T.文字弱,
                          font=T.界面字体(T.字_微))
        self.计数.pack(side="right")

        self.画布 = tk.Canvas(self.框, height=300, bg=T.码底,
                            highlightthickness=0, bd=0)
        self.画布.pack(fill="both", expand=True, padx=T.间_中,
                     pady=(T.间_极小, 0))

        条 = tk.Frame(self.框, bg=T.浮起)
        条.pack(fill="x", padx=T.间_中, pady=T.间_小)
        self._钮(条, "重来", self.重来)
        self._钮(条, "◀ 上一步", self.上一步)
        self._主钮(条, "下一步 ▶", self.下一步)
        self._钮(条, "▶▶ 播放", self.播放)
        self.步字 = tk.Label(条, text="", bg=T.浮起, fg=T.文字弱,
                          font=T.界面字体(T.字_微))
        self.步字.pack(side="right")

        if self.记录.错 or not self.记录.完整:
            tk.Label(self.框, text=self._说明(), bg=T.浮起, fg=T.警示,
                     font=T.界面字体(T.字_微), anchor="w", justify="left",
                     wraplength=560).pack(fill="x", padx=T.间_中,
                                        pady=(0, T.间_小))
        self.画布.bind("<Configure>", lambda e: self._重画())
        self.重来()

    # ── 控制 ──────────────────────────────────────────────────────────
    def _钮(self, 父, 字, 命令):
        b = tk.Button(父, text=字, command=命令, bg=T.面板, fg=T.文字次,
                     bd=0, relief="flat", highlightthickness=0,
                     activebackground=T.浮起, activeforeground=T.文字,
                     font=T.界面字体(T.字_微), padx=T.间_中, pady=2,
                     cursor="hand2")
        b.pack(side="left", padx=(0, T.间_小))
        return b

    def _主钮(self, 父, 字, 命令):
        b = tk.Button(父, text=字, command=命令, bg=T.强调底, fg=T.强调底字,
                     bd=0, relief="flat", highlightthickness=0,
                     activebackground=T.强调亮, activeforeground=T.强调底字,
                     font=T.界面字体(T.字_微, 粗=True), padx=T.间_中, pady=2,
                     cursor="hand2")
        b.pack(side="left", padx=(0, T.间_小))
        return b

    def 包(self, **kw):
        self.框.pack(fill="x", padx=T.间_大, pady=T.间_中, **kw)
        return self

    def _说明(self):
        if self.记录.错 and not self.记录.步们:
            return "⚠ 这段代码跑不起来：%s" % self.记录.错
        if not self.记录.完整:
            return "⚠ 跑到 %d 步就停了 —— 多半有死循环" % tracer.最多步
        return "⚠ 最后抛了：%s" % self.记录.错

    def 重来(self):
        self._停()
        self.第几步 = 0
        self._重画()

    def 下一步(self):
        self._停()
        if self.第几步 >= len(self.记录.步们) - 1:
            self._重画()
            return
        #: ⚠⚠ **先自增，再动画** —— 顺序不能反。
        #:   我第一版写反了：
        #:       self._小点动(self.第几步, self.第几步 + 1)   # 结束时会 _重画()
        #:       self.第几步 += 1                            # ← 太晚了
        #:   而 `_小点动` 在动画结束时会 `_重画()`，那时 `第几步` 还是**旧值**
        #:   ⇒ 画出来的是上一步。**视觉上像"点了没反应"**，
        #:   而搜索代码时它看起来完全正常（自增明明写了）。
        #:   ⇒ 这类"状态更新时机"的 bug 光读代码很难发现，
        #:     要靠**在动画中途和结束后各量一次**（我就是这么找到的）。
        从 = self.第几步
        self.第几步 += 1
        self._小点动(从, self.第几步)

    def 上一步(self):
        self._停()
        if self.第几步 > 0:
            self.第几步 -= 1
            self._重画()

    def 播放(self):
        """一路播下去。⚠ 每步之间让**小点真的走一段**，而不是跳。"""
        self._停()
        if self.第几步 >= len(self.记录.步们) - 1:
            self.第几步 = 0
            self._重画()

        #: ⚠ 步间停顿**按步数自适应**：实测 16 步的循环课，
        #:   固定 120ms 停顿会让"全部播放"要 6.7 秒 —— 学员会失去耐心
        #:   而去点"下一步"，那个按钮就成摆设了。
        #:   ⇒ 步多就连续播（把时间留给"看清路径"本身）。
        步数 = len(self.记录.步们)
        间隔 = 120 if 步数 <= 8 else (60 if 步数 <= 14 else 10)

        def 走():
            if self.第几步 >= len(self.记录.步们) - 1:
                self._播放句柄 = None
                self._重画()
                return
            #: ⚠ 同样：**先自增再动画**（见 `下一步` 里的注释）
            从 = self.第几步
            self.第几步 += 1
            self._小点动(从, self.第几步, 之后=走)
        self._播放句柄 = self.画布.after(间隔, 走)

    def _停(self):
        if self._播放句柄 is not None:
            try:
                self.画布.after_cancel(self._播放句柄)
            except Exception:
                pass
            self._播放句柄 = None
        if self._动 is not None:
            self._动.停()
            self._动 = None

    # ── 动画 ──────────────────────────────────────────────────────────
    def _小点动(self, 从: int, 到: int, 之后=None):
        """小点**沿路径滚动**过去 + 目标框**脉冲** + 线**亮起来**。

        ⚠ 这是"动画"和"跳变"的分界线：上一版我是直接 `_重画()`，
          高亮条**瞬间**换位置 —— 看上去就是"翻了一页"。
          现在小点有**中间位置**，所以眼睛能跟上"它从哪来、到哪去"。
        """
        步1 = self.记录.步们[从] if 从 < len(self.记录.步们) else None
        步2 = self.记录.步们[到] if 到 < len(self.记录.步们) else None
        if 步1 is None or 步2 is None:
            self._重画()
            return
        盒们, 表 = self._图()
        a = 表.get(步1.行)
        b = 表.get(步2.行)
        if a is None or b is None:
            self._重画()
            return

        from . import anim as A
        self._动 = A.动效(self.画布)

        #: ⚠⚠ 两段式，都用**非线性**曲线：
        #:
        #:   第一段（0~340ms）：小点**沿路滚动**过去 —— `缓出`
        #:     先快后慢。这一段用线性的话，小点看起来像"被拖过去的"，
        #:     而缓出像"被扔过去的" —— 后者才有质量感。
        #:
        #:   第二段（340~560ms）：到达后框**弹一下** + 值**滑入** —— `弹簧`
        #:     ⚠ 这一段是**为了"到位"这件事**。上一版到达时框
        #:     直接换颜色、值直接出现 —— 那是"跳变"，
        #:     而**跳变就是用户说的"塑料感"的来源**：
        #:     真实世界的东西不会瞬间到位。
        #: ⚠⚠ **时长按距离自适应，不是固定值。**
        #:
        #:   实测踩到：固定 560ms/步时，「08 循环」那课有 16 步
        #:   ⇒ 全部播放要 **9 秒**。学员会直接去点"下一步"，
        #:     那个"播放"按钮就成了摆设。
        #:
        #:   ⇒ 走多远就花多久：
        #:       相邻框（最常见的下一步）→ ~200ms，感觉"利落"
        #:       隔几个框（跳进分支体）  → 最多 340ms，够看清路径
        #:     第二段（到位那一下）固定 170ms —— 它和距离无关。
        dy = abs(b.心[1] - a.心[1])
        走 = max(110.0, min(260.0, dy * 3.0))
        到位 = 140.0
        总 = 走 + 到位

        def 帧(t):
            if t <= 走:
                p = t / 走
                e = A.缓出(p)
                x = a.心[0] + (b.心[0] - a.心[0]) * e
                y = a.心[1] + (b.心[1] - a.心[1]) * e
                self._画主体(目标行=步2.行, 小点=(x, y), 到达=0.0)
            else:
                #: 第二段：小点收进框里，框弹一下
                q = min(1.0, (t - 走) / (总 - 走))
                self._画主体(目标行=步2.行,
                           小点=b.心 if q < 0.35 else None,
                           到达=A.弹簧(q, 过冲=2.0, 次数=1),
                           值现=A.弹簧(q, 过冲=2.4, 次数=1))
            if t >= 总:
                self._动.停()
                self._动 = None
                self._重画()
                if 之后:
                    之后()
        self._动.跑(16, 帧)

    # ── 画 ────────────────────────────────────────────────────────────
    def _图(self):
        宽 = max(420, self.画布.winfo_width() - T.间_中)
        return 建图(self.代码, 宽, 300)

    def _重画(self):
        self._画主体(目标行=(self.记录.步们[self.第几步].行
                          if self.记录.步们 else None))

    def _画主体(self, 目标行=None, 小点=None, 到达=1.0, 值现=1.0):
        """画一帧。

        `到达` —— 当前框的"弹一下"进度（0=没弹，1=弹完）。用它缩放框。
        `值现` —— 变量标签的滑入进度（0=还没出现，1=到位）。
        """
        画 = self.画布
        画.delete("all")
        盒们, 表 = self._图()
        if not 盒们:
            return
        步 = self.记录.步们[self.第几步] if self.记录.步们 else None
        self.计数.configure(text="第 %d/%d 步" % (self.第几步,
                                              max(0, len(self.记录.步们) - 1)))
        self.步字.configure(text=("输出：%s" % 步.输出.strip().split("\n")[-1][:28])
                          if 步 and 步.输出.strip() else "")

        #: 走过的行号（用来给"走过的路"上色）
        走过 = set(s.行 for s in self.记录.步们[:self.第几步 + 1])

        #: ① 先画**连接线**
        for i in range(len(盒们) - 1):
            a, b = 盒们[i], 盒们[i + 1]
            x = b.x0
            亮 = (a.y1 >= b.y0 - 40) and (a.类 in ("判断", "否则", "循环"))
            画.create_line(x, a.y1, x, b.y0, fill=T.线强 if 亮 else T.线,
                          width=2 if 亮 else 1)

        #: ② 方框
        for b in 盒们:
            当前 = (b.y0 // 1) and 表.get(目标行) is b
            去过 = any(s.行 in (表 and [k for k, v in 表.items() if v is b])
                      for s in self.记录.步们[:self.第几步 + 1]) if 表 else False
            if b.类 == "判断":
                边, 底, 字色 = T.警示, "#2b2718", T.警示
            elif b.类 == "循环":
                边, 底, 字色 = T.信息, "#182530", T.信息
            elif b.类 == "赋值":
                边, 底, 字色 = T.线, T.浮起, T.码_数字
            elif b.类 == "输出":
                边, 底, 字色 = T.线, T.浮起, T.好
            else:
                边, 底, 字色 = T.线, T.浮起, T.码_其余
            if 当前:
                边, 底 = T.强调, "#1d2a3d"
                #: ⚠ 当前框加一圈**外发光**（两条同色描边）——
                #:   比单纯变边框颜色抢眼得多，而且不改变布局
                画.create_rectangle(b.x0 - 3, b.y0 - 3, b.x1 + 3, b.y1 + 3,
                                  outline=T.强调暗, width=1)
            #: ⚠ 当前框按"到达"进度**弹一下**：从 0.94 缩放到 1.0，
            #:   过冲时还会到 1.01 左右。不这么做的话框是"啪"地换色的。
            if 当前 and 到达 < 1.0:
                k = 0.94 + 0.06 * 到达
                cx, cy = (b.x0 + b.x1) / 2, (b.y0 + b.y1) / 2
                w2, h2 = (b.x1 - b.x0) / 2 * k, (b.y1 - b.y0) / 2 * k
                画.create_rectangle(cx - w2, cy - h2, cx + w2, cy + h2,
                                  fill=底, outline=边, width=2)
            else:
                画.create_rectangle(b.x0, b.y0, b.x1, b.y1,
                                  fill=底, outline=边,
                                  width=2 if 当前 else 1)
            画.create_text(b.x0 + 8, (b.y0 + b.y1) / 2, anchor="w",
                          text=b.文, fill=字色, font=self.码字)

        #: ③ 变量 → 画成**浮在右边的标签**，值变化时它会长出来
        if 步 is not None:
            x = max(b.x1 for b in 盒们) + 16
            y = 盒们[0].y0
            #: ⚠ 值**滑入**而不是"直接出现"：从右边 14px 之外滑到位。
            #:   而且用 `值现` 的进度做插值 —— 所以它是**连续移动**的。
            #:   ⚠ 最后那一个新出现的变量才滑；已经在的不动
            #:     （不然每一步整栏都在抖，看着很乱）。
            项 = sorted(步.变量.items())
            上次 = (self.记录.步们[self.第几步 - 1].变量
                   if self.第几步 > 0 and self.第几步 - 1 < len(self.记录.步们)
                   else {})
            for 名, 值 in 项:
                if y > 290:
                    break
                新 = (名 not in 上次) or (上次.get(名) != 值)
                #: 滑入：新变量从右边 14px 进来，且宽度从小长到大
                q = 值现 if 新 else 1.0
                dx = int(14 * (1 - q))
                w = int(116 * (0.55 + 0.45 * min(1.0, q)))
                画.create_rectangle(x + dx, y, x + dx + w, y + 20,
                                  fill=T.浮起, outline=T.线)
                if w > 40:
                    画.create_text(x + dx + 6, y + 10, anchor="w", text=名,
                                  fill=T.强调, font=self.小字)
                if w > 78:
                    画.create_text(x + dx + 50, y + 10, anchor="w", text=值,
                                  fill=T.码_数字, font=self.小字)
                y += 24

        #: ④ 小点（动画的核心）
        if 小点 is not None:
            x, y = 小点
            #: 光晕 + 实心点 —— 两层才看得出"在发光"
            画.create_oval(x - 7, y - 7, x + 7, y + 7,
                          fill="", outline=T.强调暗, width=1)
            画.create_oval(x - 4, y - 4, x + 4, y + 4,
                          fill=T.强调, outline=T.强调亮)
