// HybridEngineLight.Parser —— 颜色名 → RGB
//
// 与 Python 版共用同一张颜色表（中英文都能写）。
// 单独一个文件是因为它有两个使用者：Stage（画图）与 Explain（报错里给建议时举例）。
//
// 未知颜色名**不报错** —— 返回一个显眼的品红，让新手一眼看出"颜色写错了"，
// 而不是让整个程序崩掉。崩掉是最没有教育意义的失败方式。

using System;
using System.Collections.Generic;
using System.Globalization;

namespace HybridEngine-light;

public static class Parser
{
    /// <summary>新手最常用的颜色，中英文都能写。</summary>
    public static readonly IReadOnlyDictionary<string, (byte R, byte G, byte B)> Colors =
        new Dictionary<string, (byte, byte, byte)>
        {
            ["黑"] = (0x00, 0x00, 0x00), ["白"] = (0xff, 0xff, 0xff),
            ["红"] = (0xe5, 0x48, 0x4d), ["绿"] = (0x30, 0xa4, 0x6c),
            ["蓝"] = (0x00, 0x91, 0xff), ["黄"] = (0xff, 0xd6, 0x0a),
            ["橙"] = (0xff, 0x8b, 0x00), ["紫"] = (0x8e, 0x4e, 0xc6),
            ["灰"] = (0x8b, 0x8d, 0x98), ["粉"] = (0xe9, 0x3d, 0x82),
            ["青"] = (0x00, 0xb8, 0xd9), ["棕"] = (0xa1, 0x80, 0x72),
            ["black"] = (0x00, 0x00, 0x00), ["white"] = (0xff, 0xff, 0xff),
            ["red"] = (0xe5, 0x48, 0x4d), ["green"] = (0x30, 0xa4, 0x6c),
            ["blue"] = (0x00, 0x91, 0xff), ["yellow"] = (0xff, 0xd6, 0x0a),
            ["orange"] = (0xff, 0x8b, 0x00), ["purple"] = (0x8e, 0x4e, 0xc6),
            ["gray"] = (0x8b, 0x8d, 0x98), ["grey"] = (0x8b, 0x8d, 0x98),
            ["pink"] = (0xe9, 0x3d, 0x82), ["cyan"] = (0x00, 0xb8, 0xd9),
            ["brown"] = (0xa1, 0x80, 0x72),
        };

    /// <summary>颜色写错时用的显眼色（品红），让新手一眼看出问题。</summary>
    public static readonly (byte R, byte G, byte B) Unknown = (0xff, 0x00, 0xff);

    /// <summary>把颜色名（中/英）或 #RRGGBB 统一成 RGB。</summary>
    public static (byte R, byte G, byte B) Color(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Colors["黑"];
        string key = name.Trim();
        if (Colors.TryGetValue(key, out var c)) return c;
        if (Colors.TryGetValue(key.ToLowerInvariant(), out c)) return c;

        // #RRGGBB
        if (key.Length == 7 && key[0] == '#'
            && byte.TryParse(key.AsSpan(1, 2), NumberStyles.HexNumber, null, out var r)
            && byte.TryParse(key.AsSpan(3, 2), NumberStyles.HexNumber, null, out var g)
            && byte.TryParse(key.AsSpan(5, 2), NumberStyles.HexNumber, null, out var b))
            return (r, g, b);

        return Unknown;   // 不报错 —— 见文件头说明
    }

    /// <summary>这个颜色名认得出来吗（Explain 用它给建议）。</summary>
    public static bool IsKnown(string? name) => name is not null && Color(name) != Unknown;
}
