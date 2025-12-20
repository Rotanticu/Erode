using Erode.Generator;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Erode.Tests.Unit;

using Verify = CSharpAnalyzerVerifier<
    Erode.Generator.GenerateEventAnalyzer,
    DefaultVerifier>;

/// <summary>
/// Analyzer 编译时错误验证测试
/// 使用 Microsoft.CodeAnalysis.CSharp.Analyzer.Testing 框架验证 Analyzer 是否正确捕获各种错误情况
/// </summary>
public class AnalyzerTests
{
    /// <summary>
    /// 创建配置好的测试实例，自动添加 GenerateEventAttribute 定义
    /// </summary>
    private static CSharpAnalyzerTest<GenerateEventAnalyzer, DefaultVerifier> CreateTest(string testCode)
    {
        // 将 GenerateEventAttribute 定义内联到测试代码中，避免多文件问题
        var fullTestCode = @"namespace Erode
{
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class GenerateEventAttribute : System.Attribute
    {
        public GenerateEventAttribute()
        {
        }
    }
}

" + testCode;

        var test = new CSharpAnalyzerTest<GenerateEventAnalyzer, DefaultVerifier>
        {
            TestCode = fullTestCode
        };
        return test;
    }
    /// <summary>
    /// 测试：非接口类型使用 [GenerateEvent] 应该触发 ERODE007 错误
    /// </summary>
    [Fact]
    public async Task ClassWithAttribute_ShouldTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{
    public class WrongType 
    { 
        {|#0:[Erode.GenerateEvent] // ❌ 错误：不能在 class 的方法上使用（应该在接口方法上）
        public void PublishTestEvent(int a) { }|} 
    }
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE007").WithLocation(0).WithArguments("WrongType"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：非 void 方法使用 [GenerateEvent] 应该触发 ERODE002 错误
    /// </summary>
    [Fact]
    public async Task NonVoidMethod_ShouldTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{
    public interface ITestEvents
    {
        {|#0:[Erode.GenerateEvent]
        int PublishTestEvent(int a);|} // ❌ 错误：返回类型不是 void
    }
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE002").WithLocation(0).WithArguments("PublishTestEvent", "int"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：接口方法使用 static 应该触发 ERODE010 错误
    /// </summary>
    [Fact]
    public async Task StaticInterfaceMethod_ShouldTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{

public interface ITestEvents
{
    {|#0:[Erode.GenerateEvent]
    static void PublishTestEvent(int a);|} // ❌ 错误：接口方法不能是 static
}
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE010").WithLocation(0).WithArguments("PublishTestEvent"));
        test.ExpectedDiagnostics.Add(new Microsoft.CodeAnalysis.Testing.DiagnosticResult("CS0501", Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .WithSpan(18, 17, 18, 33)
            .WithArguments("Test.ITestEvents.PublishTestEvent(int)"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：参数使用 ref 应该触发 ERODE012 错误
    /// </summary>
    [Fact]
    public async Task ParameterWithRef_ShouldTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{

public interface ITestEvents
{
    {|#0:[Erode.GenerateEvent]
    void PublishTestEvent(ref int a);|} // ❌ 错误：参数不能使用 ref
}
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE012").WithLocation(0).WithArguments("PublishTestEvent", "a"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：参数使用 out 应该触发 ERODE012 错误
    /// </summary>
    [Fact]
    public async Task ParameterWithOut_ShouldTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{

public interface ITestEvents
{
    {|#0:[Erode.GenerateEvent]
    void PublishTestEvent(out int a);|} // ❌ 错误：参数不能使用 out
}
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE012").WithLocation(0).WithArguments("PublishTestEvent", "a"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：接口包含泛型参数应该触发 ERODE013 错误
    /// </summary>
    [Fact]
    public async Task InterfaceWithGenericParameters_ShouldTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{

public interface ITestEvents<T> // ❌ 错误：接口不能包含泛型参数
{
    {|#0:[Erode.GenerateEvent]
    void PublishTestEvent(int a);|}
}
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE013").WithLocation(0).WithArguments("ITestEvents"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：方法名不以 Publish 开头应该触发 ERODE011 错误
    /// </summary>
    [Fact]
    public async Task MethodNameNotStartingWithPublish_ShouldTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{

public interface ITestEvents
{
    {|#0:[Erode.GenerateEvent]
    void TestEvent(int a);|} // ❌ 错误：方法名必须以 Publish 开头
}
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE011").WithLocation(0).WithArguments("TestEvent", "方法名必须以 'Publish' 开头"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：方法名不以 Event 结尾应该触发 ERODE011 错误
    /// </summary>
    [Fact]
    public async Task MethodNameNotEndingWithEvent_ShouldTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{

public interface ITestEvents
{
    {|#0:[Erode.GenerateEvent]
    void PublishTest(int a);|} // ❌ 错误：方法名必须以 Event 结尾
}
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE011").WithLocation(0).WithArguments("PublishTest", "方法名必须以 'Event' 结尾"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：方法名为 PublishEvent 应该触发 ERODE011 错误（中间必须有其他字符）
    /// </summary>
    [Fact]
    public async Task MethodNamePublishEvent_ShouldTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{

public interface ITestEvents
{
    {|#0:[Erode.GenerateEvent]
    void PublishEvent(int a);|} // ❌ 错误：方法名不能只有 Publish 和 Event
}
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE011").WithLocation(0).WithArguments("PublishEvent", "方法名不能只有 'Publish' 和 'Event'，中间必须有其他字符"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：方法名包含多个 Publish 应该触发 ERODE011 错误
    /// </summary>
    [Fact]
    public async Task MethodNameWithMultiplePublish_ShouldTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{

public interface ITestEvents
{
    {|#0:[Erode.GenerateEvent]
    void PublishPublishTestEvent(int a);|} // ❌ 错误：方法名中不能有多个 Publish
}
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE011").WithLocation(0).WithArguments("PublishPublishTestEvent", "方法名中不能有多个 'Publish'（只能有一个在开头）"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：方法名包含多个 Event 应该触发 ERODE011 错误
    /// </summary>
    [Fact]
    public async Task MethodNameWithMultipleEvent_ShouldTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{

public interface ITestEvents
{
    {|#0:[Erode.GenerateEvent]
    void PublishTestEventEvent(int a);|} // ❌ 错误：方法名中不能有多个 Event
}
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE011").WithLocation(0).WithArguments("PublishTestEventEvent", "方法名中不能有多个 'Event'（只能有一个在结尾）"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：事件名是 C# 关键字应该触发 ERODE101 警告
    /// </summary>
    [Fact]
    public async Task KeywordEventName_ShouldTriggerWarning()
    {
        var testCode = @"namespace Test
{

public interface ITestEvents
{
    {|#0:[Erode.GenerateEvent]
    void PublishClassEvent(int a);|} // ⚠️ 警告：事件名 'ClassEvent' 中的 'Class' 是 C# 关键字
}
}";

        var test = CreateTest(testCode);
        test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE101").WithLocation(0).WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning).WithArguments("@class"));
        await test.RunAsync();
    }

    /// <summary>
    /// 测试：正确使用 [GenerateEvent] 不应该触发任何错误
    /// </summary>
    [Fact]
    public async Task ValidUsage_ShouldNotTriggerDiagnostic()
    {
        var testCode = @"namespace Test
{

public interface ITestEvents
{
    [Erode.GenerateEvent]
    void PublishTestEvent(int a);
}
}";

        var test = CreateTest(testCode);
        await test.RunAsync();
    }
}
