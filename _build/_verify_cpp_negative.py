# -*- coding: utf-8 -*-
r"""反面验证：C++ 课程（01~40）—— 喂**错误答案**，检查必须报错。

════════════════════════════════════════════════════════════════
  为什么需要这一条（这是它存在的全部理由）
════════════════════════════════════════════════════════════════

「参考答案全过」只证明检查**不冤枉对的**。
如果某条检查永远返回通过，它同样能"参考答案全过" —— 但对学员毫无价值。
⇒ 必须证明它**会拒绝错的**。

`_verify_negative_all.py` 的覆盖账本里，C++ 和 Java 一直是 **0 课**
（61/219）。这条脚本补的是 C++ 那 41 课 —— 用的是和
`_verify_c_neg.py` / `_verify_cs_negative.py` **同一个形态**（这是有意的：
新脚本要是又发明一套判据，以后没人能一眼看懂它到底判了什么）。

════════════════════════════════════════════════════════════════
  判据（三条同时成立才算「被拒」）
════════════════════════════════════════════════════════════════

  ① 输出里**没有** `全部通过` —— 有就说明错误答案被判成了通过，
     这一条检查是摆设（脚本会专门打一行 `**错误答案被判成通过了**`）。
  ② 输出里出现 `✗ <期望的检查名>` —— **必须带检查名**，
     光在整段输出里搜关键字会把"别的检查的提示里提到了这个词"也算成抓到
     （GESP 那个脚本为这件事写过教训）。
  ③ 输出里**至少有一个 ✓ 或 ✗** —— 一个都没有意味着检查**根本没跑**
     （答题文件被别人清掉、课程程序重建了模板就返回）。
     ⚠ 「没跑到」和「没过」**必须分开**：前者算失败，不是通过。

════════════════════════════════════════════════════════════════
  ⚠ 两条**已确认的检查缺口**（不是本脚本的假绿，也不许被静默吞掉）
════════════════════════════════════════════════════════════════

  第 09 / 11 / 24 课各有一条检查**是空转的**：错答案照样全过。
  它们的错例表项多带一个 `"检查空转"` 标记，脚本会：

    · 单独列出来（`⚠ **这一课的检查是空转的**`）
    · 结尾再打一段缺口清单
    · **但退出码仍是 0** —— 理由和 `_verify_negative_all.py` 的覆盖缺口一样：
      否则这条命令永远红，就没人看了。**缺口要的是"被看见"，不是"把闸门焊死"。**
    · 反过来：**标记为缺口、结果却发现它开始拒绝错答案了** ⇒ 判**失败** ——
      说明课程被改过，这个标记该撤了，不能留着糊过去。

════════════════════════════════════════════════════════════════
  ⚠ 编译型这条线特有的两个坑（都实测踩过，这里一次性绕开）
════════════════════════════════════════════════════════════════

  ① **MinGW 的 ld.exe 写不进含中文的输出路径**，而本仓库根目录里就有「引擎」
     两个字 ⇒ 编译产物必须落在**全 ASCII 的 TEMP** 里。CppRunner 自己会退到
     `C:\hybridengine_light_tmp`，但显式给一个私有 TEMP 更稳（已验证）。
  ② `CppRunner.清理旧目录` 曾经删掉**别的进程正在用**的编译目录 ⇒ 症状是
     「编译没能通过」而学员代码一个字都没错。它现在有"10 分钟内不删"的规则，
     这里再串行跑（不并发），所以不会自踩。

════════════════════════════════════════════════════════════════
  用法
════════════════════════════════════════════════════════════════

    python _build/_verify_cpp_negative.py            # 全跑（约 4~6 分钟）
    python _build/_verify_cpp_negative.py 21 25      # 只跑这两课
    python _build/_verify_cpp_negative.py --列       # 只列错例表，不跑

⚠ 路径**从 `__file__` 推**，不写死 —— 仓库挪过一次，写死路径的脚本全废。
"""
import io
import os
import shutil
import subprocess
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ANS = os.path.join(ROOT, "cpp-answers")

env = dict(os.environ)
env["DOTNET_CLI_UI_LANGUAGE"] = "en"
env["PYTHONIOENCODING"] = "utf-8"
env["PYTHONUNBUFFERED"] = "1"
# 私有 TEMP：必须是全 ASCII 的路径（见文件头）
私有TEMP = r"C:\lc_tmpx"
try:
    os.makedirs(私有TEMP, exist_ok=True)
    env["TEMP"] = 私有TEMP
    env["TMP"] = 私有TEMP
except OSError:
    pass

运行 = ["dotnet", "run", "--project", "csharp/cpp-lessons", "检查"]


def 跑检查(编号, 超时=600):
    r = subprocess.run([*运行, 编号], cwd=ROOT, env=env,
                       capture_output=True, timeout=超时)
    return (r.stdout + r.stderr).decode("utf-8", "replace")


# ════════════════════════════════════════════════════════════════
#  错例表
#
#  每项：(课号, 错答案源码, 错在哪（给人看的一句话）, 期望被拒的检查名（要能对上 ✗ 那一行）)
#
#  ⚠ 课号一律显式写 `("01", …)` 这个形状 ——
#    `_verify_negative_all.py` 的覆盖账本就是用
#    `^\s*\(\s*"(\d\d)"` 从这个文件里数课号的。写成别的形状，
#    账本会显示"没覆盖"，而实际上跑了 —— 那是**假缺口**。
# ════════════════════════════════════════════════════════════════

