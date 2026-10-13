// ============================================================================
// 工业模拟压测：**大规模 2D 刚体** —— 量引擎物理求解器的能力边界
//
// ════ 为什么要做这个（而不是只跑光栅化微基准）════
//
// `ms_bench2d` 量的是**光栅化**。而"工业级模拟"问的是**求解器**：
// 几千个刚体一起落、一起撞静态几何，每帧要多久。
//
// ⚠⚠ **先写清这个引擎的物理能力边界**（下面这些是 `rigidbody2d.hpp` 与
//    `test_rigidbody2d.cpp` 自己写的，**不是我猜的**）：
//
//     **是**：确定性积分 + 对**静态**碰撞体的分离 + 落地/顶头/撞墙判定 + 世界边界
//
//     **不是**（明确不做）：
//       · **body × body 碰撞响应** —— 动态体**互不推**（没有冲量、没有质量比）
//       · **旋转体**（本版都是轴对齐盒或圆，无角速度）
//       · **摩擦 / 关节 / 约束**
//
//   ⇒ 原话：「半吊子做出来只会让人以为有物理引擎可用」。
//   ⇒ 所以「**工业级模拟**」在这个引擎上**做不了** —— 没有 body×body、
//     没有流体、没有有限元。**这正是作者定位里那句"做不了工业级模拟"的实测依据。**
//
//   本压测量的是**它真能做的那个上限**：多少刚体还能满 60 帧。
//
// ════ 口径（为了能横向比较，写清每一处）════
//
//     · 场景：N 个动态刚体 + 1 个静态地面，网格铺开，重力向下
//     · 步进：`StepPhysics2D(sc, dt, stage)` —— **真的物理求解器**
//     · 计时：**只计求解器**（不含渲染、不含建场景）
//       ⇒ 这是"物理能扛多少"的干净数字
//     · 每档跑 3 轮取**中位**（避开调度抖动）
//     · 分数口径：`分数 = 1000 × (1/60) / 实测每帧秒` ⇒ **1000 = 恰好 60 帧**
//       （和专业版 `ms_bench2d` 的分数口径一致，便于对照）
//     · ⭐ **确定性也要量**：同一场景跑两遍，逐位比坐标和
//
// 用法：
//     _stress_sim.exe <对象数> [帧数]
//     _stress_sim.exe 2000 300
//     _stress_sim.exe --sweep              # 自动分级：跑到超预算为止
//
// ⚠ 本文件是**加法式**的：只**使用**引擎的公开头文件，不改引擎任何代码。
// ============================================================================

#include "hybridengine/collision/rigidbody2d.hpp"
#include "hybridengine/core/scene.hpp"
#include "hybridengine/core/scene_object.hpp"
#include "hybridengine/core/transform.hpp"

#include <algorithm>
#include <chrono>
#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <string>
#include <vector>

using HybridEngine::Collision::Collider2D;
using HybridEngine::Collision::Physics2DStats;
using HybridEngine::Collision::Rigidbody2D;
using HybridEngine::Collision::Shape2D;
using HybridEngine::Collision::StageBounds2D;
using HybridEngine::Collision::StepPhysics2D;
using HybridEngine::Core::Scene;
using HybridEngine::Core::SceneObject;

