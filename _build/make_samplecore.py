"""生成 **SampleCore** —— 轻量版的"同构"发行物。

═══ 作者的定位（原话，逐条对着改过）═══

  ① 「Writingcode 更名为 SampleCore」
  ② 「底层和专业版一样」
  ③ 「区别只是无法进行工业级模拟、建模等专业内容，
      游戏编写之类的性能比专业版差而已」

⚠⚠ **我第一版做错了方向，记下来免得再犯：**

  我按"阉割版 = 删掉用不上的课程线"去做 —— 把 Python / C / C++ /
  Java / C# 那五条线全删了，只留 games + enginecourse 九课，
  还写"小了 16 倍"。

  **那是"精简版"，不是作者要的东西。**
  作者要的是：**同一个底层，同一个结构**，差别在
  **能力边界**（做不了工业级模拟/建模）和**性能**（游戏跑得差一些）。
  ⇒ "小了 16 倍"不是优点，是**我做错了的证据**。

═══ 所以 SampleCore 是什么 ═══

    和专业版（`引擎/HybridEngine`，4520 文件 / 1692 MB）**同构**，
    但用轻量实现，因此：

      · 做不了**工业级模拟**（有限元、流体、大规模物理求解…）
      · 做不了**专业建模**（CAD 级网格、参数化装配…）
      · 游戏编写的**性能**比专业版差（轻量解释器 vs 编译型引擎）

    课程体系、引擎概念、组件模型 —— **这些是一样的**。

⚠ 与 `LearnCode`（LearningCore）的关系：
  两者都是轻量版，**同一套代码**。差别只是"发给谁"。
  **不是删功能，是同一底层的两个发行物。**

═══ 怎么用 ═══

    python _build/make_samplecore.py            # 只看要搬什么
    python _build/make_samplecore.py --go       # 真生成
    python _build/make_samplecore.py --go --force   # 覆盖已有的

  ⚠ 生成前会检查目标目录干不干净 —— 里面有你手改过的东西就拒绝覆盖。
"""
from __future__ import annotations

import io
import os
import shutil
import sys
from typing import Any

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
引擎 = os.path.dirname(LC)
目标 = os.path.join(引擎, "HybridEngine-SampleCore")

#: ── 搬什么 ────────────────────────────────────────────────────────────
#
#  ⚠⚠ 清单是**手动定**的，不是 glob。
#     多搬一个目录不会报错，只会让发行物悄悄变样 —— 没人会发现。
#
#  ⚠ **保留全部课程线** —— 因为作者说的是"底层和专业版一样"，
#    **不是**"删掉不用的课程"。我第一版删了五条线，方向错了。
#: ⚠⚠ **用黑名单，不用白名单** —— 这一条是踩出来的。
#:
#:   我第一版是"手动列出要搬的目录"。跑完测试才发现漏了 **10 个**：
#:       ai-answers  ai-solutions  cs-solutions  csharp-answers
#:       embedded-solutions  games-answers  raspi-solutions  exams  tools  android
#:   症状是测试报「二级 Python 题量太少」（`exams/` 没搬）。
#:
#:   ⇒ 白名单的问题是**漏一个不会报错，只会静默少一块内容** ——
#:     而"少了一块"要等测试或学员才发现。
#:     黑名单反过来：**新加的东西默认会被带上**，只有明确不要的才排除。
#:
#:   ⚠ 所以这里只列**明确不要的**，理由都写在旁边。
#: ⚠ 黑名单的值是**理由**（打印给人看）。
#:   `_可有可无` 是**唯一**例外：它表示"这条**允许源头没有**"。
#:   为什么需要它：下面那些"按环境才出现"的目录（虚拟环境、打包中间产物…）
#:   在干净的仓库里**本来就不存在** —— 那是正常的，不该报成黑名单错误。
#:   而"路径式"的条目（如 `_build/_android_cache`）源头没有就是**真的失配**。
_可有可无: Any = None

#: ⚠⚠ **`_build` 里只服务于「已排除目录」的脚本** —— 也要排掉。
#:
#:   理由（实测，不是推断）：
#:     · 这 5 个脚本全都依赖 `android/app/`（上面已排除）
#:       ⇒ 它们**永远不可能成功**。
#:     · 实测 `python _build\build_android.py` → **退出码 1**：
#:           ═══ 安卓构建 ═══
#:             源工程：…\HybridEngine-SampleCore\android\app
#:             构建区：C:\lc_android\app（ASCII —— AAPT2 要求）
#:             XX  Python 素材 / XX libpython / XX 应用图标
#:       报错很清晰、不崩 —— 但**留着等于给下一个人埋雷**：
#:       他会以为"跑一下就好了"，而正确结论是"这条线在 SampleCore 里根本不存在"。
#:
#:   ⚠ 注意这是**文件**黑名单，和上面的目录黑名单分开：
#:     路径一律**正斜杠、相对 LC**，和 `_相对()` 的输出对齐（见下面坑①）。
#:   ⚠ `make_course_data.py` 只给安卓界面生成课程数据（`android/app/web/`），
#:     也在这条线上 —— 一并排掉。
不要的文件 = {
    "_build/build_android.py": "安卓构建驱动（android/ 已排除 ⇒ 永远退出码 1）",
    "_build/prepare_android_assets.py": "拉安卓素材（android/ 已排除）",
    "_build/make_android_icon.py": "生成安卓图标（android/ 已排除）",
    "_build/make_android_web.py": "生成安卓界面（android/ 已排除）",
    "_build/make_course_data.py": "给安卓界面生成课程数据（android/ 已排除）",

    #: ── ⚠⚠ 开发草稿 / 中间快照 —— **该排但原来没排**（黑名单复核时实测出来的）──
    #:
    #:   判据两条，都是**量出来**的，不是"看着像草稿"：
    #:     ① 它们是**内容里的绝对路径大户**。按判据「文件里含 `D:\Users\etgya`」
    #:        实测（改前 63 个文件 / 137 处）：`_deadpaths_before.txt` **35 处**、
    #:        `_heads_dump.txt` **27 处** —— 光这两个就占全部出现次数的 **45%**。
    #:        （顺带把"最该排的东西"和"最不该动的 bin/"区分开：
    #:          `bin/*.dll` 那 13 个文件各 1 处，是 .NET 元数据里的 PDB 路径，
    #:          **正常行为**，见下面 `bin/` 那段。）
    #:     ② 它们是**产物**不是**输入**：`_deadpaths_before.txt` 是
    #:        `_audit_deadpaths.py` 打印出来的"审计前"快照，`_heads_dump.txt`
    #:        是 `_dump_heads.py` 的导出。跑一遍脚本就会重新生成 ⇒ 排掉不丢功能。
    #:
    #:   ⚠⚠ 为什么写成 `(理由, _可有可无)` 而不是硬条目：
    #:     这些东西**随时可能被清掉**（本来就是草稿）。硬条目的语义是
    #:     "源头必须有，否则黑名单失配 ⇒ 拒绝生成" —— 那会让"别人把草稿清理干净"
    #:     变成"生成器罢工"。草稿的语义正好是"允许源头没有"。
    "_build/_deadpaths_before.txt": ("死路径审计的『前』快照（35 处开发者绝对路径）",
                                    _可有可无),
    "_build/_heads_dump.txt": ("课文件头部导出草稿（27 处开发者绝对路径）", _可有可无),
    "_build/_cov_before.txt": ("覆盖率『前』快照（测试工具的输出）", _可有可无),
    "_build/_cpp_neg_run.log": ("反面验证的重定向日志（大小随跑脚本变，实测 0 → 9074 B）",
                                _可有可无),
    "_build/_inline_forms.py.orig": ("补丁脚本留下的 .orig 备份（后缀那组也兜得住）",
                                     _可有可无),

    #: 顶层 `out.txt`（33 B，内容是「第一行 / 第二行 / 第三行」）。
    #: ⚠ 依据同样是**源头自己的**打包脚本：`_build/build_src.py:48`
    #:   已经把 `out.txt` 列进 `排除名字` —— 源码包都不带的东西，发行物更不该带。
    "out.txt": ("顶层测试草稿（build_src.py:48 的排除名单里也有它）", _可有可无),
}

