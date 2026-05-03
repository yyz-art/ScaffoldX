# ScaffoldX

工业自动化项目脚手架生成工具 — 通过可视化向导快速生成 WPF/Avalonia 项目骨架，内置 YOLO 标注训练平台和 ONNX 推理引擎。

## 功能特性

### 项目脚手架生成

- **4 步可视化向导**：选择项目类型 → 填写基础信息 → 专项配置 → 预览生成
- **3 种项目类型**：
  - **采集类 (Collection)**：Siemens S7、Modbus TCP、OPC-UA、Mitsubishi MC、Omron FINS 驱动
  - **视觉类 (Vision)**：相机集成、图像处理 Pipeline、ONNX 推理引擎
  - **系统类 (System)**：用户管理、角色权限、审计日志、主题切换
- **Scriban 模板引擎**：32+ 个 `.stpl` 模板文件，支持条件生成
- **双 UI 框架**：WPF (.NET 8) / Avalonia 跨平台
- **生成历史管理**：保存、加载、删除历史项目配置

### YOLO 标注工具

- **图像标注**：鼠标拖拽绘制边界框，支持多类别管理
- **自动标注**：加载 ONNX 模型，一键自动检测当前/全部图像
- **多格式导出**：YOLO (.txt)、COCO (.json)、Pascal VOC (.xml)
- **撤销/重做**：完整的操作历史支持
- **文件夹导入**：批量导入图像文件
- **类别颜色**：每个类别使用不同颜色渲染标注框

### YOLO 训练平台

- **环境检测**：自动检查 Python、Ultralytics、PyTorch、CUDA
- **模型训练**：支持 YOLOv8n/s/m/l/x 五种预训练模型
- **恢复训练**：从 `.pt` 检查点恢复中断的训练
- **实时监控**：Epoch、Loss、mAP@0.5、mAP@0.5:0.95 实时显示
- **模型验证**：训练完成后自动验证 mAP、Precision、Recall、推理速度
- **ONNX 导出**：一键导出 ONNX 格式用于部署

### ONNX 推理引擎

- **目标检测**：支持 YOLOv5/v8 格式的 ONNX 模型
- **图像分类**：ImageNet 标准分类流程
- **NMS 后处理**：非极大值抑制、置信度过滤
- **反射加载**：运行时动态加载 ONNX Runtime，避免硬依赖

## 技术栈

| 组件 | 技术 |
|------|------|
| 框架 | .NET 8, WPF |
| MVVM | Prism.Unity |
| UI 主题 | MaterialDesignThemes |
| 模板引擎 | Scriban 5.x |
| 日志 | Serilog |
| 验证 | FluentValidation |
| 推理 | ONNX Runtime (反射加载) |
| 训练 | Ultralytics YOLO (Python 子进程) |
| 测试 | xUnit + FluentAssertions + Moq |

## 快速开始

### 环境要求

- .NET 8 SDK
- Visual Studio 2022 或更高版本
- (可选) Python 3.8+ — 用于 YOLO 训练功能
- (可选) CUDA — 用于 GPU 加速训练和推理

### 构建运行

```bash
# 克隆仓库
git clone https://github.com/yyz-art/ScaffoldX.git
cd ScaffoldX

# 构建
dotnet build src/ScaffoldX.App/ScaffoldX.App.csproj

# 运行
dotnet run --project src/ScaffoldX.App/ScaffoldX.App.csproj

# 运行测试
dotnet test tests/ScaffoldX.Core.Tests/
dotnet test tests/ScaffoldX.App.Tests/
```

### 使用 YOLO 训练

```bash
# 安装 Python 依赖
pip install ultralytics

# 在 ScaffoldX 中：
# 1. 导航到 "YOLO 训练" 页面
# 2. 点击 "检查环境" 确认依赖就绪
# 3. 选择数据集目录（包含 data.yaml）
# 4. 配置训练参数
# 5. 点击 "开始训练"
```

## 项目结构

```
ScaffoldX/
├── src/
│   ├── ScaffoldX.App/              # WPF 应用程序
│   │   ├── Models/                 # 数据模型
│   │   ├── Services/               # 服务层
│   │   ├── ViewModels/             # MVVM ViewModel
│   │   └── Views/                  # XAML 视图
│   ├── ScaffoldX.Core/             # 核心业务逻辑
│   │   ├── FileGeneration/         # 文件树生成
│   │   ├── Models/                 # 配置模型
│   │   ├── TemplateProcessing/     # 模板引擎管线
│   │   └── Vision/                 # ONNX 推理引擎
│   └── ScaffoldX.Templates/        # 嵌入式 .stpl 模板
│       ├── Common/                 # 通用模板（Shell、Diagnostic、Solution）
│       ├── Collection/             # 采集驱动模板
│       ├── Vision/                 # 视觉模块模板
│       └── System/                 # 系统模块模板
├── tests/
│   ├── ScaffoldX.App.Tests/        # App 层测试 (64 tests)
│   ├── ScaffoldX.Core.Tests/       # Core 层测试 (51 tests)
│   └── ScaffoldX.IntegrationTests/ # 集成测试
└── docs/
    ├── ScaffoldX_PRD_v2.md         # 产品需求文档
    └── 开发计划.md                   # 开发计划
```

## 测试

项目包含 115 个自动化测试：

```bash
# 运行所有测试
dotnet test tests/ScaffoldX.Core.Tests/
dotnet test tests/ScaffoldX.App.Tests/

# 查看测试覆盖率
dotnet test tests/ScaffoldX.Core.Tests/ --collect:"XPlat Code Coverage"
```

**测试覆盖：**
- TemplateRegistry — 模板加载、分类过滤
- VariableResolver — 变量上下文构建、PascalCase 转换
- PostProcessor — 行尾规范化、XML 实体还原
- FileTreeBuilder — 文件树结构、条件模块
- ValidationService — 项目名/IP/端口/路径验证
- AnnotationService — YOLO 格式转换、往返一致性
- ScribanTemplateEngine — 模板渲染、错误处理
- FullGenerationFlow — 完整生成管线集成测试

## 模板系统

模板文件使用 `.stpl` 扩展名，支持以下指令：

```
##OUTPUT: src/{{ProjectName}}.Core/Abstractions/IPlugin.cs
##REQUIRED: false
```

- `##OUTPUT:` — 输出文件路径（支持 Scriban 变量）
- `##REQUIRED: false` — 仅在对应功能启用时生成

变量使用 PascalCase 命名：`{{ProjectName}}`、`{{NamespacePrefix}}`、`{{EnableVision}}` 等。

## 贡献

1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'feat: add amazing feature'`)
4. 推送到远程 (`git push origin feature/amazing-feature`)
5. 创建 Pull Request

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件。