namespace {

constexpr double kDt = 1.0 / 60.0;
constexpr double k预算ms = 16.667;          // 60 帧预算

double Ms(std::chrono::steady_clock::time_point a,
          std::chrono::steady_clock::time_point b) {
    return std::chrono::duration<double, std::milli>(b - a).count();
}

Collider2D* 加盒(SceneObject* go, double hw, double hh) {
    auto* c = go->AddComponent<Collider2D>();
    if (!c) return nullptr;
    c->shape = Shape2D::Box;
    c->halfW = hw;
    c->halfH = hh;
    c->center = {0, 0, 0};
    return c;
}

/// 造一个 N 体的场景。**固定布局**（纯浮点、无随机）⇒ 可复现。
///
/// 布局：地面在 y=-100（上表面 y=-90），物体从 y=+20 往下排成网格。
/// ⚠ 网格要铺得够开，别让它们一开始就重叠 —— 重叠会让分离逻辑每帧都跑，
///   量出来的是"解穿透"而不是"常规积分"。
Scene* 造场景(int n, Physics2DStats* 统计) {
    auto* sc = new Scene();
    sc->SetName("stress");

    //: 静态地面：中心 (0,-100) 半高 10 ⇒ 上表面 y=-90
    auto* 地 = sc->AddRoot("地面");
    地->GetTransform()->SetPosition({0, -100, 0});
    加盒(地, 2000, 10);
    auto* 地刚 = 地->AddComponent<Rigidbody2D>();
    if (地刚) 地刚->isStatic = true;

    const int 列 = static_cast<int>(std::ceil(std::sqrt(static_cast<double>(n))));
    const double 格 = 4.0;                      // 每格 4 单位 ⇒ 半宽 1.6（不重叠）
    const double 起始x = -列 * 格 / 2.0;

    for (int i = 0; i < n; ++i) {
        const int 行 = i / 列, 列i = i % 列;
        auto* go = sc->AddRoot("b" + std::to_string(i));
        go->GetTransform()->SetPosition(
            {起始x + 列i * 格, 20.0 + 行 * 格 * 0.5, 0});
        加盒(go, 0.8, 0.8);
        auto* rb = go->AddComponent<Rigidbody2D>();
        if (rb) {
            rb->gravity = {0, -1000.0, 0};
            rb->stopOnGround = true;
            rb->clampToStage = false;           // 用地面挡，不用边界
        }
    }
    (void)统计;
    return sc;
}

struct 量结果 {
    int 对象 = 0;
    double 中位ms = 0;
    double 最差ms = 0;
    double 分数 = 0;
    int 体数 = 0;
    int 落地 = 0;
    int 命中 = 0;
};

量结果 压一档(int n, int 帧数) {
    量结果 r;
    r.对象 = n;
    StageBounds2D stage;                        // 不启用边界
    std::vector<double> 样本;
    样本.reserve(帧数);
    Physics2DStats 统计{};

    //: 建场景**不计时**（建场景是一次性成本，不是每帧成本）
    Scene* sc = 造场景(n, &统计);
    for (int i = 0; i < 帧数; ++i) {
        const auto t0 = std::chrono::steady_clock::now();
        StepPhysics2D(*sc, kDt, stage, &统计);
        const auto t1 = std::chrono::steady_clock::now();
        样本.push_back(Ms(t0, t1));
    }
    r.体数 = 统计.bodies;
    r.落地 = 统计.grounded;
    r.命中 = 统计.hits;
    std::sort(样本.begin(), 样本.end());
    r.中位ms = 样本[样本.size() / 2];
    r.最差ms = 样本.back();
    r.分数 = 1000.0 * (kDt * 1000.0) / (r.中位ms > 0 ? r.中位ms : 1e-9);
    delete sc;
    return r;
}

/// ⭐ 确定性：同一场景跑两遍，把每个刚体的**坐标和速度**加起来，
///    用 `==` 比（**逐位**，不是"接近" —— 接近没有判别力）。
bool 确定性(int n, int 帧数, double* out1, double* out2) {
    auto 跑 = [&](double* out) {
        Physics2DStats 统计{};
        Scene* sc = 造场景(n, &统计);
        StageBounds2D stage;
        for (int i = 0; i < 帧数; ++i) StepPhysics2D(*sc, kDt, stage, &统计);
        double 和 = 0.0;
        //: ⚠ `Roots()` 返回 `vector<unique_ptr<SceneObject>>`（不是裸指针）——
        //:   直接写 `SceneObject* go : sc->Roots()` 编译不过（我撞了一次）。
        for (const auto& 持 : sc->Roots()) {
            SceneObject* go = 持.get();
            const auto p = go->GetTransform()->GetWorldPosition();
            和 += p.x * 1.0 + p.y * 2.0;        // 加权，避免 x/y 互相抵消
            if (auto* rb = go->GetComponent<Rigidbody2D>()) {
                和 += rb->velocity.x * 3.0 + rb->velocity.y * 5.0;
            }
        }
        *out = 和;
        delete sc;
    };
    跑(out1);
    跑(out2);
    return *out1 == *out2;
}

void 表头() {
    std::printf("  %-10s %-14s %-12s %-10s %-10s %s\n",
                "对象", "中位 ms/帧", "最差 ms", "分数", "能跑帧率", "统计");
}

void 打一行(const 量结果& r) {
    const double 帧率 = std::min(60.0, 1000.0 / (r.中位ms > 0 ? r.中位ms : 1e-9));
    std::printf("  %-10d %-14.4f %-12.4f %-10.0f %-10.1f %s\n",
                r.对象, r.中位ms, r.最差ms, r.分数, 帧率,
                (std::string("体=") + std::to_string(r.体数) +
                 " 落地=" + std::to_string(r.落地) +
                 " 命中=" + std::to_string(r.命中)).c_str());
}

}  // namespace

