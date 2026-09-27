Option Strict On
Option Infer On

Imports FO4_Base_Library.Canon

Namespace Engine

    ''' <summary>A component a recipe returns.</summary>
    Friend NotInheritable Class ScrapComponent
        Friend Property Key As String
        Friend Property FormID As UInteger
        Friend Property Count As UInteger
        ''' <summary>The component's name (FULL), or its EditorID when it has none.</summary>
        Friend Property Name As String = ""
    End Class

    ''' <summary>The engine's two recipe maps (Fallout4.exe 1.11.240, builder 0x1403A5B50): a COBJ whose category holds
    ''' <c>WorkshopScrappableKeyword</c> goes to map A (0x142EDF220, scrap recipes); every other COBJ goes to map B
    ''' (0x142EDF250, build recipes) for its non-inventory items. <c>GetScrapComponents</c> (0x1403A7E30) looks in A, then B.</summary>
    Friend Enum RecipeKind
        Scrap
        Build
    End Enum

    ''' <summary>A recipe (group): a scrap recipe, or a build recipe whose object the workshop can also scrap.</summary>
    Friend NotInheritable Class ScrapRecipe
        Friend Property Kind As RecipeKind
        Friend Property Key As String
        Friend Property FormID As UInteger
        Friend Property EditorID As String
        ''' <summary>The plugin whose version of the recipe won.</summary>
        Friend Property SourcePlugin As String
        ''' <summary>False when the recipe has no created object (CNAM): it covers nothing.</summary>
        Friend Property HasCreatedObject As Boolean
        Friend Property TargetFormID As UInteger
        Friend Property TargetKey As String = ""
        Friend Property TargetSignature As String = ""
        Friend Property TargetEditorID As String = ""
        Friend Property TargetName As String = ""
        Friend Property Components As New List(Of ScrapComponent)
        ''' <summary>Keys of the base objects the recipe covers (<see cref="MemberExpander"/> order).</summary>
        Friend Property Members As New List(Of String)
    End Class

    ''' <summary>A base object some scrap recipe covers.</summary>
    Friend NotInheritable Class ScrapObject
        Friend Property Key As String
        Friend Property FormID As UInteger
        Friend Property Signature As String
        Friend Property EditorID As String
        Friend Property Name As String = ""
        ''' <summary>The MODL path as written, or "" (see <see cref="HasModel"/>).</summary>
        Friend Property Model As String = ""
        Friend Property HasModel As Boolean
        ''' <summary>The model's material swap (MODS, load-order FormID; 0 = none) and color remapping index (MODC), applied
        ''' to the preview by <c>ShapeMaterialOverrides.ApplyModelMaterial</c>.</summary>
        Friend Property MaterialSwapFormID As UInteger
        Friend Property ColorRemapIndex As Single?
        ''' <summary>The size of the object's bounds (OBND max − min, game units: X, Y, Z), or Nothing when the record
        ''' has no OBND.</summary>
        Friend Property Size As (X As Integer, Y As Integer, Z As Integer)?
        ''' <summary>The folders that decide the object: its own model folder, or its parts' for a static collection
        ''' (one entry per part, repeats kept).</summary>
        Friend Property Folders As New List(Of String)
        ''' <summary>Keys of the recipes that cover it, in the order they were found.</summary>
        Friend Property Recipes As New List(Of String)
    End Class

    ''' <summary>Every scrappable base object of the loaded plugins, read once after the load order.
    ''' <para>The engine's recipe table (<see cref="RecipeKind"/>): every COBJ not flagged Deleted, its created object (CNAM)
    ''' expanded with <see cref="MemberExpander"/>. A COBJ whose category (FNAM) holds <c>WorkshopRecipeFilterScrap</c>
    ''' (Fallout4.esm 0x106D8F = DefaultObject WorkshopScrappableKeyword, checked by EditorID at build time) is a scrap
    ''' recipe and covers every member; any other COBJ (also one without FNAM) is a build recipe and covers only the members
    ''' <see cref="IsBuildScrappableType"/> accepts; a build recipe that covers none is not in the engine's map and is left
    ''' out. The components are FVPA. Records are read through the library's schema (<see cref="CanonBridge.Tree"/>,
    ''' <see cref="CanonRecords.Flst"/>), which turns every reference into a load-order FormID.</para></summary>
    Friend NotInheritable Class ScrapCatalog

        Friend Const ScrapCategoryEditorID As String = "WorkshopRecipeFilterScrap"
        Friend Const ScrapCategoryPlugin As String = "Fallout4.esm"
        Friend Const ScrapCategoryObjectID As UInteger = &H106D8FUI

        ''' <summary>The reference-base types for which Fallout4.exe's 0x14030E570 (1.11.240, jump table at 0x14030E5D5,
        ''' FormType numbers from F4SE GameForms.h) answers False, so the builder puts them in the build-recipe map.
        ''' Left out: the types it answers True for (SCRL, ARMO, BOOK, INGR, MISC, WEAP, AMMO, KEYM, ALCH, NOTE, SLGM, LVLI,
        ''' COBJ); PROJ, decided by bit 6 of [+0xC0]; forms that cannot be a reference's base (OMOD and the rest); and LIGH,
        ''' decided by bit 1 of [+0x154], a runtime field whose source in the record is NOT FOUND (31 workshop lights in the
        ''' vanilla + Scrap Everything corpus, 26-sep).</summary>
        Private Shared ReadOnly BuildScrappableTypes As New HashSet(Of String)(StringComparer.Ordinal) From {
            "ACTI", "TACT", "CONT", "DOOR", "STAT", "SCOL", "MSTT", "GRAS", "TREE", "FLOR", "FURN", "NPC_", "IDLM", "HAZD",
            "BNDS", "TERM"}

        Friend Shared Function IsBuildScrappableType(signature As String) As Boolean
            Return BuildScrappableTypes.Contains(signature)
        End Function

        ''' <summary>Record type → how many objects of the catalog have it, sorted by type (Ordinal).</summary>
        Friend Function TypeCounts() As SortedDictionary(Of String, Integer)
            Dim out As New SortedDictionary(Of String, Integer)(StringComparer.Ordinal)
            For Each o In Objects.Values
                Dim n = 0
                out.TryGetValue(o.Signature, n)
                out(o.Signature) = n + 1
            Next
            Return out
        End Function

        Friend ReadOnly Property Recipes As New List(Of ScrapRecipe)
        Friend ReadOnly Property Objects As New Dictionary(Of String, ScrapObject)(StringComparer.Ordinal)
        ''' <summary>Folder → number of objects in it (an object counts once per folder).</summary>
        Friend ReadOnly Property FolderCounts As New Dictionary(Of String, Integer)(StringComparer.Ordinal)
        ''' <summary>COBJ records that carry an FNAM subrecord in the file, and COBJ records whose FNAM the schema tree
        ''' found: two independent counts (gate control that the schema path resolves).</summary>
        Friend Property CobjWithRawFnam As Integer
        Friend Property CobjWithTreeFnam As Integer

        Friend Shared Function Build(pm As PluginManager) As ScrapCatalog
            Dim scrapKw = pm.GlobalFormIDFromObjectID(ScrapCategoryPlugin, ScrapCategoryObjectID)
            Dim kwRec = If(scrapKw = 0UI, Nothing, pm.GetRecord(scrapKw))
            If kwRec Is Nothing OrElse kwRec.EditorID <> ScrapCategoryEditorID Then
                Throw New ScrapCategoryException(
                    $"{ScrapCategoryPlugin}:{ScrapCategoryObjectID:X6} is not '{ScrapCategoryEditorID}' in this load order; " &
                    "cannot tell which recipes are scrap recipes.")
            End If

            Dim c As New ScrapCatalog
            Dim exists As Func(Of UInteger, Boolean) = Function(f) f <> 0UI AndAlso pm.GetRecord(f) IsNot Nothing
            Dim flstMembers As Func(Of UInteger, IReadOnlyList(Of UInteger)) =
                Function(f)
                    Dim r = pm.GetRecord(f)
                    If r Is Nothing OrElse r.Header.Signature <> "FLST" Then Return Nothing
                    Return CanonRecords.Flst(r, pm).Miembros()
                End Function

            For Each recipeRec In pm.GetRecordsOfType("COBJ")
                If recipeRec.GetSubrecord("FNAM").HasValue Then c.CobjWithRawFnam += 1
                Dim tree = CanonBridge.Tree(recipeRec, pm)
                If tree Is Nothing Then Continue For
                Dim fnam = tree.BySignature("FNAM")
                If fnam IsNot Nothing Then c.CobjWithTreeFnam += 1
                If recipeRec.Header.IsDeleted Then Continue For
                Dim kind = If(fnam IsNot Nothing AndAlso Values(fnam, "Keyword").Contains(scrapKw), RecipeKind.Scrap, RecipeKind.Build)

                Dim cnam = tree.BySignature("CNAM")
                Dim target = If(cnam Is Nothing, 0UI, Values(cnam, "Created Object").FirstOrDefault())
                Dim members = If(target = 0UI, New List(Of UInteger), MemberExpander.Expand(target, flstMembers, exists).ToList())
                If kind = RecipeKind.Build Then
                    members = members.Where(Function(m) IsBuildScrappableType(pm.GetRecord(m).Header.Signature)).ToList()
                    If members.Count = 0 Then Continue For
                End If

                Dim recipe As New ScrapRecipe With {
                    .Kind = kind,
                    .FormID = recipeRec.Header.FormID,
                    .Key = KeyOf(pm, recipeRec.Header.FormID),
                    .EditorID = recipeRec.EditorID,
                    .SourcePlugin = recipeRec.SourcePluginName}
                recipe.HasCreatedObject = target <> 0UI
                recipe.TargetFormID = target
                Dim targetRec = If(target = 0UI, Nothing, pm.GetRecord(target))
                If targetRec IsNot Nothing Then
                    recipe.TargetKey = KeyOf(pm, target)
                    recipe.TargetSignature = targetRec.Header.Signature
                    recipe.TargetEditorID = targetRec.EditorID
                    recipe.TargetName = FullName(pm, targetRec)
                End If
                Dim fvpa = tree.BySignature("FVPA")
                If fvpa IsNot Nothing Then
                    For Each comp In Descendants(fvpa).Where(Function(n) n.Name = "Component" AndAlso n.ChildCount > 0)
                        Dim fid = Values(comp, "Component").FirstOrDefault()
                        Dim cnt = Values(comp, "Count").FirstOrDefault()
                        Dim compRec = If(fid = 0UI, Nothing, pm.GetRecord(fid))
                        recipe.Components.Add(New ScrapComponent With {
                            .FormID = fid, .Count = cnt, .Key = If(fid = 0UI, "", KeyOf(pm, fid)),
                            .Name = If(compRec Is Nothing, "", If(FullName(pm, compRec) <> "", FullName(pm, compRec), compRec.EditorID))})
                    Next
                End If

                For Each m In members
                    Dim obj = c.GetOrAddObject(pm, m)
                    If Not obj.Recipes.Contains(recipe.Key) Then obj.Recipes.Add(recipe.Key)
                    recipe.Members.Add(obj.Key)
                Next
                c.Recipes.Add(recipe)
            Next

            For Each o In c.Objects.Values
                For Each f In o.Folders.Distinct(StringComparer.Ordinal)
                    Dim n = 0
                    c.FolderCounts.TryGetValue(f, n)
                    c.FolderCounts(f) = n + 1
                Next
            Next
            Return c
        End Function

        Private Function GetOrAddObject(pm As PluginManager, fid As UInteger) As ScrapObject
            Dim key = KeyOf(pm, fid)
            Dim o As ScrapObject = Nothing
            If Objects.TryGetValue(key, o) Then Return o
            Dim rec = pm.GetRecord(fid)
            Dim modl = rec.GetSubrecord("MODL")
            o = New ScrapObject With {
                .Key = key, .FormID = fid, .Signature = rec.Header.Signature, .EditorID = rec.EditorID,
                .Name = FullName(pm, rec), .HasModel = modl.HasValue,
                .Model = If(modl.HasValue, modl.Value.AsStringGeneral, "")}
            o.Folders.AddRange(FoldersOf(pm, rec))
            o.Size = BoundsSize(rec)
            If rec.GetSubrecord("MODS").HasValue OrElse rec.GetSubrecord("MODC").HasValue Then
                ' Through the schema tree: it turns MODS into a load-order FormID. The FIRST Model struct is the record's
                ' own (the only MODS/MODC of the base object types a scrap recipe covers).
                Dim tree = CanonBridge.Tree(rec, pm)
                If tree IsNot Nothing Then
                    Dim mods = Descendants(tree).FirstOrDefault(Function(n) n.Signature = "MODS")
                    If mods IsNot Nothing Then o.MaterialSwapFormID = Values(mods, "Material Swap").FirstOrDefault()
                    Dim modc = Descendants(tree).FirstOrDefault(Function(n) n.Signature = "MODC")
                    If modc IsNot Nothing Then o.ColorRemapIndex = SingleValue(modc, "Color Remapping Index")
                End If
            End If
            Objects(key) = o
            Return o
        End Function

        ''' <summary>OBND as the FO4 schema declares it: Min X, Y, Z then Max X, Y, Z, six signed 16-bit integers.</summary>
        Private Shared Function BoundsSize(rec As PluginRecord) As (X As Integer, Y As Integer, Z As Integer)?
            Dim obnd = rec.GetSubrecord("OBND")
            If Not obnd.HasValue OrElse obnd.Value.Data Is Nothing OrElse obnd.Value.Data.Length < 12 Then Return Nothing
            Dim d = obnd.Value.Data
            Dim v = Function(i As Integer) CInt(BitConverter.ToInt16(d, i * 2))
            Return (v(3) - v(0), v(4) - v(1), v(5) - v(2))
        End Function

        ''' <summary>The folders that decide an object (<see cref="ModelFolder"/>).</summary>
        Private Shared Function FoldersOf(pm As PluginManager, rec As PluginRecord) As List(Of String)
            Dim out As New List(Of String)
            If rec.Header.Signature = "SCOL" Then
                Dim tree = CanonBridge.Tree(rec, pm)
                If tree IsNot Nothing Then
                    For Each onam In Descendants(tree).Where(Function(n) n.Signature = "ONAM")
                        Dim part = Values(onam, "Static").FirstOrDefault()
                        Dim partRec = If(part = 0UI, Nothing, pm.GetRecord(part))
                        If partRec Is Nothing Then Continue For
                        Dim pm2 = partRec.GetSubrecord("MODL")
                        If pm2.HasValue Then out.Add(ModelFolder.FromModelPath(pm2.Value.AsStringGeneral))
                    Next
                End If
                If out.Count = 0 Then out.Add(ModelFolder.ScolWithoutParts)
                Return out
            End If
            Dim modl = rec.GetSubrecord("MODL")
            If Not modl.HasValue Then
                out.Add(ModelFolder.NoModel(rec.Header.Signature))
            Else
                out.Add(ModelFolder.FromModelPath(modl.Value.AsStringGeneral))
            End If
            Return out
        End Function

        Friend Shared Function KeyOf(pm As PluginManager, fid As UInteger) As String
            Return FormIdentifiers.Build(pm.GetOriginatingPluginName(fid), fid)
        End Function

        Private Shared Function FullName(pm As PluginManager, rec As PluginRecord) As String
            Dim full = rec.GetSubrecord("FULL")
            If Not full.HasValue Then Return ""
            Return pm.ResolveFieldString(rec, full.Value)
        End Function

        ' ---------------------------------------------------------------- schema tree helpers

        Private Shared Iterator Function Descendants(node As WbNode) As IEnumerable(Of WbNode)
            For Each ch In node.Children
                Yield ch
                For Each d In Descendants(ch)
                    Yield d
                Next
            Next
        End Function

        ''' <summary>The integer values of the node and its descendants named <paramref name="name"/>, in tree order.</summary>
        Private Shared Function Values(node As WbNode, name As String) As List(Of UInteger)
            Dim out As New List(Of UInteger)
            For Each n In {node}.Concat(Descendants(node))
                If n.Name <> name OrElse n.Value Is Nothing Then Continue For
                Dim v = n.Value
                If TypeOf v Is UInteger Then
                    out.Add(DirectCast(v, UInteger))
                Else
                    Try
                        out.Add(CUInt(Convert.ToInt64(v) And &HFFFFFFFFL))
                    Catch ex As InvalidCastException
                    Catch ex As FormatException
                    End Try
                End If
            Next
            Return out
        End Function

        ''' <summary>The float value of the node or descendant named <paramref name="name"/>, or Nothing.</summary>
        Private Shared Function SingleValue(node As WbNode, name As String) As Single?
            For Each n In {node}.Concat(Descendants(node))
                If n.Name = name AndAlso n.Value IsNot Nothing Then Return Convert.ToSingle(n.Value, Globalization.CultureInfo.InvariantCulture)
            Next
            Return Nothing
        End Function

    End Class

    ''' <summary>The load order has no usable scrap category keyword, so no recipe can be recognised. The only exception
    ''' the catalog raises on purpose; anything else is a defect and must reach the crash report.</summary>
    Friend NotInheritable Class ScrapCategoryException
        Inherits Exception
        Friend Sub New(message As String)
            MyBase.New(message)
        End Sub
    End Class

End Namespace
