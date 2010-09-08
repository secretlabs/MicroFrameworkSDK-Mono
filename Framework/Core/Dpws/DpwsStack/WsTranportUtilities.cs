using System;
using System.Collections;
using System.Net;
using System.Threading;
using Ws.Services.Utilities;

using Microsoft.SPOT.Net.NetworkInformation;
using Microsoft.SPOT;

namespace Ws.Services.Transport
{
    /// <summary>
    /// Interface used to abstract a message processor from a specific transport service.
    /// WsThreadManager uses this insterface when calling a message processor.
    /// </summary>
    internal interface IWsTransportMessageProcessor
    {
        /// <summary>
        /// Method prototype that defines a transports message processing method.
        /// </summary>
        void ProcessRequest();
    }

    /// <summary>
    /// Class used to manage Udp processing threads.
    /// </summary>
    /// <remarks>
    /// This class is used to create processing threads that processing request messages. The class monitors
    /// a max thread count and controllable by a consumer of this class. The default value is 2.
    /// </remarks>
    internal class WsThreadManager
    {
        /// <summary>
        /// Nested class containing a processing stub. The thread manager creates an instance of this class passing
        /// the parameters required to process a message. When start is called on a thread created by the thread manager
        /// the processing stub method creates an instance of a message processor and calls the processing method.
        /// On completion this class decrements the thread managers current thread count.
        /// </summary>
        internal class MessageProcessor
        {
            private WsThreadManager m_threadManager;
            private IWsTransportMessageProcessor m_processor;

            /// <summary>
            /// Create an instance of a message procesor class.
            /// </summary>
            /// <param name="threadManager">
            /// A refernce to the WsThreadManager class that created this class. This parameter is stored
            /// locally an used to decrement the thread managers current thread count.
            /// </param>
            /// <param name="processor">A instance of an object used to process a request. This instance
            /// is the ThreadStart instance.
            /// </param>
            /// <param name="transportName">String used to display the type of thread being created.</param>
            /// <param name="soapMessage">A byte array containing the soap message to process.</param>
            /// <param name="remoteEP">A IPEndPoint that identifies who sent the request.</param>
            /// <param name="messageCheck">
            /// A optional WsMessageCheck instance. If not null the downstream
            /// processor will ignor this message if it has been previously received.
            /// </param>
            public MessageProcessor(WsThreadManager threadManager, IWsTransportMessageProcessor processor)
            {
                m_threadManager = threadManager;
                m_processor = processor;
            }

            /// <summary>
            /// Stub processing method that creates an instance of a class used to process a message. Calls the
            /// Process Message on that class and decrements the current thread count on completion.
            /// </summary>
            public void ProcessRequest()
            {
                try
                {
                    // Call soap processor
                    m_processor.ProcessRequest();
                }
                finally
                {
                    // Decrement the thread managers current thread count
                    System.Ext.Console.Write("Process on " + m_threadManager.m_transportName + " thread number " + m_threadManager.m_threadCount + " is complete.");
                    Interlocked.Decrement(ref m_threadManager.m_threadCount);

                    m_threadManager.ThreadEvent.Set();
                }
            }
        }

        private int m_threadCount;
        private int m_maxThreadCount;
        private String m_transportName;

        public AutoResetEvent ThreadEvent;

        /// <summary>
        /// Creates an instance of a WsThreadManager class.
        /// </summary>
        /// <param name="maxThreadCount">
        /// An integer containing the maximum number of threads allowed by this thread manager.
        /// </param>
        public WsThreadManager(int maxThreadCount, String transportName)
        {
            m_maxThreadCount = maxThreadCount;
            m_transportName  = transportName;
            m_threadCount    = 0;
            ThreadEvent      = new AutoResetEvent(false);
        }

        /// <summary>
        /// Method creates an instance of a message processor and returns a thread ready to run the process.
        /// </summary>
        /// <param name="processor">
        /// A instance of a transport message processor derived from IWsTransportMessageProcessor. This parameter
        /// is passed to the message processor. The message processor calls the ProcessRequest method on this
        /// class from the message processor stub.
        /// </param>
        /// <remarks>
        /// If the current thread count is less than the maximum number of threads allowed, this method
        /// creates a new instance of a MessageProcessor nested class and creates a new thread that
        /// calls the MessageProcessor.ProcessRequest method. If the max thread count is reached
        /// this method returns null.
        /// </remarks>
        public void StartNewThread(IWsTransportMessageProcessor processor)
        {
            // Check thread count
            if (m_threadCount >= m_maxThreadCount)
            {
                System.Ext.Console.Write("Max thread count " + m_threadCount + " exceeded. Request ignored.");
                return;
            }

            // Increment the thread count
            Interlocked.Increment(ref m_threadCount);
            System.Ext.Console.Write("New " + m_transportName + " thread number: " + m_threadCount);

            Thread t = new Thread(new ThreadStart(new MessageProcessor(this, processor).ProcessRequest));

            if (t != null)
            {
                t.Start();
            }
        }

