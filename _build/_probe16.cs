// 探针：把第16课检查里的「取一行」逻辑拿出来，喂进真实输出，看它到底取到什么。
using System.Text.RegularExpressions;

string cs = File.ReadAllText(
    @"D:\Users\etgya\Desktop\Project\引擎\LearnCode\csharp\cpp-lessons\Lessons\第16课_二维数组.cs");
string call = Regex.Match(cs, "private const string 调用点 = \"\"\"\r?\n(.*?)\r?\n\"\"\";", RegexOptions.Singleline).Groups[1].Value;
string ans = Regex.Match(cs, "public static string 参考答案 => \"\"\"\r?\n(.*?)\\s*\"\"\";", RegexOptions.Singleline).Groups[1].Value;

string prog = "#include <iostream>\n#include <string>\n#include <vector>\n#include <cmath>\n#include <algorithm>\n\n"
              + ans + "\n\nint main() {\n" + call + "\nreturn 0;\n}\n";
string dir = Path.Combine(Path.GetTempPath(), "probe16");
Directory.CreateDirectory(dir);
File.WriteAllText(Path.Combine(dir, "m.cpp"), prog);
Console.WriteLine("--- 生成的 main 里含 行2| 的行 ---");
foreach (var L in prog.Split('\n')) if (L.Contains("行2|")) Console.WriteLine("<" + L + ">");
Console.WriteLine("--- 生成的 main 前 3 行 ---");
foreach (var L in prog.Split('\n').SkipWhile(x => !x.StartsWith("int main")).Take(3)) Console.WriteLine("<" + L + ">");

var psi = new System.Diagnostics.ProcessStartInfo("g++",
    "-std=c++17 -O1 -Wall -o \"" + Path.Combine(dir, "m.exe") + "\" \"" + Path.Combine(dir, "m.cpp") + "\"")
{ RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false };
var p = System.Diagnostics.Process.Start(psi)!;
p.WaitForExit();
if (p.ExitCode != 0) { Console.WriteLine(p.StandardError.ReadToEnd()); return; }

Console.WriteLine("--- 调用点第 1 行（原样）---");
foreach (var L in call.Split('\n').Take(2)) Console.WriteLine("<" + L + ">");
Console.WriteLine("--- 调用点里含 行2| 的行 ---");
foreach (var L in call.Split('\n')) if (L.Contains("行2|")) Console.WriteLine("<" + L + ">");

var psi2 = new System.Diagnostics.ProcessStartInfo(Path.Combine(dir, "m.exe"))
{ RedirectStandardOutput = true, UseShellExecute = false, StandardOutputEncoding = System.Text.Encoding.UTF8 };
var p2 = System.Diagnostics.Process.Start(psi2)!;
string outp = p2.StandardOutput.ReadToEnd();
p2.WaitForExit();

var 行 = outp.Split('\n').Select(x => x.TrimEnd('\r')).Where(x => x.Length > 0).ToList();
Console.WriteLine("共 " + 行.Count + " 行原始输出");
Console.WriteLine("--- 第 1 行原样 ---");
Console.WriteLine("[" + 行[0] + "]");
Console.WriteLine("含 行2| ? " + 行[0].Contains("行2|"));
Console.WriteLine("IndexOf 行2| = " + 行[0].IndexOf("行2|", StringComparison.Ordinal));
Console.WriteLine("--- 前 3 行 ---");
for (int k = 0; k < 3 && k < 行.Count; k++) Console.WriteLine("[" + 行[k] + "]");

foreach (var (标记, 第几个) in new[] { ("行1|", 0), ("行2|", 0), ("行3|", 0), ("行小|", 0), ("列1|", 0), ("列小|", 0) })
{
    string 全部 = string.Join(" ", 行);
    var 位置 = new List<int>();
    for (int i = 全部.IndexOf(标记, StringComparison.Ordinal);
         i >= 0; i = 全部.IndexOf(标记, i + 1, StringComparison.Ordinal))
    {
        if (i == 0 || 全部[i - 1] == ' ' || 全部[i - 1] == '\t') 位置.Add(i);
    }
    Console.WriteLine(标记 + " 出现 " + 位置.Count + " 次，位置 = " + string.Join(",", 位置));
    if (位置.Count > 第几个)
    {
        int 起 = 位置[第几个] + 标记.Length;
        int 止 = 第几个 + 1 < 位置.Count ? 位置[第几个 + 1] : 全部.Length;
        Console.WriteLine("     => [" + 全部.Substring(起, 止 - 起).Trim() + "]");
    }
}
