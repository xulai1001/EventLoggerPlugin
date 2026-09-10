using Gallop;
using System.Drawing;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.Text;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using UmamusumeResponseAnalyzer;
using UmamusumeResponseAnalyzer.TerminalGui;
using TAttribute = Terminal.Gui.Drawing.Attribute;
using TColor = Terminal.Gui.Drawing.Color;

namespace EventLoggerPlugin;

static class SuccessionChoiceAnalyzer
{
    static readonly string[] ProperRanks = ["", "G", "F", "E", "D", "C", "B", "A", "S"];
    
    // ExtraSkillTips -> SkillTips
    public static SkillTips IntoBaseSkillTips(ExtraSkillTips tips) => new SkillTips
    {
        group_id = tips.group_id,
        level = tips.level,
        rarity = tips.rarity
    };

    public static ValueTask Analyze(SingleModeCheckEventResponse response)
        => Publish(new(
            response.data?.chara_info,
            response.data?.unchecked_event_array,
            response.data?.select_index_info_array));

    public static ValueTask Analyze(SingleModeExecCommandResponse response)
        => Publish(new(
            response.data?.chara_info,
            response.data?.unchecked_event_array,
            null));

    public static ValueTask Analyze(SingleModeLoadResponse response)
    {
        var common = response.data?.single_mode_load_common;
        return Publish(new(
            common?.chara_info,
            common?.unchecked_event_array,
            null));
    }

    static ValueTask Publish(EventLoggerSnapshot snapshot)
    {
        if (!TryCreateColumns(snapshot, out var columns))
            return ValueTask.CompletedTask;

        EventLoggerDisplay.SetPanel(
            "succession",
            "继承选择",
            SuccessionChoiceDisplayRenderer.Render(columns),
            fullBleed: true,
            switchToWorkspace: true);
        return ValueTask.CompletedTask;
    }

    static bool TryCreateColumns(
        EventLoggerSnapshot snapshot,
        out SuccessionChoiceColumn[] columns)
    {
        columns = [];
        if (snapshot.CharaInfo is not { skill_tips_array: { } currentTips } chara ||
            snapshot.UncheckedEvents?.FirstOrDefault()?.succession_event_info is not { } succession ||
            succession.succession_gain_info_array is not { Length: > 0 } choices)
            return false;

        var currentProper = Proper(chara);
        if (currentTips.Any(tip => tip is null) ||
            currentProper.Any(value => value.Rank is < 0 or > 8) ||
            choices.Any(choice =>
                choice is null ||
                Proper(choice).Any(value => value.Rank is < 0 or > 8) ||
                choice.effected_factor_array is not { } factors ||
                factors.Any(position =>
                    position?.factor_info_array is not { } factorInfos ||
                    factorInfos.Any(factor => factor is null)) ||
                choice.skill_tips_array is not { } choiceTips ||
                choiceTips.Any(tip => tip is null)))
            return false;

        var currentTotal = FiveStatus(chara).Select(ScoreUtils.ReviseOver1200).Sum();
        var currentTipLevels = currentTips.ToDictionary(TipKey, tip => tip.level);
        var skillLookup = Database.Skills.Apply(new()
        {
            skill_tips_array = [
                .. choices.SelectMany(choice => choice.skill_tips_array)
                    .DistinctBy(TipKey)
                    .Select(tkey => IntoBaseSkillTips(tkey))
            ],
            skill_array = [],
            chara_effect_id_array = [],
        });
        columns = new SuccessionChoiceColumn[choices.Length];
        for (var index = 0; index < choices.Length; index++)
        {
            var choice = choices[index];
            var choiceProper = Proper(choice);
            var properGains = new List<SuccessionProperGain>();
            for (var properIndex = 0; properIndex < currentProper.Length; properIndex++)
            {
                if (currentProper[properIndex].Rank >= choiceProper[properIndex].Rank)
                    continue;

                properGains.Add(new(
                    currentProper[properIndex].Name,
                    ProperRanks[currentProper[properIndex].Rank],
                    ProperRanks[choiceProper[properIndex].Rank]));
            }

            var skillHints = choice.skill_tips_array
                .Where(tip =>
                    !currentTipLevels.TryGetValue(TipKey(tip), out var currentLevel) ||
                    currentLevel != tip.level)
                .Select(tip => new SuccessionSkillHint(
                    SkillName(skillLookup, IntoBaseSkillTips(tip)),
                    currentTipLevels.GetValueOrDefault(TipKey(tip)),
                    tip.level))
                .ToArray();
            var whiteFactors = choice.effected_factor_array
                .SelectMany(position => position.factor_info_array)
                .Where(factor => factor.factor_id >= 1_000_000)
                .Select(CreateWhiteFactor)
                .ToArray();
            columns[index] = new(
                choice.lottery_id,
                FiveStatus(choice).Select(ScoreUtils.ReviseOver1200).Sum() - currentTotal,
                choice.skill_point - chara.skill_point,
                [.. properGains],
                skillHints,
                whiteFactors);
        }

        return true;
    }

