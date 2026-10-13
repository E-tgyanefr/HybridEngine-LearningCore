"""构建 HybridEngine-light 的安卓 APK。

⚠⚠ **必须经过这个脚本，不能直接 `dotnet build android/app/...`。**
  原因是一条实测出来的硬约束：

      error APT2265: This could be caused by the project having
                     non-ASCII characters in its filename or path.

  本项目的仓库根目录里带中文（`引擎`）—— **AAPT2 处理不了含中文的工程路径**。
  所以这个脚本先把工程拷到一个纯 ASCII 的临时路径，在那里构建，
  再把产物搬回来。

  ⚠ 这个「拷贝」不只是绕过限制，还**让真正的错误能露出来**：
    在中文路径下，APT2265 会把后面所有的错误都挡住 ——
    我先撞到 APT2265，解决后又连续露出 APT2260（缺图标）、
    APT2098（资产文件名含中文）两个错。看不到真正的原因就没法修。

  ⚠⚠ 上面这段是 **docstring**：反斜杠在普通字符串里会被当成转义
    （`引擎\\` 结尾会触发 SyntaxWarning: invalid escape sequence）。
    所以文档里提到路径分隔符时**要么用正斜杠、要么写成原始字符串**。
    这个警告不影响运行，但会让每次构建都多一行噪音 —— 而噪音会让人忽略真的报错。

用法：
    python _build/build_android.py              # 说明 + 检查环境
    python _build/build_android.py --go         # 真构建

前置：
    ① `python _build/prepare_android_assets.py --go`（拉 Python 产物）
    ② `python _build/make_android_icon.py`（生成图标）
    —— 上面两步这个脚本会自己检查，缺了就告诉你去跑。
"""
import os
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(LC, "android", "app")
工程 = "HybridEngineLight.Android.csproj"

#: ⚠ 构建用的 ASCII 工作路径。**必须在盘根下、且不含中文**。
#  用固定路径而不是 mkdtemp：固定路径能复用 obj/bin 的增量编译，
#  一次构建从 27 秒降到几秒（改一行代码再测时差别很明显）。
构建区 = r"C:\lc_android"
构建工程 = os.path.join(构建区, "app")

需要 = [
    (os.path.join(SRC, "python.zip"), "Python 素材",
     "python _build/prepare_android_assets.py --go"),
    (os.path.join(SRC, "jniLibs", "arm64-v8a", "libpython3.12.so"), "libpython",
     "python _build/prepare_android_assets.py --go"),
    (os.path.join(SRC, "Resources", "mipmap-xxxhdpi", "icon.png"), "应用图标",
     "python _build/make_android_icon.py"),
]

print("  ═══ 安卓构建 ═══")
print("    源工程：%s" % SRC)
print("    构建区：%s（ASCII —— AAPT2 要求）" % 构建工程)
print()
print("  === 前置检查 ===")
缺 = []
for P, 名, 怎么办 in 需要:
    ok = os.path.exists(P)
    print("    %s %s" % ("OK " if ok else "XX ", 名))
    if not ok:
        缺.append(怎么办)
if 缺:
    print()
    print("  ⚠ 还缺素材。先跑：")
    for c in dict.fromkeys(缺):
        print("      %s" % c)
    sys.exit(1)

#: ⚠⚠ **过期检查 —— 这一条是踩出来的，不是想出来的。**
#:
#:   实测：我改了网页、跑了构建、装了 APK，真机上**还是旧界面**。
#:   查时间戳才看清：
#:       生成物 web/index.html   10:53:10   ← 新
#:       app/python.zip          10:51:51   ← 旧
#:       构建目录的 python.zip    10:51:51   ← 照抄了旧的
#:   ⇒ `build_android.py` 只检查素材**在不在**，不管它**新不新** ——
#:     于是打包用的是过期产物，而构建照样"成功"。
#:
#:   ⚠ 这跟同一轮里那个「素材指纹只比手写常量」是**同一类 bug**：
#:     **构建会静默地使用过期的东西**。而它的症状是"我明明改了却没生效"，
#:     最容易被误判成"功能坏了"。
#:
#:   ⇒ 这里显式比一次时间戳，过期就**直接拦下来并说清怎么修** ——
#:     不要"自动帮我重打"：那会掩盖"我到底该跑哪一步"这件事，
#:     而下一次还是不知道。
素材 = os.path.join(SRC, "python.zip")
网页们 = [os.path.join(SRC, "web", f) for f in ("index.html", "course-data.js")]
if os.path.exists(素材) and any(os.path.exists(w) for w in 网页们):
    新 = max(os.path.getmtime(w) for w in 网页们 if os.path.exists(w))
    if 新 > os.path.getmtime(素材):
        import time as _t
        print()
        print("  XX python.zip **过期了** —— 网页比它新：")
        print("       网页     %s" % _t.strftime("%H:%M:%S", _t.localtime(新)))
        print("       python.zip %s"
              % _t.strftime("%H:%M:%S", _t.localtime(os.path.getmtime(素材))))
        print()
        print("  ⚠ 不重打就会把**旧界面**打进 APK，而构建照样报成功 ——")
        print("    真机上的症状是「我明明改了却没生效」，很容易误判成功能坏了。")
        print()
        print("  先跑：")
        print("      python _build/prepare_android_assets.py --go")
        sys.exit(1)


