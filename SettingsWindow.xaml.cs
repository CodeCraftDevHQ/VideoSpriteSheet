using System.Windows;
using System.Windows.Controls;
using VideoSpriteSheet.Services;

namespace VideoSpriteSheet;

public partial class SettingsWindow : Window
{
    private readonly bool _ready;

    public SettingsWindow()
    {
        InitializeComponent();

        LanguageCombo.SelectedIndex = AppSettings.Current.Language == "en" ? 1 : 0;
        ThemeCombo.SelectedIndex = AppSettings.Current.Theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

        _ready = true;
    }

    private void Language_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        var language = LanguageCombo.SelectedIndex == 1 ? "en" : "fa";
        AppSettings.Current.Language = language;
        Loc.Instance.Language = language;
        AppSettings.Current.Save();
    }

    private void Theme_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        var theme = ThemeCombo.SelectedIndex switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "System"
        };

        AppSettings.Current.Theme = theme;
        ThemeManager.Apply(theme);
        AppSettings.Current.Save();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
