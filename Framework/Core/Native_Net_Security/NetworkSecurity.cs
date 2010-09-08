////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Net.Security")]

namespace Microsoft.SPOT.Net.Security
{
    [FlagsAttribute]
    public enum SslProtocols
    {
        None = 0x00,
        //SSLv2   = 0x04,  // NO longer supported (obsolete and insecure)
        SSLv3 = 0x08,
        TLSv1 = 0x10,
        Default = SSLv3 | TLSv1,
    }

    public enum SslVerification
    {
        NoVerification = 1,
        VerifyPeer = 2,
        CertificateRequired = 4,
        VerifyClientOnce = 8,
    }

    [Serializable()]
    public class X509Certificate
    {
        private byte[] m_certificate;
        private string m_password;

        // The following members are extracted from the raw certificate data, so no need to serialize them.
        [NonSerialized()]
        private string m_issuer;
        [NonSerialized()]
        private string m_subject;
        [NonSerialized()]
        private DateTime m_effectiveDate;
        [NonSerialized()]
        private DateTime m_expirationDate;

        public X509Certificate(byte[] certificate)
            : this(certificate, "")
        {
        }

        public X509Certificate(byte[] certificate, string password)
        {
            m_certificate = certificate;
            m_password = password;

            Initialize();
        }

        public void Initialize()
        {
            SslNative.ParseCertificate(m_certificate, m_password, ref m_issuer, ref m_subject, ref m_effectiveDate, ref m_expirationDate);
        }

        public string Issuer
        {
            get { return m_issuer; }
        }

        public string Subject
        {
            get { return m_subject; }
        }

        public DateTime GetEffectiveDate()
        {
            return m_effectiveDate;
        }

        public DateTime GetExpirationDate()
        {
            return m_expirationDate;
        }

        public byte[] GetRawCertData()
        {
            return m_certificate;
        }
    }

    internal static class SslNative
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int SecureServerInit(int sslProtocols, int sslCertVerify, X509Certificate certificate, X509Certificate[] ca);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int SecureClientInit(int sslProtocols, int sslCertVerify, X509Certificate certificate, X509Certificate[] ca);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void UpdateCertificates(int contextHandle, X509Certificate certificate, X509Certificate[] ca);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SecureAccept(int contextHandle, object socket);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SecureConnect(int contextHandle, string targetHost, object socket);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int SecureRead(object socket, byte[] buffer, int offset, int size, int timeout_ms);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int SecureWrite(object socket, byte[] buffer, int offset, int size, int timeout_ms);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int SecureCloseSocket(object socket);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int ExitSecureContext(int contextHandle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ParseCertificate(byte[] cert, string password, ref string issuer, ref string subject, ref DateTime effectiveDate, ref DateTime expirationDate);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int DataAvailable(object socket);
    }
}


