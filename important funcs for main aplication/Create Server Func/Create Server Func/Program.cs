using System;
using System.Threading;
using System.Diagnostics;
using Server_General_Funcs;
using MinecraftServerStats;
using fileExplorer;
using Server_Network_Enabler;

namespace mainApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Public Address Ranges
            // 1.0.0.0 – 9.255.255.255

            // Public Address Ranges
            // 10.0.0.0 to 10.255.255.255

            string currentDirectory = Directory.GetCurrentDirectory();
            // e.g. string rootWorldsFolder = @"D:\Minecraft-Server\important funcs for main aplication\Create Server Func\Create Server Func\worlds";
            string rootWorldsFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory))) + "\\worlds";
            string? rootFolder = Path.GetDirectoryName(rootWorldsFolder);
            string version = "1.21";  // e.g. 1.21
            string worldNumber = "145791512891";
            string worldName = "Moldova SMP";
            string Software = "Vanilla"; // e.g. Vanilla or Forge
            int totalPlayers = 20;
            string ExternalIPAdress = "127.0.0.1";
            //string ipAdress = "192.168.100.106"; // "0.0.0.0"
            string ipAddress = "109.185.75.45"; // "0.0.0.0"
            int Server_Port = 25565;
            int JMX_Port = 25562;
            int RCON_Port = 25575;

            bool Keep_World_On_Version_Change = false;

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

            string serverDirectoryPath = System.IO.Path.Combine(rootWorldsFolder, worldNumber);
            string serverPath = System.IO.Path.Combine(serverDirectoryPath, version + ".jar");
            string serverLogPath = System.IO.Path.Combine(serverDirectoryPath, "logs\\latest.log");
            string serverPropriertiesPath = System.IO.Path.Combine(serverDirectoryPath, "server.properties");
            string serverVersionsPath = System.IO.Path.Combine(rootFolder, "versions");
            string tempFolderPath = System.IO.Path.Combine(rootFolder, "temp");

            // ↓ Create World Func ↓
            //worldNumber = serverCreator.CreateServerFunc(rootFolder, rootWorldsFolder, 12, version, worldName, Software, totalPlayers, defaultWorldSettings, memoryAlocator, ipAddress, JMX_Port, RCON_Port);

            // ↓ Start Server Func ↓
            //serverOperator.Start(worldNumber, serverPath, memoryAlocator, ipAddress, JMX_Port, RCON_Port);
            //_ = serverOperator.Stop("stop", worldNumber, "192.168.100.106", RCON_Port, JMX_Port);
            //_ = serverOperator.Restart(serverPath, worldNumber, memoryAlocator, ipAddress, RCON_Port, JMX_Port);
            //serverOperator.Kill(RCON_Port, JMX_Port);

            // ↓ Send Server Command Func ↓
            //_ = serverOperator.InputForServer("give Oleg6900 diamond 64", worldNumber, RCON_Port, ipAddress);
            //_ = serverOperator.InputForServer("op Oleg6900", worldNumber, RCON_Port, ipAddress);

            // ↓ Change Version Func ↓
            //serverOperator.ChangeVersion(worldNumber, serverDirectoryPath, tempFolderPath, serverVersionsPath, rootFolder, 12, version, worldName, Software, totalPlayers, defaultWorldSettings, memoryAlocator, ipAddress, JMX_Port, RCON_Port, Keep_World_On_Version_Change);

            // ↓ Server Files Loop ↓
            //List<string> items = ServerFileExplorer.FileExplorer(serverDirectoryPath, worldNumber);

            // ↓ Delete Server ↓
            //serverOperator.DeleteServer(worldNumber, serverDirectoryPath);

            // ↓ Server Stats Loop ↓
            //while (true)
            //{
            //    ServerStats.GetServerInfo(serverDirectoryPath, serverLogPath, worldNumber, ipAddress, JMX_Port, RCON_Port);
            //    Thread.Sleep(1000);
            //}

            PortForward.EnablePublicServer(ipAddress, Server_Port); // TODO

            Console.ReadKey();
        }
    }
}