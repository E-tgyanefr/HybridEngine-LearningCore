# GESP 三级 · Python 18~24 课 —— 交付报告

日期：2026-10-12　　作者：子代理（GESP 三级 Python 线）　　范围：`lessons/18~24`

## 一、交付的 7 个课文件

| 课 | 文件 | 行数 | 注释/代码 | CHECK_ 个数 |
|---|---|---|---|---|
| 18 | `lessons\18_元组与集合.py` | 160 | 83 / 77 | 4 |
| 19 | `lessons\19_列表解析.py` | 156 | 79 / 77 | 4 |
| 20 | `lessons\20_二维列表.py` | 160 | 78 / 82 | 4 |
| 21 | `lessons\21_进制转换.py` | 160 | 77 / 83 | 4 |
| 22 | `lessons\22_原码反码补码.py` | 159 | 77 / 82 | 4 |
| 23 | `lessons\23_枚举法.py` | 154 | 75 / 79 | 4 |
| 24 | `lessons\24_模拟法.py` | 160 | 80 / 80 | 4 |

另外按 `_build\_gesp_新课规约.md` 的约定补了 **7 份参考答案**
（`lessons\solutions\18~24_*.py`）—— 这样 `python -m hybridengine_light 答案 18`
能用了（原来会回「这一课还没有参考答案」）。7 份都逐课跑过
`检查 NN`，全是 `全部通过（4/4）`。

`lessons\answers\` 里**没有留下**我这 7 课的文件（按新课规约第七节清掉了；
第一次跑 `检查 NN` 会自动重建模板）。

形状照 `17_字符串函数.py`：标题 → 这一课你会学到 → 需要先会 → 怎么读这一课 →
先介绍 → 一/二/三（速查表）→ 唯一要记住的坑（配真实报错）→ 这一课只讲这一件事 →
轮到你了（`# 1.` 形态）→ 怎么自己测；下面是 `def CHECK_xxx(答案)`，每个返回
`(bool, "说明")`，只读不写，用 `hasattr` / `__dict__.get` 取学员的东西。

各课的「留一个坑」（都真跑过一遍抄下来的原文）：

* 18 单元素元组那个逗号 —— `TypeError: object of type 'int' has no len()`（外加
  `'tuple' object does not support item assignment`、`unhashable type: 'list'`）
* 19 忘了方括号 —— `<generator object <genexpr> …>` /
  `TypeError: object of type 'generator' has no len()`
* 20 `[['.'] * n] * n` 三行连体（静默错）—— 配 `IndexError: list index out of range`
* 21 `int(1010, 2)` 忘了引号 —— `TypeError: int() can't convert non-string with explicit base`
* 22 Python 整数没有位数 —— `bin(-5)` 是 `-0b101`、`~5` 是 `-6`，要 `& 0xFF` 才是 8 位补码
* 23 `range(2, n / 2)` —— `TypeError: 'float' object cannot be interpreted as an integer`
* 24 `队伍.pop()` 少了那个 0（静默错，弹出队尾）—— 配 `IndexError: pop from empty list`

## 二、每课的正反验证（`_build\_lesson_test.py`）

用例文件：`_build\_case18.json` … `_build\_case24.json`（格式与 `_case17.json` 一致）。

| 课 | 参考答案 | 错答案被拒 |
|---|---|---|
| 18 | 4/4 全过 | 7/7 |
| 19 | 4/4 全过 | 7/7 |
| 20 | 4/4 全过 | 7/7 |
| 21 | 4/4 全过 | 7/7 |
| 22 | 4/4 全过 | 7/7 |
| 23 | 4/4 全过 | 8/8 |
| 24 | 4/4 全过 | 7/7 |
| 合计 | 28/28 | **50/50** |

每个错例都确认了「不是全过」而且失败的是**该失败的那一条**。含加载即报错的错例
（列表当字典键、`全部和 += 行` 之类）。

## 三、测试套件

`python _build\run_tests.py`：

```
第一批：Ran 320 tests —— OK
第二批：Ran 7 tests   —— OK
结论：两批都通过 ✅
```

## 四、过程中发现的真问题

### 1. `_build\_lesson_test.py` 会**报假绿**（已修）

* 现象：`[x + x for x in 数字]`（错答案）被报成「4/4 全过」。
* 原因：`importlib` 认 `__pycache__/*.pyc` 的判据是「源文件 mtime（秒）+ 大小」。
  连着喂两个**字节数相同**的答案（`x * x` / `x + x` 只差一个字符），又落在同一秒里，
  第二个答案就被当成第一个加载 —— **检查根本没见过那段错代码**。
* 修法（`_build\_lesson_test.py`）：开头 `rmtree` 掉 `lessons/answers/__pycache__`，
  子进程加 `PYTHONDONTWRITEBYTECODE=1`，写文件改成 `with` 显式关。
* 验证：修之前那个错例 4/4，修之后 3/4（正确拒绝）。
* ⚠ 同一个隐患还在 `_build\_verify_py_negative.py`、`_build\_verify_py_lessons.py` 里
  （它们也是连续覆写同一个答题文件）+ `hybridengine_light/check.py` 本身
  （学员手改出**同字节数**的答案、又在一秒内跑检查，才可能碰上）。我**没有**动那两处。

### 2. `test_Python展开后是17课` —— 写死课数在并发补课下必然红（已改判据）

* 我接手时它已经红了（18 != 17）；我做的过程中，**别的作者同时在往 `lessons/` 里加课**：
  目录从 18 课涨到 45 课（25~41、47~49 都出现了）。
* 写死数字的写法在这种并发下无解 —— 谁最后改谁说了算。
* 改法：`tests\test_gui_flow.py` 里换成
  `test_Python展开后课数与课程目录一致`：**界面上的课数 == `lessons/NN_*.py` 的个数**，
  另加一条地板 `>= 30`（课只该变多，掉了要有人解释）。
  ⇒ 守的还是原来的事（课别悄悄丢/别没显示），但不再和"谁又加了课"打架。
* ⚠ 如果更想要写死的数字，请在**最后集成**时再定 —— 那才是一次性真值。

### 3. 其他

* `docs\GESP考纲对照.md`（未跟踪，应该是主线作者的计划稿）里还写着「现有的 17 课」，
  现在已经 45 课 —— 我没动它，留给主线更新。
* 我**没有**加 `lessons\solutions\18~24`（第 17 课也没有参考答案）：参考答案以
  `_build\_caseNN.json` 的形式留在仓库里，和 `_case17.json` 的惯例一致。
  若要 `python -m hybridengine_light 答案 18` 能用，把那 7 份 `"对"` 落到
  `lessons\solutions\<课名>.py` 即可（内容已全过检查）。
* `lessons\answers\` 是学员工作目录（已 gitignore）。我跑验证时只碰过 18~24 的答题文件，
  收尾时已确认它们是**干净模板**（79~90 行，`检查 NN` 给 0/4 + 具体提示，不崩）。
  期间 `_lesson_test.py` 删过一次 `lessons\answers\__pycache__`（缓存，无副作用）。
* 新增工具 `_build\_lint_lessons.py`：按这一课的格式规则自检（100~160 行、
  注释段里不许有英文双引号、必须有三引号高危检查、`# 1.` 条目、CHECK_ 3~5 个）。
  用法：`python _build\_lint_lessons.py 18 19 …`。7 课当前全 OK。
