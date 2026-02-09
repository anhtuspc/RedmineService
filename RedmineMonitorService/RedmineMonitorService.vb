Imports System.ServiceProcess
Imports System.Timers
Imports System.Configuration

''' <summary>
''' Windows Service that monitors Redmine tickets
''' </summary>
Public Class RedmineMonitorService
    Inherits ServiceBase

    Private WithEvents monitorTimer As Timer
    Private redmineClient As RedmineClient
    Private xmlFolderMonitor As XmlFolderMonitor

    Public Sub New()
        MyBase.New()
        Me.ServiceName = "AR_Redmine"
        Me.CanStop = True
        Me.CanPauseAndContinue = False
        Me.AutoLog = True
    End Sub

    ''' <summary>
    ''' Called when the service starts
    ''' </summary>
    Protected Overrides Sub OnStart(args As String())
        Try
            Logger.WriteLog("=== Redmine Monitor Service Starting ===")

            ' Initialize Redmine client
            redmineClient = New RedmineClient()

            ' Get timer interval from config (default to 1 minute)
            Dim intervalMinutes As Integer = 1
            Dim configInterval = ConfigurationManager.AppSettings("TimerIntervalMinutes")
            If Not String.IsNullOrEmpty(configInterval) Then
                Integer.TryParse(configInterval, intervalMinutes)
            End If

            ' Setup timer
            monitorTimer = New Timer()
            monitorTimer.Interval = intervalMinutes * 60 * 1000 ' Convert minutes to milliseconds
            monitorTimer.Enabled = True
            monitorTimer.Start()

            Logger.WriteLog("Service started successfully. Timer interval: " & intervalMinutes & " minute(s)")

            ' Initialize XML folder monitor
            Dim monitorFolder = ConfigurationManager.AppSettings("MonitorFolder")
            Dim backupFolder = ConfigurationManager.AppSettings("BackupFolder")
            
            If Not String.IsNullOrEmpty(monitorFolder) AndAlso Not String.IsNullOrEmpty(backupFolder) Then
                xmlFolderMonitor = New XmlFolderMonitor(redmineClient, monitorFolder, backupFolder)
                xmlFolderMonitor.StartMonitoring()
                Logger.WriteLog("XML folder monitoring started")
            Else
                Logger.WriteLog("XML folder monitoring disabled (MonitorFolder or BackupFolder not configured)")
            End If

            ' Run immediately on start
            MonitorTickets()

        Catch ex As Exception
            Logger.WriteLog("Error in OnStart: " & ex.Message & vbCrLf & ex.StackTrace)
            Throw
        End Try
    End Sub

    ''' <summary>
    ''' Called when the service stops
    ''' </summary>
    Protected Overrides Sub OnStop()
        Try
            Logger.WriteLog("=== Redmine Monitor Service Stopping ===")

            If monitorTimer IsNot Nothing Then
                monitorTimer.Stop()
                monitorTimer.Dispose()
                monitorTimer = Nothing
            End If

            If xmlFolderMonitor IsNot Nothing Then
                xmlFolderMonitor.StopMonitoring()
                xmlFolderMonitor = Nothing
            End If

            If redmineClient IsNot Nothing Then
                redmineClient.Dispose()
                redmineClient = Nothing
            End If

            Logger.WriteLog("Service stopped successfully")

        Catch ex As Exception
            Logger.WriteLog("Error in OnStop: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Timer elapsed event handler
    ''' </summary>
    Private Sub OnTimerElapsed(sender As Object, e As ElapsedEventArgs) Handles monitorTimer.Elapsed
        MonitorTickets()
    End Sub

    ''' <summary>
    ''' Main monitoring logic
    ''' </summary>
    Private Sub MonitorTickets()
        Try
            Logger.WriteLog("--- Starting ticket monitoring cycle ---")

            ' Fetch tickets asynchronously
            Dim tickets = redmineClient.GetTicketsAsync().Result

            If tickets IsNot Nothing AndAlso tickets.Count > 0 Then
                Logger.WriteLog("Retrieved " & tickets.Count & " ticket(s)")

                ' Log ticket details
                For Each ticket In tickets
                    Logger.WriteLog(String.Format("Ticket #{0}: {1} - Status: {2}, Priority: {3}, Requester: {4}, PIC: {5}",
                                                ticket.TicketId,
                                                ticket.Subject,
                                                ticket.Status,
                                                ticket.Priority,
                                                ticket.Requester,
                                                ticket.PIC))
                Next

                ' Generate HTML report
                HtmlReportGenerator.GenerateReport(tickets)
            Else
                Logger.WriteLog("No tickets retrieved")
            End If

            Logger.WriteLog("--- Ticket monitoring cycle completed ---")

        Catch ex As Exception
            ' Log exception but don't crash the service
            Logger.WriteLog("ERROR in MonitorTickets: " & ex.Message & vbCrLf & ex.StackTrace)
        End Try
    End Sub

    ''' <summary>
    ''' Main entry point for the service
    ''' </summary>
    Shared Sub Main(args As String())
        ' Check if running in console mode (for debugging)
        If Environment.UserInteractive Then
            Console.WriteLine("Running in console mode for debugging...")
            
            ' Check for test argument
            Dim isTestMode As Boolean = args IsNot Nothing AndAlso args.Length > 0 AndAlso args(0).ToLower() = "/test"
            
            If isTestMode Then
                Console.WriteLine("Test mode: Running service logic once...")
            Else
                Console.WriteLine("Press any key to start service logic...")
                Try
                    Console.ReadKey()
                Catch ex As Exception
                    ' If ReadKey fails (e.g., in PowerShell), continue anyway
                    Console.WriteLine("Starting automatically...")
                    System.Threading.Thread.Sleep(1000)
                End Try
            End If

            Dim service As New RedmineMonitorService()
            service.OnStart(Nothing)

            If isTestMode Then
                ' In test mode, wait for one cycle then exit
                Console.WriteLine("Waiting for monitoring cycle to complete...")
                System.Threading.Thread.Sleep(10000) ' Wait 10 seconds
                Console.WriteLine("Test completed. Stopping service...")
            Else
                Console.WriteLine("Service started. Press any key to stop...")
                Try
                    Console.ReadKey()
                Catch ex As Exception
                    ' If ReadKey fails, wait for Ctrl+C
                    Console.WriteLine("Press Ctrl+C to stop...")
                    System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite)
                End Try
            End If

            service.OnStop()
        Else
            ' Running as a Windows Service
            ServiceBase.Run(New RedmineMonitorService())
        End If
    End Sub
End Class
