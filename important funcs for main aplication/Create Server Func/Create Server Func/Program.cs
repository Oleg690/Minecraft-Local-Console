using System;
using System.Net.Sockets;
using System.Net;
using MinecraftServerStats;
using FileExplorer;
using NetworkConfig;
using Updater;
using Create_Server_Func;

namespace MainApp
{
    internal class Program
    {
        static async Task Main()
        {
            // ↓ Server Settings ↓
            string version = "";  // e.g. 1.21
            string worldNumber = ""; // e.g./ 123456789012
            string worldName = ""; // e.g. Minecfraft Server
            string software = ""; // e.g. Vanilla
            int totalPlayers = 20;
            string Server_LocalComputerIP = GetLocalMachineIP();
            string Server_PublicComputerIP = await GetPublicIP();
            int Server_Port = 25565;
            int JMX_Port = 25562;
            int RCON_Port = 25575;

            bool Keep_World_On_Version_Change = true;

            int memoryAlocator = 5000; // in MB

            object[,] defaultWorldSettings = {
                { "max-players", $"{totalPlayers}" },
                { "gamemode", "Survival" },
                { "difficulty", "Normal" },
                { "white-list", "false" },
                { "online-mode", "false" }, // For crack launchers of minecraft, so they can play
                { "pvp", "true" },
                { "enable-command-block", "true" },
                { "allow-flight", "true" },
                { "spawn-animals", "true" },
                { "spawn-monsters", "true" },
                { "spawn-npcs", "true" },
                { "allow-nether", "true" },
                { "force-gamemode", "false" },
                { "spawn-protection", "0" }
            };

            // ↓ All needed directories ↓
            string? currentDirectory = Directory.GetCurrentDirectory();
            string? rootWorldsFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory))) + "\\worlds";
            string? rootFolder = Path.GetDirectoryName(rootWorldsFolder);
            string? serverDirectoryPath = Path.Combine(rootWorldsFolder, worldNumber);
            string? serverPath = Path.Combine(serverDirectoryPath);
            string? serverJarPath = Path.Combine(serverDirectoryPath, version + ".jar");
            string? serverLogPath = Path.Combine(serverDirectoryPath, "logs\\latest.log");
            string? serverPropriertiesPath = Path.Combine(serverDirectoryPath, "server.properties");
            string? serverVersionsPath = Path.Combine(rootFolder, "versions");
            string? versionsSupprortListXML = Path.Combine(rootFolder, "SupportedVersions.xml");
            string? tempFolderPath = Path.Combine(rootFolder, "temp");

            // ↓ Update Available Versions ↓
            await VersionsUpdater.Update(serverVersionsPath);
            await VersionsUpdater.Update(serverVersionsPath, software);
            await VersionsUpdater.Update(serverVersionsPath, software, version);

            // ↓ Create World Func ↓
            worldNumber = await ServerCreator.CreateServerFunc(rootFolder, rootWorldsFolder, tempFolderPath, 12, version, worldName, software, totalPlayers, defaultWorldSettings, memoryAlocator, Server_LocalComputerIP, JMX_Port, RCON_Port);

            // ↓ Start Server Func ↓
            await ServerOperator.Start(worldNumber, serverPath, memoryAlocator, Server_PublicComputerIP, JMX_Port, RCON_Port);
            await ServerOperator.Stop("stop", worldNumber, Server_LocalComputerIP, RCON_Port, JMX_Port, "00:00");
            await ServerOperator.Restart(serverPath, worldNumber, memoryAlocator, Server_LocalComputerIP, Server_PublicComputerIP, RCON_Port, JMX_Port, "10:00");
            ServerOperator.Kill(RCON_Port, JMX_Port);

            // ↓ Send Server Command Func ↓
            _ = ServerOperator.InputForServer("op Oleg6900", worldNumber, RCON_Port, Server_LocalComputerIP);

            // ↓ Change Version Func ↓
            await ServerOperator.ChangeVersion(worldNumber, serverDirectoryPath, tempFolderPath, serverVersionsPath, rootFolder, 12, version, worldName, software, totalPlayers, defaultWorldSettings, memoryAlocator, Server_LocalComputerIP, JMX_Port, RCON_Port, Keep_World_On_Version_Change);

            // ↓ Server Files Loop ↓
            List<string> files = ServerFileExplorer.FileExplorer(serverDirectoryPath, worldNumber);

            // ↓ Delete Server ↓
            ServerOperator.DeleteServer(worldNumber, serverDirectoryPath);

            // ↓ Network Configuration ↓ Testing
            await UPnP_Port_Mapping.UPnP_Configuration_Async(25565);

            // ↓ Server Stats Loop ↓
            while (true)
            {
                ServerStats.GetServerInfo(serverDirectoryPath, worldNumber, Server_PublicComputerIP, JMX_Port, RCON_Port, Server_Port);
            }
        }

        private static string GetLocalMachineIP()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 80);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint?.Address.ToString() ?? "Unable to determine local IP";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
        private static async Task<string> GetPublicIP()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    return await client.GetStringAsync("https://api64.ipify.org");
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
    }
}