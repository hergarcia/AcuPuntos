
using Android.Content.Res;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace AcuPuntos.Handlers;

public static class CleanEntry
{
    public static void Initialize()
    {
        EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, v) =>
        {
#if IOS
            h.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif ANDROID
            h.PlatformView.BackgroundTintList = ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
#endif
        });
    }
}