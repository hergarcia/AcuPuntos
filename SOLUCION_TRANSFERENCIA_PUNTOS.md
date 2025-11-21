# Solución: Error de Permisos al Transferir Puntos

## El Problema

Al intentar transferir puntos desde un usuario NO admin, se recibía el siguiente error:

```
Error updating user points: PERMISSION_DENIED: Missing or insufficient permissions.
Error transferring points: PERMISSION_DENIED: Missing or insufficient permissions.
```

### ¿Por qué ocurre este error?

El código original intentaba actualizar documentos de Firestore directamente desde el cliente (app móvil):

1. **Paso 1**: Restar puntos del usuario origen → ✅ **Funciona** (el usuario puede modificar su propio documento)
2. **Paso 2**: Sumar puntos al usuario destino → ❌ **FALLA** (el usuario NO puede modificar el documento de otro usuario)

Las reglas de seguridad de Firestore solo permiten:
- Que un usuario modifique **su propio** documento
- Que un **admin** modifique **cualquier** documento

Esto crea un problema: un usuario normal no puede transferir puntos a otro usuario porque no tiene permiso para modificar el documento del destinatario.

## La Solución: Firebase Cloud Functions

La solución implementada usa **Firebase Cloud Functions**, que se ejecutan en el servidor con privilegios de administrador.

### Flujo Anterior (con problema)

```
[App Móvil] ──❌──> [Firestore]
     │
     ├─> Actualizar usuario A (origen) ✅
     └─> Actualizar usuario B (destino) ❌ PERMISSION_DENIED
```

### Flujo Nuevo (solución)

```
[App Móvil] ──✅──> [Cloud Function] ──✅──> [Firestore]
                           │
                           ├─> Actualizar usuario A (origen) ✅
                           ├─> Actualizar usuario B (destino) ✅
                           ├─> Crear transacción de envío ✅
                           └─> Crear transacción de recepción ✅
```

### Ventajas de esta solución

1. **Seguridad**: Las validaciones ocurren en el servidor, no en el cliente
2. **Atomicidad**: Toda la operación se ejecuta como una transacción (todo o nada)
3. **Escalabilidad**: Fácil agregar lógica adicional (notificaciones, límites, etc.)
4. **Auditoría**: Los logs del servidor registran todas las operaciones

## Cambios Implementados

### 1. Cloud Function (`functions/src/index.ts`)

Se creó una función `transferPoints` que:

- ✅ Verifica que el usuario esté autenticado
- ✅ Valida que el usuario origen tenga suficientes puntos
- ✅ Verifica que ambos usuarios existan
- ✅ Previene transferir puntos a uno mismo
- ✅ Usa transacciones de Firestore para garantizar atomicidad
- ✅ Actualiza puntos de ambos usuarios
- ✅ Crea registros de transacciones
- ✅ Actualiza contadores de puntos ganados/gastados

### 2. Actualización del Servicio (`Services/FirestoreService.cs`)

El método `TransferPointsAsync` ahora:

```csharp
// Antes: Actualizaba Firestore directamente (causaba error de permisos)
await UpdateUserPointsAsync(fromUserId, -points);
await UpdateUserPointsAsync(toUserId, points);

// Ahora: Llama a la Cloud Function
var function = _cloudFunctions.GetHttpsCallable("transferPoints");
var result = await function.CallAsync(data);
```

### 3. Configuración de Firebase

Se agregaron archivos de configuración:

- `firebase.json` - Configuración del proyecto Firebase
- `.firebaserc` - Identificador del proyecto
- `functions/package.json` - Dependencias de Node.js
- `functions/tsconfig.json` - Configuración de TypeScript

## Cómo Desplegar

### Requisitos Previos

1. **Node.js 18+**
   ```bash
   node --version  # Debe ser v18 o superior
   ```

2. **Firebase CLI**
   ```bash
   npm install -g firebase-tools
   firebase login
   ```

### Pasos de Instalación

1. **Ir al directorio de functions**
   ```bash
   cd functions
   ```

2. **Instalar dependencias**
   ```bash
   npm install
   ```

3. **Compilar TypeScript**
   ```bash
   npm run build
   ```

4. **Desplegar a Firebase**
   ```bash
   npm run deploy
   ```

   O usando Firebase CLI directamente:
   ```bash
   firebase deploy --only functions
   ```

### Verificar el Despliegue

1. Después del despliegue verás:
   ```
   ✔  functions[transferPoints(us-central1)]: Successful update operation.
   ```

2. Verifica en Firebase Console:
   - Ve a https://console.firebase.google.com
   - Selecciona tu proyecto
   - Ve a **Functions**
   - Deberías ver `transferPoints` listada

## Probando la Solución

### 1. Probar con la App

1. Inicia sesión con un usuario NO admin
2. Ve a la sección "Transferir"
3. Selecciona un destinatario
4. Ingresa una cantidad de puntos
5. Confirma la transferencia

**Resultado esperado**: ✅ La transferencia se completa exitosamente

### 2. Probar Localmente (Desarrollo)

Para probar sin desplegar a producción:

```bash
cd functions
npm run serve
```

Esto inicia el emulador de Functions en `http://localhost:5001`.

## Solución de Problemas

### Error: "Function not found"

**Causa**: La función no está desplegada.

**Solución**:
```bash
cd functions
npm run deploy
```

### Error: "CORS error"

**Causa**: Problemas de configuración de red o autenticación.

**Solución**:
1. Verifica que el usuario esté autenticado
2. Verifica que el proyecto de Firebase sea correcto en `.firebaserc`
3. Intenta desde el emulador local primero

### Error: "Quota exceeded"

**Causa**: Límite gratuito de Firebase Functions excedido.

**Solución**: Actualiza al plan Blaze (pago por uso) en Firebase Console

### Logs no aparecen

**Ver logs en tiempo real**:
```bash
firebase functions:log
```

**Ver logs en Firebase Console**:
1. Firebase Console → Functions
2. Click en `transferPoints`
3. Pestaña "Logs"

## Costos

El plan gratuito de Firebase (Spark) incluye:
- 2 millones de invocaciones/mes
- 400,000 GB-segundos/mes
- 200,000 CPU-segundos/mes

Para una app de transferencia de puntos con uso moderado, el plan gratuito es suficiente.

Si necesitas más, el plan Blaze es pago por uso (muy económico para apps pequeñas).

## Alternativas Consideradas

### ❌ Opción 1: Modificar las reglas de seguridad

Permitir que cualquier usuario actualice cualquier documento sería inseguro:
- Los usuarios podrían darse puntos ilimitados
- No habría control de validaciones
- Vulnerable a ataques

### ❌ Opción 2: Solo permitir transferencias de admin

Esto limitaría la funcionalidad de la app y frustraría a los usuarios.

### ✅ Opción 3: Cloud Functions (implementada)

Es la solución estándar de Firebase para este tipo de operaciones:
- Segura
- Escalable
- Mantenible
- Recomendada por Google

## Próximos Pasos

Puedes extender las Cloud Functions para:

1. **Notificaciones**: Enviar notificación push al recibir puntos
2. **Límites**: Establecer límite diario de transferencias
3. **Auditoría**: Detectar transacciones sospechosas
4. **Recompensas**: Manejar canjes de recompensas de forma segura

## Referencias

- [Firebase Cloud Functions Docs](https://firebase.google.com/docs/functions)
- [Firestore Security Rules](https://firebase.google.com/docs/firestore/security/get-started)
- [Plugin.Firebase para .NET MAUI](https://github.com/TobiasBuchholz/Plugin.Firebase)
