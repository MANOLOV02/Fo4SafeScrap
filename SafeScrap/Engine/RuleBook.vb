Option Strict On
Option Infer On

Imports System.IO
Imports System.Text.Json

Namespace Engine

    ''' <summary>The kinds of keyed rules the user can change (each one a section of the difference).</summary>
    Friend Enum RuleKind
        Folder = 0
        Type = 1
    End Enum

    ''' <summary>A rule the user added or changed. <see cref="Was"/> = the app's rule for that key when the user made the
    ''' change (Nothing if the app had none): it is how a later change of the app's rule is noticed.</summary>
    Friend NotInheritable Class RuleChange
        Friend Property Key As String
        Friend Property Decision As Verdict
        Friend Property Reason As String = ""
        Friend Property Was As KeyedRule
    End Class

    ''' <summary>An app rule the user removed; <see cref="Was"/> = that rule when it was removed.</summary>
    Friend NotInheritable Class RuleRemoval
        Friend Property Key As String
        Friend Property Was As KeyedRule
    End Class

    ''' <summary>The user's changes to one kind of keyed rule.</summary>
    Friend NotInheritable Class RuleSection
        Friend ReadOnly Property SetRules As New List(Of RuleChange)
        Friend ReadOnly Property Removed As New List(Of RuleRemoval)

        Friend ReadOnly Property IsEmpty As Boolean
            Get
                Return SetRules.Count = 0 AndAlso Removed.Count = 0
            End Get
        End Property

        Friend Function Clone() As RuleSection
            Dim c As New RuleSection
            For Each x In SetRules
                c.SetRules.Add(New RuleChange With {.Key = x.Key, .Decision = x.Decision, .Reason = x.Reason, .Was = RuleBook.Snapshot(x.Was)})
            Next
            For Each r In Removed
                c.Removed.Add(New RuleRemoval With {.Key = r.Key, .Was = RuleBook.Snapshot(r.Was)})
            Next
            Return c
        End Function
    End Class

    ''' <summary>The warning words the user added to / removed from one of the app's categories.</summary>
    Friend NotInheritable Class WordChange
        Friend Property Category As String
        Friend Property Added As New SortedSet(Of String)(StringComparer.Ordinal)
        Friend Property Removed As New SortedSet(Of String)(StringComparer.Ordinal)
    End Class

    ''' <summary>The user's rule changes, kept as a DIFFERENCE over the app's rules (user's decision, 26-sep) so a newer
    ''' version of the app's rules still reaches the user: <c>UserData\Rules\folders.user.json</c> and <c>types.user.json</c>
    ''' (<c>set</c>, <c>removed</c>) and <c>warnings.user.json</c> (words <c>added</c> / <c>removed</c> per category).</summary>
    Friend NotInheritable Class RuleDelta

        Friend Const FoldersFile As String = "folders.user.json"
        Friend Const TypesFile As String = "types.user.json"
        Friend Const WarningsFile As String = "warnings.user.json"

        Friend ReadOnly Property Folders As New RuleSection
        Friend ReadOnly Property Types As New RuleSection
        Friend ReadOnly Property Words As New List(Of WordChange)

        Friend Function Section(kind As RuleKind) As RuleSection
            Return If(kind = RuleKind.Type, Types, Folders)
        End Function

        Friend Shared ReadOnly Property Files As String()
            Get
                Return {FoldersFile, TypesFile, WarningsFile}
            End Get
        End Property

        ''' <summary>The file of a kind and the JSON name of its key.</summary>
        Private Shared Function FileOf(kind As RuleKind) As (File As String, KeyName As String)
            Return If(kind = RuleKind.Type, (TypesFile, "type"), (FoldersFile, "folder"))
        End Function

        Friend ReadOnly Property IsEmpty As Boolean
            Get
                Return Folders.IsEmpty AndAlso Types.IsEmpty AndAlso Words.All(Function(w) w.Added.Count = 0 AndAlso w.Removed.Count = 0)
            End Get
        End Property

        ''' <summary>Reads the user's difference from <paramref name="userDir"/>; missing files = no changes.</summary>
        Friend Shared Function Load(userDir As String) As RuleDelta
            Dim d As New RuleDelta
            For Each kind In {RuleKind.Folder, RuleKind.Type}
                ReadSection(Path.Combine(userDir, FileOf(kind).File), FileOf(kind).KeyName, d.Section(kind))
            Next
            Dim wp = Path.Combine(userDir, WarningsFile)
            If File.Exists(wp) Then
                RuleSet.ReadJson(wp, Function(doc)
                                         Dim section As JsonElement
                                         If doc.RootElement.TryGetProperty("categories", section) Then
                                             For Each e In section.EnumerateArray()
                                                 Dim w As New WordChange With {.Category = Text(e, "name", wp)}
                                                 ReadWords(e, "added", w.Added)
                                                 ReadWords(e, "removed", w.Removed)
                                                 d.Words.Add(w)
                                             Next
                                         End If
                                         Return True
                                     End Function)
            End If
            Return d
        End Function

        Private Shared Sub ReadSection(fp As String, keyName As String, target As RuleSection)
            If Not File.Exists(fp) Then Return
            RuleSet.ReadJson(fp, Function(doc)
                                     Dim root = doc.RootElement
                                     Dim section As JsonElement
                                     If root.TryGetProperty("set", section) Then
                                         For Each e In section.EnumerateArray()
                                             target.SetRules.Add(New RuleChange With {
                                                 .Key = Text(e, keyName, fp),
                                                 .Decision = RuleSet.ParseVerdict(Text(e, "decision", fp), fp),
                                                 .Reason = OptionalText(e, "reason"),
                                                 .Was = ReadWas(e, fp)})
                                         Next
                                     End If
                                     If root.TryGetProperty("removed", section) Then
                                         For Each e In section.EnumerateArray()
                                             Dim key = Text(e, keyName, fp)
                                             Dim was = ReadWas(e, fp)
                                             If was Is Nothing Then Throw New RuleDataException($"{fp}: the removed rule '{key}' has no 'was'.")
                                             target.Removed.Add(New RuleRemoval With {.Key = key, .Was = was})
                                         Next
                                     End If
                                     Return True
                                 End Function)
        End Sub

        Private Shared Function ReadWas(e As JsonElement, source As String) As KeyedRule
            Dim w As JsonElement
            If Not e.TryGetProperty("was", w) OrElse w.ValueKind = JsonValueKind.Null Then Return Nothing
            Return New KeyedRule With {.Decision = RuleSet.ParseVerdict(Text(w, "decision", source), source), .Reason = OptionalText(w, "reason")}
        End Function

        Private Shared Sub ReadWords(e As JsonElement, name As String, target As SortedSet(Of String))
            Dim a As JsonElement
            If Not e.TryGetProperty(name, a) Then Return
            For Each w In a.EnumerateArray()
                target.Add(w.GetString().ToLowerInvariant())
            Next
        End Sub

        Private Shared Function Text(e As JsonElement, name As String, source As String) As String
            Dim v As JsonElement
            If e.ValueKind <> JsonValueKind.Object OrElse Not e.TryGetProperty(name, v) OrElse v.ValueKind <> JsonValueKind.String OrElse String.IsNullOrEmpty(v.GetString()) Then
                Throw New RuleDataException($"'{name}' must be a non-empty text in {source}")
            End If
            Return v.GetString()
        End Function

        Private Shared Function OptionalText(e As JsonElement, name As String) As String
            Dim v As JsonElement
            Return If(e.TryGetProperty(name, v) AndAlso v.ValueKind = JsonValueKind.String, v.GetString(), "")
        End Function

        ''' <summary>Writes the three files (sorted, Ordinal) with the workspace's in-place write.</summary>
        Friend Sub Save(userDir As String)
            Directory.CreateDirectory(userDir)
            For Each kind In {RuleKind.Folder, RuleKind.Type}
                WriteSection(Path.Combine(userDir, FileOf(kind).File), FileOf(kind).KeyName, Section(kind))
            Next
            WriteJson(Path.Combine(userDir, WarningsFile),
                Sub(w)
                    w.WriteStartArray("categories")
                    For Each c In Words.Where(Function(x) x.Added.Count > 0 OrElse x.Removed.Count > 0)
                        w.WriteStartObject()
                        w.WriteString("name", c.Category)
                        w.WriteStartArray("added")
                        For Each x In c.Added
                            w.WriteStringValue(x)
                        Next
                        w.WriteEndArray()
                        w.WriteStartArray("removed")
                        For Each x In c.Removed
                            w.WriteStringValue(x)
                        Next
                        w.WriteEndArray()
                        w.WriteEndObject()
                    Next
                    w.WriteEndArray()
                End Sub)
        End Sub

        Private Shared Sub WriteSection(fp As String, keyName As String, sec As RuleSection)
            WriteJson(fp,
                Sub(w)
                    w.WriteStartArray("set")
                    For Each c In sec.SetRules.OrderBy(Function(x) x.Key, StringComparer.Ordinal)
                        w.WriteStartObject()
                        w.WriteString(keyName, c.Key)
                        w.WriteString("decision", RuleSet.VerdictKey(c.Decision))
                        w.WriteString("reason", c.Reason)
                        WriteWas(w, c.Was)
                        w.WriteEndObject()
                    Next
                    w.WriteEndArray()
                    w.WriteStartArray("removed")
                    For Each r In sec.Removed.OrderBy(Function(x) x.Key, StringComparer.Ordinal)
                        w.WriteStartObject()
                        w.WriteString(keyName, r.Key)
                        WriteWas(w, r.Was)
                        w.WriteEndObject()
                    Next
                    w.WriteEndArray()
                End Sub)
        End Sub

        Private Shared Sub WriteWas(w As Utf8JsonWriter, was As KeyedRule)
            If was Is Nothing Then
                w.WriteNull("was")
                Return
            End If
            w.WriteStartObject("was")
            w.WriteString("decision", RuleSet.VerdictKey(was.Decision))
            w.WriteString("reason", was.Reason)
            w.WriteEndObject()
        End Sub

        Private Shared Sub WriteJson(path As String, body As Action(Of Utf8JsonWriter))
            Dim bytes As Byte()
            Using ms As New MemoryStream()
                Using w As New Utf8JsonWriter(ms, New JsonWriterOptions With {.Indented = True})
                    w.WriteStartObject()
                    w.WriteNumber("format", 1)
                    body(w)
                    w.WriteEndObject()
                End Using
                bytes = ms.ToArray()
            End Using
            BSA_BA2_Library_DLL.EscrituraEnElLugar.Escribir(path, Sub(fs) fs.Write(bytes, 0, bytes.Length))
        End Sub

    End Class

    ''' <summary>Where an effective rule comes from.</summary>
    Friend Enum RuleOrigin
        App = 0
        ''' <summary>The user changed the app's rule for this key.</summary>
        UserChanged = 1
        ''' <summary>The app has no rule for this key; the user added it.</summary>
        UserAdded = 2
    End Enum

    ''' <summary>A user change whose <c>was</c> no longer matches the app's current rule: the app changed (or dropped, or
    ''' added) that rule after the user's edit. The user's version keeps applying until the user resolves it
    ''' (<see cref="RuleBook.KeepMine"/> / <see cref="RuleBook.UseApps"/>).</summary>
    Friend NotInheritable Class RuleConflict
        Friend Property Kind As RuleKind
        Friend Property Key As String
        ''' <summary>True for a removal, False for an added/changed rule.</summary>
        Friend Property IsRemoval As Boolean
        Friend Property Was As KeyedRule
        Friend Property AppNow As KeyedRule
    End Class

    ''' <summary>The rules in force: the app's rules plus the user's <see cref="RuleDelta"/>.
    ''' <para>Law, per kind of keyed rule (folders, types): start from the app's rules in file order; a removal drops that
    ''' key's rule; a change replaces the rule in its place, or is appended when the app has none. Warning words: per APP
    ''' category, its words minus the removed plus the added. A word change for a category the app no longer has is
    ''' ignored and reported in <see cref="Notes"/>. A key that is both changed and removed is a load error.</para></summary>
    Friend NotInheritable Class RuleBook

        Friend ReadOnly Property App As RuleSet
        Friend ReadOnly Property Delta As RuleDelta
        Friend ReadOnly Property Effective As RuleSet
        Friend ReadOnly Property Conflicts As New List(Of RuleConflict)
        Friend ReadOnly Property Notes As New List(Of String)

        Private Sub New(app As RuleSet, delta As RuleDelta)
            Me.App = app
            Me.Delta = delta
            Dim folders = ComposeSection(RuleKind.Folder, app.Folders, delta.Folders)
            Dim types = ComposeSection(RuleKind.Type, app.Types, delta.Types)

            Dim warnings As New List(Of WarningCategory)
            For Each cat In app.Warnings
                Dim words As New HashSet(Of String)(cat.Words, StringComparer.Ordinal)
                Dim change = delta.Words.FirstOrDefault(Function(w) w.Category = cat.Name)
                If change IsNot Nothing Then
                    words.ExceptWith(change.Removed)
                    words.UnionWith(change.Added)
                End If
                warnings.Add(New WarningCategory With {.Name = cat.Name, .Words = words})
            Next
            For Each w In delta.Words.Where(Function(x) Not app.Warnings.Any(Function(c) c.Name = x.Category))
                Notes.Add($"Your warning words for '{w.Category}' are ignored: the app no longer has that category.")
            Next
            Effective = New RuleSet(folders, warnings, types)
        End Sub

        ''' <summary>The composition law for one kind (see the class).</summary>
        Private Function ComposeSection(Of T As {KeyedRule, New})(kind As RuleKind, appRules As IReadOnlyList(Of T), sec As RuleSection) As List(Of T)
            Dim both = sec.SetRules.Select(Function(c) c.Key).Intersect(sec.Removed.Select(Function(r) r.Key), StringComparer.Ordinal).FirstOrDefault()
            If both IsNot Nothing Then Throw New RuleDataException($"The {KindName(kind)} rule '{both}' is both changed and removed.")
            Dim byKey = appRules.ToDictionary(Function(r) r.Key, Function(r) CType(r, KeyedRule), StringComparer.Ordinal)
            Dim rules = appRules.ToList()
            For Each r In sec.Removed
                Dim appNow As KeyedRule = Nothing
                byKey.TryGetValue(r.Key, appNow)
                If Not SameRule(r.Was, appNow) Then Conflicts.Add(New RuleConflict With {.Kind = kind, .Key = r.Key, .IsRemoval = True, .Was = r.Was, .AppNow = appNow})
                rules.RemoveAll(Function(x) x.Key = r.Key)
            Next
            For Each c In sec.SetRules
                Dim appNow As KeyedRule = Nothing
                byKey.TryGetValue(c.Key, appNow)
                If Not SameRule(c.Was, appNow) Then Conflicts.Add(New RuleConflict With {.Kind = kind, .Key = c.Key, .IsRemoval = False, .Was = c.Was, .AppNow = appNow})
                Dim mine As New T With {.Key = c.Key, .Decision = c.Decision, .Reason = c.Reason}
                Dim i = rules.FindIndex(Function(x) x.Key = c.Key)
                If i >= 0 Then rules(i) = mine Else rules.Add(mine)
            Next
            Return rules
        End Function

        Friend Shared Function KindName(kind As RuleKind) As String
            Return If(kind = RuleKind.Type, "type", "folder")
        End Function

        Friend Shared Function Compose(app As RuleSet, delta As RuleDelta) As RuleBook
            Return New RuleBook(app, delta)
        End Function

        ''' <summary>The app's rules plus the user's difference. A difference that cannot be used (hand-edited, or written
        ''' by an older version) raises <see cref="UserRulesException"/>, so the caller can offer to set it aside; a problem
        ''' in the app's own files stays a <see cref="RuleDataException"/>.</summary>
        Friend Shared Function Load(shippedDir As String, userDir As String) As RuleBook
            Dim app = RuleSet.LoadShipped(shippedDir)
            Try
                Return New RuleBook(app, RuleDelta.Load(userDir))
            Catch ex As RuleDataException
                Throw New UserRulesException(userDir, ex.Message)
            End Try
        End Function

        ''' <summary>Same decision and reason (both Nothing counts as the same).</summary>
        Friend Shared Function SameRule(a As KeyedRule, b As KeyedRule) As Boolean
            If a Is Nothing OrElse b Is Nothing Then Return a Is Nothing AndAlso b Is Nothing
            Return a.Decision = b.Decision AndAlso String.Equals(a.Reason, b.Reason, StringComparison.Ordinal)
        End Function

        ''' <summary>A copy that keeps the decision and the reason (the <c>was</c> snapshot).</summary>
        Friend Shared Function Snapshot(r As KeyedRule) As KeyedRule
            If r Is Nothing Then Return Nothing
            Return New KeyedRule With {.Key = r.Key, .Decision = r.Decision, .Reason = r.Reason}
        End Function

        Private Function AppRules(kind As RuleKind) As IEnumerable(Of KeyedRule)
            Return If(kind = RuleKind.Type, App.Types.Cast(Of KeyedRule)(), App.Folders.Cast(Of KeyedRule)())
        End Function

        Friend Function AppRule(kind As RuleKind, key As String) As KeyedRule
            Return AppRules(kind).FirstOrDefault(Function(r) r.Key = key)
        End Function

        Friend Function Origin(kind As RuleKind, key As String) As RuleOrigin
            If Not Delta.Section(kind).SetRules.Any(Function(c) c.Key = key) Then Return RuleOrigin.App
            Return If(AppRule(kind, key) IsNot Nothing, RuleOrigin.UserChanged, RuleOrigin.UserAdded)
        End Function

        ''' <summary>The difference that turns the app's rules into the editor's tables (already normalized, without
        ''' repeats): folder rows, type rows and <paramref name="words"/> (category → words). A change the user had
        ''' already made keeps its original <c>was</c> (so an unresolved conflict stays one); a new change records the app's
        ''' current rule.</summary>
        Friend Function DeltaFrom(folderRows As IEnumerable(Of KeyedRule), typeRows As IEnumerable(Of KeyedRule), words As IDictionary(Of String, IEnumerable(Of String))) As RuleDelta
            Dim d As New RuleDelta
            SectionFrom(RuleKind.Folder, folderRows, d.Folders)
            SectionFrom(RuleKind.Type, typeRows, d.Types)
            For Each cat In App.Warnings
                Dim mine As IEnumerable(Of String) = Nothing
                If Not words.TryGetValue(cat.Name, mine) Then mine = Array.Empty(Of String)()
                Dim set1 As New HashSet(Of String)(mine.Select(Function(x) x.ToLowerInvariant()), StringComparer.Ordinal)
                Dim w As New WordChange With {.Category = cat.Name}
                w.Added.UnionWith(set1.Where(Function(x) Not cat.Words.Contains(x)))
                w.Removed.UnionWith(cat.Words.Where(Function(x) Not set1.Contains(x)))
                If w.Added.Count > 0 OrElse w.Removed.Count > 0 Then d.Words.Add(w)
            Next
            Return d
        End Function

        Private Sub SectionFrom(kind As RuleKind, rows As IEnumerable(Of KeyedRule), target As RuleSection)
            Dim old = Delta.Section(kind)
            Dim rowList = rows.ToList()
            For Each r In rowList
                Dim appR = AppRule(kind, r.Key)
                If appR IsNot Nothing AndAlso SameRule(appR, r) Then Continue For
                Dim prev = old.SetRules.FirstOrDefault(Function(c) c.Key = r.Key)
                target.SetRules.Add(New RuleChange With {.Key = r.Key, .Decision = r.Decision, .Reason = If(r.Reason, ""),
                                                         .Was = If(prev IsNot Nothing, prev.Was, Snapshot(appR))})
            Next
            For Each appR In AppRules(kind)
                If rowList.Any(Function(r) r.Key = appR.Key) Then Continue For
                Dim prev = old.Removed.FirstOrDefault(Function(x) x.Key = appR.Key)
                target.Removed.Add(New RuleRemoval With {.Key = appR.Key, .Was = If(prev IsNot Nothing, prev.Was, Snapshot(appR))})
            Next
            ' A removal of a rule the app has since dropped stays (it is a conflict to resolve, not a no-op to lose).
            For Each prev In old.Removed
                If AppRule(kind, prev.Key) Is Nothing AndAlso Not rowList.Any(Function(r) r.Key = prev.Key) Then
                    target.Removed.Add(New RuleRemoval With {.Key = prev.Key, .Was = prev.Was})
                End If
            Next
        End Sub

        ''' <summary>Resolves a conflict keeping the user's version: its <c>was</c> becomes the app's current rule.</summary>
        Friend Function KeepMine(kind As RuleKind, key As String) As RuleDelta
            Dim d = CloneDelta()
            Dim sec = d.Section(kind)
            Dim appR = AppRule(kind, key)
            For Each c In sec.SetRules.Where(Function(x) x.Key = key)
                c.Was = Snapshot(appR)
            Next
            If appR Is Nothing Then
                sec.Removed.RemoveAll(Function(x) x.Key = key)   ' the app dropped the rule the user had removed: nothing left to remove
            Else
                For Each r In sec.Removed.Where(Function(x) x.Key = key)
                    r.Was = Snapshot(appR)
                Next
            End If
            Return d
        End Function

        ''' <summary>Resolves a conflict taking the app's rule: the user's change for that key is dropped.</summary>
        Friend Function UseApps(kind As RuleKind, key As String) As RuleDelta
            Dim d = CloneDelta()
            d.Section(kind).SetRules.RemoveAll(Function(x) x.Key = key)
            d.Section(kind).Removed.RemoveAll(Function(x) x.Key = key)
            Return d
        End Function

        Private Function CloneDelta() As RuleDelta
            Dim d As New RuleDelta
            For Each kind In {RuleKind.Folder, RuleKind.Type}
                Dim c = Delta.Section(kind).Clone()
                d.Section(kind).SetRules.AddRange(c.SetRules)
                d.Section(kind).Removed.AddRange(c.Removed)
            Next
            For Each w In Delta.Words
                Dim c As New WordChange With {.Category = w.Category}
                c.Added.UnionWith(w.Added)
                c.Removed.UnionWith(w.Removed)
                d.Words.Add(c)
            Next
            Return d
        End Function

        ''' <summary>Restore defaults: the user's difference files are moved into <paramref name="backupDir"/> (a new
        ''' time-stamped folder); returns that folder, or Nothing when there was nothing to back up.</summary>
        Friend Shared Function RestoreDefaults(userDir As String, backupDir As String) As String
            Dim files = RuleDelta.Files.Select(Function(n) Path.Combine(userDir, n)).Where(AddressOf File.Exists).ToList()
            If files.Count = 0 Then Return Nothing
            Dim target = Path.Combine(backupDir, "Rules-" & DateTime.Now.ToString("yyyyMMdd-HHmmss", Globalization.CultureInfo.InvariantCulture))
            Directory.CreateDirectory(target)
            For Each f In files
                File.Move(f, Path.Combine(target, Path.GetFileName(f)), overwrite:=True)
            Next
            Return target
        End Function

    End Class

    ''' <summary>The user's rule changes (<c>*.user.json</c>) cannot be used. The message names the folder and the files.</summary>
    Friend NotInheritable Class UserRulesException
        Inherits RuleDataException
        Friend ReadOnly Property UserDir As String
        Friend Sub New(userDir As String, problem As String)
            MyBase.New($"Your rule changes ({String.Join(", ", RuleDelta.Files)} in {userDir}) cannot be used: {problem}")
            Me.UserDir = userDir
        End Sub
    End Class

End Namespace
