# GESP 四级课（第 25~34 课）交付与验收报告

> 这一批课的**内容**在 `lessons/` 里，这份文件记录**验证过程与结果** ——
> 每一课都跑过「参考答案全过 + 错答案必须被拒」两段，命令和输出都在下面。
> 复核方法：`python _build/_verify_py_25_34.py`（不给参数就验 25~34 全部）。

## 一、交付的 10 课 + 10 份参考答案

| 课 | 课文件 | 总行数 | 正文行数 | CHECK_ 条数 | 参考答案 |
|---|---|---|---|---|---|
| 25 | [lessons/25_作用域.py](../lessons/25_作用域.py) | 273 | 168 | 4 | [solutions/25_作用域.py](../lessons/solutions/25_作用域.py)（29 行） |
| 26 | [lessons/26_参数传递.py](../lessons/26_参数传递.py) | 276 | 165 | 5 | [solutions/26_参数传递.py](../lessons/solutions/26_参数传递.py)（28 行） |
| 27 | [lessons/27_递归.py](../lessons/27_递归.py) | 300 | 164 | 4 | [solutions/27_递归.py](../lessons/solutions/27_递归.py)（31 行） |
| 28 | [lessons/28_递推.py](../lessons/28_递推.py) | 295 | 167 | 4 | [solutions/28_递推.py](../lessons/solutions/28_递推.py)（35 行） |
| 29 | [lessons/29_三种排序.py](../lessons/29_三种排序.py) | 268 | 168 | 5 | [solutions/29_三种排序.py](../lessons/solutions/29_三种排序.py)（45 行） |
| 30 | [lessons/30_排序稳定性.py](../lessons/30_排序稳定性.py) | 287 | 146 | 4 | [solutions/30_排序稳定性.py](../lessons/solutions/30_排序稳定性.py)（24 行） |
| 31 | [lessons/31_算法复杂度.py](../lessons/31_算法复杂度.py) | 317 | 164 | 5 | [solutions/31_算法复杂度.py](../lessons/solutions/31_算法复杂度.py)（32 行） |
| 32 | [lessons/32_文件读写.py](../lessons/32_文件读写.py) | 301 | 140 | 5 | [solutions/32_文件读写.py](../lessons/solutions/32_文件读写.py)（26 行） |
| 33 | [lessons/33_异常进阶.py](../lessons/33_异常进阶.py) | 327 | 168 | 4 | [solutions/33_异常进阶.py](../lessons/solutions/33_异常进阶.py)（37 行） |
| 34 | [lessons/34_综合项目.py](../lessons/34_综合项目.py) | 390 | 164 | 5 | [solutions/34_综合项目.py](../lessons/solutions/34_综合项目.py)（50 行） |

- 「正文行数」= 文件开头那段 `#` 讲解块（含代码示例），**全部落在 100~170 之间**；
  后面才是 `CHECK_` 代码（和模板课 17 一样：讲解 136 行 + 检查 91 行）。
- 每课 `CHECK_` 条数 **4~5**，符合 3~5 的要求。
- 每课正文里的**英文双引号 = 0**（引号一律「」；代码示例里的字符串用单引号）——
  逐课用脚本数过。

## 二、验证方法与结果

### ① 模板生成 + 参考答案全过

`_build/_verify_py_25_34.py` 对每一课：**先删掉答题文件** → 跑一次
`python -m hybridengine_light 检查 NN` 确认它**自动生成了完整模板**
（含讲解与「轮到你了」，4000~5900 字）→ 再把参考答案放进去重跑，要求「全部通过」。

```
OK  25  模板 4922 字 · 4 项全过      OK  30  模板 4928 字 · 4 项全过
OK  26  模板 4879 字 · 5 项全过      OK  31  模板 5530 字 · 5 项全过
OK  27  模板 4994 字 · 4 项全过      OK  32  模板 4769 字 · 5 项全过
OK  28  模板 5230 字 · 4 项全过      OK  33  模板 5395 字 · 4 项全过
OK  29  模板 5728 字 · 5 项全过      OK  34  模板 5408 字 · 5 项全过
== 参考答案 10/10 课全过 ==
```

### ② 错答案必须被拒（每课 4~7 个，共 52 个）

```
== 错答案 52/52 个被正确拒绝 ==
```

判据不止「没全过」，还要求报告里出现**指对地方的那句话**（否则只证明"报错了"，
不证明"提示有用"）。各课的错答案：

| 课 | 错答案（都被拒） |
|---|---|
| 25 | 忘了 global / 只 return 没改全局 / 用 range(n) 循环（负数加不上）/ 双倍偷偷碰全局 |
| 26 | 表 = 表 + [东西] / 清空写成 表 = [] / 复制一份却改了原表 / 用 .sort() 改了原表 / 对元组 append |
| 27 | 阶乘没基准情形 / fib 基准写错 / 汉诺塔用公式蒙（没递归）/ 递归求和返回 None / 0! 写成 0 |
| 28 | 斐波那契表用递归（n=200 超时）/ 爬楼梯起点错位 / 跨三级漏了跨 3 级 / 最大子段和把负数当 0 / 表开小一格 |
| 29 | 偷用 sorted / 冒泡 range(n) 越界 / 交换分两步写（丢元素）/ 插入漏 j>=0 / 是排好的用 >= |
| 30 | 接住 sort() 的 None / 忘了 reverse / 升序后整体 reverse（不稳定）/ 名次写成 +1 / 按长度排反了 |
| 31 | 平方次数直接 n*n（没真数）/ 数出次数真去循环（超时）/ 公式写成 n(n+1)/2 / 查找返回 None / 内层从 0 开始 |
| 32 | 忘写 '\n' / 全用手动 open-close（没有 with）/ 忘 encoding（GBK）/ 读成列表没 strip / 数 '\n' 个数 / 读全部忘 return |
| 33 | 自定义异常没继承 Exception / 用 isdigit()（不认负号）/ 安全相除靠 Python 崩 / 失败返回 None / 不接异常 |
| 34 | 忘 int(分数) / split(' ') 遇两个空格崩 / 算平均没管空表 / 名次没跳跃 / 写文件忘换行 / 忘 return / 读成绩用手动 open |

