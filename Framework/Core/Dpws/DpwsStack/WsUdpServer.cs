using System;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using System.IO;
using Ws.Services.Faults;
using Ws.Services.Soap;
using Ws.Services.Transport;
using Ws.Services.Utilities;
using Ws.Services.WsaAddressing;
using Ws.Services.Discovery;
using Ws.Services.Binding;
using System.Runtime.CompilerServices;

using System.Ext;
using Microsoft.SPOT;

namespace Ws.Services.Transport.UDP
{
    /// <summary>
    /// Udp service host listens for and processes Udp request made to it's service endpoints.
    /// </summary>
    internal sealed class WsUdpServiceHost
    {
        private static WsUdpServiceHost m_instance = new WsUdpServiceHost();

        // Fields
        private bool               m_ignoreLocalRequests;
        private CustomBinding      m_binding;
        private IReplyChannel      m_replyChannel;
        private bool               m_requestStop;
        private Thread             m_thread;
        private WsThreadManager    m_threadManager;
        private WsServiceEndpoints m_serviceEndpoints;
        private int                m_refcount;

        // private
        private WsUdpServiceHost()
        {
            m_threadManager     = new WsThreadManager(5, "Udp");
            m_serviceEndpoints  = new WsServiceEndpoints();
            m_refcount          = 0;
            m_binding           = null;
            m_replyChannel      = null;

            UdpTransportBindingElement transport = new UdpTransportBindingElement(new UdpTransportBindingConfig(WsDiscovery.WsDiscoveryAddress, WsDiscovery.WsDiscoveryPort, m_ignoreLocalRequests));

            m_binding = new CustomBinding(transport);
        }

        public static WsUdpServiceHost Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new WsUdpServiceHost();
                }

