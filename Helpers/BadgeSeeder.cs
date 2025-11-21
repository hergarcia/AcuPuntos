using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcuPuntos.Models;
using AcuPuntos.Services;

namespace AcuPuntos.Helpers
{
    /// <summary>
    /// Helper para crear badges predefinidos en Firestore
    /// </summary>
    public static class BadgeSeeder
    {
        public static async Task SeedBadgesAsync(IFirestoreService firestoreService)
        {
            try
            {
                var badges = GetPredefinedBadges();

                foreach (var badge in badges)
                {
                    await firestoreService.CreateBadgeAsync(badge);
                    System.Diagnostics.Debug.WriteLine($"Badge creado: {badge.Name}");
                }

                System.Diagnostics.Debug.WriteLine($"✅ {badges.Count} badges creados exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error seeding badges: {ex.Message}");
            }
        }

        private static List<Badge> GetPredefinedBadges()
        {
            return new List<Badge>
            {
                // Badges de Nivel
                new Badge
                {
                    Name = "Novato",
                    Description = "Alcanza el nivel 2",
                    Icon = "🌱",
                    Category = "Nivel",
                    RequiredLevel = 2,
                    RequiredPoints = 0,
                    Rarity = 0, // Common
                    Order = 1
                },
                new Badge
                {
                    Name = "Aprendiz",
                    Description = "Alcanza el nivel 5",
                    Icon = "🌿",
                    Category = "Nivel",
                    RequiredLevel = 5,
                    RequiredPoints = 0,
                    Rarity = 0, // Common
                    Order = 2
                },
                new Badge
                {
                    Name = "Experto",
                    Description = "Alcanza el nivel 10",
                    Icon = "🍀",
                    Category = "Nivel",
                    RequiredLevel = 10,
                    RequiredPoints = 0,
                    Rarity = 1, // Uncommon
                    Order = 3
                },
                new Badge
                {
                    Name = "Maestro",
                    Description = "Alcanza el nivel 20",
                    Icon = "🌳",
                    Category = "Nivel",
                    RequiredLevel = 20,
                    RequiredPoints = 0,
                    Rarity = 2, // Rare
                    Order = 4
                },
                new Badge
                {
                    Name = "Leyenda",
                    Description = "Alcanza el nivel 50",
                    Icon = "🏆",
                    Category = "Nivel",
                    RequiredLevel = 50,
                    RequiredPoints = 0,
                    Rarity = 4, // Legendary
                    Order = 5
                },

                // Badges de Puntos
                new Badge
                {
                    Name = "Ahorrador",
                    Description = "Acumula 500 puntos en total",
                    Icon = "💰",
                    Category = "Puntos",
                    RequiredLevel = 0,
                    RequiredPoints = 500,
                    Rarity = 0, // Common
                    Order = 10
                },
                new Badge
                {
                    Name = "Millonario",
                    Description = "Acumula 1000 puntos en total",
                    Icon = "💎",
                    Category = "Puntos",
                    RequiredLevel = 0,
                    RequiredPoints = 1000,
                    Rarity = 1, // Uncommon
                    Order = 11
                },
                new Badge
                {
                    Name = "Magnate",
                    Description = "Acumula 5000 puntos en total",
                    Icon = "👑",
                    Category = "Puntos",
                    RequiredLevel = 0,
                    RequiredPoints = 5000,
                    Rarity = 3, // Epic
                    Order = 12
                },

                // Badges Generosos
                new Badge
                {
                    Name = "Generoso",
                    Description = "Envía 100 puntos a otros usuarios",
                    Icon = "🎁",
                    Category = "Generoso",
                    RequiredLevel = 0,
                    RequiredPoints = 100,
                    Rarity = 1, // Uncommon
                    Order = 20
                },
                new Badge
                {
                    Name = "Filántropo",
                    Description = "Envía 500 puntos a otros usuarios",
                    Icon = "🌟",
                    Category = "Generoso",
                    RequiredLevel = 0,
                    RequiredPoints = 500,
                    Rarity = 2, // Rare
                    Order = 21
                },

                // Badges Coleccionista
                new Badge
                {
                    Name = "Coleccionista",
                    Description = "Canjea 5 recompensas",
                    Icon = "🎯",
                    Category = "Coleccionista",
                    RequiredLevel = 0,
                    RequiredPoints = 250,
                    Rarity = 1, // Uncommon
                    Order = 30
                },
                new Badge
                {
                    Name = "Cazatesoros",
                    Description = "Canjea 20 recompensas",
                    Icon = "🏅",
                    Category = "Coleccionista",
                    RequiredLevel = 0,
                    RequiredPoints = 1000,
                    Rarity = 3, // Epic
                    Order = 31
                },

                // Badges de Dedicación
                new Badge
                {
                    Name = "Dedicado",
                    Description = "Mantén una racha de 7 días consecutivos",
                    Icon = "🔥",
                    Category = "Dedicado",
                    RequiredLevel = 7,
                    RequiredPoints = 0,
                    Rarity = 1, // Uncommon
                    Order = 40
                },
                new Badge
                {
                    Name = "Inquebrantable",
                    Description = "Mantén una racha de 30 días consecutivos",
                    Icon = "⚡",
                    Category = "Dedicado",
                    RequiredLevel = 30,
                    RequiredPoints = 0,
                    Rarity = 3, // Epic
                    Order = 41
                },

                // Badges Especiales
                new Badge
                {
                    Name = "Pionero",
                    Description = "Uno de los primeros 100 usuarios",
                    Icon = "🚀",
                    Category = "Especial",
                    RequiredLevel = 1,
                    RequiredPoints = 0,
                    Rarity = 2, // Rare
                    Order = 50
                },
                new Badge
                {
                    Name = "Bienvenido",
                    Description = "Completa tu primer inicio de sesión",
                    Icon = "👋",
                    Category = "Especial",
                    RequiredLevel = 1,
                    RequiredPoints = 0,
                    Rarity = 0, // Common
                    Order = 51
                }
            };
        }
    }
}
