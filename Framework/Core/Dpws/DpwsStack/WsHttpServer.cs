using System;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Xml;
using Ws.Services;
using Ws.Services.WsaAddressing;
using Ws.Services.Faults;
using Ws.Services.Mtom;
using Ws.Services.Soap;
using Ws.Services.Transport;
using Ws.Services.Utilities;
using _Bind=Ws.Services.Binding;

using System.Ext;
using Microsoft.SPOT;
using System.Runtime.CompilerServices;

namespace Ws.Services.Transport.HTTP
{
    /// <summary>
    /// An Http Service host listens for and processes request made to it's service endpoints.
    /// </summary>
    internal class WsHttpServiceHost
    {
        // Fields
        private bool                   m_isStarted;
        private Thread                 m_thread;
        private WsServiceEndpoints     m_serviceEndpoints;
        private WsThreadManager        m_threadManager;
        private _Bind.Binding          m_binding;
        private _Bind.IReplyChannel    m_replyChannel;

        private static int m_maxReadPayload = 0x20000;

        /// <summary>
        /// Creates a http service host.
        /// </summary>
        /// <param name="port">An integer containing the port number this host will listen on.</param>
        /// <param name="serviceEndpoints">A collection of service endpoints this transport service can dispatch to.</param>
        public WsHttpServiceHost(_Bind.Binding binding, WsServiceEndpoints serviceEndpoints)
        {
            m_serviceEndpoints = new WsServiceEndpoints();
            m_threadManager    = new WsThreadManager(5, "Http");            
            m_binding          = binding;
            m_serviceEndpoints = serviceEndpoints;
            m_isStarted        = false;
        }

        /// <summary>
        /// Use to get or set the maximum number of processing threads for Udp request. Default is 5.
        /// </summary>
        public int MaxThreadCount { get { return m_threadManager.MaxThreadCount; } set { m_threadManager.MaxThreadCount = value; } }

        /// <summary>
        /// Property containing the maximum message size this transport service will accept.
        /// </summary>
        public static int MaxReadPayload { get { return m_maxReadPayload; } set { m_maxReadPayload = value; } }

        /// <summary>
        /// Use to start the Http Server listening for request.
        /// </summary>
        [MethodImplAttribute( MethodImplOptions.Synchronized )]
        public void Start(_Bind.ServerBindingContext ctx)
        {
            if (m_isStarted) throw new InvalidOperationException();

            m_isStarted = true;

            m_replyChannel = m_binding.CreateServerChannel(ctx);
            m_replyChannel.Open();

            m_thread = new Thread(new ThreadStart(this.Listen));
            m_thread.Start();
        }

        /// <summary>
        /// Use to stop the Http service.
        /// </summary>
        [MethodImplAttribute( MethodImplOptions.Synchronized )]
        public void Stop()
        {
            if(!m_isStarted) throw new InvalidOperationException();

            m_isStarted = false;
            
            m_replyChannel.Close();
            m_thread.Join();
        }

        /// <summary>
        /// Collection property containing service endpoints for this service host.
        /// </summary>
        public WsServiceEndpoints ServiceEndpoints { get { return m_serviceEndpoints; } set { m_serviceEndpoints = value; } }

        /// <summary>
        /// HttpServer Socket Listener
        /// </summary>
        public void Listen()
        {
            // Create listener and start listening
            while (m_isStarted)
            {
                try
                {
                    _Bind.RequestContext context = m_replyChannel.ReceiveRequest();

                    // The context returned by m_httpListener.GetContext(); can be null in case the service was stopped.
                    if (context != null)
                    {
                        WsHttpMessageProcessor processor = new WsHttpMessageProcessor(m_serviceEndpoints, context);

                        if (m_threadManager.ThreadsAvailable == false)
                        {
                            WsWsaHeader header = new WsWsaHeader();
                            
                            context.Reply(WsFault.GenerateFaultResponse(header, WsFaultType.WsaEndpointUnavailable, "Service Unavailable (busy)", context.Version));

                            System.Ext.Console.Write("Http max thread count exceeded. Request ignored.");
                        }
                        else
                        {
                            // Try to get a processing thread and process the request
                            m_threadManager.StartNewThread(processor);
                        }
                    }
                }
                catch
                {
                    if (!m_isStarted)
                    {
                        break;
                    }
                }
            }
        }
    }

