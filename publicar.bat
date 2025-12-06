@echo off
chcp 65001 >nul
cls
echo ============================================================
echo  PUBLICADOR STANDALONE - NÃO PRECISA .NET INSTALADO
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
echo [1/2] Publicando ClienteScreen (standalone - NÃO precisa .NET)
echo ============================================================
cd /d "%ROOT_DIR%ClienteScreen"
echo [INFO] Limpando cache de build do ClienteScreen...
dotnet clean ClienteScreen.csproj -c Release >nul 2>&1
echo [OK] Cache limpo!
echo [INFO] Iniciando publicação...
dotnet publish ClienteScreen.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "%OUTPUT_DIR%\Cliente"
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
echo [2/2] Publicando ServidorScreenPanel (standalone - NÃO precisa .NET)
echo ============================================================
cd /d "%ROOT_DIR%ServidorScreenPanel"
echo [INFO] Limpando cache de build do ServidorScreenPanel...
dotnet clean ServidorScreenPanel.csproj -c Release >nul 2>&1
echo [OK] Cache limpo!
echo [INFO] Iniciando publicação...
dotnet publish ServidorScreenPanel.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "%OUTPUT_DIR%\Servidor"
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
echo Executáveis STANDALONE criados (NÃO precisam .NET instalado):
echo.
echo [Cliente]  Publicado\Cliente\ClienteScreen.exe
echo [Servidor] Publicado\Servidor\ServidorScreenPanel.exe
echo.
echo IMPORTANTE: Estes executáveis incluem o runtime .NET completo.
echo             Podem ser executados em qualquer Windows SEM instalar .NET!
echo.
echo Tamanho aproximado: 60-80 MB cada
echo.
echo ============================================================

REM Limpar arquivos desnecessários
echo [INFO] Limpando arquivos temporários...
del /Q "%OUTPUT_DIR%\Cliente\*.pdb" 2>nul
del /Q "%OUTPUT_DIR%\Servidor\*.pdb" 2>nul
echo [OK] Limpeza concluída!
echo.

REM Abre o explorador de arquivos na pasta de saída
explorer "%OUTPUT_DIR%"

pause
