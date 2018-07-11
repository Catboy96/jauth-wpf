Imports System.Windows.Forms
Imports Catboy

Class ConfigWindow

    Private WithEvents watcher As New INetAvailWatcher
    Private QuitFlag As Boolean = False
    Private ini As New ConfigParser
    Private icoTray As New NotifyIcon With {
            .Visible = True
    }
    Private tim As New Forms.Timer With {
        .Interval = 5000,
        .Enabled = True
    }

#Region "UI Changes"
    Private Sub txtUsername_GotFocus(sender As Object, e As RoutedEventArgs)
        txtUsername.Foreground = Brushes.Black
    End Sub

    Private Sub txtUsername_LostFocus(sender As Object, e As RoutedEventArgs)
        txtUsername.Foreground = (New BrushConverter).ConvertFrom("#8F8F94")
    End Sub

    Private Sub txtPassword_GotFocus(sender As Object, e As RoutedEventArgs)
        txtPassword.Foreground = Brushes.Black
    End Sub

    Private Sub txtPassword_LostFocus(sender As Object, e As RoutedEventArgs)
        txtPassword.Foreground = (New BrushConverter).ConvertFrom("#8F8F94")
    End Sub

    Private Sub togStartUp_Click(sender As Object, e As RoutedEventArgs)
        If Not IsRunAsAdmin() Then
            MessageBox.Show("要更改自動啟動設定，使用管理員身分執行 Jauth。", "", MessageBoxButtons.OK, MessageBoxIcon.Information)
            togStartUp.IsChecked = False
        End If
    End Sub

    Private Sub btnQuit_Click(sender As Object, e As RoutedEventArgs)
        QuitFlag = True
        Me.Close()
    End Sub

    Private Sub btnLog_Click(sender As Object, e As RoutedEventArgs)
        If Not IO.File.Exists(Environment.CurrentDirectory & "\Jauth_Log.txt") Then Exit Sub
        Process.Start(Environment.CurrentDirectory & "\Jauth_Log.txt")
    End Sub



#End Region

#Region "Windows"

    Private Sub Window_Initialized(sender As Object, e As EventArgs)
        Dim mnuTray As New ContextMenu

        Dim itmConnect As New MenuItem With {
            .Text = "連線 (&C)",
            .Index = 0
        }
        AddHandler itmConnect.Click, AddressOf itmConnect_Click

        Dim itmDisconnect As New MenuItem With {
            .Text = "斷開 (&D)",
            .Index = 1
        }
        AddHandler itmDisconnect.Click, AddressOf itmDisconnect_Click

        Dim itmShowWindow As New MenuItem With {
            .Text = "設定 ... (&S)",
            .Index = 2
        }
        AddHandler itmShowWindow.Click, AddressOf itmShowWindow_Click

        Dim itmLog As New MenuItem With {
            .Text = "連線履歷 ...(&L)",
            .Index = 3
        }
        AddHandler itmLog.Click, AddressOf itmLog_Click

        Dim itmQuit As New MenuItem With {
            .Text = "退出 Jauth (&Q)",
            .Index = 4
        }
        AddHandler itmQuit.Click, AddressOf itmQuit_Click

        mnuTray.MenuItems.Add(itmConnect)
        mnuTray.MenuItems.Add(itmDisconnect)
        mnuTray.MenuItems.Add("-")
        mnuTray.MenuItems.Add(itmShowWindow)
        mnuTray.MenuItems.Add(itmLog)
        mnuTray.MenuItems.Add("-")
        mnuTray.MenuItems.Add(itmQuit)

        icoTray.ContextMenu = mnuTray

        If IO.File.Exists(Environment.CurrentDirectory & "\Jauth_Log.txt") Then
            IO.File.Delete(Environment.CurrentDirectory & "\Jauth_Log.txt")
        End If
        Logging("[資訊] Jauth 已啟動.")

        If watcher.CheckAvailibility = True Then
            icoTray.Icon = My.Resources.lan_connect
            icoTray.Text = "Jauth: 已連線"
            Logging("[資訊] 偵測到網際網路連線")
        Else
            icoTray.Icon = My.Resources.lan_disconnect
            icoTray.Text = "Jauth: 未連線"
            Logging("[資訊] 未偵測到網際網路連線")
        End If

        If Not IO.File.Exists(Environment.CurrentDirectory & "\.jauth_config") Then
            Logging("[資訊] 找不到配置檔案, 使用預設配置.")
            Me.Visibility = Visibility.Visible
            Exit Sub
        Else
            ini.DefaultFilePath = Environment.CurrentDirectory & "\.jauth_config"
        End If

        txtUsername.Text = ini.GetValue("account", "username", "00000000")
        Logging($"[資訊] 使用者名稱: {txtUsername.Text}, 載入.")

        txtPassword.Password = ini.GetValue("account", "password", "123456")
        Logging($"[資訊] 密碼: {txtPassword.Password}, 載入.")

        togReconnect.IsChecked = If(ini.GetValue("general", "reconnect", "0") = "1", True, False)
        Logging($"[資訊] 斷線重連設定: {togReconnect.IsChecked.ToString}, 載入.")

        Dim mgr As New Catboy.SystemControl.AutoStartup("Jauth", Environment.CurrentDirectory & "\Jauth.exe")
        togStartUp.IsChecked = mgr.Exists()
        Logging($"[資訊] 自動啟動設定: {togStartUp.IsChecked.ToString}, 載入.")

        AddHandler tim.Tick, AddressOf tim_Tick
        IsReconnect()

    End Sub

    Private Sub Window_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs)
        If Not QuitFlag = True Then



            If Not IO.File.Exists(Environment.CurrentDirectory & "\.jauth_config") Then
                Logging("[資訊] 找不到配置檔案, 創建中.")
                IO.File.Create(Environment.CurrentDirectory & "\.jauth_config")
                Logging("[資訊] 已創建配置檔.")
            End If
            ini.DefaultFilePath = Environment.CurrentDirectory & "\.jauth_config"

            ini.SetValue("account", "username", txtUsername.Text)
            Logging($"[資訊] 使用者名稱: {txtUsername.Text}, 寫入.")

            ini.SetValue("account", "password", txtPassword.Password)
            Logging($"[資訊] 密碼: {txtPassword.Password}, 寫入.")

            ini.SetValue("general", "reconnect", If(togReconnect.IsChecked, "1", "0"))
            Logging($"[資訊] 斷線重連設定: {togReconnect.IsChecked.ToString}, 寫入.")

            IsReconnect()

            Dim bolStartUpSetting As Boolean = If(ini.GetValue("general", "startup", "0") = "1", True, False)
            If bolStartUpSetting = togStartUp.IsChecked Then
                Logging("[資訊] 自動啟動設定未更改.")
                Me.Hide()
                e.Cancel = True
                Exit Sub
            End If

            Dim mgr As New Catboy.SystemControl.AutoStartup("Jauth", Environment.CurrentDirectory & "\Jauth.exe")
            mgr.SetEnabled(togStartUp.IsChecked)
            Logging($"[資訊] 自動啟動已設定為: {togStartUp.IsChecked.ToString}.")
            ini.SetValue("general", "startup", If(togStartUp.IsChecked, "1", "0"))
            Logging($"[資訊] 自動啟動設定: {togStartUp.IsChecked.ToString}, 寫入.")



            Me.Hide()
            e.Cancel = True

        End If
    End Sub

