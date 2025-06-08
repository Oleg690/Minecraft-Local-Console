using java.util;
using javax.management;
using javax.management.openmbean;
using javax.management.remote;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;

namespace Minecraft_Console
{
    [SupportedOSPlatform("windows")]
    class ServerStats
    {
        public static async Task GetServerInfo(ViewModel viewModel, string worldFolderPath, string worldNumber, object[] serverData, object[] userData, string ipAddress, int JMX_Port, int RCON_Port, int Server_Port, string user = "", string psw = "")
        {
            if (MainWindow.serverRunning == true)
            {
                Stopwatch stopwatch = new();
                stopwatch.Start();

                // Get server user and password for JMX
                user = userData[0].ToString() ?? string.Empty;
                psw = userData[1].ToString() ?? string.Empty;

                // Get memory usage
                string memoryUsage = GetUsedHeapMemory(ipAddress, JMX_Port, user, psw)[0];
                long getUsedHeapMemoryTime = stopwatch.ElapsedMilliseconds;
                string[] memoryData = [memoryUsage, getUsedHeapMemoryTime.ToString()];
                //viewModel.MemoryUsage = memoryData[0];
                stopwatch.Restart();

                // Get world folder size
                string worldSize = GetFolderSize(worldFolderPath);
                long getFolderSizeTime = stopwatch.ElapsedMilliseconds;
                string[] worldData = [worldSize, getFolderSizeTime.ToString()];
                //viewModel.WorldSize = worldData[0];
                stopwatch.Restart();

                // Get online players
                string version = (string)serverData[0];
                string maxPlayers = (string)serverData[1];

                string playersResult = GetOnlinePlayersCount(ipAddress, Server_Port, GetProtocolVersion(version));
                long getOnlinePlayersCountTime = stopwatch.ElapsedMilliseconds;
                string[] playersData = [playersResult, getOnlinePlayersCountTime.ToString()];
                //viewModel.PlayersOnline = $"{playersData[0]} / {maxPlayers}";
                stopwatch.Restart();

                // Get server uptime
                string upTime = GetServerUptime(worldFolderPath);
                long getServerUpTime = stopwatch.ElapsedMilliseconds;
                string[] uptimeData = [upTime, getServerUpTime.ToString()];
                //viewModel.UpTime = uptimeData[0];
                stopwatch.Restart();

                // Get console output
                string consoleOutput = GetConsoleOutput(worldFolderPath);
                long consoleOutputTime = stopwatch.ElapsedMilliseconds;
                string[] consoleOutputData = [consoleOutput, consoleOutputTime.ToString()];
                //viewModel.Console = consoleOutputData[0];
                stopwatch.Restart();

                //CodeLogger.ConsoleLog($"Updating VM for {worldNumber}: {viewModel.MemoryUsage}");

                // Get elapsed time
                long totalElapsedTime = getUsedHeapMemoryTime + getFolderSizeTime + getOnlinePlayersCountTime + getServerUpTime + consoleOutputTime;
                int delayTime = Math.Max(0, 500 - (int)totalElapsedTime);
                if (delayTime < 0) delayTime = 0;
                await Task.Delay(delayTime);
            }
            else
            {
                await Task.Delay(1000);
                return;
            }
        }

