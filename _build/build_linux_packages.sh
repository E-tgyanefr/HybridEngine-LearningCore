#!/bin/sh
# HybridEngine-light —— 在 **Linux 上**打 AppImage / .deb / tar.gz
#
# ⚠⚠ 为什么这个脚本是 .sh 而不是 .py：
#   它**只能在 Linux 上跑**。AppImage 要 appimagetool、.deb 要 dpkg-deb ——
#   都是 Linux 原生的，Windows 上没有（也没有能替代的）。
#   而 PyInstaller 交叉编译也走不通：它的 bootloader 是**预编译的**，
#   官方 wheel 里只有当前平台那一份（实测本机只有 `Windows-64bit-intel/run.exe`）。
#
#   ⇒ 所以这一环**必须在 Linux 机器上执行**。这是物理限制，不是没做。
#
# 用法：
#     sh build_linux_packages.sh              # 看要装什么、能打什么
#     sh build_linux_packages.sh --go         # 真打
#
# 需要（脚本会自己检查，缺什么告诉你装什么）：
#     python3 · python3-tk · python3-pip
#     pip install pyinstaller                 （打独立可执行用）
#     appimagetool                            （打 AppImage 用，可选）
#     dpkg-deb                                （打 .deb 用，Debian 系才有）
#
# 产出（放到 ../发布/）：
#     HybridEngine-light-v<版本>-linux-<架构>.tar.gz    PyInstaller 独立包
#     HybridEngine-light-v<版本>-<架构>.AppImage        单文件，双击就跑
#     HybridEngine-light-v<版本>_<架构>.deb             Debian/Ubuntu 包

set -e
#: 这个脚本在 LearnCode/_build/ 下 ⇒ 往上两级才是仓库根
LC=$(cd "$(dirname "$0")/.." && pwd)
PUBDIR=$(cd "$LC/.." && pwd)/发布

# ⚠⚠ 先挡住"在 Windows 上误跑"这种情况。
#   这个脚本**只能在 Linux 上真跑**（AppImage 要 appimagetool、
#   .deb 要 dpkg-deb，都是 Linux 原生的），但 Git Bash 也能执行到这儿。
#   实测：Git Bash 下 `uname -s` 是 `MINGW64_NT-...`，而 `python3` 解析版本号
#   会失败（拿到 0.0.0）—— 一路跑下去只打出一堆看不懂的错。
#   ⇒ 与其半途失败，不如**开头就说清楚**，并告诉对方在 Windows 上能做什么。
case "$(uname -s)" in
  Linux*|Darwin*) : ;;
  *)
    echo "⚠ 这个脚本只能在 Linux（或 macOS）上跑 —— 现在检测到：$(uname -s)" >&2
    echo "" >&2
    echo "  为什么：AppImage 要 appimagetool、.deb 要 dpkg-deb，都是 Linux 原生的；" >&2
    echo "  而 PyInstaller **交叉编译不了** —— 它的 bootloader 是预编译的，" >&2
    echo "  官方 wheel 里只有当前平台那一份（Windows 上只有 Windows-64bit-intel/run.exe）。" >&2
    echo "" >&2
    echo "  ⇒ 在 Windows 上能做的（**都已经做好了**）：" >&2
    echo "      ① 源码包：python _build/build_src.py --go" >&2
    echo "      ② 本脚本的语法与逻辑（能验，但打不出真包）" >&2
    echo "" >&2
    echo "  真要在这台机器上出 Linux 包，先装 WSL 并建一个发行版：" >&2
    echo "      wsl --install -d Ubuntu" >&2
    exit 2
    ;;
esac

GO=""
[ "$1" = "--go" ] && GO=1

