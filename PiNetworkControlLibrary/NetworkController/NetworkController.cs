// This class relies on the CliWrap library to run the nmcli command line tool to interact with NetworkManager.
// This could also be achieved using the System.Diagnostics.Process class, but CliWrap is a more modern and easier to use library.
//
// https://www.baeldung.com/linux/network-manager

using NetworkManagerWrapperLibrary.Models;
using System.Text;
using CliWrap;

namespace NetworkManagerWrapperLibrary.NetworkController
{
    public class NetworkController
    {
        public NetworkController()
        {
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
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
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
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
            }

            var networkDevices = new List<NetworkDevice>();
            var lines = stdOut.Split("\n").ToList();
            for (int i = 1; i < lines.Count() - 1; i++)
            {
                var line = lines[i];
                var parts = line.Split("  ", StringSplitOptions.RemoveEmptyEntries);
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
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
            }

            var properties = new Dictionary<string, string>();
            var lines = stdOut.Split("\n").ToList();
            foreach (var line in lines)
            {
                var parts = line.Split(":", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    properties.Add(parts[0].Trim(), parts[1].Trim());
                }
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
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
            }

            var networkConnections = new List<NetworkConnection>();
            var lines = stdOut.Split("\n").ToList();

            for (int i = 1; i < lines.Count() - 1; i++)
            {
                var line = lines[i];
                var parts = line.Split("  ", StringSplitOptions.RemoveEmptyEntries);
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
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
            }

            var properties = new Dictionary<string, string>();

            var lines = stdOut.Split("\n").ToList();
            foreach (var line in lines)
            {
                var parts = line.Split(":", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    // Mac address has a colon in it, so we need to join the parts
                    properties.Add(parts[0].Trim(), string.Join(":", parts.Skip(1)).Trim());
                }
            }

            return properties;
        }

        public async Task AddConnection(string type, string name, string interfaceId)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();

            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection add type {type} con-name {name} ifname {interfaceId}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
            }
        }

        public async Task<bool> ModifyConnectionAsync(string connectionId, string property, string value)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();

            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection modify {connectionId} {property} {value}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
            }

            return true;
        }

        public async Task<bool> DeleteConnectionAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();

            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection delete {id}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
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
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
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
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
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
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
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
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
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
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
            }

            return true;
        }

        public async Task<List<string>> GetWifiListAsync()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();

            var result = await Cli.Wrap("nmcli")
                .WithArguments("device wifi list")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
            }

            return stdOut.Split("\n").ToList();
        }

        public async Task<bool> ConnectToWifi(string ssid, string password)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();

            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli device wifi connect \"{ssid}\" password \"{password}\"")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErrBuffer}");
            }

            return true;
        }
    }
}
