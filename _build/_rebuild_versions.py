# 为 tag 重命名做准备：对每个提交**重建**对应版本的 zip。
#
# ## 为什么重建而不是下载 GitHub 上的资产
#   实测：从 GitHub 下载资产报 **HTTP 502**（这个网络对 github.com 很不稳）。
#   但那些资产本来就是我**从这些提交构建出来的** ——
#   源在 git 里 ⇒ 产物可确定性重建，**不依赖网络**。
#
# ## ⚠ 用 `git worktree` 而不是 `git checkout`
#   实测踩过的坑：`git checkout -- <目录>` 会**静默回滚该目录下新增的文件**
#   而 `git status` 看着还是干净的。
#   worktree 在**另一个目录**检出，完全不动当前工作区 —— 没有这个风险。
import io
import os
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # 仓库根（从 __file__ 推，别再写死）
OUT = os.path.join(os.environ["TEMP"], "hel_ver_assets")

计划 = [
    ("v0.1.0", "d0667d3", "改名完成（无编辑器）"),
    ("v0.1.1", "8f8c1a7", "+ 内置编辑器 + C# 回归修复"),
    ("v0.1.2", "3dd5c22", "+ C 课程 8 课"),
    ("v0.1.3", "499ae1a", "+ 命令名修复"),
]

os.makedirs(OUT, exist_ok=True)


def run(args, cwd=LC, timeout=1800):
    return subprocess.run(args, cwd=cwd, capture_output=True, timeout=timeout)


for tag, sha, 说明 in 计划:
    print(f"\n=== {tag}  ({sha})  {说明} ===")
    wt = os.path.join(OUT, "_wt_" + tag)
    if os.path.exists(wt):
        run(["git", "worktree", "remove", "--force", wt])
        shutil.rmtree(wt, ignore_errors=True)

    r = run(["git", "worktree", "add", "--detach", wt, sha])
    if r.returncode != 0:
        print("  ✗ worktree 失败: " + (r.stdout + r.stderr).decode("utf-8", "replace")[-300:])
        continue
    print(f"  worktree 就绪: {wt}")

    # 用该提交自带的构建脚本（它已经把 ROOT 推导、发布目录等坑修好了）
    r = run([sys.executable, os.path.join(wt, "_build", "build_exe.py")], cwd=wt, timeout=1800)
    out = (r.stdout + r.stderr).decode("utf-8", "replace")
    if r.returncode != 0:
        print("  ✗ 构建失败（最后 12 行）：")
        for line in out.splitlines()[-12:]:
            print("     " + line)
        run(["git", "worktree", "remove", "--force", wt])
        continue

    # 构建脚本会写到 <wt>/../发布（= OUT\发布），搬出来改名
    产 = os.path.join(OUT, "发布", "HybridEngine-light-win64.zip")
    if not os.path.isfile(产):
        print(f"  ✗ 没找到产物 {产}")
        print("     构建输出尾部：" + out.strip().splitlines()[-1] if out.strip() else "")
    else:
        目标 = os.path.join(OUT, f"HybridEngine-light-{tag}-win64.zip")
        shutil.move(产, 目标)
        print(f"  ✓ {os.path.basename(目标)}  {os.path.getsize(目标)/1024/1024:.2f}MB")
    # 清掉该版本的成品目录（只留 zip）
    shutil.rmtree(os.path.join(OUT, "发布"), ignore_errors=True)
    run(["git", "worktree", "remove", "--force", wt])
    shutil.rmtree(wt, ignore_errors=True)

print("\n=== 结果 ===")
for f in sorted(os.listdir(OUT)):
    if f.endswith(".zip"):
        p = os.path.join(OUT, f)
        print(f"  {f}  {os.path.getsize(p)/1024/1024:.2f}MB")
