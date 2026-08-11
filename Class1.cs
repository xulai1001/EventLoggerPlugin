using Gallop;
using UmamusumeResponseAnalyzer.Plugin;
using UmamusumeResponseAnalyzer.TerminalGui;

namespace EventLoggerPlugin;

public sealed class EventLoggerPlugin : IPlugin
{
    static string DataDirectory => Path.Combine("PluginData", "EventLoggerPlugin");

    public void Initialize(IPluginContext context)
    {
        EventLogger.ConfigureDataDirectory(DataDirectory);
        Directory.CreateDirectory(DataDirectory);
        EventLoggerDisplay.Initialize(context);

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
    }

    public void Dispose() => EventLoggerDisplay.Dispose();

    ValueTask AnalyzeCheckEvent(SingleModeCheckEventResponse response)
        => AnalyzeResponse(new(
            response.data?.chara_info,
            response.data?.unchecked_event_array,
            response.data?.select_index_info_array));

    ValueTask AnalyzeExecCommand(SingleModeExecCommandResponse response)
        => AnalyzeResponse(
            new(
                response.data?.chara_info,
                response.data?.unchecked_event_array,
                null),
            response.data?.command_result);

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
        if (commandResult is not null)
        {
            AnalyzeCommandResult(commandResult);
            if (EventLogger.Current.IsCapturingVital)
                EventLogger.UpdateWhileCapturing(snapshot);
            else
                EventLogger.Start(snapshot);
        }

        if (snapshot.UncheckedEvents is not null)
        {
            EventLogger.Update(snapshot);
            EventLogger.RecordScenarioEvents(snapshot.UncheckedEvents);

            if (snapshot.UncheckedEvents.Length > 0 &&
                snapshot.UncheckedEvents[0].succession_event_info is not null)
                EventLogger.AnalyzeSuccessionChoice(snapshot);
        }

        if (raceHistory is not null)
            EventLogger.UpdateRaceHistory(raceHistory);

        return ValueTask.CompletedTask;
    }

    static void AnalyzeCommandResult(SingleModeCommandResult commandResult)
    {
        if (commandResult.result_state != 1)
            return;

        EventLoggerDisplay.Notify("训练失败！", UiSeverity.Warning);
        EventLogger.MarkTrainingFailed();
    }

    static ValueTask ParseTrainingRequest(SingleModeExecCommandRequest request)
    {
        var common = request.single_mode_exec_command_request_common;
        if (common.command_type != 1)
            return ValueTask.CompletedTask;

        var turn = common.current_turn;
        EventLogger.RecordPlayerChoice(turn, GameGlobal.ToTrainId[common.command_id]);
        return ValueTask.CompletedTask;
    }
}
