"""终端里的代码编辑器 —— 学员写完代码不用离开命令行。

## 为什么要有它

学员的流程是「看课 → 写代码 → 检查」。中间那一步原本要他们自己去开记事本/VS Code，
然后再回到终端 —— 对第一次写代码的人来说，**光"用什么写"就是一道墙**。
所以直接给一个内置的。

## ⚠ 为什么不用 `curses`

Windows 上**标准库不带** `curses`（`pip install windows-curses` 才行）——
而这个项目铁律是**零第三方依赖**。
⇒ 用 ANSI 转义序列自己画，用 `msvcrt` 自己读键。

## ⚠ Windows 上 ANSI 要先打开

Windows 10+ 的控制台默认**不解释** ANSI 转义（会打出 `←[2J` 这种乱码）。
必须先 `SetConsoleMode(ENABLE_VIRTUAL_TERMINAL_PROCESSING)`。
这条不做的话，编辑器在 Windows 上就是"一屏乱码" —— 而它恰恰是主要目标平台。

## 支持的操作

    ←→↑↓ / Home / End        移动光标
    PgUp / PgDn               翻页
    Enter                     换行（会自动保持缩进）
    Backspace / Delete        删除
    Tab                       插入 4 个空格
    Ctrl+S                    保存
    Ctrl+Z                    撤销（单步）
    Ctrl+C                    保存并退出
    Esc                       退出（有未保存改动时会问）
    F5                        保存并在终端里跑一次，直接看结果

## 这一版**不是**什么

不是 IDE：没有语法高亮、没有自动补全、没有多文件、没有鼠标。
它解决的是"**给我一个能写字的地方**"，不是"给我一个好用的编辑器"。
⚠ 想要好用的编辑器就直说 —— 学员的答题文件是**普通 .py 文件**，
  他完全可以用 VS Code 打开同一个文件，两边不冲突。
"""
from __future__ import annotations

import os
import sys

# ── 终端控制（Windows 要额外开 VT 处理）──────────────────────────────────

def 开启ANSI() -> bool:
    """让 Windows 控制台解释 ANSI 转义。返回是否成功（失败也不抛 —— 降级）。"""
    if os.name != "nt":
        return True
    try:
        import ctypes
        k = ctypes.windll.kernel32
        h = k.GetStdHandle(-11)                 # STD_OUTPUT_HANDLE
        mode = ctypes.c_uint32()
        if not k.GetConsoleMode(h, ctypes.byref(mode)):
            return False
        # 0x0004 = ENABLE_VIRTUAL_TERMINAL_PROCESSING
        return bool(k.SetConsoleMode(h, mode.value | 0x0004))
    except Exception:
        return False


def _终端宽高() -> tuple[int, int]:
    try:
        s = os.get_terminal_size()
        return max(20, s.columns), max(6, s.lines)
    except OSError:
        return 80, 24


# ── 读键 ─────────────────────────────────────────────────────────────────

#: 特殊键的返回名
上, 下, 左, 右 = "UP", "DOWN", "LEFT", "RIGHT"


