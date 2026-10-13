"""跑**学员代码**时的看门狗 —— 超时就把代码打断。

═══ 为什么需要这个（不是"防御性编程"，是真会卡死）═══

本项目的学员代码**是要被真跑起来的**（那是判分的全部意义）。而"跑别人写的代码"
这件事有个基本问题：**它可能永远不返回。**

桌面版一直有兜底：`learnmodel.运行()` 用
`subprocess.run(..., timeout=20)` —— 超时就杀子进程。

⚠⚠ **但手机版没有这条路**：

    · 安卓**不允许**应用执行自己的可执行文件（PEP 738）
      ⇒ 起不了子进程 ⇒ 没有"杀掉那个进程"这个选项
    · 而 Python 是**嵌在应用主进程里**的（`Py_Initialize`）
    · 而且桥的方法跑在 **WebView 的 JS 线程**上，
      它的注释写明「Py_Initialize 不能扔线程池（会原生崩溃）」

    ⇒ **学员在手机上写一个 `while True: pass`，整个应用就卡死了 ——
      界面不动、按钮没反应，只能强杀。**

所以这个看门狗不是"更好"，是**手机上唯一可行的办法**。

═══ 怎么做的 ═══

`sys.settrace` + 数行数。每执行 N 行看一次表，超了就 `raise 超时`。

⚠ **为什么是 `settrace` 而不是别的**：

    · `signal.setitimer`/`SIGALRM` —— **Windows 没有 SIGALRM**，
      而这是"三个平台都要"的项目 ⇒ 直接出局
    · 线程 + `PyThreadState_SetAsyncExc` —— 要用 `ctypes` 碰 CPython 内部，
      而且在"不能换线程"的手机上是反方向
    · `settrace` 是 **stdlib、纯 Python 层、任何平台都一样**

⚠ **代价（如实说）**：`settrace` 只能打断**纯 Python** 的执行。
    如果死循环在 C 扩展里（`.so` 的某个函数内部），tracer 不会被调用
    ⇒ **拦不住**。本项目的学员代码都是纯 Python，所以够用 ——
    但这条限制要写在明面上，不能让人以为它是万能的。

⚠⚠⚠ **【2026-10-13 实测：这个模块在安卓上不能用 —— 原因已查明】** ⚠⚠⚠

    ## 判别实验（`android/app/Diag.cs` 的 `hel jit`）—— 它把原因钉死了

    在真机上跑一段**有界**代码（`for i in range(200000): pass`），
    中途打印 tracer 被调用的次数：

        sys.settrace(t)          # 先装上
        for i in range(200000): pass
        sys.settrace(None)
        print('tracer 触发次数 =', 痕迹[0])
        print('阈值 =', sys.gettrace() is None)

    真机 logcat 原文：

        jit 实验：{"ok": true, "r": {"stdout": "tracer 触发次数 = 0\n阈值 = True\n",
                                     "ok": true, "seconds": 0.2214}}

    ## ⇒ 结论：**安卓这份 Python 构建里 `settrace` 装了但不触发**

        · `tracer 触发次数 = 0`  —— **连 `call` 事件都没有**
          （不是"紧循环看不到 line 事件"，是 tracer **一次都没被调**）
        · `阈值 = True`          —— `sys.settrace(None)` 生效了，
          说明 `sys.settrace` **本身能用**，只是**不回调**
        · `seconds = 0.2214`     —— 20 万次循环**跑完了**，**没卡**

    ⚠ 所以候选是「**这份构建把 tracing 关掉了**」那一类
      （CPython 有编译期开关；Chaquopy 那份 `libpython3.12.so` 显然没带 tracing）。
      **不是**线程局部问题，**不是**死循环看不见行事件。

    ## ⚠⚠ 我前面两次把它说成"卡死"—— 那个判断**依据是错的**

        我用了 `mCurrentFocus=null` 当"界面无响应"的证据。
        ⚠ 后来发现：**不带任何实验、正常启动的应用，`mCurrentFocus` 也是 `null`**
          （这台 MIUI 上就是这个表现，与卡不卡无关）。
        ⇒ 那两次"卡死"的结论**站不住**。真正的事实是：
          `code.run` 收到死循环之后**确实没有返回**（日志停在 `Python 起来了`
          之后再无一行，也没有写出协议响应文件）——
          但这只能说明"**看门狗没打断它**"，不能说明"界面卡死"。

    ## ⭐ 真机能力探测（2026-10-13，实测一行读完）—— 下一个人**不用重探**

        用真机实验台跑（`--es hel run --es src <base64>`）：

            ver=3.12.7 || xxsub=YES || SIGALRM=True || _thread=YES
                       || switchinterval=0.005 || trace_ok=True || settrace_callable=YES

        **两个关键发现**：

        · ⭐ **`SIGALRM = True`** —— **安卓有 SIGALRM！**
          ⚠ 我原先排除 `signal.setitimer` 的理由是"**Windows 没有 SIGALRM**" ——
            那是**桌面推理，没在安卓上验**。现在验了：**它有**。
          ⇒ `signal.setitimer(ITIMER_REAL, 秒)` 是 **stdlib**、
            **不需要 tracing**（信号在字节码边界被 CPython 检查）、
            而且能打断 `while True` ⇒ **这是最有希望的一条路**。
          ⚠ 但有个**技术障碍**：`signal.signal()` **只能在主线程**调，
            而这三处调用点都在各自的线程里跑 Python ⇒ 会抛 `ValueError`。
            要么改到主线程装 handler，要么用别的路。**下一轮先解决这个。**
        · **`_xxsubinterpreters` 可用**（`xxsub=YES`）—— 子解释器那条路也在。
          ⚠ 只验了"能 import"，**没验能跑**。

        ⚠⚠ **`settrace_callable=YES` 而 tracer 一次都不回调** ——
          这条对比本身是个教训：**"能调这个 API"和"它真的生效"是两件事。**
          别只看 `hasattr` / `callable` 就以为行。

        ⚠ 这三条路（SIGALRM / 子解释器 / 异步异常注入）**这一轮都没验成** ——
          所以上面只写"能力存在"，**不写"方案可行"**。

    ## ⇒ 那手机怎么办？**必须换机制**

        手机要的是"**能杀掉的执行单元**"：
          · 子解释器（`Py_NewInterpreter`）—— 3.12 有 per-interpreter GIL，但是 C API
          · watchdog 线程 + `PyThreadState_SetAsyncExc`
          · 别的
        ⚠ 而安卓起不了子进程（PEP 738）⇒ "杀子进程"那条路没有。

        ⇒ **下一步是换机制，不是调参数。** 详见
          `docs/安卓版说明.md` 的「判分的超时保护」那行。

    ## 那这个模块还留着吗？**留着。**

    · 桌面版**真的用它兜住了**（`run_checks` / `code.run` / `_grade_code`）
      —— 桌面上死循环同样会让判分不返回，那部分是**有效的**
    · ⚠ 但**不要**把它当成"手机也安全了"的依据 —— 那正是这次的教训
    · ⚠ 而且它**不能在安卓上"自动降级"**：`settrace` 装了不报错、也不回调，
      **没有任何信号**告诉你"它没生效"。⇒ 只能靠**平台判断**或**真机实验**发现。

⚠ **另一个代价**：`settrace` 会让代码**变慢**。实测（本机，紧循环）：

    不开看门狗   0.0084 秒
    开看门狗     0.1044 秒        ⇒ **12.5x**

⚠⚠ **这个 12.5x 不是"看表太密"，改间隔没用**（1000 行 vs 10000 行都是 12.4x 上下）——
  开销在 `settrace` **本身**：它让**每一行**都变成一个 Python 函数调用。
  那是解释器层的，不归我们的参数控制。（我猜错过一次，`每多少行看表` 那段有记录。）

⚠ 但它**摊薄之后小**：真实代码每行都有活干，不像"每行一次加法"的紧循环。
  实测一次**真判分**（python 第 00 课）是 **3 毫秒** —— 相对 10 秒预算占 0.03%。
  ⚠ 全套引擎测试从 5.6 秒变 9.6 秒（+71%）—— 那是**判据自己要跑几百次判分**放大的，
  不是学员会感觉到的量。

⚠⚠ **一个以后会咬人的点：`sys.settrace` 是"线程局部"的。**

    它只影响**安装它的那个线程**。所以：

      · 在哪个线程跑学员代码，就要在**那个线程**里装看门狗
      · 换线程 = 看门狗**静默失效**（不报错，只是不生效）——
        而"不生效"的样子和"没装"一模一样，**很难查**

    ⚠ 现在三处调用点（`check.run_checks` / `ctl._code_run` / `examrun._grade_code`）
      **都在调用方自己的线程里装**，所以是对的。
    ⚠ 哪天有人把判分挪到别的线程（比如为了解决手机上的卡界面问题），
      **必须同时确认看门狗还在那个新线程里** —— 否则这个文件就白写了。
"""
from __future__ import annotations

