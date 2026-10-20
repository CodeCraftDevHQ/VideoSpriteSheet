namespace VideoSpriteSheet.Services;

public static class ModelDownloader
{
    public const string Rmbg14Url = "https://huggingface.co/briaai/RMBG-1.4/resolve/main/onnx/model.onnx";

    public static string DefaultModelPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VideoSpriteSheet",
        "models",
        "rmbg-1.4.onnx");

    public static async Task DownloadAsync(string url, string target, IProgress<double> progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var partial = target + ".part";

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
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
