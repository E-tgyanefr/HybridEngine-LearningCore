"""⭐ **「参考答案全过」判据** —— 拿每一课的参考答案去跑它自己的检查。

═══ 为什么需要它（2026-10-14 实测的事故）═══

这个仓里**有**反面验证（拿错答案去验"检查抓不抓得住"），
**但没有**正面验证（拿参考答案去验"检查放不放得过"）。

⚠ 后果实测：

    编译型四条线（c / cpp / csharp / java）里，**c 和 csharp 的参考答案
    过不了自己的检查**：

        c        第 00 课  4/5   ✗ 程序能编译、能跑起来
        csharp   第 00 课  2/5   ✗ 三行都打印了（实际是「没输出」）

    根因（已修的那一半）：`参考答案()` 返回的是课程程序 `答案` 子命令的
    **展示文本**（前面带 `── 参考答案 ────` 那样的框线抬头），
    而它**同时被当作可以直接提交的代码**用 ⇒ 编译器报
    `stray '\\342' in program`（`\\342` 就是 `─` 的 UTF-8 首字节）。
    ⇒ 已在 `learnmodel.剥装饰()` 修掉。

⚠⚠ **为什么一直没人发现**：几条线**各有各的反面验证器**，
  而那些验证器是**从课件源码的错例表里取代码**的 —— 那条路**绕过** `参考答案()`。
  ⇒ 所以"参考答案能过"这件事**从来没被验证过**。

═══ 这个脚本验什么 ═══

    对每一课：`M.检查(课, M.参考答案(课))` —— 走**学员真正走的那条路**，
    报告里有 `✗` 就判**不过**。

⚠ 它**故意不复用反面验证器**的取码方式：那个方式绕过了 `参考答案()`，
  而这条判据要验的**正是 `参考答案()` 这条路**。

═══ 用法 ═══

    python _build/_verify_reference_all.py              # 四条编译型线（慢：每课要编译）
    python _build/_verify_reference_all.py --轨 c       # 只跑一条
    python _build/_verify_reference_all.py --课 00      # 只跑每条的某一课
    python _build/_verify_reference_all.py --全部        # 连 Python 系（快）
"""
from __future__ import annotations

import os
import shutil
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, ROOT)
os.chdir(ROOT)

from hybridengine_light import learnmodel as M  # noqa: E402

#: ⚠⚠⚠ **报告里的"通过/不通过"记号 —— 这里踩过一次假绿，别删。**
#:
#:   第一版我数的是 `报.count("✗")`，而**只有编译型四条线**用 ✓ / ✗
#:   （那是 C# 课程程序打的）。**Python 线用的是 √ / ×**：
#:
#:       hybridengine_light/check.py:187
#:       lines.append(f"  {'√' if r.ok else '×'} {r.name}")
#:
#:   ⇒ `报.count("✗")` 对 python 线**恒等于 0** ⇒ 它**永远报"过"**。
#:   ⚠ 于是「`--轨 python` 55 过 0 不过」是**假绿**，不是真过。
#:   ⚠ 队友 `learning-verify` 查出来的 —— 而我当时差点让它拿这个假绿交差。
#:
#:   ⚠⚠ 这个坑**本仓已经踩过并记下来了**，就在 `learn_gui.py:1483`：
#:     「⚠ 实测踩到：我只写了 ✓，而课程报告实际用 √（另一处用 ✅）」
#:   ⇒ 那张表是**现成的**，我第一版却只写了一个字符。
#:   ⇒ 教训：**"没跑到"和"过了"不能长得一样** —— 所以下面还有一条自检。
通过记号 = ("√", "✓", "✔", "✅", "通过")
不通过记号 = ("✗", "✕", "✘", "❌", "×", "没能通过", "编译失败")


