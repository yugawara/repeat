Public Class Form1
    Private isWarningIcon As Boolean = False ' A flag to track the current icon

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        ' Toggle the icon based on the current icon
        If isWarningIcon Then
            Me.Icon = System.Drawing.SystemIcons.Shield
        Else
            Me.Icon = System.Drawing.SystemIcons.Warning
        End If

        ' Toggle the flag
        isWarningIcon = Not isWarningIcon
    End Sub
End Class
