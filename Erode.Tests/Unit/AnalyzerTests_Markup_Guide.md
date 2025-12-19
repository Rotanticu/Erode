# Roslyn Analyzer 测试中使用 Markup 语法指南

## 📋 目录

1. [背景](#背景)
2. [遇到的问题](#遇到的问题)
3. [解决方案](#解决方案)
4. [Markup 语法详解](#markup-语法详解)
5. [最佳实践](#最佳实践)
6. [常见错误和解决方法](#常见错误和解决方法)
7. [参考资源](#参考资源)

---

## 背景

在编写 Roslyn Analyzer 的单元测试时，传统方法需要硬编码诊断的行号和列号：

```csharp
test.ExpectedDiagnostics.Add(
    Verify.Diagnostic("ERODE007")
        .WithLocation(17, 9)  // 硬编码行号和列号
        .WithArguments("WrongType"));
```

这种方法的问题：
- **脆弱性**：代码格式变化（添加/删除空行、注释）会导致所有测试失败
- **可读性差**：`(17, 9)` 无法直观看出错误位置
- **维护困难**：需要手动计算和更新行号

**解决方案**：使用 Roslyn 测试框架的 Markup 语法，直接在代码中标记诊断位置。

---

## 遇到的问题

### 1. 文档与 API 不匹配

#### 问题描述

尝试使用文档中提到的 `MarkupOptions.AllowEnabled` 属性，但编译时发现该属性不存在。

```csharp
// ❌ 错误：编译失败
var test = new CSharpAnalyzerTest<GenerateEventAnalyzer, DefaultVerifier>
{
    TestCode = fullTestCode,
    MarkupOptions = MarkupOptions.AllowEnabled  // CS0117: "MarkupOptions"未包含"AllowEnabled"的定义
};
```

#### 原因

- 文档可能基于不同版本的包
- 某些属性可能已被移除或重命名
- Markup 语法实际上默认启用，无需额外配置

#### 解决方案

```csharp
// ✅ 正确：Markup 语法默认启用
var test = new CSharpAnalyzerTest<GenerateEventAnalyzer, DefaultVerifier>
{
    TestCode = fullTestCode
    // 无需配置 MarkupOptions
};
```

---

### 2. Markup 语法格式混乱

#### 问题描述

文档和示例中提到了多种 Markup 语法格式，但实际使用时只有部分格式有效。

#### 尝试过的格式

**格式 1：`[|...|]`（失败）**
```csharp
var testCode = @"
    [|public void PublishTestEvent(int a) { }|]
";
// ❌ 错误：Markup syntax can only omit the diagnostic ID if the first analyzer only supports a single diagnostic
```

**格式 2：`{|DIAGNOSTIC_ID:...|}`（失败）**
```csharp
var testCode = @"
    {|ERODE007:public void PublishTestEvent(int a) { }|}
";
// ❌ 错误：The markup location '#0' was not found in the input
```

**格式 3：`{|#0:...|}`（成功）**
```csharp
var testCode = @"
    {|#0:public void PublishTestEvent(int a) { }|}
";
// ✅ 成功
```

#### 原因

- `[|...|]` 只能用于 Analyzer 只支持单个诊断的情况
- `{|DIAGNOSTIC_ID:...|}` 创建的是命名标记，但 `WithLocation(0)` 期望的是编号标记
- `{|#0:...|}` 创建编号标记，与 `WithLocation(0)` 匹配

---

### 3. 行号定位的复杂性

#### 问题描述

诊断位置需要精确匹配，包括行号和列号。代码合并导致行号偏移，诊断位置可能跨越多行。

#### 具体困难

**代码合并导致行号偏移：**

```csharp
// 原始测试代码（第 1 行开始）
var testCode = @"namespace Test
{
    public class WrongType 
    { 
        [Erode.GenerateEvent]
        public void PublishTestEvent(int a) { } 
    }
}";

// 合并后的代码（添加了 GenerateEventAttribute 定义）
var fullTestCode = @"namespace Erode
{
    [System.AttributeUsage(...)]
    public sealed class GenerateEventAttribute : System.Attribute
    {
        public GenerateEventAttribute() { }
    }
}

" + testCode;  // 原来的第 1 行变成了第 13 行
```

**诊断位置跨越多行：**

```csharp
// ❌ 错误：只标记方法行
{|#0:public void PublishTestEvent(int a) { }|}
// 期望位置：(17, 9, 17, 48)
// 实际位置：(16, 9, 17, 48)  // 包括特性行

// ✅ 正确：标记整个范围（特性行 + 方法行）
{|#0:[Erode.GenerateEvent]
        public void PublishTestEvent(int a) { }|}
```

#### 解决方案

1. **标记整个诊断范围**：包括特性行和方法行
2. **使用 Markup 语法**：让框架自动计算位置，避免手动计算行号
3. **测试时验证**：运行测试查看实际位置，然后调整标记

---

### 4. 多个诊断的处理

#### 问题描述

一个测试可能有多个诊断（编译器错误 + Analyzer 诊断），需要正确使用 `#0`, `#1` 等标记。

#### 错误示例

```csharp
// ❌ 错误：嵌套标记导致代码重复
{|#0:[Erode.GenerateEvent]
    {|#1:static|} void PublishTestEvent(int a);|}
// 结果：代码被重复，导致编译错误
```

#### 正确方式

```csharp
// ✅ 正确：为每个诊断使用独立的标记
var testCode = @"
public interface ITestEvents
{
    {|#0:[Erode.GenerateEvent]
    static void PublishTestEvent(int a);|}  // ERODE010
}";

test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE010").WithLocation(0));
// 注意：CS0501 编译器错误可能不会在 Analyzer 测试中报告
```

---

## 解决方案

### 完整的测试示例

```csharp
[Fact]
public async Task ClassWithAttribute_ShouldTriggerDiagnostic()
{
    var testCode = @"namespace Test
{
    public class WrongType 
    { 
        {|#0:[Erode.GenerateEvent] // ❌ 错误：不能在 class 的方法上使用
        public void PublishTestEvent(int a) { }|}
    }
}";

    var test = CreateTest(testCode);
    test.ExpectedDiagnostics.Add(
        Verify.Diagnostic("ERODE007")
            .WithLocation(0)  // 使用编号标记，而不是行号
            .WithArguments("WrongType"));
    await test.RunAsync();
}
```

### CreateTest 方法

```csharp
private static CSharpAnalyzerTest<GenerateEventAnalyzer, DefaultVerifier> CreateTest(string testCode)
{
    // 将 GenerateEventAttribute 定义内联到测试代码中
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
        // Markup 语法默认启用，无需额外配置
    };
    return test;
}
```

---

## Markup 语法详解

### 基本语法

| 语法 | 用途 | 示例 |
|------|------|------|
| `[|...|]` | 默认诊断（仅当 Analyzer 支持单个诊断时） | `[|int x = 0;|]` |
| `{|DIAGNOSTIC_ID:...|}` | 指定诊断 ID（创建命名标记） | `{|ERODE007:code|}` |
| `{|#0:...|}` | 编号标记（与 `WithLocation(0)` 匹配） | `{|#0:code|}` |
| `{|#1:...|}` | 第二个标记（与 `WithLocation(1)` 匹配） | `{|#1:code|}` |

### 标记范围

**重要**：标记应该包含诊断的完整范围，通常包括：
- 特性行（如果有）
- 方法/类型声明行
- 可能跨越多行

```csharp
// ✅ 正确：标记整个范围
{|#0:[Erode.GenerateEvent]
    public void PublishTestEvent(int a) { }|}

// ❌ 错误：只标记部分范围
{|#0:public void PublishTestEvent(int a) { }|}  // 缺少特性行
```

### 多个诊断

```csharp
var testCode = @"
public interface ITestEvents
{
    {|#0:[Erode.GenerateEvent]
    static void PublishTestEvent(int a);|}  // 第一个诊断
}";

test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE010").WithLocation(0));
test.ExpectedDiagnostics.Add(
    new DiagnosticResult("CS0501", DiagnosticSeverity.Error)
        .WithLocation(1)  // 第二个诊断
        .WithArguments("Test.ITestEvents.PublishTestEvent(int)"));
```

---

## 最佳实践

### 1. 使用编号标记 `{|#0:...|}`

**推荐**：使用编号标记，因为它：
- 与 `WithLocation(0)`, `WithLocation(1)` 直接对应
- 不依赖诊断 ID
- 支持多个诊断

```csharp
// ✅ 推荐
{|#0:public void PublishTestEvent(int a) { }|}
test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE007").WithLocation(0));
```

### 2. 标记完整的诊断范围

**推荐**：包括特性行和方法行

```csharp
// ✅ 推荐
{|#0:[Erode.GenerateEvent]
    public void PublishTestEvent(int a) { }|}
```

### 3. 避免嵌套标记

**不推荐**：嵌套标记会导致代码重复

```csharp
// ❌ 不推荐
{|#0:[Erode.GenerateEvent]
    {|#1:static|} void PublishTestEvent(int a);|}
```

### 4. 处理编译器错误

**注意**：编译器错误可能不会在 Analyzer 测试中报告，因为：
- 编译器在 Analyzer 之前运行
- 某些编译器错误会阻止 Analyzer 运行
- 需要根据实际情况决定是否添加编译器错误的期望

```csharp
// 如果 Analyzer 仍然运行并报告诊断，可以只检查 Analyzer 诊断
test.ExpectedDiagnostics.Add(Verify.Diagnostic("ERODE010").WithLocation(0));
// 不添加 CS0501，因为编译器错误可能不会报告
```

### 5. 代码合并策略

**推荐**：将依赖定义内联到测试代码中，避免多文件问题

```csharp
var fullTestCode = attributeDefinition + "\n" + testCode;
// 而不是使用 test.TestState.Sources.Add()
```

---

## 常见错误和解决方法

### 错误 1：`The markup location '#0' was not found in the input`

**原因**：
- 使用了 `{|DIAGNOSTIC_ID:...|}` 而不是 `{|#0:...|}`
- 标记格式不正确

**解决方法**：
```csharp
// ❌ 错误
{|ERODE007:code|}

// ✅ 正确
{|#0:code|}
```

### 错误 2：`Markup syntax can only omit the diagnostic ID if the first analyzer only supports a single diagnostic`

**原因**：
- 使用了 `[|...|]` 语法，但 Analyzer 支持多个诊断

**解决方法**：
```csharp
// ❌ 错误
[|code|]

// ✅ 正确
{|#0:code|}
```

### 错误 3：`Expected diagnostic to start on line "X" was actually on line "Y"`

**原因**：
- 标记范围不正确
- 只标记了部分诊断范围

**解决方法**：
```csharp
// ❌ 错误：只标记方法行
{|#0:public void PublishTestEvent(int a) { }|}

// ✅ 正确：标记整个范围（包括特性行）
{|#0:[Erode.GenerateEvent]
    public void PublishTestEvent(int a) { }|}
```

### 错误 4：`Mismatch between number of diagnostics returned`

**原因**：
- 期望的诊断数量与实际不符
- 编译器错误没有正确添加

**解决方法**：
```csharp
// 检查实际返回的诊断
// 运行测试查看 "Actual diagnostic" 部分
// 添加所有实际返回的诊断到 ExpectedDiagnostics
```

### 错误 5：代码重复或语法错误

**原因**：
- 标记嵌套或格式错误
- 标记没有正确闭合

**解决方法**：
```csharp
// ❌ 错误：嵌套标记
{|#0:code1 {|#1:code2|} code3|}

// ✅ 正确：独立标记
{|#0:code1|}
{|#1:code2|}
```

---

## 参考资源

### 官方文档

1. **Microsoft Learn - 编写分析器和代码修复**
   - https://learn.microsoft.com/zh-cn/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix

2. **Roslyn 测试框架源码**
   - https://github.com/dotnet/roslyn-sdk
   - 查看 `Microsoft.CodeAnalysis.Testing` 命名空间

### 有用的工具

1. **调试技巧**
   ```csharp
   // 在测试中添加断点，查看实际返回的诊断
   var actualDiagnostics = await test.GetDiagnosticsAsync();
   ```

2. **最小示例测试**
   ```csharp
   // 先写一个最简单的测试，验证语法
   [Fact]
   public async Task SimpleTest()
   {
       var testCode = @"
       class Program
       {
           {|#0:void M()|}
       }";
       
       var test = CreateTest(testCode);
       test.ExpectedDiagnostics.Add(Verify.Diagnostic("YOUR_DIAGNOSTIC").WithLocation(0));
       await test.RunAsync();
   }
   ```

### 包版本信息和弃用问题 ⚠️

#### 当前使用的包

当前项目使用的包版本：
- `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` Version="1.1.2"
- `Microsoft.CodeAnalysis.Testing.Verifiers.XUnit` Version="1.1.2"

#### ⚠️ 弃用警告

**重要发现**：以下包已被标记为**旧版/不推荐使用**：

| 包名 | 状态 | 说明 |
|------|------|------|
| `Microsoft.CodeAnalysis.CSharp.Testing` | ❌ 旧版/不推荐 | 已被新包替代 |
| `Microsoft.CodeAnalysis.CSharp.Testing.XUnit` | ❌ 旧版/不推荐 | 已被新包替代 |
| `Microsoft.CodeAnalysis.SourceGenerators.Testing.XUnit` | ❌ 旧版/不推荐 | 已被新包替代 |

#### 推荐的包（当前使用）

虽然某些包被标记为旧版，但以下包**仍然可用且推荐**：

| 包名 | 状态 | 用途 |
|------|------|------|
| `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` | ✅ 可用 | Analyzer 测试 |
| `Microsoft.CodeAnalysis.Testing.Verifiers.XUnit` | ✅ 可用 | XUnit 验证器 |

#### 包选择建议

1. **避免使用已弃用的包**
   ```xml
   <!-- ❌ 不要使用 -->
   <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Testing" />
   <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Testing.XUnit" />
   ```

2. **使用推荐的包**
   ```xml
   <!-- ✅ 推荐使用 -->
   <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Analyzer.Testing" Version="1.1.2" />
   <PackageReference Include="Microsoft.CodeAnalysis.Testing.Verifiers.XUnit" Version="1.1.2" />
   ```

3. **检查包状态**
   - 在 NuGet 包管理器中查看包的"弃用"警告
   - 查看包的发布说明和迁移指南
   - 关注 GitHub Issues 中的讨论

#### 版本兼容性

**注意**：不同版本的包可能有不同的行为，建议：
- 查看对应版本的文档
- 参考对应版本的示例代码
- 如果遇到问题，查看源码了解实际行为
- **定期检查包更新**：虽然某些包被标记为旧版，但可能有新版本可用

#### 迁移建议

如果将来需要迁移到新包：

1. **查看官方迁移指南**
   - 检查 Roslyn SDK 的 GitHub 仓库
   - 查看最新的文档和示例

2. **逐步迁移**
   - 先在一个测试文件中尝试新包
   - 验证功能是否正常
   - 再逐步迁移其他测试

3. **保持兼容性**
   - 如果当前包仍然可用，不急于迁移
   - 关注官方公告，了解迁移时间表

---

## 总结

### 关键要点

1. ✅ **使用 `{|#0:...|}` 格式**：这是最可靠的方式
2. ✅ **标记完整范围**：包括特性行和方法行
3. ✅ **避免嵌套标记**：每个诊断使用独立的标记
4. ✅ **让框架计算位置**：不要手动计算行号
5. ✅ **逐步测试**：从简单到复杂，逐步验证

### 避免的陷阱

1. ❌ 不要使用 `MarkupOptions.AllowEnabled`（不存在）
2. ❌ 不要使用 `[|...|]`（仅适用于单个诊断）
3. ❌ 不要使用 `{|DIAGNOSTIC_ID:...|}`（与 `WithLocation(0)` 不匹配）
4. ❌ 不要手动计算行号（使用 Markup 语法）
5. ❌ 不要嵌套标记（会导致代码重复）
6. ⚠️ **不要使用已弃用的包**：避免使用 `Microsoft.CodeAnalysis.CSharp.Testing` 等已弃用的包
7. ⚠️ **不要忽略弃用警告**：定期检查包状态，了解是否有替代方案

### 成功的关键

- **耐心**：可能需要多次尝试才能找到正确的格式
- **测试**：运行测试查看实际行为
- **参考源码**：如果文档不清楚，查看框架源码
- **最小示例**：先写最简单的测试，验证语法

---

## 包弃用问题详解 ⚠️

### 问题背景

在使用 Roslyn 测试框架时，可能会遇到 NuGet 包被标记为"旧版/不推荐"的情况。这是一个常见但令人困惑的问题。

### 已弃用的包列表

以下包已被标记为**旧版/不推荐使用**：

| 包名 | 状态 | 替代方案 |
|------|------|----------|
| `Microsoft.CodeAnalysis.CSharp.Testing` | ❌ 旧版 | 使用 `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` |
| `Microsoft.CodeAnalysis.CSharp.Testing.XUnit` | ❌ 旧版 | 使用 `Microsoft.CodeAnalysis.Testing.Verifiers.XUnit` |
| `Microsoft.CodeAnalysis.SourceGenerators.Testing.XUnit` | ❌ 旧版 | 检查是否有新版本或替代包 |

### 为什么会出现弃用警告？

1. **包重构**：Microsoft 可能正在重构测试框架，将功能整合到新包中
2. **命名变更**：包的命名可能发生变化，但功能仍然可用
3. **维护状态**：某些包可能不再积极维护，但功能仍然稳定
4. **版本策略**：可能是版本管理策略的一部分，而不是真正的弃用

### 当前项目的应对策略

**现状**：项目使用以下包，这些包**仍然可用且功能正常**：

```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Analyzer.Testing" Version="1.1.2" />
<PackageReference Include="Microsoft.CodeAnalysis.Testing.Verifiers.XUnit" Version="1.1.2" />
```

**建议**：
1. ✅ **继续使用当前包**：如果功能正常，无需立即迁移
2. ⚠️ **关注更新**：定期检查是否有新版本或替代方案
3. 📝 **记录依赖**：在文档中明确记录使用的包版本
4. 🔍 **监控警告**：如果出现运行时问题，考虑迁移
5. 🚫 **忽略弃用警告**：如果包仍然可用且功能正常，可以暂时忽略警告

### 如何检查包状态

#### 方法 1：在 Visual Studio 中

1. 打开 **NuGet 包管理器**
2. 查看包的"弃用"或"警告"标签
3. 查看包的详细信息页面
4. 检查是否有替代包推荐

#### 方法 2：在 NuGet.org

1. 访问包的 NuGet 页面：https://www.nuget.org/packages/[包名]
2. 查看"弃用"信息（Deprecation）
3. 查看发布说明（Release Notes）
4. 检查是否有替代包链接

#### 方法 3：在 GitHub

1. 查看 Roslyn SDK 仓库：https://github.com/dotnet/roslyn-sdk
2. 搜索 Issues 中关于包弃用的讨论
3. 查看迁移指南（Migration Guide）
4. 检查 README 中的包推荐

### 如何处理弃用警告

#### 选项 1：继续使用（推荐，如果功能正常）

如果当前包仍然可用且功能正常：

```xml
<!-- 继续使用，但记录版本 -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Analyzer.Testing" Version="1.1.2" />
```

**优点**：
- 无需立即修改代码
- 功能稳定，风险低
- 可以继续开发

**缺点**：
- 可能错过新功能
- 未来可能需要迁移

#### 选项 2：迁移到新包（如果可用）

如果找到了新包或替代方案：

1. **研究新包**
   ```bash
   # 搜索新的测试包
   dotnet add package Microsoft.CodeAnalysis.* --prerelease
   ```

2. **创建测试分支**
   - 在单独的分支中测试新包
   - 验证所有测试仍然通过

3. **更新文档**
   - 记录迁移过程
   - 更新本文档

4. **逐步迁移**
   - 不要一次性迁移所有测试
   - 先迁移一个测试文件，验证无误后再继续

#### 选项 3：抑制警告（临时方案）

如果确定包仍然可用，可以抑制警告：

```xml
<PropertyGroup>
  <!-- 抑制弃用警告 -->
  <NoWarn>$(NoWarn);NU1901;NU1902</NoWarn>
</PropertyGroup>
```

**注意**：这不是推荐方案，应该定期检查包状态。

### 迁移检查清单

如果决定迁移到新包，使用以下检查清单：

- [ ] 研究新包的文档和示例
- [ ] 在测试分支中尝试新包
- [ ] 验证所有现有测试仍然通过
- [ ] 检查新包的 API 是否有变化
- [ ] 更新项目文件中的包引用
- [ ] 更新测试代码（如果有 API 变化）
- [ ] 更新文档和注释
- [ ] 在 CI/CD 中验证
- [ ] 通知团队成员

### 常见问题

#### Q: 弃用警告会影响功能吗？

**A**: 通常不会。弃用警告只是提醒，不会影响包的正常使用。但如果包真的被移除，可能会导致问题。

#### Q: 我应该立即迁移吗？

**A**: 不一定。如果当前包仍然可用且功能正常，可以继续使用。但应该定期检查是否有新版本。

#### Q: 如何知道是否有替代包？

**A**: 
1. 查看 NuGet 包的"弃用"信息，通常会推荐替代包
2. 查看 Roslyn SDK 的 GitHub 仓库
3. 搜索相关的 Issues 和讨论

#### Q: 如果包真的被移除了怎么办？

**A**: 
1. 查看是否有替代包
2. 考虑使用其他测试框架
3. 如果必须使用，可以考虑 Fork 包并自行维护（不推荐）

### 最佳实践

1. **定期检查**：每季度检查一次包的状态
2. **记录版本**：在文档中明确记录使用的包版本
3. **监控警告**：关注 NuGet 的弃用警告
4. **保持更新**：如果新版本可用，考虑更新
5. **测试先行**：在迁移前充分测试
6. **文档同步**：及时更新文档和注释

### 问题描述

在 NuGet 包管理器中，可能会看到以下警告：

```
⚠️ Microsoft.CodeAnalysis.CSharp.Testing - 旧版 / 可用但不推荐
⚠️ Microsoft.CodeAnalysis.CSharp.Testing.XUnit - 旧版 / 可用但不推荐
⚠️ Microsoft.CodeAnalysis.SourceGenerators.Testing.XUnit - 旧版 / 可用但不推荐
```

### 为什么会出现弃用警告？

1. **包重构**：Microsoft 可能正在重构测试框架，将功能整合到新包中
2. **命名变更**：包的命名可能发生变化，但功能仍然可用
3. **维护状态**：某些包可能不再积极维护，但功能仍然稳定

### 当前项目的应对策略

**现状**：项目使用 `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` 和 `Microsoft.CodeAnalysis.Testing.Verifiers.XUnit`，这些包**仍然可用且功能正常**。

**建议**：
1. ✅ **继续使用当前包**：如果功能正常，无需立即迁移
2. ⚠️ **关注更新**：定期检查是否有新版本或替代方案
3. 📝 **记录依赖**：在文档中明确记录使用的包版本
4. 🔍 **监控警告**：如果出现运行时问题，考虑迁移

### 如何检查包状态

1. **在 Visual Studio 中**
   - 打开 NuGet 包管理器
   - 查看包的"弃用"或"警告"标签
   - 查看包的详细信息页面

2. **在 NuGet.org**
   - 访问包的 NuGet 页面
   - 查看"弃用"信息
   - 查看发布说明

3. **在 GitHub**
   - 查看 Roslyn SDK 仓库的 Issues
   - 搜索关于包弃用的讨论
   - 查看迁移指南

### 如果必须迁移

如果将来需要迁移到新包，建议步骤：

1. **研究新包**
   ```bash
   # 搜索新的测试包
   dotnet add package Microsoft.CodeAnalysis.* --prerelease
   ```

2. **创建测试分支**
   - 在单独的分支中测试新包
   - 验证所有测试仍然通过

3. **更新文档**
   - 记录迁移过程
   - 更新本文档

4. **逐步迁移**
   - 不要一次性迁移所有测试
   - 先迁移一个测试文件，验证无误后再继续

---

## 更新日志

- **2024-XX-XX**：初始版本，记录从硬编码行号迁移到 Markup 语法的过程
- 包含所有遇到的问题、解决方案和最佳实践
- **2024-XX-XX**：添加包弃用问题的详细说明和应对策略

---

**希望这份文档能帮助后来者避免踩坑！** 🎯

