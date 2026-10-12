"""第 34 课参考答案 —— 综合项目：读文件 → 处理 → 排序 → 写回。"""


def 读成绩(路径):
    成绩们 = []
    with open(路径, encoding="utf-8") as 文件:
        for 行 in 文件:
            行 = 行.strip()
            if not 行:                      # 空行跳过
                continue
            姓名, 分数 = 行.split()          # ⚠ split() 不加参数：连续空格也能对付
            成绩们.append((姓名, int(分数)))
    return 成绩们


def 算平均(成绩们):
    # ⚠ 空表要单独处理，否则 总分 / 0 会 ZeroDivisionError
    if not 成绩们:
        return 0.0
    总分 = 0
    for 姓名, 分数 in 成绩们:
        总分 += 分数
    return 总分 / len(成绩们)            # / 不是 //：平均分可以有小数


def 排出名次(成绩们):
    # sorted 是稳定的：同分的保持原来的先后（第 30 课）
    排好的 = sorted(成绩们, key=lambda 对: 对[1], reverse=True)
    结果 = []
    for i, (姓名, 分数) in enumerate(排好的):
        if i > 0 and 分数 == 排好的[i - 1][1]:
            名次 = 结果[-1][0]           # 同分：沿用上一个名次（于是跳跃 1, 1, 3）
        else:
            名次 = i + 1
        结果.append((名次, 姓名, 分数))
    return 结果


def 写成绩(路径, 带名次的):
    with open(路径, "w", encoding="utf-8") as 文件:
        for 名次, 姓名, 分数 in 带名次的:
            # ⚠ 数字要 str() 才能拼；换行要自己写
            文件.write(str(名次) + " " + 姓名 + " " + str(分数) + "\n")


def 处理一遍(原路径, 新路径):
    成绩们 = 读成绩(原路径)
    名次们 = 排出名次(成绩们)
    写成绩(新路径, 名次们)
    return 名次们
