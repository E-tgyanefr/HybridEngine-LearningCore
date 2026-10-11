# 汇总 + 校验：17 课模板 -> _build\_java_模板扩写.md
$ErrorActionPreference = 'Stop'
$root = 'D:\Users\etgya\Desktop\Project\引擎\LearnCode'
$tplDir = Join-Path $root '_build\_java_tpl'
$lessonDir = Join-Path $root 'csharp\java-lessons\Lessons'
$outFile = Join-Path $root '_build\_java_模板扩写.md'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function 规范化([string]$文本) {
  $文本 = $文本 -replace "`r`n", "`n"
  return ($文本 -split "`n")
}

$特定提醒 = @{
  '09' = '⚠ 检查会拿不合法的值去调你的 改年龄() / 改名字() —— 看它们有没有**真的被挡住**。光写 private 不够，规矩要写在 setter 里。'
  '10' = '⚠ 检查会分别用三种方式 new 出对象（只知道名字 / 知道名字和分数 / 三样都知道）—— 三个构造方法都要在，而且 this(...) 那条重用要真的生效。'
  '11' = '⚠ 检查会造两个对象、分别给那个 static 计数器加一，再回头看它们看到的是不是同一个数 —— static 到底是「属于类」还是「属于对象」，这里藏不住。'
  '12' = '⚠ 检查会 new 出子类，再从**父类类型的变量**里调方法 —— 覆写有没有真的生效，看输出里是「汪」还是「……」就知道。'
  '13' = '⚠ 检查会拿一组不同的形状放进同一个循环里 —— 面积算不算得对（圆要用 Math.PI）、多态有没有真的生效，一次全看得出来。'
  '14' = '⚠ 检查会拿**接口类型的变量**去指你的对象。实现接口的方法必须是 public —— 漏了就是那句「正在尝试分配更低的访问权限; 以前为public」。'
  '15' = '⚠ 检查会逐行比对那五行的**顺序和内容** —— 尤其「出事:Index 9 out of bounds for length 3」那一行：它证明 try 里出错之后，剩下的语句全被跳过了。'
  '16' = '⚠ 检查会连着调好几个方法（加、取、查、遍历），并且**数输出的行数** —— import 少写一句，第一组就会在「找不到符号：类 ArrayList」那里红。'
}

function 取模板正文([string]$编号) {
  $f = Get-ChildItem $lessonDir -Filter ("第{0}课*.cs" -f $编号) | Select-Object -First 1
  $行 = Get-Content $f.FullName -Encoding UTF8
  $起 = -1; $止 = -1
  for ($i = 0; $i -lt $行.Count; $i++) {
    if ($起 -lt 0 -and $行[$i].Contains('public static string ') -and $行[$i].Contains('=> ""')) { $起 = $i; continue }
    if ($起 -ge 0 -and $行[$i].Trim() -eq '""";') { $止 = $i; break }
  }
  return $行[($起 + 1)..($止 - 1)]
}

function 追加怎么自己测([string]$编号, [string[]]$正文) {
  $句 = @(
    '//',
    '// ─────────────────────────────────────────────────────',
    '//  怎么自己测',
    '// ─────────────────────────────────────────────────────',
    '//   点界面上的「检查」。命令行（在源码目录里）：',
    '//',
    '//       dotnet run --project csharp/java-lessons 检查 ' + $编号,
    '//',
    '//   看这一课要干什么：dotnet run --project csharp/java-lessons ' + $编号,
    '//   看答案（先自己写一遍）：dotnet run --project csharp/java-lessons 答案 ' + $编号,
    '//',
    '//   ' + $特定提醒[$编号],
    '//',
    '//   检查会把你的代码**真的编译、真的跑一遍**再逐条比对 —— 所以它红了就一定有原因，',
    '//   照着那段说明改就行：它一般会直接点出你写错在哪、以及正确的写法长什么样。'
  )
  return ($正文 + $句)
}

$sb = New-Object System.Text.StringBuilder
$报告 = @()
foreach ($n in 0..16) {
  $编号 = '{0:D2}' -f $n
  if ($n -le 8) {
    $正文 = 规范化 ([System.IO.File]::ReadAllText((Join-Path $tplDir ($编号 + '.txt')), [System.Text.Encoding]::UTF8))
    $来源 = '新写'
  } else {
    $正文 = 追加怎么自己测 $编号 (取模板正文 $编号)
    $来源 = '原文 + 追加'
  }
  while ($正文.Count -gt 0 -and $正文[0].Trim() -eq '') { $正文 = $正文[1..($正文.Count - 1)] }
  while ($正文.Count -gt 0 -and $正文[-1].Trim() -eq '') { $正文 = $正文[0..($正文.Count - 2)] }

  [void]$sb.Append('===== ' + $编号 + ' =====').Append("`r`n")
  [void]$sb.Append('<<<模板开始>>>').Append("`r`n")
  foreach ($l in $正文) { [void]$sb.Append($l).Append("`r`n") }
  [void]$sb.Append('<<<模板结束>>>').Append("`r`n")
  $报告 += [PSCustomObject]@{ 编号 = $编号; 来源 = $来源; 行数 = $正文.Count; 首行 = $正文[0] }
}
[System.IO.File]::WriteAllText($outFile, $sb.ToString(), $utf8)
$报告 | Format-Table -AutoSize

# ── 校验 ──
$全部 = [System.IO.File]::ReadAllText($outFile, [System.Text.Encoding]::UTF8)
'=== 校验 ==='
$段 = [regex]::Matches($全部, '(?m)^===== (\d\d) =====\r?$')
'段落数 = ' + $段.Count + '  （应为 17）'
'开始标记 = ' + [regex]::Matches($全部, '(?m)^<<<模板开始>>>\r?$').Count
'结束标记 = ' + [regex]::Matches($全部, '(?m)^<<<模板结束>>>\r?$').Count
$序 = ($段 | ForEach-Object { $_.Groups[1].Value }) -join ' '
'编号顺序 = ' + $序
'三个连续双引号出现次数 = ' + ([regex]::Matches($全部, '"""')).Count + '  （应为 0）'
$裸竖线 = 0
foreach ($m in [regex]::Matches($全部, '\|')) {
  $i = $m.Index
  $前 = if ($i -gt 0) { $全部[$i-1] } else { ' ' }
  $后 = if ($i + 1 -lt $全部.Length) { $全部[$i+1] } else { ' ' }
  if ($前 -ne '|' -and $后 -ne '|') { $裸竖线++ }
}
'单根竖线出现次数 = ' + $裸竖线 + '  （应为 0）'
'总行数 = ' + (($全部 -split "`n").Count)
