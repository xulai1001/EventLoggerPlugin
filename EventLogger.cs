using Gallop;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.RootFinding;
using MessagePack;
using MessagePack;
using Newtonsoft.Json;
using Spectre.Console;
using UmamusumeResponseAnalyzer;
using static UmamusumeResponseAnalyzer.Localization.Game;

namespace EventLoggerPlugin
{

    public class LogValue
    {
        public static LogValue NULL = new();
        public int Stats = 0;
        public int Pt = 0;
        public int Vital = 0;
        private string fmt(int x) => x.ToString("+#;-#;0");
        public string Explain()
        {
            return $">> 属性: {fmt(Stats)}, Pt: {fmt(Pt)}, 体力: {fmt(Vital)}；评分: +{EventStrength}";
            // return $"属性: {fmt(Stats)}, Pt: {fmt(Pt)}, 体力: {fmt(Vital)}";
        }
        public static LogValue operator -(LogValue a, LogValue b)
        {
            return new LogValue
            {
                Stats = a.Stats - b.Stats,
                Pt = a.Pt - b.Pt,
                Vital = a.Vital - b.Vital
            };
        }

        public bool IsEmpty { get => Stats == 0 && Pt == 0 && Vital == 0; }

        // 事件强度(ES)
        // 暂时按1es = 1属性+1pt, 1体力=2属性估算
        public double EventStrength
        {
            get
            {
                return Stats * 4 + Pt * 2 + Vital * 6;
            }
        }
    }
    public class LogEvent
    {
        public LogValue Value;
        public int Turn = 0;
        public int StoryId = -1;
        public int SelectIndex = -1;    // 返回的选择结果
        public int EventType = 0;
        public int Pt => Value.Pt;
        public int Vital => Value.Vital;
        public int Stats => Value.Stats;
        public double EventStrength => Value.EventStrength;
        public LogEvent() { }
        public LogEvent(LogEvent ev)
        {
            Turn = ev.Turn;
            StoryId = ev.StoryId;
            SelectIndex = ev.SelectIndex;
            EventType = ev.EventType;
            Value = new LogValue
            {
                Pt = ev.Pt,
                Vital = ev.Vital,
                Stats = ev.Stats
            };
        }
    }

    public class CardEventLogEntry
    {
        public int scenarioId = 0;  // 剧本ID
        public int turn = -1;       // 回合数
        public int eventType = 0;   // 事件类型 4-系统，5-马娘，8-支援卡
        public int cardId = -1;     // 支援卡ID eventType=8时生效
        public int rarity = -1;     // 稀有度
        public int step = 0;        // 连续事件步数
        public bool isFinished = false;
    }

    public class SkillTipInfo
    {
        public string name = ""; // 技能名
        public int old_level = 0;
        public int new_level = 0;
    }

    public static class EventLogger
    {
        public const int MinEventStrength = 25;
        // 排除佐岳充电,SS,继承,老登三选一,第三年凯旋门（输/赢）,以及无事发生直接到下一回合的情况
        public static readonly int[] ExcludedEvents = [809043003, 400006112, 400000040, 400006474, 400006439, 830241003, -1];
        // 友人和团队卡不计入连续事件，这里仅排除这几个
        public static readonly int[] ExcludedFriendCards = [30160, 30137, 30067, 30052, 10104, 30188, 10109, 30207, 30241, 30257, 30276, 10128, 10138];
        // 这些回合不能触发连续事件
        public static readonly int[] ExcludedTurns = [1, 25, 31, 35, 37, 38, 39, 40, 49, 51, 55, 59, 61, 62, 63, 64, 72, 73, 74, 75, 76, 77, 78];

        public static List<LogEvent> CardEvents = []; // 支援卡事件
        public static List<LogEvent> AllEvents = []; // 全部事件（除去排除的）
        public static int CardEventCount = 0;   // 连续事件发生数
        public static int CardEventFinishCount = 0; // 连续事件完成数
        public static int CardEventFinishTurn = 0;  // 如果连续事件全走完，记录回合数
        public static int CardEventRemaining = 0;  // 连续事件剩余数
        public static int SuccessEventCount = 0;    // 赌狗事件发生数
        public static int SuccessEventSelectCount = 0;  // 赌的次数
        public static int SuccessEventSuccessCount = 0; // 成功数
        public static int CurrentScenario = 0;  // 记录当前剧本，用于判断成功事件
        public static List<int> InheritStats;   // 两次继承属性
        public static Dictionary<int, SkillTips> lastSkillTips;   // 上一次的Hint表
        public static Dictionary<int, Gallop.SkillData> lastSkill;  // 上一次的技能表
        public static Dictionary<string, int> lastProper;    // 上一次的适性
        public static List<int> raceHistory;    // 哪些回合跑了比赛。回合数从1开始
        // 特殊支援卡（只有一段事件）
        public static Dictionary<int, int> CardEventSpecialCount = new Dictionary<int, int>
        {
            { 30244, 1 },
            { 30258, 1 },
            { 30270, 1 }
        };

