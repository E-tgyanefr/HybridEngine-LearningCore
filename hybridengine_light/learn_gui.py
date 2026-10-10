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
        self._建布局()
        self.bind("<F5>", lambda e: self.点运行())
        self.bind("<Control-s>", lambda e: self.点保存())
        self.protocol("WM_DELETE_WINDOW", self._关闭时问)
        self.after(80, self._收任务)
        self._填课程树()
        self._开场动画()

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

    def _建布局(self):
        # 顶栏
        顶 = tk.Frame(self, bg=面板, height=44)
        顶.pack(fill="x", side="top")
        顶.pack_propagate(False)
        tk.Label(顶, text="  HybridEngine-light", bg=面板, fg=文字,
                 font=("Segoe UI", 12, "bold")).pack(side="left", pady=8)
        self.环境标签 = tk.Label(顶, text="", bg=面板, fg=暗文字, font=("Segoe UI", 9))
        self.环境标签.pack(side="right", padx=12)
        self._刷新环境标签()

        # 三栏
        主体 = tk.PanedWindow(self, orient="horizontal", bg=背景,
                             sashwidth=5, sashrelief="flat", bd=0)
        主体.pack(fill="both", expand=True)

        # 左：课程树
        左 = tk.Frame(主体, bg=面板)
        主体.add(左, width=260, minsize=180)
        tk.Label(左, text="  课程", bg=面板, fg=暗文字,
                 font=("Segoe UI", 9)).pack(anchor="w", pady=(8, 2))
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
        底 = tk.Frame(self, bg=面板, height=46)
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
    def _填课程树(self):
        self.树.delete(*self.树.get_children())
        self._树到课 = {}
        for 键, (显示, 开箱, 说明) in M.轨道.items():
            cs = M.发现(键)
            if not cs:
                continue
            标 = 显示 if 开箱 else f"{显示}（{说明}）"
            父 = self.树.insert("", "end", text=f"  {标}", open=(键 == "python"))
            for k in cs:
                子 = self.树.insert(父, "end", text=f"    {k.显示()}")
                self._树到课[子] = k

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
            self.状态.configure(text="有未保存的改动")

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
                文 = self._任务队列.get_nowait()
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

    #: 报告里出现这些字样 ⇒ 判为"没过"（用于结果徽标）
    失败字样 = ("✗", "没能通过", "编译失败", "出错", "Error", "Traceback",
              "没有找到", "应该", "失败")

    def _弹结果徽标(self, 文: str):
        """在结果页顶部弹一个 ✓ / ✗ —— 让"过没过"一眼看到。

        ⚠ 只在**看起来像检查报告**时才弹：
          单纯"看参考答案"或"运行输出"不该被判成成功/失败
          （学员只是看看，不是考试）。判据保守取"报告里有没有 ✓/✗"。
        """
        if self.判定画布 is None:
            return
        有勾 = "✓" in 文
        有叉 = "✗" in 文
        if not (有勾 or 有叉):
            return                           # 不是检查报告，不弹
        成功 = 有勾 and not 有叉
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
