"""第 32 课参考答案 —— 文件读写。"""


def 写几句(路径, 文字们):
    # ⚠ encoding='utf-8' 不能省：中文系统上不加它就会写成 GBK
    # ⚠ 'w' 会自动新建文件，但也会**清空**原有内容（这一课就是要覆盖重写）
    with open(路径, "w", encoding="utf-8") as 文件:
        for 句 in 文字们:
            文件.write(句 + "\n")          # write 不会自动换行


def 读全部(路径):
    with open(路径, encoding="utf-8") as 文件:
        return 文件.read()                 # 整个文件一个字符串（换行也在里面）


def 读成列表(路径):
    with open(路径, encoding="utf-8") as 文件:
        # ⚠ 每行末尾带着 \n，要用 strip() 去掉；空文件自然是 []
        return [行.strip() for 行 in 文件]


def 数行数(路径):
    with open(路径, encoding="utf-8") as 文件:
        # 空文件 0 行；'a\nb\nc\n' 也是 3 行
        return len(文件.readlines())
