Imports System.ComponentModel
Imports System.Configuration.Install
Imports System.ServiceProcess

''' <summary>
''' Installer for the Windows Service
''' </summary>
<RunInstaller(True)>
Public Class ProjectInstaller
    Inherits Installer

    Private serviceProcessInstaller As ServiceProcessInstaller
    Private serviceInstaller As ServiceInstaller

    Public Sub New()
        MyBase.New()

        ' Service Process Installer
        serviceProcessInstaller = New ServiceProcessInstaller()
        serviceProcessInstaller.Account = ServiceAccount.LocalSystem
        serviceProcessInstaller.Username = Nothing
        serviceProcessInstaller.Password = Nothing

        ' Service Installer
        serviceInstaller = New ServiceInstaller()
        serviceInstaller.ServiceName = "WAR_Redmine"
        serviceInstaller.DisplayName = "WAR_Redmine"
        serviceInstaller.Description = "WAR_Redmine - Monitors Redmine tickets and generates HTML reports"
        serviceInstaller.StartType = ServiceStartMode.Automatic

        ' Add installers to collection
        Me.Installers.Add(serviceProcessInstaller)
        Me.Installers.Add(serviceInstaller)
    End Sub
End Class
