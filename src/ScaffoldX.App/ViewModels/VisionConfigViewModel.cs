using Prism.Mvvm;
using ScaffoldX.Core.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 视觉类配置 ViewModel：相机品牌、模型类型、推理路径、Pipeline 开关。
/// 属性直接读写 ProjectConfig，无需手动字段拷贝。
/// </summary>
public class VisionConfigViewModel : BindableBase
{
    /// <summary>
    /// 初始化视觉类配置，构建选项列表。
    /// </summary>
    public VisionConfigViewModel() : this(new ProjectConfig
    {
        CameraBrand = "海康",
        ModelType = "Classification",
        EnablePipeline = true
    }) { }

    /// <summary>
    /// 初始化视觉类配置，绑定到指定 ProjectConfig。
    /// </summary>
    /// <param name="config">项目配置对象。</param>
    public VisionConfigViewModel(ProjectConfig config)
    {
        Config = config;
        CameraBrands = new List<string> { "海康", "大华", "Basler", "其他" };
        ModelTypes = new List<string> { "Classification", "Detection", "Segmentation" };
    }

    // ── 属性 ────────────────────────────────────────────────────────────────────

    /// <summary>关联的项目配置对象，所有属性直接读写此对象。</summary>
    public ProjectConfig Config { get; private set; }

    /// <summary>
    /// 初始化或重新绑定到指定 ProjectConfig。
    /// </summary>
    /// <param name="config">项目配置对象。</param>
    public void Initialize(ProjectConfig config)
    {
        Config = config;
        RaisePropertyChanged(nameof(CameraBrand));
        RaisePropertyChanged(nameof(ModelType));
        RaisePropertyChanged(nameof(ModelPath));
        RaisePropertyChanged(nameof(EnablePipeline));
    }

    /// <summary>相机品牌列表。</summary>
    public List<string> CameraBrands { get; }

    /// <summary>模型类型列表。</summary>
    public List<string> ModelTypes { get; }

    /// <summary>选中的相机品牌。</summary>
    public string CameraBrand
    {
        get => Config.CameraBrand;
        set
        {
            Config.CameraBrand = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>选中的模型类型。</summary>
    public string ModelType
    {
        get => Config.ModelType;
        set
        {
            Config.ModelType = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>推理模型文件路径。</summary>
    public string ModelPath
    {
        get => Config.ModelPath;
        set
        {
            Config.ModelPath = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>是否启用图像处理 Pipeline。</summary>
    public bool EnablePipeline
    {
        get => Config.EnablePipeline;
        set
        {
            Config.EnablePipeline = value;
            RaisePropertyChanged();
        }
    }

    // ── 方法 ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 重置视觉类配置到默认值。
    /// </summary>
    public void Reset()
    {
        CameraBrand = "海康";
        ModelType = "Classification";
        ModelPath = string.Empty;
        EnablePipeline = true;
    }
}
