using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Erode.Generator;

/// <summary>
/// 共享的诊断描述符，供 Analyzer 和 Source Generator 使用
/// </summary>
internal static class EventDiagnostics
{
    public static readonly DiagnosticDescriptor Error_NotVoid = new(
        id: "ERODE002",
        title: "[GenerateEvent] 只能用于返回 void 的方法",
        messageFormat: "[GenerateEvent] 只能用于返回 void 的方法，但方法 '{0}' 的返回类型是 '{1}'",
        category: "Erode.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NotInterface = new(
        id: "ERODE007",
        title: "[GenerateEvent] 只能用于接口",
        messageFormat: "[GenerateEvent] 只能用于接口中的方法，但 '{0}' 不是接口",
        category: "Erode.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_InvalidInterfaceAccessibility = new(
        id: "ERODE008",
        title: "[GenerateEvent] 接口访问修饰符无效",
        messageFormat: "[GenerateEvent] 接口 '{0}' 必须是 public 或 internal",
        category: "Erode.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_InterfaceMethodCannotBeStatic = new(
        id: "ERODE010",
        title: "[GenerateEvent] 接口方法不能是 static",
        messageFormat: "[GenerateEvent] 接口方法 '{0}' 不能是 static",
        category: "Erode.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_InvalidMethodNameFormat = new(
        id: "ERODE011",
        title: "[GenerateEvent] 方法名格式无效",
        messageFormat: "[GenerateEvent] 方法名 '{0}' 格式无效：{1}",
        category: "Erode.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_ParameterRefOrOut = new(
        id: "ERODE012",
        title: "[GenerateEvent] 参数不支持 ref 或 out",
        messageFormat: "[GenerateEvent] 方法 '{0}' 的参数 '{1}' 使用了 ref 或 out，不支持。仅支持 in 或普通传参",
        category: "Erode.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_InterfaceHasGenericParameters = new(
        id: "ERODE013",
        title: "[GenerateEvent] 接口不能包含泛型参数",
        messageFormat: "[GenerateEvent] 接口 '{0}' 不能包含泛型参数",
        category: "Erode.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_DuplicateEventName = new(
        id: "ERODE014",
        title: "[GenerateEvent] 重复的事件名冲突",
        messageFormat: "[GenerateEvent] 事件名 '{0}' 在命名空间 '{1}' 中重复定义。请确保每个事件名在同一命名空间内唯一",
        category: "Erode.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

    public static readonly DiagnosticDescriptor Warning_KeywordEventName = new(
        id: "ERODE101",
        title: "事件名是 C# 关键字",
        messageFormat: "事件名 '{0}' 是 C# 关键字，已自动添加 @ 前缀",
        category: "Erode.Generator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Info_EventGenerated = new(
        id: "ERODE201",
        title: "成功生成事件",
        messageFormat: "成功生成事件 '{0}'，类名：{1}，标准方法：{2}",
        category: "Erode.Generator",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}

/// <summary>
/// 共享的事件验证工具类，供 Analyzer 和 Source Generator 使用
/// </summary>
internal static class EventValidationHelper
{
    /// <summary>
    /// 验证接口是否符合 [GenerateEvent] 的要求
    /// </summary>
    public static InterfaceValidationResult ValidateInterface(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol == null)
            return new InterfaceValidationResult { IsValid = false, ErrorType = "InterfaceSymbolNull" };

        // 检查必须是 interface 类型
        if (interfaceSymbol.TypeKind != TypeKind.Interface)
            return new InterfaceValidationResult 
            { 
                IsValid = false, 
                ErrorType = "NotInterface", 
                InterfaceName = interfaceSymbol.Name 
            };

        // 检查接口不能包含泛型参数
        if (interfaceSymbol.TypeParameters.Length > 0)
            return new InterfaceValidationResult 
            { 
                IsValid = false, 
                ErrorType = "InterfaceHasGenericParameters", 
                InterfaceName = interfaceSymbol.Name 
            };

        return new InterfaceValidationResult { IsValid = true, InterfaceName = interfaceSymbol.Name };
    }

    /// <summary>
    /// 从接口名提取实现类名
    /// 如果接口名以 I 开头且第二个字母是大写，移除首字母 I 作为静态类名
    /// 否则直接使用接口名
    /// </summary>
    public static string ExtractImplementationClassName(string interfaceName)
    {
        if (string.IsNullOrEmpty(interfaceName))
            return interfaceName;

        // 检查是否以 I 开头
        if (!interfaceName.StartsWith("I", StringComparison.Ordinal))
            return interfaceName;

        // 如果接口名长度小于 2，直接返回
        if (interfaceName.Length < 2)
            return interfaceName;

        // 检查第二个字母是否是大写
        var secondChar = interfaceName[1];
        if (char.IsUpper(secondChar))
        {
            // 移除首字母 I
            return interfaceName.Substring(1);
        }

        // 第二个字母不是大写，直接使用接口名
        return interfaceName;
    }

    /// <summary>
    /// 验证接口方法是否符合 [GenerateEvent] 的要求
    /// </summary>
    public static MethodValidationResult ValidateInterfaceMethod(IMethodSymbol methodSymbol)
    {
        if (methodSymbol == null)
            return new MethodValidationResult { IsValid = false, ErrorType = "MethodSymbolNull" };

        // 接口方法不能是 static（虽然接口方法本身不能是 static，但明确检查）
        if (methodSymbol.IsStatic)
            return new MethodValidationResult { IsValid = false, ErrorType = "InterfaceMethodCannotBeStatic", MethodName = methodSymbol.Name };

        // 必须返回 void
        if (!methodSymbol.ReturnsVoid)
            return new MethodValidationResult 
            { 
                IsValid = false, 
                ErrorType = "NotVoid", 
                MethodName = methodSymbol.Name,
                ReturnType = methodSymbol.ReturnType.ToDisplayString()
            };

        // 检查参数不能使用 ref 或 out
        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.RefKind == RefKind.Ref || parameter.RefKind == RefKind.Out)
            {
                return new MethodValidationResult 
                { 
                    IsValid = false, 
                    ErrorType = "ParameterRefOrOut", 
                    MethodName = methodSymbol.Name,
                    ParameterName = parameter.Name
                };
            }
        }

