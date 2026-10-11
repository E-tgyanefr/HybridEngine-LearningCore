# 反面验证：喂**错误答案**给 C 课的检查，必须报错。
import io
import os
import re
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
S = os.path.join(LC, "csharp", "c-lessons", "Lessons")
ANS = os.path.join(LC, "c-answers")
env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"

错例 = [
    ("01", "#include <stdio.h>\n\nint main(void) {\n    printf(\"你好，C\\n\");\n"
           "    printf(\"%s\\n\", 42);\n    printf(\"完成\\n\");\n    return 0;\n}\n",
     "整数用了 %s", "第一行|行输出|输出"),
    ("02", "#include <stdio.h>\n\nint main(void) {\n    int 年龄 = 18;\n"
           "    printf(\"%d\\n\", 年龄);\n    printf(\"7\\n\");\n    return 0;\n}\n",
     "学号写死成字符串", "输出|行|学号"),
    ("03", "#include <stdio.h>\n\nint main(void) {\n    int 甲 = 17, 乙 = 5;\n"
           "    printf(\"3\\n\");\n    printf(\"%d\\n\", 甲 % 乙);\n    return 0;\n}\n",
     "整除写死成 3", "变量"),
    ("04", "#include <stdio.h>\n\nint main(void) {\n    float 圆周率 = 3.14159;\n"
           "    printf(\"%.2f\\n\", 圆周率 * 2 * 2);\n    return 0;\n}\n",
     "用了 float 不是 double", "double"),
    ("05", "#include <stdio.h>\n\nint main(void) {\n    int 数;\n"
           "    scanf(\"%d\", 数);\n    printf(\"%d\\n\", 数 + 1);\n    return 0;\n}\n",
     "scanf 忘了 &", "&"),
    ("06", "int 判等级(int 分) {\n    if (分 > 90) return 3;\n"
           "    if (分 > 60) return 2;\n    return 1;\n}\n",
     "边界用了 > 不是 >=", "四个边界"),
    ("07", "int 求和到(int n) {\n    int 和;\n    for (int i = 1; i < n; i++)\n"
           "        和 += i;\n    return 和;\n}\n",
     "和没初始化为 0、循环用了 <", "四组"),
    ("08", "int 平方(int x) { return x + x; }\nint 较大(int a, int b) { return a; }\n",
     "平方写成 x+x、较大永远返回 a", "平方"),
    ("09", "int 求和(int 数们[], int 个数) {\n    int 和 = 0;\n"
           "    for (int i = 0; i <= 个数; i++)\n        和 += 数们[i];\n    return 和;\n}\n",
     "循环用了 <= 越界", "数组求和"),
    ("10", "int 长度(char 文字[]) {\n    return 0;\n}\n", "长度永远返回 0", "多组"),
    ("11", "void 加倍(int *p) {\n    p = p * 2;\n}\n", "改的是指针本身", "改到了"),
    ("12", "struct 学生 { int 学号; int 分数; }\n"
           "int 是优等生(struct 学生 s) { return 1; }\n",
     "结构体末尾漏分号；永远返回 1", "能编译|struct"),
    ("15", "void 全部加一(int *数们, int 个数) {\n    for (int i = 0; i < 个数; i++)\n"
           "        数们[i] = 数们[i];\n}\n", "什么都没做", "改到了"),
    ("16", "void 统计(int 数们[], int 个数, int *最大, int *最小) {\n"
           "    int 大 = 0, 小 = 0;\n    for (int i = 0; i < 个数; i++) {\n"
           "        if (数们[i] > 大) 大 = 数们[i];\n"
           "        if (数们[i] < 小) 小 = 数们[i];\n    }\n"
           "    *最大 = 大; *最小 = 小;\n}\n", "初值写 0（全负数会错）", "最大最小"),
    # ── 第 17~19 课（switch / while / 递归）──────────────────────────
    ("17", 'char* 季节(int 月) {\n'
           '    if (月 >= 3 && 月 <= 5) return "春";\n'
           '    if (月 >= 6 && 月 <= 8) return "夏";\n'
           '    if (月 >= 9 && 月 <= 11) return "秋";\n'
           '    return "冬";\n}\n',
     "第17课：用了 if 不是 switch", "switch"),
    ("18", 'int 数位和(int 数) {\n    int 和 = 0;\n'
           '    while (数 > 0) {\n        和 += 数 % 10;\n    }\n    return 和;\n}\n',
     "第18课：忘了 数 /= 10（死循环）", "while"),
    ("19", 'int 阶乘(int n) {\n    return n * 阶乘(n - 1);\n}\n'
           'int 斐波那契(int n) {\n    if (n <= 1) return n;\n'
           '    return 斐波那契(n - 1) + 斐波那契(n - 2);\n}\n',
     "第19课：阶乘没有基准情形（栈溢出）", "递归"),
]

shutil.rmtree(ANS, ignore_errors=True)
os.makedirs(ANS, exist_ok=True)

好 = 0
for 编, 代码, 说明, 期望 in 错例:
    subprocess.run(["dotnet", "run", "--project", "csharp/c-lessons", "--no-build",
                    "检查", 编], cwd=LC, env=env, capture_output=True, timeout=300)
    文件 = [f for f in os.listdir(ANS) if f.startswith(编)]
    if not 文件:
        print("  ! %s: 建不出答题文件" % 编)
        continue
    io.open(os.path.join(ANS, 文件[0]), "w", encoding="utf-8", newline="").write(代码)
    r = subprocess.run(["dotnet", "run", "--project", "csharp/c-lessons", "--no-build",
                        "检查", 编], cwd=LC, env=env, capture_output=True, timeout=600)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    全过 = "全部通过" in out
    # 抓到了：输出里出现"✗ 某条" 或 "× 某条"
    抓到 = bool(re.search(r"[✗×]\s*\S", out))
    对 = (not 全过) and 抓到
    好 += 1 if 对 else 0
    print("  %s %s  %s" % ("OK " if 对 else "XX ", 编, 说明))
    if 全过:
        print("       **错误答案被判成通过了** —— 这条检查是摆设")

print()
print("  == %d/%d 个错误答案被正确拒绝 ==" % (好, len(错例)))
shutil.rmtree(ANS, ignore_errors=True)