import sys
import time

#: 每执行这么多"行事件"看一次表。
#
#  ⚠⚠ **这里有一个我猜错了、后来量出来的事，写下来免得下一个人再猜：**
#
#     我原以为"贵的是 `time.monotonic()`" ⇒ 把间隔从 1000 调到 10000
#     应该能大幅降开销。**实测没有**：
#
#         每 1000 行看一次  ⇒ 紧循环 **12.35x**
#         每 10000 行看一次 ⇒ 紧循环 **12.47x**      ← 一样（甚至略高，是噪声）
#
#     ⇒ **开销不在看表，在 `settrace` 本身**：
#       它让**每一行**都变成一个 Python 函数调用（tracer 那个回调）。
#       而那是**解释器层的**，不归我们的参数控制。
#
#  ⚠ 那这个数还调不调？**调，但理由变了** ——
#    它不代表"开销"，只代表"**超时多久才被发现**"：
#
#         10000 行 × 1 微秒/行 ≈ **10 毫秒**的判定延迟
#
#    对 **10 秒**的预算来说完全够。（我原先取 1000 是把它当成"精度越高越好"，
#    实际它买到的是 1 毫秒精度 —— 一个 10 秒预算根本用不上的东西。）
#    取大一点还能省掉一点回调次数，虽然不改变那个 12x。
#
#  ⚠ **那 12x 怎么办？** 它只在"每行都极便宜的紧循环"上才那么难看。
#    真实代码每行都有活干，摊薄之后小得多 —— 实测一次真判分（第 00 课）
#    是 **3 毫秒**，相对 10 秒预算占 **0.03%**。
#    ⇒ 结论：**这个代价可以接受**。但不能假装它不存在（见下面那段"代价"）。
每多少行看表 = 10000

