Option Strict On
Option Infer On

Namespace Engine

    ''' <summary>The base objects a scrap recipe covers: its created object, or — when that is a form list — every
    ''' object of the list, nested lists included.
    ''' <para>Law (the validated prototype, <c>grupos.py</c> <c>members()</c>): a FormID that does not exist or was
    ''' already visited contributes nothing; the visited set is shared by the whole walk, so a list reached twice (or a
    ''' cycle A→B→A) and an object listed twice count once; a FormID that is not a list is itself a member.</para>
    ''' <para>The two lookups are INJECTED so the gate calls this very walk on a synthetic graph instead of a copy of it.
    ''' Production passes lambdas over the loaded plugins.</para></summary>
    Friend Module MemberExpander

        ''' <param name="flstMembers">The members of <paramref name="target"/> if it is a form list, Nothing if it is not.</param>
        ''' <param name="exists">True if a record with that FormID is loaded.</param>
        Friend Function Expand(target As UInteger,
                               flstMembers As Func(Of UInteger, IReadOnlyList(Of UInteger)),
                               exists As Func(Of UInteger, Boolean)) As List(Of UInteger)
            Dim out As New List(Of UInteger)
            Dim seen As New HashSet(Of UInteger)
            Walk(target, flstMembers, exists, seen, out)
            Return out
        End Function

        Private Sub Walk(fid As UInteger,
                         flstMembers As Func(Of UInteger, IReadOnlyList(Of UInteger)),
                         exists As Func(Of UInteger, Boolean),
                         seen As HashSet(Of UInteger),
                         out As List(Of UInteger))
            If Not exists(fid) OrElse Not seen.Add(fid) Then Return
            Dim members = flstMembers(fid)
            If members Is Nothing Then
                out.Add(fid)
                Return
            End If
            For Each m In members
                Walk(m, flstMembers, exists, seen, out)
            Next
        End Sub

    End Module

End Namespace
