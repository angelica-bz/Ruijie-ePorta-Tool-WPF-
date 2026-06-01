Imports System.IO
Imports Microsoft.VisualBasic

Public Module ModConfig

    Public Const CurrentConfigVersion As Integer = 3

    Private ReadOnly DefaultConfigTemplate As String =
        "# 本配置文件内容需要根据学校服务器设置动态调整" & vbCrLf &
        "main:" & vbCrLf &
        "  version: 3 # 配置文件版本号，请勿更改" & vbCrLf &
        "" & vbCrLf &
        "function:" & vbCrLf &
        "  auto_reconnect: false # 断网后是否自动重连" & vbCrLf &
        "  reconnect_interval: 5 # 网络检测间隔（秒），同时也是断网重试间隔" & vbCrLf &
        "" & vbCrLf &
        "url:" & vbCrLf &
        "  server: http://127.0.0.1 # 校园网登录服务器的地址" & vbCrLf &
        "  login: /eportal/InterFace.do?method=login # 校园网登录地址" & vbCrLf &
        "  logout: /eportal/InterFace.do?method=logout # 校园网断线地址" & vbCrLf &
        "" & vbCrLf &
        "# 请根据抓包结果调整条目与内容" & vbCrLf &
        "cookie: ''" & vbCrLf &
        "" & vbCrLf &
        "# 请根据抓包结果调整条目与内容（?key1=value1&key2=value2）" & vbCrLf &
        "# 请不要在密码栏填入明文密码" & vbCrLf &
        "login_data:" & vbCrLf &
        "  userId: '00000000000'" & vbCrLf &
        "  password:" & vbCrLf &
        "  service:" & vbCrLf &
        "  queryString:" & vbCrLf &
        "  operatorPwd:" & vbCrLf &
        "  operatorUserId:" & vbCrLf &
        "  validcode:" & vbCrLf &
        "  passwordEncrypt: true" & vbCrLf &
        "" & vbCrLf &
        "# 一般来说不填参数也能用，但请不要删除（?key1=value1&key2=value2）" & vbCrLf &
        "logout_data:" & vbCrLf &
        "" & vbCrLf &
        "headers:" & vbCrLf &
        "  Referer:" & vbCrLf

#Region "路径"

    Public Function GetConfigPath() As String
        Return Path.Combine(PathExeFolder, "config.yml")
    End Function

    Public Function GetLogsDir() As String
        Return Path.Combine(PathExeFolder, "logs")
    End Function

#End Region

#Region "读取配置"

    Public Function ReadCfg(Optional GuiMode As Boolean = False) As Dictionary(Of String, Object)
        Dim ConfigPath As String = GetConfigPath()

        If Not File.Exists(ConfigPath) Then
            WriteDefaultConfig(ConfigPath)
            If GuiMode Then Return GetDefaultConfig()
            Return GetDefaultConfig()
        End If

        Dim Lines As String()
        Try
            Lines = File.ReadAllLines(ConfigPath, Text.Encoding.UTF8)
        Catch ex As Exception
            Log(ex, "读取配置文件失败")
            If GuiMode Then Return GetDefaultConfig()
            Return GetDefaultConfig()
        End Try

        Dim Cfg = ParseYamlLines(Lines)

        If Cfg Is Nothing OrElse Cfg.Count = 0 Then
            WriteDefaultConfig(ConfigPath)
            If GuiMode Then Return GetDefaultConfig()
            Return GetDefaultConfig()
        End If

        ValidateConfig(Cfg, GuiMode)
        NormalizeConfig(Cfg)
        Return Cfg
    End Function

#End Region

