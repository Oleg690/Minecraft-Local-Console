using Logger;
using CreateServerFunc;
using FileExplorer;
using MinecraftServerStats;
using NetworkConfig;
using System.Runtime.Versioning;
using Updater;
using databaseChanger;

namespace MainAppFuncs
{
    [SupportedOSPlatform("windows")]
    internal class Program
    {
        static async Task Main()
        {
            // ↓ Server Settings ↓
            string software = "";      // e.g. Vanilla
            string version = "";      // e.g. 1.21.4
            string worldNumber = ""; // e.g. 123456789012
            string worldName = "";  // e.g. Minecfraft Server
            int totalPlayers = 20;
            string Server_LocalComputerIP = NetworkSetup.GetLocalIP();
            string Server_PublicComputerIP = await NetworkSetup.GetPublicIP();
            int Server_Port = 25565;
            int JMX_Port = 25562;
            int RMI_Port = 25563;
            int RCON_Port = 25575;

            bool Keep_World_On_Version_Change = true;

            int memoryAlocator = 5000; // in MB

            object[,] defaultWorldSettings = {
                { "max-players", $"{totalPlayers}" },
                { "gamemode", "survival" },
                { "difficulty", "normal" },
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
            string? rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory))) ?? throw new InvalidOperationException("Root folder path is null");
            string? rootWorldsFolder = Path.Combine(rootFolder, "worlds");
            string? serverVersionsPath = Path.Combine(rootFolder, "versions");
            string? tempFolderPath = Path.Combine(rootFolder, "temp");
            string? defaultServerPropertiesPath = Path.Combine(rootFolder, "Preset Files\\server.properties");
            string? serverDirectoryPath = Path.Combine(rootWorldsFolder, worldNumber);
            // Create a log file
            CodeLogger.CreateLogFile();

            //// ↓ Update Available Versions ↓
            //await VersionsUpdater.Update(serverVersionsPath);
            //await VersionsUpdater.Update(serverVersionsPath, software);
            //await VersionsUpdater.Update(serverVersionsPath, software, version);

            //// ↓ Create World Func ↓
            //worldNumber = await ServerCreator.CreateServerFunc(rootFolder, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath, version, worldName, software, totalPlayers, defaultWorldSettings, memoryAlocator, Server_LocalComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port);

            //// ↓ Start Server Func ↓
            //await ServerOperator.Start(worldNumber, serverDirectoryPath, memoryAlocator, Server_PublicComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port, noGUI: false);
            //await ServerOperator.Stop("stop", worldNumber, Server_LocalComputerIP, RCON_Port, JMX_Port, "00:00");
            //await ServerOperator.Restart(serverDirectoryPath, worldNumber, memoryAlocator, Server_LocalComputerIP, Server_PublicComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port, "00:00");
            //ServerOperator.Kill(RCON_Port, JMX_Port);

            //// ↓ Send Server Command Func ↓
            //await ServerOperator.InputForServer("say Oleg690's Minecraft Console Project", worldNumber, RCON_Port, Server_PublicComputerIP);

            //// ↓ Change Version Func ↓
            //await ServerOperator.ChangeVersion(worldNumber, serverDirectoryPath, tempFolderPath, defaultServerPropertiesPath, serverVersionsPath, rootFolder, version, worldName, software, totalPlayers, defaultWorldSettings, memoryAlocator, Server_LocalComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port, Keep_World_On_Version_Change);

            //// ↓ Server Files Loop ↓
            //List<string> files = ServerFileExplorer.FileExplorer(serverDirectoryPath, worldNumber);

            //// ↓ Delete Server ↓
            //ServerOperator.DeleteServer(worldNumber, serverDirectoryPath);

            //// ↓ Server Stats Loop ↓
            //ServerStats.GetServerInfo(serverDirectoryPath, worldNumber, Server_PublicComputerIP, JMX_Port, RCON_Port, Server_Port);
        }
    }
}