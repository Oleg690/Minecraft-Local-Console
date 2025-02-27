using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading.Channels;
using System.IO;
using System.Reflection.Emit;
using serverPropriertiesChanger;
using databaseChanger;
using CoreRCON;
using System.Net;
using java.nio.file;
using javax.swing.plaf;
using com.sun.tools.javadoc;
using jdk.nashorn.@internal.ir;
using System.Management;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Linq;
using com.sun.tools.@internal.xjc.reader.gbind;
using com.sun.xml.@internal.ws.message;
using sun.tools.jar.resources;
using javax.sound.midi;
using javax.xml.crypto;
using sun.security.util;
using NetworkConfig;
using sun.awt.windows;
using com.sun.media.sound;
using System.Reflection.PortableExecutable;

namespace Server_General_Funcs
{
    class serverCreator
    {
        public static async Task<string> CreateServerFunc(string rootFolder, string rootWorldsFolder, string tempFolderPath, int numberOfDigitsForWorldNumber, string version, string worldName, string software, int totalPlayers, object[,] worldSettings, int ProcessMemoryAlocation, string ipAddress, int JMX_Port, int RCON_Port, string? worldNumber = null, bool Server_Auto_Start = true, bool Insert_Into_DB = true, bool Auto_Stop_After_Start = false)
        {
            string uniqueNumber = GenerateUniqueRandomNumber(numberOfDigitsForWorldNumber, rootWorldsFolder);

            if (worldNumber != null)
            {
                uniqueNumber = worldNumber;
            }

            string customDirectory = System.IO.Path.Combine(rootWorldsFolder, uniqueNumber);

            Directory.CreateDirectory(customDirectory);
            Console.WriteLine($"Created server directory: {customDirectory}");

            string jarFoldersPath = rootFolder;

            string versionName = version + ".jar";
            string[] versionData = [];
            string jarFilePath = "";

            if (software == "")
            {
                Console.WriteLine("Software not selected!");
                return "Software not selected!";
            }
            else if (software == "Vanilla")
            {
                jarFoldersPath = rootFolder + "\\versions\\Vanilla\\";
                versionData = FindJarFile(jarFoldersPath, version);
                versionName = versionData[1];
                jarFilePath = System.IO.Path.Combine(jarFoldersPath, versionName);
            }
            else if (software == "Forge")
            {
                jarFoldersPath += @"\versions\Forge";
                jarFilePath = System.IO.Path.Combine(jarFoldersPath, versionName);
            }
            else if (software == "Fabric")
            {
                jarFoldersPath += @"\versions\Fabric";
                jarFilePath = FindClosestJarFile(jarFoldersPath, "installer");
            }
            else if (software == "NeoForge")
            {
                jarFoldersPath += @"\versions\NeoForge";
                jarFilePath = System.IO.Path.Combine(jarFoldersPath, versionName);
            }
            else if (software == "Purpur")
            {
                jarFoldersPath = rootFolder + "\\versions\\Purpur\\";
                versionData = FindJarFile(jarFoldersPath, version);
                versionName = versionData[1];
                jarFilePath = System.IO.Path.Combine(jarFoldersPath, versionName);
            }
            else if (software == "Spigot")
            {
                jarFoldersPath += @"\versions\Spigot";
                jarFilePath = System.IO.Path.Combine(jarFoldersPath, versionName); // TODO
            }
            else if (software == "Quilt")
            {
                jarFoldersPath += @"\versions\Quilt";
                jarFilePath = FindClosestJarFile(jarFoldersPath, "installer");
            }

            Console.WriteLine(jarFilePath);

            if (!File.Exists(jarFilePath))
            {
                Console.WriteLine("Server .jar file not found! Check the path.");
                return "";
            }

            string destinationJarPath = System.IO.Path.Combine(customDirectory, versionName);

            File.Copy(jarFilePath, destinationJarPath);
            RenameFile(destinationJarPath, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(destinationJarPath), version + ".jar"));
            destinationJarPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(destinationJarPath), version + ".jar");

            Console.WriteLine("destinationJarPath: " + destinationJarPath);

            Console.WriteLine("Server .jar file copied to the custom directory.");

            string rconPassword = generatePassword(20);

            if (worldNumber != null)
            {
                List<object[]> data = dbChanger.GetFunc(worldNumber, true);

                foreach (var row in data)
                {
                    rconPassword = (string)row[6];
                }
            }

            object[,] rconSettings = {
                { "enable-rcon", "true" },
                { "rcon.password", $"{rconPassword}" },
                { "rcon.port", $"{RCON_Port}" },
                { "enable-query", "true" },
                { "online-mode", "false" }
            };

            Console.WriteLine($"{software} Server");

            //return "";

            if (software == "Vanilla")
            {
                await VanillaServerInitialisation(customDirectory, destinationJarPath, rconSettings, worldSettings, ProcessMemoryAlocation, uniqueNumber, worldName, version, totalPlayers, rconPassword, ipAddress, JMX_Port, RCON_Port, Server_Auto_Start, Insert_Into_DB);
            }
            else if (software == "Forge")
            {
                await ForgeServerInitialisation(customDirectory, destinationJarPath, rconSettings, worldSettings, ProcessMemoryAlocation, uniqueNumber, worldName, version, totalPlayers, rconPassword, ipAddress, JMX_Port, RCON_Port, Server_Auto_Start, Insert_Into_DB);
            }
            else if (software == "Fabric")
            {
                await FabricServerInitialisation(customDirectory, destinationJarPath, rconSettings, worldSettings, ProcessMemoryAlocation, uniqueNumber, worldName, version, totalPlayers, rconPassword, ipAddress, JMX_Port, RCON_Port, Server_Auto_Start, Insert_Into_DB);
            }
            else if (software == "NeoForge")
            {
                await NeoForgeServerInitialisation(customDirectory, destinationJarPath, rconSettings, worldSettings, ProcessMemoryAlocation, uniqueNumber, worldName, version, totalPlayers, rconPassword, ipAddress, JMX_Port, RCON_Port, Server_Auto_Start, Insert_Into_DB);
            }
            else if (software == "Purpur")
            {
                await PurpurServerInitialisation(customDirectory, destinationJarPath, rconSettings, worldSettings, ProcessMemoryAlocation, uniqueNumber, worldName, version, totalPlayers, rconPassword, ipAddress, JMX_Port, RCON_Port, Server_Auto_Start, Insert_Into_DB);
            }
            else if (software == "Spigot")
            {
                // TO DO
                await SpigotServerInitialisation(customDirectory, destinationJarPath, rconSettings, worldSettings, ProcessMemoryAlocation, uniqueNumber, worldName, version, totalPlayers, rconPassword, ipAddress, JMX_Port, RCON_Port, Server_Auto_Start, Insert_Into_DB);
            }
            else if (software == "Quilt")
            {
                await QuiltServerInitialisation(customDirectory, destinationJarPath, tempFolderPath, rconSettings, worldSettings, ProcessMemoryAlocation, uniqueNumber, worldName, version, totalPlayers, rconPassword, ipAddress, JMX_Port, RCON_Port, Server_Auto_Start, Insert_Into_DB);
            }

