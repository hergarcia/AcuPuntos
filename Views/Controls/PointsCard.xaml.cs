namespace AcuPuntos.Views.Controls;

public partial class PointsCard : Border
{
    public static readonly BindableProperty PointsProperty =
        BindableProperty.Create(nameof(Points), typeof(int), typeof(PointsCard), 0, propertyChanged: OnPointsChanged);

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(PointsCard), "Tus puntos");

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(PointsCard), "💰");

    public static readonly BindableProperty FormattedPointsProperty =
        BindableProperty.Create(nameof(FormattedPoints), typeof(string), typeof(PointsCard), "0 puntos");

    public int Points
    {
        get => (int)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string FormattedPoints
    {
        get => (string)GetValue(FormattedPointsProperty);
        private set => SetValue(FormattedPointsProperty, value);
    }

    private static void OnPointsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PointsCard card && newValue is int points)
        {
            card.FormattedPoints = $"{points:N0} puntos";
        }
    }

    public PointsCard()
    {
        InitializeComponent();
    }
}
