; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release Rules

## Error Rules

### ERODE002: [GenerateEvent] 只能用于返回 void 的方法
[GenerateEvent] 只能用于返回 void 的方法，但方法 '{0}' 的返回类型是 '{1}'

### ERODE007: [GenerateEvent] 只能用于接口
[GenerateEvent] 只能用于接口中的方法，但 '{0}' 不是接口

### ERODE008: [GenerateEvent] 接口访问修饰符无效
[GenerateEvent] 接口 '{0}' 必须是 public 或 internal

### ERODE010: [GenerateEvent] 接口方法不能是 static
[GenerateEvent] 接口方法 '{0}' 不能是 static

### ERODE011: [GenerateEvent] 方法名格式无效
[GenerateEvent] 方法名 '{0}' 格式无效：{1}

### ERODE012: [GenerateEvent] 参数不支持 ref 或 out
[GenerateEvent] 方法 '{0}' 的参数 '{1}' 使用了 ref 或 out，不支持。仅支持 in 或普通传参

### ERODE013: [GenerateEvent] 接口不能包含泛型参数
[GenerateEvent] 接口 '{0}' 不能包含泛型参数

### ERODE014: [GenerateEvent] 重复的事件名冲突
[GenerateEvent] 事件名 '{0}' 在命名空间 '{1}' 中重复定义。请确保每个事件名在同一命名空间内唯一

## Warning Rules

### ERODE101: 事件名是 C# 关键字
事件名 '{0}' 是 C# 关键字，已自动添加 @ 前缀

## Info Rules

### ERODE201: 成功生成事件
成功生成事件 '{0}'，类名：{1}，标准方法：{2}
