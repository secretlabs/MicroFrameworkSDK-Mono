#include <tinyclr/ssl_functions.h>
#include <openssl/ssl.h>
#include <openssl.h>

BOOL ssl_add_cert_auth_internal( int sslContextHandle, const char* certificate, 
	int cert_len, const char* szCertPwd )
{
    SSL *ssl = NULL;
    int ret = FALSE;
    
    if((sslContextHandle >= ARRAYSIZE(g_SSL_Driver.m_sslContextArray)) || (sslContextHandle < 0))
    {
        goto error;
    }

    ssl = (SSL*)g_SSL_Driver.m_sslContextArray[sslContextHandle].SslContext;
    if (ssl == NULL)
    {
        goto error;
    }

    // SSL_CTX_load_verify_locations is defined to accept a filename, however since
    // we do not support a filesystem the low level OpenSSL file access routines
    // have been written to fake file support via directly reading from a binary blob
    ret = SSL_CTX_load_verify_locations( SSL_get_SSL_CTX(ssl), certificate, NULL );

error:
	return ret;
}


