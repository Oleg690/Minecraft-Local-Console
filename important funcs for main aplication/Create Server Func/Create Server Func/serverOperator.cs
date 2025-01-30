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

namespace Server_General_Funcs
{
    class serverCreator
    {
        public static string CreateServerFunc(string rootFolder, string rootWorldsFolder, int numberOfDigitsForWorldNumber, string version, string worldName, string software, int totalPlayers, object[,] worldSettings, int ProcessMemoryAlocation, string ipAddress, int JMX_Port, int RCON_Port)
        {
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
                { "enable-rcon", "true" },
                { "rcon.password", $"{rconPassword}" },
                { "rcon.port", $"{RCON_Port}" },
                { "enable-query", "true" },
                { "online-mode", "false" }
            };

            Console.WriteLine($"{software} Server");

            if (software == "Vanilla")
            {
                VanillaServerInitialisation(customDirectory, destinationJarPath, rconSettings, worldSettings, ProcessMemoryAlocation, uniqueNumber, worldName, version, totalPlayers, rconPassword, ipAddress, JMX_Port, RCON_Port);
            }
            else if (software == "Forge")
            {
                ForgeServerInitialisation(destinationJarPath, customDirectory, uniqueNumber, worldName, version, totalPlayers, rconPassword, worldSettings, rconSettings, ipAddress, JMX_Port, RCON_Port, ProcessMemoryAlocation);
            }

            return uniqueNumber;
        }

        // ------------------------ Main Server Type Installators ------------------------
        private static void VanillaServerInitialisation(string customDirectory, string destinationJarPath, object[,] rconSettings, object[,] worldSettings, int processMemoryAlocation, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, string ipAddress, int JMX_Port, int RCON_Port)
        {
            // Set up the process to run the server
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-Xmx{processMemoryAlocation}M -Xms{processMemoryAlocation}M " + // nogui
                                    $"-Dcom.sun.management.jmxremote " +
                                    $"-Dcom.sun.management.jmxremote.port={JMX_Port} " +
                                    $"-Dcom.sun.management.jmxremote.authenticate=false " +
                                    $"-Dcom.sun.management.jmxremote.ssl=false " +
                                    $"-Djava.rmi.server.hostname={ipAddress} " + // Replace with actual server IP if necessary
                                    $"-jar \"{destinationJarPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(destinationJarPath) // Set the custom working directory
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

                    string eulaPath = System.IO.Path.Combine(customDirectory, "eula.txt");
                    if (!File.Exists(serverPropertiesPath))
                    {
                        File.Create(serverPropertiesPath).Close();
                        Console.WriteLine("Created missing eula.txt file.");
                    }

                    // Read server output
                    while (process.StandardOutput.EndOfStream == false)
                    {
                        string? line = process.StandardOutput.ReadLine();
                        Console.WriteLine(line);

                        // Accept EULA if prompted
                        if (line != null && line.Contains("You need to agree to the EULA in order to run the server"))
                        {
                            if (serverOperator.IsPortInUse(JMX_Port) || serverOperator.IsPortInUse(RCON_Port))
                            {
                                serverOperator.ClosePort(JMX_Port.ToString());
                                serverOperator.ClosePort(RCON_Port.ToString());
                            }

                            AcceptEULA(destinationJarPath);
                            DataChanger.SetInfo(rconSettings, serverPropertiesPath, true);
                            dbChanger.SetFunc($"{uniqueNumber}", $"{worldName}", "Vanilla", $"{version}", $"{totalPlayers}", $"{rconPassword}");
                        }
                    }

                    process.WaitForExit();
                    Console.WriteLine("Starting minecraft server.");

                    DataChanger.SetInfo(worldSettings, serverPropertiesPath, true);
                    serverOperator.Start(uniqueNumber, destinationJarPath, processMemoryAlocation, ipAddress, JMX_Port, RCON_Port);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void ForgeServerInitialisation(string forgeJarPath, string customDirectory, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, object[,] worldSettings, object[,] rconSettings, string ipAddress, int JMX_Port, int RCON_Port, int ProcessMemoryAlocation)
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


                dbChanger.SetFunc($"{uniqueNumber}", $"{worldName}", "Forge", $"{version}", $"{totalPlayers}", $"{rconPassword}");
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

                serverOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port);
                
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

                serverOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, JMX_Port, RCON_Port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        // -------------------------------- Help Functions --------------------------------
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

        // --------------------------------------------------------------------------------
    }

    class serverOperator
    {
        // ------------------------- Main Server Operator Commands -------------------------
        public static void Start(string worldNumber, string serverPath, int processMemoryAlocation, string ipAddress, int JMX_Port, int RCON_Port)
        {
            while (IsPortInUse(RCON_Port) || IsPortInUse(JMX_Port))
            {
                Console.WriteLine("Port not closed! Closing Port");
                Thread.Sleep(1000);
            }

            string JMX_Server_Settings = $"-Dcom.sun.management.jmxremote " +
                                         $"-Dcom.sun.management.jmxremote.port={JMX_Port} " +
                                         $"-Dcom.sun.management.jmxremote.authenticate=false " +
                                         $"-Dcom.sun.management.jmxremote.ssl=false " +
                                         $"-Djava.rmi.server.hostname={ipAddress} "; // Replace with actual server IP if necessary

            List<object[]> software = dbChanger.GetSpecificDataFunc($"SELECT software FROM worlds where worldNumber = '{worldNumber}';");

            ProcessStartInfo? serverProcessInfo = null;

            switch (software[0][0])
            {
                case "Vanilla":
                    Console.WriteLine("Starting Vanilla Server!");

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
                    break;
                case "Forge":
                    Console.WriteLine("Starting Forge Server!");

                    if (System.IO.Path.GetFileName(serverPath).Contains(".jar"))
                    {
                        serverPath = System.IO.Path.GetDirectoryName(serverPath);
                    }

                    string winArgsPath = FindFileInFolder(System.IO.Path.Combine(serverPath, "libraries"), "win_args.txt");

                    if (winArgsPath != "")
                    {
                        winArgsPath = $"@\"{winArgsPath}\" ";
                    }

                    string toRunJarFile = "";

                    toRunJarFile = FindClosestJarFile(serverPath, "minecraft_server");

                    if (toRunJarFile == null)
                    {
                        toRunJarFile = FindClosestJarFile(serverPath, "forge-");
                    }
                    if (toRunJarFile == null)
                    {
                        Console.WriteLine("No server file found");
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
                    break;
            }

            using (Process process = Process.Start(serverProcessInfo))
            {
                if (process == null)
                {
                    Console.WriteLine("Failed to start the Minecraft server!");
                }

                Console.WriteLine("Minecraft server started!");

                //          ↓ For output traking ↓
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"{e.Data}"); // For debugging
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

        public static async void Stop(string operation, string worldNumber, string ipAddress, int RCON_Port, int JMX_Port, bool instantStop = false)
        {
            if (!instantStop)
            {
                Countdown(5, operation, worldNumber, RCON_Port, ipAddress);
            }

            await InputForServer("stop", worldNumber, RCON_Port, ipAddress);

            bool RCON_Port_Closed = ClosePort(RCON_Port.ToString());
            bool JMX_Port_Closed = ClosePort(JMX_Port.ToString());

            Console.WriteLine("RCON_Port_Closed: " + RCON_Port_Closed);
            Console.WriteLine("JMX_Port_Closed: " + JMX_Port_Closed);
        }

        public static void Restart(string serverPath, string worldNumber, int processMemoryAlocation, string ipAddress, int RCON_Port, int JMX_Port)
        {
            Stop("restart", worldNumber, ipAddress, RCON_Port, JMX_Port);

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

        public static void DeleteServer(string worldNumber, string serverDirectoryPath, bool deleteFromDB = true, bool deleteWholeDirectory = true)
        {
            if (deleteFromDB)
            {
                dbChanger.deleteWorldFromDB(worldNumber);
            }
            DeleteFiles(serverDirectoryPath, deleteWholeDirectory);
        }

        // -------------------------------- Help Functions --------------------------------
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

        // --------------------------------------------------------------------------------
    }
}
