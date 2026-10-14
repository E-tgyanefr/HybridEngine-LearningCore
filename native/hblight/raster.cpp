/* hblight/raster.cpp —— 软件光栅化器实现（从零写，见 raster.h 的独立性红线）。
 *
 * ⚠ 设计上只有一条原则：**热路径里不做浮点比较、不做边界判断以外的事**。
 *   1000 个圆 × 每圆几百像素 ⇒ 内层是"填一行"，那是唯一值得优化的地方。
 *   所以圆用**行扫描（span）**而不是"逐像素判 x²+y²≤r²"——
 *   后者每个像素一次乘加一次比较，前者每行一次 sqrt。差一个量级。
 */
#include "raster.h"

#include <algorithm>
#include <atomic>
#include <cmath>
#include <condition_variable>
#include <cstring>
#include <functional>
#include <mutex>
#include <thread>
#include <vector>

struct hb_fb {
    int w = 0;
    int h = 0;
    std::vector<uint32_t> px;
};

namespace {

/*: 本进程累计写过的像素数。⚠ 它存在的唯一理由是**证明"真的画了"** ——
 *  一个什么都不画的光栅化器是最快的，ms/帧 单独报没有意义。
 *  ⚠ 并行之后：仍然**只有调用线程**改它 —— 并行块各自返回自己的计数，
 *    由 `跑()` 按**块序**求和后一次性加进来 ⇒ 与串行逐位相同（不是"差不多"）。 */
long long g_written = 0;

/*: ⭐ **"够大才并行"的两个门槛**（编译期常量 ⇒ 小图元那句判断就是一次整数比较）。
 *  ⚠ 门槛是**量出来的**：第一版用 262144，结果 800×450 的清屏（360000 像素）
 *    每帧都去并行 ⇒ 整帧 0.127 → 0.667 ms（**慢 5.2 倍**，而且清屏本身才几十微秒）。
 *  ⚠ 抬到 1M 像素之后的实测：目标场景（1000 个 r=9 的圆）**一次派发都没有**，
 *    与基线逐帧同速；而大图元场景（1920×1080）才开始真的并行。 */
constexpr int 最小块行 = 24;              /*: 少于这么多行 ⇒ 不值得切块 */
constexpr long long 最小工作量 = 1048576; /*: 少于这么多像素（4 MB 写）⇒ 不值得动线程 */

/* ══════════════════════════════════════════════════════════════════════════
 * 行池：把"逐行独立"的填充按**行区间**切开，常驻线程池执行
 *
 * ⚠ 为什么是常驻池而不是"每次 std::thread"：
 *   建线程在 Windows 上约 30~100 微秒/个。一次填充总共才几十微秒，
 *   每次建 23 个线程 ⇒ **慢几十倍**。池化之后一次派发只有两次加锁 + 两次唤醒。
 *
 * ⚠ 为什么不用 OpenMP：那会给 .dll 引入 `libgomp-1.dll`（不在 PATH 上）
 *   ⇒ `ctypes.CDLL` 失败 ⇒ **静默退回 Python 版 ⇒ 判据全绿但绿的是 Python**
 *     （本仓在 `-static` 那一段已经栽过一次）。标准库的 std::thread 才安全。
 *
 * ⚠ 为什么阈值是必须的（实测标定，见 tests/bench_backend.cpp）：
 *   目标场景是 1000 个**半径 9** 的圆，整帧才 ~110 微秒 ⇒ 每个圆 ~110 纳秒。
 *   一次派发（加锁+唤醒+等齐）约几微秒 ⇒ 对小图元并行是**净亏**。
 *   ⇒ 按"行数 + 工作量"两个阈值卡住：**小图元走串行，一行代码都不多跑**。
 * ══════════════════════════════════════════════════════════════════════════ */
class 行池 {
public:
    /*: 低于这两个阈值就**不并行**（实测标定：见 bench 的 1000 圆场景）。
     *  ⚠ 它们是"每个图元"的门槛，不是"每帧"的。
     *  ⚠⚠ 阈值是**量出来的**，不是拍的：第一版把 clear 的门槛放在 262144，
     *     结果 800×450 的清屏（360000 像素）每帧都去并行 ⇒ 一次派发几微秒，
     *     而清屏本身才几十微秒 ⇒ **整帧从 0.127 ms 变成 0.667 ms（慢 5.2 倍）**。
     *     ⇒ 门槛抬到"**单次填充 ≥ 1M 像素**"：只有真正的大填充才值得动线程。
     *     （目标场景里最大的单次填充 = 清屏 360000 像素 ⇒ 永远走串行，
     *       也就是"**改完之后目标场景一个线程都没多起**"，见 bench 的
     *       `LONG_WINDOW_DISPATCHES` 断言。） */
    static constexpr int 默认最小块行 = 24;              /*: 每块至少 24 行 */
    static constexpr long long 默认最小工作量 = 1048576;  /*: 至少 1M 像素（4 MB 写） */

