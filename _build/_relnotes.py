# 更新四个 Release 的**标题与说明**（纯元数据，不动资产）。
#
# ⚠ 顺序很重要：**先改文字、后动 tag**。
#   改文字不会影响资产；而删 tag 会连带删 release 与资产。
#   所以先把"能安全做的事"做完，再权衡要不要冒险删。
import json
import os
import subprocess
import sys
import urllib.error
import urllib.request

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
API = "https://api.github.com/repos/E-tgyanefr/HybridEngine-light"
out = subprocess.run(["git", "credential", "fill"],
                     input=b"protocol=https\nhost=github.com\n\n",
                     capture_output=True).stdout.decode("utf-8", "replace")
tok = next((l[9:].strip() for l in out.splitlines() if l.startswith("password=")), "")
if not tok:
    print("  XX 拿不到 token"); sys.exit(1)
H = {"Authorization": f"token {tok}", "User-Agent": "dsh", "Accept": "application/vnd.github+json"}

公共 = (
    "## 怎么用\n\n"
    "解压后双击 `HybridEngine-light.exe`。**不需要装 Python。**\n\n"
    "```\n"
    "HybridEngine-light.exe              有哪些课\n"
    "HybridEngine-light.exe 01           看第 1 课\n"
    "HybridEngine-light.exe 编辑 01      写代码（内置编辑器）\n"
    "HybridEngine-light.exe 检查 01      检查答案（首次生成答题模板）\n"
    "HybridEngine-light.exe 答案 01      参考答案\n"
    "HybridEngine-light.exe 游戏 / 嵌入式 / 树莓派 / AI / 考试\n"
    "```\n\n"
    "**开箱即用**：Python 基础 · 小游戏 · 嵌入式/单片机模拟器 · 树莓派模拟器 · "
    "人工智能入门 · 考试系统\n\n"
    "**需要自己装工具链**：C# / C++ / Java / C —— 要 .NET SDK + "
    "`g++` / `javac` / `gcc`（课程要把你写的代码**真的编译运行**才能判对错）。\n\n"
)

