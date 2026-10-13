"""准备安卓工程要用的素材：Python 运行时 + 标准库 + 课程内容。

⚠ 为什么要这个脚本，而不是把文件提交进仓库：
  这些是 **Chaquopy 在 Maven Central 上公开的预编译产物**
  （`com.chaquo.python:target`），不是我们自己的东西。整包 ~10MB，
  提交进 git 会让仓库变胖、而且**升级 Python 版本时得重新提交一堆二进制**。
  ⇒ 用脚本按需拉，写清版本号，可复现。

⚠⚠ 为什么不用 Chaquopy 的 Gradle 插件（那才是官方用法）：
  这台构建机**没有 Gradle**，而 .NET Android workload 自带完整构建链
  （`Microsoft.Android.Sdk.Windows` + 全 ABI 运行时 + NativeAOT 交叉编译器）。
  Chaquopy 的产物本身就是**裸 zip**，直接取来放进 APK 即可 ——
  省掉 Gradle 这一整层，也省掉它自己的 runtime。

  代价：**Python 的初始化要我们自己写**（见 PythonHost.cs）。这是本脚本存在的另一半原因。

用法：
    python _build/prepare_android_assets.py            # 看要什么、多大
    python _build/prepare_android_assets.py --go       # 真下

产出（都在 android/app/ 下）：
    jniLibs/arm64-v8a/libpython3.12.so
    lib-dynload/arm64-v8a/*.so
    python-stdlib/**                      （标准库 .py）
    python-app/hybridengine_light/**      （我们自己的引擎，给安卓裁剪过）
    python-app/lesson_android.py          （安卓入口：走 ctl 协议）
"""
import io
import os
import shutil
import sys
import urllib.request
import zipfile

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APP = os.path.join(LC, "android", "app")

#: ⚠ 版本必须与**构建机 Python** 的 major.minor 一致（官方要求），
#   pyc 的字节码格式与版本绑定。本机是 3.12.10 ⇒ 选 3.12.7-1。
PY = "3.12.7-1"
PY_MM = "3.12"
ABI = "arm64-v8a"
#: ⚠⚠ Maven 的路径格式：`<组路径>/<工件>/<版本>/<工件>-<版本>[-分类器].<后缀>`
#   —— 版本号是**目录名**，也是**文件名的一部分**。我第一版把版本当成了路径段，
#   拼出 `.../target/3.12.7-1/arm64-v8a` 这种不存在的 URL，直接 404。
#   ⇒ 实测确认过的两个 URL 就写在下面，别再自己拼。
MAVEN = "https://repo1.maven.org/maven2/com/chaquo/python/target"
基础 = "%s/%s/target-%s" % (MAVEN, PY, PY)

要下 = {
    "%s-%s.zip" % (基础, ABI): "运行时（libpython + lib-dynload + jniLibs）",
    "%s-stdlib-pyc.zip" % 基础: "标准库（预编译 .pyc —— 启动更快、体积更小）",
}

#: 引擎里安卓要用的模块。
#
#  ⚠ 挑法不是"看着像就拷"，而是**先算导入闭包**（实测）：
#
#      ctl.py 的闭包 = ctl → _paths, coach, explain, exam, examrun   （5 个，极轻）
#      run.py 的闭包 = 15 个，其中 **run 和 learn_gui 引 tkinter**
#
#    而 tkinter **安卓上没有**（PEP 738 官方名单）。所以：
#      ✅ 走 `ctl.py`（协议层，一行一条 JSON）—— 不碰 tkinter
#      ❌ 不要 `run.py` / `learn_gui.py` / `anim.py` / `stage.py`
#
#    课程运行器（check/learnmodel/aicourse/…）单独列 —— 它们不引 tkinter，
#    但 `run.py` 会引它们，所以**不拷 run.py 就行**，这些都能要。
引擎模块 = [
    "__init__.py",     # ⚠ 已改成**惰性导入** —— 这是安卓能用的前提
                       #   （原来顶层 `from .stage import Stage` ⇒ 一 import 就撞 tkinter）
    "_paths.py",
    "coach.py",        # ctl → coach
    "explain.py",      # ctl → coach → explain
    "exam.py",         # ctl → exam
    "examrun.py",      # ctl → examrun
    "ctl.py",          # 协议层（安卓入口用它）
    "check.py",        # Python 课的检查
    #: ⚠⚠ **`watchdog.py` 漏不得**（2026-10-13 加）——
    #:   它是"跑学员代码时的超时保护"，被 `check.py` / `ctl.py` / `examrun.py`
    #:   **三个文件** import。
    #:
    #:   ⚠ 漏了会怎样：**这个清单是硬编码的**，漏掉的那个文件不会进 APK，
    #:     而报错要到学员**点判分那一刻**才出现（`ModuleNotFoundError: watchdog`）——
    #:     构建、装机、看界面全都正常。
    #:   ⇒ 加模块时**必须同步加这里**，别指望构建脚本自己发现。
    "watchdog.py",     # 跑学员代码的超时保护（check/ctl/examrun 都用）
    "learnmodel.py",   # 课程模型（只读课程文本）
    "aicourse.py", "enginecourse.py", "embedded.py", "raspi.py",
    "raspi_course.py", "mcu.py", "gamekit.py", "cmd_engine.py",
    "cmd_exam.py", "_stub.py", "editor.py",
]

