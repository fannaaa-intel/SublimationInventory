Imports System.Security.Cryptography
Imports System.Text
Imports SublimationInventory.Data
Imports SublimationInventory.Models

Namespace Services
    ''' <summary>Salted SHA-256 password hashing + login.</summary>
    Public Module AuthService

        ' Stored format: "saltBase64:hashBase64"
        Public Function HashPassword(password As String) As String
            Dim salt(15) As Byte
            Using rng = RandomNumberGenerator.Create()
                rng.GetBytes(salt)
            End Using
            Dim hash = ComputeHash(password, salt)
            Return Convert.ToBase64String(salt) & ":" & Convert.ToBase64String(hash)
        End Function

        Public Function VerifyPassword(password As String, stored As String) As Boolean
            If String.IsNullOrEmpty(stored) OrElse Not stored.Contains(":") Then Return False
            Dim parts = stored.Split(":"c)
            Dim salt = Convert.FromBase64String(parts(0))
            Dim expected = Convert.FromBase64String(parts(1))
            Dim actual = ComputeHash(password, salt)
            Return FixedTimeEquals(expected, actual)
        End Function

        Public Function Authenticate(username As String, password As String) As User
            Dim u = UserRepository.GetByUsername(username)
            If u Is Nothing Then Return Nothing
            Return If(VerifyPassword(password, u.PasswordHash), u, Nothing)
        End Function

        Private Function ComputeHash(password As String, salt As Byte()) As Byte()
            Using sha = SHA256.Create()
                Dim pwd = Encoding.UTF8.GetBytes(If(password, ""))
                Dim combined(salt.Length + pwd.Length - 1) As Byte
                Buffer.BlockCopy(salt, 0, combined, 0, salt.Length)
                Buffer.BlockCopy(pwd, 0, combined, salt.Length, pwd.Length)
                Return sha.ComputeHash(combined)
            End Using
        End Function

        Private Function FixedTimeEquals(a As Byte(), b As Byte()) As Boolean
            If a.Length <> b.Length Then Return False
            Dim diff As Integer = 0
            For i = 0 To a.Length - 1
                diff = diff Or (a(i) Xor b(i))
            Next
            Return diff = 0
        End Function
    End Module
End Namespace
