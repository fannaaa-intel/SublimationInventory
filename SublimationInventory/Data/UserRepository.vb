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

        ''' <summary>Returns the user's stored profile picture, or Nothing if none is set.</summary>
        Public Function GetProfileImage(userId As Integer) As Byte()
            Using conn = Database.GetConnection()
                conn.Open()
                Using cmd As New MySqlCommand("SELECT ProfileImage FROM Users WHERE UserId=@id", conn)
                    cmd.Parameters.AddWithValue("@id", userId)
                    Dim v = cmd.ExecuteScalar()
                    If v Is Nothing OrElse IsDBNull(v) Then Return Nothing
                    Return DirectCast(v, Byte())
                End Using
            End Using
        End Function

        ''' <summary>Stores (or clears, when data is Nothing) the user's profile picture.</summary>
        Public Sub SaveProfileImage(userId As Integer, data As Byte())
            Using conn = Database.GetConnection()
                conn.Open()
                Using cmd As New MySqlCommand("UPDATE Users SET ProfileImage=@img WHERE UserId=@id", conn)
                    Dim p = cmd.Parameters.Add("@img", MySqlDbType.LongBlob)
                    p.Value = If(data, CObj(DBNull.Value))
                    cmd.Parameters.AddWithValue("@id", userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Module
End Namespace