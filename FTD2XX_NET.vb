Imports System.Runtime.InteropServices
Imports System.Runtime.Serialization
Imports System.Text
Imports System.Threading

Namespace Ftdi

    ''' <summary>
    ''' Работа с микросхемами FTDI через библиотеку FTD2XX.dll.
    ''' </summary>
    Public Class FTD2XX_NET

#Region "СВОЙСТВА ТОЛЬКО ДЛЯ ЧТЕНИЯ"

        ''' <summary>
        ''' Инициализирована ли библиотека.
        ''' </summary>
        Public Shared ReadOnly Property LibraryInitialized As Boolean
            Get
                Return (hFTD2XXDLL <> IntPtr.Zero)
            End Get
        End Property

        ''' <summary>
        ''' Открыто ли устройство.
        ''' </summary>
        Public ReadOnly Property IsOpen As Boolean
            Get
                Return (Me.ftHandle <> IntPtr.Zero)
            End Get
        End Property

        ''' <summary>
        ''' Индекс устройства в системе.
        ''' </summary>
        Public ReadOnly Property IndexInSystem As Integer
            Get
                Return _IndexInSystem
            End Get
        End Property
        Private _IndexInSystem As Integer = -1

        ''' <summary>
        ''' Выбранный режим работы устройства.
        ''' </summary>
        Public ReadOnly Property BitMode As FT_BIT_MODE
            Get
                Return _BitMode
            End Get
        End Property
        Private _BitMode As FT_BIT_MODE = FT_BIT_MODE.FT_BIT_MODE_RESET

        ''' <summary>
        ''' Включён ли делитель частоты (только для FT2232H и FT4232H).
        ''' </summary>
        Private ReadOnly Property DivideBy5 As Boolean
            Get
                Dim ty As FT_DEVICE = GetDeviceType()
                Select Case ty
                    Case FT_DEVICE.FT_DEVICE_2232H, FT_DEVICE.FT_DEVICE_4232H
                        Return _DivideBy5
                    Case Else
                        Return False
                End Select
            End Get
        End Property
        Private _DivideBy5 As Boolean = False

        ''' <summary>
        ''' Состояние внутреннего loop-back (только для FT2232H и FT4232H).
        ''' </summary>
        Public ReadOnly Property Loopback As Boolean
            Get
                Dim ty As FT_DEVICE = GetDeviceType()
                Select Case ty
                    Case FT_DEVICE.FT_DEVICE_2232H, FT_DEVICE.FT_DEVICE_4232H
                        Return _Loopback
                    Case Else
                        Return False
                End Select
            End Get
        End Property
        Private _Loopback As Boolean = False

#End Region '/СВОЙСТВА ТОЛЬКО ДЛЯ ЧТЕНИЯ

#Region "КОНСТРУКТОРЫ"

        ''' <summary>
        ''' Конструктор, использующий путь по умолчанию библиотеки FTD2XX.DLL.
        ''' </summary>
        Public Sub New()
            Me.New(FTD2XX_DLL_PATH)
        End Sub

        ''' <summary>
        ''' Конструктор с указанием пути к библиотеке.
        ''' </summary>
        ''' <param name="libPath">Путь к библиотеке.</param>
        Public Sub New(libPath As String)
            If (Not LibraryInitialized) Then
                hFTD2XXDLL = LoadLibrary(libPath)
            End If
            If LibraryInitialized Then
                FindFunctionPointers() 'TEST Проверить для нескольких одновременно открытых устройств.
            Else
                Throw New SystemException("Не удалось загрузить библиотеку FTD2XX.DLL.")
            End If
        End Sub

        ''' <summary>
        ''' Завершает работу с библиотекой и освобождает ресурсы.
        ''' </summary>
        Public Shared Sub Cleanup()
            If LibraryInitialized Then
                Try
                    FreeLibrary(hFTD2XXDLL)
                Catch
                Finally
                    hFTD2XXDLL = IntPtr.Zero
                End Try
            End If
        End Sub

#End Region '/КОНСТРУКТОРЫ

#Region "ОТКРЫТИЕ И ЗАКРЫТИЕ УСТРОЙСТВА"

        ''' <summary>
        ''' Открывает устройство по индексу.
        ''' </summary>
        ''' <param name="index">Индекс устройства в системе, начиная с 0.</param>
        Public Sub Open(ByVal index As Integer)
            If (index > -1) AndAlso (index < GetNumberOfDevices()) Then
                If LibraryInitialized Then
                    If (Me.pFT_Open <> IntPtr.Zero) AndAlso (Me.pFT_SetDataCharacteristics <> IntPtr.Zero) AndAlso (Me.pFT_SetFlowControl <> IntPtr.Zero) AndAlso (Me.pFT_SetBaudRate <> IntPtr.Zero) Then
                        Dim ft_status As FT_STATUS = FT_STATUS.FT_OK
                        If (Not Me.IsOpen) Then
                            Dim delegateForFunctionPointer As tFT_Open = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_Open, GetType(tFT_Open)), tFT_Open)
                            ft_status = delegateForFunctionPointer.Invoke(CUInt(index), Me.ftHandle)
                            If (ft_status > FT_STATUS.FT_OK) Then
                                Me.ftHandle = IntPtr.Zero
                                CheckStatus(ft_status)
                            End If
                        End If
                        If Me.IsOpen Then
                            Me._IndexInSystem = index
                            Dim uWordLength As Byte = FT_DATA_BITS.FT_BITS_8
                            Dim uStopBits As Byte = FT_STOP_BITS.FT_STOP_BITS_1
                            Dim uParity As Byte = FT_PARITY.FT_PARITY_NONE
                            Dim characteristics As tFT_SetDataCharacteristics = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetDataCharacteristics, GetType(tFT_SetDataCharacteristics)), tFT_SetDataCharacteristics)
                            ft_status = characteristics.Invoke(Me.ftHandle, uWordLength, uStopBits, uParity)
                            CheckStatus(ft_status)
                            Dim usFlowControl As UInt16 = FT_FLOW_CONTROL.FT_FLOW_NONE
                            Dim uXon As Byte = FT_DEFAULT_XON
                            Dim uXoff As Byte = FT_DEFAULT_XOFF
                            Dim control As tFT_SetFlowControl = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetFlowControl, GetType(tFT_SetFlowControl)), tFT_SetFlowControl)
                            ft_status = control.Invoke(Me.ftHandle, usFlowControl, uXon, uXoff)
                            CheckStatus(ft_status)
                            Dim dwBaudRate As UInt32 = FT_DEFAULT_BAUD_RATE
                            Dim rate As tFT_SetBaudRate = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetBaudRate, GetType(tFT_SetBaudRate)), tFT_SetBaudRate)
                            ft_status = rate.Invoke(Me.ftHandle, dwBaudRate)
                            CheckStatus(ft_status)
                        End If
                    Else
                        If (Me.pFT_Open = IntPtr.Zero) Then
                            Throw New SystemException("Сбой при загрузке функции FT_Open().")
                        End If
                        If (Me.pFT_SetDataCharacteristics = IntPtr.Zero) Then
                            Throw New SystemException("Сбой при загрузке функции FT_SetDataCharacteristics().")
                        End If
                        If (Me.pFT_SetFlowControl = IntPtr.Zero) Then
                            Throw New SystemException("Сбой при загрузке функции FT_SetFlowControl().")
                        End If
                        If (Me.pFT_SetBaudRate = IntPtr.Zero) Then
                            Throw New SystemException("Сбой при загрузке функции FT_SetBaudRate().")
                        End If
                    End If
                End If
            Else
                Throw New ArgumentException("Недопустимый индекс устройства.")
            End If
        End Sub

        ''' <summary>
        ''' Открывает устройство по описанию.
        ''' </summary>
        ''' <param name="description">Описание устройства.</param>
        Public Sub Open(ByVal description As String)
            If LibraryInitialized Then
                If (Me.pFT_OpenEx <> IntPtr.Zero) AndAlso (Me.pFT_SetDataCharacteristics <> IntPtr.Zero) AndAlso (Me.pFT_SetFlowControl <> IntPtr.Zero) AndAlso (Me.pFT_SetBaudRate <> IntPtr.Zero) Then
                    Dim delegateForFunctionPointer As tFT_OpenEx = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_OpenEx, GetType(tFT_OpenEx)), tFT_OpenEx)
                    Dim characteristics As tFT_SetDataCharacteristics = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetDataCharacteristics, GetType(tFT_SetDataCharacteristics)), tFT_SetDataCharacteristics)
                    Dim control As tFT_SetFlowControl = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetFlowControl, GetType(tFT_SetFlowControl)), tFT_SetFlowControl)
                    Dim rate As tFT_SetBaudRate = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetBaudRate, GetType(tFT_SetBaudRate)), tFT_SetBaudRate)
                    Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(description, 2, Me.ftHandle)
                    If (ft_status > FT_STATUS.FT_OK) Then
                        Me.ftHandle = IntPtr.Zero
                    End If
                    If (Me.IsOpen) Then
                        Dim uWordLength As Byte = 8
                        Dim uStopBits As Byte = 0
                        Dim uParity As Byte = 0
                        ft_status = characteristics.Invoke(Me.ftHandle, uWordLength, uStopBits, uParity)
                        CheckStatus(ft_status)
                        Dim usFlowControl As UInt16 = 0
                        Dim uXon As Byte = &H11
                        Dim uXoff As Byte = &H13
                        ft_status = control.Invoke(Me.ftHandle, usFlowControl, uXon, uXoff)
                        CheckStatus(ft_status)
                        Dim dwBaudRate As UInt32 = FT_DEFAULT_BAUD_RATE
                        ft_status = rate.Invoke(Me.ftHandle, dwBaudRate)
                        CheckStatus(ft_status)
                    End If
                Else
                    If (Me.pFT_OpenEx = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_OpenEx().")
                    End If
                    If (Me.pFT_SetDataCharacteristics = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetDataCharacteristics().")
                    End If
                    If (Me.pFT_SetFlowControl = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetFlowControl().")
                    End If
                    If (Me.pFT_SetBaudRate = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetBaudRate().")
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Открывает устройство по Location.
        ''' </summary>
        ''' <param name="location">Location устройства.</param>
        Public Sub Open(ByVal location As UInt32)
            If LibraryInitialized Then
                If (Me.pFT_OpenEx <> IntPtr.Zero) AndAlso (Me.pFT_SetDataCharacteristics <> IntPtr.Zero) AndAlso (Me.pFT_SetFlowControl <> IntPtr.Zero) AndAlso (Me.pFT_SetBaudRate <> IntPtr.Zero) Then
                    Dim delegateForFunctionPointer As tFT_OpenExLoc = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_OpenEx, GetType(tFT_OpenExLoc)), tFT_OpenExLoc)
                    Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(location, 4, Me.ftHandle)
                    If (ft_status > FT_STATUS.FT_OK) Then
                        Me.ftHandle = IntPtr.Zero
                    End If
                    If (Me.IsOpen) Then
                        Dim uWordLength As Byte = FT_DATA_BITS.FT_BITS_8
                        Dim uStopBits As Byte = FT_STOP_BITS.FT_STOP_BITS_1
                        Dim uParity As Byte = FT_PARITY.FT_PARITY_NONE
                        Dim characteristics As tFT_SetDataCharacteristics = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetDataCharacteristics, GetType(tFT_SetDataCharacteristics)), tFT_SetDataCharacteristics)
                        ft_status = characteristics.Invoke(Me.ftHandle, uWordLength, uStopBits, uParity)
                        CheckStatus(ft_status)
                        Dim usFlowControl As UInt16 = FT_FLOW_CONTROL.FT_FLOW_NONE
                        Dim uXon As Byte = FT_DEFAULT_XON
                        Dim uXoff As Byte = FT_DEFAULT_XOFF
                        Dim control As tFT_SetFlowControl = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetFlowControl, GetType(tFT_SetFlowControl)), tFT_SetFlowControl)
                        ft_status = control.Invoke(Me.ftHandle, usFlowControl, uXon, uXoff)
                        CheckStatus(ft_status)
                        Dim dwBaudRate As UInt32 = FT_DEFAULT_BAUD_RATE
                        Dim rate As tFT_SetBaudRate = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetBaudRate, GetType(tFT_SetBaudRate)), tFT_SetBaudRate)
                        ft_status = rate.Invoke(Me.ftHandle, dwBaudRate)
                        CheckStatus(ft_status)
                    End If
                Else
                    If (Me.pFT_OpenEx = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_OpenEx().")
                    End If
                    If (Me.pFT_SetDataCharacteristics = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetDataCharacteristics().")
                    End If
                    If (Me.pFT_SetFlowControl = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetFlowControl().")
                    End If
                    If (Me.pFT_SetBaudRate = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetBaudRate().")
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Открывает устройство по серийному номеру.
        ''' </summary>
        ''' <param name="serialNumber">Серийный номер устройства.</param>
        Public Sub Open(ByVal serialNumber As StringBuilder)
            If LibraryInitialized Then
                If (Me.pFT_OpenEx <> IntPtr.Zero) AndAlso (Me.pFT_SetDataCharacteristics <> IntPtr.Zero) AndAlso (Me.pFT_SetFlowControl <> IntPtr.Zero) AndAlso (Me.pFT_SetBaudRate <> IntPtr.Zero) Then
                    Dim delegateForFunctionPointer As tFT_OpenEx = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_OpenEx, GetType(tFT_OpenEx)), tFT_OpenEx)
                    Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(serialNumber.ToString(), 1, Me.ftHandle)
                    If (ft_status > FT_STATUS.FT_OK) Then
                        Me.ftHandle = IntPtr.Zero
                    Else
                        Dim uWordLength As Byte = FT_DATA_BITS.FT_BITS_8
                        Dim uStopBits As Byte = FT_STOP_BITS.FT_STOP_BITS_1
                        Dim uParity As Byte = FT_PARITY.FT_PARITY_NONE
                        Dim characteristics As tFT_SetDataCharacteristics = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetDataCharacteristics, GetType(tFT_SetDataCharacteristics)), tFT_SetDataCharacteristics)
                        ft_status = characteristics.Invoke(Me.ftHandle, uWordLength, uStopBits, uParity)
                        CheckStatus(ft_status)
                        Dim usFlowControl As UInt16 = FT_FLOW_CONTROL.FT_FLOW_NONE
                        Dim uXon As Byte = FT_DEFAULT_XON
                        Dim uXoff As Byte = FT_DEFAULT_XOFF
                        Dim control As tFT_SetFlowControl = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetFlowControl, GetType(tFT_SetFlowControl)), tFT_SetFlowControl)
                        ft_status = control.Invoke(Me.ftHandle, usFlowControl, uXon, uXoff)
                        CheckStatus(ft_status)
                        Dim dwBaudRate As UInt32 = FT_DEFAULT_BAUD_RATE
                        Dim rate As tFT_SetBaudRate = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetBaudRate, GetType(tFT_SetBaudRate)), tFT_SetBaudRate)
                        ft_status = rate.Invoke(Me.ftHandle, dwBaudRate)
                        CheckStatus(ft_status)
                    End If
                Else
                    If (Me.pFT_OpenEx = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_OpenEx().")
                    End If
                    If (Me.pFT_SetDataCharacteristics = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetDataCharacteristics().")
                    End If
                    If (Me.pFT_SetFlowControl = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetFlowControl().")
                    End If
                    If (Me.pFT_SetBaudRate = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetBaudRate().")
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Закрывает устройство, если оно открыто.
        ''' </summary>
        Public Sub Close()
            If LibraryInitialized Then
                If Me.IsOpen Then
                    If (Me.pFT_Close <> IntPtr.Zero) Then
                        Dim delegateForFunctionPointer As tFT_Close = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_Close, GetType(tFT_Close)), tFT_Close)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle)
                        CheckStatus(ft_status)
                        Me.ftHandle = IntPtr.Zero
                        Me._IndexInSystem = -1
                        Me._BitMode = FT_BIT_MODE.FT_BIT_MODE_RESET
                    Else
                        Throw New SystemException("Сбой при загрузке функции FT_Close().")
                    End If
                End If
            End If
        End Sub

#End Region '/ОТКРЫТИЕ И ЗАКРЫТИЕ УСТРОЙСТВА

