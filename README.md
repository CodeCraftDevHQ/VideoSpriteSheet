# VideoSpriteSheet

[فارسی](README.fa.md)

A Windows desktop app that turns a video into individual frames, optionally removes the background of every frame with an offline AI model, and packs the result into a sprite sheet (or saves each frame as a separate PNG).

## Features

- Drag & drop a video onto the window, or pick one with the file dialog
- Built-in video player with a timeline: drag the blue handles to choose the time range to convert (or type start/end seconds)
- Take one image every N frames
- Background removal on/off, fully offline (ONNX Runtime, GPU via DirectML with automatic CPU fallback)
- Save individual PNGs, a sprite sheet, or both; configurable columns, padding and image size
- Output goes next to the video by default; you can pick any folder
- Choose where downloaded models are stored (Settings)
- Persian and English UI, light / dark / system theme

## Requirements

- Windows 10 or 11, x64
- .NET 10 SDK (to build) / .NET 10 Desktop Runtime (to run)
- A DirectX 12 capable GPU for GPU acceleration (optional, CPU works too)

## Build and run

Open `VideoSpriteSheet.csproj` in Visual Studio and press F5, or:

```
dotnet run -c Release
```

## Usage

1. Drop a video on the window.
2. Optionally set the range on the timeline and the "one image every N frames" value.
3. Open "Advanced background settings", pick a model and press Download (first run only).
4. Choose whether to save individual images, a sprite sheet, or both, then press Start.

## Background removal models

Models are not part of this repository. They are downloaded from Hugging Face on demand and stored in the models folder (Settings).

| Model | License | Notes |
|---|---|---|
| [RMBG-1.4](https://huggingface.co/briaai/RMBG-1.4) | bria-rmbg-1.4 (non-commercial) | Small and fast. Not for commercial use. |
| [BiRefNet Lite](https://huggingface.co/onnx-community/BiRefNet_lite-ONNX) | MIT | Better edges, lighter than the full model |
| [BiRefNet](https://huggingface.co/onnx-community/BiRefNet-ONNX) | MIT | Best quality, about 973 MB, needs more GPU memory |

Use the "ImageNet" preset for BiRefNet models. Any other ONNX segmentation model that outputs a single-channel mask can be selected manually. Always check the license of a model before using its output commercially.

## Third-party components

| Component | License |
|---|---|
| [OpenCvSharp](https://github.com/shimat/opencvsharp) | Apache-2.0 |
| [ONNX Runtime (DirectML)](https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime.DirectML) | See the package page |
| [Microsoft.AI.DirectML](https://www.nuget.org/packages/Microsoft.AI.DirectML) | See the package page |

Licenses of NuGet packages can change between versions, so please verify them on the package pages before redistributing binaries.

## License

The source code of this project is released under the [MIT License](LICENSE). Models and third-party packages keep their own licenses.
