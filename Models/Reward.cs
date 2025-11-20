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
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty("maxRedemptionsPerUser")]
        public int? MaxRedemptionsPerUser { get; set; }

        [FirestoreProperty("expiryDate")]
        public DateTime? ExpiryDate { get; set; }

        // Propiedades para UI
        public bool CanRedeem { get; set; }
        public string? DisabledReason { get; set; }

        public Reward()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        public Color GetCategoryColor()
        {
            return Category?.ToLower() switch
            {
                "servicios" => Color.FromArgb("#2ECC71"),
                "productos" => Color.FromArgb("#3498DB"),
                "descuentos" => Color.FromArgb("#E74C3C"),
                "especial" => Color.FromArgb("#F39C12"),
                _ => Color.FromArgb("#95A5A6")
            };
        }
    }
}
