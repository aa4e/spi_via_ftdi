Imports System.Collections.Generic
Imports System.Linq
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Diagnostics

Namespace Ftdi

#Region "ENUMS"

    ''' <summary>
    ''' Режим, в котором работает устройство MPSSE.
    ''' </summary>
    Public Enum DeviceWorkingMode As Integer
        None
        SPI
        I2C
        JTAG
        OneWire
        MicroWire
        SyncFifo
        Uart
    End Enum

    ''' <summary>
    ''' Флаги состояния устройства.
    ''' </summary>
    <CLSCompliant(False)>
    <Flags()>
    Public Enum FlagsEnum As UInteger
        DEVICE_OPENED = 1
        DEVICE_HISPEED = 2
    End Enum

    ''' <summary>
    ''' Типы устройств FTDI.
    ''' </summary>
    Public Enum DeviceEnum As Integer
        FT_DEVICE_BM
        FT_DEVICE_AM
        FT100AX
        DEVICE_UNKNOWN
        FT2232C
        FT232R
        FT2232H
        FT4232H
        FT232H
        FTX_SERIES
        FT4222H_0
        FT4222H_1_2
        FT4222H_3
        FT4222_PROG
        FT900
    End Enum

#End Region '/ENUMS

    ''' <summary>
    ''' Базовый класс для всех устройств FTDI.
    ''' </summary>
    Public MustInherit Class FtdiDeviceBase

#Region "CTOR"

        Public Sub New(index As Integer)
            _DeviceIndex = index
#If WPF Then
            Try
                Using ft As New FTD2XX_NET.Ftdi(index)
                    ComPort = $"COM{ft.GetComPort()}"
                End Using
            Catch ex As Exception
                Diagnostics.Debug.WriteLine(ex)
            End Try
#Else
            Try
                Dim ff As New FTD2XX_NET()
                ff.Open(index)
                ff.GetComPort()
                ff.Close()
            Catch ex As Exception
                Debug.WriteLine(ex)
            End Try
#End If

            Dim ci As FT_DEVICE_LIST_INFO_NODE = Spi.MpsseSpi.GetChannelInfo(index)
            If String.IsNullOrWhiteSpace(ComPort) Then
                SetNameReadable($"{ci.Description}")
            Else
                SetNameReadable($"{ci.Description} ({ComPort})")
            End If

            SetSerialNumber(ci.SerialNumber)
            SetDeviceType(ci.Type)

        End Sub

#End Region '/CTOR

#Region "CONST"

        Protected Const CLOSED_HANDLE As Integer = -1

#End Region '/CONST

#Region "EVENTS"

        Public Event ConnectionStateChanged(isOpened As Boolean)

        Protected Sub RaiseConnectionStateChanged(isOpened As Boolean)
            RaiseEvent ConnectionStateChanged(isOpened)
        End Sub

#End Region '/EVENTS

#Region "PROPS"

        ''' <summary>
        ''' Размер передачи указывается в байтах (true) или битах (false).
        ''' </summary>
        Public Overridable Property TransferSizeInBytes As Boolean = True

        ''' <summary>
        ''' Имя связанного порта.
        ''' </summary>
        Public ReadOnly Property ComPort As String

        ''' <summary>
        ''' Статус.
        ''' </summary>
        Public Overridable Property Status As String

        ''' <summary>
        ''' Индекс устройства в системе.
        ''' </summary>
        Public ReadOnly Property DeviceIndex As Integer
            Get
                Return _DeviceIndex
            End Get
        End Property
        Private _DeviceIndex As Integer = CLOSED_HANDLE

        ''' <summary>
        ''' Дескриптор устройства.
        ''' </summary>
        Public Overridable ReadOnly Property DeviceHandle As Integer
            Get
                Return _DeviceHandle
            End Get
        End Property
        Private _DeviceHandle As Integer = CLOSED_HANDLE

        ''' <summary>
        ''' Задаёт дескриптор устройства.
        ''' </summary>
        ''' <param name="handle">Указатель устройства.</param>
        Protected Sub SetDevHandle(handle As Integer)
            _DeviceHandle = handle
        End Sub

        ''' <summary>
        ''' Открыто ли устройство.
        ''' </summary>
        Public Overridable ReadOnly Property IsOpened As Boolean
            Get
                Return (DeviceHandle <> CLOSED_HANDLE)
            End Get
        End Property

        ''' <summary>
        ''' Описание устройства, считанное из ПЗУ.
        ''' </summary>
        Public ReadOnly Property NameReadable As String

        ''' <summary>
        ''' Задаёт описание устройства, считанное из ПЗУ.
        ''' </summary>
        ''' <param name="name">Описание устройства, считанное из ПЗУ.</param>
        Protected Sub SetNameReadable(name As String)
            _NameReadable = name
        End Sub

        ''' <summary>
        ''' Возвращает строку без запрещённых символов.
        ''' </summary>
        Public Shared Function GetStringCleared(nameReadable As String) As String
            Dim prohibitedSymbols As Char() = IO.Path.GetInvalidFileNameChars()
            Dim nr As New Text.StringBuilder(nameReadable)
            For Each s As Char In prohibitedSymbols
                nr = nr.Replace(s, "")
            Next
            Return nr.ToString()
        End Function

        ''' <summary>
        ''' Серийный номер устройства.
        ''' </summary>
        Public ReadOnly Property SerialNumber As String

        ''' <summary>
        ''' Задаёт серийный номер устройства.
        ''' </summary>
        ''' <param name="sn"></param>
        Protected Sub SetSerialNumber(sn As String)
            _SerialNumber = sn
        End Sub

        ''' <summary>
        ''' Тип устройства.
        ''' </summary>
        Public ReadOnly Property DeviceType As DeviceEnum = DeviceEnum.DEVICE_UNKNOWN

        ''' <summary>
        ''' Задаёт тип устройства.
        ''' </summary>
        ''' <param name="devType">Тип устройства.</param>
        Protected Sub SetDeviceType(devType As DeviceEnum)
            _DeviceType = devType
        End Sub

        ''' <summary>
        ''' Режим работы устройства MPSSE.
        ''' </summary>
        Public MustOverride ReadOnly Property DeviceMode As DeviceWorkingMode

#End Region '/PROPS

#Region "OPEN, INIT, CLOSE CHANNEL"

        ''' <summary>
        ''' Открывает устройство.
        ''' </summary>
        Public MustOverride Sub OpenChannel()

        ''' <summary>
        ''' Закрывает устройство.
        ''' </summary>
        Public MustOverride Sub CloseChannel()

#End Region '/OPEN, INIT, CLOSE CHANNEL

    End Class

End Namespace
