using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiNetworkControl.Models
{
    public class WifiScanResult
    {
        public string Bssid { get; set; } = string.Empty;
        public string Ssid { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public int Channel { get; set; }
        public string Rate { get; set; } = string.Empty;
        public int Signal { get; set; }
        public string Bars { get; set; } = string.Empty;
        public string Security { get; set; } = string.Empty;
    }
}
