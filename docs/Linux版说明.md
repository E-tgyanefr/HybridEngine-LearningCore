# Linux 版 —— 四种形态、怎么装、哪些能在 Windows 上做

> **一句话**：源码包**已经做好并验过**；另外三种（PyInstaller tar.gz /
> AppImage / .deb）的脚本**已写好、语法（`bash -n`）与"平台闸门"验过**，
> 但**只能在 Linux 上真打** —— 原因不是懒，是物理限制（下面第二节）。
>
> ⚠ **"语法和闸门验过" ≠ "打包逻辑验过"** —— 组 AppDir / 调 appimagetool /
> `dpkg-deb --build` 那几段**一次都没在 Linux 上执行过**。
> 详见 **`docs/跨平台.md`**（那里有一张逐项的「实测 / 半验 / 未验」表）。
>
> 任何平台都能先跑一遍跨平台自检，退出码必须是 0：
>
> ```sh
> python3 _build/_check_platform.py
> ```

---

## 一、四种形态，按"用户要做什么"分

| 形态 | 文件 | 用户要做什么 | 状态 |
|---|---|---|---|
| **① 源码包** | `HybridEngine-light-v0.8.0-src.tar.gz`（1.3 MB） | 解压，`sh xxx-linux.sh` | ✅ **已做好、已验** |
| **② 独立可执行** | `HybridEngine-light-v0.8.0-linux-x86_64.tar.gz` | 解压，`./HybridEngine-light` | ⬜ 脚本已好，**要在 Linux 上打** |
| **③ AppImage** | `HybridEngine-light-v0.8.0-x86_64.AppImage` | `chmod +x` 后双击 | ⬜ 同上 |
| **④ .deb** | `HybridEngine-light_0.8.0_amd64.deb` | `sudo dpkg -i` | ⬜ 同上（Debian 系） |

### 怎么选

```
只想快点用上、不在乎装不装 Python   → ② 或 ③
不想装东西、也不怕装 python3        → ①
要给一堆人装、想走包管理器          → ④
```

⚠ **四门编译型课程（C / C++ / Java / C#）**无论哪种形态都要你自己装工具链：
`.NET SDK` + `g++` / `javac` / `gcc`。原因见下面第四节。
**Python 那几门开箱即用。**

---

## 二、⚠ 为什么另外三种不能在 Windows 上打

### ① PyInstaller **交叉编译不了**

它的 bootloader 是**预编译的二进制**，官方 wheel 里只有当前平台那一份。
本机实测（`PyInstaller 6.22.3`）：

```
...\PyInstaller\bootloader\
    Windows-64bit-intel\run.exe
    Windows-64bit-intel\runw.exe
    ...                                  ← 只有 Windows 的
```

⇒ 在 Windows 上打不出 Linux 可执行。这是设计如此，不是配置问题。

### ② AppImage / .deb 的工具是 Linux 原生的

`appimagetool` 和 `dpkg-deb` 在 Windows 上没有；WSL 里也没装发行版。

### ③ 结论

⇒ **必须在一台 Linux 机器上执行**（或先 `wsl --install -d Ubuntu`）。
`_build/build_linux_packages.sh` 开头就挡住了这种误跑，并直接告诉你怎么办：

```
⚠ 这个脚本只能在 Linux（或 macOS）上跑 —— 现在检测到：MINGW64_NT-10.0-26300
  ...
  真要在这台机器上出 Linux 包，先装 WSL 并建一个发行版：
      wsl --install -d Ubuntu
```

⚠ 那个保护是**实测撞出来的**：在 Git Bash 下跑，`python3` 解析版本号会失败
（拿到 `0.0.0`），一路跑下去只打出一堆看不懂的错 —— 与其半途失败，
不如开头就说清楚。

---

## 三、已经做好的：源码包

```powershell
python _build/build_src.py --go
```

产出两份：

```
引擎/发布/HybridEngine-light-v0.8.0-src.tar.gz    1.3 MB · 324 个文件
引擎/发布/HybridEngine-light-v0.8.0-linux.sh       1.3 KB
```

### 实测验过的（不是"应该没问题"）

| 项 | 结果 |
|---|---|
| 解压 | ✅ 顶层目录正确 |
| **解压后引擎真能跑** | ✅ 退出码 0，列出课程 |
| 答案文件没被误打进去 | ✅ 无 `answers/` `solutions/` |
| 课数 | ✅ Python 55 · C 41 · C++ 41 · Java 41 · C# 41 |
| 可执行位 | ✅ `tar -tvzf` 显示 `-rwxr-xr-x` |

### ⚠ 修掉的一个"只有实测才看得见"的坑

