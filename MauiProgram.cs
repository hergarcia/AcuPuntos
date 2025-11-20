using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using AcuPuntos.Services;
using AcuPuntos.ViewModels;
using AcuPuntos.Views;
using Plugin.Firebase;

namespace AcuPuntos;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Registrar servicios
        RegisterServices(builder.Services);
        
        // Registrar ViewModels
        RegisterViewModels(builder.Services);
        
        // Registrar Views
        RegisterViews(builder.Services);

        // Inicializar Firebase
        InitializeFirebase();

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Servicios Singleton (una sola instancia para toda la app)
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IFirestoreService, FirestoreService>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<LoginViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<TransferViewModel>();
        services.AddTransient<RewardsViewModel>();
        services.AddTransient<AdminViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<RewardDetailViewModel>();
        services.AddTransient<UserDetailViewModel>();
    }

    private static void RegisterViews(IServiceCollection services)
    {
        services.AddTransient<LoginPage>();
        services.AddTransient<HomePage>();
        services.AddTransient<TransferPage>();
        services.AddTransient<RewardsPage>();
        services.AddTransient<AdminPage>();
        services.AddTransient<ProfilePage>();
        services.AddTransient<HistoryPage>();
        services.AddTransient<RewardDetailPage>();
        services.AddTransient<UserDetailPage>();
    }

    private static void InitializeFirebase()
    {
#if ANDROID
        // La configuración de Firebase para Android se hace a través del google-services.json
        CrossFirebase.Initialize();
#elif IOS
        // La configuración de Firebase para iOS se hace a través del GoogleService-Info.plist
        CrossFirebase.Initialize();
#endif
    }
}
