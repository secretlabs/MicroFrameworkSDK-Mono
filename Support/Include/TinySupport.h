////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef _SUPPORT_TINYSUPPORT_H_
#define _SUPPORT_TINYSUPPORT_H_

////////////////////////////////////////////////////////////////////////////////////////////////////

#if defined(_WIN32_WCE)

#include <windows.h>
#include <stdio.h>
#include <stdarg.h>

#elif defined(_WIN32) || defined(WIN32)

#define _WIN32_WINNT 0x0501

#include <windows.h>
#include <stdio.h>
#include <stdarg.h>

#include <tinyhal.h>

#else

#include <stdarg.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

#include <tinyhal.h>

#endif


#if !defined(BUILD_RTM)
#define INVOKE_ARM_EMULATOR_INTERFACE(val) SUPPORT_StubForARMEmulatorInterface( val )
#else
#define INVOKE_ARM_EMULATOR_INTERFACE(val)
#endif

UINT32 SUPPORT_ComputeCRC( const void* rgBlock, int nLength, UINT32 crc );

int LZ77_Decompress( UINT8* inBuf, int inSize, UINT8* outBuf, int outSize );

extern "C" {

void SUPPORT_StubForARMEmulatorInterface( UINT32 data );

}

//--//

#if defined(_WIN32) || defined(WIN32) || defined(_WIN32_WCE)

bool LZ77_Compress( LPCWSTR inFileText, LPCWSTR outFileText, UINT8* prefix, size_t prefixLength );

#endif

////////////////////////////////////////////////////////////////////////////////////////////////////

#endif // _SUPPORT_TINYSUPPORT_H_
