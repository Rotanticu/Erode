# 强制结束所有 Erode.Tests 基准测试相关进程
# 用于解决文件锁定问题

param(
    [int]$ExcludePid = 0  # 要排除的进程 ID（通常是调用此脚本的进程）
)

$currentPid = $PID
if ($ExcludePid -eq 0) {
    $ExcludePid = $currentPid
}

Write-Host "正在查找 Erode.Tests 相关进程（排除 PID: $ExcludePid）..." -ForegroundColor Yellow

# 方法1: 通过进程名查找
$processesByName = Get-Process -Name "Erode.Tests" -ErrorAction SilentlyContinue | Where-Object { $_.Id -ne $ExcludePid }

# 方法2: 通过路径查找（更精确）
$processesByPath = Get-Process | Where-Object {
    $_.Path -and $_.Path -like "*Erode*Tests*" -and $_.Id -ne $ExcludePid
} -ErrorAction SilentlyContinue

# 方法3: 查找 dotnet 进程，检查命令行参数
$dotnetProcesses = @()
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | ForEach-Object {
    if ($_.Id -ne $ExcludePid) {
        try {
            $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)").CommandLine
            if ($cmdLine -and ($cmdLine -like "*Erode.Tests*" -or $cmdLine -like "*Benchmarks*" -or $cmdLine -like "*Erode*Tests*")) {
                $dotnetProcesses += $_
            }
        }
        catch {
            # 忽略错误
        }
    }
}

# 合并所有找到的进程
$allProcesses = @()
if ($processesByName) { $allProcesses += $processesByName }
if ($processesByPath) { $allProcesses += $processesByPath }
if ($dotnetProcesses) { $allProcesses += $dotnetProcesses }

# 去重（按 PID）并排除当前进程
$uniqueProcesses = $allProcesses | Where-Object { $_.Id -ne $ExcludePid } | Sort-Object -Property Id -Unique

if ($uniqueProcesses.Count -eq 0) {
    Write-Host "未找到相关进程" -ForegroundColor Green
    exit 0
}

Write-Host "找到 $($uniqueProcesses.Count) 个相关进程：" -ForegroundColor Yellow
foreach ($proc in $uniqueProcesses) {
    $path = if ($proc.Path) { $proc.Path } else { "未知路径" }
    Write-Host "  - PID: $($proc.Id), 名称: $($proc.ProcessName), 路径: $path" -ForegroundColor Cyan
}

# 先尝试优雅退出（发送关闭信号）
Write-Host "`n尝试优雅退出..." -ForegroundColor Yellow
foreach ($proc in $uniqueProcesses) {
    try {
        # 尝试关闭主窗口（如果是 GUI 应用）
        if ($proc.MainWindowHandle -ne [IntPtr]::Zero) {
            $proc.CloseMainWindow()
        }
        else {
            # 对于控制台应用，直接终止
            Stop-Process -Id $proc.Id -ErrorAction Stop
        }
        Write-Host "  已终止进程 PID: $($proc.Id)" -ForegroundColor Green
    }
    catch {
        Write-Host "  无法终止进程 PID: $($proc.Id): $_" -ForegroundColor Red
    }
}

# 等待进程退出
Start-Sleep -Seconds 2

# 检查是否还有残留进程，强制结束
$remaining = @()
try {
    $remaining += Get-Process -Name "Erode.Tests" -ErrorAction SilentlyContinue | Where-Object { $_.Id -ne $ExcludePid }
}
catch { }

$remaining += Get-Process | Where-Object {
    $_.Path -and $_.Path -like "*Erode*Tests*" -and $_.Id -ne $ExcludePid
} -ErrorAction SilentlyContinue

if ($remaining) {
    Write-Host "`n发现残留进程，强制结束..." -ForegroundColor Yellow
    foreach ($proc in $remaining) {
        try {
            Stop-Process -Id $proc.Id -Force -ErrorAction Stop
            Write-Host "  已强制终止进程 PID: $($proc.Id)" -ForegroundColor Green
        }
        catch {
            Write-Host "  无法强制终止进程 PID: $($proc.Id): $_" -ForegroundColor Red
        }
    }
}

Write-Host "`n清理完成！" -ForegroundColor Green