        // ------------------------ Method to get memory usage of Minecraft server ------------------------
        private static string[] GetUsedHeapMemory(string ip, int port, string user, string psw)
        {
            try
            {
                // Construct the JMX service URL
                string url = $"service:jmx:rmi:///jndi/rmi://{ip}:{port}/jmxrmi";
                JMXServiceURL serviceURL = new(url);

                var environment = new HashMap();
                environment.put(JMXConnector.CREDENTIALS, new string[] { user, psw });

                // Connect to the JMX server
                using JMXConnector connector = JMXConnectorFactory.connect(serviceURL, environment);
                MBeanServerConnection mBeanConnection = connector.getMBeanServerConnection();

                ObjectName memoryMXBean = new("java.lang:type=Memory");

                // Retrieve heap memory usage
                CompositeData heapMemoryUsage = (CompositeData)mBeanConnection.getAttribute(memoryMXBean, "HeapMemoryUsage");

                // Handle the Java Long to .NET long conversion
                long usedHeap = ((java.lang.Long)heapMemoryUsage.get("used")).longValue();
                long maxHeap = ((java.lang.Long)heapMemoryUsage.get("max")).longValue();

                long freeHeap = maxHeap - usedHeap;

                string freeHeapPercentage = ((double)freeHeap / maxHeap * 100).ToString("0.00");

                string usedHeapPercentage = ((double)usedHeap / maxHeap * 100).ToString("0.00");

                return [$"{usedHeapPercentage}%", $"{freeHeapPercentage}%"];
            }
            catch (Exception ex)
            {
                return [$"GetUsedHeapMemory Error: {ex.Message}", "Error"];
            }
        }

        // ------------------------ Method to get folder size of the Minecraft world ------------------------

        public static string GetFolderSize(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return $"Cannot find {folderPath}";
            try
            {
                long sizeInBytes = new DirectoryInfo(folderPath)
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(file => file.Length);

                double sizeInMB = sizeInBytes / (1024.0 * 1024.0);
                double sizeInGB = sizeInMB / 1024.0;

                return sizeInGB >= 1 ? $"{sizeInGB:F2} GB" : $"{sizeInMB:F2} MB";
            }
            catch
            {
                return "Error calculating size";
            }
        }

        // ------------------------ Method to get server uptime via StartupTime file last write time ------------------------

        private static string GetServerUptime(string? worldFolderPath)
        {
            if (worldFolderPath == null)
            {
                return "-1";
            }

            string? parentDirectory = Path.GetDirectoryName(Path.GetDirectoryName(worldFolderPath));
            if (parentDirectory == null)
            {
                return "-1";
            }

            string? startupTimePath = Path.Combine(parentDirectory, "serverStartupTime.txt");

            if (File.Exists(startupTimePath))
            {
                try
                {
                    DateTime startupTime = DateTime.Parse(File.ReadAllText(startupTimePath));
                    return (DateTime.Now - startupTime).ToString(@"hh\:mm\:ss");
                }
                catch (Exception ex)
                {
                    CodeLogger.ConsoleLog("Error reading startup time: " + ex.Message);
                }
            }
            else
            {
                CodeLogger.ConsoleLog("Startup time file not found.");
                return "-1";
            }

            return "-1";
        }

        // ------------------------ Method to get server uptime via heandshake with the server ------------------------

