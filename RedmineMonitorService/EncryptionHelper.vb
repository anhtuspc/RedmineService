Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' Simple encryption/decryption utility using AES
''' </summary>
Public Class EncryptionHelper
    Private Const EncryptionKey As String = "gsupport"
    
    ''' <summary>
    ''' Encrypts a string using AES encryption
    ''' </summary>
    Public Shared Function Encrypt(plainText As String) As String
        If String.IsNullOrEmpty(plainText) Then
            Return ""
        End If
        
        Try
            Dim key = DeriveKeyFromPassword(EncryptionKey)
            Dim iv = New Byte(15) {} ' 16 bytes for AES
            
            Using aes As Aes = Aes.Create()
                aes.Key = key
                aes.IV = iv
                aes.Mode = CipherMode.CBC
                aes.Padding = PaddingMode.PKCS7
                
                Using encryptor = aes.CreateEncryptor(aes.Key, aes.IV)
                    Dim plainBytes = Encoding.UTF8.GetBytes(plainText)
                    Dim encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length)
                    Return Convert.ToBase64String(encryptedBytes)
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception("Encryption failed: " & ex.Message, ex)
        End Try
    End Function
    
    ''' <summary>
    ''' Decrypts a string using AES decryption
    ''' </summary>
    Public Shared Function Decrypt(encryptedText As String) As String
        If String.IsNullOrEmpty(encryptedText) Then
            Return ""
        End If
        
        Try
            Dim key = DeriveKeyFromPassword(EncryptionKey)
            Dim iv = New Byte(15) {} ' 16 bytes for AES
            
            Using aes As Aes = Aes.Create()
                aes.Key = key
                aes.IV = iv
                aes.Mode = CipherMode.CBC
                aes.Padding = PaddingMode.PKCS7
                
                Using decryptor = aes.CreateDecryptor(aes.Key, aes.IV)
                    Dim encryptedBytes = Convert.FromBase64String(encryptedText)
                    Dim decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length)
                    Return Encoding.UTF8.GetString(decryptedBytes)
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception("Decryption failed: " & ex.Message, ex)
        End Try
    End Function
    
    ''' <summary>
    ''' Derives a 256-bit key from the password using SHA256
    ''' </summary>
    Private Shared Function DeriveKeyFromPassword(password As String) As Byte()
        Using sha256 As SHA256 = SHA256.Create()
            Return sha256.ComputeHash(Encoding.UTF8.GetBytes(password))
        End Using
    End Function
    
    ''' <summary>
    ''' Encrypts username and password and displays them for App.config
    ''' </summary>
    Public Shared Sub GenerateEncryptedCredentials(username As String, password As String)
        Console.WriteLine("=== Encryption Utility ===")
        Console.WriteLine()
        Console.WriteLine("Original Username: " & username)
        Console.WriteLine("Original Password: " & password)
        Console.WriteLine()
        
        Dim encryptedUsername = Encrypt(username)
        Dim encryptedPassword = Encrypt(password)
        
        Console.WriteLine("Encrypted Username: " & encryptedUsername)
        Console.WriteLine("Encrypted Password: " & encryptedPassword)
        Console.WriteLine()
        Console.WriteLine("Copy these values to App.config:")
        Console.WriteLine("<add key=""RedmineUsername"" value=""" & encryptedUsername & """ />")
        Console.WriteLine("<add key=""RedminePassword"" value=""" & encryptedPassword & """ />")
        Console.WriteLine()
        
        ' Verify decryption
        Console.WriteLine("Verification (decrypted):")
        Console.WriteLine("Username: " & Decrypt(encryptedUsername))
        Console.WriteLine("Password: " & Decrypt(encryptedPassword))
    End Sub
End Class
