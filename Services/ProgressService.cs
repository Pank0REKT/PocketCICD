using System.Windows.Controls;
using PocketCICD.Enums;
using PocketCICD.Interfaces;

namespace PocketCICD.Services;

public static class ProgressService
{
    private static ProgressBar? _progressBar;
    private static TextBlock? _textBlock;
    private static double _progressStep;

    public static void SetProgressBarElements(ProgressBar progressBar, TextBlock textBlock)
    {
        _progressBar = progressBar;
        _textBlock = textBlock;
    }

    public static void SetProgressStep(int steps)
    {
        if (_progressBar == null)
            return;
        
        _progressBar!.Value = 0;
        _progressStep = 100 / steps;
    }

    public static void SetText(ProgressStages stage)
    {
        if (_textBlock == null)
            return;

        var stagesCount = Enum.GetNames(typeof(ProgressStages)).Length;
        var text = stage switch
        {
            ProgressStages.CreatingBackup => $"Создание бекапа {(int)stage}/{stagesCount}",
            ProgressStages.MovingUpdatePackage => $"Перенос новых файлов в папку Update {(int)stage}/{stagesCount}",
            ProgressStages.EnablingAppOffline => $"Включение app_offline {(int)stage}/{stagesCount}",
            ProgressStages.DeletingOldVersionFiles => $"Удаление прошлых файлов {(int)stage}/{stagesCount}",
            ProgressStages.DeployingUpdatedFiles => $"Перенос новых файлов {(int)stage}/{stagesCount}",
            ProgressStages.DisablingAppOffline => $"Выключение app_offline {(int)stage}/{stagesCount}",
            _ => _textBlock!.Text
        };

        _textBlock!.Dispatcher.Invoke(() =>
        {
            _textBlock.Text = text;
        });
    }

    public static async Task DoStepAsync()
    {
        await _progressBar!.Dispatcher.InvokeAsync(() =>
        {
            _progressBar.Value += _progressStep;
        });
    }

    public static void ResetProgress()
    {
        _progressBar!.Dispatcher.Invoke(() =>
        {
            _progressBar.Value = 0;
        });
        _textBlock!.Dispatcher.Invoke(() =>
        {
            _textBlock.Text = string.Empty;
        });
    }
}