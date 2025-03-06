using System;
using System.Text.Json;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace Updater
{
    class VersionsUpdater
    {
        private static readonly string VanillaApiBaseUrl = "https://piston-meta.mojang.com/mc/game/version_manifest.json";
        private static readonly string ForgeApiVersionsUrl = "https://files.minecraftforge.net/net/minecraftforge/forge/";
        private static readonly string ForgeApiBaseUrl = "https://maven.minecraftforge.net/net/minecraftforge/forge/";
        private static readonly string NeoForgeVersionsUrl = "https://maven.neoforged.net/mojang-meta/net/neoforged/minecraft-dependencies/maven-metadata.xml";
        private static readonly string NeoForgeVersionInfoUrl = "https://maven.neoforged.net/mojang-meta/net/minecraft/";
        private static readonly string FabricApiBaseUrl = "https://meta.fabricmc.net/v2/versions/installer/";
        private static readonly string QuiltApiBaseUrl = "https://maven.quiltmc.org/repository/release/org/quiltmc/quilt-installer/";
        private static readonly string PurpurApiBaseUrl = "https://api.purpurmc.org/v2/purpur/";
        private static readonly string PaperApiBaseUrl = "https://api.papermc.io/v2/projects/paper";

        private static readonly HttpClient HttpClient = new();

        // ------------------------ ↓ Vanilla Updater Funcs ↓ ------------------------
        private static async Task CheckAndUpdateVanillaAsync(string downloadDirectory, string? selectedVersion = null)
        {
            Console.WriteLine($"Checking Vanilla Minecraft Server versions in: {downloadDirectory}");
            Directory.CreateDirectory(downloadDirectory);

            var availableVersions = await GetAvailableVanillaServerVersionsAsync(selectedVersion);
            if (availableVersions == null || availableVersions.Count == 0)
            {
                Console.WriteLine("No available server versions found.");
                return;
            }

            Console.WriteLine($"Found {availableVersions.Count} versions.");

            var localFiles = Directory.GetFiles(downloadDirectory, "vanilla-*.jar");
            var localVersions = localFiles.Select(f => Path.GetFileNameWithoutExtension(f).Replace("vanilla-", "")).ToHashSet();

            foreach (var version in availableVersions.Keys)
            {
                if (!localVersions.Contains(version))
                {
                    Console.WriteLine($"Vanilla Server {version} is missing. Downloading...");

                    var response = await HttpClient.GetStringAsync(availableVersions[version]);
                    using var jsonDoc = JsonDocument.Parse(response);

                    try
                    {
                        string? jarUrl = jsonDoc.RootElement.GetProperty("downloads")
                                                    .GetProperty("server")
                                                    .GetProperty("url")
                                                    .GetString();

                        string savePath = Path.Combine(downloadDirectory, $"vanilla-{version}.jar");
                        await DownloadServerJarAsync(jarUrl, savePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Version not found. Error: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Vanilla Server {version} is up to date.");
                }
            }

            Console.WriteLine("---------------------------------");
            Console.WriteLine("All Vanilla Server versions are updated.");
            Console.WriteLine("---------------------------------");
        }

        private static async Task<Dictionary<string, string>> GetAvailableVanillaServerVersionsAsync(string? selectedVersion = null)
        {
            try
            {
                Console.WriteLine("Fetching available Vanilla Minecraft Server versions...");
                var response = await HttpClient.GetStringAsync(VanillaApiBaseUrl);
                using var jsonDoc = JsonDocument.Parse(response);

                var availableVersions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray()
                    .Where(v => v.GetProperty("type").GetString() == "release")
                    .ToDictionary(
                        v => v.GetProperty("id").GetString(),
                        v => v.GetProperty("url").GetString()
                    );

                if (!string.IsNullOrEmpty(selectedVersion))
                {
                    availableVersions = availableVersions
                        .Where(v => v.Key == selectedVersion)
                        .ToDictionary(v => v.Key, v => v.Value);
                }

                foreach (var version in availableVersions)
                {
                    Console.WriteLine($"Detected server version: {version.Key}");
                }

                return availableVersions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching versions: {ex.Message}");
                return null;
            }
        }

        // ------------------------ ↓ Fabric Updater Funcs ↓ ------------------------

        private static async Task CheckAndUpdateLatestFabricAsync(string downloadDirectory)
        {
            try
            {
                Console.WriteLine("Fetching the latest Fabric Minecraft Server Installer...");
                Directory.CreateDirectory(downloadDirectory);

                // Get the latest stable Fabric Loader version
                var loaderResponse = await HttpClient.GetStringAsync(FabricApiBaseUrl);
                using var loaderJson = JsonDocument.Parse(loaderResponse);

                // Filter to get only the latest stable version
                var latestLoader = loaderJson.RootElement.EnumerateArray()
                    .Where(v => v.GetProperty("stable").GetBoolean() == true)
                    .ToDictionary(
                        v => v.GetProperty("version").GetString(),
                        v => v.GetProperty("url").GetString()
                    );

                if (latestLoader.TryGetValue == null)
                {
                    Console.WriteLine("Failed to retrieve the latest Fabric Loader version.");
                    return;
                }

                foreach (var loader in latestLoader)
                {
                    string localFilePath = Path.Combine(downloadDirectory, $"fabric-installer-{loader.Key}.jar");
                    if (File.Exists(localFilePath))
                    {
                        Console.WriteLine($"Latest Fabric Universal Server Installer {loader.Key} is already downloaded.");
                        return;
                    }
                    else
                    {
                        DeleteFiles(downloadDirectory, false);
                        Console.WriteLine($"Downloading Fabric Universal Server Installer {loader.Key}...");
                        await DownloadServerJarAsync(loader.Value, localFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching the latest Fabric version: {ex.Message}");
            }
        }

        // ------------------------ ↓ Forge Updater Funcs ↓ ------------------------

        private static async Task CheckAndUpdateForgeAsync(string downloadDirectory, string? selectedVersion = null)
        {
            try
            {
                // Fetch the promotions JSON
                string json = await HttpClient.GetStringAsync($"{ForgeApiVersionsUrl}promotions_slim.json");
                JObject promotions = JObject.Parse(json);

                // Get the list of stable versions
                JObject promos = (JObject)promotions["promos"];
                List<string> stableVersions = new List<string>();

                foreach (var promo in promos)
                {
                    if (promo.Key.EndsWith("-latest"))
                    {
                        string version = promo.Key.Replace("-latest", "");
                        stableVersions.Add(version);
                    }
                }

                if (selectedVersion != null)
                {
                    if (stableVersions.Contains(selectedVersion))
                    {
                        await ProcessVersion(downloadDirectory, promos, selectedVersion);
                    }
                    else
                    {
                        Console.WriteLine($"Version {selectedVersion} not found in the list of stable versions.");
                    }
                }
                else
                {
                    // If no specific version is provided, process all versions
                    foreach (string version in stableVersions)
                    {
                        await ProcessVersion(downloadDirectory, promos, version);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static async Task ProcessVersion(string downloadDirectory, JObject promos, string version)
        {
            string build = promos[$"{version}-latest"].ToString();
            string actualDownloadUrl = $"{ForgeApiBaseUrl}{version}-{build}/forge-{version}-{build}-installer.jar";

            string fileName = $"forge-{version}-{build}.jar";
            string destinationPath = Path.Combine(downloadDirectory, fileName);

            // Check if a file for this version already exists locally
            string[] existingFiles = Directory.GetFiles(downloadDirectory, $"forge-{version}-*.jar");
            if (existingFiles.Length > 0)
            {
                // Extract the build number from the existing file name
                string existingFileName = Path.GetFileNameWithoutExtension(existingFiles[0]);
                string[] existingFileParts = existingFileName.Split('-');
                if (existingFileParts.Length >= 3)
                {
                    string existingBuild = existingFileParts[2]; // Get the build number as a string

                    // Compare build numbers as strings
                    if (CompareForgeBuildNumbers(build, existingBuild)) // Check if the new build is greater
                    {
                        Console.WriteLine($"Newer build found for version {version}. Existing build: {existingBuild}, New build: {build}");

                        // Delete the old file
                        File.Delete(existingFiles[0]);
                        Console.WriteLine($"Deleted old file: {existingFileName}.jar");

                        // Download the new file
                        await DownloadServerJarAsync(actualDownloadUrl, destinationPath);
                    }
                    else
                    {
                        Console.WriteLine($"Skipping download for version {version}. Existing build ({existingBuild}) is up to date.");
                    }
                }
                else
                {
                    Console.WriteLine($"Unable to parse build number from existing file: {existingFileName}");
                }
            }
            else
            {
                // If no local file exists, download the new file
                Console.WriteLine($"No local file found for version {version}. Downloading new build: {build}");

                await DownloadServerJarAsync(actualDownloadUrl, destinationPath);
            }
        }

        private static bool CompareForgeBuildNumbers(string newBuild, string existingBuild)
        {
            // Split the build numbers into parts (e.g., "7.8.1.738" -> ["7", "8", "1", "738"])
            string[] newBuildParts = newBuild.Split('.');
            string[] existingBuildParts = existingBuild.Split('.');

            // Compare each part of the build numbers
            for (int i = 0; i < Math.Min(newBuildParts.Length, existingBuildParts.Length); i++)
            {
                if (int.TryParse(newBuildParts[i], out int newPart) && int.TryParse(existingBuildParts[i], out int existingPart))
                {
                    if (newPart > existingPart)
                    {
                        return true; // New build is greater
                    }
                    else if (newPart < existingPart)
                    {
                        return false; // Existing build is greater
                    }
                }
            }

            // If all parts are equal, the longer build number is considered greater
            return newBuildParts.Length > existingBuildParts.Length;
        }

        // ------------------------ ↓ NeoForge Updater Funcs ↓ ------------------------

        private static async Task CheckAndUpdateNeoForgeAsync(string downloadDirectory, string? selectedVersion = null)
        {
            try
            {
                // Fetch the list of available Minecraft versions
                string versionsXml = await HttpClient.GetStringAsync(NeoForgeVersionsUrl);
                XDocument versionsDoc = XDocument.Parse(versionsXml);

                // Extract version numbers from the XML
                var versions = versionsDoc.Descendants("version")
                                          .Select(v => v.Value)
                                          .Where(v => IsStableVersion(v)) // Filter out unstable versions
                                          .ToList();

                if (selectedVersion != null)
                {
                    // If a specific version is provided, only process that version
                    if (versions.Contains(selectedVersion))
                    {
                        await ProcessNeoForgeVersion(downloadDirectory, selectedVersion);
                    }
                    else
                    {
                        Console.WriteLine($"Version {selectedVersion} not found in the list of available versions.");
                    }
                }
                else
                {
                    // If no specific version is provided, process all versions
                    foreach (string version in versions)
                    {
                        await ProcessNeoForgeVersion(downloadDirectory, version);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static bool IsStableVersion(string version)
        {
            // Filter out versions with "-pre", "-rc", or snapshot-like versions (e.g., "25w09b")
            return !version.Contains("-pre") && !version.Contains("-rc") && !version.Contains("w");
        }

        private static async Task ProcessNeoForgeVersion(string downloadDirectory, string version)
        {
            try
            {
                // Fetch the version info from JSON
                string versionInfoJson = await HttpClient.GetStringAsync($"{NeoForgeVersionInfoUrl}{version}.json");
                JObject versionInfo = JObject.Parse(versionInfoJson);

                // Get the server download URL
                string serverUrl = versionInfo["downloads"]?["server"]?["url"]?.ToString();

                if (!string.IsNullOrEmpty(serverUrl))
                {
                    string fileName = $"neoforge-{version}.jar";
                    string destinationPath = Path.Combine(downloadDirectory, fileName);

                    // Check if a file for this version already exists locally
                    if (File.Exists(destinationPath))
                    {
                        Console.WriteLine($"Skipping download for version {version}. File already exists.");
                    }
                    else
                    {
                        // Download the server file
                        await DownloadServerJarAsync(serverUrl, destinationPath);
                    }
                }
                else
                {
                    Console.WriteLine($"Could not find server download URL for version {version}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process version {version}: {ex.Message}");
            }
        }

        // ------------------------ ↓ Quilt Updater Funcs ↓ ------------------------

        private static async Task CheckAndUpdateLatestQuiltAsync(string downloadDirectory)
        {
            try
            {
                Console.WriteLine("Fetching the latest Quilt Minecraft Server Installer...");
                Directory.CreateDirectory(downloadDirectory);

                // Get the latest stable Fabric Loader version
                string xmlContent = await HttpClient.GetStringAsync($"{QuiltApiBaseUrl}maven-metadata.xml");

                // Filter to get only the latest stable version
                XDocument doc = XDocument.Parse(xmlContent);
                string latestVersion = doc.Descendants("latest").FirstOrDefault()?.Value ?? "Unknown";

                if (doc == null)
                {
                    Console.WriteLine("Failed to retrieve the latest Quilt Loader version.");
                    return;
                }

                string localFilePath = Path.Combine(downloadDirectory, $"quilt-installer-{latestVersion}.jar");
                if (File.Exists(localFilePath))
                {
                    Console.WriteLine($"Latest Quilt Universal Server Installer {latestVersion} is already downloaded.");
                    return;
                }
                else
                {
                    DeleteFiles(downloadDirectory, false);
                    Console.WriteLine($"Downloading Quilt Universal Server Installer {latestVersion}...");
                    string jarUrl = $"{QuiltApiBaseUrl}{latestVersion}/quilt-installer-{latestVersion}.jar";
                    await DownloadServerJarAsync(jarUrl, localFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching the latest Quilt version: {ex.Message}");
            }
        }

        // ------------------------ ↓ Purpur Updater Funcs ↓ ------------------------
        private static async Task CheckAndUpdatePurpurAsync(string downloadDirectory, string? selectedVersion = null)
        {
            Console.WriteLine($"Checking Purpur versions in: {downloadDirectory}");

            // Get local versions and builds
            var localFiles = Directory.GetFiles(downloadDirectory, "purpur-*.jar");

            if (localFiles.Length == 0)
            {
                Console.WriteLine("No local files found.");
                return;
            }
            else
            {
                foreach (var file in localFiles)
                {
                    Console.WriteLine("Local files detected:");
                }
            }

            var localVersions = new Dictionary<string, int>();

            foreach (var file in localFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                var parts = fileName.Replace("purpur-", "").Split('-');
                if (parts.Length == 1) // Handle filenames without build numbers
                {
                    string version = parts[0];
                    localVersions[version] = 0;
                    Console.WriteLine($"Detected version: {version}, Build: 0 (assumed)");
                }
                else if (parts.Length == 2 && int.TryParse(parts[1], out int build)) // Handle filenames with build numbers
                {
                    localVersions[parts[0]] = build;
                    Console.WriteLine($"Detected version: {parts[0]}, Build: {build}");
                }
                else
                {
                    Console.WriteLine($"Skipping {fileName} - Unrecognized format");
                }
            }

            var availableVersions = await GetAvailablePurpurVersionsAsync(selectedVersion);
            if (availableVersions == null || availableVersions.Length == 0)
            {
                Console.WriteLine("No available versions found.");
                return;
            }

            Console.WriteLine($"Found {availableVersions.Length} versions.");
            foreach (var version in availableVersions)
            {
                // Fetch the latest build for the version
                int? latestBuild = await GetPurpurLatestBuildAsync(version);

                if (latestBuild == null)
                {
                    Console.WriteLine($"Skipping {version} because no latest build was found.");
                    continue;
                }

                string url = $"{PurpurApiBaseUrl}{version}/latest/download";
                string finalFilePath = Path.Combine(downloadDirectory, $"purpur-{version}-{latestBuild}.jar");

                // Check if the version is missing or if the local file doesn't have a build number
                if (!localVersions.ContainsKey(version) || localVersions[version] == 0)
                {
                    // Delete the old file if it exists (without a build number)
                    string oldFileWithoutBuild = Path.Combine(downloadDirectory, $"purpur-{version}.jar");
                    if (File.Exists(oldFileWithoutBuild))
                    {
                        Console.WriteLine($"Deleting old version without build number: {oldFileWithoutBuild}");
                        File.Delete(oldFileWithoutBuild);
                    }

                    // Download the latest version and rename it to include the build number
                    Console.WriteLine($"Purpur {version} is missing or outdated. Downloading...");
                    await DownloadServerJarAsync(url, finalFilePath);
                }
                else
                {
                    int currentBuild = localVersions[version];

                    if (currentBuild < latestBuild)
                    {
                        Console.WriteLine($"Purpur {version} is outdated. Updating...");
                        string oldFile = Path.Combine(downloadDirectory, $"purpur-{version}-{currentBuild}.jar");
                        if (File.Exists(oldFile))
                        {
                            Console.WriteLine($"Deleting old version: {oldFile}");
                            File.Delete(oldFile);
                        }
                        await DownloadServerJarAsync(url, finalFilePath);
                    }
                    else
                    {
                        Console.WriteLine($"Purpur {version} is up to date.");
                    }
                }
            }

            Console.WriteLine("--------------------------------");
            Console.WriteLine("All Purpur versions are updated.");
            Console.WriteLine("--------------------------------");
        }

        private static async Task<string[]> GetAvailablePurpurVersionsAsync(string? selectedVersion = null)
        {
            try
            {
                var response = await HttpClient.GetStringAsync(PurpurApiBaseUrl);

                using var jsonDoc = JsonDocument.Parse(response);
                var versions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray().Select(e => e.GetString()).ToArray();

                if (selectedVersion != null)
                {
                    versions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray().Where(v => v.GetString() == $"{selectedVersion}").Select(e => e.GetString()).ToArray();
                }
                foreach (var version in versions)
                {
                    Console.WriteLine($"Avaliable version: {version}");
                }

                return versions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching versions: {ex.Message}");
                return null;
            }
        }

        private static async Task<int?> GetPurpurLatestBuildAsync(string version)
        {
            try
            {
                var response = await HttpClient.GetStringAsync($"{PurpurApiBaseUrl}{version}");

                using var jsonDoc = JsonDocument.Parse(response);

                var buildsArray = jsonDoc.RootElement.GetProperty("builds").GetProperty("all").EnumerateArray();
                var buildNumbers = buildsArray
                    .Select(e => int.TryParse(e.GetString(), out int num) ? num : (int?)null)
                    .Where(num => num.HasValue)
                    .Select(num => num.Value);

                return buildNumbers.Any() ? buildNumbers.Max() : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching latest build for {version}: {ex.Message}");
                return null;
            }
        }

        // ------------------------ ↓ Paper Updater Funcs ↓ ------------------------

        private static async Task CheckAndUpdatePaperAsync(string downloadDirectory, string? version)
        {
            try
            {
                if (version != null)
                {
                    string latestBuild = await GetLatestBuild(version);

                    if (string.IsNullOrEmpty(latestBuild))
                    {
                        Console.WriteLine("Failed to retrieve latest build number.");
                        return;
                    }

                    string[] existingFiles = Directory.GetFiles(downloadDirectory, $"paper-{version}-*.jar");
                    if (existingFiles.Length > 0)
                    {
                        await CheckLocalFiles(existingFiles, latestBuild, version, downloadDirectory);
                    }
                    else
                    {
                        string fileName = $"paper-{version}-{latestBuild}.jar";
                        string jarUrl = $"{PaperApiBaseUrl}/versions/{version}/builds/{latestBuild}/downloads/{fileName}";
                        string destinationPath = Path.Combine(downloadDirectory, fileName);
                        Console.WriteLine($"Downloading version: {version}");
                        await DownloadServerJarAsync(jarUrl, destinationPath);
                    }
                }
                else
                {
                    var response = await HttpClient.GetStringAsync(PaperApiBaseUrl);

                    using var jsonDoc = JsonDocument.Parse(response);
                    var versions = jsonDoc.RootElement.GetProperty("versions")
                                                       .EnumerateArray()
                                                       .Select(e => e.GetString())
                                                       .Where(v => v != null && !v.Contains("pre") && !v.Contains("beta"))
                                                       .ToArray();

                    if (versions.Length == 0)
                    {
                        Console.WriteLine("No versions found.");
                        return;
                    }

                    foreach (string? versionAvaliable in versions)
                    {
                        Console.WriteLine($"-{versionAvaliable}");
                    }

                    foreach (string? versionAvaliable in versions)
                    {
                        string latestBuild = await GetLatestBuild(versionAvaliable);

                        string[] existingFiles = Directory.GetFiles(downloadDirectory, $"paper-{versionAvaliable}-*.jar");
                        if (existingFiles.Length > 0)
                        {
                            await CheckLocalFiles(existingFiles, latestBuild, versionAvaliable, downloadDirectory);
                        }
                        else
                        {
                            string fileName = $"paper-{versionAvaliable}-{latestBuild}.jar";
                            string jarUrl = $"{PaperApiBaseUrl}/versions/{versionAvaliable}/builds/{latestBuild}/downloads/{fileName}";
                            string destinationPath = Path.Combine(downloadDirectory, fileName);
                            await DownloadServerJarAsync(jarUrl, destinationPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static async Task CheckLocalFiles(string[] existingFiles, string latestBuild, string? version, string downloadDirectory)
        {
            foreach (string file in existingFiles)
            {
                string[] parts = Path.GetFileNameWithoutExtension(file).Split('-');
                string existingBuild = parts[2];
                if (latestBuild != existingBuild)
                {
                    Console.WriteLine($"Newer build found for version {version}. Existing build: {existingBuild}, New build: {latestBuild}");
                    File.Delete(file);
                    Console.WriteLine($"Deleted old file: {file}");
                    string fileName = $"paper-{version}-{latestBuild}.jar";
                    string jarUrl = $"{PaperApiBaseUrl}/versions/{version}/builds/{latestBuild}/downloads/{fileName}";
                    string destinationPath = Path.Combine(downloadDirectory, fileName);
                    await DownloadServerJarAsync(jarUrl, destinationPath);
                }
                else
                {
                    Console.WriteLine($"Skipping download for version {version}. Existing build ({existingBuild}) is up to date.");
                }
            }
        }

        private static async Task<string> GetLatestBuild(string? version)
        {
            string apiUrl = $"{PaperApiBaseUrl}/versions/{version}/builds";
            HttpResponseMessage response = await HttpClient.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                JObject data = JObject.Parse(json);
                JArray builds = (JArray)data["builds"];
                return builds.Last?["build"]?.ToString() ?? "";
            }
            return "";
        }

        // ------------------------ ↓ Main Updater Funcs ↓ ------------------------

        public static async Task Update(string serverVersionsPath)
        {
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine("Starting versions updater for all softwares.");
            Console.WriteLine("--------------------------------------------");

            object[,] softwareTypes = GetFoldersAsArray(serverVersionsPath);

            foreach (string software in softwareTypes)
            {
                string versionDirectory = Path.Combine(serverVersionsPath, software);
                Console.WriteLine($"Checking {software} versions in: {versionDirectory}");

                // Ensure the download directory exists
                if (!Directory.Exists(versionDirectory))
                {
                    Directory.CreateDirectory(versionDirectory);
                }

                await RunUpdaterForSoftware(versionDirectory, software);
            }

            Console.WriteLine("-------------------------");
            Console.WriteLine("All versions are updated!");
            Console.WriteLine("-------------------------");
        }

        public static async Task Update(string serverVersionsPath, string software)
        {
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine($"Starting versions updater for {software}.");
            Console.WriteLine("--------------------------------------------");

            string versionDirectory = Path.Combine(serverVersionsPath, software);

            // Ensure the download directory exists
            if (!Directory.Exists(versionDirectory))
            {
                Directory.CreateDirectory(versionDirectory);
            }

            await RunUpdaterForSoftware(versionDirectory, software);

            Console.WriteLine("-------------------------");
            Console.WriteLine("All versions are updated!");
            Console.WriteLine("-------------------------");
        }

        public static async Task Update(string serverVersionsPath, string software, string version)
        {
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine($"Starting versions updater for {software}.");
            Console.WriteLine("--------------------------------------------");

            string versionDirectory = Path.Combine(serverVersionsPath, software);

            // Ensure the download directory exists
            if (!Directory.Exists(versionDirectory))
            {
                Directory.CreateDirectory(versionDirectory);
            }

            await RunUpdaterForSoftware(versionDirectory, software, version, true);

            Console.WriteLine("-------------------------");
            Console.WriteLine("All versions are updated!");
            Console.WriteLine("-------------------------");
        }

        // ------------------------ ↓ Helpers for Updater Funcs ↓ ------------------------

        private static async Task RunUpdaterForSoftware(string versionDirectory, string software, string? version = null, bool skipCheckForInstallers = false)
        {
            if (skipCheckForInstallers && (software == "Fabric" || software == "Quilt"))
            {
                Console.WriteLine("This is an universal Installer, there aren't versions, checking the installer...");
            }
            if (software == "Vanilla")
            {
                await CheckAndUpdateVanillaAsync(versionDirectory, version);
            }
            else if (software == "Forge")
            {
                await CheckAndUpdateForgeAsync(versionDirectory, version);
            }
            else if (software == "NeoForge")
            {
                await CheckAndUpdateNeoForgeAsync(versionDirectory, version);
            }
            else if (software == "Fabric")
            {
                await CheckAndUpdateLatestFabricAsync(versionDirectory);
            }
            else if (software == "Quilt")
            {
                await CheckAndUpdateLatestQuiltAsync(versionDirectory);
            }
            else if (software == "Purpur")
            {
                await CheckAndUpdatePurpurAsync(versionDirectory, version);
            }
            else if (software == "Paper")
            {
                await CheckAndUpdatePaperAsync(versionDirectory, version);
            }
            else
            {
                Console.WriteLine($"Unknown version type: {software}");
            }
        }

        private static void DeleteFiles(string path, bool deleteWholeDirectory = true)
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static object[,] GetFoldersAsArray(string rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine("Root directory does not exist.");
                return new object[0, 0];
            }

            string[] folders = Directory.GetDirectories(rootDirectory);
            object[,] softwareTypes = new object[folders.Length, 1];

            for (int i = 0; i < folders.Length; i++)
            {
                softwareTypes[i, 0] = Path.GetFileName(folders[i]);
            }

            return softwareTypes;
        }

        // ------------------------ ↓ Download Function ↓ ------------------------
        private static async Task DownloadServerJarAsync(string? jarUrl, string savePath)
        {
            try
            {
                using var jarResponse = await HttpClient.GetStreamAsync(jarUrl);
                using var fileStream = File.Create(savePath);
                await jarResponse.CopyToAsync(fileStream);

                Console.WriteLine($"Downloaded successfully: {Path.GetFileName(savePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download {jarUrl}: {ex.Message}");
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
            }
        }
    }
}