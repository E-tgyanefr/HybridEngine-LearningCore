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

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, ROOT)
os.chdir(ROOT)

from hybridengine_light import learnmodel as M  # noqa: E402

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
    if 轨 in 答题区:
        shutil.rmtree(os.path.join(ROOT, 答题区[轨]), ignore_errors=True)

    好 = 坏 = 0
    问题 = []
    for 课 in 课们:
        t0 = time.monotonic()
        try:
            答 = M.参考答案(课)
            报 = M.检查(课, 答) or ""
            错 = 报.count("✗")
        except Exception as e:                                  # noqa: BLE001
            错, 报 = 1, "异常：%s: %s" % (type(e).__name__, e)
        用 = time.monotonic() - t0
        if 错:
            坏 += 1
            #: 取前两条 ✗ 的行当指纹
            指纹 = [l.strip()[:70] for l in 报.split("\n") if "✗" in l][:2]
            问题.append("课 %s：✗%d  %s（%.1fs）"
                        % (课.编号, 错, " | ".join(指纹), 用))
        else:
            好 += 1
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
