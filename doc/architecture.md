# ScaffoldX 架构设计文档

> 工业 AI 开发工作台 -- 插件式组合 + 分层设计
>
> 版本：2.0（目标架构） | 框架：.NET 10.0 + WPF

---

## 1 项目概述

ScaffoldX 是面向工业场景的桌面开发工作台，提供四大核心能力：

| 模块 | 职责 | 关键技术 |
|------|------|----------|
| **Scaffold** | 向导式生成 .NET 10.0 WPF 工业项目骨架 | Scriban 模板引擎、声明式元数据 |
| **Annotation** | YOLO 格式标注 + SAM3 半自动分割标注 | TorchSharp、System.Drawing |
| **Training** | YOLO 模型训练配置与脚本生成、模型导出 | Python 脚本生成、ONNX |
| **Management** | 已生成项目的管理、历史记录、一键打开 | 文件系统、JSON 持久化 |

生成的项目依赖 ZC 框架（Avalonia 生态），但 ScaffoldX 自身是 WPF 应用。参考项目位于 `Assets/XKL_HX/`，使用 Avalonia + ZC 框架构建。

模板分类体系：**程序类型**（Collection / Vision / System） -> **设备类型**（如 PLC 品牌、相机品牌） -> **功能模块**（如驱动、推理引擎、用户管理）。

---

## 2 当前架构分析

### 2.1 现有项目结构

```
ScaffoldX/
  src/
    ScaffoldX.App/           -- WPF 应用层（混合了全部功能）
    ScaffoldX.Core/          -- 核心层（混合了模板 + 视觉 + 配置）
    ScaffoldX.Templates/     -- Scriban 模板（嵌入资源）
  tests/
    ScaffoldX.App.Tests/
    ScaffoldX.Core.Tests/
  Assets/
    XKL_HX/                  -- 参考项目（Avalonia + ZC 框架）
```

### 2.2 问题清单

| # | 问题 | 严重程度 | 说明 |
|---|------|----------|------|
| P1 | **ScaffoldX.App 职责膨胀** | 高 | 脚手架向导（Step1-4）、标注工具（AnnotationViewModel + 20+ 个 Handler）、训练平台（YoloTrainingViewModel）全部在一个项目中，编译依赖 TorchSharp |
| P2 | **ScaffoldX.Core 职责混合** | 高 | 模板引擎（TemplateProcessing）、视觉推理（Vision/Sam3Segmentor + TorchSharp 依赖）、文件生成（FileGeneration）耦合在一起。不使用标注功能的场景也被迫引入 TorchSharp-cuda-windows（约 2GB） |
| P3 | **ProjectConfig 上帝对象** | 高 | 208 行单一类，混合了采集驱动配置、视觉相机配置、系统模块配置、UI 导航配置、PLC 连接参数等。每新增一种项目类型或功能模块都要修改此类 |
| P4 | **模板过滤靠字符串 Contains 匹配** | 高 | `TemplateRegistry.ShouldInclude*` 系列方法通过 `name.Contains("S7")`、`name.Contains("MODBUS")` 等硬编码字符串判断模板归属，脆弱且无法扩展。新增驱动类型需要修改 Core 层代码 |
| P5 | **无扩展机制** | 高 | 无法在不修改现有代码的前提下添加新项目类型、新驱动、新功能模块。违反开闭原则 |
| P6 | **ViewModel 直接引用全局 ProjectConfig** | 中 | Step1/Step2/Step3 ViewModel 通过 `Config.SetDriver()`、`Config.SetModule()` 等方法操作共享可变状态，难以测试和追踪变更 |
| P7 | **模板元数据不足** | 中 | TemplateFile 仅有 Name/Category/IsRequired 三个元数据字段，无法表达"此模板需要哪些功能开关才启用"的声明式条件 |
| P8 | **Vision 模块与 Core 强耦合** | 中 | Sam3Segmentor、ImageEmbedding、MaskToPolygonConverter 位于 Core 层，导致 Core 必须引用 TorchSharp-cuda-windows 和 System.Drawing.Common |

---

## 3 目标架构设计

### 3.1 整体架构图

```
+------------------------------------------------------------------+
|                        ScaffoldX.Shell                            |
|  (WPF 主窗口、Prism.Unity 容器、插件宿主、导航框架)               |
|  +------------------------------------------------------------+  |
|  |                  IPluginHost                                |  |
|  |  - RegisterPlugin(IPlugin)                                 |  |
|  |  - NavigateTo(string region, string view)                  |  |
|  |  - GetService<T>()                                         |  |
|  +------------------------------------------------------------+  |
+------------------------------------------------------------------+
           |                    |                    |
           v                    v                    v
+---------------------+ +---------------------+ +---------------------+
| ScaffoldX.Plugin    | | ScaffoldX.Plugin    | | ScaffoldX.Plugin    |
|   .Scaffold         | |   .Annotation       | |   .Training         |
|                     | |                     | |                     |
| - Step1-4 向导      | | - YOLO 标注编辑器   | | - 训练配置          |
| - 模板渲染引擎      | | - SAM3 半自动标注   | | - 脚本生成          |
| - 项目生成器        | | - 多格式导出        | | - 模型导出          |
+---------------------+ +---------------------+ +---------------------+
           |                    |                    |
           v                    v                    v
+---------------------+ +---------------------+ +---------------------+
| ScaffoldX.Plugin    | | ScaffoldX.Plugin    | | ScaffoldX.Plugin    |
|   .Management       | |   .Annotation       | |   .Training         |
|                     | |   .Vision           | |   .Core             |
| - 项目历史          | |                     | |                     |
| - 一键打开          | | - ISam3Engine       | | - ITrainingEngine   |
| - 配置回溯          | | - MaskToPolygon     | | - YoloScriptGen     |
+---------------------+ +---------------------+ +---------------------+
           |                    |                    |
           +--------------------+--------------------+
                                |
                                v
+------------------------------------------------------------------+
|                     ScaffoldX.Abstractions                        |
|  (零依赖接口层：IPlugin, IPluginMetadata, IPluginHost,            |
|   ITemplateProvider, IConfigSection, IFeatureToggle ...)          |
+------------------------------------------------------------------+
                                |
                                v
+------------------------------------------------------------------+
|                     ScaffoldX.Templates                           |
|  (纯资源程序集：.stpl 模板文件 + 声明式元数据 .tmeta.json)         |
+------------------------------------------------------------------+
```