#: 课程内容目录（安卓上要能读课程文本）
课程目录 = ["lessons", "games", "ai", "embedded", "raspi", "engine", "exams"]

print("  ═══ 安卓素材 ═══")
print("    Python  %s（与本机 3.12 对齐）" % PY)
print("    ABI     %s（只出 arm64 —— 每多一个 ABI 就多几 MB）" % ABI)
print()
print("  === 要下载 ===")
总共 = 0
for u, 名 in 要下.items():
    print("    %-58s %s" % (u.split("/")[-1], 名))
print()
print("  === 要拷贝的引擎模块 ===")
可用, 缺 = [], []
for m in 引擎模块:
    P = os.path.join(LC, "hybridengine_light", m)
    (可用 if os.path.isfile(P) else 缺).append(m)
print("    有 %d 个：%s" % (len(可用), " ".join(可用)))
if 缺:
    print("    缺 %d 个：%s" % (len(缺), " ".join(缺)))


def _查包内依赖():
    """⚠⚠ **这个清单是硬编码的 ⇒ 漏一个模块要等学员点判分才暴露。**

    实测过一次（2026-10-13）：给 `check.py` / `ctl.py` / `examrun.py`
    加了 `from .watchdog import ...`，忘了往 `引擎模块` 里加 `watchdog.py` ——
    构建成功、装机成功、界面正常，**只有点到"判分"那一刻**才
    `ModuleNotFoundError: No module named 'hybridengine_light.watchdog'`。

    ⇒ 这道闸就是拦这件事：**把清单里每个模块的包内 import 全解析一遍，
      凡是指向"同包某个 .py"、而那个 .py 不在清单里的，直接报出来。**

    ⚠ 只查**同包（相对 import）**：跨包的第三方依赖本来就不该有
      （本项目铁律：零第三方依赖），而 `hybridengine_light` 之外的
      全是 stdlib，安卓的 stdlib 已经有了。

    ⚠ 用 AST 而不是正则：正则会把注释和字符串里的 import 也算进来
      （本文件自己的注释里就写着 `from .watchdog import`）。
    """
    import ast as _ast
    包内 = {m for m in 引擎模块 if m.endswith(".py")}
    模块名 = {m[:-3] for m in 包内}
    漏 = []
    for m in sorted(包内):
        P = os.path.join(LC, "hybridengine_light", m)
        if not os.path.isfile(P):
            continue
        try:
            树 = _ast.parse(open(P, encoding="utf-8").read())
        except SyntaxError as e:
            print("    ??  %s 解析不了：%s" % (m, e))
            continue
        for 节 in _ast.walk(树):
            #: `from .x import y` / `from . import x`
            if isinstance(节, _ast.ImportFrom) and 节.level == 1:
                基 = 节.module or ""
                #: `from . import a, b` ⇒ 每个 name 都可能是个模块
                if not 基:
                    for a in 节.names:
                        if a.name in 模块名 and a.name + ".py" not in 包内:
                            漏.append((m, "." + a.name))
                    continue
                顶层 = 基.split(".")[0]
                #: `from .pyengine import x` 指向的是**目录**（包里另算）——
                #: 只有"指向同目录某个 .py"的才算漏
                if os.path.isfile(os.path.join(LC, "hybridengine_light",
                                               *基.split(".")) + ".py"):
                    if 顶层 + ".py" not in 包内:
                        漏.append((m, "." + 基))
                elif os.path.isdir(os.path.join(LC, "hybridengine_light", 顶层)):
                    pass          # 目录（子包）——不在这个清单的职责里
    return 漏


