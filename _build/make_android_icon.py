"""给安卓工程生成应用图标（零依赖，标准库直接写 PNG）。

⚠ 为什么要自己生成：`AndroidManifest` 里写了 `Icon = "@mipmap/icon"`，
  而工程里没有这个资源 ⇒
       error APT2260: resource mipmap/icon not found
  （**这个错误只在路径是 ASCII 时才露出来** —— 中文路径会先撞 APT2265，
    把后面的错全挡住。所以"搬到 ASCII 路径构建"不只是绕过限制，
    也是**为了能看到真正的错误**。）

⚠ mipmap-* 的尺寸照 Android 官方密度档：
      mdpi 48 · hdpi 72 · xhdpi 96 · xxhdpi 144 · xxxhdpi 192
  自适应图标（API 26+）用 108dp 的画布，前景安全区是中间 72dp ——
  这里生成的是**传统图标**（各密度一份），够用且不引入 XML 资源。
"""
import io
import os
import struct
import sys
import zlib

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(LC, "android", "app", "Resources")

#: 密度 → 边长（px）
密度 = {"mdpi": 48, "hdpi": 72, "xhdpi": 96, "xxhdpi": 144, "xxxhdpi": 192}


def 写PNG(路径, w, h, 像素):
    """像素 = 逐行 bytes（每行前面加 filter 字节 0）。"""
    raw = b"".join(b"\x00" + 行 for 行 in 像素)

    def 块(类型, 数据):
        c = 类型 + 数据
        return (struct.pack(">I", len(数据)) + c
                + struct.pack(">I", zlib.crc32(c) & 0xFFFFFFFF))

    io.open(路径, "wb").write(
        b"\x89PNG\r\n\x1a\n"
        + 块(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))   # 8bit RGB
        + 块(b"IDAT", zlib.compress(raw, 9))
        + 块(b"IEND", b""))


def 画图标(n: int):
    """深底 + 圆角 + 中间一个「</>」形状的亮块。

    纯手工画像素 —— 不引 Pillow（那会破坏本项目的零依赖红线）。
    形状用**距离场**算，这样任意尺寸都干净（不用为每个密度各写一套）。
    """
    底 = (30, 31, 34)          # #1e1f22，和界面同色
    亮 = (74, 158, 255)        # #4a9eff，界面的强调色
    白 = (214, 214, 214)       # #d6d6d6

    圆角 = n * 0.22            # iOS/Android 常见的圆角比例
    笔画 = max(1.6, n * 0.055)  # 笔画粗细随尺寸缩放

    行们 = []
    for y in range(n):
        #: 每行存 RGB 三元组
        行 = []
        for x in range(n):
            # ── 圆角矩形遮罩（四个角各算一次距离）──
            cx = min(x, n - 1 - x)
            cy = min(y, n - 1 - y)
            if cx < 圆角 and cy < 圆角:
                d = ((圆角 - cx) ** 2 + (圆角 - cy) ** 2) ** 0.5
                if d > 圆角:
                    行.append((0, 0, 0))
                    continue
            行.append(底)

        # ── 「<」和「>」：两条斜线，用点到线段的距离 ──
        #    归一化坐标，留出边距
        for x in range(n):
            u = x / (n - 1)
            v = y / (n - 1)
            c = 底
            # 左边那条「<」：从 (0.40,0.28) 到 (0.24,0.50) 再到 (0.40,0.72)
            for (x1, y1, x2, y2) in ((0.40, 0.27, 0.235, 0.50),
                                     (0.235, 0.50, 0.40, 0.73),
                                     (0.60, 0.27, 0.765, 0.50),
                                     (0.765, 0.50, 0.60, 0.73)):
                # 点到线段距离
                dx, dy = x2 - x1, y2 - y1
                L2 = dx * dx + dy * dy
                t = 0.0 if L2 == 0 else max(0.0, min(1.0, ((u - x1) * dx + (v - y1) * dy) / L2))
                px, py = x1 + t * dx, y1 + t * dy
                dist = ((u - px) ** 2 + (v - py) ** 2) ** 0.5
                # 归一化距离 → 像素距离（避免细尺寸下笔画消失）
                半 = (笔画 / 2) / (n - 1)
                if dist <= 半:
                    c = 亮
                    break
                if dist <= 半 * 1.9:          # 一点柔边，小尺寸下不至于锯齿难看
                    k = (dist - 半) / (半 * 0.9)
                    c = tuple(int(亮[i] + (底[i] - 亮[i]) * k) for i in range(3))
            # 中间的竖线（代表"光标"）
            if abs(u - 0.5) <= (笔画 / 2) / (n - 1) * 0.9 and 0.30 <= v <= 0.70:
                c = 白
            行[x] = c
        行们.append(b"".join(bytes(p) for p in 行))
    return 行们


print("  === 生成图标 ===")
for 名, n in 密度.items():
    目录 = os.path.join(RES, "mipmap-" + 名)
    os.makedirs(目录, exist_ok=True)
    P = os.path.join(目录, "icon.png")
    写PNG(P, n, n, 画图标(n))
    print("    mipmap-%-8s icon.png  %3d×%-3d  %5d 字节" % (名, n, n, os.path.getsize(P)))

#: ⚠ 顺便放一份到 `Resources\drawable\`，因为有些 SDK 版本对
#:   `@mipmap/icon` 的解析路径不同（多一份代价只有几百字节）。
D = os.path.join(RES, "drawable")
os.makedirs(D, exist_ok=True)
写PNG(os.path.join(D, "icon.png"), 192, 192, 画图标(192))
print("    drawable         icon.png  192×192（备用）")
print()
print("  产出在：%s" % RES)
