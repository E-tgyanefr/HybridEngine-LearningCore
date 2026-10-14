/* bench_backend.cpp —— 打那个靶子：1000 个圆、铺满视野。
 *
 * ⚠⚠ 这个程序的设计要点：**ms/帧 必须和"到底写了多少像素"一起报**。
 *
 *   本轮已经吃过一次假绿（dll 静默退回 ⇒ 判据全绿但绿的是 Python 版）。
 *   性能 benchmark 更容易犯这个错：**一个什么都不画的光栅化器是最快的** ——
 *   如果只报 ms/帧，把 `hb_fb_fill_circle` 改成 `return;` 就能"达标"。
 *
 *   ⇒ 所以每一次测量都同时输出：
 *        WRITTEN_PIXELS  累计写过的像素数（0 = 什么都没画 ⇒ 这个数不算数）
 *        NONBG_PIXELS    画完之后与背景色不同的像素数（"覆盖了多少面积"）
 *        CHECKSUM        整帧 FNV-1a（两次同场景必须**逐位相同** ⇒ 确定性）
 *     三者对不上就把结论标成不可信。
 *
 * ⚠ 输出**全 ASCII**：Windows 控制台是 cp936/cp1252，C++ 里 `printf` 中文会乱码。
 *   中文说明放在 docs/轻量版C++渲染.md，不放在这里。
 */
#include "../raster.h"
#include "../window.h"

#include <chrono>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <string>
#include <vector>

#if defined(_WIN32)
#  include <windows.h>
#endif

namespace {

const int 宽 = 800;
const int 高 = 450;
const int 圆数 = 1000;
const double 半径 = 9.0;
const uint32_t 背景 = 0x00FFFFFFu;          /* 白 */

/*: 靶子：比 tkinter 的 4.855 ms/帧 快 2 倍 */
const double 靶子_ms = 2.43;

struct 场景 {
    std::vector<double> x, y, r;
    std::vector<uint32_t> 色;
};

/*: 1000 个圆铺满 800x450：40 列 x 25 行。
 *  ⚠ **确定性**：不用随机数，坐标是格点算出来的 ⇒ 两次跑必然一样。 */
场景 造场景() {
    场景 s;
    const int 列 = 40;
    const double 步x = static_cast<double>(宽) / 列;          /* 20 */
    const double 步y = static_cast<double>(高) / (圆数 / 列);  /* 18 */
    const uint32_t 调色[] = {0x00E5484Du, 0x0030A46Cu, 0x000091FFu, 0x00FFD60Au, 0x008E4EC6u};
    for (int i = 0; i < 圆数; ++i) {
        const int cx = i % 列;
        const int cy = i / 列;
        s.x.push_back(步x * cx + 步x / 2.0);
        s.y.push_back(步y * cy + 步y / 2.0);
        s.r.push_back(半径);
        s.色.push_back(调色[i % 5]);
    }
    return s;
}

void 画一帧(hb_fb* fb, const 场景& s) {
    hb_fb_clear(fb, 背景);
    for (size_t i = 0; i < s.x.size(); ++i) {
        hb_fb_fill_circle(fb, s.x[i], s.y[i], s.r[i], s.色[i]);
    }
}

/*: 期望覆盖面积（用于判"真的画上去了"）—— 圆的面积和，粗略即可，
 *  它只用来抓"一个像素都没写"这种量级的错，不做像素级断言。 */
long long 期望面积(const 场景& s) {
    long long n = 0;
    for (size_t i = 0; i < s.r.size(); ++i) {
        n += static_cast<long long>(3.14159265358979 * s.r[i] * s.r[i] * 0.90);
    }
    return n;
}

}  // namespace

