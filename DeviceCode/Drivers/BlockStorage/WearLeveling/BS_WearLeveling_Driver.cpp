////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include <tinyhal.h>
#include "BS_WearLeveling.h"

//#define _VISUAL_WEARLEVELING_ 1

//--//

#ifdef _VISUAL_WEARLEVELING_
static UINT16 *g_pLcdBuffer    = NULL;
static BOOL    g_WearLevelInit = FALSE;
#define BLOCK_SIZE 6

#define COLOR_RED    0x001f
#define COLOR_GREEN  0x03c0
#define COLOR_BLUE   0xf800
#define COLOR_PURPLE 0x3807
#endif

#ifdef _VISUAL_WEARLEVELING_
static void SetBlockColor(ByteAddress BlockAddress, UINT16 color, UINT32 BytesPerBlock)
{
    if(g_pLcdBuffer)
    {
        int qqIdx = (BlockAddress / BytesPerBlock) * BLOCK_SIZE;
        int qy    = (qqIdx / LCD_GetWidth()) * BLOCK_SIZE;
        qqIdx     = qqIdx % LCD_GetWidth();
           
        for(int qq=0; qq<BLOCK_SIZE; qq++)
        {
            for(int qx=0; qx<BLOCK_SIZE; qx++)
            {
                g_pLcdBuffer[qqIdx + (qy + qq) * LCD_GetWidth() + qx] = color;
            }
        }
    }
}
#endif


BOOL BS_WearLeveling_Driver::InitializeDevice(void *context)
{
    BS_WearLeveling_Config *config = (BS_WearLeveling_Config*)context;

    if(config == NULL || config->Device == NULL) return FALSE;

    BOOL fResult = config->Device->InitializeDevice(config->BlockConfig);

    const BlockDeviceInfo *pDevInfo   = config->Device->GetDeviceInfo(config->BlockConfig);
    UINT32                 BlockCount = pDevInfo->Size / config->BytesPerBlock;
    UINT32                 mask       = 0x80000000;
    
    config->BlockIndexMask = mask;

#ifdef _VISUAL_WEARLEVELING_
    g_pLcdBuffer = (UINT16*)LCD_GetFrameBuffer();
#endif

    // verify that we have simple heap support
    void* tmp = SimpleHeap_Allocate(4); 

    if(NULL == tmp) { ASSERT(FALSE); return FALSE; }
    else            { SimpleHeap_Release(tmp);     }

    // 
    // Block index mas is used to determine block map addressing
    //
    while(config->BlockIndexMask >= BlockCount) 
    {
        config->BlockIndexMask >>= 1;
    }

    //
    // Assure that we start out compacted
    //
    //if(!CompactBlocks(config, 0xFFFFFFFF)) return FALSE;


    WL_SectorMetadata  meta;
    const UINT32    NumBlocks       = pDevInfo->Size / config->BytesPerBlock;
    UINT32          Blocks;
    const UINT32    AddressSpace    = pDevInfo->Regions[0].Start;
    ByteAddress     BlockAddress;

    //
    // Look for bad blocks so that we can maintain a list
    // In the first round, look for previously marked bad block replacements.
    // In the second round, look for any unmapped bad blocks
    //
    for(int round=1; round<=2; round++)
    {       
        Blocks = NumBlocks;
        BlockAddress = AddressSpace;
        
        while(Blocks--)
        {
            BOOL fMeta = config->Device->GetSectorMetadata(config->BlockConfig, BlockAddress, (SectorMetadata*)&meta);

            if(fMeta && !meta.IsBlockFormatted())
            {
                FormatBlock(config, BlockAddress);
            }
            else if((round == 2 && (!fMeta || meta.IsBadBlock())) ||
                    (round == 1 && meta.IsBadBlockReplacement() && meta.wOwnerBlock != 0))
            {
                UINT16 badIndex = (BlockAddress - AddressSpace) / config->BytesPerBlock;

                WL_BadBlockMap* pBadMap = config->BadBlockList;
                
                while(pBadMap != NULL)
                {
                    if(meta.IsBadBlock()            && pBadMap->VirtualBlockIndex  == badIndex) break;
                    if(meta.IsBadBlockReplacement() && pBadMap->PhysicalBlockIndex == badIndex) break;

                    pBadMap = pBadMap->Next;
                }

                if(pBadMap == NULL)
                {
                    //
                    // We found a bad block (in the second round) which is not in the bad block list
                    //
                    if(round == 2)
                    {
                        ByteAddress phyNewBlock;
                        
                        ReplaceBadBlock(config, BlockAddress, phyNewBlock);
                    }
                    //
                    // We found a bad block replacement that has not been added to the list
                    //
                    else
                    {
                        pBadMap = (WL_BadBlockMap*)SimpleHeap_Allocate(sizeof(WL_BadBlockMap));

                        if(!pBadMap) return FALSE;

                        pBadMap->VirtualBlockIndex  = meta.wOwnerBlock;
                        pBadMap->PhysicalBlockIndex = badIndex;

                        pBadMap->Next = config->BadBlockList;
                        config->BadBlockList = pBadMap;
                    }
                }


            }
            
            BlockAddress += config->BytesPerBlock;
        }
    }

    return fResult;
}

//
// Replace a bad block with a new free block
//
BOOL BS_WearLeveling_Driver::ReplaceBadBlock(BS_WearLeveling_Config* config, ByteAddress BadBlockAddress, ByteAddress &NewPhyBlockAddress)
{
    const UINT32 AddressSpace   = config->BlockConfig->BlockDeviceInformation->Regions[0].Start;
    ByteAddress  freeBlock      = AddressSpace + config->BlockConfig->BlockDeviceInformation->Size - config->BytesPerBlock;
    UINT16       BadBlockIndex  = (BadBlockAddress - AddressSpace) / config->BytesPerBlock;
    
    WL_SectorMetadata meta;

    // 
    // Either find the block or an empty space in the bad block list
    //
    WL_BadBlockMap* pBadMap = config->BadBlockList;
    
    while(pBadMap != NULL)
    {
        if(pBadMap->VirtualBlockIndex  == BadBlockIndex) break;

        pBadMap = pBadMap->Next;
    }

    //
    // Find a free block
    //
    while(!config->Device->GetSectorMetadata( config->BlockConfig, freeBlock, (SectorMetadata*)&meta ) || !meta.IsBlockFree())
    {
        freeBlock -= config->BytesPerBlock;

        if(freeBlock <= AddressSpace) return FALSE;
    }

    //
    // add the bad block map to the list
    //
    if(pBadMap == NULL)
    {
        pBadMap = (WL_BadBlockMap*)SimpleHeap_Allocate( sizeof(WL_BadBlockMap) );

        if(pBadMap == NULL) return FALSE;

        pBadMap->Next = config->BadBlockList;
        config->BadBlockList = pBadMap;
    }
    
    pBadMap->VirtualBlockIndex  = (BadBlockAddress - AddressSpace) / config->BytesPerBlock;
    pBadMap->PhysicalBlockIndex = (freeBlock       - AddressSpace) / config->BytesPerBlock;

    //
    // Set the meta data on the replacement block so that it can be identified as a replacement block
    //
    meta.SetBlockInUse();
    meta.SetBlockMapped();
    meta.SetBadBlockReplacement();
    meta.wOwnerBlock = pBadMap->VirtualBlockIndex;

#ifdef _VISUAL_WEARLEVELING_
SetBlockColor( BadBlockAddress, COLOR_PURPLE, config->BytesPerBlock );
SetBlockColor( freeBlock      , COLOR_BLUE  , config->BytesPerBlock );
#endif

    NewPhyBlockAddress = freeBlock;

    return config->Device->SetSectorMetadata( config->BlockConfig, freeBlock, (SectorMetadata*)&meta );
}