def 读键() -> str:
    """读一个按键。普通键返回字符本身，特殊键返回名字（见上面的常量）。

    ⚠ Windows 用 `msvcrt`：方向键是**两个**字节（`0xE0`/`0x00` + 扫描码），
      必须连着读第二个字节，否则方向键会被当成两个怪字符。
    """
    if os.name == "nt":
        import msvcrt
        c = msvcrt.getwch()
        if c in ("\x00", "\xe0"):
            c2 = msvcrt.getwch()
            return {
                "H": 上, "P": 下, "K": 左, "M": 右,
                "G": "HOME", "O": "END", "I": "PGUP", "Q": "PGDN",
                "S": "DELETE", "c": "F5",
            }.get(c2, "")
        if c == "\x1b":
            return "ESC"
        if c == "\r":
            return "\r"
        if c == "\x08":
            return "BACKSPACE"
        if c in ("\x13", "\x1a", "\x03", "\x16"):
            return {"\x13": "CTRL_S", "\x1a": "CTRL_Z",
                    "\x03": "CTRL_C", "\x16": "PASTE"}[c]
        return c
    # 类 Unix：读字节序列
    import termios
    import tty
    fd = sys.stdin.fileno()
    old = termios.tcgetattr(fd)
    try:
        tty.setraw(fd)
        c = sys.stdin.read(1)
        if c == "\x1b":
            nxt = sys.stdin.read(1)
            if nxt == "[":
                third = sys.stdin.read(1)
                return {"A": 上, "B": 下, "C": 左, "D": 右,
                        "H": "HOME", "F": "END", "5": "PGUP", "6": "PGDN",
                        "3": "DELETE"}.get(third, "")
            return "ESC"
        if c == "\r":
            return "\r"
        if c == "\x7f":
            return "BACKSPACE"
        if c == "\x13":
            return "CTRL_S"
        if c == "\x1a":
            return "CTRL_Z"
        if c == "\x03":
            return "CTRL_C"
        return c
    finally:
        termios.tcsetattr(fd, termios.TCSADRAIN, old)


# ── 编辑器 ───────────────────────────────────────────────────────────────

缩进 = "    "      # 4 个空格（不用制表符：混用会让学员困惑）


