/* hblight —— 轻量引擎（hybridengine_light/pyengine）的 C++ 内核。
 *
 * ══════════════════════════════════════════════════════════════════════════
 * 这个库是什么
 * ══════════════════════════════════════════════════════════════════════════
 * 它是 `hybridengine_light/pyengine/` 那 1406 行 Python 的 **C++ 对应物**：
 * 数值内核（向量 / 父子变换 / 相机 / 输入 / 固定步长）+ 场景树 + 15 个组件的
 * 数值行为。渲染**不在**这里 —— 渲染走 C ABI 回调交回调用方（见 hb_canvas）。
 *
 * ══════════════════════════════════════════════════════════════════════════
 * ⚠ 为什么是"内核"而不是"整个引擎"
 * ══════════════════════════════════════════════════════════════════════════
 * 因为判据（`tests/test_pyengine.py`，648 行，**一个字都不许改**）要求
 * **Python 对象身份**与**Python 侧子类化组件**：
 *
 *     class 探针(E.组件): ...          # 学员在 Python 里继承组件
 *     self.assertIs(c.宿主, o)         # 同一实体必须是同一个 Python 对象
 *     self.assertIs(s.找("a1"), a.子对象[0])
 *     o.取(C.圆点)                      # isinstance 语义
 *
 * ⇒ 对象模型必须留在 Python。C++ 拿走的是**算得多的那部分**，
 *   Python 侧留一层薄壳，行为逐位对齐。
 *
 * ══════════════════════════════════════════════════════════════════════════
 * ⚠ 逐位等价的两条硬约束（写代码时别"顺手优化"）
 * ══════════════════════════════════════════════════════════════════════════
 * ① **全程 double**，不许出现 float —— Python 的 float 就是 C 的 double。
 * ② **公式逐字照抄**，包括看似奇怪的写法：
 *      · 相机平滑用的是 `pow(2.718281828, -dt/tau)` —— **不是 `exp()`**！
 *        Python 原文写的是 `pow(2.718281828, ...)`，换 exp 会差最后几位。
 *      · `边界` 的 `<= 2*半宽` 判据、`min(max(...))` 的顺序，都照抄。
 *    改公式 = 改行为，而"改行为"在这里等于"判据红"。
 */
#ifndef HBLIGHT_H
#define HBLIGHT_H

#include <stddef.h>

#if defined(_WIN32)
#  define HB_API __declspec(dllexport)
#elif defined(__GNUC__)
#  define HB_API __attribute__((visibility("default")))
#else
#  define HB_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* ══════════════════════ 版本 ══════════════════════ */

HB_API const char* hb_version(void);

/* ══════════════════════ 向量 ══════════════════════ */

typedef struct { double x, y; } hb_vec2;

HB_API hb_vec2 hb_vec_add(hb_vec2 a, hb_vec2 b);
HB_API hb_vec2 hb_vec_sub(hb_vec2 a, hb_vec2 b);
HB_API hb_vec2 hb_vec_mul(hb_vec2 a, double k);
HB_API double hb_vec_len(hb_vec2 a);                 /* math.hypot(x, y) */
HB_API hb_vec2 hb_vec_norm(hb_vec2 a);               /* 零向量 → (0,0) */
HB_API hb_vec2 hb_vec_rot(hb_vec2 a, double rad);

/* ══════════════════════ 变换（支持父链） ══════════════════════ */

typedef struct hb_transform hb_transform;

HB_API hb_transform* hb_transform_new(double x, double y, double scale, double rot);
HB_API void         hb_transform_free(hb_transform* t);

/* ⚠ 只设指针，**不接管所有权** —— 父的生命周期由调用方管（与 Python 的 `_父` 一致）。 */
HB_API void         hb_transform_set_parent(hb_transform* t, hb_transform* parent);
HB_API hb_transform* hb_transform_parent(const hb_transform* t);

HB_API void    hb_transform_set_pos(hb_transform* t, double x, double y);
HB_API hb_vec2 hb_transform_pos(const hb_transform* t);
HB_API void    hb_transform_set_scale(hb_transform* t, double s);
HB_API double  hb_transform_scale(const hb_transform* t);
HB_API void    hb_transform_set_rot(hb_transform* t, double r);
HB_API double  hb_transform_rot(const hb_transform* t);

HB_API hb_vec2 hb_transform_world_pos(const hb_transform* t);
HB_API double  hb_transform_world_rot(const hb_transform* t);
HB_API double  hb_transform_world_scale(const hb_transform* t);
HB_API hb_vec2 hb_transform_local_to_world(const hb_transform* t, double lx, double ly);

/* ══════════════════════ 相机2D ══════════════════════ */

