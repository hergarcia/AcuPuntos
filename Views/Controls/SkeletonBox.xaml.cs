namespace AcuPuntos.Views.Controls;

public partial class SkeletonBox : BoxView
{
    public SkeletonBox()
    {
        InitializeComponent();
        StartAnimation();
    }

    private async void StartAnimation()
    {
        while (true)
        {
            await this.FadeTo(0.3, 800, Easing.SinInOut);
            await this.FadeTo(0.7, 800, Easing.SinInOut);
        }
    }
}
