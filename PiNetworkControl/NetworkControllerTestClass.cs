using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiNetworkControl
{
    public class NetworkControllerTestClass
    {
        NetworkController networkController;

        public NetworkControllerTestClass()
        {
            networkController = new NetworkController();
        }

        public async Task Run()
        {
            while (true)
            {
                PrintMainMenu();
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await NetworkInterfaces();
                        break;
                    case "2":
                        await NetworkConnections();
                        break;
                    case "3":
                        await Wifi();
                        break;
                    case "4":
                        await Hostname();
                        break;
                    case "5":
                        await TestForDotNet();
                        break;
                    case "6":
                        return;
                    default:
                        Console.WriteLine("Invalid input");
                        break;
                }
            }
        }

        public void PrintMainMenu()
        {
            Console.WriteLine("Main Menu");
            Console.WriteLine("1. Network Devices");
            Console.WriteLine("2. Network Connections");
            Console.WriteLine("3. Wifi");
            Console.WriteLine("4. Hostname");
            Console.WriteLine("5. Test");
            Console.WriteLine("6. Exit");
        }

        public async Task TestForDotNet()
        {
            var boardInfo = LoadBoardInfo();
            foreach (var (key, value) in boardInfo)
            {
                Console.WriteLine($"{key}: {value}");
            }
        }

        public async Task Hostname()
        {
            var hostCtl = await networkController.GetDeviceHostnameCtlAsync();
            Console.WriteLine($"Hostname ctl: {hostCtl}");

            var hostHosts = await networkController.GetDeviceHostnameHostsAsync();
            Console.WriteLine($"Hostname hosts: {hostHosts}");

            Console.WriteLine("Enter a new hostname or enter back to go back.");
            var input = Console.ReadLine();
            if (input == "back" || input is null)
            {
                return;
            }

            if (await networkController.SetDeviceHostnameAsync(input!))
            {
                Console.WriteLine("Hostname set successfully");
            }
            else
            {
                Console.WriteLine("Failed to set hostname");
            }
        }

        public async Task NetworkInterfaces()
        {
            var networkDevices = await networkController.GetDevicesAsync();
            foreach (var networkDevice in networkDevices)
            {
                Console.WriteLine($"{networkDevice.Device} {networkDevice.Type} {networkDevice.State} {networkDevice.Connection}");
            }

            Console.WriteLine();
            Console.WriteLine("Enter a network device name for properties, enter back to go back.");

            while (true)
            {
                var input = Console.ReadLine();
                if (input == "back")
                {
                    return;
                }

                var networkDevice = networkDevices.FirstOrDefault(x => x.Device == input);
                if (networkDevice == null)
                {
                    Console.WriteLine("Invalid network device name");
                    continue;
                }

                var properties = await networkController.GetDevicePropertiesAsync(networkDevice.Device);
                foreach (var property in properties)
                {
                    Console.WriteLine($"{property.Key}: {property.Value}");
                }
            }
        }

        public async Task NetworkConnections()
        {
            var networkConnections = await networkController.GetConnectionsAsync();
            foreach (var networkConnection in networkConnections)
            {
                Console.WriteLine($"{networkConnection.Name} {networkConnection.UUID} {networkConnection.Type} {networkConnection.Device}");
            }

            Console.WriteLine();
            Console.WriteLine("Enter a network connection name for properties or enter back to go back.");
            Console.WriteLine("enter 'delete' to delete all connections");


            while (true)
            {
                var input = Console.ReadLine();
                if (input == "back")
                {
                    return;
                }

                if (input == "delete")
                {
                    var connections = await networkController.GetConnectionsAsync();
                    foreach (var connection in connections)
                    {
                        if (await networkController.DeleteConnectionAsync(connection.Name))
                            Console.WriteLine($"Deleted connection {connection.Name}");
                        else
                            Console.WriteLine($"Failed to delete connection {connection.Name}");
                    }

                    return;
                }

                var networkConnection = networkConnections.FirstOrDefault(x => x.Name == input);
                if (networkConnection == null)
                {
                    Console.WriteLine("Invalid network connection name");
                    continue;
                }

                var properties = await networkController.GetConnectionPropertiesAsync(networkConnection.Name);
                foreach (var property in properties)
                {
                    Console.WriteLine($"{property.Key}: {property.Value}");
                }
            }
        }

        public async Task Wifi()
        {
            Console.WriteLine("Wifi Menu");
            Console.WriteLine("1. Radio Status");
            Console.WriteLine("2. Get Wifi List");
            Console.WriteLine("3. Go back");

            while (true)
            {
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await RadioStatus();
                        break;
                    case "2":
                        await GetWifiList();
                        break;
                    case "3":
                        return;
                    default:
                        Console.WriteLine("Invalid input");
                        break;
                }
            }
        }


        public async Task RadioStatus()
        {
            var radioStatus = await networkController.CheckRadioStatusAsync();

            if (radioStatus)
            {
                Console.WriteLine("Wifi radio is on");
            }
            else
            {
                Console.WriteLine("Wifi radio is off");
            }
        }

        public async Task GetWifiList()
        {
            var wifiList = await networkController.GetWifiListAsync();
            foreach (var wifi in wifiList)
            {
                Console.WriteLine(wifi);
            }
        }

        public Dictionary<string, string> LoadBoardInfo()
        {
            try
            {
                const string filePath = "/proc/cpuinfo";

                var cpuInfo = File.ReadAllLines(filePath);
                var settings = new Dictionary<string, string>();
                var suffix = string.Empty;

                foreach (var line in cpuInfo)
                {
                    var separator = line.IndexOf(':');

                    if (!string.IsNullOrWhiteSpace(line) && separator > 0)
                    {
                        var key = line.Substring(0, separator).Trim();
                        var val = line.Substring(separator + 1).Trim();
                        if (string.Equals(key, "processor", StringComparison.InvariantCultureIgnoreCase))
                        {
                            suffix = "." + val;
                        }

                        settings.Add(key + suffix, val);
                    }
                    else
                    {
                        suffix = string.Empty;
                    }
                }

                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load board info: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }
    }
}
