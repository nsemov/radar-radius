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

Imports System.Net
Imports RADAR.Conversion

Public Class RADIUSServer

    Private mSocket As UDPServer
    Private mSecrets As NASAuthList
    Private mLastAuthenticator() As Byte = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}

    Public Delegate Sub RADIUSHandler(ByVal packet As RADIUSPacket)

    Private HandlePacket As RADIUSHandler

    Public Sub New(ByVal portNumber As Integer, ByVal onRADIUSPacket As RADIUSHandler, ByRef secrets As NASAuthList)
        mSocket = New UDPServer(portNumber, AddressOf SocketData)
        HandlePacket = onRADIUSPacket
        mSecrets = secrets
    End Sub

    Public Sub New(ByVal ipAddress As String, ByVal portNumber As Integer, ByVal onRADIUSPacket As RADIUSHandler, ByRef secrets As NASAuthList)
        mSocket = New UDPServer(ipAddress, portNumber, AddressOf SocketData)
        HandlePacket = onRADIUSPacket
        mSecrets = secrets
    End Sub

    Public Sub SendAsRequest(ByVal packet As RADIUSPacket)
        If packet Is Nothing Then Exit Sub
        If Not packet.IsValid Then Exit Sub
        Dim data() As Byte = packet.Bytes
        Dim hasher As System.Security.Cryptography.MD5 = System.Security.Cryptography.MD5.Create
        Dim hash() As Byte = {}
        Dim secret As String = mSecrets.GetSharedSecret(packet.EndPoint.Address.ToString)
        Array.Resize(hash, data.Length + secret.Length)
        data.CopyTo(hash, 0)
        ConvertToBytes(secret).CopyTo(hash, data.Length)
        hash = hasher.ComputeHash(hash)
        hash.CopyTo(data, 4)
        hash.CopyTo(mLastAuthenticator, 0)
        mSocket.Send(data, packet.EndPoint)
    End Sub

    Public Sub SendAsResponse(ByVal packet As RADIUSPacket, ByVal requestAuth() As Byte)
        If packet Is Nothing Then Exit Sub
        If Not packet.IsValid Then Exit Sub
        If requestAuth Is Nothing Then Exit Sub
        If requestAuth.Length <> 16 Then Exit Sub
        Dim data() As Byte = packet.Bytes
        Dim hasher As System.Security.Cryptography.MD5 = System.Security.Cryptography.MD5.Create
        Dim hash() As Byte = {}
        Dim secret As String = mSecrets.GetSharedSecret(packet.EndPoint.Address.ToString)
        Array.Resize(hash, data.Length + secret.Length)
        data.CopyTo(hash, 0)
        ConvertToBytes(secret).CopyTo(hash, data.Length)
        Array.Copy(requestAuth, 0, hash, 4, 16)
        hash = hasher.ComputeHash(hash)
        hash.CopyTo(data, 4)
        mSocket.Send(data, packet.EndPoint)
    End Sub

    Private Sub SocketData(ByRef data() As Byte, ByRef ep As IPEndPoint)
        'Verify validity ...
        If data.Length < 20 Then Exit Sub
        Dim len As Integer = data(2) * 256 + data(3)
        If data.Length <> len Then Exit Sub
        Dim code As RadiusPacketCode = data(0)
        Dim auth As Boolean
        If code = RadiusPacketCode.AccessRequest Or code = RadiusPacketCode.AccountingRequest Then
            auth = AuthenticateRequest(data, mSecrets.GetSharedSecret(ep.Address.ToString))
        Else
            auth = AuthenticateResponse(data, mSecrets.GetSharedSecret(ep.Address.ToString))
        End If
        If Not auth Then Exit Sub
        Dim packet As New RADIUSPacket(data, ep)
        HandlePacket(packet)
    End Sub

    Private Function AuthenticateRequest(ByRef dataBytes() As Byte, ByVal secret As String) As Boolean
        If dataBytes Is Nothing Then Return False
        If dataBytes(0) = RadiusPacketCode.AccessRequest Then Return True
        If secret Is Nothing Then Return False
        Dim res As Boolean = True
        Dim i As Integer
        Dim hasher As System.Security.Cryptography.MD5 = System.Security.Cryptography.MD5.Create
        Dim expectedHash() As Byte = {}

        Array.Resize(Of Byte)(expectedHash, dataBytes.Length + secret.Length)
        dataBytes.CopyTo(expectedHash, 0)
        ConvertToBytes(secret).CopyTo(expectedHash, dataBytes.Length)

        For i = 4 To 19 : expectedHash(i) = 0 : Next ' Setting authenticator to zero...

        expectedHash = hasher.ComputeHash(expectedHash)

        ' Compare ...
        i = 4 : Do While res And i <= 19 : If dataBytes(i) <> expectedHash(i - 4) Then : res = False : End If : i += 1 : Loop

        Return res
    End Function

    Private Function AuthenticateResponse(ByRef dataBytes() As Byte, ByVal secret As String) As Boolean
        If secret Is Nothing Then Return False
        If secret = "" Then Return False
        Dim res As Boolean = True
        Dim i As Integer
        Dim hasher As System.Security.Cryptography.MD5 = System.Security.Cryptography.MD5.Create
        Dim expectedHash() As Byte = {}

        Array.Resize(Of Byte)(expectedHash, dataBytes.Length + secret.Length)
        dataBytes.CopyTo(expectedHash, 0)
        ConvertToBytes(secret).CopyTo(expectedHash, dataBytes.Length)

        For i = 4 To 19 : expectedHash(i) = mLastAuthenticator(i - 4) : Next ' Setting authenticator to zero...

        expectedHash = hasher.ComputeHash(expectedHash)

        ' Compare ...
        i = 4 : Do While res And i <= 19 : If dataBytes(i) <> expectedHash(i - 4) Then : res = False : End If : i += 1 : Loop

        Return res
    End Function

End Class
