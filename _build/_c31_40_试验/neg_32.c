#include <stdio.h>
#include <stdlib.h>
typedef struct 节点 { int 值; struct 节点 *下一个; } 节点;
节点 *造节点(int 值) { 节点 *新 = malloc(sizeof(节点)); 新->值=值; 新->下一个=NULL; return 新; }
void 头插(节点 **头, int 值) {
    节点 *新 = 造节点(值);
    *头 = 新;
    新->下一个 = *头;
}
void 打印链表(节点 *头) {
    节点 *走 = 头; int 先 = 1;
    while (走) { if (!先) printf(" "); printf("%d", 走->值); 先 = 0; 走 = 走->下一个; }
    printf("\n");
}
int main(void) {
    节点 *头 = NULL;
    头插(&头, 30); 头插(&头, 20); 头插(&头, 10);
    打印链表(头);
    头插(&头, 5);
    打印链表(头);
    打印链表(头);
    头 = NULL;
    return 0;
}