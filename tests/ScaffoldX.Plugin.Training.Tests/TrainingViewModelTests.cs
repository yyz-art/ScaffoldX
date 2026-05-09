using ScaffoldX.Plugin.Training.Models;
using ScaffoldX.Plugin.Training.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Training.Tests;

public class TrainingViewModelTests
{
    private static TrainingViewModel CreateVm() => new();

    [Fact]
    public void Config_默认不为null()
    {
        var vm = CreateVm();
        Assert.NotNull(vm.Config);
    }

    [Fact]
    public void Config_默认ModelType为YoloV8()
    {
        var vm = CreateVm();
        Assert.Equal(ModelType.YoloV8, vm.Config.ModelType);
    }

    [Fact]
    public void IsTraining_默认为false()
    {
        var vm = CreateVm();
        Assert.False(vm.IsTraining);
    }

    [Fact]
    public void TrainingProgress_默认为0()
    {
        var vm = CreateVm();
        Assert.Equal(0, vm.TrainingProgress);
    }

    [Fact]
    public void StartTrainingCommand_配置无效时不可执行()
    {
        var vm = CreateVm();
        Assert.False(vm.StartTrainingCommand.CanExecute(null));
    }

    [Fact]
    public void StartTrainingCommand_配置有效时可执行()
    {
        var vm = CreateVm();
        vm.Config.DatasetPath = @"C:\data\test";
        vm.Config.Epochs = 10;
        vm.Config.BatchSize = 4;
        vm.Config.ImageSize = 640;
        vm.Config.LearningRate = 0.01;
        Assert.True(vm.StartTrainingCommand.CanExecute(null));
    }

    [Fact]
    public void ExportModelCommand_模型路径为空时不可执行()
    {
        var vm = CreateVm();
        Assert.False(vm.ExportModelCommand.CanExecute(null));
    }

    [Fact]
    public void ExportModelCommand_模型路径非空时可执行()
    {
        var vm = CreateVm();
        vm.TrainedModelPath = @"C:\models\best.pt";
        Assert.True(vm.ExportModelCommand.CanExecute(null));
    }

    [Fact]
    public void StatusMessage_设置后触发PropertyChanged()
    {
        var vm = CreateVm();
        bool fired = false;
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == "StatusMessage") fired = true; };
        vm.StatusMessage = "训练中";
        Assert.True(fired);
    }

    [Fact]
    public void IsTraining_设置后触发PropertyChanged()
    {
        var vm = CreateVm();
        bool fired = false;
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == "IsTraining") fired = true; };
        vm.IsTraining = true;
        Assert.True(fired);
    }
}
