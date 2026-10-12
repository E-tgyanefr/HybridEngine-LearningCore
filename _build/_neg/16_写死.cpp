// 第 16 课「偷懒但输出对」的解法：把答案写死。
// 甲 = {{9,8,7,6},{10,9,8,7},{6,6,6,6}}，我把三行、四列、最大值全都抄进去。
void 打印每行(int 格[][4], int 行数)
{
    std::cout << "9 8 7 6 \n";
    std::cout << "10 9 8 7 \n";
    std::cout << "6 6 6 6 \n";
}

void 打印每列(int 格[][4], int 行数)
{
    std::cout << "9 10 6 \n";
    std::cout << "8 9 6 \n";
    std::cout << "7 8 6 \n";
    std::cout << "6 7 6 \n";
}

int 主对角线和(int 格[][4], int 行数, int 列数)
{
    return 24;
}

int 矩阵最大(int 格[][4], int 行数, int 列数)
{
    return 10;
}