VER=$(python3 -c "
import io,re,sys
t=io.open('$LC/hybridengine_light/__init__.py',encoding='utf-8').read()
m=re.search(r'__version__\s*=\s*[\"\x27]([^\"\x27]+)',t)
print(m.group(1) if m else '0.0.0')
" 2>/dev/null || echo "0.0.0")

ARCH=$(uname -m)
APP=HybridEngine-light

echo "════════════════════════════════════════════════════════"
echo "  $APP $VER · $ARCH · $(uname -s)"
echo "════════════════════════════════════════════════════════"
echo

# ── 0 环境检查（**先把缺的说清楚**，别打到一半失败）──────────────────
echo "── 环境 ──"
MISSING=0
if ! command -v python3 >/dev/null 2>&1; then
  echo "  ✗ python3 没有"; MISSING=1
else
  echo "  ✓ python3 $(python3 -V 2>&1 | cut -d' ' -f2)"
fi
if python3 -c "import tkinter" 2>/dev/null; then
  echo "  ✓ Tk（图形界面可用）"
else
  echo "  ⚠ Tk 没装 —— 图形界面用不了（命令行还能用）"
  echo "      Debian/Ubuntu:  sudo apt install python3-tk"
  echo "      Fedora:         sudo dnf install python3-tkinter"
fi
HAS_PI=0
if python3 -c "import PyInstaller" 2>/dev/null; then
  HAS_PI=1; echo "  ✓ PyInstaller $(python3 -c 'import PyInstaller;print(PyInstaller.__version__)')"
else
  echo "  ⚠ PyInstaller 没装 —— 打不了独立包"
  echo "      pip install pyinstaller   （或用 python3 -m venv .venv 再装）"
fi
HAS_AI=0
if command -v appimagetool >/dev/null 2>&1; then
  HAS_AI=1; echo "  ✓ appimagetool"
elif command -v appimagetool-x86_64.AppImage >/dev/null 2>&1; then
  HAS_AI=1; echo "  ✓ appimagetool-x86_64.AppImage"
else
  echo "  ⚠ appimagetool 没有 —— 打不了 AppImage（可选）"
  echo "      下载：https://github.com/AppImage/appimagetool/releases"
fi
HAS_DEB=0
if command -v dpkg-deb >/dev/null 2>&1; then
  HAS_DEB=1; echo "  ✓ dpkg-deb"
else
  echo "  ⚠ dpkg-deb 没有 —— 打不了 .deb（非 Debian 系正常）"
fi
echo

if [ -z "$GO" ]; then
  echo "（这是只读检查。加 --go 才真打）"
  echo
  echo "能打的："
  [ "$HAS_PI" = 1 ]  && echo "  · PyInstaller 独立包 → $APP-v$VER-linux-$ARCH.tar.gz"
  [ "$HAS_AI" = 1 ]  && echo "  · AppImage            → $APP-v$VER-$ARCH.AppImage"
  [ "$HAS_DEB" = 1 ] && echo "  · .deb                → ${APP}_${VER}_$ARCH.deb"
  exit 0
fi

mkdir -p "$PUBDIR"
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

# ── 1 PyInstaller 独立包 ─────────────────────────────────────────────
if [ "$HAS_PI" = 1 ]; then
  echo "── 1 PyInstaller 独立包 ──"
  # ⚠ 复用 build_exe.py，它会按平台自动选名字和归档格式 ——
  #   这一条是**单一实现**，Linux 和 Windows 走同一个脚本，不会各写各的。
  python3 "$LC/_build/build_exe.py"
  PKG="$PUBDIR/$APP-v$VER-linux-$ARCH.tar.gz"
  if [ -f "$PKG" ]; then
    echo "  ✓ $PKG"
  else
    echo "  ✗ 没打出预期的包名，看看 $PUBDIR 里多了什么"
  fi
  echo
fi

# ── 2 AppDir（AppImage 和 .deb 都用这个目录树）──────────────────────
NEED_STANDALONE=0
[ "$HAS_PI" = 1 ] && NEED_STANDALONE=1
if [ "$HAS_AI" = 1 ] || [ "$HAS_DEB" = 1 ]; then
  echo "── 2 组装 AppDir ──"
  appdir="$tmp/$APP.AppDir"
  mkdir -p "$appdir/usr/bin" "$appdir/usr/share/applications" \
           "$appdir/usr/share/icons/hicolor/256x256/apps"
  if [ "$NEED_STANDALONE" = 1 ]; then
    # 用 PyInstaller 的产物当有效载荷（自带 Python，用户不用装）
    cp -r "$PUBDIR/$APP/." "$appdir/usr/bin/"
  else
    # 没 PyInstaller 就放源码 + 一个启动器（用户需要 python3）
    mkdir -p "$appdir/usr/bin/$APP"
    for d in hybridengine_light lessons games ai embedded raspi engine exams csharp docs; do
      [ -e "$LC/$d" ] && cp -r "$LC/$d" "$appdir/usr/bin/$APP/"
    done
    cat > "$appdir/usr/bin/$APP/启动.sh" <<'EOS'
#!/bin/sh
exec python3 -m hybridengine_light "$@"
EOS
    chmod +x "$appdir/usr/bin/$APP/启动.sh"
  fi
  # .desktop —— AppImage 和 .deb 都要
  cat > "$appdir/$APP.desktop" <<EOS
[Desktop Entry]
Type=Application
Name=HybridEngine-light
Name[zh_CN]=HybridEngine-light 学编程
Comment=零依赖的编程教学项目（Python / C / C++ / Java / C#）
Exec=$APP
Icon=$APP
Terminal=true
Categories=Education;Development;
EOS
  cp "$appdir/$APP.desktop" "$appdir/usr/share/applications/"
  # AppImage 要求在 AppDir 根目录有 desktop 和一个图标
  if [ -f "$LC/docs/icon.png" ]; then
    cp "$LC/docs/icon.png" "$appdir/$APP.png"
  else
    # 没图标就生成一个纯色的（AppImage 要求必须有）
    python3 - "$appdir/$APP.png" <<'EOS' 2>/dev/null || true
import struct, sys, zlib
def png(p, w=256, h=256, rgb=(74,158,255)):
    raw = b"".join(b"\x00" + bytes(rgb)*w for _ in range(h))
    def ch(t, d):
        c = t + d
        return struct.pack(">I", len(d)) + c + struct.pack(">I", zlib.crc32(c) & 0xffffffff)
    open(p, "wb").write(b"\x89PNG\r\n\x1a\n"
        + ch(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
        + ch(b"IDAT", zlib.compress(raw)) + ch(b"IEND", b""))
png(sys.argv[1])
EOS
  fi
  cp "$appdir/$APP.png" "$appdir/usr/share/icons/hicolor/256x256/apps/" 2>/dev/null || true
  echo "  ✓ $appdir"
  echo
fi

# ── 3 AppImage ───────────────────────────────────────────────────────
if [ "$HAS_AI" = 1 ]; then
  echo "── 3 AppImage ──"
  AIT=$(command -v appimagetool || command -v appimagetool-x86_64.AppImage)
  # ⚠ AppImage 要 FUSE；容器/CI 里常常没有 ⇒ 用 --appimage-extract-and-run
  out="$PUBDIR/$APP-v$VER-$ARCH.AppImage"
  if ARCH=$ARCH "$AIT" --appimage-extract-and-run "$appdir" "$out" 2>/dev/null \
     || ARCH=$ARCH "$AIT" "$appdir" "$out"; then
    chmod +x "$out"
    echo "  ✓ $out  ($(du -h "$out" | cut -f1))"
  else
    echo "  ✗ appimagetool 失败（常见原因：没有 FUSE）"
  fi
  echo
fi

# ── 4 .deb ───────────────────────────────────────────────────────────
if [ "$HAS_DEB" = 1 ]; then
  echo "── 4 .deb ──"
  deb="$tmp/${APP}_${VER}_$ARCH"
  mkdir -p "$deb/DEBIAN"
  cp -r "$appdir/usr" "$deb/"
  # 依赖：真的用得到的才写。PyInstaller 包自带 Python，就不依赖 python3
  DEPS="libc6"
  [ "$NEED_STANDALONE" = 0 ] && DEPS="python3 (>= 3.9), python3-tk, libc6"
  cat > "$deb/DEBIAN/control" <<EOS
Package: $(echo "$APP" | tr 'A-Z' 'a-z')
Version: $VER
Section: education
Priority: optional
Architecture: $(dpkg --print-architecture 2>/dev/null || echo amd64)
Depends: $DEPS
Maintainer: HybridEngine-light
Description: 零依赖的编程教学项目
 五门语言（Python / C / C++ / Java / C#）共 219 课，
 覆盖 CCF GESP 一级到八级。另有小游戏、单片机与树莓派模拟器、
 人工智能入门、考试系统。
 .
 安装后命令： hybridengine-light
EOS
  # 启动器：装一个进 PATH 的命令
  mkdir -p "$deb/usr/bin"
  if [ "$NEED_STANDALONE" = 1 ]; then
    cat > "$deb/usr/bin/hybridengine-light" <<EOS
#!/bin/sh
exec /usr/bin/$APP/$APP "\$@"
EOS
  else
    cat > "$deb/usr/bin/hybridengine-light" <<EOS
#!/bin/sh
exec python3 -m hybridengine_light "\$@"
EOS
    # 源码模式下模块要能被 python3 找到 ⇒ 装到 site-packages 旁边
    mkdir -p "$deb/usr/lib/hybridengine-light"
    cp -r "$LC/hybridengine_light" "$deb/usr/lib/hybridengine-light/"
    cat > "$deb/usr/bin/hybridengine-light" <<EOS
#!/bin/sh
PYTHONPATH=/usr/lib/hybridengine-light\${PYTHONPATH:+:\$PYTHONPATH} \\
  exec python3 -m hybridengine_light "\$@"
EOS
  fi
  chmod +x "$deb/usr/bin/hybridengine-light"
  out="$PUBDIR/${APP}_${VER}_${ARCH}.deb"
  dpkg-deb --build --root-owner-group "$deb" "$out" >/dev/null
  echo "  ✓ $out  ($(du -h "$out" | cut -f1))"
  echo
fi

echo "════════════════════════════════════════════════════════"
echo "  成品在：$PUBDIR"
ls -1 "$PUBDIR" | grep -Ei "linux|AppImage|\.deb" || true
echo "════════════════════════════════════════════════════════"
echo
echo "⚠ 还要做的一件事：**在真机上跑一遍**再发。"
echo "  这里只保证「打出来了」，不保证「在别人的发行版上能跑」——"
echo "  不同发行版的 glibc / Tk / 字体都可能不一样。"
