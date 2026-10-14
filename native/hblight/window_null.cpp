/* hblight/window_null.cpp —— 没有窗口平台时的**兜底实现**。
 *
 * ⚠ 它不是"占位符"，是**正经的后端**：
 *   · 无头机器 / CI / 手机（Chaquopy）上本来就没有窗口
 *   · 判据必须能在那些环境里**照样跑**（本仓一贯的做法：能让判据覆盖的就不留给肉眼）
 *   · 而 `hb_window_backend()` 返回 `"null"` ⇒ **"没开窗"是可观测的**，
 *     不会和"开了窗"长得一样（本仓吃过一次假绿，就是"两条路长得一样"）
 *
 * ⇒ 所以：建窗返回 NULL、`present` 返回失败、`presented` 恒为 0，
 *   但**接口齐全、不崩**。
 */
#if !defined(_WIN32)

#include "window.h"

extern "C" {

HB_WAPI const char* hb_window_backend(void) { return "null"; }

HB_WAPI hb_window* hb_window_new(const char* title, int w, int h) {
    (void)title;
    (void)w;
    (void)h;
    return nullptr;                      /* ⚠ 明确的"这里没有窗口"，不是错误 */
}

HB_WAPI void hb_window_free(hb_window* win) { (void)win; }

HB_WAPI int hb_window_valid(const hb_window* win) {
    (void)win;
    return 0;
}

HB_WAPI long long hb_window_presented_frames(const hb_window* win) {
    (void)win;
    return 0;
}

HB_WAPI void* hb_window_native(hb_window* win) {
    (void)win;
    return nullptr;
}

HB_WAPI int hb_window_present(hb_window* win, const uint32_t* argb, int w, int h) {
    (void)win;
    (void)argb;
    (void)w;
    (void)h;
    return 0;
}

HB_WAPI int hb_window_pump(hb_window* win) {
    (void)win;
    return 0;                            /* 没有窗口 ⇒ 立刻"该退出了" */
}

}  // extern "C"

#endif /* !_WIN32 */
