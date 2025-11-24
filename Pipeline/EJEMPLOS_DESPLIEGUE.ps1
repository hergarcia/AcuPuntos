# ========================================
# EJEMPLOS DE USO DEL PIPELINE - AcuPuntos
# ========================================
# Este archivo contiene ejemplos de uso del pipeline
# para diferentes escenarios
# ========================================

# ========================================
# EJEMPLO 1: Despliegue Diario a Testers
# ========================================
# Este comando genera un build de Release y lo distribuye
# a todos los grupos de testers configurados

.\deploy.bat

# ========================================
# EJEMPLO 2: Build Local para Pruebas
# ========================================
# Genera un APK de Debug sin subirlo a Firebase
# Útil para probar localmente antes de distribuir

.\deploy.ps1 -Configuration Debug -UploadToFirebase:$false

# ========================================
# EJEMPLO 3: Actualización Rápida
# ========================================
# Reutiliza el APK compilado anteriormente
# Solo actualiza la distribución en Firebase

.\deploy.ps1 -SkipBuild -SkipVersion

# ========================================
# EJEMPLO 4: Build sin Incrementar Versión
# ========================================
# Útil cuando haces múltiples builds del mismo código

.\deploy.ps1 -SkipVersion

# ========================================
# EJEMPLO 5: Workflow Completo Manual
# ========================================

# Paso 1: Editar release notes
notepad firebase-deploy-config.json

# Paso 2: Build local para verificar
.\deploy.ps1 -Configuration Release -UploadToFirebase:$false

# Paso 3: Probar el APK localmente (instalarlo en tu dispositivo)
# El APK estará en: bin\Release\net10.0-android\

# Paso 4: Si todo está OK, subir a Firebase
.\deploy.ps1 -SkipBuild

# ========================================
# EJEMPLO 6: Automatización con Programador
# ========================================
# Puedes usar el Programador de Tareas de Windows para
# ejecutar despliegues automáticos

# 1. Abrir "Programador de tareas" (taskschd.msc)
# 2. Crear nueva tarea
# 3. Trigger: Diario a las 18:00
# 4. Action: Ejecutar deploy.bat
# 5. Configurar para ejecutar aunque el usuario no esté conectado

# ========================================
# EJEMPLO 7: Despliegue con Diferentes Grupos
# ========================================

# Para deployment interno (solo QA)
# Edita firebase-deploy-config.json:
{
  "tester_groups": ["qa-team"]
}
# Luego ejecuta:
.\deploy.bat

# Para deployment a beta testers
# Edita firebase-deploy-config.json:
{
  "tester_groups": ["beta-testers", "early-adopters"]
}
# Luego ejecuta:
.\deploy.bat

# Para deployment a todos
# Edita firebase-deploy-config.json:
{
  "tester_groups": ["qa-team", "beta-testers", "amigos", "early-adopters"]
}
# Luego ejecuta:
.\deploy.bat

# ========================================
# EJEMPLO 8: Pipeline para Hotfix
# ========================================
# Cuando necesitas distribuir un fix urgente

# 1. Hacer los cambios necesarios en el código
# 2. Compilar y distribuir inmediatamente
.\deploy.ps1 -Configuration Release

# 3. Editar release notes para indicar que es un hotfix
notepad firebase-deploy-config.json
# Cambiar a algo como:
# "release_notes": "🚨 HOTFIX: Corrección urgente de bug crítico en login"

# 4. Re-distribuir con las notas actualizadas
.\deploy.ps1 -SkipBuild

# ========================================
# EJEMPLO 9: Validación Completa antes de Despliegue
# ========================================

# Paso 1: Limpiar todo
dotnet clean

# Paso 2: Restaurar paquetes
dotnet restore

# Paso 3: Build de Debug para verificar que compila
.\deploy.ps1 -Configuration Debug -UploadToFirebase:$false

# Paso 4: Si Debug funciona, hacer Release
.\deploy.ps1 -Configuration Release -UploadToFirebase:$false

# Paso 5: Validar el APK manualmente

# Paso 6: Si todo OK, distribuir
.\deploy.ps1 -SkipBuild

# ========================================
# EJEMPLO 10: Script de Despliegue con Confirmación
# ========================================

# Crear un archivo deploy-with-confirmation.bat:

@echo off
echo ========================================
echo DESPLIEGUE CON CONFIRMACION
echo ========================================
echo.
echo Esta a punto de:
echo - Incrementar la version de la app
echo - Compilar en modo Release
echo - Distribuir a Firebase App Distribution
echo.
set /p CONFIRM="¿Desea continuar? (S/N): "

if /i "%CONFIRM%"=="S" (
    .\deploy.bat
) else (
    echo Despliegue cancelado.
    pause
)

# ========================================
# EJEMPLO 11: Despliegue Multi-Configuración
# ========================================

# Generar múltiples builds para diferentes propósitos

# Build para QA (Debug)
.\deploy.ps1 -Configuration Debug -UploadToFirebase:$false
# Renombrar APK
copy "bin\Debug\net10.0-android\*.apk" "builds\AcuPuntos-Debug-QA.apk"

# Build para Staging (Release sin optimizaciones)
.\deploy.ps1 -Configuration Release -UploadToFirebase:$false
# Renombrar APK
copy "bin\Release\net10.0-android\*.apk" "builds\AcuPuntos-Release-Staging.apk"

# Build para Producción (con APK anterior subido a Firebase)
.\deploy.ps1 -SkipBuild

# ========================================
# NOTAS IMPORTANTES
# ========================================

# 1. Siempre prueba el APK localmente antes de distribuir
# 2. Mantén actualizadas las release notes
# 3. Verifica que los grupos de testers estén correctos
# 4. Guarda logs de cada despliegue (se guardan automáticamente en deploy-log.txt)
# 5. Documenta cambios importantes en tu control de versiones (Git)

# ========================================
# TROUBLESHOOTING RÁPIDO
# ========================================

# Si Firebase CLI no funciona:
firebase login --reauth

# Si el build falla:
dotnet clean
rd /s /q bin obj
.\deploy.bat

# Si necesitas ver logs detallados:
type deploy-log.txt

# Si quieres ver todas las versiones en Firebase:
# Ve a: https://console.firebase.google.com → App Distribution → Releases
