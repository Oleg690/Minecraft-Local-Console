using System;
using System.Collections.Generic;
using System.IO;

namespace serverPropriertiesChanger
{
    class DataChanger
    {
        // Method to get the information from a file
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

                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        data += line + "\n";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading the file: {ex.Message}");
                return string.Empty;
            }

            return data;
        }

        // Method to set information in a file
        public static void SetInfo(object[,] settings, string filePath)
        {
            try
            {
                // Validate if the file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("The specified file does not exist.");
                }

                List<string> lines = new(File.ReadAllLines(filePath));

                // Process each setting to modify the file content
                for (int i = 0; i < settings.GetLength(0); i++)
                {
                    try
                    {
                        int lineNumber = Convert.ToInt32(settings[i, 0]) - 1;

                        // Validate line number range
                        if (lineNumber < 0 || lineNumber >= lines.Count)
                        {
                            throw new ArgumentOutOfRangeException($"Line number {lineNumber + 1} is out of bounds.");
                        }

                        string[] lineContents = lines[lineNumber].Split("=");

                        // Validate line format (contains '=')
                        if (lineContents.Length < 2)
                        {
                            throw new FormatException($"Line {lineNumber + 1} does not contain an '=' separator.");
                        }

                        string newContent = lineContents[0] + "=" + settings[i, 1].ToString();
                        lines[lineNumber] = newContent;
                    }
                    catch (Exception ex)
                    {
                        // Log error for specific line
                        Console.WriteLine($"Error at line {i + 1}: {ex.Message}");
                    }
                }

                // Write all lines back to the file after modification
                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                // Handle general errors (file not found, reading/writing errors)
                Console.WriteLine($"Error writing to the file: {ex.Message}");
            }
        }
    }
}