### 3.2 项目结构

```
src/
  ScaffoldX.Abstractions/          -- 零依赖接口层
    Plugins/
      IPlugin.cs
      IPluginHost.cs
      IPluginMetadata.cs
      PluginState.cs
    Templates/
      ITemplateProvider.cs
      ITemplateFilter.cs
      TemplateDescriptor.cs        -- 替代 TemplateFile，含声明式条件
      TemplateMetadata.cs          -- .tmeta.json 的 C# 映射
    Config/
      IConfigSection.cs
      IFeatureToggle.cs
    Services/
      IDialogService.cs
      IValidationService.cs

  ScaffoldX.Shell/                 -- WPF 主应用（极薄宿主层）
    App.xaml / App.xaml.cs
    MainWindow.xaml
    Services/
      PluginHost.cs                -- IPluginHost 实现
      PluginLoader.cs              -- 插件发现与加载
    ViewModels/
      ShellViewModel.cs
    Views/
      ShellView.xaml
      PluginSlot.xaml              -- 动态区域占位

  ScaffoldX.Plugin.Scaffold/       -- 脚手架插件
    ScaffoldPlugin.cs              -- IPlugin 实现
    ViewModels/
      Step1ViewModel.cs
      Step2ViewModel.cs
      Step3ViewModel.cs
      Step4ViewModel.cs
    Views/
      Step1ProjectTypeView.xaml
      Step2BasicInfoView.xaml
      Step3SpecificConfigView.xaml
      Step4ConfirmGenerateView.xaml
    Config/
      ScaffoldConfigSection.cs     -- IConfigSection 实现
    Services/
      ProjectGenerator.cs
      ScribanTemplateEngine.cs
      TemplateProvider.cs          -- ITemplateProvider 实现
    Templates/                     -- 脚手架专属模板（可选，也可留在共享 Templates 项目）

  ScaffoldX.Plugin.Annotation/     -- 标注插件
    AnnotationPlugin.cs
    ViewModels/
      AnnotationViewModel.cs
      ...
    Views/
      AnnotationView.xaml
    Services/
      AnnotationService.cs
      AnnotationExportService.cs
      AutoLabelingService.cs
      VideoFrameService.cs
    FormatExporters/
      YoloFormatExporter.cs
      CocoFormatExporter.cs
      VocFormatExporter.cs
      ...

  ScaffoldX.Plugin.Annotation.Vision/  -- SAM3 视觉推理子插件
    VisionPlugin.cs
    Services/
      Sam3Segmentor.cs
      ImageEmbedding.cs
      MaskToPolygonConverter.cs
      Sam3Tokenizer.cs

  ScaffoldX.Plugin.Training/       -- 训练插件
    TrainingPlugin.cs
    ViewModels/
      YoloTrainingViewModel.cs
    Views/
      YoloTrainingView.xaml
    Services/
      YoloTrainingService.cs
      YoloScriptGenerator.cs
    Config/
      TrainingConfigSection.cs

  ScaffoldX.Plugin.Management/     -- 管理插件
    ManagementPlugin.cs
    ViewModels/
      ProjectHistoryViewModel.cs
    Views/
      ProjectHistoryView.xaml
    Services/
      HistoryService.cs

  ScaffoldX.Templates/             -- 共享模板资源（纯嵌入资源）
    Collection/
    Vision/
    System/
    Common/
    *.tmeta.json                   -- 声明式模板元数据

tests/
  ScaffoldX.Abstractions.Tests/
  ScaffoldX.Plugin.Scaffold.Tests/
  ScaffoldX.Plugin.Annotation.Tests/
  ScaffoldX.Plugin.Training.Tests/
  ScaffoldX.Plugin.Management.Tests/
```

### 3.3 核心接口设计

#### 3.3.1 IPlugin -- 插件契约

```csharp
namespace ScaffoldX.Abstractions.Plugins;

public interface IPlugin
{
    IPluginMetadata Metadata { get; }

    void OnLoaded(IPluginHost host);

    void OnUnloading();
}

public interface IPluginMetadata
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    Version Version { get; }
    string IconKey { get; }

    /// <summary>
    /// 插件排序权重，数值越小越靠前。
    /// Scaffold=10, Annotation=20, Training=30, Management=40。
    /// </summary>
    int Order { get; }

    /// <summary>
    /// 此插件依赖的其他插件 Id 列表。
    /// 例如 Annotation.Vision 依赖 Annotation。
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// 此插件提供的功能开关声明。
    /// 宿主层据此在设置界面动态生成开关 UI。
    /// </summary>
    IReadOnlyList<IFeatureToggle> FeatureToggles { get; }
}
```

