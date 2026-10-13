"""打包入口的**模板** —— 由 `_build/build_exe.py` 复制成 `entry.py` 再交给 PyInstaller。

## ⚠ 为什么要有"模板"这一层

原来 `entry.py` 是构建脚本里**用字符串拼出来**的，而且被 .gitignore 忽略。
两个后果，都实测踩到了：

  1. **改不动**：我想给它加兜底错误处理，一改就被下次构建覆盖
  2. **看不见**：它不是仓库里的文件，`git log` 里没有它的历史，
     出事时无从追查（而它恰恰是"程序能不能起来"的入口）

⇒ 现在它是**受版本控制的模板**，构建脚本只做"复制成 entry.py"这一步。
  **改入口逻辑请改这个文件**，不要改构建脚本里的字符串。

## 这一层负责什么

· 把 `HYBRIDENGINE_LIGHT_ROOT` 指向 **exe 旁边**（不是 `_internal/`）——
  这样学员的课程目录、答题文件都在看得见的地方
· **兜底错误处理**：双击 exe 时没有终端能看到 traceback，
  窗口一闪什么都没留下，用户只能说"打不开"，而开发者不在那台机器前。
  ⇒ 任何异常都写到 exe 旁边的 `启动错误.log`，并印出来 + 等回车。
"""
import os
import sys
import traceback


def _记错误(文本: str) -> str:
    """把错误写到 exe 旁边（找不到就写临时目录），返回落盘路径。"""
    try:
        基 = os.path.dirname(os.path.abspath(sys.executable))
    except Exception:
        import tempfile
        基 = tempfile.gettempdir()
    路 = os.path.join(基, "启动错误.log")
    try:
        with open(路, "a", encoding="utf-8") as f:
            f.write("\n" + "=" * 60 + "\n")
            f.write(文本)
        return 路
    except OSError:
        return "（写不进去）"


def main() -> int:
    if getattr(sys, "frozen", False):
        _base = os.path.dirname(os.path.abspath(sys.executable))
        os.environ.setdefault("HYBRIDENGINE_LIGHT_ROOT", _base)

    try:
        from hybridengine_light.run import main as 跑
        return 跑()
    except SystemExit:
        raise
    except BaseException:
        详 = traceback.format_exc()
        路 = _记错误(
            "HybridEngine-light 启动失败\n"
            f"  exe    = {getattr(sys, 'executable', '?')}\n"
            f"  frozen = {getattr(sys, 'frozen', False)}\n"
            f"  argv   = {sys.argv!r}\n\n{详}")
        try:
            print("启动失败，原因如下：\n")
            print(详)
            print(f"\n（这份信息也写进了：{路}）")
            if sys.stdin is not None and sys.stdin.isatty():
                input("\n  按回车键关闭…")
        except Exception:
            pass
        return 1


if __name__ == "__main__":
    sys.exit(main())