    static int TipKey(SkillTips tip) => tip.group_id * 10 + tip.rarity;

    static int TipKey(ExtraSkillTips tip) => tip.group_id * 10 + tip.rarity;

    static string SkillName(SkillManager skills, SkillTips tip)
    {
        var matches = skills.FindByGroup(tip.group_id, tip.rarity);
        return (matches.Where(skill => skill.Rate > 0).MinBy(skill => skill.Rate) ??
                matches.FirstOrDefault())?.DisplayName
            ?? $"未知技能 #{tip.group_id}/{tip.rarity}";
    }

    static SuccessionWhiteFactor CreateWhiteFactor(FactorInfo factor)
    {
        if (!Database.FactorIds.TryGetValue(factor.factor_id, out var displayName))
            return new($"未知因子 #{factor.factor_id}", $" Lv.{factor.level}");

        var starsStart = displayName.IndexOf('★');
        return starsStart < 0
            ? new(displayName, string.Empty)
            : new(displayName[..starsStart], displayName[starsStart..]);
    }

    static SuccessionWhiteFactor CreateWhiteFactor(ExtraFactorInfo factor)
    {
        if (!Database.FactorIds.TryGetValue(factor.factor_id, out var displayName))
            return new($"未知因子 #{factor.factor_id}", $" Lv.{factor.level}");

        var starsStart = displayName.IndexOf('★');
        return starsStart < 0
            ? new(displayName, string.Empty)
            : new(displayName[..starsStart], displayName[starsStart..]);
    }

    static int[] FiveStatus(SingleModeChara chara)
        => [chara.speed, chara.stamina, chara.power, chara.guts, chara.wiz];

    static int[] FiveStatus(SuccessionGainInfo choice)
        => [choice.speed, choice.stamina, choice.power, choice.guts, choice.wiz];

    static ProperValue[] Proper(SingleModeChara chara)
        =>
        [
            new("短", chara.proper_distance_short),
            new("英", chara.proper_distance_mile),
            new("中", chara.proper_distance_middle),
            new("长", chara.proper_distance_long),
            new("逃", chara.proper_running_style_nige),
            new("追", chara.proper_running_style_oikomi),
            new("差", chara.proper_running_style_sashi),
            new("先", chara.proper_running_style_senko),
            new("芝", chara.proper_ground_turf),
            new("泥", chara.proper_ground_dirt),
        ];

    static ProperValue[] Proper(SuccessionGainInfo choice)
        =>
        [
            new("短", choice.proper_distance_short),
            new("英", choice.proper_distance_mile),
            new("中", choice.proper_distance_middle),
            new("长", choice.proper_distance_long),
            new("逃", choice.proper_running_style_nige),
            new("追", choice.proper_running_style_oikomi),
            new("差", choice.proper_running_style_sashi),
            new("先", choice.proper_running_style_senko),
            new("芝", choice.proper_ground_turf),
            new("泥", choice.proper_ground_dirt),
        ];

    readonly record struct ProperValue(string Name, int Rank);
}

readonly record struct SuccessionChoiceColumn(
    int LotteryId,
    int StatGain,
    int SkillPointGain,
    SuccessionProperGain[] ProperGains,
    SuccessionSkillHint[] SkillHints,
    SuccessionWhiteFactor[] WhiteFactors);

readonly record struct SuccessionProperGain(string Name, string OldRank, string NewRank);
readonly record struct SuccessionSkillHint(string Name, int OldLevel, int NewLevel);
readonly record struct SuccessionWhiteFactor(string Name, string Accent);

static class SuccessionChoiceDisplayRenderer
{
    const int ColumnWidth = 52;
    const int CellWidth = ColumnWidth + 2;
    const int ColumnStride = CellWidth + 1;
    const int HeaderY = 2;
    const int SummaryY = 4;
    static readonly RenderLine Separator = new(
        [new("------ 继承选择 ------", TextColor.Lime)]);

    public static WorkspaceContent Render(SuccessionChoiceColumn[] columns)
        => new(() => CreateView(columns));

