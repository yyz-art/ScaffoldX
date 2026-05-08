# PRD: 架构深度改进 — 7 个模块重构

> **状态**: In Progress
> **创建日期**: 2026-05-08

---

## Problem Statement

ScaffoldX 的 App 层存在多个架构问题：
1. AnnotationViewModel 是 1046 行的 God object
2. AnnotationContext 用 20+ Func/Action 委托实现双向依赖
3. AnnotationService 是 961 行的胖接口（仓储 + 5 种导出）
4. MainWindowViewModel 手动同步 12+ 字段到 ProjectConfig
5. Step3ViewModel 混合 3 个不相关配置域
6. AnnotationVM 命令直接实例化 WPF Dialog，无法测试
7. 坐标变换逻辑重复 4 次 + magic number

## 依赖顺序

```
Layer 0: #2 AnnotationContext, #6 IDialogService, #5 Step3Split, #7 CoordinateMapper
Layer 1: #4 MainWindowSync (dep #5), #3 AnnotationServiceISP (dep #2)
Layer 2: #1 AnnotationViewModel (dep #2, #3, #6)
```

## 约束

- 332 个测试不能回归
- 每个重构独立可提交
- 不改变外部行为
