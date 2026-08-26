Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Windows.Forms

Namespace UI
    ''' <summary>
    ''' Prints a DataGridView as a professional A4 report: portrait, black-on-white,
    ''' repeating column headers, zebra banding, and "Page n of m" footers.
    '''
    ''' Deliberately NOT themed - a printed report uses plain rules and black text so it
    ''' stays legible on a monochrome printer and does not waste colour ink on headings.
    ''' </summary>
    Public Class ReportExporter

        ' --- page furniture, in hundredths of an inch (the GDI+ printing unit) ---
        Private Const MarginX As Integer = 60      ' 0.60"
        Private Const MarginTop As Integer = 55
        Private Const MarginBottom As Integer = 60
        Private Const HeaderRowHeight As Single = 26.0F
        Private Const BodyRowHeight As Single = 22.0F

        Private ReadOnly _title As String
        Private ReadOnly _subtitle As String
        Private ReadOnly _headers As List(Of String)
        Private ReadOnly _rows As List(Of String())
        Private ReadOnly _rightAlign As List(Of Boolean)
        Private ReadOnly _generated As DateTime

        Private _widths() As Single
        Private _rowIndex As Integer
        Private _pageNumber As Integer
        Private _totalPages As Integer

        Private ReadOnly _fontTitle As Font = New Font("Segoe UI", 15.0F, FontStyle.Bold)
        Private ReadOnly _fontSubtitle As Font = New Font("Segoe UI", 8.5F, FontStyle.Regular)
        Private ReadOnly _fontHeader As Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        Private ReadOnly _fontBody As Font = New Font("Segoe UI", 8.5F, FontStyle.Regular)
        Private ReadOnly _fontFooter As Font = New Font("Segoe UI", 7.5F, FontStyle.Regular)

        Private Sub New(title As String, subtitle As String, grid As DataGridView)
            _title = title
            _subtitle = subtitle
            _generated = DateTime.Now
            _headers = New List(Of String)()
            _rows = New List(Of String())()
            _rightAlign = New List(Of Boolean)()

            Dim cols = grid.Columns.Cast(Of DataGridViewColumn)().Where(Function(c) c.Visible).ToList()
            For Each c In cols
                _headers.Add(c.HeaderText)
                _rightAlign.Add(c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight OrElse
                                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopRight OrElse
                                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomRight)
            Next

            For Each row As DataGridViewRow In grid.Rows
                If row.IsNewRow Then Continue For
                Dim vals(cols.Count - 1) As String
                For i = 0 To cols.Count - 1
                    vals(i) = Convert.ToString(row.Cells(cols(i).Index).Value)
                Next
                _rows.Add(vals)
            Next
        End Sub

        ''' <summary>Shows a print preview of the grid as an A4 report.</summary>
        Public Shared Sub Preview(owner As IWin32Window, grid As DataGridView, title As String, subtitle As String)
            If grid.Rows.Count = 0 Then
                AppModal.Warn(owner, "There is nothing to print - this report has no rows.", "Nothing to print")
                Return
            End If
            Dim ex As New ReportExporter(title, subtitle, grid)
            Using doc = ex.BuildDocument()
                Using dlg As New PrintPreviewDialog()
                    dlg.Document = doc
                    dlg.Width = 900
                    dlg.Height = 780
                    dlg.StartPosition = FormStartPosition.CenterParent
                    dlg.ShowIcon = False
                    dlg.Text = title & " - print preview"
                    DirectCast(dlg, Form).ShowDialog(owner)
                End Using
            End Using
        End Sub

        Private Function BuildDocument() As PrintDocument
            Dim doc As New PrintDocument()
            doc.DocumentName = _title

            ' A4 portrait, 210 x 297 mm expressed in hundredths of an inch.
            Dim a4 As New PaperSize("A4", 827, 1169)
            a4.RawKind = CInt(PaperKind.A4)
            doc.DefaultPageSettings.PaperSize = a4
            doc.DefaultPageSettings.Landscape = False
            doc.DefaultPageSettings.Margins = New Margins(MarginX, MarginX, MarginTop, MarginBottom)
            doc.OriginAtMargins = False

            AddHandler doc.BeginPrint, Sub(s, e)
                                           _rowIndex = 0
                                           _pageNumber = 0
                                           _widths = Nothing
                                           _totalPages = 0
                                       End Sub
            AddHandler doc.PrintPage, AddressOf OnPrintPage
            Return doc
        End Function

        ''' <summary>Column widths proportional to the widest sampled content, scaled to the page.</summary>
        Private Sub ComputeWidths(g As Graphics, usableWidth As Single)
            Dim weights(_headers.Count - 1) As Single
            For i = 0 To _headers.Count - 1
                weights(i) = g.MeasureString(_headers(i), _fontHeader).Width
            Next
            ' Sample a bounded number of rows - measuring thousands would be slow and adds nothing.
            Dim sample = Math.Min(_rows.Count, 200)
            For r = 0 To sample - 1
                For i = 0 To _headers.Count - 1
                    Dim w = g.MeasureString(If(_rows(r)(i), ""), _fontBody).Width
                    If w > weights(i) Then weights(i) = w
                Next
            Next

            Dim total As Single = weights.Sum()
            If total <= 0 Then total = 1
            _widths = New Single(_headers.Count - 1) {}
            Dim minW As Single = 46.0F
            For i = 0 To _headers.Count - 1
                _widths(i) = Math.Max(minW, usableWidth * (weights(i) / total))
            Next

            ' Rescale so the columns exactly fill the usable width after the minimum clamp.
            Dim sum As Single = _widths.Sum()
            If sum > 0 Then
                Dim k = usableWidth / sum
                For i = 0 To _widths.Length - 1
                    _widths(i) *= k
                Next
            End If
        End Sub

        Private Function RowsPerPage(bodyTop As Single, bottomLimit As Single) As Integer
            Return Math.Max(1, CInt(Math.Floor((bottomLimit - bodyTop) / BodyRowHeight)))
        End Function

        Private Sub OnPrintPage(sender As Object, e As PrintPageEventArgs)
            Dim g = e.Graphics
            ' Grayscale AA, not ClearType: subpixel hinting renders differently over the
            ' banded rows than over white, which makes alternating rows look bolder.
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit

            Dim left As Single = MarginX
            Dim right As Single = e.PageSettings.PaperSize.Width - MarginX
            Dim usable As Single = right - left
            Dim bottomLimit As Single = e.PageSettings.PaperSize.Height - MarginBottom

            If _widths Is Nothing Then ComputeWidths(g, usable)

            _pageNumber += 1

            ' ---- page header (first page carries the full masthead) ----
            Dim y As Single = MarginTop
            If _pageNumber = 1 Then
                g.DrawString(_title, _fontTitle, Brushes.Black, left, y)
                y += 26
                If Not String.IsNullOrWhiteSpace(_subtitle) Then
                    g.DrawString(_subtitle, _fontSubtitle, Brushes.DimGray, left, y)
                    y += 14
                End If
                g.DrawString("Generated " & _generated.ToString("dd MMM yyyy, HH:mm") & "   |   " &
                             _rows.Count & " row(s)", _fontSubtitle, Brushes.DimGray, left, y)
                y += 18
                Using pen As New Pen(Color.Black, 1.2F)
                    g.DrawLine(pen, left, y, right, y)
                End Using
                y += 10
            Else
                g.DrawString(_title & " (continued)", _fontHeader, Brushes.Black, left, y)
                y += 16
                Using pen As New Pen(Color.Gray, 0.8F)
                    g.DrawLine(pen, left, y, right, y)
                End Using
                y += 8
            End If

            ' ---- column headers: light grey band, black text, ruled underneath ----
            Dim headerTop = y
            Using bg As New SolidBrush(Color.FromArgb(232, 232, 232))
                g.FillRectangle(bg, left, headerTop, usable, HeaderRowHeight)
            End Using
            DrawRow(g, _headers.ToArray(), left, headerTop, _fontHeader, HeaderRowHeight, Brushes.Black)
            Using pen As New Pen(Color.Black, 1.0F)
                g.DrawLine(pen, left, headerTop + HeaderRowHeight, right, headerTop + HeaderRowHeight)
            End Using
            y = headerTop + HeaderRowHeight

            ' ---- body ----
            Dim perPage = RowsPerPage(y, bottomLimit)
            If _totalPages = 0 Then
                ' Page 1 has the tall masthead, so it fits fewer rows than later pages.
                Dim laterPerPage = RowsPerPage(MarginTop + 16 + 8 + HeaderRowHeight, bottomLimit)
                Dim remaining = _rows.Count - perPage
                _totalPages = If(remaining <= 0, 1, 1 + CInt(Math.Ceiling(remaining / CDbl(Math.Max(1, laterPerPage)))))
            End If

            Dim drawn = 0
            While _rowIndex < _rows.Count AndAlso drawn < perPage
                If drawn Mod 2 = 1 Then
                    ' Very light band - enough to guide the eye across a wide row without
                    ' changing how the text on it renders.
                    Using bg As New SolidBrush(Color.FromArgb(248, 248, 248))
                        g.FillRectangle(bg, left, y, usable, BodyRowHeight)
                    End Using
                End If
                DrawRow(g, _rows(_rowIndex), left, y, _fontBody, BodyRowHeight, Brushes.Black)
                y += BodyRowHeight
                _rowIndex += 1
                drawn += 1
            End While

            Using pen As New Pen(Color.Gray, 0.8F)
                g.DrawLine(pen, left, y, right, y)
            End Using

            ' ---- footer ----
            Dim footerY = e.PageSettings.PaperSize.Height - MarginBottom + 14
            g.DrawString("Sublimation Inventory", _fontFooter, Brushes.DimGray, left, footerY)
            Dim pageText = String.Format("Page {0} of {1}", _pageNumber, Math.Max(_totalPages, _pageNumber))
            Dim pw = g.MeasureString(pageText, _fontFooter).Width
            g.DrawString(pageText, _fontFooter, Brushes.DimGray, right - pw, footerY)

            e.HasMorePages = _rowIndex < _rows.Count
        End Sub

        Private Sub DrawRow(g As Graphics, values As String(), left As Single, top As Single,
                            font As Font, height As Single, brush As Brush)
            Dim x = left
            For i = 0 To _headers.Count - 1
                Dim cellW = _widths(i)
                Dim pad As Single = 5.0F
                Dim rect As New RectangleF(x + pad, top + 4, Math.Max(4.0F, cellW - pad * 2), height - 6)
                Using fmt As New StringFormat(StringFormatFlags.NoWrap)
                    fmt.Trimming = StringTrimming.EllipsisCharacter
                    fmt.Alignment = If(_rightAlign(i), StringAlignment.Far, StringAlignment.Near)
                    fmt.LineAlignment = StringAlignment.Center
                    Dim v = If(i < values.Length, values(i), "")
                    g.DrawString(If(v, ""), font, brush, rect, fmt)
                End Using
                x += cellW
            Next
        End Sub
    End Class
End Namespace