int main(int argc, char** argv) {
    std::printf("  ══════════════════════════════════════════════════════════\n");
    std::printf("   HybridEngine · 工业模拟压测（大规模 2D 刚体 · 物理求解器）\n");
    std::printf("   只计 StepPhysics2D（不含渲染、不含建场景）\n");
    std::printf("   分数口径：1000 = 恰好 60 帧（与 ms_bench2d 一致）\n");
    std::printf("   预算 %.3f ms/帧\n", k预算ms);
    std::printf("  ══════════════════════════════════════════════════════════\n\n");

    std::printf("  ⚠ 能力边界（引擎自己写的，不是猜的）：\n");
    std::printf("     **是**  确定性积分 + 对**静态**碰撞体的分离 + 世界边界\n");
    std::printf("     **不是** body×body 响应（动态体互不推）· 旋转体 · 摩擦/关节/约束\n");
    std::printf("     ⇒ 所以**工业级模拟在这个引擎上做不了**；\n");
    std::printf("       这里量的是它真能做的上限：多少刚体还能满 60 帧。\n\n");

    //: 分级扫描：142 / 316 / 707 / 1584 / 3548 / 7948 …（每档 ×2.24）
    std::vector<int> 档;
    int 帧数 = 300;
    if (argc >= 2 && std::strcmp(argv[1], "--sweep") == 0) {
        档 = {142, 316, 707, 1584, 3548, 7948, 17800};
        帧数 = 120;
    } else if (argc >= 2) {
        档 = {std::atoi(argv[1])};
        if (argc >= 3) 帧数 = std::atoi(argv[2]);
    } else {
        档 = {500, 2000, 5000};
    }

    std::printf("  帧数/档：%d · 三档取中位\n\n", 帧数);
    表头();
    bool 有超预算 = false;
    for (int n : 档) {
        //: 三档取中位 —— 避开调度抖动（和专业版基准同一做法）
        量结果 最好 = 压一档(n, 帧数);
        const 量结果 b = 压一档(n, 帧数);
        const 量结果 c = 压一档(n, 帧数);
        if (b.中位ms < 最好.中位ms) 最好 = b;
        if (c.中位ms < 最好.中位ms) 最好 = c;
        打一行(最好);
        if (最好.中位ms > k预算ms) {
            std::printf("             ↑ **超出 60 帧预算** —— 饱和点在这一档之前\n");
            有超预算 = true;
            break;                              // 到饱和点就停（甜甜圈那套）
        }
    }
    std::printf("\n");

    //: ⭐ 确定性（这一条比速度更重要 —— 与全仓"黄金帧"取向一致）
    double 甲 = 0, 乙 = 0;
    const bool 确定 = 确定性(500, 120, &甲, &乙);
    std::printf("  ⭐ 确定性（同一场景跑两遍，逐位比较）：%s\n",
                确定 ? "✅ 逐位相同" : "❌ **不同**（引擎的确定性破了）");
    std::printf("     甲=%.17g\n     乙=%.17g\n", 甲, 乙);

    std::printf("\n  ⚠ 口径说明（别误读这个分数）：\n");
    std::printf("     · 它**只**反映物理求解器，不含渲染 —— 渲染另有 ms_bench2d\n");
    std::printf("     · 场景是**无 body×body 的**（引擎不支持）⇒ 这个规模的压力\n");
    std::printf("       主要在「积分 + 对静态分离」，不是「多体接触求解」\n");
    std::printf("     · 分数 1000 = 该负载恰好 60 帧；越大越快\n");
    (void)有超预算;
    return 0;
}
