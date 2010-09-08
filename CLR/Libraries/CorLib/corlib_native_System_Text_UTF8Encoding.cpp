////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "CorLib.h"


HRESULT Library_corlib_native_System_Text_UTF8Encoding::GetBytes___SZARRAY_U1__STRING( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    size_t                  cBytes;
    CLR_RT_HeapBlock_Array* arr;
    LPCSTR                  str;
    CLR_RT_HeapBlock&       ret = stack.PushValueAndClear();

    str    = stack.Arg1().RecoverString(); FAULT_ON_NULL(str);
    cBytes = hal_strlen_s(str);    

    TINYCLR_CHECK_HRESULT(CLR_RT_HeapBlock_Array::CreateInstance( ret, (CLR_UINT32)cBytes, g_CLR_RT_WellKnownTypes.m_UInt8 ));

    arr = ret.DereferenceArray();

    memcpy( arr->GetFirstElement(), str, cBytes );

    TINYCLR_NOCLEANUP();
}

HRESULT Library_corlib_native_System_Text_UTF8Encoding::GetChars___SZARRAY_CHAR__SZARRAY_U1( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    LPSTR szText;
    CLR_RT_HeapBlock ref; ref.SetObjectReference( NULL );
    CLR_RT_ProtectFromGC gc( ref );
    CLR_RT_HeapBlock_Array* pArrayBytes = stack.Arg1().DereferenceArray(); FAULT_ON_NULL(pArrayBytes);
    CLR_RT_HeapBlock_Array* pArrayBytesCopy;
    CLR_RT_HeapBlock_Array* arrTmp;
    int cBytes;
    int cBytesCopy;

    _ASSERTE(pArrayBytes->m_typeOfElement == DATATYPE_U1);
    
    cBytes     = pArrayBytes->m_numOfElements;
    cBytesCopy = cBytes+1;
    
    /* Copy the array to a temporary buffer to create a zero-terminated string */
    TINYCLR_CHECK_HRESULT( CLR_RT_HeapBlock_Array::CreateInstance( ref, cBytesCopy, g_CLR_RT_WellKnownTypes.m_UInt8 ));
    
    pArrayBytesCopy = ref.DereferenceArray();
    szText = (LPSTR)pArrayBytesCopy->GetFirstElement(); 

    hal_strncpy_s( szText, cBytesCopy, (LPSTR)pArrayBytes->GetFirstElement(), cBytes );    

    TINYCLR_CHECK_HRESULT(Library_corlib_native_System_String::ConvertToCharArray( szText, stack.PushValueAndClear(), arrTmp, 0, -1 ));

    TINYCLR_NOCLEANUP();
}

