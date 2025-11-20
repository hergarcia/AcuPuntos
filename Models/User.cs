using System;
using Google.Cloud.Firestore;

namespace AcuPuntos.Models
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty]
        public string? Uid { get; set; }

        [FirestoreProperty]
        public string? Email { get; set; }

        [FirestoreProperty]
        public string? DisplayName { get; set; }

        [FirestoreProperty]
        public string? PhotoUrl { get; set; }

        [FirestoreProperty]
        public int Points { get; set; }

        [FirestoreProperty]
        public string Role { get; set; } = "user"; // "admin" or "user"

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty]
        public DateTime LastLogin { get; set; }

        [FirestoreProperty]
        public string? FcmToken { get; set; }

        // Propiedades no mapeadas a Firestore
        [FirestoreProperty(ConverterType = typeof(Google.Cloud.Firestore.ConverterRegistry))]
        public bool IsAdmin => Role == "admin";

        public User()
        {
            CreatedAt = DateTime.UtcNow;
            LastLogin = DateTime.UtcNow;
            Points = 0;
        }
    }
}
