# 验证：逐字符扫描（能识别 \" 转义）解析课程表，是否与 dotnet 完全一致。
import io
import os
import re
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
sys.path.insert(0, LC)
os.chdir(LC)

from hybridengine_light import learnmodel as M                  # noqa: E402

属性头 = re.compile(r'\[\s*(?:\w*课程|课程)\s*\(')


def 取字符串(文: str, i: int) -> tuple[str, int]:
    """从 `文[i] == '"'` 开始读一个 C# 字符串字面量，返回 (值, 结束位置)。

    ⚠⚠ 不能用 `"([^"]+)"` 这种正则 —— C# 里的引号可以**转义**：
         [课程("07", "第 07 课：字符串 —— 'A' 和 \"A\" 不是一回事")]
      正则遇到第一个 `\"` 就断了，取到 "第 07 课：字符串 —— 'A' 和 \\"
      （C++ 第 07 课实际就是这么断的）。
    """
    出 = []
    i += 1                                  # 跳过开头的 "
    while i < len(文):
        c = 文[i]
        if c == "\\" and i + 1 < len(文):
            n = 文[i + 1]
            出.append({"n": "\n", "t": "\t", '"': '"', "\\": "\\",
                       "r": "\r"}.get(n, n))
            i += 2
            continue
        if c == '"':
            return "".join(出), i + 1
        出.append(c)
        i += 1
    return "".join(出), i


def 解析一课(文: str) -> tuple[str, str] | None:
    m = 属性头.search(文)
    if not m:
        return None
    i = m.end()
    值 = []
    for _ in range(2):                      # 前两个参数是 编号 和 标题
        while i < len(文) and 文[i] in " \t\r\n":
            i += 1
        if i >= len(文) or 文[i] != '"':
            return None
        s, i = 取字符串(文, i)
        值.append(s)
        while i < len(文) and 文[i] in " \t\r\n":
            i += 1
        if i < len(文) and 文[i] == ",":
            i += 1
    return (值[0], 值[1])


def 直接解析(工程: str) -> list[tuple[str, str]]:
    d = os.path.join(工程, "Lessons")
    if not os.path.isdir(d):
        return []
    出 = []
    for f in sorted(os.listdir(d)):
        if f.endswith(".cs"):
            r = 解析一课(io.open(os.path.join(d, f), encoding="utf-8").read())
            if r:
                出.append(r)
    return 出


工程名 = {"csharp": "cs-lessons", "cpp": "cpp-lessons",
          "java": "java-lessons", "c": "c-lessons"}

全部一致 = True
for 键 in M.编译型轨道:
    工程 = os.path.join(LC, "csharp", 工程名[键])
    t0 = time.time()
    快 = 直接解析(工程)
    快耗时 = time.time() - t0
    t0 = time.time()
    慢 = M.发现(键)
    慢耗时 = time.time() - t0
    慢列 = [(k.编号, k.标题) for k in 慢]
    一致 = 快 == 慢列
    全部一致 = 全部一致 and 一致
    print("  %-7s 直接解析 %2d 课（%.3f 秒）· dotnet %2d 课（%.2f 秒）· %s"
          % (键, len(快), 快耗时, len(慢), 慢耗时, "一致" if 一致 else "**不一致**"))
    if not 一致:
        for i in range(max(len(快), len(慢))):
            a = 快[i] if i < len(快) else None
            b = 慢列[i] if i < len(慢列) else None
            if a != b:
                print("        XX %d 直接=%r" % (i + 1, a))
                print("             dotnet=%r" % (b,))

print()
print("  结论：%s" % ("可以切换（完全一致，快 %.0f 倍）"
                    if 全部一致 else "仍有差异，**不能**切换"))
