Imports System.Net.Http
Imports System.Text
Imports System.Threading.Tasks
Imports System.Configuration
Imports System.IO
Imports Newtonsoft.Json.Linq

''' <summary>
''' Creates Redmine tickets via REST API
''' </summary>
Public Class RedmineTicketCreator
    Private ReadOnly httpClient As HttpClient
    Private ReadOnly redmineClient As RedmineClient
    Private ReadOnly projectId As String

    Public Sub New(client As RedmineClient)
        Me.redmineClient = client
        Me.httpClient = client.GetHttpClient()
        Me.projectId = ConfigurationManager.AppSettings("RedmineProjectId")
        If String.IsNullOrEmpty(projectId) Then
            projectId = "g-support" ' Default project
        End If
    End Sub

    ''' <summary>
    ''' Creates a new Redmine ticket from XML data
    ''' </summary>
    Public Async Function CreateTicketAsync(ticketData As TicketXmlData, xmlFilePath As String) As Task(Of String)
        Try
            ' Bypass SSL certificate validation
            System.Net.ServicePointManager.ServerCertificateValidationCallback = Function(sender, certificate, chain, sslPolicyErrors) True
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12
            
            Logger.WriteLog("Creating Redmine ticket: " & ticketData.Subject)

            ' Ensure we're logged in
            Await redmineClient.EnsureLoggedInAsync()

            ' Build JSON payload
            Dim issueJson As New JObject()
            Dim issue As New JObject()

            ' Basic fields
            Dim projectIdInt As Integer
            If Not Integer.TryParse(projectId, projectIdInt) Then
                projectIdInt = 29 ' Default to 29 if parsing fails
            End If
            
            issue.Add("project_id", projectIdInt)
            issue.Add("subject", ticketData.Subject)
            issue.Add("tracker_id", 35) ' G-Support tracker
            issue.Add("status_id", 37) ' New ticket status
            issue.Add("priority_id", GetPriorityId(ticketData.Priority))
            issue.Add("assigned_to_id", 102) ' Assign to g-duc
            issue.Add("description", BuildDescription(ticketData))

            ' Due date if provided
            If Not String.IsNullOrEmpty(ticketData.DueDate) Then
                issue.Add("due_date", ticketData.DueDate)
            End If

            ' Estimated hours
            If Not String.IsNullOrEmpty(ticketData.EstimateTime) Then
                Dim hours As Decimal
                If Decimal.TryParse(ticketData.EstimateTime, hours) Then
                    issue.Add("estimated_hours", hours)
                End If
            End If

            ' Custom fields
            Dim customFields As New JArray()

            ' cf_139: Reception Date
            If Not String.IsNullOrEmpty(ticketData.ReceiptionDate) Then
                customFields.Add(New JObject(New JProperty("id", 139), New JProperty("value", ticketData.ReceiptionDate)))
            End If

            ' cf_140: Requester
            If Not String.IsNullOrEmpty(ticketData.Requester) Then
                customFields.Add(New JObject(New JProperty("id", 140), New JProperty("value", ticketData.Requester)))
            End If

            ' cf_141: Email Title
            If Not String.IsNullOrEmpty(ticketData.EmailTitle) Then
                customFields.Add(New JObject(New JProperty("id", 141), New JProperty("value", ticketData.EmailTitle)))
            End If

            ' cf_147: Email Time
            If Not String.IsNullOrEmpty(ticketData.EmailTime) Then
                customFields.Add(New JObject(New JProperty("id", 147), New JProperty("value", ticketData.EmailTime)))
            End If

            ' cf_115: Qty
            If Not String.IsNullOrEmpty(ticketData.Qty) Then
                customFields.Add(New JObject(New JProperty("id", 115), New JProperty("value", ticketData.Qty)))
            End If

            ' cf_117: Registration Date
            If Not String.IsNullOrEmpty(ticketData.RegistDate) Then
                customFields.Add(New JObject(New JProperty("id", 117), New JProperty("value", ticketData.RegistDate)))
            End If

            ' cf_136: Registered Person
            If Not String.IsNullOrEmpty(ticketData.RegistPerson) Then
                customFields.Add(New JObject(New JProperty("id", 136), New JProperty("value", ticketData.RegistPerson)))
            End If

            ' cf_116: Change the Subject
            If Not String.IsNullOrEmpty(ticketData.ChangtheSubject) Then
                customFields.Add(New JObject(New JProperty("id", 116), New JProperty("value", ticketData.ChangtheSubject)))
            End If

            ' cf_131: Detail
            If Not String.IsNullOrEmpty(ticketData.Detail) Then
                customFields.Add(New JObject(New JProperty("id", 131), New JProperty("value", ticketData.Detail)))
            End If

            ' cf_130: Item Type
            If Not String.IsNullOrEmpty(ticketData.ItemType) Then
                customFields.Add(New JObject(New JProperty("id", 130), New JProperty("value", ticketData.ItemType)))
            End If

            ' cf_124: Update Folder (always "Dummy")
            customFields.Add(New JObject(New JProperty("id", 124), New JProperty("value", "Dummy")))

            ' cf_134: Test Server & cf_135: Production Server
            ' Conditional based on Subject prefix
            Dim testServerId As String
            Dim productionServerId As String

            If Not String.IsNullOrEmpty(ticketData.Subject) Then
                If ticketData.Subject.StartsWith("SH2") Then
                    testServerId = "CL272"
                    productionServerId = "CL322"
                ElseIf ticketData.Subject.StartsWith("JP") Then
                    testServerId = "CL266"
                    productionServerId = "CL325"
                Else ' NT or other
                    testServerId = "CL271"
                    productionServerId = "CL323"
                End If
            Else
                ' Default to NT
                testServerId = "CL271"
                productionServerId = "CL323"
            End If

            customFields.Add(New JObject(New JProperty("id", 134), New JProperty("value", testServerId)))
            customFields.Add(New JObject(New JProperty("id", 135), New JProperty("value", productionServerId)))

            issue.Add("custom_fields", customFields)
            issueJson.Add("issue", issue)

            ' Send POST request
            Dim baseUrl = redmineClient.GetBaseUrl()
            Dim apiUrl = baseUrl & "/issues.json"

            ' Log the JSON payload for debugging
            Logger.WriteLog("API URL: " & apiUrl)
            Logger.WriteLog("JSON Payload: " & issueJson.ToString())

            Dim content As New StringContent(issueJson.ToString(), Encoding.UTF8, "application/json")
            
            ' Add API key header for authentication
            Dim apiKey = ConfigurationManager.AppSettings("RedmineApiKey")
            If Not String.IsNullOrEmpty(apiKey) Then
                httpClient.DefaultRequestHeaders.Remove("X-Redmine-API-Key")
                httpClient.DefaultRequestHeaders.Add("X-Redmine-API-Key", apiKey)
                Logger.WriteLog("Using API key authentication")
            End If
            
            Dim response = Await httpClient.PostAsync(apiUrl, content)

            Dim responseText = Await response.Content.ReadAsStringAsync()

            If response.IsSuccessStatusCode Then
                ' Parse response to get ticket ID
                Dim responseJson = JObject.Parse(responseText)
                Dim ticketId = responseJson("issue")("id").ToString()
                Logger.WriteLog("Successfully created ticket #" & ticketId)
                
                ' Update ticket with Teams URL and Update Folder
                Await UpdateTicketAfterCreation(ticketId, ticketData, xmlFilePath)
                
                Return ticketId
            Else
                Logger.WriteLog("Failed to create ticket. Status: " & response.StatusCode & ", Response: " & responseText)
                Return Nothing
            End If

        Catch ex As Exception
            Logger.WriteLog("Error creating ticket: " & ex.Message)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Updates ticket after creation with Teams URL (notes) and Update Folder (cf_124)
    ''' </summary>
    Private Async Function UpdateTicketAfterCreation(ticketId As String, ticketData As TicketXmlData, xmlFilePath As String) As Task(Of Boolean)
        Try
            Logger.WriteLog("Updating ticket #" & ticketId & " with Teams URL and Update Folder")

            ' Build JSON payload for update
            Dim issueJson As New JObject()
            Dim issue As New JObject()

            ' Add Teams URL to notes (comment)
            If Not String.IsNullOrEmpty(ticketData.TeamsUrl) Then
                issue.Add("notes", ticketData.TeamsUrl)
                Logger.WriteLog("Adding Teams URL to notes")
            End If

            ' Build Update Folder path
            Dim folderPath = ConfigurationManager.AppSettings("FolderPath")
            If String.IsNullOrEmpty(folderPath) Then
                folderPath = "\\172.27.0.223\情報システム\マスタ更新履歴\"
            End If

            Dim updateFolder = MakeDataFolder(folderPath, ticketId, ticketData)
            
            ' Add Update Folder to custom fields
            Dim customFields As New JArray()
            customFields.Add(New JObject(New JProperty("id", 124), New JProperty("value", updateFolder)))
            issue.Add("custom_fields", customFields)
            
            Logger.WriteLog("Setting Update Folder: " & updateFolder)

            issueJson.Add("issue", issue)

            ' Send PUT request
            Dim baseUrl = redmineClient.GetBaseUrl()
            Dim apiUrl = baseUrl & "/issues/" & ticketId & ".json"

            Logger.WriteLog("Update API URL: " & apiUrl)
            Logger.WriteLog("Update JSON Payload: " & issueJson.ToString())

            Dim content As New StringContent(issueJson.ToString(), Encoding.UTF8, "application/json")
            
            ' API key header already set in CreateTicketAsync
            Dim response = Await httpClient.PutAsync(apiUrl, content)

            Dim responseText = Await response.Content.ReadAsStringAsync()

            If response.IsSuccessStatusCode Then
                Logger.WriteLog("Successfully updated ticket #" & ticketId & " with Teams URL and Update Folder")
                
                ' Copy files from FolderNo to both Update Folder and Master Folder
                If Not String.IsNullOrEmpty(ticketData.FolderNo) Then
                    Dim xmlDirectory As String = Path.GetDirectoryName(xmlFilePath)
                    Dim sourceFolderPath As String = Path.Combine(xmlDirectory, ticketData.FolderNo)
                    
                    ' Copy to Update Folder (network share)
                    CopyFilesToUpdateFolder(sourceFolderPath, updateFolder)
                    
                    ' Copy to Master Folder (local backup)
#If DEBUG Then
                    Dim masterFolderBase = ConfigurationManager.AppSettings("MasterFolder_Debug")
#Else
                    Dim masterFolderBase = ConfigurationManager.AppSettings("MasterFolder_Release")
#End If
                    If Not String.IsNullOrEmpty(masterFolderBase) Then
                        ' Use flat folder structure for Master Folder (useDateStructure = False)
                        Dim masterFolder = MakeDataFolder(masterFolderBase, ticketId, ticketData, False)
                        CopyFilesToUpdateFolder(sourceFolderPath, masterFolder)
                    End If
                End If
                
                Return True
            Else
                Logger.WriteLog("Failed to update ticket after creation. Status: " & response.StatusCode & ", Response: " & responseText)
                Return False
            End If

        Catch ex As Exception
            Logger.WriteLog("Error updating ticket after creation: " & ex.Message)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Makes data folder path based on ticket info
    ''' Format with date: FolderPath\yyyy\yyyyMM\yyyyMMdd\NoXXXXX[Subject] [Qty]Qty
    ''' Format without date: FolderPath\NoXXXXX[Subject] [Qty]Qty
    ''' </summary>
    Private Function MakeDataFolder(folderPath As String, ticketId As String, ticketData As TicketXmlData, Optional useDateStructure As Boolean = True) As String
        Dim result As String = ""
        Dim now As DateTime = DateTime.Now
        
        ' Extract subject without prefix (remove first 2 characters like "NT ", "SH ", "JP ")
        Dim subjectWithoutPrefix As String = ""
        If Not String.IsNullOrEmpty(ticketData.Subject) AndAlso ticketData.Subject.Length > 2 Then
            subjectWithoutPrefix = ticketData.Subject.Substring(2)
            ' Limit to 100 characters
            If subjectWithoutPrefix.Length > 100 Then
                subjectWithoutPrefix = subjectWithoutPrefix.Substring(0, 100)
            End If
        End If
        
        Dim ticketFolderName As String = "No" & ticketId & subjectWithoutPrefix & " " & ticketData.Qty & "Qty"
        
        If useDateStructure Then
            result = Path.Combine(folderPath, now.ToString("yyyy"), now.ToString("yyyyMM"), now.ToString("yyyyMMdd"), ticketFolderName)
        Else
            result = Path.Combine(folderPath, ticketFolderName)
        End If
        
        Return result
    End Function

    ''' <summary>
    ''' Copies all files from source folder (FolderNo) to destination folder (Update Folder)
    ''' </summary>
    Private Sub CopyFilesToUpdateFolder(sourceFolderPath As String, destinationFolderPath As String)
        Try
            Logger.WriteLog("Starting file backup from: " & sourceFolderPath)
            Logger.WriteLog("Destination folder: " & destinationFolderPath)

            ' Check if source folder exists
            If Not Directory.Exists(sourceFolderPath) Then
                Logger.WriteLog("Source folder does not exist: " & sourceFolderPath)
                Return
            End If

            ' Create destination folder if it doesn't exist
            If Not Directory.Exists(destinationFolderPath) Then
                Directory.CreateDirectory(destinationFolderPath)
                Logger.WriteLog("Created destination folder: " & destinationFolderPath)
            End If

            ' Get all files from source folder
            Dim files = Directory.GetFiles(sourceFolderPath)
            
            If files.Length = 0 Then
                Logger.WriteLog("No files found in source folder")
                Return
            End If

            Logger.WriteLog("Found " & files.Length & " file(s) to copy")

            ' Copy each file
            Dim successCount As Integer = 0
            Dim failCount As Integer = 0

            For Each sourceFile In files
                Try
                    Dim fileName = Path.GetFileName(sourceFile)
                    Dim destFile = Path.Combine(destinationFolderPath, fileName)

                    ' Copy file (overwrite if exists)
                    File.Copy(sourceFile, destFile, True)
                    
                    Logger.WriteLog("Copied file: " & fileName)
                    successCount += 1

                Catch fileEx As Exception
                    Logger.WriteLog("Failed to copy file: " & Path.GetFileName(sourceFile) & " - " & fileEx.Message)
                    failCount += 1
                End Try
            Next

            Logger.WriteLog("File backup completed: " & successCount & " succeeded, " & failCount & " failed")

        Catch ex As Exception
            Logger.WriteLog("Error copying files to update folder: " & ex.Message)
        End Try
    End Sub


    ''' <summary>
    ''' Updates an existing Redmine ticket (status and assignee)
    ''' </summary>
    Public Async Function UpdateTicketAsync(ticketData As TicketXmlData) As Task(Of Boolean)
        Try
            ' Bypass SSL certificate validation
            System.Net.ServicePointManager.ServerCertificateValidationCallback = Function(sender, certificate, chain, sslPolicyErrors) True
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12
            
            Logger.WriteLog("Updating Redmine ticket #" & ticketData.TicketNo)

            ' Ensure we're logged in
            Await redmineClient.EnsureLoggedInAsync()

            ' First, GET the existing ticket to preserve custom fields
            Dim baseUrl = redmineClient.GetBaseUrl()
            Dim getUrl = baseUrl & "/issues/" & ticketData.TicketNo & ".json"
            
            ' Add API key header for GET request
            Dim apiKey = ConfigurationManager.AppSettings("RedmineApiKey")
            If Not String.IsNullOrEmpty(apiKey) Then
                httpClient.DefaultRequestHeaders.Remove("X-Redmine-API-Key")
                httpClient.DefaultRequestHeaders.Add("X-Redmine-API-Key", apiKey)
            End If
            
            Logger.WriteLog("Fetching existing ticket data from: " & getUrl)
            Dim getResponse = Await httpClient.GetAsync(getUrl)
            
            If Not getResponse.IsSuccessStatusCode Then
                Logger.WriteLog("Failed to fetch existing ticket. Status: " & getResponse.StatusCode)
                Return False
            End If
            
            Dim existingTicketJson = Await getResponse.Content.ReadAsStringAsync()
            Dim existingTicket = JObject.Parse(existingTicketJson)
            
            ' Build JSON payload for update
            Dim issueJson As New JObject()
            Dim issue As New JObject()

            ' Map Status to status_id
            Dim statusId = GetStatusId(ticketData.Status)
            If statusId > 0 Then
                issue.Add("status_id", statusId)
                Logger.WriteLog("Setting status to: " & ticketData.Status & " (ID: " & statusId & ")")
            End If

            ' Map Assign to assigned_to_id
            Dim assignedToId = GetAssignedToId(ticketData.Assign)
            If assignedToId > 0 Then
                issue.Add("assigned_to_id", assignedToId)
                Logger.WriteLog("Setting assignee to: " & ticketData.Assign & " (ID: " & assignedToId & ")")
            End If

            ' Map Priority to priority_id
            If Not String.IsNullOrEmpty(ticketData.Priority) Then
                Dim priorityId = GetPriorityId(ticketData.Priority)
                If priorityId > 0 Then
                    issue.Add("priority_id", priorityId)
                    Logger.WriteLog("Setting priority to: " & ticketData.Priority & " (ID: " & priorityId & ")")
                End If
            End If

            ' Note: Do not send custom_fields in update request to avoid validation errors
            ' Redmine API validates all required custom fields even if not included in request
            ' Web UI doesn't have this issue
            '
            ' Preserve existing custom fields
            'If existingTicket("issue")("custom_fields") IsNot Nothing Then
            '    Dim existingCustomFields = DirectCast(existingTicket("issue")("custom_fields"), JArray)
            '    Dim customFieldsToSend As New JArray()
            '    
            '    For Each field As JObject In existingCustomFields
            '        Dim fieldId = field("id").ToString()
            '        Dim fieldValue = If(field("value") IsNot Nothing, field("value").ToString(), "")
            '        
            '        ' Only preserve fields with non-empty values to avoid validation errors
            '        If Not String.IsNullOrEmpty(fieldValue) Then
            '            customFieldsToSend.Add(New JObject(
            '                New JProperty("id", Integer.Parse(fieldId)),
            '                New JProperty("value", fieldValue)
            '            ))
            '        End If
            '    Next
            '    
            '    If customFieldsToSend.Count > 0 Then
            '        issue.Add("custom_fields", customFieldsToSend)
            '        Logger.WriteLog("Preserving " & customFieldsToSend.Count & " custom fields with values from existing ticket")
            '    End If
            'End If

            issueJson.Add("issue", issue)

            ' Send PUT request
            Dim apiUrl = baseUrl & "/issues/" & ticketData.TicketNo & ".json"

            ' Log the JSON payload for debugging
            Logger.WriteLog("API URL: " & apiUrl)
            Logger.WriteLog("JSON Payload: " & issueJson.ToString())

            Dim content As New StringContent(issueJson.ToString(), Encoding.UTF8, "application/json")
            
            ' API key header already set above
            Logger.WriteLog("Using API key authentication")
            
            Dim response = Await httpClient.PutAsync(apiUrl, content)

            Dim responseText = Await response.Content.ReadAsStringAsync()

            If response.IsSuccessStatusCode Then
                Logger.WriteLog("Successfully updated ticket #" & ticketData.TicketNo)

                ' Check if status is "Close" to log time
                If ticketData.Status = "Close" Then
                    Dim estimateTimeStr As String = ""
                    ' Get estimated hours from existing ticket
                    If existingTicket("issue")("estimated_hours") IsNot Nothing Then
                        estimateTimeStr = existingTicket("issue")("estimated_hours").ToString()
                    End If

                    If Not String.IsNullOrEmpty(estimateTimeStr) Then
                        Logger.WriteLog("Closing ticket with Estimated Hours: " & estimateTimeStr)
                        
                        ' Calculate time for Test (Activity 26)
                        Dim timeTest As Double = GetTime(estimateTimeStr, "Test")
                        Await LogTimeEntryAsync(ticketData.TicketNo, timeTest, 26, "Registration（Test）")

                        ' Calculate time for Production (Activity 27)
                        Dim timeProd As Double = GetTime(estimateTimeStr, "Production") 
                        Await LogTimeEntryAsync(ticketData.TicketNo, timeProd, 27, "Registration（Production）")
                    Else
                        Logger.WriteLog("Estimated hours not found, skipping time entry.")
                    End If
                End If

                Return True
            Else
                Logger.WriteLog("Failed to update ticket. Status: " & response.StatusCode & ", Response: " & responseText)
                Return False
            End If

        Catch ex As Exception
            Logger.WriteLog("Error updating ticket: " & ex.Message)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Logs time entry for a ticket
    ''' </summary>
    Private Async Function LogTimeEntryAsync(ticketId As String, hours As Double, activityId As Integer, comments As String) As Task
        Try
            Logger.WriteLog("Logging " & hours & " hours for ticket #" & ticketId & " (Activity: " & activityId & ")")
            
            Dim timeEntryJson As New JObject()
            Dim timeEntry As New JObject()
            
            timeEntry.Add("issue_id", ticketId)
            timeEntry.Add("hours", hours)
            timeEntry.Add("activity_id", activityId)
            timeEntry.Add("spent_on", DateTime.Now.ToString("yyyy-MM-dd"))
            timeEntry.Add("comments", comments)
            
            timeEntryJson.Add("time_entry", timeEntry)
            
            Dim baseUrl = redmineClient.GetBaseUrl()
            Dim apiUrl = baseUrl & "/time_entries.json"
            
            Dim content As New StringContent(timeEntryJson.ToString(), Encoding.UTF8, "application/json")
            
            ' Ensure API key is set
             Dim apiKey = ConfigurationManager.AppSettings("RedmineApiKey")
            If Not String.IsNullOrEmpty(apiKey) Then
                httpClient.DefaultRequestHeaders.Remove("X-Redmine-API-Key")
                httpClient.DefaultRequestHeaders.Add("X-Redmine-API-Key", apiKey)
            End If

            Dim response = Await httpClient.PostAsync(apiUrl, content)
            Dim responseText = Await response.Content.ReadAsStringAsync()
            
            If response.IsSuccessStatusCode Then
                Logger.WriteLog("Successfully logged time entry.")
            Else
                Logger.WriteLog("Failed to log time entry. Status: " & response.StatusCode & ", Response: " & responseText)
            End If
            
        Catch ex As Exception
             Logger.WriteLog("Error logging time entry: " & ex.Message)
        End Try
    End Function

    ''' <summary>
    ''' Calculates time split based on server name (Test/Production)
    ''' Logic from legacy app
    ''' </summary>
    Private Function GetTime(ByVal EstimateTime As String, ByVal ServerName As String) As Double
        Dim result As Double = 0.25

        Dim total As Double = GetEstimateTime(EstimateTime)

        If total <= 0 Then Return 0 ' Handle invalid estimate

        If total = 0.5 Then
             If ServerName = "Test" Then
                Return 0.25
             Else
                Return 0.25
             End If
        Else
            ' Calculate 75% for Test, rounded to 0.25
            While result < total * 0.75
                result = result + 0.25
            End While

            If ServerName = "Test" Then
                Return result
            Else
                Return total - result
            End If
        End If
    End Function

    Private Function GetEstimateTime(ByVal EstimateTime As String) As Double
        Dim d As Double
        If Double.TryParse(EstimateTime, d) Then
            Return d
        End If
        Return 0
    End Function

    ''' <summary>
    ''' Maps status string to Redmine status ID
    ''' </summary>
    Private Function GetStatusId(status As String) As Integer
        ' Try to parse as integer first
        Dim id As Integer
        If Integer.TryParse(status, id) Then
            Return id
        End If

        Select Case status
            Case "TestRegistered"
                Return 31
            Case "ProductionRegistered"
                Return 32
            Case "TestVerified"
                Return 33
            Case "Close"
                Return 30
            Case "Checking"
                Return 35
            Case Else
                Return 0 ' Unknown status
        End Select
    End Function

    ''' <summary>
    ''' Maps assignee string to Redmine user ID
    ''' </summary>
    Private Function GetAssignedToId(assign As String) As Integer
        Select Case assign
            Case "SRG"
                Return 177
            Case "GSupport"
                Return 102
            Case Else
                Return 0 ' Unknown assignee
        End Select
    End Function

    ''' <summary>
    ''' Maps priority string to Redmine priority ID
    ''' </summary>
    Private Function GetPriorityId(priority As String) As Integer
        Select Case priority.ToLower()
            Case "low"
                Return 1
            Case "medium", "normal", ""
                Return 2
            Case "high"
                Return 3
            Case "urgent", "immediate"
                Return 4
            Case "critical"
                Return 5
            Case Else
                Return 2 ' Default to medium
        End Select
    End Function

    ''' <summary>
    ''' Builds description from ticket data
    ''' </summary>
    Private Function BuildDescription(ticketData As TicketXmlData) As String
        If Not String.IsNullOrEmpty(ticketData.Description) Then
            Return ticketData.Description
        End If
        Return ""
    End Function
End Class
