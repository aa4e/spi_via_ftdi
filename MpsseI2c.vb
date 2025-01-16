Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Namespace Ftdi.I2c

    ''' <summary>
    ''' Работа в режиме IIC с помощью библиотеки LibMPSSE.
    ''' </summary>
    Public Class MpsseI2c
        Inherits MpsseBase

#Region "READ-ONLY PROPS, CONST"

        ''' <summary>
        ''' Режим работы устройства MPSSE - IIC.
        ''' </summary>
        Public Overrides ReadOnly Property DeviceMode As DeviceWorkingMode
            Get
                Return DeviceWorkingMode.I2C
            End Get
        End Property

        Public Const MAX_DEVICES_ON_BUS As Integer = 127
        Public Const MAX_I2C_SPEED As Integer = 3400000
        Public Const I2C_MAX_READ_DATA_BITS_BUFFER As Integer = 524288
        Public Const I2C_MAX_READ_DATA_BYTES_BUFFER As Integer = I2C_MAX_READ_DATA_BITS_BUFFER \ 8

        ''' <summary>
        ''' Стандартные скорости IIС.
        ''' </summary>
        ''' <remarks>
        ''' В качестве ключа можно использовать значения перечисления <see cref="I2cSpeeds"/>.
        ''' </remarks>
        Public ReadOnly Property I2cSpeeds As New Dictionary(Of String, Integer) From {
            {"Standard", I2cSpeed.STANDARD_MODE},
            {"Fast", I2cSpeed.FAST_MODE},
            {"Fast+", I2cSpeed.FAST_MODE_PLUS},
            {"High speed", I2cSpeed.HIGH_SPEED_MODE}
        }

#End Region '/READ-ONLY PROPS, CONST

#Region "CTOR"

        ''' <summary>
        ''' Подключается к устройству с заданным индексом в режиме I2C.
        ''' </summary>
        ''' <param name="index">Индекс устройства в системе, начиная с 0.</param>
        ''' <param name="openNow">Открывать ли устройство сразу (True).</param>
        Public Sub New(index As Integer, Optional openNow As Boolean = True)
            MyBase.New(index)
            If openNow Then
                SetDevHandle(OpenChannel(index))
            End If
        End Sub

#End Region '/CTOR

#Region "ИНФО"

        ''' <summary>
        ''' Проверяет, является ли устройство чипом FT232D. 
        ''' Только для этих чипов актуально свойство <see cref="I2C_CONFIG_OPTIONS.ENABLE_DRIVE_ONLY_ZERO"/>.
        ''' </summary>
        Public ReadOnly Property IsFt232hDevice As Boolean
            Get
                Return (DeviceType = DeviceEnum.FT232H)
            End Get
        End Property

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function I2C_GetChannelInfo(index As Integer, ByRef chanInfo As FT_DEVICE_LIST_INFO_NODE) As Integer
        End Function

        ''' <summary>
        ''' Возвращает информацию об устройстве (канале) по его индексу в системе.
        ''' </summary>
        ''' <param name="index">Индекс канала, начиная с 0 до <see cref="GetNumI2cChannels()"/> - 1.</param>
        Public Overloads Shared Function GetChannelInfo(index As Integer) As FT_DEVICE_LIST_INFO_NODE
            Dim info As New FT_DEVICE_LIST_INFO_NODE()
            Dim r As Integer = I2C_GetChannelInfo(index, info)
            CheckStatus(r)
            Return info
        End Function

        ''' <summary>
        ''' Возвращает информацию о текущем устройстве (канале).
        ''' </summary>
        Public Overrides Function GetChannelInfo() As FT_DEVICE_LIST_INFO_NODE
            Return GetChannelInfo(DeviceHandle)
        End Function

#End Region '/ИНФО

