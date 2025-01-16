Imports System.Text
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq

Namespace Ftdi

    ''' <summary>
    ''' Базовый абстрактный класс для работы с устройствами FTDI в последовательных режимах SPI, I2C, JTAG с помощью библиотеки libMPSSE.dll.
    ''' </summary>
    Public MustInherit Class MpsseBase
        Inherits Ftdi.FtdiDeviceBase
        Implements IDisposable

#Region "CTORs"

        ''' <summary>
        ''' Инициализирует библиотеку LibMPSSE. С этой функции следует начать работу.
        ''' </summary>
        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Sub Init_libMPSSE()
        End Sub

        ''' <summary>
        ''' Статический конструктор, инициализирующий библиотеку.
        ''' </summary>
        Shared Sub New()
            Init_libMPSSE()
#If DEBUG Then
            Dim lastErr = Marshal.GetLastWin32Error()
            If (lastErr <> 0) Then
                Debug.WriteLine($"LAST WIN ERROR = {lastErr:X}")
            End If
#End If
        End Sub

        ''' <summary>
        ''' Запоминает индекс устройства; инициализирует библиотеку при первом вызове.
        ''' </summary>
        ''' <param name="index">Номер устройства в системе, начиная с 0.</param>
        Public Sub New(index As Integer)
            MyBase.New(index)

            AddHandler ConnectionStateChanged,
                Sub(state As Boolean)
                    Status = $"[ {NameReadable} ] {If(state, Application.Lang("mpbase_connected", "подключено"), Application.Lang("mpbase_disconnected", "отключено"))}."
                End Sub
        End Sub

#End Region '/CTORs

#Region "CONST"

        Public Const DLL_NAME As String = "libmpsse.dll"

#End Region '/CONST

#Region "READ-ONLY PROPS"

        ''' <summary>
        ''' Устройство HiSpeed или FullSpeed? 
        ''' Проверяет флаг <see cref="FlagsEnum.DEVICE_HISPEED"/>.
        ''' </summary>
        Public ReadOnly Property IsHighSpeedDevice As Boolean
            Get
                If (Not _IsHighSpeedDevice.HasValue) Then
                    Dim info As FT_DEVICE_LIST_INFO_NODE = Ftdi.Spi.MpsseSpi.GetChannelInfo(DeviceIndex)
                    _IsHighSpeedDevice = ((info.Flags And FlagsEnum.DEVICE_HISPEED) = FlagsEnum.DEVICE_HISPEED)
                End If
                Return _IsHighSpeedDevice.Value
            End Get
        End Property
        Private _IsHighSpeedDevice As Boolean? = Nothing

#End Region '/READ-ONLY PROPS

#Region "ИНСТРУМЕНТЫ"

#If INSTR Then

        ''' <summary>
        ''' Генератор частоты.
        ''' </summary>
        Public Overridable ReadOnly Property Generator As FrequencyGenerator

        ''' <summary>
        ''' Частотомер.
        ''' </summary>
        Public Overridable ReadOnly Property FreqMeter As FrequencyMeter

#End If

#End Region '/ИНСТРУМЕНТЫ

#Region "UNLOAD DLL"

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Sub Cleanup_libMPSSE()
        End Sub

        ''' <summary>
        ''' Освобождает ресурсы, используемые библиотекой.
        ''' </summary>
        Public Shared Sub CleanupLibrary()
            Cleanup_libMPSSE()
        End Sub

#End Region '/UNLOAD DLL

