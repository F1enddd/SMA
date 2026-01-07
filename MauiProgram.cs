using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using SMA.Services;


namespace SMA
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).UseMauiCommunityToolkit(static options =>
            {
                options.SetPopupDefaults(new DefaultPopupSettings
                {
                    CanBeDismissedByTappingOutsideOfPopup = true,
                    BackgroundColor = Colors.Transparent,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Start,
                    Margin = 20,
                    Padding = 4
                });

                options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings
                {
                    CanBeDismissedByTappingOutsideOfPopup = true,
#if ANDROID
                    OnTappingOutsideOfPopup = async () => await Toast.Make("Popup Dismissed").Show(CancellationToken.None),
#endif
                    PageOverlayColor = Colors.Black.MultiplyAlpha((float)0.5),
                    Shadow = null,
                    Shape = null
                });
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<IUserDialogService, MauiDialogService>();

            return builder.Build();
        }
    }
}