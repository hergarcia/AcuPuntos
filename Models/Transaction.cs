using System;
using Google.Cloud.Firestore;

namespace AcuPuntos.Models
{
    [FirestoreData]
    public class Transaction
    {
        [FirestoreProperty]
        public string? Id { get; set; }

        [FirestoreProperty]
        public TransactionType Type { get; set; }

        [FirestoreProperty]
        public int Amount { get; set; }

        [FirestoreProperty]
        public string? FromUserId { get; set; }

        [FirestoreProperty]
        public string? ToUserId { get; set; }

        [FirestoreProperty]
        public string? Description { get; set; }

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty]
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
