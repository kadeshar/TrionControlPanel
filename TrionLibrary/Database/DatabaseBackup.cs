using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using TrionLibrary.Setting;
using TrionLibrary.Sys;

namespace TrionLibrary.Database
{
    public class DatabaseBackup
    {
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
                string zipFileName = $"Backup_{timestamp}.zip";
                string zipFilePath = Path.Combine(backupFolder, zipFileName);

                await Task.Run(() =>
                {
                    ZipFile.CreateFromDirectory(mysqlDataPath, zipFilePath, CompressionLevel.Optimal, false);
                });

                Setting.Setting.List.LastBackupDate = DateTime.Now.ToString("o");
                await Setting.Setting.Save();

                Infos.Message = $"Backup completed successfully: {zipFileName}";
                return true;
            }
            catch (Exception ex)
            {
                Infos.Message = $"Backup failed: {ex.Message}";
                return false;
            }
        }
    }
}
