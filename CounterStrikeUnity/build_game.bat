@echo off
echo ========================================
echo Counter Strike Unity - 自动构建脚本
echo ========================================
echo.

REM 设置Unity编辑器路径 (请根据实际安装路径修改)
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2021.3.25f1\Editor\Unity.exe"

REM 检查Unity是否存在
if not exist %UNITY_PATH% (
    echo 错误: 找不到Unity编辑器，请检查路径设置
    echo 当前设置路径: %UNITY_PATH%
    echo.
    echo 请在脚本中修改UNITY_PATH变量为正确的Unity安装路径
    pause
    exit /b 1
)

REM 设置项目路径
set PROJECT_PATH=%~dp0
echo 项目路径: %PROJECT_PATH%

REM 设置构建输出路径
set BUILD_PATH=%PROJECT_PATH%Build
echo 构建输出路径: %BUILD_PATH%

REM 创建构建目录
if not exist "%BUILD_PATH%" (
    mkdir "%BUILD_PATH%"
    echo 创建构建目录: %BUILD_PATH%
)

echo.
echo 开始构建Counter Strike Unity...
echo 这可能需要几分钟时间，请耐心等待...
echo.

REM 执行Unity构建命令
%UNITY_PATH% -batchmode -quit -projectPath "%PROJECT_PATH%" -buildWindows64Player "%BUILD_PATH%\CounterStrikeUnity.exe" -logFile "%BUILD_PATH%\build.log"

REM 检查构建结果
if exist "%BUILD_PATH%\CounterStrikeUnity.exe" (
    echo.
    echo ========================================
    echo 构建成功完成！
    echo ========================================
    echo.
    echo 游戏文件位置: %BUILD_PATH%\CounterStrikeUnity.exe
    echo 构建日志: %BUILD_PATH%\build.log
    echo.
    
    REM 询问是否运行游戏
    set /p run_game="是否立即运行游戏? (y/n): "
    if /i "%run_game%"=="y" (
        echo 启动游戏...
        start "" "%BUILD_PATH%\CounterStrikeUnity.exe"
    )
    
    REM 询问是否打开构建文件夹
    set /p open_folder="是否打开构建文件夹? (y/n): "
    if /i "%open_folder%"=="y" (
        explorer "%BUILD_PATH%"
    )
    
) else (
    echo.
    echo ========================================
    echo 构建失败！
    echo ========================================
    echo.
    echo 请检查构建日志: %BUILD_PATH%\build.log
    echo 常见问题:
    echo 1. 确保Unity项目没有编译错误
    echo 2. 检查所有必要的场景是否添加到Build Settings
    echo 3. 确保项目设置正确
    echo.
    
    REM 询问是否查看日志
    set /p view_log="是否查看构建日志? (y/n): "
    if /i "%view_log%"=="y" (
        if exist "%BUILD_PATH%\build.log" (
            notepad "%BUILD_PATH%\build.log"
        ) else (
            echo 日志文件不存在
        )
    )
)

echo.
echo 按任意键退出...
pause >nul