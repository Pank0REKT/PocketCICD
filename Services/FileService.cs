using System.Diagnostics;
using PocketCICD.Interfaces;
using System.IO;

namespace PocketCICD.Services;

public class FileService : IFileService
{
    public bool MoveToUpdate(string updateDirectory, string sourceDirectory, string[] exclusionPaths)
    {
        try
        {
            var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (IsExcluded(file, exclusionPaths)) continue;

                var relativePath = Path.GetRelativePath(sourceDirectory, file);
                var destPath = Path.Combine(updateDirectory, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                File.Copy(file, destPath, overwrite: true);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogError(nameof(MoveToUpdate), ex);
            return false;
        }
    }

    public bool CreateBackup(string backupDirectory, string mainDirectory)
    {
        try
        {
            if (Directory.Exists(backupDirectory))
            {
                foreach (var file in Directory.GetFiles(backupDirectory, "*", SearchOption.AllDirectories))
                    File.Delete(file);

                foreach (var dir in Directory.GetDirectories(backupDirectory, "*", SearchOption.AllDirectories)
                             .OrderByDescending(d => d.Length))
                    Directory.Delete(dir);
            }
            
            CopyDirectory(mainDirectory, backupDirectory);

            return true;
        }
        catch (Exception ex)
        {
            LogError(nameof(CreateBackup), ex);
            return false;
        }
    }

    public bool RenameAppOffline(string targetDirectory, bool enable)
    {
        try
        {
            var activePath   = Path.Combine(targetDirectory, "app_offline.htm");
            var disabledPath = Path.Combine(targetDirectory, "--app_offline.htm");

            if (enable)
            {
                if (File.Exists(disabledPath))
                    File.Move(disabledPath, activePath);
                else if (!File.Exists(activePath))
                    File.WriteAllText(activePath, "<h1>Site is under maintenance</h1>");
            }
            else
            {
                if (File.Exists(activePath))
                    File.Move(activePath, disabledPath);
                else if (!File.Exists(disabledPath))
                    LogError(nameof(RenameAppOffline), 
                        new FileNotFoundException("Не найден ни app_offline.htm ни --app_offline.htm"));
            }

            return true;
        }
        catch (Exception ex)
        {
            LogError(nameof(RenameAppOffline), ex);
            return false;
        }
    }

    public bool DeleteOldFiles(string targetDirectory, string[] exclusionPaths)
    {
        try
        {
            foreach (var file in Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories))
            {
                if (file.ToLower().Contains("app_offline"))
                {
                    continue;
                }
                
                if (IsExcluded(file, exclusionPaths)) continue;
                File.Delete(file);
            }

            foreach (var dir in Directory.GetDirectories(targetDirectory, "*", SearchOption.AllDirectories)
                         .OrderByDescending(d => d.Length))
            {
                if (IsExcluded(dir, exclusionPaths)) continue;
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogError(nameof(DeleteOldFiles), ex);
            return false;
        }
    }

    public bool MoveUpdateToMainDirectory(string updateDirectory, string targetDirectory)
    {
        try
        {
            foreach (var file in Directory.GetFiles(updateDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(updateDirectory, file);
                var destPath = Path.Combine(targetDirectory, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                MoveFileWithRetry(file, destPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogError(nameof(MoveUpdateToMainDirectory), ex);
            return false;
        }
    }

    private static bool IsExcluded(string path, string[] exclusionPaths)
    {
        return exclusionPaths.Any(excluded =>
            path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase) ||
            path.Equals(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private static void CopyDirectory(string source, string destination)
    {
        var normalizedSource = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
        var normalizedDestination = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar);

        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            if (file.ToLower().Contains("app_offline"))
            {
                continue;
            }

            var destFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            if (dir.ToLower().Contains("backup") || dir.ToLower().Contains("update"))
            {
                continue;
            }

            var normalizedDir = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar);

            if (normalizedDir.StartsWith(normalizedDestination, StringComparison.OrdinalIgnoreCase))
                continue;

            var destDir = Path.Combine(destination, Path.GetFileName(dir));
            CopyDirectory(dir, destDir);
        }
    }

    private static void MoveFileWithRetry(string source, string dest, int retries = 3)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                File.Move(source, dest, overwrite: true);
                return;
            }
            catch (IOException) when (i < retries - 1)
            {
                Thread.Sleep(500);
            }
        }
    }
    
    public bool CreateLocalBackup(string projectName, string mainDirectory)
    {
        try
        {
            var exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            
            var safeName = string.Concat(projectName
                .Split(Path.GetInvalidFileNameChars()));
            
            var backupDir = Path.Combine(exeFolder, "Backups", $"{safeName}_{timestamp}");

            CopyDirectory(mainDirectory, backupDir);

            return true;
        }
        catch (Exception ex)
        {
            LogError(nameof(CreateLocalBackup), ex);
            return false;
        }
    }

    private static void LogError(string method, Exception ex)
    {
        Debug.WriteLine($"[FileService.{method}] {ex.Message}");
    }
}