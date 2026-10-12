// 第 20 课「偷懒但输出对」的解法：把第 11 课的递归原样搬过来。
// 小 n 全对 —— 但 n = 45 时慢到超时。
long long 爬楼梯(int n)
{
    if (n <= 1) { return 1; }
    return 爬楼梯(n - 1) + 爬楼梯(n - 2);
}

long long 斐波那契(int n)
{
    if (n <= 0) { return 0; }
    if (n == 1) { return 1; }
    return 斐波那契(n - 1) + 斐波那契(n - 2);
}

long long 斐波那契取模(int n)
{
    if (n <= 0) { return 0; }
    if (n == 1) { return 1; }
    return (斐波那契取模(n - 1) + 斐波那契取模(n - 2)) % 1000000007;
}

long long 铺地砖(int n)
{
    if (n <= 1) { return 1; }
    return 铺地砖(n - 1) + 铺地砖(n - 2);
}
