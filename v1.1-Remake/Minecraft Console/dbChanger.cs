using Logger;
using System.Data.SQLite;
using System.IO;

namespace Minecraft_Console { 
    class dbChanger
    {
        private static readonly string? currentDirectory = Directory.GetCurrentDirectory();
        private static readonly string? dbPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory))) + "\\database\\worlds.db";
        private static readonly string? connectionString = $"Data Source={dbPath};Version=3;";

        // Define column names as constants
        private const string TableName = "worlds";
        private const string ColumnId = "id";
        private const string ColumnWorldNumber = "worldNumber";
        private const string ColumnName = "name";
        private const string ColumnVersion = "version";
        private const string ColumnSoftware = "software";
        private const string ColumnTotalPlayers = "totalPlayers";
        private const string ColumnServerPort = "Server_Port";
        private const string ColumnJmxPort = "JMX_Port";
        private const string ColumnRconPort = "RCON_Port";
        private const string ColumnRmiPort = "RMI_Port";
        private const string ColumnRconPassword = "rconPassword";
        private const string ColumnServerUser = "serverUser";
        private const string ColumnServerTempPsw = "serverTempPsw";
        private const string ColumnProcessId = "Process_ID";

        private static SQLiteConnection? CreateConnection()
        {
            SQLiteConnection connection = new(connectionString);
            try
            {
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error opening database connection: {ex.Message}");
                // Consider throwing the exception or returning null depending on your error handling strategy
                return null;
            }
        }

        public static void CreateDB(string dbName, bool insertOneDefaultSQLVerificator = false)
        {
            if (!Directory.Exists(Path.GetDirectoryName(dbPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
                CodeLogger.ConsoleLog($"Directory created: {Path.GetDirectoryName(dbPath)}");
            }

            using SQLiteConnection? connection = CreateConnection();
            if (connection == null) return; // Exit if connection failed

            try
            {
                string query = $"CREATE TABLE IF NOT EXISTS {dbName} (" +
                               $"{ColumnId} integer primary key autoincrement," +
                               $"{ColumnWorldNumber} text," +
                               $"{ColumnName} text," +
                               $"{ColumnVersion} text," +
                               $"{ColumnSoftware} text," +
                               $"{ColumnTotalPlayers} text," +
                               $"{ColumnServerPort} text," +
                               $"{ColumnJmxPort} text," +
                               $"{ColumnRconPort} text," +
                               $"{ColumnRmiPort} text," +
                               $"{ColumnRconPassword} text," +
                               $"{ColumnServerUser} text," +
                               $"{ColumnServerTempPsw} text," +
                               $"{ColumnProcessId} text" +
                               $")";
                using SQLiteCommand command = new(query, connection);
                command.ExecuteNonQuery();
                CodeLogger.ConsoleLog("Table created successfully.");

                if (insertOneDefaultSQLVerificator)
                {
                    string insertDefaultSQL = $"INSERT INTO {dbName} ({ColumnWorldNumber}, {ColumnName}, {ColumnVersion}, {ColumnSoftware}, {ColumnTotalPlayers}, {ColumnRconPassword}, {ColumnProcessId}) " +
                                            $"VALUES (@worldNumber, @name, @version, @software, @totalPlayers, @rconPassword, @processId);";
                    using SQLiteCommand insertCommand = new(insertDefaultSQL, connection);
                    insertCommand.Parameters.AddWithValue("@worldNumber", "123456789");
                    insertCommand.Parameters.AddWithValue("@name", "Minecraft SMP");
                    insertCommand.Parameters.AddWithValue("@version", "1.21");
                    insertCommand.Parameters.AddWithValue("@software", "Vanilla");
                    insertCommand.Parameters.AddWithValue("@totalPlayers", "20");
                    insertCommand.Parameters.AddWithValue("@rconPassword", "123456789123456789");
                    insertCommand.Parameters.AddWithValue("@processId", DBNull.Value); // Or null if your DB allows it directly
                    insertCommand.ExecuteNonQuery();
                    CodeLogger.ConsoleLog("Default data inserted successfully.");
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error during database creation: {ex.Message}");
            }
            finally
            {
                connection?.Close();
            }
            CodeLogger.ConsoleLog("Database creation process completed.");
        }

        public static List<object[]> SpecificDataFunc(string sqlQuery)
        {
            List<object[]> data = [];

            if (!File.Exists(dbPath))
            {
                CodeLogger.ConsoleLog("Database file does not exist. Creating default 'worlds' table...");
                CreateDB("worlds");
            }

            using SQLiteConnection? connection = CreateConnection();
            if (connection == null) return data; // Exit if connection failed

            try
            {
                using SQLiteCommand selectCommand = new(sqlQuery, connection);
                using SQLiteDataReader reader = selectCommand.ExecuteReader();
                while (reader.Read())
                {
                    object[] row = new object[reader.FieldCount];
                    reader.GetValues(row);
                    data.Add(row);
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error executing specific query: {ex}");
            }
            finally
            {
                connection?.Close();
            }
            return data;
        }

        public static List<object[]> GetFunc(string worldNumber, bool verificator = false)
        {
            List<object[]> data = [];

            if (!File.Exists(dbPath))
            {
                CodeLogger.ConsoleLog("Database file does not exist. Creating default 'worlds' table...");
                CreateDB("worlds");
            }

            using SQLiteConnection? connection = CreateConnection();
            if (connection == null) return data; // Exit if connection failed

            try
            {
                string selectQuery = $"SELECT {(verificator ? "*" : $"{ColumnId}, {ColumnWorldNumber}, {ColumnName}, {ColumnVersion}, {ColumnSoftware}, {ColumnTotalPlayers}, {ColumnServerPort}, {ColumnJmxPort}, {ColumnRconPort}, {ColumnRmiPort}")} " +
                                     $"FROM {TableName} WHERE {ColumnWorldNumber} = @worldNumber;";

                using SQLiteCommand selectCommand = new(selectQuery, connection);
                selectCommand.Parameters.AddWithValue("@worldNumber", worldNumber);
                using SQLiteDataReader reader = selectCommand.ExecuteReader();
                while (reader.Read())
                {
                    object[] row = new object[reader.FieldCount];
                    reader.GetValues(row);
                    data.Add(row);
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error retrieving world data: {ex}");
            }
            finally
            {
                connection?.Close();
            }
            return data;
        }

        public static void SetFunc(string worldNumber, string worldName, string Software, string version, string totalPlayers, string Server_Port, string JMX_Port, string RCON_Port, string RMI_Port, string rconPassword)
        {
            if (!File.Exists(dbPath))
            {
                CodeLogger.ConsoleLog("Database file does not exist. Creating default 'worlds' table...");
                CreateDB("worlds");
            }

            using SQLiteConnection? connection = CreateConnection();
            if (connection == null) return; // Exit if connection failed

            try
            {
                string query = $"INSERT INTO {TableName} ({ColumnWorldNumber}, {ColumnName}, {ColumnVersion}, {ColumnSoftware}, {ColumnTotalPlayers}, {ColumnServerPort}, {ColumnJmxPort}, {ColumnRconPort}, {ColumnRmiPort}, {ColumnRconPassword}) " +
                               $"VALUES (@worldNumber, @name, @version, @software, @totalPlayers, @serverPort, @jmxPort, @rconPort, @rmiPort, @rconPassword);";
                using SQLiteCommand command = new(query, connection);
                command.Parameters.AddWithValue("@worldNumber", worldNumber);
                command.Parameters.AddWithValue("@name", worldName);
                command.Parameters.AddWithValue("@version", version);
                command.Parameters.AddWithValue("@software", Software);
                command.Parameters.AddWithValue("@totalPlayers", totalPlayers);
                command.Parameters.AddWithValue("@serverPort", Server_Port);
                command.Parameters.AddWithValue("@jmxPort", JMX_Port);
                command.Parameters.AddWithValue("@rconPort", RCON_Port);
                command.Parameters.AddWithValue("@rmiPort", RMI_Port);
                command.Parameters.AddWithValue("@rconPassword", rconPassword);
                command.ExecuteNonQuery();
                CodeLogger.ConsoleLog("Data set successfully to the database!");
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error setting data in database: {ex.Message}");
            }
            finally
            {
                connection?.Close();
            }
        }

        public static void DeleteWorldFromDB(string worldNumber)
        {
            if (!File.Exists(dbPath))
            {
                CodeLogger.ConsoleLog("Database file does not exist. Creating default 'worlds' table...");
                CreateDB("worlds");
            }

            using SQLiteConnection? connection = CreateConnection();
            if (connection == null) return; // Exit if connection failed

            try
            {
                string query = $"DELETE FROM {TableName} WHERE {ColumnWorldNumber} = @worldNumber;";
                using SQLiteCommand command = new(query, connection);
                command.Parameters.AddWithValue("@worldNumber", worldNumber);
                command.ExecuteNonQuery();
                CodeLogger.ConsoleLog("World deleted from DB!");
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error deleting world from database: {ex.Message}");
            }
            finally
            {
                connection?.Close();
            }
        }
    }
}