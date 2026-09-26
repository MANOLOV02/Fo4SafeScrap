Option Strict On
Option Infer On

Imports System.IO

Namespace Config

    ''' <summary>Where the app keeps things, all next to the executable (like Item Sorter): the shipped rules
    ''' (<c>SafeScrapRules\</c>, read-only), the user's rule copies and decisions (<c>UserData\</c>), caches and the config.</summary>
    Friend NotInheritable Class AppPaths

        Friend ReadOnly Property BaseDir As String
        Friend ReadOnly Property RulesDir As String
        Friend ReadOnly Property UserData As String

        Friend Sub New(baseDir As String, rulesDir As String, userData As String)
            Me.BaseDir = baseDir
            Me.RulesDir = rulesDir
            Me.UserData = userData
        End Sub

        Friend Shared Function ForExecutable() As AppPaths
            Dim b = AppContext.BaseDirectory
            Return New AppPaths(b, Path.Combine(b, "SafeScrapRules"), Path.Combine(b, "UserData"))
        End Function

        Friend ReadOnly Property UserRulesDir As String
            Get
                Return Path.Combine(UserData, "Rules")
            End Get
        End Property

        ''' <summary>Where Restore defaults moves the user's rule changes.</summary>
        Friend ReadOnly Property BackupDir As String
            Get
                Return Path.Combine(UserData, "Backup")
            End Get
        End Property

        Friend ReadOnly Property DecisionsFile As String
            Get
                Return Path.Combine(UserData, "decisions.json")
            End Get
        End Property

        Friend ReadOnly Property CacheDir As String
            Get
                Return Path.Combine(BaseDir, "Caches")
            End Get
        End Property

        Friend ReadOnly Property ConfigJson As String
            Get
                Return Path.Combine(BaseDir, "safescrap_config.json")
            End Get
        End Property

    End Class

    ''' <summary>The app's own settings (<c>safescrap_config.json</c>): the load-order selection and the window.</summary>
    Friend NotInheritable Class SafeScrapConfig
        Public Property PreflightSelection As New List(Of String)
        Public Property MainWindowLeft As Integer
        Public Property MainWindowTop As Integer
        Public Property MainWindowWidth As Integer
        Public Property MainWindowHeight As Integer
        Public Property MainWindowMaximized As Boolean

        Friend Shared Function Load(path As String) As SafeScrapConfig
            If Not IO.File.Exists(path) Then Return New SafeScrapConfig
            Return If(FO4_Base_Library.JsonConfigIO.Load(Of SafeScrapConfig)(path, "SafeScrap configuration"), New SafeScrapConfig)
        End Function

        Friend Sub Save(path As String)
            FO4_Base_Library.JsonConfigIO.Save(Me, path, "SafeScrap configuration")
        End Sub
    End Class

End Namespace
