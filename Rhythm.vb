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
Imports System.IO
Imports System.Collections
Imports Chronotron.WaveIO

Namespace Chronotron.Rythm

  ''' <summary>
  ''' This class holds the audio data for a drum patch
  ''' </summary>
  Public Class Patch

    Private _audioData() As Integer

    Public Sub New(ByVal stream As Stream)
      Dim s As New WaveStream(stream)
      Try
        If s.Format.wFormatTag <> CInt(Fix(WaveLib.WaveFormats.Pcm)) Then
          Throw New Exception("Invalid sample format")
        End If
        ' read everything into memory to speed up things (CF optimization)
        Dim data(CInt(s.Length - 1)) As Byte
        s.Read(data, 0, data.Length)
        Dim samples As Integer = CInt(s.Length \ s.Format.nBlockAlign)
        Dim stereo As Boolean = (s.Format.nChannels = 2)
        Dim eight As Boolean = (s.Format.wBitsPerSample = 8) ' assume 16 bit otherwise
        ' we store the audio data always in mono
        _audioData = New Integer(samples - 1) {}

        Dim reader As New BinaryReader(New MemoryStream(data))
        Try
          Dim position As Integer = 0
          For index As Integer = 0 To samples - 1
            If eight Then
              _audioData(position) = 256 * (reader.ReadByte() - 128)
            Else
              _audioData(position) = reader.ReadInt16()
            End If
            If stereo Then ' just add up channels to convert to mono
              If eight Then
                _audioData(position) += 256 * (reader.ReadByte() - 128)
              Else
                _audioData(position) += reader.ReadInt16()
              End If
            End If
            position += 1
          Next
        Finally
          reader.Close()
        End Try
      Finally
        s.Close() 'Dispose()
      End Try
    End Sub

    Public Sub New(ByVal fileName As String)
      MyClass.New(New FileStream(fileName, FileMode.Open))
    End Sub

    Public Sub New(ByVal type As Type, ByVal resourceName As String)
      MyClass.New(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(type, resourceName))
    End Sub

    Public Function GetReader() As PatchReader
      Return New PatchReader(_audioData)
    End Function

  End Class

  ''' <summary>
  ''' This class implements an audio data reader/mixer for a given patch
  ''' </summary>
  Public Class PatchReader

    Private _dataPosition As Integer
    Private _data() As Integer

    Friend Sub New(ByVal data() As Integer)
      _data = data
      _dataPosition = 0
    End Sub

    Public Function Mix(ByVal destination() As Integer, ByVal offset As Integer, ByVal samples As Integer) As Boolean
      Dim toget As Integer = Math.Min(samples, _data.Length - _dataPosition)
      If toget > 0 Then
        For index As Integer = offset To (offset + toget) - 1
          ' mix into destination buffer
          destination(index) += _data(_dataPosition)
          _dataPosition += 1
        Next
      End If
      Return _dataPosition < _data.Length
    End Function

  End Class

  ''' <summary>
  ''' Holds the patch and pattern for a specific drum track
  ''' </summary>
  Public Class Track

    Private _pattern() As Boolean

    Public Patch As Patch
    Public Name As String

    Public Sub New(ByVal name As String, ByVal patch As Patch, ByVal pattern() As Byte)
      MyClass.New(name, patch, pattern.Length)
      Init(pattern)
    End Sub

    Public Sub New(ByVal name As String, ByVal patch As Patch, ByVal length As Integer)
      ' length - base 1
      Me.Name = name
      If patch Is Nothing Then
        Throw New ArgumentNullException("patch")
      Else
        If length > 0 AndAlso length < Mixer.MAXTRACKLENGTH + 1 Then
        Else
          Throw New ArgumentException("Length must be between 1 and " & Mixer.MAXTRACKLENGTH & ".")
        End If
      End If
      Me.Patch = patch
      _pattern = New Boolean(length - 1) {}
    End Sub

    Public Sub Init(ByVal pattern() As Byte)
      SyncLock Me
        For index As Integer = 1 To pattern.Length
          Me(index) = (pattern(index - 1) <> 0)
        Next
      End SyncLock
    End Sub

    Public Function GetBeat(ByVal beat As Integer) As PatchReader
      SyncLock Me
        beat = beat Mod _pattern.Length
        Dim result As PatchReader = Nothing
        If _pattern(beat) Then
          result = Patch.GetReader()
        End If
        Return result
      End SyncLock
    End Function

    Public ReadOnly Property Length() As Integer
      Get
        Return _pattern.Length
      End Get
    End Property

    Default Public Property Item(ByVal index As Integer) As Boolean
      Get
        If index > 0 And index < _pattern.Length + 1 Then
          Return _pattern(index - 1)
        Else
          Throw New ArgumentException("Index must be between 1 and " & _pattern.Length & ".")
        End If
      End Get
      Set(ByVal Value As Boolean)
        SyncLock Me
          If index > 0 And index < _pattern.Length + 1 Then
            _pattern(index - 1) = Value
          Else
            Throw New ArgumentException("Index must be between 1 and " & _pattern.Length & ".")
          End If
        End SyncLock
      End Set
    End Property

  End Class

  ''' <summary>
  ''' Implements a basic rhythm machine
  ''' </summary>
  Public Class Mixer
    Implements IDisposable

    Public Const MAXTRACKLENGTH As Integer = 128

    Private _player As AudioPlayer.IAudioPlayer

    Private _BPMLock As New Object
    Private _BPM As Integer
    Private _ticksPerBeat As Integer
    Private _tick As Integer
    Private _tickPeriod As Integer
    Private _tickLeft As Integer

    Private _mixBuffer() As Integer
    Private _mixBuffer16() As Short
    Private _readers As New ArrayList

    Private _tracks As New ArrayList

    Public Sub New(ByVal player As AudioPlayer.IAudioPlayer, ByVal ticksPerBeat As Integer)
      If player Is Nothing Then
        Throw New ArgumentNullException("player")
      End If
      If player.BitsPerSample <> 16 OrElse player.Channels <> 1 Then
        Throw New ArgumentException("player")
      End If
      _player = player
      _ticksPerBeat = ticksPerBeat
      Me.BPM = 120
    End Sub

    Protected Overloads Overrides Sub Finalize()
      Dispose()
      MyBase.Finalize()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
      If Not (_player Is Nothing) Then
        _player.Dispose()
      End If
      GC.SuppressFinalize(Me)
    End Sub

    ''' <summary>
    ''' Number of tracks
    ''' </summary>
    Public ReadOnly Property Count() As Integer
      Get
        SyncLock _tracks
          Return _tracks.Count
        End SyncLock
      End Get
    End Property

    ''' <summary>
    ''' Delete all tracks
    ''' </summary>
    Public Sub Clear()
      SyncLock _tracks
        _tracks.Clear()
      End SyncLock
    End Sub

    ''' <summary>
    ''' Adds a track
    ''' </summary>
    ''' <param name="track">The track to add</param>
    Public Function Add(ByVal track As Track) As Track
      If track Is Nothing Then
        Throw New ArgumentNullException("track")
      End If
      SyncLock _tracks
        _tracks.Add(track)
      End SyncLock
      Return track
    End Function

    Default Public ReadOnly Property Item(ByVal index As Integer) As Track
      Get
        SyncLock _tracks
          If index > 0 AndAlso index < _tracks.Count + 1 Then
            Return CType(_tracks(index - 1), Track)
          Else
            Throw New ArgumentException("Index must be between 1 and " & _tracks.Count & ".")
          End If
        End SyncLock
      End Get
    End Property

    Default Public ReadOnly Property Item(ByVal trackName As String) As Track
      Get
        SyncLock _tracks
          For Each element As Track In _tracks
            If element.Name = trackName Then
              Return element
            End If
          Next
        End SyncLock
        Return Nothing
      End Get
    End Property

    ''' <summary>
    ''' Gets or sets the current BPM
    ''' </summary>
    Public Property BPM() As Integer
      Get
        Return _BPM
      End Get
      Set(ByVal Value As Integer)
        SyncLock _BPMLock
          _BPM = Value
          _tickPeriod = _player.SamplingRate * 60 \ _BPM \ _ticksPerBeat
        End SyncLock
      End Set
    End Property

    ''' <summary>
    ''' Starts the mixer
    ''' </summary>
    Public Sub Play()
      [Stop]()
      _tick = 0
      _tickLeft = 0
      _player.Play(New AudioPlayer.PullAudioCallback(AddressOf Mix16Mono))
    End Sub

    ''' <summary>
    ''' Stops the mixer
    ''' </summary>
    Public Sub [Stop]()
      _player.Stop()
    End Sub

    Public ReadOnly Property CurrentTick() As Integer
      Get
        Dim ticks As Integer = _tick - GetBufferedTicks()
        While ticks < 0
          ticks += MAXTRACKLENGTH
        End While
        Return ticks Mod MAXTRACKLENGTH
      End Get
    End Property

    ' private stuff

    Private Sub DoMix(ByVal samples As Integer)
      ' grow mix buffer as necessary
      If _mixBuffer Is Nothing OrElse _mixBuffer.Length < samples Then
        _mixBuffer = New Integer(samples) {}
      End If
      ' clear mix buffer
      Array.Clear(_mixBuffer, 0, _mixBuffer.Length)
      Dim position As Integer = 0
      While position < samples
        ' load current patches
        If _tickLeft = 0 Then
          DoTick()
          SyncLock _BPMLock
            _tickLeft = _tickPeriod
          End SyncLock
        End If
        Dim tomix As Integer = Math.Min(samples - position, _tickLeft)
        ' mix current streams
        For reader As Integer = _readers.Count - 1 To 0 Step -1
          Dim patch As PatchReader = CType(_readers(reader), PatchReader)
          If Not patch.Mix(_mixBuffer, position, tomix) Then
            _readers.RemoveAt(reader)
          End If
        Next
        _tickLeft -= tomix
        position += tomix
      End While
    End Sub

    Private Sub Mix16Mono(ByVal destination As IntPtr, ByVal size As Integer)
      Dim samples As Integer = size \ 2
      DoMix(samples)
      If _mixBuffer16 Is Nothing OrElse _mixBuffer16.Length < samples Then
        _mixBuffer16 = New Short(samples) {}
      End If
      ' clip to 16 bit
      For i As Integer = 0 To samples - 1
        If _mixBuffer(i) > 32767 Then
          _mixBuffer16(i) = 32767
        Else
          If _mixBuffer(i) < -32768 Then
            _mixBuffer16(i) = -32768
          Else
            _mixBuffer16(i) = CShort(Fix(_mixBuffer(i)))
          End If
        End If
      Next i
      System.Runtime.InteropServices.Marshal.Copy(_mixBuffer16, 0, destination, samples)
    End Sub

    Private Sub DoTick()
      SyncLock _tracks
        For Each t As Track In _tracks
          Dim r As PatchReader = t.GetBeat(_tick)
          If Not (r Is Nothing) Then
            _readers.Add(r)
          End If
        Next
        _tick = (_tick + 1) Mod MAXTRACKLENGTH
      End SyncLock
    End Sub

    Private Function GetBufferedTicks() As Integer
      Dim samples As Integer = _player.GetBufferedSize() \ (_player.Channels * _player.BitsPerSample \ 8)
      Return samples \ _tickPeriod
    End Function

  End Class

End Namespace