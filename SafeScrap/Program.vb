Option Strict On
Option Infer On

Imports System.Runtime.CompilerServices

''' <summary>
''' Entry point. <c>Main</c> touches none of the app's own DLLs (a DLL that fails to load has to be reportable); the real
''' start is <see cref="RealMain"/>. Command line: <c>-l:&lt;language&gt;</c> forces the language of the localized strings.
''' </summary>
Friend Module Program

    <STAThread>
    Sub Main(args As String())
        CrashReport.Install()
        If Not VersionGate.VerificarInstalacion() Then
            Environment.ExitCode = 1
            Return
        End If
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException)
        Try
            RealMain(args)
        Catch ex As Exception
            CrashReport.Report(ex, "main")
            Environment.ExitCode = 1
        End Try
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Private Sub RealMain(args As String())
        Dim language As String = Nothing
        For Each a In args
            If a.StartsWith("-l:", StringComparison.Ordinal) Then language = a.Substring(3)
        Next
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        ' Config_App (config.json next to the exe): the game paths, shared with the other apps.
        FO4_Base_Library.Config_App.DefaultShowHelperShapes = False
        FO4_Base_Library.Config_App.LoadConfig()
        FO4_Base_Library.Config_App.Current.Game = FO4_Base_Library.Config_App.Game_Enum.Fallout4
        Dim boot As AppBootstrap
        Dim paths = Config.AppPaths.ForExecutable()
        Try
            Try
                boot = New AppBootstrap(paths, language)
            Catch ex As Engine.UserRulesException
                ' The user's rule changes are unusable: without a way out the app would close on every start.
                If Not AppDialogs.Confirm(Nothing, "SafeScrap — rules", ex.Message & vbCr & vbCr &
                                          "Move your rule changes to a backup folder and start with the app's rules?") Then Return
                Dim moved = Engine.RuleBook.RestoreDefaults(ex.UserDir, paths.BackupDir)
                AppDialogs.Info(Nothing, "SafeScrap — rules", "Your rule changes were moved to:" & vbCr & moved)
                boot = New AppBootstrap(paths, language)
            End Try
        Catch ex As Engine.RuleDataException
            AppDialogs.Warn(Nothing, "SafeScrap — rules", ex.Message)
            Return
        End Try
        Using pre As New UI.SafeScrapPreflight(boot)
            If pre.ShowDialog() <> DialogResult.OK Then Return
            Try
                boot.UsePlugins(pre.LoadedPluginManager)
            Catch ex As Engine.ScrapCategoryException
                MessageBox.Show(ex.Message, "SafeScrap — load order", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End Try
        End Using
        Application.Run(New UI.MainForm(boot))
    End Sub

End Module
