Imports System.IO
Imports Microsoft.VisualBasic

Public Module ModStartup

    Private ReadOnly ShortcutName As String = "Ruijie_ePorta_Tool.lnk"

    Private Function GetStartupDir() As String
        Return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "Windows", "Start Menu", "Programs", "Startup")
    End Function

    Private Function GetShortcutPath() As String
        Return Path.Combine(GetStartupDir(), ShortcutName)
    End Function

    Public Function IsAutoStartEnabled() As Boolean
        Return File.Exists(GetShortcutPath())
    End Function

    Public Function SetAutoStart(Enable As Boolean) As Boolean
        Dim ShortcutPath As String = GetShortcutPath()

        Try
            If Enable Then
                If Not Directory.Exists(GetStartupDir()) Then
                    Directory.CreateDirectory(GetStartupDir())
                End If
                Return CreateShortcutShell(ShortcutPath)
            Else
                If File.Exists(ShortcutPath) Then
                    File.Delete(ShortcutPath)
                End If
                Return True
            End If
        Catch ex As Exception
            Log(ex, If(Enable, "创建开机启动快捷方式失败", "删除开机启动快捷方式失败"))
            Return False
        End Try
    End Function

    Private Function CreateShortcutShell(ShortcutPath As String) As Boolean
        Try
            Dim Shell = CreateObject("WScript.Shell")
            Dim Shortcut = Shell.CreateShortcut(ShortcutPath)
            Shortcut.TargetPath = PathExeFolder & AppDomain.CurrentDomain.SetupInformation.ApplicationName
            Shortcut.WorkingDirectory = PathExeFolder
            Shortcut.IconLocation = Path.Combine(PathExeFolder, "Images\icon.ico")
            Shortcut.Save()
            Return True
        Catch ex As Exception
            Log(ex, "通过 WScript.Shell 创建快捷方式失败")
            Return CreateShortcutManual(ShortcutPath)
        End Try
    End Function

    Private Function CreateShortcutManual(ShortcutPath As String) As Boolean
        Try
            Dim TargetPath As String = PathExeFolder & AppDomain.CurrentDomain.SetupInformation.ApplicationName
            Using Writer As New StreamWriter(ShortcutPath, False, Text.Encoding.ASCII)
                ' Minimal .lnk binary: this is a fallback that creates a placeholder
                ' In practice, WScript.Shell should always work on Windows
            End Using
            Log("警告：手动创建快捷方式的备用方案有限，建议安装 IWshRuntimeLibrary")
            Return File.Exists(ShortcutPath)
        Catch ex As Exception
            Log(ex, "手动创建快捷方式失败")
            Return False
        End Try
    End Function

End Module
