Imports System.Security.Principal

Module Util

    Function IsRunAsAdmin() As Boolean
        Dim principal As New WindowsPrincipal(WindowsIdentity.GetCurrent)
        Return principal.IsInRole(WindowsBuiltInRole.Administrator)
    End Function

    Sub Logging(ByVal Prompt As String)
        Dim sw As New IO.StreamWriter(Environment.CurrentDirectory & "\Jauth_Log.txt", True, Text.Encoding.UTF8)
        sw.WriteLine($"[{Date.Now}] {Prompt}")
        sw.Close()
        sw.Dispose()
    End Sub

End Module
