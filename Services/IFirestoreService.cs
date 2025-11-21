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
        Task<bool> AssignPointsToUserAsync(string userId, int points, string description);
        Task<Dictionary<string, object>> GetUserStatsAsync(string userId);
        
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
        Task<List<Redemption>> GetPendingRedemptionsAsync();
        Task<Redemption?> RedeemRewardAsync(string userId, string rewardId);
        Task UpdateRedemptionStatusAsync(string redemptionId, RedemptionStatus status);
        
        // Estadísticas (para admin)
        Task<Dictionary<string, object>> GetStatisticsAsync();

        // Gamificación
        Task UpdateUserGamificationAsync(string userId, int experience, int level);
        Task UpdateUserCheckInAsync(string userId, DateTimeOffset lastCheckIn, int consecutiveDays);
        Task UpdateUserPointsTracking(string userId, int pointsDelta, bool isEarned);

        // Badges
        Task<List<Badge>> GetAllBadgesAsync();
        Task<Badge?> GetBadgeAsync(string badgeId);
        Task CreateBadgeAsync(Badge badge);
        Task<List<UserBadge>> GetUserBadgesAsync(string userId);
        Task CreateUserBadgeAsync(UserBadge userBadge);

        // Listeners en tiempo real
        IDisposable ListenToUserChanges(string uid, Action<User> onUpdate);
        IDisposable ListenToTransactions(string userId, Action<List<Transaction>> onUpdate);
        IDisposable ListenToRewards(Action<List<Reward>> onUpdate);
    }
}
