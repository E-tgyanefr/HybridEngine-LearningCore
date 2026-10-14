/* hblight/window_embed.cpp —— `WS_CHILD` 子窗口 + 消息泵（**实验接口**）。
 *
 * ⚠ 这是一个**新文件**，不是往 `window_win32.cpp` 里塞东西 ——
 *   按"只追加、别动已有逻辑"的要求，已有那份窗口代码**一个字节都没改**。
 *
 * ══════════════════════════════════════════════════════════════════════════
 * 这个文件要回答的前提
 * ══════════════════════════════════════════════════════════════════════════
 * 「**Tk 自己的消息循环能不能驱动一个外部创建的同线程子窗口？**」
 *
 * Win32 的规则：**同线程的所有窗口共用一个消息队列** ⇒
 *   Tk 的 `update()` 内部会 `PeekMessage`/`DispatchMessage`，而 `DispatchMessage`
 *   会按消息里的 hwnd 找到**正确的窗口过程** ⇒ 理论上我们的子窗口也会被驱动。
 *
 * ⚠ 但"理论上"在本仓不算数 ⇒ `tests/embed_probe.py` 会**实测**，
 *   而且带**反面判据**：故意"只移除不派发"，看 Tk 会不会坏。
 *   会坏 ⇒ 说明"Tk 控件正常"那条判据真的在测东西（不是恒真）。
 */
#if defined(_WIN32)

#include "window.h"

#include <windows.h>

#include <cstring>

struct hb_child {
    HWND hwnd = nullptr;
    long long presented = 0;
};

namespace {

LRESULT CALLBACK 子窗过程(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp) {
    switch (msg) {
        case WM_ERASEBKGND:
            /* ⚠ 返回 1 = "我自己擦" —— 不然每次重画都会先刷一遍背景色，
             *   在"每帧整幅 blit"的模式下那是白闪一下（画面抖）。 */
            return 1;
        case WM_PAINT: {
            /* ⚠ 必须 `BeginPaint`/`EndPaint` 配对 —— 不配对的话
             *   `GetDC` 读到的东西是未定义的，判据会随机红。 */
            PAINTSTRUCT ps;
            HDC dc = BeginPaint(hwnd, &ps);
            (void)dc;
            EndPaint(hwnd, &ps);
            return 0;
        }
        case WM_CLOSE:
            return 0;                       /* 子窗口不自己关 —— 由宿主销毁 */
        default:
            break;
    }
    return DefWindowProcW(hwnd, msg, wp, lp);
}

const wchar_t* k类名 = L"HbLightChildClass";
bool g_注册 = false;

}  // namespace

