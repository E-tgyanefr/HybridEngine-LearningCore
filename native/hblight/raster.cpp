/* hblight/raster.cpp —— 软件光栅化器实现（从零写，见 raster.h 的独立性红线）。
 *
 * ⚠ 设计上只有一条原则：**热路径里不做浮点比较、不做边界判断以外的事**。
 *   1000 个圆 × 每圆几百像素 ⇒ 内层是"填一行"，那是唯一值得优化的地方。
 *   所以圆用**行扫描（span）**而不是"逐像素判 x²+y²≤r²"——
 *   后者每个像素一次乘加一次比较，前者每行一次 sqrt。差一个量级。
 */
#include "raster.h"

#include <algorithm>
#include <cmath>
#include <cstring>
#include <vector>

struct hb_fb {
    int w = 0;
    int h = 0;
    std::vector<uint32_t> px;
};

namespace {

/*: 本进程累计写过的像素数。⚠ 它存在的唯一理由是**证明"真的画了"** ——
 *  一个什么都不画的光栅化器是最快的，ms/帧 单独报没有意义。 */
long long g_written = 0;

}  // namespace

extern "C" {

HB_RAPI hb_fb* hb_fb_new(int w, int h) {
    if (w <= 0 || h <= 0) return nullptr;
    // ⚠ 上限保护：万一把宽高弄反或传了个负的，别去 new 一块 4 GB 的东西。
    if (static_cast<long long>(w) * h > 64LL * 1024 * 1024) return nullptr;
    hb_fb* fb = new hb_fb();
    fb->w = w;
    fb->h = h;
    fb->px.assign(static_cast<size_t>(w) * static_cast<size_t>(h), 0u);
    return fb;
}

HB_RAPI void hb_fb_free(hb_fb* fb) { delete fb; }
HB_RAPI int hb_fb_width(const hb_fb* fb) { return fb ? fb->w : 0; }
HB_RAPI int hb_fb_height(const hb_fb* fb) { return fb ? fb->h : 0; }
HB_RAPI uint32_t* hb_fb_pixels(hb_fb* fb) { return fb ? fb->px.data() : nullptr; }

HB_RAPI size_t hb_fb_bytes(const hb_fb* fb) {
    return fb ? fb->px.size() * sizeof(uint32_t) : 0u;
}

HB_RAPI void hb_fb_clear(hb_fb* fb, uint32_t argb) {
    if (!fb) return;
    std::fill(fb->px.begin(), fb->px.end(), argb);
    g_written += static_cast<long long>(fb->px.size());
}

/* 填充圆 —— **行扫描**。
 *   对每一行 y：dy = y - cy；若 r²-dy² > 0，则 dx = sqrt(r²-dy²)，
 *   该行的实心区间就是 [cx-dx, cx+dx]。每行一次 sqrt，不是每像素一次。 */
HB_RAPI void hb_fb_fill_circle(hb_fb* fb, double cx, double cy, double r, uint32_t argb) {
    if (!fb || !(r > 0.0)) return;

    int x0 = static_cast<int>(std::floor(cx - r));
    int x1 = static_cast<int>(std::ceil(cx + r));
    int y0 = static_cast<int>(std::floor(cy - r));
    int y1 = static_cast<int>(std::ceil(cy + r));
    if (x0 < 0) x0 = 0;
    if (y0 < 0) y0 = 0;
    if (x1 > fb->w - 1) x1 = fb->w - 1;
    if (y1 > fb->h - 1) y1 = fb->h - 1;
    if (x0 > x1 || y0 > y1) return;          /* 完全在屏外 ⇒ 一个像素都不写 */

    const double r2 = r * r;
    for (int y = y0; y <= y1; ++y) {
        const double dy = static_cast<double>(y) - cy;
        const double t = r2 - dy * dy;
        if (t <= 0.0) continue;
        const double dx = std::sqrt(t);

        int sx = static_cast<int>(std::ceil(cx - dx));
        int ex = static_cast<int>(std::floor(cx + dx));
        if (sx < x0) sx = x0;
        if (ex > x1) ex = x1;
        if (sx > ex) continue;

        uint32_t* row = fb->px.data() + static_cast<size_t>(y) * static_cast<size_t>(fb->w);
        std::fill(row + sx, row + ex + 1, argb);
        g_written += static_cast<long long>(ex - sx + 1);
    }
}

HB_RAPI void hb_fb_fill_rect(hb_fb* fb, double x, double y, double w, double h, uint32_t argb) {
    if (!fb || !(w > 0.0) || !(h > 0.0)) return;
    int x0 = static_cast<int>(std::floor(x));
    int y0 = static_cast<int>(std::floor(y));
    int x1 = static_cast<int>(std::ceil(x + w)) - 1;
    int y1 = static_cast<int>(std::ceil(y + h)) - 1;
    if (x0 < 0) x0 = 0;
    if (y0 < 0) y0 = 0;
    if (x1 > fb->w - 1) x1 = fb->w - 1;
    if (y1 > fb->h - 1) y1 = fb->h - 1;
    if (x0 > x1 || y0 > y1) return;
    for (int yy = y0; yy <= y1; ++yy) {
        uint32_t* row = fb->px.data() + static_cast<size_t>(yy) * static_cast<size_t>(fb->w);
        std::fill(row + x0, row + x1 + 1, argb);
        g_written += static_cast<long long>(x1 - x0 + 1);
    }
}

/* Bresenham 直线。⚠ 只为了"接口完整"存在 —— 轻量版的主要图元是圆，
 *   真正的教学曲线/线段由调用方决定。粗线用方形笔刷近似。 */
HB_RAPI void hb_fb_line(hb_fb* fb, double x1, double y1, double x2, double y2,
                        uint32_t argb, double width) {
    if (!fb) return;
    int x = static_cast<int>(x1);
    int y = static_cast<int>(y1);
    const int xe = static_cast<int>(x2);
    const int ye = static_cast<int>(y2);
    const int dx = std::abs(xe - x);
    const int dy = -std::abs(ye - y);
    const int sx = x < xe ? 1 : -1;
    const int sy = y < ye ? 1 : -1;
    int err = dx + dy;
    int 半径 = static_cast<int>(width / 2.0);
    if (半径 < 0) 半径 = 0;

    for (;;) {
        if (半径 == 0) {
            if (x >= 0 && x < fb->w && y >= 0 && y < fb->h) {
                fb->px[static_cast<size_t>(y) * static_cast<size_t>(fb->w) + static_cast<size_t>(x)] = argb;
                ++g_written;
            }
        } else {
            hb_fb_fill_rect(fb, static_cast<double>(x - 半径), static_cast<double>(y - 半径),
                            static_cast<double>(2 * 半径 + 1), static_cast<double>(2 * 半径 + 1), argb);
        }
        if (x == xe && y == ye) break;
        const int e2 = 2 * err;
        if (e2 >= dy) { err += dy; x += sx; }
        if (e2 <= dx) { err += dx; y += sy; }
    }
}

HB_RAPI long long hb_fb_written_pixels(void) { return g_written; }
HB_RAPI void hb_fb_reset_stats(void) { g_written = 0; }

HB_RAPI uint64_t hb_fb_checksum(const hb_fb* fb) {
    if (!fb) return 0u;
    uint64_t h = 1469598103934665603ULL;                 /* FNV-1a 64 位偏移基准 */
    const unsigned char* p = reinterpret_cast<const unsigned char*>(fb->px.data());
    const size_t n = fb->px.size() * sizeof(uint32_t);
    for (size_t i = 0; i < n; ++i) {
        h ^= static_cast<uint64_t>(p[i]);
        h *= 1099511628211ULL;
    }
    return h;
}

HB_RAPI long long hb_fb_count_not(const hb_fb* fb, uint32_t argb) {
    if (!fb) return 0;
    long long n = 0;
    for (uint32_t v : fb->px) if (v != argb) ++n;
    return n;
}

}  // extern "C"
