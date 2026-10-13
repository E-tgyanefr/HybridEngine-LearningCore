"""GUI 学习界面（tkinter）—— 一个窗口把「看课 → 写代码 → 检查」全包了。

## 为什么用 tkinter

· **零第三方依赖**（本项目的铁律）—— tkinter 是标准库
· **已经在 exe 里**了（`Stage` 画布就用它，PyInstaller 已自动收进去）
  ⇒ 做 GUI 不增加任何打包体积，也不改变"装完就能跑"

## 界面布局

    ┌──────────────────────────────────────────────────────────────┐
    │  HybridEngine-light                                          │
    ├───────────────┬──────────────────────────┬───────────────────┤
    │ 课程线        │  代码编辑器              │  这一课要做什么   │
    │  ├ Python 基础│  （带行号、语法高亮）    │  ─────────────    │
    │  │  ├ 01 …   │                          │  运行结果 /       │
    │  │  └ 02 …   │                          │  检查报告         │
    │  ├ 人工智能…  │                          │                   │
    │  └ …          │                          │                   │
    ├───────────────┴──────────────────────────┴───────────────────┤
    │  [保存] [运行 F5] [检查答案] [参考答案]       状态栏           │
    └──────────────────────────────────────────────────────────────┘

## ⚠ 一条设计底线：**界面不做课程逻辑**

"去哪找课程、答案路径怎么算、检查怎么调"全在 `learnmodel.py` 里。
那一层能写判据（无头可测），这一层只是它的皮。
**把逻辑写进控件回调里，就等于放弃测试。**

## ⚠ 关于"卡界面"

`运行`/`检查` 走子进程（`learnmodel` 里就是），但 `dotnet run` 第一次要编译，
可能十几秒。**必须放线程**，否则窗口会假死 —— 学员会以为程序坏了。
"""
from __future__ import annotations

import os
import queue
import threading
import tkinter as tk
from tkinter import font as tkfont
from tkinter import messagebox, ttk

from . import learnmodel as M
from . import anim as A
from . import lessonview as LV
from . import demos as DEMO
from . import flow as FL
from . import progress as P


def 开目录(路径) -> None:
    """用系统的文件管理器打开一个目录 —— **跨平台**。

    ⚠ 原来这里只有 `os.startfile()`，那是 **Windows 专用**的
      （它在非 Windows 上根本不存在这个属性）。
      于是 Linux / macOS 上点「打开文件夹」会抛
      `AttributeError: module 'os' has no attribute 'startfile'`，
      学员看到的是"打不开文件夹"却不知道原因。

    ⇒ 按平台分流：Windows `os.startfile`、macOS `open`、
      Linux 先试 `xdg-open`，没有就退到 `gio open`（GNOME 装了 glib 就有）。
      ⚠ 这几个命令都要**先确认存在**再调 —— 直接 Popen 一个不存在的命令
        会抛 FileNotFoundError，那和"没有文件管理器"是两回事，提示要分清。
    """
    import shutil as _sh
    import subprocess as _sp
    import sys as _sys

    p = str(路径)
    if _sys.platform.startswith("win"):
        os.startfile(p)                                    # type: ignore[attr-defined]
        return
    if _sys.platform == "darwin":
        候选 = ["open"]
    else:
        # Linux：xdg-open 是标准，但精简系统可能没有；gio 通常随 glib 一起来
        候选 = [c for c in ("xdg-open", "gio") if _sh.which(c)]
        if not 候选:
            raise RuntimeError(
                "这个系统上找不到 xdg-open / gio —— 没有图形文件管理器。\n"
                "答题文件在这里，可以用命令行打开：\n%s" % p)
    命令 = 候选[0]
    args = [命令, "open", p] if 命令 == "gio" else [命令, p]
    _sp.Popen(args, stdout=_sp.DEVNULL, stderr=_sp.DEVNULL)


# ── 配色 ──────────────────────────────────────────────────────────────
#
# ⚠⚠ **全部从 `theme` 取，这个文件里不许再出现颜色字面量。**
#
#   为什么：这次"大改 UI"最容易变成到处手写魔数 ——
#   这个控件 #2b2d30、那个 #34363a，改一轮之后自己都说不清标准是什么。
#   ⇒ 决定集中在 `theme.py`，**改一个值整个界面跟着变**，
#     而且对比度有测试守着（`tests/test_ui_tokens.py`）。
#
# ⚠ 这里保留旧名字（`背景` / `暗文字` / `好色` …）是为了不用重写
#   整个文件里的引用 —— 但**值都来自 theme**。
#   同时也给一套和 `theme` 对齐的新名字（`文字次` / `文字弱` / `线色` …），
#   新写的代码用新名字。两套指向同一个值，不会分叉。
import hybridengine_light.theme as T   # noqa: E402  （在文件中部导入是刻意的：见上）

背景 = T.底
面板 = T.面板
编辑底 = T.底
文字 = T.文字
暗文字 = T.文字次
更暗文字 = T.文字弱
文字次 = T.文字次
文字弱 = T.文字弱
强调 = T.强调
强调亮 = T.强调亮
强调暗 = T.强调暗
好色 = T.好
坏色 = T.坏
高亮 = T.浮起
线色 = T.线
线强 = T.线强

#: 字体 —— ⚠ 不再用 Segoe UI：它对中文是**回退**的，
#: 中英文混排时字重/基线/行高都不一致（中文由雅黑代打），
#: 看起来"有点土"而原因很难一眼看出来。
#: ⇒ 界面文字统一 `Microsoft YaHei UI`（中英文都它管）。
#: 见 `theme.py` 里的 `界面字体()` / `等宽字体()`。
字体界面 = T.界面字体
字体码 = T.等宽字体

#: 极简语法高亮用的关键字（只做"看得清"，不做完整词法分析）
关键字 = set("""def class return if elif else for while in not and or is None True False
import from as try except finally with lambda yield global pass break continue raise assert del
int float str bool list dict set tuple print input len range str""".split())
关键字C = set("""int char float double void long short unsigned signed struct union enum typedef
static const return if else for while do switch case break continue goto sizeof
include define ifndef endif pragma printf scanf malloc free NULL""".split())


