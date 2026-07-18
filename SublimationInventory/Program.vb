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
                "Could not initialize the database." & vbCrLf & vbCrLf & ex.Message & vbCrLf & vbCrLf &
                "Make sure SQL Server LocalDB is installed. It ships with Visual Studio 2022 " &
                "(Individual components > SQL Server Express 2019 LocalDB).",
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
