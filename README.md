# EventLoggerPlugin

`EventLoggerPlugin` records event, training, succession, and race statistics in its own workspace.

Its manifest declares optional linkage to `LegendScenarioAnalyzer` and `RamenScenarioAnalyzer`. For each supported response, EventLogger captures immutable display data for `(single_mode_chara_id, turn)` and updates only its producer-owned part for that display ID; rendering never reads the global EventLogger display source to select a target. Scenario summaries contributed through those integrations use each analyzer's `EventLogger` Extra section. Missing analyzers do not prevent EventLogger from loading or using its own workspace, and each analyzer is checked independently with `IPluginContext.IsPluginAvailable`.

Complete succession choices from `check_event`, `exec_command`, and `load` are published by priority `3` analyzers after `EventResponseAnalyzer` (priority `2`). Candidates render side by side through native Terminal.Gui Line and Label views in a square-bordered table with 52-column content cells; each candidate lists skill Hint changes before white-factor names without displaying factor source positions. The outer table exposes only a horizontal scrollbar, while each candidate's Hint rows and white-factor rows use separate native vertical scroll views (four total). Hint rows receive spare height first while each factor region keeps at least six content rows; wheel input is consumed only by the hovered detail region with a visible vertical scrollbar, or by the outer table with a visible horizontal scrollbar. Incomplete succession data does not create the panel or switch workspaces.

An `effect_type`-only succession event records its baseline and reports the revised five-stat gain together with the skill-point gain in the linked ScenarioAnalyzer `Extra` rows after the following response.

## Build

The repository pins the Host and linked plugin sources with Git submodules. From the repository root after cloning:

```powershell
git -c core.longpaths=true submodule update --init --recursive
dotnet build .\EventLoggerPlugin.csproj -c Release -m:1 -p:RuntimeIdentifier=win-x64 -p:SelfContained=false -p:PlatformTarget=AnyCPU -p:DeployUraPluginToLocalAppDataOnBuild=false
```

## 验证与发布

在 Windows 仓库根执行 `act workflow_dispatch --artifact-server-path "$env:TEMP/ura-act-artifacts"`。本地与 GitHub 使用同一份 workflow；版本 tag 触发 GitHub Release 发布。环境要求、共用 workflow 本地映射和发布规则见 [URA plugin workflows](https://github.com/URA-Plugins/.github/blob/v1/README.md)。
