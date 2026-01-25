Imports System.IO
Imports System.Threading

''' <summary>
''' Monitors folder for new XML files and creates Redmine tickets
''' </summary>
Public Class XmlFolderMonitor
    Private WithEvents fileWatcher As FileSystemWatcher
    Private ReadOnly monitorFolder As String
    Private ReadOnly backupFolder As String
    Private ReadOnly redmineClient As RedmineClient
    Private ReadOnly ticketCreator As RedmineTicketCreator
    Private ReadOnly processingFiles As New HashSet(Of String)
    Private ReadOnly lockObject As New Object()

    Public Sub New(client As RedmineClient, monitorPath As String, backupPath As String)
        Me.redmineClient = client
        Me.monitorFolder = monitorPath
        Me.backupFolder = backupPath
        Me.ticketCreator = New RedmineTicketCreator(client)

        ' Ensure folders exist
        If Not Directory.Exists(monitorFolder) Then
            Directory.CreateDirectory(monitorFolder)
            Logger.WriteLog("Created monitor folder: " & monitorFolder)
        End If

        If Not Directory.Exists(backupFolder) Then
            Directory.CreateDirectory(backupFolder)
            Logger.WriteLog("Created backup folder: " & backupFolder)
        End If
    End Sub

    ''' <summary>
    ''' Starts monitoring the folder
    ''' </summary>
    Public Sub StartMonitoring()
        Try
            fileWatcher = New FileSystemWatcher(monitorFolder)
            fileWatcher.Filter = "*.xml"
            fileWatcher.NotifyFilter = NotifyFilters.FileName Or NotifyFilters.LastWrite
            fileWatcher.EnableRaisingEvents = True

            Logger.WriteLog("Started monitoring folder: " & monitorFolder)

            ' Process any existing XML files
            ProcessExistingFiles()

        Catch ex As Exception
            Logger.WriteLog("Error starting folder monitor: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Stops monitoring the folder
    ''' </summary>
    Public Sub StopMonitoring()
        Try
            If fileWatcher IsNot Nothing Then
                fileWatcher.EnableRaisingEvents = False
                fileWatcher.Dispose()
                fileWatcher = Nothing
            End If

            Logger.WriteLog("Stopped monitoring folder")

        Catch ex As Exception
            Logger.WriteLog("Error stopping folder monitor: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Processes existing XML files in the folder
    ''' </summary>
    Private Sub ProcessExistingFiles()
        Try
            Dim xmlFiles = Directory.GetFiles(monitorFolder, "*.xml")
            For Each filePath In xmlFiles
                ProcessXmlFileAsync(filePath).Wait()
            Next
        Catch ex As Exception
            Logger.WriteLog("Error processing existing files: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Event handler for new files
    ''' </summary>
    Private Sub OnFileCreated(sender As Object, e As FileSystemEventArgs) Handles fileWatcher.Created
        ProcessXmlFileAsync(e.FullPath).Wait()
    End Sub

    ''' <summary>
    ''' Processes a single XML file
    ''' </summary>
    Private Async Function ProcessXmlFileAsync(filePath As String) As Task
        ' Prevent duplicate processing
        SyncLock lockObject
            If processingFiles.Contains(filePath) Then
                Return
            End If
            processingFiles.Add(filePath)
        End SyncLock

        Try
            Logger.WriteLog("Processing XML file: " & filePath)

            ' Wait briefly to ensure file is fully written
            Thread.Sleep(500)

            ' Check if file still exists and is accessible
            If Not WaitForFileAccess(filePath, 3) Then
                Logger.WriteLog("Cannot access file (may be locked): " & filePath)
                SyncLock lockObject
                    processingFiles.Remove(filePath)
                End SyncLock
                Return
            End If

            ' Parse XML
            Dim ticketData = TicketXmlParser.ParseXmlFile(filePath)
            If ticketData Is Nothing Then
                Logger.WriteLog("Failed to parse XML file: " & filePath)
                MoveToBackup(filePath, ".error")
                SyncLock lockObject
                    processingFiles.Remove(filePath)
                End SyncLock
                Return
            End If

            ' Validate
            If Not TicketXmlParser.ValidateTicketData(ticketData) Then
                Logger.WriteLog("XML validation failed: " & filePath)
                MoveToBackup(filePath, ".invalid")
                SyncLock lockObject
                    processingFiles.Remove(filePath)
                End SyncLock
                Return
            End If

            ' Check UpdateStatus
            If ticketData.UpdateStatus.ToLower() = "new" Then
                ' Create new ticket
                Dim ticketId = Await ticketCreator.CreateTicketAsync(ticketData)

                If Not String.IsNullOrEmpty(ticketId) Then
                    Logger.WriteLog("Successfully created ticket #" & ticketId & " from file: " & filePath)
                    MoveToBackup(filePath, "")
                Else
                    Logger.WriteLog("Failed to create ticket from file: " & filePath)
                    ' Keep file for retry
                End If
            ElseIf ticketData.UpdateStatus.ToLower() = "update" Then
                ' Update existing ticket
                If String.IsNullOrEmpty(ticketData.TicketNo) Then
                    Logger.WriteLog("Update failed: TicketNo is required for update operation")
                    MoveToBackup(filePath, ".error")
                    SyncLock lockObject
                        processingFiles.Remove(filePath)
                    End SyncLock
                    Return
                End If

                Dim success = Await ticketCreator.UpdateTicketAsync(ticketData)

                If success Then
                    Logger.WriteLog("Successfully updated ticket #" & ticketData.TicketNo & " from file: " & filePath)
                    MoveToBackup(filePath, "")
                Else
                    Logger.WriteLog("Failed to update ticket from file: " & filePath)
                    ' Keep file for retry
                End If
            Else
                Logger.WriteLog("UpdateStatus is not 'New' or 'Update', skipping: " & ticketData.UpdateStatus)
                MoveToBackup(filePath, ".skipped")
            End If

        Catch ex As Exception
            Logger.WriteLog("Error processing XML file: " & filePath & " - " & ex.Message)
        Finally
            SyncLock lockObject
                processingFiles.Remove(filePath)
            End SyncLock
        End Try
    End Function

    ''' <summary>
    ''' Waits for file to be accessible
    ''' </summary>
    Private Function WaitForFileAccess(filePath As String, maxAttempts As Integer) As Boolean
        For i As Integer = 1 To maxAttempts
            Try
                Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None)
                    Return True
                End Using
            Catch ex As IOException
                If i < maxAttempts Then
                    Thread.Sleep(1000)
                End If
            End Try
        Next
        Return False
    End Function

    ''' <summary>
    ''' Moves file to backup folder
    ''' </summary>
    Private Sub MoveToBackup(filePath As String, suffix As String)
        Try
            Dim fileName = Path.GetFileNameWithoutExtension(filePath)
            Dim extension = Path.GetExtension(filePath)
            Dim timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss")
            Dim newFileName = fileName & "_" & timestamp & suffix & extension
            Dim destPath = Path.Combine(backupFolder, newFileName)

            File.Move(filePath, destPath)
            Logger.WriteLog("Moved file to backup: " & destPath)

        Catch ex As Exception
            Logger.WriteLog("Error moving file to backup: " & ex.Message)
        End Try
    End Sub
End Class
