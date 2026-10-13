"""通过 CDP 在真机上执行 JS —— 真机验证的"手术刀"。

⚠⚠ **为什么需要这个工具**（这是我这一轮最重要的教训）：

  在此之前，我在真机上验证交互只有一条路：
      `adb shell input tap X Y` + 截图 + 用眼睛看

  而那条路要求我**把坐标算准**，于是：
    · 截图的预览尺寸是 838x1862，屏幕是 1080x2400，得乘 1.289
    · 页面滚动之后元素的 y 就变了，我得重新截图、重新量
    · 点了没反应时，分不清是"没点中"还是"点中了但逻辑没跑"

  ⇒ 我为"点不中按钮"这件事**反复烧了好几轮**，
    其中一次甚至误判成"功能坏了"，实际只是我坐标算错。

  有了 CDP，验证变成：
      evaluate("document.querySelector('[data-做=分享]').click()")
      evaluate("JSON.stringify(页面状态)")
   —— **读返回值**，不碰坐标，没有歧义。

  ⚠ 我一开始试过开 CDP 但没成（socket 没出现），就放弃了。
    那个判断是错的：真正的原因是 .NET Android **没有**那个 MSBuild 属性
    （全盘搜过 targets/props，`WebViewDebugging` 一次都没出现），
    必须显式调 `WebView.SetWebContentsDebuggingEnabled(true)`。
    补上那一行之后 socket 立刻出现。

用法（需要先 `adb forward`，见 build_android.py 或下面的 连上()）：
    python _build/_cdp.py "document.title"
    python _build/_cdp.py --点 "[data-做=分享]"
    python _build/_cdp.py --看            # 打印页面状态

⚠ 这只是一层 WebSocket 客户端，**只用标准库**（socket + base64 + struct），
  不引第三方依赖 —— 项目铁律。
"""
import base64
import json
import os
import socket
import struct
import subprocess
import sys
import time
import urllib.request

ADB = os.path.join(os.environ.get("LOCALAPPDATA", ""),
                   "Android", "Sdk", "platform-tools", "adb.exe")
端口 = 9222
包名 = "com.hybridengine.light"


def 跑(命令):
    r = subprocess.run(命令, capture_output=True, text=True,
                       encoding="utf-8", errors="replace")
    return (r.stdout or "").strip()


def 连上():
    """把设备的 WebView 调试端口转发到本机，返回 websocket 地址。"""
    有 = 跑([ADB, "shell", "pidof", 包名])
    if not 有:
        print("  XX 应用没在跑（先启动它）")
        return None
    pid = 有.split()[0]
    跑([ADB, "forward", "--remove-all"])
    print("  forward:", 跑([ADB, "forward", "tcp:%d" % 端口,
                          "localabstract:webview_devtools_remote_" + pid]))
    for _ in range(20):
        try:
            with urllib.request.urlopen(
                    "http://127.0.0.1:%d/json/list" % 端口, timeout=4) as r:
                页 = json.loads(r.read().decode("utf-8"))
            for p in 页:
                if p.get("webSocketDebuggerUrl"):
                    return p["webSocketDebuggerUrl"]
        except Exception:
            pass
        time.sleep(0.5)
    print("  XX 连不上 CDP（socket 没出现？应用启动时有没有打「已打开」那行？）")
    return None


