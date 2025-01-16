Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.InteropServices
Imports Ftdi.Spi

Namespace Ftdi.OneWire

    ''' <summary>
    ''' Симуляция интерфейса 1-Wire с помощью SPI.
    ''' </summary>
    Public Class MpsseOneWire
        Inherits ContainsMpsseBase

        Private Const ONEWIRE_READ As Byte = &H33
        Private Const ONEWIRE_SKIP As Byte = &HCC
        Private Const ONEWIRE_MATCH As Byte = &H55
        Private Const ONEWIRE_SEARCH As Byte = &HF0

        Private Const RESET_STANDARD_LEN As Double = 480
        Private Const RESET_OVER_LEN As Double = 70

#Region "CTOR"

        ''' <summary>
        ''' Подключается к устройству с индексом <paramref name="index"/> в скоростном режиме <paramref name="speed"/>.
        ''' </summary>
        Public Sub New(index As Integer, Optional speed As TimingModes = TimingModes.Standard)
            MyBase.New(index, False) 'сразу не открываем
            Me.Speed = speed
        End Sub

#End Region '/CTOR

#Region "PROPS"

        Public Overrides ReadOnly Property DeviceMode As DeviceWorkingMode
            Get
                Return DeviceWorkingMode.OneWire
            End Get
        End Property

        ''' <summary>
        ''' Скоростной режим.
        ''' </summary>
        Public Overridable Property Speed As TimingModes
            Get
                Return _Speed
            End Get
            Set(value As TimingModes)
                If (_Speed <> value) Then
                    _Speed = value
                    Status = $"{Application.Lang("ow_speed", "Выбран скоростной режим")} {value}."
                End If
            End Set
        End Property
        Private _Speed As TimingModes = TimingModes.Standard

        ''' <summary>
        ''' Длительность импульса сброса, мкс.
        ''' </summary>
        ''' <remarks>
        ''' При изменении длительности пересчитывается последовательность для сброса <see cref="ResetSeq"/>.
        ''' </remarks>
        Public Overridable Property ResetLen As Double
            Get
                Return _ResetLen
            End Get
            Set(value As Double)
                If (value > 0) Then
                    _ResetLen = value
                    ResetSeq = GenerateNewResetSequence(value)
                    Status = $"{Application.Lang("ow_rstLen", "Задана длительность импульса RESET, мкс")}: {value}."
                End If
            End Set
        End Property
        Private _ResetLen As Double = GetStdLen()

        ''' <summary>
        ''' Возвращает стандартное значение длительности импульса, мкс.
        ''' </summary>
        Protected Function GetStdLen() As Double
            Return If(Speed = TimingModes.Standard, RESET_STANDARD_LEN, RESET_OVER_LEN)
        End Function

        ''' <summary>
        ''' Выбранное устройство (если есть).
        ''' </summary>
        Public Overridable Property SelectedRom As Rom

#End Region '/PROPS

#Region "БАЗОВЫЕ ОПЕРАЦИИ"

        ''' <summary>
        ''' Последовательность байтов для генерации сигнала RESET необходимой длины.
        ''' </summary>
        Private ResetSeq() As Byte = GenerateNewResetSequence(GetStdLen())

        ''' <summary>
        ''' Генерирует новую последовательность сигнала RESET при изменении длительности <see cref="ResetLen"/>.
        ''' </summary>
        ''' <remarks>
        ''' Последний байт всегда 0xFF. Предпоследний добавляем нужное число бит. Первые байты всегда "0".
        ''' </remarks>
        Private Function GenerateNewResetSequence(len As Double) As Byte()
            Dim numBits As Integer = CInt(len * 8 / If(Speed = TimingModes.Standard, RESET_STANDARD_LEN, RESET_OVER_LEN)) 'сколько бит нужно для такой длины
            Dim numBytes As Integer = CInt(Math.Ceiling(numBits / 8))
            Dim b(numBytes) As Byte 'генерируем на 1 байт больше, т.к. нужно ещё передать 0xFF, во время которых считать с линии признак PRESENCE
            Dim bitN As Integer = 0
            For i As Integer = 0 To numBytes
                b(i) = &HFF
                For bit As Integer = 0 To 7
                    If (bitN < numBits) Then
                        b(i) = b(i) And CByte(&H7F >> bit) 'сбрасываем начиная со старшего бита очередного байта
                    Else
                        Exit For
                    End If
                    bitN += 1
                Next
            Next
            Return b
        End Function

        ''' <summary>
        ''' Генерирует импульс сброса на линии и проверяет импульс присутствия от ведомого.
        ''' </summary>
        ''' <returns>
        ''' Если получен импульс присутствия от ведомого, возвращает True, в обратном случае - False.
        ''' </returns>
        Public Function SendReset() As Boolean
            Dim c As New SpiConfig() With {
                .ClockRate = 19000, 'при такой частоте длительность импульса сброса H получается ~480 мкс
                .Pin = UInteger.MaxValue,
                .ConfigOptions = SPI_CONFIG_OPTION.MODE0
            }
            If (Speed = TimingModes.Overdrive) Then
                c.ClockRate = 112400 '=> импульс сброса H ~70 мкс
            End If
            Spi.InitChannel(c)
            Dim presence As Byte() = Spi.ReadWriteSim(ResetSeq, ResetSeq.Length * 8, SPI_TRANSFER_OPTIONS.SIZE_IN_BITS)
