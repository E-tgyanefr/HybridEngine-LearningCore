# ⭐ 独立复核：AI 训练地基（`task-19` 交付物）—— 2026-10-14 · Round 12

> ⚠ 这一份是 **Lead 自己编、自己跑**的结果 —— **不是转述 `verify-and-optimize` 的报告**。
> 交付物：`src/ai/{tensor,autograd,nn}.cpp` · `include/hybridengine/ai/*.hpp` ·
> `tools/ai_train/main.cpp`（41 KB）· `docs/AI训练-地基.md`

---

## 一、我怎么复核的（可复现）

```
① 把 `include/` + `src/ai/*.cpp` + `tools/ai_train/main.cpp` 拷到 **ASCII 路径** `C:\he_ai`
   （⚠ MinGW 的 ld 处理不了中文路径 —— 本仓老坑）
② 照它 README 的命令行编：
   g++ -std=c++23 -O2 -Wall -Wextra -I include tools/ai_train/main.cpp \
       src/ai/tensor.cpp src/ai/autograd.cpp src/ai/nn.cpp -o ai_train.exe
   ⇒ **退出码 0 · 零错误** · 389,943 B
③ 跑：`ai_train.exe`  ⇒ **退出码 0 · RESULT: ALL CHECKS PASSED · FAIL_COUNT 0**
```

---

## 二、⭐ 判据 A：梯度正确性（**这个任务的核心**）

```
graphs=5 · **params_checked=258（每一个参数，不是抽样）** · ops_covered = 9/9

  G1 线性+MSE            h=1e-5  max_rel_err = **1.054e-11**
  G2 mlp(3,8,8,2)+relu   h=1e-5  max_rel_err = 1.843e-07
  G3 mlp(3,8,3)+softmaxCE h=1e-5 max_rel_err = 3.509e-08
  G4 广播/加权 MSE        h=1e-5  max_rel_err = 3.741e-11
  G5 深层 mlp(2,4,4,4,1)  h=1e-5  max_rel_err = 5.338e-11

PASS A: max relative error < 1e-6   max_rel_err = **1.842855e-07**
```
⭐ 而且**步长选得有理由**：`h=1e-4/1e-5/1e-6` 三档都跑了，
  并写明 `chosen_h=1e-5`（`truncation ~h^2 vs roundoff ~eps/h；1e-5 时 roundoff ~2e-11`）
  ⇒ ⭐ **这正是我在任务书里要求的"报你选的步长和理由，并试 2~3 个步长"。**

---

## 三、⭐⭐ 四条反面判据 —— 每条都堵一个"看着过了"的口子

| 判据 | 它做了什么 | 结果 |
|---|---|---|
| **A-negative** | **故意破坏梯度**（置零 / 全体 ×1.001） | ✅ **抓到了**：`err=1.000` / `4.998e-04` |
| **B-negative** | 同种子同初始化、**不更新权重** | ✅ **损失逐位不动**：`1.130441/1.130441` · `0.741870/0.741870` |
| **C2** | **不同种子必须给出不同结果** | ✅ `777=91cc048ba32d92bb` vs `778=e7a0e57a911bce7d` |
| **S6** | **拒绝路径必须抛**（不许静默 clamp） | ✅ `matmul / ce_label / zero_dim / data_count` **4/4 抛了** |
| **W** | backward 是**真活**（不是空基线） | ✅ forward-only `0.0492` → fwd+bwd `0.3708` ⇒ **+655%** |

⭐⭐ **其中 C2 和 S6 是我【没要求】的** —— 它自己想出来的，而且都对：
- **C2**：没有它，"同种子逐位相同"可能是**空话**（若所有种子都给同一结果）
- **S6**：本仓最怕"**没跑到 = 看着过了**" ⇒ **拒绝路径也必须被检**

---

## 四、训练真的在学 · 确定性 · 张量自检

```
[B] task1 = fit sin(x)cos(y)   mlp(2,16,16,1) SGD(lr=0.03,m=0.9) 337 参数
        1.130441 → **0.001172**（降 99.9%）
    task2 = XOR               mlp(2,8,2) softmax+CE
        0.741870 → **0.001034** · **correct = 4/4**
[C] seed=777 run1 params_hash=91cc048ba32d92bb · run2=**91cc048ba32d92bb**（逐位同）
[S] S1 matmul 对手算 2x2 积 ✅ C=19,22,43,50
    S2 广播 add (2,3)+(3) 沿最后一轴 ✅
    S3 softmax 行和=1（|logit| 到 8）✅ max|row_sum-1|=2.220e-16
    S4 **融合 cross-entropy == 显式 log-softmax 路径** ✅ 1.155801687294 == 1.155801687294
    S5 广播后归约是精确逆运算 ✅
```

---

## 五、⚠ 必须如实说清的边界

1. **它的 AI 模块还没接进主 `CMakeLists`** —— 顶层文件不在它写盘范围，
   所以它**没改**（**守规矩，对的**）。
   ⇒ ⚠ 现状是"**能独立编译的库**"（README 里两条命令），**不是"引擎的一部分"**。
   ⇒ 接入那一行 `add_subdirectory(src/ai)` **要有人加** —— **我不加**（顶层多人共动，有并发风险）。
2. **它没在 `tests/` 下加判据**（我任务书要求"新判据"）⇒ 现在这条判据**不在门禁里**
   ⚠ 也就是说：**跑 `run_tests.ps1` 不会跑它** ⇒ **"判据写出来 ≠ 判据在门禁里跑"**
   —— 而这**正是本仓栽过三次的那件事** ⇒ 已记，要补。
3. **我复核的是"它自己的判据全过"**，我没有**另写一套独立判据**去交叉验证
   （那就是重复劳动）⇒ 但我**读了它的判据逻辑**，四个反面的设计**都是有效的**。
4. **没验**：GPU 训练（没有 compute 通路）· 卷积/注意力 · 大规模 · 混合精度 · 分布式。

---

## 六、⇒ 结论

```
⭐ 这一份交付物的质量是**本次会话里最高的**，理由：
   ① 梯度对拍**逐参数**（258 个，不是抽样）
   ② 四条反面判据，其中**两条是它自己加的**（C2 空话防护 / S6 拒绝路径）
   ③ 步长**报了三档 + 选了并说明理由**
   ④ 每一步都打印**真实工作量**（参数量/样本/轮数/ms），不是"跑完了"
   ⑤ 拒绝路径明确抛（不静默 clamp）
⚠ 唯一的缺口：**它还没进主 CMakeLists，也没进门禁** ⇒ 判据"写出来了但没人跑"
```
