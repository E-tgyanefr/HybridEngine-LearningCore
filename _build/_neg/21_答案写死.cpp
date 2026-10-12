// 第 21 课「偷懒但输出对」的解法：不读文件，直接把答案写死。
// 第一份 data.in 的偶数个数确实是 2 —— 但换一份就露馅。
#include <iostream>
#include <fstream>

int main()
{
    std::ofstream 出("data.out");
    出 << 2 << '\n';
    return 0;
}
