////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include <tinyhal.h>
#include "palevent_driver.h"

PalEventDriver g_palEventDriver;

HRESULT PalEventDriver::Initialize()
{
    if (!m_initialized)
    {
        m_listenerList.Initialize();
        m_initialized = true;
    }

    return S_OK;
}

HRESULT PalEventDriver::Uninitialize()
{
    if (m_initialized)
    {
        m_initialized = false;
        /// Remove all nodes.
    }

    return S_OK;
}

HRESULT PalEventDriver::PostEvent(unsigned int e, unsigned int param)
{
    PalEventListener *listener = m_listenerList.FirstValidNode();
        
    while(listener != NULL)
    {
        if (listener->m_eventMask & e)
        {
            listener->m_palEventListener(e, param);
        }

        listener = listener->Next();
    }

    return S_OK;
}

HRESULT PalEventDriver::EnlistListener(PalEventListener* listener)
{
    listener->Initialize();
    m_listenerList.LinkAtBack(listener);

    return S_OK;
}

