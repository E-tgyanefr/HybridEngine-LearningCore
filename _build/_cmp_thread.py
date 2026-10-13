# 对比：M.发现(键) 在主线程 vs 后台线程 里返回什么。
#
# 症状：`__诊断__`（主线程）看到 cpp=8、java=8，
#       而 GUI 的后台线程里这几条是 0，只有 csharp=8 对。
import os
import sys
import threading

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light import learnmodel as M                  # noqa: E402

print("  M.编译型轨道 =", M.编译型轨道)
print()
print("  === 主线程 ===")
for 键 in M.编译型轨道:
    try:
        课 = M.发现(键)
        print("    %-8s %d 课" % (键, len(课)))
        if 课:
            print("             第一节：%s" % 课[0].显示())
    except Exception as e:
        print("    %-8s 抛异常 %s: %s" % (键, type(e).__name__, e))

print()
print("  === 后台线程（和 GUI 一样）===")
结果 = {}

def 跑():
    for 键 in M.编译型轨道:
        try:
            结果[键] = len(M.发现(键))
        except Exception as e:
            结果[键] = "%s: %s" % (type(e).__name__, e)

t = threading.Thread(target=跑)
t.start()
t.join()
for 键 in M.编译型轨道:
    print("    %-8s %s" % (键, 结果.get(键)))
