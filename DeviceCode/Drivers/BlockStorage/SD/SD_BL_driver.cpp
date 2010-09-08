////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include <tinyhal.h>
#include "SD_BL.h"

//--//

extern struct SD_BL_CONFIGURATION g_SD_BL_Config;

#define SD_PHYISCAL_BASE_ADDRESS 0

static BYTE s_sectorBuff[SD_DATA_SIZE];
static BYTE s_cmdBuff[SD_CMD_SIZE];


//--//

static UINT8 CRC7Encode(UINT8 crc, UINT8 *input, int length)
{
    for(int j = 0; j < length; j++)
    {
        UINT8 byte = input[j];

        for(int i = 8; i > 0; i--)
        {
            crc = (crc << 1) | ((byte &0x80) ? 1 : 0);
            if(crc &0x80)
            {
                crc ^= 9; // polynomial
            } 
            byte <<= 1;
        }
        crc &= 0x7f;
    }

    for(int i = 7; i > 0; i--)
    {
        crc = (crc << 1);
        if(crc &0x80)
        {
            crc ^= 9; // polynomial
        }
    }

    crc &= 0x7f;

    return crc;
}

BYTE SD_BS_Driver::SPISendByte(BYTE data)
{
    SPI_XACTION_8 config;
    BYTE ReadByte = 0;

    config.Read8 = &ReadByte;
    config.ReadCount = 1;
    config.ReadStartOffset = 0;
    config.SPI_mod = g_SD_BL_Config.SPI.SPI_mod;
    config.Write8 = &data;
    config.WriteCount = 1;
    config.BusyPin.Pin = GPIO_PIN_NONE;

    CPU_SPI_Xaction_nWrite8_nRead8(config);

    return ReadByte;
}

void SD_BS_Driver::SPISendCount(BYTE *pWrite, UINT32 WriteCount)
{
    SPI_XACTION_8 config;

    ASSERT((pWrite != NULL) && (WriteCount != 0));

    config.Read8 = NULL;
    config.ReadCount = 0;
    config.ReadStartOffset = 0;
    config.SPI_mod = g_SD_BL_Config.SPI.SPI_mod;
    config.Write8 = pWrite;
    config.WriteCount = WriteCount;
    config.BusyPin.Pin = GPIO_PIN_NONE;

    CPU_SPI_Xaction_nWrite8_nRead8(config);
}

void SD_BS_Driver::SPIRecvCount(BYTE *pRead, UINT32 ReadCount, UINT32 Offset)
{
    SPI_XACTION_8 config;

    BYTE dummy = DUMMY;

    ASSERT((pRead != NULL) && (ReadCount != 0));

    config.Read8 = pRead;
    config.ReadCount = ReadCount;
    config.ReadStartOffset = Offset;
    config.SPI_mod = g_SD_BL_Config.SPI.SPI_mod;
    config.Write8 = &dummy;
    config.WriteCount = 1;
    config.BusyPin.Pin = GPIO_PIN_NONE;

    CPU_SPI_Xaction_nWrite8_nRead8(config);

}

void SD_BS_Driver::SD_CsSetHigh()
{
    CPU_GPIO_EnableOutputPin(g_SD_BL_Config.SPI.DeviceCS, !g_SD_BL_Config.SPI.CS_Active);
}

void SD_BS_Driver::SD_CsSetLow()
{
    CPU_GPIO_EnableOutputPin(g_SD_BL_Config.SPI.DeviceCS, g_SD_BL_Config.SPI.CS_Active);
}

BYTE SD_BS_Driver::SD_CheckBusy(void)
{
    BYTE response;
    BYTE rvalue = 0xFF;

    for(int i=10000; i!=0; i--)
    {
        response = SPISendByte(0xff);

        if(response != 0xFF && rvalue == 0xFF)
        {
            response &= 0x1f;
            switch(response) /* 7 6 5 4 3    1 0  */
            {
                /* data response  x x x 0 status 1 */
                case 0x05:
                    rvalue = SD_SUCCESS;
                    break;

                case 0x0b:
                    return (SD_CRC_ERROR);

                case 0x0d:
                    return (SD_WRITE_ERROR);

                default:
                    rvalue = SD_OTHER_ERROR;
                    break;
            }
        }
        else if(response != 0x00 && rvalue != 0xFF)
        {
            break;
        }

        HAL_Time_Sleep_MicroSeconds(10);
    }

    return rvalue;
}

