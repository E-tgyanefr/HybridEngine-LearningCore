"""hybridengine_light 课程入口 —— 让 `python -m hybridengine_light` 能用。

装了这个包之后，不需要 cd 到项目目录，直接：
    python -m hybridengine_light
"""

import sys

from .run import main

if __name__ == "__main__":
    sys.exit(main())
