using CommunityToolkit.Maui.Extensions;

namespace AcuPuntos.Views.Controls;

public partial class SkeletonBox : BoxView
{
    private bool _isAnimating;

    public SkeletonBox()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsVisibleProperty.PropertyName || propertyName == WindowProperty.PropertyName)
        {
            UpdateAnimationState();
        }
    }

    private void UpdateAnimationState()
    {
        if (IsVisible && Window != null)
        {
            // Ensure we start on the UI thread and give a moment for layout
            Dispatcher.Dispatch(StartAnimation);
        }
        else
        {
            StopAnimation();
        }
    }

    private async void StartAnimation()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        // Small delay to ensure control is ready
        await Task.Delay(50);

        // Double check after delay
        if (!_isAnimating || !IsVisible || Window == null)
        {
            _isAnimating = false;
            return;
        }

        // Reset opacity
        this.Opacity = 0.7;

        while (_isAnimating)
        {
            // Check again before starting a new cycle
            if (!IsVisible || Window == null) 
            {
                StopAnimation();
                break;
            }

            await this.FadeToAsync(0.3, 800, Easing.SinInOut);
            if (!_isAnimating) break;
            await this.FadeToAsync(0.7, 800, Easing.SinInOut);
        }
    }

    private void StopAnimation()
    {
        _isAnimating = false;
        this.CancelAnimations();
        this.Opacity = 0.7;
    }
}
