using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VideoSpriteSheet.Models;
using VideoSpriteSheet.Services;

namespace VideoSpriteSheet;

public partial class MainWindow : Window
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".m4v", ".flv"
    };

    private CancellationTokenSource? _cts;
    private VideoInfo? _videoInfo;
    private string? _videoError;
    private string? _lastOutputFolder;

    public MainWindow()
    {
        InitializeComponent();

        DownloadModelCombo.ItemsSource = ModelCatalog.All;
        DownloadModelCombo.SelectedIndex = 0;

        OutputPathBox.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "VideoSpriteSheet");

        foreach (var entry in ModelCatalog.All)
        {
            if (File.Exists(ModelDownloader.PathFor(entry)))
            {
                UseModel(entry);
                break;
            }
        }

        StatusText.Text = Loc.Get("Ready");
        Loc.Instance.LanguageChanged += OnLanguageChanged;
        Closed += (_, _) => Loc.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        RefreshVideoInfo();
        UpdateEstimate();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        new SettingsWindow { Owner = this }.ShowDialog();
    }

    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = _cts is null && GetDroppedFile(e) is not null ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_PreviewDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (_cts is not null)
        {
            return;
        }

        var path = GetDroppedFile(e);

        if (path is null)
        {
            StatusText.Text = Loc.Get("ErrUnsupportedDrop");
            return;
        }

        if (VideoExtensions.Contains(Path.GetExtension(path)))
        {
            SetVideo(path);
        }
        else
        {
            StatusText.Text = Loc.Get("ErrUnsupportedDrop");
        }
    }

    private static string? GetDroppedFile(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return null;
        }

        return (e.Data.GetData(DataFormats.FileDrop) as string[])?.FirstOrDefault();
    }

    private void BrowseVideo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.Get("BrowseVideo"),
            Filter = Loc.Get("VideoFilter")
        };

        if (dialog.ShowDialog(this) == true)
        {
            SetVideo(dialog.FileName);
        }
    }

    private void SetVideo(string path)
    {
        VideoPathBox.Text = path;
        StartTimeBox.Text = string.Empty;
        EndTimeBox.Text = string.Empty;
        StatusText.Text = Loc.Get("Ready");
        LoadVideoInfo(path);
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Loc.Get("PickOutputTitle") };

        if (dialog.ShowDialog(this) == true)
        {
            OutputPathBox.Text = dialog.FolderName;
        }
    }

    private void BrowseModel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.Get("PickModelTitle"),
            Filter = Loc.Get("ModelFilter")
        };

        if (dialog.ShowDialog(this) == true)
        {
            ModelPathBox.Text = dialog.FileName;
        }
    }

    private void UseModel(ModelEntry entry)
    {
        ModelPathBox.Text = ModelDownloader.PathFor(entry);
        PresetCombo.SelectedIndex = entry.Preset == NormalizationPreset.ImageNet ? 1 : 0;
    }

    private async void DownloadModel_Click(object sender, RoutedEventArgs e)
    {
        if (DownloadModelCombo.SelectedItem is not ModelEntry entry)
        {
            return;
        }

        var target = ModelDownloader.PathFor(entry);

        if (File.Exists(target))
        {
            UseModel(entry);
            StatusText.Text = Loc.Get("ModelExists");
            return;
        }

        DownloadModelButton.IsEnabled = false;
        DownloadProgress.Visibility = Visibility.Visible;
        DownloadProgress.Value = 0;
        StatusText.Text = Loc.Get("Downloading");

        try
        {
            var progress = new Progress<double>(value => DownloadProgress.Value = value);
            await ModelDownloader.DownloadAsync(entry.Url, target, progress, CancellationToken.None);
            UseModel(entry);
            StatusText.Text = Loc.Get("DownloadDone");
        }
        catch (Exception ex)
        {
            StatusText.Text = Loc.Format("DownloadFail", ex.Message);
        }
        finally
        {
            DownloadModelButton.IsEnabled = true;
            DownloadProgress.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadVideoInfo(string path)
    {
        try
        {
            using var video = VideoSource.Open(path);
            _videoInfo = video.Info;
            _videoError = null;
        }
        catch (Exception ex)
        {
            _videoInfo = null;
            _videoError = ex.Message;
        }

        RefreshVideoInfo();
        UpdateEstimate();
    }

    private void RefreshVideoInfo()
    {
        if (_videoInfo is null)
        {
            VideoInfoText.Text = _videoError ?? string.Empty;
            return;
        }

        var duration = _videoInfo.FrameCount > 0
            ? TimeSpan.FromSeconds(_videoInfo.FrameCount / _videoInfo.Fps).ToString(@"hh\:mm\:ss")
            : "?";

        VideoInfoText.Text = Loc.Format("VideoInfo", _videoInfo.Width, _videoInfo.Height, _videoInfo.Fps, _videoInfo.FrameCount, duration);
    }

    private void Estimate_Changed(object sender, TextChangedEventArgs e)
    {
        UpdateEstimate();
    }

    private void UpdateEstimate()
    {
        if (EstimateText is null || StepBox is null || StartTimeBox is null || EndTimeBox is null)
        {
            return;
        }

        EstimateText.Text = string.Empty;

        if (_videoInfo is null || _videoInfo.FrameCount <= 0 || !TryParse(StepBox.Text, 1, 100000, out var step))
        {
            return;
        }

        if (!ImageHelpers.TryParseTime(StartTimeBox.Text, out var start) || !ImageHelpers.TryParseTime(EndTimeBox.Text, out var end))
        {
            return;
        }

        var fps = _videoInfo.Fps;
        var startFrame = (int)Math.Round((start ?? 0) * fps);
        var endFrame = end is null ? _videoInfo.FrameCount : Math.Min(_videoInfo.FrameCount, (int)Math.Round(end.Value * fps));
        var count = Math.Max(0, endFrame - startFrame);

        EstimateText.Text = Loc.Format("Estimate", (count + step - 1) / step);
    }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ScaleText is not null)
        {
            ScaleText.Text = $"{(int)e.NewValue}%";
        }
    }

    private void SharpSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SharpText is not null)
        {
            SharpText.Text = e.NewValue.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private void RemoveBg_Changed(object sender, RoutedEventArgs e)
    {
        if (BgPanel is not null)
        {
            BgPanel.IsEnabled = RemoveBgCheck.IsChecked == true;
        }
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParse(StepBox.Text, 1, 100000, out var step))
        {
            StatusText.Text = Loc.Get("ErrStep");
            return;
        }

        if (!TryParse(ColumnsBox.Text, 1, 1000, out var columns))
        {
            StatusText.Text = Loc.Get("ErrColumns");
            return;
        }

        if (!TryParse(PaddingBox.Text, 0, 1000, out var padding))
        {
            StatusText.Text = Loc.Get("ErrPadding");
            return;
        }

        if (!ImageHelpers.TryParseTime(StartTimeBox.Text, out var start) || !ImageHelpers.TryParseTime(EndTimeBox.Text, out var end))
        {
            StatusText.Text = Loc.Get("ErrTime");
            return;
        }

        if (end is not null && end.Value <= (start ?? 0))
        {
            StatusText.Text = Loc.Get("ErrRange");
            return;
        }

        var options = new ProcessingOptions
        {
            VideoPath = VideoPathBox.Text,
            OutputFolder = OutputPathBox.Text.Trim(),
            FrameStep = step,
            StartSeconds = start ?? 0,
            EndSeconds = end,
            ScalePercent = (int)ScaleSlider.Value,
            RemoveBackground = RemoveBgCheck.IsChecked == true,
            ModelPath = ModelPathBox.Text.Trim(),
            Normalization = PresetCombo.SelectedIndex == 1 ? NormalizationPreset.ImageNet : NormalizationPreset.Rmbg14,
            UseGpu = GpuCheck.IsChecked == true,
            EdgeSharpness = SharpSlider.Value,
            SaveIndividualFrames = SaveFramesCheck.IsChecked == true,
            SaveSpriteSheet = SaveSheetCheck.IsChecked == true,
            Columns = columns,
            Padding = padding
        };

        SetBusy(true);
        MainProgress.Value = 0;
        PreviewImage.Source = null;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        var progress = new Progress<ProgressInfo>(info =>
        {
            StatusText.Text = info.Message;
            MainProgress.Value = info.Total > 0 ? Math.Min(1.0, (double)info.Processed / info.Total) : 0;

            if (info.Preview is not null)
            {
                PreviewImage.Source = info.Preview;
            }
        });

        try
        {
            var pipeline = new SpriteSheetPipeline();
            var result = await Task.Run(() => pipeline.Run(options, progress, token), token);

            _lastOutputFolder = options.OutputFolder;
            MainProgress.Value = 1;
            OpenFolderButton.IsEnabled = true;

            var parts = new List<string> { Loc.Format("ResultFrames", result.FrameCount) };

            if (result.SheetPath is not null)
            {
                parts.Add(Loc.Format("ResultSheet", result.SheetPath));
            }

            if (result.FramesFolder is not null)
            {
                parts.Add(Loc.Format("ResultFolder", result.FramesFolder));
            }

            if (options.RemoveBackground)
            {
                parts.Add(Loc.Format("ResultProvider", result.Provider));
            }

            StatusText.Text = string.Join(Environment.NewLine, parts);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = Loc.Get("Cancelled");
        }
        catch (Exception ex)
        {
            StatusText.Text = Loc.Format("ErrorPrefix", ex.Message);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            SetBusy(false);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        CancelButton.IsEnabled = false;
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_lastOutputFolder) && Directory.Exists(_lastOutputFolder))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_lastOutputFolder}\"") { UseShellExecute = true });
        }
    }

    private void SetBusy(bool busy)
    {
        StartButton.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
        BrowseVideoButton.IsEnabled = !busy;
        BrowseOutputButton.IsEnabled = !busy;
        BrowseModelButton.IsEnabled = !busy;
        DownloadModelButton.IsEnabled = !busy;
    }

    private static bool TryParse(string text, int min, int max, out int value)
    {
        return int.TryParse(ImageHelpers.NormalizeDigits(text), out value) && value >= min && value <= max;
    }
}
