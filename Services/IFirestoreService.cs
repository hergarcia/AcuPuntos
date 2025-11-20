using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcuPuntos.Models;

namespace AcuPuntos.Services
{
    public interface IFirestoreService
    {
        // Usuarios
        Task<User?> GetUserAsync(string uid);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> SearchUsersAsync(string searchTerm);
        Task CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task UpdateUserPointsAsync(string uid, int pointsDelta);
        
        // Transacciones
        Task<List<Transaction>> GetUserTransactionsAsync(string userId, int limit = 50);
        Task CreateTransactionAsync(Transaction transaction);
        Task<bool> TransferPointsAsync(string fromUserId, string toUserId, int points, string description);
        
        // Recompensas
        Task<List<Reward>> GetActiveRewardsAsync();
        Task<List<Reward>> GetAllRewardsAsync();
        Task<Reward?> GetRewardAsync(string rewardId);
        Task CreateRewardAsync(Reward reward);
        Task UpdateRewardAsync(Reward reward);
        Task DeleteRewardAsync(string rewardId);
        
        // Canjes
        Task<List<Redemption>> GetUserRedemptionsAsync(string userId);
        Task<List<Redemption>> GetAllRedemptionsAsync();
        Task<Redemption?> RedeemRewardAsync(string userId, string rewardId);
        Task UpdateRedemptionStatusAsync(string redemptionId, RedemptionStatus status);
        
        // Estadísticas (para admin)
        Task<Dictionary<string, object>> GetStatisticsAsync();
        
        // Listeners en tiempo real
        IDisposable ListenToUserChanges(string uid, Action<User> onUpdate);
        IDisposable ListenToTransactions(string userId, Action<List<Transaction>> onUpdate);
        IDisposable ListenToRewards(Action<List<Reward>> onUpdate);
    }
}
