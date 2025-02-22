﻿using System;
using System.Threading;
using System.Diagnostics;
using Server_General_Funcs;
using MinecraftServerStats;
using fileExplorer;
using NetworkConfig;
using updater;

namespace mainApp
{
    internal class Program
    {
        static async Task Main()
        //static void Main()
        {
            // Public Address Ranges
            // 1.0.0.0 – 9.255.255.255

            // Public Address Ranges
            // 10.0.0.0 to 10.255.255.255

            string currentDirectory = Directory.GetCurrentDirectory();
            string rootWorldsFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory))) + "\\worlds";
            string? rootFolder = Path.GetDirectoryName(rootWorldsFolder);
            string version = "";  // e.g. 1.21
            string worldNumber = "";
            string worldName = "Minecraft Server";
            string Software = ""; // e.g. Vanilla, Forge, NeoForge, Fabric, Quilt, Purpur
            int totalPlayers = 20;
            string Server_LocalIp = "127.0.0.1";
            string Server_LocalComputerIP = "192.168.100.106"; // "0.0.0.0"
            string Server_PublicComputerIP = "109.185.75.45"; // "0.0.0.0"
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

            string serverDirectoryPath = Path.Combine(rootWorldsFolder, worldNumber);
            string serverPath = Path.Combine(serverDirectoryPath);
            string serverJarPath = Path.Combine(serverDirectoryPath, version + ".jar");
            string serverLogPath = Path.Combine(serverDirectoryPath, "logs\\latest.log");
            string serverPropriertiesPath = Path.Combine(serverDirectoryPath, "server.properties");
            string serverVersionsPath = Path.Combine(rootFolder, "versions");
            string tempFolderPath = Path.Combine(rootFolder, "temp");

            // ↓ Update Available Versions ↓ TODO
            //await versionsUpdater.UpdateAvailableVersions(serverVersionsPath); 

            // ↓ Create World Func ↓
            //worldNumber = await serverCreator.CreateServerFunc(rootFolder, rootWorldsFolder, tempFolderPath, 12, version, worldName, Software, totalPlayers, defaultWorldSettings, memoryAlocator, Server_LocalComputerIP, JMX_Port, RCON_Port);

            //string path = serverOperator.FindClosestJarFile("D:\\Minecraft-Server\\important funcs for main aplication\\Create Server Func\\Create Server Func\\worlds\\596387740478\\", "server"); // To test
            //Console.WriteLine(path);

            // ↓ Start Server Func ↓
            //await serverOperator.Start(worldNumber, serverPath, memoryAlocator, Server_LocalComputerIP, JMX_Port, RCON_Port);
            //await serverOperator.Stop("stop", worldNumber, Server_LocalComputerIP, RCON_Port, JMX_Port, true);
            //await serverOperator.Restart(serverPath, worldNumber, memoryAlocator, Server_LocalComputerIP, RCON_Port, JMX_Port);
            //serverOperator.Kill(RCON_Port, JMX_Port);

            // ↓ Send Server Command Func ↓
            //_ = serverOperator.InputForServer("give Oleg6900 diamond 64", worldNumber, RCON_Port, Server_LocalComputerIP);
            //_ = serverOperator.InputForServer("op Oleg6900", worldNumber, RCON_Port, Server_LocalComputerIP);

            // ↓ Change Version Func ↓
            //await serverOperator.ChangeVersion(worldNumber, serverDirectoryPath, tempFolderPath, serverVersionsPath, rootFolder, 12, version, worldName, Software, totalPlayers, defaultWorldSettings, memoryAlocator, Server_LocalComputerIP, JMX_Port, RCON_Port, Keep_World_On_Version_Change);

            // ↓ Server Files Loop ↓
            //List<string> items = ServerFileExplorer.FileExplorer(serverDirectoryPath, worldNumber);

            // ↓ Delete Server ↓
            //serverOperator.DeleteServer(worldNumber, serverDirectoryPath);

            // ↓ Server Stats Loop ↓
            //while (true)
            //{
            //    ServerStats.GetServerInfo(serverDirectoryPath, serverLogPath, worldNumber, Server_LocalComputerIP, JMX_Port, RCON_Port);
            //    Thread.Sleep(1000);
            //}

            // ↓ Test Network Config ↓

            //await UPnP_Port_Mapping.UPnP_Configuration_Async(25565);

            //string domainName = DomainName.GetRandomDomainName();
            //Console.WriteLine(domainName);

            Console.ReadKey();
        }
    }
}