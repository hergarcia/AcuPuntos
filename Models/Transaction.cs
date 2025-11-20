using System;

namespace AcuPuntos.Models
{
    public class Transaction
    {
        public string? Id { get; set; }

        public TransactionType Type { get; set; }

        public int Amount { get; set; }

        public string? FromUserId { get; set; }

        public string? ToUserId { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? RewardId { get; set; }

        // Propiedades adicionales para mostrar en UI
        public string? FromUserName { get; set; }
        public string? ToUserName { get; set; }
        public string? FromUserPhoto { get; set; }
        public string? ToUserPhoto { get; set; }

        public Transaction()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public string GetIcon()
        {
            return Type switch
            {
                TransactionType.Earned => "🟢",
                TransactionType.Spent => "🔴",
                TransactionType.Transferred => "➡️",
                TransactionType.Received => "⬅️",
                _ => "⚪"
            };
        }

        public string GetFormattedAmount()
        {
            var sign = Type == TransactionType.Earned || Type == TransactionType.Received ? "+" : "-";
            return $"{sign}{Amount} pts";
        }
    }

    public enum TransactionType
    {
        Earned,
        Spent,
        Transferred,
        Received
    }
}
