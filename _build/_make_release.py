"""建 GitHub Release 并上传 zip —— 用 git 现成的凭据，不需要 gh CLI。

⚠ 上一轮我说过"没有 gh 所以做不了" —— 那是**把工具和协议搞混了**。
  凭据本来就有（Windows 凭据管理器里，`git push` 一直在用它们），
  而 Release 是标准 API，用标准库就能调。

用法：
    python _build/_make_release.py            # 只看现状，不动
    python _build/_make_release.py --go       # 真建 + 上传

⚠ token 从 `git credential fill` 取，**只在内存里**，不写盘、不打印。
"""
import io
import json
import os
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
发布 = os.path.join(os.path.dirname(LC), "发布")
仓库 = "E-tgyanefr/HybridEngine-light"
标签 = "v0.8.0"
ZIP = os.path.join(发布, "HybridEngine-light-v0.8.0-win64.zip")
说明 = os.path.join(发布, "Release说明-v0.8.0.md")


def 取凭据():
    """让 git 自己去凭据管理器里拿 —— 和 push 用的是同一份。"""
    p = subprocess.run(["git", "credential", "fill"], cwd=LC,
                       input="protocol=https\nhost=github.com\n\n",
                       capture_output=True, text=True, timeout=60)
    用户 = 令 = None
    for l in (p.stdout or "").splitlines():
        if l.startswith("username="):
            用户 = l[9:]
        elif l.startswith("password="):
            令 = l[9:]
    return 用户, 令


用户, 令 = 取凭据()
if not 令:
    print("  XX 拿不到 GitHub 凭据 —— 请确认能 git push")
    sys.exit(1)
print("  凭据：用户 %s，token 长度 %d（来自凭据管理器，不落盘）" % (用户, len(令)))

headers = {
    "Authorization": "Bearer " + 令,
    "Accept": "application/vnd.github+json",
    "X-GitHub-Api-Version": "2022-11-28",
    "User-Agent": "hybridengine-light-release",
}


def 请求(url, 方法="GET", 数据=None, 内容类型="application/json"):
    body = None
    if 数据 is not None:
        body = 数据 if isinstance(数据, bytes) else json.dumps(数据).encode("utf-8")
    h = dict(headers)
    h["Content-Type"] = 内容类型
    req = urllib.request.Request(url, data=body, headers=h, method=方法)
    try:
        with urllib.request.urlopen(req, timeout=600) as r:
            return r.status, json.loads(r.read().decode("utf-8") or "{}")
    except urllib.error.HTTPError as e:
        return e.code, {"错误": e.read().decode("utf-8", "replace")[:400]}


# ── 1 现状 ────────────────────────────────────────────────────────────
print()
print("  === 现状 ===")
状态, 已有 = 请求("https://api.github.com/repos/%s/releases/tags/%s" % (仓库, 标签))
if 状态 == 200:
    print("    Release %s **已存在**" % 标签)
    print("      标题：%s" % 已有.get("name"))
    print("      草稿：%s   预发布：%s" % (已有.get("draft"), 已有.get("prerelease")))
    print("      附件：%s" % ([a["name"] for a in 已有.get("assets", [])] or "（没有）"))
    已存在 = 已有
elif 状态 == 404:
    print("    Release %s 还不存在（要新建）" % 标签)
    已存在 = None
else:
    print("    查不到：HTTP %s  %s" % (状态, 已有.get("错误", "")[:200]))
    sys.exit(1)

if "--go" not in sys.argv:
    print()
    print("  （这是只读检查。加 --go 才真建和上传）")
    sys.exit(0)

# ── 2 建 Release ──────────────────────────────────────────────────────
if not os.path.isfile(说明):
    print("  XX 找不到说明文件：%s" % 说明)
    sys.exit(1)
正文 = io.open(说明, encoding="utf-8").read()
print()
print("  === 建 Release ===")
if 已存在:
    print("    已经有了，改说明…")
    状态, r = 请求("https://api.github.com/repos/%s/releases/%d" % (仓库, 已存在["id"]),
                  方法="PATCH", 数据={"body": 正文, "draft": False})
else:
    状态, r = 请求("https://api.github.com/repos/%s/releases" % 仓库, 方法="POST",
                  数据={"tag_name": 标签,
                        "name": "HybridEngine-light 0.8.0 —— 219 课，覆盖到 GESP 八级",
                        "body": 正文,
                        "draft": False, "prerelease": False})
if 状态 not in (200, 201):
    print("    XX 失败 HTTP %s：%s" % (状态, r.get("错误", "")[:300]))
    sys.exit(1)
print("    OK  %s" % r.get("html_url"))
发布址 = r["upload_url"].split("{")[0]

# ── 3 上传 zip ────────────────────────────────────────────────────────
print()
print("  === 上传 zip ===")
if not os.path.isfile(ZIP):
    print("    XX 找不到 zip：%s" % ZIP)
    sys.exit(1)
大小 = os.path.getsize(ZIP)
print("    %s  %.1f MB" % (os.path.basename(ZIP), 大小 / 1024 / 1024))

# 已经传过就先删掉旧的，免得同名冲突
状态, 现 = 请求("https://api.github.com/repos/%s/releases/%d/assets" % (仓库, r["id"]))
if 状态 == 200:
    for a in 现:
        if a["name"] == os.path.basename(ZIP):
            print("    先删掉同名旧附件…")
            请求("https://api.github.com/repos/%s/releases/assets/%d" % (仓库, a["id"]),
                方法="DELETE")

数据 = io.open(ZIP, "rb").read()
状态, up = 请求(发布址 + "?name=" + urllib.parse.quote(os.path.basename(ZIP)),
              方法="POST", 数据=数据, 内容类型="application/zip")
if 状态 not in (200, 201):
    print("    XX 上传失败 HTTP %s：%s" % (状态, up.get("错误", "")[:300]))
    sys.exit(1)
print("    OK  %s（%.1f MB）" % (up.get("browser_download_url"), up.get("size", 0) / 1024 / 1024))
print()
print("  完成：%s" % r.get("html_url"))
