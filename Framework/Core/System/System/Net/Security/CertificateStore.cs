////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.Security;

namespace Microsoft.SPOT.Net.Security
{
    /// <summary>
    /// The CertificateStore class is intended to enable users to store and persist CA and personal certificates in a central location.
    /// This class uses Extended Weak References (EWR) to persist the certificate data.  This abstracts the specifcs of EWR from the user
    /// and allows for certificate updates over the wire.
    /// </summary>
    public static class CertificateStore
    {
        internal class _CertStore_ { }

        [Serializable()]
        internal class _CALookup_
        {
            internal ArrayList Names = new ArrayList();
            internal ArrayList Values = new ArrayList();
        }

        [Serializable()]
        internal class _CertLookup_
        {
            private ArrayList m_names = new ArrayList();
            private ArrayList m_values = new ArrayList();

            internal bool Add(string certName, X509Certificate cert)
            {
                if (-1 != m_names.IndexOf(certName)) return false;

                m_names.Add(certName);

                uint idx = GetNextFreeIndex();

                m_values.Add(idx);

                ExtendedWeakReference ewr = ExtendedWeakReference.RecoverOrCreate(typeof(_CertStore_), (uint)idx, ExtendedWeakReference.c_SurvivePowerdown);

                ewr.Priority = (int)ExtendedWeakReference.PriorityLevel.Critical;

                ewr.Target = cert;

                ewr.PushBackIntoRecoverList();

                return true;
            }

            internal X509Certificate Get(string certName)
            {
                int idx = m_names.IndexOf(certName);

                if (-1 == idx) return null;

                ExtendedWeakReference ewr = ExtendedWeakReference.Recover(typeof(_CertStore_), (uint)m_values[idx]);

                if (ewr == null) return null;

                X509Certificate cert = ewr.Target as X509Certificate;

                if (cert.Issuer == null || cert.Issuer.Length == 0)
                {
                    cert.Initialize();
                }

                ewr.PushBackIntoRecoverList();

                return cert;
            }

            internal bool Update(string certName, X509Certificate cert)
            {
                int idx = m_names.IndexOf(certName);

                if (-1 == idx) return false;

                ExtendedWeakReference ewr = ExtendedWeakReference.Recover(typeof(_CertStore_), (uint)m_values[idx]);

                if (ewr == null) return false;

                ewr.Target = cert;
                ewr.PushBackIntoRecoverList();

                return true;
            }

            internal bool Remove(string certName)
            {
                int idx = m_names.IndexOf(certName);

                if (idx == -1) return false;

                ExtendedWeakReference ewr = ExtendedWeakReference.Recover(typeof(_CertStore_), (uint)m_values[idx]);

                if (ewr != null)
                {

                    m_names.RemoveAt(idx);
                    m_values.RemoveAt(idx);

                    ewr.Target = null;
                }

                return true;
            }

            private uint GetNextFreeIndex()
            {
                uint idx = 0;

                if (m_values.Count > 0)
                {
                    idx = (uint)m_values[m_values.Count - 1] + 1;

                    // overflow - collapse indexes
                    if (idx == 0)
                    {
                        for (int i = 0; i < m_values.Count; i++)
                        {
                            // if the current index matches the lookup value then we are already in the correct position
                            if (idx == (uint)m_values[i])
                            {
                                idx++;
                            }
                            else
                            {
                                // collapse cert indexes
                                ExtendedWeakReference ewr = ExtendedWeakReference.Recover(typeof(_CertStore_), (uint)m_values[i]);

                                if (ewr != null)
                                {
                                    X509Certificate cert = ewr.Target as X509Certificate;
                                    ewr.Target = null;

                                    ewr = new ExtendedWeakReference(cert, typeof(_CertStore_), idx, ExtendedWeakReference.c_SurvivePowerdown);
                                    ewr.Priority = (int)ExtendedWeakReference.PriorityLevel.Critical;
                                    ewr.Target = cert;
                                    ewr.PushBackIntoRecoverList();

                                    m_values[i] = (uint)idx;

                                    // increment active index count only if we have a valid EWR certificate
                                    idx++;
                                }
                            }
                        }
                    }
                }

                return idx;
            }
        }

        internal static ExtendedWeakReference s_ewrCertLookup;
        internal static ExtendedWeakReference s_ewrCALookup;
        internal static _CertLookup_ s_personalCertData;
        internal static _CALookup_ s_CACertData;

        public enum CertificateNotificationType
        {
            Added,
            Removed,
            Updated,
        }

        public delegate void CertificateNotificationMessage(CertificateNotificationType type, string certName);

        public static event CertificateNotificationMessage OnCACertificateChange;

