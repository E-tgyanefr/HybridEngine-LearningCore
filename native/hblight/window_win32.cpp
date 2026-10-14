/* hblight/window_win32.cpp —— Win32 窗口 + 整帧 blit。
 *
 * ⚠ 独立性红线：**没有** `#include` 专业版的任何头、**没有**链它的库。
 *   本文件只用 `<windows.h>`（平台原生 API，那是允许的）。
 *
 * ══════════════════════════════════════════════════════════════════════════
 * ⚠⚠ 关于像素格式：**不需要换序**（这一条我实测过，见 docs/轻量版C++渲染.md）
 * ══════════════════════════════════════════════════════════════════════════
 * 直觉上会以为"0x00RRGGBB 要转成 Windows 的 0x00BBGGRR"，于是每帧多做一遍
 * 全屏换序（800×450 = 36 万次）—— **那是白花的**。
 *
 * 实际：`0x00RRGGBB` 在小端机器上的**内存字节序**是 `[BB, GG, RR, 00]`，
 *   而 32 位 `BI_RGB` 的 DIB 要的正是内存里的 `[B, G, R, 未用]`。
 *   ⇒ **逐字节相同**，直接用 `StretchDIBits` 喂就行。
 *
 * ⚠ 但"我认为相同"不算数 —— `bench_backend.exe --窗口` 会**读回窗口上的像素**
 *   （`GetPixel`）和源缓冲比对，把这条**验出来**而不是推出来。
 */
#if defined(_WIN32)

#include "window.h"

#include <windows.h>

#include <cstring>
#include <string>

struct hb_window {
    HWND hwnd = nullptr;
    HDC  dc = nullptr;
    int  w = 0;
    int  h = 0;
    int  quit = 0;
    long long presented = 0;
};

namespace {

const wchar_t* k类名 = L"HbLightWindowClass";
bool g_已注册 = false;

std::wstring 宽字符(const char* s) {
    if (!s) return std::wstring();
    const int n = MultiByteToWideChar(CP_UTF8, 0, s, -1, nullptr, 0);
    if (n <= 0) return std::wstring();
    std::wstring w(static_cast<size_t>(n), L'\0');
    MultiByteToWideChar(CP_UTF8, 0, s, -1, &w[0], n);
    if (!w.empty() && w.back() == L'\0') w.pop_back();
    return w;
}

LRESULT CALLBACK 窗口过程(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp) {
    hb_window* self = reinterpret_cast<hb_window*>(GetWindowLongPtrW(hwnd, GWLP_USERDATA));
    switch (msg) {
        case WM_CLOSE:
            if (self) self->quit = 1;
            DestroyWindow(hwnd);
            return 0;
        case WM_DESTROY:
            if (self) self->quit = 1;
            PostQuitMessage(0);
            return 0;
        /* ⚠ 输入**没接**：这里只处理"ESC 关窗"，好让 bench 能自己结束。
         *   真正的按键/鼠标注入是下一步 —— 见 window.h 的说明。 */
        case WM_KEYDOWN:
            if (wp == VK_ESCAPE && self) {
                self->quit = 1;
                DestroyWindow(hwnd);
            }
            return 0;
        default:
            break;
    }
    return DefWindowProcW(hwnd, msg, wp, lp);
}

}  // namespace