#### 3.3.2 IPluginHost -- 宿主服务

```csharp
namespace ScaffoldX.Abstractions.Plugins;

public interface IPluginHost
{
    IContainerProvider Container { get; }

    void RegisterView(string regionName, string viewName, Type viewType);

    void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null);

    T? GetService<T>() where T : class;

    void RegisterConfigSection(IConfigSection section);

    IConfigSection? GetConfigSection(string sectionId);
}
```

#### 3.3.3 IConfigSection -- 配置分片

```csharp
namespace ScaffoldX.Abstractions.Config;

public interface IConfigSection
{
    string SectionId { get; }
    string DisplayName { get; }
    object GetDefaults();
    void Validate(object config, ValidationResult result);
}
```

替代当前 `ProjectConfig` 上帝对象的方案：每个插件定义自己的配置分片。

```csharp
// ScaffoldX.Plugin.Scaffold 中的配置分片
public class ScaffoldConfigSection : IConfigSection
{
    public string SectionId => "Scaffold";
    public string DisplayName => "项目脚手架";

    public object GetDefaults() => new ScaffoldConfig
    {
        ProjectType = string.Empty,
        ProjectName = string.Empty,
        NamespacePrefix = string.Empty,
        TargetFramework = "net10.0-windows",
        UIFramework = "WPF",
        Author = string.Empty,
        Company = string.Empty,
        Description = string.Empty,
        InitGitRepository = true,
        GeneratePublishScripts = true,
    };

    public void Validate(object config, ValidationResult result)
    {
        var c = (ScaffoldConfig)config;
        if (string.IsNullOrWhiteSpace(c.ProjectName))
            result.AddError("ProjectName", "项目名称不能为空");
    }
}

// 采集类配置（独立分片）
public class CollectionConfig : IConfigSection
{
    public string SectionId => "Scaffold.Collection";
    public string DisplayName => "采集类配置";

    public object GetDefaults() => new CollectionConfigData
    {
        Drivers = new List<DriverSelection>
        {
            new() { Key = "S7Net", DisplayName = "Siemens S7", IsSelected = false },
            new() { Key = "ModbusTcp", DisplayName = "Modbus TCP", IsSelected = false },
            new() { Key = "OpcUa", DisplayName = "OPC UA", IsSelected = false },
            new() { Key = "MitsubishiMc", DisplayName = "Mitsubishi MC", IsSelected = false },
            new() { Key = "OmronFins", DisplayName = "Omron FINS", IsSelected = false },
        },
        EnableSimulationDriver = true,
        DefaultPLCIp = "192.168.1.1",
        DefaultPLCPort = 102,
        S7Rack = 0,
        S7Slot = 1,
        OpcUaEndpoint = "opc.tcp://localhost:4840",
    };
}

// 视觉类配置（独立分片）
public class VisionConfig : IConfigSection
{
    public string SectionId => "Scaffold.Vision";
    public string DisplayName => "视觉类配置";

    public object GetDefaults() => new VisionConfigData
    {
        EnableVision = false,
        CameraBrand = "Hikvision",
        ModelType = "Classification",
        EnablePipeline = true,
        ModelPath = string.Empty,
    };
}

// 系统类配置（独立分片）
public class SystemConfig : IConfigSection
{
    public string SectionId => "Scaffold.System";
    public string DisplayName => "系统类配置";

    public object GetDefaults() => new SystemConfigData
    {
        Modules = new List<ModuleSelection>
        {
            new() { Key = "UserManagement", DisplayName = "用户管理", IsSelected = true },
            new() { Key = "RolePermission", DisplayName = "角色权限", IsSelected = true },
            new() { Key = "SystemLog", DisplayName = "审计日志", IsSelected = false },
            new() { Key = "ThemeSwitcher", DisplayName = "主题切换", IsSelected = false },
        },
        DatabaseType = "SQLite",
        EnableLoginWindow = true,
        ForcePasswordChange = false,
        EnableCrossPlatform = false,
    };
}
```

#### 3.3.4 IFeatureToggle -- 功能开关

```csharp
namespace ScaffoldX.Abstractions.Config;

public interface IFeatureToggle
{
    string Key { get; }
    string DisplayName { get; }
    string Description { get; }
    bool DefaultValue { get; }
    string Group { get; }
}
```

#### 3.3.5 ITemplateProvider / ITemplateFilter -- 模板扩展

```csharp
namespace ScaffoldX.Abstractions.Templates;

public interface ITemplateProvider
{
    string Category { get; }
    Task<IReadOnlyList<TemplateDescriptor>> GetTemplatesAsync();
}

public interface ITemplateFilter
{
    bool ShouldInclude(TemplateDescriptor template, IReadOnlyDictionary<string, object> configContext);
}

public class TemplateDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string SubCategory { get; init; } = string.Empty;
    public string OutputPathTemplate { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsRequired { get; init; } = true;

    /// <summary>
    /// 声明式启用条件。空列表表示始终包含。
    /// 每个条件是一个功能开关 Key，全部为 true 时模板才启用。
    /// </summary>
    public IReadOnlyList<string> RequiredFeatures { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 互斥条件。列表中有任一功能开关为 true 时，此模板被排除。
    /// 例如 TopNavView 的 ExcludeWhen 为 ["NavigationStyle:LeftSidebar"]。
    /// </summary>
    public IReadOnlyList<string> ExcludeWhen { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 模板标签，用于自由分类和过滤。
    /// 例如 ["driver", "s7"], ["module", "user-management"], ["nav", "sidebar"]。
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
```