                return m_instance;
            }
        }

        public void AddServiceEndpoint(IWsServiceEndpoint ep)
        {
            m_serviceEndpoints.Add(ep);
        }

        public void RemoveServiceEndpoint(IWsServiceEndpoint ep)
        {
            m_serviceEndpoints.Remove(ep);
        }


        /// <summary>
        /// Property controls whether discovery request originating from this IP will be ignored.
        /// </summary>
        public bool IgnoreRequestFromThisIP
        {
            set 
            {
                if(value != m_ignoreLocalRequests)
                {
                    m_ignoreLocalRequests = value;
                    ArrayList parms = new ArrayList();
                    parms.Add(new UdpTransportBindingConfig(WsDiscovery.WsDiscoveryAddress, WsDiscovery.WsDiscoveryPort, m_ignoreLocalRequests));
                    m_binding.Elements.SetBindingConfiguration(parms);
                }
            }
            get
            {
                return m_ignoreLocalRequests;
            }
        }

        /// <summary>
        /// Listens for Udp request on 239.255.255.250:3702
        /// </summary>
        /// <remarks>On initialization it sends a Discovery Hello message and listens on the Ws-Discovery
        /// endpoint for a request. When a request arrives it starts a UdpProcess thread that processes the message.
        /// The number of UdpProcessing threads are limited by the Device.MaxUdpRequestThreads property.
        /// </remarks>
        private void Listen()
        {
            // Create a duplicate message tester.
            WsMessageCheck messageCheck = new WsMessageCheck(40);

            while (!m_requestStop)
            {
                try
                {
                    // If threads ara availble receive next message. If we are waiting on threads let the socket
                    // buffer request until we get a thread. This will work until the reveice buffer is depleted
                    // at which time request will be dropped
                    if (m_threadManager.ThreadsAvailable == true)
                    {
                        RequestContext req = m_replyChannel.ReceiveRequest();

                        if (req != null)
                        {
                            WsWsaHeader header = req.Message.Header;

                            if (header.MessageID != null &&
                                messageCheck.IsDuplicate(header.MessageID,header.From != null ? header.From.Address.AbsoluteUri : ""))
                            {
                                continue;
                            }

                            // Try to get a processing thread and process the request
                            m_threadManager.StartNewThread(new WsUdpMessageProcessor(m_serviceEndpoints, req));
                        }
                        else
                        {
                            System.Ext.Console.Write("UDP Receive returned 0 bytes");
                        }
                        
                    }
                    else
                    {
                        System.Ext.Console.Write("Udp service host waiting for a thread...");

                        m_threadManager.ThreadEvent.WaitOne();
                    }
                }
                catch (SocketException se)
                {
                    // Since the MF Socket does not have IOControl that would be used to turn off ICMP notifications
                    // for UDP, catch 10054 and try to continue
                    if ((SocketError)se.ErrorCode == SocketError.ConnectionReset)
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception e)
                {
                    System.Ext.Console.Write(e.Message + " " + e.InnerException);
                }
            }
        }

        /// <summary>
        /// Use to get or set the maximum number of processing threads for Udp request. Default is 5.
        /// </summary>
        public int MaxThreadCount 
        { 
            get { return m_threadManager.MaxThreadCount; } 
            set { m_threadManager.MaxThreadCount = value; } 
        }

        /// <summary>
        /// Use to start the Udp service listening.
        /// </summary>
        [MethodImplAttribute( MethodImplOptions.Synchronized )]
        public void Start(ServerBindingContext ctx)
        {
            if (1 == Interlocked.Increment(ref m_refcount))
            {
                m_requestStop = false;
                m_replyChannel = m_binding.CreateServerChannel(ctx);
                m_replyChannel.Open();
                m_thread = new Thread(new ThreadStart(this.Listen));
                m_thread.Start();
            }
        }

        /// <summary>
        /// Stops the WsUdpServiceHost listening process.
        /// </summary>
        [MethodImplAttribute( MethodImplOptions.Synchronized )]
        public void Stop()
        {
            if(0 == Interlocked.Decrement(ref m_refcount))
            {
                // Stop processing loop;
                m_requestStop = true;
                m_threadManager.ThreadEvent.Set();

                m_replyChannel.Close();

                m_thread.Join();
            }
        }
    }

    /// <summary>
    /// The WsUdpServiceHost spins a UdpProcess thread for each request. This class is responsible for sending a
    /// request to the processing system and returning a Udp response.
    /// </summary>
    sealed class WsUdpMessageProcessor : IWsTransportMessageProcessor
    {
        private WsServiceEndpoints m_serviceEndpoints;
        private RequestContext m_request;

        /// <summary>
        /// Creates an empty instance of the UdpProcess class.
        /// </summary>
        public WsUdpMessageProcessor(WsServiceEndpoints serviceEndpoints, RequestContext request) 
        {
            m_serviceEndpoints = serviceEndpoints;
            m_request = request;
        }

        /// <summary>
        /// This method is called by the process manager to process a request.
        /// </summary>
        public void ProcessRequest()
        {

            // Performance debuging
            DebugTiming timeDebuger = new DebugTiming();
            timeDebuger.ResetStartTime("***Request Debug timer started");

            // Process the message
            WsMessage response = ProcessRequestMessage(m_request.Message);

            // If response is null the requested service is not implemented so just ignore this request
            if (response == null)
                return;

            // Performance debuging
            timeDebuger.PrintElapsedTime("***ProcessMessage Took");

            // Send the response
            m_request.Reply(response);

            // Performance Debuging
            timeDebuger.PrintElapsedTime("***Send Message Took");
        }

        /// <summary>
        /// Parses a Udp transport message and builds a header object and envelope document then calls processRequest
        /// on a service endpoint contained.
        /// </summary>
        /// <param name="soapRequest">A byte array containing a raw soap request message.  If null no check is performed.</param>
        /// <param name="messageCheck">A WsMessageCheck objct used to test for duplicate request.</param>
        /// <param name="remoteEP">The remote endpoint address of the requestor.</param>
        /// <returns>A byte array containing a soap response message returned from a service endpoint.</returns>
        public WsMessage ProcessRequestMessage(WsMessage message)
        {
            // Parse and validate the soap message
            WsWsaHeader header = message.Header;

            // Check Udp service endpoints collection for a target service.
            int count = m_serviceEndpoints.Count;

            // DO NOT USE the subscript operator for endpoint addresses (WsServiceEndpoints[EnpointAddress])
            // for local host testing there are multiple endpoints with the same address but different operations
            for (int i = 0; i < count; i++)
            {
                IWsServiceEndpoint serviceEndpoint = m_serviceEndpoints[i];

                if (serviceEndpoint.EndpointAddress == header.To)
                {
                    if (serviceEndpoint.ServiceOperations[header.Action] != null)
                    {
                        // Don't block discovery processes.
                        serviceEndpoint.BlockingCall = false;
                        try
                        {
                            return serviceEndpoint.ProcessRequest(message);
                        }
                        catch (WsFaultException e)
                        {
                            return WsFault.GenerateFaultResponse(e, m_request.Version);
                        }
                        catch
                        {
                            // If a valid Action is not found, fault
                            return WsFault.GenerateFaultResponse(header, WsFaultType.WsaDestinationUnreachable, "To: " + header.To + " Action: " + header.Action, m_request.Version);
                        }
                    }
                }
            }

            // Return null if service endpoint was not found
            System.Ext.Console.Write("Udp service endpoint was not found.");
            System.Ext.Console.Write("  Endpoint Address: " + header.To);
            System.Ext.Console.Write("  Action: " + header.Action);

            return null;
        }
    }
}


