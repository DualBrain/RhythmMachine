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

Namespace Chronotron.AudioPlayer

  ''' <summary>
  ''' Delegate used to fill in a buffer
  ''' </summary>
  Public Delegate Sub PullAudioCallback(ByVal data As IntPtr, ByVal count As Integer)

  ''' <summary>
  ''' Audio player interface
  ''' </summary>
  Public Interface IAudioPlayer
    Inherits IDisposable
    ReadOnly Property SamplingRate() As Integer
    ReadOnly Property BitsPerSample() As Integer
    ReadOnly Property Channels() As Integer
    Function GetBufferedSize() As Integer
    Sub Play(ByVal onAudioData As PullAudioCallback)
    Sub [Stop]()
  End Interface

End Namespace