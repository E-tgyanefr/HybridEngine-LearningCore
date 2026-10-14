/* hblight_selftest —— 跑一段**确定性脚本**，把结果按 %.17g 打出来。
 *
 * ⚠ 它不是为了"看看对不对"，而是为了**和 Python 实现对账**：
 *   `tests/diff_python.py` 用同一段脚本跑 `pyengine`（Python 版），
 *   把两边输出逐行比对。%.17g 是 double 的可逆十进制表示 ⇒ 只要两边
 *   在本机上打出的字符串相同，数值就是**逐位相同**。
 *
 * ⇒ 所以这个文件里**不许有随机数、不许有时钟**。
 */
#include "../hblight.h"

#include <cstdio>
#include <cstring>

namespace {

int 失败 = 0;

/* %.17g 是 double 的往返精度（round-trip），不丢位。 */
void 打(const char* 键, double 值) { std::printf("%s=%.17g\n", 键, 值); }
void 打2(const char* 键, double a, double b) { std::printf("%s=%.17g,%.17g\n", 键, a, b); }
void 打i(const char* 键, long long 值) { std::printf("%s=%lld\n", 键, 值); }
void 打s(const char* 键, const char* 值) { std::printf("%s=%s\n", 键, 值); }

void 检查(const char* 名字, int 条件) {
    if (!条件) {
        std::printf("!! 自检失败: %s\n", 名字);
        ++失败;
    }
}

}  // namespace