错例 = [
    # ══════════════════════════════════════════════════════════════
    #  00~02：完整程序（学生自带 main）
    # ══════════════════════════════════════════════════════════════

    # ── 00 跑通第一个程序 ───────────────────────────────────────────
    ("00", '''\
#include <iostream>

int main() {
    std::cout << "工具链就绪" << std::endl
    std::cout << "我准备好学 C++ 了" << std::endl;
    return 0;
}
''',
     "第 00 课的错答案就是**编译不过**：第一行末尾漏了分号。"
     "编译器会说 expected ';' before 'std' —— 这一课练的就是读这句话，"
     "所以「编译失败」必须被判成不过", "程序能编译、能跑起来"),

    # ── 01 输出 ─────────────────────────────────────────────────────
    ("01", '''\
#include <iostream>

int main() {
    std::cout << "你好，C++" << std::endl;
    std::cout << 13 << std::endl;
    return 0;
}
''',
     "把 6+7 的答案直接写死成 13，一个变量都没定义", "有没有按题目要求定义变量"),

    # ── 02 输入与计算 ───────────────────────────────────────────────
    ("02", '''\
#include <iostream>

int main() {
    int 甲 = 0;
    int 乙 = 0;
    std::cin >> 甲 >> 乙;

    std::cout << "商=" << 甲 / 乙 << std::endl;

    double 平均数 = (甲 + 乙) / 2;
    std::cout << "平均数=" << 平均数 << std::endl;

    return 0;
}
''',
     "平均数写成 (甲+乙)/2 —— 两个 int 相除，小数被丢掉（3 和 4 得 3 而不是 3.5）；"
     "而且读输入后不判是否成功，非数字输入会被静默当成上一轮的值",
     "平均数是不是真的带小数"),

    # ══════════════════════════════════════════════════════════════
    #  03~11：只写函数（课程骨架提供 main）
    # ══════════════════════════════════════════════════════════════

    # ── 03 条件判断 ─────────────────────────────────────────────────
    ("03", '''\
#include <string>

std::string 猜数字(int 猜) {
    if (猜 == 42) {
        return "对了";
    } else if (猜 < 42) {
        return "小了";
    } else {
        return "大了";
    }
}

std::string 成绩等级(int 分数) {
    if (分数 < 0 || 分数 > 100) {
        return "分数不合法";
    } else if (分数 > 90) {
        return "优秀";
    } else if (分数 > 60) {
        return "及格";
    } else {
        return "不及格";
    }
}

std::string 闰年(int 年) {
    if ((年 % 4 == 0 && 年 % 100 != 0) || 年 % 400 == 0) {
        return "闰年";
    } else {
        return "平年";
    }
}
''',
     "成绩等级的两个边界写成 > 而不是 >= —— 90 分被判成「及格」、60 分被判成「不及格」",
     "成绩等级：边界分数（90 / 60）判对了没有"),

    # ── 04 循环 ─────────────────────────────────────────────────────
    ("04", '''\
int 求和(int n) {
    int 和 = 0;
    for (int i = 1; i < n; i++) {
        和 += i;
    }
    return 和;
}

int 数偶数(int n) {
    int 个数 = 0;
    int i = 0;
    while (i < n) {
        i++;
        if (i % 2 != 0) {
            continue;
        }
        个数++;
    }
    return 个数;
}

int 第一个因子(int n) {
    if (n < 2) {
        return -1;
    }
    int 结果 = n;
    for (int i = 2; i < n; i++) {
        if (n % i == 0) {
            结果 = i;
            break;
        }
    }
    return 结果;
}
''',
     "求和的循环写成 i < n —— 差一错误，求和(3) 得 3 而不是 6", "求和：1 到 n 的和对不对"),

    # ── 05 函数 ─────────────────────────────────────────────────────
    ("05", '''\
int 双倍(int n) {
    return 7;
}

int 最大值(int a, int b) {
    if (a > b) {
        return a;
    }
    return b;
}

void 打印星星(int n) {
    for (int i = 0; i < n; i++) {
        std::cout << "*";
    }
}
''',
     "双倍不管传什么都返回 7 —— 函数调用了、也 return 了，但对不同输入给同一个答案",
     "同一个函数对不同输入要给出不同结果"),

    # ── 06 数组与向量 ───────────────────────────────────────────────
    ("06", '''\
int 求和(int 数[], int 长度) {
    int 和 = 数[0];                    /* ⚠ 长度是 0 时，这一步已经越界了 */
    for (int i = 1; i < 长度; i++) {
        和 += 数[i];
    }
    return 和;
}

int 求和向量(std::vector<int> 数) {
    int 和 = 0;
    for (int i = 0; i < (int)数.size(); i++) {
        和 += 数[i];
    }
    return 和;
}

int 安全取(std::vector<int> 数, int 下标) {
    if (下标 < 0 || 下标 >= (int)数.size()) {
        return -1;
    }
    return 数[下标];
}
''',
     "数组版求和先把第一格塞进结果（int 和 = 数[0]）—— 长度 1/2/4 全对，"
     "**长度 0 时越界读了 数[0]**。C++ 越界不一定会崩，于是它安静地读到一个数，"
     "答案不是 0。这一课的核心检查点就是「长度 0 必须交 0」",
     "★ 求和：长度是 0 时必须返回 0"),

    # ── 07 字符串 ───────────────────────────────────────────────────
    ("07", '''\
int 字节数(std::string s) {
    return (int)s.size();
}

char 首字符(std::string s) {
    if (s.empty()) {
        return '\\0';
    }
    return s[0];
}

int 单引号大小() {
    return (int)sizeof('A');
}

int 双引号大小() {
    return (int)sizeof("A");
}

std::string 拼接(std::string a, std::string b) {
    return "拼好了";
}

std::string 前三个(std::string s) {
    return s.substr(0, 3);
}

int 找位置(std::string s, std::string 子) {
    std::size_t 位置 = s.find(子);
    if (位置 == std::string::npos) {
        return -1;
    }
    return (int)位置;
}
''',
     "拼接直接返回常量「拼好了」—— 能编译、能返回 std::string，但没用上 a 和 b",
     "拼接：+ 连起来就行（空串也要对）"),

    # ── 08 指针与引用 ───────────────────────────────────────────────
    ("08", '''\
void 翻倍_值(int n) {
    n = n * 2;
}

void 翻倍_引用(int n) {
    n = n * 2;
}

void 翻倍_指针(int* p) {
    if (p != nullptr) {
        *p = *p * 2;
    }
}

int 安全取值(int* p, int 备用值) {
    if (p == nullptr) {
        return 备用值;
    }
    return *p;
}
''',
     "翻倍_引用 的参数漏了 & —— 退化成值传递，函数里改了外面不变（这正是本课要区分的那件事）",
     "★ 引用传递：函数里改了 n，外面必须**跟着变**"),

    # ── 09 const 与 constexpr ───────────────────────────────────────
    ("09", '''\
constexpr int 格子数 = 12;

int 圆周长的百倍(int 半径) {
    int 圆周率的百倍 = 314;
    return 2 * 圆周率的百倍 * 半径 / 100;
}

int 格子数是多少() {
    return 格子数;
}
''',
     "圆周率的百倍 只是普通 int，没写 const —— 手滑改成 315 也照样编译通过，"
     "这一课要学的那道保险没上",
     "用了 const（这一课练它）",
     "检查空转"),

    # ── 10 作用域与生命周期 ─────────────────────────────────────────
    ("10", '''\
int 数 = 100;

int 影子() {
    int 数 = 5;
    return 100;
}

int 计数器() {
    static int 次数 = 0;
    次数++;
    return 次数;
}
''',
     "影子 声明了局部变量 5 却 return 全局那个 100 —— 局部声明成了摆设，遮蔽没发生",
     "影子 返回局部那个值（5），而不是全局的 100"),

    # ── 11 递归 ─────────────────────────────────────────────────────
    ("11", '''\
int 阶乘(int n) {
    int 果 = 1;
    for (int i = 2; i <= n; i++) {
        果 *= i;
    }
    return 果;
}

int 斐波那契(int n) {
    if (n <= 0) {
        return 0;
    }
    if (n == 1) {
        return 1;
    }
    return 斐波那契(n - 1) + 斐波那契(n - 2);
}
''',
     "阶乘改用循环算 —— 结果全对，但这一课练的是「函数调用自己」",
     "真的用了递归（函数里调用了自己）",
     "检查空转"),

    # ══════════════════════════════════════════════════════════════
    #  12~15、21：完整程序
    # ══════════════════════════════════════════════════════════════

    # ── 12 结构体与类 ───────────────────────────────────────────────
    ("12", '''\
#include <stdio.h>

struct 点 {
    int x;
    int y;
};

int main(void) {
    点 a = {3, 4};                     /* ⚠ 没有 造点 这个函数，直接聚合初始化 */
    点 b = {0, 0};

    printf("%d\\n", 距离平方(a, b));
    printf("%d\\n", a.x);
    printf("%d\\n", b.x);
    return 0;
}
''',
     "干脆不写 造点 这个函数，main 里用聚合初始化 {3, 4} 直接把点造出来 —— "
     "三行输出**完全正确**、struct 和两个字段也都在，"
     "唯独「用函数造一个点」这件事没做（那正是这一课要教的：struct 能当返回值）",
     "main 里用 造点 造了两个点"),

    # ── 13 vector 与常用算法 ────────────────────────────────────────
    ("13", '''\
#include <stdio.h>
#include <vector>
#include <algorithm>
#include <numeric>

int main(void) {
    std::vector<int> 数们 = {5, 2, 9, 1};
    printf("%d\\n", (int)数们.size());

    for (int i = 0; i < (int)数们.size() - 1; i++) {
        for (int j = 0; j < (int)数们.size() - 1 - i; j++) {
            if (数们[j] > 数们[j + 1]) {
                int 临时 = 数们[j];
                数们[j] = 数们[j + 1];
                数们[j + 1] = 临时;
            }
        }
    }
    for (int 数 : 数们) {
        printf("%d\\n", 数);
    }

    printf("%d\\n", std::accumulate(数们.begin(), 数们.end(), 0));

    std::reverse(数们.begin(), 数们.end());
    printf("%d\\n", 数们[0]);
    return 0;
}
''',
     "排序用手写冒泡代替 std::sort —— 输出**完全正确**，但这一课要学的现成算法没用到",
     "用了 sort"),

    # ── 14 动态内存 ─────────────────────────────────────────────────
    ("14", '''\
#include <stdio.h>
#include <stdlib.h>

int* 造数组(int n) {
    int* p = (int*)malloc(sizeof(int) * n);
    for (int i = 0; i < n; i++) {
        p[i] = i * i;
    }
    return p;
}

int 求和(int* 数们, int n) {
    int 和 = 0;
    for (int i = 0; i < n; i++) {
        和 += 数们[i];
    }
    return 和;
}

int main(void) {
    int* a = 造数组(4);
    for (int i = 0; i < 4; i++) {
        printf("%d\\n", a[i]);
    }
    free(a);
    a = NULL;

    int b[4] = {1, 2, 3, 4};
    printf("%d\\n", 求和(b, 4));
    return 0;
}
''',
     "用 malloc/free 代替 new/delete[] —— 结果一模一样，但这一课要学的正是 new 和 delete[]",
     "用 new 造了数组（不是栈上的）"),

    # ── 15 综合练习 ─────────────────────────────────────────────────
    ("15", '''\
#include <stdio.h>
#include <string>
#include <vector>

struct 成绩 {
    std::string 名字;
    int 分数;
};

double 平均分(const std::vector<成绩>& 们) {
    if (们.empty()) return 0.0;
    int 总和 = 0;
    for (const 成绩& 一 : 们) {
        总和 += 一.分数;
    }
    return 总和 / (int)们.size();
}

std::string 最高分名字(const std::vector<成绩>& 们) {
    if (们.empty()) return "";
    int 最好 = 0;
    for (int i = 1; i < (int)们.size(); i++) {
        if (们[i].分数 > 们[最好].分数) 最好 = i;
    }
    return 们[最好].名字;
}

int main(void) {
    std::vector<成绩> 表 = { {"甲", 80}, {"乙", 90}, {"丙", 85} };

    printf("%d\\n", (int)表.size());
    printf("%.2f\\n", 平均分(表));
    printf("%s\\n", 最高分名字(表).c_str());
    return 0;
}
''',
     "平均分写成 总和/(人数) —— 两个 int 相除，没写 (double) 转换。"
     "⚠ 这份错答案最值得看的地方：它对 {80,90,85} 的输出**是对的**"
     "（255/3 整数除法恰好也是 85），所以「输出对不对」那一条抓不住它 ——"
     "抓住它的是「平均分里做了 double 转换」那一条源码检查",
     "平均分里做了 double 转换（避开整数除法）"),

    # ══════════════════════════════════════════════════════════════
    #  16~20
    # ══════════════════════════════════════════════════════════════

    # ── 16 二维数组 ─────────────────────────────────────────────────
    ("16", '''\
void 打印每行(int 格[][4], int 行数) {
    for (int i = 0; i < 行数; i++) {
        for (int j = 0; j < 4; j++) {
            std::cout << 格[i][j] << ' ';
        }
        std::cout << '\\n';
    }
}

void 打印每列(int 格[][4], int 行数) {
    for (int j = 0; j < 4; j++) {
        for (int i = 0; i < 行数; i++) {
            std::cout << 格[j][i] << ' ';
        }
        std::cout << '\\n';
    }
}

int 主对角线和(int 格[][4], int 行数, int 列数) {
    int 短边 = 行数 < 列数 ? 行数 : 列数;
    int 和 = 0;
    for (int i = 0; i < 短边; i++) {
        和 += 格[i][i];
    }
    return 和;
}

int 矩阵最大(int 格[][4], int 行数, int 列数) {
    int 最大 = 格[0][0];
    for (int i = 0; i < 行数; i++) {
        for (int j = 0; j < 列数; j++) {
            if (格[i][j] > 最大) {
                最大 = 格[i][j];
            }
        }
    }
    return 最大;
}
''',
     "按列打印写成 格[j][i] —— 行列下标对调了，方括号里必须是 格[i][j]（变的只是循环次序）",
     "★ 按列打印"),

    # ── 17 排序入门 ─────────────────────────────────────────────────
    ("17", '''\
#include <algorithm>

void 冒泡排序(int 数[], int 长度) {
    std::sort(数, 数 + 长度);
}

void 选择排序(int 数[], int 长度) {
    std::sort(数, 数 + 长度);
}

void 插入排序(int 数[], int 长度) {
    std::sort(数, 数 + 长度);
}
''',
     "三个排序全部调 std::sort —— 四组数据（含重复元素、长度 0/1）全都能过，"
     "这一课要练的手写排序一点没练", "★ 不许拿 std::sort 之类的现成排序交差"),

    # ── 18 排序稳定性 ───────────────────────────────────────────────
    ("18", '''\
int 总分(std::vector<成绩> 表) {
    int 和 = 0;
    for (const 成绩& 人 : 表) {
        和 += 人.分数;
    }
    return 和;
}

void 按分排序(std::vector<成绩>& 表) {
    std::stable_sort(表.begin(), 表.end(),
        [](const 成绩& 甲, const 成绩& 乙) {
            return 甲.分数 >= 乙.分数;      /* ⚠ 写成了 >= */
        });
}

void 按分排序_不稳定(std::vector<成绩>& 表) {
    std::sort(表.begin(), 表.end(),
        [](const 成绩& 甲, const 成绩& 乙) {
            return 甲.分数 >= 乙.分数;      /* ⚠ 同样 >= */
        });
}
''',
     "比较函数写成 >= —— 这是排序比较器里最经典的一个坑（相等的元素被当成"
     "「甲该排在乙前面」，比较器不再是严格弱序）。用例里分数都不重复，"
     "所以排序结果**看起来是对的**，抓它的是那一课专门留的比较函数检查",
     "★ 比较函数用 > 或 <，不能写 >= 或 <="),

    # ── 19 算法复杂度 ───────────────────────────────────────────────
    ("19", '''\
long long 一重和(long long n) {
    long long 累加 = 0;
    for (long long i = 1; i <= n; i++) {
        累加 += i;
    }
    return 累加;
}

long long 公式(long long n) {
    return n * (n + 1) / 2;
}

long long 平方和(long long n) {
    return 公式(n);
}
''',
     "平方和 直接 return 公式(n) —— 答案全对，但把「O(n²) 到底慢在哪」这件事整个绕过去了",
     "★ 平方和里要真的有两层循环，而且不许去调用 公式/一重和"),

    # ── 20 递推 ─────────────────────────────────────────────────────
    ("20", '''\
long long 爬楼梯(int n) {
    if (n <= 0) return 1;
    if (n == 1) return 1;
    return 爬楼梯(n - 1) + 爬楼梯(n - 2);
}

long long 斐波那契(int n) {
    if (n <= 0) return 0;
    if (n == 1) return 1;
    return 斐波那契(n - 1) + 斐波那契(n - 2);
}

long long 斐波那契取模(int n) {
    if (n <= 0) return 0;
    if (n == 1) return 1;
    return (斐波那契取模(n - 1) + 斐波那契取模(n - 2)) % 1000000007;
}

long long 铺地砖(int n) {
    if (n <= 0) return 1;
    if (n == 1) return 1;
    return 铺地砖(n - 1) + 铺地砖(n - 2);
}
''',
     "四个函数都用第 11 课那种朴素递归 —— 小 n 全对，但这一课要的是「从前往后填表」，"
     "而且爬楼梯(40) 这种规模递归根本跑不完",
     "★ 四个都必须是从前往后填表（数组 + 循环），不许用递归"),

    # ── 21 文件读写（完整程序）───────────────────────────────────────
    ("21", '''\
#include <iostream>

int main() {
    std::cout << "读完了" << std::endl;
    return 0;
}
''',
     "根本不读 data.in，也不写 data.out —— 检查会换三份不同的输入各跑一遍，"
     "任何写死/不读文件的写法都过不了", "★ 真的从 data.in 里读：换三份不同的输入，答案要跟着变"),

    # ══════════════════════════════════════════════════════════════
    #  22~40
    # ══════════════════════════════════════════════════════════════

    # ── 22 初等数论 ─────────────────────────────────────────────────
    ("22", '''\
int 因数个数(int n) {
    int 个数 = 0;
    for (int i = 1; (long long)i * i <= n; i++) {
        if (n % i == 0) {
            个数 += (i * i == n) ? 1 : 2;
        }
    }
    return 个数;
}

bool 是质数(int n) {
    if (n < 2) {
        return false;
    }
    for (int i = 2; i < n; i++) {
        if (n % i == 0) {
            return false;
        }
    }
    return true;
}

int 质因数个数(int n) {
    int 个数 = 0;
    int 剩 = n;
    for (int i = 2; (long long)i * i <= 剩; i++) {
        while (剩 % i == 0) {
            个数++;
            剩 /= i;
        }
    }
    if (剩 > 1) {
        个数++;
    }
    return 个数;
}

int 最大质因数(int n) {
    if (n < 2) {
        return 1;
    }
    int 最大 = 1;
    int 剩 = n;
    for (int i = 2; (long long)i * i <= 剩; i++) {
        while (剩 % i == 0) {
            最大 = i;
            剩 /= i;
        }
    }
    if (剩 > 1) {
        最大 = 剩;
    }
    return 最大;
}
''',
     "是质数 一个个试到 n —— **答案全对**，判两个大质数实测 6 秒多（参考实现 0 毫秒），"
     "检查自己用 chrono 计时，上限 2000 毫秒",
     "★ 是质数：小数字、大质数都要对，而且大质数不能慢"),

    # ── 23 辗转相除 ─────────────────────────────────────────────────
    ("23", '''\
long long 最大公约数(long long a, long long b) {
    if (a < 0) { a = -a; }
    if (b < 0) { b = -b; }
    while (a != b) {                  /* ⚠ 用减法代替取余 */
        if (a > b) {
            a = a - b;
        } else {
            b = b - a;
        }
    }
    return a;
}

long long 最小公倍数(long long a, long long b) {
    if (a == 0 || b == 0) {
        return 0;
    }
    long long 约 = 最大公约数(a, b);
    return a / 约 * b;                /* 先除后乘，这一步是对的 */
}

long long 三个数的最大公约数(long long a, long long b, long long c) {
    return 最大公约数(最大公约数(a, b), c);
}
''',
     "最大公约数改用**更相减损**（一次次相减）—— 四个用例的答案全对，"
     "但这一课要练的是「辗转相除」，源码里一个 % 都没有"
     "（那条检查的判据就是「函数体里要用到 %」）",
     "★ 最大公约数里要用到 %（辗转相除的本质是取余）"),

    # ── 24 素数筛 ───────────────────────────────────────────────────
    ("24", '''\
bool 能整除(int n, int p) {
    return n - n / p * p == 0;        /* ⚠ 用减法代替 %，好绕开「试除法」的字样 */
}

int 素数个数(int n) {
    if (n < 2) {
        return 0;
    }
    int cnt = 0;
    for (int x = 2; x <= n; x++) {
        bool ok = true;
        for (int p = 2; p * p <= x; p++) {
            if (能整除(x, p)) {
                ok = false;
                break;
            }
        }
        if (ok) {
            cnt = cnt + 1;
        }
    }
    return cnt;
}

bool 是质数(int n) {
    if (n < 2) {
        return false;
    }
    for (int i = 2; (long long)i * i <= n; i++) {
        if (n % i == 0) {
            return false;
        }
    }
    return true;
}
''',
     "素数个数其实是**每个数单独试除**（O(n√n)），但特意绕开了那条文本兜底检查"
     "能认的所有形状：变量名用 cnt/x/p、自增写 cnt = cnt + 1、判整除用 "
     "`n - n/p*p == 0` 而不用 %、也没有 `i * i`、没有 `= true`/`= 1`。"
     "⚠ 实测结果：它 **5/5 全过**，连「跑 100 万」那条也过了（本机 12 秒内跑完）—— "
     "这一课的两道防线都被绕开了，是一处**已确认的检查缺口**",
     "★ 素数个数(1000000) 必须在时限内算完",
     "检查空转"),

    # ── 25 二分查找 ─────────────────────────────────────────────────
    ("25", '''\
int 二分查找(const int 数[], int 长度, int 目标) {
    for (int i = 0; i < 长度; i++) {
        if (数[i] == 目标) {
            return i;
        }
    }
    return -1;
}

int 下界(const int 数[], int 长度, int 目标) {
    for (int i = 0; i < 长度; i++) {
        if (数[i] >= 目标) {
            return i;
        }
    }
    return 长度;
}
''',
     "从头到尾线性扫 —— **19 组用例答案全对**（包括 10 万元素那组，线性扫也就几百微秒），"
     "计时分不出来，只能靠源码形状（函数体里有没有「折半」）判",
     "★ 必须是真二分：函数体里要有「折半」的写法"),

    # ── 26 栈与队列 ─────────────────────────────────────────────────
    ("26", '''\
std::vector<int> 处理括号(std::string s) {
    int 深度 = 0;
    int 最大深度 = 0;
    int 配不上 = 0;

    for (char c : s) {
        if (c == '(') {
            深度++;
            if (深度 > 最大深度) {
                最大深度 = 深度;
            }
        } else if (c == ')') {
            if (深度 == 0) {
                配不上 = 1;
            } else {
                深度--;
            }
        }
    }
    if (深度 != 0) {
        配不上 = 1;
    }
    return {配不上, 最大深度};
}
''',
     "用「当前深度」这个计数器代替栈 —— 配对和最大深度其实都对，"
     "但这一课要练的是 std::stack 的 push/pop，计数器把它整个绕过去了",
     "真的用了栈（std::stack，或自己写的 push/pop）"),

    # ── 27 链表 ─────────────────────────────────────────────────────
    ("27", '''\
void 头插(结点*& 头, int 值) {
    结点* 新结点 = new 结点(值);
    新结点->下一个 = 头;
    头 = 新结点;
}

bool 删值(结点*& 头, int 值) {
    for (结点* p = 头; p != nullptr; p = p->下一个) {
        if (p->值 == 值) {
            return true;
        }
    }
    return false;
}

bool 查找(结点* 头, int 值) {
    for (结点* p = 头; p != nullptr; p = p->下一个) {
        if (p->值 == 值) return true;
    }
    return false;
}

std::vector<int> 转数组(结点* 头) {
    std::vector<int> 出;
    for (结点* p = 头; p != nullptr; p = p->下一个) {
        出.push_back(p->值);
    }
    return 出;
}
''',
     "删值「找到了就 return true」却一条链子都没改 —— 返回值看着对，链表其实原封不动",
     "★ 核心：删值——删中间、删头、删不存在的、删空链"),

    # ── 28 树与遍历 ─────────────────────────────────────────────────
    ("28", '''\
void 收集(树结点* 根, std::vector<int>& 出);

void 先序(树结点* 根, std::vector<int>& 出) {
    if (根 == nullptr) return;
    出.push_back(根->值);
    先序(根->左, 出);
    先序(根->右, 出);
}

void 中序(树结点* 根, std::vector<int>& 出) {
    if (根 == nullptr) return;
    std::vector<int> 临时;
    收集(根, 临时);
    for (size_t i = 0; i + 1 < 临时.size(); i++) {
        for (size_t j = 0; j + 1 < 临时.size() - i; j++) {
            if (临时[j] > 临时[j + 1]) {
                int 交换 = 临时[j];
                临时[j] = 临时[j + 1];
                临时[j + 1] = 交换;
            }
        }
    }
    for (size_t i = 0; i < 临时.size(); i++) {
        出.push_back(临时[i]);
    }
}

void 收集(树结点* 根, std::vector<int>& 出) {
    if (根 == nullptr) return;
    出.push_back(根->值);
    收集(根->左, 出);
    收集(根->右, 出);
}

void 后序(树结点* 根, std::vector<int>& 出) {
    if (根 == nullptr) return;
    后序(根->左, 出);
    后序(根->右, 出);
    出.push_back(根->值);
}

int 结点数(树结点* 根) {
    if (根 == nullptr) return 0;
    return 1 + 结点数(根->左) + 结点数(根->右);
}
''',
     "中序没按「左-自己-右」走，而是把整棵树的值**从小到大排了一遍** —— "
     "在一棵二叉排序树上看起来完全正确，一般树上就露馅了（这正是那条检查要防的）",
     "★ 核心：中序不是「从小到大排序」——顺序真的按树走"),

    # ── 29 二叉排序树 ───────────────────────────────────────────────
    ("29", '''\
void 插入(树结点*& 根, int 值) {
    if (根 == nullptr) {
        根 = new 树结点(值);
        return;
    }
    树结点* 当 = 根;
    while (true) {
        if (当->右 == nullptr) {
            当->右 = new 树结点(值);
            return;
        }
        当 = 当->右;
    }
}

bool 存在(树结点* 根, int 值) {
    while (根 != nullptr) {
        if (值 == 根->值) return true;
        根 = (值 < 根->值) ? 根->左 : 根->右;
    }
    return false;
}

bool 删除(树结点*& 根, int 值) {
    if (根 == nullptr) return false;
    if (值 < 根->值) return 删除(根->左, 值);
    if (值 > 根->值) return 删除(根->右, 值);

    树结点* 被删 = 根;
    if (根->左 == nullptr) {
        根 = 根->右;
    } else if (根->右 == nullptr) {
        根 = 根->左;
    } else {
        树结点* 后继 = 摘最小(根->右);
        后继->左 = 根->左;
        后继->右 = 根->右;
        根 = 后继;
    }
    delete 被删;
    return true;
}

void 中序(树结点* 根, std::vector<int>& 出) {
    if (根 == nullptr) return;
    中序(根->左, 出);
    出.push_back(根->值);
    中序(根->右, 出);
}
''',
     "插入不看大小，一律往右挂 —— 树形状完全错了，中序不再是升序（查找也会跟着错）",
     "★ 核心：四组插入序列之后，中序都必须是升序"),

    # ── 30 DFS 与 BFS ───────────────────────────────────────────────
    ("30", '''\
int 迷宫最少步数(std::vector<std::vector<int>> 图, int 起x, int 起y, int 终x, int 终y) {
    int 行 = (int)图.size();
    if (行 == 0 || 图[0].empty()) return -1;
    int 列 = (int)图[0].size();
    if (图[起x][起y] != 0 || 图[终x][终y] != 0) return -1;

    std::vector<std::vector<int>> 步(行, std::vector<int>(列, -1));
    步[起x][起y] = 0;

    std::vector<std::pair<int, int>> 栈;
    栈.push_back({起x, 起y});

    int 方向[4][2] = {{1, 0}, {-1, 0}, {0, 1}, {0, -1}};

    while (!栈.empty()) {
        auto [x, y] = 栈.back();
        栈.pop_back();

        if (x == 终x && y == 终y) return 步[x][y];

        for (int k = 0; k < 4; k++) {
            int nx = x + 方向[k][0];
            int ny = y + 方向[k][1];
            if (nx < 0 || nx >= 行 || ny < 0 || ny >= 列) continue;
            if (图[nx][ny] != 0) continue;
            if (步[nx][ny] != -1) continue;
            步[nx][ny] = 步[x][y] + 1;
            栈.push_back({nx, ny});
        }
    }
    return -1;
}
''',
     "把队列换成栈（BFS 变成 DFS）+ 忘了提前收工 —— 这是学生最容易写出来的"
     "「用 DFS 求最短路」。⚠ 实测结果值得记：它在那一课的六张图上**步数表碰巧"
     "全对**（标记放在入栈时，第一次写下的恰好就是最短的），"
     "所以真正抓住它的是「用的是队列」那一条，不是步数表那条",
     "★ 用的是队列（BFS），不是栈或递归"),

    # ── 31 一维 DP ──────────────────────────────────────────────────
    ("31", '''\
int 爬楼梯(int n) {
    if (n <= 0) return 0;
    if (n == 1) return 1;
    return 爬楼梯(n - 1) + 爬楼梯(n - 2);
}

int 打家劫舍(std::vector<int> 钱) {
    int 隔一家 = 0;
    int 上一家 = 0;
    for (int x : 钱) {
        int 当 = std::max(上一家, 隔一家 + x);
        隔一家 = 上一家;
        上一家 = 当;
    }
    return 上一家;
}

int 简单背包(std::vector<int> 重, std::vector<int> 价, int 容量) {
    std::vector<int> dp(容量 + 1, 0);
    for (int i = 0; i < (int)重.size(); i++) {
        for (int c = 重[i]; c <= 容量; c++) {
            dp[c] = std::max(dp[c], dp[c - 重[i]] + 价[i]);
        }
    }
    return dp[容量];
}
''',
     "两处退化：爬楼梯 用朴素递归（n=45 跑不完），"
     "0/1 背包的容量那层**正序**扫（同一件物品会被装两次）",
     "★ 核心：0/1 背包 —— 容量那层必须逆序"),

    # ── 32 哈夫曼与编码 ─────────────────────────────────────────────
    ("32", '''\
int 最长码长(std::vector<int> 频) {
    if (频.empty()) return 0;
    if (频.size() == 1) return 1;

    std::priority_queue<std::pair<int, int>,
                        std::vector<std::pair<int, int>>,
                        std::greater<std::pair<int, int>>> 堆;
    for (int f : 频) 堆.push({f, 0});

    int 合并次数 = 0;
    while (堆.size() > 1) {
        auto [频1, 深1] = 堆.top(); 堆.pop();
        auto [频2, 深2] = 堆.top(); 堆.pop();
        堆.push({频1 + 频2, 深1});      /* ⚠ 忘了 +1：新结点比两个孩子深一层 */
        合并次数++;
    }
    return 堆.top().second;
}

int 格雷码(int k) {
    return k ^ (k >> 1);
}

int 格雷相邻差一位(int 位数) {
    int 个数 = 1 << 位数;
    for (int i = 1; i < 个数; i++) {
        int 差 = 格雷码(i) ^ 格雷码(i - 1);
        if ((差 & (差 - 1)) != 0) return 0;
    }
    return 1;
}
''',
     "最长码长 每次挑最小的两个都挑对了，但合并之后忘了把深度加一层 —— "
     "返回的永远是 0。这一条检查就是冲着「合并的深度怎么传」来的",
     "★ 核心：六组频次的最长码长 —— 必须每次挑最小的两个"),

    # ── 33 数学库 ───────────────────────────────────────────────────
    ("33", '''\
double 幂(double 底, int 指) {
    double 果 = 1.0;
    for (int i = 0; i < 指; i++) {
        果 *= 底;
    }
    return 果;
}

double 两点距离(double x1, double y1, double x2, double y2) {
    double 横差 = x2 - x1;
    double 纵差 = y2 - y1;
    return std::sqrt(横差 * 横差 + 纵差 * 纵差);
}

int 向下取整(double x) {
    return (int)x;
}

int 向上取整(double x) {
    return (int)std::ceil(x);
}

bool 近似相等(double a, double b) {
    return std::fabs(a - b) < 1e-9;
}
''',
     "向下取整写成 (int)x 强制截断 —— 正数看不出来，-1.5 会被截成 -1 而 floor 该给 -2",
     "向下取整 / 向上取整：特别注意负数"),

    # ── 34 二维 DP ──────────────────────────────────────────────────
    ("34", '''\
#include <algorithm>

int 路径数(int 行, int 列) {
    if (行 <= 0 || 列 <= 0) return 0;
    const int 模 = 1000000007;
    std::vector<std::vector<int>> dp(行, std::vector<int>(列, 1));
    for (int i = 1; i < 行; i++) {
        for (int j = 1; j < 列; j++) {
            dp[i][j] = (dp[i - 1][j] + dp[i][j - 1]) % 模;
        }
    }
    return dp[行 - 1][列 - 1];
}

int 最长公共子序列(std::string a, std::string b) {
    int n = (int)a.size();
    int m = (int)b.size();
    std::vector<std::vector<int>> dp(n + 1, std::vector<int>(m + 1, 0));

    for (int i = 1; i <= n; i++) {
        for (int j = 1; j <= m; j++) {
            if (a[i - 1] == b[j - 1]) {
                dp[i][j] = dp[i - 1][j - 1] + 1;
            } else {
                dp[i][j] = 0;
            }
        }
    }
    int 最大 = 0;
    for (int i = 0; i <= n; i++) {
        for (int j = 0; j <= m; j++) {
            if (dp[i][j] > 最大) 最大 = dp[i][j];
        }
    }
    return 最大;
}
''',
     "最长公共子序列写成「最长公共**子串**」—— 字符不等时直接清零，"
     "连续才算数，一旦中间断开就少算一大截", "★ 核心：最长公共子序列"),

    # ── 35 图与遍历 ─────────────────────────────────────────────────
    ("35", '''\
int 连通块数(std::vector<std::vector<int>> 图) {
    int 结点数 = (int)图.size();
    int 块 = 0;
    for (int i = 0; i < 结点数; i++) {
        if (图[i].empty()) {
            块++;                      /* ⚠ 只数「没有邻居的孤立点」 */
        }
    }
    return 块;
}

int 最短步数(std::vector<std::vector<int>> 图, int 起, int 终) {
    int 结点数 = (int)图.size();
    std::vector<int> 距离(结点数, -1);
    距离[起] = 0;

    std::queue<int> 队;
    队.push(起);

    while (!队.empty()) {
        int u = 队.front();
        队.pop();
        if (u == 终) return 距离[u];
        for (int v : 图[u]) {
            if (距离[v] < 0) {
                距离[v] = 距离[u] + 1;
                队.push(v);
            }
        }
    }
    return 距离[终];
}
''',
     "连通块数用「有几个点没有邻居」来数 —— 这条链上每个点都有邻居，"
     "于是整张连通图会得到 0 块（正确答案 1）。这一课的开头注释里就写着"
     "这个错法实测得到 001000",
     "★ 核心：六张图的连通块数（含孤立点、两个块）"),

    # ── 36 泛洪与哈希 ───────────────────────────────────────────────
    ("36", '''\
int 数岛屿(std::vector<std::vector<int>> 图) {
    if (图.empty() || 图[0].empty()) return 0;
    int 行 = (int)图.size();
    int 列 = (int)图[0].size();
    int 个数 = 0;

    std::vector<std::vector<int>> 见过(行, std::vector<int>(列, 0));

    for (int i = 0; i < 行; i++) {
        for (int j = 0; j < 列; j++) {
            if (图[i][j] != 1 || 见过[i][j]) continue;
            个数++;
            std::vector<std::pair<int, int>> 栈 = {{i, j}};
            见过[i][j] = 1;
            while (!栈.empty()) {
                auto [x, y] = 栈.back();
                栈.pop_back();
                int 方向[8][2] = {{1,0},{-1,0},{0,1},{0,-1},{1,1},{1,-1},{-1,1},{-1,-1}};
                for (int k = 0; k < 8; k++) {
                    int nx = x + 方向[k][0];
                    int ny = y + 方向[k][1];
                    if (nx < 0 || nx >= 行 || ny < 0 || ny >= 列) continue;
                    if (图[nx][ny] != 1 || 见过[nx][ny]) continue;
                    见过[nx][ny] = 1;
                    栈.push_back({nx, ny});
                }
            }
        }
    }
    return 个数;
}

int 不同字符数(std::string s) {
    std::unordered_map<char, int> 次数;
    for (char c : s) {
        次数[c]++;
    }
    return (int)次数.size();
}
''',
     "泛洪走了**八个方向**（含斜对角）—— 这一课的约定是「斜对角不算相连」，"
     "斜着挨着的两片陆地会被数成一座岛", "★ 核心：六张图的岛数 —— 斜对角不算相连"),

    # ── 37 排列组合 ─────────────────────────────────────────────────
    ("37", '''\
long long 排列数(int n, int k) {
    long long 果 = 1;
    for (int i = 0; i < k; i++) {
        果 *= (n - i);
    }
    return 果;
}

long long 阶乘(long long n) {
    long long 果 = 1;
    for (long long i = 2; i <= n; i++) {
        果 *= i;
    }
    return 果;
}

long long 组合数(int n, int k) {
    if (k < 0 || k > n) return 0;
    return 阶乘(n) / (阶乘(k) * 阶乘(n - k));
}

std::vector<long long> 杨辉行(int n) {
    std::vector<long long> 行 = {1};
    for (int i = 0; i < n; i++) {
        行.push_back(0);
        for (int j = (int)行.size() - 1; j >= 1; j--) {
            行[j] = 行[j] + 行[j - 1];
        }
    }
    return 行;
}
''',
     "组合数写成 阶乘相除 —— 小 n 全对，C(50,25) 时 50! 早就溢出 long long，"
     "溢出之后再相除得到的是垃圾（参考答案里那段注释记的就是这件事）",
     "★ 核心：组合数喂到 C(50,25) —— 阶乘相除会溢出"),

    # ── 38 倍增法 ───────────────────────────────────────────────────
    ("38", '''\
long long 快速幂(long long 底, long long 指) {
    long long 果 = 1;
    for (long long i = 0; i < 指; i++) {
        果 *= 底;
    }
    return 果;
}

int 快速幂次数(long long 指) {
    int 次数 = 0;
    while (指 > 0) {
        次数++;
        指 /= 2;
    }
    return 次数;
}

int 跳到的位置(std::vector<int> 跳跃, long long 步) {
    int 当 = 0;
    long long 剩 = 步;
    while (剩 > 0) {
        if (剩 % 2 == 1) {
            当 = 跳跃[当];
        }
        剩 /= 2;
    }
    return 当;
}
''',
     "快速幂用一次次乘的循环 —— 幂本身算得对，但 10 亿次乘法根本跑不完；"
     "整个文件里一个「指 /= 2」都没有，倍增这件事完全没学。"
     "⚠ 抓住它的是**计时那条**（20 次 10 亿级的幂超过 2 秒）——"
     "「是倍增」那条文本检查要等 `跳到的位置` 里那句 `剩 /= 2` 才肯触发，"
     "会误判成「有减半的写法」，所以实测最先红的是计时那条",
     "★ 核心：复杂度——20 次 10 亿级的幂必须在 2 秒内跑完"),

    # ── 39 最短路 ───────────────────────────────────────────────────
    ("39", '''\
std::vector<int> 最短路(std::vector<std::vector<std::pair<int,int>>> 图, int 起) {
    int n = (int)图.size();
    const int 无穷 = 1000000000;

    std::vector<std::vector<int>> d(n, std::vector<int>(n, 无穷));
    for (int i = 0; i < n; i++) d[i][i] = 0;
    for (int u = 0; u < n; u++) {
        for (auto [v, w] : 图[u]) {
            if (w < d[u][v]) d[u][v] = w;
        }
    }
    for (int k = 0; k < n; k++) {
        for (int i = 0; i < n; i++) {
            for (int j = 0; j < n; j++) {
                if (d[i][k] + d[k][j] < d[i][j]) {
                    d[i][j] = d[i][k] + d[k][j];
                }
            }
        }
    }

    std::vector<int> 距(n);
    for (int i = 0; i < n; i++) {
        距[i] = (d[起][i] >= 无穷) ? -1 : d[起][i];
    }
    return 距;
}

int 单点最短路(std::vector<std::vector<std::pair<int,int>>> 图, int 起, int 终) {
    return 最短路(图, 起)[终];
}
''',
     "用 Floyd 三层循环代替 Dijkstra —— 七张图的距离表**全对**，"
     "但这一课要学的是优先队列，O(V³) 的做法把知识点绕过去了",
     "★ 核心：用了优先队列（O(V²) 线性扫描不算过关）"),

    # ── 40 最小生成树 ───────────────────────────────────────────────
    ("40", '''\
int 找根(std::vector<int>& 父, int x) {
    while (父[x] != x) {
        父[x] = 父[父[x]];
        x = 父[x];
    }
    return x;
}

bool 合并(std::vector<int>& 父, int a, int b) {
    int 根a = 找根(父, a);
    int 根b = 找根(父, b);
    父[根a] = 根b;
    return true;
}

int 最小生成树(int 点数, std::vector<std::vector<int>> 边) {
    if (点数 <= 0) return 0;
    if (点数 == 1) return 0;

    std::vector<int> 父(点数);
    for (int i = 0; i < 点数; i++) 父[i] = i;

    std::sort(边.begin(), 边.end(),
              [](const std::vector<int>& a, const std::vector<int>& b) {
                  return a[2] < b[2];
              });

    int 总价 = 0;
    int 用了几条 = 0;
    for (auto& e : 边) {
        if (合并(父, e[0], e[1])) {
            总价 += e[2];
            用了几条++;
        }
    }

    if (用了几条 != 点数 - 1) return -1;
    return 总价;
}
''',
     "合并永远返回 true（不看两点是不是已经连通）—— 最小生成树的循环照旧能跳过绕圈的边，"
     "所以总价还对，但并查集的返回值在撒谎，这一课的核心检查点就是它",
     "★ 核心：并查集 —— 已经连通的两点再合并必须返回 false"),
]

