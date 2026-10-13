#!/bin/sh
# HybridEngine-light —— 在 **macOS 上**打 .app（可选再打成 .dmg）
#
# ⚠⚠⚠ 这个脚本**没有在任何真实的 macOS 上跑过**。
#
#   本机（开发机）是 Windows，没有 macOS、也没有 WSL 发行版
#   ⇒ 只做过两件事：
#     ① `bash -n build_macos_app.sh`（语法自检）
#     ② 开头那段"平台闸门 + 前置条件"的分支逻辑在 **Linux/Git Bash 上试跑过**
#        （确认在非 Darwin 上会**明确报错退出**，而不是往下跑出一堆看不懂的错）
#   脚本本体（组 .app、Info.plist、icns、codesign）**一次都没在真机执行过**。
#   ⇒ 在 macOS 上第一次跑它的人：请把输出**原样**记下来。
#
# 用法：
#     sh build_macos_app.sh              # 只看环境、看能打什么（不动手）
#     sh build_macos_app.sh --go         # 真打
#     sh build_macos_app.sh --go --dmg   # 真打，并额外打一个 .dmg
#
# 需要（脚本自己检查，缺什么告诉你装什么）：
#     python3（3.9+）· pip install pyinstaller
#     Xcode Command Line Tools（有 icns 转换 / codesign / hdiutil 时才用到）
#
# 产出（放到 ../发布/）：
#     HybridEngine-light-v<版本>-macos.app.tar.gz      .app 压缩包（一定有）
#     HybridEngine-light-v<版本>-macos.dmg             可选（要 hdiutil）
#
# 为什么可以复用 `build_exe.py`：
#   它已经是**平台自适应**的（`是WIN` / `是MAC` / 可执行名 / 包后缀 / 运行库名），
#   macOS 上它自己就会产出 `HybridEngine-light`（不带 .exe）与 `-macos.tar.gz`。
#   ⇒ **单一实现**，Windows / Linux / macOS 走同一个脚本，不各写一份。

set -e
# ⚠ 变量名**必须 ASCII** —— Git Bash 不认中文变量名（踩过，见环境坑第 5 条）。
LC=$(cd "$(dirname "$0")/.." && pwd)
PUBDIR=$(cd "$LC/.." && pwd)/发布
APP=HybridEngine-light

# ── 0 平台闸门：**先说清楚**，别跑到一半失败 ──────────────────────────
case "$(uname -s)" in
  Darwin*) : ;;
  *)
    echo "⚠ 这个脚本只能在 **macOS** 上跑 —— 现在检测到：$(uname -s)" >&2
    echo "" >&2
    echo "  为什么：.app 是 macOS 的 bundle 格式，Info.plist、iconutil、" >&2
    echo "  codesign、hdiutil 都是 macOS 原生的；PyInstaller 也**交叉编译不了**" >&2
    echo "  （bootloader 是预编译的，官方 wheel 里只有当前平台那一份）。" >&2
    echo "" >&2
    echo "  ⇒ 在别的平台上能做的（**都已经做好了**）：" >&2
    echo "      Windows   : python _build/build_exe.py" >&2
    echo "      Linux     : sh _build/build_linux_packages.sh --go" >&2
    echo "      源码包    : python _build/build_src.py --go      （任何平台）" >&2
    echo "      跨平台自检: python _build/_check_platform.py     （任何平台）" >&2
    exit 2
    ;;
esac

GO=""
WANT_DMG=""
for a in "$@"; do
  [ "$a" = "--go" ] && GO=1
  [ "$a" = "--dmg" ] && WANT_DMG=1
done

