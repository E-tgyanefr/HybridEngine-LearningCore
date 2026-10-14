/* hblight.cpp —— 见 hblight.h 顶部的说明（尤其是"逐位等价的两条硬约束"）。
 *
 * ⚠ 本文件里每个公式都能在 Python 原文里找到一一对应的一行。
 *   注释里给出出处（文件:行 是写作时的位置，行号会漂，函数名不会）。
 *   想改公式之前先想清楚：**改公式 = 改行为 = 判据红**。
 */
#include "hblight.h"

#include <algorithm>
#include <cmath>
#include <cstring>
#include <string>
#include <vector>

/* ══════════════════════════════════════════════════════════════════════════
 * 小工具
 * ══════════════════════════════════════════════════════════════════════════ */

namespace {

/* Python 的 str.lower() 对 ASCII 的等价物。键名只有 ASCII（tkinter keysym）。 */
std::string 小写(const char* s) {
    std::string r = s ? s : "";
    for (char& c : r) {
        if (c >= 'A' && c <= 'Z') c = static_cast<char>(c - 'A' + 'a');
    }
    return r;
}

double 双精度最小(double a, double b) { return a < b ? a : b; }
double 双精度最大(double a, double b) { return a > b ? a : b; }

}  // namespace

/* ══════════════════════════════════════════════════════════════════════════
 * 版本
 * ══════════════════════════════════════════════════════════════════════════ */

extern "C" HB_API const char* hb_version(void) { return "hblight 0.1.0"; }

/* ══════════════════════════════════════════════════════════════════════════
 * 向量 —— 对应 scene.py::向量
 * ══════════════════════════════════════════════════════════════════════════ */

extern "C" {

HB_API hb_vec2 hb_vec_add(hb_vec2 a, hb_vec2 b) { return hb_vec2{a.x + b.x, a.y + b.y}; }
HB_API hb_vec2 hb_vec_sub(hb_vec2 a, hb_vec2 b) { return hb_vec2{a.x - b.x, a.y - b.y}; }
HB_API hb_vec2 hb_vec_mul(hb_vec2 a, double k) { return hb_vec2{a.x * k, a.y * k}; }

/* Python: `math.hypot(self.x, self.y)` */
HB_API double hb_vec_len(hb_vec2 a) { return std::hypot(a.x, a.y); }

/* Python: `return 向量(0.0, 0.0) if n == 0 else 向量(self.x / n, self.y / n)` */
HB_API hb_vec2 hb_vec_norm(hb_vec2 a) {
    const double n = hb_vec_len(a);
    if (n == 0.0) return hb_vec2{0.0, 0.0};
    return hb_vec2{a.x / n, a.y / n};
}

/* Python: `c, s = math.cos(弧度), math.sin(弧度)` */
HB_API hb_vec2 hb_vec_rot(hb_vec2 a, double rad) {
    const double c = std::cos(rad);
    const double s = std::sin(rad);
    return hb_vec2{a.x * c - a.y * s, a.x * s + a.y * c};
}

}  // extern "C"

/* ══════════════════════════════════════════════════════════════════════════
 * 变换 —— 对应 scene.py::变换
 * ══════════════════════════════════════════════════════════════════════════ */

struct hb_transform {
    double x = 0.0, y = 0.0;
    double 缩放 = 1.0;
    double 旋转 = 0.0;
    hb_transform* 父 = nullptr;
};

