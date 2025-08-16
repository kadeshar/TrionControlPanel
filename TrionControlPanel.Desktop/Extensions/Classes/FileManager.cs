using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TrionControlPanel.Desktop.Extensions.Classes.Monitor;
using TrionControlPanel.Desktop.Extensions.Cryptography;
using TrionControlPanel.Desktop.Extensions.Modules.Lists;
using TrionControlPanelDesktop.Extensions.Modules;

namespace TrionControlPanel.Desktop.Extensions.Classes
{
    public class FileManager
    {
        /// <summary>
        /// Returns the substring of 'input' starting from the first occurrence of 'marker'.
        /// If 'marker' isn't found, returns the original string.
        /// Used for extracting relative paths or normalizing file locations.
        /// </summary>
        public static string StringBuilder(string input, string? marker)
        {
            int index = input.IndexOf(marker);
            return index >= 0 ? input.Substring(index) : input;
        }                                                                                                                                           

        /// <summary>
        /// Unzips a file asynchronously and reports progress, elapsed time, and speed.
        /// Reads the zip file entry by entry, writes each file directly to disk, and updates progress at intervals.
        /// </summary>
        public static async Task UnzipFileAsync(FileList file, string marker, CancellationToken cancellationToken,
            IProgress<double>? progress = null, IProgress<double>? elapsedTime = null, IProgress<double>? speed = null)
        {
            await Task.Run(async () =>
            {
                try
                {
                    string downloadPath = $"{Directory.GetCurrentDirectory()}{StringBuilder(file.Path, marker)}";
                    string zipFilePath = Path.Combine(downloadPath, file.Name);

                    if (!File.Exists(zipFilePath))
                    {
                        TrionLogger.Log("Zip file does not exist!", "ERROR");
                        return;
                    }

                    string finalExtractionPath = downloadPath;
                    if (!Directory.Exists(finalExtractionPath))
                        Directory.CreateDirectory(finalExtractionPath);

                    using var archive = ZipFile.OpenRead(zipFilePath);

                    long totalUncompressedSize = archive.Entries.Sum(entry => entry.Length);
                    long totalBytesExtracted = 0;
                    long previousBytesExtracted = 0;
                    DateTime startTime = DateTime.Now;
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Skip directories (entries with no file name)
                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        string destinationPath = Path.Combine(finalExtractionPath, entry.FullName);

                        // Make sure the directory for the file exists
                        string? destinationDir = Path.GetDirectoryName(destinationPath);
                        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                            Directory.CreateDirectory(destinationDir);

                        // Skip files with paths that are too long
                        if (destinationPath.Length > 2000)
                        {
                            TrionLogger.Log($"Destination path too long, skipping: {entry.FullName}", "WARNING");
                            continue;
                        }

                        // Extract the file in buffered chunks
                        using (Stream entryStream = entry.Open())
                        using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true))
                        {
                            byte[] buffer = new byte[2048];
                            int bytesRead;
                            while ((bytesRead = await entryStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                            {
                                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                                totalBytesExtracted += bytesRead;

                                // Every 100ms, report progress, elapsed time, and speed
                                if (stopwatch.ElapsedMilliseconds >= 100)
                                {
                                    double progressValue = totalUncompressedSize > 0 ? (double)totalBytesExtracted / totalUncompressedSize * 100 : 0;
                                    double elapsedTimeValue = (DateTime.Now - startTime).TotalSeconds;
                                    double speedValue = elapsedTimeValue > 0 ? (totalBytesExtracted - previousBytesExtracted) / 1024.0 / 1024.0 / (stopwatch.ElapsedMilliseconds / 1000.0) : 0;

                                    progress?.Report(progressValue);
                                    elapsedTime?.Report(elapsedTimeValue);
                                    speed?.Report(speedValue);

                                    previousBytesExtracted = totalBytesExtracted;
                                    stopwatch.Restart();
                                }
                            }
                            Array.Clear(buffer, 0, buffer.Length);
                        }

                        // Set file timestamps to match the archive
                        File.SetCreationTime(destinationPath, entry.LastWriteTime.DateTime);
                        File.SetLastWriteTime(destinationPath, entry.LastWriteTime.DateTime);
                    }

                    // Final report after extraction
                    double finalElapsedTimeValue = (DateTime.Now - startTime).TotalSeconds;
                    double finalSpeedValue = finalElapsedTimeValue > 0 ? totalBytesExtracted / 1024.0 / 1024.0 / finalElapsedTimeValue : 0;
                    progress?.Report(100.0);
                    elapsedTime?.Report(finalElapsedTimeValue);
                    speed?.Report(finalSpeedValue);

                    TrionLogger.Log($"Extraction completed successfully. Extracted {totalBytesExtracted / 1024.0 / 1024.0:F2} MB in {finalElapsedTimeValue:F2} seconds", "INFO");
                }
                catch (OperationCanceledException)
                {
                    TrionLogger.Log("Extraction was canceled.", "CANCELED");
                }
                catch (InvalidDataException ex)
                {
                    TrionLogger.Log($"Invalid zip file: {ex.Message}", "ERROR");
                }
                catch (IOException ex)
                {
                    TrionLogger.Log($"File I/O error during extraction: {ex.Message}", "ERROR");
                }
                catch (UnauthorizedAccessException ex)
                {
                    TrionLogger.Log($"Access denied during extraction: {ex.Message}", "ERROR");
                }
                catch (Exception ex)
                {
                    TrionLogger.Log($"An unexpected error occurred during extraction: {ex.Message}", "ERROR");
                }
            });
        }

