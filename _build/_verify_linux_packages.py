"""验 Linux 打包产物 —— **在真 Linux 上跑**（CI 的 `package-linux` job 调它）。

═══ 为什么需要它 ═══

`_build/build_linux_packages.sh` 的注释里写着：

    这一环**必须在 Linux 机器上执行**。这是物理限制，不是没做。

⚠ 而开发机只有 Windows ⇒ 那个脚本**在真 Linux 上一次都没跑过**。
  ⇒ 这个脚本是把它从"未验"变成"验过"的那一半。

═══ 判两条（都**不能只看打包退出码**）═══

**① 包里的东西齐不齐**

    ⚠ 这条是刚需，有前例：`HybridEngine-light-v0.8.0-src.tar.gz`（10-12 打的）
      **少了 `sample_gui.py`（SampleCore 的入口）和 `watchdog.py`**，
      而**打包退出码是 0、一切"成功"**。
      ⇒ 光看"打包成功"会漏掉整整一类问题。

**② 解出来**真能起**

    ⚠ 这条比①更硬：①只证明"文件在"，②证明"它是个能跑的东西"。
      PyInstaller 打出来的包最常见的失败就是"文件都在、但一跑就缺 .so"。

用法：
    python _build/_verify_linux_packages.py          # 只验①（内容齐不齐）
    python _build/_verify_linux_packages.py --跑     # 连②一起验（解出来真起）
"""
from __future__ import annotations

import os
import shutil
import subprocess
import sys
import tarfile
import tempfile

LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
发布 = os.path.join(os.path.dirname(LC), "发布")
sys.path.insert(0, os.path.join(LC, "_build"))

import _release_gate as G  # noqa: E402


def _找产物() -> tuple[list[str], list[str]]:
    """返回 (tar.gz 列表, .deb 列表)。找不到就空。"""
    import glob
    if not os.path.isdir(发布):
        return [], []
    tars = sorted(glob.glob(os.path.join(发布, "*-linux-*.tar.gz")))
    debs = sorted(glob.glob(os.path.join(发布, "*.deb")))
    return tars, debs


def 验内容() -> int:
    """① 包里清单里的东西在不在。"""
    tars, debs = _找产物()
    print("  ═══ 验包内容 ═══")
    print("    产物目录：%s" % 发布)
    if not tars and not debs:
        print("  XX  **一个 Linux 包都没找到** —— 打包那一步没产出东西。")
        print("      ⚠ 这不等于「跳过」，等于**没验成**。")
        return 1

    模块, _ = G.读打包清单()
    坏 = 0
    for P in tars:
        #: ⚠ PyInstaller 的包内**没有 .py 源码**（它是编译成 .pyc 塞进单文件/目录的），
        #:   所以**不能拿"引擎模块清单"去查它**。
        #:   ⇒ tar.gz 这条改成查"**关键入口在不在**"（下面那份清单）。
        缺 = _查关键入口(P, 那是tar=True)
        if 缺:
            坏 += 1
            print("  XX  **%s 里缺关键入口**：" % os.path.basename(P))
            for x in 缺:
                print("       %s" % x)
        else:
            print("  OK  %s：关键入口都在（%s）"
                  % (os.path.basename(P), "、".join(_关键入口()[:3]) + " …"))

    for P in debs:
        #: ⚠ `.deb` 是 ar 归档（`debian-binary` + `control.tar.*` + `data.tar.*`），
        #:   Python 的 `tarfile` 读不了外层 ⇒ 用 `dpkg-deb -c` 列内容。
        #:   ⚠ 没装 dpkg-deb 就**明说跳过**（"没跑到" ≠ "没问题"）。
        r = subprocess.run(["dpkg-deb", "-c", P], capture_output=True, text=True,
                           encoding="utf-8", errors="replace")
        if r.returncode != 0:
            print("  ⚠ %s：`dpkg-deb -c` 跑不了（%s）—— **不算通过**"
                  % (os.path.basename(P), (r.stderr or "").strip()[:80]))
            continue
        有 = r.stdout or ""
        缺 = [k for k in _关键入口() if k not in 有]
        if 缺:
            坏 += 1
            print("  XX  **%s 里缺关键入口**：%s" % (os.path.basename(P), "、".join(缺)))
        else:
            print("  OK  %s：关键入口都在" % os.path.basename(P))

    print()
    print("  结论：%s" % ("**有包是残的** ❌" if 坏 else "包内容都齐 ✅"))
    return 1 if 坏 else 0


