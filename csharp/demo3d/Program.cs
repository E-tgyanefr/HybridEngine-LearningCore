// Program.cs —— 3D 实时渲染演示（旋转立方体 + 光照 + 透视相机）
//
// ══ 这个示例要证明什么 ══
//
// 证明引擎的 3D 链路是通的、可用的，并且**能量化**：
//   · 每帧真的在重绘（不是静态图）
//   · 帧率、三角形数、绘制调用都能读出来（ms_engine_render3d_stats）
//   · 光照、相机、材质都参与计算
//
// ══ 为什么不用 .obj 文件 ══
//
// 因为这次要验证的是**渲染链路**，不是资源管线。
// 手写顶点数据能去掉"文件读不到"这个干扰因素 ——
// 出错时能确定是渲染的问题，而不是模型路径的问题。
// （引擎的 obj_asset.cpp / mesh_asset.cpp 是另一条路，可以后面单独验。）

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

internal static class Program
{
    // ── C ABI（只声明这个演示用到的）──
    private const string DLL = "hybridengine.dll";

    [StructLayout(LayoutKind.Sequential)]
    private struct ms_engine { public IntPtr p; }

    // ⚠ 正确签名是 (title, width, height, int* err) —— **没有描述符参数**。
    //   我第一版凭印象写成了 ms_engine_create(desc)，传了 IntPtr.Zero：
    //   结果 title=NULL、width=0、height=0，而引擎居然还是建出来了（离屏模式），
    //   所以错误一直没暴露 —— 直到我看性能不对才回头查签名。
    //   教训：**C ABI 的签名必须从头文件抄**，传错参数不一定立刻崩。
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ms_engine_create(string title, int width, int height, out int err);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_engine_render(IntPtr e, IntPtr rendererCtx);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_engine_pump(IntPtr e);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_engine_should_close(IntPtr e);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ms_engine_destroy(IntPtr e);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ms_engine_scene(IntPtr e);

    // ⚠ 没有 ms_scene_go_new 这个函数（我第一版凭印象写的，运行时报
    //   EntryPointNotFoundException）。实际是两步：
    //     ms_scene_add_root 建对象并给出 id  →  ms_scene_go_get 用 id 换指针
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_scene_add_root(IntPtr e, IntPtr scene, string name, out ulong outId);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_scene_go_get(IntPtr e, IntPtr scene, ulong id, out IntPtr outGo);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_go_add_mesh_visual(IntPtr e, IntPtr g);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_go_set_mesh(IntPtr e, IntPtr g, double[] vertsXYZ, int triCount, uint color);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_go_add_camera(IntPtr e, IntPtr g);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_camera_set(IntPtr e, IntPtr g, double[] eye, double[] target,
                                            double fovDeg, int ortho, double orthoSize);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_go_add_light(IntPtr e, IntPtr g);

    // ⚠ 参数顺序是 position, direction, **color**, intensity, range, innerDeg, outerDeg。
    //   我第一版把 color 和 intensity 写反了 —— 结果那个 double[] 被当成 double 解引用，
    //   直接 AccessViolationException（进程崩）。
    //   教训：C ABI 的数组参数位置写错**不会编译报错**，只会在运行时炸，
    //   而且炸得很远（在 P/Invoke 那一层）。所以签名必须从头文件抄，不能凭印象。
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_light_set(IntPtr e, IntPtr g, int type, double[] position,
                                           double[] direction, double[] color, double intensity,
                                           double range, double innerDeg, double outerDeg);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_go_set_material(IntPtr e, IntPtr g, double specular,
                                                 double shininess, double[] emissive);

    // ⚠ ms_transform_set 是**按字段名**设的，不是一个调用设完 pos/rot/scale。
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_transform_set(IntPtr e, IntPtr g, string field, double[] value);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ms_engine_render3d_stats(IntPtr e, ulong[] outv, int cap);

