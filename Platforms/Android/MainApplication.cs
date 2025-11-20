using Android.App;
using Android.Runtime;
using Plugin.Firebase.Core.Platforms.Android;

namespace AcuPuntos;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        // Inicializar Firebase ANTES de crear la app MAUI
        // Esto asegura que Firebase esté listo cuando se cree el contenedor DI
        CrossFirebase.Initialize(this);

        base.OnCreate();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}