        public static string GetOnlinePlayersCount(string ip, int port, int protocolVersion)
        {
            using TcpClient client = new();
            client.NoDelay = true;
            client.ReceiveTimeout = 5000; // 5-second timeout
            if (protocolVersion < 47)
            {
                // Legacy ping (for versions pre-1.8)
                try
                {
                    client.Connect(ip, port);
                    using (NetworkStream stream = client.GetStream())
                    {
                        stream.WriteByte(0xFE);
                        stream.Flush();

                        // Read first byte: should be 0xFF.
                        int packetId = stream.ReadByte();
                        if (packetId != 0xFF)
                            return "Invalid legacy packet";

                        // Read string length (2 bytes, big-endian) that represents character count.
                        byte[] lengthBytes = new byte[2];
                        if (stream.Read(lengthBytes, 0, 2) != 2)
                            return "Incomplete legacy packet";
                        short stringLength = (short)(lengthBytes[0] << 8 | lengthBytes[1]);
                        int byteLength = stringLength * 2;

                        byte[] stringBytes = new byte[byteLength];
                        int totalRead = 0;
                        while (totalRead < byteLength)
                        {
                            int read = stream.Read(stringBytes, totalRead, byteLength - totalRead);
                            if (read <= 0)
                                break;
                            totalRead += read;
                        }
                        if (totalRead != byteLength)
                            return "Incomplete legacy string";

                        string response = Encoding.BigEndianUnicode.GetString(stringBytes);
                        // Legacy response format (typically): "§1<delimiter>MOTD<delimiter>currentPlayers<delimiter>maxPlayers"
                        string[] parts = response.Split('§');
                        if (parts.Length >= 3)
                        {
                            return parts[1];
                        }
                        return "Unexpected legacy response";
                    }
                }
                catch (Exception ex)
                {
                    return "Error: " + ex.Message;
                }
            }
            else
            {
                // Modern ping (for protocol version 47 and above)
                try
                {
                    client.Connect(ip, port);
                    using (NetworkStream stream = client.GetStream())
                    {
                        // Send handshake packet.
                        byte[] handshakePacket = CreateHandshakePacket(ip, port, protocolVersion);
                        stream.Write(handshakePacket, 0, handshakePacket.Length);
                        stream.Flush();

                        // Send status request packet.
                        using (MemoryStream ms = new MemoryStream())
                        {
                            WriteVarInt(ms, 1);
                            ms.WriteByte(0x00);
                            byte[] requestPacket = ms.ToArray();
                            stream.Write(requestPacket, 0, requestPacket.Length);
                            stream.Flush();
                        }

                        int packetLength = ReadVarInt(stream);
                        if (packetLength <= 0)
                            return "Invalid packet length";

                        byte[] responseData = new byte[packetLength];
                        int bytesRead = 0;
                        while (bytesRead < packetLength)
                        {
                            int read = stream.Read(responseData, bytesRead, packetLength - bytesRead);
                            if (read <= 0)
                                return "Incomplete packet";
                            bytesRead += read;
                        }

                        using (MemoryStream ms = new MemoryStream(responseData))
                        {
                            int packetId = ReadVarInt(ms);
                            if (packetId != 0x00)
                                return "Invalid packet ID";

                            int stringLength = ReadVarInt(ms);
                            if (stringLength <= 0)
                                return "Invalid string length";

                            byte[] jsonData = new byte[stringLength];
                            int jsonBytesRead = ms.Read(jsonData, 0, stringLength);
                            if (jsonBytesRead != stringLength)
                                return "Incomplete JSON data";

                            string jsonString = Encoding.UTF8.GetString(jsonData);
                            // Optionally, log the JSON response for debugging:
                            // Console.WriteLine("DEBUG - JSON Response: " + jsonString);
                            JObject jsonObj = JObject.Parse(jsonString);

                            JToken? onlineToken = jsonObj["players"]?["online"];
                            int online = onlineToken != null ? (int)onlineToken : 0;
                            return online.ToString();
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    return "No response";
                }
                catch (Exception ex)
                {
                    return "Error: " + ex.Message;
                }
            }
        }

        public static string GetConsoleOutput(string rootPath)
        {
            try
            {
                // 1. Check logs/latest.log
                string latestLogPath = Path.Combine(rootPath, "logs", "latest.log");
                if (File.Exists(latestLogPath))
                {
                    return ReadFile(latestLogPath);
                }

                // 2. Check root directory for server.log
                string serverLogPath = Path.Combine(rootPath, "server.log");
                if (File.Exists(serverLogPath))
                {
                    return ReadFile(serverLogPath);
                }

                // 3. Search the entire directory for any .log file
                string[] logFiles = Directory.GetFiles(rootPath, "*.log", SearchOption.AllDirectories);
                if (logFiles.Length > 0)
                {
                    return ReadFile(logFiles[0]); // Return the first found log file
                }
            }
            catch (Exception ex)
            {
                return $"Error reading log file: {ex.Message}";
            }

            return "Log file not found.";
        }

        private static string ReadFile(string filePath)
        {
            try
            {
                using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new(fs);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                return $"Error accessing file {filePath}: {ex.Message}";
            }
        }

        // ------------------------ Help Funcs for GetOnlinePlayersCount() ------------------------

        private static byte[] CreateHandshakePacket(string serverAddress, int serverPort, int protocolVersion)
        {
            using MemoryStream ms = new();
            // Write Packet ID (0x00) as VarInt.
            WriteVarInt(ms, 0x00);
            // Write protocol version as VarInt.
            WriteVarInt(ms, protocolVersion);
            // Write server address (as a string with its length).
            WriteString(ms, serverAddress);
            // Write server port (unsigned short, big endian).
            ms.WriteByte((byte)(serverPort >> 8));
            ms.WriteByte((byte)(serverPort & 0xFF));
            // Write next state (1 for status).
            WriteVarInt(ms, 1);
            byte[] packetData = ms.ToArray();

            using MemoryStream finalMs = new();
            WriteVarInt(finalMs, packetData.Length);
            finalMs.Write(packetData, 0, packetData.Length);
            return finalMs.ToArray();
        }

        private static void WriteVarInt(Stream stream, int value)
        {
            while ((value & -128) != 0)
            {
                stream.WriteByte((byte)(value & 127 | 128));
                value = (int)((uint)value >> 7);
            }
            stream.WriteByte((byte)value);
        }

        private static void WriteString(Stream stream, string value)
        {
            byte[] stringBytes = Encoding.UTF8.GetBytes(value);
            WriteVarInt(stream, stringBytes.Length);
            stream.Write(stringBytes, 0, stringBytes.Length);
        }

        private static int ReadVarInt(Stream stream)
        {
            int numRead = 0;
            int result = 0;
            byte read;
            do
            {
                int readByte = stream.ReadByte();
                if (readByte == -1)
                    throw new EndOfStreamException();
                read = (byte)readByte;
                int value = read & 0b01111111;
                result |= value << 7 * numRead;
                numRead++;
                if (numRead > 5)
                    throw new Exception("VarInt is too big");
            } while ((read & 0b10000000) != 0);
            return result;
        }

        private static int GetProtocolVersion(string version)
        {
            // Mapping based on commonly known Minecraft protocol versions.
            var mapping = new Dictionary<string, int>
                {
                    {"1.2.5", 5},
                    {"1.3.1", 5}, {"1.3.2", 5},
                    {"1.4", 5}, {"1.4.2", 5}, {"1.4.4", 5},
                    {"1.5", 5}, {"1.5.1", 5}, {"1.5.2", 5},
                    {"1.6", 5}, {"1.6.1", 5}, {"1.6.2", 5}, {"1.6.4", 5},
                    {"1.7", 5}, {"1.7.2", 5}, {"1.7.10", 5},
                    {"1.8", 47}, {"1.8.1", 47}, {"1.8.9", 47},
                    {"1.9", 107}, {"1.9.2", 107}, {"1.9.4", 107},
                    {"1.10", 210},
                    {"1.11", 315}, {"1.11.2", 315},
                    {"1.12", 335}, {"1.12.2", 335},
                    {"1.13", 393}, {"1.13.1", 393}, {"1.13.2", 393},
                    {"1.14", 498},
                    {"1.15", 573},
                    {"1.16", 735}, {"1.16.1", 736}, {"1.16.2", 736}, {"1.16.4", 754},
                    {"1.17", 755}, {"1.17.1", 756},
                    {"1.18", 757}, {"1.18.1", 757}, {"1.18.2", 757},
                    {"1.19", 759}, {"1.19.1", 760}, {"1.19.2", 761},
                    {"1.20", 763}, {"1.20.1", 764},
                };

            if (mapping.TryGetValue(version, out int proto))
            {
                return proto;
            }
            else
            {
                if (double.TryParse(version, out double verNum))
                {
                    return verNum < 1.8 ? 5 : 754;
                }
                // Default fallback
                return 754;
            }
        }
    }
}