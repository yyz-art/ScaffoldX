# ScaffoldX 插件式组合架构重构 -- 产品需求文档 (PRD)

> 版本：1.0 | 日期：2026-05-09 | 状态：Draft
>
> 架构设计参考：[architecture.md](./architecture.md)

---

## Problem Statement

作为 ScaffoldX 的开发者，我在维护和扩展这个工业 AI 开发工作台时面临以下问题：

1. **编译依赖过重**：不使用标注/训练功能的场景也被迫引入 `TorchSharp-cuda-windows`（约 2GB）。`ScaffoldX.Core.csproj` 直接引用了 `TorchSharp-cuda-windows 0.105.0` 和 `System.Drawing.Common 8.0.0`，导致任何引用 Core 的项目都必须承担这些依赖。

2. **无法独立演进功能模块**：脚手架向导（Step1-4）、标注工具（AnnotationViewModel + 9 个 Handler）、训练平台（YoloTrainingViewModel）全部耦合在 `ScaffoldX.App` 中。修改标注功能可能意外影响脚手架生成，反之亦然。

3. **扩展新功能必须修改现有代码**：`TemplateRegistry.ShouldIncludeCollectionTemplate` 通过 `name.Contains("S7")` 等硬编码字符串匹配过滤模板。新增一种驱动（如 AB EtherNet/IP）需要修改 Core 层的 `TemplateRegistry` 类，违反开闭原则。`ProjectConfig.SetDriver` 的 switch-case 同样需要为每种新驱动添加分支。

4. **配置对象膨胀**：`ProjectConfig` 已达 208 行，混合了采集驱动配置（`EnableSiemensS7`、`DefaultPLCIp`）、视觉配置（`CameraBrand`、`ModelType`）、系统模块配置（`EnableUserManagement`）、UI 配置（`NavigationStyle`、`DefaultTheme`）等。每新增一种项目类型或功能模块都要修改此类。

5. **导航硬编码**：`MainWindowViewModel.NavigateTo` 用整数 0-4、10、11 标识页面，新增功能页面需要修改此方法并重新分配编号。

## Solution

将 ScaffoldX 从单体架构重构为**插件式组合架构**，核心思路：

1. **零依赖抽象层**：创建 `ScaffoldX.Abstractions`，定义 `IPlugin`、`IPluginHost`、`IConfigSection`、`ITemplateProvider`、`ITemplateFilter` 等接口，所有插件仅依赖此层。

2. **极薄 Shell 宿主**：创建 `ScaffoldX.Shell`，仅负责插件加载、Region 导航和主窗口框架。`MainWindowViewModel` 的硬编码导航逻辑由 `IPluginHost.NavigateTo` 替代。

3. **功能拆分为独立插件**：脚手架（Scaffold）、标注（Annotation）、视觉推理（Annotation.Vision）、训练（Training）、管理（Management）各自成为独立项目，按需加载。

4. **声明式模板元数据**：用 `.tmeta.json` 替代 `TemplateRegistry` 中的字符串 Contains 匹配，新驱动只需添加模板文件 + 元数据文件，无需修改代码。

5. **配置分片**：用 `IConfigSection` 替代 `ProjectConfig` 上帝对象，每个插件定义自己的配置分片，通过 `ConfigRegistry` 聚合。

---

## User Stories

### 脚手架模块 (Scaffold)

1. As a 工业软件开发者, I want 在向导中选择项目类型和驱动后生成完整的项目骨架, so that 我不需要从零搭建项目结构。

2. As a 工业软件开发者, I want 在不修改 ScaffoldX 源码的前提下添加新的 PLC 驱动模板, so that 我可以快速支持新的设备协议而无需等待框架更新。

3. As a 工业软件开发者, I want 模板过滤逻辑由声明式元数据驱动而非硬编码字符串匹配, so that 模板归属判断不会因为名称中偶然包含关键字而误判。

4. As a 工业软件开发者, I want 配置分片独立于项目类型, so that 新增项目类型不需要修改现有的配置类。

### 标注模块 (Annotation)

5. As a AI 标注员, I want 使用矩形框、旋转框、多边形工具标注图像, so that 我可以为 YOLO 训练准备标注数据。

6. As a AI 标注员, I want 将标注结果导出为 YOLO/COCO/VOC/DOT/MOT 等多种格式, so that 我可以使用不同的训练框架。

7. As a AI 标注员, I want 在不安装 SAM3 视觉推理组件的情况下使用基础标注功能, so that 我的磁盘空间不会被 TorchSharp 的 2GB 运行时占用。

### 视觉推理子插件 (Annotation.Vision)

8. As a AI 标注员, I want 通过文本/点/框提示使用 SAM3 半自动分割标注, so that 我可以大幅提高标注效率。

9. As a AI 标注员, I want SAM3 模型在首次使用时才加载而非应用启动时, so that 应用启动速度不受影响。

### 训练模块 (Training)

10. As a 模型训练工程师, I want 在 ScaffoldX 中配置 YOLO 训练参数并生成训练脚本, so that 我不需要手动编写 Python 训练代码。

11. As a 模型训练工程师, I want 将训练好的模型导出为 ONNX 格式, so that 我可以在工业推理引擎中部署。

### 管理模块 (Management)

12. As a 项目管理者, I want 查看所有已生成项目的历史记录, so that 我可以追溯项目配置和重放生成。

### 架构迁移

13. As a ScaffoldX 开发者, I want 每个迁移阶段可独立交付且不中断现有功能, so that 我可以在迁移过程中持续发布可用版本。

14. As a ScaffoldX 开发者, I want TorchSharp 依赖被隔离在 Vision 子插件中, so that 不使用 SAM3 功能的用户无需安装 CUDA 运行时。

15. As a ScaffoldX 开发者, I want 插件通过 IPlugin 接口自描述其元数据和依赖, so that 宿主层可以自动解析加载顺序并检测缺失依赖。

16. As a ScaffoldX 开发者, I want 新增功能页面时不需要修改 Shell 的导航代码, so that 插件可以自主注册其视图到 Region。

17. As a ScaffoldX 开发者, I want 迁移后的模板过滤结果与旧逻辑完全一致, so that 已有用户不会遇到生成结果变化。

---

## Implementation Decisions

### Modules built/modified

#### 1. ScaffoldX.Abstractions（新建）

零依赖接口层，定义所有插件契约。

| 文件 | 内容 |
|------|------|
| `Plugins/IPlugin.cs` | `IPlugin` 接口：`Metadata`、`OnLoaded(IPluginHost)`、`OnUnloading()` |
| `Plugins/IPluginMetadata.cs` | `IPluginMetadata` 接口：`Id`、`DisplayName`、`Description`、`Version`、`IconKey`、`Order`、`Dependencies`、`FeatureToggles` |
| `Plugins/IPluginHost.cs` | `IPluginHost` 接口：`Container`、`RegisterView()`、`NavigateTo()`、`GetService<T>()`、`RegisterConfigSection()`、`GetConfigSection()` |
| `Plugins/PluginState.cs` | 枚举：`NotLoaded`、`Loading`、`Loaded`、`Unloading`、`Error` |
| `Templates/ITemplateProvider.cs` | `ITemplateProvider` 接口：`Category`、`GetTemplatesAsync()` |
| `Templates/ITemplateFilter.cs` | `ITemplateFilter` 接口：`ShouldInclude(TemplateDescriptor, configContext)` |
| `Templates/TemplateDescriptor.cs` | 替代 `TemplateFile`，新增 `RequiredFeatures`、`ExcludeWhen`、`Tags` |
| `Templates/TemplateMetadata.cs` | `.tmeta.json` 的 C# 映射类 |
| `Config/IConfigSection.cs` | `IConfigSection` 接口：`SectionId`、`DisplayName`、`GetDefaults()`、`Validate()` |
| `Config/IFeatureToggle.cs` | `IFeatureToggle` 接口：`Key`、`DisplayName`、`Description`、`DefaultValue`、`Group` |
| `Config/ConfigRegistry.cs` | 配置分片注册表：`Register()`、`GetSection()`、`GetAllSections()` |
| `Config/AggregatedConfigResolver.cs` | 从各 `IConfigSection` 聚合 Scriban 变量上下文 |
| `Services/IDialogService.cs` | 从 `ScaffoldX.App.Services` 迁移，接口不变 |
| `Services/IValidationService.cs` | 从 `ScaffoldX.App.Services` 迁移，接口签名中 `ProjectConfig` 替换为 `IConfigSection` |

#### 2. ScaffoldX.Shell（新建）

极薄 WPF 宿主应用。

| 文件 | 来源 | 变更 |
|------|------|------|
| `App.xaml` / `App.xaml.cs` | 从 `ScaffoldX.App` 迁移 | 移除所有插件特定的 DI 注册，仅保留 Prism 引导 + 插件加载 |
| `MainWindow.xaml` | 从 `ScaffoldX.App` 迁移 | 移除硬编码的 Step 导航，改为 Region 动态区域 |
| `Services/PluginHost.cs` | 新建 | 实现 `IPluginHost`，封装 `IContainerProvider` + `IRegionManager` |
| `Services/PluginLoader.cs` | 新建 | 插件发现（扫描 `ScaffoldX.Plugin.*.dll`）、拓扑排序、生命周期管理 |
| `ViewModels/ShellViewModel.cs` | 从 `MainWindowViewModel` 精简 | 移除 `CurrentStep` 0-4/10/11 硬编码，改为 Region 导航 |
| `Views/ShellView.xaml` | 从 `MainWindow.xaml` 精简 | 仅保留导航框架 + Region 占位 |