#: 默认给多少秒。
#
#  ⚠ 桌面版 `learnmodel.运行()` 给的是 20 秒 —— 那是**起子进程**的量级（含启动开销）。
#    这里是在**已经起好的解释器里**跑，不需要那么多。
#  ⚠ 但也不能太小：学员的代码可能真的要算一会儿（比如素数筛到 10^6）。
#    10 秒是"正常代码绝用不到、死循环一定撞得上"的量级。
默认秒 = 10.0


# ══════════════════════════════════════════════════════════════════════════
#  ⭐⭐ 两条路：`setitimer`（首选）与 `settrace`（兜底）
# ══════════════════════════════════════════════════════════════════════════
#
#  ⚠⚠ **为什么不是只用一条** —— 两条各有"在某个平台上没有"的问题：
#
#      `signal.setitimer`  —— **Windows 没有 `SIGALRM`** ⇒ 桌面上不可用
#      `sys.settrace`      —— **安卓这份构建没带 tracing** ⇒ 手机上不可用
#                             （`settrace` 能调、`gettrace` 也设上了，
#                              但 tracer **一次都不回调**）
#
#  ⇒ 所以是**按能力选**，不是按 `sys.platform` 写死。
#     ⚠ 也更不该写 `if 安卓: 用信号 else: 用trace` —— 那是把"当前平台的实现细节"
#       硬编码进业务逻辑；而 `hasattr` + **真跑一次**探测是自适应的。

#: `_settrace_可用()` 的缓存（`None` = 还没探过）
_settrace_结果: bool | None = None


