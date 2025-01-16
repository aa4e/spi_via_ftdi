Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Text

Namespace Ftdi

    ''' <summary>
    ''' Работа с устройствами FTDI.
    ''' </summary>
    ''' <remarks>
    ''' Самый общий класс-обёртка над FTD2XX.dll.
    ''' </remarks>
    Public NotInheritable Class Ftd2xx

#Region "СТРУКТУРЫ И ДАННЫЕ"

#If SOL Then
        Public Const DllFtd2xxPath As String = "ftd2xx.dll"
#Else
        Public Const DllFtd2xxPath As String = "C:\Temp\ftd2xx.dll"
#End If

        ''' <summary>
        ''' Статус устройства.
        ''' </summary>
        <StructLayout(LayoutKind.Sequential)>
        Public Structure FT_STATUS
            Public Value As Integer
        End Structure

        ''' <summary>
        ''' Дескриптор устройства.
        ''' </summary>
        <StructLayout(LayoutKind.Sequential)>
        Public Structure FT_HANDLE
            Public Value As IntPtr
        End Structure

        ''' <summary>
        ''' Информация об устройстве.
        ''' </summary>
        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi)>
        Public Structure FT_DEVICE_LIST_INFO_NODE
            Public Flags As UInteger
            Public Type As UInteger
            Public ID As UInteger
            Public LocId As Integer
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=16)> Public SerialNumber As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=64)> Public Description As String
            Public ftHandle As Integer
            Public Index As Integer 'добавил Я
        End Structure

        ''' <summary>
        ''' Device information flags.
        ''' </summary>
        Public Enum FT_FLAGS
            FT_FLAGS_OPENED = 1
            FT_FLAGS_HISPEED = 2
        End Enum

        ''' <summary>
        ''' Structure to hold program data for FT_EE_Program, FT_EE_ProgramEx, FT_EE_Read and FT_EE_ReadEx functions.
        ''' </summary>
        <StructLayout(LayoutKind.Sequential)>
        Public Structure FT_PROGRAM_DATA
            Public Signature1 As Integer  ' Header - must be 0x00000000 
            Public Signature2 As Integer  ' Header - must be 0xffffffff
            Public Version As Integer     ' Header - FT_PROGRAM_DATA version
            '  0 = original
            '  1 = FT2232 extensions
            '  2 = FT232R extensions
            '  3 = FT2232H extensions
            '  4 = FT4232H extensions
            '  5 = FT232H extensions
            Public VendorId As Short   ' 0x0403
            Public ProductId As Short  ' 0x6001
            <MarshalAs(UnmanagedType.LPStr)> Public Manufacturer As String      ' "FTDI"
            <MarshalAs(UnmanagedType.LPStr)> Public ManufacturerId As String    ' "FT"
            <MarshalAs(UnmanagedType.LPStr)> Public Description As String       ' "USB HS Serial Converter"
            <MarshalAs(UnmanagedType.LPStr)> Public SerialNumber As String      ' "FT000001" if fixed, or NULL
            Public MaxPower As Short       ' 0 < MaxPower <= 500
            Public PnP As Short            ' 0 = disabled, 1 = enabled
            Public SelfPowered As Short    ' 0 = bus powered, 1 = self powered
            Public RemoteWakeup As Short   ' 0 = not capable, 1 = capable

            ' Rev4 (FT232B) extensions
            Public Rev4 As Byte               ' non-zero if Rev4 chip, zero otherwise
            Public IsoIn As Byte              ' non-zero if in endpoint is isochronous
            Public IsoOut As Byte             ' non-zero if out endpoint is isochronous
            Public PullDownEnable As Byte     ' non-zero if pull down enabled
            Public SerNumEnable As Byte       ' non-zero if serial number to be used
            Public USBVersionEnable As Byte   ' non-zero if chip uses USBVersion
            Public USBVersion As Short        ' BCD (0x0200 => USB2)

            ' Rev 5 (FT2232) extensions
            Public Rev5 As Byte              ' non-zero if Rev5 chip, zero otherwise
            Public IsoInA As Byte            ' non-zero if in endpoint is isochronous
            Public IsoInB As Byte            ' non-zero if in endpoint is isochronous
            Public IsoOutA As Byte           ' non-zero if out endpoint is isochronous
            Public IsoOutB As Byte           ' non-zero if out endpoint is isochronous
            Public PullDownEnable5 As Byte   ' non-zero if pull down enabled
            Public SerNumEnable5 As Byte     ' non-zero if serial number to be used
            Public USBVersionEnable5 As Byte ' non-zero if chip uses USBVersion
            Public USBVersion5 As Short      ' BCD (0x0200 => USB2)
            Public AIsHighCurrent As Byte    ' non-zero if interface is high current
            Public BIsHighCurrent As Byte    ' non-zero if interface is high current
            Public IFAIsFifo As Byte         ' non-zero if interface is 245 FIFO
            Public IFAIsFifoTar As Byte      ' non-zero if interface is 245 FIFO CPU target
            Public IFAIsFastSer As Byte      ' non-zero if interface is Fast serial
            Public AIsVCP As Byte            ' non-zero if interface is to use VCP drivers
            Public IFBIsFifo As Byte         ' non-zero if interface is 245 FIFO
            Public IFBIsFifoTar As Byte      ' non-zero if interface is 245 FIFO CPU target
            Public IFBIsFastSer As Byte      ' non-zero if interface is Fast serial
            Public BIsVCP As Byte            ' non-zero if interface is to use VCP drivers

            ' Rev 6 (FT232R) extensions
            Public UseExtOsc As Byte       ' Use External Oscillator
            Public HighDriveIOs As Byte    ' High Drive I/Os
            Public EndpointSize As Byte    ' Endpoint size
            Public PullDownEnableR As Byte ' non-zero if pull down enabled
            Public SerNumEnableR As Byte   ' non-zero if serial number to be used
            Public InvertTXD As Byte       ' non-zero if invert TXD
            Public InvertRXD As Byte       ' non-zero if invert RXD
            Public InvertRTS As Byte       ' non-zero if invert RTS
            Public InvertCTS As Byte       ' non-zero if invert CTS
            Public InvertDTR As Byte       ' non-zero if invert DTR
            Public InvertDSR As Byte       ' non-zero if invert DSR
            Public InvertDCD As Byte       ' non-zero if invert DCD
            Public InvertRI As Byte        ' non-zero if invert RI
            Public Cbus0 As Byte           ' Cbus Mux control
            Public Cbus1 As Byte           ' Cbus Mux control
            Public Cbus2 As Byte           ' Cbus Mux control
            Public Cbus3 As Byte           ' Cbus Mux control
            Public Cbus4 As Byte           ' Cbus Mux control
            Public RIsD2XX As Byte         ' non-zero if using D2XX driver

            ' Rev 7 (FT2232H) Extensions
            Public PullDownEnable7 As Byte  ' non-zero if pull down enabled
            Public SerNumEnable7 As Byte    ' non-zero if serial number to be used
            Public ALSlowSlew As Byte       ' non-zero if AL pins have slow slew
            Public ALSchmittInput As Byte   ' non-zero if AL pins are Schmitt input
            Public ALDriveCurrent As Byte   ' valid values are 4mA, 8mA, 12mA, 16mA
            Public AHSlowSlew As Byte       ' non-zero if AH pins have slow slew
            Public AHSchmittInput As Byte   ' non-zero if AH pins are Schmitt input
            Public AHDriveCurrent As Byte   ' valid values are 4mA, 8mA, 12mA, 16mA
            Public BLSlowSlew As Byte       ' non-zero if BL pins have slow slew
            Public BLSchmittInput As Byte   ' non-zero if BL pins are Schmitt input
            Public BLDriveCurrent As Byte   ' valid values are 4mA, 8mA, 12mA, 16mA
            Public BHSlowSlew As Byte       ' non-zero if BH pins have slow slew
            Public BHSchmittInput As Byte   ' non-zero if BH pins are Schmitt input
            Public BHDriveCurrent As Byte   ' valid values are 4mA, 8mA, 12mA, 16mA
            Public IFAIsFifo7 As Byte       ' non-zero if interface is 245 FIFO
            Public IFAIsFifoTar7 As Byte    ' non-zero if interface is 245 FIFO CPU target
            Public IFAIsFastSer7 As Byte    ' non-zero if interface is Fast serial
            Public AIsVCP7 As Byte          ' non-zero if interface is to use VCP drivers
            Public IFBIsFifo7 As Byte       ' non-zero if interface is 245 FIFO
            Public IFBIsFifoTar7 As Byte    ' non-zero if interface is 245 FIFO CPU target
            Public IFBIsFastSer7 As Byte    ' non-zero if interface is Fast serial
            Public BIsVCP7 As Byte          ' non-zero if interface is to use VCP drivers
            Public PowerSaveEnable As Byte  ' non-zero if using BCBUS7 to save power for self-powered designs

            ' Rev 8 (FT4232H) Extensions
            Public PullDownEnable8 As Byte ' non-zero if pull down enabled
            Public SerNumEnable8 As Byte   ' non-zero if serial number to be used
            Public ASlowSlew As Byte       ' non-zero if A pins have slow slew
            Public ASchmittInput As Byte   ' non-zero if A pins are Schmitt input
            Public ADriveCurrent As Byte   ' valid values are 4mA, 8mA, 12mA, 16mA
            Public BSlowSlew As Byte       ' non-zero if B pins have slow slew
            Public BSchmittInput As Byte   ' non-zero if B pins are Schmitt input
            Public BDriveCurrent As Byte   ' valid values are 4mA, 8mA, 12mA, 16mA
            Public CSlowSlew As Byte       ' non-zero if C pins have slow slew
            Public CSchmittInput As Byte   ' non-zero if C pins are Schmitt input
            Public CDriveCurrent As Byte   ' valid values are 4mA, 8mA, 12mA, 16mA
            Public DSlowSlew As Byte       ' non-zero if D pins have slow slew
            Public DSchmittInput As Byte   ' non-zero if D pins are Schmitt input
            Public DDriveCurrent As Byte   ' valid values are 4mA, 8mA, 12mA, 16mA
            Public ARIIsTXDEN As Byte      ' non-zero if port A uses RI as RS485 TXDEN
            Public BRIIsTXDEN As Byte      ' non-zero if port B uses RI as RS485 TXDEN
            Public CRIIsTXDEN As Byte      ' non-zero if port C uses RI as RS485 TXDEN
            Public DRIIsTXDEN As Byte      ' non-zero if port D uses RI as RS485 TXDEN
            Public AIsVCP8 As Byte         ' non-zero if interface is to use VCP drivers
            Public BIsVCP8 As Byte         ' non-zero if interface is to use VCP drivers
            Public CIsVCP8 As Byte         ' non-zero if interface is to use VCP drivers
            Public DIsVCP8 As Byte         ' non-zero if interface is to use VCP drivers

            ' Rev 9 (FT232H) Extensions
            Public PullDownEnableH As Byte    ' non-zero if pull down enabled
            Public SerNumEnableH As Byte      ' non-zero if serial number to be used
            Public ACSlowSlewH As Byte        ' non-zero if AC pins have slow slew
            Public ACSchmittInputH As Byte    ' non-zero if AC pins are Schmitt input
            Public ACDriveCurrentH As Byte    ' valid values are 4mA, 8mA, 12mA, 16mA
            Public ADSlowSlewH As Byte        ' non-zero if AD pins have slow slew
            Public ADSchmittInputH As Byte    ' non-zero if AD pins are Schmitt input
            Public ADDriveCurrentH As Byte    ' valid values are 4mA, 8mA, 12mA, 16mA
            Public Cbus0H As Byte             ' Cbus Mux control
            Public Cbus1H As Byte             ' Cbus Mux control
            Public Cbus2H As Byte             ' Cbus Mux control
            Public Cbus3H As Byte             ' Cbus Mux control
            Public Cbus4H As Byte             ' Cbus Mux control
            Public Cbus5H As Byte             ' Cbus Mux control
            Public Cbus6H As Byte             ' Cbus Mux control
            Public Cbus7H As Byte             ' Cbus Mux control
            Public Cbus8H As Byte             ' Cbus Mux control
            Public Cbus9H As Byte             ' Cbus Mux control
            Public IsFifoH As Byte            ' non-zero if interface is 245 FIFO
            Public IsFifoTarH As Byte         ' non-zero if interface is 245 FIFO CPU target
            Public IsFastSerH As Byte         ' non-zero if interface is Fast serial
            Public IsFT1248H As Byte          ' non-zero if interface is FT1248
            Public FT1248CpolH As Byte        ' FT1248 clock polarity - clock idle high (1) or clock idle low (0)
            Public FT1248LsbH As Byte         ' FT1248 data is LSB (1) or MSB (0)
            Public FT1248FlowControlH As Byte ' FT1248 flow control enable
            Public IsVCPH As Byte             ' non-zero if interface is to use VCP drivers
            Public PowerSaveEnableH As Byte   ' non-zero if using ACBUS7 to save power for self-powered designs
        End Structure

        ''' <summary>
        ''' Типы устройств.
        ''' </summary>
        ''' <remarks>typedef ULONG FT_DEVICE;</remarks>
        Public Enum FT_DEVICE As Integer
            FT_DEVICE_BM
            FT_DEVICE_AM
            FT_DEVICE_100AX
            FT_DEVICE_UNKNOWN
            FT_DEVICE_2232C
            FT_DEVICE_232R
            FT_DEVICE_2232H
            FT_DEVICE_4232H
            FT_DEVICE_232H
            FT_DEVICE_X_SERIES
            FT_DEVICE_4222H_0
            FT_DEVICE_4222H_1_2
            FT_DEVICE_4222H_3
            FT_DEVICE_4222_PROG
            FT_DEVICE_900
        End Enum

#End Region '/СТРУКТУРЫ И ДАННЫЕ

        ''' <summary>
        ''' This function can be of use when trying to recover devices programatically.
        ''' </summary>
        ''' <returns>FT_OK if successful, otherwise the return value is an FT error code.</returns>
        ''' <remarks>Calling FT_Rescan is equivalent to clicking the "Scan for hardware changes" button in the Device Manager. 
        ''' Only USB hardware is checked for new devices. All USB devices are scanned, not just FTDI devices.</remarks>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_Rescan() As Integer
        End Function

        ''' <summary>
        ''' This function can be of use when trying to recover devices programatically.
        ''' </summary>
        ''' <remarks>Calling Rescan() is equivalent to clicking the "Scan for hardware changes" button in the Device Manager. 
        ''' Only USB hardware is checked for new devices. All USB devices are scanned, not just FTDI devices.</remarks>
        Public Shared Sub FtRescan()
            Dim r As Integer = FT_Rescan()
            CheckStatus(r)
        End Sub

        ''' <summary>
        ''' This function forces a reload of the driver for devices with a specific VID and PID combination.
        ''' </summary>
        ''' <param name="wVid">Vendor ID of the devices to reload the driver for.</param>
        ''' <param name="wPid">Product ID of the devices to reload the driver for.</param>
        ''' <returns>FT_OK if successful, otherwise the return value is an FT error code.</returns>
        ''' <remarks>Calling FT_Reload forces the operating system to unload and reload the driver for the specified device IDs. 
        ''' If the VID and PID parameters are null, the drivers for USB root hubs will be reloaded, causing all USB devices connected to reload their drivers. 
        ''' Please note that this function will not work correctly on 64-bit Windows when called from a 32-bit application.</remarks>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_Reload(wVid As Short, wPid As Short) As Integer
        End Function

        ''' <summary>
        ''' This function forces a reload of the driver for devices with a specific VID and PID combination.
        ''' </summary>
        ''' <param name="wVid">Vendor ID of the devices to reload the driver for.</param>
        ''' <param name="wPid">Product ID of the devices to reload the driver for.</param>
        ''' <remarks>Calling FT_Reload forces the operating system to unload and reload the driver for the specified device IDs. 
        ''' If the VID and PID parameters are null, the drivers for USB root hubs will be reloaded, causing all USB devices connected to reload their drivers. 
        ''' Please note that this function will not work correctly on 64-bit Windows when called from a 32-bit application.</remarks>
        Public Shared Sub FtReload(wVid As Short, wPid As Short)
            Dim r As Integer = FT_Reload(wVid, wPid)
            CheckStatus(r)
        End Sub

        ''' <summary>
        ''' Создаёт список устройств и возвращает кол-во подключённых устройств в системе (как открытых, так и закрытых).
        ''' </summary>
        ''' <param name="lpdwNumDevs">Указатель на список подключённых устрйоств FTDI [unsigned long].</param>
        ''' <returns>FT_OK в случае успеха, иначе код ошибки FT.</returns>
        ''' <remarks>An application can use this function to get the number of devices attached to the system. It can then
        ''' allocate space for the device information list and retrieve the list using FT_GetDeviceInfoList or
        ''' FT_GetDeviceInfoDetailFT_GetDeviceInfoDetail.
        ''' If the devices connected to the system change, the device info list will not be updated until
        ''' FT_CreateDeviceInfoList is called again.</remarks>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_CreateDeviceInfoList(ByRef lpdwNumDevs As Integer) As Integer
        End Function

        ''' <summary>
        ''' Создаёт список устройств и возвращает кол-во подключённых устройств в системе (как открытых, так и закрытых).
        ''' </summary>
        Public Shared Function FtCreateDeviceInfoList() As Integer
            Dim lpdwNumDevs As Integer 'Указатель на список подключённых устройств FTDI [unsigned long].
            Dim r As Integer = FT_CreateDeviceInfoList(lpdwNumDevs)
            CheckStatus(r)
            Return lpdwNumDevs
        End Function

        ''' <summary>
        ''' Возвращает информацию об устройстве и номер D2XX устройства в списке.
        ''' </summary>
        ''' <param name="pDest">Pointer to an array of FT_DEVICE_LIST_INFO_NODE() structures.</param>
        ''' <param name="lpdwNumDevs">Pointer to the number of elements in the array.</param>
        ''' <returns>FT_OK if successful, otherwise the return value is an FT error code. </returns>
        ''' <remarks>This function should only be called after calling FT_CreateDeviceInfoList. If the devices connected to the
        ''' system change, the device info list will not be updated until FT_CreateDeviceInfoList is called again.</remarks>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_GetDeviceInfoList(ByRef pDest As Integer, ByRef lpdwNumDevs As UInteger) As Integer
        End Function

        ''' <summary>
        ''' Возвращает список устройств FTDI в системе.
        ''' </summary>
        Public Shared Function FtGetDeviceInfoList() As FT_DEVICE_LIST_INFO_NODE()
            Try
                Dim pDest As Integer
                Dim devsCount As UInteger
                Dim r As Integer = FT_GetDeviceInfoList(pDest, devsCount)
                CheckStatus(r)

                'Dim ty As Type = GetType(tFT_CreateDeviceInfoList)
                'Dim delegateForFunctionPointer As tFT_CreateDeviceInfoList _
                '    = DirectCast(Marshal.GetDelegateForFunctionPointer(pFT_CreateDeviceInfoList, ty), tFT_CreateDeviceInfoList)
                'Dim s As FT_STATUS = delegateForFunctionPointer.Invoke(devsCount)

                'Dim nodes() As FT_DEVICE_LIST_INFO_NODE = New FT_DEVICE_LIST_INFO_NODE(devsCount - 1) {}
                'Marshal.PtrToStructure(pDest, nodes)
                'Debug.WriteLine(Marshal.ReadByte(pDest, 0))
                'Debug.WriteLine(Marshal.ReadByte(pDest, 1))
                'Debug.WriteLine(Marshal.ReadByte(pDest, 2))
                'Debug.WriteLine(Marshal.ReadByte(pDest, 3))

                'TODO возвращать оба значения.
                'Return lpdwNumDevs
                'Return pDest(0)
                Return New FT_DEVICE_LIST_INFO_NODE() {} 'TODO Доделать.
            Catch ex As Exception
                Debug.WriteLine(ex.Message)
                Return New FT_DEVICE_LIST_INFO_NODE() {}
            End Try
        End Function

        ''' <summary>
        ''' Get device information for an open device.
        ''' </summary>
        ''' <param name="ftHandle">Handle of the device.</param>
        ''' <param name="lpftDevice">Pointer to unsigned long to store device type.</param>
        ''' <param name="lpdwID">Pointer to unsigned long to store device ID.</param>
        ''' <param name="SerialNumber">Pointer to buffer to store device serial number as a null-terminated string.</param>
        ''' <param name="Description">Pointer to buffer to store device description as a null-terminated string.</param>
        ''' <param name="Dummy">Reserved for future use - should be set to NULL.</param>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_GetDeviceInfo(ftHandle As Integer, lpftDevice() As Integer, ByRef lpdwID As Integer,
                                                 <MarshalAs(UnmanagedType.LPStr)> serialNumber As StringBuilder,
                                                 <MarshalAs(UnmanagedType.LPStr)> description As StringBuilder,
                                                 dummy As IntPtr) As Integer
        End Function

        ''' <summary>
        ''' Получает информацию об открытом устройстве.
        ''' </summary>
        Public Shared Function GetDeviceInfo(ftHandle As Integer) As FT_DEVICE_LIST_INFO_NODE
            Dim lpftDevice(0) As Integer
            Dim lpdwID As Integer = 0
            Dim sb1 As New StringBuilder(31)
            Dim sb2 As New StringBuilder(31)
            Dim r As Integer = FT_GetDeviceInfo(ftHandle, lpftDevice, lpdwID, sb1, sb2, Nothing)
            CheckStatus(r)
            Dim n As New FT_DEVICE_LIST_INFO_NODE With {
                .ftHandle = ftHandle,
                .ID = CUInt(lpdwID),
                .SerialNumber = sb1.ToString(),
                .Description = sb2.ToString()
            }
            Return n
        End Function

        ''' <summary>
        ''' This function returns an entry from the device information list. 
        ''' </summary>
        ''' <param name="dwIndex">Index of the entry in the device info list (zero-based).</param>
        ''' <param name="lpdwFlags">Pointer to unsigned long to store the flag value.</param>
        ''' <param name="lpdwType">Pointer to unsigned long to store device type.</param>
        ''' <param name="lpdwID">Pointer to unsigned long to store device ID.</param>
        ''' <param name="lpdwLocId">Pointer to unsigned long to store the device location ID.</param>
        ''' <param name="lpSerialNumber">Pointer to buffer to store device serial number as a null-terminated string.</param>
        ''' <param name="lpDescription">Pointer to buffer to store device description as a null-terminated string.</param>
        ''' <param name="pftHandle">Pointer to a variable of type FT_HANDLE where the handle will be stored.</param>
        ''' <returns>FT_OK if successful, otherwise the return value is an FT error code.</returns>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_GetDeviceInfoDetail(dwIndex As Integer, ByRef lpdwFlags As IntPtr, ByRef lpdwType As IntPtr, ByRef lpdwID As IntPtr, ByRef lpdwLocId As IntPtr,
                                                       <MarshalAs(UnmanagedType.LPStr)> lpSerialNumber As StringBuilder,
                                                       <MarshalAs(UnmanagedType.LPStr)> lpDescription As StringBuilder,
                                                       ByRef pftHandle As IntPtr) As Integer
        End Function

        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_SetTimeouts(ftHandle As Integer, dwReadTimeout As Integer, dwWriteTimeout As Integer) As Integer
            'Private Shared Function FT_SetTimeouts(ftHandle As Integer, dwReadTimeout As FT_TIMEOUT, dwWriteTimeout As FT_TIMEOUT) As Integer
        End Function

        ''' <summary>
        ''' Задаёт таймауты чтения и записи.
        ''' </summary>
        ''' <param name="ftHandle"></param>
        ''' <param name="dwReadTimeout"></param>
        ''' <param name="dwWriteTimeout"></param>
        Public Shared Sub SetTimeout(ftHandle As Integer, dwReadTimeout As Integer, dwWriteTimeout As Integer)
            CheckStatus(FT_SetTimeouts(ftHandle, dwReadTimeout, dwWriteTimeout))
        End Sub

        ''' <summary>
        ''' Возвращает структуру с описанием FTDI устройства. 
        ''' Также внутри функции определяются флаги, тип, ID, LocID и указатель, но они не возвращаются.
        ''' </summary>
        ''' <param name="dwIndex">Index of the entry in the device info list (zero-based).</param>
        ''' <remarks>Перед вызовом необходимо создть список устройств, вызвав FtGetDeviceInfoList().</remarks>
        Public Shared Function FtGetDeviceInfoDetail(dwIndex As Integer) As FT_DEVICE_LIST_INFO_NODE
            Dim lpdwFlags As IntPtr
            Dim lpdwType As IntPtr
            Dim lpdwID As IntPtr
            Dim lpdwLocId As IntPtr
            Dim lpSerialNumber As New StringBuilder(15)
            Dim lpDescription As New StringBuilder(63)
            Dim pftHandle As IntPtr

            Dim r As Integer = FT_GetDeviceInfoDetail(dwIndex, lpdwFlags, lpdwType, lpdwID, lpdwLocId, lpSerialNumber, lpDescription, pftHandle)
            CheckStatus(r)

            Dim n As New FT_DEVICE_LIST_INFO_NODE With {
                .Flags = CUInt(lpdwFlags),
                .Type = CUInt(lpdwType),
                .ID = CUInt(lpdwID),
                .LocId = CInt(lpdwLocId),
                .Description = lpDescription.ToString(),
                .SerialNumber = lpSerialNumber.ToString(),
                .ftHandle = CInt(pftHandle),
                .Index = dwIndex
            }
            Return n
        End Function

        ''' <summary>
        ''' This function returns D2XX DLL version number.
        ''' </summary>
        ''' <param name="lpdwVersion">Pointer to the DLL version number.</param>
        ''' <returns>FT_OK if successful, otherwise the return value is an FT error code.</returns>
        ''' <remarks>A version number consists of major, minor and build version numbers contained in a 4-byte field 
        ''' (unsigned long). Byte0 (least significant) holds the build version, Byte1 holds the minor version, and
        ''' Byte2 holds the major version. Byte3 is currently set to zero.
        ''' For example, D2XX DLL version "3.01.15" is represented as 0x00030115. Note that this function does
        ''' not take a handle, and so it can be called without opening a device.</remarks>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_GetLibraryVersion(ByRef lpdwVersion As IntPtr) As Integer
        End Function

        ''' <summary>
        ''' This function returns D2XX DLL version number as string.
        ''' </summary>
        Public Shared Function GetLibraryVersion() As String
            Dim lpdwVersion As IntPtr
            Dim r As Integer = FT_GetLibraryVersion(lpdwVersion)

            Dim ver As New StringBuilder(Convert.ToString(lpdwVersion.ToInt32, 16))
            ver.Insert(ver.Length - 2, ".")
            ver.Insert(ver.Length - 5, ".")
            CheckStatus(r)

            Return ver.ToString()
        End Function

        ''' <summary>
        ''' Gets information concerning the devices currently connected. 
        ''' </summary>
        ''' <param name="pArg1">Meaning depends on dwFlags.</param>
        ''' <param name="pArg2">Meaning depends on dwFlags.</param>
        ''' <param name="Flags">Determines format of returned information.</param>
        ''' <returns>FT_OK if successful, otherwise the return value is an FT error code.</returns>
        ''' <remarks>Gets information concerning the devices currently connected. This function can return information such
        ''' as the number of devices connected, the device serial number and device description strings, and the
        ''' location IDs of connected devices.</remarks>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_ListDevices(ByRef pArg1 As IntPtr, pArg2 As IntPtr, Flags As Integer) As Integer
        End Function

        ''' <summary>
        ''' Gets information concerning the devices currently connected. 
        ''' </summary>
        ''' <param name="pArg1">Meaning depends on dwFlags.</param>
        ''' <param name="pArg2">Meaning depends on dwFlags.</param>
        ''' <param name="Flags">Determines format of returned information.</param>
        ''' <remarks>Gets information concerning the devices currently connected. This function can return information such
        ''' as the number of devices connected, the device serial number and device description strings, and the
        ''' location IDs of connected devices.</remarks>
        Public Shared Sub ListDevices(pArg1 As IntPtr, pArg2 As IntPtr, flags As Integer)
            Dim r As Integer = FT_ListDevices(pArg1, pArg2, flags)
            CheckStatus(r)
            'TODO 

        End Sub

        ''' <summary>
        ''' This function returns the D2XX driver version number.
        ''' </summary>
        ''' <param name="ftHandle">Handle of the device.</param>
        ''' <param name="lpdwVersion">Pointer to the driver version number.</param>
        ''' <returns>FT_OK if successful, otherwise the return value is an FT error code.</returns>
        ''' <remarks>A version number consists of major, minor and build version numbers contained in a 4-byte field
        ''' (unsigned long). Byte0 (least significant) holds the build version, Byte1 holds the minor version, and
        ''' Byte2 holds the major version. Byte3 is currently set to zero.
        ''' For example, driver version "2.04.06" is represented as 0x00020406. Note that a device has to be
        ''' opened before this function can be called.</remarks>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_GetDriverVersion(ftHandle As Integer, ByRef lpdwVersion As Integer) As Integer
        End Function

        ''' <summary>
        ''' This function returns the D2XX driver version number as string.
        ''' </summary>
        ''' <param name="ftHandle">Handle of the device.</param>
        ''' <remarks>A version number consists of major, minor and build version numbers contained in a 4-byte field
        ''' (unsigned long). Byte0 (least significant) holds the build version, Byte1 holds the minor version, and
        ''' Byte2 holds the major version. Byte3 is currently set to zero.
        ''' For example, driver version "2.04.06" is represented as 0x00020406. Note that a device has to be
        ''' opened before this function can be called.</remarks>
        Public Shared Function GetDriverVersion(ftHandle As Integer) As String
            Dim lpdwVersion As Integer = 0
            Dim r As Integer = FT_GetDriverVersion(ftHandle, lpdwVersion)
            CheckStatus(r)
            Dim verDrv As New StringBuilder(Convert.ToString(lpdwVersion, 16))
            verDrv.Insert(verDrv.Length - 2, ".")
            verDrv.Insert(verDrv.Length - 5, ".")
            Return verDrv.ToString()
        End Function

#Region "ОТКРЫТИЕ И ЗАКРЫТИЕ УСТРОЙСТВА"

        ''' <summary>
        ''' Открывает устройство и обновляет дескриптор устройства. Open the device and return a handle which will be used for subsequent accesses.
        ''' </summary>
        ''' <param name="deviceNumber">Index of the device to open. Indices are 0 based.</param>
        ''' <param name="pHandle">Pointer to a variable of type FT_HANDLE where the handle will be
        ''' stored. This handle must be used to access the device.</param>
        ''' <remarks>Although this function can be used to open multiple devices by setting iDevice to 0, 1, 2 etc. there is no
        ''' ability to open a specific device. To open named devices, use the function FT_OpenEx.</remarks>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_Open(deviceNumber As Integer, ByRef pHandle As Integer) As Integer
        End Function

        ''' <summary>
        ''' Открывает устройство и обновляет дескриптор устройства. Open the device and return a handle which will be used for subsequent accesses.
        ''' </summary>
        ''' <param name="deviceNumber">Index of the device to open. Indices are 0 based.</param>
        ''' <remarks>Although this function can be used to open multiple devices by setting iDevice to 0, 1, 2 etc. there is no
        ''' ability to open a specific device. To open named devices, use the function FT_OpenEx.</remarks>
        Public Shared Function OpenDevice(deviceNumber As Integer) As Integer
            Dim pHandle As Integer = 0 'Pointer to a variable of type FT_HANDLE where the handle will be stored. This handle must be used to access the device.
            Dim r As Integer = FT_Open(deviceNumber, pHandle)
            CheckStatus(r)
            Return pHandle
        End Function

        ''' <summary>
        ''' Open the specified device and return a handle that will be used for subsequent accesses. The device can
        ''' be specified by its serial number, device description or location.
        ''' </summary>
        ''' <param name="pArg1">Pointer to an argument whose type depends on the value of
        ''' dwFlags. It is normally be interpreted as a pointer to a null terminated string.</param>
        ''' <param name="Flags">FT_OPEN_BY_SERIAL_NUMBER, FT_OPEN_BY_DESCRIPTION or FT_OPEN_BY_LOCATION.</param>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_OpenEx(pArg1 As IntPtr, flags As Integer, pHandle As IntPtr) As Integer
        End Function

        ''' <summary>
        ''' Open the specified device and return a handle that will be used for subsequent accesses. The device can
        ''' be specified by its serial number, device description or location.
        ''' </summary>
        ''' <param name="pArg1">Pointer to an argument whose type depends on the value of
        ''' dwFlags. It is normally be interpreted as a pointer to a null terminated string.</param>
        ''' <param name="Flags">FT_OPEN_BY_SERIAL_NUMBER, FT_OPEN_BY_DESCRIPTION or FT_OPEN_BY_LOCATION.</param>
        ''' <param name="pHandle">Pointer to a variable of type FT_HANDLE where the handle will be
        ''' stored. This handle must be used to access the device.</param>
        Public Shared Sub OpenDeviceEx(pArg1 As IntPtr, flags As Integer, pHandle As IntPtr)
            Dim r As Integer = FT_OpenEx(pArg1, flags, pHandle)
            CheckStatus(r)
        End Sub

        ''' <summary>
        ''' Закрывает открытое устройство.
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_Close(ftHandle As Integer) As Integer
        End Function

        ''' <summary>
        ''' Закрывает открытое устройство.
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        Public Shared Sub CloseDevice(ftHandle As Integer)
            Dim r As Integer = FT_Close(ftHandle)
            CheckStatus(r)
        End Sub

        ''' <summary>
        ''' Сброс устройства.
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_ResetDevice(ftHandle As Integer) As Integer
        End Function

        ''' <summary>
        ''' Сброс устройства.
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        Public Shared Sub ResetDevice(ftHandle As Integer)
            Dim r As Integer = FT_ResetDevice(ftHandle)
            CheckStatus(r)
        End Sub

#End Region '/ОТКРЫТИЕ И ЗАКРЫТИЕ УСТРОЙСТВА

#Region "РЕЖИМЫ РАБОТЫ"

        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_GetBitMode(ftHandle As Integer, pucMode As Byte()) As Integer
        End Function

        ''' <summary>
        ''' Gets the instantaneous value of the data bus.
        ''' </summary>
        ''' <param name="ftHandle">Handle of the device.</param>
        Public Shared Function GetBitMode(ByVal ftHandle As Integer) As Byte
            Dim m(9) As Byte
            Dim r As Integer = FT_GetBitMode(ftHandle, m)
            CheckStatus(r)
            Return m(0)
        End Function

        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_SetBitMode(ftHandle As Integer, ucMask As Byte, ucEnable As Byte) As Integer
        End Function

        ''' <summary>
        ''' Enables different chip modes.
        ''' </summary>
        ''' <param name="ftHandle">Handle of the device.</param>
        ''' <param name="ucMask">
        ''' Required value for bit mode mask. This sets up which bits are inputs and outputs. 
        ''' A bit value of 0 sets the corresponding pin to an input, a bit value of 1 sets the corresponding pin to an output.
        ''' In the case of CBUS Bit Bang, the upper nibble of this value controls which pins are inputs And outputs, 
        ''' While the lower nibble controls which of the outputs are high And low.
        ''' </param>
        ''' <param name="ucMode">
        ''' Mode value. Can be one of the following:
        ''' <ul>
        ''' <li>0x0 = Reset</li>
        ''' <li>0x1 = Asynchronous Bit Bang</li>
        ''' <li>0x2 = MPSSE (FT2232, FT2232H, FT4232H And FT232H devices only)</li>
        ''' <li>0x4 = Synchronous Bit Bang (FT232R, FT245R, FT2232, FT2232H, FT4232H And FT232H devices only)</li>
        ''' <li>0x8 = MCU Host Bus Emulation Mode (FT2232, FT2232H, FT4232H And FT232H devices only)</li>
        ''' <li>0x10 = Fast Opto-Isolated Serial Mode (FT2232, FT2232H, FT4232H And FT232H devices only)</li>
        ''' <li>0x20 = CBUS Bit Bang Mode (FT232R And FT232H devices only)</li>
        ''' <li>0x40 = Single Channel Synchronous 245 FIFO Mode (FT2232H And FT232H devices only)</li>
        ''' </ul>
        ''' </param>
        Public Shared Sub SetBitMode(ftHandle As Integer, ucMask As Byte, ucMode As FT_BITMODE)
            Dim r As Integer = FT_SetBitMode(ftHandle, ucMask, ucMode)
            CheckStatus(r)
        End Sub

