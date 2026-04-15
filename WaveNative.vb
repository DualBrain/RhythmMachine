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
Imports System.Runtime.InteropServices

Namespace Chronotron.WaveLib

  Public Enum WaveFormats
    Pcm = 1
    Float = 3
  End Enum

  <StructLayout(LayoutKind.Sequential)> _
  Public Class WaveFormat
    Public wFormatTag As Short
    Public nChannels As Short
    Public nSamplesPerSec As Integer
    Public nAvgBytesPerSec As Integer
    Public nBlockAlign As Short
    Public wBitsPerSample As Short
    Public cbSize As Short
    Public Sub New(ByVal rate As Integer, ByVal bits As Integer, ByVal channels As Integer)
      wFormatTag = CShort(WaveFormats.Pcm)
      nChannels = CShort(channels)
      nSamplesPerSec = rate
      wBitsPerSample = CShort(bits)
      cbSize = 0
      nBlockAlign = CShort(channels * (bits \ 8))
      nAvgBytesPerSec = nSamplesPerSec * nBlockAlign
    End Sub
  End Class

End Namespace