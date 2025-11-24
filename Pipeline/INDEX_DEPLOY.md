# 🚀 Pipeline de Despliegue Automático - AcuPuntos

## ✅ INSTALACIÓN COMPLETADA

Se han creado exitosamente **8 archivos** para el pipeline de despliegue automático:

---

## 📦 ARCHIVOS CREADOS

### 🎯 ARCHIVOS EJECUTABLES (Principales)

```
📁 AcuPuntos/
│
├── 🚀 deploy.bat                      ← ¡EJECUTA ESTE!
├── ⚙️  deploy.ps1                      ← Versión avanzada con opciones
└── 🔧 install-firebase-cli.bat        ← Instalador de Firebase CLI
```

### 📋 ARCHIVOS DE CONFIGURACIÓN

```
📁 AcuPuntos/
│
├── ⚙️  firebase-deploy-config.json    ← Configura aquí tu App ID
└── 📝 .gitignore                      ← Actualizado con entradas del pipeline
```

### 📚 DOCUMENTACIÓN

```
📁 AcuPuntos/
│
├── 📖 README_DEPLOY.md                ← Documentación completa
├── 📄 INDEX_DEPLOY.md                 ← Índice general
├── 📊 GUIA_RAPIDA.txt                 ← Referencia rápida (ASCII)
└── 💡 EJEMPLOS_DESPLIEGUE.ps1         ← Ejemplos de uso
```

---

## 🎬 PRIMEROS PASOS

### 1️⃣  INSTALACIÓN INICIAL (Solo Primera Vez)

```powershell
# Paso 1: Instalar Firebase CLI
.\install-firebase-cli.bat

# Paso 2: Editar configuración
notepad firebase-deploy-config.json

# Cambiar estas líneas:
"firebase_app_id": "TU_APP_ID_AQUI"
"tester_groups": ["tus-grupos-aqui"]
```

**¿Dónde obtener tu App ID?**
1. Ve a https://console.firebase.google.com
2. Selecciona tu proyecto AcuPuntos
3. Project Settings → Your apps
4. Copia el App ID (formato: `1:123456789:android:abcdef123456`)

---

### 2️⃣  USO DIARIO

```powershell
# Simplemente ejecuta:
.\deploy.bat

# O si prefieres PowerShell:
.\deploy.ps1
```

**El pipeline automáticamente:**
- ✅ Incrementa la versión de la app
- ✅ Compila el proyecto en modo Release
- ✅ Genera el APK firmado
- ✅ Sube el APK a Firebase App Distribution
- ✅ Notifica a tus grupos de testers

---

## 🎯 COMANDOS ÚTILES

```powershell
# Despliegue completo (más común)
.\deploy.bat

# Build local sin subir a Firebase
.\deploy.ps1 -UploadToFirebase:$false

# Build de Debug para pruebas
.\deploy.ps1 -Configuration Debug -UploadToFirebase:$false

# Solo subir APK existente
.\deploy.ps1 -SkipBuild

# Ver todas las opciones
.\deploy.ps1 -Help
```

---

## 📊 FLUJO DEL PIPELINE

```
┌─────────────────┐
│ 1. CÓDIGO LISTO │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 2. EDITAR NOTES │  ← Editar firebase-deploy-config.json
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 3. DEPLOY.BAT   │  ← Doble click o ejecutar
└────────┬────────┘
         │
         ▼
   ┌─────────────────────────────────┐
   │  PIPELINE AUTOMÁTICO            │
   ├─────────────────────────────────┤
   │ ✓ Verificar requisitos          │
   │ ✓ Incrementar versión           │
   │ ✓ Limpiar build anterior        │
   │ ✓ Compilar proyecto (Release)   │
   │ ✓ Generar APK firmado           │
   │ ✓ Subir a Firebase              │
   │ ✓ Notificar testers             │
   │ ✓ Guardar logs                  │
   └────────┬────────────────────────┘
            │
            ▼
   ┌─────────────────┐
   │ 4. APK EN       │
   │    FIREBASE     │
   └────────┬────────┘
            │
            ▼
   ┌─────────────────┐
   │ 5. TESTERS      │
   │    NOTIFICADOS  │
   └─────────────────┘
```

---

## 🔧 CONFIGURACIÓN DE firebase-deploy-config.json

```json
{
  "firebase_app_id": "1:123456789:android:abcdef123456",
  
  "release_notes": "Version 1.2.0\n\n✨ Nuevas funcionalidades:\n- Dark mode\n- Agenda de citas\n\n🐛 Correcciones:\n- Fix barra de estado Android",
  
  "tester_groups": [
    "qa-team",
    "beta-testers",
    "amigos"
  ]
}
```