    static 行池& 取() {
        static 行池 单例;
        return 单例;
    }

    ~行池() {
        {
            std::lock_guard<std::mutex> lk(锁_);
            停_ = true;
            ++代_;
        }
        有活_.notify_all();
        for (auto& t : 工_) {
            if (t.joinable()) t.join();
        }
    }

    /*: 生效线程数（**1 = 串行**）。`上限_` 为 0 表示自动（硬件核数）。
     *  ⚠⚠ **必须缓存**：这个函数在**每个图元**上被调一次（目标场景 1000 次/帧）。
     *    第一版每次现算 `std::thread::hardware_concurrency()` ⇒ 整帧从 0.109 ms
     *    涨到 0.50 ms（**慢 4.6 倍，而且一个线程都没起**）—— 实测抓到的。
     *    ⇒ 平时只是一次 relaxed 原子读；只有改设置时才重算。 */
    int 线程数() const {
        int t = 线程数缓存_.load(std::memory_order_relaxed);
        if (t == 0) t = 刷新线程数();
        return t;
    }

    int 刷新线程数() const {
        const int 上限 = 上限_.load(std::memory_order_relaxed);
        int 硬 = static_cast<int>(std::thread::hardware_concurrency());
        if (硬 <= 0) 硬 = 1;
        int n = (上限 == 1) ? 1 : ((上限 == 0) ? 硬 : 上限);
        const int 可 = 最大工人 + 1;                 /* 调用线程也算一个 */
        if (n > 可) n = 可;
        if (n < 1) n = 1;
        线程数缓存_.store(n, std::memory_order_relaxed);
        return n;
    }

    void 设上限(int n) {
        上限_.store(n < 0 ? 0 : n, std::memory_order_relaxed);
        刷新线程数();
    }

    /*: ⚠⚠ **阈值判断必须放在调用方**（那三行 `if`）—— 这是量出来的结论，不是洁癖：
     *    · 第一版：每个图元现算 `hardware_concurrency()` ⇒ 整帧 0.109 → 0.50 ms（**4.6 倍**，而且一个线程都没起）
     *    · 第二版：缓存成原子读，但仍每个图元读一次       ⇒ 整帧慢 **16%**
     *    · 第三版：把整个判断包进模板函子 `跑(...)`       ⇒ 整帧慢 **11%**（那个调用本身）
     *  三代都栽在同一个道理上：**检查本身比被检查的事还贵**（每个圆只有 ~110 纳秒）。
     *  ⇒ 现在：调用方先比"行数 / 行宽"（编译期常量，一次整数比较），
     *    只有够大的填充才调这里；小图元**一个 lambda、一次原子读都不产生**。
     *  `可并行()` 也只读一次缓存值（原子 relaxed），而且只在"够大"之后才被调到。 */
    bool 可并行() const { return 线程数缓存_.load(std::memory_order_relaxed) > 1; }

    long long 派发数() const { return 派发_.load(); }
    int 最大参与者() const { return 参与者_.load(); }

