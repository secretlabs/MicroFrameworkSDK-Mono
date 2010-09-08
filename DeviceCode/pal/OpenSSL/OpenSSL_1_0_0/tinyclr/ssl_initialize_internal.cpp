#include <tinyclr/ssl_functions.h>
#include <openssl/ssl.h>
#include <openssl.h>

extern void EX_data_initalize(void);


BOOL ssl_initialize_internal()
{
    TINYCLR_SSL_MEMSET(&g_SSL_Driver, 0, sizeof(g_SSL_Driver));

    EX_data_initalize();
    SSLeay_add_ssl_algorithms();
    SSL_load_error_strings();

    return TRUE;
}

