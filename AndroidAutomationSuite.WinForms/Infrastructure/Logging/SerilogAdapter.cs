using AndroidAutomationSuite.WinForms.Core.Interfaces;
using Serilog;

namespace AndroidAutomationSuite.WinForms.Infrastructure.Logging;

public sealed class SerilogAdapter : IAppLogger
{
    private readonly ILogger _logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
    public void Info(string message) => _logger.Information(message);
    public void Error(string message) => _logger.Error(message);
}
