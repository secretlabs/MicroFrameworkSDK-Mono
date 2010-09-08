////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.SPOT
{
    public enum EventCategory
    {
        Unknown     = 0,
        Touch       = 1,
        Gesture     = 2,
        Storage     = 3,
        Network     = 4,
        SleepLevel  = 5,
        PowerLevel  = 6,
        TimeService = 7,
        LargeBuffer = 8,
        Custom      = 100,
    }

    public class BaseEvent
    {
        public ushort Source;
        public byte EventMessage;
    }

    public class GenericEvent : BaseEvent
    {
        public byte EventCategory;
        public uint EventData;
        public int X;
        public int Y;
        public DateTime Time;
    }

    namespace Touch
    {
        [FlagsAttribute]
        public enum TouchInputFlags : uint
        {
            None = 0x00,
            Primary = 0x0010,  //The Primary flag denotes the input that is passed to the single-touch Stylus events provided

            //no controls handle the Touch events.  This flag should be set on the TouchInput structure that represents
            //the first finger down as it moves around up to and including the point it is released.

            Pen = 0x0040,     //Hardware support is optional, but providing it allows for potentially richer applications.
            Palm = 0x0080,     //Hardware support is optional, but providing it allows for potentially richer applications.
        }

        public struct TouchInput
        {
            public int X;
            public int Y;
            public byte SourceID;
            public TouchInputFlags Flags;
            public uint ContactWidth;
            public uint ContactHeight;
        }

        public class TouchEvent : BaseEvent
        {
            public DateTime Time;
            public TouchInput[] Touches;
        }
    }

    public interface IEventProcessor
    {
        /// <summary>
        /// IEventProcessor should return null if it cannot process an event,
        /// in that case next processor will be given an opportunity.
        /// </summary>
        /// <param name="data1"></param>
        /// <param name="data2"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        BaseEvent ProcessEvent(uint data1, uint data2, DateTime time);
    }

    public interface IEventListener
    {
        void InitializeForEventSource();
        bool OnEvent(BaseEvent ev);
    }

    //--//

    public interface ILog
    {
        void Log(object o);
    }
}