#End Region

#Region "Context Menu"

    Private Sub itmConnect_Click(sender As Object, e As EventArgs)
        Me.Visibility = Visibility.Hidden
        Logging("[資訊] 手動請求連線")
        If watcher.CheckAvailibility = True Then
            Logging("[資訊] 偵測到網際網路連線, 忽略請求.")
            Exit Sub
        End If
        If Connect() = True Then
            icoTray.Icon = My.Resources.lan_connect
            icoTray.Text = "Jauth: 已連線"
        Else
            icoTray.Icon = My.Resources.lan_disconnect
            icoTray.Text = "Jauth: 未連線"
        End If
    End Sub

    Private Sub itmDisconnect_Click(sender As Object, e As EventArgs)
        Me.Visibility = Visibility.Hidden
        Logging("[資訊] 手動請求斷開")
        If watcher.CheckAvailibility = False Then
            Logging("[資訊] 未偵測到網際網路連線, 忽略請求.")
            Exit Sub
        End If
        If Disconnect() = True Then
            icoTray.Icon = My.Resources.lan_disconnect
            icoTray.Text = "Jauth: 未連線"
        Else
            icoTray.Icon = My.Resources.lan_connect
            icoTray.Text = "Jauth: 已連線"
        End If
    End Sub

    Public Sub itmShowWindow_Click(sender As Object, e As EventArgs)
        Me.Visibility = Visibility.Visible
    End Sub

    Public Sub itmLog_Click(sender As Object, e As EventArgs)
        Process.Start(Environment.CurrentDirectory & "\Jauth_Log.txt")
    End Sub

    Public Sub itmQuit_Click(sender As Object, e As EventArgs)
        QuitFlag = True
        Me.Close()
    End Sub

#End Region

    Private Sub IsReconnect()
        If ini.GetValue("general", "reconnect", "0") = "1" Then
            tim.Start()
            Logging("[資訊] 已啟用網路狀態監控")
        Else
            tim.Stop()
            Logging("[資訊] 已停用網路狀態監控")
        End If
    End Sub

    Private Sub tim_Tick(ByVal sender As Object, ByVal e As EventArgs)
        If watcher.CheckAvailibility = True Then
            icoTray.Icon = My.Resources.lan_connect
            icoTray.Text = "Jauth: 已連線"
            Logging("[資訊] 監控偵測到網際網路連線")
        Else
            icoTray.Icon = My.Resources.lan_disconnect
            icoTray.Text = "Jauth: 未連線"
            Logging("[資訊] 監控偵測到已斷開網路, 進行重新連線")
            If Not Connect() = True Then
                Logging("[錯誤] 嘗試重新連線失敗, 等待下次重試")
            End If
        End If
    End Sub


End Class
