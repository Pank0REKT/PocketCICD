namespace PocketCICD.Interfaces;

public interface IFileService
{
    public bool MoveToUpdate(string updateDirectory, string sourceDirectory, string[] exclusionPaths);
    public bool CreateBackup(string backupDirectory, string targetDirectory);
    public bool RenameAppOffline(string targetDirectory, bool enable);
    public bool DeleteOldFiles(string targetDirectory, string[] exclusionPaths);
    public bool MoveUpdateToMainDirectory(string updateDirectory, string targetDirectory);
    bool CreateLocalBackup(string projectName, string mainDirectory);
}