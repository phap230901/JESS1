namespace AndroidAutomationSuite.WinForms.Core.Models;

public sealed class OcrTextBlock
{
    public required string Text { get; init; }
    public required Rectangle Bounds { get; init; }
    public required float Confidence { get; init; }
}

public sealed class OcrResult
{
    public required string SourceImagePath { get; init; }
    public required IReadOnlyList<OcrTextBlock> Blocks { get; init; }

    public OcrTextBlock? Find(string targetText, StringComparison comparison = StringComparison.OrdinalIgnoreCase) =>
        Blocks.FirstOrDefault(b => b.Text.Contains(targetText, comparison));
}
