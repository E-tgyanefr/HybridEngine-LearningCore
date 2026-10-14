# ⚠ 一条**没被任何 CI 覆盖**的改动 —— 如实记（2026-10-14 · Round 25）

> ⚠ 这一份的目的：**防止把"CI 全绿"读成"我这处改动验过了"**。
> 本仓栽过三次同族的事（`test_neural.py` 没进 git · `neural_brain.cpp` 只在本机 ·
> libm 10 ULP），而**这一条是同一族的第五个形态**。

---

## 一、事实

```
① LearningCore 的 CI（`.github/workflows/cross-platform.yml`）**全绿** —— 四 job success
   ⚠ 但它只跑 `python _build/build_hblight.py --go` ⇒ **只构建 `libhblight`**
   ⚠ 而那个仓**根本没有 `src/` 目录**（`Test-Path src` = **False**）
   ⇒ ⭐ **它连 `src/ai` 都不存在，当然不会构建 `hybridengine_ai`**

② 我在 Round 23 给 `src/ai/CMakeLists.txt` 加了 `find_package(Threads REQUIRED)` +
   `target_link_libraries(hybridengine_ai PUBLIC Threads::Threads)`
   —— ⚠ **理由是"POSIX 上 `std::thread` 要链 pthread，而 MinGW/Windows 不加也能编过"**

③ 该验它的地方是 **`HybridEngine/.github/workflows/ci.yml`**：
   ```
   行51   cmake --preset unix
   行56   cmake --build --preset unix -j2
   ```
   ⇒ ⭐ 那才会构建 `src/ai`（我 Round 18 已把 `add_subdirectory(src/ai)` 提到 HEAD）
   ⚠ **而那个仓推不上去**（`origin` 的 push 被刻意禁用：`DISABLED-do-not-push-plaintext://`）

⇒ ⇒ ⭐⭐ **结论：`Threads::Threads` 这一处【在 Linux/macOS 上至今没有被验证过】。**
```

---

## 二、⚠ 而我差点把它算进"三平台绿"

```
我 Round 25 报"CI 又是四 job 全绿" —— ⚠ **那句话本身没错，但它与我这处改动无关**。
⇒ ⭐ 如果我不断言"那条 CI 构建什么"，就会得出错的结论：
   「`Threads::Threads` 在 Linux/macOS 上通过 CI 验证了」← **错的**
```

⚠ 而本仓那条规矩在这里正好适用：
> **A/B 能排除"是我的"，但不能推断"是别人的"** ——
> 同理：**"CI 绿"能说明"它测的那些过了"，不能说明"我改的那处过了"。**

---

## 三、⇒ 所以这条的正确状态是

```
✅ **形态是对的**：我抄的是 `native/hblight/CMakeLists.txt` 里**同一处**，
   而那一处**已经由 LearningCore 的 CI 在 Linux/macOS 上验过**（`libhblight` 建出来了）
   ⇒ 也就是说：**"`Threads::Threads` 在 POSIX 上可用"这件事，是被 CI 验过的**
   ⚠ 但 **"`hybridengine_ai` 这个目标挂上它之后能链上"没有被验过**
     （两者的差别：不同的 target、不同的源文件、不同的依赖图）
⚠ **未有 CI 覆盖**：`HybridEngine` 那个仓的 `ci.yml`（`cmake --preset unix` 会建 `src/ai`）
   ⇒ **只等那个仓能推**
```

⚠ **而这也解释了为什么 `HybridEngine` 仓的 4 个本地提交（含 `add_subdirectory(src/ai)`）
为什么值得推** —— 它们**正是让那个 CI 去构建新模块的钥匙**。

---

## 四、⚠ 我**没有**做的（逐条，别读大）

1. **没有在本机模拟 POSIX 构建** —— 本机是 Windows/MinGW，**没有 Linux 环境**
   ⚠ 我**不能**用"MinGW 下编得过"去代替"POSIX 上编得过"（那正是这条的成因）
2. **没有 WSL / 容器** —— 没查本机有没有（⚠ 而即使有，也不等于 CI 的镜像）
3. **没有改任何 CI** —— 那需要 `HybridEngine` 仓可推
4. **没有因为这条而回退** —— `Threads::Threads` 是**正确且必要**的（不加的话 POSIX 一定链不上），
   ⚠ **只是"还没被验过"**，不是"可能是错的"

---

## 五、⇒ 给下一个能动 `HybridEngine` 远端的人

```
① 推上去之后，**第一件要看的是 `ci.yml` 的 `cmake --build --preset unix`**
   —— ⭐ 那一步会**第一次**在 Linux 上构建 `hybridengine_ai`
