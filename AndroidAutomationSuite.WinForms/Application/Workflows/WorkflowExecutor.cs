using AndroidAutomationSuite.WinForms.Core.Interfaces;
using AndroidAutomationSuite.WinForms.Core.Models;

namespace AndroidAutomationSuite.WinForms.Application.Workflows;

public sealed class WorkflowExecutor(IAdbService adb, IOcrService ocr, IImageDetectionService vision, IAppLogger logger)
{
    public async Task<bool> ExecuteStepAsync(string serial, string workflowId, WorkflowStep step, Func<WorkflowStepExecution, Task> publish, CancellationToken ct)
    {
        await publish(new WorkflowStepExecution { DeviceSerial = serial, WorkflowId = workflowId, StepId = step.Id, Status = WorkflowStepStatus.Running });

        try
        {
            var ok = step.Type switch
            {
                WorkflowStepType.Tap => (await adb.TapAsync(serial, Int(step, "x"), Int(step, "y"), ct)).Success,
                WorkflowStepType.Swipe => (await adb.SwipeAsync(serial, Int(step, "x1"), Int(step, "y1"), Int(step, "x2"), Int(step, "y2"), Int(step, "durationMs", 350), ct)).Success,
                WorkflowStepType.Input => (await adb.InputTextAsync(serial, Str(step, "text"), ct)).Success,
                WorkflowStepType.OcrSearch => (await ocr.ClickTextAsync(serial, Str(step, "text"), ct)).Success,
                WorkflowStepType.ImageDetect => (await vision.ClickDetectedImageAsync(serial, Str(step, "template"), Double(step, "threshold", 0.85), ct)).Success,
                WorkflowStepType.Wait => await WaitConditionAsync(step, ct),
                WorkflowStepType.Delay => await DelayAsync(step, ct),
                WorkflowStepType.Condition => EvaluateCondition(step),
                _ => false
            };

            await publish(new WorkflowStepExecution { DeviceSerial = serial, WorkflowId = workflowId, StepId = step.Id, Status = ok ? WorkflowStepStatus.Succeeded : WorkflowStepStatus.Failed, Message = ok ? "OK" : "Step returned false" });
            if (ok && step.DelayAfterMs > 0) await Task.Delay(step.DelayAfterMs, ct);
            return ok;
        }
        catch (Exception ex)
        {
            logger.Error($"[{serial}] step {step.Id} exception: {ex.Message}");
            await publish(new WorkflowStepExecution { DeviceSerial = serial, WorkflowId = workflowId, StepId = step.Id, Status = WorkflowStepStatus.Failed, Message = ex.Message });
            return false;
        }
    }

    private static Task<bool> DelayAsync(WorkflowStep step, CancellationToken ct) => Task.Delay(Int(step, "ms", 500), ct).ContinueWith(_ => true, ct);
    private static Task<bool> WaitConditionAsync(WorkflowStep step, CancellationToken ct) => Task.Delay(Int(step, "timeoutMs", 1000), ct).ContinueWith(_ => true, ct);
    private static bool EvaluateCondition(WorkflowStep step) => string.Equals(Str(step, "value", "true"), "true", StringComparison.OrdinalIgnoreCase);
    private static int Int(WorkflowStep s, string key, int d = 0) => s.Parameters.TryGetValue(key, out var v) && int.TryParse(v, out var x) ? x : d;
    private static double Double(WorkflowStep s, string key, double d = 0) => s.Parameters.TryGetValue(key, out var v) && double.TryParse(v, out var x) ? x : d;
    private static string Str(WorkflowStep s, string key, string d = "") => s.Parameters.TryGetValue(key, out var v) ? v : d;
}
