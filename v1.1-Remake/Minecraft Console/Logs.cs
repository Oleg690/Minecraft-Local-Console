using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Minecraft_Console
{
    public static class CodeLogger
    {
        public static readonly string rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))) ?? string.Empty;
        private static readonly string LogsPath = Path.Combine(rootFolder, "logs");
        private static readonly Random rng = new();

        public static void CreateLogFile(int maxLogFiles = 10)
        {
            if (LogsPath == null)
                throw new InvalidOperationException("LogsPath cannot be null.");

            if (!Directory.Exists(LogsPath))
                Directory.CreateDirectory(LogsPath);

            string latestLogPath = Path.Combine(LogsPath, "latest.log");

            if (File.Exists(latestLogPath))
            {
                FileInfo logInfo = new(latestLogPath);

                // If file is empty, don't create a new one
                if (logInfo.Length == 0)
                {
                    return;
                }

                string timestamp = DateTime.Now.ToString("yyyy-MMM-dd");
                string archivedLogName;
                string archivedLogPath;
                string gzipFilePath;

                do
                {
                    string randomNumber = rng.Next(1000, 9999).ToString();
                    archivedLogName = $"{timestamp}-{randomNumber}.log";
                    archivedLogPath = Path.Combine(LogsPath, archivedLogName);
                    gzipFilePath = archivedLogPath + ".gz";
                }
                while (File.Exists(gzipFilePath)); // Ensure unique filename

                File.Move(latestLogPath, archivedLogPath);

                // Create a gzip archive
                try
                {
                    using (FileStream originalFileStream = File.OpenRead(archivedLogPath))
                    using (FileStream compressedFileStream = File.Create(gzipFilePath))
                    using (GZipStream compressionStream = new(compressedFileStream, CompressionMode.Compress))
                    {
                        originalFileStream.CopyTo(compressionStream);
                    }

                    File.Delete(archivedLogPath);
                    //Console.WriteLine($"Log compressed: {gzipFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gzipping log file: {ex.Message}");
                    //if the gzip process fails, we will not delete the original log.
                }

                // Clean up old log files if more than the limit exists
                DeleteOldLogs(LogsPath, maxLogFiles);
            }

            // Create a fresh "latest.log" without content
            File.Create(latestLogPath).Close();
            //Console.WriteLine("New log file created: latest.log");
        }

        public static void DeleteOldLogs(string LogsPath, int maxLogFiles)
        {
            var logFiles = Directory.GetFiles(LogsPath, "*.gz")
                .Select(f => new FileInfo(f))
                .OrderByDescending(fi => fi.CreationTime)
                .Skip(maxLogFiles)
                .ToList();

            foreach (var file in logFiles)
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting old log file {file.FullName}: {ex.Message}");
                }
            }
        }

        public static void ConsoleLog(object message)
        {
            try
            {
                if (LogsPath != null)
                {
                    string timestamp = $"[{DateTime.Now:ddMMMyyyy HH:mm:ss.fff}] ";
                    string logLine = timestamp + message;
                    
                    File.AppendAllText(Path.Combine(LogsPath, "latest.log"), logLine + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}