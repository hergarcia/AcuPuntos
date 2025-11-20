using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcuPuntos.Models;
using Plugin.Firebase.Firestore;

namespace AcuPuntos.Services
{
    public class FirestoreService : IFirestoreService
    {
        private readonly IFirebaseFirestore _firestore;
        private const string UsersCollection = "users";
        private const string TransactionsCollection = "transactions";
        private const string RewardsCollection = "rewards";
        private const string RedemptionsCollection = "redemptions";

        public FirestoreService()
        {
            _firestore = CrossFirebaseFirestore.Current;
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

                return querySnapshot?.ToList() ?? new List<User>();
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
                var user = await GetUserAsync(uid);
                if (user != null)
                {
                    user.Points += pointsDelta;
                    await UpdateUserAsync(user);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user points: {ex.Message}");
                throw;
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
                var sentQuery = _firestore.GetCollection(TransactionsCollection)
                    .WhereEqualsTo("fromUserId", userId)
                    .OrderByDescending("createdAt")
                    .LimitTo(limit);

                var receivedQuery = _firestore.GetCollection(TransactionsCollection)
                    .WhereEqualsTo("toUserId", userId)
                    .OrderByDescending("createdAt")
                    .LimitTo(limit);

                var sentTransactions = await sentQuery.GetDocumentsAsync<Transaction>();
                var receivedTransactions = await receivedQuery.GetDocumentsAsync<Transaction>();

                if (sentTransactions != null)
                    transactions.AddRange(sentTransactions);

                if (receivedTransactions != null)
                {
                    foreach (var transaction in receivedTransactions)
                    {
                        // Evitar duplicados en transferencias
                        if (!transactions.Any(t => t.Id == transaction.Id))
                            transactions.Add(transaction);
                    }
                }

                // Ordenar por fecha
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
                // Verificar que el usuario origen tiene suficientes puntos
                var fromUser = await GetUserAsync(fromUserId);
                var toUser = await GetUserAsync(toUserId);
                
                if (fromUser == null || toUser == null)
                    return false;
                
                if (fromUser.Points < points)
                    return false;
                
                // Actualizar puntos
                await UpdateUserPointsAsync(fromUserId, -points);
                await UpdateUserPointsAsync(toUserId, points);
                
                // Crear transacción de envío
                var sendTransaction = new Transaction
                {
                    Type = TransactionType.Transferred,
                    Amount = points,
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    Description = description ?? $"Transferencia a {toUser.DisplayName}",
                    CreatedAt = DateTime.UtcNow
                };
                
                // Crear transacción de recepción
                var receiveTransaction = new Transaction
                {
                    Type = TransactionType.Received,
                    Amount = points,
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    Description = description ?? $"Transferencia de {fromUser.DisplayName}",
                    CreatedAt = DateTime.UtcNow
                };
                
                await CreateTransactionAsync(sendTransaction);
                await CreateTransactionAsync(receiveTransaction);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error transferring points: {ex.Message}");
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

                return rewards?.ToList() ?? new List<Reward>();
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

                return rewards?.ToList() ?? new List<Reward>();
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
                var redemptions = await _firestore.GetCollection(RedemptionsCollection)
                    .WhereEqualsTo("userId", userId)
                    .OrderByDescending("redeemedAt")
                    .GetDocumentsAsync<Redemption>();

                if (redemptions != null)
                {
                    var redemptionsList = redemptions.ToList();
                    // Obtener información de la recompensa
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
                var redemptions = await _firestore.GetCollection(RedemptionsCollection)
                    .OrderByDescending("redeemedAt")
                    .GetDocumentsAsync<Redemption>();

                return redemptions?.ToList() ?? new List<Redemption>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all redemptions: {ex.Message}");
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
                    Type = TransactionType.Spent,
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
                var updates = new Dictionary<string, object>
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
                var stats = new Dictionary<string, object>();

                // Total de usuarios
                var users = await _firestore.GetCollection(UsersCollection).GetDocumentsAsync<User>();
                var usersList = users?.ToList() ?? new List<User>();
                stats["totalUsers"] = usersList.Count;

                // Total de puntos en circulación
                var totalPoints = usersList.Sum(u => u.Points);
                stats["totalPoints"] = totalPoints;

                // Total de transacciones
                var transactions = await _firestore.GetCollection(TransactionsCollection).GetDocumentsAsync<Transaction>();
                stats["totalTransactions"] = transactions?.Count() ?? 0;

                // Total de canjes
                var redemptions = await _firestore.GetCollection(RedemptionsCollection).GetDocumentsAsync<Redemption>();
                stats["totalRedemptions"] = redemptions?.Count() ?? 0;

                // Recompensas activas
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
            // Por simplicidad, escuchamos solo las transacciones donde el usuario es destinatario
            return _firestore.GetCollection(TransactionsCollection)
                .WhereEqualsTo("toUserId", userId)
                .OrderByDescending("createdAt")
                .LimitTo(50)
                .AddSnapshotListener<Transaction>(async (querySnapshot) =>
                {
                    if (querySnapshot != null)
                    {
                        // Obtener todas las transacciones del usuario
                        var transactions = await GetUserTransactionsAsync(userId);
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
                        var rewards = querySnapshot.ToList();
                        onUpdate(rewards);
                    }
                });
        }

        #endregion
    }
}