#Region "СТАТИЧЕСКИЕ МЕТОДЫ"

        ''' <summary>
        ''' Возвращает количество FTDI устройств в системе.
        ''' </summary>
        Public Shared Function GetNumberOfDevices() As Integer
            Dim devCount As UInteger = 0
            If LibraryInitialized Then
                If (pFT_CreateDeviceInfoList <> IntPtr.Zero) Then
                    Dim delegateForFunctionPointer As tFT_CreateDeviceInfoList = DirectCast(Marshal.GetDelegateForFunctionPointer(pFT_CreateDeviceInfoList, GetType(tFT_CreateDeviceInfoList)), tFT_CreateDeviceInfoList)
                    Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(devCount)
                    CheckStatus(ft_status)
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_CreateDeviceInfoList().")
                End If
            End If
            Return CInt(devCount)
        End Function

        ''' <summary>
        ''' Получает список FTDI устройств в системе.
        ''' </summary>
        Public Shared Function GetDeviceList() As FT_DEVICE_INFO_NODE()
            Dim devicelist() As FT_DEVICE_INFO_NODE = New FT_DEVICE_INFO_NODE() {}
            If LibraryInitialized Then
                If (pFT_CreateDeviceInfoList <> IntPtr.Zero) AndAlso (pFT_GetDeviceInfoDetail <> IntPtr.Zero) Then
                    Dim numdevs As UInteger = 0
                    Dim delegateForFunctionPointer As tFT_CreateDeviceInfoList = DirectCast(Marshal.GetDelegateForFunctionPointer(pFT_CreateDeviceInfoList, GetType(tFT_CreateDeviceInfoList)), tFT_CreateDeviceInfoList)
                    Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(numdevs)
                    CheckStatus(ftStatus)
                    If (numdevs > 0) Then
                        ReDim devicelist(CInt(numdevs - 1))
                        For i As Integer = 0 To CInt(numdevs - 1)
                            Dim serialnumber As Byte() = New Byte(&H10 - 1) {}
                            Dim description As Byte() = New Byte(&H40 - 1) {}
                            Dim detail As tFT_GetDeviceInfoDetail = DirectCast(Marshal.GetDelegateForFunctionPointer(pFT_GetDeviceInfoDetail, GetType(tFT_GetDeviceInfoDetail)), tFT_GetDeviceInfoDetail)
                            devicelist(i) = New FT_DEVICE_INFO_NODE
                            ftStatus = detail.Invoke(CUInt(i), devicelist(i).Flags, devicelist(i).Type, devicelist(i).ID, devicelist(i).LocId, serialnumber, description, devicelist(i).ftHandle)
                            CheckStatus(ftStatus)
                            devicelist(i).SerialNumber = Encoding.ASCII.GetString(serialnumber)
                            devicelist(i).Description = Encoding.ASCII.GetString(description)
                            Dim length As Integer = devicelist(i).SerialNumber.IndexOf(ChrW(0))
                            If (length <> -1) Then
                                devicelist(i).SerialNumber = devicelist(i).SerialNumber.Substring(0, length)
                            End If
                            length = devicelist(i).Description.IndexOf(ChrW(0))
                            If (length <> -1) Then
                                devicelist(i).Description = devicelist(i).Description.Substring(0, length)
                            End If
                        Next
                    End If
                Else
                    If (pFT_CreateDeviceInfoList = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_CreateDeviceInfoList().")
                    End If
                    If (pFT_GetDeviceInfoDetail = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_GetDeviceInfoListDetail().")
                    End If
                End If
            End If
            Return devicelist
        End Function

        ''' <summary>
        ''' Возвращает версию библиотеки.
        ''' </summary>
        Public Shared Function GetLibraryVersion() As String
            Dim libraryVersion As UInt32
            If LibraryInitialized Then
                If (pFT_GetLibraryVersion <> IntPtr.Zero) Then
                    Dim delegateForFunctionPointer As tFT_GetLibraryVersion = DirectCast(Marshal.GetDelegateForFunctionPointer(pFT_GetLibraryVersion, GetType(tFT_GetLibraryVersion)), tFT_GetLibraryVersion)
                    Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(libraryVersion)
                    CheckStatus(ft_status)
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetLibraryVersion().")
                End If
            End If
            Dim ver As Byte() = BitConverter.GetBytes(libraryVersion)
            Dim libVer As String = String.Format("{0}.{1}.{2}", ver(2).ToString("X"), ver(1).ToString("X"), ver(0).ToString("X"))
            Return libVer
        End Function

        ''' <summary>
        ''' Принудительно перезагружает драйвер для устройства с заданными VID и PID.
        ''' </summary>
        ''' <param name="vendorID">Vendor ID устройства, для которого нужно перезагрузить драйвер.</param>
        ''' <param name="productID">Product ID устройства, для которого нужно перезагрузить драйвер.</param>
        Public Shared Sub ReloadDriver(ByVal vendorID As UInt16, ByVal productID As UInt16)
            If LibraryInitialized Then
                If (pFT_Reload <> IntPtr.Zero) Then
                    Dim delegateForFunctionPointer As tFT_Reload = DirectCast(Marshal.GetDelegateForFunctionPointer(pFT_Reload, GetType(tFT_Reload)), tFT_Reload)
                    Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(vendorID, productID)
                    CheckStatus(ft_status)
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_Reload().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Используется для программного восстановления устройств.
        ''' </summary>
        Public Shared Sub RescanDevices()
            If LibraryInitialized Then
                If (pFT_Rescan <> IntPtr.Zero) Then
                    Dim delegateForFunctionPointer As tFT_Rescan = DirectCast(Marshal.GetDelegateForFunctionPointer(pFT_Rescan, GetType(tFT_Rescan)), tFT_Rescan)
                    Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke()
                    CheckStatus(ft_status)
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_Rescan().")
                End If
            End If
        End Sub

#End Region '/СТАТИЧЕСКИЕ МЕТОДЫ

#Region "ИНФОРМАЦИЯ ОБ УСТРОЙСТВЕ"

        ''' <summary>
        ''' Возвращает имя COM-порта, ассоциированного с устройством.
        ''' </summary>
        Public Function GetComPort() As String
            Dim comPortName As String = String.Empty
            If LibraryInitialized Then
                If (Me.pFT_GetComPortNumber <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim dwComPortNumber As Integer = FT_COM_PORT_NOT_ASSIGNED
                        Dim delegateForFunctionPointer As tFT_GetComPortNumber = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetComPortNumber, GetType(tFT_GetComPortNumber)), tFT_GetComPortNumber)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, dwComPortNumber)
                        CheckStatus(ft_status)
                        If (dwComPortNumber <> FT_COM_PORT_NOT_ASSIGNED) Then
                            comPortName = ("COM" & dwComPortNumber.ToString())
                        End If
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetComPortNumber().")
                End If
            End If
            Return comPortName
        End Function

        ''' <summary>
        ''' Возвращает описание устройства.
        ''' </summary>
        Public Function GetDescription() As String
            Dim description As String = String.Empty
            If LibraryInitialized Then
                If (Me.pFT_GetDeviceInfo <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim lpdwID As UInt32 = FT_DEFAULT_DEVICE_ID
                        Dim pcSerialNumber As Byte() = New Byte(&H10 - 1) {}
                        Dim pcDescription As Byte() = New Byte(&H40 - 1) {}
                        Dim pftType As FT_DEVICE = FT_DEVICE.FT_DEVICE_UNKNOWN
                        Dim delegateForFunctionPointer As tFT_GetDeviceInfo = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetDeviceInfo, GetType(tFT_GetDeviceInfo)), tFT_GetDeviceInfo)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pftType, lpdwID, pcSerialNumber, pcDescription, IntPtr.Zero)
                        CheckStatus(ft_status)
                        description = Encoding.ASCII.GetString(pcDescription)
                        description = description.Substring(0, description.IndexOf(ChrW(0)))
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetDeviceInfo().")
                End If
            End If
            Return description
        End Function

        ''' <summary>
        ''' Возвращает Device ID в виде числа 0x04036010, где старшие 2 байта - VID, младшие - PID.
        ''' </summary>
        Public Function GetDeviceId() As Long
            Dim deviceID As UInt32 = FT_DEFAULT_DEVICE_ID
            If LibraryInitialized Then
                If (Me.pFT_GetDeviceInfo <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim pftType As FT_DEVICE = FT_DEVICE.FT_DEVICE_UNKNOWN
                        Dim pcSerialNumber As Byte() = New Byte(&H10 - 1) {}
                        Dim pcDescription As Byte() = New Byte(&H40 - 1) {}
                        Dim delegateForFunctionPointer As tFT_GetDeviceInfo = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetDeviceInfo, GetType(tFT_GetDeviceInfo)), tFT_GetDeviceInfo)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pftType, deviceID, pcSerialNumber, pcDescription, IntPtr.Zero)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetDeviceInfo().")
                End If
            End If
            Return deviceID
        End Function

        ''' <summary>
        ''' Возвращает тип устройства.
        ''' </summary>
        Public Function GetDeviceType() As FT_DEVICE
            Dim deviceType As FT_DEVICE = FT_DEVICE.FT_DEVICE_UNKNOWN
            If LibraryInitialized Then
                If (Me.pFT_GetDeviceInfo <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim lpdwID As UInt32 = FT_DEFAULT_DEVICE_ID
                        Dim pcSerialNumber As Byte() = New Byte(&H10 - 1) {}
                        Dim pcDescription As Byte() = New Byte(&H40 - 1) {}
                        Dim delegateForFunctionPointer As tFT_GetDeviceInfo = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetDeviceInfo, GetType(tFT_GetDeviceInfo)), tFT_GetDeviceInfo)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(ftHandle, deviceType, lpdwID, pcSerialNumber, pcDescription, IntPtr.Zero)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetDeviceInfo().")
                End If
            End If
            Return deviceType
        End Function

        ''' <summary>
        ''' Возвращает версию драйвера (устройство должно быть открыто).
        ''' </summary>
        Public Function GetDriverVersion() As String
            Dim drvVer As String = String.Empty
            If LibraryInitialized Then
                If (pFT_GetDriverVersion <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim driverVersion As UInt32 = 0
                        Dim delegateForFunctionPointer As tFT_GetDriverVersion = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetDriverVersion, GetType(tFT_GetDriverVersion)), tFT_GetDriverVersion)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, driverVersion)
                        CheckStatus(ft_status)
                        Dim drvVerBytes As Byte() = BitConverter.GetBytes(driverVersion)
                        drvVer = String.Format("{0}.{1}.{2}", drvVerBytes(2).ToString("X"), drvVerBytes(1).ToString("X"), drvVerBytes(0).ToString("X"))
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetDriverVersion().")
                End If
            End If
            Return drvVer
        End Function

        ''' <summary>
        ''' Возвращает тип события, которое определяется в <see cref="FT_EVENTS">FT_EVENTS</see>.
        ''' </summary>
        Public Function GetEventType() As Long
            Dim eventType As UInt32
            If LibraryInitialized Then
                If (Me.pFT_GetStatus <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim lpdwAmountInRxQueue As UInt32 = 0
                        Dim lpdwAmountInTxQueue As UInt32 = 0
                        Dim delegateForFunctionPointer As tFT_GetStatus = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetStatus, GetType(tFT_GetStatus)), tFT_GetStatus)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, lpdwAmountInRxQueue, lpdwAmountInTxQueue, eventType)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetStatus().")
                End If
            End If
            Return eventType
        End Function

        ''' <summary>
        ''' Возвращает задержку (latency), мс.
        ''' </summary>
        Public Function GetLatency() As Byte
            Dim latency As Byte = FT_DEFAULT_LATENCY
            If LibraryInitialized Then
                If (Me.pFT_GetLatencyTimer <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_GetLatencyTimer = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetLatencyTimer, GetType(tFT_GetLatencyTimer)), tFT_GetLatencyTimer)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, latency)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetLatencyTimer().")
                End If
            End If
            Return latency
        End Function

        ''' <summary>
        ''' Возвращает статус линии, который определён в <see cref="FT_LINE_STATUS">FT_LINE_STATUS</see>.
        ''' </summary>
        Public Function GetLineStatus() As Byte
            Dim lineStatus As Byte = 0
            If LibraryInitialized Then
                If (Me.pFT_GetModemStatus <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim lpdwModemStatus As UInt32 = 0
                        Dim delegateForFunctionPointer As tFT_GetModemStatus = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetModemStatus, GetType(tFT_GetModemStatus)), tFT_GetModemStatus)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, lpdwModemStatus)
                        CheckStatus(ft_status)
                        lineStatus = Convert.ToByte((lpdwModemStatus >> 8) And &HFF)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetModemStatus().")
                End If
            End If
            Return lineStatus
        End Function

        ''' <summary>
        ''' Возвращает статус модема, который определён в <see cref="FT_MODEM_STATUS">FT_MODEM_STATUS</see>.
        ''' </summary>
        Public Function GetModemStatus() As Byte
            Dim modemStatus As Byte = 0
            If LibraryInitialized Then
                If (Me.pFT_GetModemStatus <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim lpdwModemStatus As UInt32 = 0
                        Dim delegateForFunctionPointer As tFT_GetModemStatus = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetModemStatus, GetType(tFT_GetModemStatus)), tFT_GetModemStatus)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, lpdwModemStatus)
                        CheckStatus(ft_status)
                        modemStatus = Convert.ToByte(lpdwModemStatus And &HFF)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetModemStatus().")
                End If
            End If
            Return modemStatus
        End Function

        ''' <summary>
        ''' Возвращает состояние выводов.
        ''' </summary>
        Public Function GetDataBusValue() As Byte
            Dim bitMode As Byte = 0
            If LibraryInitialized Then
                If (Me.pFT_GetBitMode <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_GetBitMode = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetBitMode, GetType(tFT_GetBitMode)), tFT_GetBitMode)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, bitMode)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetBitMode().")
                End If
            End If
            Return bitMode
        End Function

        ''' <summary>
        ''' Возвращает количество доступных байтов в буфере чтения.
        ''' </summary>
        Public Function GetRxBytesAvailable() As Long
            Dim rxQueue As UInt32 = 0
            If LibraryInitialized Then
                If (Me.pFT_GetQueueStatus <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_GetQueueStatus = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetQueueStatus, GetType(tFT_GetQueueStatus)), tFT_GetQueueStatus)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, rxQueue)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetQueueStatus().")
                End If
            End If
            Return rxQueue
        End Function

        ''' <summary>
        ''' Возвращает количество байтов в буфере передачи.
        ''' </summary>
        Public Function GetTxBytesWaiting() As Long
            Dim txQueue As UInt32
            If LibraryInitialized Then
                If (Me.pFT_GetStatus <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim lpdwAmountInRxQueue As UInt32 = 0
                        Dim lpdwEventStatus As UInt32 = 0
                        Dim delegateForFunctionPointer As tFT_GetStatus = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetStatus, GetType(tFT_GetStatus)), tFT_GetStatus)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, lpdwAmountInRxQueue, txQueue, lpdwEventStatus)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetStatus().")
                End If
            End If
            Return txQueue
        End Function

        ''' <summary>
        ''' Возвращает серийный номер устройства.
        ''' </summary>
        Public Function GetSerialNumber() As String
            Dim serialNumber As String = String.Empty
            If LibraryInitialized Then
                If (Me.pFT_GetDeviceInfo <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim lpdwID As UInt32 = 0
                        Dim pftType As FT_DEVICE = FT_DEVICE.FT_DEVICE_UNKNOWN
                        Dim pcSerialNumber As Byte() = New Byte(&H10 - 1) {}
                        Dim pcDescription As Byte() = New Byte(&H40 - 1) {}
                        Dim delegateForFunctionPointer As tFT_GetDeviceInfo = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_GetDeviceInfo, GetType(tFT_GetDeviceInfo)), tFT_GetDeviceInfo)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pftType, lpdwID, pcSerialNumber, pcDescription, IntPtr.Zero)
                        CheckStatus(ft_status)
                        serialNumber = Encoding.ASCII.GetString(pcSerialNumber) 'null-terminated string
                        serialNumber = serialNumber.Substring(0, serialNumber.IndexOf(ChrW(0)))
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_GetDeviceInfo().")
                End If
            End If
            Return serialNumber
        End Function

        ''' <summary>
        ''' Возвращает номер канала, если устройство многоканальное (A, B...).
        ''' </summary>
        Private Function GetInterfaceIdentifier() As String
            Dim channel As String = String.Empty
            If Me.IsOpen Then
                Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                If (deviceType = FT_DEVICE.FT_DEVICE_2232) OrElse (deviceType = FT_DEVICE.FT_DEVICE_2232H) OrElse (deviceType = FT_DEVICE.FT_DEVICE_4232H) Then
                    channel = GetDescription()
                    channel = channel.Substring(channel.Length - 1)
                End If
            Else
                CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
            End If
            Return channel
        End Function

#End Region '/ИНФОРМАЦИЯ ОБ УСТРОЙСТВЕ

#Region "УПРАВЛЕНИЕ УСТРОЙСТВОМ"

        ''' <summary>
        ''' Посылает команду USB порту на переподключение устройств.
        ''' </summary>
        ''' <remarks>
        ''' Эффект от этой функции такой же, как от отключения и повторного подключения устройства к USB порту.
        ''' Используется при возниконовении фатальных ошибок, которые невозможно исправить иначе как переподключением устрйоства.
        ''' Также может использоваться после перепрограммирования ПЗУ, чтобы заставить устройство прочитать новое содержимое ПЗУ.
        ''' После вызова этой функции приложение обнуляет дескриптор устройства, поэтому приложение должно заново открыть устройство.
        ''' Для FT4232H, FT2232H и FT2232 функция работает только с Windows XP и более поздними ОС.
        ''' </remarks>
        Public Sub ResetDriver()
            If LibraryInitialized Then
                If (Me.pFT_CyclePort <> IntPtr.Zero) AndAlso (Me.pFT_Close <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_CyclePort = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_CyclePort, GetType(tFT_CyclePort)), tFT_CyclePort)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle)
                        If (ft_status = FT_STATUS.FT_OK) Then
                            Dim close As tFT_Close = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_Close, GetType(tFT_Close)), tFT_Close)
                            ft_status = close.Invoke(Me.ftHandle)
                            If (ft_status = FT_STATUS.FT_OK) Then
                                Me.ftHandle = IntPtr.Zero
                            End If
                        End If
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    If (Me.pFT_CyclePort = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_CyclePort().")
                    End If
                    If (Me.pFT_Close = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_Close().")
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Посылает команду сброса устройству.
        ''' </summary>
        Public Sub ResetDevice()
            If LibraryInitialized Then
                If (Me.pFT_ResetDevice <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_ResetDevice = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_ResetDevice, GetType(tFT_ResetDevice)), tFT_ResetDevice)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_ResetDevice().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Посылает команду сброса порту.
        ''' </summary>
        Public Sub ResetPort()
            If LibraryInitialized Then
                If (Me.pFT_ResetPort <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_ResetPort = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_ResetPort, GetType(tFT_ResetPort)), tFT_ResetPort)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_ResetPort().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Задаёт размер входящего и исходящего буферов USB.
        ''' </summary>
        ''' <param name="transferInSize">Размер буфера для запроса входящих байтов. Должен быть кратен 64 и лежать в диапазоне от 64 байт до 64 кб.</param>
        ''' <param name="transferOutSize">Размер буфера для запроса исходящих байтов. Должен быть кратен 64 и лежать в диапазоне от 64 байт до 64 кб.</param>
        ''' <remarks>Используется для изменения размера с рамера по умолчанию (4096 байт) до более подходящих приложению значений.
        ''' Когда вызывается эта функция, изменения применяются незамедлительно, и все данные, которые были в буфере в это время, теряются.
        ''' Примечание: в настоящее время поддерживается только задание размера входящих байтов.</remarks>
        Public Sub SetUsbTransferSize(Optional ByVal transferInSize As UInt32 = USB_DEFAULT_IN_TRANSFER_SIZE, Optional transferOutSize As UInt32 = USB_DEFAULT_OUT_TRANSFER_SIZE)
            If (transferInSize < USB_DEFAULT_IN_SIZE_MIN) OrElse (transferInSize > USB_DEFAULT_IN_SIZE_MAX) Then
                Throw New SystemException("задан некорректный размер входящего буфера.")
            End If
            If (transferOutSize < USB_DEFAULT_OUT_SIZE_MIN) OrElse (transferOutSize > USB_DEFAULT_OUT_SIZE_MAX) Then
                Throw New SystemException("задан некорректный размер исходящего буфера.")
            End If
            If LibraryInitialized Then
                If (Me.pFT_SetUSBParameters <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_SetUSBParameters = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetUSBParameters, GetType(tFT_SetUSBParameters)), tFT_SetUSBParameters)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, transferInSize, transferOutSize)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_SetUSBParameters().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Очищает буферы приёма и/или передачи устройства.
        ''' </summary>
        ''' <param name="purgemask">Комбинация флагов <see cref="FT_PURGE" />.</param>
        Public Sub Purge(ByVal purgemask As FT_PURGE)
            If LibraryInitialized Then
                If (Me.pFT_Purge <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_Purge = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_Purge, GetType(tFT_Purge)), tFT_Purge)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, purgemask)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_Purge().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' ЧТО-ТО НЕИЗВЕСТНОЕ.
        ''' </summary>
        ''' <param name="request"></param>
        ''' <param name="buf"></param>
        ''' <param name="len"></param>
        Public Function VendorCmdGet(ByVal request As UInt16, ByVal buf As Byte(), ByVal len As UInt16) As FT_STATUS
            Dim ft_status As FT_STATUS = FT_STATUS.FT_OTHER_ERROR
            If LibraryInitialized Then
                If (Me.pFT_VendorCmdGet <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_VendorCmdGet = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_VendorCmdGet, GetType(tFT_VendorCmdGet)), tFT_VendorCmdGet)
                        ft_status = delegateForFunctionPointer.Invoke(Me.ftHandle, request, buf, len)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_VendorCmdGet().")
                End If
            End If
            Return ft_status
        End Function

        Public Sub VendorCmdSet(ByVal request As UInt16, ByVal buf As Byte(), ByVal len As UInt16)
            If LibraryInitialized Then
                If (Me.pFT_VendorCmdSet <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_VendorCmdSet = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_VendorCmdSet, GetType(tFT_VendorCmdSet)), tFT_VendorCmdSet)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, request, buf, len)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_VendorCmdSet().")
                End If
            End If
        End Sub

#End Region '/УПРАВЛЕНИЕ УСТРОЙСТВОМ

#Region "ВЫСТАВЛЕНИЕ ПАРАМЕТРОВ"

        ''' <summary>
        ''' Задаёт скорость обмена с устрйоством.
        ''' </summary>
        ''' <param name="baudRate">Значение скорости из <see cref="FT_BAUD_RATE"/>.</param>
        Public Sub SetBaudRate(ByVal baudRate As FT_BAUD_RATE)
            If LibraryInitialized Then
                If (Me.pFT_SetBaudRate <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_SetBaudRate = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetBaudRate, GetType(tFT_SetBaudRate)), tFT_SetBaudRate)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, baudRate)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_SetBaudRate().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Задаёт битовый режим устройства.
        ''' </summary>
        ''' <param name="mask">Маска для определения входов и выходов: бит 0 - вход, 1 - выход.
        ''' В режиме CBUS Bit Bang, старший полубайт задаёт входы и выходы, а младший - состояния выходов.</param>
        ''' <param name="bitMode">Битовый режим <see cref="FT_BIT_MODE"/>.</param>
        Public Sub SetBitMode(ByVal mask As Byte, ByVal bitMode As FT_BIT_MODE)
            If LibraryInitialized Then
                If (Me.pFT_SetBitMode <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim deviceType As FT_DEVICE = GetDeviceType()
                        Select Case deviceType
                            Case FT_DEVICE.FT_DEVICE_AM
                                CheckStatus(FT_ERROR.FT_INVALID_BITMODE)
                            Case FT_DEVICE.FT_DEVICE_100AX
                                CheckStatus(FT_ERROR.FT_INVALID_BITMODE)
                            Case Else
                                If (deviceType = FT_DEVICE.FT_DEVICE_BM) AndAlso (bitMode > 0) Then
                                    If ((bitMode And 1) = 0) Then
                                        CheckStatus(FT_ERROR.FT_INVALID_BITMODE)
                                    End If
                                ElseIf (deviceType = FT_DEVICE.FT_DEVICE_2232) AndAlso (bitMode > 0) Then
                                    If ((bitMode And &H1F) = 0) Then
                                        CheckStatus(FT_ERROR.FT_INVALID_BITMODE)
                                    End If
                                    If (bitMode = 2) AndAlso (GetInterfaceIdentifier() <> "A") Then
                                        CheckStatus(FT_ERROR.FT_INVALID_BITMODE)
                                    End If
                                ElseIf (deviceType = FT_DEVICE.FT_DEVICE_232R) AndAlso (bitMode > 0) Then
                                    If ((bitMode And &H25) = 0) Then
                                        CheckStatus(FT_ERROR.FT_INVALID_BITMODE)
                                    End If
                                ElseIf ((deviceType = FT_DEVICE.FT_DEVICE_2232H) AndAlso (bitMode > 0)) Then
                                    If ((bitMode And &H5F) = 0) Then
                                        CheckStatus(FT_ERROR.FT_INVALID_BITMODE)
                                    End If
                                    If ((bitMode = 8) Or (bitMode = &H40)) AndAlso (GetInterfaceIdentifier() <> "A") Then
                                        CheckStatus(FT_ERROR.FT_INVALID_BITMODE)
                                    End If
                                ElseIf (deviceType = FT_DEVICE.FT_DEVICE_4232H) AndAlso (bitMode > 0) Then
                                    If ((bitMode And 7) = 0) Then
                                        CheckStatus(FT_ERROR.FT_INVALID_BITMODE)
                                    End If
                                    If (bitMode = 2) AndAlso (GetInterfaceIdentifier() <> "A") AndAlso (GetInterfaceIdentifier() <> "B") Then
                                        CheckStatus(FT_ERROR.FT_INVALID_BITMODE)
                                    End If
                                ElseIf (deviceType = FT_DEVICE.FT_DEVICE_232H) AndAlso (bitMode > 0) AndAlso (bitMode > &H40) Then
                                    CheckStatus(FT_ERROR.FT_INVALID_BITMODE)
                                End If
                        End Select
                        Dim delegateForFunctionPointer As tFT_SetBitMode = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetBitMode, GetType(tFT_SetBitMode)), tFT_SetBitMode)
                        Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, mask, bitMode)
                        CheckStatus(ftStatus)
                        _BitMode = bitMode
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_SetBitMode().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Выставляет или сбрасывает условие BREAK устройству.
        ''' </summary>
        ''' <param name="enable">Устанавливает (true) или сбрасывает (false).</param>
        Public Sub SetBreak(ByVal enable As Boolean)
            If LibraryInitialized Then
                If (Me.pFT_SetBreakOn <> IntPtr.Zero) AndAlso (Me.pFT_SetBreakOff <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim ft_status As FT_STATUS = FT_STATUS.FT_OTHER_ERROR
                        If enable Then
                            Dim delegateForFunctionPointer As tFT_SetBreakOn = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetBreakOn, GetType(tFT_SetBreakOn)), tFT_SetBreakOn)
                            ft_status = delegateForFunctionPointer.Invoke(Me.ftHandle)
                        Else
                            Dim breakOff As tFT_SetBreakOff = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetBreakOff, GetType(tFT_SetBreakOff)), tFT_SetBreakOff)
                            ft_status = breakOff.Invoke(Me.ftHandle)
                        End If
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    If (Me.pFT_SetBreakOn = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetBreakOn().")
                    End If
                    If (Me.pFT_SetBreakOff = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetBreakOff().")
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Назначает устройству спецсимволы в потоке данных, которые будут сигнализировать о событиях ошибках.
        ''' </summary>
        ''' <param name="eventChar">Символ при событии.</param>
        ''' <param name="EventCharEnable">Если маркер события используется - true, иначе - false.</param>
        ''' <param name="errorChar">Смвол при ошибке.</param>
        ''' <param name="errorCharEnable">Если маркер ошибки используется - true, иначе - false.</param>
        Public Sub SetCharacters(ByVal eventChar As Byte, ByVal eventCharEnable As Boolean, ByVal errorChar As Byte, ByVal errorCharEnable As Boolean)
            If LibraryInitialized Then
                If (Me.pFT_SetChars <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_SetChars = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetChars, GetType(tFT_SetChars)), tFT_SetChars)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, eventChar, Convert.ToByte(eventCharEnable), errorChar, Convert.ToByte(errorCharEnable))
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_SetChars().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Задаёт характеристики устройства.
        ''' </summary>
        ''' <param name="dataBits">Число битов в слове из <see cref="FT_DATA_BITS"/>.</param>
        ''' <param name="stopBits">Число стоповых бит из <see cref="FT_STOP_BITS"/>.</param>
        ''' <param name="parity">Чётность из <see cref="FT_PARITY"/>.</param>
        Public Sub SetDataCharacteristics(ByVal dataBits As FT_DATA_BITS, ByVal stopBits As FT_STOP_BITS, ByVal parity As FT_PARITY)
            If LibraryInitialized Then
                If (Me.pFT_SetDataCharacteristics <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_SetDataCharacteristics = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetDataCharacteristics, GetType(tFT_SetDataCharacteristics)), tFT_SetDataCharacteristics)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, dataBits, stopBits, parity)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_SetDataCharacteristics().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Задаёт устройству максимальное время ожидания USB запроса, в мс.
        ''' </summary>
        ''' <param name="deadmanTimeout">Таймаут, мс.</param>
        Public Sub SetDeadmanTimeout(Optional ByVal deadmanTimeout As UInteger = FT_DEFAULT_DEADMAN_TIMEOUT)
            If LibraryInitialized Then
                If (Me.pFT_SetDeadmanTimeout <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_SetDeadmanTimeout = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetDeadmanTimeout, GetType(tFT_SetDeadmanTimeout)), tFT_SetDeadmanTimeout)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, deadmanTimeout)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_SetDeadmanTimeout().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Задаёт управляющий сигнал Data Terminal Ready (DTR).
        ''' </summary>
        ''' <param name="enable">Задан - True.</param>
        Public Sub SetDTR(ByVal enable As Boolean)
            If LibraryInitialized Then
                If (Me.pFT_SetDtr <> IntPtr.Zero) AndAlso (Me.pFT_ClrDtr <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim ft_status As FT_STATUS = FT_STATUS.FT_OTHER_ERROR
                        If enable Then
                            Dim delegateForFunctionPointer As tFT_SetDtr = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetDtr, GetType(tFT_SetDtr)), tFT_SetDtr)
                            ft_status = delegateForFunctionPointer.Invoke(Me.ftHandle)
                        Else
                            Dim dtr2 As tFT_ClrDtr = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_ClrDtr, GetType(tFT_ClrDtr)), tFT_ClrDtr)
                            ft_status = dtr2.Invoke(Me.ftHandle)
                        End If
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    If (Me.pFT_SetDtr = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetDtr().")
                    End If
                    If (Me.pFT_ClrDtr = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_ClrDtr().")
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Задаёт условия для уведомлений о событии.
        ''' </summary>
        ''' <param name="eventMask">Условия, вызывающие уведомление.</param>
        ''' <param name="eventHandle">Дескриптор уведомления.</param>
        ''' <remarks>Используется для задания условий, которые разрешат приложению блокировать поток до тех пор, пока событие не произойдёт.
        ''' Обычно приложение создаёт событие, вызывает эту функцию, а затем блокирует событие.
        ''' Если условия произойдут, событие установится, и приложение разблокирует поток.
        ''' </remarks>
        Public Sub SetEventNotification(ByVal eventMask As FT_EVENTS, ByVal eventHandle As EventWaitHandle)
            If LibraryInitialized Then
                If (Me.pFT_SetEventNotification <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_SetEventNotification = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetEventNotification, GetType(tFT_SetEventNotification)), tFT_SetEventNotification)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, eventMask, eventHandle.SafeWaitHandle)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_SetEventNotification().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Устанавливает контроль чётности.
        ''' </summary>
        ''' <param name="flowControl">Одно из значений перечисления <see cref="FT_FLOW_CONTROL"/>.</param>
        ''' <param name="xOn">Значение сигнала X-on приёмника и передатчика. Используется только если <paramref name="flowControl"/> имеет значение <see cref="FT_FLOW_CONTROL.FT_FLOW_XON_XOFF"/>.</param>
        ''' <param name="xOff">Значение сигнала X-off приёмника и передатчика. Используется только если <paramref name="flowControl"/>  имеет значение <see cref="FT_FLOW_CONTROL.FT_FLOW_XON_XOFF"/>.</param>
        Public Sub SetFlowControl(ByVal flowControl As FT_FLOW_CONTROL, ByVal xOn As Byte, ByVal xOff As Byte)
            If LibraryInitialized Then
                If (Me.pFT_SetFlowControl <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_SetFlowControl = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetFlowControl, GetType(tFT_SetFlowControl)), tFT_SetFlowControl)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, flowControl, xOn, xOff)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_SetFlowControl().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Задаёт задержку (latency timer), мс.
        ''' </summary>
        ''' <param name="latency">Действительные значения в диапазоне 2..255 мс.</param>
        Public Sub SetLatency(ByVal latency As Byte)
            If LibraryInitialized Then
                If (Me.pFT_SetLatencyTimer <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim deviceType As FT_DEVICE = GetDeviceType()
                        If (latency < 2) Then
                            If (deviceType = FT_DEVICE.FT_DEVICE_BM) OrElse (deviceType = FT_DEVICE.FT_DEVICE_2232) Then
                                latency = 2 ' FT_DEFAULT_LATENCY '
                            End If
                        End If
                        Dim delegateForFunctionPointer As tFT_SetLatencyTimer = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetLatencyTimer, GetType(tFT_SetLatencyTimer)), tFT_SetLatencyTimer)
                        Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, latency)
                        CheckStatus(ftStatus)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_SetLatencyTimer().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Задаёт значение повторов чтения при ошибке USB.
        ''' </summary>
        ''' <param name="resetPipeRetryCount">Число раз, которое драйвер перезапрашивает данные при ошибке USB. По умолчанию равно 50. 
        ''' В зашумлённых средах может потребоваться увеличить это число, если возникает большое число ошибок USB.</param>
        Public Sub SetResetPipeRetryCount(Optional ByVal resetPipeRetryCount As UInteger = 50)
            If LibraryInitialized Then
                If (Me.pFT_SetResetPipeRetryCount <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_SetResetPipeRetryCount = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetResetPipeRetryCount, GetType(tFT_SetResetPipeRetryCount)), tFT_SetResetPipeRetryCount)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, resetPipeRetryCount)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_SetResetPipeRetryCount().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Выставляет или сбрасывает управляющий сигнал Request To Send (RTS).
        ''' </summary>
        ''' <param name="enable">Если True - выставляет, иначе - сбрасывает.</param>
        Public Sub SetRTS(ByVal enable As Boolean)
            If LibraryInitialized Then
                If (Me.pFT_SetRts <> IntPtr.Zero) AndAlso (Me.pFT_ClrRts <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim ft_status As FT_STATUS = FT_STATUS.FT_OTHER_ERROR
                        If enable Then
                            Dim delegateForFunctionPointer As tFT_SetRts = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetRts, GetType(tFT_SetRts)), tFT_SetRts)
                            ft_status = delegateForFunctionPointer.Invoke(Me.ftHandle)
                        Else
                            Dim rts2 As tFT_ClrRts = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_ClrRts, GetType(tFT_ClrRts)), tFT_ClrRts)
                            ft_status = rts2.Invoke(Me.ftHandle)
                        End If
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    If (Me.pFT_SetRts = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_SetRts().")
                    End If
                    If (Me.pFT_ClrRts = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_ClrRts().")
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Задаёт таймауты чтения и записи устройства.
        ''' </summary>
        ''' <param name="readTimeout">Время ожидания чтения, мс.</param>
        ''' <param name="writeTimeout">Время ожидания записи, мс.</param>
        Public Sub SetTimeouts(ByVal readTimeout As UInteger, ByVal writeTimeout As UInteger)
            If LibraryInitialized Then
                If (Me.pFT_SetTimeouts <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_SetTimeouts = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_SetTimeouts, GetType(tFT_SetTimeouts)), tFT_SetTimeouts)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, readTimeout, writeTimeout)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_SetTimeouts().")
                End If
            End If
        End Sub

#End Region '/ВЫСТАВЛЕНИЕ ПАРАМЕТРОВ

#Region "УПРАВЛЕНИЕ РЕЖИМОМ MPSSE"

#Region "MPSSE В РЕЖИМЕ SPI"

        ''' <summary>
        ''' Инициализирует устройство в режиме SPI.
        ''' </summary>
        ''' <param name="clockRate">Значение тактовой частоты шины SPI в диапазоне от 0 до 30 МГц, в Гц.</param>
        Public Sub SPI_Init(ByVal clockRate As Integer)
            ResetDevice()
            Dim inpBufSize As Long = GetRxBytesAvailable()
            If (inpBufSize > 0) Then
                Purge(FT_PURGE.FT_PURGE_RX)
            End If
            SetUsbTransferSize(USB_DEFAULT_IN_TRANSFER_SIZE, USB_DEFAULT_OUT_TRANSFER_SIZE)
            SetCharacters(0, False, 0, False)
            SetTimeouts(3000, 3000)
            SetLatency(1)
            SetBitMode(0, FT_BIT_MODE.FT_BIT_MODE_RESET)
            SetBitMode(&B0111_1110, FT_BIT_MODE.FT_BIT_MODE_MPSSE) 'TEST Mask?
            Thread.Sleep(50) 'подождём, пока USB закончит настройку
            SyncDevice()
            SetDivideBy5(False)
            SetMpsseClockDivisor(clockRate)
        End Sub

        ''' <summary>
        ''' Выполняет синхронизацию интерфейса MPSSE отправкой "плохих" команд.
        ''' </summary>
        Private Sub SyncDevice()
            SetLoopback(True)
            TestBadCommand(BAD_COMMAND.COMMAND_AA)
            TestBadCommand(BAD_COMMAND.COMMAND_AB)
            SetLoopback(False)
        End Sub

        ''' <summary>
        ''' Читает из устройства SPI заданное число байтов.
        ''' </summary>
        ''' <param name="numBytesToRead">Число байтов, которое нужно прочитать.</param>
        Public Function SPI_Read(ByVal numBytesToRead As Integer) As Byte()

            'SendBuffer(0) = &H20 ' MPSSE command to read bytes in from SPI
            Dim rxDataLen() As Byte = BitConverter.GetBytes(numBytesToRead - 1)
            Dim cmd As Byte() = {OPCODE.SPI_READ_BYTES, rxDataLen(0), rxDataLen(1)}
            Dim wrote As Integer = Write(cmd)
            Thread.Sleep(10)

            Dim timeOut As DateTime = Now.AddMilliseconds(500)
            Do While ((GetRxBytesAvailable() < numBytesToRead) AndAlso (DateTime.Compare(Now, timeOut) < 0))
                Thread.Sleep(1)
            Loop
            Dim rxBuf As Byte() = Read(numBytesToRead)
            Return rxBuf
        End Function

        ''' <summary>
        ''' Передаёт по SPI заданный массив.
        ''' </summary>
        ''' <param name="dataToWrite">Данные для записи.</param>
        Public Sub SPI_Write(ByVal dataToWrite As Byte())
            'Dim w As Integer = Write(dataToWrite)
            'Debug.WriteLine(w)
            Dim rxDataLen() As Byte = BitConverter.GetBytes(dataToWrite.Length - 1)
            Dim cmd As Byte() = {OPCODE.SPI_WRITE_BYTES, rxDataLen(0), rxDataLen(1)}
            Dim len As Integer = cmd.Length
            ReDim Preserve cmd(len + dataToWrite.Length - 1)
            Array.Copy(dataToWrite, 0, cmd, len, dataToWrite.Length) 'TEST
            'cmd = cmd.Concat(dataToWrite).ToArray()
            Dim wrote As Integer = Write(cmd)
            Thread.Sleep(2)
            Write(cmd)
        End Sub

#End Region '/MPSSE В РЕЖИМЕ SPI

#Region "УПРАВЛЕНИЕ СКОРОСТЬЮ ПЕРЕДАЧИ ДАННЫХ"

        ''' <summary>
        ''' Задаёт состояние делителя частоты на 5 (только в высокоскоростных устройствах).
        ''' </summary>
        ''' <param name="enable">Делитель включён (true), выключен (false).</param>
        Public Sub SetDivideBy5(ByVal enable As Boolean)
            Dim ty As FT_DEVICE = GetDeviceType()
            If (ty = FT_DEVICE.FT_DEVICE_2232H) OrElse (ty = FT_DEVICE.FT_DEVICE_2232H) Then
                Dim cmd As Byte()
                If enable Then
                    cmd = {OPCODE.DIVIDE_BY_5_ON}
                Else
                    cmd = {OPCODE.DIVIDE_BY_5_OFF}
                End If
                Dim wrote As Integer = Write(cmd)
                If (wrote <> cmd.Length) Then
                    Throw New SystemException("Состояние делителя на 5 не задано.")
                End If
                _DivideBy5 = enable
            End If
        End Sub

        ''' <summary>
        ''' Выставляет делитель тактовой частоты в зависимости от выбранной скорости передачи данных.
        ''' </summary>
        ''' <param name="dataSpeed">Скорость передачи данных, Гц. От 1 до 30 МГц (full-speed устройства) и от 1 до 6 МГц (high-speed устройства).</param>
        Public Sub SetMpsseClockDivisor(ByVal dataSpeed As Integer)
            If (Me.BitMode = FT_BIT_MODE.FT_BIT_MODE_MPSSE) Then
                Dim divisor As Byte() = GetDivisor(dataSpeed)
                Dim buf() As Byte = {OPCODE.SET_CLOCK_DIVISOR, divisor(0), divisor(1)}
                Dim wrote As Integer = Write(buf)
                If (wrote <> buf.Length) Then
                    Throw New SystemException("Частота не задана.")
                End If
            Else
                Throw New SystemException("Для текущего режима работы устройства нельзя использовать функцию SetMpsseClock().")
            End If
        End Sub

        ''' <summary>
        ''' Возвращает младший и старший байты делителя частоты.
        ''' </summary>
        ''' <param name="dataSpeed">Желаемое значение скорости передачи, Гц.</param>
        Private Function GetDivisor(ByVal dataSpeed As Integer) As Byte()
            Dim clk As Integer = GetClockHz()
            Dim divisor As Integer = CInt(clk * 0.5 / dataSpeed - 1)
            Dim div As Byte() = BitConverter.GetBytes(divisor)
            Return {div(0), div(1)}
        End Function

        ''' <summary>
        ''' Возвращает значение частоты генератора с учётом делителя, Гц.
        ''' </summary>
        Private Function GetClockHz() As Integer
            If (Not DivideBy5) Then
                Return FAST_CLOCK
            Else
                Return LOW_CLOCK
            End If
        End Function

#End Region '/УПРАВЛЕНИЕ СКОРОСТЬЮ ПЕРЕДАЧИ ДАННЫХ

#Region "ПЕТЛЯ ОБРАТНОЙ СВЯЗИ"

        ''' <summary>
        ''' Задаёт состояние внутренней петли обратной связи.
        ''' </summary>
        ''' <param name="enable">Активно, если True.</param>
        Public Sub SetLoopback(ByVal enable As Boolean)
            Dim ty As FT_DEVICE = GetDeviceType()
            If (ty = FT_DEVICE.FT_DEVICE_2232H) OrElse (ty = FT_DEVICE.FT_DEVICE_2232H) Then
                Dim buf() As Byte
                If enable Then
                    buf = {OPCODE.LOOPBACK_ON}
                Else
                    buf = {OPCODE.LOOPBACK_OFF}
                End If
                Dim wrote As Integer = Write(buf)
                If (wrote <> buf.Length) Then
                    Throw New SystemException("Состояние loop-back не задано.")
                End If
                _Loopback = enable
            End If
        End Sub

#End Region '/ПЕТЛЯ ОБРАТНОЙ СВЯЗИ

        ''' <summary>
        ''' Проверяет конфигурацию MPSSE на "плохие" команды.
        ''' </summary>
        Public Sub TestBadCommand(ByVal badCommand As BAD_COMMAND)
            Write({badCommand})
            WaitData(2, DELAY_AFTER_WRITE)
            Dim r As Byte() = Read(2)
            If (r(0) <> &HFA) OrElse (r(1) <> badCommand) OrElse (r.Length = 0) Then
                Throw New Exception(String.Format("Сбой синхронизации MPSSE командой 0x{0}.", badCommand.ToString("X")))
            End If
        End Sub

#End Region '/УПРАВЛЕНИЕ РЕЖИМОМ MPSSE

#Region "ЧТЕНИЕ"

        ''' <summary>
        ''' Читает заданное число байтов из устройства.
        ''' </summary>
        ''' <param name="numBytesToRead">Число байтов, которые нужно прочитать.</param>
        Public Function Read(ByVal numBytesToRead As Integer) As Byte()
            Dim dataBuffer As Byte() = New Byte(numBytesToRead - 1) {}
            If LibraryInitialized Then
                If (Me.pFT_Read <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim numBytesRead As UInteger = 0
                        Dim delegateForFunctionPointer As tFT_Read = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_Read, GetType(tFT_Read)), tFT_Read)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, dataBuffer, CUInt(numBytesToRead), numBytesRead)
                        CheckStatus(ft_status)
                        ReDim Preserve dataBuffer(CInt(numBytesRead - 1))
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_Read().")
                End If
            End If
            Return dataBuffer
        End Function

        ''' <summary>
        ''' Переводит драйвер в состояние ожидания при выполнениие чтения.
        ''' </summary>
        ''' <remarks>Используется в ситуациях, когда данные приходят непрерывно, и устройство может терять данные из-за переполнения.</remarks>
        Public Sub PauseReading()
            If LibraryInitialized Then
                If (Me.pFT_StopInTask <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_StopInTask = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_StopInTask, GetType(tFT_StopInTask)), tFT_StopInTask)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_StopInTask().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Продолжает выполнение чтения после остановки функцией <see cref="PauseReading()"/>.
        ''' </summary>
        ''' <remarks>Должна быть вызывана после <see cref="PauseReading()"/>.</remarks>
        Public Sub ContinueReading()
            If LibraryInitialized Then
                If (Me.pFT_RestartInTask <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_RestartInTask = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_RestartInTask, GetType(tFT_RestartInTask)), tFT_RestartInTask)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_RestartInTask().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Ожидает, пока в буфере чтения не появится нужное число байтов или не закончится время ожидания.
        ''' </summary>
        ''' <param name="bytesToReceive"></param>
        ''' <param name="timeout"></param>
        Private Sub WaitData(ByVal bytesToReceive As Integer, Optional ByVal timeout As Integer = 2)
            Dim endTime As DateTime = Now.AddMilliseconds(timeout)
            Do While (bytesToReceive < GetRxBytesAvailable()) AndAlso (DateTime.Compare(endTime, Now) < 0)
                Thread.Sleep(2)
            Loop
        End Sub

#End Region '/ЧТЕНИЕ

#Region "ЗАПИСЬ"

        ''' <summary>
        ''' Записывает заданный массив и возвращает количество реально записанных байтов.
        ''' </summary>
        ''' <param name="dataBuffer">Данные для записи. Первый байт - команда, далее - параметры команды. См. AN_108.</param>
        Private Function Write(ByVal dataBuffer As Byte()) As Integer
            Dim numBytesWritten As UInteger = 0
            If LibraryInitialized Then
                If (Me.pFT_Write <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_Write = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_Write, GetType(tFT_Write)), tFT_Write)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, dataBuffer, CUInt(dataBuffer.Length), numBytesWritten)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_Write().")
                End If
            End If
            'Thread.Sleep(DELAY_AFTER_WRITE)
            Return CInt(numBytesWritten)
        End Function

        ''' <summary>
        ''' Записывает заданный массив и возвращает количество реально записанных символов.
        ''' </summary>
        ''' <param name="dataBuffer">Данные для записи. Первый байт - команда, далее - параметры команды. См. AN_108.</param>
        Private Function Write(ByVal dataBuffer As String) As Integer
            Dim numBytesWritten As UInteger = 0
            If LibraryInitialized Then
                If (Me.pFT_Write <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim bytes As Byte() = Encoding.ASCII.GetBytes(dataBuffer)
                        Dim delegateForFunctionPointer As tFT_Write = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_Write, GetType(tFT_Write)), tFT_Write)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, bytes, CUInt(bytes.Length), numBytesWritten)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_Write().")
                End If
            End If
            Return CInt(numBytesWritten)
        End Function

#End Region '/ЗАПИСЬ

#Region "РАБОТА С ПЗУ"

#Region "ПОЛЬЗОВАТЕЛЬСКАЯ ОБЛАСТЬ ДАННЫХ"

        ''' <summary>
        ''' Возвращает размер пользовательской области ПЗУ, в байтах.
        ''' </summary>
        Public Function EeUserAreaSize() As Integer
            Dim uaSize As UInteger = 0
            If LibraryInitialized Then
                If (Me.pFT_EE_UASize <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_EE_UASize = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_UASize, GetType(tFT_EE_UASize)), tFT_EE_UASize)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, uaSize)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_EE_UASize().")
                End If
            End If
            Return CInt(uaSize)
        End Function

        ''' <summary>
        ''' Читает пользовательскую область ПЗУ и возвращает массив той длины, которая доступна для чтения.
        ''' </summary>
        Public Function EeReadUserArea() As Byte()
            Dim numBytesRead As UInteger = 32
            Dim userAreaDataBuffer(CInt(numBytesRead - 1)) As Byte
            If LibraryInitialized Then
                If (Me.pFT_EE_UASize <> IntPtr.Zero) AndAlso (Me.pFT_EE_UARead <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim dwSize As UInteger = 0
                        Dim delegateForFunctionPointer As tFT_EE_UASize = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_UASize, GetType(tFT_EE_UASize)), tFT_EE_UASize)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, dwSize)
                        CheckStatus(ft_status)
                        If (userAreaDataBuffer.Length >= dwSize) Then
                            Dim read As tFT_EE_UARead = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_UARead, GetType(tFT_EE_UARead)), tFT_EE_UARead)
                            ft_status = read.Invoke(Me.ftHandle, userAreaDataBuffer, userAreaDataBuffer.Length, numBytesRead)
                            CheckStatus(ft_status)
                        End If
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                    ReDim userAreaDataBuffer(CInt(numBytesRead - 1))
                Else
                    If (Me.pFT_EE_UASize = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_EE_UASize().")
                    End If
                    If (Me.pFT_EE_UARead = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_EE_UARead().")
                    End If
                End If
            End If
            Return userAreaDataBuffer
        End Function

        ''' <summary>
        ''' Записывает заданный массив в пользовательскую область данных ПЗУ.
        ''' </summary>
        ''' <param name="userAreaDataBuffer">Данные для записи.</param>
        Public Sub EeWriteUserArea(ByVal userAreaDataBuffer As Byte())
            If LibraryInitialized Then
                If (Me.pFT_EE_UASize <> IntPtr.Zero) AndAlso (Me.pFT_EE_UAWrite <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim dwSize As UInt32 = 0
                        Dim delegateForFunctionPointer As tFT_EE_UASize = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_UASize, GetType(tFT_EE_UASize)), tFT_EE_UASize)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, dwSize)
                        CheckStatus(ft_status)
                        If (userAreaDataBuffer.Length <= dwSize) Then
                            Dim write As tFT_EE_UAWrite = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_UAWrite, GetType(tFT_EE_UAWrite)), tFT_EE_UAWrite)
                            ft_status = write.Invoke(Me.ftHandle, userAreaDataBuffer, userAreaDataBuffer.Length)
                            CheckStatus(ft_status)
                        End If
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    If (Me.pFT_EE_UASize = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_EE_UASize().")
                    End If
                    If (Me.pFT_EE_UAWrite = IntPtr.Zero) Then
                        Throw New SystemException("Сбой при загрузке функции FT_EE_UAWrite().")
                    End If
                End If
            End If
        End Sub

#End Region '/ПОЛЬЗОВАТЕЛЬСКАЯ ОБЛАСТЬ ДАННЫХ

#Region "ЧТЕНИЕ ПЗУ"

        ''' <summary>
        ''' Считывает значение из ПЗУ по заданному адресу.
        ''' </summary>
        ''' <param name="address">Адрес в ПЗУ.</param>
        ''' <remarks>ПЗУ устройств FTDI организованы через WORD, поэтому все возвращаемые значения 16-разрядные.</remarks>
        Public Function ReadEepromLocation(ByVal address As UInteger) As Integer
            Dim eeValue As UShort = 0
            If LibraryInitialized Then
                If (Me.pFT_ReadEE <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_ReadEE = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_ReadEE, GetType(tFT_ReadEE)), tFT_ReadEE)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, address, eeValue)
                        CheckStatus(ft_status)
                    Else
                        CheckStatus(FT_STATUS.FT_DEVICE_NOT_OPENED)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_ReadEE().")
                End If
            End If
            Return eeValue
        End Function

        ''' <summary>
        ''' Читает ПЗУ открытого устройства и возвращает содержимое ПЗУ в зависимости от его типа.
        ''' </summary>
        Public Function ReadEeprom() As FT_EEPROM_DATA
            Dim ty As FT_DEVICE = GetDeviceType()
            Select Case ty
                Case FT_DEVICE.FT_DEVICE_232H
                    Return ReadEepromFT232H()
                Case FT_DEVICE.FT_DEVICE_232R
                    Return ReadEepromFT232R()
                Case FT_DEVICE.FT_DEVICE_BM
                    Return ReadEepromFT232B()
                Case FT_DEVICE.FT_DEVICE_2232
                    Return ReadEepromFT2232()
                Case FT_DEVICE.FT_DEVICE_2232H
                    Return ReadEepromFT2232H()
                Case FT_DEVICE.FT_DEVICE_4232H
                    Return ReadEepromFT4232H()
                Case FT_DEVICE.FT_DEVICE_X_SERIES
                    Return ReadEepromXSeries()
            End Select
            Throw New NotImplementedException("Нет метода для чтения ПЗУ данного устройства.")
        End Function

#Region "ЧТЕНИЕ ПЗУ РАЗНЫХ ТИПОВ УСТРОЙСТВ"

        Private Function ReadEepromFT2232() As FT2232_EEPROM_STRUCTURE
            Dim ee2232 As New FT2232_EEPROM_STRUCTURE
            If LibraryInitialized Then
                If (Me.pFT_EE_Read <> IntPtr.Zero) Then
                    Dim deviceType As FT_DEVICE = GetDeviceType()
                    If (deviceType <> FT_DEVICE.FT_DEVICE_2232) Then
                        CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                    End If
                    Dim pData As New FT_PROGRAM_DATA With {
                        .Signature1 = 0,
                        .Signature2 = UInt32.MaxValue,
                        .Version = 2,
                        .Manufacturer = Marshal.AllocHGlobal(&H20),
                        .ManufacturerID = Marshal.AllocHGlobal(&H10),
                        .Description = Marshal.AllocHGlobal(&H40),
                        .SerialNumber = Marshal.AllocHGlobal(&H10)
                    }
                    Try
                        Dim delegateForFunctionPointer As tFT_EE_Read = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Read, GetType(tFT_EE_Read)), tFT_EE_Read)
                        Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                        CheckStatus(ftStatus)
                        ee2232.Manufacturer = Marshal.PtrToStringAnsi(pData.Manufacturer)
                        ee2232.ManufacturerID = Marshal.PtrToStringAnsi(pData.ManufacturerID)
                        ee2232.Description = Marshal.PtrToStringAnsi(pData.Description)
                        ee2232.SerialNumber = Marshal.PtrToStringAnsi(pData.SerialNumber)
                        ee2232.VendorID = pData.VendorID
                        ee2232.ProductID = pData.ProductID
                        ee2232.MaxPower = pData.MaxPower
                        ee2232.SelfPowered = Convert.ToBoolean(pData.SelfPowered)
                        ee2232.RemoteWakeup = Convert.ToBoolean(pData.RemoteWakeup)
                        ee2232.PullDownEnable = Convert.ToBoolean(pData.PullDownEnable5)
                        ee2232.SerNumEnable = Convert.ToBoolean(pData.SerNumEnable5)
                        ee2232.USBVersionEnable = Convert.ToBoolean(pData.USBVersionEnable5)
                        ee2232.USBVersion = pData.USBVersion5
                        ee2232.AIsHighCurrent = Convert.ToBoolean(pData.AIsHighCurrent)
                        ee2232.BIsHighCurrent = Convert.ToBoolean(pData.BIsHighCurrent)
                        ee2232.IFAIsFifo = Convert.ToBoolean(pData.IFAIsFifo)
                        ee2232.IFAIsFifoTar = Convert.ToBoolean(pData.IFAIsFifoTar)
                        ee2232.IFAIsFastSer = Convert.ToBoolean(pData.IFAIsFastSer)
                        ee2232.AIsVCP = Convert.ToBoolean(pData.AIsVCP)
                        ee2232.IFBIsFifo = Convert.ToBoolean(pData.IFBIsFifo)
                        ee2232.IFBIsFifoTar = Convert.ToBoolean(pData.IFBIsFifoTar)
                        ee2232.IFBIsFastSer = Convert.ToBoolean(pData.IFBIsFastSer)
                        ee2232.BIsVCP = Convert.ToBoolean(pData.BIsVCP)
                    Catch ex As Exception
                        Throw
                    Finally
                        Marshal.FreeHGlobal(pData.Manufacturer)
                        Marshal.FreeHGlobal(pData.ManufacturerID)
                        Marshal.FreeHGlobal(pData.Description)
                        Marshal.FreeHGlobal(pData.SerialNumber)
                    End Try
                End If
            Else
                Throw New SystemException("Сбой при загрузке функции FT_EE_Read().")
            End If
            Return ee2232
        End Function

        Private Function ReadEepromFT2232H() As FT2232H_EEPROM_STRUCTURE
            Dim ee2232h As New FT2232H_EEPROM_STRUCTURE
            If LibraryInitialized Then
                If (Me.pFT_EE_Read <> IntPtr.Zero) Then
                    Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                    If (deviceType <> FT_DEVICE.FT_DEVICE_2232H) Then
                        CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                    End If
                    Dim pData As New FT_PROGRAM_DATA With {
                        .Signature1 = 0,
                        .Signature2 = UInt32.MaxValue,
                        .Version = 3,
                        .Manufacturer = Marshal.AllocHGlobal(&H20),
                        .ManufacturerID = Marshal.AllocHGlobal(&H10),
                        .Description = Marshal.AllocHGlobal(&H40),
                        .SerialNumber = Marshal.AllocHGlobal(&H10)
                    }
                    Try
                        Dim delegateForFunctionPointer As tFT_EE_Read = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Read, GetType(tFT_EE_Read)), tFT_EE_Read)
                        Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                        CheckStatus(ftStatus)
                        ee2232h.Manufacturer = Marshal.PtrToStringAnsi(pData.Manufacturer)
                        ee2232h.ManufacturerID = Marshal.PtrToStringAnsi(pData.ManufacturerID)
                        ee2232h.Description = Marshal.PtrToStringAnsi(pData.Description)
                        ee2232h.SerialNumber = Marshal.PtrToStringAnsi(pData.SerialNumber)
                        ee2232h.VendorID = pData.VendorID
                        ee2232h.ProductID = pData.ProductID
                        ee2232h.MaxPower = pData.MaxPower
                        ee2232h.SelfPowered = Convert.ToBoolean(pData.SelfPowered)
                        ee2232h.RemoteWakeup = Convert.ToBoolean(pData.RemoteWakeup)
                        ee2232h.PullDownEnable = Convert.ToBoolean(pData.PullDownEnable7)
                        ee2232h.SerNumEnable = Convert.ToBoolean(pData.SerNumEnable7)
                        ee2232h.ALSlowSlew = Convert.ToBoolean(pData.ALSlowSlew)
                        ee2232h.ALSchmittInput = Convert.ToBoolean(pData.ALSchmittInput)
                        ee2232h.ALDriveCurrent = pData.ALDriveCurrent
                        ee2232h.AHSlowSlew = Convert.ToBoolean(pData.AHSlowSlew)
                        ee2232h.AHSchmittInput = Convert.ToBoolean(pData.AHSchmittInput)
                        ee2232h.AHDriveCurrent = pData.AHDriveCurrent
                        ee2232h.BLSlowSlew = Convert.ToBoolean(pData.BLSlowSlew)
                        ee2232h.BLSchmittInput = Convert.ToBoolean(pData.BLSchmittInput)
                        ee2232h.BLDriveCurrent = pData.BLDriveCurrent
                        ee2232h.BHSlowSlew = Convert.ToBoolean(pData.BHSlowSlew)
                        ee2232h.BHSchmittInput = Convert.ToBoolean(pData.BHSchmittInput)
                        ee2232h.BHDriveCurrent = pData.BHDriveCurrent
                        ee2232h.IFAIsFifo = Convert.ToBoolean(pData.IFAIsFifo7)
                        ee2232h.IFAIsFifoTar = Convert.ToBoolean(pData.IFAIsFifoTar7)
                        ee2232h.IFAIsFastSer = Convert.ToBoolean(pData.IFAIsFastSer7)
                        ee2232h.AIsVCP = Convert.ToBoolean(pData.AIsVCP7)
                        ee2232h.IFBIsFifo = Convert.ToBoolean(pData.IFBIsFifo7)
                        ee2232h.IFBIsFifoTar = Convert.ToBoolean(pData.IFBIsFifoTar7)
                        ee2232h.IFBIsFastSer = Convert.ToBoolean(pData.IFBIsFastSer7)
                        ee2232h.BIsVCP = Convert.ToBoolean(pData.BIsVCP7)
                        ee2232h.PowerSaveEnable = Convert.ToBoolean(pData.PowerSaveEnable)
                    Catch ex As Exception
                        Throw
                    Finally
                        Marshal.FreeHGlobal(pData.Manufacturer)
                        Marshal.FreeHGlobal(pData.ManufacturerID)
                        Marshal.FreeHGlobal(pData.Description)
                        Marshal.FreeHGlobal(pData.SerialNumber)
                    End Try
                End If
            Else
                Throw New SystemException("Сбой при загрузке функции FT_EE_Read().")
            End If
            Return ee2232h
        End Function

        Private Function ReadEepromFT232B() As FT232B_EEPROM_STRUCTURE
            Dim ee232b As New FT232B_EEPROM_STRUCTURE
            If LibraryInitialized Then
                If (Me.pFT_EE_Read <> IntPtr.Zero) Then
                    Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                    If (deviceType > FT_DEVICE.FT_DEVICE_BM) Then
                        CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                    End If
                    Dim pData As New FT_PROGRAM_DATA With {
                        .Signature1 = 0,
                        .Signature2 = UInt32.MaxValue,
                        .Version = 2,
                        .Manufacturer = Marshal.AllocHGlobal(&H20),
                        .ManufacturerID = Marshal.AllocHGlobal(&H10),
                        .Description = Marshal.AllocHGlobal(&H40),
                        .SerialNumber = Marshal.AllocHGlobal(&H10)
                    }
                    Try
                        Dim delegateForFunctionPointer As tFT_EE_Read = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Read, GetType(tFT_EE_Read)), tFT_EE_Read)
                        Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                        CheckStatus(ftStatus)
                        ee232b.Manufacturer = Marshal.PtrToStringAnsi(pData.Manufacturer)
                        ee232b.ManufacturerID = Marshal.PtrToStringAnsi(pData.ManufacturerID)
                        ee232b.Description = Marshal.PtrToStringAnsi(pData.Description)
                        ee232b.SerialNumber = Marshal.PtrToStringAnsi(pData.SerialNumber)
                        ee232b.VendorID = pData.VendorID
                        ee232b.ProductID = pData.ProductID
                        ee232b.MaxPower = pData.MaxPower
                        ee232b.SelfPowered = Convert.ToBoolean(pData.SelfPowered)
                        ee232b.RemoteWakeup = Convert.ToBoolean(pData.RemoteWakeup)
                        ee232b.PullDownEnable = Convert.ToBoolean(pData.PullDownEnable)
                        ee232b.SerNumEnable = Convert.ToBoolean(pData.SerNumEnable)
                        ee232b.USBVersionEnable = Convert.ToBoolean(pData.USBVersionEnable)
                        ee232b.USBVersion = pData.USBVersion
                    Catch ex As Exception
                        Throw
                    Finally
                        Marshal.FreeHGlobal(pData.Manufacturer)
                        Marshal.FreeHGlobal(pData.ManufacturerID)
                        Marshal.FreeHGlobal(pData.Description)
                        Marshal.FreeHGlobal(pData.SerialNumber)
                    End Try
                End If
            Else
                Throw New SystemException("Сбой при загрузке функции FT_EE_Read().")
            End If
            Return ee232b
        End Function

        Private Function ReadEepromFT232H() As FT232H_EEPROM_STRUCTURE
            Dim ee232h As New FT232H_EEPROM_STRUCTURE
            If LibraryInitialized Then
                If (Me.pFT_EE_Read <> IntPtr.Zero) Then
                    Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                    If (deviceType <> FT_DEVICE.FT_DEVICE_232H) Then
                        CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                    End If
                    Dim pData As New FT_PROGRAM_DATA With {
                        .Signature1 = 0,
                        .Signature2 = UInt32.MaxValue,
                        .Version = 5,
                        .Manufacturer = Marshal.AllocHGlobal(&H20),
                        .ManufacturerID = Marshal.AllocHGlobal(&H10),
                        .Description = Marshal.AllocHGlobal(&H40),
                        .SerialNumber = Marshal.AllocHGlobal(&H10)
                    }
                    Try
                        Dim delegateForFunctionPointer As tFT_EE_Read = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Read, GetType(tFT_EE_Read)), tFT_EE_Read)
                        Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                        CheckStatus(ftStatus)
                        ee232h.Manufacturer = Marshal.PtrToStringAnsi(pData.Manufacturer)
                        ee232h.ManufacturerID = Marshal.PtrToStringAnsi(pData.ManufacturerID)
                        ee232h.Description = Marshal.PtrToStringAnsi(pData.Description)
                        ee232h.SerialNumber = Marshal.PtrToStringAnsi(pData.SerialNumber)
                        ee232h.VendorID = pData.VendorID
                        ee232h.ProductID = pData.ProductID
                        ee232h.MaxPower = pData.MaxPower
                        ee232h.SelfPowered = Convert.ToBoolean(pData.SelfPowered)
                        ee232h.RemoteWakeup = Convert.ToBoolean(pData.RemoteWakeup)
                        ee232h.PullDownEnable = Convert.ToBoolean(pData.PullDownEnableH)
                        ee232h.SerNumEnable = Convert.ToBoolean(pData.SerNumEnableH)
                        ee232h.ACSlowSlew = Convert.ToBoolean(pData.ACSlowSlewH)
                        ee232h.ACSchmittInput = Convert.ToBoolean(pData.ACSchmittInputH)
                        ee232h.ACDriveCurrent = pData.ACDriveCurrentH
                        ee232h.ADSlowSlew = Convert.ToBoolean(pData.ADSlowSlewH)
                        ee232h.ADSchmittInput = Convert.ToBoolean(pData.ADSchmittInputH)
                        ee232h.ADDriveCurrent = pData.ADDriveCurrentH
                        ee232h.Cbus0 = pData.Cbus0H
                        ee232h.Cbus1 = pData.Cbus1H
                        ee232h.Cbus2 = pData.Cbus2H
                        ee232h.Cbus3 = pData.Cbus3H
                        ee232h.Cbus4 = pData.Cbus4H
                        ee232h.Cbus5 = pData.Cbus5H
                        ee232h.Cbus6 = pData.Cbus6H
                        ee232h.Cbus7 = pData.Cbus7H
                        ee232h.Cbus8 = pData.Cbus8H
                        ee232h.Cbus9 = pData.Cbus9H
                        ee232h.IsFifo = Convert.ToBoolean(pData.IsFifoH)
                        ee232h.IsFifoTar = Convert.ToBoolean(pData.IsFifoTarH)
                        ee232h.IsFastSer = Convert.ToBoolean(pData.IsFastSerH)
                        ee232h.IsFT1248 = Convert.ToBoolean(pData.IsFT1248H)
                        ee232h.FT1248Cpol = Convert.ToBoolean(pData.FT1248CpolH)
                        ee232h.FT1248Lsb = Convert.ToBoolean(pData.FT1248LsbH)
                        ee232h.FT1248FlowControl = Convert.ToBoolean(pData.FT1248FlowControlH)
                        ee232h.IsVCP = Convert.ToBoolean(pData.IsVCPH)
                        ee232h.PowerSaveEnable = Convert.ToBoolean(pData.PowerSaveEnableH)
                    Catch ex As Exception
                        Throw
                    Finally
                        Marshal.FreeHGlobal(pData.Manufacturer)
                        Marshal.FreeHGlobal(pData.ManufacturerID)
                        Marshal.FreeHGlobal(pData.Description)
                        Marshal.FreeHGlobal(pData.SerialNumber)
                    End Try
                End If
            Else
                Throw New SystemException("Сбой при загрузке функции FT_EE_Read().")
            End If
            Return ee232h
        End Function

        Private Function ReadEepromFT232R() As FT232R_EEPROM_STRUCTURE
            Dim ee232r As New FT232R_EEPROM_STRUCTURE
            If LibraryInitialized Then
                If (Me.pFT_EE_Read <> IntPtr.Zero) Then
                    Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                    If (deviceType <> FT_DEVICE.FT_DEVICE_232R) Then
                        CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                    End If
                    Dim pData As New FT_PROGRAM_DATA With {
                        .Signature1 = 0,
                        .Signature2 = UInt32.MaxValue,
                        .Version = 2,
                        .Manufacturer = Marshal.AllocHGlobal(&H20),
                        .ManufacturerID = Marshal.AllocHGlobal(&H10),
                        .Description = Marshal.AllocHGlobal(&H40),
                        .SerialNumber = Marshal.AllocHGlobal(&H10)
                    }
                    Try
                        Dim delegateForFunctionPointer As tFT_EE_Read = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Read, GetType(tFT_EE_Read)), tFT_EE_Read)
                        Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                        CheckStatus(ftStatus)
                        ee232r.Manufacturer = Marshal.PtrToStringAnsi(pData.Manufacturer)
                        ee232r.ManufacturerID = Marshal.PtrToStringAnsi(pData.ManufacturerID)
                        ee232r.Description = Marshal.PtrToStringAnsi(pData.Description)
                        ee232r.SerialNumber = Marshal.PtrToStringAnsi(pData.SerialNumber)
                        ee232r.VendorID = pData.VendorID
                        ee232r.ProductID = pData.ProductID
                        ee232r.MaxPower = pData.MaxPower
                        ee232r.SelfPowered = Convert.ToBoolean(pData.SelfPowered)
                        ee232r.RemoteWakeup = Convert.ToBoolean(pData.RemoteWakeup)
                        ee232r.UseExtOsc = Convert.ToBoolean(pData.UseExtOsc)
                        ee232r.HighDriveIOs = Convert.ToBoolean(pData.HighDriveIOs)
                        ee232r.EndpointSize = pData.EndpointSize
                        ee232r.PullDownEnable = Convert.ToBoolean(pData.PullDownEnableR)
                        ee232r.SerNumEnable = Convert.ToBoolean(pData.SerNumEnableR)
                        ee232r.InvertTXD = Convert.ToBoolean(pData.InvertTXD)
                        ee232r.InvertRXD = Convert.ToBoolean(pData.InvertRXD)
                        ee232r.InvertRTS = Convert.ToBoolean(pData.InvertRTS)
                        ee232r.InvertCTS = Convert.ToBoolean(pData.InvertCTS)
                        ee232r.InvertDTR = Convert.ToBoolean(pData.InvertDTR)
                        ee232r.InvertDSR = Convert.ToBoolean(pData.InvertDSR)
                        ee232r.InvertDCD = Convert.ToBoolean(pData.InvertDCD)
                        ee232r.InvertRI = Convert.ToBoolean(pData.InvertRI)
                        ee232r.Cbus0 = pData.Cbus0
                        ee232r.Cbus1 = pData.Cbus1
                        ee232r.Cbus2 = pData.Cbus2
                        ee232r.Cbus3 = pData.Cbus3
                        ee232r.Cbus4 = pData.Cbus4
                        ee232r.RIsD2XX = Convert.ToBoolean(pData.RIsD2XX)
                    Catch ex As Exception
                        Throw
                    Finally
                        Marshal.FreeHGlobal(pData.Manufacturer)
                        Marshal.FreeHGlobal(pData.ManufacturerID)
                        Marshal.FreeHGlobal(pData.Description)
                        Marshal.FreeHGlobal(pData.SerialNumber)
                    End Try
                End If
            Else
                Throw New SystemException("Сбой при загрузке функции FT_EE_Read().")
            End If
            Return ee232r
        End Function

        Private Function ReadEepromFT4232H() As FT4232H_EEPROM_STRUCTURE
            Dim ee4232h As New FT4232H_EEPROM_STRUCTURE
            If LibraryInitialized Then
                If (Me.pFT_EE_Read <> IntPtr.Zero) Then
                    Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                    If (deviceType <> FT_DEVICE.FT_DEVICE_4232H) Then
                        CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                    End If
                    Dim pData As New FT_PROGRAM_DATA With {
                        .Signature1 = 0,
                        .Signature2 = UInt32.MaxValue,
                        .Version = 4,
                        .Manufacturer = Marshal.AllocHGlobal(&H20),
                        .ManufacturerID = Marshal.AllocHGlobal(&H10),
                        .Description = Marshal.AllocHGlobal(&H40),
                        .SerialNumber = Marshal.AllocHGlobal(&H10)
                    }
                    Try
                        Dim delegateForFunctionPointer As tFT_EE_Read = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Read, GetType(tFT_EE_Read)), tFT_EE_Read)
                        Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                        CheckStatus(ftStatus)
                        ee4232h.Manufacturer = Marshal.PtrToStringAnsi(pData.Manufacturer)
                        ee4232h.ManufacturerID = Marshal.PtrToStringAnsi(pData.ManufacturerID)
                        ee4232h.Description = Marshal.PtrToStringAnsi(pData.Description)
                        ee4232h.SerialNumber = Marshal.PtrToStringAnsi(pData.SerialNumber)
                        ee4232h.VendorID = pData.VendorID
                        ee4232h.ProductID = pData.ProductID
                        ee4232h.MaxPower = pData.MaxPower
                        ee4232h.SelfPowered = Convert.ToBoolean(pData.SelfPowered)
                        ee4232h.RemoteWakeup = Convert.ToBoolean(pData.RemoteWakeup)
                        ee4232h.PullDownEnable = Convert.ToBoolean(pData.PullDownEnable8)
                        ee4232h.SerNumEnable = Convert.ToBoolean(pData.SerNumEnable8)
                        ee4232h.ASlowSlew = Convert.ToBoolean(pData.ASlowSlew)
                        ee4232h.ASchmittInput = Convert.ToBoolean(pData.ASchmittInput)
                        ee4232h.ADriveCurrent = pData.ADriveCurrent
                        ee4232h.BSlowSlew = Convert.ToBoolean(pData.BSlowSlew)
                        ee4232h.BSchmittInput = Convert.ToBoolean(pData.BSchmittInput)
                        ee4232h.BDriveCurrent = pData.BDriveCurrent
                        ee4232h.CSlowSlew = Convert.ToBoolean(pData.CSlowSlew)
                        ee4232h.CSchmittInput = Convert.ToBoolean(pData.CSchmittInput)
                        ee4232h.CDriveCurrent = pData.CDriveCurrent
                        ee4232h.DSlowSlew = Convert.ToBoolean(pData.DSlowSlew)
                        ee4232h.DSchmittInput = Convert.ToBoolean(pData.DSchmittInput)
                        ee4232h.DDriveCurrent = pData.DDriveCurrent
                        ee4232h.ARIIsTXDEN = Convert.ToBoolean(pData.ARIIsTXDEN)
                        ee4232h.BRIIsTXDEN = Convert.ToBoolean(pData.BRIIsTXDEN)
                        ee4232h.CRIIsTXDEN = Convert.ToBoolean(pData.CRIIsTXDEN)
                        ee4232h.DRIIsTXDEN = Convert.ToBoolean(pData.DRIIsTXDEN)
                        ee4232h.AIsVCP = Convert.ToBoolean(pData.AIsVCP8)
                        ee4232h.BIsVCP = Convert.ToBoolean(pData.BIsVCP8)
                        ee4232h.CIsVCP = Convert.ToBoolean(pData.CIsVCP8)
                        ee4232h.DIsVCP = Convert.ToBoolean(pData.DIsVCP8)
                    Catch ex As Exception
                        Throw
                    Finally
                        Marshal.FreeHGlobal(pData.Manufacturer)
                        Marshal.FreeHGlobal(pData.ManufacturerID)
                        Marshal.FreeHGlobal(pData.Description)
                        Marshal.FreeHGlobal(pData.SerialNumber)
                    End Try
                End If
            Else
                Throw New SystemException("Сбой при загрузке функции FT_EE_Read().")
            End If
            Return ee4232h
        End Function

        Private Function ReadEepromXSeries() As FT_XSERIES_EEPROM_STRUCTURE
            Dim eeX As New FT_XSERIES_EEPROM_STRUCTURE
            If LibraryInitialized Then
                If (Me.pFT_EEPROM_Read <> IntPtr.Zero) Then
                    Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                    If (deviceType <> FT_DEVICE.FT_DEVICE_X_SERIES) Then
                        CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                    End If
                    Dim ft_eeprom_header As New FT_EEPROM_HEADER With {
                        .deviceType = 9
                    }
                    Dim xData As New FT_XSERIES_DATA With {
                        .common = ft_eeprom_header
                    }
                    Dim cb As Integer = Marshal.SizeOf(xData)
                    Dim ptr As IntPtr = Marshal.AllocHGlobal(cb)
                    Marshal.StructureToPtr(xData, ptr, False)
                    Dim manufacturerID As Byte() = New Byte(&H10 - 1) {}
                    Dim description As Byte() = New Byte(&H40 - 1) {}
                    Dim serialnumber As Byte() = New Byte(&H10 - 1) {}
                    Dim manufacturer As Byte() = New Byte(&H20 - 1) {}
                    Dim delegateForFunctionPointer As tFT_EEPROM_Read = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EEPROM_Read, GetType(tFT_EEPROM_Read)), tFT_EEPROM_Read)
                    Try
                        Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, ptr, CUInt(cb), manufacturer, manufacturerID, description, serialnumber)
                        CheckStatus(ftStatus)
                        xData = DirectCast(Marshal.PtrToStructure(ptr, GetType(FT_XSERIES_DATA)), FT_XSERIES_DATA)
                    Catch ex As Exception
                        Throw
                    Finally
                        Marshal.FreeHGlobal(ptr)
                    End Try
                    Dim encoding As New UTF8Encoding
                    eeX.Manufacturer = encoding.GetString(manufacturer)
                    eeX.ManufacturerID = encoding.GetString(manufacturerID)
                    eeX.Description = encoding.GetString(description)
                    eeX.SerialNumber = encoding.GetString(serialnumber)
                    eeX.VendorID = xData.common.VendorId
                    eeX.ProductID = xData.common.ProductId
                    eeX.MaxPower = xData.common.MaxPower
                    eeX.SelfPowered = Convert.ToBoolean(xData.common.SelfPowered)
                    eeX.RemoteWakeup = Convert.ToBoolean(xData.common.RemoteWakeup)
                    eeX.SerNumEnable = Convert.ToBoolean(xData.common.SerNumEnable)
                    eeX.PullDownEnable = Convert.ToBoolean(xData.common.PullDownEnable)
                    eeX.Cbus0 = xData.Cbus0
                    eeX.Cbus1 = xData.Cbus1
                    eeX.Cbus2 = xData.Cbus2
                    eeX.Cbus3 = xData.Cbus3
                    eeX.Cbus4 = xData.Cbus4
                    eeX.Cbus5 = xData.Cbus5
                    eeX.Cbus6 = xData.Cbus6
                    eeX.ACDriveCurrent = xData.ACDriveCurrent
                    eeX.ACSchmittInput = xData.ACSchmittInput
                    eeX.ACSlowSlew = xData.ACSlowSlew
                    eeX.ADDriveCurrent = xData.ADDriveCurrent
                    eeX.ADSchmittInput = xData.ADSchmittInput
                    eeX.ADSlowSlew = xData.ADSlowSlew
                    eeX.BCDDisableSleep = xData.BCDDisableSleep
                    eeX.BCDEnable = xData.BCDEnable
                    eeX.BCDForceCbusPWREN = xData.BCDForceCbusPWREN
                    eeX.FT1248Cpol = xData.FT1248Cpol
                    eeX.FT1248FlowControl = xData.FT1248FlowControl
                    eeX.FT1248Lsb = xData.FT1248Lsb
                    eeX.I2CDeviceId = xData.I2CDeviceId
                    eeX.I2CDisableSchmitt = xData.I2CDisableSchmitt
                    eeX.I2CSlaveAddress = xData.I2CSlaveAddress
                    eeX.InvertCTS = xData.InvertCTS
                    eeX.InvertDCD = xData.InvertDCD
                    eeX.InvertDSR = xData.InvertDSR
                    eeX.InvertDTR = xData.InvertDTR
                    eeX.InvertRI = xData.InvertRI
                    eeX.InvertRTS = xData.InvertRTS
                    eeX.InvertRXD = xData.InvertRXD
                    eeX.InvertTXD = xData.InvertTXD
                    eeX.PowerSaveEnable = xData.PowerSaveEnable
                    eeX.RS485EchoSuppress = xData.RS485EchoSuppress
                    eeX.IsVCP = xData.DriverType
                End If
            Else
                Throw New SystemException("Сбой при загрузке функции FT_EE_Read().")
            End If
            Return eeX
        End Function

#End Region '/ЧТЕНИЕ ПЗУ РАЗНЫХ ТИПОВ УСТРОЙСТВ

#End Region '/ЧТЕНИЕ ПЗУ

#Region "ЗАПИСЬ ПЗУ"

        ''' <summary>
        ''' Стирает всё содержимое ПЗУ, включая пользовательскую область данных. 
        ''' </summary>
        ''' <remarks>Устройства FT232R и FT245R с внутренним ПЗУ не могут быть очищены.</remarks>
        Public Sub EraseEeprom()
            If LibraryInitialized Then
                If (Me.pFT_EraseEE <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim deviceType As FT_DEVICE = GetDeviceType()
                        If (deviceType = FT_DEVICE.FT_DEVICE_232R) Then
                            CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                        End If
                        Dim delegateForFunctionPointer As tFT_EraseEE = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EraseEE, GetType(tFT_EraseEE)), tFT_EraseEE)
                        Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle)
                        CheckStatus(ftStatus)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_EraseEE().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Записывает значение в ПЗУ по заданному адресу.
        ''' </summary>
        ''' <param name="address">Адрес в ПЗУ.</param>
        ''' <param name="eeValue">16-разрядное значение для записи в ПЗУ.</param>
        Public Sub WriteEeprom(ByVal address As UInt32, ByVal eeValue As UInt16)
            If LibraryInitialized Then
                If (Me.pFT_WriteEE <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim delegateForFunctionPointer As tFT_WriteEE = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_WriteEE, GetType(tFT_WriteEE)), tFT_WriteEE)
                        Dim ft_status As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, address, eeValue)
                        CheckStatus(ft_status)
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_WriteEE().")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Записывает данные в ПЗУ устройства.
        ''' </summary>
        ''' <param name="eeprom"></param>
        Public Sub WriteEeprom(ByVal eeprom As FT2232_EEPROM_STRUCTURE)
            WriteEepromFT2232(eeprom)
        End Sub

        ''' <summary>
        ''' Записывает данные в ПЗУ устройства.
        ''' </summary>
        Public Sub WriteEeprom(ByVal eeprom As FT2232H_EEPROM_STRUCTURE)
            WriteEepromFT2232H(eeprom)
        End Sub

        ''' <summary>
        ''' Записывает данные в ПЗУ устройства.
        ''' </summary>
        Public Sub WriteEeprom(ByVal eeprom As FT232H_EEPROM_STRUCTURE)
            WriteEepromFT232H(eeprom)
        End Sub

        ''' <summary>
        ''' Записывает данные в ПЗУ устройства.
        ''' </summary>
        Public Sub WriteEeprom(ByVal eeprom As FT232R_EEPROM_STRUCTURE)
            WriteEepromFT232R(eeprom)
        End Sub

        ''' <summary>
        ''' Записывает данные в ПЗУ устройства.
        ''' </summary>
        Public Sub WriteEeprom(ByVal eeprom As FT232B_EEPROM_STRUCTURE)
            WriteEepromFT232B(eeprom)
        End Sub

        ''' <summary>
        ''' Записывает данные в ПЗУ устройства.
        ''' </summary>
        Public Sub WriteEeprom(ByVal eeprom As FT4232H_EEPROM_STRUCTURE)
            WriteEepromFT4232H(eeprom)
        End Sub

        ''' <summary>
        ''' Записывает данные в ПЗУ устройства.
        ''' </summary>
        Public Sub WriteEeprom(ByVal eeprom As FT_XSERIES_EEPROM_STRUCTURE)
            WriteEepromXSeries(eeprom)
        End Sub

        Private Sub WriteEepromFT2232(ByVal ee2232 As FT2232_EEPROM_STRUCTURE)
            If LibraryInitialized Then
                If (Me.pFT_EE_Program <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                        If (deviceType <> FT_DEVICE.FT_DEVICE_2232) Then
                            CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                        End If
                        If (ee2232.VendorID = 0) OrElse (ee2232.ProductID = 0) Then
                            CheckStatus(FT_STATUS.FT_INVALID_PARAMETER)
                        End If
                        If (ee2232.Manufacturer.Length > &H20) Then
                            ee2232.Manufacturer = ee2232.Manufacturer.Substring(0, &H20)
                        End If
                        If (ee2232.ManufacturerID.Length > &H10) Then
                            ee2232.ManufacturerID = ee2232.ManufacturerID.Substring(0, &H10)
                        End If
                        If (ee2232.Description.Length > &H40) Then
                            ee2232.Description = ee2232.Description.Substring(0, &H40)
                        End If
                        If (ee2232.SerialNumber.Length > &H10) Then
                            ee2232.SerialNumber = ee2232.SerialNumber.Substring(0, &H10)
                        End If
                        Dim pData As New FT_PROGRAM_DATA With {
                            .Signature1 = 0,
                            .Signature2 = UInt32.MaxValue,
                            .Version = 2,
                            .Manufacturer = Marshal.AllocHGlobal(&H20),
                            .ManufacturerID = Marshal.AllocHGlobal(&H10),
                            .Description = Marshal.AllocHGlobal(&H40),
                            .SerialNumber = Marshal.AllocHGlobal(&H10),
                            .VendorID = ee2232.VendorID,
                            .ProductID = ee2232.ProductID,
                            .MaxPower = ee2232.MaxPower,
                            .SelfPowered = Convert.ToUInt16(ee2232.SelfPowered),
                            .RemoteWakeup = Convert.ToUInt16(ee2232.RemoteWakeup),
                            .Rev5 = Convert.ToByte(True),
                            .PullDownEnable5 = Convert.ToByte(ee2232.PullDownEnable),
                            .SerNumEnable5 = Convert.ToByte(ee2232.SerNumEnable),
                            .USBVersionEnable5 = Convert.ToByte(ee2232.USBVersionEnable),
                            .USBVersion5 = ee2232.USBVersion,
                            .AIsHighCurrent = Convert.ToByte(ee2232.AIsHighCurrent),
                            .BIsHighCurrent = Convert.ToByte(ee2232.BIsHighCurrent),
                            .IFAIsFifo = Convert.ToByte(ee2232.IFAIsFifo),
                            .IFAIsFifoTar = Convert.ToByte(ee2232.IFAIsFifoTar),
                            .IFAIsFastSer = Convert.ToByte(ee2232.IFAIsFastSer),
                            .AIsVCP = Convert.ToByte(ee2232.AIsVCP),
                            .IFBIsFifo = Convert.ToByte(ee2232.IFBIsFifo),
                            .IFBIsFifoTar = Convert.ToByte(ee2232.IFBIsFifoTar),
                            .IFBIsFastSer = Convert.ToByte(ee2232.IFBIsFastSer),
                            .BIsVCP = Convert.ToByte(ee2232.BIsVCP)
                        }
                        pData.Manufacturer = Marshal.StringToHGlobalAnsi(ee2232.Manufacturer)
                        pData.ManufacturerID = Marshal.StringToHGlobalAnsi(ee2232.ManufacturerID)
                        pData.Description = Marshal.StringToHGlobalAnsi(ee2232.Description)
                        pData.SerialNumber = Marshal.StringToHGlobalAnsi(ee2232.SerialNumber)
                        Try
                            Dim delegateForFunctionPointer As tFT_EE_Program = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Program, GetType(tFT_EE_Program)), tFT_EE_Program)
                            Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                            CheckStatus(ftStatus) 'дождёмся освобождения ресурсов Marshal.FreeHGlobal() и проверим статус
                        Catch ex As Exception
                            Throw
                        Finally
                            Marshal.FreeHGlobal(pData.Manufacturer)
                            Marshal.FreeHGlobal(pData.ManufacturerID)
                            Marshal.FreeHGlobal(pData.Description)
                            Marshal.FreeHGlobal(pData.SerialNumber)
                        End Try
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_EE_Program().")
                End If
            End If
        End Sub

        Private Sub WriteEepromFT2232H(ByVal ee2232h As FT2232H_EEPROM_STRUCTURE)
            If LibraryInitialized Then
                If (Me.pFT_EE_Program <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                        If (deviceType <> FT_DEVICE.FT_DEVICE_2232H) Then
                            CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                        End If
                        If (ee2232h.VendorID = 0) OrElse (ee2232h.ProductID = 0) Then
                            CheckStatus(FT_STATUS.FT_INVALID_PARAMETER)
                        End If
                        If (ee2232h.Manufacturer.Length > &H20) Then
                            ee2232h.Manufacturer = ee2232h.Manufacturer.Substring(0, &H20)
                        End If
                        If (ee2232h.ManufacturerID.Length > &H10) Then
                            ee2232h.ManufacturerID = ee2232h.ManufacturerID.Substring(0, &H10)
                        End If
                        If (ee2232h.Description.Length > &H40) Then
                            ee2232h.Description = ee2232h.Description.Substring(0, &H40)
                        End If
                        If (ee2232h.SerialNumber.Length > &H10) Then
                            ee2232h.SerialNumber = ee2232h.SerialNumber.Substring(0, &H10)
                        End If
                        Dim pData As New FT_PROGRAM_DATA With {
                            .Signature1 = 0,
                            .Signature2 = UInt32.MaxValue,
                            .Version = 3,
                            .Manufacturer = Marshal.AllocHGlobal(&H20),
                            .ManufacturerID = Marshal.AllocHGlobal(&H10),
                            .Description = Marshal.AllocHGlobal(&H40),
                            .SerialNumber = Marshal.AllocHGlobal(&H10),
                            .VendorID = ee2232h.VendorID,
                            .ProductID = ee2232h.ProductID,
                            .MaxPower = ee2232h.MaxPower,
                            .SelfPowered = Convert.ToUInt16(ee2232h.SelfPowered),
                            .RemoteWakeup = Convert.ToUInt16(ee2232h.RemoteWakeup),
                            .PullDownEnable7 = Convert.ToByte(ee2232h.PullDownEnable),
                            .SerNumEnable7 = Convert.ToByte(ee2232h.SerNumEnable),
                            .ALSlowSlew = Convert.ToByte(ee2232h.ALSlowSlew),
                            .ALSchmittInput = Convert.ToByte(ee2232h.ALSchmittInput),
                            .ALDriveCurrent = ee2232h.ALDriveCurrent,
                            .AHSlowSlew = Convert.ToByte(ee2232h.AHSlowSlew),
                            .AHSchmittInput = Convert.ToByte(ee2232h.AHSchmittInput),
                            .AHDriveCurrent = ee2232h.AHDriveCurrent,
                            .BLSlowSlew = Convert.ToByte(ee2232h.BLSlowSlew),
                            .BLSchmittInput = Convert.ToByte(ee2232h.BLSchmittInput),
                            .BLDriveCurrent = ee2232h.BLDriveCurrent,
                            .BHSlowSlew = Convert.ToByte(ee2232h.BHSlowSlew),
                            .BHSchmittInput = Convert.ToByte(ee2232h.BHSchmittInput),
                            .BHDriveCurrent = ee2232h.BHDriveCurrent,
                            .IFAIsFifo7 = Convert.ToByte(ee2232h.IFAIsFifo),
                            .IFAIsFifoTar7 = Convert.ToByte(ee2232h.IFAIsFifoTar),
                            .IFAIsFastSer7 = Convert.ToByte(ee2232h.IFAIsFastSer),
                            .AIsVCP7 = Convert.ToByte(ee2232h.AIsVCP),
                            .IFBIsFifo7 = Convert.ToByte(ee2232h.IFBIsFifo),
                            .IFBIsFifoTar7 = Convert.ToByte(ee2232h.IFBIsFifoTar),
                            .IFBIsFastSer7 = Convert.ToByte(ee2232h.IFBIsFastSer),
                            .BIsVCP7 = Convert.ToByte(ee2232h.BIsVCP),
                            .PowerSaveEnable = Convert.ToByte(ee2232h.PowerSaveEnable)
                        }
                        pData.Manufacturer = Marshal.StringToHGlobalAnsi(ee2232h.Manufacturer)
                        pData.ManufacturerID = Marshal.StringToHGlobalAnsi(ee2232h.ManufacturerID)
                        pData.Description = Marshal.StringToHGlobalAnsi(ee2232h.Description)
                        pData.SerialNumber = Marshal.StringToHGlobalAnsi(ee2232h.SerialNumber)
                        Try
                            Dim delegateForFunctionPointer As tFT_EE_Program = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Program, GetType(tFT_EE_Program)), tFT_EE_Program)
                            Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                            CheckStatus(ftStatus)
                        Catch ex As Exception
                            Throw
                        Finally
                            Marshal.FreeHGlobal(pData.Manufacturer)
                            Marshal.FreeHGlobal(pData.ManufacturerID)
                            Marshal.FreeHGlobal(pData.Description)
                            Marshal.FreeHGlobal(pData.SerialNumber)
                        End Try
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_EE_Program().")
                End If
            End If
        End Sub

        Private Sub WriteEepromFT232B(ByVal ee232b As FT232B_EEPROM_STRUCTURE)
            If LibraryInitialized Then
                If (Me.pFT_EE_Program <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                        If (deviceType > FT_DEVICE.FT_DEVICE_BM) Then
                            CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                        End If
                        If (ee232b.VendorID = 0) OrElse (ee232b.ProductID = 0) Then
                            CheckStatus(FT_STATUS.FT_INVALID_PARAMETER)
                        End If
                        If (ee232b.Manufacturer.Length > &H20) Then
                            ee232b.Manufacturer = ee232b.Manufacturer.Substring(0, &H20)
                        End If
                        If (ee232b.ManufacturerID.Length > &H10) Then
                            ee232b.ManufacturerID = ee232b.ManufacturerID.Substring(0, &H10)
                        End If
                        If (ee232b.Description.Length > &H40) Then
                            ee232b.Description = ee232b.Description.Substring(0, &H40)
                        End If
                        If (ee232b.SerialNumber.Length > &H10) Then
                            ee232b.SerialNumber = ee232b.SerialNumber.Substring(0, &H10)
                        End If
                        Dim pData As New FT_PROGRAM_DATA With {
                            .Signature1 = 0,
                            .Signature2 = UInt32.MaxValue,
                            .Version = 2,
                            .Manufacturer = Marshal.AllocHGlobal(&H20),
                            .ManufacturerID = Marshal.AllocHGlobal(&H10),
                            .Description = Marshal.AllocHGlobal(&H40),
                            .SerialNumber = Marshal.AllocHGlobal(&H10),
                            .VendorID = ee232b.VendorID,
                            .ProductID = ee232b.ProductID,
                            .MaxPower = ee232b.MaxPower,
                            .SelfPowered = Convert.ToUInt16(ee232b.SelfPowered),
                            .RemoteWakeup = Convert.ToUInt16(ee232b.RemoteWakeup),
                            .Rev4 = Convert.ToByte(True),
                            .PullDownEnable = Convert.ToByte(ee232b.PullDownEnable),
                            .SerNumEnable = Convert.ToByte(ee232b.SerNumEnable),
                            .USBVersionEnable = Convert.ToByte(ee232b.USBVersionEnable),
                            .USBVersion = ee232b.USBVersion
                        }
                        pData.Manufacturer = Marshal.StringToHGlobalAnsi(ee232b.Manufacturer)
                        pData.ManufacturerID = Marshal.StringToHGlobalAnsi(ee232b.ManufacturerID)
                        pData.Description = Marshal.StringToHGlobalAnsi(ee232b.Description)
                        pData.SerialNumber = Marshal.StringToHGlobalAnsi(ee232b.SerialNumber)
                        Try
                            Dim delegateForFunctionPointer As tFT_EE_Program = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Program, GetType(tFT_EE_Program)), tFT_EE_Program)
                            Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                            CheckStatus(ftStatus)
                        Catch ex As Exception
                            Throw
                        Finally
                            Marshal.FreeHGlobal(pData.Manufacturer)
                            Marshal.FreeHGlobal(pData.ManufacturerID)
                            Marshal.FreeHGlobal(pData.Description)
                            Marshal.FreeHGlobal(pData.SerialNumber)
                        End Try
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_EE_Program().")
                End If
            End If
        End Sub

        Private Sub WriteEepromFT232H(ByVal ee232h As FT232H_EEPROM_STRUCTURE)
            If LibraryInitialized Then
                If (Me.pFT_EE_Program <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                        If (deviceType <> FT_DEVICE.FT_DEVICE_232H) Then
                            CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                        End If
                        If (ee232h.VendorID = 0) OrElse (ee232h.ProductID = 0) Then
                            CheckStatus(FT_STATUS.FT_INVALID_PARAMETER)
                        End If
                        If (ee232h.Manufacturer.Length > &H20) Then
                            ee232h.Manufacturer = ee232h.Manufacturer.Substring(0, &H20)
                        End If
                        If (ee232h.ManufacturerID.Length > &H10) Then
                            ee232h.ManufacturerID = ee232h.ManufacturerID.Substring(0, &H10)
                        End If
                        If (ee232h.Description.Length > &H40) Then
                            ee232h.Description = ee232h.Description.Substring(0, &H40)
                        End If
                        If (ee232h.SerialNumber.Length > &H10) Then
                            ee232h.SerialNumber = ee232h.SerialNumber.Substring(0, &H10)
                        End If
                        Dim pData As New FT_PROGRAM_DATA With {
                            .Signature1 = 0,
                            .Signature2 = UInt32.MaxValue,
                            .Version = 5,
                            .Manufacturer = Marshal.AllocHGlobal(&H20),
                            .ManufacturerID = Marshal.AllocHGlobal(&H10),
                            .Description = Marshal.AllocHGlobal(&H40),
                            .SerialNumber = Marshal.AllocHGlobal(&H10),
                            .VendorID = ee232h.VendorID,
                            .ProductID = ee232h.ProductID,
                            .MaxPower = ee232h.MaxPower,
                            .SelfPowered = Convert.ToUInt16(ee232h.SelfPowered),
                            .RemoteWakeup = Convert.ToUInt16(ee232h.RemoteWakeup),
                            .PullDownEnableH = Convert.ToByte(ee232h.PullDownEnable),
                            .SerNumEnableH = Convert.ToByte(ee232h.SerNumEnable),
                            .ACSlowSlewH = Convert.ToByte(ee232h.ACSlowSlew),
                            .ACSchmittInputH = Convert.ToByte(ee232h.ACSchmittInput),
                            .ACDriveCurrentH = Convert.ToByte(ee232h.ACDriveCurrent),
                            .ADSlowSlewH = Convert.ToByte(ee232h.ADSlowSlew),
                            .ADSchmittInputH = Convert.ToByte(ee232h.ADSchmittInput),
                            .ADDriveCurrentH = Convert.ToByte(ee232h.ADDriveCurrent),
                            .Cbus0H = Convert.ToByte(ee232h.Cbus0),
                            .Cbus1H = Convert.ToByte(ee232h.Cbus1),
                            .Cbus2H = Convert.ToByte(ee232h.Cbus2),
                            .Cbus3H = Convert.ToByte(ee232h.Cbus3),
                            .Cbus4H = Convert.ToByte(ee232h.Cbus4),
                            .Cbus5H = Convert.ToByte(ee232h.Cbus5),
                            .Cbus6H = Convert.ToByte(ee232h.Cbus6),
                            .Cbus7H = Convert.ToByte(ee232h.Cbus7),
                            .Cbus8H = Convert.ToByte(ee232h.Cbus8),
                            .Cbus9H = Convert.ToByte(ee232h.Cbus9),
                            .IsFifoH = Convert.ToByte(ee232h.IsFifo),
                            .IsFifoTarH = Convert.ToByte(ee232h.IsFifoTar),
                            .IsFastSerH = Convert.ToByte(ee232h.IsFastSer),
                            .IsFT1248H = Convert.ToByte(ee232h.IsFT1248),
                            .FT1248CpolH = Convert.ToByte(ee232h.FT1248Cpol),
                            .FT1248LsbH = Convert.ToByte(ee232h.FT1248Lsb),
                            .FT1248FlowControlH = Convert.ToByte(ee232h.FT1248FlowControl),
                            .IsVCPH = Convert.ToByte(ee232h.IsVCP),
                            .PowerSaveEnableH = Convert.ToByte(ee232h.PowerSaveEnable)
                        }
                        pData.Manufacturer = Marshal.StringToHGlobalAnsi(ee232h.Manufacturer)
                        pData.ManufacturerID = Marshal.StringToHGlobalAnsi(ee232h.ManufacturerID)
                        pData.Description = Marshal.StringToHGlobalAnsi(ee232h.Description)
                        pData.SerialNumber = Marshal.StringToHGlobalAnsi(ee232h.SerialNumber)
                        Try
                            Dim delegateForFunctionPointer As tFT_EE_Program = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Program, GetType(tFT_EE_Program)), tFT_EE_Program)
                            Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                            CheckStatus(ftStatus)
                        Catch ex As Exception
                            Throw
                        Finally
                            Marshal.FreeHGlobal(pData.Manufacturer)
                            Marshal.FreeHGlobal(pData.ManufacturerID)
                            Marshal.FreeHGlobal(pData.Description)
                            Marshal.FreeHGlobal(pData.SerialNumber)
                        End Try
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_EE_Program().")
                End If
            End If
        End Sub

        Private Sub WriteEepromFT232R(ByVal ee232r As FT232R_EEPROM_STRUCTURE)
            If LibraryInitialized Then
                If (Me.pFT_EE_Program <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                        If (deviceType <> FT_DEVICE.FT_DEVICE_232R) Then
                            CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                        End If
                        If (ee232r.VendorID = 0) OrElse (ee232r.ProductID = 0) Then
                            CheckStatus(FT_STATUS.FT_INVALID_PARAMETER)
                        End If
                        If (ee232r.Manufacturer.Length > &H20) Then
                            ee232r.Manufacturer = ee232r.Manufacturer.Substring(0, &H20)
                        End If
                        If (ee232r.ManufacturerID.Length > &H10) Then
                            ee232r.ManufacturerID = ee232r.ManufacturerID.Substring(0, &H10)
                        End If
                        If (ee232r.Description.Length > &H40) Then
                            ee232r.Description = ee232r.Description.Substring(0, &H40)
                        End If
                        If (ee232r.SerialNumber.Length > &H10) Then
                            ee232r.SerialNumber = ee232r.SerialNumber.Substring(0, &H10)
                        End If
                        Dim pData As New FT_PROGRAM_DATA With {
                            .Signature1 = 0,
                            .Signature2 = UInt32.MaxValue,
                            .Version = 2,
                            .Manufacturer = Marshal.AllocHGlobal(&H20),
                            .ManufacturerID = Marshal.AllocHGlobal(&H10),
                            .Description = Marshal.AllocHGlobal(&H40),
                            .SerialNumber = Marshal.AllocHGlobal(&H10),
                            .VendorID = ee232r.VendorID,
                            .ProductID = ee232r.ProductID,
                            .MaxPower = ee232r.MaxPower,
                            .SelfPowered = Convert.ToUInt16(ee232r.SelfPowered),
                            .RemoteWakeup = Convert.ToUInt16(ee232r.RemoteWakeup),
                            .PullDownEnableR = Convert.ToByte(ee232r.PullDownEnable),
                            .SerNumEnableR = Convert.ToByte(ee232r.SerNumEnable),
                            .UseExtOsc = Convert.ToByte(ee232r.UseExtOsc),
                            .HighDriveIOs = Convert.ToByte(ee232r.HighDriveIOs),
                            .EndpointSize = &H40,
                            .InvertTXD = Convert.ToByte(ee232r.InvertTXD),
                            .InvertRXD = Convert.ToByte(ee232r.InvertRXD),
                            .InvertRTS = Convert.ToByte(ee232r.InvertRTS),
                            .InvertCTS = Convert.ToByte(ee232r.InvertCTS),
                            .InvertDTR = Convert.ToByte(ee232r.InvertDTR),
                            .InvertDSR = Convert.ToByte(ee232r.InvertDSR),
                            .InvertDCD = Convert.ToByte(ee232r.InvertDCD),
                            .InvertRI = Convert.ToByte(ee232r.InvertRI),
                            .Cbus0 = ee232r.Cbus0,
                            .Cbus1 = ee232r.Cbus1,
                            .Cbus2 = ee232r.Cbus2,
                            .Cbus3 = ee232r.Cbus3,
                            .Cbus4 = ee232r.Cbus4,
                            .RIsD2XX = Convert.ToByte(ee232r.RIsD2XX)
                        }
                        pData.Manufacturer = Marshal.StringToHGlobalAnsi(ee232r.Manufacturer)
                        pData.ManufacturerID = Marshal.StringToHGlobalAnsi(ee232r.ManufacturerID)
                        pData.Description = Marshal.StringToHGlobalAnsi(ee232r.Description)
                        pData.SerialNumber = Marshal.StringToHGlobalAnsi(ee232r.SerialNumber)
                        Try
                            Dim delegateForFunctionPointer As tFT_EE_Program = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Program, GetType(tFT_EE_Program)), tFT_EE_Program)
                            Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                            CheckStatus(ftStatus)
                        Catch ex As Exception
                            Throw
                        Finally
                            Marshal.FreeHGlobal(pData.Manufacturer)
                            Marshal.FreeHGlobal(pData.ManufacturerID)
                            Marshal.FreeHGlobal(pData.Description)
                            Marshal.FreeHGlobal(pData.SerialNumber)
                        End Try
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_EE_Program().")
                End If
            End If
        End Sub

        Private Sub WriteEepromFT4232H(ByVal ee4232h As FT4232H_EEPROM_STRUCTURE)
            If LibraryInitialized Then
                If (Me.pFT_EE_Program <> IntPtr.Zero) Then
                    If Me.IsOpen Then
                        Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                        If (deviceType <> FT_DEVICE.FT_DEVICE_4232H) Then
                            CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                        End If
                        If (ee4232h.VendorID = 0) OrElse (ee4232h.ProductID = 0) Then
                            CheckStatus(FT_STATUS.FT_INVALID_PARAMETER)
                        End If
                        If (ee4232h.Manufacturer.Length > &H20) Then
                            ee4232h.Manufacturer = ee4232h.Manufacturer.Substring(0, &H20)
                        End If
                        If (ee4232h.ManufacturerID.Length > &H10) Then
                            ee4232h.ManufacturerID = ee4232h.ManufacturerID.Substring(0, &H10)
                        End If
                        If (ee4232h.Description.Length > &H40) Then
                            ee4232h.Description = ee4232h.Description.Substring(0, &H40)
                        End If
                        If (ee4232h.SerialNumber.Length > &H10) Then
                            ee4232h.SerialNumber = ee4232h.SerialNumber.Substring(0, &H10)
                        End If
                        Dim pData As New FT_PROGRAM_DATA With {
                            .Signature1 = 0,
                            .Signature2 = UInt32.MaxValue,
                            .Version = 4,
                            .Manufacturer = Marshal.AllocHGlobal(&H20),
                            .ManufacturerID = Marshal.AllocHGlobal(&H10),
                            .Description = Marshal.AllocHGlobal(&H40),
                            .SerialNumber = Marshal.AllocHGlobal(&H10),
                            .VendorID = ee4232h.VendorID,
                            .ProductID = ee4232h.ProductID,
                            .MaxPower = ee4232h.MaxPower,
                            .SelfPowered = Convert.ToUInt16(ee4232h.SelfPowered),
                            .RemoteWakeup = Convert.ToUInt16(ee4232h.RemoteWakeup),
                            .PullDownEnable8 = Convert.ToByte(ee4232h.PullDownEnable),
                            .SerNumEnable8 = Convert.ToByte(ee4232h.SerNumEnable),
                            .ASlowSlew = Convert.ToByte(ee4232h.ASlowSlew),
                            .ASchmittInput = Convert.ToByte(ee4232h.ASchmittInput),
                            .ADriveCurrent = ee4232h.ADriveCurrent,
                            .BSlowSlew = Convert.ToByte(ee4232h.BSlowSlew),
                            .BSchmittInput = Convert.ToByte(ee4232h.BSchmittInput),
                            .BDriveCurrent = ee4232h.BDriveCurrent,
                            .CSlowSlew = Convert.ToByte(ee4232h.CSlowSlew),
                            .CSchmittInput = Convert.ToByte(ee4232h.CSchmittInput),
                            .CDriveCurrent = ee4232h.CDriveCurrent,
                            .DSlowSlew = Convert.ToByte(ee4232h.DSlowSlew),
                            .DSchmittInput = Convert.ToByte(ee4232h.DSchmittInput),
                            .DDriveCurrent = ee4232h.DDriveCurrent,
                            .ARIIsTXDEN = Convert.ToByte(ee4232h.ARIIsTXDEN),
                            .BRIIsTXDEN = Convert.ToByte(ee4232h.BRIIsTXDEN),
                            .CRIIsTXDEN = Convert.ToByte(ee4232h.CRIIsTXDEN),
                            .DRIIsTXDEN = Convert.ToByte(ee4232h.DRIIsTXDEN),
                            .AIsVCP8 = Convert.ToByte(ee4232h.AIsVCP),
                            .BIsVCP8 = Convert.ToByte(ee4232h.BIsVCP),
                            .CIsVCP8 = Convert.ToByte(ee4232h.CIsVCP),
                            .DIsVCP8 = Convert.ToByte(ee4232h.DIsVCP)
                        }
                        pData.Manufacturer = Marshal.StringToHGlobalAnsi(ee4232h.Manufacturer)
                        pData.ManufacturerID = Marshal.StringToHGlobalAnsi(ee4232h.ManufacturerID)
                        pData.Description = Marshal.StringToHGlobalAnsi(ee4232h.Description)
                        pData.SerialNumber = Marshal.StringToHGlobalAnsi(ee4232h.SerialNumber)
                        Try
                            Dim delegateForFunctionPointer As tFT_EE_Program = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EE_Program, GetType(tFT_EE_Program)), tFT_EE_Program)
                            Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, pData)
                            CheckStatus(ftStatus)
                        Catch ex As Exception
                            Throw
                        Finally
                            Marshal.FreeHGlobal(pData.Manufacturer)
                            Marshal.FreeHGlobal(pData.ManufacturerID)
                            Marshal.FreeHGlobal(pData.Description)
                            Marshal.FreeHGlobal(pData.SerialNumber)
                        End Try
                    End If
                Else
                    Throw New SystemException("Сбой при загрузке функции FT_EE_Program().")
                End If
            End If
        End Sub

        Private Sub WriteEepromXSeries(ByVal eeX As FT_XSERIES_EEPROM_STRUCTURE)
            If LibraryInitialized AndAlso (Me.pFT_EEPROM_Program <> IntPtr.Zero) Then
                If Me.IsOpen Then
                    Dim deviceType As FT_DEVICE = Me.GetDeviceType()
                    If (deviceType <> FT_DEVICE.FT_DEVICE_X_SERIES) Then
                        CheckStatus(FT_ERROR.FT_INCORRECT_DEVICE)
                    End If
                    If (eeX.VendorID = 0) OrElse (eeX.ProductID = 0) Then
                        CheckStatus(FT_STATUS.FT_INVALID_PARAMETER)
                    End If
                    If (eeX.Manufacturer.Length > &H20) Then
                        eeX.Manufacturer = eeX.Manufacturer.Substring(0, &H20)
                    End If
                    If (eeX.ManufacturerID.Length > &H10) Then
                        eeX.ManufacturerID = eeX.ManufacturerID.Substring(0, &H10)
                    End If
                    If (eeX.Description.Length > &H40) Then
                        eeX.Description = eeX.Description.Substring(0, &H40)
                    End If
                    If (eeX.SerialNumber.Length > &H10) Then
                        eeX.SerialNumber = eeX.SerialNumber.Substring(0, &H10)
                    End If
                    Dim encoding As New UTF8Encoding
                    Dim manufacturer As Byte() = New Byte(&H20 - 1) {}
                    manufacturer = encoding.GetBytes(eeX.Manufacturer)
                    Dim manufacturerID As Byte() = New Byte(&H10 - 1) {}
                    manufacturerID = encoding.GetBytes(eeX.ManufacturerID)
                    Dim description As Byte() = New Byte(&H40 - 1) {}
                    description = encoding.GetBytes(eeX.Description)
                    Dim serialnumber As Byte() = New Byte(&H10 - 1) {}
                    serialnumber = encoding.GetBytes(eeX.SerialNumber)
                    Dim xData As New FT_XSERIES_DATA With {
                        .Cbus0 = eeX.Cbus0,
                        .Cbus1 = eeX.Cbus1,
                        .Cbus2 = eeX.Cbus2,
                        .Cbus3 = eeX.Cbus3,
                        .Cbus4 = eeX.Cbus4,
                        .Cbus5 = eeX.Cbus5,
                        .Cbus6 = eeX.Cbus6,
                        .ACDriveCurrent = eeX.ACDriveCurrent,
                        .ACSchmittInput = eeX.ACSchmittInput,
                        .ACSlowSlew = eeX.ACSlowSlew,
                        .ADDriveCurrent = eeX.ADDriveCurrent,
                        .ADSchmittInput = eeX.ADSchmittInput,
                        .ADSlowSlew = eeX.ADSlowSlew,
                        .BCDDisableSleep = eeX.BCDDisableSleep,
                        .BCDEnable = eeX.BCDEnable,
                        .BCDForceCbusPWREN = eeX.BCDForceCbusPWREN,
                        .FT1248Cpol = eeX.FT1248Cpol,
                        .FT1248FlowControl = eeX.FT1248FlowControl,
                        .FT1248Lsb = eeX.FT1248Lsb,
                        .I2CDeviceId = eeX.I2CDeviceId,
                        .I2CDisableSchmitt = eeX.I2CDisableSchmitt,
                        .I2CSlaveAddress = eeX.I2CSlaveAddress,
                        .InvertCTS = eeX.InvertCTS,
                        .InvertDCD = eeX.InvertDCD,
                        .InvertDSR = eeX.InvertDSR,
                        .InvertDTR = eeX.InvertDTR,
                        .InvertRI = eeX.InvertRI,
                        .InvertRTS = eeX.InvertRTS,
                        .InvertRXD = eeX.InvertRXD,
                        .InvertTXD = eeX.InvertTXD,
                        .PowerSaveEnable = eeX.PowerSaveEnable,
                        .RS485EchoSuppress = eeX.RS485EchoSuppress,
                        .DriverType = eeX.IsVCP
                    }
                    xData.common.deviceType = 9
                    xData.common.VendorId = eeX.VendorID
                    xData.common.ProductId = eeX.ProductID
                    xData.common.MaxPower = eeX.MaxPower
                    xData.common.SelfPowered = Convert.ToByte(eeX.SelfPowered)
                    xData.common.RemoteWakeup = Convert.ToByte(eeX.RemoteWakeup)
                    xData.common.SerNumEnable = Convert.ToByte(eeX.SerNumEnable)
                    xData.common.PullDownEnable = Convert.ToByte(eeX.PullDownEnable)
                    Dim cb As Integer = Marshal.SizeOf(xData)
                    Dim ptr As IntPtr = Marshal.AllocHGlobal(cb)
                    Marshal.StructureToPtr(xData, ptr, False)
                    Dim delegateForFunctionPointer As tFT_EEPROM_Program = DirectCast(Marshal.GetDelegateForFunctionPointer(Me.pFT_EEPROM_Program, GetType(tFT_EEPROM_Program)), tFT_EEPROM_Program)
                    Dim ftStatus As FT_STATUS = delegateForFunctionPointer.Invoke(Me.ftHandle, ptr, CUInt(cb), manufacturer, manufacturerID, description, serialnumber)
                    CheckStatus(ftStatus)
                End If
            End If
        End Sub

