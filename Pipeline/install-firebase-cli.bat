@echo off
REM ========================================
REM Instalador de Firebase CLI - AcuPuntos
REM ========================================
REM Este script instala Firebase CLI y configura el entorno
REM ========================================

echo.
echo ========================================
echo  INSTALACION DE FIREBASE CLI
echo ========================================
echo.

REM Verificar si Node.js esta instalado
where node >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Node.js no esta instalado
    echo.
    echo Por favor instala Node.js desde: https://nodejs.org/
    echo Despues de instalarlo, ejecuta este script nuevamente.
    pause
    exit /b 1
)

REM Mostrar version de Node.js
echo [INFO] Node.js detectado:
node --version
call npm --version
echo.

REM Instalar Firebase CLI
echo [INFO] Instalando Firebase CLI...
echo.
call npm install -g firebase-tools

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] La instalacion fallo
    pause
    exit /b 1
)

echo.
echo [EXITO] Firebase CLI instalado correctamente!
echo.

REM Verificar instalacion
echo [INFO] Verificando instalacion...
call firebase --version
echo.

REM Preguntar si desea autenticarse ahora
echo ========================================
set /p LOGIN="¿Deseas autenticarte en Firebase ahora? (S/N): "

if /i "%LOGIN%"=="S" (
    echo.
    echo [INFO] Iniciando proceso de autenticacion...
    echo Se abrira el navegador para que inicies sesion con tu cuenta de Google.
    echo.
    call firebase login
    
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo [EXITO] Autenticacion exitosa!
        echo.
        echo [INFO] Listando proyectos disponibles...
        call firebase projects:list
    ) else (
        echo.
        echo [ADVERTENCIA] La autenticacion fallo o fue cancelada.
        echo Puedes intentar nuevamente ejecutando: firebase login
    )
) else (
    echo.
    echo [INFO] Puedes autenticarte mas tarde ejecutando: firebase login
)

echo.
echo ========================================
echo  INSTALACION COMPLETADA
echo ========================================
echo.
echo Ahora puedes usar el pipeline de despliegue ejecutando: deploy.bat
echo.
pause
