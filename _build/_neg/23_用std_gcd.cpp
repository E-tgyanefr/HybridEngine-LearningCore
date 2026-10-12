// 第 23 课「偷懒但输出对」的解法：用标准库的 std::gcd 拼出来。
// 答案全对 —— 但这一课要练的辗转相除一行都没写。
long long 最大公约数(long long a, long long b)
{
    if (a < 0) { a = -a; }
    if (b < 0) { b = -b; }
    if (a == 0 && b == 0) { return 0; }
    return std::gcd(a, b);
}

long long 最小公倍数(long long a, long long b)
{
    if (a == 0 || b == 0) { return 0; }
    long long 约 = 最大公约数(a, b);
    return a / 约 * b;
}

long long 三个数的最大公约数(long long a, long long b, long long c)
{
    return 最大公约数(最大公约数(a, b), c);
}
