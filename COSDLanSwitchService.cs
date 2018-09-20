using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Collections;


namespace COSDLanSwitchService
{
    public partial class COSDLanSwitchService : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        //enum for service state
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        //define a structure for possible service statuses
        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        }

        //constructor, takes argments for event log entry
        public COSDLanSwitchService(string[] args)
        {
            InitializeComponent();

            //create an event source and log name
            string eventSourceName = "MySource";
            string logName = "MyNewLog";

            //if the args exist, set the event source and the logName accordingly
            if (args.Count() > 0)
            {
                eventSourceName = args[0];
            }
            if (args.Count() > 1)
            {
                logName = args[1];
            }

            //instantiate the event log
            eventLog1 = new System.Diagnostics.EventLog();

            //if the log does not exist already, create it
            if (!System.Diagnostics.EventLog.SourceExists(eventSourceName))
            {
                System.Diagnostics.EventLog.CreateEventSource(eventSourceName, logName);
            }

            //set the source and the log name to the log
            eventLog1.Source = eventSourceName;
            eventLog1.Log = logName;
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            //write to the event log
            eventLog1.WriteEntry("In OnStart");

            //Create a network address changed event handler
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);
            eventLog1.WriteEntry("listener created", EventLogEntryType.Information, 100);

            // Update the service state to running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected void DisconnectWifi(ArrayList nicList)
        {
            foreach (NetworkInterface nic in nicList)
            {
                //write the start of the process to the event log
                eventLog1.WriteEntry("disconnecting from wlan: " + nic.Name, EventLogEntryType.Information, 200);

                //start a process to disconnect from wlan
                ProcessStartInfo psi = new ProcessStartInfo("netsh", "wlan disconnect interface=\"" + nic.Name + "\"");
                Process p = new Process();
                p.StartInfo = psi;
                p.Start();

                //write the success to the event log
                eventLog1.WriteEntry("disconnected from wlan: " + nic.Name, EventLogEntryType.Information, 200);
            }
        }

        protected void AddressChangedCallback(object sender, EventArgs e)
        {
            //create a flag to determine whether the wifi should be disconnected after the loop completes
            bool willSwitch = false;

            //log the process
            eventLog1.WriteEntry("AddressChangedCallback called", EventLogEntryType.Information, 50);

            //get all the adapters on the system
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

            //create an array of connected wireless adapters
            ArrayList wirelessAdaptersConnected = new ArrayList();

            //foreach adapter, log its operational status and check for connected ethernet
            foreach(NetworkInterface n in adapters)
            {
                eventLog1.WriteEntry(n.Name + " is " + n.OperationalStatus, EventLogEntryType.Information, 100);

                //if the operational status of the network adapter is up, and the interface is ethernet, mark the switch flag to true
                if (n.OperationalStatus.Equals(OperationalStatus.Up) && n.NetworkInterfaceType.Equals(NetworkInterfaceType.Ethernet) 
                    && !n.Description.Equals("Juniper Networks Virtual Adapter") && !n.Description.Equals("Hyper-V Virtual Ethernet Adapter")
                {
                    willSwitch = true;
                }

                //if the nic is wireless and connected, add it to the array for wireless connected adapters
                if (n.OperationalStatus.Equals(OperationalStatus.Up) && n.NetworkInterfaceType.Equals(NetworkInterfaceType.Wireless80211))
                {
                    wirelessAdaptersConnected.Add(n);
                    eventLog1.WriteEntry(n.Name + " is a connected wifi adapter", EventLogEntryType.Information, 200);
                }
            }

            if (willSwitch)
            {
                DisconnectWifi(wirelessAdaptersConnected);
            }
        }

        protected override void OnContinue()
        {
            eventLog1.WriteEntry("In onContinue.");
        }

        protected override void OnStop()
        {
            // Update the service state to Stop Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            //write to the event log
            eventLog1.WriteEntry("In onStop.");

            // Update the service state to running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }
    }
}