class 编辑器:
    """一个极简但**能用**的全屏文本编辑器。"""

    def __init__(self, 路径: str):
        self.路径 = 路径
        self.行: list[str] = []
        self.x = self.y = 0          # 光标（列/行）
        self.顶 = 0                  # 顶部滚动行
        self.脏 = False
        self.消息 = ""
        self.退出 = False
        self.保存并跑 = False
        self._撤销: tuple[list[str], int, int] | None = None
        # Esc 两步确认（有未保存改动时）/ 见 `处理()` 里的说明
        self._esc待确认 = False
        self._撤销前: list[str] | None = None
        self.读入()

    # ── 文件 ──
    def 读入(self) -> None:
        if os.path.isfile(self.路径):
            with open(self.路径, encoding="utf-8") as f:
                self.行 = f.read().split("\n")
        else:
            self.行 = [""]
        while self.行 and self.行[-1] == "" and len(self.行) > 1:
            self.行.pop()            # 去掉末尾多余空行（保存时再加回来）

    def 保存(self) -> bool:
        try:
            os.makedirs(os.path.dirname(os.path.abspath(self.路径)), exist_ok=True)
            with open(self.路径, "w", encoding="utf-8", newline="\n") as f:
                f.write("\n".join(self.行) + "\n")
            self.脏 = False
            self.消息 = "已保存"
            return True
        except OSError as e:
            self.消息 = "保存失败：" + e.strerror
            return False

    # ── 编辑动作 ──
    def _记撤销(self) -> None:
        self._撤销 = (list(self.行), self.x, self.y)
        self._撤销前 = None

    def 撤销(self) -> None:
        if self._撤销 is None:
            self.消息 = "没有可撤销的"
            return
        行, x, y = self._撤销
        self.行, self.x, self.y = 行, x, y
        self._撤销 = None
        self.脏 = True
        self.消息 = "已撤销"

    def 插入(self, s: str) -> None:
        self._记撤销()
        行 = self.行[self.y]
        self.行[self.y] = 行[: self.x] + s + 行[self.x :]
        self.x += len(s)
        self.脏 = True
        self.消息 = ""

    def 换行(self) -> None:
        self._记撤销()
        行 = self.行[self.y]
        左, 右 = 行[: self.x], 行[self.x :]
        # ⚠ 自动保持缩进 —— 不保持的话，学员写函数体会被反复打回行首，
        #   而"为什么我的代码总是缩进不对"是新手最常见的挫败之一。
        空 = len(左) - len(左.lstrip(" "))
        self.行[self.y] = 左
        # ⚠ 判据发现的 bug：原来写的是
        #     `" " * 空 + 右.lstrip(" ") if 右.strip() else ""`
        #   而 `右.strip()` 在"右半边为空"（也就是在行尾按回车）时会把整行变成 ""，
        #   **连缩进都没了** —— 学员在函数体里按回车会被打回行首。
        #   正确语义：**只要左边有缩进就沿用**（右半边空也要留缩进，
        #   因为下一行本来就在同一个块里）。
        self.行.insert(self.y + 1, " " * 空 + 右.lstrip(" "))
        self.y += 1
        self.x = 空
        self.脏 = True
        self.消息 = ""

    def 退格(self) -> None:
        if self.x > 0:
            self._记撤销()
            行 = self.行[self.y]
            self.行[self.y] = 行[: self.x - 1] + 行[self.x :]
            self.x -= 1
            self.脏 = True
        elif self.y > 0:
            self._记撤销()
            上一行 = self.行[self.y - 1]
            self.x = len(上一行)
            self.行[self.y - 1] = 上一行 + self.行[self.y]
            del self.行[self.y]
            self.y -= 1
            self.脏 = True
        self.消息 = ""

    def 删除(self) -> None:
        if self.x < len(self.行[self.y]):
            self._记撤销()
            行 = self.行[self.y]
            self.行[self.y] = 行[: self.x] + 行[self.x + 1 :]
            self.脏 = True
        elif self.y < len(self.行) - 1:
            self._记撤销()
            self.行[self.y] += self.行[self.y + 1]
            del self.行[self.y + 1]
            self.脏 = True
        self.消息 = ""

    # ── 画 ──
    def _画(self, 宽: int, 高: int) -> None:
        头 = 1
        底 = 2
        可见 = 高 - 头 - 底
        # 光标跟着窗口滚
        if self.y < self.顶:
            self.顶 = self.y
        elif self.y >= self.顶 + 可见:
            self.顶 = self.y - 可见 + 1
        self.顶 = max(0, min(self.顶, max(0, len(self.行) - 1)))

        out = ["\x1b[H"]                      # 回家（不用清屏 ⇒ 不闪）
        名 = os.path.basename(self.路径)
        标 = f" 编辑 {名} " + ("● 未保存" if self.脏 else "已保存")
        out.append("\x1b[7m" + 标[:宽].ljust(宽) + "\x1b[0m")
        行号宽 = len(str(len(self.行)))
        for i in range(可见):
            序号 = self.顶 + i
            if 序号 < len(self.行):
                号 = str(序号 + 1).rjust(行号宽)
                内容 = self.行[序号][: max(0, 宽 - 行号宽 - 2)]
                out.append(f"\x1b[90m{号} \x1b[0m{内容}".ljust(宽 + 12))
            else:
                out.append(" " * 4)
            out.append("\x1b[K")
            out.append("\r\n")
        # 底栏
        提示 = self.消息 or "Ctrl+S 保存 · Ctrl+C 保存退出 · Esc 退出 · F5 保存并运行 · Ctrl+Z 撤销"
        out.append("\x1b[7m" + (" " + 提示)[:宽].ljust(宽) + "\x1b[0m")
        # 光标定位（屏幕行 = y - 顶 + 头 + 1）
        out.append(f"\x1b[{self.y - self.顶 + 头 + 1};{self.x + 行号宽 + 2}H")
        sys.stdout.write("".join(out))
        sys.stdout.flush()

    # ── 主循环 ──
    def 跑(self) -> int:
        """进编辑器。返回 0 = 正常退出。"""
        if not 开启ANSI():
            # 降级：明确告诉学员怎么办，而不是打一屏乱码
            print("\n  ⚠ 这个终端不支持 ANSI 转义，内置编辑器用不了。")
            print(f"  请用别的编辑器打开这个文件：{self.路径}\n")
            return 1
        sys.stdout.write("\x1b[?25h")          # 显示光标
        try:
            while not self.退出:
                宽, 高 = _终端宽高()
                self._画(宽, 高)
                self.处理(读键(), 高)
        finally:
            sys.stdout.write("\x1b[0m\x1b[2J\x1b[H")
            sys.stdout.flush()
        if self.保存并跑:
            return self._跑一次()
        return 0

    def _夹列(self) -> None:
        """把光标列夹到当前行长度以内。

        ⚠ 判据发现的 bug：上下移动**没有夹取** —— 在长行第 6 列按"下"到只有
          1 个字符的短行，x 仍是 6，再打字就写到越界位置（或直接 IndexError）。
        """
        self.x = max(0, min(self.x, len(self.行[self.y])))

    def 处理(self, k: str, 高: int) -> None:
        # 按了别的键 ⇒ 之前那次 Esc 提醒作废（重新计数）
        if k != "ESC":
            self._esc待确认 = False
        if k == 上:
            if self.y > 0:
                self.y -= 1
                self._夹列()
        elif k == 下:
            if self.y < len(self.行) - 1:
                self.y += 1
                self._夹列()
        elif k == 左:
            if self.x > 0:
                self.x -= 1
            elif self.y > 0:
                self.y -= 1
                self.x = len(self.行[self.y])
        elif k == 右:
            if self.x < len(self.行[self.y]):
                self.x += 1
            elif self.y < len(self.行) - 1:
                self.y += 1
                self.x = 0
        elif k == "HOME":
            self.x = 0
        elif k == "END":
            self.x = len(self.行[self.y])
        elif k == "PGUP":
            self.y = max(0, self.y - (高 - 6))
            self._夹列()
        elif k == "PGDN":
            self.y = min(len(self.行) - 1, self.y + (高 - 6))
            self._夹列()
        elif k == "BACKSPACE":
            self.退格()
        elif k == "DELETE":
            self.删除()
        elif k == "\r":
            self.换行()
        elif k == "\t":
            self.插入(缩进)
        elif k == "CTRL_S":
            self.保存()
        elif k == "CTRL_Z":
            self.撤销()
        elif k == "CTRL_C":
            self.保存()
            self.退出 = True
        elif k == "F5":
            self.保存()
            self.退出 = True
            self.保存并跑 = True
        elif k == "ESC":
            # ⚠⚠ 有未保存改动时**必须两步确认**：直接退出会让学员丢掉刚写的代码，
            #    而那是他在这个工具里最气的时刻。
            #    ⚠ 我第一版写成了"把 `脏` 清掉再显消息" —— 那等于**假装已保存**：
            #      第二次 Esc 走"没有未保存改动"的分支退出，代码真的没了。
            #    ⇒ 用一个**独立**的确认标志，绝不碰 `脏`。
            if self.脏 and not self._esc待确认:
                self._esc待确认 = True
                self.消息 = "⚠ 有未保存的改动！再按一次 Esc 放弃，或 Ctrl+S 保存"
            else:
                self.退出 = True
        elif len(k) == 1 and k >= " ":
            self.插入(k)

    def _跑一次(self) -> int:
        import subprocess
        print(f"\n  ── 运行 {os.path.basename(self.路径)} ──\n")
        try:
            r = subprocess.run([sys.executable, self.路径], timeout=120)
            print(f"\n  ── 退出码 {r.returncode} ──")
            return r.returncode
        except subprocess.TimeoutExpired:
            print("\n  ⚠ 运行超过 120 秒 —— 是不是有死循环？")
            return 1


def 编辑(路径: str) -> int:
    """对外入口：编辑一个文件。"""
    if not sys.stdin.isatty() or not sys.stdout.isatty():
        # 不是交互终端（管道/重定向/测试）⇒ 不能进全屏编辑
        print(f"\n  当前不是交互终端，不能打开内置编辑器。")
        print(f"  请直接用别的编辑器打开：{路径}\n")
        return 1
    return 编辑器(路径).跑()
