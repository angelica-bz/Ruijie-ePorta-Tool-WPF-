Public Class FormMain

    Private ReadOnly PageStatus As New PageStatus()
    Private ReadOnly PageConfig As New PageConfig()
    Private CurrentPage As Integer = 0

    Private Sub FormMain_Loaded() Handles Me.Loaded
        AniStart()
        FraMain.Content = PageStatus
    End Sub

    Private Sub TitleBar_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        If TypeOf e.OriginalSource Is MyIconButton Then Return
        Try
            DragMove()
        Catch
        End Try
    End Sub

    Private Sub BtnTitleClose_Click(sender As Object, e As EventArgs)
        Close()
    End Sub

    Private Sub BtnTitleMin_Click(sender As Object, e As EventArgs)
        WindowState = WindowState.Minimized
    End Sub

    Private Sub TabStatus_Click(sender As Object, e As MouseButtonEventArgs)
        If CurrentPage = 0 Then Return
        CurrentPage = 0
        FraMain.Content = PageStatus
        TabStatus.Background = FindResource("ColorBrush3")
        CType(TabStatus.Child, TextBlock).Foreground = New SolidColorBrush(Colors.White)
        TabConfig.Background = FindResource("ColorBrushGray5")
        CType(TabConfig.Child, TextBlock).Foreground = FindResource("ColorBrush1")
    End Sub

    Private Sub TabConfig_Click(sender As Object, e As MouseButtonEventArgs)
        If CurrentPage = 1 Then Return
        CurrentPage = 1
        FraMain.Content = PageConfig
        TabConfig.Background = FindResource("ColorBrush3")
        CType(TabConfig.Child, TextBlock).Foreground = New SolidColorBrush(Colors.White)
        TabStatus.Background = FindResource("ColorBrushGray5")
        CType(TabStatus.Child, TextBlock).Foreground = FindResource("ColorBrush1")
    End Sub

End Class
