Imports System.Windows.Threading
Imports System.Windows.Media
Imports Microsoft.VisualBasic

Public Class PageStatus

    Private Cfg As Dictionary(Of String, Object)
    Private Headers As Dictionary(Of String, String)
    Private Monitor As NetworkMonitor
    Private WithEvents PollTimer As New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(500)}
    Private LogLineCount As Integer = 0
    Private LastStatus As Nullable(Of Boolean) = Nothing
    Private LastSchool As Nullable(Of Boolean) = Nothing

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Try
            Cfg = ReadCfg(GuiMode:=True)
            Headers = BuildHeaders(Cfg)
        Catch ex As Exception
            Cfg = GetDefaultConfig()
            Headers = BuildHeaders(Cfg)
        End Try

        Dim Interval As Integer = 5
        Dim FunctionCfg = TryCast(Cfg("function"), Dictionary(Of String, Object))
        If FunctionCfg IsNot Nothing AndAlso FunctionCfg.ContainsKey("reconnect_interval") Then
            Integer.TryParse(FunctionCfg("reconnect_interval").ToString(), Interval)
        End If
        TxtInterval.Text = Interval.ToString()
        LabInterval.Text = Interval & " 秒"

        Dim AutoReconnect As Boolean = False
        If FunctionCfg IsNot Nothing AndAlso FunctionCfg.ContainsKey("auto_reconnect") Then
            Dim Val = FunctionCfg("auto_reconnect")
            If TypeOf Val Is Boolean Then AutoReconnect = CBool(Val)
        End If
        ChkAutoReconnect.Checked = AutoReconnect

        ChkAutoStart.Checked = IsAutoStartEnabled()

        CleanOldLogs(7)
        StartMonitor()
        PollTimer.Start()
    End Sub

    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        PollTimer.Stop()
        StopMonitor()
    End Sub

#Region "监控"

    Private Sub StartMonitor()
        Monitor = New NetworkMonitor(Cfg)
        AddHandler Monitor.LogMessage, Sub(msg) RunInUi(Sub() AppendLog(msg))
        AddHandler Monitor.StatusChanged, Sub(connected) RunInUi(Sub() UpdateStatus(connected))
        AddHandler Monitor.SchoolStatusChanged, Sub(reachable) RunInUi(Sub() UpdateSchoolStatus(reachable))
        Monitor.Start()
    End Sub

    Private Sub StopMonitor()
        If Monitor IsNot Nothing Then
            Monitor.Stop()
            Monitor = Nothing
        End If
    End Sub

#End Region

#Region "状态刷新"

    Private Sub UpdateStatus(connected As Boolean)
        LastStatus = connected
        If connected Then
            ShapeStatusDot.Fill = New SolidColorBrush(Color.FromRgb(&H4C, &HAF, &H50))
            LabStatus.Text = "已连接"
        Else
            ShapeStatusDot.Fill = New SolidColorBrush(Color.FromRgb(&HF4, &H43, &H36))
            LabStatus.Text = "未连接"
        End If
    End Sub

    Private Sub UpdateSchoolStatus(reachable As Boolean)
        LastSchool = reachable
        LabSchoolStatus.Text = If(reachable, "可达", "不可达")
    End Sub

#End Region