class 界面(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("HybridEngine-light —— 学编程，从第一行代码看到画面")
        self.geometry("1180x720")
        self.minsize(900, 560)
        self.configure(bg=背景)

        # 当前状态
        self.当前课: M.课 | None = None
        self.脏 = False                 # 编辑器有未保存改动
        self._任务队列: queue.Queue = queue.Queue()
        self._忙 = False

        self._建样式()
        # ⚠ 先建"主页"，学习界面等学员选了再建。
        #   为什么不在启动时把学习界面一起建好再藏起来：
        #   建课程树要跑 10 条线的发现逻辑（编译型那几条要起子进程）——
        #   那是几百毫秒到几秒。主页要**立刻**能看见、能点。
        self._容器 = tk.Frame(self, bg=背景)
        self._容器.pack(fill="both", expand=True)
        self.学习界面 = None

        # ── 进度（"哪几课读过了"）─────────────────────────────────────
        #
        # ⚠⚠ 为什么桌面端也要这个：手机版加进度可视化时想明白的一件事 ——
        #   **可见的进度是坚持下来的关键**。而桌面端原来什么进度都没有，
        #   219 课列在树里，学员看不到自己走过多少、还剩多少。
        #
        # ⚠ 「读过」的判据是**停留够久**，不是"选中过" ——
        #   键上下乱按一遍就会把 219 课全标成读过，那个进度就是假的。
        #   阈值比最短的一课（约 3 分钟）短得多，正常读一定会超过。
        self.读过: dict[str, float] = P.读()
        self._停留计时: str | None = None
        self._刚读完: str | None = None      # 只给这一课播一次反馈动画

        self._建主页()
        self.bind("<F5>", lambda e: self.点运行())
        self.bind("<Control-s>", lambda e: self.点保存())
        self.bind("<Escape>", lambda e: self.回主页())
        self.protocol("WM_DELETE_WINDOW", self._关闭时问)
        self.after(80, self._收任务)
        self._开场动画()


    # ══════════════════════ 通用小件（视觉）══════════════════════════

    def _幽灵按钮(self, 钮: tk.Button, 亮色: str | None = None):
        """把按钮做成"幽灵"样式：平时只有字，鼠标上去才浮出一个底。

        ⚠ 为什么要这样：顶栏那个「← 主页」如果画成实心按钮，
          它会**比课程内容还抢眼** —— 而学员 99% 的时间在读书，
          不需要一直被提醒"可以回主页"。
        """
        底 = 钮.cget("bg")
        亮 = 亮色 or 高亮
        钮.configure(bg=底, relief="flat", bd=0, highlightthickness=0,
                     activebackground=亮)
        钮.bind("<Enter>", lambda e: 钮.configure(bg=亮), add="+")
        钮.bind("<Leave>", lambda e: 钮.configure(bg=底), add="+")

    def _实心按钮(self, 父, 字: str, 命令, 主: bool = True, **kw) -> tk.Button:
        """实心按钮。`主=True` 用强调色底 + **深色字**（对比度 6.30:1）。

        ⚠ 深色字是刻意的 —— 亮蓝底上的**白字只有 2.77:1**，过不了 WCAG AA。
          直觉觉得"按钮要白字"，但那是浅色主题留下的习惯；
          深色主题上亮底深字反而更清楚，而且保住了那个蓝的亮眼程度。
          推算过程见 `theme.py` 的 `强调底`。
        """
        底 = T.强调底 if 主 else 面板
        字色 = T.强调底字 if 主 else 文字
        钮 = tk.Button(父, text=字, command=命令,
                       bg=底, fg=字色,
                       bd=0, relief="flat", highlightthickness=0,
                       activebackground=(T.强调亮 if 主 else 高亮),
                       activeforeground=字色,
                       font=字体界面(T.字_正文, 粗=主),
                       padx=T.间_大, pady=T.间_小 - 2,
                       cursor="hand2", **kw)
        #: 悬停/按下反馈 —— 没有它按钮"死"得像张图片
        钮.bind("<Enter>", lambda e: 钮.configure(bg=T.强调亮 if 主 else 高亮),
                add="+")
        钮.bind("<Leave>", lambda e: 钮.configure(bg=底), add="+")
        return 钮

    def _建主页(self):
        """进去之后先看到两个选项：游戏编写 / 课程学习。

        ⚠ 为什么要这一层：
          原来双击直接就是"课程树 + 编辑器 + 说明"三栏 —— 信息量很大，
          第一次打开的人不知道该点哪。两个大按钮把"你想干什么"问清楚，
          比一屏控件更好上手。

        ⚠ 主页**不建**课程树：那要跑 10 条线的发现逻辑（编译型还要起子进程）。
          主页必须立刻能看见、能点。
        """
        self._清空容器()
        self.主页 = tk.Frame(self._容器, bg=背景)
        self.主页.pack(fill="both", expand=True)

        框 = tk.Frame(self.主页, bg=背景)
        框.place(relx=0.5, rely=0.46, anchor="center")

        #: 品牌名：`light` 用强调色 —— 一眼分主次，而不是一整块同色的字
        牌 = tk.Frame(框, bg=背景)
        牌.pack(pady=(0, T.间_小))
        tk.Label(牌, text="HybridEngine", bg=背景, fg=文字,
                 font=字体界面(T.字_巨, 粗=True)).pack(side="left")
        tk.Label(牌, text="-light", bg=背景, fg=强调,
                 font=字体界面(T.字_巨, 粗=True)).pack(side="left")
        tk.Label(框, text="学编程，从第一行代码看到画面", bg=背景, fg=文字次,
                 font=字体界面(T.字_大)).pack(pady=(0, T.间_巨大))

        选 = tk.Frame(框, bg=背景)
        选.pack()
        self._大按钮(选, "游戏编写", "自己写一个小游戏，看得见画面",
                   self.进游戏编写).pack(side="left", padx=T.间_小)
        self._大按钮(选, "课程学习", "按编程语言挑，一课只讲一个知识点",
                   self.进课程学习, 主=True).pack(side="left", padx=T.间_小)

        tk.Label(框, text="想回这里随时按 Esc", bg=背景, fg=更暗文字,
                 font=字体界面(T.字_小)).pack(pady=(T.间_巨大, 0))

    def _大按钮(self, 父, 标题: str, 说明: str, 命令, 主: bool = False):
        """主页上的大按钮：标题大、说明小。

        ⚠ 两个按钮**刻意不一样重**：`课程学习` 用强调色边框（主路径），
          `游戏编写` 用普通边框。原来两个一模一样 ——
          那等于把"你想干什么"这个问题又推回给学员了。

        ⚠ 悬停时**整卡变亮 + 边框变强调色**：卡片没有按钮那种按下感，
          所以悬停反馈必须做足，否则学员不确定"这点得动吗"。
        """
        边 = 强调 if 主 else 线色
        悬边 = 强调亮
        卡 = tk.Frame(父, bg=面板, highlightthickness=1,
                     highlightbackground=边, bd=0)
        内 = tk.Frame(卡, bg=面板)
        内.pack(padx=T.间_巨大, pady=T.间_特大)
        tk.Label(内, text=标题, bg=面板, fg=文字,
                 font=字体界面(17, 粗=True)).pack()
        tk.Label(内, text=说明, bg=面板, fg=文字次,
                 font=字体界面(T.字_小)).pack(pady=(T.间_小, 0))

        def 进(_e=None):
            命令()

        def 亮(_e=None):
            卡.configure(highlightbackground=悬边, bg=高亮)
            内.configure(bg=高亮)
            for w in 内.winfo_children():
                w.configure(bg=高亮)

        def 灭(_e=None):
            卡.configure(highlightbackground=边, bg=面板)
            内.configure(bg=面板)
            for w in 内.winfo_children():
                w.configure(bg=面板)

        for w in (卡, 内, *内.winfo_children()):
            w.bind("<Button-1>", 进)
            w.bind("<Enter>", 亮, add="+")
            w.bind("<Leave>", 灭, add="+")
            w.configure(cursor="hand2")
        return 卡

    def _清空容器(self):
        for w in self._容器.winfo_children():
            w.destroy()

    def 回主页(self):
        """回到两个入口那一屏。

        ⚠ 学习界面**不销毁**（下次进来秒开），只是藏起来。
          但课程树里的编译型课程是后台补的，藏起来不影响它继续补。

        ⚠⚠ 回主页时要把**编辑器清空并把空状态放回来**。
          实测踩到：不清的话，再进来时编辑器是**空的、却显示着上一课的
          标题**（`self.当前课` 还留着）—— 学员会以为"内容加载失败了"。
        """
        if self._停留计时:
            try:
                self.after_cancel(self._停留计时)
            except Exception:
                pass
            self._停留计时 = None
        if self.学习界面 is not None and self.学习界面.winfo_exists():
            self._清空编辑器()
            self.学习界面.pack_forget()
        if self.主页 is not None and self.主页.winfo_exists():
            self.主页.pack(fill="both", expand=True)
            return
        self._建主页()

    def _清空编辑器(self):
        """把中间那一栏恢复成"还没选课"的样子。

        ⚠ 三样都要清：`self.当前课`、编辑标题、代码区。
          只清一两样的话，界面会自相矛盾（有标题没内容 / 有内容没标题）。
        """
        self.当前课 = None
        try:
            self.编辑标题.configure(text="选一课开始")
        except Exception:
            pass
        try:
            self.代码.delete("1.0", "end")
            self.代码.edit_modified(False)
            self.行号.configure(state="normal")
            self.行号.delete("1.0", "end")
            self.行号.configure(state="disabled")
        except Exception:
            pass
        self.脏 = False
        try:
            self.空状态.place(relx=0, rely=0, relwidth=1, relheight=1)
            self.空状态.lift()
        except Exception:
            pass

    def 进课程学习(self):
        """进学习界面，并且把左边那条**语言列表**拉出来。"""
        self._进学习界面()
        self._填语言列表()
        # ⚠ 编译型四条线（C# / C++ / Java / C）各要起一次子进程（约 1.5 秒），
        #   所以先把它们显示成"载入中…"占位，再在后台补上真实的课。
        #   （原来这次调度在 `__init__` 里；改成主页之后必须**移到这儿** ——
        #     否则编译型会永远停在"载入中"。）
        # ⚠ 只在**没补过**的时候调一次，避免来回切主页重复起子进程。
        if not getattr(self, "_编译型补过", False):
            self._编译型补过 = True
            self.after(120, self._后台载入编译型)

    def 进游戏编写(self):
        self._进学习界面()
        self._填语言列表(只看游戏=True)

    def _进学习界面(self):
        if self.学习界面 is None or not self.学习界面.winfo_exists():
            self.主页.pack_forget()
            self.学习界面 = tk.Frame(self._容器, bg=背景)
            self.学习界面.pack(fill="both", expand=True)
            self._建布局(self.学习界面)
        else:
            self.主页.pack_forget()
            self.学习界面.pack(fill="both", expand=True)

    # ══════════════════════ 样式与布局 ══════════════════════
    def _建样式(self):
        """ttk 控件的样式。

        ⚠⚠ 为什么必须显式写这些：
          ttk 的控件**不吃 Tk 那套 `bg=`/`fg=`** —— 它们走"样式"系统。
          不写的话，`Treeview` / `Notebook` 会用**系统默认的浅色主题**，
          于是深色界面上会突兀地出现几块白 —— 而那正是最容易漏、
          又最显眼的地方（"这个软件看起来没做完"）。

        ⚠ 主题必须先 `theme_use("clam")`：默认的 `vista` 主题在 Windows 上
          **忽略大部分配色设置**（它按系统外观画）。`clam` 是可完全定制的那个。
        """
        s = ttk.Style(self)
        with contextlib_suppress():
            s.theme_use("clam")

        # ── 课程树 ──
        字体树 = 字体界面(T.字_正文)
        #: ⚠ 行高 24 → 28：课程名有点长的会折行，24 挤得看不下去；
        #:   而且鼠标点起来 28 更好命中
        s.configure("Treeview", background=面板, fieldbackground=面板,
                    foreground=文字, borderwidth=0, rowheight=28,
                    font=字体树)
        s.configure("Treeview.Heading", background=面板, foreground=文字次,
                    relief="flat", borderwidth=0)
        #: ⚠ 选中行用**强调蓝底 + 深色字**（不是白字）——
        #:   白字在 #4d9dff 上只有 2.77:1，过不了 AA。见 theme.py。
        s.map("Treeview", background=[("selected", 强调)],
              foreground=[("selected", T.强调底字)])

        # ── 标签页 ──
        s.configure("TNotebook", background=面板, borderwidth=0,
                    tabmargins=(T.间_小, T.间_极小, 0, 0))
        #: ⚠ 未选中的标签**用背景色**（比面板暗）—— 视觉上"凹进去"，
        #:   选中的那个和内容区同色，看起来是"接上了"。
        #:   这是标签页最省事也最有效的层次做法。
        s.configure("TNotebook.Tab", background=背景, foreground=更暗文字,
                    padding=(T.间_大, T.间_小), borderwidth=0,
                    font=字体界面(T.字_小))
        s.map("TNotebook.Tab",
              background=[("selected", 编辑底), ("active", 高亮)],
              foreground=[("selected", 文字), ("active", 文字)],
              expand=[("selected", (0, 0, 0, 2))])
        s.configure("TFrame", background=背景)

    def _建布局(self, 父):
        """学习界面的**整体重构**。

        ═══ 用户原话：「感觉ui没有区别，直接重构」═══

        ⚠⚠ 为什么上一次改完"没区别"—— 我改的是**看不见的东西**：
           字体族（Segoe UI → 雅黑）、对比度微调、间距从 8 到 12、
           按钮分了个组。那些都对，但**结构一模一样**：
           顶栏 + 三栏 + 底栏按钮条。
           ⇒ **要让人看出"改了"，必须动结构，不是动数值。**

        ═══ 这一版的结构（和上一版是两回事）═══

            上一版                          这一版
            ─────────────────────           ─────────────────────
            顶栏（横跨全宽）                 ┌──────┬──────────────┐
            ┌────┬──────┬─────┐             │ 侧栏 │ 课时头（大字）│
            │列表│编辑器│正文 │             │ 导航 │──────────────│
            └────┴──────┴─────┘             │ 进度 │ 编辑器       │
            底栏：五个按钮一排                │      │ + 侧栏(正文/ │
                                             │      │   结果)      │
                                             └──────┴──────────────┘
                                             底栏：左=状态 右=操作

        三处**结构性**变化：

          ① **课时头**：当前课名从"树里那一行"和一个小标签，
             升成一个**大标题区**。学员一眼知道自己在哪一课 ——
             这是整个界面最该突出的信息，上一版它被挤在编辑器顶上。

          ② **操作按重要性分层**：
               · 主操作（检查答案）→ 课时头右边，实心强调色
               · 常用（保存 / 运行）→ 课时头右边，幽灵样式
               · 导航/状态 → 底栏
             ⚠ 上一版五个按钮挤在一排、一样重，
               "保存"和"打开答题文件夹"看起来同等重要 —— 那是错的。

          ③ **侧栏独立成块**：品牌 + 进度 + 树在**同一个暗色列**里，
             和内容区用 1px 竖线分开。上一版这些都挂在顶栏下面，
             视觉上"没有边"，整块是平的。

        ⚠ 所有 `self.xxx` 控件的名字**一个都不能改** ——
          文件里另有几十处引用它们（`self.树` 16 处、`self.代码` 34 处…）。
          重写布局时最容易踩的就是这个：改了名字，测试和功能
          会在别处炸，而报错指向的地方和这个函数没关系。
        """
        # ══ 侧栏 ═══════════════════════════════════════════════════════
        #
        # ⚠ 侧栏背景用 `底`（比面板更暗），内容区用 `面板` ——
        #   视觉上"导航深、内容浅"，这是侧栏布局最省事也最有效的层次。
        #   ⚠ 所以 `面板` 这个名字在这一版里指的是**内容区底色**，
        #     `底` 指的是**侧栏/编辑器底色**。命名沿用旧值，只换用途。
        侧栏 = tk.Frame(父, bg=面板, width=300)
        侧栏.pack(side="left", fill="y")
        侧栏.pack_propagate(False)
        #: 侧栏和内容之间的 1px 竖线
        tk.Frame(父, bg=线色, width=1).pack(side="left", fill="y")

        # ── 侧栏顶：品牌 + 回主页 ──
        牌行 = tk.Frame(侧栏, bg=面板)
        牌行.pack(fill="x", padx=T.间_大, pady=(T.间_中, T.间_极小))
        牌 = tk.Frame(牌行, bg=面板)
        牌.pack(side="left")
        tk.Label(牌, text="HybridEngine", bg=面板, fg=文字,
                 font=字体界面(T.字_标题, 粗=True)).pack(side="left")
        tk.Label(牌, text="-light", bg=面板, fg=强调,
                 font=字体界面(T.字_标题, 粗=True)).pack(side="left")
        self.主页钮 = tk.Button(牌行, text="主页", command=self.回主页,
                             bg=面板, fg=更暗文字,
                             bd=0, relief="flat", highlightthickness=0,
                             activebackground=高亮, activeforeground=文字,
                             font=字体界面(T.字_微), padx=T.间_小,
                             cursor="hand2")
        self.主页钮.pack(side="right")
        self._幽灵按钮(self.主页钮)

        # ── 侧栏：语言列表标题 + 定位语 ──
        self.语言标题 = tk.Label(侧栏, text="选择编程语言", bg=面板, fg=文字次,
                              font=字体界面(T.字_小), anchor="w")
        self.语言标题.pack(fill="x", padx=T.间_大, pady=(T.间_中, 0))
        self.定位字 = tk.Label(侧栏, text="", bg=面板, fg=更暗文字,
                            font=字体界面(T.字_微), anchor="w",
                            justify="left", wraplength=252)
        self.定位字.pack(fill="x", padx=T.间_大, pady=(0, T.间_小))

        # ── 侧栏：进度条（"走过多少"）──
        #
        # ⚠ 放在**语言列表正上方**：这是侧栏最显眼的位置，
        #   而且每次从一课回来它都会"长一截"—— 那一下"长"本身就是奖励。
        # ⚠ 用 `定值进度条`（填充）而不是 `进度条`（来回滑）：
        #   前者表示"走到哪了"，后者表示"我在忙"—— 两件事。
        self.进度画布 = tk.Canvas(侧栏, height=6, bg=面板,
                                highlightthickness=0, bd=0)
        self.进度画布.pack(fill="x", padx=T.间_大, pady=(T.间_极小, T.间_极小))
        self.进度字 = tk.Label(侧栏, text="", bg=面板, fg=更暗文字,
                            font=字体界面(T.字_微), anchor="w")
        self.进度字.pack(fill="x", padx=T.间_大, pady=(0, T.间_小))
        self._进度条 = None          # 等画布有真实宽度了再建（见 _刷进度）

        #: 进度和树之间一条细线 —— 把"统计"和"清单"分开
        tk.Frame(侧栏, bg=线色, height=1).pack(fill="x", padx=T.间_中)

        # ── 侧栏：课程树 ──
        self.树 = ttk.Treeview(侧栏, show="tree", selectmode="browse")
        self.树.pack(fill="both", expand=True, padx=T.间_小, pady=T.间_小)
        self.树.bind("<<TreeviewSelect>>", self._选课)

        # ══ 内容区 ═════════════════════════════════════════════════════
        内容 = tk.Frame(父, bg=背景)
        内容.pack(side="left", fill="both", expand=True)

        # ── 课时头：**当前课名升成主角** ──────────────────────────────
        #
        # ⚠⚠ 这是这一版最重要的结构变化。
        #   上一版课名只是编辑器顶上一个 9 号小标签，
        #   而学员最需要随时知道的就是"我在哪一课"。
        头 = tk.Frame(内容, bg=面板)
        头.pack(fill="x")
        头内 = tk.Frame(头, bg=面板)
        头内.pack(fill="x", padx=T.间_大, pady=T.间_中)

        字块 = tk.Frame(头内, bg=面板)
        字块.pack(side="left", fill="x", expand=True)
        self.编辑标题 = tk.Label(字块, text="选一课开始", bg=面板, fg=文字,
                             font=字体界面(T.字_标题, 粗=True), anchor="w")
        self.编辑标题.pack(fill="x")
        self.课副标题 = tk.Label(字块, text="", bg=面板, fg=更暗文字,
                             font=字体界面(T.字_微), anchor="w")
        self.课副标题.pack(fill="x")

        #: 操作按重要性分层（见 docstring 的 ②）
        钮区 = tk.Frame(头内, bg=面板)
        钮区.pack(side="right")

        def 头钮(字, 命令, 主=False, 快捷键=""):
            文 = ("%s  %s" % (字, 快捷键)) if 快捷键 else 字
            钮 = tk.Button(钮区, text=文, command=命令,
                          bg=(T.强调底 if 主 else 面板),
                          fg=(T.强调底字 if 主 else 文字次),
                          bd=0, relief="flat", highlightthickness=0,
                          activebackground=(T.强调亮 if 主 else 高亮),
                          activeforeground=(T.强调底字 if 主 else 文字),
                          font=字体界面(T.字_小, 粗=主),
                          padx=(T.间_大 if 主 else T.间_中),
                          pady=T.间_极小 + 2, cursor="hand2")
            钮.pack(side="left", padx=(T.间_小, 0))
            if 主:
                钮.bind("<Enter>", lambda e: 钮.configure(bg=T.强调亮), add="+")
                钮.bind("<Leave>", lambda e: 钮.configure(bg=T.强调底), add="+")
            else:
                self._幽灵按钮(钮)
            return 钮

        #: 主操作放最右（视线落点），常用在它左边
        头钮("检查答案", self.点检查, 主=True)
        头钮("运行", self.点运行, 快捷键="F5")
        头钮("保存", self.点保存)
        头钮("参考答案", self.点参考答案)
        tk.Frame(内容, bg=线色, height=1).pack(fill="x")

        # ── 中部：编辑器 | 侧栏（正文 / 结果）──────────────────────────
        中主体 = tk.PanedWindow(内容, orient="horizontal", bg=背景,
                              sashwidth=5, sashrelief="flat", bd=0)
        中主体.pack(fill="both", expand=True)
        #: ⚠ 进度条的宽度靠侧栏的实际宽度 ⇒ 被拖动时要重算。
        #:   不绑这个的话，拖完之后条还是旧宽度，会**露在格子外面**。
        中主体.bind("<ButtonRelease-1>", lambda e: self._刷进度())

        # ── 编辑器 ──
        中 = tk.Frame(中主体, bg=编辑底)
        中主体.add(中, width=560, minsize=320)
        编辑框 = tk.Frame(中, bg=编辑底)
        编辑框.pack(fill="both", expand=True)
        #: 行号列独立成框 + 右边一条 1px 线（不然行号会"飘"在代码左边）
        行号区 = tk.Frame(编辑框, bg=面板)
        行号区.pack(side="left", fill="y")
        self.行号 = tk.Text(行号区, width=4, padx=T.间_小, pady=T.间_小,
                           takefocus=0, bd=0, highlightthickness=0,
                           bg=面板, fg=更暗文字, font=字体码(T.字_正文),
                           state="disabled", wrap="none")
        self.行号.pack(side="left", fill="y")
        tk.Frame(行号区, bg=线色, width=1).pack(side="right", fill="y")
        代码区 = tk.Frame(编辑框, bg=编辑底)
        代码区.pack(side="left", fill="both", expand=True)
        #: ⚠ 内边距 12/8 + 行距 2 —— 代码贴着边框看久了很累，
        #:   而这是**要盯着看很久**的界面（一节 5~18 分钟）。
        self.代码 = tk.Text(代码区, bd=0, highlightthickness=0,
                          bg=编辑底, fg=文字, insertbackground=强调,
                          insertwidth=2,
                          font=字体码(T.字_正文), wrap="none", undo=True,
                          selectbackground=强调, selectforeground=T.强调底字,
                          padx=T.间_中, pady=T.间_小,
                          spacing1=2, spacing3=2)
        self.代码.pack(side="left", fill="both", expand=True)
        sb = ttk.Scrollbar(代码区, orient="vertical", command=self._滚)
        sb.pack(side="right", fill="y")
        self.代码.configure(yscrollcommand=lambda a, b: (sb.set(a, b), self._画行号()))
        self.代码.bind("<<Modified>>", self._改动)
        self.代码.bind("<KeyRelease>", lambda e: self._重高亮())
        self.代码.bind("<MouseWheel>", lambda e: self.after(1, self._画行号))

        # ── 空状态（盖在代码区上面）──
        #
        # ⚠⚠ 为什么必须有：没选课时编辑器区是**一大片纯黑**。
        #   那不只是"不好看"—— 它让学员**不知道该干什么**，
        #   而这是全屏最大的一块区域，它的第一印象就是第一印象。
        # ⚠ 做法：一个铺满的 Frame，选了课就 `place_forget()` 收起。
        #   **不往 Text 里塞占位文字** —— 那会弄脏 undo 栈
        #   （学员按 Ctrl+Z 会把占位文字撤回来）。
        self.空状态 = tk.Frame(代码区, bg=编辑底)
        内 = tk.Frame(self.空状态, bg=编辑底)
        内.place(relx=0.5, rely=0.42, anchor="center")
        tk.Label(内, text="←", bg=编辑底, fg=线强,
                 font=字体界面(30)).pack()
        tk.Label(内, text="左边挑一门语言，再挑一课", bg=编辑底, fg=文字次,
                 font=字体界面(T.字_大)).pack(pady=(T.间_中, T.间_极小))
        tk.Label(内, text="这一课要写的东西会出现在这里", bg=编辑底,
                 fg=更暗文字, font=字体界面(T.字_小)).pack()
        tk.Label(内, text="F5 运行 · Esc 回主页", bg=编辑底, fg=更暗文字,
                 font=字体界面(T.字_微)).pack(pady=(T.间_特大, 0))
        self.空状态.place(relx=0, rely=0, relwidth=1, relheight=1)

        # ── 右：正文 / 结果 ──
        右 = tk.Frame(中主体, bg=背景)
        中主体.add(右, width=380, minsize=240)
        self.页 = ttk.Notebook(右)
        self.页.pack(fill="both", expand=True)
        #: ⚠ 正文外面套一层容器，好让**演示块挂在正文下面** ——
        #:   `_加文本页` 返回的是 Notebook 的一个 tab，直接往里面 pack
        #:   第二个控件会让正文被挤扁（`Text` 默认 expand）。
        #:   ⇒ 正文占满 tab，演示挂在一个**独立的小页**上（第 3 个 tab）。
        self.正文 = self._加文本页("这一课要做什么")
        self.演示位 = self._加容器页("动手看看")
        self.结果 = self._加文本页("运行结果 / 检查报告")
        # ── 结果徽标：检查完在结果页顶部**弹**一个 ✓ / ✗ ──
        # ⚠ 用动画而不是一行文字：检查结果是学员最关心的反馈，
        #   一闪而过的文字容易被忽略；弹出来的徽标抢注意力，
        #   而"弹"这个动作本身暗示"有新消息"。
        # ⚠ 画布要挂在**结果页那个 Frame** 上（`self.结果.master`）——
        #   直接 pack 进 Notebook 是无效的（Notebook 只认自己的 tab）。
        条 = tk.Canvas(self.结果.master, height=34, bg=编辑底,
                      highlightthickness=0, bd=0)
        条.pack(fill="x", side="top", before=self.结果)
        self.判定画布 = 条
        self.结果徽标 = A.结果徽标(条, 22, 17)

        # ══ 底栏：只放导航和状态（操作已经在课时头上了）═════════════
        tk.Frame(内容, bg=线色, height=1).pack(fill="x", side="bottom")
        底 = tk.Frame(内容, bg=面板, height=40)
        底.pack(fill="x", side="bottom")
        底.pack_propagate(False)
        #: 辅助操作放底栏最右（不占课时头的注意力）
        for 字, 命令 in (("打开答题文件夹", self.点打开文件夹),):
            钮 = tk.Button(底, text=字, command=命令, bg=面板, fg=更暗文字,
                          bd=0, relief="flat", highlightthickness=0,
                          activebackground=高亮, activeforeground=文字,
                          font=字体界面(T.字_微), padx=T.间_小,
                          cursor="hand2")
            钮.pack(side="right", padx=(0, T.间_中), pady=T.间_小)
            self._幽灵按钮(钮)
        self.环境标签 = tk.Label(底, text="", bg=面板, fg=更暗文字,
                             font=字体界面(T.字_微))
        self.环境标签.pack(side="right", padx=T.间_中)
        #: 忙碌指示：转圈圈，而不是一句死文字
        #: ⚠ `dotnet run` 第一次编译要十几秒。纯文字学员会以为卡死；
        #:   转圈是**通用**的"还在干活"信号。
        self.忙碌画布 = tk.Canvas(底, width=20, height=20, bg=面板,
                                highlightthickness=0, bd=0)
        self.忙碌画布.pack(side="left", padx=(T.间_中, 0))
        self.旋转 = A.旋转弧(self.忙碌画布, 10, 10, r=7)
        self.状态 = tk.Label(底, text="就绪", bg=面板, fg=更暗文字,
                           font=字体界面(T.字_微), anchor="w")
        self.状态.pack(side="left", fill="x", expand=True, padx=T.间_小)


    def _开场动画(self, 总毫秒: int = 1100):
        """把名字"弹"出来，再淡出 —— 让启动有个"开了"的感觉。

        ⚠ 三条约束（都是"别让动效变成负担"）：
          · **可跳过**：点一下 / 按任意键就立刻结束
          · **够短**：1.1 秒。再长就是从"有质感"变成"烦"
          · **不阻塞**：动画结束才建主界面，但中途可中断
        """
        self.遮罩 = tk.Frame(self, bg=背景)
        self.遮罩.place(relx=0, rely=0, relwidth=1, relheight=1)
        画 = tk.Canvas(self.遮罩, bg=背景, highlightthickness=0, bd=0)
        画.pack(fill="both", expand=True)
        self._开场完成 = False

        def 收尾(*_):
            if self._开场完成:
                return
            self._开场完成 = True
            self.遮罩.destroy()

        self.bind("<Button-1>", 收尾, add="+")
        self.bind("<Key>", 收尾, add="+")

        def 帧(t: float):
            if self._开场完成:
                return
            宽, 高 = self.winfo_width(), self.winfo_height()
            if 宽 < 50:
                宽, 高 = 1180, 720
            画.delete("all")
            p = min(1.0, t / 620)
            # 标题：从 0.6 倍"弹"到 1 倍
            s = A.缩放值(p, 0.6, 1.0, A.回弹)
            字号 = max(8, int(30 * s))
            画.create_text(宽 // 2, 高 // 2 - 16, text="HybridEngine-light",
                           fill=文字, font=字体界面(字号, 粗=True))
            # 副标题：晚 180ms 淡入（透明度靠灰度逼近）
            q = A.缩放值(min(1.0, max(0.0, (t - 180) / 500)), 0.0, 1.0)
            if q > 0.02:
                灰 = int(0x2b + (0xd6 - 0x2b) * q)
                画.create_text(宽 // 2, 高 // 2 + 22,
                               text="学编程，从第一行代码看到画面",
                               fill=f"#{灰:02x}{灰:02x}{灰:02x}",
                               font=字体界面(11))
            # 底部进度条
            w = 180
            x0 = 宽 // 2 - w // 2
            y0 = 高 // 2 + 62
            画.create_rectangle(x0, y0, x0 + w, y0 + 3, fill=高亮, outline="")
            画.create_rectangle(x0, y0, x0 + int(w * min(1.0, t / 总毫秒)), y0 + 3,
                                fill=强调, outline="")
            if t >= 总毫秒:
                收尾()
        自 = A.动效(self)
        self._开场动效 = 自
        自.跑(16, 帧)
        # 保险：万一动画没跑到末尾（窗口没尺寸等），也要收场
        self.after(总毫秒 + 400, 收尾)

    def _加文本页(self, 标题: str) -> tk.Text:
        框 = tk.Frame(self.页, bg=编辑底)
        self.页.add(框, text=标题)
        t = tk.Text(框, bd=0, bg=编辑底, fg=文字, font=字体码(10),
                    wrap="word", padx=10, pady=8, state="disabled")
        t.pack(fill="both", expand=True, side="left")
        sb = ttk.Scrollbar(框, orient="vertical", command=t.yview)
        sb.pack(side="right", fill="y")
        t.configure(yscrollcommand=sb.set)
        return t

    def _加容器页(self, 标题: str) -> tk.Frame:
        """加一个**空白的 tab**（用来放互动控件，不是文本）。"""
        框 = tk.Frame(self.页, bg=背景)
        self.页.add(框, text=标题)
        return 框

    def _设文本(self, 控件: tk.Text, 内容: str):
        控件.configure(state="normal")
        控件.delete("1.0", "end")
        控件.insert("1.0", 内容)
        控件.configure(state="disabled")

    def _刷新环境标签(self):
        缺 = [名 for 名, 好, _ in M.体检() if not 好]
        self.环境标签.configure(
            text=("Python 那几条线现在就能用  ·  " +
                  ("其它线缺：" + "、".join(缺) if 缺 else "全部工具链就绪  ")))

    # ══════════════════════ 课程树 ══════════════════════
    #: 列表分组 —— 作者要求把**专题**单独归一类。
    #  ⚠ 分组不是"好看"，是因为 AI / 嵌入式 / 树莓派**不是编程语言** ——
    #    把它们和 Python / C# 并列，学员会以为"树莓派"是一门语言。
    #  ⚠ 每组内部按"从哪门开始学"排，不是字典序：
    #    Python 第一（零依赖、开箱即用），编译型的排后面。
    _语言分组 = (
        ("编程语言", ("python", "c", "cpp", "java", "csharp")),
        ("专题",     ("ai", "embedded", "raspi")),
        ("动手做",   ("enginecourse", "games")),
    )

    def _填语言列表(self, 只看游戏: bool = False):
        """把**所有编程语言**直接罗列出来（作者要求）。

        ⚠ 层次：**第一层就是语言**，点开才是课。
          原来第一层是"轨道分组"，学员得先理解"轨道"这个概念才能找到课。
          少一层之后，看到的就是「Python / C# / C++ / Java / C / 人工智能 …」——
          这正是"我想学哪门语言"这个问题的答案。

        ⚠ 编译型四条线仍然**后台补**（它们要起子进程，各约 1.5 秒）：
          先把能立刻显示的语言列出来，界面秒开；编译型的随后出现。
          （这里是启动慢的真凶：实测构造整个界面含课程树要 6.48 秒，
            而命令行只要 0.09 秒 —— 因为 CLI 不预扫全部课程。）
        """
        self.树.delete(*self.树.get_children())
        self._树到课 = {}
        self._待填父节点 = {}

        if 只看游戏:
            # 「游戏编写」入口：直接展开那一组，不再问一遍"挑哪个"
            组 = (("游戏编写", ("enginecourse", "games")),)
            self.语言标题.configure(text="游戏编写")
        else:
            组 = self._语言分组
            self.语言标题.configure(text="选择编程语言")

        #: ⚠ 两个入口用的是**同一个界面**（同三栏、同编辑器），
        #:   差别只在左边列出什么。所以必须有一句话把"这是哪条路"
        #:   说清楚 —— 不然学员会以为自己点错了。
        try:
            self.定位字.configure(
                text=("想让屏幕上有东西动起来 —— 从这些小游戏改起"
                      if 只看游戏
                      else "按语言挑，一课只讲一个知识点"))
        except Exception:
            pass

        for 组名, 键们 in 组:
            键们 = [k for k in 键们 if k in M.轨道]
            if not 键们:
                continue
            # ⚠ 同一组内如果只有一条线，就不建分组节点 —— 少一层点击
            分组节点 = None
            if len(键们) > 1 or not 只看游戏:
                分组节点 = self.树.insert("", "end", text=f"  {组名}",
                                        open=(组名 in ("编程语言", "游戏编写")))
            for 键 in 键们:
                # ⚠⚠ **先判这条线是哪一类**，再看它有没有课 ——
                #   我把这两个条件写在一起过，结果是：
                #   即时轨道里"没有课"的线（比如空目录）被当成编译型去占位，
                #   `_待填父节点` 里混进了不该有的键。实测就是这么红的。
                if 键 in M.编译型轨道:
                    显示, 开箱, 说明 = M.轨道[键]
                    父 = self.树.insert(分组节点 if 分组节点 else "", "end",
                                      text=f"    {显示}（{说明}）· 载入中…")
                    self._待填父节点[键] = 父
                elif 键 in M.即时轨道:
                    # ⚠ 它在这条线没有课时返回 0，别拿 0 当 item id 用
                    节点 = self._插轨道(键, 分组节点=分组节点)
                    if 只看游戏 and 节点:
                        self.树.item(节点, open=True)
            # ⚠⚠ 这里曾经有一句 `self._待填父节点[键] = 父`（在 for **外面**）——
            #   它是改造前的残留，作用是把**每一门**语言都登记成"待填"，
            #   于是即时轨道也被当成编译型、树的层次全乱。
            #   实测症状：Python 展开后有 20 个子节点、顶层没有分组。
            #   ⇒ 删掉。**循环里改结构时，最容易漏掉的就是"循环外那半句"**。

    # ══════════════════════ 进度（"走过多少"）══════════════════════

    def _课号(self, k: M.课) -> str:
        """课在进度里的键 —— **轨道 + 课号**。

        ⚠ 光用课号不行：五门语言都有 `00`，混在一起会互相顶掉。
        """
        return f"{k.轨道}/{k.编号}"

    def _刷进度(self, 动画: bool = False):
        """重算并显示进度。`动画=True` 时让条**长过去**（读完一课回来时用）。

        ⚠⚠ **载入没完成之前不显示百分比。**

          两个原因，都是实测撞出来的：

          ① **分母会变** ⇒ 进度条会倒退。
             编译型四条线是后台补的（各约 1.5 秒），所以分母一开始是
             76（只有即时的那些），补完之后变成 240。学员会看到：

                 学过 1/76 课 · 1%     ← 刚进来
                 学过 1/240 课 · 0%    ← 编译型载入之后，**条缩回去了**

             进度条倒退是最伤的 —— 它传达的是"我白干了"。

          ② **不能为了拿总数去问编译型**。
             我第一版 `_总数与已载()` 里 `sum(len(M.发现(键)) for 键 in M.轨道)`
             —— 那会把编译型四条线全问一遍（每条起子进程约 1.5 秒）。
             测试当场红了：

                 FAIL: test_构造界面不阻塞在编译型上
                 AssertionError: Lists differ: ['csharp', 'cpp', 'java', 'c'] != []

             那条测试**就是专门守这个的**（"构造界面时问了编译型课程线
             ⇒ 那就是启动慢的原因"，实测整个界面 6.48 秒 vs 命令行 0.09 秒）。
             我为了修 ①，把启动慢的问题又引回来了 —— **测试抓住了**。

          ⇒ 现在的做法：**载入全部完成之后再显示百分比**。
            在那之前用**不确定进度**（来回滑）+「还在载入」，示意"在忙"。
            条只从"滑动"变成"填充"，**不会倒退**。
        """
        总 = getattr(self, "_总课数", None)
        宽 = max(40, self.进度画布.winfo_width())

        if not 总:
            #: 还在载入 ⇒ 不确定进度（来回滑），不显示百分比
            if self._进度条 is None:
                self._进度条 = A.进度条(self.进度画布, 宽)
                self._进度条.开始()
            elif isinstance(self._进度条, A.定值进度条):
                #: 面板被拖动过 ⇒ 按当前宽度重建（否则条宽是旧的，会露馅）
                self.进度画布.delete("all")
                self._进度条 = A.进度条(self.进度画布, 宽)
                self._进度条.开始()
            过 = sum(1 for k in self._树到课.values()
                    if self._课号(k) in self.读过)
            self.进度字.configure(text=f"  学过 {过} 课 · 还在载入课程…")
            return

        过 = sum(1 for k in self._树到课.values()
                if self._课号(k) in self.读过)
        比 = 过 / 总 if 总 else 0.0
        if not isinstance(self._进度条, A.定值进度条) or self._进度条.宽 != 宽:
            self.进度画布.delete("all")
            self._进度条 = A.定值进度条(self.进度画布, 宽)
        self._进度条.设(比, 动画=动画)
        #: ⚠ 数字**滚动**过去，不是跳过去 ——
        #:   "看到它在涨"比数字本身更有激励作用（见 anim.数字滚动 的注释）
        上次 = getattr(self, "_上次过", 过)
        self._上次过 = 过
        if not hasattr(self, "_进度滚"):
            self._进度滚 = A.数字滚动(self.进度字,
                                    lambda v: self.进度字.configure(
                                        text="  学过 %d/%d 课 · %d%%"
                                             % (v, 总, int(round(v * 100.0 / 总)))))
        self._进度滚.到(上次, 过, 毫秒=460)

    def _载入完毕(self):
        """所有课程线都载完了 ⇒ 现在总课数确定了，可以显示百分比了。

        ⚠ 总课数 = **树上已挂的课 + 还没挂的**。而"还没挂的"到这一刻是 0，
          所以直接数树就行 —— **不需要去问编译型**（那正是启动慢的原因）。
        """
        if getattr(self, "_总课数", None):
            return
        self._总课数 = len(self._树到课)
        self._刷进度(动画=True)
        #: ⚠ 课程线刚补完时，左边那一大片树是**一次性冒出来**的 ——
        #:   那正是"塑料感"的来源。让它们**依次淡入**。
        self._淡入树()

    def _标读完(self, k: M.课):
        """在树上给这一课加个 ✓，并刷新进度。"""
        for 节点, 课 in self._树到课.items():
            if 课 is k:
                try:
                    self.树.item(节点, text=f"    ✓ {k.显示()}")
                except tk.TclError:
                    pass
                break
        self._刷进度(动画=True)

    def _淡入树(self, 只看: str | None = None):
        """让课程树的可见行**依次淡入**。

        ⚠ 为什么值得做：左边那棵树是**一次性全部出现**的 ——
          没有任何东西在动，大脑读到的就是"一张静态图"，
          那正是"塑料感"。错开 20ms 依次出现，同一张图就"活了"。

        ⚠⚠ 三条实测得出的约束：
          · **只动可见的那些**（`see()` 过的、`open=True` 的分支下）——
            219 行全动的话，等它演完要 4 秒，那就是负担了。
          · **总预算 450ms 封顶**：行多了延迟自动缩小（见 anim.淡入）。
          · **`after` 要能取消**：快速连点课程时，上一轮的待播回调
            会打在已重画的树上 ⇒ 静默 `TclError`。`anim.淡入.停()` 负责清。
        """
        if not hasattr(self, "_树淡入"):
            self._树淡入 = A.淡入(self.树)
        组 = []
        for 节点 in self.树.get_children(""):
            组.append(节点)
            组.extend(self.树.get_children(节点))
        if not 组:
            return
        #: ⚠ 用"逐行改前景色"的做法对 Treeview 不适用（它没有 tag）。
        #:   ⇒ 换成**依次 `see()`** —— 视觉上是"清单一行行滚出来"，
        #:     而且顺带把焦点带到当前课，一举两得。
        #:     （Tk 的 Treeview 没法做颜色插值，这是能拿到的最接近的效果。）
        for i, 节点 in enumerate(组):
            def 到(n=节点):
                try:
                    self.树.see(n)
                except tk.TclError:
                    pass
            try:
                self.树.after(min(420, i * 14), 到)
            except tk.TclError:
                pass

    def _换演示(self, k: M.课):
        """换上这一课的**互动演示**（可播放的代码）。

        ⚠⚠ 这是"课程只有文字太干"的正解 —— 不是把文字排得好看，
          而是**让程序执行过程可见**：按一下，看到跑到哪一行、
          变量怎么变、走了哪条分支。

        ⚠⚠ 两条必须守住的：
          · **演示是额外材料，不替换也不改写课程正文**（作者的硬要求）。
            它单独成块、标着「动手看看」，和正文分得清清楚楚。
          · **没配演示的课就不显示这一块** —— 不做装饰性的假互动。
            纯语法课（f-string 怎么写）执行过程没有额外信息，
            加了是噪音。
        """
        #: 先收掉上一课的（它们是有状态的控件，留着会堆积）
        if getattr(self, "当前演示", None) is not None:
            try:
                self.当前演示.停()
                self.当前演示.框.destroy()
            except Exception:
                pass
            self.当前演示 = None

        #: 只给 **python** 线配了演示（其他四条线的代码不能在这个进程里跑）
        对 = DEMO.取(k.编号) if k.轨道 == "python" else None
        if not 对:
            return
        题, 码 = 对
        try:
            self.当前演示 = FL.流程图(self.演示位, 码, 题)
            self.当前演示.包()
        except Exception as e:
            #: ⚠ 演示挂了**不能影响看课** —— 课程正文才是主体
            self.当前演示 = None
            try:
                tk.Label(self.演示位, text="（这一课的演示没起来：%s）" % e,
                         bg=背景, fg=更暗文字,
                         font=字体界面(T.字_微)).pack(anchor="w")
            except Exception:
                pass

    def _开始停留计时(self, k: M.课):
        """进一课之后开始计时；**停留够久**才算读过。

        ⚠⚠ 为什么不能"选中就算读过"：用键盘上下键扫一遍树，
          219 课会全部标成读过 —— 那个进度就是假的。
          「读过」是唯一的进度信号，它至少该表示"真的看过一会儿"。

        ⚠ 8 秒：比最短的一课（约 3 分钟）短得多，正常读一定会超过；
          而狂按方向键一定到不了。
        """
        if self._停留计时:
            try:
                self.after_cancel(self._停留计时)
            except Exception:
                pass
            self._停留计时 = None
        if self._课号(k) in self.读过:
            return                    #: 已经读过了，不用再计

        def 到点():
            self._停留计时 = None
            #: ⚠ 到点时**再确认一次**当前看的还是这一课
            #:   （8 秒里学员可能已经翻到别的课了）
            if self.当前课 is not k:
                return
            号 = self._课号(k)
            if P.记(号):
                self.读过 = P.读()
                self._标读完(k)

        self._停留计时 = self.after(8000, 到点)

    def _插轨道(self, 键: str, 分组节点: str | None = None):
        """把一条课程线插进树。

        参数 `分组节点` 是**上级分组的 item id**（`编程语言` / `专题` / `动手做`），
        不是"要复用的那个节点" —— 传了就**在它下面新建**一个语言节点。

        ⚠⚠ 这里踩过一个很隐蔽的坑，值得留着：
          原来的签名是 `父`，而它最早是为**后台补课**写的：
          后台先占一个"· 载入中…"的节点，补课时**改写那个节点的文字**。
          我把它复用成"分组节点"之后，`else` 分支就把**分组节点的文字
          改成了语言名** ——
              建分组节点 I001 ['编程语言']
              插完 Python 之后顶层变成 ['Python 基础（16 课…）']
          于是分组节点看起来"消失"了，而课全挂在它下面：
              Python 展开后有 20 个子节点、顶层没有「编程语言」。
          我查了七八轮才定位到，因为**代码看起来完全合理**。

        ⇒ 教训：**一个参数如果承担两种语义，早晚会撞。**
          这里拆成两个明确的入口：
            `_插轨道(键)`                     —— 建到顶层
            `_插轨道(键, 分组节点=某 id)`      —— 建到分组下面
            后台补课改用 `_填占位(键, 节点)`   —— 那才是"改写已有节点"。
        """
        显示, 开箱, 说明 = M.轨道[键]
        cs = M.发现(键)
        if not cs:
            return 0
        标 = 显示 if 开箱 else f"{显示}（{说明}）"
        节点 = self.树.insert(分组节点 if 分组节点 else "", "end",
                            text=f"  {标}", open=(键 == "python"))
        for k in cs:
            #: ⚠ 读过的课前面加 ✓ —— 和进度条是**同一件事的两种呈现**：
            #:   条给"走了多远"，✓ 给"具体哪几课"
            勾 = "✓ " if self._课号(k) in self.读过 else ""
            子 = self.树.insert(节点, "end", text=f"    {勾}{k.显示()}")
            self._树到课[子] = k
        self._刷进度()
        return 节点

    def _填占位(self, 键: str, 节点: str) -> int:
        """把后台补来的课填进**已有的占位节点**（改文字 + 挂课）。

        ⚠ 与 `_插轨道` 分开：这个函数**会改写传入节点的文字**，
          因为那个节点本来就是为这门课占的位。
        """
        显示, 开箱, 说明 = M.轨道[键]
        cs = M.发现(键)
        self.树.item(节点, text=f"  {显示}（{说明}）")
        for k in cs:
            勾 = "✓ " if self._课号(k) in self.读过 else ""
            子 = self.树.insert(节点, "end", text=f"    {勾}{k.显示()}")
            self._树到课[子] = k
        self._刷进度()
        return len(cs)

    def _后台载入编译型(self):
        """在后台线程里逐条发现编译型课程，发现一条就填一条。

        ⚠ 全程**不碰 Tk**：后台线程只算数据，结果丢进队列；
          真正动控件的是主线程的 `_收任务`。
          （Tkinter 的控件调用必须在主线程 —— 踩过 `main thread is not in main loop`。）
        """
        self.旋转.开始()
        self.状态.configure(text="正在载入编译型课程（C# / C++ / Java / C）…")

        def 跑():
            for 键 in M.编译型轨道:
                try:
                    n = len(M.发现(键))
                    self._任务队列.put(("课程线", 键, n))
                except Exception as e:
                    self._任务队列.put(("课程线", 键, f"载入失败：{type(e).__name__}"))
            self._任务队列.put(("课程线完毕", None, None))
        threading.Thread(target=跑, daemon=True).start()

    def _收课程线(self, 键: str, 结果) -> None:
        """主线程：把后台发现的课程线填进树。"""
        父 = self._待填父节点.pop(键, None)
        if 父 is None:
            return
        if isinstance(结果, int):
            n = self._填占位(键, 父)
            if n == 0:
                self.树.item(父, text=f"  {M.轨道[键][0]}（没有课程）")
        else:
            self.树.item(父, text=f"  {M.轨道[键][0]}（{结果}）")

    def _选课(self, _e=None):
        sel = self.树.selection()
        if not sel:
            return
        k = self._树到课.get(sel[0])
        if k is None:
            return          # 点的是课程线那一层
        if self.脏 and not messagebox.askyesno(
                "还没保存", "现在的改动还没保存，要丢掉吗？"):
            return
        self.当前课 = k
        #: ⚠ 选上课就把空状态收起来（它盖在代码区上面）
        try:
            self.空状态.place_forget()
        except Exception:
            pass
        self.编辑标题.configure(text=k.显示())
        #: 副标题给**上下文**：哪条线、答题文件叫什么、第几课
        #: ⚠ 大字标题只放课名；这些杂项放副标题，不然标题那一行太挤
        try:
            总 = len(self._树到课)
            第几 = list(self._树到课.values()).index(k) + 1 if k in                    list(self._树到课.values()) else 0
            位置 = ("· 第 %d/%d 课" % (第几, 总)) if 第几 and 总 else ""
            self.课副标题.configure(
                text="%s  %s  ·  %s" % (k.轨道, 位置, k.答题文件.name))
        except Exception:
            pass
        LV.渲染(self.正文, M.看课(k))
        #: ⚠ 正文**依次淡入**（分节 → 正文 → 代码）。
        #:   这是"课程部分太干"最直接的解药：同一份文字，
        #:   一次性倒出来 vs 逐段浮现，观感是两回事。
        try:
            if not hasattr(self, "_正文淡入"):
                self._正文淡入 = A.淡入(self.正文)
            self._正文淡入.停()
            self._正文淡入.跑(self.正文,
                           ["标题", "节", "小标题", "正文", "步骤", "码", "坑"],
                           底色=编辑底)
        except Exception:
            pass
        self._设文本(self.结果, "")
        self.代码.delete("1.0", "end")
        self.代码.insert("1.0", M.读答案(k))
        self.代码.edit_modified(False)
        self.脏 = False
        self._重高亮()
        self._画行号()
        self.状态.configure(text=f"{k.轨道} · {k.答题文件.name}")
        #: ⚠ 进课之后开始"停留计时"—— 够久才算读过（见 _开始停留计时）
        self._开始停留计时(k)
        self._换演示(k)
        #: ⚠ 有演示就**自动切到演示页** —— 学员来这一课最该看到的是
        #:   "程序怎么跑"，而不是先读一遍文字。文字随时能切回去。
        try:
            self.页.select(2 if self.当前演示 is not None else 0)
        except Exception:
            pass

    # ══════════════════════ 编辑器：行号 + 高亮 ══════════════════════
    def _滚(self, *a):
        self.代码.yview(*a)
        self._画行号()

    def _画行号(self):
        try:
            首 = int(self.代码.index("@0,0").split(".")[0])
            末 = int(self.代码.index(f"@0,{self.代码.winfo_height()}").split(".")[0])
        except Exception:
            return
        self.行号.configure(state="normal")
        self.行号.delete("1.0", "end")
        self.行号.insert("1.0", "\n".join(str(i) for i in range(首, 末 + 1)))
        self.行号.configure(state="disabled")

    def _重高亮(self):
        """极简高亮：注释 / 字符串 / 数字 / 关键字 / 函数定义。

        ⚠ 刻意**不做完整词法分析** —— 那要写状态机，而且一旦有 bug 会把代码
          上成花的（学员正在学，颜色错了比没颜色更误导）。
          这里只求"大致看得清"，用行内正则，错了也不影响代码本身。
        """
        if self.当前课 is None:
            return
        代码 = self.代码.get("1.0", "end-1c")
        关键字集 = 关键字C if self.当前课.轨道 in ("cpp", "c") else 关键字
        self.代码.tag_remove("kw", "1.0", "end")
        self.代码.tag_remove("cm", "1.0", "end")
        self.代码.tag_remove("st", "1.0", "end")
        self.代码.tag_remove("nu", "1.0", "end")
        self.代码.tag_configure("kw", foreground=T.码_关键字)
        self.代码.tag_configure("cm", foreground=T.码_注释)
        self.代码.tag_configure("st", foreground=T.码_字符串)
        self.代码.tag_configure("nu", foreground=T.码_数字)
        import re
        for i, 行 in enumerate(代码.split("\n"), 1):
            # 注释（# 或 //）
            m = re.search(r"(#|//).*$", 行)
            有无注释 = m is not None
            if m:
                self.代码.tag_add("cm", f"{i}.{m.start()}", f"{i}.{len(行)}")
            有效 = 行[:m.start()] if 有无注释 else 行
            for mm in re.finditer(r'"[^"]*"|\'[^\']*\'', 有效):
                self.代码.tag_add("st", f"{i}.{mm.start()}", f"{i}.{mm.end()}")
            for mm in re.finditer(r"\b\d+(\.\d+)?\b", 有效):
                self.代码.tag_add("nu", f"{i}.{mm.start()}", f"{i}.{mm.end()}")
            for mm in re.finditer(r"\w+", 有效):
                if mm.group(0) in 关键字集:
                    self.代码.tag_add("kw", f"{i}.{mm.start()}", f"{i}.{mm.end()}")

    def _改动(self, _e=None):
        if self.代码.edit_modified():
            self.脏 = True
            self.代码.edit_modified(False)
            # ⚠ 只有**真的和盘上不一样**才覆盖状态栏。
            #   实测踩到：`点保存()` 先设了「已保存：xxx」，紧接着 Text 的
            #   `<<Modified>>` 事件把它盖成「有未保存的改动」——
            #   学员看到的是「我刚按了保存，它却说没保存」。
            #   （程序化 `insert` 也会触发这个事件，所以不能只靠"谁后触发"。）
            if self._与盘上不同():
                self.状态.configure(text="有未保存的改动")

    def _与盘上不同(self) -> bool:
        """编辑器内容与磁盘上的答案文件是否不一致（用于状态栏措辞）。"""
        if self.当前课 is None:
            return False
        try:
            文件 = M._答案文件(self.当前课)
            if not 文件.is_file():
                return True
            盘上 = 文件.read_text(encoding="utf-8", errors="replace")
        except OSError:
            return True
        return self.代码.get("1.0", "end-1c").rstrip("\n") != 盘上.rstrip("\n")

    # ══════════════════════ 动作 ══════════════════════
    def _要课(self) -> M.课 | None:
        if self.当前课 is None:
            messagebox.showinfo("先选一课", "先在左边点一课。")
            return None
        return self.当前课

    def 点保存(self):
        k = self._要课()
        if not k:
            return
        语 = M.保存答案(k, self.代码.get("1.0", "end-1c"))
        self.脏 = False
        self.状态.configure(text=语)

    def 点运行(self):
        k = self._要课()
        if not k:
            return
        self._后台("正在运行…", lambda: M.运行(k, self.代码.get("1.0", "end-1c")))

    def 点检查(self):
        k = self._要课()
        if not k:
            return
        if not k.开箱即用 and not M.有dotnet():
            self._设文本(self.结果, M._运行课程程序(k.工程, [])[1] if k.工程 else "需要 .NET SDK")
            self.页.select(1)
            return
        self._后台("正在检查（编译型课程第一次会慢十几秒）…",
                 lambda: M.检查(k, self.代码.get("1.0", "end-1c")))

    def 点参考答案(self):
        k = self._要课()
        if not k:
            return
        if not messagebox.askyesno("先自己写",
                                   "参考答案会直接给你答案。\n\n"
                                   "抄一遍不如写错一遍学得多 —— 确定要看吗？"):
            return
        self.页.select(1)
        self._后台("正在取参考答案…", lambda: M.参考答案(k))

    def 点打开文件夹(self):
        k = self._要课()
        if not k:
            return
        目录 = k.答题文件.parent if not k.答题文件.is_dir() else k.答题文件
        try:
            开目录(目录)
        except Exception as e:
            self.状态.configure(text=f"打不开文件夹：{e}（路径是 {目录}）")

    # ══════════════════════ 后台任务 ══════════════════════
    def _后台(self, 提示: str, 活):
        """把耗时活儿放线程里跑 —— 不然窗口假死，学员会以为程序坏了。"""
        if self._忙:
            self.状态.configure(text="上一个任务还没跑完…")
            return
        self._忙 = True
        self.状态.configure(text=提示)
        self.旋转.开始()                      # ← 转起来，别让学员以为卡死
        self.configure(cursor="watch")

        def 跑():
            try:
                文 = 活()
            except Exception as e:          # 任何异常都要变成屏幕上的字，不能让它吞掉
                文 = f"出错了（{type(e).__name__}）：{e}"
            self._任务队列.put(文)
        threading.Thread(target=跑, daemon=True).start()

    def _收任务(self):
        try:
            while True:
                项 = self._任务队列.get_nowait()
                # ⚠ 队列里现在有两种消息：普通结果（字符串）与课程线通知（元组）。
                #   不区分的话，课程线通知会被当成"检查报告"塞进结果页。
                if isinstance(项, tuple) and 项 and 项[0] == "课程线":
                    self._收课程线(项[1], 项[2])
                    continue
                if isinstance(项, tuple) and 项 and 项[0] == "课程线完毕":
                    self.旋转.停()
                    self.状态.configure(text="就绪")
                    #: ⚠ 到这一刻总课数才确定 ⇒ 进度条从"不确定"切成"百分比"
                    #:   （见 _刷进度 的注释：早显示会让条**倒退**）
                    self._载入完毕()
                    continue
                文 = 项
                self._设文本(self.结果, 文)
                self.页.select(1)
                self._忙 = False
                self.旋转.停()                # ← 停了再弹结果
                self.configure(cursor="")
                self.状态.configure(text="完成")
                self._弹结果徽标(文)
        except queue.Empty:
            pass
        self.after(80, self._收任务)

    #: 报告/输出里表示"通过"的符号
    #  ⚠ 实测踩到：我只写了 `✓`，而课程报告实际用 **`√`**（另一处用 `✅`）
    #    ⇒ 徽标永远不弹。**判据符号要和实际产出一致**，
    #      不能凭"我记得是哪个勾"。
    通过符号 = ("√", "✓", "✔", "✅", "通过")
    #: 表示"没过"的符号
    不通过符号 = ("✗", "✕", "✘", "❌", "×", "没能通过", "编译失败")

    def _弹结果徽标(self, 文: str):
        """在结果页顶部弹一个 ✓ / ✗ —— 让"过没过"一眼看到。

        ⚠ 只在**看起来像检查报告**时才弹：
          单纯"看参考答案"或"运行输出"不该被判成成功/失败
          （学员只是看看，不是考试）。
          判据取"报告里有没有通过/不通过的符号"。
        """
        if self.判定画布 is None:
            return
        有过 = any(s in 文 for s in self.通过符号)
        有否 = any(s in 文 for s in self.不通过符号)
        if not (有过 or 有否):
            return                           # 不是检查报告，不弹
        成功 = 有过 and not 有否
        self.结果徽标.显示(成功, "全过" if 成功 else "还要改")

    def _关闭时问(self):
        if self.脏 and not messagebox.askyesno("还没保存", "有改动没保存，确定退出吗？"):
            return
        self.destroy()


class contextlib_suppress:
    """`contextlib.suppress` 的最小替代（免得为一行 import 整个模块）。"""
    def __enter__(self):
        return self
    def __exit__(self, *a):
        return True


def main() -> int:
    """GUI 入口。⚠ 没有显示环境时给一句人话，不要抛 traceback。"""
    try:
        界面().mainloop()
        return 0
    except tk.TclError as e:
        print("打不开图形界面：" + str(e))
        print("（如果是没有屏幕的环境，用命令行：HybridEngine-light.exe 帮助）")
        return 1
