Imports System.Net
Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Configuration

''' <summary>
''' HTTP client for interacting with Redmine
''' </summary>
Public Class RedmineClient
    Private ReadOnly httpClient As HttpClient
    Private ReadOnly cookieContainer As CookieContainer
    Private ReadOnly baseUrl As String
    Private ReadOnly username As String
    Private ReadOnly password As String
    Private isLoggedIn As Boolean = False
    Private csrfToken As String = ""

    Public Sub New()
        ' Read configuration
        baseUrl = ConfigurationManager.AppSettings("RedmineUrl")
        
        ' Read and decrypt credentials
        Dim encryptedUsername = ConfigurationManager.AppSettings("RedmineUsername")
        Dim encryptedPassword = ConfigurationManager.AppSettings("RedminePassword")
        
        Try
            username = EncryptionHelper.Decrypt(encryptedUsername)
            password = EncryptionHelper.Decrypt(encryptedPassword)
        Catch ex As Exception
            ' If decryption fails, assume values are plain text (for backward compatibility)
            username = encryptedUsername
            password = encryptedPassword
            Logger.WriteLog("Warning: Using plain text credentials. Consider encrypting them.")
        End Try

        ' Setup HTTP client with cookie support
        cookieContainer = New CookieContainer()
        Dim handler As New HttpClientHandler() With {
            .CookieContainer = cookieContainer,
            .UseCookies = True,
            .AllowAutoRedirect = True
        }

        httpClient = New HttpClient(handler) With {
            .Timeout = TimeSpan.FromSeconds(30)
        }

        ' Set headers to mimic a browser
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")
        httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "ja,en-US;q=0.9,en;q=0.8")
    End Sub

    ''' <summary>
    ''' Logs in to Redmine
    ''' </summary>
    Public Async Function LoginAsync() As Task(Of Boolean)
        Try
            Logger.WriteLog("Attempting to login to Redmine...")

            ' First, get the login page to obtain CSRF token
            Dim loginPageUrl As String = GetBaseUrl() & "/login"
            Dim loginPageResponse = Await httpClient.GetAsync(loginPageUrl)
            Dim loginPageHtml = Await loginPageResponse.Content.ReadAsStringAsync()

            ' Extract CSRF token (authenticity_token)
            Dim csrfToken As String = ExtractCsrfToken(loginPageHtml)

            ' Prepare login form data
            Dim formData As New Dictionary(Of String, String) From {
                {"username", username},
                {"password", password},
                {"login", "Login"}
            }

            If Not String.IsNullOrEmpty(csrfToken) Then
                formData.Add("authenticity_token", csrfToken)
            End If

            Dim content As New FormUrlEncodedContent(formData)

            ' Submit login form
            Dim loginResponse = Await httpClient.PostAsync(loginPageUrl, content)
            Dim responseHtml = Await loginResponse.Content.ReadAsStringAsync()

            ' Check if login was successful (look for logged-in indicators)
            If Not responseHtml.Contains("login") OrElse responseHtml.Contains("my/page") OrElse responseHtml.Contains("logged-user") Then
                isLoggedIn = True
                ' Store CSRF token for future API calls
                Me.csrfToken = csrfToken
                Logger.WriteLog("Login successful")
                Return True
            Else
                Logger.WriteLog("Login failed - invalid credentials or unexpected response")
                Return False
            End If

        Catch ex As Exception
            Logger.WriteLog("Login error: " & ex.Message)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Gets tickets from the configured query URL
    ''' </summary>
    Public Async Function GetTicketsAsync() As Task(Of List(Of TicketInfo))
        Try
            Logger.WriteLog("Fetching tickets from: " & baseUrl)

            ' Try to get the page
            Dim response = Await httpClient.GetAsync(baseUrl)
            Dim html = Await response.Content.ReadAsStringAsync()

            ' Check if we were redirected to login page
            If html.Contains("<input") AndAlso html.Contains("password") AndAlso html.Contains("login") Then
                Logger.WriteLog("Session expired, logging in...")
                Dim loginSuccess = Await LoginAsync()

                If Not loginSuccess Then
                    Logger.WriteLog("Failed to login, cannot fetch tickets")
                    Return New List(Of TicketInfo)()
                End If

                ' Retry fetching tickets after login
                response = Await httpClient.GetAsync(baseUrl)
                html = Await response.Content.ReadAsStringAsync()
            End If

            ' Parse tickets from HTML
            Dim tickets = ParseTickets(html)
            Logger.WriteLog("Successfully fetched " & tickets.Count & " tickets")

            Return tickets

        Catch ex As Exception
            Logger.WriteLog("Error fetching tickets: " & ex.Message)
            Return New List(Of TicketInfo)()
        End Try
    End Function

    ''' <summary>
    ''' Parses ticket information from HTML
    ''' </summary>
    Public Function ParseTickets(html As String) As List(Of TicketInfo)
        Dim tickets As New List(Of TicketInfo)()

        Try
            ' Find the issues table
            Dim tableMatch = Regex.Match(html, "<table[^>]*class=""[^""]*list[^""]*issues[^""]*""[^>]*>(.*?)</table>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            If Not tableMatch.Success Then
                ' Try alternative table pattern
                tableMatch = Regex.Match(html, "<table[^>]*class=""[^""]*issues[^""]*""[^>]*>(.*?)</table>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            End If

            If Not tableMatch.Success Then
                Logger.WriteLog("Could not find issues table in HTML")
                Return tickets
            End If

            Dim tableContent = tableMatch.Groups(1).Value

            ' Extract all table rows (both group headers and issue rows)
            Dim allRows = Regex.Matches(tableContent, "<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)

            Dim currentPIC As String = ""

            For Each rowMatch As Match In allRows
                Dim rowHtml = rowMatch.Value
                Dim rowContent = rowMatch.Groups(1).Value

                ' Check if this is a group header row
                If rowHtml.Contains("class=""group") OrElse rowHtml.Contains("class='group") Then
                    ' Extract PIC from group header
                    Dim picMatch = Regex.Match(rowContent, "<a[^>]*class=""user active""[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase)
                    If picMatch.Success Then
                        currentPIC = picMatch.Groups(1).Value.Trim()
                        Logger.WriteLog("Found PIC in group header: " & currentPIC)
                    End If
                    Continue For
                End If

                ' Check if this is an issue row
                If Not (rowHtml.Contains("class=""") AndAlso rowHtml.Contains("issue")) Then
                    Continue For
                End If

                ' Parse issue row
                Dim ticket As New TicketInfo()
                ticket.PIC = currentPIC ' Assign current PIC from group header

                ' Extract ticket ID
                Dim idMatch = Regex.Match(rowContent, "issues/(\d+)", RegexOptions.IgnoreCase)
                If idMatch.Success Then
                    ticket.TicketId = idMatch.Groups(1).Value
                End If

                ' Extract all td cells
                Dim cells = Regex.Matches(rowContent, "<td[^>]*>(.*?)</td>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)

                ' Parse each cell based on class attribute
                For Each cellMatch As Match In cells
                    Dim cellHtml = cellMatch.Value
                    Dim cellContent = StripHtml(cellMatch.Groups(1).Value).Trim()

                    ' Extract Requester - class cf_140 string
                    If cellHtml.Contains("cf_140") AndAlso cellHtml.Contains("string") Then
                        ticket.Requester = cellContent
                    End If

                    ' Extract Reception Date - class cf_139 date
                    If cellHtml.Contains("cf_139") AndAlso cellHtml.Contains("date") Then
                        ticket.ReceptionDate = cellContent
                    End If

                    ' Extract Time - class cf_147
                    If cellHtml.Contains("cf_147") Then
                        ticket.Time = cellContent
                    End If

                    ' Extract Email Title - class cf_141 string
                    If cellHtml.Contains("cf_141") AndAlso cellHtml.Contains("string") Then
                        ticket.EmailTitle = cellContent
                    End If

                    ' Extract Qty - class cf_115 int
                    If cellHtml.Contains("cf_115") AndAlso cellHtml.Contains("int") Then
                        ticket.Qty = cellContent
                    End If

                    ' Extract Subject
                    If cellHtml.Contains("subject") Then
                        ticket.Subject = cellContent
                    End If

                    ' Extract Status
                    If cellHtml.Contains("status") Then
                        ticket.Status = cellContent
                    End If

                    ' Extract Priority
                    If cellHtml.Contains("priority") Then
                        ticket.Priority = cellContent
                    End If

                    ' Extract Updated
                    If cellHtml.Contains("updated_on") OrElse (cellHtml.Contains("updated") AndAlso Not cellHtml.Contains("cf_")) Then
                        ticket.Updated = cellContent
                    End If
                Next

                ' Add ticket if it has at least an ID
                If Not String.IsNullOrEmpty(ticket.TicketId) Then
                    tickets.Add(ticket)
                End If
            Next

        Catch ex As Exception
            Logger.WriteLog("Error parsing tickets: " & ex.Message)
        End Try

        Return tickets
    End Function

    ''' <summary>
    ''' Extracts CSRF token from HTML
    ''' </summary>
    Private Function ExtractCsrfToken(html As String) As String
        Dim match = Regex.Match(html, "name=""authenticity_token""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase)
        If match.Success Then
            Return match.Groups(1).Value
        End If

        ' Try alternative pattern
        match = Regex.Match(html, "name='authenticity_token'[^>]*value='([^']+)'", RegexOptions.IgnoreCase)
        If match.Success Then
            Return match.Groups(1).Value
        End If

        Return ""
    End Function

    ''' <summary>
    ''' Strips HTML tags from text
    ''' </summary>
    Private Function StripHtml(html As String) As String
        If String.IsNullOrEmpty(html) Then
            Return ""
        End If

        ' Remove HTML tags
        Dim result = Regex.Replace(html, "<[^>]+>", "")
        ' Decode HTML entities
        result = System.Web.HttpUtility.HtmlDecode(result)
        ' Remove extra whitespace
        result = Regex.Replace(result, "\s+", " ")
        Return result.Trim()
    End Function

    ''' <summary>
    ''' Gets base URL from configured URL
    ''' </summary>
    Public Function GetBaseUrl() As String
        Dim uri As New Uri(baseUrl)
        Return uri.Scheme & "://" & uri.Host
    End Function

    ''' <summary>
    ''' Ensures user is logged in, logs in if necessary
    ''' </summary>
    Public Async Function EnsureLoggedInAsync() As Task
        If Not isLoggedIn Then
            Await LoginAsync()
        End If
    End Function

    ''' <summary>
    ''' Gets the HTTP client for API calls
    ''' </summary>
    Public Function GetHttpClient() As HttpClient
        Return httpClient
    End Function

    ''' <summary>
    ''' Gets the current CSRF token
    ''' </summary>
    Public Function GetCsrfToken() As String
        Return csrfToken
    End Function

    Public Sub Dispose()
        If httpClient IsNot Nothing Then
            httpClient.Dispose()
        End If
    End Sub
End Class

