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

namespace updater
{
    class VersionsUpdater
    {
        private static readonly string VanillaApiBaseUrl = "https://piston-meta.mojang.com/mc/game/version_manifest.json";
        private static readonly string ForgeApiBaseUrl = "";
        private static readonly string NeoForgeApiBaseUrl = "";
        private static readonly string FabricApiBaseUrl = "https://meta.fabricmc.net/v2/versions/installer";
        private static readonly string QuiltApiBaseUrl = "https://maven.quiltmc.org/repository/release/org/quiltmc/quilt-installer/";
        private static readonly string PurpurApiBaseUrl = "https://api.purpurmc.org/v2/purpur/";
        private static readonly string SpigotApiBaseUrl = "";

        private static readonly HttpClient HttpClient = new HttpClient();

        // ------------------------ ↓ Vanilla Updater Funcs ↓ ------------------------
        public static async Task CheckAndUpdateVanillaAsync(string downloadDirectory, string? selectedVersion = null)
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

        public static async Task CheckAndUpdateLatestFabricAsync(string downloadDirectory)
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
                        Console.WriteLine($"Downloading Fabric Universal Server Installer {loader.Key}...");
                        await DownloadFabricServerJarAsync(loader.Value, localFilePath);
                        DeleteFiles(downloadDirectory, false);
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

        // ------------------------ ↓ NeoForge Updater Funcs ↓ ------------------------

        // ------------------------ ↓ Forge Updater Funcs ↓ ------------------------

        // ------------------------ ↓ Quilt Updater Funcs ↓ ------------------------

        public static async Task CheckAndUpdateLatestQuiltAsync(string downloadDirectory)
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
                    Console.WriteLine($"Downloading Quilt Universal Server Installer {latestVersion}...");
                    string jarUrl = $"{QuiltApiBaseUrl}{latestVersion}/quilt-installer-{latestVersion}.jar";
                    await DownloadQuiltServerJarAsync(jarUrl, localFilePath);
                    DeleteFiles(downloadDirectory, false);
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
        public static async Task CheckAndUpdatePurpurAsync(string downloadDirectory, string? selectedVersion = null)
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
                { "Vanilla", "Forge", "NeoForge", "Fabric", "Quilt", "Purpur" },
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
                // TODO
            }
            else if (software == "NeoForge")
            {
                // TODO
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
    }
}