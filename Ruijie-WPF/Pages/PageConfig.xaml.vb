Imports System.Windows.Controls
Imports Microsoft.VisualBasic

Public Class PageConfig

    Private Cfg As Dictionary(Of String, Object)
    Private HeaderRows As New List(Of Tuple(Of TextBox, TextBox, StackPanel))

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Try
            Cfg = ReadCfg(GuiMode:=True)
        Catch ex As Exception
            Cfg = GetDefaultConfig()
        End Try
        PopulateFields()
    End Sub

#Region "填充表单"

    Private Sub PopulateFields()
        Dim UrlCfg = TryCast(Cfg("url"), Dictionary(Of String, Object))
        If UrlCfg IsNot Nothing Then
            TxtServerUrl.Text = If(UrlCfg("server"), "").ToString()
            TxtLoginPath.Text = If(UrlCfg("login"), "").ToString()
            TxtLogoutPath.Text = If(UrlCfg("logout"), "").ToString()
        End If

        TxtCookie.Text = If(Cfg("cookie"), "").ToString()

        Dim LoginData = TryCast(Cfg("login_data"), Dictionary(Of String, Object))
        If LoginData IsNot Nothing Then
            TxtUserId.Text = GetLoginField(LoginData, "userId")
            TxtPassword.Password = GetLoginField(LoginData, "password")
            TxtService.Text = GetLoginField(LoginData, "service")
            TxtQueryString.Text = GetLoginField(LoginData, "queryString")
            TxtOperatorPwd.Text = GetLoginField(LoginData, "operatorPwd")
            TxtOperatorUserId.Text = GetLoginField(LoginData, "operatorUserId")
            TxtValidcode.Text = GetLoginField(LoginData, "validcode")
            TxtPasswordEncrypt.Text = GetLoginField(LoginData, "passwordEncrypt")
        End If

        RebuildHeaderRows()
    End Sub

    Private Function GetLoginField(data As Dictionary(Of String, Object), key As String) As String
        If data.ContainsKey(key) AndAlso data(key) IsNot Nothing Then
            Return data(key).ToString()
        End If
        Return ""
    End Function

    Private Sub RebuildHeaderRows()
        For Each row In HeaderRows
            PanHeaders.Children.Remove(row.Item3)
        Next
        HeaderRows.Clear()

        Dim HeadersCfg = TryCast(Cfg("headers"), Dictionary(Of String, Object))
        If HeadersCfg Is Nothing OrElse HeadersCfg.Count = 0 Then
            AddHeaderRow("Referer", "")
        Else
            For Each Kvp In HeadersCfg
                AddHeaderRow(Kvp.Key, If(Kvp.Value, "").ToString())
            Next
        End If
    End Sub

#End Region

#Region "请求头管理"

    Private Sub AddHeaderRow(key As String, value As String)
        Dim Row As New StackPanel With {.Orientation = Orientation.Horizontal, .Margin = New Thickness(0, 0, 0, 4)}

        Dim LabKey As New TextBlock With {.Text = "Key:", .VerticalAlignment = VerticalAlignment.Center, .Width = 36}
        Dim TxtKey As New TextBox With {.Text = key, .Width = 120, .Margin = New Thickness(0, 0, 4, 0)}
        Dim LabVal As New TextBlock With {.Text = "Value:", .VerticalAlignment = VerticalAlignment.Center, .Width = 42}
        Dim TxtVal As New TextBox With {.Text = value, .MinWidth = 100}

        Row.Children.Add(LabKey)
        Row.Children.Add(TxtKey)
        Row.Children.Add(LabVal)
        Row.Children.Add(TxtVal)

        PanHeaders.Children.Add(Row)
        HeaderRows.Add(Tuple.Create(TxtKey, TxtVal, Row))
    End Sub

    Private Sub BtnAddHeader_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnAddHeader.Click
        AddHeaderRow("", "")
    End Sub

    Private Sub BtnRemoveHeader_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnRemoveHeader.Click
        If HeaderRows.Count > 0 Then
            Dim Last = HeaderRows(HeaderRows.Count - 1)
            PanHeaders.Children.Remove(Last.Item3)
            HeaderRows.RemoveAt(HeaderRows.Count - 1)
        End If
    End Sub

#End Region

#Region "保存与加载"

    Private Sub BtnSaveConfig_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSaveConfig.Click
        If MessageBox.Show("将当前配置写入 config.yml？" & vbCrLf & "建议保存后重启程序。", "保存配置",
                           MessageBoxButton.YesNo, MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        Dim LoginData As New Dictionary(Of String, Object)
        SetLoginField(LoginData, "userId", TxtUserId.Text)
        SetLoginField(LoginData, "password", TxtPassword.Password)
        SetLoginField(LoginData, "service", TxtService.Text)
        SetLoginField(LoginData, "queryString", TxtQueryString.Text)
        SetLoginField(LoginData, "operatorPwd", TxtOperatorPwd.Text)
        SetLoginField(LoginData, "operatorUserId", TxtOperatorUserId.Text)
        SetLoginField(LoginData, "validcode", TxtValidcode.Text)
        SetLoginField(LoginData, "passwordEncrypt", TxtPasswordEncrypt.Text)

        Dim HeadersDict As New Dictionary(Of String, Object)
        For Each Row In HeaderRows
            Dim K = Row.Item1.Text.Trim()
            Dim V = Row.Item2.Text.Trim()
            If K <> "" Then HeadersDict(K) = V
        Next

        Dim FunctionCfg = TryCast(Cfg("function"), Dictionary(Of String, Object))
        If FunctionCfg Is Nothing Then FunctionCfg = New Dictionary(Of String, Object)
        Dim autoReconnect As Boolean = False
        Dim reconnectVal = FunctionCfg("auto_reconnect")
        If reconnectVal IsNot Nothing Then Boolean.TryParse(reconnectVal.ToString(), autoReconnect)
        FunctionCfg("auto_reconnect") = autoReconnect
        Dim Interval As Integer = 5
        Integer.TryParse(If(FunctionCfg("reconnect_interval"), "5").ToString(), Interval)
        FunctionCfg("reconnect_interval") = Interval

        Dim NewCfg As New Dictionary(Of String, Object) From {
            {"main", New Dictionary(Of String, Object) From {{"version", 3}}},
            {"function", FunctionCfg},
            {"url", New Dictionary(Of String, Object) From {
                {"server", TxtServerUrl.Text.Trim()},
                {"login", TxtLoginPath.Text.Trim()},
                {"logout", TxtLogoutPath.Text.Trim()}
            }},
            {"cookie", TxtCookie.Text.Trim()},
            {"login_data", LoginData},
            {"logout_data", New Dictionary(Of String, Object)},
            {"headers", HeadersDict}
        }

        SaveConfig(NewCfg)

        Try
            Cfg = ReadCfg()
        Catch
        End Try

        MessageBox.Show("配置已保存。" & vbCrLf & "建议重启程序使全部设置生效。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information)
    End Sub

    Private Sub SetLoginField(data As Dictionary(Of String, Object), key As String, value As String)
        If value = "" Then
            data(key) = Nothing
        ElseIf value.ToLower() = "true" Then
            data(key) = True
        ElseIf value.ToLower() = "false" Then
            data(key) = False
        Else
            data(key) = value
        End If
    End Sub

    Private Sub BtnReloadConfig_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnReloadConfig.Click
        If MessageBox.Show("放弃未保存的修改，从 config.yml 重新加载？", "重新加载",
                           MessageBoxButton.YesNo, MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return
        Try
            Cfg = ReadCfg()
        Catch
        End Try
        PopulateFields()
    End Sub

#End Region

End Class
