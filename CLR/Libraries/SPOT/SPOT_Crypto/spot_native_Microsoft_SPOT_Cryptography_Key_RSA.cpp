////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "..\SPOT.h"

HRESULT Library_spot_native_Microsoft_SPOT_Cryptography_Key_RSA::Encrypt___SZARRAY_U1__SZARRAY_U1__I4__I4__SZARRAY_U1( CLR_RT_StackFrame& stack )
{
    TINYCLR_HEADER();

    TINYCLR_SET_AND_LEAVE(AdvanceState( stack, RSA_ENCRYPT ));

    TINYCLR_NOCLEANUP();
}

HRESULT Library_spot_native_Microsoft_SPOT_Cryptography_Key_RSA::Decrypt___SZARRAY_U1__SZARRAY_U1__I4__I4__SZARRAY_U1( CLR_RT_StackFrame& stack )
{
    TINYCLR_HEADER();

    TINYCLR_SET_AND_LEAVE(AdvanceState( stack, RSA_DECRYPT ));

    TINYCLR_NOCLEANUP();
}

HRESULT Library_spot_native_Microsoft_SPOT_Cryptography_Key_RSA::VerifySignature___BOOLEAN__SZARRAY_U1__I4__I4__SZARRAY_U1__I4__I4( CLR_RT_StackFrame& stack )
{
    TINYCLR_HEADER();

    TINYCLR_SET_AND_LEAVE(AdvanceState( stack, RSA_VERIFYSIGNATURE ));

    TINYCLR_NOCLEANUP();
}

//--//

HRESULT Library_spot_native_Microsoft_SPOT_Cryptography_Key_RSA::AdvanceState( CLR_RT_StackFrame& stack, RSAOperations op )
{
    TINYCLR_HEADER();

    Library_spot_native_Microsoft_SPOT_CryptoState* st;

    if(stack.m_customPointer == NULL)
    {
        st = (Library_spot_native_Microsoft_SPOT_CryptoState*)CLR_RT_Memory::Allocate( sizeof(*st) ); CHECK_ALLOCATION(st);

        st->Init();

        stack.m_customPointer = st;

        stack.m_flags |= CLR_RT_StackFrame::c_CallOnPop;

        //--//

        TINYCLR_CHECK_HRESULT(st->LoadAsymmetricKey( stack ));

        //--//

        TINYCLR_CHECK_HRESULT(st->m_dataIn.Parse( &stack.Arg1() ));

        //--//

        switch(op)
        {
        case RSA_ENCRYPT:
            TINYCLR_CHECK_HRESULT(st->m_dataOut.Allocate( stack.PushValue(), RSAEncryptedSize( st->m_dataIn.m_count ) ));
            break;

        case RSA_DECRYPT:
            TINYCLR_CHECK_HRESULT(st->m_dataOut.Allocate( stack.PushValue(), st->m_dataIn.m_count ));
            break;

        case RSA_VERIFYSIGNATURE:
            TINYCLR_CHECK_HRESULT(st->m_dataOut.Parse( &stack.Arg4() ));
            break;
        }

        if(st->m_dataIn .m_array) st->m_dataIn .m_array->SetFlags( CLR_RT_HeapBlock::HB_Pinned );
        if(st->m_dataOut.m_array) st->m_dataOut.m_array->SetFlags( CLR_RT_HeapBlock::HB_Pinned );

        st->m_res = ::Crypto_StartRSAOperationWithKey( op, &st->m_keyAsymmetric, st->m_dataIn.Data(), st->m_dataIn.m_count, st->m_dataOut.Data(), st->m_dataOut.m_count, &st->m_handle );
    }
    else
    {
        st = (Library_spot_native_Microsoft_SPOT_CryptoState*)stack.m_customPointer;
    }

    if(st)
    {
        if(stack.m_flags & CLR_RT_StackFrame::c_CalledOnPop)
        {
            if(st->m_dataIn .m_array) st->m_dataIn .m_array->ResetFlags( CLR_RT_HeapBlock::HB_Pinned );
            if(st->m_dataOut.m_array) st->m_dataOut.m_array->ResetFlags( CLR_RT_HeapBlock::HB_Pinned );

            st->Cleanup();

            CLR_RT_Memory::Release( st );

            st                    = NULL;
            stack.m_customPointer = NULL;
        }
        else
        {
            while(true)
            {
                if(stack.m_owningThread->m_timeQuantumExpired) TINYCLR_SET_AND_LEAVE(CLR_E_RESTART_EXECUTION);

                switch(st->StepRSA())
                {
                case CRYPTO_CONTINUE:
                    break;

                case CRYPTO_SUCCESS:
                    if(op == RSA_VERIFYSIGNATURE)
                    {
                        stack.SetResult_Boolean( TRUE );
                    }

                    TINYCLR_SET_AND_LEAVE(S_OK);

                default:
                    if(op == RSA_VERIFYSIGNATURE)
                    {
                        stack.SetResult_Boolean( FALSE );
                        TINYCLR_SET_AND_LEAVE(S_OK);
                    }

                    TINYCLR_SET_AND_LEAVE(CLR_E_INVALID_PARAMETER);
                }
            }
        }
    }

    TINYCLR_NOCLEANUP();
}


