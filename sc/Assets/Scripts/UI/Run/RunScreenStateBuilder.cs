using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;
using SpireChess.Run;

namespace SpireChess.UI.Run
{
    public static class RunScreenStateBuilder
    {
        public static RunScreenState Build(
            RunSession run,
            ConfigService configs,
            string statusMessage)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }
            if (configs == null)
            {
                throw new ArgumentNullException(nameof(configs));
            }

            var state = run.State;
            var map = state.CurrentMap;
            var nodes = map == null
                ? new List<RunMapNodeState>()
                : map.Nodes.Select(node => BuildNode(state, configs, node)).ToList();
            var nodeById = nodes.ToDictionary(node => node.NodeId);
            var edges = new List<RunMapEdgeState>();
            if (map != null)
            {
                foreach (var node in map.Nodes)
                {
                    foreach (var nextId in node.NextNodeIds)
                    {
                        if (!nodeById.TryGetValue(node.Id, out var from) ||
                            !nodeById.TryGetValue(nextId, out var to))
                        {
                            continue;
                        }
                        edges.Add(new RunMapEdgeState
                        {
                            FromNodeId = node.Id,
                            ToNodeId = nextId,
                            FromStatus = from.Status,
                            ToStatus = to.Status
                        });
                    }
                }
            }

            var completedShops = nodes.Count(node =>
                node.Type == RunNodeType.Shop && node.Status == RunNodeStatus.Resolved);
            var completedCombats = nodes.Count(node =>
                IsCombat(node.Type) && node.Status == RunNodeStatus.Resolved);
            var shopCount = map?.RuleProfile.ShopCount ?? 0;
            var combatCount = map?.RuleProfile.CombatCount ?? 0;

