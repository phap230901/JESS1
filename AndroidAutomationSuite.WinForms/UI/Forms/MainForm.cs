using AndroidAutomationSuite.WinForms.Core.Interfaces;
using AndroidAutomationSuite.WinForms.Core.Models;

namespace AndroidAutomationSuite.WinForms.UI.Forms;

public sealed class MainForm : Form
{
    private readonly IDeviceManager _deviceManager;
    private readonly IAdbService _adbService;
    private readonly IOcrService _ocrService;
    private readonly IImageDetectionService _imageDetectionService;
    private readonly IAutomationEngine _automation;

    private readonly DataGridView _deviceGrid = new();
    private readonly TabControl _tabs = new();
    private readonly RichTextBox _logBox = new();
    private readonly ToolStripStatusLabel _selectedDeviceLabel = new("Device: -");
    private readonly ToolStripStatusLabel _stateLabel = new("State: Idle");

    public MainForm(IDeviceManager deviceManager, IAdbService adbService, IOcrService ocrService, IImageDetectionService imageDetectionService, IAutomationEngine automation)
    {
        _deviceManager = deviceManager;
        _adbService = adbService;
        _ocrService = ocrService;
        _imageDetectionService = imageDetectionService;
        _automation = automation;

        Text = "Android Automation Suite Pro";
        MinimumSize = new Size(1280, 760);
        Width = 1600;
        Height = 920;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        BuildUi();
        _automation.StepStatusChanged += (_, e) => BeginInvoke(() => AddLog($"[{e.DeviceSerial}] {e.StepId} -> {e.Status} {e.Message}"));
    }

    private void BuildUi()
    {
        BackColor = Color.FromArgb(24, 24, 28);
        ForeColor = Color.Gainsboro;

        var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(32, 33, 38) };
        var title = new Label
        {
            Text = "ANDROID AUTOMATION SUITE",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            Dock = DockStyle.Left,
            Padding = new Padding(16, 14, 0, 0),
            Width = 420
        };
        var refreshBtn = CreatePrimaryButton("Refresh Devices");
        refreshBtn.Dock = DockStyle.Right;
        refreshBtn.Width = 170;
        refreshBtn.Margin = new Padding(10);
        refreshBtn.Click += async (_, _) => await RefreshDevicesAsync();