def _查素材漏没漏():
    """⚠⚠ **上面那道闸只比了网页的时间戳 —— 引擎的 `.py` 它一个都没比。**

    实测事故（2026-10-13）：

        我给 `hybridengine_light/` 加了 `watchdog.py`（超时保护），
        改的 `check.py` / `ctl.py` / `examrun.py` 都要 import 它。
        · `prepare_android_assets.py` 的 `引擎模块` 清单我**加对了**
        · 那道清单闸门也**证明了会拦**（拿掉 watchdog 就退出码 2）
        · `build_android.py` **退出码 0**、构建成功、装机成功

        结果：APK 的 `assets/python.zip` 里**没有 `watchdog.py`**（20 个模块，它是第 21 个）。

    真机症状：`hel 超时` 实验**卡住不动**，应用界面无响应（`mCurrentFocus=null`），
    只能强杀。日志停在 `Python 起来了` —— 之后一行都没有。

    ⚠ 根因：**只按 mtime 比是不够的。**
      新加的**文件**（不是"改动的文件"）不会让任何旧文件的 mtime 变新 ⇒
      `python.zip` 看着"不过期"，但它里面就是少东西。

    ⇒ 正确的检查不是"比时间"，是"**清单里的东西在不在包里**"。
      这条与"过期检查"是**同一类 bug 的两种表现**：
      **构建会静默地使用不完整的东西。**
    """
    import zipfile as _zf
    if not os.path.exists(素材):
        return None
    try:
        with _zf.ZipFile(素材) as z:
            包里 = set(z.namelist())
    except Exception as e:                                    # noqa: BLE001
        return ["python.zip 打不开：%s" % e]

    LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    源目录 = os.path.join(LC, "hybridengine_light")
    漏 = []
    #: ① 引擎模块：清单里每个 .py 都要在包里
    try:
        sys.path.insert(0, os.path.join(LC, "_build"))
        import prepare_android_assets as _PA
        模块 = list(_PA.引擎模块)
    except Exception as e:                                    # noqa: BLE001
        模块 = []
        漏.append("（读不到 引擎模块 清单：%s）" % e)
    for m in 模块:
        应 = "python-app/hybridengine_light/" + m
        if 应 not in 包里:
            漏.append("引擎模块 %s 不在包里" % m)
    #: ② 课程内容：**用素材脚本自己的 `课程目录` 清单**，不自己猜。
    #:
    #:    ⚠⚠ 这一点我第一版做错了，代价是**两次假红**：
    #
    #        · 第一次：路径前缀写成 `python-app/`，实际是 `python-app/content/`
    #        · 第二次：自己列了一遍目录名（lessons/csharp/java-lessons/…），
    #          结果 `lessons/answers/` 被报"不在包里" —— 而那是**故意不打**的
    #          （手机上判分用的是 `check.run_checks`，不需要答案文件），
    #          `csharp/` 同理（要真编译器，安卓上不可能）
    #
    #    ⇒ **"该打包什么"的唯一权威是 `prepare_android_assets.py` 里的清单。**
    #      这边猜一遍 = 造第二份真相，而两份真相迟早不一致
    #      （而且不一致时的症状是"闸门假红"，会让人去修一个不存在的问题）。
    try:
        sys.path.insert(0, os.path.join(LC, "_build"))
        import prepare_android_assets as _PA2
        课程清单 = list(getattr(_PA2, "课程目录", []))
    except Exception as e:                                    # noqa: BLE001
        漏.append("（读不到 课程目录 清单：%s）" % e)
        课程清单 = []
    for 名 in 课程清单:
        源 = os.path.normpath(os.path.join(LC, 名))
        if not os.path.isdir(源):
            continue
        #: ⚠⚠ **排除规则必须和素材脚本一致**（`prepare_android_assets.py` 里那份 `不要`）。
        #:   手机上**故意不打** `answers/` 和 `solutions/` —— 判分走的是
        #:   `check.run_checks`（检查函数就在课程 `.py` 里），**不需要答案文件**。
        #:   ⚠ 我第一版漏了这条 ⇒ **假红**："lessons/answers/… 不在包里"，
        #:     而它本来就不该在。**假红会让人去修一个不存在的问题**，比漏报还费时间。
        跳过目录 = {"__pycache__", "answers", "solutions", "bin", "obj"}
        样例 = []
        for 根, 目录们, 文件们 in os.walk(源):
            目录们[:] = [d for d in 目录们 if d not in 跳过目录]
            for f in 文件们:
                if f.endswith(".pyc"):
                    continue
                相对 = os.path.relpath(os.path.join(根, f), LC).replace("\\", "/")
                if "python-app/content/" + 相对 not in 包里:
                    样例.append(相对)
                    if len(样例) >= 3:
                        break
            if len(样例) >= 3:
                break
        if 样例:
            漏.append("%s 里有 %d+ 个文件不在包里，例如 %s"
                      % (名, len(样例), "、".join(样例)))
    return 漏


