'Copyright (C) 2008-2011  Nikolay Semov

'    This program is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.

'    This program is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.

'    You should have received a copy of the GNU General Public License
'    along with this program.  If not, see <http://www.gnu.org/licenses/>.

Public Class NASAuthList
    Inherits System.Collections.Generic.Dictionary(Of String, String)

    Public Sub AddSharedSecret(ByVal nasIP As String, ByVal secret As String)
        If MyBase.ContainsKey(nasIP) Then
            MyBase.Item(nasIP) = secret
        Else
            MyBase.Add(nasIP, secret)
        End If
    End Sub

    Public Function GetSharedSecret(ByVal nasIP As String) As String
        Dim res As String = ""
        MyBase.TryGetValue(nasIP, res)
        Return res
    End Function
End Class