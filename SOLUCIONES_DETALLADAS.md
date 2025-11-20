# GUÍA DE SOLUCIONES DETALLADAS - ACUPUNTOS

---

## FASE 1: CORRECCIONES CRÍTICAS (URGENTE)

### 1. Corregir TransactionType Enum
**Archivo**: `/home/user/AcuPuntos/Models/Transaction.cs`

**Problema**: El enum tiene valores incorrectos que no coinciden con el código que los usa

**Solución**:
```csharp
public enum TransactionType
{
    Earned,      // Puntos ganados (sistema)
    Spent,       // Puntos gastados (canjes)
    Sent,        // Puntos enviados (transferencias - salida)
    Received,    // Puntos recibidos (transferencias - entrada)
    Reward,      // Recompensa específica
    Redemption   // Canje de recompensa
}
```

**Archivos afectados a actualizar**:
- Converters/TransactionColorConverter.cs
- Converters/TransactionIconConverter.cs
- Converters/TransactionTypeConverter.cs
- ViewModels/HistoryViewModel.cs (líneas 88, 92, 125-127)

---

### 2. Implementar Métodos Faltantes en FirestoreService

**Archivo**: `/home/user/AcuPuntos/Services/IFirestoreService.cs`

**Agregar a interfaz**:
```csharp
// Estadísticas por usuario
Task<Dictionary<string, object>> GetUserStatsAsync(string userId);

// Canjes pendientes (admin)
Task<List<Redemption>> GetPendingRedemptionsAsync();

// Asignar puntos (admin)
Task<bool> AssignPointsToUserAsync(string uid, int points, string description);
```

**Archivo**: `/home/user/AcuPuntos/Services/FirestoreService.cs`

**Implementar métodos**:

```csharp
public async Task<Dictionary<string, object>> GetUserStatsAsync(string userId)
{
    try
    {
        var stats = new Dictionary<string, object>();
        
        // Total de transacciones del usuario
        var transactions = await GetUserTransactionsAsync(userId);
        stats["totalTransactions"] = transactions.Count;
        
        // Puntos ganados
        stats["totalPointsEarned"] = transactions
            .Where(t => t.Type == TransactionType.Earned || t.Type == TransactionType.Received)
            .Sum(t => t.Amount);
        
        // Puntos gastados
        stats["totalPointsSpent"] = transactions
            .Where(t => t.Type == TransactionType.Spent || t.Type == TransactionType.Sent || t.Type == TransactionType.Redemption)
            .Sum(t => t.Amount);
        
        // Total canjes del usuario
        var redemptions = await GetUserRedemptionsAsync(userId);
        stats["totalRedemptions"] = redemptions.Count;
        
        return stats;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error getting user stats: {ex.Message}");
        return new Dictionary<string, object>();
    }
}

public async Task<List<Redemption>> GetPendingRedemptionsAsync()
{
    try
    {
        var redemptions = await _firestore.GetCollection(RedemptionsCollection)
            .WhereEqualsTo("status", "Pending")
            .OrderBy("redeemedAt")
            .GetDocumentsAsync<Redemption>();

        if (redemptions != null)
        {
            var redemptionsList = redemptions.Documents
                .Select(x => x.Data)
                .OrderByDescending(x => x.RedeemedAt)
                .ToList();

            // Cargar usuario y recompensa para cada canje
            foreach (var redemption in redemptionsList)
            {
                if (!string.IsNullOrEmpty(redemption.UserId))
                    redemption.User = await GetUserAsync(redemption.UserId);
                
                if (!string.IsNullOrEmpty(redemption.RewardId))
                    redemption.Reward = await GetRewardAsync(redemption.RewardId);
            }
            
            return redemptionsList;
        }
        return new List<Redemption>();
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error getting pending redemptions: {ex.Message}");
        return new List<Redemption>();
    }
}

public async Task<bool> AssignPointsToUserAsync(string uid, int points, string description)
{
    try
    {
        if (points <= 0)
            return false;

        var user = await GetUserAsync(uid);
        if (user == null)
            return false;

        // Actualizar puntos
        user.Points += points;
        await UpdateUserAsync(user);

        // Crear transacción de administrador
        var transaction = new Transaction
        {
            Type = TransactionType.Earned,
            Amount = points,
            FromUserId = "admin",
            ToUserId = uid,
            Description = description ?? "Asignación de puntos por administrador",
            CreatedAt = DateTime.UtcNow
        };

        await CreateTransactionAsync(transaction);
        return true;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error assigning points: {ex.Message}");
        return false;
    }
}
```