#If DEBUG Then
            Debug.Write("rst=")
            For Each b As Byte In ResetSeq
                Debug.Write($"{b:X2} ")
            Next
            Debug.WriteLine("")
            Debug.Write("pres=")
            For Each b As Byte In presence
                Debug.Write($"{b:X2} ")
            Next
            Debug.WriteLine("")
#End If
            'Какой-то из битов последнего принятого байта будет "0", что уменьшит последний байт ResetSeq:
            Dim presenceUs As UShort = CUShort(presence(presence.Length - 2) * 256 + presence(presence.Length - 1)) 'берём 2 последние байта ответа
            Dim resetUs As UShort = CUShort(ResetSeq(ResetSeq.Length - 2) * 256 + ResetSeq(ResetSeq.Length - 1)) 'берём 2 последние переданных байта

            Dim pres As Boolean = (presenceUs < resetUs)
            If pres Then
                Status = $"{Application.Lang("ow_rstAns", "Отправлена команда RESET, получен ответ")}."
            Else
                Status = $"{Application.Lang("ow_rstNoAns", "Отправлена команда RESET, ответа нет")}."
            End If
            Return pres
        End Function

        ''' <summary>
        ''' Записывает массив байтов на линию.
        ''' </summary>
        Public Sub WriteBytes(bytes As IEnumerable(Of Byte))
            PrepareReadWrite()
            For Each b As Byte In bytes
                For i As Integer = 0 To 7
                    WriteBit(CBool(b And 1))
                    b >>= 1
                Next
            Next
        End Sub

        ''' <summary>
        ''' Читает массив байтов с линии.
        ''' </summary>
        ''' <param name="length"></param>
        Public Function ReadBytes(length As Integer) As Byte()
            PrepareReadWrite()
            Dim bytes As New List(Of Byte)
            For len As Integer = 0 To length - 1
                Dim b As Byte = 0
                For i As Integer = 0 To 7
                    b >>= 1 'сдвигаем, чтобы получить следующий бит
                    If ReadBit() Then 'если считанный бит=1, выставляем старший бит
                        b = CByte(b Or &H80)
                    End If
                Next
                bytes.Add(b)
            Next
            Return bytes.ToArray()
        End Function

        ''' <summary>
        ''' Записывает 1 бит на линию.
        ''' </summary>
        ''' <param name="bit">Значение бита.</param>
        ''' <remarks>
        ''' Перед записью бита нужно выставить параметры SPI в методе <see cref="PrepareReadWrite()"/>.
        ''' </remarks>
        Private Sub WriteBit(bit As Boolean)
            Dim bitEqual As Byte()
            If bit Then
                bitEqual = {&H7F} 'эквивалент бита "1"
            Else
                bitEqual = {&H1} 'эквивалент бита "0"
            End If
            Dim r As Byte() = Spi.ReadWriteSim(bitEqual, 8, SPI_TRANSFER_OPTIONS.SIZE_IN_BITS) 'записываем эквивалент бита
        End Sub

        ''' <summary>
        ''' Читает 1 бит с линии.
        ''' </summary>
        ''' <remarks>
        ''' Перед чтением бита нужно выставить параметры SPI в методе <see cref="PrepareReadWrite()"/>.
        ''' </remarks>
        Private Function ReadBit() As Boolean
            Const tx As Byte = &H7F
            Dim r As Byte() = Spi.ReadWriteSim({tx}, 8, SPI_TRANSFER_OPTIONS.SIZE_IN_BITS)
            Return (r(0) = tx) 'если данные на линии не изменились, значит ведомый хочет передать "1".
        End Function

        ''' <summary>
        ''' Конфигурирует SPI с параметрами, необходимыми для чтения или записи.
        ''' </summary>
        ''' <remarks>
        ''' NOTE. Можно перенести в конструктор, а также вызывать при изменении свойства <see cref="Speed"/>.
        ''' </remarks>
        Private Sub PrepareReadWrite()
            'настройка скорости:
            Dim c As New SpiConfig() With {
                .ClockRate = 110000, '=> "1": A=9us, B=63us; "0": C=63us, D=9us
                .Pin = &HFFFFFFFFUI,
                .ConfigOptions = SPI_CONFIG_OPTION.MODE0
            }
            If (Speed = TimingModes.Overdrive) Then
                c.ClockRate = 1000000 '=> "1": A=1us, B=7us; "0": C=7us, D=1us
            End If
            Spi.InitChannel(c)
        End Sub

