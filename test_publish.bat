@echo off
cd /d "%~dp0publish"
echo Testing published application...
echo.
if exist WpfDirectoryViewer.exe (
    echo Found WpfDirectoryViewer.exe in publish directory
    echo.
    echo Attempting to run the application...
    echo (If successful, the application window should appear)
    WpfDirectoryViewer.exe
    echo Application exited with code: %errorlevel%
) else (
    echo ERROR: WpfDirectoryViewer.exe not found in publish directory!
    echo Please make sure you have published the application correctly.
)
pause