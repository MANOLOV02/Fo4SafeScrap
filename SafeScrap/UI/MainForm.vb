Option Strict On
Option Infer On

Imports SafeScrap.Engine

Namespace UI

    ''' <summary>The review window: three lists (objects, groups, folders) with the user's three-state decision, the 3D
    ''' preview of the selected object and the explanation of its result. All the logic is in <see cref="ReviewSession"/>;
    ''' this form only paints what the session answers and passes the user's clicks to it.</summary>
    Partial Class MainForm

        Private Const AppTitle As String = "SafeScrap"

        Private ReadOnly _boot As AppBootstrap
        Private _session As ReviewSession
        ''' <summary>The GL host. Created in <c>Shown</c> (not in the Designer) and torn down in <c>FormClosing</c>
        ''' (pattern of NPC Manager's MeshPicker_Form).</summary>
        Private _preview As PreviewControl
        Private ReadOnly _rows As New Dictionary(Of ReviewTab, IReadOnlyList(Of String))
        Private ReadOnly _sort As New Dictionary(Of ReviewTab, (Column As String, Descending As Boolean))
        Private _refreshing As Boolean
        Private _pendingPreview As String
        Private _loadOrderDiffs As List(Of String)

        Private Shared ReadOnly YesColor As Color = Color.FromArgb(228, 245, 228)
        Private Shared ReadOnly NoColor As Color = Color.FromArgb(251, 230, 230)
        Private Shared ReadOnly ReviewColor As Color = Color.FromArgb(255, 248, 215)

        Friend Sub New(boot As AppBootstrap)
            _boot = boot
            InitializeComponent()
            Text = VersionGate.TituloConVersion(AppTitle)
            ' Controls first, session last: the filter's change event refreshes the lists only once a session exists.
            ComboShow.Items.AddRange({"All", "Undecided", "Decided", "Yes", "No", "Review", "With warning"})   ' RowFilter order
            ComboShow.SelectedIndex = 0
            For Each tb As ReviewTab In {ReviewTab.Objects, ReviewTab.Groups, ReviewTab.Folders}
                _sort(tb) = (Nothing, False)
                SetupGrid(tb)
            Next
            _session = New ReviewSession(boot.Catalog, boot.Book, boot.Decisions)
            _loadOrderDiffs = _boot.SelectionDifferences()
            RefreshAllRows()
            UpdateStatus()
        End Sub

        ''' <summary>The session the window shows (the UI gate reads it).</summary>
        Friend ReadOnly Property Session As ReviewSession
            Get
                Return _session
            End Get
        End Property

        ' ============================================================================================ window

        Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Dim f = _boot.Settings
            If f.MainWindowWidth > 0 AndAlso f.MainWindowHeight > 0 Then
                StartPosition = FormStartPosition.Manual
                Bounds = New Rectangle(f.MainWindowLeft, f.MainWindowTop, f.MainWindowWidth, f.MainWindowHeight)
                If Not Screen.AllScreens.Any(Function(sc) sc.WorkingArea.IntersectsWith(Bounds)) Then StartPosition = FormStartPosition.CenterScreen
            End If
            If f.MainWindowMaximized Then WindowState = FormWindowState.Maximized
        End Sub

        Private Sub MainForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown
            SplitMain.SplitterDistance = CInt(SplitMain.Width * 0.6)
            SplitRight.SplitterDistance = CInt(SplitRight.Height * 0.55)
            If _preview Is Nothing OrElse _preview.IsDisposed Then
                _preview = New PreviewControl() With {.Dock = DockStyle.Fill}
                PreviewHostPanel.Controls.Add(_preview)
                LabelPreview.BringToFront()
            End If
            ShowCurrent()
        End Sub

        Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
            If _session.Dirty Then
                Select Case AppDialogs.YesNoCancel(Me, AppTitle, "Your decisions have unsaved changes." & vbCr & "Save them before closing?")
                    Case DialogResult.Yes : SaveDecisions()
                    Case DialogResult.Cancel
                        e.Cancel = True
                        Return
                End Select
            End If
            TearDownPreview()
            Dim f = _boot.Settings
            f.MainWindowMaximized = WindowState = FormWindowState.Maximized
            Dim b = If(WindowState = FormWindowState.Normal, Bounds, RestoreBounds)
            f.MainWindowLeft = b.Left
            f.MainWindowTop = b.Top
            f.MainWindowWidth = b.Width
            f.MainWindowHeight = b.Height
            f.Save(_boot.Paths.ConfigJson)
        End Sub

        ''' <summary>Quiesce the render loop, then Clean and Dispose (the ordering of the library's editor hosts).</summary>
        Private Sub TearDownPreview()
            PreviewTimer.Stop()
            If _preview IsNot Nothing AndAlso Not _preview.IsDisposed Then
                _preview.BeginTeardown()
                _preview.Clean()
                _preview.Dispose()
            End If
            _preview = Nothing
        End Sub

        ''' <summary>True once the preview control was torn down (the UI gate checks it after closing).</summary>
        Friend ReadOnly Property PreviewReleased As Boolean
            Get
                Return _preview Is Nothing
            End Get
        End Property

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = (Keys.Control Or Keys.S) Then
                SaveDecisions()
                Return True
            End If
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function

        ' ============================================================================================ grids

        Private Function GridOf(tab As ReviewTab) As DecisionGrid
            Select Case tab
                Case ReviewTab.Groups : Return GridGroups
                Case ReviewTab.Folders : Return GridFolders
                Case Else : Return GridObjects
            End Select
        End Function

        Private Function TabOf(grid As Object) As ReviewTab
            If grid Is GridGroups Then Return ReviewTab.Groups
            If grid Is GridFolders Then Return ReviewTab.Folders
            Return ReviewTab.Objects
        End Function

        Friend ReadOnly Property CurrentTab As ReviewTab
            Get
                Return CType(Tabs.SelectedIndex, ReviewTab)
            End Get
        End Property

        Private Sub SetupGrid(tab As ReviewTab)
            Dim grid = GridOf(tab)
            grid.Columns.Clear()
            For Each colName In ReviewSession.Columns(tab)
                Dim col As DataGridViewColumn
                If colName = ReviewSession.ColDecision Then
                    col = New DataGridViewCheckBoxColumn With {
                    .ThreeState = True, .ReadOnly = True, .Width = 62,
                    .ToolTipText = "Your decision: checked = Yes, empty = No, filled square = Undecided (the rules decide)."}
                Else
                    col = New DataGridViewTextBoxColumn With {.Width = ColumnWidth(tab, colName)}
                End If
                col.Name = colName
                col.HeaderText = colName
                col.SortMode = DataGridViewColumnSortMode.Programmatic
                If colName = ReviewSession.ColObjects OrElse colName = ReviewSession.ColYes OrElse colName = ReviewSession.ColNo OrElse colName = ReviewSession.ColReview Then
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                End If
                grid.Columns.Add(col)
            Next
            grid.Columns(grid.Columns.Count - 1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            grid.Columns(grid.Columns.Count - 1).MinimumWidth = 120
        End Sub

        Private Shared Function ColumnWidth(tab As ReviewTab, name As String) As Integer
            Select Case name
                Case ReviewSession.ColResult, ReviewSession.ColType, ReviewSession.ColObjects, ReviewSession.ColYes, ReviewSession.ColNo, ReviewSession.ColReview : Return 62
                Case ReviewSession.ColDecidedBy : Return 150
                Case ReviewSession.ColName : Return If(tab = ReviewTab.Groups, 220, 180)
                Case ReviewSession.ColEditorID : Return 200
                Case ReviewSession.ColKey : Return 180
                Case ReviewSession.ColFolder : Return 260
                Case ReviewSession.ColRuleSays : Return 220
                Case ReviewSession.ColOrigin : Return 170
                Case ReviewSession.ColRecipe : Return 260
                Case Else : Return 160
            End Select
        End Function

        Private Function RowKey(tab As ReviewTab, rowIndex As Integer) As String
            Dim rows As IReadOnlyList(Of String) = Nothing
            If Not _rows.TryGetValue(tab, rows) OrElse rowIndex < 0 OrElse rowIndex >= rows.Count Then Return Nothing
            Return rows(rowIndex)
        End Function

        Friend Function SelectedKeys(tab As ReviewTab) As List(Of String)
            Dim grid = GridOf(tab)
            Dim out As New List(Of String)
            For i = 0 To grid.RowCount - 1
                If grid.Rows.GetRowState(i).HasFlag(DataGridViewElementStates.Selected) Then out.Add(_rows(tab)(i))
            Next
            Return out
        End Function

        Friend Function CurrentKey(tab As ReviewTab) As String
            Dim grid = GridOf(tab)
            Return If(grid.CurrentCell Is Nothing, Nothing, RowKey(tab, grid.CurrentCell.RowIndex))
        End Function

        ''' <summary>The keys of a tab's rows as shown (the UI gate reads them).</summary>
        Friend Function ShownRows(tab As ReviewTab) As IReadOnlyList(Of String)
            Return _rows(tab)
        End Function

        ''' <summary>Re-reads the rows of a tab from the session, keeping the selection and the current row BY KEY.</summary>
        Private Sub RefreshRows(tab As ReviewTab)
            Dim grid = GridOf(tab)
            Dim hadRows = _rows.ContainsKey(tab)
            Dim sel = If(hadRows, New HashSet(Of String)(SelectedKeys(tab), StringComparer.Ordinal), New HashSet(Of String)(StringComparer.Ordinal))
            Dim cur = If(hadRows, CurrentKey(tab), Nothing)
            Dim s = _sort(tab)
            _refreshing = True
            Try
                _rows(tab) = _session.Rows(tab, CType(ComboShow.SelectedIndex, RowFilter), TextSearch.Text, s.Column, s.Descending)
                grid.RowCount = 0
                grid.RowCount = _rows(tab).Count
                Dim index As New Dictionary(Of String, Integer)(StringComparer.Ordinal)
                For i = 0 To _rows(tab).Count - 1
                    index(_rows(tab)(i)) = i
                Next
                Dim curIndex As Integer
                If cur IsNot Nothing AndAlso index.TryGetValue(cur, curIndex) Then
                    grid.CurrentCell = grid.Rows(curIndex).Cells(1)
                End If
                If hadRows Then
                    grid.ClearSelection()
                    For Each k In sel
                        Dim i As Integer
                        If index.TryGetValue(k, i) Then grid.Rows(i).Selected = True
                    Next
                End If
                grid.Invalidate()
            Finally
                _refreshing = False
            End Try
            If tab = CurrentTab Then UpdateCount()
        End Sub

        Private Sub RefreshAllRows()
            For Each tb As ReviewTab In {ReviewTab.Objects, ReviewTab.Groups, ReviewTab.Folders}
                RefreshRows(tb)
            Next
        End Sub

        Private Sub UpdateCount()
            Dim tab = CurrentTab
            Dim shown = If(_rows.ContainsKey(tab), _rows(tab).Count, 0)
            Dim total = _session.RowTotal(tab)
            LabelCount.Text = If(shown = total, $"{total:N0} rows", $"{shown:N0} of {total:N0} rows")
        End Sub

        Private Sub Grid_CellValueNeeded(sender As Object, e As DataGridViewCellValueEventArgs) Handles GridObjects.CellValueNeeded, GridGroups.CellValueNeeded, GridFolders.CellValueNeeded
            Dim tab = TabOf(sender)
            Dim key = RowKey(tab, e.RowIndex)
            If key Is Nothing Then Return
            Dim col = DirectCast(sender, DataGridView).Columns(e.ColumnIndex).Name
            If col = ReviewSession.ColDecision Then
                e.Value = ToCheckState(_session.Decision(tab, key))
            Else
                e.Value = _session.CellText(tab, key, col)
            End If
        End Sub

        Friend Shared Function ToCheckState(v As Verdict?) As CheckState
            If Not v.HasValue Then Return CheckState.Indeterminate
            Return If(v.Value = Verdict.Yes, CheckState.Checked, CheckState.Unchecked)
        End Function

        Private Sub Grid_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs) Handles GridObjects.CellFormatting, GridGroups.CellFormatting, GridFolders.CellFormatting
            Dim tab = TabOf(sender)
            Dim key = RowKey(tab, e.RowIndex)
            If key Is Nothing Then Return
            Dim v As Verdict?
            If tab = ReviewTab.Objects Then
                v = _session.Result(key).Verdict
            Else
                v = _session.Decision(tab, key)
            End If
            If Not v.HasValue Then Return
            e.CellStyle.BackColor = If(v.Value = Verdict.Yes, YesColor, If(v.Value = Verdict.No, NoColor, ReviewColor))
        End Sub

        Private Sub Grid_DecisionClicked(sender As Object, rowIndex As Integer) Handles GridObjects.DecisionClicked, GridGroups.DecisionClicked, GridFolders.DecisionClicked
            DecideFromRow(TabOf(sender), rowIndex)
        End Sub

        ''' <summary>The checkbox click / Space: the NEXT value is computed from <paramref name="rowIndex"/> (the clicked or
        ''' current row) and that same value is set on every selected row (rev-42).</summary>
        Friend Sub DecideFromRow(tab As ReviewTab, rowIndex As Integer)
            Dim key = RowKey(tab, rowIndex)
            If key Is Nothing Then Return
            Dim value = ReviewSession.NextDecision(_session.Decision(tab, key))
            Dim keys = SelectedKeys(tab)
            If Not keys.Contains(key) Then keys = New List(Of String) From {key}
            Decide(tab, keys, value)
        End Sub

        ''' <summary>Y / N / U: sets the value on every selected row.</summary>
        Friend Sub DecideSelected(tab As ReviewTab, value As Verdict?)
            Decide(tab, SelectedKeys(tab), value)
        End Sub

        Private Sub Decide(tab As ReviewTab, keys As List(Of String), value As Verdict?)
            If keys.Count = 0 Then Return
            _session.SetDecision(tab, keys, value)
            RefreshAllRows()
            UpdateStatus()
            ShowCurrent()
        End Sub

        Private Sub Grid_KeyDown(sender As Object, e As KeyEventArgs) Handles GridObjects.KeyDown, GridGroups.KeyDown, GridFolders.KeyDown
            If e.Modifiers <> Keys.None Then Return
            Dim tab = TabOf(sender)
            Select Case e.KeyCode
                Case Keys.Y : DecideSelected(tab, Verdict.Yes)
                Case Keys.N : DecideSelected(tab, Verdict.No)
                Case Keys.U : DecideSelected(tab, Nothing)
                Case Keys.Space
                    Dim grid = GridOf(tab)
                    If grid.CurrentCell IsNot Nothing Then DecideFromRow(tab, grid.CurrentCell.RowIndex)
                Case Else : Return
            End Select
            e.Handled = True
            e.SuppressKeyPress = True
        End Sub

        Private Sub Grid_ColumnHeaderMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles GridObjects.ColumnHeaderMouseClick, GridGroups.ColumnHeaderMouseClick, GridFolders.ColumnHeaderMouseClick
            Dim tab = TabOf(sender)
            Dim grid = GridOf(tab)
            Dim name = grid.Columns(e.ColumnIndex).Name
            Dim s = _sort(tab)
            _sort(tab) = (name, If(s.Column = name, Not s.Descending, False))
            For Each c As DataGridViewColumn In grid.Columns
                c.HeaderCell.SortGlyphDirection = If(c.Name = name, If(_sort(tab).Descending, SortOrder.Descending, SortOrder.Ascending), SortOrder.None)
            Next
            RefreshRows(tab)
        End Sub

        Private Sub Grid_SelectionChanged(sender As Object, e As EventArgs) Handles GridObjects.SelectionChanged, GridGroups.SelectionChanged, GridFolders.SelectionChanged
            If _refreshing OrElse TabOf(sender) <> CurrentTab Then Return
            ShowCurrent()
        End Sub

        Private Sub Tabs_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Tabs.SelectedIndexChanged
            UpdateCount()
            ShowCurrent()
        End Sub

        Private Sub TextSearch_TextChanged(sender As Object, e As EventArgs) Handles TextSearch.TextChanged
            SearchTimer.Stop()
            SearchTimer.Start()
        End Sub

        Private Sub SearchTimer_Tick(sender As Object, e As EventArgs) Handles SearchTimer.Tick
            SearchTimer.Stop()
            RefreshAllRows()
            ShowCurrent()
        End Sub

        Private Sub ComboShow_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboShow.SelectedIndexChanged
            If _session Is Nothing Then Return
            RefreshAllRows()
            ShowCurrent()
        End Sub

        ''' <summary>Shows a tab and its filter/search state as the UI gate asks (and as the user would set them).</summary>
        Friend Sub ShowTab(tab As ReviewTab, filter As RowFilter, search As String)
            Tabs.SelectedIndex = CInt(tab)
            ComboShow.SelectedIndex = CInt(filter)
            SearchTimer.Stop()
            TextSearch.Text = search
            SearchTimer.Stop()
            RefreshAllRows()
            ShowCurrent()
        End Sub

        ''' <summary>Selects rows of a tab by key (the first becomes the current row).</summary>
        Friend Sub SelectKeys(tab As ReviewTab, keys As IEnumerable(Of String))
            Dim grid = GridOf(tab)
            Dim wanted As New HashSet(Of String)(keys, StringComparer.Ordinal)
            _refreshing = True
            Try
                Dim first = -1
                For i = 0 To _rows(tab).Count - 1
                    If wanted.Contains(_rows(tab)(i)) Then
                        first = i
                        Exit For
                    End If
                Next
                If first >= 0 Then
                    grid.CurrentCell = grid.Rows(first).Cells(1)
                    If grid.Rows(first).Displayed = False Then grid.FirstDisplayedScrollingRowIndex = first
                End If
                grid.ClearSelection()
                For i = 0 To _rows(tab).Count - 1
                    If wanted.Contains(_rows(tab)(i)) Then grid.Rows(i).Selected = True
                Next
            Finally
                _refreshing = False
            End Try
            If tab = CurrentTab Then ShowCurrent()
        End Sub

        ' ============================================================================================ why + preview

        Private Sub ShowCurrent()
            If _session Is Nothing Then Return
            Dim tab = CurrentTab
            Dim key = CurrentKey(tab)
            If key Is Nothing Then
                TextWhy.Text = "Select a row to see why it has its result."
                ShowMembers(Nothing)
                QueuePreview(Nothing)
                Return
            End If
            Select Case tab
                Case ReviewTab.Objects
                    GroupWhy.Text = "Why this result"
                    TextWhy.Text = _session.Explain(key)
                    ShowMembers(Nothing)
                    QueuePreview(key)
                Case ReviewTab.Groups
                    GroupWhy.Text = "This group"
                    TextWhy.Text = _session.ExplainGroup(key)
                    ShowMembers(_session.MembersOf(tab, key))
                Case ReviewTab.Folders
                    GroupWhy.Text = "This folder"
                    TextWhy.Text = _session.ExplainFolder(key)
                    ShowMembers(_session.MembersOf(tab, key))
            End Select
        End Sub

        Private Sub ShowMembers(keys As IReadOnlyList(Of String))
            ListMembers.BeginUpdate()
            ListMembers.Items.Clear()
            If keys Is Nothing Then
                WhyLayout.RowStyles(1).Height = 0
                ListMembers.Visible = False
            Else
                WhyLayout.RowStyles(1).Height = 45
                ListMembers.Visible = True
                Dim items As New List(Of ListViewItem)(keys.Count)
                For Each k In keys
                    Dim o = _session.Catalog.Objects(k)
                    Dim r = _session.Result(k)
                    Dim it As New ListViewItem({ReviewSession.VerdictText(r.Verdict), o.Name, o.EditorID, o.Key}) With {.Tag = k}
                    it.BackColor = If(r.Verdict = Verdict.Yes, YesColor, If(r.Verdict = Verdict.No, NoColor, ReviewColor))
                    items.Add(it)
                Next
                ListMembers.Items.AddRange(items.ToArray())
                If ListMembers.Items.Count > 0 Then ListMembers.Items(0).Selected = True
            End If
            ListMembers.EndUpdate()
            If keys IsNot Nothing AndAlso keys.Count = 0 Then QueuePreview(Nothing)
        End Sub

        Private Sub ListMembers_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListMembers.SelectedIndexChanged
            If ListMembers.SelectedItems.Count = 0 Then Return
            QueuePreview(CStr(ListMembers.SelectedItems(0).Tag))
        End Sub

        Private Sub ListMembers_DoubleClick(sender As Object, e As EventArgs) Handles ListMembers.DoubleClick
            If ListMembers.SelectedItems.Count = 0 Then Return
            JumpToObject(CStr(ListMembers.SelectedItems(0).Tag))
        End Sub

        ''' <summary>Opens an object on the Objects tab; clears the filter and the search when they hide it.</summary>
        Private Sub JumpToObject(key As String)
            If Not _rows(ReviewTab.Objects).Contains(key) Then
                ComboShow.SelectedIndex = 0
                SearchTimer.Stop()
                TextSearch.Text = ""
                SearchTimer.Stop()
                RefreshAllRows()
            End If
            Tabs.SelectedIndex = CInt(ReviewTab.Objects)
            SelectKeys(ReviewTab.Objects, {key})
            GridObjects.Focus()
        End Sub

        Private Sub QueuePreview(objectKey As String)
            _pendingPreview = objectKey
            PreviewTimer.Stop()
            PreviewTimer.Start()
        End Sub

        Private Sub PreviewTimer_Tick(sender As Object, e As EventArgs) Handles PreviewTimer.Tick
            PreviewTimer.Stop()
            RenderPreview(_pendingPreview)
        End Sub

        ''' <summary>The text the viewer shows instead of a mesh ("" when it shows a mesh).</summary>
        Friend ReadOnly Property PreviewMessage As String
            Get
                Return If(LabelPreview.Visible, LabelPreview.Text, "")
            End Get
        End Property

        ''' <summary>The shapes of the mesh on screen (the UI gate checks the material was applied to them).</summary>
        Friend Property PreviewShapes As IReadOnlyList(Of IRenderableShape) = Array.Empty(Of IRenderableShape)()

        ''' <summary>Renders an object's model: its MODL through <see cref="FO4UnifiedMaterial_Class.CorrectMeshPath"/>, then
        ''' its material swap and color index (<see cref="ShapeMaterialOverrides.ApplyModelMaterial"/>). Without a mesh the
        ''' viewer says why (user's decision: a static collection whose combined mesh is missing is NOT drawn from its parts).</summary>
        Friend Sub RenderPreview(objectKey As String)
            If _preview Is Nothing OrElse _preview.IsDisposed Then Return
            If objectKey Is Nothing Then
                ShowPreviewMessage("Select an object to preview it.")
                Return
            End If
            Dim o = _session.Catalog.Objects(objectKey)
            If o.Signature = "NPC_" Then
                ShowPreviewMessage("Actors have no mesh to preview.")
                Return
            End If
            If Not o.HasModel Then
                ShowPreviewMessage("This object has no model.")
                Return
            End If
            If String.IsNullOrWhiteSpace(o.Model) Then
                ShowPreviewMessage("Its model path is empty.")
                Return
            End If
            Dim path = FO4UnifiedMaterial_Class.CorrectMeshPath(o.Model)
            Dim loc As FilesDictionary_class.File_Location = Nothing
            If Not FilesDictionary_class.Dictionary.TryGetValue(path, loc) OrElse loc Is Nothing Then
                ShowPreviewMessage("Mesh not found: " & path)
                Return
            End If
            Dim shapes As List(Of IRenderableShape)
            Try
                Dim bytes = loc.GetBytes()
                If bytes Is Nothing OrElse bytes.Length = 0 Then
                    ShowPreviewMessage("Mesh is empty: " & path)
                    Return
                End If
                Dim nif As New Nifcontent_Class_Manolo()
                nif.Load_Manolo(bytes)
                shapes = NifRenderableShape.FromNif(nif).Cast(Of IRenderableShape)().ToList()
            Catch ex As Exception
                ' A mod's mesh the reader cannot parse must not take the window down: the viewer says so (MeshPicker pattern).
                Logger.LogLazy(Function() $"[SAFESCRAP-PREVIEW] '{path}': {ex.GetType().Name}: {ex.Message}")
                ShowPreviewMessage($"Could not read the mesh: {path}{vbCrLf}{ex.GetType().Name}: {ex.Message}")
                Return
            End Try
            ShapeMaterialOverrides.ApplyModelMaterial(o.MaterialSwapFormID, o.ColorRemapIndex, shapes, _boot.Plugins)
            LabelPreview.Visible = False
            PreviewShapes = shapes
            _preview.RenderShapes(shapes, Nothing)
        End Sub

        Private Sub ShowPreviewMessage(text As String)
            PreviewShapes = Array.Empty(Of IRenderableShape)()
            _preview.RenderShapes(New List(Of IRenderableShape)(), Nothing)
            LabelPreview.Text = text
            LabelPreview.Visible = True
            LabelPreview.BringToFront()
        End Sub

        ''' <summary>A capture of the viewer (the GL surface does not come out of DrawToBitmap).</summary>
        Friend Function CapturePreview() As Bitmap
            If _preview Is Nothing OrElse _preview.IsDisposed Then Return Nothing
            Return _preview.CaptureBitmap()
        End Function

        ' ============================================================================================ status

        Private Sub UpdateStatus()
            Dim t = _session.Totals
            StatusTotals.Text = $"Yes {t.Yes:N0}   No {t.No:N0}   Review {t.Review:N0}   ({t.Objects:N0} objects in {_session.Catalog.Recipes.Count:N0} groups)"
            Dim d = _session.Decisions
            StatusDecisions.Text = $"Your decisions: {d.Objects.Count:N0} objects, {d.Groups.Count:N0} groups, {d.Folders.Count:N0} folders"
            StatusDirty.Visible = _session.Dirty
            Dim n = _boot.Plugins.Plugins.Count
            If _loadOrderDiffs.Count = 0 Then
                StatusLoadOrder.Text = $"{n} plugins loaded — the same as the game's load order"
                StatusLoadOrder.BackColor = SystemColors.Control
                StatusLoadOrder.ToolTipText = ""
            Else
                StatusLoadOrder.Text = $"{n} plugins loaded — DIFFERS from the game's load order (hover for details)"
                StatusLoadOrder.BackColor = Color.FromArgb(255, 236, 150)
                StatusLoadOrder.ToolTipText = "The list may not match what the game sees:" & vbCrLf & String.Join(vbCrLf, _loadOrderDiffs)
            End If
        End Sub

        ' ============================================================================================ actions

        Private Sub ButtonSave_Click(sender As Object, e As EventArgs) Handles ButtonSave.Click
            SaveDecisions()
        End Sub

        Private Sub SaveDecisions()
            Using New WaitCursor()
                _session.Save(_boot.Paths.DecisionsFile)
            End Using
            UpdateStatus()
        End Sub

        ''' <summary>SafeScrap's F4SE plugin (the workspace's single installer seat; the game guard is its own).</summary>
        Private ReadOnly _plugin As EmbeddedNativePlugin = GameExport.NewPlugin()

        ''' <summary>The export needs Fallout 4 as the game and its Data folder; says so when it cannot run.</summary>
        Private Function GameDataOrWarn(action As String) As String
            Dim data = If(Config_App.Current Is Nothing, "", Config_App.Current.DataPath)
            If _plugin.Applies(data) Then Return data
            AppDialogs.Warn(Me, AppTitle & " — " & action, "This needs Fallout 4 as the game and its Data folder (set in the load-order dialog).")
            Return Nothing
        End Function

        Private Sub ButtonExport_Click(sender As Object, e As EventArgs) Handles ButtonExport.Click
            Dim data = GameDataOrWarn("export")
            If data Is Nothing Then Return
            Dim text = GameExport.ConfirmationText(_session.Totals.Yes, _plugin, _session.Dirty, _loadOrderDiffs)
            If Not AppDialogs.Confirm(Me, AppTitle & " — export", text) Then Return

            Dim r As ExportResult
            Try
                Using New WaitCursor()
                    r = GameExport.Export(_session, _boot.Paths.DecisionsFile, data, _plugin)
                End Using
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException
                ' A failed save stops the export before anything reaches the game (rev-53); a failed list write too.
                UpdateStatus()
                AppDialogs.Warn(Me, AppTitle & " — export", "The export stopped: " & ex.Message & vbCr & vbCr &
                                If(_session.Dirty, "Your decisions could not be saved, so nothing was written to the game.", "The list could not be written."))
                Return
            End Try
            UpdateStatus()
            Dim msg As New Text.StringBuilder
            If r.SavedDecisions Then msg.AppendLine("Your decisions were saved.")
            msg.AppendLine(If(r.ListState = "unchanged",
                              $"The list in the game was already up to date ({r.Objects:N0} objects).",
                              $"The list was written: {r.Objects:N0} objects." & vbCrLf & r.ListPath))
            msg.AppendLine()
            Select Case r.PluginState
                Case "installed" : msg.AppendLine("The F4SE plugin was installed.")
                Case "up-to-date" : msg.AppendLine("The F4SE plugin was already up to date.")
                Case "missing-resource" : msg.AppendLine("The F4SE plugin is not part of this version yet: only the list was written.")
                Case Else : msg.AppendLine("The F4SE plugin could not be installed: " & r.PluginState)
            End Select
            AppDialogs.Info(Me, AppTitle & " — export", msg.ToString())
        End Sub

        Private Sub ButtonRemove_Click(sender As Object, e As EventArgs) Handles ButtonRemove.Click
            Dim data = GameDataOrWarn("remove")
            If data Is Nothing Then Return
            Dim present As New List(Of String)
            If IO.File.Exists(GameExport.ListPath(data)) Then present.Add("  Data\" & GameExport.ListRelativePath)
            If _plugin.IsInstalled(data) Then present.Add("  Data\" & _plugin.DataRelativePath)
            If present.Count = 0 Then
                AppDialogs.Info(Me, AppTitle & " — remove", "Nothing to remove: SafeScrap is not in your game folder.")
                Return
            End If
            If Not AppDialogs.Confirm(Me, AppTitle & " — remove", "This deletes from your game folder:" & vbCrLf & vbCrLf & String.Join(vbCrLf, present) & vbCrLf & vbCrLf &
                                      "Your decisions and rules in the app are kept.") Then Return
            Dim r As RemoveResult
            Try
                r = GameExport.Remove(data, _plugin)
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException
                AppDialogs.Warn(Me, AppTitle & " — remove", "The list could not be deleted: " & ex.Message)
                Return
            End Try
            Dim done As New List(Of String)
            If r.ListRemoved Then done.Add("The list was deleted.")
            If r.PluginState = "removed" Then done.Add("The F4SE plugin was deleted.")
            If r.PluginState.StartsWith("remove-failed", StringComparison.Ordinal) Then done.Add("The F4SE plugin could NOT be deleted: " & r.PluginState)
            AppDialogs.Info(Me, AppTitle & " — remove", If(done.Count = 0, "Nothing to remove.", String.Join(vbCrLf, done)))
        End Sub

        Private Sub ButtonClear_Click(sender As Object, e As EventArgs) Handles ButtonClear.Click
            Dim n = _session.DecisionCount
            If n = 0 Then
                AppDialogs.Info(Me, AppTitle & " — clear", "There are no decisions to clear.")
                Return
            End If
            If Not AppDialogs.Confirm(Me, AppTitle & " — clear", $"This sets your {n:N0} decisions (objects, groups and folders) back to Undecided: the rules decide." & vbCrLf & vbCrLf &
                                      "Your rules are kept. Nothing is saved until you press Save or Export.") Then Return
            Using New WaitCursor()
                _session.ClearDecisions()
            End Using
            RefreshAllRows()
            UpdateStatus()
            ShowCurrent()
        End Sub

        Private Sub ButtonHowItWorks_Click(sender As Object, e As EventArgs) Handles ButtonHowItWorks.Click
            Using d As New HowItWorksDialog()
                d.ShowDialog(Me)
            End Using
        End Sub

        Private Sub ButtonRules_Click(sender As Object, e As EventArgs) Handles ButtonRules.Click
            Using d As New RulesDialog(_boot, _session.Catalog)
                If d.ShowDialog(Me) <> DialogResult.OK Then Return
                _boot.Book = d.ResultBook
            End Using
            _session.ReloadRules(_boot.Book)
            RefreshAllRows()
            UpdateStatus()
            ShowCurrent()
        End Sub

        ''' <summary>Load order… (rev-36/rev-41): empty the viewer, reopen the load-order dialog; on OK build the new catalog
        ''' (a transaction in <see cref="AppBootstrap.UsePlugins"/>) and rebuild the session on the SAME decisions object
        ''' (unsaved changes and the unsaved mark kept). When the new load order is not taken (dialog cancelled or failed,
        ''' catalog rejected) the file dictionary is filled again for the plugins still in use before anything is drawn.</summary>
        Private Sub ButtonLoadOrder_Click(sender As Object, e As EventArgs) Handles ButtonLoadOrder.Click
            PreviewTimer.Stop()
            If _preview IsNot Nothing Then ShowPreviewMessage("Loading…")
            Dim accepted = False
            Try
                Using pre As New SafeScrapPreflight(_boot)
                    If pre.ShowDialog(Me) = DialogResult.OK Then
                        Try
                            _boot.UsePlugins(pre.LoadedPluginManager)
                            accepted = True
                        Catch ex As ScrapCategoryException
                            AppDialogs.Warn(Me, AppTitle & " — load order", ex.Message & vbCr & vbCr & "The previous load order stays in use.")
                        End Try
                    End If
                End Using
            Finally
                If Not accepted Then
                    Using New WaitCursor()
                        _boot.FillFiles(_boot.Plugins)
                    End Using
                End If
            End Try
            If accepted Then
                _session = New ReviewSession(_boot.Catalog, _boot.Book, _boot.Decisions, _session.Dirty)
                _rows.Clear()
                _loadOrderDiffs = _boot.SelectionDifferences()
                RefreshAllRows()
                UpdateStatus()
            End If
            ShowCurrent()
        End Sub

    End Class

End Namespace
