@echo off
REM 强制结束所有 Erode.Tests 基准测试相关进程
REM 用于解决文件锁定问题

echo 正在查找 Erode.Tests 相关进程...

REM 查找并结束所有 Erode.Tests.exe 进程
taskkill /F /IM "Erode.Tests.exe" 2>nul
if %errorlevel% equ 0 (
    echo 已终止 Erode.Tests.exe 进程
) else (
    echo 未找到 Erode.Tests.exe 进程
)

REM 查找并结束所有 dotnet 进程（可能正在运行基准测试）
REM 注意：这会结束所有 dotnet 进程，请谨慎使用
REM 如果需要更精确的过滤，可以使用 PowerShell 脚本

timeout /t 1 /nobreak >nul

echo.
echo 清理完成！
pause


