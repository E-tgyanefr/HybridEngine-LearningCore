# ⚠⚠ `HybridEngine` 仓：**79 个提交没推上去**，三条原因叠加（2026-10-14 · Round 28）

> ⚠ 这一份是**查清一件影响所有人的事**的记录。
> 我之前几轮把"推不上去"当成"等作者开护栏"，**那个理解是错的**。

---

## 一、事实

```
HEAD          666cc55（2026-10-14）
origin/main   faf6df4（**2026-09-11**）
git rev-list --left-right --count origin/main...HEAD  ⇒ **0 落后 / 79 领先**
⇒ ⭐ **79 个提交、整整一个月的成果，全在本地。**
```

⚠ 而 `origin/main` 上**已经有 9 处个人信息命中**：
```
docs/零基础教程.md:24 · :215
dotnet/Milestone.Game/docs/m4-final-acceptance.md:5
dotnet/Milestone.Game/docs/parity-1to1-verification.md:4 · :5
tools/mmd_stage/iris_anim.h:1 · iris_main.cpp:4 · lumine_main.cpp:4 · mmd_meshes.h:1
（模式：`[A-Za-z]:\Users\<名>`，即 `scripts/leak_guard.ps1:50` 那条）
```
⇒ ⭐ **说明那个仓【推到公开 GitHub 过】，而守门是之后才装的** ——
  也就是说 `leak_guard` 拦的是**真事**（已经发生过的外泄）。

---

## 二、⚠⚠ 三条原因叠加 ⇒ 事实上推不上去

```
① `remote.origin.pushurl = DISABLED-do-not-push-plaintext://github.com/...`
   —— 刻意禁用（**意图**是"改推内网"，不是"不许推"）
② 内网目标**都不可达**（都探过）：
      git.local:2223        ⇒ 端口不通 · **DNS 解析失败**
      raspberrypi.local:3001 ⇒ 端口不通 · **DNS 解析失败**
      git.local:443          ⇒ 端口不通
③ `core.hooksPath = .githooks` ⇒ `pre-push` → `scripts/leak_guard.ps1`
   **实跑：扫描 536 个文件 · 错误 15 / 警告 0 · 退出码 1** ⇒ **拒绝任何 push**
⇒ ⭐ **这不是"谁忘了开护栏"，是三种原因共同造成的死锁。**
```

⚠ 顺带：`pwsh` **不在 PATH**（这个会话本身跑在 pwsh 里，但没装到 PATH）⇒
  要跑那支守卫得用 `powershell.exe`。**而这是个关键的可用性细节** —— 我第一次跑失败了。

---

## 三、⭐ 15 处的归属（逐个核"在不在 HEAD 里"）

```
✅ **HEAD 本身现在是干净的。**
   原来有 **3 处在 HEAD 里 —— 全都是我写的**：
      tools/神经模拟报告.md:38   （g++ 路径：C:\Users\etgya\AppData\…）
      tools/神经模拟报告.md:62   （$mingw = "C:\Users\etgya\…\mingw64\bin"）
      tools/神经模拟报告.md:662  （同上，文里出现两次）
   ⇒ **已修并提交（666cc55）**：改成 `%LOCALAPPDATA%` / `$env:LOCALAPPDATA` 写法
     ⇒ `leak_guard`：**错误 18 → 15**，我那 3 处**不再命中** ✅

⚠ 剩下 15 处**全都不在 HEAD 里**：
   · **14 处：完全未跟踪**（不在 git 里，是**工作区里没提交的文件**）：
       docs/GPU阴影可行性.md:728 · docs/并行测试真伪.md:346/425/436/438/439/440
       docs/控制协议.md:408 · docs/收尾1_贪吃蛇完整版.md:64 · docs/收尾2_同步展示仓.md:104
       docs/阴影oracle开关.md:334 · tools/verify_camera.py:24
       tools/verify_snake_persist.py:11/15
   · **1 处：已跟踪但工作区新增**：`docs/构建指南.md:108`
     ⚠ HEAD 那版第 108 行是 `> ```powershell` —— **不命中**
     ⇒ ⭐ 所以是**工作区改动**引入了它，不是历史遗留
```

