Imports System.Windows.Threading
Imports System.Windows.Media
Imports Microsoft.VisualBasic

Public Class PageStatus

    Private Cfg As Dictionary(Of String, Object)
    Private Headers As Dictionary(Of String, String)
    Private Monitor As NetworkMonitor
    Private LogLineCount As Integer = 0
    Private LastStatus As Nullable(Of Boolean) = Nothing
    Private LastSchool As Nullable(Of Boolean) = Nothing
    Private NotifiedConnection As Boolean = False
    Private _MonitorHandlersAttached As Boolean = False

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Cfg = SharedCfg
        Headers = SharedHeaders

        Dim Interval As Integer = GetDictInt(GetFunctionDict(Cfg), ConfigKeys.ReconnectInterval, 5)
        If Interval < 1 Then Interval = 1
        If Interval > 99 Then Interval = 99
        TxtInterval.Text = Interval.ToString()
        LabInterval.Text = Interval & " 秒"

        ChkAutoReconnect.Checked = GetDictBool(GetFunctionDict(Cfg), ConfigKeys.AutoReconnect, False)

        ChkAutoStart.Checked = IsAutoStartEnabled()

        RunInNewThread(Sub() CleanOldLogs(7), "LogCleaner", ThreadPriority.Lowest)
        StartMonitor()
    End Sub

    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        StopMonitor()
    End Sub

#Region "监控"

    Private Sub StartMonitor()
        Dim MainWin = TryCast(Application.Current?.MainWindow, FormMain)
        If MainWin IsNot Nothing Then
            Monitor = MainWin.BgMonitor
            NotifiedConnection = MainWin.BgNotified
        End If
        If Monitor Is Nothing Then Return

        If Not _MonitorHandlersAttached Then
            Dim Snap = Monitor.GetSnapshot()

            UpdateStatus(Snap.Connected)

            If Snap.SchoolReachable.HasValue Then
                UpdateSchoolStatus(Snap.SchoolReachable.Value)
            End If

            For Each msg In Snap.Logs
                AppendLog(msg, writeToFile:=False)
            Next

            _MonitorHandlersAttached = True
            AddHandler Monitor.LogMessage, Sub(msg) RunInUi(Sub() AppendLog(msg, writeToFile:=False))
            AddHandler Monitor.StatusChanged, Sub(connected) RunInUi(Sub() UpdateStatus(connected))
            AddHandler Monitor.SchoolStatusChanged, Sub(reachable) RunInUi(Sub() UpdateSchoolStatus(reachable))
        End If
    End Sub

    Private Sub StopMonitor()
        _MonitorHandlersAttached = False
        Monitor = Nothing
    End Sub

#End Region

#Region "状态刷新"

    Private Sub UpdateStatus(connected As Boolean)
        LastStatus = connected
        If connected Then
            ShapeStatusDot.Fill = New SolidColorBrush(Color.FromRgb(&H4C, &HAF, &H50))
            LabStatus.Text = "已连接"
            If Application.IsBackgroundStart AndAlso Not NotifiedConnection Then
                NotifiedConnection = True
                Dim MainWin = TryCast(Application.Current?.MainWindow, FormMain)
                If MainWin IsNot Nothing Then
                    MainWin.ShowTrayNotification("连接成功", "网络已连接")
                End If
            End If
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

    Private IsConnecting As Boolean = False

    Private Sub BtnConnect_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnConnect.Click
        If IsConnecting Then Return
        IsConnecting = True
        BtnConnect.Text = "连接中…"
        BtnConnect.IsEnabled = False
        BtnDisconnect.IsEnabled = False
        RunInNewThread(
            Sub()
                Try
                    Dim Result = Login(Cfg, Headers)
                    RunInUi(Sub()
                                HandleResult(Result, "联网")
                            End Sub)
                Catch ex As Exception
                    RunInUi(Sub()
                                AppendLog("[" & GetTimeNow() & "] 连接异常: " & ex.Message)
                            End Sub)
                Finally
                    RunInUi(Sub()
                                BtnConnect.Text = "连接"
                                BtnConnect.IsEnabled = True
                                BtnDisconnect.IsEnabled = True
                                IsConnecting = False
                            End Sub)
                End Try
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
            Dim Raw = If(result.ContainsKey("message") AndAlso result("message") IsNot Nothing,
                         result("message").ToString(), "")
            Dim Msg = If(String.IsNullOrEmpty(Raw), action & "成功", Raw)
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
            MessageBox.Show("无法设置开机启动。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning)
            ChkAutoStart.Checked = False
        End If
    End Sub

    Private Sub ChkAutoReconnect_Change(sender As Object, user As Boolean) Handles ChkAutoReconnect.Change
        If Not user Then Return
        Dim FunctionCfg = GetFunctionDict(Cfg)
        If FunctionCfg IsNot Nothing Then
            FunctionCfg(ConfigKeys.AutoReconnect) = ChkAutoReconnect.Checked
            WriteCfg(Cfg)
        End If
    End Sub

    Private Sub BtnApplyInterval_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnApplyInterval.Click
        Dim Val As Integer
        If Not Integer.TryParse(TxtInterval.Text, Val) OrElse Val < 1 OrElse Val > 99 Then
            MessageBox.Show("检测间隔必须为 1 到 99 之间的整数。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning)
            Dim FunctionCfg2 = GetFunctionDict(Cfg)
            If FunctionCfg2 IsNot Nothing AndAlso FunctionCfg2.ContainsKey(ConfigKeys.ReconnectInterval) Then
                TxtInterval.Text = FunctionCfg2(ConfigKeys.ReconnectInterval).ToString()
            End If
            Return
        End If
        Dim FunctionCfg3 = GetFunctionDict(Cfg)
        If FunctionCfg3 IsNot Nothing Then
            FunctionCfg3(ConfigKeys.ReconnectInterval) = Val
            WriteCfg(Cfg)
        End If
        LabInterval.Text = Val & " 秒"
        AppendLog("[" & GetTimeNow() & "] 检测间隔已更新为 " & Val & " 秒")
    End Sub

#End Region

#Region "日志"

    Private Sub AppendLog(msg As String, Optional writeToFile As Boolean = True)
        LabLog.Text &= msg & vbLf
        If writeToFile Then RunInNewThread(Sub() DailyWrite(msg & vbCrLf), "LogWriter", ThreadPriority.Lowest)
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
