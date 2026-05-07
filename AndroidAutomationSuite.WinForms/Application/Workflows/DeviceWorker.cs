using System.Threading.Channels;
using AndroidAutomationSuite.WinForms.Core.Models;

namespace AndroidAutomationSuite.WinForms.Application.Workflows;

public sealed class DeviceWorker
{
    private readonly Channel<WorkflowDefinition> _queue = Channel.CreateUnbounded<WorkflowDefinition>();
    private readonly SemaphoreSlim _pauseGate = new(1, 1);
    private volatile bool _stopping;

    public string Serial { get; }

    public DeviceWorker(string serial) => Serial = serial;
    public ValueTask QueueAsync(WorkflowDefinition workflow, CancellationToken ct) => _queue.Writer.WriteAsync(workflow, ct);
    public async Task PauseAsync() { await _pauseGate.WaitAsync(); }
    public Task ResumeAsync() { if (_pauseGate.CurrentCount == 0) _pauseGate.Release(); return Task.CompletedTask; }
    public Task StopAsync() { _stopping = true; _queue.Writer.TryComplete(); return Task.CompletedTask; }

    public async Task RunAsync(Func<string, WorkflowDefinition, CancellationToken, Task> handler, CancellationToken ct)
    {
        await foreach (var workflow in _queue.Reader.ReadAllAsync(ct))
        {
            if (_stopping) break;
            await _pauseGate.WaitAsync(ct);
            _pauseGate.Release();
            await handler(Serial, workflow, ct);
        }
    }
}