extern "C" {

HB_API hb_transform* hb_transform_new(double x, double y, double scale, double rot) {
    hb_transform* t = new hb_transform();
    t->x = x; t->y = y; t->缩放 = scale; t->旋转 = rot; t->父 = nullptr;
    return t;
}
HB_API void hb_transform_free(hb_transform* t) { delete t; }

HB_API void hb_transform_set_parent(hb_transform* t, hb_transform* parent) {
    if (t) t->父 = parent;
}
HB_API hb_transform* hb_transform_parent(const hb_transform* t) { return t ? t->父 : nullptr; }

HB_API void hb_transform_set_pos(hb_transform* t, double x, double y) {
    if (t) { t->x = x; t->y = y; }
}
HB_API hb_vec2 hb_transform_pos(const hb_transform* t) {
    return t ? hb_vec2{t->x, t->y} : hb_vec2{0.0, 0.0};
}
HB_API void hb_transform_set_scale(hb_transform* t, double s) { if (t) t->缩放 = s; }
HB_API double hb_transform_scale(const hb_transform* t) { return t ? t->缩放 : 1.0; }
HB_API void hb_transform_set_rot(hb_transform* t, double r) { if (t) t->旋转 = r; }
HB_API double hb_transform_rot(const hb_transform* t) { return t ? t->旋转 : 0.0; }

/* Python::变换.世界位置
 *     p = self.位置
 *     t = self._父
 *     while t is not None:
 *         p = t.位置 + p.旋转(t.旋转) * t.缩放
 *         t = t._父
 */
HB_API hb_vec2 hb_transform_world_pos(const hb_transform* t) {
    if (!t) return hb_vec2{0.0, 0.0};
    hb_vec2 p{t->x, t->y};
    const hb_transform* c = t->父;
    while (c != nullptr) {
        hb_vec2 r = hb_vec_rot(p, c->旋转);
        r = hb_vec_mul(r, c->缩放);
        p = hb_vec_add(hb_vec2{c->x, c->y}, r);
        c = c->父;
    }
    return p;
}

/* Python::变换.世界旋转  —— r += t.旋转 */
HB_API double hb_transform_world_rot(const hb_transform* t) {
    if (!t) return 0.0;
    double r = t->旋转;
    const hb_transform* c = t->父;
    while (c != nullptr) { r += c->旋转; c = c->父; }
    return r;
}

/* Python::变换.世界缩放  —— s *= t.缩放 */
HB_API double hb_transform_world_scale(const hb_transform* t) {
    if (!t) return 1.0;
    double s = t->缩放;
    const hb_transform* c = t->父;
    while (c != nullptr) { s *= c->缩放; c = c->父; }
    return s;
}

/* Python::变换.局部转世界
 *     v = 向量(局部x, 局部y) * self.缩放
 *     v = v.旋转(self.旋转) + self.位置
 *     t = self._父
 *     while t is not None:
 *         v = v * t.缩放
 *         v = v.旋转(t.旋转) + t.位置
 *         t = t._父
 */
HB_API hb_vec2 hb_transform_local_to_world(const hb_transform* t, double lx, double ly) {
    if (!t) return hb_vec2{lx, ly};
    hb_vec2 v = hb_vec_mul(hb_vec2{lx, ly}, t->缩放);
    v = hb_vec_add(hb_vec_rot(v, t->旋转), hb_vec2{t->x, t->y});
    const hb_transform* c = t->父;
    while (c != nullptr) {
        v = hb_vec_mul(v, c->缩放);
        v = hb_vec_add(hb_vec_rot(v, c->旋转), hb_vec2{c->x, c->y});
        c = c->父;
    }
    return v;
}

}  // extern "C"

/* ══════════════════════════════════════════════════════════════════════════
 * 相机2D —— 对应 engine.py::相机2D
 * ══════════════════════════════════════════════════════════════════════════ */

struct hb_camera {
    double x = 0.0, y = 50.0;
    double 缩放 = 1.0;
    double 视口宽 = 100.0, 视口高 = 100.0;
    int    有目标 = 0;
    double 目标x = 0.0, 目标y = 0.0;
    double 平滑 = 0.0;
    int    有边界 = 0;
    double 左 = 0.0, 下 = 0.0, 右 = 0.0, 上 = 0.0;
};

