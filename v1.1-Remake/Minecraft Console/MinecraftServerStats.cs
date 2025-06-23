using java.util;
using javax.management;
using javax.management.openmbean;
using javax.management.remote;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;

namespace Minecraft_Console
{
    [SupportedOSPlatform("windows")]
    public class ServerStats
    {
        public static void MonitorServer(
            ViewModel viewModel,
            string worldFolderPath,
            string version,
            string ipAddress,
            int JMX_Port,
            int Server_Port,
            string maxPlayers,
            string[] userData,
            CancellationToken token)
            {
            if (viewModel == null)
                return;

            // Start Uptime monitor separat
            _ = Task.Run(() => MonitorServerUpTime(viewModel, worldFolderPath, token), token);

            string? user = userData[0];
            string? psw = userData[1];

            // Memory Monitor
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && MainWindow.serverRunning)
                {
                    try
                    {
                        var memoryStats = GetUsedHeapMemory(ipAddress, JMX_Port, user, psw);
                        if (token.IsCancellationRequested) break;

                        string usedFormatted = memoryStats[2];
                        string maxFormatted = memoryStats[4];
                        string usedPercent = memoryStats[0];

                        viewModel.MemoryUsage = $"{usedFormatted} / {maxFormatted}";

                        await Task.Delay(1000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        CodeLogger.ConsoleLog("[MemoryMonitor] Error: " + ex.Message);
                        break;
                    }
                }
            }, token);