    static View CreateView(SuccessionChoiceColumn[] choices)
        => new SuccessionChoiceViewport(choices.Select(CreateColumn).ToArray());

    static void AddLines(
        View view,
        RenderLine[] lines,
        int x,
        int y,
        TAttribute normal)
    {
        for (var index = 0; index < lines.Length; index++)
            AddRenderLine(view, lines[index], x, y + index, ColumnWidth, normal);
    }

    static Line AddRule(
        View view,
        int x,
        int y,
        int length,
        Orientation orientation,
        TAttribute attribute)
    {
        var line = new Line
        {
            X = x,
            Y = y,
            Length = length,
            Orientation = orientation,
            Style = LineStyle.Single,
            LineAttribute = attribute,
            TabStop = TabBehavior.NoStop,
        };
        view.Add(line);
        return line;
    }

    static void AddRenderLine(
        View view,
        RenderLine line,
        int x,
        int y,
        int width,
        TAttribute normal)
    {
        var offset = 0;
        foreach (var run in line.Runs)
        {
            var runWidth = run.Text.GetColumns();
            var visibleWidth = Math.Min(runWidth, width - offset);
            if (visibleWidth <= 0)
                break;

            var label = new Label
            {
                X = x + offset,
                Y = y,
                Width = visibleWidth,
                Height = 1,
                Text = run.Text,
                TabStop = TabBehavior.NoStop,
            };
            label.SetScheme(new Scheme(Attribute(run.Color, normal)));
            view.Add(label);

            offset += visibleWidth;
            if (visibleWidth < runWidth)
                break;
        }
    }

    sealed class SuccessionChoiceViewport : View
    {
        const int MinimumFactorRows = 6;

        readonly int tableWidth;
        readonly int summaryHeight;
        readonly int maximumHintHeight;
        readonly int hintItemsY;
        readonly Line factorRule;
        readonly Line bottomRule;
        readonly Line[] verticalRules;
        readonly View[] factorHeaders;
        readonly SuccessionDetailViewport[] hintViews;
        readonly SuccessionDetailViewport[] factorViews;

        public SuccessionChoiceViewport(RenderColumn[] columns)
        {
            tableWidth = columns.Length * ColumnStride + 1;
            summaryHeight = columns.Max(column => column.Summary.Length);
            maximumHintHeight = columns.Max(column => column.SkillHints.Length);
            var hintRuleY = SummaryY + summaryHeight;
            var hintHeaderY = hintRuleY + 1;
            hintItemsY = hintHeaderY + 1;

            Id = "event-logger-succession";
            Width = Dim.Fill();
            Height = Dim.Fill();
            CanFocus = true;
            TabStop = TabBehavior.TabGroup;
            ViewportSettings = ViewportSettingsFlags.HasHorizontalScrollBar;
            SetContentSize(new Size(tableWidth, 1));

            var normal = GetAttributeForRole(VisualRole.Normal);
            SetScheme(new Scheme(normal));
            AddRenderLine(this, Separator, 0, 0, tableWidth, normal);
            AddRule(this, 0, 1, tableWidth, Orientation.Horizontal, normal);
            AddRule(this, 0, 3, tableWidth, Orientation.Horizontal, normal);
            AddRule(this, 0, hintRuleY, tableWidth, Orientation.Horizontal, normal);
            factorRule = AddRule(this, 0, hintItemsY, tableWidth, Orientation.Horizontal, normal);
            bottomRule = AddRule(this, 0, hintItemsY + 2, tableWidth, Orientation.Horizontal, normal);
            verticalRules =
            [
                .. Enumerable.Range(0, columns.Length + 1).Select(columnIndex => AddRule(
                    this,
                    columnIndex * ColumnStride,
                    1,
                    hintItemsY + 1,
                    Orientation.Vertical,
                    normal)),
            ];
            factorHeaders = new View[columns.Length];
            hintViews = new SuccessionDetailViewport[columns.Length];
            factorViews = new SuccessionDetailViewport[columns.Length];

            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++)
            {
                var column = columns[columnIndex];
                var contentX = columnIndex * ColumnStride + 2;
                var detailX = columnIndex * ColumnStride + 1;
                AddRenderLine(this, column.Header, contentX, HeaderY, ColumnWidth, normal);
                AddLines(this, column.Summary, contentX, SummaryY, normal);
                AddRenderLine(this, column.SkillHeader, contentX, hintHeaderY, ColumnWidth, normal);

                var factorHeader = new View
                {
                    X = contentX,
                    Y = hintItemsY + 1,
                    Width = ColumnWidth,
                    Height = 1,
                    TabStop = TabBehavior.NoStop,
                };
                factorHeader.SetScheme(new Scheme(normal));
                AddRenderLine(factorHeader, column.FactorHeader, 0, 0, ColumnWidth, normal);
                Add(factorHeader);
                factorHeaders[columnIndex] = factorHeader;

                var hintView = new SuccessionDetailViewport(
                    $"event-logger-succession-hint-{columnIndex}",
                    column.SkillHints,
                    normal)
                {
                    X = detailX,
                    Y = hintItemsY,
                    Width = CellWidth,
                };
                Add(hintView);
                hintViews[columnIndex] = hintView;

                var factorView = new SuccessionDetailViewport(
                    $"event-logger-succession-factor-{columnIndex}",
                    column.WhiteFactors,
                    normal)
                {
                    X = detailX,
                    Width = CellWidth,
                };
                Add(factorView);
                factorViews[columnIndex] = factorView;
            }
        }

