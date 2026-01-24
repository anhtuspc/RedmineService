''' <summary>
''' Represents a Redmine ticket with all relevant information
''' </summary>
Public Class TicketInfo
    Public Property TicketId As String
    Public Property ReceptionDate As String
    Public Property EmailTitle As String
    Public Property Requester As String
    Public Property PIC As String ' Person In Charge (user active)
    Public Property Status As String
    Public Property Priority As String
    Public Property Subject As String
    Public Property Qty As String
    Public Property Updated As String

    Public Sub New()
        TicketId = ""
        ReceptionDate = ""
        EmailTitle = ""
        Requester = ""
        PIC = ""
        Status = ""
        Priority = ""
        Subject = ""
        Qty = ""
        Updated = ""
    End Sub
End Class
