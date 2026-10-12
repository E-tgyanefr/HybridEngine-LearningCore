#include <stdio.h>
void 打印(int a[]) { printf("元素个数 = %d\n", (int)sizeof(a)); }
int main(void) { int a[10]; 打印(a); return 0; }