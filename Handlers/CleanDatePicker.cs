
using Android.Content.Res;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace AcuPuntos.Handlers;

public static class CleanDatePicker
{
    public static void Initialize()
    {
        DatePickerHandler.Mapper.AppendToMapping("customDP", (h, v) =>
        {
#if IOS
            h.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif ANDROID
            h.PlatformView.BackgroundTintList = ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
#endif
        });
    }
}