#End Region '/БАЗОВЫЕ ОПЕРАЦИИ

#Region "ПОИСК УСТРОЙСТВ"

        ''' <summary>
        ''' Индекс бита, с которого должен начаться следующий поиск несоответствия (discrepancy).
        ''' </summary>
        Private LastDiscrepancy As Integer

        ''' <summary>
        ''' Индекс бита, показывающий <see cref="LastDiscrepancy" /> внутри 8-битного кода семейства в номере ROM.
        ''' </summary>
        Private LastFamilyDiscrepancy As Integer

        ''' <summary>
        ''' Флаг, показывающий, что в предыдущем поиске было найдено "последнее" устройство.
        ''' </summary>
        Private LastDevice As Boolean

        ''' <summary>
        ''' Ищет "первое" устройство на шине 1-Wire и возвращает его номер ROM или NULL.
        ''' </summary>
        Public Function OwFirst() As Rom
            LastDiscrepancy = 0 'Сбрасываем состояние
            LastFamilyDiscrepancy = 0
            LastDevice = False
            Return OwSearch()
        End Function

        ''' <summary>
        ''' Ищет "следующее" устройство на шине 1-Wire и возвращает его номер ROM или NULL.
        ''' </summary>
        Public Function OwNext() As Rom
            Return OwSearch()
        End Function

        ''' <summary>
        ''' Ищет "следующее" устройство на шине 1-Wire из семейства <paramref name="familyCode"/> и возвращает его номер ROM.
        ''' </summary>
        ''' <param name="familyCode"></param>
        Public Function OwNext(familyCode As FamilyCodes) As Rom
            Return OwSearch(familyCode)
        End Function

        ''' <summary>
        ''' Проверяет, подключено ли устройство с номером ROM <paramref name="romNo"/>, к шине 1-Wire.
        ''' </summary>
        ''' <returns>
        ''' True: устройство найдено на шине.
        ''' False: устройство не найдено.
        ''' </returns>
        Public Function OwVerify(romNo As Byte()) As Boolean
            Return OwVerify(New Rom(romNo))
        End Function

        ''' <summary>
        ''' Проверяет, подключено ли устройство с номером ROM <paramref name="romNo"/> к шине 1-Wire.
        ''' </summary>
        ''' <returns>
        ''' True: устройство найдено на шине.
        ''' False: устройство не найдено.
        ''' </returns>
        Public Function OwVerify(romNo As Rom) As Boolean

            'Сохраняем резервную копию текущего состояния:
            Dim ldBackup As Integer = LastDiscrepancy
            Dim lfdBackup As Integer = LastFamilyDiscrepancy
            Dim ldfBackup As Boolean = LastDevice

            LastDiscrepancy = 64
            LastDevice = False

            Dim result As Boolean = True 'результат верификации
            Dim romOnBus As Rom = OwSearch()
            If (romOnBus IsNot Nothing) Then
                'проверяет, найдено ли то же устройство:
                For i As Integer = 0 To 7
                    If (romOnBus(i) <> romNo(i)) Then
                        result = False
                        Exit For
                    End If
                Next
            Else
                result = False
            End If

            'Восстанавливаем состояние из резервной копии:
            LastDiscrepancy = ldBackup
            LastFamilyDiscrepancy = lfdBackup
            LastDevice = ldfBackup

            Return result
        End Function

#Region "CLOSED METHODS"

        ''' <summary>
        ''' Ищет устройства на шине.
        ''' </summary>
        ''' <returns>
        ''' Rom: номер ROM найденного устройства.
        ''' NULL: устройство не найдено.
        ''' </returns>
        Private Function OwSearch() As Rom
            Return OwSearchCommon({0, 0, 0, 0, 0, 0, 0, 0})
        End Function

        ''' <summary>
        ''' Реализует алгоритм поиска 1-Wire для конкретного семейства устройств <paramref name="familyCode"/>.
        ''' </summary>
        ''' <returns>
        ''' Rom: номер ROM найденного устройства.
        ''' NULL: устройство не найдено.
        ''' </returns>
        Private Function OwSearch(familyCode As FamilyCodes) As Rom
            LastDiscrepancy = 64
            LastFamilyDiscrepancy = 0
            LastDevice = False
            Return OwSearchCommon({familyCode, 0, 0, 0, 0, 0, 0, 0})
        End Function

        ''' <summary>
        ''' Общая часть алгоритма поиска 1-Wire.
        ''' </summary>
        ''' <param name="rom_no"></param>
        Private Function OwSearchCommon(rom_no As Byte()) As Rom
            Dim searchResult As Boolean = False
            If (Not LastDevice) Then

                Dim crc8 As Byte = 0

                'Если при сбросе 1-Wire устройства не найдены, выходим:
                If (Not SendReset()) Then
                    LastDiscrepancy = 0
                    LastFamilyDiscrepancy = 0
                    LastDevice = False
                    Return Nothing
                End If

                WriteBytes({ONEWIRE_SEARCH}) 'записываем команду поиска

                Dim lastZero As Integer = 0 'битовая позиция последнего прочитанного бита "0"
                Dim romByteNumber As Integer = 0
                Dim romByteMask As Byte = 1
                Dim idBitNumber As Integer = 1 'текущий номер бита ROM, от 1 до 64

                'Цикл поиска для всех 8-ми байтов ROM:
                Do
                    'читаем бит и его дополнение:
                    Dim idBit As Boolean = ReadBit()
                    Dim cmpIdBit As Boolean = ReadBit()
                    If (idBit AndAlso cmpIdBit) Then 'если биты совпадают, выходим
                        Exit Do
                    End If

                    Dim search_direction As Boolean
                    If (idBit <> cmpIdBit) Then
                        search_direction = idBit
                    Else
                        If (idBitNumber < LastDiscrepancy) Then
                            search_direction = ((rom_no(romByteNumber) And romByteMask) > 0)
                        Else
                            search_direction = (idBitNumber = LastDiscrepancy)
                        End If

                        If (Not search_direction) Then 'Если "0", записываем его позицию в LastZero 
                            lastZero = idBitNumber

                            If (lastZero < 9) Then 'Проверяем последнее discrepancy в семействе
                                LastFamilyDiscrepancy = lastZero
                            End If
                        End If
                    End If

                    'Устанавливаем или сбрасываем бит в ROM:
                    If search_direction Then
                        rom_no(romByteNumber) = rom_no(romByteNumber) Or romByteMask 'ROM_NO[rom_byte_number] |= rom_byte_mask;
                    Else
                        rom_no(romByteNumber) = rom_no(romByteNumber) And (Not romByteMask) 'ROM_NO[rom_byte_number] &= ~rom_byte_mask; '~ - НЕ (отрицание, дополнение к 1) 
                    End If

                    WriteBit(search_direction) 'записываем бит направления поиска серийного номера

                    'Увеличиваем счётчик байтов и сдвигаем маску:
                    idBitNumber += 1
                    romByteMask <<= 1

                    If (romByteMask = 0) Then
                        crc8 = DoCrc8(crc8, rom_no(romByteNumber)) 'аккумулируем CRC
                        romByteNumber += 1 'переходим к следующему байту серийного номера
                        romByteMask = 1 'сбрасываем маску
                    End If

                Loop While (romByteNumber < 8)

                'При успешном поиске:
                If (Not ((idBitNumber < 65) OrElse (crc8 <> 0))) Then 'if (! ((id_bit_number < 65) || (crc8 != 0)) )
                    LastDiscrepancy = lastZero
                    LastDevice = (LastDiscrepancy = 0) 'проверяем последнее устройство 
                    searchResult = True
                End If
            End If

            'Если устройство не найдено, сбрасываем счётчики для следующего поиска:
            If ((Not searchResult) OrElse (rom_no(0) = 0)) Then 'if (!search_result || !ROM_NO[0])
                LastDiscrepancy = 0
                LastFamilyDiscrepancy = 0
                LastDevice = False
                searchResult = False
            End If

            If searchResult Then
                Return New Rom(rom_no)
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Вычисляет CRC8 по предыдущему значению.
        ''' </summary>
        ''' <remarks>См. Application Note 27.</remarks>
        Private Function DoCrc8(prevValue As Byte, value As Byte) As Byte
            Dim crc8 As Byte = DsCrcTable(prevValue Xor value) 'crc8 = DsCrcTable[crc8 ^ value];
            Return crc8
        End Function

        ''' <summary>
        ''' Таблица предварительно рассчитанных контрольных сумм.
        ''' </summary>
        Private ReadOnly DsCrcTable() As Byte = {
            0, 94, 188, 226, 97, 63, 221, 131, 194, 156, 126, 32, 163, 253, 31, 65,
            157, 195, 33, 127, 252, 162, 64, 30, 95, 1, 227, 189, 62, 96, 130, 220,
            35, 125, 159, 193, 66, 28, 254, 160, 225, 191, 93, 3, 128, 222, 60, 98,
            190, 224, 2, 92, 223, 129, 99, 61, 124, 34, 192, 158, 29, 67, 161, 255,
            70, 24, 250, 164, 39, 121, 155, 197, 132, 218, 56, 102, 229, 187, 89, 7,
            219, 133, 103, 57, 186, 228, 6, 88, 25, 71, 165, 251, 120, 38, 196, 154,
            101, 59, 217, 135, 4, 90, 184, 230, 167, 249, 27, 69, 198, 152, 122, 36,
            248, 166, 68, 26, 153, 199, 37, 123, 58, 100, 134, 216, 91, 5, 231, 185,
            140, 210, 48, 110, 237, 179, 81, 15, 78, 16, 242, 172, 47, 113, 147, 205,
            17, 79, 173, 243, 112, 46, 204, 146, 211, 141, 111, 49, 178, 236, 14, 80,
            175, 241, 19, 77, 206, 144, 114, 44, 109, 51, 209, 143, 12, 82, 176, 238,
            50, 108, 142, 208, 83, 13, 239, 177, 240, 174, 76, 18, 145, 207, 45, 115,
            202, 148, 118, 40, 171, 245, 23, 73, 8, 86, 180, 234, 105, 55, 213, 139,
            87, 9, 235, 181, 54, 104, 138, 212, 149, 203, 41, 119, 244, 170, 72, 22,
            233, 183, 85, 11, 136, 214, 52, 106, 43, 117, 151, 201, 74, 20, 246, 168,
            116, 42, 200, 150, 21, 75, 169, 247, 182, 232, 10, 84, 215, 137, 107, 53
        }

