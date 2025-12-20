using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Erode.Generator;

[Generator]
public class EventGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {

        // 2. 提取 Attribute Symbol（缓存友好）
        var attrProvider = context.CompilationProvider
            .Select((compilation, _) => compilation.GetTypeByMetadataName("Erode.GenerateEventAttribute"));

        // 3. 语法分析层：查找接口中的方法声明
        var methodDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) =>
                {
                    if (node is not MethodDeclarationSyntax method || method.AttributeLists.Count == 0)
                        return false;

                    // 检查父节点是否是接口
                    if (method.Parent is not InterfaceDeclarationSyntax)
                        return false;

                    // 在语法层检查是否有 [GenerateEvent] 特性（减少无效节点）
                    foreach (var attributeList in method.AttributeLists)
                    {
                        foreach (var attribute in attributeList.Attributes)
                        {
                            var name = attribute.Name.ToString();
                            if (name == "GenerateEvent" ||
                                name == "GenerateEventAttribute" ||
                                name.EndsWith(".GenerateEvent") ||
                                name.EndsWith(".GenerateEventAttribute"))
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                },
                transform: static (ctx, _) => (MethodDeclarationSyntax)ctx.Node);

        // 4. 语义分析层：将 Symbol 信息转换为 EventData POCO
        var eventData = methodDeclarations
            .Combine(context.CompilationProvider)
            .Combine(attrProvider)
            .Select((tuple, ct) =>
            {
                var ((methodSyntax, compilation), attrSymbol) = tuple;

                // 获取语义模型（仅在此阶段使用）
                var semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(methodSyntax) is not IMethodSymbol methodSymbol)
                    return new EventData { IsValid = false };

                var containingType = methodSymbol.ContainingType;

                // 检查是否是接口
                if (containingType.TypeKind != TypeKind.Interface)
                    return new EventData { IsValid = false };

                // 检查是否有 GenerateEvent 特性
                if (attrSymbol == null)
                    return new EventData { IsValid = false };

                var hasAttribute = methodSymbol.GetAttributes()
                    .Any(attr => attr.AttributeClass?.Equals(attrSymbol, SymbolEqualityComparer.Default) == true);

                if (!hasAttribute)
                    return new EventData { IsValid = false };

                // 提取所有需要的信息到 POCO
                var location = methodSyntax.GetLocation();

                // 验证接口
                var interfaceValidation = EventValidationHelper.ValidateInterface(containingType);
                if (!interfaceValidation.IsValid)
                {
                    return new EventData
                    {
                        IsValid = false,
                        ErrorType = interfaceValidation.ErrorType,
                        MethodName = methodSymbol.Name,
                        InterfaceName = containingType.Name ?? string.Empty,
                        Location = location
                    };
                }

                // 验证接口方法
                var methodValidation = EventValidationHelper.ValidateInterfaceMethod(methodSymbol);
                if (!methodValidation.IsValid)
                {
                    return new EventData
                    {
                        IsValid = false,
                        ErrorType = methodValidation.ErrorType,
                        MethodName = methodValidation.MethodName ?? methodSymbol.Name,
                        ReturnType = methodValidation.ReturnType,
                        ParameterName = methodValidation.ParameterName,
                        Location = location
                    };
                }

                // 验证和规范化事件名
                var eventNameValidation = EventValidationHelper.ValidateAndNormalizeEventName(
                    methodSymbol.Name, compilation, out var isKeyword);

                if (!eventNameValidation.IsValid)
                {
                    return new EventData
                    {
                        IsValid = false,
                        ErrorType = eventNameValidation.ErrorType,
                        MethodName = methodSymbol.Name,
                        FormatErrorReason = eventNameValidation.FormatErrorReason,
                        Location = location
                    };
                }

                var normalizedEventName = eventNameValidation.NormalizedEventName!;

                // 收集参数信息
                var parameters = new List<EventParameter>();
                var paramIndex = 0;

                foreach (var param in methodSymbol.Parameters)
                {
                    var originalName = param.Name ?? $"param{paramIndex}";
                    var safeName = originalName;

                    if (string.IsNullOrWhiteSpace(originalName))
                        safeName = $"param{paramIndex}";

                    // 规范化参数名（处理关键字等）
                    safeName = EventValidationHelper.NormalizeParameterName(safeName);

                    // 生成属性名（PascalCase，用于 record struct）
                    var propertyName = EventValidationHelper.GeneratePropertyName(safeName);

                    parameters.Add(new EventParameter
                    {
                        Name = safeName, // camelCase，用于方法参数
                        PropertyName = propertyName, // PascalCase，用于 record struct 属性
                        Type = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    });

                    paramIndex++;
                }

                var originalMethodName = methodSymbol.Name;
                var standardMethodName = $"Publish{normalizedEventName}";

                // 提取接口信息
                var interfaceName = containingType.Name ?? string.Empty;
                var implementationClassName = EventValidationHelper.ExtractImplementationClassName(interfaceName);
                var interfaceNamespace = containingType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
                var interfaceAccessibility = containingType.DeclaredAccessibility;

                return new EventData
                {
                    IsValid = true,
                    EventName = normalizedEventName,
                    StandardMethodName = standardMethodName,
                    OriginalMethodName = originalMethodName,
                    Parameters = parameters,
                    Location = location,
                    IsKeyword = isKeyword,
                    OriginalEventName = eventNameValidation.OriginalEventName ?? string.Empty,
                    InterfaceName = interfaceName,
                    ImplementationClassName = implementationClassName,
                    InterfaceNamespace = interfaceNamespace,
                    InterfaceAccessibility = interfaceAccessibility
                };
            });

        // 5. 生成层：基于 EventData POCO 生成代码
        context.RegisterSourceOutput(
            eventData.Collect(),
            (spc, events) =>
            {
                if (events.IsDefaultOrEmpty)
                    return;

                // 分离有效和无效事件
                var validEvents = events.Where(e => e.IsValid).ToImmutableArray();
                var invalidEvents = events.Where(e => !e.IsValid).ToImmutableArray();

                // 报告错误诊断
                ReportDiagnostics(spc, invalidEvents);

                if (validEvents.IsDefaultOrEmpty)
                    return;

                // 使用 HashSet 优化重复事件名检测（按命名空间分组）
                var seenEvents = new HashSet<(string Namespace, string EventName)>();
                foreach (var evt in validEvents)
                {
                    var key = (evt.InterfaceNamespace ?? string.Empty, evt.EventName);
                    if (!seenEvents.Add(key) && evt.Location != null)
                    {
                        // 发现重复事件名
                        var diagnostic = Diagnostic.Create(
                            EventDiagnostics.Error_DuplicateEventName,
                            evt.Location,
                            evt.EventName,
                            evt.InterfaceNamespace ?? string.Empty);
                        spc.ReportDiagnostic(diagnostic);
                    }
                }

                // 按接口分组事件
                var eventsByInterface = validEvents
                    .GroupBy(e => new
                    {
                        InterfaceName = e.InterfaceName ?? string.Empty,
                        InterfaceNamespace = e.InterfaceNamespace ?? string.Empty,
                        e.InterfaceAccessibility
                    })
                    .Select(g => new InterfaceEventGroup
                    {
                        InterfaceName = g.Key.InterfaceName,
                        InterfaceNamespace = g.Key.InterfaceNamespace,
                        InterfaceAccessibility = g.Key.InterfaceAccessibility,
                        ImplementationClassName = g.First().ImplementationClassName ?? string.Empty,
                        Events = g.Select(e => new GeneratedEvent
                        {
                            EventName = e.EventName,
                            MethodName = e.StandardMethodName,
                            OriginalMethodName = e.OriginalMethodName,
                            Parameters = e.Parameters,
                            IsKeyword = e.IsKeyword,
                            OriginalEventName = e.OriginalEventName,
                            InterfaceName = e.InterfaceName ?? string.Empty,
                            ImplementationClassName = e.ImplementationClassName ?? string.Empty,
                            InterfaceNamespace = e.InterfaceNamespace ?? string.Empty,
                            InterfaceAccessibility = e.InterfaceAccessibility
                        }).ToImmutableArray()
                    })
                    .ToImmutableArray();

                // 为每个接口生成文件
                foreach (var interfaceGroup in eventsByInterface)
                {
                    // 报告警告和信息诊断
                    var interfaceEvents = validEvents
                        .Where(e => e.InterfaceName == interfaceGroup.InterfaceName)
                        .ToImmutableArray();

                    foreach (var item in interfaceEvents)
                    {
                        if (item.IsKeyword && item.Location != null)
                        {
                            var diagnostic = Diagnostic.Create(EventDiagnostics.Warning_KeywordEventName, item.Location, item.OriginalEventName ?? "");
                            spc.ReportDiagnostic(diagnostic);
                        }

                        var infoDiagnostic = Diagnostic.Create(
                            EventDiagnostics.Info_EventGenerated,
                            item.Location ?? Location.None,
                            item.EventName,
                            item.EventName,
                            item.StandardMethodName);
                        spc.ReportDiagnostic(infoDiagnostic);
                    }

                    // 生成接口对应的文件
                    var sourceCode = GenerateInterfaceSource(
                        interfaceGroup.ImplementationClassName,
                        interfaceGroup.InterfaceNamespace,
                        interfaceGroup.InterfaceAccessibility,
                        interfaceGroup.Events);
                    // 使用命名空间和类名组合作为文件名，避免不同命名空间的同名接口冲突
                    var fileName = string.IsNullOrEmpty(interfaceGroup.InterfaceNamespace)
                        ? $"{interfaceGroup.ImplementationClassName}.g.cs"
                        : $"{interfaceGroup.InterfaceNamespace.Replace(".", "_")}_{interfaceGroup.ImplementationClassName}.g.cs";
                    spc.AddSource(fileName, sourceCode);
                }
            });
    }

    /// <summary>
    /// 统一报告诊断信息
    /// </summary>
    private static void ReportDiagnostics(SourceProductionContext spc, ImmutableArray<EventData> invalidEvents)
    {
        foreach (var item in invalidEvents)
        {
            if (item.Location == null || item.MethodName == null)
                continue;

            Diagnostic? diagnostic = null;
            switch (item.ErrorType)
            {
                case "NotInterface":
                    diagnostic = Diagnostic.Create(EventDiagnostics.Error_NotInterface, item.Location, item.InterfaceName ?? "unknown");
                    break;
                case "InterfaceHasGenericParameters":
                    diagnostic = Diagnostic.Create(EventDiagnostics.Error_InterfaceHasGenericParameters, item.Location, item.InterfaceName ?? "unknown");
                    break;
                case "InterfaceMethodCannotBeStatic":
                    diagnostic = Diagnostic.Create(EventDiagnostics.Error_InterfaceMethodCannotBeStatic, item.Location, item.MethodName);
                    break;
                case "NotVoid":
                    diagnostic = Diagnostic.Create(EventDiagnostics.Error_NotVoid, item.Location, item.MethodName, item.ReturnType ?? "unknown");
                    break;
                case "ParameterRefOrOut":
                    diagnostic = Diagnostic.Create(EventDiagnostics.Error_ParameterRefOrOut, item.Location, item.MethodName ?? "unknown", item.ParameterName ?? "unknown");
                    break;
                case "InvalidMethodNameFormat":
                    diagnostic = Diagnostic.Create(
                        EventDiagnostics.Error_InvalidMethodNameFormat,
                        item.Location,
                        item.MethodName ?? "unknown",
                        item.FormatErrorReason ?? "格式无效");
                    break;
            }
            if (diagnostic != null)
            {
                spc.ReportDiagnostic(diagnostic);
            }
        }
    }


    /// <summary>
    /// 生成接口对应的实现类文件（每个接口一个文件）
    /// </summary>
    private static string GenerateInterfaceSource(
        string implementationClassName,
        string interfaceNamespace,
        Accessibility interfaceAccessibility,
        ImmutableArray<GeneratedEvent> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Erode;");
        sb.AppendLine("using System.CodeDom.Compiler;");
        sb.AppendLine("using System.Diagnostics.CodeAnalysis;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine();
        sb.AppendLine($"namespace {interfaceNamespace};");
        sb.AppendLine();

        // 生成事件类
        foreach (var evt in events)
        {
            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// {evt.EventName} 事件");
            sb.AppendLine($"/// </summary>");
            sb.Append($"public readonly record struct {evt.EventName}(");

            // 使用 StringBuilder 直接拼接参数，避免 Select + string.Join
            if (evt.Parameters.Count > 0)
            {
                for (int i = 0; i < evt.Parameters.Count; i++)
                {
                    var param = evt.Parameters[i];
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append($"{param.Type} {param.PropertyName}");
                }
            }

            sb.AppendLine(") : IEvent;");
            sb.AppendLine();
        }

        // 生成实现类（不继承接口，标记为 partial 允许用户扩展）
        var accessibilityStr = interfaceAccessibility == Accessibility.Public ? "public" : "internal";
        sb.AppendLine($"[GeneratedCode(\"Erode.Generator\", \"1.0.0\")]");
        sb.AppendLine("[ExcludeFromCodeCoverage]");
        sb.AppendLine($"{accessibilityStr} static partial class {implementationClassName}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 异常处理钩子。当此类的 handler 抛出异常时，会调用此委托。");
        sb.AppendLine("    /// 参数：(事件对象, 出错的 handler 委托, 异常对象)");
        sb.AppendLine("    /// 如果为 null，异常会被静默吞掉（不抛出，不记录）。");
        sb.AppendLine("    /// 这保证了发布者逻辑的健壮性和零分配（在正常流程下）。");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static System.Action<Erode.IEvent, System.Delegate, System.Exception>? OnException;");
        sb.AppendLine();

        foreach (var evt in events)
        {
            var eventTypeName = evt.EventName;

            // 使用 StringBuilder 直接拼接参数列表
            var paramListSb = new StringBuilder();
            var paramNamesSb = new StringBuilder();
            for (int i = 0; i < evt.Parameters.Count; i++)
            {
                var param = evt.Parameters[i];
                if (i > 0)
                {
                    paramListSb.Append(", ");
                    paramNamesSb.Append(", ");
                }
                // 方法参数使用 Name（camelCase）
                paramListSb.Append($"{param.Type} {param.Name}");
                // 创建事件时也使用 Name（camelCase），因为 record struct 构造函数按位置传递参数
                paramNamesSb.Append(param.Name);
            }
            var paramList = paramListSb.ToString();
            var paramNames = paramNamesSb.ToString();

            var eventCreation = evt.Parameters.Count > 0
                ? $"new {eventTypeName}({paramNames})"
                : $"new {eventTypeName}()";

            // 生成发布方法（实现接口方法签名）
            var standardMethodName = evt.MethodName;
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// 发布 {eventTypeName} 事件");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"    public static void {standardMethodName}({paramList})");
            sb.AppendLine("    {");
            sb.AppendLine($"        var eventData = {eventCreation};");
            sb.AppendLine($"        EventDispatcher<{eventTypeName}>.Publish(in eventData, (evt, handler, ex) =>");
            sb.AppendLine("        {");
            sb.AppendLine("            // 先调用本类的 OnException");
            sb.AppendLine("            OnException?.Invoke(evt, handler, ex);");
            sb.AppendLine("            // 再调用全局的 EventDispatcher.OnException（如果设置了）");
            sb.AppendLine("            Erode.EventDispatcher.OnException?.Invoke(evt, handler, ex);");
            sb.AppendLine("        });");
            sb.AppendLine("    }");
            sb.AppendLine();

            // 如果用户方法名与标准方法名不同，生成用户方法名的方法（inline 别名）
            if (!string.Equals(evt.OriginalMethodName, standardMethodName, StringComparison.Ordinal))
            {
                // 生成方法参数名列表（用于方法调用，使用 camelCase）
                var methodParamNamesSb = new StringBuilder();
                for (int i = 0; i < evt.Parameters.Count; i++)
                {
                    var param = evt.Parameters[i];
                    if (i > 0)
                        methodParamNamesSb.Append(", ");
                    methodParamNamesSb.Append(param.Name);
                }
                var methodParamNames = methodParamNamesSb.ToString();

                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// 发布 {eventTypeName} 事件（用户方法名）");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.AppendLine($"    public static void {evt.OriginalMethodName}({paramList}) => {standardMethodName}({methodParamNames});");
                sb.AppendLine();
            }

            // 生成订阅方法
            var subscribeMethodName = $"Subscribe{eventTypeName}";
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// 订阅 {eventTypeName} 事件（推荐使用，零拷贝）");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public static SubscriptionToken {subscribeMethodName}(InAction<{eventTypeName}> handler)");
            sb.AppendLine("    {");
            sb.AppendLine($"        return EventDispatcher<{eventTypeName}>.Subscribe(handler);");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 事件数据 POCO（不持有 Roslyn 对象，避免内存泄漏）
    /// </summary>
    private sealed class EventData
    {
        public bool IsValid { get; set; }
        public string? ErrorType { get; set; }
        public string? MethodName { get; set; } // 用于错误诊断
        public string? ReturnType { get; set; }
        public string? ParameterName { get; set; }
        public string? FormatErrorReason { get; set; }
        public string? InterfaceName { get; set; }
        public Location? Location { get; set; }

        // 有效事件的数据
        public string EventName { get; set; } = string.Empty;
        public string StandardMethodName { get; set; } = string.Empty; // 标准方法名：Publish{EventName}
        public string OriginalMethodName { get; set; } = string.Empty;
        public List<EventParameter> Parameters { get; set; } = new();
        public bool IsKeyword { get; set; }
        public string OriginalEventName { get; set; } = string.Empty;
        public string ImplementationClassName { get; set; } = string.Empty;
        public string InterfaceNamespace { get; set; } = string.Empty;
        public Accessibility InterfaceAccessibility { get; set; }
    }

    private sealed class GeneratedEvent
    {
        public string EventName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string OriginalMethodName { get; set; } = string.Empty;
        public List<EventParameter> Parameters { get; set; } = new();
        public bool IsKeyword { get; set; }
        public string OriginalEventName { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = string.Empty;
        public string ImplementationClassName { get; set; } = string.Empty;
        public string InterfaceNamespace { get; set; } = string.Empty;
        public Accessibility InterfaceAccessibility { get; set; }
    }

    private sealed class EventParameter
    {
        public string Name { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    private sealed class InterfaceEventGroup
    {
        public string InterfaceName { get; set; } = string.Empty;
        public string InterfaceNamespace { get; set; } = string.Empty;
        public Accessibility InterfaceAccessibility { get; set; }
        public string ImplementationClassName { get; set; } = string.Empty;
        public ImmutableArray<GeneratedEvent> Events { get; set; }
    }
}
