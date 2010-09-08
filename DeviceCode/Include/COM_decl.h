////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef _DRIVERS_COM_DIRECTOR_DECL_H_
#define _DRIVERS_COM_DIRECTOR_DECL_H_ 1

//--//

BOOL DebuggerPort_Initialize  ( COM_HANDLE ComPortNum );
BOOL DebuggerPort_Uninitialize( COM_HANDLE ComPortNum );

int  DebuggerPort_Write( COM_HANDLE ComPortNum, const char* Data, size_t size );
int  DebuggerPort_Read ( COM_HANDLE ComPortNum, char*       Data, size_t size );
BOOL DebuggerPort_Flush( COM_HANDLE ComPortNum                                );

//--//

void CPU_ProtectCommunicationGPIOs( BOOL On );

//--//

void CPU_InitializeCommunication();

void CPU_UninitializeCommunication();

//--//

#endif // _DRIVERS_COM_DIRECTOR_DECL_H_
