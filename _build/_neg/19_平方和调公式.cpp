// 第 19 课「偷懒但输出对」的解法：平方和直接返回公式的答案。
// 三个函数答案全对，但"O(n²) 有多慢"这件事学生一点没看见。
long long 一重和(long long n)
{
    long long 累加 = 0;
    for (long long i = 1; i <= n; i++) { 累加 += i; }
    return 累加;
}

long long 公式(long long n)
{
    return n * (n + 1) / 2;
}

long long 平方和(long long n)
{
    return 公式(n);
}
