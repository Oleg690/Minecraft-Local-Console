using com.sun.crypto.provider;
using com.sun.org.apache.bcel.@internal.classfile;
using jdk.nashorn.@internal.ir;
using Microsoft.Extensions.DependencyModel.Resolution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

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

        // Function to set information in a file
        public static void SetInfo(object[,] settings, string filePath, bool hardWrite = false)
        {
            try
            {
                // Validate if the file exists
                if (!File.Exists(filePath))
                {
                    Console.WriteLine("filePath: " + filePath);
                    throw new FileNotFoundException("The specified file does not exist.");
                }

                List<string> lines = new(File.ReadAllLines(filePath));
                // Process each setting to modify the file content
                for (int i = 0; i < settings.GetLength(0); i++)
                {
                    try
                    {
                        int lineNumber = FindLineNumber(settings[i, 0].ToString(), lines);

                        if (hardWrite && lineNumber == -1)
                        {
                            File.AppendAllText(filePath, $"{settings[i, 0].ToString()}={settings[i, 1].ToString()}" + Environment.NewLine);
                            lines = new(File.ReadAllLines(filePath));
                            lineNumber = FindLineNumber(settings[i, 0].ToString(), lines);
                            Console.WriteLine($"Created line with number {lineNumber} with the content {settings[i, 0].ToString()}={settings[i, 1].ToString()};");
                        }
                        else
                        {
                            string[] lineContents = lines[lineNumber].Split("=");

                            // Validate line number
                            if (lineNumber > lines.Count)
                            {
                                throw new FormatException($"Line {lineNumber + 1} out of bound.");
                            }
                            else if (lineNumber < 0)
                            {
                                throw new FormatException($"No line found for the content nedeed.");
                            }

                            // Validate line format (contains '=')
                            if (lineContents.Length < 2)
                            {
                                throw new FormatException($"Line {lineNumber + 1} does not contain an '=' separator.");
                            }

                            string oldContent = lines[lineNumber]; // For debugging
                            string newContent = lineContents[0] + "=" + settings[i, 1].ToString();
                            lines[lineNumber] = newContent;

                            Console.WriteLine($"Line {lineNumber + 1} was changed from: {oldContent}; to: {newContent};");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error at line {i}: {ex.Message}");
                    }
                }

                File.WriteAllLines(filePath, lines);

                Console.WriteLine($"Lines updated succeasfully!");
            }
            catch (Exception ex)
            {
                // Handle general errors (file not found, reading/writing errors)
                Console.WriteLine($"Error writing to the file: {ex.Message}");
            }
        }

        // -------------------------------- Help Functions --------------------------------

        private static int FindLineNumber(string textToFind, List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(textToFind))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
