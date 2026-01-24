# Redmine Monitor Windows Service

A Windows Service built with VB.NET (.NET Framework 4.8) that monitors Redmine tickets and generates HTML reports.

## Features

- **Automatic Monitoring**: Checks Redmine tickets every 1 minute (configurable)
- **Authentication**: Handles login with session management
- **HTML Reports**: Generates styled HTML reports grouped by requester
- **Logging**: Thread-safe logging with timestamps to `log.txt`
- **Exception Handling**: Robust error handling prevents service crashes
- **Unit Tests**: Comprehensive tests for login, ticket retrieval, and parsing

## Project Structure

```
RedmineMonitorService/
├── RedmineMonitorService.vb      # Main service class
├── RedmineClient.vb               # HTTP client for Redmine
├── TicketInfo.vb                  # Ticket data model
├── Logger.vb                      # Logging utility
├── HtmlReportGenerator.vb         # HTML report generator
├── ProjectInstaller.vb            # Service installer
├── App.config                     # Configuration file
└── RedmineMonitorService.vbproj   # Project file

RedmineMonitorService.Tests/
├── RedmineClientTests.vb          # Unit tests
├── App.config                     # Test configuration
├── packages.config                # NuGet packages
└── RedmineMonitorService.Tests.vbproj
```

## Prerequisites

- .NET Framework 4.8
- Visual Studio 2019 or later (or MSBuild)
- VPN access to `srg-redmine-prd.internal.misumi.jp`
- Administrator privileges (for service installation)

## Configuration

Edit `App.config` to configure the service:

```xml
<appSettings>
  <add key="RedmineUrl" value="https://srg-redmine-prd.internal.misumi.jp/projects/g-support/issues?query_id=227" />
  <add key="RedmineUsername" value="g-duc" />
  <add key="RedminePassword" value="ABCD@123.com" />
  <add key="TimerIntervalMinutes" value="1" />
</appSettings>
```

## Building the Service

### Using Visual Studio

1. Open `RedmineMonitorService.sln`
2. Build > Build Solution (or press `Ctrl+Shift+B`)
3. Output will be in `RedmineMonitorService\bin\Debug\` or `RedmineMonitorService\bin\Release\`

### Using MSBuild (Command Line)

```powershell
# Debug build
msbuild RedmineMonitorService.sln /p:Configuration=Debug

# Release build
msbuild RedmineMonitorService.sln /p:Configuration=Release
```

## Running Tests

**IMPORTANT**: Tests require VPN connection to access the internal Redmine server.

### Using Visual Studio

1. Open Test Explorer (Test > Test Explorer)
2. Click "Run All Tests"

### Using Command Line

```powershell
# Restore NuGet packages first
nuget restore RedmineMonitorService.sln

# Build the solution
msbuild RedmineMonitorService.sln /p:Configuration=Debug

# Run tests using VSTest
vstest.console.exe RedmineMonitorService.Tests\bin\Debug\RedmineMonitorService.Tests.dll
```

### Expected Test Results

- **TestLogin**: Verifies successful authentication
- **TestGetTicketCount**: Confirms 3 tickets are retrieved
- **TestTicketParsing**: Validates ticket data extraction
- **TestHtmlReportGeneration**: Ensures HTML report is created
- **TestLogger**: Verifies logging functionality

## Installing the Service

### Method 1: Using InstallUtil (Recommended)

```powershell
# Navigate to .NET Framework directory
cd C:\Windows\Microsoft.NET\Framework64\v4.0.30319

# Install the service (run as Administrator)
.\installutil.exe "C:\path\to\RedmineMonitorService\bin\Release\RedmineMonitorService.exe"
```

### Method 2: Using SC Command

```powershell
# Create service (run as Administrator)
sc create RedmineMonitorService binPath= "C:\path\to\RedmineMonitorService\bin\Release\RedmineMonitorService.exe" start= auto

# Set description
sc description RedmineMonitorService "Monitors Redmine tickets and generates HTML reports"
```

## Starting the Service

### Using Services Console

1. Press `Win+R`, type `services.msc`, press Enter
2. Find "Redmine Monitor Service"
3. Right-click > Start

### Using Command Line

```powershell
# Start service
net start RedmineMonitorService

# Stop service
net stop RedmineMonitorService

# Check service status
sc query RedmineMonitorService
```

## Debugging the Service

The service can run in console mode for debugging:

```powershell
# Navigate to output directory
cd RedmineMonitorService\bin\Debug

# Run directly (not as a service)
.\RedmineMonitorService.exe
```

Press any key to start the service logic, then press any key again to stop.

## Output Files

All output files are created in the same directory as the service executable:

- **log.txt**: Timestamped log entries
- **output.html**: HTML report with ticket information

### Sample Log Entry

```
[2026-01-24 13:05:00] === Redmine Monitor Service Starting ===
[2026-01-24 13:05:00] Service started successfully. Timer interval: 1 minute(s)
[2026-01-24 13:05:01] --- Starting ticket monitoring cycle ---
[2026-01-24 13:05:02] Attempting to login to Redmine...
[2026-01-24 13:05:03] Login successful
[2026-01-24 13:05:04] Fetching tickets from: https://srg-redmine-prd.internal.misumi.jp/...
[2026-01-24 13:05:05] Successfully fetched 3 tickets
[2026-01-24 13:05:05] Ticket #12345: Sample Issue - Status: New, Priority: High, Requester: John Doe
[2026-01-24 13:05:06] HTML report generated successfully: C:\...\output.html
[2026-01-24 13:05:06] --- Ticket monitoring cycle completed ---
```

## Uninstalling the Service

### Using InstallUtil

```powershell
# Navigate to .NET Framework directory
cd C:\Windows\Microsoft.NET\Framework64\v4.0.30319

# Uninstall the service (run as Administrator)
.\installutil.exe /u "C:\path\to\RedmineMonitorService\bin\Release\RedmineMonitorService.exe"
```

### Using SC Command

```powershell
# Stop service first
net stop RedmineMonitorService

# Delete service
sc delete RedmineMonitorService
```

## Troubleshooting

### Service Won't Start

1. Check Windows Event Log (Event Viewer > Windows Logs > Application)
2. Verify VPN connection to internal network
3. Check `log.txt` for error messages
4. Ensure App.config is in the same directory as the executable

### Login Fails

1. Verify credentials in App.config
2. Test URL in browser while connected to VPN
3. Check if Redmine login page structure has changed
4. Review log.txt for detailed error messages

### No Tickets Retrieved

1. Verify query URL is correct
2. Check if you have permissions to view the query
3. Ensure HTML parsing logic matches current Redmine structure
4. Run unit tests to isolate the issue

### HTML Report Not Generated

1. Check write permissions on output directory
2. Review log.txt for errors
3. Verify tickets were successfully retrieved
4. Ensure System.Web assembly is available

## Security Considerations

⚠️ **WARNING**: Credentials are stored in plain text in App.config. For production use, consider:

- Using Windows Credential Manager
- Encrypting configuration sections
- Using environment variables
- Implementing a secure credential store

## License

Internal use only for MISUMI projects.

## Support

For issues or questions, contact the development team.