#: ⚠⚠ **学生答题区（`*-answers/`）也要排掉** —— 这一条是**实测**发现漏的，不是推断。
#:
#:   症状（复核时实测）：SampleCore 里躺着 **71 个**答题文件：
#:       lessons/answers/   55 个
#:       cpp-answers/       38 个（含 38 个 LearningCore 当时没有的）
#:       cs-answers/         1 个（LearningCore 里**根本不存在**这个目录）
#:     而 `lessons/answers/` 里同名文件的**正文和 LearningCore 不同**：
#:       LearningCore 是 545~1079 字节（第 35 课只剩 545 字节），
#:       SampleCore   是 7960~8690 字节（一整篇课程正文）。
#:
#:   为什么这是**错的**，三条独立证据：
#:     ① 源头 `.gitignore:16-17` 写着 `answers/` + `*-answers/`，理由原文：
#:        「学生答题区 —— 由课程**首次运行时生成模板**（见 README「answers/」一节）
#:          ⇒ 不应随仓发布，否则新用户会以为里面已有答案」。
#:        ⇒ 发行物更不该带：SampleCore 正是"新用户拿到的那一份"。
#:     ② 运行期**真的会自己建**：`hybridengine_light/learnmodel.py:623`
#:        `文件.parent.mkdir(parents=True, exist_ok=True)`（`_确保模板`，第一次打开时）。
#:        ⇒ 排掉不会让任何一条线少东西。
#:     ③ 它们是**测试的临时答题区**，不是教学内容。`_build/_verify_negative_all.py:70,79`
#:        自己写着「会**清空** lessons/answers/」「这条自己会**清** cpp-answers/」。
#:        ⇒ 里面现在是**最后一次跑反面验证剩下的残渣**（22 个模板 + 39 个旧文件），
#:          内容取决于"最后一次跑测试是什么时候" —— 拿它当发行内容是不可复现的。
#:
#:   ⚠ 与白名单/黑名单那条老原则一致：这里排的是**派生数据**，
#:     不是课程正文 —— 课程正文在 `lessons/`、`*-solutions/` 里，一个都没动。
#:   ⚠ 键是**正斜杠、相对 LC**（见坑①）。`answers/` 是结尾带斜杠的**形态**，
#:     靠 `_结尾匹配` 判；`*-answers/` 同理。
answers_样式 = ("answers/", "-answers/")

不要的目录 = {
    "android": "安卓工程（那是另一条交付线，不属于 SampleCore）",
    "_build/_android_cache": "安卓构建缓存 10.67 MB（实测藏在 _build 里）",
    "build": ("PyInstaller 中间产物", _可有可无),
    "dist": ("PyInstaller 中间产物", _可有可无),
    ".venv": ("虚拟环境", _可有可无),
    "__pycache__": ("Python 缓存", _可有可无),
    ".pytest_cache": ("测试缓存", _可有可无),
    ".git": "版本库（SampleCore 是发行物，不是仓库）",
}

#: ⚠⚠ **.NET 的 `obj/` 也要排掉** —— 这条是**实测量出来的**，依据是源头自己的 `.gitignore`。
#:
#:   实测（在 SampleCore 里按"文件内容含绝对路径"分类统计）：
#:       obj/                76 个文件 / 484 处   ← 就是这一条要解决的
#:       其它（源码/文档）    46 个文件 /  60 处
#:       bin/*.dll|.pdb      26 个文件 /  39 处   ← .NET 元数据，正常行为，**不要动 bin**
#:     合计 148 个文件 / 583 处 ⇒ 加这一条能消掉其中 **83%**。
#:
#:   为什么必须排（三条，都是实测）：
#:     ① **它是"开发者机器的绝对路径"的载体**。源头 `.gitignore` 第一条就是
#:        `bin/` + `obj/`，注释原文：
#:          「.NET 构建产物 —— 里面记着**开发者机器的绝对路径**
#:            （实测：`csharp/**/obj/*.FileListAbsolute.txt` 有 **217 处**
#:             `D:\Users\etgya\...`）⇒ 公开仓不该带这些」
#:        ⇒ SampleCore 是**发给学员的发行物**，比公开仓更不该带。
#:     ② **`obj/` 是纯中间物，`bin/` 不是** —— 这是两者的关键区别：
#:        `bin/` 里有可执行的 dll/exe（`--no-build` 走它，离线可用），
#:        `obj/` 里只有还原缓存、编译缓存、FileListAbsolute 这类记账文件。
#:     ③ **删掉不会让编译型四线跑不起来**（实测）：
#:        `bin/ obj/ 全移走 + 空 NuGet 缓存` ⇒ 四条轨道 csharp/cpp/java/c
#:        **退出码全是 0**（7.2 s，首次重建）。根因：8 个 csproj 的
#:        `PackageReference` **都是 0 个**（纯框架引用）⇒ 还原不需要联网。
#:        ⚠ 而且 `learnmodel.py:468-480` 本来就有"预构建不匹配就退回普通构建"的兜底。
#:
#:   ⚠⚠ 但**`bin/` 不能跟着一起排**：它是 `--no-build` 的依托。
#:      `bin/` 的绝对路径（PDB/dll 里的源码路径）是 .NET 编译器的正常行为，
#:      **不是泄漏** —— 别为了那几个数字把 bin 砍了。
#:   ⚠ 已知问题（**没修**，写进 README 的"已知问题"）：实测仓库里的
#:      `HybridEngineLight.dll` 是 **167936 B**，而重新构建是 **167424 B**
#:      ⇒ 预构建的 bin **不是当前源码构建的**。可自愈（首次运行会退回普通构建重建），
#:      但要在文档里说出来。
#:   ⚠ `__pycache__` **本来就排了**（见上面 `不要的目录`），而且实测有效：
#:      刚生成完 `__pycache__` 目录数 = 0、`.pyc` = 0；
#:      跑完一次 `run_tests.py` 才出现 10 个目录 / 123 个 .pyc / 1.33 MB
#:      ⇒ 那些是**运行期产物**，不是生成器漏排，**无需再加**。
obj_样式 = ("obj",)

