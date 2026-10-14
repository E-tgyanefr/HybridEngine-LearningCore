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
