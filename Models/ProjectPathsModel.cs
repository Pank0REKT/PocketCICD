using System.Collections.ObjectModel;

namespace PocketCICD.Models;

public class ProjectPathsModel
{
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? UpdateDirectory { get; set; }
    public string? SourceDirectory { get; set; }
    public string? BackupDirectory { get; set; }
    public string? TargetDirectory { get; set; }
    public ObservableCollection<string>? ExclusionPaths { get; set; }
}