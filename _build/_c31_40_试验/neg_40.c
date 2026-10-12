#include <stdio.h>
#define 点数 5
#define 边数 7
#define 无穷 1000000000
int 边U[7]={0,2,2,1,0,1,3}, 边V[7]={1,1,3,4,2,3,4}, 边W[7]={4,1,5,9,1,4,1};
int 距离[点数];
int main(void) {
    for (int i = 0; i < 点数; i++) 距离[i] = 无穷;
    距离[0] = 0;
    for (int i = 0; i < 边数; i++) {
        int u=边U[i], v=边V[i], w=边W[i];
        if (距离[u] != 无穷 && 距离[u] + w < 距离[v]) 距离[v] = 距离[u] + w;
    }
    for (int i = 0; i < 点数; i++) printf("dist[%d] = %d\n", i, 距离[i]);
    return 0;
}