@echo off
setlocal

pushd "%~dp0"

echo Publishing WebApp to .\publish-local ...
echo.

tasklist /FI "IMAGENAME eq WebApp.exe" | find /I "WebApp.exe" >nul
if %ERRORLEVEL%==0 (
    echo A WebApp process appears to be running.
    echo Please stop it before publishing to avoid file-lock errors.
    echo.
    pause
)

dotnet publish .\WebApp\WebApp.csproj --configuration Release --output .\publish-local
if %ERRORLEVEL% neq 0 (
    echo.
    echo Publish failed.
    popd
    pause
    exit /b 1
)

echo.
echo Publish complete: .\publish-local
popd

echo Running WebApp from .\publish-local ...
cd .\publish-local
.\WebApp.exe --urls "https://localhost:5001;http://localhost:5000"

exit /b 0