#: ⚠⚠ **缓存 / 系统垃圾** —— 按**最后一段的名字**判，**任意深度**都算命中。
#:
#:   为什么必须单独一组（复核时实测出来的）：
#:     `不要的目录` 里的 `"__pycache__"` 是一条**相对路径键**，
#:     只在**顶层**那个 `__pycache__` 上命中（相对路径正好等于它）。
#:     `hybridengine_light/__pycache__` 查 `不要的` 是 **False**，
#:     也不属于 `answers` / `obj` 形态 ⇒ **静默漏排**。
#:   实测：源头有 **12 个** `__pycache__`
#:     （`ai/`、`lessons/`、`lessons/answers/`、`lessons/solutions/`、
#:      `hybridengine_light/pyengine/games/` …），黑名单只管得住顶层那一个。
#:
#:   ⚠ 当时没出事，靠的是 `copytree` 那边**另挂**的一份
#:     `shutil.ignore_patterns("__pycache__", "*.pyc", ".pytest_cache")`
#:     —— 那**正是坑②**（清单和复制两套规则）的复发形态：
#:     清单说的是"要搬 12 个 __pycache__"，复制实际一个都不搬。
#:   ⇒ 把名字形态收进**同一份规则**（`_不要`），清单/复制/清扫三处共用。
#:
#:   ⚠ `.DS_Store` / `Thumbs.db` 的依据是**源头自己的**打包脚本：
#:     `_build/build_src.py:48` 的 `排除名字 = {"out.txt", ".DS_Store", "Thumbs.db"}`。
#:     SampleCore 是发行物，比源码包更不该带这些。
#:   ⚠ `.venv` / `node_modules` / `.vs` / `.idea` 同样有先例：
#:     `build_src.py:46` 的 `排除目录` 里就有 `node_modules` / `.vs` / `.idea`。
#:     放进"按名字判"这一组，是为了**任意深度**都排 —— 虚拟环境或 IDE 目录
#:     出现在子目录里（`tools/`、`csharp/` 下）同样不该发行。
#:   ⚠ 这一组**会**被 `_查黑名单` 断言命中数 > 0：`__pycache__` 在源头一定存在
#:     （实测 12 个），所以这一组有硬保证，名字写错会当场报错。
缓存垃圾 = ("__pycache__", ".pytest_cache", ".DS_Store", "Thumbs.db",
            ".venv", "node_modules", ".vs", ".idea")

#: ⚠ **补丁 / 编辑器留下的后缀** —— 一律不发行。
#:
#:   实测：发行物里躺着 `_build/_inline_forms.py.orig`（1928 B）——
#:   那是 `_apply_any.py` / `_fix_*.py` 这批**就地改文件**的脚本留下的备份。
#:   这类脚本还在用 ⇒ 这种后缀**会再出现**，所以按**形态**排，不按文件名逐个写。
#:   ⚠ `.pyc` 也并进这里：原先"清单跳过 .pyc、复制靠 ignore_patterns"又是两套规则。
#:   ⚠ 这一组**不做"必须命中"断言**：干净仓库里可能一个都没有，
#:     断言会让「没有垃圾」反而变成生成失败（见 `_查黑名单` 的说明）。
补丁后缀 = (".orig", ".rej", ".bak", ".pyc")

#: ⚠⚠ **别拿 `_build/build_src.py` 的排除表来"统一"这里** ——
#:   两条发行线**目的不同**，排除规则**故意不一样**：
#:
#:      `build_src.py`（源码包）          `make_samplecore.py`（发行物）
#:      ─────────────────────────────    ──────────────────────────────────
#:      `:46` 排除目录含 `bin` / `obj`    `bin` **必须带**（`--no-build` 的依托）
#:      `:74` `answers` **或**            `answers/` 排掉（同）
#:           `solutions` ⇒ 排             `*-solutions/` **必须带**（**不同！**）
#:
#:   为什么必须不同（两条都是实测/断言，不是口味）：
#:     · `bin/`：排掉它 ⇒ 学员每次运行都要重新编译；留着它 `--no-build` 才成立。
#:       （而 `bin/` 里的绝对路径是 .NET 元数据，**不是泄漏** —— 见 `obj_样式` 那段。）
#:     · `*-solutions/`：SampleCore 的测试**依赖**它 ——
#:       `tests/test_ai.py:503` 断言「`ai-solutions/` 里每个课都有参考答案」，
#:       排掉 ⇒ **测试直接挂**，不是"少了点内容"。
#:
#:   ⚠ 两条发行线**唯一的共同点**是「学生答题区（`answers/`）都不要」。
#:     想复用规则时**只复用这一条**，别把整张排除表搬过来。
#:   ⚠ 定位上也是同一个道理：SampleCore 是**同构**发行物，**不是瘦身包**
#:     （见文件开头那段 —— 我第一版按"删掉用不上的课程线"做，方向就错了）。
#:     ⇒ **体积大 ≠ 该排**。判据只有两条：**学员用得上** / **测试依赖**。
#:     按这两条判据，`_build/_*模板扩写.md|_digest.txt` 那批开发笔记、
#:     `第26-40课_交付与验证记录.md`、`docs/安卓版说明.md` 都**判定为保留**。