BOOL BS_WearLeveling_Driver::UninitializeDevice(void *context)
{    
    BS_WearLeveling_Config *config = (BS_WearLeveling_Config*)context;

    if(config == NULL || config->Device == NULL) return FALSE;

    config->BadBlockList = NULL;

#ifdef _VISUAL_WEARLEVELING_
    g_WearLevelInit = FALSE;
#endif

    return config->Device->UninitializeDevice(config->BlockConfig);
}

const BlockDeviceInfo *BS_WearLeveling_Driver::GetDeviceInfo(void *context)
{
    BS_WearLeveling_Config *config = (BS_WearLeveling_Config*)context;

    if(config == NULL || config->Device == NULL) return FALSE;

    return config->Device->GetDeviceInfo(config->BlockConfig);
}

BOOL BS_WearLeveling_Driver::CompactBlocks(BS_WearLeveling_Config* config, ByteAddress virtSectAddress)
{
    GLOBAL_LOCK(x);
    
    const BlockDeviceInfo *pDevInfo = config->BlockConfig->BlockDeviceInformation;

    WL_SectorMetadata  meta;

    const UINT32    AddressSpace    = pDevInfo->Regions[0].Start;
    const UINT32    BlockCount      = pDevInfo->Size / config->BytesPerBlock;
    UINT32          NumBlocks       = BlockCount;
    ByteAddress     BlockAddress;
    ByteAddress     tmpAddress      = AddressSpace + (NumBlocks-1) * config->BytesPerBlock;
    const UINT32    SectorsPerBlock = config->BytesPerBlock / pDevInfo->BytesPerSector;
    ByteAddress     virtBlockAddr   = config->BytesPerBlock * (virtSectAddress / config->BytesPerBlock);

    ByteAddress     CopyBlockAddress1 = 0xFFFFFFFF;
    ByteAddress     CopyBlockAddress2 = 0xFFFFFFFF;

    UINT32          FreeBlockCount    = 0;

//debug_printf("BlockCompaction\r\n");    
    //
    // Get a 2 free blocks to use during compaction as temporary blocks
    //
    while(NumBlocks--)
    {
        if(config->Device->GetSectorMetadata(config->BlockConfig, tmpAddress, (SectorMetadata*)&meta) && 
          (meta.IsBlockFree() || (meta.IsBlockTrash() && !meta.IsValidBlockMapOffset())))
        {
            if(CopyBlockAddress1 == 0xFFFFFFFF)
            {
                CopyBlockAddress1 = tmpAddress;
            }
            else
            {
                CopyBlockAddress2 = tmpAddress;
                break;
            }
        }
        tmpAddress -= config->BytesPerBlock;
    }

    // out of blocks - shouldn't get here
    if(CopyBlockAddress1 == 0xFFFFFFFF || CopyBlockAddress2 == 0xFFFFFFFF) 
    {
        ASSERT(FALSE);
        return FALSE;
    }

    NumBlocks       = BlockCount;
    BlockAddress    = AddressSpace;

    while(NumBlocks--)
    {
        UINT32      dirtySectorCount = 0;
        ByteAddress sectAddr         = BlockAddress;

        for(UINT32 SectorIndex=0; SectorIndex < SectorsPerBlock; SectorIndex++)
        {
            //
            // Get the WL_SectorMetadata from the device at the given address, if there is a failure then indicate a bad
            // sector in our map
            //
            if(!config->Device->GetSectorMetadata(config->BlockConfig, sectAddr, (SectorMetadata*)&meta))
            {
                dirtySectorCount++;
            }
            //
            // The first sector contains block information so we need to special case it
            //
            else if(SectorIndex == 0)
            {
                if(meta.IsBlockFree())
                {
                    FreeBlockCount++;
                    break;
                }
                //
                // The current block is mapped to another address
                //
                else if(meta.IsValidBlockMapOffset())
                {
                    UINT32 NextBlock = BlockAddress;
                    UINT32 MappedBlock = AddressSpace + meta.GetBlockMapOffset() * config->BytesPerBlock;

                    while(TRUE)
                    {
                        //
                        // if the current block's data is owned by another block (in other words, if another block is mapped to this one)
                        // then we need to save the data to one of the temporary blocks while we format and compact this block
                        //
                        if(!meta.IsBlockTrash() && meta.IsValidOwnerBlock())
                        {
                            FormatBlock( config, CopyBlockAddress1 );
                            
                            ReplaceBlock( config, CopyBlockAddress1, NextBlock, CopyBlockAddress1, TRUE );
                        }

                        //
                        //  If the current block is marked as bad, then replace the block it maps to so it can be formatted.
                        //  This is needed so that the replacement block address can be mapped
                        //
                        if(meta.IsBadBlock())
                        {
                            ByteAddress phyNewBlock;

                            // find a new block
                            if(ReplaceBadBlock( config, BlockAddress, phyNewBlock ))
                            {
                                // move and compact the sector data
                                ReplaceBlock(config, NextBlock, MappedBlock, phyNewBlock, TRUE );
                            }
                            else
                            {
                                ASSERT(FALSE); // bad block could not be replaced
                            }
                            break;
                        }
                        //
                        // Otherwise, format and copy the blocks mapped data from the mapped block (so that we end up with a direct map)
                        //
                        else
                        {
                            WL_SectorMetadata metaOld;
                            
                            FormatBlock( config, NextBlock );

                            config->Device->GetSectorMetadata(config->BlockConfig, MappedBlock, (SectorMetadata*)&metaOld);

                            ReplaceBlock( config, NextBlock, MappedBlock, NextBlock, !metaOld.IsBlockTrash() );
                        }

                        //
                        // Set up the next block in the the chain of mapped blocks
                        //
                        if(!meta.IsBlockTrash() && meta.IsValidOwnerBlock())
                        {
                            MappedBlock = CopyBlockAddress1;
                            NextBlock   = AddressSpace + meta.wOwnerBlock * config->BytesPerBlock;

                            CopyBlockAddress1 = CopyBlockAddress2;
                            CopyBlockAddress2 = MappedBlock;
                            
                            config->Device->GetSectorMetadata(config->BlockConfig, NextBlock, (SectorMetadata*)&meta);
                        }
                        //
                        // Otherwise, we are done (no more mapped blocks in the chain)
                        //
                        else
                        {
                            break;
                        }
                    }

                    FormatBlock( config, CopyBlockAddress1 );
                    FormatBlock( config, CopyBlockAddress2 );

                    break;
                }
                //
                // Swap out the replacement block for bad blocks, so that the old replacment can be re-mapped
                //
                else if(meta.IsBadBlock())
                {
                    ByteAddress phyNewBlock, curBlock;

                    GetPhysicalBlockAddress(config, BlockAddress, curBlock, TRUE); 

                    // replace the replacement block
                    if(ReplaceBadBlock( config, BlockAddress, phyNewBlock ))
                    {
                        // move and compact the data from the old replacement block
                        ReplaceBlock(config, BlockAddress, curBlock, phyNewBlock, TRUE );
                    }
                    else
                    {
                        ASSERT(FALSE); // bad block could not be replaced
                    }
                    break;
                }
                //
                // No mapping and the block is either not formatted or it is dirty, so format the block
                //
                else if(!meta.IsBlockFormatted())
                {
                    FormatBlock( config, BlockAddress );
                    break;
                }
                //
                // compact the sectors
                //
                else if(meta.IsSectorMapped())
                {
                    FormatBlock( config, CopyBlockAddress1 );
                    ReplaceBlock( config, sectAddr, sectAddr, CopyBlockAddress1, TRUE );
                    FormatBlock( config, sectAddr );
                    ReplaceBlock( config, sectAddr, CopyBlockAddress1, sectAddr, TRUE );
                    break;
                }
                //
                // increment the dirty sector count (if all sectors are dirty, then format)
                //
                else if(meta.IsSectorDirty() || meta.IsSectorBad())
                {
                    dirtySectorCount++;
                }
                //
                // If we have at least one sector that is live, then we will not format this block so bail out
                //
                else 
                {
                    break;
                }

            }
            else if(meta.IsSectorMapped())
            {
                //
                // compact the sectors
                //
                FormatBlock( config, CopyBlockAddress1 );
                ReplaceBlock( config, sectAddr, sectAddr, CopyBlockAddress1, TRUE );
                FormatBlock( config, sectAddr );
                ReplaceBlock( config, sectAddr, CopyBlockAddress1, sectAddr, TRUE );
                break;
            }
            else if(meta.IsSectorBad() || meta.IsSectorDirty())
            {
                dirtySectorCount++;
            }
            else // if we have a live, good sector then we can break out.
            {
                break;
            }

            sectAddr += pDevInfo->BytesPerSector;
        }

        //
        // If all the sectors of a configuration are dirty, then mark the block for erase
        //
        if(SectorsPerBlock == dirtySectorCount)
        {
            config->Device->GetSectorMetadata(config->BlockConfig, BlockAddress, (SectorMetadata*)&meta);
            
            if(!meta.IsBlockTrash())
            {
                meta.SetBlockTrash();

                config->Device->SetSectorMetadata(config->BlockConfig, BlockAddress, (SectorMetadata*)&meta);
#ifdef _VISUAL_WEARLEVELING_
SetBlockColor( BlockAddress, COLOR_RED, config->BytesPerBlock );
#endif
            }
        }
        
        BlockAddress += config->BytesPerBlock;
    }

    //
    // Any blocks still mapped on the second iteration means that they are orphaned, so format them
    //
    // erase orphaned blocks when we have less than 20% free
    if((FreeBlockCount < (pDevInfo->Regions[0].NumBlocks / 5)) ||
       (virtSectAddress == 0xFFFFFFFF))
    {
//debug_printf("BlockCompactionFree\r\n");    

        NumBlocks    = BlockCount;
        BlockAddress = pDevInfo->Regions[0].Start;

        while(NumBlocks--)
        {
            if(config->Device->GetSectorMetadata(config->BlockConfig, BlockAddress, (SectorMetadata*)&meta) && 
              !meta.IsBadBlock() && (meta.IsBlockTrash() || meta.IsBlockMapped()) && !meta.IsBadBlockReplacement())
            {
                FormatBlock( config, BlockAddress );
            }
            BlockAddress += config->BytesPerBlock;
        }
    }
    else
    {
        if(config->Device->GetSectorMetadata(config->BlockConfig, virtBlockAddr, (SectorMetadata*)&meta) && meta.IsBlockTrash())
        {
            FormatBlock( config, virtBlockAddr );
        }
    }

    return TRUE;    
}

