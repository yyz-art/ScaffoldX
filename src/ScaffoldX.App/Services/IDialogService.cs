namespace ScaffoldX.App.Services;

/// <summary>
/// Abstracts WPF file/folder dialogs to enable unit testing of ViewModels.
/// All methods return <c>null</c> when the user cancels the dialog.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a folder browser dialog.
    /// </summary>
    /// <param name="description">Dialog description text.</param>
    /// <returns>Selected folder path, or <c>null</c> if cancelled.</returns>
    string? ShowOpenFolderDialog(string? description = null);

    /// <summary>
    /// Shows a file open dialog (single file selection).
    /// </summary>
    /// <param name="filter">File filter string (e.g. "Image files|*.jpg;*.png").</param>
    /// <param name="title">Dialog title.</param>
    /// <returns>Selected file path, or <c>null</c> if cancelled.</returns>
    string? ShowOpenFileDialog(string? filter = null, string? title = null);

    /// <summary>
    /// Shows a file open dialog with multi-select enabled.
    /// </summary>
    /// <param name="filter">File filter string.</param>
    /// <param name="title">Dialog title.</param>
    /// <returns>Selected file paths, or <c>null</c> if cancelled.</returns>
    IReadOnlyList<string>? ShowOpenFilesDialog(string? filter = null, string? title = null);

    /// <summary>
    /// Shows a file save dialog.
    /// </summary>
    /// <param name="filter">File filter string.</param>
    /// <param name="title">Dialog title.</param>
    /// <returns>Selected file path, or <c>null</c> if cancelled.</returns>
    string? ShowSaveFileDialog(string? filter = null, string? title = null);
}
