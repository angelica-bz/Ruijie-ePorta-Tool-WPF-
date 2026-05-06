Imports System.IO
Imports System.Net
Imports Microsoft.VisualBasic

Public Module ModNetwork

    Private Const DefaultUserAgent As String =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " &
        "AppleWebKit/537.36 (KHTML, like Gecko) " &
        "Chrome/99.0.4844.51 Safari/537.36 Edg/99.0.1150.39"

#Region "网络检测"

    Public Function TestInternet(Optional Host As String = "http://connect.rom.miui.com/generate_204", Optional Timeout As Integer = 1) As Boolean
        Try
            Dim Req As HttpWebRequest = CType(WebRequest.Create(Host), HttpWebRequest)
            Req.Method = "HEAD"
            Req.Timeout = Timeout * 1000
            Req.AllowAutoRedirect = True

            Using Resp As HttpWebResponse = CType(Req.GetResponse(), HttpWebResponse)
                If Host.EndsWith("generate_204") Then
                    Return Resp.StatusCode = HttpStatusCode.NoContent
                End If
                Dim Code As Integer = Resp.StatusCode
                Return (Code >= 200 AndAlso Code <= 208) OrElse Code = 226
            End Using
        Catch ex As Exception
            Return False
        End Try
    End Function

#End Region

#Region "请求头构建"

    Private Function StripScheme(Url As String) As String
        Return Url.Replace("http://", "").Replace("https://", "")
    End Function

    Public Function BuildHeaders(Cfg As Dictionary(Of String, Object)) As Dictionary(Of String, String)
        Dim Server As String = GetDictStr(GetUrlDict(Cfg), ConfigKeys.Server)

        Dim Hostname As String = StripScheme(Server)

        Dim Headers As New Dictionary(Of String, String)
        Headers("Connection") = "keep-alive"
        Headers("User-Agent") = DefaultUserAgent
        Headers("Content-Type") = "application/x-www-form-urlencoded; charset=UTF-8"
        Headers("Accept") = "*/*"
        Headers("Accept-Encoding") = "gzip, deflate"
        Headers("Accept-Language") = "zh-CN,zh;q=0.9,en-US;q=0.8,en;q=0.7"
        Headers("Host") = Hostname
        Headers("Origin") = Server

        Dim Cookie As String = GetDictStr(Cfg, ConfigKeys.Cookie)
        Headers("Cookie") = Cookie

        Dim UserHeaders = GetSubDict(Cfg, ConfigKeys.Headers)
        If UserHeaders IsNot Nothing Then
            For Each Kvp In UserHeaders
                If Kvp.Value IsNot Nothing Then
                    Headers(Kvp.Key) = Kvp.Value.ToString()
                End If
            Next
        End If

        Return Headers
    End Function

#End Region

#Region "POST JSON"

    Private Function EncodeFormData(Data As Dictionary(Of String, String)) As String
        Dim Parts As New List(Of String)
        For Each Kvp In Data
            Parts.Add(Uri.EscapeDataString(Kvp.Key) & "=" & Uri.EscapeDataString(If(Kvp.Value, "")))
        Next
        Return String.Join("&", Parts)
    End Function

    Public Function PostJson(Url As String, Data As Dictionary(Of String, String), Headers As Dictionary(Of String, String), Optional Timeout As Integer = 10) As Dictionary(Of String, Object)
        Dim Result As New Dictionary(Of String, Object)

        Try
            Dim Req As HttpWebRequest = CType(WebRequest.Create(Url), HttpWebRequest)
            Req.Method = "POST"
            Req.Timeout = Timeout * 1000

            For Each Kvp In Headers
                Select Case Kvp.Key.ToLower()
                    Case "content-type"
                        Req.ContentType = Kvp.Value
                    Case "accept"
                        Req.Accept = Kvp.Value
                    Case "user-agent"
                        Req.UserAgent = Kvp.Value
                    Case "host"
                        ' Skip - set by framework
                    Case Else
                        Try
                            Req.Headers(Kvp.Key) = Kvp.Value
                        Catch
                        End Try
                End Select
            Next

            If Data IsNot Nothing AndAlso Data.Count > 0 Then
                Dim PostData As String = EncodeFormData(Data)
                Dim Bytes As Byte() = Text.Encoding.UTF8.GetBytes(PostData)
                Req.ContentLength = Bytes.Length
                Using ReqStream = Req.GetRequestStream()
                    ReqStream.Write(Bytes, 0, Bytes.Length)
                End Using
            Else
                Req.ContentLength = 0
            End If

            Using Resp As HttpWebResponse = CType(Req.GetResponse(), HttpWebResponse)
                Using Reader As New StreamReader(Resp.GetResponseStream(), Text.Encoding.UTF8)
                    Dim Body As String = Reader.ReadToEnd()
                    Return ParseJsonResponse(Body)
                End Using
            End Using

        Catch ex As WebException
            If ex.Response IsNot Nothing Then
                Dim Resp = CType(ex.Response, HttpWebResponse)
                Result("result") = "error"
                Result("message") = "HTTP " & CType(Resp.StatusCode, Integer) & ": " & Resp.StatusDescription
            Else
                Result("result") = "error"
                Result("message") = ex.Message
            End If
        Catch ex As Exception
            Result("result") = "error"
            Result("message") = ex.Message
        End Try

        Return Result
    End Function