#End Region '/ЗАПИСЬ ПЗУ

#End Region '/РАБОТА С ПЗУ

#Region "ПРОВЕРКА СТАТУСА"

        ''' <summary>
        ''' Проверяет статус операции и в случае успеха возвращает True, иначе выбрасывает исключениею.
        ''' </summary>
        ''' <param name="ftStatus"></param>
        Private Shared Sub CheckStatus(ByVal ftStatus As FT_STATUS)
            If (ftStatus > FT_STATUS.FT_OK) Then
                Select Case ftStatus
                    Case FT_STATUS.FT_INVALID_HANDLE
                        Throw New FT_EXCEPTION("Неверный дескриптор устройства FTDI.")
                    Case FT_STATUS.FT_DEVICE_NOT_FOUND
                        Throw New FT_EXCEPTION("Устройство FTDI не найдено.")
                    Case FT_STATUS.FT_DEVICE_NOT_OPENED
                        Throw New FT_EXCEPTION("Устройство FTDI не открыто или не может быть открыто.")
                    Case FT_STATUS.FT_IO_ERROR
                        Throw New FT_EXCEPTION("Ошибка чтения/записи устройства FTDI.")
                    Case FT_STATUS.FT_INSUFFICIENT_RESOURCES
                        Throw New FT_EXCEPTION("Недостаточно ресурсов.")
                    Case FT_STATUS.FT_INVALID_PARAMETER
                        Throw New FT_EXCEPTION("Неверный параметр для вызова функции библиотеки FTD2XX.")
                    Case FT_STATUS.FT_INVALID_BAUD_RATE
                        Throw New FT_EXCEPTION("Неверная скорость устройства FTDI.")
                    Case FT_STATUS.FT_DEVICE_NOT_OPENED_FOR_ERASE
                        Throw New FT_EXCEPTION("Устройство FTDI не открыто для стирания.")
                    Case FT_STATUS.FT_DEVICE_NOT_OPENED_FOR_WRITE
                        Throw New FT_EXCEPTION("Устройство FTDI не открыто для записи.")
                    Case FT_STATUS.FT_FAILED_TO_WRITE_DEVICE
                        Throw New FT_EXCEPTION("Сбой записи в устройство FTDI.")
                    Case FT_STATUS.FT_EEPROM_READ_FAILED
                        Throw New FT_EXCEPTION("Сбой чтения из ПЗУ устройства FTDI.")
                    Case FT_STATUS.FT_EEPROM_WRITE_FAILED
                        Throw New FT_EXCEPTION("Сбой записи в ПЗУ устройства FTDI.")
                    Case FT_STATUS.FT_EEPROM_ERASE_FAILED
                        Throw New FT_EXCEPTION("Сбой стирания ПЗУ устройства FTDI.")
                    Case FT_STATUS.FT_EEPROM_NOT_PRESENT
                        Throw New FT_EXCEPTION("В данном устройстве FTDI нет ПЗУ.")
                    Case FT_STATUS.FT_EEPROM_NOT_PROGRAMMED
                        Throw New FT_EXCEPTION("ПЗУ устройства FTDI не запрограммировано.")
                    Case FT_STATUS.FT_INVALID_ARGS
                        Throw New FT_EXCEPTION("Неверные аргументы для вызова функции библиотеки FTD2XX.")
                    Case FT_STATUS.FT_OTHER_ERROR
                        Throw New FT_EXCEPTION("Неизвестная ошибка в просцессе обмена с устройством FTDI.")
                    Case Else
                        Throw New SystemException("Неизвестная ошибка в просцессе обмена с устройством FTDI.")
                End Select
            End If
        End Sub

        ''' <summary>
        ''' Проверяет статус операции и в случае успеха возвращает True, иначе выбрасывает исключение.
        ''' </summary>
        Private Shared Sub CheckStatus(ByVal ftErrorCondition As FT_ERROR)
            If (ftErrorCondition > FT_ERROR.FT_NO_ERROR) Then
                Select Case ftErrorCondition
                    Case FT_ERROR.FT_INCORRECT_DEVICE
                        Throw New FT_EXCEPTION("Заданная структура данных не подходит для ПЗУ этого типа устройства FTDI.")
                    Case FT_ERROR.FT_INVALID_BITMODE
                        Throw New FT_EXCEPTION("Заданный битовый режим не подходит для данного устройства FTDI.")
                    Case FT_ERROR.FT_BUFFER_SIZE
                        Throw New FT_EXCEPTION("Размер буфера недостаточен.")
                    Case Else
                        Throw New SystemException("Неизвестная ошибка в просцессе обмена с устройством FTDI.")
                End Select
            End If
        End Sub

#End Region '/ПРОВЕРКА СТАТУСА

#Region "ВСПОМОГАТЕЛЬНЫЕ ТИПЫ ДАННЫХ"

#Region "ПЕРЕЧИСЛЕНИЯ"

        Public Enum OPCODE As Byte
            DIVIDE_BY_5_ON = &H8B
            DIVIDE_BY_5_OFF = &H8A
            SET_CLOCK_DIVISOR = &H86
            LOOPBACK_ON = &H84
            LOOPBACK_OFF = &H85
            SPI_READ_BYTES = &H20
            SPI_WRITE_BYTES = &H10
        End Enum

        Public Enum FT_BIT_MODE As Byte
            FT_BIT_MODE_RESET = &H0
            FT_BIT_MODE_ASYNC_BITBANG = &H1 'Asynchronous Bit Bang
            FT_BIT_MODE_MPSSE = &H2 'MPSSE (FT2232, FT2232H, FT4232H and FT232H devices only)
            FT_BIT_MODE_SYNC_BITBANG = &H4 ' Synchronous Bit Bang (FT232R, FT245R, FT2232, FT2232H, FT4232H and FT232H devices only)
            FT_BIT_MODE_MCU_HOST = &H8 'MCU Host Bus Emulation Mode (FT2232, FT2232H, FT4232H and FT232H devices only)
            FT_BIT_MODE_FAST_SERIAL = &H10 'Fast Opto-Isolated Serial Mode (FT2232, FT2232H, FT4232H and FT232H devices only)
            FT_BIT_MODE_CBUS_BITBANG = &H20 'CBUS Bit Bang Mode (FT232R and FT232H devices only)
            FT_BIT_MODE_SYNC_FIFO = &H40 'Single Channel Synchronous 245 FIFO Mode (FT2232H and FT232H devices only)
        End Enum

        Public Enum FT_232H_CBUS_OPTIONS As Byte
            FT_CBUS_TRISTATE = 0
            FT_CBUS_RXLED = 1
            FT_CBUS_TXLED = 2
            FT_CBUS_TXRXLED = 3
            FT_CBUS_PWREN = 4
            FT_CBUS_SLEEP = 5
            FT_CBUS_DRIVE_0 = 6
            FT_CBUS_DRIVE_1 = 7
            FT_CBUS_IOMODE = 8
            FT_CBUS_TXDEN = 9
            FT_CBUS_CLK30 = 10
            FT_CBUS_CLK15 = 11
            FT_CBUS_CLK7_5 = 12
        End Enum

        Public Enum FT_CBUS_OPTIONS As Byte
            FT_CBUS_TXDEN = 0
            FT_CBUS_PWRON = 1
            FT_CBUS_RXLED = 2
            FT_CBUS_TXLED = 3
            FT_CBUS_TXRXLED = 4
            FT_CBUS_SLEEP = 5
            FT_CBUS_CLK48 = 6
            FT_CBUS_CLK24 = 7
            FT_CBUS_CLK12 = 8
            FT_CBUS_CLK6 = 9
            FT_CBUS_IOMODE = 10
            FT_CBUS_BITBANG_WR = 11
            FT_CBUS_BITBANG_RD = 12
        End Enum

        Public Enum FT_DATA_BITS As Byte
            FT_BITS_7 = 7
            FT_BITS_8 = 8
        End Enum

        Public Enum FT_DEVICE As Byte
            FT_DEVICE_BM = 0
            FT_DEVICE_AM = 1
            FT_DEVICE_100AX = 2
            FT_DEVICE_UNKNOWN = 3
            FT_DEVICE_2232 = 4
            FT_DEVICE_232R = 5
            FT_DEVICE_2232H = 6
            FT_DEVICE_4232H = 7
            FT_DEVICE_232H = 8
            FT_DEVICE_X_SERIES = 9
            FT_DEVICE_4222H_0 = 10
            FT_DEVICE_4222H_1_2 = 11
            FT_DEVICE_4222H_3 = 12
            FT_DEVICE_4222_PROG = 13
        End Enum

        Public Enum FT_DRIVE_CURRENT As Byte
            FT_DRIVE_CURRENT_4MA = 4
            FT_DRIVE_CURRENT_8MA = 8
            FT_DRIVE_CURRENT_12MA = 12
            FT_DRIVE_CURRENT_16MA = &H10
        End Enum

        Private Enum FT_ERROR As Byte
            FT_NO_ERROR = 0
            FT_INCORRECT_DEVICE = 1
            FT_INVALID_BITMODE = 2
            FT_BUFFER_SIZE = 3
        End Enum

        <Flags()>
        Public Enum FT_EVENTS As UInteger
            FT_EVENT_RXCHAR = 1 'событие установится, когда устройство получит символ
            FT_EVENT_MODEM_STATUS = 2 'событие установится, когда устройство обнаружит изменение в сигналах модема
            FT_EVENT_LINE_STATUS = 4 'событие установится, когда устройство обнаружит изменение в статусе линии
        End Enum

        Public Enum FT_FLAGS As Byte
            FT_FLAGS_OPENED = 1
            FT_FLAGS_HISPEED = 2
        End Enum

        Public Enum FT_FLOW_CONTROL As UInt16
            FT_FLOW_NONE = 0
            FT_FLOW_RTS_CTS = &H100
            FT_FLOW_DTR_DSR = &H200
            FT_FLOW_XON_XOFF = &H400
        End Enum

        <Flags()>
        Public Enum FT_LINE_STATUS As Byte
            FT_OE = &H2
            FT_PE = &H4
            FT_FE = &H8
            FT_BI = &H10
        End Enum

        Public Enum FT_MODEM_STATUS As Byte
            FT_CTS = &H10
            FT_DSR = &H20
            FT_RI = &H40
            FT_DCD = &H80
        End Enum

        Public Enum FT_PARITY As Byte
            FT_PARITY_NONE = 0
            FT_PARITY_ODD = 1
            FT_PARITY_EVEN = 2
            FT_PARITY_MARK = 3
            FT_PARITY_SPACE = 4
        End Enum

        <Flags()>
        Public Enum FT_PURGE As Byte
            FT_PURGE_RX = 1
            FT_PURGE_TX = 2
        End Enum

        Public Enum FT_STATUS
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
            FT_INVALID_ARGS = &H10
            FT_OTHER_ERROR = &H11
        End Enum

        Public Enum FT_STOP_BITS As Byte
            FT_STOP_BITS_1 = 0
            FT_STOP_BITS_2 = 2
        End Enum

        Public Enum FT_XSERIES_CBUS_OPTIONS As Byte
            FT_CBUS_TRISTATE = 0
            FT_CBUS_RXLED = 1
            FT_CBUS_TXLED = 2
            FT_CBUS_TXRXLED = 3
            FT_CBUS_PWREN = 4
            FT_CBUS_SLEEP = 5
            FT_CBUS_Drive_0 = 6
            FT_CBUS_Drive_1 = 7
            FT_CBUS_GPIO = 8
            FT_CBUS_TXDEN = 9
            FT_CBUS_CLK24MHz = 10
            FT_CBUS_CLK12MHz = 11
            FT_CBUS_CLK6MHz = 12
            FT_CBUS_BCD_Charger = 13
            FT_CBUS_BCD_Charger_N = 14
            FT_CBUS_I2C_TXE = 15
            FT_CBUS_Time_Stamp = 20
            FT_CBUS_I2C_RXF = &H10
            FT_CBUS_VBUS_Sense = &H11
            FT_CBUS_BitBang_WR = &H12
            FT_CBUS_BitBang_RD = &H13
            FT_CBUS_Keep_Awake = &H15
        End Enum

        Public Enum FT_BAUD_RATE As UInteger
            FT_BAUD_300 = 300
            FT_BAUD_600 = 600
            FT_BAUD_1200 = 1200
            FT_BAUD_2400 = 2400
            FT_BAUD_4800 = 4800
            FT_BAUD_9600 = 9600
            FT_BAUD_14400 = 14400
            FT_BAUD_19200 = 19200
            FT_BAUD_38400 = 38400
            FT_BAUD_57600 = 57600
            FT_BAUD_115200 = 115200
            FT_BAUD_230400 = 230400
            FT_BAUD_460800 = 460800
            FT_BAUD_921600 = 921600
        End Enum

        Public Enum BAD_COMMAND As Byte
            COMMAND_AA = &HAA
            COMMAND_AB = &HAB
        End Enum

#End Region '/ПЕРЕЧИСЛЕНИЯ

#Region "СТРУКТУРЫ"

        Public Structure FT_DEVICE_INFO_NODE
            Public Description As String
            Public Flags As UInt32
            Public ftHandle As IntPtr
            Public ID As UInt32
            Public LocId As UInt32
            Public SerialNumber As String
            Public Type As FT_DEVICE
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=4)>
        Private Structure FT_EEPROM_HEADER
            Public deviceType As UInt32
            Public VendorId As UInt16
            Public ProductId As UInt16
            Public SerNumEnable As Byte
            Public MaxPower As UInt16
            Public SelfPowered As Byte
            Public RemoteWakeup As Byte
            Public PullDownEnable As Byte
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=4)>
        Private Structure FT_XSERIES_DATA
            Public common As FT_EEPROM_HEADER
            Public ACSlowSlew As Byte
            Public ACSchmittInput As Byte
            Public ACDriveCurrent As Byte
            Public ADSlowSlew As Byte
            Public ADSchmittInput As Byte
            Public ADDriveCurrent As Byte
            Public Cbus0 As Byte
            Public Cbus1 As Byte
            Public Cbus2 As Byte
            Public Cbus3 As Byte
            Public Cbus4 As Byte
            Public Cbus5 As Byte
            Public Cbus6 As Byte
            Public InvertTXD As Byte
            Public InvertRXD As Byte
            Public InvertRTS As Byte
            Public InvertCTS As Byte
            Public InvertDTR As Byte
            Public InvertDSR As Byte
            Public InvertDCD As Byte
            Public InvertRI As Byte
            Public BCDEnable As Byte
            Public BCDForceCbusPWREN As Byte
            Public BCDDisableSleep As Byte
            Public I2CSlaveAddress As UInt16
            Public I2CDeviceId As UInt32
            Public I2CDisableSchmitt As Byte
            Public FT1248Cpol As Byte
            Public FT1248Lsb As Byte
            Public FT1248FlowControl As Byte
            Public RS485EchoSuppress As Byte
            Public PowerSaveEnable As Byte
            Public DriverType As Byte
        End Structure

#End Region '/СТРУКТУРЫ

#Region "КЛАССЫ"

        <Serializable>
        Public Class FT_EXCEPTION
            Inherits Exception

            Public Sub New()
            End Sub

            Public Sub New(ByVal message As String)
                MyBase.New(message)
            End Sub

            Protected Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)
                MyBase.New(info, context)
            End Sub

            Public Sub New(ByVal message As String, ByVal inner As Exception)
                MyBase.New(message, inner)
            End Sub

        End Class

        <StructLayout(LayoutKind.Sequential, Pack:=4)>
        Private Class FT_PROGRAM_DATA
            Public Signature1 As UInt32          'Header - must be 0x0000000
            Public Signature2 As UInt32          'Header - must be 0xffffffff
            Public Version As UInt32             'Header - FT_PROGRAM_DATA version: 0 = original (FT232B), 1 = FT2232 extensions, 2 = FT232R extensions,  3 = FT2232H extensions, 4 = FT4232H extensions, 5 = FT232H extensions
            Public VendorID As UInt16            '0x0403
            Public ProductID As UInt16           '0x6001
            Public Manufacturer As IntPtr        '"FTDI"
            Public ManufacturerID As IntPtr      '"FT"
            Public Description As IntPtr         '"USB HS Serial Converter"
            Public SerialNumber As IntPtr        '"FT000001" if fixed, or NULL
            Public MaxPower As UInt16            '0 < MaxPower <= 500
            Public PnP As UInt16                 '0 = disabled, 1 = enabled
            Public SelfPowered As UInt16         '0 = bus powered, 1 = self powered
            Public RemoteWakeup As UInt16        '0 = not capable, 1 = capable
            'Rev4 (FT232B) extensions:
            Public Rev4 As Byte                  'non-zero if Rev4 chip, zero otherwise
            Public IsoIn As Byte                 'non-zero if in endpoint is isochronous
            Public IsoOut As Byte                'non-zero if out endpoint is isochronous
            Public PullDownEnable As Byte        'non-zero if pull down enabled
            Public SerNumEnable As Byte          'non-zero if serial number to be used
            Public USBVersionEnable As Byte      'non-zero if chip uses USBVersion
            Public USBVersion As UInt16          'BCD (0x0200 => USB2)
            'Rev 5 (FT2232) extensions:
            Public Rev5 As Byte                  'non-zero if Rev5 chip, zero otherwise
            Public IsoInA As Byte                'non-zero if in endpoint is isochronous
            Public IsoInB As Byte                'non-zero if in endpoint is isochronous
            Public IsoOutA As Byte               'non-zero if out endpoint is isochronous
            Public IsoOutB As Byte               'non-zero if out endpoint is isochronous
            Public PullDownEnable5 As Byte       'non-zero if pull down enabled
            Public SerNumEnable5 As Byte         'non-zero if serial number to be used
            Public USBVersionEnable5 As Byte     'non-zero if chip uses USBVersion
            Public USBVersion5 As UInt16         'BCD (0x0200 => USB2)
            Public AIsHighCurrent As Byte        'non-zero if interface is high current
            Public BIsHighCurrent As Byte        'non-zero if interface is high current
            Public IFAIsFifo As Byte             'non-zero if interface is 245 FIFO
            Public IFAIsFifoTar As Byte          'non-zero if interface is 245 FIFO CPU target
            Public IFAIsFastSer As Byte          'non-zero if interface is Fast serial
            Public AIsVCP As Byte                'non-zero if interface is to use VCP drivers
            Public IFBIsFifo As Byte             'non-zero if interface is 245 FIFO
            Public IFBIsFifoTar As Byte          'non-zero if interface is 245 FIFO CPU target
            Public IFBIsFastSer As Byte          'non-zero if interface is Fast serial
            Public BIsVCP As Byte                'non-zero if interface is to use VCP drivers
            'Rev 6 (FT232R) extensions:
            Public UseExtOsc As Byte             'Use External Oscillator
            Public HighDriveIOs As Byte          'High Drive I/Os
            Public EndpointSize As Byte          'Endpoint size
            Public PullDownEnableR As Byte       'non-zero if pull down enabled
            Public SerNumEnableR As Byte         'non-zero if serial number to be used
            Public InvertTXD As Byte             'non-zero if invert TXD
            Public InvertRXD As Byte             'non-zero if invert RXD
            Public InvertRTS As Byte             'non-zero if invert RTS
            Public InvertCTS As Byte             'non-zero if invert CTS
            Public InvertDTR As Byte             'non-zero if invert DTR
            Public InvertDSR As Byte             'non-zero if invert DSR
            Public InvertDCD As Byte             'non-zero if invert DCD
            Public InvertRI As Byte              'non-zero if invert RI
            Public Cbus0 As Byte                 'Cbus Mux control
            Public Cbus1 As Byte                 'Cbus Mux control
            Public Cbus2 As Byte                 'Cbus Mux control
            Public Cbus3 As Byte                 'Cbus Mux control
            Public Cbus4 As Byte                 'Cbus Mux control
            Public RIsD2XX As Byte               'non-zero if using D2XX driver
            'Rev 7 (FT2232H) Extensions:
            Public PullDownEnable7 As Byte       'non-zero if pull down enabled
            Public SerNumEnable7 As Byte         'non-zero if serial number to be used
            Public ALSlowSlew As Byte            'non-zero if AL pins have slow slew
            Public ALSchmittInput As Byte        'non-zero if AL pins are Schmitt input
            Public ALDriveCurrent As Byte        'valid values are 4mA, 8mA, 12mA, 16mA
            Public AHSlowSlew As Byte            'non-zero if AH pins have slow slew
            Public AHSchmittInput As Byte        'non-zero if AH pins are Schmitt input
            Public AHDriveCurrent As Byte        'valid values are 4mA, 8mA, 12mA, 16mA
            Public BLSlowSlew As Byte            'non-zero if BL pins have slow slew
            Public BLSchmittInput As Byte        'non-zero if BL pins are Schmitt input
            Public BLDriveCurrent As Byte        'valid values are 4mA, 8mA, 12mA, 16mA
            Public BHSlowSlew As Byte            'non-zero if BH pins have slow slew
            Public BHSchmittInput As Byte        'non-zero if BH pins are Schmitt input
            Public BHDriveCurrent As Byte        'valid values are 4mA, 8mA, 12mA, 16mA
            Public IFAIsFifo7 As Byte            'non-zero if interface is 245 FIFO
            Public IFAIsFifoTar7 As Byte         'non-zero if interface is 245 FIFO CPU target
            Public IFAIsFastSer7 As Byte         'non-zero if interface is Fast serial
            Public AIsVCP7 As Byte               'non-zero if interface is to use VCP drivers
            Public IFBIsFifo7 As Byte            'non-zero if interface is 245 FIFO
            Public IFBIsFifoTar7 As Byte         'non-zero if interface is 245 FIFO CPU target
            Public IFBIsFastSer7 As Byte         'non-zero if interface is Fast serial
            Public BIsVCP7 As Byte               'non-zero if interface is to use VCP drivers
            Public PowerSaveEnable As Byte       'non-zero if using BCBUS7 to save power for self-powered designs
            'Rev 8 (FT4232H) Extensions:
            Public PullDownEnable8 As Byte       'non-zero if pull down enabled
            Public SerNumEnable8 As Byte         'non-zero if serial number to be used
            Public ASlowSlew As Byte             'non-zero if A pins have slow slew
            Public ASchmittInput As Byte         'non-zero if A pins are Schmitt input
            Public ADriveCurrent As Byte         'valid values are 4mA, 8mA, 12mA, 16mA
            Public BSlowSlew As Byte             'non-zero if B pins have slow slew
            Public BSchmittInput As Byte         'non-zero if B pins are Schmitt input
            Public BDriveCurrent As Byte         'valid values are 4mA, 8mA, 12mA, 16mA
            Public CSlowSlew As Byte             'non-zero if C pins have slow slew
            Public CSchmittInput As Byte         'non-zero if C pins are Schmitt input
            Public CDriveCurrent As Byte         'valid values are 4mA, 8mA, 12mA, 16mA
            Public DSlowSlew As Byte             'non-zero if D pins have slow slew
            Public DSchmittInput As Byte         'non-zero if D pins are Schmitt input
            Public DDriveCurrent As Byte         'valid values are 4mA, 8mA, 12mA, 16mA
            Public ARIIsTXDEN As Byte            'non-zero if port A uses RI as RS485 TXDEN
            Public BRIIsTXDEN As Byte            'non-zero if port B uses RI as RS485 TXDEN
            Public CRIIsTXDEN As Byte            'non-zero if port C uses RI as RS485 TXDEN
            Public DRIIsTXDEN As Byte            'non-zero if port D uses RI as RS485 TXDEN
            Public AIsVCP8 As Byte               'non-zero if interface is to use VCP drivers
            Public BIsVCP8 As Byte               'non-zero if interface is to use VCP drivers
            Public CIsVCP8 As Byte               'non-zero if interface is to use VCP drivers
            Public DIsVCP8 As Byte               'non-zero if interface is to use VCP drivers
            'Rev 9 (FT232H) Extensions:
            Public PullDownEnableH As Byte      'non-zero if pull down enabled
            Public SerNumEnableH As Byte        'non-zero if serial number to be used
            Public ACSlowSlewH As Byte          'non-zero if AC pins have slow slew
            Public ACSchmittInputH As Byte      'non-zero if AC pins are Schmitt input
            Public ACDriveCurrentH As Byte      'valid values are 4mA, 8mA, 12mA, 16mA
            Public ADSlowSlewH As Byte          'non-zero if AD pins have slow slew
            Public ADSchmittInputH As Byte      'non-zero if AD pins are Schmitt input
            Public ADDriveCurrentH As Byte      'valid values are 4mA, 8mA, 12mA, 16mA
            Public Cbus0H As Byte               'Cbus Mux control
            Public Cbus1H As Byte               'Cbus Mux control
            Public Cbus2H As Byte               'Cbus Mux control
            Public Cbus3H As Byte               'Cbus Mux control
            Public Cbus4H As Byte               'Cbus Mux control
            Public Cbus5H As Byte               'Cbus Mux control
            Public Cbus6H As Byte               'Cbus Mux control
            Public Cbus7H As Byte               'Cbus Mux control
            Public Cbus8H As Byte               'Cbus Mux control
            Public Cbus9H As Byte               'Cbus Mux control
            Public IsFifoH As Byte              'non-zero if interface is 245 FIFO
            Public IsFifoTarH As Byte           'non-zero if interface is 245 FIFO CPU target
            Public IsFastSerH As Byte           'non-zero if interface is Fast serial
            Public IsFT1248H As Byte            'non-zero if interface is FT1248
            Public FT1248CpolH As Byte          'FT1248 clock polarity - clock idle high (1) or clock idle low (0)
            Public FT1248LsbH As Byte           'FT1248 data is LSB (1) or MSB (0)
            Public FT1248FlowControlH As Byte   'FT1248 flow control enable
            Public IsVCPH As Byte               'non-zero if interface is to use VCP drivers
            Public PowerSaveEnableH As Byte     'non-zero if using ACBUS7 to save power for self-powered designs
        End Class '/FT_PROGRAM_DATA

        ''' <summary>
        ''' Базовый класс для структур данных в ПЗУ.
        ''' </summary>
        Public Class FT_EEPROM_DATA
            Public Description As String = "USB-Serial Converter"
            Public Manufacturer As String = "FTDI"
            Public ManufacturerID As String = "FT"
            Public MaxPower As UInt16 = FT_DEFAULT_POWER
            Public ProductID As UInt16 = FT_DEFAULT_PID
            Public RemoteWakeup As Boolean = False
            Public SelfPowered As Boolean = False
            Public SerialNumber As String = ""
            Public VendorID As UInt16 = FT_DEFAULT_VID
        End Class

        Public Class FT_XSERIES_EEPROM_STRUCTURE
            Inherits FT_EEPROM_DATA

            Public ACDriveCurrent As Byte
            Public ACSchmittInput As Byte
            Public ACSlowSlew As Byte
            Public ADDriveCurrent As Byte
            Public ADSchmittInput As Byte
            Public ADSlowSlew As Byte
            Public BCDDisableSleep As Byte
            Public BCDEnable As Byte
            Public BCDForceCbusPWREN As Byte
            Public Cbus0 As Byte
            Public Cbus1 As Byte
            Public Cbus2 As Byte
            Public Cbus3 As Byte
            Public Cbus4 As Byte
            Public Cbus5 As Byte
            Public Cbus6 As Byte
            Public FT1248Cpol As Byte
            Public FT1248FlowControl As Byte
            Public FT1248Lsb As Byte
            Public I2CDeviceId As UInt32
            Public I2CDisableSchmitt As Byte
            Public I2CSlaveAddress As UInt16
            Public InvertCTS As Byte
            Public InvertDCD As Byte
            Public InvertDSR As Byte
            Public InvertDTR As Byte
            Public InvertRI As Byte
            Public InvertRTS As Byte
            Public InvertRXD As Byte
            Public InvertTXD As Byte
            Public IsVCP As Byte
            Public PowerSaveEnable As Byte
            Public PullDownEnable As Boolean = False
            Public RS485EchoSuppress As Byte
            Public SerNumEnable As Boolean = True
            Public USBVersion As UInt16 = FT_DEFAULT_USBVER
            Public USBVersionEnable As Boolean = True
        End Class

        Public Class FT2232_EEPROM_STRUCTURE
            Inherits FT_EEPROM_DATA

            Public AIsHighCurrent As Boolean = False
            Public AIsVCP As Boolean = True
            Public BIsHighCurrent As Boolean = False
            Public BIsVCP As Boolean = True
            Public IFAIsFastSer As Boolean = False
            Public IFAIsFifo As Boolean = False
            Public IFAIsFifoTar As Boolean = False
            Public IFBIsFastSer As Boolean = False
            Public IFBIsFifo As Boolean = False
            Public IFBIsFifoTar As Boolean = False
            Public PullDownEnable As Boolean = False
            Public SerNumEnable As Boolean = True
            Public USBVersion As UInt16 = FT_DEFAULT_USBVER
            Public USBVersionEnable As Boolean = True
        End Class

        Public Class FT2232H_EEPROM_STRUCTURE
            Inherits FT_EEPROM_DATA

            Public AHDriveCurrent As Byte = 4
            Public AHSchmittInput As Boolean = False
            Public AHSlowSlew As Boolean = False
            Public AIsVCP As Boolean = True
            Public ALDriveCurrent As Byte = 4
            Public ALSchmittInput As Boolean = False
            Public ALSlowSlew As Boolean = False
            Public BHDriveCurrent As Byte = 4
            Public BHSchmittInput As Boolean = False
            Public BHSlowSlew As Boolean = False
            Public BIsVCP As Boolean = True
            Public BLDriveCurrent As Byte = 4
            Public BLSchmittInput As Boolean = False
            Public BLSlowSlew As Boolean = False
            Public IFAIsFastSer As Boolean = False
            Public IFAIsFifo As Boolean = False
            Public IFAIsFifoTar As Boolean = False
            Public IFBIsFastSer As Boolean = False
            Public IFBIsFifo As Boolean = False
            Public IFBIsFifoTar As Boolean = False
            Public PowerSaveEnable As Boolean = False
            Public PullDownEnable As Boolean = False
            Public SerNumEnable As Boolean = True
        End Class

        Public Class FT232B_EEPROM_STRUCTURE
            Inherits FT_EEPROM_DATA

            Public PullDownEnable As Boolean = False
            Public SerNumEnable As Boolean = True
            Public USBVersion As UInt16 = FT_DEFAULT_USBVER
            Public USBVersionEnable As Boolean = True
        End Class

        Public Class FT232H_EEPROM_STRUCTURE
            Inherits FT_EEPROM_DATA

            Public ACDriveCurrent As Byte = 4
            Public ACSchmittInput As Boolean = False
            Public ACSlowSlew As Boolean = False
            Public ADDriveCurrent As Byte = 4
            Public ADSchmittInput As Boolean = False
            Public ADSlowSlew As Boolean = False
            Public Cbus0 As Byte = 0
            Public Cbus1 As Byte = 0
            Public Cbus2 As Byte = 0
            Public Cbus3 As Byte = 0
            Public Cbus4 As Byte = 0
            Public Cbus5 As Byte = 0
            Public Cbus6 As Byte = 0
            Public Cbus7 As Byte = 0
            Public Cbus8 As Byte = 0
            Public Cbus9 As Byte = 0
            Public FT1248Cpol As Boolean = False
            Public FT1248FlowControl As Boolean = False
            Public FT1248Lsb As Boolean = False
            Public IsFastSer As Boolean = False
            Public IsFifo As Boolean = False
            Public IsFifoTar As Boolean = False
            Public IsFT1248 As Boolean = False
            Public IsVCP As Boolean = True
            Public PowerSaveEnable As Boolean = False
            Public PullDownEnable As Boolean = False
            Public SerNumEnable As Boolean = True
        End Class

        Public Class FT232R_EEPROM_STRUCTURE
            Inherits FT_EEPROM_DATA

            Public Cbus0 As Byte = 5
            Public Cbus1 As Byte = 5
            Public Cbus2 As Byte = 5
            Public Cbus3 As Byte = 5
            Public Cbus4 As Byte = 5
            Public EndpointSize As Byte = &H40
            Public HighDriveIOs As Boolean = False
            Public InvertCTS As Boolean = False
            Public InvertDCD As Boolean = False
            Public InvertDSR As Boolean = False
            Public InvertDTR As Boolean = False
            Public InvertRI As Boolean = False
            Public InvertRTS As Boolean = False
            Public InvertRXD As Boolean = False
            Public InvertTXD As Boolean = False
            Public PullDownEnable As Boolean = False
            Public RIsD2XX As Boolean = False
            Public SerNumEnable As Boolean = True
            Public UseExtOsc As Boolean = False
        End Class

        Public Class FT4232H_EEPROM_STRUCTURE
            Inherits FT_EEPROM_DATA

            Public ADriveCurrent As Byte = 4
            Public AIsVCP As Boolean = True
            Public ARIIsTXDEN As Boolean = False
            Public ASchmittInput As Boolean = False
            Public ASlowSlew As Boolean = False
            Public BDriveCurrent As Byte = 4
            Public BIsVCP As Boolean = True
            Public BRIIsTXDEN As Boolean = False
            Public BSchmittInput As Boolean = False
            Public BSlowSlew As Boolean = False
            Public CDriveCurrent As Byte = 4
            Public CIsVCP As Boolean = True
            Public CRIIsTXDEN As Boolean = False
            Public CSchmittInput As Boolean = False
            Public CSlowSlew As Boolean = False
            Public DDriveCurrent As Byte = 4
            Public DIsVCP As Boolean = True
            Public DRIIsTXDEN As Boolean = False
            Public DSchmittInput As Boolean = False
            Public DSlowSlew As Boolean = False
            Public PullDownEnable As Boolean = False
            Public SerNumEnable As Boolean = True
        End Class

