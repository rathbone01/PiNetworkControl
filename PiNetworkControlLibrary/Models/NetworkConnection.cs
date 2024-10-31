﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiNetworkControl.Models
{
    public class NetworkConnection
    {
        public string Name { get; set; } = string.Empty;
        public string UUID { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
    }
}