            // Player Count Monitor
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && MainWindow.serverRunning)
                {
                    try
                    {
                        int protocol = GetProtocolVersion(version);
                        string online = GetOnlinePlayersCount(ipAddress, Server_Port, protocol);
                        if (token.IsCancellationRequested) break;

                        viewModel.PlayersOnline = $"{online} / {maxPlayers}";

                        await Task.Delay(1000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        CodeLogger.ConsoleLog("[PlayerMonitor] Error: " + ex.Message);
                        break;
                    }
                }
            }, token);

            // World Size Monitor
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && MainWindow.serverRunning)
                {
                    try
                    {
                        string size = GetFolderSize(worldFolderPath);
                        if (token.IsCancellationRequested) break;

                        viewModel.WorldSize = size;

                        await Task.Delay(5000, token); // Size doesn't need 1s updates
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        CodeLogger.ConsoleLog("[SizeMonitor] Error: " + ex.Message);
                        break;
                    }
                }
            }, token);

            // Console Output Monitor
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && MainWindow.serverRunning)
                {
                    try
                    {
                        string console = GetConsoleOutput(worldFolderPath);
                        if (token.IsCancellationRequested) break;

                        viewModel.Console = console;

                        await Task.Delay(1000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        CodeLogger.ConsoleLog("[ConsoleMonitor] Error: " + ex.Message);
                        break;
                    }
                }
            }, token);

            // Final log
            //CodeLogger.ConsoleLog("[MainMonitor] All monitoring tasks started.");
        }

        public static async Task MonitorServerUpTime(
            ViewModel viewModel,
            string worldFolderPath,
            CancellationToken token)
        {
            if (viewModel == null)
            {
                CodeLogger.ConsoleLog("[UptimeMonitor] ViewModel is null — monitoring aborted.");
                return;
            }

            DateTime startupTime;
            string? parentDirectory = Path.GetDirectoryName(Path.GetDirectoryName(worldFolderPath));
            if (parentDirectory == null)
            {
                CodeLogger.ConsoleLog($"[UptimeMonitor] Failed to locate parent directory for: {worldFolderPath}");
                return;
            }

            string startupTimePath = Path.Combine(parentDirectory, "serverStartupTime.txt");
            if (!File.Exists(startupTimePath))
            {
                CodeLogger.ConsoleLog($"[UptimeMonitor] File not found: {startupTimePath}");
                return;
            }

            try
            {
                startupTime = DateTime.Parse(File.ReadAllText(startupTimePath));
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"[UptimeMonitor] Error parsing startup time from file: {ex}");
                return;
            }

            while (!token.IsCancellationRequested && MainWindow.serverRunning)
            {
                if (!viewModel.IsActivePanel)
                {
                    CodeLogger.ConsoleLog("[UptimeMonitor] ViewModel no longer active — exiting monitor.");
                    return;
                }

                TimeSpan uptime = DateTime.Now - startupTime;
                viewModel.UpTime = uptime.ToString(@"hh\:mm\:ss");

                DateTime nextTick = DateTime.Now.AddSeconds(1);
                int delayMs = (int)(nextTick - DateTime.Now).TotalMilliseconds;

                try
                {
                    await Task.Delay(delayMs, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        // ------------------------ Method to get memory usage of Minecraft server ------------------------
        public static string[] GetUsedHeapMemory(string ip, int port, string user, string psw)
        {
            try
            {
                string url = $"service:jmx:rmi:///jndi/rmi://{ip}:{port}/jmxrmi";
                JMXServiceURL serviceURL = new(url);

                var environment = new HashMap();

                environment.put("jmx.remote.x.request.waiting.timeout", 3000L);
                environment.put("jmx.remote.x.connection.timeout", 3000L);

                environment.put(JMXConnector.CREDENTIALS, new string[] { user, psw });

                using JMXConnector connector = JMXConnectorFactory.connect(serviceURL, environment);
                MBeanServerConnection mBeanConnection = connector.getMBeanServerConnection();

                ObjectName memoryMXBean = new("java.lang:type=Memory");

                CompositeData heapMemoryUsage = (CompositeData)mBeanConnection.getAttribute(memoryMXBean, "HeapMemoryUsage");

                long usedHeap = ((java.lang.Long)heapMemoryUsage.get("used")).longValue();
                long maxHeap = ((java.lang.Long)heapMemoryUsage.get("max")).longValue();
                long freeHeap = maxHeap - usedHeap;

                string usedHeapPercentage = ((double)usedHeap / maxHeap * 100).ToString("0.00");
                string freeHeapPercentage = ((double)freeHeap / maxHeap * 100).ToString("0.00");

                string usedFormatted = FormatMemory(usedHeap);
                string freeFormatted = FormatMemory(freeHeap);
                string maxFormatted = FormatMemory(maxHeap);

                return
                [
                    $"{usedHeapPercentage}%",
                    $"{freeHeapPercentage}%",
                    usedFormatted,
                    freeFormatted,
                    maxFormatted,
                    usedHeap.ToString()
                ];
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

                string[] sizes = ["Bytes", "KB", "MB", "GB", "TB"];
                double len = sizeInBytes;
                int order = 0;

                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }

                return $"{len:F2} {sizes[order]}";
            }
            catch
            {
                return "Error calculating size";
            }
        }

        // ------------------------ Method to get server uptime via StartupTime file last write time ------------------------

        public static string GetServerUptime(string? worldFolderPath)
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
            client.ReceiveTimeout = 3000; // 3-second timeout
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
                        using (MemoryStream ms = new())
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

                        using (MemoryStream ms = new(responseData))
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
                    // CodeLogger.ConsoleLog("[Log] Found latest.log");
                    return ReadFile(latestLogPath);
                }
                else
                {
                    CodeLogger.ConsoleLog("[Log] latest.log not found at: " + latestLogPath);
                }

                // 2. Check root directory for server.log
                string serverLogPath = Path.Combine(rootPath, "server.log");
                if (File.Exists(serverLogPath))
                {
                    // CodeLogger.ConsoleLog("[Log] Found server.log");
                    return ReadFile(serverLogPath);
                }
                else
                {
                    CodeLogger.ConsoleLog("[Log] server.log not found at: " + serverLogPath);
                }

                // 3. Search the entire directory for any .log file
                string[] logFiles = Directory.GetFiles(rootPath, "*.log", SearchOption.AllDirectories);
                if (logFiles.Length > 0)
                {
                    CodeLogger.ConsoleLog($"[Log] Found fallback log file: {logFiles[0]}");
                    return ReadFile(logFiles[0]);
                }
                else
                {
                    CodeLogger.ConsoleLog("[Log] No .log files found in directory: " + rootPath);
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog("[Log] Exception while reading log file: " + ex);
                return $"Error reading log file: {ex.Message}";
            }

            CodeLogger.ConsoleLog("[Log] No log file found at all.");
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
        public static string FormatMemory(long bytes)
        {
            string[] sizes = ["Bytes", "KB", "MB", "GB", "TB", "PB"];
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:F2} {sizes[order]}";
        }


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

        public static int GetProtocolVersion(string version)
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