**`MainWindowViewModel` 迁移要点**：

当前 `MainWindowViewModel`（221 行）的核心问题：
- `NavigateTo(int step)` 用硬编码整数映射页面
- `SharedConfig` 作为 `ProjectConfig` 上帝对象在所有 Step ViewModel 间传递
- 直接引用 `AnnotationViewModel` 和 `YoloTrainingViewModel`

迁移后 `ShellViewModel` 仅负责：
- 通过 `IPluginHost.NavigateTo(regionName, viewName)` 切换页面
- 管理插件列表的显示（侧边栏/顶部导航）
- 不持有任何业务 ViewModel 的直接引用

#### 3. ScaffoldX.Plugin.Scaffold（新建）

从 `ScaffoldX.App` 和 `ScaffoldX.Core` 迁移脚手架相关代码。

| 文件 | 来源 | 变更 |
|------|------|------|
| `ScaffoldPlugin.cs` | 新建 | 实现 `IPlugin`，在 `OnLoaded` 中注册 Step1-4 视图和 `ScaffoldConfigSection` |
| `ViewModels/Step1ViewModel.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | `Config` 属性类型从 `ProjectConfig` 改为 `ScaffoldConfig` |
| `ViewModels/Step2ViewModel.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 同上 |
| `ViewModels/Step3ViewModel.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | `CollectionConfigViewModel`/`VisionConfigViewModel`/`SystemConfigViewModel` 改为操作各自 `IConfigSection` |
| `ViewModels/Step4ViewModel.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | `Initialize` 参数从 `ProjectConfig` 改为 `ConfigRegistry` |
| `Views/Step1ProjectTypeView.xaml` | 从 `ScaffoldX.App.Views` 迁移 | 无变更 |
| `Views/Step2BasicInfoView.xaml` | 从 `ScaffoldX.App.Views` 迁移 | 无变更 |
| `Views/Step3SpecificConfigView.xaml` | 从 `ScaffoldX.App.Views` 迁移 | 无变更 |
| `Views/Step4ConfirmGenerateView.xaml` | 从 `ScaffoldX.App.Views` 迁移 | 无变更 |
| `Services/ProjectGenerator.cs` | 从 `ScaffoldX.App.Services` 迁移 | `GenerateAsync` 参数从 `ProjectConfig` 改为 `ConfigRegistry` |
| `Services/ScribanTemplateEngine.cs` | 从 `ScaffoldX.App.Services` 迁移 | 无变更 |
| `Services/ValidationService.cs` | 从 `ScaffoldX.App.Services` 迁移 | `IVariableResolver` 依赖改为 `AggregatedConfigResolver` |
| `Services/ScaffoldTemplateProvider.cs` | 新建 | 实现 `ITemplateProvider`，从 `ScaffoldX.Templates` 加载模板 |
| `Services/DeclarativeTemplateFilter.cs` | 新建 | 实现 `ITemplateFilter`，基于 `TemplateDescriptor.RequiredFeatures`/`ExcludeWhen` 过滤 |
| `Services/TemplateMetadataLoader.cs` | 新建 | 从嵌入资源加载 `.tmeta.json`，fallback 到 `##OUTPUT:`/`##REQUIRED:` 指令 |
| `Config/ScaffoldConfigSection.cs` | 新建 | 实现 `IConfigSection`，SectionId = "Scaffold" |
| `Config/CollectionConfigSection.cs` | 新建 | 实现 `IConfigSection`，SectionId = "Scaffold.Collection" |
| `Config/VisionConfigSection.cs` | 新建 | 实现 `IConfigSection`，SectionId = "Scaffold.Vision" |
| `Config/SystemConfigSection.cs` | 新建 | 实现 `IConfigSection`，SectionId = "Scaffold.System" |
| `Config/UIConfigSection.cs` | 新建 | 实现 `IConfigSection`，SectionId = "Scaffold.UI" |
| `Models/GenerationProgress.cs` | 从 `ScaffoldX.App.Models` 迁移 | 无变更 |
| `Models/GenerationResult.cs` | 从 `ScaffoldX.App.Models` 迁移 | 无变更 |

**`ProjectGenerator` 迁移要点**：

当前 `ProjectGenerator.GenerateAsync`（190 行）的六步流程：
1. 验证配置 → `_validationService.ValidateProjectName(config.ProjectName)`
2. 构建变量上下文 → `_variableResolver.BuildVariableContext(config)`
3. 选择模板 → `_templateRegistry.GetTemplatesForConfig(config)`
4. 渲染模板 → `_templateEngine.Render(template.Content, variables)`
5. 后处理 → `_postProcessor.Process(renderedContent, renderedPath, config)`
6. 记录历史 → `_historyService.SaveAsync(...)`

迁移后变更：
- 步骤 2：`BuildVariableContext` 参数从 `ProjectConfig` 改为 `IReadOnlyList<IConfigSection>`，由 `AggregatedConfigResolver` 聚合
- 步骤 3：`GetTemplatesForConfig` 参数从 `ProjectConfig` 改为 `IReadOnlyDictionary<string, object>`，由 `DeclarativeTemplateFilter` 执行过滤
- 步骤 5：`PostProcessor.Process` 参数从 `ProjectConfig` 改为 `IReadOnlyDictionary<string, object>`
- 步骤 6：`ProjectHistory.ConfigJson` 从序列化整个 `ProjectConfig` 改为序列化 `Dictionary<string, object>` 分片格式

**`TemplateRegistry` 迁移要点**：

当前 `TemplateRegistry`（231 行）的 `ShouldInclude*` 系列方法通过硬编码字符串匹配：
- `ShouldIncludeCollectionTemplate`：`name.Contains("S7")`、`name.Contains("MODBUS")`、`name.Contains("OPCUA")` 等
- `ShouldIncludeSystemTemplate`：`name.Contains("USER")`、`name.Contains("ROLE")`、`name.Contains("LOG")` 等
- `ShouldIncludeShellTemplate`：`name.Contains("SIDEBARVIEW")`、`name.Contains("TOPNAVVIEW")` 等

迁移后由 `DeclarativeTemplateFilter.ShouldInclude` 替代：
- 检查 `TemplateDescriptor.RequiredFeatures`：所有 feature key 在 configContext 中为 true 时才包含
- 检查 `TemplateDescriptor.ExcludeWhen`：任一条件满足时排除
- `IsRequired = true` 的模板始终包含

#### 4. ScaffoldX.Plugin.Annotation（新建）

从 `ScaffoldX.App` 迁移标注相关代码。

| 文件 | 来源 | 变更 |
|------|------|------|
| `AnnotationPlugin.cs` | 新建 | 实现 `IPlugin`，注册标注视图和 `AnnotationConfigSection` |
| `ViewModels/AnnotationViewModel.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 移除对 `IAutoLabelingService` 的直接依赖，改为可选注入 |
| `ViewModels/AnnotationStateVM.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `ViewModels/AnnotationContext.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `ViewModels/ImageStateVM.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `ViewModels/ClassStateVM.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `ViewModels/DrawingStateManager.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `ViewModels/UndoRedoManager.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `Handlers/AutoLabelingCommandHandler.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | `IAutoLabelingService` 改为可选依赖 |
| `Handlers/Sam3LabelingCommandHandler.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | `IAutoLabelingService` 改为可选依赖 |
| `Handlers/ImageNavigationHandler.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `Handlers/ClassManagementHandler.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `Handlers/PolygonDrawingHandler.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `Handlers/ObbDrawingHandler.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `Handlers/UndoRedoHandler.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `Handlers/ExportCommandHandler.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `Handlers/ReviewCommandHandler.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `Handlers/ProjectCommandHandler.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `Views/AnnotationView.xaml` | 从 `ScaffoldX.App.Views` 迁移 | 无变更 |
| `Services/AnnotationService.cs` | 从 `ScaffoldX.App.Services` 迁移 | 无变更 |
| `Services/AnnotationExportService.cs` | 从 `ScaffoldX.App.Services` 迁移 | 无变更 |
| `Services/AnnotationRepository.cs` | 从 `ScaffoldX.App.Services` 迁移 | 无变更 |
| `Services/VideoFrameService.cs` | 从 `ScaffoldX.App.Services` 迁移 | 无变更 |
| `Services/CoordinateMapper.cs` | 从 `ScaffoldX.App.Services` 迁移 | 无变更 |
| `FormatExporters/YoloFormatExporter.cs` | 从 `ScaffoldX.App.Services.FormatExporters` 迁移 | 无变更 |
| `FormatExporters/CocoFormatExporter.cs` | 从 `ScaffoldX.App.Services.FormatExporters` 迁移 | 无变更 |
| `FormatExporters/VocFormatExporter.cs` | 从 `ScaffoldX.App.Services.FormatExporters` 迁移 | 无变更 |
| `FormatExporters/DotFormatExporter.cs` | 从 `ScaffoldX.App.Services.FormatExporters` 迁移 | 无变更 |
| `FormatExporters/MotFormatExporter.cs` | 从 `ScaffoldX.App.Services.FormatExporters` 迁移 | 无变更 |
| `Config/AnnotationConfigSection.cs` | 新建 | 实现 `IConfigSection`，SectionId = "Annotation" |

**`AnnotationViewModel` 迁移要点**：

当前构造函数签名：
```csharp
public AnnotationViewModel(
    IAnnotationService annotationService,
    IAutoLabelingService autoLabelingService,
    IVideoFrameService videoFrameService,
    IDialogService dialogService)