        public static LogValue LastValue;   // 前一次调用时的总属性
        public static LogEvent LastEvent;   // 本次调用时已经结束的事件
        public static bool IsStart = false;
        public static int InitTurn = 0;    // 调用Init时的起始回合数
        public static List<int> CardIDs;   // 存放配卡，以过滤乱入事件
        public static int vitalSpent = 0;  // 温泉杯统计体力消耗
        public static int LastVital = 0;    // 上一个动作的体力消耗
        public static bool captureVitalSpending = false;    // 是否统计体力消耗的开关

        // 获取当前的属性
        public static LogValue Capture(SingleModeCheckEventResponse @event)
        {
            // sanity check
            if (@event.data.chara_info == null) return LogValue.NULL;
            var currentFiveValue = new int[]
            {
                    @event.data.chara_info.speed,
                    @event.data.chara_info.stamina,
                    @event.data.chara_info.power,
                    @event.data.chara_info.guts,
                    @event.data.chara_info.wiz,
            };
            var currentFiveValueRevised = currentFiveValue.Select(ScoreUtils.ReviseOver1200);
            var totalValue = currentFiveValueRevised.Sum();
            var pt = @event.data.chara_info.skill_point;
            var vital = @event.data.chara_info.vital;
            CurrentScenario = @event.data.chara_info.scenario_id;
            return new LogValue()
            {
                Stats = totalValue,
                Pt = pt,
                Vital = vital
            };
        }
        public static void Print(string s)
        {
            // 以后可能用别的打印方式
            AnsiConsole.MarkupLine(s);
        }

        //--------------------------
        // 这个方法在重复发送第一回合时会被反复调用，需要可重入
        public static void Init(SingleModeCheckEventResponse @event)
        {
            CardEvents = [];
            AllEvents = [];
            InheritStats = [];
            CardEventCount = 0;
            CardEventFinishTurn = 0;
            CardEventFinishCount = 0;
            SuccessEventCount = 0;
            SuccessEventSuccessCount = 0;
            SuccessEventSelectCount = 0;
            CurrentScenario = 0;
            IsStart = false;
            InitTurn = @event.data.chara_info.turn;
            // 需要传入SupportCard数组以确认带了哪些卡
            CardIDs = @event.data.chara_info.support_card_array.Select(x => x.support_card_id).ToList();
            CardEventRemaining = 0;
            foreach (var c in CardIDs)
            {
                if (!ExcludedFriendCards.Contains(c) && c / 10000 > 1)  // 稀有度>1
                    CardEventRemaining += c / 10000;
            }
            lastSkill = new Dictionary<int, Gallop.SkillData>();
            lastSkillTips = new Dictionary<int, SkillTips>();
            lastProper = new Dictionary<string, int>();
            vitalSpent = 0;
            captureVitalSpending = false;
            LastEvent = new LogEvent();
            LastValue = new LogValue();
            LastVital = 0;
        }

        // 开始记录属性变化
        public static void Start(SingleModeCheckEventResponse @event)
        {
            LastValue = Capture(@event);
            LastEvent = new LogEvent();
            IsStart = true;
        }

