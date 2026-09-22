namespace EventLoggerPlugin;

public sealed record EventLoggerDisplaySnapshot(
    bool IsStarted,
    int CurrentTurn,
    int CurrentScenario,
    int EventCount,
    EventLoggerCardEventSummary CardEvents,
    EventLoggerSuccessEventSummary SuccessEvents,
    EventLoggerTrainingFailureSummary? TrainingFailures,
    EventLoggerScenarioFriendSummary? ScenarioFriend,
    IReadOnlyList<int> InheritStats,
    int RaceWinCount)
{
    public static EventLoggerDisplaySnapshot Empty { get; } = new(
        false,
        0,
        0,
        0,
        EventLoggerCardEventSummary.Empty,
        EventLoggerSuccessEventSummary.Empty,
        null,
        null,
        [],
        0);

    public bool HasData =>
        IsStarted ||
        CurrentTurn > 0 ||
        EventCount > 0 ||
        CardEvents.HasData ||
        SuccessEvents.HasData ||
        TrainingFailures is not null ||
        ScenarioFriend is not null ||
        InheritStats.Count > 0 ||
        RaceWinCount > 0;
}

public sealed record EventLoggerCardEventSummary(
    int Appeared,
    int Finished,
    int Remaining,
    int FinishedTurn,
    IReadOnlyDictionary<int, int> AppearedByCard)
{
    public static EventLoggerCardEventSummary Empty { get; } = new(0, 0, 0, 0, new Dictionary<int, int>());
    public bool HasData => Appeared > 0 || Finished > 0 || Remaining > 0 || FinishedTurn > 0 || AppearedByCard.Count > 0;
}

public sealed record EventLoggerSuccessEventSummary(int Appeared, int Selected, int Succeeded)
{
    public static EventLoggerSuccessEventSummary Empty { get; } = new(0, 0, 0);
    public bool HasData => Appeared > 0 || Selected > 0 || Succeeded > 0;
}

public sealed record EventLoggerTrainingFailureSummary(
    int GambleTimes,
    int FailureTimes,
    int TotalFailureRate);

public sealed record EventLoggerScenarioFriendSummary(
    string Label,
    int ClickedTimes,
    int ActivatedTimes);

public static class EventLoggerDisplaySource
{
    public static EventLoggerDisplaySnapshot Current => Capture(EventLogger.Current);

    static EventLoggerDisplaySnapshot Capture(EventLoggerRoundSnapshot current)
    {
        if (!current.IsStarted && current.CurrentTurn <= 0 && current.AllEvents.Length == 0)
            return EventLoggerDisplaySnapshot.Empty;

        return new(
            current.IsStarted,
            current.CurrentTurn,
            current.CurrentScenario,
            current.AllEvents.Length,
            new(
                current.CardEventCount,
                current.CardEventFinishCount,
                current.CardEventRemaining,
                current.CardEventFinishTurn,
                current.CardEventCountByCard),
            new(
                current.SuccessEventCount,
                current.SuccessEventSelectCount,
                current.SuccessEventSuccessCount),
            CaptureTrainingFailures(current),
            CaptureScenarioFriend(current),
            current.InheritStats,
            current.RaceHistory.Length);
    }

    static EventLoggerTrainingFailureSummary? CaptureTrainingFailures(EventLoggerRoundSnapshot current)
    {
        var gambleTimes = 0;
        var failureTimes = 0;
        var totalFailureRate = 0;
        for (var turn = LastPastTurnIndex(current); turn >= 1; turn--)
        {
            if (current.Turns[turn] is not { } stats)
                break;
            if (!TryGetTrainStat(stats, out var trainStat))
                continue;
            if (stats.IsTrainingFailed)
                failureTimes++;
            if (trainStat.FailureRate <= 0)
                continue;
            gambleTimes++;
            totalFailureRate += trainStat.FailureRate;
        }

        return gambleTimes == 0 && failureTimes == 0
            ? null
            : new(gambleTimes, failureTimes, totalFailureRate);
    }

    static EventLoggerScenarioFriendSummary? CaptureScenarioFriend(EventLoggerRoundSnapshot current)
        => current.Scenario switch
        {
            (int)ScenarioType.LArc => CaptureFriend(current, "佐岳", static (stats, index) =>
                stats.LArcFriendAtTrain[index] && stats.LArcFriendEvent != 5,
                static stats => stats.LArcFriendEvent is 1 or 2 or 4),
            (int)ScenarioType.UAF => CaptureFriend(current, "凉花", static (stats, index) =>
                stats.UafFriendAtTrain[index] && stats.UafFriendEvent != 5,
                static stats => stats.UafFriendEvent is 1 or 2),
            (int)ScenarioType.Legend => CaptureFriend(current, "团卡", static (stats, index) =>
                stats.LegendFriendAtTrain[index] && stats.LegendFriendClickEventCountConcerned,
                static stats => stats.LegendFriendClickEvent),
            _ => null,
        };

    static EventLoggerScenarioFriendSummary CaptureFriend(
        EventLoggerRoundSnapshot current,
        string label,
        Func<TurnStatsSnapshot, int, bool> wasClicked,
        Func<TurnStatsSnapshot, bool> wasActivated)
    {
        var clickedTimes = 0;
        var activatedTimes = 0;
        foreach (var stats in CompletedTrainingTurns(current))
        {
            if (!TryGetTrainIndex(stats, out var trainIndex) || !wasClicked(stats, trainIndex))
                continue;
            clickedTimes++;
            if (wasActivated(stats))
                activatedTimes++;
        }
        return new(label, clickedTimes, activatedTimes);
    }

    static IEnumerable<TurnStatsSnapshot> CompletedTrainingTurns(EventLoggerRoundSnapshot current)
    {
        for (var turn = Math.Min(Math.Max(current.CurrentTurn, 0), current.Turns.Length - 1); turn >= 1; turn--)
        {
            if (current.Turns[turn] is not { } stats)
                yield break;
            if (stats.IsTrainingFailed || !TryGetTrainIndex(stats, out _))
                continue;
            yield return stats;
        }
    }

    static bool TryGetTrainIndex(TurnStatsSnapshot stats, out int trainIndex)
        => GameGlobal.ToTrainIndex.TryGetValue(stats.PlayerChoice, out trainIndex);

    static bool TryGetTrainStat(TurnStatsSnapshot stats, out TrainStatsSnapshot trainStat)
    {
        trainStat = null!;
        if (!TryGetTrainIndex(stats, out var trainIndex) ||
            trainIndex < 0 ||
            trainIndex >= stats.FiveTrainStats.Length ||
            stats.FiveTrainStats[trainIndex] is not { } value)
            return false;
        trainStat = value;
        return true;
    }

    static int LastPastTurnIndex(EventLoggerRoundSnapshot current)
        => Math.Min(Math.Max(current.CurrentTurn - 1, 0), current.Turns.Length - 1);
}
