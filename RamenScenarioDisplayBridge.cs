using RamenScenarioAnalyzer;

namespace EventLoggerPlugin;

internal static class RamenScenarioDisplayBridge
{
    internal static IDisposable Register()
        => RamenTrainingDisplay.RegisterPartProducer();

    internal static void Update(
        IDisposable registration,
        EventLoggerScenarioDisplayPart part)
    {
        var producer = (RamenTrainingDisplayPartProducer)registration;
        producer.Update(
            new(part.SingleModeCharaId, part.TargetTurn),
            (_, display) => Apply(part, display));
    }

    static void Apply(EventLoggerScenarioDisplayPart part, RamenTrainingDisplayEditor display)
    {
        var snapshot = part.Snapshot;

        foreach (var line in part.CardEventLines)
            if (line.Length != 0)
                display.Important.AddStyled(new RamenDisplaySegment(line, RamenDisplayColor.Yellow));

        if (snapshot.CurrentTurn == part.TargetTurn && snapshot.TrainingFailures is { } failures)
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
