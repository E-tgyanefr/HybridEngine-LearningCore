# 🎉 CI 四 job 全绿 —— 而它验掉了两条"只有 CI 能验"的事（2026-10-14 · Round 21）

> 来源：`c6380e1`（`LearningCore`，已推送）· 工作流 `cross-platform.yml`
> ⚠ 这一份的价值不在"绿了"，而在**它验掉的是两条【本机无法验】的命题**。

---

## 一、结果

```
c6380e1 / **success** —— 四个 job 全绿

Linux 打包   success
**Linux**    success   ← Build C++ core ✅ · C++ vs Python 逐位 ✅ · Prove 走 C++ ✅ · Default 回退 ✅
**macOS**    success   ← 同上四条全过
**Windows**  success   ← 同上四条全过
```

⚠ 而 `c6380e1` 的祖先里含 `c6a06de` = **队友的并行光栅化器**（4 文件 / +700）。

---

## 二、⭐⭐ 它验掉了两条**本机无法验**的命题

### ① 「POSIX 上 `std::thread` 要链 pthread」—— 队友明确说"我不猜它绿不绿"

它的原话：
> ⚠ **Linux/macOS 一次没跑过**（本机没有）。POSIX 上 `std::thread` 要链 pthread ⇒
> 我追加了 `Threads::Threads`（CMake 官方跨平台写法）；本机只能证明"MinGW 下
> `find_package(Threads)=TRUE` 且不多依赖"，**pthread 那条路只有 CI 能验**。

⇒ ✅ **`Build C++ core (libhblight)` 在 Linux 与 macOS 上都 `success`**
  ⇒ 那意味着：**`find_package(Threads)` + `Threads::Threads` 在两个 POSIX 平台上都生效了**
  ⇒ ⭐ **它那句"我不猜"是对的判断，而答案由 CI 给出了。**

### ② ⭐ 「并行之后还逐位相同吗」—— 而这条**在三个平台上都过了**

```
C++ vs Python bit-for-bit   ⇒ **Linux success · macOS success · Windows success**
```
⇒ ⭐ 队友的并行实现（**只在单个图元内部按行区间派发**；写过的像素数**各块独立计数、按块序求和**）
  在 **三个平台**上都与 Python 侧 **75/75 逐位一致**
⇒ ⭐ 也就是说：**它的"按块序求和"设计，抵消了并行带来的次序变化** ——
  而这一点**在本机只能证明 MinGW 下成立**，POSIX 上是 CI 证的。

### ③ 顺带：我接的三条判据在**三平台都在跑**

```
Prove the C++ path is actually taken        ⇒ 三平台 success（⚠ 防的是"静默退回 Python"那次假绿）
Default must fall back to Python (no crash) ⇒ 三平台 success
C++ vs Python bit-for-bit                  ⇒ 三平台 success
```
⚠ 其中 **`Prove` 那一条**是我在最初接手时**第一版写漏了 `HBLIGHT_ENABLE=1`** 的
（当时它要么误报失败、要么根本证明不了 C++ 路）—— 现在它在三平台都真的在断言。

---

## 三、⚠ 而这一轮也踩了两次"仪器/工具坏了"（都记）

```
① `git push` 报：**Failed to connect to github.com:443**（21070 ms 超时）
   ⚠ 而我第一次还把它误读成 `Everything up-to-date`
     —— 真因是我脚本取 `Select-Object -Last 1`，**取到了别的那一行**
   ⇒ 后来自愈（网络恢复）⇒ **3 个提交一次推上去**
② 我写临时脚本时 `C:\Users\...\Temp\` **报 "file no longer exists — re-read the file"**
   ⇒ 换写到工作区才成功
   ⚠ 两次都不是判据/代码的问题，是**工具层**
```

⚠ 而 ① 有一个**有用的副产品**：`gitee.com:443` **是通的**，`github.com:443` 不通
⇒ 如果以后还要靠 CI，**网络是唯一通路**（那个仓只有 github 远端）。

---

## 四、⇒ 对目标里「三引擎压力测试」的意义

```
目标：「结束后三引擎统一进行 GPU，CPU 渲染压力测试，看能效与核心调度」
✅ 三引擎能效 + 核心调度：**有硬数**（`task-16` 的表 + 我的归因）
✅ 但**"三引擎"目前实际是"两个"** —— `SampleCore` 与 `LearningCore` 逐字相同（定位未分化）
✅ 而**轻量版那条 C++ 路**现在**三平台都构建并逐位对账通过** ⇒ 那些压力测试数**有跨平台基础**
⚠ 缺：那三个数**都是在 Windows 上量的** —— Linux/macOS 上**没量过性能**
   （CI 只验"能构建 + 逐位一致"，**不量 ms/帧**）
```
