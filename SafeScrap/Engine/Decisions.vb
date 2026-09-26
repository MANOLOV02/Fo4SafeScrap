Option Strict On
Option Infer On

Imports System.IO
Imports System.Text.Json

Namespace Engine

    ''' <summary>The user's decisions: Yes or No per object, per group (scrap recipe) and per model folder. A missing
    ''' entry is "undecided" — the next rule down decides. Objects and groups are keyed by their form identifier
    ''' (<c>Plugin|XXXXXX</c>, <see cref="FormIdentifiers"/>), folders by the folder text, so the decisions survive any
    ''' load-order change.</summary>
    Friend NotInheritable Class Decisions

        Friend ReadOnly Property Objects As New Dictionary(Of String, Verdict)(StringComparer.OrdinalIgnoreCase)
        Friend ReadOnly Property Groups As New Dictionary(Of String, Verdict)(StringComparer.OrdinalIgnoreCase)
        Friend ReadOnly Property Folders As New Dictionary(Of String, Verdict)(StringComparer.Ordinal)

        ''' <summary>Loads <paramref name="path"/>; a missing file is an empty set of decisions.</summary>
        Friend Shared Function Load(path As String) As Decisions
            Dim d As New Decisions
            If Not File.Exists(path) Then Return d
            RuleSet.ReadJson(path, Function(doc)
                                       Fill(doc.RootElement, "objects", d.Objects, path)
                                       Fill(doc.RootElement, "groups", d.Groups, path)
                                       Fill(doc.RootElement, "folders", d.Folders, path)
                                       Return True
                                   End Function)
            Return d
        End Function

        Private Shared Sub Fill(root As JsonElement, name As String, target As Dictionary(Of String, Verdict), source As String)
            Dim section As JsonElement
            If Not root.TryGetProperty(name, section) Then Return
            For Each p In section.EnumerateObject()
                If p.Value.ValueKind <> JsonValueKind.String Then
                    Throw New RuleDataException($"{source}: the decision for '{p.Name}' must be ""yes"" or ""no"".")
                End If
                Dim v = RuleSet.ParseVerdict(p.Value.GetString(), source)
                If v = Verdict.Review Then Continue For      ' "review" = undecided: not stored
                target(p.Name) = v
            Next
        End Sub

        ''' <summary>Writes the decisions (sorted, Ordinal) with the workspace's in-place write.</summary>
        Friend Sub Save(path As String)
            Directory.CreateDirectory(IO.Path.GetDirectoryName(path))
            Dim bytes As Byte()
            Using ms As New MemoryStream()
                Using w As New Utf8JsonWriter(ms, New JsonWriterOptions With {.Indented = True})
                    w.WriteStartObject()
                    w.WriteNumber("format", 1)
                    Write(w, "objects", Objects)
                    Write(w, "groups", Groups)
                    Write(w, "folders", Folders)
                    w.WriteEndObject()
                End Using
                bytes = ms.ToArray()
            End Using
            BSA_BA2_Library_DLL.EscrituraEnElLugar.Escribir(path, Sub(fs) fs.Write(bytes, 0, bytes.Length))
        End Sub

        Private Shared Sub Write(w As Utf8JsonWriter, name As String, source As Dictionary(Of String, Verdict))
            w.WriteStartObject(name)
            For Each kv In source.OrderBy(Function(k) k.Key, StringComparer.Ordinal)
                w.WriteString(kv.Key, RuleSet.VerdictKey(kv.Value))
            Next
            w.WriteEndObject()
        End Sub

    End Class

End Namespace
