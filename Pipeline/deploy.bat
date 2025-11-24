@echo off
REM ========================================
REM Pipeline de Despliegue Automatico - AcuPuntos
REM ========================================
REM Este script automatiza el proceso de build y despliegue a Firebase
REM Autor: Hernan Garcia
REM Fecha: 2025-11-24
REM ========================================

echo.
echo ========================================
echo  ACUPUNTOS - PIPELINE DE DESPLIEGUE
echo ========================================
echo.

REM Verificar si PowerShell esta disponible
where powershell >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] PowerShell no esta disponible en este sistema
    pause
    exit /b 1
)

REM Ejecutar el script PowerShell
echo [INFO] Ejecutando pipeline de despliegue...
powershell -ExecutionPolicy Bypass -File "%~dp0deploy.ps1"

REM Verificar el resultado
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] El despliegue fallo. Revisa los logs arriba.
    pause
    exit /b 1
) else (
    echo.
    echo [EXITO] Despliegue completado exitosamente!
    pause
    exit /b 0
)
