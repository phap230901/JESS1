using AndroidAutomationSuite.WinForms.Core.Interfaces;

namespace AndroidAutomationSuite.WinForms.Application.Managers;

public sealed class DeviceManager(IAdbService adbService) : IDeviceManager
{
    public BindingSource Devices { get; } = new();

    public async Task RefreshDevicesAsync(CancellationToken ct)
    {
        var devices = await adbService.GetDevicesAsync(ct);
        Devices.DataSource = devices;
    }
}
