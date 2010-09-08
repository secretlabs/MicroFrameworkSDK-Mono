////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef _K9F2G_8_H_1
#define _K9F2G_8_H_1   1

//--//

#define cmndAddress 0x40200000
#define addrAddress 0x40400000
#define dataAddress 0x40000000

//--//

struct K9F2G_8_BS_Driver
{
    typedef UINT8 CHIP_WORD;

    //--//
    
    static BOOL ChipInitialize( void* context );
    static BOOL ChipUnInitialize( void* context );
    static BOOL ReadProductID( void* context, FLASH_WORD& ManufacturerCode, FLASH_WORD& DeviceCode );
    static const BlockDeviceInfo* GetDeviceInfo( void* context);
    static BOOL Read( void* context, ByteAddress Address, UINT32 NumBytes, BYTE * pSectorBuff );
    static BOOL Write( void* context, ByteAddress Address, UINT32 NumBytes, BYTE * pSectorBuff, BOOL ReadModifyWrite );
    static BOOL Memset( void* context, ByteAddress Address, UINT8 Data, UINT32 NumBytes );
    static BOOL GetSectorMetadata(void* context, ByteAddress SectorStart, SectorMetadata* pSectorMetadata);
    static BOOL SetSectorMetadata(void* context, ByteAddress SectorStart, SectorMetadata* sectorMetadata);
    static BOOL IsBlockErased( void* context, ByteAddress BlockStartAddress, UINT32 BlockLength );
    static BOOL EraseBlock( void* context, ByteAddress Sector );
    static void SetPowerState( void* context, UINT32 State );
    static UINT32 MaxSectorWrite_uSec( void* context );
    static UINT32 MaxBlockErase_uSec( void* context );

private:

    static BOOL WriteX( void* context, ByteAddress StartSector, UINT32 NumBytes, BYTE * pSectorBuff, BOOL ReadModifyWrite, BOOL fIncrementDataPtr );
    static void WaitReady();
    static void EnableCE() { CPU_GPIO_SetPinState(AT91_NAND_CE, FALSE); };
    static void DisableCE() { CPU_GPIO_SetPinState(AT91_NAND_CE, TRUE); };
    static void WriteCommand(UINT8 command) { *((volatile UINT8 *)cmndAddress) = command; };
    static void WriteAddress(UINT8 Address) { *((volatile UINT8 *)addrAddress) = Address; };
    static void WriteData8(UINT8 Data) { *((volatile UINT8 *)dataAddress) = Data; };
    static UINT8 ReadData8() { return  (*((volatile UINT8 *)dataAddress)); };
    static BOOL ReadPage(ByteAddress StartSector, BYTE *outBuf, BYTE* inBuf, UINT32 size, UINT32 offset, UINT32 sectorSize, BOOL fIncrementDataPtr);
};

/// Nand flash commands
#define COMMAND_READ_1                  0x00
#define COMMAND_READ_2                  0x30
#define COMMAND_COPYBACK_READ_1         0x00
#define COMMAND_COPYBACK_READ_2         0x35
#define COMMAND_COPYBACK_PROGRAM_1      0x85
#define COMMAND_COPYBACK_PROGRAM_2      0x10
#define COMMAND_RANDOM_OUT              0x05
#define COMMAND_RANDOM_OUT_2            0xE0
#define COMMAND_RANDOM_IN               0x85
#define COMMAND_READID                  0x90
#define COMMAND_WRITE_1                 0x80
#define COMMAND_WRITE_2                 0x10
#define COMMAND_ERASE_1                 0x60
#define COMMAND_ERASE_2                 0xD0
#define COMMAND_STATUS                  0x70
#define COMMAND_RESET                   0xFF

/// Nand flash chip status codes
#define STATUS_READY                    (1 << 6)
#define STATUS_ERROR                    (1 << 0)

#endif // _K9F2G_8_H_1

