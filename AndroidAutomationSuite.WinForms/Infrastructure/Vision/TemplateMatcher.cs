using AndroidAutomationSuite.WinForms.Core.Models;
using OpenCvSharp;

namespace AndroidAutomationSuite.WinForms.Infrastructure.Vision;

public sealed class TemplateMatcher
{
    public DetectionResult MatchMultiScale(string screenshotPath, string templatePath, double threshold)
    {
        using var source = Cv2.ImRead(screenshotPath, ImreadModes.Color);
        using var template = Cv2.ImRead(templatePath, ImreadModes.Color);

        if (source.Empty() || template.Empty())
        {
            return new DetectionResult
            {
                IsMatch = false,
                Confidence = 0,
                Bounds = Rectangle.Empty,
                SourceImagePath = screenshotPath,
                TemplateImagePath = templatePath
            };
        }

        var scales = new[] { 0.7, 0.85, 1.0, 1.15, 1.3 };
        var bestScore = double.MinValue;
        Rect bestRect = Rect.Empty;

        foreach (var scale in scales)
        {
            var newWidth = Math.Max(1, (int)(template.Width * scale));
            var newHeight = Math.Max(1, (int)(template.Height * scale));
            if (newWidth >= source.Width || newHeight >= source.Height) continue;

            using var resizedTemplate = new Mat();
            Cv2.Resize(template, resizedTemplate, new OpenCvSharp.Size(newWidth, newHeight));

            using var result = new Mat();
            Cv2.MatchTemplate(source, resizedTemplate, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out var maxLoc);

            if (maxVal > bestScore)
            {
                bestScore = maxVal;
                bestRect = new Rect(maxLoc.X, maxLoc.Y, resizedTemplate.Width, resizedTemplate.Height);
            }
        }

        var match = bestScore >= threshold;
        return new DetectionResult
        {
            IsMatch = match,
            Confidence = bestScore,
            Bounds = match ? new Rectangle(bestRect.X, bestRect.Y, bestRect.Width, bestRect.Height) : Rectangle.Empty,
            SourceImagePath = screenshotPath,
            TemplateImagePath = templatePath
        };
    }
}
