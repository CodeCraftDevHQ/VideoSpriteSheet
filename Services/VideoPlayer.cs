using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenCvSharp;
using VideoSpriteSheet.Models;

namespace VideoSpriteSheet.Services;

public sealed class VideoPlayer : IDisposable
{
    private const int MaxDisplayWidth = 1280;

    private readonly Image _target;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _clock = new();
    private readonly Mat _frame = new();
    private readonly Mat _scaled = new();

    private VideoSource? _source;
    private WriteableBitmap? _bitmap;
    private int _currentFrame;
    private int _playOrigin;

    public VideoPlayer(Image target)
    {
        _target = target;
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };
        _timer.Tick += OnTick;
    }

    public event Action<double>? PositionChanged;

    public event Action? PlayingChanged;

    public VideoInfo? Info { get; private set; }

    public bool IsLoaded => _source is not null;

    public bool IsPlaying => _timer.IsEnabled;

    public double RangeStart { get; set; }

    public double RangeEnd { get; set; } = double.MaxValue;

    public double Duration => Info is { FrameCount: > 0 } info ? info.FrameCount / info.Fps : 0;

    public double Position => Info is null ? 0 : _currentFrame / Info.Fps;

    public VideoInfo Load(string path)
    {
        Unload();

        _source = VideoSource.Open(path);
        Info = _source.Info;
        RangeStart = 0;
        RangeEnd = double.MaxValue;
        ShowFrame(0);
        PositionChanged?.Invoke(Position);

        return Info;
    }

    public void Unload()
    {
        Pause();
        _source?.Dispose();
        _source = null;
        Info = null;
        _bitmap = null;
        _currentFrame = 0;
        _target.Source = null;
    }

    public void Seek(double seconds)
    {
        if (_source is null || Info is null)
        {
            return;
        }

        ShowFrame(FrameAt(seconds));

        if (IsPlaying)
        {
            _playOrigin = _currentFrame;
            _clock.Restart();
        }

        PositionChanged?.Invoke(Position);
    }

    public void Play()
    {
        if (_source is null || Info is null || IsPlaying)
        {
            return;
        }

        var end = EndSeconds();

        if (Position < RangeStart - 0.001 || Position >= end - 0.05)
        {
            ShowFrame(FrameAt(RangeStart));
            PositionChanged?.Invoke(Position);
        }

        _playOrigin = _currentFrame;
        _clock.Restart();
        _timer.Start();
        PlayingChanged?.Invoke();
    }

    public void Pause()
    {
        if (!_timer.IsEnabled)
        {
            return;
        }

        _timer.Stop();
        _clock.Stop();
        PlayingChanged?.Invoke();
    }

    public void Toggle()
    {
        if (IsPlaying)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }

    public void Dispose()
    {
        Unload();
        _frame.Dispose();
        _scaled.Dispose();
    }

    private double EndSeconds()
    {
        var duration = Duration;
        return duration > 0 ? Math.Min(RangeEnd, duration) : RangeEnd;
    }

    private int FrameAt(double seconds)
    {
        return Info is null ? 0 : (int)Math.Round(Math.Max(0, seconds) * Info.Fps);
    }

    private void ShowFrame(int frame)
    {
        if (_source is null)
        {
            return;
        }

        _source.Seek(frame);
        var index = _source.NextIndex;

        if (_source.ReadInto(_frame))
        {
            _currentFrame = index;
            Render();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_source is null || Info is null)
        {
            Pause();
            return;
        }

        var target = _playOrigin + (int)(_clock.Elapsed.TotalSeconds * Info.Fps);
        var endSeconds = EndSeconds();
        var endFrame = endSeconds >= double.MaxValue / 2 ? int.MaxValue : FrameAt(endSeconds);

        if (target >= endFrame)
        {
            Pause();
            return;
        }

        if (target <= _currentFrame)
        {
            return;
        }

        while (_source.NextIndex < target)
        {
            if (!_source.Skip())
            {
                Pause();
                return;
            }
        }

        var index = _source.NextIndex;

        if (!_source.ReadInto(_frame))
        {
            Pause();
            return;
        }

        _currentFrame = index;
        Render();
        PositionChanged?.Invoke(Position);
    }

    private void Render()
    {
        var source = _frame;

        if (_frame.Width > MaxDisplayWidth)
        {
            var ratio = (double)MaxDisplayWidth / _frame.Width;
            Cv2.Resize(
                _frame,
                _scaled,
                new OpenCvSharp.Size(MaxDisplayWidth, Math.Max(1, (int)(_frame.Height * ratio))),
                0,
                0,
                InterpolationFlags.Area);
            source = _scaled;
        }

        var width = source.Width;
        var height = source.Height;

        if (_bitmap is null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
        {
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
            _target.Source = _bitmap;
        }

        var stride = (int)source.Step();
        _bitmap.WritePixels(new Int32Rect(0, 0, width, height), source.Data, stride * height, stride);
    }
}
