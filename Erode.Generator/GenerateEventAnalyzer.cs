using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Erode.Generator;

/// <summary>
/// Analyzer：在 IDE 中实时检查 [GenerateEvent] 特性的使用，提供语法提示
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GenerateEventAnalyzer : DiagnosticAnalyzer
{
    // 使用共享的诊断描述符

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            EventDiagnostics.Error_NotInterface,
            EventDiagnostics.Error_InterfaceHasGenericParameters,
            EventDiagnostics.Error_InterfaceMethodCannotBeStatic,
            EventDiagnostics.Error_NotVoid,
            EventDiagnostics.Error_ParameterRefOrOut,
            EventDiagnostics.Error_InvalidMethodNameFormat,
            EventDiagnostics.Error_DuplicateEventName,
            EventDiagnostics.Warning_KeywordEventName);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        
        // 注册方法声明的语法节点操作
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        
        // 注册编译单元操作，用于检测重复事件名
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;
        
        // 检查是否有 [GenerateEvent] 特性
        var hasAttribute = methodSyntax.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(attr =>
            {
                var name = attr.Name.ToString();
                return name == "GenerateEvent" || 
                       name == "GenerateEventAttribute" || 
                       name.EndsWith(".GenerateEvent") ||
                       name.EndsWith(".GenerateEventAttribute");
            });

        if (!hasAttribute)
            return;

        var semanticModel = context.SemanticModel;
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax) as IMethodSymbol;
        
        if (methodSymbol == null)
            return;

        var location = methodSyntax.GetLocation();

        // 检查方法所在的类型必须是接口
        var containingType = methodSymbol.ContainingType;
        var interfaceValidation = EventValidationHelper.ValidateInterface(containingType);
        if (!interfaceValidation.IsValid)
        {
            Diagnostic? diagnostic = null;
            switch (interfaceValidation.ErrorType)
            {
                case "NotInterface":
                    diagnostic = Diagnostic.Create(EventDiagnostics.Error_NotInterface, location, containingType.Name);
                    break;
                case "InterfaceHasGenericParameters":
                    diagnostic = Diagnostic.Create(EventDiagnostics.Error_InterfaceHasGenericParameters, location, containingType.Name);
                    break;
            }
            if (diagnostic != null)
            {
                context.ReportDiagnostic(diagnostic);
            }
            return;
        }

        // 使用共享验证工具验证接口方法
        var methodValidation = EventValidationHelper.ValidateInterfaceMethod(methodSymbol);
        if (!methodValidation.IsValid)
        {
            Diagnostic? diagnostic = null;
            switch (methodValidation.ErrorType)
            {
                case "InterfaceMethodCannotBeStatic":
                    diagnostic = Diagnostic.Create(EventDiagnostics.Error_InterfaceMethodCannotBeStatic, location, methodValidation.MethodName ?? "unknown");
                    break;
                case "NotVoid":
                    diagnostic = Diagnostic.Create(EventDiagnostics.Error_NotVoid, location, methodValidation.MethodName ?? "unknown", methodValidation.ReturnType ?? "unknown");
                    break;
                case "ParameterRefOrOut":
                    diagnostic = Diagnostic.Create(EventDiagnostics.Error_ParameterRefOrOut, location, methodValidation.MethodName ?? "unknown", methodValidation.ParameterName ?? "unknown");
                    break;
            }
            if (diagnostic != null)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        // 从方法名中提取并验证事件名
        var eventNameValidation = EventValidationHelper.ValidateAndNormalizeEventName(methodSymbol.Name, context.Compilation, out var isKeyword);
        
        if (!eventNameValidation.IsValid)
        {
            Diagnostic? diagnostic = null;
            switch (eventNameValidation.ErrorType)
            {
                case "InvalidMethodNameFormat":
                    diagnostic = Diagnostic.Create(
                        EventDiagnostics.Error_InvalidMethodNameFormat,
                        location,
                        methodSymbol.Name,
                        eventNameValidation.FormatErrorReason ?? "格式无效");
                    break;
            }
            if (diagnostic != null)
            {
                context.ReportDiagnostic(diagnostic);
            }
            return;
        }

        // 如果是关键字，显示警告
        if (isKeyword && eventNameValidation.CleanedEventName != null)
        {
            string eventNameForDiagnostic;
            if (eventNameValidation.NormalizedEventName != null && eventNameValidation.NormalizedEventName.StartsWith("@", StringComparison.Ordinal))
            {
                var normalized = eventNameValidation.NormalizedEventName.Substring(1);
                if (normalized.EndsWith("Event", StringComparison.Ordinal) && normalized.Length > 5)
                {
                    eventNameForDiagnostic = "@" + normalized.Substring(0, normalized.Length - 5);
                }
                else
                {
                    eventNameForDiagnostic = eventNameValidation.NormalizedEventName;
                }
            }
            else
            {
                eventNameForDiagnostic = eventNameValidation.CleanedEventName;
            }
            
            var diagnostic = Diagnostic.Create(
                EventDiagnostics.Warning_KeywordEventName,
                location,
                eventNameForDiagnostic);
            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 分析编译单元，检测重复的事件名
    /// </summary>
    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        // 收集所有有效的方法：按 (命名空间, 事件名) 分组
        var methodsByNamespaceAndEventName = new Dictionary<(string Namespace, string EventName), List<(IMethodSymbol Method, Location Location)>>();

        // 遍历所有语法树
        foreach (var syntaxTree in context.Compilation.SyntaxTrees)
        {
            var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot(context.CancellationToken);

            // 查找所有方法声明
            var methodDeclarations = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var methodSyntax in methodDeclarations)
            {
                // 检查是否有 [GenerateEvent] 特性
                var hasAttribute = methodSyntax.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(attr =>
                    {
                        var name = attr.Name.ToString();
                        return name == "GenerateEvent" || 
                               name == "GenerateEventAttribute" || 
                               name.EndsWith(".GenerateEvent") ||
                               name.EndsWith(".GenerateEventAttribute");
                    });

                if (!hasAttribute)
                    continue;

                var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax) as IMethodSymbol;
                if (methodSymbol == null)
                    continue;

                // 验证接口和方法
                var containingType = methodSymbol.ContainingType;
                var interfaceValidation = EventValidationHelper.ValidateInterface(containingType);
                if (!interfaceValidation.IsValid)
                    continue;

                var methodValidation = EventValidationHelper.ValidateInterfaceMethod(methodSymbol);
                if (!methodValidation.IsValid)
                    continue;

                // 提取事件名
                var eventNameValidation = EventValidationHelper.ValidateAndNormalizeEventName(
                    methodSymbol.Name, 
                    context.Compilation, 
                    out _);
                
                if (!eventNameValidation.IsValid || eventNameValidation.CleanedEventName == null)
                    continue;

                var eventName = eventNameValidation.CleanedEventName;
                var namespaceName = methodSymbol.ContainingNamespace?.ToDisplayString() ?? "";
                var key = (namespaceName, eventName);

                // 记录方法信息
                if (!methodsByNamespaceAndEventName.ContainsKey(key))
                {
                    methodsByNamespaceAndEventName[key] = new List<(IMethodSymbol, Location)>();
                }

                var location = methodSyntax.GetLocation();
                methodsByNamespaceAndEventName[key].Add((methodSymbol, location));
            }
        }

        // 检查重复：如果同一命名空间内同一事件名有多个方法，除了第一个，其他都报告错误
        foreach (var kvp in methodsByNamespaceAndEventName)
        {
            var namespaceName = kvp.Key.Namespace;
            var eventName = kvp.Key.EventName;
            var methods = kvp.Value;

            if (methods.Count > 1)
            {
                // 报告所有重复的方法（除了第一个）
                foreach (var methodInfo in methods.Skip(1))
                {
                    var method = methodInfo.Method;
                    var methodLocation = methodInfo.Location;
                    var diagnostic = Diagnostic.Create(
                        EventDiagnostics.Error_DuplicateEventName,
                        methodLocation,
                        eventName,
                        namespaceName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}

