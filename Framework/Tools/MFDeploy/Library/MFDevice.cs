// Portions Copyright (c) Secret Labs LLC.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using _DBG = Microsoft.SPOT.Debugger;
using _WP = Microsoft.SPOT.Debugger.WireProtocol;
using System.Threading;

namespace Microsoft.NetMicroFramework.Tools.MFDeployTool.Engine
{
    [Flags]
    public enum EraseOptions
    {
        Deployment  = 0x01,
        UserStorage = 0x02,
        FileSystem  = 0x04,
    }

    public enum PingConnectionType
    {
        TinyCLR,
        TinyBooter,
        NoConnection,
    }

    public class DebugOutputEventArgs : EventArgs
    {
        private string m_txt;

        public DebugOutputEventArgs(string text)
        {
            m_txt = text;
        }

        public string Text { get{ return m_txt; } }
    }

    public class OemMonitorInfo
    {
        public OemMonitorInfo(_WP.Commands.Monitor_OemInfo.Reply reply)
        {
            m_releaseInfo = reply.m_releaseInfo;
        }
        
        public Version Version
        {
            get { return m_releaseInfo.Version; }
        }
        
        public string OemString
        {
            get { return m_releaseInfo.Info; }
        }

        public override string ToString()
        {
            return String.Format("Bootloader build info: {0}\nVersion {1}\n", OemString, Version);
        }

        public bool Valid
        {
            get { return true; }
        }

        private _DBG.WireProtocol.ReleaseInfo m_releaseInfo;
    }
    
    public class MFDevice : IDisposable
    {
        private _DBG.Engine m_eng;
        private _DBG.PortDefinition m_port;
        private _DBG.PortDefinition m_portTinyBooter;
        private bool disposed;

        public delegate void OnProgressHandler(long value, long total, string status);
        public event OnProgressHandler OnProgress;
        public AutoResetEvent EventCancel = new AutoResetEvent(false);

        private MFDevice()
        {
        }

        ~MFDevice()
        {
            Dispose(false);
        }

        public _DBG.Engine DbgEngine { get { return m_eng; } }

        private void OnNoiseHandler(byte[] data, int index, int count)
        {
            if (OnDebugText != null)
            {
                OnDebugText(this, new DebugOutputEventArgs(System.Text.ASCIIEncoding.ASCII.GetString(data, index, count)));
            }
        }
        private void OnMessage(_DBG.WireProtocol.IncomingMessage msg, string text)
        {
            if (OnDebugText != null)
            {
                OnDebugText(this, new DebugOutputEventArgs(text));
            }
        }
        private void PrepareForDeploy(ArrayList blocks)
        {
            const uint c_DeploySector = _WP.Commands.Monitor_FlashSectorMap.c_MEMORY_USAGE_DEPLOYMENT;
            const uint c_SectorUsageMask = _WP.Commands.Monitor_FlashSectorMap.c_MEMORY_USAGE_MASK;

            bool fEraseDeployment     = false;

            // if vsdebug is not enabled then we cannot write/erase
            if (!IsClrDebuggerEnabled())
            {
                if (OnProgress != null) OnProgress(0, 1, Properties.Resources.StatusConnectingToTinyBooter);

                // only check for signature file if we are uploading firmware
                if (!ConnectToTinyBooter()) throw new MFDeviceNoResponseException();
            }

            _WP.Commands.Monitor_FlashSectorMap.Reply map = m_eng.GetFlashSectorMap();

            if (map == null) throw new MFDeviceNoResponseException();

            foreach (_DBG.SRecordFile.Block bl in blocks)
            {
                foreach (_WP.Commands.Monitor_FlashSectorMap.FlashSectorData sector in map.m_map)
                {
                    if (sector.m_address == bl.address)
                    {
                        // only support writing with CLR to the deployment sector and RESERVED sector (for digi)
                        if (c_DeploySector == (c_SectorUsageMask & sector.m_flags))
                        {
                            fEraseDeployment        = true;
                        }
                        else
                        {
                            if (m_eng.ConnectionSource != _DBG.ConnectionSource.TinyBooter)
                            {
                                if (OnProgress != null) OnProgress(0, 1, Properties.Resources.StatusConnectingToTinyBooter);

                                // only check for signature file if we are uploading firmware
                                if (!ConnectToTinyBooter()) throw new MFDeviceNoResponseException();
                            }
                        }
                        break;
                    }
                }
            }
            if (fEraseDeployment)
            {
                this.Erase(EraseOptions.Deployment);
            }
            else if(m_eng.ConnectionSource != _DBG.ConnectionSource.TinyBooter) 
            {
                //if we are not writing to the deployment sector then assure that we are talking with TinyBooter
                ConnectToTinyBooter();
            }
            if (m_eng.ConnectionSource == _DBG.ConnectionSource.TinyCLR)
            {
                m_eng.PauseExecution();
            }
        }

