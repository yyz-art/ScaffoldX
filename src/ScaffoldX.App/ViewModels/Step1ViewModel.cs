using Prism.Commands;
using Prism.Mvvm;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 步骤一 ViewModel：项目类型选择。
/// 提供三种类型（Collection/Vision/System）的选择状态和验证。
/// </summary>
public class Step1ViewModel : BindableBase
{
    private string _selectedProjectType = string.Empty;

    /// <summary>
    /// 初始化步骤一 ViewModel，注册选择命令。
    /// </summary>
    public Step1ViewModel()
    {
        SelectTypeCommand = new DelegateCommand<string>(ExecuteSelectType);
    }

    /// <summary>当前选中的项目类型："Collection"、"Vision" 或 "System"。</summary>
    public string SelectedProjectType
    {
        get => _selectedProjectType;
        private set
        {
            if (SetProperty(ref _selectedProjectType, value))
            {
                RaisePropertyChanged(nameof(IsCollectionSelected));
                RaisePropertyChanged(nameof(IsVisionSelected));
                RaisePropertyChanged(nameof(IsSystemSelected));
                RaisePropertyChanged(nameof(CanProceed));
                RaisePropertyChanged(nameof(TypeDescription));
                RaisePropertyChanged(nameof(CollectionBorderBrush));
                RaisePropertyChanged(nameof(VisionBorderBrush));
                RaisePropertyChanged(nameof(SystemBorderBrush));
            }
        }
    }

    /// <summary>是否选中了"工业采集"类型。</summary>
    public bool IsCollectionSelected => SelectedProjectType == "Collection";

    /// <summary>是否选中了"视觉检测"类型。</summary>
    public bool IsVisionSelected => SelectedProjectType == "Vision";

    /// <summary>是否选中了"系统定制"类型。</summary>
    public bool IsSystemSelected => SelectedProjectType == "System";

    /// <summary>是否已选择类型，控制下一步按钮可用性。</summary>
    public bool CanProceed => !string.IsNullOrEmpty(SelectedProjectType);

    /// <summary>当前选中类型的详细描述文字。</summary>
    public string TypeDescription => SelectedProjectType switch
    {
        "Collection" => "工业采集项目：集成 S7Net、ModbusTcp、OPC UA 等主流工业协议驱动，" +
                        "生成完整的数据采集、存储、报警和趋势图表框架，适用于 PLC/传感器数据采集场景。",
        "Vision" => "视觉检测项目：集成海康、大华、Basler 等主流相机 SDK，" +
                    "支持分类、检测、分割三种 AI 推理模式，生成图像采集、处理 Pipeline 和结果展示框架。",
        "System" => "系统定制项目：生成包含用户管理、角色权限、审计日志等企业级功能模块的完整上位机系统框架，" +
                    "支持独立登录窗口和跨平台（Avalonia）选项。",
        _ => "请选择一种项目类型以查看详细说明。"
    };

    /// <summary>工业采集卡片边框颜色（选中时为主色调蓝色）。</summary>
    public string CollectionBorderBrush => IsCollectionSelected ? "#1565C0" : "#BDBDBD";

    /// <summary>视觉检测卡片边框颜色（选中时为主色调蓝色）。</summary>
    public string VisionBorderBrush => IsVisionSelected ? "#1565C0" : "#BDBDBD";

    /// <summary>系统定制卡片边框颜色（选中时为主色调蓝色）。</summary>
    public string SystemBorderBrush => IsSystemSelected ? "#1565C0" : "#BDBDBD";

    /// <summary>选择项目类型命令，参数为类型字符串。</summary>
    public DelegateCommand<string> SelectTypeCommand { get; }

    /// <summary>重置选择状态（新建项目时调用）。</summary>
    public void Reset()
    {
        SelectedProjectType = string.Empty;
    }

    private void ExecuteSelectType(string type)
    {
        SelectedProjectType = type;
    }
}
