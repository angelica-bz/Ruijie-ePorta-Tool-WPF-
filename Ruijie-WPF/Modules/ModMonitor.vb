Imports System.Collections.Generic
Imports System.IO
Imports System.Threading
Imports System.Windows
Imports Microsoft.VisualBasic

Public Module ModMonitor

#Region "格式化工具"

    Public Function FormatDuration(Dur As TimeSpan) As String
        Dim Total As Integer = CInt(Math.Floor(Dur.TotalSeconds))
        If Total < 60 Then
            Return Total & "s"
        ElseIf Total < 3600 Then
            Return (Total \ 60) & "m" & (Total Mod 60) & "s"
        Else
            Dim H As Integer = Total \ 3600
            Dim M As Integer = (Total Mod 3600) \ 60
            Return H & "h" & M & "m"
        End If
    End Function

#End Region

End Module

Public Class NetworkMonitor

    Public Event LogMessage(Msg As String)
    Public Event StatusChanged(Connected As Boolean)
    Public Event SchoolStatusChanged(Reachable As Boolean)

    Private ReadOnly _Cfg As Dictionary(Of String, Object)
    Private ReadOnly _StopEvent As New ManualResetEvent(False)
    Private _WasConnected As Nullable(Of Boolean) = Nothing
    Private _Headers As New Dictionary(Of String, String)
    Private _ConnectedSince As Nullable(Of DateTime) = Nothing
    Private _DisconnectTime As Nullable(Of DateTime) = Nothing
    Private _DisconnectSchoolReachable As Nullable(Of Boolean) = Nothing
    Private _SchoolReachable As Nullable(Of Boolean) = Nothing
    Private ReadOnly _RecentLogs As New Queue(Of String)
    Private ReadOnly _RecentLogsLock As New Object()
    Private _Thread As Thread

    Public ReadOnly Property IsCurrentlyConnected As Boolean
        Get
            Return _WasConnected.HasValue AndAlso _WasConnected.Value
        End Get
    End Property

    Public Function GetSnapshot() As (
        Connected As Boolean,
        SchoolReachable As Nullable(Of Boolean),
        Logs As String()
    )
        SyncLock _RecentLogsLock
            Return (_WasConnected.GetValueOrDefault(False), _SchoolReachable, _RecentLogs.ToArray())
        End SyncLock
    End Function

    Public Sub New(Cfg As Dictionary(Of String, Object))
        _Cfg = Cfg
    End Sub

    Public Sub Start()
        _StopEvent.Reset()
        _Thread = New Thread(AddressOf RunLoop) With {
            .Name = "NetworkMonitor",
            .IsBackground = True,
            .Priority = ThreadPriority.BelowNormal
        }
        _Thread.Start()
    End Sub

    Public Sub [Stop]()
        _StopEvent.Set()
    End Sub

    Private Sub RunLoop()
        Dim ServerUrl As String = GetServerUrl()

        Do While Not _StopEvent.WaitOne(0)
            Try
                Dim IsConnected As Boolean = False
                Try
                    Dim CheckTimeout = Math.Max(1, Math.Min(3, GetReconnectInterval()))
                    IsConnected = TestInternet(Timeout:=CheckTimeout)
                Catch
                    IsConnected = False
                End Try

                Dim Now As DateTime = DateTime.Now
                Dim Ts As String = Now.ToString("HH:mm:ss")

                If Not _WasConnected.HasValue Then
                    _WasConnected = IsConnected
                    If IsConnected Then _ConnectedSince = Now
                    Dim Status As String = If(IsConnected, "已连接", "未连接")
                    RaiseLog("[" & Ts & "] 监控启动 - 网络" & Status)
                    RaiseStatus(IsConnected)
                    RaiseSchoolStatus(ServerUrl)

                    If Not IsConnected AndAlso GetAutoReconnect() Then
                        RaiseLog("[" & Ts & "] 启动时已断网，尝试连接…")
                        TryReconnect(ServerUrl, Now)
                    End If

                ElseIf _WasConnected.Value AndAlso Not IsConnected Then
                    RaiseLog("[" & Ts & "] ⚠ 网络已断开")
                    RaiseStatus(False)
                    _WasConnected = False
                    _DisconnectTime = Now

                    Dim SchoolReachable = CheckSchool(ServerUrl)
                    _DisconnectSchoolReachable = SchoolReachable
                    Dim OnlineDuration As String = If(_ConnectedSince.HasValue, FormatDuration(Now - _ConnectedSince.Value), "未知")
                    Dim Reason As String = If(SchoolReachable, "认证服务器可达,疑似认证丢失", "认证服务器不可达,疑似物理断网")
                    WriteDisconnectLog("[" & Now.ToString("HH:mm:ss") & "] ⬇ 中断开始 | 已在线 " & OnlineDuration & " | " & Reason)

                    If GetAutoReconnect() Then
                        RaiseLog("[" & Ts & "] 尝试自动重连…")
                        Dim Ok = TryReconnect(ServerUrl, Now)
                        If Ok Then
                            Dim Dur As String = If(_DisconnectTime.HasValue, FormatDuration(DateTime.Now - _DisconnectTime.Value), "未知")
                            Dim SchoolNow = CheckSchool(ServerUrl)
                            WriteDisconnectLog("[" & DateTime.Now.ToString("HH:mm:ss") & "] ⬆ 中断结束(自动重连) | 持续 " & Dur & " | 认证服务器: " & If(SchoolNow, "可达", "不可达"))
                        End If
                    End If

                ElseIf Not _WasConnected.Value AndAlso IsConnected Then
                    RaiseLog("[" & Ts & "] 网络已恢复")
                    RaiseStatus(True)
                    _WasConnected = True
                    _ConnectedSince = Now

                    Dim Dur As String = If(_DisconnectTime.HasValue, FormatDuration(Now - _DisconnectTime.Value), "未知")
                    Dim SchoolNow = CheckSchool(ServerUrl)
                    WriteDisconnectLog("[" & Now.ToString("HH:mm:ss") & "] ⬆ 中断结束(自行恢复) | 持续 " & Dur & " | 认证服务器: " & If(SchoolNow, "可达", "不可达"))

                ElseIf _WasConnected.Value AndAlso IsConnected Then
                    If Not _ConnectedSince.HasValue Then _ConnectedSince = Now
                End If

                Dim Interval As Integer = Math.Max(1, GetReconnectInterval())
                _StopEvent.WaitOne(Interval * 1000)

            Catch ex As Exception
                RaiseLog("[" & DateTime.Now.ToString("HH:mm:ss") & "] 监控线程异常: " & ex.ToString())
                _StopEvent.WaitOne(5000)
            End Try
        Loop
    End Sub

    Private Function TryReconnect(ServerUrl As String, Now As DateTime) As Boolean
        Dim Result As Dictionary(Of String, Object) = Nothing
        Try
            If _Headers.Count = 0 Then
                _Headers = BuildHeaders(_Cfg)
            End If
            Result = Login(_Cfg, _Headers)
        Catch ex As Exception
            Dim Ts As String = Now.ToString("HH:mm:ss")
            RaiseLog("[" & Ts & "] 自动重连异常: " & ex.Message)
            WriteDisconnectLog("[" & DateTime.Now.ToString("HH:mm:ss") & "]   自动重连: 异常 (" & ex.Message & ")")
            Return False
        End Try

        Dim Now2 As DateTime = DateTime.Now
        Dim Ts2 As String = Now2.ToString("HH:mm:ss")
        If Result.ContainsKey("result") AndAlso Result("result").ToString() = "success" Then
            RaiseLog("[" & Ts2 & "] 自动重连成功")
            RaiseStatus(True)
            _WasConnected = True
            _ConnectedSince = Now2
            Return True
        Else
            Dim Msg As String = "未知错误"
            If Result.ContainsKey("message") AndAlso Result("message") IsNot Nothing Then
                Msg = Result("message").ToString()
            End If
            RaiseLog("[" & Ts2 & "] 自动重连失败: " & Msg)
            WriteDisconnectLog("[" & Now2.ToString("HH:mm:ss") & "]   自动重连: 失败 (" & Msg & ")")
            Return False
        End If
    End Function

    Private Sub RaiseLog(Msg As String)
        RaiseEvent LogMessage(Msg)
        DailyWrite(Msg & vbCrLf)
        SyncLock _RecentLogsLock
            _RecentLogs.Enqueue(Msg)
            While _RecentLogs.Count > 50
                _RecentLogs.Dequeue()
            End While
        End SyncLock
    End Sub

    Private Sub RaiseStatus(Connected As Boolean)
        RaiseEvent StatusChanged(Connected)
    End Sub

    Private Sub RaiseSchoolStatus(ServerUrl As String)
        Dim Reachable As Boolean = False
        Try
            Reachable = TcpProbe(ServerUrl)
        Catch
        End Try
        _SchoolReachable = Reachable
        RaiseEvent SchoolStatusChanged(Reachable)
    End Sub

    Private Function CheckSchool(ServerUrl As String) As Boolean
        Try
            Return TcpProbe(ServerUrl)
        Catch
            Return False
        End Try
    End Function

    Private Sub WriteDisconnectLog(Msg As String)
        DailyWrite(Msg & vbCrLf)
    End Sub

    Private Function GetServerUrl() As String
        Dim Server As String = GetDictStr(GetUrlDict(_Cfg), ConfigKeys.Server)
        If Server = "" Then Return "http://127.0.0.1"
        Return Server
    End Function

    Private Function GetAutoReconnect() As Boolean
        Return GetDictBool(GetFunctionDict(_Cfg), ConfigKeys.AutoReconnect, False)
    End Function

    Private Function GetReconnectInterval() As Integer
        Return GetDictInt(GetFunctionDict(_Cfg), ConfigKeys.ReconnectInterval, 5)
    End Function

End Class
