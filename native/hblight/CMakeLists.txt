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

# ══════════════════════════════════════════════════════════════════════════
# 子窗口嵌入探针（架构实验）—— `tests/embed_probe.py` 用 ctypes 驱动它
#
# ⚠ **必须是 SHARED**：驱动方是 **Python/Tk**（Tk 占着主线程和它自己的事件循环），
#   所以只能由 Python `ctypes` 加载。`hblight_render` 是 STATIC（消费者是
#   同一个构建里的 exe），这里**另起一个 SHARED 目标**，
#   ⇒ 不是把已有目标改掉，而是**纯追加**（已有两个目标一个字没动）。
#
# ⚠ 只用 `raster.cpp` + `window_embed.cpp`：**不把 window_win32.cpp 拉进来**，
#   免得和已有那个顶层窗口类重名/抢注册表项。
# ══════════════════════════════════════════════════════════════════════════
add_library(hblight_embed SHARED
  ${CMAKE_CURRENT_SOURCE_DIR}/raster.cpp
  ${CMAKE_CURRENT_SOURCE_DIR}/window_embed.cpp
)
target_include_directories(hblight_embed PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
if(WIN32)
  target_link_libraries(hblight_embed PUBLIC user32 gdi32)
endif()
if(MSVC)
  target_compile_options(hblight_embed PRIVATE /W4)
else()
  target_compile_options(hblight_embed PRIVATE -Wall -Wextra -Wpedantic)
endif()
if(MINGW)
  target_link_options(hblight_embed PRIVATE -static -static-libgcc -static-libstdc++)
endif()

# ══════════════════════════════════════════════════════════════════════════
# ⚠⚠ task-17 追加：**线程库**（`raster.cpp` 现在用 `std::thread` 做按行并行）
#
#   为什么必须显式加：POSIX 上 `std::thread` 要链 pthread（GCC 用 `-pthread`），
#   不链的话是**链接期**报 `undefined reference to pthread_create` ——
#   而 Windows（MinGW）上什么都不需要。
#   ⇒ 用 CMake 的 `Threads::Threads`（它自己知道各平台该加什么），
#     这样三平台都对，而不是只在 Windows 上"看起来没事"。
#
#   ⚠ 只影响**含 raster.cpp 的两个目标**：`hblight_render`（静态，给 bench 用）
#     与 `hblight_embed`（共享，Python ctypes 用）。**已有目标一个都没动**。
#   ⚠ 静态链 MinGW 运行时的策略不变（上面那两行），
#     所以 `libhblight_embed.dll` 的依赖里**不会**多出 libwinpthread-1.dll。
# ══════════════════════════════════════════════════════════════════════════
find_package(Threads REQUIRED)
target_link_libraries(hblight_render PUBLIC Threads::Threads)
target_link_libraries(hblight_embed  PUBLIC Threads::Threads)
