using OpenCvSharp;

namespace VideoSpriteSheet.Services;

public static class ImageWriter
{
    public static void SavePng(string path, Mat image, int compression = 3)
    {
        Cv2.ImEncode(".png", image, out var bytes, new ImageEncodingParam(ImwriteFlags.PngCompression, compression));
        File.WriteAllBytes(path, bytes);
    }
}
