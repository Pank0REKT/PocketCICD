using System.Collections.ObjectModel;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using PocketCICD.Interfaces;
using PocketCICD.Models;

namespace PocketCICD.Services;

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        var exeFolder = AppDomain.CurrentDomain.BaseDirectory;
        var dbPath = Path.Combine(exeFolder, "projects.db");

        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Projects (
                    ProjectId       INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectName     TEXT    NOT NULL,
                    UpdateDirectory TEXT,
                    SourceDirectory TEXT,
                    BackupDirectory TEXT,
                    TargetDirectory TEXT
                );

                CREATE TABLE IF NOT EXISTS ExclusionFiles (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectId INTEGER NOT NULL,
                    FilePath  TEXT    NOT NULL,
                    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ExclusionDirectories (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectId INTEGER NOT NULL,
                    DirPath   TEXT    NOT NULL,
                    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId) ON DELETE CASCADE
                );
            ");
    }

    public async Task<IEnumerable<ProjectPathsModel>> GetAllProjectsAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryAsync<ProjectPathsModel>(
            "SELECT * FROM Projects ORDER BY ProjectName");
    }

    public async Task<ProjectPathsModel?> GetProjectByIdAsync(int projectId)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var project = await connection.QueryFirstOrDefaultAsync<ProjectPathsModel>(
            "SELECT * FROM Projects WHERE ProjectId = @ProjectId",
            new { ProjectId = projectId });

        if (project is null) return null;

        // Подгружаем исключения
        var files = await connection.QueryAsync<string>(
            "SELECT FilePath FROM ExclusionFiles WHERE ProjectId = @ProjectId",
            new { ProjectId = projectId });

        var dirs = await connection.QueryAsync<string>(
            "SELECT DirPath FROM ExclusionDirectories WHERE ProjectId = @ProjectId",
            new { ProjectId = projectId });

        project.ExclusionPaths = new ObservableCollection<string>(
            files.Concat(dirs));

        return project;
    }

    public async Task<int> SaveProjectAsync(ProjectPathsModel project)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            int projectId;

            if (project.ProjectId.HasValue)
            {
                await connection.ExecuteAsync(@"
                        UPDATE Projects SET
                            ProjectName     = @ProjectName,
                            UpdateDirectory = @UpdateDirectory,
                            SourceDirectory = @SourceDirectory,
                            BackupDirectory = @BackupDirectory,
                            TargetDirectory = @TargetDirectory
                        WHERE ProjectId = @ProjectId",
                    project, transaction);

                projectId = project.ProjectId.Value;
                
                await connection.ExecuteAsync(
                    "DELETE FROM ExclusionFiles       WHERE ProjectId = @ProjectId",
                    new { ProjectId = projectId }, transaction);
                await connection.ExecuteAsync(
                    "DELETE FROM ExclusionDirectories WHERE ProjectId = @ProjectId",
                    new { ProjectId = projectId }, transaction);
            }
            else
            {
                projectId = await connection.ExecuteScalarAsync<int>(@"
                        INSERT INTO Projects (ProjectName, UpdateDirectory, SourceDirectory, BackupDirectory, TargetDirectory)
                        VALUES (@ProjectName, @UpdateDirectory, @SourceDirectory, @BackupDirectory, @TargetDirectory);
                        SELECT last_insert_rowid();",
                    project, transaction);
            }
            
            if (project.ExclusionPaths?.Count > 0)
            {
                foreach (var path in project.ExclusionPaths)
                {
                    if (File.Exists(path))
                    {
                        await connection.ExecuteAsync(
                            "INSERT INTO ExclusionFiles (ProjectId, FilePath) VALUES (@ProjectId, @Path)",
                            new { ProjectId = projectId, Path = path }, transaction);
                    }
                    else if (Directory.Exists(path))
                    {
                        await connection.ExecuteAsync(
                            "INSERT INTO ExclusionDirectories (ProjectId, DirPath) VALUES (@ProjectId, @Path)",
                            new { ProjectId = projectId, Path = path }, transaction);
                    }
                }
            }

            await transaction.CommitAsync();
            return projectId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteProjectAsync(int projectId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(
            "DELETE FROM Projects WHERE ProjectId = @ProjectId",
            new { ProjectId = projectId });
    }
}