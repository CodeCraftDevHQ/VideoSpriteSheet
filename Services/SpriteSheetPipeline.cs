using System.Diagnostics;
using OpenCvSharp;
using VideoSpriteSheet.Models;

namespace VideoSpriteSheet.Services;

public sealed class SpriteSheetPipeline
{
    private const long MaxSheetBytes = 1_800_000_000L;

    public ProcessingResult Run(ProcessingOptions options, IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
    {
        Validate(options);

        var step = Math.Max(1, options.FrameStep);
        var baseName = Path.GetFileNameWithoutExtension(options.VideoPath);

        Directory.CreateDirectory(options.OutputFolder);

        string? framesFolder = null;

        if (options.SaveIndividualFrames)
        {
            framesFolder = Path.Combine(options.OutputFolder, baseName + "_frames");
            Directory.CreateDirectory(framesFolder);
        }

        using var video = VideoSource.Open(options.VideoPath);

        var startFrame = (int)Math.Round(Math.Max(0, options.StartSeconds) * video.Fps);
        var endFrame = options.EndSeconds is { } endSeconds
            ? (int)Math.Round(endSeconds * video.Fps)
            : int.MaxValue;

        if (video.FrameCount > 0)
        {
            if (startFrame >= video.FrameCount)
            {
                throw new InvalidOperationException(Loc.Get("ErrStartBeyond"));
            }

            endFrame = Math.Min(endFrame, video.FrameCount);
        }

        if (endFrame <= startFrame)
        {
            throw new InvalidOperationException(Loc.Get("ErrRange"));
        }

        var total = endFrame == int.MaxValue ? 0 : (endFrame - startFrame + step - 1) / step;

        BackgroundRemover? remover = null;
        var provider = "-";

        if (options.RemoveBackground)
        {
            progress.Report(new ProgressInfo(0, total, Loc.Get("LoadingModel")));
            remover = new BackgroundRemover(options.ModelPath, options.UseGpu, options.Normalization, options.EdgeSharpness);
            provider = remover.Provider;
        }

        var encodedFrames = new List<byte[]>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            foreach (var (index, rawFrame) in video.ReadFrames(step, startFrame, endFrame, cancellationToken))
            {
                using var frame = rawFrame;
                using var scaled = Scale(frame, options.ScalePercent);
                using var result = remover is null ? scaled.Clone() : remover.Process(scaled);

                Cv2.ImEncode(".png", result, out var png, new ImageEncodingParam(ImwriteFlags.PngCompression, 3));
                encodedFrames.Add(png);

                if (framesFolder is not null)
                {
                    File.WriteAllBytes(Path.Combine(framesFolder, $"frame_{index:D6}.png"), png);
                }

                var processed = encodedFrames.Count;
                var elapsed = stopwatch.Elapsed.ToString(@"mm\:ss");
                var preview = ImageHelpers.MakePreview(result);
                var message = total > 0
                    ? Loc.Format("FrameProgress", processed, total, elapsed)
                    : Loc.Format("FrameProgressNoTotal", processed, elapsed);
                progress.Report(new ProgressInfo(processed, total, message, preview));
            }
        }
        finally
        {
            remover?.Dispose();
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (encodedFrames.Count == 0)
        {
            throw new InvalidOperationException(Loc.Get("ErrNoFrames"));
        }

        string? sheetPath = null;

        if (options.SaveSpriteSheet)
        {
            progress.Report(new ProgressInfo(encodedFrames.Count, encodedFrames.Count, Loc.Get("BuildingSheet")));
            sheetPath = Path.Combine(options.OutputFolder, baseName + "_spritesheet.png");
            BuildSheet(encodedFrames, options.Columns, options.Padding, sheetPath, cancellationToken);
        }

        progress.Report(new ProgressInfo(
            encodedFrames.Count,
            encodedFrames.Count,
            Loc.Format("Done", encodedFrames.Count, stopwatch.Elapsed.ToString(@"mm\:ss"))));

        return new ProcessingResult(encodedFrames.Count, sheetPath, framesFolder, provider);
    }

    private static void Validate(ProcessingOptions options)
    {
        if (!File.Exists(options.VideoPath))
        {
            throw new InvalidOperationException(Loc.Get("ErrVideoMissing"));
        }

        if (string.IsNullOrWhiteSpace(options.OutputFolder))
        {
            throw new InvalidOperationException(Loc.Get("ErrOutputMissing"));
        }

        if (!options.SaveIndividualFrames && !options.SaveSpriteSheet)
        {
            throw new InvalidOperationException(Loc.Get("ErrNothingToSave"));
        }

        if (options.RemoveBackground && !File.Exists(options.ModelPath))
        {
            throw new InvalidOperationException(Loc.Get("ErrModelMissing"));
        }
    }

    private static Mat Scale(Mat frame, int percent)
    {
        if (percent >= 100)
        {
            return frame.Clone();
        }

        var factor = Math.Max(1, percent) / 100.0;
        var resized = new Mat();
        Cv2.Resize(
            frame,
            resized,
            new Size(Math.Max(1, (int)Math.Round(frame.Width * factor)), Math.Max(1, (int)Math.Round(frame.Height * factor))),
            0,
            0,
            InterpolationFlags.Area);
        return resized;
    }

    private static void BuildSheet(List<byte[]> encodedFrames, int requestedColumns, int padding, string path, CancellationToken cancellationToken)
    {
        using var first = Cv2.ImDecode(encodedFrames[0], ImreadModes.Unchanged);
        var cellWidth = first.Width;
        var cellHeight = first.Height;
        var channels = first.Channels();
        var count = encodedFrames.Count;
        var columns = Math.Clamp(requestedColumns, 1, count);
        var rows = (count + columns - 1) / columns;
        padding = Math.Max(0, padding);

        var sheetWidth = columns * cellWidth + (columns + 1) * padding;
        var sheetHeight = rows * cellHeight + (rows + 1) * padding;
        var bytes = (long)sheetWidth * sheetHeight * channels;

        if (bytes > MaxSheetBytes)
        {
            throw new InvalidOperationException(Loc.Format("ErrSheetTooBig", sheetWidth, sheetHeight));
        }

        var type = channels == 4 ? MatType.CV_8UC4 : MatType.CV_8UC3;
        using var sheet = new Mat(sheetHeight, sheetWidth, type, Scalar.All(0));

        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var cell = Cv2.ImDecode(encodedFrames[i], ImreadModes.Unchanged);
            var x = padding + (i % columns) * (cellWidth + padding);
            var y = padding + (i / columns) * (cellHeight + padding);

            using var target = new Mat(sheet, new Rect(x, y, cellWidth, cellHeight));
            cell.CopyTo(target);
        }

        ImageWriter.SavePng(path, sheet);
    }
}