        // 结束记录前一个事件的属性变化，并保存
        public static void Update(SingleModeCheckEventResponse @event)
        {
            // sanity check
            if (LastEvent == null)
            {
                Init(@event);
                IsStart = true;
            }
            // 获取上一个事件的结果
            if (IsStart && @event.data.select_index != null && @event.data.select_index != 1)
            {
                // 不太对
                //Print($"[yellow]上次事件结果: {(State)@event.data.select_index}[/]");
                LastEvent.SelectIndex = (int)@event.data.select_index;
            }

            // 获取技能表和适性
            if (IsStart && @event.data.chara_info != null)
            {
                var currentSkillTip = SkillTipsToDict(@event.data.chara_info.skill_tips_array);
                var currentSkill = @event.data.chara_info.skill_array.ToDictionary(x => x.skill_id);
                var newSkills = new List<string>();

                if (lastSkill != null)
                {
                    foreach (var k in currentSkill.Keys)
                    {
                        if (!lastSkill.ContainsKey(k) || lastSkill[k].level != currentSkill[k].level)
                        {
                            var skill = currentSkill[k];
                            var name = SkillManagerGenerator.Default[skill.skill_id]?.DisplayName ?? $"#{skill.skill_id}";
                            //Print($"[violet]习得技能 {name}[/]");
                            newSkills.Add(name);
                        }
                    }
                    if (newSkills.Count() > 0)
                        Print($"[violet]习得技能: {string.Join(", ", newSkills)}[/]");
                }
                if (lastSkillTips != null)
                {
                    var newTips = AnalyzeSkillTips(currentSkillTip);
                    foreach (var t in newTips)
                        Print($"[violet]习得Hint: {t.name} Lv.{t.old_level} -> {t.new_level}[/]");
                }

                lastSkill = currentSkill;
                lastSkillTips = currentSkillTip;

                var currProper = UpdateProper(@event);
                if (lastProper != null && lastProper.Count() == currProper.Count())
                {
                    string[] properText = ["", "G", "F", "E", "D", "C", "B", "A", "S"];
                    foreach (var k in currProper.Keys)
                    {
                        if (lastProper.Keys.Contains(k) && lastProper[k] < currProper[k])
                            Print($"[yellow]{k} 适性提升: {properText[lastProper[k]]} -> {properText[currProper[k]]}[/]");
                    }
                }
                lastProper = currProper;
            }

            // 获得上一个动作或事件的属性并保存
            var currentValue = Capture(@event);
            LastEvent.Value = currentValue - LastValue;
            // 记录体力消耗(不记录恢复)
            if (captureVitalSpending)
            {
                LastVital = LastEvent.Value.Vital;
                if (LastVital < 0)
                {
                    var spent = Math.Abs(LastVital);
                    vitalSpent += spent;
                    Print($"[blue]体力 - {spent}[/]");
                } 
                else if (LastVital > 0)
                {
                    Print($"[green]体力 + {LastVital}[/]");
                }
            }
            // 分析事件
            if (IsStart && @event.data.unchecked_event_array != null)
            {
                if (@event.data.unchecked_event_array.Count() > 0)
                {
                    var choices = @event.data.unchecked_event_array.First().event_contents_info.choice_array;
                    if (choices.Count() > 0)
                        LastEvent.SelectIndex = choices[0].select_index;
                }

                // 分析事件
                var eventType = LastEvent.StoryId / 100000000;
                var rarity = LastEvent.StoryId / 10000000 % 10;    // 取第二位-稀有度
                var which = LastEvent.StoryId % 100;   // 取低2位
                var cardId = LastEvent.StoryId / 1000 % 100000;

                if (!ExcludedEvents.Contains(LastEvent.StoryId))
                {
                    // 首先判断是否为支援卡事件，如"8 30161 003"
                    if (eventType == 8)
                    {
                        if (rarity > 1 && which <= rarity && !ExcludedFriendCards.Contains(cardId))    // 是连续事件
                        {
                            if (CardIDs.Contains(cardId))   // 是携带的支援卡
                            {
                                // sanity check 防止重入
                                if (CardEvents.Any(e => e.StoryId == LastEvent.StoryId))
                                {
                                    AnsiConsole.MarkupLine($"[red]已经记录该连续事件: {LastEvent.StoryId}, 忽略重复记录[/]");
                                }
                                else
                                {
                                    ++CardEventCount;
                                    --CardEventRemaining;
                                    // 记录事件
                                    var logEntry = new CardEventLogEntry
                                    {
                                        scenarioId = @event.data.chara_info.scenario_id,
                                        turn = @event.data.chara_info.turn,
                                        eventType = eventType,
                                        cardId = cardId,
                                        rarity = rarity,
                                        step = which,
                                        isFinished = false
                                    };
                                    if (which == rarity)
                                    {
                                        ++CardEventFinishCount;    // 走完了N个事件（N是稀有度）则认为连续事件走完了                                    
                                        Print($"[green]连续事件完成[/]");
                                        logEntry.isFinished = true;
                                    }
                                    else
                                    {
                                        if (IsEventInterrupted(null))
                                        {
                                            ++CardEventFinishCount;
                                            logEntry.isFinished = true;
                                        }
                                        else
                                            Print($"[yellow]连续事件 {which} / {rarity}[/]");
                                    }
                                    if (CardEventFinishCount == 5)
                                        CardEventFinishTurn = @event.data.chara_info.turn;
                                    WriteLog(logEntry);
                                }
                            }
                            else
                            {
                                Print($"[red]乱入连续事件[/]");
                            }
                            CardEvents.Add(new LogEvent(LastEvent));
                        }
                        AllEvents.Add(new LogEvent(LastEvent));
                        Print($">> {LastEvent.Value.Explain()}");
                    }
                    else if (!LastEvent.Value.IsEmpty && LastEvent.Pt >= 0)
                    {
                        // 马娘或系统事件
                        // 过滤掉特判的、不加属性的。
                        // pt<0的是因为点了技能，会干扰统计，也排除掉
                        var st = LastEvent.EventStrength;
                        if (st < 0 || st >= MinEventStrength) // 过滤掉蚊子腿事件（<0是坏事件，需要留着）
                        {
                            AllEvents.Add(new LogEvent(LastEvent));
                            Print($">> #{LastEvent.StoryId}: {LastEvent.Value.Explain()}");
                        }
                    }
                }
                else
                {
                    // 分析特殊事件
                    if (LastEvent.StoryId == 400000040)    // 继承
                    { 
                        var color = "yellow";
                        if (LastEvent.Stats < 126)
                            color = "red";
                        else if (LastEvent.Stats >= 192)
                            color = "green";
                        Print($"[{color}]本次继承属性：{LastEvent.Stats}, Pt: {LastEvent.Pt}[/]");
                        InheritStats.Add(LastEvent.Stats);
                    }
                } // if excludedevents
                LastEvent.StoryId = @event.data.unchecked_event_array.Count() > 0 ? @event.data.unchecked_event_array.First().story_id : -1;
            } // if isstart
            // 保存当前回合数和story_id到lastEvent，用于下次调用
            LastValue = currentValue;
            LastEvent.Turn = @event.data.chara_info.turn;            
        }