```

迁移后 `IAutoLabelingService` 变为可选依赖：
```csharp
public AnnotationViewModel(
    IAnnotationService annotationService,
    IVideoFrameService videoFrameService,
    IDialogService dialogService,
    IAutoLabelingService? autoLabelingService = null)
```

当 `autoLabelingService` 为 null 时，SAM3 相关按钮和菜单项禁用。

#### 5. ScaffoldX.Plugin.Annotation.Vision（新建）

从 `ScaffoldX.Core.Vision` 迁移 SAM3 视觉推理组件。

| 文件 | 来源 | 变更 |
|------|------|------|
| `VisionPlugin.cs` | 新建 | 实现 `IPlugin`，`Metadata.Dependencies` 包含 "Annotation"，注册 `IAutoLabelingService` |
| `Services/Sam3Segmentor.cs` | 从 `ScaffoldX.Core.Vision` 迁移 | 无变更（仍依赖 TorchSharp） |
| `Services/ImageEmbedding.cs` | 从 `ScaffoldX.Core.Vision` 迁移 | 无变更 |
| `Services/MaskToPolygonConverter.cs` | 从 `ScaffoldX.Core.Vision` 迁移 | 无变更 |
| `Services/Sam3Tokenizer.cs` | 从 `ScaffoldX.Core.Vision` 迁移 | 无变更 |
| `Services/AutoLabelingService.cs` | 从 `ScaffoldX.App.Services` 迁移 | 无变更 |

**关键设计**：此插件是唯一引用 `TorchSharp-cuda-windows` 的项目。用户可选择不安装此插件，此时标注功能仍可使用，只是没有 SAM3 半自动标注能力。

#### 6. ScaffoldX.Plugin.Training（新建）

从 `ScaffoldX.App` 迁移训练相关代码。

| 文件 | 来源 | 变更 |
|------|------|------|
| `TrainingPlugin.cs` | 新建 | 实现 `IPlugin`，注册训练视图和 `TrainingConfigSection` |
| `ViewModels/YoloTrainingViewModel.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `ViewModels/YoloTrainingViewModel.Commands.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `ViewModels/TrainingConfigViewModel.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `Views/YoloTrainingView.xaml` | 从 `ScaffoldX.App.Views` 迁移 | 无变更 |
| `Services/YoloTrainingService.cs` | 从 `ScaffoldX.App.Services` 迁移 | 无变更 |
| `Services/YoloScriptGenerator.cs` | 从 `ScaffoldX.App.Services` 迁移 | 无变更 |
| `Config/TrainingConfigSection.cs` | 新建 | 实现 `IConfigSection`，SectionId = "Training" |

#### 7. ScaffoldX.Plugin.Management（新建）

从 `ScaffoldX.App` 迁移项目管理相关代码。

| 文件 | 来源 | 变更 |
|------|------|------|
| `ManagementPlugin.cs` | 新建 | 实现 `IPlugin`，注册管理视图 |
| `ViewModels/ProjectHistoryViewModel.cs` | 从 `ScaffoldX.App.ViewModels` 迁移 | 无变更 |
| `Views/ProjectHistoryView.xaml` | 从 `ScaffoldX.App.Views` 迁移 | 无变更 |
| `Services/HistoryService.cs` | 从 `ScaffoldX.App.Services` 迁移 | 无变更 |

#### 8. ScaffoldX.Core（修改后大幅精简）

迁移完成后，`ScaffoldX.Core` 中的代码全部迁出到各插件。此项目最终可删除或仅保留 `FileGeneration` 和 `TemplateProcessing` 供 Scaffold 插件内部使用（合并到 `ScaffoldX.Plugin.Scaffold` 中）。

**需从 `ScaffoldX.Core.csproj` 移除的依赖**：
- `TorchSharp-cuda-windows 0.105.0` → 迁移到 `ScaffoldX.Plugin.Annotation.Vision`
- `System.Drawing.Common 8.0.0` → 迁移到 `ScaffoldX.Plugin.Annotation.Vision`
- `Scriban 5.10.0` → 迁移到 `ScaffoldX.Plugin.Scaffold`
- `FluentValidation 11.9.0` → 迁移到 `ScaffoldX.Plugin.Scaffold`

#### 9. ScaffoldX.Templates（新增 .tmeta.json 文件）

模板 .stpl 文件保持不变，每个模板目录下新增 `.tmeta.json` 元数据文件。

需要创建的 `.tmeta.json` 文件（基于现有模板目录结构）：

| 模板路径 | requiredFeatures | excludeWhen |
|----------|-----------------|-------------|
| `Collection/Drivers/SiemensS7/*.tmeta.json` | `["EnableSiemensS7"]` | - |
| `Collection/Drivers/ModbusTcp/*.tmeta.json` | `["EnableModbusTcp"]` | - |
| `Collection/Drivers/OpcUa/*.tmeta.json` | `["EnableOpcUa"]` | - |
| `Collection/Drivers/MitsubishiMc/*.tmeta.json` | `["EnableMitsubishiMc"]` | - |
| `Collection/Drivers/OmronFins/*.tmeta.json` | `["EnableOmronFins"]` | - |
| `Collection/Drivers/Simulation/*.tmeta.json` | `["EnableSimulationDriver"]` | - |
| `Collection/Core/*.tmeta.json` | `[]` (IsRequired=true) | - |
| `Vision/Core/*.tmeta.json` | `["EnableVision"]` | - |
| `Vision/Camera/Simulate/*.tmeta.json` | `["EnableVision"]` | - |
| `Vision/Inference/*.tmeta.json` | `["EnableVision"]` | - |
| `System/Core/*.tmeta.json` | `[]` (IsRequired=true) | - |
| `System/UserManagement/*.tmeta.json` | `["EnableUserManagement"]` | - |
| `System/RolePermission/*.tmeta.json` | `["EnableRolePermission"]` | - |
| `System/SystemLog/*.tmeta.json` | `["EnableSystemLog"]` | - |
| `System/ThemeSwitcher/*.tmeta.json` | `["EnableThemeSwitcher"]` | - |
| `System/LoginWindow/*.tmeta.json` | `["EnableLoginWindow"]` | - |
| `Common/Shell/SidebarView*.tmeta.json` | `[]` | `["NavigationStyle:TopNav"]` |
| `Common/Shell/TopNavView*.tmeta.json` | `[]` | `["NavigationStyle:LeftSidebar"]` |
| `Common/App/Strings*.tmeta.json` | `["EnableLocalization"]` | - |

### Interface changes

#### 旧接口 → 新接口映射