class 会话:
    """最小 WebSocket 客户端 —— 只够 CDP 用（文本帧、无分片、无扩展）。"""

    def __init__(self, url):
        #: ws://127.0.0.1:9222/devtools/page/XXXX
        左, 右 = url.split("://", 1)
        主机端口, 路径 = 右.split("/", 1)
        主机, 端口s = 主机端口.split(":")
        self.路径 = "/" + 路径
        self.s = socket.create_connection((主机, int(端口s)), timeout=20)
        self.号 = 0
        密 = base64.b64encode(os.urandom(16)).decode()
        握手 = ("GET %s HTTP/1.1\r\nHost: %s\r\nUpgrade: websocket\r\n"
                "Connection: Upgrade\r\nSec-WebSocket-Key: %s\r\n"
                "Sec-WebSocket-Version: 13\r\n\r\n" % (self.路径, 主机端口, 密))
        self.s.sendall(握手.encode())
        回 = b""
        while b"\r\n\r\n" not in 回:
            块 = self.s.recv(4096)
            if not 块:
                raise RuntimeError("握手被断开")
            回 += 块
        if b"101" not in 回.split(b"\r\n")[0]:
            raise RuntimeError("握手失败：" + 回.split(b"\r\n")[0].decode(errors="replace"))

    def 发(self, 字):
        数 = 字.encode("utf-8")
        头 = bytearray([0x81])                    #: FIN + 文本帧
        n = len(数)
        if n < 126:
            头.append(0x80 | n)
        elif n < 65536:
            头.append(0x80 | 126)
            头 += struct.pack(">H", n)
        else:
            头.append(0x80 | 127)
            头 += struct.pack(">Q", n)
        掩 = os.urandom(4)
        头 += 掩
        数 = bytes(b ^ 掩[i % 4] for i, b in enumerate(数))
        self.s.sendall(bytes(头) + 数)

    def 收(self):
        def 读(n):
            出 = b""
            while len(出) < n:
                块 = self.s.recv(n - len(出))
                if not 块:
                    raise RuntimeError("连接断了")
                出 += 块
            return 出
        _b1, b2 = 读(2)
        n = b2 & 0x7F
        if n == 126:
            n = struct.unpack(">H", 读(2))[0]
        elif n == 127:
            n = struct.unpack(">Q", 读(8))[0]
        数 = 读(n)
        return 数.decode("utf-8", errors="replace")

    def 求值(self, 表达式, 等=True):
        self.号 += 1
        号 = self.号
        self.发(json.dumps({
            "id": 号, "method": "Runtime.evaluate",
            "params": {"expression": 表达式, "returnByValue": True,
                       "awaitPromise": True},
        }))
        #: ⚠ 要**跳过事件通知**（`Runtime.consoleAPICalled` 之类），
        #:   只认 id 对得上的那条 —— 否则拿到的会是一条无关事件。
        截止 = time.time() + 30
        while time.time() < 截止:
            条 = json.loads(self.收())
            if 条.get("id") != 号:
                continue
            果 = 条.get("result", {})
            if "exceptionDetails" in 果:
                return "!! 异常：" + json.dumps(果["exceptionDetails"],
                                             ensure_ascii=False)[:400]
            r = 果.get("result", {})
            return r.get("value", r.get("description", "（无返回值）"))
        return "!! 超时"

    def 关(self):
        try:
            self.s.close()
        except Exception:
            pass


def 主():
    ws = 连上()
    if not ws:
        return 2
    会 = 会话(ws)
    try:
        参 = sys.argv[1:]
        if not 参 or 参[0] == "--看":
            #: 读页面自己的自检（那个函数就是为这个写的）
            print("  状态：" + str(会.求值("window.自检 ? window.自检() : '（没有自检函数）'")))
            print("  标题：" + str(会.求值("document.title")))
            return 0
        if 参[0] == "--点":
            #: ⚠ 用 `click()` 而不是模拟指针 —— 前者触发的是**同一个**事件
            #:   处理器，而且不用管坐标。（要测真实的触摸行为才需要 CDP 的
            #:   Input.dispatchTouchEvent，那是另一件事。）
            选 = 参[1]
            print("  点：%s" % 选)
            print("  结果：" + str(会.求值(
                "(function(){var e=document.querySelector(%s);"
                "if(!e)return '找不到元素';e.click();return '已点';})()"
                % json.dumps(选, ensure_ascii=False))))
            time.sleep(0.6)
            print("  点后：" + str(会.求值("window.自检()")))
            return 0
        print("  求值：" + str(会.求值(" ".join(参))))
        return 0
    finally:
        会.关()


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(主())
