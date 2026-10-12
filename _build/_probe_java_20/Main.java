
public class Main {
// 参考答案 —— 算法复杂度（只写方法，不用写 class / main / import）

// 1. 一层循环：O(n)。n=100000 时这里要真的做十万次加法 ——
//    这一课的「贵」就是它，不能换成公式。
static long 循环求和(int n) {
    long 和 = 0;                       /* ⚠ 用 long：int 在 n=100000 时会溢出 */
    for (int i = 1; i <= n; i++) {
        和 = 和 + i;
    }
    return 和;
}

// 2. 公式：O(1)。和 n 无关，永远就这几步
static long 公式求和(int n) {
    /* ⚠ 先转 long 再乘：n*(n+1) 在 n=100000 时是 10^10，int 装不下 */
    return (long) n * (n + 1) / 2;
}

// 3. 数一数内层跑了多少次：外层 n 次 × 内层 n 次 = n²
static long 平方对(int n) {
    long 次数 = 0;
    for (int i = 0; i < n; i++) {
        for (int j = 0; j < n; j++) {
            次数 = 次数 + 1;           /* 内层每转一圈算一次 */
        }
    }
    return 次数;
}

// 4. n 小的时候两种都看不出慢；n 大了，O(n²) 才要命
static String 选哪个(int n) {
    if (n <= 1000) {
        return "都不慢";
    }
    return "循环太慢";
}


    public static void main(String[] args) {

        System.out.println("循环小|" + 循环求和(10));
        System.out.println("公式小|" + 公式求和(10));
        System.out.println("循环零|" + 循环求和(0));
        System.out.println("公式零|" + 公式求和(0));
        System.out.println("循环大|" + 循环求和(100000));
        System.out.println("公式大|" + 公式求和(100000));
        System.out.println("平方3|" + 平方对(3));
        System.out.println("平方10|" + 平方对(10));
        System.out.println("选1000|" + 选哪个(1000));
        System.out.println("选1001|" + 选哪个(1001));
        System.out.println("选十万|" + 选哪个(100000));

        long 起 = System.nanoTime();
        long 甲 = 循环求和(100000);
        long 循环纳秒 = System.nanoTime() - 起;
        起 = System.nanoTime();
        long 乙 = 公式求和(100000);
        long 公式纳秒 = System.nanoTime() - 起;
        System.out.println("循环毫秒|" + (循环纳秒 / 1000000));
        System.out.println("公式毫秒|" + (公式纳秒 / 1000000));
        System.out.println("计时闸门|" + (甲 == 乙 ? "一致" : "不一致"));

    }
}
