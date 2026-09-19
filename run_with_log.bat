@echo off
cd /d "%~dp0publish"
echo Running WpfDirectoryViewer.exe from publish directory... > run_log.txt
echo Timestamp: %date% %time% >> run_log.txt
echo. >> run_log.txt
echo System info: >> run_log.txt
echo OS: %OS% >> run_log.txt
echo Architecture: %PROCESSOR_ARCHITECTURE% >> run_log.txt
echo. >> run_log.txt
if exist WpfDirectoryViewer.exe (
    echo Executable found, starting application... >> run_log.txt
    echo. >> run_log.txt
    WpfDirectoryViewer.exe 2>> run_log.txt
    echo Exit code: %errorlevel% >> run_log.txt
) else (
    echo ERROR: WpfDirectoryViewer.exe not found in publish directory! >> run_log.txt
    echo Check if the application was properly published. >> run_log.txt
    echo Exit code: 1 >> run_log.txt
)
echo. >> run_log.txt
echo Press any key to view log...
pause >nul
type run_log.txt
pause