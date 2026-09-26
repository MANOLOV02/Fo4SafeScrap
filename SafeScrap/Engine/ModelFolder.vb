Option Strict On
Option Infer On

Imports System.Text.RegularExpressions

Namespace Engine

    ''' <summary>The model folder of a base object: where Bethesda filed its mesh, which says what the object IS
    ''' (<c>setdressing\rubble</c> is junk, <c>architecture\…</c> a building, <c>landscape\trees</c> a plant).
    ''' <para>Law (prototype <c>grupos.py</c> <c>folder_of_model</c>): the MODL path, <c>/</c> → <c>\</c>, lower case, split
    ''' on <c>\</c> keeping empty parts; a leading <c>meshes</c> is dropped, then a leading <c>dlcNN</c>; the file name is
    ''' dropped; the first two remaining parts joined with <c>\</c>, or <c>(root)</c> when nothing is left (an EMPTY MODL
    ''' lands here). An object with NO MODL is <c>(no model: SIG)</c>; a static collection takes the folders of its parts
    ''' (see <see cref="ScrapCatalog"/>), or <c>(static collection without parts)</c> when none of them has a model.</para>
    ''' <para>These labels are KEYS (the rules and the user's decisions use them). The prototype's were Spanish
    ''' (<c>(raiz)</c>, <c>(sin modelo: SIG)</c>, <c>(scol sin piezas)</c>); the user chose English keys (26-sep).</para></summary>
    Friend Module ModelFolder

        Friend Const RootFolder As String = "(root)"
        Friend Const ScolWithoutParts As String = "(static collection without parts)"
        Private ReadOnly DlcPrefix As New Regex("^dlc[0-9][0-9]$", RegexOptions.CultureInvariant)

        Friend Function NoModel(signature As String) As String
            Return $"(no model: {signature})"
        End Function

        ''' <summary>A leading <c>dlcNN</c> folder (lower case): dropped from model paths and from rule keys.</summary>
        Friend Function IsDlcFolder(part As String) As Boolean
            Return DlcPrefix.IsMatch(part)
        End Function

        Friend Function FromModelPath(modelPath As String) As String
            Dim parts = New List(Of String)(modelPath.Replace("/"c, "\"c).ToLowerInvariant().Split("\"c))
            If parts.Count > 0 AndAlso parts(0) = "meshes" Then parts.RemoveAt(0)
            If parts.Count > 0 AndAlso IsDlcFolder(parts(0)) Then parts.RemoveAt(0)
            If parts.Count > 0 Then parts.RemoveAt(parts.Count - 1)
            If parts.Count = 0 Then Return RootFolder
            Return String.Join("\", parts.Take(2))
        End Function

    End Module

End Namespace
