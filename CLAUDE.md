# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ScaffoldX** is a WPF desktop application (.NET 10) that generates industrial automation project scaffolds through a visual wizard. It targets industrial HMI/SCADA developers who need to quickly bootstrap WPF/Avalonia projects with pre-configured drivers, vision modules, and system components.

**Key characteristics:**
- Offline-first design for industrial control network isolation
- Scriban template engine with `.stpl` template files
- Prism MVVM framework for modular WPF architecture
- Generates complete Visual Studio solutions ready to compile
- Integrated annotation tool with TorchSharp inference and SAM 3 segmentation

## Build & Development Commands

```bash
# Build the entire solution
dotnet build ScaffoldX.slnx

# Build specific projects
dotnet build src/ScaffoldX.App/ScaffoldX.App.csproj
dotnet build src/ScaffoldX.Core/ScaffoldX.Core.csproj
dotnet build src/ScaffoldX.Templates/ScaffoldX.Templates.csproj

# Run the application
dotnet run --project src/ScaffoldX.App/ScaffoldX.App.csproj

# Run all unit tests
dotnet test tests/ScaffoldX.Core.Tests/

# Run a single test by name
dotnet test tests/ScaffoldX.Core.Tests/ --filter "FullyQualifiedName~TemplateRegistryTests"

# Run tests with detailed output
dotnet test tests/ScaffoldX.Core.Tests/ --verbosity normal

# Restore NuGet packages
dotnet restore
```

## Architecture

### Solution Structure

```
ScaffoldX.slnx
├── src/
│   ├── ScaffoldX.App/          # WPF application (UI layer)
│   ├── ScaffoldX.Core/         # Business logic, template processing, TorchSharp vision inference
│   └── ScaffoldX.Templates/    # Embedded .stpl template resources
└── tests/
    ├── ScaffoldX.App.Tests/        # App layer tests (~181 test methods)
    └── ScaffoldX.Core.Tests/       # Core layer tests (~63 test methods)
```

### Dependency Flow

```
ScaffoldX.App → ScaffoldX.Core → ScaffoldX.Templates
```

- **ScaffoldX.App**: WPF UI, ViewModels, Services, dependency injection via Prism.Unity
- **ScaffoldX.Core**: TemplateRegistry, VariableResolver, PostProcessor, FileTreeBuilder, TorchSharp inference, SAM 3 segmentation
- **ScaffoldX.Templates**: Embedded `.stpl` files (Scriban templates) as assembly resources

### Key Components

**Template System:**
- `.stpl` files in `ScaffoldX.Templates/` are embedded resources loaded at runtime
- Template directives: `##OUTPUT:` (output path), `##REQUIRED:` (conditional generation)
- Variables use `PascalCase` naming convention (e.g., `{{ProjectName}}`, `{{EnableVision}}`)
- PostProcessor handles line endings, XML entity restoration, trailing whitespace

**Project Generation Flow (PRD §10.4):**
1. Validate configuration (ProjectName, OutputPath) via `IValidationService`
2. Map App's `ProjectConfig` to Core's `ProjectConfig` (bridge two-layer models)
3. Build variable context via `VariableResolver.BuildVariableContext()` (PascalCase keys)
4. Load templates from assembly via `TemplateRegistry.LoadFromAssemblyAsync()`
5. Select templates via `TemplateRegistry.GetTemplatesForConfig()` (conditional filtering)
6. Render template content and output path via `ITemplateEngine.Render()`
7. Post-process via `PostProcessor.Process()` (line endings, XML entities, trailing whitespace)
8. Write files and record history via `IHistoryService`

**System Modules (PRD-aligned):**
- `EnableUserManagement` — User management (IUserService, User model, SeedData)
- `EnableRolePermission` — Role/permission management (IRoleService, RoleInfo)
- `EnableSystemLog` — Audit logging (IAuditLogService, AuditLogEntry)
- `EnableThemeSwitcher` — Theme switching (IThemeService, ThemeInfo)

