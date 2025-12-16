using System;
using Plugin.Firebase.Firestore;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AcuPuntos.Models
{
    public enum AppointmentStatus
    {
        Available,
        PendingApproval,
        Confirmed,
        Cancelled,
        ModificationRequested
    }

    public class AppointmentSlot : ObservableObject, IFirestoreObject
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        private DateTimeOffset _startTime;
        [FirestoreProperty("startTime")]
        public DateTimeOffset StartTime
        {
            get => _startTime;
            set
            {
                if (SetProperty(ref _startTime, value))
                {
                    OnPropertyChanged(nameof(StartTimeLocal));
                }
            }
        }

        private DateTimeOffset _endTime;
        [FirestoreProperty("endTime")]
        public DateTimeOffset EndTime
        {
            get => _endTime;
            set
            {
                if (SetProperty(ref _endTime, value))
                {
                    OnPropertyChanged(nameof(EndTimeLocal));
                }
            }
        }

        [FirestoreProperty("userId")]
        public string? UserId { get; set; }

        [FirestoreProperty("durationMinutes")]
        public int DurationMinutes { get; set; } = 60; // Duración por defecto: 60 minutos

        private string _statusString = AppointmentStatus.Available.ToString();

        [FirestoreProperty("status")]
        public string StatusString
        {
            get => _statusString;
            set
            {
                if (SetProperty(ref _statusString, value))
                {
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        public AppointmentStatus Status
        {
            get => Enum.TryParse(StatusString, out AppointmentStatus status) ? status : AppointmentStatus.Available;
            set => StatusString = value.ToString();
        }

        [FirestoreProperty("userNotes")]
        public string? UserNotes { get; set; }

        [FirestoreProperty("adminNotes")]
        public string? AdminNotes { get; set; }

        [FirestoreProperty("requestedModification")]
        public string? RequestedModification { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Propiedades de UI (No mapeadas a Firestore)
        public DateTime StartTimeLocal => StartTime.LocalDateTime;
        public DateTime EndTimeLocal => EndTime.LocalDateTime;

        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    AppointmentStatus.Available => "Disponible",
                    AppointmentStatus.PendingApproval => "Pendiente de Aprobación",
                    AppointmentStatus.Confirmed => "Confirmado",
                    AppointmentStatus.Cancelled => "Cancelado",
                    AppointmentStatus.ModificationRequested => "Modificación Solicitada",
                    _ => StatusString
                };
            }
        }

        public User? User { get; set; }
    }
}