typedef struct hb_camera hb_camera;

HB_API hb_camera* hb_camera_new(double x, double y, double scale, double vw, double vh);
HB_API void       hb_camera_free(hb_camera* c);

HB_API void   hb_camera_set_pos(hb_camera* c, double x, double y);
HB_API hb_vec2 hb_camera_pos(const hb_camera* c);
HB_API void   hb_camera_set_scale(hb_camera* c, double s);
HB_API double hb_camera_scale(const hb_camera* c);
HB_API void   hb_camera_set_viewport(hb_camera* c, double w, double h);
HB_API void   hb_camera_viewport(const hb_camera* c, double* w, double* h);

/* 跟随目标用**世界位置**表示（Python 侧传 `跟随.世界位置`），None = 不跟随。
 * ⚠ 为什么不让 C++ 持有 Python 对象：那样每帧都要跨语言查属性；
 *   传两个 double 进来，语义完全一样。 */
HB_API void hb_camera_set_target(hb_camera* c, int has_target, double tx, double ty);
HB_API void hb_camera_set_smooth(hb_camera* c, double tau);
HB_API double hb_camera_smooth(const hb_camera* c);
HB_API void hb_camera_set_bounds(hb_camera* c, int has_bounds,
                                 double left, double bottom, double right, double top);

HB_API hb_vec2 hb_camera_world_to_screen(const hb_camera* c, double x, double y);
HB_API hb_vec2 hb_camera_screen_to_world(const hb_camera* c, double sx, double sy);
HB_API void    hb_camera_step(hb_camera* c, double dt);

/* ══════════════════════ 输入 ══════════════════════ */

typedef struct hb_input hb_input;

HB_API hb_input* hb_input_new(void);
HB_API void      hb_input_free(hb_input* in);

HB_API void hb_input_press(hb_input* in, const char* key);    /* 内部转小写 */
HB_API void hb_input_release(hb_input* in, const char* key);
HB_API void hb_input_clear_edges(hb_input* in);               /* `帧末清理` */
HB_API int  hb_input_down(const hb_input* in, const char* key);
HB_API int  hb_input_just_pressed(const hb_input* in, const char* key);
HB_API int  hb_input_just_released(const hb_input* in, const char* key);
HB_API void hb_input_set_mouse(hb_input* in, double x, double y);
HB_API void hb_input_mouse(const hb_input* in, double* x, double* y);
HB_API hb_vec2 hb_input_direction(const hb_input* in);        /* 已归一；无输入 → (0,0) */

/* ══════════════════════ 固定步长计时 ══════════════════════ */

typedef struct hb_fixed hb_fixed;
HB_API hb_fixed* hb_fixed_new(double step);
HB_API void      hb_fixed_free(hb_fixed* f);
/* 累加 `elapsed` 秒，返回**这次应当跑几个逻辑帧**（卡顿保护：一次最多补 8 帧）。 */
HB_API int  hb_fixed_advance(hb_fixed* f, double elapsed);
HB_API int  hb_fixed_consume(hb_fixed* f);                    /* 取走一帧，返回 1/0 */
HB_API double hb_fixed_step_size(const hb_fixed* f);

/* ══════════════════════ 场景树 ══════════════════════ */

typedef struct hb_node hb_node;
typedef struct hb_scene hb_scene;

HB_API hb_scene* hb_scene_new(const char* name);
HB_API void      hb_scene_free(hb_scene* s);   /* 释放场景**拥有**的所有节点 */
HB_API const char* hb_scene_name(const hb_scene* s);

HB_API hb_node* hb_scene_spawn(hb_scene* s, const char* name, double x, double y); /* 建 + 加根 */
HB_API hb_node* hb_node_new(const char* name, double x, double y);                 /* 游离节点 */
HB_API void     hb_scene_add_root(hb_scene* s, hb_node* n);
HB_API void     hb_scene_add_root_deferred(hb_scene* s, hb_node* n);               /* 帧末并入 */
HB_API void     hb_scene_merge_deferred(hb_scene* s);                              /* `_并入延后的` */
HB_API int      hb_scene_root_count(const hb_scene* s);
HB_API hb_node* hb_scene_root_at(const hb_scene* s, int i);
HB_API void     hb_node_add_child(hb_node* parent, hb_node* child);                /* 自动换爹 */
HB_API int      hb_node_child_count(const hb_node* n);
HB_API hb_node* hb_node_child_at(const hb_node* n, int i);
HB_API hb_node* hb_node_parent(const hb_node* n);
HB_API const char* hb_node_name(const hb_node* n);
HB_API void     hb_node_set_alive(hb_node* n, int alive);
HB_API int      hb_node_alive(const hb_node* n);
HB_API hb_transform* hb_node_transform(hb_node* n);
HB_API hb_node* hb_scene_find(const hb_scene* s, const char* name);   /* 深度优先，顺序确定 */
HB_API int      hb_scene_node_count(const hb_scene* s);               /* 自己 + 全部后代 */
HB_API int      hb_scene_alive_root_count(const hb_scene* s);
HB_API void     hb_scene_cull_dead(hb_scene* s);                      /* 清掉不活着的根 */

