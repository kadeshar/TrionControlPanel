using Dapper;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.Text;
using TrionControlPanel.Desktop.Extensions.Classes.Monitor;
using TrionControlPanel.Desktop.Extensions.Modules.Lists;

namespace TrionControlPanel.Desktop.Extensions.Database
{
    // AccessManager class for handling database operations.
    public class AccessManager
    {
        /// <summary>
        /// Asynchronously executes a large SQL script file against a MySQL database,
        /// handling batching and providing detailed progress updates.
        /// </summary>
        public static async Task ExecuteSqlFileAsync(string filePath, string connectionString, CancellationToken cancellationToken,
            IProgress<double>? progress = null, IProgress<double>? elapsedTime = null, IProgress<double>? speed = null)
        {
            await Task.Run(async () =>
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    TrionLogger.Log($"SQL file not found: {filePath}", "ERROR");
                    return;
                }

                long totalBytes = fileInfo.Length;
                long totalBytesRead = 0;
                long previousBytesRead = 0;
                var startTime = DateTime.Now;
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    // Use await using for asynchronous disposal
                    await using var connection = new MySqlConnection(connectionString);
                    await connection.OpenAsync(cancellationToken);

                    // Open the file with a FileStream to get access to the Stream.Position for accurate progress
                    await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true);
                    using var reader = new StreamReader(fileStream, Encoding.UTF8);

                    var commandStringBuilder = new StringBuilder();
                    string? line;

                    while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Trim the line and check if it ends with the standard SQL delimiter
                        var trimmedLine = line.Trim();
                        commandStringBuilder.AppendLine(line);

                        if (trimmedLine.EndsWith(";"))
                        {
                            string commandText = commandStringBuilder.ToString();
                            commandStringBuilder.Clear();

                            if (!string.IsNullOrWhiteSpace(commandText))
                            {
                                TrionLogger.Log($"Executing SQL: {commandText.Substring(0, Math.Min(commandText.Length, 100))}...", "DEBUG");

                                // Execute the command asynchronously
                                await connection.ExecuteAsync(new CommandDefinition(commandText, cancellationToken: cancellationToken));
                            }
                        }

                        // Report progress at regular intervals
                        if (stopwatch.ElapsedMilliseconds >= 250) // Report every 250ms
                        {
                            totalBytesRead = fileStream.Position;
                            double progressValue = totalBytes > 0 ? (double)totalBytesRead / totalBytes * 100 : 0;
                            double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
                            double speedValue = elapsedSeconds > 0 ? (totalBytesRead - previousBytesRead) / 1024.0 / 1024.0 / (stopwatch.ElapsedMilliseconds / 1000.0) : 0;

                            progress?.Report(progressValue);
                            elapsedTime?.Report(elapsedSeconds);
                            speed?.Report(speedValue); // Speed of reading the SQL file in MB/s

                            previousBytesRead = totalBytesRead;
                            stopwatch.Restart();
                        }
                    }

                    // Execute any remaining command in the buffer
                    if (commandStringBuilder.Length > 0)
                    {
                        await connection.ExecuteAsync(new CommandDefinition(commandStringBuilder.ToString(), cancellationToken: cancellationToken));
                    }

                    // Final report
                    progress?.Report(100.0);
                    elapsedTime?.Report((DateTime.Now - startTime).TotalSeconds);
                    TrionLogger.Log($"SQL script execution completed successfully for {fileInfo.Name}.", "INFO");
                }
                catch (OperationCanceledException)
                {
                    TrionLogger.Log("SQL script execution was canceled.", "CANCELED");
                }
                catch (MySqlException ex)
                {
                    TrionLogger.Log($"Database error during script execution: {ex.Message}", "ERROR");
                    // You might want to re-throw or handle this more specifically
                }
                catch (IOException ex)
                {
                    TrionLogger.Log($"File I/O error during script execution: {ex.Message}", "ERROR");
                }
                catch (Exception ex)
                {
                    TrionLogger.Log($"An unexpected error occurred during script execution: {ex.Message}", "ERROR");
                }
            });
        }
        // Constructs a connection string for the specified database using the provided settings.
        public static string ConnectionString(AppSettings Settings)
        {
            return new($"Server={Settings.DBServerHost};Port={Settings.DBServerPort};User Id={Settings.DBServerUser};Password={Settings.DBServerPassword};");
        }
        public static string ConnectionString(AppSettings Settings, string Database)
        {
            return new($"Server={Settings.DBServerHost};Port={Settings.DBServerPort};User Id={Settings.DBServerUser};Password={Settings.DBServerPassword};Database={Database}");
        }

        // Tests the connection to the specified database using the provided settings.
        public static async Task<bool> ConnectionTest(AppSettings Settings, string Database)
        {
            using (MySqlConnection conn = new(ConnectionString(Settings, Database)))
            {
                try
                {
                    if (conn.State == ConnectionState.Closed)
                    {
                        await conn.OpenAsync();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    TrionLogger.Log($"Connection test failed: {ex.Message}", "ERROR");
                    return false;
                }
            }
            return false;
        }

        // Loads a list of data from the database using the specified SQL query and parameters.
        public static async Task<List<T>> LodaDataList<T, U>(string sql, U parameters, string connectionString)
        {
            using (IDbConnection con = new MySqlConnection(connectionString))
            {
                var rows = await con.QueryAsync<T>(sql, parameters);
                return rows.ToList();
            }
        }

        // Loads a single data item from the database using the specified SQL query and parameters.
        public static async Task<T> LoadDataType<T, U>(string sql, U parameters, string connectionString)
        {
            using (IDbConnection connectionNoList = new MySqlConnection(connectionString))
            {
                var rows = await connectionNoList.QuerySingleAsync<T>(sql, parameters);
                return rows;
            }
        }

        // Saves data to the database using the specified SQL query and parameters.
        public static async Task SaveData<T>(string sql, T parameters, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                TrionLogger.Log("Connection string cannot be null or empty.", "ERROR");
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }

            if (sql == null)
            {
                TrionLogger.Log("SQL query cannot be null", "ERROR");
                throw new ArgumentNullException(nameof(sql), "SQL query cannot be null.");
            }

            if (parameters == null)
            {
                TrionLogger.Log("Parameters cannot be null.", "ERROR");
                throw new ArgumentNullException(nameof(parameters), "Parameters cannot be null.");
            }

            using (IDbConnection connectionSave = new MySqlConnection(connectionString))
            {
                try
                {
                    await connectionSave.ExecuteAsync(sql, parameters);
                }
                catch (Exception ex)
                {
                    TrionLogger.Log($"Error occurred: {ex.Message}", "ERROR");
                    throw;
                }
            }
        }
    }
}
