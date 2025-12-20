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
        // 确保控制台输出是实时的（不缓冲）
        Console.Out.Flush();
        
        // 启动时清理可能残留的测试进程（使用 PowerShell 脚本）
        RunCleanupScript(force: true);

        // 注册 Ctrl+C 和进程退出事件，确保优雅退出
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\n收到退出信号，正在清理残留进程...");
            e.Cancel = true; // 先阻止退出，等待清理完成
            try
            {
                // 先尝试简单清理（更快）
                SimpleCleanup();
                // 然后运行完整清理脚本
                CleanupOnExit();
                Console.WriteLine("清理完成，正在退出...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理时出错: {ex.Message}");
            }
            finally
            {
                // 强制退出，不再等待
                Environment.Exit(0);
            }
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            CleanupOnExit();
        };

        try
        {
            // 检查是否使用快速模式（--quick 或 -q）
            bool quickMode = args.Length > 0 && (args[0] == "--quick" || args[0] == "-q");

            // 设置输出路径：结果输出到 bin\Benchmarks\Result 文件夹，日志输出到 bin\Benchmarks\Log 文件夹
            var assemblyLocation = typeof(Program).Assembly.Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? "";
            // 从 bin\Release\net8.0 或 bin\Debug\net8.0 回到 bin 目录
            var binDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", ".."));
            var benchmarksDir = Path.Combine(binDir, "Benchmarks");
            var resultPath = Path.Combine(benchmarksDir, "Result");
            var logPath = Path.Combine(benchmarksDir, "Log");

            // 确保目录存在
            Directory.CreateDirectory(resultPath);
            Directory.CreateDirectory(logPath);

            Console.WriteLine($"Benchmark 结果输出路径: {resultPath}");
            Console.WriteLine($"结果文件位置: {Path.Combine(resultPath, "results")}");
            Console.WriteLine($"日志文件位置: {resultPath} (BenchmarkDotNet 自动生成)");

            // 配置：使用自定义输出路径，禁用优化验证
            // 注意：BenchmarkDotNet 会根据测试执行时间自动调整 InvocationCount 和 UnrollFactor
            // 这是正常的优化行为，但会导致输出格式略有不同
            // DefaultConfig.Instance 已经包含了 ConsoleLogger，无需重复添加
            // 使用 WithArtifactsPath 不会影响控制台输出，日志文件会额外保存到指定路径
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .WithArtifactsPath(resultPath)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

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

    /// <summary>
    /// 运行清理脚本（PowerShell）
    /// </summary>
    private static void RunCleanupScript(bool force = false)
    {
        try
        {
            var assemblyLocation = typeof(Program).Assembly.Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? "";
            // 从 bin\Release\net8.0 或 bin\Debug\net8.0 回到 Erode.Tests 目录
            // assemblyDir = bin\Release\net8.0
            // ..\.. = bin
            // ..\..\.. = Erode.Tests (项目根目录)
            var testsDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."));
            var scriptPath = Path.Combine(testsDir, "stop-benchmarks.ps1");

            if (!File.Exists(scriptPath))
            {
                // 脚本不存在，使用简单的清理方法
                SimpleCleanup();
                return;
            }

            // 尝试使用 pwsh (PowerShell Core)，如果不存在则使用 powershell (Windows PowerShell)
            var powershellPath = "pwsh.exe";
            try
            {
                var testProc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = powershellPath,
                    Arguments = "-Command exit",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                testProc?.WaitForExit(1000);
            }
            catch
            {
                powershellPath = "powershell.exe";
            }

            // 传递当前进程 ID 给脚本，让它排除当前进程
            var currentPid = Environment.ProcessId;
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = powershellPath,
                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -ExcludePid {currentPid}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process != null)
            {
                // 等待脚本执行完成，确保清理完成后再继续
                // 增加等待时间，确保脚本有足够时间清理所有进程
                if (!process.WaitForExit(10000)) // 最多等待 10 秒
                {
                    Console.WriteLine("警告：清理脚本执行超时，强制终止脚本进程");
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // 忽略
                    }
                }
                
                // 读取并输出脚本的输出
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    Console.WriteLine(output);
                }
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.Error.WriteLine(error);
                }
            }
        }
        catch (Exception ex)
        {
            // 脚本执行失败，使用简单的清理方法
            Console.WriteLine($"执行清理脚本失败: {ex.Message}，使用简单清理方法");
            SimpleCleanup();
        }
    }

    /// <summary>
    /// 简单的进程清理方法（备用方案）
    /// </summary>
    private static void SimpleCleanup()
    {
        try
        {
            var currentProcessId = Environment.ProcessId;
            var processes = System.Diagnostics.Process.GetProcesses()
                .Where(p => p.ProcessName.Contains("Erode.Tests", StringComparison.OrdinalIgnoreCase) 
                         && p.Id != currentProcessId)
                .ToList();

            if (processes.Count > 0)
            {
                Console.WriteLine($"发现 {processes.Count} 个残留的测试进程，正在清理...");
                foreach (var process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            process.WaitForExit(1000);
                        }
                    }
                    catch
                    {
                        // 忽略无法终止的进程
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
                System.Threading.Thread.Sleep(500);
            }
        }
        catch
        {
            // 清理失败不影响主流程
        }
    }

    /// <summary>
    /// 退出时清理
    /// </summary>
    private static void CleanupOnExit()
    {
        // 运行清理脚本清理其他进程
        // stop-benchmarks.ps1 会自动排除当前进程
        RunCleanupScript(force: false);
    }
}

