using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VideoSpriteSheet.Models;
using VideoSpriteSheet.Services;

namespace VideoSpriteSheet;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _cts;
    private VideoInfo? _videoInfo;
    private string? _lastOutputFolder;

    public MainWindow()
    {
        InitializeComponent();

        OutputPathBox.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "VideoSpriteSheet");

        if (File.Exists(ModelDownloader.DefaultModelPath))
        {
            ModelPathBox.Text = ModelDownloader.DefaultModelPath;
        }
    }

    private void BrowseVideo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "انتخاب ویدیو",
            Filter = "ویدیو|*.mp4;*.mov;*.avi;*.mkv;*.webm;*.wmv;*.m4v;*.flv|همه فایل‌ها|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        VideoPathBox.Text = dialog.FileName;
        LoadVideoInfo(dialog.FileName);
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "انتخاب پوشه خروجی" };

        if (dialog.ShowDialog(this) == true)
        {
            OutputPathBox.Text = dialog.FolderName;
        }
    }

    private void BrowseModel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "انتخاب مدل ONNX",
            Filter = "مدل ONNX|*.onnx"
        };

        if (dialog.ShowDialog(this) == true)
        {
            ModelPathBox.Text = dialog.FileName;
        }
    }

    private async void DownloadModel_Click(object sender, RoutedEventArgs e)
    {
        var target = ModelDownloader.DefaultModelPath;

        if (File.Exists(target))
        {
            ModelPathBox.Text = target;
            StatusText.Text = "مدل از قبل دانلود شده است.";
            return;
        }

        DownloadModelButton.IsEnabled = false;
        DownloadProgress.Visibility = Visibility.Visible;
        DownloadProgress.Value = 0;
        StatusText.Text = "در حال دانلود مدل...";

        try
        {
            var progress = new Progress<double>(value => DownloadProgress.Value = value);
            await ModelDownloader.DownloadAsync(ModelDownloader.Rmbg14Url, target, progress, CancellationToken.None);
            ModelPathBox.Text = target;
            StatusText.Text = "دانلود مدل کامل شد.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "دانلود مدل ناموفق بود: " + ex.Message;
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
            VideoInfoText.Text = $"{_videoInfo.Width}×{_videoInfo.Height} — {_videoInfo.Fps:0.##} فریم بر ثانیه — {_videoInfo.FrameCount} فریم";
        }
        catch (Exception ex)
        {
            _videoInfo = null;
            VideoInfoText.Text = ex.Message;
        }

        UpdateEstimate();
    }

    private void Estimate_Changed(object sender, TextChangedEventArgs e)
    {
        UpdateEstimate();
    }

    private void UpdateEstimate()
    {
        if (EstimateText is null)
        {
            return;
        }

        if (_videoInfo is null || _videoInfo.FrameCount <= 0 || !TryParse(StepBox.Text, 1, 100000, out var step))
        {
            EstimateText.Text = string.Empty;
            return;
        }

        EstimateText.Text = $"≈ {(_videoInfo.FrameCount + step - 1) / step} عکس";
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
            SharpText.Text = e.NewValue.ToString("0.0");
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
            StatusText.Text = "مقدار «هر چند فریم» باید عدد صحیح مثبت باشد.";
            return;
        }

        if (!TryParse(ColumnsBox.Text, 1, 1000, out var columns))
        {
            StatusText.Text = "تعداد ستون باید عدد صحیح مثبت باشد.";
            return;
        }

        if (!TryParse(PaddingBox.Text, 0, 1000, out var padding))
        {
            StatusText.Text = "مقدار فاصله باید عدد صحیح صفر یا بیشتر باشد.";
            return;
        }

        var options = new ProcessingOptions
        {
            VideoPath = VideoPathBox.Text,
            OutputFolder = OutputPathBox.Text.Trim(),
            FrameStep = step,
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

            var parts = new List<string> { $"{result.FrameCount} فریم پردازش شد" };

            if (result.SheetPath is not null)
            {
                parts.Add("اسپرایت‌شیت: " + result.SheetPath);
            }

            if (result.FramesFolder is not null)
            {
                parts.Add("عکس‌ها: " + result.FramesFolder);
            }

            if (options.RemoveBackground)
            {
                parts.Add("پردازنده: " + result.Provider);
            }

            StatusText.Text = string.Join(Environment.NewLine, parts);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "عملیات لغو شد.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "خطا: " + ex.Message;
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
