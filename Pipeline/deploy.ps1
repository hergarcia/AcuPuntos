# ========================================
# Pipeline de Despliegue Automatico - AcuPuntos
# ========================================
# Este script automatiza el proceso completo de build y despliegue
# Incluye: incremento de version, build, firma y distribucion via Firebase
# Autor: Hernan Garcia
# Fecha: 2025-11-24
# ========================================

param(
    [string]$Configuration = "Release",
    [switch]$SkipBuild = $false,
    [switch]$SkipVersion = $false,
    [switch]$UploadToFirebase = $true,
    [switch]$Help
)

# Colores para output
$ErrorColor = "Red"
$SuccessColor = "Green"
$InfoColor = "Cyan"
$WarningColor = "Yellow"

# ========================================
# FUNCIONES AUXILIARES
# ========================================

function Write-StepHeader {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor $InfoColor
    Write-Host " $Message" -ForegroundColor $InfoColor
    Write-Host "========================================`n" -ForegroundColor $InfoColor
}

function Write-Success {
    param([string]$Message)
    Write-Host "[EXITO] $Message" -ForegroundColor $SuccessColor
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor $InfoColor
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[ADVERTENCIA] $Message" -ForegroundColor $WarningColor
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $ErrorColor
}

function Show-Help {
    Write-Host @"

PIPELINE DE DESPLIEGUE - AcuPuntos
===================================

USO:
    .\deploy.ps1 [-Configuration <Debug|Release>] [-SkipBuild] [-SkipVersion] [-UploadToFirebase] [-Help]

PARAMETROS:
    -Configuration      Configuracion de build (Debug o Release). Default: Release
    -SkipBuild         Omitir el proceso de build (usar APK existente)
    -SkipVersion       No incrementar la version automaticamente
    -UploadToFirebase  Subir el APK a Firebase App Distribution (Default: true)
    -Help              Mostrar esta ayuda

EJEMPLOS:
    .\deploy.ps1                                    # Build Release completo con subida a Firebase
    .\deploy.ps1 -Configuration Debug               # Build Debug
    .\deploy.ps1 -SkipBuild -UploadToFirebase       # Solo subir APK existente a Firebase
    .\deploy.ps1 -SkipVersion                       # Build sin incrementar version

"@
    exit 0
}

# ========================================
# CONFIGURACION
# ========================================

$ScriptRoot = $PSScriptRoot
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$ProjectFile = Join-Path $ProjectRoot "AcuPuntos.csproj"
$ConfigFile = Join-Path $ScriptRoot "firebase-deploy-config.json"
$DeployLogFile = Join-Path $ScriptRoot "deploy-log.txt"

# Verificar si existe el archivo de configuracion
if (-not (Test-Path $ConfigFile)) {
    Write-Warning "Archivo de configuracion no encontrado. Creando configuracion por defecto..."
    $defaultConfig = @{
        firebase_app_id = "YOUR_FIREBASE_APP_ID"
        firebase_token = "YOUR_FIREBASE_TOKEN_OR_USE_CLI_LOGIN"
        release_notes = "Nueva version con mejoras y correcciones"
        tester_groups = @("qa-team", "beta-testers")
        apk_output_path = "bin\Release\net10.0-android\com.hergarcia.acupuntos-Signed.apk"
    }
    $defaultConfig | ConvertTo-Json -Depth 10 | Set-Content $ConfigFile
    Write-Warning "Por favor, edita 'firebase-deploy-config.json' con tus credenciales de Firebase"
}

# ========================================
# VERIFICACIONES PREVIAS
# ========================================

function Test-Prerequisites {
    Write-StepHeader "Verificando Requisitos Previos"
    
    # Verificar si existe el proyecto
    if (-not (Test-Path $ProjectFile)) {
        Write-ErrorMsg "No se encontro el archivo del proyecto: $ProjectFile"
        exit 1
    }
    Write-Success "Archivo de proyecto encontrado"
    
    # Verificar .NET
    try {
        $dotnetVersion = & dotnet --version
        Write-Success ".NET instalado - Version: $dotnetVersion"
    } catch {
        Write-ErrorMsg ".NET no esta instalado o no esta en el PATH"
        exit 1
    }
    
    # Verificar Firebase CLI (opcional pero recomendado)
    try {
        $firebaseVersion = & firebase --version 2>$null
        Write-Success "Firebase CLI instalado - Version: $firebaseVersion"
        $script:FirebaseCLIAvailable = $true
    } catch {
        Write-Warning "Firebase CLI no esta instalado. Algunas funciones pueden no estar disponibles"
        Write-Info "Instala Firebase CLI con: npm install -g firebase-tools"
        $script:FirebaseCLIAvailable = $false
    }
}