#Region "YAML 手动解析"

    Private Function ParseYamlLines(Lines As String()) As Dictionary(Of String, Object)
        Dim Result As New Dictionary(Of String, Object)
        Dim CurrentSection As String = ""
        Dim CurrentDict As Dictionary(Of String, Object) = Nothing

        For Each RawLine As String In Lines
            Dim Line As String = RawLine.TrimEnd(vbCr, vbLf)
            Dim Trimmed As String = Line.TrimStart()

            If Trimmed = "" OrElse Trimmed.StartsWith("#") Then Continue For

            Dim Indent As Integer = Line.Length - Line.TrimStart().Length

            If Indent = 0 AndAlso Trimmed.EndsWith(":") Then
                CurrentSection = Trimmed.TrimEnd(":"c)
                CurrentDict = New Dictionary(Of String, Object)
                Result(CurrentSection) = CurrentDict
                Continue For
            End If

            If Indent = 0 AndAlso Trimmed.Contains(":") Then
                Dim ColonIdx As Integer = Trimmed.IndexOf(":"c)
                Dim Key As String = Trimmed.Substring(0, ColonIdx).Trim()
                Dim Value As String = Trimmed.Substring(ColonIdx + 1).Trim()
                Value = StripInlineComment(Value)
                Value = TrimQuoted(Value)
                Result(Key) = ParseValue(Value)
                Continue For
            End If

            If Indent > 0 AndAlso CurrentDict IsNot Nothing AndAlso Trimmed.Contains(":") Then
                Dim ColonIdx As Integer = Trimmed.IndexOf(":"c)
                Dim Key As String = Trimmed.Substring(0, ColonIdx).Trim()
                Dim Value As String = Trimmed.Substring(ColonIdx + 1).Trim()
                Value = StripInlineComment(Value)
                Value = TrimQuoted(Value)
                CurrentDict(Key) = ParseValue(Value)
                Continue For
            End If
        Next

        Return Result
    End Function

    Private Function StripInlineComment(Value As String) As String
        If Value = "" Then Return Value
        Dim InQuote As Boolean = False
        Dim QuoteChar As Char = Nothing
        For i As Integer = 0 To Value.Length - 1
            Dim c As Char = Value(i)
            If InQuote Then
                If c = QuoteChar Then InQuote = False
            Else
                If c = "'"c OrElse c = """"c Then
                    InQuote = True
                    QuoteChar = c
                ElseIf c = "#"c Then
                    Return Value.Substring(0, i).Trim()
                End If
            End If
        Next
        Return Value
    End Function

    Private Function TrimQuoted(Value As String) As String
        If Value.Length >= 2 Then
            If (Value.StartsWith("'") AndAlso Value.EndsWith("'")) OrElse
               (Value.StartsWith("""") AndAlso Value.EndsWith("""")) Then
                Return Value.Substring(1, Value.Length - 2)
            End If
        End If
        Return Value
    End Function

    Private Function ParseValue(Value As String) As Object
        If Value = "" Then Return Nothing
        Dim Lower = Value.ToLower()
        If Lower = "true" Then Return True
        If Lower = "false" Then Return False
        If Lower = "null" OrElse Lower = "~" Then Return Nothing
        If Value.Length > 1 AndAlso Value.StartsWith("0") AndAlso Not Value.Contains(".") Then Return Value
        Dim IntVal As Integer
        If Integer.TryParse(Value, IntVal) Then Return IntVal
        Dim DblVal As Double
        If Double.TryParse(Value, Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, DblVal) Then Return DblVal
        Return Value
    End Function

#End Region

#Region "写入配置"

    Public Sub WriteCfg(Cfg As Dictionary(Of String, Object))
        Dim ConfigPath As String = GetConfigPath()
        Dim Lines As String()
        Try
            Lines = File.ReadAllLines(ConfigPath, Text.Encoding.UTF8)
        Catch ex As Exception
            Log(ex, "读取配置文件失败，无法写入")
            Return
        End Try

        Dim AutoReconnectVal As String = "false"
        Dim ReconnectIntervalVal As String = "5"

        Dim FunctionCfg = GetFunctionDict(Cfg)
        If FunctionCfg IsNot Nothing Then
            If FunctionCfg.ContainsKey(ConfigKeys.AutoReconnect) Then AutoReconnectVal = FunctionCfg(ConfigKeys.AutoReconnect).ToString().ToLower()
            If FunctionCfg.ContainsKey(ConfigKeys.ReconnectInterval) Then ReconnectIntervalVal = FunctionCfg(ConfigKeys.ReconnectInterval).ToString()
        End If

        Dim HasAuto As Boolean = False
        Dim HasInterval As Boolean = False
        Dim NewLines As New List(Of String)

        For Each Line As String In Lines
            Dim Trimmed As String = Line.TrimStart()
            If Trimmed.StartsWith("auto_reconnect:") Then
                Dim Indent As String = Line.Substring(0, Line.Length - Trimmed.Length)
                NewLines.Add(Indent & "auto_reconnect: " & AutoReconnectVal)
                HasAuto = True
            ElseIf Trimmed.StartsWith("reconnect_interval:") Then
                Dim Indent As String = Line.Substring(0, Line.Length - Trimmed.Length)
                NewLines.Add(Indent & "reconnect_interval: " & ReconnectIntervalVal)
                HasInterval = True
            Else
                NewLines.Add(Line)
            End If
        Next

        If Not HasAuto OrElse Not HasInterval Then
            Dim InsertLines As New List(Of String)
            For i As Integer = 0 To NewLines.Count - 1
                InsertLines.Add(NewLines(i))
                If NewLines(i).TrimStart().StartsWith("disconnect_network:") Then
                    If Not HasAuto Then
                        Dim Indent As String = NewLines(i).Substring(0, NewLines(i).Length - NewLines(i).TrimStart().Length)
                        InsertLines.Add(Indent & "auto_reconnect: " & AutoReconnectVal)
                        InsertLines.Add(Indent & "reconnect_interval: " & ReconnectIntervalVal)
                        HasAuto = True : HasInterval = True
                    End If
                End If
            Next
            NewLines = InsertLines
        End If

        Try
            File.WriteAllText(ConfigPath, String.Join(vbCrLf, NewLines) & vbCrLf, Text.Encoding.UTF8)
        Catch ex As Exception
            Log(ex, "写入配置文件失败")
        End Try
    End Sub

    Public Sub SaveConfig(Cfg As Dictionary(Of String, Object))
        Dim ConfigPath As String = GetConfigPath()
        Dim Header As String =
            "# 本配置文件内容需要根据学校服务器设置动态调整" & vbCrLf &
            "# 通过 GUI 配置面板生成／修改" & vbCrLf &
            "# 请不要在密码栏填入明文密码" & vbCrLf & vbCrLf
        Dim YamlStr As String = DumpYaml(Cfg)
        Try
            File.WriteAllText(ConfigPath, Header & YamlStr, Text.Encoding.UTF8)
        Catch ex As Exception
            Log(ex, "保存配置文件失败")
        End Try
    End Sub

    Private Function DumpYaml(Cfg As Dictionary(Of String, Object), Optional Indent As Integer = 0) As String
        Dim Sb As New Text.StringBuilder()
        Dim Prefix As String = New String(" "c, Indent)
        For Each Kvp In Cfg
            If TypeOf Kvp.Value Is Dictionary(Of String, Object) Then
                Sb.AppendLine(Prefix & Kvp.Key & ":")
                Sb.Append(DumpYaml(CType(Kvp.Value, Dictionary(Of String, Object)), Indent + 2))
            ElseIf Kvp.Value Is Nothing Then
                Sb.AppendLine(Prefix & Kvp.Key & ":")
            ElseIf TypeOf Kvp.Value Is Boolean Then
                Sb.AppendLine(Prefix & Kvp.Key & ": " & Kvp.Value.ToString().ToLower())
            Else
                Dim Val As String = Kvp.Value.ToString()
                If Val.Contains(":") OrElse Val.Contains("#") OrElse Val = "" Then
                    Sb.AppendLine(Prefix & Kvp.Key & ": '" & Val & "'")
                Else
                    Sb.AppendLine(Prefix & Kvp.Key & ": " & Val)
                End If
            End If
        Next
        Return Sb.ToString()
    End Function

    Private Sub WriteDefaultConfig(ConfigPath As String)
        Try
            Dim Dir = Path.GetDirectoryName(ConfigPath)
            If Not Directory.Exists(Dir) Then Directory.CreateDirectory(Dir)
            File.WriteAllText(ConfigPath, DefaultConfigTemplate, Text.Encoding.UTF8)
        Catch ex As Exception
            Log(ex, "写入默认配置文件失败")
        End Try
    End Sub

#End Region

#Region "默认配置与校验"

    Public Function GetDefaultConfig() As Dictionary(Of String, Object)
        Dim Cfg As New Dictionary(Of String, Object)
        Cfg(ConfigKeys.Main) = New Dictionary(Of String, Object) From {{ConfigKeys.Version, CurrentConfigVersion}}
        Cfg(ConfigKeys.FunctionSection) = New Dictionary(Of String, Object) From {
            {ConfigKeys.AutoReconnect, False},
            {ConfigKeys.ReconnectInterval, 5}
        }
        Cfg(ConfigKeys.Url) = New Dictionary(Of String, Object) From {
            {ConfigKeys.Server, "http://127.0.0.1"},
            {ConfigKeys.Login, "/eportal/InterFace.do?method=login"},
            {ConfigKeys.Logout, "/eportal/InterFace.do?method=logout"}
        }
        Cfg(ConfigKeys.Cookie) = ""
        Cfg(ConfigKeys.LoginData) = New Dictionary(Of String, Object) From {
            {"userId", "00000000000"},
            {"password", Nothing},
            {"service", Nothing},
            {"queryString", Nothing},
            {"operatorPwd", Nothing},
            {"operatorUserId", Nothing},
            {"validcode", Nothing},
            {"passwordEncrypt", "true"}
        }
        Cfg(ConfigKeys.LogoutData) = New Dictionary(Of String, Object)
        Cfg(ConfigKeys.Headers) = New Dictionary(Of String, Object) From {{"Referer", ""}}
        Return Cfg
    End Function

    Public Sub ValidateConfig(Cfg As Dictionary(Of String, Object), GuiMode As Boolean)
        Dim ConfigVersion As Integer = 0
        Dim Main = GetSubDict(Cfg, ConfigKeys.Main)
        If Main IsNot Nothing AndAlso Main.ContainsKey(ConfigKeys.Version) Then
            Integer.TryParse(Main(ConfigKeys.Version).ToString(), ConfigVersion)
        End If

        If ConfigVersion < CurrentConfigVersion Then
            Log("警告：配置文件格式过期，请备份并删除原配置文件后重新生成")
        End If
        If ConfigVersion > CurrentConfigVersion Then
            Log("警告：配置文件版本过高，请更新本程序")
        End If

        Dim LoginData = GetSubDict(Cfg, ConfigKeys.LoginData)
        If LoginData Is Nothing Then
            Cfg(ConfigKeys.LoginData) = New Dictionary(Of String, Object)
        ElseIf LoginData.ContainsKey("userId") Then
            Dim Uid = LoginData("userId")
            If Uid Is Nothing OrElse Uid.ToString() = "" OrElse Uid.ToString() = "00000000000" Then
                Log("警告：配置文件未正确填写 userId")
            End If
        End If
    End Sub

    Public Sub NormalizeConfig(Cfg As Dictionary(Of String, Object))
        Dim Url = GetUrlDict(Cfg)
        If Url IsNot Nothing Then
            If Url.ContainsKey(ConfigKeys.Server) AndAlso Url(ConfigKeys.Server) IsNot Nothing Then
                Dim Server As String = Url(ConfigKeys.Server).ToString()
                If Server.EndsWith("/") Then Url(ConfigKeys.Server) = Server.TrimEnd("/"c)
            End If
        End If

        Dim LoginData = GetSubDict(Cfg, ConfigKeys.LoginData)
        ' LoginData 保留原始类型，GetDictStr/GetDictBool/GetDictInt 已兼容

        If Cfg.ContainsKey(ConfigKeys.LogoutData) Then
            If Cfg(ConfigKeys.LogoutData) Is Nothing Then
                Cfg(ConfigKeys.LogoutData) = New Dictionary(Of String, Object)
            End If
        Else
            Cfg(ConfigKeys.LogoutData) = New Dictionary(Of String, Object)
        End If

        If Cfg.ContainsKey(ConfigKeys.Headers) Then
            If Cfg(ConfigKeys.Headers) Is Nothing Then
                Cfg(ConfigKeys.Headers) = New Dictionary(Of String, Object)
            End If
        Else
            Cfg(ConfigKeys.Headers) = New Dictionary(Of String, Object)
        End If

        If GetFunctionDict(Cfg) Is Nothing Then
            Cfg(ConfigKeys.FunctionSection) = New Dictionary(Of String, Object)
        End If
        Dim FunctionCfg = CType(Cfg(ConfigKeys.FunctionSection), Dictionary(Of String, Object))
        If Not FunctionCfg.ContainsKey(ConfigKeys.AutoReconnect) Then FunctionCfg(ConfigKeys.AutoReconnect) = False
        If Not FunctionCfg.ContainsKey(ConfigKeys.ReconnectInterval) Then FunctionCfg(ConfigKeys.ReconnectInterval) = 5
    End Sub

#End Region

#Region "配置访问 Helpers"

    Public Class ConfigKeys
        Public Const Main As String = "main"
        Public Const Version As String = "version"
        Public Const FunctionSection As String = "function"
        Public Const AutoReconnect As String = "auto_reconnect"
        Public Const ReconnectInterval As String = "reconnect_interval"
        Public Const Url As String = "url"
        Public Const Server As String = "server"
        Public Const Login As String = "login"
        Public Const Logout As String = "logout"
        Public Const Cookie As String = "cookie"
        Public Const LoginData As String = "login_data"
        Public Const LogoutData As String = "logout_data"
        Public Const Headers As String = "headers"
    End Class

    Public Function GetSubDict(Dict As Dictionary(Of String, Object), Key As String) As Dictionary(Of String, Object)
        If Dict Is Nothing Then Return Nothing
        If Dict.ContainsKey(Key) AndAlso TypeOf Dict(Key) Is Dictionary(Of String, Object) Then
            Return CType(Dict(Key), Dictionary(Of String, Object))
        End If
        Return Nothing
    End Function

    Public Function GetDictStr(Dict As Dictionary(Of String, Object), Key As String, Optional DefaultValue As String = "") As String
        If Dict Is Nothing Then Return DefaultValue
        If Dict.ContainsKey(Key) AndAlso Dict(Key) IsNot Nothing Then
            Return Dict(Key).ToString()
        End If
        Return DefaultValue
    End Function

    Public Function GetDictBool(Dict As Dictionary(Of String, Object), Key As String, Optional DefaultValue As Boolean = False) As Boolean
        If Dict Is Nothing Then Return DefaultValue
        If Dict.ContainsKey(Key) Then
            Dim Val = Dict(Key)
            If Val Is Nothing Then Return DefaultValue
            If TypeOf Val Is Boolean Then Return CBool(Val)
            Dim s = Val.ToString().ToLower()
            Return s = "true"
        End If
        Return DefaultValue
    End Function

    Public Function GetDictInt(Dict As Dictionary(Of String, Object), Key As String, Optional DefaultValue As Integer = 0) As Integer
        If Dict Is Nothing Then Return DefaultValue
        If Dict.ContainsKey(Key) AndAlso Dict(Key) IsNot Nothing Then
            Dim IntVal As Integer
            If Integer.TryParse(Dict(Key).ToString(), IntVal) Then Return IntVal
        End If
        Return DefaultValue
    End Function

    Public Function GetUrlDict(Cfg As Dictionary(Of String, Object)) As Dictionary(Of String, Object)
        Return GetSubDict(Cfg, ConfigKeys.Url)
    End Function

    Public Function GetFunctionDict(Cfg As Dictionary(Of String, Object)) As Dictionary(Of String, Object)
        Return GetSubDict(Cfg, ConfigKeys.FunctionSection)
    End Function

#End Region

End Module