#: ⚠ 目录黑名单 + 文件黑名单 = **唯一一份规则**，
#:   清单（os.walk）、复制（copytree）、**生成后清扫**三处共用它（见下面坑②）。
不要的 = {**不要的目录, **不要的文件}

#: ── 怎么判断"不要" ────────────────────────────────────────────────────
#
#  ⚠⚠⚠ **两处实测出来的坑，都在这一小段里**（2026-xx 复核时发现）：
#
#  ① `os.path.relpath` 在 Windows 上返回**反斜杠**：
#         relpath(LC/_build/_android_cache, LC)  ==  '_build\\_android_cache'
#     而黑名单里写的是 `'_build/_android_cache'`（正斜杠）
#     ⇒ `in 不要的` **永远为 False**，排除**静默失效**。
#     实测后果：SampleCore 里真躺着 `_build/_android_cache`
#     （10.67 MB，占发行物 35.6% 的体积）—— 而脚本注释里写着"已排除"。
#
#  ② **清单和复制原来是两套规则**：
#       清单（os.walk）按 `不要的` 排；
#       复制（shutil.copytree）只按 `ignore_patterns("__pycache__", …)` 排。
#     ⇒ 清单上"不搬"的东西**照样会被复制过去**。
#     **"清单"和"动作"必须是同一份规则** —— 否则清单只是好看的说明，
#     而发行物的实际内容要靠人去比对才发现不一样。
#
#  ⇒ 下面几个函数就是那一份规则，**清单和复制共用**。
#
#  ⚠⚠ 它们必须定义在**用到它们的地方之前** ——
#     我第一版把 `_键()` 放到"清单"循环后面，一跑就是
#     `NameError: name '_键' is not defined`（模块级代码是顺序执行的）。


def _相对(p: str) -> str:
    """相对 LC 的路径，**统一成正斜杠**（见上面坑①）。"""
    return os.path.relpath(p, LC).replace(os.sep, "/")


def _键(p: str) -> str:
    """黑名单查表用的键 —— **所有查询都必须走这里**（见上面坑①）。

    ⚠⚠ 存在的意义就是**让"忘了归一化"不可能发生**：
       如果有人在某处直接写 `os.path.relpath(...) in 不要的`，
       Windows 上返回反斜杠 ⇒ 永远 False ⇒ 排除**静默失效**
       （而且"排除失效"没有任何报错，只有发行物悄悄变样）。
       统一从这里过，就不必再靠人记得 replace。
    """
    return _相对(p)


def _结尾匹配(键: str) -> bool:
    """`*-answers` / `answers` 这类**形态**条目：任意深度、任意前缀都算命中。

    ⚠ 为什么不把每一条都写全（`lessons/answers`、`cpp-answers`、`cs-answers`…）：
      带 `-answers` 的目录**会随课程线增加**，逐个写全就是回到白名单那个老毛病
      —— **新加一条线，它就静默漏一个**。而"学生答题区一律不发行"是
      **一以贯之的规则**（见源头 `.gitignore:16-17`）
      ⇒ 规则该按形态写，不该按目录名逐个写。

    ⚠⚠ **这里我自己踩过一次，记下来**：第一版写的是
        `键.endswith(s)`，而 `s` 是 `"-answers/"`（带尾斜杠）
        ⇒ 拿 `"cpp-answers"` 去比 `"-answers/"` **永远 False**，
          一个都命不中 —— 和坑①（反斜杠）是**同一类错误**：
          "规则看起来写了，实际一条都不生效，而且不报错"。
        ⇒ 所以 `_查黑名单()` 里加了"形态条目命中数必须 > 0"的断言。
        ⚠ 判据用 `fnmatch` 的 `*` 来吸收"有没有前导路径"，
          比手写 startswith/endswith 组合不容易错。

    ⚠ 存在的意义和 `_键()` 一样：**让三处判断不可能不一致**（见坑②，
      清单和复制曾经是两套规则）。
    """
    import fnmatch
    for s in answers_样式 + obj_样式:
        干 = s.rstrip("/")
        #: `干` 是目录名（如 `-answers`）或相对路径片段（如 `answers`）。
        #: 目录名可能在任意深度 ⇒ 三个形态都要试：
        #:     顶层就叫这个名  |  在某个目录下面
        if (fnmatch.fnmatch(键, "*" + 干)
                or fnmatch.fnmatch(键, "*/" + 干)
                or 键 == 干):
            return True
    return False


def _不要(键: str) -> bool:
    """**唯一**的"要不要排掉"判断 —— 清单、复制、清扫**三处共用**。"""
    if 键 in 不要的 or _结尾匹配(键):
        return True
    #: ⚠ 缓存/垃圾：按**最后一段的名字**判 ⇒ 任意深度都排（见 `缓存垃圾`）。
    #:   少了这一条，`hybridengine_light/__pycache__` 会**静默漏排**。
    if 键.rsplit("/", 1)[-1] in 缓存垃圾:
        return True
    #: ⚠ 补丁备份 / 字节码：按**后缀**判（见 `补丁后缀`）。
    return 键.endswith(补丁后缀)


def _要忽略(根: str, 名字们) -> set:
    """给 `shutil.copytree` 用的 ignore —— 按**同一份**黑名单排（见上面坑②）。

    ⚠ 目录和文件都要过：只判 `isdir` 的话，`不要的文件` 里的脚本照样会被复制。
    """
    return {d for d in 名字们
            if _不要(_键(os.path.join(根, d)))}


