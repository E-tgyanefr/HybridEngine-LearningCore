"""raspi —— 树莓派模拟器（不需要真板子也能学）。

════ 树莓派和单片机有什么不一样 ════

看起来都是"用程序控制引脚"，但**写代码的方式完全不同**：

    单片机（Arduino 那类）
        写一个 loop()，程序**永远在转**，靠 delay 控制节奏
        没有操作系统，你的代码就是整个程序

    树莓派
        跑着完整的 Linux，**你的程序是一个普通进程**
        有开始、有结束 —— 想一直监测就写 while True 或定时任务
        可以用文件、网络、数据库，因为它就是一台小电脑

这个差别是这一课线的重点：**学生要从"嵌入式思维"转到"小电脑思维"**。

════ 模拟的是什么 ════

  · GPIO 引脚（输入/输出、上下拉）
  · LED / 按钮（和 mcu 模块同一套语义）
  · 几种常见的传感器（温度、光线、距离）
  · 一个"文件系统"的极简替身（记录写入的数据）

════ ⚠ 和 mcu 一样，保证不了电路 ════

限流电阻、正负极、接线位置 —— 模拟器一个都发现不了。
每个检查结果后面都会提醒这一点。
"""

from __future__ import annotations

import random

__all__ = ["树莓派", "引脚错误", "GPIO"]


class 引脚错误(Exception):
    """用了不存在的 GPIO 引脚号。

    ⚠ 真实树莓派上这**不会报错** —— 那个引脚只是什么都不做。
    所以模拟器特意报错，帮学生养成"先确认引脚号"的习惯。
    """


class 传感器:
    """一个挂在树莓派上的传感器。

    真实传感器要通过 I2C / SPI / 单总线读，每家库的用法都不一样。
    模拟器把它统一成一个 读() 方法 ——
    这样学生先学会"读数 → 判断 → 动作"这个**结构**，
    真拿到具体型号时只需要换掉读取那一行。
    """

    def __init__(self, 名字: str, 单位: str, 范围: tuple[float, float], 初始: float):
        self.名字 = 名字
        self.单位 = 单位
        self.范围 = 范围
        self._值 = 初始

    def 读(self) -> float:
        return self._值

    def 设为(self, 值: float):
        """模拟"环境变了"（例如用手捂住光敏电阻、往传感器上哈气）。

        为什么需要它：不设的话，传感器永远返回同一个数，
        学生写的"根据读数做判断"就无法验证。
        """
        低, 高 = self.范围
        if not (低 <= 值 <= 高):
            raise ValueError(f"{self.名字}的读数应该在这之间：{低} ~ {高}（{self.单位}）")
        self._值 = 值
        return self

    def __repr__(self):
        return f"<{self.名字} {self._值}{self.单位}>"


class GPIO:
    """GPIO 引脚的集合。用法和真实的 RPi.GPIO 库刻意保持相似。"""

    输入 = "输入"
    输出 = "输出"
    高 = True
    低 = False

    def __init__(self, 板子: "树莓派"):
        self._板 = 板子

    def 设模式(self, 号: int, 模式: str):
        self._板._取引脚(号).模式 = 模式
        self._板.日志.append(f"[{self._板.时间:6.2f}s] GPIO {号} 设为{模式}")
        return self

    def 写(self, 号: int, 值):
        p = self._板._取引脚(号)
        if p.模式 != "输出":
            raise 引脚错误(
                f"GPIO {号} 现在是**{p.模式}**模式，不能往里写。\n"
                f"      改法：先 设模式({号}, GPIO.输出)。\n"
                f"      ⚠ 真实树莓派上这个错误**不会有提示** —— 引脚只是没反应。")
        p.高 = bool(值)
        self._板.日志.append(f"[{self._板.时间:6.2f}s] GPIO {号} ← {'高' if p.高 else '低'}")
        return self

    def 读(self, 号: int) -> bool:
        p = self._板._取引脚(号)
        if p.模式 != "输入":
            raise 引脚错误(
                f"GPIO {号} 现在是**{p.模式}**模式，读它没有意义。\n"
                f"      改法：先 设模式({号}, GPIO.输入)。")
        return p.高

    def 清理(self):
        """复位所有引脚。真实程序结束时应该调用它，否则引脚会保持之前的状态。"""
        for p in self._板._引脚:
            p.高 = False
        self._板.日志.append(f"[{self._板.时间:6.2f}s] 所有引脚已复位")
        return self


class _引脚:
    __slots__ = ("号", "模式", "高", "上下拉")

    def __init__(self, 号: int):
        self.号 = 号
        self.模式 = "未设置"
        self.高 = False
        self.上下拉 = None


