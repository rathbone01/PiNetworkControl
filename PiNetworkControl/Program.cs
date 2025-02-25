namespace PiNetworkControl
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string Connections = File.ReadAllText("connections.txt");
            var networkConnections = ParseConnections(Connections);
            var test = new NetworkControllerTestClass();
            test.Run().Wait();
        }


        private static List<NetworkConnection> ParseConnections(string input)
        {
            var networkConnections = new List<NetworkConnection>();
            var lines = input.Split("\n").ToList();

            foreach (var line in lines)
            {
                var fline = line.Trim();
                var parts = fline.Split("  ", StringSplitOptions.RemoveEmptyEntries);
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
    }
}