| 旧接口 | 旧命名空间 | 新接口 | 新命名空间 | 变更说明 |
|--------|-----------|--------|-----------|----------|
| `ITemplateRegistry` | `ScaffoldX.Core.TemplateProcessing` | `ITemplateProvider` + `ITemplateFilter` | `ScaffoldX.Abstractions.Templates` | 拆分为提供者和过滤者两个角色；`GetTemplatesForConfig(ProjectConfig)` → `ITemplateFilter.ShouldInclude(TemplateDescriptor, configContext)` |
| `ITemplateSource` | `ScaffoldX.Core.TemplateProcessing` | `ITemplateProvider` | `ScaffoldX.Abstractions.Templates` | `LoadTemplatesAsync()` → `GetTemplatesAsync()`，返回类型从 `IReadOnlyList<TemplateFile>` 改为 `IReadOnlyList<TemplateDescriptor>` |
| `IVariableResolver` | `ScaffoldX.Core.TemplateProcessing` | `AggregatedConfigResolver` | `ScaffoldX.Abstractions.Config` | `BuildVariableContext(ProjectConfig)` → `BuildVariableContext(IReadOnlyList<IConfigSection>)`，不再依赖 `ProjectConfig` |
| `IPostProcessor` | `ScaffoldX.Core.TemplateProcessing` | `IPostProcessor` | `ScaffoldX.Abstractions.Services` | `Process(string, string, ProjectConfig)` → `Process(string, string, IReadOnlyDictionary<string,object>)`，第三个参数从 `ProjectConfig` 改为配置上下文字典 |
| `IFileTreeBuilder` | `ScaffoldX.Core.FileGeneration` | `IFileTreeBuilder` | `ScaffoldX.Plugin.Scaffold.Services` | `BuildTree(ProjectConfig)` → `BuildTree(ConfigRegistry)`，参数从 `ProjectConfig` 改为 `ConfigRegistry` |
| `IProjectGenerator` | `ScaffoldX.App.Services` | `IProjectGenerator` | `ScaffoldX.Plugin.Scaffold.Services` | `GenerateAsync(ProjectConfig, IProgress<>)` → `GenerateAsync(ConfigRegistry, IProgress<>)` |
| `ITemplateEngine` | `ScaffoldX.App.Services` | `ITemplateEngine` | `ScaffoldX.Plugin.Scaffold.Services` | 无变更 |
| `IValidationService` | `ScaffoldX.App.Services` | `IValidationService` | `ScaffoldX.Abstractions.Services` | 接口签名不变，实现类迁移到 `ScaffoldX.Plugin.Scaffold` |
| `IHistoryService` | `ScaffoldX.App.Services` | `IHistoryService` | `ScaffoldX.Plugin.Management.Services` | 接口签名不变，实现类迁移到 `ScaffoldX.Plugin.Management` |
| `IAnnotationService` | `ScaffoldX.App.Services` | `IAnnotationService` | `ScaffoldX.Plugin.Annotation.Services` | 接口签名不变 |
| `IAnnotationExporter` | `ScaffoldX.App.Services` | `IAnnotationExporter` | `ScaffoldX.Plugin.Annotation.Services` | 接口签名不变 |
| `IAnnotationRepository` | `ScaffoldX.App.Services` | `IAnnotationRepository` | `ScaffoldX.Plugin.Annotation.Services` | 接口签名不变 |
| `IAutoLabelingService` | `ScaffoldX.App.Services` | `IAutoLabelingService` | `ScaffoldX.Plugin.Annotation.Vision.Services` | 接口签名不变，实现类迁移到 Vision 子插件 |
| `IYoloTrainingService` | `ScaffoldX.App.Services` | `IYoloTrainingService` | `ScaffoldX.Plugin.Training.Services` | 接口签名不变 |
| `IVideoFrameService` | `ScaffoldX.App.Services` | `IVideoFrameService` | `ScaffoldX.Plugin.Annotation.Services` | 接口签名不变 |
| `IDialogService` | `ScaffoldX.App.Services` | `IDialogService` | `ScaffoldX.Abstractions.Services` | 接口签名不变，提升到抽象层 |
| `ISam3SegmentationEngine` | `ScaffoldX.Core.Vision` | `ISam3SegmentationEngine` | `ScaffoldX.Plugin.Annotation.Vision.Services` | 接口签名不变 |

#### 数据模型变更

| 旧类 | 新类 | 变更说明 |
|------|------|----------|
| `ProjectConfig`（208 行） | 拆分为 `ScaffoldConfig` + `CollectionConfig` + `VisionConfig` + `SystemConfig` + `UIConfig` | 每个配置类实现 `IConfigSection`，通过 `ConfigRegistry` 聚合 |
| `TemplateFile` | `TemplateDescriptor` | 新增 `Id`、`SubCategory`、`RequiredFeatures`、`ExcludeWhen`、`Tags` 字段 |
| `ProjectHistory` | `ProjectHistory` | `ConfigJson` 从序列化 `ProjectConfig` 改为序列化分片格式 `Dictionary<string, object>` |
| `ValidationResult` | `ValidationResult` | 从 `ScaffoldX.App.Services` 迁移到 `ScaffoldX.Abstractions.Services`，结构不变 |

### Technical clarifications

1. **Prism Region 与插件视图注册**：每个插件在 `OnLoaded` 中调用 `host.RegisterView(regionName, viewName, viewType)`，内部通过 `IRegionManager.RegisterViewWithRegion()` 实现。Region 名称约定：`"MainContent"` 用于主内容区，`"Navigation"` 用于侧边栏/顶部导航。

2. **配置分片的变量聚合**：`AggregatedConfigResolver.BuildVariableContext` 遍历所有活跃的 `IConfigSection`，通过反射提取每个分片 `GetDefaults()` 返回对象的所有属性，合并为 `Dictionary<string, object>`。属性名冲突时，后注册的分片覆盖先注册的。

3. **.tmeta.json fallback 策略**：当 `.tmeta.json` 不存在时，`TemplateMetadataLoader` 从 `.stpl` 文件头部解析 `##OUTPUT:` 和 `##REQUIRED:` 指令（与当前 `AssemblyTemplateSource.ExtractOutputPathTemplate`/`ExtractIsRequired` 逻辑一致），构造 `TemplateDescriptor` 实例。

4. **TorchSharp 延迟加载**：`VisionPlugin.OnLoaded` 不加载模型。`Sam3Segmentor.LoadModelAsync` 仅在用户首次触发 SAM3 功能时调用，与当前实现一致（`_isLoaded` 标志 + `SemaphoreSlim` 锁）。

5. **插件间通信**：`Annotation.Vision` 通过 `IPluginHost.GetService<IAutoLabelingService>()` 向 Annotation 插件提供服务。Annotation 插件在构造 `AnnotationViewModel` 时可选注入 `IAutoLabelingService`。

### Architectural decisions

| 决策 | 选择 | 放弃 | 理由 |
|------|------|------|------|
| 插件加载 | 自定义 `PluginLoader` + 反射发现 | MEF | 项目已有 Prism.Unity，自定义 PluginLoader 直接操作 Unity 容器，避免双容器问题。插件数量有限（4-5 个），不需要 MEF 的自动发现能力 |
| 模板过滤 | 声明式 `.tmeta.json` + `DeclarativeTemplateFilter` | 代码内字符串 Contains 匹配 | 声明式可扩展、可测试、无需修改代码即可新增模板 |
| 配置管理 | 分片式 `IConfigSection` + `ConfigRegistry` | 单一 `ProjectConfig` | 消除上帝对象，支持按需加载，新插件可独立定义配置 |
| Vision 隔离 | 独立子插件 `Annotation.Vision` | 合并到 `Annotation` | TorchSharp 约 2GB 运行时依赖，不使用标注功能的用户无需安装 |
| 模板资源 | 共享 `ScaffoldX.Templates` 项目 + `.tmeta.json` | 每个插件自带模板 | 模板跨项目类型共享（Common 目录），避免重复 |
| 迁移方式 | 渐进式 10 阶段 | 一次性重写 | 保证每个阶段可交付，降低风险 |

---

## Testing Decisions

### What makes a good test

1. **确定性**：测试结果不依赖文件系统状态、网络、GPU 等外部因素。涉及文件 I/O 的测试使用临时目录并在 TearDown 中清理。
2. **隔离性**：每个测试用例独立运行，不依赖其他测试的执行顺序或副作用。
3. **具体断言**：断言必须具体到预期值，避免 `Assert.NotNull` 或 `Assert.IsTrue(result)` 等模糊断言。
4. **覆盖边界**：除正常路径外，必须覆盖空输入、null 输入、极端值、格式错误等边界情况。
5. **回归保障**：迁移相关测试必须对比新旧实现的输出一致性，确保行为不变。

### Modules to test

#### P0 -- 抽象层接口契约测试（ScaffoldX.Abstractions.Tests）

**IPlugin 契约测试**

```csharp
// IPlugin_OnLoaded_ReceivesNonNullHost
// 输入：mock IPluginHost
// 预期：OnLoaded 被调用，host 参数非 null

// IPlugin_OnUnloading_CalledAfterOnLoaded
// 输入：先调用 OnLoaded，再调用 OnUnloading
// 预期：OnUnloading 正常执行，无异常

// IPluginMetadata_Dependencies_ContainsValidPluginIds
// 输入：各插件的 IPluginMetadata 实现
// 预期：Dependencies 中的每个 Id 都对应当前已加载的插件
```

**IPluginHost 契约测试**

```csharp
// IPluginHost_RegisterView_AddsViewToRegion
// 输入：regionName="MainContent", viewName="Step1", viewType=typeof(Step1ProjectTypeView)
// 预期：IRegionManager 中 "MainContent" region 包含名为 "Step1" 的视图

// IPluginHost_NavigateTo_ActivatesView
// 输入：regionName="MainContent", viewName="Step1"
// 预期：当前活跃视图为 Step1ProjectTypeView

// IPluginHost_GetService_ReturnsRegisteredService
// 输入：注册 IValidationService → ValidationService
// 预期：GetService<IValidationService>() 返回 ValidationService 实例

// IPluginHost_RegisterConfigSection_StoresSection
// 输入：注册 ScaffoldConfigSection (SectionId="Scaffold")
// 预期：GetConfigSection("Scaffold") 返回同一实例

// IPluginHost_GetConfigSection_ReturnsNullForUnknownId
// 输入：GetConfigSection("NonExistent")
// 预期：返回 null
```

**IConfigSection 契约测试**

```csharp
// IConfigSection_GetDefaults_ReturnsNonNullObject
// 输入：各 IConfigSection 实现
// 预期：GetDefaults() 返回非 null 对象

// IConfigSection_Validate_ValidInput_ReturnsNoErrors
// 输入：ScaffoldConfigSection + 有效的 ScaffoldConfig
// 预期：ValidationResult 无错误

// IConfigSection_Validate_EmptyProjectName_ReturnsError
// 输入：ScaffoldConfigSection + ScaffoldConfig { ProjectName = "" }
// 预期：ValidationResult 包含 "ProjectName" 错误

// IConfigSection_SectionId_IsUnique
// 输入：所有 IConfigSection 实现的 SectionId
// 预期：无重复 SectionId
```

