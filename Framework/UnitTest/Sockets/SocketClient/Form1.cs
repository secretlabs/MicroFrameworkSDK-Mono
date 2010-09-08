using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;

namespace SocketClient
{
    public partial class Form1 : Form
    {
        IPAddress m_ip;
        int m_port;
        ArrayList m_sockets = new ArrayList();
        ArrayList m_threads = new ArrayList();
        AutoResetEvent m_evt = new AutoResetEvent(false);
        Socket m_udpSocket = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                m_ip = IPAddress.Parse(textBox1.Text);
            }
            catch
            {
                MessageBox.Show("Invalid IP address");
                textBox1.Text = "";
                return;
            }
            try
            {
                m_port = int.Parse(textBox2.Text);
            }
            catch
            {
                MessageBox.Show("Invalid Port number");
                textBox2.Text = "";
                return;
            }

            try
            {
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.ReceiveTimeout = 3000;
                sock.SendTimeout = 3000;
                m_sockets.Add(sock);
                try
                {
                    sock.Connect(m_ip, m_port);

                    Thread rcvTh = new Thread(new ThreadStart(RecvThread));
                    m_threads.Add(rcvTh);
                    rcvTh.Start();

                    m_evt.WaitOne();

                    sock.Send(StringToByteArray("Socket " + m_sockets.Count + " Requested\r\n"));

                    button3.Enabled = true;

                }
                catch
                {
                    if (sock.Connected)
                    {
                        sock.Shutdown(SocketShutdown.Both);
                    }
                    sock.Close();

                    m_sockets.Remove(sock);
                    MessageBox.Show("Unable to connect to " + m_ip.ToString());
                }
            }
            catch
            {
                MessageBox.Show("Unable to connect to " + m_ip.ToString());
            }

        }

        private void RecvThread()
        {
            byte []data = new byte[100];
            Socket sock = m_sockets[m_sockets.Count - 1] as Socket;

            m_evt.Set();

            while (true)
            {
                try
                {
                    int len = sock.Receive(data);
                    StringBuilder sb = new StringBuilder(len);
                    if (len > 0)
                    {
                        for (int i = 0; i < len; i++)
                        {
                            sb.Append((char)data[i]);
                        }
                        richTextBox2.Invoke((MethodInvoker)delegate
                        {
                            richTextBox2.AppendText(sb.ToString());
                            richTextBox2.ScrollToCaret();
                        });
                    }
                    else
                    {
                        break;
                    }
                }
                catch (SocketException se)
                {
                    if (se.ErrorCode != (int)SocketError.TimedOut && se.ErrorCode != (int)SocketError.WouldBlock)
                    {
                        System.Diagnostics.Debug.WriteLine(se.Message);
                        break;
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    break;
                }
            }
        }

        private void CreateUdpSock()
        {
            byte[] data = new byte[1024];
            m_udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint localEP = null;

            IPHostEntry he = Dns.GetHostEntry("");

            foreach(IPAddress addr in he.AddressList)
            {
                if(addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    localEP = new IPEndPoint(addr, 1237);
                    break;
                }
            }

            m_udpSocket.ReceiveTimeout = 10000;
            m_udpSocket.Bind(localEP);
            m_udpSocket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoChecksum, true);
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

        private void button2_Click(object sender, EventArgs e)
        {
            int idx = m_sockets.Count - 1;
            if (richTextBox1.Text.Length > 0 && idx >= 0)
            {
                byte[] data = StringToByteArray(richTextBox1.Text);
                int cnt = (m_sockets[idx] as Socket).Send(data);
                richTextBox1.Text = "";
                richTextBox2.AppendText(cnt + " bytes sent\r\n");
                richTextBox2.ScrollToCaret();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (Socket sock in m_sockets)
            {
                if (sock.Connected)
                {
                    sock.Shutdown(SocketShutdown.Both);
                }
                sock.Close();
            }
            m_sockets.Clear();
            m_threads.Clear();

            button1.Enabled = true;
            button3.Enabled = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            button3_Click(null, null);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            richTextBox2.Text = "";
        }

        private void buttonPing_Click(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < m_sockets.Count; i++)
                {
                    Socket s = m_sockets[i] as Socket;
                    s.Send(StringToByteArray("Ping from socket " + (i + 1) + "\r\n"));
                }
            }
            catch
            {
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (richTextBox1.Text.Length > 0)
            {
                if (m_udpSocket == null)
                {
                    CreateUdpSock();
                }

                byte[] data = StringToByteArray(richTextBox1.Text);
                int cnt = m_udpSocket.SendTo(data, new IPEndPoint(IPAddress.Parse(textBox1.Text), 54321));
                richTextBox1.Text = "";
                richTextBox2.AppendText(cnt + " bytes sent\r\n");
                richTextBox2.ScrollToCaret();

                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    int len = m_udpSocket.ReceiveFrom(data, ref ep);

                    if (len > 0)
                    {
                        StringBuilder sb = new StringBuilder(len);
                        for (int i = 0; i < len; i++)
                        {
                            sb.Append((char)data[i]);
                        }

                        richTextBox2.AppendText(sb.ToString());
                        richTextBox2.ScrollToCaret();
                    }
                }
                catch
                {
                }
            }

        }
    }
}