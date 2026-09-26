Option Strict On
Option Infer On

Imports SafeScrap.Engine

Namespace UI

''' <summary>The rules editor: folder rules, type rules and warning words. What the user edits is saved as a DIFFERENCE
''' over the app's rules (<see cref="RuleDelta"/>); keys are cleaned up with the engine's seats
''' (<see cref="RuleSet.NormalizeFolder"/>, upper case for types) and a repeated key, a key that can never match or a word
''' that is not letters only blocks Save. Each rule shows how many loaded objects it decides.</summary>
Partial Class RulesDialog

    ''' <summary>One table of keyed rules (folders or types): the same editing, checking and conflict handling for both.</summary>
    Private NotInheritable Class KeyedTable
        Friend Kind As RuleKind
        Friend Grid As DataGridView
        Friend KeyCol As DataGridViewColumn
        Friend DecisionCol As DataGridViewColumn
        Friend ReasonCol As DataGridViewColumn
        Friend MatchesCol As DataGridViewColumn
        Friend StatusCol As DataGridViewColumn
        ''' <summary>The engine's cleanup of a typed key.</summary>
        Friend Normalize As Func(Of String, String)
        ''' <summary>Why a key can never match, or Nothing.</summary>
        Friend Problem As Func(Of String, String)
        ''' <summary>How many loaded objects each of these rules decides.</summary>
        Friend Counts As Func(Of IReadOnlyList(Of KeyedRule), Dictionary(Of KeyedRule, Integer))
    End Class

    Private ReadOnly _boot As AppBootstrap
    Private ReadOnly _objectFolders As List(Of List(Of String))
    Private ReadOnly _objectTypes As Dictionary(Of String, Integer)
    Private ReadOnly _tables As New List(Of KeyedTable)
    ''' <summary>The app's rules plus the difference being edited (Keep mine / Use the app's replace it).</summary>
    Private _work As RuleBook
    Private _loading As Boolean
    ''' <summary>Restore defaults changed the files on disk: the main window must reload even if this dialog is cancelled.</summary>
    Private _restoredOnDisk As Boolean

    ''' <summary>The rules to use after OK.</summary>
    Friend Property ResultBook As RuleBook

    Friend Sub New(boot As AppBootstrap, catalog As ScrapCatalog)
        _boot = boot
        _work = boot.Book
        _objectFolders = catalog.Objects.Values.Select(Function(o) o.Folders.Distinct(StringComparer.Ordinal).ToList()).ToList()
        _objectTypes = catalog.Objects.Values.GroupBy(Function(o) o.Signature).ToDictionary(Function(g) g.Key, Function(g) g.Count(), StringComparer.Ordinal)
        InitializeComponent()
        ColCategory.Items.AddRange(_work.App.Warnings.Select(Function(c) CObj(c.Name)).ToArray())
        _tables.Add(New KeyedTable With {
            .Kind = RuleKind.Folder, .Grid = GridRules, .KeyCol = ColFolder, .DecisionCol = ColDecision, .ReasonCol = ColReason,
            .MatchesCol = ColMatches, .StatusCol = ColStatus,
            .Normalize = AddressOf RuleSet.NormalizeFolder, .Problem = AddressOf RuleSet.FolderKeyProblem,
            .Counts = Function(rules)
                          Dim asFolders = rules.Select(Function(r) New FolderRule With {.Folder = r.Key, .Decision = r.Decision, .Reason = r.Reason}).ToList()
                          Dim c = RuleSet.MatchCounts(asFolders, _objectFolders)
                          Return Enumerable.Range(0, rules.Count).ToDictionary(Function(i) rules(i), Function(i) c(asFolders(i)))
                      End Function})
        _tables.Add(New KeyedTable With {
            .Kind = RuleKind.Type, .Grid = GridTypes, .KeyCol = ColType, .DecisionCol = ColTypeDecision, .ReasonCol = ColTypeReason,
            .MatchesCol = ColTypeMatches, .StatusCol = ColTypeStatus,
            .Normalize = Function(t) If(t, "").Trim().ToUpperInvariant(), .Problem = AddressOf RuleSet.TypeKeyProblem,
            .Counts = Function(rules) rules.ToDictionary(Function(r) r, Function(r) If(_objectTypes.ContainsKey(r.Key), _objectTypes(r.Key), 0))})
        LoadRows()
    End Sub

    Private Function TableOf(kind As RuleKind) As KeyedTable
        Return _tables.First(Function(t) t.Kind = kind)
    End Function

    Private Function TableOf(grid As Object) As KeyedTable
        Return _tables.FirstOrDefault(Function(t) t.Grid Is grid)
    End Function

    Private Function EffectiveRules(kind As RuleKind) As IEnumerable(Of KeyedRule)
        Return If(kind = RuleKind.Type, _work.Effective.Types.Cast(Of KeyedRule)(), _work.Effective.Folders.Cast(Of KeyedRule)())
    End Function

    ' ============================================================================================ rows

    Private Sub LoadRows()
        _loading = True
        Try
            For Each t In _tables
                t.Grid.Rows.Clear()
                For Each r In EffectiveRules(t.Kind)
                    t.Grid.Rows.Add(r.Key, ReviewSession.VerdictText(r.Decision), r.Reason)
                Next
            Next
            GridWords.Rows.Clear()
            For Each c In _work.Effective.Warnings
                For Each w In c.Words.OrderBy(Function(x) x, StringComparer.Ordinal)
                    GridWords.Rows.Add(c.Name, w)
                Next
            Next
        Finally
            _loading = False
        End Try
        RefreshConflicts()
        ValidateRows()
    End Sub

    Private Shared Function CellText(row As DataGridViewRow, col As DataGridViewColumn) As String
        Return If(TryCast(row.Cells(col.Index).Value, String), "")
    End Function

    ''' <summary>Adds a rule the way the user does (Add, type, leave the cell): the key is cleaned up and the tables
    ''' re-checked. The UI gate calls it.</summary>
    Friend Function AddRow(kind As RuleKind, key As String, decision As Verdict, reason As String) As Integer
        Dim t = TableOf(kind)
        Dim i = t.Grid.Rows.Add(key, ReviewSession.VerdictText(decision), reason)
        NormalizeRow(t, i)
        ValidateRows()
        Return i
    End Function

    Friend Function AddFolderRow(folder As String, decision As Verdict, reason As String) As Integer
        Return AddRow(RuleKind.Folder, folder, decision, reason)
    End Function

    Friend Function KeyOfRow(kind As RuleKind, i As Integer) As String
        Dim t = TableOf(kind)
        Return CellText(t.Grid.Rows(i), t.KeyCol)
    End Function

    Friend Function FolderOfRow(i As Integer) As String
        Return KeyOfRow(RuleKind.Folder, i)
    End Function

    Friend ReadOnly Property SaveEnabled As Boolean
        Get
            Return ButtonSave.Enabled
        End Get
    End Property

    Private Sub NormalizeRow(t As KeyedTable, i As Integer)
        Dim raw = CellText(t.Grid.Rows(i), t.KeyCol)
        Dim norm = t.Normalize(raw)
        If norm <> raw Then
            _loading = True
            t.Grid.Rows(i).Cells(t.KeyCol.Index).Value = norm
            _loading = False
        End If
    End Sub

    Private Sub Keyed_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs) Handles GridRules.CellEndEdit, GridTypes.CellEndEdit
        Dim t = TableOf(sender)
        If e.ColumnIndex = t.KeyCol.Index Then NormalizeRow(t, e.RowIndex)
        ValidateRows()
    End Sub

    Private Sub Grid_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs) Handles GridRules.CurrentCellDirtyStateChanged, GridTypes.CurrentCellDirtyStateChanged, GridWords.CurrentCellDirtyStateChanged
        ' A combo box value counts at once, not when the cell is left.
        Dim g = DirectCast(sender, DataGridView)
        If g.IsCurrentCellDirty AndAlso TypeOf g.CurrentCell Is DataGridViewComboBoxCell Then g.CommitEdit(DataGridViewDataErrorContexts.Commit)
    End Sub

    Private Sub Grid_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles GridRules.CellValueChanged, GridTypes.CellValueChanged, GridWords.CellValueChanged
        If _loading OrElse e.RowIndex < 0 Then Return
        If sender Is GridWords AndAlso e.ColumnIndex = ColWord.Index Then
            Dim cell = GridWords.Rows(e.RowIndex).Cells(ColWord.Index)
            Dim t = If(TryCast(cell.Value, String), "").Trim().ToLowerInvariant()
            If t <> CStr(If(cell.Value, "")) Then
                _loading = True
                cell.Value = t
                _loading = False
            End If
        End If
        If Not DirectCast(sender, DataGridView).IsCurrentCellInEditMode Then ValidateRows()
    End Sub

    ''' <summary>The rows of a table as rules (rows with an empty key or no decision are left out; they are errors).</summary>
    Private Function RowRules(t As KeyedTable) As List(Of KeyedRule)
        Dim out As New List(Of KeyedRule)
        For Each row As DataGridViewRow In t.Grid.Rows
            Dim k = CellText(row, t.KeyCol)
            Dim d = CellText(row, t.DecisionCol)
            If k = "" OrElse d = "" Then Continue For
            out.Add(New KeyedRule With {.Key = k, .Decision = RuleSet.ParseVerdict(d.ToLowerInvariant(), "the rules table"), .Reason = CellText(row, t.ReasonCol)})
        Next
        Return out
    End Function

    Private Function RowWords() As Dictionary(Of String, IEnumerable(Of String))
        Dim out As New Dictionary(Of String, IEnumerable(Of String))(StringComparer.Ordinal)
        For Each c In _work.App.Warnings
            out(c.Name) = New List(Of String)
        Next
        For Each row As DataGridViewRow In GridWords.Rows
            Dim c = CellText(row, ColCategory)
            Dim w = CellText(row, ColWord)
            If c = "" OrElse w = "" OrElse Not out.ContainsKey(c) Then Continue For
            DirectCast(out(c), List(Of String)).Add(w)
        Next
        Return out
    End Function

    Private Shared ReadOnly BadRow As Color = Color.FromArgb(255, 215, 215)
    Private Shared ReadOnly ConflictRow As Color = Color.FromArgb(255, 240, 200)

    ''' <summary>Marks the problems, fills Decides and Status, and enables Save only when nothing blocks it.</summary>
    Friend Sub ValidateRows()
        Dim problems As New List(Of String)
        _loading = True
        Try
            For Each t In _tables
                ValidateTable(t, problems)
            Next
            Dim wordCount = GridWords.Rows.Cast(Of DataGridViewRow)().GroupBy(Function(r) CellText(r, ColCategory) & "|" & CellText(r, ColWord), StringComparer.Ordinal).ToDictionary(Function(g) g.Key, Function(g) g.Count(), StringComparer.Ordinal)
            For Each row As DataGridViewRow In GridWords.Rows
                Dim c = CellText(row, ColCategory)
                Dim w = CellText(row, ColWord)
                Dim err As String = Nothing
                If c = "" Then
                    err = "a word has no category"
                ElseIf Not RuleSet.IsValidWord(w) Then
                    err = If(w = "", "a word is empty", $"'{w}' is not letters only (a-z)")
                ElseIf wordCount(c & "|" & w) > 1 Then
                    err = $"'{w}' is twice in {c}"
                End If
                If err IsNot Nothing AndAlso Not problems.Contains(err) Then problems.Add(err)
                row.DefaultCellStyle.BackColor = If(err IsNot Nothing, BadRow, SystemColors.Window)
                Dim appCat = _work.App.Warnings.FirstOrDefault(Function(x) x.Name = c)
                row.Cells(ColWordStatus.Index).Value = If(appCat IsNot Nothing AndAlso appCat.Words.Contains(w), "App word", "Your word")
            Next
        Finally
            _loading = False
        End Try

        ButtonSave.Enabled = problems.Count = 0
        LabelProblems.Text = If(problems.Count = 0, "", "Fix before saving: " & String.Join("; ", problems.Take(6)) & If(problems.Count > 6, $"; and {problems.Count - 6} more", "") & ".")
    End Sub

    Private Sub ValidateTable(t As KeyedTable, problems As List(Of String))
        Dim what = RuleBook.KindName(t.Kind)
        Dim byKey = t.Grid.Rows.Cast(Of DataGridViewRow)().GroupBy(Function(r) CellText(r, t.KeyCol), StringComparer.Ordinal).ToDictionary(Function(g) g.Key, Function(g) g.Count(), StringComparer.Ordinal)
        Dim rules = RowRules(t)
        Dim counts = t.Counts(rules)
        Dim conflicted = New HashSet(Of String)(_work.Conflicts.Where(Function(c) c.Kind = t.Kind AndAlso Not c.IsRemoval).Select(Function(c) c.Key), StringComparer.Ordinal)
        For Each row As DataGridViewRow In t.Grid.Rows
            Dim k = CellText(row, t.KeyCol)
            Dim d = CellText(row, t.DecisionCol)
            Dim err As String = Nothing
            If k = "" Then
                err = $"a {what} rule has no {what}"
            ElseIf byKey(k) > 1 Then
                err = $"'{k}' has more than one rule"
            ElseIf t.Problem(k) IsNot Nothing Then
                err = t.Problem(k)
            ElseIf d = "" Then
                err = $"'{k}' has no decision"
            End If
            If err IsNot Nothing AndAlso Not problems.Contains(err) Then problems.Add(err)
            row.DefaultCellStyle.BackColor = If(err IsNot Nothing, BadRow, If(conflicted.Contains(k), ConflictRow, SystemColors.Window))

            Dim rule = rules.FirstOrDefault(Function(r) r.Key = k)
            Dim n = If(rule Is Nothing, 0, counts(rule))
            row.Cells(t.MatchesCol.Index).Value = If(err IsNot Nothing AndAlso rule Is Nothing, "", If(n = 1, "decides 1 object", $"decides {n:N0} objects"))
            row.Cells(t.MatchesCol.Index).Style.ForeColor = If(n = 0, Color.DarkRed, SystemColors.WindowText)
            row.Cells(t.StatusCol.Index).Value = RowStatus(t.Kind, k, d, CellText(row, t.ReasonCol), conflicted)
        Next
    End Sub

    Private Function RowStatus(kind As RuleKind, key As String, decision As String, reason As String, conflicted As HashSet(Of String)) As String
        If conflicted.Contains(key) Then
            Dim c = _work.Conflicts.First(Function(x) x.Kind = kind AndAlso Not x.IsRemoval AndAlso x.Key = key)
            Return "The app changed this rule since your edit — app now: " & RuleText(c.AppNow)
        End If
        Dim appR = _work.AppRule(kind, key)
        If appR Is Nothing Then Return "Your rule"
        If decision.Equals(ReviewSession.VerdictText(appR.Decision), StringComparison.Ordinal) AndAlso reason = appR.Reason Then Return "App rule"
        Return "Your change — app: " & RuleText(appR)
    End Function

    Private Shared Function RuleText(r As KeyedRule) As String
        If r Is Nothing Then Return "no rule"
        Return ReviewSession.VerdictText(r.Decision) & If(r.Reason <> "", $" ({r.Reason})", "")
    End Function

    ' ============================================================================================ conflicts

    Private Sub RefreshConflicts()
        ListConflicts.Items.Clear()
        For Each c In _work.Conflicts
            Dim mine = If(c.IsRemoval, "you removed it", "yours: " & RuleText(EffectiveRules(c.Kind).FirstOrDefault(Function(r) r.Key = c.Key)))
            ListConflicts.Items.Add(New ConflictItem(c, $"{RuleBook.KindName(c.Kind)} {c.Key}: {mine}; the app had {RuleText(c.Was)}, now {RuleText(c.AppNow)}"))
        Next
        For Each n In _work.Notes
            ListConflicts.Items.Add(n)
        Next
        GroupConflicts.Visible = ListConflicts.Items.Count > 0
        If ListConflicts.Items.Count > 0 Then ListConflicts.SelectedIndex = 0
    End Sub

    Private NotInheritable Class ConflictItem
        Friend ReadOnly Property Conflict As RuleConflict
        Private ReadOnly _text As String
        Friend Sub New(c As RuleConflict, text As String)
            Conflict = c
            _text = text
        End Sub
        Public Overrides Function ToString() As String
            Return _text
        End Function
    End Class

    Private Function SelectedConflict() As RuleConflict
        Return TryCast(ListConflicts.SelectedItem, ConflictItem)?.Conflict
    End Function

    Private Sub ButtonKeepMine_Click(sender As Object, e As EventArgs) Handles ButtonKeepMine.Click
        Dim c = SelectedConflict()
        If c Is Nothing Then Return
        _work = RuleBook.Compose(_work.App, _work.KeepMine(c.Kind, c.Key))
        RefreshConflicts()
        ValidateRows()
    End Sub

    Private Sub ButtonUseApps_Click(sender As Object, e As EventArgs) Handles ButtonUseApps.Click
        Dim c = SelectedConflict()
        If c Is Nothing Then Return
        _work = RuleBook.Compose(_work.App, _work.UseApps(c.Kind, c.Key))
        Dim t = TableOf(c.Kind)
        Dim appR = _work.AppRule(c.Kind, c.Key)
        Dim row = t.Grid.Rows.Cast(Of DataGridViewRow)().FirstOrDefault(Function(r) CellText(r, t.KeyCol) = c.Key)
        _loading = True
        Try
            If appR Is Nothing Then
                If row IsNot Nothing Then t.Grid.Rows.Remove(row)
            ElseIf row Is Nothing Then
                t.Grid.Rows.Add(appR.Key, ReviewSession.VerdictText(appR.Decision), appR.Reason)
            Else
                row.Cells(t.DecisionCol.Index).Value = ReviewSession.VerdictText(appR.Decision)
                row.Cells(t.ReasonCol.Index).Value = appR.Reason
            End If
        Finally
            _loading = False
        End Try
        RefreshConflicts()
        ValidateRows()
    End Sub

    ' ============================================================================================ buttons

    Private Sub AddKeyedRow(t As KeyedTable, page As TabPage, defaultDecision As String)
        Tabs.SelectedTab = page
        Dim i = t.Grid.Rows.Add("", defaultDecision, "")
        t.Grid.CurrentCell = t.Grid.Rows(i).Cells(t.KeyCol.Index)
        t.Grid.BeginEdit(True)
    End Sub

    Private Sub RemoveSelected(grid As DataGridView)
        For Each row In grid.SelectedRows.Cast(Of DataGridViewRow)().ToList()
            grid.Rows.Remove(row)
        Next
        ValidateRows()
    End Sub

    Private Sub ButtonAddRule_Click(sender As Object, e As EventArgs) Handles ButtonAddRule.Click
        AddKeyedRow(TableOf(RuleKind.Folder), TabFolderRules, "Yes")
    End Sub

    Private Sub ButtonRemoveRule_Click(sender As Object, e As EventArgs) Handles ButtonRemoveRule.Click
        RemoveSelected(GridRules)
    End Sub

    Private Sub ButtonAddType_Click(sender As Object, e As EventArgs) Handles ButtonAddType.Click
        AddKeyedRow(TableOf(RuleKind.Type), TabTypeRules, "No")
    End Sub

    Private Sub ButtonRemoveType_Click(sender As Object, e As EventArgs) Handles ButtonRemoveType.Click
        RemoveSelected(GridTypes)
    End Sub

    Private Sub ButtonAddWord_Click(sender As Object, e As EventArgs) Handles ButtonAddWord.Click
        Tabs.SelectedTab = TabWords
        Dim cat = If(GridWords.CurrentRow IsNot Nothing, CellText(GridWords.CurrentRow, ColCategory), _work.App.Warnings.First().Name)
        Dim i = GridWords.Rows.Add(cat, "")
        GridWords.CurrentCell = GridWords.Rows(i).Cells(ColWord.Index)
        GridWords.BeginEdit(True)
    End Sub

    Private Sub ButtonRemoveWord_Click(sender As Object, e As EventArgs) Handles ButtonRemoveWord.Click
        RemoveSelected(GridWords)
    End Sub

    Private Sub ButtonRestore_Click(sender As Object, e As EventArgs) Handles ButtonRestore.Click
        If Not AppDialogs.Confirm(Me, "Rules", "Drop all your rule changes and use the app's rules?" & vbCr &
                                  "Your changes are moved to a backup folder, not deleted.") Then Return
        Dim moved = RuleBook.RestoreDefaults(_boot.Paths.UserRulesDir, _boot.Paths.BackupDir)
        _work = RuleBook.Load(_boot.Paths.RulesDir, _boot.Paths.UserRulesDir)
        _restoredOnDisk = True
        LoadRows()
        AppDialogs.Info(Me, "Rules", If(moved Is Nothing, "You had no rule changes.", "Your rule changes were moved to:" & vbCr & moved))
    End Sub

    ''' <summary>Saves the difference and closes; the resulting rules are checked before anything is written.</summary>
    Friend Function SaveRules() As Boolean
        For Each t In _tables
            t.Grid.EndEdit()
        Next
        GridWords.EndEdit()
        ValidateRows()
        If Not ButtonSave.Enabled Then Return False
        Dim delta = _work.DeltaFrom(RowRules(TableOf(RuleKind.Folder)), RowRules(TableOf(RuleKind.Type)), RowWords())
        Dim book As RuleBook
        Try
            book = RuleBook.Compose(_work.App, delta)
        Catch ex As RuleDataException
            AppDialogs.Warn(Me, "Rules", ex.Message)
            Return False
        End Try
        delta.Save(_boot.Paths.UserRulesDir)
        ResultBook = book
        Return True
    End Function

    Private Sub ButtonSave_Click(sender As Object, e As EventArgs) Handles ButtonSave.Click
        If Not SaveRules() Then Return
        DialogResult = DialogResult.OK
        Close()
    End Sub

    Private Sub RulesDialog_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If DialogResult <> DialogResult.OK AndAlso _restoredOnDisk Then
            ResultBook = _work
            DialogResult = DialogResult.OK
        End If
    End Sub

    Private Sub Grid_DataError(sender As Object, e As DataGridViewDataErrorEventArgs) Handles GridRules.DataError, GridTypes.DataError, GridWords.DataError
        ' A combo value that is not in its list: shown as a problem by ValidateRows, not as the grid's exception dialog.
        e.ThrowException = False
    End Sub

End Class

End Namespace
