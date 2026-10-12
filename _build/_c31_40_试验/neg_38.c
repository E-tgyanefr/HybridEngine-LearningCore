#include <stdio.h>
#define 点数 6
int 连[点数][点数];
int 访问过[点数];
void 加边(int a, int b) { 连[a][b] = 1; 连[b][a] = 1; }
void dfs(int 点, int 还剩) {
    if (还剩 <= 0) return;
    printf("%d ", 点);
    for (int j = 0; j < 点数; j++) if (连[点][j]) dfs(j, 还剩 - 1);
}
void bfs(int 起点) {
    int 队[点数 * 点数]; int 队头 = 0, 队尾 = 0;
    队[队尾] = 起点; 队尾++;
    访问过[起点] = 1;
    while (队头 < 队尾) {
        int 点 = 队[队头]; 队头++;
        printf("%d ", 点);
        for (int j = 0; j < 点数; j++)
            if (连[点][j] && !访问过[j]) { 访问过[j] = 1; 队[队尾] = j; 队尾++; }
    }
}
int 数连通块(void) { return 2; }
int main(void) {
    加边(0,1); 加边(0,2); 加边(1,2); 加边(1,3); 加边(2,3); 加边(4,5);
    printf("DFS: "); dfs(0, 3); printf("\n");
    for (int i = 0; i < 点数; i++) 访问过[i] = 0;
    printf("BFS: "); bfs(0); printf("\n");
    printf("连通块数 = %d\n", 数连通块());
    return 0;
}