        /// <summary>
        /// Static constructor so that we can load the certificate map from EWR prior to usage.
        /// </summary>
        static CertificateStore()
        {
            s_ewrCertLookup = ExtendedWeakReference.RecoverOrCreate(typeof(_CertLookup_), (uint)0, ExtendedWeakReference.c_SurvivePowerdown);

            if (s_ewrCertLookup.Target == null)
            {
                s_ewrCertLookup.Priority = (int)ExtendedWeakReference.PriorityLevel.Critical;

                s_personalCertData = new _CertLookup_();

                s_ewrCertLookup.Target = s_personalCertData;
            }
            else
            {
                s_personalCertData = s_ewrCertLookup.Target as _CertLookup_;
            }

            s_ewrCALookup = ExtendedWeakReference.RecoverOrCreate(typeof(_CertLookup_), (uint)1, ExtendedWeakReference.c_SurvivePowerdown);

            if (s_ewrCALookup.Target == null)
            {
                s_ewrCALookup.Priority = (int)ExtendedWeakReference.PriorityLevel.Critical;

                s_CACertData = new _CALookup_();

                s_ewrCALookup.Target = s_CACertData;
            }
            else
            {
                s_CACertData = s_ewrCALookup.Target as _CALookup_;
            }

        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void ClearPersonalCertificates()
        {
            s_personalCertData = new _CertLookup_();
            s_ewrCertLookup.Target = s_personalCertData;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void ClearCACertificates()
        {
            s_CACertData = new _CALookup_();
            s_ewrCALookup.Target = s_CACertData;
        }

        public static void ClearAllCertificates()
        {
            ClearPersonalCertificates();
            ClearCACertificates();
        }

        /// <summary>
        /// Add a personal certificate to the store.
        /// </summary>
        /// <param name="certName">Friendly name for the certificate (user defined, case sensitive)</param>
        /// <param name="cert">X509 certificate to be stored</param>
        /// <returns>Returns false if certificate already exists, otherwise true</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool AddPersonalCertificate(string certName, X509Certificate cert)
        {
            if (certName == null || cert == null) throw new ArgumentException();

            if (!s_personalCertData.Add(certName, cert)) throw new ArgumentException();

            s_ewrCertLookup.Target = s_personalCertData;
            s_ewrCertLookup.PushBackIntoRecoverList();

            return true;
        }

        /// <summary>
        /// Adds a Certificate Authority (CA) certificate to the store and notifies event subscribers of the addition.
        /// </summary>
        /// <param name="certName">Friendly name of the certificate (user defined, case sensitive)</param>
        /// <param name="cert">X509 certificate to be stored</param>
        /// <returns>Returns false if the certificate already exists, true otherwise</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool AddCACertificate(string certName, X509Certificate cert)
        {
            if (certName == null || cert == null) throw new ArgumentException();

            if (-1 != s_CACertData.Names.IndexOf(certName)) throw new ArgumentException();

            s_CACertData.Names.Add(certName);
            s_CACertData.Values.Add(cert);

            s_ewrCALookup.Target = s_CACertData;
            s_ewrCALookup.PushBackIntoRecoverList();

            if (OnCACertificateChange != null)
            {
                OnCACertificateChange(CertificateNotificationType.Added, certName);
            }

            return true;
        }

        /// <summary>
        /// Gets a personal certificate from the store given the user defined certificate name.
        /// </summary>
        /// <param name="certName">User defined certificate name (case sensitive)</param>
        /// <returns>Returns null if the certificate does not exist, otherwise, it returns the certificate.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static X509Certificate GetPersonalCertificate(string certName)
        {
            return s_personalCertData.Get(certName);
        }

        /// <summary>
        /// Gets a CA certificate from the store given the user defined certificate name.
        /// </summary>
        /// <param name="certName">User defined certificate name (case sensitive)</param>
        /// <returns>Returns null if the certificate does not exist, otherwise, it returns the certificate.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static X509Certificate GetCACertificate(string certName)
        {
            int idx = s_CACertData.Names.IndexOf(certName);

            if (idx == -1) return null;

            return s_CACertData.Values[idx] as X509Certificate;
        }

        /// <summary>
        /// Updates a given personal certificate in the store.
        /// </summary>
        /// <param name="certName">User defined name of the certificate (case sensitive)</param>
        /// <param name="cert">Updated X509 personal certificate data</param>
        /// <returns>Returns false if the certificate does not exist, otherwise true.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool UpdatePersonalCertificate(string certName, X509Certificate cert)
        {
            return s_personalCertData.Update(certName, cert);
        }

        /// <summary>
        /// Updates a given CA certificate in the store.
        /// </summary>
        /// <param name="certName">User defined name of the certificate (case sensitive)</param>
        /// <param name="cert">Updated X509 CA certificate data</param>
        /// <returns>Returns false if the certificate does not exist, otherwise true.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool UpdateCACertificate(string certName, X509Certificate cert)
        {
            int idx = s_CACertData.Names.IndexOf(certName);

            if (-1 == idx) return false;

            s_CACertData.Values[idx] = cert;

            s_ewrCALookup.Target = s_CACertData;
            s_ewrCALookup.PushBackIntoRecoverList();

            if (OnCACertificateChange != null)
            {
                OnCACertificateChange(CertificateNotificationType.Updated, certName);
            }

            return true;
        }

        /// <summary>
        /// Removes the given personal certificate from the store
        /// </summary>
        /// <param name="certName"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool RemovePersonalCertificate(string certName)
        {
            bool ret = s_personalCertData.Remove(certName);

            s_ewrCertLookup.Target = s_personalCertData;
            s_ewrCertLookup.PushBackIntoRecoverList();

            return ret;
        }

        /// <summary>
        /// Removes a given CA certificate
        /// </summary>
        /// <param name="certName">Name of the CA certificate</param>
        /// <returns>true if the given certificate was removed, false otherwise</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool RemoveCACertificate(string certName)
        {
            int idx = s_CACertData.Names.IndexOf(certName);

            if (idx == -1) return false;

            s_CACertData.Names.RemoveAt(idx);
            s_CACertData.Values.RemoveAt(idx);

            s_ewrCALookup.Target = s_CACertData;
            s_ewrCALookup.PushBackIntoRecoverList();

            if (OnCACertificateChange != null)
            {
                OnCACertificateChange(CertificateNotificationType.Removed, certName);
            }

            return true;
        }

        /// <summary>
        /// The list of CA certificates in the CA store.
        /// </summary>
        public static X509Certificate[] CACertificates
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return s_CACertData.Values.ToArray(typeof(X509Certificate)) as X509Certificate[];
            }
        }
    }
}


