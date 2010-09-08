////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.SPOT.Cryptography
{
    [Serializable]
    [SerializationHints(Flags = SerializationFlags.DemandTrusted)]
    public abstract class Key
    {
        public abstract byte[] Encrypt(byte[] data, int offset, int count, byte[] IV);
        public abstract byte[] Decrypt(byte[] data, int offset, int count, byte[] IV);
        public abstract byte[] Signature
        {
            get;
        }
    }

    //--//
    [Serializable]
    [SerializationHints(Flags = SerializationFlags.DemandTrusted)]
    public class Key_TinyEncryptionAlgorithm : Key
    {
        public const int c_SizeOfKey = 16;
        public const int c_SizeOfSignature = 5;

        [SerializationHints(ArraySize = c_SizeOfKey, Flags = SerializationFlags.PointerNeverNull | SerializationFlags.FixedType)]
        private byte[] m_value;
        /******************************************************************************************************************/
        private bool m_doublyEncrypted;
        public Key_TinyEncryptionAlgorithm(byte[] val)
        {
            m_value = val;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern public override byte[] Encrypt(byte[] data, int offset, int count, byte[] IV);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern public override byte[] Decrypt(byte[] data, int offset, int count, byte[] IV);
        extern public override byte[] Signature
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        //--//
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern public string GetActivationString(ushort region, ushort model);
    }

    //--//
    [Serializable]
    [SerializationHints(Flags = SerializationFlags.DemandTrusted)]
    public class Key_RSA : Key
    {
        public const int c_SizeOfKey = 1024 / 8;
        public const int c_SizeOfSignature = c_SizeOfKey;

        [SerializationHints(ArraySize = c_SizeOfKey, Flags = SerializationFlags.PointerNeverNull | SerializationFlags.FixedType)]
        private byte[] m_modulus;
        [SerializationHints(ArraySize = c_SizeOfKey, Flags = SerializationFlags.PointerNeverNull | SerializationFlags.FixedType)]
        private byte[] m_exponent;
        public Key_RSA(byte[] modulus, byte[] exponent)
        {
            m_modulus = modulus;
            m_exponent = exponent;
        }

        //--//
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern public override byte[] Encrypt(byte[] data, int offset, int count, byte[] IV);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern public override byte[] Decrypt(byte[] data, int offset, int count, byte[] IV);
        public override byte[] Signature
        {
            get
            {
                return null;
            }
        }

        //--//
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern public bool VerifySignature(byte[] data, int dataOffset, int dataCount, byte[] sig, int sigOffset, int sigCount);
    }
}


