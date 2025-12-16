# Solución: Inconsistencias de Zona Horaria (UTC)

## Problema Identificado

Había inconsistencias en el manejo de zonas horarias en la aplicación:
- Algunos horarios se mostraban en UTC
- Otros se mostraban en horario local (UTC-3 para Uruguay)
- La causa era crear `DateTimeOffset` sin especificar el offset explícitamente

## Causa Raíz

Cuando se crea un `DateTimeOffset` de esta manera:
```csharp
var dt = new DateTimeOffset(localDateTime);
```

Se usa automáticamente el **offset local del sistema** (UTC-3 en Uruguay), no UTC.

## Solución Implementada

### 1. **Almacenamiento en UTC**
Todos los horarios ahora se almacenan en UTC en Firebase/Firestore usando componentes de fecha:
```csharp
// CORRECTO: Usar componentes de fecha para evitar conflictos de offset
var startTime = new DateTimeOffset(
    SelectedDate.Year, SelectedDate.Month, SelectedDate.Day,
    NewSlotTime.Hours, NewSlotTime.Minutes, NewSlotTime.Seconds,
    TimeSpan.Zero); // UTC

// ❌ INCORRECTO: Esto causaría ArgumentException
// var localDateTime = SelectedDate.Date.Add(NewSlotTime);
// var startTime = new DateTimeOffset(localDateTime, TimeSpan.Zero);
```

**Por qué**: `SelectedDate.Date` es un `DateTime` con `Kind = Local`, y .NET no permite crear un `DateTimeOffset` con un DateTime local especificando un offset diferente (UTC).

### 2. **Consultas en UTC**
Todas las consultas a la base de datos se realizan con fechas en UTC usando componentes:
```csharp
// CORRECTO: Usar componentes de fecha
var start = new DateTimeOffset(
    SelectedDate.Year, SelectedDate.Month, SelectedDate.Day, 
    0, 0, 0, TimeSpan.Zero); // UTC 00:00:00

var endDate = SelectedDate.Date.AddDays(1).AddTicks(-1);
var end = new DateTimeOffset(
    endDate.Year, endDate.Month, endDate.Day, 
    endDate.Hour, endDate.Minute, endDate.Second, 
    TimeSpan.Zero).AddTicks(endDate.Ticks % TimeSpan.TicksPerSecond); // UTC 23:59:59.9999999
```

### 3. **Visualización en Horario Local**
Para mostrar al usuario, usamos `.ToLocalTime()` en las vistas:
```csharp
Text="{Binding StartTime, StringFormat='{0:HH:mm}'}" // Automático
// O manualmente:
slot.StartTime.ToLocalTime().TimeOfDay
```

## Archivos Modificados

### AdminAgendaViewModel.cs
- ✅ `CreateSlot()` - Crea turnos en UTC
- ✅ `BatchCreateSlots()` - Crea múltiples turnos en UTC
- ✅ `EditSlot()` - Edita turnos manteniendo UTC
- ✅ `SetupListeners()` - Consultas en UTC
- ✅ `OnSelectedDateChanged()` - Consultas en UTC
- ✅ `LoadSlotsAsync()` - Consultas en UTC

### AgendaViewModel.cs
- ✅ `SetupListeners()` - Consultas en UTC
- ✅ `OnSelectedDateChanged()` - Consultas en UTC
- ✅ `LoadAvailableSlotsAsync()` - Consultas en UTC

## Flujo Correcto de Fechas

```
┌────────────────────────────────────────────────────┐
│ 1. Usuario selecciona hora: 14:00 (hora local)    │
└────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────┐
│ 2. App crea DateTime: 2025-11-24 14:00            │
└────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────┐
│ 3. Convierte a UTC para almacenar:                │
│    new DateTimeOffset(dt, TimeSpan.Zero)          │
│    = 2025-11-24 14:00 +00:00 (UTC)                │
└────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────┐
│ 4. Firestore almacena: 2025-11-24T14:00:00Z       │
└────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────┐
│ 5. Al recuperar, se convierte a local para UI:    │
│    StartTime.ToLocalTime() = 11:00 (UTC-3)        │
│    PERO se muestra como 14:00 porque el DatePicker│
│    interpretó 14:00 como hora local                │
└────────────────────────────────────────────────────┘
```

## Importante ⚠️

**La solución actual asume que:**
- El usuario ingresa la hora **tal como quiere que aparezca** (14:00 significa "a las 14:00")
- Esa hora se almacena directamente como UTC (sin conversión de zona horaria)
- Al mostrar, se usa el valor directo sin conversión

**Esto es correcto si:**
- La aplicación solo se usa en una zona horaria
- Los turnos son solo para la zona local del negocio

**Si en el futuro necesitas soporte multi-zona-horaria:**
- Deberás almacenar también la zona horaria del negocio/servicio
- Convertir correctamente entre zonas al mostrar

## Verificación

Para verificar que funciona correctamente:

1. **Crear un turno a las 14:00**
   - Debe mostrarse como 14:00 en la lista
   - En Firestore debe aparecer como `2025-11-24T14:00:00Z`

2. **Consultar turnos del día**
   - Debe traer todos los turnos entre 00:00 y 23:59 UTC del día seleccionado
   - Se mostrarán en la hora correcta (14:00 se ve como 14:00)

3. **Cambiar zona horaria del dispositivo** (para probar)
   - Los turnos deberían seguir mostrándose en la misma hora
   - Porque estamos usando el valor directo sin conversión

## Notas Adicionales

- `DateTimeOffset` siempre incluye información de zona horaria
- `TimeSpan.Zero` = UTC (+00:00)
- `.ToLocalTime()` convierte al horario local del dispositivo
- Firebase/Firestore almacena automáticamente en formato ISO 8601 con 'Z' (UTC)

## Fecha de Implementación
24 de noviembre de 2025
