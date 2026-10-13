"""打 **Linux（及 macOS）源码包** —— 不依赖 PyInstaller，纯 Python。

⚠ 为什么单独有这个脚本，而不是只用 `build_exe.py`：
  `build_exe.py` 用 PyInstaller 打「解压即用、不需要装 Python」的包。
  但 **PyInstaller 的 bootloader 是预编译的，只对当前平台有效**
  （本机 wheel 里只有 `Windows-64bit-intel/run.exe`）——
  所以**在 Windows 上编不出 Linux 可执行**，那是物理限制，不是配置问题。

  ⇒ 源码包这条路**天然跨平台**：它只是把 `.py` 打包，
    任何有 python3 的 Linux 都能直接跑。而且**中途不经过 PyInstaller**，
    所以 Windows 上也能打、也能验（验的是包内容和启动脚本，不是二进制）。

用法：
    python _build/build_src.py              # 看要打什么
    python _build/build_src.py --go         # 真打

产出：
    <发布>/HybridEngine-light-v<版本>-src.tar.gz        （纯源码）
    <发布>/HybridEngine-light-v<版本>-linux.sh          （一条命令装好并跑）
"""
import io
import os
import re
import shutil
import sys
import tarfile
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
发布 = os.path.join(os.path.dirname(LC), "发布")
APP = "HybridEngine-light"

#: 要打进源码包的东西 —— **课程内容必须全在**，否则学员拿到的是空壳
内容 = [
    "hybridengine_light",   # 引擎 + 课程运行器
    "lessons", "games", "ai", "embedded", "raspi", "engine", "exams",
    "csharp",               # 四条编译型线的课程 + 运行器
    "docs",
    "tools",                # hybridengine-light-ctl 等
    "README.md", "LICENSE",
    "hybridengine-light-ctl", "hybridengine-light-ctl.cmd",
]

#: 不打进去的：构建产物、缓存、答案、编辑器垃圾
排除目录 = {"__pycache__", ".git", "bin", "obj", "node_modules",
            "_build", ".vs", ".idea", "dist", "work"}
排除名字 = {"out.txt", ".DS_Store", "Thumbs.db"}


def 版本():
    P = os.path.join(LC, "hybridengine_light", "__init__.py")
    m = re.search(r'__version__\s*=\s*["\']([^"\']+)',
                  io.open(P, encoding="utf-8").read())
    return m.group(1) if m else "0.0.0"


版本号 = 版本()
tar名 = "%s-v%s-src.tar.gz" % (APP, 版本号)
tar路径 = os.path.join(发布, tar名)
脚本路径 = os.path.join(发布, "%s-v%s-linux.sh" % (APP, 版本号))

#: 顶层目录名 —— 解压后就是这个文件夹，用户进去跑
顶层 = "%s-%s" % (APP, 版本号)


def 该收(相对: str) -> bool:
    段 = 相对.replace("\\", "/").split("/")
    if any(s in 排除目录 for s in 段):
        return False
    if 段[-1] in 排除名字:
        return False
    # ⚠ 答案不进发布包：学员会以为里面已经有答案了
    if "answers" in 段 or "solutions" in 段:
        return False
    return True


print("  HybridEngine-light 源码包 · 版本 %s" % 版本号)
print()
print("  === 要收进来的顶层项 ===")
总文件 = 0
总字节 = 0
要收 = []
for 项 in 内容:
    P = os.path.join(LC, 项)
    if not os.path.exists(P):
        print("    ??  %s（不存在，跳过）" % 项)
        continue
    if os.path.isfile(P):
        要收.append(项)
        n, b = 1, os.path.getsize(P)
    else:
        n = b = 0
        for 根, 子目录们, 文件们 in os.walk(P):
            子目录们[:] = [d for d in 子目录们 if d not in 排除目录]
            for f in 文件们:
                相对 = os.path.relpath(os.path.join(根, f), LC)
                if 该收(相对):
                    n += 1
                    b += os.path.getsize(os.path.join(根, f))
        要收.append(项)
    总文件 += n
    总字节 += b
    print("    %-24s %5d 个文件  %7.1f KB" % (项, n, b / 1024))

print()
print("    合计 %d 个文件，%.1f MB（压缩后会更小）" % (总文件, 总字节 / 1024 / 1024))

if "--go" not in sys.argv:
    print()
    print("  （只读检查。加 --go 才真打）")
    sys.exit(0)

os.makedirs(发布, exist_ok=True)

# ── 打 tar.gz ─────────────────────────────────────────────────────────
#   ⚠⚠ 两个坑，都在这里处理：
#
#   ① **Windows 没有 Unix 的 x 位**。
#      源文件 `hybridengine-light-ctl` 在 Windows 上 stat 出来就是 0o666，
#      `tarfile.add()` 忠实地把 0o666 记进去 ⇒ 解压到 Linux 上
#      **带 shebang 的脚本不能直接跑**（要用户先 chmod +x，那是作者没做好）。
#      实测：第一版打出来 mode=0o666，我核出来了。
#      ⇒ 哪些该是可执行的，**显式写下来**，打包时覆盖 tar 里的 mode。
#
#   ② **目录条目**：`tarfile.add()` 只加文件时，tar 里没有目录条目。
#      大多数解压工具能自己建，但显式加上更稳（有些精简 tar 不会）。
可执行 = {"hybridengine-light-ctl"}
可执行后缀 = (".sh",)