**ITemplateFilter 契约测试**

```csharp
// ITemplateFilter_ShouldInclude_RequiredTemplate_ReturnsTrue
// 输入：TemplateDescriptor { IsRequired=true, RequiredFeatures=["EnableSiemensS7"] }, configContext={}
// 预期：返回 true（IsRequired 优先）

// ITemplateFilter_ShouldInclude_AllFeaturesEnabled_ReturnsTrue
// 输入：TemplateDescriptor { IsRequired=false, RequiredFeatures=["EnableSiemensS7","EnableModbusTcp"] },
//       configContext={"EnableSiemensS7":true, "EnableModbusTcp":true}
// 预期：返回 true

// ITemplateFilter_ShouldInclude_FeatureDisabled_ReturnsFalse
// 输入：TemplateDescriptor { IsRequired=false, RequiredFeatures=["EnableSiemensS7"] },
//       configContext={"EnableSiemensS7":false}
// 预期：返回 false

// ITemplateFilter_ShouldInclude_ExcludeWhenConditionMet_ReturnsFalse
// 输入：TemplateDescriptor { IsRequired=false, RequiredFeatures=[], ExcludeWhen=["NavigationStyle:TopNav"] },
//       configContext={"NavigationStyle":"TopNav"}
// 预期：返回 false

// ITemplateFilter_ShouldInclude_ExcludeWhenConditionNotMet_ReturnsTrue
// 输入：TemplateDescriptor { IsRequired=false, RequiredFeatures=[], ExcludeWhen=["NavigationStyle:TopNav"] },
//       configContext={"NavigationStyle":"LeftSidebar"}
// 预期：返回 true

// ITemplateFilter_ShouldInclude_EmptyRequiredFeatures_ReturnsTrue
// 输入：TemplateDescriptor { IsRequired=false, RequiredFeatures=[], ExcludeWhen=[] }, configContext={}
// 预期：返回 true

// ITemplateFilter_ShouldInclude_FeatureKeyNotInContext_ReturnsFalse
// 输入：TemplateDescriptor { IsRequired=false, RequiredFeatures=["UnknownFeature"] }, configContext={}
// 预期：返回 false
```

**ConfigRegistry 测试**

```csharp
// ConfigRegistry_Register_AddsSection
// 输入：注册 ScaffoldConfigSection
// 预期：GetSection("Scaffold") 返回该实例

// ConfigRegistry_Register_DuplicateSectionId_Overwrites
// 输入：注册两个 SectionId="Scaffold" 的分片
// 预期：GetSection("Scaffold") 返回后注册的实例

// ConfigRegistry_GetAllSections_ReturnsAllRegistered
// 输入：注册 3 个分片
// 预期：GetAllSections().Count == 3

// ConfigRegistry_GetSection_UnknownId_ReturnsNull
// 输入：GetSection("NonExistent")
// 预期：返回 null
```

**AggregatedConfigResolver 测试**

```csharp
// AggregatedConfigResolver_BuildVariableContext_SingleSection
// 输入：[ScaffoldConfigSection]，其 GetDefaults() 返回 { ProjectName="TestApp", TargetFramework="net10.0-windows" }
// 预期：结果字典包含 "ProjectName"="TestApp", "TargetFramework"="net10.0-windows"

// AggregatedConfigResolver_BuildVariableContext_MultipleSections_NoConflict
// 输入：[ScaffoldConfigSection, CollectionConfigSection]
// 预期：结果字典包含两个分片的所有属性

// AggregatedConfigResolver_BuildVariableContext_PropertyConflict_LastWins
// 输入：两个分片都有 "ProjectName" 属性
// 预期：后注册的分片值覆盖先注册的

// AggregatedConfigResolver_BuildVariableContext_EmptySections_ReturnsEmptyDict
// 输入：[]
// 预期：返回空字典
```

#### P0 -- DeclarativeTemplateFilter 单元测试（ScaffoldX.Plugin.Scaffold.Tests）

```csharp
// DeclarativeTemplateFilter_S7DriverTemplate_WhenS7Enabled_ReturnsTrue
// 输入：TemplateDescriptor { Name="S7Driver", RequiredFeatures=["EnableSiemensS7"] },
//       configContext={"EnableSiemensS7":true}
// 预期：true

// DeclarativeTemplateFilter_S7DriverTemplate_WhenS7Disabled_ReturnsFalse
// 输入：TemplateDescriptor { Name="S7Driver", RequiredFeatures=["EnableSiemensS7"] },
//       configContext={"EnableSiemensS7":false}
// 预期：false

// DeclarativeTemplateFilter_SidebarView_WhenTopNav_ReturnsFalse
// 输入：TemplateDescriptor { Name="SidebarView", ExcludeWhen=["NavigationStyle:TopNav"] },
//       configContext={"NavigationStyle":"TopNav"}
// 预期：false

// DeclarativeTemplateFilter_TopNavView_WhenLeftSidebar_ReturnsFalse
// 输入：TemplateDescriptor { Name="TopNavView", ExcludeWhen=["NavigationStyle:LeftSidebar"] },
//       configContext={"NavigationStyle":"LeftSidebar"}
// 预期：false

// DeclarativeTemplateFilter_LocalizationTemplate_WhenEnabled_ReturnsTrue
// 输入：TemplateDescriptor { Name="StringsZhCnResx", RequiredFeatures=["EnableLocalization"] },
//       configContext={"EnableLocalization":true}
// 预期：true

// DeclarativeTemplateFilter_UserMgmtTemplate_WhenEnabled_ReturnsTrue
// 输入：TemplateDescriptor { Name="UserService", RequiredFeatures=["EnableUserManagement"] },
//       configContext={"EnableUserManagement":true}
// 预期：true

// DeclarativeTemplateFilter_CommonRequiredTemplate_AlwaysIncluded
// 输入：TemplateDescriptor { Name="AppCsproj", IsRequired=true, RequiredFeatures=[] },
//       configContext={}
// 预期：true
```

#### P0 -- 模板过滤回归测试（关键：确保迁移不破坏现有行为）

对比 `TemplateRegistry.ShouldInclude*` 和 `DeclarativeTemplateFilter.ShouldInclude` 的输出一致性：

```csharp
// Regression_CollectionS7Driver_OldAndNewFilterMatch
// 输入：name="S7Driver", config={EnableSiemensS7=true, ProjectType="Collection"}
// 旧逻辑：ShouldIncludeCollectionTemplate → name.Contains("S7") → true
// 新逻辑：DeclarativeTemplateFilter → RequiredFeatures=["EnableSiemensS7"], context["EnableSiemensS7"]=true → true
// 预期：两者结果一致

// Regression_CollectionModbusDriver_OldAndNewFilterMatch
// 输入：name="ModbusTcpDriver", config={EnableModbusTcp=true, ProjectType="Collection"}
// 旧逻辑：name.Contains("MODBUS") → true
// 新逻辑：RequiredFeatures=["EnableModbusTcp"], context["EnableModbusTcp"]=true → true
// 预期：两者结果一致

// Regression_SystemUserMgmt_OldAndNewFilterMatch
// 输入：name="UserService", config={EnableUserManagement=true}
// 旧逻辑：name.Contains("USER") → true
// 新逻辑：RequiredFeatures=["EnableUserManagement"], context["EnableUserManagement"]=true → true
// 预期：两者结果一致

// Regression_ShellSidebarView_LeftSidebar_OldAndNewFilterMatch
// 输入：name="SidebarView", config={NavigationStyle="LeftSidebar"}
// 旧逻辑：IsShellNavigationTemplate → ShouldIncludeShellTemplate → !isTopNav → true
// 新逻辑：ExcludeWhen=["NavigationStyle:TopNav"], context["NavigationStyle"]="LeftSidebar" → true
// 预期：两者结果一致

// Regression_ShellTopNavView_TopNav_OldAndNewFilterMatch
// 输入：name="TopNavView", config={NavigationStyle="TopNav"}
// 旧逻辑：isTopNav → true
// 新逻辑：ExcludeWhen=["NavigationStyle:LeftSidebar"], context["NavigationStyle"]="TopNav" → true
// 预期：两者结果一致

// Regression_FullTemplateSet_CollectionProject_OldAndNewFilterMatch
// 输入：ProjectConfig { ProjectType="Collection", EnableSiemensS7=true, EnableModbusTcp=true, ... }
// 旧逻辑：TemplateRegistry.GetTemplatesForConfig(config) 返回的模板名集合
// 新逻辑：DeclarativeTemplateFilter 过滤后的模板名集合
// 预期：两个集合完全一致

// Regression_FullTemplateSet_VisionProject_OldAndNewFilterMatch
// 输入：ProjectConfig { ProjectType="Vision", EnableVision=true, CameraBrand="Hikvision", ... }
// 预期：两个集合完全一致

// Regression_FullTemplateSet_SystemProject_OldAndNewFilterMatch
// 输入：ProjectConfig { ProjectType="System", EnableUserManagement=true, EnableRolePermission=true, ... }
// 预期：两个集合完全一致
```

#### P1 -- PluginLoader 单元测试（ScaffoldX.Shell.Tests）

