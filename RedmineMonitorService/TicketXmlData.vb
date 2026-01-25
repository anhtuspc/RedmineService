''' <summary>
''' Represents ticket data parsed from XML file
''' </summary>
Public Class TicketXmlData
    ' Update-specific fields
    Public Property TicketNo As String
    Public Property Status As String
    Public Property Assign As String
    
    ' Create-specific fields
    Public Property UpdateStatus As String
    Public Property Subject As String
    Public Property Priority As String
    Public Property ReceiptionDate As String
    Public Property Requester As String
    Public Property EmailTitle As String
    Public Property EmailTime As String
    Public Property RegistDate As String
    Public Property RegistPerson As String
    Public Property Qty As String
    Public Property ChangtheSubject As String
    Public Property Detail As String
    Public Property ItemType As String
    Public Property DueDate As String
    Public Property FolderNo As String
    Public Property EstimateTime As String
    Public Property UpdateFolder As String
    Public Property TestServer As String
    Public Property ProductionServer As String
    Public Property TeamsUrl As String
    Public Property Description As String

    Public Sub New()
        TicketNo = ""
        Status = ""
        Assign = ""
        UpdateStatus = ""
        Subject = ""
        Priority = "medium"
        ReceiptionDate = ""
        Requester = ""
        EmailTitle = ""
        EmailTime = ""
        RegistDate = ""
        RegistPerson = ""
        Qty = ""
        ChangtheSubject = ""
        Detail = ""
        ItemType = ""
        DueDate = ""
        FolderNo = ""
        EstimateTime = ""
        UpdateFolder = ""
        TestServer = ""
        ProductionServer = ""
        TeamsUrl = ""
        Description = ""
    End Sub
End Class
