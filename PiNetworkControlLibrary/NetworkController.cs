using System.Text;
using CliWrap;
using Microsoft.Extensions.Logging;

namespace PiNetworkControl
{
    /// <summary>
    /// This class provides methods to interact with the NetworkManager service using the <c>nmcli</c> command-line tool.
    /// It leverages the <see cref="CliWrap"/> library to execute <c>nmcli</c> commands asynchronously for network configuration tasks.
    /// </summary>
    /// <remarks>
    /// The class offers functionality to manage network connections, enable/disable Wi-Fi radios, retrieve network properties, 
    /// and more. These operations are performed by invoking the <c>nmcli</c> command-line tool, which is commonly used 
    /// for network management on Linux-based systems. While it is also possible to achieve similar functionality using 
    /// <see cref="System.Diagnostics.Process"/> to run the commands, <see cref="CliWrap"/> is used here for its modern, 
    /// easier-to-use API that simplifies handling asynchronous command execution and output handling.
    /// </remarks>
    /// <example>
    /// <code>
    /// var networkManager = new NetworkManager();
    /// bool success = await networkManager.ConnectToWifi("MyNetworkSSID", "MyPassword");
    /// </code>
    /// </example>
    public class NetworkController
    {
        private ILogger<NetworkController>? _logger;

        public NetworkController(ILogger<NetworkController>? logger = null)
        {
            _logger = logger;
            _logger?.LogDebug("NetworkController class created, Logger injected");
        }

        /// <summary>
        /// Asynchronously checks the status of the NetworkManager service on the system.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation. The task result is a boolean indicating
        /// whether the NetworkManager service is active and running. Returns <c>true</c> if the service
        /// is active, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method uses the <see cref="Cli.Wrap"/> library to invoke the <c>systemctl status NetworkManager</c>
        /// command and checks the output to determine if the NetworkManager service is running. If the command
        /// execution fails or returns a non-zero exit code, an error is logged, and the method returns <c>false</c>.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if the command execution encounters an unexpected error that prevents proper execution.
        /// </exception>
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

        /// <summary>
        /// Asynchronously retrieves a list of network devices using the <c>nmcli device</c> command.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation. The task result is a list of <see cref="NetworkDevice"/>
        /// objects representing the network devices on the system. If the operation fails, an empty list is returned.
        /// </returns>
        /// <remarks>
        /// This method uses the <see cref="Cli.Wrap"/> library to execute the <c>nmcli device</c> command to retrieve
        /// information about the network devices on the system. The output is parsed line by line, and for each device,
        /// a <see cref="NetworkDevice"/> object is created with properties such as the device name, type, state, and
        /// associated connection.
        ///
        /// If the command execution fails (i.e., a non-zero exit code is returned), an error is logged and an empty list
        /// is returned.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if the command execution encounters an unexpected error that prevents proper execution.
        /// </exception>
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

        /// <summary>
        /// Asynchronously retrieves the properties of a specific network device using the <c>nmcli device show</c> command.
        /// </summary>
        /// <param name="id">
        /// The identifier (name or MAC address) of the network device for which properties are to be retrieved.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result is a dictionary where the keys are property
        /// names (e.g., "IP4.ADDRESS", "TYPE", etc.) and the values are the corresponding property values for the specified
        /// network device. If the operation fails, an empty dictionary is returned.
        /// </returns>
        /// <remarks>
        /// This method uses the <see cref="Cli.Wrap"/> library to execute the <c>nmcli device show {id}</c> command, where
        /// <c>{id}</c> is the network device's identifier. The command output is parsed line by line, and each property is
        /// extracted into a key-value pair, with the property name as the key and the corresponding value as the dictionary value.
        ///
        /// If the command execution fails (i.e., a non-zero exit code is returned), an error is logged and an empty dictionary
        /// is returned.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if the command execution encounters an unexpected error that prevents proper execution.
        /// </exception>
        public async Task<Dictionary<string, string>> GetDevicePropertiesAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("nmcli")
                .WithArguments($"device show \"{id}\"")
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

