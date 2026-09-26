Namespace UI

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class HowItWorksDialog
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
        LayoutMain = New TableLayoutPanel()
        TextHelp = New RichTextBox()
        ButtonClose = New Button()
        LayoutMain.SuspendLayout()
        SuspendLayout()
        '
        'LayoutMain
        '
        LayoutMain.ColumnCount = 1
        LayoutMain.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        LayoutMain.Controls.Add(TextHelp, 0, 0)
        LayoutMain.Controls.Add(ButtonClose, 0, 1)
        LayoutMain.Dock = DockStyle.Fill
        LayoutMain.Name = "LayoutMain"
        LayoutMain.Padding = New Padding(8)
        LayoutMain.RowCount = 2
        LayoutMain.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        LayoutMain.RowStyles.Add(New RowStyle())
        LayoutMain.TabIndex = 0
        '
        'TextHelp
        '
        TextHelp.BackColor = SystemColors.Window
        TextHelp.BorderStyle = BorderStyle.None
        TextHelp.Dock = DockStyle.Fill
        TextHelp.Font = New Font("Segoe UI", 10.0F)
        TextHelp.Name = "TextHelp"
        TextHelp.ReadOnly = True
        TextHelp.TabIndex = 0
        '
        'ButtonClose
        '
        ButtonClose.Anchor = AnchorStyles.Right
        ButtonClose.AutoSize = True
        ButtonClose.DialogResult = DialogResult.Cancel
        ButtonClose.Name = "ButtonClose"
        ButtonClose.Padding = New Padding(12, 2, 12, 2)
        ButtonClose.TabIndex = 1
        ButtonClose.Text = "Close"
        ButtonClose.UseVisualStyleBackColor = True
        '
        'HowItWorksDialog
        '
        AcceptButton = ButtonClose
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        CancelButton = ButtonClose
        ClientSize = New Size(760, 700)
        Controls.Add(LayoutMain)
        MinimizeBox = False
        MinimumSize = New Size(500, 400)
        Name = "HowItWorksDialog"
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        Text = "How SafeScrap works"
        LayoutMain.ResumeLayout(False)
        LayoutMain.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents LayoutMain As TableLayoutPanel
    Friend WithEvents TextHelp As RichTextBox
    Friend WithEvents ButtonClose As Button
End Class

End Namespace
