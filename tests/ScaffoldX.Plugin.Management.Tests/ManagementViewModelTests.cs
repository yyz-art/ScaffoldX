using System.ComponentModel;
using ScaffoldX.Plugin.Management.Models;
using ScaffoldX.Plugin.Management.Services;
using ScaffoldX.Plugin.Management.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Management.Tests;

public class ManagementViewModelTests
{
    private readonly StubHistoryService _service;
    private readonly ManagementViewModel _vm;

    public ManagementViewModelTests()
    {
        _service = new StubHistoryService();
        _vm = new ManagementViewModel(_service);
    }

    [Fact]
    public void 默认构造_StatusMessage为就绪()
    {
        var vm = new ManagementViewModel();
        Assert.Equal("就绪", vm.StatusMessage);
    }

    [Fact]
    public void 默认构造_Projects为空集合()
    {
        var vm = new ManagementViewModel();
        Assert.Empty(vm.Projects);
    }

    [Fact]
    public void 默认构造_SelectedProject为null()
    {
        var vm = new ManagementViewModel();
        Assert.Null(vm.SelectedProject);
    }

    [Fact]
    public void 默认构造_SearchText为空字符串()
    {
        var vm = new ManagementViewModel();
        Assert.Equal(string.Empty, vm.SearchText);
    }

    [Fact]
    public async Task RefreshAsync_从服务加载记录到Projects()
    {
        _service.AddStubRecord(CreateRecord("Proj1", "WPF", @"C:\P1"));
        _service.AddStubRecord(CreateRecord("Proj2", "Web", @"C:\P2"));

        await _vm.RefreshAsync();

        Assert.Equal(2, _vm.Projects.Count);
        Assert.Equal("Proj1", _vm.Projects[0].ProjectName);
    }

    [Fact]
    public async Task RefreshAsync_更新StatusMessage()
    {
        _service.AddStubRecord(CreateRecord("Proj1", "WPF", @"C:\P1"));

        await _vm.RefreshAsync();

        Assert.Equal("已加载 1 个项目", _vm.StatusMessage);
    }

    [Fact]
    public async Task DeleteProjectAsync_删除选中项目()
    {
        var record = CreateRecord("Proj1", "WPF", @"C:\P1");
        _service.AddStubRecord(record);
        await _vm.RefreshAsync();
        _vm.SelectedProject = record;

        await _vm.DeleteProjectAsync();

        Assert.Empty(_vm.Projects);
    }

    [Fact]
    public async Task DeleteProjectAsync_删除后清空SelectedProject()
    {
        var record = CreateRecord("Proj1", "WPF", @"C:\P1");
        _service.AddStubRecord(record);
        await _vm.RefreshAsync();
        _vm.SelectedProject = record;

        await _vm.DeleteProjectAsync();

        Assert.Null(_vm.SelectedProject);
    }

    [Fact]
    public async Task DeleteProjectAsync_删除后StatusMessage更新()
    {
        var record = CreateRecord("Proj1", "WPF", @"C:\P1");
        _service.AddStubRecord(record);
        await _vm.RefreshAsync();
        _vm.SelectedProject = record;

        await _vm.DeleteProjectAsync();

        Assert.Equal("项目已删除", _vm.StatusMessage);
    }

    [Fact]
    public async Task SearchAsync_按ProjectName过滤()
    {
        _service.AddStubRecord(CreateRecord("AlphaProject", "WPF", @"C:\Alpha"));
        _service.AddStubRecord(CreateRecord("BetaProject", "Web", @"C:\Beta"));
        _vm.SearchText = "Alpha";

        await _vm.SearchAsync();

        Assert.Single(_vm.Projects);
        Assert.Equal("AlphaProject", _vm.Projects[0].ProjectName);
    }

    [Fact]
    public async Task SearchAsync_按ProjectType过滤()
    {
        _service.AddStubRecord(CreateRecord("Proj1", "WPF", @"C:\P1"));
        _service.AddStubRecord(CreateRecord("Proj2", "Web", @"C:\P2"));
        _vm.SearchText = "WPF";

        await _vm.SearchAsync();

        Assert.Single(_vm.Projects);
        Assert.Equal("Proj1", _vm.Projects[0].ProjectName);
    }

    [Fact]
    public async Task SearchAsync_空搜索文本_返回全部()
    {
        _service.AddStubRecord(CreateRecord("Proj1", "WPF", @"C:\P1"));
        _service.AddStubRecord(CreateRecord("Proj2", "Web", @"C:\P2"));
        _vm.SearchText = "";

        await _vm.SearchAsync();

        Assert.Equal(2, _vm.Projects.Count);
    }

    [Fact]
    public async Task SearchAsync_大小写不敏感()
    {
        _service.AddStubRecord(CreateRecord("MyProject", "WPF", @"C:\MP"));
        _vm.SearchText = "myproject";

        await _vm.SearchAsync();

        Assert.Single(_vm.Projects);
    }

    [Fact]
    public void SearchText设置_触发PropertyChanged()
    {
        string? changedProp = null;
        _vm.PropertyChanged += (_, e) => changedProp = e.PropertyName;
        _vm.SearchText = "test";
        Assert.Equal(nameof(ManagementViewModel.SearchText), changedProp);
    }

    [Fact]
    public void OpenProjectCommand_未选中项目_CanExecute为false()
    {
        Assert.False(_vm.OpenProjectCommand.CanExecute(null));
    }

    [Fact]
    public async Task OpenProjectCommand_选中项目_CanExecute为true()
    {
        var record = CreateRecord("Proj1", "WPF", @"C:\P1");
        _service.AddStubRecord(record);
        await _vm.RefreshAsync();
        _vm.SelectedProject = record;

        Assert.True(_vm.OpenProjectCommand.CanExecute(null));
    }

    [Fact]
    public void DeleteProjectCommand_未选中项目_CanExecute为false()
    {
        Assert.False(_vm.DeleteProjectCommand.CanExecute(null));
    }

    private static ProjectHistoryRecord CreateRecord(
        string name, string type, string path,
        DateTime? createdAt = null, DateTime? lastOpenedAt = null)
    {
        return new ProjectHistoryRecord
        {
            ProjectName = name,
            ProjectType = type,
            OutputPath = path,
            CreatedAt = createdAt ?? DateTime.Now,
            LastOpenedAt = lastOpenedAt ?? DateTime.Now,
            Tags = []
        };
    }

    private sealed class StubHistoryService : IProjectHistoryService
    {
        private readonly List<ProjectHistoryRecord> _records = [];

        public Task AddRecordAsync(ProjectHistoryRecord record)
        {
            _records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ProjectHistoryRecord>> GetAllRecordsAsync()
            => Task.FromResult<IReadOnlyList<ProjectHistoryRecord>>(_records.ToList().AsReadOnly());

        public Task DeleteRecordAsync(string outputPath)
        {
            _records.RemoveAll(r => r.OutputPath == outputPath);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ProjectHistoryRecord>> GetRecentRecordsAsync(int count)
            => Task.FromResult<IReadOnlyList<ProjectHistoryRecord>>(
                _records.OrderByDescending(r => r.LastOpenedAt != default ? r.LastOpenedAt : r.CreatedAt)
                    .Take(count).ToList().AsReadOnly());

        public Task ClearAllAsync()
        {
            _records.Clear();
            return Task.CompletedTask;
        }

        public void AddStubRecord(ProjectHistoryRecord record) => _records.Add(record);
    }
}
