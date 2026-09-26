Option Strict On
Option Infer On

Imports System.IO
Imports System.Text.Json
Imports System.Text.RegularExpressions

Namespace Engine

    ''' <summary>The three outcomes. <c>Review</c> = nobody has decided and the rules cannot tell.</summary>
    Friend Enum Verdict
        Review = 0
        Yes = 1
        No = 2
    End Enum

    ''' <summary>A rule keyed by text (a folder or a record type): its key, decision and reason. Also the snapshot a user
    ''' change keeps of the app's rule (<c>was</c>), where only the decision and the reason count.</summary>
    Friend Class KeyedRule
        Friend Property Key As String
        Friend Property Decision As Verdict
        Friend Property Reason As String = ""
    End Class

    ''' <summary>One folder rule: every object whose model folder is <see cref="Folder"/> or lies under it gets
    ''' <see cref="KeyedRule.Decision"/>, for <see cref="KeyedRule.Reason"/>.</summary>
    Friend NotInheritable Class FolderRule
        Inherits KeyedRule
        Friend Property Folder As String
            Get
                Return Key
            End Get
            Set(value As String)
                Key = value
            End Set
        End Property
    End Class

    ''' <summary>One type rule: the app's default for every object of a record type (<see cref="Type"/> = the 4-letter
    ''' signature), e.g. CONT = No because a container holds items (user's decision, 26-sep).</summary>
    Friend NotInheritable Class TypeRule
        Inherits KeyedRule
        Friend Property Type As String
            Get
                Return Key
            End Get
            Set(value As String)
                Key = value
            End Set
        End Property
    End Class

    ''' <summary>A warning category: words that, found in an object's EditorID, name a plant, terrain or a building part.</summary>
    Friend NotInheritable Class WarningCategory
        Friend Property Name As String
        Friend Property Words As HashSet(Of String)
    End Class

    ''' <summary>The rule data: folder rules (<c>folders.json</c>) and warning words (<c>warnings.json</c>).
    ''' <para>Folder law (prototype <c>carpetas_default.py</c> <c>default</c>): a rule matches a folder when the folder IS its
    ''' key or starts with key + <c>\</c> (component boundary, Ordinal); the LONGEST matching key wins; no match ⇒ Review.
    ''' Keys are unique — a repeated key is a load error (user's decision, 26-sep), so two keys can never tie.</para>
    ''' <para>Warning law (prototype <c>grupos.py</c> <c>warning</c>): the EditorID is split into words at a lower→upper
    ''' case change and at every non-letter; each category whose word set meets the object's words is reported as
    ''' <c>category (w1/w2)</c>, words sorted Ordinal, categories in file order, joined with <c>, </c>.</para></summary>
    Friend NotInheritable Class RuleSet

        Friend ReadOnly Property Folders As IReadOnlyList(Of FolderRule)
        Friend ReadOnly Property Warnings As IReadOnlyList(Of WarningCategory)
        ''' <summary>The type rules (<c>types.json</c>), one per record type.</summary>
        Friend ReadOnly Property Types As IReadOnlyList(Of TypeRule)

        Friend Sub New(folders As IEnumerable(Of FolderRule), warnings As IEnumerable(Of WarningCategory), Optional types As IEnumerable(Of TypeRule) = Nothing)
            Dim fl = folders.ToList()
            For Each r In fl
                If r.Folder <> NormalizeFolder(r.Folder) Then
                    Throw New RuleDataException($"The folder rule '{r.Folder}' is not written the way model folders are (it would be '{NormalizeFolder(r.Folder)}'), so it could never match.")
                End If
                Dim problem = FolderKeyProblem(r.Folder)
                If problem IsNot Nothing Then Throw New RuleDataException($"Folder rule: {problem}.")
            Next
            Dim dup = fl.GroupBy(Function(r) r.Folder, StringComparer.Ordinal).FirstOrDefault(Function(g) g.Count() > 1)
            If dup IsNot Nothing Then
                Throw New RuleDataException($"The folder rule '{dup.Key}' appears {dup.Count()} times. Each folder can have only one rule.")
            End If
            Dim wl = warnings.ToList()
            For Each c In wl
                Dim bad = c.Words.FirstOrDefault(Function(w) Not IsValidWord(w))
                If bad IsNot Nothing Then
                    Throw New RuleDataException($"The warning word '{bad}' ({c.Name}) has characters other than letters, so it could never match.")
                End If
            Next
            Dim tl = If(types, Array.Empty(Of TypeRule)()).ToList()
            For Each t In tl
                Dim problem = TypeKeyProblem(t.Type)
                If problem IsNot Nothing Then Throw New RuleDataException($"Type rule: {problem}.")
            Next
            Dim dupT = tl.GroupBy(Function(t) t.Type, StringComparer.Ordinal).FirstOrDefault(Function(g) g.Count() > 1)
            If dupT IsNot Nothing Then
                Throw New RuleDataException($"The type rule '{dupT.Key}' appears {dupT.Count()} times. Each type can have only one rule.")
            End If
            _folders = fl
            _warnings = wl
            _types = tl
        End Sub

        ''' <summary>The type rule of a record signature, or Nothing.</summary>
        Friend Function TypeRuleOf(signature As String) As TypeRule
            Return Types.FirstOrDefault(Function(t) t.Type = signature)
        End Function

        Private Shared ReadOnly SignatureKey As New Regex("^[A-Z0-9_]{4}$", RegexOptions.CultureInvariant)

        ''' <summary>Why a type key can never match a record, or Nothing: a record signature is four characters, capitals,
        ''' digits or <c>_</c> (e.g. CONT, NPC_).</summary>
        Friend Shared Function TypeKeyProblem(key As String) As String
            If String.IsNullOrEmpty(key) Then Return "the type is empty"
            If Not SignatureKey.IsMatch(key) Then Return $"'{key}' is not a record type (four capitals, digits or _, e.g. CONT)"
            Return Nothing
        End Function

        ''' <summary>The rule that decides <paramref name="folder"/>, or Nothing.</summary>
        Friend Function Match(folder As String) As FolderRule
            Return BestMatch(Folders, folder)
        End Function

        ''' <summary>The folder law over any rule list (the rules editor calls it on rows that are not saved yet): the
        ''' longest key that is the folder or a parent of it. On a repeated key the first one wins (the editor flags
        ''' repeats; a <see cref="RuleSet"/> never has them).</summary>
        Friend Shared Function BestMatch(rules As IEnumerable(Of FolderRule), folder As String) As FolderRule
            Dim best As FolderRule = Nothing
            For Each r In rules
                If folder = r.Folder OrElse folder.StartsWith(r.Folder & "\", StringComparison.Ordinal) Then
                    If best Is Nothing OrElse r.Folder.Length > best.Folder.Length Then best = r
                End If
            Next
            Return best
        End Function

        ''' <summary>How many objects each rule decides: for every object, the rules that win for its folders (an object
        ''' whose parts sit in two folders of the same rule counts once for it). Rules that decide nothing are present
        ''' with 0.</summary>
        Friend Shared Function MatchCounts(rules As IReadOnlyList(Of FolderRule), objectFolders As IEnumerable(Of IEnumerable(Of String))) As Dictionary(Of FolderRule, Integer)
            Dim counts = rules.Distinct().ToDictionary(Function(r) r, Function(r) 0)
            Dim winner As New Dictionary(Of String, FolderRule)(StringComparer.Ordinal)
            For Each objFolders In objectFolders
                Dim hit As New HashSet(Of FolderRule)
                For Each f In objFolders
                    Dim r As FolderRule = Nothing
                    If Not winner.TryGetValue(f, r) Then
                        r = BestMatch(rules, f)
                        winner(f) = r
                    End If
                    If r IsNot Nothing Then hit.Add(r)
                Next
                For Each r In hit
                    counts(r) += 1
                Next
            Next
            Return counts
        End Function

        ''' <summary>A folder key written the way <see cref="ModelFolder"/> writes model folders: <c>/</c> to <c>\</c>, trimmed,
        ''' lower case, a leading <c>meshes\</c> and then a leading <c>dlcNN\</c> dropped, no leading or trailing <c>\</c>. The
        ''' two-level cut is NOT applied here: a deeper key is reported by <see cref="FolderKeyProblem"/> instead of being cut
        ''' silently into another folder's key. The synthetic keys (<c>(root)</c>, <c>(no model: SIG)</c>, ...) start with
        ''' <c>(</c> and are kept as they are (the signature in them is upper case).</summary>
        Friend Shared Function NormalizeFolder(text As String) As String
            Dim t = If(text, "").Trim()
            If t.StartsWith("("c) Then Return t
            Dim parts = New List(Of String)(t.Replace("/"c, "\"c).ToLowerInvariant().Trim("\"c).Split("\"c))
            If parts.Count > 0 AndAlso parts(0) = "meshes" Then parts.RemoveAt(0)
            If parts.Count > 0 AndAlso ModelFolder.IsDlcFolder(parts(0)) Then parts.RemoveAt(0)
            Return String.Join("\", parts).Trim("\"c)
        End Function

        Private Shared ReadOnly NoModelKey As New Regex("^\(no model: [A-Z0-9_]{4}\)$", RegexOptions.CultureInvariant)

        ''' <summary>Why a (normalized) folder key can never match an object, or Nothing when it can. Object folders are at
        ''' most two levels (<see cref="ModelFolder.FromModelPath"/> keeps the first two), so a deeper key never matches; the
        ''' synthetic keys must be written exactly as <see cref="ModelFolder"/> writes them.</summary>
        Friend Shared Function FolderKeyProblem(key As String) As String
            If String.IsNullOrEmpty(key) Then Return "the folder is empty"
            If key.StartsWith("("c) Then
                If key = ModelFolder.RootFolder OrElse key = ModelFolder.ScolWithoutParts OrElse NoModelKey.IsMatch(key) Then Return Nothing
                Return $"'{key}' is not one of the special folders: {ModelFolder.RootFolder}, {ModelFolder.ScolWithoutParts}, (no model: SIG) with SIG the 4-letter record type in capitals"
            End If
            Dim parts = key.Split("\"c)
            If parts.Any(Function(p) p = "") Then Return $"'{key}' has an empty folder name"
            If parts.Length > 2 Then Return $"'{key}' has more than two folder levels; objects are filed by their first two (after Meshes\ and DLCnn\), so it could never match"
            Return Nothing
        End Function

        ''' <summary>A warning word can only match if it is lower-case letters: the warning law splits the EditorID at every
        ''' non-letter and lower-cases the pieces (<see cref="Warning"/>).</summary>
        Friend Shared Function IsValidWord(word As String) As Boolean
            Return Not String.IsNullOrEmpty(word) AndAlso word.All(Function(ch) ch >= "a"c AndAlso ch <= "z"c)
        End Function

        Private Shared ReadOnly CaseBreak As New Regex("([a-z])([A-Z])", RegexOptions.CultureInvariant)
        Private Shared ReadOnly NonLetters As New Regex("[^A-Za-z]+", RegexOptions.CultureInvariant)

        Friend Function Warning(editorId As String) As String
            Dim spaced = CaseBreak.Replace(editorId, "$1 $2")
            Dim words As New HashSet(Of String)(StringComparer.Ordinal)
            For Each t In NonLetters.Split(spaced)
                If t <> "" Then words.Add(t.ToLowerInvariant())
            Next
            Dim parts As New List(Of String)
            For Each c In Warnings
                Dim hit = c.Words.Where(Function(w) words.Contains(w)).OrderBy(Function(w) w, StringComparer.Ordinal).ToList()
                If hit.Count > 0 Then parts.Add($"{c.Name} ({String.Join("/", hit)})")
            Next
            Return String.Join(", ", parts)
        End Function

        ' ---------------------------------------------------------------- JSON

        ''' <summary>The rules the app ships (<c>folders.json</c>, <c>warnings.json</c> in <paramref name="shippedDir"/>),
        ''' without the user's changes (those are applied by <see cref="RuleBook"/>).</summary>
        Friend Shared Function LoadShipped(shippedDir As String) As RuleSet
            ' types.json is optional: without it there are no type rules (the prototype's law, which the parity gate runs).
            Dim typesPath = Path.Combine(shippedDir, "types.json")
            Return New RuleSet(ReadFolders(Shipped(shippedDir, "folders.json")), ReadWarnings(Shipped(shippedDir, "warnings.json")),
                               If(File.Exists(typesPath), ReadTypes(typesPath), Nothing))
        End Function

        Private Shared Function Shipped(shippedDir As String, name As String) As String
            Dim p = Path.Combine(shippedDir, name)
            If Not File.Exists(p) Then Throw New RuleDataException($"Rule file not found: {p}")
            Return p
        End Function

        Private Shared ReadOnly JsonRead As New JsonDocumentOptions With {.CommentHandling = JsonCommentHandling.Skip, .AllowTrailingCommas = True}

        ''' <summary>Runs a JSON reader and turns malformed JSON or a value of the wrong kind into a
        ''' <see cref="RuleDataException"/> that names the file (these files are edited by hand).</summary>
        Friend Shared Function ReadJson(Of T)(path As String, reader As Func(Of JsonDocument, T)) As T
            Try
                Using doc = JsonDocument.Parse(File.ReadAllText(path), JsonRead)
                    Return reader(doc)
                End Using
            Catch ex As JsonException
                Throw New RuleDataException($"{path} is not valid JSON: {ex.Message}")
            Catch ex As InvalidOperationException
                Throw New RuleDataException($"{path}: a value has the wrong kind ({ex.Message})")
            End Try
        End Function

        Friend Shared Function ReadFolders(path As String) As List(Of FolderRule)
            Return ReadJson(path, Function(doc)
                Dim out As New List(Of FolderRule)
                For Each e In Required(doc.RootElement, "rules", path).EnumerateArray()
                    Dim reason As JsonElement
                    out.Add(New FolderRule With {
                        .Folder = RequiredString(e, "folder", path),
                        .Decision = ParseVerdict(RequiredString(e, "decision", path), path),
                        .Reason = If(e.TryGetProperty("reason", reason) AndAlso reason.ValueKind = JsonValueKind.String, reason.GetString(), "")})
                Next
                Return out
            End Function)
        End Function

        Friend Shared Function ReadTypes(path As String) As List(Of TypeRule)
            Return ReadJson(path, Function(doc)
                Dim out As New List(Of TypeRule)
                For Each e In Required(doc.RootElement, "rules", path).EnumerateArray()
                    Dim reason As JsonElement
                    out.Add(New TypeRule With {
                        .Type = RequiredString(e, "type", path),
                        .Decision = ParseVerdict(RequiredString(e, "decision", path), path),
                        .Reason = If(e.TryGetProperty("reason", reason) AndAlso reason.ValueKind = JsonValueKind.String, reason.GetString(), "")})
                Next
                Return out
            End Function)
        End Function

        Friend Shared Function ReadWarnings(path As String) As List(Of WarningCategory)
            Return ReadJson(path, Function(doc)
                Dim out As New List(Of WarningCategory)
                For Each e In Required(doc.RootElement, "categories", path).EnumerateArray()
                    Dim words As New HashSet(Of String)(StringComparer.Ordinal)
                    For Each w In Required(e, "words", path).EnumerateArray()
                        words.Add(w.GetString().ToLowerInvariant())
                    Next
                    out.Add(New WarningCategory With {.Name = RequiredString(e, "name", path), .Words = words})
                Next
                Return out
            End Function)
        End Function

        ''' <summary>The file spelling of a decision (the inverse of <see cref="ParseVerdict"/>).</summary>
        Friend Shared Function VerdictKey(v As Verdict) As String
            Select Case v
                Case Verdict.Yes : Return "yes"
                Case Verdict.No : Return "no"
                Case Else : Return "review"
            End Select
        End Function

        Friend Shared Function ParseVerdict(text As String, source As String) As Verdict
            Select Case text
                Case "yes" : Return Verdict.Yes
                Case "no" : Return Verdict.No
                Case "review" : Return Verdict.Review
            End Select
            Throw New RuleDataException($"'{text}' is not a decision (yes, no, review) in {source}")
        End Function

        Private Shared Function Required(e As JsonElement, name As String, source As String) As JsonElement
            Dim v As JsonElement
            If e.ValueKind <> JsonValueKind.Object OrElse Not e.TryGetProperty(name, v) Then
                Throw New RuleDataException($"Missing '{name}' in {source}")
            End If
            Return v
        End Function

        Private Shared Function RequiredString(e As JsonElement, name As String, source As String) As String
            Dim v = Required(e, name, source)
            If v.ValueKind <> JsonValueKind.String OrElse String.IsNullOrEmpty(v.GetString()) Then
                Throw New RuleDataException($"'{name}' must be a non-empty text in {source}")
            End If
            Return v.GetString()
        End Function

    End Class

    ''' <summary>Rule data that cannot be used as it is (missing file, repeated key, unknown value). The message names
    ''' the problem so the user can fix it.</summary>
    Friend Class RuleDataException
        Inherits Exception
        Friend Sub New(message As String)
            MyBase.New(message)
        End Sub
    End Class

End Namespace
