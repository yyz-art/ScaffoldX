# ADR-0001: Core Layer Refactor

## Status

Accepted

## Context

The ScaffoldX Core layer has accumulated architectural debt:
- Static classes (`VariableResolver`, `PostProcessor`) are called directly by `ProjectGenerator`, making the generation pipeline untestable in isolation.
- `TemplateRegistry` is registered as a concrete singleton with no interface.
- Dead code exists: `FileWriter`, `DirectoryCreator` (unused by `ProjectGenerator`), `InferenceEngineBase` (abstract class with zero subclasses).
- Two `ProjectConfig` classes exist in different namespaces with different field names, bridged by a `MapToCoreConfig()` method with hardcoded string matching.
- `ISam3SegmentationEngine` exists but is never used as an abstraction boundary — `AutoLabelingService` hardcodes `new Sam3Segmentor()`.
- The template system is closed — templates are embedded assembly resources with no extensibility mechanism.

## Decisions

### 1. Convert static classes to DI-managed instances

`VariableResolver`, `PostProcessor`, and `TemplateRegistry` become instance classes behind interfaces (`IVariableResolver`, `IPostProcessor`, `ITemplateRegistry`). Registered as singletons in the DI container.

**Why:** Enables constructor injection, unit testing with mocks, and future substitution of implementations.

### 2. Delete dead code

- `FileWriter` — unused, `ProjectGenerator` writes files inline.
- `DirectoryCreator` — unused, `ProjectGenerator` creates directories inline.
- `InferenceEngineBase` — abstract class with zero subclasses.

**Why:** Dead code adds confusion and maintenance burden without providing value. If YOLO detection engines are needed later, a new base class can be designed with concrete requirements.

### 3. Unify ProjectConfig on Core's model

Delete `App.Models.ProjectConfig`. Use `Core.Models.ProjectConfig` as the single source of truth. The wizard ViewModels populate Core's fields directly.

**Why:** Eliminates the `MapToCoreConfig()` bridge code with its hardcoded string matching (`SelectedDrivers.Contains("S7Net")` → `EnableSiemensS7`). Reduces drift risk between two parallel models.

### 4. One interface per class

Core interfaces: `ITemplateRegistry`, `IVariableResolver`, `IPostProcessor`, `IFileTreeBuilder`, `ITemplateSource`.

**Why:** Simple, clear boundaries. Each interface maps to a single responsibility. Avoids fat interfaces that mix concerns.

### 5. Abstract template loading with ITemplateSource

`TemplateRegistry` receives an `ITemplateSource` via constructor injection. Built-in implementation: `AssemblyTemplateSource` (loads from embedded resources). Future: `DirectoryTemplateSource` (loads from a file-system directory).

**Why:** Opens the template system to user-provided templates without changing the registry's core logic.

### 6. Inject ISam3SegmentationEngine into AutoLabelingService

`AutoLabelingService` receives `ISam3SegmentationEngine` via constructor injection instead of creating `Sam3Segmentor` directly. `Sam3Segmentor` is registered in DI.

**Why:** Enables testing with mock engines, swapping in alternative segmentation implementations, and proper lifecycle management through the DI container.

## Consequences

- `ProjectGenerator`'s constructor grows from 4 to 6+ injected dependencies (ITemplateRegistry, IVariableResolver, IPostProcessor, IFileTreeBuilder, ITemplateEngine, IHistoryService, IValidationService).
- The App layer's wizard ViewModels need minor updates to populate Core's `ProjectConfig` fields directly.
- `FileTreeBuilder` and `Step4ViewModel.BuildFileTree()` must be unified — only `FileTreeBuilder` survives, with the ViewModel consuming its output.
- `AnnotationService` fat interface (13 methods) is NOT addressed in this ADR — deferred to a future App-layer refactor.