        /// <summary>
        /// Asynchronously retrieves a list of network connections using the <c>nmcli connection</c> command.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation. The task result is a list of <see cref="NetworkConnection"/>
        /// objects, each representing a network connection on the system. If the operation fails, an empty list is returned.
        /// </returns>
        /// <remarks>
        /// This method uses the <see cref="Cli.Wrap"/> library to execute the <c>nmcli connection</c> command to retrieve
        /// information about the network connections on the system. The output is parsed line by line, and for each connection,
        /// a <see cref="NetworkConnection"/> object is created with properties such as the connection name, UUID, type, and
        /// the associated device.
        ///
        /// If the command execution fails (i.e., a non-zero exit code is returned), an error is logged, and an empty list
        /// is returned.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if the command execution encounters an unexpected error that prevents proper execution.
        /// </exception>
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

            return ParseConnections(stdOut);
        }

        private List<NetworkConnection> ParseConnections(string input)
        {
            var networkConnections = new List<NetworkConnection>();
            foreach (var line in input.Split("\n").ToList())
            {
                var filteredLine = line.Trim();
                var parts = filteredLine.Split("  ", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 4)
                    continue;

                if (parts[1].Contains("UUID"))
                    continue;

                var connection = new NetworkConnection
                {
                    Name = parts[0].Trim(),
                    UUID = parts[1].Trim(),
                    Type = parts[2].Trim(),
                    Device = parts[3].Trim()
                };
                networkConnections.Add(connection);
            }

            return networkConnections;
        }


        /// <summary>
        /// Asynchronously retrieves the properties of a specific network connection using the <c>nmcli connection show id</c> command.
        /// </summary>
        /// <param name="id">
        /// The identifier (UUID or name) of the network connection for which properties are to be retrieved.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result is a dictionary where the keys are property
        /// names (e.g., "IP4.ADDRESS", "TYPE", etc.) and the values are the corresponding property values for the specified
        /// network connection. If the operation fails, an empty dictionary is returned.
        /// </returns>
        /// <remarks>
        /// This method uses the <see cref="Cli.Wrap"/> library to execute the <c>nmcli connection show id {id}</c> command, where
        /// <c>{id}</c> is the identifier of the network connection. The command output is parsed line by line, and each property
        /// is extracted into a key-value pair, with the property name as the key and the corresponding value as the dictionary value.
        /// Special handling is performed for MAC addresses to join parts of the value that may contain colons (e.g., in MAC addresses).
        ///
        /// If the command execution fails (i.e., a non-zero exit code is returned), an error is logged, and an empty dictionary
        /// is returned.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if the command execution encounters an unexpected error that prevents proper execution.
        /// </exception>
        public async Task<Dictionary<string, string>> GetConnectionPropertiesAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("nmcli")
                .WithArguments($"connection show id \"{id}\"")
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

