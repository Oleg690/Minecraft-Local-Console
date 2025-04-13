using CoreRCON;
using databaseChanger;
using Logger;
using NetworkConfig;
using serverPropriertiesChanger;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;
using Updater;
using Minecraft_Console;
using MinecraftServerStats;

namespace CreateServerFunc
{
    [SupportedOSPlatform("windows")]
    class ServerCreator
    {
        public static readonly string? rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))) ?? string.Empty;
        private static readonly string? UpdaterLastCheck = Path.Combine(rootFolder, "lastQuiltorFabricCheckUpdater.txt");

        private static readonly TimeSpan DelayTime = TimeSpan.FromHours(72);

        // ------------------------ Main Create Server Function ------------------------
        public static async Task<string> CreateServerFunc(string rootFolder, string rootWorldsFolder, string tempFolderPath, string defaultServerPropertiesPath, string version, string worldName, string software, int totalPlayers, object[,] worldSettings, int ProcessMemoryAlocation, string ipAddress, int Server_Port, int JMX_Port, int RCON_Port, int RMI_Port, string? worldNumber = null, bool Server_Auto_Start = true, bool Insert_Into_DB = true)
        {
            MainWindow.SetLoadingBarProgress(5);

            if (string.IsNullOrEmpty(software)) return ServerOperator.LogError("Software not selected!");
            if (string.IsNullOrEmpty(worldName)) worldName = $"{software} Server";

            if (!VersionsUpdater.CheckVersionExists(software, version)) return ServerOperator.LogError($"{software} {version} is not supported!");
            await CheckVersions(rootFolder, software, version);

            if (!ServerOperator.CheckFilesAndNetworkSettings(Server_Port, JMX_Port, RMI_Port))
            {
                await NetworkSetup.Setup(Server_Port, JMX_Port, RMI_Port);
            }

            // Making the custom folder for the new world
            string uniqueNumber = worldNumber ?? GenerateUniqueRandomNumber(12, rootWorldsFolder);
            string customDirectory = Path.Combine(rootWorldsFolder, uniqueNumber);
            Directory.CreateDirectory(customDirectory);
            CodeLogger.ConsoleLog($"Created server directory: {customDirectory}");

            // Founding the verison in the versions folder
            string jarFileName = GetJarFilePath(rootFolder, software, version);
            string jarFilePath = Path.Combine(rootFolder, $"versions\\{software}\\" + jarFileName);
            if (string.IsNullOrEmpty(jarFilePath) || !File.Exists(jarFilePath)) return ServerOperator.LogError("Server .jar file not found! Check the path.");

            // Copying the version to the new world folder for it to be used
            string destinationJarPath = Path.Combine(customDirectory, Path.GetFileName(jarFilePath));
            File.Copy(jarFilePath, destinationJarPath);
            string jarPath = Path.Combine(Path.GetDirectoryName(destinationJarPath) ?? throw new Exception("Failed to get the destinationJarPath file name."), version + ".jar");
            RenameFile(destinationJarPath, jarPath);
            CodeLogger.ConsoleLog("Server .jar file copied to the custom directory.");

            string rconPassword = worldNumber != null ? GetRconPassword(worldNumber) : GeneratePassword(20);

            object[,] rconSettings = {
                { "enable-rcon", "true" },
                { "rcon.password", $"{rconPassword}" },
                { "rcon.port", $"{RCON_Port}" },
                { "enable-query", "true" },
            };

            CodeLogger.ConsoleLog($"Seting world settings...");

            ProcessStartInfo processInfo = new()
            {
                FileName = "java",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = software == "Quilt" ? tempFolderPath : customDirectory
            };

            string serverPropertiesNewPath = Path.Combine(customDirectory, "server.properties");

            if (!File.Exists(serverPropertiesNewPath) && File.Exists(defaultServerPropertiesPath))
            {
                File.Copy(defaultServerPropertiesPath, serverPropertiesNewPath);
                CodeLogger.ConsoleLog("Created missing server.properties file.");
            }

            MainWindow.SetLoadingBarProgress(10);

            DataChanger.SetInfo(worldSettings, serverPropertiesNewPath, true);
            DataChanger.SetInfo(rconSettings, serverPropertiesNewPath, true);

            await WaitForPortClosure(RCON_Port, JMX_Port);

            CodeLogger.ConsoleLog($"Creating {software} Server!");

            var requiresInstallation = new Dictionary<string, string>
            {
                { "Forge", $"-jar \"{jarPath}\" --installServer" },
                { "Fabric", $"-jar \"{jarPath}\" server -mcversion {version} -downloadMinecraft" },
                { "Quilt", $"-jar \"{jarPath}\" install server {version} --download-server" }
            };

            if (requiresInstallation.TryGetValue(software, out var arguments))
            {
                processInfo.Arguments = arguments;
                BasicServerInstaller(processInfo, software);

                if (software == "Quilt")
                {
                    CopyFiles(Path.Combine(tempFolderPath, "server"), customDirectory);
                    ServerOperator.DeleteFiles(tempFolderPath, false);
                }
            }

            await BasicServerInitializator(software, customDirectory, jarPath, rconSettings, worldSettings, ProcessMemoryAlocation, uniqueNumber, worldName, version, totalPlayers, rconPassword, ipAddress, Server_Port, JMX_Port, RCON_Port, RMI_Port, Insert_Into_DB);

            dbChanger.SpecificDataFunc($"UPDATE worlds SET Process_ID = NULL WHERE worldNumber = \"{uniqueNumber}\";");
            dbChanger.SpecificDataFunc($"UPDATE worlds SET serverUser = NULL WHERE worldNumber = \"{uniqueNumber}\";");
            dbChanger.SpecificDataFunc($"UPDATE worlds SET serverTempPsw = NULL WHERE worldNumber = \"{uniqueNumber}\";");

            CodeLogger.ConsoleLog("World Created Succeasfully");
            MainWindow.SetLoadingBarProgress(100);

            return uniqueNumber;
        }

        // -------------------------------- Help Functions --------------------------------

        private static void BasicServerInstaller(ProcessStartInfo processInfo, string software)
        {
            try
            {
                using (Process process = new() { StartInfo = processInfo })
                {
                    int currentProgress = 0;
                    int checksumCount = 0;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (string.IsNullOrEmpty(e.Data)) return;

                        if (software == "Forge")
                        {
                            if (e.Data.Contains("Extracting main jar") && currentProgress < 10)
                            {
                                currentProgress = 10;
                                MainWindow.SetLoadingBarProgress(15);
                                CodeLogger.ConsoleLog($"Progress: {currentProgress}% - Extracting main jar...");
                            }
                            else if (e.Data.Contains("Downloading libraries") && currentProgress < 30)
                            {
                                currentProgress = 30;
                                MainWindow.SetLoadingBarProgress(20);
                                CodeLogger.ConsoleLog($"Progress: {currentProgress}% - Downloading libraries...");
                            }
                            else if (e.Data.Contains("Checksum validated"))
                            {
                                checksumCount++;
                                if (checksumCount >= 3 && currentProgress < 50)
                                {
                                    currentProgress = 50;
                                    MainWindow.SetLoadingBarProgress(25);
                                    CodeLogger.ConsoleLog($"Progress: {currentProgress}% - Libraries validated...");
                                }
                            }
                            else if (e.Data.Contains("EXTRACT_FILES") && currentProgress < 70)
                            {
                                currentProgress = 70;
                                MainWindow.SetLoadingBarProgress(30);
                                CodeLogger.ConsoleLog($"Progress: {currentProgress}% - Extracting server files...");
                            }
                            else if (e.Data.Contains("BUNDLER_EXTRACT") && currentProgress < 85)
                            {
                                currentProgress = 85;
                                MainWindow.SetLoadingBarProgress(35);
                                CodeLogger.ConsoleLog($"Progress: {currentProgress}% - Processing bundled files...");
                            }
                            else if (e.Data.Contains("The server installed successfully") && currentProgress < 100)
                            {
                                currentProgress = 100;
                                MainWindow.SetLoadingBarProgress(40);
                                CodeLogger.ConsoleLog($"Progress: {currentProgress}% - Installation complete!");
                            }
                        }
                        else
                        {
                            Console.WriteLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            CodeLogger.ConsoleLog($"Error: {e.Data}");
                        }
                    };

                    // Start the process
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    CodeLogger.ConsoleLog($"Minecraft {software} server installation is starting...");
                    process.WaitForExit();
                }

                CodeLogger.ConsoleLog("Installation completed!");
                MainWindow.loadingScreenProcentage = 45;
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error: {ex.Message}");
            }
        }

        private static async Task BasicServerInitializator(string software, string customDirectory, string JarPath, object[,] rconSettings, object[,] worldSettings, int ProcessMemoryAlocation, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, string ipAddress, int Server_Port, int JMX_Port, int RCON_Port, int RMI_Port, bool Insert_Into_DB = false)
        {
            try
            {
                if (Insert_Into_DB) dbChanger.SetFunc($"{uniqueNumber}", $"{worldName}", $"{software}", $"{version}", $"{totalPlayers}", $"{Server_Port}", $"{JMX_Port}", $"{RCON_Port}", $"{RMI_Port}", $"{rconPassword}");
                MainWindow.SetLoadingBarProgress(50);
                await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, Server_Port, JMX_Port, RCON_Port, RMI_Port, Auto_Stop: true);
                MainWindow.SetLoadingBarProgress(70);
                AcceptEULA(JarPath);
                MainWindow.SetLoadingBarProgress(75);
                await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, Server_Port, JMX_Port, RCON_Port, RMI_Port, Auto_Stop: true);
                MainWindow.SetLoadingBarProgress(99);
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error: {ex.Message}");
            }
        }

        private static void RenameFile(string fileName, string newName)
        {
            try
            {
                FileInfo fileInfo = new(fileName);
                File.Move(fileName, newName);

                CodeLogger.ConsoleLog($"File has been renamed successfully.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to rename the file. Error: {ex.Message}");
            }
        }

        private static string[] FindJarFile(string rootPath, string targetVersion)
        {
            string? jarPath = null;

            // Get all files in the specified directory
            string[] files = Directory.GetFiles(rootPath, "*.jar");
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string[] parts = fileName.Split('-');

                if (parts[1].Contains(".jar"))
                {
                    parts[1] = parts[1].Replace(".jar", "");
                }
                if (parts.Length >= 1 && parts[1].Trim() == targetVersion)
                {
                    // Found the matching jar file
                    jarPath = Path.Combine(rootPath, fileName);
                    // Use or copy the jar file as needed
                    return [jarPath, fileName];
                }
            }

            // Handle exceptions, such as no matching jar found
            if (jarPath == null)
            {
                CodeLogger.ConsoleLog("No jar file found with the specified version.");
            }
            return ["0", "No jar file found with the specified version."];
        }

        private static void AcceptEULA(string jarFilePath)
        {
            string? serverDir = Path.GetDirectoryName(jarFilePath) ?? throw new Exception("Failed to get the jar file.");
            string eulaFile = Path.Combine(serverDir, "eula.txt");

            try
            {
                File.WriteAllText(eulaFile, "eula=true");
                CodeLogger.ConsoleLog("EULA accepted. Restart the server.");
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Failed to write EULA file: {ex.Message}");
            }
        }

        public static string GenerateRandomNumber(int numDigits)
        {
            if (numDigits <= 0)
            {
                throw new ArgumentException("The number of digits must be a positive integer.");
            }

            Random random = new();

            // Generate the random number as a string
            char[] number = new char[numDigits];

            for (int i = 0; i < numDigits; i++)
            {
                // The first digit must be non-zero if numDigits > 1
                if (i == 0 && numDigits > 1)
                {
                    number[i] = random.Next(1, 10).ToString()[0]; // 1 to 9
                }
                else
                {
                    number[i] = random.Next(0, 10).ToString()[0]; // 0 to 9
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
                List<object[]> worldExistingNumbers = dbChanger.SpecificDataFunc("SELECT worldNumber FROM worlds;");

                foreach (string folder in folders)
                {
                    // Get the folder name (not the full path)
                    string currentFolderName = Path.GetFileName(folder);


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
                CodeLogger.ConsoleLog($"An error occurred: {ex.Message}");
            }

            return false;
        }

        private static string GeneratePassword(int passLength)
        {
            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";

            // Combine all character groups
            string allChars = upperChars + lowerChars + numbers;

            // Use a random generator
            Random random = new();
            StringBuilder password = new();

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
                string fileName = Path.GetFileName(file);
                string destinationPath = Path.Combine(destinationFolder, fileName);

                File.Copy(file, destinationPath, true); // Overwrite if exists
            }
        }

        private static bool ShouldRunNow()
        {
            if (!File.Exists(UpdaterLastCheck))
            {
                return true; // First run
            }

            string lastRunTimeStr = File.ReadAllText(UpdaterLastCheck);
            if (DateTime.TryParse(lastRunTimeStr, out DateTime lastRunTime))
            {
                return DateTime.UtcNow - lastRunTime >= DelayTime;
            }

            return true; // Run if the timestamp is corrupted
        }

        private static async Task CheckVersions(string rootFolder, string software, string version)
        {
            CodeLogger.ConsoleLog($"Software: '{software}'; Version: '{version}';");

            string serverVersionsPath = Path.Combine(rootFolder, "versions");
            if (!Path.Exists(Path.Combine(serverVersionsPath, software)))
            {
                Directory.CreateDirectory(Path.Combine(serverVersionsPath, software));
            }
            var localFiles = Directory.GetFiles(Path.Combine(serverVersionsPath, software), "*.jar");

            bool _contiune = false;

            if (software == "Quilt" || software == "Fabric")
            {
                if (ShouldRunNow() || localFiles.Length == 0)
                {
                    CodeLogger.ConsoleLog("Checking for updates...");
                    await VersionsUpdater.Update(serverVersionsPath, software);
                    File.WriteAllText(UpdaterLastCheck ?? throw new Exception("Failed to write in the UpdaterLastCheck.txt file."), DateTime.UtcNow.ToString("o"));
                    localFiles = Directory.GetFiles(Path.Combine(serverVersionsPath, software), "*.jar");
                    if (localFiles.Length == 0)
                    {
                        CodeLogger.ConsoleLog("Error downloading the server file.");
                        return;
                    }
                }
            }
            else
            {
                foreach (var localVersion in localFiles)
                {
                    if (localVersion.Contains(version))
                    {
                        _contiune = true;
                        break;
                    }
                }
                if (!_contiune)
                {
                    CodeLogger.ConsoleLog("Version not found in local files! Downloading it...");
                    await VersionsUpdater.Update(serverVersionsPath, software, version);

                    localFiles = Directory.GetFiles(Path.Combine(serverVersionsPath, software), "*.jar");
                    foreach (var localVersion in localFiles)
                    {
                        if (localVersion.Contains(version))
                        {
                            _contiune = true;
                            break;
                        }
                    }
                    if (!_contiune)
                    {
                        CodeLogger.ConsoleLog($"Error downloading the server file with the version {version}.");
                        return;
                    }
                }
            }
        }

        private static async Task WaitForPortClosure(int rconPort, int jmxPort)
        {
            while (ServerOperator.IsPortInUse(rconPort) || ServerOperator.IsPortInUse(jmxPort))
            {
                CodeLogger.ConsoleLog("Port not closed!");
                await Task.Delay(1000);
            }
        }

        private static string GetRconPassword(string worldNumber)
        {
            List<object[]> data = dbChanger.GetFunc(worldNumber, true);
            return data.Count > 0 ? (string)data[0][10] : GeneratePassword(20);
        }

        private static string GetJarFilePath(string rootFolder, string software, string version)
        {
            string jarFoldersPath = Path.Combine(rootFolder, "versions", software);
            return software switch
            {
                "Fabric" or "Quilt" => ServerOperator.FindClosestJarFile(jarFoldersPath, "installer", true) ?? string.Empty,
                _ => FindJarFile(jarFoldersPath, version)?[1] ?? string.Empty
            };
        }

        // --------------------------------------------------------------------------------
    }

    [SupportedOSPlatform("windows")]
    class ServerOperator
    {
        private static readonly string? StartupTimePath = ServerCreator.rootFolder != null ? Path.Combine(ServerCreator.rootFolder, "serverStartupTime.txt") : null;
        private static readonly string? JMX_Access_File_Path = ServerCreator.rootFolder != null ? Path.Combine(ServerCreator.rootFolder, "jmx\\jmxremote.access") : null;
        private static readonly string? JMX_Password_File_Path = ServerCreator.rootFolder != null ? Path.Combine(ServerCreator.rootFolder, "jmx\\jmxremote.password") : null;

        private static ProcessStartInfo? serverProcessInfo = new();

        // ------------------------- Main Server Operator Commands -------------------------
        public static async Task Start(string worldNumber, string serverPath, int processMemoryAlocation, string ipAddress, int Server_Port, int JMX_Port, int RCON_Port, int RMI_Port, bool Auto_Stop = false, bool noGUI = true, ServerInfoViewModel? viewModel = null)
        {
            MainWindow.serverRunning = true;
            bool verificator = true;

            void ValidateInput(string input, string errorMessage)
            {
                if (string.IsNullOrEmpty(input))
                {
                    CodeLogger.ConsoleLog(errorMessage);
                    verificator = false;
                    return;
                }
            }

            void ValidateDirectory(string path, string errorMessage)
            {
                if (!Directory.Exists(path))
                {
                    CodeLogger.ConsoleLog(errorMessage);
                    verificator = false;
                    return;
                }
            }

            ValidateInput(worldNumber, "No worldNumber was supplied!");
            ValidateDirectory(serverPath, "No local server files were found!");

            string? software = dbChanger.SpecificDataFunc($"SELECT software FROM worlds where worldNumber = '{worldNumber}';")[0][0].ToString() ?? String.Empty;
            ValidateInput(software, "No software was supplied!");
            if (!verificator) return;

            while (IsPortInUse(RCON_Port) || IsPortInUse(JMX_Port))
            {
                CodeLogger.ConsoleLog("Port not closed!");
                Thread.Sleep(1000);
            }

            if (!CheckFilesAndNetworkSettings(Server_Port, JMX_Port, RMI_Port))
            {
                await NetworkSetup.Setup(Server_Port, JMX_Port, RMI_Port);
            }

            //           ↓ For debugging! ↓
            CodeLogger.ConsoleLog($"World Number: '{worldNumber}'");
            CodeLogger.ConsoleLog($"Software: '{software}'");

            string user = "admin";
            string psw = ServerCreator.GenerateRandomNumber(5);

            void WriteToFile(string path, string content, string fileType)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllText(path, content);
                }
                else
                {
                    CodeLogger.ConsoleLog($"{fileType} path is null. Cannot write to file.");
                }
            }

            if (File.Exists(JMX_Password_File_Path) && File.Exists(JMX_Access_File_Path))
            {
                WriteToFile(JMX_Access_File_Path, $"{user} readwrite", "JMX Access File");
                WriteToFile(JMX_Password_File_Path, $"{user} {psw}", "JMX Password File");
            }

            string Server_Settings = $"-Xmx{processMemoryAlocation}M -Xms{processMemoryAlocation}M " +
                                     $"-Dcom.sun.management.jmxremote " +
                                     $"-Dcom.sun.management.jmxremote.port={JMX_Port} " +
                                     $"-Dcom.sun.management.jmxremote.rmi.port={RMI_Port} " +
                                     $"-Dcom.sun.management.jmxremote.ssl=false " +
                                     $"-Dcom.sun.management.jmxremote.authenticate=true " +
                                     $"-Dcom.sun.management.jmxremote.access.file=\"{JMX_Access_File_Path}\" " +
                                     $"-Dcom.sun.management.jmxremote.password.file=\"{JMX_Password_File_Path}\" " +
                                     $"-Djava.rmi.server.hostname={ipAddress} ";

            serverProcessInfo = new()
            {
                FileName = "java",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = serverPath
            };

            CodeLogger.ConsoleLog($"Starting {software} Server!");

            string? toRunJarFile = "";
            string GetVersionedJarFile() => Path.Combine(serverPath, dbChanger.SpecificDataFunc($"SELECT version FROM worlds where worldNumber = '{worldNumber}';")[0][0].ToString() + ".jar");

            void SetServerArguments(string arguments)
            {
                if (serverProcessInfo == null) return;
                serverProcessInfo.Arguments = arguments;
            }

            switch (software)
            {
                case "Vanilla":
                case "NeoForge":
                case "Purpur":
                case "Paper":
                    toRunJarFile = GetVersionedJarFile();
                    SetServerArguments($"{Server_Settings} -jar \"{toRunJarFile}\"");
                    break;
                case "Forge":
                    string winArgsPath = FindFileInFolder(Path.Combine(serverPath, "libraries"), "win_args.txt");
                    if (!string.IsNullOrEmpty(winArgsPath)) winArgsPath = $"@\"{winArgsPath}\"";

                    toRunJarFile = FindClosestJarFile(serverPath, "minecraft_server") ?? FindClosestJarFile(serverPath, "forge-");
                    if (toRunJarFile == null) CodeLogger.ConsoleLog("No server file found");

                    SetServerArguments($"{Server_Settings} {winArgsPath} {toRunJarFile}");
                    break;
                case "Fabric":
                case "Quilt":
                    toRunJarFile = FindClosestJarFile(serverPath, "server");
                    if (toRunJarFile == null) CodeLogger.ConsoleLog("No server file found");

                    SetServerArguments($"{Server_Settings} {toRunJarFile}");
                    break;
                default:
                    CodeLogger.ConsoleLog($"Invalid software type!");
                    return;
            }

            serverProcessInfo.Arguments += noGUI ? " nogui" : "";

            using (Process? process = Process.Start(serverProcessInfo))
            {
                if (process == null)
                {
                    CodeLogger.ConsoleLog("Failed to start the Minecraft server!");
                    return;
                }

                dbChanger.SpecificDataFunc($"UPDATE worlds SET Process_ID = \"{process.Id}\" WHERE worldNumber = \"{worldNumber}\";");
                dbChanger.SpecificDataFunc($"UPDATE worlds SET serverUser = \"{user}\" WHERE worldNumber = \"{worldNumber}\";");
                dbChanger.SpecificDataFunc($"UPDATE worlds SET serverTempPsw = \"{psw}\" WHERE worldNumber = \"{worldNumber}\";");

                CodeLogger.ConsoleLog("Minecraft server started!");

                RecordServerStart();

                //          ↓ For output traking ↓
                process.OutputDataReceived += async (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"{e.Data}"); // For debugging

                        if (Auto_Stop && (e.Data.Contains("You need to agree to the EULA") || e.Data.Contains("Done")))
                        {
                            Kill(RCON_Port, JMX_Port);
                        }
                        else if (Auto_Stop == true && e.Data.Contains("RCON running on"))
                        {
                            Kill(RCON_Port, JMX_Port);
                        }
                        else if (e.Data.Contains("RCON running on"))
                        {
                            while (MainWindow.serverRunning && viewModel != null)
                            {
                                await ServerStats.GetServerInfo(viewModel, serverPath, worldNumber, ipAddress, JMX_Port, RCON_Port, Server_Port, user, psw);
                            }
                        }
                    }
                };

                //     ↓ For error output traking ↓
                process.ErrorDataReceived += async (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"e: {e.Data}"); // For debugging

                        if (Auto_Stop && (e.Data.Contains("You need to agree to the EULA") || e.Data.Contains("Done")))
                        {
                            Kill(RCON_Port, JMX_Port);
                        }
                        else if (Auto_Stop == true && e.Data.Contains("RCON running on"))
                        {
                            Kill(RCON_Port, JMX_Port);
                        }
                        else if (e.Data.Contains("RCON running on"))
                        {
                            while (MainWindow.serverRunning == true && viewModel != null)
                            {
                                await ServerStats.GetServerInfo(viewModel, serverPath, worldNumber, ipAddress, JMX_Port, RCON_Port, Server_Port, user, psw);
                            }
                        }
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                CodeLogger.ConsoleLog($"Process for the server has stopped with exit code: {process.ExitCode}");
            }
        }

        public static async Task Stop(string operation, string worldNumber, string ipAddress, int RCON_Port, int JMX_Port, string time = "00:00")
        {
            if (string.IsNullOrEmpty(worldNumber))
            {
                CodeLogger.ConsoleLog("No worldNumber was supplied!");
                return;
            }

            await Countdown(time, operation, worldNumber, RCON_Port, ipAddress);

            await InputForServer("save-all flush", worldNumber, RCON_Port, ipAddress);
            await InputForServer("stop", worldNumber, RCON_Port, ipAddress);

            Thread.Sleep(1000);

            dbChanger.SpecificDataFunc($"UPDATE worlds SET Process_ID = NULL WHERE worldNumber = \"{worldNumber}\";");
            dbChanger.SpecificDataFunc($"UPDATE worlds SET serverUser = NULL WHERE worldNumber = \"{worldNumber}\";");
            dbChanger.SpecificDataFunc($"UPDATE worlds SET serverTempPsw = NULL WHERE worldNumber = \"{worldNumber}\";");
        }

        public static void Kill(int RCON_Port, int JMX_Port)
        {
            ClosePort(RCON_Port);
            ClosePort(JMX_Port);
        }

        public static async Task InputForServer(string input, string worldNumber, int RCON_Port, string serverIp)
        {
            // Replace with your server details
            ushort port = (ushort)RCON_Port; // RCON port
            string password = "";
            List<object[]> data = dbChanger.GetFunc(worldNumber, true);

            foreach (var row in data)
            {
                password = (string)row[10]; // Your RCON password
            }

            try
            {
                // Parse the IP address
                var serverAddress = IPAddress.Parse(serverIp);

                // Create an RCON client
                using (var rcon = new RCON(serverAddress, port, password))
                {
                    CodeLogger.ConsoleLog("Connecting to the server...");
                    await rcon.ConnectAsync();

                    CodeLogger.ConsoleLog("Connected. Sending command...");
                    // Send a command
                    string response = await rcon.SendCommandAsync(input);
                    // Output the server response
                    CodeLogger.ConsoleLog($"Server response: {response}");
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"An error occurred: {ex.Message}");
            }
        }

        public static async Task ChangeVersion(string worldNumber, string worldPath, string tempFolderPath, string defaultServerPropertiesPath, string serverVersionsPath, string rootFolder, string version, string worldName, string toSoftware, int totalPlayers, object[,] worldSettings, int processMemoryAllocation, string ipAddress, int serverPort, int jmxPort, int rconPort, int RMI_Port, bool keepWorldOnVersionChange = false)
        {
            if (string.IsNullOrEmpty(worldNumber))
            {
                CodeLogger.ConsoleLog("No world number was supplied!");
                return;
            }

            string rootWorldsFolder = Path.Combine(rootFolder, "worlds");
            string rootWorldFilesFolder = Path.Combine(rootWorldsFolder, worldNumber);

            string[,] worldFilesPaths = {
                { "world\\region", "world\\region" },
                { "world\\DIM-1", "world_nether\\DIM-1" },
                { "world\\DIM1",  "world_the_end\\DIM1" }
            };

            if (!keepWorldOnVersionChange)
            {
                await UpdateWorld(worldPath, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath, version, worldName, toSoftware, totalPlayers, worldSettings, processMemoryAllocation, ipAddress, serverPort, jmxPort, rconPort, RMI_Port, worldNumber, true);
                return;
            }

            string? currentSoftware = dbChanger.SpecificDataFunc($"SELECT software FROM worlds WHERE worldNumber = '{worldNumber}';")[0][0]?.ToString();
            bool isCurrentPaperOrPurpur = currentSoftware == "Purpur" || currentSoftware == "Paper";
            bool isNewPaperOrPurpur = toSoftware == "Purpur" || toSoftware == "Paper";

            CodeLogger.ConsoleLog("Storing world...");
            BackupWorldFiles(worldFilesPaths, tempFolderPath, rootWorldFilesFolder, isCurrentPaperOrPurpur);
            CodeLogger.ConsoleLog("World saved...");

            DeleteFiles(worldPath, false);
            CodeLogger.ConsoleLog("Database updated.");
            dbChanger.SpecificDataFunc($"UPDATE worlds SET name = \"{worldName}\", version = \"{version}\", software = \"{toSoftware}\", totalPlayers = \"{totalPlayers}\", Server_Port = \"{serverPort}\", JMX_Port = \"{jmxPort}\", RCON_Port = \"{rconPort}\", RMI_Port = \"{RMI_Port}\" WHERE worldNumber = \"{worldNumber}\";");

            CodeLogger.ConsoleLog("Creating New Server...");
            await ServerCreator.CreateServerFunc(rootFolder, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath, version, worldName, toSoftware, totalPlayers, worldSettings, processMemoryAllocation, ipAddress, serverPort, jmxPort, rconPort, RMI_Port, worldNumber, Insert_Into_DB: false);

            CodeLogger.ConsoleLog("Restoring old world...");
            RestoreWorldFiles(worldFilesPaths, tempFolderPath, rootWorldFilesFolder, isNewPaperOrPurpur);

            CodeLogger.ConsoleLog("Deleting old stored world files...");
            DeleteFiles(tempFolderPath, false);
            CodeLogger.ConsoleLog("Version changed successfully!");
        }

        public static void DeleteServer(string worldNumber, string serverDirectoryPath, bool deleteFromDB = true, bool deleteWholeDirectory = true)
        {
            if (deleteFromDB) dbChanger.DeleteWorldFromDB(worldNumber);

            DeleteFiles(serverDirectoryPath, deleteWholeDirectory);
            CodeLogger.ConsoleLog("Done!");
        }

        // -------------------------------- Help Functions --------------------------------

        public static bool CheckFilesAndNetworkSettings(int Server_Port = 25565, int JMX_Port = 25562, int RMI_Port = 25563)
        {
            int[] ports = [Server_Port, JMX_Port, RMI_Port];

            foreach (int port in ports)
            {
                if (!CheckFirewallRuleExists($"MinecraftServer_TCP_{port}") &&
                    !CheckFirewallRuleExists($"MinecraftServer_UDP_{port}"))
                {
                    CodeLogger.ConsoleLog("Network settings are not correct!");
                    return false;
                }
            }

            if (!JMX_Setter.EnsureJMXPasswordFile())
            {
                return false;
            }

            return true;
        }

        public static string LogError(string message)
        {
            CodeLogger.ConsoleLog(message);
            return message;
        }

        private static void BackupWorldFiles(string[,] worldFilesPaths, string tempFolderPath, string rootWorldFilesFolder, bool useSecondColumn)
        {
            for (int i = 0; i < worldFilesPaths.GetLength(0); i++)
            {
                try
                {
                    CopyFiles(worldFilesPaths[i, useSecondColumn ? 1 : 0], tempFolderPath, rootWorldFilesFolder);
                    CodeLogger.ConsoleLog($"Backup of '{worldFilesPaths[i, useSecondColumn ? 1 : 0]}' completed.");
                }
                catch (Exception ex)
                {
                    CodeLogger.ConsoleLog($"Error: {ex.Message}");
                }
            }
        }

        private static void RestoreWorldFiles(string[,] worldFilesPaths, string tempFolderPath, string rootWorldFilesFolder, bool useSecondColumn)
        {
            for (int i = 0; i < worldFilesPaths.GetLength(0); i++)
            {
                try
                {
                    string fileToChange = Path.Combine(rootWorldFilesFolder, worldFilesPaths[i, useSecondColumn ? 1 : 0]);
                    DeleteFiles(fileToChange, true);

                    if (!Directory.Exists(fileToChange))
                    {
                        Directory.CreateDirectory(fileToChange);
                    }

                    CopyFolderToDirectory(Path.Combine(tempFolderPath, worldFilesPaths[i, useSecondColumn ? 0 : 1]), fileToChange);
                }
                catch (Exception ex)
                {
                    CodeLogger.ConsoleLog($"Error: {ex.Message}");
                }
            }
        }

        private static async Task UpdateWorld(string worldPath, string rootWorldsFolder, string tempFolderPath, string defaultServerPropertiesPath, string version, string worldName, string toSoftware, int totalPlayers, object[,] worldSettings, int processMemoryAllocation, string ipAddress, int ServerPort, int jmxPort, int rconPort, int RMI_Port, string worldNumber, bool insertIntoDB)
        {
            DeleteFiles(worldPath, false);
            dbChanger.SpecificDataFunc($"UPDATE worlds SET name = \"{worldName}\", version = \"{version}\", software = \"{toSoftware}\", totalPlayers = \"{totalPlayers}\", Server_Port = \"{ServerPort}\", JMX_Port = \"{jmxPort}\", RCON_Port = \"{rconPort}\", RMI_Port = \"{RMI_Port}\" WHERE worldNumber = \"{worldNumber}\";");

            CodeLogger.ConsoleLog("Creating New Server...");
            await ServerCreator.CreateServerFunc(rootWorldsFolder, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath, version, worldName, toSoftware, totalPlayers, worldSettings, processMemoryAllocation, ipAddress, ServerPort, jmxPort, rconPort, RMI_Port, worldNumber, insertIntoDB, false);
        }

        private static async Task Countdown(string time, string action, string worldNumber, int RCON_Port, string serverIp)
        {
            int totalSeconds = ConvertToSeconds(time);

            if (totalSeconds <= 0 || totalSeconds >= 5400)
                return;

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            string message = $"say Server will {action} in {(minutes > 0 ? $"{minutes}m" : "")}{(seconds > 0 ? $" {seconds}s" : "")}!";
            await InputForServer(message.Trim(), worldNumber, RCON_Port, serverIp);

            Stopwatch stopwatch = new();

            await Task.Delay(1000);

            for (int i = totalSeconds - 1; i > 0; i--)
            {
                stopwatch.Restart();

                if (i % 600 == 0 && i > 600) // Every 10 minutes until 10 min left
                    await InputForServer($"say {i / 60} minutes remaining!", worldNumber, RCON_Port, serverIp);
                else if (i == 600)
                    await InputForServer("say 10 minutes remaining!", worldNumber, RCON_Port, serverIp);
                else if (i == 300)
                    await InputForServer("say 5 minutes remaining!", worldNumber, RCON_Port, serverIp);
                else if (i == 60)
                    await InputForServer("say 1 minute remaining!", worldNumber, RCON_Port, serverIp);
                else if (i == 30)
                    await InputForServer("say 30 seconds remaining!", worldNumber, RCON_Port, serverIp);
                else if (i <= 10)
                    await InputForServer($"say {i}", worldNumber, RCON_Port, serverIp);

                // Ensure accurate timing
                int waitTime = 1000 - (int)stopwatch.ElapsedMilliseconds;
                if (waitTime > 0)
                    await Task.Delay(waitTime);
            }

            await InputForServer($"say Server is now {action}!", worldNumber, RCON_Port, serverIp);
        }

        private static void CopyFolderToDirectory(string sourceFolder, string destinationFolder)
        {
            if (!Directory.Exists(sourceFolder))
            {
                CodeLogger.ConsoleLog($"Folder '{sourceFolder}' not found.");
                return;
            }

            CopyAll(new DirectoryInfo(sourceFolder), new DirectoryInfo(destinationFolder));
            CodeLogger.ConsoleLog($"Folder '{sourceFolder}' copied to '{destinationFolder}'.");
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName); // Ensure target directory exists

            // Copy each file in the folder
            foreach (FileInfo file in source.GetFiles())
            {
                string destFilePath = Path.Combine(target.FullName, file.Name);
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
                    CodeLogger.ConsoleLog($"Deleted entire directory: {path}");
                }
                else
                {
                    // Delete all files in the directory
                    foreach (string file in Directory.GetFiles(path))
                    {
                        File.Delete(file);
                        CodeLogger.ConsoleLog($"Deleted file: {file}");
                    }

                    // Delete all subdirectories and their contents
                    foreach (string directory in Directory.GetDirectories(path))
                    {
                        Directory.Delete(directory, true);
                        CodeLogger.ConsoleLog($"Deleted directory: {directory}");
                    }

                    CodeLogger.ConsoleLog($"All contents deleted successfully inside the folder: \"{path}\".");
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"An error occurred: {ex.Message}");
            }
        }

        private static int ConvertToSeconds(string time)
        {
            string[] parts = time.Split(':');
            if (parts.Length != 2) throw new ArgumentException("Invalid time format. Use MM:SS.");

            int minutes = int.Parse(parts[0]);
            int seconds = int.Parse(parts[1]);

            return (minutes * 60) + seconds;
        }

        public static bool IsPortInUse(int port)
        {
            bool isAvailable = true;

            try
            {
                var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                listener.Stop();
            }
            catch (Exception)
            {
                isAvailable = false;
            }

            return !isAvailable;
        }

        public static string? FindClosestJarFile(string folderPath, string targetPattern, bool onlyFileName = false)
        {
            try
            {
                // Ensure the folder exists
                if (!Directory.Exists(folderPath))
                {
                    CodeLogger.ConsoleLog("The specified folder does not exist.");
                    return null;
                }

                // Get all .jar files in the folder
                string[] jarFiles = Directory.GetFiles(folderPath, "*.jar");

                // Search for the closest match
                string? bestMatch = null;
                int bestScore = int.MinValue;

                foreach (string file in jarFiles)
                {
                    string fileName = Path.GetFileName(file); // Extract only the file name

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
                if (string.IsNullOrEmpty(bestMatch))
                {
                    return "";
                }

                if (onlyFileName)
                {
                    return bestMatch;
                }
                return $"-jar \"{folderPath}\\{bestMatch}\"";
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"An error occurred: {ex.Message}");
                return null;
            }
        }

        private static int CalculateMatchScore(string fileName, string targetPattern)
        {
            if (fileName.Equals(targetPattern, StringComparison.OrdinalIgnoreCase))
            {
                return int.MaxValue; // Exact match is best
            }

            int index = fileName.IndexOf(targetPattern, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
            {
                return 0; // No match
            }

            // Higher score for earlier occurrences + length of matching substring
            return 1000 - index + targetPattern.Length;
        }

        public static bool ClosePort(int portInt)
        {
            string port = portInt.ToString();
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

                            CodeLogger.ConsoleLog($"Found PID: {pid}");

                            string killCommand = $"taskkill /PID {pid} /F";
                            string killOutput = ExecuteCommand(killCommand);

                            if (!string.IsNullOrWhiteSpace(killOutput) && !killOutput.Contains("Error"))
                            {
                                CodeLogger.ConsoleLog($"Successfully terminated process with PID {pid}.");
                                return true;
                            }
                            else
                            {
                                CodeLogger.ConsoleLog($"Failed to terminate process with PID {pid}.");
                            }
                        }
                    }
                }
                else
                {
                    CodeLogger.ConsoleLog($"No process found on port {port}.");
                }

                return false;
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Exception: {ex.Message}");
                return false;
            }
        }

        private static string ExecuteCommand(string command)
        {
            try
            {
                ProcessStartInfo processInfo = new("cmd.exe", "/C " + command)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process? process = Process.Start(processInfo) ?? throw new Exception("Failed to start the process.");
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
                CodeLogger.ConsoleLog($"Access denied to folder: {folderPath}. Error: {ex.Message}");
                return "";
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"An error occurred: {ex.Message}");
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
                    CodeLogger.ConsoleLog($"Folder '{folderName}' was NOT found inside '{rootDir}'.");
                    return;
                }

                string sourcePath = directories[0]; // Take the first found match
                string destinationPath = Path.Combine(destinationDir, folderName);

                Directory.CreateDirectory(destinationPath); // Ensure destination exists

                foreach (string file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        string relativePath = file.Substring(sourcePath.Length + 1); // Get relative path
                        string destFile = Path.Combine(destinationPath, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destFile) ?? throw new Exception("Failed to get the destination file.")); // Ensure subdirectory exists
                        File.Copy(file, destFile, true); // Copy file
                    }
                    catch (FileNotFoundException ex)
                    {
                        CodeLogger.ConsoleLog($"File not found: {ex.FileName}");
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        CodeLogger.ConsoleLog($"Directory not found: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        CodeLogger.ConsoleLog($"An error occurred: {ex.Message}");
                    }
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                CodeLogger.ConsoleLog($"Directory not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"An error occurred: {ex.Message}");
            }
        }

        public static bool CheckFirewallRuleExists(string portName)
        {
            try
            {
                // Create a process to run the netsh command
                Process process = new();
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

                if (output.Contains(portName))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog("Error: " + ex.Message);
            }

            return false;
        }

        private static void RecordServerStart()
        {
            // Record the server start in the database
            DateTime startupTime = DateTime.Now;

            if (StartupTimePath != null)
            {
                // Write the timestamp to the file
                File.WriteAllText(StartupTimePath, startupTime.ToString("o")); // "o" for ISO 8601 format
                CodeLogger.ConsoleLog($"Server start time recorded: {startupTime}");
            }
            else
            {
                CodeLogger.ConsoleLog("StartupTimePath is null. Cannot record server start time.");
            }
        }
    }
}