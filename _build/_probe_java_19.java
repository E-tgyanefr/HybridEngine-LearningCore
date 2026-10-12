
import java.util.*;

public class Main {
// 参考答案 —— 稳定性与 Comparator（只写方法，不用写 class / main / import）

// 1. 稳定排序：自己写插入排序
//    它稳定的原因就在那一行 `分数[j] > 拿着` —— **严格大于**。
//    遇到相等的（90 和 90）不再往左挪，于是后来的人永远留在右边。
//    ⚠ 分数和名字要**一起**挪，否则分数对了、名字对不上。
static void 按分数稳定排序(int[] 分数, String[] 名字) {
    for (int i = 1; i < 分数.length; i++) {
        int 拿着分 = 分数[i];
        String 拿着名 = 名字[i];
        int j = i - 1;
        while (j >= 0 && 分数[j] > 拿着分) {      /* ⚠ 严格大于，不能写成 >= */
            分数[j + 1] = 分数[j];
            名字[j + 1] = 名字[j];
            j--;
        }
        分数[j + 1] = 拿着分;
        名字[j + 1] = 拿着名;
    }
}

// 2. 不稳定排序：选择排序（每轮一次远距离交换）
//    那个「远距离交换」就是罪魁祸首：它会把一个元素从很后面直接甩到前面，
//    路上所有和它相等的人就被它跨过去了。
static void 按分数不稳定排序(int[] 分数, String[] 名字) {
    for (int i = 0; i < 分数.length - 1; i++) {
        int 最小 = i;
        for (int j = i + 1; j < 分数.length; j++) {
            if (分数[j] < 分数[最小]) {
                最小 = j;
            }
        }
        if (最小 != i) {
            int 临时分 = 分数[i];
            分数[i] = 分数[最小];
            分数[最小] = 临时分;

            String 临时名 = 名字[i];
            名字[i] = 名字[最小];
            名字[最小] = 临时名;
        }
    }
}

// 3. 两级比较器：先长度，再字典序
//    ⚠ 返回值的**符号**才是关键：负数=排前面，0=一样大，正数=排后面。
//      两级都要照顾到 —— 只写第一级的话，长度相同的那些会乱序。
static Comparator<String> 按长度再按字典() {
    return (a, b) -> {
        if (a.length() != b.length()) {
            return a.length() - b.length();
        }
        return a.compareTo(b);        /* ⚠ 比先后用 compareTo，不是 ==，也不是 equals */
    };
}


    public static void main(String[] args) {

        int[] 分数 = {90, 85, 90, 80};
        String[] 名字 = {"小明", "小红", "小刚", "小美"};
        System.out.println("原始名|" + String.join(" ", 名字));

        int[] 分数2 = {90, 85, 90, 80};
        String[] 名字2 = {"小明", "小红", "小刚", "小美"};
        按分数稳定排序(分数2, 名字2);
        System.out.println("稳定分|" + Arrays.toString(分数2));
        System.out.println("稳定名|" + String.join(" ", 名字2));

        int[] 分数3 = {90, 85, 90, 80};
        String[] 名字3 = {"小明", "小红", "小刚", "小美"};
        按分数不稳定排序(分数3, 名字3);
        System.out.println("不稳分|" + Arrays.toString(分数3));
        System.out.println("不稳名|" + String.join(" ", 名字3));

        ArrayList<String> 词 = new ArrayList<>(Arrays.asList("java", "c", "abc", "ab", "zz"));
        词.sort(按长度再按字典());
        System.out.println("比较器|" + 词);

    }
}
