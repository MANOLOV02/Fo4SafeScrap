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

    ''' <summary>A scrap recipe (group): a COBJ in the workshop's scrap category.</summary>
    Friend NotInheritable Class ScrapRecipe
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
        ''' <summary>The folders that decide the object: its own model folder, or its parts' for a static collection
        ''' (one entry per part, repeats kept).</summary>
        Friend Property Folders As New List(Of String)
        ''' <summary>Keys of the recipes that cover it, in the order they were found.</summary>
        Friend Property Recipes As New List(Of String)
    End Class

    ''' <summary>Every scrappable base object of the loaded plugins, read once after the load order.
    ''' <para>A scrap recipe = a COBJ whose category (FNAM) holds <c>WorkshopRecipeFilterScrap</c> (Fallout4.esm 0x106D8F,
    ''' checked by EditorID at build time). Its created object (CNAM) is expanded with <see cref="MemberExpander"/>; the
    ''' components are FVPA. Records are read through the library's schema (<see cref="CanonBridge.Tree"/>,
    ''' <see cref="CanonRecords.Flst"/>), which turns every reference into a load-order FormID. Any record type counts as
    ''' a member, part or target (the prototype only loaded some types; measured: same result).</para></summary>
    Friend NotInheritable Class ScrapCatalog

        Friend Const ScrapCategoryEditorID As String = "WorkshopRecipeFilterScrap"
        Friend Const ScrapCategoryPlugin As String = "Fallout4.esm"
        Friend Const ScrapCategoryObjectID As UInteger = &H106D8FUI

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
                If fnam Is Nothing Then Continue For
                c.CobjWithTreeFnam += 1
                If Not Values(fnam, "Keyword").Contains(scrapKw) Then Continue For

                Dim recipe As New ScrapRecipe With {
                    .FormID = recipeRec.Header.FormID,
                    .Key = KeyOf(pm, recipeRec.Header.FormID),
                    .EditorID = recipeRec.EditorID,
                    .SourcePlugin = recipeRec.SourcePluginName}
                Dim cnam = tree.BySignature("CNAM")
                Dim target = If(cnam Is Nothing, 0UI, Values(cnam, "Created Object").FirstOrDefault())
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

                If recipe.HasCreatedObject Then
                    For Each m In MemberExpander.Expand(target, flstMembers, exists)
                        Dim obj = c.GetOrAddObject(pm, m)
                        If Not obj.Recipes.Contains(recipe.Key) Then obj.Recipes.Add(recipe.Key)
                        recipe.Members.Add(obj.Key)
                    Next
                End If
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
