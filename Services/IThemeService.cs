namespace AcuPuntos.Services
{
    public interface IThemeService
    {
        AppTheme CurrentTheme { get; }
        void SetTheme(AppTheme theme);
        void LoadSavedTheme();
        void Initialize();
        void ApplyTheme();
    }
}