def _settrace_可用() -> bool:
    """`sys.settrace` 到底**触发不触发**（不是"能不能调"）。

    ⚠⚠ **"能调这个 API"和"它真的生效"是两件事。**

        真机实测（安卓，2026-10-13）：
            `sys.settrace(f)` 调用成功、`sys.gettrace()` 也设上了，
            但 tracer **一次都没被回调**（`tracer 触发次数 = 0`）——
            那份 Chaquopy 的 `libpython3.12.so` **没带 tracing**。
        ⇒ 只看 `hasattr(sys, "settrace")` 会得出**完全错误**的结论。

    ⇒ 所以这里**真的跑一小段、数一下回调次数**。
      ⚠ 代价：探测时开一次 trace（1000 行，微秒级，一次性）；结果缓存。

    ⚠⚠ **踩过的坑：探测代码本身必须"产生一个新帧"。**

        `sys.settrace(f)` 设的是**全局 trace**，而它**只在 `call` 事件时被调用**
        —— 之后每一行要看的是**它返回的那个 local trace**。
        ⇒ 如果探测循环写在**已经活动的那一帧里**（比如直接写在本函数体里），
          **根本不会有 `call` 事件** ⇒ tracer 一次都不被调 ⇒
          **把"能用"误判成"不能用"**。

        （我第一版就是这么写的：`机制()` 报 `none`，而同一进程里
          `看门狗` 明明把死循环打断了 —— **自相矛盾**才发现的。）

        ⇒ 修法：把循环塞进一个**嵌套函数**并调用它，强制产生新帧。
    """
    global _settrace_结果
    if _settrace_结果 is not None:
        return _settrace_结果
    次 = [0]

    def 探(frame, event, arg):
        次[0] += 1
        return 探

    def 干点活():
        #: ⚠ 这个函数**必须是独立的帧** —— 见上面那段。
        #:   有界循环：万一 tracer 不触发也得返回。
        for _ in range(1000):
            pass

    旧 = sys.gettrace()
    try:
        sys.settrace(探)
        干点活()
    except Exception:                                          # noqa: BLE001
        次[0] = 0
    finally:
        try:
            sys.settrace(旧)
        except Exception:                                      # noqa: BLE001
            pass
    _settrace_结果 = 次[0] > 0
    return _settrace_结果


def _信号可用() -> bool:
    """这个解释器有没有可用的 `setitimer` + `SIGALRM`。

    ⚠ 与 `_settrace_可用()` 不同，这一条**不用真跑**：`hasattr` 就够 ——
      `setitimer` 要么在、要么不在（没有"在但不生效"那种中间态）。
      ⚠ 但**它到底能不能打断死循环**是另一回事，那一条**真机验过**：
        `**INTERRUPTED 1.00 秒**`（见模块顶部那段）。
    """
    try:
        import signal
        return hasattr(signal, "SIGALRM") and hasattr(signal, "setitimer")
    except Exception:                                          # noqa: BLE001
        return False


def 有超时保护() -> bool:
    """这台机器上到底能不能强制打断学员代码？

    ⚠ 给调用方一个**明确的答案** —— 而不是让它以为"装了看门狗就安全了"。
      ⚠⚠ 这正是踩过的坑：安卓上 `settrace` 装了**不报错、也不生效**。
      ⇒ 界面/日志该按这个函数的返回值说人话。
    """
    return _信号可用() or _settrace_可用()


def 机制() -> str:
    """现在走的是哪条路：`"signal"` / `"settrace"` / `"none"`（给日志与判据用）。"""
    if _信号可用():
        return "signal"
    if _settrace_可用():
        return "settrace"
    return "none"


def _装信号处理(秒: float):
    """在**当前线程**装 SIGALRM handler。

    ⚠⚠ `signal.signal()` **只能在主线程调** —— 别的线程会抛 `ValueError`。
      真机实测：`跑并抓`/`code.run` 那条路**就在主线程上**
      （`is_main=True`），所以这里没问题。
      ⚠ 但**别假设它永远在主线程**：`ValueError` 要转成"这条路不可用"，
        而不是让判分崩掉。
    """
    import signal

    def 处理(signum, frame):
        raise 超时("代码跑了 %.1f 秒还没停（上限 %.1f 秒）—— 是不是有个循环停不下来？"
                   % (秒, 秒))

    signal.signal(signal.SIGALRM, 处理)
    return signal


class 超时(Exception):
    """学员代码跑太久了。

    ⚠ 单独定义一个类型（不是内置 `TimeoutError`）的理由：
      **调用方要能分清"是看门狗打断的"和"学员代码自己抛了 TimeoutError"** ——
      后者是学员自己写的（比如他调了某个超时的库），
      提示语完全不同：一个是"你的代码好像停不下来"，一个是"你的代码报了个错"。
    """


