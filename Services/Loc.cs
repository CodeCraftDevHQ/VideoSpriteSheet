using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace VideoSpriteSheet.Services;

public sealed class Loc : INotifyPropertyChanged
{
    private static readonly Dictionary<string, (string Fa, string En)> Strings = new()
    {
        ["AppTitle"] = ("ویدیو به اسپرایت‌شیت", "Video to Sprite Sheet"),
        ["Settings"] = ("تنظیمات", "Settings"),
        ["GroupInput"] = ("ورودی و خروجی", "Input & Output"),
        ["BrowseVideo"] = ("انتخاب ویدیو", "Select video"),
        ["BrowseOutput"] = ("پوشه خروجی", "Output folder"),
        ["GroupFrames"] = ("گرفتن فریم‌ها", "Frame capture"),
        ["EveryNth"] = ("هر چند فریم یک عکس:", "One image every N frames:"),
        ["ImageSize"] = ("اندازه هر عکس:", "Image size:"),
        ["FromSec"] = ("از ثانیه:", "From (sec):"),
        ["ToSec"] = ("تا ثانیه:", "To (sec):"),
        ["GroupBg"] = ("حذف بکگراند", "Background removal"),
        ["RemoveBg"] = ("حذف بکگراند فعال باشد", "Remove background"),
        ["SelectModel"] = ("انتخاب مدل", "Select model"),
        ["Download"] = ("دانلود", "Download"),
        ["ModelNote"] = ("توجه: مجوز مدل RMBG-1.4 غیرتجاری است. برای استفاده تجاری از مدل‌های BiRefNet (مجوز MIT) استفاده کنید.", "Note: RMBG-1.4 is licensed for non-commercial use only. For commercial use choose a BiRefNet model (MIT license)."),
        ["Preset"] = ("پیش‌تنظیم مدل:", "Model preset:"),
        ["PresetRmbg"] = ("RMBG-1.4 (پیش‌فرض)", "RMBG-1.4 (default)"),
        ["PresetImageNet"] = ("ImageNet (BiRefNet و مشابه)", "ImageNet (BiRefNet & similar)"),
        ["Sharpness"] = ("تیزی لبه‌ها:", "Edge sharpness:"),
        ["UseGpu"] = ("استفاده از کارت گرافیک (DirectML)", "Use GPU (DirectML)"),
        ["GroupOutput"] = ("خروجی", "Output"),
        ["SaveFrames"] = ("ذخیره تک‌تک عکس‌ها (PNG)", "Save individual images (PNG)"),
        ["SaveSheet"] = ("ذخیره به صورت اسپرایت‌شیت", "Save as sprite sheet"),
        ["Columns"] = ("تعداد ستون:", "Columns:"),
        ["Padding"] = ("فاصله (پیکسل):", "Padding (px):"),
        ["Start"] = ("شروع", "Start"),
        ["Cancel"] = ("لغو", "Cancel"),
        ["OpenFolder"] = ("باز کردن پوشه خروجی", "Open output folder"),
        ["Ready"] = ("آماده", "Ready"),
        ["Language"] = ("زبان", "Language"),
        ["Theme"] = ("ظاهر برنامه", "Theme"),
        ["ThemeSystem"] = ("مطابق سیستم", "Follow system"),
        ["ThemeLight"] = ("روشن", "Light"),
        ["ThemeDark"] = ("تاریک", "Dark"),
        ["Close"] = ("بستن", "Close"),
        ["VideoFilter"] = ("ویدیو|*.mp4;*.mov;*.avi;*.mkv;*.webm;*.wmv;*.m4v;*.flv|همه فایل‌ها|*.*", "Video|*.mp4;*.mov;*.avi;*.mkv;*.webm;*.wmv;*.m4v;*.flv|All files|*.*"),
        ["ModelFilter"] = ("مدل ONNX|*.onnx", "ONNX model|*.onnx"),
        ["PickModelTitle"] = ("انتخاب مدل ONNX", "Select ONNX model"),
        ["PickOutputTitle"] = ("انتخاب پوشه خروجی", "Select output folder"),
        ["VideoInfo"] = ("{0}×{1} — {2:0.##} فریم بر ثانیه — {3} فریم — مدت: {4}", "{0}×{1} — {2:0.##} fps — {3} frames — duration: {4}"),
        ["DropPlaceholder"] = ("ویدیو را اینجا رها کنید\nیا برای انتخاب کلیک کنید", "Drop a video here\nor click to select"),
        ["TimeHint"] = ("خالی = ابتدا/انتهای ویدیو. بازه را می‌توانید روی نوار زیر ویدیو هم تعیین کنید. فرمت: ثانیه یا mm:ss", "Empty = start/end of the video. You can also set the range on the timeline under the video. Format: seconds or mm:ss"),
        ["TabVideo"] = ("ویدیو", "Video"),
        ["TabResult"] = ("نتیجه", "Result"),
        ["Play"] = ("پخش", "Play"),
        ["Pause"] = ("توقف", "Pause"),
        ["SetStart"] = ("شروع از اینجا", "Start here"),
        ["SetEnd"] = ("پایان تا اینجا", "End here"),
        ["FullVideo"] = ("کل ویدیو", "Full video"),
        ["AdvBg"] = ("تنظیمات پیشرفته حذف بکگراند", "Advanced background settings"),
        ["AdvLayout"] = ("اندازه تصویر و چیدمان اسپرایت‌شیت", "Image size & sprite sheet layout"),
        ["ModelsFolder"] = ("پوشه ذخیره مدل‌ها", "Models folder"),
        ["ModelsFolderNote"] = ("مدل‌هایی که قبلاً دانلود شده‌اند به پوشه جدید منتقل نمی‌شوند.", "Models that were already downloaded are not moved to the new folder."),
        ["ResetDefault"] = ("پیش‌فرض", "Default"),
        ["Browse"] = ("انتخاب...", "Browse..."),
        ["Estimate"] = ("≈ {0} عکس", "≈ {0} images"),
        ["ErrStep"] = ("مقدار «هر چند فریم» باید عدد صحیح مثبت باشد.", "\"Every N frames\" must be a positive whole number."),
        ["ErrColumns"] = ("تعداد ستون باید عدد صحیح مثبت باشد.", "Columns must be a positive whole number."),
        ["ErrPadding"] = ("مقدار فاصله باید عدد صحیح صفر یا بیشتر باشد.", "Padding must be a whole number, zero or more."),
        ["ErrTime"] = ("زمان وارد شده معتبر نیست. از ثانیه یا فرمت mm:ss استفاده کنید.", "Invalid time. Use seconds or mm:ss."),
        ["ErrRange"] = ("زمان شروع باید کمتر از زمان پایان باشد.", "The start time must be earlier than the end time."),
        ["ErrStartBeyond"] = ("زمان شروع بعد از پایان ویدیو است.", "The start time is beyond the end of the video."),
        ["ErrUnsupportedDrop"] = ("فرمت فایل پشتیبانی نمی‌شود.", "This file format is not supported."),
        ["ErrVideoMissing"] = ("فایل ویدیو انتخاب نشده یا وجود ندارد.", "No video selected or the file does not exist."),
        ["ErrOutputMissing"] = ("پوشه خروجی انتخاب نشده است.", "No output folder selected."),
        ["ErrNothingToSave"] = ("حداقل یکی از گزینه‌های ذخیره تک‌تک عکس‌ها یا اسپرایت‌شیت را فعال کنید.", "Enable at least one of: save individual images or sprite sheet."),
        ["ErrModelMissing"] = ("برای حذف بکگراند باید فایل مدل ONNX را انتخاب یا دانلود کنید.", "Select or download an ONNX model to remove the background."),
        ["ErrModelFile"] = ("فایل مدل حذف بکگراند پیدا نشد.", "The background removal model file was not found."),
        ["ErrVideoOpen"] = ("فایل ویدیو باز نشد. فرمت یا کدک ویدیو پشتیبانی نمی‌شود.", "Could not open the video. The format or codec is not supported."),
        ["ErrVideoSize"] = ("ابعاد ویدیو قابل تشخیص نیست.", "Could not determine the video size."),
        ["ErrNoFrames"] = ("هیچ فریمی از ویدیو خوانده نشد.", "No frames were read from the video."),
        ["ErrSheetTooBig"] = ("اسپرایت‌شیت بسیار بزرگ می‌شود ({0}×{1}). درصد اندازه را کم کنید، فاصله فریم‌ها را بیشتر کنید یا بازه زمانی را کوتاه‌تر کنید.", "The sprite sheet would be too large ({0}×{1}). Reduce the image size, take fewer frames, or shorten the time range."),
        ["LoadingModel"] = ("در حال بارگذاری مدل...", "Loading model..."),
        ["FrameProgress"] = ("فریم {0} از {1} — {2}", "Frame {0} of {1} — {2}"),
        ["FrameProgressNoTotal"] = ("فریم {0} — {1}", "Frame {0} — {1}"),
        ["BuildingSheet"] = ("در حال ساخت اسپرایت‌شیت...", "Building sprite sheet..."),
        ["Done"] = ("پایان — {0} فریم در {1}", "Done — {0} frames in {1}"),
        ["ResultFrames"] = ("{0} فریم پردازش شد", "{0} frames processed"),
        ["ResultSheet"] = ("اسپرایت‌شیت: {0}", "Sprite sheet: {0}"),
        ["ResultFolder"] = ("عکس‌ها: {0}", "Images: {0}"),
        ["ResultProvider"] = ("پردازنده: {0}", "Processor: {0}"),
        ["Cancelled"] = ("عملیات لغو شد.", "Operation cancelled."),
        ["ErrorPrefix"] = ("خطا: {0}", "Error: {0}"),
        ["ModelExists"] = ("این مدل از قبل دانلود شده است.", "This model is already downloaded."),
        ["Downloading"] = ("در حال دانلود مدل...", "Downloading model..."),
        ["DownloadDone"] = ("دانلود مدل کامل شد.", "Model download finished."),
        ["DownloadFail"] = ("دانلود مدل ناموفق بود: {0}", "Model download failed: {0}")
    };

    public static Loc Instance { get; } = new();

    private string _language = "fa";

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action? LanguageChanged;

    public string Language
    {
        get => _language;
        set
        {
            if (_language == value)
            {
                return;
            }

            _language = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlowDirection)));
            LanguageChanged?.Invoke();
        }
    }

    public FlowDirection FlowDirection => _language == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public string this[string key] => Get(key);

    public static string Get(string key)
    {
        if (!Strings.TryGetValue(key, out var entry))
        {
            return key;
        }

        return Instance._language == "fa" ? entry.Fa : entry.En;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, Get(key), args);
    }
}

[MarkupExtensionReturnType(typeof(string))]
public sealed class TExtension : MarkupExtension
{
    public TExtension()
    {
    }

    public TExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = Loc.Instance,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