_漏 = _查包内依赖()
if _漏:
    print()
    print("  XX **清单漏了模块**（构建、装机、界面都会正常，只有用到时才炸）：")
    for 谁, 什么 in _漏:
        print("       %s 里 import 了 %s —— 但它不在 引擎模块 清单里" % (谁, 什么))
    print()
    print("     ⇒ 往上面的 `引擎模块` 列表里加上它们，再跑。")
    sys.exit(2)
print("    （包内依赖检查：清单没有漏模块）")
print()
print("  === 要拷贝的课程目录 ===")
for d in 课程目录:
    P = os.path.join(LC, d)
    if not os.path.isdir(P):
        print("    ??  %s（不存在）" % d)
        continue
    n = sum(len(f) for _, _, f in os.walk(P))
    b = sum(os.path.getsize(os.path.join(根, f))
            for 根, _, 文件们 in os.walk(P) for f in 文件们)
    总共 += b
    print("    %-10s %4d 个文件  %7.1f KB" % (d, n, b / 1024))
print()
print("    课程内容合计 %.1f MB（压缩进 APK 后会小很多）" % (总共 / 1024 / 1024))

if "--go" not in sys.argv:
    print()
    print("  （只读检查。加 --go 才真下真拷）")
    sys.exit(0)

# ── 下载 ──────────────────────────────────────────────────────────────
缓存 = os.path.join(LC, "_build", "_android_cache")
os.makedirs(缓存, exist_ok=True)
os.makedirs(APP, exist_ok=True)


def 取(url: str) -> str:
    名 = url.split("/")[-1]
    P = os.path.join(缓存, 名)
    if os.path.isfile(P) and os.path.getsize(P) > 0:
        print("    （已有缓存）%s" % 名)
        return P
    print("    下载 %s …" % 名)
    with urllib.request.urlopen(url, timeout=600) as r, open(P, "wb") as f:
        shutil.copyfileobj(r, f)
    print("      %.2f MB" % (os.path.getsize(P) / 1024 / 1024))
    return P


print()
print("  ═══ 下载 ═══")
压缩包 = {u: 取(u) for u in 要下}

# ── 解压 ──────────────────────────────────────────────────────────────
print()
print("  ═══ 解压（按需挑，不是整包塞）═══")

