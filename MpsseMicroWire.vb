Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq

Namespace Ftdi.MicroWire

    ''' <summary>
    ''' Реализует интерфейс MicroWire.
    ''' </summary>
    Public Class MpsseMicroWire
        Inherits ContainsMpsseBase

#Region "EVENTS"

        Public Event AddressBitnessChanged(bitness As Integer)
        Public Event MemOrganizationChanged(memOrg As Organization)

#End Region '/EVENTS

#Region "CTOR"

        ''' <summary>
        ''' Подключается к устройству с заданным индексом в режиме MicroWire.
        ''' </summary>
        ''' <param name="index">Индекс устройства в системе, начиная с 0.</param>
        ''' <param name="addrBitness">Разрядность адреса. Зависит от типа устройства и организации памяти.</param>
        ''' <param name="org">Тип организации памяти.</param>
        Public Sub New(index As Integer, addrBitness As Integer, org As Organization)
            MyBase.New(index, False) 'сразу не открываем
            Me.AddressBitness = addrBitness
            Me.MemOrganization = org
            CreateCommands()
            AddHandler Spi.ConnectionStateChanged, Sub() ConfigInternalSpi()
        End Sub

        Private Sub CreateCommands()
            Commands.Add(Instructions.READ, New ReadCommand(AddressBitness, MemOrganization))
            Commands.Add(Instructions.WEN, New WenCommand(AddressBitness, MemOrganization))
            Commands.Add(Instructions.WDS, New WdsCommand(AddressBitness, MemOrganization))
            Commands.Add(Instructions.WRITE, New WriteCommand(AddressBitness, MemOrganization))
            Commands.Add(Instructions.ERASE, New EraseCommand(AddressBitness, MemOrganization))
            Commands.Add(Instructions.ERAL, New EraseAllCommand(AddressBitness, MemOrganization))
            Commands.Add(Instructions.WRAL, New WriteAllCommand(AddressBitness, MemOrganization))
            For Each kvp In Commands
                AddHandler AddressBitnessChanged, AddressOf kvp.Value.AddressBitnessChangedHandler
                AddHandler MemOrganizationChanged, AddressOf kvp.Value.MemOrganizationChangedHandler
            Next
        End Sub

#End Region '/CTOR

#Region "PROPS"

        ''' <summary>
        ''' Конфигурация SPI устройства.
        ''' </summary>
        Private Conf As New Spi.SpiConfig() With {
            .LatencyTimer = 8,
            .ClockRate = 100_000,
            .Pin = 0UI,
            .ConfigOptions = Ftdi.Spi.SPI_CONFIG_OPTION.MODE0 '+Active_HIGH по умолчанию
        }

        Public Overrides ReadOnly Property DeviceMode As DeviceWorkingMode
            Get
                Return DeviceWorkingMode.MicroWire
            End Get
        End Property

        ''' <summary>
        ''' Список команд.
        ''' </summary>
        Public ReadOnly Property Commands As New Dictionary(Of Instructions, MicroWireCommandBase)

        ''' <summary>
        ''' Разрядность адреса. Для каждого устройства постоянна.
        ''' </summary>
        Public Property AddressBitness As Integer
            Get
                Return _AddressBitness
            End Get
            Set(value As Integer)
                If (_AddressBitness <> value) Then
                    _AddressBitness = value
                    RaiseEvent AddressBitnessChanged(value)
                End If
            End Set
        End Property
        Private _AddressBitness As Integer = 9

        ''' <summary>
        ''' Максимальный адрес при данной разрядности адреса.
        ''' </summary>
        Public ReadOnly Property MaxAddress As Integer
            Get
                Return CInt(Math.Pow(2, AddressBitness))
            End Get
        End Property

        ''' <summary>
        ''' Тип организации памяти.
        ''' </summary>
        ''' <remarks>
        ''' Может меняться во время работы, но это скорее исключение, поэтому Read-only.
        ''' </remarks>
        Public Property MemOrganization As Organization
            Get
                Return _MemOrganization
            End Get
            Set(value As Organization)
                If (_MemOrganization <> value) Then
                    _MemOrganization = value
                    RaiseEvent MemOrganizationChanged(value)
                End If
            End Set
        End Property
        Private _MemOrganization As Organization = Organization.Bytes

        Public Overridable Property ClockRate As Integer
            Get
                Return Conf.ClockRate
            End Get
            Set(value As Integer)
                If (Conf.ClockRate <> value) Then
                    Conf.ClockRate = value
                    ConfigInternalSpi()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Номер вывода CS (0..5).
        ''' </summary>
        Public Property xDBUS As Spi.CsEnum
            Get
                Return CType(((Conf.ConfigOptions >> 2) And &H7), Spi.CsEnum)
            End Get
            Set(value As Spi.CsEnum)
                If (((Conf.ConfigOptions >> 2) And &H7) <> value) Then
                    Conf.ConfigOptions = CType((Conf.ConfigOptions And &HFFFFFFE3UI) Or (value << 2), Spi.SPI_CONFIG_OPTION)
                    'Spi.InitChannel(Conf)
                    Spi.ChangeCS(Conf.ConfigOptions)
                End If
            End Set
        End Property

        ''' <summary>
        ''' Память заблокирована для записи/стирания. Изменяется при отправке команд <see cref="WenCommand"/> и <see cref="WdsCommand"/>.
        ''' </summary>
        Public Overridable Property MemoryLocked As Boolean
            Get
                Return _MemoryLocked
            End Get
            Protected Set(value As Boolean)
                _MemoryLocked = value
            End Set
        End Property
        Private _MemoryLocked As Boolean = True

        ''' <summary>
        ''' Коррекция данных при чтении.
        ''' </summary>
        ''' <remarks>
        ''' Некоторые м/сх, в частности 93C66, требуют до выбора ведомого 1 импульс тактовой частоты. 
        ''' FTDI так не умеет, поэтому добавляется 1 лишний тактовый импульс после адреса, и это работает.
        ''' Если <see langword="True"/>, при чтении добавляет 1 "лишний" тактовый импульс для корректировки значения.
        ''' </remarks>
        Public Property CorrectRead As Boolean
            Get
                Return CType(Commands(Instructions.READ), ReadCommand).CorrectRead
            End Get
            Set(value As Boolean)
                CType(Commands(Instructions.READ), ReadCommand).CorrectRead = value
            End Set
        End Property

#End Region '/PROPS

#Region "METHODS"

#Region "READ"

        ''' <summary>
        ''' Отправляет команду READ (чтение данных из памяти) и возвращает прочитанное значение (или NULL).
        ''' </summary>
        Public Function SendRead(cmd As ReadCommand) As Integer?
            Debug.WriteLine(cmd.ToString())
            Dim read As Integer? = WriteCommandBits(cmd, If(cmd.Organization = Organization.Bytes, 8, 16))
            Commands(Instructions.READ).Data = read
            Return read
        End Function

        ''' <summary>
        ''' Отправляет команду <see cref="Commands(Instructions.READ)"/> (чтение данных из памяти) и возвращает прочитанное значение (или NULL).
        ''' </summary>
        Public Function SendRead() As Integer?
            Dim cmd As ReadCommand = CType(Commands(Instructions.READ), ReadCommand)
            Return SendRead(cmd)
        End Function

        ''' <summary>
        ''' Отправляет команду READ (чтение данных из ячейки памяти) и возвращает прочитанное значение (или NULL).
        ''' </summary>
        Public Function SendRead(address As Integer) As Integer?
            Dim cmd As New ReadCommand(AddressBitness, MemOrganization) With {
                .Address = address,
                .CorrectRead = CorrectRead
            }
            Return SendRead(cmd)
        End Function