```csharp
// PluginLoader_DiscoverPluginAssemblies_FindsAllPluginDlls
// 输入：临时目录包含 ScaffoldX.Plugin.Scaffold.dll, ScaffoldX.Plugin.Annotation.dll
// 预期：返回 2 个程序集

// PluginLoader_DiscoverPluginAssemblies_IgnoresNonPluginDlls
// 输入：临时目录包含 ScaffoldX.App.dll, Newtonsoft.Json.dll
// 预期：返回 0 个程序集

// PluginLoader_LoadPluginsAsync_CorrectOrder
// 输入：Annotation.Vision (Dependencies=["Annotation"]), Annotation (Dependencies=[])
// 预期：加载顺序为 Annotation → Annotation.Vision

// PluginLoader_LoadPluginsAsync_MissingDependency_Throws
// 输入：Annotation.Vision (Dependencies=["Annotation"])，但 Annotation 插件不存在
// 预期：抛出 InvalidOperationException，提示缺少依赖 "Annotation"

// PluginLoader_LoadPluginsAsync_CircularDependency_Throws
// 输入：PluginA (Dependencies=["PluginB"]), PluginB (Dependencies=["PluginA"])
// 预期：抛出 InvalidOperationException，提示循环依赖

// PluginLoader_UnloadPluginsAsync_ReverseOrder
// 输入：按 A → B → C 顺序加载
// 预期：OnUnloading 调用顺序为 C → B → A
```

#### P1 -- PluginHost 单元测试（ScaffoldX.Shell.Tests）

```csharp
// PluginHost_RegisterView_RegionExists_AddsView
// 输入：regionName="MainContent", viewName="Step1", viewType=typeof(MockView)
// 预期：IRegionManager 中 "MainContent" region 包含该视图

// PluginHost_NavigateTo_RegisteredView_Activates
// 输入：先 RegisterView，再 NavigateTo
// 预期：Region.ActiveViews 包含目标视图

// PluginHost_GetService_RegisteredService_ReturnsInstance
// 输入：注册 IValidationService → ValidationService
// 预期：GetService<IValidationService>() 非 null

// PluginHost_RegisterConfigSection_ThenGetConfigSection_ReturnsSame
// 输入：注册 ScaffoldConfigSection
// 预期：GetConfigSection("Scaffold") 返回同一实例
```

#### P1 -- 配置分片单元测试（各插件 Tests 项目）

**ScaffoldConfigSection 测试**

```csharp
// ScaffoldConfigSection_SectionId_EqualsScaffold
// 预期：SectionId == "Scaffold"

// ScaffoldConfigSection_GetDefaults_ReturnsScaffoldConfig
// 预期：GetDefaults() 是 ScaffoldConfig 类型，ProjectName=="", TargetFramework=="net10.0-windows"

// ScaffoldConfigSection_Validate_EmptyProjectName_HasError
// 输入：ScaffoldConfig { ProjectName = "" }
// 预期：ValidationResult 包含 "ProjectName" 错误

// ScaffoldConfigSection_Validate_ValidConfig_NoErrors
// 输入：ScaffoldConfig { ProjectName = "MyApp", OutputDirectory = "C:\Projects" }
// 预期：ValidationResult 无错误
```

**CollectionConfigSection 测试**

```csharp
// CollectionConfigSection_SectionId_EqualsScaffoldCollection
// 预期：SectionId == "Scaffold.Collection"

// CollectionConfigSection_GetDefaults_DriversListContains5Entries
// 预期：GetDefaults() 转为 CollectionConfigData 后，Drivers.Count == 5

// CollectionConfigSection_GetDefaults_SimulationDriverEnabled
// 预期：EnableSimulationDriver == true

// CollectionConfigSection_GetDefaults_DefaultPlcIp_Is192_168_1_1
// 预期：DefaultPLCIp == "192.168.1.1"（与 PlcDefaults.DefaultPlcIp 一致）
```

**VisionConfigSection 测试**

```csharp
// VisionConfigSection_SectionId_EqualsScaffoldVision
// 预期：SectionId == "Scaffold.Vision"

// VisionConfigSection_GetDefaults_EnableVisionFalse
// 预期：EnableVision == false

// VisionConfigSection_GetDefaults_CameraBrandHikvision
// 预期：CameraBrand == "Hikvision"
```

**SystemConfigSection 测试**

```csharp
// SystemConfigSection_SectionId_EqualsScaffoldSystem
// 预期：SectionId == "Scaffold.System"

// SystemConfigSection_GetDefaults_ModulesContain4Entries
// 预期：Modules.Count == 4 (UserManagement, RolePermission, SystemLog, ThemeSwitcher)

// SystemConfigSection_GetDefaults_UserManagementSelectedByDefault
// 预期：Modules.First(m => m.Key=="UserManagement").IsSelected == true

// SystemConfigSection_GetDefaults_SystemLogNotSelectedByDefault
// 预期：Modules.First(m => m.Key=="SystemLog").IsSelected == false
```

#### P1 -- ProjectGenerator 迁移测试（ScaffoldX.Plugin.Scaffold.Tests）

```csharp
// ProjectGenerator_GenerateAsync_ValidConfig_ReturnsSuccess
// 输入：ConfigRegistry 包含 Scaffold + Collection 分片，EnableSiemensS7=true
// 预期：GenerationResult.Success == true, FileCount > 0

// ProjectGenerator_GenerateAsync_InvalidProjectName_ReturnsFail
// 输入：ScaffoldConfig { ProjectName = "123Invalid" }
// 预期：GenerationResult.Success == false, ErrorMessage 包含 "项目名称验证失败"

// ProjectGenerator_GenerateAsync_OutputPathExists_ReturnsFail
// 输入：输出目录下已存在同名子目录
// 预期：GenerationResult.Success == false, ErrorMessage 包含 "目标目录已存在"

// ProjectGenerator_GenerateAsync_NoMatchingTemplates_ReturnsFail
// 输入：ConfigRegistry 中无任何启用的功能开关
// 预期：GenerationResult.Success == false, ErrorMessage 包含 "没有匹配当前配置的模板"

// ProjectGenerator_GenerateAsync_GeneratesFilesOnDisk
// 输入：有效的 ConfigRegistry
// 预期：输出目录下存在 .sln 文件和至少一个 .csproj 文件

// ProjectGenerator_GenerateAsync_RecordsHistory
// 输入：有效的 ConfigRegistry
// 预期：IHistoryService.SaveAsync 被调用一次，ProjectHistory.ProjectName 正确
```

#### P1 -- VariableResolver 迁移测试（ScaffoldX.Plugin.Scaffold.Tests）

```csharp
// AggregatedConfigResolver_BuildVariableContext_MatchesOldVariableResolver
// 输入：与旧 VariableResolver.BuildVariableContext 相同的 ProjectConfig 参数
// 预期：两个字典的键集合一致，所有值相等

// AggregatedConfigResolver_BuildVariableContext_CollectionConfig_ContainsHasAnyCollection
// 输入：CollectionConfig { EnableSiemensS7=true }
// 预期：结果字典包含 "HasAnyCollection"=true

// AggregatedConfigResolver_BuildVariableContext_VisionConfig_ContainsCameraBrandPascal
// 输入：VisionConfig { CameraBrand="hik-vision" }
// 预期：结果字典包含 "CameraBrandPascal"="HikVision"

// AggregatedConfigResolver_BuildVariableContext_XamlExt_Wpf
// 输入：ScaffoldConfig { UIFramework="WPF" }
// 预期：结果字典包含 "XamlExt"="xaml"

// AggregatedConfigResolver_BuildVariableContext_XamlExt_Avalonia
// 输入：ScaffoldConfig { UIFramework="Avalonia" }
// 预期：结果字典包含 "XamlExt"="axaml"
```

#### P1 -- PostProcessor 单元测试（ScaffoldX.Plugin.Scaffold.Tests）

```csharp
// PostProcessor_Process_NormalizesLineEndings
// 输入："line1\r\nline2\rline3\n"
// 预期："line1\nline2\nline3\n"

// PostProcessor_Process_RestoresXmlEntities_ForXamlFile
// 输入：content="&lt;Button/&gt;", outputPath="View.xaml"
// 预期："<Button/>"

// PostProcessor_Process_DoesNotRestoreXmlEntities_ForCsFile
// 输入：content="var x = a &amp; b;", outputPath="Service.cs"
// 预期："var x = a &amp; b;"（不变）

// PostProcessor_Process_TrimsTrailingWhitespace
// 输入："line1  \nline2\t\n"
// 预期："line1\nline2\n"

// PostProcessor_Process_EnsuresTrailingNewline
// 输入："content without newline"
// 预期：以 "\n" 结尾

// PostProcessor_Process_EmptyInput_ReturnsEmpty
// 输入：""
// 预期：""
```

#### P1 -- ValidationService 单元测试（ScaffoldX.Plugin.Scaffold.Tests）