BOOL BS_WearLeveling_Driver::FormatBlock(BS_WearLeveling_Config* config, ByteAddress phyBlockAddress)
{
    WL_SectorMetadata meta;

    config->Device->GetSectorMetadata(config->BlockConfig, phyBlockAddress, (SectorMetadata*)&meta);

    //
    // Erase the block and mark it as formatted
    //
    if(!meta.IsBadBlock())
    {
        config->Device->EraseBlock(config->BlockConfig, phyBlockAddress);

        memset(&meta, 0xff, sizeof(meta));
        
        meta.SetBlockFormated();

        config->Device->SetSectorMetadata(config->BlockConfig, phyBlockAddress, (SectorMetadata*)&meta);
    }
    //
    // If the block is bad there is no reason to erase it, just change the replacement block 
    //
    else
    {
        ByteAddress phyNewBlock, curBlock;

        if(GetPhysicalBlockAddress(config, phyBlockAddress, curBlock, TRUE))
        {
            //
            // Replace the mapped block in the bad block list
            //
            if(!ReplaceBadBlock( config, phyBlockAddress, phyNewBlock )) return FALSE;
        }
    }

#ifdef _VISUAL_WEARLEVELING_
SetBlockColor( phyBlockAddress, COLOR_GREEN, config->BytesPerBlock );
#endif
    
    return TRUE;
}

BOOL BS_WearLeveling_Driver::GetNextFreeSector(BS_WearLeveling_Config* config, ByteAddress phyAddress, ByteAddress &phyFreeAddress, WL_SectorMetadata &metaFree)
{
    const UINT32 BytesPerSector = config->BlockConfig->BlockDeviceInformation->BytesPerSector;
    UINT32 phyBlockAddress = config->BytesPerBlock  * (phyAddress / config->BytesPerBlock );

    UINT32 nextFree = phyBlockAddress + config->BytesPerBlock - BytesPerSector;

    //
    // Start from the end of the block to find free sectors (because users are likely to program sequentially)
    // This will hopefully cut down on the number of mapped sectors.
    // Ignore first block - to simplify mapping scheme we don't allow maps to the first sector it can only be direct mapped
    //
    while(nextFree > phyBlockAddress)
    {
        config->Device->GetSectorMetadata( config->BlockConfig, nextFree, (SectorMetadata*)&metaFree );

        if(metaFree.IsSectorFree())
        {
            phyFreeAddress = nextFree;
            break;
        }

        nextFree -= BytesPerSector;
    }

    return (nextFree > phyBlockAddress);
}