        /// <summary>
        /// Asynchronously adds a new Ethernet network connection using the <c>nmcli connection add</c> command.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the new Ethernet connection.
        /// </param>
        /// <param name="interfaceId">
        /// The network interface ID (e.g., "eth0", "enp3s0") to associate with the new Ethernet connection.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result is <c>true</c> if the Ethernet connection was
        /// successfully added, or <c>false</c> if an error occurred while adding the connection.
        /// </returns>
        /// <remarks>
        /// This method uses the <see cref="Cli.Wrap"/> library to execute the <c>nmcli connection add</c> command with the
        /// specified connection name and interface ID. If the command execution is successful (i.e., the exit code is 0),
        /// the method returns <c>true</c>. If the command execution fails, an error is logged, and the method returns <c>false</c>.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if the command execution encounters an unexpected error that prevents proper execution.
        /// </exception>
        public async Task<bool> AddEthernetConnectionAsync(string name, string interfaceId)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection add type ethernet con-name \"{name}\" ifname {interfaceId}")
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

        /// <summary>
        /// Asynchronously adds a new Wi-Fi network connection using the <c>nmcli connection add</c> command.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the new Wi-Fi connection.
        /// </param>
        /// <param name="interfaceId">
        /// The network interface ID (e.g., "wlan0", "wlp2s0") to associate with the new Wi-Fi connection.
        /// </param>
        /// <param name="ssid">
        /// The SSID (network name) of the Wi-Fi network to which the connection will be made.
        /// </param>
        /// <param name="password">
        /// The password (PSK) for the Wi-Fi network. If the network does not require a password, an empty string can be passed.
        /// </param>
        /// <param name="keyManagement">
        /// The key management type for the Wi-Fi network (e.g., "none", "ieee8021x", "wpa-none", "wpa-psk", "wpa-eap"). This specifies the security protocol used by the network.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result is <c>true</c> if the Wi-Fi connection was
        /// successfully added, or <c>false</c> if an error occurred while adding the connection.
        /// </returns>
        /// <remarks>
        /// This method uses the <see cref="Cli.Wrap"/> library to execute the <c>nmcli connection add</c> command with the
        /// specified connection name, interface ID, SSID, password, and key management type. If the command execution is successful
        /// (i.e., the exit code is 0), the method returns <c>true</c>. If the command execution fails, an error is logged, and the method
        /// returns <c>false</c>.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if the command execution encounters an unexpected error that prevents proper execution.
        /// </exception>
        public async Task<bool> AddWifiConnectionAsync(string name, string interfaceId, string ssid, string password, string keyManagement)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection add type wifi con-name \"{name}\" ifname {interfaceId} ssid \"{ssid}\" +802-11-wireless-security.key-mgmt \"{keyManagement}\" +802-11-wireless-security.psk \"{password}\"")
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

        /// <summary>
        /// Asynchronously modifies a property of a specific network connection using the <c>nmcli connection modify</c> command.
        /// </summary>
        /// <param name="connectionId">
        /// The identifier (name or UUID) of the network connection to modify.
        /// </param>
        /// <param name="property">
        /// The name of the property to modify (e.g., "ipv4.addresses", "ipv4.gateway").
        /// </param>
        /// <param name="value">
        /// The new value to set for the specified property.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result is <c>true</c> if the connection property was
        /// successfully modified, or <c>false</c> if an error occurred while modifying the property.
        /// </returns>
        /// <remarks>
        /// This method uses the <see cref="Cli.Wrap"/> library to execute the <c>nmcli connection modify</c> command to modify
        /// a specific property of the network connection identified by <paramref name="connectionId"/>. After modifying the property,
        /// the method checks whether the value was set correctly by retrieving the new value for the property and comparing it
        /// with the desired value. If the modification fails or the value doesn't match, an error is logged and the method
        /// returns <c>false</c>.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if the command execution encounters an unexpected error that prevents proper execution.
        /// </exception>
        public async Task<bool> ModifyConnectionPropertyAsync(string connectionId, string property, string value)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection modify \"{connectionId}\" {property} {value}")
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

        /// <summary>
        /// Asynchronously modifies multiple properties of a connection.
        /// </summary>
        /// <param name="connectionId">The identifier of the connection whose properties are to be modified.</param>
        /// <param name="properties">A dictionary of property names and their corresponding values to be modified.</param>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> that represents the asynchronous operation. 
        /// The result is <c>true</c> if all properties were modified successfully, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method will attempt to modify each property in the provided dictionary. 
        /// If any modification fails (i.e., <see cref="ModifyConnectionPropertyAsync"/> returns <c>false</c>), 
        /// the method will immediately return <c>false</c>.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously retrieves the value of a specific property for a given connection.
        /// </summary>
        /// <param name="connectionId">The identifier of the connection whose property is to be retrieved.</param>
        /// <param name="property">The name of the property whose value is being requested.</param>
        /// <returns>
        /// A <see cref="Task{String}"/> that represents the asynchronous operation. 
        /// The result is the value of the specified property, or an empty string if an error occurs or the property cannot be retrieved.
        /// </returns>
        /// <remarks>
        /// This method executes a CLI command using <c>nmcli</c> to fetch the specified property for the given connection. 
        /// If the command fails or if the property value cannot be parsed correctly, an error is logged, and the method returns an empty string.
        /// </remarks>
        public async Task<string> GetConnectionPropertyAsync(string connectionId, string property)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli --terse --fields {property} con show \"{connectionId}\"")
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

        /// <summary>
        /// Asynchronously deletes a network connection by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the connection to be deleted.</param>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> that represents the asynchronous operation. 
        /// The result is <c>true</c> if the connection was successfully deleted, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method executes a CLI command using <c>nmcli</c> to delete the specified network connection.
        /// If the command fails (non-zero exit code), an error message is logged, and the method returns <c>false</c>.
        /// If the operation succeeds, it returns <c>true</c>.
        /// </remarks>
        public async Task<bool> DeleteConnectionAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection delete \"{id}\"")
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

        /// <summary>
        /// Asynchronously enables (brings up) a network connection by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the connection to be enabled.</param>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> that represents the asynchronous operation. 
        /// The result is <c>true</c> if the connection was successfully enabled, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method executes a CLI command using <c>nmcli</c> to bring up the specified network connection.
        /// If the command fails (non-zero exit code), an error message is logged, and the method returns <c>false</c>.
        /// If the operation succeeds, it returns <c>true</c>.
        /// </remarks>
        public async Task<bool> EnableConnectionAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection up \"{id}\"")
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

        /// <summary>
        /// Asynchronously disables (brings down) a network connection by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the connection to be disabled.</param>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> that represents the asynchronous operation. 
        /// The result is <c>true</c> if the connection was successfully disabled, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method executes a CLI command using <c>nmcli</c> to bring down the specified network connection.
        /// If the command fails (non-zero exit code), an error message is logged, and the method returns <c>false</c>.
        /// If the operation succeeds, it returns <c>true</c>.
        /// </remarks>
        public async Task<bool> DisableConnectionAsync(string id)
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli connection down \"{id}\"")
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

        /// <summary>
        /// Asynchronously checks the status of the Wi-Fi radio (enabled or disabled).
        /// </summary>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> that represents the asynchronous operation. 
        /// The result is <c>true</c> if the Wi-Fi radio is enabled, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method executes a CLI command using <c>nmcli</c> to check the status of the Wi-Fi radio.
        /// If the command fails (non-zero exit code), an error message is logged, and the method returns <c>false</c>.
        /// If the radio is enabled, the method returns <c>true</c>. Otherwise, it returns <c>false</c>.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously enables the Wi-Fi radio, turning it on.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> that represents the asynchronous operation. 
        /// The result is <c>true</c> if the Wi-Fi radio was successfully enabled, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method executes a CLI command using <c>nmcli</c> to enable the Wi-Fi radio.
        /// If the command fails (non-zero exit code), an error message is logged, and the method returns <c>false</c>.
        /// If the operation succeeds, it returns <c>true</c>.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously disables the Wi-Fi radio, turning it off.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> that represents the asynchronous operation. 
        /// The result is <c>true</c> if the Wi-Fi radio was successfully disabled, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method executes a CLI command using <c>nmcli</c> to disable the Wi-Fi radio.
        /// If the command fails (non-zero exit code), an error message is logged, and the method returns <c>false</c>.
        /// If the operation succeeds, it returns <c>true</c>.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously retrieves a list of available Wi-Fi networks (scan results).
        /// </summary>
        /// <returns>
        /// A <see cref="Task{List{WifiScanResult}}"/> that represents the asynchronous operation. 
        /// The result is a list of <see cref="WifiScanResult"/> objects, each representing a Wi-Fi network found during the scan.
        /// An empty list is returned if the scan fails or no Wi-Fi networks are found.
        /// </returns>
        /// <remarks>
        /// This method executes a CLI command using <c>nmcli</c> to scan for available Wi-Fi networks.
        /// It processes the output of the scan, parses each network's information (SSID, BSSID, security type), 
        /// and returns a list of valid Wi-Fi network results.
        /// If the command fails or the result cannot be parsed, an error is logged, and the method continues processing the rest of the results.
        /// </remarks>
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

            //Active wifi connection has * in front of it, remove it for parsing
            //stdOut.Replace("*", " ");

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

        /// <summary>
        /// Parses a row of Wi-Fi scan results into a <see cref="WifiScanResult"/> object.
        /// </summary>
        /// <param name="row">A single row of the output from a Wi-Fi scan, typically containing information about a Wi-Fi network.</param>
        /// <returns>
        /// A <see cref="WifiScanResult"/> object representing the parsed Wi-Fi network information (BSSID, SSID, mode, channel, etc.).
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the row does not contain enough parts to parse correctly, or if the row is improperly formatted.
        /// </exception>
        /// <remarks>
        /// This method splits the input row into parts, handles cases where the SSID contains multiple spaces, 
        /// and maps the parsed data into a structured <see cref="WifiScanResult"/> object. 
        /// If the row cannot be parsed due to missing or incorrect data, an exception will be thrown.
        /// </remarks>
        public WifiScanResult ParseWifiResultRow(string row)
        {
            bool isActive = row.StartsWith("*");
            if (isActive)
                row = row.Substring(1);

            List<string> parts = row.Trim().Split("  ", StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count < 8)
            {
                throw new ArgumentException();
            }

            // this will happen if the SSID has 2 consecutive spaces, unlikely but possible
            if (parts.Count > 8)
            {
                List<string> parsedParts = [parts[0]];

                // Combine parts for SSID (from index 1 to parts.Count - 7)
                string combinedSsid = string.Join("  ", parts.Skip(1).Take(parts.Count - 7));
                parsedParts.Add(combinedSsid);

                // Add the remaining elements after SSID (the last 6 parts)
                parsedParts.AddRange(parts.Skip(parts.Count - 6));

                parts = parsedParts;
            }

            return new WifiScanResult
            {
                IsActive = isActive,
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

        /// <summary>
        /// Asynchronously connects to a Wi-Fi network using the specified SSID and password.
        /// </summary>
        /// <param name="ssid">The SSID (name) of the Wi-Fi network to connect to.</param>
        /// <param name="password">The password for the Wi-Fi network.</param>
        /// <param name="hidden">Indicates whether the Wi-Fi network is hidden (defaults to <c>false</c>).</param>
        /// <param name="connectionName">An optional name to assign to the new connection. If not specified, a default name will be used.</param>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> that represents the asynchronous operation. 
        /// The result is <c>true</c> if the connection was successful, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method executes a CLI command using <c>nmcli</c> to connect to the specified Wi-Fi network.
        /// If the connection is hidden, the <paramref name="hidden"/> parameter should be set to <c>true</c>.
        /// If a custom name is provided for the connection, it will be used instead of the default network name.
        /// If the command fails (non-zero exit code), an error message is logged, and the method returns <c>false</c>.
        /// If the operation succeeds, it returns <c>true</c>.
        /// </remarks>
        public async Task<bool> ConnectToWifi(string ssid, string password, bool hidden = false, string connectionName = "")
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments($"nmcli device wifi connect \"{ssid}\" password \"{password}\" hidden {(hidden == false ? "no" : "yes")}{(connectionName == "" ? "" : " name ")}\"{connectionName}\"")
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

        /// <summary>
        /// Asynchronously retrieves the hostname of the device.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{String}"/> that represents the asynchronous operation. 
        /// The result is the device's hostname as a string, or an empty string if the operation fails.
        /// </returns>
        /// <remarks>
        /// This method executes a command using <c>hostname</c> to retrieve the device's hostname.
        /// If the command fails (non-zero exit code), an error message is logged, and the method returns an empty string.
        /// The output is cleaned up to remove any carriage return or newline characters before returning the hostname.
        /// </remarks>
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

            return stdOutBuffer.ToString().Replace("\r", "").Replace("\n", "");
        }

        /// <summary>
        /// Asynchronously sets the device's hostname to the specified value.
        /// </summary>
        /// <param name="hostName">The new hostname to be set for the device.</param>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> that represents the asynchronous operation. 
        /// The result is <c>true</c> if the hostname was successfully set, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method executes a command using <c>hostnamectl</c> to set the device's hostname.
        /// If the command fails (non-zero exit code), an error message is logged, and the method returns <c>false</c>.
        /// If the operation succeeds, it returns <c>true</c>.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously retrieves the MAC address of a specified network interface.
        /// </summary>
        /// <param name="interfaceName">The name of the network interface (e.g., "eth0", "wlan0") whose MAC address is to be retrieved.</param>
        /// <returns>
        /// A <see cref="Task{String}"/> that represents the asynchronous operation. 
        /// The result is the MAC address of the specified interface as a string, or an empty string if the operation fails.
        /// </returns>
        /// <remarks>
        /// This method executes a command to read the MAC address of the specified network interface from the system's file system.
        /// If the command fails (non-zero exit code), an error message is logged, and the method returns an empty string.
        /// The output is cleaned up to remove any carriage return or newline characters before returning the MAC address.
        /// </remarks>
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

            return stdOutBuffer.ToString().Replace("\r", "").Replace("\n", "");
        }

        /// <summary>
        /// Asynchronously reboots the Raspberry Pi device, and logs an error if the reboot fails.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous operation. 
        /// This method does not return a value, but performs the reboot operation and logs an error if the command fails.
        /// </returns>
        /// <remarks>
        /// This method executes a command using <c>sudo reboot</c> to restart the device.
        /// If the reboot command fails (non-zero exit code), an error message is logged, but the reboot process is not interrupted.
        /// The method completes once the reboot command has been executed, regardless of success or failure.
        /// </remarks>
        public async Task RebootPi()
        {
            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();
            var result = await Cli.Wrap("sudo")
                .WithArguments("reboot")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                _logger?.LogError($"Error rebooting device: {stdErrBuffer}");
            }
        }
    }
}