def 报告里有记号吗(报: str) -> bool:
    """报告里到底有没有"过/不过"的记号。

    ⚠ 没有记号的报告**不是"全过"** —— 它可能是"根本没跑到"
      （比如答题文件写坏了 ⇒ 加载失败 ⇒ 报告只有一句说明文字）。
      这两种情况必须分开判，否则就是假绿。
    """
    #: ⚠ 同样按**行首**认 —— 正文里出现 `×`（乘号）不算"跑到了"。
    for l in 报.split("\n"):
        s = l.strip()
        if s.startswith(通过记号) or s.startswith(不通过记号):
            return True
    return False


def 数不过(报: str) -> int:
    """报告里"不过"的记号个数。

    ⚠⚠⚠ **必须按【行首】判，不能用【全串计数】** —— 这里栽过两次：

      第一次（假绿）：只数 `✗`，而 python 线用 `√`/`×` ⇒ 恒等于 0 ⇒ 永远报"过"。
      第二次（假红）：改成数一组符号，但用的是**全串 `count`**，
        而 **`×` 在中文里还是乘号** —— 好几课的**检查名字里就带 `×`**：

            课 45  六棵树 × 三种遍历都对（含空树、单节点、满树、缺孩子、斜树）
            课 46  八组都对（含空迷宫、起点=终点、走不通、一行、50×50 全空 98 步）
            课 48  6 组都和暴力枚举一致（含 1×1、单行、单列）
            课 50  3×3 的 512 种网格全试过，一块都没数错

        ⇒ 这 4 课**参考答案其实全过**，却各被判一次"不过"（每课全串 `×=1`、行首 `×=0`）。

    ⇒ 教训：`√`/`×` 是**行首记号**，不是正文里随便出现的字符。
      正文里的 `×`（乘号）、`✓`（说明文字里的对勾）都不该算数。
    """
    return sum(1 for l in 报.split("\n")
               if l.strip().startswith(("✗", "✕", "✘", "❌", "×")))

#: ⚠ 编译型的答题区 —— 跑之前要清，因为这些线的验证器会 `rmtree` 它，
#:   残留别的答案会让这条判据**假红**（我第一次就被这个坑了）。
答题区 = {
    "c": "c-answers",
    "cpp": "cpp-answers",
    "csharp": "csharp-answers",
    "java": "java-answers",
    "python": "lessons/answers",
}

编译型 = ("c", "cpp", "csharp", "java")
Python系 = ("python", "enginecourse", "ai", "embedded", "raspi", "games")


