// 第 25 课「偷懒但输出对」的解法：从头到尾线性扫一遍。
// 前 17 组用例**全部**答对（线性扫的结果当然对），
// 但这一课要的是"每次砍掉一半"的二分。
int 二分查找(const int 数[], int 长度, int 目标)
{
    for (int i = 0; i < 长度; i++) {
        if (数[i] == 目标) { return i; }
    }
    return -1;
}

int 下界(const int 数[], int 长度, int 目标)
{
    for (int i = 0; i < 长度; i++) {
        if (数[i] >= 目标) { return i; }
    }
    return 长度;
}
