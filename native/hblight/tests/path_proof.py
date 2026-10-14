#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""path_proof.py —— ⭐ **证"走的是哪条路"**（task-13 第一小步的第 ④ 条判据）。

## 为什么必须单独有这一条

本仓为"只看 `exit 0`"栽过**一次假绿**：dll 加载失败 ⇒ `pyengine_c` 按设计
**静默退回** Python ⇒ 判据全绿，但**绿的是回退路**。
⇒ 所以每个"有开关、有回退"的东西，都要能**当场问出来走的是哪条**。

## 三个模式（正 + 反 + 缺库）

    默认    不给 `HBLIGHT_RENDER`   ⇒ 必须走 **tkinter**（手机版靠它）
    启用    `HBLIGHT_RENDER=1`     ⇒ 必须走 **Cpp后端**，而且**真的画了像素**
    缺库    `HBLIGHT_RENDER=1` + 库加载失败 ⇒ **必须退回 tkinter，且不许崩**

⚠ 第三个模式是**最容易漏的**：开关开着但库不在，如果直接崩，
  用户看到的是"引擎起不来了"，而根因是"后端没建出来"。

## 用法

    python native/hblight/tests/path_proof.py            # 跑全部三个模式
    python native/hblight/tests/path_proof.py --跑 启用   # 内部：被测子进程
