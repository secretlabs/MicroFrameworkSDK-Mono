////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef _SPOT_NET_SECURITY_NATIVE_H_
#define _SPOT_NET_SECURITY_NATIVE_H_

struct Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative
{
    TINYCLR_NATIVE_DECLARE(SecureServerInit___STATIC__I4__I4__I4__MicrosoftSPOTNetSecurityX509Certificate__SZARRAY_MicrosoftSPOTNetSecurityX509Certificate);
    TINYCLR_NATIVE_DECLARE(SecureClientInit___STATIC__I4__I4__I4__MicrosoftSPOTNetSecurityX509Certificate__SZARRAY_MicrosoftSPOTNetSecurityX509Certificate);
    TINYCLR_NATIVE_DECLARE(UpdateCertificates___STATIC__VOID__I4__MicrosoftSPOTNetSecurityX509Certificate__SZARRAY_MicrosoftSPOTNetSecurityX509Certificate);
    TINYCLR_NATIVE_DECLARE(SecureAccept___STATIC__VOID__I4__OBJECT);
    TINYCLR_NATIVE_DECLARE(SecureConnect___STATIC__VOID__I4__STRING__OBJECT);
    TINYCLR_NATIVE_DECLARE(SecureRead___STATIC__I4__OBJECT__SZARRAY_U1__I4__I4__I4);
    TINYCLR_NATIVE_DECLARE(SecureWrite___STATIC__I4__OBJECT__SZARRAY_U1__I4__I4__I4);
    TINYCLR_NATIVE_DECLARE(SecureCloseSocket___STATIC__I4__OBJECT);
    TINYCLR_NATIVE_DECLARE(ExitSecureContext___STATIC__I4__I4);
    TINYCLR_NATIVE_DECLARE(ParseCertificate___STATIC__VOID__SZARRAY_U1__STRING__BYREF_STRING__BYREF_STRING__BYREF_mscorlibSystemDateTime__BYREF_mscorlibSystemDateTime);
    TINYCLR_NATIVE_DECLARE(DataAvailable___STATIC__I4__OBJECT);

    //--//

    static HRESULT ReadWriteHelper( CLR_RT_StackFrame& stack, bool isWrite  );
    static HRESULT InitHelper     ( CLR_RT_StackFrame& stack, bool isServer );
    static HRESULT ThrowOnError   ( CLR_RT_StackFrame& stack, int err       );
    static void    ThrowError     ( CLR_RT_StackFrame& stack, int errorCode );
};

struct Library_spot_net_security_native_Microsoft_SPOT_Net_Security_X509Certificate
{
    static const int FIELD__m_certificate    = 1;
    static const int FIELD__m_password       = 2;
    static const int FIELD__m_issuer         = 3;
    static const int FIELD__m_subject        = 4;
    static const int FIELD__m_effectiveDate  = 5;
    static const int FIELD__m_expirationDate = 6;


    //--//

};

extern const CLR_RT_NativeAssemblyData g_CLR_AssemblyNative_Microsoft_SPOT_Net_Security;

#endif  //_SPOT_NET_SECURITY_NATIVE_H_
