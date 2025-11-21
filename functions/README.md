# Firebase Cloud Functions para AcuPuntos

Este directorio contiene las Cloud Functions de Firebase para AcuPuntos, que permiten ejecutar código en el servidor de manera segura.

## ¿Por qué Cloud Functions?

Las Cloud Functions resuelven el problema de permisos al transferir puntos entre usuarios no-admin:

- **Problema**: Las reglas de seguridad de Firestore solo permiten que un usuario modifique su propio documento. Cuando un usuario intenta transferir puntos, puede restar puntos de su cuenta, pero NO puede agregar puntos a la cuenta de otro usuario.

- **Solución**: Las Cloud Functions se ejecutan en el servidor con privilegios de administrador, permitiendo operaciones complejas que involucran múltiples documentos de forma segura.

## Funciones Disponibles

### `transferPoints`

Transfiere puntos de un usuario a otro de manera segura y atómica.

**Parámetros:**
- `fromUserId` (string): UID del usuario que envía los puntos
- `toUserId` (string): UID del usuario que recibe los puntos
- `points` (number): Cantidad de puntos a transferir
- `description` (string, opcional): Descripción de la transferencia

**Validaciones:**
- El usuario autenticado debe ser el mismo que está enviando los puntos
- Ambos usuarios deben existir
- El usuario debe tener suficientes puntos
- Los puntos deben ser mayor a 0
- No se puede transferir puntos a uno mismo

**Retorno:**
```typescript
{
  success: true,
  fromUserPoints: number,  // Puntos restantes del usuario origen
  toUserPoints: number     // Puntos nuevos del usuario destino
}
```

## Instalación

### Prerrequisitos

1. **Node.js 18 o superior**
   ```bash
   node --version  # Debe ser v18.x o superior
   ```

2. **Firebase CLI**
   ```bash
   npm install -g firebase-tools
   ```

3. **Iniciar sesión en Firebase**
   ```bash
   firebase login
   ```

### Configuración Inicial

1. **Verificar proyecto de Firebase**

   Asegúrate de estar en el directorio raíz del proyecto y verifica el proyecto:
   ```bash
   cd /path/to/AcuPuntos
   firebase projects:list
   ```

2. **Inicializar Firebase (si aún no está inicializado)**

   Si no existe un archivo `firebase.json` en la raíz del proyecto:
   ```bash
   firebase init functions
   ```

   Selecciona:
   - Proyecto existente: **AcuPuntos** (o el nombre de tu proyecto)
   - Lenguaje: **TypeScript**
   - ESLint: **No** (opcional)
   - Instalar dependencias: **Sí**

3. **Instalar dependencias**

   ```bash
   cd functions
   npm install
   ```

## Desarrollo Local

### Ejecutar en el Emulador

Para probar las funciones localmente antes de desplegarlas:

```bash
cd functions
npm run serve
```

Esto iniciará el emulador de Functions en `http://localhost:5001`.

### Probar la función localmente

Puedes probar la función con curl:

```bash
curl -X POST http://localhost:5001/YOUR_PROJECT_ID/us-central1/transferPoints \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -d '{
    "data": {
      "fromUserId": "user1_uid",
      "toUserId": "user2_uid",
      "points": 100,
      "description": "Test transfer"
    }
  }'
```

## Despliegue a Producción

### 1. Compilar el código TypeScript

```bash
cd functions
npm run build
```

### 2. Desplegar las funciones

```bash
npm run deploy
```

O directamente con Firebase CLI:

```bash
firebase deploy --only functions
```

### 3. Verificar el despliegue

Después del despliegue, verás la URL de la función:
```
✔  functions[transferPoints(us-central1)]: Successful update operation.
Function URL: https://us-central1-YOUR_PROJECT.cloudfunctions.net/transferPoints
```

### 4. Verificar en Firebase Console

1. Ve a [Firebase Console](https://console.firebase.google.com)
2. Selecciona tu proyecto
3. Ve a **Functions** en el menú lateral
4. Deberías ver `transferPoints` listada

## Configuración de Reglas de Seguridad

Asegúrate de que las reglas de Firestore permitan que la Cloud Function funcione correctamente. En Firebase Console → Firestore Database → Reglas:

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Usuarios - lectura pública, escritura solo por owner o admin
    match /users/{userId} {
      allow read: if request.auth != null;
      allow write: if request.auth != null &&
        (request.auth.uid == userId ||
         get(/databases/$(database)/documents/users/$(request.auth.uid)).data.role == 'admin');
    }

    // Transacciones - creadas por Cloud Function
    match /transactions/{transactionId} {
      allow read: if request.auth != null &&
        (resource.data.fromUserId == request.auth.uid ||
         resource.data.toUserId == request.auth.uid ||
         get(/databases/$(database)/documents/users/$(request.auth.uid)).data.role == 'admin');
      allow create: if request.auth != null;
    }

    // ... resto de las reglas
  }
}
```

## Monitoreo y Logs

### Ver logs en tiempo real

```bash
firebase functions:log
```

### Ver logs de una función específica

```bash
firebase functions:log --only transferPoints
```

### Ver logs en Firebase Console

1. Ve a Firebase Console → Functions
2. Click en `transferPoints`
3. Ve a la pestaña "Logs"

## Solución de Problemas

### Error: "Missing or insufficient permissions"

**Causa**: Las reglas de Firestore no permiten la operación.

**Solución**: Verifica que las reglas de seguridad estén configuradas correctamente (ver sección anterior).

### Error: "Function not found"

**Causa**: La función no está desplegada o el nombre es incorrecto.

**Solución**:
1. Verifica que la función esté desplegada: `firebase functions:list`
2. Verifica el nombre en `src/index.ts`
3. Despliega nuevamente: `firebase deploy --only functions`

### Error: "CORS error" o "Network error"

**Causa**: Problemas de configuración de CORS o red.

**Solución**:
1. Asegúrate de que la app esté autenticada con Firebase Auth
2. Verifica que el proyecto de Firebase sea el correcto
3. Intenta desde el emulador local primero

### Error: "Quota exceeded"

**Causa**: Límite de uso gratuito de Firebase Functions excedido.

**Solución**:
1. Revisa el uso en Firebase Console → Functions → Usage
2. Considera actualizar al plan Blaze (pago por uso)

## Costos

Firebase Functions tiene un plan gratuito (Spark) con límites:
- 2 millones de invocaciones/mes
- 400,000 GB-segundos/mes
- 200,000 CPU-segundos/mes

Para más información: https://firebase.google.com/pricing

Para transferencias de puntos normales, el plan gratuito es más que suficiente.

## Siguientes Pasos

1. **Añadir más funciones** para otras operaciones que requieran privilegios de servidor:
   - Asignar puntos (admin)
   - Canjear recompensas
   - Aprobar canjes

2. **Añadir validaciones adicionales**:
   - Límites de transferencia diaria
   - Historial de transacciones sospechosas
   - Notificaciones por correo

3. **Optimizar rendimiento**:
   - Cache de datos frecuentes
   - Batch operations
   - Background processing

## Referencias

- [Documentación de Firebase Functions](https://firebase.google.com/docs/functions)
- [Plugin.Firebase para .NET MAUI](https://github.com/TobiasBuchholz/Plugin.Firebase)
- [Seguridad en Firestore](https://firebase.google.com/docs/firestore/security/get-started)