#End Region '/READ

#Region "WRITE"

        ''' <summary>
        ''' Отправляет команду WRITE (запись данных в ячейку памяти).
        ''' </summary>
        Public Sub SendWrite(cmd As WriteCommand)
            Debug.WriteLine(cmd.ToString())
            WriteCommandBits(cmd)
            CheckReadyBusyStatus()
        End Sub

        ''' <summary>
        ''' Отправляет команду WRITE (запись данных в ячейку памяти).
        ''' </summary>
        Public Sub SendWrite(address As Integer, addressBitness As Integer, data As Integer, org As Organization)
            Dim cmd As New WriteCommand(address, addressBitness, data, org)
            SendWrite(cmd)
        End Sub

        ''' <summary>
        ''' Отправляет команду <see cref="Commands(Instructions.WRITE)"/> (запись данных в ячейку памяти).
        ''' </summary>
        Public Sub SendWrite()
            Dim cmd As WriteCommand = CType(Commands(Instructions.WRITE), WriteCommand)
            SendWrite(cmd)
        End Sub

#End Region '/WRITE

#Region "WRAL"

        ''' <summary>
        ''' Отправляет команду WRAL (запись данных во всю память).
        ''' </summary>
        Public Sub SendWriteAll(cmd As WriteAllCommand)
            Debug.WriteLine(cmd.ToString())
            WriteCommandBits(cmd)
            CheckReadyBusyStatus()
        End Sub

        ''' <summary>
        ''' Отправляет команду WRAL (запись данных во всю память).
        ''' </summary>
        Public Sub SendWriteAll(data As Integer)
            Dim cmd As New WriteAllCommand(AddressBitness, MemOrganization, data)
            SendWriteAll(cmd)
        End Sub

        ''' <summary>
        ''' Отправляет команду <see cref="Commands(Instructions.WRAL)"/> (запись данных во всю память).
        ''' </summary>
        Public Sub SendWriteAll()
            Dim cmd As WriteAllCommand = CType(Commands(Instructions.WRAL), WriteAllCommand)
            SendWriteAll(cmd)
        End Sub

