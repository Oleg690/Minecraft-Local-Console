using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace databaseChanger
{
    class dbChanger
    {
        public static List<object[]> GetSpecificDataFunc(string sqlQuery)
        {
            List<object[]> data = [];

            string dbPath = @"D:\Minecraft-Server\important funcs for main aplication\Create Server Func\Create Server Func\database\worlds.db";
            string connectionString = $"Data Source={dbPath};Version=3;";

            using (SQLiteConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();

                    using (SQLiteCommand selectCommand = new(sqlQuery, connection))
                    using (SQLiteDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object[] row = new object[reader.FieldCount];
                            reader.GetValues(row);
                            data.Add(row);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            }
            return data;
        }

        public static List<object[]> GetFunc(string worldNumber, bool verificator = false)
        {
            List<object[]> data = [];

            string dbPath = @"D:\Minecraft-Server\important funcs for main aplication\Create Server Func\Create Server Func\database\worlds.db";
            string connectionString = $"Data Source={dbPath};Version=3;";

            using (SQLiteConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();

                    string selectQuery = $"SELECT id, worldNumber, name, version, totalPlayers FROM worlds WHERE worldNumber = {worldNumber};";

                    if (verificator)
                    {
                        selectQuery = $"SELECT id, worldNumber, name, version, totalPlayers, rconPassword FROM worlds WHERE worldNumber = {worldNumber};";
                    }

                    using (SQLiteCommand selectCommand = new(selectQuery, connection))
                    using (SQLiteDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object[] row = new object[reader.FieldCount];
                            reader.GetValues(row);
                            data.Add(row);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                
            }
            return data;
        }
        public static void SetFunc(string worldNumber, string worldName, string Software, string version, string totalPlayers, string rconPassword)
        {
            // Specify the full path for the database file
            string dbPath = @"D:\Minecraft-Server\important funcs for main aplication\Create Server Func\Create Server Func\database\worlds.db";

            // Ensure the directory exists
            string directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Specify the database file
            string connectionString = $"Data Source={dbPath};Version=3;";

            // Establish connection
            using (SQLiteConnection connection = new(connectionString))
            {
                try
                {
                    // Open the connection
                    connection.Open();

                    // Create a command
                    string query = $"insert into worlds (worldNumber, name, version, software, totalPlayers, rconPassword) values('{worldNumber}', '{worldName}', '{version}', '{Software}', '{totalPlayers}', '{rconPassword}');";
                    // Insert Data
                    using (SQLiteCommand command = new(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    connection.Close();
                }
            }
            Console.WriteLine("Done!");
        }

        public static void deleteWorldFromDB(string worldNumber)
        {
            // Specify the full path for the database file
            string dbPath = @"D:\Minecraft-Server\important funcs for main aplication\Create Server Func\Create Server Func\database\worlds.db";

            // Ensure the directory exists
            string directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Specify the database file
            string connectionString = $"Data Source={dbPath};Version=3;";

            // Establish connection
            using (SQLiteConnection connection = new(connectionString))
            {
                try
                {
                    // Open the connection
                    connection.Open();

                    // Create a command
                    string query = $"DELETE FROM worlds WHERE worldNumber = '{worldNumber}';";
                    // Insert Data
                    using (SQLiteCommand command = new(query, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("World deleted from DB!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    connection.Close();
                }
            }
            Console.WriteLine("Done!");
        }
    }
}
