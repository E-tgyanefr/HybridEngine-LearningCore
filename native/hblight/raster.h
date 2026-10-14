/* hblight/raster.h —— 轻量版**自己的**软件光栅化器（C ABI）。
 *
 * ══════════════════════════════════════════════════════════════════════════
 * ⚠ 独立性红线（作者裁决）
 * ══════════════════════════════════════════════════════════════════════════
 * 专业版（`引擎/HybridEngine/src/platform/software_renderer.cpp`）那份光栅化器
 * **可以读、可以学它踩过的坑**，但：
 *     · **不许 `#include` 它的头**
 *     · **不许链它的库**
 *     · **不许拷代码**
 * ⇒ 本文件与 `raster.cpp` 是**从零写的**，只依赖 C++ 标准库。
 *
 * ══════════════════════════════════════════════════════════════════════════
 * 它是什么
 * ══════════════════════════════════════════════════════════════════════════
 * 一块**直写像素**的 32 位 ARGB 缓冲 + 几个填充图元。
 * 没有 GPU、没有第三方、没有窗口 —— 窗口在 `window.h`，两件事分开。
 *
 * ⚠ 颜色格式是 **0x00RRGGBB**（不是 Windows 原生的 0x00BBGGRR）。
 *   上屏那一步由 `window_win32.cpp` 负责换序 —— 换序只做一次，
 *   而不是在每个图元里做（那是白花的性能）。
 */
#ifndef HBLIGHT_RASTER_H
#define HBLIGHT_RASTER_H

#include <stddef.h>
#include <stdint.h>

#if defined(_WIN32)
#  define HB_RAPI __declspec(dllexport)
#elif defined(__GNUC__)
#  define HB_RAPI __attribute__((visibility("default")))
#else
#  define HB_RAPI
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct hb_fb hb_fb;                 /* framebuffer（不透明） */

HB_RAPI hb_fb*   hb_fb_new(int w, int h);
HB_RAPI void     hb_fb_free(hb_fb* fb);
HB_RAPI int      hb_fb_width(const hb_fb* fb);
HB_RAPI int      hb_fb_height(const hb_fb* fb);
HB_RAPI uint32_t* hb_fb_pixels(hb_fb* fb);
HB_RAPI size_t   hb_fb_bytes(const hb_fb* fb);

HB_RAPI void hb_fb_clear(hb_fb* fb, uint32_t argb);
HB_RAPI void hb_fb_fill_circle(hb_fb* fb, double cx, double cy, double r, uint32_t argb);
HB_RAPI void hb_fb_fill_rect(hb_fb* fb, double x, double y, double w, double h, uint32_t argb);
HB_RAPI void hb_fb_line(hb_fb* fb, double x1, double y1, double x2, double y2,
                        uint32_t argb, double width);

/* ── 判据用：证"真的画了东西" ────────────────────────────────────────────
 * ⚠⚠ 为什么光栅化器必须自带这两条：
 *   本轮已经吃过一次**假绿** —— 判据 exit 0，但绿的是**没有生效的那条路**。
 *   性能benchmark 更容易犯这个错：**一个什么都不画的"光栅化器"是最快的**。
 *   ⇒ 所以 ms/帧 必须和"到底写了多少像素"一起报，缺一个就没有意义。 */

/*: 本进程**累计**写过的像素数（含被覆盖的重复写）。0 = 什么都没画。 */
HB_RAPI long long hb_fb_written_pixels(void);
HB_RAPI void      hb_fb_reset_stats(void);
/*: FNV-1a 64 位校验和 —— 两次跑同一场景应当**逐位相同**（确定性）。 */
HB_RAPI uint64_t  hb_fb_checksum(const hb_fb* fb);
/*: 与给定背景色**不同**的像素个数 —— "画上去了多少面积"的粗口径。 */
HB_RAPI long long hb_fb_count_not(const hb_fb* fb, uint32_t argb);

/* ── 并行（task-17）────────────────────────────────────────────────────────
 *
 * ⚠⚠ 三条设计红线（都能被判据钉住）：
 *
 *   ① **逐位不变**：并行只切"**行区间**"——每一行仍然由**唯一一个线程**整行写完；
 *      共享累加器（写过的像素数）用"**每块私有 + 事后按块序求和**"。
 *      ⇒ 同一场景，并行与串行的**校验和必须逐位相同**。
 *   ② **只在一个图元内部并行**，绝不跨图元：图元是"后画的盖前面的"（z 序），
 *      跨图元并行会让覆盖顺序变得不确定 —— 那会**改变像素**，不是优化。
 *   ③ **小图元自动走串行**：一次并行派发要两次加锁 + 两次条件变量（微秒级），
 *      而半径 9 的小圆整块只有几百像素（纳秒级）⇒ 并行反而**慢得多**。
 *      ⇒ 用"行数 + 工作量"两个阈值卡住（实测标定，见 `tests/bench_backend.cpp`）。
 *
 * ⚠ 零第三方：**只用 C++ 标准库**（`std::thread` + 条件变量）。
 *   不用 OpenMP 的理由不是口味：`-fopenmp` 会给 .dll 引入 `libgomp-1.dll`，
 *   而那个 DLL 不在系统 PATH 上 ⇒ `ctypes.CDLL` 加载失败 ⇒ 静默退回 Python 版
 *   ⇒ **判据全绿但绿的是 Python 版**（本仓在 `-static` 那一段已经栽过一次）。
 */
/*: 当前生效的线程数。**1 = 串行**（0 = 还没建池）。 */
HB_RAPI int       hb_raster_threads(void);
/*: `0` = 自动（硬件核数）· `1` = 强制串行 · `k` = 最多用 k 个线程。
 *  ⚠ 允许运行期切换：`bench_backend` 靠它做"同一进程内 串行 vs 并行"的对照
 *    （同一个二进制 ⇒ 不存在"两份代码/两次构建"的口径差）。 */
HB_RAPI void      hb_raster_set_threads(int n);
/*: ⚠ **证据**用：真的发生过多少次并行派发。`exit 0` 不等于"走了并行那条路" ——
 *  这个计数就是用来断言"走的是哪条路"的（0 次 = 一直在串行）。 */
HB_RAPI long long hb_raster_parallel_dispatches(void);
/*: 并行派发时**实际参与**过的最大线程数（0 = 从没并行过）。 */
HB_RAPI int       hb_raster_max_participants(void);

#ifdef __cplusplus
}
#endif

#endif /* HBLIGHT_RASTER_H */
