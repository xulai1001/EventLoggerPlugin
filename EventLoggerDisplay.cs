using UmamusumeResponseAnalyzer.LiveDisplay;
using UmamusumeResponseAnalyzer.Plugin;

namespace EventLoggerPlugin;

static class EventLoggerDisplay
{
    static ILiveDisplayOutput? liveDisplay;
    static LiveDisplayWorkspace? workspace;

    public static void Initialize(IPluginContext context)
    {
        liveDisplay = context.LiveDisplay;
        workspace = liveDisplay.CreateWorkspace("事件记录");
    }

    public static void Dispose()
    {
        var display = liveDisplay;
        var currentWorkspace = workspace;
        liveDisplay = null;
        workspace = null;

        if (display is not null && currentWorkspace is not null)
            display.RemoveWorkspace(currentWorkspace);
    }

    public static void Log(string text, LiveDisplaySeverity severity = LiveDisplaySeverity.Info)
    {
        LiveDisplay.Log(Workspace, text, severity);
    }

    public static void Notify(string text, LiveDisplaySeverity severity = LiveDisplaySeverity.Info, TimeSpan? ttl = null)
    {
        LiveDisplay.Notify(Workspace, text, severity, ttl);
    }

    public static void SetPanel(
        string key,
        string title,
        LiveDisplayContent content,
        bool fullBleed = false,
        bool switchToWorkspace = true)
    {
        LiveDisplay.SetPanel(Workspace, key, title, content, fullBleed, switchToWorkspace);
    }

    static ILiveDisplayOutput LiveDisplay => liveDisplay
        ?? throw new InvalidOperationException("EventLoggerPlugin 尚未初始化 LiveDisplay。");

    static LiveDisplayWorkspace Workspace => workspace
        ?? throw new InvalidOperationException("EventLoggerPlugin 尚未创建 LiveDisplay workspace。");
}