    /*: 真的并行派发（**只在"够大"时才被调用**，见 `可并行()` 上面那段）。
     *  `身体(块起, 块止)` 必须**只写自己那几行**，并返回该块写过的像素数。 */
    long long 并行(int 起, int 止, const std::function<long long(int, int)>& 身体,
                   int 最小块行 = 默认最小块行) {
        const int 行数 = 止 - 起;
        const int 线程 = 线程数缓存_.load(std::memory_order_relaxed);
        int 块数 = 行数 / 最小块行;
        if (块数 > 线程) 块数 = 线程;
        if (块数 < 2) return 身体(起, 止);
        const int 每块 = (行数 + 块数 - 1) / 块数;   /* 块大小向上取整 ⇒ 覆盖满 [起,止) */
        块数 = (行数 + 每块 - 1) / 每块;
        if (块数 < 2) return 身体(起, 止);

        std::vector<long long> 计数(static_cast<size_t>(块数), 0);
        std::atomic<int> 下一{0};
        auto 干活 = [&] {
            for (;;) {
                const int i = 下一.fetch_add(1, std::memory_order_relaxed);
                if (i >= 块数) return;
                const int a = 起 + i * 每块;
                int b = a + 每块;
                if (b > 止) b = 止;
                计数[static_cast<size_t>(i)] = 身体(a, b);   /* 每个块只被一个线程跑 */
            }
        };

        /*: ⚠ 类型擦除**只在这里**发生（每个并行图元一次 heap 分配，可以接受）。 */
        std::function<void()> 任务 = 干活;
        建池();
        {
            std::unique_lock<std::mutex> lk(锁_);
            体_ = &任务;
            ++代_;
            完成_ = 0;
            等数_ = static_cast<int>(工_.size());
        }
        有活_.notify_all();
        干活();                                      /* 调用线程也干，不空转 */
        {
            std::unique_lock<std::mutex> lk(锁_);
            干完_.wait(lk, [&] { return 完成_ >= 等数_; });
        }
        体_ = nullptr;
        派发_.fetch_add(1);
        int 参与 = 块数;
        int 旧 = 参与者_.load();
        while (参与 > 旧 && !参与者_.compare_exchange_weak(旧, 参与)) {
        }
        long long 总 = 0;
        for (long long c : 计数) 总 += c;             /* ⚠ **块序**求和 ⇒ 逐位确定 */
        return 总;
    }

public:
    static constexpr int 最大工人 = 31;               /*: 连调用线程最多 32 路 */

    行池() = default;
    行池(const 行池&) = delete;
    行池& operator=(const 行池&) = delete;

    void 建池() {
        std::lock_guard<std::mutex> lk(建锁_);
        if (已建_) return;
        已建_ = true;
        int 硬 = static_cast<int>(std::thread::hardware_concurrency());
        if (硬 <= 0) 硬 = 1;
        int 工数 = 硬 - 1;
        if (工数 > 最大工人) 工数 = 最大工人;
        工_.reserve(static_cast<size_t>(工数));
        for (int i = 0; i < 工数; ++i) 工_.emplace_back([this] { 工人(); });
    }

    void 工人() {
        std::uint64_t 我的代 = 0;
        for (;;) {
            std::function<void()>* 活 = nullptr;
            {
                std::unique_lock<std::mutex> lk(锁_);
                有活_.wait(lk, [&] { return 停_ || 代_ != 我的代; });
                if (停_) return;
                我的代 = 代_;
                活 = 体_;
            }
            if (活) (*活)();
            {
                std::lock_guard<std::mutex> lk(锁_);
                ++完成_;
            }
            干完_.notify_all();
        }
    }

