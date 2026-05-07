# CONTEXT.md — ScaffoldX Domain Glossary

## Core Domain

**ScaffoldX** — A WPF desktop tool that generates industrial automation project scaffolds through a visual wizard. Targets HMI/SCADA developers bootstrapping WPF/Avalonia projects with pre-configured drivers, vision modules, and system components.

## Project Generation

**ProjectConfig** — (Core.Models) Configuration for scaffold generation. Single source of truth. Fields: boolean flags (EnableSiemensS7, EnableModbusTcp, etc.), TargetFramework string, OutputDirectory, Author, Company, Description. The App-layer wizard populates these fields directly.

**TemplateRegistry** — Loads `.stpl` template files from an `ITemplateSource`. Filters templates based on ProjectConfig flags via `GetTemplatesForConfig()`. Replaces the old concrete class.

**ITemplateSource** — Abstraction for template loading. Built-in implementation: `AssemblyTemplateSource` (embedded resources). Future: `DirectoryTemplateSource` (file-system).

**VariableResolver** — Builds the Scriban variable context (`Dictionary<string, object>`) from ProjectConfig. PascalCase keys. Instance class behind `IVariableResolver` interface.

**PostProcessor** — Pipeline of 4 steps applied to rendered template output: normalize line endings, restore XML entities, trim trailing whitespace, ensure trailing newline. Instance class behind `IPostProcessor` interface.

**FileTreeBuilder** — Constructs a `FileTreeNode` hierarchy from ProjectConfig for preview purposes. Instance class behind `IFileTreeBuilder` interface.

**FileTreeNode** — Hierarchical node representing a file or folder in the generated project tree. Single class (the duplicate in Step4ViewModel is eliminated).

## Template System

**`.stpl` file** — Scriban template with two directives: `##OUTPUT:` (output path template) and `##REQUIRED:` (boolean, controls conditional inclusion). 67 templates across 4 categories: Common, Collection, Vision, System.

**TemplateFile** — Parsed template metadata POCO: Name, Content, OutputPathTemplate, Category, IsRequired.

## Industrial Domain

**Collection project** — Data acquisition from PLCs. Drivers: Siemens S7, Modbus TCP, OPC-UA, Mitsubishi MC, Omron FINS. Each driver implements `IDriver`.

**Vision project** — Camera integration and image processing. Brands: Basler, Hikvision, Cognex.

**System project** — User management, role permissions, audit logging, theme switching. Modules: UserManagement, RolePermission, SystemLog, ThemeSwitcher.

## Vision / Annotation Subsystem

**ISam3SegmentationEngine** — (Core.Vision) Interface for SAM 3 segmentation. Methods: LoadModelAsync, EncodeImageAsync, SegmentByTextAsync, SegmentByPointsAsync, SegmentByBoxAsync, SegmentByReferenceAsync. Single implementation: Sam3Segmentor.

**ImageEmbedding** — Disposable wrapper around a cached image feature tensor. Caller is responsible for Dispose. Expensive to compute (~1s), cheap to reuse.

**AutoLabelingService** — (App.Services) Facade over ISam3SegmentationEngine. Manages model lifecycle, embedding cache, and adapts Core types to App annotation models. Receives ISam3SegmentationEngine via constructor injection.

**AnnotationType** — Six types: BoundingBox (axis-aligned rect), Polygon (arbitrary vertices), OBB (oriented bounding box with rotation), Polyline (open path), Circle (center + radius), Segmentation (mask + contour).

**AnnotationService** — (App.Services) Handles annotation project CRUD, format conversion (YOLO/COCO/VOC/DOTA/MOT), and export. Candidate for ISP split into IAnnotationRepository + IAnnotationExporter.

## Key Decisions

See `docs/adr/` for architectural decision records.
