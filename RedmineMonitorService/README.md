# Redmine Monitor Windows Service

A Windows Service built with VB.NET (.NET Framework 4.8) that monitors Redmine tickets, generates HTML reports, and automates ticket creation/updates via XML files.

## Features

### Core Monitoring
- **Automatic Monitoring**: Checks Redmine tickets every 1 minute (configurable)
- **API Key Authentication**: Secure authentication using Redmine API keys
- **HTML Reports**: Generates styled HTML reports grouped by requester
- **Logging**: Thread-safe logging with timestamps to `log.txt`
- **Exception Handling**: Robust error handling prevents service crashes

### XML-Based Automation (NEW)
- **Auto-Create Tickets**: Create new Redmine tickets from XML files
- **Auto-Update Tickets**: Update existing tickets with status and assignee changes
- **Custom Fields**: Full support for all custom fields (Registration date, Registered person, etc.)
- **Smart Field Preservation**: Automatically preserves existing custom fields during updates
- **Status Mapping**: Maps friendly status names to Redmine status IDs
- **Assignee Mapping**: Maps team names (SRG, GSupport) to user IDs
- **File Backup**: Processed XML files are automatically moved to backup folder with timestamps

### Security
- **Password Encryption**: AES-256 encryption for sensitive credentials
- **Encryption Utility**: Built-in tool to encrypt passwords for App.config
- **API Key Support**: Secure API key authentication (recommended)

## Project Structure

```
RedmineMonitorService/
├── RedmineMonitorService.vb      # Main service class
├── RedmineClient.vb               # HTTP client for Redmine
├── RedmineTicketCreator.vb        # XML-based ticket creation/update (NEW)
├── TicketInfo.vb                  # Ticket data model
├── Logger.vb                      # Logging utility
├── HtmlReportGenerator.vb         # HTML report generator
├── ProjectInstaller.vb            # Service installer
├── EncryptionHelper.vb            # Password encryption utility (NEW)
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
- Redmine API key (recommended for authentication)

## Configuration

Edit `App.config` to configure the service:

```xml
<appSettings>
  <!-- Redmine Connection -->
  <add key="RedmineUrl" value="https://srg-redmine-prd.internal.misumi.jp/projects/g-support/issues?query_id=227" />
  <add key="RedmineApiUrl" value="https://srg-redmine-prd.internal.misumi.jp" />
  <add key="RedmineApiKey" value="your_api_key_here" />
  <add key="RedmineProjectId" value="g-support" />
  
  <!-- Legacy Authentication (Optional - API key recommended) -->
  <add key="RedmineUsername" value="g-duc" />
  <add key="RedminePassword" value="ENCRYPTED_PASSWORD_HERE" />
  <add key="UseEncryption" value="true" />
  
  <!-- Monitoring Settings -->
  <add key="TimerIntervalMinutes" value="1" />
  
  <!-- XML Automation Folders (NEW) -->
  <add key="XmlFolderPath" value="C:\Users\Nguyen\OneDrive - MISUMI Group Inc\bk\test" />
  <add key="BackupFolderPath" value="C:\Users\Nguyen\OneDrive - MISUMI Group Inc\bk\test\bk" />
</appSettings>
```

### Configuration Options

| Setting | Description | Required |
|---------|-------------|----------|
| `RedmineUrl` | URL to Redmine query for monitoring | Yes |
| `RedmineApiUrl` | Base URL for Redmine API | Yes (for XML automation) |
| `RedmineApiKey` | Your Redmine API key | **Recommended** |
| `RedmineProjectId` | Project identifier (e.g., "g-support") | Yes (for XML automation) |
| `RedmineUsername` | Username for legacy auth | Optional |
| `RedminePassword` | Encrypted password | Optional |
| `UseEncryption` | Enable password encryption | Optional (default: false) |
| `TimerIntervalMinutes` | Monitoring interval in minutes | Yes (default: 1) |
| `XmlFolderPath` | Folder to monitor for XML files | Yes (for XML automation) |
| `BackupFolderPath` | Folder to store processed XML files | Yes (for XML automation) |

### Getting Your API Key

1. Log into Redmine
2. Go to My Account → API access key
3. Click "Show" to reveal your API key
4. Copy the key to `RedmineApiKey` in App.config

### Encrypting Passwords (Optional)

If using username/password authentication:

```powershell
# Navigate to output directory
cd RedmineMonitorService\bin\Debug

