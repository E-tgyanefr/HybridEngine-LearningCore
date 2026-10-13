"""专业版（HybridEngine）的 Python 侧压测 —— 与轻量版**同一口径**。

═══ 口径（这条最要紧，三个引擎必须一致）═══

作者给的性能比例：专业版 100% / SampleCore 90% / LearningCore 30%。
**要验证它，就得让三个引擎做同一件事、计同一段时间。**

本脚本的口径：

    · 分辨率 1920×1080
    · 场景 = **N 个 AA 圆**（专业版基准里的 S3「图元率」档）
    · 计时 = **端到端每帧墙上时间**（构造绘制命令 + 提交渲染）
      ⚠ 专业版自己的 `ms_bench2d` 只计 `ms_engine_render`（纯光栅化），
        **不含命令录制**。那个口径和轻量版没法比 ——
        轻量版的"命令录制"就是它画圆本身，分不开。
      ⇒ 所以这里统一用**端到端**：三个引擎都在同一个口径下比。
        专业版的"纯光栅化"数字另附，供参考。

═══ ⚠ 为什么用 `hidden=True` ═══

`GameEngine` 的 `hidden=True` 让引擎**建窗口但不 ShowWindow** ⇒
窗口句柄有效（GPU 也能用）但**不弹窗、不抢焦点** ⇒ 压测可以无人值守跑。

═══ 用法 ═══

    python _build/_bench_pro.py               # 默认 3 档
    python _build/_bench_pro.py 1000 4000     # 指定对象数
"""
from __future__ import annotations

import os
import statistics
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

#: 专业版 Python 绑定的位置（**不在本仓**，在引擎仓里）
_引擎根 = os.environ.get("HYBRIDENGINE_ROOT") or \
    os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(
        os.path.abspath(__file__)))), "HybridEngine")
_PY = os.path.join(_引擎根, "python")
if _PY not in sys.path:
    sys.path.insert(0, _PY)

宽, 高 = 1920, 1080


def 试导入():
    try:
        from hybridengine import GameEngine          # noqa: F401
        return True, ""
    except Exception as e:
        return False, "%s: %s" % (type(e).__name__, e)


def 压(对象数: int, 帧数: int = 30) -> dict:
    """画 N 个圆，返回端到端每帧毫秒。

    ⚠ API 是**探出来的**（不问人、不猜）：
        `GameEngine(title, width, height, windowed=False, hidden=True)`
        `引擎.draw` 是 `DrawCtx` ⇒ `draw.fill_circle(cx, cy, r, 颜色)`
        `引擎.run_frame()` 跑一帧
        颜色是 `int`，`0xAARRGGBB`（A = alpha 直通）
    """
    from hybridengine import GameEngine

    引擎 = GameEngine("pro-bench", width=宽, height=高,
                     windowed=False, hidden=True)
    try:
        面 = 引擎.draw
        #: 铺开成网格，避免全叠在一起（那会测成"同一个小区域反复画"）
        列 = int(对象数 ** 0.5) + 1
        步 = 宽 / (列 + 1)
        半径 = max(2.0, 步 * 0.35)

        def 一帧():
            引擎.run_frame()
            for i in range(对象数):
                x = (i % 列) * 步 + 步
                y = (i // 列) * 步 + 步
                面.fill_circle(x, y, 半径, 0xFF4488FF)

        #: 预热两帧（首次会建资源）
        一帧()
        一帧()
        样本 = []
        for _ in range(帧数):
            t0 = time.perf_counter()
            一帧()
            样本.append((time.perf_counter() - t0) * 1000.0)
        #: 引擎自报的上一帧毫秒（它内部的口径，供对照）
        自报 = None
        try:
            自报 = 引擎.last_frame_ms()
        except Exception:
            pass
        return {"ms": statistics.median(样本), "样本": 样本, "自报": 自报}
    finally:
        try:
            引擎.dispose()
        except Exception:
            try:
                引擎.close()
            except Exception:
                pass


def 主():
    print("  ═══ 专业版 HybridEngine · Python 侧压测 ═══")
    print("  分辨率 %d×%d · 场景 = N 个 AA 圆 · 口径 = **端到端每帧墙上时间**" % (宽, 高))
    print("  60 帧预算 = 16.67 ms/帧")
    print()
    能, 错 = 试导入()
    if not 能:
        print("  XX 导入不了专业版 Python 绑定：%s" % 错)
        print("     引擎根：%s" % _引擎根)
        print("     可以用 HYBRIDENGINE_ROOT 环境变量指定引擎仓位置")
        return 1

    档 = [int(x) for x in sys.argv[1:]] or [1000, 4000, 8000]
    print("  %-10s %-16s %-14s %s" % ("对象", "中位 ms/帧", "能跑帧率", "结论"))
    结果 = []
    for n in 档:
        r = 压(n)
        if "错" in r:
            print("  %-10d XX %s" % (n, r["错"]))
            continue
        ms = r["ms"]
        结果.append((n, ms))
        print("  %-10d %-16.3f %-14.1f %s" % (
            n, ms, min(60.0, 1000.0 / ms),
            "✅ 满帧" if ms <= 16.667 else "⚠ 超预算"))
    print()
    if 结果:
        n, ms = 结果[-1]
        print("  最重一档：%d 个圆 ⇒ %.2f ms/帧" % (n, ms))
    return 0


if __name__ == "__main__":
    sys.exit(主())
