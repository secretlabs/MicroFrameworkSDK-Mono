using Microsoft.SPOT;
using System.Diagnostics;

namespace System.Ext
{
    public static class Console
    {
        public static bool Verbose = false;

        //--//

        [Conditional("DEBUG")]
        public static void Write(byte[] message, int index, int count)
        {
            if (Verbose && message != null)
            {
                try
                {
                    Debug.Print(new string(System.Text.Encoding.UTF8.GetChars(message), index, count));
                }
                catch
                {
                }
            }
        }

        [Conditional("DEBUG")]
        public static void Write(byte[] message)
        {
            Write(message, 0, message.Length);
        }

        [Conditional("DEBUG")]
        public static void Write(string message)
        {
            if (Verbose)
            {
                Debug.Print(message);
            }
        }
    }
}


