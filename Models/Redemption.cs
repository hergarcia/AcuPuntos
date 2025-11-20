using System;
using Microsoft.Maui.Graphics;
using Plugin.Firebase.Firestore;

namespace AcuPuntos.Models
{
    public class Redemption : IFirestoreObject
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty("userId")]
        public string? UserId { get; set; }

        [FirestoreProperty("rewardId")]
        public string? RewardId { get; set; }

        [FirestoreProperty("pointsUsed")]
        public int PointsUsed { get; set; }

        // Propiedad interna para Firestore (maneja la conversión int <-> enum)
        [FirestoreProperty("status")]
        private long StatusValue
        {
            get => (long)Status;
            set => Status = (RedemptionStatus)value;
        }

        // Propiedad pública para uso en código (sin atributo = ignorada por Firestore)
        public RedemptionStatus Status { get; set; }

        [FirestoreProperty("redeemedAt")]
        public DateTime? RedeemedAt { get; set; }

        [FirestoreProperty("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [FirestoreProperty("notes")]
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