#End Region '/РЕЖИМЫ РАБОТЫ

#Region "СТАТУС УСТРОЙСТВА"

        ''' <summary>
        ''' Вызывает исключение по статусу устройства.
        ''' </summary>
        ''' <param name="st">Код статуса.</param>
        Public Shared Sub CheckStatus(st As Integer)
            'FUTURE 
            'If devHandle <> 0 Then'Дескриптор устрйоства. При передаче этого рагумента, используется для определения последней ошибки.</param>
            'st = W32GetLastError(devHandle)
            'End If
            Select Case st
                Case StatusCode.FT_OK
                    Exit Sub
                Case StatusCode.FT_INVALID_HANDLE
                    Throw New NullReferenceException("Ошибка: Неверный дескриптор устройства")
                Case StatusCode.FT_DEVICE_NOT_FOUND
                    Throw New Exception("Ошибка: Устройство не найдено")
                Case StatusCode.FT_DEVICE_NOT_OPENED
                    Throw New Exception("Ошибка: Устройство невозможно открыть")
                Case StatusCode.FT_IO_ERROR
                    Throw New Exception("Ошибка: Ошибка чтения/записи")
                Case StatusCode.FT_INSUFFICIENT_RESOURCES
                    Throw New Exception("Ошибка: Несуществующий ресурс")
                Case StatusCode.FT_INVALID_PARAMETER
                    Throw New Exception("Ошибка: Неверный параметр")
                Case StatusCode.FT_INVALID_BAUD_RATE
                    Throw New Exception("Ошибка: Неверный битрейт")
                Case StatusCode.FT_DEVICE_NOT_OPENED_FOR_ERASE
                    Throw New Exception("Ошибка: Устройство не открыто для очистки")
                Case StatusCode.FT_DEVICE_NOT_OPENED_FOR_WRITE
                    Throw New Exception("Ошибка: Устройство не открыто для записи")
                Case StatusCode.FT_FAILED_TO_WRITE_DEVICE
                    Throw New Exception("Ошибка: Ошибка записи на устройство")
                Case StatusCode.FT_EEPROM_READ_FAILED
                    Throw New Exception("Ошибка: Ошибка чтения EEPROM")
                Case StatusCode.FT_EEPROM_WRITE_FAILED
                    Throw New Exception("Ошибка: Ошибка записи в EEPROM")
                Case StatusCode.FT_EEPROM_ERASE_FAILED
                    Throw New Exception("Ошибка стирания EEPROM")
                Case StatusCode.FT_EEPROM_NOT_PRESENT
                    Throw New Exception("Ошибка: EEPROM не представлено")
                Case StatusCode.FT_EEPROM_NOT_PROGRAMMED
                    Throw New Exception("Ошибка: EEPROM не запрограммировано")
                Case StatusCode.FT_INVALID_ARGS
                    Throw New Exception("Ошибка: Неверные аргументы")
                Case StatusCode.FT_NOT_SUPPORTED
                    Throw New Exception("Ошибка: Не поддерживается")
                Case StatusCode.FT_OTHER_ERROR
                    Throw New Exception("Ошибка: Иная ошибка")
                Case StatusCode.FT_DEVICE_LIST_NOT_READY
                    Throw New Exception("Ошибка: Список устройств не готов")

                Case StatusCode.FTC_FAILED_TO_COMPLETE_COMMAND
                    Throw New Exception("Ошибка: Невозможно завершить задачу")
                Case StatusCode.FTC_FAILED_TO_SYNCHRONIZE_DEVICE_MPSSE
                    Throw New Exception("Ошибка: Невозможно синхронизировать устройство MPSSE")
                Case StatusCode.FTC_INVALID_DEVICE_NAME_INDEX
                    Throw New Exception("Ошибка: Неверные имя или индекс устройства")
                Case StatusCode.FTC_NULL_DEVICE_NAME_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель на имя устройства")
                Case StatusCode.FTC_DEVICE_NAME_BUFFER_TOO_SMALL
                    Throw New Exception("Ошибка: Слишком маленький буфер для имени устройства")
                Case StatusCode.FTC_INVALID_DEVICE_NAME
                    Throw New Exception("Ошибка: Неверное имя устройства")
                Case StatusCode.FTC_INVALID_LOCATION_ID
                    Throw New Exception("Ошибка: Неверный Location ID")
                Case StatusCode.FTC_DEVICE_IN_USE
                    Throw New Exception("Ошибка: Устройство занято")
                Case StatusCode.FTC_TOO_MANY_DEVICES
                    Throw New Exception("Ошибка: Слишком много устройств")
                Case StatusCode.FTC_NULL_CHANNEL_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель номера канала")
                Case StatusCode.FTC_CHANNEL_BUFFER_TOO_SMALL
                    Throw New Exception("Ошибка: Слишком маленький буфер для канала устройства")
                Case StatusCode.FTC_INVALID_CHANNEL
                    Throw New Exception("Ошибка: Неверный канал")
                Case StatusCode.FTC_INVALID_TIMER_VALUE
                    Throw New Exception("Ошибка: Неверное значение таймера")
                Case StatusCode.FTC_INVALID_CLOCK_DIVISOR
                    Throw New Exception("Ошибка: Неверное значение делителя частоты")
                Case StatusCode.FTC_NULL_INPUT_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель входного буфера")
                Case StatusCode.FTC_NULL_CHIP_SELECT_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера CS")
                Case StatusCode.FTC_NULL_INPUT_OUTPUT_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера I/O")
                Case StatusCode.FTC_NULL_OUTPUT_PINS_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель выходного буфера")
                Case StatusCode.FTC_NULL_INITIAL_CONDITION_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера начального состояния")
                Case StatusCode.FTC_NULL_WRITE_CONTROL_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера контроля записи")
                Case StatusCode.FTC_NULL_WRITE_DATA_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера записи данных")
                Case StatusCode.FTC_NULL_WAIT_DATA_WRITE_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера записи данных")
                Case StatusCode.FTC_NULL_READ_DATA_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера чтения данных")
                Case StatusCode.FTC_NULL_READ_CMDS_DATA_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера чтения команд")
                Case StatusCode.FTC_INVALID_NUMBER_CONTROL_BITS
                    Throw New Exception("Ошибка: Неверное число битов управления")
                Case StatusCode.FTC_INVALID_NUMBER_CONTROL_BYTES
                    Throw New Exception("Ошибка: Неверное число байтов управления")
                Case StatusCode.FTC_NUMBER_CONTROL_BYTES_TOO_SMALL
                    Throw New Exception("Ошибка: Слишком маленькое число байтов управления")
                Case StatusCode.FTC_INVALID_NUMBER_WRITE_DATA_BITS
                    Throw New Exception("Ошибка: Неверное число битов записи данных")
                Case StatusCode.FTC_INVALID_NUMBER_WRITE_DATA_BYTES
                    Throw New Exception("Ошибка: Неверное число байтов записи данных")
                Case StatusCode.FTC_NUMBER_WRITE_DATA_BYTES_TOO_SMALL
                    Throw New Exception("Ошибка: Слишком маленькое число байтов записи данных")
                'Case StatusCode.FTC_INVALID_NUMBER_READ_DATA_BITS 50
                'Throw New Exception("Ошибка: ")
                Case StatusCode.FTC_INVALID_INIT_CLOCK_PIN_STATE
                    Throw New Exception("Ошибка: Неверное начальное состояние вывода тактовой частоты")
                Case StatusCode.FTC_INVALID_FT2232C_CHIP_SELECT_PIN
                    Throw New Exception("Ошибка: Неверный вывод CS FT2232C")
                Case StatusCode.FTC_INVALID_FT2232C_DATA_WRITE_COMPLETE_PIN
                    Throw New Exception("Ошибка: Неверный вывод завершения записи FT2232C")
                Case StatusCode.FTC_DATA_WRITE_COMPLETE_TIMEOUT
                    Throw New Exception("Ошибка: Превышено время завершения записи")
                Case StatusCode.FTC_INVALID_CONFIGURATION_HIGHER_GPIO_PIN
                    Throw New Exception("Ошибка: Неверный вывод конфигурации верхних GPIO")
                Case StatusCode.FTC_COMMAND_SEQUENCE_BUFFER_FULL
                    Throw New Exception("Ошибка: Очередь команд полна")
                Case StatusCode.FTC_NO_COMMAND_SEQUENCE
                    Throw New Exception("Ошибка: Нет очереди команд")
                Case StatusCode.FTC_NULL_CLOSE_FINAL_STATE_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера закрытия финального состояния")
                Case StatusCode.FTC_NULL_DLL_VERSION_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера версии DLL")
                Case StatusCode.FTC_DLL_VERSION_BUFFER_TOO_SMALL
                    Throw New Exception("Ошибка: Слишком маленький буфер версии DLL")
                Case StatusCode.FTC_NULL_LANGUAGE_CODE_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера кода языка")
                Case StatusCode.FTC_NULL_ERROR_MESSAGE_BUFFER_POINTER
                    Throw New Exception("Ошибка: Несуществующий указатель буфера ошибки сообщения")
                Case StatusCode.FTC_ERROR_MESSAGE_BUFFER_TOO_SMALL
                    Throw New Exception("Ошибка: Слишком маленький буфер ошибки сообщения")
                Case StatusCode.FTC_INVALID_LANGUAGE_CODE
                    Throw New Exception("Ошибка: Неверный код языка")
                Case StatusCode.FTC_INVALID_STATUS_CODE
                    Throw New Exception("Ошибка: Неверный код статуса")

                Case StatusCode.FT_WRONG_READ_BUFFER_SIZE
                    Throw New Exception("Ошибка: Задан неверный размер буфера чтения")
                Case Else
                    Throw New Exception("Ошибка: Неизвестная ошибка")
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
            'FTC_INVALID_NUMBER_READ_DATA_BITS 50
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

        ''' <summary>
        ''' Gets the last error that occurred on the device.
        ''' </summary>
        ''' <param name="ftHandle"></param>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_W32_GetLastError(ftHandle As Integer) As Integer
        End Function

        ''' <summary>
        ''' Gets the last error that occurred on the device.
        ''' </summary>
        ''' <param name="ftHandle">Указатель на устройство.</param>
        Public Shared Function W32GetLastError(ftHandle As Integer) As Integer
            Dim r As Integer = FT_W32_GetLastError(ftHandle)
            Return r
        End Function

