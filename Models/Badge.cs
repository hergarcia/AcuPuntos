using System;
using Plugin.Firebase.Firestore;

namespace AcuPuntos.Models
{
    public class Badge : IFirestoreObject
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("icon")]
        public string Icon { get; set; } = string.Empty;

        [FirestoreProperty("category")]
        public string Category { get; set; } = string.Empty; // Ej: "Principiante", "Maestro", "Generoso", etc.

        [FirestoreProperty("requiredPoints")]
        public int RequiredPoints { get; set; }

        [FirestoreProperty("requiredLevel")]
        public int RequiredLevel { get; set; }

        [FirestoreProperty("rarity")]
        public int Rarity { get; set; } = 0; // 0=Common, 1=Uncommon, 2=Rare, 3=Epic, 4=Legendary

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; } = true;

        [FirestoreProperty("order")]
        public int Order { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTimeOffset? CreatedAt { get; set; }

        public Badge()
        {
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }

    public enum BadgeRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    // Modelo para badges del usuario (relación muchos a muchos)
    public class UserBadge : IFirestoreObject
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("badgeId")]
        public string BadgeId { get; set; } = string.Empty;

        [FirestoreProperty("earnedAt")]
        public DateTimeOffset EarnedAt { get; set; }

        [FirestoreProperty("isDisplayed")]
        public bool IsDisplayed { get; set; } = false;

        // No mapeada - se carga dinámicamente
        public Badge? Badge { get; set; }

        public UserBadge()
        {
            EarnedAt = DateTimeOffset.UtcNow;
        }
    }
}
