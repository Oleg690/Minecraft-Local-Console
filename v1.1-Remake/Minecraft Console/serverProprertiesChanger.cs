using Logger;
using System.IO;

namespace serverPropriertiesChanger
{
    class DataChanger
    {
        // Function to get the information from a file
        public static string GetInfo(string filePath)
        {
            string data = "";

            try
            {
                // Ensure the file exists before attempting to read it
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("The specified file does not exist.");
                }

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using StreamReader reader = new(filePath);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    data += line + "\n";
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error reading the file: {ex.Message}");
                return string.Empty;
            }

            return data;
        }

        // Function to set information in a file
        public static void SetInfo(object[,] settings, string filePath, bool hardWrite = false)
        {
            try
            {
                // Validate if the file exists
                if (!File.Exists(filePath))
                {
                    CodeLogger.ConsoleLog("filePath: " + filePath);
                    throw new FileNotFoundException("The specified file does not exist.");
                }

                List<string> lines = [.. File.ReadAllLines(filePath)];
                // Process each setting to modify the file content
                for (int i = 0; i < settings.GetLength(0); i++)
                {
                    try
                    {
                        string settingKey = settings[i, 0]?.ToString() ?? throw new ArgumentNullException(nameof(settings), "Setting key cannot be null");
                        int lineNumber = FindLineNumber(settingKey, lines);

                        if (hardWrite && lineNumber == -1)
                        {
                            File.AppendAllText(filePath, $"{settingKey}={settings[i, 1]}" + Environment.NewLine);
                            lines = [.. File.ReadAllLines(filePath)];
                            lineNumber = FindLineNumber(settingKey, lines);
                            CodeLogger.ConsoleLog($"Created line with number {lineNumber} with the content {settingKey}={settings[i, 1]};");
                        }
                        else
                        {
                            string[] lineContents = lines[lineNumber].Split("=");

                            // Validate line number
                            if (lineNumber > lines.Count)
                            {
                                throw new FormatException($"Line {lineNumber + 3} out of bound.");
                            }
                            else if (lineNumber < 0)
                            {
                                throw new FormatException($"No line found for the content nedeed.");
                            }

                            // Validate line format (contains '=')
                            if (lineContents.Length < 2)
                            {
                                throw new FormatException($"Line {lineNumber + 3} does not contain an '=' separator.");
                            }

                            string oldContent = lines[lineNumber]; // For debugging
                            string newContent = $"{lineContents[0]}={settings[i, 1]}";
                            lines[lineNumber] = newContent;

                            CodeLogger.ConsoleLog($"Line {lineNumber + 3} was changed from: {oldContent}; to: {newContent};");
                        }
                    }
                    catch (Exception ex)
                    {
                        CodeLogger.ConsoleLog($"Error at line {i}: {ex.Message}");
                    }
                }

                File.WriteAllLines(filePath, lines);

                CodeLogger.ConsoleLog($"Lines updated succeasfully!");
            }
            catch (Exception ex)
            {
                // Handle general errors (file not found, reading/writing errors)
                CodeLogger.ConsoleLog($"Error writing to the file: {ex.Message}");
            }
        }

        // -------------------------------- Help Functions --------------------------------

        private static int FindLineNumber(string textToFind, List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string[] line = lines[i].Split('=');
                if (line[0] == textToFind)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}