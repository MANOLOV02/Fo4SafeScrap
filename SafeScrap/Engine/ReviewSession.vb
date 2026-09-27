Option Strict On
Option Infer On

Imports System.Diagnostics
Imports System.Text

Namespace Engine

    ''' <summary>The three lists of the review window.</summary>
    Friend Enum ReviewTab
        Objects = 0
        Groups = 1
        Folders = 2
    End Enum

    ''' <summary>The "Show" filter. On Groups and Folders, Yes / No / Review mean "has at least one object with that
    ''' result" and With warning "has at least one object with a warning".</summary>
    Friend Enum RowFilter
        All = 0
        Undecided = 1
        Decided = 2
        Yes = 3
        No = 4
        Review = 5
        ''' <summary>Objects whose warning matters: lowered to Review by it, or Yes although the name warns.</summary>
        WithWarning = 6
    End Enum

    ''' <summary>The results of a set of objects.</summary>
    Friend NotInheritable Class Tally
        Friend Property Objects As Integer
        Friend Property Yes As Integer
        Friend Property No As Integer
        Friend Property Review As Integer
        ''' <summary>Objects whose warning matters (<see cref="ObjectResult.WarningMatters"/>).</summary>
        Friend Property Warnings As Integer

        Friend Sub Add(r As ObjectResult)
            Objects += 1
            Select Case r.Verdict
                Case Verdict.Yes : Yes += 1
                Case Verdict.No : No += 1
                Case Else : Review += 1
            End Select
            If r.WarningMatters Then Warnings += 1
        End Sub

        Friend Function Count(v As Verdict) As Integer
            Select Case v
                Case Verdict.Yes : Return Yes
                Case Verdict.No : Return No
                Case Else : Return Review
            End Select
        End Function
    End Class

    ''' <summary>The review session: catalog + rules + the user's decisions + every object's result, cached and
    ''' recalculated after each change. The ONE seat of the review logic: the main window only paints what this class
    ''' answers, and the UI gate calls this very class.</summary>
    Friend NotInheritable Class ReviewSession

        Friend Const ColDecision As String = "Decision"
        Friend Const ColResult As String = "Result"
        Friend Const ColDecidedBy As String = "Decided by"
        Friend Const ColName As String = "Name"
        Friend Const ColEditorID As String = "EditorID"
        Friend Const ColType As String = "Type"
        Friend Const ColKey As String = "Key"
        Friend Const ColFolders As String = "Folders"
        Friend Const ColWarning As String = "Warning"
        Friend Const ColObjects As String = "Objects"
        Friend Const ColYes As String = "Yes"
        Friend Const ColNo As String = "No"
        Friend Const ColReview As String = "Review"
        Friend Const ColOrigin As String = "Origin"
        Friend Const ColRecipe As String = "Recipe"
        Friend Const ColKind As String = "Kind"
        Friend Const ColSize As String = "Size"
        Friend Const ColReturns As String = "Returns"
        Friend Const ColFolder As String = "Folder"
        Friend Const ColRuleSays As String = "Rule says"

        Private Shared ReadOnly ObjectColumns As String() = {ColDecision, ColResult, ColDecidedBy, ColName, ColSize, ColEditorID, ColType, ColKey, ColFolders, ColWarning}
        Private Shared ReadOnly GroupColumns As String() = {ColDecision, ColName, ColKind, ColObjects, ColYes, ColNo, ColReview, ColOrigin, ColRecipe, ColReturns}
        Private Shared ReadOnly FolderColumns As String() = {ColDecision, ColFolder, ColRuleSays, ColObjects, ColYes, ColNo, ColReview}
        Private Shared ReadOnly NumericColumns As New HashSet(Of String)(StringComparer.Ordinal) From {ColObjects, ColYes, ColNo, ColReview, ColSize}

        Friend ReadOnly Property Catalog As ScrapCatalog
        Friend ReadOnly Property Decisions As Decisions
        Friend Property Book As RuleBook
            Get
                Return _book
            End Get
            Private Set(value As RuleBook)
                _book = value
            End Set
        End Property
        Private _book As RuleBook

        ''' <summary>True when the decisions changed since they were loaded or saved.</summary>
        Friend Property Dirty As Boolean
        ''' <summary>How long the last full recalculation took.</summary>
        Friend Property LastRecalcMilliseconds As Double

        Private _evaluator As Evaluator
        Private ReadOnly _results As New Dictionary(Of String, ObjectResult)(StringComparer.Ordinal)
        Private ReadOnly _groupTally As New Dictionary(Of String, Tally)(StringComparer.Ordinal)
        Private ReadOnly _folderTally As New Dictionary(Of String, Tally)(StringComparer.Ordinal)
        Private ReadOnly _groups As New Dictionary(Of String, ScrapRecipe)(StringComparer.Ordinal)
        Private ReadOnly _folderMembers As New Dictionary(Of String, List(Of String))(StringComparer.Ordinal)
        Private _totals As New Tally

        ''' <param name="dirty">The state of <paramref name="decisions"/> (a session rebuilt on a new load order keeps it).</param>
        Friend Sub New(catalog As ScrapCatalog, book As RuleBook, decisions As Decisions, Optional dirty As Boolean = False)
            Me.Catalog = catalog
            Me.Decisions = decisions
            Me.Dirty = dirty
            For Each r In catalog.Recipes
                _groups(r.Key) = r
            Next
            For Each key In catalog.Objects.Keys.OrderBy(Function(k) k, StringComparer.Ordinal)
                For Each f In catalog.Objects(key).Folders.Distinct(StringComparer.Ordinal)
                    Dim l As List(Of String) = Nothing
                    If Not _folderMembers.TryGetValue(f, l) Then
                        l = New List(Of String)
                        _folderMembers(f) = l
                    End If
                    l.Add(key)
                Next
            Next
            UseRules(book)
        End Sub

        Friend ReadOnly Property Rules As RuleSet
            Get
                Return _book.Effective
            End Get
        End Property

        ''' <summary>Takes new rules (after the rules editor) and recalculates.</summary>
        Friend Sub ReloadRules(book As RuleBook)
            UseRules(book)
        End Sub

        Private Sub UseRules(book As RuleBook)
            _book = book
            _evaluator = New Evaluator(book.Effective, Decisions)
            Recalculate()
        End Sub

        ''' <summary>Evaluates every object and rebuilds the per-group and per-folder tallies.</summary>
        Friend Sub Recalculate()
            Dim sw = Stopwatch.StartNew()
            _results.Clear()
            _totals = New Tally
            For Each o In Catalog.Objects.Values
                Dim r = _evaluator.Evaluate(o)
                _results(o.Key) = r
                _totals.Add(r)
            Next
            _groupTally.Clear()
            For Each g In Catalog.Recipes
                Dim t As New Tally
                For Each m In g.Members
                    t.Add(_results(m))
                Next
                _groupTally(g.Key) = t
            Next
            _folderTally.Clear()
            For Each kv In _folderMembers
                Dim t As New Tally
                For Each m In kv.Value
                    t.Add(_results(m))
                Next
                _folderTally(kv.Key) = t
            Next
            LastRecalcMilliseconds = sw.Elapsed.TotalMilliseconds
        End Sub

        Friend Function Result(objectKey As String) As ObjectResult
            Return _results(objectKey)
        End Function

        Friend ReadOnly Property Totals As Tally
            Get
                Return _totals
            End Get
        End Property

        Friend Function GroupTally(key As String) As Tally
            Return _groupTally(key)
        End Function

        Friend Function FolderTally(key As String) As Tally
            Return _folderTally(key)
        End Function

        Friend Function Group(key As String) As ScrapRecipe
            Return _groups(key)
        End Function

        ''' <summary>The folders that have objects (the Folders tab), Ordinal.</summary>
        Friend ReadOnly Property FolderKeys As IEnumerable(Of String)
            Get
                Return _folderMembers.Keys
            End Get
        End Property

        ''' <summary>The objects of a group or a folder (for an object: itself).</summary>
        Friend Function MembersOf(tab As ReviewTab, key As String) As IReadOnlyList(Of String)
            Select Case tab
                Case ReviewTab.Groups : Return _groups(key).Members.Distinct(StringComparer.Ordinal).ToList()
                Case ReviewTab.Folders : Return _folderMembers(key)
                Case Else : Return {key}
            End Select
        End Function

        ' ============================================================================================ decisions

        Private Function Store(tab As ReviewTab) As Dictionary(Of String, Verdict)
            Select Case tab
                Case ReviewTab.Groups : Return Decisions.Groups
                Case ReviewTab.Folders : Return Decisions.Folders
                Case Else : Return Decisions.Objects
            End Select
        End Function

        ''' <summary>The user's decision on that tab's row (Nothing = undecided).</summary>
        Friend Function Decision(tab As ReviewTab, key As String) As Verdict?
            Dim v As Verdict
            If Store(tab).TryGetValue(key, v) Then Return v
            Return Nothing
        End Function

        ''' <summary>Sets (or, with Nothing, clears) the user's decision on every key, then recalculates.</summary>
        Friend Sub SetDecision(tab As ReviewTab, keys As IEnumerable(Of String), value As Verdict?)
            Dim st = Store(tab)
            Dim changed = False
            For Each k In keys
                Dim cur As Verdict
                Dim has = st.TryGetValue(k, cur)
                If value.HasValue Then
                    If has AndAlso cur = value.Value Then Continue For
                    st(k) = value.Value
                    changed = True
                ElseIf has Then
                    st.Remove(k)
                    changed = True
                End If
            Next
            If Not changed Then Return
            Dirty = True
            Recalculate()
        End Sub

        ''' <summary>Clears every decision on the three tabs (user's request: one button); the rules are untouched.</summary>
        Friend Sub ClearDecisions()
            If DecisionCount = 0 Then Return
            Decisions.Objects.Clear()
            Decisions.Groups.Clear()
            Decisions.Folders.Clear()
            Dirty = True
            Recalculate()
        End Sub

        ''' <summary>The checkbox cycle (user's request: true / false / undetermined): Undecided → Yes → No → Undecided.</summary>
        Friend Shared Function NextDecision(current As Verdict?) As Verdict?
            If Not current.HasValue Then Return Verdict.Yes
            If current.Value = Verdict.Yes Then Return Verdict.No
            Return Nothing
        End Function

        Friend Sub Save(path As String)
            Decisions.Save(path)
            Dirty = False
        End Sub

        Friend ReadOnly Property DecisionCount As Integer
            Get
                Return Decisions.Objects.Count + Decisions.Groups.Count + Decisions.Folders.Count
            End Get
        End Property

        ' ============================================================================================ rows

        Friend Shared Function Columns(tab As ReviewTab) As IReadOnlyList(Of String)
            Select Case tab
                Case ReviewTab.Groups : Return GroupColumns
                Case ReviewTab.Folders : Return FolderColumns
                Case Else : Return ObjectColumns
            End Select
        End Function

        ''' <summary>How many rows a tab has without filter or search.</summary>
        Friend Function RowTotal(tab As ReviewTab) As Integer
            Select Case tab
                Case ReviewTab.Groups : Return _groups.Count
                Case ReviewTab.Folders : Return _folderMembers.Count
                Case Else : Return Catalog.Objects.Count
            End Select
        End Function

        Private Function AllKeys(tab As ReviewTab) As IEnumerable(Of String)
            Select Case tab
                Case ReviewTab.Groups : Return _groups.Keys
                Case ReviewTab.Folders : Return _folderMembers.Keys
                Case Else : Return Catalog.Objects.Keys
            End Select
        End Function

        ''' <summary>The keys of a tab's rows, filtered, searched (case-insensitive, on name, EditorID, key and folders),
        ''' limited to <paramref name="recordType"/> (Nothing = every type; on Groups and Folders: the rows with an object of
        ''' that type) and to <paramref name="onlyObjects"/> (object keys, e.g. a workshop's; same rule), and sorted by <paramref name="sortColumn"/> (Nothing = by key); ties by key, Ordinal.</summary>
        Friend Function Rows(tab As ReviewTab, filter As RowFilter, search As String, sortColumn As String, descending As Boolean,
                             Optional recordType As String = Nothing, Optional onlyObjects As IReadOnlyDictionary(Of String, Integer) = Nothing) As IReadOnlyList(Of String)
            Dim q = If(search, "").Trim()
            Dim keys = AllKeys(tab).Where(Function(k) Passes(tab, k, filter) AndAlso (q = "" OrElse Matches(tab, k, q)) AndAlso
                                                      (recordType Is Nothing OrElse HasType(tab, k, recordType)) AndAlso
                                                      (onlyObjects Is Nothing OrElse MembersOf(tab, k).Any(Function(m) onlyObjects.ContainsKey(m)))).ToList()
            Dim col = If(String.IsNullOrEmpty(sortColumn), ColKey, sortColumn)
            Dim cmp As Comparison(Of String)
            If NumericColumns.Contains(col) Then
                Dim num = keys.ToDictionary(Function(k) k, Function(k) Number(tab, k, col), StringComparer.Ordinal)
                cmp = Function(a, b)
                          Dim c = num(a).CompareTo(num(b))
                          Return If(c <> 0, If(descending, -c, c), String.CompareOrdinal(a, b))
                      End Function
            Else
                Dim txt = keys.ToDictionary(Function(k) k, Function(k) CellText(tab, k, col), StringComparer.Ordinal)
                cmp = Function(a, b)
                          Dim c = StringComparer.OrdinalIgnoreCase.Compare(txt(a), txt(b))
                          Return If(c <> 0, If(descending, -c, c), String.CompareOrdinal(a, b))
                      End Function
            End If
            keys.Sort(cmp)
            Return keys
        End Function

        Private Function Passes(tab As ReviewTab, key As String, filter As RowFilter) As Boolean
            Select Case filter
                Case RowFilter.All : Return True
                Case RowFilter.Undecided : Return Not Decision(tab, key).HasValue
                Case RowFilter.Decided : Return Decision(tab, key).HasValue
            End Select
            If tab = ReviewTab.Objects Then
                Dim r = _results(key)
                Select Case filter
                    Case RowFilter.Yes : Return r.Verdict = Verdict.Yes
                    Case RowFilter.No : Return r.Verdict = Verdict.No
                    Case RowFilter.Review : Return r.Verdict = Verdict.Review
                    Case Else : Return r.WarningMatters
                End Select
            End If
            Dim t = If(tab = ReviewTab.Groups, _groupTally(key), _folderTally(key))
            Select Case filter
                Case RowFilter.Yes : Return t.Yes > 0
                Case RowFilter.No : Return t.No > 0
                Case RowFilter.Review : Return t.Review > 0
                Case Else : Return t.Warnings > 0
            End Select
        End Function

        Private Function HasType(tab As ReviewTab, key As String, recordType As String) As Boolean
            Return MembersOf(tab, key).Any(Function(m) Catalog.Objects(m).Signature = recordType)
        End Function

        Private Function Matches(tab As ReviewTab, key As String, q As String) As Boolean
            Dim hay As IEnumerable(Of String)
            Select Case tab
                Case ReviewTab.Groups
                    Dim g = _groups(key)
                    hay = {g.Key, g.EditorID, g.TargetKey, g.TargetEditorID, g.TargetName}
                Case ReviewTab.Folders
                    hay = {key}
                Case Else
                    Dim o = Catalog.Objects(key)
                    hay = {o.Key, o.EditorID, o.Name}.Concat(o.Folders)
            End Select
            Return hay.Any(Function(h) h IsNot Nothing AndAlso h.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
        End Function

        Private Function Number(tab As ReviewTab, key As String, col As String) As Integer
            If col = ColSize Then Return LongestSide(Catalog.Objects(key))
            Dim t = If(tab = ReviewTab.Groups, _groupTally(key), _folderTally(key))
            Select Case col
                Case ColYes : Return t.Yes
                Case ColNo : Return t.No
                Case ColReview : Return t.Review
                Case Else : Return t.Objects
            End Select
        End Function

        ''' <summary>The text of a cell (also what the column sorts by, except the number columns).</summary>
        Friend Function CellText(tab As ReviewTab, key As String, col As String) As String
            If col = ColDecision Then
                Dim d = Decision(tab, key)
                Return If(d.HasValue, VerdictText(d.Value), "")
            End If
            Select Case tab
                Case ReviewTab.Objects
                    Dim o = Catalog.Objects(key)
                    Dim r = _results(key)
                    Select Case col
                        Case ColResult : Return VerdictText(r.Verdict)
                        Case ColDecidedBy : Return DecidedByText(r)
                        Case ColName : Return o.Name
                        Case ColEditorID : Return o.EditorID
                        Case ColType : Return o.Signature
                        Case ColSize : Return SizeText(o)
                        Case ColKey : Return o.Key
                        Case ColFolders : Return String.Join(", ", o.Folders.Distinct(StringComparer.Ordinal))
                        Case ColWarning : Return r.Warning
                    End Select
                Case ReviewTab.Groups
                    Dim g = _groups(key)
                    Select Case col
                        Case ColName : Return GroupName(g)
                        Case ColKind : Return KindText(g.Kind)
                        Case ColOrigin : Return g.SourcePlugin
                        Case ColRecipe : Return $"{g.EditorID} ({g.Key})"
                        Case ColKey : Return g.Key
                        Case ColReturns : Return String.Join(", ", g.Components.Select(Function(c) $"{c.Count} {If(c.Name <> "", c.Name, c.Key)}"))
                        Case ColObjects, ColYes, ColNo, ColReview : Return Number(tab, key, col).ToString(Globalization.CultureInfo.InvariantCulture)
                    End Select
                Case ReviewTab.Folders
                    Select Case col
                        Case ColFolder, ColKey : Return key
                        Case ColRuleSays
                            Dim rule = Rules.Match(key)
                            If rule Is Nothing Then Return "no rule (Review)"
                            Return VerdictText(rule.Decision) & If(rule.Reason <> "", $" ({rule.Reason})", "") & If(rule.Folder <> key, $" — from {rule.Folder}", "")
                        Case ColObjects, ColYes, ColNo, ColReview : Return Number(tab, key, col).ToString(Globalization.CultureInfo.InvariantCulture)
                    End Select
            End Select
            Return ""
        End Function

        Friend Shared Function GroupName(g As ScrapRecipe) As String
            If g.TargetName <> "" Then Return g.TargetName
            If g.TargetEditorID <> "" Then Return g.TargetEditorID
            Return g.EditorID
        End Function

        ''' <summary>The bounds as "X × Y × Z" (game units), or "" without bounds.</summary>
        Friend Shared Function SizeText(o As ScrapObject) As String
            If Not o.Size.HasValue Then Return ""
            Dim s = o.Size.Value
            Return String.Format(Globalization.CultureInfo.InvariantCulture, "{0} × {1} × {2}", s.X, s.Y, s.Z)
        End Function

        ''' <summary>What the Size column sorts by: the longest side of the bounds (−1 without bounds).</summary>
        Friend Shared Function LongestSide(o As ScrapObject) As Integer
            If Not o.Size.HasValue Then Return -1
            Dim s = o.Size.Value
            Return Math.Max(s.X, Math.Max(s.Y, s.Z))
        End Function

        Friend Shared Function KindText(k As RecipeKind) As String
            Return If(k = RecipeKind.Scrap, "Scrap", "Build")
        End Function

        Friend Shared Function VerdictText(v As Verdict) As String
            Select Case v
                Case Verdict.Yes : Return "Yes"
                Case Verdict.No : Return "No"
                Case Else : Return "Review"
            End Select
        End Function

        Friend Function DecidedByText(r As ObjectResult) As String
            Select Case r.DecidedBy
                Case DecidedBy.ObjectDecision : Return "Your object decision"
                Case DecidedBy.GroupDecision : Return "Your group decision"
                Case DecidedBy.FolderLoweredByWarning : Return "Folder, lowered by warning"
                Case DecidedBy.TypeRule : Return "Type rule"
                Case Else
                    If r.Steps.Any(Function(s) s.Kind = DecidedBy.Folder AndAlso s.ByUser) Then Return "Folder (your decision)"
                    If r.Steps.All(Function(s) Not s.ByUser AndAlso Rules.Match(s.Subject) Is Nothing) Then Return "No rule"
                    Return "Folder rule"
            End Select
        End Function

        ' ============================================================================================ explanations

        ''' <summary>Why an object has its result, in plain English: one line per rule step (naming the step's subject and
        ''' what it says) and what the user can do.</summary>
        Friend Function Explain(objectKey As String) As String
            Dim o = Catalog.Objects(objectKey)
            Dim r = _results(objectKey)
            Dim sb As New StringBuilder
            sb.AppendLine($"{If(o.Name <> "", o.Name, o.EditorID)}  —  {o.Signature} {o.EditorID}  ({o.Key})")
            sb.AppendLine($"Result: {VerdictText(r.Verdict)}   ·   decided by: {DecidedByText(r)}")
            If o.Size.HasValue Then sb.AppendLine($"Size: {SizeText(o)} game units (its bounds)")
            sb.AppendLine()
            sb.AppendLine("How the result came out (the first level that decides wins):")

            Dim own = r.Steps.FirstOrDefault(Function(s) s.Kind = DecidedBy.ObjectDecision)
            If own IsNot Nothing Then
                sb.AppendLine($"  1. Your decision on this object {own.Subject}: {VerdictText(own.Says.Value)}. It overrides everything below.")
            Else
                sb.AppendLine("  1. Your decision on this object: none.")
                Dim groupSteps = r.Steps.Where(Function(s) s.Kind = DecidedBy.GroupDecision).ToList()
                If groupSteps.Count = 0 Then
                    sb.AppendLine($"  2. Groups (recipes) that contain it: {o.Recipes.Count}, none decided by you.")
                Else
                    sb.AppendLine($"  2. Groups (recipes) that contain it: {o.Recipes.Count}; your decisions on them (No wins over Yes):")
                    For Each s In groupSteps
                        Dim g As ScrapRecipe = Nothing
                        Dim label = If(_groups.TryGetValue(s.Subject, g), $"{GroupName(g)} [{g.EditorID}]", "")
                        sb.AppendLine($"       - group {s.Subject} {label}: {VerdictText(s.Says.Value)}")
                    Next
                End If
                Dim typeStep = r.Steps.FirstOrDefault(Function(s) s.Kind = DecidedBy.TypeRule)
                If groupSteps.Count = 0 Then
                    Dim typeRule = Rules.TypeRuleOf(o.Signature)
                    If typeStep IsNot Nothing Then
                        sb.AppendLine($"  3. Type rule for {typeStep.Subject}: {VerdictText(typeStep.Says.Value)}{If(typeStep.Detail <> "", $" ({typeStep.Detail})", "")}. It wins over the folder rules.")
                    ElseIf typeRule IsNot Nothing Then
                        sb.AppendLine($"  3. Type rule for {o.Signature} ({VerdictText(typeRule.Decision)}) does not apply: you decided one of its folders.")
                    Else
                        sb.AppendLine($"  3. Type rule for {o.Signature}: none.")
                    End If
                End If
                If groupSteps.Count = 0 AndAlso typeStep Is Nothing Then
                    Dim folderSteps = r.Steps.Where(Function(s) s.Kind = DecidedBy.Folder).ToList()
                    sb.AppendLine(If(o.Signature = "SCOL",
                        "  4. Model folders of its parts (a static collection: any No ⇒ No; all Yes ⇒ Yes; otherwise Review):",
                        "  4. Model folder (where its mesh is filed):"))
                    For Each s In folderSteps
                        Dim who = If(s.ByUser, "your decision", If(s.Says.HasValue AndAlso Rules.Match(s.Subject) IsNot Nothing, $"rule {Rules.Match(s.Subject).Folder}", "no rule"))
                        Dim says = VerdictText(If(s.Says, Verdict.Review))
                        sb.AppendLine($"       - folder {s.Subject}: {says} ({who}{If(s.Detail <> "" AndAlso Not s.ByUser, ": " & s.Detail, "")})")
                    Next
                    Dim warn = r.Steps.FirstOrDefault(Function(s) s.Kind = DecidedBy.FolderLoweredByWarning)
                    If warn IsNot Nothing Then
                        sb.AppendLine($"  5. Warning: the EditorID {warn.Subject} mentions {warn.Detail} ⇒ lowered from Yes to {VerdictText(warn.Says.Value)}.")
                    ElseIf r.Warning <> "" Then
                        sb.AppendLine($"  Note: the EditorID mentions {r.Warning} (only lowers a Yes coming from the folders).")
                    End If
                End If
            End If

            sb.AppendLine()
            sb.AppendLine("What you can do:")
            Select Case r.DecidedBy
                Case DecidedBy.ObjectDecision
                    sb.AppendLine("  This is your own decision. Set it to Undecided (U) to let its groups and folders decide again.")
                Case DecidedBy.GroupDecision
                    sb.AppendLine("  A group decision covers every object of the group. Decide this object on its own (Y / N) to override it, or change the group on the Groups tab.")
                Case DecidedBy.FolderLoweredByWarning
                    sb.AppendLine("  The folder says junk, but the name suggests a plant, terrain or a building part. Look at the preview: Yes (Y) if it is junk, No (N) if it is not.")
                Case DecidedBy.TypeRule
                    sb.AppendLine($"  Every {o.Signature} gets this by default. To scrap this one, set Yes (Y) here; to change it for all of them, edit the type rule in Rules….")
                Case Else
                    Select Case r.Verdict
                        Case Verdict.Yes : sb.AppendLine("  The folder says it is junk. If it is not, set No (N) here, or decide the folder on the Folders tab.")
                        Case Verdict.No : sb.AppendLine("  The folder says it is not junk. If it is, set Yes (Y) here.")
                        Case Else
                            sb.AppendLine("  Nobody decided and the rules cannot tell. Look at the preview and set Yes (Y) or No (N), or decide its group or folder to settle many objects at once.")
                    End Select
            End Select
            Return sb.ToString()
        End Function

        Friend Function ExplainGroup(key As String) As String
            Dim g = _groups(key)
            Dim t = _groupTally(key)
            Dim d = Decision(ReviewTab.Groups, key)
            Dim sb As New StringBuilder
            Dim build = g.Kind = RecipeKind.Build
            sb.AppendLine($"Group {GroupName(g)}  —  {If(build, "build", "scrap")} recipe {g.EditorID} ({g.Key}), from {g.SourcePlugin}")
            If Not g.HasCreatedObject Then
                sb.AppendLine("It names no object to scrap, so it covers nothing.")
            ElseIf g.TargetKey = "" Then
                sb.AppendLine("The object it names is not loaded, so it covers nothing.")
            ElseIf build Then
                sb.AppendLine($"Builds: {g.TargetSignature} {g.TargetEditorID} ({g.TargetKey})" & If(g.TargetSignature = "FLST", " — a form list; the objects in it that can stand in a settlement", "") &
                              ". What the workshop can build, it can also scrap.")
            Else
                sb.AppendLine($"Scraps: {g.TargetSignature} {g.TargetEditorID} ({g.TargetKey})" & If(g.TargetSignature = "FLST", " — a form list, every object in it", ""))
            End If
            If g.Components.Count > 0 Then
                sb.AppendLine(If(build, "Costs (scrapping returns it, unless a scrap recipe also covers the object): ", "Returns: ") & CellText(ReviewTab.Groups, key, ColReturns))
            End If
            sb.AppendLine()
            sb.AppendLine($"Your decision on this group: {If(d.HasValue, VerdictText(d.Value), "none")}.")
            sb.AppendLine($"Its {t.Objects} objects: {t.Yes} Yes, {t.No} No, {t.Review} Review.")
            sb.AppendLine()
            sb.AppendLine("A group decision applies to every object of the group that has no decision of its own; when an object is in several groups and your decisions disagree, No wins. Double-click an object below to open it.")
            Return sb.ToString()
        End Function

        Friend Function ExplainFolder(key As String) As String
            Dim t = _folderTally(key)
            Dim d = Decision(ReviewTab.Folders, key)
            Dim rule = Rules.Match(key)
            Dim sb As New StringBuilder
            sb.AppendLine($"Folder {key}")
            If rule Is Nothing Then
                sb.AppendLine("Rule: none (Review).")
            Else
                Dim origin = Book.Origin(RuleKind.Folder, rule.Folder)
                Dim whose = If(origin = RuleOrigin.App, "the app's rule", "your rule")
                sb.AppendLine($"Rule: {VerdictText(rule.Decision)}{If(rule.Reason <> "", $" ({rule.Reason})", "")} — {whose} for {rule.Folder}{If(rule.Folder <> key, " (a parent folder)", "")}.")
            End If
            sb.AppendLine($"Your decision on this folder: {If(d.HasValue, VerdictText(d.Value), "none")}{If(d.HasValue, " (it replaces the rule)", "")}.")
            sb.AppendLine($"Its {t.Objects} objects: {t.Yes} Yes, {t.No} No, {t.Review} Review.")
            sb.AppendLine()
            sb.AppendLine("A folder decides the objects that have no object or group decision. To change it for every load order, edit the rule in Rules…; to decide just here, set Yes / No on this row. Double-click an object below to open it.")
            Return sb.ToString()
        End Function

    End Class

End Namespace
