Option Strict On
Option Infer On

Imports System.IO
Imports System.Text

Namespace Engine

    ''' <summary>Which rule produced a result.</summary>
    Friend Enum DecidedBy
        ObjectDecision = 0
        GroupDecision = 1
        Folder = 2
        ''' <summary>The folders said Yes but the EditorID names a plant, terrain or a building part: lowered to Review.</summary>
        FolderLoweredByWarning = 3
        ''' <summary>The type rule of its record type (e.g. CONT: containers hold items).</summary>
        TypeRule = 4
    End Enum

    ''' <summary>One step of the explanation of a result, in the order the law reads them.</summary>
    Friend NotInheritable Class ReasonStep
        Friend Property Kind As DecidedBy
        ''' <summary>Group key, folder text, or the object key.</summary>
        Friend Property Subject As String
        ''' <summary>What that step says (Nothing = undecided / no rule).</summary>
        Friend Property Says As Verdict?
        ''' <summary>For a folder: its rule's reason; for a warning: the matched words.</summary>
        Friend Property Detail As String = ""
        ''' <summary>True if the user decided this step (object, group or folder decision), False if a shipped rule did.</summary>
        Friend Property ByUser As Boolean
    End Class

    Friend NotInheritable Class ObjectResult
        Friend Property Verdict As Verdict
        Friend Property DecidedBy As DecidedBy
        Friend Property Warning As String = ""

        ''' <summary>The warning matters for this result (the "With warning" filter, user's decision 26-sep: "lo que consideres
        ''' mejor"): it lowered a Yes to Review, or the result is Yes although the EditorID names a plant, terrain or a
        ''' building part (a decision of the user or of a group made it Yes). A No with a warning is not scrapped anyway.</summary>
        Friend ReadOnly Property WarningMatters As Boolean
            Get
                Return DecidedBy = DecidedBy.FolderLoweredByWarning OrElse (Verdict = Verdict.Yes AndAlso Warning <> "")
            End Get
        End Property
        Friend Property Steps As New List(Of ReasonStep)
    End Class

    ''' <summary>The result of an object and why.
    ''' <para>Law (prototype <c>reglas.py</c> <c>object_effective</c>), first rule that applies:
    ''' (1) the object's own decision; (2) the decisions of the groups that contain it — No wins over Yes;
    ''' (3) its folders, each one's decision or else its rule: any No ⇒ No; all Yes ⇒ Yes unless the EditorID warns, then
    ''' Review; otherwise Review. A folder without a rule says Review.</para>
    ''' <para>Between (2) and (3), the TYPE RULE of its record type (user's decision, 26-sep: CONT = No, a container holds
    ''' items): it is a default, so it decides only when none of the object's folders has a decision of the user. Without
    ''' type rules (no <c>types.json</c>) the law is the prototype's.</para></summary>
    Friend NotInheritable Class Evaluator

        Private ReadOnly _rules As RuleSet
        Private ReadOnly _decisions As Decisions

        Friend Sub New(rules As RuleSet, decisions As Decisions)
            _rules = rules
            _decisions = decisions
        End Sub

        ''' <summary>What a folder says: the user's decision, else the rule's (Nothing when no rule matches = Review).</summary>
        Friend Function FolderSays(folder As String, ByRef rule As FolderRule, ByRef byUser As Boolean) As Verdict
            rule = _rules.Match(folder)
            Dim d As Verdict
            If _decisions.Folders.TryGetValue(folder, d) Then byUser = True : Return d
            byUser = False
            Return If(rule Is Nothing, Verdict.Review, rule.Decision)
        End Function

        Friend Function Evaluate(o As ScrapObject) As ObjectResult
            Dim r As New ObjectResult With {.Warning = _rules.Warning(o.EditorID)}

            Dim own As Verdict
            If _decisions.Objects.TryGetValue(o.Key, own) Then
                r.Steps.Add(New ReasonStep With {.Kind = DecidedBy.ObjectDecision, .Subject = o.Key, .Says = own, .ByUser = True})
                r.Verdict = own : r.DecidedBy = DecidedBy.ObjectDecision
                Return r
            End If

            Dim groupSays As New HashSet(Of Verdict)
            For Each g In o.Recipes
                Dim d As Verdict
                If _decisions.Groups.TryGetValue(g, d) Then
                    groupSays.Add(d)
                    r.Steps.Add(New ReasonStep With {.Kind = DecidedBy.GroupDecision, .Subject = g, .Says = d, .ByUser = True})
                End If
            Next
            If groupSays.Count > 0 Then
                r.Verdict = If(groupSays.Contains(Verdict.No), Verdict.No, Verdict.Yes)
                r.DecidedBy = DecidedBy.GroupDecision
                Return r
            End If

            Dim typeRule = _rules.TypeRuleOf(o.Signature)
            If typeRule IsNot Nothing AndAlso Not o.Folders.Any(Function(f) _decisions.Folders.ContainsKey(f)) Then
                r.Steps.Add(New ReasonStep With {.Kind = DecidedBy.TypeRule, .Subject = o.Signature, .Says = typeRule.Decision, .Detail = typeRule.Reason})
                r.Verdict = typeRule.Decision
                r.DecidedBy = DecidedBy.TypeRule
                Return r
            End If

            Dim folderVerdicts As New HashSet(Of Verdict)
            For Each f In o.Folders.Distinct(StringComparer.Ordinal)
                Dim rule As FolderRule = Nothing, byUser As Boolean
                Dim s = FolderSays(f, rule, byUser)
                folderVerdicts.Add(s)
                r.Steps.Add(New ReasonStep With {.Kind = DecidedBy.Folder, .Subject = f, .Says = s, .ByUser = byUser,
                                                 .Detail = If(rule Is Nothing, "", rule.Reason)})
            Next
            r.DecidedBy = DecidedBy.Folder
            If folderVerdicts.Contains(Verdict.No) Then
                r.Verdict = Verdict.No
            ElseIf folderVerdicts.Count = 1 AndAlso folderVerdicts.Contains(Verdict.Yes) Then
                If r.Warning <> "" Then
                    r.Verdict = Verdict.Review
                    r.DecidedBy = DecidedBy.FolderLoweredByWarning
                    r.Steps.Add(New ReasonStep With {.Kind = DecidedBy.FolderLoweredByWarning, .Subject = o.EditorID,
                                                     .Says = Verdict.Review, .Detail = r.Warning})
                Else
                    r.Verdict = Verdict.Yes
                End If
            Else
                r.Verdict = Verdict.Review
            End If
            Return r
        End Function

    End Class

    ''' <summary>The list the F4SE plugin reads: one line per object whose result is Yes, sorted Ordinal by key:
    ''' <c>Plugin|XXXXXX ; EditorID</c>. Lines starting with <c>;</c> are comments.</summary>
    Friend Module ListWriter

        Friend Function Lines(catalog As ScrapCatalog, evaluator As Evaluator) As List(Of String)
            Dim out As New List(Of String) From {
                "; SafeScrap: base objects that are safe to scrap in settlements (generated by SafeScrap; edit the decisions in the app).",
                "; format: Plugin|ObjectID ; EditorID"}
            For Each key In catalog.Objects.Keys.OrderBy(Function(k) k, StringComparer.Ordinal)
                Dim o = catalog.Objects(key)
                If evaluator.Evaluate(o).Verdict = Verdict.Yes Then out.Add($"{o.Key} ; {o.EditorID}")
            Next
            Return out
        End Function

        ''' <summary>What a line of the list file is, for its reader.</summary>
        Friend Enum LineKind
            Skip = 0
            Entry = 1
            Invalid = 2
        End Enum

        ''' <summary>The READING law of the list file (the inverse of <see cref="Lines"/>): empty lines and lines starting
        ''' with <c>;</c> are skipped; otherwise the identifier is the text up to the first <c>;</c> after the <c>|</c>
        ''' (the EditorID comment follows it) and is read with <see cref="FormIdentifiers.TryParse"/>. The F4SE plugin
        ''' implements the same law in C++ (<c>SafeScrap\Native\SafeScrap_FO4\src\SafeScrapList.h</c>); one table of
        ''' cases (<c>list-cases.json</c> there) holds the two to each other.</summary>
        Friend Function ParseLine(line As String, ByRef plugin As String, ByRef localFormID As UInteger) As LineKind
            plugin = ""
            localFormID = 0UI
            Dim t = If(line, "").Trim()
            If t = "" OrElse t.StartsWith(";", StringComparison.Ordinal) Then Return LineKind.Skip
            Dim identifier = line
            Dim pipe = line.IndexOf("|"c)
            If pipe >= 0 Then
                Dim semi = line.IndexOf(";"c, pipe)
                If semi >= 0 Then identifier = line.Substring(0, semi)
            End If
            Return If(FormIdentifiers.TryParse(identifier, plugin, localFormID), LineKind.Entry, LineKind.Invalid)
        End Function

        ''' <summary>The file's exact bytes: UTF-8 without BOM, lines joined with LF, a final LF (one seat: the export compares
        ''' against these to leave an unchanged file alone).</summary>
        Friend Function Bytes(lines As IEnumerable(Of String)) As Byte()
            Return New UTF8Encoding(False).GetBytes(String.Join(vbLf, lines) & vbLf)
        End Function

        Friend Sub Write(path As String, lines As IEnumerable(Of String))
            Dim bytes = ListWriter.Bytes(lines)
            Directory.CreateDirectory(IO.Path.GetDirectoryName(path))
            BSA_BA2_Library_DLL.EscrituraEnElLugar.Escribir(path, Sub(fs) fs.Write(bytes, 0, bytes.Length))
        End Sub

    End Module

End Namespace