        protected override void OnSubViewLayout(LayoutEventArgs args)
        {
            var visibleWidth = Math.Max(1, Viewport.Width);
            var visibleHeight = Math.Max(1, Viewport.Height);
            var contentSize = new Size(
                Math.Max(visibleWidth, tableWidth),
                visibleHeight);
            if (GetContentSize() != contentSize)
                SetContentSize(contentSize);

            var maxX = Math.Max(0, contentSize.Width - visibleWidth);
            if (Viewport.X > maxX || Viewport.Y != 0)
                Viewport = new Rectangle(
                    Math.Min(Viewport.X, maxX),
                    0,
                    Viewport.Width,
                    Viewport.Height);

            var availableRows = Math.Max(0, visibleHeight - 9 - summaryHeight);
            var reservedFactorRows = Math.Min(MinimumFactorRows, availableRows);
            var hintRows = Math.Min(
                maximumHintHeight,
                Math.Max(0, availableRows - reservedFactorRows));
            var factorRows = availableRows - hintRows;
            var factorRuleY = hintItemsY + hintRows;
            var factorHeaderY = factorRuleY + 1;
            var factorItemsY = factorHeaderY + 1;
            var bottomY = factorItemsY + factorRows;

            factorRule.Y = factorRuleY;
            bottomRule.Y = bottomY;
            foreach (var verticalRule in verticalRules)
                verticalRule.Length = bottomY;
            for (var columnIndex = 0; columnIndex < hintViews.Length; columnIndex++)
            {
                hintViews[columnIndex].Height = hintRows;
                factorHeaders[columnIndex].Y = factorHeaderY;
                factorViews[columnIndex].Y = factorItemsY;
                factorViews[columnIndex].Height = factorRows;
            }

            base.OnSubViewLayout(args);
        }

