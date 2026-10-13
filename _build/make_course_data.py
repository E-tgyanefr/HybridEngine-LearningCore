"""把 219 课转成手机版要的数据（`android/app/web/course-data.js`）。

⚠⚠ **这个脚本不改任何课程内容** —— 它只做搬运。
  「不要更改教学内容」是硬要求，所以抽取逻辑**照抄 `run.py::_stub_for`**
  （那是桌面端生成答题模板的地方）：

      · 课程正文 = 课程文件**开头的连续 `#` 注释块**（碰到第一行真代码就停）
      · 逐行去掉一个 `#`；`# ───` 这种分隔线去掉一个 `#` 后保留 `─`
      · 标题 = 第一行

  ⇒ 手机版和桌面端读的是**同一份正文**，不会"改了一处忘了另一处"。

⚠ 为什么手机版不能直接用桌面引擎：
  · Tkinter 安卓上没有（PEP 738 官方名单）
  · 更硬的：这台设备上 `Py_Initialize()` 一调就原生崩
    （已实测排除 HttpListener / 线程 / WebView / 环境变量 / 扩展模块）
  ⇒ 手机版**单独一版**：C# 宿主提供内容，WebView 做界面，**不需要 Python**。

用法：
    python _build/make_course_data.py            # 看要抽什么
    python _build/make_course_data.py --go       # 真写

产出：
    android/app/web/course-data.js
"""
import io
import json
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WEB = os.path.join(LC, "android", "app", "web")
目标 = os.path.join(WEB, "course-data.js")

#: 五条线：显示名 · 目录 · 后缀 · 覆盖说明
#
#  ⚠ 手机上**五条线都判不了分**。判分要在设备上真编译真运行，而
#    PEP 738 明确说安卓只允许执行**系统目录**里的可执行文件，
#    那些目录运行时不可写。所以界面要如实标出来，不装作能用。
线们 = [
    ("Python", "lessons", ".py", "GESP 一级 → 八级"),
    ("C", "csharp/c-lessons/Lessons", ".cs", "GESP 一级 → 八级"),
    ("C++", "csharp/cpp-lessons/Lessons", ".cs", "GESP 一级 → 八级"),
    ("Java", "csharp/java-lessons/Lessons", ".cs", "按 C++ 那批算法补齐"),
    ("C#", "csharp/cs-lessons/Lessons", ".cs", "按 C++ 那批算法补齐"),
]


def 抽正文(路径: str, 后缀: str) -> str:
    """把课程文件里的**讲解正文**抽出来。

    ⚠⚠ **逻辑必须和桌面端一致**（`run.py::_stub_for`）——
      两边不一致会让手机版和桌面端的正文分叉，而那种错很难发现
      （两处单独看都对）。要改两边一起改。

    两种来源，**取决于文件格式**（实测得来的，不是猜的）：

      ① Python 课（`.py`）：正文就是**文件开头的连续 `#` 注释块**
         逐行去掉一个 `#`；`# ───` 这种分隔线去掉一个 `#` 后保留 `─`。
         碰到第一行真代码就停。这是 `_stub_for` 的原逻辑。

      ② 编译型四线（`.cs`）：那是 **C# 源码**，注释是 `//` 不是 `#`。
         正文在 `public static string 模板 => \"\"\"…\"\"\"` 这个**原始字符串**里。
         ⇒ 直接抠那个三引号之间的内容，**不要**自己去认 `//`
           （认 `//` 会把生成器加的代码注释也当成课程内容）。

      ⚠ 我第一版只写了 ①，于是 142 课的正文是空的 —— 而**自检把这事抓出来了**
        （"空正文 142 课"）。没有那条自检的话，界面会显示 142 个空课，
        而我可能以为是"数据本来就这样"。
    """
    if 后缀 == ".py":
        行们 = io.open(路径, encoding="utf-8").read().splitlines()
        讲解: list[str] = []
        开始了 = False
        for 行 in 行们:
            s = 行.rstrip()
            if s.startswith("#"):
                开始了 = True
                讲解.append(s[1:].lstrip("#") if s.startswith("# ") else s[1:])
            elif 开始了 and not s:
                讲解.append("")
            elif 开始了:
                break
        while 讲解 and not 讲解[-1].strip():
            讲解.pop()
        return "\n".join(讲解)

    #: 编译型：抠 `模板 => """ … """`
    文 = io.open(路径, encoding="utf-8").read()
    m = re.search(r'public static string 模板 => """\r?\n(.*?)\r?\n"""',
                  文, re.S)
    if not m:
        return ""
    行们 = m.group(1).replace("\r\n", "\n").split("\n")

    # ⚠⚠ **必须去掉原始字符串的公共缩进**。
    #   generator 写出来的是（缩进是给 C# 看的）：
    #       public static string 模板 => """
    #       // 第 01 课：…
    #       // ─────
    #       """;
    #   C# 的**原始字符串字面量**会自己按闭引号的缩进去掉那层缩进，
    #   但我们是**直接读 .cs 源文件**的 —— 不自己处理就会每行多几个空格。
    #   实测：不去的话正文每行都顶着一截空白，手机上看着像排版坏了。
    #
    #   做法：找**第一行非空**的缩进作为基准（generator 里所有行缩进一致），
    #   统一去掉。只去公共前缀，不动行内空格。
    基准 = None
    for l in 行们:
        if l.strip():
            基准 = len(l) - len(l.lstrip(" "))
            break
    if 基准:
        行们 = [l[基准:] if l[:基准].strip() == "" else l for l in 行们]

    正文 = "\n".join(行们)
    while 正文.endswith("\n"):
        正文 = 正文[:-1]
    return 正文