版本说明 = {
    "v1.0.0": (
        "HybridEngine-light v0.1.0（Windows 开箱即用）",
        "## 第一个可用版本\n\n"
        "改名 LearnCode → HybridEngine-light 完成（含包名与命令）。\n\n"
        "**开箱即用**：Python 基础 · 小游戏 · 嵌入式/单片机模拟器 · 树莓派模拟器 · "
        "人工智能入门 · 考试系统\n\n"
        "**需要工具链**：C# / C++ / Java（要 .NET SDK + 对应编译器）\n\n"
        "### ⚠ 已知缺陷（已在 v0.1.1 修复）\n\n"
        "**C# / C++ / Java 三门的课程程序编译不过** ——\n"
        "改名时把 C# 命名空间写成了 `HybridEngine-light`，而 C# 标识符**不允许连字符**。\n"
        "（Python 那几门不受影响，所以当时没暴露。）\n\n"
        "> 版本号说明：本 Release 原以 `v1.0.0` 发布。`1.0.0` 暗示「已稳定发布」，"
        "与项目实际状态不符（引擎专业版也才 0.5.0）—— 现按**轻量版自己的 0.1.x 线**"
        "改称 `v0.1.0`。轻量版**独立于引擎版本**，两者发布节奏不同。\n\n" + 公共
    ),
    "v1.0.1": (
        "HybridEngine-light v0.1.1（含内置编辑器）",
        "## 修了 v0.1.0 的缺陷 + 新增内置代码编辑器\n\n"
        "### 🔴 修复：C# / C++ / Java 课程编译不过\n\n"
        "改名把 C# 命名空间写成了非法的 `HybridEngine-light`（C# 标识符不允许连字符）。\n"
        "漏网原因：替换规则 `LearnCode.` → `HybridEngineLight.` 处理了带点号的形式，\n"
        "但**裸 `LearnCode`**（在 `namespace`/`using` 里没有点号）被替换成了带连字符的展示名。\n\n"
        "修了 36 个 `.cs` + 7 个 `.csproj`，其中三处是功能性的：\n"
        "`ProjectReference` 指向**不存在的目录**、`<RootNamespace>` 非法标识符、`<AssemblyName>` 同上。\n\n"
        "> ⚠ 这个回归**只在有 dotnet 的机器上才暴露** —— 当时只跑了 Python 的 184 个用例。\n"
        "> **「改了名字」与「名字在每种语言里都合法」是两件事。**\n\n"
        "验证：4 个工程从零编译全过 · C++ 课程 8 课正常 · C# 测试 74/74。\n\n"
        "### ✨ 新增：内置代码编辑器\n\n"
        "学员不用再另开记事本 —— 直接在终端里写：\n\n"
        "```\n"
        "HybridEngine-light.exe 编辑 01     写代码（内置编辑器）\n"
        "HybridEngine-light.exe 检查 01     检查答案；首次会建模板并问要不要直接打开\n"
        "```\n\n"
        "快捷键：光标键 / Home / End / PgUp / PgDn / Enter（**自动保持缩进**）/\n"
        "Tab（4 空格）/ Backspace / Delete / `Ctrl+S` 保存 / `Ctrl+Z` 撤销 /\n"
        "`Ctrl+C` 保存退出 / `Esc` 退出（**有未保存改动要按两次**）/ `F5` 保存并运行\n\n"
        "纯标准库实现（不用 `curses` —— Windows 标准库不带它，而本项目零第三方依赖）。\n\n"
        "判据 19 条，**跑出两个真 bug**：换行时把缩进也吃掉了、上下移动没做列夹取。\n\n"
        "### 验证\n\n"
        "Python 侧 203 个用例全过（含 19 条编辑器判据）。\n\n"
        "> 版本号说明：本 Release 原以 `v1.0.1` 发布，现按轻量版自己的 0.1.x 线改称 `v0.1.1`。\n\n" + 公共
    ),
    "v1.0.2": (
        "HybridEngine-light v0.1.2（新增 C 语言课程）",
        "## 新增 C 语言课（8 课）\n\n"
        "与 C++/Java 并列的第四条编译型课程线：\n"
        "输出 · 变量与计算 · 条件判断 · 循环 · 函数 · 数组 · 字符串 · 指针\n\n"
        "```\n"
        "dotnet run --project csharp/c-lessons            # 列课程\n"
        "dotnet run --project csharp/c-lessons 检查 01     # 检查答案\n"
        "```\n"
        "答案文件在 `c-answers/`。⚠ 需要 .NET SDK + `gcc`。\n\n"
        "### ⚠ 关键设计：**不复制运行器**\n\n"
        "C 与 C++ 用同一个编译器，差别只有 `-std=c11`/`-std=c++17`、`.c`/`.cpp`、"
        "`printf`/`std::cout` ⇒ 加一个 `CppRunner.用C` 开关让既有运行器**语言感知**。\n"
        "复制 360 行只为了改这几个字，会让以后的 bug 要修两处 —— "
        "而那份代码里每个坑都是实测撞出来的。\n\n"
        "### ⚠⚠ 加课时抓到 4 个 bug（3 个是**课程自己的错**）\n\n"
        "验证方式：**用「参考答案」跑全部 8 课的检查**。\n"
        "为什么必须这么做：课程里的检查是我写的、参考答案也是我写的 ——\n"
        "**两边都错时检查会「通过」，而课程是错的。**\n\n"
        "1. `CppCheck.写了函数` 里还硬编码着 C++ 的 `std::cout`\n"
        "   ⇒ C 课生成出 `std::cout << printf(...) << \"\\n\"`，报错里出现 `cc1plus` 与 `<<`\n"
        "   —— **课程自己的 bug，学生却会以为是自己写错了**\n"
        "2. 两课把**带分号的完整语句**当「表达式」传进去 ⇒ 机制又包一层 `printf`\n"
        "3. 一课期望值算成 13 字节，实际 **10**（「你好，C」= 3+3+3+1）\n"
        "4. 验证脚本自己按标题拼文件名，与真实文件名不符 ⇒ 改用 `CCourse.答案路径()` 单一真值\n\n"
        "修完 **8/8 课用参考答案全通**（34 项检查）。\n\n"
        "> 版本号说明：本 Release 原以 `v1.0.2` 发布，现按轻量版自己的 0.1.x 线改称 `v0.1.2`。\n\n" + 公共
    ),
    "v1.0.3": (
        "HybridEngine-light v0.1.3（修：学员看到的命令名不对）",
        "## 🔴 修一个打包后才暴露的缺陷\n\n"
        "打包成 exe 后，**各课程线的提示文字仍然写着 `python -m hybridengine_light`**：\n\n"
        "```\n"
        "嵌入式：  看某一课：   python -m hybridengine_light 嵌入式 01\n"
        "```\n\n"
        "而学员机器上**没有 Python** —— 照抄就失败，\n"
        "**而新手唯一的线索就是这些提示文字**。\n\n"
        "影响 **11 处**：`aicourse.py` · `embedded.py` · `raspi_course.py` · `cmd_exam.py`\n"
        "（上一版只修了 `run.py`，当时只看了一个模块。）\n\n"
        "现在全部走**单一真值** `_paths.命令名()`；帮助标题也从包名\n"
        "`hybridengine_light 课程` 改成产品名 `HybridEngine-light 课程`。\n\n"
        "## 还修了构建脚本两处硬伤\n\n"
        "· `ROOT` **写死**了旧路径 ⇒ 重新构建直接 `FileNotFoundError`。改成从 `__file__` 推导。\n"
        "· 不能把 PyInstaller 的 `--distpath` 直接指到发布目录"
        "（`--noconfirm` 会**整个删掉**它，连带删掉同级的 zip）\n\n"
        "## 版本号\n\n"
        "`__version__ = \"0.1.3\"`，与本 tag 一致。\n"
        "**轻量版走自己的 0.1.x 线，独立于引擎版本**"
        "（引擎专业版是 0.5.0，见 `HybridEngine/CMakeLists.txt`）。\n\n"
        "> 版本号说明：本 Release 原以 `v1.0.3` 发布，现按轻量版自己的 0.1.x 线改称 `v0.1.3`。\n\n" + 公共
        + "### 验证\n\n"
        "· Python 侧 **203 个用例全过**\n"
        "· C 课 **8/8 用参考答案跑通**（34 项检查）\n"
        "· C# 测试 **74/74** · 引擎 `ctest` **11/11** · 黄金帧 `4634E387E024BE90` 未变\n"
        "· exe 在**没有 Python 的 PATH** 下实测可跑，三条课程线提示都显示 `HybridEngine-light.exe`\n"
        "· 包里**不含**任何答案残留（`lessons/answers/` 与 `*-answers/` 都为空）\n"
    ),
}

rels = json.loads(urllib.request.urlopen(
    urllib.request.Request(f"{API}/releases", headers=H), timeout=40).read())
for r in rels:
    tag = r["tag_name"]
    if tag not in 版本说明:
        print(f"  {tag}: 不在计划里，跳过")
        continue
    name, body = 版本说明[tag]
    try:
        req = urllib.request.Request(
            f"{API}/releases/{r['id']}", method="PATCH", headers=H,
            data=json.dumps({"name": name, "body": body}).encode("utf-8"))
        j = json.loads(urllib.request.urlopen(req, timeout=60).read())
        print(f"  ✓ {tag} 标题已更新：{j['name']}")
    except urllib.error.HTTPError as e:
        print(f"  XX {tag}: HTTP {e.code} {e.read().decode('utf-8','replace')[:160]}")
