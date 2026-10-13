// 第 03 课参考答案
static string 判等级(int 分)
{
    if (分 >= 90) return "优秀";
    else if (分 >= 60) return "及格";
    else return "不及格";
}

static string 星期几(int n)
{
    // C# 的 switch 表达式：不用 break，也漏不掉
    return n switch
    {
        1 => "周一",
        2 => "周二",
        3 => "周三",
        4 => "周四",
        5 => "周五",
        6 => "周六",
        7 => "周日",
        _ => "不是星期",
    };
}