#End Region '/CLOSED METHODS

#End Region '/ПОИСК УСТРОЙСТВ

#Region "СТАНДАРТНЫЕ КОМАНДЫ"

        ''' <summary>
        ''' Отправляет команду READ (чтение адреса единственного устройства на шине) и читает ROM.
        ''' </summary>
        ''' <remarks>
        ''' Перед чтением должна быть послана команда RESET.
        ''' </remarks>
        Public Function SendReadRom() As Rom
            WriteBytes({ONEWIRE_READ})
            Dim bytes As IEnumerable(Of Byte) = ReadBytes(8)
            Dim r As New Rom(bytes)
            Status = $"{Application.Lang("ow_rdAns", "Отправлена команда READ, получен ответ")} {r}."
            Return r
        End Function

        ''' <summary>
        ''' Отправляет команду SKIP (игнорировать адрес устройства).
        ''' </summary>
        ''' <remarks>
        ''' Используется при обращении к единственному устройству на шине или сразу ко всем устройствам.
        ''' </remarks>
        Public Sub SendSkipRom()
            WriteBytes({ONEWIRE_SKIP})
            Status = $"{Application.Lang("ow_skip", "Отправлена команда SKIP")}."
        End Sub

        ''' <summary>
        '''  Отправляет команду MATCH (выбор адреса для обращения к конкретному устройству на шине).
        ''' </summary>
        Public Sub SendMatchRom()
            If (SelectedRom IsNot Nothing) Then
                Dim matchRomCommand As New List(Of Byte) From {ONEWIRE_MATCH}
                For Each b As Byte In SelectedRom.ROM_NO
                    matchRomCommand.Add(b)
                Next
                WriteBytes(matchRomCommand)
                Status = $"{Application.Lang("ow_match", "Отправлена команда MATCH")}."
            End If
        End Sub

#End Region '/СТАНДАРТНЫЕ КОМАНДЫ

    End Class '/MpsseOneWire

    Public Enum TimingModes
        Standard
        Overdrive
    End Enum

    Public Enum FamilyCodes As Byte
        ''' <summary>
        ''' 1–Wire net address (serial number) only
        ''' </summary>
        DS1990A_DS2401 = &H1
        ''' <summary>
        ''' MultiKey iButton, 1152–bit secure memory.
        ''' </summary>
        DS1991_DS1425 = &H2
        ''' <summary>
        ''' 4K NVRAM memory and clock, timer, alarms.
        ''' </summary>
        DS1994_DS2404 = &H4
        ''' <summary>
        ''' Single addressable switch.
        ''' </summary>
        DS2405 = &H5
        ''' <summary>
        ''' 4K NVRAM memory.
        ''' </summary>
        DS1993 = &H6
        ''' <summary>
        '''  1K NVRAM memory.
        ''' </summary>
        DS1992 = &H8
        ''' <summary>
        ''' 1K EPROM memory.
        ''' </summary>
        DS1982_DS2502 = &H9
        ''' <summary>
        '''  16K NVRAM memory
        ''' </summary>
        DS1995 = &HA
        ''' <summary>
        ''' 16K EPROM memory.
        ''' </summary>
        DS1985_DS2505 = &HB
        ''' <summary>
        ''' 64K to 256K NVRAM memory.
        ''' </summary>
        DS1996_DS1996x2_DS1996x4 = &HC
        ''' <summary>
        ''' 64K EPROM memory.
        ''' </summary>
        DS1986_DS2506 = &HF
        ''' <summary>
        ''' Temperature with alarm trips.
        ''' </summary>
        DS1920_DS1820_DS18S20 = &H10
        ''' <summary>
        ''' 1K EPROM memory, 2 channel addressable switch.
        ''' </summary>
        DS2406_DS2407 = &H12
        ''' <summary>
        ''' 256–bit EEPROM memory and 64–bit OTP register.
        ''' </summary>
        DS1971_DS2430A = &H14
        ''' <summary>
        ''' 4K NVRAM memory and SHA–1 engine.
        ''' </summary>
        DS1963S = &H18
        ''' <summary>
        ''' 4K NVRAM memory with write cycle counters.
        ''' </summary>
        DS1963L = &H1A
        ''' <summary>
        ''' 4K NVRAM memory with external counters.
        ''' </summary>
        DS2423 = &H1D
        ''' <summary>
        ''' 2 channel addressable coupler for sub–netting.
        ''' </summary>
        DS2409 = &H1F
        ''' <summary>
        ''' 4 channel A/D.
        ''' </summary>
        DS2450 = &H20
        ''' <summary>
        '''  Thermochron temperature logger.
        ''' </summary>
        DS1921_DS1921H_DS1921Z = &H21
        ''' <summary>
        ''' Econo–Temperature.
        ''' </summary>
        DS1822 = &H22
        ''' <summary>
        ''' 4K EEPROM memory.
        ''' </summary>
        DS1973_DS2433 = &H23
        ''' <summary>
        ''' Real–time–clock.
        ''' </summary>
        DS1904_DS2415 = &H24
        ''' <summary>
        ''' Temperature, A/D.
        ''' </summary>
        DS2438 = &H26
        ''' <summary>
        ''' Real–time–clock with interrupt.
        ''' </summary>
        DS2417 = &H27
        ''' <summary>
        ''' Adjustable resolution temperature.
        ''' </summary>
        DS18B20 = &H28
        ''' <summary>
        ''' Single channel digital potentiometer.
        ''' </summary>
        DS2890 = &H2C
        ''' <summary>
        ''' Temperature, current, A/D.
        ''' </summary>
        DS2760 = &H30
        ''' <summary>
        ''' 1K EEPROM memory with SHA–1 engine.
        ''' </summary>
        DS1961S_DS2432 = &H33
        ''' <summary>
        ''' 512–bit EPROM memory (Uniqueware only).
        ''' </summary>
        DS1981 = &H91
        ''' <summary>
        '''  Java–powered cryptographic iButton (64K–byte ROM, 6 to 134K–byte NVRAM).
        ''' </summary>
        DS1955_DS1957B = &H96
    End Enum

    ''' <summary>
    ''' Номер ROM.
    ''' </summary>
    Public Class Rom

#Region "CTORs"

        ''' <summary>
        ''' Создаёт ROM со значением по умолчанию (8 нулей).
        ''' </summary>
        Public Sub New()
            ROM_NO = New Byte() {0, 0, 0, 0, 0, 0, 0, 0}
        End Sub

        ''' <summary>
        ''' Создаёт ROM из строки <paramref name="bytes"/>. В строке содержатся 8 байтов в hex-представлении. Разделитель - любой.
        ''' </summary>
        Public Sub New(bytes As String)
            Dim hexRegex As New Text.RegularExpressions.Regex("[0-9A-Fa-f]{2}")
            Dim mc As Text.RegularExpressions.MatchCollection = hexRegex.Matches(bytes)
            ReDim ROM_NO(mc.Count - 1)
            For i As Integer = 0 To mc.Count - 1
                ROM_NO(i) = Byte.Parse(mc(i).Value, Globalization.NumberStyles.HexNumber)
            Next
        End Sub

        ''' <summary>
        ''' Создаёт ROM из массива 8-ми байтов <paramref name="bytes"/>.
        ''' </summary>
        Public Sub New(bytes As IEnumerable(Of Byte))
            If (bytes.Count <> 8) Then
                Throw New ArgumentException($"{Application.Lang("ow_romEx", "ROM должен состоять из 8-ми байтов")}.")
            End If
            ROM_NO = bytes.ToArray()
        End Sub

