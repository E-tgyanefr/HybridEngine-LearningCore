"""BMP → PNG，只用标准库（zlib + struct + binascii）。

⚠ 为什么需要它：
  `_shot.py` 用 GDI 抓屏存的是 **BMP** —— 因为 BMP 头我手写就行，
  而 PNG 要 zlib 压缩 + CRC32 + 分块，看着麻烦。
  但**看图工具只认 PNG/JPEG/WebP/GIF**（BMP 不行）。
  ⇒ 于是补这一步转换。

⚠ 为什么不用 PIL：项目铁律**零第三方依赖**。
  而这个转换其实不长 —— PNG 的每个块就是
  「长度(4) + 类型(4) + 数据 + CRC(4)」，CRC 用 `binascii.crc32`。
  真正的工作量只有一处：**行首那个 filter 字节**（0 = 不滤波）。
"""
from __future__ import annotations

import binascii
import struct
import sys
import zlib


def 读BMP(路径: str):
    """返回 (宽, 高, RGB 像素bytes)。只认 `_shot.py` 产的那一种（32位、自上而下）。"""
    with open(路径, "rb") as f:
        数据 = f.read()
    if 数据[:2] != b"BM":
        raise ValueError("不是 BMP")
    偏移 = struct.unpack_from("<I", 数据, 10)[0]
    宽, 高 = struct.unpack_from("<ii", 数据, 18)
    位深 = struct.unpack_from("<H", 数据, 28)[0]
    if 位深 != 32:
        raise ValueError("只支持 32 位 BMP，这个是 %d 位" % 位深)
    自上而下 = 高 < 0
    高 = abs(高)
    像素 = 数据[偏移:偏移 + 宽 * 高 * 4]
    return 宽, 高, 像素, 自上而下


def 写PNG(路径: str, 宽: int, 高: int, 像素: bytes, 自上而下: bool):
    """写成 8 位 RGB 的 PNG。

    ⚠ BMP 是 BGRA、PNG 要 RGB，而且**方向可能相反**（BMP 通常是自下而上）。
      这两件事都得转，不然图会**上下颠倒 + 红蓝互换** ——
      而那种错一眼就能看出来（肤色变蓝），所以不算隐蔽，只是烦。
    """
    行们 = []
    for y in range(高):
        真行 = y if 自上而下 else (高 - 1 - y)
        起 = 真行 * 宽 * 4
        #: 每行前面加一个 filter 字节 0（None）
        行 = bytearray([0])
        for x in range(宽):
            i = 起 + x * 4
            行 += bytes((像素[i + 2], 像素[i + 1], 像素[i]))   #: BGR → RGB
        行们.append(bytes(行))
    原 = zlib.compress(b"".join(行们), 6)

    def 块(类型: bytes, 数据: bytes) -> bytes:
        return (struct.pack(">I", len(数据)) + 类型 + 数据
                + struct.pack(">I", binascii.crc32(类型 + 数据) & 0xFFFFFFFF))

    with open(路径, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(块(b"IHDR", struct.pack(">IIBBBBB", 宽, 高, 8, 2, 0, 0, 0)))
        f.write(块(b"IDAT", 原))
        f.write(块(b"IEND", b""))


def 转(来源: str, 目标: str):
    宽, 高, 像素, 自上而下 = 读BMP(来源)
    写PNG(目标, 宽, 高, 像素, 自上而下)
    return 宽, 高


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    来源 = sys.argv[1]
    目标 = sys.argv[2] if len(sys.argv) > 2 else 来源.rsplit(".", 1)[0] + ".png"
    宽, 高 = 转(来源, 目标)
    import os
    print("  %s → %s（%dx%d，%.1f KB）"
          % (来源, 目标, 宽, 高, os.path.getsize(目标) / 1024))