#End Region '/КЛАССЫ

#Region "КОНСТАНТЫ"

        Public Const FTD2XX_DLL_PATH As String = "c:\temp\FTD2XX.DLL"
        Public Const FT_DEFAULT_VID As Integer = &H403
        Public Const FT_DEFAULT_PID As Integer = &H6001
        Public Const FT_DEFAULT_LATENCY As Byte = &H10
        Public Const USB_DEFAULT_IN_TRANSFER_SIZE As Long = &H1000
        Public Const USB_DEFAULT_OUT_TRANSFER_SIZE As Long = &H1000

        ''' <summary>
        ''' Частота тактового генератора для низкоскоростных устройств и высокоскоростных устройств при включённом делителе на 5.
        ''' </summary>
        Public Const LOW_CLOCK As Integer = 12000000

        ''' <summary>
        ''' Частота тактового генератора высокоскоростных устройств при выключенном делителе на 5.
        ''' </summary>
        Public Const FAST_CLOCK As Integer = 60000000

        Private Const DELAY_AFTER_WRITE As Integer = 2
        Private Const FT_DEFAULT_POWER As UInt16 = &H90
        Private Const FT_DEFAULT_USBVER As UInt16 = &H200
        Private Const FT_OPEN_BY_SERIAL_NUMBER As UInt32 = 1
        Private Const FT_OPEN_BY_DESCRIPTION As UInt32 = 2
        Private Const FT_OPEN_BY_LOCATION As UInt32 = 4
        Private Const FT_COM_PORT_NOT_ASSIGNED As Integer = -1
        Private Const FT_DEFAULT_BAUD_RATE As UInt32 = &H2580
        Private Const FT_DEFAULT_DEADMAN_TIMEOUT As UInt32 = &H1388
        Private Const FT_DEFAULT_DEVICE_ID As UInt32 = &H4036001
        Private Const FT_DEFAULT_XON As Byte = &H11
        Private Const FT_DEFAULT_XOFF As Byte = &H13
        Private Const USB_DEFAULT_OUT_SIZE_MIN As Integer = 64
        Private Const USB_DEFAULT_OUT_SIZE_MAX As Integer = &H10000
        Private Const USB_DEFAULT_IN_SIZE_MIN As Integer = 64
        Private Const USB_DEFAULT_IN_SIZE_MAX As Integer = &H10000