#Region "按钮事件"

    Private Sub BtnConnect_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnConnect.Click
        BtnConnect.Text = "连接中…"
        BtnConnect.IsEnabled = False
        BtnDisconnect.IsEnabled = False
        RunInNewThread(
            Sub()
                Dim Result = Login(Cfg, Headers)
                RunInUi(Sub()
                            HandleResult(Result, "联网")
                            BtnConnect.Text = "连接"
                            BtnConnect.IsEnabled = True
                            BtnDisconnect.IsEnabled = True
                        End Sub)
            End Sub, "Connect")
    End Sub

    Private Sub BtnDisconnect_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDisconnect.Click
        BtnConnect.IsEnabled = False
        BtnDisconnect.Text = "断开中…"
        BtnDisconnect.IsEnabled = False
        RunInNewThread(
            Sub()
                Dim Result = Logout(Cfg, Headers)
                RunInUi(Sub()
                            HandleResult(Result, "断网")
                            BtnDisconnect.Text = "断开"
                            BtnConnect.IsEnabled = True
                            BtnDisconnect.IsEnabled = True
                        End Sub)
            End Sub, "Disconnect")
    End Sub

    Private Sub HandleResult(result As Dictionary(Of String, Object), action As String)
        Dim Ts As String = GetTimeNow()
        Dim Status = If(result.ContainsKey("result"), result("result").ToString(), "")
        If Status = "success" Then
            Dim Msg = If(result.ContainsKey("message"), result("message").ToString(), action & "成功")
            AppendLog("[" & Ts & "] " & Msg)
            UpdateStatus(action = "联网")
        Else
            Dim Msg = action & "失败: " & If(result.ContainsKey("message"), result("message").ToString(), "未知错误")
            AppendLog("[" & Ts & "] " & Msg)
            MessageBox.Show(Msg, "错误", MessageBoxButton.OK, MessageBoxImage.Error)
        End If
    End Sub

#End Region

#Region "设置"

    Private Sub ChkAutoStart_Change(sender As Object, user As Boolean) Handles ChkAutoStart.Change
        If Not user Then Return
        Dim Ok = SetAutoStart(ChkAutoStart.Checked)
        If Not Ok AndAlso ChkAutoStart.Checked Then
            MessageBox.Show("无法创建开机启动快捷方式。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning)
            ChkAutoStart.Checked = False
        End If
    End Sub

    Private Sub ChkAutoReconnect_Change(sender As Object, user As Boolean) Handles ChkAutoReconnect.Change
        If Not user Then Return
        Dim FunctionCfg = TryCast(Cfg("function"), Dictionary(Of String, Object))
        If FunctionCfg IsNot Nothing Then
            FunctionCfg("auto_reconnect") = ChkAutoReconnect.Checked
            WriteCfg(Cfg)
        End If
    End Sub

    Private Sub BtnApplyInterval_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnApplyInterval.Click
        Dim Val As Integer
        If Not Integer.TryParse(TxtInterval.Text, Val) OrElse Val < 1 Then
            MessageBox.Show("检测间隔必须为不小于 1 的整数。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning)
            Dim FunctionCfg2 = TryCast(Cfg("function"), Dictionary(Of String, Object))
            If FunctionCfg2 IsNot Nothing AndAlso FunctionCfg2.ContainsKey("reconnect_interval") Then
                TxtInterval.Text = FunctionCfg2("reconnect_interval").ToString()
            End If
            Return
        End If
        Dim FunctionCfg3 = TryCast(Cfg("function"), Dictionary(Of String, Object))
        If FunctionCfg3 IsNot Nothing Then
            FunctionCfg3("reconnect_interval") = Val
            WriteCfg(Cfg)
        End If
        LabInterval.Text = Val & " 秒"
        AppendLog("[" & GetTimeNow() & "] 检测间隔已更新为 " & Val & " 秒")
    End Sub

#End Region

#Region "日志"

    Private Sub AppendLog(msg As String)
        LabLog.Text &= msg & vbLf
        DailyWrite(msg & vbCrLf)
        LogLineCount += 1
        If LogLineCount > 500 Then
            Dim Lines = LabLog.Text.Split(vbLf)
            If Lines.Length > 100 Then
                LabLog.Text = String.Join(vbLf, Lines.Skip(100))
                LogLineCount -= 100
            End If
        End If
    End Sub

    Private Sub BtnClearLog_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnClearLog.Click
        LabLog.Text = ""
        LogLineCount = 0
    End Sub

    Private Sub BtnOpenLog_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnOpenLog.Click
        Dim LogsDir = GetLogsDir()
        Try
            If Not IO.Directory.Exists(LogsDir) Then IO.Directory.CreateDirectory(LogsDir)
        Catch
        End Try
        Diagnostics.Process.Start(LogsDir)
    End Sub

#End Region

End Class
