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
Imports System.Windows.Forms
Imports Microsoft.DirectX.DirectSound
Imports System.Runtime.InteropServices

Namespace DSoundPlayer

  ''' <summary>
  ''' An audio streaming player using DirectSound
  ''' </summary>

  Public Class StreamingPlayer
    Implements IDisposable, Chronotron.AudioPlayer.IAudioPlayer

    Private Const MaxLatencyMs As Integer = 300

    Private _device As Device
    Private _ownsDevice As Boolean
    Private _buffer As SecondaryBuffer
    Private _timer As System.Timers.Timer
    Private _nextWrite As Integer
    Private _bufferBytes As Integer
    Private _pullStream As Stream

    Private Class PullStream
      Inherits Stream

      Private _pullAudio As Chronotron.AudioPlayer.PullAudioCallback

      Public Sub New(ByVal pullAudio As Chronotron.AudioPlayer.PullAudioCallback)
        _pullAudio = pullAudio
      End Sub

      Public Overrides ReadOnly Property CanRead() As Boolean
        Get
          Return True
        End Get
      End Property

      Public Overrides ReadOnly Property CanSeek() As Boolean
        Get
          Return False
        End Get
      End Property

      Public Overrides ReadOnly Property CanWrite() As Boolean
        Get
          Return False
        End Get
      End Property

      Public Overrides ReadOnly Property Length() As Long
        Get
          Return 0
        End Get
      End Property

      Public Overrides Property Position() As Long
        Get
          Return 0
        End Get
        Set(ByVal Value As Long)
        End Set
      End Property

      Public Overrides Sub Close()
      End Sub

      Public Overrides Sub Flush()
      End Sub

      Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        If Not (_pullAudio Is Nothing) Then
          Dim h As GCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned)
          Try
            _pullAudio(New IntPtr(h.AddrOfPinnedObject().ToInt64() + offset), count)
          Finally
            h.Free()
          End Try
        Else
          For i As Integer = offset To (offset + count) - 1
            buffer(i) = 0
          Next
        End If
        Return count
      End Function

      Public Overrides Function Seek(ByVal offset As Long, ByVal origin As System.IO.SeekOrigin) As Long
        Return 0
      End Function

      Public Overrides Sub SetLength(ByVal length As Long)
      End Sub

      Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
      End Sub

      Public Overrides Sub WriteByte(ByVal value As Byte)
      End Sub

    End Class

    ''' <summary>
    ''' Helper function for creating WaveFormat instances
    ''' </summary>
    ''' <param name="sr">Sampling rate</param>
    ''' <param name="bps">Bits per sample</param>
    ''' <param name="ch">Channels</param>
    ''' <returns></returns>
    Public Shared Function CreateWaveFormat(ByVal sr As Integer, ByVal bps As Short, ByVal ch As Short) As WaveFormat

      Dim wfx As New WaveFormat

      wfx.FormatTag = WaveFormatTag.Pcm
      wfx.SamplesPerSecond = sr
      wfx.BitsPerSample = bps
      wfx.Channels = ch

      wfx.BlockAlign = CShort(wfx.Channels * wfx.BitsPerSample \ 8)
      wfx.AverageBytesPerSecond = wfx.SamplesPerSecond * wfx.BlockAlign

      Return wfx

    End Function

    Public Sub New(ByVal owner As Control, ByVal sr As Integer, ByVal bps As Short, ByVal ch As Short)
      MyClass.New(owner, Nothing, CreateWaveFormat(sr, bps, ch))
    End Sub

    Public Sub New(ByVal owner As Control, ByVal format As WaveFormat)
      MyClass.New(owner, Nothing, format)
    End Sub

    Public Sub New(ByVal owner As Control, ByVal device As Device, ByVal sr As Integer, ByVal bps As Short, ByVal ch As Short)
      MyClass.New(owner, device, CreateWaveFormat(sr, bps, ch))
    End Sub

    Public Sub New(ByVal owner As Control, ByVal device As Device, ByVal format As WaveFormat)
      _device = device
      If _device Is Nothing Then
        _device = New device
        _device.SetCooperativeLevel(owner, CooperativeLevel.Normal)
        _ownsDevice = True
      End If
      Dim desc As New BufferDescription(format)
      desc.BufferBytes = format.AverageBytesPerSecond
      desc.ControlVolume = True
      desc.GlobalFocus = True
      _buffer = New SecondaryBuffer(desc, _device)
      _bufferBytes = _buffer.Caps.BufferBytes
      _timer = New System.Timers.Timer(BytesToMs(_bufferBytes) / 6)
      _timer.Enabled = False
      AddHandler _timer.Elapsed, AddressOf Timer_Elapsed
    End Sub

    Protected Overloads Overrides Sub Finalize()
      Dispose()
      MyBase.Finalize()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
      [Stop]()
      If Not (_timer Is Nothing) Then
        _timer.Dispose()
        _timer = Nothing
      End If
      If Not (_buffer Is Nothing) Then
        _buffer.Dispose()
        _buffer = Nothing
      End If
      If _ownsDevice AndAlso Not (_device Is Nothing) Then
        _device.Dispose()
        _device = Nothing
      End If
      GC.SuppressFinalize(Me)
    End Sub

    ' IAudioPlayer

    Public ReadOnly Property SamplingRate() As Integer Implements Chronotron.AudioPlayer.IAudioPlayer.SamplingRate
      Get
        Return _buffer.Format.SamplesPerSecond
      End Get
    End Property

    Public ReadOnly Property BitsPerSample() As Integer Implements Chronotron.AudioPlayer.IAudioPlayer.BitsPerSample
      Get
        Return _buffer.Format.BitsPerSample
      End Get
    End Property

    Public ReadOnly Property Channels() As Integer Implements Chronotron.AudioPlayer.IAudioPlayer.Channels
      Get
        Return _buffer.Format.Channels
      End Get
    End Property

    Public Sub Play(ByVal pullAudio As Chronotron.AudioPlayer.PullAudioCallback) Implements Chronotron.AudioPlayer.IAudioPlayer.Play
      [Stop]()
      _pullStream = New PullStream(pullAudio)
      _buffer.SetCurrentPosition(0)
      _nextWrite = 0
      Feed(_bufferBytes)
      _timer.Enabled = True
      _buffer.Play(0, BufferPlayFlags.Looping)
    End Sub

    Public Sub [Stop]() Implements Chronotron.AudioPlayer.IAudioPlayer.Stop
      If Not (_timer Is Nothing) Then
        _timer.Enabled = False
      End If
      If Not (_buffer Is Nothing) Then
        Try
          _buffer.Stop()
        Catch ex As Exception
          'HACK: swallow for now.
        End Try
      End If
    End Sub

    Public Function GetBufferedSize() As Integer Implements Chronotron.AudioPlayer.IAudioPlayer.GetBufferedSize
      Dim played As Integer = GetPlayedSize()
      If played > 0 AndAlso played < _bufferBytes Then
        Return _bufferBytes - played
      Else
        Return 0
      End If
    End Function

    Public ReadOnly Property Device() As Device
      Get
        Return _device
      End Get
    End Property

    Private Function BytesToMs(ByVal bytes As Integer) As Integer
      Return bytes * 1000 \ _buffer.Format.AverageBytesPerSecond
    End Function

    Private Function MsToBytes(ByVal ms As Integer) As Integer
      Dim bytes As Integer = ms * _buffer.Format.AverageBytesPerSecond \ 1000
      bytes -= bytes Mod _buffer.Format.BlockAlign
      Return bytes
    End Function

    Private Sub Feed(ByVal bytes As Integer)

      ' limit latency to some milliseconds

      Dim tocopy As Integer = Math.Min(bytes, MsToBytes(MaxLatencyMs))

      If tocopy > 0 Then
        ' restore buffer
        If _buffer.Status.BufferLost Then
          _buffer.Restore()
        End If
        ' copy data to the buffer
        _buffer.Write(_nextWrite, _pullStream, tocopy, LockFlag.None)
        _nextWrite += tocopy
        If _nextWrite >= _bufferBytes Then
          _nextWrite -= _bufferBytes
        End If
      End If

    End Sub

    Private Function GetPlayedSize() As Integer
      Dim pos As Integer = _buffer.PlayPosition
      If pos < _nextWrite Then
        Return pos + _bufferBytes - _nextWrite
      Else
        Return pos - _nextWrite
      End If
    End Function

    Private Sub Timer_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs)
      Feed(GetPlayedSize())
    End Sub

  End Class

End Namespace