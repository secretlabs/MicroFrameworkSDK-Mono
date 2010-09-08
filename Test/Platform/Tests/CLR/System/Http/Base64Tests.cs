////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Platform.Test;
using System.Net;
using System.IO;
using System.Text;
using System.Xml;


namespace Microsoft.SPOT.Platform.Tests
{
    public class Base64Tests : IMFTestInterface
    {
        [SetUp]
        public InitializeResult Initialize()
        {
            Log.Comment("Testing base64 encoding");
            return InitializeResult.ReadyToGo;
        }

        [TearDown]
        public void CleanUp()
        {
            Log.Comment("No cleanup required after tests of base64 encoding.");

        }

        [TestMethod]
        public MFTestResults TestBase64Encoding()
        {
            try
            {
                string strOriginal = "Over the past few years, you've asked us to make some changes to Windows. We listened closely. Now it's time to share an early look at how we've used your feedback. Windows 7 is faster, more reliable, and makes it easier to do what you want. Both the everyday things and the killer \"is that really possible?\" things. Dig into this site to see what's coming...";
                byte [] dataToEncode = Encoding.UTF8.GetBytes(strOriginal);
                string strEncoded = System.ConvertBase64.ToBase64String(dataToEncode);

                string strEthalon = "T3ZlciB0aGUgcGFzdCBmZXcgeWVhcnMsIHlvdSd2ZSBhc2tlZCB1cyB0byBtYWtlIHNvbWUgY2hhbmdlcyB0byBXaW5kb3dzLiBXZSBsaXN0ZW5lZCBjbG9zZWx5LiBOb3cgaXQncyB0aW1lIHRvIHNoYXJlIGFuIGVhcmx5IGxvb2sgYXQgaG93IHdlJ3ZlIHVzZWQgeW91ciBmZWVkYmFjay4gV2luZG93cyA3IGlzIGZhc3RlciwgbW9yZSByZWxpYWJsZSwgYW5kIG1ha2VzIGl0IGVhc2llciB0byBkbyB3aGF0IHlvdSB3YW50LiBCb3RoIHRoZSBldmVyeWRheSB0aGluZ3MgYW5kIHRoZSBraWxsZXIgImlzIHRoYXQgcmVhbGx5IHBvc3NpYmxlPyIgdGhpbmdzLiBEaWcgaW50byB0aGlzIHNpdGUgdG8gc2VlIHdoYXQncyBjb21pbmcuLi4=";
                if (strEncoded != strEthalon)
                {
                    Log.Comment("Failed in encoding of the string." );
                    Log.Comment("Expected result: " + strEthalon);
                    Log.Comment("Actual result: " + strEncoded);
                    return MFTestResults.Fail;
                }

                byte[] dataDecoded = System.ConvertBase64.FromBase64String(strEncoded);
                string strDecoded = new string( Encoding.UTF8.GetChars(dataDecoded) );
                if (strOriginal != strDecoded)
                {
                    Log.Comment("Failed in decoding of the string.");
                    Log.Comment("Expected result: " + strOriginal);
                    Log.Comment("Actual result: " + strDecoded);
                    return MFTestResults.Fail;
                }

            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected Exception", ex);
                return MFTestResults.Fail;
            }
            Log.Comment("Base64 decoding/encoding tests succeded");
            return MFTestResults.Pass;
        }


    }

}