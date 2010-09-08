////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include <tinyhal.h>

//--//

#define NVIC_ICER       0xE000E180
#define NVIC_ISER       0xE000E100
#define NVIC_ABR        0xE000E300

//--//
#define DISABLED_MASK 0x3

//--//

///////////////////////////////////////////////////////////////////
// For Thumb2 code, we need to declare some functions as extern 
// and implement them in assembly, as the RVDS3.1 compiler does
// not support inline assembly
// 
extern "C"
{
void   IRQ_LOCK_Probe_asm();
UINT32 IRQ_LOCK_Release_asm();
UINT32 IRQ_LOCK_GetState_asm();
UINT32 IRQ_LOCK_ForceDisabled_asm();
UINT32 IRQ_LOCK_ForceEnabled_asm();
UINT32 IRQ_LOCK_Disable_asm();
void   IRQ_LOCK_Restore_asm();
}
//
//
///////////////////////////////////////////////////////////////////


SmartPtr_IRQ::SmartPtr_IRQ(void* context)
{ 
    m_context = context; 
    Disable(); 
}

SmartPtr_IRQ::~SmartPtr_IRQ() 
{ 
    Restore(); 
}

BOOL SmartPtr_IRQ::WasDisabled()
{
    return (m_state & DISABLED_MASK) == DISABLED_MASK;
}

void SmartPtr_IRQ::Acquire()
{
    UINT32 Cp = m_state;

    if((Cp & DISABLED_MASK) == DISABLED_MASK)
    {
        Disable();
    }
}

void SmartPtr_IRQ::Release()
{
    UINT32 Cp = m_state;

    if((Cp & DISABLED_MASK) == 0)
    {
        m_state = IRQ_LOCK_Release_asm();
    }
}

void SmartPtr_IRQ::Probe()
{
    UINT32 Cp = m_state;

    if((Cp & DISABLED_MASK) == 0)
    {
        IRQ_LOCK_Probe_asm(); 
    }
}

BOOL SmartPtr_IRQ::GetState(void* context)
{
    return IRQ_LOCK_GetState_asm();
}

BOOL SmartPtr_IRQ::ForceDisabled(void* context)
{
    return IRQ_LOCK_ForceDisabled_asm();
}

BOOL SmartPtr_IRQ::ForceEnabled(void* context)
{
    return IRQ_LOCK_ForceEnabled_asm();
}

void SmartPtr_IRQ::Disable()
{
    m_state = IRQ_LOCK_Disable_asm();
}

void SmartPtr_IRQ::Restore()
{
    IRQ_LOCK_Restore_asm();
}