# Run encryption utility
.\RedmineMonitorService.exe /encrypt "YourPassword123"

# Copy the encrypted output to App.config
# Set UseEncryption="true"
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

### Test Mode

Run the service once without looping:

```powershell
.\RedmineMonitorService.exe /test
```

This will:
1. Process all XML files in the monitored folder
2. Run one monitoring cycle
3. Exit automatically

## XML-Based Ticket Automation

### Overview

The service monitors a configured folder for XML files and automatically:
- Creates new Redmine tickets when `UpdateStatus = "New"`
- Updates existing tickets when `UpdateStatus = "Update"`
- Preserves all existing custom fields during updates
- Moves processed files to backup folder with timestamps

### XML File Format

#### Creating a New Ticket

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Root>
  <UpdateStatus>New</UpdateStatus>
  <Subject>NT Item-Purchase Update</Subject>
  <Description>Detailed description of the issue</Description>
  <Status>Receptionist</Status>
  <Priority>medium</Priority>
  <Assign>GSupport</Assign>
  <Requester>蔡 蕾</Requester>
  
  <!-- Custom Fields -->
  <RegistrationDate>2026-01-24</RegistrationDate>
  <RegisteredPerson>グエン ドク</RegisteredPerson>
  <ChangeSubject>Item Master Update</ChangeSubject>
  <Detail>Add new item codes</Detail>
  <ItemType>Purchase</ItemType>
  <UpdateFolder>\\server\share\folder</UpdateFolder>
  <Test>Test environment details</Test>
  <Production>Production deployment plan</Production>
</Root>
```

#### Updating an Existing Ticket

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Root>
  <UpdateStatus>Update</UpdateStatus>
  <TicketNo>46837</TicketNo>
  <Status>TestRegistered</Status>
  <Assign>SRG</Assign>
</Root>
```

**Note**: When updating, only `UpdateStatus`, `TicketNo`, `Status`, and `Assign` are required. All other custom fields are automatically preserved from the existing ticket.

### XML Field Reference

| XML Element | Description | Required for Create | Required for Update |
|-------------|-------------|---------------------|---------------------|
| `UpdateStatus` | "New" or "Update" | ✅ Yes | ✅ Yes |
| `TicketNo` | Ticket number to update | ❌ No | ✅ Yes |
| `Subject` | Ticket subject/title | ✅ Yes | ❌ No |
| `Description` | Ticket description | ✅ Yes | ❌ No |
| `Status` | Status name (see mapping below) | ✅ Yes | ⚠️ Optional |
| `Priority` | "low", "medium", or "high" | ⚠️ Optional | ❌ No |
| `Assign` | Team name (see mapping below) | ⚠️ Optional | ⚠️ Optional |
| `Requester` | Requester name | ⚠️ Optional | ❌ No |
| `RegistrationDate` | Date in YYYY-MM-DD format | ✅ Yes | ❌ No |
| `RegisteredPerson` | Person who registered | ✅ Yes | ❌ No |
| `ChangeSubject` | Subject of change | ✅ Yes | ❌ No |
| `Detail` | Detailed information | ✅ Yes | ❌ No |
| `ItemType` | Type of item | ✅ Yes | ❌ No |
| `UpdateFolder` | Folder path for updates | ✅ Yes | ❌ No |
| `Test` | Test environment info | ✅ Yes | ❌ No |
| `Production` | Production info | ✅ Yes | ❌ No |

### Status Mapping

| XML Status Name | Redmine Status ID | Description |
|-----------------|-------------------|-------------|
| `Receptionist` | 5 | Initial reception status |
| `TestRegistered` | 32 | Registered for testing |
| `TestVerified` | 33 | Verified in test environment |
| `ProductionRegistered` | 34 | Registered for production |
| `SendBack` | 35 | Sent back for revision |

**Important**: Redmine has workflow validations. You cannot skip statuses. For example:
- ❌ Cannot go directly from `Receptionist` → `TestVerified`
- ✅ Must go `Receptionist` → `TestRegistered` → `TestVerified`

### Assignee Mapping

| XML Assign Name | Redmine User ID | User Name |
|-----------------|-----------------|-----------|
| `SRG` | 177 | SRG Team |
| `GSupport` | 178 | グエン ドク (G-Support) |

