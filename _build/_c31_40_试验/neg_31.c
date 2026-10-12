#include <stdio.h>
int 括号匹配(const char *s) {
    int 开 = 0;
    for (int i = 0; s[i]; i++) { if (s[i]=='('||s[i]=='[') 开++; else 开--; }
    return 开 == 0;
}
int main(void) {
    int 栈[100]; int 栈顶 = 0;
    for (int v = 1; v <= 3; v++) { 栈[栈顶]=v; 栈顶++; }
    while (栈顶 > 0) { 栈顶--; printf("弹出 = %d\n", 栈[栈顶]); }
    int 队[100]; int 队头=0, 队尾=0;
    for (int v = 1; v <= 3; v++) { 队[队尾]=v; 队尾++; }
    while (队头 < 队尾) { printf("出队 = %d\n", 队[队头]); 队头++; }
    const char *例[3] = {"([])", "([)]", "(("};
    for (int i = 0; i < 3; i++) printf("%s %s\n", 例[i], 括号匹配(例[i]) ? "匹配" : "不匹配");
    return 0;
}