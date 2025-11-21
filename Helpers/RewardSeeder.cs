using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcuPuntos.Models;
using AcuPuntos.Services;

namespace AcuPuntos.Helpers
{
    /// <summary>
    /// Helper para crear recompensas predefinidas en Firestore
    /// </summary>
    public static class RewardSeeder
    {
        public static async Task SeedRewardsAsync(IFirestoreService firestoreService)
        {
            try
            {
                var rewards = GetPredefinedRewards();

                foreach (var reward in rewards)
                {
                    await firestoreService.CreateRewardAsync(reward);
                    System.Diagnostics.Debug.WriteLine($"Recompensa creada: {reward.Name}");
                }

                System.Diagnostics.Debug.WriteLine($"✅ {rewards.Count} recompensas creadas exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error seeding rewards: {ex.Message}");
            }
        }

        private static List<Reward> GetPredefinedRewards()
        {
            return new List<Reward>
            {
                // Servicios
                new Reward
                {
                    Name = "Consulta Médica General",
                    Description = "Una consulta médica general gratuita",
                    PointsCost = 500,
                    Category = "Servicios",
                    Icon = "🩺",
                    IsActive = true,
                    MaxRedemptionsPerUser = 3
                },
                new Reward
                {
                    Name = "Sesión de Fisioterapia",
                    Description = "Una sesión de fisioterapia de 1 hora",
                    PointsCost = 750,
                    Category = "Servicios",
                    Icon = "💆",
                    IsActive = true,
                    MaxRedemptionsPerUser = 5
                },
                new Reward
                {
                    Name = "Masaje Terapéutico",
                    Description = "Masaje terapéutico de 60 minutos",
                    PointsCost = 800,
                    Category = "Servicios",
                    Icon = "💆‍♀️",
                    IsActive = true,
                    MaxRedemptionsPerUser = 4
                },
                new Reward
                {
                    Name = "Limpieza Dental",
                    Description = "Limpieza dental profesional",
                    PointsCost = 600,
                    Category = "Servicios",
                    Icon = "🦷",
                    IsActive = true,
                    MaxRedemptionsPerUser = 2
                },
                new Reward
                {
                    Name = "Examen de la Vista",
                    Description = "Examen oftalmológico completo",
                    PointsCost = 400,
                    Category = "Servicios",
                    Icon = "👓",
                    IsActive = true,
                    MaxRedemptionsPerUser = 2
                },

                // Productos
                new Reward
                {
                    Name = "Kit de Primeros Auxilios",
                    Description = "Kit completo de primeros auxilios para el hogar",
                    PointsCost = 350,
                    Category = "Productos",
                    Icon = "🏥",
                    IsActive = true,
                    MaxRedemptionsPerUser = 1
                },
                new Reward
                {
                    Name = "Termómetro Digital",
                    Description = "Termómetro digital de alta precisión",
                    PointsCost = 200,
                    Category = "Productos",
                    Icon = "🌡️",
                    IsActive = true,
                    MaxRedemptionsPerUser = 2
                },
                new Reward
                {
                    Name = "Oxímetro de Pulso",
                    Description = "Oxímetro de pulso portátil",
                    PointsCost = 300,
                    Category = "Productos",
                    Icon = "📱",
                    IsActive = true,
                    MaxRedemptionsPerUser = 1
                },
                new Reward
                {
                    Name = "Paquete de Vitaminas",
                    Description = "Paquete de vitaminas y suplementos (3 meses)",
                    PointsCost = 450,
                    Category = "Productos",
                    Icon = "💊",
                    IsActive = true,
                    MaxRedemptionsPerUser = 4
                },
                new Reward
                {
                    Name = "Tensiómetro Digital",
                    Description = "Monitor de presión arterial digital",
                    PointsCost = 400,
                    Category = "Productos",
                    Icon = "🩺",
                    IsActive = true,
                    MaxRedemptionsPerUser = 1
                },

                // Descuentos
                new Reward
                {
                    Name = "20% de Descuento en Medicamentos",
                    Description = "Cupón de 20% de descuento en tu próxima compra de medicamentos",
                    PointsCost = 150,
                    Category = "Descuentos",
                    Icon = "💳",
                    IsActive = true,
                    MaxRedemptionsPerUser = 10,
                    ExpiryDate = DateTimeOffset.UtcNow.AddMonths(6)
                },
                new Reward
                {
                    Name = "30% de Descuento en Análisis de Laboratorio",
                    Description = "Cupón de 30% de descuento en análisis clínicos",
                    PointsCost = 250,
                    Category = "Descuentos",
                    Icon = "🧪",
                    IsActive = true,
                    MaxRedemptionsPerUser = 5,
                    ExpiryDate = DateTimeOffset.UtcNow.AddMonths(6)
                },
                new Reward
                {
                    Name = "50% de Descuento en Óptica",
                    Description = "Cupón de 50% de descuento en lentes y monturas",
                    PointsCost = 300,
                    Category = "Descuentos",
                    Icon = "👓",
                    IsActive = true,
                    MaxRedemptionsPerUser = 2,
                    ExpiryDate = DateTimeOffset.UtcNow.AddMonths(6)
                },
                new Reward
                {
                    Name = "15% de Descuento en Farmacia",
                    Description = "Cupón de 15% de descuento en toda la farmacia",
                    PointsCost = 100,
                    Category = "Descuentos",
                    Icon = "💊",
                    IsActive = true,
                    MaxRedemptionsPerUser = 15,
                    ExpiryDate = DateTimeOffset.UtcNow.AddMonths(6)
                },

                // Especial
                new Reward
                {
                    Name = "Chequeo Médico Completo",
                    Description = "Chequeo médico completo con análisis de sangre incluidos",
                    PointsCost = 1200,
                    Category = "Especial",
                    Icon = "⚕️",
                    IsActive = true,
                    MaxRedemptionsPerUser = 1
                },
                new Reward
                {
                    Name = "Plan de Nutrición Personalizado",
                    Description = "Plan de nutrición personalizado con seguimiento de 3 meses",
                    PointsCost = 900,
                    Category = "Especial",
                    Icon = "🥗",
                    IsActive = true,
                    MaxRedemptionsPerUser = 2
                },
                new Reward
                {
                    Name = "Sesión con Psicólogo",
                    Description = "Sesión de terapia psicológica de 1 hora",
                    PointsCost = 700,
                    Category = "Especial",
                    Icon = "🧠",
                    IsActive = true,
                    MaxRedemptionsPerUser = 5
                },
                new Reward
                {
                    Name = "Programa de Ejercicios Personalizados",
                    Description = "Plan de ejercicios personalizado con entrenador por 1 mes",
                    PointsCost = 850,
                    Category = "Especial",
                    Icon = "💪",
                    IsActive = true,
                    MaxRedemptionsPerUser = 3
                },
                new Reward
                {
                    Name = "Kit de Bienestar Premium",
                    Description = "Kit completo con productos de bienestar y relajación",
                    PointsCost = 1000,
                    Category = "Especial",
                    Icon = "🎁",
                    IsActive = true,
                    MaxRedemptionsPerUser = 1
                },
                new Reward
                {
                    Name = "Membresía Gimnasio - 1 Mes",
                    Description = "Membresía de 1 mes en gimnasio afiliado",
                    PointsCost = 650,
                    Category = "Especial",
                    Icon = "🏋️",
                    IsActive = true,
                    MaxRedemptionsPerUser = 6
                }
            };
        }
    }
}