def 去掉注释前缀(正文: str, 后缀: str) -> str:
    """把正文每行的注释标记去掉，换成**呈现用的标记**。

    ⚠ 这一步**不是改内容**，是换表示法：去掉的只有注释符本身
      （`//` 后面那个空格、`#` 后面那个空格），
      **一个字都没删**。

    为什么要做（真机/手机视角）：
      · 编译型四线的正文在 `.cs` 里是 `// xxx` 形式（C# 的注释）
      · Python 课是 `# xxx`
      · 桌面上正文显示在**代码编辑器**里，带着 `//` 是对的
        —— 学员就是要照着改代码
      · 但手机版是**阅读器**：一屏 20 行，每行前面顶着 `//`
        既浪费宽度又像代码不像文章

    换成什么：
      · 去掉注释符和紧跟的一个空格
      · `─────` 这样的分隔线 → `---`（手机上是**一条细分隔线**，
        比一串长破折号省地方、也不折行）
      · 行内 `**粗体**` 原样保留 —— 界面会把它渲染成强调

    ⚠ 保留行首缩进：正文里"缩进 4 格的行"是**代码示例或引用**，
      界面靠缩进认它、给它等宽字体和底色。所以只去注释符，不去缩进。
    """
    前 = "//" if 后缀 == ".cs" else "#"
    出 = []
    for 行 in 正文.split("\n"):
        s = 行
        缩 = len(s) - len(s.lstrip(" "))
        身 = s[缩:]
        if 身.startswith(前):
            身 = 身[len(前):]
            if 身.startswith(" "):
                身 = 身[1:]
        #: 分隔线：整行都是 `─`（可能夹空格）→ `---`
        if 身.strip() and re.fullmatch(r"[─\-\s]{4,}", 身):
            身 = "---"
        出.append(" " * 缩 + 身)
    #: 收尾空行
    while 出 and not 出[-1].strip():
        出.pop()
    return "\n".join(出)


def 抽特性(路径: str):
    """从 `[课程("01", "标题", "1. …\\n2. …")]` 里抠出 (课号, 标题, 要求)。

    ⚠ 只有编译型四线有这个特性。Python 课没有 —— 它的标题在正文第一行，
      要求藏在正文的「轮到你了」里。

    ⚠⚠ 这里**不用正则**，改手动扫引号。原因是我第一版用了正则：

        \\[(?:Java)?课程\\(\\s*"(\\d\\d)"\\s*,\\s*"((?:[^"\\\\]|\\\\.)*)"\\s*,\\s*"…"

      看着没问题，但它匹配不到**实际的写法** —— 实际生成出来的是**原始字符串**：

        [课程("01", "第 01 课：printf —— 让程序开口说话",
            \"\"\"
        1. 打印一行文字：你好，C
        2. 打印一个整数：42
        \"\"\")]

      第三段是 `\"\"\"…\"\"\"`（三引号、跨多行），不是普通引号字符串。
      正则要么匹配不上、要么匹配偏 —— 实测**抽出来的标题是正文第 3 行**。
      ⇒ 手动扫：找到 `课程(`，然后依次取三种引号里的内容。
        这样不管以后 generator 改成什么写法，只要还是引号都能读。
    """
    文 = io.open(路径, encoding="utf-8").read()
    i = 文.find("课程(")
    if i < 0:
        return None
    i += len("课程(")
    n = len(文)
    段 = []
    while len(段) < 3 and i < n:
        while i < n and 文[i] in " \t\r\n":
            i += 1
        if i >= n or 文[i] == ")":
            break
        if 文[i] == '"':
            if 文[i:i + 3] == '"""':                     # 原始字符串，跨行
                j = 文.find('"""', i + 3)
                if j < 0:
                    break
                段.append(文[i + 3:j])
                i = j + 3
            else:                                        # 普通字符串
                j = i + 1
                buf = []
                while j < n and 文[j] != '"':
                    if 文[j] == "\\" and j + 1 < n:
                        buf.append(文[j:j + 2])
                        j += 2
                        continue
                    buf.append(文[j])
                    j += 1
                段.append("".join(buf))
                i = j + 1
        else:
            i += 1
    if len(段) < 2:
        return None

    def 解(s: str) -> str:
        return (s.replace('\\"', '"').replace("\\n", "\n")
                 .replace("\\r", "").replace("\\\\", "\\"))

    编号 = 段[0].strip()
    标题 = 解(段[1]).strip()
    要求 = 解(段[2]).strip() if len(段) > 2 else ""
    #: ⚠ 要求可能是 `"""\n1. …\n2. …\n"""` 抽出来的，首尾会有空行
    要求 = "\n".join(l.rstrip() for l in 要求.split("\n")).strip()
    return 编号, 标题, 要求


