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
        缺 = _查关键入口(P, 那是tar=True)
        if 缺:
            坏 += 1
            print("  XX  **%s 里缺东西**：" % os.path.basename(P))
            for x in 缺:
                print("       %s" % x)
        else:
            print("  OK  %s：该有的路径都在" % os.path.basename(P))

    for P in debs:
        缺 = _查关键入口(P, 那是tar=False)
        if 缺:
            坏 += 1
            print("  XX  **%s 里缺东西**：" % os.path.basename(P))
            for x in 缺:
                print("       %s" % x)
        else:
            print("  OK  %s：该有的路径都在" % os.path.basename(P))

    print()
    print("  结论：%s" % ("**有包是残的** ❌" if 坏 else "包内容都齐 ✅"))
    return 1 if 坏 else 0


def _关键入口() -> list[str]:
    """Linux 包里"必须在"的**路径后缀**（按真实布局，不是猜 basename）。

    ⚠⚠ **这一版是 CI 抓出来的第二版。**

        第一版我猜的是"可执行的 basename 就叫 `hybridengine-light`" ——
        **两处都猜错**，红了：
          · `tar.gz` 里是 PyInstaller 的产物，入口在 `HybridEngine-light/` **子目录**里
          · `.deb` 里 `hybridengine-light` 在 `usr/bin/`，而 `ctl` **根本没有**

        ⇒ 教训和第①版一样（**"该打包什么"不能靠猜**）：判据要照着
          **打包脚本真实的产物布局**写。这一次是读了 `build_linux_packages.sh`
          里"搬/装"那几步才定下来的。

    真实布局（读脚本得到，不是推测）：

        tar.gz（PyInstaller 独立包）
            HybridEngine-light/HybridEngine-light       ← 主可执行
            HybridEngine-light/<课程目录>/              ← 832 个内容文件
            HybridEngine-light/怎么用.txt

        .deb（**源码形态**）
            usr/bin/hybridengine-light                   ← sh 包装器（PYTHONPATH + python3 -m）
            usr/lib/hybridengine-light/hybridengine_light/...  ← 整个源码包
            usr/share/applications/*.desktop

    ⚠ 所以判据是**"路径后缀在不在"**，不是"basename 等不等于某个名字"：
      同一个可执行，在 tar.gz 里是 `.../HybridEngine-light/HybridEngine-light`，
      在 .deb 里是 `usr/bin/hybridengine-light`。
    """
    return [
        #: 主入口（两种形态的路径后缀都算）
        "hybridengine-light",
        "HybridEngine-light",
        #: 内容根 —— 少了它，界面能起但**一节课都看不到**
        "lessons",
    ]


def _查关键入口(P: str, 那是tar: bool) -> list[str]:
    """查包里"该有的路径"在不在。

    ⚠ 用**路径后缀**匹配（不是 basename）：`.../HybridEngine-light/HybridEngine-light`
      这种形态下，basename 匹配会漏，而"以 `/名字` 结尾"能同时覆盖两种布局。
    """
    if 那是tar:
        try:
            import tarfile as _tf
            with _tf.open(P) as t:
                名 = t.getnames()
        except Exception as e:                                 # noqa: BLE001
            return ["打不开：%s" % e]
    else:
        #: `.deb` 是 ar 归档（`debian-binary` + `control.tar.*` + `data.tar.*`），
        #: Python 的 tarfile 读不了外层 ⇒ 用 `dpkg-deb -c` 列路径。
        r = subprocess.run(["dpkg-deb", "-c", P], capture_output=True, text=True,
                           encoding="utf-8", errors="replace")
        if r.returncode != 0:
            return ["`dpkg-deb -c` 跑不了（%s）—— **不算通过**"
                    % (r.stderr or "").strip()[:70]]
        #: `dpkg-deb -c` 的行形如 `-rwxr-xr-x root/root 123 2026-.. ./usr/bin/x`
        #: ⇒ 取出最后那个路径段
        名 = []
        for l in (r.stdout or "").split("\n"):
            段 = l.split()
            if 段 and 段[-1].startswith(("./", "/")):
                名.append(段[-1].lstrip("./"))

    名集 = set(名)
    小集 = {n.lower() for n in 名集}

    def _有(k: str) -> bool:
        """包里有没有"以 `k` 结尾的路径段"。

        ⚠ 用**后缀**匹配（不是 basename 相等），因为：
          · tar.gz 里主入口是 `HybridEngine-light/HybridEngine-light`
            —— 末段是**大写**的 `HybridEngine-light`，而清单里是小写
          · `.deb` 里是 `usr/bin/hybridengine-light` —— 前缀完全不同
        ⇒ 只有"**路径以 `/<名字>` 结尾**"能同时覆盖两种布局。

        ⚠ 也比一次小写（大小写不敏感）—— 那两个名字本来就只是大小写不同，
          把"大小写不一致"当失败没有意义。
        """
        k = k.lower()
        return any(n == k or n.endswith("/" + k) or n.rstrip("/").endswith("/" + k)
                   for n in 小集)

    缺 = [k for k in _关键入口() if not _有(k)]
    #: ⚠ 带源码的包（`.deb` 那种）要顺带查引擎模块 —— 那是"源码包"该有的判据。
    #:   ⚠ 但**只查".py 在不在"**，不查"内容目录在不在"：
    #:     `.deb` 里课程内容在 `usr/share/` 下，路径与源码树不同，
    #:     硬套源码树的相对路径会**假红**（第一版就是这么红的）。
    if any(n.endswith("hybridengine_light/__init__.py") for n in 名集):
        模块, _ = G.读打包清单()
        for m in 模块:
            if not any(n.endswith("hybridengine_light/" + m) for n in 名集):
                缺.append("引擎模块 %s" % m)
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