// This method determines how many bits are set to 1 in a UINT16
__inline UINT16 BitsSet16(UINT16 x) 
{
    x = ((x & 0xaaaa) >> 1) + (x & 0x5555);
    x = ((x & 0xcccc) >> 2) + (x & 0x3333);
    x = ((x & 0xF0F0) >> 4) + (x & 0x0F0F);
    x = ((x & 0xFF00) >> 8) + (x & 0x00FF);

    return x;
}

//
// Block mapping has can normally only be done once per BlockOffset field, because you have to erase the enitre block to write a new offset in the metadata
// To get around this issue, we look for the next available block that flips the fewest bits from 1->0.  This way we can reuse the field more than once.  
// (e.g. 0xFFFF -> 0xFFEFF -> 0xF7EFF -> etc).  Note that the indexes are bitwise inverted in order to maintain more 1s than 0s.
//
BOOL BS_WearLeveling_Driver::GetNextFreeBlock(BS_WearLeveling_Config* config, ByteAddress virtAddress, ByteAddress &phyNewBlockAddress)
{
    ByteAddress       virtBlockAddress   = config->BytesPerBlock * (virtAddress / config->BytesPerBlock);
    UINT16            curBlockIndex;
    WL_SectorMetadata meta, metaVirt;

    ByteAddress       phySectAddr;
    UINT16            maxBits;
    UINT16            maxValue      = config->BlockIndexMask << 1;
    BOOL              fRound2       = FALSE;
    const UINT32      AddressSpace  = config->BlockConfig->BlockDeviceInformation->Regions[0].Start;

    if(!config->Device->GetSectorMetadata( config->BlockConfig, virtBlockAddress, (SectorMetadata*)&metaVirt )) return FALSE;

    curBlockIndex = metaVirt.GetBlockMapOffset();

    maxBits = BitsSet16(curBlockIndex) + 1;

    // two rounds since we have two block offset fields - we may want to change this to use more than the first sector
    // to limit the block erases on the virtual block
    while(TRUE)
    {
        // Start by looking for addresses with only 1 bit difference, then increase to 2, and so on ...
        while(TRUE)
        {
            for(int i=maxValue-1; i>0; i--)
            {
                if(BitsSet16((i | curBlockIndex)) == maxBits)
                {
                    phySectAddr = AddressSpace + (i | curBlockIndex) * config->BytesPerBlock;
                
                    if(config->Device->GetSectorMetadata( config->BlockConfig, phySectAddr, (SectorMetadata*)&meta ) && meta.IsBlockFree())
                    {
                        phyNewBlockAddress = phySectAddr;

                        return TRUE;
                    }   
                }
            }
                
            maxBits++;

            if((1ul << maxBits) > maxValue)
            {
                break;
            }
        }

        if(fRound2) return FALSE;

        curBlockIndex = ~(metaVirt.wBlockOffset[1]);

        maxBits = BitsSet16(curBlockIndex) + 1;
        fRound2 = TRUE;
    }

    return TRUE;
}


BOOL BS_WearLeveling_Driver::GetPhysicalBlockAddress(BS_WearLeveling_Config* config, ByteAddress virtAddress, ByteAddress &phyBlockAddress, BOOL fAllocateNew)
{
    const UINT32      AddressSpace       = config->BlockConfig->BlockDeviceInformation->Regions[0].Start;
    UINT16            virtualBlockIndex  = (UINT16)((virtAddress - AddressSpace) / config->BytesPerBlock);
    ByteAddress       virtBlockAddress   = config->BytesPerBlock * virtualBlockIndex;
    WL_SectorMetadata meta;

    //
    // The block map offset is the index to the referenced block
    //
    if(config->Device->GetSectorMetadata( config->BlockConfig, virtBlockAddress, (SectorMetadata*)&meta ) && !meta.IsBadBlock())
    {
        BOOL fGetNewBlock = FALSE;

        //
        // This block maps to another location
        //
        if(meta.IsValidBlockMapOffset())
        {
            phyBlockAddress = AddressSpace + meta.GetBlockMapOffset() * config->BytesPerBlock;
            
            config->Device->GetSectorMetadata( config->BlockConfig, phyBlockAddress, (SectorMetadata*)&meta );
        }
        // 
        // This block is owned by another block and we are not mapped, so make a new mapping
        //
        else if(meta.IsBlockMapped())
        {
            fGetNewBlock = TRUE;
        }
        //
        // Direct map
        //
        else
        {
            phyBlockAddress = virtBlockAddress;
        }

        //
        // In the case that the block is dirty or the direct map is not available and there is no map
        //
        if(meta.IsBlockTrash() || fGetNewBlock)
        {
            if(!fAllocateNew) return FALSE;
            
            if(!GetNextFreeBlock(config, virtBlockAddress, phyBlockAddress))
            {
                if(!CompactBlocks(config, virtBlockAddress)) return FALSE;

                // compacting the blocks will force a direct mapping
                phyBlockAddress = virtBlockAddress;
            }
            else
            {
                if(!ReplaceBlock(config, virtBlockAddress, phyBlockAddress, phyBlockAddress, FALSE)) return FALSE;
            }
        }
    }
    //
    // Look in the bad block list for this address
    //
    else
    {
        WL_BadBlockMap *pBadList = config->BadBlockList;
        
        while(pBadList != NULL)
        {
            if(pBadList->VirtualBlockIndex == virtualBlockIndex)
            {
                phyBlockAddress = AddressSpace + pBadList->PhysicalBlockIndex * config->BytesPerBlock;
                return TRUE;
            }
            pBadList = pBadList->Next;
        }
        return FALSE;
    }

    ASSERT(phyBlockAddress < ((config->BlockIndexMask << 1) * config->BytesPerBlock));    

    return TRUE;
}