//return Card status in R1 response
BYTE SD_BS_Driver::SD_SendCmdWithR1Resp(BYTE cmd, UINT32 arg, BYTE crc, BYTE expectedToken, INT32 iterations)
{
    BYTE response;
    BYTE retVal = 0xFF;

    s_cmdBuff[0] = (0x40 | cmd); // command
    s_cmdBuff[1] = ((BYTE)((arg >> 24) &0xff)); // parameter
    s_cmdBuff[2] = ((BYTE)((arg >> 16) &0xff));
    s_cmdBuff[3] = ((BYTE)((arg >> 8) &0xff));
    s_cmdBuff[4] = ((BYTE)((arg >> 0) &0xff));
    s_cmdBuff[5] = (crc); // CRC check code

    SPISendByte(DUMMY);

    SPISendCount(s_cmdBuff, SD_CMD_SIZE);

    for(int i = 0; i < iterations; i++)
    {
        response = SPISendByte(DUMMY);

        if(response == expectedToken)
        {
            return expectedToken;
        }
        else if(response != 0xFF && retVal == 0xFF)
        {
            retVal = response;
        }
    }

    return retVal;
}

//return support voltage range in R3response
BOOL SD_BS_Driver::SD_SendCmdWithR7Resp(BYTE cmd, UINT32 arg, BYTE *outVoltage)
{
    BYTE response;
    BYTE i, echo_back;

    ASSERT(outVoltage != NULL);

    *outVoltage = 0xFF;

    s_cmdBuff[0] = (0x40 | cmd); // tansmition bit | command
    s_cmdBuff[1] = ((BYTE)((arg >> 24) &0xff)); // parameter
    s_cmdBuff[2] = ((BYTE)((arg >> 16) &0xff));
    s_cmdBuff[3] = ((BYTE)((arg >> 8) &0xff));
    s_cmdBuff[4] = ((BYTE)((arg >> 0) &0xff));
    s_cmdBuff[5] = (CRC7Encode(0, s_cmdBuff, 5) << 1) | 1; // CRC | ENDBIT

    SPISendByte(DUMMY);

    SPISendCount(s_cmdBuff, SD_CMD_SIZE);

    for(i = 0; i < 100; i++)
    {
        response = SPISendByte(DUMMY);
        if(response != 0xFF)
        // begin token of R7 response
            break;
    }

    if(i == 100)
        return FALSE;

    //recieve voltage content in R7 response
    SPISendByte(DUMMY);
    SPISendByte(DUMMY);
    *outVoltage = SPISendByte(DUMMY);
    echo_back = SPISendByte(DUMMY);

    if(echo_back != s_cmdBuff[4])
    {
        return FALSE;
    }

    return TRUE;
}

