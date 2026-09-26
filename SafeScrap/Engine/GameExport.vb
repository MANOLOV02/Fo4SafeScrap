Option Strict On
Option Infer On

Imports System.IO

Namespace Engine

    ''' <summary>What an export did.</summary>
    Friend NotInheritable Class ExportResult
        ''' <summary>True when the decisions had changes and were saved first.</summary>
        Friend Property SavedDecisions As Boolean
        Friend Property ListPath As String = ""
        ''' <summary>"written" or "unchanged" (the file already had exactly these bytes).</summary>
        Friend Property ListState As String = ""
        ''' <summary>Objects in the list (results Yes).</summary>
        Friend Property Objects As Integer
        ''' <summary>What <see cref="EmbeddedNativePlugin.Install"/> said.</summary>
        Friend Property PluginState As String = ""
    End Class

    ''' <summary>What a removal did.</summary>
    Friend NotInheritable Class RemoveResult
        Friend Property ListRemoved As Boolean
        ''' <summary>What <see cref="EmbeddedNativePlugin.Remove"/> said ("" = there was no plugin).</summary>
        Friend Property PluginState As String = ""
        Friend ReadOnly Property NothingToRemove As Boolean
            Get
                Return Not ListRemoved AndAlso PluginState = ""
            End Get
        End Property
    End Class

    ''' <summary>Export to the game and removal from it (SafeScrap phase C; the user chose a "Remove from game" button, 26-sep).
    ''' <para>The list goes to <c>Data\F4SE\Plugins\SafeScrap.txt</c> (<see cref="ListWriter"/>), the F4SE plugin next to it
    ''' through the workspace's single seat <see cref="EmbeddedNativePlugin"/>. Both are F4SE files: the guard of the game and
    ''' the Data folder is the plugin's (<see cref="EmbeddedNativePlugin.Applies"/>), one seat for both.</para>
    ''' <para>Order (rev-53): the decisions are saved FIRST; if that fails the exception goes up and NOTHING is written to
    ''' the game — the list in the game has to be reproducible from the app. The list is written only if its bytes differ.
    ''' With no object in Yes the list has only its header: the plugin reads it as an empty list and scraps nothing.</para></summary>
    Friend NotInheritable Class GameExport

        Private Sub New()
        End Sub

        Friend Const ListRelativePath As String = "F4SE\Plugins\SafeScrap.txt"

        ''' <summary>SafeScrap's F4SE plugin (<c>SafeScrap\Native\SafeScrap_FO4</c>), embedded with this LogicalName and
        ''' guarded by <c>SafeScrap\Native\SafeScrapPlugin.targets</c>; installed under its own file name.</summary>
        Friend Shared Function NewPlugin() As EmbeddedNativePlugin
            Return New EmbeddedNativePlugin(GetType(GameExport).Assembly, "SafeScrap.Native.SafeScrap_FO4.dll", "F4SE\Plugins\SafeScrap_FO4.dll",
                                            Config_App.Game_Enum.Fallout4, "SAFESCRAP")
        End Function

        Friend Shared Function ListPath(dataPath As String) As String
            Return Path.Combine(dataPath, ListRelativePath)
        End Function

        ''' <summary>Saves the decisions if they changed, writes the list if it changed, installs the plugin. The caller has
        ''' checked <c>plugin.Applies(dataPath)</c>; a save failure is thrown before anything is written.</summary>
        Friend Shared Function Export(session As ReviewSession, decisionsPath As String, dataPath As String, plugin As EmbeddedNativePlugin) As ExportResult
            If Not plugin.Applies(dataPath) Then Throw New InvalidOperationException("Export needs the plugin's game active and its Data folder.")
            Dim r As New ExportResult
            If session.Dirty Then
                session.Save(decisionsPath)
                r.SavedDecisions = True
            End If
            Dim lines = ListWriter.Lines(session.Catalog, New Evaluator(session.Rules, session.Decisions))
            r.Objects = lines.Where(Function(l) Not l.StartsWith(";", StringComparison.Ordinal)).Count()
            r.ListPath = ListPath(dataPath)
            Dim bytes = ListWriter.Bytes(lines)
            If File.Exists(r.ListPath) AndAlso File.ReadAllBytes(r.ListPath).SequenceEqual(bytes) Then
                r.ListState = "unchanged"
            Else
                ListWriter.Write(r.ListPath, lines)
                r.ListState = "written"
            End If
            r.PluginState = plugin.Install(dataPath)
            Return r
        End Function

        ''' <summary>The export confirmation: what will be written, and where (rev-56: it names the plugin's .dll only when
        ''' this build carries it — a confirmation must not announce a file that will not be written).</summary>
        Friend Shared Function ConfirmationText(yesCount As Integer, plugin As EmbeddedNativePlugin, dirty As Boolean, loadOrderDiffs As IReadOnlyList(Of String)) As String
            Dim sb As New Text.StringBuilder
            sb.AppendLine("This writes into your game folder:")
            sb.AppendLine()
            sb.AppendLine($"  Data\{ListRelativePath}  —  {yesCount:N0} objects to scrap" & If(yesCount = 0, " (an empty list: the plugin will scrap nothing)", ""))
            If plugin.HasResource() Then
                sb.AppendLine($"  Data\{plugin.DataRelativePath}  —  the F4SE plugin")
            Else
                sb.AppendLine("  (the F4SE plugin is not part of this version yet: only the list is written)")
            End If
            sb.AppendLine()
            sb.AppendLine(If(dirty, "Your decisions are saved first.", "Your decisions are already saved."))
            If loadOrderDiffs.Count > 0 Then
                sb.AppendLine()
                sb.AppendLine("WARNING: the loaded plugins differ from the game's load order, so the list may not match what the game sees:")
                For Each d In loadOrderDiffs.Take(5)
                    sb.AppendLine("  " & d)
                Next
                If loadOrderDiffs.Count > 5 Then sb.AppendLine($"  … and {loadOrderDiffs.Count - 5} more")
            End If
            Return sb.ToString()
        End Function

        ''' <summary>Removes the list and the plugin from the game. Touches nothing when the plugin's game is not the active
        ''' one or there is no Data folder.</summary>
        Friend Shared Function Remove(dataPath As String, plugin As EmbeddedNativePlugin) As RemoveResult
            Dim r As New RemoveResult
            If Not plugin.Applies(dataPath) Then Return r
            Dim list = ListPath(dataPath)
            If File.Exists(list) Then
                File.Delete(list)
                r.ListRemoved = True
            End If
            r.PluginState = plugin.Remove(dataPath)
            Return r
        End Function

    End Class

End Namespace