BOOL BS_WearLeveling_Driver::ReplaceBlock(BS_WearLeveling_Config* config, ByteAddress virtAddress, ByteAddress currentBlockAddr, ByteAddress phyNewBlockAddress, BOOL fCopyData)
{
    WL_SectorMetadata meta;
    ByteAddress    phyNewSectAddr;
    const UINT32   BytesPerSector   = config->BlockConfig->BlockDeviceInformation->BytesPerSector;
    ByteAddress    virtBlockAddress = config->BytesPerBlock * (virtAddress / config->BytesPerBlock);
    UINT32         SectorCount      = config->BytesPerBlock / BytesPerSector;
    UINT32         curSectAddr      = config->BytesPerBlock * (currentBlockAddr / config->BytesPerBlock);
    const UINT32   AddressSpace     = config->BlockConfig->BlockDeviceInformation->Regions[0].Start;

    currentBlockAddr = curSectAddr;

    phyNewSectAddr   = phyNewBlockAddress;

    if(fCopyData)
    {
        UINT8 *pData = (UINT8*)SimpleHeap_Allocate(BytesPerSector);

        if(pData == NULL) return FALSE;

        //
        // Copy the data sector by sector
        //
        for(int i=0; i<SectorCount; i++)
        {
            if(config->Device->GetSectorMetadata( config->BlockConfig, curSectAddr, (SectorMetadata*)&meta ))
            {
                // if the block is dirty do not copy it!
                if(i == 0 && meta.IsBlockTrash())
                {
                    break;
                }
                if(meta.IsValidSectorMap()) 
                {
                    UINT32 linkedSectAddr = currentBlockAddr + meta.wMappedSectorOffset * BytesPerSector;

                    //
                    // Since the sector is mapped, we need to unwind the linkage and place the physical sector back into its
                    // direct mapping location.
                    //
                    while(TRUE)
                    {
                        if(!config->Device->GetSectorMetadata( config->BlockConfig, linkedSectAddr, (SectorMetadata*)&meta )) break;

                        //
                        // Follow the link to the next sector
                        //
                        if(meta.IsValidSectorLink()) 
                        {
                            linkedSectAddr = currentBlockAddr + meta.wLinkedSectorOffset * BytesPerSector;
                        }
                        
                        //
                        // we are at the end of the linked mapping move the data to the direct map location of the new block
                        //
                        else
                        {
                            // if sector is bad or dirty then ignore it
                            if(meta.IsSectorBad() || meta.IsSectorDirty() || !meta.IsSectorInUse()) break;
                            
                            if(config->Device->Read(config->BlockConfig, linkedSectAddr, BytesPerSector, pData)
                               && (meta.CRC == 0 || meta.CRC == SUPPORT_ComputeCRC(pData, BytesPerSector, 0)))
                            {
                                if(!WriteToSector( config, phyNewSectAddr, pData, 0, BytesPerSector, FALSE ))
                                {
                                    SimpleHeap_Release(pData);
                                    return FALSE;
                                }
                            }
                            break;
                        }
                    }
                }
                //
                // Direct map - only copy if the sector is still valid
                //
                else if(!meta.IsSectorBad() && !meta.IsSectorMapped() && !meta.IsSectorDirty() && meta.IsSectorInUse())
                {
                    if(config->Device->Read(config->BlockConfig, curSectAddr, BytesPerSector, pData)
                      && (meta.CRC == 0 || meta.CRC == SUPPORT_ComputeCRC(pData, BytesPerSector, 0)))
                    {
                        if(!WriteToSector( config, phyNewSectAddr, pData, 0, BytesPerSector, FALSE ))
                        {
                            SimpleHeap_Release(pData);
                            return FALSE;
                        }
                    }
                }
            }

            curSectAddr    += BytesPerSector;
            phyNewSectAddr += BytesPerSector;
        }   

        SimpleHeap_Release(pData);
    }

    //
    // only format the old block if it was a mapped block
    //
    if(phyNewBlockAddress != currentBlockAddr)
    {
        if(config->Device->GetSectorMetadata( config->BlockConfig, currentBlockAddr, (SectorMetadata*)&meta ) && !meta.IsBlockTrash())
        {
            meta.SetBlockTrash();

            config->Device->SetSectorMetadata( config->BlockConfig, currentBlockAddr, (SectorMetadata*)&meta );

#ifdef _VISUAL_WEARLEVELING_
SetBlockColor( currentBlockAddr, COLOR_RED, config->BytesPerBlock );
#endif

        }
    }

    //
    // Now set up the sector metadata
    //
    if(virtBlockAddress != phyNewBlockAddress)
    {
        WL_SectorMetadata origMeta;

        UINT32 phyNewBlockIndex = (phyNewBlockAddress - AddressSpace) / config->BytesPerBlock;

        //
        // Link the direct mapped block with the new block
        //
        config->Device->GetSectorMetadata( config->BlockConfig, virtBlockAddress, (SectorMetadata*)&origMeta );

        if(!origMeta.IsBadBlock())
        {
            origMeta.SetBlockMapOffset(phyNewBlockIndex);

            config->Device->SetSectorMetadata( config->BlockConfig, virtBlockAddress, (SectorMetadata*)&origMeta );
        }
    }

    // 
    // Mark the new block as in use and mapped
    //
    config->Device->GetSectorMetadata( config->BlockConfig, phyNewBlockAddress, (SectorMetadata*)&meta );

    meta.SetBlockInUse();

    if(virtBlockAddress != phyNewBlockAddress)
    {
        meta.SetBlockMapped();
        meta.wOwnerBlock = (virtBlockAddress - AddressSpace) / config->BytesPerBlock;
    }            

    config->Device->SetSectorMetadata( config->BlockConfig, phyNewBlockAddress, (SectorMetadata*)&meta );
    
    return TRUE;
}

