using OpenCvSharp;
using VideoSpriteSheet.Models;

namespace VideoSpriteSheet.Services;

public sealed class VideoSource : IDisposable
{
    private readonly VideoCapture _capture;
    private readonly string? _tempCopy;

    public int Width { get; }
    public int Height { get; }
    public double Fps { get; }
    public int FrameCount { get; }

    private VideoSource(VideoCapture capture, string? tempCopy)
    {
        _capture = capture;
        _tempCopy = tempCopy;
        Width = capture.FrameWidth;
        Height = capture.FrameHeight;
        Fps = capture.Fps > 0 ? capture.Fps : 30.0;
        FrameCount = Math.Max(0, capture.FrameCount);
    }

    public VideoInfo Info => new(Width, Height, Fps, FrameCount);

    public static VideoSource Open(string path)
    {
        var capture = new VideoCapture(path, VideoCaptureAPIs.FFMPEG);
        string? tempCopy = null;

        if (!capture.IsOpened())
        {
            capture.Dispose();
            tempCopy = CopyToAsciiLocation(path);
            capture = new VideoCapture(tempCopy, VideoCaptureAPIs.FFMPEG);

            if (!capture.IsOpened())
            {
                capture.Dispose();
                TryDelete(tempCopy);
                throw new InvalidOperationException("فایل ویدیو باز نشد. فرمت یا کدک ویدیو پشتیبانی نمی‌شود.");
            }
        }

        var source = new VideoSource(capture, tempCopy);

        if (source.Width <= 0 || source.Height <= 0)
        {
            source.Dispose();
            throw new InvalidOperationException("ابعاد ویدیو قابل تشخیص نیست.");
        }

        return source;
    }

    public IEnumerable<(int Index, Mat Frame)> ReadFrames(int step, CancellationToken cancellationToken)
    {
        var index = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_capture.Grab())
            {
                yield break;
            }

            if (index % step == 0)
            {
                var frame = new Mat();

                if (!_capture.Retrieve(frame) || frame.Empty())
                {
                    frame.Dispose();
                    yield break;
                }

                yield return (index, frame);
            }

            index++;
        }
    }

    public void Dispose()
    {
        _capture.Dispose();

        if (_tempCopy is not null)
        {
            TryDelete(_tempCopy);
        }
    }

    private static string CopyToAsciiLocation(string path)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "VideoSpriteSheet",
            "tmp");
        Directory.CreateDirectory(folder);
        var target = Path.Combine(folder, Guid.NewGuid().ToString("N") + Path.GetExtension(path));
        File.Copy(path, target, true);
        return target;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }
}
