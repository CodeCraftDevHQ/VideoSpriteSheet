using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    private readonly VideoPlayer _player;
    private CancellationTokenSource? _cts;
    private VideoInfo? _videoInfo;
    private string? _videoError;
    private string? _lastOutputFolder;
    private bool _syncing;
    private bool _programmaticOutput;
    private bool _outputUserChosen;

    public MainWindow()
    {
        InitializeComponent();

        _player = new VideoPlayer(PlayerImage);
        _player.PositionChanged += OnPlayerPosition;
        _player.PlayingChanged += UpdatePlayButton;

        Timeline.PositionDragged += time => _player.Seek(time);
        Timeline.RangeDragged += OnTimelineRangeDragged;

        DownloadModelCombo.ItemsSource = ModelCatalog.All;
        DownloadModelCombo.SelectedIndex = 0;

        RescanModels();
        UpdatePlayButton();
        StatusText.Text = Loc.Get("Ready");
        Loc.Instance.LanguageChanged += OnLanguageChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        Loc.Instance.LanguageChanged -= OnLanguageChanged;
        _player.Dispose();
        base.OnClosed(e);
    }

    private void OnLanguageChanged()
    {
        RefreshVideoInfo();
        UpdateEstimate();
        UpdatePlayButton();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        new SettingsWindow { Owner = this }.ShowDialog();
        RescanModels();
    }

    private void RescanModels()
    {
        if (!string.IsNullOrWhiteSpace(ModelPathBox.Text) && File.Exists(ModelPathBox.Text))
        {
            return;
        }

        foreach (var entry in ModelCatalog.All)
        {
            if (File.Exists(ModelDownloader.PathFor(entry)))
            {
                UseModel(entry);
                return;
            }
        }
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

        if (path is not null && VideoExtensions.Contains(Path.GetExtension(path)))
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
        BrowseVideo();
    }

    private void BrowseVideo()
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
        _player.Pause();
        VideoPathBox.Text = path;
        StatusText.Text = Loc.Get("Ready");

        _syncing = true;
        StartTimeBox.Text = string.Empty;
        EndTimeBox.Text = string.Empty;
        _syncing = false;

        try
        {
            _videoInfo = _player.Load(path);
            _videoError = null;
        }
        catch (Exception ex)
        {
            _videoInfo = null;
            _videoError = ex.Message;
            _player.Unload();
        }

        var duration = _player.Duration;
        Timeline.Duration = duration;
        Timeline.Start = 0;
        Timeline.End = duration;
        Timeline.Position = 0;
        ApplyRangeToPlayer();

        PlaceholderText.Visibility = _videoInfo is null ? Visibility.Visible : Visibility.Collapsed;
        ShowVideoView(true);

        if (!_outputUserChosen)
        {
            var folder = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(folder))
            {
                _programmaticOutput = true;
                OutputPathBox.Text = folder;
                _programmaticOutput = false;
            }
        }

        RefreshVideoInfo();
        UpdateEstimate();
        UpdateTimeText();
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Loc.Get("PickOutputTitle") };

        if (dialog.ShowDialog(this) == true)
        {
            OutputPathBox.Text = dialog.FolderName;
        }
    }

    private void OutputPath_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_programmaticOutput)
        {
            _outputUserChosen = !string.IsNullOrWhiteSpace(OutputPathBox.Text);
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

    private void VideoTab_Click(object sender, RoutedEventArgs e)
    {
        ShowVideoView(true);
    }

    private void ResultTab_Click(object sender, RoutedEventArgs e)
    {
        ShowVideoView(false);
    }

    private void ShowVideoView(bool video)
    {
        if (!video)
        {
            _player.Pause();
        }

        PlayerBorder.Visibility = video ? Visibility.Visible : Visibility.Collapsed;
        ResultBorder.Visibility = video ? Visibility.Collapsed : Visibility.Visible;
        PlayerControls.Visibility = video ? Visibility.Visible : Visibility.Collapsed;
        VideoTabButton.IsEnabled = !video;
        ResultTabButton.IsEnabled = video;
    }

    private void Player_Click(object sender, MouseButtonEventArgs e)
    {
        if (_player.IsLoaded)
        {
            _player.Toggle();
        }
        else if (_cts is null)
        {
            BrowseVideo();
        }
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        _player.Toggle();
    }

    private void UpdatePlayButton()
    {
        PlayButton.Content = Loc.Get(_player.IsPlaying ? "Pause" : "Play");
    }

    private void OnPlayerPosition(double seconds)
    {
        Timeline.Position = seconds;
        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        TimeText.Text = _videoInfo is null ? string.Empty : $"{Clock(_player.Position)} / {Clock(Timeline.Duration)}";
    }

    private static string Clock(double seconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return span.TotalHours >= 1 ? span.ToString(@"h\:mm\:ss\.f") : span.ToString(@"mm\:ss\.f");
    }

    private void OnTimelineRangeDragged(double handleTime)
    {
        SyncBoxesFromTimeline();
        _player.Seek(handleTime);
    }

    private void SetStart_Click(object sender, RoutedEventArgs e)
    {
        if (Timeline.Duration <= 0)
        {
            return;
        }

        var time = _player.Position;

        if (time < Timeline.End - 0.05)
        {
            Timeline.Start = time;
            SyncBoxesFromTimeline();
        }
    }

    private void SetEnd_Click(object sender, RoutedEventArgs e)
    {
        if (Timeline.Duration <= 0)
        {
            return;
        }

        var time = _player.Position;

        if (time > Timeline.Start + 0.05)
        {
            Timeline.End = time;
            SyncBoxesFromTimeline();
        }
    }

    private void FullVideo_Click(object sender, RoutedEventArgs e)
    {
        Timeline.Start = 0;
        Timeline.End = Timeline.Duration;
        SyncBoxesFromTimeline();
    }

    private void SyncBoxesFromTimeline()
    {
        _syncing = true;
        StartTimeBox.Text = Timeline.Start <= 0.001 ? string.Empty : FormatSeconds(Timeline.Start);
        EndTimeBox.Text = Timeline.End >= Timeline.Duration - 0.001 ? string.Empty : FormatSeconds(Timeline.End);
        _syncing = false;

        ApplyRangeToPlayer();
        UpdateEstimate();
    }

    private void SyncTimelineFromBoxes()
    {
        if (Timeline.Duration <= 0)
        {
            return;
        }

        if (!ImageHelpers.TryParseTime(StartTimeBox.Text, out var start) || !ImageHelpers.TryParseTime(EndTimeBox.Text, out var end))
        {
            return;
        }

        var startSeconds = Math.Clamp(start ?? 0, 0, Timeline.Duration);
        var endSeconds = Math.Clamp(end ?? Timeline.Duration, 0, Timeline.Duration);

        if (endSeconds <= startSeconds)
        {
            return;
        }

        Timeline.Start = startSeconds;
        Timeline.End = endSeconds;
        ApplyRangeToPlayer();
    }

    private void ApplyRangeToPlayer()
    {
        _player.RangeStart = Timeline.Start;
        _player.RangeEnd = Timeline.Duration > 0 ? Timeline.End : double.MaxValue;
    }

    private static string FormatSeconds(double seconds)
    {
        return seconds.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void Estimate_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_syncing)
        {
            SyncTimelineFromBoxes();
        }

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
            SharpText.Text = e.NewValue.ToString("0.0", CultureInfo.InvariantCulture);
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

        var outputFolder = OutputPathBox.Text.Trim();

        if (outputFolder.Length == 0 && File.Exists(VideoPathBox.Text))
        {
            outputFolder = Path.GetDirectoryName(VideoPathBox.Text) ?? string.Empty;
        }

        var options = new ProcessingOptions
        {
            VideoPath = VideoPathBox.Text,
            OutputFolder = outputFolder,
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
        ShowVideoView(false);
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
        SettingsButton.IsEnabled = !busy;
    }

    private static bool TryParse(string text, int min, int max, out int value)
    {
        return int.TryParse(ImageHelpers.NormalizeDigits(text), out value) && value >= min && value <= max;
    }
}