### Usage Workflow

1. **Create XML file** in the monitored folder (e.g., `C:\bk\test\CreateTicket.xml`)
2. **Service automatically detects** the file within 1 minute (or immediately in test mode)
3. **Ticket is created/updated** in Redmine
4. **File is moved** to backup folder with timestamp (e.g., `CreateTicket_20260124_233703.xml`)
5. **Check log.txt** for results

### Example Scenarios

#### Scenario 1: Create New Ticket

```xml
<!-- File: NewTicket.xml -->
<?xml version="1.0" encoding="UTF-8"?>
<Root>
  <UpdateStatus>New</UpdateStatus>
  <Subject>Database Schema Update</Subject>
  <Description>Add new columns to user table</Description>
  <Status>Receptionist</Status>
  <Priority>high</Priority>
  <Assign>GSupport</Assign>
  <Requester>John Doe</Requester>
  <RegistrationDate>2026-02-09</RegistrationDate>
  <RegisteredPerson>グエン ドク</RegisteredPerson>
  <ChangeSubject>User Table Schema</ChangeSubject>
  <Detail>Add email_verified and last_login columns</Detail>
  <ItemType>Database</ItemType>
  <UpdateFolder>\\db-server\schemas</UpdateFolder>
  <Test>Applied to test DB successfully</Test>
  <Production>Pending approval</Production>
</Root>
```

**Result**: Creates ticket #46838 with all custom fields populated.

#### Scenario 2: Update Ticket Status

```xml
<!-- File: UpdateTicket46838.xml -->
<?xml version="1.0" encoding="UTF-8"?>
<Root>
  <UpdateStatus>Update</UpdateStatus>
  <TicketNo>46838</TicketNo>
  <Status>TestRegistered</Status>
</Root>
```

**Result**: Updates ticket #46838 status to "TestRegistered", preserves all other fields.

#### Scenario 3: Change Assignee

```xml
<!-- File: ReassignTicket46838.xml -->
<?xml version="1.0" encoding="UTF-8"?>
<Root>
  <UpdateStatus>Update</UpdateStatus>
  <TicketNo>46838</TicketNo>
  <Assign>SRG</Assign>
</Root>
```

**Result**: Reassigns ticket #46838 to SRG team, preserves status and all other fields.

#### Scenario 4: Update Status and Assignee

```xml
<!-- File: UpdateTicket46838_Full.xml -->
<?xml version="1.0" encoding="UTF-8"?>
<Root>
  <UpdateStatus>Update</UpdateStatus>
  <TicketNo>46838</TicketNo>
  <Status>TestVerified</Status>
  <Assign>SRG</Assign>
</Root>
```

**Result**: Updates both status and assignee, preserves all custom fields.



All output files are created in the same directory as the service executable:

- **log.txt**: Timestamped log entries (includes XML processing logs)
- **output.html**: HTML report with ticket information

### Sample Log Entry

```
[2026-02-09 00:00:00] === Redmine Monitor Service Starting ===
[2026-02-09 00:00:00] Service started successfully. Timer interval: 1 minute(s)
[2026-02-09 00:00:00] XML folder monitoring started
[2026-02-09 00:00:01] --- Starting ticket monitoring cycle ---
[2026-02-09 00:00:01] Using API key authentication
[2026-02-09 00:00:02] Fetching tickets from: https://srg-redmine-prd.internal.misumi.jp/...
[2026-02-09 00:00:03] Successfully fetched 4 tickets
[2026-02-09 00:00:03] Ticket #46837: NT Item-Purchase Update - Status: Receptionist, Priority: medium, Requester: 蔡 蕾, PIC: グエン ドク
[2026-02-09 00:00:04] HTML report generated successfully: C:\...\output.html
[2026-02-09 00:00:04] --- Ticket monitoring cycle completed ---

[2026-02-09 00:01:00] --- Starting XML file processing ---
[2026-02-09 00:01:00] Found XML file: C:\bk\test\CreateNewTicket.xml
[2026-02-09 00:01:00] Creating new ticket from XML...
[2026-02-09 00:01:01] Using API key authentication
[2026-02-09 00:01:02] Successfully created ticket #46838
[2026-02-09 00:01:02] Successfully created ticket #46838 from file: C:\bk\test\CreateNewTicket.xml
[2026-02-09 00:01:02] Moved file to backup: C:\bk\test\bk\CreateNewTicket_20260209_000102.xml

[2026-02-09 00:02:00] Found XML file: C:\bk\test\UpdateTicket46838.xml
[2026-02-09 00:02:00] Updating existing ticket #46838...
[2026-02-09 00:02:00] Fetching existing ticket #46838 to preserve custom fields...
[2026-02-09 00:02:01] Preserving custom field: Registration date = 2026-02-09
[2026-02-09 00:02:01] Preserving custom field: Registered person = グエン ドク
[2026-02-09 00:02:01] Using API key authentication
[2026-02-09 00:02:02] Successfully updated ticket #46838
[2026-02-09 00:02:02] Successfully updated ticket #46838 from file: C:\bk\test\UpdateTicket46838.xml
[2026-02-09 00:02:02] Moved file to backup: C:\bk\test\bk\UpdateTicket46838_20260209_000202.xml
```