BOOL SD_BS_Driver::ChipInitialize(void *context)
{
    SD_BLOCK_CONFIG *config = (SD_BLOCK_CONFIG*)context;

    if(!config || !config->BlockDeviceInformation)
    {
        return FALSE;
    }

    BlockDeviceInfo* pDevInfo = config->BlockDeviceInformation;

    UINT32 clkNormal = g_SD_BL_Config.SPI.Clock_RateKHz;

    g_SD_BL_Config.SPI.Clock_RateKHz = 400; // initialization clock speed

    //one test for insert \ eject ISR
    if(g_SD_BL_Config.InsertIsrPin != GPIO_PIN_NONE)
    {
        CPU_GPIO_EnableInputPin(g_SD_BL_Config.InsertIsrPin, TRUE, InsertISR, GPIO_INT_EDGE_LOW, RESISTOR_PULLUP);
    }
    if(g_SD_BL_Config.EjectIsrPin != GPIO_PIN_NONE)
    {
        CPU_GPIO_EnableInputPin(g_SD_BL_Config.EjectIsrPin, TRUE, EjectISR, GPIO_INT_EDGE_LOW, RESISTOR_PULLUP);
    }

    CPU_SPI_Initialize();

    CPU_SPI_Xaction_Start(g_SD_BL_Config.SPI);

    BYTE response;
    int i = 0;

    SD_CsSetHigh();

    //need 74 clock for initialize
    for(i = 0; i < 10; i++)
    {
        SPISendByte(DUMMY);
    }

    SD_CsSetLow();

    // send CMD0, turn to IDLE state
    for(i = 0; i < 10; i++)
    {
        response = SD_SendCmdWithR1Resp(SD_GO_IDLE_STATE, 0, 0x95, R1_IN_IDLE_STATUS);

        if(response == R1_IN_IDLE_STATUS)
            break;
    }

    if(response != R1_IN_IDLE_STATUS)
    {
        SD_CsSetHigh();
        CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);
        return FALSE;
    }

    //send CMD8, check voltage range support 
    UINT32 CMD8_Arg = CMD8_CHECK_PATTERN;
    BYTE support_voltage = 0;
    UINT32 supply_voltage;

    if(g_SD_BL_Config.Low_Voltage_Flag == TRUE)
        supply_voltage = 2;
    else
        supply_voltage = 1;

    CMD8_Arg |= (supply_voltage << 8);
    if(SD_SendCmdWithR7Resp(SD_SEND_IF_COND, CMD8_Arg, &support_voltage))
    {
        if(support_voltage != supply_voltage) return FALSE;
    }

    //send CMD55 + ACMD41 until return 0x00
    for(i=0; i<0x7fff; i++)
    {
        SD_SendCmdWithR1Resp(SD_APP_CMD, 0, 0xFF, R1_IN_IDLE_STATUS);

        response = SD_SendCmdWithR1Resp(SD_SEND_OP_COND, 0, 0xFF, R1_IN_READY_STATUS);

        if(response == R1_IN_READY_STATUS)
        {
            break;
        }
    }

    if(response != R1_IN_READY_STATUS)
    {
        SD_CsSetHigh();
        CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);
        return FALSE;
    }

    // send CMD16, set block length to 512
    response = SD_SendCmdWithR1Resp(SD_SET_BLOCKLEN, 512, 0xFF, R1_IN_READY_STATUS);

    if(response != R1_IN_READY_STATUS)
    {
        SD_CsSetHigh();
        CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);
        return FALSE;
    }    

    //send CMD9  to get CSD
    BYTE regCSD[16];

    BYTE C_SIZE_MULT = 0;

    BYTE TAAC, NSAC, MAX_RAN_SPEED, READ_BL_LEN, SECTOR_SIZE;

    BOOL ERASE_BL_EN;

    UINT32 C_SIZE;

    UINT32 MemCapacity = 0; //total memory size, in unit of byte

    UINT32 Max_Trans_Speed = 0; //in unit of Hz

    response = SD_SendCmdWithR1Resp(SD_SEND_CSD, 0, 0xFF, SD_START_DATA_BLOCK_TOKEN);

    if(response != SD_START_DATA_BLOCK_TOKEN)
    {
        SD_CsSetHigh();
        CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);
        return FALSE;
    }
    else
    {
        // receive one sector data
        SPIRecvCount(regCSD, CSD_CID_LENGTH, 0);

        // receive 16 bit CRC
        SPISendByte(DUMMY);
        SPISendByte(DUMMY);

        TAAC = regCSD[1];
        NSAC = regCSD[2];
        MAX_RAN_SPEED = regCSD[3];
        READ_BL_LEN = regCSD[5] &0x0F;

        if(MAX_RAN_SPEED == 0x32)
            Max_Trans_Speed = 25000000;
        //normal mode
        else if(MAX_RAN_SPEED == 0x5A)
            Max_Trans_Speed = 50000000;
        //High-Speed mode

        if(regCSD[0] == 0x00)
        //SD spec version1.0
        {
            C_SIZE = ((regCSD[6] &0x3) << 10) | (regCSD[7] << 2) | ((regCSD[8] &0xC0) >> 6);

            C_SIZE_MULT = ((regCSD[9] &0x03) << 1) | ((regCSD[10] &0x80) >> 7);

            ERASE_BL_EN = ((regCSD[10] &0x40) == 0x00) ? FALSE : TRUE;

            SECTOR_SIZE = ((regCSD[10] &0x3F) << 1) | ((regCSD[11] &0x80) >> 7);

            MemCapacity = (C_SIZE + 1)*(0x1 << (C_SIZE_MULT + 2))*(0x1 << READ_BL_LEN);
        }
        else
        //SD spec version2.0
        {
            C_SIZE = ((regCSD[7] &0x3F) << 16) | (regCSD[8] << 8) | regCSD[9];

            ERASE_BL_EN = ((regCSD[10] &0x40) == 0x00) ? FALSE : TRUE;

            SECTOR_SIZE = ((regCSD[10] &0x3F) << 1) | ((regCSD[11] &0x80) >> 7);

            MemCapacity = (C_SIZE + 1) *512 * 1024;
        }

#if 0
        UINT8 crc = (CRC7Encode(0, regCSD, 15) << 1) | 1;

        if(crc != regCSD[15])
        {
            lcd_printf("Wrong CRC for CSD register!\r\n");
            debug_printf("Wrong CRC for CSD register!\r\n");
        }
#endif

        //Update SD config according to CSD register
        UINT32 SectorsPerBlock    = (ERASE_BL_EN == TRUE) ? 1 : (SECTOR_SIZE + 1);
        pDevInfo->BytesPerSector  = 512; // data bytes per sector is always 512
        pDevInfo->Size            = MemCapacity;

        BlockRegionInfo* pRegions = (BlockRegionInfo*)&pDevInfo->Regions[0];
        pRegions[0].BytesPerBlock = SectorsPerBlock * pDevInfo->BytesPerSector;
        pRegions[0].NumBlocks     = MemCapacity / pRegions[0].BytesPerBlock;
        
        BlockRange* pRanges   = (BlockRange*)&pRegions[0].BlockRanges[0];

        pRanges[0].StartBlock = 0;
        pRanges[0].EndBlock   = pRegions[0].NumBlocks-1;
    }

    //CMD55+ACMD51 to get SCR register
    BYTE regSCR[8];

    SD_SendCmdWithR1Resp(SD_APP_CMD, 0, 0xFF, R1_IN_READY_STATUS);

    response = SD_SendCmdWithR1Resp(SD_SEND_SCR, 0, 0xFF, SD_START_DATA_BLOCK_TOKEN);

    if(response != SD_START_DATA_BLOCK_TOKEN)
    {
        SD_CsSetHigh();
        CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);
        return FALSE;
    }
    else
    {
        // receive one sector data
        SPIRecvCount(regSCR, SCR_LENGTH, 0);

        // receive 16 bit CRC
        SPISendByte(DUMMY);
        SPISendByte(DUMMY);

        g_SD_BL_Config.State_After_Erase = ((regSCR[1] &0x80) != 0x0);

    }

    //CMD10 to get CID

    BYTE regCID[16];

    BYTE ManufacturerCode;

    UINT16 OEMID;

    BYTE ProductName[5];

    response = SD_SendCmdWithR1Resp(SD_SEND_CID, 0, 0xFF, SD_START_DATA_BLOCK_TOKEN);

    if(response != SD_START_DATA_BLOCK_TOKEN)
    {
        SD_CsSetHigh();
        CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);
        return FALSE;
    }
    else
    {
        // receive one sector data
        SPIRecvCount(regCID, CSD_CID_LENGTH, 0);

        // receive 16 bit CRC
        SPISendByte(DUMMY);
        SPISendByte(DUMMY);

    }

    ManufacturerCode = regCID[0];

    memcpy(&OEMID, &regCID[1], 2);

    memcpy(&ProductName, &regCID[3], 5);

    SD_CsSetHigh();
    CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);

    g_SD_BL_Config.SPI.Clock_RateKHz = clkNormal;
    
    return TRUE;
}

