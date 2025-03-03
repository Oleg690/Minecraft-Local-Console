using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using com.sun.xml.@internal.ws.util;
using static jdk.jfr.@internal.SecuritySupport;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;

namespace Updater
{
    class VersionsUpdater
    {
        private static readonly string VanillaApiBaseUrl = "https://piston-meta.mojang.com/mc/game/version_manifest.json";
        private static readonly string ForgeApiVersionsUrl = "https://files.minecraftforge.net/net/minecraftforge/forge/";
        private static readonly string ForgeApiBaseUrl = "https://maven.minecraftforge.net/net/minecraftforge/forge/";
        private static readonly string NeoForgeVersionsUrl = "https://maven.neoforged.net/mojang-meta/net/neoforged/minecraft-dependencies/maven-metadata.xml";
        private static readonly string NeoForgeVersionInfoUrl = "https://maven.neoforged.net/mojang-meta/net/minecraft/{0}.json";
        private static readonly string FabricApiBaseUrl = "https://meta.fabricmc.net/v2/versions/installer/";
        private static readonly string QuiltApiBaseUrl = "https://maven.quiltmc.org/repository/release/org/quiltmc/quilt-installer/";
        private static readonly string PurpurApiBaseUrl = "https://api.purpurmc.org/v2/purpur/";
        private static readonly string SpigotApiBaseUrl = "";

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
                    await DownloadVannillaServerJarAsync(availableVersions[version], downloadDirectory, version);
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

