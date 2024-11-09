using PiNetworkControl;

namespace PiNetworkControlTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestWifiParser1()
        {
            NetworkController controller = new NetworkController();
            string input = "6C:5A:B0:40:E1:9C  Test  SSID                 Infra  2     195 Mbit/s  100     1111  WPA2";
            WifiScanResult result = controller.ParseWifiResultRow(input);

            Assert.AreEqual("6C:5A:B0:40:E1:9C", result.Bssid);
            Assert.AreEqual("Test  SSID", result.Ssid);
            Assert.AreEqual("Infra", result.Mode);
            Assert.AreEqual(2, result.Channel);
            Assert.AreEqual("195 Mbit/s", result.Rate);
            Assert.AreEqual(100, result.Signal);
            Assert.AreEqual("1111", result.Bars);
            Assert.AreEqual("WPA2", result.Security);
        }

        [TestMethod]
        public void TestWifiParser2()
        {
            NetworkController controller = new NetworkController();
            string input = "6C:5A:B0:40:E1:9C     Test  SSID                   Infra  2       195 Mbit/s  100          1111   WPA2";
            WifiScanResult result = controller.ParseWifiResultRow(input);

            Assert.AreEqual("6C:5A:B0:40:E1:9C", result.Bssid);
            Assert.AreEqual("Test  SSID", result.Ssid);
            Assert.AreEqual("Infra", result.Mode);
            Assert.AreEqual(2, result.Channel);
            Assert.AreEqual("195 Mbit/s", result.Rate);
            Assert.AreEqual(100, result.Signal);
            Assert.AreEqual("1111", result.Bars);
            Assert.AreEqual("WPA2", result.Security);
        }

        [TestMethod]
        public void TestWifiParser3()
        {
            NetworkController controller = new NetworkController();
            string input = "6C:5A:B0:40:E1:9C  NormalSsid123                 Infra  2     195 Mbit/s  100     1111  WPA2";
            WifiScanResult result = controller.ParseWifiResultRow(input);

            Assert.AreEqual("6C:5A:B0:40:E1:9C", result.Bssid);
            Assert.AreEqual("NormalSsid123", result.Ssid);
            Assert.AreEqual("Infra", result.Mode);
            Assert.AreEqual(2, result.Channel);
            Assert.AreEqual("195 Mbit/s", result.Rate);
            Assert.AreEqual(100, result.Signal);
            Assert.AreEqual("1111", result.Bars);
            Assert.AreEqual("WPA2", result.Security);
        }

        [TestMethod]
        public void TestWifiParser4()
        {
            NetworkController controller = new NetworkController();
            string input = "         42:75:C3:00:22:3A  testssid                   Infra  6     540 Mbit/s  100     1111  WPA2             ";
            WifiScanResult result = controller.ParseWifiResultRow(input);

            Assert.AreEqual("42:75:C3:00:22:3A", result.Bssid);
            Assert.AreEqual("testssid", result.Ssid);
            Assert.AreEqual("Infra", result.Mode);
            Assert.AreEqual(6, result.Channel);
            Assert.AreEqual("540 Mbit/s", result.Rate);
            Assert.AreEqual(100, result.Signal);
            Assert.AreEqual("1111", result.Bars);
            Assert.AreEqual("WPA2", result.Security);
        }
    }
}