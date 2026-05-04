Imports System.Globalization
Imports System.Diagnostics
Imports System.Threading
Imports System.Windows
Imports System.Windows.Media
Imports Microsoft.VisualBasic
Imports System.Runtime.CompilerServices

Public Module ModBase

#Region "路径与常量"

    Public PathExeFolder As String = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
    Public PathExe As String = PathExeFolder & AppDomain.CurrentDomain.SetupInformation.ApplicationName
    Public PathAppdata As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\Ruijie\"
    Public ApplicationStartTick As Long = GetTimeMs()

#End Region

#Region "颜色"

    Public ReadOnly Color1 As New MyColor(&HFF, &H34, &H3D, &H4A)
    Public ReadOnly Color2 As New MyColor(&HFF, &H0B, &H5B, &HCB)
    Public ReadOnly Color3 As New MyColor(&HFF, &H13, &H70, &HF3)
    Public ReadOnly Color4 As New MyColor(&HFF, &H48, &H90, &HF5)
    Public ReadOnly Color5 As New MyColor(&HFF, &H96, &HC0, &HF9)
    Public ReadOnly Color6 As New MyColor(&HFF, &HD5, &HE6, &HFD)
    Public ReadOnly Color7 As New MyColor(&HFF, &HE0, &HEA, &HFD)
    Public ReadOnly Color8 As New MyColor(&HFF, &HEA, &HF2, &HFE)
    Public ReadOnly ColorBg1 As New MyColor(&H01, &HEA, &HF2, &HFE)
    Public ReadOnly ColorSemiTransparent As New MyColor(&H01, &HEA, &HF2, &HFE)
    Public ReadOnly ColorGray1 As New MyColor(&HFF, &H40, &H40, &H40)
    Public ReadOnly ColorGray2 As New MyColor(&HFF, &H73, &H73, &H73)
    Public ReadOnly ColorGray3 As New MyColor(&HFF, &H8C, &H8C, &H8C)
    Public ReadOnly ColorGray4 As New MyColor(&HFF, &HA6, &HA6, &HA6)
    Public ReadOnly ColorGray5 As New MyColor(&HFF, &HCC, &HCC, &HCC)
    Public ReadOnly ColorGray6 As New MyColor(&HFF, &HEB, &HEB, &HEB)
    Public ReadOnly ColorGray7 As New MyColor(&HFF, &HF0, &HF0, &HF0)
    Public ReadOnly ColorGray8 As New MyColor(&HFF, &HF5, &HF5, &HF5)
    Public ReadOnly ColorWhite As New MyColor(255, 255, 255, 255)
    Public ReadOnly ColorBlack As New MyColor(255, 0, 0, 0)

#End Region

