# PiNetworkControl
This is a library to control the network interfaces on a raspberry pi from a .NET application.
This library works by controlling NMCLI via the command line using CLIWrap.
I will turn it into a nuget package when I get around to learning how to do that.

## Examples
In these examples assume the following:

    public enum NetworkType
    {
        Ethernet,
        Wifi
    }
    
    public class NetworkConfiguration
    {
        public NetworkType? NetworkType { get; set; }
        public bool? DhcpEnabled { get; set; }
        public string? IpAddress { get; set; } 
        public string? SubnetMask { get; set; } 
        public string? Gateway { get; set; }
        public string? DnsServerPrimary { get; set; }
        public string? DnsServerSecondary { get; set; }
        public string? WifiSsid { get; set; }
        public string? WifiPassword { get; set; }
    }

### Example of setting up a network:


    private async Task CreateConnection(string connectionName, NetworkConfiguration networkConfiguration)
    {
        if (networkConfiguration.NetworkType == NetworkType.Wifi)
        {
            configurationService.SetNetworkConnectionType("wifi");
            await networkController.AddWifiConnectionAsync(connectionName, "wlan0", networkConfiguration.WifiSsid!, networkConfiguration.WifiPassword!, "WPA-PSK");
            _logger?.LogInformation($"Added wifi connection {connectionName}, SSID: {networkConfiguration.WifiSsid}");
            return;
        }
    
        configurationService.SetNetworkConnectionType("ethernet");
        await networkController.AddEthernetConnectionAsync(connectionName, "eth0");
        _logger?.LogInformation($"Added ethernet connection {connectionName}");
    }
    
    private async Task SetConnectionProperties(string connectionName, NetworkConfiguration networkConfiguration)
    {
        _logger?.LogInformation("Setting connection properties");
    
        if ((bool)networkConfiguration.DhcpEnabled!)
        {
            await networkController.ModifyConnectionPropertyAsync(connectionName, "ipv4.method", "auto");
            _logger?.LogInformation("Set ipv4.method to auto");
            return;
        }
    
        _logger?.LogInformation("Setting manual connection properties");
    
        await networkController.ModifyConnectionPropertyAsync(connectionName, "ipv4.addresses", $"{networkConfiguration.IpAddress}/{ConvertSubnetMaskToBits(networkConfiguration.SubnetMask!)}");
        await networkController.ModifyConnectionPropertyAsync(connectionName, "ipv4.gateway", networkConfiguration.Gateway!);
        await networkController.ModifyConnectionPropertyAsync(connectionName, "ipv4.dns", $"\"{networkConfiguration.DnsServerPrimary!} {networkConfiguration.DnsServerSecondary!}\"");
        await networkController.ModifyConnectionPropertyAsync(connectionName, "ipv4.method", "manual");
        await networkController.ModifyConnectionPropertyAsync(connectionName, "connection.autoconnect", "yes");
    
        _logger?.LogInformation($"Set ipv4.addresses to {networkConfiguration.IpAddress}/{ConvertSubnetMaskToBits(networkConfiguration.SubnetMask!)}");
    }

### Connecting to the network:

    private async Task<bool> Connect(string connectionName, NetworkConfiguration networkConfiguration)
    {
        if (networkConfiguration.NetworkType == NetworkType.Wifi)
        {
            if (!await networkController.EnableRadioAsync())
            {
                _logger?.LogError("Error enabling wifi radio");
            }
            else
            {
                _logger?.LogInformation("Enabled wifi radio");
            }
        }
    
        if (await networkController.EnableConnectionAsync(connectionName))
        {
            _logger?.LogInformation($"Enabled connection {connectionName}");
            return true;
        }
        else
        {
            _logger?.LogError($"Error enabling connection {connectionName}");
        }
    
        return false;
    }

### Creating an access point

    public async Task CreateAP(string connectionName, string Ssid, string Password)
    {
        await RemoveConnection(connectionName);
        await networkController.AddWifiConnectionAsync(connectionName, "wlan0", Ssid, Password, "WPA-PSK");
        _logger?.LogInformation($"Added wifi connection {connectionName}, SSID: {Ssid}");
    
        _logger?.LogInformation($"Setting connection properties for Access Point");
        await networkController.ModifyConnectionPropertyAsync(connectionName, "connection.autoconnect", "no");
        await networkController.ModifyConnectionPropertyAsync(connectionName, "802-11-wireless.mode", "ap");
        await networkController.ModifyConnectionPropertyAsync(connectionName, "802-11-wireless.band", "bg");
        await networkController.ModifyConnectionPropertyAsync(connectionName, "ipv4.method", "shared");
        await networkController.EnableConnectionAsync(connectionName);
    
        _logger?.LogInformation($"Created and enabled ap: {connectionName} with ssid {Ssid}");
    }
