#include <stdio.h>
#include <stdlib.h>
int main(void) {
    int *p = malloc(10 * sizeof(int));
    printf("sizeof(p) = %d\n", (int)sizeof(p));
    for (int i = 0; i < (int)sizeof(p); i++) p[i] = i;
    free(p);
    return 0;
}