#End Region '/CTORs

#Region "PROPS"

        ''' <summary>
        ''' Значение элемента ROM, по индексу <paramref name="index"/>.
        ''' </summary>
        Default Public ReadOnly Property Item(index As Integer) As Byte
            Get
                Return ROM_NO(index)
            End Get
        End Property

        ''' <summary>
        ''' Номер ROM.
        ''' </summary>
        ''' <remarks>
        ''' Первый байт - код семейства, последний - CRC8.
        ''' </remarks>
        Public ReadOnly Property ROM_NO As Byte() '= New Byte() {0, 0, 0, 0, 0, 0, 0, 0}

        ''' <summary>
        ''' Семейство устройства.
        ''' </summary>
        Public ReadOnly Property DeviceFamily As FamilyCodes
            Get
                Return CType(ROM_NO(0), FamilyCodes)
            End Get
        End Property

        ''' <summary>
        ''' Контрольная сумма номера устройства.
        ''' </summary>
        Public ReadOnly Property RomCrc As Byte
            Get
                Return ROM_NO(7)
            End Get
        End Property

#End Region '/PROPS

#Region "METHODS"

        ''' <summary>
        ''' Выводит ROM в hex-представлении (8 байт).
        ''' </summary>
        Public Overrides Function ToString() As String
            Dim sb As New Text.StringBuilder()
            For i As Integer = 0 To 7
                sb.Append($"{Item(i):X2} ")
            Next
            Return sb.ToString()
        End Function

#End Region '/METHODS

    End Class '/Rom

End Namespace
