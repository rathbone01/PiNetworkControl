// This class relies on the CliWrap library to run the nmcli command line tool to interact with NetworkManager (nmcli).
// This could also be achieved using the System.Diagnostics.Process class, but CliWrap is a more modern and easier to use library.

using PiNetworkControl.Models;
using System.Text;
using CliWrap;
using Microsoft.Extensions.Logging;

namespace PiNetworkControl
{
    public class NetworkController
    {
        private ILogger<NetworkController>? _logger;

        public NetworkController(ILogger<NetworkController>? logger = null)
        {
            _logger = logger;
            _logger?.LogDebug("NetworkController class created, Logger injected");
        }

        // General methods
        public async Task<bool> CheckNetworkManagerServiceExecutionAsync()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("systemctl")
                .WithArguments("status NetworkManager")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error checking NetworkManager service status: {stdErrBuffer}");
                return false;
            }

            return stdOut.Contains("Active: active (running)");
        }

        public async Task<List<NetworkDevice>> GetDevicesAsync()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("nmcli")
                .WithArguments("device")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error getting devices: {stdErrBuffer}");
                return new();
            }

            var networkDevices = new List<NetworkDevice>();
            foreach (var line in stdOut.Split("\n").ToList())
            {
                var parts = line.Split("  ", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 4)
                    continue;

                var device = new NetworkDevice
                {
                    Device = parts[0],
                    Type = parts[1],
                    State = parts[2],
                    Connection = parts[3]
                };
                networkDevices.Add(device);
            }

            return networkDevices;
        }

        public async Task<Dictionary<string, string>> GetDevicePropertiesAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("nmcli")
                .WithArguments($"device show {id}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error getting device properties: {stdErrBuffer}");
                return new();
            }

            var properties = new Dictionary<string, string>();
            foreach (var line in stdOut.Split("\n").ToList())
            {
                var parts = line.Split(":", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    continue;
                properties.Add(parts[0].Trim(), parts[1].Trim());
            }

            return properties;
        }

        public async Task<List<NetworkConnection>> GetConnectionsAsync()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("nmcli")
                .WithArguments("connection")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error getting connections: {stdErrBuffer}");
                return new();
            }

            var networkConnections = new List<NetworkConnection>();
            foreach (var line in stdOut.Split("\n").ToList())
            {
                var parts = line.Split("  ", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 4)
                    continue;

                var connection = new NetworkConnection
                {
                    Name = parts[0],
                    UUID = parts[1],
                    Type = parts[2],
                    Device = parts[3]
                };
                networkConnections.Add(connection);
            }

            return networkConnections;
        }

        public async Task<Dictionary<string, string>> GetConnectionPropertiesAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("nmcli")
                .WithArguments($"connection show id {id}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error getting connection properties: {stdErrBuffer}");
                return new();
            }

            var properties = new Dictionary<string, string>();
            foreach (var line in stdOut.Split("\n").ToList())
            {
                var parts = line.Split(":", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length! > 1)
                    continue;
                // Mac address has a colon in it, so we need to join the parts
                properties.Add(parts[0].Trim(), string.Join(":", parts.Skip(1)).Trim());
            }

            return properties;
        }

        public async Task<bool> AddEthernetConnectionAsync(string name, string interfaceId)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection add type ethernet con-name {name} ifname {interfaceId}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error adding ethernet connection: {stdErrBuffer}");
                return false;
            }

            return true;
        }

        public async Task<bool> AddWifiConnectionAsync(string name, string interfaceId, string ssid, string password, string keyManagement)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection add type wifi con-name {name} ifname {interfaceId} ssid \"{ssid}\" +802-11-wireless-security.key-mgmt \"{keyManagement}\" +802-11-wireless-security.psk \"{password}\"")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error adding wifi connection: {stdErrBuffer}");
                return false;
            }

            return true;
        }

        public async Task<bool> ModifyConnectionPropertyAsync(string connectionId, string property, string value)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection modify {connectionId} {property} {value}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error modifying connection property {property} to {value}: {stdErrBuffer}");
                return false;
            }

            //Check if the property was set correctly
            var newValue = await GetConnectionPropertyAsync(connectionId, property);
            if (newValue.ToLower() != value.ToLower())
            {
                _logger?.LogError($"Error: {newValue} != {value}");
            }

            return true;
        }

        public async Task<bool> ModifyConnectionPropertiesAsync(string connectionId, Dictionary<string, string> properties)
        {
            foreach (var property in properties)
            {
                if (!await ModifyConnectionPropertyAsync(connectionId, property.Key, property.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<string> GetConnectionPropertyAsync(string connectionId, string property)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli --terse --fields {property} con show {connectionId}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            // result is property:value
            // we only want the value
            var parts = stdOutBuffer.ToString().Split(":");
            if (result.ExitCode != 0 || parts.Length < 2)
            {
                _logger?.LogError($"Error getting connection property {property} on connection {connectionId}: {stdErrBuffer}");
                return string.Empty;
            }

            return parts[1].Replace("\r", string.Empty).Replace("\n", string.Empty);
        }

        public async Task<bool> DeleteConnectionAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection delete {id}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error deleting connection {id}: {stdErrBuffer}");
                return false;
            }

            return true;
        }

        public async Task<bool> EnableConnectionAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection up {id}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error: {stdErrBuffer}");
                return false;
            }

            return true;
        }

        public async Task<bool> DisableConnectionAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection down {id}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error disabling connection {id}: {stdErrBuffer}");
                return false;
            }

            return true;
        }

        // Wifi methods
        public async Task<bool> CheckRadioStatusAsync()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("nmcli")
                .WithArguments("radio wifi")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error checking radio status: {stdErrBuffer}");
                return false;
            }

            return stdOut.Contains("enabled");
        }

        public async Task<bool> EnableRadioAsync()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments("nmcli radio wifi on")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error enabling radio: {stdErrBuffer}");
                return false;
            }

            return true;
        }

        public async Task<bool> DisableRadioAsync()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments("nmcli radio wifi off")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error disabling radio: {stdErrBuffer}");
                return false;
            }

            return true;
        }

        public async Task<List<WifiScanResult>> GetWifiListAsync()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("nmcli")
                .WithArguments("device wifi list")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error getting wifi list: {stdErrBuffer}");
                return new();
            }

            var lines = stdOut.Split("\n").ToList();
            lines.RemoveAt(0); // Remove the header

            List<WifiScanResult> parsedResult = new();
            foreach (var line in lines)
            {
                try
                {
                    var wifiResult = ParseWifiResultRow(line);
                    if (wifiResult.Bssid == "--" || wifiResult.Ssid == "--" || wifiResult.Security == "--")
                        continue;

                    parsedResult.Add(wifiResult);
                }
                catch (Exception e)
                {
                    _logger?.LogError($"Error parsing wifi result from string {line}: {e.Message}");
                    continue;
                }
            }

            return parsedResult;
        }

        public WifiScanResult ParseWifiResultRow(string row)
        {
            List<string> parts = row.Trim().Split("  ", StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count < 8)
            {
                throw new ArgumentException();
            }

            // this will happen if the SSID has 2 consecutive spaces, unlikely but possible
            if (parts.Count > 8)
            {
                List<string> parsedParts = new List<string>();
                parsedParts.Add(parts[0]);

                // Combine parts for SSID (from index 1 to parts.Count - 7)
                string combinedSsid = string.Join("  ", parts.Skip(1).Take(parts.Count - 7));
                parsedParts.Add(combinedSsid);

                // Add the remaining elements after SSID (the last 6 parts)
                parsedParts.AddRange(parts.Skip(parts.Count - 6));

                parts = parsedParts;
            }

            return new WifiScanResult
            {
                Bssid = parts[0],
                Ssid = parts[1].Trim(),
                Mode = parts[2].Trim(),
                Channel = int.Parse(parts[3]),
                Rate = parts[4].Trim(),
                Signal = int.Parse(parts[5]),
                Bars = parts[6].Trim(),
                Security = parts[7].Trim()
            };
        }

        public async Task<bool> ConnectToWifi(string ssid, string password)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli device wifi connect \"{ssid}\" password \"{password}\"")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error connecting to wifi: {stdErrBuffer}");
                return false;
            }

            return true;
        }

        // Pi Device methods
        public async Task<string> GetDeviceHostnameAsync()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("hostname")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error getting device host name: {stdErrBuffer}");
                return string.Empty;
            }

            return stdOutBuffer.ToString();
        }

        public async Task<bool> SetDeviceHostNameAsync(string hostName)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"hostnamectl set-hostname {hostName}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error setting device host name: {stdErrBuffer}");
                return false;
            }

            return true;
        }

        public async Task<string> GetInterfaceMacAddressAsync(string interfaceName)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("cat")
                .WithArguments($"/sys/class/net/{interfaceName}/address")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error getting interface mac address: {stdErrBuffer}");
                return string.Empty;
            }

            return stdOutBuffer.ToString();
        }

        public async Task RebootPi()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            await Cli.Wrap("sudo")
                .WithArguments("reboot")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();
        }
    }
}
