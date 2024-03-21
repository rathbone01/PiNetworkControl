using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkManagerWrapperLibrary.NetworkController;

namespace PiNetworkControl
{
    public class NetworkControllerTestClass
    {
        public NetworkControllerTestClass() { }

        public async Task Run()
        {
            NetworkController networkController = new NetworkController();

            var NmcliRunning = await networkController.CheckNetworkManagerServiceExecutionAsync();

            if (NmcliRunning)
            {
                Console.WriteLine("NetworkManager is running");
            }
            else
            {
                Console.WriteLine("NetworkManager is not running");
            }

            var devices = await networkController.GetDevicesAsync();
            foreach (var device in devices)
            {
                Console.WriteLine($"Device: {device.Device}, Type: {device.Type}, State: {device.State}, Connection: {device.Connection}");

                var properties = await networkController.GetDevicePropertiesAsync(device.Device);

                foreach (var property in properties)
                {
                    Console.WriteLine($"Property: {property.Key}, Value: {property.Value}");
                }
            }

            var connections = await networkController.GetConnectionsAsync();
            foreach (var connection in connections)
            {
                Console.WriteLine($"Name: {connection.Name}, UUID: {connection.UUID}, Type: {connection.Type}, Device: {connection.Device}");

                var properties = await networkController.GetConnectionPropertiesAsync(connection.Name);
                foreach (var property in properties)
                {
                    Console.WriteLine($"Property: {property.Key}, Value: {property.Value}");
                }
            }

            var modResult = await networkController.ModifyConnectionAsync("rendition", "ipv4.addresses", "192.168.2.100/24");

            if (modResult)
            {
                Console.WriteLine("Connection modified");
            }
            else
            {
                Console.WriteLine("Connection not modified");
            }
        }
    }
}
