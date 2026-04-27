namespace ScaffoldX.App.Models;

/// <summary>
/// 项目生成配置，包含所有类型项目的完整参数集合。
/// </summary>
public class ProjectConfig
{
    // ── 基础信息 ──────────────────────────────────────────────────────────────

    /// <summary>项目名称，须符合 C# 标识符规范（^[A-Za-z][A-Za-z0-9_]{0,49}$）。</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>输出根目录的绝对路径。</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>命名空间前缀，默认与 ProjectName 相同。</summary>
    public string NamespacePrefix { get; set; } = string.Empty;

    /// <summary>UI 框架选择："WPF" 或 "Avalonia"。</summary>
    public string UIFramework { get; set; } = "WPF";

    /// <summary>.NET 目标版本：".NET 6" 或 ".NET 8"。</summary>
    public string DotNetVersion { get; set; } = ".NET 8";

    /// <summary>项目类型："Collection"（采集）| "Vision"（视觉）| "System"（系统）。</summary>
    public string ProjectType { get; set; } = string.Empty;

    /// <summary>项目描述，写入生成的 README 及程序集信息。</summary>
    public string ProjectDescription { get; set; } = string.Empty;

    // ── 采集类（Collection）────────────────────────────────────────────────────

    /// <summary>已选驱动列表，如 "S7Net"、"ModbusTcp"、"OpcUa"。</summary>
    public List<string> SelectedDrivers { get; set; } = new();

    /// <summary>是否生成仿真驱动（SimulationDriver）。</summary>
    public bool EnableSimulationDriver { get; set; } = true;

    /// <summary>默认 PLC IP 地址。</summary>
    public string DefaultPLCIp { get; set; } = "192.168.1.1";

    /// <summary>默认 PLC 端口。</summary>
    public int DefaultPLCPort { get; set; } = 102;

    /// <summary>S7 协议机架号。</summary>
    public int S7Rack { get; set; } = 0;

    /// <summary>S7 协议槽号。</summary>
    public int S7Slot { get; set; } = 1;

    /// <summary>OPC UA 服务端端点 URL。</summary>
    public string OpcUaEndpoint { get; set; } = "opc.tcp://localhost:4840";

    // ── 视觉类（Vision）────────────────────────────────────────────────────────

    /// <summary>相机品牌，如 "海康"、"大华"、"Basler"。</summary>
    public string CameraBrand { get; set; } = "海康";

    /// <summary>模型类型："Classification"（分类）| "Detection"（检测）| "Segmentation"（分割）。</summary>
    public string ModelType { get; set; } = "Classification";

    /// <summary>推理模型文件路径（.onnx / .engine 等）。</summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>是否启用图像处理 Pipeline。</summary>
    public bool EnablePipeline { get; set; } = true;

    // ── 系统类（System）────────────────────────────────────────────────────────

    /// <summary>已选功能模块列表，如 "UserManagement"、"RolePermission"、"AuditLog"。</summary>
    public List<string> SelectedModules { get; set; } = new() { "UserManagement", "RolePermission" };

    /// <summary>是否生成独立登录窗口。</summary>
    public bool EnableLoginWindow { get; set; } = true;

    /// <summary>是否启用跨平台（Avalonia）支持。</summary>
    public bool EnableCrossPlatform { get; set; } = false;

    /// <summary>是否强制首次登录修改密码。</summary>
    public bool ForcePasswordChange { get; set; } = false;
}
