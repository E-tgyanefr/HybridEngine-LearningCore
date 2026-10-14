# ⚠⚠⚠ 两个仓的推送/外泄状态 —— **判清了**（2026-10-14 · Round 28）

> 接 `_audit_79个提交没推上去.md`。
> `verify-and-optimize` 报了一条**更要紧**的事，但它说「仓库是 public 还是 private **我判不了**，
> 而这正是决定紧急程度的唯一变量」。
> ⚠ **那个变量可以自己判** —— 一条**不带任何凭据**的 API 请求就够。

---

## 一、⭐⭐ 仓库可见性（**不带凭据**判定）

```
GET https://api.github.com/repos/E-tgyanefr/HybridEngine-LearningCore   （**无 Authorization**）
  ⇒ **HTTP 200** ⇒ ⭐ **PUBLIC（公开）**
     private=False · visibility=public · pushed_at=2026-10-14T06:12:47Z · size=17850KB

GET https://api.github.com/repos/E-tgyanefr/HybridEngine                 （**无 Authorization**）
  ⇒ **HTTP 404** ⇒ ⚠ **PRIVATE**（或不存在）
```
⇒ ⭐ **方法本身值得记**：**"仓库是否公开"是可以自查的** ——
  不带凭据能读到 = 公开；读不到 = 私有（或不存在）。
  ⚠ 而 `web_fetch` 那个 HTML 地址在**限制外网的环境里会失败** ⇒ **换 API 端点就通了**。

---

## 二、⭐ 对**公开仓**做的全仓凭据扫描（不是抽样）

```
公开仓 main 分支：**803 个文件**，其中扫 **775 个文本文件**（逐个下载 + base64 解码 + 正则）
```

| 模式 | 结果 |
|---|---|
| GitHub PAT（`ghp_`/`gho_`/`ghu_`/`ghs_`/`ghr_`） | ✅ **无** |
| GitHub 细粒度 PAT（`github_pat_`） | ✅ **无** |
| AWS Access Key（`AKIA…`） | ✅ **无** |
| 私钥块（`BEGIN … PRIVATE KEY`） | ✅ **无** |
| `sk-` 风格（OpenAI/Gitee 等） | ✅ **无** |
| `password = "…"` 赋值 | ✅ **无** |
| `token = "…"` 赋值 | ✅ **无** |
| `api_key = "…"` 赋值 | ✅ **无** |
| SSH 私钥文件名（`id_rsa`/`id_ed25519`） | ✅ **无** |
| **Windows 家目录（`X:\Users\<名>`）** | ⚠ **109 处** |

⇒ ⭐ **泄露面 = 用户名 + 本机目录结构**，**没有真凭据**。
  首个样本：`_build/_audit_deadpaths.py` ⇒ `D:\Users\etgya`

⚠⚠ **而这是启发式模式扫描 —— 不是"保证没有"**。如实说：
  · 只扫了**文本类后缀**（`.md/.txt/.py/.ps1/.json/.cs/.cpp/.hpp/.h/.c/.yml/.yaml/.sh/.bat/.cmd/.html/.js/.toml/.cfg/.ini`）
  · 跳过 >900 KB 的文件（有几个）· **二进制/图片/压缩包没扫**
  · 模式表是**我写的**，不认识的凭据形态**扫不出来**
  · 只扫了 `main` 分支的**当前**树 —— ⚠ **历史提交里的内容没扫**
    ⇒ ⭐ 即使现在改掉，**git 历史里仍然有**（要清历史得 `filter-repo` 级别的手术）

---

## 三、⇒ 两个仓的真实状态（一张表）

| | `HybridEngine-LearningCore` | `HybridEngine` |
|---|---|---|
| 远端可见性 | ⭐ **PUBLIC**（无凭据 200） | **PRIVATE**（无凭据 404） |
| 能不能推 | ✅ **能**（github 通，我推过多次） | ❌ **不能**（三条叠加） |
| 未推送提交 | 1 个（`f32487f`，我那份审计） | ⚠ **79 个**（整整一个月） |
| 个人路径命中 | ⚠ **109 处**（**已在公开远端**） | ⚠ 15 处（**只在本地工作区**） |
| 有 `leak_guard` 闸门吗 | ❌ **没有**（那仓根本没有这个脚本） | ✅ 有（`core.hooksPath=.githooks`） |
| CI 状态 | ✅ 绿（但**只建 `libhblight`**） | ❌ **从不跑**（推不上去） |

⇒ ⭐⭐ **所以"哪个更紧急"的答案和我原来的判断相反**（`verify-and-optimize` 说对了）：
```
· `HybridEngine` 的 79 个提交 **还安全躺在本地**（私有仓 + 推不上去 ⇒ 至少没外泄）
· `LearningCore` 的 **109 处个人路径已经在一个 public 仓上**
⇒ ⭐ **该先处理的是"已经出去的"，不是"推不上去的"**
```

---

## 四、⚠ 而 `verify-and-optimize` 抓到一个**很关键的自坑**（我把它记下来）

```
它 dry-run 了 15 处的自动替换，结果**第一版会废掉两个脚本**：
  ❌ `tools/verify_camera.py` **没有 `import os`** ⇒ 换成 env 写法会 **NameError**
  ❌ `tools/verify_snake_persist.py:15` 的 `r"%LOCALAPPDATA%\…"` 是 **Python 原始字符串**
     ⇒ `%LOCALAPPDATA%` **谁都不会展开** ⇒ 路径直接失效
⇒ ⭐ 正是"改完 exit 0 ≠ 生效了"那一类：**文本上没个人路径了，但脚本废了**
⇒ ⭐ 而它把验法定成**两条缺一不可**：
   ① `leak_guard` 退出码 0    ② 两个 `.py` 至少 `python -m py_compile` 过
   ⚠ **"光 leak_guard 绿 ≠ 脚本还能用"** —— 这句话是这一条的核心
```

⚠ 而我这边如实说：**我修自己那 3 处时也踩了同类**（占位符 `X:\Users\<名>` 自己命中模式），
  ⚠ 但我**没有**去跑那些文件是不是还能用 —— **因为我改的是 `.md`**（文档，不是脚本）。
  `tools/神经模拟报告.md` 里那两处是**代码片段**（PowerShell 示例）⇒
  ⭐ **严格说我也该验一下那段片段能不能跑** —— **我没验**，记为缺陷。

---

## 五、⇒ 建议的处置顺序（**给作者，我不自己动别人的文件**）

```
① ⭐ **先决定 `LearningCore` 的公开性** —— 它现在是 public，而里面有 **109 处**个人路径
   ⚠ 而"改成 private"是一条**一条设置就能做完**的事，且**立刻止住继续暴露**
   （⚠ 但**已经 fetch/clone 过的人手里还有** —— 公开过就无法收回）
② 那 109 处该修 —— ⚠ 但**修当前树 ≠ 清历史**；要真清得 `git filter-repo` 级手术
③ `HybridEngine` 那 79 个提交 **不急**（私有仓、没外泄）
   ⇒ 想保险：`git bundle create <路径> --all`（**不经过任何远端、不绕任何闸门**）
④ 那 15 处（`HybridEngine` 工作区）**迟早要修** —— ⚠ 因为**一旦被提交，HEAD 就从干净变脏**
```

⚠ **我这一轮没有动任何别人的文件、没有 push、没有 `--no-verify`。**
