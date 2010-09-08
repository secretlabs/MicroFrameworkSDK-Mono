////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace Microsoft.SPOT.Emulator.Time
{
    public interface ITimeDriver
    {
        bool Initialize();
        bool Uninitialize();
        ulong CurrentTicks();
        long TicksToTime(ulong Ticks);
        long CurrentTime();
        void SetCompare(ulong CompareValue);
        void Sleep_MicroSeconds(uint uSec);
        long MicrosecondsToTicks(long ms);
        uint SystemClock { get; }
        uint TicksPerSecond { get; }

        void EnqueueCompletion( IntPtr Completion, uint uSecFromNow );
        void AbortCompletion( IntPtr Completion );        
        void EnqueueContinuation( IntPtr Continuation );
        void AbortContinuation( IntPtr Continuation );
        bool DequeueAndExecuteContinuation();

        bool IsExecutionPaused { set; }
    }
}
