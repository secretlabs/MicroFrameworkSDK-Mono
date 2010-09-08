////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include <tinyhal.h>

namespace EmulatorBlockStorage = Microsoft::SPOT::Emulator::BlockStorage;

//--//

BlockDeviceInfo*    g_Emulator_BS_DevicesInfo;
BlockStorageDevice* g_Emulator_BS_Devices;
int                 g_Emulator_BS_NumDevices;

//--//

struct Emulator_BS_Driver
{

public:
    
    static BOOL Initialize( void* context );

    static BOOL Uninitialize( void* context );

    static const BlockDeviceInfo* GetDeviceInfo( void* context );

    static BOOL Read( void* context, ByteAddress Address, UINT32 NumBytes, BYTE * pSectorBuff );

    static BOOL Write( void* context, ByteAddress Address, UINT32 NumBytes, BYTE * pSectorBuff, BOOL ReadMoifyWrite );

    static BOOL Memset( void* context, ByteAddress Address, UINT8 Data, UINT32 NumBytes ); 

    static BOOL GetSectorMetadata(void* context, ByteAddress SectorStart, SectorMetadata* pSectorMetadata);

    static BOOL SetSectorMetadata(void* context, ByteAddress SectorStart, SectorMetadata* pSectorMetadata);

    static BOOL IsBlockErased( void* context, ByteAddress Address, UINT32 BlockLength );

    static BOOL EraseBlock( void* context, ByteAddress Address );

    static void SetPowerState( void* context, UINT32 State );

    static BOOL FindForBlockUsage( void* context, UINT32 BlockUsage, ByteAddress& Address, UINT32& RegionIndex, UINT32& RangeIndex );
    
    static UINT32 MaxSectorWrite_uSec( void* context );

    static UINT32 MaxBlockErase_uSec( void* context );

private:

};


//--//

BOOL Emulator_BS_Driver::Initialize( void* context )
{
    return EmulatorNative::GetIBlockStorageDriver()->Initialize( (UINT32)context );
}

BOOL Emulator_BS_Driver::Uninitialize( void* context )
{
    return EmulatorNative::GetIBlockStorageDriver()->Uninitialize( (UINT32)context );
}

const BlockDeviceInfo* Emulator_BS_Driver::GetDeviceInfo( void* context )
{
    int index = (int)context;

    if (index >= g_Emulator_BS_NumDevices) return NULL;
    
    return &(g_Emulator_BS_DevicesInfo[index]);
}

BOOL Emulator_BS_Driver::Read( void* context, ByteAddress Address, UINT32 NumBytes, BYTE* pSectorBuff )
{
    return EmulatorNative::GetIBlockStorageDriver()->Read( (UINT32)context, Address, NumBytes, IntPtr(pSectorBuff));
}

BOOL Emulator_BS_Driver::Write( void* context, ByteAddress Address, UINT32 NumBytes, BYTE * pSectorBuff, BOOL ReadModifyWrite )
{
    return EmulatorNative::GetIBlockStorageDriver()->Write( (UINT32)context, Address, NumBytes, IntPtr(pSectorBuff), (TRUE == ReadModifyWrite) );
}

BOOL Emulator_BS_Driver::Memset( void* context, ByteAddress Address, UINT8 Data, UINT32 NumBytes )
{
    return EmulatorNative::GetIBlockStorageDriver()->Memset( (UINT32)context, Address, Data, NumBytes );
}

BOOL Emulator_BS_Driver::GetSectorMetadata( void* context, ByteAddress SectorStart, SectorMetadata* pSectorMetadata )
{
    EmulatorBlockStorage::BsSectorMetadata^ managedMetadata = gcnew EmulatorBlockStorage::BsSectorMetadata();

    BOOL result = EmulatorNative::GetIBlockStorageDriver()->GetSectorMetadata((UINT32)context, SectorStart, (EmulatorBlockStorage::BsSectorMetadata%)*managedMetadata);

    if(result)
    {
        pSectorMetadata->dwReserved1  = managedMetadata->reserved1;
        pSectorMetadata->bOEMReserved = managedMetadata->oemReserved;
        pSectorMetadata->bBadBlock    = managedMetadata->badBlock;
        pSectorMetadata->wReserved2   = managedMetadata->reserved2;
        pSectorMetadata->ECC[0]       = managedMetadata->ECC0;
        pSectorMetadata->ECC[1]       = managedMetadata->ECC1;
    }

    return result;
}

BOOL Emulator_BS_Driver::SetSectorMetadata( void* context, ByteAddress SectorStart, SectorMetadata* pSectorMetadata )
{
    EmulatorBlockStorage::BsSectorMetadata^ managedMetadata = gcnew EmulatorBlockStorage::BsSectorMetadata();

    managedMetadata->reserved1   = pSectorMetadata->dwReserved1;
    managedMetadata->oemReserved = pSectorMetadata->bOEMReserved;
    managedMetadata->badBlock    = pSectorMetadata->bBadBlock;
    managedMetadata->reserved2   = pSectorMetadata->wReserved2;
    managedMetadata->ECC0        = pSectorMetadata->ECC[0];
    managedMetadata->ECC1        = pSectorMetadata->ECC[1];

    return EmulatorNative::GetIBlockStorageDriver()->SetSectorMetadata((UINT32)context, SectorStart, (EmulatorBlockStorage::BsSectorMetadata%)*managedMetadata);
}

