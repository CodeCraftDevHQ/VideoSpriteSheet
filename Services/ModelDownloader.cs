using VideoSpriteSheet.Models;

namespace VideoSpriteSheet.Services;

public sealed record ModelEntry(string Name, string Url, string FileName, NormalizationPreset Preset)
{
    public override string ToString() => Name;
}

public static class ModelCatalog
{
    public static IReadOnlyList<ModelEntry> All { get; } = new[]
    {
        new ModelEntry(
            "RMBG-1.4 (176 MB, non-commercial)",
            "https://huggingface.co/briaai/RMBG-1.4/resolve/main/onnx/model.onnx",
            "rmbg-1.4.onnx",
            NormalizationPreset.Rmbg14),
        new ModelEntry(
            "BiRefNet Lite (MIT)",
            "https://huggingface.co/onnx-community/BiRefNet_lite-ONNX/resolve/main/onnx/model.onnx",
            "birefnet-lite.onnx",
            NormalizationPreset.ImageNet),
        new ModelEntry(
            "BiRefNet (973 MB, MIT)",
            "https://huggingface.co/onnx-community/BiRefNet-ONNX/resolve/main/onnx/model.onnx",
            "birefnet.onnx",
            NormalizationPreset.ImageNet)
    };
}

public static class ModelDownloader
{
    public static string DefaultModelsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VideoSpriteSheet",
        "models");

    public static string ModelsFolder => string.IsNullOrWhiteSpace(AppSettings.Current.ModelsFolder)
        ? DefaultModelsFolder
        : AppSettings.Current.ModelsFolder;

    public static string PathFor(ModelEntry entry) => Path.Combine(ModelsFolder, entry.FileName);

    public static async Task DownloadAsync(string url, string target, IProgress<double> progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var partial = target + ".part";

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1L;

        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var destination = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            var buffer = new byte[81920];
            long received = 0;
            int read;

            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                received += read;

                if (total > 0)
                {
                    progress.Report((double)received / total);
                }
            }
        }

        File.Move(partial, target, true);
    }
}
