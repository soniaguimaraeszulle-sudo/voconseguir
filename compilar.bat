@echo off
chcp 65001 >nul
cls
echo ============================================================
echo    COMPILADOR - Screen Panel (Servidor, Cliente e Hook)
echo ============================================================
echo.

REM Verifica se dotnet está instalado
where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [ERRO] .NET SDK não encontrado!
    echo Por favor, instale o .NET 8.0 SDK em: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

echo [INFO] .NET SDK encontrado:
dotnet --version
echo.

REM Diretório raiz do projeto
set ROOT_DIR=%~dp0

echo ============================================================
echo [1/3] Compilando Hook (biblioteca)...
echo ============================================================
cd /d "%ROOT_DIR%Hook"
dotnet build Hook.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERRO] Falha ao compilar Hook!
    echo.
    pause
    exit /b 1
)
echo [OK] Hook compilado com sucesso!
echo.

echo ============================================================
echo [2/3] Compilando ClienteScreen (aplicação cliente)...
echo ============================================================
cd /d "%ROOT_DIR%ClienteScreen"
dotnet build ClienteScreen.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERRO] Falha ao compilar ClienteScreen!
    echo.
    pause
    exit /b 1
)
echo [OK] ClienteScreen compilado com sucesso!
echo.

echo ============================================================
echo [3/3] Compilando ServidorScreenPanel (aplicação servidor)...
echo ============================================================
cd /d "%ROOT_DIR%ServidorScreenPanel"
dotnet build ServidorScreenPanel.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERRO] Falha ao compilar ServidorScreenPanel!
    echo.
    pause
    exit /b 1
)
echo [OK] ServidorScreenPanel compilado com sucesso!
echo.

cd /d "%ROOT_DIR%"

echo ============================================================
echo                COMPILAÇÃO CONCLUÍDA COM SUCESSO!
echo ============================================================
echo.
echo Arquivos compilados em modo Release:
echo.
echo [Cliente]  ClienteScreen\bin\Release\net8.0-windows\ClienteScreen.exe
echo [Servidor] ServidorScreenPanel\bin\Release\net8.0-windows\ServidorScreenPanel.exe
echo [Hook]     Hook\bin\Release\net8.0-windows\Hook.dll
echo.
echo ============================================================
pause