        public static Dictionary<int, SkillTips> SkillTipsToDict(SkillTips[] tips)
        {
            return tips.ToDictionary(x => x.group_id * 10 + x.rarity);
        }

        /// <summary>
        /// 将转为Dict的SkillTips数组和现有hint等级比对，得到新增的hint文字结果
        /// </summary>
        public static List<SkillTipInfo> AnalyzeSkillTips(Dictionary<int, SkillTips> tipsDict)
        {
            var newTips = new List<SkillTipInfo>();
            // 获取技能Hint更新情况
            if (lastSkillTips != null)
            {
                foreach (var k in tipsDict.Keys)
                {
                    if (!lastSkillTips.ContainsKey(k) || lastSkillTips[k].level != tipsDict[k].level)
                    {
                        var skill = tipsDict[k];
                        var old_level = 0;
                        if (lastSkillTips.TryGetValue(k, out var v))
                        {
                            old_level = v.level;
                        }
                        var name = $"#{skill.group_id}, {skill.rarity}, {skill.level}";
                        var sks = SkillManagerGenerator.Default[(skill.group_id, skill.rarity)];
                        if (sks != null && sks.Count() >= 1)
                        {
                            var which = sks[0].Name.Contains("◎") ? 1 : 0;  // 排除双圈
                            name = sks[which].DisplayName;
                        }
                        newTips.Add(new SkillTipInfo
                        {
                            name = name,
                            old_level = old_level,
                            new_level = skill.level
                        });
                    }
                }
            }
            return newTips;
        }

        public static void WriteLog(CardEventLogEntry entry)
        {
            var filename = "PluginData/EventLoggerPlugin/events.json";
            List<CardEventLogEntry> events = new List<CardEventLogEntry>();
            // 读取现有事件记录
            if (File.Exists(filename))
            {
                try
                {
                    var f = File.ReadAllText(filename);
                    events = JsonConvert.DeserializeObject<List<CardEventLogEntry>>(f);
                    if (events == null) events = new List<CardEventLogEntry>();
                }
                catch
                {
                    AnsiConsole.MarkupLine($"[red]读取 events.json 出错，请检查 EventLoggerPlugin 插件[/]");
                }
            }
            // 写入
            try
            {
                events.Add(entry);
                File.WriteAllText(filename, JsonConvert.SerializeObject(events, Formatting.Indented));
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]写入 events.json 出错，请检查 EventLoggerPlugin 插件[/]");
                AnsiConsole.WriteLine(e.Message);
            }
        }