**Vision Module (TorchSharp + SAM 3):**
- `AnnotationService` / `AnnotationView` — annotation tool with bounding box, polygon, OBB, polyline, circle, and segmentation
- `YoloTrainingService` — Python/Ultralytics training script generation and execution
- `InferenceEngineBase` — TorchSharp inference base class with `BitmapToTensor` (CHW format, [0,1] normalization)
- `Sam3Segmentor` (`ISam3SegmentationEngine`) — SAM 3 segmentation engine, loads TorchScript models (encoder.pt, text_encoder.pt, decoder.pt)
- `Sam3Tokenizer` — BPE tokenizer for SAM 3 text encoder (vocab.json + merges.txt)
- `MaskToPolygonConverter` — binary mask to polygon contour (Marching Squares + Douglas-Peucker)
- `ImageEmbedding` — disposable cached image embedding tensor for interactive segmentation
- `AutoLabelingService` (`IAutoLabelingService`) — unified service for detection and SAM 3 segmentation modes
- `AnnotationModels.cs` — Core data models: `BoundingBoxAnnotation`, `PolygonAnnotation`, `OrientedBoundingBoxAnnotation`, `SegmentationAnnotation`, `Sam3Point`, `Sam3PromptMode`

**Annotation & Training Platform Features:**
- Bounding box, polygon, oriented bounding box (OBB), polyline, circle, and segmentation annotation
- SAM 3 auto-labeling: text prompt (batch), point prompt (interactive left=positive/right=negative), reference image
- Auto-labeling with TorchSharp models (detection mode)
- Annotation interpolation between keyframes
- YOLO, COCO, VOC, DOTA, MOT format export
- Export report generation
- Import annotations from YOLO format
- Recent files tracking
- Keyboard shortcuts: Delete, Ctrl+Z/Y (undo/redo), arrow keys (nudge), 1-9 (class select), Space (finish polygon), B (bbox mode), P (polygon mode), O (OBB mode)
- Zoom/pan with mouse wheel and middle button
- Undo/redo for all annotation types via generic `UndoRedoManager`
- Annotation statistics and count tracking
- Video frame extraction from video files
- Model zoo with pretrained YOLO models for auto-labeling
- UI accessibility support

**Wizard Steps (MVVM):**
- Step 1: Project type selection (Collection/Vision/System)
- Step 2: Basic info (name, path, namespace, UI framework)
- Step 3: Specific configuration (drivers, vision, modules)
- Step 4: Preview file tree and generate

### Annotation ViewModel Architecture (9 Handlers)

The AnnotationViewModel delegates to 9 specialized handlers:
- AutoLabelingCommandHandler — model load/unload, auto-detect (detection mode)
- Sam3LabelingCommandHandler — SAM 3 text/point/reference segmentation, mask preview with cancellation
- ImageNavigationHandler — previous/next, load/save
- ClassManagementHandler — add/remove/select class
- PolygonDrawingHandler — polygon mode, add/finish/cancel
- ObbDrawingHandler — OBB mode, drag/rotate/finish
- UndoRedoHandler — push/undo/redo snapshots
- ExportCommandHandler — YOLO/COCO/VOC/DOTA/MOT export, import
- ReviewCommandHandler — review summary, goto unannotated

### Design Patterns

**MVVM with Prism:**
- ViewModels inherit `BindableBase`
- Commands use `DelegateCommand` with `ObservesProperty` — always pass `Func<bool>` (lambda), never a `bool` property directly
- Navigation via `RegisterForNavigation<T>()`
- Services registered as singletons in DI container

**Template Registry Pattern:**
- `TemplateRegistry` loads all `.stpl` files from assembly resources
- `GetTemplatesForConfig()` filters templates based on `ProjectConfig` flags
- Category-based filtering: Collection, Vision, System, Common

**Validation:**
- `IValidationService` validates project names, paths, IP addresses, ports
- Regex pattern for project names: `^[A-Za-z][A-Za-z0-9_]{0,49}$`

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Prism.Unity | 8.1.97 | MVVM framework and DI container |
| MaterialDesignThemes | 5.1.0 | UI theme and controls |
| Scriban | 5.10.0 | Template engine for code generation |
| Serilog | 3.1.1 | Structured logging |
| FluentValidation | 11.9.0 | Input validation |
| Ookii.Dialogs.Wpf | 4.0.0 | Native folder browser dialogs |
| System.Drawing.Common | 8.0.0 | Bitmap processing for vision inference |
| TorchSharp-cuda-windows | 0.105.0 | TorchSharp bindings for libtorch (GPU inference) |
| xUnit | 2.6.2 | Test framework |
| FluentAssertions | 6.12.0 | Test assertions |
| Moq | 4.20.69 | Mocking framework |