## Output Files



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

1. Check Windows Event Log (Event Viewer → Windows Logs → Application)
2. Verify VPN connection to internal network
3. Check `log.txt` for error messages
4. Ensure App.config is in the same directory as the executable
5. Verify API key is valid

### Authentication Fails

1. **Using API Key (Recommended)**:
   - Verify API key in App.config is correct
   - Check if API key is still active in Redmine (My Account → API access key)
   - Ensure no extra spaces in the API key value

2. **Using Username/Password**:
   - Verify credentials in App.config
   - If using encryption, ensure `UseEncryption="true"` is set
   - Test URL in browser while connected to VPN

### No Tickets Retrieved

1. Verify query URL is correct
2. Check if you have permissions to view the query
3. Ensure API key has access to the project
4. Review log.txt for detailed error messages

### XML Files Not Processing

1. **File Not Detected**:
   - Verify `XmlFolderPath` in App.config is correct
   - Check folder permissions (service needs read/write access)
   - Ensure file extension is `.xml` (case-sensitive)
   - Wait for next monitoring cycle (1 minute) or use `/test` mode

2. **Ticket Creation Fails**:
   - Check log.txt for error messages
   - Verify all required custom fields are present
   - Ensure XML is well-formed (valid UTF-8 encoding)
   - Check API key has permission to create tickets

3. **Ticket Update Fails**:
   - Verify ticket number exists
   - Check status workflow (cannot skip statuses)
   - Ensure assignee has permission to be assigned
   - Review error message in log.txt (422 = validation error)

### Common Error Messages

| Error | Cause | Solution |
|-------|-------|----------|
| `422 - Registration date cannot be blank` | Missing required custom field | Ensure all required fields are in XML |
| `422 - Status transition not allowed` | Invalid workflow transition | Follow correct status sequence |
| `401 - Unauthorized` | Invalid API key | Check API key in App.config |
| `404 - Not Found` | Ticket doesn't exist | Verify ticket number |
| `File not found` | XML folder path incorrect | Check `XmlFolderPath` in App.config |

### HTML Report Not Generated

1. Check write permissions on output directory
2. Review log.txt for errors
3. Verify tickets were successfully retrieved
4. Ensure System.Web assembly is available

### Debugging Tips

1. **Use Test Mode**: Run `.\RedmineMonitorService.exe /test` to process files immediately
2. **Check Logs**: Always review `log.txt` for detailed error messages
3. **Validate XML**: Ensure XML files are well-formed before placing in monitored folder
4. **Test in Browser**: Verify Redmine access and permissions in web browser first
5. **Check Workflow**: Understand Redmine status workflow before updating tickets

## Security Considerations

⚠️ **SECURITY BEST PRACTICES**:

1. **Use API Key Authentication** (Recommended):
   - More secure than username/password
   - Can be revoked without changing password
   - Easier to manage in multi-environment setups

2. **If Using Password Authentication**:
   - Enable encryption with `UseEncryption="true"`
   - Use the built-in encryption utility: `.\RedmineMonitorService.exe /encrypt "password"`
   - Never commit unencrypted passwords to version control

3. **Additional Security Measures**:
   - Restrict file system permissions on App.config
   - Use Windows Credential Manager for sensitive data
   - Rotate API keys regularly
   - Monitor log files for unauthorized access attempts

## License

Internal use only for MISUMI projects.

## Support

For issues or questions, contact the development team.
