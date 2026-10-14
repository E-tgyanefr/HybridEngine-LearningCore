#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""embed_probe.py —— 验那个**前提**：Tk 自己的消息循环能不能驱动一个外部创建的
同线程子窗口？

## 为什么要有这个探针

Lead 定了架构：**同线程 + 子窗口（`WS_CHILD` 挂到 Tk 的 HWND 上）**。
但它有**一条没验的前提** —— Win32 里同线程窗口共用一个消息队列，
所以"应该"能驱动。**"应该"在本仓不算数。**
⇒ 先验它，再往上盖房子。

## 三条判据（⚠ 少一条都会让失败"看起来像成功"）

    ① 子窗口**可见**              —— `IsWindowVisible` + 从窗口上**读回像素**
    ② **帧真的在变**              —— 每帧缓冲的校验和**必须互不相同**
                                      （否则是在放同一张图 ⇒ "画面在动"是假的）
    ③ ⚠⚠ **Tk 自己的控件仍然正常** —— 投一条**真实**鼠标点击，看 Tk 按钮响不响应

第 ③ 条最容易翻车（消息被我们吃掉），而它是**整个方案的生死线**：
  "窗口出现了 + 帧在动"会**看起来成功**，真正的失败要等你真去点按钮才发现。

## ⚠⚠ 而且 ③ 必须带**反面**

一条"永远绿"的判据比没有判据更糟（本仓吃过假绿）。
⇒ `--eat` 模式：消息泵**只移除、不派发** ⇒ 同线程共用队列 ⇒
   **Tk 的点击消息被我们吃掉** ⇒ ③ **必须变红**。
   ③ 不红 ⇒ 这条判据测不出东西 ⇒ 方案结论也不可信。

## 用法

    python native/hblight/tests/embed_probe.py            # 正面
    python native/hblight/tests/embed_probe.py --eat      # 反面（③ 应当红）
    python native/hblight/tests/embed_probe.py --no-pump  # 完全不泵我们自己的消息
