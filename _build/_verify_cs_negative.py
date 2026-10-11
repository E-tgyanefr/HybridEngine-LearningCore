# 反面验证：喂**错误答案**，检查必须**报错**。
#
# ## 为什么必须做（否则检查可能是摆设）
#   "参考答案全过"只证明检查**不冤枉对的**。
#   如果检查永远返回"通过"，那它同时也是"参考答案全过" ——
#   但那对学员毫无价值。
#   ⇒ 必须证明它**会拒绝错的**。
#
#   作者的标准一直是"我这边能跑 ≠ 用户那边能用"，
#   这里是同一件事的另一面：**"答案能过" ≠ "检查有用"**。
import os
import re
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
ANS = os.path.join(ROOT, "cs-answers")
env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"

# 每项：(课号, 错误答案, 说明, 期望失败的检查名片段)
错例 = [
    ("01", 'static void 打招呼()\n{\n    Console.WriteLine("你好，C#");\n'
           '    int 甲 = 6;\n    int 乙 = 7;\n    Console.WriteLine(13);\n}\n',
     "把和写死成 13（没用变量相加）", "真的用了变量相加"),
    ("01", 'static void 打招呼()\n{\n    Console.Write("你好，C#");\n'
           '    int 甲 = 6;\n    int 乙 = 7;\n    Console.Write(甲 + 乙);\n}\n',
     "用了 Console.Write（不换行）", "用了 Console.WriteLine"),
    ("02", 'static int 算面积(int 半径)\n{\n    return 22 / 7 * 半径 * 半径;\n}\n',
     "整数除法丢了小数、返回类型写成 int", "返回类型"),
    ("03", 'static string 判等级(int 分)\n{\n    if (分 > 90) return "优秀";\n'
           '    if (分 > 60) return "及格";\n    return "不及格";\n}\n'
           'static string 星期几(int n) { return n switch { 1 => "周一", _ => "不是星期" }; }\n',
     "边界写成 > 而不是 >=（90 分被判成及格）", "判等级"),
    ("04", 'static int 求和(List<int> 数)\n{\n    int 和;\n    foreach (var x in 数) 和 += x;\n'
           '    return 和;\n}\n'
           'static List<int> 只要偶数(List<int> 数)\n{\n    var 出 = new List<int>();\n'
           '    foreach (var x in 数) if (x % 2 == 1) 出.Add(x);\n    return 出;\n}\n',
     "偶数判据写成 x % 2 == 1（挑出了奇数）", "只要偶数"),
    ("05", 'static int 平方(int x) { return x + x; }\n'
           'static void 加倍(ref int x) { x = x * 2; }\n'
           'static bool 试着转成整数(string s, out int 值) { return int.TryParse(s, out 值); }\n',
     "平方写成了 x + x（不是 x * x）", "平方"),
    ("06", 'static string 反转(string s) { return s; }\n'
           'static int 数元音(string s)\n{\n    int n = 0;\n'
           '    foreach (var c in s) if ("aeiou".IndexOf(c) >= 0) n++;\n    return n;\n}\n',
     "反转直接返回原串；数元音忘了 ToLower", "数元音"),
    ("07", 'class 计数器\n{\n    public int 值 { get; set; }\n    public 计数器(int 初值 = 0) { 值 = 初值; }\n'
           '    public void 加(int n) => 值 += n;\n    public void 重置() => 值 = 0;\n}\n'
           'class 银行账户\n{\n    public int 余额 { get; set; }\n    public 银行账户(int 初值 = 0) { 余额 = 初值; }\n'
           '    public void 存(int n) { 余额 += n; }\n'
           '    public bool 取(int n) { 余额 -= n; return true; }\n}\n',
     "取款不检查余额（先扣再判断）", "账户"),
    ("08", 'interface 图形 { double 面积(); string 名字(); }\n'
           'class 圆 : 图形 { public double 面积() => 3.0; public string 名字() => "圆"; }\n'
           'class 矩形 : 图形 { public double 面积() => 12.0; public string 名字() => "矩形"; }\n'
           'class 正方形 : 图形 { public double 面积() => 25.0; public string 名字() => "正方形"; }\n'
           'static string 报告(List<图形> 们) { return "x"; }\n',
     "正方形没有复用矩形（各写一遍）", "继承"),
]

shutil.rmtree(ANS, ignore_errors=True)
os.makedirs(ANS, exist_ok=True)

好 = 0
for 编, 代码, 说明, 期望抓住 in 错例:
    # 先让课程建出真实答题文件，拿它的路径
    subprocess.run(["dotnet", "run", "--project", "csharp/cs-lessons", "--no-build",
                    "检查", 编], cwd=ROOT, env=env, capture_output=True, timeout=300)
    # 找到该课的答案文件并写入错误答案
    文件 = [f for f in os.listdir(ANS) if f.startswith(编)]
    if not 文件:
        print("  ! %s: 建不出答题文件" % 编)
        continue
    with open(os.path.join(ANS, 文件[0]), "w", encoding="utf-8") as f:
        f.write(代码)

    r = subprocess.run(["dotnet", "run", "--project", "csharp/cs-lessons", "--no-build",
                        "检查", 编], cwd=ROOT, env=env, capture_output=True, timeout=600)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    全过 = "全部通过" in out
    # 找失败的检查名
    失败行 = [l.strip() for l in out.splitlines() if l.strip().startswith("✗")]
    抓到 = any(期望抓住 in l for l in 失败行)
    好 += 1 if (not 全过 and 抓到) else 0
    print("  %s %s  %s" % ("OK " if (not 全过 and 抓到) else "XX ", 编, 说明))
    if 全过:
        print("       **错误答案被判成通过了** —— 这检查是摆设")
    elif not 抓到:
        print("       报错了，但不是" + 期望抓住 + "这一项：")
        for l in 失败行[:4]:
            print("         " + l[:100])

print()
print("  == %d/%d 个错误答案被正确拒绝 ==" % (好, len(错例)))
shutil.rmtree(ANS, ignore_errors=True)
sys.exit(0 if 好 == len(错例) else 1)
