using System.Data.SQLite;
using Logger;

namespace databaseChanger
{
    class dbChanger
    {
        public static readonly string? currentDirectory = Directory.GetCurrentDirectory();
        public static readonly string? dbPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory))) + "\\database\\worlds.db";
        public static readonly string? connectionString = $"Data Source={dbPath};Version=3;";
        public static void CreateDB(string dbName, bool insertOneDefaultSQLVerificator = false)
        {
            // Ensure the directory exists
            string? directory = Path.GetDirectoryName(dbPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                CodeLogger.ConsoleLog($"Directory created: {directory}");
            }

            // Establish connection
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    // Open the connection
                    connection.Open();
                    CodeLogger.ConsoleLog("Connected to the database.");

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
                        CodeLogger.ConsoleLog("Table created successfully.");
                    }

                    // Insert data
                    string insertDefaultSQL = $"insert into {dbName} (worldNumber, name, version, totalPlayers, rconPassword) values('123456789', 'Minecraft SMP', '1.21', '20', '123456789123456789');";
                    if (insertOneDefaultSQLVerificator != false)
                    {
                        using (SQLiteCommand insertCommand = new SQLiteCommand(insertDefaultSQL, connection))
                        {
                            insertCommand.ExecuteNonQuery();
                            CodeLogger.ConsoleLog("Data inserted successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    CodeLogger.ConsoleLog($"Error: {ex.Message}");
                }
            }
            CodeLogger.ConsoleLog("Done!");
        }

        public static List<object[]> SpecificDataFunc(string sqlQuery)
        {
            List<object[]> data = new List<object[]>();

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
                    CodeLogger.ConsoleLog(ex.ToString());
                }

            }
            return data;
        }

        public static List<object[]> GetFunc(string worldNumber, bool verificator = false)
        {
            List<object[]> data = new List<object[]>();

            using (SQLiteConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();

                    string selectQuery = $"SELECT id, worldNumber, name, version, software, totalPlayers FROM worlds WHERE worldNumber = {worldNumber};";

                    if (verificator)
                    {
                        selectQuery = $"SELECT id, worldNumber, name, version, software, totalPlayers, rconPassword FROM worlds WHERE worldNumber = {worldNumber};";
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
                    CodeLogger.ConsoleLog(ex.ToString());
                }

            }
            return data;
        }

        public static void SetFunc(string worldNumber, string worldName, string Software, string version, string totalPlayers, string rconPassword)
        {
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
                    CodeLogger.ConsoleLog($"Error: {ex.Message}");
                }
                finally
                {
                    connection.Close();
                }
            }
            CodeLogger.ConsoleLog("Data set succeasfully to database!");
        }

        public static void DeleteWorldFromDB(string worldNumber)
        {
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
                        CodeLogger.ConsoleLog("World deleted from DB!");
                    }
                }
                catch (Exception ex)
                {
                    CodeLogger.ConsoleLog($"Error: {ex.Message}");
                }
                finally
                {
                    connection.Close();
                }
            }
        }
    }
}