# ── 命令行 ──────────────────────────────────────────────────────────────
参 = sys.argv[1:]
if "--列" in 参:
    print("  错例表：%d 条" % len(错例))
    for 项 in 错例:
        编, _码, 说明, 期望 = 项[:4]
        print("    %s  %-46s 期望被拒：%s%s"
              % (编, 说明, 期望, "   ⚠ 检查空转" if len(项) > 4 else ""))
    sys.exit(0)
筛选 = [a for a in 参 if not a.startswith("-")]

# ── 表自检：**先校验，再动手**（跑之前，不是跑之后）────────────────────
#  ⚠ 这条规矩是这个项目用代价换来的：把校验放在"写文件之后"，
#    校验失败时改动已经落盘，脚本停在半改状态。
#    这里也一样 —— 表坏了就不该去碰 cpp-answers/。
#    实测有效：这条自检第一次跑就抓出两行是 5 个字段（新增的备注字段没被
#    下面的解包接住），而那时它**一条错例都还没跑**。
def 校验表():
    问题 = []
    if not 错例:
        问题.append("错例表是空的")
    见过 = set()
    for i, 项 in enumerate(错例):
        if len(项) not in (4, 5):
            问题.append("第 %d 项的字段数不是 4 或 5（是 %d）" % (i, len(项)))
            continue
        编, 码, 说明, 期望 = 项[:4]
        if not (len(编) == 2 and 编.isdigit()):
            问题.append("第 %d 项的课号不是两位数字：%r" % (i, 编))
        if 编 in 见过:
            问题.append("课号 %s 出现了两次（一课只留一条，账本才不会重复计）" % 编)
        见过.add(编)
        if not 码.strip():
            问题.append("%s 的错答案是空的" % 编)
        if not 说明.strip() or not 期望.strip():
            问题.append("%s 的「错在哪」或「期望被拒的检查名」是空的" % 编)
    应有 = set("%02d" % i for i in range(41))
    if 筛选:
        应有 = 应有 & set(筛选)
    缺 = sorted(应有 - 见过)
    if not 筛选 and 缺:
        问题.append("C++ 共 41 课，错例表漏了：%s" % " ".join(缺))
    return 问题


