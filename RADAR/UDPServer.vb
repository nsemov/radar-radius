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
Imports System.Net.Sockets

Friend Class UDPServer

    Private mSocket As UdpClient
    Private mAsyncResult As IAsyncResult

    Friend Delegate Sub UDPPacketHandler(ByRef data() As Byte, ByRef endPoint As IPEndPoint)

    Private HandlePacket As UDPPacketHandler

    Friend Sub New(ByVal portNumber As Integer, ByVal onDataArrived As UDPPacketHandler)
        mSocket = New UdpClient(portNumber)
        HandlePacket = onDataArrived
        commonNew()
    End Sub

    Friend Sub New(ByVal ipAddress As String, ByVal portNumber As Integer, ByVal onDataArrived As UDPPacketHandler)
        Dim ep As New IPEndPoint(System.Net.IPAddress.Parse(ipAddress), portNumber)
        mSocket = New UdpClient(ep)
        HandlePacket = onDataArrived
        commonNew()
    End Sub

    Private Sub commonNew()
        mAsyncResult = mSocket.BeginReceive(New AsyncCallback(AddressOf DataReceived), Nothing)
    End Sub

    Private Sub DataReceived(ByVal ar As IAsyncResult)
        Dim ep As New IPEndPoint(0, 0)
        Dim ef As Boolean = False
        Dim data() As Byte = {}
        Try
            data = mSocket.EndReceive(mAsyncResult, ep)
        Catch ex As Exception
            ef = True
        End Try
        If Not ef Then
            mAsyncResult = mSocket.BeginReceive(New AsyncCallback(AddressOf DataReceived), Nothing)
            HandlePacket(data, ep)
        End If
    End Sub

    Friend Sub Send(ByRef data() As Byte, ByRef endPoint As IPEndPoint)
        mSocket.Send(data, data.Length, endPoint)
    End Sub

End Class
