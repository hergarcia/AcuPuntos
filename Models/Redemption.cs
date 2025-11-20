using System;
using Google.Cloud.Firestore;

namespace AcuPuntos.Models
{
    [FirestoreData]
    public class Redemption
    {
        [FirestoreProperty]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string? UserId { get; set; }

        [FirestoreProperty]
        public string? RewardId { get; set; }

        [FirestoreProperty]
        public int PointsUsed { get; set; }

        [FirestoreProperty]
        public RedemptionStatus Status { get; set; }

        [FirestoreProperty]
        public DateTime RedeemedAt { get; set; }

        [FirestoreProperty]
        public DateTime? CompletedAt { get; set; }

        [FirestoreProperty]
        public string? Notes { get; set; }

        // Propiedades para UI
        public Reward? Reward { get; set; }
        public User? User { get; set; }

        public Redemption()
        {
            RedeemedAt = DateTime.UtcNow;
            Status = RedemptionStatus.Pending;
        }

        public Color GetStatusColor()
        {
            return Status switch
            {
                RedemptionStatus.Pending => Color.FromArgb("#F39C12"),
                RedemptionStatus.Completed => Color.FromArgb("#2ECC71"),
                RedemptionStatus.Cancelled => Color.FromArgb("#E74C3C"),
                _ => Color.FromArgb("#95A5A6")
            };
        }

        public string GetStatusText()
        {
            return Status switch
            {
                RedemptionStatus.Pending => "Pendiente",
                RedemptionStatus.Completed => "Completado",
                RedemptionStatus.Cancelled => "Cancelado",
                _ => "Desconocido"
            };
        }
    }

    public enum RedemptionStatus
    {
        Pending,
        Completed,
        Cancelled
    }
}