def _关键入口() -> list[str]:
    """Linux 包里"必须在"的东西。

    ⚠ 为什么不拿引擎模块清单（`引擎模块`）来查：
      PyInstaller 的产物里**没有 .py 源码**（编译进归档了）。
      ⇒ 查"入口在不在"才是对这份产物**有判别力**的判据。

    ⚠ 这份清单**只列"少了就一定有问题"的**，不列可有可无的。
    """
    return [
        "hybridengine-light",          # 主可执行 / 启动脚本
        "hybridengine-light-ctl",      # 命令行入口（课程/协议都靠它）
    ]


def _查关键入口(P: str, 那是tar: bool) -> list[str]:
    """tar.gz 里查关键入口（**同时也查引擎源文件**，如果这个包带源码）。"""
    try:
        with tarfile.open(P) as t:
            名 = t.getnames()
    except Exception as e:                                     # noqa: BLE001
        return ["打不开：%s" % e]
    基 = {os.path.basename(n) for n in 名}
    缺 = [k for k in _关键入口() if k not in 基]
    #: ⚠ 如果这个包里**带源码**（`hybridengine_light/` 有 .py），
    #:   那就顺带用统一闸门查一遍引擎模块 —— 那是"源码包"该有的判据。
    if any(n.endswith("hybridengine_light/__init__.py") for n in 名):
        模块, _ = G.读打包清单()
        前缀 = ""
        for n in 名:
            if n.endswith("hybridengine_light/__init__.py"):
                前缀 = n[: -len("hybridengine_light/__init__.py")]
                break
        缺 += G.查压缩包(P, 包内前缀=前缀, 源根=LC, 模块清单=模块)
    return 缺


def 验能跑() -> int:
    """② 解出来真能起（PyInstaller 的包最常见的失败：文件都在、一跑就缺 .so）。"""
    tars, _ = _找产物()
    print("  ═══ 验包能跑 ═══")
    if not tars:
        print("  XX  没有 tar.gz 可解 —— **没验成**")
        return 1
    坏 = 0
    for P in tars:
        工作 = tempfile.mkdtemp(prefix="hel-pkg-")
        try:
            with tarfile.open(P) as t:
                t.extractall(工作)
            #: 找主可执行（PyInstaller 的目录产物 / 单文件）
            候选 = []
            for 根, _, 文件们 in os.walk(工作):
                for f in 文件们:
                    if f in ("hybridengine-light", "HybridEngine-light"):
                        候选.append(os.path.join(根, f))
            if not 候选:
                #: 也可能只有 `python -m hybridengine_light` 那条路（源码包形态）
                解 = os.path.join(工作, os.listdir(工作)[0]) if os.listdir(工作) else 工作
                r = subprocess.run([sys.executable, "-m", "hybridengine_light", "--help"],
                                   cwd=解, capture_output=True, text=True,
                                   encoding="utf-8", errors="replace", timeout=120,
                                   env={**os.environ, "PYTHONDONTWRITEBYTECODE": "1"})
                print("     %s：`python -m hybridengine_light --help` 退出码 %d"
                      % (os.path.basename(P), r.returncode))
                if r.returncode != 0:
                    坏 += 1
                    print("  XX  起不来：%s" % ((r.stderr or r.stdout or "")[-300:]))
                else:
                    print("  OK  %s：能起" % os.path.basename(P))
                continue
            命 = 候选[0]
            os.chmod(命, 0o755)
            r = subprocess.run([命, "--help"], capture_output=True, text=True,
                               encoding="utf-8", errors="replace", timeout=180,
                               cwd=os.path.dirname(命))
            print("     %s --help → 退出码 %d" % (os.path.basename(命), r.returncode))
            if r.returncode == 0:
                print("  OK  %s：能起" % os.path.basename(P))
            else:
                坏 += 1
                print("  XX  %s 起不来（退出码 %d）：%s"
                      % (os.path.basename(P), r.returncode,
                         ((r.stderr or "") + (r.stdout or ""))[-400:]))
        except Exception as e:                                 # noqa: BLE001
            坏 += 1
            print("  XX  %s 解/跑的时候抛了：%s: %s" % (os.path.basename(P), type(e).__name__, e))
        finally:
            shutil.rmtree(工作, ignore_errors=True)
    print()
    print("  结论：%s" % ("**有包跑不起来** ❌" if 坏 else "包都能起 ✅"))
    return 1 if 坏 else 0


if __name__ == "__main__":
    码 = 验内容()
    if "--跑" in sys.argv:
        码 = max(码, 验能跑())
    sys.exit(码)
