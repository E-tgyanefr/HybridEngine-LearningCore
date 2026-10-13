"""发布物完整性闸门 —— **打出来的包里，该有的东西在不在。**

═══ 为什么需要它（两次实测事故）═══

## 事故 ①：安卓的 `python.zip` 少了一个模块

    我给 `hybridengine_light/` 加了 `watchdog.py`，改了 `check.py` 用它。
    · `prepare_android_assets.py` 的「引擎模块」清单我**加对了**
    · 那道「清单漏模块」闸门也**证明了会拦**
    · `build_android.py` **退出码 0**、构建成功、装机成功

    ⇒ 结果 APK 的 `assets/python.zip` 里**没有 `watchdog.py`**
      ⇒ 真机上学员**点判分那一刻**才炸。

## 事故 ②：Linux 源码包少了两个模块（**2026-10-13，本文件诞生的直接原因**）

    `HybridEngine-light-v0.8.0-src.tar.gz`（10-12 造的）里：

        hybridengine_light/           35 项
          sample_gui.py                0 项   ← **没有**
          watchdog.py                  0 项   ← **没有**

    ⚠ `sample_gui.py` 是 **SampleCore 的入口**（游戏开发 IDE）——
      **用户拿到包，游戏界面打不开**。而打包脚本退出码 0、一切"成功"。

## ⇒ 两种事故是**同一类**

    **发布物和源码脱节，而且没有人会发现。**

    而它们的表现都是"我这明明是新的，怎么没生效 / 怎么没有" ——
    最容易被误判成"功能坏了"。

═══ 做法 ═══

**不按 mtime 判断。** 事故①已经证明"比时间"不够：
**新加的文件**不会让任何旧文件的 mtime 变新 ⇒ 包看着"不过期"，里面就是少东西。

⇒ 判据是「**清单里的东西在不在包里**」：

    · 引擎模块：`prepare_android_assets.引擎模块` 里每个 `.py` 都要在
    · 课程内容：`课程目录` 里每个文件都要在（**用素材脚本自己的排除规则**）

⚠ 别在这儿再猜一遍"该打包什么" —— 事故①的假红就是这么来的
  （我猜了两轮：路径前缀猜错、排除规则猜错）。
  **"该打包什么"的唯一权威是打包脚本自己的清单。**
"""
from __future__ import annotations

import os
import sys


def 查压缩包(压缩包: str, 包内前缀: str, 源根: str,
             模块清单: list[str] | None = None,
             课程清单: list[str] | None = None,
             课程前缀: str | None = None,
             跳过目录: set[str] | None = None) -> list[str]:
    """查一个 zip/tar.gz 里"清单里的东西"在不在。

    参数：
        `压缩包`      文件路径（`.zip` / `.tar.gz` / `.tgz`）
        `包内前缀`    包内的目录前缀（如 `"python-app/"`；没有就传 `""`）
        `源根`        源码根（找引擎模块与课程用）
        `模块清单`    引擎模块文件名清单（`None` = 不查）
        `课程清单`    课程目录名清单（`None` = 不查）
        `课程前缀`    课程在包内的前缀（默认跟 `包内前缀` 一样）
        `跳过目录`    查课程时要跳过的目录名（**必须和打包脚本一致**）

    返回：**缺什么**的说明列表（空列表 = 全在）。
    """
    import tarfile
    import zipfile

    跳过目录 = 跳过目录 or {"__pycache__", "answers", "solutions", "bin", "obj"}
    课程前缀 = 包内前缀 if 课程前缀 is None else 课程前缀

    包里 = None
    try:
        if 压缩包.endswith(".zip"):
            with zipfile.ZipFile(压缩包) as z:
                包里 = set(z.namelist())
        elif 压缩包.endswith((".tar.gz", ".tgz")):
            with tarfile.open(压缩包) as t:
                包里 = set(t.getnames())
        else:
            return ["（不认识的压缩格式：%s）" % os.path.basename(压缩包)]
    except Exception as e:                                     # noqa: BLE001
        return ["%s 打不开：%s" % (os.path.basename(压缩包), e)]

    漏: list[str] = []

    #: ① 引擎模块 —— 逐个点名（这条最要紧：它就是事故①②的直接原因）
    if 模块清单:
        for m in 模块清单:
            应 = 包内前缀 + "hybridengine_light/" + m
            #: ⚠ tar.gz 的解包目录名是 `<名>-<版本>/`，所以用"结尾匹配"兜一下 ——
            #:   但**只在没找到精确匹配时**（免得把 `a/x.py` 和 `b/x.py` 混掉）。
            if 应 not in 包里 and not any(n.endswith("/" + 应) for n in 包里):
                漏.append("引擎模块 %s 不在包里" % m)

    #: ② 课程内容
    if 课程清单:
        for 名 in 课程清单:
            源 = os.path.normpath(os.path.join(源根, 名))
            if not os.path.isdir(源):
                continue
            样例 = []
            for 根, 目录们, 文件们 in os.walk(源):
                目录们[:] = [d for d in 目录们 if d not in 跳过目录]
                for f in 文件们:
                    if f.endswith(".pyc"):
                        continue
                    相对 = os.path.relpath(os.path.join(根, f), 源根).replace("\\", "/")
                    应 = 课程前缀 + 相对
                    if 应 not in 包里 and not any(n.endswith("/" + 应) for n in 包里):
                        样例.append(相对)
                        if len(样例) >= 3:
                            break
                if len(样例) >= 3:
                    break
            if 样例:
                漏.append("%s 里有文件不在包里，例如 %s" % (名, "、".join(样例)))

    return 漏


