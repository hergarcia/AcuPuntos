namespace AcuPuntos.Services
{
    public class ThemeService : IThemeService
    {
        private const string ThemePreferenceKey = "app_theme";
        private AppTheme _currentTheme;

        public AppTheme CurrentTheme => _currentTheme;

        public event EventHandler<AppTheme>? ThemeChanged;

        public ThemeService()
        {
            _currentTheme = AppTheme.Unspecified;
        }

        public void SetTheme(AppTheme theme)
        {
            if (_currentTheme == theme)
                return;

            _currentTheme = theme;

            // Apply theme to the application
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = theme;
            }

            // Save preference
            Preferences.Set(ThemePreferenceKey, (int)theme);

            // Notify listeners
            ThemeChanged?.Invoke(this, theme);
        }

        public void LoadSavedTheme()
        {
            // Load saved theme preference (default to Unspecified/System)
            var savedTheme = (AppTheme)Preferences.Get(ThemePreferenceKey, (int)AppTheme.Unspecified);
            
            _currentTheme = savedTheme;

            // Apply the saved theme
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = savedTheme;
            }
        }

        public void ApplyTheme()
        {
            // No longer needed - StatusBarBehavior handles this automatically
        }

        public void Initialize()
        {
            // No longer needed - StatusBarBehavior handles this automatically
        }
    }
}