### 3.4 插件生命周期

```
  应用启动
     |
     v
  [1] 扫描插件目录 / 程序集
     |
     v
  [2] 读取 IPluginMetadata（不实例化插件）
     |
     v
  [3] 拓扑排序（按 Dependencies 解析加载顺序）
     |
     v
  [4] 逐个实例化 IPlugin
     |
     v
  [5] 调用 plugin.OnLoaded(host)
      - 插件注册视图到 Region
      - 插件注册 IConfigSection
      - 插件注册 ITemplateProvider（如适用）
      - 插件注册服务到 DI 容器
     |
     v
  [6] Shell 显示主界面，插件 UI 可用
     |
     v
  ... 用户操作 ...
     |
     v
  [7] 应用退出时调用 plugin.OnUnloading()
      - 释放资源（如 TorchSharp 模型）
      - 持久化状态
```

插件加载器核心逻辑：

```csharp
public class PluginLoader
{
    private readonly List<IPlugin> _plugins = new();

    public async Task LoadPluginsAsync(IPluginHost host, string pluginDirectory)
    {
        var assemblies = DiscoverPluginAssemblies(pluginDirectory);
        var metadataList = new List<(Type Type, IPluginMetadata Metadata)>();

        foreach (var assembly in assemblies)
        {
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);
            if (pluginType == null) continue;

            var tempInstance = (IPlugin)Activator.CreateInstance(pluginType)!;
            metadataList.Add((pluginType, tempInstance.Metadata));
        }

        var sorted = TopologicalSort(metadataList);

        foreach (var (type, _) in sorted)
        {
            var plugin = (IPlugin)Activator.CreateInstance(type)!;
            plugin.OnLoaded(host);
            _plugins.Add(plugin);
        }
    }

    public async Task UnloadPluginsAsync()
    {
        foreach (var plugin in Enumerable.Reverse(_plugins))
            plugin.OnUnloading();
        _plugins.Clear();
    }
}
```

### 3.5 模板系统设计

#### 3.5.1 当前问题

当前 `TemplateRegistry.ShouldInclude*` 方法通过硬编码字符串匹配判断模板归属：

```csharp
// 当前实现 -- 脆弱、不可扩展
if (name.Contains("S7") || name.Contains("SIEMENS"))
    return config.EnableSiemensS7;
if (name.Contains("MODBUS"))
    return config.EnableModbusTcp;
```

问题：
- 新增驱动类型需要修改 Core 层代码
- 字符串匹配容易误判（如 "S7" 可能匹配到不相关的模板名）
- 条件逻辑分散在多个 ShouldInclude 方法中

#### 3.5.2 目标方案：声明式元数据

每个模板目录下放置 `.tmeta.json` 文件，声明模板的启用条件：

```json
// Collection/Drivers/SiemensS7/S7Driver.tmeta.json
{
  "id": "S7Driver",
  "name": "S7Driver",
  "category": "Collection",
  "subCategory": "Drivers/SiemensS7",
  "outputPathTemplate": "src/{{project_name}}.Infrastructure/Drivers/S7Driver.cs",
  "isRequired": false,
  "requiredFeatures": [ "EnableSiemensS7" ],
  "tags": [ "driver", "s7", "siemens" ]
}
```

```json
// Common/Shell/SidebarView.tmeta.json
{
  "id": "SidebarView",
  "name": "SidebarView",
  "category": "Common",
  "subCategory": "Shell",
  "outputPathTemplate": "src/{{project_name}}.Modules.Shell/SidebarView.{{xaml_ext}}",
  "isRequired": false,
  "requiredFeatures": [],
  "excludeWhen": [ "NavigationStyle:TopNav" ],
  "tags": [ "nav", "sidebar" ]
}
```

```json
// System/UserManagement/UserService.tmeta.json
{
  "id": "UserService",
  "name": "UserService",
  "category": "System",
  "subCategory": "UserManagement",
  "outputPathTemplate": "src/{{project_name}}.Infrastructure/UserManagement/UserService.cs",
  "isRequired": false,
  "requiredFeatures": [ "EnableUserManagement" ],
  "tags": [ "module", "user-management" ]
}
```

模板过滤引擎替代当前的字符串匹配：

```csharp
public class DeclarativeTemplateFilter : ITemplateFilter
{
    public bool ShouldInclude(TemplateDescriptor template, IReadOnlyDictionary<string, object> configContext)
    {
        if (template.IsRequired)
            return true;

        foreach (var feature in template.RequiredFeatures)
        {
            if (!IsFeatureEnabled(feature, configContext))
                return false;
        }

        foreach (var exclusion in template.ExcludeWhen)
        {
            if (IsConditionMet(exclusion, configContext))
                return false;
        }

        return true;
    }

    private static bool IsFeatureEnabled(string featureKey, IReadOnlyDictionary<string, object> ctx)
    {
        if (!ctx.TryGetValue(featureKey, out var value))
            return false;
        return value is bool b && b;
    }

    private static bool IsConditionMet(string condition, IReadOnlyDictionary<string, object> ctx)
    {
        var parts = condition.Split(':');
        if (parts.Length != 2) return false;
        var key = parts[0];
        var expectedValue = parts[1];
        if (!ctx.TryGetValue(key, out var value)) return false;
        return string.Equals(value?.ToString(), expectedValue, StringComparison.OrdinalIgnoreCase);
    }
}
```

