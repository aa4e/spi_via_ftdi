Imports System.Diagnostics
Imports System.Runtime.InteropServices

Namespace Ftdi.Spi

    ''' <summary>
    ''' Работа в режиме SPI с помощью библиотеки LibMPSSE.
    ''' </summary>
    ''' <remarks>
    ''' Стандартная библиотека в ftdi_spi.c -> SPI_ToggleCS() имеет задержку <code>INFRA_SLEEP(2);</code> моя - <code>INFRA_SLEEP(0)</code>
    ''' </remarks>
    Public Class MpsseSpi
        Inherits MpsseBase

#Region "READ-ONLY PROPS, CONST"

        Public Const HISPEED_MAX_CLOCK As Integer = 60_000_000
        Public Const FULLSPEED_MAX_CLOCK As Integer = 12_000_000
        Public Const MAX_SPI_FREQUENCY As Integer = 30_000_000
        Public Const SPI_MAX_READ_DATA_BITS_BUFFER As Integer = 524288
        Public Const SPI_MAX_READ_DATA_BYTES_BUFFER As Integer = SPI_MAX_READ_DATA_BITS_BUFFER \ 8
        Public Const SPI_MAX_WRITE_DATA_BYTES_BUFFER As Integer = SPI_MAX_READ_DATA_BYTES_BUFFER

        ''' <summary>
        ''' Режим работы устройства MPSSE - SPI.
        ''' </summary>
        Public Overrides ReadOnly Property DeviceMode As DeviceWorkingMode
            Get
                Return DeviceWorkingMode.SPI
            End Get
        End Property

#End Region '/READ-ONLY PROPS, CONST

#Region "CTOR"

        ''' <summary>
        ''' Подключается к устройству с заданным индексом в режиме SPI.
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

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function SPI_GetChannelInfo(index As Integer, ByRef chanInfo As FT_DEVICE_LIST_INFO_NODE) As Integer
        End Function

        ''' <summary>
        ''' Возвращает информацию об устройстве (канале) по его индексу в системе.
        ''' </summary>
        ''' <param name="index">Индекс канала, начиная с 0 до <see cref="GetNumSpiChannels()"/> - 1.</param>
        Public Overloads Shared Function GetChannelInfo(index As Integer) As FT_DEVICE_LIST_INFO_NODE
            Dim info As New FT_DEVICE_LIST_INFO_NODE()
            Dim r As Integer = SPI_GetChannelInfo(index, info)
            CheckStatus(r)
            Return info
        End Function

        Public Overrides Function GetChannelInfo() As FT_DEVICE_LIST_INFO_NODE
            Return GetChannelInfo(DeviceIndex)
        End Function

        ''' <summary>
        ''' Пересчитывает реальную частоту по желаемой заданной.
        ''' </summary>
        ''' <param name="desiredFrequency"></param>
        Friend Function GetRealClockFrequency(desiredFrequency As Integer) As Integer
            Dim maxClk As Integer = HISPEED_MAX_CLOCK 'TODO если устройство 2232D, то 12 МГц, если 2232H - 60 МГц. Определить можно по флагу DEVICE_HISPEED 
            If (Not IsHighSpeedDevice) Then
                maxClk = FULLSPEED_MAX_CLOCK
            End If
            Dim divisor As Double = Math.Floor(maxClk / (desiredFrequency * 2) - 1)
            Debug.WriteLine(divisor)
            Return GetClockFrequencyByDivisor(CInt(divisor))
        End Function

        ''' <summary>
        ''' Возвращает частоту, заданную делителем.
        ''' </summary>
        ''' <param name="divisor"></param>
        Protected Function GetClockFrequencyByDivisor(divisor As Integer) As Integer
            Dim maxClk As Integer = HISPEED_MAX_CLOCK 'если устройство 2232D, то 12 МГц, если 2232H - 60 МГц.
            If (Not IsHighSpeedDevice) Then
                maxClk = FULLSPEED_MAX_CLOCK
            End If
            Dim freq As Integer = CInt(maxClk / ((1 + divisor) * 2))
            Return freq
        End Function

#End Region '/ИНФО

#Region "INIT"

        ''' <summary>
        ''' Последняя заданная конфигурация.
        ''' </summary>
        Private LastConfig As SpiConfig

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function SPI_InitChannel(handle As Integer, ByRef config As SpiConfig) As Integer
        End Function

        ''' <summary>
        ''' Инициализирует канал заданными параметрами.
        ''' </summary>
        ''' <param name="devHandle">Дескриптор устройства.</param>
        ''' <param name="config">Конфигурация канала.</param>
        Public Shared Function InitChannel(devHandle As Integer, config As SpiConfig) As SpiConfig
            Dim r As Integer = SPI_InitChannel(devHandle, config)
            CheckStatus(r)
            Return config
        End Function

        ''' <summary>
        ''' Инициализирует канал заданными параметрами.
        ''' </summary>
        ''' <param name="conf">Конфигурация канала.</param>
        Public Function InitChannel(conf As SpiConfig) As SpiConfig
            If IsOpened Then
                LastConfig = conf
                Return InitChannel(DeviceHandle, conf)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Устанавливает последнюю заданную конфигурацию.
        ''' </summary>
        Public Function ReInitChannel() As SpiConfig
            Return InitChannel(DeviceHandle, LastConfig)
        End Function

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function SPI_ChangeCS(handle As Integer, configOption As SPI_CONFIG_OPTION) As Integer
        End Function

        ''' <summary>
        ''' Изменяет пин CS.
        ''' </summary>
        ''' <param name="configOption"></param>
        Public Sub ChangeCS(configOption As SPI_CONFIG_OPTION)
            Dim s As Integer = SPI_ChangeCS(DeviceHandle, configOption)
            CheckStatus(s)
        End Sub