# ========================================
# INCREMENTAR VERSION
# ========================================

function Update-Version {
    if ($SkipVersion) {
        Write-Info "Omitiendo incremento de version (flag -SkipVersion activado)"
        return
    }
    
    Write-StepHeader "Incrementando Version"
    
    try {
        [xml]$csproj = Get-Content $ProjectFile
        
        # Buscar ApplicationVersion en todos los PropertyGroup
        $versionNode = $null
        $displayVersionNode = $null
        $propertyGroupWithVersion = $null
        
        foreach ($propGroup in $csproj.Project.PropertyGroup) {
            if ($propGroup.ApplicationVersion) {
                $versionNode = $propGroup.ApplicationVersion
                $propertyGroupWithVersion = $propGroup
            }
            if ($propGroup.ApplicationDisplayVersion) {
                $displayVersionNode = $propGroup.ApplicationDisplayVersion
            }
        }
        
        if (-not $versionNode) {
            Write-ErrorMsg "No se encontro ApplicationVersion en el archivo .csproj"
            exit 1
        }
        
        # Obtener version actual
        $currentVersion = $versionNode
        $currentDisplayVersion = if ($displayVersionNode) { $displayVersionNode } else { "1.0" }
        
        Write-Info "Version actual: $currentDisplayVersion ($currentVersion)"
        
        # Incrementar version
        $newVersion = [int]$currentVersion + 1
        
        # Actualizar en el PropertyGroup correspondiente
        $propertyGroupWithVersion.ApplicationVersion = $newVersion.ToString()
        
        # Guardar cambios
        $csproj.Save($ProjectFile)
        
        Write-Success "Version actualizada a: $currentDisplayVersion ($newVersion)"
        
        $script:NewVersionCode = $newVersion
        $script:NewVersionName = $currentDisplayVersion
        
    } catch {
        Write-ErrorMsg "Error al actualizar la version: $_"
        exit 1
    }
}

# ========================================
# BUILD DEL PROYECTO
# ========================================

function Build-Project {
    if ($SkipBuild) {
        Write-Info "Omitiendo build (flag -SkipBuild activado)"
        return
    }
    
    Write-StepHeader "Compilando Proyecto - Configuracion: $Configuration"
    
    # Limpiar builds anteriores
    Write-Info "Limpiando builds anteriores..."
    & dotnet clean $ProjectFile -c $Configuration | Out-Null
    
    # Build del proyecto
    Write-Info "Iniciando build..."
    $buildOutput = & dotnet build $ProjectFile -c $Configuration -f net10.0-android 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMsg "Build fallido. Output:"
        Write-Host $buildOutput
        exit 1
    }
    
    Write-Success "Build completado exitosamente"
    
    # Guardar log
    $buildOutput | Out-File $DeployLogFile -Append
}

# ========================================
# PUBLICAR APK
# ========================================

function Publish-APK {
    Write-StepHeader "Generando APK de $Configuration"
    
    Write-Info "Publicando APK..."
    
    $publishOutput = & dotnet publish $ProjectFile -c $Configuration -f net10.0-android 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMsg "Publicacion fallida. Output:"
        Write-Host $publishOutput
        exit 1
    }
    
    Write-Success "APK generado exitosamente"
    
    # Buscar el APK generado
    $apkPattern = Join-Path $ProjectRoot "bin\$Configuration\net10.0-android\*.apk"
    $apkFiles = Get-ChildItem $apkPattern -ErrorAction SilentlyContinue
    
    if ($apkFiles.Count -eq 0) {
        Write-ErrorMsg "No se encontro el APK generado en: $apkPattern"
        exit 1
    }
    
    $script:GeneratedAPK = $apkFiles[0].FullName
    Write-Success "APK ubicado en: $($script:GeneratedAPK)"
    
    # Mostrar informacion del APK
    $apkSize = [math]::Round((Get-Item $script:GeneratedAPK).Length / 1MB, 2)
    Write-Info "Tamano del APK: $apkSize MB"
    
    # Guardar log
    $publishOutput | Out-File $DeployLogFile -Append
}

# ========================================
# SUBIR A FIREBASE APP DISTRIBUTION
# ========================================

