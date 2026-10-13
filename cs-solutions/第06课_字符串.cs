// 第 06 课参考答案
static string 反转(string s)
{
    // 字符串是不可变的 ⇒ 这里其实是**造了一个新的**字符串
    return new string(s.Reverse().ToArray());
}

static int 数元音(string s)
{
    int n = 0;
    foreach (var c in s.ToLower())          // ⚠ 先转小写，才能不分大小写
        if ("aeiou".IndexOf(c) >= 0) n++;
    return n;
}
