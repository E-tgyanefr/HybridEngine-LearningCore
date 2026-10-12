#include <stdio.h>
#include <string.h>
int 路径数(int 行, int 列) {
    int dp[100][100] = {0};
    for (int j = 0; j < 列; j++) dp[0][j] = 1;
    for (int i = 0; i < 行; i++) dp[i][0] = 1;
    for (int i = 1; i < 行; i++)
        for (int j = 1; j < 列; j++)
            dp[i][j] = dp[i][j-1] + dp[i][j-1];
    return dp[行-1][列-1];
}
int 最长公共子序列(const char *a, const char *b) {
    int n = (int)strlen(a), m = (int)strlen(b);
    int dp[100][100] = {0};
    for (int i = 1; i <= n; i++)
        for (int j = 1; j <= m; j++)
            if (a[i-1] == b[j-1]) dp[i][j] = dp[i-1][j-1];
            else { int 上 = dp[i-1][j], 左 = dp[i][j-1]; dp[i][j] = 上 > 左 ? 上 : 左; }
    return dp[n][m];
}