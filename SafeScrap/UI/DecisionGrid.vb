Option Strict On
Option Infer On

Imports SafeScrap.Engine

Namespace UI

    ''' <summary>The review lists: a virtual-mode grid whose Decision column (a read-only three-state checkbox) is decided
    ''' by the form, not by the grid.
    ''' <para>A plain left click on the Decision box of a row that is ALREADY selected must not collapse the selection to
    ''' that row (the decision applies to every selected row, rev-42): the grid's own mouse-down (which reselects) is
    ''' skipped for that click. A click on an unselected row selects it the normal way first. Either way
    ''' <see cref="DecisionClicked"/> is raised with the clicked row.</para></summary>
    Friend Class DecisionGrid
        Inherits DataGridView

        Friend Event DecisionClicked(sender As Object, rowIndex As Integer)

        Friend Sub New()
            DoubleBuffered = True
        End Sub

        Protected Overrides Sub OnCellMouseDown(e As DataGridViewCellMouseEventArgs)
            If e.Button = MouseButtons.Left AndAlso ModifierKeys = Keys.None AndAlso e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 AndAlso
               Columns(e.ColumnIndex).Name = ReviewSession.ColDecision Then
                If Not Rows(e.RowIndex).Selected Then MyBase.OnCellMouseDown(e)
                RaiseEvent DecisionClicked(Me, e.RowIndex)
                Return
            End If
            MyBase.OnCellMouseDown(e)
        End Sub

    End Class

End Namespace
