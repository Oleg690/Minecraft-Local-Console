using System;
using System.Threading;
using System.Diagnostics;
using Server_General_Funcs;
using MinecraftServerStats;
using fileExplorer;

namespace mainApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            // e.g. string rootWorldsFolder = @"D:\Minecraft-Server\important funcs for main aplication\Create Server Func\Create Server Func\worlds";
            string rootWorldsFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory))) + "\\worlds";
            string? rootFolder = Path.GetDirectoryName(rootWorldsFolder);
            string version = "1.21";  // e.g. 1.21
            string worldNumber = "";
            string worldName = "Moldova SMP";
            string Software = "Forge"; // e.g. Vanilla or Forge
            int totalPlayers = 20;
            string ExternalIPAdress = "127.0.0.1";
            string ipAdress = "192.168.100.106"; // "0.0.0.0"
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
            //worldNumber = serverCreator.CreateServerFunc(rootFolder, rootWorldsFolder, 12, version, worldName, Software, totalPlayers, defaultWorldSettings, memoryAlocator, ipAdress, JMX_Port, RCON_Port);

            // ↓ Start Server Func ↓
            //serverOperator.Start(worldNumber, serverPath, memoryAlocator, ipAdress, JMX_Port, RCON_Port);
            //_ = serverOperator.Stop("stop", worldNumber, ipAdress, RCON_Port, JMX_Port);
            //_ = serverOperator.Restart(serverPath, worldNumber, memoryAlocator, ipAdress, RCON_Port, JMX_Port);
            //serverOperator.Kill(RCON_Port, JMX_Port);

            // ↓ Send Server Command Func ↓
            //_ = serverOperator.InputForServer("give Oleg6900 diamond 64", worldNumber, RCON_Port, ipAdress);
            //_ = serverOperator.InputForServer("op Oleg6900", worldNumber, RCON_Port, ipAdress);

            // ↓ Change Version Func ↓
            //serverOperator.ChangeVersion(worldNumber, serverDirectoryPath, tempFolderPath, serverVersionsPath, rootFolder, 12, version, worldName, Software, totalPlayers, defaultWorldSettings, memoryAlocator, ipAdress, JMX_Port, RCON_Port, Keep_World_On_Version_Change);


            // ↓ Server Files Loop ↓
            //List<string> items = ServerFileExplorer.FileExplorer(serverDirectoryPath, worldNumber);

            // ↓ Delete Server ↓
            //serverOperator.DeleteServer(worldNumber, serverDirectoryPath);

            // ↓ Server Stats Loop ↓
            //while (true)
            //{
            //    ServerStats.GetServerInfo(serverDirectoryPath, serverLogPath, worldNumber, ipAdress, JMX_Port, RCON_Port);
            //    Thread.Sleep(1000);
            //}

            Console.ReadKey();
        }
    }
}