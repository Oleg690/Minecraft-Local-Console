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

namespace Server_General_Funcs
{
    class serverCreator
    {
        public static string CreateServerFunc(string rootFolder, string rootWorldsFolder, int numberOfDigitsForWorldNumber, string version, string worldName, string software, int totalPlayers, object[,] worldSettings, int processMemoryAlocation, string ipAddress, int JMX_Port, int RCON_Port)
        {
            // string rootFolder = @"D:\Minecraft-Server\important funcs for main aplication\Create Server Func\Create Server Func";
            string uniqueNumber = GenerateUniqueRandomNumber(numberOfDigitsForWorldNumber, rootWorldsFolder);

            // Path to the custom directory where server files will be stored
            string customDirectory = System.IO.Path.Combine(rootWorldsFolder, uniqueNumber);

            Directory.CreateDirectory(customDirectory);
            Console.WriteLine($"Created server directory: {customDirectory}");

            // Path to the Minecraft server .jar file
            string jarFoldersPath = rootFolder;
            switch (software)
            {
                case "Vanilla":
                    jarFoldersPath += @"\versions\Vanilla";
                    break;
                case "Forge":
                    jarFoldersPath += @"\versions\Forge";
                    break;
            }

            string versionName = version + ".jar";
            string jarFilePath = System.IO.Path.Combine(jarFoldersPath, versionName);

            // Check if the jar file exists
            if (!System.IO.File.Exists(jarFilePath))
            {
                Console.WriteLine("Server .jar file not found! Check the path.");
            }

            // Copy the .jar file to the unique folder
            string destinationJarPath = System.IO.Path.Combine(customDirectory, versionName);

            File.Copy(jarFilePath, destinationJarPath);
            Console.WriteLine("Server .jar file copied to the custom directory.");

            // RCON Password Generator
            string rconPassword = generatePassword(20);

            // RCON Settings array
            object[,] rconSettings = {
                { 13, "true" },
                { 44, $"{rconPassword}" },
                { 37, "false" }, // <- For online-mode=false
                { 32, $"{totalPlayers}" },
                { 12, "true" }
            };

            switch (software)
            {
                case "Vanilla":
                    Console.WriteLine("Vanilla Server");
                    vanillaServerInitialisation(customDirectory, destinationJarPath, rconSettings, worldSettings, processMemoryAlocation, uniqueNumber, worldName, version, totalPlayers, rconPassword, ipAddress, JMX_Port, RCON_Port);
                    return uniqueNumber;
                case "Forge":
                    Console.WriteLine("Forge Server");
                    fabricServerInitialisation(destinationJarPath, customDirectory);
                    return uniqueNumber;
            }

            return uniqueNumber;
        }

        //-------------------------------------------------------------------------------------
        // Main Server Type Installators
        private static void fabricServerInitialisation(string forgeJarPath, string worldPath)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{forgeJarPath}\" --installServer", // nogui
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = worldPath // Directory where the server files will be installed
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
                                Console.WriteLine($"Progress: {currentProgress}% - Installation complete!");
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

                    Console.WriteLine("Installation completed!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void vanillaServerInitialisation(string customDirectory, string destinationJarPath, object[,] rconSettings, object[,] worldSettings, int processMemoryAlocation, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, string ipAddress, int JMX_Port, int RCON_Port)
        {
            // Set up the process to run the server
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-Xmx{processMemoryAlocation}M -Xms{processMemoryAlocation}M -jar \"{destinationJarPath}\" ", // nogui
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = customDirectory // Set the custom working directory
            };

            int totalLibraries = 26;
            int unpackedLibraries = 0;

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
                            AcceptEULA(destinationJarPath);
                            DataChanger.SetInfo(rconSettings, serverPropertiesPath);
                            DataChanger.SetInfo(worldSettings, serverPropertiesPath);
                            dbChanger.SetFunc($"{uniqueNumber}", $"{worldName}", "Vanilla", $"{version}", $"{totalPlayers}", $"{rconPassword}");
                        }
                    }