extern "C" {

HB_WAPI const char* hb_child_backend(void) { return "win32"; }

HB_WAPI void* hb_child_root_of(void* hwnd) {
    if (!hwnd) return nullptr;
    /* ⚠ `winfo_id()` 给的是 Tk 的**子窗**；这里往上找真正的顶层。 */
    return static_cast<void*>(GetAncestor(static_cast<HWND>(hwnd), GA_ROOT));
}

HB_WAPI hb_child* hb_child_new(void* parent, int x, int y, int w, int h) {
    if (!parent || w <= 0 || h <= 0) return nullptr;
    HINSTANCE inst = GetModuleHandleW(nullptr);
    if (!g_注册) {
        WNDCLASSEXW wc;
        std::memset(&wc, 0, sizeof(wc));
        wc.cbSize = sizeof(wc);
        wc.style = CS_OWNDC;
        wc.lpfnWndProc = 子窗过程;
        wc.hInstance = inst;
        wc.hCursor = LoadCursorW(nullptr, reinterpret_cast<LPCWSTR>(IDC_ARROW));
        wc.lpszClassName = k类名;
        if (!RegisterClassExW(&wc)) return nullptr;
        g_注册 = true;
    }

    HWND 父 = static_cast<HWND>(parent);
    /* ⚠ `WS_CHILD` 必须带 `WS_VISIBLE`，否则子窗口建出来是隐藏的 ——
     *   而"隐藏"的症状是**什么都看不到**，很容易被当成"画错了"。 */
    HWND hw = CreateWindowExW(0, k类名, L"", WS_CHILD | WS_VISIBLE,
                              x, y, w, h, 父, nullptr, inst, nullptr);
    if (!hw) return nullptr;

    hb_child* c = new hb_child();
    c->hwnd = hw;
    return c;
}

HB_WAPI void hb_child_free(hb_child* c) {
    if (!c) return;
    if (c->hwnd) DestroyWindow(c->hwnd);
    delete c;
}

HB_WAPI void* hb_child_native(hb_child* c) { return c ? static_cast<void*>(c->hwnd) : nullptr; }
HB_WAPI long long hb_child_presented(const hb_child* c) { return c ? c->presented : 0; }

HB_WAPI int hb_child_visible(const hb_child* c) {
    if (!c || !c->hwnd) return 0;
    return IsWindowVisible(c->hwnd) ? 1 : 0;
}

HB_WAPI int hb_child_present(hb_child* c, const uint32_t* argb, int w, int h) {
    if (!c || !c->hwnd || !argb || w <= 0 || h <= 0) return 0;
    HDC dc = GetDC(c->hwnd);                 /* CS_OWNDC ⇒ 不用 ReleaseDC，但还了也无害 */

    BITMAPINFO bmi;
    std::memset(&bmi, 0, sizeof(bmi));
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = w;
    bmi.bmiHeader.biHeight = -h;             /* 负 = 自上而下，与我们的行序一致 */
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32;
    bmi.bmiHeader.biCompression = BI_RGB;

    const int 行数 = StretchDIBits(dc, 0, 0, w, h, 0, 0, w, h,
                                   argb, &bmi, DIB_RGB_COLORS, SRCCOPY);
    ReleaseDC(c->hwnd, dc);
    if (行数 <= 0) return 0;

    ++c->presented;
    return 1;
}

HB_WAPI uint32_t hb_child_get_pixel(hb_child* c, int x, int y) {
    if (!c || !c->hwnd) return 0xFFFFFFFFu;
    HDC dc = GetDC(c->hwnd);
    const COLORREF v = GetPixel(dc, x, y);
    ReleaseDC(c->hwnd, dc);
    return static_cast<uint32_t>(v);
}

/*: ⚠⚠ 消息泵 —— 这个函数的两种行为就是**正/反面判据**本身。
 *
 *   `丢弃 = 0`（正常）：捞出来的消息**派发掉** ⇒ Tk 与我们的子窗口都能收到。
 *   `丢弃 = 1`（反面）：捞出来**只移除、不派发** ⇒
 *       同线程共用一个队列 ⇒ **Tk 自己的消息被我们吃掉了** ⇒ Tk 控件应当失灵。
 *       如果这时 Tk 仍然正常，说明"Tk 控件还正常"那条判据**测不出东西**（恒真），
 *       那它就是一条假判据 —— 这比方案不成立更糟。
 *
 *   ⚠ 只取本线程、且只取 `hwnd` 属于本线程的窗口的消息（`PeekMessage` 的 hwnd 传 NULL
 *     就是"本线程所有窗口"）。这正是危险所在，也正是要被证明的地方。 */
HB_WAPI int hb_child_pump(hb_child* c, int 丢弃) {
    (void)c;
    int 处理 = 0;
    MSG msg;
    while (PeekMessageW(&msg, nullptr, 0, 0, PM_REMOVE)) {
        ++处理;
        if (丢弃) continue;                  /* ★ 反面：只移除，不派发 */
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }
    return 处理;
}

HB_WAPI int hb_child_post_click(void* hwnd, int x, int y) {
    if (!hwnd) return 0;
    HWND h = static_cast<HWND>(hwnd);
    const LPARAM lp = MAKELPARAM(x, y);
    /* 真实序列：先移动、再按下、最后抬起。少一步 Tk 可能不认。 */
    if (!PostMessageW(h, WM_MOUSEMOVE, 0, lp)) return 0;
    if (!PostMessageW(h, WM_LBUTTONDOWN, MK_LBUTTON, lp)) return 0;
    if (!PostMessageW(h, WM_LBUTTONUP, 0, lp)) return 0;
    return 1;
}

}  // extern "C"

#else  /* ── 非 Windows：同一套接口的兜底实现 ── */

#include "window.h"

extern "C" {

HB_WAPI const char* hb_child_backend(void) { return "null"; }
HB_WAPI void* hb_child_root_of(void* hwnd) { (void)hwnd; return nullptr; }
HB_WAPI hb_child* hb_child_new(void* parent, int x, int y, int w, int h) {
    (void)parent; (void)x; (void)y; (void)w; (void)h;
    return nullptr;
}
HB_WAPI void hb_child_free(hb_child* c) { (void)c; }
HB_WAPI void* hb_child_native(hb_child* c) { (void)c; return nullptr; }
HB_WAPI long long hb_child_presented(const hb_child* c) { (void)c; return 0; }
HB_WAPI int hb_child_visible(const hb_child* c) { (void)c; return 0; }
HB_WAPI int hb_child_present(hb_child* c, const uint32_t* a, int w, int h) {
    (void)c; (void)a; (void)w; (void)h;
    return 0;
}
HB_WAPI uint32_t hb_child_get_pixel(hb_child* c, int x, int y) {
    (void)c; (void)x; (void)y;
    return 0xFFFFFFFFu;
}
HB_WAPI int hb_child_pump(hb_child* c, int 丢弃) { (void)c; (void)丢弃; return 0; }
HB_WAPI int hb_child_post_click(void* hwnd, int x, int y) {
    (void)hwnd; (void)x; (void)y;
    return 0;
}

}  // extern "C"

#endif
