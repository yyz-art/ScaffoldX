# ScaffoldX — 工业上位机脚手架生成工具 PRD（完整版）

> **文档版本**：1.0  
> **最后更新**：2026-04-28  
> **目标读者**：AI Agent、开发工程师、技术负责人  
> **阅读指南**：本文档为可执行 PRD，任何标注 `[AGENT-SPEC]` 的段落为 AI Agent 执行规范，必须严格遵守；标注 `[HUMAN-CHECK]` 的段落需人工确认。

---

## 目录

1. [项目概述](#1-项目概述)
2. [产品范围与边界](#2-产品范围与边界)
3. [架构决策矩阵](#3-架构决策矩阵)
4. [脚手架本体：可视化配置向导](#4-脚手架本体可视化配置向导)
5. [生成目标程序完整规范](#5-生成目标程序完整规范)
6. [插件系统完整规范](#6-插件系统完整规范)
7. [工业采集类专项规范](#7-工业采集类专项规范)
8. [视觉检测类专项规范](#8-视觉检测类专项规范)
9. [系统定制类专项规范](#9-系统定制类专项规范)
10. [模板引擎与生成流程](#10-模板引擎与生成流程)
11. [脚手架工具自身技术规范](#11-脚手架工具自身技术规范)
12. [数据库 Schema](#12-数据库-schema)
13. [非功能需求](#13-非功能需求)
14. [界面与交互规范](#14-界面与交互规范)
15. [错误处理与边界条件](#15-错误处理与边界条件)
16. [命名规范与代码风格](#16-命名规范与代码风格)
17. [验收标准](#17-验收标准)
18. [里程碑与版本规划](#18-里程碑与版本规划)
19. [附录](#19-附录)

---

## 1. 项目概述

### 1.1 产品定义

**产品名称**：ScaffoldX（代号）  
**产品定位**：面向工业上位机开发者的项目生成器，以可视化配置向导快速产出具备工业级稳定性的 WPF/Avalonia 程序框架，覆盖采集、视觉、系统定制三类场景，让开发者仅需填充核心业务逻辑，零摩擦启动新项目。

### 1.2 核心价值

| 价值点 | 说明 |
|--------|------|
| 生成即合理 | 重复的胶水代码、架构模式、异常处理、日志配置全部自动生成 |
| 强约束保稳定 | 硬编码隔离、插件状态机、资源释放检查通过模板注入和接口约束强制执行 |
| 可离线演示 | 内建模拟驱动、模拟相机、测试图像源，无硬件环境下完成全流程开发调试 |
| 开箱即工业级 | 权限、多语言、远程数据存储、运行诊断等常见需求预置 |

### 1.3 目标用户

- 工业上位机/WPF/Avalonia 开发工程师
- 需要快速搭建采集监控、视觉检测、生产管理系统框架的团队
- 希望统一团队项目规范和基础架构的技术负责人

### 1.4 术语表

| 术语 | 定义 |
|------|------|
| 上位机 | 运行在 PC 上，用于监控和控制下位机（PLC、传感器等）的软件 |
| 脚手架 | 项目生成器，根据配置自动创建项目骨架代码 |
| 插件 | 独立编译的程序集，实现 `IPlugin` 接口，运行时动态加载 |
| 驱动 | 实现 `IDriver` 接口的通信模块，负责与硬件设备交互 |
| Tag | 工业数据标签，对应一个可读写的 PLC 地址或变量 |
| 模板 | `.stpl` 文件，包含带变量占位符的代码文本 |
| 状态机 | 插件生命周期管理器，控制 Normal/Faulted/Offline 状态转换 |
| .stpl | ScaffoldX Template Language，本项目模板文件扩展名 |
| `$VAR$` | 模板中的变量占位符标记 |

---

## 2. 产品范围与边界

### 2.1 脚手架本身（在范围内）

- 一个 **WPF 桌面应用**（.NET 8），提供可视化配置向导，将用户选项转换为项目模板生成指令。
- 内置 **Scriban 模板引擎**，管理所有项目模板。
- 无网络依赖，所有模板和逻辑本地化，适合工控隔离网络环境。
- 生成完整的 `.sln` 解决方案及相关项目文件，确保用 Visual Studio 2022 / Rider 2023+ 直接打开即可编译运行。

### 2.2 不在范围内

| 排除项 | 理由 |
|--------|------|
| 运行时监控功能 | 那是生成程序的能力，不是脚手架的 |
| 云端模板仓库/在线更新 | 工控隔离网络不适用 |
| 单元测试项目生成 | V1 暂不覆盖 |
| C++/WinForm/MVVM Light 等技术栈 | 聚焦 Prism + WPF/Avalonia |
| 数据库迁移工具 | 生成时一次性建表即可 |
| CI/CD 管道配置 | 企业环境差异大，不通用 |

---

## 3. 架构决策矩阵

| 决策项 | 最终选择 | 选型理由 |
|--------|----------|----------|
| 整体架构模式 | Prism 强约束框架 | 业界 WPF 大型项目事实标准，模块化/导航/事件总线一站式 |
| 解决方案组织 | 单一解决方案，多项目共存 | 降低部署复杂度，便于跨项目引用 |
| 生成机制 | Scriban 模板引擎 + 实体 `.stpl` 文件 | 可调试、可版本管理、可离线 |
| 插件加载方式 | `AssemblyLoadContext` 完全隔离 | .NET 推荐的插件隔离方案，支持卸载 |
| 插件发现时机 | 仅启动时扫描，运行期锁定 | 工业场景追求确定性 |
| 插件通信方式 | Prism `IEventAggregator` 消息总线 | 松耦合，发布/订阅模式成熟 |
| 插件异常隔离 | 状态机强隔离（Normal/Faulted/Offline） | 单插件故障不扩散 |
| 插件资源释放 | `IAsyncDisposable` + 基类强制覆写 | 编译期强制，防止遗漏 |
| 插件版本兼容 | 接口版本号常量 + 启动硬校验 | 运行前拦截不兼容插件 |
| 配置管理 | 本地 SQLite 强类型配置服务 | 无外部依赖，结构化存储 |
| 日志策略 | Serilog 结构化文件日志 | 可离线，结构化便于后续分析 |
| 看门狗自愈 | 深度诊断+健康报告替代 | 避免自愈引入的不确定性 |
| 多语言 | 强制中英双语，`.resx` 资源文件分离 | 国际化刚需 |
| 部署策略 | 自包含绿色发布，XCopy 部署 | 工控机场景，无管理员权限 |
| 数据持久化 | 远端数据库优先 + 本地 SQLite 离线缓冲 | 网络断连时业务不中断 |
| 权限管理 | 菜单/页面级角色授权 | 粒度适中，实现成本可控 |
| 主界面布局 | 经典侧边栏导航 | 工业软件通用范式 |
| 视觉图像抽象 | `ICameraService` + 工厂反射 + 配置切换 | 多品牌相机统一接入 |
| 脚手架自身 UI 框架 | WPF + MaterialDesign in XAML | 与目标项目技术栈一致，减少学习成本 |

---

## 4. 脚手架本体：可视化配置向导

### 4.1 启动与历史项目管理

#### 4.1.1 启动界面

脚手架启动后显示主界面，分两个区域：

**上半区 — 历史项目列表**
- `ListView` 或 `DataGrid` 展示，列定义：

| 列名 | 绑定字段 | 宽度 | 排序 |
|------|----------|------|------|
| 项目名称 | `ProjectName` | 200px | 默认升序 |
| 项目类型 | `ProjectType` (中文显示) | 100px | - |
| 存储路径 | `OutputPath` | 自动填充 | - |
| 目标框架 | `TargetFramework` | 100px | - |
| 生成时间 | `CreatedAt` (yyyy-MM-dd HH:mm) | 150px | - |

- 每行右键菜单：`打开所在文件夹`、`用 VS 打开`、`从历史中删除`。
- 双击行 = 用系统默认方式打开 `.sln` 文件。

**下半区 — 操作按钮**
- 大按钮：`+ 新建项目`（MaterialDesign `Plus` 图标，主色调）
- 小按钮：`设置`（齿轮图标）、`关于`

**历史记录存储**
- JSON 文件：`%APPDATA%/ScaffoldX/history.json`
- 格式见 §12.2

#### 4.1.2 `[AGENT-SPEC]` 历史管理服务接口

```csharp
public interface IHistoryService
{
    Task<List<ProjectHistory>> LoadAsync();
    Task SaveAsync(ProjectHistory entry);
    Task DeleteAsync(string projectName);
    Task UpdateAsync(ProjectHistory entry);
}

public class ProjectHistory
{
    public string ProjectName { get; set; }
    public string ProjectType { get; set; }       // "Collection" | "Vision" | "System"
    public string OutputPath { get; set; }
    public string TargetFramework { get; set; }    // "net6.0-windows" | "net8.0-windows" | "net8.0"
    public string UIFramework { get; set; }        // "WPF" | "Avalonia"
    public DateTime CreatedAt { get; set; }
    public string ConfigJson { get; set; }         // 完整 ProjectConfig JSON，用于"重新生成"
}
```

### 4.2 向导步骤一：项目类型选择

#### 4.2.1 UI 布局

三个大卡片横排，每个卡片尺寸：`280px × 200px`，间距 `24px`，居中排列。

| 卡片 | 图标 (MaterialDesign) | 标题 | 描述 |
|------|----------------------|------|------|
| 工业采集 | `Factory` / `LanConnect` | 工业采集 | PLC 通信、数据采集、实时监控 |
| 视觉检测 | `Camera` / `Eye` | 视觉检测 | 相机接入、图像推理、缺陷检测 |
| 系统定制 | `Cog` / `Settings` | 系统定制 | 用户管理、权限控制、主题定制 |

- 卡片为 `RadioButton` 样式，选中时边框变主色调 `#1565C0`，带 `DropShadowEffect`。
- 选中后底部显示详细描述文本（2~3 行）。
- 默认选中：无（必须主动选择才能点"下一步"）。

#### 4.2.2 `[AGENT-SPEC]` 验证规则

- 未选择任何类型时，"下一步"按钮 `IsEnabled=false`。
- 选择后 `IsEnabled=true`，按钮文本从灰色变为蓝色。

### 4.3 向导步骤二：基础信息

#### 4.3.1 表单字段

| 字段 | 控件类型 | 验证规则 | 默认值 | 必填 |
|------|----------|----------|--------|------|
| 项目名称 | `TextBox` | 正则 `^[A-Za-z][A-Za-z0-9_]{0,49}$`；不允许空格、中文、特殊字符 | 空 | ✅ |
| 存储路径 | `TextBox` + `Button`(浏览) | 必须是已存在的目录；路径中不允许非法字符 | `%USERPROFILE%\Projects` | ✅ |
| 命名空间前缀 | `TextBox` | 同项目名称规则；实时从项目名称生成 PascalCase | 自动等于项目名称的 PascalCase | ✅ |
| 目标 UI 框架 | `ComboBox` | 枚举：WPF / Avalonia | WPF | ✅ |
| .NET 版本 | `ComboBox` | 枚举：.NET 6 / .NET 8 | .NET 8 | ✅ |
| 项目描述 | `TextBox` (MultiLine) | 最大 500 字符 | 空 | ❌ |

#### 4.3.2 `[AGENT-SPEC]` 实时预览与联动

```
用户输入项目名称 "myFactory" 
    → 命名空间前缀自动填充 "MyFactory"（PascalCase 转换）
    → 底部预览文本："将生成 MyFactory.sln，命名空间 MyFactory.*"
```

**PascalCase 转换规则**：
- 输入 `my_factory` → `MyFactory`
- 输入 `myFactory` → `MyFactory`
- 输入 `MYFACTORY` → `Myfactory`（首字母大写，其余小写）
- 输入 `my-factory` → `MyFactory`（去除连字符，驼峰合并）

**Avalonia 选择时的联动**：
- .NET 6 选项禁用（Avalonia 最低 .NET 8），提示文本："Avalonia 需要 .NET 8 或更高版本"

#### 4.3.3 `[AGENT-SPEC]` 验证逻辑

```csharp
public class ValidationService : IValidationService
{
    // 项目名称验证
    private static readonly Regex ProjectNameRegex = new(@"^[A-Za-z][A-Za-z0-9_]{0,49}$", RegexOptions.Compiled);
    
    public ValidationResult ValidateProjectName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.Fail("项目名称不能为空");
        if (!ProjectNameRegex.IsMatch(name))
            return ValidationResult.Fail("项目名称只能包含英文字母、数字和下划线，且必须以字母开头");
        if (name.Length > 50)
            return ValidationResult.Fail("项目名称不能超过50个字符");
        return ValidationResult.Success();
    }

    public ValidationResult ValidateOutputPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ValidationResult.Fail("存储路径不能为空");
        if (!Directory.Exists(path))
            return ValidationResult.Fail("存储路径不存在");
        if (Path.GetInvalidPathChars().Any(c => path.Contains(c)))
            return ValidationResult.Fail("存储路径包含非法字符");
        // 检查目标文件夹是否已存在同名项目
        string projectName = /* 从上下文获取 */;
        if (Directory.Exists(Path.Combine(path, projectName)))
            return ValidationResult.Fail($"目标路径已存在同名文件夹：{projectName}");
        return ValidationResult.Success();
    }

    public string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        // 按 _, -, 空格 分割
        var words = Regex.Split(input, @"[_\-\s]+")
            .Where(w => w.Length > 0)
            .Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower());
        return string.Concat(words);
    }
}
```

### 4.4 向导步骤三：专项配置（动态区块）

此步骤根据步骤一选择的项目类型动态切换内容区域。切换时使用 `ContentControl` + `DataTemplate`，动画淡入 `300ms`。

#### 4.4.1 工业采集类配置

| 配置项 | 控件类型 | 选项/规则 | 默认值 | 必填 |
|--------|----------|-----------|--------|------|
| 预置协议驱动 | `CheckBox` 列表 | Siemens S7, Modbus TCP, OPC UA, Mitsubishi MC | 全部取消 | ❌ |
| 生成模拟驱动 | `CheckBox` | - | ✅ 勾选 | - |
| 默认 PLC IP | `TextBox` | IP 格式验证 `^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$` | `192.168.1.1` | ❌ |
| 默认 PLC 端口 | `TextBox` (Numeric) | 1~65535 | `102` (S7) / `502` (Modbus) | ❌ |
| S7 机架号 | `TextBox` (Numeric) | 0~7 | `0` | ❌ |
| S7 槽号 | `TextBox` (Numeric) | 0~31 | `1` | ❌ |

**联动逻辑**：
- 勾选 Siemens S7 时，显示 S7 专属参数（机架号、槽号）。
- 勾选 Modbus TCP 时，端口默认变为 `502`。
- 勾选 OPC UA 时，显示 OPC Endpoint URL 输入框（默认 `opc.tcp://localhost:4840`）。
- 至少勾选一个驱动（含模拟驱动）才能进入下一步。

#### 4.4.2 视觉检测类配置

| 配置项 | 控件类型 | 选项/规则 | 默认值 | 必填 |
|--------|----------|-----------|--------|------|
| 相机品牌 | `ComboBox` | 海康、大恒、Basler、本地图像源 | 海康 | ✅ |
| 模型类型 | `ComboBox` | ONNX 分类 / ONNX 检测 (YOLO) | ONNX 分类 | ✅ |
| 模型文件路径 | `TextBox` + 浏览按钮 | 文件存在性验证（可留空生成占位） | 空 | ❌ |
| 生成结果拦截器管道 | `CheckBox` | - | ✅ 勾选 | - |
| 生成模拟相机 | `CheckBox` | - | ✅ 勾选（始终自动包含） | - |

**联动逻辑**：
- 选择"本地图像源"时，相机品牌下拉禁用，模拟相机自动勾选且不可取消。
- 模型文件路径留空时，生成代码中使用 `// TODO: [ScaffoldX] 请指定模型文件路径` 占位。

#### 4.4.3 系统定制类配置

| 配置项 | 控件类型 | 选项/规则 | 默认值 | 必填 |
|--------|----------|-----------|--------|------|
| 预置模块 | `CheckBox` 列表 | 用户管理、角色权限、系统日志、主题切换 | 用户管理 + 角色权限 | ❌ |
| 跨平台发布配置 | `CheckBox` | 仅 Avalonia 可选（WPF 时灰显） | ❌ | - |
| 启用登录窗口 | `CheckBox` | - | ✅ 勾选 | - |
| 种子用户密码策略 | `ComboBox` | 默认密码 / 强制首次修改 | 默认密码 | ❌ |

### 4.5 向导步骤四：确认与生成

#### 4.5.1 解决方案预览

**树形图** 使用 `TreeView` 控件展示即将生成的文件结构：
- 根节点：`{ProjectName}.sln`
- 一级节点：`src/`、`tools/`、`docs/`、根目录文件
- 二级节点：各 `.csproj` 项目
- 三级节点：关键文件（`Bootstrapper.cs`、`MainWindow.xaml` 等）
- 使用不同图标区分文件夹、`.cs`、`.xaml`、`.csproj`、`.resx` 等类型

**统计面板**（树形图右侧或底部）：

| 统计项 | 值 |
|--------|-----|
| 项目总数 | `{N}` 个 |
| 预估文件数 | `{N}` 个 |
| 目标框架 | `net8.0-windows` |
| UI 框架 | WPF |
| 包含驱动 | Siemens S7, Modbus TCP |
| 包含模块 | 用户管理, 角色权限 |

#### 4.5.2 生成按钮行为

1. 点击"生成"后：
   - 按钮变为"生成中..."，带 `ProgressBar`（不确定模式）。
   - 所有"上一步"按钮禁用。
2. 生成过程中实时更新状态文本：
   - "正在验证配置..."
   - "正在解析模板..."
   - "正在创建目录结构..."
   - "正在生成 Core 项目..."
   - "正在生成 {ModuleName} 模块..."
   - "正在执行后处理..."
   - "完成！"
3. 生成完成后：
   - 按钮变为"完成"，绿色对勾图标。
   - 显示"打开文件夹"和"用 Visual Studio 打开"两个按钮。
   - 显示生成耗时（精确到毫秒）。

---

## 5. 生成目标程序完整规范

### 5.1 解决方案结构（完整）

以**工业采集类**为基准，标注 `[动态]` 的项目/文件按配置选项决定是否生成：

```
{ProjectName}.sln
│
├── src/
│   ├── {ProjectName}.App/                              # 启动项目
│   │   ├── App.xaml / App.axaml                        # [动态] 按 UI 框架
│   │   ├── App.xaml.cs / App.axaml.cs                  # [动态]
│   │   ├── Bootstrapper.cs                             # Prism 启动引导
│   │   ├── Views/
│   │   │   ├── MainWindow.xaml / .axaml                # 主窗口（侧边栏壳）
│   │   │   ├── MainWindow.xaml.cs / .axaml.cs
│   │   │   ├── LoginWindow.xaml / .axaml               # [动态] 启用权限时
│   │   │   ├── LoginWindow.xaml.cs / .axaml.cs
│   │   │   └── DiagnosticsView.xaml / .axaml           # 诊断面板入口
│   │   ├── ViewModels/
│   │   │   ├── MainWindowViewModel.cs
│   │   │   ├── LoginViewModel.cs                       # [动态]
│   │   │   └── DiagnosticsViewModel.cs
│   │   ├── Resources/
│   │   │   ├── Strings.zh-CN.resx                     # 中文资源
│   │   │   ├── Strings.en-US.resx                     # 英文资源
│   │   │   └── Themes/
│   │   │       ├── Colors.xaml / .axaml                # 调色板
│   │   │       ├── Styles.xaml / .axaml                # 全局样式
│   │   │       └── Icons.xaml / .axaml                 # 图标资源
│   │   ├── appsettings.json                            # Bootstrap 配置
│   │   └── {ProjectName}.App.csproj
│   │
│   ├── {ProjectName}.Core/                             # 核心公共库
│   │   ├── Abstractions/
│   │   │   ├── IPlugin.cs
│   │   │   ├── PluginBase.cs
│   │   │   ├── IMenuModule.cs
│   │   │   └── IDriver.cs                              # [动态] 采集类
│   │   ├── Models/
│   │   │   ├── PluginState.cs
│   │   │   ├── PluginInfo.cs
│   │   │   ├── Tag.cs                                  # [动态] 采集类
│   │   │   ├── DataValue.cs                            # [动态] 采集类
│   │   │   ├── ConnectionParams.cs                     # [动态] 采集类
│   │   │   ├── UserRole.cs                             # [动态] 系统类
│   │   │   └── AppConstants.cs
│   │   ├── Services/
│   │   │   ├── IConfigService.cs
│   │   │   ├── ConfigService.cs
│   │   │   ├── IUserService.cs                         # [动态] 系统类
│   │   │   ├── UserService.cs                          # [动态] 系统类
│   │   │   ├── IPermissionService.cs                   # [动态] 系统类
│   │   │   ├── PermissionService.cs                    # [动态] 系统类
│   │   │   ├── IMenuService.cs
│   │   │   ├── MenuService.cs
│   │   │   ├── INavigationRegistry.cs
│   │   │   └── HealthCheckService.cs
│   │   ├── Infrastructure/
│   │   │   ├── PluginLoadContext.cs
│   │   │   ├── PluginScanner.cs
│   │   │   ├── PluginLoader.cs
│   │   │   ├── PluginStateMachine.cs
│   │   │   └── DatabaseInitializer.cs
│   │   ├── Events/
│   │   │   ├── DataUpdatedEvent.cs                     # [动态] 采集类
│   │   │   ├── PluginStateChangedEvent.cs
│   │   │   ├── AlarmEvent.cs                           # [动态] 采集/视觉类
│   │   │   └── InferenceResultEvent.cs                 # [动态] 视觉类
│   │   ├── Extensions/
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   └── LogContextExtensions.cs
│   │   └── {ProjectName}.Core.csproj
│   │
│   ├── {ProjectName}.Modules.Shell/                    # 主界面模块
│   │   ├── Views/
│   │   │   ├── SidebarView.xaml / .axaml
│   │   │   ├── SidebarView.xaml.cs / .axaml.cs
│   │   │   ├── StatusBarView.xaml / .axaml
│   │   │   ├── StatusBarView.xaml.cs / .axaml.cs
│   │   │   ├── HomePageView.xaml / .axaml
│   │   │   └── HomePageView.xaml.cs / .axaml.cs
│   │   ├── ViewModels/
│   │   │   ├── SidebarViewModel.cs
│   │   │   ├── StatusBarViewModel.cs
│   │   │   └── HomePageViewModel.cs
│   │   ├── ShellModule.cs
│   │   └── {ProjectName}.Modules.Shell.csproj
│   │
│   ├── {ProjectName}.Modules.Diagnostic/               # 诊断模块
│   │   ├── Views/
│   │   │   ├── PluginStatusView.xaml / .axaml
│   │   │   ├── PluginStatusView.xaml.cs / .axaml.cs
│   │   │   ├── LogViewerView.xaml / .axaml
│   │   │   └── LogViewerView.xaml.cs / .axaml.cs
│   │   ├── ViewModels/
│   │   │   ├── PluginStatusViewModel.cs
│   │   │   └── LogViewerViewModel.cs
│   │   ├── DiagnosticModule.cs
│   │   └── {ProjectName}.Modules.Diagnostic.csproj
│   │
│   │── [以下按项目类型动态生成]
│   │
│   │── # ===== 工业采集类 =====
│   ├── {ProjectName}.Drivers.Simulation/               # 模拟驱动插件 [动态:采集类]
│   │   ├── SimDriver.cs
│   │   ├── SimDriverPlugin.cs
│   │   ├── SimDataGenerator.cs
│   │   └── {ProjectName}.Drivers.Simulation.csproj
│   ├── {ProjectName}.Drivers.SiemensS7/                # [动态:勾选S7]
│   │   ├── S7Driver.cs
│   │   ├── S7DriverPlugin.cs
│   │   └── {ProjectName}.Drivers.SiemensS7.csproj
│   ├── {ProjectName}.Drivers.ModbusTcp/                # [动态:勾选Modbus]
│   │   ├── ModbusDriver.cs
│   │   ├── ModbusDriverPlugin.cs
│   │   └── {ProjectName}.Drivers.ModbusTcp.csproj
│   ├── {ProjectName}.Drivers.OpcUa/                    # [动态:勾选OPC UA]
│   │   ├── OpcUaDriver.cs
│   │   ├── OpcUaDriverPlugin.cs
│   │   └── {ProjectName}.Drivers.OpcUa.csproj
│   ├── {ProjectName}.Drivers.MitsubishiMc/             # [动态:勾选三菱]
│   │   ├── McDriver.cs
│   │   ├── McDriverPlugin.cs
│   │   └── {ProjectName}.Drivers.MitsubishiMc.csproj
│   │
│   │── # ===== 视觉检测类 =====
│   ├── {ProjectName}.Vision.Camera.Simulate/           # [动态:视觉类]
│   │   ├── SimCameraService.cs
│   │   ├── SimCameraPlugin.cs
│   │   └── {ProjectName}.Vision.Camera.Simulate.csproj
│   ├── {ProjectName}.Vision.Camera.Hikvision/          # [动态:选海康]
│   │   ├── HikCameraService.cs
│   │   ├── HikCameraPlugin.cs
│   │   └── {ProjectName}.Vision.Camera.Hikvision.csproj
│   ├── {ProjectName}.Vision.Camera.Daheng/             # [动态:选大恒]
│   ├── {ProjectName}.Vision.Camera.Basler/             # [动态:选Basler]
│   ├── {ProjectName}.Vision.Inference/                 # [动态:视觉类]
│   │   ├── InferenceEngineBase.cs
│   │   ├── OnnxClassifier.cs                           # [动态:选分类]
│   │   ├── OnnxDetector.cs                             # [动态:选检测]
│   │   ├── InferenceResult.cs
│   │   └── {ProjectName}.Vision.Inference.csproj
│   ├── {ProjectName}.Vision.Pipeline/                  # [动态:勾选管道]
│   │   ├── IPipelineStep.cs
│   │   ├── PipelineContext.cs
│   │   ├── PipelineEngine.cs
│   │   ├── ConsecutiveNgAlarmStep.cs
│   │   └── {ProjectName}.Vision.Pipeline.csproj
│   ├── {ProjectName}.Modules.Vision/                   # [动态:视觉类]
│   │   ├── Views/
│   │   │   ├── VisionMainView.xaml / .axaml
│   │   │   └── VisionMainView.xaml.cs / .axaml.cs
│   │   ├── ViewModels/
│   │   │   └── VisionMainViewModel.cs
│   │   ├── VisionModule.cs
│   │   └── {ProjectName}.Modules.Vision.csproj
│   │
│   │── # ===== 系统定制类 =====
│   ├── {ProjectName}.Modules.UserManagement/           # [动态:勾选用户管理]
│   │   ├── Views/
│   │   │   ├── UserListView.xaml / .axaml
│   │   │   ├── UserListView.xaml.cs / .axaml.cs
│   │   │   ├── UserEditView.xaml / .axaml
│   │   │   └── UserEditView.xaml.cs / .axaml.cs
│   │   ├── ViewModels/
│   │   │   ├── UserListViewModel.cs
│   │   │   └── UserEditViewModel.cs
│   │   ├── UserManagementModule.cs
│   │   └── {ProjectName}.Modules.UserManagement.csproj
│   ├── {ProjectName}.Modules.RolePermission/           # [动态:勾选角色权限]
│   ├── {ProjectName}.Modules.SystemLog/                # [动态:勾选系统日志]
│   ├── {ProjectName}.Modules.ThemeSwitcher/            # [动态:勾选主题切换]
│   └── {ProjectName}.Modules.SystemLog/                # [动态:勾选系统日志]
│
├── tools/
│   ├── publish.bat                                     # 一键发布脚本 (WPF)
│   ├── publish.sh                                      # 一键发布脚本 (Avalonia/Linux)
│   ├── update.bat                                      # 增量更新脚本
│   └── clean.bat                                       # 清理构建产物
│
├── docs/
│   ├── Architecture.md                                 # 架构说明
│   ├── PluginDevelopment.md                            # 插件开发指南
│   └── Deployment.md                                   # 部署说明
│
├── Plugins/                                            # 插件输出目录
│   └── .gitkeep
│
├── Data/                                               # 本地数据目录
│   └── .gitkeep
│
├── Logs/                                               # 日志输出目录
│   └── .gitkeep
│
├── .gitignore
├── Directory.Build.props                               # 统一 NuGet/编译配置
├── global.json                                         # .NET SDK 版本锁定
└── README.md
```

### 5.2 Bootstrapper 完整启动流程

```csharp
// Bootstrapper.cs — Prism Bootstrapper
public class Bootstrapper : PrismBootstrapper
{
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 1. 注册配置服务
        containerRegistry.RegisterSingleton<IConfigService, ConfigService>();
        
        // 2. 注册日志
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("ProjectType", AppConstants.ProjectType)
            .WriteTo.File(
                path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{MachineName}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        containerRegistry.RegisterInstance<ILogger>(logger);
        
        // 3. 注册健康检查
        containerRegistry.RegisterSingleton<HealthCheckService>();
        
        // 4. 注册插件基础设施
        containerRegistry.RegisterSingleton<PluginScanner>();
        containerRegistry.RegisterSingleton<PluginLoader>();
        
        // 5. 注册菜单/权限/导航
        containerRegistry.RegisterSingleton<IMenuService, MenuService>();
        containerRegistry.RegisterSingleton<IPermissionService, PermissionService>();
        containerRegistry.RegisterSingleton<INavigationRegistry, NavigationRegistry>();
        
        // 6. 注册用户服务
        containerRegistry.RegisterSingleton<IUserService, UserService>();
        
        // 7. 注册事件聚合器（Prism 内置）
        // IEventAggregator 由 Prism 自动注册
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 始终加载
        moduleCatalog.AddModule<ShellModule>();
        moduleCatalog.AddModule<DiagnosticModule>();
        
        // 按配置加载
        if (AppConstants.ProjectType == "Vision")
            moduleCatalog.AddModule<VisionModule>();
        if (AppConstants.Modules.Contains("UserManagement"))
            moduleCatalog.AddModule<UserManagementModule>();
        // ... 其他模块
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        var configService = Container.Resolve<IConfigService>();
        var pluginLoader = Container.Resolve<PluginLoader>();
        var logger = Container.Resolve<ILogger>();
        
        // 数据库初始化
        var dbInitializer = new DatabaseInitializer(configService);
        dbInitializer.InitializeAsync().GetAwaiter().GetResult();
        
        // 插件加载（异步同步化，启动阶段可接受）
        var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        pluginLoader.LoadPluginsAsync(pluginDir).GetAwaiter().GetResult();
        
        // 多语言设置
        var lang = configService.GetValueAsync<string>("App.Language").GetAwaiter().GetResult() ?? "zh-CN";
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);
        Thread.CurrentThread.CurrentCulture = new CultureInfo(lang);
        
        // 健康检查启动
        var healthCheck = Container.Resolve<HealthCheckService>();
        healthCheck.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        
        logger.Information("Application initialized successfully. Plugins loaded: {Count}", pluginLoader.LoadedPlugins.Count);
    }
}
```

### 5.3 启动顺序时序图

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  appsettings │     │   Serilog    │     │    SQLite     │     │   Health     │
│   .json      │     │   Logger     │     │  ConfigService│     │   Check      │
└──────┬───────┘     └──────┬───────┘     └──────┬───────┘     └──────┬───────┘
       │                    │                    │                    │
       │  1. Read Config    │                    │                    │
       ├───────────────────►│                    │                    │
       │                    │  2. Init Logger    │                    │
       │                    ├───────────────────►│                    │
       │                    │                    │  3. Init DB        │
       │                    │                    ├───────────────────►│
       │                    │                    │                    │  4. Start HC
       │                    │                    │                    ├────────►
       │                    │                    │                    │
       ▼                    ▼                    ▼                    ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                              5. Plugin Loading                               │
│  PluginScanner → PluginLoader → VersionCheck → InitializeAsync()            │
│  Failed plugins logged and skipped                                          │
└──────────────────────────────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                        6. Register Prism Modules                             │
│  ShellModule → DiagnosticModule → BusinessModules                           │
└──────────────────────────────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                   7. Register Services (Menu/Permission/Nav)                 │
│  MenuService collects IMenuModule → PermissionService loads roles           │
│  NavigationRegistry registers all page regions                              │
└──────────────────────────────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                        8. Set Language Resources                             │
│  Read ConfigService → Set CultureInfo → Load .resx                          │
└──────────────────────────────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                          9. Show UI                                          │
│  Permission enabled? → LoginWindow → Verify → MainWindow                    │
│  Permission disabled? → Direct MainWindow                                   │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. 插件系统完整规范

### 6.1 接口定义（完整代码）

#### 6.1.1 IPlugin 接口

```csharp
/// <summary>
/// 插件接口，所有插件必须实现此接口。
/// 版本：当前为 V1，变更时需同步更新 PluginInterfaceVersions.Current。
/// </summary>
public interface IPlugin
{
    /// <summary>插件唯一标识，建议使用 GUID 或反向域名格式</summary>
    string Id { get; }
    
    /// <summary>插件显示名称</summary>
    string Name { get; }
    
    /// <summary>插件自身版本号（SemVer 格式）</summary>
    string Version { get; }
    
    /// <summary>接口版本号，必须等于 PluginInterfaceVersions.Current</summary>
    int InterfaceVersion { get; }
    
    /// <summary>当前状态</summary>
    PluginState State { get; }
    
    /// <summary>
    /// 初始化插件。在插件加载时由 PluginLoader 调用。
    /// </summary>
    Task InitializeAsync(IEventAggregator eventAggregator, 
                         IConfigService configService,
                         ILogger logger);
    
    /// <summary>
    /// 关闭插件，释放资源。在程序退出或插件卸载时调用。
    /// </summary>
    Task ShutdownAsync();
}
```

#### 6.1.2 PluginBase 抽象基类

```csharp
/// <summary>
/// 插件基类，提供状态机、依赖注入、资源管理的默认实现。
/// 所有插件必须继承此类并实现 InitializePluginAsync() 和 DisposeResourcesAsync()。
/// </summary>
public abstract class PluginBase : IPlugin, IAsyncDisposable
{
    // ---- IPlugin 属性 ----
    public string Id { get; protected set; }
    public string Name { get; protected set; }
    public string Version { get; protected set; }
    public virtual int InterfaceVersion => PluginInterfaceVersions.Current;
    public PluginState State => _stateMachine.CurrentState;

    // ---- 注入的依赖（子类可访问） ----
    protected IEventAggregator EventAggregator { get; private set; }
    protected IConfigService ConfigService { get; private set; }
    protected ILogger Logger { get; private set; }

    // ---- 内部状态机 ----
    private readonly PluginStateMachine _stateMachine;
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    protected PluginBase()
    {
        _stateMachine = new PluginStateMachine();
    }

    // ---- IPlugin.InitializeAsync 实现 ----
    public async Task InitializeAsync(
        IEventAggregator eventAggregator,
        IConfigService configService,
        ILogger logger)
    {
        // 1. 版本校验
        if (InterfaceVersion != PluginInterfaceVersions.Current)
        {
            throw new PluginVersionMismatchException(
                $"Plugin '{Name}' (v{Version}) requires interface version {InterfaceVersion}, " +
                $"but current is {PluginInterfaceVersions.Current}");
        }

        // 2. 注入依赖
        EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        ConfigService = configService ?? throw new ArgumentNullException(nameof(configService));
        Logger = logger?.ForContext("PluginId", Id)?.ForContext("PluginName", Name) 
                 ?? throw new ArgumentNullException(nameof(logger));

        // 3. 状态机切换到 Normal
        await _stateLock.WaitAsync();
        try
        {
            _stateMachine.TransitionTo(PluginState.Normal);
        }
        finally
        {
            _stateLock.Release();
        }

        // 4. 子类初始化
        try
        {
            await InitializePluginAsync();
            Logger.Information("Plugin '{PluginName}' initialized successfully", Name);
        }
        catch (Exception ex)
        {
            _stateMachine.TransitionTo(PluginState.Faulted);
            Logger.Error(ex, "Plugin '{PluginName}' failed during initialization", Name);
            throw;
        }
    }

    // ---- 子类必须实现 ----
    
    /// <summary>
    /// 插件初始化逻辑。在此方法中注册事件订阅、加载配置、建立连接等。
    /// 状态已为 Normal，可安全使用 EventAggregator、ConfigService、Logger。
    /// </summary>
    protected abstract Task InitializePluginAsync();

    /// <summary>
    /// 资源释放逻辑。在此方法中关闭连接、释放非托管资源、取消事件订阅等。
    /// 调用时状态已切换为 Offline。
    /// </summary>
    protected abstract Task DisposeResourcesAsync();

    // ---- ShutdownAsync ----
    public async Task ShutdownAsync()
    {
        Logger.Information("Plugin '{PluginName}' shutting down...", Name);
        _stateMachine.TransitionTo(PluginState.Offline);
        await DisposeResourcesAsync();
    }

    // ---- IAsyncDisposable ----
    public async ValueTask DisposeAsync()
    {
        if (State != PluginState.Offline)
        {
            _stateMachine.TransitionTo(PluginState.Offline);
        }
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await DisposeResourcesAsync().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Logger.Warning("Plugin '{PluginName}' dispose timed out after 5 seconds", Name);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Plugin '{PluginName}' error during dispose", Name);
        }
        
        _stateLock.Dispose();
        GC.SuppressFinalize(this);
    }

    // ---- 状态检查辅助方法 ----
    
    /// <summary>
    /// 在执行核心操作前检查插件状态。如果不是 Normal 状态，抛出 InvalidOperationException。
    /// 子类在所有对外方法中应调用此方法。
    /// </summary>
    protected void EnsureNormalState([CallerMemberName] string caller = "")
    {
        if (State != PluginState.Normal)
        {
            throw new InvalidOperationException(
                $"Plugin '{Name}' cannot execute '{caller}' in state {State}");
        }
    }
}
```

#### 6.1.3 PluginState 枚举与状态机

```csharp
public enum PluginState
{
    /// <summary>正常运行</summary>
    Normal = 0,
    
    /// <summary>发生错误，需手动重置</summary>
    Faulted = 1,
    
    /// <summary>已关闭/离线</summary>
    Offline = 2
}

/// <summary>
/// 插件状态机，保证状态转换的原子性和合法性。
/// </summary>
public class PluginStateMachine
{
    private PluginState _currentState = PluginState.Offline;
    
    public PluginState CurrentState => _currentState;
    
    // 合法的状态转换表
    private static readonly Dictionary<PluginState, HashSet<PluginState>> AllowedTransitions = new()
    {
        { PluginState.Offline, new HashSet<PluginState> { PluginState.Normal } },
        { PluginState.Normal, new HashSet<PluginState> { PluginState.Faulted, PluginState.Offline } },
        { PluginState.Faulted, new HashSet<PluginState> { PluginState.Normal, PluginState.Offline } }
    };
    
    public void TransitionTo(PluginState target)
    {
        if (!AllowedTransitions.TryGetValue(_currentState, out var allowed) || !allowed.Contains(target))
        {
            throw new InvalidOperationException(
                $"Invalid state transition: {_currentState} → {target}");
        }
        _currentState = target;
    }
}
```

#### 6.1.4 PluginInterfaceVersions

```csharp
/// <summary>
/// 插件接口版本常量。变更 IPlugin 接口时必须递增此值。
/// </summary>
public static class PluginInterfaceVersions
{
    public const int Current = 1;
}
```

### 6.2 插件加载流程

```csharp
public class PluginLoader
{
    private readonly ILogger _logger;
    private readonly List<PluginLoadResult> _results = new();
    private readonly List<(IPlugin Plugin, AssemblyLoadContext Context)> _loadedPlugins = new();

    public IReadOnlyList<IPlugin> LoadedPlugins => _loadedPlugins.Select(x => x.Plugin).ToList();

    public async Task LoadPluginsAsync(string pluginsDirectory)
    {
        if (!Directory.Exists(pluginsDirectory))
        {
            _logger.Warning("Plugins directory not found: {Path}", pluginsDirectory);
            return;
        }

        var dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly);
        
        foreach (var dllPath in dllFiles)
        {
            var result = await LoadSinglePluginAsync(dllPath);
            _results.Add(result);
        }
        
        _logger.Information("Plugin loading complete. Success: {Success}, Failed: {Failed}", 
            _results.Count(r => r.Success), _results.Count(r => !r.Success));
    }

    private async Task<PluginLoadResult> LoadSinglePluginAsync(string dllPath)
    {
        var fileName = Path.GetFileName(dllPath);
        var context = new PluginLoadContext(dllPath);
        
        try
        {
            var assembly = context.LoadFromAssemblyPath(dllPath);
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
            
            if (pluginType == null)
            {
                context.Unload();
                return PluginLoadResult.Skipped(fileName, "No IPlugin implementation found");
            }

            var plugin = (IPlugin)Activator.CreateInstance(pluginType);
            
            // 版本校验
            if (plugin.InterfaceVersion != PluginInterfaceVersions.Current)
            {
                context.Unload();
                return PluginLoadResult.Failed(fileName, 
                    $"Interface version mismatch: {plugin.InterfaceVersion} vs {PluginInterfaceVersions.Current}");
            }

            // 初始化
            await plugin.InitializeAsync(eventAggregator, configService, logger);
            
            _loadedPlugins.Add((plugin, context));
            return PluginLoadResult.Success(fileName, plugin.Id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load plugin: {FileName}", fileName);
            context.Unload();
            return PluginLoadResult.Failed(fileName, ex.Message);
        }
    }

    /// <summary>
    /// 卸载所有插件（LIFO 顺序）。
    /// </summary>
    public async Task UnloadAllAsync()
    {
        for (int i = _loadedPlugins.Count - 1; i >= 0; i--)
        {
            var (plugin, context) = _loadedPlugins[i];
            try
            {
                await plugin.DisposeAsync();
                context.Unload();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error unloading plugin: {PluginId}", plugin.Id);
            }
        }
        _loadedPlugins.Clear();
    }
}

/// <summary>
/// 插件专用 AssemblyLoadContext，实现依赖隔离。
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }
        return null; // 回退到默认上下文
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }
        return IntPtr.Zero;
    }
}
```

### 6.3 事件总线通信

```csharp
// 事件定义（全部在 Core 项目中）

/// <summary>数据更新事件，驱动发布，UI 模块订阅</summary>
public class DataUpdatedEvent
{
    public string DriverId { get; set; }
    public IReadOnlyList<Tag> UpdatedTags { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>插件状态变更事件</summary>
public class PluginStateChangedEvent
{
    public string PluginId { get; set; }
    public string PluginName { get; set; }
    public PluginState OldState { get; set; }
    public PluginState NewState { get; set; }
    public string Reason { get; set; }
}

/// <summary>报警事件</summary>
public class AlarmEvent
{
    public string Source { get; set; }
    public AlarmLevel Level { get; set; }  // Info, Warning, Error, Critical
    public string Message { get; set; }
    public Dictionary<string, object> Details { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>推理结果事件（视觉类）</summary>
public class InferenceResultEvent
{
    public string CameraId { get; set; }
    public InferenceResult Result { get; set; }
    public Bitmap OriginalImage { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

## 7. 工业采集类专项规范

### 7.1 IDriver 接口

```csharp
/// <summary>
/// 通信驱动接口，所有协议驱动必须实现。
/// </summary>
public interface IDriver
{
    /// <summary>驱动唯一标识</summary>
    string DriverId { get; }
    
    /// <summary>协议名称（如 "SiemensS7", "ModbusTcp"）</summary>
    string ProtocolName { get; }
    
    /// <summary>是否已连接</summary>
    bool IsConnected { get; }
    
    /// <summary>连接到设备</summary>
    Task<bool> ConnectAsync(ConnectionParams parameters);
    
    /// <summary>断开连接</summary>
    Task DisconnectAsync();
    
    /// <summary>读取单个 Tag</summary>
    Task<DataValue> ReadAsync(string address);
    
    /// <summary>写入单个 Tag</summary>
    Task<bool> WriteAsync(string address, object value);
    
    /// <summary>批量读取</summary>
    Task<IDictionary<string, DataValue>> ReadBatchAsync(IEnumerable<string> addresses);
}
```

### 7.2 ConnectionParams 模型

```csharp
public class ConnectionParams
{
    public string IpAddress { get; set; } = "192.168.1.1";
    public int Port { get; set; } = 102;
    
    // S7 专属
    public int Rack { get; set; } = 0;
    public int Slot { get; set; } = 1;
    
    // OPC UA 专属
    public string EndpointUrl { get; set; } = "opc.tcp://localhost:4840";
    
    // 通用
    public int TimeoutMs { get; set; } = 3000;
    public int RetryCount { get; set; } = 3;
    public int RetryIntervalMs { get; set; } = 1000;
}
```

### 7.3 DataValue 模型

```csharp
public class DataValue
{
    public object Value { get; set; }
    public DateTime Timestamp { get; set; }
    public TagQuality Quality { get; set; }
    
    public T GetValue<T>() => Value is T typed ? typed : default;
}

public enum TagQuality : byte
{
    Good = 0x00,
    Bad = 0x01,
    Uncertain = 0x02,
    Timeout = 0x03
}
```

### 7.4 Tag 模型

```csharp
public class Tag
{
    public string Id { get; set; }
    public string Address { get; set; }      // PLC 地址，如 "DB1.0", "40001"
    public string Alias { get; set; }        // 业务别名，如 "MotorSpeed"
    public TagDataType DataType { get; set; }
    public double ScanRateMs { get; set; } = 100;  // 扫描周期
    public bool IsActive { get; set; } = true;
}

public enum TagDataType
{
    Bool,
    Int16,
    Int32,
    Int64,
    Float32,
    Float64,
    String,
    DateTime
}
```

### 7.5 模拟驱动实现规范

```csharp
/// <summary>
/// 模拟驱动，生成正弦波/随机值/阶梯波数据。
/// 用于无硬件环境下的开发调试。
/// </summary>
public class SimDriver : IDriver
{
    public string DriverId => "Simulation";
    public string ProtocolName => "Simulation";
    public bool IsConnected => _isConnected;
    
    private bool _isConnected;
    private readonly Dictionary<string, SimTagConfig> _simTags = new();
    private readonly Random _random = new();
    private Timer _updateTimer;
    private IEventAggregator _eventAggregator;
    
    public Task<bool> ConnectAsync(ConnectionParams parameters)
    {
        _isConnected = true;
        StartSimulation();
        return Task.FromResult(true);
    }
    
    public Task DisconnectAsync()
    {
        _isConnected = false;
        _updateTimer?.Dispose();
        return Task.CompletedTask;
    }
    
    public Task<DataValue> ReadAsync(string address)
    {
        if (!_simTags.TryGetValue(address, out var config))
        {
            config = new SimTagConfig { Pattern = SimPattern.Random, MinValue = 0, MaxValue = 100 };
            _simTags[address] = config;
        }
        
        var value = GenerateValue(config);
        return Task.FromResult(new DataValue
        {
            Value = value,
            Timestamp = DateTime.Now,
            Quality = TagQuality.Good
        });
    }
    
    public Task<bool> WriteAsync(string address, object value)
    {
        // 模拟驱动忽略写入，直接返回成功
        return Task.FromResult(true);
    }
    
    public Task<IDictionary<string, DataValue>> ReadBatchAsync(IEnumerable<string> addresses)
    {
        var result = new Dictionary<string, DataValue>();
        foreach (var addr in addresses)
        {
            result[addr] = ReadAsync(addr).GetAwaiter().GetResult();
        }
        return Task.FromResult<IDictionary<string, DataValue>>(result);
    }
    
    private void StartSimulation()
    {
        _updateTimer = new Timer(async _ =>
        {
            if (!_isConnected) return;
            
            var tags = _simTags.Select(kv => new Tag
            {
                Id = kv.Key,
                Address = kv.Key,
                Value = GenerateValue(kv.Value),
                Timestamp = DateTime.Now,
                Quality = TagQuality.Good
            }).ToList();
            
            _eventAggregator?.GetEvent<DataUpdatedEvent>().Publish(new DataUpdatedEvent
            {
                DriverId = DriverId,
                UpdatedTags = tags,
                Timestamp = DateTime.Now
            });
        }, null, 0, 100); // 100ms 刷新
    }
    
    private double GenerateValue(SimTagConfig config)
    {
        return config.Pattern switch
        {
            SimPattern.Random => config.MinValue + _random.NextDouble() * (config.MaxValue - config.MinValue),
            SimPattern.SineWave => config.MinValue + (config.MaxValue - config.MinValue) * 
                (0.5 + 0.5 * Math.Sin(DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond * config.Frequency)),
            SimPattern.StepWave => DateTime.Now.Second % 10 < 5 ? config.MinValue : config.MaxValue,
            _ => 0
        };
    }
}

public enum SimPattern { Random, SineWave, StepWave }

public class SimTagConfig
{
    public SimPattern Pattern { get; set; } = SimPattern.Random;
    public double MinValue { get; set; } = 0;
    public double MaxValue { get; set; } = 100;
    public double Frequency { get; set; } = 1.0; // Hz，正弦波频率
}
```

---

## 8. 视觉检测类专项规范

### 8.1 ICameraService 接口

```csharp
public interface ICameraService
{
    string CameraId { get; }
    string CameraName { get; }
    bool IsOpen { get; }
    
    Task<bool> OpenAsync(CameraConfig config);
    Task CloseAsync();
    Task<Bitmap> CaptureAsync();
    Task StartContinuousCaptureAsync(CancellationToken ct);
    event EventHandler<FrameCapturedEventArgs> FrameCaptured;
}

public class CameraConfig
{
    public string CameraType { get; set; }   // "Hikvision", "Daheng", "Basler", "Simulate"
    public string DeviceId { get; set; }     // 设备序列号或索引
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public double FrameRate { get; set; } = 30.0;
    public Dictionary<string, object> ExtraParams { get; set; } = new();
}

public class FrameCapturedEventArgs : EventArgs
{
    public Bitmap Frame { get; set; }
    public DateTime Timestamp { get; set; }
    public long FrameIndex { get; set; }
}
```

### 8.2 相机工厂

```csharp
public class CameraFactory
{
    private static readonly Dictionary<string, Type> CameraTypes = new()
    {
        { "Hikvision", typeof(HikCameraService) },
        { "Daheng", typeof(DahengCameraService) },
        { "Basler", typeof(BaslerCameraService) },
        { "Simulate", typeof(SimCameraService) }
    };
    
    public static ICameraService Create(string cameraType)
    {
        if (!CameraTypes.TryGetValue(cameraType, out var type))
        {
            throw new ArgumentException($"Unknown camera type: {cameraType}");
        }
        return (ICameraService)Activator.CreateInstance(type);
    }
}
```

### 8.3 推理引擎

```csharp
public abstract class InferenceEngineBase : IDisposable
{
    protected string ModelPath { get; private set; }
    protected ILogger Logger { get; private set; }
    
    private InferenceSession _session;
    private bool _isLoaded;
    
    public bool IsLoaded => _isLoaded;
    
    protected InferenceEngineBase(ILogger logger)
    {
        Logger = logger;
    }
    
    public async Task LoadModelAsync(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Model file not found: {modelPath}");
        
        ModelPath = modelPath;
        
        await Task.Run(() =>
        {
            _session = new InferenceSession(modelPath);
            _isLoaded = true;
            Logger.Information("Model loaded: {ModelPath}, Input: {InputName}", 
                modelPath, _session.InputMetadata.First().Key);
        });
    }
    
    public async Task<InferenceResult> RunAsync(Bitmap image)
    {
        if (!_isLoaded)
            throw new InvalidOperationException("Model not loaded. Call LoadModelAsync first.");
        
        return await Task.Run(() =>
        {
            var inputTensor = Preprocess(image);
            var inputs = new Dictionary<string, OrtValue>
            {
                { _session.InputMetadata.First().Key, OrtValue.CreateTensor(inputTensor) }
            };
            
            using var outputs = _session.Run(new RunOptions(), inputs, _session.OutputNames);
            return Postprocess(outputs);
        });
    }
    
    protected abstract Tensor<float> Preprocess(Bitmap image);
    protected abstract InferenceResult Postprocess(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs);
    
    public void Dispose()
    {
        _session?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class InferenceResult
{
    public string Label { get; set; }
    public float Confidence { get; set; }
    public RectangleF BoundingBox { get; set; }  // 检测模式使用
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

### 8.4 结果拦截器管道

```csharp
public interface IPipelineStep
{
    string StepName { get; }
    int Order { get; }
    Task<PipelineContext> ExecuteAsync(PipelineContext context);
}

public class PipelineContext
{
    public Bitmap OriginalImage { get; set; }
    public InferenceResult Result { get; set; }
    public Dictionary<string, object> SharedData { get; set; } = new();
    public bool ShouldAbort { get; set; }
}

/// <summary>
/// 管道引擎，按 Order 顺序执行所有步骤。
/// </summary>
public class PipelineEngine
{
    private readonly List<IPipelineStep> _steps = new();
    private readonly ILogger _logger;
    
    public PipelineEngine(ILogger logger)
    {
        _logger = logger;
    }
    
    public void AddStep(IPipelineStep step)
    {
        _steps.Add(step);
        _steps.Sort((a, b) => a.Order.CompareTo(b.Order));
    }
    
    public async Task<PipelineContext> ExecuteAsync(PipelineContext context)
    {
        foreach (var step in _steps)
        {
            if (context.ShouldAbort)
            {
                _logger.Information("Pipeline aborted before step: {StepName}", step.StepName);
                break;
            }
            
            try
            {
                context = await step.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Pipeline step failed: {StepName}", step.StepName);
                // 单步失败不中断管道，记录日志后继续
            }
        }
        return context;
    }
}

/// <summary>
/// 预置示例步骤：连续 NG 超限报警。
/// 连续 N 次置信度低于阈值时触发 AlarmEvent。
/// </summary>
public class ConsecutiveNgAlarmStep : IPipelineStep
{
    public string StepName => "ConsecutiveNgAlarm";
    public int Order => 100;
    
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger _logger;
    private readonly float _confidenceThreshold;
    private readonly int _consecutiveLimit;
    private int _consecutiveCount;
    
    public ConsecutiveNgAlarmStep(
        IEventAggregator eventAggregator, 
        ILogger logger,
        float confidenceThreshold = 0.8f,
        int consecutiveLimit = 5)
    {
        _eventAggregator = eventAggregator;
        _logger = logger;
        _confidenceThreshold = confidenceThreshold;
        _consecutiveLimit = consecutiveLimit;
    }
    
    public Task<PipelineContext> ExecuteAsync(PipelineContext context)
    {
        if (context.Result.Confidence < _confidenceThreshold)
        {
            _consecutiveCount++;
            if (_consecutiveCount >= _consecutiveLimit)
            {
                _eventAggregator.GetEvent<AlarmEvent>().Publish(new AlarmEvent
                {
                    Source = "VisionPipeline",
                    Level = AlarmLevel.Error,
                    Message = $"连续 {_consecutiveCount} 次检测结果低于置信度阈值 {_confidenceThreshold:P0}",
                    Timestamp = DateTime.Now
                });
                _logger.Warning("Consecutive NG alarm triggered: {Count} times", _consecutiveCount);
            }
        }
        else
        {
            _consecutiveCount = 0;
        }
        
        return Task.FromResult(context);
    }
}
```

---

## 9. 系统定制类专项规范

### 9.1 菜单模块自注册

```csharp
public interface IMenuModule
{
    string MenuKey { get; }
    string DisplayNameResourceKey { get; }   // .resx 中的 Key
    string IconKind { get; }                 // MaterialDesign 图标名
    UserRole RequiredRole { get; }
    string TargetViewName { get; }           // Prism Region 导航目标
    int SortOrder { get; }
    string ParentMenuKey { get; }            // null = 顶级菜单
}
```

### 9.2 角色定义

```csharp
[Flags]
public enum UserRole
{
    None = 0,
    Operator = 1,
    Engineer = 2,
    Administrator = 4 | Engineer | Operator  // Admin 包含所有权限
}
```

### 9.3 用户服务

```csharp
public interface IUserService
{
    Task<User> AuthenticateAsync(string username, string password);
    Task<User> GetUserAsync(string username);
    Task<List<User>> GetAllUsersAsync();
    Task CreateUserAsync(User user, string password);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(string username);
    Task ChangePasswordAsync(string username, string oldPassword, string newPassword);
    Task<bool> ForceChangePasswordOnNextLogin(string username);
}

public class User
{
    public string Username { get; set; }
    public string DisplayName { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 9.4 种子数据

```csharp
public static class SeedData
{
    public static readonly (string Username, string Password, UserRole Role, string DisplayName)[] Users =
    {
        ("admin", "admin123", UserRole.Administrator, "管理员"),
        ("engineer", "engineer123", UserRole.Engineer, "工程师"),
        ("operator", "operator123", UserRole.Operator, "操作员")
    };
}
```

---

## 10. 模板引擎与生成流程

### 10.1 Scriban 模板语法规范

#### 10.1.1 变量引用

```
{{ variable_name }}
{{ object.property }}
```

#### 10.1.2 条件判断

```
{{ if EnableSerilogFile }}
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
{{ end }}
```

#### 10.1.3 循环

```
{{ for driver in Drivers }}
// Driver: {{ driver.Name }}
{{ end }}
```

#### 10.1.4 注释

```
{{# 这是 Scriban 注释，不会输出到生成文件 #}}
```

### 10.2 完整变量清单

#### 10.2.1 基础变量

| 变量名 | 类型 | 说明 | 示例值 |
|--------|------|------|--------|
| `ProjectName` | string | 项目名称（原始输入） | `MyFactory` |
| `NamespacePrefix` | string | 命名空间前缀（PascalCase） | `MyFactory` |
| `TargetFramework` | string | 目标框架标识 | `net8.0-windows` |
| `TargetFrameworkShort` | string | 短格式 | `net8.0` |
| `UIFramework` | string | UI 框架 | `WPF` / `Avalonia` |
| `ProjectType` | string | 项目类型 | `Collection` / `Vision` / `System` |
| `ProjectDescription` | string | 项目描述 | `工业采集系统` |
| `ScaffoldXVersion` | string | 脚手架版本 | `1.0.0` |
| `GeneratedAt` | string | 生成时间 | `2026-04-28T00:40:00` |
| `DotNetVersion` | string | .NET 版本号 | `8.0` |
| `IsWPF` | bool | 是否 WPF | `true` / `false` |
| `IsAvalonia` | bool | 是否 Avalonia | `true` / `false` |

#### 10.2.2 采集类变量

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `EnableSimulationDriver` | bool | 是否生成模拟驱动 |
| `EnableSiemensS7` | bool | 是否生成 S7 驱动 |
| `EnableModbusTcp` | bool | 是否生成 Modbus 驱动 |
| `EnableOpcUa` | bool | 是否生成 OPC UA 驱动 |
| `EnableMitsubishiMc` | bool | 是否生成三菱 MC 驱动 |
| `DefaultPLCIp` | string | 默认 PLC IP |
| `DefaultPLCPort` | int | 默认端口 |
| `S7Rack` | int | S7 机架号 |
| `S7Slot` | int | S7 槽号 |
| `OpcUaEndpoint` | string | OPC UA Endpoint URL |

#### 10.2.3 视觉类变量

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `CameraBrand` | string | 相机品牌 |
| `EnableSimCamera` | bool | 是否生成模拟相机 |
| `EnableHikvision` | bool | 海康 |
| `EnableDaheng` | bool | 大恒 |
| `EnableBasler` | bool | Basler |
| `ModelType` | string | `Classification` / `Detection` |
| `ModelPath` | string | 模型文件路径（可空） |
| `EnablePipeline` | bool | 是否生成管道 |

#### 10.2.4 系统类变量

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `EnableUserManagement` | bool | 用户管理模块 |
| `EnableRolePermission` | bool | 角色权限模块 |
| `EnableSystemLog` | bool | 系统日志模块 |
| `EnableThemeSwitcher` | bool | 主题切换模块 |
| `EnableLoginWindow` | bool | 是否启用登录窗口 |
| `EnableCrossPlatform` | bool | 跨平台发布（仅 Avalonia） |
| `ForcePasswordChange` | bool | 强制首次修改密码 |

#### 10.2.5 XAML 文件名变量

| 变量名 | WPF 值 | Avalonia 值 |
|--------|--------|-------------|
| `XamlExt` | `.xaml` | `.axaml` |
| `XamlCodeBehindExt` | `.xaml.cs` | `.axaml.cs` |
| `XamlRootTag` | `Window` / `UserControl` | `Window` / `UserControl` |
| `XamlNs` | `http://schemas.microsoft.com/winfx/2006/xaml/presentation` | `https://github.com/avaloniaui` |
| `XamlXNs` | `http://schemas.microsoft.com/winfx/2006/xaml` | `http://schemas.microsoft.com/winfx/2006/xaml` |

### 10.3 模板文件命名规范

```
{项目类型}_{层级}_{文件名}.stpl
```

示例：
- `Common_Core_IPlugin.cs.stpl` — 通用核心层 IPlugin 接口
- `Collection_Drivers_SimDriver.cs.stpl` — 采集类模拟驱动
- `Vision_Inference_InferenceEngineBase.cs.stpl` — 视觉类推理引擎基类
- `System_UserManagement_UserService.cs.stpl` — 系统类用户服务

**目录映射规则**：
模板文件的目录结构映射到生成项目的目录结构。例如：
```
模板: Collection/Drivers/Simulation/SimDriver.cs.stpl
生成: src/{ProjectName}.Drivers.Simulation/SimDriver.cs
```

### 10.4 `[AGENT-SPEC]` 生成流程（伪代码）

```csharp
public class ProjectGenerator : IProjectGenerator
{
    private readonly ITemplateEngine _templateEngine;
    private readonly IValidationService _validationService;
    private readonly IHistoryService _historyService;
    private readonly ILogger _logger;

    public async Task<GenerationResult> GenerateAsync(ProjectConfig config, IProgress<GenerationProgress> progress)
    {
        // ===== 步骤 1: 验证配置 =====
        progress.Report(new GenerationProgress("正在验证配置...", 0));
        
        var nameValidation = _validationService.ValidateProjectName(config.ProjectName);
        if (!nameValidation.IsValid)
            return GenerationResult.Fail(nameValidation.ErrorMessage);
        
        var pathValidation = _validationService.ValidateOutputPath(config.OutputPath);
        if (!pathValidation.IsValid)
            return GenerationResult.Fail(pathValidation.ErrorMessage);
        
        // ===== 步骤 2: 构建变量上下文 =====
        progress.Report(new GenerationProgress("正在解析模板...", 10));
        
        var variables = BuildVariableContext(config);
        
        // ===== 步骤 3: 选择模板文件 =====
        var templates = SelectTemplates(config);
        
        // ===== 步骤 4: 渲染模板 =====
        progress.Report(new GenerationProgress("正在创建目录结构...", 20));
        
        var outputRoot = Path.Combine(config.OutputPath, config.ProjectName);
        Directory.CreateDirectory(outputRoot);
        
        int totalFiles = templates.Count;
        int processedFiles = 0;
        
        foreach (var template in templates)
        {
            var relativePath = _templateEngine.Render(template.OutputPathTemplate, variables);
            var fullPath = Path.Combine(outputRoot, relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(directory);
            
            var content = _templateEngine.Render(template.Content, variables);
            await File.WriteAllTextAsync(fullPath, content, Encoding.UTF8);
            
            processedFiles++;
            int percent = 20 + (int)(70.0 * processedFiles / totalFiles);
            progress.Report(new GenerationProgress($"正在生成 {Path.GetFileName(relativePath)}...", percent));
        }
        
        // ===== 步骤 5: 后处理 =====
        progress.Report(new GenerationProgress("正在执行后处理...", 90));
        
        await PostProcessAsync(outputRoot, config, variables);
        
        // ===== 步骤 6: 记录历史 =====
        progress.Report(new GenerationProgress("完成！", 100));
        
        await _historyService.SaveAsync(new ProjectHistory
        {
            ProjectName = config.ProjectName,
            ProjectType = config.ProjectType,
            OutputPath = config.OutputPath,
            TargetFramework = config.TargetFramework,
            UIFramework = config.UIFramework,
            CreatedAt = DateTime.Now,
            ConfigJson = JsonSerializer.Serialize(config)
        });
        
        return GenerationResult.Success(outputRoot, processedFiles);
    }

    private Dictionary<string, object> BuildVariableContext(ProjectConfig config)
    {
        var vars = new Dictionary<string, object>
        {
            ["ProjectName"] = config.ProjectName,
            ["NamespacePrefix"] = _validationService.ToPascalCase(config.ProjectName),
            ["TargetFramework"] = config.TargetFramework switch
            {
                ".NET 6" => config.UIFramework == "WPF" ? "net6.0-windows" : "net6.0",
                ".NET 8" => config.UIFramework == "WPF" ? "net8.0-windows" : "net8.0",
                _ => "net8.0-windows"
            },
            ["UIFramework"] = config.UIFramework,
            ["ProjectType"] = config.ProjectType,
            ["IsWPF"] = config.UIFramework == "WPF",
            ["IsAvalonia"] = config.UIFramework == "Avalonia",
            ["ScaffoldXVersion"] = "1.0.0",
            ["GeneratedAt"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            ["DotNetVersion"] = config.DotNetVersion.Replace(".NET ", ""),
            ["XamlExt"] = config.UIFramework == "WPF" ? ".xaml" : ".axaml",
            ["XamlCodeBehindExt"] = config.UIFramework == "WPF" ? ".xaml.cs" : ".axaml.cs",
            ["XamlRootTag"] = "Window",
            ["XamlNs"] = config.UIFramework == "WPF" 
                ? "http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                : "https://github.com/avaloniaui",
            ["XamlXNs"] = "http://schemas.microsoft.com/winfx/2006/xaml",
        };
        
        // 采集类变量
        if (config.ProjectType == "Collection")
        {
            vars["EnableSimulationDriver"] = config.EnableSimulationDriver;
            vars["EnableSiemensS7"] = config.SelectedDrivers.Contains("SiemensS7");
            vars["EnableModbusTcp"] = config.SelectedDrivers.Contains("ModbusTcp");
            vars["EnableOpcUa"] = config.SelectedDrivers.Contains("OpcUa");
            vars["EnableMitsubishiMc"] = config.SelectedDrivers.Contains("MitsubishiMc");
            vars["DefaultPLCIp"] = config.DefaultPLCIp ?? "192.168.1.1";
            vars["DefaultPLCPort"] = config.DefaultPLCPort;
            vars["S7Rack"] = config.S7Rack;
            vars["S7Slot"] = config.S7Slot;
            vars["OpcUaEndpoint"] = config.OpcUaEndpoint ?? "opc.tcp://localhost:4840";
        }
        
        // 视觉类变量
        if (config.ProjectType == "Vision")
        {
            vars["CameraBrand"] = config.CameraBrand;
            vars["EnableSimCamera"] = true; // 始终包含
            vars["EnableHikvision"] = config.CameraBrand == "海康";
            vars["EnableDaheng"] = config.CameraBrand == "大恒";
            vars["EnableBasler"] = config.CameraBrand == "Basler";
            vars["ModelType"] = config.ModelType;
            vars["ModelPath"] = config.ModelPath ?? "";
            vars["EnablePipeline"] = config.EnablePipeline;
        }
        
        // 系统类变量
        if (config.ProjectType == "System")
        {
            vars["EnableUserManagement"] = config.SelectedModules.Contains("UserManagement");
            vars["EnableRolePermission"] = config.SelectedModules.Contains("RolePermission");
            vars["EnableSystemLog"] = config.SelectedModules.Contains("SystemLog");
            vars["EnableThemeSwitcher"] = config.SelectedModules.Contains("ThemeSwitcher");
            vars["EnableLoginWindow"] = config.EnableLoginWindow;
            vars["EnableCrossPlatform"] = config.EnableCrossPlatform;
            vars["ForcePasswordChange"] = config.ForcePasswordChange;
        }
        
        return vars;
    }
}
```

### 10.5 后处理规范

后处理在所有模板文件生成完成后执行：

| 后处理步骤 | 说明 | 适用范围 |
|-----------|------|----------|
| `.sln` 项目引用注入 | 将所有生成的 `.csproj` 路径写入 `.sln` 文件的 `Project` 段 | 所有项目 |
| `.resx` 资源条目生成 | 根据菜单模块定义，生成对应的资源 Key-Value 条目 | 系统类 |
| `Directory.Build.props` 生成 | 写入统一的 NuGet 版本管理和编译选项 | 所有项目 |
| `global.json` 生成 | 锁定 .NET SDK 版本 | 所有项目 |
| `.gitignore` 生成 | 标准 .NET gitignore 模板 | 所有项目 |
| `README.md` 生成 | 项目说明文档 | 所有项目 |
| 发布脚本生成 | `publish.bat` / `publish.sh` | 所有项目 |
| 文件编码统一 | 所有生成文件使用 UTF-8 with BOM（`.cs`）或 UTF-8（其他） | 所有文件 |

---

## 11. 脚手架工具自身技术规范

### 11.1 脚手架项目结构

```
ScaffoldX/
│
├── ScaffoldX.sln
│
├── src/
│   ├── ScaffoldX.App/                              # 脚手架主程序
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── Views/
│   │   │   ├── MainWindow.xaml                     # 主窗口
│   │   │   ├── ProjectHistoryView.xaml             # 历史项目列表
│   │   │   ├── Step1_ProjectType.xaml              # 向导步骤一
│   │   │   ├── Step2_BasicInfo.xaml                # 向导步骤二
│   │   │   ├── Step3_SpecificConfig.xaml           # 向导步骤三（容器）
│   │   │   ├── Step3_CollectionConfig.xaml         # 采集类动态区块
│   │   │   ├── Step3_VisionConfig.xaml             # 视觉类动态区块
│   │   │   ├── Step3_SystemConfig.xaml             # 系统类动态区块
│   │   │   └── Step4_ConfirmGenerate.xaml          # 向导步骤四
│   │   ├── ViewModels/
│   │   │   ├── MainWindowViewModel.cs
│   │   │   ├── ProjectHistoryViewModel.cs
│   │   │   ├── Step1ViewModel.cs
│   │   │   ├── Step2ViewModel.cs
│   │   │   ├── Step3ViewModel.cs
│   │   │   └── Step4ViewModel.cs
│   │   ├── Models/
│   │   │   ├── ProjectConfig.cs                    # 项目配置数据模型
│   │   │   ├── ProjectHistory.cs                   # 历史记录模型
│   │   │   └── ProjectType.cs                      # 项目类型枚举
│   │   ├── Services/
│   │   │   ├── IProjectGenerator.cs
│   │   │   ├── ProjectGenerator.cs
│   │   │   ├── ITemplateEngine.cs
│   │   │   ├── ScribanTemplateEngine.cs
│   │   │   ├── IHistoryService.cs
│   │   │   ├── HistoryService.cs
│   │   │   ├── IValidationService.cs
│   │   │   └── ValidationService.cs
│   │   ├── Resources/
│   │   │   ├── Strings.zh-CN.resx
│   │   │   ├── Strings.en-US.resx
│   │   │   └── Themes/
│   │   │       ├── Colors.xaml
│   │   │       └── Styles.xaml
│   │   └── ScaffoldX.App.csproj
│   │
│   ├── ScaffoldX.Core/                             # 脚手架核心库
│   │   ├── TemplateProcessing/
│   │   │   ├── TemplateRegistry.cs
│   │   │   ├── TemplateFile.cs
│   │   │   ├── VariableResolver.cs
│   │   │   └── PostProcessor.cs
│   │   ├── FileGeneration/
│   │   │   ├── FileTreeBuilder.cs
│   │   │   ├── DirectoryCreator.cs
│   │   │   └── FileWriter.cs
│   │   └── ScaffoldX.Core.csproj
│   │
│   └── ScaffoldX.Templates/                        # 模板资源项目（嵌入资源）
│       ├── Common/                                 # 通用模板
│       │   ├── Core/                               # IPlugin, PluginBase, 状态机等
│       │   ├── App/                                # App.xaml, Bootstrapper, MainWindow
│       │   ├── Shell/                              # 侧边栏, 状态栏, 首页
│       │   ├── Diagnostic/                         # 诊断面板
│       │   ├── Solution/                           # .sln, Directory.Build.props, .gitignore
│       │   ├── Resources/                          # .resx, Styles
│       │   └── Tools/                              # publish.bat, update.bat
│       ├── Collection/                             # 工业采集类模板
│       │   ├── Drivers/Simulation/
│       │   ├── Drivers/SiemensS7/
│       │   ├── Drivers/ModbusTcp/
│       │   ├── Drivers/OpcUa/
│       │   ├── Drivers/MitsubishiMc/
│       │   └── Core/                               # IDriver, Tag, DataValue
│       ├── Vision/                                 # 视觉检测类模板
│       │   ├── Camera/Simulate/
│       │   ├── Camera/Hikvision/
│       │   ├── Camera/Daheng/
│       │   ├── Camera/Basler/
│       │   ├── Inference/
│       │   ├── Pipeline/
│       │   ├── Views/
│       │   └── Core/                               # ICameraService, CameraConfig
│       └── System/                                 # 系统定制类模板
│           ├── UserManagement/
│           ├── RolePermission/
│           ├── SystemLog/
│           ├── ThemeSwitcher/
│           └── Core/                               # UserRole, IMenuModule
│
├── tests/
│   ├── ScaffoldX.Core.Tests/
│   └── ScaffoldX.IntegrationTests/
│
└── docs/
```

### 11.2 NuGet 依赖

| 组件 | 包名 | 版本 | 用途 |
|------|------|------|------|
| 模板引擎 | Scriban | 5.10.0 | 模板渲染 |
| UI 框架 | MaterialDesignThemes | 5.1.0 | 界面风格 |
| MVVM 框架 | Prism.Unity | 8.1.97 | 脚手架自身导航/DI |
| 序列化 | System.Text.Json | 内置 | 历史记录存储 |
| 日志 | Serilog + Serilog.Sinks.File | 3.1.1 / 5.0.0 | 脚手架自身日志 |
| 文件对话框 | Ookii.Dialogs.Wpf | 4.0.0 | 文件夹选择器 |
| 验证 | FluentValidation | 11.9.0 | 输入验证（可选） |

### 11.3 ProjectConfig 完整模型

```csharp
public class ProjectConfig
{
    // ===== 基础信息 =====
    public string ProjectName { get; set; }
    public string OutputPath { get; set; }
    public string NamespacePrefix { get; set; }
    public string UIFramework { get; set; }       // "WPF" | "Avalonia"
    public string DotNetVersion { get; set; }     // ".NET 6" | ".NET 8"
    public string ProjectType { get; set; }       // "Collection" | "Vision" | "System"
    public string ProjectDescription { get; set; }
    
    // ===== 采集类 =====
    public List<string> SelectedDrivers { get; set; } = new();
    public bool EnableSimulationDriver { get; set; } = true;
    public string DefaultPLCIp { get; set; } = "192.168.1.1";
    public int DefaultPLCPort { get; set; } = 102;
    public int S7Rack { get; set; } = 0;
    public int S7Slot { get; set; } = 1;
    public string OpcUaEndpoint { get; set; }
    
    // ===== 视觉类 =====
    public string CameraBrand { get; set; } = "海康";
    public string ModelType { get; set; } = "Classification";
    public string ModelPath { get; set; }
    public bool EnablePipeline { get; set; } = true;
    
    // ===== 系统类 =====
    public List<string> SelectedModules { get; set; } = new() { "UserManagement", "RolePermission" };
    public bool EnableLoginWindow { get; set; } = true;
    public bool EnableCrossPlatform { get; set; }
    public bool ForcePasswordChange { get; set; }
}
```

---

## 12. 数据库 Schema

### 12.1 生成项目 SQLite 表结构

#### 12.1.1 配置表 (`app_config`)

```sql
CREATE TABLE IF NOT EXISTS app_config (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    category    TEXT NOT NULL,           -- 配置分类，如 "App", "Driver", "Camera"
    key         TEXT NOT NULL,           -- 配置键
    value       TEXT,                    -- 配置值（JSON 格式）
    value_type  TEXT NOT NULL DEFAULT 'string', -- "string", "int", "bool", "json"
    description TEXT,                    -- 描述
    updated_at  TEXT NOT NULL DEFAULT (datetime('now')),
    UNIQUE(category, key)
);

-- 种子数据
INSERT INTO app_config (category, key, value, value_type, description) VALUES
('App', 'Language', '"zh-CN"', 'string', '界面语言'),
('App', 'Theme', '"Light"', 'string', '主题'),
('App', 'LogLevel', '"Information"', 'string', '日志级别');
```

#### 12.1.2 用户表 (`sys_user`) — 系统类

```sql
CREATE TABLE IF NOT EXISTS sys_user (
    username            TEXT PRIMARY KEY,
    display_name        TEXT NOT NULL,
    password_hash       TEXT NOT NULL,          -- BCrypt 哈希
    role                INTEGER NOT NULL DEFAULT 1, -- UserRole 枚举值
    is_active           INTEGER NOT NULL DEFAULT 1,
    must_change_password INTEGER NOT NULL DEFAULT 0,
    last_login_at       TEXT,
    created_at          TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at          TEXT NOT NULL DEFAULT (datetime('now'))
);

-- 种子用户（密码为 BCrypt 哈希后的值）
-- admin/admin123, engineer/engineer123, operator/operator123
INSERT INTO sys_user (username, display_name, password_hash, role) VALUES
('admin', '管理员', '$2a$11$...', 7),
('engineer', '工程师', '$2a$11$...', 3),
('operator', '操作员', '$2a$11$...', 1);
```

#### 12.1.3 操作日志表 (`sys_audit_log`) — 系统类

```sql
CREATE TABLE IF NOT EXISTS sys_audit_log (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    username    TEXT NOT NULL,
    action      TEXT NOT NULL,           -- "Login", "Logout", "CreateUser", etc.
    target      TEXT,                    -- 操作对象
    detail      TEXT,                    -- 详细信息（JSON）
    ip_address  TEXT,
    created_at  TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX idx_audit_log_username ON sys_audit_log(username);
CREATE INDEX idx_audit_log_created_at ON sys_audit_log(created_at);
```

#### 12.1.4 插件状态表 (`plugin_state`) — 诊断

```sql
CREATE TABLE IF NOT EXISTS plugin_state (
    plugin_id       TEXT PRIMARY KEY,
    plugin_name     TEXT NOT NULL,
    version         TEXT NOT NULL,
    state           TEXT NOT NULL DEFAULT 'Offline', -- "Normal", "Faulted", "Offline"
    last_error      TEXT,
    loaded_at       TEXT,
    faulted_at      TEXT,
    updated_at      TEXT NOT NULL DEFAULT (datetime('now'))
);
```

### 12.2 脚手架自身历史记录格式

文件路径：`%APPDATA%/ScaffoldX/history.json`

```json
{
  "projects": [
    {
      "projectName": "MyFactory",
      "projectType": "Collection",
      "outputPath": "C:\\Projects",
      "targetFramework": ".NET 8",
      "uiFramework": "WPF",
      "createdAt": "2026-04-28T00:40:00",
      "configJson": "{\"ProjectName\":\"MyFactory\",...}"
    }
  ]
}
```

---

## 13. 非功能需求

### 13.1 性能指标

| 指标 | 目标值 | 测量方式 |
|------|--------|----------|
| 脚手架启动时间 | ≤ 2 秒 | 从进程启动到主窗口显示 |
| 项目生成时间 | ≤ 10 秒（不含测试项目） | 内部 `Stopwatch` 计时 |
| 生成程序冷启动 | ≤ 3 秒（含扫描插件） | 从进程启动到主窗口显示 |
| 插件加载延迟 | 不影响主 UI 线程 | 异步加载 + `Dispatcher` 切换 |
| 生成程序内存占用（空载） | ≤ 150 MB | 任务管理器 |
| 脚手架内存占用 | ≤ 100 MB | 任务管理器 |
| 单个模板渲染时间 | ≤ 50 ms | 内部计时 |

### 13.2 可靠性

| 场景 | 处理策略 |
|------|----------|
| 生成代码编译零错误 | 模板经过 CI 验证，每个项目类型组合至少一次完整编译测试 |
| 插件加载失败 | 记录日志并跳过，不影响主程序 |
| 配置文件损坏 | 回退默认配置，启动不崩溃 |
| SQLite 数据库损坏 | 自动备份损坏文件，重建空数据库 |
| 模板文件缺失 | 报错并中止生成，提示缺失文件路径 |
| 目标路径磁盘满 | 捕获 `IOException`，提示用户清理空间 |
| 非法字符输入 | 实时验证，禁用下一步按钮 |

### 13.3 安全性

| 措施 | 实现 |
|------|------|
| 密码存储 | BCrypt 哈希，`BCrypt.Net` 库 |
| 连接字符串加密 | `IConfigService` 支持 AES 加密存储，启动时解密 |
| 远程数据传输 | 可选 TLS（预留接口） |
| 插件 DLL 签名校验 | 预留 `IPluginSignatureVerifier` 接口 |

### 13.4 兼容性

| 维度 | 范围 |
|------|------|
| 生成项目 OS (WPF) | Windows 7 SP1+ / Windows 10+ |
| 生成项目 OS (Avalonia) | Windows 10+ / Linux (Ubuntu 20.04+) |
| 开发工具 | Visual Studio 2022 17.0+ / JetBrains Rider 2023.0+ |
| .NET SDK | .NET 6.0.x / .NET 8.0.x |
| 发布格式 | `win-x64` 自包含单文件 / `linux-x64` 自包含 |

---

## 14. 界面与交互规范

### 14.1 脚手架 UI 设计规范

#### 14.1.1 配色方案

| 用途 | 色值 | 说明 |
|------|------|------|
| 主色调 | `#1565C0` | 工业蓝，用于按钮、选中态、标题 |
| 主色调 Light | `#1E88E5` | Hover 状态 |
| 主色调 Dark | `#0D47A1` | Pressed 状态 |
| 表面色 | `#ECEFF1` | 页面背景 |
| 卡片色 | `#FFFFFF` | 卡片/面板背景 |
| 文本 Primary | `#212121` | 主文本 |
| 文本 Secondary | `#757575` | 辅助文本 |
| 错误色 | `#D32F2F` | 错误提示、验证失败 |
| 成功色 | `#388E3C` | 生成成功 |

#### 14.1.2 字体规范

| 用途 | 字体 | 大小 | 粗细 |
|------|------|------|------|
| 标题 H1 | Microsoft YaHei UI | 24px | Bold |
| 标题 H2 | Microsoft YaHei UI | 18px | SemiBold |
| 正文 | Microsoft YaHei UI / Segoe UI | 14px | Regular |
| 辅助文本 | Microsoft YaHei UI | 12px | Regular |
| 按钮文本 | Microsoft YaHei UI | 14px | SemiBold |
| 代码/路径 | Consolas | 13px | Regular |

#### 14.1.3 布局规范

| 元素 | 规范 |
|------|------|
| 页面内边距 | 24px |
| 卡片间距 | 16px |
| 表单项间距 | 12px |
| 按钮高度 | 36px |
| 输入框高度 | 36px |
| 圆角 | 4px（卡片）、2px（按钮） |
| 阴影 | `DropShadowEffect` BlurRadius=10, ShadowDepth=2 |

### 14.2 生成程序 UI 预置规范

#### 14.2.1 主窗口布局

```
┌─────────────────────────────────────────────────────────┐
│  标题栏：{ProjectName} - {当前页面名称}                   │
├──────────┬──────────────────────────────────────────────┤
│          │                                              │
│  侧边栏  │              内容区域                          │
│  200px   │          (Prism Region)                      │
│          │                                              │
│  🏠 首页  │                                              │
│  📊 采集  │                                              │
│  📷 视觉  │                                              │
│  ⚙ 设置  │                                              │
│  🔧 诊断  │                                              │
│          │                                              │
├──────────┴──────────────────────────────────────────────┤
│  状态栏：连接状态 | 当前用户 | 时间 | 插件状态指示灯       │
└─────────────────────────────────────────────────────────┘
```

#### 14.2.2 侧边栏样式

- 宽度：200px（展开）/ 48px（折叠）
- 背景色：`#263238`（深蓝灰）
- 菜单项：白色图标 + 白色文字，Hover 时背景 `#37474F`
- 选中项：左侧 3px 蓝色竖线指示，背景 `#37474F`
- 底部：用户头像 + 用户名，点击弹出菜单（修改密码、退出登录）

---

## 15. 错误处理与边界条件

### 15.1 脚手架错误处理

| 错误场景 | 处理方式 | UI 反馈 |
|----------|----------|---------|
| 项目名称已存在 | 验证失败，阻止生成 | 红色提示："目标路径已存在同名文件夹" |
| 存储路径不可写 | 验证失败，阻止生成 | 红色提示："存储路径没有写入权限" |
| 模板文件缺失 | 中止生成，记录日志 | 弹窗："模板文件缺失：{path}，请检查安装完整性" |
| 磁盘空间不足 | 捕获 IOException | 弹窗："磁盘空间不足，请清理后重试" |
| 生成过程中断电 | 无回滚机制 | 下次启动时目标文件夹可能有残留，提示用户手动清理 |

### 15.2 生成程序错误处理

| 错误场景 | 处理方式 |
|----------|----------|
| 插件 DLL 不存在 | 跳过，日志 Warning |
| 插件接口版本不匹配 | 跳过，日志 Error，记录期望版本 vs 实际版本 |
| 插件 InitializeAsync 抛异常 | 状态机→Faulted，日志 Error，继续加载其他插件 |
| 插件运行时未捕获异常 | 状态机→Faulted，日志 Error，发布 PluginStateChangedEvent |
| SQLite 数据库文件损坏 | 备份损坏文件为 `.corrupt.{timestamp}`，重建空库 |
| 配置键不存在 | 返回默认值，不抛异常 |
| 日志文件写入失败 | 不影响主程序，Console.Error 输出 |
| 所有驱动插件加载失败 | 主程序正常启动，首页显示警告："无可用驱动" |
| 登录密码错误 | 提示"用户名或密码错误"（不具体指出哪个错） |
| 连续登录失败 5 次 | 锁定账户 5 分钟 |

### 15.3 边界条件清单

| 边界条件 | 预期行为 |
|----------|----------|
| 项目名称刚好 50 字符 | 允许 |
| 项目名称 51 字符 | 阻止，提示 |
| 项目名称以数字开头 | 阻止，提示 |
| 项目名称包含中文 | 阻止，提示 |
| 存储路径为网络驱动器 `\\server\share` | 允许（如可写） |
| 存储路径为根目录 `C:\` | 阻止，提示"请选择子目录" |
| 插件目录为空 | 正常启动，日志 Info "无插件加载" |
| 插件目录有 100+ DLL | 正常加载，但日志 Warning "插件数量过多" |
| .resx 资源键重复 | 编译时由 MSBuild 报错 |
| 模拟驱动 Tag 数量 0 | 正常运行，不产生数据事件 |
| 模拟驱动 Tag 数量 10000+ | 正常运行，注意内存 |

---

## 16. 命名规范与代码风格

### 16.1 命名规范

| 元素 | 规范 | 示例 |
|------|------|------|
| 解决方案 | `{ProjectName}` | `MyFactory.sln` |
| 项目（csproj） | `{ProjectName}.{Layer}` | `MyFactory.Core.csproj` |
| 命名空间 | `{NamespacePrefix}.{Layer}` | `MyFactory.Core.Services` |
| 接口 | `I` 前缀 | `IPlugin`, `IDriver` |
| 抽象类 | `Base` 后缀 | `PluginBase`, `InferenceEngineBase` |
| 类名 | PascalCase | `PluginScanner`, `ConfigService` |
| 方法名 | PascalCase | `InitializeAsync`, `ReadBatchAsync` |
| 属性名 | PascalCase | `IsConnected`, `DriverId` |
| 私有字段 | `_` 前缀 + camelCase | `_stateMachine`, `_isConnected` |
| 局部变量 | camelCase | `pluginType`, `dllPath` |
| 常量 | PascalCase | `PluginInterfaceVersions.Current` |
| 枚举值 | PascalCase | `PluginState.Faulted` |
| 事件 | PascalCase + `Event` 后缀 | `DataUpdatedEvent` |
| 异步方法 | `Async` 后缀 | `InitializeAsync` |
| 模板文件 | `{Type}_{Layer}_{Name}.stpl` | `Common_Core_IPlugin.cs.stpl` |
| 资源键 | PascalCase 或 `Category.Key` | `Menu.Home`, `Button.Save` |

### 16.2 代码风格

```csharp
// 文件编码：UTF-8 with BOM（.cs 文件）
// 缩进：4 空格
// 花括号：Allman 风格（换行）
// using 排序：System > 第三方 > 项目内

// 示例：
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Prism.Events;
using Serilog;

using MyFactory.Core.Abstractions;
using MyFactory.Core.Models;

namespace MyFactory.Core.Services
{
    /// <summary>
    /// 配置服务实现，基于 SQLite。
    /// </summary>
    public class ConfigService : IConfigService
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public ConfigService(string connectionString, ILogger logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T> GetValueAsync<T>(string category, string key, T defaultValue = default)
        {
            // 实现...
        }
    }
}
```

### 16.3 生成代码中的 TODO 标记规范

所有需要开发者填充的位置使用统一标记：

```csharp
// TODO: [ScaffoldX] 请在此处实现 S7 协议连接逻辑
// 参考：https://github.com/your-s7-library
protected override async Task InitializePluginAsync()
{
    // TODO: [ScaffoldX] 初始化 S7 连接
    throw new NotImplementedException("请实现 S7 驱动初始化逻辑");
}
```

标记格式：`// TODO: [ScaffoldX] {描述}`

---

## 17. 验收标准

### 17.1 脚手架工具验收

- [ ] 启动时间 ≤ 2 秒
- [ ] 四步向导可正常走完，无崩溃
- [ ] 所有输入验证生效（项目名称、路径、命名空间）
- [ ] 历史记录正确保存和读取
- [ ] 步骤间切换保留已填数据
- [ ] 步骤三动态区块根据项目类型正确切换
- [ ] Avalonia + .NET 6 组合正确拦截
- [ ] 生成进度条正确显示
- [ ] 生成完成后可打开文件夹

### 17.2 生成项目验收 — 通用

- [ ] `.sln` 文件在 VS 2022 中零错误编译
- [ ] 零警告编译（除 `CS8618` nullable 警告外）
- [ ] 启动后主窗口正常显示
- [ ] 侧边栏菜单正确渲染
- [ ] 中英文切换无遗漏（所有 UI 文本从 .resx 引用）
- [ ] 日志文件正确输出到 `Logs/` 目录
- [ ] SQLite 数据库自动创建
- [ ] 诊断面板可查看插件状态
- [ ] `publish.bat` 可正常执行发布

### 17.3 生成项目验收 — 采集类

- [ ] 模拟驱动正常加载，状态为 Normal
- [ ] 模拟数据正常刷新（100ms 周期）
- [ ] 勾选的协议驱动插件项目均生成
- [ ] 未勾选的协议驱动项目未生成
- [ ] `IDriver` 接口定义正确
- [ ] `Tag` 模型包含所有必要字段
- [ ] `ConnectionParams` 包含所有协议参数

### 17.4 生成项目验收 — 视觉类

- [ ] 模拟相机正常加载，可出图
- [ ] `ICameraService` 接口定义正确
- [ ] 推理引擎基类可正常编译
- [ ] 管道引擎可正常编译
- [ ] `ConsecutiveNgAlarmStep` 示例步骤存在

### 17.5 生成项目验收 — 系统类

- [ ] 种子用户可正常登录（admin/admin123）
- [ ] 密码 BCrypt 哈希存储（数据库中非明文）
- [ ] 菜单按角色正确过滤
- [ ] 用户管理 CRUD 功能正常（编译通过，接口完整）
- [ ] 主题切换功能正常（编译通过，接口完整）

---

## 18. 里程碑与版本规划

| 版本 | 范围 | 预估工期 | 交付物 |
|------|------|----------|--------|
| v0.1 MVP | 脚手架向导 UI + 模板引擎 + 采集类单驱动模板生成 | 3 周 | 可生成含模拟驱动的采集类项目 |
| v0.2 | 采集类全部驱动 + 基础诊断模块 | 2 周 | 4 种协议驱动插件模板 |
| v0.3 | 视觉类全部模板（相机 + 推理 + 管道） | 2 周 | 视觉检测类项目完整生成 |
| v0.4 | 系统类全部模板（用户 + 权限 + 主题） | 2 周 | 系统定制类项目完整生成 |
| v0.5 | Avalonia 支持 + 多语言 + 发布脚本 | 2 周 | 双 UI 框架支持 |
| v1.0 | 测试 + 打磨 + 文档 | 2 周 | 生产就绪版本 |

---

## 19. 附录

### 19.1 变更记录

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| 0.1 | 2026-04-28 | 初始 PRD 创建 |
| 1.0 | 2026-04-28 | 完整版，覆盖所有功能模块和实现细节 |

### 19.2 参考资料

- Prism Library: https://prismlibrary.com/
- Scriban Template Engine: https://github.com/scriban/scriban
- MaterialDesign in XAML: https://materialdesigninxaml.net/
- Serilog: https://serilog.net/
- ONNX Runtime: https://onnxruntime.ai/
- Avalonia UI: https://avaloniaui.net/

### 19.3 开放问题

| 编号 | 问题 | 状态 | 决策 |
|------|------|------|------|
| Q1 | 是否需要支持 .NET 9？ | 待定 | V1 暂不支持，后续版本扩展 |
| Q2 | 是否需要模板自定义编辑器？ | 待定 | V1 暂不支持，高级用户可直接编辑 .stpl 文件 |
| Q3 | 是否需要插件签名验证？ | 待定 | V1 预留接口，不强制实现 |
| Q4 | 远程配置中心具体实现？ | 待定 | V1 仅本地 SQLite，远程为预留 |

---

> **文档结束**  
> 本文档为 ScaffoldX 的完整可执行 PRD，任何标注 `[AGENT-SPEC]` 的段落包含可直接使用的代码实现。  
> 如有疑问，请在对应章节的"开放问题"中记录。