void SD_BS_Driver::InsertISR(GPIO_PIN Pin, BOOL PinState, void *Param)
{
    FS_MountVolume("SD1", 0, 0, g_SD_BL_Config.Device);
}

void SD_BS_Driver::EjectISR(GPIO_PIN Pin, BOOL PinState, void *Param)
{
    FS_UnmountVolume(g_SD_BL_Config.Device);
}

BOOL SD_BS_Driver::ChipUnInitialize(void *context)
{    
    return TRUE;
}

BOOL SD_BS_Driver::ReadProductID(void *context, BYTE *ManufacturerCode, BYTE *OEMID, BYTE *ProductName)
{
    BYTE regCID[16];
    BYTE response;

    CPU_SPI_Xaction_Start(g_SD_BL_Config.SPI);

    // enable SD card
    SD_CsSetLow();

    //CMD10 to CID
    response = SD_SendCmdWithR1Resp(SD_SEND_CID, 0, 0xFF, SD_START_DATA_BLOCK_TOKEN);

    if(response != SD_START_DATA_BLOCK_TOKEN)
    {
        SD_CsSetHigh();
        CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);
        return FALSE;
    }
    else
    {
        // receive one sector data
        SPIRecvCount(regCID, CSD_CID_LENGTH, 0);

        // receive 16 bit CRC
        SPISendByte(DUMMY);
        SPISendByte(DUMMY);
    }

    *ManufacturerCode = regCID[0];

    memcpy(OEMID, &regCID[1], 2);

    memcpy(ProductName, &regCID[3], 5);

    CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);

    // disable SD card
    SD_CsSetHigh();

    return TRUE;
}