#End Region '/WRAL

#Region "ERASE"

        ''' <summary>
        ''' Отправляет команду ERASE (erase - стереть байт/слово).
        ''' </summary>
        Public Sub SendErase(cmd As EraseCommand)
            Debug.WriteLine(cmd.ToString())
            WriteCommandBits(cmd)
            CheckReadyBusyStatus()
        End Sub

        ''' <summary>
        ''' Отправляет команду ERASE (erase - стереть байт/слово).
        ''' </summary>
        ''' <param name="address">Адрес стирания.</param>
        Public Sub SendErase(address As Integer)
            Dim c As New EraseCommand(address, Me.AddressBitness, MemOrganization)
            SendErase(c)
        End Sub

        ''' <summary>
        ''' Отправляет команду <see cref="Commands(Instructions.ERASE)"/> (erase - стереть байт/слово).
        ''' </summary>
        Public Sub SendErase()
            Dim c As EraseCommand = CType(Commands(Instructions.ERASE), EraseCommand)
            SendErase(c)
        End Sub

#End Region '/ERASE

#Region "ERAL"

        ''' <summary>
        ''' Отправляет команду ERAL (erase all memory - стереть всю память).
        ''' </summary>
        Public Sub SendEraseAll(cmd As EraseAllCommand)
            Debug.WriteLine(cmd.ToString())
            WriteCommandBits(cmd)
            CheckReadyBusyStatus()
        End Sub

        ''' <summary>
        ''' Отправляет команду <see cref="Commands(Instructions.ERAL)"/> (erase all memory - стереть всю память).
        ''' </summary>
        Public Sub SendEraseAll()
            Dim cmd As EraseAllCommand = CType(Commands(Instructions.ERAL), EraseAllCommand)
            SendEraseAll(cmd)
        End Sub

#End Region '/ERAL