BOOL Emulator_BS_Driver::IsBlockErased( void* context, ByteAddress Address, UINT32 BlockLength )
{
    return EmulatorNative::GetIBlockStorageDriver()->IsBlockErased( (UINT32)context, Address, BlockLength );
}

BOOL Emulator_BS_Driver::EraseBlock( void* context, ByteAddress Address )
{
    return EmulatorNative::GetIBlockStorageDriver()->EraseBlock( (UINT32)context, Address );
}

void Emulator_BS_Driver::SetPowerState( void* context, UINT32 State )
{
    EmulatorNative::GetIBlockStorageDriver()->SetPowerState( (UINT32)context, State );
}

BOOL Emulator_BS_Driver::FindForBlockUsage( void* context, UINT32 BlockUsage, ByteAddress& Address, UINT32& RegionIndex, UINT32& RangeIndex )
{
    return GetDeviceInfo(context)->FindForBlockUsage( BlockUsage, Address, RegionIndex, RangeIndex );
}

UINT32 Emulator_BS_Driver::MaxSectorWrite_uSec( void* context )
{
    return EmulatorNative::GetIBlockStorageDriver()->MaxSectorWrite_uSec( (UINT32)context );
}

UINT32 Emulator_BS_Driver::MaxBlockErase_uSec( void* context )
{
    return EmulatorNative::GetIBlockStorageDriver()->MaxBlockErase_uSec( (UINT32)context );
}

struct IBlockStorageDevice Emulator_BS_DeviceTable =
{
    &Emulator_BS_Driver::Initialize,
    &Emulator_BS_Driver::Uninitialize,
    &Emulator_BS_Driver::GetDeviceInfo,
    &Emulator_BS_Driver::Read,
    &Emulator_BS_Driver::Write,
    &Emulator_BS_Driver::Memset,
    &Emulator_BS_Driver::GetSectorMetadata,
    &Emulator_BS_Driver::SetSectorMetadata,
    &Emulator_BS_Driver::IsBlockErased,
    &Emulator_BS_Driver::EraseBlock,
    &Emulator_BS_Driver::SetPowerState,
    &Emulator_BS_Driver::MaxSectorWrite_uSec,
    &Emulator_BS_Driver::MaxBlockErase_uSec,    
};

//--//

void BlockStorage_AddDevices()
{
    BlockDeviceInfo* deviceInfo;
    BlockRegionInfo* regionInfo;
    int numDevices, numRegions, numRanges;
    
    array<InternalBlockDeviceInfo>^ devices;
    array<InternalBlockRegionInfo>^ regions;
    array<InternalBlockRange>^ ranges;

    devices = EmulatorNative::GetIBlockStorageDriver()->GetDevicesInfo();

    numDevices = devices->Length;

    g_Emulator_BS_DevicesInfo = new BlockDeviceInfo   [numDevices];
    g_Emulator_BS_Devices     = new BlockStorageDevice[numDevices];
    g_Emulator_BS_NumDevices  = numDevices;

    memset( g_Emulator_BS_Devices, 0, sizeof(BlockStorageDevice)*numDevices );

    for(int i = 0; i < numDevices; i++)
    {
        deviceInfo = &(g_Emulator_BS_DevicesInfo[i]);

        deviceInfo->Attribute.Removable      = devices[i].Removable;
        deviceInfo->Attribute.SupportsXIP    = devices[i].SupportsXIP;
        deviceInfo->Attribute.WriteProtected = devices[i].WriteProtected;

        deviceInfo->MaxSectorWrite_uSec = devices[i].MaxSectorWrite_uSec;
        deviceInfo->MaxBlockErase_uSec  = devices[i].MaxBlockErase_uSec;
        deviceInfo->BytesPerSector      = devices[i].BytesPerSector;

        // If a device is removable, it might not have regions (when there's no media present)
        if(!devices[i].Removable)
        {
            regions    = devices[i].Regions;
            numRegions = regions->Length;

            deviceInfo->NumRegions     = numRegions;
            deviceInfo->Regions        = new BlockRegionInfo[numRegions];

            for(int j = 0; j < numRegions; j++)
            {
                regionInfo = (BlockRegionInfo*)&(deviceInfo->Regions[j]);
                numRanges  = regions[j].NumBlockRanges;
                
                regionInfo->Start          = regions[j].Start;
                regionInfo->NumBlocks      = regions[j].NumBlocks;
                regionInfo->NumBlockRanges = numRanges;
                regionInfo->BytesPerBlock  = regions[j].BytesPerBlock;
                regionInfo->BlockRanges    = new BlockRange[numRanges];

                ranges = regions[j].BlockRanges;

                for(int k = 0; k < numRanges; k++)
                {
                    BlockRange *pRange = (BlockRange*)&regionInfo->BlockRanges[k];

                    pRange->RangeType  = ranges[k].RangeType;
                    pRange->StartBlock = ranges[k].StartBlock;
                    pRange->EndBlock   = ranges[k].EndBlock;
                }
            }

            // The index into the g_Emulator_BS_Devices will be used as context so the emulator
            // block storage driver can differentiate between different devices
            BlockStorageList::AddDevice( &(g_Emulator_BS_Devices[i]), &Emulator_BS_DeviceTable, (void*)i, FALSE );
        }
        else
        {
            deviceInfo->NumRegions = 0;
            deviceInfo->Regions = NULL;
        }
    }
}

