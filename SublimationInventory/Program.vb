Imports System.Windows.Forms
Imports SublimationInventory.Data
Imports SublimationInventory.Forms
Imports SublimationInventory.UI
''' <summary>Application entry point (custom Sub Main).</summary>
Module Program

    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        ' Create the LocalDB database, schema and seed data on first run.
        Try
            Database.Initialize()
        Catch ex As Exception
            AppModal.ErrorBox(Nothing,
                "Could not connect to the database." & vbCrLf & vbCrLf & ex.Message & vbCrLf & vbCrLf &
                "Check that MySQL is running (start Apache + MySQL in the XAMPP Control Panel) " &
                "and that the connection details in App.config are correct.",
                "Startup error")
            Return
        End Try

        Using login As New LoginForm()
            If login.ShowDialog() = DialogResult.OK Then
                Application.Run(New MainForm(login.AuthenticatedUser))
            End If
        End Using
    End Sub

End Module
