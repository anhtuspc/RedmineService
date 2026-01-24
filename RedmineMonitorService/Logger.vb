Imports System.IO
Imports System.Reflection

''' <summary>
''' Thread-safe logger that writes to log.txt in the same directory as the service binary
''' </summary>
Public Class Logger
    Private Shared ReadOnly lockObject As New Object()

    ''' <summary>
    ''' Writes a log message with timestamp to log.txt
    ''' </summary>
    Public Shared Sub WriteLog(message As String)
        Try
            SyncLock lockObject
                Dim logPath As String = Path.Combine(GetExecutingDirectory(), "log.txt")
                Dim timestamp As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                Dim logEntry As String = String.Format("[{0}] {1}", timestamp, message)

                File.AppendAllText(logPath, logEntry & Environment.NewLine)
            End SyncLock
        Catch ex As Exception
            ' If logging fails, write to event log as fallback
            Try
                EventLog.WriteEntry("RedmineMonitorService", "Logging error: " & ex.Message, EventLogEntryType.Error)
            Catch
                ' Silently fail if both logging mechanisms fail
            End Try
        End Try
    End Sub

    ''' <summary>
    ''' Gets the directory where the service executable is located
    ''' </summary>
    Private Shared Function GetExecutingDirectory() As String
        Return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    End Function
End Class