#Region "WEN"

        ''' <summary>
        ''' Отправляет команду WEN (write enable - разрешение записи/стирания).
        ''' </summary>
        Public Sub SendWriteEnable(cmd As WenCommand)
            Debug.WriteLine(cmd.ToString())
            WriteCommandBits(cmd)
            MemoryLocked = False
            'RaiseEvent SentWriteEnabled(True)
        End Sub

        ''' <summary>
        ''' Отправляет команду <see cref="Commands(Instructions.WEN)"/> (write enable - разрешение записи) или WDS (write disable - запрет записи).
        ''' </summary>
        Public Sub SendWriteEnable()
            Dim cmd As WenCommand = CType(Commands(Instructions.WEN), WenCommand)
            SendWriteEnable(cmd)
        End Sub

#End Region '/WEN

#Region "WDS"

        ''' <summary>
        ''' Отправляет команду WDS (write disable - запрет записи/стирания).
        ''' </summary>
        Public Sub SendWriteDisable(cmd As WdsCommand)
            Debug.WriteLine(cmd.ToString())
            WriteCommandBits(cmd)
            MemoryLocked = True
            'RaiseEvent SentWriteEnabled(False)
        End Sub

        Public Sub SendWriteDisable()
            Dim cmd As WdsCommand = CType(Commands(Instructions.WDS), WdsCommand)
            SendWriteDisable(cmd)
        End Sub

#End Region '/WDS

        ''' <summary>
        ''' Проверяет, закончена ли операция стирания или записи.
        ''' </summary>
        ''' <remarks>
        ''' Если при активном CS на выводе DI HIGH, значит операция завершена. До завершения операции все остальные команды непринимаются.
        ''' </remarks>
        Private Function CheckReadyBusyStatus() As Boolean
            Dim success As Boolean = False
            Dim cnt As Integer = 0
            Do
                Dim r As Byte() = Spi.Read(1, Ftdi.Spi.SPI_TRANSFER_OPTIONS.CHIPSELECT_ENABLE Or Ftdi.Spi.SPI_TRANSFER_OPTIONS.CHIPSELECT_DISABLE)
                For Each b As Byte In r
                    Debug.WriteLine(b)
                    If (b > 0) Then
                        success = True
                        Exit Do
                    End If
                Next
                cnt += 1
                Threading.Thread.Sleep(1)
            Loop Until success OrElse (cnt > 10)
            Return success
        End Function

        ''' <summary>
        ''' Конфигурирует SPI с параметрами, необходимыми для чтения или записи.
        ''' </summary>
        Private Sub ConfigInternalSpi()
            Dim cfg = Spi.InitChannel(Conf)
            Debug.WriteLine($"pin = {cfg.Pin:X}")
            'Conf.Pin = cfg.Pin
        End Sub

        ''' <summary>
        ''' Отправляет массив битов команды <paramref name="cmd"/> устройству.
        ''' </summary>
        ''' <param name="cmd">Команда.</param>
        ''' <param name="readBitsLen">Число битов для чтения (только в команде типа READ).</param>
        Private Function WriteCommandBits(cmd As MicroWireCommandBase, Optional readBitsLen As Integer = 0) As Integer?
            Dim sendBits As IEnumerable(Of Boolean) = cmd.GetBits()
            Dim cmdBytes As Byte() = GetBytes(sendBits)
            Dim r As Byte() = Spi.ReadWriteBits(cmdBytes, sendBits.Count - readBitsLen, readBitsLen)
            If (readBitsLen > 0) Then
                Dim res As Integer = 0
                For i As Integer = 0 To r.Length - 1
                    res <<= (8 * i)
                    res += r(i)
                Next
                Return res
                'Return (res << 1) 'TEST почему-то на 1 бит сдвинут
            End If
            Return Nothing
        End Function

#End Region '/METHODS

#Region "HELPERS"

        ''' <summary>
        ''' Преобразует битовый массив в массив байтов.
        ''' </summary>
        ''' <param name="bits"></param>
        ''' <remarks>
        ''' Если число бит не кратно 8-ми, последний байт дополняется нулями.
        ''' </remarks>
        Private Shared Function GetBytes(bits As IEnumerable(Of Boolean)) As Byte()
            Dim len As Integer = CInt(Math.Ceiling(bits.Count / 8))
            Dim bytes(len - 1) As Byte
            For byteIndex As Integer = 0 To len - 1
                For bitIndex As Integer = 0 To 7
                    Dim throughIndex As Integer = (byteIndex * 8) + bitIndex
                    If (throughIndex < bits.Count) Then
                        Dim bit As Integer = CInt(bits(throughIndex)) And 1
                        'Debug.WriteLine($"Сквозной индекс = {throughIndex}, очередной бит = {bit <> 0}")
                        bytes(byteIndex) = bytes(byteIndex) Or CByte(bit << (7 - bitIndex))
                    End If
                Next
            Next
            Return bytes
        End Function

