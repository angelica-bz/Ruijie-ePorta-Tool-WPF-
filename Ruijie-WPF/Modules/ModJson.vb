Imports System.Web.Script.Serialization

Public Module ModJson

    Private ReadOnly Serializer As New JavaScriptSerializer()

    Public Function ParseJsonResponse(Body As String) As Dictionary(Of String, Object)
        If Body Is Nothing OrElse Body.Trim() = "" Then
            Return New Dictionary(Of String, Object) From {
                {"result", "error"},
                {"message", "服务器返回了空响应"}
            }
        End If

        Try
            Dim Result = Serializer.Deserialize(Of Dictionary(Of String, Object))(Body.Trim())
            If Result IsNot Nothing Then Return Result
        Catch
        End Try

        Return New Dictionary(Of String, Object) From {
            {"result", "error"},
            {"message", "服务器返回了非 JSON 响应（可能服务异常或需重新认证）"}
        }
    End Function

End Module