_漏 = _查素材漏没漏()
if _漏:
    print()
    print("  XX **python.zip 里缺东西** —— 构建会照常成功，但真机上要用到才炸：")
    for x in _漏:
        print("       %s" % x)
    print()
    print("  ⚠ 只按 mtime 比是不够的：**新加的文件**不会让旧文件的 mtime 变新 ⇒")
    print("     zip 看着「不过期」，但里面就是少东西。")
    print()
    print("  先跑：")
    print("      python _build/prepare_android_assets.py --go")
    sys.exit(1)
print("    OK  素材完整性（清单里的模块与课程文件都在包里）")

#: 构建要的 JDK —— 系统 PATH 上是 25，而 .NET Android 拒绝
JDK = r"D:\AndroidToolchain\jdk21\jdk-21.0.5+11"
print("    %s JDK 21（%s）" % ("OK " if os.path.isdir(JDK) else "XX ", JDK))
if not os.path.isdir(JDK):
    print("      ⇒ 没装 JDK 21。系统 PATH 上的 JDK 25 会被 .NET Android 拒绝：")
    print("        error XA0030: Building with JDK version 25.0.3 is not supported.")
    sys.exit(1)

#: ⚠⚠⚠ **`obj/` 缓存：每次真构建**都清掉 —— 2026-10-13 真机事故换来的。
#:
#:   事故：APK 装上去**启动即崩**（SIGABRT），原生栈是：
#:       Abort message: 'Invalid compressed assembly descriptor index 36'
#:       #01 … /lib/arm64/libmonodroid.so (xamarin::android::Helpers::abort_…)
#:   ⇒ `.NET Android` 运行时读**程序集存储**时索引越界 ——
#:     **没到我们的 C# 代码**（logcat 里连一行 `HybridEngineLight` 都没有）。
#:
#:   真因：**`android/app/obj/` 里的程序集存储是旧的**。
#:   `build_android.py` 同步时**排除了 `obj`/`bin`**（那是对的 —— 路径变了旧产物会失效），
#:   于是构建区里**留着上一次的 `obj`** ⇒ 程序集清单和实际 dll 对不上 ⇒ 索引越界。
#:
#:   ⚠⚠ 最坑的是**它跟代码无关**：我把 `Diag.cs` 回退到干净的 git HEAD、
#:     全新构建、全新安装，**一样崩** ⇒ 我为此连试了五轮，**全在错的前提上**。
#:
#:   ⇒ **不按时间戳判断，直接清。** 理由两条：
#:     ① 这个缓存**没有任何需要保留的价值** —— 它就是中间产物，
#:        重建的代价只是几十秒，而"带着坏缓存构建"的代价是**一小时的排查 + 一个坏发布物**
#:     ② 我第一版写的是"比时间戳，旧了就删" —— ⚠ 那**防不住**
#:        （`obj` 存在但内容不对时，时间戳看着是新的 ⇒ 照旧中招）
#:   ⚠ 也**不是**删构建区 `C:\lc_android` —— 那是全量重建（几分钟）；
#:     只删 `obj`/`bin` 就够（几十秒），因为坏的就是程序集存储。
if "--go" in sys.argv:
    import shutil as _sh2
    for _d in ("obj", "bin"):
        _p = os.path.join(LC, "android", "app", _d)
        if os.path.isdir(_p):
            _sh2.rmtree(_p, ignore_errors=True)
    print("    已清 android/app/obj 与 bin（⚠ 不清会中\"程序集描述符越界\"那个坑）")

