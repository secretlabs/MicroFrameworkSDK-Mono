#include <tinyclr/ssl_functions.h>
#include <openssl/ssl.h>
#include <openssl/err.h>
#include <openssl.h>


//TODO:  ctx, meth need to be OPENSSL_free'd somewhere (unitialize maybe?)
BOOL ssl_generic_init_internal( int sslMode, int sslVerify, const char* certificate, 
    int cert_len, int& sslContextHandle, BOOL isServer )
{
    SSL*                ssl = NULL;
    SSL_CTX*            ctx = NULL;
    SSL_METHOD*         meth = NULL;
    X509*               cert_x509 = NULL;
    BIO*                cert_bio = NULL;
    EVP_PKEY*           pkey = NULL;

    int                 sslCtxIndex = -1;

    for(int i=0; i<ARRAYSIZE(g_SSL_Driver.m_sslContextArray); i++)
    { 
        if(g_SSL_Driver.m_sslContextArray[i].SslContext == NULL)
        {
            sslCtxIndex = i;           
            break;
        }
    }
    
    if(sslCtxIndex == -1) return FALSE;


    if(isServer)
    {
        
        if ((cert_bio=BIO_new(BIO_s_mem())) == NULL)
        {
            ERR_print_errors_fp(OPENSSL_TYPE__FILE_STDERR);
            goto err;
        }
        BIO_puts(cert_bio,certificate);
        
        cert_x509 = PEM_read_bio_X509_AUX(cert_bio, NULL, 0, NULL);

        if (cert_x509 == NULL)
        {
            ERR_print_errors_fp(OPENSSL_TYPE__FILE_STDERR);
            goto err;
        }
        
        pkey = PEM_read_bio_PrivateKey(cert_bio, NULL,
                                       ctx->default_passwd_callback,
                                       ctx->default_passwd_callback_userdata);

        if (pkey == NULL)
        {
            ERR_print_errors_fp(OPENSSL_TYPE__FILE_STDERR);
            goto err;
        }

        if(sslMode & TINYCLR_SSL_MODE_TLS1)
        {
            meth = (SSL_METHOD*)TLSv1_server_method();
        }
        else
        {
            meth = (SSL_METHOD*)SSLv3_server_method();  
        }
        
        ctx = SSL_CTX_new (meth);

        if (ctx == NULL)
        {
            ERR_print_errors_fp(OPENSSL_TYPE__FILE_STDERR);
            goto err;
        }


        if (SSL_CTX_use_certificate(ctx, cert_x509) <= 0) 
        {
            ERR_print_errors_fp(OPENSSL_TYPE__FILE_STDERR);
            goto err;
        }
        if (SSL_CTX_use_PrivateKey(ctx, pkey) <= 0) 
        {
            ERR_print_errors_fp(OPENSSL_TYPE__FILE_STDERR);
            goto err;
        }
        
        if (!SSL_CTX_check_private_key(ctx)) 
        {
            TINYCLR_SSL_FPRINTF(OPENSSL_TYPE__FILE_STDERR,
                "Private key does not match the certificate public key\n");
            goto err;
        }

        // create the SSL object
        ssl = SSL_new(ctx);
        if (ssl == NULL)
        {
            ERR_print_errors_fp(OPENSSL_TYPE__FILE_STDERR);
            goto err;
        }

        X509_free(cert_x509);
        BIO_free(cert_bio);
    }
    else
    {
        if(sslMode & TINYCLR_SSL_MODE_TLS1)
        {
            meth = (SSL_METHOD*)TLSv1_client_method();
        }
        else
        {
            meth = (SSL_METHOD*)SSLv3_client_method();  
        }
        ctx = SSL_CTX_new (meth);
        if (ctx == NULL)
        {
            ERR_print_errors_fp(OPENSSL_TYPE__FILE_STDERR);
            goto err;
        }
        ssl = SSL_new(ctx);
        if (ssl == NULL)
        {
            ERR_print_errors_fp(OPENSSL_TYPE__FILE_STDERR);
            goto err;
        }
    }
    
    if (ssl == NULL) goto err;

    // TINYCLR_SSL_VERIFY_XXX >> 1 == SSL_VERIFY_xxx
    ssl->verify_mode = (sslVerify >> 1);

    g_SSL_Driver.m_sslContextArray[sslCtxIndex].SslContext = ssl;
    g_SSL_Driver.m_sslContextCount++;

    sslContextHandle = sslCtxIndex;

    return (ctx != NULL);

err:
    SSL_free(ssl);
    SSL_CTX_free(ctx);
    X509_free(cert_x509);
    BIO_free(cert_bio);
    EVP_PKEY_free(pkey);
    
    return FALSE;
}

