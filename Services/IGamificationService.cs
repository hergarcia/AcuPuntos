using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcuPuntos.Models;

namespace AcuPuntos.Services
{
    public interface IGamificationService
    {
        // Experiencia y Niveles
        Task<int> AddExperienceAsync(string userId, int experience, string reason);
        int CalculateLevel(int experience);
        int GetExperienceForLevel(int level);
        int GetExperienceToNextLevel(int currentExperience);
        double GetLevelProgress(int currentExperience);

        // Badges
        Task<List<UserBadge>> GetUserBadgesAsync(string userId);
        Task<List<Badge>> GetAllBadgesAsync();
        Task<UserBadge?> AwardBadgeAsync(string userId, string badgeId);
        Task<List<Badge>> CheckAndAwardBadgesAsync(string userId);

        // Check-in diario
        Task<(bool success, int bonus, int streak)> DailyCheckInAsync(string userId);

        // Estadísticas
        Task<Dictionary<string, object>> GetGamificationStatsAsync(string userId);
    }
}
