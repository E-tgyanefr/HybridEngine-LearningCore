// 第 08 课参考答案
interface 图形
{
    double 面积();
    string 名字();
}

class 圆 : 图形
{
    private readonly double 半径;
    public 圆(double 半径) { this.半径 = 半径; }
    public double 面积() => 3.14159 * 半径 * 半径;
    public string 名字() => "圆";
}

class 矩形 : 图形
{
    protected readonly double 宽, 高;
    public 矩形(double 宽, double 高) { this.宽 = 宽; this.高 = 高; }
    public virtual double 面积() => 宽 * 高;
    public virtual string 名字() => "矩形";
}

class 正方形 : 矩形
{
    // 正方形**就是**一个宽高相等的矩形 ⇒ 复用它的面积计算
    public 正方形(double 边长) : base(边长, 边长) { }
    public override string 名字() => "正方形";
}

// ⚠ 注意这个方法：它**不知道**拿到的是圆还是矩形
static string 报告(List<图形> 们)
{
    var sb = new System.Text.StringBuilder();
    foreach (var g in 们)
        sb.AppendLine($"{g.名字()} 面积={g.面积():F2}");
    return sb.ToString().TrimEnd();
}
