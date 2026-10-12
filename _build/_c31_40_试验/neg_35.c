#include <stdio.h>
int 爬楼梯(int n) {
    if (n <= 0) return 0;
    if (n == 1) return 1;
    int dp[100] = {0};
    dp[1] = 1; dp[2] = 1;
    for (int i = 3; i <= n; i++) dp[i] = dp[i-1] + dp[i-2];
    return dp[n];
}
int 打家劫舍(int 钱[], int 家数) {
    if (家数 <= 0) return 0;
    int 总 = 0, i = 0;
    while (i < 家数) {
        int 拿 = i, j = i + 1;
        if (j < 家数 && 钱[j] > 钱[拿]) 拿 = j;
        总 += 钱[拿];
        i = 拿 + 2;
    }
    return 总;
}
int 背包(int 重量[], int 价值[], int 件数, int 容量) {
    int dp[1000] = {0};
    for (int i = 0; i < 件数; i++)
        for (int j = 重量[i]; j <= 容量; j++) {
            int 拿 = dp[j - 重量[i]] + 价值[i];
            if (拿 > dp[j]) dp[j] = 拿;
        }
    return dp[容量];
}