#End Region '/КОНСТАНТЫ

#Region "ЗАКРЫТЫЕ ПОЛЯ"

        ''' <summary>
        ''' Дескриптор библиотеки, получаемый при инициализации библиотеки.
        ''' </summary>
        Private Shared hFTD2XXDLL As IntPtr = IntPtr.Zero
        Private Shared pFT_GetLibraryVersion As IntPtr = IntPtr.Zero
        Private Shared pFT_CreateDeviceInfoList As IntPtr = IntPtr.Zero
        Private Shared pFT_GetDeviceInfoDetail As IntPtr = IntPtr.Zero
        Private Shared pFT_Reload As IntPtr = IntPtr.Zero
        Private Shared pFT_Rescan As IntPtr = IntPtr.Zero

        ''' <summary>
        ''' Дескриптор устройства, получаемый при открытии устройства.
        ''' </summary>
        Private ftHandle As IntPtr = IntPtr.Zero
        Private pFT_Close As IntPtr = IntPtr.Zero
        Private pFT_ClrDtr As IntPtr = IntPtr.Zero
        Private pFT_ClrRts As IntPtr = IntPtr.Zero
        Private pFT_CyclePort As IntPtr = IntPtr.Zero
        Private pFT_EE_Program As IntPtr = IntPtr.Zero
        Private pFT_EE_Read As IntPtr = IntPtr.Zero
        Private pFT_EE_UARead As IntPtr = IntPtr.Zero
        Private pFT_EE_UASize As IntPtr = IntPtr.Zero
        Private pFT_EE_UAWrite As IntPtr = IntPtr.Zero
        Private pFT_EEPROM_Program As IntPtr = IntPtr.Zero
        Private pFT_EEPROM_Read As IntPtr = IntPtr.Zero
        Private pFT_EraseEE As IntPtr = IntPtr.Zero
        Private pFT_GetBitMode As IntPtr = IntPtr.Zero
        Private pFT_GetComPortNumber As IntPtr = IntPtr.Zero
        Private pFT_GetDeviceInfo As IntPtr = IntPtr.Zero
        Private pFT_GetDriverVersion As IntPtr = IntPtr.Zero
        Private pFT_GetLatencyTimer As IntPtr = IntPtr.Zero
        Private pFT_GetModemStatus As IntPtr = IntPtr.Zero
        Private pFT_GetQueueStatus As IntPtr = IntPtr.Zero
        Private pFT_GetStatus As IntPtr = IntPtr.Zero
        Private pFT_Open As IntPtr = IntPtr.Zero
        Private pFT_OpenEx As IntPtr = IntPtr.Zero
        Private pFT_Purge As IntPtr = IntPtr.Zero
        Private pFT_Read As IntPtr = IntPtr.Zero
        Private pFT_ReadEE As IntPtr = IntPtr.Zero
        Private pFT_ResetDevice As IntPtr = IntPtr.Zero
        Private pFT_ResetPort As IntPtr = IntPtr.Zero
        Private pFT_RestartInTask As IntPtr = IntPtr.Zero
        Private pFT_SetBaudRate As IntPtr = IntPtr.Zero
        Private pFT_SetBitMode As IntPtr = IntPtr.Zero
        Private pFT_SetBreakOff As IntPtr = IntPtr.Zero
        Private pFT_SetBreakOn As IntPtr = IntPtr.Zero
        Private pFT_SetChars As IntPtr = IntPtr.Zero
        Private pFT_SetDataCharacteristics As IntPtr = IntPtr.Zero
        Private pFT_SetDeadmanTimeout As IntPtr = IntPtr.Zero
        Private pFT_SetDtr As IntPtr = IntPtr.Zero
        Private pFT_SetEventNotification As IntPtr = IntPtr.Zero
        Private pFT_SetFlowControl As IntPtr = IntPtr.Zero
        Private pFT_SetLatencyTimer As IntPtr = IntPtr.Zero
        Private pFT_SetResetPipeRetryCount As IntPtr
        Private pFT_SetRts As IntPtr = IntPtr.Zero
        Private pFT_SetTimeouts As IntPtr = IntPtr.Zero
        Private pFT_SetUSBParameters As IntPtr = IntPtr.Zero
        Private pFT_StopInTask As IntPtr = IntPtr.Zero
        Private pFT_VendorCmdGet As IntPtr = IntPtr.Zero
        Private pFT_VendorCmdSet As IntPtr = IntPtr.Zero
        Private pFT_Write As IntPtr = IntPtr.Zero
        Private pFT_WriteEE As IntPtr = IntPtr.Zero