#: ① 运行时：只要 jniLibs 和 lib-dynload，**不要 include/**（C 头文件，用不上）
want_jni, want_dyn = [], []
with zipfile.ZipFile(压缩包["%s-%s.zip" % (基础, ABI)]) as z:
    for n in z.namelist():
        if n.startswith("jniLibs/%s/" % ABI) and n.endswith(".so"):
            want_jni.append(n)
        elif n.startswith("lib-dynload/%s/" % ABI) and n.endswith(".so"):
            want_dyn.append(n)

    #: ⚠ libpython 必须放 `jniLibs/<abi>/`（.NET Android 靠**路径**推断 ABI）
    #
    #  ⚠⚠ 这几条是**按体积实测砍的**（APK 拆开量过）：
    #       libcrypto_python.so   1.36 MB   ← 只有 import ssl 才要
    #       libsqlite3_python.so  0.59 MB   ← 只有 import sqlite3 才要
    #       libssl_python.so      0.24 MB   ← 同 ssl
    #     三门课的内容里**没有一处用 ssl / sqlite3**（课程是本地文件读写 + 算法），
    #     而 Chaquopy 那几个几 KB 的 `*_chaquopy.so` 是给它的 Java 层用的，
    #     我们走 P/Invoke、不用它 —— 也一起不要。
    #     ⇒ 砍掉合计 2.19 MB。**如果真需要**（比如以后加网络课），
    #       把名字从 不要 里去掉即可，不必改别的。
    不要 = ("libcrypto_python.so", "libsqlite3_python.so", "libssl_python.so",
            "libcrypto_chaquopy.so", "libsqlite3_chaquopy.so", "libssl_chaquopy.so")
    want_jni = [n for n in want_jni if os.path.basename(n) not in 不要]

    #: ⚠ 跟着砍的扩展模块 —— **名单要保守**。
    #    我第一版顺手把 `_decimal`/`_lzma`/`_hashlib` 也列进去了，那是错的：
    #      · `_decimal`  ← stdlib 的 `decimal.pyc` 会 import 它；json/格式化都碰
    #      · `_lzma`     ← zipfile/tarfile 用；而我们**自己**就要解 python.zip
    #      · `_hashlib`  ← `random` 播种、`hash()` 都用
    #    砍了它们省不到 0.5 MB，却会让随机数、压缩、甚至启动路径挂掉。
    #    ⇒ 只砍**确实没有任何 import 会碰**的：ssl / sqlite3 / syslog / 模糊测试桩。
    #
    #    ⚠ 留着比砍掉更糟的情况也要避免：`import ssl` 若找得到模块、
    #      却在加载时发现缺 .so，报的是 `dlopen failed` ——
    #      比 `No module named ssl` 难懂得多。所以模块和 .so 要**一起**砍。
    不要扩展 = ("_ssl.", "_sqlite3.", "syslog.", "_xxtestfuzz.")
    want_dyn = [n for n in want_dyn if not any(基 in os.path.basename(n) for 基 in 不要扩展)]

    出jni = os.path.join(APP, "jniLibs", ABI)
    shutil.rmtree(出jni, ignore_errors=True)
    os.makedirs(出jni, exist_ok=True)
    for n in want_jni:
        with z.open(n) as src, open(os.path.join(出jni, os.path.basename(n)), "wb") as dst:
            shutil.copyfileobj(src, dst)
    print("    jniLibs/%s/  %d 个 .so" % (ABI, len(want_jni)))
    for n in want_jni:
        P = os.path.join(出jni, os.path.basename(n))
        print("       %-30s %6.2f MB" % (os.path.basename(n), os.path.getsize(P) / 1024 / 1024))

    #: lib-dynload 保持**原目录结构**，因为 CPython 按 `lib-dynload/<abi>/` 找扩展模块
    出dyn = os.path.join(APP, "lib-dynload", ABI)
    shutil.rmtree(os.path.join(APP, "lib-dynload"), ignore_errors=True)
    os.makedirs(出dyn, exist_ok=True)
    for n in want_dyn:
        with z.open(n) as src, open(os.path.join(出dyn, os.path.basename(n)), "wb") as dst:
            shutil.copyfileobj(src, dst)
    print("    lib-dynload/%s/  %d 个扩展模块" % (ABI, len(want_dyn)))

#: ② 标准库
出std = os.path.join(APP, "python-stdlib")
shutil.rmtree(出std, ignore_errors=True)
os.makedirs(出std, exist_ok=True)
n_std = 0
with zipfile.ZipFile(压缩包["%s-stdlib-pyc.zip" % 基础]) as z:
    for n in z.namelist():
        if n.endswith("/"):
            continue
        #: ⚠ 跳过 tkinter/turtle/curses/readline —— **安卓上根本没有**
        #   （PEP 738 官方名单）。带着它们只会让体积变大、且 import 时报错更难懂。
        段 = n.split("/")
        if 段[0] in ("tkinter", "turtle.py", "turtle", "curses", "idlelib",
                     "lib2to3", "test", "unittest", "distutils",
                     "ensurepip", "venv", "pydoc_data", "msilib"):
            continue
        出 = os.path.join(出std, n.replace("/", os.sep))
        os.makedirs(os.path.dirname(出), exist_ok=True)
        with z.open(n) as src, open(出, "wb") as dst:
            shutil.copyfileobj(src, dst)
        n_std += 1
print("    python-stdlib/  %d 个文件  %.2f MB"
      % (n_std, sum(os.path.getsize(os.path.join(根, f))
                    for 根, _, 文件们 in os.walk(出std) for f in 文件们) / 1024 / 1024))

#: ③ 引擎（只拷安卓要用的那几个模块）
出包 = os.path.join(APP, "python-app", "hybridengine_light")
shutil.rmtree(os.path.join(APP, "python-app"), ignore_errors=True)
os.makedirs(出包, exist_ok=True)
拷了 = 0
for m in 引擎模块:
    P = os.path.join(LC, "hybridengine_light", m)
    if os.path.isfile(P):
        shutil.copy2(P, os.path.join(出包, m))
        拷了 += 1
