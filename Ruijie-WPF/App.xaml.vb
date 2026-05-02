Class Application

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        AddHandler Me.DispatcherUnhandledException, AddressOf App_DispatcherUnhandledException
    End Sub

    Private Sub App_DispatcherUnhandledException(sender As Object, e As Windows.Threading.DispatcherUnhandledExceptionEventArgs)
        MessageBox.Show("发生未处理的异常：" & vbCrLf & e.Exception.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error)
        e.Handled = True
    End Sub

End Class
