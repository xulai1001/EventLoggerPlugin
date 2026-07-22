using Gallop;
using MathNet.Numerics.Distributions;
using Newtonsoft.Json;
using UmamusumeResponseAnalyzer.LiveDisplay;

namespace EventLoggerPlugin
{
    public readonly record struct EventLoggerSnapshot(
        SingleModeChara? CharaInfo,
        SingleModeEventInfo[]? UncheckedEvents,
        SingleModeSelectIndexInfo[]? SelectIndexInfo);

    public class LogValue
    {
        public static LogValue NULL = new();
        public int Stats = 0;
        public int Pt = 0;
        public int Vital = 0;
        static string FormatDelta(int x) => x.ToString("+#;-#;0");
        public string Explain()
        {
            return $">> 属性: {FormatDelta(Stats)}, Pt: {FormatDelta(Pt)}, 体力: {FormatDelta(Vital)}；评分: +{EventStrength}";
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
        public static readonly int[] ExcludedFriendCards = [30160, 30137, 30067, 30052, 10104, 30188, 10109, 30207, 30241, 30257, 30276, 10128, 10138, 10141, 30290];
        // 这些回合不能触发连续事件
        public static readonly int[] ExcludedTurns = [1, 25, 31, 35, 37, 38, 39, 40, 49, 51, 55, 59, 61, 62, 63, 64, 72, 73, 74, 75, 76, 77, 78];
        public static string DataDirectory { get; set; } = Path.Combine("PluginData", "EventLoggerPlugin");

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
        public static List<int> CardIDs = new List<int>();   // 存放配卡，以过滤乱入事件
        public static int vitalSpent = 0;  // 温泉杯统计体力消耗
        public static int LastVital = 0;    // 上一个动作的体力消耗
        public static bool captureVitalSpending = false;    // 是否统计体力消耗的开关

        // 获取当前的属性
        public static LogValue Capture(EventLoggerSnapshot snapshot)
        {
            var chara = snapshot.CharaInfo;
            if (chara == null) return LogValue.NULL;
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
            var vital = chara.vital;
            CurrentScenario = chara.scenario_id;
            return new LogValue()
            {
                Stats = totalValue,
                Pt = pt,
                Vital = vital
            };
        }

        static SingleModeChara RequireChara(EventLoggerSnapshot snapshot)
            => snapshot.CharaInfo
                ?? throw new InvalidOperationException("EventLogger 需要响应 DTO 的 chara_info。");

        public static void Print(string s)
        {
            EventLoggerDisplay.Log(s);
        }

        // 这个方法在重复发送第一回合时会被反复调用，需要可重入
        public static void Init(EventLoggerSnapshot snapshot)
        {
            var chara = RequireChara(snapshot);
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
            InitTurn = chara.turn;
            // 需要传入SupportCard数组以确认带了哪些卡
            CardIDs = chara.support_card_array.Select(x => x.support_card_id).ToList();
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
        public static void Start(EventLoggerSnapshot snapshot)
        {
            LastValue = Capture(snapshot);
            LastEvent = new LogEvent();
            IsStart = true;
        }

        // 结束记录前一个事件的属性变化，并保存
        public static void Update(EventLoggerSnapshot snapshot)
        {
            var chara = RequireChara(snapshot);
            var uncheckedEvents = snapshot.UncheckedEvents;
            // sanity check
            if (LastEvent == null)
            {
                Init(snapshot);
                IsStart = true;
            }
            var lastEvent = LastEvent
                ?? throw new InvalidOperationException("EventLogger 初始化后 LastEvent 仍为空。");
            // 获取上一个事件的结果
            var selectedIndex = FirstSelectIndex(snapshot.SelectIndexInfo);
            if (IsStart && selectedIndex is { } index && index != 1)
            {
                lastEvent.SelectIndex = index;
            }

            // 获取技能表和适性
            if (IsStart)
            {
                var currentSkillTip = SkillTipsToDict(chara.skill_tips_array);
                var currentSkill = chara.skill_array.ToDictionary(x => x.skill_id);
                var newSkills = new List<string>();

                if (lastSkill != null)
                {
                    foreach (var k in currentSkill.Keys)
                    {
                        if (!lastSkill.ContainsKey(k) || lastSkill[k].level != currentSkill[k].level)
                        {
                            var skill = currentSkill[k];
                            var name = $"#{skill.skill_id}";
                            newSkills.Add(name);
                        }
                    }
                    if (newSkills.Count > 0)
                        Print($"习得技能: {string.Join(", ", newSkills)}");
                }
                if (lastSkillTips != null)
                {
                    var newTips = AnalyzeSkillTips(currentSkillTip);
                    foreach (var t in newTips)
                        Print($"习得Hint: {t.name} Lv.{t.old_level} -> {t.new_level}");
                }

                lastSkill = currentSkill;
                lastSkillTips = currentSkillTip;

                var currProper = UpdateProper(chara);
                if (lastProper != null && lastProper.Count == currProper.Count)
                {
                    string[] properText = ["", "G", "F", "E", "D", "C", "B", "A", "S"];
                    foreach (var k in currProper.Keys)
                    {
                        if (lastProper.ContainsKey(k) && lastProper[k] < currProper[k])
                            Print($"{k} 适性提升: {properText[lastProper[k]]} -> {properText[currProper[k]]}");
                    }
                }
                lastProper = currProper;
            }

            // 获得上一个动作或事件的属性并保存
            var currentValue = Capture(snapshot);
            lastEvent.Value = currentValue - LastValue;
            // 记录体力消耗(不记录恢复)
            if (captureVitalSpending)
            {
                LastVital = lastEvent.Value.Vital;
                if (LastVital < 0)
                {
                    var spent = Math.Abs(LastVital);
                    vitalSpent += spent;
                    Print($"体力 - {spent}");
                } 
                else if (LastVital > 0)
                {
                    Print($"体力 + {LastVital}");
                }
            }
            // 分析事件
            if (IsStart && uncheckedEvents != null)
            {
                if (uncheckedEvents.Length > 0)
                {
                    var choices = uncheckedEvents.First().event_contents_info.choice_array;
                    if (choices.Length > 0)
                        lastEvent.SelectIndex = FirstSelectIndex(choices[0]);
                }

                // 分析事件
                var eventType = lastEvent.StoryId / 100000000;
                var rarity = lastEvent.StoryId / 10000000 % 10;    // 取第二位-稀有度
                var which = lastEvent.StoryId % 100;   // 取低2位
                var cardId = lastEvent.StoryId / 1000 % 100000;

                if (!ExcludedEvents.Contains(lastEvent.StoryId))
                {
                    // 首先判断是否为支援卡事件，如"8 30161 003"
                    if (eventType == 8)
                    {
                        if (rarity > 1 && which <= rarity && !ExcludedFriendCards.Contains(cardId))    // 是连续事件
                        {
                            if (CardIDs.Contains(cardId))   // 是携带的支援卡
                            {
                                // sanity check 防止重入
                                if (CardEvents.Any(e => e.StoryId == lastEvent.StoryId))
                                {
                                    EventLoggerDisplay.Log($"已经记录该连续事件: {lastEvent.StoryId}, 忽略重复记录", LiveDisplaySeverity.Warning);
                                }
                                else
                                {
                                    ++CardEventCount;
                                    --CardEventRemaining;
                                    // 记录事件
                                    var logEntry = new CardEventLogEntry
                                    {
                                        scenarioId = chara.scenario_id,
                                        turn = chara.turn,
                                        eventType = eventType,
                                        cardId = cardId,
                                        rarity = rarity,
                                        step = which,
                                        isFinished = false
                                    };
                                    if (which == rarity)
                                    {
                                        ++CardEventFinishCount;    // 走完了N个事件（N是稀有度）则认为连续事件走完了                                    
                                        Print("连续事件完成");
                                        logEntry.isFinished = true;
                                    }
                                    else
                                    {
                                        Print($"连续事件 {which} / {rarity}");
                                    }
                                    if (CardEventFinishCount == 5)
                                        CardEventFinishTurn = chara.turn;
                                    WriteLog(logEntry);
                                }
                            }
                            else
                            {
                                Print("乱入连续事件");
                            }
                            CardEvents.Add(new LogEvent(lastEvent));
                        }
                        AllEvents.Add(new LogEvent(lastEvent));
                        Print($">> {lastEvent.Value.Explain()}");
                    }
                    else if (!lastEvent.Value.IsEmpty && lastEvent.Pt >= 0)
                    {
                        // 马娘或系统事件
                        // 过滤掉特判的、不加属性的。
                        // pt<0的是因为点了技能，会干扰统计，也排除掉
                        var st = lastEvent.EventStrength;
                        if (st < 0 || st >= MinEventStrength) // 过滤掉蚊子腿事件（<0是坏事件，需要留着）
                        {
                            AllEvents.Add(new LogEvent(lastEvent));
                            Print($">> #{lastEvent.StoryId}: {lastEvent.Value.Explain()}");
                        }
                    }
                }
                else
                {
                    // 分析特殊事件
                    if (lastEvent.StoryId == 400000040)    // 继承
                    { 
                        Print($"本次继承属性：{lastEvent.Stats}, Pt: {lastEvent.Pt}");
                        InheritStats.Add(lastEvent.Stats);
                    }
                } // if excludedevents
                lastEvent.StoryId = uncheckedEvents.Length > 0 ? uncheckedEvents.First().story_id : -1;
            } // if isstart
            // 保存当前回合数和story_id到lastEvent，用于下次调用
            LastValue = currentValue;
            lastEvent.Turn = chara.turn;
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
            Directory.CreateDirectory(DataDirectory);
            var filename = Path.Combine(DataDirectory, "events.json");
            var events = File.Exists(filename)
                ? JsonConvert.DeserializeObject<List<CardEventLogEntry>>(File.ReadAllText(filename)) ?? []
                : [];

            events.Add(entry);
            File.WriteAllText(filename, JsonConvert.SerializeObject(events, Formatting.Indented));
        }

        public static void AnalyzeSuccessionChoice(EventLoggerSnapshot snapshot) {
            Print("------ 继承选择 ------");
            var chara = RequireChara(snapshot);
            var se = snapshot.UncheckedEvents?.FirstOrDefault()?.succession_event_info
                ?? throw new InvalidOperationException("EventLogger 需要 succession_event_info。");
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
            var proper = UpdateProper(chara);

            var sections = new List<string>();
            foreach (var choice in se.succession_gain_info_array)
            {
                var lines = new List<string>();
                lines.Add($"继承结果 {choice.lottery_id}");
                var newTotal = FiveStatus(choice).Select(ScoreUtils.ReviseOver1200).Sum();
                var newPt = choice.skill_point;
                var newProper = Proper(choice);
                lines.Add($"属性: {newTotal - totalValue}, PT: {newPt - pt}");
                // 统计适性
                foreach (var k in newProper.Keys)
                {
                    if (proper.ContainsKey(k) && proper[k] < newProper[k])
                        lines.Add($"{k} 适性提升: {properText[proper[k]]} -> {properText[newProper[k]]}");
                }
                // 统计白因子数 factor_id >= 1000000
                var whiteCount = 0;
                foreach (var pos in choice.effected_factor_array)
                    whiteCount += pos.factor_info_array.Count(x => x.factor_id >= 1000000);
                lines.Add($"白因子: {whiteCount}");
                // 统计Hint
                var tipsDict = SkillTipsToDict(choice.skill_tips_array);
                var newTips = AnalyzeSkillTips(tipsDict);
                lines.Add($"技能Hint: {newTips.Count}");
                sections.Add(string.Join(Environment.NewLine, lines));
            }
            EventLoggerDisplay.SetPanel(
                "succession",
                "继承选择",
                LiveDisplayContent.Text(string.Join(Environment.NewLine + Environment.NewLine, sections)));
        }

        /// <summary>
        /// 读取适性
        /// </summary>
        /// <param name="chara">当前角色状态</param>
        /// <returns>新的适性数据</returns>
        public static Dictionary<string, int> UpdateProper(SingleModeChara chara)
        {
            return new Dictionary<string, int>
            {
                { "短", chara.proper_distance_short },
                { "英", chara.proper_distance_mile },
                { "中", chara.proper_distance_middle },
                { "长", chara.proper_distance_long },
                { "逃", chara.proper_running_style_nige },
                { "追", chara.proper_running_style_oikomi },
                { "差", chara.proper_running_style_sashi },
                { "先", chara.proper_running_style_senko },
                { "芝", chara.proper_ground_turf },
                { "泥", chara.proper_ground_dirt }
            };
        }

        public static int FirstSelectIndex(ChoiceArray choice)
        {
            return FirstSelectIndex(choice.select_index_info_array) ?? 0;
        }

        static int? FirstSelectIndex(SingleModeSelectIndexInfo[]? infos)
        {
            return infos?.FirstOrDefault()?.select_index;
        }

        static int[] FiveStatus(SuccessionGainInfo info)
        {
            return [info.speed, info.stamina, info.power, info.guts, info.wiz];
        }

        static Dictionary<string, int> Proper(SuccessionGainInfo info)
        {
            return new Dictionary<string, int>
            {
                { "短", info.proper_distance_short },
                { "英", info.proper_distance_mile },
                { "中", info.proper_distance_middle },
                { "长", info.proper_distance_long },
                { "逃", info.proper_running_style_nige },
                { "追", info.proper_running_style_oikomi },
                { "差", info.proper_running_style_sashi },
                { "先", info.proper_running_style_senko },
                { "芝", info.proper_ground_turf },
                { "泥", info.proper_ground_dirt }
            };
        }

        // 当玩家选择选项时进行记录
        public static void UpdatePlayerChoice(SingleModeCheckEventRequestCommon request)
        {
            var choiceNumber = request.choice_number;
            Print($"选择选项 {choiceNumber}");
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
                    ret.Add(string.Format("连续事件出现 {0} 次", CardEventCount));
                    ret.Add(string.Format("走完 {0} 张卡", CardEventFinishCount));
                    if (InitTurn != 1 && n > 0)
                        ret.Add(string.Format("连续事件运气: {0}%", ((bn + bn_1) / 2 * 200 - 100).ToString("+#;-#;0")));
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
                        ret.Add(string.Format("剩余 {0} 个连续事件", CardEventRemaining));
                        ret.Add(string.Format("完成概率: {0}%", (pFinish * 100).ToString("0")));
                    }
                }
                else
                {
                    ret.Add("连续事件全部完成");
                }
            }
            return ret;
        }

        public static void UpdateRaceHistory(SingleRaceHistory[] history)
        {
            raceHistory = new List<int>();
            foreach (var h in history)
            {
                if (h.result_rank == 1)
                    raceHistory.Add(h.turn);
            }
            EventLoggerDisplay.Log($"当前已取胜 {raceHistory.Count} 场");
        }
    }
}