#Region "INIT"

        ''' <summary>
        ''' Последняя заданная конфигурация.
        ''' </summary>
        Private LastConfig As I2cConfig

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function I2C_InitChannel(handle As Integer, ByRef config As I2cConfig) As Integer
        End Function

        ''' <summary>
        ''' Инициализирует канал заданными параметрами.
        ''' </summary>
        ''' <param name="devHandle">Дескриптор устройства.</param>
        ''' <param name="config">Конфигурация канала.</param>
        Public Shared Function InitChannel(devHandle As Integer, config As I2cConfig) As I2cConfig
            Dim r As Integer = I2C_InitChannel(devHandle, config)
            CheckStatus(r)
            Return config
        End Function

        ''' <summary>
        ''' Инициализирует канал заданными параметрами.
        ''' </summary>
        ''' <param name="conf">Конфигурация канала.</param>
        Public Function InitChannel(conf As I2cConfig) As I2cConfig
            If (DeviceHandle <> CLOSED_HANDLE) Then
                LastConfig = conf
                'If VerboseLog Then
                '    Log($"Устройство инициализировано ({conf.ClockRate}).")
                'End If
                Return InitChannel(DeviceHandle, conf)
            End If
        End Function

        ''' <summary>
        ''' Устанавливает последнюю заданную конфигурацию.
        ''' </summary>
        Public Function ReInitChannel() As I2cConfig
            Return InitChannel(DeviceHandle, LastConfig)
        End Function

#End Region '/INIT

#Region "OPEN, CLOSE CHANNEL"

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function I2C_OpenChannel(index As Integer, ByRef handle As Integer) As Integer
        End Function

        ''' <summary>
        ''' Открывает устройство в режиме I2C и возвращает его дескриптор.
        ''' </summary>
        ''' <param name="index">Индекс устройства в системе.</param>
        Private Overloads Shared Function OpenChannel(index As Integer) As Integer
            Dim devH As Integer = 0
            Dim r As Integer = I2C_OpenChannel(index, devH)
            CheckStatus(r)
            If (devH = 0) Then
                Throw New SystemException("Устройство не открыто.")
            End If
            Return devH
        End Function

        ''' <summary>
        ''' Открывает канал в режиме I2C, если это не было сделано при создании экземпляра.
        ''' </summary>
        Public Overrides Sub OpenChannel()
            If (Not IsOpened) Then
                SetDevHandle(OpenChannel(DeviceIndex))
                RaiseConnectionStateChanged(True)
            End If
        End Sub

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function I2C_CloseChannel(handle As Integer) As Integer
        End Function

        ''' <summary>
        ''' Закрывает устройство (если оно открыто).
        ''' </summary>
        Public Overrides Sub CloseChannel()
            If IsOpened Then
                Dim r As Integer = I2C_CloseChannel(DeviceHandle)
                CheckStatus(r)
                SetDevHandle(CLOSED_HANDLE)
                RaiseConnectionStateChanged(False)
            End If
        End Sub

#End Region '/OPEN, CLOSE CHANNEL

#Region "READ, WRITE"

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function I2C_DeviceRead(handle As Integer, deviceAddress As Integer, sizeToTransfer As Integer, buffer As Byte(), ByRef sizeTransfered As Integer, options As I2C_TRANSFER_OPTIONS) As Integer
        End Function

        ''' <summary>
        ''' Читает по I2C из заданного адреса заданное число байтов и возвращает массив реально прочитанных байтов.
        ''' </summary>
        ''' <param name="devHandle">Дескриптор устройства (канала).</param>
        ''' <param name="deviceAddress">Адрес I2C устройства, 7-битовое значение (всегда меньше 128).</param>
        ''' <param name="readLength">Число байтов для чтения.</param>
        ''' <param name="options">Опции чтения.</param>
        Public Shared Function Read(devHandle As Integer, deviceAddress As Integer, readLength As Integer, Optional options As I2C_TRANSFER_OPTIONS = I2C_TRANSFER_OPTIONS.START_BIT Or I2C_TRANSFER_OPTIONS.STOP_BIT) As Byte()
            Dim wasRedLength As Integer = 0
            Dim bufferToRead(readLength - 1) As Byte
            Dim r As Integer = I2C_DeviceRead(devHandle, deviceAddress, readLength, bufferToRead, wasRedLength, options)
            CheckStatus(r)
            ReDim Preserve bufferToRead(wasRedLength - 1)
            Return bufferToRead
        End Function

        ''' <summary>
        ''' Читает по I2C из заданного адреса заданное число байтов и возвращает массив реально прочитанных байтов.
        ''' </summary>
        ''' <param name="deviceAddress">Адрес I2C устройства, 7-битовое значение (всегда меньше 128).</param>
        ''' <param name="readLength">Число байтов для чтения.</param>
        ''' <param name="options">Опции чтения.</param>
        Public Function Read(deviceAddress As Integer, readLength As Integer, Optional options As I2C_TRANSFER_OPTIONS = I2C_TRANSFER_OPTIONS.START_BIT Or I2C_TRANSFER_OPTIONS.STOP_BIT) As Byte()
            Return Read(DeviceHandle, deviceAddress, readLength, options)
        End Function

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function I2C_DeviceWrite(handle As Integer, deviceAddress As Integer, sizeToTransfer As Integer, buffer As Byte(), ByRef sizeTransfered As Integer, options As I2C_TRANSFER_OPTIONS) As Integer
        End Function

        ''' <summary>
        ''' Записывает по I2C заданный массив и возвращает число переданных байтов.
        ''' </summary>
        ''' <param name="devHandle">Дескриптор устройства (канала).</param>
        ''' <param name="deviceAddress">Адрес I2C устройства.</param>
        ''' <param name="bufferToWrite">Массив для записи.</param>
        ''' <param name="options">Опции записи.</param>
        Public Shared Function Write(devHandle As Integer, deviceAddress As Integer, bufferToWrite As Byte(), Optional options As I2C_TRANSFER_OPTIONS = I2C_TRANSFER_OPTIONS.START_BIT Or I2C_TRANSFER_OPTIONS.STOP_BIT) As Integer
            Dim bytesTransferred As Integer = 0
            Dim writeLength As Integer = bufferToWrite.Length
            Dim r As Integer = I2C_DeviceWrite(devHandle, deviceAddress, writeLength, bufferToWrite, bytesTransferred, options)
            CheckStatus(r)
            Return bytesTransferred
        End Function

        ''' <summary>
        ''' Записывает по I2C заданный массив и возвращает число переданных байтов.
        ''' </summary>
        ''' <param name="deviceAddress">Адрес I2C устройства.</param>
        ''' <param name="bufferToWrite">Массив для записи.</param>
        ''' <param name="options">Опции записи.</param>
        Public Function Write(deviceAddress As Integer, bufferToWrite As Byte(), Optional options As I2C_TRANSFER_OPTIONS = I2C_TRANSFER_OPTIONS.START_BIT Or I2C_TRANSFER_OPTIONS.STOP_BIT) As Integer
            Return Write(DeviceHandle, deviceAddress, bufferToWrite, options)
        End Function

#End Region '/READ, WRITE

    End Class '/MpsseI2c

#Region "СТРУКТУРЫ И ПЕРЕЧИСЛЕНИЯ"

    ''' <summary>
    ''' Конфигурация устройства (канала) I2C.
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Public Structure I2cConfig

        ''' <summary>
        ''' Значение тактовой частоты шины IIC, в Гц. 
        ''' Можно задать стандартные значения <see cref="I2cSpeed"/> 
        ''' или произвольные значения в диапазоне от 0 до 3400000.
        ''' </summary>
        <CLSCompliant(False)>
        Public ClockRate As I2cSpeed

        ''' <summary>
        ''' Значение таймера задержек, в мс. Действительные значения 0...255. 
        ''' Рекомендуются диапазоны:
        ''' - для full-speed устройств (FT2232D): 2...255;
        ''' - для hi-speed устройств (FT232H, FT2232H, FT4232H): 1...255.
        ''' </summary>
        Public LatencyTimer As Byte

        ''' <summary>
        ''' <list>
        ''' <item>Бит 0 - Задаёт 3-фазовое тактирование.</item>
        ''' <item>Бит 1 - Задаёт опцию Drive-Only-Zero.</item>
        ''' <item>Биты 2..31 - Резерв.</item>
        ''' </list>
        ''' </summary>
        Public ConfigOptions As I2C_CONFIG_OPTIONS

    End Structure

    ''' <summary>
    ''' Стандартные значения частоты шины данных в Гц.
    ''' </summary>
    Public Enum I2cSpeed As Integer
        STANDARD_MODE = 100000
        FAST_MODE = 400000
        FAST_MODE_PLUS = 1000000
        HIGH_SPEED_MODE = 3400000
    End Enum

    ''' <summary>
    ''' Конфигурация IIC.
    ''' </summary>
    <Flags()>
    Public Enum I2C_CONFIG_OPTIONS As Integer
        NONE = 0
        ''' <summary>
        ''' Отключить 3-фазовое тактирование. По умолчанию включено.
        ''' </summary>
        DISABLE_3_PHASE_CLOCKING = 1
        ''' <summary>
        ''' Задаёт опцию Drive-Only-Zero.
        ''' </summary>
        ''' <remarks>
        ''' The I2C master should actually drive the SDA line only when the output is LOW. 
        ''' It should be tristate the SDA line when the output should be high. 
        ''' This tristating the SDA line during output HIGH is supported only in FT232H chip. 
        ''' </remarks>
        ENABLE_DRIVE_ONLY_ZERO = 2
    End Enum

    ''' <summary>
    ''' Параметры передачи I2C.
    ''' </summary>
    <Flags()>
    Public Enum I2C_TRANSFER_OPTIONS As Integer

        ''' <summary>
        ''' Генерировать стартовый бит до начала передачи.
        ''' </summary>
        START_BIT = &H1

        ''' <summary>
        ''' Генерировать стоповый бит после передачи.
        ''' </summary>
        STOP_BIT = &H2

        ''' <summary>
        ''' Будет ли прервана передача, если не получено подтверждение (NAK).
        ''' Используется только при записи.
        ''' </summary>
        ''' <remarks>
        ''' Continue transmitting data in bulk without caring about Ack or nAck from device if this bit is not set. 
        ''' If this bit is set then stop transitting the data in the buffer when the device nAcks.
        ''' </remarks>
        BREAK_ON_NACK = &H4

        ''' <summary>
        ''' Некоторые I2C ведомые требуют чтобы мастер генерировал NAK после чтения последнего байта.
        ''' Эта опция позволяет работать с такими ведомыми.
        ''' Используется только при чтении.
        ''' </summary>
        ''' <remarks>
        ''' libMPSSE-I2C generates an ACKs for every byte read. 
        ''' Some I2C slaves require the I2C master to generate a nACK for the last data byte read. 
        ''' Setting this bit enables working with such I2C slaves.
        ''' </remarks>
        NACK_LAST_BYTE = &H8

        '*** No address phase, no USB interframe delays: 

        ''' <summary>
        ''' Быстрая передача - без задержек между фазами START, ADDRESS, DATA и STOP.
        ''' </summary>
        FAST_TRANSFER_BYTES = &H10

        ''' <summary>
        ''' Быстрая передача - без задержек между фазами START, ADDRESS, DATA и STOP.
        ''' </summary>
        FAST_TRANSFER_BITS = &H20

        ''' <summary>
        ''' Быстрая передача - без задержки между фазами START, ADDRESS, DATA и STOP, без USB задержек.
        ''' </summary>
        FAST_TRANSFER = &H30

        ''' <summary>
        ''' Игнорирует IIC адрес ведомого.
        ''' Работает только когда включены опции <see cref="FAST_TRANSFER_BYTES"/> и <see cref="FAST_TRANSFER_BITS" /> (или <see cref="FAST_TRANSFER"/>).
        ''' </summary>
        ''' <remarks>
        ''' If is set then setting this bit would mean that the address field should be ignored. 
        ''' The address is either a part of the data or this is a special I2C frame that doesn't require an address.
        ''' </remarks>
        NO_ADDRESS = &H40

    End Enum

#End Region '/СТРУКТУРЫ И ПЕРЕЧИСЛЕНИЯ

End Namespace