### ③ 全项目测试

```
第一批：Ran 320 tests —— OK
第二批：Ran 7 tests   —— OK
结论：两批都通过 ✅
```

## 三、几条硬要求是怎么落实的

**函数检查喂多组数据（≥4 组，含边界）**：每一条函数检查都是 4~7 组，
边界按题意铺到「空 / 0 / 1 / 负数 / 重复 / 极值」。例如
`汉诺塔` 喂 0/1/2/3/10/25，`算平均` 喂空表/单个/全 0/一正一负，
`最大子段和` 喂空表/全负数/单个 0/正负交错。

**排序那两课真的验了「排对了」**：29 课喂 6 组乱序（含空表、单元素、重复、
负数、完全逆序），判据是「结果 == sorted(原表)」**且长度一致**（有序 + 元素没丢），
另外还要求**就地改**；30 课则分成两步诊断：先看**分数/长度那一列**对不对，
再看同分的先后有没有被打乱 —— 这两件事的提示完全不同（第一版把「忘了 reverse」
误判成「同分被打乱」，已经改掉）。

**检查函数只读不写**：所有检查都不改学员模块里的东西；
32/34 课需要真实文件，一律建在 `tempfile.mkdtemp()` 里并在 `finally` 里删掉，
只对**临时目录**写入，不碰学员的代码和文件。

**报错原文都真跑过**：见 `_build/_probe_err_25_34.py` 与 `_build/_probe_err2_25_34.py`
（两个探针脚本，输出里的每一行就是课程里引用的原文）。课程里出现的有：
`UnboundLocalError: cannot access local variable '次数' …`、
`NameError: name '局部' is not defined`、
`TypeError: 'tuple' object does not support item assignment`、
`RecursionError: maximum recursion depth exceeded`、
`IndexError: list assignment index out of range`、
`IndexError: list index out of range`、
`TypeError: 'NoneType' object is not subscriptable`、
`KeyboardInterrupt`（Ctrl+C 打断跑不完的循环，用 `signal.raise_signal` 真复现）、
`TypeError: write() argument must be str, not int`、
`FileNotFoundError: [Errno 2] No such file or directory: '成绩.txt'`、
`ValueError: invalid literal for int() with base 10: 'abc'`、
`TypeError: catching classes that do not inherit from BaseException is not allowed`、
`ValueError: I/O operation on closed file.`

## 四、过程中真撞到的问题（都不是猜的）

1. **`test_Python展开后是17课` 在我开工前就是红的**（`18 != 17`，
   因为 `17_字符串函数.py` 是新增的）。写这批课期间，
   **第 18~24 课的作者已经把它改成「界面课数 == lessons/ 目录里的课数」+ 一条 ≥30 的地板**，
   比写死数字结实。所以**我没有再去改这个断言** —— 后来别人又加了 35~50 课，
   这条断言一直绿（我最后一次跑全项目测试时 `lessons/` 里是 **50** 个课文件）。
2. **第 34 课一开始有 6 条 `CHECK_`**（超出 3~5）。已把「必须用 with」并进
   `CHECK_读成绩`，现在正好 5 条。
3. **合并时撞到一个真坑**：「with」的扫描原本搜**整个文件**，于是
   「读成绩用手动 open、写成绩用 with」这种半对答案能蒙混过关（反面测试抓到的）。
   改成只看 `def 读成绩` 那一段才对。
4. **第 30 课的诊断逻辑错过一次**：`sorted(得) == sorted(想)` 分不清
   「从低到高」和「同分被打乱」（两者都是同一批元素）。改成比对**分数那一列**。
5. **`in` 列表的复杂度**：`x in 表` 是 O(n)，写在循环里就是 O(n²) ——
   这一课最隐蔽的坑，实测 10 万元素查 1000 次要 0.31 秒（写进第 31 课）。
6. **本机默认编码是 cp936**（`locale.getpreferredencoding()`），
   所以第 32 课「忘了 `encoding='utf-8'`」这条错答案能**真的**被抓住（中文变乱码/解码失败）。
7. **第 28 课的「递归冒充递推」**：如果只比对数值，`斐波那契表(200)` 的递归写法会**永远跑不完**，
   检查会跟着卡死。所以那一课带一个 2~3 秒的**线程超时**判失败
   （第 31 课「不许循环」那一题同理）。
8. **给第 34 课的检查看过 `with`**：这一课的读/写两个函数都想验，但课数规约只允许 5 条检查，
   最后按「检查名要对得上错在哪」的原则并进了 `CHECK_读成绩`。

## 五、复核用的命令

```powershell
cd D:\Users\etgya\Desktop\Project\引擎\LearnCode
$env:PYTHONIOENCODING="utf-8"

python _build\_verify_py_25_34.py          # 这 10 课：参考答案 + 52 个错答案
python _build\run_tests.py                 # 全项目两批测试
python -m hybridengine_light 检查 25       # 单课（学员用法）
python -m hybridengine_light 答案 29       # 看参考答案
```