#Region "INFO"

        ''' <summary>
        ''' Возвращает информацию о текущем устройстве (канале).
        ''' </summary>
        Public MustOverride Function GetChannelInfo() As FT_DEVICE_LIST_INFO_NODE

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function SPI_GetNumChannels(ByRef numChannels As UInt32) As Integer
        End Function

        ''' <summary>
        ''' Возвращает количество подключённых SPI устройств.
        ''' </summary>
        Public Shared Function GetNumSpiChannels() As Integer
            Dim n As UInteger = 0UI
            Dim s As Integer = SPI_GetNumChannels(n)
            CheckStatus(s)
            Return CInt(n)
        End Function

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function I2C_GetNumChannels(ByRef numChannels As UInt32) As Integer
        End Function

        ''' <summary>
        ''' Возвращает количество подключённых I2C устройств.
        ''' </summary>
        Public Shared Function GetNumI2cChannels() As Integer
            Dim n As UInteger = 0UI
            Dim s As Integer = I2C_GetNumChannels(n)
            CheckStatus(s)
            Return CInt(n)
        End Function

#End Region '/INFO

#Region "GPIO"

        Public ReadOnly Property LastGpioValue As Integer?

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function FT_WriteGPIO(handle As Integer, dir As Byte, value As Byte) As Integer
        End Function

        ''' <summary>
        ''' Записывает в порты ввода-вывода общего назначения.
        ''' </summary>
        ''' <param name="devHandle">Описатель устройства.</param>
        ''' <param name="dir">Каждый бит представляет направление 8-ми линий порта GPIO: 0 - вход, 1 - выход.</param>
        ''' <param name="value">Каждый бит представляет состояние 8-ми линий порта GPIO: 0 - LOW, 1 - HI.</param>
        Public Shared Sub WriteGpio(devHandle As Integer, dir As Byte, value As Byte)
            Dim r As Integer = FT_WriteGPIO(devHandle, dir, value)
            CheckStatus(r)
        End Sub

        ''' <summary>
        ''' Записывает в порты ввода-вывода общего назначения.
        ''' </summary>
        ''' <param name="dir">Каждый бит представляет направление 8-ми линий порта GPIO: 0 - вход, 1 - выход.</param>
        ''' <param name="value">Каждый бит представляет состояние 8-ми линий порта GPIO: 0 - LOW, 1 - HI.</param>
        Public Sub WriteGpio(dir As Byte, value As Byte)
            Dim r As Integer = FT_WriteGPIO(DeviceHandle, dir, value)
            CheckStatus(r)
        End Sub

        <DllImport(DLL_NAME, SetLastError:=True, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function FT_ReadGPIO(handle As Integer, ByRef gpioState As Byte) As Integer
        End Function

        ''' <summary>
        ''' Читает состояние портов ввода-вывода GPIOH. Если направление линии порта - вход, то состояние всегда HI.
        ''' </summary>
        ''' <param name="devHandle">Описатель устройства.</param>
        ''' <remarks>
        ''' Использовать с осторожностью, т.к. метод может заблокировать устройство.
        ''' </remarks>
        Public Shared Function ReadGpio(devHandle As Integer) As Byte?
            '<Obsolete("Использовать с осторожностью, т.к. метод может заблокировать устройство.")>
            Dim gpioState As Byte = 0
            Dim act As New Action(
                Sub()
                    Try
                        Dim res As Integer = FT_ReadGPIO(devHandle, gpioState)
                        CheckStatus(res)
                    Catch ex As Exception
                        Debug.WriteLine(ex)
                    End Try
                End Sub)
            Dim ct As New CancellationTokenSource()
            Dim t As Task = Tasks.Task.Factory.StartNew(act, ct.Token)
            If (Not t.Wait(200)) Then
                ct.Cancel()
                Return Nothing
            End If
            Return gpioState
        End Function

        ''' <summary>
        ''' Читает состояние портов ввода-вывода GPIOH. Если направление линии порта - вход, то состояние всегда HI.
        ''' </summary>
        Public Function ReadGpio() As Byte?
            '<Obsolete("Использовать с осторожностью, т.к. метод может заблокировать устройство.")>
            Dim gp As Byte? = ReadGpio(Me.DeviceHandle)
            _LastGpioValue = gp
            Return gp

            'Dim gpioState As Byte = 0
            'Dim res As Integer = FT_ReadGPIO(DeviceHandle, gpioState)
            'CheckStatus(res)
            '_LastGpioValue = gpioState
            'Return gpioState
        End Function

        ''' <summary>
        ''' Название банка GPIOH (ACBUS/BCBUS/...).
        ''' </summary>
        Public ReadOnly Property GpioBank As String
            Get
                Dim info As FT_DEVICE_LIST_INFO_NODE = Ftdi.Spi.MpsseSpi.GetChannelInfo(DeviceIndex)
                Dim serial As String = info.SerialNumber
                If serial.EndsWith("A") Then
                    Return "ACBUS"
                ElseIf serial.EndsWith("B") Then
                    Return "BCBUS"
                Else
                    Debug.WriteLine("Gpio bank else")
                    Return ""
                End If
            End Get
        End Property

#End Region '/GPIO

#Region "IDISPOSABLE SUPPORT"

        Private DisposedValue As Boolean

        Protected Overridable Sub Dispose(disposing As Boolean)
            If (Not DisposedValue) Then
                If disposing Then
                    ' dispose managed state (managed objects).
                End If
                CloseChannel()
                ' free unmanaged resources (unmanaged objects) and override Finalize() below; set large fields to null.
            End If
            DisposedValue = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
        End Sub

#End Region '/IDISPOSABLE

#Region "СТАТУС УСТРОЙСТВА"

        ''' <summary>
        ''' Проверяет статус устройства и вызывает исключение в зависимости от кода статуса.
        ''' </summary>
        ''' <param name="status">Код статуса.</param>
        Protected Shared Sub CheckStatus(status As Integer)
            Select Case status
                Case StatusCode.FT_OK
                    Exit Sub
                Case StatusCode.FT_INVALID_HANDLE
                    Throw New NullReferenceException(Application.Lang("mpbase_err_invhandl", "Ошибка: Неверный дескриптор устройства."))
                Case StatusCode.FT_DEVICE_NOT_FOUND
                    Throw New NullReferenceException(Application.Lang("mpbase_err_nf", "Ошибка: Устройство не найдено."))
                Case StatusCode.FT_DEVICE_NOT_OPENED
                    Throw New Exception(Application.Lang("mpbase_err_nopn", "Ошибка: Устройство невозможно открыть."))
                Case StatusCode.FT_IO_ERROR
                    Throw New Exception(Application.Lang("mpbase_err_io", "Ошибка: Ошибка чтения/записи."))
                Case StatusCode.FT_INSUFFICIENT_RESOURCES
                    Throw New Exception(Application.Lang("mpbase_err_insres", "Ошибка: Недостаточно ресурсов."))
                Case StatusCode.FT_INVALID_PARAMETER
                    Throw New Exception(Application.Lang("mpbase_err_invpar", "Ошибка: Неверный параметр."))
                Case StatusCode.FT_INVALID_BAUD_RATE
                    Throw New Exception(Application.Lang("mpbase_err_invBr", "Ошибка: Неверная скорость."))
                Case StatusCode.FT_DEVICE_NOT_OPENED_FOR_ERASE
                    Throw New Exception(Application.Lang("mpbase_err_notOpenForErase", "Ошибка: Устройство не открыто для очистки."))
                Case StatusCode.FT_DEVICE_NOT_OPENED_FOR_WRITE
                    Throw New Exception(Application.Lang("mpbase_err_notOpenForWrite", "Ошибка: Устройство не открыто для записи."))
                Case StatusCode.FT_FAILED_TO_WRITE_DEVICE
                    Throw New Exception(Application.Lang("mpbase_err_failWrite", "Ошибка: Ошибка записи на устройство."))
                Case StatusCode.FT_EEPROM_READ_FAILED
                    Throw New Exception(Application.Lang("mpbase_err_eeRdFail", "Ошибка: Ошибка чтения EEPROM."))
                Case StatusCode.FT_EEPROM_WRITE_FAILED
                    Throw New Exception(Application.Lang("mpbase_err_eeWrFail", "Ошибка: Ошибка записи в EEPROM."))
                Case StatusCode.FT_EEPROM_ERASE_FAILED
                    Throw New Exception(Application.Lang("mpbase_err_eeErFail", "Ошибка стирания EEPROM."))
                Case StatusCode.FT_EEPROM_NOT_PRESENT
                    Throw New Exception(Application.Lang("mpbase_err_eeNotPres", "Ошибка: EEPROM не представлено."))
                Case StatusCode.FT_EEPROM_NOT_PROGRAMMED
                    Throw New Exception(Application.Lang("mpbase_err_eeNotProg", "Ошибка: EEPROM не запрограммировано."))
                Case StatusCode.FT_INVALID_ARGS
                    Throw New Exception(Application.Lang("mpbase_err_invArgs", "Ошибка: Неверные аргументы."))
                Case StatusCode.FT_NOT_SUPPORTED
                    Throw New Exception(Application.Lang("mpbase_err_notsup", "Ошибка: Не поддерживается."))
                Case StatusCode.FT_OTHER_ERROR
                    Throw New Exception(Application.Lang("mpbase_err_other", "Ошибка: Иная ошибка."))
                Case StatusCode.FT_DEVICE_LIST_NOT_READY
                    Throw New Exception(Application.Lang("mpbase_err_lstNotRdy", "Ошибка: Список устройств не готов."))

                Case StatusCode.FTC_FAILED_TO_COMPLETE_COMMAND
                    Throw New Exception(Application.Lang("mpbase_err_FTC_FAILED_TO_COMPLETE_COMMAND", "Ошибка: Невозможно завершить задачу."))
                Case StatusCode.FTC_FAILED_TO_SYNCHRONIZE_DEVICE_MPSSE
                    Throw New Exception(Application.Lang("mpbase_err_FTC_FAILED_TO_SYNCHRONIZE_DEVICE_MPSSE", "Ошибка: Невозможно синхронизировать устройство MPSSE."))
                Case StatusCode.FTC_INVALID_DEVICE_NAME_INDEX
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_DEVICE_NAME_INDEX", "Ошибка: Неверные имя или индекс устройства."))
                Case StatusCode.FTC_NULL_DEVICE_NAME_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_DEVICE_NAME_BUFFER_POINTER", "Ошибка: Несуществующий указатель на имя устройства."))
                Case StatusCode.FTC_DEVICE_NAME_BUFFER_TOO_SMALL
                    Throw New Exception(Application.Lang("mpbase_err_FTC_DEVICE_NAME_BUFFER_TOO_SMALL", "Ошибка: Слишком маленький буфер для имени устройства."))
                Case StatusCode.FTC_INVALID_DEVICE_NAME
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_DEVICE_NAME", "Ошибка: Неверное имя устройства."))
                Case StatusCode.FTC_INVALID_LOCATION_ID
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_LOCATION_ID", "Ошибка: Неверный Location ID."))
                Case StatusCode.FTC_DEVICE_IN_USE
                    Throw New Exception(Application.Lang("mpbase_err_FTC_DEVICE_IN_USE", "Ошибка: Устройство занято."))
                Case StatusCode.FTC_TOO_MANY_DEVICES
                    Throw New Exception(Application.Lang("mpbase_err_FTC_TOO_MANY_DEVICES", "Ошибка: Слишком много устройств."))
                Case StatusCode.FTC_NULL_CHANNEL_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_CHANNEL_BUFFER_POINTER", "Ошибка: Несуществующий указатель номера канала."))
                Case StatusCode.FTC_CHANNEL_BUFFER_TOO_SMALL
                    Throw New Exception(Application.Lang("mpbase_err_FTC_CHANNEL_BUFFER_TOO_SMALL", "Ошибка: Слишком маленький буфер для канала устройства."))
                Case StatusCode.FTC_INVALID_CHANNEL
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_CHANNEL", "Ошибка: Неверный канал."))
                Case StatusCode.FTC_INVALID_TIMER_VALUE
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_TIMER_VALUE", "Ошибка: Неверное значение таймера."))
                Case StatusCode.FTC_INVALID_CLOCK_DIVISOR
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_CLOCK_DIVISOR", "Ошибка: Неверное значение делителя частоты."))
                Case StatusCode.FTC_NULL_INPUT_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_INPUT_BUFFER_POINTER", "Ошибка: Несуществующий указатель входного буфера."))
                Case StatusCode.FTC_NULL_CHIP_SELECT_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_CHIP_SELECT_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера CS."))
                Case StatusCode.FTC_NULL_INPUT_OUTPUT_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_INPUT_OUTPUT_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера I/O."))
                Case StatusCode.FTC_NULL_OUTPUT_PINS_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_OUTPUT_PINS_BUFFER_POINTER", "Ошибка: Несуществующий указатель выходного буфера."))
                Case StatusCode.FTC_NULL_INITIAL_CONDITION_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_INITIAL_CONDITION_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера начального состояния."))
                Case StatusCode.FTC_NULL_WRITE_CONTROL_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_WRITE_CONTROL_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера контроля при записи."))
                Case StatusCode.FTC_NULL_WRITE_DATA_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_WRITE_DATA_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера данных при записи."))
                Case StatusCode.FTC_NULL_WAIT_DATA_WRITE_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_WAIT_DATA_WRITE_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера данных при записи."))
                Case StatusCode.FTC_NULL_READ_DATA_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_READ_DATA_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера данных при чтении."))
                Case StatusCode.FTC_NULL_READ_CMDS_DATA_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_READ_CMDS_DATA_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера команд при чтении."))
                Case StatusCode.FTC_INVALID_NUMBER_CONTROL_BITS
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_NUMBER_CONTROL_BITS", "Ошибка: Неверное число битов управления."))
                Case StatusCode.FTC_INVALID_NUMBER_CONTROL_BYTES
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_NUMBER_CONTROL_BYTES", "Ошибка: Неверное число байтов управления."))
                Case StatusCode.FTC_NUMBER_CONTROL_BYTES_TOO_SMALL
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NUMBER_CONTROL_BYTES_TOO_SMALL", "Ошибка: Слишком маленькое число байтов управления."))
                Case StatusCode.FTC_INVALID_NUMBER_WRITE_DATA_BITS
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_NUMBER_WRITE_DATA_BITS", "Ошибка: Неверное число битов данных при записи."))
                Case StatusCode.FTC_INVALID_NUMBER_WRITE_DATA_BYTES
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_NUMBER_WRITE_DATA_BYTES", "Ошибка: Неверное число байтов данных при записи."))
                Case StatusCode.FTC_NUMBER_WRITE_DATA_BYTES_TOO_SMALL
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NUMBER_WRITE_DATA_BYTES_TOO_SMALL", "Ошибка: Слишком маленькое число байтов данных при записи."))
                Case StatusCode.FTC_INVALID_NUMBER_READ_DATA_BITS
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_NUMBER_READ_DATA_BITS", "Ошибка: Неверное число битов данных при чтении."))
                Case StatusCode.FTC_INVALID_INIT_CLOCK_PIN_STATE
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_INIT_CLOCK_PIN_STATE", "Ошибка: Неверное начальное состояние вывода тактовой частоты."))
                Case StatusCode.FTC_INVALID_FT2232C_CHIP_SELECT_PIN
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_FT2232C_CHIP_SELECT_PIN", "Ошибка: Неверный вывод CS FT2232C."))
                Case StatusCode.FTC_INVALID_FT2232C_DATA_WRITE_COMPLETE_PIN
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_FT2232C_DATA_WRITE_COMPLETE_PIN", "Ошибка: Неверный вывод завершения записи FT2232C."))
                Case StatusCode.FTC_DATA_WRITE_COMPLETE_TIMEOUT
                    Throw New Exception(Application.Lang("mpbase_err_FTC_DATA_WRITE_COMPLETE_TIMEOUT", "Ошибка: Превышено время завершения записи."))
                Case StatusCode.FTC_INVALID_CONFIGURATION_HIGHER_GPIO_PIN
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_CONFIGURATION_HIGHER_GPIO_PIN", "Ошибка: Неверный вывод конфигурации верхних GPIO."))
                Case StatusCode.FTC_COMMAND_SEQUENCE_BUFFER_FULL
                    Throw New Exception(Application.Lang("mpbase_err_FTC_COMMAND_SEQUENCE_BUFFER_FULL", "Ошибка: Очередь команд полна."))
                Case StatusCode.FTC_NO_COMMAND_SEQUENCE
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NO_COMMAND_SEQUENCE", "Ошибка: Нет очереди команд."))
                Case StatusCode.FTC_NULL_CLOSE_FINAL_STATE_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_CLOSE_FINAL_STATE_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера закрытия финального состояния."))
                Case StatusCode.FTC_NULL_DLL_VERSION_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_DLL_VERSION_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера версии DLL."))
                Case StatusCode.FTC_DLL_VERSION_BUFFER_TOO_SMALL
                    Throw New Exception(Application.Lang("mpbase_err_FTC_DLL_VERSION_BUFFER_TOO_SMALL", "Ошибка: Слишком маленький буфер версии DLL."))
                Case StatusCode.FTC_NULL_LANGUAGE_CODE_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_LANGUAGE_CODE_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера кода языка."))
                Case StatusCode.FTC_NULL_ERROR_MESSAGE_BUFFER_POINTER
                    Throw New Exception(Application.Lang("mpbase_err_FTC_NULL_ERROR_MESSAGE_BUFFER_POINTER", "Ошибка: Несуществующий указатель буфера ошибки сообщения."))
                Case StatusCode.FTC_ERROR_MESSAGE_BUFFER_TOO_SMALL
                    Throw New Exception(Application.Lang("mpbase_err_FTC_ERROR_MESSAGE_BUFFER_TOO_SMALL", "Ошибка: Слишком маленький буфер ошибки сообщения."))
                Case StatusCode.FTC_INVALID_LANGUAGE_CODE
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_LANGUAGE_CODE", "Ошибка: Неверный код языка."))
                Case StatusCode.FTC_INVALID_STATUS_CODE
                    Throw New Exception(Application.Lang("mpbase_err_FTC_INVALID_STATUS_CODE", "Ошибка: Неверный код статуса."))
                Case StatusCode.FT_WRONG_READ_BUFFER_SIZE
                    Throw New Exception(Application.Lang("mpbase_err_FT_WRONG_READ_BUFFER_SIZE", "Ошибка: Задан неверный размер буфера чтения."))
                Case Else
                    Throw New Exception(Application.Lang("mpbase_err_unknown", "Ошибка: Неизвестная ошибка."))
            End Select
        End Sub

        ''' <summary>
        ''' Коды статуса устройства.
        ''' </summary>
        Public Enum StatusCode As Integer
            'Из библиотеки D2XX:
            FT_OK = 0
            FT_INVALID_HANDLE = 1
            FT_DEVICE_NOT_FOUND = 2
            FT_DEVICE_NOT_OPENED = 3
            FT_IO_ERROR = 4
            FT_INSUFFICIENT_RESOURCES = 5
            FT_INVALID_PARAMETER = 6
            FT_INVALID_BAUD_RATE = 7
            FT_DEVICE_NOT_OPENED_FOR_ERASE = 8
            FT_DEVICE_NOT_OPENED_FOR_WRITE = 9
            FT_FAILED_TO_WRITE_DEVICE = 10
            FT_EEPROM_READ_FAILED = 11
            FT_EEPROM_WRITE_FAILED = 12
            FT_EEPROM_ERASE_FAILED = 13
            FT_EEPROM_NOT_PRESENT = 14
            FT_EEPROM_NOT_PROGRAMMED = 15
            FT_INVALID_ARGS = 16
            FT_NOT_SUPPORTED = 17
            FT_OTHER_ERROR = 18
            FT_DEVICE_LIST_NOT_READY = 19
            FT_WRONG_READ_BUFFER_SIZE = 50
            'Из библиотеки FtcSPI:
            FTC_FAILED_TO_COMPLETE_COMMAND = 20
            FTC_FAILED_TO_SYNCHRONIZE_DEVICE_MPSSE = 21
            FTC_INVALID_DEVICE_NAME_INDEX = 22
            FTC_NULL_DEVICE_NAME_BUFFER_POINTER = 23
            FTC_DEVICE_NAME_BUFFER_TOO_SMALL = 24
            FTC_INVALID_DEVICE_NAME = 25
            FTC_INVALID_LOCATION_ID = 26
            FTC_DEVICE_IN_USE = 27
            FTC_TOO_MANY_DEVICES = 28
            FTC_NULL_CHANNEL_BUFFER_POINTER = 29
            FTC_CHANNEL_BUFFER_TOO_SMALL = 30
            FTC_INVALID_CHANNEL = 31
            FTC_INVALID_TIMER_VALUE = 32
            FTC_INVALID_CLOCK_DIVISOR = 33
            FTC_NULL_INPUT_BUFFER_POINTER = 34
            FTC_NULL_CHIP_SELECT_BUFFER_POINTER = 35
            FTC_NULL_INPUT_OUTPUT_BUFFER_POINTER = 36
            FTC_NULL_OUTPUT_PINS_BUFFER_POINTER = 37
            FTC_NULL_INITIAL_CONDITION_BUFFER_POINTER = 38
            FTC_NULL_WRITE_CONTROL_BUFFER_POINTER = 39
            FTC_NULL_WRITE_DATA_BUFFER_POINTER = 40
            FTC_NULL_WAIT_DATA_WRITE_BUFFER_POINTER = 41
            FTC_NULL_READ_DATA_BUFFER_POINTER = 42
            FTC_NULL_READ_CMDS_DATA_BUFFER_POINTER = 43
            FTC_INVALID_NUMBER_CONTROL_BITS = 44
            FTC_INVALID_NUMBER_CONTROL_BYTES = 45
            FTC_NUMBER_CONTROL_BYTES_TOO_SMALL = 46
            FTC_INVALID_NUMBER_WRITE_DATA_BITS = 47
            FTC_INVALID_NUMBER_WRITE_DATA_BYTES = 48
            FTC_NUMBER_WRITE_DATA_BYTES_TOO_SMALL = 49
            FTC_INVALID_NUMBER_READ_DATA_BITS = 50
            FTC_INVALID_INIT_CLOCK_PIN_STATE = 51
            FTC_INVALID_FT2232C_CHIP_SELECT_PIN = 52
            FTC_INVALID_FT2232C_DATA_WRITE_COMPLETE_PIN = 53
            FTC_DATA_WRITE_COMPLETE_TIMEOUT = 54
            FTC_INVALID_CONFIGURATION_HIGHER_GPIO_PIN = 55
            FTC_COMMAND_SEQUENCE_BUFFER_FULL = 56
            FTC_NO_COMMAND_SEQUENCE = 57
            FTC_NULL_CLOSE_FINAL_STATE_BUFFER_POINTER = 58
            FTC_NULL_DLL_VERSION_BUFFER_POINTER = 59
            FTC_DLL_VERSION_BUFFER_TOO_SMALL = 60
            FTC_NULL_LANGUAGE_CODE_BUFFER_POINTER = 61
            FTC_NULL_ERROR_MESSAGE_BUFFER_POINTER = 62
            FTC_ERROR_MESSAGE_BUFFER_TOO_SMALL = 63
            FTC_INVALID_LANGUAGE_CODE = 64
            FTC_INVALID_STATUS_CODE = 65
        End Enum

#End Region '/СТАТУС УСТРОЙСТВА 

    End Class '/MpsseBase

#Region "СТРУКТУРЫ И ПЕРЕЧИСЛЕНИЯ"

    ''' <summary>
    ''' Информация об устройстве.
    ''' </summary>
    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi)>
    Public Structure FT_DEVICE_LIST_INFO_NODE

        <CLSCompliant(False)>
        Public Flags As FlagsEnum

        <CLSCompliant(False)>
        Public Type As DeviceEnum

        <CLSCompliant(False)>
        Public ID As UInteger

        <CLSCompliant(False)>
        Public LocId As UInteger

        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=16)>
        Public SerialNumber As String

        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=64)>
        Public Description As String

        <CLSCompliant(False)>
        Public FtHandle As UInteger

        Public Overrides Function ToString() As String
            Dim sb As New Text.StringBuilder()
            sb.AppendLine($"Flags={Flags}")
            sb.AppendLine($"Type={Type}")
            sb.AppendLine($"ID={ID}")
            sb.AppendLine($"LocID={LocId}")
            sb.AppendLine($"S/n={SerialNumber}")
            sb.Append($"Descr={Description}")
            Return sb.ToString()
        End Function

    End Structure

#End Region '/СТРУКТУРЫ И ПЕРЕЧИСЛЕНИЯ

End Namespace