⚠ 而我 Round 27 曾说"这一条两次核的结论不同" —— **这一轮查清了**：
```
git cat-file -e HEAD:docs/构建指南.md  ⇒ 退出码 0（**文件在 HEAD 里**）
而它的**工作区版本**第 108 行有个人路径
⇒ ⭐ **"文件在 HEAD 里" 和 "工作区那一行在不在 HEAD 里" 是两件事** —— 我上一轮把它们混了
```

---

## 四、⚠ 修法（**保持可复现，但不含个人路径**）

```
❌ 不要删文件（那是工作成果）—— 只改路径那一处
✅ 文档里：
     C:\Users\<名>\AppData\Local\...        ⇒  %LOCALAPPDATA%\...
     D:\Users\<名>\Desktop\Project\...      ⇒  用相对路径，或 <项目根>/...
✅ 脚本里（Python 例）：
     HE = r"D:\Users\<名>\Desktop\Project\HybridEngine"
   ⇒ HE = os.environ.get("HE_ROOT", os.path.join(os.path.expanduser("~"), "Desktop", "Project", "HybridEngine"))
✅ 脚本里（PowerShell 例）：
     $mingw = "C:\Users\<名>\AppData\Local\...\mingw64\bin"
   ⇒ $mingw = Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages\BrechtSanders.*\mingw64\bin"
     ⚠ 通配符能展开 ⇒ **别人照样能照抄这条命令**
✅ 自查：`powershell -NoProfile -ExecutionPolicy Bypass -File scripts\leak_guard.ps1` ⇒ **退出码 0**
```

⚠⚠ **而我踩了一个自造的坑**：我第一版写了个占位符 `X:\Users\<名>` 来说明那个模式，
**而那个占位符自己命中了守门模式** ⇒ 错误数 16 → 仍是 16，改了两次才消掉。
⇒ ⭐ **"说明这个模式"的那句话本身触发了这个模式。**

---

## 五、⚠⚠ 这件事解释了一个我一直没想通的问题

```
我 Round 23 给 `src/ai/CMakeLists.txt` 加了 `Threads::Threads`（POSIX 上链线程库），
⚠ 而**验它的 CI（`HybridEngine/.github/workflows/ci.yml` 的 `cmake --preset unix`）从来没跑过**
   —— ⭐ **因为这个仓 79 个提交都推不上去。**
⇒ 而 `LearningCore` 那个仓**能推**（github 通）⇒ **它的 CI 绿了三轮**
   ⚠ 但它只跑 `build_hblight.py --go` ⇒ **只建 `libhblight`**，且那个仓**没有 `src/`**
⇒ ⭐ 所以「`hybridengine_ai` 在 Linux 上能不能构建」这件事，
   **在 79 个提交推上去之前，谁都验不了。**
```

---

## 六、⚠ 我**没有**做的（逐条）

```
❌ 没为了让闸门通过而改别人的文件（那是他们的工作区 / 未提交成果）
❌ **没绕过 pre-push**（`git push --no-verify`）—— 那道闸门挡的是**真事**
   （`origin/main` 上已经有 9 处个人信息就是证据），而 `origin` 的 pushurl 本来就被刻意禁用
   ⇒ **绕它等于同时绕两道，我不做**
❌ 没推（推了也会被拒）
❌ **没查**"那 14 处未跟踪文件是谁的"（我只知道不是我的）——
   ⚠ 而它们**一旦被提交，会让 HEAD 也变脏** ⇒ 那才是真正的风险
```

---

## 七、⇒ 需要决定的三条路（**我不自己选**）

```
甲) 先修那 14 处 ⇒ 然后**仍需**解决 ①pushurl ②内网可达
乙) 先让 `gitea` / `pi` 可达 ⇒ 但仍要过 pre-push（守门不看推去哪）
丙) 接受"这个仓长期只在本地" ⇒ ⚠ 那它的 CI **永远不会跑**，
    所有"只在 Windows/MinGW 验证过"的结论**永远不会被跨平台检验**
⇒ ⭐ 而无论选哪条：**那 14 处未跟踪文件里的个人路径，早晚要修** ——
   因为**一旦它们被提交，HEAD 就从"干净"变"脏"**。
```