        /// <summary>
        /// Downloads a file from the server asynchronously.
        /// Streams the file directly to disk in buffered chunks, reporting progress, elapsed time, and speed.
        /// </summary>
        public static async Task DownloadFileAsync(FileList file, string marker, string emulator, string key, CancellationToken cancellationToken,
     IProgress<double>? progress = null, IProgress<double>? elapsedTime = null, IProgress<double>? speed = null)
        {
            // It is highly recommended to reuse HttpClient instances. 
            // Consider making it static or using IHttpClientFactory for better performance and resource management.
            using HttpClient client = new();

            try
            {
                string url = Links.APIRequests.DownlaodFiles(emulator, key); // API base URL
                TrionLogger.Log($"Requesting: {url}", "INFO");

                // Ensure that the file path is valid
                string filePath = $"{file.Path}/{file.Name}";
                if (filePath.Length > 2000)
                {
                    TrionLogger.Log("File path is too long!", "ERROR");
                    return;
                }

                var requestObj = new { filePath };
                string jsonContent = JsonSerializer.Serialize(requestObj);
                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // *** CORRECTION IS HERE: Manually create HttpRequestMessage to use SendAsync ***
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                // Use SendAsync with HttpCompletionOption.ResponseHeadersRead
                // This is the key to preventing the large file from being buffered in memory.
                using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                response.EnsureSuccessStatusCode();

                string downloadPath = $"{Directory.GetCurrentDirectory()}{StringBuilder(file.Path, marker)}";
                Directory.CreateDirectory(downloadPath);
                string fileDownload = Path.Combine(downloadPath, file.Name);

                // Use a larger buffer (e.g., 80KB) for potentially better I/O performance.
                const int bufferSize = 81920;

                // The 'using' declarations can be simplified in modern C#
                await using var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = new FileStream(fileDownload, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);

                long totalBytesRead = 0;
                var buffer = new byte[bufferSize];
                int bytesRead;

                var stopwatch = Stopwatch.StartNew();
                long previousBytesRead = 0;

                while ((bytesRead = await downloadStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalBytesRead += bytesRead;

                    // Report progress at reasonable intervals to avoid excessive UI updates.
                    if (stopwatch.ElapsedMilliseconds > 250) // Report every 250ms
                    {
                        // Only calculate progress if the total size is known
                        if (response.Content.Headers.ContentLength.HasValue)
                        {
                            progress?.Report((double)totalBytesRead / response.Content.Headers.ContentLength.Value * 100);
                        }

                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                        // Calculate speed in MB/s since the last report
                        double speedValue = (totalBytesRead - previousBytesRead) / 1024.0 / 1024.0 / elapsedSeconds;
                        speed?.Report(speedValue);

                        elapsedTime?.Report((DateTime.Now - stopwatch.Elapsed).Second); // This seems incorrect, maybe you want total elapsed time

                        // Update for next calculation
                        previousBytesRead = totalBytesRead;
                        stopwatch.Restart();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                TrionLogger.Log("Download was canceled.", "CANCELED");
            }
            catch (HttpRequestException ex)
            {
                TrionLogger.Log($"HTTP request error: {ex.Message}", "ERROR");
            }
            catch (IOException ex)
            {
                TrionLogger.Log($"File I/O error: {ex.Message}", "ERROR");
            }
            catch (Exception ex)
            {
                TrionLogger.Log($"An unexpected error occurred: {ex.Message}", "ERROR");
            }
        }

        /// <summary>
        /// Deletes a list of files asynchronously.
        /// Each file is deleted in a background task to avoid blocking the main thread.
        /// </summary>
        public static async Task DeleteFiles(List<FileList> files)
        {
            foreach (var file in files)
            {
                await Task.Run(() => File.Delete($"{file.Path}/{file.Name}"));
            }
        }
        public static async Task DeleteFolderAsync(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, recursive: true);

                while (Directory.Exists(folderPath))
                {
                    await Task.Delay(50); // non-blocking, keeps UI responsive
                }
            }
        }

        public static async Task DeleteInstallFiles(List<FileList> files, string InstallPath)
        {
            string exePath = Directory.GetCurrentDirectory();
            foreach (var file in files)
            {
                TrionLogger.Log($@"{exePath}\{InstallPath}\{file.Name}");
                await Task.Run(() => File.Delete($@"{exePath}\{InstallPath}\{file.Name}"));
            }
        }
        /// <summary>
        /// Compares server and local files to find which files are missing locally and which should be deleted.
        /// Uses hash, name, and normalized path for comparison. Reports progress for both lists.
        /// </summary>
        public async static Task<(List<FileList> MissingFiles, List<FileList> FilesToDelete)>
            CompareFiles(List<FileList> ServerFiles, List<FileList> LocalFiles, string marker,
            IProgress<string> FileToDelete, IProgress<string> FileToDownload)
        {
            List<FileList> MissingFiles = new();
            List<FileList> FilesToDelete = new();

            // Find files present on the server but missing locally
            await Task.Run(() =>
            {
                foreach (var serverFile in ServerFiles)
                {
                    var localFile = LocalFiles.FirstOrDefault(file =>
                        file.Hash == serverFile.Hash &&
                        file.Name == serverFile.Name &&
                        StringBuilder(file.Path.Replace(@"\", "/"), marker) == StringBuilder(serverFile.Path, marker));
                    if (localFile == null)
                        MissingFiles.Add(serverFile);

                    FileToDownload?.Report($"{MissingFiles.Count}");
                }
            });

            // Find files present locally but missing on the server
            await Task.Run(() =>
            {
                foreach (var localFile in LocalFiles)
                {
                    var serverFile = ServerFiles.FirstOrDefault(file =>
                        file.Hash == localFile.Hash &&
                        file.Name == localFile.Name &&
                        StringBuilder(file.Path.Replace(@"\", "/"), marker) == StringBuilder(localFile.Path, marker));
                    if (serverFile == null)
                        FilesToDelete.Add(localFile);

                    FileToDelete?.Report($"{FilesToDelete.Count}");
                }
            });

            return (MissingFiles, FilesToDelete);
        }

        /// <summary>
        /// Scans a directory and its subdirectories, creating a FileList for each file.
        /// Calculates file hashes asynchronously and reports progress.
        /// Uses a semaphore to limit concurrency and avoid resource exhaustion.
        /// </summary>
        public static async Task<List<FileList>> ProcessFilesAsync(string filePath, IProgress<string>? progress, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

            var fileList = new List<FileList>();
            var semaphore = new SemaphoreSlim(1000); // Limit concurrent tasks
            var tasks = new List<Task>();
            int processedFiles = 0;
            var totalFiles = Directory.EnumerateFiles(filePath, "*", SearchOption.AllDirectories);

            foreach (var file in totalFiles)
            {
                processedFiles++;
                await semaphore.WaitAsync(cancellationToken);
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var fileData = new FileList
                        {
                            Name = fileInfo.Name,
                            Size = fileInfo.Length / 1_000.0,
                            Hash = await MD5FileHasah.GetMd5HashFromFileAsync(file),
                            Path = fileInfo.DirectoryName?.Replace(@"\", "/")!
                        };

                        lock (fileList)
                        {
                            fileList.Add(fileData);
                        }
                        progress?.Report($"{processedFiles} / {totalFiles.Count()}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);

                // Prevent too many pending tasks in memory
                if (tasks.Count >= 100)
                {
                    await Task.WhenAny(tasks);
                    tasks.RemoveAll(t => t.IsCompleted);
                }
            }

            await Task.WhenAll(tasks);

            return fileList;
        }

        /// <summary>
        /// Searches for an executable file in a directory and its subdirectories.
        /// Returns the full path if found, otherwise returns an empty string.
        /// </summary>
        public static string GetExecutableLocation(string location, string Executable)
        {
            if (Executable != null)
            {
                foreach (string f in Directory.EnumerateFiles(location, $"{Executable}.exe", SearchOption.AllDirectories))
                {
                    return f;
                }
            }
            return string.Empty;
        }
    }
}

