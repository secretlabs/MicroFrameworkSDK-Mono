////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "..\Core.h"

////////////////////////////////////////////////////////////////////////////////////////////////////


bool CLR_RT_Persistence_Manager::ObjectHeader::Initialize( CLR_RT_HeapBlock_WeakReference* wr )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return true;
}

bool CLR_RT_Persistence_Manager::ObjectHeader::IsGood( bool fIncludeData ) const
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return true;
}

void CLR_RT_Persistence_Manager::ObjectHeader::Delete()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

CLR_UINT32 CLR_RT_Persistence_Manager::ObjectHeader::Length() const
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return 0;
}

CLR_UINT32 CLR_RT_Persistence_Manager::ObjectHeader::Length( const CLR_RT_HeapBlock_WeakReference* ref )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return 0;
}

CLR_UINT32 CLR_RT_Persistence_Manager::ObjectHeader::Length( CLR_UINT32 data )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return 0;
}

CLR_RT_Persistence_Manager::ObjectHeader* CLR_RT_Persistence_Manager::ObjectHeader::Next() const
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return NULL;
}

CLR_UINT32 CLR_RT_Persistence_Manager::ObjectHeader::ComputeCRC() const
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return 0;
}

CLR_RT_Persistence_Manager::ObjectHeader* CLR_RT_Persistence_Manager::ObjectHeader::Find( FLASH_WORD* start, FLASH_WORD* end )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return NULL;
}

//--//--//--//--//--//

void CLR_RT_Persistence_Manager::BankHeader::Initialize()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

bool CLR_RT_Persistence_Manager::BankHeader::IsGood() const
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return true;
}

void CLR_RT_Persistence_Manager::BankHeader::Delete()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

CLR_RT_Persistence_Manager::BankHeader* CLR_RT_Persistence_Manager::BankHeader::Find( FLASH_WORD* start, FLASH_WORD* end )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return NULL;
}

//--//--//--//--//--//

bool CLR_RT_Persistence_Manager::Bank::Initialize( UINT32 kind )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();

    return false;
}

bool CLR_RT_Persistence_Manager::Bank::IsGood() const
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return true;
}

bool CLR_RT_Persistence_Manager::Bank::Erase( int& sectorIndex )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return true;
}

void CLR_RT_Persistence_Manager::Bank::EraseAll()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

bool CLR_RT_Persistence_Manager::Bank::Format()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return true;
}

void CLR_RT_Persistence_Manager::Bank::Switch( Bank& other )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

//--//

CLR_RT_Persistence_Manager::ObjectHeader* CLR_RT_Persistence_Manager::Bank::RecoverHeader( CLR_RT_HeapBlock_WeakReference* ref )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return NULL;
}

bool CLR_RT_Persistence_Manager::Bank::WriteHeader( CLR_RT_HeapBlock_WeakReference* ref, ObjectHeader*& pOH, FLASH_WORD*& pData )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return true;
}

//--//

bool CLR_RT_Persistence_Manager::Bank::CanWrite( FLASH_WORD* dst, CLR_UINT32 length )
{    
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return true;
}

bool CLR_RT_Persistence_Manager::Bank::Write( FLASH_WORD* dst, const FLASH_WORD* src, CLR_UINT32 length )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return true;
}

void CLR_RT_Persistence_Manager::Bank::Invalidate( FLASH_WORD* dst, FLASH_WORD match, CLR_UINT32 length )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

//--//

void CLR_RT_Persistence_Manager::Uninitialize()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}


void CLR_RT_Persistence_Manager::Initialize()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

void CLR_RT_Persistence_Manager::EraseAll()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

//--//

CLR_RT_Persistence_Manager::ObjectHeader* CLR_RT_Persistence_Manager::RecoverHeader( CLR_RT_HeapBlock_WeakReference* weak )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return NULL;
}

void CLR_RT_Persistence_Manager::InvalidateEntry( CLR_RT_HeapBlock_WeakReference* weak )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

//--//

void CLR_RT_Persistence_Manager::Callback( void* arg )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

void CLR_RT_Persistence_Manager::EnqueueNextCallback()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

void CLR_RT_Persistence_Manager::Relocate()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

bool CLR_RT_Persistence_Manager::AdvanceState( bool force )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return false;
}

void CLR_RT_Persistence_Manager::Flush()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

//--//

#if !defined(BUILD_RTM)
void CLR_RT_Persistence_Manager::GenerateStatistics( CLR_UINT32& totalSize, CLR_UINT32& inUse )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////

CLR_UINT32 CLR_RT_HeapBlock_WeakReference_Identity::ComputeCRC( const CLR_UINT8* ptr, CLR_UINT32 len ) const
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return 0;
}

//--//

HRESULT CLR_RT_HeapBlock_WeakReference::CreateInstance( CLR_RT_HeapBlock_WeakReference*& weakref )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    TINYCLR_FEATURE_STUB_RETURN();
}

HRESULT CLR_RT_HeapBlock_WeakReference::SetTarget( CLR_RT_HeapBlock& targetReference )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    TINYCLR_FEATURE_STUB_RETURN();
}

HRESULT CLR_RT_HeapBlock_WeakReference::GetTarget( CLR_RT_HeapBlock& targetReference )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    TINYCLR_FEATURE_STUB_RETURN();
}

void CLR_RT_HeapBlock_WeakReference::InsertInPriorityOrder()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}


void CLR_RT_HeapBlock_WeakReference::Relocate()
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}

////////////////////////////////////////////////////////////////////////////////////////////////////

bool CLR_RT_HeapBlock_WeakReference::PrepareForRecovery( CLR_RT_HeapBlock_Node* ptr, CLR_RT_HeapBlock_Node* end )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
    return false;
}

void CLR_RT_HeapBlock_WeakReference::RecoverObjects( CLR_RT_DblLinkedList& lstHeap )
{
    NATIVE_PROFILE_CLR_HEAP_PERSISTENCE();
}
