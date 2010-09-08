////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include <cores\arm\include\cpu.h>

//--//


void HAL_AssertEx()
{
    // cause an abort and let the abort handler take over
    volatile BOOL fContinue = FALSE;

    lcd_printf("\f***** ASSERT *****\r\n");
#if _DEBUG
    while(!fContinue);
#else
    char* ptr = NULL;
    *ptr = 'a';
#endif    
}


BOOL CPU_Initialize()
{
    return AT91_SAM_Driver::Initialize();
}

//--//

#pragma arm section code

void CPU_ChangePowerLevel(POWER_LEVEL level)
{
    switch(level)
    {
        case POWER_LEVEL__MID_POWER:
            break;

        case POWER_LEVEL__LOW_POWER:
            break;

        case POWER_LEVEL__HIGH_POWER:
        default:
            break;
    }
}

void CPU_Sleep(SLEEP_LEVEL level, UINT64 wakeEvents)
{
    switch(level)
    {
        case SLEEP_LEVEL__DEEP_SLEEP:
            AT91_SAM_Driver::Hibernate();
            break;
        case SLEEP_LEVEL__OFF:
            AT91_SAM_Driver::Shutdown();
            break;
        case SLEEP_LEVEL__SLEEP:
        default:
            AT91_SAM_Driver::Sleep();
            break;
    }
}

void CPU_Halt()
{
    AT91_SAM_Driver::Halt();
}

void CPU_Reset()
{
    AT91_SAM_Driver::Reset();
}

BOOL CPU_IsSoftRebootSupported ()
{
    return TRUE;
}

