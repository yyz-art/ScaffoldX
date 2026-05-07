# PRD: 生成程序界面模板升级 — 导航栏可选、六主题系统、中英翻译

> **状态**: Draft
> **创建日期**: 2026-05-07
> **标签**: `ready-for-agent`

---

## Problem Statement

当前 ScaffoldX 生成的程序模板存在以下问题：

1. **导航布局固定**：MainWindow 硬编码为左侧边栏布局，无法选择顶部导航栏。工业采集类项目中，部分场景更适合顶部导航（如宽屏监控、多窗口并排）。
2. **主题系统简陋**：IThemeService 接口仅定义了 Light/Dark 两种主题，App.xaml 硬编码 MaterialDesign Light+Blue+Amber。缺乏工业场景常用的深色主题、高对比度主题等。
3. **无国际化基础设施**：HomePageView、StatusBarView、SidebarView 等模板中所有字符串硬编码为中文，PRD v2 §5.1 规划的 .resx 资源文件从未实现。
4. **生成程序基本逻辑不完整**：Shell 模块缺少 Prism Module 初始化、菜单动态构建、主题切换联动等基础功能的正确实现。

## Solution

升级 ScaffoldX 的模板系统，使生成的程序模板支持：
- 可选的导航栏布局（左侧 / 顶部）
- 内置六套工业级主题，开箱即用
- 中英双语资源文件，支持运行时切换
- Shell 模块的正确初始化逻辑

## User Stories

1. As an 工业上位机开发者, I want 在向导步骤三中选择导航栏位置（左侧/顶部）, so that 我可以根据项目场景选择最合适的布局
2. As an 工业上位机开发者, I want 生成的程序默认包含六套主题（Light、Dark、Industrial Dark、High Contrast、Blue Steel、Green Terminal）, so that 我可以直接在运行时切换主题而无需额外开发
3. As an 工业上位机开发者, I want 生成的程序包含中英文资源文件, so that 我的程序支持中英双语切换
4. As an 工业上位机开发者, I want 选择顶部导航时生成 TopNavView 而非 SidebarView, so that 程序布局符合我的使用场景
5. As an 工业上位机开发者, I want 生成的 SidebarView/TopNavView 菜单项基于已启用模块动态构建, so that 菜单与实际功能一致
6. As an 工业上位机开发者, I want 主题切换时 StatusBar、Sidebar、HomePage 等所有 Shell 组件联动更新, so that 用户体验一致
7. As an 工业上位机开发者, I want 生成的 App.xaml 根据选中主题正确初始化 MaterialDesign BundledTheme, so that 程序启动时显示正确的主题
8. As an 工业上位机开发者, I want 语言切换功能通过 .resx 资源文件实现, so that 我可以轻松添加更多语言支持
9. As an 工业上位机开发者, I want 生成的 HomePageView 使用资源键而非硬编码中文, so that 界面文本随语言切换自动更新
10. As an 工业上位机开发者, I want 生成的 StatusBarView 使用资源键显示连接状态等文本, so that 状态栏支持多语言
11. As an 工业上位机开发者, I want 主题和语言设置持久化到本地配置, so that 下次启动时保持用户偏好
12. As an 工业上位机开发者, I want 选择左侧导航时 SidebarView 支持折叠/展开, so that 在小屏幕上节省空间
13. As an 工业上位机开发者, I want 顶部导航模式下 StatusBar 仍然显示在底部, so that 保持工业软件的状态监控习惯
14. As an 工业上位机开发者, I want 六套主题的配色方案经过工业场景验证, so that 长时间使用不疲劳且关键信息醒目
15. As an 工业上位机开发者, I want IThemeService.GetAvailableThemes() 返回六套主题的完整信息, so that 主题选择器 UI 可以正确渲染
16. As an 工业上位机开发者, I want 生成的 ShellModule.cs 正确注册所有 Shell 视图到 Prism DI 容器, so that 导航和区域管理正常工作
17. As an 工业上位机开发者, I want 向导步骤三在采集类配置面板中增加"导航栏位置"选项, so that 我可以在配置阶段做出选择
18. As an 工业上位机开发者, I want ProjectConfig 新增 NavigationStyle 字段, so that 模板系统可以根据配置选择不同的 Shell 模板
19. As an 工业上位机开发者, I want 生成的程序在 StatusBar 中提供语言切换下拉框, so that 用户可以在运行时切换中英文
20. As an 工业上位机开发者, I want 生成的程序在 StatusBar 或菜单中提供主题切换入口, so that 用户可以方便地切换主题

## Implementation Decisions

### 1. ProjectConfig 新增字段

在 `Core.Models.ProjectConfig` 中新增：
- `NavigationStyle` (string): "LeftSidebar" | "TopNav"，默认 "LeftSidebar"
- `DefaultTheme` (string): 默认主题 ID，默认 "IndustrialDark"
- `DefaultLanguage` (string): 默认语言 "zh-CN" | "en-US"，默认 "zh-CN"

### 2. 向导步骤三增加配置项

Step3ViewModel 新增：
- `NavigationStyle` 属性（ComboBox 绑定）
- 采集类配置面板中显示导航栏位置选项

### 3. 六套主题定义

