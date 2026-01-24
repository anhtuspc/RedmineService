@echo off
REM Build script for Redmine Monitor Service

SET MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe"

echo Building Redmine Monitor Service...
%MSBUILD% RedmineMonitorService\RedmineMonitorService.vbproj /p:Configuration=Debug /v:minimal /nologo

IF %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b 1
)

echo.
echo Building Test Project...
%MSBUILD% RedmineMonitorService.Tests\RedmineMonitorService.Tests.vbproj /p:Configuration=Debug /v:minimal /nologo

IF %ERRORLEVEL% NEQ 0 (
    echo Test build failed!
    exit /b 1
)

echo.
echo Build completed successfully!
echo Output: RedmineMonitorService\bin\Debug\RedmineMonitorService.exe
