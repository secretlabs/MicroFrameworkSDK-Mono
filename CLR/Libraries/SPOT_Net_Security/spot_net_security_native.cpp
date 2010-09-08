////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "SPOT_Net_Security.h"


static const CLR_RT_MethodHandler method_lookup[] =
{
    Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative::SecureServerInit___STATIC__I4__I4__I4__MicrosoftSPOTNetSecurityX509Certificate__SZARRAY_MicrosoftSPOTNetSecurityX509Certificate,
    Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative::SecureClientInit___STATIC__I4__I4__I4__MicrosoftSPOTNetSecurityX509Certificate__SZARRAY_MicrosoftSPOTNetSecurityX509Certificate,
    Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative::UpdateCertificates___STATIC__VOID__I4__MicrosoftSPOTNetSecurityX509Certificate__SZARRAY_MicrosoftSPOTNetSecurityX509Certificate,
    Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative::SecureAccept___STATIC__VOID__I4__OBJECT,
    Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative::SecureConnect___STATIC__VOID__I4__STRING__OBJECT,
    Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative::SecureRead___STATIC__I4__OBJECT__SZARRAY_U1__I4__I4__I4,
    Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative::SecureWrite___STATIC__I4__OBJECT__SZARRAY_U1__I4__I4__I4,
    Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative::SecureCloseSocket___STATIC__I4__OBJECT,
    Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative::ExitSecureContext___STATIC__I4__I4,
    Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative::ParseCertificate___STATIC__VOID__SZARRAY_U1__STRING__BYREF_STRING__BYREF_STRING__BYREF_mscorlibSystemDateTime__BYREF_mscorlibSystemDateTime,
    Library_spot_net_security_native_Microsoft_SPOT_Net_Security_SslNative::DataAvailable___STATIC__I4__OBJECT,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
};

const CLR_RT_NativeAssemblyData g_CLR_AssemblyNative_Microsoft_SPOT_Net_Security =
{
    "Microsoft.SPOT.Net.Security", 
    0xCE14C821,
    method_lookup
};