extern "C" {

HB_API hb_camera* hb_camera_new(double x, double y, double scale, double vw, double vh) {
    hb_camera* c = new hb_camera();
    c->x = x; c->y = y; c->缩放 = scale; c->视口宽 = vw; c->视口高 = vh;
    return c;
}
HB_API void hb_camera_free(hb_camera* c) { delete c; }

HB_API void hb_camera_set_pos(hb_camera* c, double x, double y) { if (c) { c->x = x; c->y = y; } }
HB_API hb_vec2 hb_camera_pos(const hb_camera* c) {
    return c ? hb_vec2{c->x, c->y} : hb_vec2{0.0, 0.0};
}
HB_API void hb_camera_set_scale(hb_camera* c, double s) { if (c) c->缩放 = s; }
HB_API double hb_camera_scale(const hb_camera* c) { return c ? c->缩放 : 1.0; }
HB_API void hb_camera_set_viewport(hb_camera* c, double w, double h) {
    if (c) { c->视口宽 = w; c->视口高 = h; }
}
HB_API void hb_camera_viewport(const hb_camera* c, double* w, double* h) {
    if (!c) return;
    if (w) *w = c->视口宽;
    if (h) *h = c->视口高;
}
HB_API void hb_camera_set_target(hb_camera* c, int has_target, double tx, double ty) {
    if (!c) return;
    c->有目标 = has_target;
    c->目标x = tx; c->目标y = ty;
}
HB_API void hb_camera_set_smooth(hb_camera* c, double tau) { if (c) c->平滑 = tau; }
HB_API double hb_camera_smooth(const hb_camera* c) { return c ? c->平滑 : 0.0; }
HB_API void hb_camera_set_bounds(hb_camera* c, int has_bounds,
                                 double left, double bottom, double right, double top) {
    if (!c) return;
    c->有边界 = has_bounds;
    c->左 = left; c->下 = bottom; c->右 = right; c->上 = top;
}

/* Python::相机2D.世界转屏幕
 *     sx = (x - self.x) * self.缩放 + self.视口宽 / 2
 *     sy = (y - self.y) * self.缩放 + self.视口高 / 2
 */
HB_API hb_vec2 hb_camera_world_to_screen(const hb_camera* c, double x, double y) {
    if (!c) return hb_vec2{x, y};
    return hb_vec2{(x - c->x) * c->缩放 + c->视口宽 / 2.0,
                   (y - c->y) * c->缩放 + c->视口高 / 2.0};
}

/* Python::相机2D.屏幕转世界
 *     x = (sx - self.视口宽 / 2) / self.缩放 + self.x
 */
HB_API hb_vec2 hb_camera_screen_to_world(const hb_camera* c, double sx, double sy) {
    if (!c) return hb_vec2{sx, sy};
    return hb_vec2{(sx - c->视口宽 / 2.0) / c->缩放 + c->x,
                   (sy - c->视口高 / 2.0) / c->缩放 + c->y};
}

/* Python::相机2D.每帧
 *     if self.跟随 is None: return
 *     目标 = self.跟随.世界位置
 *     if self.平滑 <= 0: self.x, self.y = 目标.x, 目标.y
 *     else:
 *         系数 = 1.0 - pow(2.718281828, -dt / max(1e-6, self.平滑))
 *         self.x += (目标.x - self.x) * 系数
 *         self.y += (目标.y - self.y) * 系数
 *     if self.边界 is not None:
 *         半宽 = self.视口宽 / (2 * self.缩放); 半高 = self.视口高 / (2 * self.缩放)
 *         self.x = (左+右)/2 if (右-左) <= 2*半宽 else min(max(self.x, 左+半宽), 右-半宽)
 *         self.y = (下+上)/2 if (上-下) <= 2*半高 else min(max(self.y, 下+半高), 上-半高)
 *
 * ⚠⚠ `pow(2.718281828, ...)` —— **不是 `std::exp`**。Python 原文就是 pow，
 *    换成 exp 会在最后几位上分道扬镳。
 */
HB_API void hb_camera_step(hb_camera* c, double dt) {
    if (!c) return;
    if (!c->有目标) return;

    if (c->平滑 <= 0.0) {
        c->x = c->目标x;
        c->y = c->目标y;
    } else {
        const double 系数 = 1.0 - std::pow(2.718281828, -dt / 双精度最大(1e-6, c->平滑));
        c->x += (c->目标x - c->x) * 系数;
        c->y += (c->目标y - c->y) * 系数;
    }

    if (c->有边界) {
        const double 半宽 = c->视口宽 / (2.0 * c->缩放);
        const double 半高 = c->视口高 / (2.0 * c->缩放);
        c->x = ((c->右 - c->左) <= 2.0 * 半宽)
                   ? (c->左 + c->右) / 2.0
                   : 双精度最小(双精度最大(c->x, c->左 + 半宽), c->右 - 半宽);
        c->y = ((c->上 - c->下) <= 2.0 * 半高)
                   ? (c->下 + c->上) / 2.0
                   : 双精度最小(双精度最大(c->y, c->下 + 半高), c->上 - 半高);
    }
}

}  // extern "C"

/* ══════════════════════════════════════════════════════════════════════════
 * 输入 —— 对应 engine.py::输入
 * ══════════════════════════════════════════════════════════════════════════ */

struct hb_input {
    std::vector<std::string> 按下;
    std::vector<std::string> 这帧按下;
    std::vector<std::string> 这帧抬起;
    double 鼠标x = 0.0, 鼠标y = 0.0;

    bool 有(std::vector<std::string>& v, const std::string& k) const {
        return std::find(v.begin(), v.end(), k) != v.end();
    }
    static void 加(std::vector<std::string>& v, const std::string& k) {
        if (std::find(v.begin(), v.end(), k) == v.end()) v.push_back(k);
    }
    static void 删(std::vector<std::string>& v, const std::string& k) {
        v.erase(std::remove(v.begin(), v.end(), k), v.end());
    }
};