#Region "自定义类"

    Public Class MyColor

        Public A As Double = 255
        Public R As Double = 0
        Public G As Double = 0
        Public B As Double = 0

        Public Shared Widening Operator CType(col As Color) As MyColor
            Return New MyColor(col)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As Color
            Return Color.FromArgb(DoubleToByte(conv.A), DoubleToByte(conv.R), DoubleToByte(conv.G), DoubleToByte(conv.B))
        End Operator
        Public Shared Widening Operator CType(bru As SolidColorBrush) As MyColor
            Return New MyColor(bru.Color)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As SolidColorBrush
            Return New SolidColorBrush(Color.FromArgb(DoubleToByte(conv.A), DoubleToByte(conv.R), DoubleToByte(conv.G), DoubleToByte(conv.B)))
        End Operator
        Public Shared Widening Operator CType(bru As Brush) As MyColor
            Return New MyColor(bru)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As Brush
            Return New SolidColorBrush(Color.FromArgb(DoubleToByte(conv.A), DoubleToByte(conv.R), DoubleToByte(conv.G), DoubleToByte(conv.B)))
        End Operator

        Public Shared Operator +(a As MyColor, b As MyColor) As MyColor
            Return New MyColor With {.A = a.A + b.A, .B = a.B + b.B, .G = a.G + b.G, .R = a.R + b.R}
        End Operator
        Public Shared Operator -(a As MyColor, b As MyColor) As MyColor
            Return New MyColor With {.A = a.A - b.A, .B = a.B - b.B, .G = a.G - b.G, .R = a.R - b.R}
        End Operator
        Public Shared Operator *(a As MyColor, b As Double) As MyColor
            Return New MyColor With {.A = a.A * b, .B = a.B * b, .G = a.G * b, .R = a.R * b}
        End Operator
        Public Shared Operator /(a As MyColor, b As Double) As MyColor
            Return New MyColor With {.A = a.A / b, .B = a.B / b, .G = a.G / b, .R = a.R / b}
        End Operator
        Public Shared Operator =(a As MyColor, b As MyColor) As Boolean
            If a Is Nothing AndAlso b Is Nothing Then Return True
            If a Is Nothing OrElse b Is Nothing Then Return False
            Return a.A = b.A AndAlso a.R = b.R AndAlso a.G = b.G AndAlso a.B = b.B
        End Operator
        Public Shared Operator <>(a As MyColor, b As MyColor) As Boolean
            Return Not (a = b)
        End Operator

        Public Shared Function Lerp(ValueA As MyColor, ValueB As MyColor, Percent As Double) As MyColor
            Return ValueA * (1 - Percent) + ValueB * Percent
        End Function

        Public Shared Function DoubleToByte(d As Double) As Byte
            If d < 0 Then d = 0
            If d > 255 Then d = 255
            Return Math.Round(d)
        End Function

        Public Sub New()
        End Sub
        Public Sub New(col As Color)
            Me.A = col.A : Me.R = col.R : Me.G = col.G : Me.B = col.B
        End Sub
        Public Sub New(newR As Double, newG As Double, newB As Double)
            Me.A = 255 : Me.R = newR : Me.G = newG : Me.B = newB
        End Sub
        Public Sub New(newA As Double, newR As Double, newG As Double, newB As Double)
            Me.A = newA : Me.R = newR : Me.G = newG : Me.B = newB
        End Sub
        Public Sub New(brush As Brush)
            Dim c As Color = CType(brush, SolidColorBrush).Color
            A = c.A : R = c.R : G = c.G : B = c.B
        End Sub
        Public Sub New(brush As SolidColorBrush)
            Dim c As Color = brush.Color
            A = c.A : R = c.R : G = c.G : B = c.B
        End Sub
        Public Sub New(alpha As Double, brush As Brush)
            Dim c As Color = CType(brush, SolidColorBrush).Color
            Me.A = alpha : Me.R = c.R : Me.G = c.G : Me.B = c.B
        End Sub
        Public Sub New(hexString As String)
            Dim c As Color = ColorConverter.ConvertFromString(hexString)
            A = c.A : R = c.R : G = c.G : B = c.B
        End Sub
        Public Sub New(obj As Object)
            If obj Is Nothing Then
                A = 255 : R = 255 : G = 255 : B = 255
            ElseIf TypeOf obj Is SolidColorBrush Then
                Dim c As Color = CType(obj, SolidColorBrush).Color
                A = c.A : R = c.R : G = c.G : B = c.B
            ElseIf TypeOf obj Is String Then
                Dim c As Color = ColorConverter.ConvertFromString(CStr(obj))
                A = c.A : R = c.R : G = c.G : B = c.B
            Else
                A = obj.A : R = obj.R : G = obj.G : B = obj.B
            End If
        End Sub

        Public Overrides Function ToString() As String
            Return "(" & A & "," & R & "," & G & "," & B & ")"
        End Function
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me = obj
        End Function
        Public Overrides Function GetHashCode() As Integer
            Return (A * 1000000 + R * 10000 + G * 100 + B).GetHashCode()
        End Function

    End Class

    Public Class MyRect
        Public Property Width As Double = 0
        Public Property Height As Double = 0
        Public Property Left As Double = 0
        Public Property Top As Double = 0
        Public Sub New()
        End Sub
        Public Sub New(left As Double, top As Double, width As Double, height As Double)
            Me.Left = left : Me.Top = top : Me.Width = width : Me.Height = height
        End Sub
    End Class

#End Region

