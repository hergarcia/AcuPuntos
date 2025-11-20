using System;
using Google.Cloud.Firestore;

namespace AcuPuntos.Models
{
    [FirestoreData]
    public class Reward
    {
        [FirestoreProperty]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string? Name { get; set; }

        [FirestoreProperty]
        public int PointsCost { get; set; }

        [FirestoreProperty]
        public string? Description { get; set; }

        [FirestoreProperty]
        public bool IsActive { get; set; }

        [FirestoreProperty]
        public string? Icon { get; set; }

        [FirestoreProperty]
        public string? Category { get; set; }

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty]
        public int? MaxRedemptionsPerUser { get; set; }

        [FirestoreProperty]
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
