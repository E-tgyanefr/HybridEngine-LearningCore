"""题库与考试系统的测试。

════ 为什么单独测题库 ════

题库是**数据**，写错了不会立刻暴露 —— 直到学生做到那一题才炸。
我自己就踩过：题库文件里一个引号没转义，学生运行 `考试` 看到的是
"SyntaxError: Perhaps you forgot a comma?"，而他根本不知道那是什么。

所以题库必须**发布前自检**：
  · 语法能解析
  · 选择题的 answer 在选项范围内
  · output/fill 题有标准答案
  · code 题有可执行的测试用例，而且**正确答案能通过、错误答案通不过**
  · 每题都有 explain（没有解释的题等于白做）

最后一条最重要：如果一道 code 题的测试用例写得不对，正确解法也会被判错 ——
那比没有题更糟。这里**真的跑一遍参考解法**来验证。
"""

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from learncode.exam import load_banks, validate_bank, normalize  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


class TestBankIntegrity(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.bank, cls.metas = load_banks(__import__("pathlib").Path(ROOT))

    def test_bank_is_not_empty(self):
        self.assertGreater(len(self.bank), 0, "题库加载不到任何题目")

    def test_no_problems(self):
        """核心：题库自检必须零问题。"""
        problems = validate_bank(self.bank)
        self.assertEqual(problems, [], "题库有问题：\n" + "\n".join(problems))

    def test_ids_unique_across_banks(self):
        ids = [q.id for q in self.bank]
        self.assertEqual(len(ids), len(set(ids)), "题号有重复")

    def test_every_question_has_explain(self):
        """答错却看不懂为什么，这道题就白做了。"""
        missing = [q.id for q in self.bank if not q.explain.strip()]
        self.assertEqual(missing, [], f"这些题缺 explain：{missing}")

    def test_choice_answers_in_range(self):
        for q in self.bank:
            if q.type == "choice":
                with self.subTest(q=q.id):
                    self.assertGreaterEqual(q.answer, 0)
                    self.assertLess(q.answer, len(q.options))

    def test_code_questions_have_tests(self):
        for q in self.bank:
            if q.type == "code":
                with self.subTest(q=q.id):
                    self.assertTrue(q.tests, "代码题没有测试用例，无法判分")


class TestCodeTestsActuallyWork(unittest.TestCase):
    """代码题的测试用例必须真的能判对错。

    ⚠ 这一组是**用参考解法验证测试用例本身**：
      如果测试用例写错了，正确解法会被判错 —— 那比没有题更糟。
      所以对每道 code 题，都跑一遍「正确解」和「错误解」，
      确认前者通过、后者不通过。
    """

    REFERENCE = {
        # 题号 → 参考解法（自己写的，与题库独立）
        "二级-201": "def 是偶数(n):\n    return n % 2 == 0\n",
        "二级-202": "def 反转(文字):\n    return 文字[::-1]\n",
        "二级-203": "def 素数们(n):\n    r = []\n    for m in range(2, n + 1):\n"
                    "        ok = True\n        for d in range(2, int(m ** 0.5) + 1):\n"
                    "            if m % d == 0:\n                ok = False\n                break\n"
                    "        if ok:\n            r.append(m)\n    return r\n",
        "二级-204": "def 统计字数(文字):\n    d = {}\n    for c in 文字:\n"
                    "        d[c] = d.get(c, 0) + 1\n    return d\n",
        "二级-205": "def 排序并去重(数字们):\n    return sorted(set(数字们))\n",
        "基础-050": "def 平方(n):\n    return n * n\n",
        "基础-051": "def 较大(a, b):\n    return a if a > b else b\n",
        "基础-052": "def 求和(数字们):\n    s = 0\n    for x in 数字们:\n        s += x\n    return s\n",
        "基础-053": "def 数个数(列表, 目标):\n    c = 0\n    for x in 列表:\n        if x == 目标:\n            c += 1\n    return c\n",
    }

    # 故意写错的版本：每种错法都该被判错
    WRONG = {
        "二级-201": "def 是偶数(n):\n    return n % 2\n",          # 返回数字而不是布尔
        "二级-202": "def 反转(文字):\n    return 文字\n",           # 根本没反转
        "二级-203": "def 素数们(n):\n    return [m for m in range(1, n + 1)]\n",  # 把 1 也算素数
        "二级-204": "def 统计字数(文字):\n    return len(文字)\n",   # 返回长度而不是字典
        "二级-205": "def 排序并去重(数字们):\n    return list(set(数字们))\n",   # 忘了排序
        "基础-050": "def 平方(n):\n    return n * 2\n",            # 平方写成了乘 2
        "基础-051": "def 较大(a, b):\n    return a\n",              # 永远返回 a
        "基础-052": "def 求和(数字们):\n    s = 0\n    for i in range(len(数字们)):\n        s += i\n    return s\n",  # 加下标而不是元素
        "基础-053": "def 数个数(列表, 目标):\n    return 1\n",       # 硬编码
    }

    @staticmethod
    def _grade(q, src):
        """复刻 examrun._grade_code 的判定逻辑（不 import 是为了避免循环依赖）。"""
        ns = {}
        try:
            exec(compile(src, "<test>", "exec"), ns)
        except Exception as e:                       # noqa: BLE001
            return False, f"跑不起来：{e}"
        for desc, fn in q.tests:
            try:
                if not fn(ns):
                    return False, f"{desc} 不通过"
            except Exception as e:                   # noqa: BLE001
                return False, f"{desc} 出错：{e}"
        return True, "全部通过"

    def test_reference_solutions_pass(self):
        """参考解法必须全过 —— 否则是测试用例写错了，不是解法错了。"""
        bank, _ = load_banks(__import__("pathlib").Path(ROOT))
        for q in bank:
            if q.type != "code":
                continue
            src = self.REFERENCE.get(q.id)
            if src is None:
                self.skipTest(f"{q.id} 没有参考解法")     # 新题忘了补，会在这里显形
                continue
            with self.subTest(q=q.id):
                ok, note = self._grade(q, src)
                self.assertTrue(ok, f"{q.id} 的参考解法竟然没过：{note}\n"
                                    f"说明测试用例写得不对，不是解法不对。")

    def test_wrong_solutions_fail(self):
        """错误解法必须被判错 —— 否则测试用例太松，等于没判。"""
        bank, _ = load_banks(__import__("pathlib").Path(ROOT))
        for q in bank:
            if q.type != "code":
                continue
            bad = self.WRONG.get(q.id)
            if bad is None:
                continue
            with self.subTest(q=q.id):
                ok, _ = self._grade(q, bad)
                self.assertFalse(ok, f"{q.id} 的错误解法居然过了 —— 测试用例太松")


class TestExamMeta(unittest.TestCase):
    def test_cert_exam_has_meta(self):
        _, metas = load_banks(__import__("pathlib").Path(ROOT))
        shorts = [m.short for m in metas]
        self.assertIn("二级Python", shorts, "二级 Python 题库缺 EXAM 元数据")

    def test_meta_has_name_and_source(self):
        """有考试名和依据说明 —— 学生需要知道自己在准备什么，
        也需要知道这里不是真题。"""
        _, metas = load_banks(__import__("pathlib").Path(ROOT))
        for m in metas:
            with self.subTest(exam=m.short):
                self.assertTrue(m.name, "考试缺名称")
                self.assertTrue(m.source, "考试缺来源说明（必须写明是不是真题）")

    def test_questions_tagged_with_exam(self):
        bank, _ = load_banks(__import__("pathlib").Path(ROOT))
        cert = [q for q in bank if q.exam == "二级Python"]
        self.assertGreater(len(cert), 10, "二级 Python 题量太少")
        for q in cert:
            with self.subTest(q=q.id):
                self.assertGreater(q.points, 0, "考级题必须有分值")


class TestNormalize(unittest.TestCase):
    """答案归一化 —— 不能因为标点洁癖判学生错。"""

    def test_whitespace_ignored(self):
        self.assertEqual(normalize("print(1)"), normalize("print( 1 )"))

    def test_quote_style_ignored(self):
        """中文引号和英文引号都该接受 —— 学生用中文输入法很常见。"""
        self.assertEqual(normalize('"abc"'), normalize("“abc”"))

    def test_case_matters(self):
        """大小写必须区分 —— 那是有意义的信息。"""
        self.assertNotEqual(normalize("True"), normalize("true"))

    def test_content_matters(self):
        self.assertNotEqual(normalize("int"), normalize("float"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