extern "C" {

HB_API hb_input* hb_input_new(void) { return new hb_input(); }
HB_API void hb_input_free(hb_input* in) { delete in; }

/* Python::输入._按 —— ⚠ 存入时**也必须**转小写（当年就是这里没转，判据抓到的）。 */
HB_API void hb_input_press(hb_input* in, const char* key) {
    if (!in) return;
    const std::string k = 小写(key);
    if (!in->有(in->按下, k)) hb_input::加(in->这帧按下, k);
    hb_input::加(in->按下, k);
}

HB_API void hb_input_release(hb_input* in, const char* key) {
    if (!in) return;
    const std::string k = 小写(key);
    hb_input::删(in->按下, k);
    hb_input::加(in->这帧抬起, k);
}

HB_API void hb_input_clear_edges(hb_input* in) {
    if (!in) return;
    in->这帧按下.clear();
    in->这帧抬起.clear();
}

HB_API int hb_input_down(const hb_input* in, const char* key) {
    if (!in) return 0;
    const std::string k = 小写(key);
    return std::find(in->按下.begin(), in->按下.end(), k) != in->按下.end() ? 1 : 0;
}
HB_API int hb_input_just_pressed(const hb_input* in, const char* key) {
    if (!in) return 0;
    const std::string k = 小写(key);
    return std::find(in->这帧按下.begin(), in->这帧按下.end(), k) != in->这帧按下.end() ? 1 : 0;
}
HB_API int hb_input_just_released(const hb_input* in, const char* key) {
    if (!in) return 0;
    const std::string k = 小写(key);
    return std::find(in->这帧抬起.begin(), in->这帧抬起.end(), k) != in->这帧抬起.end() ? 1 : 0;
}

HB_API void hb_input_set_mouse(hb_input* in, double x, double y) {
    if (in) { in->鼠标x = x; in->鼠标y = y; }
}
HB_API void hb_input_mouse(const hb_input* in, double* x, double* y) {
    if (!in) return;
    if (x) *x = in->鼠标x;
    if (y) *y = in->鼠标y;
}

/* Python::输入.方向 —— WASD 与方向键等价；长度已归一（斜走不加速）。 */
HB_API hb_vec2 hb_input_direction(const hb_input* in) {
    if (!in) return hb_vec2{0.0, 0.0};
    const int x = ((hb_input_down(in, "d") || hb_input_down(in, "right")) ? 1 : 0) -
                  ((hb_input_down(in, "a") || hb_input_down(in, "left")) ? 1 : 0);
    const int y = ((hb_input_down(in, "w") || hb_input_down(in, "up")) ? 1 : 0) -
                  ((hb_input_down(in, "s") || hb_input_down(in, "down")) ? 1 : 0);
    const hb_vec2 v{static_cast<double>(x), static_cast<double>(y)};
    const double n = hb_vec_len(v);
    if (n == 0.0) return hb_vec2{0.0, 0.0};
    return hb_vec2{v.x / n, v.y / n};
}

}  // extern "C"

/* ══════════════════════════════════════════════════════════════════════════
 * 固定步长 —— 对应 engine.py::游戏.跑 的累积器
 *   `累积 = min(累积 + 经过, self.步长 * 8)`   ← 卡顿保护，一次最多补 8 帧
 *   `while 累积 >= self.步长: ...; 累积 -= self.步长`
 * ⚠ 用**反复相减**而不是 `floor(累积/步长)` —— 浮点余数不一样。
 * ══════════════════════════════════════════════════════════════════════════ */

struct hb_fixed {
    double 步长 = 1.0 / 60.0;
    double 累积 = 0.0;
};

extern "C" {

HB_API hb_fixed* hb_fixed_new(double step) {
    hb_fixed* f = new hb_fixed();
    if (step > 0.0) f->步长 = step;
    return f;
}
HB_API void hb_fixed_free(hb_fixed* f) { delete f; }
HB_API double hb_fixed_step_size(const hb_fixed* f) { return f ? f->步长 : 0.0; }

HB_API int hb_fixed_advance(hb_fixed* f, double elapsed) {
    if (!f) return 0;
    const double 上限 = f->步长 * 8.0;
    const double 和 = f->累积 + elapsed;
    f->累积 = 双精度最小(和, 上限);
    int n = 0;
    while (f->累积 >= f->步长) {
        f->累积 -= f->步长;
        ++n;
    }
    return n;
}

HB_API int hb_fixed_consume(hb_fixed* f) {
    if (!f || f->累积 < f->步长) return 0;
    f->累积 -= f->步长;
    return 1;
}

}  // extern "C"

