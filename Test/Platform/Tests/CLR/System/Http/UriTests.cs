////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Platform.Test;

namespace Microsoft.SPOT.Platform.Tests
{
    public class UriTests : IMFTestInterface
    {
        Uri uri;
        UriProperties props;

        [SetUp]
        public InitializeResult Initialize()
        {
            Log.Comment("Adding set up for the tests.");
            // Add your functionality here.   

            return InitializeResult.ReadyToGo;
        }

        [TearDown]
        public void CleanUp()
        {
            Log.Comment("Cleaning up after the tests.");

            // TODO: Add your clean up steps here.
        }

        #region helper functions

        private bool ValidUri(Uri uri, UriProperties props)
        {
            bool result = true;

            // AbsolutePath
            if (props.Path != null && uri.AbsolutePath != props.Path)
            {
                Log.Exception("Expected AbsolutePath: " + props.Path + ", but got: " + uri.AbsolutePath);
                result = false;
            }
            // AbsoluteUri
            if (uri.AbsoluteUri != props.AbsoluteUri)
            {
                Log.Exception("Expected AbsoluteUri: " + props.AbsoluteUri + ", but got: " + uri.AbsoluteUri);
                result = false;
            }
            // HostNameType
            if (uri.HostNameType != props.Type)
            {
                Log.Exception("Expected HostNameType: " + props.Type + ", but got: " + uri.HostNameType);
                result = false;
            }

            switch (uri.Scheme.ToLower())
            {
                case "http": 
                case "https":
                    if (uri.Port != props.Port)
                    {
                        Log.Exception("Expected Port: " + props.Port + ", but got: " + uri.Port);
                        result = false;
                    }
                    // Host
                    if (uri.Host != props.Host)
                    {
                        Log.Exception("Expected Host: " + props.Host + ", but got: " + uri.Host);
                        result = false;
                    }
                    break;
                default:
                    // no validation
                    break;
            }
            // Scheme
            if (uri.Scheme != props.Scheme)
            {
                Log.Exception("Expected Scheme: " + props.Scheme + ", but got: " + uri.Scheme);
                result = false;
            }

            return result;
        }

        #endregion Helper functions

        #region Test Cases
        [TestMethod]
        public MFTestResults InvalidConstructorTests()
        {
            MFTestResults result = MFTestResults.Pass;
            try
            {
                Log.Comment("null string constructor");
                try { uri = new Uri(null); }
                catch (ArgumentNullException ex) 
                {
                    if (!HttpTests.ValidateException(ex, typeof(ArgumentNullException)))
                        result = MFTestResults.Fail;
                }

                Log.Comment("no uri string");
                try { uri = new Uri("foo"); }
                catch (ArgumentException ex)
                {
                    if (!HttpTests.ValidateException(ex, typeof(ArgumentException)))
                        result = MFTestResults.Fail;
                }

                Log.Comment("uri, no address");
                try { uri = new Uri("http:"); }
                catch (ArgumentException ex)
                {
                    if (!HttpTests.ValidateException(ex, typeof(ArgumentException)))
                        result = MFTestResults.Fail;
                }
                
                Log.Comment("uri, starts with non-alpha");
                try { uri = new Uri("1ttp://foo.com"); }
                catch (ArgumentException ex)
                {
                    if (!HttpTests.ValidateException(ex, typeof(ArgumentException)))
                        result = MFTestResults.Fail;
                }

                Log.Comment("uri, includes numeric");
                try { uri = new Uri("h1tp://foo.com"); }
                catch (ArgumentException ex)
                {
                    if (!HttpTests.ValidateException(ex, typeof(ArgumentException)))
                        result = MFTestResults.Fail;
                }

                Log.Comment("uri, includes non-alpha");
                try { uri = new Uri("h@tp://foo.com"); }
                catch (ArgumentException ex)
                {
                    if (!HttpTests.ValidateException(ex, typeof(ArgumentException)))
                        result = MFTestResults.Fail;
                }

                Log.Comment("No ABSPath port URI");
                try { uri = new Uri(HttpTests.MSUrl + ":80"); }
                catch (ArgumentException ex) 
                {
                    if (!HttpTests.ValidateException(ex, typeof(ArgumentException)))
                        result = MFTestResults.Fail;
                }

                Log.Comment("Empty string constructor");
                try { uri = new Uri(""); }
                catch (ArgumentException ex)
                {
                    if (!HttpTests.ValidateException(ex, typeof(ArgumentNullException)))
                        result = MFTestResults.Fail;
                }

            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected Exception", ex);
                result = MFTestResults.Fail;
            }

            return result;
        }

        public MFTestResults ValidUri()
        {
            MFTestResults result = MFTestResults.Pass;
            try
            {
                Log.Comment("Microsoft URL");
                props = new UriProperties("http", "www.microsoft.com");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;


                Log.Comment("Alternate http port URL");
                props = new UriProperties("http", "www.microsoft.com")
                {
                    Port = 1080,
                    Path = "/" //Need to remove later.  This seems like a bug to require it.
                };
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("URL with content");
                props = new UriProperties("http", "www.microsoft.com")
                {
                    Path = "/en/us/default.aspx"
                };
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected Exception", ex);
                result = MFTestResults.Fail;
            }

            return result;
        }

