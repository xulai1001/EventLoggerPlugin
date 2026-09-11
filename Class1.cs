using Gallop;
using System.Runtime.CompilerServices;
using UmamusumeResponseAnalyzer.Plugin;
using UmamusumeResponseAnalyzer.TerminalGui;

namespace EventLoggerPlugin;

public sealed class EventLoggerPlugin : IPlugin
{
    static string DataDirectory => Path.Combine("PluginData", "EventLoggerPlugin");
    readonly object scenarioGate = new();
    IDisposable? ramenPartProducer;
    int scenarioCharaId;
    int pendingTrainingTurn = -1;

    public void Initialize(IPluginContext context)
    {
        const int SuccessionDisplayPriority = 3;

        EventLogger.ConfigureDataDirectory(DataDirectory);
        Directory.CreateDirectory(DataDirectory);
        EventLoggerDisplay.Initialize(context);
        if (context.IsPluginAvailable("RamenScenarioAnalyzer"))
            ramenPartProducer = RegisterRamenPartProducer();

        context.Analyzers.Register<SingleModeCheckEventResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/check_event")],
            invocation => AnalyzeCheckEvent(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModeExecCommandResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/exec_command")],
            invocation => AnalyzeExecCommand(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModeRaceEndResponse>(
            AnalyzerKind.Response,
            [
                EndpointPattern.Wildcard("/umamusume/single_mode*/race_end"),
                EndpointPattern.Regex(
                    "^/umamusume/single_mode_(?:arc/arc_race_end|legend/legend_race_end|venus/venus_race_end)$"),
            ],
            invocation => AnalyzeRaceEnd(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModeLoadResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/load")],
            invocation => AnalyzeLoad(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModeTeamTeamRaceEndOutResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Regex("^/umamusume/single_mode_team/team_race_end(?:_out)?$")],
            invocation => AnalyzeTeamRaceEnd(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModeExecCommandRequest>(
            AnalyzerKind.Request,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/exec_command")],
            invocation => ParseTrainingRequest(invocation.Payload),
            priority: -1);
        context.Analyzers.Register<SingleModeCheckEventResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/check_event")],
            invocation => SuccessionChoiceAnalyzer.Analyze(invocation.Payload),
            priority: SuccessionDisplayPriority);
        context.Analyzers.Register<SingleModeExecCommandResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/exec_command")],
            invocation => SuccessionChoiceAnalyzer.Analyze(invocation.Payload),
            priority: SuccessionDisplayPriority);
        context.Analyzers.Register<SingleModeLoadResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Wildcard("/umamusume/single_mode*/load")],
            invocation => SuccessionChoiceAnalyzer.Analyze(invocation.Payload),
            priority: SuccessionDisplayPriority);
    }

    public void Dispose()
    {
        try
        {
            ramenPartProducer?.Dispose();
        }
        finally
        {
            EventLoggerDisplay.Dispose();
        }
    }

    ValueTask AnalyzeCheckEvent(SingleModeCheckEventResponse response)
        => AnalyzeResponse(new(
            response.data?.chara_info,
            response.data?.unchecked_event_array,
            response.data?.select_index_info_array,
            response.data?.home_info));

    ValueTask AnalyzeExecCommand(SingleModeExecCommandResponse response)
        => AnalyzeResponse(
            new(
                response.data?.chara_info,
                response.data?.unchecked_event_array,
                null,
                response.data?.home_info),
            response.data?.command_result);

    ValueTask AnalyzeLoad(SingleModeLoadResponse response)
    {
        var common = response.data?.single_mode_load_common;
        return common is null
            ? ValueTask.CompletedTask
            : AnalyzeResponse(
                new(
                    common.chara_info,
                    common.unchecked_event_array,
                    null,
                    common.home_info),
                raceHistory: common.race_history);
    }

    ValueTask AnalyzeRaceEnd(SingleModeRaceEndResponse response)
        => AnalyzeResponse(default, raceHistory: response.data?.race_history);

    ValueTask AnalyzeTeamRaceEnd(SingleModeTeamTeamRaceEndOutResponse response)
        => AnalyzeResponse(new(
            response.data?.chara_info,
            response.data?.unchecked_event_array,
            null));

    ValueTask AnalyzeResponse(
        EventLoggerSnapshot snapshot,
        SingleModeCommandResult? commandResult = null,
        SingleRaceHistory[]? raceHistory = null)
    {
        var sessionReset = PrepareScenarioSession(snapshot);
        var trainingTurn = commandResult is null
            ? -1
            : Interlocked.Exchange(ref pendingTrainingTurn, -1);
        if (commandResult is not null && !sessionReset)
        {
            AnalyzeCommandResult(commandResult, trainingTurn);
        }

        if (commandResult is not null)
        {
            if (EventLogger.Current.IsCapturingVital)
                EventLogger.UpdateWhileCapturing(snapshot);
            else
                EventLogger.Start(snapshot);
        }

        if (snapshot.UncheckedEvents is not null)
        {
            EventLogger.Update(snapshot);
            EnsureScenarioTurn(snapshot);
            EventLogger.RecordScenarioEvents(snapshot.UncheckedEvents);
        }

        if (raceHistory is not null)
            EventLogger.UpdateRaceHistory(raceHistory);

        TrackScenarioTurn(snapshot);
        UpdateScenarioDisplay(snapshot);

        return ValueTask.CompletedTask;
    }

    static void AnalyzeCommandResult(SingleModeCommandResult commandResult, int trainingTurn)
    {
        if (commandResult.result_state != 1)
            return;

        EventLoggerDisplay.Notify("训练失败！", UiSeverity.Warning);
        if (trainingTurn >= 0)
            EventLogger.MarkTrainingFailed(trainingTurn);
    }

    ValueTask ParseTrainingRequest(SingleModeExecCommandRequest request)
    {
        var common = request.single_mode_exec_command_request_common;
        if (common.command_type != 1)
            return ValueTask.CompletedTask;

        var turn = common.current_turn;
        Interlocked.Exchange(ref pendingTrainingTurn, turn);
        EventLogger.RecordPlayerChoice(turn, GameGlobal.ToTrainId[common.command_id]);
        return ValueTask.CompletedTask;
    }

    bool PrepareScenarioSession(EventLoggerSnapshot snapshot)
    {
        if (snapshot.CharaInfo is not { } chara ||
            chara.scenario_id is not ((int)ScenarioType.Legend or (int)ScenarioType.Ramen))
            return false;

        lock (scenarioGate)
        {
            var round = EventLogger.Current;
            var reset = scenarioCharaId != chara.single_mode_chara_id ||
                round.Scenario != chara.scenario_id ||
                chara.turn == 1 && round.CurrentTurn != 1 ||
                chara.turn != 1 &&
                round.CurrentTurn != chara.turn - 1 &&
                round.CurrentTurn != chara.turn;
            if (!reset)
                return false;

            scenarioCharaId = chara.single_mode_chara_id;
            EventLogger.ResetAndStartSession(snapshot, isFullGame: chara.turn == 1);
            return true;
        }
    }

    void TrackScenarioTurn(EventLoggerSnapshot snapshot)
    {
        if (snapshot is not { CharaInfo: { } chara, HomeInfo: { } homeInfo } ||
            chara.scenario_id is not ((int)ScenarioType.Legend or (int)ScenarioType.Ramen))
            return;

        lock (scenarioGate)
        {
            var round = EventLogger.Current;
            if (round.CurrentTurn != chara.turn)
            {
                EventLogger.EnsureScenarioTurn(chara.scenario_id, chara.turn);
                round = EventLogger.Current;
            }

            var stats = round.NewTurnBuilder(chara.turn);
            stats.isTraining = chara.playing_state == 1;
            stats.motivation = chara.motivation;
            stats.fiveTrainStats = CreateTrainingStats(chara, homeInfo);
            stats.trainLevel = CreateTrainingLevels(chara);
            if (chara.scenario_id == (int)ScenarioType.Legend)
                UpdateLegendState(stats, chara, homeInfo);
            EventLogger.CommitScenarioTurn(chara.scenario_id, chara.turn, stats);
        }
    }

    static void EnsureScenarioTurn(EventLoggerSnapshot snapshot)
    {
        if (snapshot.CharaInfo is { } chara &&
            chara.scenario_id is (int)ScenarioType.Legend or (int)ScenarioType.Ramen)
            EventLogger.EnsureScenarioTurn(chara.scenario_id, chara.turn);
    }

    static TrainStats[] CreateTrainingStats(SingleModeChara chara, SingleModeHomeInfo homeInfo)
    {
        var currentStats = new[] { chara.speed, chara.stamina, chara.power, chara.guts, chara.wiz };
        var result = new TrainStats[GameGlobal.TrainIds.Length];
        for (var i = 0; i < GameGlobal.TrainIds.Length; i++)
        {
            var trainId = GameGlobal.TrainIds[i];
            var commands = homeInfo.command_info_array
                .Where(command => GameGlobal.ToTrainId.TryGetValue(command.command_id, out var id) && id == trainId)
                .ToArray();
            var values = new int[31];
            foreach (var command in commands)
                foreach (var parameter in command.params_inc_dec_info_array ?? [])
                    if (parameter.target_type is >= 1 and <= 5 or 10 or 30)
                        values[parameter.target_type] += parameter.value;

            var vitalGain = Math.Clamp(values[10], -chara.vital, chara.max_vital - chara.vital);
            var gains = Enumerable.Range(0, 5)
                .Select(index =>
                    ScoreUtils.ReviseOver1200(currentStats[index] + values[index + 1]) -
                    ScoreUtils.ReviseOver1200(currentStats[index]))
                .ToArray();
            var failureRate = commands
                .FirstOrDefault(command => command.command_id == trainId)?.failure_rate
                ?? commands.FirstOrDefault()?.failure_rate
                ?? 0;
            result[i] = new()
            {
                FiveValueGain = gains,
                PtGain = values[30],
                FiveValueGainNonScenario = [.. gains],
                PtGainNonScenario = values[30],
                VitalGain = vitalGain,
                FailureRate = failureRate,
            };
        }
        return result;
    }

    static int[] CreateTrainingLevels(SingleModeChara chara)
    {
        var levels = new[] { 1, 1, 1, 1, 1 };
        foreach (var training in chara.training_level_info_array ?? [])
            if (GameGlobal.ToTrainIndex.TryGetValue(training.command_id, out var index))
                levels[index] = training.level;
        return levels;
    }

    static void UpdateLegendState(
        TurnStats stats,
        SingleModeChara chara,
        SingleModeHomeInfo homeInfo)
    {
        stats.legend_friendAtTrain = new bool[5];
        var groupCardPositions = chara.support_card_array
            .Where(card => card.support_card_id == 30241)
            .Select(card => card.position)
            .ToHashSet();
        foreach (var command in homeInfo.command_info_array)
            if (GameGlobal.ToTrainIndex.TryGetValue(command.command_id, out var index) &&
                (command.training_partner_array ?? []).Any(groupCardPositions.Contains))
                stats.legend_friendAtTrain[index] = true;
        stats.legend_isEffect104 = (chara.chara_effect_id_array ?? []).Contains(104);
        stats.legend_friendClickEventCountConcerned = !stats.legend_isEffect104;
    }

    void UpdateScenarioDisplay(EventLoggerSnapshot source)
    {
        if (source.CharaInfo is not { } chara)
            return;

        if (chara.scenario_id == (int)ScenarioType.Ramen &&
            ramenPartProducer is { } ramenProducer)
        {
            UpdateRamenPart(
                ramenProducer,
                EventLoggerScenarioDisplayPart.Capture(
                    chara.scenario_id,
                    chara.single_mode_chara_id,
                    chara.turn));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IDisposable RegisterRamenPartProducer() => RamenScenarioDisplayBridge.Register();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void UpdateRamenPart(
        IDisposable producer,
        EventLoggerScenarioDisplayPart part)
        => RamenScenarioDisplayBridge.Update(producer, part);
}
