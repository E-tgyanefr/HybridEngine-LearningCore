#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""diff_python.py —— **逐位对账**：同一段脚本，跑 C++ 内核 vs 跑 Python 版引擎。

## 这个脚本证明什么

`native/hblight/tests/selftest.cpp` 跑一段**确定性脚本**，每个标量按 `%.17g` 打印。
本脚本在 **Python 侧**跑**等价的一段**，同样按 `%.17g` 打印，然后**逐行比对**。

`%.17g` 是 IEEE-754 double 的**往返精度**（`float(repr(x)) == x`）⇒
**只要字符串相同，数值就是逐位相同**。所以这不是"看起来差不多"。

## 它不证明什么（⚠ 别过度解读）

  · 不证明**渲染**等价 —— 渲染不在 C++ 里（走 C ABI 回调交回调用方）。
  · 不证明**整个 649 行契约**通过 —— 那是 `tests/test_pyengine.py` 的事。
    本脚本只覆盖 C++ 内核**已经接手**的那部分数值语义。
  · 不证明跨平台等价 —— 只在**本机**（Windows/MinGW ucrt）比过。
    ⚠ 尤其 `hypot`/`cos`/`sin`/`pow` 是 libm 实现，换了运行时**最后一位可能不同**。

## 用法

    python native/hblight/tests/diff_python.py

