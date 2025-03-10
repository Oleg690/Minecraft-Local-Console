using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Runtime.CompilerServices;

namespace Default_Database_Creator
{
    public class createDatabase
    {
        public static void createDB(bool insertOneDefaultSQLVerificator = false)
        {
            string dbName = "worlds";

            // Specify the full path for the database file
            string dbPath = @"D:\Minecraft-Server\important funcs for main aplication\Create Server Func\Create Server Func\database\worlds.db";

            // Ensure the directory exists
            string directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Console.WriteLine($"Directory created: {directory}");
            }

            // Specify the database file
            string connectionString = $"Data Source={dbPath};Version=3;";

            // Establish connection
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    // Open the connection
                    connection.Open();
                    Console.WriteLine("Connected to the database.");

                    // Create a command
                    string query = $"CREATE TABLE IF NOT EXISTS {dbName} (" +
                        $"id integer primary key autoincrement," +
                        $"worldNumber text," +
                        $"name text," +
                        $"version text," +
                        $"software text," +
                        $"totalPlayers text," +
                        $"rconPassword text" +
                        $")";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Table created successfully.");
                    }

                    // Insert data
                    string insertDefaultSQL = $"insert into {dbName} (worldNumber, name, version, totalPlayers, rconPassword) values('123456789', 'Minecraft SMP', '1.21', '20', '123456789123456789');";
                    if (insertOneDefaultSQLVerificator != false)
                    {
                        using (SQLiteCommand insertCommand = new SQLiteCommand(insertDefaultSQL, connection))
                        {
                            insertCommand.ExecuteNonQuery();
                            Console.WriteLine("Data inserted successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            Console.WriteLine("Done!");
        }
    }
}