    // ── 几何：一个立方体 ──
    //
    // 手写 12 个三角形（6 面 × 2）。
    // 顶点按"行序"给：每个三角形 9 个 double（3 个顶点 × xyz）。
    // 法线没给 —— 引擎会用面法线（见 ms_go_set_mesh 的注释）。
    private static double[] 立方体(double 边长)
    {
        double h = 边长 / 2;
        // 8 个角
        var v = new (double X, double Y, double Z)[]
        {
            (-h, -h, -h), ( h, -h, -h), ( h,  h, -h), (-h,  h, -h),
            (-h, -h,  h), ( h, -h,  h), ( h,  h,  h), (-h,  h,  h),
        };
        // 6 个面，每面两个三角形（逆时针朝外）
        int[][] 面 =
        {
            new[]{0,1,2}, new[]{0,2,3},   // 后
            new[]{4,6,5}, new[]{4,7,6},   // 前
            new[]{0,4,5}, new[]{0,5,1},   // 下
            new[]{3,2,6}, new[]{3,6,7},   // 上
            new[]{0,3,7}, new[]{0,7,4},   // 左
            new[]{1,5,6}, new[]{1,6,2},   // 右
        };
        var 数据 = new double[面.Length * 9];
        int k = 0;
        foreach (var 三 in 面)
            foreach (var i in 三)
            {
                数据[k++] = v[i].X;
                数据[k++] = v[i].Y;
                数据[k++] = v[i].Z;
            }
        return 数据;
    }

    /// <summary>在场景里建一个对象，返回它的指针。失败返回 IntPtr.Zero。</summary>
    private static IntPtr 建对象(IntPtr e, IntPtr scene, string 名字)
    {
        if (ms_scene_add_root(e, scene, 名字, out ulong id) != 0) return IntPtr.Zero;
        if (ms_scene_go_get(e, scene, id, out IntPtr go) != 0) return IntPtr.Zero;
        return go;
    }

