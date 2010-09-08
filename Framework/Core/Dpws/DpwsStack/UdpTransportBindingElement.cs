using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections;
using Ws.Services.Soap;
using Ws.Services.Transport;
using Ws.Services.Discovery;

namespace Ws.Services.Binding
{
    /// <summary>
    /// Abtracts the configuration for the UDP transport
    /// </summary>
    public class UdpTransportBindingConfig
    {
        /// <summary>
        /// Creates an instance of the UDP configuration
        /// </summary>
        /// <param name="discoveryAddress">The discovery address of the UDP service.</param>
        /// <param name="discoveryPort">The discovery port of the UDP service.</param>
        /// <param name="ignoreRequestsFromThisIp">When <c>true</c>, all requests from thsi IP address will be ignored.</param>
        public UdpTransportBindingConfig(IPAddress discoveryAddress, int discoveryPort, bool ignoreRequestsFromThisIp)
        {
            this.DiscoveryAddress         = discoveryAddress;
            this.DiscoveryPort            = discoveryPort;
            this.IgnoreRequestsFromThisIp = ignoreRequestsFromThisIp;
        }

        /// <summary>
        /// When true, all requests from thsi IP address will be ignored
        /// </summary>
        public readonly bool      IgnoreRequestsFromThisIp;
        /// <summary>
        /// The discovery address of this service.
        /// </summary>
        public readonly IPAddress DiscoveryAddress;
        /// <summary>
        /// The discovery port of this service
        /// </summary>
        public readonly int       DiscoveryPort;
    }

    /// <summary>
    /// Abtracts the BindingElement for the UDP transport
    /// </summary>
    public class UdpTransportBindingElement : TransportBindingElement
    {
        Socket                    m_udpReceiveClient;
        Socket                    m_udpSendClient;
        UdpTransportBindingConfig m_config;
        EndPoint                  m_remoteEndpoint;

        /// <summary>
        ///  The maximum size of a UDP packet 
        /// </summary>
        public const int MaxUdpPacketSize = 5229;

        /// <summary>
        /// Creates an instance of the UDP binding element
        /// </summary>
        /// <param name="cfg">The configuration associated with this binding element.</param>
        public UdpTransportBindingElement(UdpTransportBindingConfig cfg)
        {
            m_config = cfg;
        }

        //public override string Transport
        //{
        //    get { return "udp://" + m_endpointAddress.Host + ":" + m_endpointAddress.Port + "/"; }
        //}

        /// <summary>
        /// Sets the configuration for the UDP transport binding 
        /// </summary>
        /// <param name="cfg">The configuration for this binding.</param>
        protected override void OnSetBindingConfiguration(object cfg)
        {
            if (cfg is HttpTransportBindingConfig)
            {
                m_config = (UdpTransportBindingConfig)cfg;
            }

            if (m_config == null) throw new Exception(); // no binding configuration
        }

        /// <summary>
        /// Opens the stream for the UDP tansport binding 
        /// </summary>
        /// <param name="stream">The stream for this binding.</param>
        /// <param name="ctx">The context associated with the stream for this binding.</param>
        /// <returns>The handling status for this operation.</returns>
        protected override ChainResult OnOpen( ref Stream stream, BindingContext ctx )
        {
            m_udpReceiveClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, m_config.DiscoveryPort);
            m_udpReceiveClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            m_udpReceiveClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 0x5000);
            m_udpReceiveClient.Bind(localEP);
            // Join Multicast Group
            byte[] discoveryAddr = m_config.DiscoveryAddress.GetAddressBytes();
            byte[] multicastOpt = new byte[] { discoveryAddr[0], discoveryAddr[1], discoveryAddr[2], discoveryAddr[3],   // WsDiscovery Multicast Address: 239.255.255.250
                                                 0,   0,   0,   0 }; // IPAddress.Any: 0.0.0.0
            m_udpReceiveClient.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOpt);

            // Create a UdpClient used to send request responses. Set SendTimeout.
            m_udpSendClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_udpSendClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            return ChainResult.Continue;
        }

        /// <summary>
        /// Closes the stream for the UDP transport binding
        /// </summary>
        /// <param name="stream">The stream for this binding.</param>
        /// <param name="ctx">The context associated with the stream for this binding.</param>
        /// <returns>The handling status for this operation.</returns>
        protected override ChainResult OnClose( Stream stream, BindingContext ctx )
        {
            m_udpReceiveClient.Close();
            m_udpSendClient.Close();

            return ChainResult.Handled;
        }

        /// <summary>
        /// Processes a message
        /// </summary>
        /// <param name="stream">The message being processed.</param>
        /// <param name="ctx">The context associated with the message.</param>
        /// <returns>The handling status for this operation.</returns>
        protected override ChainResult OnProcessInputMessage( ref WsMessage msg, BindingContext ctx )
        {
            byte[] soapMessage = null;
            byte[] buffer = new byte[MaxUdpPacketSize];

            while (true)
            {
                EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);

                int size = m_udpReceiveClient.ReceiveFrom(buffer, MaxUdpPacketSize, SocketFlags.None, ref remoteEndpoint);

                // If the stack is set to ignore request from this address do so
                if (m_config.IgnoreRequestsFromThisIp &&
                    ((IPEndPoint)m_remoteEndpoint).Address.ToString() == WsNetworkServices.GetLocalIPV4Address())
                {
                    continue;
                }

                if (size > 0)
                {
                    soapMessage = new byte[size];
                    Array.Copy(buffer, soapMessage, size);
                }
                else
                {
                    System.Ext.Console.Write("UDP Receive returned 0 bytes");
                }

                m_remoteEndpoint = remoteEndpoint;

                break;
            }

            System.Ext.Console.Write("UDP Request From: " + m_remoteEndpoint.ToString());
            System.Ext.Console.Write(soapMessage);

            msg.Body = soapMessage;

            return ChainResult.Continue;
        }

        /// <summary>
        /// Processes a message 
        /// </summary>
        /// <param name="msg">The message being processed.</param>
        /// <param name="ctx">The context associated with the message.</param>
        /// <returns>The handling status for this operation.</returns>
        protected override ChainResult OnProcessOutputMessage( ref WsMessage msg, BindingContext ctx )
        {
            if (m_remoteEndpoint == null) throw new Exception();

            byte []message = msg.Body as byte[];

            if (message == null) return ChainResult.Abort;

            System.Ext.Console.Write("UDP Message Sent To: " + m_remoteEndpoint.ToString());
            System.Ext.Console.Write(message);
            
            try
            {
                Random rand = new Random();
                for (int i = 0; i < 3; ++i)
                {
                    int backoff = rand.Next(200) + 50; // 50-250
                    System.Threading.Thread.Sleep(backoff);
                    m_udpSendClient.SendTo(message, message.Length, SocketFlags.None, m_remoteEndpoint);
                }
            }
            catch
            {
                return ChainResult.Abort;
            }

            return ChainResult.Handled;
        }
    }
}
