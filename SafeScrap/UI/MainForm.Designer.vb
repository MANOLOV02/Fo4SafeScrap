Namespace UI

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainForm
    Inherits FO4_Base_Library.IconFormBase

    'Form overrides dispose to clean up the component list.
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

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        components = New System.ComponentModel.Container()
        LayoutMain = New TableLayoutPanel()
        ActionBar = New FlowLayoutPanel()
        ButtonSave = New Button()
        ButtonClear = New Button()
        ButtonRules = New Button()
        ButtonHowItWorks = New Button()
        ButtonExport = New Button()
        ButtonRemove = New Button()
        LabelHint = New Label()
        SplitMain = New SplitContainer()
        LeftLayout = New TableLayoutPanel()
        FilterRow = New FlowLayoutPanel()
        LabelSearch = New Label()
        TextSearch = New TextBox()
        LabelShow = New Label()
        ComboShow = New ComboBox()
        LabelType = New Label()
        ComboType = New ComboBox()
        LabelCount = New Label()
        WorkshopRow = New FlowLayoutPanel()
        LabelWorkshop = New Label()
        ComboWorkshop = New ComboBox()
        ButtonWorkshops = New Button()
        ProgressWorkshops = New ProgressBar()
        LabelWorkshopStatus = New Label()
        Tabs = New TabControl()
        TabObjects = New TabPage()
        GridObjects = New DecisionGrid()
        TabGroups = New TabPage()
        GridGroups = New DecisionGrid()
        TabFolders = New TabPage()
        GridFolders = New DecisionGrid()
        SplitRight = New SplitContainer()
        PreviewHostPanel = New Panel()
        LabelPreview = New Label()
        GroupWhy = New GroupBox()
        WhyLayout = New TableLayoutPanel()
        TextWhy = New TextBox()
        ListMembers = New ListView()
        ColumnMemberResult = New ColumnHeader()
        ColumnMemberName = New ColumnHeader()
        ColumnMemberEditorID = New ColumnHeader()
        ColumnMemberKey = New ColumnHeader()
        StatusBar = New StatusStrip()
        StatusTotals = New ToolStripStatusLabel()
        StatusDecisions = New ToolStripStatusLabel()
        StatusDirty = New ToolStripStatusLabel()
        StatusLoadOrder = New ToolStripStatusLabel()
        ToolTips = New ToolTip(components)
        SearchTimer = New Timer(components)
        PreviewTimer = New Timer(components)
        LayoutMain.SuspendLayout()
        ActionBar.SuspendLayout()
        CType(SplitMain, System.ComponentModel.ISupportInitialize).BeginInit()
        SplitMain.Panel1.SuspendLayout()
        SplitMain.Panel2.SuspendLayout()
        SplitMain.SuspendLayout()
        LeftLayout.SuspendLayout()
        FilterRow.SuspendLayout()
        Tabs.SuspendLayout()
        TabObjects.SuspendLayout()
        CType(GridObjects, System.ComponentModel.ISupportInitialize).BeginInit()
        TabGroups.SuspendLayout()
        CType(GridGroups, System.ComponentModel.ISupportInitialize).BeginInit()
        TabFolders.SuspendLayout()
        CType(GridFolders, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(SplitRight, System.ComponentModel.ISupportInitialize).BeginInit()
        SplitRight.Panel1.SuspendLayout()
        SplitRight.Panel2.SuspendLayout()
        SplitRight.SuspendLayout()
        PreviewHostPanel.SuspendLayout()
        GroupWhy.SuspendLayout()
        WhyLayout.SuspendLayout()
        StatusBar.SuspendLayout()
        SuspendLayout()
        '
        'LayoutMain
        '
        LayoutMain.ColumnCount = 1
        LayoutMain.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        LayoutMain.Controls.Add(ActionBar, 0, 0)
        LayoutMain.Controls.Add(SplitMain, 0, 1)
        LayoutMain.Dock = DockStyle.Fill
        LayoutMain.Location = New Point(0, 0)
        LayoutMain.Name = "LayoutMain"
        LayoutMain.Padding = New Padding(6)
        LayoutMain.RowCount = 2
        LayoutMain.RowStyles.Add(New RowStyle())
        LayoutMain.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        LayoutMain.Size = New Size(1400, 838)
        LayoutMain.TabIndex = 0
        '
        'ActionBar
        '
        ActionBar.AutoSize = True
        ActionBar.AutoSizeMode = AutoSizeMode.GrowAndShrink
        ActionBar.Controls.Add(ButtonSave)
        ActionBar.Controls.Add(ButtonClear)
        ActionBar.Controls.Add(ButtonRules)
        ActionBar.Controls.Add(ButtonHowItWorks)
        ActionBar.Controls.Add(ButtonExport)
        ActionBar.Controls.Add(ButtonRemove)
        ActionBar.Controls.Add(LabelHint)
        ActionBar.Dock = DockStyle.Fill
        ActionBar.Location = New Point(9, 9)
        ActionBar.Margin = New Padding(3, 3, 3, 6)
        ActionBar.Name = "ActionBar"
        ActionBar.Size = New Size(1382, 33)
        ActionBar.TabIndex = 0
        ActionBar.WrapContents = False
        '
        'ButtonSave
        '
        ButtonSave.AutoSize = True
        ButtonSave.Name = "ButtonSave"
        ButtonSave.Padding = New Padding(8, 2, 8, 2)
        ButtonSave.TabIndex = 0
        ButtonSave.Text = "Save"
        ToolTips.SetToolTip(ButtonSave, "Save your decisions (Ctrl+S).")
        ButtonSave.UseVisualStyleBackColor = True
        '
        'ButtonClear
        '
        ButtonClear.AutoSize = True
        ButtonClear.Name = "ButtonClear"
        ButtonClear.Padding = New Padding(8, 2, 8, 2)
        ButtonClear.TabIndex = 7
        ButtonClear.Text = "Clear decisions…"
        ToolTips.SetToolTip(ButtonClear, "Set every object, group and folder back to Undecided (the rules decide). Your rules are kept.")
        ButtonClear.UseVisualStyleBackColor = True
        '
        'ButtonRules
        '
        ButtonRules.AutoSize = True
        ButtonRules.Name = "ButtonRules"
        ButtonRules.Padding = New Padding(8, 2, 8, 2)
        ButtonRules.TabIndex = 2
        ButtonRules.Text = "Rules…"
        ToolTips.SetToolTip(ButtonRules, "Edit the folder rules and the warning words.")
        ButtonRules.UseVisualStyleBackColor = True
        '
        'ButtonHowItWorks
        '
        ButtonHowItWorks.AutoSize = True
        ButtonHowItWorks.Name = "ButtonHowItWorks"
        ButtonHowItWorks.Padding = New Padding(8, 2, 8, 2)
        ButtonHowItWorks.TabIndex = 3
        ButtonHowItWorks.Text = "How it works…"
        ToolTips.SetToolTip(ButtonHowItWorks, "How each result comes out and what to do.")
        ButtonHowItWorks.UseVisualStyleBackColor = True
        '
        'ButtonExport
        '
        ButtonExport.AutoSize = True
        ButtonExport.Name = "ButtonExport"
        ButtonExport.Padding = New Padding(8, 2, 8, 2)
        ButtonExport.TabIndex = 4
        ButtonExport.Text = "Export to game…"
        ToolTips.SetToolTip(ButtonExport, "Save your decisions and write the list (and the F4SE plugin) to Data\F4SE\Plugins in your game folder.")
        ButtonExport.UseVisualStyleBackColor = True
        '
        'ButtonRemove
        '
        ButtonRemove.AutoSize = True
        ButtonRemove.Name = "ButtonRemove"
        ButtonRemove.Padding = New Padding(8, 2, 8, 2)
        ButtonRemove.TabIndex = 5
        ButtonRemove.Text = "Remove from game…"
        ToolTips.SetToolTip(ButtonRemove, "Delete SafeScrap.txt and the F4SE plugin from Data\F4SE\Plugins in your game folder.")
        ButtonRemove.UseVisualStyleBackColor = True
        '
        'LabelHint
        '
        LabelHint.Anchor = AnchorStyles.Left
        LabelHint.AutoSize = True
        LabelHint.ForeColor = SystemColors.GrayText
        LabelHint.Margin = New Padding(16, 0, 3, 0)
        LabelHint.Name = "LabelHint"
        LabelHint.TabIndex = 6
        LabelHint.Text = "Decide the selected rows: click the Decision box, or press Y (yes), N (no), U (undecided), Space (next)."
        '
        'SplitMain
        '
        SplitMain.Dock = DockStyle.Fill
        SplitMain.Location = New Point(9, 51)
        SplitMain.Name = "SplitMain"
        '
        'SplitMain.Panel1
        '
        SplitMain.Panel1.Controls.Add(LeftLayout)
        '
        'SplitMain.Panel2
        '
        SplitMain.Panel2.Controls.Add(SplitRight)
        SplitMain.Size = New Size(1382, 778)
        SplitMain.SplitterDistance = 820
        SplitMain.TabIndex = 1
        '
        'LeftLayout
        '
        LeftLayout.ColumnCount = 1
        LeftLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        LeftLayout.Controls.Add(FilterRow, 0, 0)
        LeftLayout.Controls.Add(WorkshopRow, 0, 1)
        LeftLayout.Controls.Add(Tabs, 0, 2)
        LeftLayout.Dock = DockStyle.Fill
        LeftLayout.Location = New Point(0, 0)
        LeftLayout.Name = "LeftLayout"
        LeftLayout.RowCount = 3
        LeftLayout.RowStyles.Add(New RowStyle())
        LeftLayout.RowStyles.Add(New RowStyle())
        LeftLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        LeftLayout.Size = New Size(820, 778)
        LeftLayout.TabIndex = 0
        '
        'FilterRow
        '
        FilterRow.AutoSize = True
        FilterRow.AutoSizeMode = AutoSizeMode.GrowAndShrink
        FilterRow.Controls.Add(LabelSearch)
        FilterRow.Controls.Add(TextSearch)
        FilterRow.Controls.Add(LabelShow)
        FilterRow.Controls.Add(ComboShow)
        FilterRow.Controls.Add(LabelType)
        FilterRow.Controls.Add(ComboType)
        FilterRow.Controls.Add(LabelCount)
        FilterRow.Dock = DockStyle.Fill
        FilterRow.Name = "FilterRow"
        FilterRow.TabIndex = 0
        FilterRow.WrapContents = False
        '
        'LabelSearch
        '
        LabelSearch.Anchor = AnchorStyles.Left
        LabelSearch.AutoSize = True
        LabelSearch.Name = "LabelSearch"
        LabelSearch.TabIndex = 0
        LabelSearch.Text = "Search:"
        '
        'TextSearch
        '
        TextSearch.Name = "TextSearch"
        TextSearch.Size = New Size(220, 23)
        TextSearch.TabIndex = 1
        ToolTips.SetToolTip(TextSearch, "Name, EditorID, key (Plugin|ID) or folder. Not case-sensitive.")
        '
        'LabelShow
        '
        LabelShow.Anchor = AnchorStyles.Left
        LabelShow.AutoSize = True
        LabelShow.Margin = New Padding(12, 0, 3, 0)
        LabelShow.Name = "LabelShow"
        LabelShow.TabIndex = 2
        LabelShow.Text = "Show:"
        '
        'ComboShow
        '
        ComboShow.DropDownStyle = ComboBoxStyle.DropDownList
        ComboShow.Name = "ComboShow"
        ComboShow.Size = New Size(150, 23)
        ComboShow.TabIndex = 3
        ToolTips.SetToolTip(ComboShow, "Undecided / Decided: your decision on this tab's rows. Yes / No / Review: the result (on Groups and Folders: has objects with that result). With warning: the name suggests a plant, terrain or a building part AND it matters: lowered to Review, or Yes anyway.")
        '
        'LabelType
        '
        LabelType.Anchor = AnchorStyles.Left
        LabelType.AutoSize = True
        LabelType.Margin = New Padding(12, 0, 3, 0)
        LabelType.Name = "LabelType"
        LabelType.TabIndex = 4
        LabelType.Text = "Type:"
        '
        'ComboType
        '
        ComboType.DropDownStyle = ComboBoxStyle.DropDownList
        ComboType.Name = "ComboType"
        ComboType.Size = New Size(125, 23)
        ComboType.TabIndex = 5
        ToolTips.SetToolTip(ComboType, "The record type of the objects (STAT, CONT, FURN...). On Groups and Folders: the rows that have objects of that type.")
        '
        'LabelCount
        '
        LabelCount.Anchor = AnchorStyles.Left
        LabelCount.AutoSize = True
        LabelCount.ForeColor = SystemColors.GrayText
        LabelCount.Margin = New Padding(12, 0, 3, 0)
        LabelCount.Name = "LabelCount"
        LabelCount.TabIndex = 6
        '
        'WorkshopRow
        '
        WorkshopRow.AutoSize = True
        WorkshopRow.AutoSizeMode = AutoSizeMode.GrowAndShrink
        WorkshopRow.Controls.Add(LabelWorkshop)
        WorkshopRow.Controls.Add(ComboWorkshop)
        WorkshopRow.Controls.Add(ButtonWorkshops)
        WorkshopRow.Controls.Add(ProgressWorkshops)
        WorkshopRow.Controls.Add(LabelWorkshopStatus)
        WorkshopRow.Dock = DockStyle.Fill
        WorkshopRow.Name = "WorkshopRow"
        WorkshopRow.TabIndex = 1
        WorkshopRow.WrapContents = False
        '
        'LabelWorkshop
        '
        LabelWorkshop.Anchor = AnchorStyles.Left
        LabelWorkshop.AutoSize = True
        LabelWorkshop.Name = "LabelWorkshop"
        LabelWorkshop.TabIndex = 0
        LabelWorkshop.Text = "Workshop:"
        '
        'ComboWorkshop
        '
        ComboWorkshop.DropDownStyle = ComboBoxStyle.DropDownList
        ComboWorkshop.Enabled = False
        ComboWorkshop.Name = "ComboWorkshop"
        ComboWorkshop.Size = New Size(300, 23)
        ComboWorkshop.TabIndex = 1
        ToolTips.SetToolTip(ComboWorkshop, "Only the objects placed inside this workshop's build area, as the game decides it: the area boxes linked to the workbench (or, without boxes, the build radius). " &
            "What the plugins place — your save (what you built, scrapped or moved) is not seen.")
        '
        'ButtonWorkshops
        '
        ButtonWorkshops.AutoSize = True
        ButtonWorkshops.Name = "ButtonWorkshops"
        ButtonWorkshops.TabIndex = 2
        ButtonWorkshops.Text = "Filter by workshop..."
        ButtonWorkshops.UseVisualStyleBackColor = True
        ToolTips.SetToolTip(ButtonWorkshops, "Reads the placed objects of your load order once (it takes a while) and lists the workshops. Nothing read is kept: only which objects are in each workshop.")
        '
        'ProgressWorkshops
        '
        ProgressWorkshops.Anchor = AnchorStyles.Left
        ProgressWorkshops.Name = "ProgressWorkshops"
        ProgressWorkshops.Size = New Size(140, 16)
        ProgressWorkshops.TabIndex = 3
        ProgressWorkshops.Visible = False
        '
        'LabelWorkshopStatus
        '
        LabelWorkshopStatus.Anchor = AnchorStyles.Left
        LabelWorkshopStatus.AutoSize = True
        LabelWorkshopStatus.ForeColor = SystemColors.GrayText
        LabelWorkshopStatus.Name = "LabelWorkshopStatus"
        LabelWorkshopStatus.TabIndex = 4
        '
        'Tabs
        '
        Tabs.Controls.Add(TabObjects)
        Tabs.Controls.Add(TabGroups)
        Tabs.Controls.Add(TabFolders)
        Tabs.Dock = DockStyle.Fill
        Tabs.Name = "Tabs"
        Tabs.SelectedIndex = 0
        Tabs.TabIndex = 1
        '
        'TabObjects
        '
        TabObjects.Controls.Add(GridObjects)
        TabObjects.Name = "TabObjects"
        TabObjects.Padding = New Padding(3)
        TabObjects.TabIndex = 0
        TabObjects.Text = "Objects"
        TabObjects.ToolTipText = "Every base object a recipe covers, with its result."
        TabObjects.UseVisualStyleBackColor = True
        '
        'GridObjects
        '
        GridObjects.AllowUserToAddRows = False
        GridObjects.AllowUserToDeleteRows = False
        GridObjects.AllowUserToResizeRows = False
        GridObjects.BackgroundColor = SystemColors.Window
        GridObjects.Dock = DockStyle.Fill
        GridObjects.Name = "GridObjects"
        GridObjects.ReadOnly = True
        GridObjects.RowHeadersVisible = False
        GridObjects.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        GridObjects.StandardTab = True
        GridObjects.TabIndex = 0
        GridObjects.VirtualMode = True
        '
        'TabGroups
        '
        TabGroups.Controls.Add(GridGroups)
        TabGroups.Name = "TabGroups"
        TabGroups.Padding = New Padding(3)
        TabGroups.TabIndex = 1
        TabGroups.Text = "Groups"
        TabGroups.ToolTipText = "The recipes (scrap and build): a decision here applies to all their objects."
        TabGroups.UseVisualStyleBackColor = True
        '
        'GridGroups
        '
        GridGroups.AllowUserToAddRows = False
        GridGroups.AllowUserToDeleteRows = False
        GridGroups.AllowUserToResizeRows = False
        GridGroups.BackgroundColor = SystemColors.Window
        GridGroups.Dock = DockStyle.Fill
        GridGroups.Name = "GridGroups"
        GridGroups.ReadOnly = True
        GridGroups.RowHeadersVisible = False
        GridGroups.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        GridGroups.StandardTab = True
        GridGroups.TabIndex = 0
        GridGroups.VirtualMode = True
        '
        'TabFolders
        '
        TabFolders.Controls.Add(GridFolders)
        TabFolders.Name = "TabFolders"
        TabFolders.Padding = New Padding(3)
        TabFolders.TabIndex = 2
        TabFolders.Text = "Folders"
        TabFolders.ToolTipText = "The model folders: a decision here applies to the objects filed there."
        TabFolders.UseVisualStyleBackColor = True
        '
        'GridFolders
        '
        GridFolders.AllowUserToAddRows = False
        GridFolders.AllowUserToDeleteRows = False
        GridFolders.AllowUserToResizeRows = False
        GridFolders.BackgroundColor = SystemColors.Window
        GridFolders.Dock = DockStyle.Fill
        GridFolders.Name = "GridFolders"
        GridFolders.ReadOnly = True
        GridFolders.RowHeadersVisible = False
        GridFolders.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        GridFolders.StandardTab = True
        GridFolders.TabIndex = 0
        GridFolders.VirtualMode = True
        '
        'SplitRight
        '
        SplitRight.Dock = DockStyle.Fill
        SplitRight.Name = "SplitRight"
        SplitRight.Orientation = Orientation.Horizontal
        '
        'SplitRight.Panel1
        '
        SplitRight.Panel1.Controls.Add(PreviewHostPanel)
        '
        'SplitRight.Panel2
        '
        SplitRight.Panel2.Controls.Add(GroupWhy)
        SplitRight.Size = New Size(558, 778)
        SplitRight.SplitterDistance = 400
        SplitRight.TabIndex = 0
        '
        'PreviewHostPanel
        '
        PreviewHostPanel.BackColor = Color.FromArgb(40, 40, 44)
        PreviewHostPanel.BorderStyle = BorderStyle.FixedSingle
        PreviewHostPanel.Controls.Add(LabelPreview)
        PreviewHostPanel.Dock = DockStyle.Fill
        PreviewHostPanel.Name = "PreviewHostPanel"
        PreviewHostPanel.TabIndex = 0
        '
        'LabelPreview
        '
        LabelPreview.Dock = DockStyle.Fill
        LabelPreview.ForeColor = Color.Gainsboro
        LabelPreview.Name = "LabelPreview"
        LabelPreview.Padding = New Padding(12)
        LabelPreview.TabIndex = 0
        LabelPreview.Text = "Select an object to preview it."
        LabelPreview.TextAlign = ContentAlignment.MiddleCenter
        '
        'GroupWhy
        '
        GroupWhy.Controls.Add(WhyLayout)
        GroupWhy.Dock = DockStyle.Fill
        GroupWhy.Name = "GroupWhy"
        GroupWhy.Padding = New Padding(6)
        GroupWhy.TabIndex = 0
        GroupWhy.TabStop = False
        GroupWhy.Text = "Why this result"
        '
        'WhyLayout
        '
        WhyLayout.ColumnCount = 1
        WhyLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        WhyLayout.Controls.Add(TextWhy, 0, 0)
        WhyLayout.Controls.Add(ListMembers, 0, 1)
        WhyLayout.Dock = DockStyle.Fill
        WhyLayout.Name = "WhyLayout"
        WhyLayout.RowCount = 2
        WhyLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 55.0F))
        WhyLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 45.0F))
        WhyLayout.TabIndex = 0
        '
        'TextWhy
        '
        TextWhy.BackColor = SystemColors.Window
        TextWhy.BorderStyle = BorderStyle.None
        TextWhy.Dock = DockStyle.Fill
        TextWhy.Multiline = True
        TextWhy.Name = "TextWhy"
        TextWhy.ReadOnly = True
        TextWhy.ScrollBars = ScrollBars.Vertical
        TextWhy.TabIndex = 0
        '
        'ListMembers
        '
        ListMembers.Columns.AddRange(New ColumnHeader() {ColumnMemberResult, ColumnMemberName, ColumnMemberEditorID, ColumnMemberKey})
        ListMembers.Dock = DockStyle.Fill
        ListMembers.FullRowSelect = True
        ListMembers.HideSelection = False
        ListMembers.MultiSelect = False
        ListMembers.Name = "ListMembers"
        ListMembers.TabIndex = 1
        ToolTips.SetToolTip(ListMembers, "Select to preview; double-click to open the object on the Objects tab.")
        ListMembers.UseCompatibleStateImageBehavior = False
        ListMembers.View = View.Details
        '
        'ColumnMemberResult
        '
        ColumnMemberResult.Text = "Result"
        ColumnMemberResult.Width = 60
        '
        'ColumnMemberName
        '
        ColumnMemberName.Text = "Name"
        ColumnMemberName.Width = 160
        '
        'ColumnMemberEditorID
        '
        ColumnMemberEditorID.Text = "EditorID"
        ColumnMemberEditorID.Width = 180
        '
        'ColumnMemberKey
        '
        ColumnMemberKey.Text = "Key"
        ColumnMemberKey.Width = 150
        '
        'StatusBar
        '
        StatusBar.Items.AddRange(New ToolStripItem() {StatusTotals, StatusDecisions, StatusDirty, StatusLoadOrder})
        StatusBar.Name = "StatusBar"
        StatusBar.ShowItemToolTips = True
        StatusBar.TabIndex = 1
        '
        'StatusTotals
        '
        StatusTotals.Name = "StatusTotals"
        StatusTotals.ToolTipText = "Results of all objects."
        '
        'StatusDecisions
        '
        StatusDecisions.BorderSides = ToolStripStatusLabelBorderSides.Left
        StatusDecisions.Name = "StatusDecisions"
        StatusDecisions.ToolTipText = "Your decisions on objects, groups and folders."
        '
        'StatusDirty
        '
        StatusDirty.BorderSides = ToolStripStatusLabelBorderSides.Left
        StatusDirty.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        StatusDirty.ForeColor = Color.DarkRed
        StatusDirty.Name = "StatusDirty"
        StatusDirty.Text = "Unsaved changes"
        StatusDirty.Visible = False
        '
        'StatusLoadOrder
        '
        StatusLoadOrder.BorderSides = ToolStripStatusLabelBorderSides.Left
        StatusLoadOrder.Name = "StatusLoadOrder"
        StatusLoadOrder.Spring = True
        StatusLoadOrder.TextAlign = ContentAlignment.MiddleLeft
        '
        'SearchTimer
        '
        SearchTimer.Interval = 250
        '
        'PreviewTimer
        '
        PreviewTimer.Interval = 120
        '
        'MainForm
        '
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1400, 860)
        Controls.Add(LayoutMain)
        Controls.Add(StatusBar)
        KeyPreview = True
        MinimumSize = New Size(1000, 600)
        Name = "MainForm"
        StartPosition = FormStartPosition.CenterScreen
        Text = "SafeScrap"
        LayoutMain.ResumeLayout(False)
        LayoutMain.PerformLayout()
        ActionBar.ResumeLayout(False)
        ActionBar.PerformLayout()
        SplitMain.Panel1.ResumeLayout(False)
        SplitMain.Panel2.ResumeLayout(False)
        CType(SplitMain, System.ComponentModel.ISupportInitialize).EndInit()
        SplitMain.ResumeLayout(False)
        LeftLayout.ResumeLayout(False)
        LeftLayout.PerformLayout()
        FilterRow.ResumeLayout(False)
        FilterRow.PerformLayout()
        Tabs.ResumeLayout(False)
        TabObjects.ResumeLayout(False)
        CType(GridObjects, System.ComponentModel.ISupportInitialize).EndInit()
        TabGroups.ResumeLayout(False)
        CType(GridGroups, System.ComponentModel.ISupportInitialize).EndInit()
        TabFolders.ResumeLayout(False)
        CType(GridFolders, System.ComponentModel.ISupportInitialize).EndInit()
        SplitRight.Panel1.ResumeLayout(False)
        SplitRight.Panel2.ResumeLayout(False)
        CType(SplitRight, System.ComponentModel.ISupportInitialize).EndInit()
        SplitRight.ResumeLayout(False)
        PreviewHostPanel.ResumeLayout(False)
        GroupWhy.ResumeLayout(False)
        WhyLayout.ResumeLayout(False)
        WhyLayout.PerformLayout()
        StatusBar.ResumeLayout(False)
        StatusBar.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents LayoutMain As TableLayoutPanel
    Friend WithEvents ActionBar As FlowLayoutPanel
    Friend WithEvents ButtonSave As Button
    Friend WithEvents ButtonRules As Button
    Friend WithEvents ButtonHowItWorks As Button
    Friend WithEvents ButtonClear As Button
    Friend WithEvents ButtonExport As Button
    Friend WithEvents ButtonRemove As Button
    Friend WithEvents LabelHint As Label
    Friend WithEvents SplitMain As SplitContainer
    Friend WithEvents LeftLayout As TableLayoutPanel
    Friend WithEvents FilterRow As FlowLayoutPanel
    Friend WithEvents LabelSearch As Label
    Friend WithEvents TextSearch As TextBox
    Friend WithEvents LabelShow As Label
    Friend WithEvents ComboShow As ComboBox
    Friend WithEvents LabelType As Label
    Friend WithEvents ComboType As ComboBox
    Friend WithEvents LabelCount As Label
    Friend WithEvents WorkshopRow As FlowLayoutPanel
    Friend WithEvents LabelWorkshop As Label
    Friend WithEvents ComboWorkshop As ComboBox
    Friend WithEvents ButtonWorkshops As Button
    Friend WithEvents ProgressWorkshops As ProgressBar
    Friend WithEvents LabelWorkshopStatus As Label
    Friend WithEvents Tabs As TabControl
    Friend WithEvents TabObjects As TabPage
    Friend WithEvents GridObjects As DecisionGrid
    Friend WithEvents TabGroups As TabPage
    Friend WithEvents GridGroups As DecisionGrid
    Friend WithEvents TabFolders As TabPage
    Friend WithEvents GridFolders As DecisionGrid
    Friend WithEvents SplitRight As SplitContainer
    Friend WithEvents PreviewHostPanel As Panel
    Friend WithEvents LabelPreview As Label
    Friend WithEvents GroupWhy As GroupBox
    Friend WithEvents WhyLayout As TableLayoutPanel
    Friend WithEvents TextWhy As TextBox
    Friend WithEvents ListMembers As ListView
    Friend WithEvents ColumnMemberResult As ColumnHeader
    Friend WithEvents ColumnMemberName As ColumnHeader
    Friend WithEvents ColumnMemberEditorID As ColumnHeader
    Friend WithEvents ColumnMemberKey As ColumnHeader
    Friend WithEvents StatusBar As StatusStrip
    Friend WithEvents StatusTotals As ToolStripStatusLabel
    Friend WithEvents StatusDecisions As ToolStripStatusLabel
    Friend WithEvents StatusDirty As ToolStripStatusLabel
    Friend WithEvents StatusLoadOrder As ToolStripStatusLabel
    Friend WithEvents ToolTips As ToolTip
    Friend WithEvents SearchTimer As Timer
    Friend WithEvents PreviewTimer As Timer
End Class

End Namespace
