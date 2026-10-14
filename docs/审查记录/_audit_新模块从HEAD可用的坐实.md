# ⭐ 坐实：两个新模块**从 HEAD 真的能用**（2026-10-14 · Round 19）

> 背景：`neural-pro` 与 `verify-and-optimize` 都如实交代「**我的 CMake 验证跑的是工作区那份
> （含别人的在途改动）⇒ "接线可用"对工作区成立、对 HEAD 不成立**」。
> ⇒ 我在 Round 18 提交了那 6 行接线（`add_subdirectory(src/numeric)` + `(src/ai)`）。
> **这一份是"入库之后真的能用"的完整证据链** —— 全部从 **HEAD** 取源码，不碰工作区。
> ⚠ **取证方法本身踩了两次"仪器坏了"**，一并记在第四节。

---

## 一、证据链（六步，全部从 HEAD）

```
① 从 HEAD 抽源码：`git archive HEAD -o src.tar` + **Python 的 tarfile** 解包（⚠ 见第四节）
② cmake configure（HEAD 那份 CMakeLists，**不带**工作区那 4 行）⇒ **退出码 0**
③ ninja hybridengine_ai hybridengine_numeric ⇒ **退出码 0**
      src\ai\libhybridengine_ai.a            **1,057,062 B**
      src\numeric\libhybridengine_numeric.a    **395,718 B**
④ tools/ai_train 从 HEAD 编出并跑 ⇒ **退出码 0**
      max_rel_err = **1.843e-07**   （与我在 Round 12 的独立复核一致）
⑤ tools/heat1d 从 HEAD 编出并跑 ⇒ **退出码 0**
      **MAX_REL_ERR=8.770865e-07** · **TOTAL_FAILURES=0**（与 `neural-pro` 报的逐位相同）
⑥ ninja all（全量）⇒ **退出码 0 · 125/125**
      ⇒ ⭐ 顺带证明：**HEAD 的 `tools/` 构建是自洽的**
        （它建出了 `HybridEngineEditor.exe` / `HybridEngine.exe` / `rr_micro.exe` ——
         那是 **HEAD 那份 `tools/CMakeLists.txt` 里的九个 target**，
         与工作区正在被重写的那份无关）
```

### ⭐ 而"目标存在"这件事有**正反两面**证据

```
【正面】HEAD（含那 6 行）⇒ ninja -t targets ⇒ `hybridengine_ai` / `hybridengine_numeric` 都在
【反面】把 HEAD 版里那两行**去掉**再 configure ⇒ **退出码 0，但两个目标不存在**
⇒ ⭐ **反面成立**：这说明 **"configure 成功" ≠ "模块参与构建"** ——
   而**这正是我 Round 18 修的那个问题**（在那之前，`numeric`/`ai` 的源码进了 git，
   却**没有任何构建目标引用它们**）
```

---

## 二、⚠ 门禁：`ms_test_editor` 1 个失败 —— **判定为环境缺产物，不是接线**

```
从 HEAD 全量构建后跑 ctest：**86% passed, 1 failed out of 7**
   ms_test_core ✅ · ms_test_platform ✅ · ms_test_bind ✅
   **ms_test_editor ❌** · ms_test_render3d ✅ · ms_test_plugin ✅ · ms_test_app ✅
单跑 ms_test_editor：**155 cases, 154 pass, 1 fail**
```

**失败的那一条**：
```
[python] 启动 Python 失败：...Python312\python.exe（**CreateProcessW failed (GetLastError=87)**）
CHECK_TRUE failed: std::filesystem::exists(root + "/Library/ScriptAssemblies")
[FAIL] P3 编辑器：应用内写脚本端到端（打开→绘制→中文输入→Ctrl+S）
```

**判定：环境缺产物** —— 三条证据：
```
① `Library/ScriptAssemblies` 是 **`ms_dotnet` 编译 C# 项目脚本**时生成的。
   `scripts\release.ps1:132` 的注释原话：
     「断言 `<项目根>/Library/ScriptAssemblies` 存在（`test_editor.cpp:3935`，由 `ms_dotnet` …）」
   ⚠ **它不是 CMake/ninja 的产物**（全仓搜 ScriptAssemblies：只在 `.ps1`/`.py`/测试里，
      **CMake 侧零命中**）⇒ **`ninja all` 不会产生它**
② 我的 HEAD 验证环境里 **ScriptAssemblies = 0 个** ⇒ 缺产物 ⇒ 该用例红
③ 队友的权威构建（`build-ninja-release`，那次 **942/942**）⇒ 单跑 **155/155 pass**
   —— 那个目录树里有历史生成的产物

⇒ ⭐ **我改的是两行 `add_subdirectory`** —— 它**不可能影响**
   "C# 项目脚本编译" 或 "启动 Python 子进程"（另两条报错同属环境）
```

