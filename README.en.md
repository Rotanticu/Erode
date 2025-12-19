# Erode

**Zero-GC, High Performance, Type-Safe, Thread-Safe Event System with Auto-Generated Event Classes**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![NuGet](https://XXX)](https://www.nuget.org/packages/XXX)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

English | [中文](README.md)

---

## Why Erode?

### Problems with Traditional Event Systems

Traditional C# event systems have the following issues:

- **Manual Event Class Definition** - Each event requires manually creating a class, leading to code duplication and errors
- **GC Pressure** - Event objects are allocated on the heap, frequent publishing causes GC pressure
- **Type Unsafe** - Using `object` or strings as event types, prone to runtime errors
- **Performance Overhead** - Event passing may involve boxing, unboxing, or struct copying

### Advantages of Erode

- ✅ **No Manual Event Classes** - Just define interfaces, Source Generator automatically generates all code
- ✅ **Zero-GC Pressure** - Uses `record struct` and `in` parameters, zero allocation for publish operations
- ✅ **Zero-Copy Passing** - `in` keyword enables pass-by-reference, O(1) overhead even for large event objects
- ✅ **Compile-Time Type Safety** - Strong type checking, IDE IntelliSense, avoiding runtime errors
- ✅ **Thread Safe (Framework Level)** - Subscription/unsubscription/publishing processes are thread-safe at the framework level (Copy-On-Write + minimal locking), thread safety of subscriber callbacks is the user's responsibility
- ✅ **Zero Runtime Overhead** - Compile-time code generation, no reflection or dictionary lookups at runtime

### Use Cases

- 🎮 **Game Development** - High-frequency event publishing, GC-sensitive
- 📊 **Real-Time Data Processing** - Large event stream processing, requires low latency
- 🔄 **Event-Driven Architecture** - Need type-safe event systems
- ⚡ **High-Performance Applications** - Scenarios with extreme performance requirements

---

## Quick Start

### Installation

**IDE Requirement**: Please ensure you are using Visual Studio 2022 17.8+ or JetBrains Rider 2023.3+. Older IDE versions have incomplete support for Source Generator and may not correctly recognize generated code.

Install Erode package via NuGet:

```bash
dotnet add package Erode
```

### Basic Usage

#### 1. Define Event Interface

Create an interface file anywhere in your project (e.g., `IPlayerEvents.cs`). This interface file serves only as a source file for the Source Generator to generate the actual event code.

Interface naming rules:
- If the interface name starts with `I` and the second letter is uppercase, the generated static class name will remove the leading `I`
  - Example: `IPlayerEvents` → generates `PlayerEvents` static class
  - Example: `IGameEvents` → generates `GameEvents` static class
- Otherwise, the interface name is used directly as the static class name

```csharp
using Erode;

public interface IPlayerEvents
{
    [GenerateEvent]
    void PublishPlayerMovedEvent(int x, int y);
}
```

#### 2. Auto-Generated After Compilation

After compilation, Source Generator will automatically generate:
- **Event class**: `PlayerMovedEvent` (`record struct` implementing `IEvent` interface)
- **Static class**: `PlayerEvents` (automatically extracted from interface name `IPlayerEvents`)
- **Publish method**: `PlayerEvents.PublishPlayerMovedEvent(x, y)`
- **Subscribe method**: `PlayerEvents.SubscribePlayerMovedEvent(handler)`

**Zero-Copy Design**:
- Event classes use `record struct` (value type), avoiding heap allocation
- Callbacks use `InAction<T>` delegate, passing parameters via `in` keyword
- The `in` keyword enables **zero-copy passing**: event objects are passed by reference but guaranteed read-only, no need to copy the entire struct
- This is the key design for achieving Zero-GC and zero-copy

The generated code is located in `.g.cs` files under the `obj` directory, no need to view or modify manually.

#### 3. Subscribe to Events

**Important**: `PlayerMovedEvent` and `PlayerEvents` are both auto-generated types at compile time. You need to compile the project once (`dotnet build` or build in your IDE) before you can use them in your code. After creating the interface file for the first time, compile the project first, then write the subscription and publish code.

**Performance Tip**: Note that the callback parameter uses the `in` keyword. The `InAction<T>` delegate passes event objects via `in` parameter, achieving zero-copy:
- `in` parameters are passed by reference, not copying the entire struct
- `in` parameters are guaranteed read-only, the compiler prevents modification
- Even if the event object contains many fields, the passing overhead is O(1), not O(n)

```csharp
var handler = new InAction<PlayerMovedEvent>((in PlayerMovedEvent evt) =>
{
    // Note: evt parameter uses in keyword for zero-copy passing
    // You can read evt.X and evt.Y, but cannot modify evt (compiler prevents it)
    Console.WriteLine($"Player moved to ({evt.X}, {evt.Y})");
});

var token = PlayerEvents.SubscribePlayerMovedEvent(handler);
```

#### 4. Publish Events

```csharp
PlayerEvents.PublishPlayerMovedEvent(10, 20);
```

#### 5. Unsubscribe

```csharp
token.Dispose(); // or use token.Unsubscribe()
```

---

## Usage Examples

### Multiple Events Interface

```csharp
public interface IGameEvents
{
    [GenerateEvent]
    void PublishPlayerJoinedEvent(string playerName);
    
    [GenerateEvent]
    void PublishPlayerLeftEvent(string playerName);
    
    [GenerateEvent]
    void PublishScoreUpdatedEvent(int score);
}

// Usage
GameEvents.PublishPlayerJoinedEvent("Alice");
GameEvents.PublishPlayerLeftEvent("Bob");
GameEvents.PublishScoreUpdatedEvent(100);
```

### No Parameter Event

```csharp
public interface IApplicationEvents
{
    [GenerateEvent]
    void PublishApplicationStartedEvent();
}

// Usage
ApplicationEvents.PublishApplicationStartedEvent();
```

### Exception Handling

```csharp
// Set global exception handler
EventDispatcher.OnException = (evt, handler, ex) =>
{
    Console.WriteLine($"Exception in handler: {ex.Message}");
};

// Or set exception handler for specific event class
PlayerEvents.OnException = (evt, handler, ex) =>
{
    // Handle PlayerEvents related exceptions
};

// Exceptions won't be thrown during publish, all handlers will be called
PlayerEvents.PublishPlayerMovedEvent(10, 20);
```

### Multiple Subscribers

```csharp
var handler1 = new InAction<PlayerMovedEvent>((in PlayerMovedEvent evt) => 
{
    Console.WriteLine($"Handler 1: {evt.X}, {evt.Y}");
});

var handler2 = new InAction<PlayerMovedEvent>((in PlayerMovedEvent evt) => 
{
    Console.WriteLine($"Handler 2: {evt.X}, {evt.Y}");
});

var token1 = PlayerEvents.SubscribePlayerMovedEvent(handler1);
var token2 = PlayerEvents.SubscribePlayerMovedEvent(handler2);

// Publish once, all subscribers will receive
PlayerEvents.PublishPlayerMovedEvent(10, 20);
// Output:
// Handler 1: 10, 20
// Handler 2: 10, 20
```

---

## Performance

Erode focuses on Zero-GC and deterministic distribution as core goals.
Benchmark tests (BenchmarkDotNet) are being refined.

---

## Requirements

- .NET 8.0 or higher
- C# 12.0 or higher (for collection expressions and primary constructor)
- **IDE Support**:
  - Visual Studio 2022 17.8+ or
  - JetBrains Rider 2023.3+
  
  > **Note**: Source Generator has poor experience in older IDE versions and may not correctly recognize generated code. Strongly recommend using the above IDE versions for the best development experience.

---

## License

This project is licensed under the [MIT License](LICENSE).

---

## Project Structure

```
Erode/
├── Erode/                    # Core library
│   ├── Attributes/           # GenerateEventAttribute
│   ├── Core/                 # Core interfaces and types
│   └── Dispatchers/          # Event dispatcher implementation
├── Erode.Generator/          # Source Generator
│   ├── EventGenerator.cs     # Code generator
│   ├── GenerateEventAnalyzer.cs  # Compile-time analyzer
│   └── EventValidationHelper.cs  # Validation utilities
└── Erode.Tests/              # Test project
```

---

## Contributing

Issues and Pull Requests are welcome!

---

## Acknowledgments

This project is fully generated by Cursor with human assistance, containing zero manually written code.
It demonstrates the capabilities of modern AI code generation for high-performance code.

---

**Made with ❤️ by Cursor AI**

