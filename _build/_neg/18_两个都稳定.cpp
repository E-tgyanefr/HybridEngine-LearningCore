// 第 18 课「偷懒但输出对」的解法：两个函数都用 stable_sort。
// 输出完全正确，但学生**没有看见**这一课要讲的稳定性区别。
int 总分(std::vector<成绩> 表)
{
    int 和 = 0;
    for (int i = 0; i < (int)表.size(); i++) { 和 += 表[i].分数; }
    return 和;
}

void 按分排序(std::vector<成绩>& 表)
{
    std::stable_sort(表.begin(), 表.end(),
        [](const 成绩& 甲, const 成绩& 乙) { return 甲.分数 > 乙.分数; });
}

void 按分排序_不稳定(std::vector<成绩>& 表)
{
    std::stable_sort(表.begin(), 表.end(),
        [](const 成绩& 甲, const 成绩& 乙) { return 甲.分数 > 乙.分数; });
}
