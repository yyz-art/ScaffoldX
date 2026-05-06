using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 类别管理处理器，管理标注类别的添加、移除和选择。
/// </summary>
public class ClassManagementHandler : BindableBase
{
    private readonly Func<AnnotationProject?> _getProject;
    private readonly Action _updateClassesList;
    private readonly Action<string> _setStatusMessage;

    private int _selectedClassIndex;

    /// <summary>
    /// 初始化类别管理处理器。
    /// </summary>
    /// <param name="getProject">获取当前项目的回调。</param>
    /// <param name="updateClassesList">更新类别列表的回调。</param>
    /// <param name="setStatusMessage">设置状态消息的回调。</param>
    public ClassManagementHandler(
        Func<AnnotationProject?> getProject,
        Action updateClassesList,
        Action<string> setStatusMessage)
    {
        _getProject = getProject;
        _updateClassesList = updateClassesList;
        _setStatusMessage = setStatusMessage;

        AddClassCommand = new DelegateCommand(ExecuteAddClass);
        RemoveClassCommand = new DelegateCommand(ExecuteRemoveClass, CanRemoveClass);
        SelectClassCommand = new DelegateCommand<int>(ExecuteSelectClass);
    }

    /// <summary>当前选中的类别索引。</summary>
    public int SelectedClassIndex
    {
        get => _selectedClassIndex;
        set => SetProperty(ref _selectedClassIndex, value);
    }

    /// <summary>当前选中类别的名称。</summary>
    public string CurrentClassName
    {
        get
        {
            var project = _getProject();
            if (project == null || SelectedClassIndex < 0 || SelectedClassIndex >= project.Classes.Count)
                return string.Empty;
            return project.Classes[SelectedClassIndex].Name;
        }
    }

    /// <summary>添加类别命令。</summary>
    public DelegateCommand AddClassCommand { get; }

    /// <summary>移除类别命令。</summary>
    public DelegateCommand RemoveClassCommand { get; }

    /// <summary>按索引选择类别命令。</summary>
    public DelegateCommand<int> SelectClassCommand { get; }

    /// <summary>
    /// 添加新的标注类别，自动分配索引和颜色。
    /// </summary>
    private void ExecuteAddClass()
    {
        var project = _getProject();
        if (project == null) return;

        var newIndex = project.Classes.Count;
        var newClass = new AnnotationClass
        {
            Index = newIndex,
            Name = $"class_{newIndex}",
            Color = GetColorForIndex(newIndex)
        };

        project.Classes.Add(newClass);
        _updateClassesList();
        _setStatusMessage($"已添加类别: {newClass.Name}");
    }

    /// <summary>
    /// 判断是否可以移除类别（至少保留一个类别）。
    /// </summary>
    private bool CanRemoveClass()
    {
        var project = _getProject();
        return project != null && project.Classes.Count > 1;
    }

    /// <summary>
    /// 移除最后一个标注类别。
    /// </summary>
    private void ExecuteRemoveClass()
    {
        var project = _getProject();
        if (project == null || project.Classes.Count <= 1) return;

        var lastClass = project.Classes.Last();
        project.Classes.Remove(lastClass);
        _updateClassesList();
        _setStatusMessage($"已移除类别: {lastClass.Name}");
    }

    /// <summary>
    /// 按索引选择类别（快捷键 1-9）。
    /// </summary>
    /// <param name="index">类别索引。</param>
    private void ExecuteSelectClass(int index)
    {
        var project = _getProject();
        if (project == null || index < 0 || index >= project.Classes.Count) return;
        SelectedClassIndex = index;
        _setStatusMessage($"已切换类别: {project.Classes[index].Name}");
    }

    /// <summary>
    /// 根据索引返回预定义颜色。
    /// </summary>
    /// <param name="index">类别索引。</param>
    private static string GetColorForIndex(int index)
    {
        var colors = new[]
        {
            "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF",
            "#00FFFF", "#FF8000", "#8000FF", "#0080FF", "#FF0080"
        };
        return colors[index % colors.Length];
    }
}
