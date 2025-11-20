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
                var document = await _firestore.Collection(UsersCollection)
                    .Document(uid)
                    .GetAsync();
                
                if (document.Exists)
                {
                    var user = document.ToObject<User>();
                    if (user != null)
                        user.Uid = uid;
                    return user;
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
                var snapshot = await _firestore.Collection(UsersCollection)
                    .OrderBy("displayName")
                    .GetAsync();
                
                var users = new List<User>();
                foreach (var document in snapshot.Documents)
                {
                    var user = document.ToObject<User>();
                    if (user != null)
                    {
                        user.Uid = document.Id;
                        users.Add(user);
                    }
                }
                return users;
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
                
                await _firestore.Collection(UsersCollection)
                    .Document(user.Uid)
                    .SetAsync(user);
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
                
                await _firestore.Collection(UsersCollection)
                    .Document(user.Uid)
                    .UpdateAsync(user);
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
                var sentQuery = _firestore.Collection(TransactionsCollection)
                    .WhereEqualsTo("fromUserId", userId)
                    .OrderByDescending("createdAt")
                    .LimitTo(limit);
                
                var receivedQuery = _firestore.Collection(TransactionsCollection)
                    .WhereEqualsTo("toUserId", userId)
                    .OrderByDescending("createdAt")
                    .LimitTo(limit);
                
                var sentSnapshot = await sentQuery.GetAsync();
                var receivedSnapshot = await receivedQuery.GetAsync();
                
                foreach (var doc in sentSnapshot.Documents)
                {
                    var transaction = doc.ToObject<Transaction>();
                    if (transaction != null)
                    {
                        transaction.Id = doc.Id;
                        transactions.Add(transaction);
                    }
                }
                
                foreach (var doc in receivedSnapshot.Documents)
                {
                    var transaction = doc.ToObject<Transaction>();
                    if (transaction != null)
                    {
                        transaction.Id = doc.Id;
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
                var docRef = await _firestore.Collection(TransactionsCollection)
                    .AddAsync(transaction);
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
                var snapshot = await _firestore.Collection(RewardsCollection)
                    .WhereEqualsTo("isActive", true)
                    .OrderBy("pointsCost")
                    .GetAsync();
                
                var rewards = new List<Reward>();
                foreach (var doc in snapshot.Documents)
                {
                    var reward = doc.ToObject<Reward>();
                    if (reward != null)
                    {
                        reward.Id = doc.Id;
                        rewards.Add(reward);
                    }
                }
                return rewards;
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
                var snapshot = await _firestore.Collection(RewardsCollection)
                    .OrderBy("pointsCost")
                    .GetAsync();
                
                var rewards = new List<Reward>();
                foreach (var doc in snapshot.Documents)
                {
                    var reward = doc.ToObject<Reward>();
                    if (reward != null)
                    {
                        reward.Id = doc.Id;
                        rewards.Add(reward);
                    }
                }
                return rewards;
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
                var document = await _firestore.Collection(RewardsCollection)
                    .Document(rewardId)
                    .GetAsync();
                
                if (document.Exists)
                {
                    var reward = document.ToObject<Reward>();
                    if (reward != null)
                        reward.Id = rewardId;
                    return reward;
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
                var docRef = await _firestore.Collection(RewardsCollection)
                    .AddAsync(reward);
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
                
                await _firestore.Collection(RewardsCollection)
                    .Document(reward.Id)
                    .UpdateAsync(reward);
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
                await _firestore.Collection(RewardsCollection)
                    .Document(rewardId)
                    .DeleteAsync();
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
                var snapshot = await _firestore.Collection(RedemptionsCollection)
                    .WhereEqualsTo("userId", userId)
                    .OrderByDescending("redeemedAt")
                    .GetAsync();
                
                var redemptions = new List<Redemption>();
                foreach (var doc in snapshot.Documents)
                {
                    var redemption = doc.ToObject<Redemption>();
                    if (redemption != null)
                    {
                        redemption.Id = doc.Id;
                        // Obtener información de la recompensa
                        if (!string.IsNullOrEmpty(redemption.RewardId))
                        {
                            redemption.Reward = await GetRewardAsync(redemption.RewardId);
                        }
                        redemptions.Add(redemption);
                    }
                }
                return redemptions;
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
                var snapshot = await _firestore.Collection(RedemptionsCollection)
                    .OrderByDescending("redeemedAt")
                    .GetAsync();
                
                var redemptions = new List<Redemption>();
                foreach (var doc in snapshot.Documents)
                {
                    var redemption = doc.ToObject<Redemption>();
                    if (redemption != null)
                    {
                        redemption.Id = doc.Id;
                        redemptions.Add(redemption);
                    }
                }
                return redemptions;
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
                
                var docRef = await _firestore.Collection(RedemptionsCollection)
                    .AddAsync(redemption);
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
                
                await _firestore.Collection(RedemptionsCollection)
                    .Document(redemptionId)
                    .UpdateAsync(updates);
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
                var usersSnapshot = await _firestore.Collection(UsersCollection).GetAsync();
                stats["totalUsers"] = usersSnapshot.Count;
                
                // Total de puntos en circulación
                var totalPoints = 0;
                foreach (var doc in usersSnapshot.Documents)
                {
                    var user = doc.ToObject<User>();
                    if (user != null)
                        totalPoints += user.Points;
                }
                stats["totalPoints"] = totalPoints;
                
                // Total de transacciones
                var transactionsSnapshot = await _firestore.Collection(TransactionsCollection).GetAsync();
                stats["totalTransactions"] = transactionsSnapshot.Count;
                
                // Total de canjes
                var redemptionsSnapshot = await _firestore.Collection(RedemptionsCollection).GetAsync();
                stats["totalRedemptions"] = redemptionsSnapshot.Count;
                
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
            return _firestore.Collection(UsersCollection)
                .Document(uid)
                .AddSnapshotListener((snapshot, error) =>
                {
                    if (error != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in user listener: {error.Message}");
                        return;
                    }
                    
                    if (snapshot?.Exists == true)
                    {
                        var user = snapshot.ToObject<User>();
                        if (user != null)
                        {
                            user.Uid = uid;
                            onUpdate(user);
                        }
                    }
                });
        }

        public IDisposable ListenToTransactions(string userId, Action<List<Transaction>> onUpdate)
        {
            // Por simplicidad, escuchamos solo las transacciones donde el usuario es destinatario
            return _firestore.Collection(TransactionsCollection)
                .WhereEqualsTo("toUserId", userId)
                .OrderByDescending("createdAt")
                .LimitTo(50)
                .AddSnapshotListener(async (snapshot, error) =>
                {
                    if (error != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in transactions listener: {error.Message}");
                        return;
                    }
                    
                    if (snapshot != null)
                    {
                        // Obtener todas las transacciones del usuario
                        var transactions = await GetUserTransactionsAsync(userId);
                        onUpdate(transactions);
                    }
                });
        }

        public IDisposable ListenToRewards(Action<List<Reward>> onUpdate)
        {
            return _firestore.Collection(RewardsCollection)
                .WhereEqualsTo("isActive", true)
                .OrderBy("pointsCost")
                .AddSnapshotListener((snapshot, error) =>
                {
                    if (error != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in rewards listener: {error.Message}");
                        return;
                    }
                    
                    if (snapshot != null)
                    {
                        var rewards = new List<Reward>();
                        foreach (var doc in snapshot.Documents)
                        {
                            var reward = doc.ToObject<Reward>();
                            if (reward != null)
                            {
                                reward.Id = doc.Id;
                                rewards.Add(reward);
                            }
                        }
                        onUpdate(rewards);
                    }
                });
        }

        #endregion
    }
}