        public static void AnalyzeSuccessionChoice(SingleModeCheckEventResponse @event) { 
            Print("[lime]------ 继承选择 ------[/]");
            var chara = @event.data.chara_info;
            var se = @event.data.unchecked_event_array.First().succession_event_info;
            string[] properText = ["", "G", "F", "E", "D", "C", "B", "A", "S"];

            var currentFiveValue = new int[]
            {
                chara.speed,
                chara.stamina,
                chara.power,
                chara.guts,
                chara.wiz,
            };
            var currentFiveValueRevised = currentFiveValue.Select(ScoreUtils.ReviseOver1200);
            var totalValue = currentFiveValueRevised.Sum();
            var pt = chara.skill_point;
            var proper = UpdateProper(@event);

            var table = new Table();
            var cols = new List<Markup>();
            foreach (var choice in se.succession_gain_info_array)
                table.AddColumn($"继承结果 [lime]{choice.lottery_id}[/]", col => col.Width(32));
           
            foreach (var choice in se.succession_gain_info_array)
            {
                var lines = new List<string>();
                var newTotal = choice.FiveStatus.Select(ScoreUtils.ReviseOver1200).Sum();
                var newPt = choice.skill_point;
                var newProper = choice.Proper;
                lines.Add($"属性: [cyan]{newTotal - totalValue}[/], PT: {newPt - pt}");
                // 统计适性
                foreach (var k in newProper.Keys)
                {
                    if (proper.Keys.Contains(k) && proper[k] < newProper[k])
                        lines.Add($"[yellow]{k} 适性提升: {properText[proper[k]]} -> {properText[newProper[k]]}[/]");
                }
                // 统计白因子数 factor_id >= 1000000
                var whiteCount = 0;
                foreach (var pos in choice.effected_factor_array)
                    whiteCount += pos.factor_info_array.Count(x => x.factor_id >= 1000000);
                lines.Add($"白因子: [cyan]{whiteCount}[/]");
                // 统计Hint
                var tipsDict = SkillTipsToDict(choice.skill_tips_array);
                var newTips = AnalyzeSkillTips(tipsDict);
                lines.Add($"技能Hint: [cyan]{newTips.Count}[/]");

                cols.Add(new Markup(string.Join("\n", lines)));
            }
            table.AddRow(cols);
            AnsiConsole.Write(table);            
        }

        /// <summary>
        ///  判断是否断事件
        /// </summary>
        /// <param name="request_choice">手动选择时，为@event.choice_number - 1</param>
        public static bool IsEventInterrupted(int? request_choice)
        {
            if (LastEvent != null && Database.Events.TryGetValue(LastEvent.StoryId, out var story))
            {
                var eventType = LastEvent.StoryId / 100000000;
                var cardId = LastEvent.StoryId / 1000 % 100000;
                var rarity = LastEvent.StoryId / 10000000 % 10;  // 取第二位-稀有度
                var which = LastEvent.StoryId % 100;   // 取低2位

                if (request_choice != null)
                {
                    var choiceIndex = (int)request_choice;
                    // 主动选择断事件
                    // 是非友人事件，且记录了事件效果
                    if (eventType == 8 &&
                        !ExcludedFriendCards.Contains(cardId) &&
                        story.Choices.Count > choiceIndex &&
                        story.Choices[choiceIndex].Count > 0)
                    {
                        var choice = story.Choices[choiceIndex][0];
                        // 判断是否为断事件
                        if (choice.SuccessEffectValue != null && choice.SuccessEffectValue.Extras.Any(x => x.Contains("打ち切り")))
                        {
                            Print(@"[red]事件中断[/]");
                            return true;
                        }
                    }
                }
                else
                {
                    // 自动断事件
                    if (eventType == 8
                        && !ExcludedFriendCards.Contains(cardId)
                        && story.Choices.Count > 0
                        && story.Choices[0].Count > LastEvent.SelectIndex)
                    {
                        var choice = story.Choices[0][LastEvent.SelectIndex];
                        // 判断是否为断事件
                        if (choice.SuccessEffectValue != null && choice.SuccessEffectValue.Extras.Any(x => x.Contains("打ち切り")))
                        {
                            Print(@"[green]事件提前完成[/]");
                            return true;
                        }
                        // 特判(类似光明哥的单事件卡)
                        if (CardEventSpecialCount.Keys.Contains(cardId) && which >= CardEventSpecialCount[cardId])
                            return true;
                    }
                }   // if request_choice
            }  // if LastEvent
            return false;
        }