def _清扫(根目录: str):
    """生成后清扫：把**黑名单说不要的**东西从生成目录里删掉。

    返回 `(删掉的文件数, 删掉的目录数, 例子列表)`。

    ⚠⚠ 为什么是"**重放同一份黑名单**"，而不是另写一套删法：
      原来的清扫只认 `__pycache__` / `*.pyc`。复核时**实测**发现不够 ——
      生成完之后**再跑一遍 `run_tests.py`**，`learnmodel.py` 的「确保模板」
      会在首次打开课程时就地建 `*-answers/` 答题文件 ⇒
      "学生答题区一律不发行"这条规则**静默失效**（详见 `主()` 里那段实测）。
      ⇒ 让清扫走**同一个** `_不要()`：以后再冒出别的东西，不用再补一次清扫。

    ⚠ 键的归一化必须走**同一套** `replace(os.sep, "/")`（坑①）。
      这里要是再写一遍 `os.path.relpath(...)`，坑① 就原地复活了
      —— 黑名单里 `_build/...` 是正斜杠，relpath 给回来的是**反斜杠**，
      于是"该删的一个都没删"而且**不报错**。
    """
    def 键T(p: str) -> str:
        return os.path.relpath(p, 根目录).replace(os.sep, "/")

    删文件 = 删目录 = 0
    例子 = []
    #: ⚠ `topdown=False`：先到最深处。删父目录时里面的文件已经处理过了。
    for 底, 子目录们, 文件们 in os.walk(根目录, topdown=False):
        #: ⚠ `.git` **不扫也不删** —— 万一下面真有个仓库，别顺手毁掉。
        if os.path.basename(底) == ".git":
            continue
        子目录们[:] = [d for d in 子目录们 if d != ".git"]
        for f in 文件们:
            p = os.path.join(底, f)
            if not _不要(键T(p)):
                continue
            try:
                os.remove(p)
                删文件 += 1
                例子.append(键T(p))
            except OSError:
                pass
        for d in 子目录们:
            p = os.path.join(底, d)
            if not _不要(键T(p)):
                continue
            n = sum(len(_f) for _r, _d, _f in os.walk(p))
            try:
                shutil.rmtree(p)
            except OSError:
                continue
            删目录 += 1
            删文件 += n
            例子.append("%s/（整个目录，%d 个文件）" % (键T(p), n))
    return 删文件, 删目录, 例子


def _查黑名单() -> list:
    """⚠⚠ **写之前**的校验 —— 黑名单里每一条都要在源头真的存在。

    ⚠ 为什么必须在 `--go` 之前跑：
      我踩过一次 —— 校验写在 `write()` 之后，assert 一失败改动已经落盘，
      脚本停在**半改状态**（比不改还糟：不知道改了哪些）。
      ⇒ 所以这个函数只读、不写，并且在任何复制/改动**之前**调用。

    它抓的是这一类**静默失效**：
      · 键打错字（`_buid/...`）⇒ 永远命中不了 ⇒ 该排的没排
      · 源头改名/挪走（黑名单是**相对 LC 的路径**，源头一动就失配）
      · ⚠ 尤其是反斜杠/正斜杠写错（见坑①）—— 键里写反斜杠就永远命中不了
    """
    问题 = []
    for 键 in sorted(不要的):
        if "\\" in 键:
            #: ⚠ 这条就是坑①的复发形态：键写反斜杠，`_相对()` 给正斜杠 ⇒ 永不命中
            问题.append("键里有反斜杠（必须用正斜杠）：%r" % 键)
            continue
        值 = 不要的[键]
        可有可无 = isinstance(值, tuple) and _可有可无 in 值
        if not os.path.exists(os.path.join(LC, *键.split("/"))) and not 可有可无:
            问题.append("源头不存在：%s" % 键)
    #: ⚠⚠ **形态条目也要校验** —— 否则 `answers_样式` / `obj_样式` 写错
    #:   （比如把 "obj" 写成 "objs"）会**静默失效**：一个都命不中，
    #:   却没有任何报错，发行物又悄悄把 76 个 obj / 71 个答题文件带上。
    #:   这正是坑①（反斜杠）的同一类错误。
    #:   ⚠ 判据是**每一组各自命中数 > 0**，不是"合起来 > 0" ——
    #:     合起来算的话，`obj_样式` 写错会被 `answers_样式` 的命中**掩盖掉**。
    #:   ⚠ `缓存垃圾` 这一组也放进来：它里面 `__pycache__` 在源头**一定存在**
    #:     （实测 12 个），所以这一组有硬保证，写错名字会**当场报错**。
    #:     同组的 `.DS_Store` / `Thumbs.db` 是**文件**、走不到下面这个"只走目录"
    #:     的循环 ⇒ 它们靠的是"万一出现就排掉"，不靠这条断言。
    #:   ⚠ `补丁后缀` **故意不进来**：干净仓库里 `.orig` / `.bak` 可能一个都没有，
    #:     加进来会把「没有垃圾」判成「形态写错了」⇒ 生成被拒。
    for 名, 样式 in [("answers_样式", answers_样式), ("obj_样式", obj_样式),
                     ("缓存垃圾", 缓存垃圾)]:
        本组 = []
        for 根, 子目录们, _文件 in os.walk(LC):
            子目录们[:] = [d for d in 子目录们 if d != ".git"]
            for d in 子目录们:
                键 = _键(os.path.join(根, d))
                #: ⚠ 只按**这一组**判，别用 `_结尾匹配`（那是两组合起来的）
                import fnmatch
                if any(fnmatch.fnmatch(键, "*" + s.rstrip("/"))
                       or fnmatch.fnmatch(键, "*/" + s.rstrip("/"))
                       or 键 == s.rstrip("/") for s in 样式):
                    本组.append(键)
        if not 本组:
            问题.append("%s %r 一个目录都没命中 —— 形态写错了？" % (名, 样式))
        else:
            print("    （%s %r 命中 %d 个目录：%s）"
                  % (名, 样式, len(本组), "、".join(sorted(本组)[:6])))
    return 问题


#: ⚠ 顶层**逐项判断**，不写死清单。
目录们 = []
for _名 in sorted(os.listdir(LC)):
    _p = os.path.join(LC, _名)
    if _不要(_键(_p)):
        continue
    if os.path.isdir(_p):
        #: `_build` 特殊：大部分脚本是给 LearningCode 的课程线做生成/校验的，
        #: 但**测试要跑**（`run_tests.py`）、打包要用 —— 所以整个带上，
        #: 靠 `不要的` 排掉里面的大件和跑不通的那几个。
        目录们.append((_名, ""))
    elif os.path.isfile(_p):
        #: ⚠ 顶层文件也要 —— `README.md` 里有版本号、
        #:   `hybridengine-light-ctl` 是命令行入口、`.gitignore` 是发布时要的。
        #:   我第一版**只搬了 LICENSE 和 README**，漏了别的。
        目录们.append((_名, ""))