    private static int Main(string[] argv)
    {
        Console.OutputEncoding = Encoding.UTF8;
        int 帧数上限 = argv.Length > 0 && int.TryParse(argv[0], out var n) ? n : 0;

        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("  HybridEngine 3D 实时渲染演示");
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine();

        // 引擎描述符：用 gpuRender（3D）—— 但结构体布局要和引擎一致，
        // 所以这里直接传 NULL 让引擎用默认值，然后靠场景里的相机/光照驱动 3D。
        int err = 0;
        // 传 0×0 → 引擎走离屏模式（无窗口），正好适合基准测试和自动化验证
        IntPtr e = ms_engine_create("3D 演示", 1280, 800, out err);
        if (e == IntPtr.Zero)
        {
            Console.WriteLine($"  引擎创建失败（err={err}）。");
            return 1;
        }
        Console.WriteLine($"  引擎已创建（离屏 1280×800，err={err}）");

        IntPtr scene = ms_engine_scene(e);
        Console.WriteLine($"  场景句柄: 0x{scene.ToInt64():X}");

        // ── 立方体 ──
        IntPtr cube = 建对象(e, scene, "立方体");
        ms_go_add_mesh_visual(e, cube);
        var 顶点 = 立方体(2.0);
        int r = ms_go_set_mesh(e, cube, 顶点, 顶点.Length / 9, 0xFF4A9EFF);   // 不透明蓝
        Console.WriteLine($"  立方体: {顶点.Length / 9} 个三角形，ms_go_set_mesh → {r}");

        // 材质：高光
        r = ms_go_set_material(e, cube, 0.6, 32.0, new double[] { 0, 0, 0 });
        Console.WriteLine($"  材质（高光 0.6 / 光泽 32）→ {r}");

        // ── 相机 ──
        IntPtr cam = 建对象(e, scene, "相机");
        ms_go_add_camera(e, cam);
        r = ms_camera_set(e, cam,
            new double[] { 4, 3, 5 },        // 眼睛位置
            new double[] { 0, 0, 0 },        // 看向原点
            50.0,                            // 视场角
            0, 0);                           // 0 = 透视投影
        Console.WriteLine($"  透视相机 (4,3,5)→原点，fov 50° → {r}");

        // ── 光照 ──
        IntPtr light = 建对象(e, scene, "主光");
        ms_go_add_light(e, light);
        r = ms_light_set(e, light,
            0,                                        // 0 = 方向光
            new double[] { 5, 8, 6 },                 // 位置
            new double[] { -0.5, -1, -0.6 },          // 方向
            new double[] { 1.0, 0.98, 0.92 },         // 暖白（0..1）
            1.2,                                      // 强度
            0, 0, 0);                                 // range / 内外角（方向光用不到）
        Console.WriteLine($"  方向光（强度 1.2，暖白）→ {r}");

        // ── 渲染循环 ──
        //
        // ⚠ 分两轮：先**预热**，再**静默测量**。
        //
        //   我第一版是"边渲染边打印统计"，结果帧时间从 32ms 一路涨到 265ms ——
        //   不是引擎变慢，是 Console.WriteLine 的 I/O 阻塞了渲染循环，
        //   而且我打印的那些数**恰好把干扰算了进去**。
        //   所有正经基准都这么做：**先预热、再静默采样**。
        const int 预热帧数 = 30;
        int 测量帧数 = 帧数上限 > 0 ? 帧数上限 : 120;

        double 角度 = 0;
        var 统计 = new ulong[8];

        Console.WriteLine($"  预热 {预热帧数} 帧（不计时）…");
        for (int i = 0; i < 预热帧数; i++)
        {
            角度 += 0.03;
            ms_transform_set(e, cube, "rot", new double[] { 角度 * 0.3, 角度, 0 });
            ms_engine_render(e, IntPtr.Zero);
        }

        Console.WriteLine($"  静默测量 {测量帧数} 帧（中途不打印）…");
        var 每帧 = new double[测量帧数];
        var 计时 = Stopwatch.StartNew();
        for (int i = 0; i < 测量帧数; i++)
        {
            double t0 = 计时.Elapsed.TotalSeconds;

            角度 += 0.03;
            ms_transform_set(e, cube, "rot", new double[] { 角度 * 0.3, 角度, 0 });
            ms_engine_render(e, IntPtr.Zero);

            每帧[i] = (计时.Elapsed.TotalSeconds - t0) * 1000;
        }
        计时.Stop();

        // ── 统计（这一轮是纯计算，不影响计时）──
        Array.Sort(每帧);
        double 总毫秒 = 0;
        foreach (var v in 每帧) 总毫秒 += v;
        double 平均 = 总毫秒 / 每帧.Length;
        double 中位 = 每帧[每帧.Length / 2];
        double p95 = 每帧[(int)(每帧.Length * 0.95)];
        double 最快 = 每帧[0];
        double 最慢 = 每帧[每帧.Length - 1];

        Console.WriteLine();
        Console.WriteLine("  ─────────────────────────────────────────────────");
        Console.WriteLine($"  测了 {测量帧数} 帧，总用时 {计时.Elapsed.TotalSeconds:F3} 秒");
        Console.WriteLine();
        Console.WriteLine("  每帧渲染耗时（毫秒）：");
        Console.WriteLine($"    平均  {平均,7:F2}     中位  {中位,7:F2}");
        Console.WriteLine($"    最快  {最快,7:F2}     P95   {p95,7:F2}     最慢  {最慢,7:F2}");
        Console.WriteLine($"    平均帧率  {1000.0 / Math.Max(0.001, 平均):F1} fps");
        Console.WriteLine();

        Array.Clear(统计, 0, 统计.Length);
        ms_engine_render3d_stats(e, 统计, 统计.Length);
        Console.WriteLine("  最终 3D 统计（ms_engine_render3d_stats）：");
        string[] 含义 = { "三角形数", "绘制调用", "?", "?", "光源数", "像素数" };
        for (int i = 0; i < 统计.Length; i++)
            if (统计[i] != 0)
                Console.WriteLine($"    [{i}] {含义[i],-10} = {统计[i]}");
        Console.WriteLine();

        ms_engine_destroy(e);
        Console.WriteLine("  引擎已销毁。");
        return 0;
    }
}