⚠ **但我如实说清**：我没有去**把那个产物补出来再跑一遍**（那要 `dotnet` 全流程，几分钟级）
⇒ 所以这条是「**证据链判定**」，**不是"我补上产物后 155/155"的直接实测**。

---

## 三、⇒ 结论

```
✅ `hybridengine_ai` / `hybridengine_numeric` **在 HEAD 上会构建**（正面 + 反面都验过）
✅ 两个工具（`ai_train` / `heat1d`）**从 HEAD 编出、跑出与作者一致的数**
✅ HEAD 全量构建 **125/125 退出码 0**
✅ 门禁 6/7 套件绿；唯一的失败**有完整证据指向"环境缺 .NET 产物"**，与接线无关
⚠ 缺一步：没补产物复跑那一条
```

---

## 四、⚠⚠ 取证过程本身踩了**两次"仪器坏了"**（都值得记）

### ① Windows 的 `tar` 处理不了中文文件名 —— 而它**静默跳过**

```
`tar -xf src.tar` ⇒ `docs/AI上下文.md: Invalid empty pathname` × 13 个条目
⇒ **跳过了 docs/ 里 13 个中文名文件**，而 `src/` 也被跳掉一部分
⇒ 于是 `cmake configure` 红在 **91~96 行**（`add_subdirectory(src/core/platform/app/bind/render3d/plugin)`）
⇒ ⚠⚠ **看起来完全像"我的接线把 HEAD 弄红了"** —— 我**差点照那个结论去回退**
✅ 换 **Python 的 `tarfile`**（按 UTF-8 处理路径名）⇒ **configure 立刻 exit 0**
```
> ⭐ **又是「报错指向的地方不是真因」。** 而这次我**先分了"是我的错还是仪器坏了"**，所以没白回退。

### ② `ctest` 第一次报 **0% passed, 7/7 failed · Total Test time = 0.01 sec**

```
⚠ 0.01 秒 = **根本没跑起来** —— 因为**我当时只建了两个库**，测试可执行文件还不存在
⇒ ⚠ **"7/7 全红"看起来像灾难**，其实是我**没建完**就跑了
```
> ⭐ 判据：**先看"耗时"合不合理**（0.01 秒跑 7 个套件是不可能的）。

### ⭐ 合起来：这一轮我**没有**在"第一次红"时就下结论 —— 两次都先分了"谁的错"

```
第 1 次（tar/中文名）   ⇒ 判定"仪器坏了" ⇒ 换 tarfile ⇒ 绿
第 2 次（0.01 秒 7/7 红）⇒ 判定"我没建完" ⇒ 建完 ⇒ 6/7 绿
第 3 次（ms_test_editor）⇒ 判定"环境缺 .NET 产物" ⇒ 用证据链 + 权威构建对照 ⇒ 与接线无关
⇒ ⚠ 而**这三次如果任何一个我按字面报出去**，都会变成一条错结论
```

---

## 五、⚠ 没做的（逐条）

1. **补 `Library/ScriptAssemblies` 后复跑 `ms_test_editor`** —— 没做（要 dotnet 全流程）
2. **`tools/CMakeLists.txt` 的接线**（`heat1d` / `ai_train` / `bench_gpu3` 三个目标）——
   ⚠ **仍未提交**（那个文件有**整文件重写**的在途改动）⇒ 那三个工具**在 HEAD 上不会被构建**
   ⭐ 而我验的④⑤是**手动 g++ 编的**，不是 `cmake --build --target heat1d`
3. **`HybridEngine` 仓推不上去** —— `origin` 的 push 被刻意禁用 ⇒
   ⚠ **我这一轮的提交（`a103578`）只在本地**
4. **`src/collision`/`animation`/`render2d`/`uikit` 的接线** —— 只在工作区，
   它们的源码**不在 git 里** ⇒ 提上去 HEAD 会红（我 Round 18 已记）
