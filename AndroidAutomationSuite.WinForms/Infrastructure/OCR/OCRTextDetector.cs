using AndroidAutomationSuite.WinForms.Core.Models;
using PaddleOCRSharp;

namespace AndroidAutomationSuite.WinForms.Infrastructure.OCR;

public sealed class OcrTextDetector : IDisposable
{
    private readonly PaddleOCREngine _engine;

    public OcrTextDetector()
    {
        var parameter = new OCRModelConfig();
        _engine = new PaddleOCREngine(parameter, new OCRParameter { enable_mkldnn = true, cpu_math_library_num_threads = 4 });
    }

    public OcrResult Detect(string imagePath)
    {
        var result = _engine.DetectText(imagePath);
        var blocks = new List<OcrTextBlock>();

        foreach (var line in result.TextBlocks)
        {
            var x = line.BoxPoints.Min(p => p.X);
            var y = line.BoxPoints.Min(p => p.Y);
            var maxX = line.BoxPoints.Max(p => p.X);
            var maxY = line.BoxPoints.Max(p => p.Y);

            blocks.Add(new OcrTextBlock
            {
                Text = line.Text,
                Confidence = line.Score,
                Bounds = new Rectangle(x, y, maxX - x, maxY - y)
            });
        }

        return new OcrResult { SourceImagePath = imagePath, Blocks = blocks };
    }

    public void Dispose() => _engine.Dispose();
}
