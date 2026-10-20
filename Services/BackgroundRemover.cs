using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using VideoSpriteSheet.Models;

namespace VideoSpriteSheet.Services;

public sealed class BackgroundRemover : IDisposable
{
    private const int DefaultInputSize = 1024;

    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly int _inputWidth;
    private readonly int _inputHeight;
    private readonly float[] _mean;
    private readonly float[] _std;
    private readonly double _sharpness;

    public string Provider { get; }

    public BackgroundRemover(string modelPath, bool useGpu, NormalizationPreset preset, double sharpness)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("فایل مدل حذف بکگراند پیدا نشد.", modelPath);
        }

        _sharpness = Math.Max(1.0, sharpness);

        (_session, Provider) = CreateSession(modelPath, useGpu);

        var input = _session.InputMetadata.First();
        _inputName = input.Key;
        var dims = input.Value.Dimensions;
        _inputHeight = dims.Length >= 4 && dims[2] > 0 ? dims[2] : DefaultInputSize;
        _inputWidth = dims.Length >= 4 && dims[3] > 0 ? dims[3] : DefaultInputSize;

        if (preset == NormalizationPreset.ImageNet)
        {
            _mean = new[] { 0.485f, 0.456f, 0.406f };
            _std = new[] { 0.229f, 0.224f, 0.225f };
        }
        else
        {
            _mean = new[] { 0.5f, 0.5f, 0.5f };
            _std = new[] { 1f, 1f, 1f };
        }
    }

    public Mat Process(Mat bgr)
    {
        using var mask = PredictMask(bgr);
        return Compose(bgr, mask);
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    private static (InferenceSession Session, string Provider) CreateSession(string modelPath, bool useGpu)
    {
        if (useGpu)
        {
            try
            {
                using var gpuOptions = new SessionOptions();
                gpuOptions.EnableMemoryPattern = false;
                gpuOptions.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
                gpuOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                gpuOptions.AppendExecutionProvider_DML(0);
                return (new InferenceSession(modelPath, gpuOptions), "GPU (DirectML)");
            }
            catch
            {
            }
        }

        using var cpuOptions = new SessionOptions();
        cpuOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        return (new InferenceSession(modelPath, cpuOptions), "CPU");
    }

    private Mat PredictMask(Mat bgr)
    {
        using var resized = new Mat();
        Cv2.Resize(bgr, resized, new Size(_inputWidth, _inputHeight), 0, 0, InterpolationFlags.Area);

        using var rgb = new Mat();
        Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

        using var floatImage = new Mat();
        rgb.ConvertTo(floatImage, MatType.CV_32FC3, 1.0 / 255.0);

        var plane = _inputWidth * _inputHeight;
        var data = new float[3 * plane];
        var channels = Cv2.Split(floatImage);

        try
        {
            for (var c = 0; c < 3; c++)
            {
                channels[c].GetArray(out float[] values);
                var offset = c * plane;
                var mean = _mean[c];
                var std = _std[c];

                for (var i = 0; i < plane; i++)
                {
                    data[offset + i] = (values[i] - mean) / std;
                }
            }
        }
        finally
        {
            foreach (var channel in channels)
            {
                channel.Dispose();
            }
        }

        var tensor = new DenseTensor<float>(data, new[] { 1, 3, _inputHeight, _inputWidth });
        using var results = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(_inputName, tensor) });
        var output = results.First().AsTensor<float>();
        var dims = output.Dimensions.ToArray();
        var outHeight = dims[^2];
        var outWidth = dims[^1];
        var raw = output.ToArray();

        var min = float.MaxValue;
        var max = float.MinValue;

        for (var i = 0; i < raw.Length; i++)
        {
            var v = raw[i];
            if (v < min) min = v;
            if (v > max) max = v;
        }

        if (min < 0f || max > 1f)
        {
            for (var i = 0; i < raw.Length; i++)
            {
                raw[i] = 1f / (1f + MathF.Exp(-raw[i]));
            }
        }

        var small = new Mat(outHeight, outWidth, MatType.CV_32FC1);
        small.SetArray(raw.AsSpan(0, outHeight * outWidth).ToArray());
        Cv2.Normalize(small, small, 0, 1, NormTypes.MinMax);

        var full = new Mat();
        Cv2.Resize(small, full, new Size(bgr.Width, bgr.Height), 0, 0, InterpolationFlags.Cubic);
        small.Dispose();

        if (_sharpness > 1.0)
        {
            full.ConvertTo(full, MatType.CV_32FC1, _sharpness, 0.5 - 0.5 * _sharpness);
        }

        Cv2.Threshold(full, full, 0, 0, ThresholdTypes.Tozero);
        Cv2.Threshold(full, full, 1, 1, ThresholdTypes.Trunc);

        return full;
    }

    private static Mat Compose(Mat bgr, Mat mask)
    {
        using var alpha = new Mat();
        mask.ConvertTo(alpha, MatType.CV_8UC1, 255.0);

        var channels = Cv2.Split(bgr);

        try
        {
            var bgra = new Mat();
            Cv2.Merge(new[] { channels[0], channels[1], channels[2], alpha }, bgra);
            return bgra;
        }
        finally
        {
            foreach (var channel in channels)
            {
                channel.Dispose();
            }
        }
    }
}
