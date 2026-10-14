# 审计：引擎 `FixedUpdate` 的真实语义（2026-10-14）

> 来源：`neural-pro` 在 `task-7` 报告里留了一条
> 「**`--engine 2` 实测引擎每帧调 `onFixedUpdate` 6 次不是 1 次**」。
> 它当时用 dt=0.1/0.05/0.02 做**假设检验**得出 6/3/1.2，
> 并警告"把积分写进去会**错 6 倍**"。
>
> ⇒ 我（Lead）去读了源码，**独立核实**。这一份是结果。

---

## 一、✅ 它的观察**成立**，而且机制很清楚

```cpp
include/hybridengine/core/lifecycle_driver.hpp:39   double fixedAccum_ = 0;
include/hybridengine/core/lifecycle_driver.hpp:40   double fixedStep_ = 1.0 / 60.0;
src/core/lifecycle_driver.cpp:33                    fixedAccum_ += dt;
src/core/lifecycle_driver.cpp:35                    while (fixedAccum_ >= fixedStep_ && steps < 8) {
src/core/lifecycle_driver.cpp:36                        fixedAccum_ -= fixedStep_;
src/core/lifecycle_driver.cpp:39                    if (fixedAccum_ > fixedStep_ * 8) fixedAccum_ = 0;
src/core/lifecycle_driver.cpp:47                    Trace("FixedUpdate"); c->FixedUpdate(fixedStep_);
```

⇒ **标准固定步长累加器**（Unity 同款思路）：
```
固定步长 = 1/60 秒
每帧跑 0..8 次（steps < 8 是防"死亡螺旋"的上限）
传给回调的是 **fixedStep_**（= 1/60），**不是帧的 dt**
```

**验算它的数**：`dt = 0.1` ⇒ `0.1 ÷ (1/60) = 6` ✅ **正好 6 次**。
`dt = 0.05` ⇒ 3 次 ✅ · `dt = 0.02` ⇒ 1.2 ⇒ 1 次 ✅
⇒ **它那组 6/3/1.2 的假设检验是对的。**

---

## 二、⚠ 所以"6 次"**不是 bug** —— 真缺口是**文档**

| | |
|---|---|
| 行为 | ✅ **正确**（固定步长累加器本就该这样） |
| ⚠ 公开头里的说明 | ❌ **一个字都没有** |

`include/hybridengine/bind/ms_bind.h` 里只有：

```c
ms_cb_dt    onFixedUpdate;
```

⇒ **没有说**：每帧可能 0..N 次 · N 的上限是 8 · 传进来的是固定步长不是帧 dt。
⇒ 而**这正是队友踩到的**：他把积分写进 `onFixedUpdate` ⇒ **错 6 倍**，
  而他**自己用实验查出来了**（那很好）—— 但**下一个用户还会再踩一次**。

---

## 三、⚠ 三处**没有公开入口**的东西（潜在的"能改但改不了"）

```
fixedStep_ = 1/60        ← 硬编码在 hpp 的成员初始化里
steps < 8                ← 硬编码在 cpp 的循环里
fixedAccum_ > fixedStep_*8 ⇒ 归零  ← 硬编码（防螺旋）
```

⇒ 我在 `ms_bind.h` 里 grep `fixed|Fixed` —— **只有 `onFixedUpdate` 和一段注释**，
  **没有任何 setter**。
⇒ 也就是说：**用户改不了固定步长**，只能接受 1/60。
  ⚠ 而 Unity 里 `Time.fixedDeltaTime` 是**可配的**。
  ⇒ 这是**产品决定**（要不要给入口），不是 bug —— 但**该记下来**。

---

## 四、⚠ 我**没有**去改那些文件 —— 说明理由

`引擎/HybridEngine` 当时有 **210 项别人的在途改动**。
⇒ 我**不动**它，理由三条：
1. 我的结论是"**文档缺失**"，**加注释**能治 —— 但那要碰公开头，
   而这个头**可能正被别人改**（在途 210 项里就有 `README.md` / `docs/` 等）
2. **行为本身是对的** ⇒ **没有 bug 要修**，只有说明要补
3. 本仓的规矩：**不在别人在途的时候动同一批文件**（`git add -A` 的教训）

⇒ 所以这一份**只落审计**，**改法写在这里**，等文件干净了再动：

```c
//: ⚠⚠ **每帧可能调用 0..N 次**（固定步长累加器，不是"每帧一次"）。
//:     · 固定步长 = 1/60 秒（**当前没有公开入口可改**）
//:     · 单帧上限 8 次（防死亡螺旋）；累加超过 8 步则归零
//:     · `dt` 是**固定步长**（1/60），**不是**本帧的帧时长
//:   ⇒ 把"每帧一次"的积分写进来会**按倍数放大**
//:     （实测：dt=0.1 时调 6 次 ⇒ 积分错 6 倍）
//:   ⇒ 需要"每帧一次"的逻辑请放 `onUpdate`。
ms_cb_dt    onFixedUpdate;
```

---

## 五、⚠ 另一件顺手查到的事：**帧钩子每帧调两次**