## Template Conventions

**File naming:** `Category/Name.cs.stpl` (e.g., `Collection/Core/IDriver.cs.stpl`)

**Template directives (must be first lines):**
```
##OUTPUT: src/{{ProjectName}}.Core/Abstractions/IDriver.cs
##REQUIRED: false
```

- `##OUTPUT:` — Scriban template for the output file path (relative to project root)
- `##REQUIRED: true` (default if omitted) — always included in generated project
- `##REQUIRED: false` — only included when the corresponding category flag is enabled in `ProjectConfig`

**Variable naming:** Always use `PascalCase` in templates (matching PRD §10.4):
- `{{ProjectName}}` — Project name in PascalCase
- `{{NamespacePrefix}}` — Namespace prefix
- `{{TargetFramework}}` — Target framework (e.g., net10.0-windows)
- `{{UIFramework}}` — WPF or Avalonia
- `{{XamlExt}}` — xaml or axaml based on UI framework
- `{{EnableVision}}`, `{{EnableSiemensS7}}`, etc. — boolean flags for conditional generation
- `{{ScaffoldXVersion}}` — ScaffoldX version string
- `{{GeneratedAt}}` — Generation timestamp

## SAM 3 Model Directory Convention

SAM 3 models are loaded from a directory containing three TorchScript files:
```
model_dir/
├── encoder.pt        # Image encoder (required)
├── text_encoder.pt   # Text encoder (required)
├── decoder.pt        # Mask decoder (required)
├── vocab.json        # BPE vocabulary (optional, falls back to char-level encoding)
└── merges.txt        # BPE merge rules (optional)
```

Model zoo variants: `sam3-vit-b` (~375MB), `sam3-vit-l` (~1.2GB). Cached in `models/segmentation/`.

## MSBuild Gotcha: `.cs.stpl` Files as Embedded Resources

Files with `.cs` in the name (e.g., `IDriver.cs.stpl`) cause MSBuild's `CreateCSharpManifestResourceName` to treat `.cs` as a culture identifier (Czech), setting `WithCulture=true` and breaking embedding. The `ScaffoldX.Templates.csproj` must include:

```xml
<ItemGroup>
    <None Remove="**\*.stpl" />
    <EmbeddedResource Include="**\*.stpl" WithCulture="false" Culture="" />
</ItemGroup>
```

Without `WithCulture="false"`, only files without `.cs` in the name (like `gitignore.stpl`, `SidebarView.xaml.stpl`) get embedded.

## Industrial Domain Context

**Project Types:**
- **Collection**: Data acquisition from PLCs (Siemens S7, Modbus TCP, OPC-UA, Mitsubishi MC, Omron FINS)
- **Vision**: Camera integration and image processing (Basler, Hikvision, Cognex)
- **System**: User management, role permissions, audit logging, theme switching

**Driver Architecture:**
- Each driver implements `IDriver` interface
- Simulation drivers available for offline development
- Connection parameters: IP, port, rack, slot (S7), endpoint (OPC-UA)

## Testing

**Stack:** xUnit + FluentAssertions + Moq

**Run all tests:**
```bash
dotnet test tests/ScaffoldX.Core.Tests/
dotnet test tests/ScaffoldX.App.Tests/
```

**Run a single test class:**
```bash
dotnet test tests/ScaffoldX.Core.Tests/ --filter "ClassName~VariableResolverTests"
dotnet test tests/ScaffoldX.App.Tests/ --filter "ClassName~ValidationServiceTests"
```

**Current test coverage (~244 test methods across 30 files):**

