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

/* ══════════════════════════════════════════════════════════════════════════
 * 子窗口（`WS_CHILD`）—— 为"嵌进 Tk 窗口"这个架构做的**实验接口**
 *
 * ⚠ 这一组是**先验前提、再盖房子**用的：
 *   待验的前提 = 「**Tk 自己的消息循环能不能驱动一个外部创建的同线程子窗口**」。
 *   不成立的话，"同线程 + 子窗口"这个架构要退回顶层窗口或子进程 ——
 *   两个退路都比原方案差。⇒ 所以**先有 embed_probe，再谈接进 `pyengine.后端`**。
 * ══════════════════════════════════════════════════════════════════════════ */

typedef struct hb_child hb_child;

/*: 在 `parent`（Win32 的 `HWND`，从 Tk 的 `winfo_id()` 那一路取来）下建一个子窗口。 */
HB_WAPI hb_child* hb_child_new(void* parent, int x, int y, int w, int h);
HB_WAPI void      hb_child_free(hb_child* c);

/*: 整帧 blit 进子窗口。`argb` 见 raster.h（0x00RRGGBB）。返回 0 = 失败。 */
HB_WAPI int hb_child_present(hb_child* c, const uint32_t* argb, int w, int h);

/*: 抽一次本线程的消息。
 *  ⚠ `丢弃 = 1` ⇒ **只移除、不派发** —— 那是**反面判据**要用的：
 *    同线程的所有窗口**共用一个消息队列**，所以我们 `PeekMessage` 时会把
 *    **Tk 自己的消息也一起捞走**；只移除不派发就会**把 Tk 弄坏**。
 *    用它来证明"Tk 控件还正常"那条判据**真的在测东西**。 */
HB_WAPI int hb_child_pump(hb_child* c, int 丢弃);

HB_WAPI void*     hb_child_native(hb_child* c);
HB_WAPI long long hb_child_presented(const hb_child* c);
/*: 读回子窗口上的一个像素（`COLORREF` 口径 —— ⚠ 与 `0x00RRGGBB` **相反**，见 docs）。 */
HB_WAPI uint32_t  hb_child_get_pixel(hb_child* c, int x, int y);
HB_WAPI int       hb_child_visible(const hb_child* c);
/*: 后端名：`"win32"` / `"null"` —— 路径证明。 */
HB_WAPI const char* hb_child_backend(void);
/*: 把一条**真实的鼠标点击**投递给某个 HWND（客户区坐标）。
 *  ⚠ 用 `PostMessage` 而不是 `SendInput`：**不动用户的鼠标**，
 *    但仍然走真正的 Win32 消息队列 ⇒ 能测出"消息有没有被吃掉"。 */
HB_WAPI int hb_child_post_click(void* hwnd, int x, int y);
/*: 查一个 HWND 的祖先根窗口。
 *  ⚠ Tk 的 `winfo_id()` 在 Windows 上给的是**子窗**，真顶层要 `GetAncestor(GA_ROOT)`。 */
HB_WAPI void* hb_child_root_of(void* hwnd);

#ifdef __cplusplus
}
#endif

#endif /* HBLIGHT_WINDOW_H */