print("    python-app/hybridengine_light/  %d 个模块" % 拷了)

#: ④ 课程内容
出课 = os.path.join(APP, "python-app", "content")
for d in 课程目录:
    src = os.path.join(LC, d)
    if not os.path.isdir(src):
        continue
    dst = os.path.join(出课, d)
    shutil.copytree(src, dst, ignore=shutil.ignore_patterns(
        "__pycache__", "answers", "solutions", "*.pyc", "bin", "obj"))
print("    python-app/content/  课程内容已拷")

print()
print("  ═══ 总计 ═══")
总 = 0
for 根, _, 文件们 in os.walk(APP):
    for f in 文件们:
        总 += os.path.getsize(os.path.join(根, f))
print("    android/app/  %.2f MB" % (总 / 1024 / 1024))

# ── ⑤ 把 Python 资产打成**一个 zip** ────────────────────────────────────
#
# ⚠⚠ 为什么必须打 zip（实测撞出来的硬约束）：
#   AAPT2 **处理不了非 ASCII 的文件名**。课程文件名是中文的
#   （`01_线性回归.py`、`第16课_二维数组.cs` …，667 个里 79 个含中文），
#   而这些名字**不能改** —— 那是学员在界面上看到的课名。于是：
#       error APT2098: failed to open file. .../content/ai/01_线性回归.py
#
#   同一个 aapt2 还处理不了**含中文的工程路径**：
#       error APT2265: This could be caused by the project having
#                      non-ASCII characters in its filename or path.
#   后者靠"把工程拷到 ASCII 路径构建"解决（见 build_android.py），
#   前者只能靠**打包成一个 ASCII 名字的 zip**。
#
#   ⇒ APK 里只多一个 `assets/python.zip`，运行时解压（AssetInstaller.cs）。
#     附带好处：aapt2 不用逐个处理 667 个资产文件，**构建明显更快**。
print()
print("  ═══ 打 python.zip ═══")
import zipfile as _zf
ZIP = os.path.join(APP, "python.zip")
#: 要进 zip 的四个目录（APK 里就靠这一个文件承载全部 Python 侧内容）
进包 = ["python-stdlib", "lib-dynload", "python-app", "web"]
if os.path.isfile(ZIP):
    os.remove(ZIP)
n = 0
with _zf.ZipFile(ZIP, "w", _zf.ZIP_DEFLATED, compresslevel=6) as z:
    for d in 进包:
        P = os.path.join(APP, d)
        if not os.path.isdir(P):
            continue
        for 根, _, 文件们 in os.walk(P):
            for f in 文件们:
                F = os.path.join(根, f)
                z.write(F, os.path.relpath(F, APP).replace("\\", "/"))
                n += 1
print("    python.zip  %d 个文件  %.2f MB" % (n, os.path.getsize(ZIP) / 1024 / 1024))

#: ⚠⚠ 打完就把**生成出来的**散目录删掉 —— 否则它们仍会被 csproj 的 glob
#:   收进 assets，于是又撞回 APT2098（而且体积翻倍）。
#:
#: ⚠⚠ 但 `web/` **不能删** —— 它是**源文件**，不是生成物。
#:   我第一版把它和那三个生成目录一起放进名单里，结果：
#:     · 本机 `android/app/web/index.html` 被删掉
#:     · 而它从没被 git 跟踪过 ⇒ **文件真的没了**
#:     · 真机上表现是 WebView 报 `net::ERR_FILE_NOT_FOUND`
#:   ⇒ **「生成物」和「源文件」不能混在一个删除名单里。**
#:     混了以后，"清理"就等于"删源码"。
生成 = ["python-stdlib", "lib-dynload", "python-app"]
print("    删掉生成的散目录（避免又被 glob 收进去、且体积翻倍）…")
for d in 生成:
    P = os.path.join(APP, d)
    if os.path.isdir(P):
        shutil.rmtree(P, ignore_errors=True)
        print("      - %s" % d)
print("    （web/ 留着 —— 那是源文件）")

总 = 0
for 根, _, 文件们 in os.walk(APP):
    for f in 文件们:
        总 += os.path.getsize(os.path.join(根, f))
print()
print("    android/app/ 现在 %.2f MB" % (总 / 1024 / 1024))
print()
print("  下一步：python _build/build_android.py")