#End Region '/HELPERS

    End Class '/MpsseMicroWire

#Region "ENUMS"

    ''' <summary>
    ''' Тип организации памяти.
    ''' </summary>
    Public Enum Organization As Integer
        Bytes = 8
        Words = 16
    End Enum

    Public Enum Instructions As Integer
        READ
        WRITE
        WEN
        WDS
        [ERASE]
        ERAL
        WRAL
    End Enum

#End Region '/ENUMS

#Region "COMMANDS TYPES"

    ''' <summary>
    ''' Базовый класс команд uWire.
    ''' </summary>
    Public MustInherit Class MicroWireCommandBase
        Implements INotifyPropertyChanged

#Region "CTOR"

        Protected Sub New(addrBitness As Integer, org As Organization)
            Me.AddressBitness = addrBitness
            Me.Organization = org
        End Sub

#End Region '/CTOR

#Region "PROPS"

        Public MustOverride ReadOnly Property Instruction As Instructions

        Public Property AddressBitness As Integer
            Get
                Return _AddressBitness
            End Get
            Set(value As Integer)
                If (_AddressBitness <> value) Then
                    _AddressBitness = value
                    NotifyPropertyChanged(NameOf(AddressBitness))
                    NotifyPropertyChanged(NameOf(MaxAddress))
                End If
            End Set
        End Property
        Private _AddressBitness As Integer = 9

        ''' <summary>
        ''' Максимальный адрес при данной разрядности адреса.
        ''' </summary>
        Public ReadOnly Property MaxAddress As Integer
            Get
                Return CInt(Math.Pow(2, AddressBitness))
            End Get
        End Property

        Public Property StartBit As New List(Of Boolean)({True})

        Public MustOverride Property Opcode As List(Of Boolean)

        Public Property Organization As Organization
            Get
                Return _Organization
            End Get
            Set(value As Organization)
                If (_Organization <> value) Then
                    _Organization = value
                    NotifyPropertyChanged(NameOf(Organization))
                End If
            End Set
        End Property
        Private _Organization As Organization = Organization.Bytes

        Public Overridable Property Address As Integer
            Get
                Return _Address
            End Get
            Set(value As Integer)
                If (_Address <> value) Then
                    _Address = value
                    NotifyPropertyChanged(NameOf(Address))
                End If
            End Set
        End Property
        Private _Address As Integer = 0

        Public Property Data As Integer?
            Get
                Return _Data
            End Get
            Set(value As Integer?)
                _Data = value
                NotifyPropertyChanged(NameOf(Data))
            End Set
        End Property
        Private _Data As Integer? = Nothing

#End Region '/PROPS

#Region "METHDOS"

        Friend Sub AddressBitnessChangedHandler(bitness As Integer)
            Debug.WriteLine($"Поменялась разрядность: {bitness}")
            AddressBitness = bitness

            Select Case Instruction
                Case Instructions.WEN
                    Address = &B11 << (AddressBitness - 2)

                Case Instructions.WDS
                    Address = &B0 << (AddressBitness - 2)

                Case Instructions.ERAL
                    Address = &B10 << (AddressBitness - 2)

                Case Instructions.WRAL
                    Address = &B1 << (AddressBitness - 2)
            End Select
        End Sub

        Friend Sub MemOrganizationChangedHandler(memOrg As Organization)
            Debug.WriteLine($"Поменялась организация памяти: {memOrg}")
            Organization = memOrg
        End Sub

        Public Overloads Function ToString() As String
            Dim sb As New Text.StringBuilder()
            Dim bits As List(Of Boolean) = GetBits()
            For i As Integer = 0 To bits.Count - 1
                sb.Append(CInt(bits(i)) And 1)
                Select Case i
                    Case 0, 2, AddressBitness + 2
                        sb.Append("-")
                End Select
            Next
            If (sb(sb.Length - 1) = "-") Then
                sb = sb.Remove(sb.Length - 1, 1)
            End If
            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Возвращает массив битов данной команды.
        ''' </summary>
        Public Overridable Function GetBits() As List(Of Boolean)
            Dim bits As New List(Of Boolean)
            'bits.Add(False) 'TEST
            For Each b As Boolean In StartBit
                bits.Add(b)
            Next
            For Each b As Boolean In Opcode
                bits.Add(b)
            Next
            Dim addr As New BitArray({Address})
            For i As Integer = 0 To AddressBitness - 1
                Dim ind As Integer = AddressBitness - i - 1
                bits.Add(addr(ind))
            Next
            Return bits
        End Function

