using System;
using System.Threading;
using System.Collections;
using System.Runtime.CompilerServices;

using Microsoft.SPOT.Hardware;
using System.IO.Ports;


namespace Microsoft.SPOT.UnitTest.SerialPortUnitTest
{
    public class SerialXonXoff
    {
        static void Main(string[] args)
        {   
            SerialPort ser = new SerialPort( "COM1", (int)BaudRate.Baud115200 );
            bool b = true;
            byte[] buf = new byte[26];

            ser.Handshake = Handshake.XOnXOff;

            ser.Open();

            Debug.Print("Initializing Serial Test");
                
            for (int i = 0;i < buf.Length;i++)
            {
                buf[i] = (byte)('a' + i);
            }
            while (b)
            {
                int len = ser.Write(buf, 0, buf.Length);
                if (len > 26)
                {
                    Debug.Print("ERROR: LENGTH RETURNED TOO LONG");
                }
                else if (len < 0)
                {
                    Debug.Print("ERROR: Write returned error");
                }
                else if (len < 26)
                {
                    while (len < 26)
                    {
                        int tlen = ser.Write(buf, len, buf.Length - len);
                        if( tlen > 0 )
                        {
                            len += tlen;
                        }
                    }
                }
                Thread.Sleep(10);
            }
             
        }
    }
}
