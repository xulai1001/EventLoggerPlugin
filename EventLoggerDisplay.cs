using System.Collections.Concurrent;
using UmamusumeResponseAnalyzer.Plugin;
using UmamusumeResponseAnalyzer.TerminalGui;

namespace EventLoggerPlugin;

static class EventLoggerDisplay
{
    static readonly ConcurrentDictionary<string, byte> PanelKeys = [];
    static Workspace? workspace;

    public static void Initialize(IPluginContext _) { }

    public static void Dispose()
    {
        var currentWorkspace = Interlocked.Exchange(ref workspace, null);
        if (currentWorkspace is null)
            return;

        foreach (var key in PanelKeys.Keys)
            currentWorkspace.RemovePanel(key);
        PanelKeys.Clear();
    }

    public static void Notify(string text, UiSeverity severity = UiSeverity.Info, TimeSpan? ttl = null)
        => GetWorkspace().Notify(text, severity, ttl);

    public static void SetPanel(
        string key,
        string title,
        WorkspaceContent content,
        bool fullBleed = false,
        bool switchToWorkspace = true)
    {
        GetWorkspace().SetPanel(key, title, content, fullBleed, switchToWorkspace);
        PanelKeys.TryAdd(key, 0);
    }

    static Workspace GetWorkspace()
    {
        if (Volatile.Read(ref workspace) is { } current)
            return current;

        var created = Workspace.Create("事件记录");
        return Interlocked.CompareExchange(ref workspace, created, null) ?? created;
    }
}