#End Region '/СТАТУС УСТРОЙСТВА

#Region "ПЕРЕЧИСЛЕНИЯ/КОНСТАНТЫ"

        ''' <summary>
        ''' FT232R CBUS Options EEPROM values.
        ''' </summary>
        Public Enum FT_232R_CBUS As Integer
            FT_232R_CBUS_TXDEN = 0       ' Tx Data Enable
            FT_232R_CBUS_PWRON = 1       ' Power On
            FT_232R_CBUS_RXLED = 2       ' Rx LED
            FT_232R_CBUS_TXLED = 3       ' Tx LED
            FT_232R_CBUS_TXRXLED = 4     ' Tx and Rx LED
            FT_232R_CBUS_SLEEP = 5       ' Sleep
            FT_232R_CBUS_CLK48 = 6       ' 48MHz clock
            FT_232R_CBUS_CLK24 = 7       ' 24MHz clock
            FT_232R_CBUS_CLK12 = 8       ' 12MHz clock
            FT_232R_CBUS_CLK6 = 9        ' 6MHz clock
            FT_232R_CBUS_IOMODE = 10     ' IO Mode for CBUS bit-bang
            FT_232R_CBUS_BITBANG_WR = 11 ' Bit-bang write strobe
            FT_232R_CBUS_BITBANG_RD = 12 ' Bit-bang read strobe
        End Enum

        'FT232H CBUS Options EEPROM values
        Public Enum FT_232H_CBUS As Integer
            FT_232H_CBUS_TRISTATE = 0 ' Tristate
            FT_232H_CBUS_TXLED = 1    ' Tx LED
            FT_232H_CBUS_RXLED = 2    ' Rx LED
            FT_232H_CBUS_TXRXLED = 3  ' Tx and Rx LED
            FT_232H_CBUS_PWREN = 4    ' Power Enable
            FT_232H_CBUS_SLEEP = 5    ' Sleep
            FT_232H_CBUS_DRIVE_0 = 6  ' Drive pin to logic 0
            FT_232H_CBUS_DRIVE_1 = 7  ' Drive pin to logic 1
            FT_232H_CBUS_IOMODE = 8   ' IO Mode for CBUS bit-bang
            FT_232H_CBUS_TXDEN = 9    ' Tx Data Enable
            FT_232H_CBUS_CLK15 = 11   ' 15MHz clock
            FT_232H_CBUS_CLK30 = 10   ' 30MHz clock
            FT_232H_CBUS_CLK7_5 = 12  ' 7.5MHz clock
        End Enum

        ''' <summary>
        ''' FT X Series CBUS Options EEPROM values.
        ''' </summary>
        Public Enum FT_X_SERIES_CBUS As Integer
            FT_X_SERIES_CBUS_BCD_CHARGER = 13    '  Battery charger detected
            FT_X_SERIES_CBUS_BCD_CHARGER_N = 14  '  Battery charger detected inverted   
            FT_X_SERIES_CBUS_BITBANG_RD = 19     '  Bit-bang read strobe
            FT_X_SERIES_CBUS_BITBANG_WR = 18     '  Bit-bang write strobe
            FT_X_SERIES_CBUS_CLK12 = 11          '  12MHz clock 
            FT_X_SERIES_CBUS_CLK24 = 10          '  24MHz clock
            FT_X_SERIES_CBUS_CLK6 = 12           '  6MHz clock   
            FT_X_SERIES_CBUS_DRIVE_0 = 6         '  Drive pin to logic 0 
            FT_X_SERIES_CBUS_DRIVE_1 = 7         '  Drive pin to logic 1 
            FT_X_SERIES_CBUS_I2C_RXF = 16        '  I2C Rx full  
            FT_X_SERIES_CBUS_I2C_TXE = 15        '  I2C Tx empty  
            FT_X_SERIES_CBUS_IOMODE = 8          '  IO Mode for CBUS bit-bang
            FT_X_SERIES_CBUS_KEEP_AWAKE = 21     '  Keep awake
            FT_X_SERIES_CBUS_PWREN = 4           '  Power Enable
            FT_X_SERIES_CBUS_RXLED = 2           '  Rx LED
            FT_X_SERIES_CBUS_SLEEP = 5           '  Sleep  
            FT_X_SERIES_CBUS_TIMESTAMP = 20      '  Toggle output when a USB SOF token is received
            FT_X_SERIES_CBUS_TRISTATE = 0        '  Tristate
            FT_X_SERIES_CBUS_TXDEN = 9           '  Tx Data Enable
            FT_X_SERIES_CBUS_TXLED = 1           '  Tx LED 
            FT_X_SERIES_CBUS_TXRXLED = 3         '  Tx and Rx LED    
            FT_X_SERIES_CBUS_VBUS_SENSE = 17     '  Detect VBUS
        End Enum

        ''' <summary>
        ''' Baud Rates.
        ''' </summary>
        Public Enum FT_BAUDRATE As Integer
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

        ''' <summary>
        ''' Bit Modes.
        ''' </summary>
        Public Enum FT_BITMODE As Byte

            ''' <summary>
            ''' Reset
            ''' </summary>
            FT_BITMODE_RESET = 0

            ''' <summary>
            ''' 0x1 = Asynchronous Bit Bang
            ''' </summary>
            FT_BITMODE_ASYNC_BITBANG = 1

            ''' <summary>
            ''' MPSSE (FT2232, FT2232H, FT4232H And FT232H devices only)
            ''' </summary>
            FT_BITMODE_MPSSE = 2

            ''' <summary>
            ''' Synchronous Bit Bang (FT232R, FT245R, FT2232, FT2232H, FT4232H And FT232H devices only)
            ''' </summary>
            FT_BITMODE_SYNC_BITBANG = 4

            ''' <summary>
            ''' MCU Host Bus Emulation Mode (FT2232, FT2232H, FT4232H And FT232H devices only) 
            ''' </summary>
            FT_BITMODE_MCU_HOST = 8

            ''' <summary>
            ''' Fast Opto-Isolated Serial Mode (FT2232, FT2232H, FT4232H And FT232H devices only)
            ''' </summary>
            FT_BITMODE_FAST_SERIAL = 16

            ''' <summary>
            ''' CBUS Bit Bang Mode (FT232R And FT232H devices only)
            ''' </summary>
            FT_BITMODE_CBUS_BITBANG = 32

            ''' <summary>
            ''' Single Channel Synchronous 245 FIFO Mode (FT2232H And FT232H devices only)
            ''' </summary>
            FT_BITMODE_SYNC_FIFO = 64

        End Enum

        ''' <summary>
        ''' Word Lengths.
        ''' </summary>
        Public Enum FT_BIT As Integer
            FT_BITS_7 = 7
            FT_BITS_8 = 8
        End Enum

        ''' <summary>
        ''' Timeouts, мс.
        ''' </summary>
        Public Enum FT_TIMEOUT
            FT_DEFAULT_RX_TIME = 300
            FT_DEFAULT_TX_TIMEOUT = 0
        End Enum

        ''' <summary>
        ''' Driver types.
        ''' </summary>
        Public Enum FT_DRIVER_TYPE As Integer
            FT_DRIVER_TYPE_D2XX = 0
            FT_DRIVER_TYPE_VCP = 1
        End Enum

        'Events
        'typedef void (*PFT_EVENT_HANDLER)(DWORD,DWORD);
        Public Const FT_EVENT_LINE_STATUS As Integer = 4
        Public Const FT_EVENT_MODEM_STATUS As Integer = 2
        Public Const FT_EVENT_RXCHAR As Integer = 1

        'Флаги
        Public Const FT_FLOW_DTR_DSR As Integer = 512
        Public Const FT_FLOW_NONE As Integer = 0
        Public Const FT_FLOW_RTS_CTS As Integer = 256
        Public Const FT_FLOW_XON_XOFF As Integer = 1024
        Public Const FT_LIST_ALL As Integer = 536870912
        Public Const FT_LIST_BY_INDEX As Integer = 1073741824
        Public Const FT_LIST_MASK As Integer = -536870912
        Public Const FT_LIST_NUMBER_ONLY As Integer = -2147483648
        Public Const FT_OPEN_BY_DESCRIPTION As Integer = 2
        Public Const FT_OPEN_BY_LOCATION As Integer = 4
        Public Const FT_OPEN_BY_SERIAL_NUMBER As Integer = 1
        Public Const FT_OPEN_MASK As Integer = 7
        Public Const FT_PARITY_EVEN As Integer = 2
        Public Const FT_PARITY_MARK As Integer = 3
        Public Const FT_PARITY_NONE As Integer = 0
        Public Const FT_PARITY_ODD As Integer = 1
        Public Const FT_PARITY_SPACE As Integer = 4
        Public Const FT_PURGE_RX As Integer = 1
        Public Const FT_PURGE_TX As Integer = 2
        Public Const FT_STOP_BITS_1 As Integer = 0
        Public Const FT_STOP_BITS_2 As Integer = 2
        Private Const V As Integer = 0

#End Region '/ПЕРЕЧИСЛЕНИЯ/КОНСТАНТЫ

#Region "РАБОТА С EEPROM"

        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_EE_Program(ftHandle As Integer, pData As PFT_PROGRAM_DATA) As Integer
        End Function

        ''' <summary>
        ''' Запись в ПЗУ микросхемы.
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        ''' <param name="pData">Данные об устройстве.</param>
        Public Shared Sub EepromProgram(ftHandle As Integer, pData As PFT_PROGRAM_DATA)
            Dim r As Integer = FT_EE_Program(ftHandle, pData)
            CheckStatus(r)
        End Sub

        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_EE_ProgramEx(ftHandle As IntPtr, pData As PFT_PROGRAM_DATA,
                                                <MarshalAs(UnmanagedType.LPStr)> Manufacturer As StringBuilder,
                                                <MarshalAs(UnmanagedType.LPStr)> ManufacturerId As StringBuilder,
                                                <MarshalAs(UnmanagedType.LPStr)> Description As StringBuilder,
                                                <MarshalAs(UnmanagedType.LPStr)> SerialNumber As StringBuilder) As Integer
        End Function

        ''' <summary>
        ''' Запись в ПЗУ микросхемы, расширенный режим.
        ''' </summary>
        ''' <param name="ftHandle"></param>
        ''' <param name="pData"></param>
        ''' <param name="Manufacturer"></param>
        ''' <param name="ManufacturerId"></param>
        ''' <param name="Description"></param>
        ''' <param name="SerialNumber"></param>
        Public Shared Sub EepromProgramEx(ftHandle As IntPtr, pData As PFT_PROGRAM_DATA,
                                          Manufacturer As StringBuilder, ManufacturerId As StringBuilder,
                                          Description As StringBuilder, SerialNumber As StringBuilder)
            Dim r As Integer = FT_EE_ProgramEx(ftHandle, pData, Manufacturer, ManufacturerId, Description, SerialNumber)
            CheckStatus(r)
        End Sub

        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_EE_Read(ftHandle As Integer, ByRef pData As PFT_PROGRAM_DATA) As Integer
        End Function

        ''' <summary>
        ''' Считывает содержимое ПЗУ микросхемы.
        ''' </summary>
        ''' <param name="ftHandle"></param>
        Public Shared Function EepromRead(ftHandle As Integer) As PFT_PROGRAM_DATA
            Dim pd As New PFT_PROGRAM_DATA
            Dim r As Integer = FT_EE_Read(ftHandle, pd)
            CheckStatus(r)
            Return pd
        End Function

        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_EE_ReadEx(ftHandle As Integer, pData As PFT_PROGRAM_DATA,
                                             <MarshalAs(UnmanagedType.LPStr)> Manufacturer As StringBuilder,
                                             <MarshalAs(UnmanagedType.LPStr)> ManufacturerId As StringBuilder,
                                             <MarshalAs(UnmanagedType.LPStr)> Description As StringBuilder,
                                             <MarshalAs(UnmanagedType.LPStr)> SerialNumber As StringBuilder) As Integer
        End Function

        ''' <summary>
        ''' Считывание из ПЗУ микросхемы, расширенный режим.
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        Public Shared Function EepromReadEx(ftHandle As Integer) As PFT_PROGRAM_DATA
            Dim pData As New PFT_PROGRAM_DATA
            Dim manufacturer As New StringBuilder
            Dim manufacturerId As New StringBuilder
            Dim description As New StringBuilder
            Dim serialNumber As New StringBuilder
            Dim r As Integer = FT_EE_ReadEx(ftHandle, pData, manufacturer, manufacturerId, description, serialNumber)
            CheckStatus(r)

            'TEST 
            pData.Manufacturer = manufacturer.ToString()
            pData.ManufacturerId = manufacturerId.ToString()
            pData.Description = description.ToString()
            pData.SerialNumber = serialNumber.ToString()

            Return pData
        End Function

        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_EE_UASize(ftHandle As Integer, ByRef lpdwSize As Integer) As Integer
        End Function

        ''' <summary>
        ''' Получает доступный размер доступной пользователю EEPROM, в байтах. Эта функция д/б вызван прежде вызова EeUAWrite() или EeUARead().
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        Public Shared Function GetEepromUserAreaSize(ftHandle As Integer) As Integer
            Dim lpdwSize As Integer = 0
            Dim r As Integer = FT_EE_UASize(ftHandle, lpdwSize)
            CheckStatus(r)
            Return lpdwSize
        End Function

        ''' <summary>
        ''' Возвращает размер свободной памяти в пользовательской области, в байтах.
        ''' </summary>
        ''' <remarks>
        ''' Свободное место вычисляется по формуле:
        ''' User Area Size = (48 – ((string)Manufacturer + (string)Description  + (string)ManufacturerId + (string)SerialNumber)) * 2 (in bytes).
        ''' </remarks>
        Public Shared Function GetEepromFreeUserAreaSize(ftHandle As Integer) As Integer
            Dim pd As PFT_PROGRAM_DATA = EepromRead(ftHandle)
            Dim freeUaSize As Integer = (48 - pd.Manufacturer.Length - pd.Description.Length - pd.ManufacturerId.Length - pd.SerialNumber.Length) * 2
            Return freeUaSize
        End Function

        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_EE_UAWrite(ftHandle As Integer, <MarshalAs(UnmanagedType.LPStr)> pucData As String, dwDataLen As Integer) As Integer
        End Function

        ''' <summary>
        ''' Запись строки в пользовательскую оболасть памяти EEPROM.
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        ''' <param name="value">Строка для записи.</param>
        Public Shared Sub EepromUserAreaWrite(ByVal ftHandle As Integer, ByVal value As String)
            Dim r As Integer = FT_EE_UAWrite(ftHandle, value, value.Length)
            CheckStatus(r)
        End Sub

        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_EE_UARead(ftHandle As Integer, <MarshalAs(UnmanagedType.LPStr)> pucData As StringBuilder,
                                             dwDataLen As Integer, ByRef lpdwBytesRead As Integer) As Integer
        End Function

        ''' <summary>
        ''' Читает содержимое пользовательской области EEPROM.
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        ''' <param name="dataLength">Длина данных для чтения.</param>
        Public Shared Function EepromUserAreaRead(ftHandle As Integer, dataLength As Integer) As String
            Dim lpdwBytesRead As Integer = 0
            Dim sb As New StringBuilder(dataLength)
            Dim r As Integer = FT_EE_UARead(ftHandle, sb, dataLength, lpdwBytesRead)
            CheckStatus(r)
            Return sb.ToString()
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        ''' <param name="dwWordOffset"></param>
        ''' <param name="lpwValue"></param>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_ReadEE(ftHandle As Integer, dwWordOffset As Integer, ByRef lpwValue As Char) As Integer
        End Function

        ''' <summary>
        ''' Читает значение из EEPROM по заданному смещению.
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        ''' <param name="offset">Смещение EEPROM.</param>
        ''' <remarks>
        ''' The start address of the user area will also vary depending on the size of the above strings used. It can be calculated As follows:
        ''' Start Address = the address following the last Byte Of SerialNumber String.
        ''' </remarks>
        Public Shared Function ReadEeprom(ftHandle As Integer, offset As Integer) As Char
            Dim c As Char
            Dim r As Integer = FT_ReadEE(ftHandle, offset, c)
            CheckStatus(r)
            Return c
        End Function

        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_WriteEE(ftHandle As Integer, dwWordOffset As Integer, wValue As Short) As Integer
        End Function

        ''' <summary>
        ''' Записывает значение в EEPROM по заданному смещению.
        ''' </summary>
        ''' <param name="ftHandle">Дескриптор устройства.</param>
        ''' <param name="offset">Смещение.</param>
        ''' <param name="value">Значение для записи в EEPROM.</param>
        Public Shared Sub WriteEeprom(ftHandle As Integer, offset As Integer, value As Short)
            Dim r As Integer = FT_WriteEE(ftHandle, offset, value)
            CheckStatus(r)
        End Sub

#Region "СТРУКТУРЫ EEPROM"

        ''' <summary>
        ''' Structure to hold program data for FT_EE_Program, FT_EE_ProgramEx, FT_EE_Read and FT_EE_ReadEx functions.
        ''' </summary>
        <StructLayout(LayoutKind.Sequential)>
        Public Structure PFT_PROGRAM_DATA
            Public Signature1 As Integer
            Public Signature2 As Integer
            Public Version As Integer
            Public VendorId As Short
            Public ProductId As Short
            <MarshalAs(UnmanagedType.LPStr)> Public Manufacturer As String
            <MarshalAs(UnmanagedType.LPStr)> Public ManufacturerId As String
            <MarshalAs(UnmanagedType.LPStr)> Public Description As String
            <MarshalAs(UnmanagedType.LPStr)> Public SerialNumber As String
            Public MaxPower As Short
            Public PnP As Short
            Public SelfPowered As Short
            Public RemoteWakeup As Short
            Public Rev4 As Byte
            Public IsoIn As Byte
            Public IsoOut As Byte
            Public PullDownEnable As Byte
            Public SerNumEnable As Byte
            Public USBVersionEnable As Byte
            Public USBVersion As Short
            Public Rev5 As Byte
            Public IsoInA As Byte
            Public IsoInB As Byte
            Public IsoOutA As Byte
            Public IsoOutB As Byte
            Public PullDownEnable5 As Byte
            Public SerNumEnable5 As Byte
            Public USBVersionEnable5 As Byte
            Public USBVersion5 As Short
            Public AIsHighCurrent As Byte
            Public BIsHighCurrent As Byte
            Public IFAIsFifo As Byte
            Public IFAIsFifoTar As Byte
            Public IFAIsFastSer As Byte
            Public AIsVCP As Byte
            Public IFBIsFifo As Byte
            Public IFBIsFifoTar As Byte
            Public IFBIsFastSer As Byte
            Public BIsVCP As Byte
            Public UseExtOsc As Byte
            Public HighDriveIOs As Byte
            Public EndpointSize As Byte
            Public PullDownEnableR As Byte
            Public SerNumEnableR As Byte
            Public InvertTXD As Byte
            Public InvertRXD As Byte
            Public InvertRTS As Byte
            Public InvertCTS As Byte
            Public InvertDTR As Byte
            Public InvertDSR As Byte
            Public InvertDCD As Byte
            Public InvertRI As Byte
            Public Cbus0 As Byte
            Public Cbus1 As Byte
            Public Cbus2 As Byte
            Public Cbus3 As Byte
            Public Cbus4 As Byte
            Public RIsD2XX As Byte
            Public PullDownEnable7 As Byte
            Public SerNumEnable7 As Byte
            Public ALSlowSlew As Byte
            Public ALSchmittInput As Byte
            Public ALDriveCurrent As Byte
            Public AHSlowSlew As Byte
            Public AHSchmittInput As Byte
            Public AHDriveCurrent As Byte
            Public BLSlowSlew As Byte
            Public BLSchmittInput As Byte
            Public BLDriveCurrent As Byte
            Public BHSlowSlew As Byte
            Public BHSchmittInput As Byte
            Public BHDriveCurrent As Byte
            Public IFAIsFifo7 As Byte
            Public IFAIsFifoTar7 As Byte
            Public IFAIsFastSer7 As Byte
            Public AIsVCP7 As Byte
            Public IFBIsFifo7 As Byte
            Public IFBIsFifoTar7 As Byte
            Public IFBIsFastSer7 As Byte
            Public BIsVCP7 As Byte
            Public PowerSaveEnable As Byte
            Public PullDownEnable8 As Byte
            Public SerNumEnable8 As Byte
            Public ASlowSlew As Byte
            Public ASchmittInput As Byte
            Public ADriveCurrent As Byte
            Public BSlowSlew As Byte
            Public BSchmittInput As Byte
            Public BDriveCurrent As Byte
            Public CSlowSlew As Byte
            Public CSchmittInput As Byte
            Public CDriveCurrent As Byte
            Public DSlowSlew As Byte
            Public DSchmittInput As Byte
            Public DDriveCurrent As Byte
            Public ARIIsTXDEN As Byte
            Public BRIIsTXDEN As Byte
            Public CRIIsTXDEN As Byte
            Public DRIIsTXDEN As Byte
            Public AIsVCP8 As Byte
            Public BIsVCP8 As Byte
            Public CIsVCP8 As Byte
            Public DIsVCP8 As Byte
            Public PullDownEnableH As Byte
            Public SerNumEnableH As Byte
            Public ACSlowSlewH As Byte
            Public ACSchmittInputH As Byte
            Public ACDriveCurrentH As Byte
            Public ADSlowSlewH As Byte
            Public ADSchmittInputH As Byte
            Public ADDriveCurrentH As Byte
            Public Cbus0H As Byte
            Public Cbus1H As Byte
            Public Cbus2H As Byte
            Public Cbus3H As Byte
            Public Cbus4H As Byte
            Public Cbus5H As Byte
            Public Cbus6H As Byte
            Public Cbus7H As Byte
            Public Cbus8H As Byte
            Public Cbus9H As Byte
            Public IsFifoH As Byte
            Public IsFifoTarH As Byte
            Public IsFastSerH As Byte
            Public IsFT1248H As Byte
            Public FT1248CpolH As Byte
            Public FT1248LsbH As Byte
            Public FT1248FlowControlH As Byte
            Public IsVCPH As Byte
            Public PowerSaveEnableH As Byte
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Public Structure FT_EEPROM_HEADER
            Public deviceType As Integer  'FTxxxx device type to be programmed
            'Device descriptor options
            Public VendorId As Short      '0x0403
            Public ProductId As Short     '0x6001
            Public SerNumEnable As Byte   'non-zero if serial number to be used
            'Config descriptor options
            Public MaxPower As Short      '0 < MaxPower <= 500
            Public SelfPowered As Byte    '0 = bus powered, 1 = self powered
            Public RemoteWakeup As Byte   '0 = not capable, 1 = capable
            'Hardware options
            Public PullDownEnable As Byte 'non-zero if pull down in suspend enabled
        End Structure

        ''' <summary>
        ''' FT232B EEPROM structure for use with FT_EEPROM_Read and FT_EEPROM_Program
        ''' </summary>
        <StructLayout(LayoutKind.Sequential)>
        Public Structure FT_EEPROM_232B
            Public common As FT_EEPROM_HEADER 'common elements for all device EEPROMs
        End Structure

        ''' <summary>
        ''' FT2232H EEPROM structure for use with FT_EEPROM_Read and FT_EEPROM_Program
        ''' </summary>
        <StructLayout(LayoutKind.Sequential)>
        Public Structure FT_EEPROM_2232H
            Public common As FT_EEPROM_HEADER  'common elements for all device EEPROMs
            'Drive options
            Public ALSlowSlew As Byte          ' non-zero if AL pins have slow slew
            Public ALSchmittInput As Byte      ' non-zero if AL pins are Schmitt input
            Public ALDriveCurrent As Byte      ' valid values are 4mA, 8mA, 12mA, 16mA
            Public AHSlowSlew As Byte          ' non-zero if AH pins have slow slew
            Public AHSchmittInput As Byte      ' non-zero if AH pins are Schmitt input
            Public AHDriveCurrent As Byte      ' valid values are 4mA, 8mA, 12mA, 16mA
            Public BLSlowSlew As Byte          ' non-zero if BL pins have slow slew
            Public BLSchmittInput As Byte      ' non-zero if BL pins are Schmitt input
            Public BLDriveCurrent As Byte      ' valid values are 4mA, 8mA, 12mA, 16mA
            Public BHSlowSlew As Byte          ' non-zero if BH pins have slow slew
            Public BHSchmittInput As Byte      ' non-zero if BH pins are Schmitt input
            Public BHDriveCurrent As Byte      ' valid values are 4mA, 8mA, 12mA, 16mA
            'Hardware options
            Public AIsFifo As Byte             ' non-zero if interface is 245 FIFO
            Public AIsFifoTar As Byte          ' non-zero if interface is 245 FIFO CPU target
            Public AIsFastSer As Byte          ' non-zero if interface is Fast serial
            Public BIsFifo As Byte             ' non-zero if interface is 245 FIFO
            Public BIsFifoTar As Byte          ' non-zero if interface is 245 FIFO CPU target
            Public BIsFastSer As Byte          ' non-zero if interface is Fast serial
            Public PowerSaveEnable As Byte     ' non-zero if using BCBUS7 to save power for self-powered designs
            'Driver option
            Public ADriverType As Byte         '
            Public BDriverType As Byte         '
        End Structure