#End Region '/INIT

#Region "OPEN, CLOSE CHANNEL"

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function SPI_OpenChannel(index As Integer, ByRef handle As Integer) As Integer
        End Function

        ''' <summary>
        ''' Открывает устройство режиме SPI и возвращает его дескриптор.
        ''' </summary>
        ''' <param name="index">Индекс устройства в системе.</param>
        Private Overloads Shared Function OpenChannel(index As Integer) As Integer
            Dim devH As Integer = 0
            Dim r As Integer = SPI_OpenChannel(index, devH)
            CheckStatus(r)
            If (devH = 0) Then
                Throw New SystemException("Устройство не открыто.")
            End If
            Return devH
        End Function

        ''' <summary>
        ''' Открывает канал, если это не было сделано при создании экземпляра.
        ''' </summary>
        Public Overrides Sub OpenChannel()
            If (Not IsOpened) Then
                SetDevHandle(OpenChannel(DeviceIndex))
                RaiseConnectionStateChanged(True)
            End If
        End Sub

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function SPI_CloseChannel(handle As Integer) As Integer
        End Function

        ''' <summary>
        ''' Закрывает устройство (если оно открыто).
        ''' </summary>
        Public Overrides Sub CloseChannel()
            If IsOpened Then
                Dim r As Integer = SPI_CloseChannel(DeviceHandle)
                CheckStatus(r)
                SetDevHandle(CLOSED_HANDLE)
                RaiseConnectionStateChanged(False)
            End If
        End Sub

#End Region '/OPEN, CLOSE CHANNEL

#Region "ИНСТРУМЕНТЫ"

#If INSTR Then

        ''' <summary>
        ''' Генератор частоты.
        ''' </summary>
        Public Overrides ReadOnly Property Generator As FrequencyGenerator
            Get
                Return _Generator
            End Get
        End Property
        Private ReadOnly _Generator As New FrequencyGenerator(Me)

        ''' <summary>
        ''' Генератор частоты.
        ''' </summary>
        Public Overrides ReadOnly Property FreqMeter As FrequencyMeter
            Get
                Return _FreqMeter
            End Get
        End Property
        Private ReadOnly _FreqMeter As New FrequencyMeter(Me)

#End If

#End Region '/ИНСТРУМЕНТЫ