/* ══════════════════════════════════════════════════════════════════════════
 * 场景树 —— 对应 scene.py::场景 / 场景对象
 * ══════════════════════════════════════════════════════════════════════════ */

struct hb_scene;

struct hb_node {
    std::string 名字;
    hb_transform* 变换 = nullptr;
    std::vector<hb_node*> 子对象;
    hb_node* 父亲 = nullptr;
    int 活着 = 1;
    hb_scene* 场景 = nullptr;   // nullptr = 还没进场景
};

struct hb_scene {
    std::string 名字;
    std::vector<hb_node*> 根;
    std::vector<hb_node*> 待加;      // `_待加`
    std::vector<hb_node*> 拥有;      // 由本场景 free
    std::vector<hb_node*> 待释放;    // 换爹/被摘下来的，free 时一起放
};

namespace {

/* `场景对象._传播场景` —— 递归设场景指针（组件宿主那边由 Python 自己管）。 */
void 传播场景(hb_node* n, hb_scene* s) {
    n->场景 = s;
    for (hb_node* c : n->子对象) 传播场景(c, s);
}

void 收进场景(hb_scene* s, hb_node* n) {
    if (!s || !n) return;
    if (std::find(s->拥有.begin(), s->拥有.end(), n) == s->拥有.end()) s->拥有.push_back(n);
    传播场景(n, s);
}

/* 从父那里摘下来（`加子` 里的换爹分支）。不释放 —— 由场景 free 时统一放。 */
void 摘下来(hb_node* n) {
    if (!n || !n->父亲) return;
    hb_node* p = n->父亲;
    p->子对象.erase(std::remove(p->子对象.begin(), p->子对象.end(), n), p->子对象.end());
    n->父亲 = nullptr;
}

}  // namespace

extern "C" {

HB_API hb_scene* hb_scene_new(const char* name) {
    hb_scene* s = new hb_scene();
    s->名字 = name ? name : "主场景";
    return s;
}

HB_API void hb_scene_free(hb_scene* s) {
    if (!s) return;
    for (hb_node* n : s->拥有) {
        if (n) {
            hb_transform_free(n->变换);
            delete n;
        }
    }
    delete s;
}

HB_API const char* hb_scene_name(const hb_scene* s) { return s ? s->名字.c_str() : ""; }

HB_API hb_node* hb_node_new(const char* name, double x, double y) {
    hb_node* n = new hb_node();
    n->名字 = name ? name : "对象";
    n->变换 = hb_transform_new(x, y, 1.0, 0.0);
    return n;
}

HB_API void hb_scene_add_root(hb_scene* s, hb_node* n) {
    if (!s || !n) return;
    收进场景(s, n);
    s->根.push_back(n);
}

/* `延后加根` —— 组件在 `每帧` 里造东西时用它（遍历中改容器是经典崩溃点）。 */
HB_API void hb_scene_add_root_deferred(hb_scene* s, hb_node* n) {
    if (!s || !n) return;
    s->待加.push_back(n);
}

/* `场景._并入延后的` */
HB_API void hb_scene_merge_deferred(hb_scene* s) {
    if (!s || s->待加.empty()) return;
    std::vector<hb_node*> 批;
    批.swap(s->待加);
    for (hb_node* n : 批) {
        收进场景(s, n);
        s->根.push_back(n);
    }
}

HB_API hb_node* hb_scene_spawn(hb_scene* s, const char* name, double x, double y) {
    hb_node* n = hb_node_new(name, x, y);
    hb_scene_add_root(s, n);
    return n;
}

HB_API int hb_scene_root_count(const hb_scene* s) {
    return s ? static_cast<int>(s->根.size()) : 0;
}
HB_API hb_node* hb_scene_root_at(const hb_scene* s, int i) {
    if (!s || i < 0 || i >= static_cast<int>(s->根.size())) return nullptr;
    return s->根[static_cast<size_t>(i)];
}

/* `场景对象.加子` —— 换爹时先从旧爹摘掉；变换的 `_父` 也要跟着设。 */
HB_API void hb_node_add_child(hb_node* parent, hb_node* child) {
    if (!parent || !child) return;
    摘下来(child);
    child->父亲 = parent;
    hb_transform_set_parent(child->变换, parent->变换);
    parent->子对象.push_back(child);
    if (parent->场景) 收进场景(parent->场景, child);
}

HB_API int hb_node_child_count(const hb_node* n) {
    return n ? static_cast<int>(n->子对象.size()) : 0;
}
HB_API hb_node* hb_node_child_at(const hb_node* n, int i) {
    if (!n || i < 0 || i >= static_cast<int>(n->子对象.size())) return nullptr;
    return n->子对象[static_cast<size_t>(i)];
}
HB_API hb_node* hb_node_parent(const hb_node* n) { return n ? n->父亲 : nullptr; }
HB_API const char* hb_node_name(const hb_node* n) { return n ? n->名字.c_str() : ""; }
HB_API void hb_node_set_alive(hb_node* n, int alive) { if (n) n->活着 = alive ? 1 : 0; }
HB_API int hb_node_alive(const hb_node* n) { return n ? n->活着 : 0; }
HB_API hb_transform* hb_node_transform(hb_node* n) { return n ? n->变换 : nullptr; }

/* `场景对象.找` —— 深度优先，**顺序确定**（不是字典序）。 */
namespace {
hb_node* 找(hb_node* n, const char* name) {
    const std::string 目标 = name ? name : "";
    for (hb_node* c : n->子对象) {
        if (c->名字 == 目标) return c;
        hb_node* r = 找(c, name);
        if (r) return r;
    }
    return nullptr;
}
int 数(hb_node* n) {
    int k = 1;
    for (hb_node* c : n->子对象) k += 数(c);
    return k;
}
}  // namespace

HB_API hb_node* hb_scene_find(const hb_scene* s, const char* name) {
    if (!s) return nullptr;
    const std::string 目标 = name ? name : "";
    for (hb_node* r : s->根) {
        if (r->名字 == 目标) return r;
        hb_node* h = 找(r, name);
        if (h) return h;
    }
    return nullptr;
}

HB_API int hb_scene_node_count(const hb_scene* s) {
    if (!s) return 0;
    int k = 0;
    for (hb_node* r : s->根) k += 数(r);
    return k;
}

HB_API int hb_scene_alive_root_count(const hb_scene* s) {
    if (!s) return 0;
    int k = 0;
    for (hb_node* r : s->根) if (r->活着) ++k;
    return k;
}

/* `场景.更新` 的清理段：**只清根**（组件的 `卸载时` 不许改父列表）。 */
HB_API void hb_scene_cull_dead(hb_scene* s) {
    if (!s) return;
    bool 有死的 = false;
    for (hb_node* r : s->根) if (!r->活着) { 有死的 = true; break; }
    if (!有死的) return;
    std::vector<hb_node*> 留;
    for (hb_node* r : s->根) if (r->活着) 留.push_back(r);
    s->根.swap(留);
}

}  // extern "C"

