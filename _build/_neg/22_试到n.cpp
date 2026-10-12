// 第 22 课「偷懒但输出对」的解法：是质数一个个试到 n。
// 小数字全对 —— 但检查里喂了接近 10^9 的大质数，会撞上运行器超时。
int 因数个数(int n)
{
    int 个数 = 0;
    for (int i = 1; i <= n; i++) { if (n % i == 0) { 个数++; } }
    return 个数;
}

bool 是质数(int n)
{
    if (n < 2) { return false; }
    for (int i = 2; i < n; i++) { if (n % i == 0) { return false; } }
    return true;
}

int 质因数个数(int n)
{
    int 个数 = 0;
    int 剩 = n;
    for (int i = 2; i <= 剩; i++) {
        while (剩 % i == 0) { 个数++; 剩 /= i; }
    }
    return 个数;
}

int 最大质因数(int n)
{
    if (n < 2) { return 1; }
    int 最大 = 1;
    int 剩 = n;
    for (int i = 2; i <= 剩; i++) {
        while (剩 % i == 0) { 最大 = i; 剩 /= i; }
    }
    if (剩 > 1) { 最大 = 剩; }
    return 最大;
}
