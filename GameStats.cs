using System.Collections.Frozen;
using System.Collections.Immutable;
using UmamusumeResponseAnalyzer.TerminalGui;

namespace EventLoggerPlugin;

public enum ScenarioType
{
    GrandMasters = 5,
    LArc = 6,
    UAF = 7,
    Legend = 10,
    Ramen = 14,
}

public sealed class TrainStats
{
    public int[] FiveValueGain = [];
    public int PtGain;
    public int[] FiveValueGainNonScenario = [];
    public int PtGainNonScenario;
    public int VitalGain;
    public int FailureRate;

    public double ScoreAssumeSuccessNoVital() => FiveValueGain.Sum() + PtGain * 0.7;

    public double ScoreNoVital()
    {
        const double scoreAssumeFailed = -100;
        return ScoreAssumeSuccessNoVital() * (1 - 0.01 * FailureRate) +
            0.01 * FailureRate * scoreAssumeFailed;
    }

    public double Score(int currentVital, int maxVital)
    {
        const double scoreAssumeFailed = -100;
        var scoreAssumeSuccess = FiveValueGain.Sum() + PtGain * 0.7;
        return (scoreAssumeSuccess - ScoreUtils.ScoreOfVital(currentVital, maxVital) +
            ScoreUtils.ScoreOfVital(currentVital + VitalGain, maxVital)) *
            (1 - 0.01 * FailureRate) + 0.01 * FailureRate * scoreAssumeFailed;
    }
}

public sealed record TrainStatsSnapshot(
    ImmutableArray<int> FiveValueGain,
    int PtGain,
    ImmutableArray<int> FiveValueGainNonScenario,
    int PtGainNonScenario,
    int VitalGain,
    int FailureRate)
{
    internal static TrainStatsSnapshot From(TrainStats value) => new(
        [.. value.FiveValueGain],
        value.PtGain,
        [.. value.FiveValueGainNonScenario],
        value.PtGainNonScenario,
        value.VitalGain,
        value.FailureRate);

    public TrainStats ToMutable() => new()
    {
        FiveValueGain = [.. FiveValueGain],
        PtGain = PtGain,
        FiveValueGainNonScenario = [.. FiveValueGainNonScenario],
        PtGainNonScenario = PtGainNonScenario,
        VitalGain = VitalGain,
        FailureRate = FailureRate,
    };
}

public sealed class TurnStats
{
    public bool isTraining;
    public int motivation;
    public TrainStats[] fiveTrainStats = new TrainStats[5];
    public int playerChoice = -1;
    public bool isTrainingFailed;
    public int[] trainLevel = [1, 1, 1, 1, 1];
    public int[] trainLevelCount = new int[5];
    public bool[] larc_zuoyueAtTrain = new bool[5];
    public bool larc_playerChoiceSS;
    public int larc_SSPersonCount;
    public bool larc_isSSS;
    public int larc_zuoyueEvent;
    public int larc_totalApproval;
    public int venus_yellowVenusLevel;
    public int venus_redVenusLevel;
    public int venus_blueVenusLevel;
    public bool venus_isEffect102;
    public int venus_venusTrain = -100;
    public bool venus_isVenusCountConcerned = true;
    public bool venus_venusEvent;
    public bool[] uaf_friendAtTrain = new bool[5];
    public int uaf_friendEvent;
    public bool[] cook_friendAtTrain = new bool[5];
    public bool[] legend_friendAtTrain = new bool[5];
    public bool legend_isEffect104;
    public bool legend_friendClickEvent;
    public bool legend_friendClickEventCountConcerned = true;
}