int main() {
    std::printf("# %s\n", hb_version());

    /* ── 1. 父子变换 ───────────────────────────────────────────────── */
    {
        hb_transform* 根 = hb_transform_new(10.0, 10.0, 1.0, 0.0);
        hb_transform* 子 = hb_transform_new(5.0, 0.0, 1.0, 0.0);
        hb_transform* 孙 = hb_transform_new(0.0, 3.0, 1.0, 0.0);
        hb_transform_set_parent(子, 根);
        hb_transform_set_parent(孙, 子);

        hb_vec2 p = hb_transform_world_pos(孙);
        打2("t1.world_pos", p.x, p.y);

        hb_transform_set_pos(根, 20.0, 20.0);
        p = hb_transform_world_pos(孙);
        打2("t2.world_pos", p.x, p.y);

        hb_transform_set_pos(根, 0.0, 0.0);
        hb_transform_set_scale(根, 2.0);
        p = hb_transform_world_pos(孙);           /* 子(5,0)*2 + 孙(0,3)*2 = (10,6) */
        打2("t3.world_pos", p.x, p.y);

        hb_transform_set_scale(根, 1.0);
        hb_transform_set_rot(根, 1.5707963267948966);  /* pi/2 */
        p = hb_transform_world_pos(孙);           /* 子 (5,0) 转 90° ⇒ (0,5)，孙 (0,3) ⇒ (-3,0) */
        打2("t4.world_pos", p.x, p.y);
        打("t4.world_rot", hb_transform_world_rot(孙));
        打("t4.world_scale", hb_transform_world_scale(孙));

        p = hb_transform_local_to_world(孙, 1.0, 2.0);
        打2("t4.local_to_world", p.x, p.y);

        /* 缩放链：根 3 × 子 0.5 ⇒ 1.5 */
        hb_transform_set_rot(根, 0.0);
        hb_transform_set_scale(根, 3.0);
        hb_transform_set_scale(子, 0.5);
        打("t5.world_scale", hb_transform_world_scale(孙));

        hb_transform_free(孙);
        hb_transform_free(子);
        hb_transform_free(根);
    }

    /* ── 2. 相机：平滑 + 边界 ─────────────────────────────────────── */
    {
        hb_camera* 相 = hb_camera_new(0.0, 50.0, 1.0, 100.0, 100.0);
        hb_camera_set_target(相, 1, 100.0, 0.0);
        hb_camera_set_smooth(相, 0.2);
        double 每步 = 1.0 / 60.0;
        for (int i = 0; i < 120; ++i) hb_camera_step(相, 每步);
        打2("cam1.smooth_60fps_2s", hb_camera_pos(相).x, hb_camera_pos(相).y);

        hb_camera* 相2 = hb_camera_new(0.0, 50.0, 1.0, 100.0, 100.0);
        hb_camera_set_target(相2, 1, 100.0, 0.0);
        hb_camera_set_smooth(相2, 0.2);
        for (int i = 0; i < 240; ++i) hb_camera_step(相2, 1.0 / 120.0);
        打("cam1.smooth_120fps_2s", hb_camera_pos(相2).x);

        /* 不平滑 ⇒ 立刻到位 */
        hb_camera* 相3 = hb_camera_new(0.0, 0.0, 1.0, 100.0, 100.0);
        hb_camera_set_target(相3, 1, 200.0, 130.0);
        hb_camera_set_smooth(相3, 0.0);
        for (int i = 0; i < 5; ++i) hb_camera_step(相3, 每步);
        打2("cam2.snap", hb_camera_pos(相3).x, hb_camera_pos(相3).y);

        /* 边界夹取：世界 300×300、视口 100 ⇒ 夹到 [50,250] */
        hb_camera* 相4 = hb_camera_new(0.0, 0.0, 1.0, 100.0, 100.0);
        hb_camera_set_target(相4, 1, 999.0, 999.0);
        hb_camera_set_smooth(相4, 0.0);
        hb_camera_set_bounds(相4, 1, 0.0, 0.0, 300.0, 300.0);
        hb_camera_step(相4, 每步);
        打2("cam3.clamp_hi", hb_camera_pos(相4).x, hb_camera_pos(相4).y);
        hb_camera_set_target(相4, 1, -999.0, -999.0);
        hb_camera_step(相4, 每步);
        打2("cam3.clamp_lo", hb_camera_pos(相4).x, hb_camera_pos(相4).y);

        /* 世界比视口小 ⇒ 居中（不是夹到负数区间） */
        hb_camera* 相5 = hb_camera_new(0.0, 0.0, 1.0, 100.0, 100.0);
        hb_camera_set_target(相5, 1, 999.0, 999.0);
        hb_camera_set_smooth(相5, 0.0);
        hb_camera_set_bounds(相5, 1, 0.0, 0.0, 40.0, 40.0);
        hb_camera_step(相5, 每步);
        打("cam4.center_small_world", hb_camera_pos(相5).x);

        /* 世界↔屏幕 */
        hb_camera* 相6 = hb_camera_new(50.0, 50.0, 1.0, 100.0, 100.0);
        hb_vec2 屏 = hb_camera_world_to_screen(相6, 60.0, 40.0);
        打2("cam5.world2screen", 屏.x, 屏.y);
        hb_vec2 回 = hb_camera_screen_to_world(相6, 屏.x, 屏.y);
        打2("cam5.screen2world", 回.x, 回.y);

        hb_camera_free(相); hb_camera_free(相2); hb_camera_free(相3);
        hb_camera_free(相4); hb_camera_free(相5); hb_camera_free(相6);
    }

    /* ── 3. 输入 ─────────────────────────────────────────────────── */
    {
        hb_input* i = hb_input_new();
        hb_input_press(i, "d");
        打i("in1.down_d", hb_input_down(i, "d"));
        打i("in1.just_d", hb_input_just_pressed(i, "d"));
        hb_input_clear_edges(i);
        打i("in2.down_d_after_clear", hb_input_down(i, "d"));
        打i("in2.just_d_after_clear", hb_input_just_pressed(i, "d"));

        hb_input* j = hb_input_new();
        hb_input_press(j, "d");
        hb_input_press(j, "w");
        hb_vec2 dir = hb_input_direction(j);
        打2("in3.diag_dir", dir.x, dir.y);
        打("in3.diag_len", hb_vec_len(dir));

        hb_input* k = hb_input_new();
        打2("in4.empty_dir", hb_input_direction(k).x, hb_input_direction(k).y);

        hb_input* m = hb_input_new();
        hb_input_press(m, "A");
        打i("in5.case_insensitive", hb_input_down(m, "a"));

        hb_input* n = hb_input_new();
        hb_input_press(n, "a");
        hb_input_press(n, "left");
        hb_input_press(n, "d");
        hb_input_press(n, "right");
        hb_vec2 d2 = hb_input_direction(n);
        打2("in6.opposite_cancel", d2.x, d2.y);

        hb_input_free(i); hb_input_free(j); hb_input_free(k);
        hb_input_free(m); hb_input_free(n);
    }

    /* ── 4. 匀速移动 + 边界反弹 ──────────────────────────────────── */
    {
        hb_transform* t = hb_transform_new(90.0, 0.0, 1.0, 0.0);
        hb_mover mv = hb_mover_new(10.0, 0.0);
        hb_mover_set_bounds(&mv, 0.0, 100.0);
        hb_mover_step(&mv, t, 1.0);
        打("mv1.x_after_step1", hb_transform_pos(t).x);
        打("mv1.vx_after_step1", mv.vx);
        hb_mover_step(&mv, t, 1.0);
        打("mv2.x_after_step2", hb_transform_pos(t).x);
        hb_mover_step(&mv, t, 1.0);
        打("mv3.x_after_step3", hb_transform_pos(t).x);

        /* 匀速累积：vx=10，10 步 dt=0.1 ⇒ 10.0 */
        hb_transform* t2 = hb_transform_new(0.0, 0.0, 1.0, 0.0);
        hb_mover mv2 = hb_mover_new(10.0, 0.0);
        for (int i = 0; i < 10; ++i) hb_mover_step(&mv2, t2, 0.1);
        打("mv4.uniform_10_steps", hb_transform_pos(t2).x);

        hb_transform_free(t); hb_transform_free(t2);
    }

    /* ── 5. 重力落地不抖 ─────────────────────────────────────────── */
    {
        hb_transform* t = hb_transform_new(50.0, 50.0, 1.0, 0.0);
        hb_gravity g = hb_gravity_new(-60.0, 0.0);
        for (int i = 0; i < 600; ++i) hb_gravity_step(&g, t, 1.0 / 60.0);
        打("gr1.y_after_600", hb_transform_pos(t).y);
        打("gr1.vy_after_600", g.vy);
        打i("gr1.on_ground", g.on_ground);

        /* 空中不能连跳 */
        hb_gravity g2 = hb_gravity_new(-60.0, 0.0);
        g2.vy = 10.0;
        g2.on_ground = 0;
        打i("gr2.jump_in_air", hb_gravity_jump(&g2, 22.0));

        hb_gravity g3 = hb_gravity_new(-60.0, 0.0);
        打i("gr3.jump_on_ground", hb_gravity_jump(&g3, 22.0));
        打("gr3.vy_after_jump", g3.vy);

        hb_transform_free(t);
    }

    /* ── 6. 计时器 / 闪烁 / 尾迹 ─────────────────────────────────── */
    {
        hb_timer tm = hb_timer_new(0.1);
        int 总 = 0;
        int 触发[] = {0, 0};
        for (int i = 0; i < 25; ++i) {
            int n = hb_timer_step(&tm, 0.1);
            总 += n;
            if (i == 0) 触发[0] = n;
            if (i == 1) 触发[1] = n;
        }
        打i("tm1.total_triggers", 总);
        打i("tm1.count_field", tm.count);
        打i("tm1.first_step", 触发[0]);

        hb_blink b = hb_blink_new(0.1);
        打i("bl1.step_0.15", hb_blink_step(&b, 0.15));
        打i("bl1.phase_after", b.phase);
        打i("bl1.step_0.15_again", hb_blink_step(&b, 0.15));
        打i("bl1.phase_after2", b.phase);

        hb_trail tr = hb_trail_new(0.01);
        int 留痕 = 0;
        for (int i = 0; i < 30; ++i) 留痕 += hb_trail_step(&tr, 0.01);
        打i("tr1.trails_in_30_steps", 留痕);
    }

    /* ── 7. 圆形碰撞（闭区间）───────────────────────────────────── */
    {
        打i("hit1.dist5_r3_r2",
            hb_circle_hit(hb_vec2{0.0, 0.0}, 3.0, hb_vec2{3.0, 4.0}, 2.0));
        打i("hit2.dist5.657_r3_r2",
            hb_circle_hit(hb_vec2{0.0, 0.0}, 3.0, hb_vec2{4.0, 4.0}, 2.0));
        打i("hit3.no_overlap",
            hb_circle_hit(hb_vec2{0.0, 0.0}, 1.0, hb_vec2{10.0, 0.0}, 1.0));
    }

    /* ── 8. 固定步长 ─────────────────────────────────────────────── */
    {
        hb_fixed* f = hb_fixed_new(1.0 / 60.0);
        打i("fx1.frames_from_1s", hb_fixed_advance(f, 1.0));
        /* 卡顿保护：一次给 10 秒也只补 8 帧 */
        hb_fixed* f2 = hb_fixed_new(1.0 / 60.0);
        打i("fx2.catchup_cap", hb_fixed_advance(f2, 10.0));
        hb_fixed_free(f); hb_fixed_free(f2);
    }

    /* ── 9. 场景树 ───────────────────────────────────────────────── */
    {
        hb_scene* s = hb_scene_new("主场景");
        hb_node* a = hb_scene_spawn(s, "a", 0.0, 0.0);
        hb_scene_spawn(s, "b", 0.0, 0.0);
        hb_node* a1 = hb_node_new("a1", 0.0, 0.0);
        hb_node* a2 = hb_node_new("a2", 0.0, 0.0);
        hb_node_add_child(a, a1);
        hb_node_add_child(a, a2);
        打i("sc1.node_count", hb_scene_node_count(s));
        打i("sc1.find_a1_is_child0", hb_scene_find(s, "a1") == hb_node_child_at(a, 0));
        打i("sc1.find_missing", hb_scene_find(s, "nope") == nullptr);

        /* 换爹：a2 改挂到 b 下，父侧不该留 */
        hb_node* b = hb_scene_find(s, "b");
        hb_node_add_child(b, a2);
        打i("sc2.a_child_count", hb_node_child_count(a));
        打i("sc2.b_child_count", hb_node_child_count(b));
        打i("sc2.a2_parent_is_b", hb_node_parent(a2) == b);

        /* 延后加根：并入前不算，并入后算 */
        hb_node* late = hb_node_new("late", 0.0, 0.0);
        hb_scene_add_root_deferred(s, late);
        打i("sc3.roots_before_merge", hb_scene_root_count(s));
        hb_scene_merge_deferred(s);
        打i("sc3.roots_after_merge", hb_scene_root_count(s));

        /* 死亡清理：只清根 */
        hb_node_set_alive(a, 0);
        打i("sc4.alive_roots_before", hb_scene_alive_root_count(s));
        hb_scene_cull_dead(s);
        打i("sc4.roots_after_cull", hb_scene_root_count(s));

        hb_scene_free(s);
    }

    /* ── 10. 颜色 ────────────────────────────────────────────────── */
    {
        char buf[64];
        const char* 名[] = {"黑", "白", "红", "green", "#ff0000", "不存在的颜色", " 红 "};
        for (const char* n : 名) {
            hb_color(n, buf, sizeof(buf));
            std::printf("color[%s]=%s\n", n, buf);
        }
    }

    /* ── 11. 画笔（相机换算 + 缩放）───────────────────────────────── */
    {
        struct 收 { int 圆; int 矩; int 线; int 字; double 首圆[3]; int 首字号; } r{0,0,0,0,{0,0,0},0};
        hb_canvas cv{};
        cv.user = &r;
        cv.circle = [](void* u, double x, double y, double rr, const char*) {
            auto* p = static_cast<收*>(u);
            if (p->圆 == 0) { p->首圆[0] = x; p->首圆[1] = y; p->首圆[2] = rr; }
            p->圆++;
        };
        cv.rect = [](void* u, double, double, double, double, const char*) {
            static_cast<收*>(u)->矩++;
        };
        cv.line = [](void* u, double, double, double, double, const char*, double) {
            static_cast<收*>(u)->线++;
        };
        cv.text = [](void* u, double, double, const char*, const char*, int sz) {
            auto* p = static_cast<收*>(u);
            p->首字号 = sz;
            p->字++;
        };

        hb_camera* 相 = hb_camera_new(50.0, 50.0, 2.0, 100.0, 100.0);
        hb_brush* 笔 = hb_brush_new(&cv, 相);
        hb_brush_circle(笔, 30.0, 40.0, 5.0, "红");
        hb_brush_rect(笔, 10.0, 10.0, 4.0, 6.0, "蓝");
        hb_brush_line(笔, 0.0, 0.0, 1.0, 1.0, "黑", 3.0);
        hb_brush_text(笔, 0.0, 0.0, "x", "黑", 12);
        打i("br1.circles", r.圆);
        打2("br1.first_circle_xy", r.首圆[0], r.首圆[1]);
        打("br1.first_circle_r_scaled", r.首圆[2]);       /* 5 * 2 = 10 */
        打i("br1.rects", r.矩);
        打i("br1.lines", r.线);
        打i("br1.texts", r.字);
        打i("br1.text_size_unscaled", r.首字号);          /* 12，**不**乘 2 */

        /* 无相机 ⇒ 1:1 直通 */
        struct 收 r2{0,0,0,0,{0,0,0},0};
        hb_canvas cv2 = cv;
        cv2.user = &r2;
        hb_brush* 笔2 = hb_brush_new(&cv2, nullptr);
        hb_brush_circle(笔2, 7.0, 8.0, 3.0, "红");
        打2("br2.passthrough_xy", r2.首圆[0], r2.首圆[1]);
        打("br2.passthrough_r", r2.首圆[2]);

        hb_brush_free(笔); hb_brush_free(笔2);
        hb_camera_free(相);
    }

    检查("失败数为 0", 失败 == 0);
    std::printf("# 自检失败数=%d\n", 失败);
    return 失败 == 0 ? 0 : 1;
}