#### 3.5.3 向后兼容

保留 `.stpl` 文件内的 `##OUTPUT:` 和 `##REQUIRED:` 指令作为 fallback。当 `.tmeta.json` 存在时以 JSON 为准，否则从模板文件头部解析。

### 3.6 配置系统设计

#### 3.6.1 配置分片注册表

```csharp
public class ConfigRegistry
{
    private readonly Dictionary<string, IConfigSection> _sections = new();

    public void Register(IConfigSection section)
        => _sections[section.SectionId] = section;

    public IConfigSection? GetSection(string sectionId)
        => _sections.GetValueOrDefault(sectionId);

    public IReadOnlyList<IConfigSection> GetAllSections()
        => _sections.Values.OrderBy(s => s.SectionId).ToList().AsReadOnly();
}
```

#### 3.6.2 配置聚合

生成项目时，`ProjectGenerator` 从各配置分片聚合出完整的 Scriban 变量上下文：

```csharp
public class AggregatedConfigResolver
{
    private readonly ConfigRegistry _registry;

    public Dictionary<string, object> BuildVariableContext(IReadOnlyList<IConfigSection> activeSections)
    {
        var ctx = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var section in activeSections)
        {
            var defaults = section.GetDefaults();
            foreach (var prop in defaults.GetType().GetProperties())
            {
                ctx[prop.Name] = prop.GetValue(defaults)!;
            }
        }

        return ctx;
    }
}
```

#### 3.6.3 配置持久化

每个配置分片独立序列化为 JSON，存储在项目历史记录中：

```json
{
  "projectName": "MyApp",
  "createdAt": "2026-05-08T10:00:00Z",
  "sections": {
    "Scaffold": { "projectType": "Collection", "targetFramework": "net10.0-windows", ... },
    "Scaffold.Collection": { "drivers": ["S7Net", "ModbusTcp"], ... },
    "Scaffold.Vision": { "enableVision": false, ... },
    "Scaffold.System": { "modules": ["UserManagement"], ... }
  }
}
```

---

## 4 各插件详细设计

### 4.1 ScaffoldX.Plugin.Scaffold

**职责**：向导式收集用户需求，基于模板生成完整 .NET 10.0 WPF 项目骨架。

**依赖**：
- ScaffoldX.Abstractions
- ScaffoldX.Templates（编译时引用，运行时通过 ITemplateProvider 提供模板）
- Scriban
- FluentValidation

**不依赖**：TorchSharp、System.Drawing

**核心类**：

| 类 | 职责 |
|----|------|
| `ScaffoldPlugin` | IPlugin 实现，注册 Step1-4 视图和 ScaffoldConfigSection |
| `Step1ViewModel` | 项目类型选择（Collection / Vision / System） |
| `Step2ViewModel` | 基础信息（项目名、命名空间、输出路径等） |
| `Step3ViewModel` | 专项配置（委托给 CollectionConfig / VisionConfig / SystemConfig） |
| `Step4ViewModel` | 确认预览 + 触发生成 |
| `ProjectGenerator` | 六步生成流程：验证 -> 构建上下文 -> 选择模板 -> 渲染 -> 后处理 -> 记录历史 |
| `ScribanTemplateEngine` | Scriban 模板渲染封装 |
| `ScaffoldTemplateProvider` | ITemplateProvider 实现，从 ScaffoldX.Templates 加载模板 |
| `DeclarativeTemplateFilter` | 基于 .tmeta.json 的声明式模板过滤 |

**配置分片**：

| SectionId | 类 | 内容 |
|-----------|-----|------|
| `Scaffold` | `ScaffoldConfig` | 项目名、命名空间、框架、输出路径等 |
| `Scaffold.Collection` | `CollectionConfig` | 驱动选择、PLC 参数、仿真驱动 |
| `Scaffold.Vision` | `VisionConfig` | 相机品牌、模型类型、Pipeline |
| `Scaffold.System` | `SystemConfig` | 功能模块、数据库、登录窗口 |
| `Scaffold.UI` | `UIConfig` | 导航样式、主题、语言、国际化 |

**模板分类体系**：

```
程序类型（第一级）
  +-- Collection（工业采集）
  |     +-- 设备类型（第二级）
  |     |     +-- Core（采集核心：Tag、IDriver、ConnectionParams）
  |     |     +-- Drivers（驱动实现）
  |     |           +-- SiemensS7
  |     |           +-- ModbusTcp
  |     |           +-- OpcUa
  |     |           +-- MitsubishiMc
  |     |           +-- OmronFins
  |     |           +-- Simulation
  |     +-- 功能模块（第三级，可选）
  |
  +-- Vision（视觉检测）
  |     +-- Core（视觉核心：ICameraService）
  |     +-- Camera
  |     |     +-- Simulate（模拟相机）
  |     +-- Inference
  |           +-- InferenceEngineBase
  |           +-- Sam3Engine
  |
  +-- System（系统定制）
  |     +-- Core（IMenuModule、UserRole）
  |     +-- LoginWindow
  |     +-- UserManagement
  |     +-- RolePermission
  |     +-- SystemLog
  |     +-- ThemeSwitcher
  |
  +-- Common（所有类型共享）
        +-- Solution（.sln、Directory.Build.props、global.json）
        +-- App（App.csproj、App.xaml、MainWindow）
        +-- Core（IPlugin、ConfigService、LocalizationService）
        +-- Shell（ShellModule、HomePage、StatusBar、Sidebar/TopNav）
        +-- Diagnostic（日志查看、插件状态）
```

