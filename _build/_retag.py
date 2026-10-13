# 把 4 个 Release 的 tag 从 v1.0.x 改成 v0.1.x（与产品版本号一致）。
#
# ## 为什么必须"删了重建"
#   GitHub 的 API **不支持重命名 release 或 tag**。
#   删除 tag 会**连带删掉 release 与它的资产** ⇒ 重建后要重新上传。
#
# ## ⚠ 为什么敢删：资产在本地有确定性来源
#   那 4 个 zip 都是从 git 提交构建出来的 ⇒ 可用 `git worktree` 重建
#   （实测：4 个都重建成功，文件数 1055/1057/1068/1068，与内容演进吻合）。
#   不依赖从 GitHub 下载（实测那条路会 502）。
#
# ## 顺序
#   ① 建新 tag  → ② 建新 release → ③ 传资产 → ④ ④ 全成功后才删旧 release/tag
#   ⚠ 新东西全部建好之前**不删旧的** —— 中途失败也不会出现"什么都没有"。
import io
import json
import os
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
API = "https://api.github.com/repos/E-tgyanefr/HybridEngine-light"
# ⚠ 原来写 `os.environ["TEMP"]`（Windows 专有）⇒ Linux/macOS 上 KeyError。
#   `tempfile.gettempdir()` 跨平台，Windows 上取到的就是 %TEMP%。
import tempfile
ASSETS = os.path.join(tempfile.gettempdir(), "hel_ver_assets")

out = subprocess.run(["git", "credential", "fill"],
                     input=b"protocol=https\nhost=github.com\n\n",
                     capture_output=True).stdout.decode("utf-8", "replace")
tok = next((l[9:].strip() for l in out.splitlines() if l.startswith("password=")), "")
if not tok:
    print("  XX 拿不到 token"); sys.exit(1)
H = {"Authorization": f"token {tok}", "User-Agent": "dsh", "Accept": "application/vnd.github+json"}


def 请求(url, method="GET", data=None, timeout=120):
    req = urllib.request.Request(url, method=method, headers=H,
                                 data=json.dumps(data).encode("utf-8") if data is not None else None)
    return json.loads(urllib.request.urlopen(req, timeout=timeout).read())


# tag 名 -> (目标 commit, 标题)
#   ⚠ 用**本地 git 解析出的完整 SHA**，不靠 API 的 /git/ref/tags：
#     实测那条路会 404（tag 存在但 API 取 ref 失败），而且网络不稳。
#     本地能解析就说明提交真实存在 —— 更可靠。
计划 = {
    "v0.1.0": ("d0667d378ea745618584aada7d55ab06c8dd48f2", "HybridEngine-light v0.1.0（Windows 开箱即用）"),
    "v0.1.1": ("8f8c1a7659100ad320655d8f547a647ff54caf9f", "HybridEngine-light v0.1.1（含内置编辑器）"),
    "v0.1.2": ("3dd5c22d9442a84f1536745a053d04d722e83e1c", "HybridEngine-light v0.1.2（新增 C 语言课程）"),
    "v0.1.3": ("b6550235cb3b7d5078f2b28a3095d1030e4a673f", "HybridEngine-light v0.1.3（修：学员看到的命令名不对）"),
}

# 取旧 release 的说明文字（刚更新过，直接复用）
旧 = {r["tag_name"]: r for r in 请求(f"{API}/releases")}
print("  现有 release:", ", ".join(sorted(旧)))

# 旧 tag -> 新 tag 的映射，以及各自的 commit
旧名到新名 = {f"v1.0.{i}": f"v0.1.{i}" for i in range(4)}

建好 = []
for 旧名, 新名 in 旧名到新名.items():
    sha = 计划[新名][0]
    标题 = 计划[新名][1]
    zip路径 = os.path.join(ASSETS, f"HybridEngine-light-{新名}-win64.zip")
    print(f"\n── {旧名} → {新名}  ({sha[:10]}) ──")

    # ① 建 tag
    try:
        请求(f"{API}/git/refs", "POST", {"ref": f"refs/tags/{新名}", "sha": sha})
        print(f"   ① tag {新名} 已建")
    except urllib.error.HTTPError as e:
        m = e.read().decode("utf-8", "replace")
        if e.code == 422 and "already exists" in m:
            print(f"   ① tag {新名} 已存在（复用）")
        else:
            print(f"   ① XX 建 tag 失败 HTTP {e.code}: {m[:150]}"); continue

    # ② 建 release（说明文字沿用旧的，标题换成新的）
    说明 = 旧[旧名].get("body") or ""
    try:
        rel = 请求(f"{API}/releases", "POST", {
            "tag_name": 新名, "target_commitish": "main", "name": 标题,
            "body": 说明, "draft": False, "prerelease": False})
        print(f"   ② release 已建: {rel['html_url']}")
    except urllib.error.HTTPError as e:
        m = e.read().decode("utf-8", "replace")
        print(f"   ② XX 建 release 失败 HTTP {e.code}: {m[:180]}"); continue

    # ③ 传资产（带重试）
    if not os.path.isfile(zip路径):
        print(f"   ③ XX 本地没有 {os.path.basename(zip路径)}"); continue
    数据 = open(zip路径, "rb").read()
    up = rel["upload_url"].split("{")[0] + "?name=" + urllib.parse.quote(os.path.basename(zip路径))
    成功 = False
    for 次 in range(4):
        try:
            req = urllib.request.Request(up, method="POST",
                                         headers={**H, "Content-Type": "application/zip"}, data=数据)
            a = json.loads(urllib.request.urlopen(req, timeout=900).read())
            print(f"   ③ ✓ {a['name']}  {a['size']/1024/1024:.2f}MB  第 {次+1} 次")
            成功 = True
            break
        except urllib.error.HTTPError as e:
            print(f"   ③ 第 {次+1} 次失败 HTTP {e.code}: {e.read().decode('utf-8','replace')[:110]}")
        except Exception as e:
            print(f"   ③ 第 {次+1} 次失败: {type(e).__name__}: {str(e)[:110]}")
        time.sleep(6)
    if 成功:
        建好.append((旧名, 新名))

print(f"\n=== 新建成功 {len(建好)}/4 ===")
for 旧名, 新名 in 建好:
    print(f"  {旧名} -> {新名}")

# ④ 全部成功才删旧的
if len(建好) == 4:
    print("\n=== 四个都建好了 ⇒ 删旧 release 与旧 tag ===")
    for 旧名, _ in 建好:
        r = 旧[旧名]
        try:
            urllib.request.urlopen(urllib.request.Request(
                f"{API}/releases/{r['id']}", method="DELETE", headers=H), timeout=60).read()
            print(f"  删 release {旧名}")
        except urllib.error.HTTPError as e:
            print(f"  XX 删 release {旧名} 失败 HTTP {e.code}")
        try:
            urllib.request.urlopen(urllib.request.Request(
                f"{API}/git/refs/tags/{旧名}", method="DELETE", headers=H), timeout=60).read()
            print(f"  删 tag {旧名}")
        except urllib.error.HTTPError as e:
            print(f"  XX 删 tag {旧名} 失败 HTTP {e.code}")
else:
    print("\n⚠ 没有全部成功 ⇒ **不删旧的**（宁可留着不一致，也不要什么都没有）")
