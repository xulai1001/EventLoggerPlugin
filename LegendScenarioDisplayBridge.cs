using LegendScenarioAnalyzer;

namespace EventLoggerPlugin;

internal static class LegendScenarioDisplayBridge
{
    internal static IDisposable Register()
        => LegendTrainingDisplay.RegisterPartProducer("EventLogger");

    internal static void Update(
        IDisposable registration,
        EventLoggerScenarioDisplayPart part)
    {
        var producer = (LegendTrainingDisplayPartProducer)registration;
        producer.Update(
            new(part.SingleModeCharaId, part.TargetTurn),
            (_, display) => Apply(part, display));
    }

    static void Apply(EventLoggerScenarioDisplayPart part, LegendTrainingDisplayEditor display)
    {
        var snapshot = part.Snapshot;

        foreach (var line in part.CardEventLines)
            if (line.Length != 0)
                display.Extra.AddStyled(new LegendDisplaySegment(line, LegendDisplayColor.Yellow));

        if (snapshot.CurrentTurn == part.TargetTurn && snapshot.TrainingFailures is { } failures)
        {
            display.Extra.AddStyled(
                new LegendDisplaySegment("训练赌博: "),
                new LegendDisplaySegment(failures.GambleTimes.ToString(), LegendDisplayColor.Yellow),
                new LegendDisplaySegment("次, 失败"),
                new LegendDisplaySegment(failures.FailureTimes.ToString(), LegendDisplayColor.Yellow),
                new LegendDisplaySegment("次, 总失败率"),
                new LegendDisplaySegment(failures.TotalFailureRate.ToString(), LegendDisplayColor.Yellow),
                new LegendDisplaySegment("%"));
        }

        AddExtraRows(snapshot, part.InheritGains, display.Extra, part.TargetTurn);
    }

    static void AddExtraRows(
        EventLoggerDisplaySnapshot snapshot,
        IReadOnlyList<InheritGain> inheritGains,
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
        if (inheritGains.Count > 0)
            rows.AddStyled(
                new LegendDisplaySegment("继承属性: "),
                new LegendDisplaySegment(
                    string.Join('+', inheritGains.Select(gain => gain.Stats)),
                    LegendDisplayColor.Cyan),
                new LegendDisplaySegment(", PT: "),
                new LegendDisplaySegment(
                    string.Join('+', inheritGains.Select(gain => gain.SkillPoints)),
                    LegendDisplayColor.Cyan));
        if (snapshot.RaceWinCount > 0)
            rows.AddStyled(
                new LegendDisplaySegment("胜场: "),
                new LegendDisplaySegment(snapshot.RaceWinCount.ToString(), LegendDisplayColor.Yellow));
    }
}
