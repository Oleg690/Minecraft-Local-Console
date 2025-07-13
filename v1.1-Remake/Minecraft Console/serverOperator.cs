using CoreRCON;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;

namespace Minecraft_Console
{
    [SupportedOSPlatform("windows")]
    class ServerCreator
    {
        public static readonly string? rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))) ?? string.Empty;
        private static readonly string? UpdaterLastCheck = Path.Combine(rootFolder, "lastQuiltorFabricCheckUpdater.txt");

        private static readonly TimeSpan DelayTime = TimeSpan.FromHours(72);

        public static async Task<string[]> CreateServerFunc(string rootFolder, string rootWorldsFolder, string tempFolderPath, string defaultServerPropertiesPath, string version, string worldName, string software, int totalPlayers, object[,] worldSettings, int ProcessMemoryAlocation, string ipAddress, int Server_Port, int JMX_Port, int RCON_Port, int RMI_Port, string? worldNumber = null, bool Server_Auto_Start = true, bool Insert_Into_DB = true)
        {
            MainWindow.SetLoadingBarProgress(5);

            if (string.IsNullOrEmpty(software)) return ServerOperator.LogError("Software not selected!");
            if (string.IsNullOrEmpty(worldName)) worldName = $"{software} Server";

            if (!VersionsUpdater.CheckVersionExists(software, version) && software != "Fabric" && software != "Quilt")
                return ServerOperator.LogError($"{software} {version} is not supported!");

            // 🔧 Run version check and early abort if the file doesn't exist
            string softwarePath = Path.Combine(rootFolder, "versions", software);
            bool versionStatus = await CheckVersions(rootFolder, software, version);
            if (!versionStatus)
                return ServerOperator.LogError($"Encountered error while downloading version {version} for {software}!");

            // double check the file exists now
            string jarFileName = GetJarFilePath(rootFolder, software, version);
            string jarFilePath = Path.Combine(softwarePath, jarFileName);
            if (string.IsNullOrEmpty(jarFileName) || !File.Exists(jarFilePath))
                return ServerOperator.LogError($"Server .jar file for {software} {version} is missing after download attempt!");

            // 🔌 Check ports and networking
            if (!ServerOperator.CheckFilesAndNetworkSettings(Server_Port, JMX_Port, RMI_Port))
                await NetworkSetup.Setup(Server_Port, JMX_Port, RMI_Port);

            // 📁 Create unique directory for world
            string uniqueNumber = worldNumber ?? GenerateUniqueRandomNumber(12, rootWorldsFolder);
            string customDirectory = Path.Combine(rootWorldsFolder, uniqueNumber);
            Directory.CreateDirectory(customDirectory);
            CodeLogger.ConsoleLog($"Created server directory: {customDirectory}");

            // 📤 Copy version file to world folder
            string destinationJarPath = Path.Combine(customDirectory, Path.GetFileName(jarFilePath));
            File.Copy(jarFilePath, destinationJarPath);
            string jarPath = Path.Combine(Path.GetDirectoryName(destinationJarPath) ?? throw new Exception("Invalid path."), version + ".jar");
            RenameFile(destinationJarPath, jarPath);
            CodeLogger.ConsoleLog("Server .jar file copied to the custom directory.");

            // 🔐 Generate RCON credentials
            string rconPassword = worldNumber != null ? GetRconPassword(worldNumber) : GeneratePassword(20);

            object[,] rconSettings =
            {
                { "enable-rcon", "true" },
                { "rcon.password", $"{rconPassword}" },
                { "rcon.port", $"{RCON_Port}" },
                { "enable-query", "true" },
            };

            CodeLogger.ConsoleLog($"Setting world settings...");

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

            CodeLogger.ConsoleLog($"Creating {software} Server...");

            // 🧩 Handle installer-based software
            var requiresInstallation = new Dictionary<string, string>
            {
                { "Forge", $"-jar \"{jarPath}\" --installServer" },
                { "Fabric", $"-jar \"{jarPath}\" server -mcversion {version} -downloadMinecraft" },
                { "Quilt", $"-jar \"{jarPath}\" install server {version} --download-server" }
            };

            object[] verificator = [];

            if (requiresInstallation.TryGetValue(software, out var arguments))
            {
                processInfo.Arguments = arguments;
                verificator = BasicServerInstaller(processInfo, software);

                if (software == "Quilt")
                {
                    CopyFiles(Path.Combine(tempFolderPath, "server"), customDirectory);
                    ServerOperator.DeleteFiles(tempFolderPath, false);
                    File.Delete(jarPath);
                    RenameFile(Path.Combine(customDirectory, "server.jar"), jarPath);
                }
                else if (software == "Fabric")
                {
                    File.Delete(jarPath);
                    RenameFile(Path.Combine(customDirectory, "server.jar"), jarPath);
                }
            }

            if (verificator.Length != 0)
            {
                if (!Convert.ToBoolean(verificator[0]))
                {
                    CodeLogger.ConsoleLog($"{verificator[1]}");
                    return ["Error", $"{verificator[1]}", $"{uniqueNumber}"];
                }
            }

            // 🔁 Final server init
            verificator = await BasicServerInitializator(
                software, customDirectory, jarPath,
                rconSettings, worldSettings, ProcessMemoryAlocation,
                uniqueNumber, worldName, version, totalPlayers,
                rconPassword, ipAddress, Server_Port, JMX_Port, RCON_Port, RMI_Port,
                Insert_Into_DB
            );

            if (verificator.Length != 0)
            {
                if (!Convert.ToBoolean(verificator[0]))
                {
                    CodeLogger.ConsoleLog($"{verificator[1]}");
                    return ["Error", $"{verificator[1]}", $"{uniqueNumber}"];
                }
            }

            //CodeLogger.ConsoleLog($"jarPath: {jarPath}");

            // 🧹 Clean up temp values
            DbChanger.SpecificDataFunc($"UPDATE worlds SET Process_ID = NULL, serverUser = NULL, serverTempPsw = NULL, startingStatus = NULL WHERE worldNumber = \"{uniqueNumber}\";");

            //CodeLogger.ConsoleLog("World Created Successfully");
            MainWindow.SetLoadingBarProgress(100);

            return ["Success", "World Created Successfully", $"{uniqueNumber}"];
        }

        // -------------------------------- Help Functions --------------------------------

        private static object[] BasicServerInstaller(ProcessStartInfo processInfo, string software)
        {
            try
            {
                using Process process = new() { StartInfo = processInfo };

                bool installSuccess = false;
                string lastOutput = string.Empty;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        lastOutput = e.Data;
                        Console.WriteLine(e.Data);

                        // Optionally look for success indicators in the output
                        if (e.Data.Contains("Done") || e.Data.Contains("Starting server"))
                        {
                            installSuccess = true;
                        }
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        CodeLogger.ConsoleLog($"[Error] {e.Data}");
                    }
                };

                CodeLogger.ConsoleLog($"Minecraft {software} server installation is starting...");

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                // Fallback to checking exit code
                if (process.ExitCode == 0)
                    installSuccess = true;

                string resultMessage = installSuccess ? "Installation completed successfully!" : "Installation finished, but may have failed.";

                CodeLogger.ConsoleLog(resultMessage);
                MainWindow.loadingScreenProcentage = 45;

                return [installSuccess, resultMessage];
            }
            catch (Exception ex)
            {
                string error = $"[Exception] {ex.Message}";
                CodeLogger.ConsoleLog(error);
                return [false, error];
            }
        }


        private static async Task<object[]> BasicServerInitializator(string software, string customDirectory, string JarPath, object[,] rconSettings, object[,] worldSettings, int ProcessMemoryAlocation, string uniqueNumber, string worldName, string version, int totalPlayers, string rconPassword, string ipAddress, int Server_Port, int JMX_Port, int RCON_Port, int RMI_Port, bool Insert_Into_DB = false)
        {
            try
            {
                if (Insert_Into_DB)
                {
                    DbChanger.SetFunc($"{uniqueNumber}", $"{worldName}", $"{software}", $"{version}", $"{totalPlayers}", $"{Server_Port}", $"{JMX_Port}", $"{RCON_Port}", $"{RMI_Port}", $"{rconPassword}");
                }

                AcceptEULA(JarPath);

                MainWindow.SetLoadingBarProgress(50);

                object[] firstStartExitCode = await ServerOperator.Start(uniqueNumber, customDirectory, ProcessMemoryAlocation, ipAddress, Server_Port, JMX_Port, RCON_Port, RMI_Port, Auto_Stop: true);
                if (Convert.ToInt32(firstStartExitCode[0]) != 0)
                {
                    CodeLogger.ConsoleLog($"Server failed to start the first time. Exit code: {firstStartExitCode[0]}");
                    return [false, firstStartExitCode[1]];
                }

                MainWindow.SetLoadingBarProgress(75);

                //File.Delete(JarPath);

                MainWindow.SetLoadingBarProgress(99);

                return [true, "Success"]; // Success
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error: {ex.Message}");
                return [false, ex.Message]; // Failure
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
                CodeLogger.ConsoleLog("EULA accepted.");
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
                List<object[]> worldExistingNumbers = DbChanger.SpecificDataFunc("SELECT worldNumber FROM worlds;");

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

        private static async Task<bool> CheckVersions(string rootFolder, string software, string version)
        {
            try
            {
                string serverVersionsPath = Path.Combine(rootFolder, "versions");
                string softwarePath = Path.Combine(serverVersionsPath, software);

                // Ensure the software directory exists
                if (!Directory.Exists(softwarePath))
                {
                    Directory.CreateDirectory(softwarePath);
                }

                // Verify version validity (except for Fabric or Quilt which are installer based)
                if (!VersionsUpdater.CheckVersionExists(software, version) && software != "Fabric" && software != "Quilt")
                {
                    CodeLogger.ConsoleLog($"Version '{version}' is not supported for software '{software}'. Aborting.");
                    return false;
                }

                var localFiles = Directory.GetFiles(softwarePath, "*.jar");
                bool exists = localFiles.Any(file =>
                {
                    string fileName = Path.GetFileName(file);
                    string fileVersion = fileName.Split('-')[1].Replace(".jar", "");

                    return fileVersion == version;
                });

                if (software == "Fabric" || software == "Quilt")
                {
                    if (ShouldRunNow() || localFiles.Length == 0)
                    {
                        CodeLogger.ConsoleLog("Checking for updates (installer-based software)...");

                        bool updated = await VersionsUpdater.Update(serverVersionsPath, software);
                        if (!updated)
                        {
                            CodeLogger.ConsoleLog($"[ERROR] Failed to update installer for '{software}'.");
                            return false;
                        }

                        if (string.IsNullOrEmpty(UpdaterLastCheck))
                        {
                            throw new Exception("UpdaterLastCheck file path is not defined.");
                        }
                        File.WriteAllText(UpdaterLastCheck, DateTime.UtcNow.ToString("o"));

                        localFiles = Directory.GetFiles(softwarePath, "*.jar");
                        if (localFiles.Length == 0)
                        {
                            CodeLogger.ConsoleLog($"[ERROR] Installer for '{software}' could not be downloaded.");
                            return false;
                        }
                    }
                }
                else
                {
                    if (!exists)
                    {
                        CodeLogger.ConsoleLog($"Version '{version}' not found locally. Attempting to download...");

                        bool updated = await VersionsUpdater.Update(serverVersionsPath, software, version);
                        if (!updated)
                        {
                            CodeLogger.ConsoleLog($"[ERROR] Failed to download '{software}' version '{version}'. Aborting.");
                            return false;
                        }

                        localFiles = Directory.GetFiles(softwarePath, "*.jar");
                        exists = localFiles.Any(file => file.Contains(version));

                        if (!exists)
                        {
                            CodeLogger.ConsoleLog($"[ERROR] Downloaded '{software}' version '{version}' not found after update.");
                            return false;
                        }
                    }
                }

                CodeLogger.ConsoleLog($"'{software}' version '{version}' is ready.");
                return true;
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"[EXCEPTION] {ex.Message}");
                return false;
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
            List<object[]> data = DbChanger.GetFunc(worldNumber, true);
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
        public static async Task<object[]> Start(
            string worldNumber, string serverPath, int processMemoryAlocation,
            string ipAddress, int Server_Port, int JMX_Port, int RCON_Port, int RMI_Port,
            bool Auto_Stop = false, bool noGUI = true,
            ViewModel? viewModel = null, Action<string>? onServerRunning = null)
        {
            DbChanger.SpecificDataFunc($"UPDATE worlds SET startingStatus = 'starting' WHERE worldNumber = '{worldNumber}';");

            bool verificator = true;

            void ValidateInput(string input, string errorMessage)
            {
                if (string.IsNullOrEmpty(input))
                {
                    CodeLogger.ConsoleLog(errorMessage);
                    verificator = false;
                }
            }

            void ValidateDirectory(string path, string errorMessage)
            {
                if (!Directory.Exists(path))
                {
                    CodeLogger.ConsoleLog(errorMessage);
                    verificator = false;
                }
            }

            ValidateInput(worldNumber, "No worldNumber was supplied!");
            ValidateDirectory(serverPath, "No local server files were found!");

            string? software = DbChanger.SpecificDataFunc($"SELECT software FROM worlds where worldNumber = '{worldNumber}';")[0][0]?.ToString() ?? string.Empty;
            ValidateInput(software, "No software was supplied!");
            if (!verificator) return [-1, "Validation failed."];

            while (IsPortInUse(RCON_Port) || IsPortInUse(JMX_Port))
            {
                CodeLogger.ConsoleLog("Port not closed!");
                Thread.Sleep(1000);
            }

            if (!CheckFilesAndNetworkSettings(Server_Port, JMX_Port, RMI_Port))
            {
                await NetworkSetup.Setup(Server_Port, JMX_Port, RMI_Port);
            }

            string user = "admin";
            string psw = ServerCreator.GenerateRandomNumber(5);

            void WriteToFile(string path, string content, string fileType)
            {
                if (!string.IsNullOrEmpty(path))
                    File.WriteAllText(path, content);
                else
                    CodeLogger.ConsoleLog($"{fileType} path is null. Cannot write to file.");
            }

            if (File.Exists(JMX_Password_File_Path) && File.Exists(JMX_Access_File_Path))
            {
                WriteToFile(JMX_Access_File_Path, $"{user} readwrite", "JMX Access File");
                WriteToFile(JMX_Password_File_Path, $"{user} {psw}", "JMX Password File");
            }

            string serverSettings = $"-Xmx{processMemoryAlocation}M -Xms{processMemoryAlocation}M " +
                                    $"-Dcom.sun.management.jmxremote " +
                                    $"-Dcom.sun.management.jmxremote.port={JMX_Port} " +
                                    $"-Dcom.sun.management.jmxremote.rmi.port={RMI_Port} " +
                                    $"-Dcom.sun.management.jmxremote.ssl=false " +
                                    $"-Dcom.sun.management.jmxremote.authenticate=true " +
                                    $"-Dcom.sun.management.jmxremote.access.file=\"{JMX_Access_File_Path}\" " +
                                    $"-Dcom.sun.management.jmxremote.password.file=\"{JMX_Password_File_Path}\" " +
                                    $"-Djava.rmi.server.hostname={ipAddress} ";

            ProcessStartInfo serverProcessInfo = new()
            {
                FileName = "java",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = serverPath
            };

            object[] serverData = DbChanger.SpecificDataFunc($"SELECT version, totalPlayers FROM worlds WHERE worldNumber = \"{worldNumber}\";")[0];
            string? toRunJarFile = string.Empty;
            string GetVersionedJarFile() => Path.Combine(serverPath, serverData[0].ToString() + ".jar");

            void SetServerArguments(string arguments)
            {
                serverProcessInfo.Arguments = arguments;
                CodeLogger.ConsoleLog($"Full arguments: {arguments}");
            }

            switch (software)
            {
                case "Vanilla":
                case "Fabric":
                case "NeoForge":
                case "Purpur":
                case "Paper":
                case "Quilt":
                    toRunJarFile = GetVersionedJarFile();
                    if (string.IsNullOrEmpty(toRunJarFile) || !File.Exists(toRunJarFile))
                        return [-1, "Versioned JAR file not found."];
                    SetServerArguments($"{serverSettings} -jar \"{toRunJarFile}\"");
                    break;

                case "Forge":
                    string winArgsPath = FindFileInFolder(Path.Combine(serverPath, "libraries"), "win_args.txt");
                    if (!string.IsNullOrEmpty(winArgsPath)) winArgsPath = $"@\"{winArgsPath}\"";

                    toRunJarFile = FindClosestJarFile(serverPath, "forge-") ?? FindClosestJarFile(serverPath, "minecraft_server");
                    if (string.IsNullOrEmpty(toRunJarFile) || !File.Exists(ExtractPathFromQuotedJar(toRunJarFile)))
                        return [-1, "No Forge server JAR file found."];

                    SetServerArguments($"{serverSettings} {winArgsPath} {toRunJarFile}");
                    break;

                default:
                    return [-1, "Invalid software type."];
            }

            serverProcessInfo.Arguments += noGUI ? " nogui" : "";

            using Process? process = Process.Start(serverProcessInfo);
            if (process == null)
                return [-1, "Failed to start process."];

            process.EnableRaisingEvents = true;
            DbChanger.SpecificDataFunc($"UPDATE worlds SET Process_ID = \"{process.Id}\", serverUser = \"{user}\", serverTempPsw = \"{psw}\" WHERE worldNumber = \"{worldNumber}\";");
            RecordServerStart();

            bool serverStarted = false;
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    CodeLogger.ConsoleLog(e.Data);

                    if (e.Data.Contains("RCON running on"))
                    {
                        serverStarted = true;
                        onServerRunning?.Invoke(worldNumber);
                        cts.Cancel(); // cancel timeout
                    }

                    if (Auto_Stop && (e.Data.Contains("EULA") || e.Data.Contains("Done")))
                        Kill(RCON_Port, JMX_Port);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    CodeLogger.ConsoleLog("stderr: " + e.Data);

                    if (e.Data.Contains("RCON running on"))
                    {
                        serverStarted = true;
                        onServerRunning?.Invoke(worldNumber);
                        cts.Cancel(); // cancel timeout
                    }

                    if (Auto_Stop && (e.Data.Contains("EULA") || e.Data.Contains("Done")))
                        Kill(RCON_Port, JMX_Port);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
            }
            catch (TaskCanceledException)
            {
                if (!serverStarted)
                {
                    CodeLogger.ConsoleLog("Timeout reached. Server didn't start.");
                    try { process.Kill(true); } catch { }
                    return [-1, "Server failed to start within 30 seconds."];
                }
            }

            await process.WaitForExitAsync();

            string message = process.ExitCode != 0
                ? $"Server exited with code {process.ExitCode}"
                : "Server stopped successfully.";

            return [0, message];
        }


        public static async Task Stop(string operation, string worldNumber, string ipAddress, int RCON_Port, string time = "00:00")
        {
            if (string.IsNullOrEmpty(worldNumber))
            {
                CodeLogger.ConsoleLog("No worldNumber was supplied!");
                return;
            }

            MainWindow.serverRunning = false;

            await Countdown(time, operation, worldNumber, RCON_Port, ipAddress);

            await InputForServer("save-all flush", worldNumber, RCON_Port, ipAddress);
            await InputForServer("stop", worldNumber, RCON_Port, ipAddress);

            Thread.Sleep(1000);

            DbChanger.SpecificDataFunc($"UPDATE worlds SET serverUser = NULL, serverTempPsw = NULL, Process_ID = NULL WHERE worldNumber = \"{worldNumber}\";");
        }

        public static void Kill(int RCON_Port, int JMX_Port)
        {
            ClosePort(RCON_Port);
            ClosePort(JMX_Port);
        }

        public static async Task<string?> InputForServer(string input, string worldNumber, int RCON_Port, string serverIp)
        {
            try
            {
                List<object[]> data = DbChanger.GetFunc(worldNumber, true);
                if (data == null || data.Count == 0 || data[0].Length <= 10)
                {
                    return "No RCON password found for this world.";
                }

                string password = data[0][10]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(password))
                {
                    return "RCON password is empty.";
                }

                if (!IPAddress.TryParse(serverIp, out var serverAddress))
                {
                    return $"Invalid server IP address: {serverIp}";
                }

                using var rcon = new RCON(serverAddress, (ushort)RCON_Port, password);
                await rcon.ConnectAsync();

                string response = await rcon.SendCommandAsync(input);
                CodeLogger.ConsoleLog($"[Server Response] {response}");

                return null; // success
            }
            catch (Exception ex)
            {
                return $"An exception occurred: {ex.Message}";
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

            string? currentSoftware = DbChanger.SpecificDataFunc($"SELECT software FROM worlds WHERE worldNumber = '{worldNumber}';")[0][0]?.ToString();
            bool isCurrentPaperOrPurpur = currentSoftware == "Purpur" || currentSoftware == "Paper";
            bool isNewPaperOrPurpur = toSoftware == "Purpur" || toSoftware == "Paper";

            CodeLogger.ConsoleLog("Storing world...");
            BackupWorldFiles(worldFilesPaths, tempFolderPath, rootWorldFilesFolder, isCurrentPaperOrPurpur);
            CodeLogger.ConsoleLog("World saved...");

            DeleteFiles(worldPath, false);
            CodeLogger.ConsoleLog("Database updated.");
            DbChanger.SpecificDataFunc($"UPDATE worlds SET name = \"{worldName}\", version = \"{version}\", software = \"{toSoftware}\", totalPlayers = \"{totalPlayers}\", Server_Port = \"{serverPort}\", JMX_Port = \"{jmxPort}\", RCON_Port = \"{rconPort}\", RMI_Port = \"{RMI_Port}\" WHERE worldNumber = \"{worldNumber}\";");

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
            if (deleteFromDB) DbChanger.DeleteWorldFromDB(worldNumber);

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

        public static string[] LogError(string message)
        {
            CodeLogger.ConsoleLog(message);
            return ["Error", message];
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
            DbChanger.SpecificDataFunc($"UPDATE worlds SET name = \"{worldName}\", version = \"{version}\", software = \"{toSoftware}\", totalPlayers = \"{totalPlayers}\", Server_Port = \"{ServerPort}\", JMX_Port = \"{jmxPort}\", RCON_Port = \"{rconPort}\", RMI_Port = \"{RMI_Port}\" WHERE worldNumber = \"{worldNumber}\";");

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

            return minutes * 60 + seconds;
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

        public static string? FindClosestJarFile(string folderPath, string targetPattern, bool onlyFileName = false, bool fallbackToFirst = false)
        {
            try
            {
                // Validate inputs
                if (!Directory.Exists(folderPath))
                {
                    CodeLogger.ConsoleLog("The specified folder does not exist.");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(targetPattern))
                {
                    CodeLogger.ConsoleLog("Target pattern is null or empty.");
                    return null;
                }

                // Get all .jar files in the folder
                string[] jarFiles = Directory.GetFiles(folderPath, "*.jar");
                if (jarFiles.Length == 0)
                {
                    CodeLogger.ConsoleLog("No .jar files found in the folder.");
                    return null;
                }

                // Search for the closest match
                string? bestMatch = null;
                int bestScore = int.MinValue;

                foreach (string file in jarFiles)
                {
                    string fileName = Path.GetFileName(file);

                    if (fileName.Contains(targetPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        int score = CalculateMatchScore(fileName, targetPattern);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMatch = fileName;
                        }
                    }
                }

                // Optional fallback to first .jar file if no match found
                if (string.IsNullOrEmpty(bestMatch))
                {
                    if (fallbackToFirst)
                    {
                        bestMatch = Path.GetFileName(jarFiles[0]);
                        CodeLogger.ConsoleLog("No close match found. Falling back to first .jar file.");
                    }
                    else
                    {
                        return null;
                    }
                }

                if (onlyFileName)
                {
                    return bestMatch;
                }

                return $"-jar \"{Path.Combine(folderPath, bestMatch)}\"";
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
                return int.MaxValue; // Best possible match
            }

            int index = fileName.IndexOf(targetPattern, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
            {
                return 0;
            }

            int lengthMatch = targetPattern.Length;
            int proximityScore = Math.Max(0, 1000 - index); // Earlier match = higher score

            return proximityScore + lengthMatch * 10;
        }

        private static string? ExtractPathFromQuotedJar(string input)
        {
            int firstQuote = input.IndexOf('"');
            int lastQuote = input.LastIndexOf('"');

            if (firstQuote >= 0 && lastQuote > firstQuote)
            {
                return input.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
            }

            return null; // Return null if quotes not found properly
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
                    string[] lines = output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        string[] parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 4)
                        {
                            string pid = parts[^1];

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
            }
            else
            {
                CodeLogger.ConsoleLog("StartupTimePath is null. Cannot record server start time.");
            }
        }
    }
}