#End Region

#Region "登录与断网"

    Public Function Login(Cfg As Dictionary(Of String, Object), Headers As Dictionary(Of String, String)) As Dictionary(Of String, Object)
        Dim UrlCfg = GetUrlDict(Cfg)
        Dim Server As String = GetDictStr(UrlCfg, ConfigKeys.Server)
        Dim LoginPath As String = GetDictStr(UrlCfg, ConfigKeys.Login)
        Dim FullUrl As String = Server & LoginPath
        Dim LoginData = ExtractStringDict(Cfg, ConfigKeys.LoginData)
        Return PostJson(FullUrl, LoginData, Headers)
    End Function

    Public Function Logout(Cfg As Dictionary(Of String, Object), Headers As Dictionary(Of String, String)) As Dictionary(Of String, Object)
        Dim UrlCfg = GetUrlDict(Cfg)
        Dim Server As String = GetDictStr(UrlCfg, ConfigKeys.Server)
        Dim LogoutPath As String = GetDictStr(UrlCfg, ConfigKeys.Logout)
        Dim FullUrl As String = Server & LogoutPath
        Dim LogoutData = ExtractStringDict(Cfg, ConfigKeys.LogoutData)
        Return PostJson(FullUrl, LogoutData, Headers)
    End Function

    Private Function ExtractStringDict(Cfg As Dictionary(Of String, Object), Key As String) As Dictionary(Of String, String)
        Dim Result As New Dictionary(Of String, String)
        If Cfg.ContainsKey(Key) AndAlso TypeOf Cfg(Key) Is Dictionary(Of String, Object) Then
            Dim Src = CType(Cfg(Key), Dictionary(Of String, Object))
            For Each Kvp In Src
                If Kvp.Value IsNot Nothing Then
                    Result(Kvp.Key) = Kvp.Value.ToString()
                End If
            Next
        End If
        Return Result
    End Function

#End Region

#Region "TCP探针"

    Public Function TcpProbe(Url As String, Optional Timeout As Integer = 2) As Boolean
        Try
            Dim Host As String = StripScheme(Url)
            Dim Port As Integer = 80
            Dim ColonIdx As Integer = Host.IndexOf(":"c)
            If ColonIdx > 0 Then
                Integer.TryParse(Host.Substring(ColonIdx + 1), Port)
                Host = Host.Substring(0, ColonIdx)
            End If
            Dim SlashIdx As Integer = Host.IndexOf("/"c)
            If SlashIdx > 0 Then Host = Host.Substring(0, SlashIdx)

            Using Client As New Net.Sockets.TcpClient()
                Dim Ar = Client.BeginConnect(Host, Port, Nothing, Nothing)
                If Ar.AsyncWaitHandle.WaitOne(Timeout * 1000) Then
                    Client.EndConnect(Ar)
                    Return True
                End If
                Return False
            End Using
        Catch
            Return False
        End Try
    End Function

#End Region

End Module
