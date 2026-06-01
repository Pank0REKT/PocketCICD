namespace PocketCICD.Enums;

public enum ProgressStages
{
    CreatingBackup = 1,
    MovingUpdatePackage = 2,
    EnablingAppOffline = 3,
    DeletingOldVersionFiles = 4,
    DeployingUpdatedFiles = 5,
    DisablingAppOffline = 6
}