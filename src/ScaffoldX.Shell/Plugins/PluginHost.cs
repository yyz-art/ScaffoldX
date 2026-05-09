using System.Windows;
using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Plugins;

namespace ScaffoldX.Shell.Plugins;

public sealed class PluginHost : IPluginHost
{
    private readonly Dictionary<string, IConfigSection> _configSections = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Type, object> _services = new();
    private readonly Dictionary<(string Region, string Key), (Func<object> ViewFactory, Func<object> ViewModelFactory)> _views = new();

    public void RegisterView(string regionName, string navigationKey, Func<object> viewFactory, Func<object> viewModelFactory)
    {
        _views[(regionName, navigationKey)] = (viewFactory, viewModelFactory);
    }

    public IEnumerable<ViewInfo> GetRegisteredViews()
    {
        foreach (var kvp in _views)
        {
            yield return new ViewInfo(
                kvp.Key.Region,
                kvp.Key.Key,
                kvp.Value.ViewFactory,
                kvp.Value.ViewModelFactory);
        }
    }

    public record ViewInfo(string RegionName, string ViewName, Func<object> ViewFactory, Func<object> ViewModelFactory);

    public void NavigateTo(string regionName, string navigationKey)
    {
    }

    public T? GetService<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var service) ? (T?)service : null;
    }

    public void RegisterConfigSection(IConfigSection section)
    {
        _configSections[section.SectionId] = section;
    }

    public IConfigSection? GetConfigSection(string sectionId)
    {
        return _configSections.TryGetValue(sectionId, out var section) ? section : null;
    }

    public bool HasView(string regionName, string navigationKey)
    {
        return _views.ContainsKey((regionName, navigationKey));
    }

    public object? GetView(string regionName, string navigationKey)
    {
        if (_views.TryGetValue((regionName, navigationKey), out var viewEntry))
        {
            var view = viewEntry.ViewFactory();
            if (viewEntry.ViewModelFactory != null && view is FrameworkElement fe)
            {
                fe.DataContext = viewEntry.ViewModelFactory();
            }
            return view;
        }
        return null;
    }

    public void RegisterService<T>(T instance) where T : class
    {
        _services[typeof(T)] = instance;
    }
}
