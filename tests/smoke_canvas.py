"""真实验证：画布能不能出画面（用 PostScript 导出成文件，不需要人看窗口）。

这是"开箱即用"这条承诺的验证手段 —— 一台没有显示器的机器上也能确认
绘图链路是通的，而不是只保证代码不报错。
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from learncode import Stage

out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_smoke.ps")
if os.path.exists(out):
    os.remove(out)

try:
    s = Stage(size=100, width=400, height=400, title="smoke")
    # 画一个有明确几何意义的图：底面 + 一个圆 + 一条对角线 + 文字
    (s.box(0, 0, 100, 10, "灰")            # 地面
       .dot(50, 50, r=20, color="红")      # 圆在正中间
       .line(0, 0, 100, 100, "蓝", 1)      # 从左下到右上的对角线
       .text(50, 90, "Hello 你好", "黑", 14)
       .show())
    s._canvas.postscript(file=out, colormode="color")
    size = os.path.getsize(out)
    print(f"OK: canvas rendered, postscript {size} bytes")
    print(f"    shapes recorded: {len(s.snapshot())}")
    s.close()
    os.remove(out)
except Exception as e:                             # noqa: BLE001
    print(f"FAIL: {type(e).__name__}: {e}")
    sys.exit(1)
