#include <stdio.h>
int *造数组(void) { int a[3] = {1,2,3}; return a; }
int main(void) { int *p = 造数组(); printf("%d\n", p[0]); return 0; }