using System.Collections.ObjectModel;
using Prism.Mvvm;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 类别状态子 ViewModel，管理标注类别列表。
/// 选中类别索引仍由 ClassManagementHandler 持有。
/// </summary>
public class ClassStateVM : BindableBase
{
    /// <summary>类别列表。</summary>
    public ObservableCollection<AnnotationClass> Classes { get; } = new();

    /// <summary>
    /// 从项目中刷新类别列表。
    /// </summary>
    /// <param name="project">当前标注项目，为 null 时清空列表。</param>
    public void UpdateClassesList(AnnotationProject? project)
    {
        Classes.Clear();
        if (project == null) return;

        foreach (var cls in project.Classes)
            Classes.Add(cls);
    }
}
