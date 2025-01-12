using System;
using System.Diagnostics.Tracing;
using System.Reflection.Metadata;
using Server_General_Funcs;
using serverPropriertiesChanger;
using databaseChanger;
using MinecraftServerStats;
using fileExplorer;
using System.Threading;
using System.Diagnostics;

namespace mainApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            object[,] defaultSettings = {
                { 32, "20" },
                { 20, "Survival" },
                { 9, "Normal" },
                { 63, "false" },
                { 37, "false" },
                { 41, "true" },
                { 10, "true" },
                { 4, "true" },
                { 55, "true" },
                { 56, "true" },
                { 57, "true" },
                { 5, "true" },
                { 18, "false" },
                { 58, "0" }
            };

            string currentDirectory = Directory.GetCurrentDirectory();
            // string rootWorldsFolder = @"D:\Minecraft-Server\important funcs for main aplication\Create Server Func\Create Server Func\worlds";
            string rootWorldsFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory)))+ "\\worlds";
            string version = "1.21";
            string worldNumber = "241175006405";
            string worldName = "Moldova SMP";
            int totalPlayers = 20;
            string ipAdress = "192.168.100.106";
            int JMX_Port = 25562;
            int RCON_Port = 25575;

            int memoryAlocator = 5000; // in MB

            // Create World Func
            // worldNumber = serverCreator.CreateServerFunc(rootWorldsFolder, 12, version, worldName, totalPlayers, memoryAlocator);
            // Console.WriteLine($"worldNumber={worldNumber}");

            string serverDirectoryPath = System.IO.Path.Combine(rootWorldsFolder, worldNumber);
            string serverPath = System.IO.Path.Combine(serverDirectoryPath, version + ".jar");
            string serverLogPath = System.IO.Path.Combine(serverDirectoryPath, "logs\\latest.log");
            string serverPropriertiesPath = System.IO.Path.Combine(serverDirectoryPath, "server.proprierties");

            // Start Server Func
            // serverOperator.Start(serverPath, memoryAlocator, ipAdress, JMX_Port, RCON_Port);
            // serverOperator.Stop(worldNumber, ipAdress, RCON_Port);
            // serverOperator.Restart(serverPath, worldNumber, memoryAlocator, ipAdress, RCON_Port, JMX_Port);

            // Send Server Command Func
            // _ = serverOperator.InputForServer("give OlegHD6900 diamond 64", worldNumber, RCON_Port, ipAdress);

            // while (true)
            // { 
            //     ServerStats.GetServerInfo(serverDirectoryPath, serverLogPath, worldNumber, ipAdress, JMX_Port, RCON_Port);
            //     Thread.Sleep(1000);
            // }

            // List<string> items = ServerFileExplorer.FileExplorer(serverDirectoryPath, worldNumber);

            Console.ReadKey();
        }
    }
}