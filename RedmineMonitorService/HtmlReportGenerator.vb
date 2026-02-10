Imports System.IO
Imports System.Reflection
Imports System.Text
Imports System.Linq

''' <summary>
''' Generates HTML reports from ticket data
''' </summary>
Public Class HtmlReportGenerator
    ''' <summary>
    ''' Generates an HTML report and saves it to output.html
    ''' </summary>
    Public Shared Sub GenerateReport(tickets As List(Of TicketInfo), outputFolder As String)
        Try
            If String.IsNullOrEmpty(outputFolder) Then
                outputFolder = GetExecutingDirectory()
            End If
            
            ' Create directory if it doesn't exist
            If Not Directory.Exists(outputFolder) Then
                Directory.CreateDirectory(outputFolder)
            End If
            
            Dim outputPath As String = Path.Combine(outputFolder, "Output.html")
            Dim html As New StringBuilder()

            ' HTML header with styling
            html.AppendLine("<!DOCTYPE html>")
            html.AppendLine("<html lang='ja'>")
            html.AppendLine("<head>")
            html.AppendLine("    <meta charset='UTF-8'>")
            html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>")
            html.AppendLine("    <title>Redmine Tickets Report</title>")
            html.AppendLine("    <style>")
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }")
            html.AppendLine("        h1 { color: #333; }")
            html.AppendLine("        h2 { color: #555; margin-top: 30px; }")
            html.AppendLine("        .timestamp { color: #888; font-size: 0.9em; }")
            html.AppendLine("        table { width: 100%; border-collapse: collapse; background-color: white; margin-bottom: 30px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }")
            html.AppendLine("        th { background-color: #4CAF50; color: white; padding: 12px; text-align: left; }")
            html.AppendLine("        td { padding: 10px; border-bottom: 1px solid #ddd; }")
            html.AppendLine("        tr:hover { background-color: #f5f5f5; }")
            html.AppendLine("        .priority-high { color: #d32f2f; font-weight: bold; }")
            html.AppendLine("        .priority-normal { color: #1976d2; }")
            html.AppendLine("        .priority-low { color: #388e3c; }")
            html.AppendLine("        .status { padding: 4px 8px; border-radius: 4px; font-size: 0.85em; }")
            html.AppendLine("        .status-new { background-color: #e3f2fd; color: #1976d2; }")
            html.AppendLine("        .status-in-progress { background-color: #fff3e0; color: #f57c00; }")
            html.AppendLine("        .status-resolved { background-color: #e8f5e9; color: #388e3c; }")
            html.AppendLine("        .status-closed { background-color: #f5f5f5; color: #757575; }")
            html.AppendLine("    </style>")
            html.AppendLine("</head>")
            html.AppendLine("<body>")
            html.AppendLine("    <h1>Redmine Tickets Report</h1>")
            html.AppendLine("    <p class='timestamp'>Generated: " & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "</p>")

            ' Group tickets by PIC (Person In Charge)
            Dim groupedTickets = tickets.GroupBy(Function(t) If(String.IsNullOrEmpty(t.PIC), "Unknown", t.PIC))

            For Each group In groupedTickets
                html.AppendLine("    <h2>PIC: " & System.Web.HttpUtility.HtmlEncode(group.Key) & " (" & group.Count() & " tickets)</h2>")
                html.AppendLine("    <table>")
                html.AppendLine("        <thead>")
                html.AppendLine("            <tr>")
                html.AppendLine("                <th>Ticket ID</th>")
                html.AppendLine("                <th>Reception Date</th>")
                html.AppendLine("                <th>Time</th>")
                html.AppendLine("                <th>Email Title</th>")
                html.AppendLine("                <th>Requester</th>")
                html.AppendLine("                <th>Status</th>")
                html.AppendLine("                <th>Priority</th>")
                html.AppendLine("                <th>Subject</th>")
                html.AppendLine("                <th>Qty</th>")
                html.AppendLine("                <th>Updated</th>")
                html.AppendLine("            </tr>")
                html.AppendLine("        </thead>")
                html.AppendLine("        <tbody>")

                For Each ticket In group
                    html.AppendLine("            <tr>")
                    html.AppendLine("                <td>" & System.Web.HttpUtility.HtmlEncode(ticket.TicketId) & "</td>")
                    html.AppendLine("                <td>" & System.Web.HttpUtility.HtmlEncode(ticket.ReceptionDate) & "</td>")
                    html.AppendLine("                <td>" & System.Web.HttpUtility.HtmlEncode(ticket.Time) & "</td>")
                    html.AppendLine("                <td>" & System.Web.HttpUtility.HtmlEncode(ticket.EmailTitle) & "</td>")
                    html.AppendLine("                <td>" & System.Web.HttpUtility.HtmlEncode(ticket.Requester) & "</td>")
                    html.AppendLine("                <td><span class='status " & GetStatusClass(ticket.Status) & "'>" & System.Web.HttpUtility.HtmlEncode(ticket.Status) & "</span></td>")
                    html.AppendLine("                <td class='" & GetPriorityClass(ticket.Priority) & "'>" & System.Web.HttpUtility.HtmlEncode(ticket.Priority) & "</td>")
                    html.AppendLine("                <td>" & System.Web.HttpUtility.HtmlEncode(ticket.Subject) & "</td>")
                    html.AppendLine("                <td>" & System.Web.HttpUtility.HtmlEncode(ticket.Qty) & "</td>")
                    html.AppendLine("                <td>" & System.Web.HttpUtility.HtmlEncode(ticket.Updated) & "</td>")
                    html.AppendLine("            </tr>")
                Next

                html.AppendLine("        </tbody>")
                html.AppendLine("    </table>")
            Next

            html.AppendLine("</body>")
            html.AppendLine("</html>")

            ' Write to file
            File.WriteAllText(outputPath, html.ToString(), Encoding.UTF8)
            Logger.WriteLog("HTML report generated successfully: " & outputPath)

        Catch ex As Exception
            Logger.WriteLog("Error generating HTML report: " & ex.Message)
        End Try
    End Sub

    Private Shared Function GetExecutingDirectory() As String
        Return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    End Function

    Private Shared Function GetStatusClass(status As String) As String
        Dim statusLower As String = status.ToLower()
        If statusLower.Contains("new") Then
            Return "status-new"
        ElseIf statusLower.Contains("progress") OrElse statusLower.Contains("assigned") Then
            Return "status-in-progress"
        ElseIf statusLower.Contains("resolved") OrElse statusLower.Contains("feedback") Then
            Return "status-resolved"
        ElseIf statusLower.Contains("closed") Then
            Return "status-closed"
        Else
            Return ""
        End If
    End Function

    Private Shared Function GetPriorityClass(priority As String) As String
        Dim priorityLower As String = priority.ToLower()
        If priorityLower.Contains("high") OrElse priorityLower.Contains("urgent") OrElse priorityLower.Contains("immediate") Then
            Return "priority-high"
        ElseIf priorityLower.Contains("low") Then
            Return "priority-low"
        Else
            Return "priority-normal"
        End If
    End Function
End Class
