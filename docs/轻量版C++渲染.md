# 轻量版自己的 C++ 渲染后端（第一版）

> 目标：让轻量版**不再依赖 tkinter Canvas 画图** —— 自己光栅化、自己开窗。
> 作者裁决：「轻量版把渲染**委托**给专业版」算**违反**「两个内核完全不共用」⇒ 那条路关闭；
> 但批准**甲**：**允许轻量版有自己的窗口**。

---

## 1. 结果（实测，不是估算）

场景：**1000 个圆、铺满视野（800×450、半径 9、40 列 × 25 行格点）**

| | ms/帧 |
|---|---|
| tkinter 现状（要打的靶子） | 4.855 |
| **本后端（纯光栅化，不开窗）** | **0.1086 ~ 0.1130**（多次跑） |
| 本后端（含 Win32 上屏，120 帧） | 0.2313 ~ 0.2317 |
| 判据线 | ≤ 2.43 |

```
MS_PER_FRAME=0.1130          ← 判据线 2.43，余量约 21 倍
WINDOW_MS_PER_FRAME=0.2317   ← 含 StretchDIBits 上屏
VERDICT=PASS
```

⚠ **性能数字是"本机单次（预热 20 帧、测 200 帧）"**，没有重复取中位数 ——
上面给的是几次跑的**区间**，不是统计量。

⚠ **这个 0.1086 是"光栅化"的钱，不含输入、不含文字、不含 tkinter 的任何东西。**
⚠ 而且**场景是我定义的**（格点 40×25、r=9）——它**未必**和当初量 tkinter 那 4.855 时
用的场景逐像素相同。⇒ 「快 44.7 倍」这个比值**只在"两边都是 1000 个圆"这个口径上成立**，
不是同一份画面逐像素比出来的。

## 2. ⚠⚠ ms/帧 单独看是**没有意义**的 —— 所以判据里带了三样东西

本仓吃过一次**假绿**（dll 静默退回 ⇒ 判据全绿，但绿的是 Python 版）。
性能 benchmark 更容易犯这个错：**一个什么都不画的光栅化器是最快的**。
把 `hb_fb_fill_circle` 改成 `return;` 就能"达标"。

⇒ 所以每次测量都同时输出，三条**都成立**才认：

```
WRITTEN_PIXELS=122200000     累计写过的像素数（0 ⇒ 这个 ms 不算数）
WRITTEN_PER_FRAME=611000     每帧 611000 = 360000(清屏) + 251000(圆)
NONBG_PIXELS=251000          画完之后与背景不同的像素 —— "覆盖了多少面积"
EXPECTED_AREA_MIN=229000     圆的面积和 × 0.9 ⇒ 低于它说明压根没画满
CHECKSUM=0x018FE44A9793CD03  整帧 FNV-1a
DETERMINISTIC=1              同场景再画一遍，校验和**逐位相同**
EFFECTIVE=1                  上面四条全过
VERDICT=PASS
```

## 3. 怎么上屏（这条路唯一可能死掉的地方）

⚠ **不许走「光栅化 → 塞进 tkinter PhotoImage」** —— 那条已经量死：
Tk 8.6.15 的 `PhotoImage` **没有 `frombuffer`**（要 Tk 8.7+），退而求其次每帧 `put()`
整帧 800×450 是 **64.3 ms**，比老实画 300 个图元慢 15 倍。

⇒ 本后端**自己开窗**：`CreateWindowExW` + `StretchDIBits`（`window_win32.cpp`）。
`CS_OWNDC` 让窗口自带 DC，省掉每帧 `GetDC`。
窗口尺寸按**客户区正好 w×h** 反算（`AdjustWindowRect`）—— 不然画面会被标题栏挤掉一条。

### ⚠ 像素格式：一个**很容易误判成 bug** 的地方

