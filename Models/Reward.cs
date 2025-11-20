using System;
using Microsoft.Maui.Graphics;

namespace AcuPuntos.Models
{
    public class Reward
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public int PointsCost { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public string? Icon { get; set; }

        public string? Category { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? MaxRedemptionsPerUser { get; set; }

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
