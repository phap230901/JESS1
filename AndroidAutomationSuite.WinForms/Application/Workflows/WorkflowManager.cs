using System.Collections.Concurrent;
using AndroidAutomationSuite.WinForms.Core.Interfaces;
using AndroidAutomationSuite.WinForms.Core.Models;

namespace AndroidAutomationSuite.WinForms.Application.Workflows;

public sealed class WorkflowManager : IAutomationEngine
{
    private readonly ConcurrentDictionary<string, DeviceWorker> _workers = new();
    private readonly ConcurrentDictionary<string, Task> _workerLoops = new();
    private readonly WorkflowExecutor _executor;
    private readonly IAppLogger _logger;
    private readonly CancellationTokenSource _globalCts = new();

    public event EventHandler<WorkflowStepExecution>? StepStatusChanged;

    public WorkflowManager(WorkflowExecutor executor, IAppLogger logger)
    {
        _executor = executor;
        _logger = logger;
    }

    public async Task EnqueueAsync(string deviceSerial, WorkflowDefinition workflow, CancellationToken ct)
    {
        var worker = _workers.GetOrAdd(deviceSerial, serial =>
        {
            var w = new DeviceWorker(serial);
            _workerLoops[serial] = Task.Run(() => w.RunAsync(ProcessWorkflowAsync, _globalCts.Token), _globalCts.Token);
            return w;
        });

        await worker.QueueAsync(workflow, ct);
        _logger.Info($"Workflow queued: {workflow.WorkflowId} -> {deviceSerial}");
    }

    public Task PauseAsync(string deviceSerial) => _workers.TryGetValue(deviceSerial, out var w) ? w.PauseAsync() : Task.CompletedTask;
    public Task ResumeAsync(string deviceSerial) => _workers.TryGetValue(deviceSerial, out var w) ? w.ResumeAsync() : Task.CompletedTask;
    public Task StopAsync(string deviceSerial) => _workers.TryGetValue(deviceSerial, out var w) ? w.StopAsync() : Task.CompletedTask;

    private async Task ProcessWorkflowAsync(string serial, WorkflowDefinition workflow, CancellationToken ct)
    {
        foreach (var step in workflow.Steps)
        {
            var attempts = Math.Max(0, step.RetryCount) + 1;
            var succeeded = false;
            for (var i = 0; i < attempts && !succeeded; i++)
            {
                if (i > 0) await PublishAsync(new WorkflowStepExecution { DeviceSerial = serial, WorkflowId = workflow.WorkflowId, StepId = step.Id, Status = WorkflowStepStatus.Retrying, Message = $"Retry {i}/{attempts - 1}" });
                succeeded = await _executor.ExecuteStepAsync(serial, workflow.WorkflowId, step, PublishAsync, ct);
            }

            if (!succeeded)
            {
                _logger.Error($"Workflow '{workflow.WorkflowId}' stopped on step '{step.Id}' for device {serial}");
                break;
            }
        }
    }

    private Task PublishAsync(WorkflowStepExecution status)
    {
        StepStatusChanged?.Invoke(this, status);
        return Task.CompletedTask;
    }
}
