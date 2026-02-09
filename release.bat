@echo off
SET MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe"

echo Building Redmine Monitor Service (Release)...
%MSBUILD% RedmineMonitorService\RedmineMonitorService.vbproj /p:Configuration=Release /t:Rebuild /v:minimal /nologo

IF %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b 1
)

echo.
echo Build completed successfully!
echo Output: RedmineMonitorService\bin\Release\RedmineMonitorService.exe
