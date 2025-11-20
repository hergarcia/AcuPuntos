# AcuPuntos - Sistema de Puntos para Acupuntura

Sistema de gestión de puntos y recompensas para un emprendimiento de acupuntura, desarrollado en .NET MAUI.

## 📱 Características

- **Login con Google** mediante Firebase Authentication
- **Sistema de puntos** acumulables
- **Transferencias** de puntos entre usuarios
- **Catálogo de recompensas** canjeables
- **Panel de administración** para gestión
- **Actualización en tiempo real** con Firestore
- **Diseño moderno** con paleta verde y minimalista
- **Multiplataforma** (Android e iOS)

## 🚀 Configuración del Proyecto

### Prerrequisitos

- .NET 10.0 SDK
- Visual Studio 2022 o VS Code
- Cuenta de Firebase
- Android SDK (para Android)
- Xcode (para iOS en Mac)

### 1. Configurar Firebase

#### Crear proyecto en Firebase Console

1. Ve a [Firebase Console](https://console.firebase.google.com)
2. Crea un nuevo proyecto llamado "AcuPuntos"
3. Habilita Google Analytics (opcional)

#### Configurar para Android

1. **Agregar app Android**
   - Click en "Agregar app" → Android
   - Package name: `com.acupuntura.acupuntos`
   - Registrar la app

2. **Descargar google-services.json**
   - Descarga el archivo `google-services.json`
   - Colócalo en: `/Platforms/Android/google-services.json`
   - En las propiedades del archivo, marca "GoogleServicesJson" como Build Action

3. **Configurar SHA-1** (necesario para Google Sign-In)
   ```bash
   keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android
   ```
   - Copia el SHA-1 y agrégalo en Firebase Console → Configuración del proyecto

#### Configurar para iOS

1. **Agregar app iOS**
   - Click en "Agregar app" → iOS
   - Bundle ID: `com.acupuntura.acupuntos`
   - Registrar la app

2. **Descargar GoogleService-Info.plist**
   - Descarga el archivo
   - Colócalo en: `/Platforms/iOS/GoogleService-Info.plist`
   - Build Action: "BundleResource"

### 2. Configurar Authentication

1. En Firebase Console → Authentication
2. Click en "Comenzar"
3. Habilitar proveedor "Google"
4. Configurar email de soporte

### 3. Configurar Firestore Database

1. En Firebase Console → Firestore Database
2. Crear base de datos
3. Comenzar en modo de prueba
4. Seleccionar ubicación más cercana

#### Reglas de Seguridad

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Usuarios pueden leer su propio documento
    match /users/{userId} {
      allow read: if request.auth != null && request.auth.uid == userId;
      allow write: if request.auth != null && 
        (request.auth.uid == userId || 
         get(/databases/$(database)/documents/users/$(request.auth.uid)).data.role == 'admin');
    }
    
    // Todos los usuarios autenticados pueden leer usuarios (para transferencias)
    match /users/{userId} {
      allow read: if request.auth != null;
    }
    
    // Transacciones
    match /transactions/{transactionId} {
      allow read: if request.auth != null && 
        (resource.data.fromUserId == request.auth.uid || 
         resource.data.toUserId == request.auth.uid ||
         get(/databases/$(database)/documents/users/$(request.auth.uid)).data.role == 'admin');
      allow create: if request.auth != null;
    }
    
    // Recompensas - todos pueden leer, solo admin puede escribir
    match /rewards/{rewardId} {
      allow read: if request.auth != null;
      allow write: if request.auth != null && 
        get(/databases/$(database)/documents/users/$(request.auth.uid)).data.role == 'admin';
    }
    
    // Canjes
    match /redemptions/{redemptionId} {
      allow read: if request.auth != null && 
        (resource.data.userId == request.auth.uid ||
         get(/databases/$(database)/documents/users/$(request.auth.uid)).data.role == 'admin');
      allow create: if request.auth != null && 
        request.resource.data.userId == request.auth.uid;
      allow update: if request.auth != null && 
        get(/databases/$(database)/documents/users/$(request.auth.uid)).data.role == 'admin';
    }
  }
}
```

### 4. Configurar primer Admin

1. Registra el primer usuario con Google Sign-In
2. Ve a Firebase Console → Firestore
3. Encuentra el documento en `users/{uid}`
4. Cambia el campo `role` de "user" a "admin"

## 🏗️ Estructura del Proyecto

```
AcuPuntos/
├── Models/              # Modelos de datos
│   ├── User.cs
│   ├── Transaction.cs
│   ├── Reward.cs
│   └── Redemption.cs
├── Services/            # Servicios
│   ├── AuthService.cs
│   └── FirestoreService.cs
├── ViewModels/          # ViewModels (MVVM)
│   ├── BaseViewModel.cs
│   ├── HomeViewModel.cs
│   ├── TransferViewModel.cs
│   └── RewardsViewModel.cs
├── Views/               # Vistas XAML
│   ├── LoginPage.xaml
│   ├── HomePage.xaml
│   ├── TransferPage.xaml
│   └── RewardsPage.xaml
├── Resources/           # Recursos
│   ├── Styles/
│   ├── Fonts/
│   └── Images/
└── Platforms/           # Código específico por plataforma
    ├── Android/
    └── iOS/
```

## 📦 Datos de Ejemplo

### Recompensas iniciales

```json
[
  {
    "name": "Cambio de horario gratis",
    "pointsCost": 500,
    "description": "Cambia tu cita sin costo adicional",
    "icon": "🕐",
    "category": "servicios"
  },
  {
    "name": "Sesión de 30 min gratis",
    "pointsCost": 2000,
    "description": "Una sesión corta completamente gratis",
    "icon": "💆",
    "category": "servicios"
  },
  {
    "name": "10% de descuento",
    "pointsCost": 300,
    "description": "Aplica en tu próxima sesión",
    "icon": "🎫",
    "category": "descuentos"
  }
]
```

## 🎨 Paleta de Colores

- **Verde Principal**: #2ECC71
- **Verde Oscuro**: #27AE60
- **Verde Claro**: #A8E6CF
- **Blanco**: #FFFFFF
- **Gris Claro**: #F5F5F5
- **Texto**: #333333

## 📱 Compilar y Ejecutar

### Android
```bash
dotnet build -t:Run -f net8.0-android
```

### iOS (solo en Mac)
```bash
dotnet build -t:Run -f net8.0-ios
```

## 🔧 Solución de Problemas

### Error de autenticación con Google
- Verificar que el SHA-1 esté configurado correctamente
- Asegurar que google-services.json esté actualizado

### Error de Firestore
- Verificar las reglas de seguridad
- Confirmar que el proyecto de Firebase esté activo

### Error de compilación
- Limpiar y reconstruir: `dotnet clean && dotnet build`
- Verificar versiones de paquetes NuGet

## 📄 Licencia

Este proyecto es privado y propietario del emprendimiento de acupuntura.

## 👨‍💻 Desarrollado por

Sistema desarrollado con .NET MAUI y Firebase.