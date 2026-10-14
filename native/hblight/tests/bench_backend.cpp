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
#elif defined(__unix__) || defined(__APPLE__)
#  include <sys/resource.h>
#endif

namespace {

/* ══════════════════════════════════════════════════════════════════════════
 * ⚠⚠ **等效核数**（task-17 加的）：进程 CPU 时间 ÷ 墙钟
 *
 *   为什么要它：任务书写的是"证明真的吃到了多个核"。
 *   而"代码里有 std::thread"**不等于**"实测用了多核"（本仓栽过好几次：
 *   构建 exit 0 但走的是另一条路）。逐核占用曲线也能看，但它受采样频率影响；
 *   **总共花了多少 CPU 时间**这个口径更硬。
 *
 * ⚠⚠ 而**窗口必须够长**（Lead 2026-10-14 实测的教训）：
 *   他在短窗口（0.013 s / 0.032 s）下量出 **0.000 / 0.487** 核 ——
 *   因为 `GetProcessTimes` 的分辨率是 **15.625 ms**（一个时钟刻度）：
 *       0.013 s 的窗口里 CPU 读数只有 0.0000 ⇒ 报 0.000 核（**看着完全正常，不会报错**）
 *   ⇒ 长窗口（本程序默认目标 **2 秒**墙钟）才量得准。
 *   ⇒ 所以下面 `--秒` 的默认值是 2.0，而且**低于 1 秒会显式警告**。
 * ══════════════════════════════════════════════════════════════════════════ */
double 进程CPU秒() {
#if defined(_WIN32)
    FILETIME 创, 退, 核, 用;
    if (!GetProcessTimes(GetCurrentProcess(), &创, &退, &核, &用)) return -1.0;
    ULARGE_INTEGER k, u;
    k.LowPart = 核.dwLowDateTime; k.HighPart = 核.dwHighDateTime;
    u.LowPart = 用.dwLowDateTime; u.HighPart = 用.dwHighDateTime;
    return static_cast<double>(k.QuadPart + u.QuadPart) / 1e7;   /* 100ns 单位 */
#elif defined(__unix__) || defined(__APPLE__)
    struct rusage ru;
    if (getrusage(RUSAGE_SELF, &ru) != 0) return -1.0;
    return static_cast<double>(ru.ru_utime.tv_sec + ru.ru_stime.tv_sec) +
           static_cast<double>(ru.ru_utime.tv_usec + ru.ru_stime.tv_usec) / 1e6;
#else
    return -1.0;   /* 拿不到就说拿不到 —— 不编一个数 */
#endif
}

/*: ⚠ 长窗口测量：**跑到墙钟 ≥ 目标秒数**，不是"固定帧数"。
 *  帧数固定时，负载一变窗口就跟着变 ⇒ 两次的口径会不可比（lead 踩过）。
 *  ⚠ 定义放在 `画一帧` 之后（它要用 场景/画一帧）。 */

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

/* ⚠ 长窗口测量（定义放这里：要用 场景 / 画一帧 / hb_fb_checksum）────────────
 *   跑到**墙钟 ≥ 目标秒**为止 —— 不是"固定帧数"：帧数固定时负载一变窗口就变，
 *   两次的口径就不可比了。 */
struct 长窗口 {
    int 帧数 = 0;
    double 墙钟秒 = 0;
    double CPU秒 = 0;
    double 每帧ms = 0;
    double 等效核数 = 0;
    uint64_t 校验和 = 0;
};

长窗口 量长窗口(hb_fb* fb, const 场景& s, double 目标秒) {
    长窗口 出;
    if (目标秒 <= 0.0) return 出;
    for (int i = 0; i < 20; ++i) 画一帧(fb, s);      /* 预热：首触页/缓存不算钱 */
    const double cpu0 = 进程CPU秒();
    const auto t0 = std::chrono::steady_clock::now();
    auto 已过 = [&] {
        return std::chrono::duration<double>(std::chrono::steady_clock::now() - t0).count();
    };
    while (已过() < 目标秒) {
        画一帧(fb, s);
        ++出.帧数;
    }
    const auto t1 = std::chrono::steady_clock::now();
    const double cpu1 = 进程CPU秒();
    出.墙钟秒 = std::chrono::duration<double>(t1 - t0).count();
    出.CPU秒 = cpu1 - cpu0;
    出.每帧ms = 出.墙钟秒 * 1000.0 / static_cast<double>(出.帧数);
    出.等效核数 = 出.墙钟秒 > 0 ? 出.CPU秒 / 出.墙钟秒 : 0.0;
    出.校验和 = hb_fb_checksum(fb);
    return 出;
}

/* ══════════════════════════════════════════════════════════════════════════
 * ⭐ task-17 的对照台：**大图元场景**（并行真正该赚钱的地方）
 *
 * ⚠ 为什么另立一个场景：目标场景（1000 个 r=9 的小圆）整帧才 ~110 微秒，
 *   每个圆的填充只有几百纳秒 —— 一次并行派发的开销（微秒级）就吃掉整帧。
 *   ⇒ 那个场景**注定**并行不赚（照实报）。
 *   而"大填充"（整屏矩形 / 大圆）是**逐行独立 + 每行很长**的形态，
 *   那才是这个改造能证明自己的地方。
 *
 * ⚠ z 序：三个图元仍然**按顺序**提交（清屏 → 两个大圆 → 全屏矩形），
 *   只在**每个图元内部**并行 ⇒ 覆盖顺序与串行完全一致 ⇒ 校验和必须相同。
 * ══════════════════════════════════════════════════════════════════════════ */
const int 大宽 = 1920;
const int 大高 = 1080;

void 画一大帧(hb_fb* fb) {
    hb_fb_clear(fb, 0x00112233u);
    hb_fb_fill_circle(fb, 700.0, 540.0, 600.0, 0x00E5484Du);
    hb_fb_fill_circle(fb, 1300.0, 540.0, 600.0, 0x0030A46Cu);
    hb_fb_fill_rect(fb, 0.0, 0.0, 1920.0, 200.0, 0x00FFD60Au);
}

长窗口 量大窗口(hb_fb* fb, double 目标秒) {
    长窗口 出;
    if (目标秒 <= 0.0) return 出;
    for (int i = 0; i < 10; ++i) 画一大帧(fb);
    const double cpu0 = 进程CPU秒();
    const auto t0 = std::chrono::steady_clock::now();
    auto 已过 = [&] {
        return std::chrono::duration<double>(std::chrono::steady_clock::now() - t0).count();
    };
    while (已过() < 目标秒) {
        画一大帧(fb);
        ++出.帧数;
    }
    const auto t1 = std::chrono::steady_clock::now();
    const double cpu1 = 进程CPU秒();
    出.墙钟秒 = std::chrono::duration<double>(t1 - t0).count();
    出.CPU秒 = cpu1 - cpu0;
    出.每帧ms = 出.墙钟秒 * 1000.0 / static_cast<double>(出.帧数);
    出.等效核数 = 出.墙钟秒 > 0 ? 出.CPU秒 / 出.墙钟秒 : 0.0;
    出.校验和 = hb_fb_checksum(fb);
    return 出;
}

/* ══════════════════════════════════════════════════════════════════════════
 * ⭐ 逐位一致**扫描**（task-17 的主判据，比"两个场景"厚得多的证据）
 *
 *   对一组**各种尺寸**的填充（小到 3×3，大到整屏），每一个都在
 *   `线程=1` 与 `线程=自动` 两种走法下各画一遍，比较整帧校验和。
 *   ⚠ 尺寸表**故意跨过并行门槛**（1M 像素）：这样"串行那条路"和"并行那条路"
 *     都被扫到 —— 否则只测小图元就永远测不到并行，那是"判据空转"。
 *   ⚠ 每一例都同时报 `工作量`，读者能看出它走的是哪条路（>1M 才可能并行）。
 * ══════════════════════════════════════════════════════════════════════════ */
struct 扫描结果 {
    int 例数 = 0;
    int 不一致 = 0;
    int 并行例数 = 0;      /*: 其中"真的派发过"的例数（否则并行路没被扫到） */
};

扫描结果 扫描逐位一致(hb_fb* fb) {
    扫描结果 出;
    const double 半径表[] = {4.0, 9.0, 33.0, 100.0, 260.0, 520.0, 900.0};
    const int 矩形表[][2] = {{3, 3}, {64, 64}, {400, 300}, {1920, 200}, {1920, 1080}};
    /*: ⚠⚠ 必须在**大画布**上扫：并行门槛是"单次填充 ≥ 1M 像素"，
     *  而在 800×450 上**最大的可能填充只有 360000 像素** ⇒ 所有例子都会走串行
     *  ⇒ 那个"逐位相同"是**空转的**（两边都在串行，当然相同）。
     *  ⚠ 第一版就是这么写的，被 `SWEEP_PARALLEL_CASES=0` 这条自检当场抓住
     *  （见 main 里的 SWEEP_VERDICT）—— 这正是它存在的理由。 */
    const int 宽画 = hb_fb_width(fb), 高画 = hb_fb_height(fb);
    const double cx = 宽画 / 2.0, cy = 高画 / 2.0;

    for (double r : 半径表) {
        hb_raster_set_threads(1);
        hb_fb_clear(fb, 背景);
        hb_fb_fill_circle(fb, cx, cy, r, 0x00E5484Du);
        const uint64_t 串 = hb_fb_checksum(fb);
        hb_raster_set_threads(0);
        hb_fb_clear(fb, 背景);
        /*: ⚠ 只看**这一笔填充**有没有派发 —— 清屏本身（1920×1080 = 2.07M 像素）
         *  不管圆的半径多小都会派发；把清屏算进去 ⇒ **12/12 例全是"并行例"**，
         *  那条自检就再也分不出"小圆应该走串行"（实测抓到过）。 */
        const long long 圆派前 = hb_raster_parallel_dispatches();
        hb_fb_fill_circle(fb, cx, cy, r, 0x00E5484Du);
        const uint64_t 并 = hb_fb_checksum(fb);
        if (hb_raster_parallel_dispatches() != 圆派前) ++出.并行例数;
        ++出.例数;
        if (串 != 并) ++出.不一致;
        std::printf("SWEEP circle r=%.0f  serial=0x%016llX parallel=0x%016llX %s\n", r,
                    static_cast<unsigned long long>(串),
                    static_cast<unsigned long long>(并), 串 == 并 ? "SAME" : "**DIFF**");
    }
    for (const auto& 矩 : 矩形表) {
        const double 宽矩 = 矩[0], 高矩 = 矩[1];
        hb_raster_set_threads(1);
        hb_fb_clear(fb, 背景);
        hb_fb_fill_rect(fb, 10.0, 10.0, 宽矩, 高矩, 0x0030A46Cu);
        const uint64_t 串 = hb_fb_checksum(fb);
        hb_raster_set_threads(0);
        hb_fb_clear(fb, 背景);
        const long long 矩派前 = hb_raster_parallel_dispatches();
        hb_fb_fill_rect(fb, 10.0, 10.0, 宽矩, 高矩, 0x0030A46Cu);
        const uint64_t 并 = hb_fb_checksum(fb);
        if (hb_raster_parallel_dispatches() != 矩派前) ++出.并行例数;
        ++出.例数;
        if (串 != 并) ++出.不一致;
        std::printf("SWEEP rect %.0fx%.0f  serial=0x%016llX parallel=0x%016llX %s\n",
                    宽矩, 高矩, static_cast<unsigned long long>(串),
                    static_cast<unsigned long long>(并), 串 == 并 ? "SAME" : "**DIFF**");
    }
    return 出;
}

}  // namespace

