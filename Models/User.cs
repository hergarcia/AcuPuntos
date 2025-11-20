using System;

namespace AcuPuntos.Models
{
    public class User
    {
        public string? Uid { get; set; }

        public string? Email { get; set; }

        public string? DisplayName { get; set; }

        public string? PhotoUrl { get; set; }

        public int Points { get; set; }

        public string Role { get; set; } = "user"; // "admin" or "user"

        public DateTime CreatedAt { get; set; }

        public DateTime LastLogin { get; set; }

        public string? FcmToken { get; set; }

        // Propiedades no mapeadas a Firestore
        public bool IsAdmin => Role == "admin";

        public User()
        {
            CreatedAt = DateTime.UtcNow;
            LastLogin = DateTime.UtcNow;
            Points = 0;
        }
    }
}
