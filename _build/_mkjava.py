"""把 Java 子代理第 1/4 批（00、01、02 课）落成稿子文件。

⚠ 后续批次会追加到同一个文件，最后一次性落盘。
"""
import os
import io
import sys
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
P = os.path.join(LC, "_build", "_java_模板扩写.md")
print("  目标文件：%s" % P)
print("  这一批的内容由父代理从子代理消息里原样抄入。")
