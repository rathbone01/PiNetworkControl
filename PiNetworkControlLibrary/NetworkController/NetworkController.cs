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
            var stdErr = stdErrBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErr}");
            }

            return stdOut.Contains("Active: active (running)");
        }

        public async Task<List<NetworkDevice>> GetNetworkDevicesAsync()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();

            var result = await Cli.Wrap("nmcli")
                .WithArguments("device")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            var stdErr = stdErrBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErr}");
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

        public async Task<Dictionary<string, string>> GetNetworkDevicePropertiesAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();

            var result = await Cli.Wrap("nmcli")
                .WithArguments($"device show {id}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            var stdErr = stdErrBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErr}");
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

        public async Task<List<NetworkConnection>> GetNetworkConnectionsAsync()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();

            var result = await Cli.Wrap("nmcli")
                .WithArguments("connection")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            var stdErr = stdErrBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErr}");
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

        public async Task<Dictionary<string, string>> GetNetworkConnectionPropertiesAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();

            var result = await Cli.Wrap("nmcli")
                .WithArguments($"connection show id {id}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            var stdErr = stdErrBuffer.ToString();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Error: {stdErr}");
            }

            var properties = new Dictionary<string, string>();

            var lines = stdOut.Split("\n").ToList();
            foreach (var line in lines)
            {
                var parts = line.Split(":", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    // Mac address has a colon in it, so we need to join the parts
                    properties.Add(parts[0].Trim(), string.Join(":", parts.Skip(1)));
                }
            }

            return properties;
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

        // Wifi will be added later
    }
}