const BlockDeviceInfo *SD_BS_Driver::GetDeviceInfo(void *context)
{
    SD_BLOCK_CONFIG *config = (SD_BLOCK_CONFIG*)context;

    return config->BlockDeviceInformation;
}

BOOL SD_BS_Driver::ReadSector(SectorAddress sectorAddress, UINT32 Offset, UINT32 NumBytes, BYTE* pSectorBuff, UINT32 BytesPerSector)
{
    BYTE response;
    UINT32 leftOver;
    bool   fResponse = false;

    NumBytes = (NumBytes + Offset > BytesPerSector ? BytesPerSector - Offset : NumBytes);
    leftOver = BytesPerSector - (Offset + NumBytes);

    for(int i=0; i<10; i++)
    {
        // enable SD card
        SD_CsSetLow();

        // send CMD17 and wait for DATA_BLOCK_TOKEN
        response = SD_SendCmdWithR1Resp(SD_READ_SINGLE_BLOCK, sectorAddress << 9, 0xff, SD_START_DATA_BLOCK_TOKEN, 10000);

        if(response == SD_START_DATA_BLOCK_TOKEN)
        {
            fResponse = true;
            break;
        }

        SD_CsSetHigh();
    }

    if(fResponse)
    {
        // receive one sector data
        SPIRecvCount(pSectorBuff, BytesPerSector, Offset);

        while(leftOver--)
        {
            SPISendByte(DUMMY);
        }

        // receive 16 bit CRC
        SPISendByte(DUMMY);
        SPISendByte(DUMMY);
    }
    else
    //can't get valid response after CMD17
    {
        SD_CsSetHigh();
        return FALSE;
    }

    //disable select SD card
    SD_CsSetHigh();
    return TRUE;
}

BOOL SD_BS_Driver::Read(void *context, ByteAddress phyAddress, UINT32 NumBytes, BYTE *pSectorBuff)
{
    NATIVE_PROFILE_HAL_DRIVERS_FLASH();
    UINT32 RangeIndex;
    UINT32 RegionIndex;
    UINT32 BytesPerSector;

    BLOCK_CONFIG* pConfig = (BLOCK_CONFIG*)context;
    
    if(pConfig->BlockDeviceInformation->FindRegionFromAddress(phyAddress, RegionIndex, RangeIndex))
    {
        ByteAddress StartSector = pConfig->BlockDeviceInformation->PhysicalToSectorAddress( &pConfig->BlockDeviceInformation->Regions[RegionIndex], phyAddress);

        BytesPerSector = pConfig->BlockDeviceInformation->BytesPerSector;

        CHIP_WORD *pBuf = (CHIP_WORD*)pSectorBuff;

        UINT32 offset = phyAddress - (StartSector * pConfig->BlockDeviceInformation->BytesPerSector);

        UINT32 bytes  = (NumBytes + offset > BytesPerSector ? BytesPerSector - offset : NumBytes);

        CPU_SPI_Xaction_Start(g_SD_BL_Config.SPI);

        while(NumBytes > 0)
        {
            if(!ReadSector(StartSector, offset, bytes, pBuf, BytesPerSector))
            {
                CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);

                return FALSE;
            }
            
            offset    = 0;
            pBuf      = (CHIP_WORD*)((UINT32)pBuf + bytes);
            NumBytes -= bytes;
            StartSector++;

            bytes = __min(BytesPerSector, NumBytes);
        }

        CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);
        
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

BOOL SD_BS_Driver::Write(void *context, ByteAddress phyAddr, UINT32 NumBytes, BYTE *pSectorBuff, BOOL ReadModifyWrite )
{
    NATIVE_PROFILE_PAL_FLASH();

    return WriteX( context, phyAddr, NumBytes, pSectorBuff, ReadModifyWrite, TRUE );
}

BOOL SD_BS_Driver::Memset(void *context, ByteAddress phyAddr, UINT8 Data, UINT32 NumBytes )
{
    NATIVE_PROFILE_PAL_FLASH();

    return WriteX( context, phyAddr, NumBytes, &Data, TRUE, FALSE );
}