def 跑一条(轨: str, 限课: str | None = None) -> tuple[int, int, list[str]]:
    """返回 (过, 不过, 不过的说明列表)。"""
    课们 = M.发现(轨)
    if 限课:
        课们 = [c for c in 课们 if c.编号 == 限课]
    if not 课们:
        return 0, 0, ["（这条线没有课）"]

    #: ⚠ 清答题区 —— 不清会被上一条线的残留弄成假红
    #:
    #: ⚠⚠ **清之前先备份，跑完还原** —— 否则会把别的判据弄挂（实测）：
    #:   本判据走 `M.检查(课, M.参考答案(课))`，而 `M.检查` 会 `保存答案(...)`
    #:   ⇒ **把参考答案写进了答题区**。而 `读答案()` 看到文件就直接返回、
    #:     不再生成模板 ⇒ `tests/test_gui_model.py` 那条
    #:     「每条线都能生成非空的答题模板」读到的是参考答案（3 行）⇒ 报红：
    #:
    #:         FAIL: test_每条线都能生成非空的答题模板
    #:               python：模板只有 3 行非空（像没生成）
    #:
    #:   ⚠ 这类"共享状态被清了不还原"在本仓**踩过第二次**了
    #:     （第一次是 `tests/验收.py`）⇒ 判据脚本必须自己收尾。
    _备份目录 = None
    if 轨 in 答题区:
        _原 = os.path.join(ROOT, 答题区[轨])
        if os.path.isdir(_原):
            _备份目录 = _原 + ".判据备份"
            shutil.rmtree(_备份目录, ignore_errors=True)
            try:
                shutil.copytree(_原, _备份目录)
            except OSError:
                _备份目录 = None
        shutil.rmtree(_原, ignore_errors=True)

    好 = 坏 = 0
    问题 = []
    for 课 in 课们:
        t0 = time.monotonic()
        try:
            答 = M.参考答案(课)
            报 = M.检查(课, 答) or ""
            #: ⚠⚠ **两条都要判**：有"不过"记号 ⇒ 不过；
            #:   **一个记号都没有 ⇒ 也算不过**（那是"没跑到"，不是"过了"）。
            if not 报告里有记号吗(报):
                错 = 1
                报 = ("⚠ **报告里一个记号都没有 —— 判「没跑到」，不是「过了」**\n"
                      "      （常见原因：答题文件写坏了 ⇒ 加载失败 ⇒ 报告只剩说明文字）\n"
                      + 报[:300])
            else:
                错 = 数不过(报)
        except Exception as e:                                  # noqa: BLE001
            错, 报 = 1, "异常：%s: %s" % (type(e).__name__, e)
        用 = time.monotonic() - t0
        if 错:
            坏 += 1
            #: 取前两条 ✗ 的行当指纹
            指纹 = [l.strip()[:70] for l in 报.split("\n")
                    if l.strip().startswith(("✗", "✕", "✘", "❌", "×"))][:2]
            if not 指纹:
                指纹 = [l.strip()[:70] for l in 报.split("\n") if "没跑到" in l][:1]
            问题.append("课 %s：✗%d  %s（%.1fs）"
                        % (课.编号, 错, " | ".join(指纹), 用))
        else:
            好 += 1

    #: ⚠ 收尾：把答题区还原成跑之前的样子（见上面那段说明）
    if _备份目录:
        _原 = os.path.join(ROOT, 答题区[轨])
        shutil.rmtree(_原, ignore_errors=True)
        try:
            shutil.move(_备份目录, _原)
        except OSError:
            shutil.rmtree(_备份目录, ignore_errors=True)
    return 好, 坏, 问题


def main() -> int:
    参 = sys.argv[1:]
    限课 = None
    if "--课" in 参:
        限课 = 参[参.index("--课") + 1]
    轨们 = list(编译型)
    if "--轨" in 参:
        轨们 = [参[参.index("--轨") + 1]]
    elif "--全部" in 参:
        轨们 = list(编译型) + list(Python系)

    print("  ══════════════════════════════════════════════════════")
    print("   「参考答案全过」判据 —— 拿参考答案跑它自己的检查")
    print("  ⚠ 这条路**绕过**反面验证器；而反面验证器绕过 `参考答案()`")
    print("  ⚠ 记号：编译型四条线用 ✓/✗；**Python 线用 √/×** —— 两边都认（踩过假绿）")
    print("  ══════════════════════════════════════════════════════")
    if 限课:
        print("  只跑课号：%s" % 限课)
    print()

    总好 = 总坏 = 0
    全问题 = []
    for 轨 in 轨们:
        t0 = time.monotonic()
        好, 坏, 问题 = 跑一条(轨, 限课)
        总好 += 好
        总坏 += 坏
        print("  %-12s %3d 过 · %3d 不过   （%.0f 秒）"
              % (轨, 好, 坏, time.monotonic() - t0))
        for x in 问题[:6]:
            print("      %s" % x)
        if len(问题) > 6:
            print("      …… 还有 %d 课" % (len(问题) - 6))
        全问题 += 问题

    print()
    print("  ══════════════════════════════════════════════════════")
    if 总坏:
        print("  结论：**有 %d 课的参考答案过不了自己的检查** ❌" % 总坏)
        print("     ⚠ 这不是「学员写错了」—— 是**参考答案或检查本身**的问题。")
        print("     ⚠ 也别只看这一条：反面验证可能全过（它走的是另一条路）。")
    else:
        print("  结论：参考答案全部通过 ✅（%d 课）" % 总好)
    print("  ══════════════════════════════════════════════════════")
    return 1 if 总坏 else 0


if __name__ == "__main__":
    sys.exit(main())