        internal MFDevice(_DBG.PortDefinition port, _DBG.PortDefinition tinyBooterPort)
        {
            m_port = port;
            m_portTinyBooter = tinyBooterPort;
        }

        internal bool Connect( int timeout_ms, bool tryConnect )
        {
            // to use user cancel event, so that cancel button is more responsive
            int retries = timeout_ms/100;
            int loops = 1;
            
            if (retries == 0) retries = 1;

            if (m_portTinyBooter != null && m_port.UniqueId != m_portTinyBooter.UniqueId)
            {
                retries /= 2;
                loops   =  2;
            }
            for (int i = 0; i < loops; i++)
            {
                _DBG.PortDefinition pd = i == 0 ? m_port : m_portTinyBooter;

                if (EventCancel.WaitOne(0, false)) throw new MFUserExitException();

                try
                {
                    if (m_eng == null)
                    {
                        m_eng = new _DBG.Engine(pd);

                        m_eng.OnNoise += new _DBG.NoiseEventHandler(OnNoiseHandler);
                        m_eng.OnMessage += new _DBG.MessageEventHandler(OnMessage);
                    }

                    if (!m_eng.IsConnected)
                    {
                        m_eng.Start();
                        if (tryConnect)
                        {
                            for (int j = retries; j > 0; j-=5)
                            {
                                if (m_eng.TryToConnect(5, 100, true, _DBG.ConnectionSource.Unknown))
                                {
                                    //UNLOCK DEVICE in secure way?
                                    m_eng.UnlockDevice(m_data);
                                    break;
                                }

                                if (EventCancel.WaitOne(0, false)) throw new MFUserExitException();
                            }
                            if (m_eng.IsConnected)
                            {
                                break;
                            }
                            else
                            {
                                Disconnect();
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (MFUserExitException)
                {
                    Disconnect();
                    throw;
                }
                catch
                {
                    Disconnect();
                }
            }
            return (m_eng != null && (!tryConnect || m_eng.IsConnected));
        }
        internal bool Disconnect()
        {
            if (m_eng != null)
            {
                m_eng.OnNoise   -= new _DBG.NoiseEventHandler(OnNoiseHandler);
                m_eng.OnMessage -= new _DBG.MessageEventHandler(OnMessage);

                m_eng.Stop();
                m_eng = null;
            }
            return true;
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // release managed components
                    Disconnect();
                }
                
                disposed = true;
            }
        }

        private bool IsClrDebuggerEnabled()
        {
            try
            {
                if (m_eng.IsConnectedToTinyCLR)
                {
                    return (m_eng.Capabilities.SourceLevelDebugging);
                }
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// Event for notifying user of debug text coming from the device.
        /// </summary>
        public event EventHandler<DebugOutputEventArgs> OnDebugText;
        /// <summary>
        /// Standard Dispose method for releasing resources such as the connection to the device.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Attempt to establish a connection with TinyBooter (with reboot if necessary)
        /// </summary>
        /// <returns>true connection was made, false otherwise</returns>
        public bool ConnectToTinyBooter()
        {
            bool ret = false;

            if (m_eng == null)
            {
                _DBG.PortDefinition pd = m_portTinyBooter;

                try
                {
                    if (m_eng == null)
                    {
                        m_eng = new _DBG.Engine(pd);

                        m_eng.OnNoise += new _DBG.NoiseEventHandler(OnNoiseHandler);
                        m_eng.OnMessage += new _DBG.MessageEventHandler(OnMessage);

                        m_eng.Start();
                        m_eng.TryToConnect(5, 100, true, _DBG.ConnectionSource.Unknown);
                    }
                }
                catch
                {
                }
            }


            if (m_eng != null)
            {
                if (m_eng.ConnectionSource == _DBG.ConnectionSource.TinyBooter) return true;

                m_eng.RebootDevice(_DBG.Engine.RebootOption.EnterBootloader);

                // tinyBooter is only com port so
                if (m_port is _DBG.PortDefinition_Tcp)
                {
                    Disconnect();

                    m_port = m_portTinyBooter;

                    // digi takes forever to reset
                    if (!Connect(60000, true))
                    {
                        Console.WriteLine(Properties.Resources.ErrorUnableToConnectToTinyBooterSerial);
                        return false;
                    }
                }
                bool fConnected = false;
                for(int i = 0; i<40; i++)
                {
                    if (EventCancel.WaitOne(0, false)) throw new MFUserExitException();

                    if (fConnected = m_eng.TryToConnect(0, 500, true, _DBG.ConnectionSource.Unknown))
                    {
                        _WP.Commands.Monitor_Ping.Reply reply = m_eng.GetConnectionSource();
                        ret = (reply.m_source == _WP.Commands.Monitor_Ping.c_Ping_Source_TinyBooter);

                        break;
                    }
                }
                if(!fConnected)
                {
                    Console.WriteLine(Properties.Resources.ErrorUnableToConnectToTinyBooter);
                }
            }
            return ret;
        }
        
        /// <summary>
        /// Erases the deployment sectors of the connected .Net Micro Framework device
        /// </summary>
        /// <param name="options">Identifies which areas are to be erased, if no options are given, all 
        /// user sectors will be erased.
        /// </param>
        /// <returns>Returns false if the erase fails, true otherwise
        /// Possible exceptions: MFUserExitException, MFDeviceNoResponseException
        /// </returns>
        public bool Erase(params EraseOptions[] options)
        {
            bool ret = false;
            bool fReset = false;
            if (m_eng == null) throw new MFDeviceNoResponseException();
            EraseOptions optionFlags = 0;

            if (options == null || options.Length == 0)
            {
                optionFlags = (EraseOptions.Deployment | EraseOptions.FileSystem | EraseOptions.UserStorage);
            }
            else
            {
                foreach (EraseOptions opt in options)
                {
                    optionFlags |= opt;
                }
            }

            if (!m_eng.TryToConnect(5, 100, true, _DBG.ConnectionSource.Unknown))
            {
                throw new MFDeviceNoResponseException();
            }

            if (!IsClrDebuggerEnabled())
            {
                fReset = (Ping() == PingConnectionType.TinyCLR);
                ConnectToTinyBooter();
            }

            _WP.Commands.Monitor_FlashSectorMap.Reply reply = m_eng.GetFlashSectorMap();

            if (reply == null) throw new MFDeviceNoResponseException();

            _WP.Commands.Monitor_Ping.Reply ping = m_eng.GetConnectionSource();

            ret = true;


            long total = 0;
            long value = 0;

            bool isConnectedToCLR = ((ping != null) && (ping.m_source == _WP.Commands.Monitor_Ping.c_Ping_Source_TinyCLR));


            if (isConnectedToCLR)
            {
                m_eng.PauseExecution();
            }
            
            List<_WP.Commands.Monitor_FlashSectorMap.FlashSectorData> eraseSectors = new List<_WP.Commands.Monitor_FlashSectorMap.FlashSectorData>();

            foreach (_WP.Commands.Monitor_FlashSectorMap.FlashSectorData fsd in reply.m_map)
            {
                if (EventCancel.WaitOne(0, false)) throw new MFUserExitException();

                switch (fsd.m_flags & _WP.Commands.Monitor_FlashSectorMap.c_MEMORY_USAGE_MASK)
                {
                    case _WP.Commands.Monitor_FlashSectorMap.c_MEMORY_USAGE_DEPLOYMENT:
                        if (EraseOptions.Deployment == (optionFlags & EraseOptions.Deployment))
                        {
                            eraseSectors.Add(fsd);
                            total++;
                        }
                        break;

                    case _WP.Commands.Monitor_FlashSectorMap.c_MEMORY_USAGE_STORAGE_A:
                    case _WP.Commands.Monitor_FlashSectorMap.c_MEMORY_USAGE_STORAGE_B:
                        if (EraseOptions.UserStorage == (optionFlags & EraseOptions.UserStorage))
                        {
                            eraseSectors.Add(fsd);
                            total++;
                        }
                        break;

                    case _WP.Commands.Monitor_FlashSectorMap.c_MEMORY_USAGE_FS :
                        if (EraseOptions.FileSystem == (optionFlags & EraseOptions.FileSystem))
                        {
                            eraseSectors.Add(fsd);
                            total++;
                        }
                        break;
                }

            }


            foreach (_WP.Commands.Monitor_FlashSectorMap.FlashSectorData fsd in eraseSectors)
            {
                ret &= m_eng.EraseMemory(fsd.m_address, fsd.m_size);

                value++;

                if (OnProgress != null) OnProgress(value, total, string.Format(Properties.Resources.StatusEraseSector, fsd.m_address));
            }

            // reset if we specifically entered tinybooter for the erase
            if(fReset)
            {
                m_eng.ExecuteMemory(0);
            }
            // reboot if we are talking to the clr
            if (isConnectedToCLR)
            {
                if (OnProgress != null) OnProgress(0, 0, Properties.Resources.StatusRebooting);
                
                m_eng.RebootDevice(_DBG.Engine.RebootOption.RebootClrOnly);
                m_eng.ResumeExecution();
            }

            return ret;
        }
        /// <summary>
        /// Attempts to talk to the connected .Net Micro Framework device
        /// </summary>
        /// <returns>returns ConnectionType if the device was responsive, false otherwise</returns>
        public PingConnectionType Ping()
        {            
            PingConnectionType ret = PingConnectionType.NoConnection;
            if (m_eng != null)
            {
                _WP.Commands.Monitor_Ping.Reply reply = m_eng.GetConnectionSource();

                if( reply != null)
                {
                    switch(reply.m_source)
                    {
                        case _WP.Commands.Monitor_Ping.c_Ping_Source_TinyCLR:
                            ret = PingConnectionType.TinyCLR;
                            break;
                        case _WP.Commands.Monitor_Ping.c_Ping_Source_TinyBooter:
                            ret = PingConnectionType.TinyBooter;
                            break;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Gets TinyBooter's OEM-specified OEMInfo string and compile-time build version
        /// number. 
        /// </summary>
        /// <returns>returns an OemMonitorInfo object, or null if we are not connected or 
        /// connected to the CLR rather than TinyBooter.</returns>
        public OemMonitorInfo GetOemMonitorInfo()
        {
            if (m_eng == null || !m_eng.IsConnected || m_eng.IsConnectedToTinyCLR) return null;
            
            _WP.Commands.Monitor_OemInfo.Reply reply = m_eng.GetMonitorOemInfo();
            return reply == null ? null : new OemMonitorInfo(reply);
        }

        /// <summary>
        /// Attempts to deploy an SREC (.hex) file to the connected .Net Micro Framework device.  The 
        /// signatureFile is used to validate the image once it has been deployed to the device.  If 
        /// the signature does not match the image is erased.
        /// </summary>
        /// <param name="filePath">Full path to the SREC (.hex) file</param>
        /// <param name="signatureFile">Full path to the signature file (.sig) for the SREC file identified by the filePath parameter</param>
        /// <param name="entrypoint">Out parameter that is set to the entry point address for the given SREC file</param>
        /// <returns>Returns false if the deployment fails, true otherwise
        /// Possible exceptions: MFFileNotFoundException, MFDeviceNoResponseException, MFUserExitException
        /// </returns>
        public bool Deploy(string filePath, string signatureFile, ref uint entryPoint)
        {
            entryPoint = 0;

            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);
            if (m_eng == null) throw new MFDeviceNoResponseException();

            // make sure we know who we are talking to
            m_eng.TryToConnect(1, 100, true, _DBG.ConnectionSource.Unknown);

            bool sigExists = File.Exists(signatureFile);
            FileInfo fi = new FileInfo(filePath);

            ArrayList blocks = new ArrayList();
            entryPoint = _DBG.SRecordFile.Parse(filePath, blocks, sigExists? signatureFile: null);

            if (blocks.Count > 0)
            {
                long total = 0;
                long value = 0;

                for (int i = 0;i < blocks.Count;i++)
                {
                    total += (blocks[i] as _DBG.SRecordFile.Block).data.Length;
                }

                PrepareForDeploy(blocks);

                foreach (_DBG.SRecordFile.Block block in blocks)
                {
                    long len  = block.data.Length;
                    uint addr = block.address;

                    if (EventCancel.WaitOne(0, false)) throw new MFUserExitException();

                    block.data.Seek(0, SeekOrigin.Begin);

                    if (OnProgress != null)
                    {
                        OnProgress(0, total, string.Format(Properties.Resources.StatusEraseSector, block.address));
                    }

                    // the clr requires erase before writing
                    if (!m_eng.EraseMemory(block.address, (uint)len)) return false;

                    while(len > 0 )
                    {
                        if (EventCancel.WaitOne(0, false)) throw new MFUserExitException();

                        int buflen = len > 1024? 1024: (int)len;
                        byte[] data = new byte[buflen];

                        if (block.data.Read(data, 0, buflen) <= 0)  return false;

                        if (!m_eng.WriteMemory(addr, data)) return false;

                        value += buflen;
                        addr += (uint)buflen;
                        len  -= buflen;

                        if (OnProgress != null)
                        {
                            OnProgress(value, total, string.Format(Properties.Resources.StatusFlashing, fi.Name));
                        }
                    }
                    if (_DBG.ConnectionSource.TinyCLR != m_eng.ConnectionSource)
                    {
                        byte[] emptySig = new byte[128];

                        if (OnProgress != null) OnProgress(value, total, Properties.Resources.StatusCheckingSignature);

                        if (!m_eng.CheckSignature(((block.signature == null || block.signature.Length == 0)? emptySig: block.signature), 0))  throw new MFSignatureFailureException(signatureFile);
                    }
                }
            }

            return true;
        }
        /// <summary>
        /// Starts execution on the connected .Net Micro Framework device at the supplied address (parameter entrypoint).
        /// This method is generally used after the Deploy method to jump into the code that was deployed.
        /// </summary>
        /// <param name="entrypoint">Entry point address for execution to begin</param>
        /// <returns>Returns false if execution fails, true otherwise
        /// Possible exceptions: MFDeviceNoResponseException
        /// </returns>
        public bool Execute(uint entryPoint)
        {
            if (m_eng == null) throw new MFDeviceNoResponseException(); 
            
            _WP.Commands.Monitor_Ping.Reply reply = m_eng.GetConnectionSource();

            if (reply == null) throw new MFDeviceNoResponseException();

            // only execute if we are talking to the tinyBooter, otherwise reboot
            if (reply.m_source == _WP.Commands.Monitor_Ping.c_Ping_Source_TinyBooter)
            {
                return m_eng.ExecuteMemory(entryPoint);
            }
            else // if we are talking to the CLR then we simply did a deployment update, so reboot
            {
                m_eng.RebootDevice(_DBG.Engine.RebootOption.RebootClrOnly);
            }
            return true;
        }
        /// <summary>
        /// Reboots the connected .Net Micro Framework device
        /// Possible exceptions: MFDeviceNoResponseException
        /// </summary>
        /// <param name="coldBoot">Determines whether a cold/hardware reboot or a warm/software reboot is performed</param>
        public void Reboot(bool coldBoot)
        {
            if (m_eng == null) throw new MFDeviceNoResponseException(); 

            m_eng.RebootDevice(coldBoot? _DBG.Engine.RebootOption.NoReconnect: _DBG.Engine.RebootOption.RebootClrWaitForDebugger);

            if(!coldBoot)
            {
                bool fOK = false;
                try
                {
                    if(m_eng.TryToReconnect(true))
                    {
                        m_eng.ResumeExecution();
                        fOK = true;
                    }
                }
                catch
                {
                }

                if(!fOK)
                {
                    this.Disconnect();
                    this.Connect(1000, true);
                }
                
            }
        }
        /// <summary>
        /// Determines if the device is connected
        /// </summary>
        public bool IsConnected { get { return (m_eng != null ? m_eng.IsConnected : false); } }


        public interface IAppDomainInfo
        {
            string Name { get; }
            uint ID { get; }
            uint[] AssemblyIndicies { get; }
        }

        public interface IAssemblyInfo
        {
            string Name { get; }
            System.Version Version { get; }
            uint Index { get; }
            List<IAppDomainInfo> InAppDomains { get; }            
        }

        private class AppDomainInfo : IAppDomainInfo
        {
            private uint m_id;
            _WP.Commands.Debugging_Resolve_AppDomain.Reply m_reply;

            public AppDomainInfo(uint id, _WP.Commands.Debugging_Resolve_AppDomain.Reply reply)
            {
                m_id = id;
                m_reply = reply;
            }

            public string Name
            {
                get { return m_reply.Name; }
            }

            public uint ID
            {
                get { return m_id; }
            }

            public uint[] AssemblyIndicies
            {
                get { return m_reply.m_data; }
            }

        }

        private class AssemblyInfoFromResolveAssembly : IAssemblyInfo
        {
            private _WP.Commands.Debugging_Resolve_Assembly m_dra;
            private List<IAppDomainInfo> m_AppDomains = new List<IAppDomainInfo>();
            
            public AssemblyInfoFromResolveAssembly(_WP.Commands.Debugging_Resolve_Assembly dra)
            {
                m_dra = dra;
            }
            
            public string Name
            {
                get { return m_dra.m_reply.Name; }
            }
            
            public System.Version Version
            {
                get
                {
                    _WP.Commands.Debugging_Resolve_Assembly.Version draver = m_dra.m_reply.m_version;
                    return new System.Version(draver.iMajorVersion, draver.iMinorVersion, draver.iBuildNumber, draver.iRevisionNumber);
                }
            }
            
            public uint Index
            {
                get { return m_dra.m_idx; }
            }

            public List<IAppDomainInfo> InAppDomains
            {
                get { return m_AppDomains; }
            }

            public void AddDomain(IAppDomainInfo adi)
            {
                if ( adi != null )
                {
                    m_AppDomains.Add(adi);
                }
            }
        }


        public delegate void AppDomainAction(IAppDomainInfo adi);
        public void DoForEachAppDomain(AppDomainAction appDomainAction)
        {
            if (m_eng.Capabilities.AppDomains)
            {
                _WP.Commands.Debugging_TypeSys_AppDomains.Reply domainsReply = m_eng.GetAppDomains();
                if ( domainsReply != null )
                {
                    foreach (uint id in domainsReply.m_data)
                    {
                        _WP.Commands.Debugging_Resolve_AppDomain.Reply reply = m_eng.ResolveAppDomain(id);
                        if (reply != null)
                        {
                            appDomainAction(new AppDomainInfo(id, reply));
                        }
                    }                
                }
            }
        }

        public delegate void AssemblyAction(IAssemblyInfo ai);
        public void DoForEachAssembly(AssemblyAction assemblyAction)
        {
            List<IAppDomainInfo> theDomains = new List<IAppDomainInfo>();
            
            this.DoForEachAppDomain(
                delegate(IAppDomainInfo adi)
                {
                    theDomains.Add(adi);
                }
            );

            _WP.Commands.Debugging_Resolve_Assembly[] reply = m_eng.ResolveAllAssemblies();
            if ( reply != null )
                foreach (_WP.Commands.Debugging_Resolve_Assembly resolvedAssm in reply)
                {
                    AssemblyInfoFromResolveAssembly ai = new AssemblyInfoFromResolveAssembly(resolvedAssm);
                    
                    foreach (IAppDomainInfo adi in theDomains)
                    {
                        if (Array.IndexOf<uint>(adi.AssemblyIndicies, ai.Index) != -1 )
                        {
                            ai.AddDomain(adi);
                        }
                    }
                    
                    assemblyAction(ai);
                }
        }

        public interface IMFDeviceInfo
        {
            bool                        Valid { get; }
            System.Version              HalBuildVersion { get; }
            string                      HalBuildInfo { get; }
            byte                        OEM { get; }
            byte                        Model { get; }
            ushort                      SKU { get; }
            string                      ModuleSerialNumber { get; }
            string                      SystemSerialNumber { get; }
            System.Version              ClrBuildVersion { get; }
            string                      ClrBuildInfo { get; }
            System.Version              TargetFrameworkVersion { get; }
            System.Version              SolutionBuildVersion { get; }
            string                      SolutionBuildInfo { get; }
            IAppDomainInfo[]            AppDomains { get; }
            IAssemblyInfo[]             Assemblies { get; }
        }

        private class MFDeviceInfoImpl : IMFDeviceInfo
        {
            private MFDevice m_self;

            private bool m_fValid = false;            
            private List<IAppDomainInfo> m_Domains = new List<IAppDomainInfo>();
            private List<IAssemblyInfo> m_AssemblyInfos = new List<IAssemblyInfo>();
            
            
            public MFDeviceInfoImpl(MFDevice dev)
            {
                m_self = dev;

                if ( !Dbg.IsConnectedToTinyCLR ) return;

                m_self.DoForEachAppDomain(
                    delegate(IAppDomainInfo adi)
                    {
                        m_Domains.Add(adi);
                    }
                );
               
                m_self.DoForEachAssembly(
                    delegate(MFDevice.IAssemblyInfo ai) 
                    {
                        m_AssemblyInfos.Add(ai);
                    }
                );                
                
                m_fValid = true;
            }

            private _DBG.Engine Dbg { get { return m_self.DbgEngine; } }
            
            public bool Valid { get { return m_fValid; } }

            public System.Version HalBuildVersion
            {
                get { return Dbg.Capabilities.HalSystemInfo.halVersion; }
            }
            
            public string HalBuildInfo
            {
                get { return Dbg.Capabilities.HalSystemInfo.halVendorInfo; }
            }
            
            public byte OEM
            {
                get { return Dbg.Capabilities.HalSystemInfo.oemCode; }
            }
            
            public byte Model
            {
                get { return Dbg.Capabilities.HalSystemInfo.modelCode; }
            }
            
            public ushort SKU
            {
                get { return Dbg.Capabilities.HalSystemInfo.skuCode; }
            }
            
            public string ModuleSerialNumber
            {
                get { return Dbg.Capabilities.HalSystemInfo.moduleSerialNumber; }
            }
            
            public string SystemSerialNumber
            {
                get { return Dbg.Capabilities.HalSystemInfo.systemSerialNumber; }
            }

            public System.Version ClrBuildVersion
            {
                get { return Dbg.Capabilities.ClrInfo.clrVersion; }
            }

            public string ClrBuildInfo
            {
                get { return Dbg.Capabilities.ClrInfo.clrVendorInfo; }
            }

            public System.Version TargetFrameworkVersion
            {
                get { return Dbg.Capabilities.ClrInfo.targetFrameworkVersion; }
            }

            public System.Version SolutionBuildVersion
            {
                get { return Dbg.Capabilities.SolutionReleaseInfo.solutionVersion; }
            }

            public string SolutionBuildInfo
            {
                get { return Dbg.Capabilities.SolutionReleaseInfo.solutionVendorInfo; }
            }

            public IAppDomainInfo[] AppDomains
            {
                get { return m_Domains.ToArray(); }
            }

            public IAssemblyInfo[] Assemblies
            {
                get { return m_AssemblyInfos.ToArray(); }
            }
        }
        
        private IMFDeviceInfo m_deviceInfoCache = null;
        public IMFDeviceInfo MFDeviceInfo
        {
            get
            {
                if ( m_deviceInfoCache == null )
                {
                    m_deviceInfoCache = new MFDeviceInfoImpl(this);
                }
                return m_deviceInfoCache;
            }
        }
                
        private readonly byte[] m_data =
                    {
                        67, 111, 112, 121, 114, 105, 103, 104, 116, 32, 50, 48, 48, 51, 13, 10,
                        77, 105, 99, 114, 111, 115, 111, 102, 116, 32, 67, 111, 114, 112, 13, 10,
                        49, 32, 77, 105, 99, 114, 111, 115, 111, 102, 116, 32, 87, 97, 121, 13,
                        10, 82, 101, 100, 109, 111, 110, 100, 44, 32, 87, 65, 13, 10, 57, 56,
                        48, 53, 50, 45, 54, 51, 57, 57, 13, 10, 85, 46, 83, 46, 65, 46,
                        13, 10, 65, 108, 108, 32, 114, 105, 103, 104, 116, 115, 32, 114, 101, 115,
                        101, 114, 118, 101, 100, 46, 13, 10, 77, 73, 67, 82, 79, 83, 79, 70,
                        84, 32, 67, 79, 78, 70, 73, 68, 69, 78, 84, 73, 65, 76, 13, 10,
                        55, 231, 64, 0, 118, 157, 50, 129, 173, 196, 117, 75, 87, 255, 238, 223,
                        181, 114, 130, 29, 130, 170, 89, 70, 194, 108, 71, 230, 192, 61, 9, 29,
                        216, 23, 196, 204, 21, 89, 242, 196, 143, 255, 49, 65, 179, 224, 237, 213,
                        15, 250, 92, 181, 77, 10, 200, 21, 219, 202, 181, 127, 64, 172, 101, 87,
                        166, 35, 162, 28, 70, 172, 138, 40, 35, 215, 207, 160, 195, 119, 187, 95,
                        239, 213, 127, 201, 46, 15, 60, 225, 19, 252, 227, 17, 211, 80, 209, 52,
                        74, 122, 115, 2, 144, 20, 153, 241, 244, 57, 139, 10, 57, 65, 248, 204,
                        149, 252, 17, 159, 244, 11, 186, 176, 59, 187, 167, 107, 83, 163, 62, 122
                    };

    }
}
