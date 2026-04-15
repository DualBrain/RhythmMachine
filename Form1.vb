Option Explicit On 
Option Strict On

Imports System
Imports System.Drawing
Imports System.Collections
Imports System.ComponentModel
Imports System.Windows.Forms
Imports System.IO

Imports Chronotron.RythmMachineApp

''' <summary>
''' Summary description for Form1.
''' </summary>
Public Class Form1
  Inherits System.Windows.Forms.Form

  Public Sub New()
    '
    ' Required for Windows Form Designer support
    '
    InitializeComponent()

    _machine = New RythmMachineApp(Me, New DSoundPlayer.StreamingPlayer(Me, 22050, 16, 1))

  End Sub

  ''' <summary>
  ''' Clean up any resources being used.
  ''' </summary>
  Protected Overloads Sub Dispose(ByVal disposing As Boolean)
    If disposing Then
      If Not (_Machine Is Nothing) Then
        _Machine.Dispose()
      End If
    End If
    MyBase.Dispose(disposing)
  End Sub

#Region "Windows Form Designer generated code"

  '/ <summary>
  '/ Required method for Designer support - do not modify
  '/ the contents of this method with the code editor.
  '/ </summary>
  Friend WithEvents cmdStartStop As System.Windows.Forms.Button
  Private Sub InitializeComponent()
    Me.mainMenu1 = New System.Windows.Forms.MainMenu
    Me.menuItem1 = New System.Windows.Forms.MenuItem
    Me.menuItem4 = New System.Windows.Forms.MenuItem
    Me.cmdStartStop = New System.Windows.Forms.Button
    Me.SuspendLayout()
    '
    'mainMenu1
    '
    Me.mainMenu1.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.menuItem1})
    '
    'menuItem1
    '
    Me.menuItem1.Index = 0
    Me.menuItem1.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.menuItem4})
    Me.menuItem1.Text = "&File"
    '
    'menuItem4
    '
    Me.menuItem4.Index = 0
    Me.menuItem4.Text = "E&xit"
    '
    'cmdStartStop
    '
    Me.cmdStartStop.FlatStyle = System.Windows.Forms.FlatStyle.System
    Me.cmdStartStop.Location = New System.Drawing.Point(140, 268)
    Me.cmdStartStop.Name = "cmdStartStop"
    Me.cmdStartStop.Size = New System.Drawing.Size(68, 24)
    Me.cmdStartStop.TabIndex = 0
    Me.cmdStartStop.Text = "&Start"
    '
    'Form1
    '
    Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
    Me.ClientSize = New System.Drawing.Size(218, 303)
    Me.Controls.Add(Me.cmdStartStop)
    Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
    Me.MaximizeBox = False
    Me.Menu = Me.mainMenu1
    Me.Name = "Form1"
    Me.Text = "Drum Machine VB.NET"
    Me.ResumeLayout(False)

  End Sub 'InitializeComponent 
#End Region

  Private mainMenu1 As System.Windows.Forms.MainMenu
  Private menuItem1 As System.Windows.Forms.MenuItem
  Private WithEvents menuItem4 As System.Windows.Forms.MenuItem

  Private _machine As RythmMachineApp

  ''' <summary>
  ''' The main entry point for the application.
  ''' </summary>
  <STAThread()> _
  Shared Sub Main()
    Application.EnableVisualStyles()
    Application.DoEvents()
    Application.Run(New Form1)
  End Sub

  Private Sub menuItem4_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles menuItem4.Click
    Close()
  End Sub

  Private Sub cmdStartStop_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdStartStop.Click
    If cmdStartStop.Text = "&Start" Then
      cmdStartStop.Text = "&Stop"
      _machine.Mixer.Play()
    Else
      cmdStartStop.Text = "&Start"
      _machine.Mixer.Stop()
    End If
  End Sub

End Class