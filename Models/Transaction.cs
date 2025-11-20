using System;
using Plugin.Firebase.Firestore;

namespace AcuPuntos.Models
{
    public class Transaction : IFirestoreObject
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty("type")]
        public TransactionType Type { get; set; }

        [FirestoreProperty("amount")]
        public int Amount { get; set; }

        [FirestoreProperty("fromUserId")]
        public string? FromUserId { get; set; }

        [FirestoreProperty("toUserId")]
        public string? ToUserId { get; set; }

        [FirestoreProperty("description")]
        public string? Description { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty("rewardId")]
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
