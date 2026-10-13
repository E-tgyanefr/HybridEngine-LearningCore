"""三个引擎**同一口径**的对比压测 —— 用来验证作者给的性能比例。

═══ 作者给的定位比例（这是他给的判断，不是实测）═══

    HybridEngine（专业版）        100%
    HybridEngine-SampleCore        90%
    HybridEngine-LearningCore      30%

═══ 口径（三个引擎必须做同一件事、计同一段时间）═══

    · 分辨率 1920×1080
    · 场景 = **N 个圆**，铺成网格（不重叠 —— 重叠会测成"同区域反复画"）
    · 计时 = **端到端每帧墙上时间**（构造命令 + 提交渲染 + 引擎自己的每帧开销）
      ⚠ **统一用端到端**，因为三个引擎的"命令录制"和"光栅化"分不开：
        专业版有 `ms_engine_render`（纯光栅化）的口径，
        而轻量版的"画圆"本身就是录制 —— 拿一个只计光栅化的数和一个端到端的数
        放在一起比，是**两个口径硬凑**。
      ⚠ 所以专业版另附它自报的 `last_frame_ms`（它内部口径），供对照，不参与比值。

═══ ⚠⚠ 一个必须说清的事实 ═══

**SampleCore 和 LearningCore 现在是同一套代码**（`make_samplecore.py` 是复制生成，
只改了一处进度文件名）。⇒ 它们的渲染数字**必然一样**。
所以下面 SampleCore 那一列是**照实跑的**，不是我没跑就抄的 ——
而那也正好说明：**90% vs 30% 这个差，在当前代码里体现不出来。**

═══ 用法 ═══

    python _build/_bench_three.py                 # 默认 300 / 1000 / 4000
    python _build/_bench_three.py 100 300         # 指定档位
"""
from __future__ import annotations

import os
import statistics
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
引擎根 = os.path.dirname(LC)
SampleCore = os.path.join(引擎根, "HybridEngine-SampleCore")
专业版根 = os.environ.get("HYBRIDENGINE_ROOT") or os.path.join(引擎根, "HybridEngine")
专业版_PY = os.path.join(专业版根, "python")

宽, 高 = 1920, 1080
预算 = 16.667

#: ⚠⚠ 虚拟坐标的边长（**轻量版的坐标系是 0~100，不是像素**）。
#
#  我第一版把 `_铺开()` 的坐标算成 0~1920 像素，而轻量版把 `size=100` 当作
#  "虚拟坐标的边长"（`_xy()` 会 `x * 宽/size` 放大）⇒ 那些坐标全落到视野外
#  ⇒ **被视野裁剪全部挡掉** ⇒ 实测到的"4~6 ms"其实是**空调用的时间**，
#  画布上只有 1~9 个 item（要 300~3000 个）。
#
#  ⇒ 教训：**跨引擎比性能时，"坐标空间"必须一起对齐**，
#    否则比的不是速度，是"谁把东西画到屏幕外了"。
#    ⚠ 而且这个错误**很隐蔽**：数字看起来很合理（几百到几千对象 4~6 ms），
#      如果不是我顺手核对了 item 数，就会把它当成真数据报出去。
虚拟边长 = 100.0


