#include <tinyclr/ssl_functions.h>
#include <openssl/ssl.h>
#include <openssl.h>

BOOL ssl_uninitialize_internal()
{
    BOOL result = TRUE;
    
    for(int i = 0; i<ARRAYSIZE(g_SSL_Driver.m_sslContextArray); i++)
    {
        if(g_SSL_Driver.m_sslContextArray[i].SslContext != NULL)
        {
            ssl_exit_context_internal( i );
        }
    }
      
    g_SSL_Driver.m_sslContextCount = 0;

    return result;
}




