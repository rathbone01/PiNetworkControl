using NetworkManagerWrapperLibrary.ProcessRunner;

namespace PiNetworkControl
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var test = new NetworkControllerTestClass();

            test.Run();
        }
    }
}