    sealed class WsHttpMessageProcessor : IDisposable, IWsTransportMessageProcessor
    {
        // Fields
        _Bind.RequestContext       m_context;
        private WsServiceEndpoints m_serviceEndpoints;

        //private const int ReadPayload = 0x800;

        /// <summary>
        /// HttpProcess()
        ///     Summary:
        ///         Main Http processor class.
        /// </summary>
        /// <param name="serviceEndpoints">A collection of service endpoints.</param>
        /// <param name="s">
        /// Socket s
        /// </param>
        public WsHttpMessageProcessor(WsServiceEndpoints serviceEndpoints, _Bind.RequestContext context)
        {
            m_context = context;
            m_serviceEndpoints = serviceEndpoints;
        }

        void Dispose(bool disposing)
        {
            if(disposing)
            {
            }
        }

        /// <summary>
        /// Releases all resources used by the HttpProcess object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~WsHttpMessageProcessor()
        {
            Dispose(false);
        }

        /// <summary>
        /// Http servers message processor. This method reads a message from a socket and calls downstream
        /// processes that parse the request, dispatch to a method and returns a response.
        /// </summary>
        /// <remarks>The parameters should always be set to null. See IWsTransportMessageProcessor for details.</remarks>
        public void ProcessRequest()
        {
            // Process the message
            if (m_context.Message != null)
            {
                WsMessage response = ProcessRequestMessage(m_context.Message);

                m_context.Reply(response);
            }
        }

        /// <summary>
        /// Parses a transport message and builds a header object and envelope document then calls processRequest
        /// on a service endpoint.
        /// </summary>
        /// <param name="soapRequest">WsRequestMessage object containing a raw soap message or mtom soap request.</param>
        /// <returns>WsResponseMessage object containing the soap response returned from a service endpoint.</returns>
        private WsMessage ProcessRequestMessage(WsMessage soapRequest)
        {
            // Now check for implementation specific service endpoints.
            IWsServiceEndpoint serviceEndpoint = null;
            string             endpointAddress;
            WsWsaHeader        header = soapRequest.Header;

            // If this is Uri convert it
            if (header.To.IndexOf("urn") == 0 || header.To.IndexOf("http") == 0)
            {

                // Convert to address to Uri
                Uri toUri;
                try
                {
                    toUri = new Uri(header.To);
                }
                catch
                {
                    System.Ext.Console.Write("Unsupported Header.To Uri format: " + header.To);
                    return WsFault.GenerateFaultResponse(header, WsFaultType.ArgumentException, "Unsupported Header.To Uri format", m_context.Version);
                }

                // Convert the to address to a Urn:uuid if it is an Http endpoint
                if (toUri.Scheme == "urn")
                    endpointAddress = toUri.AbsoluteUri;
                else if (toUri.Scheme == "http")
                {
                    endpointAddress = "urn:uuid:" + toUri.AbsoluteUri.Substring(1);
                }
                else
                    endpointAddress = header.To;
            }
            else
                endpointAddress = "urn:uuid:" + header.To;

            // Look for a service at the requested endpoint that contains an operation matching the Action            
            IWsServiceEndpoint ep = m_serviceEndpoints[endpointAddress];

            if(ep != null)
            {
                if (ep.ServiceOperations[header.Action] != null)
                {
                    serviceEndpoint = ep;
                }
                else
                {
                    ep = m_serviceEndpoints[0]; // mex endpoint

                    if (ep.ServiceOperations[header.Action] != null)
                    {
                        serviceEndpoint = ep;
                    }
                }
            }
            
            // If a matching service endpoint is found call operation
            if (serviceEndpoint != null)
            {
                // Process the request
                WsMessage response;
                try
                {
                    response = serviceEndpoint.ProcessRequest(soapRequest);
                }
                catch (WsFaultException e)
                {
                    return WsFault.GenerateFaultResponse(e, m_context.Version);
                }
                catch (Exception e)
                {
                    return WsFault.GenerateFaultResponse(header, WsFaultType.Exception, e.ToString(), m_context.Version);
                }

                return response;
            }

            // Unreachable endpoint requested. Generate fault response
            return WsFault.GenerateFaultResponse(header, WsFaultType.WsaDestinationUnreachable, "Unknown service endpoint", m_context.Version);
        }

    }
}


