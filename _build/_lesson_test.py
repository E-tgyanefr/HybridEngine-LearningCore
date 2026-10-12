"""通用课验证器：给定课号和若干「参考答案 / 错答案」，报告正反验证结果。

用法（在 _build 下）：
    python _lesson_test.py <轨道> <课号> <用例文件.json>

用例文件格式（UTF-8 JSON）：
    {
      "对": "完整参考答案代码",
      "错": [["标签", "错误代码"], ...]
    }

⚠ 判定同时认两种输出写法：
    「全部通过（2/2）」  全角括号
    「通过 4/5。」
"""
import io
import json
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, LC)
os.chdir(LC)
from hybridengine_light import learnmodel as M  # noqa: E402


def 答案路径(轨道, 号):
    for k in M.发现(轨道):
        if k.编号 == 号:
            return M._答案文件(k)
    return None


def 取结果(出):
    m = re.search(r"全部通过（\s*(\d+)\s*/\s*(\d+)\s*）", 出)
    if not m:
        m = re.search(r"通过\s*(\d+)\s*/\s*(\d+)", 出)
    return (int(m.group(1)), int(m.group(2))) if m else None


def main():
    轨道, 号, 用例 = sys.argv[1], sys.argv[2], sys.argv[3]
    P = 答案路径(轨道, 号)
    if P is None:
        print("  XX 找不到课 %s/%s" % (轨道, 号))
        return 1
    if not P.is_file():
        M.读答案([k for k in M.发现(轨道) if k.编号 == 号][0])   # 生成模板
    数据 = json.loads(io.open(用例, encoding="utf-8").read())
    备份 = io.open(P, encoding="utf-8").read() if P.is_file() else None
    env = dict(os.environ, PYTHONIOENCODING="utf-8")

    def 跑(代码):
        io.open(P, "w", encoding="utf-8", newline="").write(代码)
        r = subprocess.run([sys.executable, "-m", "hybridengine_light", "检查", 号],
                           cwd=LC, env=env, capture_output=True, timeout=300)
        return (r.stdout + r.stderr).decode("utf-8", "replace")

    try:
        出 = 跑(数据["对"])
        r = 取结果(出)
        if r is None:
            print("  ?? 拿不到结果（输出里没有「通过 N/M」）")
            print("     " + "\n     ".join(出.strip().split("\n")[:8]))
            return 1
        print("  参考答案：%d/%d  ⇒ %s" % (r[0], r[1], "全过 OK" if r[0] == r[1] else "**有没过的**"))
        坏 = r[0] != r[1]
        if 坏:
            for l in 出.split("\n"):
                if l.strip().startswith("×"):
                    print("      " + l.strip()[:84])

        好 = 0
        错们 = 数据.get("错", [])
        for 标签, 码 in 错们:
            出2 = 跑(码)
            r2 = 取结果(出2)
            if r2 is None:
                好 += 1
                print("    OK  %-20s （加载/运行就出错 ⇒ 被拒）" % 标签)
                continue
            拒 = r2[0] < r2[1]
            好 += 1 if 拒 else 0
            第一 = next((l.strip()[1:56] for l in 出2.split("\n")
                        if l.strip().startswith("×")), "")
            print("    %s  %-20s %d/%d  %s"
                  % ("OK" if 拒 else "XX", 标签, r2[0], r2[1], 第一))
        print("  ⇒ 错答案 %d/%d 被拒；参考答案 %s"
              % (好, len(错们), "过" if not 坏 else "**没过**"))
        return 0 if (not 坏 and 好 == len(错们)) else 1
    finally:
        if 备份 is not None:
            io.open(P, "w", encoding="utf-8", newline="").write(备份)


if __name__ == "__main__":
    sys.exit(main())