function Upload-ToFirebase {
    if (-not $UploadToFirebase) {
        Write-Info "Omitiendo subida a Firebase (flag -UploadToFirebase no activado)"
        return
    }
    
    Write-StepHeader "Subiendo a Firebase App Distribution"
    
    if (-not $script:FirebaseCLIAvailable) {
        Write-Warning "Firebase CLI no esta disponible. Saltando subida a Firebase"
        Write-Info "Para habilitar esta funcionalidad, instala Firebase CLI:"
        Write-Info "  npm install -g firebase-tools"
        Write-Info "Y luego autenticate con: firebase login"
        return
    }
    
    # Cargar configuracion
    try {
        $config = Get-Content $ConfigFile | ConvertFrom-Json
    } catch {
        Write-ErrorMsg "Error al leer el archivo de configuracion: $_"
        return
    }
    
    # Verificar que el APK exista
    if (-not (Test-Path $script:GeneratedAPK)) {
        Write-ErrorMsg "APK no encontrado: $($script:GeneratedAPK)"
        return
    }
    
    # Preparar release notes
    $releaseNotes = $config.release_notes
    if ($script:NewVersionName) {
        $releaseNotes = "Version $($script:NewVersionName) ($($script:NewVersionCode))`n`n$releaseNotes"
    }
    
    Write-Info "Subiendo APK a Firebase App Distribution..."
    Write-Info "App ID: $($config.firebase_app_id)"
    Write-Info "Release Notes: $releaseNotes"
    
    # Comando de Firebase
    $firebaseArgs = @(
        "appdistribution:distribute",
        "`"$($script:GeneratedAPK)`"",
        "--app", $config.firebase_app_id,
        "--release-notes", "`"$releaseNotes`""
    )
    
    # Agregar grupos de testers si estan configurados
    if ($config.tester_groups -and $config.tester_groups.Count -gt 0) {
        $groups = $config.tester_groups -join ","
        $firebaseArgs += "--groups", $groups
        Write-Info "Grupos de testers: $groups"
    }
    
    # Ejecutar comando
    try {
        Write-Info "Ejecutando: firebase $($firebaseArgs -join ' ')"
        
        $firebaseOutput = & firebase @firebaseArgs 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "APK subido exitosamente a Firebase App Distribution!"
            Write-Host $firebaseOutput
        } else {
            Write-Warning "Subida a Firebase fallo. Posibles razones:"
            Write-Warning "  1. No estas autenticado: ejecuta 'firebase login'"
            Write-Warning "  2. App ID incorrecto en firebase-deploy-config.json"
            Write-Warning "  3. No tienes permisos en el proyecto de Firebase"
            Write-Host $firebaseOutput
        }
        
        $firebaseOutput | Out-File $DeployLogFile -Append
        
    } catch {
        Write-Warning "Error al subir a Firebase: $_"
    }
}

# ========================================
# RESUMEN FINAL
# ========================================

function Show-Summary {
    Write-StepHeader "Resumen del Despliegue"
    
    Write-Host "Configuracion:         $Configuration" -ForegroundColor White
    
    if ($script:NewVersionName) {
        Write-Host "Version:               $($script:NewVersionName) ($($script:NewVersionCode))" -ForegroundColor White
    }
    
    if ($script:GeneratedAPK) {
        Write-Host "APK Generado:          $($script:GeneratedAPK)" -ForegroundColor White
        $apkSize = [math]::Round((Get-Item $script:GeneratedAPK).Length / 1MB, 2)
        Write-Host "Tamano:                $apkSize MB" -ForegroundColor White
    }
    
    Write-Host "Log guardado en:       $DeployLogFile" -ForegroundColor White
    
    Write-Host "`n" -NoNewline
    Write-Success "DESPLIEGUE COMPLETADO EXITOSAMENTE!"
    
    # Abrir la carpeta del APK
    if ($script:GeneratedAPK -and (Test-Path $script:GeneratedAPK)) {
        $openFolder = Read-Host "`n¿Deseas abrir la carpeta del APK? (S/N)"
        if ($openFolder -eq "S" -or $openFolder -eq "s") {
            explorer (Split-Path $script:GeneratedAPK -Parent)
        }
    }
}

# ========================================
# MAIN - EJECUCION PRINCIPAL
# ========================================

function Main {
    # Mostrar ayuda si se solicita
    if ($Help) {
        Show-Help
    }
    
    # Iniciar log
    "========================================" | Out-File $DeployLogFile
    "Pipeline de Despliegue - AcuPuntos" | Out-File $DeployLogFile -Append
    "Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File $DeployLogFile -Append
    "Configuracion: $Configuration" | Out-File $DeployLogFile -Append
    "========================================`n" | Out-File $DeployLogFile -Append
    
    Write-Host "`n"
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host "  ACUPUNTOS - PIPELINE DE DESPLIEGUE   " -ForegroundColor Magenta
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host "`n"
    
    try {
        # Ejecutar pasos del pipeline
        Test-Prerequisites
        Update-Version
        Build-Project
        Publish-APK
        Upload-ToFirebase
        Show-Summary
        
        exit 0
        
    } catch {
        Write-ErrorMsg "Error fatal en el pipeline: $_"
        Write-ErrorMsg $_.ScriptStackTrace
        exit 1
    }
}

# Ejecutar main
Main
