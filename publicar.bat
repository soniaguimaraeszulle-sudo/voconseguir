@echo off
chcp 65001 >nul
cls
echo ============================================================
echo  PUBLICADOR - Screen Panel (Gera executáveis standalone)
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
set OUTPUT_DIR=%ROOT_DIR%Publicado

echo [INFO] Criando diretório de saída: %OUTPUT_DIR%
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if not exist "%OUTPUT_DIR%\Cliente" mkdir "%OUTPUT_DIR%\Cliente"
if not exist "%OUTPUT_DIR%\Servidor" mkdir "%OUTPUT_DIR%\Servidor"

echo ============================================================
echo [1/2] Publicando ClienteScreen (standalone)...
echo ============================================================
cd /d "%ROOT_DIR%ClienteScreen"
dotnet publish ClienteScreen.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%OUTPUT_DIR%\Cliente"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERRO] Falha ao publicar ClienteScreen!
    echo.
    pause
    exit /b 1
)
echo [OK] ClienteScreen publicado com sucesso!
echo.

echo ============================================================
echo [2/2] Publicando ServidorScreenPanel (standalone)...
echo ============================================================
cd /d "%ROOT_DIR%ServidorScreenPanel"
dotnet publish ServidorScreenPanel.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%OUTPUT_DIR%\Servidor"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERRO] Falha ao publicar ServidorScreenPanel!
    echo.
    pause
    exit /b 1
)
echo [OK] ServidorScreenPanel publicado com sucesso!
echo.

cd /d "%ROOT_DIR%"

echo ============================================================
echo            PUBLICAÇÃO CONCLUÍDA COM SUCESSO!
echo ============================================================
echo.
echo Executáveis standalone criados em:
echo.
echo [Cliente]  Publicado\Cliente\ClienteScreen.exe
echo [Servidor] Publicado\Servidor\ServidorScreenPanel.exe
echo.
echo Estes executáveis podem ser distribuídos sem instalar .NET!
echo.
echo ============================================================

REM Abre o explorador de arquivos na pasta de saída
explorer "%OUTPUT_DIR%"

pause
