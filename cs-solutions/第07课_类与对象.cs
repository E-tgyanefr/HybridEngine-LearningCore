// 第 07 课参考答案
class 计数器
{
    // 只读属性：外面**改不了**，唯一的入口是下面的方法
    public int 值 { get; private set; }

    public 计数器(int 初值 = 0)   // 默认参数 ⇒ new 计数器() 也行
    {
        值 = 初值;
    }

    public void 加(int n) => 值 += n;
    public void 重置() => 值 = 0;
}

class 银行账户
{
    public int 余额 { get; private set; }

    public 银行账户(int 初值 = 0)
    {
        余额 = 初值;
    }

    public void 存(int n)
    {
        if (n <= 0) return;
        余额 += n;
    }

    public bool 取(int n)
    {
        // ⚠ 顺序很重要：**先检查，通过才扣**。
        //   反过来的话，一次失败的取款已经把余额扣掉了。
        if (n <= 0 || n > 余额) return false;
        余额 -= n;
        return true;
    }
}
