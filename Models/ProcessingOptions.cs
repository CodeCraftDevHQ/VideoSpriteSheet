using System.Windows.Media.Imaging;

namespace VideoSpriteSheet.Models;

public enum NormalizationPreset
{
    Rmbg14 = 0,
    ImageNet = 1
}

public sealed class ProcessingOptions
{
    public string VideoPath { get; set; } = string.Empty;
    public string OutputFolder { get; set; } = string.Empty;
    public int FrameStep { get; set; } = 1;
    public int ScalePercent { get; set; } = 100;
    public bool RemoveBackground { get; set; } = true;
    public string ModelPath { get; set; } = string.Empty;
    public NormalizationPreset Normalization { get; set; } = NormalizationPreset.Rmbg14;
    public bool UseGpu { get; set; } = true;
    public double EdgeSharpness { get; set; } = 2.0;
    public bool SaveIndividualFrames { get; set; }
    public bool SaveSpriteSheet { get; set; } = true;
    public int Columns { get; set; } = 8;
    public int Padding { get; set; }
}

public sealed record ProgressInfo(int Processed, int Total, string Message, BitmapSource? Preview = null);

public sealed record ProcessingResult(int FrameCount, string? SheetPath, string? FramesFolder, string Provider);

public sealed record VideoInfo(int Width, int Height, double Fps, int FrameCount);
