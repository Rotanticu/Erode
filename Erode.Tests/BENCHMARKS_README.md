# 基准测试使用说明

## 运行基准测试

### 完整测试模式（推荐，结果精度高）

```bash
# 运行所有基准测试（完整模式，约 2-3 分钟）
dotnet run -c Release

# 运行特定基准测试类（完整模式）
dotnet run -c Release -- --filter "*SubscribePerformanceBenchmarks*"
```

### 快速测试模式（快速验证，结果精度较低）

```bash
# 快速模式：运行所有基准测试（约 10-30 秒）
dotnet run -c Release -- --quick

# 快速模式：运行特定基准测试类
dotnet run -c Release -- --quick --filter "*SubscribePerformanceBenchmarks*"
```

### 单独运行对比测试（避免重复运行 Erode）

Erode 已设置为所有对比测试的 Baseline，可以单独运行某个对比实现，自动与 Erode 对比：

```bash
# 只运行 MulticastDelegate 对比（会自动包含 Erode 作为 Baseline）
dotnet run -c Release -- --filter "*ComparisonBenchmarks*Publish_MulticastDelegate*"

# 只运行 DictionaryEventBus 对比
dotnet run -c Release -- --filter "*ComparisonBenchmarks*Publish_DictionaryEventBus*"

# 只运行 NativeEventDelegate 对比
dotnet run -c Release -- --filter "*ComparisonBenchmarks*Publish_NativeEventDelegate*"

# 运行所有发布性能对比（Erode 作为 Baseline）
dotnet run -c Release -- --filter "*ComparisonBenchmarks*Publish*"

# 运行所有订阅性能对比
dotnet run -c Release -- --filter "*ComparisonBenchmarks*Subscribe*"

# 运行所有内存分配对比
dotnet run -c Release -- --filter "*ComparisonBenchmarks*MemoryAllocation*"

# 运行所有并发性能对比
dotnet run -c Release -- --filter "*ComparisonBenchmarks*Concurrent*"
```

**注意**：使用 `--filter` 时，BenchmarkDotNet 会自动包含 Baseline 方法（Erode），所以即使只指定一个对比方法，也会运行 Erode 和该对比方法，结果中会显示 Ratio（相对于 Erode 的倍数）。

## 为什么测试需要这么长时间？

BenchmarkDotNet 为了保证结果的准确性和统计意义，会进行大量的迭代：

1. **编译阶段**：每次测试都需要重新编译（约 4-7 秒）
2. **试点阶段（Pilot）**：自动调整迭代次数，从 16 次逐步增加到 65,536 次（约 13 次试点）
3. **预热阶段（Warmup）**：让 JIT 编译器优化代码（约 8 次预热）
4. **开销测量**：测量测试框架本身的开销（约 20 次测量）
5. **实际测量**：进行 100 次实际测量，每次执行 65,536 次操作

**示例**：对于 Publish 测试（每次操作约 9 μs）
- 每次迭代：65,536 次操作 × 9 μs ≈ 0.6 秒
- 100 次迭代：100 × 0.6 秒 ≈ 60 秒
- 加上预热和试点：总时间约 2-3 分钟

这是 BenchmarkDotNet 的正常行为，目的是确保结果的统计准确性。如果只需要快速验证，请使用 `--quick` 模式。

## 解决文件锁定问题

当基准测试进程异常退出或被手动终止时，可能会留下文件锁定。使用以下方法清理：

### 方法 1: 使用 PowerShell 脚本（推荐）

```powershell
# 在 Erode.Tests 目录下运行
.\stop-benchmarks.ps1
```

### 方法 2: 使用批处理脚本

```cmd
# 在 Erode.Tests 目录下运行
stop-benchmarks.bat
```

### 方法 3: 手动清理

1. 打开任务管理器（Ctrl+Shift+Esc）
2. 查找并结束所有 `Erode.Tests.exe` 和相关的 `dotnet` 进程
3. 等待几秒后重试编译

## 注意事项

- 基准测试会启动多个子进程，手动结束主进程时，子进程可能仍在运行
- 如果遇到文件锁定，先运行清理脚本，再重新编译运行
- 建议在运行基准测试前先运行清理脚本，确保环境干净