#Region "SPI READ, WRITE"

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function SPI_IsBusy(handle As Integer, <MarshalAs(UnmanagedType.Bool)> ByRef state As Boolean) As Integer
        End Function

        ''' <summary>
        ''' Проверяет, не занято ли устройство.
        ''' This function reads the state of the MISO line without clocking the SPI bus.
        ''' Some applications need the SPI master To poll the MISO line without clocking the bus To
        ''' check if the SPI slave has completed previous operation And Is ready for the next
        ''' operation. This function Is useful for such applications.
        ''' </summary>
        Public Function IsBusy() As Boolean
            Dim state As Boolean
            CheckStatus(SPI_IsBusy(DeviceHandle, state))
            Return state
        End Function

        ''' <remarks>
        ''' FTDI_API FT_STATUS SPI_ReadWrite( FT_HANDLE handle, uint8 *inBuffer, uint8 *outBuffer, uint32 sizeToTransfer, uint32 *sizeTransferred, uint32 transferOptions);
        ''' </remarks>
        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function SPI_ReadWrite(handle As Integer, rxBuffer As Byte(), txBuffer As Byte(),
                                              sizeToTransfer As UInteger, ByRef sizeTransferred As UInteger, options As UInteger) As Integer
        End Function

        ''' <summary>
        ''' Одновременные запись и чтение SPI.
        ''' </summary>
        ''' <param name="txBuffer">Передаваемый буфер.</param>
        ''' <param name="sizeToRead">Число байтов/битов для чтения.</param>
        ''' <param name="sizeToWrite">Число битов для записи. Учитывается только при передаче в битах!</param>
        Public Function ReadWriteBits(txBuffer As Byte(), sizeToWrite As Integer, sizeToRead As Integer) As Byte()

            Dim sizeToTransfer As Integer = sizeToWrite
            Dim rxBuffer() As Byte = New Byte(sizeToRead \ 8 - 1) {}
            Dim transferred As Integer = 0

            Try
                Dim r1 = SPI_Write(DeviceHandle, txBuffer, sizeToTransfer, transferred, SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE Or SPI_TRANSFER_OPTIONS.SIZE_IN_BITS)
                CheckStatus(r1)

                Dim r2 = SPI_Read(DeviceHandle, rxBuffer, sizeToRead, transferred, SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE Or SPI_TRANSFER_OPTIONS.SIZE_IN_BITS)
                CheckStatus(r2)
            Catch ex As Exception
                Throw
            End Try

            Return rxBuffer
        End Function

        '''' <summary>
        '''' Одновременные запись и чтение SPI.
        '''' </summary>
        '''' <param name="txBuffer">Передаваемый буфер.</param>
        '''' <param name="sizeToRead">Число байтов/битов для чтения.</param>
        '''' <param name="options">Параметры передачи.</param>
        '''' <param name="sizeToWrite">Число битов для записи. Учитывается только при передаче в битах!</param>
        'Public Function ReadWriteSim(txBuffer As Byte(), sizeToRead As Integer, sizeToWrite As Integer,
        '                             Optional options As SPI_TRANSFER_OPTIONS _
        '                             = SPI_TRANSFER_OPTIONS.SIZE_IN_BYTES _
        '                             Or SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE _
        '                             Or SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE) As Byte()
        '    'TODO Длины посчитать
        '    Dim sizeToTransfer As Integer = sizeToWrite
        '    Dim readLen As Integer = If(options.HasFlag(SPI_TRANSFER_OPTIONS.SIZE_IN_BITS), sizeToRead \ 8, sizeToRead)
        '    Dim rxBuffer() As Byte = New Byte(readLen - 1) {}
        '    Dim transferred As Integer = 0
        '    Dim r1 = SPI_Write(DeviceHandle, txBuffer, sizeToTransfer, transferred, options)
        '    CheckStatus(r1)

        '    Dim r2 = SPI_Read(DeviceHandle, rxBuffer, readLen, transferred, options)
        '    CheckStatus(r2)

        '    'Dim r3 As Integer = SPI_ReadWrite(DeviceHandle, rxBuffer, txBuffer, sizeToTransfer, transferred, CUInt(options))
        '    Return rxBuffer
        'End Function

        ''' <summary>
        ''' Одновременные запись и чтение SPI.
        ''' </summary>
        ''' <param name="txBuffer">Передаваемый буфер.</param>
        ''' <param name="sizeToRead">Число байтов/битов для чтения.</param>
        ''' <param name="options">Параметры передачи.</param>
        ''' <param name="bitsToWrite">Число битов для записи. Учитывается только при передаче в битах!</param>
        Public Overridable Function ReadWriteSim(txBuffer As Byte(), sizeToRead As Integer,
                                                 Optional options As SPI_TRANSFER_OPTIONS =
                                                 SPI_TRANSFER_OPTIONS.SIZE_IN_BYTES _
                                                 Or SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE _
                                                 Or SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE,
                                                 Optional bitsToWrite As Integer = 0) As Byte()
            Dim len As Integer = txBuffer.Length
            Dim rxBuffer() As Byte = New Byte(sizeToRead - 1) {}
            If ((options And SPI_TRANSFER_OPTIONS.SIZE_IN_BITS) = SPI_TRANSFER_OPTIONS.SIZE_IN_BITS) Then 'TEST Почему-то при SIZE_IN_BYTES передача некорректна
                len *= 8
                len += bitsToWrite 'TEST
                ReDim rxBuffer(sizeToRead \ 8 - 1)
            End If
            Dim transferred As UInteger = 0
            Dim r As Integer = SPI_ReadWrite(DeviceHandle, rxBuffer, txBuffer, CUInt(len), transferred, CUInt(options))
            CheckStatus(r)
            Return rxBuffer
        End Function

        ''' <summary>
        ''' Последовательные запись и чтение SPI.
        ''' </summary>
        ''' <param name="txBuffer">Передаваемый буфер.</param>
        ''' <param name="sizeToRead">Число байтов/битов для чтения. Определяется свойством <see cref="TransferSizeInBytes"/>.</param>
        ''' <remarks>Используется не стандартная функция <see cref="SPI_ReadWrite"/> библиотеки, 
        ''' а комбинация записи и чтения с разными параметрами <see cref="SPI_TRANSFER_OPTIONS"/>.</remarks>
        Public Function ReadWrite(txBuffer As Byte(), sizeToRead As Integer) As Byte()
            Dim rxData As Byte() = New Byte() {}
            Dim transferred As Integer = 0
            If TransferSizeInBytes Then
                SPI_Write(DeviceHandle, txBuffer, txBuffer.Length, transferred, SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE Or SPI_TRANSFER_OPTIONS.SIZE_IN_BYTES)
                rxData = Read(sizeToRead, SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE Or SPI_TRANSFER_OPTIONS.SIZE_IN_BYTES)
            Else
                Dim writeLen As Integer = CInt(txBuffer.Length * 8)
                SPI_Write(DeviceHandle, txBuffer, writeLen, transferred, SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE Or SPI_TRANSFER_OPTIONS.SIZE_IN_BITS)
                rxData = Read(sizeToRead, SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE Or SPI_TRANSFER_OPTIONS.SIZE_IN_BITS)
            End If
            Return rxData
        End Function

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function SPI_Read(handle As Integer, buffer() As Byte, sizeToTransfer As Integer, ByRef sizeTransfered As Integer, options As Integer) As Integer
        End Function

        ''' <summary>
        ''' Запрашивает заданное число байтов (или битов - в зависимости от параметра options) из ведомого SPI.
        ''' </summary>
        ''' <param name="devHandle">Дескриптор устройства (канала).</param>
        ''' <param name="sizeToRead">Число байтов или битов для чтения.</param>
        ''' <param name="options">Параметры передачи.</param>
        Public Shared Function Read(devHandle As Integer, sizeToRead As Integer,
                                    Optional options As SPI_TRANSFER_OPTIONS = SPI_TRANSFER_OPTIONS.SIZE_IN_BYTES Or SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE Or SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE) As Byte()
            Dim buffer() As Byte = New Byte(sizeToRead - 1) {}
            If options.HasFlag(SPI_TRANSFER_OPTIONS.SIZE_IN_BITS) Then
                ReDim buffer(CInt(Math.Ceiling(sizeToRead / 8)) - 1)
            End If
            Dim received As Integer = 0
            Dim r As Integer = SPI_Read(devHandle, buffer, sizeToRead, received, options)
            CheckStatus(r)
            If options.HasFlag(SPI_TRANSFER_OPTIONS.SIZE_IN_BITS) Then
                ReDim Preserve buffer(CInt(Math.Ceiling(received / 8)) - 1)
            Else
                ReDim Preserve buffer(received - 1)
            End If
            Return buffer
        End Function

        ''' <summary>
        ''' Запрашивает заданное число байтов (или битов - в зависимости от параметра <paramref name="options"/>) из ведомого устройства.
        ''' </summary>
        ''' <param name="sizeToRead">Число байтов или битов для чтения.</param>
        ''' <param name="options">Параметры передачи.</param>
        Public Function Read(sizeToRead As Integer, Optional options As SPI_TRANSFER_OPTIONS = SPI_TRANSFER_OPTIONS.SIZE_IN_BYTES Or SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE Or SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE) As Byte()
            Return Read(DeviceHandle, sizeToRead, options)
        End Function

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function SPI_Write(handle As Integer, buffer As Byte(), sizeToTransfer As Integer, ByRef sizeTransfered As Integer,
                                          Optional options As SPI_TRANSFER_OPTIONS = SPI_TRANSFER_OPTIONS.SIZE_IN_BYTES Or SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE Or SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE) As Integer
        End Function

        ''' <summary>
        ''' Передаёт в ведомое устройство заданный массив байтов.
        ''' </summary>
        ''' <param name="devHandle">Дескриптор устройства (канала).</param>
        ''' <param name="buffer">Массив байтов для записи.</param>
        ''' <param name="options">Параметры передачи.</param>
        Public Shared Sub Write(devHandle As Integer, buffer As Byte(), Optional options As SPI_TRANSFER_OPTIONS = SPI_TRANSFER_OPTIONS.SIZE_IN_BYTES Or SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE Or SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE)
            Dim buf As Byte() = buffer
            Dim transferred As Integer = 0
            Dim r As Integer = SPI_Write(devHandle, buf, buf.Length, transferred, options)
            CheckStatus(r)
        End Sub

        ''' <summary>
        ''' Передаёт в ведомое устройство заданное число битов из массива <paramref name="buffer"/>.
        ''' </summary>
        ''' <param name="devHandle">Дескриптор устройства (канала).</param>
        ''' <param name="bitsToWrite">Число битов, которые требуется записать.</param>
        ''' <param name="buffer">Массив байтов для записи.</param>
        ''' <param name="options">Параметры передачи.</param>
        Public Shared Sub WriteBits(devHandle As Integer, buffer As Byte(), bitsToWrite As Integer, Optional options As SPI_TRANSFER_OPTIONS = SPI_TRANSFER_OPTIONS.SIZE_IN_BITS Or SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE Or SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE)
            Dim buf As Byte() = buffer
            Dim transferred As Integer = 0
            Dim r As Integer = SPI_Write(devHandle, buf, bitsToWrite, transferred, options)
            CheckStatus(r)
        End Sub

        ''' <summary>
        ''' Передаёт в ведомое устройство заданный массив байтов.
        ''' </summary>
        ''' <param name="buffer">Массив байтов для записи.</param>
        ''' <param name="options">Параметры передачи.</param>
        Public Sub Write(buffer As Byte(), Optional options As SPI_TRANSFER_OPTIONS = SPI_TRANSFER_OPTIONS.SIZE_IN_BYTES Or SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE Or SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE)
            Write(DeviceHandle, buffer, options)
        End Sub

        Public Sub WriteBits(buffer As Byte(), bitsToWrite As Integer, Optional options As SPI_TRANSFER_OPTIONS = SPI_TRANSFER_OPTIONS.SIZE_IN_BITS Or SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE Or SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE)
            WriteBits(DeviceHandle, buffer, bitsToWrite, options)
        End Sub