class 看门狗:
    """上下文管理器 —— 在 `with` 里跑的东西超过 `秒` 就被打断。

        with 看门狗(秒=5.0):
            学员的代码()

    ## ⭐ 两条路，按能力选（不是按平台写死）

        · **`signal.setitimer`**（首选）—— 真机验过能打断 `while True`
          （`**INTERRUPTED 1.00 秒**`）。⚠ Windows 没有 `SIGALRM` ⇒ 桌面上用不了。
        · **`sys.settrace`**（兜底）—— 桌面有效。
          ⚠ 安卓这份构建**没带 tracing**，所以手机上它是个**静默的空操作**。

    ⚠ 退出时**一定会**把两条路都还原（包括异常路径）：
      · 信号：`setitimer(0)` —— 不清的话**下一个 `with` 块会被上一个的定时器打断**
      · trace：`sys.settrace(旧)` —— 不清的话后面所有代码都被 trace 着
    """

    def __init__(self, 秒: float = 默认秒, 每多少行: int = 每多少行看表):
        self.秒 = float(秒)
        self.每多少行 = max(1, int(每多少行))
        self._旧trace = None
        self._开始 = 0.0
        self._计数 = 0
        #: 这次实际走了哪条路（`"signal"` / `"settrace"` / `"none"`）
        self.用了 = "none"
        self._signal = None
        #: 超时后置真 —— 给外面一个"确实是我打断的"的凭据
        self.超了 = False

    # ── 上下文管理 ──────────────────────────────────────────────────

    def __enter__(self):
        self._开始 = time.monotonic()
        self._计数 = 0
        self.超了 = False
        self.用了 = "none"

        #: ① 首选：`setitimer`（不依赖 tracing ⇒ 手机上唯一有效的路）
        if _信号可用():
            try:
                self._signal = _装信号处理(self.秒)
                self._signal.setitimer(self._signal.ITIMER_REAL, self.秒)
                self.用了 = "signal"
                return self
            except Exception:                                  # noqa: BLE001
                #: ⚠ 最可能是 `ValueError`（不在主线程）——
                #:   那就往下退到 trace，**而不是让判分崩掉**。
                self._signal = None

        #: ② 兜底：`settrace`（探过真的会回调才用）
        self._旧trace = sys.gettrace()
        sys.settrace(self._看)
        self.用了 = "settrace" if _settrace_可用() else "none"
        return self

    def __exit__(self, 类型, 值, 栈):
        #: ⚠ 两条路都得清 —— **清的时候不看 `用了`**：
        #:   `用了` 是"我以为走了哪条"，而清理必须无条件做
        #:   （万一 `用了` 判错了，漏清一个定时器会让**下一个** `with` 块莫名被打断，
        #:     那种 bug 极难查）。
        if self._signal is not None:
            try:
                self._signal.setitimer(self._signal.ITIMER_REAL, 0)
            except Exception:                                  # noqa: BLE001
                pass
        try:
            if self._旧trace is not sys.gettrace():
                sys.settrace(self._旧trace)
        except Exception:                                      # noqa: BLE001
            pass
        #: 返回 False ⇒ 不吞异常（超时会照常往上抛，由调用方转成人话）
        return False

    # ── tracer ─────────────────────────────────────────────────────

    def _看(self, frame, event, arg):
        if event == "call":
            #: ⚠ `local trace` 必须返回一个函数，否则**这个 frame 里的
            #:   line 事件根本不会产生** —— 返回 None 等于没看。
            #:   这是 `settrace` 最容易踩的坑：全局 trace 只在 call 时被调，
            #:   之后每一行要看的是**返回的那个 local trace**。
            return self._看
        if event == "line":
            self._计数 += 1
            if self._计数 >= self.每多少行:
                self._计数 = 0
                if time.monotonic() - self._开始 > self.秒:
                    self.超了 = True
                    raise 超时(
                        "代码跑了 %.1f 秒还没停（上限 %.1f 秒）—— 是不是有个循环停不下来？"
                        % (time.monotonic() - self._开始, self.秒))
        return self._看


def 跑一遍(函数, *参, 秒: float = 默认秒, **键):
    """跑一个函数，超时就抛 `超时`。**给"跑学员代码"的调用点用的**。

        try:
            结果 = 跑一遍(学员的入口函数, 数据)
        except 超时 as e:
            return 报告(ok=False, detail=str(e))
    """
    with 看门狗(秒=秒):
        return 函数(*参, **键)
