////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "sddl.h"

////////////////////////////////////////////////////////////////////////////////////////////////////

using namespace Microsoft::SPOT::Emulator;

BOOL USART_Initialize( int ComPortNum, int BaudRate, int Parity, int DataBits, int StopBits, int FlowValue )
{
    return EmulatorNative::GetISerialDriver()->Initialize(ComPortNum, BaudRate, Parity, DataBits, StopBits, FlowValue);
}

BOOL USART_Uninitialize( int ComPortNum )
{
    return EmulatorNative::GetISerialDriver()->Uninitialize( ComPortNum );    
}

int USART_Write( int ComPortNum, const char* Data, size_t size )
{       
    return EmulatorNative::GetISerialDriver()->Write( ComPortNum, (System::IntPtr)(void*)Data, (int)size );     
}

int USART_Read( int ComPortNum, char* Data, size_t size )
{
    return EmulatorNative::GetISerialDriver()->Read( ComPortNum, (System::IntPtr)(void*)Data, (int)size );         
}

BOOL USART_Flush( int ComPortNum )
{
    return EmulatorNative::GetISerialDriver()->Flush( ComPortNum );    
}

BOOL USART_AddCharToRxBuffer( int ComPortNum, char c )
{
    return EmulatorNative::GetISerialDriver()->AddCharToRxBuffer( ComPortNum, c );
}

BOOL USART_RemoveCharFromTxBuffer( int ComPortNum, char& c )
{
    return EmulatorNative::GetISerialDriver()->RemoveCharFromTxBuffer( ComPortNum, (wchar_t %)c);
}

INT8 USART_PowerSave( int ComPortNum, INT8 Enable )
{
    return EmulatorNative::GetISerialDriver()->PowerSave( ComPortNum, Enable ); 
}

void USART_PrepareForClockStop()
{
    EmulatorNative::GetISerialDriver()->PrepareForClockStop();
}

void USART_ClockStopFinished()
{
    EmulatorNative::GetISerialDriver()->ClockStopFinished();    
}

void USART_CloseAllPorts()
{
    EmulatorNative::GetISerialDriver()->CloseAllPorts();    
}

int  USART_BytesInBuffer( int ComPortNum, BOOL fRx )
{
    return EmulatorNative::GetISerialDriver()->BytesInBuffer( ComPortNum, (fRx == TRUE) );
}

void USART_DiscardBuffer( int ComPortNum, BOOL fRx )
{
    EmulatorNative::GetISerialDriver()->DiscardBuffer( ComPortNum, (fRx == TRUE) );
}

UINT32 CPU_USART_PortsCount()
{
    return EmulatorNative::GetISerialDriver()->PortsCount();
}

void CPU_USART_GetPins( int ComPortNum, GPIO_PIN& rxPin, GPIO_PIN& txPin,GPIO_PIN& ctsPin, GPIO_PIN& rtsPin )
{   
    EmulatorNative::GetISerialDriver()->GetPins(ComPortNum, rxPin, txPin, ctsPin, rtsPin );
}

BOOL CPU_USART_SupportNonStandardBaudRate ( int ComPortNum )
{
    return EmulatorNative::GetISerialDriver()->SupportNonStandardBaudRate(ComPortNum);
}

void CPU_USART_GetBaudrateBoundary( int ComPortNum, UINT32& maxBaudrateHz, UINT32& minBaudrateHz )
{
    EmulatorNative::GetISerialDriver()->BaudrateBoundary(ComPortNum, maxBaudrateHz, minBaudrateHz);
}

BOOL CPU_USART_IsBaudrateSupported( int ComPortNum, UINT32 & BaudrateHz )
{   
    return EmulatorNative::GetISerialDriver()->IsBaudrateSupported(ComPortNum, BaudrateHz);
}

BOOL USART_ConnectEventSink( int ComPortNum, int EventType, void* pContext, PFNUsartEvent pfnUsartEvtHandler, void** ppArg )
{
    return TRUE;
}

void USART_SetEvent( int ComPortNum, unsigned int event )
{
}

