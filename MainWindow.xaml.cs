using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using PocketCICD.Enums;
using PocketCICD.Interfaces;
using PocketCICD.Models;
using PocketCICD.Services;

namespace PocketCICD
{
    public partial class MainWindow : Window
    {
        private readonly IFileService _fileService;
        private readonly IDatabaseService _databaseService;

        public ProjectPathsModel Project { get; } = new();

        public MainWindow(IFileService fileService, IDatabaseService databaseService)
        {
            InitializeComponent();
            _fileService = fileService;
            _databaseService = databaseService;

            Project.ExclusionPaths = new ObservableCollection<string>();
            ListExclusions.ItemsSource = Project.ExclusionPaths;
            ProgressService.SetProgressBarElements(ProgressBar, ProgressBarTextBlock);

            Loaded += async (_, _) => await LoadProjectsAsync();
        }


        private static string? PickFolder(string title)
        {
            using var dlg = new CommonOpenFileDialog
            {
                Title = title,
                IsFolderPicker = true,
                EnsurePathExists = true
            };
            return dlg.ShowDialog() == CommonFileDialogResult.Ok ? dlg.FileName : null;
        }

        private static string[] PickFoldersAndFiles(string title)
        {
            var results = new List<string>();

            using var fileDlg = new CommonOpenFileDialog
            {
                Title = title + " — выберите файлы",
                Multiselect = true,
                EnsureFileExists = true
            };
            if (fileDlg.ShowDialog() == CommonFileDialogResult.Ok)
                results.AddRange(fileDlg.FileNames);

            using var folderDlg = new CommonOpenFileDialog
            {
                Title = title + " — выберите папки",
                Multiselect = true,
                IsFolderPicker = true
            };
            if (folderDlg.ShowDialog() == CommonFileDialogResult.Ok)
                results.AddRange(folderDlg.FileNames);

            return results.ToArray();
        }

        private static void SetLabel(TextBlock label, string path)
        {
            label.Text = path;
            label.Foreground = System.Windows.Media.Brushes.LightGreen;
        }

        private static void SetLabelComboBox(TextBlock label, string? path,
            System.Windows.Media.Brush activeBrush,
            System.Windows.Media.Brush emptyBrush)
        {
            if (string.IsNullOrEmpty(path))
            {
                label.Text = "Путь не выбран";
                label.Foreground = emptyBrush;
            }
            else
            {
                label.Text = path;
                label.Foreground = activeBrush;
            }
        }
        

        private void BtnSourceProject_Click(object sender, RoutedEventArgs e)
        {
            var path = PickFolder("Выберите исходный проект");
            if (path is null) return;
            Project.SourceDirectory = path;
            SetLabel(LblSourceProject, path);
        }

        private void BtnTargetDir_Click(object sender, RoutedEventArgs e)
        {
            var path = PickFolder("Выберите целевую директорию");
            if (path is null) return;
            Project.TargetDirectory = path;
            SetLabel(LblTargetDir, path);
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var path = PickFolder("Выберите папку Backup");
            if (path is null) return;
            Project.BackupDirectory = path;
            SetLabel(LblBackup, path);
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            var path = PickFolder("Выберите папку Update");
            if (path is null) return;
            Project.UpdateDirectory = path;
            SetLabel(LblUpdate, path);
        }

        private void BtnExclusions_Click(object sender, RoutedEventArgs e)
        {
            var paths = PickFoldersAndFiles("Файлы исключения");

            Project.ExclusionPaths!.Clear();

            foreach (var p in paths)
                Project.ExclusionPaths.Add(p);

            LblExclusionsEmpty.Visibility =
                Project.ExclusionPaths!.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void BtnPublish_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePaths()) return;

            // ✅ Читаем всё из UI-потока до любых Task.Run
            Project.ExclusionPaths!.Add(Project.BackupDirectory!);
            Project.ExclusionPaths!.Add(Project.UpdateDirectory!);

            var exclusions = Project.ExclusionPaths?.ToArray() ?? [];
            var localBackup = ChkLocalBackup.IsChecked == true;
            var projectName = Project.ProjectName;
            var sourceDir = Project.SourceDirectory!;
            var targetDir = Project.TargetDirectory!;
            var backupDir = Project.BackupDirectory!;
            var updateDir = Project.UpdateDirectory!;
            
            if (localBackup && string.IsNullOrWhiteSpace(projectName))
            {
                var name = AskProjectName(null);
                if (name is null)
                {
                    var result = MessageBox.Show(
                        "Название проекта не указано — локальный Backup не будет создан.\nПродолжить публикацию?",
                        "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No) return;
                    localBackup = false;
                }
                else
                {
                    Project.ProjectName = name;
                    projectName = name;
                }
            }
            
            ProgressService.SetText(ProgressStages.CreatingBackup);
            ProgressService.SetProgressStep(_fileService.GetFilesCount(targetDir));
            await Task.Run(async () => await _fileService.CreateBackup(backupDir, targetDir));

            if (localBackup && !string.IsNullOrWhiteSpace(projectName))
            {
                var pName = projectName;
                await Task.Run(async () => await _fileService.CreateLocalBackup(pName, targetDir));
            }
            
            ProgressService.SetText(ProgressStages.MovingUpdatePackage);
            ProgressService.SetProgressStep(_fileService.GetFilesCount(sourceDir));
            await Task.Run(async () => await _fileService.MoveToUpdate(updateDir, sourceDir, exclusions));
            
            ProgressService.SetText(ProgressStages.EnablingAppOffline);
            ProgressService.SetProgressStep(1);
            await Task.Run(async () => await _fileService.RenameAppOffline(targetDir, enable: true));
            
            ProgressService.SetText(ProgressStages.DeletingOldVersionFiles);
            ProgressService.SetProgressStep(_fileService.GetFilesCount(targetDir));
            await Task.Run(async () => await _fileService.DeleteOldFiles(targetDir, exclusions));
            
            ProgressService.SetText(ProgressStages.DeployingUpdatedFiles);
            ProgressService.SetProgressStep(_fileService.GetFilesCount(updateDir));
            await Task.Run(async () => await _fileService.MoveUpdateToMainDirectory(updateDir, targetDir));
            
            ProgressService.SetText(ProgressStages.DisablingAppOffline);
            ProgressService.SetProgressStep(1);
            await Task.Run(async () => await _fileService.RenameAppOffline(targetDir, enable: false));

            ProgressService.ResetProgress();

            MessageBox.Show("Публикация завершена!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            Project.SourceDirectory = null;
            Project.TargetDirectory = null;
            Project.BackupDirectory = null;
            Project.UpdateDirectory = null;
            Project.ExclusionPaths?.Clear();

            var grey = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x6C, 0x70, 0x86));

