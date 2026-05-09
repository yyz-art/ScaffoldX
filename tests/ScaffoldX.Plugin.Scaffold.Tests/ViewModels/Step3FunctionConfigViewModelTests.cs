using ScaffoldX.Plugin.Scaffold.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class Step3FunctionConfigViewModelTests
{
    private Step3FunctionConfigViewModel CreateViewModel()
    {
        return new Step3FunctionConfigViewModel();
    }

    #region Constructor & Default Values

    [Fact]
    public void Constructor_默认值正确()
    {
        var vm = CreateViewModel();

        // Collection 默认值
        Assert.Equal(1000, vm.ScanCycleMs);
        Assert.True(vm.EnableDataLogging);
        Assert.Equal(StorageType.Sqlite, vm.StorageType);
        Assert.Equal(string.Empty, vm.DatabaseConnectionString);
        Assert.Equal("30 天", vm.DataRetentionPolicy);

        // Vision 默认值
        Assert.Equal(VisionAlgorithm.Yolo, vm.SelectedAlgorithm);

        // YOLO 默认值
        Assert.Equal("YOLOv8n", vm.YoloModelVersion);
        Assert.Equal(0.5, vm.YoloConfidenceThreshold);
        Assert.Equal(0.45, vm.YoloIouThreshold);
        Assert.Equal("640", vm.YoloInputSize);

        // SAM3 默认值
        Assert.Equal("SAM3-H", vm.Sam3ModelType);
        Assert.Equal("自动分割", vm.Sam3SegmentMode);
        Assert.Equal(0.95, vm.Sam3StabilityScoreThresh);
        Assert.Equal(0.7, vm.Sam3CropNmsThresh);
    }

    #endregion

    #region Collection Properties

    [Fact]
    public void ScanCycleMs_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.ScanCycleMs = 500;

        Assert.Equal(500, vm.ScanCycleMs);
    }

    [Fact]
    public void EnableDataLogging_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnableDataLogging = false;

        Assert.False(vm.EnableDataLogging);
    }

    [Fact]
    public void StorageType_设置Sqlite_正确存储()
    {
        var vm = CreateViewModel();

        vm.StorageType = StorageType.Sqlite;

        Assert.Equal(StorageType.Sqlite, vm.StorageType);
        Assert.True(vm.StorageTypeSqlite);
        Assert.False(vm.StorageTypeInfluxDb);
        Assert.False(vm.StorageTypeSqlServer);
        Assert.False(vm.StorageTypeRequiresConnectionString);
    }

    [Fact]
    public void StorageType_设置InfluxDb_正确存储()
    {
        var vm = CreateViewModel();

        vm.StorageType = StorageType.InfluxDb;

        Assert.Equal(StorageType.InfluxDb, vm.StorageType);
        Assert.False(vm.StorageTypeSqlite);
        Assert.True(vm.StorageTypeInfluxDb);
        Assert.False(vm.StorageTypeSqlServer);
        Assert.True(vm.StorageTypeRequiresConnectionString);
    }

    [Fact]
    public void StorageType_设置SqlServer_正确存储()
    {
        var vm = CreateViewModel();

        vm.StorageType = StorageType.SqlServer;

        Assert.Equal(StorageType.SqlServer, vm.StorageType);
        Assert.False(vm.StorageTypeSqlite);
        Assert.False(vm.StorageTypeInfluxDb);
        Assert.True(vm.StorageTypeSqlServer);
        Assert.True(vm.StorageTypeRequiresConnectionString);
    }

    [Fact]
    public void StorageTypeSqlite_设置True_切换到Sqlite()
    {
        var vm = CreateViewModel();
        vm.StorageType = StorageType.InfluxDb;

        vm.StorageTypeSqlite = true;

        Assert.Equal(StorageType.Sqlite, vm.StorageType);
    }

    [Fact]
    public void StorageTypeInfluxDb_设置True_切换到InfluxDb()
    {
        var vm = CreateViewModel();

        vm.StorageTypeInfluxDb = true;

        Assert.Equal(StorageType.InfluxDb, vm.StorageType);
    }

    [Fact]
    public void StorageTypeSqlServer_设置True_切换到SqlServer()
    {
        var vm = CreateViewModel();

        vm.StorageTypeSqlServer = true;

        Assert.Equal(StorageType.SqlServer, vm.StorageType);
    }

    [Fact]
    public void DatabaseConnectionString_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.DatabaseConnectionString = "Server=localhost;Database=test";

        Assert.Equal("Server=localhost;Database=test", vm.DatabaseConnectionString);
    }

    [Fact]
    public void DataRetentionPolicy_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.DataRetentionPolicy = "7 天";

        Assert.Equal("7 天", vm.DataRetentionPolicy);
    }

    #endregion

    #region Vision Properties

    [Fact]
    public void SelectedAlgorithm_设置Yolo_正确存储()
    {
        var vm = CreateViewModel();

        vm.SelectedAlgorithm = VisionAlgorithm.Yolo;

        Assert.Equal(VisionAlgorithm.Yolo, vm.SelectedAlgorithm);
        Assert.True(vm.IsYoloSelected);
        Assert.False(vm.IsSam3Selected);
    }

    [Fact]
    public void SelectedAlgorithm_设置Sam3_正确存储()
    {
        var vm = CreateViewModel();

        vm.SelectedAlgorithm = VisionAlgorithm.Sam3;

        Assert.Equal(VisionAlgorithm.Sam3, vm.SelectedAlgorithm);
        Assert.False(vm.IsYoloSelected);
        Assert.True(vm.IsSam3Selected);
    }

    [Fact]
    public void SelectYoloCommand_执行后_选择Yolo算法()
    {
        var vm = CreateViewModel();
        vm.SelectedAlgorithm = VisionAlgorithm.Sam3;

        vm.SelectYoloCommand.Execute(null);

        Assert.Equal(VisionAlgorithm.Yolo, vm.SelectedAlgorithm);
    }

    [Fact]
    public void SelectSam3Command_执行后_选择Sam3算法()
    {
        var vm = CreateViewModel();
        vm.SelectedAlgorithm = VisionAlgorithm.Yolo;

        vm.SelectSam3Command.Execute(null);

        Assert.Equal(VisionAlgorithm.Sam3, vm.SelectedAlgorithm);
    }

    #endregion

    #region YOLO Parameters

    [Fact]
    public void YoloModelVersion_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.YoloModelVersion = "YOLOv8x";

        Assert.Equal("YOLOv8x", vm.YoloModelVersion);
    }

    [Fact]
    public void YoloConfidenceThreshold_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.YoloConfidenceThreshold = 0.7;

        Assert.Equal(0.7, vm.YoloConfidenceThreshold);
    }

    [Fact]
    public void YoloIouThreshold_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.YoloIouThreshold = 0.5;

        Assert.Equal(0.5, vm.YoloIouThreshold);
    }

    [Fact]
    public void YoloInputSize_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.YoloInputSize = "1280";

        Assert.Equal("1280", vm.YoloInputSize);
    }

    #endregion

    #region SAM3 Parameters

    [Fact]
    public void Sam3ModelType_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.Sam3ModelType = "SAM3-L";

        Assert.Equal("SAM3-L", vm.Sam3ModelType);
    }

    [Fact]
    public void Sam3SegmentMode_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.Sam3SegmentMode = "点提示分割";

        Assert.Equal("点提示分割", vm.Sam3SegmentMode);
    }

    [Fact]
    public void Sam3StabilityScoreThresh_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.Sam3StabilityScoreThresh = 0.85;

        Assert.Equal(0.85, vm.Sam3StabilityScoreThresh);
    }

    [Fact]
    public void Sam3CropNmsThresh_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.Sam3CropNmsThresh = 0.6;

        Assert.Equal(0.6, vm.Sam3CropNmsThresh);
    }

    #endregion

    #region Visibility Tests

    [Fact]
    public void SetProjectType_Collection_集合可见()
    {
        var vm = CreateViewModel();

        vm.SetProjectType(ProjectTypeCategory.Collection);

        Assert.True(vm.IsCollectionVisible);
        Assert.False(vm.IsVisionVisible);
        Assert.False(vm.IsSystemVisible);
    }

    [Fact]
    public void SetProjectType_Vision_视觉可见()
    {
        var vm = CreateViewModel();

        vm.SetProjectType(ProjectTypeCategory.Vision);

        Assert.False(vm.IsCollectionVisible);
        Assert.True(vm.IsVisionVisible);
        Assert.False(vm.IsSystemVisible);
    }

    [Fact]
    public void SetProjectType_System_系统可见()
    {
        var vm = CreateViewModel();

        vm.SetProjectType(ProjectTypeCategory.System);

        Assert.False(vm.IsCollectionVisible);
        Assert.False(vm.IsVisionVisible);
        Assert.True(vm.IsSystemVisible);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void IsValid_Collection_始终返回True()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Collection);

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_Vision选择Yolo_返回True()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Vision);
        vm.SelectedAlgorithm = VisionAlgorithm.Yolo;

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_Vision选择Sam3_返回True()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Vision);
        vm.SelectedAlgorithm = VisionAlgorithm.Sam3;

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_Vision选择None_返回False()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Vision);
        vm.SelectedAlgorithm = VisionAlgorithm.None;

        Assert.False(vm.IsValid);
    }

    [Fact]
    public void IsValid_System_始终返回True()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.System);

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_无项目类型_返回False()
    {
        var vm = CreateViewModel();
        // 不设置项目类型，默认为None

        Assert.False(vm.IsValid);
    }

    #endregion

    #region Property Changed Tests

    [Fact]
    public void PropertyChanged_存储类型变更_触发相关属性通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.StorageType = StorageType.InfluxDb;

        Assert.Contains(nameof(vm.StorageType), propertyChangedEvents);
        Assert.Contains(nameof(vm.StorageTypeSqlite), propertyChangedEvents);
        Assert.Contains(nameof(vm.StorageTypeInfluxDb), propertyChangedEvents);
        Assert.Contains(nameof(vm.StorageTypeSqlServer), propertyChangedEvents);
        Assert.Contains(nameof(vm.StorageTypeRequiresConnectionString), propertyChangedEvents);
    }

    [Fact]
    public void PropertyChanged_算法选择变更_触发相关属性通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.SelectedAlgorithm = VisionAlgorithm.Sam3;

        Assert.Contains(nameof(vm.SelectedAlgorithm), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsYoloSelected), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsSam3Selected), propertyChangedEvents);
        Assert.Contains(nameof(vm.YoloCardBrush), propertyChangedEvents);
        Assert.Contains(nameof(vm.Sam3CardBrush), propertyChangedEvents);
    }

    [Fact]
    public void PropertyChanged_SetProjectType_触发可见性通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.SetProjectType(ProjectTypeCategory.Vision);

        Assert.Contains(nameof(vm.IsCollectionVisible), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsVisionVisible), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsSystemVisible), propertyChangedEvents);
    }

    [Fact]
    public void PropertyChanged_YOLO参数变更_触发通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.YoloConfidenceThreshold = 0.8;
        vm.YoloIouThreshold = 0.6;

        Assert.Contains(nameof(vm.YoloConfidenceThreshold), propertyChangedEvents);
        Assert.Contains(nameof(vm.YoloIouThreshold), propertyChangedEvents);
    }

    [Fact]
    public void PropertyChanged_SAM3参数变更_触发通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.Sam3StabilityScoreThresh = 0.9;
        vm.Sam3CropNmsThresh = 0.5;

        Assert.Contains(nameof(vm.Sam3StabilityScoreThresh), propertyChangedEvents);
        Assert.Contains(nameof(vm.Sam3CropNmsThresh), propertyChangedEvents);
    }

    #endregion

    #region Card Brush Tests

    [Fact]
    public void YoloCardBrush_选择Yolo时_不为Null()
    {
        var vm = CreateViewModel();
        vm.SelectedAlgorithm = VisionAlgorithm.Yolo;

        Assert.NotNull(vm.YoloCardBrush);
    }

    [Fact]
    public void Sam3CardBrush_选择Sam3时_不为Null()
    {
        var vm = CreateViewModel();
        vm.SelectedAlgorithm = VisionAlgorithm.Sam3;

        Assert.NotNull(vm.Sam3CardBrush);
    }

    #endregion
}
