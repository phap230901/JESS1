using System.Diagnostics;
using System.Text;
using AndroidAutomationSuite.WinForms.Core.Interfaces;
using AndroidAutomationSuite.WinForms.Core.Models;

namespace AndroidAutomationSuite.WinForms.Infrastructure.ADB;

public sealed class AdbService(IAppLogger logger) : IAdbService
{
    private const string AdbExe = "adb";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken ct)
    {
        var result = await RunAdbCommandAsync("devices", DefaultTimeout, ct);
        if (!result.Success)
        {
            logger.Error($"Failed to get devices: {result.StandardError}");
            return [];
        }

        return result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(line => line.Trim())
            .Where(line => line.Contains('\t'))
            .Select(line => line.Split('\t'))
            .Where(parts => parts.Length >= 2)
            .Select(parts => new DeviceInfo(parts[0], parts[1], parts[1].Equals("device", StringComparison.OrdinalIgnoreCase), DateTimeOffset.UtcNow))
            .ToList();
    }

    public Task<CommandResult> TapAsync(string serial, int x, int y, CancellationToken ct) =>
        RunDeviceCommandAsync(serial, $"shell input tap {x} {y}", DefaultTimeout, ct);

    public Task<CommandResult> SwipeAsync(string serial, int x1, int y1, int x2, int y2, int durationMs, CancellationToken ct) =>
        RunDeviceCommandAsync(serial, $"shell input swipe {x1} {y1} {x2} {y2} {durationMs}", DefaultTimeout, ct);

    public Task<CommandResult> InputTextAsync(string serial, string text, CancellationToken ct)
    {
        var escaped = text.Replace(" ", "%s").Replace("\"", "\\\"");
        return RunDeviceCommandAsync(serial, $"shell input text \"{escaped}\"", DefaultTimeout, ct);
    }

    public async Task<CommandResult> CaptureScreenAsync(string serial, string outputPath, CancellationToken ct)
    {
        var remotePath = $"/sdcard/__auto_screen_{serial}.png";
        var capture = await RunDeviceCommandAsync(serial, $"shell screencap -p {remotePath}", DefaultTimeout, ct);
        if (!capture.Success) return capture;

        var pull = await RunDeviceCommandAsync(serial, $"pull {remotePath} \"{outputPath}\"", DefaultTimeout, ct);
        _ = await RunDeviceCommandAsync(serial, $"shell rm {remotePath}", DefaultTimeout, ct);
        return pull;
    }

    public Task<CommandResult> OpenAppAsync(string serial, string packageName, string activity, CancellationToken ct) =>
        RunDeviceCommandAsync(serial, $"shell am start -n {packageName}/{activity}", DefaultTimeout, ct);

    public Task<CommandResult> ForceStopAppAsync(string serial, string packageName, CancellationToken ct) =>
        RunDeviceCommandAsync(serial, $"shell am force-stop {packageName}", DefaultTimeout, ct);

    public Task<CommandResult> ClearAppDataAsync(string serial, string packageName, CancellationToken ct) =>
        RunDeviceCommandAsync(serial, $"shell pm clear {packageName}", DefaultTimeout, ct);

    public Task<CommandResult> InstallApkAsync(string serial, string apkPath, CancellationToken ct) =>
        RunDeviceCommandAsync(serial, $"install -r \"{apkPath}\"", InstallTimeout, ct);

    private Task<CommandResult> RunDeviceCommandAsync(string serial, string command, TimeSpan timeout, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(serial)) throw new ArgumentException("Device serial is required", nameof(serial));
        return RunAdbCommandAsync($"-s {serial} {command}", timeout, ct);
    }

    private async Task<CommandResult> RunAdbCommandAsync(string arguments, TimeSpan timeout, CancellationToken ct)
    {
        var start = DateTime.UtcNow;
        var psi = new ProcessStartInfo
        {
            FileName = AdbExe,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        try
        {
            if (!process.Start())
            {
                return new CommandResult { Success = false, Command = $"{AdbExe} {arguments}", StandardOutput = string.Empty, StandardError = "Failed to start adb process", ExitCode = -1, Duration = DateTime.UtcNow - start };
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(timeout);

            var outTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            var errTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);
            var stdout = await outTask;
            var stderr = await errTask;

            var result = new CommandResult
            {
                Success = process.ExitCode == 0,
                Command = $"{AdbExe} {arguments}",
                StandardOutput = stdout,
                StandardError = stderr,
                ExitCode = process.ExitCode,
                Duration = DateTime.UtcNow - start
            };

            if (result.Success) logger.Info($"{result.Command} completed in {result.Duration.TotalMilliseconds:F0} ms");
            else logger.Error($"{result.Command} failed ({result.ExitCode}): {result.StandardError}");

            return result;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            try { if (!process.HasExited) process.Kill(true); } catch { }
            var timeoutResult = CommandResult.Timeout($"{AdbExe} {arguments}", timeout);
            logger.Error(timeoutResult.StandardError + $" | {timeoutResult.Command}");
            return timeoutResult;
        }
        catch (Exception ex)
        {
            try { if (!process.HasExited) process.Kill(true); } catch { }
            logger.Error($"ADB execution exception for '{arguments}': {ex.Message}");
            return new CommandResult { Success = false, Command = $"{AdbExe} {arguments}", StandardOutput = string.Empty, StandardError = ex.ToString(), ExitCode = -1, Duration = DateTime.UtcNow - start };
        }
    }
}
