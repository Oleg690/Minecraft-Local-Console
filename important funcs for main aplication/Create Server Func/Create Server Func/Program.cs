using System;
using System.Threading;
using Server_General_Funcs;
using serverPropriertiesChanger;
using databaseChanger;
using MinecraftServerStats;
using fileExplorer;
using System.Diagnostics;

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
            string worldNumber = "823659724973";
            string worldName = "Moldova SMP";
            string Software = "Vanilla"; // e.g. Vanilla or Forge
            int totalPlayers = 15;
            string ExternalIPAdress = "127.0.0.1";
            string ipAdress = "192.168.100.106"; // "0.0.0.0"
            int JMX_Port = 25562;
            int RCON_Port = 25575;

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

            // ↓ Create World Func ↓
            //worldNumber = serverCreator.CreateServerFunc(rootFolder, rootWorldsFolder, 12, version, worldName, Software, totalPlayers, defaultWorldSettings, memoryAlocator, ipAdress, JMX_Port, RCON_Port);

            // ↓ Start Server Func ↓
            //serverOperator.Start(worldNumber, serverPath, memoryAlocator, ipAdress, JMX_Port, RCON_Port);
            //serverOperator.Stop("stop", worldNumber, ipAdress, RCON_Port, JMX_Port);
            //serverOperator.Restart(serverPath, worldNumber, memoryAlocator, ipAdress, RCON_Port, JMX_Port);

            // ↓ Send Server Command Func ↓
            //_ = serverOperator.InputForServer("give OlegHD6900 diamond 64", worldNumber, RCON_Port, ipAdress);
            //_ = serverOperator.InputForServer("op OlegHD6900", worldNumber, RCON_Port, ipAdress);

            // ↓ Server Stats Loop ↓
            //while (true)
            //{
            //    ServerStats.GetServerInfo(serverDirectoryPath, serverLogPath, worldNumber, ipAdress, JMX_Port, RCON_Port);
            //    Thread.Sleep(1000);
            //}

            // ↓ Server Files Loop ↓
            //List<string> items = ServerFileExplorer.FileExplorer(serverDirectoryPath, worldNumber);

            // ↓ Delete Server ↓
            //serverOperator.DeleteServer(worldNumber, serverDirectoryPath);

            Console.ReadKey();
        }
    }
}