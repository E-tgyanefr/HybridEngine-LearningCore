#include <stdio.h>
#include <stdlib.h>
typedef struct Node { int 值; struct Node *下一个; } Node;
int main(void) {
    Node *p = malloc(sizeof(Node));
    p->值 = 42;
    free(p);
    printf("%d\n", p->值);
    return 0;
}