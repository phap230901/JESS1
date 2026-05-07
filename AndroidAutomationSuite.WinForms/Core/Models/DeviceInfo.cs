namespace AndroidAutomationSuite.WinForms.Core.Models;

public sealed record DeviceInfo(string Serial, string State, bool IsConnected, DateTimeOffset LastSeenUtc);
