Option Strict On
Option Infer On

Imports System.Threading
Imports FO4_Base_Library

Namespace Engine

    ''' <summary>A box primitive of a workshop's build area, ready to test (Fallout4.exe 1.11.240).</summary>
    Friend NotInheritable Class AreaBox
        ''' <summary>Primitive position (DATA).</summary>
        Friend X, Y, Z As Double
        ''' <summary>R = Rz(rz)·Ry(ry)·Rx(rx) of the primitive's DATA angles, row-major (0x1416B79F0, fitted by running it).</summary>
        Friend R(8) As Double
        ''' <summary>The reference scale as the engine keeps it: a whole percent (0x1404F2DC8: XSCL × 100, rounded half
        ''' up; 100 when there is no XSCL, 0x1404F1EEB), read back × 0.01 (0x1404FF850).</summary>
        Friend Scale As Double
        ''' <summary>BGSPrimitiveBox half extents = |XPRM bounds| (0x1402778E8 hands the bounds to the factory 0x140356AC0
        ''' as read; the setter 0x1403577D0 stores their absolute values).</summary>
        Friend HX, HY, HZ As Double

        ''' <summary>0x140510CE0: local = R·(p − t) / s, then BGSPrimitiveBox's test 0x140357810: −h ≤ local ≤ h per axis.</summary>
        Friend Function Contains(px As Double, py As Double, pz As Double) As Boolean
            Dim dx = px - X, dy = py - Y, dz = pz - Z
            Dim lx = (R(0) * dx + R(1) * dy + R(2) * dz) / Scale
            Dim ly = (R(3) * dx + R(4) * dy + R(5) * dz) / Scale
            Dim lz = (R(6) * dx + R(7) * dy + R(8) * dz) / Scale
            Return lx >= -HX AndAlso lx <= HX AndAlso ly >= -HY AndAlso ly <= HY AndAlso lz >= -HZ AndAlso lz <= HZ
        End Function

        ''' <summary>A sphere that holds the box, to skip the far references quickly (not a law: only a bound).</summary>
        Friend ReadOnly Property BoundRadius As Double
            Get
                Return Math.Sqrt(HX * HX + HY * HY + HZ * HZ) * Scale
            End Get
        End Property
    End Class

    ''' <summary>One workshop and what the scan found inside its build area.</summary>
    Friend NotInheritable Class WorkshopArea
        Friend Property Key As String
        Friend Property FormID As UInteger
        Friend Property Name As String
        ''' <summary>0 for a workshop in an interior cell.</summary>
        Friend Property WorldspaceFormID As UInteger
        Friend Property CellFormID As UInteger
        Friend Property Interior As Boolean
        Friend Property X As Double
        Friend Property Y As Double
        Friend Property Z As Double
        ''' <summary>The linked primitives the engine TESTS (not flagged 0x800) whose shape is a box.</summary>
        Friend ReadOnly Property Boxes As New List(Of AreaBox)
        ''' <summary>The linked primitives the engine COUNTS (every linked ref that has a primitive): when there is one,
        ''' the primitives decide and the radius is not used (0x1403845F4, 0x14038466D).</summary>
        Friend Property PrimitiveCount As Integer
        ''' <summary>Tested primitives whose shape is not a box (sphere, plane, line, ellipsoid): their test is NOT
        ''' decoded, so they are left out and the result may miss objects inside them.</summary>
        Friend Property UnsupportedShapes As Integer
        ''' <summary>Without primitives: the radius test (0x14038468C..0x1403847EA), in 3D, on the reference position.</summary>
        Friend Property CenterX As Double
        Friend Property CenterY As Double
        Friend Property CenterZ As Double
        Friend Property Radius As Double
        ''' <summary>Why the radius is what it is (for the user).</summary>
        Friend Property RadiusSource As String = ""
        ''' <summary>Set when the radius needs a location the engine finds by position (not decoded).</summary>
        Friend Property FallbackUnresolved As Boolean
        ''' <summary>Object key → how many of its references are inside.</summary>
        Friend ReadOnly Property Objects As New Dictionary(Of String, Integer)(StringComparer.Ordinal)
        Friend Property BoundRadius As Double

        Friend ReadOnly Property UsesPrimitives As Boolean
            Get
                Return PrimitiveCount > 0
            End Get
        End Property

        ''' <summary>IsRefInBuildArea 0x140384810 → IsPointInBuildArea 0x1403844D0, for a reference already known to be
        ''' in the same interior cell / worldspace.</summary>
        Friend Function Contains(px As Double, py As Double, pz As Double) As Boolean
            If UsesPrimitives Then
                ' The primitives are tested with the point raised by 3.0 ([0x1429294B0], 0x140384513).
                Dim qz = pz + 3.0
                For Each b In Boxes
                    If b.Contains(px, py, qz) Then Return True
                Next
                Return False
            End If
            If FallbackUnresolved Then Return False
            Dim dx = px - CenterX, dy = py - CenterY, dz = pz - CenterZ
            Return dx * dx + dy * dy + dz * dz <= Radius * Radius
        End Function
    End Class

    ''' <summary>Which catalog objects stand inside each workshop's build area, as the plugins place them. Reads the
    ''' plugins twice with <see cref="PlacedRefScanner"/> (workshops and their primitives, then the objects) and keeps
    ''' only the result: per workshop, which objects and how many references. Nothing parsed is kept.
    ''' <para>The law is the engine's (Tools\re-docs\RE_SAFESCRAP_WORKSHOP_SCRAP_2026-09-26.md §3, and the decoding of
    ''' 27-sep in the same file): a workshop is a reference whose base has the DefaultObject Workshop keyword; its build
    ''' area is the primitives linked to it with DefaultObject WorkshopLinkedPrimitiveKeyword, or — without any — a
    ''' radius around it (its location's MNAM/RNAM when the location chain has DefaultObject WorkshopLocationLink,
    ''' otherwise 5000, the default of fItemPlacementRadius:Workshop at 0x142EDF400). A reference counts when it is in
    ''' the same interior cell, or the same worldspace, and its position passes that test.</para>
    ''' <para>What it cannot see: the save game. What the player built, scrapped or moved is not in any plugin.</para></summary>
    Friend NotInheritable Class WorkshopAreas

        Friend ReadOnly Property Workshops As New List(Of WorkshopArea)
        Friend Property ReferencesChecked As Long
        Friend Property Milliseconds As Long

        Friend Const DefaultBuildRadius As Double = 5000.0
        Private Const PrimitiveShapeBox As UInteger = 1UI
        Private Const FlagInitiallyDisabled As UInteger = &H800UI

        ''' <summary>Progress: phase (1 = workshops, 2 = objects), fraction 0..1, the plugin being read.</summary>
        Friend Structure ScanProgress
            Friend Phase As Integer
            Friend Fraction As Double
            Friend Plugin As String
        End Structure

        Friend Shared Function Scan(pm As PluginManager, dataPath As String, catalog As ScrapCatalog,
                                    progress As Action(Of ScanProgress), cancel As CancellationToken) As WorkshopAreas
            Dim sw = Diagnostics.Stopwatch.StartNew()
            Dim result As New WorkshopAreas
            Dim kwWorkshop = DefaultObject(pm, "Workshop")
            Dim kwPrimitive = DefaultObject(pm, "WorkshopLinkedPrimitiveKeyword")
            Dim kwLocation = DefaultObject(pm, "WorkshopLocationLink")
            If kwWorkshop = 0UI OrElse kwPrimitive = 0UI Then Throw New InvalidOperationException("This load order has no Workshop / WorkshopLinkedPrimitiveKeyword default object.")

            ' Location markers (MNAM) of the locations a radius could use: their positions are read in phase 1.
            Dim markers As New Dictionary(Of UInteger, (X As Double, Y As Double, Z As Double))
            For Each lctn As PluginRecord In pm.GetRecordsOfType("LCTN")
                Dim mnam = FormAt(pm, lctn, "MNAM")
                If mnam <> 0UI Then markers(mnam) = (Double.NaN, 0, 0)
            Next

            ' ---- phase 1: workshops and their primitives
            Dim workshopBases As New Dictionary(Of UInteger, Boolean)
            Dim isWorkshopBase = Function(b As UInteger)
                                     Dim v As Boolean
                                     If workshopBases.TryGetValue(b, v) Then Return v
                                     Dim rec = If(b = 0UI, Nothing, pm.GetRecord(b))
                                     v = rec IsNot Nothing AndAlso KeywordsOf(pm, rec).Contains(kwWorkshop)
                                     workshopBases(b) = v
                                     Return v
                                 End Function
            Dim found As New List(Of PlacedRef)
            Dim prims As New Dictionary(Of UInteger, List(Of PlacedRef))
            PlacedRefScanner.Scan(pm, dataPath,
                Sub(r)
                    If isWorkshopBase(r.BaseFormID) Then found.Add(r)
                    If markers.ContainsKey(r.FormID) Then markers(r.FormID) = (r.PosX, r.PosY, r.PosZ)
                    If r.LinkedRefs Is Nothing OrElse r.Primitive Is Nothing Then Return
                    For Each l In r.LinkedRefs
                        If l.Keyword <> kwPrimitive Then Continue For
                        Dim list As List(Of PlacedRef) = Nothing
                        If Not prims.TryGetValue(l.Ref, list) Then list = New List(Of PlacedRef) : prims(l.Ref) = list
                        list.Add(r)
                    Next
                End Sub,
                progress:=Sub(p) progress?.Invoke(New ScanProgress With {.Phase = 1, .Fraction = p.BytesDone / Math.Max(1.0, p.BytesTotal), .Plugin = p.Plugin}),
                cancel:=cancel)

            Dim locations = WorkshopLocations(pm)
            For Each w In found
                Dim a As New WorkshopArea With {
                    .FormID = w.FormID, .Key = ScrapCatalog.KeyOf(pm, w.FormID), .WorldspaceFormID = w.WorldspaceFormID,
                    .CellFormID = w.CellFormID, .Interior = w.WorldspaceFormID = 0UI, .X = w.PosX, .Y = w.PosY, .Z = w.PosZ}
                a.Name = DisplayName(pm, w, a.Interior, locations)
                Dim list As List(Of PlacedRef) = Nothing
                If prims.TryGetValue(w.FormID, list) Then
                    For Each p In list
                        a.PrimitiveCount += 1
                        If (p.RecordFlags And FlagInitiallyDisabled) <> 0UI Then Continue For
                        Dim shape = If(p.Primitive.Length >= 32, BitConverter.ToUInt32(p.Primitive, 28), 0UI)
                        If shape <> PrimitiveShapeBox Then a.UnsupportedShapes += 1 : Continue For
                        Dim b = NewBox(p)
                        a.Boxes.Add(b)
                        a.BoundRadius = Math.Max(a.BoundRadius, Distance(b.X, b.Y, b.Z, a.X, a.Y, a.Z) + b.BoundRadius)
                    Next
                End If
                If Not a.UsesPrimitives Then SetRadius(pm, a, kwLocation, markers)
                If Not a.UsesPrimitives Then a.BoundRadius = Distance(a.CenterX, a.CenterY, a.CenterZ, a.X, a.Y, a.Z) + a.Radius
                result.Workshops.Add(a)
            Next

            ' ---- phase 2: the catalog objects' references, only where a workshop is
            Dim keyOfBase = catalog.Objects.Values.ToDictionary(Function(o) o.FormID, Function(o) o.Key)
            Dim byWorld = result.Workshops.Where(Function(a) Not a.Interior).GroupBy(Function(a) a.WorldspaceFormID).ToDictionary(Function(g) g.Key, Function(g) g.ToList())
            Dim byCell = result.Workshops.Where(Function(a) a.Interior).GroupBy(Function(a) a.CellFormID).ToDictionary(Function(g) g.Key, Function(g) g.ToList())
            Dim checkedRefs As Long = 0
            PlacedRefScanner.Scan(pm, dataPath,
                Sub(r)
                    Dim key As String = Nothing
                    If Not keyOfBase.TryGetValue(r.BaseFormID, key) Then Return
                    Dim candidates As List(Of WorkshopArea) = Nothing
                    If r.WorldspaceFormID <> 0UI Then
                        If Not byWorld.TryGetValue(r.WorldspaceFormID, candidates) Then Return
                    ElseIf Not byCell.TryGetValue(r.CellFormID, candidates) Then
                        Return
                    End If
                    checkedRefs += 1
                    For Each a In candidates
                        If Distance(r.PosX, r.PosY, r.PosZ, a.X, a.Y, a.Z) > a.BoundRadius + 3.0 Then Continue For
                        If Not a.Contains(r.PosX, r.PosY, r.PosZ) Then Continue For
                        Dim n = 0
                        a.Objects.TryGetValue(key, n)
                        a.Objects(key) = n + 1
                    Next
                End Sub,
                wantWorld:=Function(w) byWorld.ContainsKey(w),
                wantInteriorCell:=Function(cell) byCell.ContainsKey(cell),
                progress:=Sub(p) progress?.Invoke(New ScanProgress With {.Phase = 2, .Fraction = p.BytesDone / Math.Max(1.0, p.BytesTotal), .Plugin = p.Plugin}),
                cancel:=cancel)
            result.ReferencesChecked = checkedRefs
            result.Milliseconds = sw.ElapsedMilliseconds
            For Each same In result.Workshops.GroupBy(Function(a) a.Name, StringComparer.OrdinalIgnoreCase).Where(Function(g) g.Count() > 1)
                For Each a In same
                    Dim where = pm.GetRecord(If(a.Interior, a.CellFormID, a.WorldspaceFormID))
                    If where IsNot Nothing Then a.Name &= $" ({where.EditorID})"
                Next
            Next
            result.Workshops.Sort(Function(x, y) StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name))
            Return result
        End Function

        Private Shared Function NewBox(p As PlacedRef) As AreaBox
            Dim b As New AreaBox With {.X = p.PosX, .Y = p.PosY, .Z = p.PosZ,
                .HX = Math.Abs(BitConverter.ToSingle(p.Primitive, 0)),
                .HY = Math.Abs(BitConverter.ToSingle(p.Primitive, 4)),
                .HZ = Math.Abs(BitConverter.ToSingle(p.Primitive, 8))}
            b.Scale = ScalePercent(p.Scale) * 0.01
            Dim cx = Math.Cos(p.RotX), sx = Math.Sin(p.RotX), cy = Math.Cos(p.RotY), sy = Math.Sin(p.RotY), cz = Math.Cos(p.RotZ), sz = Math.Sin(p.RotZ)
            ' Rz·Ry·Rx
            b.R(0) = cz * cy : b.R(1) = cz * sy * sx - sz * cx : b.R(2) = cz * sy * cx + sz * sx
            b.R(3) = sz * cy : b.R(4) = sz * sy * sx + cz * cx : b.R(5) = sz * sy * cx - cz * sx
            b.R(6) = -sy : b.R(7) = cy * sx : b.R(8) = cy * cx
            Return b
        End Function

        ''' <summary>The engine's u16 percent: 100 without XSCL (0x1404F1EEB); with it, trunc(xscl × 100) + 1 when the
        ''' fraction is ≥ 0.5 (0x1404F2DED..0x1404F2E11).</summary>
        Friend Shared Function ScalePercent(xscl As Single?) As Integer
            If Not xscl.HasValue Then Return 100
            Dim v = xscl.Value * 100.0F
            Dim t = CInt(Math.Truncate(v))
            Return CUShort((t + If(v - t >= 0.5F, 1, 0)) And &HFFFF)
        End Function

        ''' <summary>Radius of a workshop without primitives (0x14038468C): the location of the workshop's cell
        ''' (0x140517030 → 0x1404C4E80: encounter zone's location, else XLCN), walked up by PNAM (0x14049D730) to the first
        ''' with the WorkshopLocationLink keyword; that location's RNAM is the radius and its MNAM marker the centre (the
        ''' workshop's own position when the marker is not found). No such location: 5000 around the workshop.</summary>
        Private Shared Sub SetRadius(pm As PluginManager, a As WorkshopArea, kwLocation As UInteger,
                                     markers As Dictionary(Of UInteger, (X As Double, Y As Double, Z As Double)))
            a.CenterX = a.X : a.CenterY = a.Y : a.CenterZ = a.Z
            a.Radius = DefaultBuildRadius
            a.RadiusSource = "build radius 5000 (no area boxes)"
            Dim cell = pm.GetRecord(a.CellFormID)
            Dim loc As UInteger = 0UI
            If cell IsNot Nothing Then
                Dim ez = FormAt(pm, cell, "XEZN")
                Dim ezRec = If(ez = 0UI, Nothing, pm.GetRecord(ez))
                ' ECZN DATA = Owner, Location, … (FO4 schema Rec_ECZN): the zone's only location field.
                If ezRec IsNot Nothing Then loc = FormAt(pm, ezRec, "DATA", 4)
                If loc = 0UI Then loc = FormAt(pm, cell, "XLCN")
            End If
            If loc = 0UI AndAlso Not a.Interior Then
                ' An exterior cell without a location: the engine asks the worldspace by position (0x1405746C0), NOT decoded.
                a.FallbackUnresolved = True
                a.RadiusSource = "no area boxes and a location the game finds by position (not supported)"
                Return
            End If
            Dim seen As New HashSet(Of UInteger)
            While loc <> 0UI AndAlso seen.Add(loc)
                Dim rec = pm.GetRecord(loc)
                If rec Is Nothing Then Exit While
                If kwLocation <> 0UI AndAlso KeywordsOf(pm, rec).Contains(kwLocation) Then
                    Dim rnam = rec.GetSubrecord("RNAM")
                    a.Radius = If(rnam.HasValue AndAlso rnam.Value.Data.Length >= 4, BitConverter.ToSingle(rnam.Value.Data, 0), 0.0)
                    Dim marker = FormAt(pm, rec, "MNAM")
                    Dim m As (X As Double, Y As Double, Z As Double) = Nothing
                    If marker <> 0UI AndAlso markers.TryGetValue(marker, m) AndAlso Not Double.IsNaN(m.X) Then
                        a.CenterX = m.X : a.CenterY = m.Y : a.CenterZ = m.Z
                    End If
                    a.RadiusSource = $"radius {a.Radius:0} of location {rec.EditorID} (no area boxes)"
                    Return
                End If
                loc = FormAt(pm, rec, "PNAM")
            End While
        End Sub

        Private Shared Function Distance(x1 As Double, y1 As Double, z1 As Double, x2 As Double, y2 As Double, z2 As Double) As Double
            Return Math.Sqrt((x1 - x2) ^ 2 + (y1 - y2) ^ 2 + (z1 - z2) ^ 2)
        End Function

        ''' <summary>A DefaultObject's form, as the game binds it: the DFOB record whose EditorID is the object's name,
        ''' its DATA the form (RE §2: the names at the registration stubs; the DFOB values measured in Fallout4.esm).</summary>
        Friend Shared Function DefaultObject(pm As PluginManager, name As String) As UInteger
            Dim d = pm.GetRecordsOfType("DFOB").FirstOrDefault(Function(r) r.EditorID = name)
            Return If(d Is Nothing, 0UI, FormAt(pm, d, "DATA"))
        End Function

        ''' <summary>A FormID stored in a subrecord at <paramref name="offset"/>, in load-order space.</summary>
        Private Shared Function FormAt(pm As PluginManager, rec As PluginRecord, sig As String, Optional offset As Integer = 0) As UInteger
            Dim s = rec.GetSubrecord(sig)
            If Not s.HasValue OrElse s.Value.Data Is Nothing OrElse s.Value.Data.Length < offset + 4 Then Return 0UI
            Return pm.ResolveReferencedFormID(rec.SourcePluginName, BitConverter.ToUInt32(s.Value.Data, offset))
        End Function

        ''' <summary>KWDA: an array of keyword FormIDs (the FO4 schema's Keywords member).</summary>
        Private Shared Function KeywordsOf(pm As PluginManager, rec As PluginRecord) As HashSet(Of UInteger)
            Dim out As New HashSet(Of UInteger)
            Dim s = rec.GetSubrecord("KWDA")
            If Not s.HasValue OrElse s.Value.Data Is Nothing Then Return out
            For i = 0 To s.Value.Data.Length - 4 Step 4
                out.Add(pm.ResolveReferencedFormID(rec.SourcePluginName, BitConverter.ToUInt32(s.Value.Data, i)))
            Next
            Return out
        End Function

        ''' <summary>Workbench → the location that declares it as its workshop reference: an LCTN's LCSR (master) or
        ''' ACSR (added) entry {Loc Ref Type, Ref, World/Cell, Grid Y, Grid X} (FO4 schema Rec_LCTN, 16 bytes) whose Loc Ref
        ''' Type is the DefaultObject WorkshopLocRefType_DO. Measured 27-sep: every listed workbench is in such an entry.</summary>
        Private Shared Function WorkshopLocations(pm As PluginManager) As Dictionary(Of UInteger, PluginRecord)
            Dim out As New Dictionary(Of UInteger, PluginRecord)
            Dim refType = DefaultObject(pm, "WorkshopLocRefType_DO")
            If refType = 0UI Then Return out
            For Each lctn As PluginRecord In pm.GetRecordsOfType("LCTN")
                For Each s In lctn.Subrecords
                    If s.Signature <> "LCSR" AndAlso s.Signature <> "ACSR" OrElse s.Data Is Nothing Then Continue For
                    For i = 0 To s.Data.Length - 16 Step 16
                        If pm.ResolveReferencedFormID(lctn.SourcePluginName, BitConverter.ToUInt32(s.Data, i)) <> refType Then Continue For
                        Dim bench = pm.ResolveReferencedFormID(lctn.SourcePluginName, BitConverter.ToUInt32(s.Data, i + 4))
                        If Not out.ContainsKey(bench) Then out(bench) = lctn
                    Next
                Next
            Next
            Return out
        End Function

        ''' <summary>What the user sees (a label, not a law): the name of the workshop's location; without one, an interior
        ''' workshop by its cell's name and an exterior one by the workbench's EditorID without the "Workshop…Ref" part.</summary>
        Private Shared Function DisplayName(pm As PluginManager, w As PlacedRef, interior As Boolean, locations As Dictionary(Of UInteger, PluginRecord)) As String
            Dim loc As PluginRecord = Nothing
            If locations.TryGetValue(w.FormID, loc) Then
                Dim lfull = loc.GetSubrecord("FULL")
                If lfull.HasValue Then
                    Dim n = pm.ResolveFieldString(loc, lfull.Value)
                    If n <> "" Then Return n
                End If
            End If
            Dim cell = If(interior, pm.GetRecord(w.CellFormID), Nothing)
            If cell IsNot Nothing Then
                Dim full = cell.GetSubrecord("FULL")
                If full.HasValue Then
                    Dim n = pm.ResolveFieldString(cell, full.Value)
                    If n <> "" Then Return n
                End If
            End If
            Dim ed = If(w.EditorID, "")
            If ed = "" AndAlso cell IsNot Nothing Then ed = cell.EditorID
            If ed = "" Then Return ScrapCatalog.KeyOf(pm, w.FormID)
            Dim t = Text.RegularExpressions.Regex.Replace(ed, "^DLC\d\d_?", "")
            t = Text.RegularExpressions.Regex.Replace(t, "(Workshop)?(_?Workbench)?(REF|Ref)?(DUPLICATE\d+)?$", "")
            t = Text.RegularExpressions.Regex.Replace(t, "(?<=[a-z])(?=[A-Z])", " ")
            Return If(t.Trim() = "", ed, t.Trim())
        End Function

    End Class

End Namespace
