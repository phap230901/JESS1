namespace AndroidAutomationSuite.WinForms.Core.Models;

public sealed class DetectionResult
{
    public required bool IsMatch { get; init; }
    public required double Confidence { get; init; }
    public required Rectangle Bounds { get; init; }
    public required string SourceImagePath { get; init; }
    public required string TemplateImagePath { get; init; }

    public Point Center => new(Bounds.X + (Bounds.Width / 2), Bounds.Y + (Bounds.Height / 2));
}