                    process.WaitForExit();
                    Console.WriteLine("Starting minecraft server.");

                    serverOperator.Start(destinationJarPath, processMemoryAlocation, ipAddress, JMX_Port, RCON_Port);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        //-------------------------------------------------------------------------------------
        // Help Functions
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

        private static string GenerateUniqueRandomNumber(int numOfDigits, string rootFolder)
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

        private static bool CheckForMatchingFolder(string rootFolder, string folderName)
        {
            try
            {
                // Get all folder names in the root directory
                string[] folders = Directory.GetDirectories(rootFolder);
                List<object[]> worldExistingNumbers = dbChanger.GetSpecificDataFunc("SELECT worldNumber FROM worlds;");

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

        //-------------------------------------------------------------------------------------
    }

    class serverOperator
    {
        public static void Start(string serverPath, int processMemoryAlocation, string ipAddress, int JMX_Port, int RCON_Port)
        {
            while (IsPortInUse(RCON_Port) || IsPortInUse(JMX_Port))
            {
                Console.WriteLine("Port not closed!");
                Thread.Sleep(1000);
            }

            ProcessStartInfo serverProcessInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-Xmx{processMemoryAlocation}M -Xms{processMemoryAlocation}M " + // nogui
                $"-Dcom.sun.management.jmxremote " +
                $"-Dcom.sun.management.jmxremote.port={JMX_Port} " +
                $"-Dcom.sun.management.jmxremote.authenticate=false " +
                $"-Dcom.sun.management.jmxremote.ssl=false " +
                $"-Djava.rmi.server.hostname={ipAddress} " + // Replace with actual server IP if necessary
                $"-jar \"{serverPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(serverPath) // Set the custom working directory
            };

            using (Process process = Process.Start(serverProcessInfo))
            {
                if (process == null)
                {
                    Console.WriteLine("Failed to start the Minecraft server!");
                }

                Console.WriteLine("Minecraft server started.");

                // Read server output
                // while (!process.StandardOutput.EndOfStream)
                // {
                //    string line = process.StandardOutput.ReadLine();
                //    Console.WriteLine(line);
                // }

                process.WaitForExit();
                Console.WriteLine("Process for the server has stopped.");
            }
        }

        public static void Stop(string worldNumber, string ipAddress, int RCON_Port)
        {
            Countdown(5, "stop", worldNumber, RCON_Port, ipAddress);
            _ = InputForServer("save-all", worldNumber, RCON_Port, ipAddress);
            _ = InputForServer("stop", worldNumber, RCON_Port, ipAddress);
        }

        public static void Restart(string serverPath, string worldNumber, int processMemoryAlocation, string ipAddress, int RCON_Port, int JMX_Port)
        {
            Countdown(5, "restart", worldNumber, RCON_Port, ipAddress);
            _ = InputForServer("save-all", worldNumber, RCON_Port, ipAddress);
            _ = InputForServer("stop", worldNumber, RCON_Port, ipAddress);

            Start(serverPath, processMemoryAlocation, ipAddress, JMX_Port, RCON_Port);
        }

        public static async Task InputForServer(string input, string worldNumber, int RCON_Port, string serverIp)
        {
            // Replace with your server details
            ushort port = (ushort)RCON_Port;          // Default RCON port
            string password = "";
            List<object[]> data = dbChanger.GetFunc(worldNumber, true);

            foreach (var row in data)
            {
                password = (string)row[5]; // Your RCON password
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

        public static void DeleteServer(string worldNumber, string serverDirectoryPath, bool deleteFromDB = true, bool deleteWholeDirectory = true)
        {
            if (deleteFromDB)
            {
                dbChanger.deleteWorldFromDB(worldNumber);
            }
            DeleteFiles(serverDirectoryPath, deleteWholeDirectory);
        }

        //-------------------------------------------------------------------------------------
        // Help Functions
        private static void DeleteFiles(string path, bool deleteWholeDirectory)
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

        private static bool IsPortInUse(int port)
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

        //-------------------------------------------------------------------------------------
    }
}
