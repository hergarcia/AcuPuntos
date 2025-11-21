using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcuPuntos.Models;

namespace AcuPuntos.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly IFirestoreService _firestoreService;

        // Fórmula de experiencia: XP requerido = nivel^2 * 100
        private const int BASE_XP_MULTIPLIER = 100;

        public GamificationService(IFirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        #region Experiencia y Niveles

        public async Task<int> AddExperienceAsync(string userId, int experience, string reason)
        {
            try
            {
                var user = await _firestoreService.GetUserAsync(userId);
                if (user == null)
                    return 0;

                int oldLevel = user.Level;
                int newExperience = user.Experience + experience;
                int newLevel = CalculateLevel(newExperience);

                // Actualizar experiencia y nivel del usuario
                await _firestoreService.UpdateUserGamificationAsync(userId, newExperience, newLevel);

                System.Diagnostics.Debug.WriteLine($"[Gamification] Usuario {userId}: +{experience} XP ({reason}). Nivel: {oldLevel} -> {newLevel}");

                // Si subió de nivel, verificar y otorgar badges
                if (newLevel > oldLevel)
                {
                    await CheckAndAwardBadgesAsync(userId);
                }

                return newLevel - oldLevel; // Retorna cuántos niveles subió
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding experience: {ex.Message}");
                return 0;
            }
        }

        public int CalculateLevel(int experience)
        {
            // Nivel = √(XP / 100)
            // Nivel 1: 0-100 XP
            // Nivel 2: 100-400 XP (300 XP para subir)
            // Nivel 3: 400-900 XP (500 XP para subir)
            // Nivel 4: 900-1600 XP (700 XP para subir)
            // etc.

            if (experience < 0)
                return 1;

            int level = (int)Math.Floor(Math.Sqrt(experience / (double)BASE_XP_MULTIPLIER)) + 1;
            return Math.Max(1, level);
        }

        public int GetExperienceForLevel(int level)
        {
            // XP requerido para alcanzar un nivel específico
            if (level <= 1)
                return 0;

            return (level - 1) * (level - 1) * BASE_XP_MULTIPLIER;
        }

        public int GetExperienceToNextLevel(int currentExperience)
        {
            int currentLevel = CalculateLevel(currentExperience);
            int nextLevelXP = GetExperienceForLevel(currentLevel + 1);
            return nextLevelXP - currentExperience;
        }

        public double GetLevelProgress(int currentExperience)
        {
            int currentLevel = CalculateLevel(currentExperience);
            int currentLevelXP = GetExperienceForLevel(currentLevel);
            int nextLevelXP = GetExperienceForLevel(currentLevel + 1);

            if (nextLevelXP <= currentLevelXP)
                return 1.0;

            double progress = (double)(currentExperience - currentLevelXP) / (nextLevelXP - currentLevelXP);
            return Math.Max(0, Math.Min(1, progress)); // Entre 0 y 1
        }

        #endregion

        #region Badges

        public async Task<List<UserBadge>> GetUserBadgesAsync(string userId)
        {
            try
            {
                return await _firestoreService.GetUserBadgesAsync(userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user badges: {ex.Message}");
                return new List<UserBadge>();
            }
        }

        public async Task<List<Badge>> GetAllBadgesAsync()
        {
            try
            {
                return await _firestoreService.GetAllBadgesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all badges: {ex.Message}");
                return new List<Badge>();
            }
        }

        public async Task<UserBadge?> AwardBadgeAsync(string userId, string badgeId)
        {
            try
            {
                // Verificar si el usuario ya tiene el badge
                var userBadges = await GetUserBadgesAsync(userId);
                if (userBadges.Any(ub => ub.BadgeId == badgeId))
                {
                    System.Diagnostics.Debug.WriteLine($"[Gamification] Usuario {userId} ya tiene el badge {badgeId}");
                    return null;
                }

                // Crear el UserBadge
                var userBadge = new UserBadge
                {
                    UserId = userId,
                    BadgeId = badgeId,
                    EarnedAt = DateTimeOffset.UtcNow
                };

                await _firestoreService.CreateUserBadgeAsync(userBadge);

                System.Diagnostics.Debug.WriteLine($"[Gamification] Badge {badgeId} otorgado a usuario {userId}");
                return userBadge;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error awarding badge: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Badge>> CheckAndAwardBadgesAsync(string userId)
        {
            try
            {
                var user = await _firestoreService.GetUserAsync(userId);
                if (user == null)
                    return new List<Badge>();

                var allBadges = await GetAllBadgesAsync();
                var userBadges = await GetUserBadgesAsync(userId);
                var userBadgeIds = userBadges.Select(ub => ub.BadgeId).ToHashSet();

                var newlyAwardedBadges = new List<Badge>();

                foreach (var badge in allBadges.Where(b => b.IsActive))
                {
                    // Si ya tiene el badge, saltarlo
                    if (userBadgeIds.Contains(badge.Id))
                        continue;

                    // Verificar si cumple los requisitos
                    bool meetsRequirements = true;

                    if (badge.RequiredLevel > 0 && user.Level < badge.RequiredLevel)
                        meetsRequirements = false;

                    if (badge.RequiredPoints > 0 && user.TotalPointsEarned < badge.RequiredPoints)
                        meetsRequirements = false;

                    // Requisitos especiales por categoría
                    if (meetsRequirements)
                    {
                        meetsRequirements = await CheckSpecialBadgeRequirements(userId, badge, user);
                    }

                    // Otorgar badge si cumple requisitos
                    if (meetsRequirements)
                    {
                        var awarded = await AwardBadgeAsync(userId, badge.Id!);
                        if (awarded != null)
                        {
                            newlyAwardedBadges.Add(badge);
                        }
                    }
                }

                return newlyAwardedBadges;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking and awarding badges: {ex.Message}");
                return new List<Badge>();
            }
        }

        private async Task<bool> CheckSpecialBadgeRequirements(string userId, Badge badge, User user)
        {
            try
            {
                // Requisitos especiales según la categoría del badge
                switch (badge.Category.ToLower())
                {
                    case "generoso":
                        // Requiere X transferencias enviadas
                        var transactions = await _firestoreService.GetUserTransactionsAsync(userId, 1000);
                        var sentCount = transactions.Count(t => t.Type == TransactionType.Sent);
                        return sentCount >= badge.RequiredPoints / 10; // Por ejemplo, 1 transferencia por cada 10 puntos requeridos

                    case "coleccionista":
                        // Requiere X canjes completados
                        var redemptions = await _firestoreService.GetUserRedemptionsAsync(userId);
                        var completedCount = redemptions.Count(r => r.Status == RedemptionStatus.Completed);
                        return completedCount >= badge.RequiredPoints / 50; // 1 canje por cada 50 puntos

                    case "dedicado":
                        // Requiere racha de días consecutivos
                        return user.ConsecutiveDays >= badge.RequiredLevel;

                    default:
                        return true; // Sin requisitos especiales
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking special badge requirements: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Check-in Diario

        public async Task<(bool success, int bonus, int streak)> DailyCheckInAsync(string userId)
        {
            try
            {
                var user = await _firestoreService.GetUserAsync(userId);
                if (user == null)
                    return (false, 0, 0);

                var now = DateTimeOffset.UtcNow;
                var lastCheckIn = user.LastCheckIn;

                // Verificar si ya hizo check-in hoy
                if (lastCheckIn.HasValue)
                {
                    var timeSinceLastCheckIn = now - lastCheckIn.Value;

                    // Si ya hizo check-in hoy (menos de 24 horas), no dar bonus
                    if (timeSinceLastCheckIn.TotalHours < 20) // Margen de 4 horas
                    {
                        return (false, 0, user.ConsecutiveDays);
                    }

                    // Si pasaron más de 48 horas, resetear racha
                    if (timeSinceLastCheckIn.TotalHours > 48)
                    {
                        user.ConsecutiveDays = 0;
                    }
                }

                // Incrementar racha
                user.ConsecutiveDays++;
                user.LastCheckIn = now;

                // Calcular bonus (aumenta con la racha)
                int baseBonus = 10;
                int streakBonus = Math.Min(user.ConsecutiveDays * 5, 100); // Máximo 100 puntos de bonus por racha
                int totalBonus = baseBonus + streakBonus;

                // Dar puntos y experiencia
                await _firestoreService.AssignPointsToUserAsync(userId, totalBonus, $"Check-in diario (Racha: {user.ConsecutiveDays} días)");
                await AddExperienceAsync(userId, totalBonus / 2, "Check-in diario");

                // Actualizar lastCheckIn y consecutiveDays
                await _firestoreService.UpdateUserCheckInAsync(userId, now, user.ConsecutiveDays);

                return (true, totalBonus, user.ConsecutiveDays);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in daily check-in: {ex.Message}");
                return (false, 0, 0);
            }
        }

        #endregion

        #region Estadísticas

        public async Task<Dictionary<string, object>> GetGamificationStatsAsync(string userId)
        {
            try
            {
                var stats = new Dictionary<string, object>();
                var user = await _firestoreService.GetUserAsync(userId);

                if (user == null)
                    return stats;

                var userBadges = await GetUserBadgesAsync(userId);

                stats["level"] = user.Level;
                stats["experience"] = user.Experience;
                stats["experienceToNextLevel"] = GetExperienceToNextLevel(user.Experience);
                stats["levelProgress"] = GetLevelProgress(user.Experience);
                stats["totalBadges"] = userBadges.Count;
                stats["consecutiveDays"] = user.ConsecutiveDays;
                stats["totalPointsEarned"] = user.TotalPointsEarned;
                stats["totalPointsSpent"] = user.TotalPointsSpent;

                return stats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting gamification stats: {ex.Message}");
                return new Dictionary<string, object>();
            }
        }

        #endregion
    }
}
