// 第 05 课参考答案
static int 平方(int x)
{
    return x * x;
}

static void 加倍(ref int x)
{
    // ref 让方法能改到**外面那个变量**，而不是一份拷贝
    x = x * 2;
}

static bool 试着转成整数(string s, out int 值)
{
    // out 参数要求方法里必须赋值 —— TryParse 正好两个要求都满足
    return int.TryParse(s, out 值);
}
