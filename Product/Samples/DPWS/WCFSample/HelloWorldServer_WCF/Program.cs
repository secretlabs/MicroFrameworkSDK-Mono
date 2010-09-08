using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Discovery;
using System.Net;

namespace ServiceHelloWCF
{
    class Program
    {
        static void Main(string[] args)
        {
            IPHostEntry entry = Dns.GetHostEntry("");
            string addr = "localhost";

            for (int i = 0; i < entry.AddressList.Length; i++)
            {
                if (entry.AddressList[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    addr = entry.AddressList[i].ToString();
                    break;
                }
            }

            Uri baseAddress = new Uri("http://" + addr + ":8084/319D0A4D-2253-47DC-AC4A-C1951FF6667D");

            ServiceHost serviceHost = new ServiceHost(typeof(ServiceHelloWCF), baseAddress);
            try
            {
                ServiceEndpoint wsEndpoint = serviceHost.AddServiceEndpoint(typeof(IServiceHelloWCF), new WSHttpBinding(SecurityMode.None), string.Empty);
                EndpointDiscoveryBehavior endpointDiscoveryBehavior = new EndpointDiscoveryBehavior();

                // Add the discovery behavior to the endpoint.
                wsEndpoint.Behaviors.Add(endpointDiscoveryBehavior);
                
                // Make the service discoverable over UDP multicast
                serviceHost.Description.Behaviors.Add(new ServiceDiscoveryBehavior());
                serviceHost.AddServiceEndpoint(new UdpDiscoveryEndpoint(DiscoveryVersion.WSDiscovery11));

                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                smb.HttpGetUrl = baseAddress;
                serviceHost.Description.Behaviors.Add(smb);

                serviceHost.Open();

                Console.WriteLine("Hello World WCF Service started at {0}", baseAddress);
                Console.WriteLine();
                Console.WriteLine("Press <ENTER> to terminate the service.");
                Console.WriteLine();
                Console.ReadLine();

                serviceHost.Close();
            }
            catch (CommunicationException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (TimeoutException e)
            {
                Console.WriteLine(e.Message);
            }

            if (serviceHost.State != CommunicationState.Closed)
            {
                Console.WriteLine("Aborting service...");
                serviceHost.Abort();
            }

        }
    }
}