#: ── 要改的文件 ────────────────────────────────────────────────────────
#
#  ⚠⚠ **这一版几乎没有"删功能"的改动** —— 因为 SampleCore 和
#     LearningCore 是同一套代码，差别在能力/性能，不在结构。
#     只有"两个产品同时装着时不要互相干扰"这一类才必须改。
改动 = [
    #: ① 进度文件分开 —— 两个发行物各记各的。
    #:    ⚠ 不改的话，装了 LearningCore 再来装 SampleCore，
    #:      进度会串（而学员会以为"我的记录丢了"）。
    ("hybridengine_light/progress.py",
     '文件名 = ".学习进度.json"',
     '文件名 = ".示例进度.json"      #: ⚠ 和 LearningCore 分开记'),
    #: ② ⚠⚠ **这是两个产品唯一的"界面分叉点"** —— 作者定的：
    #:   「学习内核与样本内核是两个不同的程序，UI 应该分别重新设计」，
    #:   而且明确选了「**完全不共用，各写一份**」。
    #:
    #:   ⇒ LearningCore 开 `learn_gui`（学习界面：课程树 + 检查 + 参考答案）
    #:     SampleCore   开 `sample_gui`（游戏开发 IDE：组件面板 + 场景树 + 代码 + 运行窗）
    #:
    #:   ⚠ 两个界面文件**互不 import**（只在 `run.py` 这一行按名字选）。
    #:     所以"完全不共用"不是口号，它体现在 import 图上。
    ("hybridengine_light/run.py",
     '图形界面模块 = "hybridengine_light.learn_gui"',
     '图形界面模块 = "hybridengine_light.sample_gui"'),
]


