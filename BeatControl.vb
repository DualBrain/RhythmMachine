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

Namespace Chronotron.UI

  Public Class SelectEventArgs
    Inherits EventArgs

    Public Index As Integer
    Public State As Boolean
    Public Cancel As Boolean

    Public Sub New(ByVal index As Integer, ByVal state As Boolean)
      Me.Index = index
      Me.State = state
    End Sub

  End Class

  Public Delegate Sub ControlSelectEvent(ByVal sender As Object, ByVal e As SelectEventArgs)

  ''' <summary>
  ''' This class implements a simple UI control for selefting beat patterns
  ''' </summary>
  Public Class BeatControl
    Inherits Control

    Private _ticks() As Boolean

    Public Event BeatControlSelect As ControlSelectEvent

    Public Sub New()
      Count = 8
    End Sub

    Protected Overrides Sub OnPaintBackground(ByVal e As PaintEventArgs)
    End Sub

    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
      For index As Integer = 1 To _ticks.Length
        PaintTick(index, e.Graphics)
      Next
    End Sub

    Protected Overrides Sub OnMouseDown(ByVal e As MouseEventArgs)
      If e.Button = MouseButtons.Left Then
        Dim index As Integer = ((_ticks.Length * e.X) \ Me.Width) + 1
        If index > 0 AndAlso index < _ticks.Length + 1 Then
          Me(index) = Not Me(index)
        End If
      End If
      MyBase.OnMouseDown(e)
    End Sub

    Public Property Count() As Integer
      Get
        ' .Length is base 0, add 1.
        Return _ticks.Length + 1
      End Get
      Set(ByVal Value As Integer)
        ' passed in as base 1
        If Value > 0 And Value < 129 Then
          _ticks = New Boolean(Value - 1) {}
          Invalidate()
        Else
          Throw New ArgumentException("Count must be between 1 and 128.")
        End If
      End Set
    End Property

    Default Public Property Item(ByVal index As Integer) As Boolean
      Get
        If index > 0 AndAlso index < _ticks.Length + 1 Then
          Return _ticks(index - 1)
        Else
          Throw New ArgumentException("Index must be between 1 and " & _ticks.Length & ".")
        End If
      End Get
      Set(ByVal Value As Boolean)
        If index > 0 AndAlso index < _ticks.Length + 1 Then
          If Value <> _ticks(index - 1) Then
            Dim e As New SelectEventArgs(index, Value)
            OnBeatControlSelect(e)
            If Not e.Cancel Then
              _ticks(index - 1) = Value
              Dim g As Graphics = CreateGraphics()
              Try
                PaintTick(index, g)
              Finally
                g.Dispose()
              End Try
            End If
          End If
        Else
          Throw New ArgumentException("Index must be between 1 and " & _ticks.Length & ".")
        End If
      End Set
    End Property

    Private Sub PaintTick(ByVal index As Integer, ByVal g As Graphics)
      ' index - base 1
      If index > 0 AndAlso index < _ticks.Length + 1 Then
        Dim width As Integer = Me.Width \ _ticks.Length
        Dim r As New Rectangle((index - 1) * width, 0, width, Me.Height)
        r.Height -= 1
        If _ticks(index - 1) Then
          g.FillRectangle(New SolidBrush(Me.ForeColor), r)
        Else
          g.FillRectangle(New SolidBrush(Me.BackColor), r)
        End If
        g.DrawRectangle(New Pen(Color.Black), r)
      Else
        Throw New ArgumentException("Index must be between 1 and " & _ticks.Length & ".")
      End If
    End Sub

    Protected Sub OnBeatControlSelect(ByVal e As SelectEventArgs)
      RaiseEvent BeatControlSelect(Me, e)
    End Sub

  End Class

End Namespace