////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace System
{
    /// <summary>
    /// Defines the kinds of <see cref="System.Uri"/>s for the
    /// <see cref="System.Uri.IsWellFormedUriString"/> method and several
    /// <see cref="System.Uri"/> methods.
    /// </summary>
    public enum UriKind
    {
        /// <summary>
        /// The kind of the Uri is indeterminate.
        /// </summary>
        RelativeOrAbsolute = 0,

        /// <summary>
        /// The Uri is an absolute Uri.
        /// </summary>
        Absolute = 1,

        /// <summary>
        /// The Uri is a relative Uri.
        /// </summary>
        Relative = 2,
    }

    /// <summary>
    /// Defines host name types for the http and https protocols.
    /// method.
    /// </summary>
    public enum UriHostNameType
    {
        /// <summary>
        /// The type of the host name is not supplied.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The host is set, but the type cannot be determined.
        /// </summary>
        Basic = 1,

        /// <summary>
        /// The host name is a domain name system (DNS) style host name.
        /// </summary>
        Dns = 2,

        /// <summary>
        /// The host name is an Internet Protocol (IP) version 4 host address.
        /// </summary>
        IPv4 = 3,

        /// <summary>
        /// The host name is an Internet Protocol (IP) version 6 host address.
        /// </summary>
        IPv6 = 4,
    }

    /// <summary>
    /// Provides an object representation of a uniform resource identifier (URI)
    /// and easy access to the parts of the URI.
    /// </summary>
    public class Uri
    {
        /// <summary>
        /// Defines flags kept in m_Flags variable.
        /// </summary>
        protected enum Flags
        {
            /// <summary>
            /// Flag value for loopback host
            /// </summary>
            LoopbackHost = 0x00400000
        }

        /// <summary>
        /// Default port for http protocol - 80
        /// </summary>
        public const int HttpDefaultPort = 80;

        /// <summary>
        /// Default port for https protocol - 443
        /// </summary>
        public const int HttpsDefaultPort = 443;

        /// <summary>
        /// Constant to indicate that port for this protocol is unknown
        /// </summary>
        protected const int UnknownPort = -1;

        /// <summary>
        /// Type of the host.
        /// </summary>
        protected UriHostNameType m_hostNameType;

        /// <summary>
        /// Member variable that keeps port used by this uri.
        /// </summary>
        protected int m_port = UnknownPort;

        /// <summary>
        /// Member variable that keeps internal flags/
        /// </summary>
        protected int m_Flags = 0;

        /// <summary>
        /// Member varialbe that keeps absolute path.
        /// </summary>
        protected string m_AbsolutePath = null;

        /// <summary>
        /// Member varialbe that keeps original string passed to Uri constructor.
        /// </summary>
        protected string m_OriginalUriString = null;

        /// <summary>
        /// Member varialbe that keeps scheme of Uri.
        /// </summary>
        protected string m_scheme = null;

        /// <summary>
        /// Member varialbe that keeps host name ( http and https ).
        /// </summary>
        protected string m_host = "";

        /// <summary>
        /// Member varialbe that keeps boolean if Uri is absolute.
        /// </summary>
        protected bool m_isAbsoluteUri = false;

        /// <summary>
        /// Member varialbe that tells if path is UNC ( Universal Naming Convention )
        /// In this class it is always false, but can be changed in derived classes.
        /// </summary>
        protected bool m_isUnc = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="System.Uri"/> class
        /// with the specified URI.
        /// </summary>
        /// <remarks>
        /// This constructor parses the URI string, therefore it can be used to
        /// validate a URI.
        /// </remarks>
        /// <param name="uriString">A URI.</param>
        /// <exception cref="System.Exception">
        /// The <paramref name="uriString"/> is null.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// <p>The <paramref name="uriString"/> is empty.</p>
        /// <p>-or-</p><p>The scheme specified in <paramref name="uriString"/>
        /// is not correctly formed.  </p>
        /// <p>-or-</p><p><paramref name="uriString"/> contains too many
        /// slashes.</p>
        /// <p>-or-</p><p>The password specified in <paramref name="uriString"/>
        /// is not valid.</p>
        /// <p>-or-</p><p>The host name specified in
        /// <paramref name="uriString"/> is not valid.</p>
        /// <p>-or-</p><p>The file name specified in
        /// <paramref name="uriString"/> is not valid.</p>
        /// <p>-or-</p><p>The user name specified in
        /// <paramref name="uriString"/> is not valid.</p>
        /// <p>-or-</p><p>The host or authority name specified in
        /// <paramref name="uriString"/> cannot be terminated by backslashes.
        /// </p>
        /// <p>-or-</p><p>The port number specified in
        /// <paramref name="uriString"/> is not valid or cannot be parsed.</p>
        /// <p>-or-</p><p>The length of <paramref name="uriString"/> exceeds
        /// 65534 characters.</p>
        /// <p>-or-</p><p>The length of the scheme specified in
        /// <paramref name="uriString"/> exceeds 1023 characters.</p>
        /// <p>-or-</p><p>There is an invalid character sequence in
        /// <paramref name="uriString"/>.</p>
        /// <p>-or-</p><p>The MS-DOS path specified in
        /// <paramref name="uriString"/> must start with c:\\.</p>
        /// </exception>
        public Uri(string uriString)
        {
            ConstructAbsoluteUri(ref uriString);
        }

        /// <summary>
        /// Constructs an absolute Uri from a URI string.
        /// </summary>
        /// <param name="uriString">A URI.</param>
        /// <remarks>
        /// See <see cref="System.Uri(string)"/>.
        /// </remarks>
        protected void ConstructAbsoluteUri(ref string uriString)
        {
            // ParseUriString provides full validation including testing for
            // null.
            ParseUriString(ref uriString);
            m_OriginalUriString = uriString;
        }

        /// <summary>
        /// Constructs Uri from string and enumeration that tell what is the type of Uri.
        /// </summary>
        /// <param name="uriString">String to construct Uri from</param>
        /// <param name="kind">Type of Uri to construct</param>
        public Uri(string uriString, UriKind kind)
        {
            // ParseUriString provides full validation including testing for null.
            switch (kind)
            {
                case UriKind.Absolute: { ConstructAbsoluteUri(ref uriString); break; }
                // Do not support unknown type of Uri. User should decide what he wants.
                case UriKind.RelativeOrAbsolute: { throw new ArgumentException(); }
                // Relative Uri. Store in original string.
                case UriKind.Relative:
                    {
                        // Validates the relative Uri.
                        ValidateUriPart(uriString, 0);
                        m_OriginalUriString = uriString;
                        break;
                    }
            }

            m_OriginalUriString = uriString;
        }

        /// <summary>
        /// Validates that part of Uri after sheme is valid for unknown Uri scheme
        /// </summary>
        /// <param name="uriString">Uri string </param>
        /// <param name="startIndex">Index in the string where Uri part ( after scheme ) starts</param>
        protected void ValidateUriPart(string uriString, int startIndex)
        {
            // Check for valid alpha numeric characters
            int pathLength = uriString.Length - startIndex;

            // This is unknown scheme. We do validate following rules:
            // 1. All character values are less than 128. For characters it means they are more than zero.
            // 2. All charaters are >= 32. Lower values are control characters.
            // 3. If there is %, then there should be 2 hex digits which are 0-10 and A-F or a-f.

            for (int i = startIndex; i < pathLength; ++i)
            {
                //if (!(IsAlphaNumeric(uriString[i]) || uriString[i] == '+' || uriString[i] == '-' || uriString[i] == '.'))
                // If character is upper ( in signed more than 127, then value is negative ).
                char value = uriString[i];
                if (value < 32)
                {
                    throw new ArgumentException("Invalid char: " + value);
                }

                // If it is percent, then there should be 2 hex digits after.
                if (value == '%')
                {
                    if (pathLength - i < 3)
                    {
                        throw new ArgumentException("No data after %");
                    }

                    // There are at least 2 characters. Check their values
                    for (int j = 1; j < 3; j++)
                    {
                        char nextVal = uriString[i + j];
                        if (!((nextVal >= '0' && nextVal <= '9') ||
                                (nextVal >= 'A' && nextVal <= 'F') ||
                                (nextVal >= 'a' && nextVal <= 'f')
                              )
                           )
                        {
                            throw new ArgumentException("Invalid char after %: " + value);
                        }
                    }

                    // Moves i by 2 up to bypass verified characters.
                    i += 2;
                }
            }
        }

        /// <summary>
        /// Internal method parses a URI string into Uri variables
        /// </summary>
        /// <param name="uriString">A Uri.</param>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="uriString"/> is null.
        /// </exception>
        /// <exception cref="System.Exception">
        /// See constructor description.
        /// </exception>
        protected void ParseUriString(ref string uriString)
        {
            int startIndex = 0;
            int endIndex = 0;

            // Check for null or empty string.
            if (uriString == null || uriString.Length == 0)
                throw new ArgumentNullException();

            // Check for presence of ':'. Colon always should be present in URI.
            if (uriString.IndexOf(':') == -1)
            {
                throw new ArgumentException();
            }

            string uriStringLower = uriString.ToLower();

            // If this is a urn parse and return
            if (uriStringLower.IndexOf("urn:", startIndex) == 0)
            {
                ValidateUrn(uriString);
                return;
            }

            // If the uri is a relative path parse and return
            if (uriString[0] == '/')
            {
                ValidateRelativePath(uriString);
                return;
            }

            // Validate Scheme
            endIndex = uriString.IndexOf(':');
            m_scheme = uriString.Substring(0, endIndex); ;
            if (!IsAlpha(m_scheme[0]))
                throw new ArgumentException();
            for (int i = 1; i < m_scheme.Length; ++i)
                if (!(IsAlphaNumeric(m_scheme[i]) || m_scheme[i] == '+' || m_scheme[i] == '-' || m_scheme[i] == '.'))
                    throw new ArgumentException();

            // Get past the colon
            startIndex = endIndex + 1;

            // If this is an http uri parse the host. IP host should start with //
            string schemeLower = m_scheme.ToLower();
            if (schemeLower == "http" || schemeLower == "https")
            {
                if (uriString.Substring(startIndex).IndexOf("//") == 0)
                {
                    m_host = "";

                    // Set the host start index an determine if the host includes a port
                    startIndex = startIndex + 2;
                    if (uriString[startIndex] == '[')
                    {
                        if(-1 == (endIndex = uriString.IndexOf(']', startIndex)))
                        {
                            throw new ArgumentException();
                        }

                        endIndex++;

                        m_host = uriString.Substring(startIndex, endIndex - startIndex);

                        startIndex = endIndex;
                    }
                    else if ((endIndex = uriString.IndexOf(':', startIndex)) != -1)
                    {
                        // If port is listed, parse to port sentinel
                        m_host = uriString.Substring(startIndex, endIndex - startIndex);
                        startIndex = endIndex;
                    }

                    if(m_host != "")
                    {
                        // Look for the path sentinel
                        endIndex = uriString.IndexOf('/', startIndex);

                        // If there was no '/' at the end, we add it. For HTTP it means root page if address is http://www.microsoft.com
                        if (endIndex == -1)
                        {
                            uriString += '/';
                            endIndex = uriString.IndexOf('/', startIndex);
                        }

                        ++startIndex;
                        int portLength = endIndex - startIndex;
                        m_port = Convert.ToInt32(uriString.Substring(startIndex, portLength));
                        startIndex += portLength;
                    }
                    else
                    {
                        if ((endIndex = uriString.IndexOf('/', startIndex)) == -1)
                        {
                            // If "/" was not found, means it is just host address like http://itgproxy.dns.microsoft.com
                            // In this case we add add "/" and repeat the same operation as in prevo
                            uriString += '/';
                            endIndex = uriString.IndexOf('/', startIndex);
                        }

                        m_host = uriString.Substring(startIndex, endIndex - startIndex);
                        startIndex = startIndex + m_host.Length;
                        // The control flow comes here only if schemeLower is http or https.
                        // Thus we have only 2 cases.
                        switch (schemeLower)
                        {
                            case "http": m_port = HttpDefaultPort; break;
                            case "https": m_port = HttpsDefaultPort; break;
                        }
                    }

                    m_isAbsoluteUri = true;
                }
                else
                    throw new ArgumentException();

                if (m_host[0] == '[')
                {
                    m_hostNameType = UriHostNameType.IPv6;
                }
                else if (IsIPv4(m_host))
                {
                    m_hostNameType = UriHostNameType.IPv4;
                }
                else
                {
                    m_hostNameType = UriHostNameType.Dns;
                }
            }

            // The last test is for a scheme and valid path
            else
            {
                if (m_scheme == null)
                { throw new ArgumentException(); }

                // Check that remaining part of Uri is valid
                ValidateUriPart(uriString, startIndex);
                m_hostNameType = UriHostNameType.Unknown;
                m_isAbsoluteUri = true;
            }

            // Set Uri properties
            m_AbsolutePath = uriString.Substring(startIndex, uriString.Length - startIndex);
            if (m_host != null)
            {
                string hostLower = m_host.ToLower();
                if (hostLower == "localhost" || hostLower == "loopback")
                {
                    m_Flags |= m_Flags | (int)Flags.LoopbackHost;
                }
            }

            m_isUnc = false;
            return;
        }

        /// <summary>
        /// Returns if host name is IP adress 4 bytes. Like 192.1.1.1
        /// </summary>
        /// <param name="host">string with host name</param>
        /// <returns>True if name is string with IPv4 address</returns>
        protected bool IsIPv4(String host)
        {
            int dots = 0;
            int number = 0;
            bool haveNumber = false;
            int length = host.Length;

            for (int i = 0; i < length; i++)
            {
                char ch = host[i];

                if (ch <= '9' && ch >= '0')
                {
                    haveNumber = true;
                    number = number * 10 + (host[i] - '0');
                    if (number > 255)
                    {
                        return false;
                    }
                }
                else if (ch == '.')
                {
                    if (!haveNumber)
                    {
                        return false;
                    }

                    ++dots;
                    haveNumber = false;
                    number = 0;
                }
                else
                {
                    return false;
                }
            }

            return (dots == 3) && haveNumber;
        }

        /// <summary>
        /// Parses urn string into Uri variables.
        /// Parsing is restricted to basic urn:NamespaceID, urn:uuid formats only.
        /// </summary>
        /// <param name="uri">A Uri.</param>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="uri"/> is null.
        /// </exception>
        /// <exception cref="System.Exception">
        /// See the constructor description.
        /// </exception>
        protected void ValidateUrn(string uri)
        {
            bool invalidUrn = false;

            // If this is a urn:uuid validate the uuid
            if (uri.ToLower().IndexOf("urn:uuid:", 0) == 0)
            {
                char[] tempUUID = uri.Substring(9).ToLower().ToCharArray();
                int length = tempUUID.Length;
                int uuidSegmentCount = 0;
                int[] delimiterIndexes = { 8, 13, 18, 23 };
                for (int i = 0; i < length; ++i)
                {
                    // Make sure these are valid hex numbers numbers
                    if (!IsHex(tempUUID[i]) && tempUUID[i] != '-')
                    {
                        invalidUrn = true;
                        break;
                    }
                    else
                    {
                        // Check each segment length
                        if (tempUUID[i] == '-')
                        {
                            if (uuidSegmentCount > 3)
                            {
                                invalidUrn = true;
                                break;
                            }

                            if (i != delimiterIndexes[uuidSegmentCount])
                            {
                                invalidUrn = true;
                                break;
                            }

                            ++uuidSegmentCount;
                        }
                    }
                }

                m_AbsolutePath = uri.Substring(4);
            }

            // Else validate against RFC2141
            else
            {
                string lowerUrn = uri.Substring(4).ToLower();
                char[] tempUrn = lowerUrn.ToCharArray();

                // Validate the NamespaceID (NID)
                int index = lowerUrn.IndexOf(':');
                if (index == -1)
                    throw new ArgumentException();
                int i = 0;
                for (i = 0; i < index; ++i)
                {
                    // Make sure these are valid hex numbers numbers
                    if (!IsAlphaNumeric(tempUrn[i]) && tempUrn[i] != '-')
                    {
                        invalidUrn = true;
                        break;
                    }
                }

                // Validate the Namespace String
                tempUrn = lowerUrn.Substring(index + 1).ToCharArray();
                int urnLength = tempUrn.Length;
                if (!invalidUrn && urnLength != 0)
                {
                    string otherChars = "()+,-.:=@;$_!*'";
                    for (i = 0; i < urnLength; ++i)
                    {
                        if (!IsAlphaNumeric(tempUrn[i]) && !IsHex(tempUrn[i]) && tempUrn[i] != '%' && otherChars.IndexOf(tempUrn[i]) == -1)
                        {
                            invalidUrn = true;
                            break;
                        }
                    }

                    m_AbsolutePath = uri.Substring(4);
                }
            }

            if (invalidUrn)
                throw new ArgumentNullException();

            // Set Uri properties
            m_host = "";
            m_isAbsoluteUri = true;
            m_isUnc = false;
            m_hostNameType = UriHostNameType.Unknown;
            m_port = UnknownPort;
            m_scheme = "urn";

            return;
        }

        /// <summary>
        /// Parses relative Uri into variables.
        /// </summary>
        /// <param name="uri">A Uri.</param>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="uri"/> is null.
        /// </exception>
        /// <exception cref="System.Exception">
        /// See constructor description.
        /// </exception>
        protected void ValidateRelativePath(string uri)
        {
            // Check for null
            if (uri == null || uri.Length == 0)
                throw new ArgumentNullException();
            // Check for "//"
            if (uri[1] == '/')
                throw new ArgumentException();

            // Check for alphnumeric and special characters
            for (int i = 1; i < uri.Length; ++i)
                if (!IsAlphaNumeric(uri[i]) && ("()+,-.:=@;$_!*'").IndexOf(uri[i]) == -1)
                    throw new ArgumentException();

            m_AbsolutePath = uri.Substring(1);
            m_host = "";
            m_isAbsoluteUri = false;
            m_isUnc = false;
            m_hostNameType = UriHostNameType.Unknown;
            m_port = UnknownPort;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();   
        }

        public override bool Equals(object o)
        {
            return this == (Uri)o;
        }

        public static bool operator ==(Uri lhs, Uri rhs)
        {
            object l = lhs, r = rhs;

            if (l == null)
            {
                return (r == null);
            }
            else if(r == null)
            {
                return false;
            }
            else
            {
                if (lhs.m_isAbsoluteUri && rhs.m_isAbsoluteUri)
                {
                    return lhs.m_AbsolutePath.ToLower() == rhs.m_AbsolutePath.ToLower();
                }
                else
                {
                    return lhs.m_OriginalUriString.ToLower() == rhs.m_OriginalUriString.ToLower();
                }
            }
        }

        public static bool operator !=(Uri lhs, Uri rhs)
        {
            object l = lhs, r = rhs;

            if (l == null)
            {
                return (r != null);
            }
            else if(r == null)
            {
                return true;
            }
            else
            {
                if (lhs.m_isAbsoluteUri && rhs.m_isAbsoluteUri)
                {
                    return lhs.m_AbsolutePath.ToLower() != rhs.m_AbsolutePath.ToLower();
                }
                else
                {
                    return lhs.m_OriginalUriString.ToLower() != rhs.m_OriginalUriString.ToLower();
                }
            }
        }

        /// <summary>
        /// Checks to see if the character value is an alpha character.
        /// </summary>
        /// <param name="testChar">The character to evaluate.</param>
        /// <returns><itemref>true</itemref> if the character is Alpha;
        /// otherwise, <itemref>false</itemref>.</returns>
        protected bool IsAlpha(char testChar)
        {
            return (testChar >= 'A' && testChar <= 'Z') || (testChar >= 'a' && testChar <= 'z');
        }

        /// <summary>
        /// Checks to see if the character value is an alpha or numeric.
        /// </summary>
        /// <param name="testChar">The character to evaluate.</param>
        /// <returns><itemref>true</itemref> if the character is Alpha or
        /// numeric; otherwise, <itemref>false</itemref>.</returns>
        protected bool IsAlphaNumeric(char testChar)
        {
            return (testChar >= 'A' && testChar <= 'Z') || (testChar >= 'a' && testChar <= 'z') || (testChar >= '0' && testChar <= '9');
        }

        /// <summary>
        /// Checks to see if the character value is Hex.
        /// </summary>
        /// <param name="testChar">The character to evaluate.</param>
        /// <returns><itemref>true</itemref> if the character is a valid Hex
        /// character; otherwise, <itemref>false</itemref>.</returns>
        protected bool IsHex(char testChar)
        {
            return (testChar >= 'A' && testChar <= 'F') || (testChar >= 'a' && testChar <= 'f') || (testChar >= '0' && testChar <= '9');
        }

        /// <summary>
        /// Gets the type of the host name specified in the URI.
        /// </summary>
        /// <value>A member of the <see cref="System.UriHostNameType"/>
        /// enumeration.</value>
        public UriHostNameType HostNameType { get { return m_hostNameType; } }

        /// <summary>
        /// Gets the port number of this URI.
        /// </summary>
        /// <value>An <itemref>Int32</itemref> value containing the port number
        /// for this URI.</value>
        /// <exception cref="System.InvalidOperationException">
        /// This instance represents a relative URI, and this property is valid
        /// only for absolute URIs.
        /// </exception>
        public int Port
        {
            get
            {
                if (m_isAbsoluteUri == false)
                    throw new InvalidOperationException();
                return m_port;
            }
        }

        /// <summary>
        /// Gets whether the <see cref="System.Uri"/> instance is absolute.
        /// </summary>
        /// <value><itemref>true</itemref> if the <itemref>Uri</itemref>
        /// instance is absolute; otherwise, <itemref>false</itemref>.</value>
        public bool IsAbsoluteUri { get { return m_isAbsoluteUri; } }

        /// <summary>
        /// Gets whether the specified <see cref="System.Uri"/> is a universal
        /// naming convention (UNC) path.
        /// </summary>
        /// <value><itemref>true</itemref> if the <see cref="System.Uri"/> is a
        /// UNC path; otherwise, <itemref>false</itemref>.</value>
        /// <exception cref="System.InvalidOperationException">
        /// This instance represents a relative URI, and this property is valid
        /// only for absolute URIs.
        /// </exception>
        public bool IsUnc
        {
            get
            {
                if (m_isAbsoluteUri == false)
                    throw new InvalidOperationException();
                return m_isUnc;
            }
        }

        /// <summary>
        /// Gets a local operating-system representation of a file name.
        /// </summary>
        /// <value>A <itemref>String</itemref> containing the local
        /// operating-system representation of a file name.</value>
        /// <exception cref="System.InvalidOperationException">
        /// This instance represents a relative URI, and this property is valid
        /// only for absolute URIs.
        /// </exception>
        public string AbsolutePath
        {
            get
            {
                if (m_isAbsoluteUri == false)
                    throw new InvalidOperationException();
                return m_AbsolutePath;
            }
        }

        /// <summary>
        /// Gets the original URI string that was passed to the Uri constructor.
        /// </summary>
        public string OriginalString
        {
            get
            {
                // The original string was saved in m_OriginalUriString.
                return m_OriginalUriString;
            }
        }

        /// <summary>
        /// Gets a string containing the absolute uri or entire uri of this instance.
        /// </summary>
        /// <value>A <itemref>String</itemref> containing the entire URI.
        /// </value>
        public string AbsoluteUri
        {
            get
            {
                if (m_isAbsoluteUri == false)
                    throw new InvalidOperationException();
                return m_OriginalUriString;
            }
        }

        /// <summary>
        /// Gets the scheme name for this URI.
        /// </summary>
        /// <value>A <itemref>String</itemref> containing the scheme for this
        /// URI, converted to lowercase.</value>
        /// <exception cref="System.InvalidOperationException">
        /// This instance represents a relative URI, and this property is valid only
        /// for absolute URIs.
        /// </exception>
        public string Scheme
        {
            get
            {
                if (m_isAbsoluteUri == false)
                    throw new InvalidOperationException();
                return m_scheme;
            }
        }

        /// <summary>
        /// Gets the host component of this instance.
        /// </summary>
        /// <value>A <itemref>String</itemref> containing the host name.  This
        /// is usually the DNS host name or IP address of the server.</value>
        public string Host { get { return m_host; } }

        /// <summary>
        /// Gets whether the specified <see cref="System.Uri"/> refers to the
        /// local host.
        /// </summary>
        /// <value><itemref>true</itemref> if the host specified in the Uri is
        /// the local computer; otherwise, <itemref>false</itemref>.</value>
        public bool IsLoopback
        {
            get
            {
                return (m_Flags & (int)Flags.LoopbackHost) != 0;
            }
        }

        /// <summary>
        /// Indicates whether the string is well-formed by attempting to
        /// construct a URI with the string.
        /// </summary>
        /// <param name="uriString">A URI.</param>
        /// <param name="uriKind">The type of the URI in
        /// <paramref name="uriString"/>.</param>
        /// <returns>
        /// <itemref>true</itemref> if the string was well-formed in accordance
        /// with RFC 2396 and RFC 2732; otherwise <itemref>false</itemref>.
        /// </returns>
        public static bool IsWellFormedUriString(string uriString, UriKind uriKind)
        {
            try
            {   // If absolute Uri was passed - create Uri object.
                switch (uriKind)
                {
                    case UriKind.Absolute:
                        {
                            Uri testUri = new Uri(uriString);

                            if (testUri.IsAbsoluteUri)
                            {
                                return true;
                            }

                            return false;
                        }

                    case UriKind.Relative:
                        {
                            Uri testUri = new Uri(uriString, UriKind.Relative);
                            if (!testUri.IsAbsoluteUri)
                            {
                                return true;
                            }

                            return false;
                        }
                    default: return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}