| 主题 ID | 名称 | BaseTheme | PrimaryColor | SecondaryColor | 主色调 | 适用场景 |
|---------|------|-----------|-------------|----------------|--------|---------|
| DeepSeaBlue | 深海灰蓝 | Dark | BlueGrey | LightBlue | #4A5F73 | 长时间监控（推荐） |
| MossGreen | 灰调墨绿 | Dark | Teal | Green | #3D4F46 | 传统工控风格 |
| CharcoalBlack | 高级炭黑 | Dark | Grey | BlueGrey | #1A1A1A | 强光环境/远距离 |
| BlueSteel | 蓝钢 | Dark | Indigo | LightBlue | #4A6A8A | 专业风格 |
| AmberIndustrial | 琥珀暖调 | Light | Orange | Amber | #D4A017 | 日间办公 |
| NeutralSteel | 中性钢灰 | Light | Grey | BlueGrey | #5C636A | 通用中性 |

每个主题通过 MaterialDesign BundledTheme 的 BaseTheme + PrimaryColor + SecondaryColor 组合实现。

### 4. 模板拆分策略

**Shell 模板变体：**
- `Common/Shell/SidebarView.xaml.stpl` — 左侧导航版（现有，优化）
- `Common/Shell/TopNavView.xaml.stpl` — 顶部导航版（新增）
- `Common/Shell/SidebarViewModel.cs.stpl` — 复用，增加折叠逻辑
- `Common/Shell/TopNavViewModel.cs.stpl` — 顶部导航 ViewModel（新增）

**TemplateRegistry 筛选逻辑：**
- 当 `NavigationStyle == "LeftSidebar"` 时选择 SidebarView 模板
- 当 `NavigationStyle == "TopNav"` 时选择 TopNavView 模板
- 两者共享 StatusBar、HomePage 模板

### 5. 国际化实现方案

- 在生成的 `{ProjectName}.App` 项目中创建 `Resources/Strings.zh-CN.resx` 和 `Resources/Strings.en-US.resx`
- 使用 `System.Resources.ResourceManager` 加载资源
- 创建 `ILocalizationService` 接口和 `LocalizationService` 实现，管理当前语言和资源查询
- Shell 组件通过绑定到 LocalizationService 的属性实现动态语言切换
- 语言选择持久化到 IConfigService

### 6. IThemeService 扩展

- `ThemeInfo` 新加属性：`BaseTheme` (string), `PrimaryColor` (string), `SecondaryColor` (string)
- `SetThemeAsync` 实现：动态切换 MaterialDesign BundledTheme 的 BaseTheme/PrimaryColor/SecondaryColor
- 主题选择持久化到 IConfigService

### 7. Shell 模块正确初始化

生成的 `ShellModule.cs` 必须：
- 实现 `IModule` 接口
- 在 `RegisterTypes` 中注册 Shell 相关视图
- 在 `OnInitialized` 中设置默认导航到 HomePageView
- 根据 NavigationStyle 注册对应的导航视图（SidebarView 或 TopNavView）

### 8. App.xaml 模板改造

- 读取 DefaultTheme 配置初始化 BundledTheme
- 注册 IThemeService、ILocalizationService 到 DI 容器
- 根据 NavigationStyle 条件注册 Shell 视图

### 9. StatusBar 增强

- 新增语言切换 ComboBox（中/英）
- 新增主题切换按钮/下拉
- 连接状态文本使用资源键

## Testing Decisions

**好测试的标准：** 只测试外部可观察行为（生成的文件内容、模板选择逻辑、变量上下文），不测试模板内部渲染细节。

**需要测试的模块：**

1. **TemplateRegistry 模板选择** — 验证 NavigationStyle 配置正确筛选 SidebarView/TopNavView 模板
2. **VariableResolver 变量上下文** — 验证 NavigationStyle、DefaultTheme、DefaultLanguage 字段正确传入模板
3. **ProjectConfig 新字段** — 验证序列化/反序列化、默认值
4. **Step3ViewModel 导航选项** — 验证采集类面板显示导航栏位置选项
5. **FullGenerationFlow 集成测试** — 验证选择 TopNav 时生成 TopNavView 而非 SidebarView
6. **App 层模板渲染** — 验证 ScribanTemplateEngine 正确渲染带 .resx 资源键的模板

**现有测试参考：**
- `TemplateRegistryTests` — 模板加载和分类过滤的测试模式
- `VariableResolverTests` — 变量上下文构建的测试模式
- `FullGenerationFlowTests` — 集成测试的测试模式
- `ValidationServiceTests` — 验证逻辑的测试模式

## Out of Scope

- Avalonia 跨平台适配（本次仅 WPF）
- 数据库 Schema 变更（主题/语言配置使用 IConfigService 本地存储）
- 自定义主题编辑器（仅预置六套）
- RTL 语言支持
- 动态主题资源导入（仅预置主题）
- Vision 类和 System 类的导航布局定制（后续迭代）
- 插件系统改造（保持现有 IPlugin 接口不变）

## Further Notes

### 实施顺序建议

1. **Phase 1: 基础设施** — ProjectConfig 新字段、向导 UI、模板选择逻辑
2. **Phase 2: 主题系统** — 六套主题模板、IThemeService 实现、App.xaml 改造
3. **Phase 3: 国际化** — .resx 资源文件、ILocalizationService、Shell 组件改造
4. **Phase 4: 导航变体** — TopNavView 模板、ShellModule 正确初始化
5. **Phase 5: 测试与打磨** — 单元测试、集成测试、模板渲染验证

### 与 PRD v2 的关系

本 PRD 实现了 PRD v2 中以下未完成的规范：
- §5.1 解决方案结构中的 `.resx` 资源文件
- §5.1 中 `Bootstrapper.cs` / `ShellModule.cs` 的正确初始化
- §3 架构决策矩阵中的"强制中英双语"和"经典侧边栏导航"
- §9 系统定制类中的主题切换模块完整实现