#Region "工具函数"

    Private Uuid As Integer = 1
    Private UuidLock As Object
    Public Function GetUuid() As Integer
        If UuidLock Is Nothing Then UuidLock = New Object
        SyncLock UuidLock
            Uuid += 1
            Return Uuid
        End SyncLock
    End Function

    Public Function GetTimeMs() As Long
        Return Stopwatch.GetTimestamp() \ (Stopwatch.Frequency \ 1000L)
    End Function

    Public Function GetTimeNow() As String
        Return Date.Now.ToString("HH':'mm':'ss")
    End Function

    Public Function GetWPFSize(PixelSize As Double) As Double
        Return PixelSize * 96 / GetDPI()
    End Function

    Public Function GetStringFromEnum(EnumData As [Enum]) As String
        Return [Enum].GetName(EnumData.GetType, EnumData)
    End Function

    Private Function GetDPI() As Double
        Try
            Return PresentationSource.FromVisual(Application.Current.MainWindow)?.CompositionTarget?.TransformFromDevice.M11 * 96
        Catch
        End Try
        Return 96
    End Function

    Public Function Lerp(ValueA As Double, ValueB As Double, Percent As Double) As Double
        Return ValueA + (ValueB - ValueA) * Percent
    End Function

    Private ReadOnly UiThreadId As Integer = Thread.CurrentThread.ManagedThreadId
    Public Function RunInUi() As Boolean
        Return Thread.CurrentThread.ManagedThreadId = UiThreadId
    End Function

    Public Sub RunInUi(Action As Action)
        If RunInUi() Then
            Action()
        Else
            Application.Current.Dispatcher.InvokeAsync(Action)
        End If
    End Sub

    Public Sub RunInUiWait(Action As Action)
        If RunInUi() Then
            Action()
        Else
            Application.Current.Dispatcher.Invoke(Action)
        End If
    End Sub

    Public Function RunInNewThread(Action As Action, Optional Name As String = Nothing, Optional Priority As ThreadPriority = ThreadPriority.Normal) As Thread
        Dim th As New Thread(
        Sub()
            Try
                Action()
            Catch ex As ThreadInterruptedException
                Log(Name & ": thread aborted")
            Catch ex As Exception
                Log(ex, Name & ": thread execution failed")
            End Try
        End Sub) With {.Name = If(Name, "Runtime New Invoke " & GetUuid() & "#"), .Priority = Priority}
        th.Start()
        Return th
    End Function

#End Region

#Region "共享配置"

    Public SharedCfg As Dictionary(Of String, Object)
    Public SharedHeaders As Dictionary(Of String, String)

    Public Sub InitSharedConfig()
        Try
            SharedCfg = ReadCfg(GuiMode:=True)
            SharedHeaders = BuildHeaders(SharedCfg)
        Catch ex As Exception
            SharedCfg = GetDefaultConfig()
            SharedHeaders = BuildHeaders(SharedCfg)
            Log(ex, "预加载配置失败")
        End Try
    End Sub

#End Region

#Region "日志"

    Private LogLock As New Object
    Public Sub Log(Text As String)
        Try
            Dim AppendText As String = $"[{GetTimeNow()}] {Text}{vbCrLf}"
            SyncLock LogLock
                Debug.Write(AppendText)
            End SyncLock
        Catch
        End Try
    End Sub

    Public Sub Log(Ex As Exception, Desc As String)
        Try
            Dim AppendText As String = $"[{GetTimeNow()}] {Desc}: {Ex.Message}{vbCrLf}"
            SyncLock LogLock
                Debug.Write(AppendText)
            End SyncLock
        Catch
        End Try
    End Sub

    Public Sub DailyWrite(Msg As String)
        Dim Today As String = Date.Now.ToString("yyyy-MM-dd")
        Dim LogsDir As String = GetLogsDir()
        Try
            If Not Directory.Exists(LogsDir) Then Directory.CreateDirectory(LogsDir)
        Catch ex As Exception
            Return
        End Try
        Try
            Dim LogFile As String = System.IO.Path.Combine(LogsDir, Today & ".txt")
            File.AppendAllText(LogFile, Msg, Text.Encoding.UTF8)
        Catch
        End Try
    End Sub

    Public Sub CleanOldLogs(Optional KeepDays As Integer = 7)
        Dim LogsDir As String = GetLogsDir()
        If Not Directory.Exists(LogsDir) Then Return
        Dim Cutoff As Date = Date.Now.AddDays(-KeepDays)
        Try
            For Each F In Directory.GetFiles(LogsDir)
                Dim Stem As String = System.IO.Path.GetFileNameWithoutExtension(F)
                Dim FileDate As Date
                If Date.TryParseExact(Stem, "yyyy-MM-dd", Nothing, Globalization.DateTimeStyles.None, FileDate) Then
                    If FileDate < Cutoff Then
                        Try
                            File.Delete(F)
                        Catch
                        End Try
                    End If
                End If
            Next
        Catch
        End Try
    End Sub

#End Region

End Module

''' <summary>
''' 自定义路由事件参数。
''' </summary>
Public NotInheritable Class RouteEventArgs
    Inherits EventArgs
    Public RaiseByMouse As Boolean
    Public Handled As Boolean = False
    Public Sub New(Optional RaiseByMouse As Boolean = False)
        Me.RaiseByMouse = RaiseByMouse
    End Sub
End Class

Public Module ModBaseExtensions
    <Extension>
    Public Sub RaiseCustomEvent(element As FrameworkElement)
    End Sub
End Module
