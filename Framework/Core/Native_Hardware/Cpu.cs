using System;
using System.Collections;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Microsoft.SPOT.Hardware
{
    public static class Cpu
    {
        [Flags]
        public enum PinUsage : byte
        {
            NONE = 0,
            INPUT = 1,
            OUTPUT = 2,
            ALTERNATE_A = 4,
            ALTERNATE_B = 8,
        };

        [Flags]
        public enum PinValidResistorMode : byte
        {
            NONE = 0,
            Disabled = 1 << Microsoft.SPOT.Hardware.Port.ResistorMode.Disabled,
            PullUp = 1 << Microsoft.SPOT.Hardware.Port.ResistorMode.PullDown,
            PullDown = 1 << Microsoft.SPOT.Hardware.Port.ResistorMode.PullUp,
        };

        [Flags]
        public enum PinValidInterruptMode : byte
        {
            NONE = 0,
            InterruptEdgeLow = 1 << Microsoft.SPOT.Hardware.Port.InterruptMode.InterruptEdgeLow,
            InterruptEdgeHigh = 1 << Microsoft.SPOT.Hardware.Port.InterruptMode.InterruptEdgeHigh,
            InterruptEdgeBoth = 1 << Microsoft.SPOT.Hardware.Port.InterruptMode.InterruptEdgeBoth,
            InterruptEdgeLevelHigh = 1 << Microsoft.SPOT.Hardware.Port.InterruptMode.InterruptEdgeLevelHigh,
            InterruptEdgeLevelLow = 1 << Microsoft.SPOT.Hardware.Port.InterruptMode.InterruptEdgeLevelLow,
        };

        public enum Pin : int
        {
            GPIO_NONE = -1,
            GPIO_Pin0 = 0,
            GPIO_Pin1 = 1,
            GPIO_Pin2 = 2,
            GPIO_Pin3 = 3,
            GPIO_Pin4 = 4,
            GPIO_Pin5 = 5,
            GPIO_Pin6 = 6,
            GPIO_Pin7 = 7,
            GPIO_Pin8 = 8,
            GPIO_Pin9 = 9,
            GPIO_Pin10 = 10,
            GPIO_Pin11 = 11,
            GPIO_Pin12 = 12,
            GPIO_Pin13 = 13,
            GPIO_Pin14 = 14,
            GPIO_Pin15 = 15,
        }

        //--//

        extern public static uint SystemClock
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        extern public static uint SlowClock
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        extern public static TimeSpan GlitchFilterTime
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;

            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            set;
        }
    }
}