def 切一段(正文: str, 关键词: str) -> str:
    """把正文里含 `关键词` 的那一行**到末尾**切出来。

    ⚠ 这是**呈现用的切分**，不是改写：切出来的文字和正文里**一字不差**。
      手机屏窄，一屏放不下整篇，所以界面要能分屏显示
      「先看讲解 / 直接看要做什么」。
    """
    行们 = 正文.split("\n")
    for i, 行 in enumerate(行们):
        if 关键词 in 行:
            return "\n".join(行们[i:]).strip()
    return ""


def 抽答案_PY(课号: str) -> str:
    """Python 课的参考答案在 `lessons/solutions/NN_*.py` —— 就是那个文件本身。

    ⚠ 去掉文件头的 docstring（那是"这是参考答案"的说明，不是答案的一部分）——
      这和 `_check_solutions.py` 的处理**必须一致**，否则两边看到的答案不一样。
    """
    import glob as _g
    D = os.path.join(LC, "lessons", "solutions")
    if not os.path.isdir(D):
        return ""
    们 = [f for f in os.listdir(D) if f.startswith(课号) and f.endswith(".py")]
    if not 们:
        return ""
    文 = io.open(os.path.join(D, 们[0]), encoding="utf-8").read()
    return re.sub(r'^\s*"""(?:.|\n)*?"""\s*', "", 文, count=1).strip()


def 抽答案_CS(路径: str) -> str:
    """编译型四线的参考答案在课程文件的 `参考答案 => 三引号…三引号` 里。

    ⚠ 和抽正文一样：要**去掉原始字符串的公共缩进**（直接读 .cs 源文件会有）。
    """
    文 = io.open(路径, encoding="utf-8").read()
    m = re.search(r'public static string 参考答案 => """\r?\n(.*?)\r?\n"""',
                  文, re.S)
    if not m:
        return ""
    行们 = m.group(1).replace("\r\n", "\n").split("\n")
    基准 = None
    for l in 行们:
        if l.strip():
            基准 = len(l) - len(l.lstrip(" "))
            break
    if 基准:
        行们 = [l[基准:] if l[:基准].strip() == "" else l for l in 行们]
    出 = "\n".join(行们)
    while 出.endswith("\n"):
        出 = 出[:-1]
    return 出


def 估时(正文: str) -> int:
    """粗估阅读时长（分钟）。

    ⚠ 这是**给界面做进度感用的**，不是给学员看的承诺。
      取值：中文约 300 字/分钟，代码行按 6 秒算。
      宁可估多不估少 —— 估少了学员会觉得"怎么还没读完"。
    """
    中文字 = len(re.findall(r"[\u4e00-\u9fff]", 正文))
    代码行 = sum(1 for l in 正文.split("\n")
                if l.strip() and not re.search(r"[\u4e00-\u9fff]", l))
    分钟 = 中文字 / 300.0 + 代码行 * 6 / 60.0
    return max(1, int(round(分钟)))


print("  ═══ 手机版课程数据 ═══")
print("    ⚠ 只搬运，不改写 —— 抽取逻辑照抄 run.py::_stub_for")
print()

