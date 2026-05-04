Imports Microsoft.VisualBasic

Public Module ModJson

    Public Function ParseJsonResponse(Body As String) As Dictionary(Of String, Object)
        Dim Result As New Dictionary(Of String, Object)
        If Body Is Nothing OrElse Body.Trim() = "" Then
            Result("result") = "error"
            Result("message") = "服务器返回了空响应"
            Return Result
        End If

        Try
            Body = Body.Trim()
            If Body.StartsWith("{") Then
                Return ParseJsonObject(Body)
            End If
        Catch
        End Try

        Result("result") = "error"
        Result("message") = "服务器返回了非 JSON 响应（可能服务异常或需重新认证）"
        Return Result
    End Function

    Private Function ParseJsonObject(Json As String) As Dictionary(Of String, Object)
        Dim Pos As Integer = 0
        Return ParseObject(Json, Pos)
    End Function

    Private Function ParseObject(Json As String, ByRef Pos As Integer) As Dictionary(Of String, Object)
        Dim Result As New Dictionary(Of String, Object)
        SkipWhitespace(Json, Pos)
        If Pos >= Json.Length OrElse Json(Pos) <> "{"c Then Return Result
        Pos += 1
        SkipWhitespace(Json, Pos)

        If Pos < Json.Length AndAlso Json(Pos) = "}"c Then
            Pos += 1 : Return Result
        End If

        Do
            SkipWhitespace(Json, Pos)
            Dim Key As String = ParseString(Json, Pos)
            SkipWhitespace(Json, Pos)
            If Pos >= Json.Length OrElse Json(Pos) <> ":"c Then Exit Do
            Pos += 1
            SkipWhitespace(Json, Pos)
            Result(Key) = ParseValue(Json, Pos)
            SkipWhitespace(Json, Pos)
            If Pos < Json.Length AndAlso Json(Pos) = ","c Then
                Pos += 1
            Else
                Exit Do
            End If
        Loop

        SkipWhitespace(Json, Pos)
        If Pos < Json.Length AndAlso Json(Pos) = "}"c Then Pos += 1
        Return Result
    End Function

    Private Function ParseValue(Json As String, ByRef Pos As Integer) As Object
        SkipWhitespace(Json, Pos)
        If Pos >= Json.Length Then Return Nothing
        Dim c As Char = Json(Pos)
        If c = """"c Then Return ParseString(Json, Pos)
        If c = "{"c Then Return ParseObject(Json, Pos)
        If c = "["c Then Return ParseArray(Json, Pos)
        If c = "n"c Then
            If Json.Substring(Pos, 4) = "null" Then Pos += 4 : Return Nothing
        End If
        If c = "t"c Then
            If Json.Substring(Pos, 4) = "true" Then Pos += 4 : Return True
        End If
        If c = "f"c Then
            If Json.Substring(Pos, 5) = "false" Then Pos += 5 : Return False
        End If
        Return ParseNumber(Json, Pos)
    End Function

    Private Function ParseString(Json As String, ByRef Pos As Integer) As String
        If Pos >= Json.Length OrElse Json(Pos) <> """"c Then Return ""
        Pos += 1
        Dim sb As New System.Text.StringBuilder()
        Do While Pos < Json.Length
            If Json(Pos) = "\"c Then
                Pos += 1
                If Pos >= Json.Length Then Exit Do
                Select Case Json(Pos)
                    Case """"c : sb.Append(""""c)
                    Case "\"c : sb.Append("\"c)
                    Case "/"c : sb.Append("/"c)
                    Case "n"c : sb.Append(vbLf)
                    Case "r"c : sb.Append(vbCr)
                    Case "t"c : sb.Append(vbTab)
                    Case "b"c : sb.Append(Chr(8))
                    Case "f"c : sb.Append(Chr(12))
                    Case Else : sb.Append("\"c).Append(Json(Pos))
                End Select
                Pos += 1
            ElseIf Json(Pos) = """"c Then
                Pos += 1
                Return sb.ToString()
            Else
                sb.Append(Json(Pos))
                Pos += 1
            End If
        Loop
        Return sb.ToString()
    End Function

    Private Function ParseNumber(Json As String, ByRef Pos As Integer) As Object
        Dim Start As Integer = Pos
        Dim HasDot As Boolean = False
        While Pos < Json.Length
            Dim c As Char = Json(Pos)
            If Char.IsDigit(c) OrElse c = "-"c OrElse c = "+"c Then
                Pos += 1
            ElseIf c = "."c AndAlso Not HasDot Then
                HasDot = True : Pos += 1
            Else
                Exit While
            End If
        End While
        Dim NumStr As String = Json.Substring(Start, Pos - Start)
        If HasDot Then
            Dim Dbl As Double
            If Double.TryParse(NumStr, Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, Dbl) Then Return Dbl
        Else
            Dim IntVal As Integer
            If Integer.TryParse(NumStr, IntVal) Then Return IntVal
            Dim Lng As Long
            If Long.TryParse(NumStr, Lng) Then Return Lng
        End If
        Return NumStr
    End Function

    Private Function ParseArray(Json As String, ByRef Pos As Integer) As Object
        Dim Result As New List(Of Object)
        If Pos >= Json.Length OrElse Json(Pos) <> "["c Then Return Result
        Pos += 1
        SkipWhitespace(Json, Pos)
        If Pos < Json.Length AndAlso Json(Pos) = "]"c Then Pos += 1 : Return Result
        Do
            SkipWhitespace(Json, Pos)
            Result.Add(ParseValue(Json, Pos))
            SkipWhitespace(Json, Pos)
            If Pos < Json.Length AndAlso Json(Pos) = ","c Then
                Pos += 1
            Else
                Exit Do
            End If
        Loop
        SkipWhitespace(Json, Pos)
        If Pos < Json.Length AndAlso Json(Pos) = "]"c Then Pos += 1
        Return Result
    End Function

    Private Sub SkipWhitespace(Json As String, ByRef Pos As Integer)
        While Pos < Json.Length AndAlso (Json(Pos) = " "c OrElse Json(Pos) = vbLf OrElse Json(Pos) = vbCr OrElse Json(Pos) = vbTab)
            Pos += 1
        End While
    End Sub

End Module