        protected override bool OnMouseEvent(Mouse mouse)
        {
            if (mouse.Flags.HasFlag(MouseFlags.WheeledRight) && HorizontalScrollBar.Visible)
            {
                HorizontalScrollBar.Value += HorizontalScrollBar.Increment;
                return true;
            }

            if (mouse.Flags.HasFlag(MouseFlags.WheeledLeft) && HorizontalScrollBar.Visible)
            {
                HorizontalScrollBar.Value -= HorizontalScrollBar.Increment;
                return true;
            }

            return base.OnMouseEvent(mouse);
        }
    }

    sealed class SuccessionDetailViewport : View
    {
        readonly int contentHeight;

        public SuccessionDetailViewport(string id, RenderLine[] lines, TAttribute normal)
        {
            contentHeight = lines.Length;
            Id = id;
            CanFocus = true;
            TabStop = TabBehavior.TabStop;
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar;
            SetScheme(new Scheme(normal));
            SetContentSize(new Size(CellWidth, contentHeight));
            AddLines(this, lines, 1, 0, normal);

            AddCommand(Command.PageUp, () => Scroll(this, Command.PageUp));
            AddCommand(Command.PageDown, () => Scroll(this, Command.PageDown));
            AddCommand(Command.Start, () => Scroll(this, Command.Start));
            AddCommand(Command.End, () => Scroll(this, Command.End));
            KeyBindings.ReplaceCommands(Key.PageUp, Command.PageUp);
            KeyBindings.ReplaceCommands(Key.PageDown, Command.PageDown);
            KeyBindings.ReplaceCommands(Key.Home, Command.Start);
            KeyBindings.ReplaceCommands(Key.End, Command.End);
        }

        protected override void OnSubViewLayout(LayoutEventArgs args)
        {
            var visibleWidth = Math.Max(1, Viewport.Width);
            var visibleHeight = Math.Max(0, Viewport.Height);
            var contentSize = new Size(
                visibleWidth,
                Math.Max(visibleHeight, contentHeight));
            if (GetContentSize() != contentSize)
                SetContentSize(contentSize);

            var maxY = Math.Max(0, contentSize.Height - visibleHeight);
            if (Viewport.X != 0 || Viewport.Y > maxY)
                Viewport = new Rectangle(
                    0,
                    Math.Min(Viewport.Y, maxY),
                    Viewport.Width,
                    Viewport.Height);

            base.OnSubViewLayout(args);
        }

        protected override bool OnMouseEvent(Mouse mouse)
        {
            if (mouse.Flags.HasFlag(MouseFlags.WheeledDown) && VerticalScrollBar.Visible)
            {
                VerticalScrollBar.Value += VerticalScrollBar.Increment;
                return true;
            }

            if (mouse.Flags.HasFlag(MouseFlags.WheeledUp) && VerticalScrollBar.Visible)
            {
                VerticalScrollBar.Value -= VerticalScrollBar.Increment;
                return true;
            }

            return base.OnMouseEvent(mouse);
        }
    }

    static bool Scroll(View view, Command command)
    {
        var maxY = Math.Max(0, view.GetContentSize().Height - view.Viewport.Height);
        var page = Math.Max(1, view.Viewport.Height);
        var next = command switch
        {
            Command.PageUp => Math.Max(0, view.Viewport.Y - page),
            Command.PageDown => Math.Min(maxY, view.Viewport.Y + page),
            Command.Start => 0,
            Command.End => maxY,
            _ => view.Viewport.Y,
        };
        if (next == view.Viewport.Y)
            return false;

        view.Viewport = new Rectangle(
            view.Viewport.X,
            next,
            view.Viewport.Width,
            view.Viewport.Height);
        view.SetNeedsDraw();
        return true;
    }

    static RenderColumn CreateColumn(SuccessionChoiceColumn column)
    {
        var summary = new List<RenderLine>
        {
            new(
            [
                new("属性: ", TextColor.Normal),
                new(column.StatGain.ToString(), TextColor.Cyan),
                new($", PT: {column.SkillPointGain}", TextColor.Normal),
            ]),
        };
        summary.AddRange(column.ProperGains.Select(gain => new RenderLine(
            [new($"{gain.Name} 适性提升: {gain.OldRank} -> {gain.NewRank}", TextColor.Yellow)])));

        return new(
            new([new("继承结果 ", TextColor.Normal), new(column.LotteryId.ToString(), TextColor.Lime)]),
            [.. summary],
            new([new("技能Hint详情: ", TextColor.Normal), new(column.SkillHints.Length.ToString(), TextColor.Cyan)]),
            [.. column.SkillHints.Select(hint => new RenderLine(
            [
                new($"  {hint.Name}", TextColor.Normal),
                new($" Lv.{hint.OldLevel} -> Lv.{hint.NewLevel}", TextColor.Cyan),
            ]))],
            new([new("白因子详情: ", TextColor.Normal), new(column.WhiteFactors.Length.ToString(), TextColor.Cyan)]),
            [.. column.WhiteFactors.Select(CreateFactorLine)]);
    }

    static RenderLine CreateFactorLine(SuccessionWhiteFactor factor)
    {
        var runs = new List<RenderRun> { new($"  {factor.Name}", TextColor.Normal) };
        if (factor.Accent.Length > 0)
            runs.Add(new(factor.Accent, TextColor.Cyan));
        return new([.. runs]);
    }

    static TAttribute Attribute(TextColor color, TAttribute normal)
        => color switch
        {
            TextColor.Lime => WithForeground(new(StandardColor.BrightGreen), normal),
            TextColor.Cyan => WithForeground(new(StandardColor.BrightCyan), normal),
            TextColor.Yellow => WithForeground(new(StandardColor.BrightYellow), normal),
            _ => normal,
        };

    static TAttribute WithForeground(TColor foreground, TAttribute normal)
        => new(foreground, normal.Background, normal.Style);

    sealed record RenderColumn(
        RenderLine Header,
        RenderLine[] Summary,
        RenderLine SkillHeader,
        RenderLine[] SkillHints,
        RenderLine FactorHeader,
        RenderLine[] WhiteFactors);
    readonly record struct RenderLine(RenderRun[] Runs);
    readonly record struct RenderRun(string Text, TextColor Color);

    enum TextColor
    {
        Normal,
        Lime,
        Cyan,
        Yellow,
    }
}
