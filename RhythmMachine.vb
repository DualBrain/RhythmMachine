'  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
'  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
'  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
'  PURPOSE.
'
'  You can use this code however you see fit.  If you want to give me some
'  credit, than, by all means, your welcome to do so, but it is not necessary.
'
'  web:    http://www.addressof.com
'  email:  corysmith@addressof.com
' 
'  This code is based on the C# example provided by Ianier Munoz, you can find
'  the original C# example or contact him at:
' 
'  web:    http://www.chronotron.com 
'  email:  ianier@hotmail.com

Option Explicit On 
Option Strict On

Imports System
Imports System.Windows.Forms
Imports System.Drawing

Imports Chronotron.AudioPlayer
Imports Chronotron.Rythm
Imports MixerImpl = Chronotron.Rythm.Mixer

Namespace Chronotron.RythmMachineApp

  Public Class RythmMachineApp
    Implements IDisposable

    Private Const TrackLength As Integer = 16

    Private _timer As Timer
    'Private _progress As ProgressBar
    Private _beatProgress As Chronotron.UI.BeatControl

    Public Mixer As MixerImpl

    Public Sub New(ByVal control As Control, ByVal player As IAudioPlayer)

      Dim measuresPerBeat As Integer = 2
      Dim resType As Type = control.GetType()

      Mixer = New MixerImpl(player, measuresPerBeat)
      Mixer.Add(New Track("Bass drum", New Patch(resType, "bass.wav"), TrackLength))
      Mixer.Add(New Track("Snare drum", New Patch(resType, "snare.wav"), TrackLength))
      Mixer.Add(New Track("Closed hat", New Patch(resType, "closed.wav"), TrackLength))
      Mixer.Add(New Track("Open hat", New Patch(resType, "open.wav"), TrackLength))
      Mixer.Add(New Track("Toc", New Patch(resType, "rim.wav"), TrackLength))

      ' Init with any preset
      Mixer("Bass drum").Init(New Byte() {1, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0})
      Mixer("Snare drum").Init(New Byte() {0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0})
      Mixer("Closed hat").Init(New Byte() {1, 1, 0, 1, 1, 1, 0, 0, 1, 1, 0, 1, 1, 1, 0, 0})
      Mixer("Open hat").Init(New Byte() {0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1})

      BuildUI(control)

      _timer = New Timer
      _timer.Interval = 50 '100 '250
      AddHandler _timer.Tick, AddressOf _timer_Tick
      _timer.Enabled = True

    End Sub

    Protected Overloads Overrides Sub Finalize()
      Dispose()
      MyBase.Finalize()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
      If Not (_timer Is Nothing) Then
        _timer.Dispose()
      End If
      If Not (Mixer Is Nothing) Then
        Mixer.Dispose()
      End If
      GC.SuppressFinalize(Me)
    End Sub

    Private Sub BuildUI(ByVal control As Control)

      Dim x As Integer = 10
      Dim dy As Integer = 10

      Dim barWidth As Integer = TrackLength * 12 + 1

      '' configure the progressbar.
      '_progress = New ProgressBar
      'AddHandler _progress.Disposed, AddressOf _progress_Disposed
      '_progress.Location = New Point(x, dy)
      '_progress.Size = New Size(barWidth, 8)
      '_progress.Parent = control
      '_progress.Minimum = 0
      '_progress.Maximum = TrackLength

      'dy += _progress.Height + 10

      ' the new progressbar.

      _beatProgress = New Chronotron.UI.BeatControl
      AddHandler _beatProgress.Disposed, AddressOf _progress_Disposed
      _beatProgress.Text = "Mixed"
      _beatProgress.Count = Mixer(1).Length
      _beatProgress.Location = New Point(x, dy)
      _beatProgress.Size = New Size(barWidth, 12)
      _beatProgress.ForeColor = Color.Blue
      _beatProgress.BackColor = Color.Silver
      ' load current selection
      For j As Integer = 1 To Mixer(1).Length ' number of ticks.
        _beatProgress(j) = False 'Mixer(1).Item(j)
      Next
      'AddHandler _beatProgress.BeatControlSelect, AddressOf b_BeatControlSelect
      _beatProgress.Parent = control

      dy += _beatProgress.Height + 10

      ' Create the beat controls

      For i As Integer = 1 To Mixer.Count

        Dim t As Track = Mixer(i)

        ' Create the label for this beat control.

        Dim l As New Label
        l.Text = t.Name
        l.Location = New Point(x, dy)
        l.Parent = control

        dy += l.Height

        ' Create the actual beat control.

        Dim beat As New Chronotron.UI.BeatControl
        beat.Text = t.Name
        beat.Count = t.Length ' 1 based.
        beat.Location = New Point(x, dy)
        beat.Size = New Size(barWidth, 12)
        beat.ForeColor = Color.LimeGreen
        beat.BackColor = Color.Silver
        ' load current selection
        For j As Integer = 1 To t.Length ' number of ticks.
          beat(j) = t(j)
        Next
        AddHandler beat.BeatControlSelect, AddressOf b_BeatControlSelect
        beat.Parent = control

        dy += beat.Height + 10

      Next

    End Sub

    Private ReadOnly Property CurrentTick() As Integer
      Get
        Return Mixer.CurrentTick Mod TrackLength
      End Get
    End Property

    Private Sub b_BeatControlSelect(ByVal sender As Object, ByVal e As Chronotron.UI.SelectEventArgs)
      Dim c As Chronotron.UI.BeatControl = CType(sender, Chronotron.UI.BeatControl)
      If Not Mixer(c.Text) Is Nothing Then
        Mixer(c.Text)(e.Index) = e.State
      End If
    End Sub

    Private Sub _timer_Tick(ByVal sender As Object, ByVal e As EventArgs)
      'If Not (_progress Is Nothing) Then
      If Not _beatProgress Is Nothing Then
        '_progress.Value = CurrentTick

        If Not _beatProgress.Item(CurrentTick + 1) Then
          For index As Integer = 1 To 16
            _beatProgress.Item(index) = False
          Next
          _beatProgress.Item(CurrentTick + 1) = True
        End If

      End If
    End Sub

    Private Sub _progress_Disposed(ByVal sender As Object, ByVal e As EventArgs)
      _timer.Enabled = False
      '_progress = Nothing
      _beatProgress = Nothing
    End Sub

  End Class

End Namespace