```csharp
// ValidationService_ValidateProjectName_ValidName_ReturnsValid
// 输入："MyProject"
// 预期：IsValid == true

// ValidationService_ValidateProjectName_StartsWithNumber_ReturnsInvalid
// 输入："1Project"
// 预期：IsValid == false

// ValidationService_ValidateProjectName_Empty_ReturnsInvalid
// 输入：""
// 预期：IsValid == false, ErrorMessage 包含 "不能为空"

// ValidationService_ValidateProjectName_TooLong_ReturnsInvalid
// 输入：new string('A', 51)
// 预期：IsValid == false

// ValidationService_ValidateOutputPath_NonExistentPath_ReturnsInvalid
// 输入：path="Z:\NonExistent\Path", projectName="Test"
// 预期：IsValid == false

// ValidationService_ValidateIpAddress_ValidIp_ReturnsValid
// 输入："192.168.1.1"
// 预期：IsValid == true

// ValidationService_ValidateIpAddress_InvalidFormat_ReturnsInvalid
// 输入："999.999.999.999"
// 预期：IsValid == false

// ValidationService_ValidatePort_ValidPort_ReturnsValid
// 输入：102
// 预期：IsValid == true

// ValidationService_ValidatePort_Zero_ReturnsInvalid
// 输入：0
// 预期：IsValid == false

// ValidationService_ValidatePort_TooLarge_ReturnsInvalid
// 输入：70000
// 预期：IsValid == false
```

#### P2 -- AnnotationViewModel 迁移测试（ScaffoldX.Plugin.Annotation.Tests）

```csharp
// AnnotationViewModel_WithoutAutoLabelingService_Sam3CommandsDisabled
// 输入：构造时不传入 IAutoLabelingService
// 预期：SAM3 相关命令 CanExecute == false

// AnnotationViewModel_WithAutoLabelingService_Sam3CommandsEnabled
// 输入：构造时传入 mock IAutoLabelingService
// 预期：SAM3 相关命令 CanExecute == true

// AnnotationViewModel_LoadProject_CallsRepository
// 输入：调用 LoadProject 命令
// 预期：IAnnotationRepository.LoadProjectAsync 被调用

// AnnotationViewModel_ExportYolo_CallsExporter
// 输入：调用 Export YOLO 命令
// 预期：IAnnotationExporter.ExportYoloDatasetAsync 被调用
```

#### P2 -- TemplateMetadataLoader 测试（ScaffoldX.Plugin.Scaffold.Tests）

```csharp
// TemplateMetadataLoader_LoadFromTmetaJson_ParsesCorrectly
// 输入：包含 S7Driver.tmeta.json 的嵌入资源
// 预期：TemplateDescriptor.Name=="S7Driver", RequiredFeatures=["EnableSiemensS7"]

// TemplateMetadataLoader_NoTmetaJson_FallsBackToStplDirectives
// 输入：模板文件头部包含 ##OUTPUT: 和 ##REQUIRED: 指令，无 .tmeta.json
// 预期：TemplateDescriptor.OutputPathTemplate 和 IsRequired 从指令解析

// TemplateMetadataLoader_TmetaJsonOverridesStplDirectives
// 输入：同时存在 .tmeta.json 和 ##OUTPUT: 指令
// 预期：以 .tmeta.json 的值为准
```

#### P2 -- HistoryService 迁移测试（ScaffoldX.Plugin.Management.Tests）

```csharp
// HistoryService_SaveAsync_CreatesFile
// 输入：ProjectHistory { ProjectName="TestApp" }
// 预期：history.json 文件被创建

// HistoryService_LoadAsync_ReturnsSavedEntries
// 输入：先 SaveAsync，再 LoadAsync
// 预期：LoadAsync 返回的列表包含已保存的条目

// HistoryService_DeleteAsync_RemovesEntry
// 输入：先 SaveAsync，再 DeleteAsync("TestApp")
// 预期：LoadAsync 返回的列表不包含 "TestApp"

// HistoryService_UpdateAsync_ModifiesEntry
// 输入：先 SaveAsync，再 UpdateAsync（修改 OutputPath）
// 预期：LoadAsync 返回的条目 OutputPath 已更新
```

#### P2 -- VisionPlugin 生命周期测试（ScaffoldX.Plugin.Annotation.Vision.Tests）

```csharp
// VisionPlugin_Metadata_DependenciesContainsAnnotation
// 预期：Metadata.Dependencies 包含 "Annotation"

// VisionPlugin_OnLoaded_RegistersAutoLabelingService
// 输入：mock IPluginHost
// 预期：host 容器中注册了 IAutoLabelingService

// VisionPlugin_OnUnloading_DisposesSam3Segmentor
// 输入：先 OnLoaded，再 OnUnloading
// 预期：ISam3SegmentationEngine.Dispose 被调用
```

#### P2 -- Sam3Tokenizer 单元测试（ScaffoldX.Plugin.Annotation.Vision.Tests）

```csharp
// Sam3Tokenizer_Encode_EmptyInput_ReturnsBosEos
// 输入：""
// 预期：返回 [49406, 49407]

// Sam3Tokenizer_Encode_SingleWord_ReturnsBosWordEos
// 输入："hello"（假设词表中有）
// 预期：返回 [49406, <wordId>, 49407]

// Sam3Tokenizer_EncodePadded_PadsToTargetLength
// 输入：text="hi", length=10
// 预期：返回数组长度 == 10，末尾填充 0

// Sam3Tokenizer_Decode_RoundTrip
// 输入：Encode("test") → Decode(result)
// 预期：Decode 结果包含 "test"
```

#### P2 -- MaskToPolygonConverter 单元测试（ScaffoldX.Plugin.Annotation.Vision.Tests）

```csharp
// MaskToPolygonConverter_Convert_EmptyMask_ReturnsEmptyList
// 输入：new byte[0,0]
// 预期：返回空列表

// MaskToPolygonConverter_Convert_SinglePixelMask_ReturnsOnePoint
// 输入：new byte[1,1] { {1} }
// 预期：返回包含至少 1 个点的列表

// MaskToPolygonConverter_Convert_NormalizedCoordinates
// 输入：10x10 掩码，中心 5x5 区域为 1
// 预期：所有轮廓点坐标在 0-1 范围内

// MaskToPolygonConverter_Simplify_ReducesPointCount
// 输入：100 个点的正方形轮廓，tolerance=5.0f
// 预期：简化后点数 < 100
```

### 端到端测试场景

#### E2E-1：脚手架完整生成流程

```
前置条件：ScaffoldX.Shell 启动，Scaffold 插件已加载
步骤：
  1. 点击"新建项目"
  2. Step1：选择"工业采集"
  3. Step2：输入项目名 "E2ETestApp"，命名空间 "E2ETest"，选择输出目录
  4. Step3：勾选 SiemensS7 和 ModbusTcp 驱动，NavigationStyle=LeftSidebar
  5. Step4：确认文件树预览包含 S7Driver 和 ModbusTcpDriver
  6. 点击"确认生成"
  7. 等待生成完成
断言：
  - GenerationResult.Success == true
  - 输出目录存在 E2ETestApp.sln
  - 输出目录存在 src/E2ETestApp.Infrastructure/Drivers/S7Driver.cs
  - 输出目录存在 src/E2ETestApp.Infrastructure/Drivers/ModbusTcpDriver.cs
  - 输出目录不存在 OmronFinsDriver.cs（未勾选）
  - 输出目录存在 SidebarView.xaml（LeftSidebar 模式）
  - 输出目录不存在 TopNavView.xaml（非 TopNav 模式）
  - history.json 中包含 E2ETestApp 条目
```

#### E2E-2：标注工具基础流程

```
前置条件：ScaffoldX.Shell 启动，Annotation 插件已加载，Vision 子插件未安装
步骤：
  1. 点击"标注工具"
  2. 创建新标注项目
  3. 添加图像
  4. 使用矩形框工具标注
  5. 导出为 YOLO 格式
断言：
  - 标注数据正确保存
  - 导出目录包含 images/ 和 labels/ 子目录
  - SAM3 相关按钮不可见或禁用
```

#### E2E-3：Vision 子插件按需加载

```
前置条件：ScaffoldX.Shell 启动，Annotation + Vision 插件均已加载
步骤：
  1. 点击"标注工具"
  2. 确认 SAM3 按钮可见但 IsModelLoaded == false
  3. 点击"加载 SAM3 模型"，选择模型目录
  4. 等待模型加载完成
  5. 使用文本提示进行分割标注
断言：
  - 加载前 IsModelLoaded == false
  - 加载后 IsModelLoaded == true
  - 分割结果包含 SegmentationAnnotation
```

#### E2E-4：插件缺失依赖检测

```
前置条件：ScaffoldX.Shell 启动，仅安装 Annotation.Vision 插件（缺少 Annotation 插件）
步骤：
  1. 启动应用
断言：
  - 应用显示错误提示："插件 Annotation.Vision 依赖的 Annotation 未加载"
  - Annotation.Vision 插件状态为 Error
  - 其他插件正常加载
```

#### E2E-5：配置分片序列化与回放

```
前置条件：ScaffoldX.Shell 启动，Scaffold 插件已加载
步骤：
  1. 生成一个 Collection 项目
  2. 打开历史记录
  3. 查看该项目的配置详情
  4. 点击"重新生成"
断言：
  - 历史记录中 ConfigJson 为分片格式 JSON
  - "重新生成"使用保存的配置，生成的文件树与原始生成一致
```

