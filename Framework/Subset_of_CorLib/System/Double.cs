////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace System
{

    using System;
    using System.Globalization;
    using System.Runtime.CompilerServices;

    [Serializable]
    public struct Double
    {
        internal double m_value;

        //
        // Public Constants
        //
        public const double MinValue = -1.7976931348623157E+308;
        public const double MaxValue = 1.7976931348623157E+308;
        // Real value of Epsilon: 4.9406564584124654e-324 (0x1), but JVC misparses that
        // number, giving 2*Epsilon (0x2).
        public const double Epsilon = 4.9406564584124650E-324;
        public const double NegativeInfinity = (double)-1.0 / (double)(0.0);
        public const double PositiveInfinity = (double)1.0 / (double)(0.0);

        public override String ToString()
        {
            return Number.Format(m_value, false, "G", NumberFormatInfo.CurrentInfo);
        }

        public String ToString(String format)
        {
            return Number.Format(m_value, false, format, NumberFormatInfo.CurrentInfo);
        }

        public static double Parse(String s)
        {
            if (s == null)
            {
                throw new ArgumentNullException();
            }

            return Convert.ToDouble(s);
        }

    }
}


