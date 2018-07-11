Public Class INetAvailWatcher

    Public Structure PortalSettings
        Dim RequestURL As String
        Dim SuccessReturn As String

        Public Overrides Function ToString() As String
            Return $"{RequestURL},{SuccessReturn}"
        End Function

    End Structure

    Public Property InternetChecker As PortalSettings

    Public Sub New()
        InternetChecker = New PortalSettings With {
            .RequestURL = "http://cdn.ralf.ren/res/portal.html",
            .SuccessReturn = "Success"
        }
    End Sub

    Public Sub New(ByVal PortalSettings As PortalSettings)
        InternetChecker = PortalSettings
    End Sub

    Public Sub New(ByVal RequestURL As String, ByVal SuccessReturn As String)
        InternetChecker = New PortalSettings With {
            .RequestURL = RequestURL,
            .SuccessReturn = SuccessReturn
        }
    End Sub

    Public Function CheckAvailibility() As Boolean
        Dim req As New Net.WebClient
        Dim ret As String
        Try
            ret = req.DownloadString(InternetChecker.RequestURL)
            If Not ret = InternetChecker.SuccessReturn Then
                Return False
            Else
                Return True
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

End Class
