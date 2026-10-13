/* 用**真控制台**启动一个程序，把它的 stdout/stderr 落盘。
 *
 * 为什么需要这个：
 *   双击 exe 时 Windows 会给它一个**新的控制台**。我用普通重定向测的话
 *   `isatty()` 为假 —— 那是**另一个环境**，测出来的结论不作数。
 *   这个启动器用 CREATE_NEW_CONSOLE 给出同样的环境，
 *   同时把两个流接到文件上，好让我看到它到底报什么错。
 */
#include <stdio.h>
#include <windows.h>

int main(int argc, char** argv) {
    if (argc < 4) {
        fprintf(stderr, "用法: launcher <工作目录> <输出文件> <程序> [参数...]\n");
        return 2;
    }
    const char* cwd = argv[1];
    const char* outfile = argv[2];
    const char* prog = argv[3];

    SECURITY_ATTRIBUTES sa = { sizeof(sa), NULL, TRUE };
    HANDLE hOut = CreateFileA(outfile, GENERIC_WRITE, FILE_SHARE_READ, &sa,
                              CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hOut == INVALID_HANDLE_VALUE) { fprintf(stderr, "打不开输出文件\n"); return 3; }

    char cmd[8192];
    int n = snprintf(cmd, sizeof(cmd), "\"%s\"", prog);
    for (int i = 4; i < argc; i++) n += snprintf(cmd + n, sizeof(cmd) - n, " %s", argv[i]);

    STARTUPINFOA si;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESTDHANDLES;
    si.hStdOutput = hOut;
    si.hStdError = hOut;
    si.hStdInput = NULL;                 /* 新控制台的 stdin（关键：让它像双击） */
    PROCESS_INFORMATION pi;
    ZeroMemory(&pi, sizeof(pi));

    BOOL ok = CreateProcessA(NULL, cmd, NULL, NULL, TRUE,
                             CREATE_NEW_CONSOLE, NULL, cwd, &si, &pi);
    if (!ok) {
        DWORD e = GetLastError();
        CloseHandle(hOut);
        fprintf(stderr, "CreateProcess 失败，错误码 %lu\n", e);
        return 4;
    }
    DWORD wait = WaitForSingleObject(pi.hProcess, 25000);
    DWORD code = 0;
    BOOL exited = GetExitCodeProcess(pi.hProcess, &code);
    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);
    CloseHandle(hOut);
    printf("退出码=%lu 已结束=%d 等待结果=%lu\n", code, exited ? 1 : 0, wait);
    return 0;
}
