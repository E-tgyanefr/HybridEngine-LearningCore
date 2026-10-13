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

#: 构建要的 JDK —— 系统 PATH 上是 25，而 .NET Android 拒绝
JDK = r"D:\AndroidToolchain\jdk21\jdk-21.0.5+11"
print("    %s JDK 21（%s）" % ("OK " if os.path.isdir(JDK) else "XX ", JDK))
if not os.path.isdir(JDK):
    print("      ⇒ 没装 JDK 21。系统 PATH 上的 JDK 25 会被 .NET Android 拒绝：")
    print("        error XA0030: Building with JDK version 25.0.3 is not supported.")
    sys.exit(1)

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
