////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "..\SPOT.h"

HRESULT Library_spot_native_Microsoft_SPOT_Cryptography_Key_TinyEncryptionAlgorithm::Encrypt___SZARRAY_U1__SZARRAY_U1__I4__I4__SZARRAY_U1( CLR_RT_StackFrame& stack )
{
    TINYCLR_HEADER();

    Library_spot_native_Microsoft_SPOT_CryptoState st;

    TINYCLR_CHECK_HRESULT(Initialize( stack, st, false, -1 ));

    st.Execute( Crypto_Encrypt );

    TINYCLR_NOCLEANUP();
}

HRESULT Library_spot_native_Microsoft_SPOT_Cryptography_Key_TinyEncryptionAlgorithm::Decrypt___SZARRAY_U1__SZARRAY_U1__I4__I4__SZARRAY_U1( CLR_RT_StackFrame& stack )
{
    TINYCLR_HEADER();

    Library_spot_native_Microsoft_SPOT_CryptoState st;

    TINYCLR_CHECK_HRESULT(Initialize( stack, st, false, -1 ));

    st.Execute( Crypto_Decrypt );

    TINYCLR_NOCLEANUP();
}

HRESULT Library_spot_native_Microsoft_SPOT_Cryptography_Key_TinyEncryptionAlgorithm::get_Signature___SZARRAY_U1( CLR_RT_StackFrame& stack )
{
    TINYCLR_HEADER();

    const int                                      c_SizeOfDeviceID = 5;
    BYTE                                           rgDeviceID[ c_SizeOfDeviceID ];
    Library_spot_native_Microsoft_SPOT_CryptoState st;

    TINYCLR_CHECK_HRESULT(Initialize( stack, st, true, sizeof(rgDeviceID) ));

    if(::Crypto_GetFingerprint( st.m_keySymmetric.Seed, rgDeviceID, ARRAYSIZE(rgDeviceID) ) != CRYPTO_SUCCESS)
    {
        TINYCLR_SET_AND_LEAVE(CLR_E_FAIL);
    }

    memcpy( st.m_dataOut.m_array->GetFirstElement(), rgDeviceID, sizeof(rgDeviceID) );

    TINYCLR_NOCLEANUP();
}

HRESULT Library_spot_native_Microsoft_SPOT_Cryptography_Key_TinyEncryptionAlgorithm::GetActivationString___STRING__U2__U2( CLR_RT_StackFrame& stack )
{
    TINYCLR_HEADER();

    char                                           szWatchID[ ACTIVATION_STRING_SIZE+1 ];
    UINT16                                         region;
    UINT16                                         model;
    Library_spot_native_Microsoft_SPOT_CryptoState st;

    TINYCLR_CHECK_HRESULT(Initialize( stack, st, true, 0 ));

    region = stack.Arg1().NumericByRefConst().u2;
    model  = stack.Arg2().NumericByRefConst().u2;

    szWatchID[ 0 ] = 0;
    if(::Crypto_GetActivationStringFromSeed( szWatchID, MAXSTRLEN(szWatchID), &st.m_keySymmetric, region, model ) == FALSE)
    {
        TINYCLR_SET_AND_LEAVE(CLR_E_FAIL);
    }

    TINYCLR_SET_AND_LEAVE(stack.SetResult_String( szWatchID ));

    TINYCLR_NOCLEANUP();
}

//--//

HRESULT Library_spot_native_Microsoft_SPOT_Cryptography_Key_TinyEncryptionAlgorithm::Initialize( CLR_RT_StackFrame& stack, Library_spot_native_Microsoft_SPOT_CryptoState& st, bool fKeyOnly, int resultLength )
{
    TINYCLR_HEADER();

    st.Init();

    TINYCLR_CHECK_HRESULT(st.LoadSymmetricKey( stack ));

    if(fKeyOnly == false)
    {
        TINYCLR_CHECK_HRESULT(st.m_dataIn.Parse( &stack.Arg1() ));

        st.m_IV = stack.Arg4().DereferenceArray();
    }

    if(resultLength != 0)
    {
        TINYCLR_CHECK_HRESULT(st.m_dataOut.Allocate( stack.PushValue(), resultLength < 0 ? st.m_dataIn.m_count : resultLength ));
    }

    TINYCLR_NOCLEANUP();
}