课程 = []
统计 = {}
for 显示名, 目录, 后缀, 覆盖 in 线们:
    D = os.path.join(LC, 目录.replace("/", os.sep))
    if not os.path.isdir(D):
        print("    ??  %s 目录不存在：%s" % (显示名, 目录))
        continue
    文件们 = sorted(f for f in os.listdir(D)
                   if f.endswith(后缀) and re.match(r"^第?\d\d", f))
    for f in 文件们:
        路径 = os.path.join(D, f)
        正文 = 去掉注释前缀(抽正文(路径, 后缀), 后缀)
        特性 = 抽特性(路径) if 后缀 == ".cs" else None
        if 特性:
            #: 编译型：课号/标题/要求都在 `[课程(...)]` 里，最准。
            #
            #  ⚠⚠ 这三样**必须都用特性里那份**，不能拿正文第一行当标题 ——
            #    实测：正文第一行是标题，但**第二行起是 `// ───` 分隔线**，
            #    而我第一版对 `.cs` 也用了"第一行 = 标题"的逻辑，
            #    结果标题抽成了「怎么读这一课」（那是正文第 3 行）。
            #    ⇒ 特性里有作者写好的标题和要求，用它，别自己猜。
            编号, 标题, 要求 = 特性
        else:
            #: Python：标题在正文第一行；要求从正文里切
            m = re.match(r"^第?(\d\d)", f)
            编号 = m.group(1) if m else "??"
            标题 = (正文.split("\n") or [""])[0].strip() or "（没有标题）"
            要求 = ""
        课程.append({
            "线": 显示名,
            "编": 编号,
            "题": 标题,
            "文": 正文,
            #: ⚠ `要` 优先用特性里那份（作者写的原话，一行一条）；
            #   没有特性时才从正文切「轮到你了」那一段
            "要": 要求 or 切一段(正文, "轮到你了"),
            "覆盖": 覆盖,
            "分": 估时(正文),
            #: 参考答案 —— 手机判不了分，学员写不出来时**只能靠它**。
            #  ⚠ 抽法和桌面端一致（见两个 抽答案_* 的注释）。
            "答": (抽答案_PY(编号) if 后缀 == ".py"
                   else 抽答案_CS(路径)),
            #: ⚠ 手机上五条线都判不了分（见文件头）
            "跑": False,
        })
    统计[显示名] = len(文件们)
    print("    %-8s %3d 课" % (显示名, len(文件们)))

总 = len(课程)
总字 = sum(len(c["文"]) for c in 课程)
print()
print("    合计 %d 课 · 正文 %d 字（%.1f MB）" % (总, 总字, 总字 * 3 / 1024 / 1024))

#: 自检：正文不能是空的（空了说明抽取逻辑坏了，而不是"这一课没内容"）
空的 = [c["线"] + c["编"] for c in 课程 if len(c["文"]) < 40]
无答案 = [c["线"] + c["编"] for c in 课程 if len(c["答"]) < 5]
print("    自检：没有参考答案 %d 课 %s"
      % (len(无答案), ("（有 %s）" % " ".join(无答案[:6])) if 无答案 else "OK"))
没要的 = [c["线"] + c["编"] for c in 课程 if not c["要"]]
print("    自检：空正文 %d 课 %s" % (len(空的), ("**" + " ".join(空的[:6]) + "**") if 空的 else "OK"))
print("    自检：没切出「轮到你了」%d 课 %s"
      % (len(没要的), ("（有 %s）" % " ".join(没要的[:6])) if 没要的 else "OK"))

if "--go" not in sys.argv:
    print()
    print("  （只读检查。加 --go 才真写）")
    sys.exit(0)

#: ── 写出来 ────────────────────────────────────────────────────────────
#  ⚠ 用 JS 文件而不是 .json：`file://` 下 WebView 的 `fetch` 被禁
#    （实测 `TypeError: Failed to fetch`），但 `<script src=...>` 正常。
#    所以数据以 JS 变量的形式给出去，界面直接 `courseData` 就能用。
JS = ("// 手机版课程数据 —— **由 _build/make_course_data.py 生成，不要手改**\n"
      "//\n"
      "// ⚠ 课程正文是从各线的课程文件里**原样搬运**的（抽取逻辑照抄\n"
      "//   run.py::_stub_for），一个字都没改写。要改内容请改课程文件本身。\n"
      "//   重新生成：python _build/make_course_data.py --go\n"
      "//\n"
      "// 字段：线=哪条线 · 编=两位课号 · 题=标题 · 文=正文\n"
      "//      要=「轮到你了」那段（正文里原样切出来的）\n"
      "//      覆盖=考纲覆盖 · 分=粗估阅读分钟 · 跑=能不能判分（手机上都是 false）\n"
      "const courseData = " + json.dumps(课程, ensure_ascii=False,
                                        separators=(",", ":")) + ";\n")

os.makedirs(WEB, exist_ok=True)
io.open(目标, "w", encoding="utf-8", newline="\n").write(JS)
print()
print("  写了 %s（%.2f MB）" % (目标, os.path.getsize(目标) / 1024 / 1024))
