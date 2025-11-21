using System;
using Microsoft.Maui.Graphics;
using Plugin.Firebase.Firestore;

namespace AcuPuntos.Models
{
    public class Reward : IFirestoreObject
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty("name")]
        public string? Name { get; set; }

        [FirestoreProperty("pointsCost")]
        public int PointsCost { get; set; }

        [FirestoreProperty("description")]
        public string? Description { get; set; }

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; }

        [FirestoreProperty("icon")]
        public string? Icon { get; set; }

        [FirestoreProperty("category")]
        public string? Category { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTimeOffset? CreatedAt { get; set; }

        [FirestoreProperty("maxRedemptionsPerUser")]
        public int? MaxRedemptionsPerUser { get; set; }

        [FirestoreProperty("expiryDate")]
        public DateTimeOffset? ExpiryDate { get; set; }

        // Propiedades para UI
        public bool CanRedeem { get; set; }
        public string? DisabledReason { get; set; }

        public Reward()
        {
            CreatedAt = DateTimeOffset.UtcNow;
            IsActive = true;
        }

        public Color GetCategoryColor()
        {
            return Category?.ToLower() switch
            {
                "servicios" => Color.FromArgb("#10B981"), // Success
                "productos" => Color.FromArgb("#0EA5E9"), // Secondary
                "descuentos" => Color.FromArgb("#EF4444"), // Danger
                "especial" => Color.FromArgb("#F59E0B"), // Warning
                _ => Color.FromArgb("#6B7280") // Gray
            };
        }
    }
}
