using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace VideoSpriteSheet.Controls;

public sealed class RangeTimeline : FrameworkElement
{
    private const double SidePadding = 10;
    private const double HandleHitDistance = 9;
    private const double MinimumGap = 0.05;

    private static readonly Brush TrackBrush = Freeze(new SolidColorBrush(Color.FromArgb(90, 128, 128, 128)));
    private static readonly Brush RangeBrush = Freeze(new SolidColorBrush(Color.FromArgb(170, 59, 130, 246)));
    private static readonly Brush HandleBrush = Freeze(new SolidColorBrush(Color.FromRgb(59, 130, 246)));
    private static readonly Pen HandlePen = FreezePen(new Pen(Brushes.White, 1));
    private static readonly Pen PlayheadPen = FreezePen(new Pen(new SolidColorBrush(Color.FromRgb(229, 57, 53)), 2));

    private double _duration;
    private double _start;
    private double _end;
    private double _position;
    private DragTarget _drag;

    private enum DragTarget
    {
        None,
        Start,
        End,
        Position
    }

    public event Action<double>? PositionDragged;

    public event Action<double>? RangeDragged;

    public double Duration
    {
        get => _duration;
        set
        {
            _duration = Math.Max(0, value);
            InvalidateVisual();
        }
    }

    public double Start
    {
        get => _start;
        set
        {
            _start = Math.Clamp(value, 0, _duration);
            InvalidateVisual();
        }
    }

    public double End
    {
        get => _end;
        set
        {
            _end = Math.Clamp(value, 0, _duration);
            InvalidateVisual();
        }
    }

    public double Position
    {
        get => _position;
        set
        {
            _position = Math.Clamp(value, 0, _duration);
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(double.IsInfinity(availableSize.Width) ? 300 : availableSize.Width, 34);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        var height = ActualHeight;
        var centerY = height / 2;

        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));
        dc.DrawRoundedRectangle(TrackBrush, null, new Rect(SidePadding, centerY - 3, Math.Max(0, width - 2 * SidePadding), 6), 3, 3);

        if (_duration <= 0)
        {
            return;
        }

        var startX = ToX(_start);
        var endX = ToX(_end);

        dc.DrawRectangle(RangeBrush, null, new Rect(startX, centerY - 5, Math.Max(1, endX - startX), 10));
        dc.DrawRoundedRectangle(HandleBrush, HandlePen, new Rect(startX - 4, centerY - 12, 8, 24), 3, 3);
        dc.DrawRoundedRectangle(HandleBrush, HandlePen, new Rect(endX - 4, centerY - 12, 8, 24), 3, 3);

        var playX = ToX(_position);
        dc.DrawLine(PlayheadPen, new Point(playX, 2), new Point(playX, height - 2));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (_duration <= 0)
        {
            return;
        }

        var x = e.GetPosition(this).X;
        var startX = ToX(_start);
        var endX = ToX(_end);
        var startDistance = Math.Abs(x - startX);
        var endDistance = Math.Abs(x - endX);

        if (Math.Min(startDistance, endDistance) <= HandleHitDistance)
        {
            if (startDistance < endDistance)
            {
                _drag = DragTarget.Start;
            }
            else if (endDistance < startDistance)
            {
                _drag = DragTarget.End;
            }
            else
            {
                _drag = x < startX ? DragTarget.Start : DragTarget.End;
            }
        }
        else
        {
            _drag = DragTarget.Position;
        }

        CaptureMouse();
        Apply(x);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_drag != DragTarget.None && IsMouseCaptured)
        {
            Apply(e.GetPosition(this).X);
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_drag != DragTarget.None)
        {
            _drag = DragTarget.None;
            ReleaseMouseCapture();
        }
    }

    private void Apply(double x)
    {
        var time = Math.Clamp(FromX(x), 0, _duration);

        switch (_drag)
        {
            case DragTarget.Start:
                _start = Math.Clamp(Math.Min(time, _end - MinimumGap), 0, _duration);
                InvalidateVisual();
                RangeDragged?.Invoke(_start);
                break;

            case DragTarget.End:
                _end = Math.Clamp(Math.Max(time, _start + MinimumGap), 0, _duration);
                InvalidateVisual();
                RangeDragged?.Invoke(_end);
                break;

            case DragTarget.Position:
                _position = time;
                InvalidateVisual();
                PositionDragged?.Invoke(time);
                break;
        }
    }

    private double ToX(double time)
    {
        var usable = Math.Max(1, ActualWidth - 2 * SidePadding);
        return SidePadding + (_duration <= 0 ? 0 : time / _duration * usable);
    }

    private double FromX(double x)
    {
        var usable = Math.Max(1, ActualWidth - 2 * SidePadding);
        return (x - SidePadding) / usable * _duration;
    }

    private static Brush Freeze(SolidColorBrush brush)
    {
        brush.Freeze();
        return brush;
    }

    private static Pen FreezePen(Pen pen)
    {
        pen.Freeze();
        return pen;
    }
}
