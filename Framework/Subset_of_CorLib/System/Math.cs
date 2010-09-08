////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace System
{

    //This class contains only static members and doesn't require serialization.
    using System;
    using System.Runtime.CompilerServices;
    public static class Math
    {

        public const double PI = 3.14159265358979323846;
        public const double E = 2.7182818284590452354;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern double Round(double a);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern double Ceiling(double a);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern double Floor(double d);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern double Pow(double x, double y);

        public static int Abs(int val)
        {
            return (val >= 0) ? val : -val;
        }

        public static int Max(int val1, int val2)
        {
            return (val1 >= val2) ? val1 : val2;
        }

        public static int Min(int val1, int val2)
        {
            return (val1 <= val2) ? val1 : val2;
        }

    }
}