### 测试优先级排序

| 优先级 | 范围 | 用例数 | 理由 |
|--------|------|--------|------|
| P0 | 抽象层接口契约 + DeclarativeTemplateFilter + 模板过滤回归 | ~30 | 这些是架构的基础，任何错误都会级联影响所有插件 |
| P0 | 模板过滤回归（新旧对比） | ~8 | 确保迁移不破坏现有功能，这是用户最直接的感知 |
| P1 | PluginLoader + PluginHost + 配置分片 + ProjectGenerator + VariableResolver + PostProcessor + ValidationService | ~40 | 核心功能模块，影响脚手架生成流程 |
| P1 | VariableResolver 迁移对比 | ~5 | 确保变量上下文与旧实现一致 |
| P2 | AnnotationViewModel + TemplateMetadataLoader + HistoryService + VisionPlugin + Sam3Tokenizer + MaskToPolygonConverter | ~25 | 辅助功能模块，可容忍短暂故障 |

### 回归测试策略

#### 阶段性回归检查点

每个迁移阶段结束后执行以下回归检查：

| 阶段 | 回归检查项 |
|------|-----------|
| 阶段一（建立抽象层） | 全量编译通过；现有测试全部通过；新接口编译通过 |
| 阶段二（拆分配置） | `AggregatedConfigResolver.BuildVariableContext` 输出与 `VariableResolver.BuildVariableContext` 完全一致（逐键对比）；Step1-4 UI 功能正常 |
| 阶段三（模板元数据声明化） | **关键回归**：对每种 ProjectConfig 组合（Collection+S7、Collection+Modbus、Vision、System+UserMgmt 等），`DeclarativeTemplateFilter` 过滤结果与 `TemplateRegistry.GetTemplatesForConfig` 完全一致；生成出的文件内容与旧版本逐字节对比 |
| 阶段四（创建 Shell 宿主） | 应用正常启动；插件列表正确显示；Region 导航正常 |
| 阶段五（拆分 Scaffold 插件） | E2E-1 脚手架完整生成流程通过；生成结果与旧版本逐字节对比 |
| 阶段六（拆分 Annotation 插件） | E2E-2 标注基础流程通过 |
| 阶段七（拆分 Vision 子插件） | E2E-3 Vision 按需加载通过；不安装 Vision 时标注功能正常 |
| 阶段八（拆分 Training 插件） | 训练配置 UI 正常；训练脚本生成正确 |
| 阶段九（拆分 Management 插件） | E2E-5 配置回放通过 |
| 阶段十（清理与验证） | 全量 E2E 测试；性能基准对比（启动时间、内存占用） |

#### 生成结果对比方法

```csharp
// 对比方法：使用相同 ProjectConfig，分别用新旧架构生成项目，逐文件对比
[TestMethod]
public void Regression_GeneratedOutput_IdenticalToOldVersion()
{
    var config = new ProjectConfig
    {
        ProjectType = "Collection",
        ProjectName = "RegressionTest",
        EnableSiemensS7 = true,
        EnableModbusTcp = true,
        NavigationStyle = "LeftSidebar",
        // ... 其他字段
    };

    // 旧架构生成
    var oldOutput = GenerateWithOldArchitecture(config);
    // 新架构生成
    var newOutput = GenerateWithNewArchitecture(config);

    // 逐文件对比
    var oldFiles = Directory.GetFiles(oldOutput, "*", SearchOption.AllDirectories);
    var newFiles = Directory.GetFiles(newOutput, "*", SearchOption.AllDirectories);

    Assert.AreEqual(oldFiles.Length, newFiles.Length, "文件数量不一致");

    foreach (var oldFile in oldFiles)
    {
        var relativePath = Path.GetRelativePath(oldOutput, oldFile);
        var newFile = Path.Combine(newOutput, relativePath);
        Assert.IsTrue(File.Exists(newFile), $"新架构缺少文件: {relativePath}");

        var oldContent = File.ReadAllText(oldFile);
        var newContent = File.ReadAllText(newFile);
        Assert.AreEqual(oldContent, newContent, $"文件内容不一致: {relativePath}");
    }
}
```

#### 持续回归保障

- 每次提交触发 CI 流水线，运行 P0 + P1 测试
- 每日夜间构建运行全量测试（含 P2）
- 模板过滤回归测试作为 CI 的必过门槛，任何失败阻塞合并

---

## Out of Scope

以下内容不在本次 PRD 范围内：

1. **新驱动模板**：本次仅迁移现有 6 种驱动模板（SiemensS7、ModbusTcp、OpcUa、MitsubishiMc、OmronFins、Simulation）到 `.tmeta.json` 格式，不新增驱动类型。
2. **新标注格式**：不新增导出格式，仅迁移现有 5 种（YOLO、COCO、VOC、DOT、MOT）。
3. **新训练引擎**：不新增训练后端，仅迁移现有 YOLO 训练功能。
4. **UI 重设计**：不改变现有 UI 布局和交互流程，仅将 View/ViewModel 迁移到对应插件项目。
5. **跨平台支持**：不引入 Avalonia 或 MAUI 替代 WPF，Shell 仍为 WPF 应用。
6. **插件热加载/卸载**：插件仅在应用启动时加载，不支持运行时动态安装/卸载。
7. **插件沙箱安全**：不实现代码访问安全或 AppDomain 隔离，所有插件在同一个 AppDomain 中运行。
8. **网络插件仓库**：不实现插件在线发现和下载，插件通过文件系统部署。
9. **性能优化**：不针对启动速度、内存占用进行专项优化（但隔离 TorchSharp 后预期有改善）。
10. **国际化**：不新增语言支持，仅迁移现有中英文资源。
11. **数据库迁移**：不涉及数据库 schema 变更，历史记录仍使用 JSON 文件持久化。
12. **ScaffoldX.Core 的最终处置**：Core 项目的删除或合并到 Scaffold 插件，留待阶段十评估决定。

---

## Further Notes

### 迁移阶段与时间估算

| 阶段 | 内容 | 预估工时 | 依赖 |
|------|------|----------|------|
| 一 | 建立 Abstractions 抽象层 | 1-2 天 | 无 |
| 二 | 拆分配置（IConfigSection + ConfigRegistry + AggregatedConfigResolver） | 2-3 天 | 阶段一 |
| 三 | 模板元数据声明化（.tmeta.json + DeclarativeTemplateFilter + 回归测试） | 2-3 天 | 阶段二 |
| 四 | 创建 Shell 宿主（PluginHost + PluginLoader + Region 导航） | 2-3 天 | 阶段一 |
| 五 | 拆分 Scaffold 插件 | 2-3 天 | 阶段二、三、四 |
| 六 | 拆分 Annotation 插件 | 2-3 天 | 阶段四 |
| 七 | 拆分 Vision 子插件 | 1-2 天 | 阶段六 |
| 八 | 拆分 Training 插件 | 1-2 天 | 阶段四 |
| 九 | 拆分 Management 插件 | 1 天 | 阶段四 |
| 十 | 清理与验证 | 1-2 天 | 阶段五-九 |

**总预估**：15-24 个工作日。阶段四可与阶段二、三并行，阶段六-九可并行。

### 风险

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|----------|
| Prism Region 导航与插件动态注册冲突 | 插件视图无法正确显示 | 中 | Shell 初始化完成后再加载插件；使用 RegionBehavior 延迟注册 |
| TorchSharp 跨插件加载 | Vision 子插件的 TorchSharp 初始化可能影响主进程稳定性 | 低 | Vision 子插件使用独立 AssemblyLoadContext 隔离；延迟加载模型 |
| 配置分片序列化格式变更 | 旧版 history.json 无法直接读取 | 中 | 提供配置迁移工具，自动将旧格式 ProjectConfig JSON 转换为分片格式 |
| 模板过滤回归不一致 | 生成的项目缺少或多余文件 | 低 | 阶段三强制要求逐文件对比回归测试通过后才进入下一阶段 |
| 过度设计 | 4-5 个插件对当前团队规模可能过重 | 中 | 插件接口保持简单；初期可合并 Annotation + Annotation.Vision 为一个插件，后续再拆分 |

### 验收标准

1. **功能等价**：迁移后所有现有功能（脚手架生成、标注、训练、管理）行为与迁移前完全一致，通过 E2E 测试验证。
2. **依赖隔离**：`ScaffoldX.Plugin.Annotation` 的 csproj 不包含 `TorchSharp` 依赖；不安装 Vision 子插件时标注功能正常。
3. **模板过滤一致**：`DeclarativeTemplateFilter` 对所有现有模板的过滤结果与 `TemplateRegistry.ShouldInclude*` 完全一致，通过回归测试验证。
4. **生成结果一致**：使用相同配置生成的项目文件内容与旧版本逐字节一致。
5. **插件可插拔**：删除任意插件的 DLL 后，应用仍可启动，仅缺失该插件的功能。
6. **配置分片独立**：每个插件的配置分片可独立序列化和反序列化，`ProjectConfig` 上帝对象不再存在。
7. **测试覆盖**：P0 测试 100% 通过，P1 测试 100% 通过，P2 测试至少 80% 通过。
8. **编译通过**：全量编译无错误无警告。