退出码：0 = 全部逐位相同；非 0 = 有差异（**差异就是发现，不要修输出**）。
"""
from __future__ import annotations

import io
import os
import subprocess
import sys

def _找仓库根() -> str:
    """从本文件往上找，直到看见 `hybridengine_light/`。

    ⚠ **不要写死层级数**（`dirname` 三次/四次）—— 本轮就先写错了一次：
       本文件在 `native/hblight/tests/` 下，是**四层**，不是三层。
       更别说本仓刚吃过"写死绝对路径、目录改了名就静默失效"的亏。
    """
    d = os.path.dirname(os.path.abspath(__file__))
    while True:
        父 = os.path.dirname(d)
        if 父 == d:
            raise RuntimeError("往上找到盘根也没看见 hybridengine_light/")
        d = 父
        if os.path.isdir(os.path.join(d, "hybridengine_light")):
            return d


根 = _找仓库根()
sys.path.insert(0, 根)

# ⚠⚠ Windows 控制台默认是 GBK(cp936)，而本脚本会打 `✓`/`⚠` 这类字符 ⇒
#    `UnicodeEncodeError: 'gbk' codec can't encode character '\u2713'`
#    ⇒ **在"打印结论"这一步崩掉**，而那时对账其实已经通过了
#      （看起来像"对账失败"，实际是打印失败 —— 最难查的一类假红）。
#
#    ⚠ 本轮同一个坑踩了**两次**（先 `build_hblight.py`，再这里）。
#    ⇒ 保留控制台编码（中文照常可读），只把编不了的字符降级成 `?`。
try:
    sys.stdout.reconfigure(errors="replace")
    sys.stderr.reconfigure(errors="replace")
except Exception:
    pass

from hybridengine_light import pyengine as E                     # noqa: E402
from hybridengine_light.pyengine import components as C          # noqa: E402

自检 = os.path.join(根, "hybridengine_light", "pyengine_c",
                    "hblight_selftest.exe" if os.name == "nt" else "hblight_selftest")

行: list[str] = []


def 打(键: str, 值) -> None:
    行.append("%s=%.17g" % (键, float(值)))


def 打2(键: str, a, b) -> None:
    行.append("%s=%.17g,%.17g" % (键, float(a), float(b)))


def 打i(键: str, 值) -> None:
    行.append("%s=%d" % (键, int(值)))


def 打s(键: str, 值: str) -> None:
    行.append("%s=%s" % (键, 值))


def 跑_python() -> list[str]:
    行.clear()

    # ── 1. 父子变换（对应 selftest.cpp 第 1 节）──────────────────────
    根节点 = E.场景().造("根", 10.0, 10.0)
    子 = E.场景对象("子", 5.0, 0.0)
    孙 = E.场景对象("孙", 0.0, 3.0)
    根节点.加子(子)
    子.加子(孙)

    p = 孙.世界位置
    打2("t1.world_pos", p.x, p.y)

    根节点.变换.位置 = E.向量(20.0, 20.0)
    p = 孙.世界位置
    打2("t2.world_pos", p.x, p.y)

    根节点.变换.位置 = E.向量(0.0, 0.0)
    根节点.变换.缩放 = 2.0
    p = 孙.世界位置
    打2("t3.world_pos", p.x, p.y)

    根节点.变换.缩放 = 1.0
    根节点.变换.旋转 = 1.5707963267948966
    p = 孙.世界位置
    打2("t4.world_pos", p.x, p.y)
    打("t4.world_rot", 孙.变换.世界旋转())
    打("t4.world_scale", 孙.变换.世界缩放())

    p = 孙.变换.局部转世界(1.0, 2.0)
    打2("t4.local_to_world", p.x, p.y)

    根节点.变换.旋转 = 0.0
    根节点.变换.缩放 = 3.0
    子.变换.缩放 = 0.5
    打("t5.world_scale", 孙.变换.世界缩放())

    # ── 2. 相机 ───────────────────────────────────────────────────
    def 造相机():
        相 = E.相机2D(x=0.0, y=50.0, 缩放=1.0, 视口=(100.0, 100.0))
        return 相

    目标 = E.场景对象("t", 100.0, 0.0)
    相 = 造相机()
    相.跟随 = 目标
    相.平滑 = 0.2
    for _ in range(120):
        相.每帧(1.0 / 60.0)
    打2("cam1.smooth_60fps_2s", 相.x, 相.y)

    目标2 = E.场景对象("t", 100.0, 0.0)
    相2 = 造相机()
    相2.跟随 = 目标2
    相2.平滑 = 0.2
    for _ in range(240):
        相2.每帧(1.0 / 120.0)
    打("cam1.smooth_120fps_2s", 相2.x)

    目标3 = E.场景对象("t", 200.0, 130.0)
    相3 = 造相机()
    相3.跟随 = 目标3
    相3.平滑 = 0.0
    for _ in range(5):
        相3.每帧(1.0 / 60.0)
    打2("cam2.snap", 相3.x, 相3.y)

    目标4 = E.场景对象("t", 999.0, 999.0)
    相4 = 造相机()
    相4.跟随 = 目标4
    相4.平滑 = 0.0
    相4.边界 = (0.0, 0.0, 300.0, 300.0)
    相4.每帧(1.0 / 60.0)
    打2("cam3.clamp_hi", 相4.x, 相4.y)
    目标4.变换.位置 = E.向量(-999.0, -999.0)
    相4.每帧(1.0 / 60.0)
    打2("cam3.clamp_lo", 相4.x, 相4.y)

    目标5 = E.场景对象("t", 999.0, 999.0)
    相5 = 造相机()
    相5.跟随 = 目标5
    相5.平滑 = 0.0
    相5.边界 = (0.0, 0.0, 40.0, 40.0)
    相5.每帧(1.0 / 60.0)
    打("cam4.center_small_world", 相5.x)

    相6 = E.相机2D(x=50.0, y=50.0, 缩放=1.0, 视口=(100.0, 100.0))
    屏 = 相6.世界转屏幕(60.0, 40.0)
    打2("cam5.world2screen", 屏[0], 屏[1])
    回 = 相6.屏幕转世界(屏[0], 屏[1])
    打2("cam5.screen2world", 回[0], 回[1])

    # ── 3. 输入 ───────────────────────────────────────────────────
    i = E.输入()
    i._按("d")
    打i("in1.down_d", i.按键("d"))
    打i("in1.just_d", i.刚按下("d"))
    i.帧末清理()
    打i("in2.down_d_after_clear", i.按键("d"))
    打i("in2.just_d_after_clear", i.刚按下("d"))

    j = E.输入()
    j._按("d")
    j._按("w")
    d = j.方向()
    打2("in3.diag_dir", d.x, d.y)
    打("in3.diag_len", d.长度())

    k = E.输入()
    d = k.方向()
    打2("in4.empty_dir", d.x, d.y)

    m = E.输入()
    m._按("A")
    打i("in5.case_insensitive", m.按键("a"))

    n = E.输入()
    for 键 in ("a", "left", "d", "right"):
        n._按(键)
    d = n.方向()
    打2("in6.opposite_cancel", d.x, d.y)

    # ── 4. 匀速移动 + 边界反弹 ────────────────────────────────────
    o = E.场景().造("b", 90.0, 0.0)
    mv = C.匀速移动(vx=10.0, vy=0.0, 边界=(0.0, 100.0))
    o.加(mv)
    mv.每帧(1.0)
    打("mv1.x_after_step1", o.变换.位置.x)
    打("mv1.vx_after_step1", mv.vx)
    mv.每帧(1.0)
    打("mv2.x_after_step2", o.变换.位置.x)
    mv.每帧(1.0)
    打("mv3.x_after_step3", o.变换.位置.x)

    o2 = E.场景().造("b2", 0.0, 0.0)
    mv2 = C.匀速移动(vx=10.0, vy=0.0)
    o2.加(mv2)
    for _ in range(10):
        mv2.每帧(0.1)
    打("mv4.uniform_10_steps", o2.变换.位置.x)

    # ── 5. 重力 ───────────────────────────────────────────────────
    o3 = E.场景().造("g", 50.0, 50.0)
    gr = C.重力(加速度=-60.0, 地面=0.0)
    o3.加(gr)
    for _ in range(600):
        gr.每帧(1.0 / 60.0)
    打("gr1.y_after_600", o3.变换.位置.y)
    打("gr1.vy_after_600", gr.vy)
    打i("gr1.on_ground", gr.在地面)

    gr2 = C.重力(加速度=-60.0, 地面=0.0)
    gr2.vy = 10.0
    gr2.在地面 = False
    打i("gr2.jump_in_air", gr2.跳(22.0))

    gr3 = C.重力(加速度=-60.0, 地面=0.0)
    打i("gr3.jump_on_ground", gr3.跳(22.0))
    打("gr3.vy_after_jump", gr3.vy)

    # ── 6. 计时器 / 闪烁 / 尾迹 ───────────────────────────────────
    触发: list[int] = []
    tm = C.计时器(间隔=0.1, 回调=lambda 宿, n: 触发.append(n))
    o4 = E.场景().造("tm")
    o4.加(tm)
    首步: list[int] = []
    for _ in range(25):
        before = len(触发)
        tm.每帧(0.1)
        首步.append(len(触发) - before)
    打i("tm1.total_triggers", len(触发))
    打i("tm1.count_field", tm.次数)
    打i("tm1.first_step", 首步[0])

    o5 = E.场景().造("bl")
    bl = C.闪烁(周期=0.1, 颜色a="红", 颜色b="白")
    o5.加(bl)
    翻转1 = bl.每帧(0.15)
    打i("bl1.step_0.15", 1 if bl.当前 == "白" else 0)
    打i("bl1.phase_after", 1 if bl.当前 == "白" else 0)
    bl.每帧(0.15)
    打i("bl1.step_0.15_again", 1 if bl.当前 == "红" else 0)
    打i("bl1.phase_after2", 0 if bl.当前 == "红" else 1)

    # 尾迹：数**真的触发了几次**（替换 `延后加根` 计数，用的是真组件）
    s6 = E.场景()
    o6 = s6.造("tr", 10.0, 10.0)
    tr = C.尾迹(间隔=0.01, 寿命=0.05)
    o6.加(tr)
    次数 = []
    原 = s6.延后加根
    s6.延后加根 = lambda *o: (次数.append(1), 原(*o))[1]
    for _ in range(30):
        tr.每帧(0.01)
    打i("tr1.trails_in_30_steps", len(次数))

    # ── 7. 圆形碰撞 ───────────────────────────────────────────────
    sa = E.场景()
    a = sa.造("a", 0.0, 0.0)
    b = sa.造("b", 3.0, 4.0)
    ca, cb = C.圆形碰撞(半径=3.0), C.圆形碰撞(半径=2.0)
    a.加(ca)
    b.加(cb)
    打i("hit1.dist5_r3_r2", ca.碰到(cb))
    b.变换.位置 = E.向量(4.0, 4.0)
    打i("hit2.dist5.657_r3_r2", ca.碰到(cb))
    c2 = sa.造("c", 10.0, 0.0)
    cc = C.圆形碰撞(半径=1.0)
    c2.加(cc)
    ca2 = C.圆形碰撞(半径=1.0)
    a.加(ca2)
    打i("hit3.no_overlap", ca2.碰到(cc))

    # ── 8. 固定步长（镜像 engine.py `跑` 里那两行累积器）──────────
    def 累积帧数(步长: float, 经过: float) -> int:
        累积 = 0.0
        累积 = min(累积 + 经过, 步长 * 8)     # engine.py: `累积 = min(累积 + 经过, self.步长 * 8)`
        k = 0
        while 累积 >= 步长:                   # engine.py: `while 累积 >= self.步长`
            累积 -= 步长
            k += 1
        return k

    打i("fx1.frames_from_1s", 累积帧数(1.0 / 60.0, 1.0))
    打i("fx2.catchup_cap", 累积帧数(1.0 / 60.0, 10.0))

    # ── 9. 场景树 ─────────────────────────────────────────────────
    s = E.场景("主场景")
    a = s.造("a", 0.0, 0.0)
    s.造("b", 0.0, 0.0)
    a1 = E.场景对象("a1")
    a2 = E.场景对象("a2")
    a.加子(a1)
    a.加子(a2)
    打i("sc1.node_count", sum(1 for _ in s.遍历()))
    打i("sc1.find_a1_is_child0", 1 if s.找("a1") is a.子对象[0] else 0)
    打i("sc1.find_missing", 1 if s.找("nope") is None else 0)

    bb = s.找("b")
    bb.加子(a2)
    打i("sc2.a_child_count", len(a.子对象))
    打i("sc2.b_child_count", len(bb.子对象))
    打i("sc2.a2_parent_is_b", 1 if a2.父亲 is bb else 0)

    s.延后加根(E.场景对象("late"))
    打i("sc3.roots_before_merge", len(s.根))
    s._并入延后的()
    打i("sc3.roots_after_merge", len(s.根))

    a.活着 = False
    打i("sc4.alive_roots_before", sum(1 for r in s.根 if r.活着))
    s.根 = [r for r in s.根 if r.活着]
    打i("sc4.roots_after_cull", len(s.根))

    # ── 10. 颜色 ──────────────────────────────────────────────────
    for 名 in ("黑", "白", "红", "green", "#ff0000", "不存在的颜色", " 红 "):
        打s("color[%s]" % 名, E.颜色(名))

    # ── 11. 画笔（相机换算 + 缩放）────────────────────────────────
    class 收:
        def __init__(self):
            self.圆, self.矩, self.线, self.字 = 0, 0, 0, 0
            self.首圆 = None
            self.首字号 = 0

        def 清屏(self, 色): pass

        def 画圆(self, x, y, r, 色):
            if self.圆 == 0:
                self.首圆 = (x, y, r)
            self.圆 += 1

        def 画矩形(self, x, y, w, h, 色): self.矩 += 1
        def 画线(self, x1, y1, x2, y2, 色, 粗): self.线 += 1

        def 画文字(self, x, y, 内容, 色, 字号):
            self.首字号 = 字号
            self.字 += 1

        def 呈现(self): pass
        def 关闭(self): pass

        @property
        def 还开着(self): return True

    r1 = 收()
    相b = E.相机2D(x=50.0, y=50.0, 缩放=2.0, 视口=(100.0, 100.0))
    笔 = E.画笔接口(r1, 相b)
    笔.圆(30.0, 40.0, 5.0, "红")
    笔.矩形(10.0, 10.0, 4.0, 6.0, "蓝")
    笔.线(0.0, 0.0, 1.0, 1.0, "黑", 3.0)
    笔.文字(0.0, 0.0, "x", "黑", 12)
    打i("br1.circles", r1.圆)
    打2("br1.first_circle_xy", r1.首圆[0], r1.首圆[1])
    打("br1.first_circle_r_scaled", r1.首圆[2])
    打i("br1.rects", r1.矩)
    打i("br1.lines", r1.线)
    打i("br1.texts", r1.字)
    打i("br1.text_size_unscaled", r1.首字号)

    r2 = 收()
    笔2 = E.画笔接口(r2, None)
    笔2.圆(7.0, 8.0, 3.0, "红")
    打2("br2.passthrough_xy", r2.首圆[0], r2.首圆[1])
    打("br2.passthrough_r", r2.首圆[2])

    return list(行)


def main() -> int:
    if not os.path.isfile(自检):
        print("!! 找不到 C++ 自检程序：%s" % 自检)
        print("   ⇒ 先构建：python _build/build_hblight.py --go")
        return 2

    cpp = subprocess.run([自检], capture_output=True, text=True,
                         encoding="utf-8", errors="replace")
    if cpp.returncode != 0:
        print("!! C++ 自检自身退出码 %d —— 先修它，再谈对账" % cpp.returncode)
        print(cpp.stdout)
        return 3
    cpp行 = [x for x in (cpp.stdout or "").splitlines() if x and not x.startswith("#")]
    py行 = [x for x in 跑_python() if x and not x.startswith("#")]

    print("C++ 行数=%d   Python 行数=%d" % (len(cpp行), len(py行)))
    if len(cpp行) != len(py行):
        print("!! 行数不同 —— 两边的脚本已经不同步了（不是数值差异，是脚本差异）")
        only_cpp = [x for x in cpp行 if x not in set(py行)]
        only_py = [x for x in py行 if x not in set(cpp行)]
        for x in only_cpp[:20]:
            print("   只在 C++ : %s" % x)
        for x in only_py[:20]:
            print("   只在 Py  : %s" % x)
        return 4

    不同 = [(i, a, b) for i, (a, b) in enumerate(zip(cpp行, py行), 1) if a != b]
    if not 不同:
        print("✓ 全部 %d 行**逐位相同**" % len(cpp行))
        print("   （两边都用 %s 往返精度打印 ⇒ 同一个 double）" % "%.17g")
        return 0

    print("!! 有 %d 行不同：" % len(不同))
    for i, a, b in 不同:
        print("   行%d" % i)
        print("      C++ : %s" % a)
        print("      Py  : %s" % b)
    return 1


if __name__ == "__main__":
    sys.exit(main())
