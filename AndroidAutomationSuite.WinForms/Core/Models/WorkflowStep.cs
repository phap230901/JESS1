namespace AndroidAutomationSuite.WinForms.Core.Models;

public enum WorkflowStepType
{
    Tap,
    Swipe,
    Input,
    OcrSearch,
    ImageDetect,
    Wait,
    Delay,
    Condition
}

public enum WorkflowStepStatus { Pending, Running, Succeeded, Failed, Skipped, Retrying }

public sealed class WorkflowStep
{
    public required string Id { get; init; }
    public required WorkflowStepType Type { get; init; }
    public string? Name { get; init; }
    public int RetryCount { get; init; } = 2;
    public int DelayAfterMs { get; init; } = 300;
    public Dictionary<string, string> Parameters { get; init; } = [];
}

public sealed class WorkflowDefinition
{
    public required string WorkflowId { get; init; }
    public required IReadOnlyList<WorkflowStep> Steps { get; init; }
}

public sealed class WorkflowStepExecution
{
    public required string DeviceSerial { get; init; }
    public required string WorkflowId { get; init; }
    public required string StepId { get; init; }
    public required WorkflowStepStatus Status { get; init; }
    public string? Message { get; init; }
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}