            return uniqueNumber;
        }

        // ------------------------ Main Server Type Installators ------------------------
        private static async Task VanillaServerInitialisation(string customDirectory, string vanillaJarPath, object[,] rconSettings, object[,] worldSettings, int ProcessMemoryAlocation, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, string ipAddress, int JMX_Port, int RCON_Port, bool Server_Auto_Start, bool Insert_Into_DB)
        {
            Console.WriteLine($"vanillaJarPath: '{vanillaJarPath}'");
            // Set up the process to run the server
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-Xmx{ProcessMemoryAlocation}M -Xms{ProcessMemoryAlocation}M " + // nogui
                                    $"-Dcom.sun.management.jmxremote " +
                                    $"-Dcom.sun.management.jmxremote.port={JMX_Port} " +
                                    $"-Dcom.sun.management.jmxremote.authenticate=false " +
                                    $"-Dcom.sun.management.jmxremote.ssl=false " +
                                    $"-Djava.rmi.server.hostname={ipAddress} " + // Replace with actual server IP if necessary
                                    $"-jar \"{vanillaJarPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(vanillaJarPath) // Set the custom working directory
            };

            try
            {
                using (Process process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        Console.WriteLine("Failed to start the Minecraft server.");
                    }

                    Console.WriteLine("Minecraft server is starting...");

                    string serverPropertiesPath = System.IO.Path.Combine(customDirectory, "server.properties");
                    if (!File.Exists(serverPropertiesPath))
                    {
                        File.Create(serverPropertiesPath).Close();
                        Console.WriteLine("Created missing server.properties file.");
                    }

                    // Read server output
                    while (process.StandardOutput.EndOfStream == false)
                    {
                        string? line = process.StandardOutput.ReadLine();
                        Console.WriteLine(line);

                        // Accept EULA if prompted
                        if (line != null && line.Contains("You need to agree to the EULA in order to run the server"))
                        {
                            if (ServerOperator.IsPortInUse(JMX_Port) || ServerOperator.IsPortInUse(RCON_Port))
                            {
                                ServerOperator.ClosePort(JMX_Port.ToString());
                                ServerOperator.ClosePort(RCON_Port.ToString());
                            }

                            AcceptEULA(vanillaJarPath);
                            DataChanger.SetInfo(rconSettings, serverPropertiesPath, true);
                            
                            Console.WriteLine("Insert_Into_DB: " + Insert_Into_DB);

                            if (Insert_Into_DB)
                            {
                                dbChanger.SetFunc($"{uniqueNumber}", $"{worldName}", "Vanilla", $"{version}", $"{totalPlayers}", $"{rconPassword}");
                            }
                        }
                    }

                    process.WaitForExit();

                    DataChanger.SetInfo(worldSettings, serverPropertiesPath, true);

                    Console.WriteLine("Starting minecraft server.");

                    await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task ForgeServerInitialisation(string customDirectory, string forgeJarPath, object[,] rconSettings, object[,] worldSettings, int ProcessMemoryAlocation, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, string ipAddress, int JMX_Port, int RCON_Port, bool Server_Auto_Start, bool Insert_Into_DB)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{forgeJarPath}\" --installServer",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = customDirectory // Directory where the server files will be installed
            };
            try
            {
                using (Process process = new Process { StartInfo = processInfo })
                {
                    int currentProgress = 0;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            // Determine progress based on output logs and update state
                            if (e.Data.Contains("Extracting main jar") && currentProgress < 10)
                            {
                                currentProgress = 10;
                                Console.WriteLine($"Progress: {currentProgress}% - Extracting main jar...");
                            }
                            else if (e.Data.Contains("Downloading library from") && currentProgress < 30)
                            {
                                currentProgress = 30;
                                Console.WriteLine($"Progress: {currentProgress}% - Downloading libraries...");
                            }
                            else if (e.Data.Contains("Checksum validated") && currentProgress < 50)
                            {
                                currentProgress = 50;
                                Console.WriteLine($"Progress: {currentProgress}% - Libraries validated...");
                            }
                            else if (e.Data.Contains("EXTRACT_FILES") && currentProgress < 70)
                            {
                                currentProgress = 70;
                                Console.WriteLine($"Progress: {currentProgress}% - Extracting server files...");
                            }
                            else if (e.Data.Contains("BUNDLER_EXTRACT") && currentProgress < 85)
                            {
                                currentProgress = 85;
                                Console.WriteLine($"Progress: {currentProgress}% - Processing bundled files...");
                            }
                            else if (e.Data.Contains("The server installed successfully") && currentProgress < 100)
                            {
                                currentProgress = 100;
                                Console.WriteLine($"Progress: {currentProgress}% - Files installation complete!");
                            }
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine($"Error: {e.Data}");
                        }
                    };

                    // Start the process
                    process.Start();

                    // Begin asynchronous reading of output and error streams
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    Console.WriteLine("Minecraft server installation is starting...");

                    process.WaitForExit();
                }

                if (Insert_Into_DB)
                {
                    dbChanger.SetFunc($"{uniqueNumber}", $"{worldName}", "Forge", $"{version}", $"{totalPlayers}", $"{rconPassword}");
                }
                Console.WriteLine("Installation completed!");

                File.Delete(forgeJarPath);

                object[,] filesToDelete = {
                { "run.bat", "run.sh", "user_jvm_args.txt", "installer.log"}
                };

                foreach (var file in filesToDelete)
                {
                    if (File.Exists(customDirectory + $"\\{file}"))
                    {
                        File.Delete(customDirectory + $"\\{file}");
                    }
                }

                if (!File.Exists(customDirectory + $"\\mods"))
                {
                    Directory.CreateDirectory(customDirectory + $"\\mods");
                }

                await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port);

                string currentDirectory = Directory.GetCurrentDirectory();

                string serverPropertiesPath = System.IO.Path.Combine(customDirectory, "server.properties");
                string serverPropertiesPresetPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(currentDirectory))) + "\\Preset Files\\server.properties";
                if (!File.Exists(serverPropertiesPath))
                {
                    File.Copy(serverPropertiesPresetPath, serverPropertiesPath);
                    Console.WriteLine("Created missing server.properties file.");
                }

                AcceptEULA(forgeJarPath);

                DataChanger.SetInfo(rconSettings, serverPropertiesPath, true);
                DataChanger.SetInfo(worldSettings, serverPropertiesPath, true);

                await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task FabricServerInitialisation(string customDirectory, string fabricJarPath, object[,] rconSettings, object[,] worldSettings, int ProcessMemoryAlocation, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, string ipAddress, int JMX_Port, int RCON_Port, bool Server_Auto_Start, bool Insert_Into_DB)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{fabricJarPath}\" server -mcversion {version} -downloadMinecraft",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = customDirectory // Directory where the server files will be installed
            };
            try
            {
                using (Process process = new Process { StartInfo = processInfo })
                {
                    int currentProgress = 0;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            // Determine progress based on output logs and update state
                            if (e.Data.Contains("Extracting main jar") && currentProgress < 10)
                            {
                                currentProgress = 10;
                                Console.WriteLine($"Progress: {currentProgress}% - Extracting main jar...");
                            }
                            else if (e.Data.Contains("Downloading library from") && currentProgress < 30)
                            {
                                currentProgress = 30;
                                Console.WriteLine($"Progress: {currentProgress}% - Downloading libraries...");
                            }
                            else if (e.Data.Contains("Checksum validated") && currentProgress < 50)
                            {
                                currentProgress = 50;
                                Console.WriteLine($"Progress: {currentProgress}% - Libraries validated...");
                            }
                            else if (e.Data.Contains("EXTRACT_FILES") && currentProgress < 70)
                            {
                                currentProgress = 70;
                                Console.WriteLine($"Progress: {currentProgress}% - Extracting server files...");
                            }
                            else if (e.Data.Contains("BUNDLER_EXTRACT") && currentProgress < 85)
                            {
                                currentProgress = 85;
                                Console.WriteLine($"Progress: {currentProgress}% - Processing bundled files...");
                            }
                            else if (e.Data.Contains("The server installed successfully") && currentProgress < 100)
                            {
                                currentProgress = 100;
                                Console.WriteLine($"Progress: {currentProgress}% - Files installation complete!");
                            }
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine($"Error: {e.Data}");
                        }
                    };

                    // Start the process
                    process.Start();

                    // Begin asynchronous reading of output and error streams
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    Console.WriteLine("Minecraft server installation is starting...");

                    process.WaitForExit();
                }

                if (Insert_Into_DB)
                {
                    dbChanger.SetFunc($"{uniqueNumber}", $"{worldName}", "Fabric", $"{version}", $"{totalPlayers}", $"{rconPassword}");
                }
                Console.WriteLine("Installation completed!");

                object[,] filesToDelete = {
                { "fabric-server-launch.jar", "fabric-server-launcher.properties" }
                };

                foreach (var file in filesToDelete)
                {
                    if (File.Exists(customDirectory + $"\\{file}"))
                    {
                        File.Delete(customDirectory + $"\\{file}");
                    }
                }

                if (!File.Exists(customDirectory + $"\\mods"))
                {
                    Directory.CreateDirectory(customDirectory + $"\\mods");
                }

                await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port);

                string currentDirectory = Directory.GetCurrentDirectory();

                string serverPropertiesPath = System.IO.Path.Combine(customDirectory, "server.properties");
                string serverPropertiesPresetPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(currentDirectory))) + "\\Preset Files\\server.properties";
                if (!File.Exists(serverPropertiesPath))
                {
                    File.Copy(serverPropertiesPresetPath, serverPropertiesPath);
                    Console.WriteLine("Created missing server.properties file.");
                }

                AcceptEULA(fabricJarPath);

                DataChanger.SetInfo(rconSettings, serverPropertiesPath, true);
                DataChanger.SetInfo(worldSettings, serverPropertiesPath, true);

                await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task NeoForgeServerInitialisation(string customDirectory, string neoForgeJarPath, object[,] rconSettings, object[,] worldSettings, int ProcessMemoryAlocation, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, string ipAddress, int JMX_Port, int RCON_Port, bool Server_Auto_Start, bool Insert_Into_DB)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{neoForgeJarPath}\" --installServer",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = customDirectory // Directory where the server files will be installed
            };
            try
            {
                using (Process process = new Process { StartInfo = processInfo })
                {
                    int currentProgress = 0;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            // Determine progress based on output logs and update state
                            if (e.Data.Contains("Extracting main jar") && currentProgress < 10)
                            {
                                currentProgress = 10;
                                Console.WriteLine($"Progress: {currentProgress}% - Extracting main jar...");
                            }
                            else if (e.Data.Contains("Downloading library from") && currentProgress < 30)
                            {
                                currentProgress = 30;
                                Console.WriteLine($"Progress: {currentProgress}% - Downloading libraries...");
                            }
                            else if (e.Data.Contains("Checksum validated") && currentProgress < 50)
                            {
                                currentProgress = 50;
                                Console.WriteLine($"Progress: {currentProgress}% - Libraries validated...");
                            }
                            else if (e.Data.Contains("EXTRACT_FILES") && currentProgress < 70)
                            {
                                currentProgress = 70;
                                Console.WriteLine($"Progress: {currentProgress}% - Extracting server files...");
                            }
                            else if (e.Data.Contains("BUNDLER_EXTRACT") && currentProgress < 85)
                            {
                                currentProgress = 85;
                                Console.WriteLine($"Progress: {currentProgress}% - Processing bundled files...");
                            }
                            else if (e.Data.Contains("The server installed successfully") && currentProgress < 100)
                            {
                                currentProgress = 100;
                                Console.WriteLine($"Progress: {currentProgress}% - Files installation complete!");
                            }
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine($"Error: {e.Data}");
                        }
                    };

                    // Start the process
                    process.Start();

                    // Begin asynchronous reading of output and error streams
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    Console.WriteLine("Minecraft server installation is starting...");

                    process.WaitForExit();
                }

                if (Insert_Into_DB)
                {
                    dbChanger.SetFunc($"{uniqueNumber}", $"{worldName}", "NeoForge", $"{version}", $"{totalPlayers}", $"{rconPassword}");
                }
                Console.WriteLine("Installation completed!");

                File.Delete(neoForgeJarPath);

                object[,] filesToDelete = {
                { "run.bat", "run.sh", "user_jvm_args.txt", "installer.log" }
                };

                foreach (var file in filesToDelete)
                {
                    if (File.Exists(customDirectory + $"\\{file}"))
                    {
                        File.Delete(customDirectory + $"\\{file}");
                    }
                }

                if (!File.Exists(customDirectory + $"\\mods"))
                {
                    Directory.CreateDirectory(customDirectory + $"\\mods");
                }

                await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port);

                string currentDirectory = Directory.GetCurrentDirectory();

                string serverPropertiesPath = System.IO.Path.Combine(customDirectory, "server.properties");
                string serverPropertiesPresetPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(currentDirectory))) + "\\Preset Files\\server.properties";
                if (!File.Exists(serverPropertiesPath))
                {
                    File.Copy(serverPropertiesPresetPath, serverPropertiesPath);
                    Console.WriteLine("Created missing server.properties file.");
                }

                AcceptEULA(neoForgeJarPath);

                DataChanger.SetInfo(rconSettings, serverPropertiesPath, true);
                DataChanger.SetInfo(worldSettings, serverPropertiesPath, true);

                await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task PurpurServerInitialisation(string customDirectory, string purpurJarPath, object[,] rconSettings, object[,] worldSettings, int ProcessMemoryAlocation, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, string ipAddress, int JMX_Port, int RCON_Port, bool Server_Auto_Start, bool Insert_Into_DB)
        {
            // Set up the process to run the server
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-Xmx{ProcessMemoryAlocation}M -Xms{ProcessMemoryAlocation}M " + // nogui
                                    $"-Dcom.sun.management.jmxremote " +
                                    $"-Dcom.sun.management.jmxremote.port={JMX_Port} " +
                                    $"-Dcom.sun.management.jmxremote.authenticate=false " +
                                    $"-Dcom.sun.management.jmxremote.ssl=false " +
                                    $"-Djava.rmi.server.hostname={ipAddress} " + // Replace with actual server IP if necessary
                                    $"-jar \"{purpurJarPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(purpurJarPath) // Set the custom working directory
            };

            try
            {
                using (Process process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        Console.WriteLine("Failed to start the Minecraft server.");
                    }

                    Console.WriteLine("Minecraft server is starting...");

                    string serverPropertiesPath = System.IO.Path.Combine(customDirectory, "server.properties");
                    if (!File.Exists(serverPropertiesPath))
                    {
                        File.Create(serverPropertiesPath).Close();
                        Console.WriteLine("Created missing server.properties file.");
                    }
                    
                    // Read server output
                    while (process.StandardOutput.EndOfStream == false)
                    {
                        string? line = process.StandardOutput.ReadLine();
                        Console.WriteLine(line);

                        // Accept EULA if prompted
                        if (line != null && line.Contains("You need to agree to the EULA in order to run the server"))
                        {
                            if (ServerOperator.IsPortInUse(JMX_Port) || ServerOperator.IsPortInUse(RCON_Port))
                            {
                                ServerOperator.ClosePort(JMX_Port.ToString());
                                ServerOperator.ClosePort(RCON_Port.ToString());
                            }

                            AcceptEULA(purpurJarPath);
                            DataChanger.SetInfo(rconSettings, serverPropertiesPath, true);
                            if (Insert_Into_DB)
                            {
                                dbChanger.SetFunc($"{uniqueNumber}", $"{worldName}", "Purpur", $"{version}", $"{totalPlayers}", $"{rconPassword}");
                            }
                        }
                    }

                    process.WaitForExit();
                    Console.WriteLine("Starting minecraft server.");

                    DataChanger.SetInfo(worldSettings, serverPropertiesPath, true);

                    await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task SpigotServerInitialisation(string customDirectory, string spigotJarPath, object[,] rconSettings, object[,] worldSettings, int ProcessMemoryAlocation, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, string ipAddress, int JMX_Port, int RCON_Port, bool Server_Auto_Start, bool Insert_Into_DB)
        {
            // TO DO
        }

        private static async Task QuiltServerInitialisation(string customDirectory, string quiltJarPath, string tempFolderPath, object[,] rconSettings, object[,] worldSettings, int ProcessMemoryAlocation, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, string ipAddress, int JMX_Port, int RCON_Port, bool Server_Auto_Start, bool Insert_Into_DB)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{quiltJarPath}\" install server {version} --download-server",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = tempFolderPath // Directory where the server files will be installed
            };
            try
            {
                using (Process process = new Process { StartInfo = processInfo })
                {
                    int currentProgress = 0;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            // Determine progress based on output logs and update state
                            if (e.Data.Contains("Extracting main jar") && currentProgress < 10)
                            {
                                currentProgress = 10;
                                Console.WriteLine($"Progress: {currentProgress}% - Extracting main jar...");
                            }
                            else if (e.Data.Contains("Downloading library from") && currentProgress < 30)
                            {
                                currentProgress = 30;
                                Console.WriteLine($"Progress: {currentProgress}% - Downloading libraries...");
                            }
                            else if (e.Data.Contains("Checksum validated") && currentProgress < 50)
                            {
                                currentProgress = 50;
                                Console.WriteLine($"Progress: {currentProgress}% - Libraries validated...");
                            }
                            else if (e.Data.Contains("EXTRACT_FILES") && currentProgress < 70)
                            {
                                currentProgress = 70;
                                Console.WriteLine($"Progress: {currentProgress}% - Extracting server files...");
                            }
                            else if (e.Data.Contains("BUNDLER_EXTRACT") && currentProgress < 85)
                            {
                                currentProgress = 85;
                                Console.WriteLine($"Progress: {currentProgress}% - Processing bundled files...");
                            }
                            else if (e.Data.Contains("The server installed successfully") && currentProgress < 100)
                            {
                                currentProgress = 100;
                                Console.WriteLine($"Progress: {currentProgress}% - Files installation complete!");
                            }
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine($"Error: {e.Data}");
                        }
                    };

                    // Start the process
                    process.Start();

                    // Begin asynchronous reading of output and error streams
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    Console.WriteLine("Minecraft server installation is starting...");

                    process.WaitForExit();
                }

                if (Insert_Into_DB)
                {
                    dbChanger.SetFunc($"{uniqueNumber}", $"{worldName}", "Quilt", $"{version}", $"{totalPlayers}", $"{rconPassword}");
                }
                Console.WriteLine("Installation completed!");

                CopyFiles(System.IO.Path.Combine(tempFolderPath, "server"), customDirectory);

                object[,] filesToDelete = {
                { "quilt-server-launch.jar", "quilt-server-launcher.properties", ".cache" }
                };

                foreach (var file in filesToDelete)
                {
                    if (File.Exists(customDirectory + $"\\{file}"))
                    {
                        File.Delete(customDirectory + $"\\{file}");
                    }
                }

                ServerOperator.DeleteFiles(tempFolderPath, false);

                if (!File.Exists(customDirectory + $"\\mods"))
                {
                    Directory.CreateDirectory(customDirectory + $"\\mods");
                }

                await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port);

                string currentDirectory = Directory.GetCurrentDirectory();

                string serverPropertiesPath = System.IO.Path.Combine(customDirectory, "server.properties");
                string serverPropertiesPresetPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(currentDirectory))) + "\\Preset Files\\server.properties";
                if (!File.Exists(serverPropertiesPath))
                {
                    File.Copy(serverPropertiesPresetPath, serverPropertiesPath);
                    Console.WriteLine("Created missing server.properties file.");
                }

                AcceptEULA(quiltJarPath);

                DataChanger.SetInfo(rconSettings, serverPropertiesPath, true);
                DataChanger.SetInfo(worldSettings, serverPropertiesPath, true);

                await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        // -------------------------------- Help Functions --------------------------------

        private static void RenameFile(string fileName, string newName)
        {
            try
            {
                // Create a FileInfo object from the source file path.
                FileInfo fileInfo = new FileInfo(fileName);

                // Rename the file using File.Move method.
                File.Move(fileName, newName);

                // Print a success message.
                Console.WriteLine($"File has been renamed successfully from {fileName} to {newName}.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to rename the file. Error: {ex.Message}");
            }
        }

        public static string[] FindJarFile(string rootPath, string targetVersion)
        {
            string? jarPath = null;

            // Get all files in the specified directory
            string[] files = Directory.GetFiles(rootPath, "*.jar");

            foreach (string file in files)
            {
                string fileName = System.IO.Path.GetFileName(file);
                string[] parts = fileName.Split('-');

                if (parts[1].Contains(".jar"))
                {
                    parts[1] = parts[1].Replace(".jar", "");
                }
                if (parts.Length >= 1 && parts[1].Trim() == targetVersion)
                {
                    // Found the matching jar file
                    jarPath = System.IO.Path.Combine(rootPath, fileName);
                    // Use or copy the jar file as needed
                    return [jarPath, fileName];
                }
            }

            // Handle exceptions, such as no matching jar found
            if (jarPath == null)
            {
                
                throw new Exception("No jar file found with the specified version.");
            }
            return ["0"];
        }

        private static void AcceptEULA(string jarFilePath)
        {
            string serverDir = System.IO.Path.GetDirectoryName(jarFilePath);
            string eulaFile = System.IO.Path.Combine(serverDir, "eula.txt");

            try
            {
                System.IO.File.WriteAllText(eulaFile, "eula=true");
                Console.WriteLine("EULA accepted. Restart the server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write EULA file: {ex.Message}");
            }
        }

        private static string GenerateRandomNumber(int numDigits)
        {
            if (numDigits <= 0)
            {
                throw new ArgumentException("The number of digits must be a positive integer.");
            }

            Random random = new Random();

            // Generate the random number as a string
            char[] number = new char[numDigits];

            for (int i = 0; i < numDigits; i++)
            {
                // The first digit must be non-zero if numDigits > 1
                if (i == 0 && numDigits > 1)
                {
                    number[i] = (char)random.Next(1, 10).ToString()[0]; // 1 to 9
                }
                else
                {
                    number[i] = (char)random.Next(0, 10).ToString()[0]; // 0 to 9
                }
            }

            return new string(number);
        }

        public static string GenerateUniqueRandomNumber(int numOfDigits, string rootFolder)
        {
            string randomNumber;
            do
            {
                // Generate a random numOfDigits-digits number
                randomNumber = GenerateRandomNumber(numOfDigits);
            }
            while (CheckForMatchingFolder(rootFolder, randomNumber)); // Repeat if match is found

            return randomNumber;
        }

        public static bool CheckForMatchingFolder(string rootFolder, string folderName)
        {
            try
            {
                // Get all folder names in the root directory
                string[] folders = Directory.GetDirectories(rootFolder);
                List<object[]> worldExistingNumbers = dbChanger.SpecificDataFunc("SELECT worldNumber FROM worlds;");

                foreach (string folder in folders)
                {
                    // Get the folder name (not the full path)
                    string currentFolderName = System.IO.Path.GetFileName(folder);


                    if (currentFolderName == folderName)
                    {
                        return true;
                    }
                }
                foreach (object[] number in worldExistingNumbers)
                {
                    if (Convert.ToInt64(number[0]) == Convert.ToInt64(folderName))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return false;
        }

        public static string generatePassword(int passLength)
        {
            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";

            // Combine all character groups
            string allChars = upperChars + lowerChars + numbers;

            // Use a random generator
            Random random = new Random();
            StringBuilder password = new StringBuilder();

            // Ensure at least one character from each group
            password.Append(upperChars[random.Next(upperChars.Length)]);
            password.Append(lowerChars[random.Next(lowerChars.Length)]);
            password.Append(numbers[random.Next(numbers.Length)]);

            // Fill the rest of the password length with random characters
            for (int i = 3; i < passLength; i++) // Already added 3 characters
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the password to randomize the order
            char[] passwordArray = password.ToString().ToCharArray();
            for (int i = passwordArray.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (passwordArray[i], passwordArray[j]) = (passwordArray[j], passwordArray[i]);
            }

            return new string(passwordArray);
        }

        private static string FindClosestJarFile(string folderPath, string targetPattern)
        {
            try
            {
                // Ensure the folder exists
                if (!Directory.Exists(folderPath))
                {
                    Console.WriteLine("The specified folder does not exist.");
                    return null;
                }

                // Get all .jar files in the folder
                string[] jarFiles = Directory.GetFiles(folderPath, "*.jar");

                // Search for the closest match
                string bestMatch = null;
                int bestScore = int.MinValue;

                foreach (string file in jarFiles)
                {
                    string fileName = System.IO.Path.GetFileName(file); // Extract only the file name

                    // Check if the file name contains the target pattern
                    if (fileName.Contains(targetPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        // Calculate a score based on similarity (e.g., length or exact pattern match)
                        int score = CalculateMatchScore(fileName, targetPattern);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMatch = fileName; // Store only the file name
                        }
                    }
                }

                return $"{folderPath}\\{bestMatch}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        private static int CalculateMatchScore(string fileName, string targetPattern)
        {
            // Simple scoring: the more characters the file name matches with the pattern, the higher the score
            int matchLength = 0;

            for (int i = 0; i < Math.Min(fileName.Length, targetPattern.Length); i++)
            {
                if (fileName[i] == targetPattern[i])
                {
                    matchLength++;
                }
                else
                {
                    break; // Stop counting when characters differ
                }
            }

            return matchLength;
        }

        private static void CopyFiles(string sourceFolder, string destinationFolder)
        {
            if (!Directory.Exists(sourceFolder))
            {
                throw new DirectoryNotFoundException($"Source folder not found: {sourceFolder}");
            }

            // Ensure destination folder exists
            Directory.CreateDirectory(destinationFolder);

            // Get all files in the source folder
            string[] files = Directory.GetFiles(sourceFolder);

            foreach (string file in files)
            {
                string fileName = System.IO.Path.GetFileName(file);
                string destinationPath = System.IO.Path.Combine(destinationFolder, fileName);

                File.Copy(file, destinationPath, true); // Overwrite if exists
            }
        }


        // --------------------------------------------------------------------------------
    }

    class ServerOperator
    {
        // ------------------------- Main Server Operator Commands -------------------------
        public static async Task Start(string worldNumber, string serverPath, int processMemoryAlocation, string ipAddress, int JMX_Port, int RCON_Port, bool Auto_Stop = false)
        {
            while (IsPortInUse(RCON_Port) || IsPortInUse(JMX_Port))
            {
                Console.WriteLine("Port not closed! Closing Port");
                Thread.Sleep(1000);
            }

            if (!CheckFirewallRuleExists("MinecraftServer_TCP_25565") && !CheckFirewallRuleExists("MinecraftServer_UDP_25565"))
            {
                await NetworkConfigSetup.Setup(25565);
                return;
            }

            string JMX_Server_Settings = $"-Dcom.sun.management.jmxremote " +
                                         $"-Dcom.sun.management.jmxremote.port={JMX_Port} " +
                                         $"-Dcom.sun.management.jmxremote.authenticate=false " +
                                         $"-Dcom.sun.management.jmxremote.ssl=false " +
                                         $"-Djava.rmi.server.hostname={ipAddress} ";

            List<object[]> softwareList = dbChanger.SpecificDataFunc($"SELECT software FROM worlds where worldNumber = '{worldNumber}';");

            ProcessStartInfo? serverProcessInfo = null;

            string software = string.Join("\n", softwareList.Select(arr => string.Join(", ", arr)));

            //           ↓ For debugging! ↓
            Console.WriteLine($"worldNumber: '{worldNumber}'");
            Console.WriteLine($"Software: '{software}'");
            Console.WriteLine(software == "Vanilla");
            Console.WriteLine(software == "Forge");
            Console.WriteLine(software == "NeoForge");
            Console.WriteLine(software == "Fabric");
            Console.WriteLine(software == "Purpur");

            if (software == "Vanilla")
            {
                Console.WriteLine("Starting Vanilla Server!");

                if (!serverPath.Contains(".jar"))
                {
                    string version = dbChanger.SpecificDataFunc($"SELECT version FROM worlds where worldNumber = '{worldNumber}';")[0][0].ToString() + ".jar";
                    serverPath = System.IO.Path.Combine(serverPath, version);
                }

                serverProcessInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-Xmx{processMemoryAlocation}M -Xms{processMemoryAlocation}M " + // nogui
                                JMX_Server_Settings +
                                $"-jar \"{serverPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(serverPath) // Set the custom working directory
                };
            }
            else if (software == "Forge")
            {
                Console.WriteLine("Starting Forge Server!");

                string winArgsPath = FindFileInFolder(System.IO.Path.Combine(serverPath, "libraries"), "win_args.txt");

                if (winArgsPath != "")
                {
                    winArgsPath = $"@\"{winArgsPath}\" ";
                }

                string toRunJarFile = FindClosestJarFile(serverPath, "minecraft_server");

                if (toRunJarFile == null)
                {
                    toRunJarFile = FindClosestJarFile(serverPath, "forge-");

                    if (toRunJarFile == null)
                    {
                        Console.WriteLine("No server file found");
                    }
                }

                serverProcessInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-Xmx{processMemoryAlocation}M -Xms{processMemoryAlocation}M " +
                                JMX_Server_Settings +
                                $"{winArgsPath}" +
                                $"{toRunJarFile} %*", // nogui
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = serverPath
                };
            }
            else if (software == "NeoForge")
            {
                Console.WriteLine("Starting NeoForge Server!");

                string winArgsPath = FindFileInFolder(System.IO.Path.Combine(serverPath, "libraries"), "win_args.txt");

                if (winArgsPath != "")
                {
                    winArgsPath = $"@\"{winArgsPath}\" ";
                }

                string toRunJarFile = FindClosestJarFile(serverPath, "minecraft_server");

                if (toRunJarFile == null)
                {
                    toRunJarFile = FindClosestJarFile(serverPath, "server");

                    if (toRunJarFile == null)
                    {
                        Console.WriteLine("No server file found");
                    }
                }


                serverProcessInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-Xmx{processMemoryAlocation}M -Xms{processMemoryAlocation}M " +
                                JMX_Server_Settings +
                                $"{winArgsPath}" +
                                $"{toRunJarFile} %*", // nogui
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = serverPath
                };
            }
            else if (software == "Fabric")
            {
                Console.WriteLine("Starting Fabric Server!");

                string toRunJarFile = FindClosestJarFile(serverPath, "server");

                if (toRunJarFile == null)
                {
                    Console.WriteLine("No server file found");
                }

                serverProcessInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-Xmx{processMemoryAlocation}M -Xms{processMemoryAlocation}M " +
                                JMX_Server_Settings +
                                $"{toRunJarFile} ", // nogui
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = serverPath
                };
            }
            else if (software == "Quilt")
            {
                Console.WriteLine("Starting Quilt Server!");

                string toRunJarFile = FindClosestJarFile(serverPath, "server");

                if (toRunJarFile == null)
                {
                    Console.WriteLine("No server file found");
                }

                serverProcessInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-Xmx{processMemoryAlocation}M -Xms{processMemoryAlocation}M " +
                                JMX_Server_Settings +
                                $"{toRunJarFile} ", // nogui
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = serverPath
                };
            }
            else if (software == "Purpur")
            {
                Console.WriteLine("Starting Purpur Server!");

                if (!serverPath.Contains(".jar"))
                {
                    string version = dbChanger.SpecificDataFunc($"SELECT version FROM worlds where worldNumber = '{worldNumber}';")[0][0].ToString() + ".jar";
                    serverPath = System.IO.Path.Combine(serverPath, version);
                }

                serverProcessInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-Xmx{processMemoryAlocation}M -Xms{processMemoryAlocation}M " + // nogui
                                JMX_Server_Settings +
                                $"-jar \"{serverPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(serverPath) // Set the custom working directory
                };
            }

            using (Process process = Process.Start(serverProcessInfo))
            {
                if (process == null)
                {
                    Console.WriteLine("Failed to start the Minecraft server!");
                }

                Console.WriteLine("Minecraft server started!");

                //          ↓ For output traking ↓
                process.OutputDataReceived += async (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"{e.Data}"); // For debugging

                        if (e.Data.Contains("You need to agree to the EULA"))
                        {
                            Kill(RCON_Port, JMX_Port);
                        }
                        if (Auto_Stop == true && e.Data.Contains("RCON running on"))
                        {
                            await Stop("stop", worldNumber, ipAddress, RCON_Port, JMX_Port, true);
                        }
                    }
                };

                //     ↓ For error output traking ↓
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"Error: {e.Data}"); // For debugging
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                Console.WriteLine($"Process for the server has stopped with exit code: {process.ExitCode}");
            }
        }

        public static async Task Stop(string operation, string worldNumber, string ipAddress, int RCON_Port, int JMX_Port, bool instantStop = false)
        {
            if (!instantStop)
            {
                Countdown(5, operation, worldNumber, RCON_Port, ipAddress);
            }

            await InputForServer("stop", worldNumber, RCON_Port, ipAddress);

            bool RCON_Port_Closed = ClosePort(RCON_Port.ToString());
            bool JMX_Port_Closed = ClosePort(JMX_Port.ToString());

            //Console.WriteLine("RCON_Port_Closed: " + RCON_Port_Closed);
            //Console.WriteLine("JMX_Port_Closed: " + JMX_Port_Closed);
        }

        public static async Task Restart(string serverPath, string worldNumber, int processMemoryAlocation, string ipAddress, int RCON_Port, int JMX_Port)
        {
            await Stop("restart", worldNumber, "192.168.100.106", RCON_Port, JMX_Port);

            Start(worldNumber, serverPath, processMemoryAlocation, ipAddress, JMX_Port, RCON_Port);
        }

        public static void Kill(int RCON_Port, int JMX_Port)
        {
            bool RCON_Port_Closed = ClosePort(RCON_Port.ToString());
            bool JMX_Port_Closed = ClosePort(JMX_Port.ToString());

            Console.WriteLine("JMX_Port_Closed: " + JMX_Port_Closed);
            Console.WriteLine("RCON_Port_Closed: " + RCON_Port_Closed);
        }

        public static async Task InputForServer(string input, string worldNumber, int RCON_Port, string serverIp)
        {
            // Replace with your server details
            ushort port = (ushort)RCON_Port; // RCON port
            string password = "";
            List<object[]> data = dbChanger.GetFunc(worldNumber, true);

            foreach (var row in data)
            {
                password = (string)row[6]; // Your RCON password
            }

            try
            {
                // Parse the IP address
                var serverAddress = IPAddress.Parse(serverIp);

                // Create an RCON client
                using (var rcon = new RCON(serverAddress, port, password))
                {
                    Console.WriteLine("Connecting to the server...");
                    await rcon.ConnectAsync();

                    Console.WriteLine("Connected. Sending command...");

                    // Send a command
                    string response = await rcon.SendCommandAsync(input);
                    // Output the server response
                    Console.WriteLine($"Server response: {response}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public static async Task ChangeVersion(string worldNumber, string worldPath, string tempFolderPath, string serverVersionsPath, string rootFolder, int numberOfDigitsForWorldNumber, string version, string worldName, string software, int totalPlayers, object[,] worldSettings, int ProcessMemoryAlocation, string ipAddress, int JMX_Port, int RCON_Port, bool Keep_World_On_Version_Change = false)
        {
            if (Keep_World_On_Version_Change)
            {
                string rootWorldsFolder = System.IO.Path.Combine(rootFolder, "worlds");

                string curentSoftware = dbChanger.SpecificDataFunc($"SELECT software FROM worlds where worldNumber = '{worldNumber}';").ToString();

                if (software == "Vanilla" || software == "Fabric" || software == "Forge" || software == "NeoForge" || software == "Quilt" && curentSoftware != "Purpur")
                {
                    string rootWorldFilesFolder = System.IO.Path.Combine(rootFolder, $"worlds\\{worldNumber}");

                    Console.WriteLine("Copying world...");
                    CopyFiles("world", tempFolderPath, rootWorldFilesFolder);

                    DeleteFiles(worldPath, false);

                    dbChanger.SpecificDataFunc($"UPDATE worlds SET name = \"{worldName}\", version = \"{version}\", software = \"{software}\", totalPlayers = \"{totalPlayers}\" WHERE worldNumber = \"{worldNumber}\";");

                    Console.WriteLine("Creating New Server...");
                    await serverCreator.CreateServerFunc(rootFolder, rootWorldsFolder, tempFolderPath, 12, version, worldName, software, totalPlayers, worldSettings, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port, worldNumber, true, false, true);

                    object[,] filesToDelete = {
                        { "world" }
                    };

                    foreach (string file in filesToDelete)
                    {
                        try
                        {
                            string deletedFolderPath = DeleteFolderAndReturnPath(System.IO.Path.Combine(rootWorldFilesFolder, file));

                            if (!string.IsNullOrEmpty(deletedFolderPath))
                            {
                                Directory.CreateDirectory(deletedFolderPath);
                                CopyFolderToDirectory(System.IO.Path.Combine(tempFolderPath, file), deletedFolderPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                    }
                }
                else if (software == "Purpur")
                {
                    DeleteFiles(worldPath, false);

                    dbChanger.SpecificDataFunc($"UPDATE worlds SET name = \"{worldName}\", version = \"{version}\", software = \"{software}\", totalPlayers = \"{totalPlayers}\" WHERE worldNumber = \"{worldNumber}\";");

                    Console.WriteLine("Creating New Server...");
                    await serverCreator.CreateServerFunc(rootFolder, rootWorldsFolder, tempFolderPath, 12, version, worldName, software, totalPlayers, worldSettings, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port, worldNumber, true, false, true);
                }

                DeleteFiles(tempFolderPath, false);
            }
            else
            {
                string rootWorldsFolder = System.IO.Path.Combine(rootFolder, "worlds");

                DeleteFiles(worldPath, false);

                dbChanger.SpecificDataFunc($"UPDATE worlds SET name = \"{worldName}\", version = \"{version}\", software = \"{software}\", totalPlayers = \"{totalPlayers}\" WHERE worldNumber = \"{worldNumber}\";");

                Console.WriteLine("Creating New Server...");
                await serverCreator.CreateServerFunc(rootFolder, rootWorldsFolder, tempFolderPath, 12, version, worldName, software, totalPlayers, worldSettings, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port, worldNumber, true, false, true);
            }
        }

        public static void DeleteServer(string worldNumber, string serverDirectoryPath, bool deleteFromDB = true, bool deleteWholeDirectory = true)
        {
            if (deleteFromDB)
            {
                dbChanger.deleteWorldFromDB(worldNumber);
            }
            DeleteFiles(serverDirectoryPath, deleteWholeDirectory);
            Console.WriteLine("Done!");
        }


        // -------------------------------- Help Functions --------------------------------

        private static string DeleteFolderAndReturnPath(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true); // Delete folder and all contents
                Console.WriteLine($"Folder '{folderPath}' deleted successfully.");
                return folderPath; // Return the deleted folder path
            }
            else
            {
                Console.WriteLine($"Folder '{folderPath}' not found.");
                return string.Empty; // Return empty string if not found
            }
        }

        private static void CopyFolderToDirectory(string sourceFolder, string destinationFolder)
        {
            if (!Directory.Exists(sourceFolder))
            {
                Console.WriteLine($"Folder '{sourceFolder}' not found.");
                return;
            }

            CopyAll(new DirectoryInfo(sourceFolder), new DirectoryInfo(destinationFolder));
            Console.WriteLine($"Folder '{sourceFolder}' copied to '{destinationFolder}'.");
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName); // Ensure target directory exists

            // Copy each file in the folder
            foreach (FileInfo file in source.GetFiles())
            {
                string destFilePath = System.IO.Path.Combine(target.FullName, file.Name);
                file.CopyTo(destFilePath, true);
            }

            // Copy each subdirectory
            foreach (DirectoryInfo subDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(subDir.Name);
                CopyAll(subDir, nextTargetSubDir); // Recursively copy subdirectories
            }
        }

        public static void DeleteFiles(string path, bool deleteWholeDirectory = true)
        {
            try
            {
                if (deleteWholeDirectory)
                {
                    Directory.Delete(path, true); // Deletes the entire directory and its contents
                    Console.WriteLine($"Deleted entire directory: {path}");
                }
                else
                {
                    // Delete all files in the directory
                    foreach (string file in Directory.GetFiles(path))
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted file: {file}");
                    }

                    // Delete all subdirectories and their contents
                    foreach (string directory in Directory.GetDirectories(path))
                    {
                        Directory.Delete(directory, true);
                        Console.WriteLine($"Deleted directory: {directory}");
                    }

                    Console.WriteLine("All contents deleted successfully, but the directory itself remains.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void Countdown(int time, string action, string worldNumber, int RCON_Port, string serverIp)
        {
            _ = InputForServer($"say Server will {action} in...", worldNumber, RCON_Port, serverIp);
            Thread.Sleep(1000);

            for (int i = time; i > 0; i--)
            {
                _ = InputForServer($"say {i}", worldNumber, RCON_Port, serverIp);
                Thread.Sleep(1000);
            }
        }

        public static bool IsPortInUse(int port)
        {
            bool isAvailable = true;

            try
            {
                var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
                listener.Start();
                listener.Stop();
            }
            catch (Exception)
            {
                isAvailable = false;
            }

            return !isAvailable;
        }

        public static string FindClosestJarFile(string folderPath, string targetPattern)
        {
            try
            {
                // Ensure the folder exists
                if (!Directory.Exists(folderPath))
                {
                    Console.WriteLine("The specified folder does not exist.");
                    return null;
                }

                // Get all .jar files in the folder
                string[] jarFiles = Directory.GetFiles(folderPath, "*.jar");

                // Search for the closest match
                string bestMatch = null;
                int bestScore = int.MinValue;

                foreach (string file in jarFiles)
                {
                    string fileName = System.IO.Path.GetFileName(file); // Extract only the file name

                    // Check if the file name contains the target pattern
                    if (fileName.Contains(targetPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        // Calculate a score based on similarity (e.g., length or exact pattern match)
                        int score = CalculateMatchScore(fileName, targetPattern);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMatch = fileName; // Store only the file name
                        }
                    }
                }

                return $"-jar \"{folderPath}\\{bestMatch}\"";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        private static int CalculateMatchScore(string fileName, string targetPattern)
        {
            // Simple scoring: the more characters the file name matches with the pattern, the higher the score
            int matchLength = 0;

            for (int i = 0; i < Math.Min(fileName.Length, targetPattern.Length); i++)
            {
                if (fileName[i] == targetPattern[i])
                {
                    matchLength++;
                }
                else
                {
                    break; // Stop counting when characters differ
                }
            }

            return matchLength;
        }

        public static bool ClosePort(string port)
        {
            try
            {
                string findPidCommand = $"netstat -ano | findstr :{port}";
                string output = ExecuteCommand(findPidCommand);

                if (!string.IsNullOrWhiteSpace(output))
                {
                    string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 4)
                        {
                            string pid = parts[parts.Length - 1];

                            Console.WriteLine($"Found PID: {pid}");

                            string killCommand = $"taskkill /PID {pid} /F";
                            string killOutput = ExecuteCommand(killCommand);

                            if (!string.IsNullOrWhiteSpace(killOutput) && !killOutput.Contains("Error"))
                            {
                                Console.WriteLine($"Successfully terminated process with PID {pid}.");
                                return true;
                            }
                            else
                            {
                                Console.WriteLine($"Failed to terminate process with PID {pid}.");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"No process found on port {port}.");
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return false;
            }
        }

        private static string ExecuteCommand(string command)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/C " + command)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = Process.Start(processInfo);

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(error))
                {
                    return $"Error: {error}";
                }

                return output;
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        private static string FindFileInFolder(string folderPath, string fileName)
        {
            try
            {
                string[] files = Directory.GetFiles(folderPath, fileName, SearchOption.AllDirectories);

                return files.Length > 0 ? files[0] : "";
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied to folder: {folderPath}. Error: {ex.Message}");
                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return "";
            }
        }

        private static void CopyFiles(string folderName, string destinationDir, string rootDir)
        {
            try
            {
                string[] directories = Directory.GetDirectories(rootDir, folderName, SearchOption.AllDirectories);

                if (directories.Length == 0)
                {
                    Console.WriteLine($"Folder '{folderName}' was NOT found inside '{rootDir}'.");
                    return;
                }

                string sourcePath = directories[0]; // Take the first found match
                string destinationPath = System.IO.Path.Combine(destinationDir, folderName);

                Directory.CreateDirectory(destinationPath); // Ensure destination exists

                foreach (string file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        string relativePath = file.Substring(sourcePath.Length + 1); // Get relative path
                        string destFile = System.IO.Path.Combine(destinationPath, relativePath);
                        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destFile)); // Ensure subdirectory exists
                        File.Copy(file, destFile, true); // Copy file
                    }
                    catch (FileNotFoundException ex)
                    {
                        Console.WriteLine($"File not found: {ex.FileName}");
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        Console.WriteLine($"Directory not found: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                    }
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Directory not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static bool CheckFirewallRuleExists(string portName)
        {
            try
            {
                // Create a process to run the netsh command
                Process process = new Process();
                process.StartInfo.FileName = "netsh";
                process.StartInfo.Arguments = $"advfirewall firewall show rule name={portName}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                // Start the process
                process.Start();

                // Read the output of the command
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Check if the output contains the rule and port 25565
                if (output.Contains("MinecraftServer_TCP_25565") && output.Contains("25565"))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return false;
        }

        // --------------------------------------------------------------------------------
    }
}
