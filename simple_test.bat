@echo off
cd /d "%~dp0publish"
echo Current directory: %cd%
echo Checking if WpfDirectoryViewer.exe exists...
if exist WpfDirectoryViewer.exe (
    echo Found WpfDirectoryViewer.exe
    echo Running program...
    WpfDirectoryViewer.exe > output.log 2> error.log
    set EXIT_CODE=%ERRORLEVEL%
    echo Program exited with code: %EXIT_CODE%
    echo.
    echo Output log preview:
    powershell -NoProfile -Command "Get-Content -Path output.log -TotalCount 5"
    echo.
    echo Error log preview:
    powershell -NoProfile -Command "Get-Content -Path error.log -TotalCount 5"
) else (
    echo ERROR: WpfDirectoryViewer.exe not found in publish directory!
    dir
)
pause