"""
from __future__ import annotations

import argparse
import ctypes
import os
import sys
import tkinter as tk

# ⚠ 本仓硬规矩：Windows 控制台是 cp936，中文/符号直接 print 会 UnicodeEncodeError
#   （本仓栽过三次）。保留编码、只把编不了的降级成 `?`。
try:
    sys.stdout.reconfigure(errors="replace")
    sys.stderr.reconfigure(errors="replace")
except Exception:
    pass

子宽, 子高 = 320, 240
帧数 = 60


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
# ⚠⚠ **MinGW 给共享库加 `lib` 前缀**（MSVC 不加）⇒ 两个名字都要认。
#   本仓已经在 `build_hblight.py` 上因为这一条栽过一次（"构建成功却找不到产物"），
#   这里**又栽一次**（探针 exit 2）—— 所以同一个坑挖了两个地方，都记下来。
候选名 = (("libhblight_embed.dll", "hblight_embed.dll") if sys.platform.startswith("win")
          else (("libhblight_embed.dylib",) if sys.platform == "darwin"
                else ("libhblight_embed.so",)))


def _找库():
    目录 = os.path.join(根, "hybridengine_light", "pyengine_c")
    for 名 in 候选名:
        p = os.path.join(目录, 名)
        if os.path.isfile(p):
            return p
    return None


DLL = _找库() or os.path.join(根, "hybridengine_light", "pyengine_c", 候选名[0])


def 装载():
    lib = ctypes.CDLL(DLL)
    P, I = ctypes.c_void_p, ctypes.c_int
    lib.hb_child_backend.restype = ctypes.c_char_p
    lib.hb_child_root_of.argtypes = [P]; lib.hb_child_root_of.restype = P
    lib.hb_child_new.argtypes = [P, I, I, I, I]; lib.hb_child_new.restype = P
    lib.hb_child_free.argtypes = [P]
    lib.hb_child_native.argtypes = [P]; lib.hb_child_native.restype = P
    lib.hb_child_visible.argtypes = [P]; lib.hb_child_visible.restype = I
    lib.hb_child_present.argtypes = [P, P, I, I]; lib.hb_child_present.restype = I
    lib.hb_child_pump.argtypes = [P, I]; lib.hb_child_pump.restype = I
    lib.hb_child_presented.argtypes = [P]; lib.hb_child_presented.restype = ctypes.c_longlong
    lib.hb_child_get_pixel.argtypes = [P, I, I]; lib.hb_child_get_pixel.restype = ctypes.c_uint32
    lib.hb_child_post_click.argtypes = [P, I, I]; lib.hb_child_post_click.restype = I
    return lib


def fnv1a(data: bytes) -> int:
    h = 1469598103934665603
    for b in data:
        h = ((h ^ b) * 1099511628211) & 0xFFFFFFFFFFFFFFFF
    return h


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--eat", action="store_true", help="反面：消息只移除不派发")
    ap.add_argument("--no-pump", action="store_true", help="完全不泵我们自己的消息")
    args = ap.parse_args()

    if not os.path.isfile(DLL):
        print("!! 找不到 %s" % DLL)
        print("   ⇒ 先构建：python _build/build_hblight.py --go")
        return 2

    lib = 装载()
    后端 = lib.hb_child_backend().decode()
    print("CHILD_BACKEND=%s" % 后端)
    print("MODE=%s" % ("EAT" if args.eat else ("NOPUMP" if args.no_pump else "NORMAL")))
    if 后端 != "win32":
        print("!! 这个实验只在 Windows 上有意义（后端=%s）" % 后端)
        return 3

    # ── Tk 主窗 + 一个按钮（判据 ③ 要用它）──────────────────────────
    root = tk.Tk()
    root.title("embed_probe")
    root.geometry("640x420")

    点击 = []
    btn = tk.Button(root, text="点我", command=lambda: 点击.append(1))
    btn.place(x=20, y=20, width=140, height=40)
    标签 = tk.Label(root, text="初始", bg="#dddddd")
    标签.place(x=20, y=80, width=140, height=30)
    root.update()                      # 让 Tk 真的把窗口建出来

    tk_hwnd = root.winfo_id()
    顶层 = lib.hb_child_root_of(ctypes.c_void_p(tk_hwnd))
    print("TK_WINFO_ID=%d" % tk_hwnd)
    print("TK_ROOT_HWND=%d" % (顶层 or 0))
    print("WININFO_IS_ROOT=%d" % (1 if 顶层 == tk_hwnd else 0))

    子 = lib.hb_child_new(ctypes.c_void_p(顶层), 280, 120, 子宽, 子高)
    print("CHILD_HWND=%d" % (子 or 0))
    print("CHILD_VISIBLE=%d" % lib.hb_child_visible(子))
    if not 子:
        print("!! 子窗口没建出来 —— 前提直接不成立")
        root.destroy()
        return 1

    # ── 每帧一张**内容在变**的位图 ──────────────────────────────────
    缓冲 = ctypes.create_string_buffer(子宽 * 子高 * 4)
    像素 = (ctypes.c_uint32 * (子宽 * 子高)).from_buffer(缓冲)
    校验和 = []
    呈现成功 = [0]
    泵过 = [0]
    状态 = {"帧": 0}

    def 一帧():
        f = 状态["帧"]
        # 每帧整幅换色 ⇒ 校验和必然不同（判据 ②）
        for i in range(len(像素)):
            像素[i] = (0x00 << 24) | ((f * 4 % 256) << 16) | ((128) << 8) | (255 - f * 4 % 256)
        校验和.append(fnv1a(缓冲.raw))
        呈现成功[0] += lib.hb_child_present(子, ctypes.cast(缓冲, ctypes.c_void_p), 子宽, 子高)
        if not args.no_pump:
            泵过[0] += lib.hb_child_pump(子, 1 if args.eat else 0)
        状态["帧"] += 1
        if 状态["帧"] < 帧数:
            root.after(16, 一帧)
        else:
            root.after(1, 收尾)

    def 收尾():
        print("FRAMES=%d" % 状态["帧"])
        print("PRESENTS_OK=%d" % 呈现成功[0])
        print("PUMPED_MESSAGES=%d" % 泵过[0])
        print("PRESENTED_FIELD=%d" % lib.hb_child_presented(子))

        # ① 可见 + 从窗口读回像素
        可见 = lib.hb_child_visible(子)
        读回 = lib.hb_child_get_pixel(子, 子宽 // 2, 子高 // 2)
        print("VISIBLE=%d" % 可见)
        print("PIXEL_COLORREF=0x%06X" % 读回)
        判1 = bool(可见) and 读回 != 0xFFFFFFFF

        # ② 帧真的在变
        不同 = len(set(校验和))
        print("DISTINCT_FRAME_HASHES=%d/%d" % (不同, len(校验和)))
        判2 = 不同 == len(校验和) and len(校验和) == 帧数

        # ③ Tk 自己的控件仍然正常 —— 投一条**真实**的点击
        cx = btn.winfo_x() + btn.winfo_width() // 2
        cy = btn.winfo_y() + btn.winfo_height() // 2     # 客户区坐标

        # ⚠⚠ 第一次用 `PostMessage(WM_LBUTTONDOWN)` **按钮根本不响应** ——
        #   而且 `--no-pump`（我们的泵一个消息都没抽）下**同样不响应** ⇒
        #   **是投递手段坏了，不是消息被吃**。差点因此得出"前提不成立"的错结论。
        #   ⇒ 改用**真实输入注入**：`SetCursorPos` + `mouse_event`。
        #     它会动用户的鼠标，所以先存后还。
        u32 = ctypes.windll.user32
        # ⚠ 窗口**没被激活**时，第一下点击只会把它激活，不会到控件上 ⇒
        #   先强行抬到前台并给焦点，否则投递再多下也白搭。
        try:
            root.lift()
            root.focus_force()
            u32.SetForegroundWindow(ctypes.c_void_p(顶层))
        except Exception as e:
            print("FOREGROUND_FAILED=%s" % e)
        root.update()
        print("FOREGROUND_HWND=%d" % (u32.GetForegroundWindow() or 0))
        print("TK_ROOT_HWND_FOR_CMP=%d" % (顶层 or 0))
        class _P(ctypes.Structure):
            _fields_ = [("x", ctypes.c_long), ("y", ctypes.c_long)]
        旧 = _P()
        u32.GetCursorPos(ctypes.byref(旧))
        # 客户区 → 屏幕：拿 Tk 顶层窗口的客户区原点
        屏x = root.winfo_rootx() + cx
        屏y = root.winfo_rooty() + cy
        u32.SetCursorPos(int(屏x), int(屏y))
        root.update()
        u32.mouse_event(0x0002, 0, 0, 0, 0)      # LEFTDOWN
        u32.mouse_event(0x0004, 0, 0, 0, 0)      # LEFTUP
        print("CLICK_AT_SCREEN=%d,%d" % (屏x, 屏y))

        # ⚠⚠ 这一段是**反面判据的修正版**：
        #   原来我把点击投在帧循环**之后**，而泵只在帧循环里跑 ⇒
        #   反面（--eat）根本吃不到那条点击 ⇒ 正/反面长得一样 ⇒ **判据无效**。
        #   ⇒ 现在：投完点击**立刻**用同一个函数、只改"派发/丢弃"这一位。
        if not args.no_pump:
            抽到 = lib.hb_child_pump(子, 1 if args.eat else 0)
            print("PUMP_AFTER_CLICK=%d dispatched=%d" % (抽到, 0 if args.eat else 1))

        # ⚠ 让 Tk 自己把剩下的消息抽完（**这一步正反面都有** —— 区别只在上面那一泵）
        for _ in range(30):
            root.update()
        u32.SetCursorPos(int(旧.x), int(旧.y))   # 还回鼠标
        标签.config(text="改过了")
        for _ in range(5):
            root.update()
        标签变了 = 标签.cget("text") == "改过了"
        print("BUTTON_CLICKS=%d" % len(点击))
        print("LABEL_UPDATED=%d" % (1 if 标签变了 else 0))
        # ⚠ 只有"点击真的被 Tk 处理了"才算 ③ 过；标签更新**不算数**
        #   （`config`+`update` 走的是 Tk 内部路径，不经过我们的消息泵 ⇒ 它恒真）
        判3 = len(点击) > 0

        print("JUDGE1_VISIBLE=%d" % (1 if 判1 else 0))
        print("JUDGE2_FRAMES_CHANGE=%d" % (1 if 判2 else 0))
        print("JUDGE3_TK_STILL_WORKS=%d" % (1 if 判3 else 0))

        全过 = 判1 and 判2 and 判3
        if args.eat or args.no_pump:
            # 反面/旁证：③ **期望红**
            print("VERDICT=%s" % ("NEGATIVE_CONTROL_RED_AS_EXPECTED" if not 判3 else
                                  "NEGATIVE_CONTROL_GREEN_UNEXPECTED"))
        else:
            print("VERDICT=%s" % ("PASS" if 全过 else "FAIL"))

        lib.hb_child_free(子)
        root.destroy()

    root.after(16, 一帧)
    root.mainloop()
    return 0


if __name__ == "__main__":
    sys.exit(main())
