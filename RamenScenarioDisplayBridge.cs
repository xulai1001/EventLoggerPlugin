using RamenScenarioAnalyzer;

namespace EventLoggerPlugin;

internal static class RamenScenarioDisplayBridge
{
    internal static IDisposable Register()
        => RamenTrainingDisplay.RegisterModifier(Apply);

    internal static void Refresh() => RamenTrainingDisplay.RefreshCurrent(switchToWorkspace: false);

    static void Apply(RamenTrainingDisplayContext context, RamenTrainingDisplayEditor display)
    {
        var snapshot = EventLoggerDisplaySource.Current;
        if (snapshot.CurrentScenario != (int)ScenarioType.Ramen)
            return;

        foreach (var line in EventLogger.PrintCardEventPerf((int)ScenarioType.Ramen))
            if (line.Length != 0)
                display.Important.AddStyled(new RamenDisplaySegment(line, RamenDisplayColor.Yellow));

        if (snapshot.CurrentTurn == context.Turn.Turn && snapshot.TrainingFailures is { } failures)
            display.Important.AddStyled(
                new RamenDisplaySegment("训练赌博: "),
                new RamenDisplaySegment(failures.GambleTimes.ToString(), RamenDisplayColor.Yellow),
                new RamenDisplaySegment("次, 失败"),
                new RamenDisplaySegment(failures.FailureTimes.ToString(), RamenDisplayColor.Yellow),
                new RamenDisplaySegment("次, 总失败率"),
                new RamenDisplaySegment(failures.TotalFailureRate.ToString(), RamenDisplayColor.Yellow),
                new RamenDisplaySegment("%"));

        if (snapshot.EventCount > 0)
            display.Extra.AddStyled(
                new RamenDisplaySegment("事件数: "),
                new RamenDisplaySegment(snapshot.EventCount.ToString(), RamenDisplayColor.Yellow));
        if (snapshot.SuccessEvents.Appeared > 0)
            display.Extra.AddStyled(
                new RamenDisplaySegment("赌狗事件: "),
                new RamenDisplaySegment(snapshot.SuccessEvents.Appeared.ToString(), RamenDisplayColor.Yellow),
                new RamenDisplaySegment("次, 选择"),
                new RamenDisplaySegment(snapshot.SuccessEvents.Selected.ToString(), RamenDisplayColor.Yellow),
                new RamenDisplaySegment("次, 成功"),
                new RamenDisplaySegment(snapshot.SuccessEvents.Succeeded.ToString(), RamenDisplayColor.Yellow),
                new RamenDisplaySegment("次"));
        if (snapshot.InheritStats.Count > 0)
            display.Extra.AddStyled(
                new RamenDisplaySegment("继承属性: "),
                new RamenDisplaySegment(string.Join('+', snapshot.InheritStats), RamenDisplayColor.Cyan));
        if (snapshot.RaceWinCount > 0)
            display.Extra.AddStyled(
                new RamenDisplaySegment("胜场: "),
                new RamenDisplaySegment(snapshot.RaceWinCount.ToString(), RamenDisplayColor.Yellow));
    }
}
