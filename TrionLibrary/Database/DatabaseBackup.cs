using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TrionLibrary.Sys;

namespace TrionLibrary.Database
{
    public class DatabaseBackup
    {
        private const int SevenZipCompressionLevel = 7;

        public static bool IsBackupDue()
        {
            if (!Setting.Setting.List.BackupEnabled)
                return false;

            if (string.IsNullOrEmpty(Setting.Setting.List.BackupDatabaseLocation) ||
                Setting.Setting.List.BackupDatabaseLocation == "N/A")
                return false;

            if (string.IsNullOrEmpty(Setting.Setting.List.BackupFolder) ||
                Setting.Setting.List.BackupFolder == "N/A")
                return false;

            if (string.IsNullOrEmpty(Setting.Setting.List.LastBackupDate))
                return true;

            if (DateTime.TryParse(Setting.Setting.List.LastBackupDate, out DateTime lastBackup))
            {
                return (DateTime.Now - lastBackup).TotalDays >= Setting.Setting.List.BackupIntervalDays;
            }

            return true;
        }

        public static async Task<bool> BackupDatabaseFiles(string mysqlDataPath, string backupFolder)
        {
            try
            {
                mysqlDataPath = Path.GetFullPath(mysqlDataPath);
                backupFolder = Path.GetFullPath(backupFolder);

                if (!Directory.Exists(mysqlDataPath))
                {
                    Infos.Message = $"Backup failed: MySQL data directory not found: {mysqlDataPath}";
                    return false;
                }

                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string archiveFileName = $"Backup_{timestamp}.7z";
                string archiveFilePath = Path.Combine(backupFolder, archiveFileName);

                string sevenZipExe = FindSevenZipExecutable();
                if (!string.IsNullOrWhiteSpace(sevenZipExe))
                {
                    await CreateSevenZipArchive(mysqlDataPath, archiveFilePath, sevenZipExe);
                }
                else
                {
                    archiveFileName = $"Backup_{timestamp}.zip";
                    archiveFilePath = Path.Combine(backupFolder, archiveFileName);

                    await Task.Run(() =>
                    {
                        ZipFile.CreateFromDirectory(mysqlDataPath, archiveFilePath, CompressionLevel.Optimal, false);
                    });
                }

                Setting.Setting.List.LastBackupDate = DateTime.Now.ToString("o");
                await Setting.Setting.Save();

                Infos.Message = $"Backup completed successfully: {archiveFileName}";
                return true;
            }
            catch (Exception ex)
            {
                Infos.Message = $"Backup failed: {ex.Message}";
                return false;
            }
        }

        private static async Task CreateSevenZipArchive(string sourceDirectory, string archiveFilePath, string sevenZipExe)
        {
            sourceDirectory = Path.GetFullPath(sourceDirectory);
            archiveFilePath = Path.GetFullPath(archiveFilePath);

            if (File.Exists(archiveFilePath))
            {
                File.Delete(archiveFilePath);
            }

            int threadCount = Math.Max(1, Environment.ProcessorCount);
            using Process process = new();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = sevenZipExe,
                WorkingDirectory = sourceDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process.StartInfo.ArgumentList.Add("a");
            process.StartInfo.ArgumentList.Add("-t7z");
            process.StartInfo.ArgumentList.Add($"-mx={SevenZipCompressionLevel}");
            process.StartInfo.ArgumentList.Add("-m0=LZMA2");
            process.StartInfo.ArgumentList.Add($"-mmt={threadCount}");
            process.StartInfo.ArgumentList.Add("-mqs=on");
            process.StartInfo.ArgumentList.Add("-ms=on");
            process.StartInfo.ArgumentList.Add("-y");
            process.StartInfo.ArgumentList.Add(archiveFilePath);
            process.StartInfo.ArgumentList.Add("*");
            process.StartInfo.ArgumentList.Add("-r");

            process.Start();

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string output = await outputTask;
            string error = await errorTask;

            if (process.ExitCode != 0 || !File.Exists(archiveFilePath))
            {
                if (File.Exists(archiveFilePath))
                {
                    File.Delete(archiveFilePath);
                }

                string details = string.Join(" ", new[] { error, output }
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                    .Trim();

                throw new InvalidOperationException(string.IsNullOrWhiteSpace(details)
                    ? $"7-Zip exited with code {process.ExitCode}."
                    : details);
            }
        }

        private static string FindSevenZipExecutable()
        {
            string[] candidatePaths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe")
            };

            foreach (string candidate in candidatePaths)
            {
                if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                {
                    return candidate;
                }
            }

            string pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(pathValue))
            {
                return string.Empty;
            }

            foreach (string pathPart in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                try
                {
                    string candidate = Path.Combine(pathPart, "7z.exe");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch
                {
                }
            }

            return string.Empty;
        }
    }
}