int main(int argc, char** argv) {
    int 预热 = 20;
    int 测量 = 200;
    bool 开窗 = false;
    for (int i = 1; i < argc; ++i) {
        if (std::strcmp(argv[i], "--窗口") == 0 || std::strcmp(argv[i], "--window") == 0) 开窗 = true;
        else if (std::strcmp(argv[i], "--帧数") == 0 && i + 1 < argc) 测量 = std::atoi(argv[++i]);
    }

    std::printf("BACKEND=%s\n", hb_window_backend());
    std::printf("SCENE circles=%d size=%dx%d radius=%.1f\n", 圆数, 宽, 高, 半径);

    hb_fb* fb = hb_fb_new(宽, 高);
    if (!fb) {
        std::printf("FATAL=framebuffer alloc failed\n");
        return 2;
    }
    const 场景 s = 造场景();

    /* ── 预热 ── */
    for (int i = 0; i < 预热; ++i) 画一帧(fb, s);

    /* ── 正戏：计时 ── */
    hb_fb_reset_stats();
    const auto 起 = std::chrono::steady_clock::now();
    for (int i = 0; i < 测量; ++i) 画一帧(fb, s);
    const auto 止 = std::chrono::steady_clock::now();
    const double 总ms = std::chrono::duration<double, std::milli>(止 - 起).count();
    const double 每帧ms = 总ms / 测量;

    const long long 写过 = hb_fb_written_pixels();
    const long long 非背景 = hb_fb_count_not(fb, 背景);
    const uint64_t 和1 = hb_fb_checksum(fb);

    /* ── 确定性：同一场景再画一帧，校验和必须一样 ── */
    hb_fb_clear(fb, 背景);
    for (size_t i = 0; i < s.x.size(); ++i) hb_fb_fill_circle(fb, s.x[i], s.y[i], s.r[i], s.色[i]);
    const uint64_t 和2 = hb_fb_checksum(fb);

    std::printf("FRAMES warmup=%d measured=%d\n", 预热, 测量);
    std::printf("MS_PER_FRAME=%.4f\n", 每帧ms);
    std::printf("MS_TOTAL=%.2f\n", 总ms);
    std::printf("WRITTEN_PIXELS=%lld\n", 写过);
    std::printf("WRITTEN_PER_FRAME=%lld\n", 写过 / 测量);
    std::printf("NONBG_PIXELS=%lld\n", 非背景);
    std::printf("EXPECTED_AREA_MIN=%lld\n", 期望面积(s));
    std::printf("CHECKSUM=0x%016llX\n", static_cast<unsigned long long>(和1));
    std::printf("DETERMINISTIC=%d\n", 和1 == 和2 ? 1 : 0);
    std::printf("TARGET_MS=%.2f\n", 靶子_ms);

    /* ── 判据：三条都成立才算真的达标 ──
     *   ⚠ 少任何一条都会让"什么都不画"的实现蒙混过关。 */
    const bool 有效 = (写过 > 0) && (非背景 >= 期望面积(s)) && (和1 == 和2) && (和1 != 0);
    const bool 达标 = 有效 && (每帧ms <= 靶子_ms);
    std::printf("EFFECTIVE=%d\n", 有效 ? 1 : 0);
    std::printf("VERDICT=%s\n", 达标 ? "PASS" : (有效 ? "SLOW" : "INVALID"));

    /* ── 可选：真开窗，验证"上屏"这条路 ── */
    if (开窗) {
        hb_window* win = hb_window_new("hblight backend bench", 宽, 高);
        std::printf("WINDOW_VALID=%d\n", hb_window_valid(win) ? 1 : 0);
        if (hb_window_valid(win)) {
            const int 窗帧 = 120;
            const auto w起 = std::chrono::steady_clock::now();
            int 成功 = 0;
            for (int i = 0; i < 窗帧; ++i) {
                画一帧(fb, s);
                成功 += hb_window_present(win, hb_fb_pixels(fb), 宽, 高);
                if (!hb_window_pump(win)) break;
            }
            const auto w止 = std::chrono::steady_clock::now();
            const double wms = std::chrono::duration<double, std::milli>(w止 - w起).count();
            std::printf("WINDOW_PRESENTS=%d\n", 成功);
            std::printf("WINDOW_MS_PER_FRAME=%.4f\n", wms / 窗帧);

#if defined(_WIN32)
            /* ⚠ 验"像素格式不用换序"这条**断言**（不是推理）：
             *   取一个圆心处的窗口像素，看它等于 0x00RRGGBB
             *   还是它的 R/B 反序。两边都打出来 —— 哪个对由数据说。 */
            HWND hwnd = static_cast<HWND>(hb_window_native(win));
            if (hwnd) {
                HDC dc = GetDC(hwnd);
                const int px = static_cast<int>(s.x[0]);
                const int py = static_cast<int>(s.y[0]);
                const COLORREF got = GetPixel(dc, px, py);
                ReleaseDC(hwnd, dc);
                const uint32_t want = s.色[0];
                /* ⚠⚠ 这里有个**很容易误判**的地方，必须写清楚：
                 *   `GetPixel` 返回的是 `COLORREF`，它的通道顺序是 **0x00BBGGRR**，
                 *   和我们缓冲里的 **0x00RRGGBB** **正好相反**。
                 *   ⇒ 直接比 `got == want` 会得到"不相等"，看起来像画错了 ——
                 *     **其实画对了**（屏幕上那个像素就是 #E5484D）。
                 *   ⇒ 所以期望值要按 COLORREF 摆出来再比。 */
                const uint32_t 期望COLORREF = ((want & 0x0000FFu) << 16) |
                                              (want & 0x00FF00u) |
                                              ((want >> 16) & 0x0000FFu);
                std::printf("PIXEL_AT=%d,%d\n", px, py);
                std::printf("PIXEL_WANT_RGB=0x%06X\n", want);
                std::printf("PIXEL_EXPECTED_COLORREF=0x%06X\n", 期望COLORREF);
                std::printf("PIXEL_GOT_COLORREF=0x%06X\n", static_cast<unsigned>(got));
                std::printf("PIXEL_COLOR_CORRECT=%d\n",
                            static_cast<unsigned>(got) == 期望COLORREF ? 1 : 0);
            }
#endif
            hb_window_free(win);
        }
    }

    hb_fb_free(fb);
    return 达标 ? 0 : 1;
}
