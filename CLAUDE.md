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
    ├── ScaffoldX.App.Tests/        # App layer tests (249 tests)
    ├── ScaffoldX.Core.Tests/       # Core layer tests (80 tests)
    └── ScaffoldX.IntegrationTests/ # Integration tests (empty, needs implementation)
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

**Current test coverage (329 tests total):**

Core layer (80 tests):
- `TemplateRegistryTests` — template loading, category filtering (Vision/System/Common)
- `VariableResolverTests` — variable context building (PascalCase), ToPascalCase conversion, ScaffoldX metadata, system module variables
- `PostProcessorTests` — line endings, XML entity restoration, trailing whitespace
- `FileTreeBuilderTests` — file tree structure, conditional modules, gitignore, inference engine, system modules
- `FullGenerationFlowTests` — integration tests: Collection/Vision/System project template selection, mixed config, Common templates always included, PostProcessor pipeline, variable context completeness
- `InferenceRobustnessTests` — null guards, model-not-loaded (TorchSharp engine)
- `InferenceUtilityTests` — ResizeImage, CreateDetectionResult
- `MaskToPolygonTests` — mask-to-polygon conversion (Marching Squares, Douglas-Peucker simplification)
- `Sam3SegmentorTests` — SAM 3 model loading, tokenizer, ImageEmbedding, contour extraction

App layer (249 tests):
- `ValidationServiceTests` — project name validation, IP address validation, port validation, PascalCase conversion, output path validation
- `AnnotationServiceTests` — YOLO format conversion (ToYoloFormat/FromYoloFormat), round-trip consistency, invalid input handling
- `ScribanTemplateEngineTests` — template rendering, variable substitution, boolean/loop syntax, error handling
- `PolygonAnnotationTests` — polygon model, round-trip
- `OrientedBoundingBoxAnnotationTests` — OBB model
- `AnnotationServicePolygonTests` — polygon YOLO export
- `AnnotationServiceObbTests` — OBB YOLO export
- `AnnotationImportTests` — import annotations
- `AnnotationKeyboardTests` — keyboard shortcuts
- `AnnotationZoomTests` — zoom/pan
- `AnnotationUndoRedoTests` — undo/redo for all types
- `AnnotationStatisticsTests` — annotation counts
- `UndoRedoManagerTests` — generic undo/redo manager
- `DrawingStateManagerTests` — drawing state
- `VideoFrameServiceTests` — video frame extraction
- `AutoLabelingHandlerTests` — SAM 3 model lifecycle, text/point/reference segmentation, batch operations
- `Sam3AutoLabelingServiceTests` — SAM 3 service mock tests

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