Core layer (63 methods, 7 files):
- `TemplateRegistryTests` — template loading, category filtering (Vision/System/Common)
- `VariableResolverTests` — variable context building (PascalCase), ToPascalCase conversion, ScaffoldX metadata, system module variables
- `PostProcessorTests` — line endings, XML entity restoration, trailing whitespace
- `FileTreeBuilderTests` — file tree structure, conditional modules, gitignore, inference engine, system modules
- `FullGenerationFlowTests` — integration tests: Collection/Vision/System project template selection, mixed config, Common templates always included, PostProcessor pipeline, variable context completeness
- `MaskToPolygonTests` — mask-to-polygon conversion (Marching Squares, Douglas-Peucker simplification)
- `Sam3SegmentorTests` — SAM 3 model loading, tokenizer, ImageEmbedding, contour extraction

App layer (181 methods, 23 files):
- `ValidationServiceTests` — project name validation, IP address validation, port validation, PascalCase conversion, output path validation
- `AnnotationServiceTests` — YOLO format conversion (ToYoloFormat/FromYoloFormat), round-trip consistency, invalid input handling
- `AnnotationServicePolygonTests` — polygon YOLO export
- `AnnotationServiceObbTests` — OBB YOLO export
- `AnnotationServiceDotaTests` — DOTA format export
- `AnnotationServiceMotTests` — MOT format export
- `ScribanTemplateEngineTests` — template rendering, variable substitution, boolean/loop syntax, error handling
- `PolygonAnnotationTests` — polygon model, round-trip
- `OrientedBoundingBoxAnnotationTests` — OBB model
- `PolylineCircleAnnotationTests` — polyline and circle annotation models
- `AnnotationImportTests` — import annotations from YOLO format
- `AnnotationKeyboardTests` — keyboard shortcuts
- `AnnotationZoomTests` — zoom/pan
- `AnnotationUndoRedoTests` — undo/redo for all types
- `AnnotationReviewTests` — review summary, goto unannotated
- `AnnotationStatisticsTests` — annotation counts
- `UndoRedoManagerTests` — generic undo/redo manager
- `DrawingStateManagerTests` — drawing state
- `VideoFrameServiceTests` — video frame extraction
- `AutoLabelingHandlerTests` — SAM 3 model lifecycle, text/point/reference segmentation, batch operations
- `Sam3LabelingHandlerTests` — SAM 3 text/point/reference prompt handlers
- `Sam3AutoLabelingServiceTests` — SAM 3 service mock tests
- `HandlerTests` — handler integration tests

**Test conventions:**
- Follow Arrange-Act-Assert pattern
- Test file mirrors source namespace (e.g., `TemplateProcessing/TemplateRegistryTests.cs`)
- Use `[Theory]` with `[InlineData]` for parameterized tests
- Target 80%+ coverage on business logic

## Code Style

- C# 12 with nullable reference types enabled, target framework net10.0-windows
- Implicit usings enabled
- XML documentation comments on all public APIs
- Async/await for I/O operations
- Structured logging with Serilog (use `Log.ForContext<T>()`)
- Python scripts embedded in C# use `$$$"""` raw string literals (triple dollar) to handle `{{`/`}}` dict braces

## TorchSharp Patterns

**Tensor lifecycle:** TorchSharp tensors hold native memory. Always `Dispose()` tensors when done. Use `using` where possible. For `module.forward()` results, cast explicitly: `(torch.Tensor)module.forward(input)`.

**Image preprocessing:** `BitmapToTensor()` in `InferenceEngineBase` converts Bitmap to CHW float tensor normalized to [0,1]. `Sam3Segmentor` has its own `BitmapToNormalizedTensor()` for SAM 3's 1024x1024 input.

**SAM 3 interaction modes:**
- Text prompt: `SegmentByTextAsync` — batch segmentation by class names
- Point prompt: `SegmentByPointsAsync` — interactive, left=positive/right=negative points
- Reference image: `SegmentByReferenceAsync` — find similar objects

**ImageEmbedding caching:** `EncodeImageAsync` is expensive (~1s). Cache the result and reuse for multiple prompts on the same image. `AutoLabelingService` tracks `_cachedEmbeddingPath` to invalidate stale caches on image navigation.

**MaskToPolygonConverter:** Converts byte[,] binary masks to normalized polygon contours. Uses Marching Squares for contour extraction (checks `visited` array during tracing) and iterative Douglas-Peucker for simplification.

