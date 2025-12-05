@echo off
chcp 65001 >nul
cls
echo ============================================================
echo           LIMPEZA - Remover arquivos compilados
echo ============================================================
echo.
echo Este script irá remover:
echo  - Pasta bin/ de todos os projetos
echo  - Pasta obj/ de todos os projetos
echo  - Pasta Publicado/ (executáveis standalone)
echo.
echo Deseja continuar? (S/N)
set /p CONFIRM=
if /i not "%CONFIRM%"=="S" (
    echo.
    echo [CANCELADO] Limpeza cancelada pelo usuário.
    pause
    exit /b 0
)

echo.
echo [INFO] Removendo arquivos compilados...
echo.

set ROOT_DIR=%~dp0

REM Remover bin e obj do Hook
if exist "%ROOT_DIR%Hook\bin" (
    echo [REMOVENDO] Hook\bin
    rmdir /s /q "%ROOT_DIR%Hook\bin"
)
if exist "%ROOT_DIR%Hook\obj" (
    echo [REMOVENDO] Hook\obj
    rmdir /s /q "%ROOT_DIR%Hook\obj"
)

REM Remover bin e obj do ClienteScreen
if exist "%ROOT_DIR%ClienteScreen\bin" (
    echo [REMOVENDO] ClienteScreen\bin
    rmdir /s /q "%ROOT_DIR%ClienteScreen\bin"
)
if exist "%ROOT_DIR%ClienteScreen\obj" (
    echo [REMOVENDO] ClienteScreen\obj
    rmdir /s /q "%ROOT_DIR%ClienteScreen\obj"
)

REM Remover bin e obj do ServidorScreenPanel
if exist "%ROOT_DIR%ServidorScreenPanel\bin" (
    echo [REMOVENDO] ServidorScreenPanel\bin
    rmdir /s /q "%ROOT_DIR%ServidorScreenPanel\bin"
)
if exist "%ROOT_DIR%ServidorScreenPanel\obj" (
    echo [REMOVENDO] ServidorScreenPanel\obj
    rmdir /s /q "%ROOT_DIR%ServidorScreenPanel\obj"
)

REM Remover pasta Publicado
if exist "%ROOT_DIR%Publicado" (
    echo [REMOVENDO] Publicado\
    rmdir /s /q "%ROOT_DIR%Publicado"
)

echo.
echo ============================================================
echo            LIMPEZA CONCLUÍDA COM SUCESSO!
echo ============================================================
echo.
pause
