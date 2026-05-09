using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Plugin.Management.Services;
using Xunit;

namespace ScaffoldX.Plugin.Management.Tests;

public class ManagementPluginServiceRegistrationTests
{
    private sealed class TestPluginHost : IPluginHost
    {
        private readonly Dictionary<string, IConfigSection> _configSections = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dictionary<(string, string), (Func<object>, Func<object>)> _views = new();

        public void RegisterView(string regionName, string navigationKey, Func<object> viewFactory, Func<object> viewModelFactory)
            => _views[(regionName, navigationKey)] = (viewFactory, viewModelFactory);
        public void NavigateTo(string regionName, string navigationKey) { }
        public T? GetService<T>() where T : class => _services.TryGetValue(typeof(T), out var svc) ? (T?)svc : null;
        public void RegisterService<T>(T instance) where T : class => _services[typeof(T)] = instance;
        public void RegisterConfigSection(IConfigSection section) => _configSections[section.SectionId] = section;
        public IConfigSection? GetConfigSection(string sectionId) => _configSections.TryGetValue(sectionId, out var s) ? s : null;
    }

    [Fact]
    public async Task OnLoadedAsync_注册IProjectHistoryService()
    {
        var plugin = new Management.ManagementPlugin();
        var host = new TestPluginHost();

        await plugin.OnLoadedAsync(host);

        var service = host.GetService<IProjectHistoryService>();
        Assert.NotNull(service);
    }

    [Fact]
    public async Task OnLoadedAsync_注册的服务为JsonProjectHistoryService()
    {
        var plugin = new Management.ManagementPlugin();
        var host = new TestPluginHost();

        await plugin.OnLoadedAsync(host);

        var service = host.GetService<IProjectHistoryService>();
        Assert.IsType<JsonProjectHistoryService>(service);
    }
}
