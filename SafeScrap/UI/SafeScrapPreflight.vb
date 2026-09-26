Option Strict On
Option Infer On

Namespace UI

    ''' <summary>The load order dialog: the library's shared form (<see cref="LoadOrderPreflight_Form"/>) for Fallout 4 only,
    ''' with ALL signatures (a form list or a static collection can point to any record type, and the name and model of any
    ''' base object are needed), the <c>-l:</c> language, the caches in the app folder and the selection remembered in
    ''' <c>safescrap_config.json</c> (user's decision: free selection like Item Sorter; the main window warns when it differs
    ''' from the game's load order).</summary>
    Friend Class SafeScrapPreflight
        Inherits LoadOrderPreflight_Form

        Private ReadOnly _boot As AppBootstrap

        Friend Sub New(boot As AppBootstrap)
            _boot = boot
            WindowTitle = VersionGate.TituloConVersion("SafeScrap") & " — Load order"
            AllowedGames = {Config_App.Game_Enum.Fallout4}
            SignatureFilter = ALL_FO4_SIGNATURES
            LanguageOverride = boot.LanguageOverride
            CacheDirectory = boot.Paths.CacheDir
        End Sub

        Protected Overrides Function LoadSavedSelection(game As Config_App.Game_Enum) As IEnumerable(Of String)
            Return _boot.Settings.PreflightSelection
        End Function

        Protected Overrides Sub SaveSelection(game As Config_App.Game_Enum, literalOrNothing As List(Of String))
            _boot.Settings.PreflightSelection = If(literalOrNothing, New List(Of String))
            _boot.Settings.Save(_boot.Paths.ConfigJson)
        End Sub

    End Class

End Namespace
