<div align="center">

# 🎬 VideoSpriteSheet

**A fast, modern Windows desktop app to turn videos into frame sequences and sprite sheets with offline AI background removal.**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows)](https://www.microsoft.com/windows)
[![Persian](https://img.shields.io/badge/Language-Persian%20%7C%20English-blue)](#-features)

[فارسی](README.fa.md) • [English](README.md) • [Features](#-features) • [Installation](#-build-and-run) • [Models](#-background-removal-models)

---

</div>

## 📌 Overview

**VideoSpriteSheet** is an intuitive Windows utility built for game developers, animators, and digital creators. It allows you to break down video files into individual PNG frames or compact sprite sheets, featuring built-in, offline AI background removal powered by ONNX Runtime.

---

## ✨ Features

- 🖱️ **Drag & Drop Interface:** Easily load videos directly into the app.
- 🎞️ **Interactive Timeline:** Fine-tune start/end ranges with visual handles or numerical inputs.
- ⏱️ **Frame Extraction Control:** Extract every $N^{\text{th}}$ frame to optimize sheet density and animation smoothness.
- 🤖 **Offline AI Background Removal:** Powered by ONNX Runtime with DirectML GPU acceleration and automatic CPU fallback.
- 🖼️ **Flexible Export Settings:** Save individual PNG frames, a packed sprite sheet, or both. Customize columns, padding, and frame dimensions.
- 📁 **Custom Output Location:** Automatically saves next to the source video or to any custom folder.
- ⚙️ **Storage Control:** Specify custom download directories for AI models.
- 🌐 **Multi-language & Themes:** Supports English and Persian languages along with Light, Dark, and System themes.

---

## 🛠️ Requirements

- **OS:** Windows 10 or Windows 11 (x64)
- **Runtime:** [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/) (to run) or **.NET 10 SDK** (to build)
- **GPU (Optional):** DirectX 12 capable GPU for DirectML hardware acceleration *(CPU fallback is automatically used if no compatible GPU is detected)*.

---

## 🚀 Build and Run

Clone the repository and open `VideoSpriteSheet.csproj` in Visual Studio, then hit `F5`, or use the CLI:

```bash
# Clone the repository
git clone https://github.com/your-username/VideoSpriteSheet.git

# Navigate to the directory
cd VideoSpriteSheet

# Run the application
dotnet run -c Release
```

---

## 📖 How to Use

1. **Import:** Drag & drop a video into the window or select one using the file dialog.
2. **Trim:** Set your desired segment on the visual timeline and choose the frame extraction step (e.g., *one image every N frames*).
3. **Background Removal (Optional):** Open *Advanced background settings*, pick an AI model, and download it (required only on the first run).
4. **Export:** Choose to output individual PNGs, a merged sprite sheet, or both, then click **Start**.

---

## 🤖 Background Removal Models

Models are not bundled with the application repository to keep the download light. They are fetched directly from Hugging Face on demand and cached locally.

| Model | License | Description / Performance |
| :--- | :--- | :--- |
| **[RMBG-1.4](https://huggingface.co/briaai/RMBG-1.4)** | `bria-rmbg-1.4` *(Non-Commercial)* | Ultra-fast and lightweight. Great for personal projects. |
| **[BiRefNet Lite](https://huggingface.co/onnx-community/BiRefNet_lite-ONNX)** | `MIT` | Balanced option with sharp edge detection and moderate resource usage. |
| **[BiRefNet](https://huggingface.co/onnx-community/BiRefNet-ONNX)** | `MIT` | Highest quality cutouts (~973 MB). Recommended for high VRAM GPUs. |

> 💡 **Tip:** Select the **ImageNet** preset for BiRefNet models. Any ONNX segmentation model outputting a single-channel mask is supported manually. Always double-check model licenses before commercial use.

---

## 📦 Third-Party Libraries

| Component | License | Purpose |
| :--- | :--- | :--- |
| **[OpenCvSharp](https://github.com/shimat/opencvsharp)** | Apache-2.0 | Video processing & image manipulation |
| **[ONNX Runtime (DirectML)](https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime.DirectML)** | Package License | AI model inference engine |
| **[Microsoft.AI.DirectML](https://www.nuget.org/packages/Microsoft.AI.DirectML)** | Package License | Direct3D 12 hardware acceleration |

*Note: NuGet package licenses may vary across versions. Please verify licensing prior to redistributing binaries.*

---

## 📄 License

This project is licensed under the **[MIT License](LICENSE)**. Third-party packages and AI models maintain their respective licenses.