### 4.2 ScaffoldX.Plugin.Annotation

**职责**：YOLO 格式图像标注编辑器，支持矩形框、旋转框、多边形标注。

**依赖**：
- ScaffoldX.Abstractions
- MaterialDesignThemes

**不依赖**：TorchSharp（视觉推理由子插件 Annotation.Vision 提供）

**核心类**：

| 类 | 职责 |
|----|------|
| `AnnotationPlugin` | IPlugin 实现，注册标注视图 |
| `AnnotationViewModel` | 标注主 ViewModel，协调各 Handler |
| `AnnotationService` | 标注 CRUD 操作 |
| `AnnotationExportService` | 多格式导出调度 |
| `IAnnotationExporter` | 导出器接口（YOLO/COCO/VOC/DOT/MOT） |
| `AnnotationRepository` | 标注数据持久化 |
| `VideoFrameService` | 视频帧提取 |
| `DrawingStateManager` | 绘制状态机 |
| `UndoRedoManager` | 撤销/重做栈 |

**导出格式**：

| 格式 | 类 | 用途 |
|------|-----|------|
| YOLO | `YoloFormatExporter` | YOLO 训练标准格式 |
| COCO | `CocoFormatExporter` | 通用标注交换格式 |
| VOC | `VocFormatExporter` | Pascal VOC 格式 |
| DOT | `DotFormatExporter` | 旋转框标注格式 |
| MOT | `MotFormatExporter` | 多目标跟踪格式 |

**配置分片**：

| SectionId | 类 | 内容 |
|-----------|-----|------|
| `Annotation` | `AnnotationConfig` | 默认标注格式、自动保存间隔、SAM3 模型路径 |

### 4.3 ScaffoldX.Plugin.Annotation.Vision

**职责**：SAM3 半自动分割标注能力，作为 Annotation 插件的子插件。

**依赖**：
- ScaffoldX.Abstractions
- ScaffoldX.Plugin.Annotation（编译时引用接口，运行时通过 Dependencies 声明依赖）
- TorchSharp-cuda-windows
- System.Drawing.Common

**核心类**：

| 类 | 职责 |
|----|------|
| `VisionPlugin` | IPlugin 实现，Metadata.Dependencies 包含 "Annotation" |
| `Sam3Segmentor` | SAM3 分割引擎（TorchScript 模型加载与推理） |
| `ImageEmbedding` | 图像编码缓存 |
| `MaskToPolygonConverter` | 二值掩码转多边形轮廓 |
| `Sam3Tokenizer` | BPE 分词器 |
| `AutoLabelingService` | 自动标注服务（文本/点/框/参考图提示） |

**关键设计**：此插件独立于 Annotation 插件，用户可选择不安装。不安装时标注功能仍可使用，只是没有 SAM3 半自动标注能力。

**延迟加载**：TorchSharp 模型仅在用户首次触发 SAM3 功能时加载，不影响应用启动速度。

### 4.4 ScaffoldX.Plugin.Training

**职责**：YOLO 模型训练配置、训练脚本生成、模型导出。

**依赖**：
- ScaffoldX.Abstractions

**不依赖**：TorchSharp（训练通过生成 Python 脚本执行，不在进程内运行）

**核心类**：

| 类 | 职责 |
|----|------|
| `TrainingPlugin` | IPlugin 实现，注册训练视图 |
| `YoloTrainingViewModel` | 训练配置主 ViewModel |
| `YoloTrainingService` | 训练任务管理（启动/停止/监控） |
| `YoloScriptGenerator` | 生成 Python 训练脚本 |
| `TrainingConfigSection` | 训练配置分片 |

**配置分片**：

| SectionId | 类 | 内容 |
|-----------|-----|------|
| `Training` | `TrainingConfig` | 模型变体、Epoch、BatchSize、ImgSize、数据集路径、增强策略 |

### 4.5 ScaffoldX.Plugin.Management

**职责**：已生成项目的管理、历史记录、配置回溯、一键打开。

**依赖**：
- ScaffoldX.Abstractions

**不依赖**：TorchSharp、Scriban

**核心类**：

| 类 | 职责 |
|----|------|
| `ManagementPlugin` | IPlugin 实现，注册管理视图 |
| `ProjectHistoryViewModel` | 历史列表 ViewModel |
| `HistoryService` | 历史记录 CRUD（JSON 文件持久化） |

**配置分片**：无（管理插件不需要用户配置）。

---

## 5 迁移策略

### 5.1 迁移原则

- **渐进式迁移**：每个阶段可独立交付，不中断现有功能
- **先抽象后拆分**：先建立 Abstractions 层接口，再逐步将实现迁移到插件
- **保持编译通过**：每个迁移步骤结束后确保全量编译和测试通过
- **双轨运行**：新旧架构可短暂共存，通过特性开关切换

### 5.2 迁移阶段

#### 阶段一：建立抽象层（1-2 天）