class 树莓派:
    """一块虚拟的树莓派。

    用法（和真实的 RPi.GPIO 很像）::

        from hybridengine_light.raspi import 树莓派, GPIO

        派 = 树莓派()
        派.GPIO.设模式(17, GPIO.输出)
        派.GPIO.写(17, GPIO.高)          # 点亮 LED 17
        派.加传感器("温度", "°C", (0, 50), 25.0)
        派.温度.读()                     # 25.0
        派.报告()
    """

    def __init__(self, 名字: str = "模拟树莓派"):
        self.名字 = 名字
        self.时间 = 0.0
        self.日志: list[str] = []
        self.警告: list[str] = []
        # 树莓派的 GPIO 号不是连续的 1~40，用的是 BCM 编号
        self._引脚 = [_引脚(n) for n in
                      (2, 3, 4, 17, 18, 22, 23, 24, 25, 27, 5, 6, 13, 19, 26, 12, 16, 20, 21)]
        self.GPIO = GPIO(self)
        self._传感器: dict[str, 传感器] = {}
        self._文件: dict[str, str] = {}

    # ══════════════════ 引脚 ══════════════════

    def _取引脚(self, 号: int) -> _引脚:
        if not isinstance(号, int):
            raise 引脚错误(f"GPIO 号应该是整数，你给的是 {type(号).__name__}")
        for p in self._引脚:
            if p.号 == 号:
                return p
        raise 引脚错误(
            f"GPIO {号} 不是这块板子上可用的引脚。\n"
            f"      可用的：{self.可用引脚()}\n"
            f"      ⚠ 真实树莓派上写错引脚号**不会报错**，那个引脚只是毫无反应 ——\n"
            f"        所以先查清引脚号再写代码，能省掉很多时间。")

    def 可用引脚(self) -> str:
        return "、".join(str(p.号) for p in self._引脚)

    # ══════════════════ 按钮 ══════════════════

    def 按下(self, 号: int):
        p = self._取引脚(号)
        if p.模式 != "输入":
            raise 引脚错误(f"GPIO {号} 不是输入模式，不能当按钮按。先 设模式({号}, GPIO.输入)")
        p.高 = True
        self.日志.append(f"[{self.时间:6.2f}s] 按下 GPIO {号}")
        return self

    def 松开(self, 号: int):
        p = self._取引脚(号)
        p.高 = False
        self.日志.append(f"[{self.时间:6.2f}s] 松开 GPIO {号}")
        return self

    # ══════════════════ 传感器 ══════════════════

    def 加传感器(self, 名字: str, 单位: str, 范围: tuple[float, float], 初始: float) -> 传感器:
        s = 传感器(名字, 单位, 范围, 初始)
        self._传感器[名字] = s
        # 允许 派.温度 这样直接访问 —— 比 派.传感器["温度"] 顺手
        setattr(self, 名字, s)
        return s

    @property
    def 传感器们(self) -> dict[str, 传感器]:
        return dict(self._传感器)

    # ══════════════════ 时间 ══════════════════

    def 时间推进(self, 秒: float):
        if 秒 < 0:
            raise ValueError("时间不能倒流")
        self.时间 += 秒
        return self

    def 睡(self, 秒: float):
        """模拟 time.sleep()。树莓派上很常用（轮询、防抖、控制节奏）。"""
        return self.时间推进(秒)

    # ══════════════════ 极简"文件系统" ══════════════════

    def 写文件(self, 路径: str, 内容: str):
        """记录数据到"文件"。

        为什么要有这个：树莓派最典型的用法之一是**记录传感器数据**。
        模拟一个极简的文件系统，就能验证"有没有真的把数据存下来" ——
        而那正是这类项目的核心动作。
        """
        self._文件[路径] = self._文件.get(路径, "") + 内容
        self.日志.append(f"[{self.时间:6.2f}s] 写入 {路径}（{len(内容)} 字符）")
        return self

    def 读文件(self, 路径: str) -> str:
        return self._文件.get(路径, "")

    def 文件列表(self) -> list[str]:
        return sorted(self._文件)

    # ══════════════════ 报告 ══════════════════

    def 报告(self) -> str:
        行 = [f"── {self.名字}（时间 {self.时间:.2f}s）──"]
        用过的 = [p for p in self._引脚 if p.模式 != "未设置"]
        if 用过的:
            for p in 用过的:
                if p.模式 == "输出":
                    行.append(f"  GPIO {p.号:<3} 输出 → {'高（亮/开）' if p.高 else '低（灭/关）'}")
                else:
                    行.append(f"  GPIO {p.号:<3} 输入 → 读到 {'高' if p.高 else '低'}")
        else:
            行.append("  （还没有配置过任何引脚）")

        if self._传感器:
            行.append("  传感器：")
            for s in self._传感器.values():
                行.append(f"    {s.名字} = {s._值}{s.单位}")

        if self._文件:
            行.append("  文件：")
            for k in self.文件列表():
                行.append(f"    {k}（{len(self._文件[k])} 字符）")

        if self.警告:
            行.append("")
            行.append("  ⚠ 这些地方逻辑上没问题，但真实硬件上要留意：")
            for w in self.警告:
                行.append(f"    · {w}")
        return "\n".join(行)

    def 日志文本(self) -> str:
        return "\n".join(self.日志) if self.日志 else "（没有记录到任何操作）"

    # ══════════════════ 常用组合 ══════════════════

    def 接LED(self, 号: int, 名字: str = "LED"):
        """把一个引脚配成输出并当 LED 用。返回一个能读状态的小对象。"""
        self.GPIO.设模式(号, GPIO.输出)
        return _LED(self, 号, 名字)


class _LED:
    def __init__(self, 派: 树莓派, 号: int, 名字: str):
        self._派 = 派
        self.号 = 号
        self.名字 = 名字

    def 亮(self):
        self._派.GPIO.写(self.号, GPIO.高)
        return self

    def 灭(self):
        self._派.GPIO.写(self.号, GPIO.低)
        return self

    def 翻转(self):
        if self.状态() == "亮":
            return self.灭()
        return self.亮()

    def 状态(self) -> str:
        return "亮" if self._派._取引脚(self.号).高 else "灭"

    def __repr__(self):
        return f"<LED GPIO{self.号} {self.状态()}>"