#End Region '/СТРУКТУРЫ EEPROM

#End Region 'РАБОТА С EEPROM

#Region "ЧТЕНИЕ И ЗАПИСЬ"

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="ftHandle"></param>
        ''' <param name="lpBuffer">Pointer to the buffer that receives the data from the device.</param>
        ''' <param name="dwBytesToRead">Number of bytes to be read from the device.</param>
        ''' <param name="lpBytesReturned">Pointer to a variable of type DWORD which receives the number of bytes read from the device.</param>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_Read(ftHandle As Integer, lpBuffer As Byte(), dwBytesToRead As Integer, ByRef lpBytesReturned As Integer) As Integer
        End Function

        ''' <summary>
        ''' Read data from the device.
        ''' </summary>
        ''' <param name="ftHandle">Handle of the device.</param>
        ''' <param name="bytesToRead">Number of bytes to be read from the device.</param>
        Public Shared Function FtRead(ftHandle As Integer, bytesToRead As Integer) As Byte()
            Dim b(bytesToRead - 1) As Byte
            Dim bytesReturned As Integer = 0
            Dim r As Integer = FT_Read(ftHandle, b, bytesToRead, bytesReturned)
            CheckStatus(r)
            Return b
        End Function

        ''' <summary>
        ''' Write data to the device.
        ''' </summary>
        ''' <param name="ftHandle"></param>
        ''' <param name="lpBuffer"></param>
        ''' <param name="dwBytesToWrite"></param>
        ''' <param name="lpBytesWritten"></param>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_Write(ftHandle As Integer, lpBuffer() As Byte, dwBytesToWrite As Integer, ByRef lpBytesWritten As Integer) As Integer
        End Function

        ''' <summary>
        ''' Записывает данные в устройство и возвращает количество записанных данных.
        ''' </summary>
        ''' <param name="ftHandle"></param>
        ''' <param name="lpBuffer"></param>
        Public Shared Function FtWrite(ftHandle As Integer, lpBuffer() As Byte) As Integer
            'Public Shared Sub FtWrite(ByVal ftHandle As Integer, ByVal lpBuffer() As Byte, ByVal dwBytesToWrite As Integer, ByRef lpBytesWritten As Integer)
            'Dim r As Integer = FT_Write(ftHandle, lpBuffer, dwBytesToWrite, lpBytesWritten)
            Dim lpBytesWritten As Integer = 0
            Dim r As Integer = FT_Write(ftHandle, lpBuffer, lpBuffer.Length, lpBytesWritten)
            CheckStatus(r)
            Return lpBytesWritten
        End Function

        ''' <summary>
        ''' Gets the number of bytes in the receive queue.
        ''' </summary>
        ''' <param name="ftHandle">Handle of the device.</param>
        ''' <param name="dwRxBytes">Pointer to a variable of type DWORD which receives the number of bytes in the receive queue.</param>
        ''' <returns>FT_OK if successful, otherwise the return value is an FT error code.</returns>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_GetQueueStatus(ftHandle As Integer, ByRef dwRxBytes As Integer) As Integer
        End Function

        ''' <summary>
        ''' Gets the number of bytes in the receive queue.
        ''' </summary>
        ''' <param name="ftHandle">Handle of the device.</param>
        ''' <returns>Pointer to a variable of type DWORD which receives the number of bytes in the receive queue.</returns>
        Public Shared Function GetQueueStatus(ftHandle As Integer) As Integer
            Dim dwRxBytes As Integer = 0 'Pointer to a variable of type DWORD which receives the number of bytes in the receive queue.
            Dim r As Integer = FT_GetQueueStatus(ftHandle, dwRxBytes)
            CheckStatus(r)
            Return dwRxBytes
        End Function

        ''' <summary>
        ''' Gets the device status including number of characters in the receive queue, number of characters in the transmit queue, and the current event status.
        ''' </summary>
        ''' <param name="ftHandle">Handle of the device.</param>
        ''' <param name="dwRxBytes">Pointer to a variable of type DWORD which receives the number of characters in the receive queue.</param>
        ''' <param name="dwTxBytes">Pointer to a variable of type DWORD which receives the number of characters in the transmit queue.</param>
        ''' <param name="dwEventDWord">Pointer to a variable of type DWORD which receives the current state of the event status.</param>
        ''' <returns>FT_OK if successful, otherwise the return value is an FT error code.</returns>
        <DllImport(DllFtd2xxPath, SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function FT_GetStatus(ftHandle As Integer, ByRef dwRxBytes As Integer, ByRef dwTxBytes As Integer, ByRef dwEventDWord As Integer) As Integer
        End Function

        ''' <summary>
        ''' Gets the device status including number of characters in the receive queue, 
        ''' number of characters in the transmit queue, and the current event status.
        ''' </summary>
        ''' <param name="ftHandle">Handle of the device.</param>
        Public Shared Function FtGetStatus(ftHandle As Integer) As FtStatus
            Dim stat As New FtStatus
            Dim r As Integer = FT_GetStatus(ftHandle, stat.RxBytes, stat.TxBytes, stat.EventDWord)
            CheckStatus(r)
            Return stat
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        Public Structure FtStatus

            ''' <summary>
            ''' Pointer to a variable of type DWORD which receives the number of characters in the receive queue.
            ''' </summary>
            Public Property RxBytes As Integer

            ''' <summary>
            ''' Pointer to a variable of type DWORD which receives the number of characters in the transmit queue.
            ''' </summary>
            Public Property TxBytes As Integer

            ''' <summary>
            ''' Pointer to a variable of type DWORD which receives the current state of the event status.
            ''' </summary>
            Public Property EventDWord As Integer

        End Structure

#End Region '/ЧТЕНИЕ И ЗАПИСЬ

    End Class '/Ftdi.Ftd2xx

End Namespace
