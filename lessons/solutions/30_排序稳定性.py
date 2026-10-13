"""第 30 课参考答案 —— 稳定性与 sorted 的 key=。"""


def 成绩排名(名单):
    # ⚠ 用 sorted（返回新列表），不要用 名单.sort(…)（就地改 + 返回 None）
    # ⚠ reverse=True 不会翻乱同分的先后 —— Python 的排序是稳定的
    return sorted(名单, key=lambda 对: 对[1], reverse=True)


def 按长度排(文字们):
    # key= 后面写函数名本身，不带括号
    return sorted(文字们, key=len)


def 给名次(名单):
    结果 = []
    for i, (姓名, 分数) in enumerate(名单):
        if i > 0 and 分数 == 名单[i - 1][1]:
            # 同分：沿用上一个名次（于是名次会跳跃：1, 1, 3, 4）
            名次 = 结果[-1][2]
        else:
            名次 = i + 1
        结果.append((姓名, 分数, 名次))
    return 结果
