Namespace UI

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class RulesDialog
    Inherits FO4_Base_Library.IconFormBase

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        components = New System.ComponentModel.Container()
        LayoutMain = New TableLayoutPanel()
        Tabs = New TabControl()
        TabFolderRules = New TabPage()
        FoldersLayout = New TableLayoutPanel()
        LabelFoldersHelp = New Label()
        GridRules = New DataGridView()
        ColFolder = New DataGridViewTextBoxColumn()
        ColDecision = New DataGridViewComboBoxColumn()
        ColReason = New DataGridViewTextBoxColumn()
        ColMatches = New DataGridViewTextBoxColumn()
        ColStatus = New DataGridViewTextBoxColumn()
        FolderButtons = New FlowLayoutPanel()
        ButtonAddRule = New Button()
        ButtonRemoveRule = New Button()
        GroupConflicts = New GroupBox()
        ConflictsLayout = New TableLayoutPanel()
        ListConflicts = New ListBox()
        ConflictButtons = New FlowLayoutPanel()
        ButtonKeepMine = New Button()
        ButtonUseApps = New Button()
        TabTypeRules = New TabPage()
        TypesLayout = New TableLayoutPanel()
        LabelTypesHelp = New Label()
        GridTypes = New DataGridView()
        ColType = New DataGridViewComboBoxColumn()
        ColTypeDecision = New DataGridViewComboBoxColumn()
        ColTypeReason = New DataGridViewTextBoxColumn()
        ColTypeMatches = New DataGridViewTextBoxColumn()
        ColTypeStatus = New DataGridViewTextBoxColumn()
        TypeButtons = New FlowLayoutPanel()
        ButtonAddType = New Button()
        ButtonRemoveType = New Button()
        TabWords = New TabPage()
        WordsLayout = New TableLayoutPanel()
        LabelWordsHelp = New Label()
        GridWords = New DataGridView()
        ColCategory = New DataGridViewComboBoxColumn()
        ColWord = New DataGridViewTextBoxColumn()
        ColWordStatus = New DataGridViewTextBoxColumn()
        WordButtons = New FlowLayoutPanel()
        ButtonAddWord = New Button()
        ButtonRemoveWord = New Button()
        LabelProblems = New Label()
        BottomBar = New TableLayoutPanel()
        ButtonRestore = New Button()
        ButtonSave = New Button()
        ButtonCancel = New Button()
        ToolTips = New ToolTip(components)
        LayoutMain.SuspendLayout()
        Tabs.SuspendLayout()
        TabFolderRules.SuspendLayout()
        FoldersLayout.SuspendLayout()
        CType(GridRules, System.ComponentModel.ISupportInitialize).BeginInit()
        FolderButtons.SuspendLayout()
        GroupConflicts.SuspendLayout()
        ConflictsLayout.SuspendLayout()
        ConflictButtons.SuspendLayout()
        TabTypeRules.SuspendLayout()
        TypesLayout.SuspendLayout()
        CType(GridTypes, System.ComponentModel.ISupportInitialize).BeginInit()
        TypeButtons.SuspendLayout()
        TabWords.SuspendLayout()
        WordsLayout.SuspendLayout()
        CType(GridWords, System.ComponentModel.ISupportInitialize).BeginInit()
        WordButtons.SuspendLayout()
        BottomBar.SuspendLayout()
        SuspendLayout()
        '
        'LayoutMain
        '
        LayoutMain.ColumnCount = 1
        LayoutMain.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        LayoutMain.Controls.Add(Tabs, 0, 0)
        LayoutMain.Controls.Add(GroupConflicts, 0, 1)
        LayoutMain.Controls.Add(LabelProblems, 0, 2)
        LayoutMain.Controls.Add(BottomBar, 0, 3)
        LayoutMain.Dock = DockStyle.Fill
        LayoutMain.Name = "LayoutMain"
        LayoutMain.Padding = New Padding(8)
        LayoutMain.RowCount = 4
        LayoutMain.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        LayoutMain.RowStyles.Add(New RowStyle())
        LayoutMain.RowStyles.Add(New RowStyle())
        LayoutMain.RowStyles.Add(New RowStyle())
        LayoutMain.TabIndex = 0
        '
        'Tabs
        '
        Tabs.Controls.Add(TabFolderRules)
        Tabs.Controls.Add(TabTypeRules)
        Tabs.Controls.Add(TabWords)
        Tabs.Dock = DockStyle.Fill
        Tabs.Name = "Tabs"
        Tabs.SelectedIndex = 0
        Tabs.TabIndex = 0
        '
        'TabFolderRules
        '
        TabFolderRules.Controls.Add(FoldersLayout)
        TabFolderRules.Name = "TabFolderRules"
        TabFolderRules.Padding = New Padding(6)
        TabFolderRules.TabIndex = 0
        TabFolderRules.Text = "Folder rules"
        TabFolderRules.UseVisualStyleBackColor = True
        '
        'FoldersLayout
        '
        FoldersLayout.ColumnCount = 1
        FoldersLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        FoldersLayout.Controls.Add(LabelFoldersHelp, 0, 0)
        FoldersLayout.Controls.Add(GridRules, 0, 1)
        FoldersLayout.Controls.Add(FolderButtons, 0, 2)
        FoldersLayout.Dock = DockStyle.Fill
        FoldersLayout.Name = "FoldersLayout"
        FoldersLayout.RowCount = 3
        FoldersLayout.RowStyles.Add(New RowStyle())
        FoldersLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        FoldersLayout.RowStyles.Add(New RowStyle())
        FoldersLayout.TabIndex = 0
        '
        'LabelFoldersHelp
        '
        LabelFoldersHelp.AutoSize = True
        LabelFoldersHelp.Dock = DockStyle.Fill
        LabelFoldersHelp.Margin = New Padding(3, 0, 3, 6)
        LabelFoldersHelp.Name = "LabelFoldersHelp"
        LabelFoldersHelp.TabIndex = 0
        LabelFoldersHelp.Text = "An object whose mesh is in this folder (or in a folder under it) gets this decision; the most specific folder wins; " &
            "no rule = Review. Write the folder as it is under Data\Meshes, at most two levels, e.g. setdressing\rubble (it is cleaned up for you: lower case, no Meshes\ or DLCnn\ at the start). " &
            "Each folder can have only one rule."
        '
        'GridRules
        '
        GridRules.AllowUserToAddRows = False
        GridRules.AllowUserToResizeRows = False
        GridRules.BackgroundColor = SystemColors.Window
        GridRules.Columns.AddRange(New DataGridViewColumn() {ColFolder, ColDecision, ColReason, ColMatches, ColStatus})
        GridRules.Dock = DockStyle.Fill
        GridRules.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
        GridRules.Name = "GridRules"
        GridRules.RowHeadersWidth = 24
        GridRules.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        GridRules.TabIndex = 1
        '
        'ColFolder
        '
        ColFolder.HeaderText = "Folder"
        ColFolder.Name = "ColFolder"
        ColFolder.Width = 260
        '
        'ColDecision
        '
        ColDecision.HeaderText = "Decision"
        ColDecision.Items.AddRange(New Object() {"Yes", "No", "Review"})
        ColDecision.Name = "ColDecision"
        ColDecision.Width = 80
        '
        'ColReason
        '
        ColReason.HeaderText = "Reason"
        ColReason.Name = "ColReason"
        ColReason.Width = 170
        '
        'ColMatches
        '
        ColMatches.HeaderText = "Decides"
        ColMatches.Name = "ColMatches"
        ColMatches.ReadOnly = True
        ColMatches.ToolTipText = "How many objects of the loaded plugins this rule decides."
        ColMatches.Width = 130
        '
        'ColStatus
        '
        ColStatus.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        ColStatus.HeaderText = "Status"
        ColStatus.Name = "ColStatus"
        ColStatus.ReadOnly = True
        '
        'FolderButtons
        '
        FolderButtons.AutoSize = True
        FolderButtons.Controls.Add(ButtonAddRule)
        FolderButtons.Controls.Add(ButtonRemoveRule)
        FolderButtons.Dock = DockStyle.Fill
        FolderButtons.Name = "FolderButtons"
        FolderButtons.TabIndex = 2
        '
        'ButtonAddRule
        '
        ButtonAddRule.AutoSize = True
        ButtonAddRule.Name = "ButtonAddRule"
        ButtonAddRule.TabIndex = 0
        ButtonAddRule.Text = "Add rule"
        ButtonAddRule.UseVisualStyleBackColor = True
        '
        'ButtonRemoveRule
        '
        ButtonRemoveRule.AutoSize = True
        ButtonRemoveRule.Name = "ButtonRemoveRule"
        ButtonRemoveRule.TabIndex = 1
        ButtonRemoveRule.Text = "Remove rule"
        ButtonRemoveRule.UseVisualStyleBackColor = True
        '
        'GroupConflicts
        '
        GroupConflicts.AutoSize = True
        GroupConflicts.Controls.Add(ConflictsLayout)
        GroupConflicts.Dock = DockStyle.Fill
        GroupConflicts.ForeColor = Color.DarkRed
        GroupConflicts.Name = "GroupConflicts"
        GroupConflicts.TabIndex = 3
        GroupConflicts.TabStop = False
        GroupConflicts.Text = "The app changed these rules since your edit"
        '
        'ConflictsLayout
        '
        ConflictsLayout.AutoSize = True
        ConflictsLayout.ColumnCount = 1
        ConflictsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        ConflictsLayout.Controls.Add(ListConflicts, 0, 0)
        ConflictsLayout.Controls.Add(ConflictButtons, 0, 1)
        ConflictsLayout.Dock = DockStyle.Fill
        ConflictsLayout.Name = "ConflictsLayout"
        ConflictsLayout.RowCount = 2
        ConflictsLayout.RowStyles.Add(New RowStyle())
        ConflictsLayout.RowStyles.Add(New RowStyle())
        ConflictsLayout.TabIndex = 0
        '
        'ListConflicts
        '
        ListConflicts.Dock = DockStyle.Fill
        ListConflicts.ForeColor = SystemColors.WindowText
        ListConflicts.IntegralHeight = False
        ListConflicts.Name = "ListConflicts"
        ListConflicts.Size = New Size(600, 80)
        ListConflicts.TabIndex = 0
        '
        'ConflictButtons
        '
        ConflictButtons.AutoSize = True
        ConflictButtons.Controls.Add(ButtonKeepMine)
        ConflictButtons.Controls.Add(ButtonUseApps)
        ConflictButtons.Name = "ConflictButtons"
        ConflictButtons.TabIndex = 1
        '
        'ButtonKeepMine
        '
        ButtonKeepMine.AutoSize = True
        ButtonKeepMine.ForeColor = SystemColors.ControlText
        ButtonKeepMine.Name = "ButtonKeepMine"
        ButtonKeepMine.TabIndex = 0
        ButtonKeepMine.Text = "Keep mine"
        ToolTips.SetToolTip(ButtonKeepMine, "Your version stays and the mark goes away.")
        ButtonKeepMine.UseVisualStyleBackColor = True
        '
        'ButtonUseApps
        '
        ButtonUseApps.AutoSize = True
        ButtonUseApps.ForeColor = SystemColors.ControlText
        ButtonUseApps.Name = "ButtonUseApps"
        ButtonUseApps.TabIndex = 1
        ButtonUseApps.Text = "Use the app's"
        ToolTips.SetToolTip(ButtonUseApps, "Your change is dropped; the app's current rule applies.")
        ButtonUseApps.UseVisualStyleBackColor = True
        '
        'TabTypeRules
        '
        TabTypeRules.Controls.Add(TypesLayout)
        TabTypeRules.Name = "TabTypeRules"
        TabTypeRules.Padding = New Padding(6)
        TabTypeRules.TabIndex = 1
        TabTypeRules.Text = "Type rules"
        TabTypeRules.UseVisualStyleBackColor = True
        '
        'TypesLayout
        '
        TypesLayout.ColumnCount = 1
        TypesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        TypesLayout.Controls.Add(LabelTypesHelp, 0, 0)
        TypesLayout.Controls.Add(GridTypes, 0, 1)
        TypesLayout.Controls.Add(TypeButtons, 0, 2)
        TypesLayout.Dock = DockStyle.Fill
        TypesLayout.Name = "TypesLayout"
        TypesLayout.RowCount = 3
        TypesLayout.RowStyles.Add(New RowStyle())
        TypesLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        TypesLayout.RowStyles.Add(New RowStyle())
        TypesLayout.TabIndex = 0
        '
        'LabelTypesHelp
        '
        LabelTypesHelp.AutoSize = True
        LabelTypesHelp.Dock = DockStyle.Fill
        LabelTypesHelp.Margin = New Padding(3, 0, 3, 6)
        LabelTypesHelp.Name = "LabelTypesHelp"
        LabelTypesHelp.TabIndex = 0
        LabelTypesHelp.Text = "The default for every object of a record type, e.g. CONT (containers: they hold items). It wins over the folder rules, " &
            "but not over your decisions: an object or group you decided, or a folder you decided on the Folders tab. Pick the record type from the list: it has the types of the loaded objects."
        '
        'GridTypes
        '
        GridTypes.AllowUserToAddRows = False
        GridTypes.AllowUserToResizeRows = False
        GridTypes.BackgroundColor = SystemColors.Window
        GridTypes.Columns.AddRange(New DataGridViewColumn() {ColType, ColTypeDecision, ColTypeReason, ColTypeMatches, ColTypeStatus})
        GridTypes.Dock = DockStyle.Fill
        GridTypes.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
        GridTypes.Name = "GridTypes"
        GridTypes.RowHeadersWidth = 24
        GridTypes.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        GridTypes.TabIndex = 1
        '
        'ColType
        '
        ColType.HeaderText = "Type"
        ColType.Name = "ColType"
        ColType.Width = 90
        '
        'ColTypeDecision
        '
        ColTypeDecision.HeaderText = "Decision"
        ColTypeDecision.Items.AddRange(New Object() {"Yes", "No", "Review"})
        ColTypeDecision.Name = "ColTypeDecision"
        ColTypeDecision.Width = 80
        '
        'ColTypeReason
        '
        ColTypeReason.HeaderText = "Reason"
        ColTypeReason.Name = "ColTypeReason"
        ColTypeReason.Width = 200
        '
        'ColTypeMatches
        '
        ColTypeMatches.HeaderText = "Decides"
        ColTypeMatches.Name = "ColTypeMatches"
        ColTypeMatches.ReadOnly = True
        ColTypeMatches.ToolTipText = "How many objects of the loaded plugins have this type (your folder decisions can still override it)."
        ColTypeMatches.Width = 130
        '
        'ColTypeStatus
        '
        ColTypeStatus.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        ColTypeStatus.HeaderText = "Status"
        ColTypeStatus.Name = "ColTypeStatus"
        ColTypeStatus.ReadOnly = True
        '
        'TypeButtons
        '
        TypeButtons.AutoSize = True
        TypeButtons.Controls.Add(ButtonAddType)
        TypeButtons.Controls.Add(ButtonRemoveType)
        TypeButtons.Dock = DockStyle.Fill
        TypeButtons.Name = "TypeButtons"
        TypeButtons.TabIndex = 2
        '
        'ButtonAddType
        '
        ButtonAddType.AutoSize = True
        ButtonAddType.Name = "ButtonAddType"
        ButtonAddType.TabIndex = 0
        ButtonAddType.Text = "Add type rule"
        ButtonAddType.UseVisualStyleBackColor = True
        '
        'ButtonRemoveType
        '
        ButtonRemoveType.AutoSize = True
        ButtonRemoveType.Name = "ButtonRemoveType"
        ButtonRemoveType.TabIndex = 1
        ButtonRemoveType.Text = "Remove type rule"
        ButtonRemoveType.UseVisualStyleBackColor = True
        '
        'TabWords
        '
        TabWords.Controls.Add(WordsLayout)
        TabWords.Name = "TabWords"
        TabWords.Padding = New Padding(6)
        TabWords.TabIndex = 2
        TabWords.Text = "Warning words"
        TabWords.UseVisualStyleBackColor = True
        '
        'WordsLayout
        '
        WordsLayout.ColumnCount = 1
        WordsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        WordsLayout.Controls.Add(LabelWordsHelp, 0, 0)
        WordsLayout.Controls.Add(GridWords, 0, 1)
        WordsLayout.Controls.Add(WordButtons, 0, 2)
        WordsLayout.Dock = DockStyle.Fill
        WordsLayout.Name = "WordsLayout"
        WordsLayout.RowCount = 3
        WordsLayout.RowStyles.Add(New RowStyle())
        WordsLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        WordsLayout.RowStyles.Add(New RowStyle())
        WordsLayout.TabIndex = 0
        '
        'LabelWordsHelp
        '
        LabelWordsHelp.AutoSize = True
        LabelWordsHelp.Dock = DockStyle.Fill
        LabelWordsHelp.Margin = New Padding(3, 0, 3, 6)
        LabelWordsHelp.Name = "LabelWordsHelp"
        LabelWordsHelp.TabIndex = 0
        LabelWordsHelp.Text = "When the folders say Yes but the EditorID contains one of these words, the object is lowered to Review. " &
            "The EditorID is split into words at each capital letter and at every non-letter (TreeStumpLarge01 = tree, stump, large), " &
            "so a word must be letters only."
        '
        'GridWords
        '
        GridWords.AllowUserToAddRows = False
        GridWords.AllowUserToResizeRows = False
        GridWords.BackgroundColor = SystemColors.Window
        GridWords.Columns.AddRange(New DataGridViewColumn() {ColCategory, ColWord, ColWordStatus})
        GridWords.Dock = DockStyle.Fill
        GridWords.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
        GridWords.Name = "GridWords"
        GridWords.RowHeadersWidth = 24
        GridWords.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        GridWords.TabIndex = 1
        '
        'ColCategory
        '
        ColCategory.HeaderText = "Category"
        ColCategory.Name = "ColCategory"
        ColCategory.Width = 140
        '
        'ColWord
        '
        ColWord.HeaderText = "Word"
        ColWord.Name = "ColWord"
        ColWord.Width = 180
        '
        'ColWordStatus
        '
        ColWordStatus.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        ColWordStatus.HeaderText = "Status"
        ColWordStatus.Name = "ColWordStatus"
        ColWordStatus.ReadOnly = True
        '
        'WordButtons
        '
        WordButtons.AutoSize = True
        WordButtons.Controls.Add(ButtonAddWord)
        WordButtons.Controls.Add(ButtonRemoveWord)
        WordButtons.Dock = DockStyle.Fill
        WordButtons.Name = "WordButtons"
        WordButtons.TabIndex = 2
        '
        'ButtonAddWord
        '
        ButtonAddWord.AutoSize = True
        ButtonAddWord.Name = "ButtonAddWord"
        ButtonAddWord.TabIndex = 0
        ButtonAddWord.Text = "Add word"
        ButtonAddWord.UseVisualStyleBackColor = True
        '
        'ButtonRemoveWord
        '
        ButtonRemoveWord.AutoSize = True
        ButtonRemoveWord.Name = "ButtonRemoveWord"
        ButtonRemoveWord.TabIndex = 1
        ButtonRemoveWord.Text = "Remove word"
        ButtonRemoveWord.UseVisualStyleBackColor = True
        '
        'LabelProblems
        '
        LabelProblems.AutoSize = True
        LabelProblems.Dock = DockStyle.Fill
        LabelProblems.ForeColor = Color.DarkRed
        LabelProblems.Margin = New Padding(3, 6, 3, 6)
        LabelProblems.Name = "LabelProblems"
        LabelProblems.TabIndex = 1
        '
        'BottomBar
        '
        BottomBar.AutoSize = True
        BottomBar.ColumnCount = 4
        BottomBar.ColumnStyles.Add(New ColumnStyle())
        BottomBar.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        BottomBar.ColumnStyles.Add(New ColumnStyle())
        BottomBar.ColumnStyles.Add(New ColumnStyle())
        BottomBar.Controls.Add(ButtonRestore, 0, 0)
        BottomBar.Controls.Add(ButtonSave, 2, 0)
        BottomBar.Controls.Add(ButtonCancel, 3, 0)
        BottomBar.Dock = DockStyle.Fill
        BottomBar.Name = "BottomBar"
        BottomBar.RowCount = 1
        BottomBar.RowStyles.Add(New RowStyle())
        BottomBar.TabIndex = 2
        '
        'ButtonRestore
        '
        ButtonRestore.AutoSize = True
        ButtonRestore.Name = "ButtonRestore"
        ButtonRestore.Padding = New Padding(6, 2, 6, 2)
        ButtonRestore.TabIndex = 0
        ButtonRestore.Text = "Restore defaults…"
        ToolTips.SetToolTip(ButtonRestore, "Drop all your rule changes (they are moved to UserData\Backup).")
        ButtonRestore.UseVisualStyleBackColor = True
        '
        'ButtonSave
        '
        ButtonSave.AutoSize = True
        ButtonSave.Name = "ButtonSave"
        ButtonSave.Padding = New Padding(12, 2, 12, 2)
        ButtonSave.TabIndex = 1
        ButtonSave.Text = "Save"
        ButtonSave.UseVisualStyleBackColor = True
        '
        'ButtonCancel
        '
        ButtonCancel.AutoSize = True
        ButtonCancel.DialogResult = DialogResult.Cancel
        ButtonCancel.Name = "ButtonCancel"
        ButtonCancel.Padding = New Padding(8, 2, 8, 2)
        ButtonCancel.TabIndex = 2
        ButtonCancel.Text = "Cancel"
        ButtonCancel.UseVisualStyleBackColor = True
        '
        'RulesDialog
        '
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        CancelButton = ButtonCancel
        ClientSize = New Size(960, 720)
        Controls.Add(LayoutMain)
        MinimizeBox = False
        MinimumSize = New Size(700, 500)
        Name = "RulesDialog"
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        Text = "Rules"
        LayoutMain.ResumeLayout(False)
        LayoutMain.PerformLayout()
        Tabs.ResumeLayout(False)
        TabFolderRules.ResumeLayout(False)
        FoldersLayout.ResumeLayout(False)
        FoldersLayout.PerformLayout()
        CType(GridRules, System.ComponentModel.ISupportInitialize).EndInit()
        FolderButtons.ResumeLayout(False)
        FolderButtons.PerformLayout()
        GroupConflicts.ResumeLayout(False)
        GroupConflicts.PerformLayout()
        ConflictsLayout.ResumeLayout(False)
        ConflictsLayout.PerformLayout()
        ConflictButtons.ResumeLayout(False)
        ConflictButtons.PerformLayout()
        TabTypeRules.ResumeLayout(False)
        TypesLayout.ResumeLayout(False)
        TypesLayout.PerformLayout()
        CType(GridTypes, System.ComponentModel.ISupportInitialize).EndInit()
        TypeButtons.ResumeLayout(False)
        TypeButtons.PerformLayout()
        TabWords.ResumeLayout(False)
        WordsLayout.ResumeLayout(False)
        WordsLayout.PerformLayout()
        CType(GridWords, System.ComponentModel.ISupportInitialize).EndInit()
        WordButtons.ResumeLayout(False)
        WordButtons.PerformLayout()
        BottomBar.ResumeLayout(False)
        BottomBar.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents LayoutMain As TableLayoutPanel
    Friend WithEvents Tabs As TabControl
    Friend WithEvents TabFolderRules As TabPage
    Friend WithEvents FoldersLayout As TableLayoutPanel
    Friend WithEvents LabelFoldersHelp As Label
    Friend WithEvents GridRules As DataGridView
    Friend WithEvents ColFolder As DataGridViewTextBoxColumn
    Friend WithEvents ColDecision As DataGridViewComboBoxColumn
    Friend WithEvents ColReason As DataGridViewTextBoxColumn
    Friend WithEvents ColMatches As DataGridViewTextBoxColumn
    Friend WithEvents ColStatus As DataGridViewTextBoxColumn
    Friend WithEvents FolderButtons As FlowLayoutPanel
    Friend WithEvents ButtonAddRule As Button
    Friend WithEvents ButtonRemoveRule As Button
    Friend WithEvents GroupConflicts As GroupBox
    Friend WithEvents ConflictsLayout As TableLayoutPanel
    Friend WithEvents ListConflicts As ListBox
    Friend WithEvents ConflictButtons As FlowLayoutPanel
    Friend WithEvents ButtonKeepMine As Button
    Friend WithEvents ButtonUseApps As Button
    Friend WithEvents TabTypeRules As TabPage
    Friend WithEvents TypesLayout As TableLayoutPanel
    Friend WithEvents LabelTypesHelp As Label
    Friend WithEvents GridTypes As DataGridView
    Friend WithEvents ColType As DataGridViewComboBoxColumn
    Friend WithEvents ColTypeDecision As DataGridViewComboBoxColumn
    Friend WithEvents ColTypeReason As DataGridViewTextBoxColumn
    Friend WithEvents ColTypeMatches As DataGridViewTextBoxColumn
    Friend WithEvents ColTypeStatus As DataGridViewTextBoxColumn
    Friend WithEvents TypeButtons As FlowLayoutPanel
    Friend WithEvents ButtonAddType As Button
    Friend WithEvents ButtonRemoveType As Button
    Friend WithEvents TabWords As TabPage
    Friend WithEvents WordsLayout As TableLayoutPanel
    Friend WithEvents LabelWordsHelp As Label
    Friend WithEvents GridWords As DataGridView
    Friend WithEvents ColCategory As DataGridViewComboBoxColumn
    Friend WithEvents ColWord As DataGridViewTextBoxColumn
    Friend WithEvents ColWordStatus As DataGridViewTextBoxColumn
    Friend WithEvents WordButtons As FlowLayoutPanel
    Friend WithEvents ButtonAddWord As Button
    Friend WithEvents ButtonRemoveWord As Button
    Friend WithEvents LabelProblems As Label
    Friend WithEvents BottomBar As TableLayoutPanel
    Friend WithEvents ButtonRestore As Button
    Friend WithEvents ButtonSave As Button
    Friend WithEvents ButtonCancel As Button
    Friend WithEvents ToolTips As ToolTip
End Class

End Namespace
