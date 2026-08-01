using Gallop;
using Gallop.Endpoints;
using UmamusumeResponseAnalyzer.TerminalGui;
using UmamusumeResponseAnalyzer.Plugin;

// 共享库插件：依赖它的场景分析器都用 [assembly: SharedContextWith("EventLoggerPlugin")] 与它同组，
// 一起进一个可卸载的 collectible ALC（整组可热重载），不再用 LoadInHostContext 进永不可卸载的 default ALC。
namespace EventLoggerPlugin
{
    public class EventLoggerPlugin : IPlugin
    {
        public string Name => "EventLoggerPlugin";
        public string Author => "xulai1001";
        public string[] Targets => [];
        string DataDirectory => Path.Combine("PluginData", Name);

        public void Initialize(IPluginContext context)
        {
            EventLogger.DataDirectory = DataDirectory;
            Directory.CreateDirectory(EventLogger.DataDirectory);
            EventLoggerDisplay.Initialize(context);
        }

        public void Dispose() => EventLoggerDisplay.Dispose();

        [ResponseAnalyzer<GameApi.SingleMode.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeArc.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeArcCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeBreeders.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeBreedersCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeCook.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeCookCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeFree.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeFreeCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeLegend.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeLegendCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeLive.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeLiveCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeMecha.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeMechaCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeOnsen.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeOnsenCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModePioneer.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModePioneerCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeRamen.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeRamenCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeSport.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeSportCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeTeam.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeTeamCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleModeVenus.CheckEvent>(-1)]
        public ValueTask StartEventLogger(SingleModeVenusCheckEventResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, response.data?.select_index_info_array));