```cpp
engine.cpp:734   if (frameHook_) frameHook_(frameHookUserData_, dt, 0);   // 帧首
engine.cpp:757   if (frameHook_) frameHook_(frameHookUserData_, dt, 1);   // 帧尾
```

⇒ `ms_cb_frame` 有 **phase 参数**（0=帧首 / 1=帧尾），
  `ms_bind.h:315` 的注释**说明了 phase=1 的时机**（Lifecycle 全序之后、Events Flush 之前）。
  ⇒ 这一条**文档是有的** ✅ —— 对比之下，`onFixedUpdate` 那条**明显是漏写**。

⇒ **同一个头里，一条写了、一条没写** —— 这更像"漏"而不是"设计如此"。

---

## 六、⭐ Round 5 补：**用真程序实测确认了**（不再只是源码级验算）

`neural-pro` 的 `neural_brain.cpp` 有 `--engine 0|1|2` 三个模式，正好能验这件事。
（⚠ 它的 exe 我重新链过 —— 见第七节：**我上次链错了库**。）

```
--engine 0   MEAN_RATE_ALL=48.4462198
--engine 1   MEAN_RATE_ALL=48.4462198   HOOK_PHASE0_CALLS=40  HOOK_PHASE1_CALLS=40
                                        COMPONENT_FIXEDUPDATE_CALLS=**0**
--engine 2   MEAN_RATE_ALL=48.4462198   HOOK_PHASE0_CALLS=40  HOOK_PHASE1_CALLS=40
                                        COMPONENT_FIXEDUPDATE_CALLS=**120000**
```
参数：`--pops 500 --steps 40 --dt 0.1`

**验算**：
```
120000 ÷ (500 群体 × 40 步) = **恰好 6.0 次/步**
源码验算：dt=0.1，fixedStep=1/60 ⇒ 0.1 ÷ (1/60) = **6**   ✅ **完全对上**
帧钩子：phase0 = 40 次 · phase1 = 40 次（= 步数）          ✅ 对上 `engine.cpp:734/757`
```
⇒ **"每帧多次"这条从"源码推算"变成了"实测"**，而且次数精确一致。

---

## 七、⚠⚠ 但实测露出一个**语义差异**（**别当成 bug**）

```
engine 0（纯循环）     每步积分 **1 次**
engine 2（引擎驱动）   每步 FixedUpdate **6 次**
```
⇒ ⚠ **同一个模型的"每步推进量"差 6 倍** —— 如果模型有**真实时间动力学**，
  两种模式的**物理时间就不一样**（engine 2 推进得快 6 倍）。

⚠ 而这份实测里 **`MEAN_RATE_ALL` 在三种模式下完全相同（48.4462198）** ——
  ⚠ **不要因此以为"没关系"**。真因是：**该模型在此配置下收敛到了同一个稳态**
  （rate 模型的平均放电率是**不动点**，与"走了几步"无关）。
  ⇒ ⭐ **它对齐，是因为模型没有时间依赖，不是因为"每步多次"无害。**
  ⚠ 这个区分很重要：**下一个把带时间动力学的模型接进来的人，会踩到 6 倍。**

⇒ **结论**：把模型接到引擎上时，**必须显式选择**：
  · 用 `onUpdate`（每帧 1 次，帧时长 = `dt`）—— 适合"跟着帧走"的
  · 用 `onFixedUpdate`（每帧 0..8 次，步长 = 固定 1/60）—— 适合"物理时间必须均匀"的
  ⚠ **而这两者的时间尺度不同，选错就差 6 倍。**

---

## 八、⚠ 我上一轮的错：**链错了库**（已纠正）

我在 Round 20 里给 `neural_brain.cpp` 加了平台守卫，然后链接时报：
```
undefined reference to `ms_save_set_path' / `ms_save_set_float' / `ms_bind_error_text'
```
⇒ 我当时以为"引擎的 `build/release` 是过期的（09/24）"，**那说法只对了一半**。

**真相（Round 5 实测）**：
```
build/release        hybridengine.dll  09-24 16:58    ← **过期的那个**（我引的就是它）
build-ninja-release  hybridengine.dll  **10-10 22:28**  ← **比最新源码(10-10 20:11)还新** ✅
CMakePresets 里权威预设 = **`release-ninja`**（`scripts\run_tests.ps1 -Preset release-ninja`）
```
⇒ 用 `release-ninja` 的导入库重链 ⇒ **退出码 0 · 链接成功 148,126 B · 三个缺的符号全有了。**
⇒ ⚠ **所以"代码有问题"这个结论是错的** —— 真因是**我引了错的构建目录**。
  ⚠ 又一次「**报错指向的地方不是出问题的地方**」。

**而权威门禁实测通过**：
```
[run_tests.ps1] MATRIX **942/942 pass, 0 fail** (preset=release-ninja)   退出码 0
render3d 50/50 · plugin 4/4 · app 19/19 · python 249/249 · csharp 204/204
```
⇒ ⭐ **六条线（`task-14`~`task-19`）的地基是稳的。**

⚠ **没验到的**：`nm` 读不到那个 dll 的导出表（可能是 clang/MSVC 产物，不是 MinGW 的），
  所以我是**用"能不能链接 + 能不能跑"**来判定的，**不是**直接读导出表。
