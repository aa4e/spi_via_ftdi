Namespace Ftdi

    ''' <summary>
    ''' Абстрактный класс устройств, которые не являются MPSSE, но имеют в своём составе устройство MPSSE.
    ''' </summary>
    Public MustInherit Class ContainsMpsseBase
        Inherits MpsseBase

#Region "CTOR"

        Protected Sub New(index As Integer, Optional openNow As Boolean = True)
            MyBase.New(index)
            Spi = New Ftdi.Spi.MpsseSpi(index, openNow)
            SetSerialNumber(Spi.SerialNumber)
            SetDeviceType(Spi.DeviceType)
            SetNameReadable(Spi.NameReadable)
        End Sub

#End Region '/CTOR

#Region "PROPS"

        ''' <summary>
        ''' Устройство MPSSE, которое реализует произвольные интерфейсы, отличные от нативных SPI/I2C.
        ''' </summary>
        Protected Overridable ReadOnly Property Spi As Ftdi.Spi.MpsseSpi

        Public Overrides ReadOnly Property DeviceHandle As Integer
            Get
                Return If(Spi IsNot Nothing, Spi.DeviceHandle, CLOSED_HANDLE)
            End Get
        End Property

        Public Overrides ReadOnly Property IsOpened As Boolean
            Get
                If (Spi IsNot Nothing) Then
                    Return Spi.IsOpened()
                End If
                Return False
            End Get
        End Property

#End Region '/PROPS

#Region "OVERRIDES"

        Public Overrides Function GetChannelInfo() As FT_DEVICE_LIST_INFO_NODE
            Return Spi.GetChannelInfo()
        End Function

        Public Overrides Sub OpenChannel()
            Spi.OpenChannel()
            RaiseConnectionStateChanged(True)
        End Sub

        Public Overrides Sub CloseChannel()
            Spi.CloseChannel()
            RaiseConnectionStateChanged(False)
        End Sub

        ''' <summary>
        ''' Возвращает устройство, которое обеспечивает обмен по протоколу.
        ''' </summary>
        Public Function GetControllerDevice() As Ftdi.Spi.MpsseSpi
            Return Spi
        End Function

#End Region '/OVERRIDES

    End Class

End Namespace