1. 创建 `ScaffoldX.Abstractions` 项目，零依赖
2. 定义 `IPlugin`、`IPluginHost`、`IPluginMetadata`、`IConfigSection`、`ITemplateProvider`、`ITemplateFilter`、`TemplateDescriptor` 等接口
3. 在 `ScaffoldX.App` 中添加对 Abstractions 的引用
4. 现有代码暂不修改，仅确保新接口编译通过

#### 阶段二：拆分配置（2-3 天）

1. 从 `ProjectConfig` 中提取 `ScaffoldConfig`、`CollectionConfig`、`VisionConfig`、`SystemConfig`、`UIConfig`
2. 实现 `IConfigSection` 的各配置分片
3. 创建 `ConfigRegistry` 和 `AggregatedConfigResolver`
4. 修改 `VariableResolver` 从 `ConfigRegistry` 聚合变量，而非直接读取 `ProjectConfig`
5. 修改 Step1-4 ViewModel 使用新的配置分片
6. 确保 `ProjectConfig` 作为聚合对象暂时保留（向后兼容），内部委托到各分片

#### 阶段三：模板元数据声明化（2-3 天）

1. 为 `ScaffoldX.Templates` 中所有模板创建 `.tmeta.json` 文件
2. 实现 `TemplateMetadataLoader` 从嵌入资源加载 `.tmeta.json`
3. 实现 `DeclarativeTemplateFilter`
4. 修改 `TemplateRegistry` 使用 `DeclarativeTemplateFilter` 替代字符串匹配
5. 保留 `ShouldInclude*` 方法作为 fallback，标记 `[Obsolete]`
6. 验证所有模板的过滤结果与旧逻辑一致

#### 阶段四：创建 Shell 宿主（2-3 天）

1. 创建 `ScaffoldX.Shell` 项目
2. 实现 `PluginHost` 和 `PluginLoader`
3. 将 `App.xaml.cs` 中的 DI 注册迁移到 Shell
4. 将 `MainWindow` 迁移到 Shell（重命名为 ShellView）
5. 实现插件 Region 导航

#### 阶段五：拆分 Scaffold 插件（2-3 天）

1. 创建 `ScaffoldX.Plugin.Scaffold` 项目
2. 迁移 Step1-4 ViewModel 和 View
3. 迁移 `ProjectGenerator`、`ScribanTemplateEngine`、`ValidationService`
4. 实现 `ScaffoldPlugin`（IPlugin）
5. 从 `ScaffoldX.App` 中删除已迁移的代码
6. 验证脚手架向导功能完整

#### 阶段六：拆分 Annotation 插件（2-3 天）

1. 创建 `ScaffoldX.Plugin.Annotation` 项目
2. 迁移标注相关 ViewModel、View、Service、FormatExporter
3. 实现 `AnnotationPlugin`
4. 从 `ScaffoldX.App` 中删除已迁移的代码

#### 阶段七：拆分 Vision 子插件（1-2 天）

1. 创建 `ScaffoldX.Plugin.Annotation.Vision` 项目
2. 迁移 `Sam3Segmentor`、`ImageEmbedding`、`MaskToPolygonConverter`、`Sam3Tokenizer`、`AutoLabelingService`
3. 实现 `VisionPlugin`，声明对 Annotation 的依赖
4. 从 `ScaffoldX.Core` 中删除 Vision 目录
5. 从 `ScaffoldX.Core.csproj` 中移除 TorchSharp 和 System.Drawing.Common 依赖

#### 阶段八：拆分 Training 插件（1-2 天）

1. 创建 `ScaffoldX.Plugin.Training` 项目
2. 迁移 `YoloTrainingViewModel`、`YoloTrainingView`、`YoloTrainingService`、`YoloScriptGenerator`
3. 实现 `TrainingPlugin`

#### 阶段九：拆分 Management 插件（1 天）

1. 创建 `ScaffoldX.Plugin.Management` 项目
2. 迁移 `ProjectHistoryViewModel`、`ProjectHistoryView`、`HistoryService`
3. 实现 `ManagementPlugin`

#### 阶段十：清理与验证（1-2 天）

1. 删除 `ScaffoldX.App` 和 `ScaffoldX.Core` 中所有已迁移的代码
2. `ScaffoldX.Core` 仅保留 `FileGeneration` 和 `TemplateProcessing`（或合并到 Scaffold 插件）
3. 全量回归测试
4. 更新 CI/CD 配置
5. 更新文档

### 5.3 迁移风险缓解

| 风险 | 缓解措施 |
|------|----------|
| 迁移期间功能回归 | 每个阶段结束后运行全量测试，对比模板过滤结果 |
| TorchSharp 加载时机变化 | Vision 子插件延迟加载，首次使用时才初始化 |
| Prism Region 导航冲突 | 使用插件 Id 作为 Region 名称前缀，避免冲突 |
| 配置分片序列化格式变更 | 提供配置迁移工具，自动将旧格式 ProjectConfig JSON 转换为分片格式 |

---

## 6 技术选型

### 6.1 WPF + Prism（已有）

Prism.Unity 已在项目中使用，提供 DI 容器、Region 导航、ViewModel 定位等能力。目标架构继续使用 Prism 作为 Shell 层框架。

**插件与 Prism 的集成方式**：

- 每个插件在 `OnLoaded` 中调用 `host.RegisterView(regionName, viewName, viewType)` 注册视图
- Shell 使用 Prism 的 `RegionManager` 实现动态导航
- 插件的服务注册通过 `IContainerRegistry` 完成