        header.Controls.Add(refreshBtn);
        header.Controls.Add(title);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 420,
            BackColor = Color.FromArgb(24, 24, 28),
            Panel1MinSize = 320,
            Panel2MinSize = 700
        };

        split.Panel1.Controls.Add(BuildDevicePanel());
        split.Panel2.Controls.Add(BuildMainPanel());

        var status = new StatusStrip
        {
            Dock = DockStyle.Bottom,
            BackColor = Color.FromArgb(32, 33, 38),
            ForeColor = Color.Gainsboro,
            SizingGrip = false
        };
        status.Items.Add(_selectedDeviceLabel);
        status.Items.Add(new ToolStripStatusLabel { Spring = true });
        status.Items.Add(_stateLabel);

        Controls.Add(split);
        Controls.Add(status);
        Controls.Add(header);
    }

    private Control BuildDevicePanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 35), Padding = new Padding(10) };

        var deviceTitle = new Label { Text = "Devices", Dock = DockStyle.Top, Height = 28, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 11F) };
        var statusBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
        statusBar.Controls.Add(CreateStatusPill("Online", Color.FromArgb(76, 175, 80)));
        statusBar.Controls.Add(CreateStatusPill("Offline", Color.FromArgb(244, 67, 54)));
        statusBar.Controls.Add(CreateStatusPill("Unauthorized", Color.FromArgb(255, 193, 7)));

        _deviceGrid.Dock = DockStyle.Fill;
        _deviceGrid.BackgroundColor = Color.FromArgb(35, 35, 41);
        _deviceGrid.BorderStyle = BorderStyle.None;
        _deviceGrid.EnableHeadersVisualStyles = false;
        _deviceGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 47, 54);
        _deviceGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _deviceGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _deviceGrid.DefaultCellStyle.BackColor = Color.FromArgb(35, 35, 41);
        _deviceGrid.DefaultCellStyle.ForeColor = Color.Gainsboro;
        _deviceGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(72, 88, 179);
        _deviceGrid.DefaultCellStyle.SelectionForeColor = Color.White;
        _deviceGrid.RowHeadersVisible = false;
        _deviceGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _deviceGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _deviceGrid.MultiSelect = true;
        _deviceGrid.DataSource = _deviceManager.Devices;
        _deviceGrid.SelectionChanged += (_, _) => UpdateSelectedDevice();

        panel.Controls.Add(_deviceGrid);
        panel.Controls.Add(statusBar);
        panel.Controls.Add(deviceTitle);
        return panel;
    }

    private Control BuildMainPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 24, 28), Padding = new Padding(10) };

        _tabs.Dock = DockStyle.Fill;
        _tabs.Appearance = TabAppearance.Normal;
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.ItemSize = new Size(140, 34);
        _tabs.SizeMode = TabSizeMode.Fixed;
        _tabs.DrawItem += (_, e) => DrawDarkTab(e);

        AddActionsTab();
        foreach (var tab in new[] { "Auto", "Text Search", "Restore & Reset", "Random", "Settings" })
        {
            _tabs.TabPages.Add(CreateEmptyTab(tab));
        }

        var logsPanel = new Panel { Dock = DockStyle.Bottom, Height = 220, Padding = new Padding(0, 8, 0, 0) };
        var logsTitle = new Label { Text = "Real-time Logs", Dock = DockStyle.Top, Height = 24, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 10F) };

        _logBox.Dock = DockStyle.Fill;
        _logBox.BackColor = Color.FromArgb(17, 17, 20);
        _logBox.ForeColor = Color.FromArgb(177, 232, 194);
        _logBox.BorderStyle = BorderStyle.FixedSingle;
        _logBox.ReadOnly = true;
        _logBox.Font = new Font("Consolas", 9F);

        logsPanel.Controls.Add(_logBox);
        logsPanel.Controls.Add(logsTitle);

        panel.Controls.Add(_tabs);
        panel.Controls.Add(logsPanel);
        return panel;
    }

    private void AddActionsTab()
    {
        var page = CreateEmptyTab("Actions");
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };
        string[] names = ["Capture", "Paste Data", "Paste Password", "Run One", "Run All", "Find Click", "OCR Read", "Check Device"];

        foreach (var name in names)
        {
            var btn = CreatePrimaryButton(name);
            btn.Width = 170;
            btn.Height = 42;
            btn.Click += async (_, _) => await HandleActionAsync(name);
            flow.Controls.Add(btn);
        }

        page.Controls.Add(flow);
        _tabs.TabPages.Add(page);
    }

    private async Task HandleActionAsync(string action)
    {
        var serial = GetSelectedDevice();
        if (serial is null) return;

        _stateLabel.Text = $"State: {action}";
        AddLog($"Executing {action} on {serial}");

        switch (action)
        {
            case "Capture":
                await _adbService.CaptureScreenAsync(serial, Path.Combine(AppContext.BaseDirectory, $"{serial}.png"), CancellationToken.None);
                break;
            case "OCR Read":
                var img = Path.Combine(AppContext.BaseDirectory, $"{serial}.png");
                var result = await _ocrService.DetectTextAsync(img, CancellationToken.None);
                AddLog($"OCR: {string.Join(" | ", result.Blocks.Select(b => b.Text))}");
                break;
            case "Find Click":
                await _imageDetectionService.ClickDetectedImageAsync(serial, "template.png", 0.85, CancellationToken.None);
                break;
            case "Run One":
                await _automation.EnqueueAsync(serial, new WorkflowDefinition
                {
                    WorkflowId = $"wf_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                    Steps =
                    [
                        new() { Id = "step_delay", Type = WorkflowStepType.Delay, Parameters = new Dictionary<string, string>{{"ms","500"}} },
                        new() { Id = "step_tap", Type = WorkflowStepType.Tap, Parameters = new Dictionary<string, string>{{"x","100"},{"y","100"}} }
                    ]
                }, CancellationToken.None);
                break;
            case "Check Device":
                await RefreshDevicesAsync();
                break;
        }

        _stateLabel.Text = "State: Idle";
    }

    private async Task RefreshDevicesAsync()
    {
        _stateLabel.Text = "State: Refreshing";
        await _deviceManager.RefreshDevicesAsync(CancellationToken.None);
        AddLog("Device list refreshed.");
        _stateLabel.Text = "State: Idle";
    }

    private void AddLog(string msg)
    {
        _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        _logBox.ScrollToCaret();
    }

    private void UpdateSelectedDevice()
    {
        _selectedDeviceLabel.Text = $"Device: {GetSelectedDevice() ?? "-"}";
    }

    private string? GetSelectedDevice() => _deviceGrid.SelectedRows.Count > 0 ? _deviceGrid.SelectedRows[0].Cells[nameof(DeviceInfo.Serial)]?.Value?.ToString() : null;

    private static TabPage CreateEmptyTab(string title) => new() { Text = title, BackColor = Color.FromArgb(24, 24, 28), ForeColor = Color.Gainsboro };

    private static Button CreatePrimaryButton(string text) => new()
    {
        Text = text,
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(59, 130, 246),
        ForeColor = Color.White,
        Margin = new Padding(8),
        FlatAppearance = { BorderSize = 0 }
    };

    private static Control CreateStatusPill(string text, Color dotColor)
    {
        var panel = new Panel { Width = 120, Height = 26, Margin = new Padding(3) };
        var dot = new Panel { BackColor = dotColor, Width = 10, Height = 10, Left = 8, Top = 8 };
        var label = new Label { Text = text, ForeColor = Color.Gainsboro, Left = 24, Top = 5, Width = 90 };
        panel.Controls.Add(dot);
        panel.Controls.Add(label);
        return panel;
    }

    private void DrawDarkTab(DrawItemEventArgs e)
    {
        var tab = _tabs.TabPages[e.Index];
        var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        using var back = new SolidBrush(isSelected ? Color.FromArgb(59, 130, 246) : Color.FromArgb(43, 44, 50));
        using var fore = new SolidBrush(Color.WhiteSmoke);
        e.Graphics.FillRectangle(back, e.Bounds);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString(tab.Text, Font, fore, e.Bounds, sf);
    }
}
