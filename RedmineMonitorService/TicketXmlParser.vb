Imports System.Xml
Imports System.IO

''' <summary>
''' Parses XML files to extract ticket data
''' </summary>
Public Class TicketXmlParser
    ''' <summary>
    ''' Parses XML file and returns ticket data
    ''' </summary>
    Public Shared Function ParseXmlFile(filePath As String) As TicketXmlData
        Dim ticketData As New TicketXmlData()

        Try
            Dim doc As New XmlDocument()
            doc.Load(filePath)

            Dim root As XmlNode = doc.SelectSingleNode("Root")
            If root Is Nothing Then
                Logger.WriteLog("XML file does not contain Root element: " & filePath)
                Return Nothing
            End If

            ' Parse all fields
            ticketData.UpdateStatus = GetNodeValue(root, "UpdateStatus")
            ticketData.TicketNo = GetNodeValue(root, "TicketNo")
            ticketData.Status = GetNodeValue(root, "Status")
            ticketData.Assign = GetNodeValue(root, "Assign")
            ticketData.Subject = GetNodeValue(root, "Subject")
            ticketData.Priority = GetNodeValue(root, "Priority")
            ticketData.ReceiptionDate = GetNodeValue(root, "ReceiptionDate")
            ticketData.Requester = GetNodeValue(root, "Requester")
            ticketData.EmailTitle = GetNodeValue(root, "EmailTitle")
            ticketData.EmailTime = GetNodeValue(root, "EmailTime")
            ticketData.RegistDate = GetNodeValue(root, "RegistDate")
            ticketData.RegistPerson = GetNodeValue(root, "RegistPerson")
            ticketData.Qty = GetNodeValue(root, "Qty")
            ticketData.ChangtheSubject = GetNodeValue(root, "ChangtheSubject")
            ticketData.Detail = GetNodeValue(root, "Detail")
            ticketData.ItemType = GetNodeValue(root, "ItemType")
            ticketData.DueDate = GetNodeValue(root, "DueDate")
            ticketData.FolderNo = GetNodeValue(root, "FolderNo")
            ticketData.EstimateTime = GetNodeValue(root, "EstimateTime")
            ticketData.UpdateFolder = GetNodeValue(root, "UpdateFolder")
            ticketData.TestServer = GetNodeValue(root, "TestServer")
            ticketData.ProductionServer = GetNodeValue(root, "ProductionServer")
            ticketData.TeamsUrl = GetNodeValue(root, "TeamsUrl")
            ticketData.Description = GetNodeValue(root, "Description")

            Logger.WriteLog("Successfully parsed XML file: " & filePath)
            Return ticketData

        Catch ex As Exception
            Logger.WriteLog("Error parsing XML file: " & filePath & " - " & ex.Message)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Gets value of XML node, returns empty string if not found
    ''' </summary>
    Private Shared Function GetNodeValue(parent As XmlNode, nodeName As String) As String
        Dim node As XmlNode = parent.SelectSingleNode(nodeName)
        If node IsNot Nothing Then
            Return node.InnerText.Trim()
        End If
        Return ""
    End Function

    ''' <summary>
    ''' Validates that required fields are present
    ''' </summary>
    Public Shared Function ValidateTicketData(ticketData As TicketXmlData) As Boolean
        If String.IsNullOrEmpty(ticketData.UpdateStatus) Then
            Logger.WriteLog("Validation failed: UpdateStatus is required")
            Return False
        End If

        ' For New tickets, Subject is required
        If ticketData.UpdateStatus.ToLower() = "new" Then
            If String.IsNullOrEmpty(ticketData.Subject) Then
                Logger.WriteLog("Validation failed: Subject is required for new tickets")
                Return False
            End If
        End If

        ' For Update operations, TicketNo is required
        If ticketData.UpdateStatus.ToLower() = "update" Then
            If String.IsNullOrEmpty(ticketData.TicketNo) Then
                Logger.WriteLog("Validation failed: TicketNo is required for update operations")
                Return False
            End If
        End If

        Return True
    End Function
End Class
