namespace ScaffoldX.App.Services;

/// <summary>
/// Manages a persistent list of recently opened files, stored as a JSON file
/// in the application base directory.
/// </summary>
public interface IRecentFilesService
{
    /// <summary>
    /// Returns the list of recently opened file paths, ordered from most recent to oldest.
    /// </summary>
    IReadOnlyList<string> GetRecentFiles();

    /// <summary>
    /// Adds a file path to the recent files list. If the path already exists, it is moved to the top.
    /// The list is capped at 10 entries.
    /// </summary>
    /// <param name="path">The absolute file path to add.</param>
    void AddRecentFile(string path);

    /// <summary>
    /// Clears all entries from the recent files list and removes the persisted JSON file.
    /// </summary>
    void ClearRecentFiles();
}