        private static async Task DownloadVannillaServerJarAsync(string versionUrl, string downloadDirectory, string version)
        {
            try
            {
                var response = await HttpClient.GetStringAsync(versionUrl);
                using var jsonDoc = JsonDocument.Parse(response);

                string jarUrl = jsonDoc.RootElement.GetProperty("downloads")
                                                  .GetProperty("server")
                                                  .GetProperty("url")
                                                  .GetString();

                string savePath = Path.Combine(downloadDirectory, $"vanilla-{version}.jar");

                using var jarResponse = await HttpClient.GetStreamAsync(jarUrl);
                using var fileStream = File.Create(savePath);
                await jarResponse.CopyToAsync(fileStream);

                Console.WriteLine($"Downloaded Minecraft Server {version} successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download Minecraft Server {version}: {ex.Message}");
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
                    //Console.WriteLine($"{loader.Key}");
                    //Console.WriteLine($"{loader.Value}");

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
                        await DownloadFabricServerJarAsync(loader.Value, localFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching the latest Fabric version: {ex.Message}");
            }
        }

        private static async Task DownloadFabricServerJarAsync(string jarUrl, string savePath)
        {
            try
            {
                using var jarResponse = await HttpClient.GetStreamAsync(jarUrl);
                using var fileStream = File.Create(savePath);
                await jarResponse.CopyToAsync(fileStream);

                Console.WriteLine($"Fabric Server downloaded successfully: {Path.GetFileName(savePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download Fabric Server: {ex.Message}");
            }
        }

        // ------------------------ ↓ Forge Updater Funcs ↓ ------------------------

        private static async Task CheckAndUpdateForgeAsync(string downloadDirectory, string? selectedVersion = null)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Fetch the promotions JSON
                    string json = await client.GetStringAsync($"{ForgeApiVersionsUrl}promotions_slim.json");
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

                    // Ensure the download directory exists
                    if (!Directory.Exists(downloadDirectory))
                    {
                        Directory.CreateDirectory(downloadDirectory);
                    }

                    // Process versions
                    if (selectedVersion != null)
                    {
                        // If a specific version is provided, only process that version
                        if (stableVersions.Contains(selectedVersion))
                        {
                            await ProcessVersion(client, downloadDirectory, promos, selectedVersion);
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
                            await ProcessVersion(client, downloadDirectory, promos, version);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        private static async Task ProcessVersion(HttpClient client, string downloadDirectory, JObject promos, string version)
        {
            string build = promos[$"{version}-latest"].ToString();
            string actualDownloadUrl = $"{ForgeApiBaseUrl}{version}-{build}/forge-{version}-{build}-installer.jar";

            string fileName = $"forge-{version}-{build}.jar"; // File name without "installer"
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
                        await DownloadForgeServerJarAsync(client, actualDownloadUrl, destinationPath);
                        Console.WriteLine($"Downloaded {fileName}");
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

                // Download the new file
                await DownloadForgeServerJarAsync(client, actualDownloadUrl, destinationPath);
                Console.WriteLine($"Downloaded {fileName}");
            }
        }

        private static async Task DownloadForgeServerJarAsync(HttpClient client, string url, string destinationPath)
        {
            //Console.WriteLine("URL: " + url);
            try
            {
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode(); // Ensure the request was successful

                    using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                    {
                        using (FileStream streamToWriteTo = File.Open(destinationPath, FileMode.Create))
                        {
                            await streamToReadFrom.CopyToAsync(streamToWriteTo);
                            await streamToWriteTo.FlushAsync(); // Ensure all data is written to disk
                        }
                    }
                }
                Console.WriteLine($"Successfully downloaded to {destinationPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download {url}: {ex.Message}");
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath); // Delete the incomplete file
                }
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
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Fetch the list of available Minecraft versions
                    string versionsXml = await client.GetStringAsync(NeoForgeVersionsUrl);
                    XDocument versionsDoc = XDocument.Parse(versionsXml);

                    // Extract version numbers from the XML
                    var versions = versionsDoc.Descendants("version")
                                              .Select(v => v.Value)
                                              .Where(v => IsStableVersion(v)) // Filter out unstable versions
                                              .ToList();

                    // Ensure the download directory exists
                    if (!Directory.Exists(downloadDirectory))
                    {
                        Directory.CreateDirectory(downloadDirectory);
                    }

                    // Process versions
                    if (selectedVersion != null)
                    {
                        // If a specific version is provided, only process that version
                        if (versions.Contains(selectedVersion))
                        {
                            await ProcessNeoForgeVersion(client, downloadDirectory, selectedVersion);
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
                            await ProcessNeoForgeVersion(client, downloadDirectory, version);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        private static bool IsStableVersion(string version)
        {
            // Filter out versions with "-pre", "-rc", or snapshot-like versions (e.g., "25w09b")
            return !version.Contains("-pre") && !version.Contains("-rc") && !version.Contains("w");
        }

        private static async Task ProcessNeoForgeVersion(HttpClient client, string downloadDirectory, string version)
        {
            try
            {
                // Fetch the version info JSON
                string versionInfoUrl = string.Format(NeoForgeVersionInfoUrl, version);
                string versionInfoJson = await client.GetStringAsync(versionInfoUrl);
                JObject versionInfo = JObject.Parse(versionInfoJson);

                // Get the server download URL
                string serverUrl = versionInfo["downloads"]?["server"]?["url"]?.ToString();

                if (!string.IsNullOrEmpty(serverUrl))
                {
                    // Define the destination file path with the new filename pattern
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
                        await DownloadNeoForgeServerFileAsync(client, serverUrl, destinationPath);
                        Console.WriteLine($"Downloaded {fileName}");
                        //Console.WriteLine($"Downloaded {fileName} to {downloadDirectory}");
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

        private static async Task DownloadNeoForgeServerFileAsync(HttpClient client, string url, string destinationPath)
        {
            //Console.WriteLine("URL: " + url);
            try
            {
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode(); // Ensure the request was successful

                    using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                    {
                        using (FileStream streamToWriteTo = File.Open(destinationPath, FileMode.Create))
                        {
                            await streamToReadFrom.CopyToAsync(streamToWriteTo);
                            await streamToWriteTo.FlushAsync(); // Ensure all data is written to disk
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download {url}: {ex.Message}");
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath); // Delete the incomplete file
                }
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

                //Console.WriteLine($"{latestVersion}");

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
                    await DownloadQuiltServerJarAsync(jarUrl, localFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching the latest Quilt version: {ex.Message}");
            }
        }

        private static async Task DownloadQuiltServerJarAsync(string jarUrl, string savePath)
        {
            try
            {
                using var jarResponse = await HttpClient.GetStreamAsync(jarUrl);
                using var fileStream = File.Create(savePath);
                await jarResponse.CopyToAsync(fileStream);

                Console.WriteLine($"Fabric Server downloaded successfully: {Path.GetFileName(savePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download Fabric Server: {ex.Message}");
            }
        }

        // ------------------------ ↓ Purpur Updater Funcs ↓ ------------------------
        private static async Task CheckAndUpdatePurpurAsync(string downloadDirectory, string? selectedVersion = null)
        {
            Console.WriteLine($"Checking Purpur versions in: {downloadDirectory}");
            Directory.CreateDirectory(downloadDirectory);

            // Get local versions and builds
            var localFiles = Directory.GetFiles(downloadDirectory, "purpur-*.jar");

            Console.WriteLine("Local files detected:");

            var localVersions = new Dictionary<string, int>();

            foreach (var file in localFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                //Console.WriteLine($"Processing local file: {fileName}");

                var parts = fileName.Replace("purpur-", "").Split('-');
                if (parts.Length == 1) // Handle filenames without build numbers
                {
                    string version = parts[0];
                    localVersions[version] = 0; // Assume build number is 0 or handle as needed
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
                //Console.WriteLine($"Processing version {version}...");

                // Fetch the latest build for the version
                int? latestBuild = await GetPurpurLatestBuildAsync(version);

                if (latestBuild == null)
                {
                    Console.WriteLine($"Skipping {version} because no latest build was found.");
                    continue;
                }

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
                    await DownloadPurpurVersionAsync(version, downloadDirectory, latestBuild.Value);
                }
                else
                {
                    // Compare builds
                    int currentBuild = localVersions[version];
                    //Console.WriteLine($"Local build for {version}: {currentBuild}, Latest build: {latestBuild}");

                    if (currentBuild < latestBuild)
                    {
                        Console.WriteLine($"Purpur {version} is outdated. Updating...");
                        string oldFile = Path.Combine(downloadDirectory, $"purpur-{version}-{currentBuild}.jar");
                        if (File.Exists(oldFile))
                        {
                            Console.WriteLine($"Deleting old version: {oldFile}");
                            File.Delete(oldFile);
                        }
                        await DownloadPurpurVersionAsync(version, downloadDirectory, latestBuild.Value);
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
                //Console.WriteLine("Fetching available Purpur versions...");
                var response = await HttpClient.GetStringAsync(PurpurApiBaseUrl);

                using var jsonDoc = JsonDocument.Parse(response);
                var versions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray().Select(e => e.GetString()).ToArray();

                if (selectedVersion != null)
                {
                    versions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray().Where(v => v.GetString() == $"{selectedVersion}").Select(e => e.GetString()).ToArray();
                }
                Console.WriteLine($"API Response: ");
                foreach (var version in versions)
                {
                    Console.WriteLine($"Detected version: {version}");
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
                //Console.WriteLine($"API Response for {version}: {response}");

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

        private static async Task DownloadPurpurVersionAsync(string version, string downloadDirectory, int latestBuild)
        {
            try
            {
                string url = $"{PurpurApiBaseUrl}{version}/latest/download";
                string tempFilePath = Path.Combine(downloadDirectory, $"purpur-{version}.jar");
                string finalFilePath = Path.Combine(downloadDirectory, $"purpur-{version}-{latestBuild}.jar");

                using (var response = await HttpClient.GetStreamAsync(url))
                {
                    using (var fileStream = File.Create(tempFilePath))
                    {
                        await response.CopyToAsync(fileStream);
                    }
                }

                if (!File.Exists(finalFilePath))
                {
                    try
                    {
                        File.Move(tempFilePath, finalFilePath);
                        //Console.WriteLine($"Successfully renamed Purpur {version} to {finalFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to rename Purpur {version}: {ex.Message}");
                        // Clean up both files if move fails
                        if (File.Exists(finalFilePath))
                            File.Delete(finalFilePath);
                        File.Delete(tempFilePath);
                    }
                }
                Console.WriteLine($"Downloaded Purpur {version} successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download Purpur {version}: {ex.Message}");
            }
        }

        // ------------------------ ↓ Spigot Updater Funcs ↓ ------------------------

        // ------------------------ ↓ Main Updater Funcs ↓ ------------------------

        public static async Task Update(string serverVersionsPath)
        {
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine("Starting versions updater for all softwares.");
            Console.WriteLine("--------------------------------------------");

            object[,] softwareTypes = {
                { "Vanilla", "Forge", "NeoForge", "Fabric", "Quilt", "Purpur", "Spigot" },
            };

            foreach (string software in softwareTypes)
            {
                string versionDirectory = Path.Combine(serverVersionsPath, software);
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

            await RunUpdaterForSoftware(versionDirectory, software, version, true);

            Console.WriteLine("-------------------------");
            Console.WriteLine("All versions are updated!");
            Console.WriteLine("-------------------------");
        }

        // ------------------------ ↓ Main Updater Funcs Helpers ↓ ------------------------

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

            else if (software == "Spigot")
            {
                // TODO
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

                    Console.WriteLine("All contents deleted successfully, but the directory itself remains.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}