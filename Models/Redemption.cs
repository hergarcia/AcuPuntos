using System;
using Microsoft.Maui.Graphics;

namespace AcuPuntos.Models
{
    public class Redemption
    {
        public string? Id { get; set; }

        public string? UserId { get; set; }

        public string? RewardId { get; set; }

        public int PointsUsed { get; set; }

        public RedemptionStatus Status { get; set; }

        public DateTime RedeemedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

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
