// 第 24 课「偷懒但输出对」的解法：素数个数对每个数单独试除。
// 小 n 全对 —— 但 n = 1000000 时本机要 50 秒，撞上运行器超时。
int 素数个数(int n)
{
    int 个数 = 0;
    for (int i = 2; i <= n; i++) {
        bool 是 = true;
        for (int d = 2; (long long)d * d <= i; d++) {
            if (i % d == 0) { 是 = false; break; }
        }
        if (是) { 个数++; }
    }
    return 个数;
}

bool 是质数(int n)
{
    if (n < 2) { return false; }
    for (int i = 2; (long long)i * i <= n; i++) {
        if (n % i == 0) { return false; }
    }
    return true;
}
