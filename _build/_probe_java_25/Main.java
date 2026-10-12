
public class Main {
// 参考答案 —— 埃氏筛（只写方法，不用写 class / main / import）

// 1. 标记表：表[i] == true 表示 i 是质数
//    先把所有数都当成候选，再把合数一个个划掉
static boolean[] 标记表(int n) {
    boolean[] 是质数 = new boolean[n + 1];      /* ⚠ n+1：下标 0..n 都要装得下 */
    if (n < 2) {
        return 是质数;                          /* 0 和 1 都不是质数，表里全是 false 就对 */
    }
    Arrays.fill(是质数, true);
    是质数[0] = false;                          /* ⚠ 这两行不能忘 */
    是质数[1] = false;

    for (int i = 2; (long) i * i <= n; i++) {    /* 只需用 ≤ √n 的质数去划 */
        if (是质数[i]) {                        /* 被划掉过的（合数）不用管 */
            for (int j = i * i; j <= n; j = j + i) {
                是质数[j] = false;              /* 从 i*i 开始：更小的倍数前几轮已经划过了 */
            }
        }
    }
    return 是质数;
}

// 2. 把所有「还站着的」收集起来
static int[] 筛(int n) {
    boolean[] 是质数 = 标记表(n);
    int 个数 = 0;
    for (int i = 0; i <= n; i++) {
        if (是质数[i]) {
            个数 = 个数 + 1;
        }
    }
    int[] 结果 = new int[个数];
    int 位置 = 0;
    for (int i = 0; i <= n; i++) {
        if (是质数[i]) {
            结果[位置] = i;
            位置 = 位置 + 1;
        }
    }
    return 结果;
}

// 3. 数一数就行（不用真的建数组）
static int 数质数(int n) {
    boolean[] 是质数 = 标记表(n);
    int 个数 = 0;
    for (int i = 0; i <= n; i++) {
        if (是质数[i]) {
            个数 = 个数 + 1;
        }
    }
    return 个数;
}

// 4. 试除法（第 23 课的写法）—— 这一课拿它做速度对照
static boolean 试除法(int n) {
    if (n < 2) {
        return false;
    }
    for (int i = 2; (long) i * i <= n; i++) {
        if (n % i == 0) {
            return false;
        }
    }
    return true;
}


    public static void main(String[] args) {

        boolean[] 表30 = 标记表(30);
        System.out.println("表长|" + 表30.length);
        System.out.println("表0|" + 表30[0]);
        System.out.println("表1|" + 表30[1]);
        System.out.println("表2|" + 表30[2]);
        System.out.println("表30|" + 表30[30]);
        boolean[] 表1 = 标记表(1);
        System.out.println("表-1|" + 表1[1]);

        System.out.println("筛30|" + 拼(筛(30)));
        System.out.println("数1|" + 数质数(1));
        System.out.println("数2|" + 数质数(2));
        System.out.println("数10|" + 数质数(10));
        System.out.println("数100|" + 数质数(100));
        System.out.println("数1000|" + 数质数(1000));

        long 起 = System.nanoTime();
        int 两百万个数 = 数质数(2000000);
        int[] 两百万表 = 筛(2000000);
        long 两百万纳秒 = System.nanoTime() - 起;
        System.out.println("两百万秒|" + (两百万纳秒 / 1000000000));
        System.out.println("两百万个数|" + 两百万个数);
        StringBuilder 首 = new StringBuilder();
        for (int i = 0; i < 6 && i < 两百万表.length; i++) {
            if (i > 0) 首.append(" ");
            首.append(两百万表[i]);
        }
        System.out.println("两百万首|" + 首);
        System.out.println("两百万尾|" + 两百万表[两百万表.length - 1]);

        int 一致的个数 = 0;
        boolean[] 小表 = 标记表(2000);
        for (int i = 0; i <= 2000; i++) {
            if (小表[i] == 试除法(i)) 一致的个数++;
        }
        System.out.println("小范围一致|" + 一致的个数);

    }
}
