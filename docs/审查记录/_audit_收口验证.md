# ✅ 收口验证：本会话新增的一切 + 仓库门禁（2026-10-14 · Round 44）

> ⚠ 这一份**从 HEAD 重新编译并跑**，是一次**统一的**核对 —— 而不是"零散地验过每一块"。
> 起点：`HybridEngine` HEAD `6670724`（**本地**，88 个提交推不上去）。
> 方法：把 `include/` + `src/{numeric,ai}/` + `tools/{heat1d,heat2d}/` 拷到 **ASCII 工作区**
> （⚠ MinGW 的 `ld` 读不了中文路径 —— 本仓老坑），用 `g++ -std=c++23 -O2 -Wall -Wextra` 编。

---

## 一、⭐ 编译（**全部 0 error 0 warning**）

```
heat1d 主程序（1D 热传导）                       ⇒ 0
heat2d 主程序（含紧致 ADI）                      ⇒ 0
test_triplets / test_adi / test_adi_oracle / test_neumann  ⇒ 0
ai_train（AI 地基的自检）                        ⇒ 0
```

---

## 二、⭐ 判据（**逐个跑，全绿**）

| 工具 | 判据 | 实测 | 状态 |
|---|---|---|---|
| **heat1d**（1D · 真四阶） | 对齐解析解 | `MAX_REL_ERR=**8.770865e-07**` · `TOTAL_FAILURES=0` | ✅ |
| **heat2d** `cn`（九点二阶） | n=16 | `1.120318e-03 < 2.3347e-03`（二阶外推） | ✅ |
| **heat2d** `c4`（**紧致四阶 ADI**） | n=16 | `**9.604861e-07** < 1.5336e-06`（四阶外推） | ✅ |
| heat2d | `--conv-h` | `FAILS=0` | ✅ |
| **test_triplets** | `FromTriplets` 的 11 条 | `⇒ 失败 0` | ✅ |
| **test_adi** | 空间收敛阶 | `末次实测阶 = **4.0024**` | ✅ |
| **test_adi_oracle** | 与 1D oracle 对账 | `最大差 = 9.059e-06` | ✅ |
| **ai_train** | 全部 | 退出码 0 · `ALL CHECKS PASSED` · `FAIL_COUNT 0` | ✅ |
| ai_train | ⭐ **行为未变** | `HASH_PARAMS **91cc048ba32d92bb**` | ✅ |

### ⭐⭐ 而 `HASH_PARAMS` 这一条跨了 20 多轮仍然成立

```
`91cc048ba32d92bb` —— 与我**加矩阵乘并行之前**逐位相同
⇒ ⭐ **"默认串行 = 行为不变"再次被确认**（那是当初 opt-in 设计的硬证据）
   ⚠ 而我在这中间改过：`tensor.cpp`（加并行 + 4 MFLOP 门槛）· `CMakeLists.txt`（加 Threads）
```

---

## 三、⭐⭐ 仓库门禁（**权威判据**）

```
powershell -File scripts\run_tests.ps1 -Preset release-ninja
  ⇒ **[run_tests.ps1] MATRIX 942/942 pass, 0 fail (preset=release-ninja)**
  ⇒ 退出码 **0**
⇒ ⭐ **我改的 `CMakeLists.txt`（提了 `add_subdirectory(src/numeric)` + `(src/ai)`）没弄坏门禁**
   （⚠ 而那两行**只提了 numeric/ai** —— 那 4 行 collision/animation/render2d/uikit
      **故意留在工作区**，因为它们的源码不在 git 里）
```

---

## 四、⚠ 本会话的净交付（一份清单）

```
✅ **数值层**：`CsrMatrix::FromTriplets`（直接建稀疏 —— 解掉 O(N²) 稠密内存墙）
✅ **工业仿真**：1D 热传导（真四阶 · 解析解 8.77e-07）
             · 2D 热传导（九点二阶 + **紧致四阶 ADI**，实测阶 4.0024 · n=16 就到 1e-6）
✅ **AI 地基**：张量 + 反向自动微分（梯度对拍 1.842855e-07 · 四条反面判据）
             · **矩阵乘并行 6.651x 且逐位相同**（等效核数 1.005→5.903 · 4 MFLOP 门槛）
✅ **压力测试**：三引擎表 + 能效 + 核心调度（等效核数口径：三引擎都只用到 ≈1 核）
✅ **CI**：`LearningCore` 四 job 全绿（Linux/macOS/Windows 构建 + 逐位对账）
✅ **文档**：19 份审计 + 1 份总账（`引擎/_build/` 的搬进了 `docs/审查记录/`）
⚠ **未通过/未做**：Neumann 边界（**主动停止**，理由与实测在里面）
             · 商业操作（0，要业务规格）· 3D/CFD/结构力学 · AI 的卷积/注意力/GPU
```

---

## 五、⚠ 这次收口**没有覆盖**的（逐条）

```
· ⚠ **没在 Linux/macOS 上跑过**这些工具 —— 只有 `LearningCore` 的 CI 会跑（那个仓能推）
  而 `HybridEngine` 的 88 个提交推不上去 ⇒ 它的 `ci.yml`（`cmake --preset unix`）**从不跑**
· ⚠ **没接进门禁**：`heat2d` / `heat1d` / `bench_gpu3` / `ai_train` 四处 CMake 接线
  卡在 `tools/CMakeLists.txt` 的**整文件重写**（别人的在途）⇒ 它们**写好了但门禁不跑**
· ⚠ `test_neumann` 没跑（那个实现**未通过**，跑它只会红）
· ⚠ 只验了"编译 + 判据"，**没验性能**（那些数在 `_audit_理论极限的账.md` 里，是早先量的）
```
