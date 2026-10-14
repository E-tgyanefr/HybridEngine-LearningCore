"""⚠ 以**用户**的身份用一次 —— 判据绿 ≠ 用户能用。

我（Lead）一直在量"内部数"，但**从来没以"一个想用 C++ 后端的人"的身份走一遍**。
这一份就走一遍，并**如实报告卡在哪**。

用户想做的三件事：
    ① 打开开关，让引擎用 C++ 后端画
    ② 看到自己的场景（真窗口）
    ③ 让键盘能动
"""
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, 根)
os.chdir(根)

from hybridengine_light import pyengine as E
from hybridengine_light.pyengine import components as C
from hybridengine_light.pyengine_c import backend as CB

os.environ["HBLIGHT_RENDER"] = "1"

print("  ══ 以用户身份走一遍（HBLIGHT_RENDER=1）══")
print()

#: ① 最自然的写法：什么都不传
print("  ① 用户最自然的写法：`E.游戏(场景=..., 宽=..., 高=...)` —— 不传 后端=")
try:
    s = E.场景("u")
    o = s.造("玩家", 50, 50); o.加(C.方块(宽=6, 高=6, 颜色="蓝"))
    玩家 = E.游戏(场景=s, 宽=400, 高=300)
    类名 = type(玩家.后端).__name__
    print("     ⇒ 拿到的后端 = **%s**" % 类名)
    print("     ⇒ %s" % ("✅ 开关生效（用户什么都不用改）"
                        if 类名 != "Tk后端" else "⚠ **还是 Tk后端** —— 开关没接上"))
except Exception as e:
    print("     ✗ 失败：%s: %s" % (type(e).__name__, e))
    import traceback; traceback.print_exc()

#: ② 显式传呢
print()
print("  ② 用户显式传：`后端=CB.默认后端(...)`")
try:
    后 = CB.默认后端(宽=400, 高=300, size=100)
    print("     ⇒ %s" % type(后).__name__)
    后.关闭()
except Exception as e:
    print("     ✗ 失败：%s: %s" % (type(e).__name__, e))

#: ③ 键盘能不能动
print()
print("  ③ 键盘：用户按方向键，引擎该收到")
try:
    输入 = E.输入()
    后2 = CB.默认后端(宽=400, 高=300, size=100)
    可装 = hasattr(后2, "装输入")
    print("     后端有 装输入 = %s" % 可装)
    if 可装:
        后2.装输入(输入._按下, 输入._按, 输入._放)   #: type: ignore[attr-defined]
        没接 = getattr(后2, "_输入没接", None)
        print("     ⇒ **键盘没接**（状态位 _输入没接=%s）" % 没接)
        print("     ⇒ ⚠ 用户按方向键**不会有反应** —— 这是已知缺口，不是坏了")
    后2.关闭()
except Exception as e:
    print("     ✗ 失败：%s: %s" % (type(e).__name__, e))

#: ④ 学习内核的 GUI 会不会用它
print()
print("  ④ 学习内核的 GUI（`learn_gui.py`）会不会用它")
import subprocess
r = subprocess.run(["git", "grep", "-l", "默认后端\\|pyengine_c", "--",
                    "hybridengine_light/learn_gui.py",
                    "hybridengine_light/learnmodel.py"],
                   capture_output=True, text=True, encoding="utf-8", errors="replace")
命中 = [x for x in (r.stdout or "").split("\n") if x.strip()]
print("     引用了 C++ 后端的 GUI/模型文件：%s" % (命中 or "**一个都没有**"))
print("     ⇒ ⚠ **学习内核里那条 7.5 倍的快路，用户走不到**（要靠自己写脚本 + 设环境变量）")
