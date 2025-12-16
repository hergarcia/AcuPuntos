# Resumen: Duración de Sesión Agregada

## ✅ Cambios Implementados

### 1. **Modelo (`AppointmentSlot.cs`)**
- ✅ Agregada propiedad `DurationMinutes` (int) con valor por defecto de 60 minutos
- ✅ Marcada con `[FirestoreProperty("durationMinutes")]` para guardar en Firestore

### 2. **ViewModel (`AdminAgendaViewModel.cs`)**
- ✅ Al crear turno individual: Se guarda `SlotDurationMinutes`
- ✅ Al crear turnos por lotes: Se guarda `BatchIntervalMinutes`

### 3. **Vista Usuario (`AgendaPage.xaml`)**
- ✅ **Mis Turnos**: Muestra "Duración: XX min" debajo del horario
- ✅ **Turnos Disponibles**: Muestra "Duración: XX min" debajo del horario

## 📋 Cómo Funciona

1. **Admin crea un turno**:
   - Selecciona duración con el Stepper (30, 45, 60, etc. minutos)
   - Al crear el turno, se guarda `DurationMinutes` en Firestore

2. **Usuario ve los turnos**:
   - En "Mis Turnos": Ve la hora y la duración de su sesión
   - En "Turnos Disponibles": Ve la hora y duración antes de reservar

## 🎯 Ejemplo

Si el admin crea un turno de:
- **Hora**: 10:00 AM
- **Duración**: 45 minutos

El usuario verá:
```
🕐 10:00
Duración: 45 min
[Status Chip]
```

## 📝 Notas

- **Valor por defecto**: 60 minutos
- **Turnos antiguos**: Los turnos creados antes de este cambio mostrarán 60 minutos por defecto
- **Ubicación**: Se muestra en color gris (Gray600/Gray400) para diferenciarlo de la información principal

## Fecha de Implementación
25 de noviembre de 2025
