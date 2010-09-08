////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include <tinyhal.h>

void Listener(unsigned int e, unsigned int param);

class GestureDriver
{
    static const int c_IgnoreCount = 2;
protected:
    PalEventListener m_gestureListener;
    bool             m_initialized;
    HAL_COMPLETION   m_gestureCompletion;
    UINT32           m_index;
    UINT32           m_currentState;
    UINT16           m_lastx;
    UINT16           m_lasty;
    UINT16           m_startx;
    UINT16           m_starty;

    UINT32           m_stateIgnoreIndex;
    UINT32           m_stateIgnoreHead;
    UINT32           m_stateIgnoreTail;
    UINT32           m_stateIgnoreBuffer[c_IgnoreCount];


public:

    GestureDriver()
    {
        m_initialized = false;
    }


    HRESULT Initialize();
    HRESULT Uninitialize();
    static bool ProcessPoint(UINT32 flags, UINT16 source, UINT16 x, UINT16 y, INT64 time);

    void ResetRecognition();
    static void EventListener(unsigned int e, unsigned int param);
    static void GestureContinuationRoutine(void *arg);

};

extern GestureDriver g_GestureDriver;