def 主():
    #: ⚠⚠ **防呆第一**：目标目录**绝不能**是源头自己，也不能把源头套在里面。
    #:   这个脚本有两处 `shutil.rmtree`（`--force` 清空 + 末尾清扫），
    #:   路径一旦推错，"清空"清掉的就是**源头**（14 MB 课程内容）。
    #:   `LC` / `引擎` / `目标` 都是从 `__file__` 推的，正常推不错；
    #:   正因为如此，没有任何人会去检查它 —— 所以这里显式挡一道。
    _绝 = os.path.abspath(目标)
    _源 = os.path.abspath(LC)
    if _绝 == _源 or _源.startswith(_绝 + os.sep):
        print("  XX 目标目录指向源头自己 —— 拒绝执行（会删掉源头）：")
        print("       源  ：%s" % _源)
        print("       目标：%s" % _绝)
        return 1
    print("  ═══ 生成 SampleCore ═══")
    print("    源：%s" % LC)
    print("    目标：%s" % 目标)
    print()
    print("    ⚠ 定位：**和专业版同构** —— 差别在能力边界和性能，")
    print("      不是「删掉用不上的功能」。所以课程线**全都要**。")
    print()

    #: ⚠⚠⚠ **校验在最前面** —— 必须在任何复制/改动**之前**（见 `_查黑名单`）。
    #:   放在 write 之后的话，发现问题时改动已经落盘 ⇒ 半改状态。
    _问题 = _查黑名单()
    if _问题:
        print("  XX 黑名单有 %d 处对不上源头 —— **没有动任何文件**：" % len(_问题))
        for _x in _问题:
            print("       · %s" % _x)
        print()
        print("  ⚠ 黑名单是**相对源头的路径**。源头改名/挪走之后必须同步改这里，")
        print("    否则那一条**静默失效**（该排的没排，而发行物不会报错）。")
        return 1
    print("  === 黑名单校验（写之前）===")
    print("    OK  %d 条全部对得上源头" % len(不要的))
    print()

    print("  === 要搬的 ===")
    总文件 = 总字节 = 0
    for 名, 说明 in 目录们:
        源 = os.path.join(LC, *名.split(":"))
        if not os.path.exists(源):
            print("    ??  %-24s 不在（跳过）" % 名)
            continue
        if os.path.isfile(源):
            文件们 = [源]
        else:
            文件们 = []
            for 根, 子目录们, 文件 in os.walk(源):
                子目录们[:] = [d for d in 子目录们
                              if not _不要(_键(os.path.join(根, d)))]
                for f in 文件:
                    #: ⚠ 这里原来有一句 `if f.endswith(".pyc"): continue` ——
                    #:   现在 `.pyc` 已经在 `_不要()` 里（`补丁后缀`）⇒ 删掉，
                    #:   让"清单"和"复制"真的只有**一份**判断（见坑②）。
                    p = os.path.join(根, f)
                    if _不要(_键(p)):
                        continue
                    文件们.append(p)
        n = len(文件们)
        b = sum(os.path.getsize(x) for x in 文件们)
        总文件 += n
        总字节 += b
        print("    OK  %-24s %4d 个文件  %7.2f MB   %s"
              % (名, n, b / 1e6, 说明))

    print()
    print("    合计：%d 个文件  %.2f MB" % (总文件, 总字节 / 1e6))

    if "--go" not in sys.argv:
        print()
        print("  （只读检查。加 --go 才真生成）")
        return 0

    #: ── 干净检查 ──────────────────────────────────────────────────────
    #
    #  ⚠ 这个脚本会**覆盖**目标目录。有人在 SampleCore 里手改过东西的话，
    #    覆盖就把它冲掉了 —— 而那种损失是不可发现的
    #    （人不会记得自己改过哪一行）。
    if os.path.isdir(目标) and "--force" not in sys.argv:
        有 = os.listdir(目标)
        if 有:
            print()
            print("  XX %s 不是空的（%d 项）：" % (目标, len(有)))
            for x in 有[:8]:
                print("       %s" % x)
            print()
            print("  ⚠ 覆盖会把你改过的东西冲掉。确认要重来就加 --force：")
            print("      python _build/make_samplecore.py --go --force")
            return 1

    print()
    print("  === 生成 ===")
    if os.path.isdir(目标) and "--force" in sys.argv:
        print("    清掉旧目录")
        shutil.rmtree(目标)
    for 名, _ in 目录们:
        源 = os.path.join(LC, *名.split(":"))
        if not os.path.exists(源):
            continue
        去处 = os.path.join(目标, *名.split(":"))
        if os.path.isfile(源):
            os.makedirs(os.path.dirname(去处), exist_ok=True)
            shutil.copy2(源, 去处)
        else:
            #: ⚠⚠ 这里**只过 `_要忽略`** —— 见 `_要忽略` 上面的坑②。
            #:   原来这里还挂着一份
            #:   `shutil.ignore_patterns("__pycache__", "*.pyc", ".pytest_cache")`，
            #:   那是**第二套规则**：清单按 `不要的` 判、复制按模式判，
            #:   两边说的不是一回事（清单会数出 12 个 `__pycache__` 说"要搬"，
            #:   复制实际一个都不搬）。复核时把 `__pycache__` / `.pyc` /
            #:   `.pytest_cache` / `.orig` / `.bak` 这些**名字形态**收进了
            #:   `_不要()` ⇒ 一份规则就够，`ignore_patterns` 已删。
            shutil.copytree(源, 去处,
                            ignore=lambda 根, 名字们: _要忽略(根, 名字们),
                            dirs_exist_ok=True)
        print("    写了 %s" % 名)

    print()
    print("  === 改内容 ===")
    命中 = 0
    for 相对, 旧, 新 in 改动:
        p = os.path.join(目标, 相对)
        if not os.path.exists(p):
            print("    ??  %s 不在（跳过）" % 相对)
            continue
        t = io.open(p, encoding="utf-8").read()
        if 旧 not in t:
            print("    XX  %s 里找不到要改的那句：" % 相对)
            print("          %s" % 旧[:70])
            continue
        io.open(p, "w", encoding="utf-8", newline="").write(t.replace(旧, 新, 1))
        命中 += 1
        print("    OK  %s" % 相对)
    print("    改了 %d / %d 处" % (命中, len(改动)))

    #: ⚠⚠ **不能自己写一份全新的 README**。
    #:   我第一版就是那么干的（写了一份只讲 SampleCore 的），
    #:   结果 SampleCore 自己的测试**挂了 14 条**，其中一条是：
    #:       `test_README里的版本与代码一致` —— 它要在 README 里找
    #:       「当前 **X.Y.Z**」那句，而我的新 README 里没有。
    #:   ⇒ 教训：**发行物要保留源头的事实**（版本号、课数、命令…），
    #:     只**追加**它自己的说明。另写一份等于把这些事实丢了 ——
    #:     而测试会先发现这件事（幸好它们查了）。
    #:   ⚠ `尾注`（「已知问题」那一节）**必须由生成器追加** ——
    #:     手改 SampleCore/README.md 是白改：这个文件每次生成都重写一遍。
    p = os.path.join(目标, "README.md")
    if os.path.exists(p):
        t2 = io.open(p, encoding="utf-8").read()
        io.open(p, "w", encoding="utf-8", newline="\n").write(头注 + t2 + 尾注)
        print("    拷了 LICENSE、README.md（前面加了 SampleCore 说明，"
              "末尾追加了「已知问题」）")
    else:
        io.open(p, "w", encoding="utf-8", newline="\n").write(头注 + 尾注)
        print("    ⚠ 源头没有 README，只写了说明")

    #: ── ⚠⚠ 生成后清扫：把**黑名单说不要的**东西清掉 ──────────────────────
    #
    #  ⚠⚠ 原来是"只删 `__pycache__` / `*.pyc`"。**复核时实测发现不够**：
    #
    #    生成器**确实**排掉了 `*-answers/`（刚生成完实测 = 0 个），
    #    但我**生成完又跑了一遍 `run_tests.py`**（每次必做的回归），
    #    而 `hybridengine_light/learnmodel.py` 的「确保模板」会在
    #    **第一次打开课程时**就地建答题文件 ⇒ 答题区**又回来了**。
    #
    #    实测（同一次「生成 → 跑测试」）：
    #        文件数 669 → **674**
    #        ai-answers/01_线性回归.py          10867 B
    #        embedded-answers/02_按钮控制.py      9404 B
    #        engine-answers/01_场景与对象.py       5008 B
    #        raspi-answers/01_点亮LED.py         11845 B
    #        lessons/answers/00_跑通第一个程序.py    6223 B
    #      ⚠ 这 5 个跟源头里那批**不是同一份内容**（源头 `01_线性回归.py` 是 10391 B）
    #        ⇒ 它们不是"复制漏了"，是**生成之后新造出来的**。
    #      ⚠ 而"学生答题区一律不发行"是`.gitignore:16-17` 白纸黑字的规则
    #        ⇒ 这 5 个文件**静默留在发行物里**，和规则直接矛盾。
    #
    #  ⇒ 清扫改成**重放同一份黑名单**（`_不要`），不再另写一套：
    #      "清单 / 复制 / 清扫"三处永远一致（坑②的正面形态）。
    #      它同时覆盖 `__pycache__`、`*.pyc`、`*-answers/`、`.pytest_cache`…
    #      —— **以后再冒出别的东西，不用再补一次清扫**（这条最要紧）。
    #
    #  ⚠ 给用的人：正确顺序是「**生成 → 跑测试 → 再生成一次**」。
    #    实测：`PYTHONDONTWRITEBYTECODE=1` **只挡得住 `.pyc`**
    #    （跑完测试 `.pyc` = 0），**挡不住** `*-answers/`（照样 +5 个文件）
    #    ⇒ 不能拿环境变量代替「再生成一次」。这一条写进了 README 的「已知问题」。
    #
    #  ⚠⚠ 局部变量**不能叫 `目录们`** —— 外层有一个同名的清单变量，
    #     这里再赋值会让 Python 把整个函数里的 `目录们` 都当成局部的
    #     ⇒ `UnboundLocalError: cannot access local variable '目录们'`
    #     （报错指向上面那行 `for 名, 说明 in 目录们:`，和这里看着无关）。
    _删文件, _删目录, _例 = _清扫(目标)
    print("    清扫：%s"
          % (("删掉 %d 个文件 / %d 个目录（黑名单说不要的，"
              "含生成后被跑测试带回来的）" % (_删文件, _删目录))
             if (_删文件 or _删目录) else "没有要清的（干净）"))
    for _x in _例[:6]:
        print("       - %s" % _x)
    if len(_例) > 6:
        print("       …… 还有 %d 项" % (len(_例) - 6))

    print()
    print("  ⚠ 生成目录里的 `hybridengine_light/` 是**复制**来的 ——")
    print("    改它会在下次生成时被覆盖。要改公共代码请改 LearnCode 那边。")
    return 0


