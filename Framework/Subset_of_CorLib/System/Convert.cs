////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace System
{
    //We don't want to implement this whole class, but VB needs an external function to convert any integer type to a Char.
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Convert
    {
        [CLSCompliant(false)]
        public static char ToChar(ushort value)
        {
            return (char)value;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(string value)
        {
            return (sbyte)ToInt64(value, true, SByte.MinValue, SByte.MaxValue);
        }

        public static byte ToByte(string value)
        {
            return (byte)ToInt64(value, false, Byte.MinValue, Byte.MaxValue);
        }

        public static short ToInt16(string value)
        {
            return (short)ToInt64(value, true, Int16.MinValue, Int16.MaxValue);
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(string value)
        {
            return (ushort)ToInt64(value, false, UInt16.MinValue, UInt16.MaxValue);;
        }

        public static int ToInt32(string value)
        {
            return (int)ToInt64(value, true, Int32.MinValue, Int32.MaxValue);
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(string value)
        {
            return (uint)ToInt64(value, false, UInt32.MinValue, UInt32.MaxValue);
        }

        public static long ToInt64(string value)
        {
            return ToInt64(value, true, Int64.MinValue, Int64.MaxValue);
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(string value)
        {
            return (ulong)ToInt64(value, false, 0, 0);
        }

        //--//

        public static int ToInt32(string hexNumber, int fromBase)
        {
            if (hexNumber == null)
                return 0;

            if (fromBase != 16)
                throw new ArgumentException();

            int result = 0;
            int digit;

            char[] hexDigit = hexNumber.Trim(' ').ToUpper().ToCharArray();

            // Trim hex sentinal if present 
            int len = hexDigit.Length;
            int i   = (len >= 2 && hexDigit[0] == '0' && hexDigit[1] == 'X') ? 2 : 0;

            // 8 hex chars == 4 bytes == sizeof(Int32)
            if ((len - i) > 8) throw new ArgumentException();

            // Convert hex to integer
            for (; i < len; i++)
            {
                char c = hexDigit[i];

                switch (c)
                {
                    case '0':
                        digit = 0;
                        break;
                    case '1':
                        digit = 1;
                        break;
                    case '2':
                        digit = 2;
                        break;
                    case '3':
                        digit = 3;
                        break;
                    case '4':
                        digit = 4;
                        break;
                    case '5':
                        digit = 5;
                        break;
                    case '6':
                        digit = 6;
                        break;
                    case '7':
                        digit = 7;
                        break;
                    case '8':
                        digit = 8;
                        break;
                    case '9':
                        digit = 9;
                        break;
                    case 'A':
                        digit = 10;
                        break;
                    case 'B':
                        digit = 11;
                        break;
                    case 'C':
                        digit = 12;
                        break;
                    case 'D':
                        digit = 13;
                        break;
                    case 'E':
                        digit = 14;
                        break;
                    case 'F':
                        digit = 15;
                        break;
                    default:
                        throw new ArgumentException();
                }

                result <<= 4;
                result += digit;
            }

            return result;
        }

        public static double ToDouble(string s)
        {
            if (s == null)
                return 0;

            s = s.Trim(' ').ToLower();

            int decimalpoint = s.IndexOf('.');
            int exp          = s.IndexOf('e');
            
            if (exp != -1 && decimalpoint > exp)
                throw new Exception();

            char [] chars           = s.ToCharArray();
            int     len             = chars.Length;
            double  power           = 0;
            double  rightDecimal    = 0;
            int     decLeadingZeros = 0;
            double  leftDecimal     = 0;
            int     leftDecLen      = 0;

            // convert the exponential portion to a number            
            if (exp != -1 && exp + 1 < len - 1)
            {
                int tmp;
                power = GetDoubleNumber(chars, exp + 1, len - (exp + 1), out tmp);
            }

            // convert the decimal portion to a number
            if (decimalpoint != -1)
            {
                double number;
                int decLen;

                if (exp == -1)
                {
                    decLen = len - (decimalpoint + 1);
                }
                else
                {
                    decLen = (exp - (decimalpoint + 1));
                }

                number = GetDoubleNumber(chars, decimalpoint + 1, decLen, out decLeadingZeros);

                rightDecimal = number * System.Math.Pow(10, -decLen);
            }

            // convert the integer portion to a number
            if (decimalpoint != 0)
            {
                int leadingZeros;
                
                     if (decimalpoint == -1 && exp == -1) leftDecLen = len;
                else if (decimalpoint != -1)              leftDecLen = decimalpoint;
                else                                      leftDecLen = exp;

                leftDecimal = GetDoubleNumber(chars, 0, leftDecLen, out leadingZeros);
                // subtract leading zeros from integer length
                leftDecLen -= leadingZeros;

                if (chars[0] == '-' || chars[0] == '+') leftDecLen--;
            }

            double value = 0;
            if (leftDecimal < 0)
            {
                value = -leftDecimal + rightDecimal;
                value = -value;
            }
            else
            {
                value = leftDecimal + rightDecimal;
            }

            // lets normalize the integer portion first
            while(leftDecLen > 1)
            {
                switch(leftDecLen)
                {
                    case 2:
                        value      /= 10.0;
                        power      += 1;
                        leftDecLen -= 1;
                        break;
                    case 3:
                        value      /= 100.0;
                        power      += 2;
                        leftDecLen -= 2;
                        break;                    
                    case 4:
                        value      /= 1000.0;
                        power      += 3;
                        leftDecLen -= 3;
                        break;
                    default:
                        value      /= 10000.0;
                        power      += 4;
                        leftDecLen -= 4;
                        break;
                }
            }

            // now normalize the decimal portion
            if (value != 0.0 && value < 1.0 && value > -1.0)
            {
                // for normalization we want x.xxx instead of 0.xxx
                decLeadingZeros++;

                while(decLeadingZeros > 0)
                {
                    switch (decLeadingZeros)
                    {
                        case 1:
                            value           *= 10.0;
                            power           -= 1;
                            decLeadingZeros -= 1;
                            break;
                        case 2:
                            value           *= 100.0;
                            power           -= 2;
                            decLeadingZeros -= 2;
                            break;
                        case 3:
                            value           *= 1000.0;
                            power           -= 3;
                            decLeadingZeros -= 3;
                            break;
                        default:
                            value           *= 10000.0;
                            power           -= 4;
                            decLeadingZeros -= 4;
                            break;
                    }
                }
            }

            // special case for epsilon (the System.Math.Pow native method will return zero for -324)
            if (power == -324)
            {
                value = value * System.Math.Pow(10, power + 1);
                value /= 10.0;
            }
            else
            {
                value = value * System.Math.Pow(10, power);
            }

            if (value == double.PositiveInfinity || value == double.NegativeInfinity)
            {
                throw new Exception();
            }

            return value;
        }

        //--//

        private static long ToInt64(string value, bool signed, long min, long max)
        {
            if (value == null)
                return 0;

            value = value.Trim(' ');

            char[] num    = value.ToCharArray();
            int    len    = num.Length;
            ulong  result = 0;
            int    index  = 0;
            bool   isNeg  = false;

            // check the sign
            if (num[0] == '-')
            {
                isNeg = true;
                index = 1;
            }
            else if (num[0] == '+')
            {
                index = 1;
            }
            
            for (int i = index; i < len; i++)
            {
                ulong digit;
                char c = num[i];

                // switch statement is faster than subtracting '0'
                switch(c)
                {
                    case '0':
                        digit = 0;
                        break;
                    case '1':
                        digit = 1;
                        break;
                    case '2':
                        digit = 2;
                        break;
                    case '3':
                        digit = 3;
                        break;
                    case '4':
                        digit = 4;
                        break;
                    case '5':
                        digit = 5;
                        break;
                    case '6':
                        digit = 6;
                        break;
                    case '7':
                        digit = 7;
                        break;
                    case '8':
                        digit = 8;
                        break;
                    case '9':
                        digit = 9;
                        break;
                    default:
                        throw new Exception();
                }

                // check for overflow - any number greater than this number will cause an overflow
                // when multiplied by 10
                if(( signed && result > 0x0CCCCCCCCCCCCCCC) || 
                   (!signed && result > 0x1999999999999999))
                {
                    throw new Exception();
                }

                result *= 10;
                result += digit;
            }

            if (isNeg && !signed && result != 0) throw new Exception();

            long res;

            if (isNeg)
            {
                res = -(long)result;

                // if the result is not negative, we had an overflow
                if(res > 0) throw new Exception();
            }
            else
            {
                res = (long)result;

                // if the result is negative and we are not converting a
                // UInt64, we had an overflow
                if(max != 0 && res < 0) throw new Exception();
            }

            // final check for max/min
            if (max != 0 && (res < min || res > max)) throw new Exception();

            return res;
        }

        private static double GetDoubleNumber(char[] chars, int start, int length, out int numLeadingZeros)
        {
            double number = 0;
            bool   isNeg  = false;
            int    end    = start + length;

            numLeadingZeros = 0;

            if(chars[start] == '-')
            {
                isNeg      = true;
                start++;
            }
            else if(chars[start] == '+')
            {
                start++;
            }

            for (int i = start; i < end; i++)
            {
                int  digit;
                char c = chars[i];

                // switch statement is faster than subtracting '0'                
                switch(c)
                {
                    case '0':
                        // update the number of leading zeros (used for normalizing)
                        if((numLeadingZeros + start) == i)
                        {
                            numLeadingZeros++;
                        }
                        digit = 0;
                        break;
                    case '1':
                        digit = 1;
                        break;
                    case '2':
                        digit = 2;
                        break;
                    case '3':
                        digit = 3;
                        break;
                    case '4':
                        digit = 4;
                        break;
                    case '5':
                        digit = 5;
                        break;
                    case '6':
                        digit = 6;
                        break;
                    case '7':
                        digit = 7;
                        break;
                    case '8':
                        digit = 8;
                        break;
                    case '9':
                        digit = 9;
                        break;
                    default:
                        throw new Exception();
                }

                number *= 10;
                number += digit;
            }

            return isNeg ? -number : number;
        }
    }
}


