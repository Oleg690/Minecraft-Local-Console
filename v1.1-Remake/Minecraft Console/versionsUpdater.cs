using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Logger;
using System.Net.Http;
using System.IO;

namespace Updater
{
    class VersionsUpdater
    {
        private static readonly string? currentDirectory = Directory.GetCurrentDirectory();
        private static readonly string? rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory))) ?? string.Empty;
        private static readonly string? versionsSupprortListXML = Path.Combine(rootFolder, "SupportedVersions.xml");

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
            var availableVersions = await GetAvailableVanillaServerVersionsAsync(selectedVersion);
            if (availableVersions == null || availableVersions.Count == 0)
            {
                CodeLogger.ConsoleLog("No available server versions found.");
                return;
            }

            var localFiles = Directory.GetFiles(downloadDirectory, "vanilla-*.jar");
            var localVersions = localFiles.Select(f => Path.GetFileNameWithoutExtension(f).Replace("vanilla-", "")).ToHashSet();

            foreach (var version in availableVersions.Keys)
            {
                if (CheckVersionExists("Vanilla", version) == false)
                {
                    continue;
                }

                if (!localVersions.Contains(version))
                {
                    var response = await HttpClient.GetStringAsync(availableVersions[version]);
                    using var jsonDoc = JsonDocument.Parse(response);

                    try
                    {
                        string? jarUrl = jsonDoc.RootElement.GetProperty("downloads")
                                                    .GetProperty("server")
                                                    .GetProperty("url")
                                                    .GetString();

                        string savePath = Path.Combine(downloadDirectory, $"vanilla-{version}.jar");

                        CodeLogger.ConsoleLog($"Vanilla Server {version} is missing. Downloading...");

                        await DownloadServerJarAsync(jarUrl, savePath);
                    }
                    catch (Exception ex)
                    {
                        CodeLogger.ConsoleLog($"Version not found. Error: {ex.Message}");
                    }
                }
                else
                {
                    CodeLogger.ConsoleLog($"Skipping download for version {version}. Existing build is up to date.");
                }
            }
        }

        private static async Task<Dictionary<string, string>?> GetAvailableVanillaServerVersionsAsync(string? selectedVersion = null)
        {
            try
            {
                var response = await HttpClient.GetStringAsync(VanillaApiBaseUrl);
                using var jsonDoc = JsonDocument.Parse(response);

                var availableVersions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray()
                    .Where(v => v.GetProperty("type").GetString() == "release")
                    .ToDictionary(
                        v => v.GetProperty("id").GetString() ?? string.Empty,
                        v => v.GetProperty("url").GetString() ?? string.Empty
                    );

                if (!string.IsNullOrEmpty(selectedVersion))
                {
                    availableVersions = availableVersions
                        .Where(v => v.Key == selectedVersion)
                        .ToDictionary(v => v.Key, v => v.Value);
                }
                return availableVersions;
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error fetching versions: {ex.Message}");
                return null;
            }
        }

        // ------------------------ ↓ Fabric Updater Funcs ↓ ------------------------

        private static async Task CheckAndUpdateLatestFabricAsync(string downloadDirectory)
        {
            try
            {
                // Get the latest stable Fabric Loader version
                var loaderResponse = await HttpClient.GetStringAsync(FabricApiBaseUrl);
                using var loaderJson = JsonDocument.Parse(loaderResponse);

                // Filter to get only the latest stable version
                var latestLoader = loaderJson.RootElement.EnumerateArray()
                    .Where(v => v.GetProperty("stable").GetBoolean() == true)
                    .ToDictionary(
                        v => v.GetProperty("version").GetString() ?? string.Empty,
                        v => v.GetProperty("url").GetString() ?? string.Empty
                    );

                if (latestLoader.TryGetValue == null)
                {
                    CodeLogger.ConsoleLog("Failed to retrieve the latest Fabric Loader version.");
                    return;
                }

                foreach (var loader in latestLoader)
                {
                    string localFilePath = Path.Combine(downloadDirectory, $"fabric-installer-{loader.Key}.jar");
                    if (File.Exists(localFilePath))
                    {
                        CodeLogger.ConsoleLog($"Latest Fabric Universal Server Installer {loader.Key} is already downloaded.");
                        return;
                    }
                    else
                    {
                        DeleteFiles(downloadDirectory, false);
                        CodeLogger.ConsoleLog($"Downloading Fabric Universal Server Installer {loader.Key}...");
                        await DownloadServerJarAsync(loader.Value, localFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error fetching the latest Fabric version: {ex.Message}");
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
                List<string> stableVersions = [];

                if (promotions["promos"] is not JObject promos)
                {
                    CodeLogger.ConsoleLog("Promotions data is null.");
                    return;
                }

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
                        CodeLogger.ConsoleLog($"Version {selectedVersion} not found in the list of stable versions.");
                    }
                }
                else
                {
                    // If no specific version is provided, process all versions
                    foreach (string version in stableVersions)
                    {
                        if (CheckVersionExists("Forge", version) == false)
                        {
                            continue;
                        }
                        await ProcessVersion(downloadDirectory, promos, version);
                    }
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"An error occurred: {ex.Message}");
            }
        }

        private static async Task ProcessVersion(string downloadDirectory, JObject promos, string version)
        {
            string? build = promos[$"{version}-latest"]?.ToString();
            if (build == null)
            {
                CodeLogger.ConsoleLog($"Build information for version {version} is missing.");
                return;
            }
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
                        CodeLogger.ConsoleLog($"Newer build found for version {version}. Existing build: {existingBuild}, New build: {build}");

                        // Delete the old file
                        File.Delete(existingFiles[0]);
                        CodeLogger.ConsoleLog($"Deleted old file: {existingFileName}.jar");

                        // Download the new file
                        await DownloadServerJarAsync(actualDownloadUrl, destinationPath);
                    }
                    else
                    {
                        CodeLogger.ConsoleLog($"Skipping download for version {version}. Existing build ({existingBuild}) is up to date.");
                    }
                }
                else
                {
                    CodeLogger.ConsoleLog($"Unable to parse build number from existing file: {existingFileName}");
                }
            }
            else
            {
                // If no local file exists, download the new file
                CodeLogger.ConsoleLog($"No local file found for version {version}. Downloading new build: {build}");

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
                        CodeLogger.ConsoleLog($"Version {selectedVersion} not found in the list of available versions.");
                    }
                }
                else
                {
                    // If no specific version is provided, process all versions
                    foreach (string version in versions)
                    {
                        if (CheckVersionExists("NeoForge", version) == false)
                        {
                            continue;
                        }

                        await ProcessNeoForgeVersion(downloadDirectory, version);
                    }
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"An error occurred: {ex.Message}");
            }
        }

        private static bool IsStableVersion(string version)
        {
            // Filter out versions with "-pre", "-rc", or snapshot-like versions (e.g., "25w09b")
            return !version.Contains("-pre") && !version.Contains("-rc") && !version.Contains('w');
        }

        private static async Task ProcessNeoForgeVersion(string downloadDirectory, string version)
        {
            try
            {
                // Fetch the version info from JSON
                string versionInfoJson = await HttpClient.GetStringAsync($"{NeoForgeVersionInfoUrl}{version}.json");
                JObject versionInfo = JObject.Parse(versionInfoJson);

                // Get the server download URL
                string? serverUrl = versionInfo["downloads"]?["server"]?["url"]?.ToString();

                if (!string.IsNullOrEmpty(serverUrl))
                {
                    string fileName = $"neoforge-{version}.jar";
                    string destinationPath = Path.Combine(downloadDirectory, fileName);

                    // Check if a file for this version already exists locally
                    if (File.Exists(destinationPath))
                    {
                        CodeLogger.ConsoleLog($"Skipping download for version {version}. Existing build is up to date.");
                    }
                    else
                    {
                        // Download the server file
                        CodeLogger.ConsoleLog($"No local file found for version {version}. Downloading...");
                        await DownloadServerJarAsync(serverUrl, destinationPath);
                    }
                }
                else
                {
                    CodeLogger.ConsoleLog($"Could not find server download URL for version {version}.");
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Failed to process version {version}: {ex.Message}");
            }
        }

        // ------------------------ ↓ Quilt Updater Funcs ↓ ------------------------

        private static async Task CheckAndUpdateLatestQuiltAsync(string downloadDirectory)
        {
            try
            {
                // Get the latest stable Fabric Loader version
                string xmlContent = await HttpClient.GetStringAsync($"{QuiltApiBaseUrl}maven-metadata.xml");

                // Filter to get only the latest stable version
                XDocument doc = XDocument.Parse(xmlContent);
                string latestVersion = doc.Descendants("latest").FirstOrDefault()?.Value ?? "Unknown";

                if (doc == null)
                {
                    CodeLogger.ConsoleLog("Failed to retrieve the latest Quilt Loader version.");
                    return;
                }

                string localFilePath = Path.Combine(downloadDirectory, $"quilt-installer-{latestVersion}.jar");
                if (File.Exists(localFilePath))
                {
                    CodeLogger.ConsoleLog($"Latest Quilt Universal Server Installer {latestVersion} is already downloaded.");
                    return;
                }
                else
                {
                    DeleteFiles(downloadDirectory, false);
                    CodeLogger.ConsoleLog($"Downloading Quilt Universal Server Installer {latestVersion}...");
                    string jarUrl = $"{QuiltApiBaseUrl}{latestVersion}/quilt-installer-{latestVersion}.jar";
                    await DownloadServerJarAsync(jarUrl, localFilePath);
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error fetching the latest Quilt version: {ex.Message}");
            }
        }

        // ------------------------ ↓ Purpur Updater Funcs ↓ ------------------------
        private static async Task CheckAndUpdatePurpurAsync(string downloadDirectory, string? selectedVersion = null)
        {
            var localFiles = Directory.GetFiles(downloadDirectory, "purpur-*.jar");
            var localVersions = new Dictionary<string, int>();

            foreach (var file in localFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                var parts = fileName.Replace("purpur-", "").Split('-');
                if (parts.Length == 1)
                {
                    string version = parts[0];
                    localVersions[version] = 0;
                }
                else if (parts.Length == 2 && int.TryParse(parts[1], out int build)) // Handle filenames with build numbers
                {
                    localVersions[parts[0]] = build;
                }
                else
                {
                    CodeLogger.ConsoleLog($"Skipping {fileName} - Unrecognized format");
                }
            }

            var availableVersions = await GetAvailablePurpurVersionsAsync(selectedVersion);
            if (availableVersions == null || availableVersions.Length == 0)
            {
                CodeLogger.ConsoleLog("No available versions found.");
                return;
            }

            foreach (var version in availableVersions)
            {
                if (CheckVersionExists("Purpur", version) == false)
                {
                    continue;
                }
                // Fetch the latest build for the version
                int? latestBuild = await GetPurpurLatestBuildAsync(version);

                if (latestBuild == null)
                {
                    CodeLogger.ConsoleLog($"Skipping {version} because no latest build was found.");
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
                        CodeLogger.ConsoleLog($"Deleting old version without build number: {oldFileWithoutBuild}");
                        File.Delete(oldFileWithoutBuild);
                    }

                    // Download the latest version and rename it to include the build number
                    CodeLogger.ConsoleLog($"Purpur {version} is missing. Downloading...");
                    await DownloadServerJarAsync(url, finalFilePath);
                }
                else
                {
                    int currentBuild = localVersions[version];

                    if (currentBuild < latestBuild)
                    {
                        CodeLogger.ConsoleLog($"Purpur {version} is outdated. Updating...");
                        string oldFile = Path.Combine(downloadDirectory, $"purpur-{version}-{currentBuild}.jar");
                        if (File.Exists(oldFile))
                        {
                            CodeLogger.ConsoleLog($"Deleting old version: {oldFile}");
                            File.Delete(oldFile);
                        }
                        await DownloadServerJarAsync(url, finalFilePath);
                    }
                    else
                    {
                        CodeLogger.ConsoleLog($"Skipping download for version {version}. Existing is up to date.");
                    }
                }
            }
        }

        private static async Task<string[]?> GetAvailablePurpurVersionsAsync(string? selectedVersion = null)
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

                return versions!;
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error fetching versions: {ex.Message}");
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
                    .Select(num => num!.Value);

                return buildNumbers.Any() ? buildNumbers.Max() : null;
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error fetching latest build for {version}: {ex.Message}");
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
                        CodeLogger.ConsoleLog("Failed to retrieve latest build number.");
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
                        CodeLogger.ConsoleLog($"Paper Server {version} is missing. Downloading...");
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
                        CodeLogger.ConsoleLog("No versions found.");
                        return;
                    }

                    foreach (string? versionAvaliable in versions)
                    {
                        if (CheckVersionExists("Paper", versionAvaliable!) == false)
                        {
                            continue;
                        }

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
                            CodeLogger.ConsoleLog($"Paper Server {versionAvaliable} is missing. Downloading...");
                            await DownloadServerJarAsync(jarUrl, destinationPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"An error occurred: {ex.Message}");
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
                    CodeLogger.ConsoleLog($"Newer build found for version {version}. Existing build: {existingBuild}, New build: {latestBuild}");
                    File.Delete(file);
                    CodeLogger.ConsoleLog($"Deleted old file: {file}");
                    string fileName = $"paper-{version}-{latestBuild}.jar";
                    string jarUrl = $"{PaperApiBaseUrl}/versions/{version}/builds/{latestBuild}/downloads/{fileName}";
                    string destinationPath = Path.Combine(downloadDirectory, fileName);
                    await DownloadServerJarAsync(jarUrl, destinationPath);
                }
                else
                {
                    CodeLogger.ConsoleLog($"Skipping download for version {version}. Existing build ({existingBuild}) is up to date.");
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
                JArray? builds = data["builds"] as JArray;
                return builds!.Last?["build"]?.ToString() ?? "";
            }
            return "";
        }

        // ------------------------ ↓ Main Updater Funcs ↓ ------------------------

        public static async Task Update(string serverVersionsPath)
        {
            CodeLogger.ConsoleLog("--------------------------------------------");
            CodeLogger.ConsoleLog("Starting versions updater for all softwares.");
            CodeLogger.ConsoleLog("--------------------------------------------");

            object[,] softwareTypes = GetAvailableSoftwares();

            foreach (string software in softwareTypes)
            {
                string versionDirectory = Path.Combine(serverVersionsPath, software);
                CodeLogger.ConsoleLog($"Checking {software} versions in: {versionDirectory} \n");

                if (!Directory.Exists(versionDirectory))
                {
                    Directory.CreateDirectory(versionDirectory);
                }

                await RunUpdaterForSoftware(versionDirectory, software);

                CodeLogger.ConsoleLog("----------------------------------------------");
                CodeLogger.ConsoleLog($"All {software} Server versions are updated!");
                CodeLogger.ConsoleLog("----------------------------------------------");

            }

            CodeLogger.ConsoleLog("--------------------------");
            CodeLogger.ConsoleLog("All softwares are updated!");
            CodeLogger.ConsoleLog("--------------------------");
        }

        public static async Task Update(string serverVersionsPath, string software)
        {
            CodeLogger.ConsoleLog("--------------------------------------------");
            CodeLogger.ConsoleLog($"Starting versions updater for {software}.");
            CodeLogger.ConsoleLog("--------------------------------------------");

            string versionDirectory = Path.Combine(serverVersionsPath, software);

            // Ensure the download directory exists
            if (!Directory.Exists(versionDirectory))
            {
                Directory.CreateDirectory(versionDirectory);
            }

            await RunUpdaterForSoftware(versionDirectory, software);

            CodeLogger.ConsoleLog("-------------------------");
            CodeLogger.ConsoleLog("All versions are updated!");
            CodeLogger.ConsoleLog("-------------------------");
        }

        public static async Task Update(string serverVersionsPath, string software, string version)
        {
            CodeLogger.ConsoleLog("--------------------------------------------");
            CodeLogger.ConsoleLog($"Starting versions updater for {software}.");
            CodeLogger.ConsoleLog("--------------------------------------------");

            string versionDirectory = Path.Combine(serverVersionsPath, software);

            // Ensure the download directory exists
            if (!Directory.Exists(versionDirectory))
            {
                Directory.CreateDirectory(versionDirectory);
            }

            await RunUpdaterForSoftware(versionDirectory, software, version, true);

            CodeLogger.ConsoleLog("-------------------------");
            CodeLogger.ConsoleLog("All versions are updated!");
            CodeLogger.ConsoleLog("-------------------------");
        }

        // ------------------------ ↓ Helpers for Updater Funcs ↓ ------------------------

        private static async Task RunUpdaterForSoftware(string versionDirectory, string software, string? version = null, bool skipCheckForInstallers = false)
        {
            if (version != null)
            {
                if (CheckVersionExists(software, version) == false)
                {
                    CodeLogger.ConsoleLog($"Version {version} not found in the list of supported versions.");
                    return;
                }
            }
            if (skipCheckForInstallers && (software == "Fabric" || software == "Quilt"))
            {
                CodeLogger.ConsoleLog("This is an universal Installer, there aren't versions, checking the installer...");
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
                CodeLogger.ConsoleLog($"Unknown version type: {software}");
            }
        }

        private static void DeleteFiles(string path, bool deleteWholeDirectory = true)
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
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"An error occurred: {ex.Message}");
            }
        }

        public static object[,] GetAvailableSoftwares()
        {
            if (string.IsNullOrEmpty(versionsSupprortListXML))
            {
                throw new InvalidOperationException("The path to the supported versions XML file is not set.");
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(versionsSupprortListXML);

            XmlNodeList? softwareNodes = doc.SelectNodes("/minecraft_softwares/software");
            List<object> softwareList = new List<object>();

            if (softwareNodes == null)
            {
                return new object[0, 0];
            }

            foreach (XmlNode softwareNode in softwareNodes)
            {
                string? softwareName = softwareNode.Attributes?["name"]?.Value;
                if (!string.IsNullOrEmpty(softwareName) && !softwareList.Contains(softwareName))
                {
                    softwareList.Add(softwareName);
                }
            }

            // Convert List<object> to object[,]
            object[,] result = new object[softwareList.Count, 1];
            for (int i = 0; i < softwareList.Count; i++)
            {
                result[i, 0] = softwareList[i];
            }

            return result;
        }

        public static bool CheckVersionExists(string softwareName, string version)
        {
            if (string.IsNullOrEmpty(versionsSupprortListXML))
            {
                throw new InvalidOperationException("The path to the supported versions XML file is not set.");
            }

            XmlDocument doc = new();
            doc.Load(versionsSupprortListXML);

            XmlNode? softwareNode = doc.SelectSingleNode($"/minecraft_softwares/software[@name='{softwareName}']");
            if (softwareNode == null)
            {
                return false; // Software not found
            }

            foreach (XmlNode versionNode in softwareNode.SelectNodes("version")!)
            {
                if (versionNode.InnerText == version)
                {
                    return true; // Version found
                }
            }

            return false; // Version not found
        }

        // ------------------------ ↓ Download Function ↓ ------------------------
        private static async Task DownloadServerJarAsync(string? jarUrl, string savePath)
        {
            try
            {
                using var jarResponse = await HttpClient.GetStreamAsync(jarUrl);
                using var fileStream = File.Create(savePath);
                await jarResponse.CopyToAsync(fileStream);

                CodeLogger.ConsoleLog($"Downloaded successfully: {Path.GetFileName(savePath)}");
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Failed to download {jarUrl}: {ex.Message}");
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
            }
        }
    }
}