extern "C" {

HB_WAPI const char* hb_window_backend(void) { return "win32"; }

HB_WAPI hb_window* hb_window_new(const char* title, int w, int h) {
    if (w <= 0 || h <= 0) return nullptr;

    HINSTANCE inst = GetModuleHandleW(nullptr);
    if (!g_已注册) {
        WNDCLASSEXW wc;
        std::memset(&wc, 0, sizeof(wc));
        wc.cbSize = sizeof(wc);
        wc.style = CS_OWNDC;                      /* ⚠ 自己的 DC：blit 时少一次 GetDC */
        wc.lpfnWndProc = 窗口过程;
        wc.hInstance = inst;
        /* ⚠ `IDC_ARROW` 展开成 `MAKEINTRESOURCE(...)` ⇒ 类型是 **`LPSTR`（char*）**，
         *   而 `LoadCursorW` 要 `LPCWSTR`。不转就报
         *       cannot convert 'LPSTR' to 'LPCWSTR'
         *   —— 这是"混用 ANSI 宏与宽字符 API"的经典坑（`IDC_*` / `IDI_*` 都是这样）。 */
        wc.hCursor = LoadCursorW(nullptr, reinterpret_cast<LPCWSTR>(IDC_ARROW));
        wc.lpszClassName = k类名;
        if (!RegisterClassExW(&wc)) return nullptr;
        g_已注册 = true;
    }

    /* ⚠ 按"客户区正好 w×h"反算窗口尺寸 —— 不然画面会被标题栏挤掉一块，
     *   表现出来的现象是"右下角少一条"，很难查。 */
    RECT r;
    r.left = 0; r.top = 0; r.right = w; r.bottom = h;
    const DWORD 风格 = WS_OVERLAPPEDWINDOW & ~WS_THICKFRAME & ~WS_MAXIMIZEBOX;
    AdjustWindowRect(&r, 风格, FALSE);

    hb_window* win = new hb_window();
    win->w = w;
    win->h = h;

    const std::wstring 标 = 宽字符(title ? title : "hblight");

    win->hwnd = CreateWindowExW(0, k类名, 标.c_str(), 风格,
                                CW_USEDEFAULT, CW_USEDEFAULT,
                                r.right - r.left, r.bottom - r.top,
                                nullptr, nullptr, inst, nullptr);
    if (!win->hwnd) {
        delete win;
        return nullptr;
    }
    SetWindowLongPtrW(win->hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(win));
    ShowWindow(win->hwnd, SW_SHOWNORMAL);
    UpdateWindow(win->hwnd);
    win->dc = GetDC(win->hwnd);                   /* CS_OWNDC ⇒ 不用还 */

    /* 把首帧之前积的消息清掉，免得一开窗就收到一堆重画 */
    MSG msg;
    while (PeekMessageW(&msg, nullptr, 0, 0, PM_REMOVE)) {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }
    return win;
}

HB_WAPI void hb_window_free(hb_window* win) {
    if (!win) return;
    if (win->hwnd) DestroyWindow(win->hwnd);
    delete win;
}

HB_WAPI int hb_window_valid(const hb_window* win) {
    return (win && win->hwnd) ? 1 : 0;
}

HB_WAPI long long hb_window_presented_frames(const hb_window* win) {
    return win ? win->presented : 0;
}

HB_WAPI void* hb_window_native(hb_window* win) {
    return win ? static_cast<void*>(win->hwnd) : nullptr;
}

HB_WAPI int hb_window_present(hb_window* win, const uint32_t* argb, int w, int h) {
    if (!hb_window_valid(win) || !argb || w <= 0 || h <= 0) return 0;

    BITMAPINFO bmi;
    std::memset(&bmi, 0, sizeof(bmi));
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = w;
    bmi.bmiHeader.biHeight = -h;                  /* ⚠ 负的 = **自上而下**，与我们的行序一致 */
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32;
    bmi.bmiHeader.biCompression = BI_RGB;

    /* ⚠ 不做换序：0x00RRGGBB 的内存字节序就是 DIB 要的 B,G,R,0（见文件头） */
    const int 行数 = StretchDIBits(win->dc, 0, 0, w, h, 0, 0, w, h,
                                   argb, &bmi, DIB_RGB_COLORS, SRCCOPY);
    /* ⚠ 用 `<= 0` 而不是 `== GDI_ERROR`：`GDI_ERROR` 是 `DWORD`（无符号），
     *   和 `int` 比会触发 -Wsign-compare（本仓 0 警告是门禁约定）。
     *   `StretchDIBits` 成功时返回**拷贝的行数** ⇒ 非正数就是失败。 */
    if (行数 <= 0) return 0;

    ++win->presented;
    return 1;
}

HB_WAPI int hb_window_pump(hb_window* win) {
    if (!win) return 0;
    MSG msg;
    while (PeekMessageW(&msg, nullptr, 0, 0, PM_REMOVE)) {
        if (msg.message == WM_QUIT) {
            win->quit = 1;
            break;
        }
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }
    return win->quit ? 0 : 1;
}

}  // extern "C"

#endif /* _WIN32 */
