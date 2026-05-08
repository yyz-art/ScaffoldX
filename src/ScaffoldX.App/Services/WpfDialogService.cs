using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace ScaffoldX.App.Services;

/// <summary>
/// WPF implementation of <see cref="IDialogService"/> using Ookii.Dialogs and Microsoft.Win32.
/// </summary>
public sealed class WpfDialogService : IDialogService
{
    /// <inheritdoc />
    public string? ShowOpenFolderDialog(string? description = null)
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = description ?? string.Empty
        };

        return dialog.ShowDialog() == true ? dialog.SelectedPath : null;
    }

    /// <inheritdoc />
    public string? ShowOpenFileDialog(string? filter = null, string? title = null)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter ?? "All files|*.*",
            Title = title ?? "打开文件"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<string>? ShowOpenFilesDialog(string? filter = null, string? title = null)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter ?? "All files|*.*",
            Title = title ?? "打开文件",
            Multiselect = true
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }

    /// <inheritdoc />
    public string? ShowSaveFileDialog(string? filter = null, string? title = null)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter ?? "All files|*.*",
            Title = title ?? "保存文件"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
