using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcuPuntos.Models;
using Plugin.Firebase.Firestore;
using Plugin.Firebase.CloudFunctions;

namespace AcuPuntos.Services
{
    public class FirestoreService : IFirestoreService
    {
        private readonly IFirebaseFirestore _firestore;
        private readonly IFirebaseCloudFunctions _cloudFunctions;
        private const string UsersCollection = "users";
        private const string TransactionsCollection = "transactions";
        private const string RewardsCollection = "rewards";
        private const string RedemptionsCollection = "redemptions";
        private const string BadgesCollection = "badges";
        private const string UserBadgesCollection = "userBadges";

        public FirestoreService()
        {
            _firestore = CrossFirebaseFirestore.Current;
            _cloudFunctions = CrossFirebaseCloudFunctions.Current;
        }

        #region Usuarios

        public async Task<User?> GetUserAsync(string uid)
        {
            try
            {
                var snapshot = await _firestore.GetCollection(UsersCollection)
                    .GetDocument(uid)
                    .GetDocumentSnapshotAsync<User>();

                if (snapshot != null && snapshot.Data != null)
                {
                    return snapshot.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user: {ex.Message}");
                return null;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var querySnapshot = await _firestore.GetCollection(UsersCollection)
                    .OrderBy("displayName")
                    .GetDocumentsAsync<User>();

                return querySnapshot?.Documents.Select(x => x.Data).ToList() ?? new List<User>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all users: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                // NOTA: Firestore no soporta búsqueda full-text nativa.
                // Esta implementación carga todos los usuarios y filtra en cliente.
                // Para apps con muchos usuarios, considerar:
                // 1. Usar Algolia o ElasticSearch para búsqueda
                // 2. Implementar índices con trigrams para búsqueda parcial
                // 3. Limitar resultados o paginar
                var searchLower = searchTerm.ToLower();
                var allUsers = await GetAllUsersAsync();

                return allUsers.Where(u =>
                    u.DisplayName?.ToLower().Contains(searchLower) == true ||
                    u.Email?.ToLower().Contains(searchLower) == true)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching users: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task CreateUserAsync(User user)
        {
            try
            {
                if (string.IsNullOrEmpty(user.Uid))
                    throw new ArgumentException("User UID cannot be null or empty");

                await _firestore.GetCollection(UsersCollection)
                    .GetDocument(user.Uid)
                    .SetDataAsync(user);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating user: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            try
            {
                if (string.IsNullOrEmpty(user.Uid))
                    throw new ArgumentException("User UID cannot be null or empty");

                // Plugin.Firebase UpdateDataAsync requires a dictionary
                var updates = new Dictionary<object, object>
                {
                    { "email", user.Email ?? "" },
                    { "displayName", user.DisplayName ?? "" },
                    { "photoUrl", user.PhotoUrl ?? "" },
                    { "points", user.Points },
                    { "role", user.Role },
                    { "createdAt", user.CreatedAt },
                    { "lastLogin", user.LastLogin },
                    { "fcmToken", user.FcmToken ?? "" }
                };

                await _firestore.GetCollection(UsersCollection)
                    .GetDocument(user.Uid)
                    .UpdateDataAsync(updates);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateUserPointsAsync(string uid, int pointsDelta)
        {
            try
            {
                // Primero obtener el usuario para verificar que existe
                var user = await GetUserAsync(uid);
                if (user == null)
                {
                    throw new ArgumentException($"Usuario con UID {uid} no existe");
                }

                // Calcular nuevos puntos
                int newPoints = user.Points + pointsDelta;

                // Actualizar solo el campo points de forma atómica
                // Esto evita race conditions con listeners de tiempo real
                var updates = new Dictionary<object, object>
                {
                    { "points", newPoints }
                };

                await _firestore.GetCollection(UsersCollection)
                    .GetDocument(uid)
                    .UpdateDataAsync(updates);

                System.Diagnostics.Debug.WriteLine($"Puntos actualizados para {uid}: {user.Points} + {pointsDelta} = {newPoints}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user points: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> AssignPointsToUserAsync(string userId, int points, string description)
        {
            try
            {
                // Actualizar puntos del usuario
                await UpdateUserPointsAsync(userId, points);

                // Rastrear puntos ganados
                await UpdateUserPointsTracking(userId, points, true);

                // Crear transacción de recompensa
                var transaction = new Transaction
                {
                    Type = TransactionType.Reward,
                    Amount = points,
                    ToUserId = userId,
                    Description = description,
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

        public async Task<Dictionary<string, object>> GetUserStatsAsync(string userId)
        {
            try
            {
                var stats = new Dictionary<string, object>();
                var transactions = await GetUserTransactionsAsync(userId, 1000);
                var redemptions = await GetUserRedemptionsAsync(userId);

                stats["totalTransactions"] = transactions.Count;
                stats["totalRedemptions"] = redemptions.Count;

                stats["totalPointsEarned"] = transactions
                    .Where(t => t.Type == TransactionType.Received || t.Type == TransactionType.Reward)
                    .Sum(t => t.Amount);

                stats["totalPointsSpent"] = transactions
                    .Where(t => t.Type == TransactionType.Sent || t.Type == TransactionType.Redemption)
                    .Sum(t => t.Amount);

                return stats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user stats: {ex.Message}");
                return new Dictionary<string, object>();
            }
        }

        #endregion

        #region Transacciones

        public async Task<List<Transaction>> GetUserTransactionsAsync(string userId, int limit = 50)
        {
            try
            {
                var transactions = new List<Transaction>();

                // Obtener transacciones donde el usuario es origen o destino
                // Removido OrderBy en Firestore para evitar doble ordenamiento - se ordena solo en cliente
                var sentQuery = _firestore.GetCollection(TransactionsCollection)
                    .WhereEqualsTo("fromUserId", userId)
                    .LimitedTo(limit * 2); // Límite mayor para compensar filtrado posterior

                var receivedQuery = _firestore.GetCollection(TransactionsCollection)
                    .WhereEqualsTo("toUserId", userId)
                    .LimitedTo(limit * 2);

                var sentTransactions = await sentQuery.GetDocumentsAsync<Transaction>();
                var receivedTransactions = await receivedQuery.GetDocumentsAsync<Transaction>();

                if (sentTransactions != null)
                {
                    var sentList = sentTransactions.Documents.Select(x => x.Data).ToList();
                    transactions.AddRange(sentList);
                }

                if (receivedTransactions != null)
                {
                    var receivedList = receivedTransactions.Documents.Select(x => x.Data).ToList();
                    foreach (var transaction in receivedList)
                    {
                        // Evitar duplicados en transferencias
                        if (!transactions.Any(t => t.Id == transaction.Id))
                            transactions.Add(transaction);
                    }
                }

                // Ordenar por fecha descendente SOLO en el cliente (más eficiente)
                return transactions.OrderByDescending(t => t.CreatedAt).Take(limit).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user transactions: {ex.Message}");
                return new List<Transaction>();
            }
        }

        public async Task CreateTransactionAsync(Transaction transaction)
        {
            try
            {
                var docRef = await _firestore.GetCollection(TransactionsCollection)
                    .AddDocumentAsync(transaction);
                transaction.Id = docRef.Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating transaction: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> TransferPointsAsync(string fromUserId, string toUserId, int points, string description)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Iniciando transferencia de {points} puntos de {fromUserId} a {toUserId}");

                // Usar Cloud Function para transferir puntos de manera segura
                // Esto evita problemas de permisos con las reglas de seguridad de Firestore
                var data = new Dictionary<string, object>
                {
                    { "fromUserId", fromUserId },
                    { "toUserId", toUserId },
                    { "points", points },
                    { "description", description ?? "" }
                };

                var function = _cloudFunctions.GetHttpsCallable("transferPoints");
                var result = await function.CallAsync(data);

                System.Diagnostics.Debug.WriteLine($"Transferencia completada exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error transferring points: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Extraer el mensaje de error más útil si es una excepción de Cloud Functions
                if (ex.Message.Contains("Puntos insuficientes") ||
                    ex.Message.Contains("No puedes transferir") ||
                    ex.Message.Contains("no existe"))
                {
                    throw new Exception(ex.Message);
                }

                return false;
            }
        }

        #endregion

        #region Recompensas

        public async Task<List<Reward>> GetActiveRewardsAsync()
        {
            try
            {
                var rewards = await _firestore.GetCollection(RewardsCollection)
                    .WhereEqualsTo("isActive", true)
                    .OrderBy("pointsCost")
                    .GetDocumentsAsync<Reward>();

                return rewards?.Documents.Select(x => x.Data).ToList() ?? new List<Reward>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting active rewards: {ex.Message}");
                return new List<Reward>();
            }
        }

        public async Task<List<Reward>> GetAllRewardsAsync()
        {
            try
            {
                var rewards = await _firestore.GetCollection(RewardsCollection)
                    .OrderBy("pointsCost")
                    .GetDocumentsAsync<Reward>();

                return rewards?.Documents.Select(x => x.Data).ToList() ?? new List<Reward>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all rewards: {ex.Message}");
                return new List<Reward>();
            }
        }

        public async Task<Reward?> GetRewardAsync(string rewardId)
        {
            try
            {
                var snapshot = await _firestore.GetCollection(RewardsCollection)
                    .GetDocument(rewardId)
                    .GetDocumentSnapshotAsync<Reward>();

                if (snapshot != null && snapshot.Data != null)
                {
                    return snapshot.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting reward: {ex.Message}");
                return null;
            }
        }

        public async Task CreateRewardAsync(Reward reward)
        {
            try
            {
                var docRef = await _firestore.GetCollection(RewardsCollection)
                    .AddDocumentAsync(reward);
                reward.Id = docRef.Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating reward: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateRewardAsync(Reward reward)
        {
            try
            {
                if (string.IsNullOrEmpty(reward.Id))
                    throw new ArgumentException("Reward ID cannot be null or empty");

                var updates = new Dictionary<object, object>
                {
                    { "name", reward.Name ?? "" },
                    { "pointsCost", reward.PointsCost },
                    { "description", reward.Description ?? "" },
                    { "isActive", reward.IsActive },
                    { "icon", reward.Icon ?? "" },
                    { "category", reward.Category ?? "" },
                    { "createdAt", reward.CreatedAt },
                    { "maxRedemptionsPerUser", reward.MaxRedemptionsPerUser ?? 0 },
                    { "expiryDate", reward.ExpiryDate }
                };

                await _firestore.GetCollection(RewardsCollection)
                    .GetDocument(reward.Id)
                    .UpdateDataAsync(updates);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating reward: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteRewardAsync(string rewardId)
        {
            try
            {
                await _firestore.GetCollection(RewardsCollection)
                    .GetDocument(rewardId)
                    .DeleteDocumentAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting reward: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Canjes

        public async Task<List<Redemption>> GetUserRedemptionsAsync(string userId)
        {
            try
            {
                // OPTIMIZADO: Removido OrderBy en Firestore, ordenamos solo en cliente
                var redemptions = await _firestore.GetCollection(RedemptionsCollection)
                    .WhereEqualsTo("userId", userId)
                    .GetDocumentsAsync<Redemption>();

                if (redemptions != null)
                {
                    var redemptionsList = redemptions.Documents.Select(x => x.Data).OrderByDescending(x => x.RedeemedAt).ToList();
                    // NOTA: N+1 query problem - carga cada recompensa individualmente
                    // Firestore no tiene buen soporte para batch gets con where clauses
                    // Alternativa: mantener cache local de recompensas
                    foreach (var redemption in redemptionsList)
                    {
                        if (!string.IsNullOrEmpty(redemption.RewardId))
                        {
                            redemption.Reward = await GetRewardAsync(redemption.RewardId);
                        }
                    }
                    return redemptionsList;
                }
                return new List<Redemption>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user redemptions: {ex.Message}");
                return new List<Redemption>();
            }
        }

        public async Task<List<Redemption>> GetAllRedemptionsAsync()
        {
            try
            {
                // OPTIMIZADO: Removido OrderBy en Firestore, ordenamos solo en cliente
                var redemptions = await _firestore.GetCollection(RedemptionsCollection)
                    .GetDocumentsAsync<Redemption>();

                return redemptions?.Documents.Select(x => x.Data).OrderByDescending(x => x.RedeemedAt).ToList() ?? new List<Redemption>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all redemptions: {ex.Message}");
                return new List<Redemption>();
            }
        }

        public async Task<List<Redemption>> GetPendingRedemptionsAsync()
        {
            try
            {
                // OPTIMIZADO: Removido OrderBy en Firestore, ordenamos solo en cliente
                var redemptions = await _firestore.GetCollection(RedemptionsCollection)
                    .WhereEqualsTo("status", (int)RedemptionStatus.Pending)
                    .GetDocumentsAsync<Redemption>();

                if (redemptions != null)
                {
                    var redemptionsList = redemptions.Documents.Select(x => x.Data).OrderByDescending(x => x.RedeemedAt).ToList();
                    // NOTA: N+1 query problem - carga cada recompensa y usuario individualmente
                    // Firestore no tiene buen soporte para batch gets con where clauses
                    // Alternativa: mantener cache local de recompensas y usuarios
                    foreach (var redemption in redemptionsList)
                    {
                        if (!string.IsNullOrEmpty(redemption.RewardId))
                        {
                            redemption.Reward = await GetRewardAsync(redemption.RewardId);
                        }
                        if (!string.IsNullOrEmpty(redemption.UserId))
                        {
                            redemption.User = await GetUserAsync(redemption.UserId);
                        }
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

        public async Task<Redemption?> RedeemRewardAsync(string userId, string rewardId)
        {
            try
            {
                var user = await GetUserAsync(userId);
                var reward = await GetRewardAsync(rewardId);

                if (user == null || reward == null)
                    return null;

                if (user.Points < reward.PointsCost)
                    return null;

                // Actualizar puntos del usuario
                await UpdateUserPointsAsync(userId, -reward.PointsCost);

                // Rastrear puntos gastados
                await UpdateUserPointsTracking(userId, reward.PointsCost, false);

                // Crear canje
                var redemption = new Redemption
                {
                    UserId = userId,
                    RewardId = rewardId,
                    PointsUsed = reward.PointsCost,
                    Status = RedemptionStatus.Pending,
                    RedeemedAt = DateTime.UtcNow
                };

                var docRef = await _firestore.GetCollection(RedemptionsCollection)
                    .AddDocumentAsync(redemption);
                redemption.Id = docRef.Id;

                // Crear transacción
                var transaction = new Transaction
                {
                    Type = TransactionType.Redemption,
                    Amount = reward.PointsCost,
                    FromUserId = userId,
                    Description = $"Canje: {reward.Name}",
                    RewardId = rewardId,
                    CreatedAt = DateTime.UtcNow
                };

                await CreateTransactionAsync(transaction);

                redemption.Reward = reward;
                return redemption;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error redeeming reward: {ex.Message}");
                return null;
            }
        }

        public async Task UpdateRedemptionStatusAsync(string redemptionId, RedemptionStatus status)
        {
            try
            {
                var updates = new Dictionary<object, object>
                {
                    ["status"] = status.ToString(),
                    ["completedAt"] = status == RedemptionStatus.Completed ? DateTime.UtcNow : (DateTime?)null
                };

                await _firestore.GetCollection(RedemptionsCollection)
                    .GetDocument(redemptionId)
                    .UpdateDataAsync(updates);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating redemption status: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Estadísticas

        public async Task<Dictionary<string, object>> GetStatisticsAsync()
        {
            try
            {
                // NOTA: Este método carga todos los documentos para calcular estadísticas globales.
                // Para apps con muchos datos, considerar:
                // 1. Usar Firestore Aggregation Queries (requiere configuración)
                // 2. Mantener contadores en documentos separados
                // 3. Cachear resultados con TTL

                var stats = new Dictionary<string, object>();

                // Total de usuarios - carga todos (necesario para suma de puntos)
                var users = await _firestore.GetCollection(UsersCollection).GetDocumentsAsync<User>();
                var usersList = users?.Documents.Select(x => x.Data).ToList() ?? new List<User>();
                stats["totalUsers"] = usersList.Count;

                // Total de puntos en circulación
                var totalPoints = usersList.Sum(u => u.Points);
                stats["totalPoints"] = totalPoints;

                // Total de transacciones - solo cuenta (no carga datos completos)
                var transactions = await _firestore.GetCollection(TransactionsCollection).GetDocumentsAsync<Transaction>();
                stats["totalTransactions"] = transactions?.Documents.Count() ?? 0;

                // Total de canjes - solo cuenta (no carga datos completos)
                var redemptions = await _firestore.GetCollection(RedemptionsCollection).GetDocumentsAsync<Redemption>();
                stats["totalRedemptions"] = redemptions?.Documents.Count() ?? 0;

                // Recompensas activas (usa caché si GetActiveRewardsAsync lo implementa)
                var activeRewards = await GetActiveRewardsAsync();
                stats["activeRewards"] = activeRewards.Count;

                return stats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting statistics: {ex.Message}");
                return new Dictionary<string, object>();
            }
        }

        #endregion

        #region Gamificación

        public async Task UpdateUserGamificationAsync(string userId, int experience, int level)
        {
            try
            {
                var updates = new Dictionary<object, object>
                {
                    { "experience", experience },
                    { "level", level }
                };

                await _firestore.GetCollection(UsersCollection)
                    .GetDocument(userId)
                    .UpdateDataAsync(updates);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user gamification: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateUserCheckInAsync(string userId, DateTimeOffset lastCheckIn, int consecutiveDays)
        {
            try
            {
                var updates = new Dictionary<object, object>
                {
                    { "lastCheckIn", lastCheckIn },
                    { "consecutiveDays", consecutiveDays }
                };

                await _firestore.GetCollection(UsersCollection)
                    .GetDocument(userId)
                    .UpdateDataAsync(updates);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user check-in: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateUserPointsTracking(string userId, int pointsDelta, bool isEarned)
        {
            try
            {
                var user = await GetUserAsync(userId);
                if (user == null)
                    return;

                var updates = new Dictionary<object, object>();

                if (isEarned)
                {
                    updates["totalPointsEarned"] = user.TotalPointsEarned + pointsDelta;
                }
                else
                {
                    updates["totalPointsSpent"] = user.TotalPointsSpent + Math.Abs(pointsDelta);
                }

                await _firestore.GetCollection(UsersCollection)
                    .GetDocument(userId)
                    .UpdateDataAsync(updates);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user points tracking: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Badges

        public async Task<List<Badge>> GetAllBadgesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[FirestoreService] Obteniendo badges de colección '{BadgesCollection}'...");

                var badges = await _firestore.GetCollection(BadgesCollection)
                    .OrderBy("order")
                    .GetDocumentsAsync<Badge>();

                if (badges == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[FirestoreService] Query retornó null");
                    return new List<Badge>();
                }

                System.Diagnostics.Debug.WriteLine($"[FirestoreService] Documentos encontrados: {badges.Documents.Count()}");

                var badgesList = new List<Badge>();
                foreach (var doc in badges.Documents)
                {
                    if (doc.Data != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FirestoreService] Badge: {doc.Data.Name} - ID: {doc.Data.Id} - Active: {doc.Data.IsActive}");
                        badgesList.Add(doc.Data);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[FirestoreService] Documento con data null, ID: {doc.Reference.Id}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[FirestoreService] Total badges cargados: {badgesList.Count}");
                return badgesList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FirestoreService] Error getting all badges: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[FirestoreService] Stack trace: {ex.StackTrace}");
                return new List<Badge>();
            }
        }

        public async Task<Badge?> GetBadgeAsync(string badgeId)
        {
            try
            {
                var snapshot = await _firestore.GetCollection(BadgesCollection)
                    .GetDocument(badgeId)
                    .GetDocumentSnapshotAsync<Badge>();

                return snapshot?.Data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting badge: {ex.Message}");
                return null;
            }
        }

        public async Task CreateBadgeAsync(Badge badge)
        {
            try
            {
                var docRef = await _firestore.GetCollection(BadgesCollection)
                    .AddDocumentAsync(badge);
                badge.Id = docRef.Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating badge: {ex.Message}");
                throw;
            }
        }

        public async Task<List<UserBadge>> GetUserBadgesAsync(string userId)
        {
            try
            {
                var userBadges = await _firestore.GetCollection(UserBadgesCollection)
                    .WhereEqualsTo("userId", userId)
                    .GetDocumentsAsync<UserBadge>();

                if (userBadges != null)
                {
                    var userBadgesList = userBadges.Documents.Select(x => x.Data).OrderByDescending(x => x.EarnedAt).ToList();

                    // Cargar los badges completos
                    foreach (var userBadge in userBadgesList)
                    {
                        if (!string.IsNullOrEmpty(userBadge.BadgeId))
                        {
                            userBadge.Badge = await GetBadgeAsync(userBadge.BadgeId);
                        }
                    }

                    return userBadgesList;
                }

                return new List<UserBadge>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user badges: {ex.Message}");
                return new List<UserBadge>();
            }
        }

        public async Task CreateUserBadgeAsync(UserBadge userBadge)
        {
            try
            {
                var docRef = await _firestore.GetCollection(UserBadgesCollection)
                    .AddDocumentAsync(userBadge);
                userBadge.Id = docRef.Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating user badge: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Listeners en tiempo real

        public IDisposable ListenToUserChanges(string uid, Action<User> onUpdate)
        {
            return _firestore.GetCollection(UsersCollection)
                .GetDocument(uid)
                .AddSnapshotListener<User>((snapshot) =>
                {
                    if (snapshot != null && snapshot.Data != null)
                    {
                        onUpdate(snapshot.Data);
                    }
                });
        }

        public IDisposable ListenToTransactions(string userId, Action<List<Transaction>> onUpdate)
        {
            // OPTIMIZADO: Usar directamente los datos del snapshot en lugar de hacer query adicional
            // Nota: Solo escucha transacciones recibidas. Para transacciones completas, usar GetUserTransactionsAsync
            return _firestore.GetCollection(TransactionsCollection)
                .WhereEqualsTo("toUserId", userId)
                .LimitedTo(50)
                .AddSnapshotListener<Transaction>((querySnapshot) =>
                {
                    if (querySnapshot != null)
                    {
                        // Usar directamente los datos del snapshot (evita query adicional)
                        var transactions = querySnapshot.Documents
                            .Select(x => x.Data)
                            .OrderByDescending(t => t.CreatedAt)
                            .ToList();
                        onUpdate(transactions);
                    }
                });
        }

        public IDisposable ListenToRewards(Action<List<Reward>> onUpdate)
        {
            return _firestore.GetCollection(RewardsCollection)
                .WhereEqualsTo("isActive", true)
                .OrderBy("pointsCost")
                .AddSnapshotListener<Reward>((querySnapshot) =>
                {
                    if (querySnapshot != null)
                    {
                        var rewards = querySnapshot.Documents.Select(x => x.Data).ToList();
                        onUpdate(rewards);
                    }
                });
        }

        #endregion
    }
}
