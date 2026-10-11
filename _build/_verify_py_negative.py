# 反面验证：喂**错误答案**，Python 课的检查必须报错。
#
# ## 为什么必须做
#   "参考答案全过"只证明检查**不冤枉对的**。
#   如果检查永远返回"通过"，那它同时也"参考答案全过" —— 但对学员毫无价值。
#   ⇒ 必须证明它**会拒绝错的**。
#
#   C# 那边这么做的时候抓到了 0 个问题（说明检查都好），
#   但它同时也证明了"检查不是摆设" —— 这个证明本身有价值。
import os
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = r"D:\Users\etgya\Desktop\Project\引擎\LearnCode"
S = os.path.join(LC, "lessons")
ANS = os.path.join(S, "answers")
env = dict(os.environ)
env["PYTHONIOENCODING"] = "utf-8"

# 每项：(课号, 错误答案, 说明, 期望失败的检查名片段)
错例 = [
    ("01", "我的名字 = 小明\n我的年龄 = 18\n",
     "名字忘了加引号", "我的名字"),
    ("01", "我的名字 = '小明'\n我的年龄 = '18'\n",
     "年龄写成了文字（带引号）", "我的年龄"),
    ("02", "print（'第 02 课开始'）\nprint('你好' + '世界')\n",
     "用了全角括号", "打印了第一行"),
    ("03", "苹果数 = 7\n梨数 = 2\n总个数 = 苹果数 + 梨数\n平均 = 总个数 // 2\n",
     "用了 // 把小数丢了", "平均"),
    ("04", '名字 = "小明"\n年龄 = 18\n\n\ndef 自我介绍():\n'
           '    return "我叫{名字}，今年{年龄}岁。"\n',
     "f-string 少了那个 f", "自我介绍"),
    ("04", '名字 = "小明"\n年龄 = 18\n\n\ndef 自我介绍():\n'
           '    return "我叫" + 名字 + "，今年" + 年龄 + "岁。"\n',
     "没用 f-string（而且数字不能拼）", "真的用了f字符串"),
    ("05", "成绩 = [90, 75, 60, 45, 88]\n第一个 = 成绩[0]\n第三个 = 成绩[3]\n"
           "最后一个 = 成绩[-1]\n",
     "第三个用了下标 3（应该是 2）", "第三个"),
    ("06", "购物 = ['苹果']\n购物.append('香蕉')\n购物.remove('苹果')\n"
           "弹出的 = 购物.pop()\n购物.pop(0)\n购物.remove('牛奶')\n",
     "insert 没用（顺序也不对）", "用了三个方法"),
    ("07", 'def 判等级(分数):\n    if 分数 > 90:\n        return "优秀"\n'
           '    elif 分数 > 60:\n        return "及格"\n    else:\n        return "不及格"\n',
     "边界用了 > 而不是 >=（90/60 会被判错）", "四个边界"),
    ("08", "数字 = [3, 1, 4, 1, 5]\n和 = sum(数字)\n个数 = len(数字)\n",
     "没用循环（直接 sum/len）", "用了for循环"),
    ("09", "def 打招呼():\n    print('你好')\n    print('我在学 Python')\n",
     "定义了但忘了调用", "两行都打印"),
    ("10", "def 平方(数):\n    print(数 * 数)\n\n\ndef 统计(数字们):\n"
           "    return len(数字们), sum(数字们)\n",
     "平方用了 print 而不是 return", "平方"),
    ("11", 'def 问候(名字, 称呼):\n    return f"你好，{名字}{称呼}！"\n\n\ndef 打折(价格, 折扣=0.9):\n'
           "    return 价格 * 折扣\n",
     "问候的称呼没有默认值", "问候"),
    ("12", "def 加全部(*数字):\n    return sum(数字)\n\n\ndef 最长(*文字们):\n"
           "    return max(文字们, key=len)\n",
     "最长在一个都不传时会崩", "最长"),
    ("13", "库存 = {'苹果': 3, '香蕉': 5, '橘子': 1}\n\n\ndef 查数量(库存表, 商品名):\n"
           "    return 库存表[商品名]\n\n\ndef 总件数(库存表):\n    return sum(库存表)\n",
     "查数量直接下标（KeyError）；总件数加起来的是键", "查数量"),
    ("14", "def 转成整数(文字):\n    return int(文字)\n\n\ndef 安全相除(a, b):\n"
           "    try:\n        return a / b\n    except:\n        return None\n",
     "转成整数没接住异常；安全相除用了空 except", "转成整数"),
    ("15", 'class 学生:\n    def _init_(self, 姓名):\n        self.姓名 = 姓名\n\n'
           "    def 问好(self):\n        return f'我是{self.姓名}'\n",
     "__init__ 写成了 _init_（少一个下划线）", "能造出对象"),
    ("16", "class 存钱罐:\n    def __init__(self, 初值=0):\n        self.余额 = 初值\n\n"
           "    def 存(self, n):\n        self.余额 += n\n\n"
           "    def 取(self, n):\n        self.余额 -= n\n        if self.余额 < 0:\n"
           "            return False\n        return True\n",
     "取款先扣再判断（失败时余额已被改坏）", "取钱不够"),
]

shutil.rmtree(ANS, ignore_errors=True)
os.makedirs(ANS, exist_ok=True)

好 = 0
for 编, 代码, 说明, 期望抓住 in 错例:
    # 先让课程建出答题文件（问它要真实文件名）
    subprocess.run([sys.executable, "-m", "hybridengine_light", "检查", 编],
                   cwd=LC, env=env, capture_output=True, timeout=300)
    文件 = [f for f in os.listdir(ANS) if f.startswith(编)]
    if not 文件:
        print("  ! %s: 建不出答题文件" % 编)
        continue
    with open(os.path.join(ANS, 文件[0]), "w", encoding="utf-8") as f:
        f.write(代码)

    r = subprocess.run([sys.executable, "-m", "hybridengine_light", "检查", 编],
                       cwd=LC, env=env, capture_output=True, timeout=300)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    全过 = "全部通过" in out
    # ⚠ 判据别只看"失败那一行"：报告里 `× 检查名` 和它下面的**说明**是两行，
    #   而我想验证的常常是**说明**里的话（比如「我的名字」）。
    #   我第一版只扫 `×` 那一行 ⇒ 4 个用例误判成"没抓到"，
    #   而它们其实都正确拒绝了（说明在下一行）。
    #   ⇒ 全输出里只要出现那句话，就算抓到了。
    失败行 = [l.strip() for l in out.splitlines()
              if l.strip().startswith(("✗", "×"))]
    抓到 = 期望抓住 in out
    对 = (not 全过) and 抓到
    好 += 1 if 对 else 0
    print("  %s %s  %s" % ("OK " if 对 else "XX ", 编, 说明))
    if 全过:
        print("       **错误答案被判成通过了** —— 这条检查是摆设")
    elif not 抓到:
        print("       报错了，但没抓到「%s」这一项，实际是：" % 期望抓住)
        for l in 失败行[:4]:
            print("         " + l[:96])

print()
print("  == %d/%d 个错误答案被正确拒绝 ==" % (好, len(错例)))
shutil.rmtree(ANS, ignore_errors=True)
sys.exit(0 if 好 == len(错例) else 1)
