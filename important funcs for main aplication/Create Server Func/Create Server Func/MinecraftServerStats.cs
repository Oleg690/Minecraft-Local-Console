using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.IO;
using System.Linq;
using System.Net;
using CoreRCON;
using databaseChanger;
using javax.management;
using javax.management.remote;
using javax.management.openmbean;
using System.Text.RegularExpressions;
using com.sun.org.glassfish.gmbal;
using java.beans;
using MCQuery;

namespace MinecraftServerStats
{
    class ServerStats
    {
        private static readonly HashSet<string> OnlinePlayers = new HashSet<string>();

        // Method to get memory usage of Minecraft server
        public static string GetUsedHeapMemory(string host, int port)
        {
            try
            {
                // Construct the JMX service URL
                string url = $"service:jmx:rmi:///jndi/rmi://{host}:{port}/jmxrmi";
                JMXServiceURL serviceURL = new JMXServiceURL(url);

                // Connect to the JMX server
                using (JMXConnector connector = JMXConnectorFactory.connect(serviceURL)) // Use using statement for proper disposal
                {
                    MBeanServerConnection mBeanConnection = connector.getMBeanServerConnection();

                    // Access MemoryMXBean
                    ObjectName memoryMXBean = new ObjectName("java.lang:type=Memory");

                    // Retrieve heap memory usage
                    CompositeData heapMemoryUsage = (CompositeData)mBeanConnection.getAttribute(memoryMXBean, "HeapMemoryUsage");

                    // Correctly handle the Java Long to .NET long conversion
                    long usedHeap = ((java.lang.Long)heapMemoryUsage.get("used")).longValue();
                    long maxHeap = ((java.lang.Long)heapMemoryUsage.get("max")).longValue();

                    long freeHeap = maxHeap - usedHeap;

                    double freeHeapPercentage = ((double)freeHeap / maxHeap) * 100;

                    return $"{usedHeap / (1024.0 * 1024.0):0} MB ({freeHeapPercentage:0}% free)";
                }
            }
            catch (System.Exception ex)
            {
                return $"GetUsedHeapMemory Error: {ex.Message}";
            }
        }

        // Method to get folder size of the Minecraft world
        public static long GetFolderSize(string folderPath)
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            long totalSize = directoryInfo.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            return totalSize;
        }

        // Method to get list of online players via CoreRCON
        public static string GetOnlinePlayersCount(string address, int port)
        {
            MCServer server = new MCServer(address, port);
            ServerStatus status = server.Status();
            // double ping = server.Ping();
            // Console.WriteLine($"Ping:   {ping}ms");
            long playersOnline = status.Players.Online;
            long maxPlayers = status.Players.Max;

            return $"{playersOnline} / {maxPlayers}";
        }

        // Method to get server uptime via latest.log
        public static string GetServerUptime(string logFilePath)
        {
            try
            {
                // Open the log file with FileShare.ReadWrite to allow other processes to access it
                using (var fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream))
                {
                    string line;
                    // Regular expression to match the line where the server starts
                    string pattern = @"\[\d{2}:\d{2}:\d{2}\] \[Server thread/INFO\]: Starting minecraft server version (\d+\.\d+)";
                    while ((line = reader.ReadLine()) != null)
                    {
                        var match = Regex.Match(line, pattern);
                        if (match.Success)
                        {
                            // Parse the timestamp from the log file
                            string timestampStr = line.Substring(1, 8);  // Extracts the HH:mm:ss part
                            DateTime startTime = DateTime.ParseExact(timestampStr, "HH:mm:ss", null);
                            DateTime currentTime = DateTime.Now;

                            // Calculate uptime in seconds
                            TimeSpan uptime = currentTime - startTime;
                            return FormatTime((int)uptime.TotalSeconds);
                        }
                    }
                }

                throw new Exception("No start time found in the log file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return "-1s";
            }
        }

        public static void GetServerInfo(string worldFolderPath, string serverLogPath, string worldNumber, string ipAddress, int JMX_Port, int RCON_Port)
        {
            Console.WriteLine($"--------------------------------------------------");
            // Get memory usage
            var memoryUsage = GetUsedHeapMemory(ipAddress, JMX_Port);
            Console.WriteLine($"Memory Usage: {memoryUsage}");

            // Get world folder size
            long worldSize = GetFolderSize(worldFolderPath);
            Console.WriteLine($"World Folder Size: {worldSize / 1024.0 / 1024.0:F2} MB");

            // Get online players
            string PlayersResult = GetOnlinePlayersCount(ipAddress, 25565);
            Console.WriteLine($"Players Online: {PlayersResult};");

            // Get server uptime
            string upTime = GetServerUptime(serverLogPath);
            Console.WriteLine($"UpTime: {upTime}");
            Console.WriteLine($"--------------------------------------------------");
            Console.WriteLine("");
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------

        private static string FormatTime(int seconds)
        {
            // Calculate hours, minutes, and remaining seconds
            int hours = seconds / 3600;
            int minutes = (seconds % 3600) / 60;
            int remainingSeconds = seconds % 60;

            // Return the formatted string
            return string.Format("{0:D2}h:{1:D2}m:{2:D2}s", hours, minutes, remainingSeconds);
        }
    }
}