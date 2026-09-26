Option Strict On
Option Infer On

Imports System.Threading.Tasks
Imports SafeScrap.Config
Imports SafeScrap.Engine

''' <summary>The session: paths, settings, rules, decisions, the loaded plugins and the catalog built from them.</summary>
Friend NotInheritable Class AppBootstrap

    Friend ReadOnly Property Paths As AppPaths
    Friend ReadOnly Property Settings As SafeScrapConfig
    Friend ReadOnly Property LanguageOverride As String
    ''' <summary>The app's rules plus the user's changes.</summary>
    Friend Property Book As RuleBook
    Friend Property Decisions As Decisions
    Friend Property Plugins As PluginManager
    Friend Property Catalog As ScrapCatalog

    ''' <summary>The rules in force.</summary>
    Friend ReadOnly Property Rules As RuleSet
        Get
            Return Book.Effective
        End Get
    End Property

    Friend Sub New(paths As AppPaths, languageOverride As String)
        Me.Paths = paths
        Me.LanguageOverride = languageOverride
        PluginEncodingSettings.InitializeForGame(Config_App.Game_Enum.Fallout4)
        PluginEncodingSettings.SetLanguage(If(String.IsNullOrEmpty(languageOverride), PluginEncodingSettings.ReadLanguageFromIni(), languageOverride))
        PluginEncodingSettings.ApplyOverrideIni(AppDomain.CurrentDomain.BaseDirectory)
        Settings = SafeScrapConfig.Load(paths.ConfigJson)
        Book = RuleBook.Load(paths.RulesDir, paths.UserRulesDir)
        Decisions = Engine.Decisions.Load(paths.DecisionsFile)
    End Sub

    Friend ReadOnly Property DataDir As String
        Get
            Return Config_App.Current.DataPath
        End Get
    End Property

    ''' <summary>Takes the plugins the load-order dialog loaded and builds the catalog. A transaction: the catalog is built
    ''' first and <see cref="Plugins"/> and <see cref="Catalog"/> change TOGETHER only when it succeeds; when the load order
    ''' has no scrap category (<see cref="ScrapCategoryException"/>) the session keeps what it had.</summary>
    Friend Sub UsePlugins(pm As PluginManager)
        Dim built = ScrapCatalog.Build(pm)
        Plugins = pm
        Catalog = built
    End Sub

    ''' <summary>Fills the loose/BA2 file dictionary for the given plugins (meshes, textures, localized strings). Runs on
    ''' the thread pool: waiting on it from a WinForms UI thread would hang (Item Sorter).</summary>
    Friend Sub FillFiles(pm As PluginManager)
        IO.Directory.CreateDirectory(Paths.CacheDir)
        FilesDictionary_class.CacheDirectory = Paths.CacheDir
        Dim loaded = pm.Plugins.Where(Function(x) x IsNot Nothing).Select(Function(x) x.FileName).ToList()
        Dim noProgress As New Progress(Of (Stepn As String, Value As Integer, Max As Integer))()
        Dim data = DataDir
        Task.Run(Function() FilesDictionary_class.Fill_DictionaryAsync(data, noProgress, loadedPlugins:=loaded)).GetAwaiter().GetResult()
    End Sub

    ''' <summary>Headless load (no dialog): the given plugin list, the loose/BA2 file dictionary (the localized strings
    ''' come through it), then the catalog. Used by the gate.</summary>
    Friend Sub LoadPlugins(order As IEnumerable(Of String))
        Dim pm As New PluginManager()
        pm.LoadAllPlugins(DataDir, order, Nothing, ALL_FO4_SIGNATURES)
        FillFiles(pm)
        UsePlugins(pm)
    End Sub

    Friend Function NewEvaluator() As Evaluator
        Return New Evaluator(Rules, Decisions)
    End Function

    ''' <summary>The active load order the game will use, restricted to plugins present in Data, compared with what was
    ''' loaded: returns the differences as text lines (empty = same). Plugins excluded for missing masters are named apart.</summary>
    Friend Function SelectionDifferences() As List(Of String)
        Dim out As New List(Of String)
        If Plugins Is Nothing Then Return out
        Dim active = PluginManager.ReadActiveLoadOrder().
            Where(Function(f) IO.File.Exists(IO.Path.Combine(DataDir, f))).ToList()
        Dim loaded = Plugins.Plugins.Select(Function(p) p.FileName).ToList()
        For Each f In active.Except(loaded, StringComparer.OrdinalIgnoreCase)
            If Plugins.LastExcludedForMissingMasters.Contains(f, StringComparer.OrdinalIgnoreCase) Then
                out.Add($"Excluded (missing masters): {f}")
            Else
                out.Add($"Active in the game but not loaded here: {f}")
            End If
        Next
        For Each f In loaded.Except(active, StringComparer.OrdinalIgnoreCase)
            out.Add($"Loaded here but not active in the game: {f}")
        Next
        If out.Count = 0 Then
            Dim common = loaded.Where(Function(f) active.Contains(f, StringComparer.OrdinalIgnoreCase)).ToList()
            Dim commonActive = active.Where(Function(f) common.Contains(f, StringComparer.OrdinalIgnoreCase)).ToList()
            If Not common.SequenceEqual(commonActive, StringComparer.OrdinalIgnoreCase) Then out.Add("Same plugins, different order than the game.")
        End If
        Return out
    End Function

End Class
