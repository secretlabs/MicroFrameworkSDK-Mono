using System;
using System.Collections;
using System.Threading;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.SPOT.Hardware
{
    public class HardwareProvider
    {
        private static HardwareProvider s_hwProvider = null;

        //--//

        public static void Register(HardwareProvider provider)
        {
            s_hwProvider = provider;

        }

        //--//

        public static HardwareProvider HwProvider
        {
            get
            {
                if (s_hwProvider == null)
                {
                    s_hwProvider = new HardwareProvider();
                }

                return s_hwProvider;
            }
        }

        //--//

        public virtual void GetSerialPins(string comPort, out Cpu.Pin rxPin, out Cpu.Pin txPin, out Cpu.Pin ctsPin, out Cpu.Pin rtsPin)
        {
            int comIdx = System.IO.Ports.SerialPortName.ConvertNameToIndex(comPort);

            rxPin = Cpu.Pin.GPIO_NONE;
            txPin = Cpu.Pin.GPIO_NONE;
            ctsPin = Cpu.Pin.GPIO_NONE;
            rtsPin = Cpu.Pin.GPIO_NONE;

            NativeGetSerialPins(comIdx, ref rxPin, ref txPin, ref ctsPin, ref rtsPin);
        }

        public virtual int GetSerialPortsCount()
        {

            return NativeGetSerialPortsCount();
        }

        public virtual bool SupportsNonStandardBaudRate(int com)
        {
            return NativeSupportsNonStandardBaudRate(com);
        }

        public virtual void GetBaudRateBoundary(int com, out uint MaxBaudRate, out uint MinBaudRate)
        {
            NativeGetBaudRateBoundary(com, out MaxBaudRate, out MinBaudRate);
        }

        public virtual bool IsSupportedBaudRate(int com, ref uint baudrateHz)
        {
            return NativeIsSupportedBaudRate(com, ref baudrateHz);
        }

        public virtual void GetSupportBaudRates(int com, out System.IO.Ports.BaudRate[] StdBaudRate, out int size)
        {
            uint rBaudrate = 0;
            uint[] baudrateSet = new uint[]  { 75,
                                               150,
                                               300,
                                               600,
                                               1200,
                                               2400,
                                               4800,
                                               9600,
                                               19200,
                                               38400,
                                               57600,
                                               115200,
                                               230400,
                                              };

            StdBaudRate = new System.IO.Ports.BaudRate[13] {    System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                                System.IO.Ports.BaudRate.BaudrateNONE,
                                                            };

            size = 0;
            for (int i = 0; i < baudrateSet.Length; i++)
            {
                rBaudrate = baudrateSet[i];
                if (IsSupportedBaudRate(com, ref rBaudrate))
                {
                    StdBaudRate[size] = (System.IO.Ports.BaudRate)rBaudrate;
                    size++;
                }
            }

        }

        //--//
        public virtual void GetSpiPins(SPI.SPI_module spi_mod, out Cpu.Pin msk, out Cpu.Pin miso, out Cpu.Pin mosi)
        {
            msk = Cpu.Pin.GPIO_NONE;
            miso = Cpu.Pin.GPIO_NONE;
            mosi = Cpu.Pin.GPIO_NONE;

            NativeGetSpiPins(spi_mod, out msk, out miso, out mosi);
        }

        public virtual int GetSpiPortsCount()
        {

            return NativeGetSpiPortsCount();
        }

        //--//
        public virtual void GetI2CPins(out Cpu.Pin scl, out Cpu.Pin sda)
        {
            scl = Cpu.Pin.GPIO_NONE;
            sda = Cpu.Pin.GPIO_NONE;

            NativeGetI2CPins(out scl, out sda);
        }

        //--//
        public virtual int GetPinsCount()
        {
            return NativeGetPinsCount();
        }

        public virtual void GetPinsMap(out Cpu.PinUsage[] pins, out int PinCount)
        {

            PinCount = GetPinsCount();

            pins = new Cpu.PinUsage[PinCount];

            NativeGetPinsMap(pins);
        }

        public virtual Cpu.PinUsage GetPinsUsage(Cpu.Pin pin)
        {
            return NativeGetPinUsage(pin);
        }

        public virtual Cpu.PinValidResistorMode GetSupportedResistorModes(Cpu.Pin pin)
        {
            return NativeGetSupportedResistorModes(pin);
        }

        public virtual Cpu.PinValidInterruptMode GetSupportedInterruptModes(Cpu.Pin pin)
        {
            return NativeGetSupportedInterruptModes(pin);
        }

        //--//

        public virtual Cpu.Pin GetButtonPins(Button iButton)
        {
            return NativeGetButtonPins(iButton);
        }

        //--//
        public virtual void GetLCDMetrics(out int width, out int height, out int bitsPerPixel, out int orientationDeg)
        {
            NativeGetLCDMetrics(out height, out width, out bitsPerPixel, out orientationDeg);
        }

        //---//

        //---// native calls

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private void NativeGetSerialPins(int com, ref Cpu.Pin rxPin, ref Cpu.Pin txPin, ref Cpu.Pin ctsPin, ref Cpu.Pin rtsPin)
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private int NativeGetSerialPortsCount()
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private bool NativeSupportsNonStandardBaudRate(int com)
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private void NativeGetBaudRateBoundary(int com, out uint MaxBaudRate, out uint MinBaudRate)

#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private bool NativeIsSupportedBaudRate(int com, ref uint baudrateHz)

#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private void NativeGetSpiPins(SPI.SPI_module spi_mod, out Cpu.Pin msk, out Cpu.Pin miso, out Cpu.Pin mosi)
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private int NativeGetSpiPortsCount()
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private void NativeGetI2CPins(out Cpu.Pin scl, out Cpu.Pin sda)
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private int NativeGetPinsCount()
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private void NativeGetPinsMap(Cpu.PinUsage[] pins)
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private Cpu.PinUsage NativeGetPinUsage(Cpu.Pin pin)
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private Cpu.PinValidResistorMode NativeGetSupportedResistorModes(Cpu.Pin pin)
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private Cpu.PinValidInterruptMode NativeGetSupportedInterruptModes(Cpu.Pin pin)
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private Cpu.Pin NativeGetButtonPins(Button iButton)
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

#if !TINYCLR_DIRECTBAND_SERVER
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern
#endif
 private void NativeGetLCDMetrics(out int height, out int width, out int bitPerPixel, out int orientationDeg)
#if TINYCLR_DIRECTBAND_SERVER
        {
            throw new NotImplementedException();
        }

#else
;
#endif

    }
}


