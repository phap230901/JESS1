using AndroidAutomationSuite.WinForms.Core.Interfaces;
using AndroidAutomationSuite.WinForms.Core.Models;

namespace AndroidAutomationSuite.WinForms.Infrastructure.OCR;

public sealed class OcrService(IAdbService adbService, IAppLogger logger) : IOcrService, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly OcrTextDetector _detector = new();

    public async Task<OcrResult> DetectTextAsync(string imagePath, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return await Task.Run(() => _detector.Detect(imagePath), ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OcrTextBlock?> SearchTextAsync(string imagePath, string targetText, CancellationToken ct)
    {
        var result = await DetectTextAsync(imagePath, ct);
        return result.Find(targetText);
    }

    public async Task<CommandResult> ClickTextAsync(string serial, string targetText, CancellationToken ct)
    {
        var screenshotPath = Path.Combine(Path.GetTempPath(), $"ocr_{serial}_{Guid.NewGuid():N}.png");
        var screenResult = await adbService.CaptureScreenAsync(serial, screenshotPath, ct);
        if (!screenResult.Success) return screenResult;

        try
        {
            var match = await SearchTextAsync(screenshotPath, targetText, ct);
            if (match is null)
            {
                return new CommandResult { Success = false, Command = $"OCR ClickText '{targetText}'", StandardOutput = string.Empty, StandardError = "Target text not found", ExitCode = -1, Duration = TimeSpan.Zero };
            }

            var centerX = match.Bounds.X + (match.Bounds.Width / 2);
            var centerY = match.Bounds.Y + (match.Bounds.Height / 2);
            logger.Info($"OCR matched '{targetText}' at {centerX},{centerY}");
            return await adbService.TapAsync(serial, centerX, centerY, ct);
        }
        finally
        {
            try { if (File.Exists(screenshotPath)) File.Delete(screenshotPath); } catch { }
        }
    }

    public void Dispose()
    {
        _detector.Dispose();
        _gate.Dispose();
    }
}
