cmake_minimum_required(VERSION 3.16)
project(hblight LANGUAGES CXX)

# ══════════════════════════════════════════════════════════════════════════
# hblight —— 轻量引擎（pyengine）的 C++ 内核
#
# ⚠ 零第三方依赖是**硬约束**：只用 C++ 标准库。
#   这里连 find_package 都不调一个 —— 唯一"外部"的东西是系统 libm
#   （C++ 标准库本来就带），Windows 上就是 ucrt。
# ══════════════════════════════════════════════════════════════════════════

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

if(NOT CMAKE_BUILD_TYPE)
  set(CMAKE_BUILD_TYPE Release CACHE STRING "" FORCE)
endif()

set(HBLIGHT_SOURCES
  ${CMAKE_CURRENT_SOURCE_DIR}/hblight.cpp
)

# 命名跟 Python 侧 ctypes 的期望一致：Windows= hblight.dll（与 libhblight.dll 都可能），
# Linux= libhblight.so，macOS= libhblight.dylib
add_library(hblight SHARED ${HBLIGHT_SOURCES})
target_include_directories(hblight PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

# ⚠ 符号可见性：非 Windows 上不显式开 visibility=hidden，
#   否则 HB_API 的 default 属性没意义（默认就全导出，也没坏处）。
if(NOT WIN32)
  set_target_properties(hblight PROPERTIES CXX_VISIBILITY_PRESET default)
endif()

# ⚠ 0 警告是本仓的门禁约定（专业版 CI 就是这么卡的）。这里对**自己的代码**要求：
if(MSVC)
  target_compile_options(hblight PRIVATE /W4)
else()
  target_compile_options(hblight PRIVATE -Wall -Wextra -Wpedantic)
endif()

# ── 自检程序（判据的 C++ 侧）────────────────────────────────────────────
#   `hblight_selftest` 打印确定性结果，供 `tests/diff_python.py` 与 Python 实现对账。
add_executable(hblight_selftest ${CMAKE_CURRENT_SOURCE_DIR}/tests/selftest.cpp)
target_link_libraries(hblight_selftest PRIVATE hblight)
if(MSVC)
  target_compile_options(hblight_selftest PRIVATE /W4)
else()
  target_compile_options(hblight_selftest PRIVATE -Wall -Wextra -Wpedantic)
endif()

install(TARGETS hblight LIBRARY DESTINATION lib RUNTIME DESTINATION bin)

# ══════════════════════════════════════════════════════════════════════════
# ⚠⚠ MinGW：**必须静态链上 GCC 运行时**，否则这个 .dll 在 Python 里加载不了
#
# 实测踩到（而且它是**静默**的）：
#   MinGW 默认让 .dll 依赖 libgcc_s_seh-1.dll / libstdc++-6.dll /
#   libwinpthread-1.dll —— 那三个在 WinLibs 的 `mingw64\bin` 里，
#   **不在系统 PATH 上**。于是 `ctypes.CDLL(libhblight.dll)` 抛
#   "Could not find module ... (or one of its dependencies)"，
#   而 `pyengine_c` 按设计**退回 Python 版** ⇒ 判据照样全绿。
#   ⇒ 那是一次**假绿**：颜色是 Python 版的，不是内核的。
#
# 写法只用 C ABI（不跨 C++ ABI 传对象），所以静态链运行时是**安全**的。
if(MINGW)
  target_link_options(hblight PRIVATE -static -static-libgcc -static-libstdc++)
  target_link_options(hblight_selftest PRIVATE -static -static-libgcc -static-libstdc++)
endif()

# ══════════════════════════════════════════════════════════════════════════
# 渲染后端（task-12）—— 轻量版**自己的**软件光栅化器 + 自己的窗口
#
# ⚠ 独立性红线（作者裁决）：**不共用专业版任何代码**。
#   这里没有 link 专业版的任何 target，也没有它的 include 目录。
#
# ⚠ 窗口实现按平台选：Windows 用 win32，其余用**兜底的空实现**
#   （`window_null.cpp` 不是占位符 —— CI/无头/手机本来就没有窗口，
#     而 `hb_window_backend()` 会明说 `"null"` ⇒ "没开窗"是可观测的）。
# ══════════════════════════════════════════════════════════════════════════
set(HBLIGHT_RENDER_SOURCES
  ${CMAKE_CURRENT_SOURCE_DIR}/raster.cpp
)
if(WIN32)
  list(APPEND HBLIGHT_RENDER_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/window_win32.cpp)
else()
  list(APPEND HBLIGHT_RENDER_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/window_null.cpp)
endif()

# ⚠ STATIC 而不是 SHARED：这一版的消费者是 `bench_backend`（同一个构建里的可执行）。
#   要暴露给 Python 侧是**下一步**（怎么嵌回学习内核的 GUI 还没定，别抢跑）。
add_library(hblight_render STATIC ${HBLIGHT_RENDER_SOURCES})
target_include_directories(hblight_render PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

if(WIN32)
  # 窗口 + StretchDIBits：user32 / gdi32 是**系统库**，不算第三方依赖
  target_link_libraries(hblight_render PUBLIC user32 gdi32)
endif()

if(MSVC)
  target_compile_options(hblight_render PRIVATE /W4)
else()
  target_compile_options(hblight_render PRIVATE -Wall -Wextra -Wpedantic)
endif()

#: 那个靶子的实测程序（1000 个圆 ⇒ ms/帧 + "真的画了"的证据）
add_executable(bench_backend ${CMAKE_CURRENT_SOURCE_DIR}/tests/bench_backend.cpp)
target_link_libraries(bench_backend PRIVATE hblight_render)
if(MSVC)
  target_compile_options(bench_backend PRIVATE /W4)
else()
  target_compile_options(bench_backend PRIVATE -Wall -Wextra -Wpedantic)
endif()

if(MINGW)
  target_link_options(hblight_render PRIVATE -static -static-libgcc -static-libstdc++)
  target_link_options(bench_backend PRIVATE -static -static-libgcc -static-libstdc++)
endif()
