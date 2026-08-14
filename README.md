# EventLoggerPlugin

`EventLoggerPlugin` records event, training, succession, and race statistics in its own workspace.

Its manifest declares optional linkage to `LegendScenarioAnalyzer` and `RamenScenarioAnalyzer`. When either analyzer is available in the shared plugin context, EventLogger registers a persistent modifier through that analyzer's public display API. Missing analyzers do not prevent EventLogger from loading or using its own workspace, and each analyzer is checked independently with `IPluginContext.IsPluginAvailable`.