表问题 = 校验表()
if 表问题:
    print("  **错例表没通过自检，一条都不跑（不动 cpp-answers/）**")
    for p in 表问题:
        print("    · " + p)
    sys.exit(2)

跑到 = [x for x in 错例 if not 筛选 or x[0] in 筛选]
if not 跑到:
    print("  **筛出来的错例是空的** —— 判为「没跑到」，不是「没过」")
    sys.exit(1)

print("═" * 68)
print("  反面验证：C++ 课程 —— 错答案必须被拒")
print("═" * 68)
print("  仓库根：%s" % ROOT)
print("  错例：%d 条   （编译产物落 %s）" % (len(跑到), 私有TEMP))
print()

好 = 0
建不出 = []
没跑到 = []
假通过 = []
抓错项 = []
缺口还在 = []          # 标记为"检查空转"、实测确实全过（符合预期）
缺口变了 = []          # 标记为"检查空转"、实测却开始拒绝了（标记该撤了 ⇒ 失败）
t0 = time.time()

# 先清一次场，保证第一条的模板是课程程序自己建的
shutil.rmtree(ANS, ignore_errors=True)
os.makedirs(ANS, exist_ok=True)

for 项 in 跑到:
    编, 码, 说明, 期望 = 项[:4]
    空转 = 项[4] if len(项) > 4 else None
    文件 = None
    out = ""
    for _ in range(3):
        # ① 让课程程序自己建答题文件（文件名约定由它算，别自己拼）
        跑检查(编)
        候选 = [x for x in os.listdir(ANS) if x.startswith(编)]
        if not 候选:
            continue
        文件 = os.path.join(ANS, 候选[0])
        io.open(文件, "w", encoding="utf-8", newline="").write(码)
        # ② 跑检查
        out = 跑检查(编)
        # ③ 一个 ✓/✗ 都没有 ⇒ 检查没跑到（被别人清场了），重写再跑
        if out.count("✓") or out.count("✗"):
            break
    if 文件 is None:
        建不出.append(编)
        print("  !! %s 建不出答题文件（**没跑到**）" % 编)
        continue
    if not (out.count("✓") or out.count("✗")):
        没跑到.append(编)
        print("  !! %s 一条 ✓/✗ 都没有（**没跑到**，不是没过）" % 编)
        continue

    全过 = "全部通过" in out
    抓到 = ("✗ " + 期望) in out
    if 空转 == "检查空转":
        # 这一条检查是空转的：错答案本来就该全过。
        # ⚠ 但**必须仍然确认它全过** —— 哪天它开始拒绝了，说明课程被改过，
        #   这个标记就该撤掉，不能留着当挡箭牌。
        if 全过:
            缺口还在.append(编)
            print("  --  %s  %s" % (编, 说明))
            print("       ⚠ **这一课的检查是空转的**（错答案全过，与预期一致 —— 见文件头）")
        else:
            缺口变了.append(编)
            print("  XX  %s  %s" % (编, 说明))
            print("       **这个缺口标记该撤了**：错答案现在被拒了"
                  "（实际被拒：%s）" % ("、".join(
                      s.strip()[:40] for s in out.splitlines()
                      if s.strip().startswith("✗"))[:120]))
        continue

    对 = (not 全过) and 抓到
    好 += 1 if 对 else 0
    print("  %s %s  %s" % ("OK " if 对 else "XX ", 编, 说明))
    if 全过:
        假通过.append(编)
        print("       **错误答案被判成通过了** —— 这一课的检查是摆设")
        for l in out.splitlines():
            if "全部通过" in l:
                print("       | " + l.strip()[:100])
    elif not 抓到:
        抓错项.append(编)
        print("       报错了，但没有「✗ %s」这一行。实际被拒的是：" % 期望)
        for l in out.splitlines():
            s = l.strip()
            if s.startswith("✗"):
                print("         " + s[:100])