BOOL BS_WearLeveling_Driver::GetPhysicalAddress(BS_WearLeveling_Config* config, ByteAddress virtAddress, ByteAddress &phyAddress, BOOL &fDirectSectorMap)
{
    const UINT32 BytesPerSector    = config->BlockConfig->BlockDeviceInformation->BytesPerSector;
    ByteAddress  virtBlockAddress  = config->BytesPerBlock * (virtAddress / config->BytesPerBlock);
    ByteAddress  virtSectAddr      = BytesPerSector * (virtAddress / BytesPerSector);
    UINT32       virtSectOffset    = virtSectAddr - virtBlockAddress;
    UINT32       virtualOffset     = virtAddress - virtSectAddr;

    ByteAddress phySectAddr;
    ByteAddress phyBlockAddr;
    
    WL_SectorMetadata meta;
    bool fMappedAddress = false;

    //
    // First get the mapped block address
    //
    if(!GetPhysicalBlockAddress(config, virtAddress, phyBlockAddr, TRUE)) return FALSE;

    //
    // Calculate the sector address from the mapped block
    //
    phySectAddr = phyBlockAddr + virtSectOffset;

    fMappedAddress = false;

    //
    // Determin the mapped sector address within the mapped block
    //
    while(TRUE)
    {
        if(!config->Device->GetSectorMetadata( config->BlockConfig, phySectAddr, (SectorMetadata*)&meta )) return FALSE;

        //
        // If this sector is currently in use via another mapping, but it is our direct map, then
        // we need to use the wSectorOffset to determine where to map the address
        //
        if((meta.IsSectorBad() || meta.IsSectorMapped()) && !fMappedAddress)
        {
            //
            // We must not have a mapping for this virtual sector address yet, so go find a free sector
            //
            if(!meta.IsValidSectorMap())
            {
                WL_SectorMetadata metaNew;
                ByteAddress newPhyAddr;
                
                //
                // Get the next free sector in this block
                //
                if(GetNextFreeSector(config, phyBlockAddr, newPhyAddr, metaNew))
                {
                    metaNew.SetSectorInUse();
                    metaNew.SetSectorMapped();

                    config->Device->SetSectorMetadata( config->BlockConfig, newPhyAddr, (SectorMetadata*)&metaNew );

                    meta.wMappedSectorOffset = (newPhyAddr - phyBlockAddr) / BytesPerSector;

                    config->Device->SetSectorMetadata( config->BlockConfig, phySectAddr, (SectorMetadata*)&meta );

                    phySectAddr = newPhyAddr;
                }
                //
                // We ran out of free sectors, so replace this block (compacting the sectors)
                //
                else
                {
                    ByteAddress phyNewBlockAddress;
                    
                    if(!GetNextFreeBlock( config, virtBlockAddress, phyNewBlockAddress )) 
                    {
                        if(!CompactBlocks(config, virtBlockAddress)) return FALSE;

                        phyNewBlockAddress = virtBlockAddress;
                    }
                    else
                    {
                        if(!ReplaceBlock( config, virtBlockAddress, phyBlockAddr, phyNewBlockAddress, TRUE )) return FALSE;
                    }

                    phySectAddr = phyNewBlockAddress + virtSectOffset;
                }

                break;
            }
            //
            // Otherwise we have a current map for this virtual address so continue with the new address
            //
            else
            {
                phySectAddr = phyBlockAddr + meta.wMappedSectorOffset * BytesPerSector;
                
                fMappedAddress = true;
            }
            
        }
        else if(!fMappedAddress)
        {
            //
            // No more linking so we must be at the correct sector
            //
            if( meta.wMappedSectorOffset == WL_SectorMetadata::c_FREE_LINK_INDEX)
            {
                break;
            }
            else
            {
                phySectAddr =  phyBlockAddr + meta.wMappedSectorOffset * BytesPerSector;
                
                fMappedAddress = true;
            }
        }
        else
        {
            //
            // No more linking so we must be at the correct sector
            //
            if( meta.wLinkedSectorOffset == WL_SectorMetadata::c_FREE_LINK_INDEX)
            {
                break;
            }
            else 
            {
                phySectAddr =  phyBlockAddr + meta.wLinkedSectorOffset * BytesPerSector;
            }
        }
    }

    fDirectSectorMap = !fMappedAddress;
    phyAddress       = phySectAddr + virtualOffset;

    return TRUE;
}

BOOL BS_WearLeveling_Driver::Read(void *context, ByteAddress virtAddress, UINT32 NumBytes, BYTE *pSectorBuff)
{
    GLOBAL_LOCK(x);

    BS_WearLeveling_Config *config = (BS_WearLeveling_Config*)context;

    if(config == NULL || config->Device == NULL) return FALSE;

    const BlockDeviceInfo *pDevInfo = config->Device->GetDeviceInfo(config->BlockConfig);

    SectorAddress phyAddr;
    UINT32        offset        = virtAddress % pDevInfo->BytesPerSector;
    UINT32        bytes         = (NumBytes + offset > pDevInfo->BytesPerSector ? pDevInfo->BytesPerSector - offset : NumBytes);
    BYTE*         pRead         = pSectorBuff;
    BOOL          fDirectMap;

    if(config == NULL || config->Device == NULL) return FALSE;

    //
    // We must read sector by sector to ensure sector validity
    //
    while(NumBytes > 0)
    {
        if(GetPhysicalAddress(config, virtAddress, phyAddr, fDirectMap))
        {
            if(!config->Device->Read(config->BlockConfig, phyAddr, bytes, pRead)) return FALSE;
        }
        else
        {
            return FALSE;
        }

        NumBytes    -= bytes;
        virtAddress += bytes;
        pRead       += bytes;

        bytes = __min(NumBytes, pDevInfo->BytesPerSector);
    }

    return TRUE;
}

BOOL BS_WearLeveling_Driver::Write(void *context, ByteAddress phyAddr, UINT32 NumBytes, BYTE *pSectorBuff, BOOL ReadModifyWrite )
{
    return WriteInternal(context, phyAddr, NumBytes, pSectorBuff, ReadModifyWrite, FALSE);
}

