
using Android.Content.Res;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace AcuPuntos.Handlers;

public static class CleanPicker
{
    public static void Initialize()
    {
        PickerHandler.Mapper.AppendToMapping("customPH", (h, v) =>
        {
#if IOS
            h.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif ANDROID
            h.PlatformView.BackgroundTintList = ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
#endif
        });
    }
}