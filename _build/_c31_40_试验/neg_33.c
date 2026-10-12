#include <stdio.h>
#include <stdlib.h>
typedef struct 树节点 { int 值; struct 树节点 *左; struct 树节点 *右; } 树节点;
树节点 *造节点(int 值) { 树节点 *新 = malloc(sizeof(树节点)); if (新 == NULL) return NULL; 新->值=值; 新->左=NULL; 新->右=NULL; return 新; }
void 前序(树节点 *根) { if (根 == NULL) return; printf("%d ", 根->值); 前序(根->左); 前序(根->右); }
void 中序(树节点 *根) { if (根 == NULL) return; printf("%d ", 根->值); 中序(根->左); 中序(根->右); }
void 后序(树节点 *根) { if (根 == NULL) return; 后序(根->左); 后序(根->右); printf("%d ", 根->值); }
int main(void) {
    树节点 *一=造节点(1),*二=造节点(2),*三=造节点(3),*四=造节点(4),*五=造节点(5),*六=造节点(6);
    一->左=二; 一->右=三; 二->左=四; 二->右=五; 三->右=六;
    printf("前序: "); 前序(一); printf("\n");
    printf("中序: "); 中序(一); printf("\n");
    printf("后序: "); 后序(一); printf("\n");
    return 0;
}