public sealed record TurnStatsSnapshot(
    bool IsTraining,
    int Motivation,
    ImmutableArray<TrainStatsSnapshot?> FiveTrainStats,
    int PlayerChoice,
    bool IsTrainingFailed,
    ImmutableArray<int> TrainLevel,
    ImmutableArray<int> TrainLevelCount,
    ImmutableArray<bool> LArcFriendAtTrain,
    bool LArcPlayerChoiceSs,
    int LArcSsPersonCount,
    bool LArcIsSss,
    int LArcFriendEvent,
    int LArcTotalApproval,
    int VenusYellowLevel,
    int VenusRedLevel,
    int VenusBlueLevel,
    bool VenusIsEffect102,
    int VenusTrain,
    bool VenusCountConcerned,
    bool VenusEvent,
    ImmutableArray<bool> UafFriendAtTrain,
    int UafFriendEvent,
    ImmutableArray<bool> CookFriendAtTrain,
    ImmutableArray<bool> LegendFriendAtTrain,
    bool LegendIsEffect104,
    bool LegendFriendClickEvent,
    bool LegendFriendClickEventCountConcerned)
{
    internal static TurnStatsSnapshot From(TurnStats value) => new(
        value.isTraining,
        value.motivation,
        [.. value.fiveTrainStats.Select(x => x is null ? null : TrainStatsSnapshot.From(x))],
        value.playerChoice,
        value.isTrainingFailed,
        [.. value.trainLevel],
        [.. value.trainLevelCount],
        [.. value.larc_zuoyueAtTrain],
        value.larc_playerChoiceSS,
        value.larc_SSPersonCount,
        value.larc_isSSS,
        value.larc_zuoyueEvent,
        value.larc_totalApproval,
        value.venus_yellowVenusLevel,
        value.venus_redVenusLevel,
        value.venus_blueVenusLevel,
        value.venus_isEffect102,
        value.venus_venusTrain,
        value.venus_isVenusCountConcerned,
        value.venus_venusEvent,
        [.. value.uaf_friendAtTrain],
        value.uaf_friendEvent,
        [.. value.cook_friendAtTrain],
        [.. value.legend_friendAtTrain],
        value.legend_isEffect104,
        value.legend_friendClickEvent,
        value.legend_friendClickEventCountConcerned);

    public TurnStats ToMutable() => new()
    {
        isTraining = IsTraining,
        motivation = Motivation,
        fiveTrainStats = [.. FiveTrainStats.Select(x => x?.ToMutable()!)],
        playerChoice = PlayerChoice,
        isTrainingFailed = IsTrainingFailed,
        trainLevel = [.. TrainLevel],
        trainLevelCount = [.. TrainLevelCount],
        larc_zuoyueAtTrain = [.. LArcFriendAtTrain],
        larc_playerChoiceSS = LArcPlayerChoiceSs,
        larc_SSPersonCount = LArcSsPersonCount,
        larc_isSSS = LArcIsSss,
        larc_zuoyueEvent = LArcFriendEvent,
        larc_totalApproval = LArcTotalApproval,
        venus_yellowVenusLevel = VenusYellowLevel,
        venus_redVenusLevel = VenusRedLevel,
        venus_blueVenusLevel = VenusBlueLevel,
        venus_isEffect102 = VenusIsEffect102,
        venus_venusTrain = VenusTrain,
        venus_isVenusCountConcerned = VenusCountConcerned,
        venus_venusEvent = VenusEvent,
        uaf_friendAtTrain = [.. UafFriendAtTrain],
        uaf_friendEvent = UafFriendEvent,
        cook_friendAtTrain = [.. CookFriendAtTrain],
        legend_friendAtTrain = [.. LegendFriendAtTrain],
        legend_isEffect104 = LegendIsEffect104,
        legend_friendClickEvent = LegendFriendClickEvent,
        legend_friendClickEventCountConcerned = LegendFriendClickEventCountConcerned,
    };
}

public sealed record EventLoggerRoundSnapshot(
    bool IsFullGame,
    int Scenario,
    int CurrentTurn,
    ImmutableArray<TurnStatsSnapshot?> Turns,
    int MotivationDropCount,
    int FullSsCount,
    int SssCount,
    int ConsecutiveNonSssCount,
    FrozenDictionary<int, int> SsRivalsSpecialBuffs,
    bool IsStarted,
    int InitTurn,
    int CurrentScenario,
    ImmutableArray<LogEventSnapshot> CardEvents,
    ImmutableArray<LogEventSnapshot> AllEvents,
    int CardEventCount,
    int CardEventFinishCount,
    int CardEventFinishTurn,
    int CardEventRemaining,
    int SuccessEventCount,
    int SuccessEventSelectCount,
    int SuccessEventSuccessCount,
    ImmutableArray<int> InheritStats,
    ImmutableArray<int> RaceHistory,
    int VitalSpent,
    int LastVital,
    bool IsCapturingVital)
{
    public static EventLoggerRoundSnapshot Empty { get; } = new(
        false,
        0,
        0,
        Enumerable.Repeat<TurnStatsSnapshot?>(null, 79).ToImmutableArray(),
        0,
        0,
        0,
        0,
        new Dictionary<int, int>().ToFrozenDictionary(),
        false,
        0,
        0,
        [],
        [],
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        [],
        [],
        0,
        0,
        false);

    public TurnStats NewTurnBuilder(int turn)
        => turn >= 0 && turn < Turns.Length && Turns[turn] is { } value
            ? value.ToMutable()
            : new TurnStats();
}

