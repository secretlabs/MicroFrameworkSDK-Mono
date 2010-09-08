////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


struct SD_BLOCK_CONFIG
{
    GPIO_FLAG WriteProtectionPin;
    BlockDeviceInfo *BlockDeviceInformation;
};

struct SD_BL_CONFIGURATION
{
    SPI_CONFIGURATION   SPI;
    GPIO_PIN            InsertIsrPin;
    GPIO_PIN            EjectIsrPin;
    BOOL                State_After_Erase;
    BOOL                Low_Voltage_Flag;
    BlockStorageDevice* Device;
};

struct SD_BS_Driver
{
    typedef BYTE CHIP_WORD;

    #define SD_DATA_SIZE  512

    #define SD_CMD_SIZE   6 

    #define DUMMY   0xff      

    #define RESPONSE_TIME_OUT     0xFFFFFFFF

    #define CSD_CID_LENGTH        0x10

    #define SCR_LENGTH            0x8

    #define CMD8_CHECK_PATTERN    0xAA

    #define SD_GO_IDLE_STATE      0x00            // CMD0
    #define SD_SEND_IF_COND       0x08            // CMD8
    #define SD_SEND_CSD           0x09            // CMD9
    #define SD_SEND_CID           0x0A            // CMD10
    #define SD_READ_SINGLE_BLOCK  0x11            // CMD17
    #define SD_SET_BLOCKLEN       0x10            // CMD16
    #define SD_WRITE_SINGLE_BLOCK 0x18            // CMD24
    #define SD_ERASE_WR_BLK_START 0x20            // CMD32
    #define SD_ERASE_WR_BLK_END   0x21            // CMD33
    #define SD_ERASE              0x26            // CMD38
    #define SD_APP_CMD            0x37            // CMD55
    #define SD_SEND_OP_COND       0x29            // ACMD41
    #define SD_SEND_SCR           0x33            // ACMD51

    #define SD_START_DATA_BLOCK_TOKEN   0xfe      

    // R1 response code
    #define R1_IN_READY_STATUS    0x00
    #define R1_IN_IDLE_STATUS     0x01
    #define R1_ERASE_RESET        0x02
    #define R1_ILLEGAL_COOMMAND   0x04
    #define R1_COM_CRC_ERROR      0x08
    #define R1_ERASE_SEQ_ERROR    0x10
    #define R1_ADDRESS_ERROR      0x20
    #define R1_PARAMETER_ERROR    0x40

    // SD operation result 
    #define SD_SUCCESS            0x00
    #define SD_BLOCK_SET_ERROR    0x01
    #define SD_RESPONSE_ERROR     0x02
    #define SD_DATA_TOKEN_ERROR   0x03
    #define SD_INIT_ERROR         0x04
    #define SD_CRC_ERROR          0x10
    #define SD_WRITE_ERROR        0x11
    #define SD_OTHER_ERROR        0x12
    #define SD_TIMEOUT_ERROR      0xFF

    //--//

    static BOOL ChipInitialize(void *context);

    static BOOL ChipUnInitialize(void *context);

    static const BlockDeviceInfo *GetDeviceInfo(void *context);

    static BOOL Read(void *context, ByteAddress Address, UINT32 NumBytes, BYTE *pSectorBuff);

    static BOOL Write(void *context, ByteAddress Address, UINT32 NumBytes, BYTE *pSectorBuff, BOOL ReadModifyWrite);

    static BOOL Memset(void *context, ByteAddress Address, UINT8 Data, UINT32 NumBytes);

    static BOOL GetSectorMetadata(void* context, ByteAddress SectorStart, SectorMetadata* pSectorMetadata);

    static BOOL SetSectorMetadata(void* context, ByteAddress SectorStart, SectorMetadata* pSectorMetadata);

    static BOOL IsBlockErased(void *context, ByteAddress BlockStartAddress, UINT32 BlockLength);

    static BOOL EraseBlock(void *context, ByteAddress Sector);

    static void SetPowerState(void *context, UINT32 State);

    static UINT32 MaxSectorWrite_uSec(void *context);

    static UINT32 MaxBlockErase_uSec(void *context);

    static BOOL ChipReadOnly(void *context, BOOL On, UINT32 ProtectionKey);

    static BOOL ReadProductID(void *context, BYTE *ManufacturerCode, BYTE *OEMID, BYTE *ProductName);

  private:
    static BOOL EraseSectors(SectorAddress Address, INT32 SectorCount);
    static BOOL ReadSector(SectorAddress Address, UINT32 Offset, UINT32 NumBytes, BYTE *pSectorBuff, UINT32 BytesPerSector);
    static BOOL WriteX(void *context, ByteAddress Address, UINT32 NumBytes, BYTE *pSectorBuff, BOOL ReadModifyWrite, BOOL fIncrementDataPtr);
    static void InsertISR(GPIO_PIN Pin, BOOL PinState, void* Param);
    static void EjectISR(GPIO_PIN Pin, BOOL PinState, void* Param);

    static BYTE SPISendByte(BYTE data);
    static void SD_CsSetHigh();
    static void SD_CsSetLow();
    static BYTE SD_SendCmdWithR1Resp(BYTE cmd, UINT32 arg, BYTE crc, BYTE expectedToken, INT32 iterations=100);
    static BOOL SD_SendCmdWithR7Resp(BYTE cmd, UINT32 arg, BYTE *outVoltage);
    static BYTE SD_CheckBusy(void);
    static BOOL ReadSectorHelper(void *context, ByteAddress StartSector, UINT32 NumSectors, BYTE *pSectorBuff, SectorMetadata *pSectorMetadata);
    static void SPISendCount(BYTE *pWrite, UINT32 WriteCount);
    static void SPIRecvCount(BYTE *pRead, UINT32 ReadCount, UINT32 Offset);
};
