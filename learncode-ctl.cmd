@echo off
rem `learncode-ctl` 的 Windows 入口 —— 让命令行形状与专业版的 ms_ctl.exe 一致。
rem
rem ⚠ 为什么需要它：Windows 不认 shebang（`#!/usr/bin/env python3`）——
rem   没有 .cmd/.bat 的话，命令行里打 `learncode-ctl` 会报"不是内部或外部命令"。
rem   而"打一条命令就能用"正是这个项目对新手最基本的要求。
rem
rem ⚠ `python` 而不是 `py`：本项目的目标是"装完 Python 就能跑"，
rem   而 `python` 是两条安装路径（官网安装包 / Microsoft Store）都会有的名字。
rem   装了 Microsoft Store 版的机器上 `py` 反而不一定有。
python "%~dp0learncode-ctl" %*
