using UmamusumeResponseAnalyzer.TerminalGui;
using UmamusumeResponseAnalyzer.Plugin;

namespace EventLoggerPlugin;

static class EventLoggerDisplay
{
    static readonly object gate = new();
    static readonly HashSet<string> panelKeys = [];
    static Workspace? workspace;
    static bool accepting;
    static int inFlight;
    static bool cleanupRunning;

    public static void Initialize(IPluginContext _)
    {
        bool initialized;
        lock (gate)
        {
            initialized = !accepting &&
                inFlight == 0 &&
                !cleanupRunning &&
                workspace is null &&
                panelKeys.Count == 0;
            if (initialized)
                accepting = true;
        }
        if (!initialized)
            throw new InvalidOperationException("EventLoggerPlugin 上一个 lifecycle 尚未停止完成。");
    }

    public static void Dispose()
    {
        Workspace currentWorkspace;
        string[] publishedKeys;
        lock (gate)
        {
            accepting = false;
            while (inFlight != 0 || cleanupRunning)
                Monitor.Wait(gate);

            if (panelKeys.Count == 0)
            {
                workspace = null;
                return;
            }

            currentWorkspace = workspace!;
            publishedKeys = [.. panelKeys];
            cleanupRunning = true;
        }

        try
        {
            foreach (var key in publishedKeys)
            {
                currentWorkspace.RemovePanel(key);
                lock (gate)
                    panelKeys.Remove(key);
            }
        }
        finally
        {
            lock (gate)
            {
                if (panelKeys.Count == 0)
                    workspace = null;
                cleanupRunning = false;
                Monitor.PulseAll(gate);
            }
        }
    }

    public static void Log(string text, UiSeverity severity = UiSeverity.Info)
    {
        EnterOperation();
        try
        {
            GetWorkspace().Log(text, severity);
        }
        finally
        {
            ExitOperation();
        }
    }

    public static void Notify(string text, UiSeverity severity = UiSeverity.Info, TimeSpan? ttl = null)
    {
        EnterOperation();
        try
        {
            GetWorkspace().Notify(text, severity, ttl);
        }
        finally
        {
            ExitOperation();
        }
    }

    public static void SetPanel(
        string key,
        string title,
        WorkspaceContent content,
        bool fullBleed = false,
        bool switchToWorkspace = true)
    {
        EnterOperation();
        try
        {
            GetWorkspace().SetPanel(key, title, content, fullBleed, switchToWorkspace);
            lock (gate)
                panelKeys.Add(key);
        }
        finally
        {
            ExitOperation();
        }
    }

    static void EnterOperation()
    {
        bool admitted;
        lock (gate)
        {
            admitted = accepting;
            if (admitted)
                inFlight++;
        }
        if (!admitted)
            throw new InvalidOperationException("EventLoggerPlugin 尚未初始化或已停止。");
    }

    static void ExitOperation()
    {
        lock (gate)
        {
            if (--inFlight == 0)
                Monitor.PulseAll(gate);
        }
    }

    static Workspace GetWorkspace()
    {
        lock (gate)
        {
            if (workspace is { } currentWorkspace)
                return currentWorkspace;
        }

        var createdWorkspace = Workspace.Create("事件记录");
        Workspace stableWorkspace;
        bool canonical;
        lock (gate)
        {
            stableWorkspace = workspace ??= createdWorkspace;
            canonical = ReferenceEquals(stableWorkspace, createdWorkspace);
        }
        if (!canonical)
            throw new InvalidOperationException("Host 未为同名 Workspace 返回 canonical handle。");
        return stableWorkspace;
    }
}