            LblSourceProject.Text = "Путь не выбран";
            LblSourceProject.Foreground = grey;
            LblTargetDir.Text = "Путь не выбран";
            LblTargetDir.Foreground = grey;
            LblBackup.Text = "Путь не выбран";
            LblBackup.Foreground = grey;
            LblUpdate.Text = "Путь не выбран";
            LblUpdate.Foreground = grey;

            LblExclusionsEmpty.Visibility = Visibility.Visible;
        }
        
        private async Task LoadProjectsAsync()
        {
            var projects = await _databaseService.GetAllProjectsAsync();
            ComboBoxProjects.ItemsSource = projects.ToList();
            ComboBoxProjects.DisplayMemberPath = nameof(ProjectPathsModel.ProjectName);
            ComboBoxProjects.SelectedValuePath = nameof(ProjectPathsModel.ProjectId);
        }

        private async void ComboBoxProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnEditProject.IsEnabled = ComboBoxProjects.SelectedValue is int;

            if (ComboBoxProjects.SelectedValue is not int projectId) return;

            var project = await _databaseService.GetProjectByIdAsync(projectId);
            if (project is null) return;

            Project.ProjectId = project.ProjectId;
            Project.ProjectName = project.ProjectName;
            Project.SourceDirectory = project.SourceDirectory;
            Project.TargetDirectory = project.TargetDirectory;
            Project.BackupDirectory = project.BackupDirectory;
            Project.UpdateDirectory = project.UpdateDirectory;

            Project.ExclusionPaths!.Clear();
            foreach (var path in project.ExclusionPaths ?? [])
                Project.ExclusionPaths.Add(path);

            var green = System.Windows.Media.Brushes.LightGreen;
            var grey = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x6C, 0x70, 0x86));

            SetLabelComboBox(LblSourceProject, project.SourceDirectory, green, grey);
            SetLabelComboBox(LblTargetDir, project.TargetDirectory, green, grey);
            SetLabelComboBox(LblBackup, project.BackupDirectory, green, grey);
            SetLabelComboBox(LblUpdate, project.UpdateDirectory, green, grey);

            LblExclusionsEmpty.Visibility =
                Project.ExclusionPaths.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        

        private bool ValidatePaths()
        {
            var missing = new List<string>();

            if (string.IsNullOrEmpty(Project.SourceDirectory)) missing.Add("Исходный проект");
            if (string.IsNullOrEmpty(Project.TargetDirectory)) missing.Add("Целевая директория");
            if (string.IsNullOrEmpty(Project.BackupDirectory)) missing.Add("Папка Backup");
            if (string.IsNullOrEmpty(Project.UpdateDirectory)) missing.Add("Папка Update");

            if (missing.Count == 0) return true;

            MessageBox.Show(
                $"Не заполнены обязательные пути:\n• {string.Join("\n• ", missing)}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

            return false;
        }

        private async void BtnSaveNew_OnClick(object sender, RoutedEventArgs e)
        {
            Project.ProjectId = null;

            var name = AskProjectName(Project.ProjectName);
            if (name is null) return;
            Project.ProjectName = name;

            var savedId = await _databaseService.SaveProjectAsync(Project);
            Project.ProjectId = savedId;

            await LoadProjectsAsync();
            ComboBoxProjects.SelectedValue = savedId;

            MessageBox.Show($"Проект «{Project.ProjectName}» сохранён.", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnEditProject_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Project.ProjectId.HasValue) return;

            var name = AskProjectName(Project.ProjectName);
            if (name is null) return;
            Project.ProjectName = name;

            await _databaseService.SaveProjectAsync(Project);

            await LoadProjectsAsync();
            ComboBoxProjects.SelectedValue = Project.ProjectId;

            MessageBox.Show($"Проект «{Project.ProjectName}» обновлён.", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClearComboBox_Click(object sender, RoutedEventArgs e)
        {
            ComboBoxProjects.SelectedIndex = -1;
            BtnEditProject.IsEnabled = false;
            BtnReset_Click(sender, e);
        }

        private static string? AskProjectName(string? currentName)
        {
            var dialog = new ProjectNameDialog(currentName);
            return dialog.ShowDialog() == true ? dialog.ProjectName : null;
        }

        private void BtnGitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Pank0REKT/PocketCICD",
                UseShellExecute = true
            });
        }
    }
}