using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiNetworkControl
{
    public class NetworkDevice
    {
        public string Device { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Connection { get; set; } = string.Empty;
    }
}
