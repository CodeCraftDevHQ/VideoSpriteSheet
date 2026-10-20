using System.Windows;
using VideoSpriteSheet.Services;

namespace VideoSpriteSheet;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppSettings.Load();
        Loc.Instance.Language = AppSettings.Current.Language;
        ThemeManager.Apply(AppSettings.Current.Theme);
        base.OnStartup(e);
    }
}