## PRD Reference

Detailed requirements in `docs/ScaffoldX_PRD_v2.md` — covers all feature specifications, architecture decisions, and acceptance criteria.
SAM 3 and TorchSharp integration specs in `docs/ScaffoldX_PRD_v3.md`.

## Context & Decision Records

- **CONTEXT.md** — Domain glossary with key terms, interfaces, and subsystem definitions. Read this first when unfamiliar with domain terminology.
- **docs/adr/** — Architectural Decision Records. Key ADR: `0001-core-layer-refactor.md` covers the DI refactor, dead code cleanup, ProjectConfig unification, and ITemplateSource abstraction.

## Agent skills

### Issue tracker

GitHub Issues via `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Default vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout: `CONTEXT.md` + `docs/adr/` at repo root. See `docs/agents/domain.md`.

### Multi-Agent Orchestration

For batch development workflows, use the orchestration system defined in `docs/agents/orchestrator.md`. Key rules:
- **谁写谁修**: The agent that writes code fixes its own bugs
- **谁提谁验**: The agent that finds a bug verifies its fix
- **三轮上限**: Max 3 fix rounds, then escalate to human
- **批量并行**: Independent issues run in parallel via `Agent(run_in_background=true)`
- **可恢复性**: Use `Agent(name="dev-{n}")` + `SendMessage(to="dev-{n}")` for recovery

Skills workflow order: `/grill-with-docs` → `/to-prd` → `/to-issues` → `/tdd` → `/diagnose`

Sub-agent registry template: `docs/agents/sub-agent-registry.md`

### Continuous Development Workflow (持续开发工作流)

**IMPORTANT: 主智能体（Claude）是编排者，不直接写代码。所有实现工作委托给子智能体。**

当用户说"开始开发"、"全方面完善"、"继续执行"等指令时，按以下流程循环执行直到项目可验收：

#### 启动条件检查
1. `gh auth status` — 确认 GitHub 认证
2. `dotnet test` — 确认当前测试全部通过
3. 读取 `docs/agents/orchestrator.md` — 加载编排规范
4. 读取 `CONTEXT.md` + `docs/adr/` — 加载领域上下文
5. 检查 GitHub open issues — 了解待办事项

#### 循环执行流程
```
WHILE 项目未达到可验收状态:
  1. ANALYZE — 运行架构分析（/improve-codebase-architecture 或手动探索）
  2. PLAN — 创建 PRD + 拆分 Issues（/to-prd → /to-issues）
  3. BATCH DEVELOP — 按依赖层级并行启动 dev-{n} 子智能体
  4. BATCH TEST — 并行启动 test-{n} 子智能体验证
  5. FIX LOOP (max 3 rounds):
     FOR each bug:
       SendMessage(to="dev-{n}") — 恢复原开发者修复
       SendMessage(to="test-{n}") — 恢复原测试者验收
     IF round >= 3 and still bugs: ESCALATE to human
  6. COMMIT — 提交本轮所有变更
  7. LOG — 生成 WORKFLOW_LOG
  8. REPEAT — 回到步骤 1 继续下一轮
```

#### 子智能体启动规范
- 使用 `Agent(name="dev-{issue}", run_in_background=true)` 启动
- 使用 `Agent(name="test-{issue}", run_in_background=true)` 启动测试
- 等待完成通知（task-notification）后继续下一步
- 独立任务必须并行启动，有依赖的串行等待

#### 完成标准（可验收）
- [ ] 所有 GitHub Issues 关闭
- [ ] `dotnet test` 全部通过（Core + App）
- [ ] 架构无 God object（单文件 < 500 行）
- [ ] 所有接口遵循 ISP 原则
- [ ] 所有 handler 可通过 mock 接口独立测试
- [ ] 无重复代码（DRY）
- [ ] 无 magic number（使用常量）
- [ ] PRD 中所有功能实现完成

#### 会话恢复
如果会话中断，新会话应：
1. 读取 CLAUDE.md 和 docs/agents/orchestrator.md
2. 检查 git log 了解上次提交
3. 检查 GitHub open issues
4. 从上次中断处继续执行循环