#End Region '/SPI READ, WRITE

    End Class '/MpsseSpi

#Region "СТРУКТУРЫ И ПЕРЕЧИСЛЕНИЯ"

    ''' <summary>
    ''' Конфигурация устройства (канала) SPI.
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Public Structure SpiConfig

        ''' <summary>
        ''' Значение тактовой частоты шины SPI, в Гц. 
        ''' Значения в диапазоне от 0 до 30 МГц.
        ''' </summary>
        Public ClockRate As Integer

        ''' <summary>
        ''' Значение таймера задержек, в мс. Действительные значения 0...255. 
        ''' Рекомендуются диапазоны:
        ''' - для full-speed устройств (FT2232D): 2...255;
        ''' - для Hi-speed устройств (FT232H, FT2232H, FT4232H): 1...255.
        ''' </summary>
        Public LatencyTimer As Byte

        ''' <summary>
        ''' <list>
        ''' <item>Биты 1..0 - задают режим SPI:
        ''' - 00 - <see cref="SPI_CONFIG_OPTION.MODE0"/> - data are captured on rising edge and propagated on falling edge;
        ''' - 01 - <see cref="SPI_CONFIG_OPTION.MODE1"/> - data are captured on falling edge and propagated on rising edge;
        ''' - 10 - <see cref="SPI_CONFIG_OPTION.MODE2"/> - data are captured on falling edge and propagated on rising edge;
        ''' - 11 - <see cref="SPI_CONFIG_OPTION.MODE3"/> - data are captured on rising edge and propagated on falling edge.
        ''' </item>
        ''' <item>Биты 4..2 - задают, какие линии выбора ведомого CS будут использоваться:
        ''' - 000 - xDBUS3 <see cref="SPI_CONFIG_OPTION.CS_DBUS3"/>;
        ''' - 001 - xDBUS4 <see cref="SPI_CONFIG_OPTION.CS_DBUS4"/>;
        ''' - 010 - xDBUS5 <see cref="SPI_CONFIG_OPTION.CS_DBUS5"/>;
        ''' - 011 - xDBUS6 <see cref="SPI_CONFIG_OPTION.CS_DBUS6"/>;
        ''' - 100 - xDBUS7 <see cref="SPI_CONFIG_OPTION.CS_DBUS7"/>.
        ''' </item>
        ''' <item>Бит 5 - задаёт, каким уровнем будет осуществляться выбор ведомого:
        ''' - 0 - CS активен высоким;
        ''' - 1 - CS активен низким - <see cref="SPI_CONFIG_OPTION.CS_ACTIVELOW"/>.
        ''' </item>
        ''' <item>Биты 6..31 - резерв.</item>
        ''' </list>
        ''' </summary>
        ''' <remarks>
        ''' Обозначение xDBUS0..xDBUS7 соответствует линиям:
        ''' - ADBUS0..ADBUS7 - если используется первый канал MPSSE;
        ''' - BDBUS0..BDBUS7 - если используется второй MPSSE канал (если он есть).
        ''' </remarks>
        Public ConfigOptions As SPI_CONFIG_OPTION

        ''' <summary>
        ''' Определяет направления и значения выводов.
        ''' <list>
        ''' <item># bit 7..0 - Direction of the lines after <see cref="MpsseSpi.InitChannel(Integer, SpiConfig)"/> is called (1 = OUTPUT, 0 = INPUT)</item>
        ''' <item># bit 15..8 - Value of the lines after <see cref="MpsseSpi.InitChannel(Integer, SpiConfig)"/> is called (1 = HIGH, 0 = LOW)</item>
        ''' <item># bit 23..16 - Direction of the lines after <see cref="MpsseSpi.CloseChannel"/> is called (1 = OUTPUT, 0 = INPUT)</item>
        ''' <item># bit 31..24 - Value of the lines after <see cref="MpsseSpi.CloseChannel"/> is called (1 = HIGH, 0 = LOW)</item>
        ''' </list>
        ''' </summary>
        ''' <remarks>
        ''' Note that the directions of the SCLK, MOSI and the specified chip select line will be overwritten to 1 
        ''' and the direction of the MISO like will be overwritten to 0 irrespective of the values passed by the user application.
        ''' </remarks>
        <CLSCompliant(False)>
        Public Pin As UInteger

        ''' <summary>
        ''' Резерв.
        ''' </summary>
        <CLSCompliant(False)>
        Public Reserved As UShort

        Public Overrides Function ToString() As String
            Dim sb As New Text.StringBuilder()
            sb.AppendLine($"CLK={ClockRate} Hz")
            sb.AppendLine($"Options={ConfigOptions}")
            sb.AppendLine($"Pin={Pin}")
            Return sb.ToString()
        End Function

    End Structure

    Public Enum SpiModes As Integer
        ''' <summary>
        ''' Биты 0..1: MODE.
        ''' </summary>
        MODE0 = &H0

        ''' <summary>
        ''' Биты 0..1: MODE.
        ''' </summary>
        MODE1 = &H1

        ''' <summary>
        ''' Биты 0..1: MODE.
        ''' </summary>
        MODE2 = &H2

        ''' <summary>
        ''' Биты 0..1: MODE.
        ''' </summary>
        MODE3 = &H3
    End Enum

    ''' <summary>
    ''' Конфигурация SPI - режимы и CS.
    ''' </summary>
    <Flags()>
    Public Enum SPI_CONFIG_OPTION As Integer

        ''' <summary>
        ''' Биты 0..1: MODE.
        ''' </summary>
        MODE0 = SpiModes.MODE0

        ''' <summary>
        ''' Биты 0..1: MODE.
        ''' </summary>
        MODE1 = SpiModes.MODE1

        ''' <summary>
        ''' Биты 0..1: MODE.
        ''' </summary>
        MODE2 = SpiModes.MODE2

        ''' <summary>
        ''' Биты 0..1: MODE.
        ''' </summary>
        MODE3 = SpiModes.MODE3

        ''' <summary>
        ''' Биты 2..4: CS.
        ''' </summary>
        CS_DBUS3 = &H0

        ''' <summary>
        ''' Биты 2..4: CS.
        ''' </summary>
        CS_DBUS4 = &H4

        ''' <summary>
        ''' Биты 2..4: CS.
        ''' </summary>
        CS_DBUS5 = &H8

        ''' <summary>
        ''' Биты 2..4: CS.
        ''' </summary>
        CS_DBUS6 = &HC

        ''' <summary>
        ''' Биты 2..4: CS.
        ''' </summary>
        CS_DBUS7 = &H10

        ''' <summary>
        ''' Бит 5: Линия CS активна в состоянии LOW?
        ''' </summary>
        CS_ACTIVELOW = &H20

    End Enum

    Public Enum CsEnum As Integer
        xDBUS3 = 0
        xDBUS4 = 1
        xDBUS5 = 2
        xDBUS6 = 3
        xDBUS7 = 4
        xDBUS0 = 5 'TEST
        xDBUS1 = 6 'TEST
        xDBUS2 = 7 'TEST
    End Enum

    ''' <summary>
    ''' Настройки передачи.
    ''' </summary>
    <Flags()>
    Public Enum SPI_TRANSFER_OPTIONS As Integer

        ''' <summary>
        ''' BIT 0: Счёт передаваемых данных - в байтах.
        ''' </summary>
        SIZE_IN_BYTES = 0

        ''' <summary>
        ''' BIT 0: Счёт передаваемых данных - в битах.
        ''' </summary>
        SIZE_IN_BITS = 1

        ''' <summary>
        ''' BIT 1: Если задано, линия CS выставляется до начала передачи.
        ''' </summary>
        CHIPSELECT_ENABLE = 2

        ''' <summary>
        ''' BIT 2: Если задано, линия CS убирается после окончания передачи.
        ''' </summary>
        CHIPSELECT_DISABLE = 4

    End Enum

#End Region '/СТРУКТУРЫ И ПЕРЕЧИСЛЕНИЯ

End Namespace
