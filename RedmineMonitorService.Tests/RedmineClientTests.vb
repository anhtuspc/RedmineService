Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Configuration
Imports System.Threading.Tasks

''' <summary>
''' Unit tests for RedmineClient
''' NOTE: These tests require VPN connection to access internal Redmine server
''' </summary>
<TestClass>
Public Class RedmineClientTests

    ''' <summary>
    ''' Tests that login functionality works correctly
    ''' </summary>
    <TestMethod>
    Public Async Function TestLogin() As Task
        ' Arrange
        Dim client As New RedmineClient()

        ' Act
        Dim result = Await client.LoginAsync()

        ' Assert
        Assert.IsTrue(result, "Login should succeed with valid credentials")

        ' Cleanup
        client.Dispose()
    End Function

    ''' <summary>
    ''' Tests that we can retrieve tickets and count is correct (expecting 3 tickets)
    ''' </summary>
    <TestMethod>
    Public Async Function TestGetTicketCount() As Task
        ' Arrange
        Dim client As New RedmineClient()

        ' Act
        Dim tickets = Await client.GetTicketsAsync()

        ' Assert
        Assert.IsNotNull(tickets, "Tickets list should not be null")
        Assert.AreEqual(3, tickets.Count, "Should retrieve exactly 3 tickets")

        ' Log ticket information for verification
        For Each ticket In tickets
            Console.WriteLine($"Ticket #{ticket.TicketId}: {ticket.Subject}")
        Next

        ' Cleanup
        client.Dispose()
    End Function

    ''' <summary>
    ''' Tests that ticket parsing extracts all required fields
    ''' </summary>
    <TestMethod>
    Public Async Function TestTicketParsing() As Task
        ' Arrange
        Dim client As New RedmineClient()

        ' Act
        Dim tickets = Await client.GetTicketsAsync()

        ' Assert
        Assert.IsTrue(tickets.Count > 0, "Should have at least one ticket")

        Dim firstTicket = tickets(0)
        Assert.IsFalse(String.IsNullOrEmpty(firstTicket.TicketId), "Ticket ID should not be empty")

        ' Log all ticket details
        For Each ticket In tickets
            Console.WriteLine("=== Ticket Details ===")
            Console.WriteLine($"ID: {ticket.TicketId}")
            Console.WriteLine($"Reception Date: {ticket.ReceptionDate}")
            Console.WriteLine($"Email Title: {ticket.EmailTitle}")
            Console.WriteLine($"Requester: {ticket.Requester}")
            Console.WriteLine($"Status: {ticket.Status}")
            Console.WriteLine($"Priority: {ticket.Priority}")
            Console.WriteLine($"Subject: {ticket.Subject}")
            Console.WriteLine($"Qty: {ticket.Qty}")
            Console.WriteLine($"Updated: {ticket.Updated}")
            Console.WriteLine()
        Next

        ' Cleanup
        client.Dispose()
    End Function

    ''' <summary>
    ''' Tests HTML report generation
    ''' </summary>
    <TestMethod>
    Public Async Function TestHtmlReportGeneration() As Task
        ' Arrange
        Dim client As New RedmineClient()
        Dim tickets = Await client.GetTicketsAsync()

        ' Act
        HtmlReportGenerator.GenerateReport(tickets)

        ' Assert
        Dim outputPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "output.html")

        Assert.IsTrue(System.IO.File.Exists(outputPath), "output.html should be created")

        Dim htmlContent = System.IO.File.ReadAllText(outputPath)
        Assert.IsTrue(htmlContent.Contains("Redmine Tickets Report"), "HTML should contain report title")
        Assert.IsTrue(htmlContent.Contains("<table"), "HTML should contain a table")

        Console.WriteLine($"HTML report generated at: {outputPath}")

        ' Cleanup
        client.Dispose()
    End Function

    ''' <summary>
    ''' Tests logger functionality
    ''' </summary>
    <TestMethod>
    Public Sub TestLogger()
        ' Arrange
        Dim testMessage = "Test log message at " & DateTime.Now.ToString()

        ' Act
        Logger.WriteLog(testMessage)

        ' Assert
        Dim logPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "log.txt")

        Assert.IsTrue(System.IO.File.Exists(logPath), "log.txt should be created")

        Dim logContent = System.IO.File.ReadAllText(logPath)
        Assert.IsTrue(logContent.Contains(testMessage), "Log should contain test message")

        Console.WriteLine($"Log file location: {logPath}")
    End Sub
End Class
