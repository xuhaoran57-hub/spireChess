using System;
using System.Collections.Generic;
using SpireChess.Run;

namespace SpireChess.UI.Run
{
    public enum RunUiActionType
    {
        None,
        SelectReward,
        SkipReward,
        SelectRelic,
        SkipRelic,
        SelectEvent,
        ApplyEnhancement,
        SkipEnhancement,
        SelectRest,
        ContinueAfterBattle,
        RetryBoss,
        ContinueToNextFloor,
        StartNewRun
    }

    public sealed class RunScreenState
    {
        public string Title { get; set; } = "三层远征";
        public string ResourceSummary { get; set; } = string.Empty;
        public string ProgressSummary { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RouteHint { get; set; } = string.Empty;
        public int MaximumColumn { get; set; }
        public IReadOnlyList<RunMapNodeState> Nodes { get; set; } =
            Array.Empty<RunMapNodeState>();
        public IReadOnlyList<RunMapEdgeState> Edges { get; set; } =
            Array.Empty<RunMapEdgeState>();
        public IReadOnlyList<RunRelicState> Relics { get; set; } =
            Array.Empty<RunRelicState>();
        public RunChoiceOverlayState Choice { get; set; }
        public RunSummaryState Summary { get; set; } = new RunSummaryState();
    }

    public sealed class RunMapNodeState
    {
        public string NodeId { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string RouteText { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }
        public RunNodeType Type { get; set; }
        public RunNodeStatus Status { get; set; }
        public bool IsInteractable { get; set; }
    }

    public sealed class RunMapEdgeState
    {
        public string FromNodeId { get; set; }
        public string ToNodeId { get; set; }
        public RunNodeStatus FromStatus { get; set; }
        public RunNodeStatus ToStatus { get; set; }
    }

    public sealed class RunRelicState
    {
        public string RelicId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string GradeText { get; set; }
        public string CategoryText { get; set; }
        public string ProgressText { get; set; }
    }

    public sealed class RunChoiceOverlayState
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public IReadOnlyList<RunChoiceOptionState> Options { get; set; } =
            Array.Empty<RunChoiceOptionState>();
    }

    public sealed class RunChoiceOptionState
    {
        public string Label { get; set; }
        public string Description { get; set; }
        public string Badge { get; set; }
        public bool IsInteractable { get; set; } = true;
        public RunUiActionType Action { get; set; }
        public string PrimaryId { get; set; }
        public string SecondaryId { get; set; }
    }

    public sealed class RunSummaryState
    {
        public string Text { get; set; } = string.Empty;
        public bool IsActionVisible { get; set; }
        public string ActionLabel { get; set; } = string.Empty;
        public RunUiActionType Action { get; set; }
    }
}
