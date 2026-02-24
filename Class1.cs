using MessagePack;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.IO.Compression;
using UmamusumeResponseAnalyzer;
using UmamusumeResponseAnalyzer.Plugin;

[assembly: LoadInHostContext]
namespace EventLoggerPlugin
{
    [MessagePackObject]
    public class SingleModeRaceHistory
    {
        [Key("turn")]
        public int turn;
        [Key("program_id")]
        public int program_id;
        [Key("weather")]
        public int weather;
        [Key("ground_condition")]
        public int ground_condition;
        [Key("running_style")]
        public int running_style;
        [Key("result_rank")]
        public int result_rank;
        [Key("frame_order")]
        public int frame_order;
        [Key("npc_count")]
        public int npc_count;
    }

    public class EventLoggerPlugin : IPlugin
    {
        [PluginDescription("记录事件信息&干劲等")]
        public string Name => "EventLoggerPlugin";
        public string Author => "xulai1001";
        public string[] Targets => [];
        public async Task UpdatePlugin(ProgressContext ctx)
        {
            var progress = ctx.AddTask($"[[{Name}]] 更新");

            using var client = new HttpClient();
            using var resp = await client.GetAsync($"https://api.github.com/repos/URA-Plugins/{Name}/releases/latest");
            var json = await resp.Content.ReadAsStringAsync();
            var jo = JObject.Parse(json);

            var isLatest = ("v" + ((IPlugin)this).Version.ToString()).Equals("v" + jo["tag_name"]?.ToString());
            if (isLatest)
            {
                progress.Increment(progress.MaxValue);
                progress.StopTask();
                return;
            }
            progress.Increment(25);

            var downloadUrl = jo["assets"][0]["browser_download_url"].ToString();
            if (Config.Updater.IsGithubBlocked && !Config.Updater.ForceUseGithubToUpdate)
            {
                downloadUrl = downloadUrl.Replace("https://", "https://gh.shuise.dev/");
            }
            using var msg = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            using var stream = await msg.Content.ReadAsStreamAsync();
            var buffer = new byte[8192];
            while (true)
            {
                var read = await stream.ReadAsync(buffer);
                if (read == 0)
                    break;
                progress.Increment(read / msg.Content.Headers.ContentLength ?? 1 * 0.5);
            }
            using var archive = new ZipArchive(stream);
            archive.ExtractToDirectory(Path.Combine("Plugins", Name), true);
            progress.Increment(25);

            progress.StopTask();
        }

