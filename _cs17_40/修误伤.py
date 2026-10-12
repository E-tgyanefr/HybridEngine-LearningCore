# 修复「自动补引号」误伤的课程文件（**只动第 17~40 课**，绝不碰别人的课）。
#
# 背景：我先前用脚本把课程注释里的英文双引号批量换成「」。那个脚本太粗：
#   ① 把**相邻字符串字面量之间的胶水**也包进去了：
#          ("稳定甲", "甲,乙,丙", "降序名字(…)")   ⇒  ("稳定甲「, 」甲,乙,丙「, 」降序名字(…)")
#      —— 直接编译不过（CS1503）。
#   ② 把**代码示例行**里的字符串也换了：
#          File.WriteAllText("a.txt", "你好");      ⇒  File.WriteAllText(「a.txt」, 「你好」);
#      —— 编译没事，但学生照着抄是错的。
#
# 这个脚本做两件事（都只针对 17~40）：
#   ① 全局把 `「, 」` 还原成 `", "`（这个形状不可能是正文引用）
#   ② 在**注释行**里，把内容不含中文的 `「X」` 还原成 `"X"` —— 那是代码里的字面量
#
# 用法：python _cs17_40\修误伤.py
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
根 = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
课程目录 = os.path.join(根, "csharp", "cs-lessons", "Lessons")

编号们 = ["%02d" % i for i in range(17, 41)]
总胶水 = 总字面量 = 0

for 编 in 编号们:
    命中 = [f for f in os.listdir(课程目录) if f.startswith("第%s课" % 编) and f.endswith(".cs")]
    if not 命中:
        print("!! 找不到第 %s 课" % 编)
        continue
    路径 = os.path.join(课程目录, 命中[0])
    t = io.open(路径, encoding="utf-8").read()

    胶水 = t.count("「, 」")
    t = t.replace("「, 」", '", "')
    总胶水 += 胶水

    行们 = t.split("\n")
    改 = 0
    for i, line in enumerate(行们):
        s = line.lstrip()
        if not s.startswith("//"):
            continue
        # 注释行里，内容不含中文的「X」还原成 "X"
        def 换(m):
            global 改
            if re.search(r"[\u4e00-\u9fff]", m.group(1)):
                return m.group(0)
            改 += 1
            return '"' + m.group(1) + '"'
        新 = re.sub(r"「([^」\n]{0,20})」", 换, line)
        if 新 != line:
            行们[i] = 新
    总字面量 += 改
    io.open(路径, "w", encoding="utf-8", newline="").write("\n".join(行们))
    if 胶水 or 改:
        print("%s：胶水 %d 处、注释里的字面量 %d 处" % (命中[0], 胶水, 改))

print("合计：胶水 %d 处、注释字面量 %d 处" % (总胶水, 总字面量))