            return new RunScreenState
            {
                Title = $"第 {state.Floor} 层 · 三层远征",
                ResourceSummary =
                    $"生命 {state.Health}/{state.MaxHealth}   商店回合 {state.ShopTurn}   " +
                    $"战绩 {state.Statistics.BattlesWon}胜/{state.Statistics.BattlesNotWon}未胜",
                ProgressSummary =
                    $"本层商店 {completedShops}/{shopCount}   固定战斗 {completedCombats}/{combatCount}   " +
                    $"地图步数 {state.MapStep}",
                Status = statusMessage ?? string.Empty,
                RouteHint = "C2/C5 选择遭遇机制 · C4 选择强攻、奇遇或保守路线 · 事件可能触发额外战斗",
                MaximumColumn = map?.Nodes.Count > 0
                    ? map.Nodes.Max(node => node.Column)
                    : 1,
                Nodes = nodes,
                Edges = edges,
                Relics = BuildRelics(state, configs),
                Choice = BuildChoice(run),
                Summary = BuildSummary(state)
            };
        }

        private static RunMapNodeState BuildNode(
            RunState state,
            ConfigService configs,
            MapNodeDefinition node)
        {
            var status = state.MapProgress.GetStatus(node.Id);
            var title = ToNodeTypeText(node.Type);
            var subtitle = string.Empty;
            if (node.Type == RunNodeType.Shop)
            {
                subtitle = "补给与整备";
            }
            else if (IsCombat(node.Type) &&
                     configs.TryGetEncounter(node.PayloadId, out var encounter))
            {
                title = $"第 {node.CombatIndex} 战 · {title}";
                subtitle = encounter.Name;
            }
            else
            {
                subtitle = node.Type == RunNodeType.Enhance
                    ? "永久强化随从"
                    : node.Type == RunNodeType.Event
                        ? "未知机会与风险"
                        : node.Type == RunNodeType.Rest
                            ? "恢复与整备"
                            : node.Id;
            }

            return new RunMapNodeState
            {
                NodeId = node.Id,
                Title = title,
                Subtitle = subtitle,
                RouteText = ToRouteTagText(node.RouteTag),
                Column = node.Column,
                Row = node.Row,
                Type = node.Type,
                Status = status,
                IsInteractable = state.Phase == RunPhase.MapSelection &&
                                 status == RunNodeStatus.Reachable
            };
        }

        private static IReadOnlyList<RunRelicState> BuildRelics(
            RunState state,
            ConfigService configs)
        {
            var result = new List<RunRelicState>();
            foreach (var owned in state.OwnedRelics)
            {
                if (!configs.TryGetRelic(owned.RelicId, out var config))
                {
                    continue;
                }
                var progress = config.Interval > 0
                    ? $"进度 {Math.Min(config.Interval, owned.ShopProgress)}/{config.Interval}"
                    : owned.ActivationCount > 0
                        ? $"已触发 {owned.ActivationCount} 次"
                        : "持续生效";
                result.Add(new RunRelicState
                {
                    RelicId = owned.RelicId,
                    Name = config.Name,
                    Description = config.Description,
                    GradeText = config.Grade == "Crown" ? "冠冕" : "奇物",
                    CategoryText = ToCategoryText(config.Category),
                    ProgressText = progress
                });
            }
            return result;
        }

        private static RunChoiceOverlayState BuildChoice(RunSession run)
        {
            var state = run.State;
            if (state.Phase == RunPhase.RewardChoice &&
                state.PendingRewardChoice != null)
            {
                var options = new List<RunChoiceOptionState>();
                foreach (var candidate in state.PendingRewardChoice.Candidates)
                {
                    if (candidate.RequiresOwnedMinionTarget)
                    {
                        foreach (var card in run.Shop.Collection.Battle.Where(card => card != null))
                        {
                            options.Add(Option(
                                candidate.DisplayText,
                                $"目标：{card.Minion.Name}",
                                RunUiActionType.SelectReward,
                                candidate.CandidateId,
                                card.InstanceId));
                        }
                    }
                    else
                    {
                        options.Add(Option(
                            candidate.DisplayText,
                            string.Empty,
                            RunUiActionType.SelectReward,
                            candidate.CandidateId));
                    }
                }
                if (state.PendingRewardChoice.AllowSkip)
                {
                    options.Add(Option(
                        "跳过奖励",
                        "不获得本次奖励并继续结算。",
                        RunUiActionType.SkipReward));
                }
                return Choice("选择一项奖励", "奖励处理完成前无法继续选择地图节点。", options);
            }

            if (state.Phase == RunPhase.RelicChoice &&
                state.PendingRelicChoice != null)
            {
                var pending = state.PendingRelicChoice;
                var options = pending.Candidates.Select(candidate => new RunChoiceOptionState
                {
                    Label = candidate.Name,
                    Description = candidate.Description,
                    Badge = pending.HealthCost > 0
                        ? $"{ToGradeText(candidate.Grade)} · 生命 {state.Health} → {state.Health - pending.HealthCost}"
                        : $"{ToGradeText(candidate.Grade)} · {ToCategoryText(candidate.Category)}",
                    Action = RunUiActionType.SelectRelic,
                    PrimaryId = candidate.CandidateId,
                    IsInteractable = pending.HealthCost < state.Health
                }).ToList();
                if (pending.AllowSkip)
                {
                    options.Add(Option(
                        "离开",
                        "不获得遗珍，也不会失去生命。",
                        RunUiActionType.SkipRelic));
                }
                return Choice(
                    pending.HealthCost > 0 ? "以生命换取遗珍" : "选择一件 Boss 遗珍",
                    pending.HealthCost > 0
                        ? $"选择具体遗珍时失去 {pending.HealthCost} 点生命；查看候选不扣血。"
                        : "冠冕级遗珍会在后续楼层持续改变规则。",
                    options);
            }

            if (state.Phase == RunPhase.EventChoice &&
                state.PendingEventChoice != null)
            {
                var pending = state.PendingEventChoice;
                var options = pending.Config.Options.Select(option => Option(
                    option.Label,
                    BuildEventOptionDescription(option),
                    RunUiActionType.SelectEvent,
                    pending.Config.Id,
                    option.Id)).ToList();
                return Choice(pending.Config.Name, pending.Config.Description, options);
            }

            if (state.Phase == RunPhase.EnhanceChoice &&
                state.PendingEnhanceChoice != null)
            {
                var options = new List<RunChoiceOptionState>();
                foreach (var recipe in state.PendingEnhanceChoice.Recipes)
                {
                    foreach (var card in run.Shop.Collection.Battle.Where(card => card != null))
                    {
                        options.Add(Option(
                            recipe.Name,
                            $"目标：{card.Minion.Name}",
                            RunUiActionType.ApplyEnhancement,
                            recipe.Id,
                            card.InstanceId));
                    }
                }
                if (state.PendingEnhanceChoice.NodeConfig.AllowSkip)
                {
                    options.Add(Option(
                        "离开锻造台",
                        "不进行永久强化。",
                        RunUiActionType.SkipEnhancement));
                }
                return Choice("选择锻造配方和目标", "锻造会永久修改当前持有的随从。", options);
            }

            if (state.Phase == RunPhase.RestChoice &&
                state.PendingRestChoice != null)
            {
                var options = state.PendingRestChoice.Config.Options.Select(option => Option(
                    option.Label,
                    option.MaxHealth > 0
                        ? $"最大生命 +{option.MaxHealth}，恢复 {option.Heal}"
                        : option.Heal > 0 ? $"恢复 {option.Heal} 点生命" : "不修改生命",
                    RunUiActionType.SelectRest,
                    option.Id)).ToList();
                return Choice("选择恢复方式", $"当前生命 {state.Health}/{state.MaxHealth}", options);
            }

            return null;
        }

        private static RunSummaryState BuildSummary(RunState state)
        {
            if (state.Phase == RunPhase.MapSelection)
            {
                return new RunSummaryState
                {
                    Text = "选择高亮节点继续；未选择的互斥路线会在进入后锁定。"
                };
            }
            if (state.Phase == RunPhase.FloorComplete)
            {
                return Summary(
                    $"第 {state.Floor} 层完成 · Boss 已击败，遗珍已结算",
                    "进入下一层",
                    RunUiActionType.ContinueToNextFloor);
            }
            if (state.Phase == RunPhase.BattleResult)
            {
                var isBossRetry = state.CurrentMap.TryGetNode(
                    state.CurrentNodeId,
                    out var node) &&
                    node.Type == RunNodeType.Boss &&
                    !state.CurrentAttempt.NodeResolved;
                return Summary(
                    BuildResultSummary(state),
                    isBossRetry ? "再次挑战" : "继续前进",
                    isBossRetry
                        ? RunUiActionType.RetryBoss
                        : RunUiActionType.ContinueAfterBattle);
            }
            if (state.Phase == RunPhase.RunWon || state.Phase == RunPhase.RunLost)
            {
                return Summary(
                    BuildResultSummary(state),
                    "重新开始",
                    RunUiActionType.StartNewRun);
            }
            return new RunSummaryState
            {
                Text = BuildResultSummary(state)
            };
        }

        private static string BuildResultSummary(RunState state)
        {
            if (state.Phase == RunPhase.RunWon)
            {
                return $"三层通关 · {state.Statistics.BattlesWon} 胜 / " +
                       $"{state.Statistics.BattlesNotWon} 未胜 · 击败 " +
                       $"{state.Statistics.BossesDefeated} Boss · 三连 {state.Statistics.TriplesFormed}";
            }
            if (state.Phase == RunPhase.RunLost)
            {
                return $"单局失败：止步第 {state.Floor} 层 · " +
                       $"{state.Statistics.BattlesWon} 胜 / {state.Statistics.BattlesNotWon} 未胜";
            }
            if (state.LastSettlement == null)
            {
                return "等待节点结算。";
            }
            var outcome = state.LastSettlement.PlayerWon ? "胜利" : "未胜利";
            var reward = string.IsNullOrWhiteSpace(state.LastRewardSummary)
                ? string.Empty
                : $"；{state.LastRewardSummary}";
            return $"{outcome}；{state.LastSettlement.BuildDamageText()}{reward}";
        }

        private static RunChoiceOverlayState Choice(
            string title,
            string description,
            IReadOnlyList<RunChoiceOptionState> options)
        {
            return new RunChoiceOverlayState
            {
                Title = title,
                Description = description,
                Options = options
            };
        }

        private static RunChoiceOptionState Option(
            string label,
            string description,
            RunUiActionType action,
            string primaryId = null,
            string secondaryId = null)
        {
            var split = SplitLabel(label);
            return new RunChoiceOptionState
            {
                Label = split.Item1,
                Description = string.IsNullOrWhiteSpace(description)
                    ? split.Item2
                    : description,
                Action = action,
                PrimaryId = primaryId,
                SecondaryId = secondaryId,
                IsInteractable = true
            };
        }

        private static Tuple<string, string> SplitLabel(string value)
        {
            value = value ?? string.Empty;
            var split = value.IndexOf('\n');
            return split < 0
                ? Tuple.Create(value, string.Empty)
                : Tuple.Create(value.Substring(0, split), value.Substring(split + 1));
        }

        private static RunSummaryState Summary(
            string text,
            string label,
            RunUiActionType action)
        {
            return new RunSummaryState
            {
                Text = text,
                IsActionVisible = true,
                ActionLabel = label,
                Action = action
            };
        }

        private static string BuildEventOptionDescription(EventOptionConfig option)
        {
            if (!string.IsNullOrWhiteSpace(option.FollowupRelicGrade))
            {
                return "查看遗珍候选；具体代价在选择界面确认。";
            }
            if (!string.IsNullOrWhiteSpace(option.FollowupEncounterId))
            {
                return "进入额外战斗；胜负后返回当前事件。";
            }
            if (!string.IsNullOrWhiteSpace(option.FollowupRewardTableId))
            {
                return "结算后进入奖励选择。";
            }
            var effects = option.Effects.Select(effect =>
                $"{effect.Type} {effect.Amount:+#;-#;0}").ToArray();
            return effects.Length == 0 ? "安全离开。" : string.Join(" · ", effects);
        }

        private static bool IsCombat(RunNodeType type)
        {
            return type == RunNodeType.Normal ||
                   type == RunNodeType.Elite ||
                   type == RunNodeType.Boss;
        }

        private static string ToNodeTypeText(RunNodeType type)
        {
            switch (type)
            {
                case RunNodeType.Shop: return "商店";
                case RunNodeType.Normal: return "普通战斗";
                case RunNodeType.Elite: return "精英战斗";
                case RunNodeType.Enhance: return "锻造";
                case RunNodeType.Event: return "事件";
                case RunNodeType.Rest: return "恢复";
                case RunNodeType.Boss: return "Boss";
                default: return type.ToString();
            }
        }

        private static string ToRouteTagText(string routeTag)
        {
            switch (routeTag)
            {
                case "Aggressive": return "强攻";
                case "Adventure": return "奇遇";
                case "Conservative": return "保守";
                default: return string.IsNullOrWhiteSpace(routeTag) ? string.Empty : routeTag;
            }
        }

        private static string ToGradeText(string grade)
        {
            return grade == "Crown" ? "冠冕" : "奇物";
        }

        private static string ToCategoryText(string category)
        {
            switch (category)
            {
                case "Economy": return "经济";
                case "Combat": return "战斗";
                case "Spell": return "法术";
                case "Recruit": return "招募";
                case "Sustain": return "续航";
                case "Trigger": return "触发";
                default: return string.IsNullOrWhiteSpace(category) ? "规则" : category;
            }
        }
    }
}
