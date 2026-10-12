# 跑十课的反面测试，把结果写成一份摘要（给我验收用）。
# 用法：pwsh -File _build\_neg_all.ps1
$ErrorActionPreference = "Continue"
$root = "D:\Users\etgya\Desktop\Project\引擎\LearnCode"
$dll = Join-Path $env:TEMP "cpl17\bin\Debug\net8.0\HybridEngineLight.CppLessons.dll"
$neg = Join-Path $root "_build\_neg"
$out = Join-Path $root "_build\_neg_out"
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location (Join-Path $env:TEMP "refl16")
$env:DOTNET_CLI_UI_LANGUAGE = "en"

$摘要 = @()
foreach ($n in @("16","17","18","19","20","21","22","23","24","25")) {
    $f = Get-ChildItem $neg -Filter "$n`_*.cpp" | Select-Object -First 1
    if (-not $f) { $摘要 += "第 $n 课：**没有反面用例**"; continue }
    $args = @("run", "--no-build", "--", "第$n课", $f.FullName, $dll)
    & dotnet @args 2>&1 | Out-File -Encoding utf8 (Join-Path $out "neg$n.txt")
    $行 = Get-Content (Join-Path $out "neg$n.txt") -Encoding utf8
    $分 = ($行 | Where-Object { $_ -match '^== ' } | Select-Object -First 1)
    $不过 = ($行 | Where-Object { $_ -match '^  XX ' } | ForEach-Object { $_.Trim() })
    $摘要 += "第 $n 课  反面用例：$($f.Name)   $分"
    if ($不过.Count -eq 0) { $摘要 += "        ⚠⚠ 一条都没拦住！" }
    else { foreach ($x in $不过) { $摘要 += "        $x" } }
    $摘要 += ""
}
$摘要 | Out-File -Encoding utf8 (Join-Path $out "摘要.txt")
$摘要 | ForEach-Object { $_ }
