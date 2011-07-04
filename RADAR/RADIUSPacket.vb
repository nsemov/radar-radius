Imports System.Net
Imports System.Security.Cryptography
Imports RADAR.Conversion

Public Class RADIUSPacket
    Private mCode As RadiusPacketCode
    Private mIdentifier As Byte
    Private mAuthenticator() As Byte = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
    Private mAttributes As New RADIUSAttributes
    Private mEndPoint As IPEndPoint
    Private mIsValid As Boolean

    Friend Sub New(ByRef data() As Byte, ByVal endPoint As IPEndPoint)
        'Check validity ...
        mIsValid = mAttributes.LoadAttributes(data)
        If mIsValid Then
            mCode = data(0)
            mIdentifier = data(1)
            Array.Copy(data, 4, mAuthenticator, 0, 16)
            mEndPoint = endPoint
        End If
    End Sub

    Public Sub New(ByVal code As RadiusPacketCode, ByVal identifier As Byte, ByVal attributes As RADIUSAttributes, ByVal endPoint As IPEndPoint)
        mCode = code
        mIdentifier = identifier
        If attributes Is Nothing Then
            mAttributes = New RADIUSAttributes
        Else
            mAttributes = attributes
        End If
        If endPoint Is Nothing Then
            mIsValid = False
        Else
            mEndPoint = endPoint
            mIsValid = True
        End If
    End Sub

    Public ReadOnly Property IsValid() As Boolean
        Get
            Return mIsValid
        End Get
    End Property

    Public ReadOnly Property Code() As RadiusPacketCode
        Get
            Return mCode
        End Get
    End Property

    Public ReadOnly Property Identifier() As Byte
        Get
            Return mIdentifier
        End Get
    End Property

    Public ReadOnly Property Attributes() As RADIUSAttributes
        Get
            Return mAttributes
        End Get
    End Property

    Public ReadOnly Property Authenticator() As Byte()
        Get
            Return mAuthenticator
        End Get
    End Property

    Public ReadOnly Property EndPoint() As IPEndPoint
        Get
            Return mEndPoint
        End Get
    End Property

    Friend Function Bytes() As Byte()
        Dim mLength = 20 + mAttributes.Length
        Dim result() As Byte = {}
        Array.Resize(result, mLength)
        result(0) = mCode
        result(1) = mIdentifier
        result(2) = mLength \ 256
        result(3) = mLength Mod 256
        mAuthenticator.CopyTo(result, 4)
        If mLength > 20 Then mAttributes.Bytes.CopyTo(result, 20)
        Return result
    End Function

    Public Function AuthenticateAccessRequest(ByRef authList As NASAuthList, ByRef nasList As NASAuthList) As Boolean
        If authList Is Nothing Then Return False
        If nasList Is Nothing Then Return False
        If Not mIsValid Then Return False
        If mCode <> RadiusPacketCode.AccessRequest Then Return True

        Dim secret As String = nasList.GetSharedSecret(mEndPoint.Address.ToString)
        If secret = "" Then Return False
        Dim username As RADIUSAttribute = mAttributes.GetFirstAttribute(RadiusAttributeType.UserName)
        If username Is Nothing Then Return False
        Dim userpass As RADIUSAttribute = mAttributes.GetFirstAttribute(RadiusAttributeType.UserPassword)
        If userpass Is Nothing Then Return False
        Dim password As String = authList.GetSharedSecret(username.GetString)
        If password = "" Then Return False

        Dim passlen As Integer = password.Length \ 16
        If password.Length Mod 16 > 0 Then
            passlen += 1
            password = password & StrDup(16 - (password.Length Mod 16), Chr(0))
        End If

        Dim hasher As MD5 = MD5.Create
        Dim expect() As Byte = {}
        Array.Resize(expect, passlen * 16)
        Dim temp() As Byte = {}
        Array.Resize(temp, secret.Length + 16)
        ConvertToBytes(secret).CopyTo(temp, 0)
        Dim i As Integer
        For i = 0 To passlen - 1
            If i = 0 Then
                mAuthenticator.CopyTo(temp, secret.Length)
            Else
                Array.Copy(expect, (i - 1) * 16, temp, secret.Length, 16)
            End If
            Array.Copy(XorBytes(hasher.ComputeHash(temp), ConvertToBytes(password.Substring(i * 16, 16))), 0, expect, i * 16, 16)
        Next

        hasher = Nothing

        Return (ConvertToString(expect) = userpass.GetString)
    End Function

    Private Function XorBytes(ByVal oper1() As Byte, ByVal oper2() As Byte) As Byte()
        Dim res() As Byte = {}
        If oper1.Length <> oper2.Length Then Return res
        Dim i As Integer
        Array.Resize(res, oper1.Length)
        For i = 0 To oper1.Length - 1
            res(i) = oper1(i) Xor oper2(i)
        Next
        Return res
    End Function

End Class

Public Enum RadiusPacketCode As Byte
    AccessRequest = 1
    AccessAccept = 2
    AccessReject = 3
    AccountingRequest = 4
    AccountingResponse = 5
    AccessChallenge = 11
    StatusServer = 12
    StatusClient = 13
    Reserved = 255
End Enum