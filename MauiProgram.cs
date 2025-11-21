using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using CommunityToolkit.Maui;
using AcuPuntos.Services;
using AcuPuntos.ViewModels;
using AcuPuntos.Views;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Auth.Google;
#if IOS
using Plugin.Firebase.Core.Platforms.iOS;
using Plugin.Firebase.Auth.Google.Platforms.iOS;
#elif ANDROID
using Plugin.Firebase.Core.Platforms.Android;
using Plugin.Firebase.Auth.Google.Platforms.Android;
#endif

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

        // Configurar Firebase con eventos de ciclo de vida
        builder.ConfigureLifecycleEvents(events =>
        {
#if IOS
            events.AddiOS(iOS => iOS.WillFinishLaunching((app, launchOptions) =>
            {
                CrossFirebase.Initialize();
                FirebaseAuthGoogleImplementation.Initialize();
                return false;
            }));
#elif ANDROID
            events.AddAndroid(android => android.OnCreate((activity, state) =>
            {
                CrossFirebase.Initialize(activity);
                FirebaseAuthGoogleImplementation.Initialize("289604700066-ai6amis5kgfcnk9gu81huf9gqj26j9kd.apps.googleusercontent.com");
            }));
#endif
        });

        // Registrar servicios
        RegisterServices(builder.Services);

        // Registrar ViewModels
        RegisterViewModels(builder.Services);

        // Registrar Views
        RegisterViews(builder.Services);

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Servicios Singleton (una sola instancia para toda la app)
        services.AddSingleton(_ => CrossFirebaseAuth.Current);
        services.AddSingleton(_ => CrossFirebaseAuthGoogle.Current);
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IFirestoreService, FirestoreService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<UserStateService>();
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
}
