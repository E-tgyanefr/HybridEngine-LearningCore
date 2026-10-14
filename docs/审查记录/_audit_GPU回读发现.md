# ⚠⚠ 架构发现：引擎的 GPU 路径**每帧做一次 GPU→CPU 回读**（2026-10-14 · Round 9）

> 这一份是**看代码看出来的**，而它**推翻了我上一份探针的口径**。
> ⚠ 我是从"作者的 10 分钟视频任务"倒推去查真路径时发现的。

---

## 一、引擎的真实 GPU 路径（`src/app/engine.cpp:838`）

```cpp
// m-gpu：窗口面 GPU 渲染场景 → 读回像素 → 软渲窗口面 → HUD 叠加 → Present
gpu_->Begin(-1, -1);
RenderSceneGpu(*gpu_, drawables, &dbgGpuVP_);
gpu_->End();
...
const size_t need = (size_t)gpu_->Width() * (size_t)gpu_->Height();
if (gpuReadback_.size() < need) gpuReadback_.resize(need);   // 缓冲复用（稳态零分配）
if (gpu_->ReadbackFrame(gpuReadback_.data())) {              // ⚠⚠ **每帧 GPU→CPU 回读**
    windowRenderer_->BlitRect(0, 0, w, h, gpuReadback_.data());  // 拷回软件渲染器
    DrawSpriteItems(*windowRenderer_, Assets(), drawables.sprites);  // HUD 叠在读回的像素上
} else {
    RenderSceneDrawables(...);                                // 读回失败=软渲兜底
}
```

⇒ ⚠⚠ **每帧的完整链路是**：
```
GPU 渲染 3D 场景  →  **ReadbackFrame：像素从显存读回内存**  →  软件渲染器 BlitRect
                  →  HUD/2D 精灵叠加（软渲）  →  Present
```

---

## 二、⚠⚠ 为什么这是**对"10 分钟视频"最要紧的一件事**

### ① 我上一份探针**完全跳过了回读**

```
我的 `video_render.cpp` 用的是 `gpu.Composite()`（**直接 Present，GPU 表面**）
⇒ **没有 ReadbackFrame、没有 BlitRect、没有 HUD**
⇒ ⭐ **所以那份"2,048 模型 4.47 ms/帧"是【不含回读】的数** —— 而引擎**每帧都要回读**
```

### ② 回读本身是同步传输，代价通常很大

```
1280×720 × 4 B = **3,686,400 B ≈ 3.5 MB / 帧**
60 fps ⇒ **约 211 MB/s 的 GPU→CPU 传输**，而且是**同步**的（要等 GPU 画完才能读）
⚠ 而 D3D11 的回读（staging texture + Map）**会强制 CPU 等 GPU** ⇒ 典型的管线气泡
```

### ③ ⚠ 而它**不是 bug —— 是被架构逼出来的**

```
为什么要回读？因为 **HUD / 2D 精灵是【软件渲染器】画的**（`windowRenderer_`），
而它画在**同一块像素缓冲**上 ⇒ 必须把 GPU 画好的像素拿回 CPU 才能叠。
⇒ ⚠ 所以这不是"实现不好"，是**"GPU 3D + 软件 2D"这个组合的必然代价**
```

---

## 三、⇒ 对"10 分钟视频"的直接影响（**必须重新量**）

```
我上一份的数（不含回读）：
   2,048 模型 ⇒ 4.47 ms/帧 ⇒ 10 分钟视频约 2.7 分钟
⚠ 但引擎路径**每帧多一次 3.5 MB 回读 + 一次 BlitRect** ⇒ 真实数**必然更慢**
   ⇒ ⚠ **我不知道慢多少 —— 因为我还没测。**
```

**而修法有三条，代价很不一样**：

| 方案 | 做法 | 代价 |
|---|---|---|
| **① 减少回读频率** | HUD 不必每帧更新时就不回读 | ⚠ 但 HUD 每帧都要（分数/时间在变） |
| **② HUD 也走 GPU** | 把 2D 图层也做成 GPU 绘制（`gpu2d_renderer_win32.cpp` **已经存在**，37.5 KB！） | ⭐ **最正当** —— 那条路本来就有，引擎没让它接管窗口面 |
| **③ 回读异步化** | staging + 双缓冲，让 CPU 不等 GPU | ⚠ 会引入一帧延迟；且 HUD 叠的是**上一帧**的像素 |

⭐ **而 ② 值得单独查**：`src/platform/gpu2d_renderer_win32.cpp` **存在**，
   而 `engine.cpp:578` 的注释写着：

```
// m-gpu2d：窗口面可以走 D3D11。**parity 面（下方 349 行）永远走软件** ——
```
⇒ ⚠ **所以"窗口面走 D3D11"是已有的能力** —— 那为什么 `engine.cpp:838` 那条路还在回读？
  ⇒ **这一条我没查清**（上下文用完了）⇒ **如实记：待查**

---

## 四、⚠ 没验到的（逐条）

1. **回读的实际 ms/帧** —— **没测**（我加 `--readback` 时猜错了 API：`PresentPixels` 不存在，
   正确的是 `IWindow::Present(const uint32_t*)` 或 `GpuRenderer::Composite`）
2. **"窗口面能不能完全走 GPU（含 HUD）"** —— 没查（见第三节 ②）
3. **异步回读的可能性** —— 没查
4. **`Composite()` 与 `ReadbackFrame+Present` 两条路的像素是否逐位相同** —— 没验

---

## 五、⭐ 这一轮的元教训

> **我上一份探针报了一个"可行"的结论（2,048 模型 ⇒ 10 分钟视频 2.7 分钟），
> 而它测的是一条【引擎不走的】路径。**

⚠ 原因不是"我编了数"，是**我量了一个自认为有代表性的入口**（`GpuRenderer::Submit/Composite`），
  而**没先去读引擎到底怎么用这个接口**。

⇒ ⭐ **规矩**：**探针必须先用代码确认"引擎走的是哪条路"，再照着那条路写。**
  ⚠ 否则会得到"数字很漂亮、但和被测系统无关"的结果 —— 而**这比没有数更危险**
  （它会让人以为已经量过了）。
