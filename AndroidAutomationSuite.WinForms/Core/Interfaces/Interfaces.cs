using AndroidAutomationSuite.WinForms.Core.Models;

namespace AndroidAutomationSuite.WinForms.Core.Interfaces;

public interface IAdbService
{
    Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken ct);
    Task<CommandResult> TapAsync(string serial, int x, int y, CancellationToken ct);
    Task<CommandResult> SwipeAsync(string serial, int x1, int y1, int x2, int y2, int durationMs, CancellationToken ct);
    Task<CommandResult> InputTextAsync(string serial, string text, CancellationToken ct);
    Task<CommandResult> CaptureScreenAsync(string serial, string outputPath, CancellationToken ct);
    Task<CommandResult> OpenAppAsync(string serial, string packageName, string activity, CancellationToken ct);
    Task<CommandResult> ForceStopAppAsync(string serial, string packageName, CancellationToken ct);
    Task<CommandResult> ClearAppDataAsync(string serial, string packageName, CancellationToken ct);
    Task<CommandResult> InstallApkAsync(string serial, string apkPath, CancellationToken ct);
}

public interface IDeviceManager
{
    BindingSource Devices { get; }
    Task RefreshDevicesAsync(CancellationToken ct);
}

public interface IAutomationEngine
{
    event EventHandler<WorkflowStepExecution>? StepStatusChanged;
    Task EnqueueAsync(string deviceSerial, WorkflowDefinition workflow, CancellationToken ct);
    Task PauseAsync(string deviceSerial);
    Task ResumeAsync(string deviceSerial);
    Task StopAsync(string deviceSerial);
}

public interface IOcrService
{
    Task<OcrResult> DetectTextAsync(string imagePath, CancellationToken ct);
    Task<OcrTextBlock?> SearchTextAsync(string imagePath, string targetText, CancellationToken ct);
    Task<CommandResult> ClickTextAsync(string serial, string targetText, CancellationToken ct);
}

public interface IImageDetectionService
{
    Task<DetectionResult> DetectTemplateAsync(string screenshotPath, string templatePath, double threshold, CancellationToken ct);
    Task<DetectionResult> DetectOnDeviceAsync(string serial, string templatePath, double threshold, CancellationToken ct);
    Task<CommandResult> ClickDetectedImageAsync(string serial, string templatePath, double threshold, CancellationToken ct);
}

public interface IDatabaseService
{
    void Initialize();
}

public interface IAppLogger
{
    void Info(string message);
    void Error(string message);
}