/* ══════════════════════════════════════════════════════════════════════════
 * 组件的数值行为 —— 对应 components.py
 * ══════════════════════════════════════════════════════════════════════════ */

extern "C" {

HB_API hb_mover hb_mover_new(double vx, double vy) {
    hb_mover m{};
    m.vx = vx; m.vy = vy; m.has_bounds = 0; m.lo = 0.0; m.hi = 0.0;
    return m;
}
HB_API void hb_mover_set_bounds(hb_mover* m, double lo, double hi) {
    if (!m) return;
    m->has_bounds = 1; m->lo = lo; m->hi = hi;
}

/* Python::匀速移动.每帧
 *     t.位置 = 向量(t.位置.x + self.vx*dt, t.位置.y + self.vy*dt)
 *     if 边界 is not None:
 *         if   t.位置.x <= 小: t.位置 = 向量(小, y);        self.vx =  abs(self.vx)
 *         elif t.位置.x >= 大: t.位置 = 向量(大, y);        self.vx = -abs(self.vx)
 *         if   t.位置.y <= 小: t.位置 = 向量(x, 小);        self.vy =  abs(self.vy)
 *         elif t.位置.y >= 大: t.位置 = 向量(x, 大);        self.vy = -abs(self.vy)
 * ⚠ y 那两个分支用的是**x 夹取之后**的 `t.位置.x` —— 顺序不能调。
 */
HB_API void hb_mover_step(hb_mover* m, hb_transform* t, double dt) {
    if (!m || !t) return;
    hb_vec2 p = hb_transform_pos(t);
    hb_transform_set_pos(t, p.x + m->vx * dt, p.y + m->vy * dt);
    if (!m->has_bounds) return;

    hb_vec2 q = hb_transform_pos(t);
    if (q.x <= m->lo) {
        hb_transform_set_pos(t, m->lo, q.y);
        m->vx = std::fabs(m->vx);
    } else if (q.x >= m->hi) {
        hb_transform_set_pos(t, m->hi, q.y);
        m->vx = -std::fabs(m->vx);
    }
    q = hb_transform_pos(t);
    if (q.y <= m->lo) {
        hb_transform_set_pos(t, q.x, m->lo);
        m->vy = std::fabs(m->vy);
    } else if (q.y >= m->hi) {
        hb_transform_set_pos(t, q.x, m->hi);
        m->vy = -std::fabs(m->vy);
    }
}

/* Python::按键驱动.每帧（方向为零时**直接返回**，不碰变换） */
HB_API void hb_keymove_step(hb_transform* t, double speed, hb_vec2 dir, double dt,
                            int has_bounds, double lo, double hi) {
    if (!t) return;
    if (dir.x == 0.0 && dir.y == 0.0) return;
    hb_vec2 p = hb_transform_pos(t);
    hb_vec2 新 = hb_vec_add(p, hb_vec_mul(dir, speed * dt));
    if (has_bounds) {
        新.x = 双精度最小(双精度最大(新.x, lo), hi);
        新.y = 双精度最小(双精度最大(新.y, lo), hi);
    }
    hb_transform_set_pos(t, 新.x, 新.y);
}

HB_API hb_gravity hb_gravity_new(double accel, double ground) {
    hb_gravity g{};
    g.accel = accel; g.ground = ground; g.vy = 0.0; g.on_ground = 1;
    return g;
}

/* Python::重力.每帧 */
HB_API void hb_gravity_step(hb_gravity* g, hb_transform* t, double dt) {
    if (!g || !t) return;
    g->vy += g->accel * dt;
    hb_vec2 p = hb_transform_pos(t);
    double y = p.y + g->vy * dt;
    if (y <= g->ground) {
        y = g->ground;
        g->vy = 0.0;
        g->on_ground = 1;
    } else {
        g->on_ground = 0;
    }
    hb_transform_set_pos(t, p.x, y);
}

/* Python::重力.跳 —— 不在地上就不跳（避免空中连跳） */
HB_API int hb_gravity_jump(hb_gravity* g, double 初速) {
    if (!g) return 0;
    if (!g->on_ground) return 0;
    g->vy = 初速;
    g->on_ground = 0;
    return 1;
}

HB_API hb_timer hb_timer_new(double interval) {
    hb_timer t{};
    t.interval = interval; t.accum = 0.0; t.count = 0;
    return t;
}

/* Python::计时器.每帧 —— 返回这一帧该触发几次（调用方据此回调几次） */
HB_API int hb_timer_step(hb_timer* t, double dt) {
    if (!t || t->interval <= 0.0) return 0;
    t->accum += dt;
    int n = 0;
    while (t->accum >= t->interval) {
        t->accum -= t->interval;
        t->count += 1;
        ++n;
    }
    return n;
}

HB_API hb_blink hb_blink_new(double period) {
    hb_blink b{};
    b.period = period; b.accum = 0.0; b.phase = 0;
    return b;
}

/* Python::闪烁.每帧 —— ⚠ 每帧**最多翻转一次**（不是 while） */
HB_API int hb_blink_step(hb_blink* b, double dt) {
    if (!b || b->period <= 0.0) return 0;
    b->accum += dt;
    if (b->accum >= b->period) {
        b->accum -= b->period;
        b->phase = b->phase ? 0 : 1;
        return 1;
    }
    return 0;
}

HB_API hb_trail hb_trail_new(double interval) {
    hb_trail t{};
    t.interval = interval; t.accum = 0.0;
    return t;
}

/* Python::尾迹.每帧 —— 返回 1 = 这一帧该留一个痕 */
HB_API int hb_trail_step(hb_trail* t, double dt) {
    if (!t || t->interval <= 0.0) return 0;
    t->accum += dt;
    if (t->accum < t->interval) return 0;
    t->accum -= t->interval;
    return 1;
}

/* Python::圆形碰撞.碰到 —— `math.hypot(...) <= 半径和`（**闭区间**，相接算碰到） */
HB_API int hb_circle_hit(hb_vec2 a, double ra, hb_vec2 b, double rb) {
    return std::hypot(a.x - b.x, a.y - b.y) <= (ra + rb) ? 1 : 0;
}

}  // extern "C"

