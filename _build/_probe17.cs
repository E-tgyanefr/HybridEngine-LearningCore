// 探针：把第17课「取一段」里那几行原样搬出来，喂进真实输出，看它到底取出什么。
using System.Text.RegularExpressions;

string cs = File.ReadAllText(
    @"D:\Users\etgya\Desktop\Project\引擎\LearnCode\csharp\cpp-lessons\Lessons\第17课_排序入门.cs");
string call = Regex.Match(cs, "private const string 调用点 = \"\"\"\r?\n(.*?)\r?\n\"\"\";", RegexOptions.Singleline).Groups[1].Value;
string ans = Regex.Match(cs, "public static string 参考答案 => \"\"\"\r?\n(.*?)\r?\n\"\"\";", RegexOptions.Singleline).Groups[1].Value;

string prog = "#include <iostream>\n#include <string>\n#include <vector>\n#include <cmath>\n#include <algorithm>\n\n"
              + ans + "\n\nint main() {\n" + call + "\nreturn 0;\n}\n";
string dir = Path.Combine(Path.GetTempPath(), "probe17");
Directory.CreateDirectory(dir);
File.WriteAllText(Path.Combine(dir, "m.cpp"), prog);

var psi = new System.Diagnostics.ProcessStartInfo("g++",
    "-std=c++17 -O1 -Wall -o \"" + Path.Combine(dir, "m.exe") + "\" \"" + Path.Combine(dir, "m.cpp") + "\"")
{ RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false };
var p = System.Diagnostics.Process.Start(psi)!;
string err = p.StandardError.ReadToEnd();
p.WaitForExit();
if (p.ExitCode != 0) { Console.WriteLine(err); return; }

var psi2 = new System.Diagnostics.ProcessStartInfo(Path.Combine(dir, "m.exe"))
{ RedirectStandardOutput = true, UseShellExecute = false, StandardOutputEncoding = System.Text.Encoding.UTF8 };
var p2 = System.Diagnostics.Process.Start(psi2)!;
string outp = p2.StandardOutput.ReadToEnd();
p2.WaitForExit();

var 行 = outp.Split('\n').Select(x => x.TrimEnd('\r')).Where(x => x.Length > 0).ToList();
string 全部 = string.Join(" ", 行);
Console.WriteLine("全长 " + 全部.Length);
Console.WriteLine(全部);
Console.WriteLine();

string[] 所有标记 = { "冒泡1|", "冒泡2|", "冒泡3|", "冒泡4|", "冒泡空|",
                      "选择1|", "选择2|", "选择3|", "选择4|", "选择空|",
                      "插入1|", "插入2|", "插入3|", "插入4|", "插入空|", "哨兵|" };

foreach (var 标记 in new[] { "冒泡1|", "冒泡空|", "哨兵|" })
{
    var 本标记 = new List<int>();
    for (int i = 全部.IndexOf(标记, StringComparison.Ordinal);
         i >= 0; i = 全部.IndexOf(标记, i + 1, StringComparison.Ordinal))
    {
        if (i == 0 || 全部[i - 1] == ' ' || 全部[i - 1] == '\t') 本标记.Add(i);
    }
    int 起 = 本标记[0] + 标记.Length;
    int 止 = 全部.Length; int 谁 = -1;
    foreach (var 名 in 所有标记)
        for (int i = 全部.IndexOf(名, StringComparison.Ordinal);
             i >= 0; i = 全部.IndexOf(名, i + 1, StringComparison.Ordinal))
            if (i > 起 && i < 止 && (i == 0 || 全部[i - 1] == ' ' || 全部[i - 1] == '\t')) { 止 = i; 谁 = i; }
    Console.WriteLine(标记 + " 本标记=" + string.Join(",", 本标记) + " 起=" + 起 + " 止=" + 止 + " 谁=" + 谁
                      + " => [" + 全部.Substring(起, 止 - 起).Trim() + "]");
}