if "--go" not in sys.argv:
    print()
    print("  （检查通过。加 --go 才真构建）")
    sys.exit(0)

# ── 1 同步到 ASCII 路径 ────────────────────────────────────────────────
#  ⚠ 排除 obj/bin：那是上一次的编译产物，拷过去反而可能因为路径变了而失效，
#    留着只会让拷贝变慢、还可能带进脏状态。
print()
print("  === 1/3 同步到 ASCII 路径 ===")
os.makedirs(构建工程, exist_ok=True)
r = subprocess.run(["robocopy", SRC, 构建工程, "/E", "/XD", "obj", "bin",
                    "/NFL", "/NDL", "/NJH", "/NJS", "/NP"],
                   capture_output=True)
#: robocopy 的退出码：0=没变、1=有拷贝、≥8 才是错
if r.returncode >= 8:
    print("    XX robocopy 失败（退出码 %d）" % r.returncode)
    print((r.stdout or b"").decode("utf-8", "replace")[:800])
    sys.exit(1)
n = sum(len(f) for _, _, f in os.walk(构建工程))
print("    同步完成，%d 个文件" % n)

# ── 2 构建 ─────────────────────────────────────────────────────────────
print()
print("  === 2/3 构建 ===")
#: ⚠ 环境变量要给全：ANDROID_HOME 不给的话 SDK 定位会飘。
env = dict(os.environ,
           JAVA_HOME=JDK,
           ANDROID_HOME=os.path.join(os.environ.get("LOCALAPPDATA", ""), "Android", "Sdk"),
           ANDROID_SDK_ROOT=os.path.join(os.environ.get("LOCALAPPDATA", ""), "Android", "Sdk"),
           DOTNET_CLI_UI_LANGUAGE="en")
参数 = ["dotnet", "build", 工程, "-c", "Release", "--nologo", "-v", "q"]
r = subprocess.run(参数, cwd=构建工程, env=env, capture_output=True, timeout=3600)
出 = (r.stdout or b"").decode("utf-8", "replace") + (r.stderr or b"").decode("utf-8", "replace")
for l in 出.splitlines():
    if "error" in l.lower() or "Build succeeded" in l:
        print("    " + l.strip()[:150])
if r.returncode != 0:
    print("    XX 构建失败（退出码 %d）" % r.returncode)
    sys.exit(1)

# ── 3 搬回产物 ─────────────────────────────────────────────────────────
print()
print("  === 3/3 搬回产物 ===")
APK们 = []
for 根, _, 文 in os.walk(os.path.join(构建工程, "bin")):
    for f in 文:
        if f.endswith(".apk"):
            APK们.append(os.path.join(根, f))
if not APK们:
    print("    XX 没找到 APK")
    sys.exit(1)
出目录 = os.path.join(os.path.dirname(LC), "发布")
os.makedirs(出目录, exist_ok=True)
首选 = [p for p in APK们 if "Signed" in p] or APK们
for P in 首选:
    目标 = os.path.join(出目录, os.path.basename(P))
    shutil.copy2(P, 目标)
    print("    %s  %.2f MB" % (os.path.basename(P), os.path.getsize(P) / 1024 / 1024))
print()
print("    产出在：%s" % 出目录)
print()
print("  ⚠ 装到手机：")
print("      adb install -r \"%s\"" % os.path.join(出目录, os.path.basename(首选[0])))
print("      或把 apk 传到手机点开（首次要允许「未知来源」）")
print()
print("  ⚠⚠ 第一次跑要盯日志：")
print("      adb logcat -s HybridEngineLight:V python.stdout:V python.stderr:V")
print("      （Python 的 print 走 stderr；这边没有 adb，没在真机上验过）")
