Imports MySql.Data.MySqlClient
Imports SublimationInventory.Models

Namespace Data
    Public Module UserRepository

        Public Function GetByUsername(username As String) As User
            Using conn = Database.GetConnection()
                conn.Open()
                Using cmd As New MySqlCommand("SELECT UserId,Username,PasswordHash,FullName FROM Users WHERE Username=@u", conn)
                    cmd.Parameters.AddWithValue("@u", username)
                    Using r = cmd.ExecuteReader()
                        If r.Read() Then
                            Return New User With {
                                .UserId = Convert.ToInt32(r("UserId")),
                                .Username = Convert.ToString(r("Username")),
                                .PasswordHash = Convert.ToString(r("PasswordHash")),
                                .FullName = If(IsDBNull(r("FullName")), "", Convert.ToString(r("FullName")))
                            }
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

    End Module
End Namespace