        [ResponseAnalyzer<GameApi.SingleMode.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeArc.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeArcExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeBreeders.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeBreedersExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeCook.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeCookExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeFree.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeFreeExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeLegend.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeLegendExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeLive.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeLiveExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeMecha.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeMechaExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeOnsen.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeOnsenExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModePioneer.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModePioneerExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeRamen.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeRamenExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeSport.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeSportExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeTeam.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeTeamExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleModeVenus.ExecCommand>(-1)]
        public ValueTask StartEventLogger(SingleModeVenusExecCommandResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null), response.data?.command_result);

        [ResponseAnalyzer<GameApi.SingleMode.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeArc.ArcRaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeArcArcRaceEndResponse response)
            => AnalyzeResponse(default);

        [ResponseAnalyzer<GameApi.SingleModeArc.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeArcRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeBreeders.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeBreedersRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeCook.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeCookRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeFree.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeFreeRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeLegend.LegendRaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeLegendLegendRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeLegend.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeLegendRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeLive.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeLiveRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeMecha.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeMechaRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeOnsen.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeOnsenRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModePioneer.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModePioneerRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeRamen.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeRamenRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeSport.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeSportRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeTeam.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeTeamRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeTeam.TeamRaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeTeamTeamRaceEndResponse response)
            => AnalyzeResponse(default);

        [ResponseAnalyzer<GameApi.SingleModeTeam.TeamRaceEndOut>(-1)]
        public ValueTask StartEventLogger(SingleModeTeamTeamRaceEndOutResponse response)
            => AnalyzeResponse(new(response.data?.chara_info, response.data?.unchecked_event_array, null));

        [ResponseAnalyzer<GameApi.SingleModeVenus.RaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeVenusRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        [ResponseAnalyzer<GameApi.SingleModeVenus.VenusRaceEnd>(-1)]
        public ValueTask StartEventLogger(SingleModeVenusVenusRaceEndResponse response)
            => AnalyzeResponse(default, raceHistory: response.data?.race_history);

        ValueTask AnalyzeResponse(
            EventLoggerSnapshot snapshot,
            SingleModeCommandResult? commandResult = null,
            SingleRaceHistory[]? raceHistory = null)
        {
            if (commandResult != null)
            {
                AnalyzeCommandResult(commandResult);
                if (EventLogger.captureVitalSpending)
                {
                    EventLogger.IsStart = true;
                    EventLogger.Update(snapshot);
                }
                else
                {
                    EventLogger.Start(snapshot);
                }
            }

            if (snapshot.UncheckedEvents != null)
            {
                EventLogger.Update(snapshot);
                AnalyzeScenarioFlags(snapshot.UncheckedEvents);

                if (snapshot.UncheckedEvents.Length > 0 &&
                    snapshot.UncheckedEvents.First().succession_event_info != null)
                {
                    EventLogger.AnalyzeSuccessionChoice(snapshot);
                }
            }

            if (raceHistory != null)
                EventLogger.UpdateRaceHistory(raceHistory);

            return ValueTask.CompletedTask;
        }

        static void AnalyzeCommandResult(SingleModeCommandResult commandResult)
        {
            if (commandResult.result_state != 1) return;

            EventLoggerDisplay.Log("训练失败！", UiSeverity.Warning);
            EventLoggerDisplay.Notify("训练失败！", UiSeverity.Warning);
            if (GameStats.stats[GameStats.currentTurn] != null)
                GameStats.stats[GameStats.currentTurn].isTrainingFailed = true;
        }

        static void AnalyzeScenarioFlags(SingleModeEventInfo[] events)
        {
            foreach (var i in events)
            {
                var turnStats = GameStats.stats[GameStats.currentTurn];
                if (turnStats == null) continue;

                if (i.story_id == 830137001)
                    turnStats.venus_isVenusCountConcerned = false;

                if (i.story_id == 830137003)
                    turnStats.venus_venusEvent = true;

                if (i.story_id == 400006112)
                    turnStats.larc_playerChoiceSS = true;

                if (i.story_id == 809043002)
                    turnStats.larc_zuoyueEvent = 5;

                if (i.story_id == 809043003)
                {
                    var suc = EventLogger.FirstSelectIndex(i.event_contents_info.choice_array[0]);
                    turnStats.larc_zuoyueEvent = suc switch
                    {
                        1 => 2,
                        2 => 1,
                        _ => 0
                    };
                }

                if (i.story_id == 400006115)
                    turnStats.larc_zuoyueEvent = 4;

                if (i.story_id == 809044002)
                    turnStats.uaf_friendEvent = 5;

                if (i.story_id == 809044003)
                    turnStats.uaf_friendEvent = 1;
            }
        }

        [RequestAnalyzer<GameApi.SingleMode.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeArc.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeArcCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeBreeders.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeBreedersCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeCook.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeCookCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeFree.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeFreeCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeLegend.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeLegendCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeLive.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeLiveCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeMecha.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeMechaCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeOnsen.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeOnsenCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModePioneer.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModePioneerCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeRamen.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeRamenCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeSport.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeSportCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeTeam.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeTeamCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        [RequestAnalyzer<GameApi.SingleModeVenus.CheckEvent>(-1)]
        public ValueTask ParseChoiceRequest(SingleModeVenusCheckEventRequest request)
            => ParseChoiceRequest(request.single_mode_check_event_request_common);

        ValueTask ParseChoiceRequest(SingleModeCheckEventRequestCommon request)
        {
            if (request.choice_number > 0)
                EventLogger.UpdatePlayerChoice(request);

            return ValueTask.CompletedTask;
        }

        [RequestAnalyzer<GameApi.SingleMode.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeArc.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeArcExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeBreeders.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeBreedersExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeCook.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeCookExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeFree.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeFreeExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeLegend.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeLegendExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeLive.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeLiveExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeMecha.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeMechaExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeOnsen.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeOnsenExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModePioneer.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModePioneerExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeRamen.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeRamenExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeSport.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeSportExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeTeam.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeTeamExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        [RequestAnalyzer<GameApi.SingleModeVenus.ExecCommand>(-1)]
        public ValueTask ParseTrainingRequest(SingleModeVenusExecCommandRequest request)
            => ParseTrainingRequest(request.single_mode_exec_command_request_common);

        ValueTask ParseTrainingRequest(SingleModeExecCommandRequestCommon request)
        {
            if (request.command_type != 1) return ValueTask.CompletedTask;

            var turn = request.current_turn;
            if (GameStats.currentTurn != 0 && turn != GameStats.currentTurn) return ValueTask.CompletedTask;
            var trainingId = GameGlobal.ToTrainId[request.command_id];
            if (GameStats.stats[turn] != null)
                GameStats.stats[turn].playerChoice = trainingId;

            return ValueTask.CompletedTask;
        }
    }
}
