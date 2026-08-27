namespace EventLoggerPlugin;

internal sealed record EventLoggerScenarioDisplayPart(
    int SingleModeCharaId,
    int TargetTurn,
    EventLoggerDisplaySnapshot Snapshot,
    IReadOnlyList<InheritGain> InheritGains,
    IReadOnlyList<string> CardEventLines)
{
    internal static EventLoggerScenarioDisplayPart Capture(
        int scenario,
        int singleModeCharaId,
        int targetTurn)
    {
        var current = EventLoggerDisplaySource.Current;
        var snapshot = current with
        {
            InheritStats = Array.AsReadOnly(current.InheritStats.ToArray()),
        };
        return new(
            singleModeCharaId,
            targetTurn,
            snapshot,
            EventLogger.CaptureInheritGains(),
            Array.AsReadOnly(EventLogger.PrintCardEventPerf(scenario).ToArray()));
    }
}
