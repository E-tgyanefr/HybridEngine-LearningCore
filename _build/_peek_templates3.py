# 直接调各线的模板生成器，量出"第一次打开答题文件"有多少行、含多少讲解。
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light import learnmodel as M                  # noqa: E402
from hybridengine_light import run as R                          # noqa: E402

关键 = ("先介绍", "轮到你了", "常见坑", "怎么自己测", "这一课")
print("  %-13s %-7s %-9s %s" % ("课程线", "模板行数", "含讲解", "模板开头两行"))
print("  " + "─" * 92)


def 量(键):
    课们 = M.发现(键)
    if not 课们:
        return None
    k = 课们[0]
    if 键 == "python":
        return R._stub_for(k.源文件)
    if 键 in ("ai", "embedded", "raspi"):
        模 = {"ai": "aicourse", "embedded": "embedded", "raspi": "raspi_course"}[键]
        m = __import__(f"hybridengine_light.{模}", fromlist=["x"])
        return m._生成模板(k.源文件)
    if 键 == "enginecourse":
        from hybridengine_light import enginecourse as EC
        for 名 in ("_生成模板", "_模板"):
            if hasattr(EC, 名):
                return getattr(EC, 名)(k.源文件)
        return None
    # 编译型：模板就写在课程文件的 `模板 => """…"""` 里
    import re
    文 = k.源文件.read_text(encoding="utf-8")
    mm = re.search(r'模板 => """(.*?)""";', 文, re.S)
    return mm.group(1) if mm else None


for 键 in list(M.即时轨道) + list(M.编译型轨道):
    try:
        文 = 量(键)
    except Exception as e:
        文 = None
        print("  %-13s 取模板失败：%s: %s" % (键, type(e).__name__, e))
        continue
    if 文 is None:
        print("  %-13s （拿不到模板）" % 键)
        continue
    行 = 文.splitlines()
    命中 = [x for x in 关键 if x in 文]
    前 = " / ".join(l.strip() for l in 行[:2] if l.strip())[:46]
    print("  %-13s %5d 行  %d/%d 项   %s" % (键, len(行), len(命中), len(关键), 前))
