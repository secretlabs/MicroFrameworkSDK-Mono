#include <e_os.h>
#include <tinyclr/ssl_functions.h>
#include <openssl/ssl.h>
#include <openssl.h>

int ssl_connect_internal(int sd, const char* szTargetHost, int sslContextHandle)
{
    int err = SOCK_SOCKET_ERROR;
    SSL *ssl = NULL;

    // Retrieve SSL struct from g_SSL_Driver    
    if((sslContextHandle >= ARRAYSIZE(g_SSL_Driver.m_sslContextArray)) || (sslContextHandle < 0))
    {
        goto error;
    }
    
    // sd should already have been created
    // Now do the SSL negotiation   
    ssl = (SSL*)g_SSL_Driver.m_sslContextArray[sslContextHandle].SslContext;
    if (ssl == NULL) goto error;

    if (!SSL_set_fd(ssl, sd))
    {
        goto error;
    }

    err = SSL_connect (ssl);    

    err = SSL_get_error(ssl,err);
      
    if(err == SSL_ERROR_WANT_READ)
    {
        err = SOCK_EWOULDBLOCK;
#ifndef TCPIP_LWIP
        SOCKET_DRIVER.ClearStatusBitsForSocket( sd, FALSE );        
#endif
    }
    else if(err == SSL_ERROR_WANT_WRITE)
    {
        err = SOCK_TRY_AGAIN;
#ifndef TCPIP_LWIP
        SOCKET_DRIVER.ClearStatusBitsForSocket( sd, TRUE );        
#endif
    }

    SOCKET_DRIVER.SetSocketSslData(sd, (void*)ssl);

error:
    return err;
}

