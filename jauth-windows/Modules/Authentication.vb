Imports System.Net
Imports Catboy
Imports HtmlAgilityPack

Module Authentication

    Private ini As New ConfigParser With {
        .DefaultFilePath = Environment.CurrentDirectory & "\.jauth_config"
    }

    Public Function Connect() As Boolean
        Try
            Dim reqRdr As New WebClient
            Dim strRdrReturn As String = reqRdr.DownloadString("http://cdn.ralf.ren/res/portal.html")
            Dim strRdrUrl As String = strRdrReturn.Replace("<script>top.self.location.href='", "").Replace("'</script>", "").Trim
            Logging($"[資訊] 獲取認證頁面: {strRdrUrl}")

            Dim reqAuthPage As HttpWebRequest = HttpWebRequest.Create(strRdrUrl)
            reqAuthPage.AllowAutoRedirect = True
            Dim resAuthPage As HttpWebResponse = reqAuthPage.GetResponse
            Dim htmAuthPage As New HtmlDocument
            htmAuthPage.LoadHtml(New IO.StreamReader(resAuthPage.GetResponseStream).ReadToEnd)

            Dim strHost As String = strRdrUrl.Replace("http://", "").Split("/")(0)
            Logging($"[資訊] 獲取認證主機: {strHost}")

            Dim data_mac As String = htmAuthPage.DocumentNode.SelectSingleNode("//input[@id='mac']").Attributes("value").Value
            Logging($"[資訊] 獲取認證資訊 - mac: {data_mac}")
            Dim data_wlancname As String = htmAuthPage.DocumentNode.SelectSingleNode("//input[@id='wlanacname']").Attributes("value").Value
            Logging($"[資訊] 獲取認證資訊 - wlancname: {data_wlancname}")
            Dim data_url As String = htmAuthPage.DocumentNode.SelectSingleNode("//input[@id='url']").Attributes("value").Value
            Logging($"[資訊] 獲取認證資訊 - url: {data_url}")
            Dim data_nasip As String = htmAuthPage.DocumentNode.SelectSingleNode("//input[@id='nasip']").Attributes("value").Value
            Logging($"[資訊] 獲取認證資訊 - nasip: {data_nasip}")
            Dim data_wlanuserip As String = htmAuthPage.DocumentNode.SelectSingleNode("//input[@id='wlanuserip']").Attributes("value").Value
            Logging($"[資訊] 獲取認證資訊 - wlanuserip: {data_wlanuserip}")

            Dim data_username As String = ini.GetValue("account", "username", "00000000")
            Logging($"[資訊] 獲取認證資訊 - username: {data_username}")
            Dim data_pwd As String = ini.GetValue("account", "password", "000000")
            Logging($"[資訊] 獲取認證資訊 - pwd: {data_pwd}")

            Dim strParam As String = $"qrCodeId=请输入编号&username={data_username}&pwd={data_pwd}&validCode=验证码&validCodeFlag=false&ssid=&mac={data_mac}&t=wireless-v2&wlanacname={data_wlancname}&url={data_url}&nasip={data_nasip}&wlanuserip={data_wlanuserip}"
            Dim bytParam() As Byte = Text.Encoding.UTF8.GetBytes(strParam)
            Dim reqAuth As HttpWebRequest = HttpWebRequest.Create($"http://{strHost}/zportal/login/do")
            reqAuth.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36"
            reqAuth.AllowAutoRedirect = True
            reqAuth.Method = "POST"
            reqAuth.ContentType = "application/x-www-form-urlencoded"
            Using swAuth As IO.Stream = reqAuth.GetRequestStream
                swAuth.Write(bytParam, 0, bytParam.Length)
            End Using

            Logging($"[資訊] 發送認證資訊")
            Dim resResult As HttpWebResponse = reqAuth.GetResponse
            Dim strResult As String = New IO.StreamReader(resResult.GetResponseStream).ReadToEnd
            Logging($"[資訊] 得到返回結果: {strResult}")

            If strResult.Contains("""result"":""success""") Then
                Logging($"[資訊] 認證成功")

                Dim data_jsessionid As String = resResult.Headers.Get("Set-Cookie").Split(";")(0).Split("=")(1)
                Logging($"[資訊] 會話 ID: {data_jsessionid}")

                Dim cooks As New CookieContainer()
                cooks.Add(New Cookie("JSESSIONID", data_jsessionid, "/zportal/", strHost.Split(":")(0)))
                Dim reqSuccess As HttpWebRequest = HttpWebRequest.Create($"http://{strHost}/zportal/goToAuthResult")
                reqSuccess.Method = "GET"
                reqSuccess.CookieContainer = cooks

                Dim resSuccess As HttpWebResponse = reqSuccess.GetResponse
                Dim htmSuccess As New HtmlDocument
                htmSuccess.LoadHtml(New IO.StreamReader(resSuccess.GetResponseStream).ReadToEnd)

                Dim data_usermac As String = htmSuccess.DocumentNode.SelectSingleNode("//input[@id='userMac']").Attributes("value").Value
                Logging($"[資訊] 提取會話資訊 - userMac: {data_usermac}")
                Dim data_userip As String = htmSuccess.DocumentNode.SelectSingleNode("//input[@name='userIp']").Attributes("value").Value
                Logging($"[資訊] 提取會話資訊 - userIp: {data_userip}")
                Dim data_deviceip As String = htmSuccess.DocumentNode.SelectSingleNode("//input[@name='deviceIp']").Attributes("value").Value
                Logging($"[資訊] 提取會話資訊 - deviceIp: {data_deviceip}")

                ini.SetValue("last", "host", strHost)
                ini.SetValue("last", "userip", data_userip)
                ini.SetValue("last", "deviceip", data_deviceip)
                ini.SetValue("last", "usermac", data_usermac)
                ini.SetValue("last", "jsessionid", data_jsessionid)
                Logging($"[資訊] 已存儲會話資訊")

                Return True
            Else
                Logging($"[錯誤] 認證失敗")
                Return False
            End If
        Catch ex As Exception
        Logging($"[錯誤] 認證中出現問題: {ex.Message}")
        Return False
        End Try

    End Function

    Public Function Disconnect() As Boolean
        Try

            Dim data_username As String = ini.GetValue("account", "username", "00000000")
            Logging($"[資訊] 會話資訊 - userName: {data_username}, 載入.")
            Dim data_userip As String = ini.GetValue("last", "userip", "")
            Logging($"[資訊] 會話資訊 - userIp: {data_userip}, 載入.")
            Dim data_deviceip As String = ini.GetValue("last", "deviceip", "")
            Logging($"[資訊] 會話資訊 - deviceIp: {data_deviceip}, 載入.")
            Dim data_usermac As String = ini.GetValue("last", "usermac", "")
            Logging($"[資訊] 會話資訊 - userMac: {data_usermac}, 載入.")
            Dim data_jsessionid As String = ini.GetValue("last", "jsessionid", "")
            Logging($"[資訊] 會話 ID: {data_jsessionid}, 載入.")
            Dim strHost As String = ini.GetValue("last", "host", "")
            Logging($"[資訊] 認證主機: {strHost}, 載入.")

            Dim strParam As String = $"userName={data_username}&service.id=&autoLoginFlag=false&userIp={data_userip}&deviceIp={data_deviceip}&userMac={data_usermac}&operationType=&isMacFastAuth=false"
            Dim bytParam() As Byte = Text.Encoding.UTF8.GetBytes(strParam)
            Dim cooks As New CookieContainer()
            cooks.Add(New Cookie("JSESSIONID", data_jsessionid, "/zportal/", strHost.Split(":")(0)))

            Dim reqDeauth As HttpWebRequest = HttpWebRequest.Create($"http://{strHost}/zportal/logout")
            reqDeauth.CookieContainer = cooks
            reqDeauth.Method = "POST"
            reqDeauth.ContentType = "application/x-www-form-urlencoded"
            Using swDeAuth As IO.Stream = reqDeauth.GetRequestStream
                swDeAuth.Write(bytParam, 0, bytParam.Length)
            End Using

            Logging($"[資訊] 發送解除認證資訊")
            Dim resDeauth As HttpWebResponse = reqDeauth.GetResponse
            Dim htmSuccess As String = New IO.StreamReader(resDeauth.GetResponseStream).ReadToEnd

            If htmSuccess.Contains("已下线") Then
                Logging($"[資訊] 已解除認證")
                Return True
            Else
                Logging($"[錯誤] 解除認證失敗")
                Return False
            End If
        Catch ex As Exception
            Logging($"[錯誤] 解除認證中出現問題: {ex.Message}")
        Return False
        End Try
    End Function

End Module
