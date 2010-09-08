////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Management;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.SPOT.Debugger
{
    // This is an internal object implementing IAsyncResult with fields
    // for all of the relevant data necessary to complete the IO operation.
    // This is used by AsyncFSCallback and all of the async methods.
    unsafe internal class AsyncFileStream_AsyncResult : IAsyncResult
    {
        private unsafe static readonly IOCompletionCallback s_callback = new IOCompletionCallback( DoneCallback );

        internal AsyncCallback     m_userCallback;
        internal Object            m_userStateObject;
        internal ManualResetEvent  m_waitHandle;

        internal GCHandle          m_bufferHandle;    // GCHandle to pin byte[].
        internal bool              m_bufferIsPinned;  // Whether our m_bufferHandle is valid.

        internal bool              m_isWrite;         // Whether this is a read or a write
        internal bool              m_isComplete;
        internal bool              m_EndXxxCalled;    // Whether we've called EndXxx already.
        internal int               m_numBytes;        // number of bytes read OR written
        internal int               m_errorCode;
        internal NativeOverlapped* m_overlapped;
        
        internal AsyncFileStream_AsyncResult( AsyncCallback userCallback, Object stateObject, bool isWrite )
        {
            m_userCallback    = userCallback;
            m_userStateObject = stateObject;
            m_waitHandle      = new ManualResetEvent( false );

            m_isWrite         = isWrite;

            Overlapped overlapped = new Overlapped( 0, 0, IntPtr.Zero, this );

            m_overlapped = overlapped.Pack( s_callback, null );            
        }

        public virtual Object AsyncState
        {
            get { return m_userStateObject; }
        }

        public bool IsCompleted
        {
            get { return m_isComplete;  }
            set { m_isComplete = value; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return m_waitHandle; }
        }

        public bool CompletedSynchronously
        {
            get { return false; }
        }

        internal void SignalCompleted()
        {
            AsyncCallback userCallback = null;

            lock(this)
            {
                if(m_isComplete == false)
                {
                    userCallback = m_userCallback;

                    ManualResetEvent wh = m_waitHandle;
                    if(wh != null && wh.Set() == false)
                    {
                        Native.ThrowIOException( string.Empty );
                    }

                    // Set IsCompleted to true AFTER we've signalled the WaitHandle!
                    // Necessary since we close the WaitHandle after checking IsCompleted,
                    // so we could cause the SetEvent call to fail.
                    m_isComplete = true;

                    ReleaseMemory();
                }
            }

            if(userCallback != null)
            {
                userCallback( this );
            }
        }

        internal void WaitCompleted()
        {
            ManualResetEvent wh = m_waitHandle;
            if(wh != null)
            {
                if(m_isComplete == false)
                {
                    wh.WaitOne();
                    // There's a subtle race condition here.  In AsyncFSCallback,
                    // I must signal the WaitHandle then set _isComplete to be true,
                    // to avoid closing the WaitHandle before AsyncFSCallback has
                    // signalled it.  But with that behavior and the optimization
                    // to call WaitOne only when IsCompleted is false, it's possible
                    // to return from this method before IsCompleted is set to true.
                    // This is currently completely harmless, so the most efficient
                    // solution of just setting the field seems like the right thing
                    // to do.     -- BrianGru, 6/19/2000
                    m_isComplete = true;
                }
                wh.Close();
            }
        }

        internal NativeOverlapped* OverlappedPtr
        {
            get { return m_overlapped; }
        }

        internal unsafe void ReleaseMemory()
        {
            if(m_overlapped != null)
            {
                Overlapped.Free( m_overlapped );
                m_overlapped = null;
            }

            UnpinBuffer();
        }

        internal void PinBuffer( byte[] buffer )
        {
            m_bufferHandle   = GCHandle.Alloc( buffer, GCHandleType.Pinned );
            m_bufferIsPinned = true;
        }

        internal void UnpinBuffer()
        {
            if(m_bufferIsPinned)
            {
                m_bufferHandle.Free();
                m_bufferIsPinned = false;
            }
        }

        // this callback is called by a free thread in the threadpool when the IO operation completes.
        unsafe private static void DoneCallback( uint errorCode, uint numBytes, NativeOverlapped* pOverlapped )
        {
            if(errorCode == Native.ERROR_OPERATION_ABORTED)
            {
                numBytes  = 0;
                errorCode = 0;
            }

            // Unpack overlapped
            Overlapped overlapped = Overlapped.Unpack( pOverlapped );
            // Free the overlapped struct in EndRead/EndWrite.

            // Extract async result from overlapped
            AsyncFileStream_AsyncResult asyncResult = (AsyncFileStream_AsyncResult)overlapped.AsyncResult;


            asyncResult.m_numBytes  = (int)numBytes;
            asyncResult.m_errorCode = (int)errorCode;

            asyncResult.SignalCompleted();
        }
    }

    public class GenericAsyncStream : System.IO.Stream, IDisposable, WireProtocol.IStreamAvailableCharacters
    {
        protected SafeHandle m_handle;
        protected ArrayList m_outstandingRequests;

        protected GenericAsyncStream(SafeHandle handle)
        {
            System.Diagnostics.Debug.Assert(handle != null);

            m_handle = handle;

            if(ThreadPool.BindHandle( m_handle ) == false)
            {
                throw new IOException( "BindHandle Failed" );
            }

            m_outstandingRequests = ArrayList.Synchronized(new ArrayList());
        }

        ~GenericAsyncStream()
        {
            Dispose( false );
        }

        public void CancelPendingIO()
        {
            lock(m_outstandingRequests.SyncRoot)
            {
                for(int i = m_outstandingRequests.Count - 1; i >= 0; i--)
                {
                    AsyncFileStream_AsyncResult asfar = (AsyncFileStream_AsyncResult)m_outstandingRequests[i];
                    asfar.SignalCompleted();
                }

                m_outstandingRequests.Clear();
            }
        }
        
        protected override void Dispose( bool disposing )
        {            
            // Nothing will be done differently based on whether we are disposing vs. finalizing.
            lock (this)
            {
                if (m_handle != null && !m_handle.IsInvalid)
                {
                    if(disposing)
                    {
                        CancelPendingIO();
                    }

                    m_handle.Close();
                    m_handle.SetHandleAsInvalid();
                }                
            }

            base.Dispose(disposing);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw NotImplemented(); }
        }

        public override long Position
        {
            get { throw NotImplemented(); }
            set { throw NotImplemented(); }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return BeginReadCore(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return BeginWriteCore(buffer, offset, count, callback, state);
        }

        public override void Close()
        {
            Dispose(true);
        }

        public override int EndRead( IAsyncResult asyncResult )
        {
            AsyncFileStream_AsyncResult afsar = CheckParameterForEnd( asyncResult, false );

            afsar.WaitCompleted();

            m_outstandingRequests.Remove( afsar );

            // Now check for any error during the read.
            if(afsar.m_errorCode != 0) throw new IOException( "Async Read failed", afsar.m_errorCode );

            return afsar.m_numBytes;
        }

        public override void EndWrite( IAsyncResult asyncResult )
        {
            AsyncFileStream_AsyncResult afsar = CheckParameterForEnd( asyncResult, true );

            afsar.WaitCompleted();

            m_outstandingRequests.Remove( afsar );

            // Now check for any error during the write.
            if(afsar.m_errorCode != 0) throw new IOException( "Async Write failed", afsar.m_errorCode );
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            IAsyncResult result = BeginRead(buffer, offset, count, null, null);
            return EndRead( result );
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw NotImplemented();
        }

        public override void SetLength(long value)
        {
            throw NotImplemented();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            IAsyncResult result = BeginWrite(buffer, offset, count, null, null);
            EndWrite( result );
        }

        public SafeHandle Handle
        {
            get
            {
                return m_handle;
            }
        }

        public virtual int AvailableCharacters
        {
            get
            {
                return 0;
            }
        }

        private Exception NotImplemented()
        {
            return new NotSupportedException( "Not Supported" );
        }

        private void CheckParametersForBegin( byte[] array, int offset, int count )
        {
            if(array == null) throw new ArgumentNullException( "array" );

            if(offset < 0) throw new ArgumentOutOfRangeException( "offset" );

            if(count < 0 || array.Length - offset < count) throw new ArgumentOutOfRangeException( "count" );

            if(m_handle.IsInvalid)
            {
                throw new ObjectDisposedException( null );
            }
        }

        private AsyncFileStream_AsyncResult CheckParameterForEnd( IAsyncResult asyncResult, bool isWrite )
        {
            if(asyncResult == null) throw new ArgumentNullException( "asyncResult" );

            AsyncFileStream_AsyncResult afsar = asyncResult as AsyncFileStream_AsyncResult;
            if(afsar == null || afsar.m_isWrite != isWrite) throw new ArgumentException( "asyncResult" );
            if(afsar.m_EndXxxCalled) throw new InvalidOperationException( "EndRead called twice" );
            afsar.m_EndXxxCalled = true;

            return afsar;
        }

        private unsafe IAsyncResult BeginReadCore( byte[] array, int offset, int count, AsyncCallback userCallback, Object stateObject )
        {
            CheckParametersForBegin( array, offset, count );

            AsyncFileStream_AsyncResult asyncResult = new AsyncFileStream_AsyncResult( userCallback, stateObject, false );

            if(count == 0)
            {
                asyncResult.SignalCompleted();
            }
            else
            {
                // Keep the array in one location in memory until the OS writes the
                // relevant data into the array.  Free GCHandle later.
                asyncResult.PinBuffer( array );

                fixed(byte* p = array)
                {
                    int  numBytesRead = 0;
                    bool res;

                    res = Native.ReadFile( m_handle.DangerousGetHandle(), p + offset, count, out numBytesRead, asyncResult.OverlappedPtr );
                    if(res == false)
                    {
                        if(HandleErrorSituation( "BeginRead", false ))
                        {
                            asyncResult.SignalCompleted();
                        }
                        else
                        {
                            m_outstandingRequests.Add( asyncResult );
                        }
                    }                    
                }
            }

            return asyncResult;
        }

        private unsafe IAsyncResult BeginWriteCore( byte[] array, int offset, int count, AsyncCallback userCallback, Object stateObject )
        {
            CheckParametersForBegin( array, offset, count );

            AsyncFileStream_AsyncResult asyncResult = new AsyncFileStream_AsyncResult( userCallback, stateObject, true );

            if(count == 0)
            {
                asyncResult.SignalCompleted();
            }
            else
            {
                // Keep the array in one location in memory until the OS writes the
                // relevant data into the array.  Free GCHandle later.
                asyncResult.PinBuffer( array );

                fixed(byte* p = array)
                {
                    int  numBytesWritten = 0;
                    bool res;

                    res = Native.WriteFile( m_handle.DangerousGetHandle(), p + offset, count, out numBytesWritten, asyncResult.OverlappedPtr );
                    if(res == false)
                    {
                        if(HandleErrorSituation( "BeginWrite", true ))
                        {
                            asyncResult.SignalCompleted();
                        }
                        else
                        {
                            m_outstandingRequests.Add( asyncResult );
                        }
                    }
                }
            }

            return asyncResult;
        }

        protected virtual bool HandleErrorSituation( string msg, bool isWrite )
        {
            int hr = Marshal.GetLastWin32Error();

            // For invalid handles, detect the error and close ourselves
            // to prevent a malicious app from stealing someone else's file
            // handle when the OS recycles the handle number.
            if(hr == Native.ERROR_INVALID_HANDLE)
            {
                m_handle.Close();
            }

            if(hr != Native.ERROR_IO_PENDING)
            {
                if(isWrite == false && hr == Native.ERROR_HANDLE_EOF)
                {
                    throw new EndOfStreamException( msg );
                }

                throw new IOException( msg, hr );
            }

            return false;
        }


        #region IDisposable Members

        void IDisposable.Dispose()
        {
            base.Dispose( true );

            Dispose( true );

            GC.SuppressFinalize( this );
        }

        #endregion
}

    public class AsyncFileStream : GenericAsyncStream
    {
        private string m_fileName = null;

        public AsyncFileStream( string file, System.IO.FileShare share ) : base( OpenHandle( file, share ) )
        {
            m_fileName = file;
        }

        static private SafeFileHandle OpenHandle( string file, System.IO.FileShare share )
        {
            if(file == null || file.Length == 0)
            {
                throw new ArgumentNullException( "file" );
            }

            SafeFileHandle handle = Native.CreateFile(file, Native.GENERIC_READ | Native.GENERIC_WRITE, share, Native.NULL, System.IO.FileMode.Open, Native.FILE_FLAG_OVERLAPPED, Native.NULL);
            
            if(handle.IsInvalid)
            {
                throw new InvalidOperationException( String.Format( "Cannot open {0}", file ) );
            }

            return handle;
        }

        public String Name
        {
            get
            {
                return m_fileName;
            }
        }

        public unsafe override int AvailableCharacters
        {
            get
            {
                int bytesRead;
                int totalBytesAvail;
                int bytesLeftThisMessage;

                if(Native.PeekNamedPipe( m_handle.DangerousGetHandle(), (byte*)Native.NULL, 0, out bytesRead, out totalBytesAvail, out bytesLeftThisMessage ) == false)
                {
                    totalBytesAvail = 1;
                }

                return totalBytesAvail;
            }
        }
    }

    public class AsyncSerialStream : AsyncFileStream
    {
        public AsyncSerialStream( string port, uint baudrate ) : base( port, System.IO.FileShare.None )
        {
            Native.COMMTIMEOUTS cto = new Native.COMMTIMEOUTS(); cto.Initialize();
            Native.DCB          dcb = new Native.DCB         (); dcb.Initialize();

            Native.GetCommState( m_handle.DangerousGetHandle(), ref dcb );

            dcb.BaudRate = baudrate;
            dcb.ByteSize = 8;
            dcb.StopBits = 0;

            dcb.__BitField = 0;
            dcb.__BitField &= ~Native.DCB.mask_fDtrControl  ;
            dcb.__BitField &= ~Native.DCB.mask_fRtsControl  ;
            dcb.__BitField |=  Native.DCB.mask_fBinary      ;
            dcb.__BitField &= ~Native.DCB.mask_fParity      ;
            dcb.__BitField &= ~Native.DCB.mask_fOutX        ;
            dcb.__BitField &= ~Native.DCB.mask_fInX         ;
            dcb.__BitField &= ~Native.DCB.mask_fErrorChar   ;
            dcb.__BitField &= ~Native.DCB.mask_fNull        ;
            dcb.__BitField |=  Native.DCB.mask_fAbortOnError;

            Native.SetCommState( m_handle.DangerousGetHandle(), ref dcb );

            Native.SetCommTimeouts( m_handle.DangerousGetHandle(), ref cto );
        }

        public override int AvailableCharacters
        {
            get
            {
                Native.COMSTAT cs = new Native.COMSTAT(); cs.Initialize();
                uint           errors;

                Native.ClearCommError( m_handle.DangerousGetHandle(), out errors, ref cs );

                return (int)cs.cbInQue;
            }
        }

        protected override bool HandleErrorSituation( string msg, bool isWrite )
        {
            if(Marshal.GetLastWin32Error() == Native.ERROR_OPERATION_ABORTED)
            {
                Native.COMSTAT cs = new Native.COMSTAT(); cs.Initialize();
                uint           errors;

                Native.ClearCommError( m_handle.DangerousGetHandle(), out errors, ref cs );

                return true;
            }

            return base.HandleErrorSituation( msg, isWrite );
        }

        public void ConfigureXonXoff( bool fEnable )
        {
            Native.DCB dcb = new Native.DCB(); dcb.Initialize();

            Native.GetCommState( m_handle.DangerousGetHandle(), ref dcb );

            if(fEnable)
            {
                dcb.__BitField |= Native.DCB.mask_fOutX;
            }
            else
            {
                dcb.__BitField &= ~Native.DCB.mask_fOutX;
            }

            Native.SetCommState( m_handle.DangerousGetHandle(), ref dcb );
        }

        static public PortDefinition[] EnumeratePorts()
        {
            SortedList lst = new SortedList();

            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey( @"HARDWARE\DEVICEMAP\SERIALCOMM" );

                foreach(string name in key.GetValueNames())
                {
                    string         val = (string)key.GetValue( name );
                    PortDefinition pd  = PortDefinition.CreateInstanceForSerial( val, @"\\.\" + val, 115200 );

                    lst.Add( val, pd );
                }
            }
            catch
            {
            }

            ICollection      col = lst.Values;
            PortDefinition[] res = new PortDefinition[col.Count];

            col.CopyTo( res, 0 );

            return res;
        }
    }

    public class AsyncNetworkStream : NetworkStream, WireProtocol.IStreamAvailableCharacters
    {
        public AsyncNetworkStream(Socket socket, bool ownsSocket)
            : base(socket, ownsSocket)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #region IStreamAvailableCharacters

        int WireProtocol.IStreamAvailableCharacters.AvailableCharacters
        {
            get
            {
                return this.Socket.Available;
            }
        }
        #endregion 
    }

    [Serializable]
    public class PortDefinition_Serial : PortDefinition
    {
        uint m_baudRate;

        public PortDefinition_Serial( string displayName, string port, uint baudRate ) : base(displayName, port)
        {
            m_baudRate = baudRate;
        }

        public uint BaudRate
        {
            get
            {
                return m_baudRate;
            }

            set
            {
                m_baudRate = value;
            }
        }

        public override Stream CreateStream()
        {
            return new AsyncSerialStream( m_port, m_baudRate );
        }

        public override string PersistName
        {
            get { return m_displayName; }
        }
    }

    public class UsbDeviceDiscovery : IDisposable
    {
        public enum DeviceChanged : ushort
        {
            None          = 0,
            Configuration = 1,
            DeviceArrival = 2,
            DeviceRemoval = 3,
            Docking       = 4,
        }

        public delegate void DeviceChangedEventHandler( DeviceChanged change );

        private const string c_EventQuery    = "Win32_DeviceChangeEvent";
        private const string c_InstanceQuery = "SELECT * FROM __InstanceOperationEvent WITHIN 5 WHERE TargetInstance ISA \"Win32_PnPEntity\"";

        ManagementEventWatcher    m_eventWatcher;
        DeviceChangedEventHandler m_subscribers;

        public UsbDeviceDiscovery()
        {
        }

        ~UsbDeviceDiscovery()
        {
            try
            {
                Dispose();
            }
            catch
            {
            }
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            if( m_eventWatcher != null )
            {
                m_eventWatcher.Stop();

                m_eventWatcher = null;
                m_subscribers = null;
            }
            GC.SuppressFinalize(this);
        }

        // subscribing to this event allows applications to be notified when USB devices are plugged and unplugged
        // as well as configuration changed and docking; upon receiving teh notification the applicaion can decide
        // to call UsbDeviceDiscovery.EnumeratePorts to get an updated list of Usb devices
        public event DeviceChangedEventHandler OnDeviceChanged
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                try
                {
                    TryEventNotification( value );
                }
                catch
                {
                    TryInstanceNotification( value );
                }
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                m_subscribers -= value;

                if(m_subscribers == null)
                {
                    if (m_eventWatcher != null)
                    {                        
                        m_eventWatcher.Stop();
                        m_eventWatcher = null;
                    }
                }
            }
        }

        private void TryEventNotification( DeviceChangedEventHandler handler )
        {
            m_eventWatcher = new ManagementEventWatcher( new WqlEventQuery( c_EventQuery  ) );
                
            m_eventWatcher.EventArrived += new EventArrivedEventHandler( HandleDeviceEvent ); 

            if(m_subscribers == null)
            {
                m_eventWatcher.Start();
            }

            m_subscribers += handler;           
        }

        private void TryInstanceNotification( DeviceChangedEventHandler handler )
        {
            m_eventWatcher = new ManagementEventWatcher( new WqlEventQuery( c_InstanceQuery ) );

            m_eventWatcher.EventArrived += new EventArrivedEventHandler( HandleDeviceInstance );  

            if(m_subscribers == null)
            {
                m_eventWatcher.Start();
            }

            m_subscribers += handler;          
        }

        private void HandleDeviceEvent( object sender, EventArrivedEventArgs args )
        {
            if(m_subscribers != null)
            {
                ManagementBaseObject deviceEvent = args.NewEvent;

                ushort eventType = (ushort)deviceEvent["EventType"];

                m_subscribers( (DeviceChanged)eventType );
            }
        }

        private void HandleDeviceInstance( object sender, EventArrivedEventArgs args )
        {
            if(m_subscribers != null)
            {
                ManagementBaseObject deviceEvent = args.NewEvent;

                if(deviceEvent.ClassPath.ClassName.Equals( "__InstanceCreationEvent" ))
                {
                    m_subscribers( DeviceChanged.DeviceArrival );                    
                }
                else if(deviceEvent.ClassPath.ClassName.Equals( "__InstanceDeletionEvent" ))
                {
                    m_subscribers( DeviceChanged.DeviceRemoval );
                }
            }
        }
    }

    public class AsyncUsbStream : AsyncFileStream
    {        
        // IOCTL codes
        private const int IOCTL_SPOTUSB_READ_AVAILABLE   = 0;
        private const int IOCTL_SPOTUSB_DEVICE_HASH      = 1;
        private const int IOCTL_SPOTUSB_MANUFACTURER     = 2;
        private const int IOCTL_SPOTUSB_PRODUCT          = 3;
        private const int IOCTL_SPOTUSB_SERIAL_NUMBER    = 4;
        private const int IOCTL_SPOTUSB_VENDOR_ID        = 5;
        private const int IOCTL_SPOTUSB_PRODUCT_ID       = 6;
        private const int IOCTL_SPOTUSB_DISPLAY_NAME     = 7;
        private const int IOCTL_SPOTUSB_PORT_NAME        = 8;

        // paths
        static readonly string SpotGuidKeyPath           = @"System\CurrentControlSet\Services\SpotUsb\Parameters";

        // discovery keys
        static public readonly string InquiriesInterface =  "InquiriesInterface";
        static public readonly string DriverVersion      =  "DriverVersion";
                                                       
        // mandatory property keys                 
        static public readonly string DeviceHash         =  "DeviceHash";
        static public readonly string DisplayName        =  "DisplayName";

        // optional property keys 
        static public readonly string Manufacturer       = "Manufacturer";
        static public readonly string Product            = "Product";
        static public readonly string SerialNumber       = "SerialNumber";
        static public readonly string VendorId           = "VendorId"; 
        static public readonly string ProductId          = "ProductId";
        
        private const int c_DeviceStringBufferSize     = 260;

        static private Hashtable s_textProperties;
        static private Hashtable s_digitProperties;

        static AsyncUsbStream()
        {
            s_textProperties  = new Hashtable();
            s_digitProperties = new Hashtable();

            s_textProperties.Add( DeviceHash  , IOCTL_SPOTUSB_DEVICE_HASH   );
            s_textProperties.Add( Manufacturer, IOCTL_SPOTUSB_MANUFACTURER  );
            s_textProperties.Add( Product     , IOCTL_SPOTUSB_PRODUCT       );
            s_textProperties.Add( SerialNumber, IOCTL_SPOTUSB_SERIAL_NUMBER );  
            
            s_digitProperties.Add( VendorId   , IOCTL_SPOTUSB_VENDOR_ID     );
            s_digitProperties.Add( ProductId  , IOCTL_SPOTUSB_PRODUCT_ID    );   
        }

        public AsyncUsbStream( string port ) : base( port, System.IO.FileShare.None )
        {
        }

        public unsafe override int AvailableCharacters
        {
            get
            {
                int code = Native.ControlCode( Native.FILE_DEVICE_UNKNOWN, 0, Native.METHOD_BUFFERED, Native.FILE_ANY_ACCESS );
                int avail;
                int read;

                if(!Native.DeviceIoControl( m_handle.DangerousGetHandle(), code, null, IOCTL_SPOTUSB_READ_AVAILABLE, (byte*)&avail, sizeof(int), out read, null ) || read != sizeof(int))
                {
                    return 0;
                }

                return avail;
            }
        }

        public static PortDefinition[] EnumeratePorts()
        {
            SortedList lst = new SortedList();

            // enumerate each guid under the discovery key
            RegistryKey driverParametersKey = Registry.LocalMachine.OpenSubKey( SpotGuidKeyPath );

            // if no parameters key is found, it means that no USB device has ever been plugged into the host 
            // or no driver was installed
            if(driverParametersKey != null)
            {
                string inquiriesInterfaceGuid   = (string)driverParametersKey.GetValue( InquiriesInterface   );
                string driverVersion            = (string)driverParametersKey.GetValue( DriverVersion        );
            
                if((inquiriesInterfaceGuid != null) && (driverVersion != null))
                {
                    EnumeratePorts( new Guid( inquiriesInterfaceGuid ), driverVersion, lst ); 
                }
            }

            ICollection      col = lst.Values;
            PortDefinition[] res = new PortDefinition[col.Count];

            col.CopyTo( res, 0 );

            return res;
        }


        // The following procedure works with the USB device driver; upon finding all instances of USB devices
        // that match the requested Guid, the procedure checks the corresponding registry keys to find the unique
        // serial number to show to the user; the serial number is decided by the device driver at installation
        // time and stored in a registry key whose name is the hash of the laser etched security key of the device
        private static void EnumeratePorts( Guid inquiriesInterface, string driverVersion, SortedList lst )
        {
            IntPtr devInfo = Native.SetupDiGetClassDevs( ref inquiriesInterface, null, 0, Native.DIGCF_DEVICEINTERFACE | Native.DIGCF_PRESENT );

            if(devInfo == Native.INVALID_HANDLE_VALUE)
            {
                return;
            }

            Native.SP_DEVICE_INTERFACE_DATA interfaceData = new Native.SP_DEVICE_INTERFACE_DATA(); interfaceData.cbSize = Marshal.SizeOf(interfaceData);
            
            int index = 0;

            while(Native.SetupDiEnumDeviceInterfaces( devInfo, 0, ref inquiriesInterface, index++, ref interfaceData ))
            {
                Native.SP_DEVICE_INTERFACE_DETAIL_DATA detail = new Native.SP_DEVICE_INTERFACE_DETAIL_DATA();
                // explicit size of unmanaged structure must be provided, because it does not include transfer buffer
                // for whatever reason on 64 bit machines the detail size is 8 rather than 5, likewise the interfaceData.cbSize
                // is 32 rather than 28 for non 64bit machines, therefore, we make the detemination of the size based 
                // on the interfaceData.cbSize (kind of hacky but it works).
                if( interfaceData.cbSize == 32 )
                {
                    detail.cbSize = 8;
                }
                else
                {
                    detail.cbSize = 5;
                }
                

                if(Native.SetupDiGetDeviceInterfaceDetail( devInfo, ref interfaceData, ref detail, Marshal.SizeOf(detail) * 2, 0, 0 ))
                {
                    string port = detail.DevicePath.ToLower();

                    AsyncUsbStream s = null;

                    try
                    {
                        s = new AsyncUsbStream( port );

                        string displayName     = s.RetrieveStringFromDevice( IOCTL_SPOTUSB_DISPLAY_NAME ); 
                        string hash            = s.RetrieveStringFromDevice( IOCTL_SPOTUSB_DEVICE_HASH  ); 
                        string operationalPort = s.RetrieveStringFromDevice( IOCTL_SPOTUSB_PORT_NAME    ); 

                        if((operationalPort == null) || (displayName == null) || (hash == null))
                        {
                            continue;
                        }

                        // convert  kernel format to user mode format                        
                        // kernel   : @"\??\USB#Vid_beef&Pid_0009#5&4162af8&0&1#{09343630-a794-10ef-334f-82ea332c49f3}"
                        // user     : @"\\?\usb#vid_beef&pid_0009#5&4162af8&0&1#{09343630-a794-10ef-334f-82ea332c49f3}"
                        StringBuilder operationalPortUser = new StringBuilder();
                        operationalPortUser.Append( @"\\?" );
                        operationalPortUser.Append( operationalPort.Substring( 3 ) );

                        // change the display name if there is a collision (otherwise you will only be able to use one of the devices)
                        displayName += "_" + hash;
                        if (lst.ContainsKey(displayName))
                        {
                            int i = 2;
                            while (lst.ContainsKey(displayName + " (" + i + ")"))
                            {
                                i++;
                            }
                            displayName += " (" + i + ")";
                        }

                        PortDefinition pd  = PortDefinition.CreateInstanceForUsb( displayName, operationalPortUser.ToString() );
                        
                        RetrieveProperties( hash, ref pd, s );

                        lst.Add( pd.DisplayName, pd );
                    }
                    catch
                    {
                    }
                    finally
                    {
                        if(s != null) s.Close();
                    }
                }
            }

            Native.SetupDiDestroyDeviceInfoList( devInfo );
        }
        
        private static void RetrieveProperties( string hash, ref PortDefinition pd, AsyncUsbStream s )
        {
            IDictionaryEnumerator dict;
            
            dict = s_textProperties.GetEnumerator();

            while(dict.MoveNext())
            {
                pd.Properties.Add( dict.Key, s.RetrieveStringFromDevice( (int)dict.Value ) );
            }

            dict = s_digitProperties.GetEnumerator();
            
            while(dict.MoveNext())
            {
                pd.Properties.Add( dict.Key, s.RetrieveIntegerFromDevice( (int)dict.Value ) );
            }
        }

        private unsafe string RetrieveStringFromDevice( int controlCode )
        {
            int code = Native.ControlCode( Native.FILE_DEVICE_UNKNOWN, controlCode, Native.METHOD_BUFFERED, Native.FILE_ANY_ACCESS );
            
            string data;
            int read; 
            byte[] buffer = new byte[ c_DeviceStringBufferSize ];

            fixed(byte* p = buffer)
            {
                if(!Native.DeviceIoControl( m_handle.DangerousGetHandle(), code, null, 0, p, buffer.Length, out read, null ) || (read <= 0))
                {
                    data = null;
                }
                else
                {
                    if(read > (c_DeviceStringBufferSize-2))
                    {
                        read = c_DeviceStringBufferSize-2;
                    }

                    p[read  ] = 0;
                    p[read+1] = 0;

                    data = new string( (char *)p );
                }
            }

            return data;
        }

        private unsafe int RetrieveIntegerFromDevice( int controlCode )
        {
            int code = Native.ControlCode( Native.FILE_DEVICE_UNKNOWN, controlCode, Native.METHOD_BUFFERED, Native.FILE_ANY_ACCESS );
            
            int read; 
            int digits = 0;

            if(!Native.DeviceIoControl( m_handle.DangerousGetHandle(), code, null, 0, (byte*)&digits, sizeof(int), out read, null ) || (read <= 0))
            {
                digits = -1;
            }

            return digits;
        }
    }

    [Serializable]
    public class PortDefinition_Usb : PortDefinition
    {
        public PortDefinition_Usb( string displayName, string port, ListDictionary properties ) : base(displayName, port)
        {
            m_properties = properties;
        }

        public override object UniqueId
        {
            get
            {
                return m_properties[AsyncUsbStream.DeviceHash];
            }
        }

        public override Stream CreateStream()
        {
            try
            {
                return new AsyncUsbStream( m_port );
            }
            catch
            {
                object uniqueId = UniqueId;

                foreach(PortDefinition pd in AsyncUsbStream.EnumeratePorts())
                {
                    if(Object.Equals( pd.UniqueId, uniqueId ))
                    {
                        m_properties = pd.Properties;
                        m_port       = pd.Port;

                        return new AsyncUsbStream( m_port );
                    }
                }

                throw;
            }
        }    
    }

    [Serializable]
    public class PortDefinition_Tcp : PortDefinition
    {
        public const int WellKnownPort = 26000;

        IPEndPoint m_ipEndPoint;

        string m_macAddress = "";

        internal unsafe struct SOCK_discoveryinfo 
        {
            internal uint       ipaddr;
            internal uint       macAddressLen;
            internal fixed byte macAddressBuffer[64];    
        };

        public string MacAddress
        {
            get { return m_macAddress; }
                
        }
        

        public PortDefinition_Tcp(IPEndPoint ipEndPoint, string macAddress)
            : base(ipEndPoint.Address.ToString(), ipEndPoint.ToString())
        {
            if(!string.IsNullOrEmpty(macAddress))
            {
                m_displayName += " - (" + macAddress + ")";
            }
            m_ipEndPoint = ipEndPoint;
            m_macAddress = macAddress;
        }

        public PortDefinition_Tcp(IPEndPoint ipEndPoint)
            : this(ipEndPoint, "")
        {
            m_ipEndPoint = ipEndPoint;
        }

        public PortDefinition_Tcp(IPAddress address)
            : this(new IPEndPoint(address, WellKnownPort), "")
        {
        }

        public PortDefinition_Tcp(IPAddress address, string macAddress)
            : this(new IPEndPoint(address, WellKnownPort), macAddress)
        {
        }

        public override object UniqueId
        {
            get
            {
                return m_ipEndPoint.ToString();
            }
        }

        public static PortDefinition[] EnumeratePorts()
        {
            return EnumeratePorts(System.Net.IPAddress.Parse("234.102.98.44"), System.Net.IPAddress.Parse("234.102.98.45"), 26001, "DOTNETMF", 3000, 1);
        }

        public static PortDefinition[] EnumeratePorts(
            System.Net.IPAddress DiscoveryMulticastAddress    ,
            System.Net.IPAddress DiscoveryMulticastAddressRecv,
            int       DiscoveryMulticastPort       ,
            string    DiscoveryMulticastToken      ,
            int       DiscoveryMulticastTimeout    ,
            int       DiscoveryTTL                 
        )
        {
            PortDefinition_Tcp []ports = null;
            Dictionary<string, string> addresses = new Dictionary<string, string>();
            
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());

                foreach (IPAddress ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        int cnt = 0;
                        int total = 0;
                        byte[] data = new byte[1024];
                        Socket sock = null;
                        Socket recv = null;

                        System.Net.IPEndPoint endPoint    = new System.Net.IPEndPoint(ip, 0);
                        System.Net.EndPoint   epRemote    = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 26001);
                        System.Net.IPEndPoint epRecv      = new System.Net.IPEndPoint(ip, DiscoveryMulticastPort);
                        System.Net.IPEndPoint epMulticast = new System.Net.IPEndPoint(DiscoveryMulticastAddress, DiscoveryMulticastPort);

                        try
                        {
                            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            recv = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                            recv.Bind(epRecv);
                            recv.ReceiveTimeout = DiscoveryMulticastTimeout;
                            recv.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(DiscoveryMulticastAddressRecv, ip));

                            sock.Bind(endPoint);
                            sock.MulticastLoopback = false;
                            sock.Ttl = (short)DiscoveryTTL;
                            sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 64);

                            // send ping
                            sock.SendTo(System.Text.Encoding.ASCII.GetBytes(DiscoveryMulticastToken), SocketFlags.None, epMulticast);

                            while (0 < (cnt = recv.ReceiveFrom(data, total, data.Length - total, SocketFlags.None, ref epRemote)))
                            {
                                addresses[((IPEndPoint)epRemote).Address.ToString()] = "";
                                total += cnt;
                                recv.ReceiveTimeout = DiscoveryMulticastTimeout / 2;
                            }

                            recv.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(DiscoveryMulticastAddressRecv));

                        }
                        // SocketException occurs in RecieveFrom if there is no data.
                        catch (SocketException)
                        {
                        }
                        finally
                        {
                            if (recv != null)
                            {
                                recv.Close();
                                recv = null;
                            }
                            if (sock != null)
                            {
                                sock.Close();
                                sock = null;
                            }
                        }

                        // use this if we need to get the MAC address of the device
                        SOCK_discoveryinfo disc = new SOCK_discoveryinfo();
                        disc.ipaddr = 0;
                        disc.macAddressLen = 0;
                        int idx = 0;
                        int c_DiscSize = Marshal.SizeOf(disc);
                        while (total >= c_DiscSize)
                        {
                            byte[] discData = new byte[c_DiscSize];
                            Array.Copy(data, idx, discData, 0, c_DiscSize);
                            GCHandle gch = GCHandle.Alloc(discData, GCHandleType.Pinned);
                            disc = (SOCK_discoveryinfo)Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(SOCK_discoveryinfo));
                            gch.Free();

                            // previously we only displayed the IP address for the device, which doesn't
                            // really tell you which device you are talking to.  The MAC address should be unique.
                            // therefore we will display the MAC address in the device display name to help distinguish
                            // the devices.  
                            if (disc.macAddressLen <= 64 && disc.macAddressLen > 0)
                            {
                                IPAddress ipResp = new IPAddress((long)disc.ipaddr);

                                // only append the MAC if it matches one of the IP address we got responses from
                                if (addresses.ContainsKey(ipResp.ToString()))
                                {
                                    string strMac = "";
                                    for (int mi = 0; mi < disc.macAddressLen - 1; mi++)
                                    {
                                        unsafe
                                        {
                                            strMac += string.Format("{0:x02}-", disc.macAddressBuffer[mi]);
                                        }
                                    }
                                    unsafe
                                    {
                                        strMac += string.Format("{0:x02}", disc.macAddressBuffer[disc.macAddressLen - 1]);
                                    }

                                    addresses[ipResp.ToString()] = strMac;
                                }
                            }
                            total -= c_DiscSize;
                            idx += c_DiscSize;
                        }
                    }
                }
            }
            catch( Exception e2)
            {
                System.Diagnostics.Debug.Print(e2.ToString());
            }

            ports = new PortDefinition_Tcp[addresses.Count];
            int i = 0;

            foreach(string key in addresses.Keys)
            {
                ports[i++] = new PortDefinition_Tcp(IPAddress.Parse(key), addresses[key]);
            }

            return ports;            
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public override Stream CreateStream()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.NoDelay = true;
            socket.LingerState = new LingerOption(false, 0);

            IAsyncResult asyncResult = socket.BeginConnect(m_ipEndPoint, null, null);

            if (asyncResult.AsyncWaitHandle.WaitOne(2000, false))
            {
                socket.EndConnect(asyncResult);
            }
            else
            {
                socket.Close();
                throw new IOException("Connect failed");
            }

            AsyncNetworkStream stream = new AsyncNetworkStream(socket, true);

            return stream;
        }    
    }
}