#End Region '/ЗАКРЫТЫЕ ПОЛЯ

#End Region '/ВСПОМОГАТЕЛЬНЫЕ ТИПЫ ДАННЫХ

#Region "ВЫЗОВ НАТИВНОГО КОДА"

        <DllImport("kernel32.dll")>
        Private Shared Function LoadLibrary(ByVal dllToLoad As String) As IntPtr
        End Function

        <DllImport("kernel32.dll")>
        Private Shared Function FreeLibrary(ByVal hModule As IntPtr) As Boolean
        End Function

        <DllImport("kernel32.dll")>
        Private Shared Function GetProcAddress(ByVal hModule As IntPtr, ByVal procedureName As String) As IntPtr
        End Function

        ''' <summary>
        ''' Получает указатели нативных функций.
        ''' </summary>
        Private Sub FindFunctionPointers()
            pFT_CreateDeviceInfoList = GetProcAddress(hFTD2XXDLL, "FT_CreateDeviceInfoList")
            pFT_GetDeviceInfoDetail = GetProcAddress(hFTD2XXDLL, "FT_GetDeviceInfoDetail")
            pFT_Open = GetProcAddress(hFTD2XXDLL, "FT_Open")
            pFT_OpenEx = GetProcAddress(hFTD2XXDLL, "FT_OpenEx")
            pFT_Close = GetProcAddress(hFTD2XXDLL, "FT_Close")
            pFT_Read = GetProcAddress(hFTD2XXDLL, "FT_Read")
            pFT_Write = GetProcAddress(hFTD2XXDLL, "FT_Write")
            pFT_GetQueueStatus = GetProcAddress(hFTD2XXDLL, "FT_GetQueueStatus")
            pFT_GetModemStatus = GetProcAddress(hFTD2XXDLL, "FT_GetModemStatus")
            pFT_GetStatus = GetProcAddress(hFTD2XXDLL, "FT_GetStatus")
            pFT_SetBaudRate = GetProcAddress(hFTD2XXDLL, "FT_SetBaudRate")
            pFT_SetDataCharacteristics = GetProcAddress(hFTD2XXDLL, "FT_SetDataCharacteristics")
            pFT_SetFlowControl = GetProcAddress(hFTD2XXDLL, "FT_SetFlowControl")
            pFT_SetDtr = GetProcAddress(hFTD2XXDLL, "FT_SetDtr")
            pFT_ClrDtr = GetProcAddress(hFTD2XXDLL, "FT_ClrDtr")
            pFT_SetRts = GetProcAddress(hFTD2XXDLL, "FT_SetRts")
            pFT_ClrRts = GetProcAddress(hFTD2XXDLL, "FT_ClrRts")
            pFT_ResetDevice = GetProcAddress(hFTD2XXDLL, "FT_ResetDevice")
            pFT_ResetPort = GetProcAddress(hFTD2XXDLL, "FT_ResetPort")
            pFT_CyclePort = GetProcAddress(hFTD2XXDLL, "FT_CyclePort")
            pFT_Rescan = GetProcAddress(hFTD2XXDLL, "FT_Rescan")
            pFT_Reload = GetProcAddress(hFTD2XXDLL, "FT_Reload")
            pFT_Purge = GetProcAddress(hFTD2XXDLL, "FT_Purge")
            pFT_SetTimeouts = GetProcAddress(hFTD2XXDLL, "FT_SetTimeouts")
            pFT_SetBreakOn = GetProcAddress(hFTD2XXDLL, "FT_SetBreakOn")
            pFT_SetBreakOff = GetProcAddress(hFTD2XXDLL, "FT_SetBreakOff")
            pFT_GetDeviceInfo = GetProcAddress(hFTD2XXDLL, "FT_GetDeviceInfo")
            pFT_SetResetPipeRetryCount = GetProcAddress(hFTD2XXDLL, "FT_SetResetPipeRetryCount")
            pFT_StopInTask = GetProcAddress(hFTD2XXDLL, "FT_StopInTask")
            pFT_RestartInTask = GetProcAddress(hFTD2XXDLL, "FT_RestartInTask")
            pFT_GetDriverVersion = GetProcAddress(hFTD2XXDLL, "FT_GetDriverVersion")
            pFT_GetLibraryVersion = GetProcAddress(hFTD2XXDLL, "FT_GetLibraryVersion")
            pFT_SetDeadmanTimeout = GetProcAddress(hFTD2XXDLL, "FT_SetDeadmanTimeout")
            pFT_SetChars = GetProcAddress(hFTD2XXDLL, "FT_SetChars")
            pFT_SetEventNotification = GetProcAddress(hFTD2XXDLL, "FT_SetEventNotification")
            pFT_GetComPortNumber = GetProcAddress(hFTD2XXDLL, "FT_GetComPortNumber")
            pFT_SetLatencyTimer = GetProcAddress(hFTD2XXDLL, "FT_SetLatencyTimer")
            pFT_GetLatencyTimer = GetProcAddress(hFTD2XXDLL, "FT_GetLatencyTimer")
            pFT_SetBitMode = GetProcAddress(hFTD2XXDLL, "FT_SetBitMode")
            pFT_GetBitMode = GetProcAddress(hFTD2XXDLL, "FT_GetBitMode")
            pFT_SetUSBParameters = GetProcAddress(hFTD2XXDLL, "FT_SetUSBParameters")
            pFT_ReadEE = GetProcAddress(hFTD2XXDLL, "FT_ReadEE")
            pFT_WriteEE = GetProcAddress(hFTD2XXDLL, "FT_WriteEE")
            pFT_EraseEE = GetProcAddress(hFTD2XXDLL, "FT_EraseEE")
            pFT_EE_UASize = GetProcAddress(hFTD2XXDLL, "FT_EE_UASize")
            pFT_EE_UARead = GetProcAddress(hFTD2XXDLL, "FT_EE_UARead")
            pFT_EE_UAWrite = GetProcAddress(hFTD2XXDLL, "FT_EE_UAWrite")
            pFT_EE_Read = GetProcAddress(hFTD2XXDLL, "FT_EE_Read")
            pFT_EE_Program = GetProcAddress(hFTD2XXDLL, "FT_EE_Program")
            pFT_EEPROM_Read = GetProcAddress(hFTD2XXDLL, "FT_EEPROM_Read")
            pFT_EEPROM_Program = GetProcAddress(hFTD2XXDLL, "FT_EEPROM_Program")
            pFT_VendorCmdGet = GetProcAddress(hFTD2XXDLL, "FT_VendorCmdGet")
            pFT_VendorCmdSet = GetProcAddress(hFTD2XXDLL, "FT_VendorCmdSet")
        End Sub

#Region "ДЕЛЕГАТЫ НАТИВНЫХ МЕТОДОВ"

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_Close(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_ClrDtr(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_ClrRts(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_CreateDeviceInfoList(ByRef numdevs As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_CyclePort(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_EE_Program(ByVal ftHandle As IntPtr, ByVal pData As FT_PROGRAM_DATA) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_EE_Read(ByVal ftHandle As IntPtr, ByVal pData As FT_PROGRAM_DATA) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_EE_UARead(ByVal ftHandle As IntPtr, ByVal pucData As Byte(), ByVal dwDataLen As Integer, ByRef lpdwDataRead As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_EE_UASize(ByVal ftHandle As IntPtr, ByRef dwSize As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_EE_UAWrite(ByVal ftHandle As IntPtr, ByVal pucData As Byte(), ByVal dwDataLen As Integer) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_EEPROM_Program(ByVal ftHandle As IntPtr, ByVal eepromData As IntPtr, ByVal eepromDataSize As UInt32, ByVal manufacturer As Byte(), ByVal manufacturerID As Byte(), ByVal description As Byte(), ByVal serialnumber As Byte()) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_EEPROM_Read(ByVal ftHandle As IntPtr, ByVal eepromData As IntPtr, ByVal eepromDataSize As UInt32, ByVal manufacturer As Byte(), ByVal manufacturerID As Byte(), ByVal description As Byte(), ByVal serialnumber As Byte()) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_EraseEE(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_GetBitMode(ByVal ftHandle As IntPtr, ByRef ucMode As Byte) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_GetComPortNumber(ByVal ftHandle As IntPtr, ByRef dwComPortNumber As Integer) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_GetDeviceInfo(ByVal ftHandle As IntPtr, ByRef pftType As FT_DEVICE, ByRef lpdwID As UInt32, ByVal pcSerialNumber As Byte(), ByVal pcDescription As Byte(), ByVal pvDummy As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_GetDeviceInfoDetail(ByVal index As UInt32, ByRef flags As UInt32, ByRef chiptype As FT_DEVICE, ByRef id As UInt32, ByRef locid As UInt32, ByVal serialnumber As Byte(), ByVal description As Byte(), ByRef ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_GetDriverVersion(ByVal ftHandle As IntPtr, ByRef lpdwDriverVersion As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_GetLatencyTimer(ByVal ftHandle As IntPtr, ByRef ucLatency As Byte) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_GetLibraryVersion(ByRef lpdwLibraryVersion As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_GetModemStatus(ByVal ftHandle As IntPtr, ByRef lpdwModemStatus As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_GetQueueStatus(ByVal ftHandle As IntPtr, ByRef lpdwAmountInRxQueue As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_GetStatus(ByVal ftHandle As IntPtr, ByRef lpdwAmountInRxQueue As UInt32, ByRef lpdwAmountInTxQueue As UInt32, ByRef lpdwEventStatus As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_Open(ByVal index As UInt32, ByRef ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_OpenEx(ByVal devstring As String, ByVal dwFlags As UInt32, ByRef ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_OpenExLoc(ByVal devloc As UInt32, ByVal dwFlags As UInt32, ByRef ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_Purge(ByVal ftHandle As IntPtr, ByVal dwMask As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_Read(ByVal ftHandle As IntPtr, ByVal lpBuffer As Byte(), ByVal dwBytesToRead As UInt32, ByRef lpdwBytesReturned As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_ReadEE(ByVal ftHandle As IntPtr, ByVal dwWordOffset As UInt32, ByRef lpwValue As UInt16) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_Reload(ByVal wVID As UInt16, ByVal wPID As UInt16) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_Rescan() As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_ResetDevice(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_ResetPort(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_RestartInTask(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetBaudRate(ByVal ftHandle As IntPtr, ByVal dwBaudRate As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetBitMode(ByVal ftHandle As IntPtr, ByVal ucMask As Byte, ByVal ucMode As Byte) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetBreakOff(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetBreakOn(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetChars(ByVal ftHandle As IntPtr, ByVal uEventCh As Byte, ByVal uEventChEn As Byte, ByVal uErrorCh As Byte, ByVal uErrorChEn As Byte) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetDataCharacteristics(ByVal ftHandle As IntPtr, ByVal uWordLength As Byte, ByVal uStopBits As Byte, ByVal uParity As Byte) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetDeadmanTimeout(ByVal ftHandle As IntPtr, ByVal dwDeadmanTimeout As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetDtr(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetEventNotification(ByVal ftHandle As IntPtr, ByVal dwEventMask As UInt32, ByVal hEvent As SafeHandle) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetFlowControl(ByVal ftHandle As IntPtr, ByVal usFlowControl As UInt16, ByVal uXon As Byte, ByVal uXoff As Byte) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetLatencyTimer(ByVal ftHandle As IntPtr, ByVal ucLatency As Byte) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetResetPipeRetryCount(ByVal ftHandle As IntPtr, ByVal dwCount As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetRts(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetTimeouts(ByVal ftHandle As IntPtr, ByVal dwReadTimeout As UInt32, ByVal dwWriteTimeout As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_SetUSBParameters(ByVal ftHandle As IntPtr, ByVal dwInTransferSize As UInt32, ByVal dwOutTransferSize As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_StopInTask(ByVal ftHandle As IntPtr) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_VendorCmdGet(ByVal ftHandle As IntPtr, ByVal request As UInt16, ByVal buf As Byte(), ByVal len As UInt16) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_VendorCmdSet(ByVal ftHandle As IntPtr, ByVal request As UInt16, ByVal buf As Byte(), ByVal len As UInt16) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_Write(ByVal ftHandle As IntPtr, ByVal lpBuffer As Byte(), ByVal dwBytesToWrite As UInt32, ByRef lpdwBytesWritten As UInt32) As FT_STATUS

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function tFT_WriteEE(ByVal ftHandle As IntPtr, ByVal dwWordOffset As UInt32, ByVal wValue As UInt16) As FT_STATUS

#End Region '/ДЕЛЕГАТЫ НАТИВНЫХ МЕТОДОВ

#End Region '/ВЫЗОВ НАТИВНОГО КОДА

    End Class '/FTD2XX_NET

End Namespace