BOOL SD_BS_Driver::WriteX(void *context, ByteAddress phyAddr, UINT32 NumBytes, BYTE *pSectorBuff, BOOL ReadModifyWrite, BOOL fIncrementDataPtr )
{
    NATIVE_PROFILE_PAL_FLASH();

    UINT32 RangeIndex;
    UINT32 RegionIndex;
    UINT32 BytesPerSector;
    UINT32 offset;
    UINT32 bytes;
    BYTE response;

    BLOCK_CONFIG* pConfig = (BLOCK_CONFIG*)context;

    CHIP_WORD *pData, *pWrite;

    // find the corresponding region     
    if(!pConfig->BlockDeviceInformation->FindRegionFromAddress(phyAddr, RegionIndex, RangeIndex))
        return FALSE;

    ByteAddress StartSector = pConfig->BlockDeviceInformation->PhysicalToSectorAddress( &pConfig->BlockDeviceInformation->Regions[RegionIndex], phyAddr);

    pData = (CHIP_WORD*)pSectorBuff;
    BytesPerSector = pConfig->BlockDeviceInformation->BytesPerSector;

    offset = phyAddr - (StartSector * BytesPerSector);

    bytes = (NumBytes + offset > BytesPerSector ? BytesPerSector - offset : NumBytes);

    CPU_SPI_Xaction_Start(g_SD_BL_Config.SPI);

    while(NumBytes > 0)
    {
        // if we are using memset, or if the bytes written are less than the BytesPerSector then do read/modify/write
        if(!fIncrementDataPtr || (bytes != BytesPerSector))
        {   
            if(bytes != BytesPerSector)
            {
                if(!ReadSector(StartSector, 0, BytesPerSector, s_sectorBuff, BytesPerSector))
                {
                    CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);
                    return FALSE;
                }

            }
            
            pWrite = (CHIP_WORD*)&s_sectorBuff[0];

            if(fIncrementDataPtr)
            {
                memcpy(&pWrite[offset], pData, bytes);
            }
            else
            {
                memset(&pWrite[offset], *pData, bytes);
            }
        }
        else
        {
            pWrite = pData;
        }

        // select SD CS
        SD_CsSetLow();
        
        // send CMD24 --read single block data
        response = SD_SendCmdWithR1Resp(SD_WRITE_SINGLE_BLOCK, StartSector << 9, 0xff, R1_IN_READY_STATUS);

        if(response == R1_IN_READY_STATUS)
        {
            SPISendByte(SD_START_DATA_BLOCK_TOKEN); // send DATA_BLOCK_TOKEN

            // send data
            SPISendCount(pWrite, BytesPerSector);

            // send CRC
            SPISendByte(0xff);
            SPISendByte(0xff);

            // wait for end of write busy
            response = SD_CheckBusy();
        }

        //disable SD card CS
        SD_CsSetHigh();

        if(fIncrementDataPtr) pData = (CHIP_WORD*)((UINT32)pData + bytes);

        NumBytes   -= bytes;
        offset      = 0;
        StartSector++;
        bytes = __min(BytesPerSector, NumBytes);        
    }

    CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);

    return TRUE;

}

BOOL SD_BS_Driver::GetSectorMetadata(void* context, ByteAddress SectorStart, SectorMetadata* pSectorMetadata)
{
    return TRUE;
}

BOOL SD_BS_Driver::SetSectorMetadata(void* context, ByteAddress SectorStart, SectorMetadata* pSectorMetadata)
{
    return TRUE;
}