        return new MethodValidationResult { IsValid = true };
    }

    /// <summary>
    /// 验证方法名格式是否符合要求
    /// 要求：Publish 开头、Event 结尾、中间有内容、不能有多个 Publish 或 Event
    /// </summary>
    public static MethodNameFormatValidationResult ValidateMethodNameFormat(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            return new MethodNameFormatValidationResult
            {
                IsValid = false,
                ErrorReason = "方法名不能为空"
            };
        }

        const string publishPrefix = "Publish";
        const string eventSuffix = "Event";

        // 检查是否以 Publish 开头
        if (!methodName.StartsWith(publishPrefix, StringComparison.Ordinal))
        {
            return new MethodNameFormatValidationResult
            {
                IsValid = false,
                ErrorReason = $"方法名必须以 '{publishPrefix}' 开头"
            };
        }

        // 检查是否以 Event 结尾
        if (!methodName.EndsWith(eventSuffix, StringComparison.Ordinal))
        {
            return new MethodNameFormatValidationResult
            {
                IsValid = false,
                ErrorReason = $"方法名必须以 '{eventSuffix}' 结尾"
            };
        }

        // 检查不能只有 Publish 和 Event（中间必须有其他字符）
        if (methodName.Length == publishPrefix.Length + eventSuffix.Length)
        {
            return new MethodNameFormatValidationResult
            {
                IsValid = false,
                ErrorReason = $"方法名不能只有 '{publishPrefix}' 和 '{eventSuffix}'，中间必须有其他字符"
            };
        }

        // 检查不能有多个 Publish（只能有一个在开头）
        var publishCount = 0;
        var index = 0;
        while ((index = methodName.IndexOf(publishPrefix, index, StringComparison.Ordinal)) != -1)
        {
            publishCount++;
            index += publishPrefix.Length;
        }

        if (publishCount > 1)
        {
            return new MethodNameFormatValidationResult
            {
                IsValid = false,
                ErrorReason = $"方法名中不能有多个 '{publishPrefix}'（只能有一个在开头）"
            };
        }

        // 检查不能有多个 Event（只能有一个在结尾）
        var eventCount = 0;
        index = 0;
        while ((index = methodName.IndexOf(eventSuffix, index, StringComparison.Ordinal)) != -1)
        {
            eventCount++;
            index += eventSuffix.Length;
        }

        if (eventCount > 1)
        {
            return new MethodNameFormatValidationResult
            {
                IsValid = false,
                ErrorReason = $"方法名中不能有多个 '{eventSuffix}'（只能有一个在结尾）"
            };
        }

        return new MethodNameFormatValidationResult
        {
            IsValid = true
        };
    }

    /// <summary>
    /// 从方法名中提取并验证事件名
    /// </summary>
    public static EventNameValidationResult ValidateAndNormalizeEventName(
        string methodName,
        Compilation compilation,
        out bool isKeyword)
    {
        isKeyword = false;

        // 首先验证方法名格式
        var formatValidation = ValidateMethodNameFormat(methodName);
        if (!formatValidation.IsValid)
        {
            return new EventNameValidationResult 
            { 
                IsValid = false, 
                ErrorType = "InvalidMethodNameFormat",
                OriginalEventName = methodName,
                FormatErrorReason = formatValidation.ErrorReason
            };
        }

        // 从方法名中提取事件名：去掉开头的 "Publish"，保留 "Event" 后缀
        // 例如：PublishTestGeneratedEvent -> TestGeneratedEvent
        const string publishPrefix = "Publish";
        var eventName = methodName.Substring(publishPrefix.Length); // 去掉 "Publish" 前缀

        // 验证提取的事件名是否是有效的标识符
        if (!SyntaxFacts.IsValidIdentifier(eventName))
        {
            return new EventNameValidationResult 
            { 
                IsValid = false, 
                ErrorType = "InvalidMethodNameFormat",
                OriginalEventName = methodName,
                FormatErrorReason = $"从方法名 '{methodName}' 提取的事件名 '{eventName}' 不是有效的 C# 标识符"
            };
        }

        // 检查长度限制
        if (eventName.Length > 512)
        {
            eventName = eventName.Substring(0, 512);
        }

        // 使用 Roslyn 检查是否是关键字
        var normalizedEventName = eventName;
        var keywordKind = SyntaxFacts.GetKeywordKind(eventName);
        isKeyword = keywordKind != SyntaxKind.None && SyntaxFacts.IsReservedKeyword(keywordKind);

        // 也检查上下文关键字
        if (!isKeyword)
        {
            var contextualKeywordKind = SyntaxFacts.GetContextualKeywordKind(eventName);
            isKeyword = contextualKeywordKind != SyntaxKind.None && SyntaxFacts.IsContextualKeyword(contextualKeywordKind);
        }

        // 如果整个事件名不是关键字，检查事件名中是否包含关键字（去掉 Event 后缀后）
        if (!isKeyword && eventName.EndsWith("Event", StringComparison.Ordinal) && eventName.Length > 5)
        {
            var eventNameWithoutSuffix = eventName.Substring(0, eventName.Length - 5); // 移除末尾的 "Event"
            if (!string.IsNullOrEmpty(eventNameWithoutSuffix))
            {
                // 检查去掉 Event 后缀后的部分是否是关键字（先检查原样，再检查小写版本）
                var keywordKindWithoutSuffix = SyntaxFacts.GetKeywordKind(eventNameWithoutSuffix);
                isKeyword = keywordKindWithoutSuffix != SyntaxKind.None && SyntaxFacts.IsReservedKeyword(keywordKindWithoutSuffix);
                
                if (!isKeyword)
                {
                    var contextualKeywordKindWithoutSuffix = SyntaxFacts.GetContextualKeywordKind(eventNameWithoutSuffix);
                    isKeyword = contextualKeywordKindWithoutSuffix != SyntaxKind.None && SyntaxFacts.IsContextualKeyword(contextualKeywordKindWithoutSuffix);
                }
                
                // 如果原样不是关键字，检查小写版本
                if (!isKeyword && !string.IsNullOrEmpty(eventNameWithoutSuffix))
                {
                    var lowerCaseName = eventNameWithoutSuffix.ToLowerInvariant();
                    var keywordKindLower = SyntaxFacts.GetKeywordKind(lowerCaseName);
                    isKeyword = keywordKindLower != SyntaxKind.None && SyntaxFacts.IsReservedKeyword(keywordKindLower);
                    
                    if (!isKeyword)
                    {
                        var contextualKeywordKindLower = SyntaxFacts.GetContextualKeywordKind(lowerCaseName);
                        isKeyword = contextualKeywordKindLower != SyntaxKind.None && SyntaxFacts.IsContextualKeyword(contextualKeywordKindLower);
                    }
                    
                    if (isKeyword)
                    {
                        // 如果小写版本是关键字，使用小写版本加 @ 前缀
                        normalizedEventName = "@" + lowerCaseName + "Event";
                    }
                }
                else if (isKeyword)
                {
                    // 如果原样是关键字，使用 @ 前缀
                    normalizedEventName = "@" + eventNameWithoutSuffix + "Event";
                }
            }
        }

        if (isKeyword && !normalizedEventName.StartsWith("@", StringComparison.Ordinal))
        {
            normalizedEventName = "@" + eventName;
        }

        return new EventNameValidationResult
        {
            IsValid = true,
            CleanedEventName = eventName,
            NormalizedEventName = normalizedEventName,
            OriginalEventName = methodName
        };
    }

    /// <summary>
    /// 检查参数名是否是关键字，如果是则添加 @ 前缀
    /// </summary>
    public static string NormalizeParameterName(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return parameterName;

        var safeName = parameterName;

        // 使用 Roslyn 检查参数名是否是关键字
        var paramKeywordKind = SyntaxFacts.GetKeywordKind(safeName);
        var isParamKeyword = paramKeywordKind != SyntaxKind.None && SyntaxFacts.IsReservedKeyword(paramKeywordKind);

        if (!isParamKeyword)
        {
            var paramContextualKeywordKind = SyntaxFacts.GetContextualKeywordKind(safeName);
            isParamKeyword = paramContextualKeywordKind != SyntaxKind.None && SyntaxFacts.IsContextualKeyword(paramContextualKeywordKind);
        }

        if (isParamKeyword)
        {
            safeName = "@" + safeName;
        }

        return safeName;
    }

    /// <summary>
    /// 验证事件名格式
    /// </summary>
    private static EventNameFormatValidationResult ValidateEventNameFormat(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            return new EventNameFormatValidationResult
            {
                IsValid = false,
                ErrorReason = "事件名不能为空"
            };
        }

        // 检查是否只有一个 "Event" 没有其他字符
        if (string.Equals(eventName, "Event", StringComparison.OrdinalIgnoreCase))
        {
            return new EventNameFormatValidationResult
            {
                IsValid = false,
                ErrorReason = "事件名不能仅为 'Event'，必须包含其他字符"
            };
        }

        // 检查是否以 "Event" 结尾
        if (!eventName.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
        {
            return new EventNameFormatValidationResult
            {
                IsValid = false,
                ErrorReason = "事件名必须以 'Event' 结尾"
            };
        }

        // 检查是否包含多个 "Event"（除了末尾的）
        var eventNameWithoutSuffix = eventName.Substring(0, eventName.Length - 5); // 移除末尾的 "Event"
        if (eventNameWithoutSuffix.IndexOf("Event", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new EventNameFormatValidationResult
            {
                IsValid = false,
                ErrorReason = "事件名中不能包含多个 'Event'，只能以 'Event' 结尾"
            };
        }

        return new EventNameFormatValidationResult { IsValid = true };
    }

    /// <summary>
    /// 生成属性名（首字母大写）
    /// </summary>
    public static string GeneratePropertyName(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return parameterName;

        if (parameterName.Length == 1)
        {
            return char.ToUpperInvariant(parameterName[0]).ToString();
        }

        return char.ToUpperInvariant(parameterName[0]) + parameterName.Substring(1);
    }
}

/// <summary>
/// 方法验证结果
/// </summary>
internal sealed class MethodValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorType { get; set; }
    public string? MethodName { get; set; }
    public string? ReturnType { get; set; }
    public string? ParameterName { get; set; }
}

/// <summary>
/// 事件名验证结果
/// </summary>
internal sealed class EventNameValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorType { get; set; }
    public string? OriginalEventName { get; set; }
    public string? CleanedEventName { get; set; }
    public string? NormalizedEventName { get; set; }
    public string? FormatErrorReason { get; set; }
}

/// <summary>
/// 事件名格式验证结果
/// </summary>
internal sealed class EventNameFormatValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorReason { get; set; }
}

/// <summary>
/// 方法名格式验证结果
/// </summary>
internal sealed class MethodNameFormatValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorReason { get; set; }
}

/// <summary>
/// 接口验证结果
/// </summary>
internal sealed class InterfaceValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorType { get; set; }
    public string? InterfaceName { get; set; }
}

