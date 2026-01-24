Imports System.Net.Http
Imports System.Text
Imports System.Threading.Tasks
Imports System.Configuration
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
    Public Async Function CreateTicketAsync(ticketData As TicketXmlData) As Task(Of String)
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
            Case Else
                Return 2 ' Default to medium
        End Select
    End Function

    ''' <summary>
    ''' Builds description from ticket data
    ''' </summary>
    Private Function BuildDescription(ticketData As TicketXmlData) As String
        Dim sb As New StringBuilder()

        ' Add main description
        If Not String.IsNullOrEmpty(ticketData.Description) Then
            sb.AppendLine(ticketData.Description)
            sb.AppendLine()
        End If

        ' Add additional details
        sb.AppendLine("=== Ticket Details ===")

        If Not String.IsNullOrEmpty(ticketData.Detail) Then
            sb.AppendLine("Detail: " & ticketData.Detail)
        End If

        If Not String.IsNullOrEmpty(ticketData.ChangtheSubject) Then
            sb.AppendLine("Change Subject: " & ticketData.ChangtheSubject)
        End If

        If Not String.IsNullOrEmpty(ticketData.ItemType) Then
            sb.AppendLine("Item Type: " & ticketData.ItemType)
        End If

        If Not String.IsNullOrEmpty(ticketData.FolderNo) Then
            sb.AppendLine("Folder No: " & ticketData.FolderNo)
        End If

        If Not String.IsNullOrEmpty(ticketData.TestServer) Then
            sb.AppendLine("Test Server: " & ticketData.TestServer)
        End If

        If Not String.IsNullOrEmpty(ticketData.ProductionServer) Then
            sb.AppendLine("Production Server: " & ticketData.ProductionServer)
        End If

        If Not String.IsNullOrEmpty(ticketData.TeamsUrl) Then
            sb.AppendLine()
            sb.AppendLine("Teams URL: " & ticketData.TeamsUrl)
        End If

        If Not String.IsNullOrEmpty(ticketData.RegistPerson) Then
            sb.AppendLine()
            sb.AppendLine("Registered by: " & ticketData.RegistPerson)
        End If

        If Not String.IsNullOrEmpty(ticketData.RegistDate) Then
            sb.AppendLine("Registration Date: " & ticketData.RegistDate)
        End If

        Return sb.ToString()
    End Function
End Class
