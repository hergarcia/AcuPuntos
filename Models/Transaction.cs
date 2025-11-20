using System;
using Plugin.Firebase.Firestore;

namespace AcuPuntos.Models
{
    public class Transaction : IFirestoreObject
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        // Propiedad interna para Firestore (maneja la conversión int <-> enum)
        [FirestoreProperty("type")]
        private long TypeValue
        {
            get => (long)Type;
            set => Type = (TransactionType)value;
        }

        // Propiedad pública para uso en código (sin atributo = ignorada por Firestore)
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
        public DateTimeOffset? CreatedAt { get; set; }

        [FirestoreProperty("rewardId")]
        public string? RewardId { get; set; }

        // Propiedades adicionales para mostrar en UI
        public string? FromUserName { get; set; }
        public string? ToUserName { get; set; }
        public string? FromUserPhoto { get; set; }
        public string? ToUserPhoto { get; set; }

        public Transaction()
        {
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public string Icon => Type switch
        {
            TransactionType.Received => "📩",
            TransactionType.Reward => "🎁",
            TransactionType.Sent => "📤",
            TransactionType.Redemption => "🎯",
            _ => "📝"
        };

        public string FormattedAmount
        {
            get
            {
                var sign = Type == TransactionType.Received || Type == TransactionType.Reward ? "+" : "-";
                return $"{sign}{Amount} pts";
            }
        }
    }

    public enum TransactionType
    {
        Received,      // Transferencia recibida de otro usuario
        Reward,        // Puntos recibidos por recompensa/admin
        Sent,          // Transferencia enviada a otro usuario
        Redemption     // Puntos gastados en canje de recompensa
    }
}
