////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "..\SPOT.h"


//--//

HRESULT Library_spot_native_Microsoft_SPOT_CryptoState::Buffer::Parse( CLR_RT_HeapBlock* ptr )
{
    TINYCLR_HEADER();

    m_array  = ptr[ 0 ].DereferenceArray(); FAULT_ON_NULL(m_array);
    m_offset = ptr[ 1 ].NumericByRefConst().s4;
    m_count  = ptr[ 2 ].NumericByRefConst().s4;

    TINYCLR_NOCLEANUP();
}

HRESULT Library_spot_native_Microsoft_SPOT_CryptoState::Buffer::Allocate( CLR_RT_HeapBlock& ref, int size )
{
    TINYCLR_HEADER();

    ref.SetObjectReference( NULL );

    if(size)
    {
        TINYCLR_CHECK_HRESULT(CLR_RT_HeapBlock_Array::CreateInstance( ref, size, g_CLR_RT_WellKnownTypes.m_UInt8 ));

        m_array = ref.DereferenceArray();
    }
    else
    {
        m_array = NULL;
    }

    m_offset = 0;
    m_count  = size;

    TINYCLR_NOCLEANUP();
}

CLR_UINT8* Library_spot_native_Microsoft_SPOT_CryptoState::Buffer::Data()
{
    return m_array->GetFirstElement() + m_offset;
}

//--//

void Library_spot_native_Microsoft_SPOT_CryptoState::Init()
{
    memset( this, 0, sizeof(*this) );
}

void Library_spot_native_Microsoft_SPOT_CryptoState::Cleanup()
{
    ::Crypto_AbortRSAOperation( &m_handle );
}

HRESULT Library_spot_native_Microsoft_SPOT_CryptoState::LoadSymmetricKey( CLR_RT_StackFrame& stack )
{
    TINYCLR_HEADER();

    CLR_RT_HeapBlock*       pThis;
    CLR_RT_HeapBlock_Array* array;

    pThis = stack.This()                                                                                                           ; FAULT_ON_NULL(pThis);
    array = pThis[ Library_spot_native_Microsoft_SPOT_Cryptography_Key_TinyEncryptionAlgorithm::FIELD__m_value ].DereferenceArray(); FAULT_ON_NULL(array);

    memcpy( m_keySymmetric.Seed, array->GetFirstElement(), ARRAYSIZE(m_keySymmetric.Seed) );

    if(pThis[ Library_spot_native_Microsoft_SPOT_Cryptography_Key_TinyEncryptionAlgorithm::FIELD__m_doublyEncrypted ].NumericByRefConst().u1 != 0)
    {
        KeySeed deviceKey;
        UINT8   iv[ TEA_KEY_SIZE_BYTES ]; memset( iv, 0, sizeof(iv) );

        GetDeviceKey( (UINT8*)deviceKey.Seed );

        ::Crypto_Decrypt( deviceKey.Seed, iv, sizeof(iv), m_keySymmetric.Seed, sizeof(m_keySymmetric.Seed), m_keySymmetric.Seed, sizeof(m_keySymmetric.Seed) );
    }

    TINYCLR_NOCLEANUP();
}

HRESULT Library_spot_native_Microsoft_SPOT_CryptoState::LoadAsymmetricKey( CLR_RT_StackFrame& stack )
{
    TINYCLR_HEADER();

    CLR_RT_HeapBlock*       pThis;
    CLR_RT_HeapBlock_Array* array;

    pThis = stack.This(); FAULT_ON_NULL(pThis);

    //
    // Init RSA key.
    //
    array = pThis[ Library_spot_native_Microsoft_SPOT_Cryptography_Key_RSA::FIELD__m_modulus ].DereferenceArray(); FAULT_ON_NULL(array);
    memcpy( &m_keyAsymmetric.module, array->GetFirstElement(), sizeof(m_keyAsymmetric.module) );

    array = pThis[ Library_spot_native_Microsoft_SPOT_Cryptography_Key_RSA::FIELD__m_exponent ].DereferenceArray(); FAULT_ON_NULL(array);
    memcpy( &m_keyAsymmetric.exponent, array->GetFirstElement(), sizeof(m_keyAsymmetric.exponent) );

    m_keyAsymmetric.exponent_len = RSA_KEY_SIZE_BYTES / sizeof(DWORD);

    TINYCLR_NOCLEANUP();
}

//--//

BOOL Library_spot_native_Microsoft_SPOT_CryptoState::Execute( CryptoFpn fpn )
{
    CLR_UINT8* IVPtr;
    CLR_UINT32 IVLen;
    UINT8      tmp[ TEA_KEY_SIZE_BYTES ];

    if(m_IV)
    {
        IVPtr = m_IV->GetFirstElement();
        IVLen = m_IV->m_numOfElements;
    }
    else
    {
        IVPtr = tmp;
        IVLen = sizeof(tmp);

        memset( tmp, 0, sizeof(tmp) );
    }

    return fpn( m_keySymmetric.Seed, IVPtr, IVLen, m_dataIn.Data(), m_dataIn.m_count, m_dataOut.Data(), m_dataOut.m_count );
}

CRYPTO_RESULT Library_spot_native_Microsoft_SPOT_CryptoState::StepRSA()
{
    if(m_res == CRYPTO_CONTINUE)
    {
        m_res = ::Crypto_StepRSAOperation( &m_handle );
    }

    return m_res;
}

//--//

void Library_spot_native_Microsoft_SPOT_CryptoState::GetSeed( KeySeed& seed )
{
    UINT8 buffer[ sizeof(KeySeed) ];

    ::SecurityKey_Copy( buffer, sizeof(buffer) );

    ::Crypto_KeySeedFromLaser( buffer, &seed );
}

void Library_spot_native_Microsoft_SPOT_CryptoState::GetDeviceKey( UINT8* key )
{
    KeySeed keySeed;

    GetSeed( keySeed );

    ::GetC1Value( (BYTE*)&keySeed, key, TEA_KEY_SIZE_BYTES );
}

void Library_spot_native_Microsoft_SPOT_CryptoState::GetDeviceKey( RSAKey& key )
{
    KeySeed keySeed;

    GetSeed( keySeed );

    ::Crypto_GeneratePrivateKey( &keySeed, &key );
}