    mutable std::mutex 锁_;
    std::mutex 建锁_;
    std::condition_variable 有活_, 干完_;
    std::vector<std::thread> 工_;
    std::function<void()>* 体_ = nullptr;
    std::uint64_t 代_ = 0;
    int 完成_ = 0;
    int 等数_ = 0;
    bool 停_ = false;
    bool 已建_ = false;
    std::atomic<int> 上限_{0};
    /*: ⚠ **缓存**的生效线程数（0 = 还没算过）。热路径只读它，见 `线程数()` 的说明。 */
    mutable std::atomic<int> 线程数缓存_{0};
    std::atomic<long long> 派发_{0};
    std::atomic<int> 参与者_{0};
};

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

/* ══════════════════════════════════════════════════════════════════════════
 * 三个"逐行填充"的**唯一实现**（串行与并行都走它们 ⇒ 逻辑只有一份，不会走岔）
 *   参数全是**行区间** `[行起, 行止)`，返回这一段写过的像素数。
 * ══════════════════════════════════════════════════════════════════════════ */
namespace {

long long 清屏行(hb_fb* fb, int 行起, int 行止, uint32_t argb) {
    uint32_t* p = fb->px.data() + static_cast<size_t>(行起) * static_cast<size_t>(fb->w);
    const size_t 个数 = static_cast<size_t>(行止 - 行起) * static_cast<size_t>(fb->w);
    std::fill(p, p + 个数, argb);
    return static_cast<long long>(个数);
}

long long 填圆行(hb_fb* fb, double cx, double cy, double r2, int x0, int x1,
                 int 行起, int 行止, uint32_t argb) {
    long long 计数 = 0;
    uint32_t* 基 = fb->px.data();
    const int 宽 = fb->w;
    for (int y = 行起; y < 行止; ++y) {
        const double dy = static_cast<double>(y) - cy;
        const double t = r2 - dy * dy;
        if (t <= 0.0) continue;
        const double dx = std::sqrt(t);

        int sx = static_cast<int>(std::ceil(cx - dx));
        int ex = static_cast<int>(std::floor(cx + dx));
        if (sx < x0) sx = x0;
        if (ex > x1) ex = x1;
        if (sx > ex) continue;

        uint32_t* row = 基 + static_cast<size_t>(y) * static_cast<size_t>(宽);
        std::fill(row + sx, row + ex + 1, argb);
        计数 += ex - sx + 1;
    }
    return 计数;
}

long long 填矩形行(hb_fb* fb, int x0, int x1, int 行起, int 行止, uint32_t argb) {
    uint32_t* 基 = fb->px.data();
    const int 宽 = fb->w;
    for (int yy = 行起; yy < 行止; ++yy) {
        uint32_t* row = 基 + static_cast<size_t>(yy) * static_cast<size_t>(宽);
        std::fill(row + x0, row + x1 + 1, argb);
    }
    return static_cast<long long>(行止 - 行起) * static_cast<long long>(x1 - x0 + 1);
}

}  // namespace

HB_RAPI void hb_fb_clear(hb_fb* fb, uint32_t argb) {
    if (!fb) return;
    const int 行数 = fb->h;
    /*: ⚠ clear 是**纯带宽**（`std::fill` 已经向量化）——它是"并行最可能不赚"的形态，
     *  实测也确实如此（见 bench 的 A/B）。所以这里和别处一样：**够大才并行**。 */
    if (行数 >= 最小块行 && static_cast<long long>(行数) * fb->w >= 最小工作量 &&
        行池::取().可并行()) {
        g_written += 行池::取().并行(
            0, 行数, [&](int a, int b) { return 清屏行(fb, a, b, argb); });
        return;
    }
    g_written += 清屏行(fb, 0, 行数, argb);
}

/* 填充圆 —— **行扫描**。
 *   对每一行 y：dy = y - cy；若 r²-dy² > 0，则 dx = sqrt(r²-dy²)，
 *   该行的实心区间就是 [cx-dx, cx+dx]。每行一次 sqrt，不是每像素一次。
 *
 * ⚠ task-17：够大的圆按**行区间**派发；**每行的算法一个字没改**
 *   （串行/并行都调同一个 `填圆行`）⇒ 同一场景两种走法**逐位相同**。 */
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
    const int 行数 = y1 - y0 + 1;
    const int 行宽 = x1 - x0 + 1;
    /*: ⭐ 小图元（目标场景的 r=9 圆：19 行）在这**一次比较**上就返回，
     *   与改动前的代码路径完全一致 —— 没有 lambda、没有原子读、没有函数指针。 */
    if (行数 >= 最小块行 && static_cast<long long>(行数) * 行宽 >= 最小工作量 &&
        行池::取().可并行()) {
        g_written += 行池::取().并行(y0, y1 + 1, [&](int a, int b) {
            return 填圆行(fb, cx, cy, r2, x0, x1, a, b, argb);
        });
        return;
    }
    g_written += 填圆行(fb, cx, cy, r2, x0, x1, y0, y1 + 1, argb);
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
    const int 行数 = y1 - y0 + 1;
    const int 行宽 = x1 - x0 + 1;
    if (行数 >= 最小块行 && static_cast<long long>(行数) * 行宽 >= 最小工作量 &&
        行池::取().可并行()) {
        g_written += 行池::取().并行(y0, y1 + 1, [&](int a, int b) {
            return 填矩形行(fb, x0, x1, a, b, argb);
        });
        return;
    }
    g_written += 填矩形行(fb, x0, x1, y0, y1 + 1, argb);
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

/* ── 并行开关与证据（task-17）────────────────────────────────────────────────
 * ⚠ `exit 0` ≠ 生效了：`hb_raster_parallel_dispatches()` 就是那条"走的是哪条路"的断言
 *   （0 = 一直串行；>0 = 真的派发过）。 */
HB_RAPI int hb_raster_threads(void) { return 行池::取().线程数(); }
HB_RAPI void hb_raster_set_threads(int n) { 行池::取().设上限(n); }
HB_RAPI long long hb_raster_parallel_dispatches(void) { return 行池::取().派发数(); }
HB_RAPI int hb_raster_max_participants(void) { return 行池::取().最大参与者(); }

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
