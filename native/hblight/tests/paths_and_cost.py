#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""paths_and_cost.py —— 每层必备的两件事：**路径证明** + **成本实测**。

## 为什么要这两个（都是踩出来的）

### ① 路径证明 —— 防"假绿"
`pyengine_c` 是**按设计静默退回** Python 的（手机版 Chaquopy 里没有 .so）。
⇒ **"内核没生效"和"内核生效了"长得一模一样，判据都是绿的。**
   本轮实测踩到过：MinGW 的 dll 缺 `libstdc++-6.dll` ⇒ 加载失败 ⇒ 悄悄走 Python
   ⇒ `test_pyengine` 照样 exit 0，**但绿的是 Python 版**。
⇒ 所以任何一层都必须**显式断言"走的是哪条路"**，不能只看 exit code。

### ② 成本实测 —— 防"想当然地以为搬过去更快"
已量过：数值内核只占总时间的 ~10%（绘制占 90%）。
⇒ 搬过去**很可能不变快甚至变慢**（ctypes 每帧往返）。**这不是失败**
   —— 这一层是为了**规格**（作者要求引擎是 C++），不是性能。
   但**必须把成本量出来**，否则没法判断"继续搬值不值"。

⚠ 量的时候**必须带上"画布上有几个 item"** —— 裁剪会改变量级
  （踩过：1000 个对象时 Tk 画布只有 20 个 item，那个数不能当渲染性能）。

## 用法

    python native/hblight/tests/paths_and_cost.py            # 路径证明 + 三配置成本
    python native/hblight/tests/paths_and_cost.py --跑基准    # 内部用：被测子进程
"""
from __future__ import annotations

import os
import sys
import time

try:
    sys.stdout.reconfigure(errors="replace")
    sys.stderr.reconfigure(errors="replace")
except Exception:
    pass

对象数 = 1000
帧数 = 300

配置 = [
    ("① Python 版（默认，不设开关）", {}),
    ("② 第1层 C++（向量+变换）", {"HBLIGHT_ENABLE": "1", "HBLIGHT_LAYER": "1"}),
    ("③ 第2层 C++（+相机2D）", {"HBLIGHT_ENABLE": "1", "HBLIGHT_LAYER": "2"}),
]


def _找仓库根() -> str:
    d = os.path.dirname(os.path.abspath(__file__))
    while True:
        父 = os.path.dirname(d)
        if 父 == d:
            raise RuntimeError("找不到仓库根")
        d = 父
        if os.path.isdir(os.path.join(d, "hybridengine_light")):
            return d


根 = _找仓库根()


# ══════════════════════════════════════════════════════════════════════════
# 被测子进程：铺一个场景，跑 K 帧，报 ms/帧 + item 数 + 路径
# ══════════════════════════════════════════════════════════════════════════

def 跑基准() -> None:
    if 根 not in sys.path:
        sys.path.insert(0, 根)
    from hybridengine_light import pyengine as E
    from hybridengine_light.pyengine import components as C
    from hybridengine_light import pyengine_c as PC

    后 = E.录制后端()
    g = E.游戏(场景=E.场景("bench"), 后端=后, 尺寸=100, 宽=400, 高=400)
    g.场景.设服务("输入", g.输入)

    # 铺对象：世界坐标 0..99 的网格（和 Lead 那次同口径）
    for i in range(对象数):
        o = g.场景.造("o%d" % i, float((i % 32) * 3), float((i // 32) * 3))
        o.加(C.圆点(半径=1.0), C.匀速移动(vx=1.0, vy=0.5, 边界=(0.0, 100.0)))

    for _ in range(10):          # 预热（JIT/缓存/首帧分配）
        g.一步()
    后.清空()

    t0 = time.perf_counter()
    for _ in range(帧数):
        g.一步()
    耗时 = (time.perf_counter() - t0) / 帧数 * 1000.0

    item数 = len(后.本帧())      # 录制后端**不裁剪** ⇒ 这是"提交了多少图元"
    对象总数 = sum(1 for _ in g.场景.遍历())

    # ── 路径证明：**这一层到底走的是谁** ──
    路 = {
        "向量": E.向量.__module__,
        "变换": E.变换.__module__,
        "相机2D": E.相机2D.__module__,
    }
    内核路 = all(v.endswith("pyengine_c") for v in 路.values())
    内核可用 = PC.可用()

    print("RESULT ms_per_frame=%.4f items=%d objects=%d frames=%d kernel_available=%s all_cpp=%s"
          % (耗时, item数, 对象总数, 帧数, 内核可用, 内核路))
    print("RESULT 向量=%s 变换=%s 相机2D=%s" % (路["向量"], 路["变换"], 路["相机2D"]))


# ══════════════════════════════════════════════════════════════════════════
# 驱动：每个配置起一个**干净的子进程**（环境变量不能互相污染）
# ══════════════════════════════════════════════════════════════════════════

def 主() -> int:
    import subprocess

    print("=" * 74)
    print("路径证明 + 成本实测   （%d 对象 · %d 帧 · 录制后端）" % (对象数, 帧数))
    print("=" * 74)

    结果 = []
    for 名, 环境 in 配置:
        env = dict(os.environ)
        env.pop("HBLIGHT_ENABLE", None)
        env.pop("HBLIGHT_LAYER", None)
        env.update(环境)
        env["PYTHONIOENCODING"] = "utf-8"
        r = subprocess.run([sys.executable, os.path.abspath(__file__), "--跑基准"],
                           capture_output=True, text=True, encoding="utf-8",
                           errors="replace", env=env, cwd=根)
        行 = [x for x in (r.stdout or "").splitlines() if x.startswith("RESULT")]
        数据 = {}
        for x in 行:
            for kv in x[len("RESULT "):].split():
                if "=" in kv:
                    k, v = kv.split("=", 1)
                    数据[k.strip()] = v.strip()
        结果.append((名, r.returncode, 数据, 行))

    print()
    print("%-26s %10s %8s %10s %8s" % ("配置", "ms/帧", "item数", "对象数", "全C++?"))
    print("-" * 74)
    for 名, 码, d, 行 in 结果:
        if 码 != 0 or "ms_per_frame" not in d:
            print("%-26s !! 子进程退出码 %d" % (名, 码))
            continue
        print("%-26s %10s %8s %10s %8s"
              % (名, d.get("ms_per_frame"), d.get("items"),
                 d.get("objects"), d.get("all_cpp")))
    print()
    print("── 路径证明（每一层的判据都要有这一条）──")
    for 名, 码, d, 行 in 结果:
        for x in 行[1:]:
            print("  %-26s %s" % (名, x[len("RESULT "):]))
    print()

    # ── 结论：只报实测，不修饰 ──
    可用 = [d for _, 码, d, _ in 结果 if 码 == 0 and "ms_per_frame" in d]
    if len(可用) == 3:
        a, b, c = (float(d["ms_per_frame"]) for d in 可用)
        print("── 差值（如实报，不写成“移植后更快”）──")
        print("  ①→②  %+.4f ms/帧  (%+.1f%%)" % (b - a, (b / a - 1) * 100 if a else 0))
        print("  ②→③  %+.4f ms/帧  (%+.1f%%)" % (c - b, (c / b - 1) * 100 if b else 0))
        print("  ①→③  %+.4f ms/帧  (%+.1f%%)" % (c - a, (c / a - 1) * 100 if a else 0))
        print()
        print("  ⚠ 正数 = 更慢。ctypes 每帧往返的代价是**真实存在**的。")
    return 0


if __name__ == "__main__":
    if "--跑基准" in sys.argv:
        跑基准()
        sys.exit(0)
    sys.exit(主())