        public MFTestResults ValidURN()
        {
            MFTestResults result = MFTestResults.Pass;

            try
            {
                Log.Comment("isbn");
                props = new UriProperties("urn", "isbn:0451450523");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("isan");
                props = new UriProperties("urn", "isan:0000-0000-9E59-0000-O-0000-0000-2");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("issn");
                props = new UriProperties("urn", "issn:0167-6423");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("ietf");
                props = new UriProperties("urn", "ietf:rfc:2648");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("mpeg");
                props = new UriProperties("urn", "mpeg:mpeg7:schema:2001");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("oid");
                props = new UriProperties("urn", "oid:2.216.840");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("urn:uuid");
                props = new UriProperties("urn", "uuid:6e8bc430-9c3a-11d9-9669-0800200c9a66");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("uuid");
                props = new UriProperties("uuid", "6e8bc430-9c3a-11d9-9669-0800200c9a66");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("uci");
                props = new UriProperties("urn", "uci:I001+SBSi-B10000083052");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected Exception", ex);
                result = MFTestResults.Fail;
            }

            return result;
        }


        public MFTestResults AdditionalValidUri()
        {
            MFTestResults result = MFTestResults.Pass;

            try
            {
                Log.Comment("iris.beep");
                props = new UriProperties("iris.beep", "bop") { Type = UriHostNameType.Unknown };
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("Microsoft Secure URL");
                props = new UriProperties("https", "www.microsoft.com");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("Alternate https port URL");
                props = new UriProperties("https", "www.microsoft.com")
                {
                    Port = 1443
                };
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("H323 uri");
                props = new UriProperties("h323", "user@host:54");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("FTP URI");
                props = new UriProperties("ftp","//ftp.microsoft.com/file.txt");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("Unix style file");
                props = new UriProperties("file", @"///etc/hosts");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("Windows share style file");
                props = new UriProperties("file", "///\\\\server\\folder\\file.txt");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("Windows drive style file");
                props = new UriProperties("file" , "///c:\\rbllog");
                uri = new Uri(props.OriginalUri);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected Exception - these cases currently all fail", ex);
                result = MFTestResults.Fail;
            }

            return result;
        }

        public MFTestResults RelativeURI()
        {
            MFTestResults result = MFTestResults.Pass;

            try
            {
                Log.Comment("relative url");
                uri = new Uri("/doc/text.html", UriKind.Relative);

                Log.Comment("absolute url");
                props = new UriProperties("https", "www.microsoft.com")
                {
                    Path = "/doc/text.html",
                    Port = 1443
                };
                uri = new Uri(props.OriginalUri, UriKind.Absolute);
                if (!ValidUri(uri, props))
                    result = MFTestResults.Fail;

                Log.Comment("RelativeOrAbsolute");
                try { uri = new Uri("/doc/text.html", UriKind.RelativeOrAbsolute); }
                catch (ArgumentException ex)
                {
                    if (!HttpTests.ValidateException(ex, typeof(ArgumentException)))
                        result = MFTestResults.Fail;
                }
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected Exception", ex);
                result = MFTestResults.Fail;
            }

            return result;
        }
        
        #endregion Test Cases
    }

    #region helper class
    public class UriProperties
    {
        private string _scheme;
        private string _host;
        private bool _portSet = false;
        private int _port;
        private UriHostNameType _type = UriHostNameType.Unknown;

        public UriProperties(string Scheme, string Host)
        {
            // Minimal required properties
            _scheme = Scheme;
            _host = Host;
        }

        public string Scheme 
        {
            get { return _scheme; }
        }

        public string Host 
        {
            get { return _host; }
        }

        public int Port 
        { 
            get
            {
                return _port;
            } 
            set
            {
                _portSet = true;
                _port = value;
            } 
        }

        public bool PortSet
        {
            get
            {
                return _portSet;
            }
        }

        public string Path { get; set; }

        public UriHostNameType Type 
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        public string AbsoluteUri
        {
            get
            {
                string uri = OriginalUri;
                // for http[s] add trailing / if no path
                if (Path == null && _scheme.ToLower().IndexOf("http") == 0 && uri[uri.Length - 1] != '/')
                {
                    uri += "/";
                }
                return uri;
            }
        }

        public string OriginalUri
        {
            get
            {
                string uri = _scheme;
                int defaultPort = 0;
                switch (_scheme.ToLower())
                {
                    case "http":
                        _type = UriHostNameType.Dns;
                        defaultPort = 80;
                        uri += "://" + _host;
                        break;
                    case "https":
                        _type = UriHostNameType.Dns;
                        defaultPort = 443;
                        uri += "://" + _host;
                        break;

                    default:
                        // No hosts, so move _host to Path
                        if (_host != "")
                        {
                            Path = _host;
                            _host = "";
                        } 
                        uri += ":" + _host;
                        break;
                }

                if (_portSet)
                    uri += ":" + Port;
                else
                    _port = defaultPort;

                if (Path != null)
                    uri += Path;

                return uri;
            }
        }

    }
    #endregion helper class
}
