Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms

Namespace UI
    Public Module UiHelpers

        Public Sub StyleAccentButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = Theme.AccentHover
            btn.FlatAppearance.MouseDownBackColor = Theme.AccentDeep
            btn.BackColor = Theme.Accent
            btn.ForeColor = Color.White
            btn.Font = Theme.AppFont(10.0F, FontStyle.Bold)
            btn.Cursor = Cursors.Hand
            btn.Height = 38
        End Sub

        ''' <summary>Navy solid button - for primary actions that sit next to a gold one.</summary>
        Public Sub StyleNavyButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = Theme.Shift(Theme.SidebarBg, 0.12F)
            btn.FlatAppearance.MouseDownBackColor = Theme.SidebarBgDarker
            btn.BackColor = Theme.SidebarBg
            btn.ForeColor = Color.White
            btn.Font = Theme.AppFont(10.0F, FontStyle.Bold)
            btn.Cursor = Cursors.Hand
            btn.Height = 38
        End Sub

        Public Sub StyleSecondaryButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderColor = Theme.BorderStrong
            btn.FlatAppearance.BorderSize = 1
            btn.FlatAppearance.MouseOverBackColor = Theme.SelectedRow
            btn.BackColor = Theme.CardBg
            btn.ForeColor = Theme.TextDark
            btn.Font = Theme.AppFont(10.0F)
            btn.Cursor = Cursors.Hand
            btn.Height = 38
        End Sub

        ''' <summary>Peso currency symbol (U+20B1), built from a code point so file encoding can't break it.</summary>
        Public ReadOnly Peso As String = ChrW(&H20B1)

        ''' <summary>Formats a decimal as peso currency, e.g. ₱1,234.50 — use everywhere instead of "C2".</summary>
        Public Function Money(value As Decimal) As String
            Return Peso & value.ToString("N2")
        End Function

        Public Sub StyleGrid(grid As DataGridView)
            grid.BorderStyle = BorderStyle.FixedSingle
            grid.BackgroundColor = Theme.CardBg
            grid.EnableHeadersVisualStyles = False
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.SidebarBg
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextLight
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.SidebarBg
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.TextLight
            grid.ColumnHeadersDefaultCellStyle.Font = Theme.AppFont(11.0F, FontStyle.Bold)
            grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(12, 0, 8, 0)
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            grid.ColumnHeadersHeight = 48
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            grid.RowHeadersVisible = False
            grid.AllowUserToAddRows = False
            grid.AllowUserToDeleteRows = False
            grid.[ReadOnly] = True
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            grid.MultiSelect = False
            grid.AllowUserToResizeRows = False
            grid.AllowUserToResizeColumns = False
            grid.AllowUserToOrderColumns = False
            grid.RowTemplate.Height = 44
            grid.DefaultCellStyle.Font = Theme.AppFont(10.5F)
            ' Clicked row gets a special blue highlight (signals it's picked for an action).
            grid.DefaultCellStyle.BackColor = Theme.CardBg
            grid.DefaultCellStyle.ForeColor = Theme.TextDark
            grid.DefaultCellStyle.SelectionBackColor = Theme.Highlight
            grid.DefaultCellStyle.SelectionForeColor = Color.White
            grid.DefaultCellStyle.Padding = New Padding(12, 0, 8, 0)
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False
            grid.GridColor = Theme.BorderStrong
            grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.Shift(Theme.CardBg, -0.03F)
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Theme.Highlight
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleVertical
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
            grid.ScrollBars = ScrollBars.Vertical
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None


            ' --- Row hover that respects each row's own colour (e.g. low-stock amber) ---
            Dim rawBase As New Dictionary(Of Integer, Color)()   ' the row's own set BackColor (Empty if none)
            Dim effBase As New Dictionary(Of Integer, Color)()   ' the colour actually displayed at rest
            Dim hovered As Integer = -1

            Dim capture As Action(Of Integer) =
                Sub(i)
                    If i < 0 OrElse i >= grid.Rows.Count OrElse rawBase.ContainsKey(i) Then Return
                    rawBase(i) = grid.Rows(i).DefaultCellStyle.BackColor
                    effBase(i) = grid.Rows(i).Cells(0).InheritedStyle.BackColor
                End Sub

            Dim clearHover As Action =
                Sub()
                    If hovered >= 0 AndAlso hovered < grid.Rows.Count AndAlso rawBase.ContainsKey(hovered) Then
                        grid.Rows(hovered).DefaultCellStyle.BackColor = rawBase(hovered)   ' restore EXACT original
                    End If
                    hovered = -1
                End Sub

            AddHandler grid.CellMouseEnter, Sub(sender, e)
                                                Dim g = DirectCast(sender, DataGridView)
                                                If e.RowIndex < 0 Then clearHover() : Return
                                                If e.RowIndex = hovered Then Return          ' same row, don't reflash
                                                clearHover()
                                                capture(e.RowIndex)
                                                g.Rows(e.RowIndex).DefaultCellStyle.BackColor = Shade(effBase(e.RowIndex), -14, -18, -26)
                                                hovered = e.RowIndex
                                                g.Cursor = Cursors.Hand
                                            End Sub
            AddHandler grid.MouseLeave, Sub(sender, e)
                                            clearHover()
                                            grid.Cursor = Cursors.Default
                                        End Sub
            ' rows are rebuilt on Refresh/reload — drop cached colours so we re-read the fresh ones
            AddHandler grid.RowsRemoved, Sub()
                                             rawBase.Clear() : effBase.Clear() : hovered = -1
                                         End Sub
            ' No highlight until the user actually clicks a row.
            AddHandler grid.RowsAdded, Sub(sender, e) DirectCast(sender, DataGridView).ClearSelection()
        End Sub

        ''' <summary>Shifts a colour by per-channel deltas (negative = darker), clamped 0-255.</summary>
        Private Function Shade(c As Color, dr As Integer, dg As Integer, db As Integer) As Color
            If c.A = 0 Then c = Color.White   ' unset/transparent resolves to white
            Return Color.FromArgb(
                Math.Max(0, Math.Min(255, CInt(c.R) + dr)),
                Math.Max(0, Math.Min(255, CInt(c.G) + dg)),
                Math.Max(0, Math.Min(255, CInt(c.B) + db)))
        End Function

        ''' <summary>
        ''' Sets relative fill weights for columns. With AutoSizeColumnsMode.Fill the grid
        ''' shares spare width by FillWeight, so explicit weights stop one column (usually
        ''' the first) from absorbing everything and squeezing the rest into ellipses.
        ''' </summary>
        Public Sub SetFillWeights(grid As DataGridView, ParamArray weights As Integer())
            For i = 0 To Math.Min(weights.Length, grid.Columns.Count) - 1
                grid.Columns(i).FillWeight = weights(i)
            Next
        End Sub

        ''' <summary>Right-aligns the named numeric/currency columns so figures line up on their digits.</summary>
        Public Sub AlignRight(grid As DataGridView, ParamArray columnNames As String())
            For Each n In columnNames
                If grid.Columns.Contains(n) Then
                    grid.Columns(n).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                End If
            Next
        End Sub

        ''' <summary>
        ''' Shows a centred message over an empty grid. Uses a paint overlay rather than a
        ''' placeholder row, so the "message" is never sorted, exported or printed as data.
        ''' Safe to call on every reload - the handler is attached once per grid.
        ''' </summary>
        Public Sub ShowEmptyMessage(grid As DataGridView, message As String)
            grid.Tag = message
            If _emptyHooked.Contains(grid) Then
                grid.Invalidate()
                Return
            End If
            _emptyHooked.Add(grid)
            AddHandler grid.Paint, Sub(sender, e)
                                       Dim gv = DirectCast(sender, DataGridView)
                                       If gv.Rows.Count > 0 Then Return
                                       Dim msg = Convert.ToString(gv.Tag)
                                       If String.IsNullOrEmpty(msg) Then Return
                                       Dim area = gv.ClientRectangle
                                       Using f = Theme.AppFont(10.0F)
                                           TextRenderer.DrawText(e.Graphics, msg, f, area, Theme.TextMuted,
                                               TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or
                                               TextFormatFlags.WordBreak)
                                       End Using
                                   End Sub
            grid.Invalidate()
        End Sub

        Private ReadOnly _emptyHooked As New HashSet(Of DataGridView)()

        ''' <summary>Applies sensible minimum widths so columns don't compress to unreadable widths when the grid stretches to fill.</summary>
        Public Sub SetMinColumnWidths(grid As DataGridView, ParamArray widths As Integer())
            For i = 0 To Math.Min(widths.Length, grid.Columns.Count) - 1
                grid.Columns(i).MinimumWidth = widths(i)
            Next
        End Sub

        Public Sub StyleInput(c As Control)
            c.Font = Theme.AppFont(10.5F)
            Dim tb = TryCast(c, TextBox)
            If tb IsNot Nothing Then tb.BorderStyle = BorderStyle.FixedSingle
        End Sub

        ''' <summary>Exports every visible row of a grid to a CSV file chosen by the user.</summary>
        Public Sub ExportGridToCsv(grid As DataGridView, defaultName As String)
            Using sfd As New SaveFileDialog()
                sfd.Filter = "CSV files (*.csv)|*.csv"
                sfd.FileName = defaultName
                If sfd.ShowDialog() <> DialogResult.OK Then Return
                Dim sb As New StringBuilder()
                Dim headers = grid.Columns.Cast(Of DataGridViewColumn)().
                                  Where(Function(col) col.Visible).
                                  Select(Function(col) CsvField(col.HeaderText))
                sb.AppendLine(String.Join(",", headers))
                For Each row As DataGridViewRow In grid.Rows
                    If row.IsNewRow Then Continue For
                    Dim vals As New List(Of String)()
                    For Each col As DataGridViewColumn In grid.Columns
                        If col.Visible Then vals.Add(CsvField(Convert.ToString(row.Cells(col.Index).Value)))
                    Next
                    sb.AppendLine(String.Join(",", vals))
                Next
                ' UTF-8 *with* BOM so Excel renders the peso sign instead of mojibake.
                IO.File.WriteAllText(sfd.FileName, sb.ToString(), New System.Text.UTF8Encoding(True))
                AppModal.Info(Nothing, "Exported to " & sfd.FileName, "Export complete")
            End Using
        End Sub

        Private Function CsvField(s As String) As String
            If s Is Nothing Then Return ""
            ' Spreadsheets execute a leading =, +, - or @ as a formula. Prefix with an
            ' apostrophe so exported data is always read as text, never evaluated.
            If s.Length > 0 AndAlso "=+-@".IndexOf(s(0)) >= 0 Then s = "'" & s
            If s.IndexOfAny(New Char() {","c, """"c, ChrW(10), ChrW(13)}) >= 0 Then
                Return """" & s.Replace("""", """""") & """"
            End If
            Return s
        End Function
    End Module
End Namespace