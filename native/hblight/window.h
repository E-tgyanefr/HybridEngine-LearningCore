/* hblight/window.h —— 平台无关的窗口接口。
 *
 * ⚠ 为什么轻量版要**自己开窗**（而不是继续嵌 tkinter）：
 *   实测把整帧塞进 tkinter 的路**已经量死** ——
 *       Tk 8.6.15 的 PhotoImage **没有 `frombuffer`**（要 Tk 8.7+）
 *       退而求其次每帧 `put()` 整帧 800×450：**64.3 ms**，比老实画 300 个图元慢 15 倍
 *   ⇒ 所以"光栅化 → 塞进 tkinter"这条**不许走**。作者因此批了「轻量版可以有自己的窗口」。
 *
 * ⚠ 本阶段（第一版）**只做渲染 + 窗口，不接输入事件**。
 *   tkinter 原来绑了键盘；自开窗后那部分要自己接，**那是下一步**。
 *   没接就是没接 —— `hb_window_backend()` 与文档里都要说清楚，别让人以为能玩。
 */
#ifndef HBLIGHT_WINDOW_H
#define HBLIGHT_WINDOW_H

#include <stdint.h>

#if defined(_WIN32)
#  define HB_WAPI __declspec(dllexport)
#elif defined(__GNUC__)
#  define HB_WAPI __attribute__((visibility("default")))
#else
#  define HB_WAPI
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct hb_window hb_window;

/*: 建窗。失败返回 NULL（无头环境、没有 DISPLAY、平台没实现都会走这条）。 */
HB_WAPI hb_window* hb_window_new(const char* title, int w, int h);
HB_WAPI void       hb_window_free(hb_window* win);

/*: ⚠ 返回**实际用的后端名** —— `"win32"` / `"null"`。
 *  这就是"路径证明"：`exit 0` 不等于"真的开了窗"，
 *  必须能问出**走的哪条路**（本仓吃过一次假绿：dll 静默退回 ⇒ 判据全绿但绿的是 Python）。 */
HB_WAPI const char* hb_window_backend(void);

/*: 把 `argb`（0x00RRGGBB，见 raster.h）整帧呈上去。返回 0 = 失败。 */
HB_WAPI int hb_window_present(hb_window* win, const uint32_t* argb, int w, int h);

/*: 抽一次消息。返回 0 = 该退出了（关窗/ESC）。 */
HB_WAPI int hb_window_pump(hb_window* win);

/*: 这个窗口对象是不是真的可用（NULL 后端 ⇒ 0）。 */
HB_WAPI int hb_window_valid(const hb_window* win);

/*: 呈现累计帧数 —— 用来证"真的呈上去了"，而不是只跑了个空循环。 */
HB_WAPI long long hb_window_presented_frames(const hb_window* win);

/*: 原生句柄（Win32 上是 `HWND`，别的平台是 NULL）。
 *  ⚠ 存在的理由：判据要能**从窗口上读回一个像素**去验"像素格式对不对"。
 *    只靠推理说"字节序应该一样"是不够的 —— 本仓的规矩是把它量出来。 */
HB_WAPI void* hb_window_native(hb_window* win);

#ifdef __cplusplus
}
#endif

#endif /* HBLIGHT_WINDOW_H */
