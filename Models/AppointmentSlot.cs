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

        [FirestoreProperty("startTime")]
        public DateTimeOffset StartTime { get; set; }

        [FirestoreProperty("endTime")]
        public DateTimeOffset EndTime { get; set; }

        [FirestoreProperty("userId")]
        public string? UserId { get; set; }

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
