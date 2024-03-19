using NetworkManagerWrapperLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkManagerWrapperLibrary.NetworkController
{
    public class NetworkController
    {
        public NetworkController()
        {
        }

        public bool CheckNetworkManagerServiceExection()
        {
            throw new NotImplementedException();
        }

        public List<NetworkDevice> GetNetworkDevices()
        {
            throw new NotImplementedException();
        }

        public NetworkDeviceProperties GetNetworkDeviceProperties(string id)
        {
            throw new NotImplementedException();
        }

        public List<NetworkConnection> GetNetworkConnections()
        {
            throw new NotImplementedException();
        }

        public NetworkConnectionProperties GetNetworkConnectionProperties(string id)
        {
            throw new NotImplementedException();
        }

        public List<WirelessAdapter> GetWirelessAdapters()
        {
            throw new NotImplementedException();
        }

        public bool ModifyNetworkConnection()
        {
            throw new NotImplementedException();
        }

        public bool EnableNetworkConnection()
        {
            throw new NotImplementedException();
        }

        public bool DisableNetworkConnection()
        {
            throw new NotImplementedException();
        }

        public bool ConnectToWirelessNetwork()
        {
            throw new NotImplementedException();
        }
    }
}