---

### 3. Corregir RedeemRewardAsync

**Archivo**: `/home/user/AcuPuntos/Services/FirestoreService.cs`

El método ya existe con firma correcta, pero los ViewModels lo llaman con parámetro extra.

**Corregir en RewardsViewModel.cs:148-151**:
```csharp
// ANTES (INCORRECTO):
var success = await _firestoreService.RedeemRewardAsync(
    CurrentUser.Uid!,
    reward.Id!,
    reward.PointsCost);  // ← REMOVER este parámetro

// DESPUÉS (CORRECTO):
var success = await _firestoreService.RedeemRewardAsync(
    CurrentUser.Uid!,
    reward.Id!);
```

**Corregir en RewardDetailViewModel.cs:102-105**:
```csharp
// ANTES (INCORRECTO):
var success = await _firestoreService.RedeemRewardAsync(
    CurrentUser.Uid!,
    Reward.Id!,
    Reward.PointsCost);  // ← REMOVER este parámetro

// DESPUÉS (CORRECTO):
var success = await _firestoreService.RedeemRewardAsync(
    CurrentUser.Uid!,
    Reward.Id!);
```

---

## FASE 2: REFACTORIZACIÓN DE CÓDIGO

### 1. Crear FilterableViewModelBase<T>

**Archivo**: `/home/user/AcuPuntos/ViewModels/FilterableViewModelBase.cs` (nuevo)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace AcuPuntos.ViewModels
{
    public abstract partial class FilterableViewModelBase<T> : BaseViewModel where T : class
    {
        [ObservableProperty]
        private ObservableCollection<T> items = new();

        [ObservableProperty]
        private ObservableCollection<T> filteredItems = new();

        [ObservableProperty]
        private string searchText = "";

        protected virtual bool MatchesSearch(T item, string searchLower) => true;

        protected virtual void FilterItems()
        {
            FilteredItems.Clear();

            var filtered = Items.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(item => MatchesSearch(item, searchLower));
            }

            foreach (var item in filtered)
            {
                FilteredItems.Add(item);
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterItems();
        }
    }
}
```

**Usar en TransferViewModel, RewardsViewModel, etc.**

---

### 2. Crear LoadableViewModelBase<T>

**Archivo**: `/home/user/AcuPuntos/ViewModels/LoadableViewModelBase.cs` (nuevo)

```csharp
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcuPuntos.ViewModels
{
    public abstract partial class LoadableViewModelBase<T> : BaseViewModel where T : class
    {
        protected abstract Task<List<T>> LoadDataAsync();
        protected abstract void PopulateCollection(List<T> data);
        protected virtual string LoadingMessage => "Cargando...";

        protected async Task LoadData()
        {
            await ExecuteAsync(async () =>
            {
                var data = await LoadDataAsync();
                PopulateCollection(data);
            }, LoadingMessage);
        }

        [RelayCommand]
        protected async Task RefreshData()
        {
            await LoadData();
        }
    }
}
```

---

### 3. Crear Validador Reutilizable

**Archivo**: `/home/user/AcuPuntos/Helpers/PointsValidator.cs` (nuevo)

```csharp
namespace AcuPuntos.Helpers
{
    public static class PointsValidator
    {
        public static (bool IsValid, string ErrorMessage) ValidateTransferPoints(
            int points, 
            int userAvailablePoints)
        {
            if (points <= 0)
                return (false, "La cantidad debe ser mayor a 0");
            
            if (points > userAvailablePoints)
                return (false, $"No tienes suficientes puntos (disponibles: {userAvailablePoints})");
            
            return (true, "");
        }

        public static (bool IsValid, string ErrorMessage) ValidateRedemption(
            int rewardCost, 
            int userPoints,
            DateTime? expiryDate = null)
        {
            if (rewardCost <= 0)
                return (false, "El costo de la recompensa es inválido");
            
            if (userPoints < rewardCost)
                return (false, $"Te faltan {rewardCost - userPoints} puntos");
            
            if (expiryDate.HasValue && expiryDate.Value < DateTime.UtcNow)
                return (false, "Esta recompensa ha expirado");
            
            return (true, "");
        }