        /// <summary>
        /// 读取适性
        /// </summary>
        /// <param name="event">当前事件</param>
        /// <returns>新的适性数据</returns>
        public static Dictionary<string, int> UpdateProper(SingleModeCheckEventResponse @event)
        {
            var chara = @event.data.chara_info;
            return new Dictionary<string, int>
            {
                { I18N_Short, chara.proper_distance_short },
                { I18N_Mile, chara.proper_distance_mile },
                { I18N_Middle, chara.proper_distance_middle },
                { I18N_Long, chara.proper_distance_long },
                { I18N_Nige, chara.proper_running_style_nige },
                { I18N_Oikomi, chara.proper_running_style_oikomi },
                { I18N_Sashi, chara.proper_running_style_sashi },
                { I18N_Senko, chara.proper_running_style_senko },
                { I18N_Grass, chara.proper_ground_turf },
                { I18N_Dirt, chara.proper_ground_dirt }
            };
        }

        // 当玩家选择选项时进行记录
        public static void UpdatePlayerChoice(Gallop.SingleModeChoiceRequest @event)
        {
            Print($"[violet]选择选项 {@event.choice_number}[/]");
            if (IsEventInterrupted(@event.choice_number - 1))
            {
                var cardId = LastEvent.StoryId / 1000 % 100000;
                var rarity = LastEvent.StoryId / 10000000 % 10;  // 取第二位-稀有度
                var which = LastEvent.StoryId % 100;   // 取低2位
                if (CardIDs.Contains(cardId))             // 排除打断乱入连续事件的情况
                {
                    ++CardEventFinishCount;
                    CardEventRemaining -= (rarity - which); // 计算打断了几段事件，从总数里减去
                    if (CardEventFinishCount == 5)
                        CardEventFinishTurn = LastEvent.Turn;
                }
            }
        }
        public static List<string> PrintCardEventPerf(int scenario)
        {
            var ret = new List<string>();
            if (CardEventCount > 0)
            {
                // https://x.com/Alefrain_ht/status/1811300886737797511/photo/3
                // 回合数不满78的剧本比其他剧本高5%，这部分调整中
                var p = scenario switch
                {
                    6 | 13 => 0.35,
                    _ => 0.3
                };
                var n = (GameStats.currentTurn - InitTurn + 1) - ExcludedTurns.Count(x => x >= InitTurn && x <= GameStats.currentTurn);
                //(p(x<=k-1) + p(x<=k)) / 2
                var bn = Binomial.CDF(p, n, CardEventCount);
                var bn_1 = Binomial.CDF(p, n, CardEventCount - 1);

                ret.Add("");
                if (CardEventFinishCount < 5)
                {
                    // 调试中，暂不加入I18N
                    ret.Add(string.Format("连续事件出现[yellow]{0}[/]次", CardEventCount));
                    ret.Add(string.Format("走完[yellow]{0}[/]张卡", CardEventFinishCount));
                    if (InitTurn != 1 && n > 0)
                        ret.Add(string.Format("连续事件运气: [yellow]{0}%[/]", ((bn + bn_1) / 2 * 200 - 100).ToString("+#;-#;0")));
                    else
                    {
                        // 从第1回合开始记录则可以计算连续事件走完率
                        var TurnRemaining = 78 - GameStats.currentTurn - ExcludedTurns.Count(x => x > GameStats.currentTurn); // 还剩多少回合，不包括本回合
                        // p(x>=k) = 1-p(x<=k-1)
                        double pFinish = 0;
                        if (CardEventRemaining <= 0)
                            pFinish = 1;
                        else if (TurnRemaining > 0)
                            pFinish = 1 - Binomial.CDF(p, TurnRemaining, CardEventRemaining - 1);
                        ret.Add(string.Format("剩余[yellow]{0}[/]个连续事件", CardEventRemaining));
                        ret.Add(string.Format("完成概率: [yellow]{0}%[/]", (pFinish * 100).ToString("0")));
                    }
                }
                else
                {
                    ret.Add(string.Format("[green]连续事件全部完成[/]"));
                }
            }
            return ret;
        }

        public static void UpdateRaceHistory(SingleModeRaceHistory[] history)
        {
            raceHistory = new List<int>();
            foreach (var h in history)
            {
                if (h.result_rank == 1)
                    raceHistory.Add(h.turn);
            }
            AnsiConsole.MarkupLine($"[magenta]当前已取胜 {raceHistory.Count} 场[/]");
        }
    }
}
