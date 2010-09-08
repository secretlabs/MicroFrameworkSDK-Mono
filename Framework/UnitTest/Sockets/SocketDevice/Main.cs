using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using Microsoft.SPOT.Net.NetworkInformation;

namespace Microsoft.SPOT.Samples.Sockets
{
    public class PingPong
    {
        Socket socketServer;
        Thread udpThread;

        private long DottedDecimalToIp(byte a1, byte a2, byte a3, byte a4)
        {
            return (long)((ulong)a4 << 24 | (ulong)a3 << 16 | (ulong)a2 << 8 | (ulong)a1); 
        }

        private byte[] StringToByteArray(string txt)
        {
            char[] cdata = txt.ToCharArray();
            byte[] data = new byte[cdata.Length];

            for (int i = 0; i < cdata.Length; i++)
            {
                data[i] = (byte)cdata[i];
            }
            return data;
        }

        private void SocketThread()
        {
            byte[] buff = new byte[100];
            Socket s = m_sockets[m_sockets.Count - 1] as Socket;
            m_evt.Set();

            s.Send(StringToByteArray("Socket " + m_sockets.Count + " created\r\n"));
            try
            {
                while (true)
                {
                    if(s.Poll(-1, SelectMode.SelectRead))
                    {
                        Thread.Sleep(0);

                        int len = s.Available;

                        if (len > buff.Length) len = buff.Length;

                        len = s.Receive(buff, len, SocketFlags.None);
                        if (len > 0)
                        {
                            s.Send(buff, len, SocketFlags.None);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                s.Close();
                m_socketCloseEvt.Set();
                m_sockets.Remove(s);
            }
        }
        private AutoResetEvent m_evt = new AutoResetEvent(false);
        private AutoResetEvent m_socketCloseEvt = new AutoResetEvent(false);
        private ArrayList m_sockets = new ArrayList();
        private ArrayList m_threads = new ArrayList();
        public void StartServer()
        {
            socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();
            IPAddress addr = null;

            foreach (NetworkInterface ni in nis)
            {
                if (ni.IPAddress.Length <= 15)
                {
                    addr = IPAddress.Parse(ni.IPAddress);
                    break;
                }
            }

            if (addr == null) return;

            Debug.Print("Device IP Address is: " + addr.ToString());

            socketServer.Bind(new IPEndPoint(addr, 33555));
            //socketServer.Bind(new IPEndPoint(DottedDecimalToIp(127, 0, 0, 1), 123));
            //socketServer.Bind(new IPEndPoint(IPAddress.Any, 0));
            //socketServer.Bind(new IPEndPoint(DottedDecimalToIp(192, 168, 187, 163), 0));

            socketServer.Listen(1);

            try
            {
                while (true)
                {
                    try
                    {
                        Debug.Print("Waiting for client to connect");
                        Socket s = socketServer.Accept();
                        m_sockets.Add(s);
                        Debug.Print("Socket " + m_sockets.Count + " connected");
                        Thread th = new Thread(new ThreadStart(SocketThread));
                        th.Start();
                        m_threads.Add(th);
                        m_evt.WaitOne();
                    }
                    catch (Exception e)
                    {
                        Debug.Print("Exception: " + e.ToString());
                        m_socketCloseEvt.WaitOne();
                    }
                }
            }
            finally
            {
                foreach (Thread th in m_threads)
                {
                    if (th.IsAlive)
                    {
                        th.Abort();
                        th.Join();
                    }
                }
                socketServer.Close();
            }
        }

        void UdpThread()
        {
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];

            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(ni.IPAddress), 54321);
            listener.Bind(localEP);

            byte[] data = new byte[1024];


            while (true)
            {
                int len;
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                if (0 < (len = listener.ReceiveFrom(data, SocketFlags.None, ref ep)))
                {
                    ep = new IPEndPoint(((IPEndPoint)ep).Address, 1237);

                    listener.SendTo(data, len, SocketFlags.None, ep);
                }
            }
        }

        public void StartUDP()
        {
            udpThread = new Thread(new ThreadStart(UdpThread));

            udpThread.Start();
        }

        public void WaitForNetwork()
        {
            while (true)
            {
                NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();

                if (nis[0].IPAddress != "192.168.5.100")
                    break;

                Thread.Sleep(1000);
            }
        }


        static void Main(string[] args)
        {
            PingPong png = new PingPong();

            png.WaitForNetwork();

            png.StartUDP();
            png.StartServer();
            Debug.Print("Ping Pong Server Exited!");
        }
    }
}
