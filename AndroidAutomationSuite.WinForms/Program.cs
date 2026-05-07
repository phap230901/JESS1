using AndroidAutomationSuite.WinForms.Application.Managers;
using AndroidAutomationSuite.WinForms.Application.Workflows;
using AndroidAutomationSuite.WinForms.Core.Interfaces;
using AndroidAutomationSuite.WinForms.Infrastructure.ADB;
using AndroidAutomationSuite.WinForms.Infrastructure.Data;
using AndroidAutomationSuite.WinForms.Infrastructure.Logging;
using AndroidAutomationSuite.WinForms.Infrastructure.OCR;
using AndroidAutomationSuite.WinForms.Infrastructure.Vision;
using AndroidAutomationSuite.WinForms.UI.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AndroidAutomationSuite.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IAppLogger, SerilogAdapter>();
                services.AddSingleton<IAdbService, AdbService>();
                services.AddSingleton<IDeviceManager, DeviceManager>();
                services.AddSingleton<IOcrService, OcrService>();
                services.AddSingleton<IImageDetectionService, ImageDetectionService>();
                services.AddSingleton<IDatabaseService, SqliteDatabaseService>();
                services.AddSingleton<WorkflowExecutor>();
                services.AddSingleton<IAutomationEngine, WorkflowManager>();
                services.AddSingleton<MainForm>();
            })
            .Build();

        host.Services.GetRequiredService<IDatabaseService>().Initialize();
        Application.Run(host.Services.GetRequiredService<MainForm>());
    }
}