BOOL BS_WearLeveling_Driver::WriteToSector(BS_WearLeveling_Config* config, ByteAddress virtSectStart, UINT8* pSectorData, UINT32 offset, UINT32 length, BOOL fMemFill)
{
    const UINT32    BytesPerSector      = config->BlockConfig->BlockDeviceInformation->BytesPerSector;
    BOOL            fReadModifyNeeded   = FALSE;
    ByteAddress     phyAddr;
    ByteAddress     mappedSectStart;
    UINT32          crc;
    WL_SectorMetadata  phyMeta, virtMeta;
    ByteAddress     sectStart           = virtSectStart;
    ByteAddress     blockAddr;
    BOOL            fDirectMap;
    BOOL            fResult             = FALSE;

    UINT8*          pSectorBuffer = (UINT8*)SimpleHeap_Allocate(BytesPerSector); if(!pSectorBuffer) {                              return FALSE; }
    UINT8*          pCrcBuffer    = (UINT8*)SimpleHeap_Allocate(BytesPerSector); if(!pCrcBuffer   ) { SimpleHeap_Release(pSectorBuffer); return FALSE; }

    //
    // FIND THE NEXT AVAILABLE FREE SECTOR
    //
    if(!GetPhysicalAddress(config, sectStart, phyAddr, fDirectMap)) goto CLEANUP;

#ifdef _VISUAL_WEARLEVELING_
if(g_pLcdBuffer)
{
    const UINT32    AddressSpace        = config->BlockConfig->BlockDeviceInformation->Regions[0].Start;

    if(!g_WearLevelInit) 
    { 
        LCD_Clear(); 
        g_WearLevelInit = TRUE; 

        for(int i=0; i<(config->BlockConfig->BlockDeviceInformation->Regions[0].NumBlocks); i++)
        {
            UINT16 color = COLOR_BLUE;
            
            config->Device->GetSectorMetadata(config->BlockConfig, AddressSpace + i*config->BytesPerBlock, (SectorMetadata*)&virtMeta);

                 if(virtMeta.IsBlockFree())  color = COLOR_GREEN;
            else if(virtMeta.IsBlockTrash()) color = COLOR_RED;
            
            SetBlockColor( i*config->BytesPerBlock, color, config->BytesPerBlock );
        }
    }
    
    SetBlockColor( phyAddr, COLOR_BLUE, config->BytesPerBlock );
}
#endif

    if(!config->Device->GetSectorMetadata(config->BlockConfig, phyAddr, (SectorMetadata*)&virtMeta)) goto CLEANUP;
    
    mappedSectStart = phyAddr;

    //
    // Read in the entire sector that we wish to write (for read/modify/write), then we will store it in another
    // sector.
    //
    config->Device->Read(config->BlockConfig, phyAddr, BytesPerSector, pSectorBuffer);

    //
    // we only need a new sector if we are moving any bits from 0 -> 1 
    //
    for(int i=0; i<length; i++)
    {
        if((fMemFill ? *pSectorData : pSectorData[i]) & (~pSectorBuffer[offset + i]))
        {
            fReadModifyNeeded = TRUE;
            break;
        }
    }

    //
    // Fill or write memory into the sector buffer prior to saving to a new sector address
    //
    if(fMemFill)
    {
        memset( &pSectorBuffer[offset], *pSectorData, length );
    }
    else
    {
        memcpy( &pSectorBuffer[offset], pSectorData, length );
    }

    //
    // Compute CRC on the buffer so that we can validate the write
    //
    crc = SUPPORT_ComputeCRC(pSectorBuffer, BytesPerSector, 0);

    while(TRUE)
    {
        //
        // The current sector has data that can not be over written by the new data (bits from 0->1)
        //
        if(fReadModifyNeeded)
        {
            //
            // Try to find a new sector in this block
            //
            if(!GetNextFreeSector( config, phyAddr, mappedSectStart, phyMeta ))
            {
                ByteAddress phyBlockAddr;

                config->Device->GetSectorMetadata(config->BlockConfig, phyAddr, (SectorMetadata*)&virtMeta);
                    
                virtMeta.SetSectorDirty();

                config->Device->SetSectorMetadata(config->BlockConfig, phyAddr, (SectorMetadata*)&virtMeta);

                if(!GetNextFreeBlock(config, sectStart, phyBlockAddr))
                {
                    if(!CompactBlocks(config, sectStart)) goto CLEANUP;
                }
                else
                {
                    if(!ReplaceBlock( config, sectStart, phyAddr, phyBlockAddr, TRUE )) goto CLEANUP;
                }
                    
                if(!GetPhysicalAddress( config, sectStart, phyAddr, fDirectMap )) goto CLEANUP;

                // if we compacted then we do not need to map the address
                fReadModifyNeeded = FALSE;

                mappedSectStart = phyAddr;
            }
        }
        else
        {
            mappedSectStart = phyAddr;
        }

        //
        // Write the data to the new sector
        //
        if(!config->Device->Write(config->BlockConfig, mappedSectStart, BytesPerSector, pSectorBuffer, FALSE)) goto BAD_SECTOR;

        //
        // Read the entire sector back into the buffer to check the CRC 
        //
        
        if(!config->Device->Read(config->BlockConfig, mappedSectStart, BytesPerSector, pCrcBuffer) || 
          (crc != SUPPORT_ComputeCRC(pCrcBuffer, BytesPerSector, 0)))
        {
            goto BAD_SECTOR;
        }
        
        goto GOOD_SECTOR;

        //
        // BAD SECTOR - The CRC check failed on the write, which indicates we are at a bad sector, so mark the sector
        // and continue with the next free sector.
        //
BAD_SECTOR:
        /*blockAddr = config->BytesPerBlock * (mappedSectStart / config->BytesPerBlock);

        if(config->Device->GetSectorMetadata(config->BlockConfig, blockAddr, (SectorMetadata*)&phyMeta))
        {
            // make the block bad
            memset(&phyMeta, 0x00, sizeof(phyMeta));
                
            if(!config->Device->SetSectorMetadata(config->BlockConfig, blockAddr, (SectorMetadata*)&phyMeta)) goto CLEANUP;

            ByteAddress phyNewBlock;
            
            // find a new block
            ReplaceBadBlock( config, blockAddr, phyNewBlock );

            ReplaceBlock( config, config->BytesPerBlock * (virtSectStart / config->BytesPerBlock), blockAddr, phyNewBlock, TRUE );

            phyAddr = phyNewBlock + (mappedSectStart - blockAddr);
        }
        */

        if(config->Device->GetSectorMetadata(config->BlockConfig, mappedSectStart, (SectorMetadata*)&phyMeta))
        {
            // make the block bad
            phyMeta.SetSectorBad();
                
            if(!config->Device->SetSectorMetadata(config->BlockConfig, mappedSectStart, (SectorMetadata*)&phyMeta)) goto CLEANUP;
        }

        fReadModifyNeeded = TRUE;

        // try again with a new sector
        continue;


        //
        // GOOD SECTOR - The CRC check passed so we need to update the sector metadata and the sector config map to indicate
        // this sector is in use.
        //
GOOD_SECTOR:

        blockAddr = config->BytesPerBlock * (mappedSectStart / config->BytesPerBlock);

        config->Device->GetSectorMetadata(config->BlockConfig, phyAddr, (SectorMetadata*)&virtMeta);

        if(fReadModifyNeeded)
        {
            config->Device->GetSectorMetadata(config->BlockConfig, mappedSectStart, (SectorMetadata*)&phyMeta);
            
            phyMeta.SetSectorInUse();
            phyMeta.SetSectorMapped();

            // reuse the CRC value if we can, but we can not flip bits from 0->1
            if((~phyMeta.CRC) & crc)
            {
                phyMeta.CRC = 0;
            }
            else
            {
                phyMeta.CRC = crc;
            }

            if(!config->Device->SetSectorMetadata(config->BlockConfig, mappedSectStart, (SectorMetadata*)&phyMeta)) goto CLEANUP;

            //
            // mark the old sector as dirty
            //
            virtMeta.SetSectorDirty();

            //
            // Direct mapping uses the sector offset, and linked mapping uses the linked sector offset field
            //
            if(fDirectMap) virtMeta.wMappedSectorOffset = (mappedSectStart - blockAddr) / BytesPerSector;
            else           virtMeta.wLinkedSectorOffset = (mappedSectStart - blockAddr) / BytesPerSector;
            
            if(!config->Device->SetSectorMetadata(config->BlockConfig, phyAddr, (SectorMetadata*)&virtMeta)) goto CLEANUP;
        }
        //
        // In the case where we have a direct map we need to indicate that the sector is now in use
        //
        else 
        {
            virtMeta.SetSectorInUse();

            // reuse the CRC value if we can, but we can not flip bits from 0->1
            if((~virtMeta.CRC) & crc) 
            {
                virtMeta.CRC = 0;
            }
            else
            {
                virtMeta.CRC = crc;
            }
            
            if(!config->Device->SetSectorMetadata(config->BlockConfig, phyAddr, (SectorMetadata*)&virtMeta)) goto CLEANUP;
        }

        //
        // Set the block in use bit
        //
        if(config->Device->GetSectorMetadata(config->BlockConfig, blockAddr, (SectorMetadata*)&phyMeta))
        {
            if(!phyMeta.IsBlockInUse() || !phyMeta.IsBlockDirty())
            {
                phyMeta.SetBlockInUse();
                phyMeta.SetBlockDirty();

#ifdef _VISUAL_WEARLEVELING_
SetBlockColor( blockAddr, COLOR_BLUE, config->BytesPerBlock );
#endif
                config->Device->SetSectorMetadata(config->BlockConfig, blockAddr, (SectorMetadata*)&phyMeta);
            }
        }

        // we are done so break out of the loop
        break;
        
    }

    fResult = TRUE;

CLEANUP:
    if(pSectorBuffer) { SimpleHeap_Release(pSectorBuffer); }
    if(pCrcBuffer   ) { SimpleHeap_Release(pCrcBuffer   ); }

    return fResult;
}