int main(int argc, char** argv) {
    int 预热 = 20;
    int 测量 = 200;
    bool 开窗 = false;
    /*: 长窗口目标（秒）。⚠ 默认 2.0 —— 理由见 `进程CPU秒()` 上面那段：
     *  `GetProcessTimes` 的分辨率是 15.625 ms，短窗口会报出**看着正常**的假数。 */
    double 长窗口秒 = 2.0;
    /*: ⭐ task-17：同一进程内的"串行 vs 并行"对照窗口（也是 ≥1 秒，理由同上）。 */
    double AB秒 = 2.0;
    int 线程上限 = 0;              /*: 0 = 自动（硬件核数） */
    bool 做AB = true;
    for (int i = 1; i < argc; ++i) {
        if (std::strcmp(argv[i], "--窗口") == 0 || std::strcmp(argv[i], "--window") == 0) 开窗 = true;
        else if (std::strcmp(argv[i], "--秒") == 0 && i + 1 < argc) 长窗口秒 = std::atof(argv[++i]);
        else if (std::strcmp(argv[i], "--无长窗口") == 0) 长窗口秒 = 0.0;
        else if (std::strcmp(argv[i], "--线程") == 0 && i + 1 < argc) 线程上限 = std::atoi(argv[++i]);
        else if (std::strcmp(argv[i], "--AB秒") == 0 && i + 1 < argc) AB秒 = std::atof(argv[++i]);
        else if (std::strcmp(argv[i], "--只圆") == 0) 做AB = false;
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

    /* ── ⭐ 长窗口：等效核数（task-17）────────────────────────────────────
     *   ⚠ 窗口太短会报出"看着正常"的假数（`GetProcessTimes` 分辨率 15.625 ms）：
     *     实测 0.013 s 的窗口读出 0.0000 s CPU ⇒ 报 0.000 核。
     *   ⇒ 这里默认跑满 2 秒墙钟，并把帧数/墙钟/CPU 三个数**一起打出来**
     *     （只报一个比值的话，读者没法判断窗口够不够长）。 */
    if (长窗口秒 > 0.0) {
        const long long 派发前 = hb_raster_parallel_dispatches();
        长窗口 lw = 量长窗口(fb, s, 长窗口秒);
        const long long 派发 = hb_raster_parallel_dispatches() - 派发前;
        std::printf("LONG_WINDOW_TARGET_SEC=%.3f\n", 长窗口秒);
        std::printf("LONG_FRAMES=%d\n", lw.帧数);
        std::printf("LONG_WALL_SEC=%.4f\n", lw.墙钟秒);
        std::printf("LONG_CPU_SEC=%.4f\n", lw.CPU秒);
        std::printf("LONG_EQUIV_CORES=%.3f\n", lw.等效核数);
        std::printf("LONG_MS_PER_FRAME=%.4f\n", lw.每帧ms);
        std::printf("LONG_CHECKSUM=0x%016llX\n", static_cast<unsigned long long>(lw.校验和));
        std::printf("LONG_CHECKSUM_MATCHES=%d\n", lw.校验和 == 和1 ? 1 : 0);
        /*: ⭐ task-17：**目标场景一次并行都没派发**才对 —— 它每帧最大的单次填充
         *  只有 360000 像素（清屏），低于 1M 门槛 ⇒ 走的还是原来那条串行路。
         *  ⚠ 这条是"没有回归"的**证据**（不是推理）：派发次数 0 ⇒ 那段热路径
         *    一行都没变；而 ms/帧 的前后对比在 [A/B] 段里并排给出。 */
        std::printf("LONG_WINDOW_DISPATCHES=%lld\n", 派发);
        std::printf("LONG_WINDOW_THREADS=%d\n", hb_raster_threads());
        /*: ⚠ 窗口太短的**显式警告**：本仓的规矩是"不许给看着正常的假数"。 */
        if (lw.墙钟秒 < 1.0) {
            std::printf("WARN=window_too_short(%.3fs<1s) equiv_cores_unreliable\n", lw.墙钟秒);
        }
    }

    /* ══════════════════════════════════════════════════════════════════════
     * ⭐ [A/B] 同一进程内：**串行(线程=1) vs 并行(自动)**（task-17）
     *
     * ⚠ 为什么必须在**同一个二进制、同一个进程**里对照：
     *   两份构建/两次进程之间的差异（编译布局、CPU 频率、后台负载）会混进结论里，
     *   本仓在这上面栽过（"原型 2x、产品 1.2x"）。
     *   同一个进程里只切换 `hb_raster_set_threads()` ⇒ 除了线程数，别的都一样。
     * ⚠ 两个场景都测：
     *   · 目标场景（1000 个 r=9 小圆）—— 预期**并行不赚**（整帧才 ~110 µs）
     *   · 大图元场景（1920×1080，两个 r=600 大圆 + 全屏矩形）—— 预期**赚**
     * ⚠ 主判据：两个场景的**校验和必须与串行逐位相同**。
     * ══════════════════════════════════════════════════════════════════════ */
    bool AB过 = true;
    if (做AB) {
        hb_fb* 大fb = hb_fb_new(大宽, 大高);
        if (!大fb) {
            std::printf("AB_SKIPPED=framebuffer_alloc_failed\n");
            做AB = false;
        } else {
            std::printf("[A/B] serial(threads=1) vs parallel(auto), same process, %.1fs windows\n",
                        AB秒);
            std::printf("AB_WINDOW_SEC=%.3f\n", AB秒);

            /* ── ① 串行 ── */
            hb_raster_set_threads(1);
            const int 串行线程 = hb_raster_threads();
            const long long 串行派发前 = hb_raster_parallel_dispatches();
            长窗口 串圆 = 量长窗口(fb, s, AB秒);
            长窗口 串大 = 量大窗口(大fb, AB秒);
            const long long 串行派发 = hb_raster_parallel_dispatches() - 串行派发前;
            std::printf("AB_SERIAL_THREADS=%d\n", 串行线程);
            std::printf("AB_SERIAL_DISPATCHES=%lld\n", 串行派发);
            std::printf("AB_SERIAL_CIRCLES_MS_PER_FRAME=%.4f\n", 串圆.每帧ms);
            std::printf("AB_SERIAL_CIRCLES_EQUIV_CORES=%.3f\n", 串圆.等效核数);
            std::printf("AB_SERIAL_CIRCLES_CHECKSUM=0x%016llX\n",
                        static_cast<unsigned long long>(串圆.校验和));
            std::printf("AB_SERIAL_BIG_MS_PER_FRAME=%.4f\n", 串大.每帧ms);
            std::printf("AB_SERIAL_BIG_EQUIV_CORES=%.3f\n", 串大.等效核数);
            std::printf("AB_SERIAL_BIG_CHECKSUM=0x%016llX\n",
                        static_cast<unsigned long long>(串大.校验和));

            /* ── ② 并行 ── */
            hb_raster_set_threads(线程上限);
            const int 并行线程 = hb_raster_threads();
            const long long 并行派发前 = hb_raster_parallel_dispatches();
            长窗口 并圆 = 量长窗口(fb, s, AB秒);
            长窗口 并大 = 量大窗口(大fb, AB秒);
            const long long 并行派发 = hb_raster_parallel_dispatches() - 并行派发前;
            const int 参与者 = hb_raster_max_participants();
            std::printf("AB_PARALLEL_THREADS=%d\n", 并行线程);
            std::printf("AB_PARALLEL_DISPATCHES=%lld\n", 并行派发);
            std::printf("AB_MAX_PARTICIPANTS=%d\n", 参与者);
            std::printf("AB_PARALLEL_CIRCLES_MS_PER_FRAME=%.4f\n", 并圆.每帧ms);
            std::printf("AB_PARALLEL_CIRCLES_EQUIV_CORES=%.3f\n", 并圆.等效核数);
            std::printf("AB_PARALLEL_CIRCLES_CHECKSUM=0x%016llX\n",
                        static_cast<unsigned long long>(并圆.校验和));
            std::printf("AB_PARALLEL_BIG_MS_PER_FRAME=%.4f\n", 并大.每帧ms);
            std::printf("AB_PARALLEL_BIG_EQUIV_CORES=%.3f\n", 并大.等效核数);
            std::printf("AB_PARALLEL_BIG_CHECKSUM=0x%016llX\n",
                        static_cast<unsigned long long>(并大.校验和));

            /* ── ③ 判据：逐位相同 + 提速 ── */
            const bool 圆一致 = (串圆.校验和 == 并圆.校验和);
            const bool 大一致 = (串大.校验和 == 并大.校验和);
            /*: ⚠ 串行那一轮**必须真的没派发过**（否则"串行"是假的，
             *  这个对照台自己就假绿了 —— 本仓的规矩：断言"走的是哪条路"）。 */
            const bool 串行真串行 = (串行派发 == 0);
            /*: ⚠ 并行那一轮**必须真的派发过**（否则等于没并行，白测）。 */
            const bool 并行真并行 = (并行派发 > 0);
            const double 圆提速 = 串圆.每帧ms / (并圆.每帧ms > 0 ? 并圆.每帧ms : 1e-9);
            const double 大提速 = 串大.每帧ms / (并大.每帧ms > 0 ? 并大.每帧ms : 1e-9);
            std::printf("AB_SERIAL_REALLY_SERIAL=%d\n", 串行真串行 ? 1 : 0);
            std::printf("AB_PARALLEL_REALLY_PARALLEL=%d\n", 并行真并行 ? 1 : 0);
            std::printf("AB_CIRCLES_CHECKSUM_IDENTICAL=%d\n", 圆一致 ? 1 : 0);
            std::printf("AB_BIG_CHECKSUM_IDENTICAL=%d\n", 大一致 ? 1 : 0);
            std::printf("AB_CIRCLES_SPEEDUP=%.3f\n", 圆提速);
            std::printf("AB_BIG_SPEEDUP=%.3f\n", 大提速);
            AB过 = 圆一致 && 大一致 && 串行真串行 && 并行真并行;
            std::printf("AB_VERDICT=%s\n", AB过 ? "PASS" : "FAIL");

            hb_fb_free(大fb);
        }
    }


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

    /* ══ ⭐ [SWEEP] 逐位一致扫描（主判据的加厚证据）══ */
    int 扫描不一致 = 0;
    int 扫描例数 = 0;
    int 扫描并行例数 = 0;
    if (做AB) {
        hb_fb* sfb = hb_fb_new(大宽, 大高);      /* ⚠ **大画布**：见 扫描逐位一致 的说明 */
        if (sfb) {
            std::printf("[SWEEP] bit-identical checksum sweep on %dx%d (serial vs parallel)\n",
                        大宽, 大高);
            const 扫描结果 sw = 扫描逐位一致(sfb);
            扫描不一致 = sw.不一致;
            扫描例数 = sw.例数;
            扫描并行例数 = sw.并行例数;
            std::printf("SWEEP_CASES=%d\n", sw.例数);
            std::printf("SWEEP_MISMATCHES=%d\n", sw.不一致);
            /*: ⚠ 这一条防"判据空转"：扫描表**必须**跨过并行门槛，
             *  否则"逐位相同"只是因为**两边都在走串行**。 */
            std::printf("SWEEP_PARALLEL_CASES=%d\n", sw.并行例数);
            std::printf("SWEEP_VERDICT=%s\n",
                        (sw.不一致 == 0 && sw.并行例数 > 0) ? "PASS" : "FAIL");
            hb_fb_free(sfb);
        }
    }
    const bool 扫描过 = (扫描不一致 == 0) && (扫描例数 == 0 || 扫描并行例数 > 0);

    hb_fb_free(fb);
    /*: ⚠ 退出码要求**两件事都成立**：
     *   ① 目标场景达标（原判据，一个字没放松）
     *   ② task-17 的对照：并行前后**校验和逐位相同**，而且"串行那轮真的没并行、
     *      并行那轮真的派发过"（否则这个对照台自己就会假绿）。 */
    const bool 全过 = 达标 && AB过 && 扫描过;
    std::printf("EXIT_REASON=%s\n",
                !达标 ? "target_scene_not_met"
                      : (!AB过 ? "parallel_ab_failed"
                               : (!扫描过 ? "sweep_not_bit_identical" : "ok")));
    return 全过 ? 0 : 1;
}
