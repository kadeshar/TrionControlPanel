using System.Collections.Concurrent;
using TrionControlPanel.API.Classes.Cryptography;
using TrionControlPanel.API.Classes.Lists;

namespace TrionControlPanel.API.Classes
{
    // FileManager class for handling file-related operations.
    public static class FileManager
    {
        // Gets the version from the specified file location.
        public static string GetVersion(string location)
        {
            // First, check if the provided path from the config is empty.
            if (string.IsNullOrEmpty(location))
            {
                // This is not an error, it's just not configured.
                return "N/A";
            }

            // A special case you already had.
            if (location == "N/A")
            {
                return "N/A";
            }

            try
            {
                // If the path exists in config, try to read it.
                return File.ReadAllText(location);
            }
            catch (FileNotFoundException)
            {
                // Log a more specific error!
                TrionLogger.Log($"Configuration Error: The version file was not found at the specified path: {location}", "ERROR");
                return "N/A";
            }
            catch (Exception ex)
            {
                // Catch other potential errors like permission issues.
                TrionLogger.Log($"Error getting version from file '{location}': {ex.Message}", "ERROR");
                return "N/A";
            }
        }

        // Asynchronously gets a list of files from the specified file path.
        public static async Task<ConcurrentBag<FileList>> GetFilesAsync(string filePath,bool install)
        {
            Console.WriteLine($"Loading all files from {filePath}");
            var filePaths = Directory.GetFiles(filePath, "*", SearchOption.AllDirectories);
            var fileList = new ConcurrentBag<FileList>();
            var batchSize = 1000; // Adjust based on your system's capabilities

            // Semaphore to limit the number of concurrent operations
            var semaphore = new SemaphoreSlim(10); // Limit to 10 concurrent tasks, adjust as needed

            var tasks = new List<Task>();

            for (int i = 0; i < filePaths.Length; i += batchSize)
            {
                var batch = filePaths.Skip(i).Take(batchSize).ToArray();

                var task = Task.Run(async () =>
                {
                    await semaphore.WaitAsync(); // Limit concurrency
                    try
                    {
                        if (install)
                        {
                            foreach (var file in batch)
                            {
                                var fileInfo = new FileInfo(file);
                                var fileData = new FileList
                                {
                                    Name = fileInfo.Name,
                                    Size = fileInfo.Length / 1_000_000.0, // Size in MB
                                    
                                    Path = fileInfo.DirectoryName?.Replace(@"\", "/") // Optional: Normalize path
                                };
                                fileList.Add(fileData);
                            }
                        }
                        else
                        {
                            foreach (var file in batch)
                            {
                                var fileInfo = new FileInfo(file);
                                var fileData = new FileList
                                {
                                    Name = fileInfo.Name,
                                    Size = fileInfo.Length / 1_000_000.0, // Size in MB
                                    Hash = await EncryptManager.GetMd5HashFromFileAsync(file), // Async hash calculation
                                    Path = fileInfo.DirectoryName?.Replace(@"\", "/") // Optional: Normalize path
                                };
                                fileList.Add(fileData);
                            }
                       }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file batch: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release(); // Release the semaphore after processing
                    }
                });

                tasks.Add(task);
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            Console.WriteLine("Done");
            return fileList;
        }
    }
}
