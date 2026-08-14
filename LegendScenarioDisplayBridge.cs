using LegendScenarioAnalyzer;

namespace EventLoggerPlugin;

internal static class LegendScenarioDisplayBridge
{
    internal static IDisposable Register()
        => LegendTrainingDisplay.RegisterModifier(Apply);

    internal static void Refresh() => LegendTrainingDisplay.RefreshCurrent(switchToWorkspace: false);

    static void Apply(LegendTrainingDisplayContext context, LegendTrainingDisplayEditor display)
    {
        var snapshot = EventLoggerDisplaySource.Current;
        if (snapshot.CurrentScenario != (int)ScenarioType.Legend)
            return;

        foreach (var line in EventLogger.PrintCardEventPerf((int)ScenarioType.Legend))
            if (line.Length != 0)
                display.Important.AddStyled(new LegendDisplaySegment(line, LegendDisplayColor.Yellow));

        if (snapshot.CurrentTurn == context.Turn.Turn && snapshot.TrainingFailures is { } failures)
        {
            display.Important.AddStyled(
                new LegendDisplaySegment("训练赌博: "),
                new LegendDisplaySegment(failures.GambleTimes.ToString(), LegendDisplayColor.Yellow),
                new LegendDisplaySegment("次, 失败"),
                new LegendDisplaySegment(failures.FailureTimes.ToString(), LegendDisplayColor.Yellow),
                new LegendDisplaySegment("次, 总失败率"),
                new LegendDisplaySegment(failures.TotalFailureRate.ToString(), LegendDisplayColor.Yellow),
                new LegendDisplaySegment("%"));
        }

        AddExtraRows(snapshot, display.Extra, context.Turn.Turn);
    }

    static void AddExtraRows(
        EventLoggerDisplaySnapshot snapshot,
        LegendDisplayRowsEditor rows,
        int displayedTurn)
    {
        if (snapshot.EventCount > 0)
            rows.AddStyled(
                new LegendDisplaySegment("事件数: "),
                new LegendDisplaySegment(snapshot.EventCount.ToString(), LegendDisplayColor.Yellow));
        if (snapshot.SuccessEvents.Appeared > 0)
            rows.AddStyled(
                new LegendDisplaySegment("赌狗事件: "),
                new LegendDisplaySegment(snapshot.SuccessEvents.Appeared.ToString(), LegendDisplayColor.Yellow),
                new LegendDisplaySegment("次, 选择"),
                new LegendDisplaySegment(snapshot.SuccessEvents.Selected.ToString(), LegendDisplayColor.Yellow),
                new LegendDisplaySegment("次, 成功"),
                new LegendDisplaySegment(snapshot.SuccessEvents.Succeeded.ToString(), LegendDisplayColor.Yellow),
                new LegendDisplaySegment("次"));
        if (snapshot.CurrentTurn == displayedTurn && snapshot.ScenarioFriend is { } friend)
            rows.AddStyled(
                new LegendDisplaySegment($"{friend.Label}: 点击"),
                new LegendDisplaySegment(friend.ClickedTimes.ToString(), LegendDisplayColor.Aqua),
                new LegendDisplaySegment("次, 启动"),
                new LegendDisplaySegment(friend.ActivatedTimes.ToString(), LegendDisplayColor.Aqua),
                new LegendDisplaySegment("次"));
        if (snapshot.InheritStats.Count > 0)
            rows.AddStyled(
                new LegendDisplaySegment("继承属性: "),
                new LegendDisplaySegment(string.Join('+', snapshot.InheritStats), LegendDisplayColor.Cyan));
        if (snapshot.RaceWinCount > 0)
            rows.AddStyled(
                new LegendDisplaySegment("胜场: "),
                new LegendDisplaySegment(snapshot.RaceWinCount.ToString(), LegendDisplayColor.Yellow));
    }
}