print()
print("  === 打包 ===")
if os.path.isfile(tar路径):
    os.remove(tar路径)
已加 = 0
已加目录 = set()


def _该可执行(相对: str) -> bool:
    return os.path.basename(相对) in 可执行 or 相对.endswith(可执行后缀)


def _加(t, 真实路径, 相对):
    """加一个文件，顺带把它经过的目录也加成目录条目。"""
    global 已加
    相对 = 相对.replace("\\", "/")
    # 目录条目
    段 = 相对.split("/")[:-1]
    for i in range(1, len(段) + 1):
        子 = "/".join(段[:i])
        if 子 not in 已加目录:
            已加目录.add(子)
            ti = tarfile.TarInfo("%s/%s" % (顶层, 子))
            ti.type = tarfile.DIRTYPE
            ti.mode = 0o755
            ti.mtime = int(time.time())
            t.addfile(ti)
    # 文件本身
    ti = t.gettarinfo(真实路径, arcname="%s/%s" % (顶层, 相对))
    if _该可执行(相对):
        ti.mode = 0o755
    else:
        ti.mode = 0o644
    with open(真实路径, "rb") as fh:
        t.addfile(ti, fh)
    已加 += 1


with tarfile.open(tar路径, "w:gz") as t:
    for 项 in 要收:
        P = os.path.join(LC, 项)
        if os.path.isfile(P):
            _加(t, P, 项)
            continue
        for 根, 子目录们, 文件们 in os.walk(P):
            子目录们[:] = [d for d in 子目录们 if d not in 排除目录]
            for f in 文件们:
                F = os.path.join(根, f)
                相对 = os.path.relpath(F, LC)
                if not 该收(相对):
                    continue
                _加(t, F, 相对)
print("     %s  %.1f MB  装了 %d 个文件、%d 个目录"
      % (tar名, os.path.getsize(tar路径) / 1024 / 1024, 已加, len(已加目录)))

# ── 生成一键脚本 ──────────────────────────────────────────────────────
#   ⚠ 这个脚本**必须能自己检查环境**并把缺的东西说清楚 ——
#     学员在 Linux 上第一件事就是撞"Tk 没装 / 没编译器"。
print()
print("  === 写一键脚本 ===")
L = []
L.append("#!/bin/sh")
L.append("# HybridEngine-light %s —— Linux / macOS 启动脚本" % 版本号)
L.append("#")
L.append("# 用法：  sh %s-v%s-linux.sh" % (APP, 版本号))
L.append("#")
L.append("# 它只做三件事：① 找 python3 ② 检查 Tk 在不在 ③ 跑起来。")
L.append("# **不装任何东西**（也不需要联网）——缺什么就告诉你装什么。")
L.append("")
L.append('set -e')
L.append('这里=$(cd "$(dirname "$0")" && pwd)')
L.append('cd "$这里"')
L.append("")
L.append("# ① 找 python3（有些系统只有 python）")
L.append('PY=""')
L.append('for c in python3 python; do')
L.append('  if command -v "$c" >/dev/null 2>&1; then')
L.append('    if "$c" -c "import sys; sys.exit(0 if sys.version_info>=(3,9) else 1)" 2>/dev/null; then')
L.append('      PY="$c"; break')
L.append('    fi')
L.append('  fi')
L.append('done')
L.append('if [ -z "$PY" ]; then')
L.append('  echo "✗ 找不到 python3（需要 3.9 或更新）" >&2')
L.append('  echo "  Debian/Ubuntu:  sudo apt install python3" >&2')
L.append('  echo "  Fedora:         sudo dnf install python3" >&2')
L.append('  exit 1')
L.append('fi')
L.append('echo "✓ 用 $PY（$($PY -V 2>&1)）"')
L.append("")
L.append("# ② 图形界面要 Tk —— 缺了 GUI 起不来，但命令行还能用")
L.append('if "$PY" -c "import tkinter" 2>/dev/null; then')
L.append('  echo "✓ Tk 在（图形界面可用）"')
L.append('else')
L.append('  echo "⚠ 没装 python3-tk —— 图形界面用不了，但命令行能用。"')
L.append('  echo "    Debian/Ubuntu:  sudo apt install python3-tk"')
L.append('  echo "    Fedora:         sudo dnf install python3-tkinter"')
L.append('fi')
L.append("")
L.append("# ③ 跑起来（把命令行参数原样传进去）")
L.append('exec "$PY" -m hybridengine_light "$@"')
io.open(脚本路径, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
os.chmod(脚本路径, 0o755)      # ⚠ Windows 上这个调用是空操作，Linux 上才有效
print("     %s  (%.1f KB)" % (os.path.basename(脚本路径), os.path.getsize(脚本路径) / 1024))
print()
print("  完成：")
print("    %s" % tar路径)
print("    %s" % 脚本路径)
