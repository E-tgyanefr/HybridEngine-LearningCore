#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""build_hblight.py —— 构建 C++ 内核 `libhblight` 并把产物搬回仓库。

## ⚠⚠ 为什么要先拷到 `C:\\hblight\\` 再构建

本仓在 `引擎\\` 下（含中文）。**MinGW 的 `ld` 处理不了中文路径** ——
报错还离真因很远（`No rule to make target ...` 或链接期莫名的
`cannot find -l...`）。所以：

    源码（仓库，含中文路径）  --拷贝-->  C:\\hblight\\
                                        ↓ cmake + ninja 在这里跑
    仓库 hybridengine_light/pyengine_c/  <--拷贝--  产物

⇒ 这个脚本的**唯一职责**就是别让人再踩那个坑。

## 用法（照本仓 `_build/build_*.py` 的既有风格）

    python _build/build_hblight.py            # 只报告要做什么（不落盘）
    python _build/build_hblight.py --go       # 真构建
    python _build/build_hblight.py --go --clean   # 先清空工作区再构建

退出码：0 = 成功；非 0 = 失败（**不要把失败当成功**）。
"""
from __future__ import annotations

import argparse
import io
import os
import shutil
import subprocess
import sys
import sysconfig

根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
源码 = os.path.join(根, "native", "hblight")
#: ⚠⚠ 构建工作区**必须分平台**（2026-10-14 修：原来是写死的 `C:\hblight`）。
#:
#:   为什么原来写死在 Windows：**MinGW 的 `ld` 处理不了中文路径**
#:   （仓库在 `引擎\` 下）⇒ 必须换到 ASCII 路径去编译。
#:
#:   ⚠ 但写死 `C:\hblight` 在 Linux/macOS 上会变成**相对路径** `./C:\hblight`
#:     （反斜杠在 POSIX 里是文件名字符，不是分隔符）⇒ 会建出一个怪目录名，
#:     而 `cmake` 多半还能跑起来 ⇒ **症状会离真因很远**。
#:
#:   ⚠ 而这正是"跨平台只写了、没构建过"的直接后果：
#:     队友坦白说他本机没有 Linux/macOS，`.so`/`.dylib` **一次都没产出过**。
#:     ⇒ 接进 CI 就是要把这类问题**在别人的机器上**抓出来。
#:
#:   ⇒ POSIX 上用 `/tmp/hblight`；Windows 上保持 `C:\hblight`（ASCII、短、无空格）。
#:     想换地方就设 `HBLIGHT_WORK`。
工作区 = os.environ.get("HBLIGHT_WORK") or (
    r"C:\hblight" if sys.platform.startswith("win") else "/tmp/hblight")
工作源码 = os.path.join(工作区, "src")
工作构建 = os.path.join(工作区, "build")
绑定目录 = os.path.join(根, "hybridengine_light", "pyengine_c")


def _说(文本: str = "") -> None:
    """打印一行。

    ⚠⚠ **不能直接 `sys.stdout.write`** —— Windows 上控制台默认是 GBK(cp936)，
      而本脚本的输出里有 `⚠`/`✓` 这类字符 ⇒
      `UnicodeEncodeError: 'gbk' codec can't encode character '\\u26a0'`
      ⇒ **脚本在"打印"这一步就崩了**（实测踩到，而且崩在构建之前的第 1 步）。
      换个编码的控制台（或重定向到文件）就复现不了 —— 典型的"只在别人机器上炸"。

    ⇒ 兜底：编码不了就 `replace` 成 `?`，**绝不让打印把构建搞挂**。
    """
    流 = sys.stdout
    行 = 文本 + "\n"
    try:
        流.write(行)
    except UnicodeEncodeError:
        编码 = 流.encoding or "utf-8"
        流.write(行.encode(编码, "replace").decode(编码, "replace"))
    流.flush()


def 候选名() -> list[str]:
    """产物可能叫什么。

    ⚠⚠ **MinGW 会给共享库加 `lib` 前缀**：同一个 CMake 目标 `hblight`，
       MSVC 产出 `hblight.dll`，而 MinGW 产出 `libhblight.dll`。
       ⇒ 只认一个名字的话会出现"构建返回 0 但找不到产物"（退出码 3）——
         本轮实测就踩了这一次。**两个都要认。**
    """
    if sys.platform.startswith("win"):
        return ["libhblight.dll", "hblight.dll"]
    if sys.platform == "darwin":
        return ["libhblight.dylib"]
    return ["libhblight.so"]


def 产物名() -> str:
    return 候选名()[0]


def 跑(命令: list[str], 说明: str) -> int:
    _说("  $ " + " ".join(命令))
    返回值 = subprocess.call(命令)
    if 返回值 != 0:
        _说("  !! %s 失败，退出码 %d" % (说明, 返回值))
    return 返回值


def 找产物() -> str | None:
    """cmake 可能把 dll 放在 build/ 或 build/Release/ 或 build/Debug/。"""
    for 子 in ("", "Release", "Debug", "RelWithDebInfo"):
        目录 = os.path.join(工作构建, 子) if 子 else 工作构建
        for 名 in 候选名():
            p = os.path.join(目录, 名)
            if os.path.isfile(p):
                return p
    return None


def main() -> int:
    ap = argparse.ArgumentParser(description="构建 libhblight（轻量引擎 C++ 内核）")
    ap.add_argument("--go", action="store_true", help="真的构建（默认只报告）")
    ap.add_argument("--clean", action="store_true", help="先删掉工作区")
    ap.add_argument("--jobs", type=int, default=0, help="并行度（0=交给 ninja 定）")
    参 = ap.parse_args()

    _说("=" * 70)
    _说("构建 libhblight —— 轻量引擎（pyengine）的 C++ 内核")
    _说("=" * 70)
    _说("  仓库根   ：%s" % 根)
    _说("  源码     ：%s" % 源码)
    _说("  ASCII 工作区：%s" % 工作区)
    _说("  产物名   ：%s" % 产物名())
    _说("  解释器   ：%s（%s）" % (sys.executable, sysconfig.get_platform()))
    _说()

    # ── 前置检查：给人话，不要 traceback ──
    缺 = []
    for 工具 in ("cmake", "ninja"):
        if shutil.which(工具) is None:
            缺.append(工具)
    if 缺:
        _说("!! 缺工具：%s" % "、".join(缺))
        _说("   ⇒ 装一个：cmake / ninja 都在 PATH 上才能构建。")
        _说("     Windows：winget install Kitware.CMake Ninja-build.Ninja")
        return 2
    if not os.path.isfile(os.path.join(源码, "CMakeLists.txt")):
        _说("!! 找不到 %s" % os.path.join(源码, "CMakeLists.txt"))
        return 2

    if not 参.go:
        _说("（预览模式）要真构建就加 --go：")
        _说("    python _build/build_hblight.py --go")
        return 0

    # ── ① 拷到 ASCII 工作区 ──
    if 参.clean and os.path.isdir(工作区):
        _说("[1/4] --clean：删掉 %s" % 工作区)
        shutil.rmtree(工作区, ignore_errors=True)
    os.makedirs(工作区, exist_ok=True)
    _说("[1/4] 把源码拷到 ASCII 工作区（⚠ MinGW 的 ld 不认中文路径）")
    if os.path.isdir(工作源码):
        shutil.rmtree(工作源码, ignore_errors=True)
    shutil.copytree(源码, 工作源码)

    # ── ② configure ──
    _说("[2/4] cmake configure（Ninja）")
    值 = 跑(["cmake", "-S", 工作源码, "-B", 工作构建, "-G", "Ninja",
             "-DCMAKE_BUILD_TYPE=Release"], "configure")
    if 值 != 0:
        return 值

    # ── ③ build ──
    _说("[3/4] cmake build")
    命令 = ["cmake", "--build", 工作构建]
    if 参.jobs:
        命令 += ["-j", str(参.jobs)]
    值 = 跑(命令, "build")
    if 值 != 0:
        return 值

    # ── ④ 产物搬回仓库 ──
    _说("[4/4] 把产物搬回 %s" % os.path.relpath(绑定目录, 根))
    源 = 找产物()
    if 源 is None:
        _说("!! 构建返回 0 但找不到 %s —— 去 %s 里看看" % (产物名(), 工作构建))
        return 3
    os.makedirs(绑定目录, exist_ok=True)
    目标 = os.path.join(绑定目录, 产物名())
    shutil.copyfile(源, 目标)
    大小 = os.path.getsize(目标)
    _说("  ✓ %s  (%d 字节)" % (os.path.relpath(目标, 根), 大小))

    # 自检程序也搬回去 —— diff_python.py 要用它
    自检源 = None
    名 = "hblight_selftest.exe" if sys.platform.startswith("win") else "hblight_selftest"
    for 子 in ("", "Release", "Debug"):
        p = os.path.join(工作构建, 子, 名) if 子 else os.path.join(工作构建, 名)
        if os.path.isfile(p):
            自检源 = p
            break
    if 自检源:
        自检目标 = os.path.join(绑定目录, 名)
        shutil.copyfile(自检源, 自检目标)
        _说("  ✓ %s  (%d 字节)" % (os.path.relpath(自检目标, 根), os.path.getsize(自检目标)))
    else:
        _说("  ⚠ 没找到自检程序 %s（对账要用它）" % 名)

    _说()
    _说("完成。跑对账：python native/hblight/tests/diff_python.py")
    return 0


if __name__ == "__main__":
    sys.exit(main())
