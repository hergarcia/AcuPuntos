using System;
using Plugin.Firebase.Firestore;

namespace AcuPuntos.Models
{
    public class User : IFirestoreObject
    {
        [FirestoreDocumentId]
        public string? Uid { get; set; }

        [FirestoreProperty("email")]
        public string? Email { get; set; }

        [FirestoreProperty("displayName")]
        public string? DisplayName { get; set; }

        [FirestoreProperty("photoUrl")]
        public string? PhotoUrl { get; set; }

        [FirestoreProperty("points")]
        public int Points { get; set; }

        [FirestoreProperty("role")]
        public string Role { get; set; } = "user"; // "admin" or "user"

        [FirestoreProperty("createdAt")]
        public DateTimeOffset? CreatedAt { get; set; }

        [FirestoreProperty("lastLogin")]
        public DateTimeOffset? LastLogin { get; set; }

        [FirestoreProperty("fcmToken")]
        public string? FcmToken { get; set; }

        // Propiedades no mapeadas a Firestore
        public bool IsAdmin => Role == "admin";

        public User()
        {
            CreatedAt = DateTimeOffset.UtcNow;
            LastLogin = DateTimeOffset.UtcNow;
            Points = 0;
        }
    }
}
