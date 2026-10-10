// HybridEngineLight.Safe —— 让新手第一段代码不必写 try/catch
//
// 为什么需要它：新手第一课就教 try/catch 是反教育的 ——
// 那是「我知道这里可能出错」之后才需要的工具，而不是起点。
//
// 用法：
//     Safe.Run(() => {
//         var s = new Stage();
//         s.Dot(50, 50, r: 20, color: "红");
//         s.Wait();
//     });
//
// 出错时会打印人话解释（并保留原始报错），而不是一串调用栈。

using System;

namespace HybridEngineLight;

public static class Safe
{
    /// <summary>跑一段代码；出错就打印人话解释，不抛异常。</summary>
    /// <returns>成功返回 true。</returns>
    public static bool Run(Action body)
    {
        try
        {
            body();
            return true;
        }
        catch (Exception ex)
        {
            Console.Write(Explain.For(ex));
            return false;
        }
    }

    /// <summary>跑一段返回值的代码；出错就打印人话解释并返回默认值。</summary>
    public static T? Run<T>(Func<T> body)
    {
        try
        {
            return body();
        }
        catch (Exception ex)
        {
            Console.Write(Explain.For(ex));
            return default;
        }
    }
}