        public static (bool IsValid, string ErrorMessage) ValidateAssignPoints(int points)
        {
            if (points <= 0)
                return (false, "La cantidad debe ser mayor a 0");
            
            if (points > 1000000) // Límite razonable
                return (false, "La cantidad es demasiado grande");
            
            return (true, "");
        }
    }
}
```

---

## FASE 3: OPTIMIZACIONES DE FIRESTORE

### 1. Remover Doble Ordenamiento

**Archivo**: `/home/user/AcuPuntos/Services/FirestoreService.cs:153-198`

**ANTES**:
```csharp
var sentQuery = _firestore.GetCollection(TransactionsCollection)
    .WhereEqualsTo("fromUserId", userId)
    .OrderBy("createdAt")        // ← Ordena aquí
    .LimitedTo(limit);

// ...

return transactions.OrderByDescending(t => t.CreatedAt)  // ← Y aquí de nuevo!
    .Take(limit).ToList();
```

**DESPUÉS**:
```csharp
var sentQuery = _firestore.GetCollection(TransactionsCollection)
    .WhereEqualsTo("fromUserId", userId)
    .OrderBy("createdAt")
    .LimitedTo(limit);

// ...

// Solo unificar en cliente sin reordenar
var allTransactions = sentList.Concat(receivedList)
    .DistinctBy(t => t.Id)
    .OrderByDescending(t => t.CreatedAt)
    .ToList();

return allTransactions;
```

---

### 2. Refactorizar ListenToTransactions

**Archivo**: `/home/user/AcuPuntos/Services/FirestoreService.cs:571-587`

**ANTES** (ineficiente):
```csharp
public IDisposable ListenToTransactions(string userId, Action<List<Transaction>> onUpdate)
{
    return _firestore.GetCollection(TransactionsCollection)
        .WhereEqualsTo("toUserId", userId)
        .AddSnapshotListener<Transaction>(async (querySnapshot) =>
        {
            var transactions = await GetUserTransactionsAsync(userId);  // ← EXTRA QUERY!
            onUpdate(transactions);
        });
}
```

**DESPUÉS** (eficiente):
```csharp
public IDisposable ListenToTransactions(string userId, Action<List<Transaction>> onUpdate)
{
    var transactionsCache = new List<Transaction>();
    var disposables = new List<IDisposable>();

    // Escuchar transacciones enviadas
    disposables.Add(
        _firestore.GetCollection(TransactionsCollection)
            .WhereEqualsTo("fromUserId", userId)
            .OrderBy("createdAt")
            .LimitedTo(50)
            .AddSnapshotListener<Transaction>((querySnapshot) =>
            {
                if (querySnapshot != null)
                {
                    var sent = querySnapshot.Documents.Select(x => x.Data).ToList();
                    MergeAndUpdate(sent, transactionsCache, onUpdate);
                }
            })
    );

    // Escuchar transacciones recibidas
    disposables.Add(
        _firestore.GetCollection(TransactionsCollection)
            .WhereEqualsTo("toUserId", userId)
            .OrderBy("createdAt")
            .LimitedTo(50)
            .AddSnapshotListener<Transaction>((querySnapshot) =>
            {
                if (querySnapshot != null)
                {
                    var received = querySnapshot.Documents.Select(x => x.Data).ToList();
                    MergeAndUpdate(received, transactionsCache, onUpdate);
                }
            })
    );

    return new CompositeDisposable(disposables);
}

private void MergeAndUpdate(
    List<Transaction> newTransactions, 
    List<Transaction> cache,
    Action<List<Transaction>> onUpdate)
{
    cache.AddRange(newTransactions);
    var merged = cache
        .DistinctBy(t => t.Id)
        .OrderByDescending(t => t.CreatedAt)
        .Take(50)
        .ToList();
    
    onUpdate(merged);
}

private class CompositeDisposable : IDisposable
{
    private readonly List<IDisposable> _disposables;