print()
正常 = [x for x in 跑到 if len(x) <= 4]
if 缺口还在:
    # ⚠ 缺口清单**必须打**，而且要用显眼的框 —— 但它**不能**长得像结论行。
    #   `_verify_negative_all.py` 把「最后一条 == … ==」当结论，
    #   这里要是也写成 == ==，统一入口那栏会显示成
    #   「结论：另有 3 条已确认的检查缺口」—— 看着像这条命令没过，其实它过了。
    #   实测就是这么被自己的账本绊了一下。
    print("  ⚠⚠ 以下 %d 课的反面验证**是空转的**（错答案照样全过，缺口仍在，未修）：%s"
          % (len(缺口还在), " ".join(缺口还在)))
    print("      这三课**不能算进「已反面验证」**。详见各课错例表上方的说明。")
    print()
if 正常 or (not 缺口还在 and not 缺口变了):
    print("  == %d/%d 个错误答案被正确拒绝 ==" % (好, len(正常)))
if 建不出 or 没跑到:
    print("  **没跑到 %d 条**（不算通过）：%s"
          % (len(建不出) + len(没跑到), " ".join(建不出 + 没跑到)))
print("  用时 %.0f 秒" % (time.time() - t0))
shutil.rmtree(ANS, ignore_errors=True)

# ⚠ 退出码必须反映结论。
#   · 「没跑到」和「没过」一样不能当通过 —— 这是项目里反复写的那一条。
#   · 标了"检查空转"的那几条**不判失败**（理由见文件头：缺口要的是被看见，
#     不是把闸门焊死）；但**它们必须真的还是空转的** ——
#     哪天它们开始拒绝了，说明标记过期了，那要判失败。
sys.exit(0 if (好 == len(正常) and not 建不出 and not 没跑到
               and not 缺口变了) else 1)