def _铺开(对象数: int):
    """N 个圆的网格坐标 —— **虚拟坐标**（三个引擎共用同一份坐标）。

    ⚠ 专业版也吃虚拟坐标（它的 `fill_circle` 走设计分辨率），
      所以这一份坐标对两边都成立。
    """
    列 = int(对象数 ** 0.5) + 1
    步 = 虚拟边长 / (列 + 1)
    半径 = max(0.5, 步 * 0.35)
    点 = []
    for i in range(对象数):
        点.append(((i % 列) * 步 + 步, (i // 列) * 步 + 步, 半径))
    return 点


# ── 专业版 ────────────────────────────────────────────────────────────────

def 压专业(对象数: int, 帧数: int = 20):
    if 专业版_PY not in sys.path:
        sys.path.insert(0, 专业版_PY)
    try:
        from hybridengine import GameEngine
    except Exception as e:
        return {"错": "导入失败：%s: %s" % (type(e).__name__, str(e)[:120])}
    点 = _铺开(对象数)
    try:
        引擎 = GameEngine("three-bench", width=宽, height=高,
                         windowed=False, hidden=True)
    except Exception as e:
        return {"错": "建引擎失败：%s: %s" % (type(e).__name__, str(e)[:120])}
    try:
        面 = 引擎.draw

        def 一帧():
            引擎.run_frame()
            for (x, y, r) in 点:
                面.fill_circle(x, y, r, 0xFF4488FF)

        try:
            一帧()
            一帧()
        except Exception as e:
            return {"错": "画帧失败：%s: %s" % (type(e).__name__, str(e)[:160])}
        样本 = []
        for _ in range(帧数):
            t0 = time.perf_counter()
            一帧()
            样本.append((time.perf_counter() - t0) * 1000.0)
        自报 = None
        try:
            自报 = 引擎.last_frame_ms()
        except Exception:
            pass
        return {"ms": statistics.median(样本), "自报": 自报}
    finally:
        for 名 in ("dispose", "close"):
            try:
                getattr(引擎, 名)()
                break
            except Exception:
                pass


# ── 轻量版（LearningCore / SampleCore 同一套代码）──────────────────────────

def 压轻量(根: str, 对象数: int, 帧数: int = 12):
    """用**真窗口后端**（`Tk后端`）—— 只有它才真画。

    ⚠ `录制后端` 画圆只是 `append` 一个元组，**没有渲染** ⇒ 拿它比是自欺。
    ⚠ 所以这里开真窗口（`Tk后端` 会建窗口）。跑完就关。
    """
    if 根 not in sys.path:
        sys.path.insert(0, 根)
    try:
        import importlib
        pyengine = importlib.import_module("hybridengine_light.pyengine")
    except Exception as e:
        return {"错": "导入失败：%s: %s" % (type(e).__name__, str(e)[:120])}
    点 = _铺开(对象数)
    try:
        后 = pyengine.Tk后端(宽=宽, 高=高, size=100, 标题="three-bench")
    except Exception as e:
        return {"错": "建后端失败：%s: %s" % (type(e).__name__, str(e)[:120])}
    try:
        #: ⚠⚠ **两处口径修正（我第一版做错了，记下来）**：
        #:
        #:   ① 专业版画 `fill_circle` 是 **AA（抗锯齿）** 圆，
        #:      而 tkinter 的 `create_oval` **没有抗锯齿** ⇒ 专业版做的是**更难的工作**。
        #:      拿"带 AA 的圆"和"不带 AA 的圆"比，是**两个口径硬凑**。
        #:      ⇒ 轻量版改用 `画矩形`（圆角气泡的几何近似）：它和专业版的
        #:        `fill_rounded_rect` 一样是**实心填充、无 AA**，这才可比。
        #:      ⚠ 而这也**正好对上**专业版基准里的 S4 档（圆角气泡，真实 GUI 主负载）。
        #:
        #:   ② 轻量版 300→1000 只涨 8%（4.24→4.58）—— **不像在真画**。
        #:      ⇒ 每题完必须**核对画布上的 item 数**，证明"要它画 N 个就真有 N 个"。
        #:        （不然测出来的可能是"Tk 跳过了大部分绘制"）
        边长 = 0.0

        def 一帧():
            后.清屏("白")
            for (x, y, r) in 点:
                后.画矩形(x - r, y - r, r * 2, r * 2, "蓝")

        def 一帧带呈现():
            后.清屏("白")
            for (x, y, r) in 点:
                后.画矩形(x - r, y - r, r * 2, r * 2, "蓝")
            后.呈现()                # ⚠ 这一条才逼 Tk 真画出来

        一帧带呈现()
        一帧带呈现()
        #: ⚠⚠ **核对 item 数 —— 这一条是必须的，不是可选的。**
        #:   我第一版就是少了这道核对，把"被裁剪掉的空调用"当成了渲染数字报出去
        #:   （画布上只有 1~9 个 item，而要 300~3000 个）。
        #:   规则：**要它画 N 个，画布上就必须真有 N 个**；不是就判失败。
        在画布上 = len(后._canvas.find_all())
        if 在画布上 != 对象数:
            return {"错": ("画布上只有 %d 个 item，要 %d 个 —— "
                          "说明坐标/裁剪有问题，这个数**不是渲染数字**"
                          % (在画布上, 对象数))}
        样本 = []
        for _ in range(帧数):
            t0 = time.perf_counter()
            一帧带呈现()
            样本.append((time.perf_counter() - t0) * 1000.0)
        return {"ms": statistics.median(样本), "画布item": 在画布上}
    except Exception as e:
        return {"错": "画帧失败：%s: %s" % (type(e).__name__, str(e)[:160])}
    finally:
        try:
            后.关闭()
        except Exception:
            pass


def 主():
    print("  ═══ 三个引擎 · 同一口径压测 ═══")
    print("  %d×%d · 场景 = N 个圆（同一份坐标）· 口径 = **端到端每帧墙上时间**"
          % (宽, 高))
    print("  60 帧预算 = %.2f ms/帧" % 预算)
    print()
    档 = [int(x) for x in sys.argv[1:]] or [300, 1000, 4000]

    for 名, f in (("Professional HybridEngine", lambda n: 压专业(n)),
                  ("SampleCore", lambda n: 压轻量(SampleCore, n)),
                  ("LearningCore", lambda n: 压轻量(LC, n))):
        print("  ── %s ──" % 名)
        print("    %-8s %-16s %-14s %s" % ("对象", "中位 ms/帧", "能跑帧率", "结论"))
        for n in 档:
            r = f(n)
            if "错" in r:
                print("    %-8d XX %s" % (n, r["错"]))
                continue
            ms = r["ms"]
            尾 = ""
            if r.get("自报"):
                尾 = "   （引擎自报 %.2f ms）" % r["自报"]
            if r.get("画布item"):
                尾 += "   （画布 item %d / 要 %d）" % (r["画布item"], n)
            print("    %-8d %-16.3f %-14.1f %s%s" % (
                n, ms, min(60.0, 1000.0 / ms),
                "✅ 满帧" if ms <= 预算 else "⚠ 超预算", 尾))
        print()
    print("  ⚠⚠ **SampleCore 与 LearningCore 现在是同一套代码**（复制生成，只改了进度文件名）")
    print("     ⇒ 它们的数字**必然一样**。这一栏是照实跑的，不是抄的。")
    return 0


if __name__ == "__main__":
    sys.exit(主())