    public CompositeDisposable(List<IDisposable> disposables)
    {
        _disposables = disposables;
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
            disposable?.Dispose();
    }
}
```

---

### 3. Agregar Agregación en Servidor

**Archivo**: `/home/user/AcuPuntos/Services/FirestoreService.cs:518-552`

**ANTES** (carga todos):
```csharp
public async Task<Dictionary<string, object>> GetStatisticsAsync()
{
    var users = await _firestore.GetCollection(UsersCollection).GetDocumentsAsync<User>();
    var transactions = await _firestore.GetCollection(TransactionsCollection).GetDocumentsAsync<Transaction>();
    // ...
}
```

**DESPUÉS** (limitado):
```csharp
public async Task<Dictionary<string, object>> GetStatisticsAsync()
{
    try
    {
        var stats = new Dictionary<string, object>();

        // Solo contar, no cargar documentos
        var usersCount = await _firestore.GetCollection(UsersCollection)
            .GetDocumentsAsync<User>();
        stats["totalUsers"] = usersCount?.Documents.Count ?? 0;

        // Calcular suma de puntos
        var users = usersCount?.Documents.Select(x => x.Data).ToList() ?? new List<User>();
        stats["totalPoints"] = users.Sum(u => u.Points);

        // Contar transacciones (solo últimas 100)
        var transactions = await _firestore.GetCollection(TransactionsCollection)
            .OrderBy("createdAt")
            .LimitedTo(100)
            .GetDocumentsAsync<Transaction>();
        stats["totalTransactions"] = transactions?.Documents.Count() ?? 0;

        // Contar canjes
        var redemptions = await _firestore.GetCollection(RedemptionsCollection)
            .LimitedTo(100)
            .GetDocumentsAsync<Redemption>();
        stats["totalRedemptions"] = redemptions?.Documents.Count() ?? 0;

        return stats;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error getting statistics: {ex.Message}");
        return new Dictionary<string, object>();
    }
}
```

---

## FASE 4: MEJORAS DE UI/UX

### 1. Extraer CardFrame a Componente Reutilizable

**Archivo**: `/home/user/AcuPuntos/Views/Controls/StatisticCard.xaml` (nuevo)

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<Frame xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       BackgroundColor="{Binding BackgroundColor, Source={RelativeSource Self}}"
       CornerRadius="15"
       Padding="20"
       HasShadow="True">

    <Grid ColumnDefinitions="*,Auto">
        <VerticalStackLayout Grid.Column="0">
            <Label Text="{Binding Title}"
                   TextColor="White"
                   FontSize="14"
                   Opacity="0.9"/>
            <Label Text="{Binding Value}"
                   TextColor="White"
                   FontSize="32"
                   FontAttributes="Bold"/>
            <Label Text="{Binding Subtitle}"
                   TextColor="White"
                   FontSize="12"
                   Opacity="0.8"/>
        </VerticalStackLayout>

        <Label Grid.Column="1"
               Text="{Binding Icon}"
               FontSize="60"
               VerticalOptions="Center"
               Margin="20,0,0,0"/>
    </Grid>
</Frame>
```

### 2. Sincronización Centralizada de Usuario

**Archivo**: `/home/user/AcuPuntos/Services/UserStateService.cs` (nuevo)

```csharp
namespace AcuPuntos.Services
{
    public interface IUserStateService
    {
        User? CurrentUser { get; }
        event EventHandler<User?>? UserChanged;
        Task RefreshUserAsync(string uid);
        Task UpdateUserAsync(User user);
    }

    public class UserStateService : IUserStateService
    {
        private readonly IFirestoreService _firestoreService;
        private User? _currentUser;

        public User? CurrentUser => _currentUser;
        public event EventHandler<User?>? UserChanged;

        public UserStateService(IFirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public async Task RefreshUserAsync(string uid)
        {
            _currentUser = await _firestoreService.GetUserAsync(uid);
            UserChanged?.Invoke(this, _currentUser);
        }

        public async Task UpdateUserAsync(User user)
        {
            await _firestoreService.UpdateUserAsync(user);
            _currentUser = user;
            UserChanged?.Invoke(this, _currentUser);
        }
    }
}
```

**Registrar en MauiProgram.cs**:
```csharp
services.AddSingleton<IUserStateService, UserStateService>();
```

---

## CHECKLIST DE IMPLEMENTACIÓN

- [ ] Fase 1: Correcciones Críticas
  - [ ] Actualizar TransactionType enum
  - [ ] Agregar métodos faltantes a interfaz
  - [ ] Implementar métodos en FirestoreService
  - [ ] Corregir llamadas en ViewModels
  - [ ] Compilar y probar

- [ ] Fase 2: Refactorización
  - [ ] Crear FilterableViewModelBase<T>
  - [ ] Crear LoadableViewModelBase<T>
  - [ ] Crear PointsValidator
  - [ ] Migrar ViewModels a nuevas clases base

- [ ] Fase 3: Optimizaciones
  - [ ] Remover doble ordenamiento
  - [ ] Refactorizar listeners
  - [ ] Optimizar GetStatisticsAsync
  - [ ] Pruebas de performance

- [ ] Fase 4: UI/UX
  - [ ] Extraer componentes XAML
  - [ ] Crear UserStateService
  - [ ] Registrar navegación AdminPage en AppShell
  - [ ] Pruebas de UX

