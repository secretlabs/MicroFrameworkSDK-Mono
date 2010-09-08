////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "tinyhal.h"
#include "FAT_FS.h"
#include "FAT_FS_Utility.h"
#include "TinyCLR_Interop.h"

//--//

/////////////////////////////////////////////////////////
//    IO buffer scenario
//          buffer size: one block size in hardware device
//          when block size is changed(For different region can has different block size) ,
//          delete old buffer and re-allocate new buffer
//     
//     *m_IOBuffer--IO buffer pointer
//    m_bufferSectorBegin--FAT FS sector index at the beginning of IO buffer
//    m_bufferSectorEnd--FAT FS sector index at the end of IO buffer
//    m_bufferDirty--IO buffer pointer has been rewrited and need to flush
////////////////////////////////////////////////////////////////

void FAT_SectorCache::Initialize( BlockStorageDevice* blockStorageDevice, UINT32 bytesPerSector, UINT32 baseAddress, UINT32 sectorCount )
{
    m_blockStorageDevice = blockStorageDevice;
    m_bytesPerSector     = bytesPerSector;
    m_sectorCount        = sectorCount;
    m_baseByteAddress    = baseAddress;
    m_sectorsPerLine     = SECTORCACHE_LINESIZE / bytesPerSector;
    m_LRUCounter         = 1;

    for(int i = 0; i < SECTORCACHE_MAXSIZE; i++)
    {
        m_cacheLines[i].m_buffer = NULL;
        m_cacheLines[i].m_flags  = 0;
    }
}

void FAT_SectorCache::Uninitialize()
{
    FAT_CacheLine* cacheLine;
    for(int i = 0; i < SECTORCACHE_MAXSIZE; i++)
    {
        cacheLine = &m_cacheLines[i];
        
        if(cacheLine->m_buffer)
        {
            FlushSector( cacheLine );

            private_free( cacheLine->m_buffer );

            cacheLine->m_buffer = NULL;
        }
    }
}

BYTE* FAT_SectorCache::GetSector( UINT32 sectorIndex, BOOL forWrite )
{
    if(sectorIndex > m_sectorCount)  //sectorIndex out of range of this device
        return NULL;

    FAT_CacheLine* cacheLine = GetCacheLine( sectorIndex );

    if(!cacheLine)
    {
        cacheLine = GetUnusedCacheLine();

        if(!cacheLine->m_buffer)
        {
            cacheLine->m_buffer = (BYTE*)private_malloc( SECTORCACHE_LINESIZE );

            if(!cacheLine->m_buffer) return NULL;
        }

        cacheLine->m_begin         = sectorIndex - (sectorIndex % m_sectorsPerLine);
        cacheLine->m_bsByteAddress = m_baseByteAddress + cacheLine->m_begin * m_bytesPerSector;
        cacheLine->m_flags         = 0;

        if(!m_blockStorageDevice->Read( cacheLine->m_bsByteAddress, SECTORCACHE_LINESIZE, cacheLine->m_buffer ))
        {
            private_free( cacheLine->m_buffer );
            
            cacheLine->m_buffer = NULL;
            
            return NULL;
        }
    }

    if(forWrite) cacheLine->SetDirty( TRUE );

    if((cacheLine->GetLRUCounter()) != m_LRUCounter)
    {
        m_LRUCounter++;

        cacheLine->SetLRUCOunter( m_LRUCounter );
    }

    return cacheLine->m_buffer + (sectorIndex - cacheLine->m_begin) * m_bytesPerSector;
}


void FAT_SectorCache::MarkSectorDirty( UINT32 sectorIndex )
{
    FAT_CacheLine* cacheLine = GetCacheLine( sectorIndex );

    if(cacheLine)
    {
        cacheLine->SetDirty( TRUE );
    }
}

FAT_SectorCache::FAT_CacheLine* FAT_SectorCache::GetUnusedCacheLine()
{
    FAT_CacheLine* cacheLine;
    FAT_CacheLine* topCandidate = NULL;
    UINT32 counter;
    UINT32 minLRUCounter = 0x7FFFFFFF;
    
    for(int i = 0; i < SECTORCACHE_MAXSIZE; i++)
    {
        cacheLine = &m_cacheLines[i];

        if(!cacheLine->m_buffer)
        {
            return cacheLine;
        }

        counter = cacheLine->GetLRUCounter();
        
        if(counter < minLRUCounter)
        {
            minLRUCounter = counter;
            topCandidate  = cacheLine;
        }
    }

    FlushSector( topCandidate );

    return topCandidate;
}


FAT_SectorCache::FAT_CacheLine* FAT_SectorCache::GetCacheLine( UINT32 sectorIndex )
{
    FAT_CacheLine* cacheLine;
    
    for(int i = 0; i < SECTORCACHE_MAXSIZE; i++)
    {
        cacheLine = &m_cacheLines[i];

        if(cacheLine->m_buffer && (sectorIndex >= cacheLine->m_begin) && (sectorIndex < cacheLine->m_begin + m_sectorsPerLine))
        {
            return cacheLine;
        }
    }

    return NULL;
}

/////////////////////////////////////////////////////////
// Description:
//  flush content in IOBuffer to real hardware storage
// 
// Input:
//  
//
// output:
//   
//
// Remarks:
// 
// Returns:
void FAT_SectorCache::FlushSector( UINT32 sectorIndex )
{
    FAT_CacheLine* cacheLine = GetCacheLine( sectorIndex );

    if(cacheLine)
    {
        FlushSector( cacheLine );
    }
}

void FAT_SectorCache::FlushSector( FAT_CacheLine* cacheLine )
{
    if(cacheLine->m_buffer && cacheLine->IsDirty())
    {        
        m_blockStorageDevice->Write( cacheLine->m_bsByteAddress, SECTORCACHE_LINESIZE, cacheLine->m_buffer, TRUE );

        cacheLine->SetDirty( FALSE );
    }
}

void FAT_SectorCache::FlushAll()
{
    for(int i = 0; i < SECTORCACHE_MAXSIZE; i++)
    {
        FlushSector( &m_cacheLines[i] );
    }
}

/////////////////////////////////////////////////////////
// Description:
//  flush one sector in ram buffer (full of 0) to hardware storage
// 
// Input:
//   sectorIndex
//
// output:
//
// Remarks:
// 
// Returns:
void FAT_SectorCache::EraseSector( UINT32 sectorIndex )
{
    BYTE* sector = GetSector( sectorIndex, TRUE );

    if(sector)
    {
        memset( sector, 0, m_bytesPerSector );
    }
}