internal static class GameStats
{
    internal static bool IsFullGame;
    internal static int Scenario;
    internal static int CurrentTurn;
    internal static TurnStats?[] Turns = new TurnStats?[79];
    internal static int MotivationDropCount;
    internal static int FullSsCount;
    internal static int SssCount;
    internal static int ConsecutiveNonSssCount;
    internal static Dictionary<int, int> SsRivalsSpecialBuffs = [];

    internal static void Reset(bool isFullGame)
    {
        IsFullGame = isFullGame;
        Scenario = 0;
        CurrentTurn = 0;
        Turns = new TurnStats?[79];
        MotivationDropCount = 0;
        FullSsCount = 0;
        SssCount = 0;
        ConsecutiveNonSssCount = 0;
        SsRivalsSpecialBuffs = [];
    }

    internal static void BeginTurn(int scenario, int turn)
    {
        Scenario = scenario;
        CurrentTurn = turn;
        Turns[turn] = new TurnStats();
    }

    internal static void CommitTurn(
        int scenario,
        int turn,
        TurnStats value,
        IReadOnlyDictionary<int, int>? specialBuffs)
    {
        Scenario = scenario;
        CurrentTurn = turn;
        Turns[turn] = TurnStatsSnapshot.From(value).ToMutable();
        if (specialBuffs is not null)
            SsRivalsSpecialBuffs = specialBuffs.ToDictionary();
        RefreshSummary();
    }

    internal static void RefreshSummary()
    {
        MotivationDropCount = 0;
        for (var i = CurrentTurn; i >= 2; i--)
        {
            if (Turns[i] is not { } current || Turns[i - 1] is not { } previous)
                break;
            if (current.motivation < previous.motivation)
                MotivationDropCount += previous.motivation - current.motivation;
        }

        if (Scenario == (int)ScenarioType.GrandMasters)
            PublishGrandMastersSummary();

        if (Scenario != (int)ScenarioType.LArc)
            return;

        FullSsCount = 0;
        SssCount = 0;
        ConsecutiveNonSssCount = 0;
        for (var i = CurrentTurn; i >= 1; i--)
        {
            if (Turns[i] is not { } stats)
                break;
            if (!stats.larc_playerChoiceSS)
                continue;

            FullSsCount++;
            if (stats.larc_isSSS)
                SssCount++;
            if (SssCount == 0)
                ConsecutiveNonSssCount += stats.larc_SSPersonCount;
        }
    }

    static void PublishGrandMastersSummary()
    {
        var fiveGain = new int[5];
        var ptGain = 0;
        var fiveGainSpirit = new int[5];
        var ptGainSpirit = 0;
        for (var turn = CurrentTurn - 1; turn >= 1; turn--)
        {
            if (Turns[turn] is not { } stats)
                break;
            if (!GameGlobal.ToTrainIndex.TryGetValue(stats.playerChoice, out var trainIndex) ||
                stats.isTrainingFailed ||
                stats.fiveTrainStats.ElementAtOrDefault(trainIndex) is not { } trainStat ||
                trainStat.FiveValueGain.Length == 0)
                continue;

            double[] venusLevelBonus = [0, 0.05, 0.08, 0.11, 0.13, 0.15];
            var venusBonus = venusLevelBonus[stats.venus_blueVenusLevel] +
                venusLevelBonus[stats.venus_yellowVenusLevel] +
                venusLevelBonus[stats.venus_redVenusLevel];
            for (var i = 0; i < 5; i++)
            {
                var gain = trainStat.FiveValueGain[i];
                var gainNonSpirit = (int)(trainStat.FiveValueGainNonScenario[i] * (1 + venusBonus));
                fiveGain[i] += gain;
                fiveGainSpirit[i] += gain - gainNonSpirit;
            }

            var ptGainNonSpirit = (int)(trainStat.PtGainNonScenario * (1 + venusBonus));
            ptGain += trainStat.PtGain;
            ptGainSpirit += trainStat.PtGain - ptGainNonSpirit;
        }

        var lines = new[]
        {
            "          速    耐    力    根    智    总    pt",
            $"总训练    {string.Join("    ", fiveGain)}    {fiveGain.Sum()}    {ptGain}",
            $"碎片加成  {string.Join("    ", fiveGainSpirit)}    {fiveGainSpirit.Sum()}    {ptGainSpirit}",
        };
        EventLoggerDisplay.SetPanel(
            "training-summary",
            "训练收益",
            WorkspaceContent.Text(string.Join(Environment.NewLine, lines)));
    }
}
