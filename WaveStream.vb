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
Imports Chronotron.WaveLib

Namespace Chronotron.WaveIO

  Public Class WaveStream
    Inherits Stream

    Private _stream As Stream
    Private _dataPos As Long
    Private _length As Long

    Private _format As WaveFormat

    Public ReadOnly Property Format() As WaveFormat
      Get
        Return _format
      End Get
    End Property

    Private Function ReadChunk(ByVal reader As BinaryReader) As String
      Dim chunk(3) As Byte
      reader.Read(chunk, 0, chunk.Length)
      Return System.Text.Encoding.ASCII.GetString(chunk, 0, chunk.Length)
    End Function

    Private Sub ReadHeader()

      Dim Reader As New BinaryReader(_stream)

      If ReadChunk(Reader) <> "RIFF" Then
        Throw New Exception("Invalid file format")
      End If
      Reader.ReadInt32() ' File length minus first 8 bytes of RIFF description, we don't use it
      If ReadChunk(Reader) <> "WAVE" Then
        Throw New Exception("Invalid file format")
      End If
      If ReadChunk(Reader) <> "fmt " Then
        Throw New Exception("Invalid file format")
      End If
      If Reader.ReadInt32() <> 16 Then ' bad format chunk length
        Throw New Exception("Invalid file format")
      End If
      _format = New WaveFormat(22050, 16, 2) ' initialize to any format
      _format.wFormatTag = Reader.ReadInt16()
      _format.nChannels = Reader.ReadInt16()
      _format.nSamplesPerSec = Reader.ReadInt32()
      _format.nAvgBytesPerSec = Reader.ReadInt32()
      _format.nBlockAlign = Reader.ReadInt16()
      _format.wBitsPerSample = Reader.ReadInt16()

      ' assume the data chunk is aligned
      While _stream.Position < _stream.Length AndAlso ReadChunk(Reader) <> "data"
      End While

      If _stream.Position >= _stream.Length Then
        Throw New Exception("Invalid file format")
      End If

      _length = Reader.ReadInt32()
      _dataPos = _stream.Position

      Position = 0

    End Sub

    Public Sub New(ByVal fileName As String)
      MyClass.New(New FileStream(fileName, FileMode.Open))
    End Sub

    Public Sub New(ByVal stream As Stream)
      _stream = stream
      ReadHeader()
    End Sub

    Protected Overloads Overrides Sub Finalize()
      Close() 'Dispose()
      MyBase.Finalize()
    End Sub

    Public Overrides Sub Close()
      If Not (_stream Is Nothing) Then
        _stream.Close()
      End If
      GC.SuppressFinalize(Me)
    End Sub

    'Public Sub Dispose()
    '  If Not (_stream Is Nothing) Then
    '    _stream.Close()
    '  End If
    '  GC.SuppressFinalize(Me)
    'End Sub

    Public Overrides ReadOnly Property CanRead() As Boolean
      Get
        Return True
      End Get
    End Property

    Public Overrides ReadOnly Property CanSeek() As Boolean
      Get
        Return True
      End Get
    End Property

    Public Overrides ReadOnly Property CanWrite() As Boolean
      Get
        Return False
      End Get
    End Property

    Public Overrides ReadOnly Property Length() As Long
      Get
        Return _length
      End Get
    End Property

    Public Overrides Property Position() As Long
      Get
        Return _stream.Position - _dataPos
      End Get
      Set(ByVal Value As Long)
        Seek(Value, SeekOrigin.Begin)
      End Set
    End Property

    'Public Overrides Sub Close()
    '  Dispose()
    'End Sub

    Public Overrides Sub Flush()
    End Sub

    Public Overrides Sub SetLength(ByVal length As Long)
      Throw New InvalidOperationException
    End Sub

    Public Overrides Function Seek(ByVal position As Long, ByVal origin As SeekOrigin) As Long
      Select Case origin
        Case SeekOrigin.Begin
          _stream.Position = position + _dataPos
        Case SeekOrigin.Current
          _stream.Seek(position, SeekOrigin.Current)
        Case SeekOrigin.End
          _stream.Position = _dataPos + _length - position
      End Select
      Return Me.Position
    End Function

    Public Overrides Function ReadByte() As Integer
      Return CInt(IIf(Position < _length, _stream.ReadByte(), -1))
    End Function

    Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
      Dim toread As Integer = CInt(Fix(Math.Min(count, _length - Position)))
      Return _stream.Read(buffer, offset, toread)
    End Function

    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
      Throw New InvalidOperationException
    End Sub

  End Class

End Namespace