**Campos importantes:**
- `firebase_app_id`: **OBLIGATORIO** - Tu App ID de Firebase
- `release_notes`: Descripción de cambios (soporta `\n` para saltos de línea)
- `tester_groups`: Grupos que recibirán el APK

---

## 📖 DOCUMENTACIÓN

### 📘 Para empezar:
```
INDEX_DEPLOY.md  ← ¡Empieza aquí! (este archivo)
```

### 📗 Referencia completa:
```
README_DEPLOY.md  ← Instrucciones detalladas
```

### 📙 Referencia rápida:
```
GUIA_RAPIDA.txt  ← Consulta rápida en formato texto
```

### 📕 Ejemplos prácticos:
```
EJEMPLOS_DESPLIEGUE.ps1  ← Casos de uso específicos
```

---

## 🐛 PROBLEMAS COMUNES

### ❌ "Firebase CLI no está disponible"
```powershell
# Solución:
.\install-firebase-cli.bat
```

### ❌ "App ID incorrecto" o "Unauthorized"
```powershell
# 1. Verifica el App ID en firebase-deploy-config.json
# 2. Re-autentícate:
firebase login --reauth
```

### ❌ "Build fallido"
```powershell
# Limpiar y reconstruir:
dotnet clean
rd /s /q bin obj
.\deploy.bat
```

### ❌ Ver logs de error
```powershell
# El pipeline guarda logs en:
type deploy-log.txt
```

---

## ✅ CHECKLIST PRE-DESPLIEGUE

Antes de ejecutar `deploy.bat`:

- [ ] ✅ Firebase CLI instalado (`firebase --version`)
- [ ] ✅ Autenticado en Firebase (`firebase login`)
- [ ] ✅ `firebase-deploy-config.json` editado
- [ ] ✅ App ID configurado
- [ ] ✅ Grupos de testers creados en Firebase Console
- [ ] ✅ Código probado localmente
- [ ] ✅ Release notes actualizadas
- [ ] ✅ Git commit realizado (backup)

---

## 🎯 SIGUIENTE PASO

### 👉 **Configuración Inicial**

```powershell
# 1. Instala Firebase CLI
.\install-firebase-cli.bat

# 2. Edita la configuración
notepad firebase-deploy-config.json

# 3. Lee la documentación completa
# Abre: README_DEPLOY.md
```

---

## 🎉 ¡YA ESTÁS LISTO!

Una vez configurado, desplegar nuevas versiones es tan simple como:

```powershell
.\deploy.bat
```

**El pipeline hace todo el resto automáticamente** ✨

---

## 📞 RECURSOS

- 🔥 **Firebase Console**: https://console.firebase.google.com
- 📚 **Firebase CLI Docs**: https://firebase.google.com/docs/cli  
- 🛠️ **.NET MAUI Docs**: https://learn.microsoft.com/dotnet/maui
- 📖 **Documentación Local**: `README_DEPLOY.md`

---

## 📝 VERSIÓN DEL PIPELINE

**Versión**: 1.0  
**Fecha**: 2025-11-24  
**Autor**: Hernan Garcia  
**Proyecto**: AcuPuntos  

**Características v1.0:**
- ✅ Pipeline automatizado completo
- ✅ Integración Firebase App Distribution
- ✅ Incremento automático de versiones
- ✅ Generación APK firmado
- ✅ Distribución a grupos de testers
- ✅ Logging detallado
- ✅ Documentación completa
- ✅ Scripts de instalación

---

## 🌟 CARACTERÍSTICAS DESTACADAS

### 🚀 **Totalmente Automatizado**
Un solo comando para todo el proceso de despliegue

### 🔄 **Versionado Automático**
Incrementa el build number automáticamente

### 📱 **Distribución Instantánea**
Los testers reciben notificación automática

### 📊 **Logging Completo**
Todos los despliegues quedan registrados

### 🎯 **Flexible**
Múltiples opciones y configuraciones

### 📖 **Bien Documentado**
Documentación extensa y ejemplos

---

## 💡 TIPS FINALES

1. **Prueba localmente primero**: Usa `-UploadToFirebase:$false`
2. **Actualiza release notes**: Los testers lo agradecerán
3. **Usa diferentes grupos**: Separa QA de producción
4. **Guarda backups**: Mantén APKs importantes
5. **Revisa Firebase Console**: Verifica cada despliegue
6. **Lee la documentación**: `README_DEPLOY.md` tiene todo

---

**¡Feliz Despliegue! 🎉**

Si tienes dudas, consulta **`README_DEPLOY.md`** o **`GUIA_RAPIDA.txt`**

---

**Creado con ❤️ para AcuPuntos**
