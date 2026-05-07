# AndroidAutomationSuite (.NET 8 WinForms)

## Architecture
- **UI Layer**: WinForms `MainForm`, DataGridView device manager, tab-driven operator console.
- **Application Layer**: Managers + workflow automation orchestration.
- **Core Layer**: Interfaces and domain models.
- **Infrastructure Layer**: ADB, OCR, OpenCV, SQLite, logging adapters.

## Folder Structure
- `AndroidAutomationSuite.WinForms/Core`
- `AndroidAutomationSuite.WinForms/Application`
- `AndroidAutomationSuite.WinForms/Infrastructure`
- `AndroidAutomationSuite.WinForms/UI`
- `AndroidAutomationSuite.WinForms/Config`

## Key Modules
- `DeviceManager`: discovers and binds ADB devices.
- `AdbService`: async wrapper for tap/swipe/input/screenshot/open/clear/install.
- `AutomationEngine`: workflow steps with retry/delay/logging.
- `TesseractOcrService`: OCR text extraction.
- `OpenCvImageDetectionService`: template matching and click-point output.
- `SqliteDatabaseService`: bootstrap tables (`devices`, `logs`, `workflows`, `profiles`).

## Build
1. Install .NET 8 SDK and Visual Studio 2022+ with WinForms workload.
2. Ensure `adb` is in PATH.
3. Restore packages:
   - `dotnet restore AndroidAutomationSuite.sln`
4. Build:
   - `dotnet build AndroidAutomationSuite.sln -c Release`

## NuGet Packages
- CommunityToolkit.Mvvm
- Dapper
- Microsoft.Data.Sqlite
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Hosting
- OpenCvSharp4
- OpenCvSharp4.runtime.win
- Tesseract
- Serilog