def 读打包清单(打包脚本: str = "prepare_android_assets.py"):
    """从打包脚本里**静态读**它的模块清单与课程清单（**唯一权威**）。

    ⚠ 别自己另列一份 —— 事故①的**两次假红**都是"这边猜了一遍"造成的。

    ⚠⚠ **必须静态读，不能 `import`。**

        第一版我写的是 `__import__("prepare_android_assets")` ——
        而 `prepare_android_assets.py` 是个**脚本**（不是模块）：它 import 的时候
        **整个脚本体会跑**（打印清单、甚至真去下载素材）⇒
        这里只是想知道"清单是什么"，却把人家整个打包过程开了一遍。

        ⇒ 用 AST 读**模块级赋值**，不执行任何代码。
          （和 `tests/test_ui_split.py` 里"用 AST 而不是 grep"同一个道理：
           正则会把注释/字符串里的东西也算进来。）
    """
    import ast
    P = os.path.join(os.path.dirname(os.path.abspath(__file__)), 打包脚本)
    模块, 课程 = [], []
    if not os.path.isfile(P):
        return 模块, 课程
    try:
        树 = ast.parse(open(P, encoding="utf-8").read())
    except Exception:                                          # noqa: BLE001
        return 模块, 课程
    for 节 in 树.body:
        if not isinstance(节, ast.Assign):
            continue
        for 目标 in 节.targets:
            if not isinstance(目标, ast.Name):
                continue
            try:
                值 = ast.literal_eval(节.value)
            except Exception:                                  # noqa: BLE001
                continue
            if 目标.id == "引擎模块" and isinstance(值, (list, tuple)):
                模块 = [str(x) for x in 值]
            elif 目标.id == "课程目录" and isinstance(值, (list, tuple)):
                课程 = [str(x) for x in 值]
    return 模块, 课程


def 报告(压缩包: str, 漏: list[str], 前缀说: str = "") -> int:
    """打人话 + 返回退出码（0 = 全在）。"""
    名 = os.path.basename(压缩包)
    if not 漏:
        print("    OK  %s：清单里的东西都在%s" % (名, 前缀说))
        return 0
    print()
    print("  XX  **%s 里缺东西** —— 这个包能发出去，但用户用到才炸：" % 名)
    for x in 漏:
        print("       %s" % x)
    print()
    print("  ⚠ 别只看「打包成功」：**新加的文件不会让旧文件的时间变新** ⇒")
    print("     包看着「不过期」，里面就是少东西。判据只能是「清单里的在不在」。")
    return 1