"""
from __future__ import annotations

import os
import subprocess
import sys

try:
    sys.stdout.reconfigure(errors="replace")
    sys.stderr.reconfigure(errors="replace")
except Exception:
    pass


def _找根() -> str:
    d = os.path.dirname(os.path.abspath(__file__))
    while True:
        父 = os.path.dirname(d)
        if 父 == d:
            raise RuntimeError("找不到仓库根")
        d = 父
        if os.path.isdir(os.path.join(d, "hybridengine_light")):
            return d


根 = _找根()
sys.path.insert(0, 根)


def 跑一个模式(名: str) -> int:
    from hybridengine_light import pyengine as E
    from hybridengine_light import pyengine_c
    from hybridengine_light.pyengine_c import backend as B
    from hybridengine_light.pyengine.render import Tk后端

    print("--- 模式=%s ---" % 名)
    print("HBLIGHT_RENDER=%r" % os.environ.get("HBLIGHT_RENDER"))
    print("库可用=%s 后端名=%s" % (B.可用(), B.后端名()))

    if 名 == "缺库":
        # ⚠ 人为制造"开关开着但库不在"：把加载结果抹掉
        B._lib = None
        B._加载错误 = "（本判据人为制造）模拟库不存在"
        print("模拟后 可用=%s" % B.可用())

    if 名 == "默认":
        后 = B.默认后端(400, 300, 100)
        print("默认后端类型=%s" % type(后).__name__)
        ok = isinstance(后, Tk后端)
        print("走的是 tkinter=%d" % (1 if ok else 0))
        后.关闭()
        return 0 if ok else 1

    if 名 == "缺库":
        后 = B.默认后端(400, 300, 100)
        print("缺库时后端类型=%s" % type(后).__name__)
        ok = isinstance(后, Tk后端)
        print("缺库时退回 tkinter=%d" % (1 if ok else 0))
        后.关闭()
        return 0 if ok else 1

    if 名 in ("接入", "接入默认"):
        # ⭐ 这一段才是"**接进了 pyengine**"的证据：
        #   ⚠ **不传 `后端=`** ⇒ 走的是 `游戏.后端` 那个 property（本轮改的那一处）
        g = E.游戏(场景=E.场景("t"), 尺寸=100, 宽=400, 高=300)
        类名 = type(g.后端).__name__
        print("游戏()_拿到后端=%s" % 类名)
        # ⚠ `装输入` 是**键盘唯一的接线点**：它被调过 ⇒ 说明 engine.py 那段**还在**
        #   （`_engine_fingerprint` **看不见**这个 —— 无头判据不按键）
        装输入调过 = bool(getattr(g.后端, "_输入没接", False))
        print("装输入被调过=%d" % (1 if 装输入调过 else 0))
        预期 = "Cpp后端" if 名 == "接入" else "Tk后端"
        ok = (类名 == 预期)
        print("预期=%s 对上了=%d" % (预期, 1 if ok else 0))
        if 名 == "接入":
            print("INPUT_NOT_WIRED=%d" % (1 if 装输入调过 else 0))
        try:
            g.后端.关闭()
        except Exception:
            pass
        return 0 if ok else 1

    # ── 启用：必须真的用 C++ 后端，而且真的画了 ──
    import ctypes
    import tkinter as tk
    root = tk.Tk()
    root.title("path_proof")
    root.geometry("460x360")
    root.update()
    lib = B._载入()
    # ⚠ `winfo_id()` 给的是 Tk 的**子窗**，要往上找真顶层（`GetAncestor(GA_ROOT)`）
    lib.hb_child_root_of.argtypes = [ctypes.c_void_p]
    lib.hb_child_root_of.restype = ctypes.c_void_p
    顶层 = int(lib.hb_child_root_of(ctypes.c_void_p(int(root.winfo_id()))) or 0)
    print("TK_WINFO_ID=%d" % root.winfo_id())
    print("TK_ROOT_HWND=%d" % 顶层)

    后 = B.Cpp后端(400, 300, 100, 父窗口=顶层)
    print("后端类型=%s" % type(后).__name__)

    from hybridengine_light.pyengine import components as C
    g = E.游戏(场景=E.场景("proof"), 后端=后, 尺寸=100, 宽=400, 高=300)
    o = g.场景.造("p", 30, 40)
    o.加(C.圆点(半径=5, 颜色="红"))
    g.一步()

    引擎用的就是它 = g.后端 is 后
    print("ENGINE_USES_CPP=%d" % (1 if 引擎用的就是它 else 0))
    print("WRITTEN_PIXELS=%d" % 后.写过的像素())
    print("PRESENTED_FRAMES=%d" % 后.呈现过的帧())
    print("CHILD_VISIBLE=%d" % (1 if 后.子窗口可见() else 0))
    print("ALLOW_CLIP=%d" % (1 if 后.允许裁剪 else 0))

    # ⚠ 三条都要：用对了后端 + 真写了像素 + 真呈上去了
    ok = (引擎用的就是它 and 后.写过的像素() > 0 and 后.呈现过的帧() >= 1
          and 后.子窗口可见())
    print("CPP_PATH_OK=%d" % (1 if ok else 0))

    # 画文字必须明确报 NotImplementedError（不是静默空白）
    try:
        后.画文字(0, 0, "x", "黑", 12)
        print("TEXT_RAISES=0")
        ok = False
    except NotImplementedError as e:
        print("TEXT_RAISES=1")
        print("TEXT_MSG=%s" % str(e)[:60])

    后.关闭()
    print("AFTER_CLOSE_STILL_OPEN=%d" % (1 if 后.还开着 else 0))
    root.destroy()
    return 0 if ok else 1


def main() -> int:
    if "--跑" in sys.argv:
        i = sys.argv.index("--跑")
        return 跑一个模式(sys.argv[i + 1])

    模式表 = [("默认", {}), ("启用", {"HBLIGHT_RENDER": "1"}), ("缺库", {"HBLIGHT_RENDER": "1"}),
              # ⭐ 这两个才是"接进 pyengine"的证据：**不传 后端=**，走 `游戏.后端`
              ("接入", {"HBLIGHT_RENDER": "1"}), ("接入默认", {})]
    全过 = True
    for 名, 环境 in 模式表:
        env = dict(os.environ)
        env.pop("HBLIGHT_RENDER", None)
        env.update(环境)
        env["PYTHONIOENCODING"] = "utf-8"
        r = subprocess.run([sys.executable, os.path.abspath(__file__), "--跑", 名],
                           capture_output=True, text=True, encoding="utf-8",
                           errors="replace", env=env, cwd=根)
        print((r.stdout or "").rstrip())
        if r.returncode != 0:
            print("!! 模式 %s 子进程退出码 %d" % (名, r.returncode))
            全过 = False
    print()
    print("PATH_PROOF=%s" % ("PASS" if 全过 else "FAIL"))
    return 0 if 全过 else 1


if __name__ == "__main__":
    sys.exit(main())
