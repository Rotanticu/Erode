using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace Erode.Tests.Benchmarks;

/// <summary>
/// 基准测试程序入口点
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // 注册 Ctrl+C 和进程退出事件，确保优雅退出
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\n收到退出信号，正在清理...");
            e.Cancel = false; // 允许正常退出
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            Console.WriteLine("进程退出，清理资源...");
        };

        try
        {
            // 检查是否使用快速模式（--quick 或 -q）
            bool quickMode = args.Length > 0 && (args[0] == "--quick" || args[0] == "-q");

            // 设置输出路径：结果输出到 Result 文件夹，日志输出到 Log 文件夹
            var assemblyLocation = typeof(Program).Assembly.Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? "";
            var benchmarksDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "Benchmarks"));
            var resultPath = Path.Combine(benchmarksDir, "Result");
            var logPath = Path.Combine(benchmarksDir, "Log");

            // 确保目录存在
            Directory.CreateDirectory(resultPath);
            Directory.CreateDirectory(logPath);

            Console.WriteLine($"Benchmark 结果输出路径: {resultPath}");
            Console.WriteLine($"Benchmark 日志输出路径: {logPath}");

            // 创建日志文件路径
            var logFilePath = Path.Combine(logPath, $"Benchmark-{DateTime.Now:yyyyMMdd-HHmmss}.log");

            // 配置：使用自定义输出路径，禁用优化验证
            // 注意：BenchmarkDotNet 会根据测试执行时间自动调整 InvocationCount 和 UnrollFactor
            // 这是正常的优化行为，但会导致输出格式略有不同
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .WithArtifactsPath(resultPath)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddLogger(ConsoleLogger.Default)
                .AddLogger(new StreamLogger(File.CreateText(logFilePath)));

            if (quickMode)
            {
                // 快速模式：使用 ShortRun，大幅减少迭代次数，适合快速验证
                // 注意：快速模式的结果精度较低，仅用于快速验证，正式测试请使用默认模式
                Console.WriteLine("⚠️  快速模式：使用 ShortRun，结果精度较低，仅用于快速验证");
                config = config.WithOptions(ConfigOptions.DisableOptimizationsValidator)
                    .AddJob(BenchmarkDotNet.Jobs.Job.ShortRun);

                // 移除快速模式参数，传递剩余参数
                var remainingArgs = args.Skip(1).ToArray();
                if (remainingArgs.Length == 0)
                {
                    var summary = BenchmarkRunner.Run(typeof(Program).Assembly, config);
                }
                else
                {
                    var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
                    switcher.Run(remainingArgs, config);
                }
            }
            else
            {
                // 默认模式：完整测试，结果精度高，但需要较长时间
                // 如果没有指定参数，运行所有基准测试
                if (args.Length == 0)
                {
                    var summary = BenchmarkRunner.Run(typeof(Program).Assembly, config);
                }
                else
                {
                    // 运行指定的基准测试类
                    var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
                    switcher.Run(args, config);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"基准测试执行出错: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}

