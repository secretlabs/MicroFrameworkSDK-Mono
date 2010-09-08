
#include "e_os.h"
#ifdef OPENSSL_SYS_WINDOWS
#include <time.h>
#else
#include <tinyhal.h>
#endif

static struct tm g_timestruct;

int tinyclr_ssl_stat( const char *path, struct stat *buffer )
{
    return 0;
}

int tinyclr_ssl_toupper(int c)
{   
    int retval = c;
    if(('a' <= c) && (c <= 'z'))            
        retval = 'A' + (c - 'a');
    return retval;
} 
int tinyclr_ssl_tolower(int c)
{   
    int retval = c;
    if(('A' <= c) && (c <= 'Z'))            
        retval = 'a' + (c - 'A');

    return retval;
}
int tinyclr_strcmp ( const char * str1, const char * str2 )
{
    int n = TINYCLR_SSL_STRLEN(str1);
    while (*str1 && *str2 && n)
        {
        int res = *str1 - *str2;
        if (res) return res < 0 ? -1 : 1;
        str1++;
        str2++;
        n--;
        }
    if (n == 0)
        return 0;
    if (*str1)
        return 1;
    if (*str2)
        return -1;
    return 0;
}

size_t tinyclr_ssl_fwrite ( const void * ptr, size_t size, size_t count, void * stream )
{
    return 0;
};

int tinyclr_ssl_gettimeofday(void *tp, void *tzp)
{
    time_t localtime = Time_GetLocalTime(); //100nanoseconds 
    ((struct TINYCLR_SSL_TIMEVAL*)tp)->tv_sec = localtime / 10000000; //for seconds 
    ((struct TINYCLR_SSL_TIMEVAL*)tp)->tv_usec = (localtime % 10000000) / 1000; //for microseconds
    return 0;
};

int tinyclr_ssl_chmod(const char *filename, int pmode )
{
    return 0; // for now
};

struct servent *tinyclr_ssl_getservbyname(const char *name, const char *proto)
{
    // TODO - temp workaround
#ifndef TCPIP_LWIP 
    return getservbyname(name, proto);
#else
    debug_printf("tinyclr_ssl_getservbyname(%s,%s) stubbed for LWIP!\r\n",name,proto);
    return NULL;
#endif
}
struct hostent *tinyclr_ssl_gethostbyname(const char *name)
{
#ifndef TCPIP_LWIP 
    return gethostbyname(name);
#else
    debug_printf("tinyclr_ssl_gethostbyname(%s) stubbed for LWIP!\r\n",name);
    // undefined symbol, how is that possible ? return lwip_gethostbyname(name);
    return NULL;
#endif
}
struct hostent *tinyclr_ssl_gethostbyaddr(const char *addr, int length, int type)
{
#ifndef TCPIP_LWIP 
    return gethostbyaddr(addr, length, type);
#else
    debug_printf("tinyclr_ssl_gethostbyaddr(%s,%d,%d) stubbed for LWIP!\r\n",addr,length,type);
    return NULL;
#endif
}

pid_t tinyclr_ssl_getpid()
{
    return 0;
}

void tinyclr_qsort ( void * base, size_t num, size_t size, 
    int ( * comparator ) ( const void *, const void * ) )
{
    return;
}
    
// localtime as returned here is milliseconds elapsed since 1/1/1601 rather
// than that C library standard of milliseconds elapsed wince 1/1/1970. However
// the following implementation of localtime and gmtime handle this correctly.
time_t tinyclr_time ( time_t * timer )
{
    INT64 tim = Time_GetLocalTime();
    time_t localtime = (time_t)(tim / 10000); // convert from 100nano to milli seconds
    if (timer != NULL) *timer = localtime;
    return localtime;
}

// timer is milliseconds elapsed since 1/1/1601
struct tm * tinyclr_localtime ( const time_t * timer )
{
    SYSTEMTIME systime;
    INT64 tim = (INT64)(*timer);
    INT64 localtime = tim*10000; // convert from milli to 100nano seconds
    Time_ToSystemTime(localtime, &systime); 
    g_timestruct.tm_hour = systime.wHour;
    g_timestruct.tm_mday = systime.wDay;
    g_timestruct.tm_min = systime.wMinute;
    g_timestruct.tm_mon = systime.wMonth;
    g_timestruct.tm_sec = systime.wSecond;
    g_timestruct.tm_wday = systime.wDayOfWeek;
    g_timestruct.tm_year = systime.wYear - 1900; // years since 1900
    return &g_timestruct;
}

// timer is milliseconds elapsed since 1/1/1601
struct tm * tinyclr_gmtime ( const time_t * timer )
{
    SYSTEMTIME systime;
    INT64 offset = Time_GetTimeZoneOffset() * 600000000; //convert from min to 100nanoseconds
    INT64 tim = (INT64)(*timer);
    INT64 utctime = tim*10000 + offset;
    Time_ToSystemTime(utctime, &systime); 
    g_timestruct.tm_hour = systime.wHour;
    g_timestruct.tm_mday = systime.wDay;
    g_timestruct.tm_min = systime.wMinute;
    g_timestruct.tm_mon = systime.wMonth;
    g_timestruct.tm_sec = systime.wSecond;
    g_timestruct.tm_wday = systime.wDayOfWeek;
    g_timestruct.tm_year = systime.wYear - 1900; // years since 1900
    return &g_timestruct;
}

time_t tinyclr_mktime ( struct tm * timeptr )
{
    SYSTEMTIME systime;
    systime.wHour = timeptr->tm_hour;
    systime.wDay = timeptr->tm_mday;
    systime.wMinute = timeptr->tm_min;
    systime.wMonth = timeptr->tm_mon;
    systime.wSecond = timeptr->tm_sec;
    systime.wDayOfWeek = timeptr->tm_wday;
    systime.wYear = timeptr->tm_yday;
    time_t localtime = Time_FromSystemTime(&systime) / 10000; //convert from 100nano to milliseconds
    return localtime;
}

