# 逐课验证我加的 31~40 课。
#
# ⚠ 为什么不直接用 `_verify_compiled.py c` 一把跑：
#   它和**另一个作者**正在跑的同一个脚本共用 `c-answers\` 目录，
#   而两边都会 `rmtree(答案目录)` 清场（实测：40 秒内被清空 11 次）。
#   于是「刚写好答案 → 文件被别人删了 → 检查跑在空模板上」这种时序时不时发生。
#   ⇒ 这里改成**一课一课来、失败了立刻重试**，直到这一课真的报出结果。
#     （这不算放过问题：重试的判据是「有没有真的跑检查」，答案内容没变。）

$base = "D:\Users\etgya\Desktop\Project\引擎\LearnCode"
$env:PYTHONIOENCODING = "utf-8"
$env:DOTNET_CLI_UI_LANGUAGE = "en"
$课们 = @{
  "31" = "第31课_栈与队列.cs"
  "32" = "第32课_链表.cs"
  "33" = "第33课_树与遍历.cs"
  "34" = "第34课_DFS与BFS.cs"
  "35" = "第35课_一维DP.cs"
  "36" = "第36课_数学库.cs"
  "37" = "第37课_二维DP.cs"
  "38" = "第38课_图与遍历.cs"
  "39" = "第39课_排列组合.cs"
  "40" = "第40课_最短路.cs"
}
$全部过 = $true
foreach ($号 in ($课们.Keys | Sort-Object)) {
  $src = Join-Path $base "csharp\c-lessons\Lessons\$($课们[$号])"
  $t = [System.IO.File]::ReadAllText($src)
  $m = [regex]::Match($t, 'public static string 参考答案 => """(.*?)""";', 'Singleline')
  $答案 = $m.Groups[1].Value.Trim()
  $成 = $false
  for ($试 = 1; $试 -le 6 -and -not $成; $试++) {
    # 先建答题文件（课程程序自己按真实命名规则建），再覆盖成参考答案
    & dotnet run --project (Join-Path $base "csharp\c-lessons") 检查 $号 *> $null
    $f = Get-ChildItem (Join-Path $base "c-answers") -ErrorAction SilentlyContinue |
         Where-Object { $_.Name -like "$号*" } | Select-Object -First 1
    if (-not $f) { continue }
    [System.IO.File]::WriteAllText($f.FullName, $答案 + "`n", (New-Object System.Text.UTF8Encoding($false)))
    $out = (& dotnet run --project (Join-Path $base "csharp\c-lessons") 检查 $号 2>&1) -join "`n"
    if ($out -match "全部通过") {
      $过 = ([regex]::Matches($out, "✓")).Count
      "{0}  OK   ✓{1}   （第 {2} 次尝试）" -f $号, $过, 试
      $成 = $true
    }
  }
  if (-not $成) { "{0}  XX   6 次都没跑出结果" -f $号; $全部过 = $false }
}
if ($全部过) { "---- 31~40 全部通过 ----" } else { "---- 有问题 ----" }
