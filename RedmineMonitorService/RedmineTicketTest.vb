Imports System.Net.Http
Imports System.Text
Imports System.Threading.Tasks
Imports System.Configuration
Imports Newtonsoft.Json.Linq

''' <summary>
''' Test class for creating Redmine tickets
''' </summary>
Public Class RedmineTicketTest
    ''' <summary>
    ''' Creates a test ticket with sample data
    ''' </summary>
    Public Shared Async Function CreateTestTicketAsync() As Task
        Try
            ' Bypass SSL certificate validation
            System.Net.ServicePointManager.ServerCertificateValidationCallback = Function(sender, certificate, chain, sslPolicyErrors) True
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12
            
            Console.WriteLine("=== Redmine Ticket Creation Test ===")
            Console.WriteLine()

            ' Read configuration
            Dim apiKey = ConfigurationManager.AppSettings("RedmineApiKey")
            Dim projectId = ConfigurationManager.AppSettings("RedmineProjectId")

            If String.IsNullOrEmpty(apiKey) Then
                Console.WriteLine("ERROR: RedmineApiKey not found in App.config")
                Return
            End If

            If String.IsNullOrEmpty(projectId) Then
                projectId = "29" ' Default
            End If

            Console.WriteLine("API Key: " & apiKey.Substring(0, 8) & "...")
            Console.WriteLine("Project ID: " & projectId)
            Console.WriteLine()

            ' Create HTTP client
            Using httpClient As New HttpClient()
                httpClient.Timeout = TimeSpan.FromSeconds(30)
                httpClient.DefaultRequestHeaders.Add("User-Agent", "RedmineMonitorService/1.0")
                httpClient.DefaultRequestHeaders.Add("X-Redmine-API-Key", apiKey)

                ' Build test ticket JSON
                Dim issueJson As New JObject()
                Dim issue As New JObject()

                ' Parse project ID to integer
                Dim projectIdInt As Integer
                If Not Integer.TryParse(projectId, projectIdInt) Then
                    projectIdInt = 29
                End If

                ' Basic fields
                issue.Add("project_id", projectIdInt)
                issue.Add("subject", "Test Ticket - " & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                issue.Add("tracker_id", 35) ' G-Support tracker
                issue.Add("status_id", 37) ' New status
                issue.Add("priority_id", 2) ' Medium priority
                issue.Add("assigned_to_id", 102) ' Assign to g-duc
                issue.Add("description", "This is a test ticket created by RedmineMonitorService." & vbCrLf & vbCrLf & 
                                        "Test Details:" & vbCrLf & 
                                        "- Created at: " & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & vbCrLf & 
                                        "- Purpose: Testing API ticket creation" & vbCrLf & 
                                        "- Status: Automated test")

                ' Add custom fields
                Dim customFields As New JArray()
                
                ' cf_139: Reception Date
                customFields.Add(New JObject(New JProperty("id", 139), New JProperty("value", DateTime.Now.ToString("yyyy-MM-dd"))))
                
                ' cf_140: Requester
                customFields.Add(New JObject(New JProperty("id", 140), New JProperty("value", "Test User")))
                
                ' cf_141: Email Title
                customFields.Add(New JObject(New JProperty("id", 141), New JProperty("value", "Test Email Subject")))
                
                ' cf_147: Email Time
                customFields.Add(New JObject(New JProperty("id", 147), New JProperty("value", DateTime.Now.ToString("HH:mm"))))
                
                ' cf_115: Qty
                customFields.Add(New JObject(New JProperty("id", 115), New JProperty("value", "1")))

                issue.Add("custom_fields", customFields)
                issueJson.Add("issue", issue)

                ' Display JSON payload
                Console.WriteLine("JSON Payload:")
                Console.WriteLine(issueJson.ToString())
                Console.WriteLine()

                ' Send POST request
                Dim apiUrl = "https://srg-redmine-prd.internal.misumi.jp/issues.json"
                Console.WriteLine("Sending POST request to: " & apiUrl)
                Console.WriteLine()

                Dim content As New StringContent(issueJson.ToString(), Encoding.UTF8, "application/json")
                Dim response = Await httpClient.PostAsync(apiUrl, content)

                Dim responseText = Await response.Content.ReadAsStringAsync()

                Console.WriteLine("Response Status: " & response.StatusCode)
                Console.WriteLine("Response Body:")
                Console.WriteLine(responseText)
                Console.WriteLine()

                If response.IsSuccessStatusCode Then
                    ' Parse response to get ticket ID
                    Dim responseJson = JObject.Parse(responseText)
                    Dim ticketId = responseJson("issue")("id").ToString()
                    Console.WriteLine("✓ SUCCESS! Created ticket #" & ticketId)
                    Console.WriteLine("URL: https://srg-redmine-prd.internal.misumi.jp/issues/" & ticketId)
                Else
                    Console.WriteLine("✗ FAILED to create ticket")
                    Console.WriteLine("Status Code: " & response.StatusCode)
                End If

            End Using

        Catch ex As Exception
            Console.WriteLine("ERROR: " & ex.Message)
            If ex.InnerException IsNot Nothing Then
                Console.WriteLine("Inner Exception: " & ex.InnerException.Message)
            End If
            Console.WriteLine("Stack Trace: " & ex.StackTrace)
        End Try
    End Function

    ''' <summary>
    ''' Main entry point for standalone testing
    ''' </summary>
    Shared Sub Main()
        Console.WriteLine("Starting Redmine Ticket Creation Test...")
        Console.WriteLine()

        CreateTestTicketAsync().Wait()

        Console.WriteLine()
        Console.WriteLine("Press any key to exit...")
        Console.ReadKey()
    End Sub
End Class
