Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms

Namespace UI
    Public Module UiHelpers

        Public Sub StyleAccentButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = Theme.AccentHover
            btn.BackColor = Theme.Accent
            btn.ForeColor = Color.White
            btn.Font = Theme.AppFont(10.0F, FontStyle.Bold)
            btn.Cursor = Cursors.Hand
            btn.Height = 38
        End Sub

        Public Sub StyleSecondaryButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderColor = Theme.BorderColor
            btn.FlatAppearance.BorderSize = 1
            btn.BackColor = Color.White
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
            grid.BackgroundColor = Color.White
            grid.EnableHeadersVisualStyles = False
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.SecondaryDark
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextLight
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.SecondaryDark
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
            grid.DefaultCellStyle.BackColor = Color.White
            grid.DefaultCellStyle.ForeColor = Theme.TextDark
            grid.DefaultCellStyle.SelectionBackColor = Theme.Highlight
            grid.DefaultCellStyle.SelectionForeColor = Color.White
            grid.DefaultCellStyle.Padding = New Padding(12, 0, 8, 0)
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False
            grid.GridColor = Theme.BorderStrong
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 248, 246)
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
                                                g.Rows(e.RowIndex).DefaultCellStyle.BackColor = Shade(effBase(e.RowIndex), -18, -30, -42)
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
                IO.File.WriteAllText(sfd.FileName, sb.ToString())
                AppModal.Info(Nothing, "Exported to " & sfd.FileName, "Export complete")
            End Using
        End Sub

        Private Function CsvField(s As String) As String
            If s Is Nothing Then Return ""
            If s.IndexOfAny(New Char() {","c, """"c, ChrW(10), ChrW(13)}) >= 0 Then
                Return """" & s.Replace("""", """""") & """"
            End If
            Return s
        End Function
    End Module
End Namespace