**Windows 没有 Unix 的 x 位**。源文件 `hybridengine-light-ctl` 在 Windows 上
`stat` 出来就是 `0o666`，`tarfile.add()` 忠实地把它记进 tar ⇒ 解压到 Linux 上
**带 shebang 的脚本不能直接跑**（要用户先 `chmod +x`，那是作者没做好）。

⇒ 现在**显式设 0o755**，并用 `tar -tvzf` 确认。

⚠ **但"Linux 上解压后 x 位真的在不在"我验不了** —— 没有 Linux 环境。
  只能证明"tar 里记的是 755"（`tar -tvzf` 是这么显示的），
  而 `python -m tarfile` 解到 Windows 上永远是 666（Windows 本身没有 x 位）。
  **这一条列在"没验到"里。**

### 一键脚本做什么

```sh
sh HybridEngine-light-v0.8.0-linux.sh
```

只做三件事，**不装任何东西、不需要联网**：

```
① 找 python3（要 3.9+）—— 没有就告诉你怎么装
② 检查 Tk 在不在 —— 缺了只用命令行也能用，并告诉你怎么装
③ 跑起来（把命令行参数原样传进去）
```

⚠ 为什么要检查 Tk：**没装 `python3-tk` 时图形界面起不来**，而报错通常
指向别处。这是 Linux 上最常见的第一个坑。

---

## 四、在 Linux 上打另外三种

```sh
cd <你放这个仓库的目录>      # ⚠ 目录名不重要（克隆下来是什么就叫什么），
                            #   认准里面有 _build/ 和 lessons/ 就行 ——
                            #   原来这里写的是 `cd LearnCode`，那是**旧目录名**，
                            #   仓库改名后这条命令会直接失败。
sh _build/build_linux_packages.sh          # 先看环境、看能打什么
sh _build/build_linux_packages.sh --go     # 真打
```

脚本会**先检查环境再动手**，缺什么直接告诉你装什么：

```
python3 · python3-tk          （Tk 缺了只有命令行）
pip install pyinstaller       （打独立可执行用）
appimagetool                  （打 AppImage 用，可选）
dpkg-deb                      （打 .deb 用，Debian 系才有）
```

### 打完之后**必须做的一件事**

```
⚠ 在真机上跑一遍再发。
```

脚本只保证"打出来了"，**不保证"在别人的发行版上能跑"** ——
glibc 版本、Tk 版本、字体、桌面环境都可能不一样。
我在 Windows 上能验的只有"脚本语法对、逻辑对"，**包本身一次都没在 Linux 上跑过**。

### 打之前/之后都该跑一遍的跨平台自检

```sh
python3 _build/_check_platform.py        # 退出码 0 = 本平台这一关过了
```

它查：Python 版本 · **38 个子模块能不能导入** · tkinter · 内容根与课数 ·
**文件编码（源码必须 UTF-8）** · **大小写冲突**（Linux 上会直接覆盖）·
**硬编码 Windows 假设扫描**（清单外的命中 = 失败）· 工具链 · **`bash -n`**。
⚠ 跳过项会**明说「跳过，不算通过」**，不会静默当绿。

### 四门编译型课程要装什么

```
Debian / Ubuntu   sudo apt install dotnet-sdk-8.0 g++ default-jdk
Fedora            sudo dnf install dotnet-sdk-8.0 gcc-c++ java-17-openjdk-devel
```

⚠ Java 要的是 **JDK**，不是 JRE —— JRE 里没有 `javac`。
（这正是第 00 课专门讲的事，调研里"卡在装工具"是放弃率最高的一步。）

---

## 五、这一节没验到的（如实列出来）

| 项 | 为什么没验 |
|---|---|
| Linux 上解压后的 x 位 | 没有 Linux 环境（本机无 WSL 发行版） |
| PyInstaller 打出的 Linux 包能不能跑 | 交叉编译不了 ⇒ 打都没打出来 |
| AppImage / .deb 能不能装 | 工具是 Linux 原生的 |
| 各发行版的兼容性 | 同上 —— **必须真机验，脚本只能保证"打出来了"** |
| `.deb` 的依赖声明对不对 | 没打过真包，`Depends:` 是按常识写的 |
| `_check_platform.py` 在 Linux 上的实际输出 | 脚本在 Windows 上跑过（退出码 0），**Linux 上一次没跑** |
| 组 AppDir / appimagetool / `dpkg-deb` 那几段 | ⚠ 本轮只验了 `bash -n` 与"在非 Linux 上会明确拒绝"（退出码 2）；**打包逻辑本身从没执行过** |

⇒ 这些都是**同一类限制**：这台机器不是 Linux。
   如果你有 Linux 机器（或愿意 `wsl --install -d Ubuntu`），
   上面每一条都能在几分钟内验完。

> 完整的逐项状态（含 **macOS** 那一栏）见 **`docs/跨平台.md`**。
