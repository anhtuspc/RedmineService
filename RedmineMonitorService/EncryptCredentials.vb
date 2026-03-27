Imports System.Configuration

''' <summary>
''' Utility to generate encrypted credentials for App.config
''' </summary>
Module EncryptCredentials
    Sub Main()
        Console.WriteLine("=== Redmine Credentials Encryption Tool ===")
        Console.WriteLine()
        
        ' Current credentials
        Dim username As String = "g-duc"
        Dim password As String = "ABCD@123.com"
        
        ' Generate encrypted values
        EncryptionHelper.GenerateEncryptedCredentials(username, password)
        
        Console.WriteLine()
        Console.WriteLine("Press any key to exit...")
        Try
            Console.ReadKey()
        Catch
            ' Ignore if running in non-interactive mode
        End Try
    End Sub
End Module