        [Analyzer(priority: -1)]
        public void StartEventLogger(JObject jo)
        {
            if (!jo.HasCharaInfo()) return;
            if (jo["data"] is null || jo["data"] is not JObject data) return;

            // 有command_result -> 记录训练结果
            //  captureVitalSpending=False -> 跳过训练结果，记录事件(start)
            //  captureVitalSpending=True -> 记录训练体力和事件(update)
            // 没有command_result, 有unchecked_event_array -> 记录事件(update)
            if (data["command_result"] is JObject command_result) // 训练结果
            {
                if (command_result["result_state"].ToInt() == 1) // 训练失败
                {
                    AnsiConsole.MarkupLine("训练失败！");
                    if (GameStats.stats[GameStats.currentTurn] != null)
                        GameStats.stats[GameStats.currentTurn].isTrainingFailed = true;
                }
                var @event = jo.ToObject<Gallop.SingleModeCheckEventResponse>();
                if (@event != null)
                {
                    if (EventLogger.captureVitalSpending)
                    {
                        EventLogger.IsStart = true;
                        EventLogger.Update(@event);
                    }
                    else
                    {
                        EventLogger.Start(@event); // 开始记录事件，跳过从上一次调用update到这里的所有事件和训练
                    }
                }
            }
            if (data.ContainsKey("unchecked_event_array"))
            {
                var @event = jo.ToObject<Gallop.SingleModeCheckEventResponse>();
                if (@event != null)
                {
                    // 这时当前事件还没有生效，先显示上一个事件的收益
                    EventLogger.Update(@event);
                    foreach (var i in @event.data.unchecked_event_array)
                    {
                        if (GameStats.stats[GameStats.currentTurn] != null)
                        {
                            if (i.story_id == 830137001)//第一次点击女神
                            {
                                GameStats.stats[GameStats.currentTurn].venus_isVenusCountConcerned = false;
                            }

                            if (i.story_id == 830137003)//女神三选一事件
                            {
                                GameStats.stats[GameStats.currentTurn].venus_venusEvent = true;
                            }


                            if (i.story_id == 400006112)//ss训练
                            {
                                GameStats.stats[GameStats.currentTurn].larc_playerChoiceSS = true;
                            }

                            if (i.story_id == 809043002)//佐岳启动
                            {
                                GameStats.stats[GameStats.currentTurn].larc_zuoyueEvent = 5;
                            }

                            if (i.story_id == 809043003)//佐岳充电
                            {
                                var suc = i.event_contents_info.choice_array[0].select_index;
                                var eventType = 0;
                                if (suc == 1)//加心情
                                {
                                    eventType = 2;
                                }
                                else if (suc == 2)//不加心情
                                {
                                    eventType = 1;
                                }

                                GameStats.stats[GameStats.currentTurn].larc_zuoyueEvent = eventType;
                            }
                            if (i.story_id == 400006115)//远征佐岳加pt
                            {
                                GameStats.stats[GameStats.currentTurn].larc_zuoyueEvent = 4;
                            }
                            if (i.story_id == 809044002) // 凉花出门
                            {
                                GameStats.stats[GameStats.currentTurn].uaf_friendEvent = 5;
                            }
                            if (i.story_id == 809044003) // 凉花加体力
                            {
                                GameStats.stats[GameStats.currentTurn].uaf_friendEvent = 1;
                            }
                        }
                    }
                }
            }

            if (data.ContainsKey("race_history"))
            {
                var history = data["race_history"].ToObject<List<SingleModeRaceHistory>>();
                if (history != null)
                    EventLogger.UpdateRaceHistory(history.ToArray());
            }
        }
        [Analyzer(false, -1)]
        public void ParseChoiceRequest(JObject jo)
        {
            if (jo["choice_number"].ToInt() > 0)  // 玩家点击了事件
            {
                EventLogger.UpdatePlayerChoice(jo.ToObject<Gallop.SingleModeChoiceRequest>());
            }
        }
        [Analyzer(false, -1)]
        public void ParseTrainingRequest(JObject jo)
        {
            if (jo["command_type"].ToInt() == 1) //玩家点击了训练
            {
                var @event = jo.ToObject<Gallop.SingleModeExecCommandRequest>();
                var turn = @event.current_turn;
                if (GameStats.currentTurn != 0 && turn != GameStats.currentTurn) return;
                var trainingId = GameGlobal.ToTrainId[@event.command_id];
                if (GameStats.stats[turn] != null)
                    GameStats.stats[turn].playerChoice = trainingId;
            }
            /*
            if (jo["single_mode_race_entry_request_common"] != null) // 点击了比赛
            {
                var turn = jo["single_mode_race_entry_request_common"]["current_turn"].ToInt();
                //AnsiConsole.MarkupLine($"[magenta]回合{turn}: 进入比赛[/]");
                if (!EventLogger.raceHistory.Contains(turn)) EventLogger.raceHistory.Add(turn);
            }
            if (jo["single_mode_race_start_request_common"] != null)
            {
                var turn = jo["single_mode_race_start_request_common"]["current_turn"].ToInt();
                //AnsiConsole.MarkupLine($"[magenta]回合{turn}: 比赛中[/]");
                if (!EventLogger.raceHistory.Contains(turn)) EventLogger.raceHistory.Add(turn);
            }
            if (jo["single_mode_race_out_request_common"] != null )
            {
                var turn = jo["single_mode_race_out_request_common"]["current_turn"].ToInt();
                //AnsiConsole.MarkupLine($"[magenta]回合{turn}: 比赛结束[/]");
                if (!EventLogger.raceHistory.Contains(turn)) EventLogger.raceHistory.Add(turn);
            }
            */
        }
    }
}