### 6.2 插件加载机制

| 方案 | 优点 | 缺点 | 选型 |
|------|------|------|------|
| **MEF (Managed Extensibility Framework)** | .NET 内置、声明式导出导入、成熟 | 与 Prism.Unity 容器双容器问题、调试困难 | 不选 |
| **纯反射** | 简单直接、无额外依赖 | 需手动处理依赖排序、类型转换 | 不选 |
| **自定义 PluginLoader + 反射发现** | 完全可控、与 Unity 容器无缝集成、支持依赖排序 | 需自行实现插件发现和生命周期 | **选用** |

**选用理由**：

1. 项目已有 Prism.Unity，自定义 PluginLoader 直接操作 Unity 容器，避免双容器问题
2. 插件数量有限（4-5 个），不需要 MEF 的自动发现能力
3. 自定义方案可以精确控制加载顺序（拓扑排序）和生命周期
4. 便于实现插件间依赖检查和特性开关

**插件发现策略**：

```csharp
public class PluginLoader
{
    public IReadOnlyList<Assembly> DiscoverPluginAssemblies(string pluginDirectory)
    {
        var assemblies = new List<Assembly>();

        // 策略 1：扫描固定目录下的 DLL
        if (Directory.Exists(pluginDirectory))
        {
            foreach (var dll in Directory.GetFiles(pluginDirectory, "ScaffoldX.Plugin.*.dll"))
            {
                try
                {
                    assemblies.Add(Assembly.LoadFrom(dll));
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "跳过无法加载的插件程序集: {Path}", dll);
                }
            }
        }

        // 策略 2：扫描已加载程序集（开发时便利）
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name?.StartsWith("ScaffoldX.Plugin.") == true)
                assemblies.Add(assembly);
        }

        return assemblies;
    }
}
```

### 6.3 模板引擎（Scriban，已有）

继续使用 Scriban 作为模板渲染引擎。当前 `ScribanTemplateEngine` 封装已足够，无需更换。

**增强点**：

- 模板变量上下文从 `VariableResolver` 聚合各 `IConfigSection` 生成，而非硬编码
- 支持 `ITemplateProvider` 扩展点，允许插件注入自定义模板

### 6.4 DI 容器（Unity，已有）

继续使用 Prism.Unity。插件通过 `IPluginHost.Container` 访问 Unity 容器，注册和解析服务。

**注意事项**：

- 插件注册的服务使用命名注册（以插件 Id 为前缀），避免跨插件命名冲突
- 单例服务的生命周期由 Unity 容器管理，插件 `OnUnloading` 时无需手动释放

---

## 7 风险与权衡

### 7.1 架构风险

| 风险 | 影响 | 概率 | 缓解 |
|------|------|------|------|
| 过度设计：4 个插件对当前团队规模可能过重 | 增加开发和调试复杂度 | 中 | 插件接口保持简单，初期可合并 Annotation + Annotation.Vision 为一个插件 |
| TorchSharp 跨插件加载 | Vision 子插件的 TorchSharp 初始化可能影响主进程 | 低 | Vision 子插件使用独立 AssemblyLoadContext 隔离 |
| Prism Region 导航与插件动态注册冲突 | 插件加载时序可能导致 Region 未就绪 | 中 | Shell 初始化完成后再加载插件，使用 RegionBehavior 延迟注册 |
| 模板元数据维护成本 | 每个模板需要维护 .tmeta.json 文件 | 低 | 提供 T4 模板或 Source Generator 自动生成 .tmeta.json |

### 7.2 权衡决策

| 决策 | 选择 | 放弃 | 理由 |
|------|------|------|------|
| 插件加载 | 自定义 PluginLoader | MEF | 避免 Unity + MEF 双容器，插件数量有限不需要 MEF 的自动发现 |
| 模板过滤 | 声明式 .tmeta.json | 代码内字符串匹配 | 声明式可扩展、可测试、无需修改代码即可新增模板 |
| 配置管理 | 分片式 IConfigSection | 单一 ProjectConfig | 消除上帝对象，支持按需加载，新插件可独立定义配置 |
| Vision 隔离 | 独立子插件 | 合并到 Annotation | TorchSharp 约 2GB 运行时依赖，不使用标注功能的用户无需安装 |
| 模板资源 | 共享 Templates 项目 + .tmeta.json | 每个插件自带模板 | 模板跨项目类型共享（Common），避免重复；.tmeta.json 提供归属声明 |
| 迁移方式 | 渐进式 | 一次性重写 | 保证每个阶段可交付，降低风险 |

### 7.3 未来扩展点

当前设计为以下扩展预留了空间：

1. **新项目类型**：创建新插件 `ScaffoldX.Plugin.Scaffold.Robotics`，实现 `ITemplateProvider` 提供机器人项目模板，无需修改现有代码
2. **新驱动类型**：在 Templates 目录下新增驱动模板目录 + `.tmeta.json`，在 Collection 配置分片中新增 `DriverSelection` 条目
3. **新标注格式**：实现 `IAnnotationExporter` 接口，在 Annotation 插件中注册
4. **新训练引擎**：创建 `ScaffoldX.Plugin.Training.Detectron3`，实现 `ITrainingEngine` 接口
5. **第三方插件**：外部开发者实现 `IPlugin` 接口，将 DLL 放入 plugins 目录即可加载
