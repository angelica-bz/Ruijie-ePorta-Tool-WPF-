Imports Microsoft.VisualBasic

Class Application

    Public Shared IsBackgroundStart As Boolean = False

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        If e.Args.Contains("--test") Then
            DailyWriteEnabled = False
            Me.ShutdownMode = ShutdownMode.OnExplicitShutdown
            ModMonitorTests.RunAllTests()
            Me.Shutdown()
            Return
        End If
        If e.Args.Contains("--background") Then
            IsBackgroundStart = True
        End If
        AddHandler Me.DispatcherUnhandledException, AddressOf App_DispatcherUnhandledException
        System.Runtime.ProfileOptimization.SetProfileRoot(PathExeFolder)
        System.Runtime.ProfileOptimization.StartProfile("Startup.profile")
    End Sub

    Private Sub App_DispatcherUnhandledException(sender As Object, e As Windows.Threading.DispatcherUnhandledExceptionEventArgs)
        MessageBox.Show("发生未处理的异常：" & vbCrLf & e.Exception.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error)
        e.Handled = True
    End Sub

End Class