② 若它红在 `undefined reference to pthread_create`（或类似）⇒ **`Threads::Threads` 没生效**
   ⇒ 那说明我的形态抄错了（或 `PUBLIC`/`PRIVATE` 用错）—— **那正是我要的发现**
③ 若它绿 ⇒ 「`hybridengine_ai` 在 Linux 上可构建」**才算验过**
```


---

# ⭐ 附（Round 26）：**我把理由查错了，已更正**

> 上一节记的是「`Threads::Threads` 这处**未被 CI 覆盖**」。这一节记的是**更基本的一件事**：
> ⚠ **我给的那个理由本身是错的**，而且我是**抄来的、没实测过**。

## 一、⚠ 原话（错的）

我在 `src/ai/CMakeLists.txt` 里写：
> 「⚠ 为什么必须加：`tensor.cpp` 的 `矩阵乘` 现在可以按输出行分块并行（`std::thread`）
> ⇒ **POSIX 上要链 pthread**。⚠ 而在 MinGW/Windows 上**不加也能编过**（所以本机发现不了）」

⚠ 而 `native/hblight/CMakeLists.txt:151`（队友写的）是**同一句**：
> 「为什么必须显式加：POSIX 上 `std::thread` 要链 pthread（GCC 用 `-pthread`），
> **不链的话是链接期报 `undefined reference to pthread_create`** —— 而 Windows（MinGW）上什么都不需要。」

⇒ ⭐ **两个人写的是同一句话** ⇒ 那说明它是**流传的说法**，而**我们都当成了事实**。

## 二、⭐ 实测（`C:\he_ai\pthread_probe.cpp`）

```
① 不带 `-pthread`：编译 **0** · 链接 **0** · **运行成功**（汇=6）· 依赖里**没有** libwinpthread
② 带   `-pthread`：同上
③ ⭐ **目标文件的未定义符号**（`nm -u`）：
     p_none.o（不带）：只有 `_ZNSt6thread15_M_start_threadESt10unique_ptrI...` 与 `_ZNSt6thread4joinEv`
                       ⚠ **连一个 `pthread_*` 都没有**
     p_yes.o（带）    ：**完全相同**
⇒ ⇒ ⭐ **「会报 `undefined reference to pthread_create`」在本机根本不成立** ——
     **`pthread_create` 压根不是引用**（MinGW 的 libstdc++ 用 `_M_start_thread` 走自己的路）
```

## 三、⇒ 那 `Threads::Threads` 到底该不该加？**该加，但理由要换**

```
✅ **站得住的理由只有两条**：
   ① `Threads::Threads` 是 **CMake 内建的 FindThreads**（**不是第三方库**）——
      它自己知道各平台该给什么（Linux 给 `-pthread`，macOS 可能什么都不给）
   ② **上界风险为零**：最坏情况是它给了个不需要的标志；
      而不加的话，**万一**某个平台/老 glibc 真需要，就是**链接期**红
⚠ **而这两条都不是"不加就编不过"**。

⚠ **glibc 2.34（2021-08）起把 libpthread 合进了 libc**；macOS 一直在 libSystem 里
  ⇒ **现代平台上它很可能本来就是多余的**
  ⚠ ⚠ **但这一句我没在 Linux/macOS 上实测过** —— 本机没有那个环境：
     `wsl --list` 空 · 无 docker/podman · 无 MSYS2/Cygwin（**四条都核过**）
```

## 四、⇒ 处置

```
✅ **没有回退** `Threads::Threads` —— 它是对的、是官方写法、没有害处
✅ **改了它上面的注释**：把"流传的说法"换成"实测的 + 标注没实测的"
   ⚠ 而**队友那份 `native/hblight/CMakeLists.txt:151` 的同一句话我没改**
     —— 那是它的文件、它已提交、且**它的结论（加 Threads）是对的**；
     我只在自己的文件里把理由修正，**并把这份记录留在公共文档里**
⭐ **一句话**：**加它是因为"官方写法 + 零上界风险"，不是因为"不加就编不过"。**
```

## 五、⚠ 这一条的教训（本会话第 N 次同族）

```
我一直在查别人的"流传的说法"（`git archive | tar` · 中文路径 · Windows tar · 短窗口计时），
⚠ **而这次我自己的注释里就有一句"流传的说法"，而且我抄了队友的同一句。**
⇒ ⭐ 判据：**"两个人写的一样"不是证据 —— 那可能只说明"传的是同一个说法"。**
⇒ ⭐ 而发现它的方法很便宜：**去量**（这一支探针不到 20 行，一次编译就出答案）。
```