头注 = """> # ⚠ 这一份是 **SampleCore**
>
> 本目录由 `引擎/%(源名)s/_build/make_samplecore.py` 生成。
> 下面的内容**原样来自 LearningCore**（版本号、课数、命令都是真的）——
> 因为 SampleCore 和它**同一套代码**。
>
> ## ⚠ 定位：用 HybridEngine 做出来的东西，不是重构
>
> **底层依旧是 HybridEngine。** LearningCore / SampleCore 都是
> **用它做出来的编程教学软件** —— 在**同一个底层**上做出来的**应用**。
> 不是把引擎重写了一遍，也不是另起炉灶。
>
> ⚠ 但**别把它读成「运行期链接引擎 dll」**：实测两边的源码里都没有加载
> `hybridengine.dll` / `ms_engine_*` 的地方（`hybridengine_light/` 里唯一的
> `ctypes` 出现在 `editor.py:53`，用途是开 Windows 控制台的 ANSI）。
> 准确的说法是三层：**同一个底层** · **同一个上层代码库** ·
> **学员侧零依赖**（"装个 Python 就能跑"说的是"学员要装什么"，
> 不是"这东西建立在什么之上"）。
>
> **区别只有一条**：SampleCore **做不了工业级模拟、建模等专业内容，
> 游戏性能比专业版差**。课程体系、引擎概念、组件写法都是完整的 ——
> 差的是**能扛多重的活**。
>
> ⚠ 进度分开记录（`.示例进度.json`），同时装两个不会互相干扰。
> ⚠ **不要在这个目录改公共代码** —— 下次生成会被覆盖。
>   要改请改 `引擎/%(源名)s/`，然后
>   `python _build/make_samplecore.py --go --force`。

---

""" % {"源名": os.path.basename(LC)}

#: ⚠⚠ **「已知问题」必须写在生成器里，不能手改 SampleCore/README.md** ——
#:   那个 README 是**生成物**（本文件 `头注 + 源头 README`，每次生成重写一遍）。
#:   手改它下次生成就无声消失 —— 这正是"不要在这个目录改东西"那条规矩。
#:   ⇒ 追加在**末尾**，不动源头 README 一个字（保留源头事实，见上面那段教训）。
#:   ⚠ 里面每个数字都是**实测**的，不是抄来的（判据见各自那一行）。
尾注 = """

---

## 已知问题（SampleCore 发行物）

> 这一节由生成器追加（`_build/make_samplecore.py` 的 `尾注`）。
> **不要手改本文件** —— 下次生成会重写；要改请改生成器。
> 每条都标了**严重程度**和**实测依据**。

### 1. `csharp/*/bin` 里的预构建 dll 与当前源码不一致 —— 严重程度：低（可自愈）

实测：仓库里的 `csharp/*/bin/Debug/net8.0/HybridEngineLight.dll` 是
**167936 B**，而按当前源码重新构建出来是 **167424 B**
⇒ 随发行物带的那份**不是当前源码的产物**。

**但它是可自愈的，学员不会卡住**，三条独立依据：

1. `hybridengine_light/learnmodel.py:468-480` 有兜底：先用
   `dotnet run --no-build`（因为 `bin/` 在）；**一旦非 0 就退回普通构建**重来一遍。
   所以"预构建不匹配"最多多花一次编译时间，不会变成假错误。
2. 实测（本次复核）：把 **7 个 `bin/` 目录整个移走** + `NUGET_PACKAGES`
   指向**空目录** ⇒ 四条编译型轨道（csharp / cpp / java / c）
   **退出码全是 0**，总耗时 **7.9 秒**（首次重建）。
3. 8 个 `.csproj` 里 `PackageReference` **全是 0 个**（纯框架引用）
   ⇒ 还原**不需要联网**。

⚠ 没验的：**没有真正切断网卡**。上面的"离线"是用
「空 `NUGET_PACKAGES` + 0 个 `PackageReference`」**近似**的，
不是物理断网。真断网只影响 `dotnet` 首次运行的那点自带资源。

⇒ **不要为了这个数字把 `bin/` 砍掉** —— `bin/` 是 `--no-build` 的依托，
砍了反而让每次运行都要重新编译。

### 2. `bin/*.dll` 里能搜到绝对路径 —— 严重程度：无（不是泄漏）

实测：13 个 `csharp/*/bin/**/*.dll` 里各能搜到 1 处 `D:\\Users\\etgya\\...`。

**那是 .NET 编译器的正常行为**（PDB 路径嵌在程序集元数据里），
**不是**把开发者目录暴露出去的泄漏，也**不是你机器上的问题**。

⚠ 对照组才说明问题：真正该排的是 `obj/` —— 实测（本次复核，
   源头 `csharp/**/obj/`）：**27 个文件 / 284 处**开发者绝对路径，
   其中 7 个 `.FileListAbsolute.txt` 一个后缀就占 **264 处**（单文件最多 57 处）。
   `obj/` 已经在黑名单里，`bin/` 不在。
**别为了这几个数字把 `bin/` 砍了**（见上一条）。

### 3. 生成完**跑过测试**之后，要**再生成一次** —— 严重程度：中（否则发行物不干净）

实测：`python _build/run_tests.py` **会往发行物里写东西**，两类：

| 产物 | 实测 |
|---|---|
| `__pycache__/` + `*.pyc` | 10 个目录 / **123 个 `.pyc`** |
| `*-answers/` 答题模板 | **5 个文件**（ai / embedded / engine / raspi / lessons 各一个） |

来源是 `learnmodel.py` 的「确保模板」——**第一次打开课程**时就地建答题区。
它们**不是**生成器漏排（刚生成完实测都是 0），是**生成之后新造出来的**。

⇒ **正确顺序是「生成 → 跑测试 → 再生成一次」**，
   最后一步用生成器收口（生成器末尾会按黑名单清一遍）。

⚠ **`PYTHONDONTWRITEBYTECODE=1` 不能代替"再生成一次"**（实测）：
   它只挡得住 `.pyc`（跑完测试 `.pyc` 仍是 **0**），
   **挡不住** `*-answers/`（文件数照样 669 → **674**）。
   想只跑一次生成，就得在**生成之前**把测试跑完。

⚠ 顺带一句：`*-answers/` 是**学生答题区**，源头 `.gitignore:16-17` 明确
   "不应随仓发布，否则新用户会以为里面已有答案" —— 发行物更不该带。
"""


if __name__ == "__main__":
    sys.exit(主())
