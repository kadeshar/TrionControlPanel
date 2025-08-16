using Dapper;
using MySql.Data.MySqlClient;
using System.Data;
using TrionControlPanel.API.Classes; // Assuming TrionLogger is here

namespace TrionControlPanel.API.Classes.Database
{
    /// <summary>
    /// Manages all data access logic for the application using Dapper.
    /// Includes integrated logging, error handling, and retry mechanisms.
    /// </summary>
    public class AccessManager
    {
        private readonly string _connectionString;
        private const int MaxRetryAttempts = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

        public AccessManager(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Default connection string is not configured.");
        }

        #region Public Data Access Methods

        /// <summary>
        /// Asynchronously loads a list of data from the database.
        /// Returns an empty list if no records are found or an error occurs.
        /// </summary>
        /// <typeparam name="T">The type of object to map the data to.</typeparam>
        /// <typeparam name="U">The type of the parameters object.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>A list of objects of type T, or an empty list on failure.</returns>
        public async Task<List<T>> LoadDataAsync<T, U>(string sql, U parameters)
        {
            return await ExecuteWithRetryAsync(async (con) =>
            {
                var data = await con.QueryAsync<T>(sql, parameters);
                return data.ToList();
            }, Enumerable.Empty<T>().ToList()); // Return empty list on failure
        }

        /// <summary>
        /// Asynchronously loads a single record from the database.
        /// Returns the default value for T (e.g., null for reference types) if no record is found or an error occurs.
        /// </summary>
        /// <typeparam name="T">The type of object to map the data to.</typeparam>
        /// <typeparam name="U">The type of the parameters object.</typeparam>
        /// <param name="sql">The SQL query that should return one record.</param>
        /// <param name="parameters">The parameters for the SQL query.</param>
        /// <returns>A single object of type T, or default(T) on failure or if not found.</returns>
        public async Task<T?> LoadSingleOrDefaultAsync<T, U>(string sql, U parameters)
        {
            return await ExecuteWithRetryAsync(async (con) =>
            {
                return await con.QueryFirstOrDefaultAsync<T>(sql, parameters);
            }, default);
        }

        /// <summary>
        /// Asynchronously executes a command (e.g., INSERT, UPDATE, DELETE).
        /// </summary>
        /// <typeparam name="T">The type of the parameters object.</typeparam>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">The parameters for the SQL command.</param>
        /// <returns>The number of rows affected, or 0 on failure.</returns>
        public async Task<int> SaveDataAsync<T>(string sql, T parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                TrionLogger.Log("SaveDataAsync was called with a null or empty SQL query.", "ERROR");
                return 0;
            }

            return await ExecuteWithRetryAsync(async (con) =>
            {
                return await con.ExecuteAsync(sql, parameters);
            }, 0); // Return 0 affected rows on failure
        }

        #endregion

        #region Private Execution Wrapper

        /// <summary>
        /// A private helper that wraps database operations with connection management,
        /// logging, error handling, and retry logic.
        /// </summary>
        /// <param name="dbOperation">The actual database operation to perform.</param>
        /// <param name="defaultValueOnFailure">The value to return if all retry attempts fail.</param>
        /// <returns>The result of the database operation or the default value on failure.</returns>
        private async Task<T> ExecuteWithRetryAsync<T>(Func<IDbConnection, Task<T>> dbOperation, T defaultValueOnFailure)
        {
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    // Using a new connection for each attempt is crucial for retry logic.
                    // The connection pool handles the performance aspect.
                    using (IDbConnection con = new MySqlConnection(_connectionString))
                    {
                        TrionLogger.Log($"DB Operation Attempt {attempt}/{MaxRetryAttempts}: Opening connection...", "DEBUG");
                        await ((MySqlConnection)con).OpenAsync();

                        // Execute the actual Dapper call passed in as a function.
                        var result = await dbOperation(con);

                        TrionLogger.Log($"DB Operation Attempt {attempt} Succeeded.", "DEBUG");
                        return result;
                    }
                }
                catch (MySqlException ex)
                {
                    // Log detailed MySQL-specific errors.
                    TrionLogger.Log($"MySQL Exception on attempt {attempt}: {ex.Message}. (Number: {ex.Number})", "ERROR");
                    // Check if the error is a transient fault that is worth retrying.
                    if (IsTransient(ex) && attempt < MaxRetryAttempts)
                    {
                        await Task.Delay(RetryDelay);
                        continue; // Go to the next attempt
                    }
                    else
                    {
                        // Not a transient error or retries exhausted, so we fail.
                        break;
                    }
                }
                catch (TimeoutException ex)
                {
                    TrionLogger.Log($"Database command timeout on attempt {attempt}: {ex.Message}", "ERROR");
                    if (attempt < MaxRetryAttempts)
                    {
                        await Task.Delay(RetryDelay); // Wait before retrying on timeout
                    }
                }
                catch (Exception ex)
                {
                    // Catch any other unexpected exceptions.
                    TrionLogger.Log($"Unhandled Exception during DB operation on attempt {attempt}: {ex.Message}\n{ex.StackTrace}", "FATAL");
                    // It's usually not safe to retry on unknown exceptions, so we break.
                    break;
                }
            }

            TrionLogger.Log($"All {MaxRetryAttempts} DB attempts failed. Returning default value.", "ERROR");
            return defaultValueOnFailure;
        }

        /// <summary>
        /// Determines if a MySQL exception is likely due to a transient (temporary) fault.
        /// </summary>
        /// <param name="ex">The MySqlException.</param>
        /// <returns>True if the error is transient and worth retrying.</returns>
        private static bool IsTransient(MySqlException ex)
        {
            // Error numbers for lock wait timeout, deadlock found, and connection loss
            // are good candidates for retrying. You can add more as you identify them.
            return ex.Number == 1205 // Lock wait timeout
                || ex.Number == 1213 // Deadlock found
                || ex.Number == 1042 // Can't get hostname for your address (DNS/network issue)
                || ex.Number == 1043; // Bad handshake (transient network issue)
        }

        #endregion
    }
}