BOOL BS_WearLeveling_Driver::WriteInternal(void *context, ByteAddress Address, UINT32 NumBytes, BYTE *pSectorBuff, BOOL ReadModifyWrite, BOOL fFillMem)
{
    GLOBAL_LOCK(x);

    BS_WearLeveling_Config *config = (BS_WearLeveling_Config*)context;

    if(config == NULL || config->Device == NULL) return FALSE;

    const BlockDeviceInfo *pDevInfo = config->Device->GetDeviceInfo(config->BlockConfig);

    UINT32 BytesPerSector   = pDevInfo->BytesPerSector;
    UINT32 sectStart        = BytesPerSector * (Address / BytesPerSector);
    UINT32 sectOffset       = Address - sectStart;
    UINT32 bytes            = (NumBytes + sectOffset > BytesPerSector ? BytesPerSector - sectOffset : NumBytes);
    BYTE*  pWrite           = pSectorBuff;

    //
    // Write sector by sector to ensure data integrity
    //
    while(NumBytes > 0)
    {
        if(!WriteToSector( config, sectStart, pWrite, sectOffset, bytes, fFillMem )) return FALSE;

        sectStart += BytesPerSector;

        sectOffset = 0;
        NumBytes  -= bytes;
        //
        // Only increment if we are not filling memory
        //
        if(!fFillMem) pWrite += bytes;

        bytes = __min(NumBytes, BytesPerSector);
    }

    return TRUE;    
}

BOOL BS_WearLeveling_Driver::Memset(void *context, ByteAddress phyAddr, UINT8 Data, UINT32 NumBytes )
{
    return WriteInternal(context, phyAddr, NumBytes, &Data, TRUE, TRUE);
}

BOOL BS_WearLeveling_Driver::GetSectorMetadata(void* context, ByteAddress SectorStart, SectorMetadata* pSectorMetadata)
{
    //
    // The wear leveling layer controls the sector meta data for the underlying block storage device.  Do not allow upper layers
    // to manipulate it.
    //
    return FALSE;
}

BOOL BS_WearLeveling_Driver::SetSectorMetadata(void* context, ByteAddress SectorStart, SectorMetadata* pSectorMetadata)
{
    //
    // The wear leveling layer controls the sector meta data for the underlying block storage device.  Do not allow upper layers
    // to manipulate it.
    //
    return FALSE;
}

BOOL BS_WearLeveling_Driver::IsBlockErased(void *context, ByteAddress Address, UINT32 BlockLength)
{
    BS_WearLeveling_Config *config = (BS_WearLeveling_Config*)context;

    if(config == NULL || config->Device == NULL) return FALSE;

    ByteAddress phyAddr;
    WL_SectorMetadata meta;

    if(!GetPhysicalBlockAddress(config, Address, phyAddr, FALSE)) return TRUE;

    config->Device->GetSectorMetadata(config->BlockConfig, phyAddr, (SectorMetadata*)&meta);

    return !meta.IsBlockDirty() || meta.IsBlockTrash();
}

BOOL BS_WearLeveling_Driver::EraseBlock(void *context, ByteAddress Address)
{
    GLOBAL_LOCK(x);
    
    BS_WearLeveling_Config *config = (BS_WearLeveling_Config*)context;

    if(config == NULL || config->Device == NULL) return FALSE;

    UINT32          virtAddr   = config->BytesPerBlock * (Address / config->BytesPerBlock);
    ByteAddress     phyAddr;
    WL_SectorMetadata meta;

    //
    // GetPhysicalBlockAddress will return false if the virtual address is not assigned yet
    //
    if(!GetPhysicalBlockAddress(config, virtAddr, phyAddr, FALSE))
    {   
        return TRUE;
    }

    if(config->Device->GetSectorMetadata(config->BlockConfig, phyAddr, (SectorMetadata*)&meta))
    {
        //
        // if the block is free, then the block is already erased
        //
        if(!meta.IsBlockDirty()) 
        {
            return TRUE;
        }

        if(!meta.IsBlockTrash())
        {
            meta.SetBlockTrash();

            config->Device->SetSectorMetadata(config->BlockConfig, phyAddr, (SectorMetadata*)&meta);

#ifdef _VISUAL_WEARLEVELING_
SetBlockColor( phyAddr, COLOR_RED, config->BytesPerBlock );
#endif                
        }
    }
    else
    {
        // again, something is not right, so go ahead an format this block in an attempt to recover
        return FormatBlock(config, phyAddr);
    }

    return TRUE;
}


void BS_WearLeveling_Driver::SetPowerState(void *context, UINT32 State)
{
    BS_WearLeveling_Config *config = (BS_WearLeveling_Config*)context;

    if(config == NULL || config->Device == NULL) return;

    return config->Device->SetPowerState(config->BlockConfig, State);
}

UINT32 BS_WearLeveling_Driver::MaxSectorWrite_uSec(void *context)
{
    BS_WearLeveling_Config *config = (BS_WearLeveling_Config*)context;

    if(config == NULL || config->Device == NULL) return FALSE;

    return config->Device->MaxSectorWrite_uSec(config->BlockConfig);
}

UINT32 BS_WearLeveling_Driver::MaxBlockErase_uSec(void *context)
{
    BS_WearLeveling_Config *config = (BS_WearLeveling_Config*)context;

    if(config == NULL || config->Device == NULL) return FALSE;

    return config->Device->MaxBlockErase_uSec(config->BlockConfig);
}

#if defined(ADS_LINKER_BUG__NOT_ALL_UNUSED_VARIABLES_ARE_REMOVED)
#pragma arm section rodata = "g_BS_WearLeveling_DeviceTable"
#endif

struct IBlockStorageDevice g_BS_WearLeveling_DeviceTable = 
{
    &BS_WearLeveling_Driver::InitializeDevice, 
    &BS_WearLeveling_Driver::UninitializeDevice, 
    &BS_WearLeveling_Driver::GetDeviceInfo, 
    &BS_WearLeveling_Driver::Read, 
    &BS_WearLeveling_Driver::Write,
    &BS_WearLeveling_Driver::Memset,
    &BS_WearLeveling_Driver::GetSectorMetadata,
    &BS_WearLeveling_Driver::SetSectorMetadata,
    &BS_WearLeveling_Driver::IsBlockErased, 
    &BS_WearLeveling_Driver::EraseBlock, 
    &BS_WearLeveling_Driver::SetPowerState, 
    &BS_WearLeveling_Driver::MaxSectorWrite_uSec, 
    &BS_WearLeveling_Driver::MaxBlockErase_uSec, 
};

#if defined(ADS_LINKER_BUG__NOT_ALL_UNUSED_VARIABLES_ARE_REMOVED)
#pragma arm section rodata 
#endif 

