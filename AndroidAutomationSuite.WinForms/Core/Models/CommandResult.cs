namespace AndroidAutomationSuite.WinForms.Core.Models;

public sealed class CommandResult
{
    public required bool Success { get; init; }
    public required string Command { get; init; }
    public required string StandardOutput { get; init; }
    public required string StandardError { get; init; }
    public required int ExitCode { get; init; }
    public required TimeSpan Duration { get; init; }
    public string CombinedOutput => $"{StandardOutput}\n{StandardError}".Trim();

    public static CommandResult Timeout(string command, TimeSpan duration) => new()
    {
        Success = false,
        Command = command,
        StandardOutput = string.Empty,
        StandardError = $"Command timed out after {duration.TotalSeconds:F1}s",
        ExitCode = -1,
        Duration = duration
    };
}
