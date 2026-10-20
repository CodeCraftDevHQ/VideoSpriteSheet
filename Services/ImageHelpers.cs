using System.Windows.Media.Imaging;
using OpenCvSharp;

namespace VideoSpriteSheet.Services;

public static class ImageHelpers
{
    public static BitmapSource ToBitmap(byte[] encoded)
    {
        using var stream = new MemoryStream(encoded);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public static BitmapSource MakePreview(Mat image, int maxSide = 480)
    {
        var longest = Math.Max(image.Width, image.Height);

        if (longest <= maxSide)
        {
            Cv2.ImEncode(".png", image, out var direct);
            return ToBitmap(direct);
        }

        var ratio = (double)maxSide / longest;
        using var small = new Mat();
        Cv2.Resize(image, small, new Size(Math.Max(1, (int)(image.Width * ratio)), Math.Max(1, (int)(image.Height * ratio))), 0, 0, InterpolationFlags.Area);
        Cv2.ImEncode(".png", small, out var bytes);
        return ToBitmap(bytes);
    }

    public static string NormalizeDigits(string text)
    {
        var chars = text.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];

            if (c >= '۰' && c <= '۹')
            {
                chars[i] = (char)('0' + (c - '۰'));
            }
            else if (c >= '٠' && c <= '٩')
            {
                chars[i] = (char)('0' + (c - '٠'));
            }
        }

        return new string(chars).Trim();
    }
}
