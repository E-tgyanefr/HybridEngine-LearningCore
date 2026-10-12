# Sync a PRIVATE sandbox copy of the C# course project.
#
# WHY: csharp/cs-lessons is ONE .NET project holding all lesson files.
# If several authors build/run the real project at the same time they fight over
# obj\ / bin\ file locks -- symptoms are "compile failed although my code is fine",
# and it hurts OTHER authors too.
#   => Lesson files are written into the REAL directory (that is the deliverable),
#      but every author BUILDS and RUNS in his own sandbox copy.
#
# USAGE (Windows PowerShell 5.1):
#   powershell -NoProfile -ExecutionPolicy Bypass -File _cs17_40\sync-sandbox.ps1 -Box C:\lc_box1 -Keep 18,19,20,21
#
# -Keep lists the lesson numbers YOU own. Lessons 00..16 are always copied
# (they are finished and stable); of the new lessons 17..40 only yours are copied.
# WHY: several authors write different lessons at the same time. Without -Keep the
# sandbox would also copy their half-finished files, and ONE broken file anywhere
# would break YOUR build too. With -Keep, your build only depends on your own files.
#
# THEN:
#   cd C:\lc_box1
#   $env:TEMP="C:\lc_tmp1"        # every author needs his OWN TEMP too
#   dotnet run --project csharp/cs-lessons 检查 18
#
# WHY a private TEMP: CSharpRunner compiles the student code inside the fixed
# directory %TEMP%\hybridengine_light_cs. Two authors sharing one TEMP overwrite
# each other's Program.cs there -- symptoms are nonsense check results, or
# "0 passed 0 failed" (the checks never ran at all).
#
# NOTE: keep this file ASCII-only. PowerShell 5.1 reads BOM-less .ps1 files as
# ANSI, so non-ASCII text here turns into parser errors.
param(
    [Parameter(Mandatory=$true)][string]$Box,
    [string]$Keep = ""
)

# The real project root is the PARENT of this script's folder
# (this script lives in <project>\_cs17_40\).
# Derived, NOT hard-coded: the path contains non-ASCII characters, and a BOM-less
# .ps1 is read as ANSI by PowerShell 5.1 -- a hard-coded Chinese path would be
# mangled into a "path not found" error.
$Real = Split-Path -Parent $PSScriptRoot
if ($Box -match 'LearnCode' -or $Box -eq $Real) {
    throw "Refusing to use the real project directory as a sandbox: $Box"
}

Write-Host "real   : $Real"
Write-Host "sandbox: $Box"

New-Item -ItemType Directory -Force -Path "$Box\csharp\cs-lessons\Lessons" | Out-Null

# Remove previously copied lessons so that a stale file cannot linger
# (e.g. a lesson that was renamed in the real project).
Get-ChildItem "$Box\csharp\cs-lessons\Lessons\*.cs" -ErrorAction SilentlyContinue | Remove-Item -Force

Copy-Item "$Real\README.md" "$Box\README.md" -Force
Copy-Item "$Real\csharp\HybridEngineLight" "$Box\csharp\HybridEngineLight" -Recurse -Force
Remove-Item -Recurse -Force "$Box\csharp\HybridEngineLight\obj","$Box\csharp\HybridEngineLight\bin" -ErrorAction SilentlyContinue
Copy-Item "$Real\csharp\cs-lessons\Program.cs","$Real\csharp\cs-lessons\CsLessons.csproj" "$Box\csharp\cs-lessons\" -Force

$Wanted = @{}
foreach ($k in ($Keep -split ",")) {
    $k = $k.Trim()
    if ($k -ne "") { $Wanted[$k] = $true }
}

$copied = 0
foreach ($f in (Get-ChildItem "$Real\csharp\cs-lessons\Lessons\*.cs")) {
    # The file name starts with the (non-ASCII) word for "lesson" followed by two
    # digits: take the FIRST two-digit run as the lesson number. Kept ASCII-only
    # on purpose (see the note about ANSI parsing above).
    $num = ""
    if ($f.Name -match '(\d\d)') { $num = $Matches[1] }
    $take = ($num -eq "" -or [int]$num -le 17 -or $Wanted.ContainsKey($num))
    if ($take) {
        Copy-Item $f.FullName "$Box\csharp\cs-lessons\Lessons\" -Force
        $copied++
    }
}

Write-Host "sandbox synced: $copied lesson files (keep: $Keep)"

