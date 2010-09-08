#include "e_os.h"
#include <tinyhal.h>
#include <tinyclr/ssl_functions.h>
#include <openssl/asn1.h>
#include <openssl/pem.h>
#include <openssl/err.h>

static const char *mon[12]=
{ 
    "Jan","Feb","Mar","Apr","May","Jun",
    "Jul","Aug","Sep","Oct","Nov","Dec"
};

static int ssl_get_ASN1_UTCTIME(BIO *bp, const ASN1_UTCTIME *tm, DATE_TIME_INFO *dti);
static X509 *ssl_load_cert(const char *cert_buf, int format, X509CertData* x509,
                           const char *pass, const char *descrip);

BOOL ssl_parse_certificate_internal(void * bytes, size_t size,  void * pwd, void* x509CertData, int format)
{
    // load certificate 
    X509* x = ssl_load_cert((const char*)bytes, format, (X509CertData*)x509CertData, NULL, "Certificate");

    BOOL ret = (x!=NULL ? TRUE: FALSE);

    X509_free(x);

    return (ret);
}

static X509 *ssl_load_cert(const char *cert_buf, int format, X509CertData* x509,
                           const char *pass, const char *descrip)
{
    BUF_MEM *buf=NULL;
    X509 *x=NULL;
    BIO *cert;
    char *name,*subject;

    if ((cert=BIO_new(BIO_s_mem())) == NULL)
    {
        ERR_print_errors_fp(OPENSSL_TYPE__FILE_STDERR);
        goto end;
    }
    BIO_puts(cert,cert_buf);
    
    x=PEM_read_bio_X509_AUX(cert, NULL, 0, NULL);
    name=X509_NAME_oneline(X509_get_issuer_name(x),NULL,0);
    subject=X509_NAME_oneline(X509_get_subject_name(x),NULL,0);

    TINYCLR_SSL_STRNCPY(x509->Issuer, name, TINYCLR_SSL_STRLEN(name));
    TINYCLR_SSL_STRNCPY(x509->Subject, subject, TINYCLR_SSL_STRLEN(subject));

    ssl_get_ASN1_UTCTIME(cert, X509_get_notBefore(x), &x509->EffectiveDate);
    ssl_get_ASN1_UTCTIME(cert, X509_get_notAfter(x), &x509->ExpirationDate);

#if defined(DEBUG) || defined(_DEBUG)
    TINYCLR_SSL_PRINTF("\n        Issuer: ");
    TINYCLR_SSL_PRINTF(name);
    TINYCLR_SSL_PRINTF("\n",1);
    TINYCLR_SSL_PRINTF("        Validity\n");
    TINYCLR_SSL_PRINTF("            Not Before: ");
    TINYCLR_SSL_PRINTF("%s %2d %02d:%02d:%02d %d%s",
        mon[x509->EffectiveDate.month-1],
        x509->EffectiveDate.day,
        x509->EffectiveDate.hour,
        x509->EffectiveDate.minute,
        x509->EffectiveDate.second,
        x509->EffectiveDate.year,
        (x509->EffectiveDate.tzOffset)?" GMT":"");
    TINYCLR_SSL_PRINTF("\n            Not After : ");
    TINYCLR_SSL_PRINTF("%s %2d %02d:%02d:%02d %d%s",
        mon[x509->ExpirationDate.month-1],
        x509->ExpirationDate.day,
        x509->ExpirationDate.hour,
        x509->ExpirationDate.minute,
        x509->ExpirationDate.second,
        x509->ExpirationDate.year,
        (x509->ExpirationDate.tzOffset)?" GMT":"");
    TINYCLR_SSL_PRINTF("\n");
    TINYCLR_SSL_PRINTF("        Subject: ");
    TINYCLR_SSL_PRINTF(subject);
    TINYCLR_SSL_PRINTF("\n");
#endif

end:
    if (x == NULL)
    {
        TINYCLR_SSL_PRINTF("Unable to load certificate\n");
        ERR_print_errors_fp(OPENSSL_TYPE__FILE_STDERR);
    }

    OPENSSL_free(name);
    OPENSSL_free(subject);
    if (cert != NULL) BIO_free(cert);
    if (buf != NULL) BUF_MEM_free(buf);
    return(x);
}

//MS: copied decoding algo from get_ASN1_UTCTIME of asn1_openssl.lib
//MS: populate DATE_TIME_INFO struct with year,month, day,hour,minute,second,etc
static int ssl_get_ASN1_UTCTIME(BIO *bp, const ASN1_UTCTIME *tm, DATE_TIME_INFO *dti)
{
    const char *v;
    int gmt=0;
    int i;
    int y=0,M=0,d=0,h=0,m=0,s=0;

    i=SSL_LONG_LITTLE_ENDIAN(tm->length);
    v=(const char *)tm->data;

    if (i < 10) { goto err; }
    if (v[i-1] == 'Z') gmt=1;
    for (i=0; i<10; i++)
        if ((v[i] > '9') || (v[i] < '0')) { goto err; }
    y= (v[0]-'0')*10+(v[1]-'0');
    if (y < 50) y+=100;
    M= (v[2]-'0')*10+(v[3]-'0');
    if ((M > 12) || (M < 1)) { goto err; }
    d= (v[4]-'0')*10+(v[5]-'0');
    h= (v[6]-'0')*10+(v[7]-'0');
    m=  (v[8]-'0')*10+(v[9]-'0');
    if (tm->length >=12 &&
        (v[10] >= '0') && (v[10] <= '9') &&
        (v[11] >= '0') && (v[11] <= '9'))
        s=  (v[10]-'0')*10+(v[11]-'0');

    dti->year = y+1900;
    dti->month = M;
    dti->day = d;
    dti->hour = h;
    dti->minute = m;
    dti->second = s;
    dti->dlsTime = 0; //TODO:HOW to find
    dti->tzOffset = gmt; //TODO:How to find

    return(1);

    
err:
    TINYCLR_SSL_PRINTF("Bad time value\r\n");
    return(0);
}
