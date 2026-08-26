Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Reflection
Imports System.Windows.Forms

Namespace UI
    ''' <summary>
    ''' Loads and draws the company logo (RJA Sportswear).
    '''
    ''' The image is compiled into the assembly as an embedded resource, so the
    ''' published .exe carries its own branding and there is no file to lose when
    ''' the app is copied to another machine.
    ''' </summary>
    Public Module Branding

        Private _logo As Image
        Private _looked As Boolean


        ''' <summary>
        ''' The logo, or Nothing when no logo file has been supplied. Callers must
        ''' handle Nothing - the app has to keep working before the asset is added.
        ''' </summary>
        Public ReadOnly Property Logo As Image
            Get
                If Not _looked Then
                    _looked = True
                    _logo = LoadLogo()
                End If
                Return _logo
            End Get
        End Property

        Public ReadOnly Property HasLogo As Boolean
            Get
                Return Logo IsNot Nothing
            End Get
        End Property

        Private Function LoadLogo() As Image
            ' 1) Embedded resource - the normal path once Assets\logo.png is in the project.
            Try
                Dim asm = Assembly.GetExecutingAssembly()
                For Each name In asm.GetManifestResourceNames()
                    If name.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase) Then
                        Using st = asm.GetManifestResourceStream(name)
                            If st IsNot Nothing Then Return Image.FromStream(st)
                        End Using
                    End If
                Next
            Catch
                ' fall through to the on-disk copy
            End Try

            ' 2) Beside the executable - lets the logo be swapped without a rebuild.
            Try
                Dim dir = IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                For Each rel In New String() {"Assets\logo.png", "logo.png", "..\..\Assets\logo.png"}
                    Dim p = IO.Path.Combine(dir, rel)
                    If IO.File.Exists(p) Then
                        ' Copy through a bitmap so the file is not left locked.
                        Using tmp = Image.FromFile(p)
                            Return New Bitmap(tmp)
                        End Using
                    End If
                Next
            Catch
            End Try

            Return Nothing
        End Function

        ''' <summary>
        ''' Draws the complete logo centred inside a circle of the given diameter,
        ''' scaled to fit and never distorted. Falls back to a gold badge with the
        ''' given text when no logo is available.
        ''' </summary>
        ''' <param name="zoom">
        ''' Fraction of the circle the logo fills, 0-1. 1.0 fills it edge to edge;
        ''' lower values inset the mark, leaving a white margin inside the ring.
        ''' </param>
        Public Sub DrawCircular(g As Graphics, x As Integer, y As Integer, size As Integer,
                                Optional ringColor As Color? = Nothing, Optional ringWidth As Single = 2.5F,
                                Optional fallbackText As String = "", Optional zoom As Single = 1.0F)
            Dim ring = If(ringColor.HasValue, ringColor.Value, Theme.Accent)
            g.SmoothingMode = SmoothingMode.AntiAlias

            If HasLogo Then
                Using clip As New GraphicsPath()
                    clip.AddEllipse(x, y, size, size)
                    Dim old = g.Clip
                    g.SetClip(clip)

                    ' White disc behind the mark so a logo with its own background
                    ' does not blend into the navy rail.
                    Using b As New SolidBrush(Color.White)
                        g.FillEllipse(b, x, y, size, size)
                    End Using

                    ' Fit the WHOLE logo inside the circle rather than cropping into it.
                    ' The artwork carries the crown above and "SPORTSWEAR" below the
                    ' monogram; any centre-crop tight enough to enlarge the letters
                    ' slices those off, so scale the complete mark down instead.
                    Dim src = Logo
                    Dim z = Math.Max(0.2F, Math.Min(1.0F, zoom))
                    Dim box = size * z
                    Dim sc = Math.Min(box / src.Width, box / src.Height)
                    Dim dw = CInt(src.Width * sc)
                    Dim dh = CInt(src.Height * sc)
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality
                    g.DrawImage(src, New Rectangle(x + (size - dw) \ 2, y + (size - dh) \ 2, dw, dh))

                    g.Clip = old
                End Using
            Else
                ' No logo yet - keep the gold badge so the UI still looks finished.
                Using b As New SolidBrush(Theme.Accent)
                    g.FillEllipse(b, x, y, size, size)
                End Using
                If fallbackText <> "" Then
                    Using f = Theme.AppFont(size * 0.3F, FontStyle.Bold)
                        TextRenderer.DrawText(g, fallbackText, f, New Rectangle(x, y, size, size), Color.White,
                                              TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or
                                              TextFormatFlags.NoPrefix)
                    End Using
                End If
            End If

            If ringWidth > 0 Then
                Using p As New Pen(ring, ringWidth)
                    g.DrawEllipse(p, x - ringWidth, y - ringWidth,
                                  size + ringWidth * 2, size + ringWidth * 2)
                End Using
            End If
        End Sub

        ''' <summary>Draws the logo to fit a rectangle, preserving aspect ratio. No-op without a logo.</summary>
        Public Sub DrawFitted(g As Graphics, bounds As Rectangle)
            If Not HasLogo Then Return
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.InterpolationMode = InterpolationMode.HighQualityBicubic
            g.PixelOffsetMode = PixelOffsetMode.HighQuality

            Dim src = Logo
            Dim scale = Math.Min(bounds.Width / CSng(src.Width), bounds.Height / CSng(src.Height))
            Dim w = CInt(src.Width * scale)
            Dim h = CInt(src.Height * scale)
            Dim x = bounds.X + (bounds.Width - w) \ 2
            Dim y = bounds.Y + (bounds.Height - h) \ 2
            g.DrawImage(src, New Rectangle(x, y, w, h))
        End Sub

        ''' <summary>Builds a window/taskbar icon from the logo, or Nothing if unavailable.</summary>
        Public Function CreateWindowIcon() As Icon
            If Not HasLogo Then Return Nothing
            Try
                Using bmp As New Bitmap(64, 64)
                    Using g = Graphics.FromImage(bmp)
                        g.Clear(Color.Transparent)
                        DrawFitted(g, New Rectangle(0, 0, 64, 64))
                    End Using
                    Return Icon.FromHandle(bmp.GetHicon())
                End Using
            Catch
                Return Nothing
            End Try
        End Function
    End Module
End Namespace
