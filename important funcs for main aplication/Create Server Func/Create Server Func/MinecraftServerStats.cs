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
using Server_General_Funcs;

namespace MinecraftServerStats
{
    class ServerStats
    {
        public static void GetServerInfo(string worldFolderPath, string serverLogPath, string worldNumber, string ipAddress, int JMX_Port, int RCON_Port)
        {
            if (ServerOperator.IsPortInUse(JMX_Port) && ServerOperator.IsPortInUse(RCON_Port))
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
                string upTime = GetServerUptime(serverLogPath, worldNumber);
                Console.WriteLine($"UpTime: {upTime}");
                Console.WriteLine($"--------------------------------------------------");
            }
            else
            {
                Console.WriteLine($"--------------------------------------------------");
                Console.WriteLine("There is no server running!");
                Console.WriteLine($"--------------------------------------------------");
            }
        }

        // Method to get memory usage of Minecraft server
        private static string GetUsedHeapMemory(string host, int port)
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
        private static long GetFolderSize(string folderPath)
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            long totalSize = directoryInfo.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            return totalSize;
        }

        // Method to get list of online players via CoreRCON
        private static string GetOnlinePlayersCount(string address, int port)
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
        private static string GetServerUptime(string logFilePath, string worldNumber)
        {
            // Simulating database call to get software type
            List<object[]> software = dbChanger.SpecificDataFunc($"SELECT software FROM worlds WHERE worldNumber = '{worldNumber}';");

            string softwareType = software[0][0].ToString();

            try
            {
                // Open the log file with FileShare.ReadWrite to allow other processes to access it
                using (var fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream))
                {
                    string line;
                    Regex pattern = null;

                    // Select the appropriate log pattern based on software type
                    if (softwareType == "Vanilla" || softwareType == "Fabric" || softwareType == "Quilt" || softwareType == "Purpur")
                    {
                        pattern = new Regex(@"\[(\d{2}:\d{2}:\d{2})\] \[Server thread/INFO\]: Starting minecraft server version");
                    }
                    else if (softwareType == "Forge")
                    {
                        pattern = new Regex(@"\[(\d{2}[a-zA-Z]{3}\d{4} \d{2}:\d{2}:\d{2}\.\d{3})\] \[.*?/INFO\] \[.*?DedicatedServer.*?\]: Starting minecraft server version");
                    }
                    else if (softwareType == "NeoForge")
                    {
                        pattern = new Regex(@"\[(\d{2}[a-zA-Z]{3}\d{4} \d{2}:\d{2}:\d{2}\.\d{3})\] \[.*?/INFO\] \[.*?DedicatedServer.*?\]: Starting minecraft server version");
                    }
                    else
                    {
                        throw new Exception("Unsupported software type.");
                    }

                    // Read through the log file to find the start timestamp
                    while ((line = reader.ReadLine()) != null)
                    {
                        var match = pattern.Match(line);
                        if (match.Success)
                        {
                            DateTime startTime;
                            string timestampStr = match.Groups[1].Value;

                            if (softwareType == "Vanilla" || softwareType == "Fabric" || softwareType == "Quilt" || softwareType == "Purpur")
                            {
                                // Parse timestamp for Vanilla
                                startTime = DateTime.Today.Add(TimeSpan.Parse(timestampStr));
                            }
                            else if (softwareType == "Forge")
                            {
                                // Parse timestamp for Forge
                                startTime = DateTime.ParseExact(timestampStr, "ddMMMyyyy HH:mm:ss.fff", null);
                            }
                            else if (softwareType == "NeoForge")
                            {
                                // Parse timestamp for NeoForge
                                startTime = DateTime.ParseExact(timestampStr, "ddMMMyyyy HH:mm:ss.fff", null);
                            }
                            else
                            {
                                throw new Exception("Unhandled software type.");
                            }

                            // Calculate uptime
                            DateTime currentTime = DateTime.Now;
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

        // -------------------------------- Help Functions --------------------------------
        private static string FormatTime(int totalSeconds)
        {
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }
    }
}