#End Region '/METHDOS

#Region "NOTIFY"

        Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged
        Private Sub NotifyPropertyChanged(propName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
        End Sub

#End Region '/NOTIFY

    End Class '/MicroWireCommandBase

    ''' <summary>
    ''' Команда READ - чтение данных из памяти.
    ''' </summary>
    Public Class ReadCommand
        Inherits MicroWireCommandBase

        Public Sub New(addrBitness As Integer, org As Organization)
            MyBase.New(addrBitness, org)
        End Sub

        Public Overrides ReadOnly Property Instruction As Instructions = Instructions.READ
        Public Overrides Property Opcode As New List(Of Boolean)({True, False})
        Public Property CorrectRead As Boolean = True

        Public Overrides Function GetBits() As List(Of Boolean)
            Dim bits As List(Of Boolean) = MyBase.GetBits()
            Dim dataLen As Integer = If(Organization = Organization.Bytes, 8, 16)
            Dim dataBits As Boolean() = New Boolean(dataLen - 1) {}
            If CorrectRead Then
                Return bits.Concat({False}).Concat(dataBits).ToList() 'HACK Из-за странной особенности м/сх при чтении выдаём на 1 бит больше.
            Else
                Return bits.Concat(dataBits).ToList()
            End If
        End Function

    End Class '/ReadCommand

    ''' <summary>
    ''' Команда WRITE - запись данных в ячейку памяти.
    ''' </summary>
    Public Class WriteCommand
        Inherits MicroWireCommandBase

        Public Sub New(addrBitness As Integer, org As Organization)
            MyBase.New(addrBitness, org)
            Data = 0
        End Sub

        Public Sub New(addr As Integer, addrBitness As Integer, data As Integer, org As Organization)
            MyBase.New(addrBitness, org)
            Me.Address = addr
            Me.Data = data
        End Sub

        Public Overrides ReadOnly Property Instruction As Instructions = Instructions.WRITE
        Public Overrides Property Opcode As New List(Of Boolean)({False, True})

        Public Overrides Function GetBits() As List(Of Boolean)
            Dim bits As List(Of Boolean) = MyBase.GetBits()
            If Data.HasValue Then
                Dim dataLen As Integer = If(Organization = Organization.Bytes, 8, 16)
                For i As Integer = 0 To dataLen - 1
                    Dim shift As Integer = dataLen - i - 1
                    bits.Add(CBool((Data >> shift) And 1)) 'TEST 
                Next
            End If
            Return bits
        End Function

    End Class '/WriteCommand

    ''' <summary>
    ''' Команда ERASE - стирание заданной ячеёки памяти.
    ''' </summary>
    Public Class EraseCommand
        Inherits MicroWireCommandBase

        Public Sub New(addrBitness As Integer, org As Organization)
            MyBase.New(addrBitness, org)
        End Sub

        Public Sub New(addr As Integer, addrBitness As Integer, org As Organization)
            MyBase.New(addrBitness, org)
            Me.Address = addr
        End Sub

        Public Overrides ReadOnly Property Instruction As Instructions = Instructions.ERASE
        Public Overrides Property Opcode As New List(Of Boolean)({True, True})

    End Class

    ''' <summary>
    ''' Команда WRAL - запись данных в память.
    ''' </summary>
    Public Class WriteAllCommand
        Inherits MicroWireCommandBase

        Public Sub New(addrBitness As Integer, org As Organization, data As Integer)
            MyBase.New(addrBitness, org)
            Me.Data = data
            Address = (&B1 << (AddressBitness - 2))
        End Sub

        Public Sub New(addrBitness As Integer, org As Organization)
            MyBase.New(addrBitness, org)
            Data = 0
            Address = (&B1 << (AddressBitness - 2))
        End Sub

        Public Overrides ReadOnly Property Instruction As Instructions = Instructions.WRAL
        Public Overrides Property Opcode As New List(Of Boolean)({False, False})

        Public Overrides Property Address As Integer
            Get
                Return MyBase.Address
            End Get
            Set(value As Integer)
                MyBase.Address = (&B1 << (AddressBitness - 2))
            End Set
        End Property

        Public Overrides Function GetBits() As List(Of Boolean)
            Dim bits As List(Of Boolean) = MyBase.GetBits()
            If Data.HasValue Then
                Dim dataLen As Integer = If(Organization = Organization.Bytes, 8, 16)
                For i As Integer = 0 To dataLen - 1
                    Dim shift As Integer = dataLen - i - 1
                    bits.Add(CBool((Data >> shift) And 1)) 'TEST 
                Next
            End If
            Return bits
        End Function

    End Class

    ''' <summary>
    ''' Команда WEN - разрешение записи/стирания.
    ''' </summary>
    Public Class WenCommand
        Inherits MicroWireCommandBase

        Public Sub New(addrBitness As Integer, org As Organization)
            MyBase.New(addrBitness, org)
            Address = (&B11 << (AddressBitness - 2))
        End Sub

        Public Overrides ReadOnly Property Instruction As Instructions = Instructions.WEN
        Public Overrides Property Opcode As New List(Of Boolean)({False, False})

        Public Overrides Property Address As Integer
            Get
                Return MyBase.Address
            End Get
            Set(value As Integer)
                MyBase.Address = (&B11 << (AddressBitness - 2))
            End Set
        End Property

    End Class

    ''' <summary>
    ''' Команда WDS - запрещение записи/стирания.
    ''' </summary>
    Public Class WdsCommand
        Inherits MicroWireCommandBase

        Public Sub New(addrBitness As Integer, org As Organization)
            MyBase.New(addrBitness, org)
            Address = (&B0 << (AddressBitness - 2))
        End Sub

        Public Overrides ReadOnly Property Instruction As Instructions = Instructions.WDS
        Public Overrides Property Opcode As New List(Of Boolean)({False, False})

        Public Overrides Property Address As Integer
            Get
                Return MyBase.Address
            End Get
            Set(value As Integer)
                MyBase.Address = (&B0 << (AddressBitness - 2))
            End Set
        End Property

    End Class

    ''' <summary>
    ''' Команда ERAL - стирание всей памяти.
    ''' </summary>
    Public Class EraseAllCommand
        Inherits MicroWireCommandBase

        Public Sub New(addrBitness As Integer, org As Organization)
            MyBase.New(addrBitness, org)
            Address = (&B10 << (AddressBitness - 2))
        End Sub

        Public Overrides ReadOnly Property Instruction As Instructions = Instructions.ERAL
        Public Overrides Property Opcode As New List(Of Boolean)({False, False})

        Public Overrides Property Address As Integer
            Get
                Return MyBase.Address
            End Get
            Set(value As Integer)
                MyBase.Address = (&B10 << (AddressBitness - 2))
            End Set
        End Property

    End Class

#End Region '/COMMANDS TYPES

End Namespace
