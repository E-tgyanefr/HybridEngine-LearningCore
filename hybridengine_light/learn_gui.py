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

# ── 配色（深色，护眼；也少些"白板刺眼") ─────────────────────────────────
背景 = "#1e1f22"
面板 = "#2b2d30"
编辑底 = "#1e1f22"
文字 = "#d6d6d6"
暗文字 = "#8a8f98"
强调 = "#4a9eff"
好色 = "#4ec9b0"
坏色 = "#f48771"
高亮 = "#3a3d41"

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
        self._建主页()
        self.bind("<F5>", lambda e: self.点运行())
        self.bind("<Control-s>", lambda e: self.点保存())
        self.bind("<Escape>", lambda e: self.回主页())
        self.protocol("WM_DELETE_WINDOW", self._关闭时问)
        self.after(80, self._收任务)
        self._开场动画()


    # ══════════════════════ 主页（两个入口） ══════════════════════
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

        tk.Label(框, text="HybridEngine-light", bg=背景, fg=文字,
                 font=("Segoe UI", 26, "bold")).pack(pady=(0, 6))
        tk.Label(框, text="学编程，从第一行代码看到画面", bg=背景, fg=暗文字,
                 font=("Segoe UI", 11)).pack(pady=(0, 34))

        选 = tk.Frame(框, bg=背景)
        选.pack()
        self._大按钮(选, "游戏编写", "自己写一个小游戏，看得见画面", self.进游戏编写).pack(
            side="left", padx=14)
        self._大按钮(选, "课程学习", "按编程语言挑，一课只讲一个知识点", self.进课程学习).pack(
            side="left", padx=14)

        tk.Label(框, text="（想回这里随时按 Esc）", bg=背景, fg=暗文字,
                 font=("Segoe UI", 9)).pack(pady=(30, 0))

    def _大按钮(self, 父, 标题: str, 说明: str, 命令):
        """主页上的大按钮：标题大、说明小。"""
        卡 = tk.Frame(父, bg=面板, highlightthickness=1,
                     highlightbackground=高亮, bd=0)
        内 = tk.Frame(卡, bg=面板)
        内.pack(padx=34, pady=26)
        tk.Label(内, text=标题, bg=面板, fg=文字,
                 font=("Segoe UI", 17, "bold")).pack()
        tk.Label(内, text=说明, bg=面板, fg=暗文字,
                 font=("Segoe UI", 9)).pack(pady=(8, 0))
        卡.bind("<Button-1>", lambda e: 命令())
        内.bind("<Button-1>", lambda e: 命令())
        for w in 内.winfo_children():
            w.bind("<Button-1>", lambda e: 命令())
            w.configure(cursor="hand2")
        卡.configure(cursor="hand2")
        return 卡

    def _清空容器(self):
        for w in self._容器.winfo_children():
            w.destroy()

    def 回主页(self):
        """回到两个入口那一屏。

        ⚠ 学习界面**不销毁**（下次进来秒开），只是藏起来。
          但课程树里的编译型课程是后台补的，藏起来不影响它继续补。
        """
        if self.学习界面 is not None and self.学习界面.winfo_exists():
            self.学习界面.pack_forget()
        if self.主页 is not None and self.主页.winfo_exists():
            self.主页.pack(fill="both", expand=True)
            return
        self._建主页()

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
        s = ttk.Style(self)
        with contextlib_suppress():
            s.theme_use("clam")
        s.configure("Treeview", background=面板, fieldbackground=面板,
                    foreground=文字, borderwidth=0, rowheight=24)
        s.configure("Treeview.Heading", background=面板, foreground=暗文字)
        s.map("Treeview", background=[("selected", 强调)], foreground=[("selected", "#ffffff")])
        s.configure("TNotebook", background=面板, borderwidth=0)
        s.configure("TNotebook.Tab", background=面板, foreground=暗文字, padding=(12, 6))
        s.map("TNotebook.Tab", background=[("selected", 背景)], foreground=[("selected", 文字)])
        s.configure("TFrame", background=背景)

    def _建布局(self, 父):
        # 顶栏
        顶 = tk.Frame(父, bg=面板, height=44)
        顶.pack(fill="x", side="top")
        顶.pack_propagate(False)
        # ⚠ 回主页的入口要**一直在**：学员点进去之后不能迷路
        tk.Button(顶, text="← 主页", command=self.回主页, bg=面板, fg=暗文字,
                  bd=0, activebackground=高亮, activeforeground=文字,
                  font=("Segoe UI", 9), padx=8, cursor="hand2").pack(side="left", pady=8)
        tk.Label(顶, text="HybridEngine-light", bg=面板, fg=文字,
                 font=("Segoe UI", 12, "bold")).pack(side="left", pady=8)
        self.环境标签 = tk.Label(顶, text="", bg=面板, fg=暗文字, font=("Segoe UI", 9))
        self.环境标签.pack(side="right", padx=12)
        self._刷新环境标签()

        # 三栏
        主体 = tk.PanedWindow(父, orient="horizontal", bg=背景,
                             sashwidth=5, sashrelief="flat", bd=0)
        主体.pack(fill="both", expand=True)

        # 左：**语言列表**（作者要求：课程学习处直接把所有编程语言罗列出来）
        #
        # ⚠ 原来是"轨道 → 课程"两层树，第一眼看到的是一堆分组名。
        #   现在改成：第一层就是**语言**（Python / C# / C++ / Java / C ...），
        #   点一门语言才展开它的课 —— 少一层，更接近"我想学 X 语言"的想法。
        左 = tk.Frame(主体, bg=面板)
        主体.add(左, width=260, minsize=180)
        self.语言标题 = tk.Label(左, text="  选择编程语言", bg=面板, fg=暗文字,
                              font=("Segoe UI", 9))
        self.语言标题.pack(anchor="w", pady=(8, 2))
        self.树 = ttk.Treeview(左, show="tree", selectmode="browse")
        self.树.pack(fill="both", expand=True, padx=6, pady=(0, 6))
        self.树.bind("<<TreeviewSelect>>", self._选课)

        # 中：编辑器
        中 = tk.Frame(主体, bg=编辑底)
        主体.add(中, width=520, minsize=320)
        栏 = tk.Frame(中, bg=面板, height=30)
        栏.pack(fill="x")
        栏.pack_propagate(False)
        self.编辑标题 = tk.Label(栏, text="  选一课开始", bg=面板, fg=暗文字,
                             font=("Segoe UI", 9), anchor="w")
        self.编辑标题.pack(side="left", fill="x", expand=True, padx=4)
        编辑框 = tk.Frame(中, bg=编辑底)
        编辑框.pack(fill="both", expand=True)
        self.行号 = tk.Text(编辑框, width=4, padx=6, takefocus=0, bd=0,
                          bg=面板, fg=暗文字, font=("Consolas", 11),
                          state="disabled", wrap="none")
        self.行号.pack(side="left", fill="y")
        代码区 = tk.Frame(编辑框, bg=编辑底)
        代码区.pack(side="left", fill="both", expand=True)
        self.代码 = tk.Text(代码区, bd=0, bg=编辑底, fg=文字, insertbackground=强调,
                          font=("Consolas", 11), wrap="none", undo=True,
                          selectbackground=高亮, padx=8, pady=4)
        self.代码.pack(side="left", fill="both", expand=True)
        sb = ttk.Scrollbar(代码区, orient="vertical", command=self._滚)
        sb.pack(side="right", fill="y")
        self.代码.configure(yscrollcommand=lambda a, b: (sb.set(a, b), self._画行号()))
        self.代码.bind("<<Modified>>", self._改动)
        self.代码.bind("<KeyRelease>", lambda e: self._重高亮())
        self.代码.bind("<MouseWheel>", lambda e: self.after(1, self._画行号))

        # 右：说明 + 结果
        右 = tk.Frame(主体, bg=背景)
        主体.add(右, width=380, minsize=240)
        self.页 = ttk.Notebook(右)
        self.页.pack(fill="both", expand=True)
        self.正文 = self._加文本页("这一课要做什么")
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

        # 底栏按钮
        底 = tk.Frame(父, bg=面板, height=46)
        底.pack(fill="x", side="bottom")
        底.pack_propagate(False)
        for 字, 命令, 宽 in (("保存", self.点保存, 8), ("运行  F5", self.点运行, 11),
                            ("检查答案", self.点检查, 10), ("参考答案", self.点参考答案, 10),
                            ("打开答题文件夹", self.点打开文件夹, 15)):
            tk.Button(底, text=字, command=命令, bg=高亮, fg=文字, bd=0,
                      activebackground=强调, activeforeground="#fff",
                      font=("Segoe UI", 9), padx=6, width=宽,
                      cursor="hand2").pack(side="left", padx=(8, 0), pady=9)
        self.状态 = tk.Label(底, text="就绪", bg=面板, fg=暗文字,
                           font=("Segoe UI", 9), anchor="e")
        self.状态.pack(side="right", fill="x", expand=True, padx=(0, 12))
        # ── 忙碌指示：转圈圈，而不是一句死文字 ──
        # ⚠ `dotnet run` 第一次编译要十几秒。纯文字学员会以为卡死；
        #   转圈是**通用**的"还在干活"信号。
        self.忙碌画布 = tk.Canvas(底, width=22, height=22, bg=面板,
                                highlightthickness=0, bd=0)
        self.忙碌画布.pack(side="right", padx=(0, 4))
        self.旋转 = A.旋转弧(self.忙碌画布, 11, 11, r=8)

    # ══════════════════════ 启动动画 ══════════════════════
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
                           fill=文字, font=("Segoe UI", 字号, "bold"))
            # 副标题：晚 180ms 淡入（透明度靠灰度逼近）
            q = A.缩放值(min(1.0, max(0.0, (t - 180) / 500)), 0.0, 1.0)
            if q > 0.02:
                灰 = int(0x2b + (0xd6 - 0x2b) * q)
                画.create_text(宽 // 2, 高 // 2 + 22,
                               text="学编程，从第一行代码看到画面",
                               fill=f"#{灰:02x}{灰:02x}{灰:02x}",
                               font=("Segoe UI", 11))
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
        t = tk.Text(框, bd=0, bg=编辑底, fg=文字, font=("Consolas", 10),
                    wrap="word", padx=10, pady=8, state="disabled")
        t.pack(fill="both", expand=True, side="left")
        sb = ttk.Scrollbar(框, orient="vertical", command=t.yview)
        sb.pack(side="right", fill="y")
        t.configure(yscrollcommand=sb.set)
        return t

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
            self.语言标题.configure(text="  游戏编写")
        else:
            组 = self._语言分组
            self.语言标题.configure(text="  选择编程语言")

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
            子 = self.树.insert(节点, "end", text=f"    {k.显示()}")
            self._树到课[子] = k
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
            子 = self.树.insert(节点, "end", text=f"    {k.显示()}")
            self._树到课[子] = k
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
        self.编辑标题.configure(text=f"  {k.显示()}")
        self._设文本(self.正文, M.看课(k))
        self._设文本(self.结果, "")
        self.代码.delete("1.0", "end")
        self.代码.insert("1.0", M.读答案(k))
        self.代码.edit_modified(False)
        self.脏 = False
        self._重高亮()
        self._画行号()
        self.状态.configure(text=f"{k.轨道} · {k.答题文件.name}")

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
        self.代码.tag_configure("kw", foreground="#569cd6")
        self.代码.tag_configure("cm", foreground="#6a9955")
        self.代码.tag_configure("st", foreground="#ce9178")
        self.代码.tag_configure("nu", foreground="#b5cea8")
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
            os.startfile(str(目录))          # noqa: S606 —— Windows 专用，刻意
        except Exception as e:
            self.状态.configure(text=f"打不开文件夹：{e}")

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
