namespace PocketCICD.Interfaces;

public interface IFileService
{
    int GetFilesCount(string directory);
    Task<bool> MoveToUpdate(string updateDirectory, string sourceDirectory, string[] exclusionPaths);
    Task<bool> CreateBackup(string backupDirectory, string mainDirectory);
    Task<bool> RenameAppOffline(string targetDirectory, bool enable);
    Task<bool> DeleteOldFiles(string targetDirectory, string[] exclusionPaths);
    Task<bool> MoveUpdateToMainDirectory(string updateDirectory, string targetDirectory);
    Task<bool> CreateLocalBackup(string projectName, string mainDirectory);
}