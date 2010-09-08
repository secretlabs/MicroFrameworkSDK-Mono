////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "CorLib.h"

#if !defined(TINYCLR_EMULATED_FLOATINGPOINT)
#include <math.h>

HRESULT Library_corlib_native_System_Math::Round___STATIC__R8__R8( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    double d = stack.Arg0().NumericByRefConst().r8;
    double hi = d + 0.5;
    double res = floor( hi );

    //If the number was in the middle of two integers, we need to round to the even one.
    if(res==hi)
    {
        if(fmod( res, 2.0 ) != 0)
        {
            //Rounding up made the number odd so we should round down.
            res -= 1.0;
        }
    }
	
    stack.PushValue().SetDouble( res );

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_corlib_native_System_Math::Ceiling___STATIC__R8__R8( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    double d = stack.Arg0().NumericByRefConst().r8;
    double c = ceil( d );

    stack.PushValue().SetDouble( c );

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_corlib_native_System_Math::Floor___STATIC__R8__R8( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    double d = stack.Arg0().NumericByRefConst().r8;
    double f = floor( d );

    stack.PushValue().SetDouble( f );

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_corlib_native_System_Math::Pow___STATIC__R8__R8__R8( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    double x = stack.Arg0().NumericByRefConst().r8;
    double y = stack.Arg1().NumericByRefConst().r8;

    double z = pow( x, y );

    stack.PushValue().SetDouble( z );

    TINYCLR_NOCLEANUP_NOLABEL();

}

#else


/// No floating point 
HRESULT Library_corlib_native_System_Math::Round___STATIC__R8__R8( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_INT64 d = stack.Arg0().NumericByRefConst().r8;

    //for negative number we have to be banker's round, if -0.5, round to 0, but 0.5 to 0
    if (d <0) d =d + 1; 
    CLR_INT64 res = (CLR_INT64)(d + (CLR_INT64)(CLR_RT_HeapBlock::HB_DoubleMask>>1) ) & (~CLR_RT_HeapBlock::HB_DoubleMask);

    stack.PushValue().SetDouble( res );

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_corlib_native_System_Math::Ceiling___STATIC__R8__R8( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_INT64  d = stack.Arg0().NumericByRefConst().r8;

    CLR_INT64 c = (CLR_INT64)(d + (CLR_INT64)CLR_RT_HeapBlock::HB_DoubleMask) & (~CLR_RT_HeapBlock::HB_DoubleMask);

    stack.PushValue().SetDouble( c);

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_corlib_native_System_Math::Floor___STATIC__R8__R8( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_INT64 d = stack.Arg0().NumericByRefConst().r8;

    CLR_INT64 f = (CLR_INT64)( d  & (~CLR_RT_HeapBlock::HB_DoubleMask) );

    stack.PushValue().SetDouble( f );

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_corlib_native_System_Math::Pow___STATIC__R8__R8__R8( CLR_RT_StackFrame& stack )
{

    TINYCLR_HEADER();

    TINYCLR_SET_AND_LEAVE(stack.NotImplementedStub());

    TINYCLR_NOCLEANUP();

}

#endif