        /// <summary>
        /// A property containing the maximun number of threads this thread manager allows.
        /// </summary>
        public int MaxThreadCount { get { return m_maxThreadCount; } set { m_maxThreadCount = value; } }

        /// <summary>
        /// Flag indicating whether or not processing threads are availble.
        /// </summary>
        public bool ThreadsAvailable { get { return m_threadCount < m_maxThreadCount ? true : false; } }
    }

    /// <summary>
    /// Class used to check for repeat messages.
    /// </summary>
    /// <remarks>Udp messages are typically repeated to insure delivery. This can cause excessive processing
    /// and require an applicaotin to do excessive testing of types to determine if duplicates exists.
    /// This class eliminates this potential problem by providing a function used to check for duplicates.
    /// See the IsDuplicate method for details. Use the MaxTestQSize property to set the number of message
    /// identifiers to queue for the test. The default is 20;
    /// </remarks>
    public class WsMessageCheck
    {
        private ArrayList m_headerSamples;
        private int m_maxQSize;

        public WsMessageCheck(int maxQSize)
        {
            m_headerSamples = new ArrayList();
            m_maxQSize = maxQSize;
        }

        public WsMessageCheck() : this(20)
        {
        }

        /// <summary>
        /// Check for a duplicate request message.
        /// </summary>
        /// <param name="messageID">A string containing the message ID obtained from a WsaWsHeader object.</param>
        /// <param name="remoteEndpoint">A string containing a remote endpoint address obtained from a receiving socket.</param>
        /// <returns>True is a match is found, false if no match is found.</returns>
        public bool IsDuplicate(string messageID, string remoteEndpoint)
        {
            String msg = messageID + remoteEndpoint;

            lock (m_headerSamples)
            {
                if (m_headerSamples.Contains(msg))
                {
                    return true;
                }
                else
                {
                    m_headerSamples.Add(msg);

                    if (m_headerSamples.Count >= m_maxQSize)
                    {
                        m_headerSamples.RemoveAt(0);
                    }

                    return false;
                }
            }
        }
    }

    /// <summary>
    /// Class used to provide basic network services.
    /// </summary>
    internal static class WsNetworkServices
    {
        /// <summary>
        /// Method used to get the local IPV4 address.
        /// </summary>
        /// <returns>
        /// A string representing the local IPV4 address, null if a valid IPV4 addresss is not aquired.
        /// </returns>
        public static string GetLocalIPV4Address()
        {
            // Special conditions are implemented here because of a ptoblem with GetHostEntry
            // on the digi device and NetworkInterface from the emulator.
            // In the emulator we must use GetHostEntry.
            // On the device and Windows NetworkInterface works and is preferred.
            try
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

                int cnt = interfaces.Length;
                for(int i=0; i<cnt; i++)
                {
                    NetworkInterface ni = interfaces[i];

                    if(ni.IPAddress != "0.0.0.0" && ni.SubnetMask != "0.0.0.0")
                    {
                        return ni.IPAddress;
                    }
                }
            }
            catch
            {
                System.Ext.Console.Write("NetworkInterface.GetAllNetworkInterfaces failed. Attempting GetHostEntry.");
            }

            try
            {
                IPAddress localAddress = null;
                IPHostEntry hostEntry = Dns.GetHostEntry("");
                
                int cnt = hostEntry.AddressList.Length;
                for (int i = 0; i < cnt; ++i)
                {
                    if ((localAddress = hostEntry.AddressList[i]) != null)
                    {
                        return localAddress.ToString();
                    }
                }
            }
            catch
            {
                System.Ext.Console.Write("GetHostEntry failed attempting on GetHostEntry.");
            }

            return "";
        }

        public static UInt32 GetLocalIPV4AddressValue()
        {
            byte[] ipBytes = IPAddress.Parse(GetLocalIPV4Address()).GetAddressBytes();
            return (UInt32)((ipBytes[0] + (ipBytes[1] << 0x08) + (ipBytes[2] << 0x10) + (ipBytes[3] << 0x18)) & 0xFFFFFFFF);
        }
    }
}