BOOL SD_BS_Driver::IsBlockErased(void *context, ByteAddress phyAddress, UINT32 BlockLength)
{

    NATIVE_PROFILE_HAL_DRIVERS_FLASH();

    UINT32 RegionIndex;
    UINT32 RangeIndex;
    UINT32 SectorsPerBlock;
    UINT32 BytesPerSector;

    BLOCK_CONFIG* pConfig = (BLOCK_CONFIG*)context;

    // this is static buffer, as the driver is current tailor for SD, a page size is 2048 bytes.
    BYTE *pSectorBuff = s_sectorBuff;


    BYTE state_After_Erase = g_SD_BL_Config.State_After_Erase ? 0xFF : 0x00;

    if(!pConfig->BlockDeviceInformation->FindRegionFromAddress(phyAddress, RegionIndex, RangeIndex))
        return FALSE;

    ByteAddress StartSector = pConfig->BlockDeviceInformation->PhysicalToSectorAddress( &pConfig->BlockDeviceInformation->Regions[RegionIndex], phyAddress);

    const BlockRegionInfo* pRegion = &pConfig->BlockDeviceInformation->Regions[RegionIndex];

    // as the input arg Sector may not be the startSector address of a block,
    // we need to recalculate it.
    BytesPerSector  = pConfig->BlockDeviceInformation->BytesPerSector;
    SectorsPerBlock = (pRegion->BytesPerBlock / BytesPerSector);

    StartSector = (StartSector / SectorsPerBlock) * SectorsPerBlock;
    
    for(UINT32 i = 0; i < SectorsPerBlock; i++)
    {
        SD_BS_Driver::Read(context, StartSector, BytesPerSector, pSectorBuff);
        for(UINT32 j = 0; j < BytesPerSector; j++)
        {
            if(pSectorBuff[j] != state_After_Erase)
            {
                return FALSE;
            }
        }
    }
    return TRUE;
}


BOOL SD_BS_Driver::EraseSectors(SectorAddress Address, INT32 SectorCount)
{
    BYTE response;

    SD_CsSetLow(); // cs low

    //send ERASE_WR_BLK_START command
    response = SD_SendCmdWithR1Resp(SD_ERASE_WR_BLK_START, Address << 9, 0xff, R1_IN_READY_STATUS);

    if(response != R1_IN_READY_STATUS)
    {
        SD_CsSetHigh();
        return FALSE;
    }

    //send ERASE_WR_BLK_END command
    response = SD_SendCmdWithR1Resp(SD_ERASE_WR_BLK_END, (Address + SectorCount - 1) << 9, 0xff, R1_IN_READY_STATUS);

    if(response != R1_IN_READY_STATUS)
    {
        SD_CsSetHigh();
        return FALSE;
    }

    // send erase command
    response = SD_SendCmdWithR1Resp(SD_ERASE, 0xffffffff, 0xff, R1_IN_READY_STATUS);

    if(response != R1_IN_READY_STATUS)
    {
        SD_CsSetHigh();
        return FALSE;
    }

    // wait for IDLE
    SD_CheckBusy();

    SD_CsSetHigh();

    return TRUE;

}

BOOL SD_BS_Driver::EraseBlock(void *context, ByteAddress phyAddr)
{
    NATIVE_PROFILE_HAL_DRIVERS_FLASH();

    UINT32 RangeIndex;
    UINT32 RegionIndex;

    BLOCK_CONFIG* pConfig = (BLOCK_CONFIG*)context;

    if(!pConfig->BlockDeviceInformation->FindRegionFromAddress(phyAddr, RegionIndex, RangeIndex))
        return FALSE;

    const BlockRegionInfo* pRegion = &pConfig->BlockDeviceInformation->Regions[RegionIndex];

    ByteAddress StartSector = pConfig->BlockDeviceInformation->PhysicalToSectorAddress( pRegion, phyAddr );

    UINT32 SectorsPerBlock = pRegion->BytesPerBlock / pConfig->BlockDeviceInformation->BytesPerSector;

    SectorAddress SectorAddress = (StartSector / SectorsPerBlock) * SectorsPerBlock;

    CPU_SPI_Xaction_Start(g_SD_BL_Config.SPI);

    EraseSectors(SectorAddress, SectorsPerBlock);

    CPU_SPI_Xaction_Stop(g_SD_BL_Config.SPI);

    return TRUE;

}

void SD_BS_Driver::SetPowerState(void *context, UINT32 State)
{
    // our flash driver is always Power ON
    return ;
}

UINT32 SD_BS_Driver::MaxSectorWrite_uSec(void *context)
{
    NATIVE_PROFILE_PAL_FLASH();

    SD_BLOCK_CONFIG *config = (SD_BLOCK_CONFIG*)context;

    return config->BlockDeviceInformation->MaxSectorWrite_uSec;
}

UINT32 SD_BS_Driver::MaxBlockErase_uSec(void *context)
{
    NATIVE_PROFILE_PAL_FLASH();

    SD_BLOCK_CONFIG *config = (SD_BLOCK_CONFIG*)context;

    return config->BlockDeviceInformation->MaxBlockErase_uSec;
}
