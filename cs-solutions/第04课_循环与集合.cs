// 第 04 课参考答案
static int 求和(List<int> 数)
{
    int 和 = 0;                 // ⚠ 必须初始化，否则空列表会出问题
    foreach (var x in 数) 和 += x;
    return 和;
}

static List<int> 只要偶数(List<int> 数)
{
    var 出 = new List<int>();   // 空列表，不是 null
    foreach (var x in 数)
        if (x % 2 == 0) 出.Add(x);
    return 出;
}