/* ══════════════════════ 组件的数值行为 ══════════════════════
 * ⚠ 这些是**行为**，不是对象 —— 状态由调用方按需保存（与 Python 组件的字段一一对应）。 */

typedef struct {
    double vx, vy;
    int    has_bounds;
    double lo, hi;
} hb_mover;                       /* 匀速移动 */

HB_API hb_mover hb_mover_new(double vx, double vy);
HB_API void     hb_mover_set_bounds(hb_mover* m, double lo, double hi);
HB_API void     hb_mover_step(hb_mover* m, hb_transform* t, double dt);

typedef struct { double ax, ay; } hb_dir;   /* 方向：给 按键驱动 用 */

HB_API void hb_keymove_step(hb_transform* t, double speed, hb_vec2 dir, double dt,
                            int has_bounds, double lo, double hi);

typedef struct {
    double accel;      /* 加速度 */
    double ground;     /* 地面 */
    double vy;
    int    on_ground;
} hb_gravity;

HB_API hb_gravity hb_gravity_new(double accel, double ground);
HB_API void       hb_gravity_step(hb_gravity* g, hb_transform* t, double dt);
HB_API int        hb_gravity_jump(hb_gravity* g, double 初速);   /* 不在空中才跳 */

typedef struct { double interval; double accum; int count; } hb_timer;
HB_API hb_timer hb_timer_new(double interval);
/* 返回这一帧触发的**次数**（调用方据此决定回调几次） */
HB_API int  hb_timer_step(hb_timer* t, double dt);

typedef struct { double period; double accum; int phase; } hb_blink;
HB_API hb_blink hb_blink_new(double period);
HB_API int       hb_blink_step(hb_blink* b, double dt);   /* 返回 1 = 这一帧翻转了 */

typedef struct { double interval; double accum; } hb_trail;
HB_API hb_trail hb_trail_new(double interval);
HB_API int       hb_trail_step(hb_trail* t, double dt);   /* 返回 1 = 这一帧该留痕 */

HB_API int hb_circle_hit(hb_vec2 a, double ra, hb_vec2 b, double rb);  /* <= 半径和 */

/* ══════════════════════ 渲染：C ABI 回调 ══════════════════════
 * 渲染**不在这里** —— 库把图元通过回调交给调用方给的画布。
 * ⚠ 两条语义不同的后端（录制后端 不裁剪 / Tk后端 裁剪）靠**调用方自己**区分，
 *   库这边只负责"照原样提交"，见 Python 侧 `画笔接口` 的缩放逻辑。 */

typedef struct {
    void* user;
    void (*clear)(void* user, const char* color);
    void (*circle)(void* user, double x, double y, double r, const char* color);
    void (*rect)(void* user, double x, double y, double w, double h, const char* color);
    void (*line)(void* user, double x1, double y1, double x2, double y2,
                 const char* color, double width);
    void (*text)(void* user, double x, double y, const char* content,
                 const char* color, int size);
    void (*present)(void* user);
} hb_canvas;

/* 画笔：把**世界坐标**换算成屏幕坐标后交给画布。相机为 NULL 时 1:1 直通。 */
typedef struct hb_brush hb_brush;
HB_API hb_brush* hb_brush_new(const hb_canvas* canvas, const hb_camera* cam);
HB_API void      hb_brush_free(hb_brush* b);
HB_API void hb_brush_circle(hb_brush* b, double x, double y, double r, const char* color);
HB_API void hb_brush_rect(hb_brush* b, double x, double y, double w, double h, const char* color);
HB_API void hb_brush_line(hb_brush* b, double x1, double y1, double x2, double y2,
                          const char* color, double width);
HB_API void hb_brush_text(hb_brush* b, double x, double y, const char* content,
                          const char* color, int size);

/* ══════════════════════ 颜色 ══════════════════════
 * 与 Python `render.颜色` 同一张表。写进 caller 缓冲（>= 8 字节）。
 * 未知名字**原样写回**（不报错 —— 与 Stage 同一约定）。 */
HB_API void hb_color(const char* name, char* out, size_t out_size);

#ifdef __cplusplus
}  /* extern "C" */
#endif

#endif /* HBLIGHT_H */