VER=$(python3 -c "
import io,re
t=io.open('$LC/hybridengine_light/__init__.py',encoding='utf-8').read()
m=re.search(r'__version__\s*=\s*[\"\x27]([^\"\x27]+)',t)
print(m.group(1) if m else '0.0.0')
" 2>/dev/null || echo "0.0.0")

ARCH=$(uname -m)
echo "════════════════════════════════════════════════════════"
echo "  $APP $VER · macOS · $ARCH"
echo "════════════════════════════════════════════════════════"
echo

# ── 1 前置条件（**先把缺的说清楚**）──────────────────────────────────
echo "── 环境 ──"
MISSING=0
if ! command -v python3 >/dev/null 2>&1; then
  echo "  ✗ python3 没有 —— 装一个："
  echo "      官方安装包  https://www.python.org/downloads/macos/"
  echo "      或 Homebrew  brew install python-tk"
  MISSING=1
else
  echo "  ✓ python3 $(python3 -V 2>&1 | cut -d' ' -f2)"
  # ⚠ macOS 自带的是"系统 python3"，它**可能没有 Tk**（要 python.org 版或 brew python-tk）
  if python3 -c "import tkinter" 2>/dev/null; then
    echo "  ✓ Tk（图形界面可用）"
  else
    echo "  ⚠ 这个 python3 **没有 Tk** —— 图形界面起不来（命令行还能用）"
    echo "      用 python.org 的官方安装包（自带 Tk），或 brew install python-tk"
  fi
fi
if python3 -c "import PyInstaller" 2>/dev/null; then
  echo "  ✓ PyInstaller $(python3 -c 'import PyInstaller;print(PyInstaller.__version__)' 2>/dev/null)"
else
  echo "  ⚠ PyInstaller 没装 —— 打不了 .app"
  echo "      python3 -m pip install --user pyinstaller"
  echo "      （或 python3 -m venv .venv && . .venv/bin/activate && pip install pyinstaller）"
fi
for t in iconutil sips codesign hdiutil; do
  if command -v "$t" >/dev/null 2>&1; then
    echo "  ✓ $t"
  else
    echo "  ⚠ $t 没有 —— 需要 Xcode Command Line Tools: xcode-select --install"
  fi
done
echo

# ⚠ 必须比 "= 1"，不能写 `[ -n "$MISSING" ]` ——
#   `MISSING=0` 是**非空字符串**，`-n` 判定为真 ⇒ 明明有 python3 也会报"缺"。
#   （这个是"假 uname + 真环境检查"试跑时抓到的：`-n` 永远真。`bash -n` 看不出来。）
if [ "$MISSING" = 1 ]; then
  echo "✗ 缺 python3，先装好再回来。" >&2
  exit 1
fi
if ! python3 -c "import PyInstaller" 2>/dev/null; then
  echo "⚠ 没有 PyInstaller ⇒ 现在打不出 .app。装上再跑："
  echo "      python3 -m pip install --user pyinstaller"
  [ -z "$GO" ] && exit 0
  exit 1
fi

if [ -z "$GO" ]; then
  echo "（这是只读检查。加 --go 才真打；要 .dmg 再加 --dmg）"
  echo
  echo "会做这些事："
  echo "  ① python3 _build/build_exe.py      → 产出 onedir（可执行 + 课程内容 + _internal/）"
  echo "  ② 包成 $APP.app（Contents/MacOS 放本体、Info.plist、Resources/icon.icns）"
  echo "  ③ 可选 ad-hoc 签名（codesign -s -）—— ⚠ 不是公证，别人的机器上仍会被 Gatekeeper 拦"
  echo "  ④ 自检：跑一次产物（HYBRIDENGINE_LIGHT_CLI=1 $APP __诊断__）"
  echo "  ⑤ 打 tar.gz（+ 可选 .dmg），放到 $PUBDIR"
  exit 0
fi

mkdir -p "$PUBDIR"

# ── 2 先用 build_exe.py 产出 onedir（**单一实现**）────────────────────
echo "── 2 产出 onedir（复用 _build/build_exe.py）──"
python3 "$LC/_build/build_exe.py"
ONEDIR="$PUBDIR/$APP"
if [ ! -d "$ONEDIR" ]; then
  echo "✗ build_exe.py 跑完了，但没看到 $ONEDIR" >&2
  echo "  ⇒ 去 $PUBDIR 里看看它到底产出了什么。" >&2
  exit 1
fi
echo "  ✓ $ONEDIR"
echo

# ── 3 组装 .app ───────────────────────────────────────────────────────
# 布局（**故意和 onedir 一模一样**）：
#     $APP.app/Contents/MacOS/$APP         ← 真正的可执行
#     $APP.app/Contents/MacOS/_internal/   ← PyInstaller 运行时
#     $APP.app/Contents/MacOS/lessons/ …   ← 课程内容
#     $APP.app/Contents/Resources/icon.icns
#     $APP.app/Contents/Info.plist
#
# ⚠ 为什么课程内容放在 Contents/MacOS/ 而不是 Resources/：
#   `_build/entry_template.py` 把内容根设成 `dirname(sys.executable)`
#   —— 也就是可执行文件**旁边**。放 Resources/ 的话得改入口逻辑，
#   而**入口那条路径在 Windows 上是验过的**、这里改不了也验不了。
#   ⇒ 不动入口，让目录结构去适配它（Contents/MacOS 正是 CFBundleExecutable 的家）。
echo "── 3 组装 $APP.app ──"
APPDIR="$PUBDIR/$APP.app"
rm -rf "$APPDIR"
mkdir -p "$APPDIR/Contents/MacOS" "$APPDIR/Contents/Resources"

# 把 onedir 的内容整体挪进 Contents/MacOS/
for f in "$ONEDIR"/* "$ONEDIR"/.[!.]*; do
  [ -e "$f" ] || continue
  mv "$f" "$APPDIR/Contents/MacOS/"
done
rmdir "$ONEDIR" 2>/dev/null || true
EXE="$APPDIR/Contents/MacOS/$APP"
if [ ! -f "$EXE" ]; then
  echo "✗ 没找到可执行文件：$EXE" >&2
  echo "  目录里实际有：$(ls -1 "$APPDIR/Contents/MacOS" | head -20 | tr '\n' ' ')" >&2
  exit 1
fi
chmod +x "$EXE"
echo "  ✓ Contents/MacOS/（$(ls -1 "$APPDIR/Contents/MacOS" | wc -l | tr -d ' ') 项）"

# Info.plist
cat > "$APPDIR/Contents/Info.plist" <<EOS
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>$APP</string>
  <key>CFBundleDisplayName</key><string>HybridEngine-light 学编程</string>
  <key>CFBundleExecutable</key><string>$APP</string>
  <key>CFBundleIdentifier</key><string>com.hybridengine.light</string>
  <key>CFBundleVersion</key><string>$VER</string>
  <key>CFBundleShortVersionString</key><string>$VER</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>CFBundleIconFile</key><string>icon</string>
  <key>LSMinimumSystemVersion</key><string>11.0</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>NSRequiresAquaSystemAppearance</key><false/>
</dict>
</plist>
EOS
echo "  ✓ Info.plist"

# 图标：docs/icon.png -> icon.icns（要 sips + iconutil）
if [ -f "$LC/docs/icon.png" ] && command -v sips >/dev/null 2>&1 \
   && command -v iconutil >/dev/null 2>&1; then
  ICONSET=$(mktemp -d)/icon.iconset
  mkdir -p "$ICONSET"
  for sz in 16 32 128 256 512; do
    sips -z $sz $sz "$LC/docs/icon.png" --out "$ICONSET/icon_${sz}x${sz}.png" >/dev/null 2>&1 || true
    d=$((sz * 2))
    sips -z $d $d "$LC/docs/icon.png" --out "$ICONSET/icon_${sz}x${sz}@2x.png" >/dev/null 2>&1 || true
  done
  if iconutil -c icns "$ICONSET" -o "$APPDIR/Contents/Resources/icon.icns" 2>/dev/null; then
    echo "  ✓ Resources/icon.icns"
  else
    echo "  ⚠ iconutil 失败 —— 没图标也能跑（只是 Finder 里是白纸图标）"
  fi
  rm -rf "$(dirname "$ICONSET")"
else
  echo "  ⚠ 跳过图标（没有 docs/icon.png，或没有 sips/iconutil）—— 不影响运行"
fi

# 顺手放一份纯文本说明，和 Windows 的「怎么用.txt」对齐
if [ -f "$APPDIR/Contents/MacOS/怎么用.txt" ]; then
  cp "$APPDIR/Contents/MacOS/怎么用.txt" "$APPDIR/Contents/Resources/怎么用.txt" 2>/dev/null || true
fi
echo

# ── 4 自检：产物**真的能跑**吗 ────────────────────────────────────────
# ⚠ 和 Windows 一样：自检必须跑**命令行模式**（HYBRIDENGINE_LIGHT_CLI=1），
#   否则它会开 GUI —— 而 GUI 不会退出，脚本会一直挂着。
echo "── 4 自检：真跑一次产物 ──"
OUT=$(HYBRIDENGINE_LIGHT_CLI=1 "$EXE" __诊断__ 2>&1 || true)
if printf '%s' "$OUT" | grep -q "frozen      = True"; then
  printf '%s\n' "$OUT" | grep -E "frozen|内容根|lessons 数|Tk 版本" | sed 's/^/       /'
  echo "     ✓ 自检通过（产物真的能跑）"
else
  echo "     ✗ 自检失败。产物输出：" >&2
  printf '%s\n' "$OUT" | head -15 | sed 's/^/        /' >&2
  echo "       ⇒ 不要发布这个 .app。" >&2
  exit 1
fi
echo

# ── 5 可选：ad-hoc 签名 ───────────────────────────────────────────────
if command -v codesign >/dev/null 2>&1; then
  if codesign --force --deep --sign - "$APPDIR" 2>/dev/null; then
    echo "  ✓ ad-hoc 签名（codesign -s -）"
    echo "    ⚠⚠ 这**不是公证**（notarization）。别人的机器上第一次打开仍会被"
    echo "       Gatekeeper 拦（「无法验证开发者」），要在 系统设置→隐私与安全性 里放行，"
    echo "       或者你自己用开发者 ID 重签 + 公证。"
  else
    echo "  ⚠ ad-hoc 签名失败 —— 不影响本机运行，但别人拿到会被 Gatekeeper 拦"
  fi
else
  echo "  ⚠ 没有 codesign —— 跳过签名（别人拿到会被 Gatekeeper 拦）"
fi
echo

# ── 6 打包 ────────────────────────────────────────────────────────────
echo "── 6 打包 ──"
TARBALL="$PUBDIR/$APP-v$VER-macos.app.tar.gz"
rm -f "$TARBALL"
# ⚠ 用 `tar -C 目录 名字` 而不是 `tar 全路径`：后者会把一串父目录也打进包
tar -czf "$TARBALL" -C "$PUBDIR" "$APP.app"
echo "  ✓ $TARBALL  ($(du -h "$TARBALL" | cut -f1))"

if [ -n "$WANT_DMG" ] && command -v hdiutil >/dev/null 2>&1; then
  DMG="$PUBDIR/$APP-v$VER-macos.dmg"
  rm -f "$DMG"
  STAGE=$(mktemp -d)
  cp -R "$APPDIR" "$STAGE/"
  ln -s /Applications "$STAGE/Applications"
  if hdiutil create -volname "$APP" -srcfolder "$STAGE" -ov -format UDZO "$DMG" >/dev/null; then
    echo "  ✓ $DMG  ($(du -h "$DMG" | cut -f1))"
  else
    echo "  ⚠ hdiutil 失败 —— tar.gz 还在，够用"
  fi
  rm -rf "$STAGE"
elif [ -n "$WANT_DMG" ]; then
  echo "  ⚠ 要 --dmg 但没有 hdiutil —— 跳过（xcode-select --install）"
fi
echo

echo "════════════════════════════════════════════════════════"
echo "  成品在：$PUBDIR"
ls -1 "$PUBDIR" | grep -Ei "macos|\.app|\.dmg" || true
echo "════════════════════════════════════════════════════════"
echo
echo "⚠⚠ 这个脚本**从没在真实 macOS 上跑过**（开发机是 Windows）。"
echo "   上面每一步都可能有问题 —— 第一次跑请把输出原样记下来，"
echo "   并**在别人的 Mac 上试开一次**再发。"