/* ══════════════════════════════════════════════════════════════════════════
 * 画笔 + 颜色 —— 对应 scene.py::画笔接口 / stage.py::color_of
 * ══════════════════════════════════════════════════════════════════════════ */

struct hb_brush {
    const hb_canvas* 画布 = nullptr;
    const hb_camera* 相机 = nullptr;
};

extern "C" {

HB_API hb_brush* hb_brush_new(const hb_canvas* canvas, const hb_camera* cam) {
    hb_brush* b = new hb_brush();
    b->画布 = canvas;
    b->相机 = cam;
    return b;
}
HB_API void hb_brush_free(hb_brush* b) { delete b; }

/* Python::画笔接口.圆 —— ⚠ 半径要乘**相机缩放**（相机为空时 1.0） */
HB_API void hb_brush_circle(hb_brush* b, double x, double y, double r, const char* color) {
    if (!b || !b->画布 || !b->画布->circle) return;
    const hb_vec2 s = hb_camera_world_to_screen(b->相机, x, y);
    const double k = b->相机 ? hb_camera_scale(b->相机) : 1.0;
    b->画布->circle(b->画布->user, s.x, s.y, r * k, color);
}

/* Python::画笔接口.矩形 —— 宽高乘 k */
HB_API void hb_brush_rect(hb_brush* b, double x, double y, double w, double h, const char* color) {
    if (!b || !b->画布 || !b->画布->rect) return;
    const hb_vec2 s = hb_camera_world_to_screen(b->相机, x, y);
    const double k = b->相机 ? hb_camera_scale(b->相机) : 1.0;
    b->画布->rect(b->画布->user, s.x, s.y, w * k, h * k, color);
}

/* Python::画笔接口.线 —— ⚠ 线宽**不**乘 k（原文如此） */
HB_API void hb_brush_line(hb_brush* b, double x1, double y1, double x2, double y2,
                          const char* color, double width) {
    if (!b || !b->画布 || !b->画布->line) return;
    const hb_vec2 a = hb_camera_world_to_screen(b->相机, x1, y1);
    const hb_vec2 c = hb_camera_world_to_screen(b->相机, x2, y2);
    b->画布->line(b->画布->user, a.x, a.y, c.x, c.y, color, width);
}

/* Python::画笔接口.文字 —— ⚠ 字号**不**乘 k（原文如此） */
HB_API void hb_brush_text(hb_brush* b, double x, double y, const char* content,
                          const char* color, int size) {
    if (!b || !b->画布 || !b->画布->text) return;
    const hb_vec2 s = hb_camera_world_to_screen(b->相机, x, y);
    b->画布->text(b->画布->user, s.x, s.y, content, color, size);
}

/* Python::stage.color_of
 *     key = name.strip().lower()
 *     return colors.get(name.strip(), colors.get(key, name))    ← 未知时返回**原串**
 */
namespace {
struct 色项 { const char* 名; const char* 值; };
const 色项 色表[] = {
    {"黑", "#000000"}, {"白", "#ffffff"}, {"红", "#e5484d"}, {"绿", "#30a46c"},
    {"蓝", "#0091ff"}, {"黄", "#ffd60a"}, {"橙", "#ff8b00"}, {"紫", "#8e4ec6"},
    {"灰", "#8b8d98"}, {"粉", "#e93d82"}, {"青", "#00b8d9"}, {"棕", "#a18072"},
    {"black", "#000000"}, {"white", "#ffffff"}, {"red", "#e5484d"}, {"green", "#30a46c"},
    {"blue", "#0091ff"}, {"yellow", "#ffd60a"}, {"orange", "#ff8b00"}, {"purple", "#8e4ec6"},
    {"gray", "#8b8d98"}, {"grey", "#8b8d98"}, {"pink", "#e93d82"}, {"cyan", "#00b8d9"},
    {"brown", "#a18072"},
};
const char* 查表(const std::string& k) {
    for (const 色项& e : 色表) {
        if (k == e.名) return e.值;
    }
    return nullptr;
}
std::string 两侧去空白(const char* s) {
    std::string r = s ? s : "";
    size_t b = 0, e = r.size();
    while (b < e && (r[b] == ' ' || r[b] == '\t' || r[b] == '\n' || r[b] == '\r')) ++b;
    while (e > b && (r[e - 1] == ' ' || r[e - 1] == '\t' || r[e - 1] == '\n' || r[e - 1] == '\r')) --e;
    return r.substr(b, e - b);
}
}  // namespace

HB_API void hb_color(const char* name, char* out, size_t out_size) {
    if (!out || out_size == 0) return;
    const std::string 原 = name ? name : "";
    const std::string 去空 = 两侧去空白(name);
    const char* 命中 = 查表(去空);
    if (!命中) 命中 = 查表(小写(去空.c_str()));
    const std::string 结果 = 命中 ? std::string(命中) : 原;
    const size_t n = 双精度最小(static_cast<double>(结果.size()), static_cast<double>(out_size - 1));
    std::memcpy(out, 结果.data(), static_cast<size_t>(n));
    out[static_cast<size_t>(n)] = '\0';
}

}  // extern "C"