直觉上以为"0x00RRGGBB 要转成 Windows 的 0x00BBGGRR，每帧多一遍全屏换序"——
**那是白花的**：`0x00RRGGBB` 在小端上的**内存字节序**就是 `[BB, GG, RR, 00]`，
正是 32 位 `BI_RGB` 的 DIB 要的 ⇒ **喂给 `StretchDIBits` 不用换序**。

⚠ 但**"我认为相同"不算数**，`--window` 会**读回窗口上的像素**：

```
PIXEL_WANT_RGB=0xE5484D
PIXEL_EXPECTED_COLORREF=0x4D48E5
PIXEL_GOT_COLORREF=0x4D48E5
PIXEL_COLOR_CORRECT=1
```

⚠⚠ **注意这里有个陷阱**：`GetPixel` 返回的是 `COLORREF`，通道序是 **0x00BBGGRR**，
和我们缓冲里的 **0x00RRGGBB** **正好相反**。所以直接 `got == want` 会**不相等**，
看起来像"颜色画反了" —— **其实画对了**（屏幕上那个像素就是 `#E5484D`）。
⇒ 判据必须拿 **COLORREF 口径**的期望值去比，否则会得到一个**假红**。

## 4. 独立性红线（作者裁决）

- **没有** `#include` 专业版的任何头，**没有**链它的任何 target
- 只用 **C++ 标准库 + 平台原生 API**（`<windows.h>`、user32、gdi32 是**系统库**）
- 专业版那份 `software_renderer.cpp` / `win32_window.cpp` **可以读、可以学它踩过的坑**，
  **但不拷代码** —— 本目录的实现是从零写的

## 5. ⚠ 本阶段**没做**的事（别当成能做）

1. **输入没接** —— tkinter 原来绑了键盘；自开窗后要自己 `WM_KEYDOWN`。
   现在只处理 **ESC 关窗**（好让 bench 能自己结束）。
   ⇒ **这一版窗口里的画面是"会动但玩不了"**的状态。
2. **没嵌回学习内核的 GUI** —— 现在仍是 tkinter 主窗 + Canvas。
   怎么把新后端接进去**等作者/Lead 定**（这一步刻意不抢跑）。
3. **文字没做** —— 图元只有圆 / 矩形 / 线，没有字形光栅化。
4. **只做了 Windows** —— Linux/macOS 走 `window_null.cpp`（`hb_window_backend()` 返回 `"null"`）。
   三平台的**构建**由 CI 验，本机没跑过 `.so` / `.dylib`。
5. **没有 alpha 混合、没有裁剪区、没有 z 序** —— 就"后画的盖前面的"。

## 6. 怎么复现

```bash
python _build/build_hblight.py --go            # 构建（含新目标 bench_backend）
C:\hblight\build\bench_backend.exe             # 纯光栅化
C:\hblight\build\bench_backend.exe --window    # 含开窗上屏 + 像素读回
```

⚠ **两个坑，都踩过**：
- 中文参数名（`--窗口`）**经 PowerShell 传不到 C++ 的 `argv`**（控制台编码）
  ⇒ 第一次跑"什么都没发生"，却**不报错** —— 和那次假绿是同一种失败形态。
  **所以命令行开关一律同时提供 ASCII 别名**（`--window` / `--frames`）。
- bench 的输出**全是 ASCII**：Windows 控制台是 cp936，C++ 里 `printf` 中文会乱码。

## 7. 交付物

| 文件 | 作用 |
|---|---|
| `native/hblight/raster.h` / `raster.cpp` | 32 位 ARGB 缓冲 + 填充圆（行扫描）/矩形/线 + 校验和/计数 |
| `native/hblight/window.h` | 平台无关窗口接口 |
| `native/hblight/window_win32.cpp` | Win32 窗口 + `StretchDIBits` 上屏 |
| `native/hblight/window_null.cpp` | 无窗口平台的**正经兜底**（不是占位符；`"null"` 是可观测的） |
| `native/hblight/tests/bench_backend.cpp` | 靶子实测 + 上屏验证 + 像素读回 |
