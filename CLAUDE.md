# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ScaffoldX** is a WPF desktop application (.NET 8) that generates industrial automation project scaffolds through a visual wizard. It targets industrial HMI/SCADA developers who need to quickly bootstrap WPF/Avalonia projects with pre-configured drivers, vision modules, and system components.

**Key characteristics:**
- Offline-first design for industrial control network isolation
- Scriban template engine with `.stpl` template files
- Prism MVVM framework for modular WPF architecture
- Generates complete Visual Studio solutions ready to compile
- Integrated YOLO annotation tool, training platform, and ONNX inference engine

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
│   ├── ScaffoldX.Core/         # Business logic, template processing, vision inference
│   └── ScaffoldX.Templates/    # Embedded .stpl template resources
└── tests/
    ├── ScaffoldX.App.Tests/        # App layer tests (64 tests, ValidationService, AnnotationService, ScribanTemplateEngine)
    ├── ScaffoldX.Core.Tests/       # Core layer tests (51 tests, TemplateRegistry, VariableResolver, PostProcessor, FileTreeBuilder, Integration)
    └── ScaffoldX.IntegrationTests/ # Integration tests (empty, needs implementation)
```

### Dependency Flow

```
ScaffoldX.App → ScaffoldX.Core → ScaffoldX.Templates
```

- **ScaffoldX.App**: WPF UI, ViewModels, Services, dependency injection via Prism.Unity
- **ScaffoldX.Core**: TemplateRegistry, VariableResolver, PostProcessor, FileTreeBuilder, ONNX inference
- **ScaffoldX.Templates**: Embedded `.stpl` files (Scriban templates) as assembly resources

### Key Components

**Template System:**
- `.stpl` files in `ScaffoldX.Templates/` are embedded resources loaded at runtime
- Template directives: `##OUTPUT:` (output path), `##REQUIRED:` (conditional generation)
- Variables use `snake_case` naming convention
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

**Vision Module (YOLO + ONNX):**
- `AnnotationService` / `AnnotationView` — YOLO bounding-box annotation tool
- `YoloTrainingService` — Python/Ultralytics training script generation and execution
- `InferenceEngineBase` / `OnnxDetector` / `OnnxClassifier` — ONNX Runtime inference (reflection-based loading to avoid hard NuGet dependency)
- `AnnotationModels.cs` — Core data models for annotation, training config, and results

**Wizard Steps (MVVM):**
- Step 1: Project type selection (Collection/Vision/System)
- Step 2: Basic info (name, path, namespace, UI framework)
- Step 3: Specific configuration (drivers, vision, modules)
- Step 4: Preview file tree and generate

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
- `{{TargetFramework}}` — Target framework (e.g., net8.0-windows)
- `{{UIFramework}}` — WPF or Avalonia
- `{{XamlExt}}` — xaml or axaml based on UI framework
- `{{EnableVision}}`, `{{EnableSiemensS7}}`, etc. — boolean flags for conditional generation
- `{{ScaffoldXVersion}}` — ScaffoldX version string
- `{{GeneratedAt}}` — Generation timestamp

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

**Current test coverage (115 tests total):**

Core layer (51 tests):
- `TemplateRegistryTests` — template loading, category filtering (Vision/System/Common)
- `VariableResolverTests` — variable context building (PascalCase), ToPascalCase conversion, ScaffoldX metadata, system module variables
- `PostProcessorTests` — line endings, XML entity restoration, trailing whitespace
- `FileTreeBuilderTests` — file tree structure, conditional modules, gitignore, inference engine, system modules
- `FullGenerationFlowTests` — integration tests: Collection/Vision/System project template selection, mixed config, Common templates always included, PostProcessor pipeline, variable context completeness

App layer (64 tests):
- `ValidationServiceTests` — project name validation, IP address validation, port validation, PascalCase conversion, output path validation
- `AnnotationServiceTests` — YOLO format conversion (ToYoloFormat/FromYoloFormat), round-trip consistency, invalid input handling
- `ScribanTemplateEngineTests` — template rendering, variable substitution, boolean/loop syntax, error handling

**Test conventions:**
- Follow Arrange-Act-Assert pattern
- Test file mirrors source namespace (e.g., `TemplateProcessing/TemplateRegistryTests.cs`)
- Use `[Theory]` with `[InlineData]` for parameterized tests
- Target 80%+ coverage on business logic

## Code Style

- C# 12 with nullable reference types enabled
- Implicit usings enabled
- XML documentation comments on all public APIs
- Async/await for I/O operations
- Structured logging with Serilog (use `Log.ForContext<T>()`)
- Python scripts embedded in C# use `$$$"""` raw string literals (triple dollar) to handle `{{`/`}}` dict braces

## PRD Reference

Detailed requirements in `docs/ScaffoldX_PRD_v2.md` — covers all feature specifications, architecture decisions, and acceptance criteria.
