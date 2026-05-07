using AndroidAutomationSuite.WinForms.Core.Interfaces;
using AndroidAutomationSuite.WinForms.Core.Models;

namespace AndroidAutomationSuite.WinForms.Infrastructure.Vision;

public sealed class ImageDetectionService(IAdbService adbService, IAppLogger logger) : IImageDetectionService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TemplateMatcher _matcher = new();

    public async Task<DetectionResult> DetectTemplateAsync(string screenshotPath, string templatePath, double threshold, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return await Task.Run(() => _matcher.MatchMultiScale(screenshotPath, templatePath, threshold), ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DetectionResult> DetectOnDeviceAsync(string serial, string templatePath, double threshold, CancellationToken ct)
    {
        var screenshotPath = Path.Combine(Path.GetTempPath(), $"detect_{serial}_{Guid.NewGuid():N}.png");
        try
        {
            var screenResult = await adbService.CaptureScreenAsync(serial, screenshotPath, ct);
            if (!screenResult.Success)
            {
                logger.Error($"Capture for detection failed: {screenResult.StandardError}");
                return new DetectionResult { IsMatch = false, Confidence = 0, Bounds = Rectangle.Empty, SourceImagePath = screenshotPath, TemplateImagePath = templatePath };
            }

            return await DetectTemplateAsync(screenshotPath, templatePath, threshold, ct);
        }
        finally
        {
            try { if (File.Exists(screenshotPath)) File.Delete(screenshotPath); } catch { }
        }
    }

    public async Task<CommandResult> ClickDetectedImageAsync(string serial, string templatePath, double threshold, CancellationToken ct)
    {
        var detection = await DetectOnDeviceAsync(serial, templatePath, threshold, ct);
        if (!detection.IsMatch)
        {
            return new CommandResult
            {
                Success = false,
                Command = $"ImageDetectClick {templatePath}",
                StandardOutput = string.Empty,
                StandardError = $"Template not found above threshold {threshold}. Best confidence: {detection.Confidence:F3}",
                ExitCode = -1,
                Duration = TimeSpan.Zero
            };
        }

        logger.Info($"Image detected at {detection.Center.X},{detection.Center.Y} (confidence: {detection.Confidence:F3})");
        return await adbService.TapAsync(serial, detection.Center.X, detection.Center.Y, ct);
    }
}
