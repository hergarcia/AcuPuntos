using System;
using Plugin.Firebase.Firestore;

namespace AcuPuntos.Models
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class Notification : IFirestoreObject
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty("userId")]
        public string? UserId { get; set; }

        [FirestoreProperty("title")]
        public string? Title { get; set; }

        [FirestoreProperty("message")]
        public string? Message { get; set; }

        [FirestoreProperty("type")]
        public string TypeString { get; set; } = NotificationType.Info.ToString();

        public NotificationType Type
        {
            get => Enum.TryParse(TypeString, out NotificationType type) ? type : NotificationType.Info;
            set => TypeString = value.ToString();
        }

        [FirestoreProperty("relatedEntityId")]
        public string? RelatedEntityId { get; set; }

        [FirestoreProperty("isRead")]
        public bool IsRead { get; set; } = false;

        [FirestoreProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
