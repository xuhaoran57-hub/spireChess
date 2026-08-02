using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.UI.Run;
using SpireChess.Utils;

namespace SpireChess.Tests.EditMode
{
    public sealed class RunScreenStateBuilderTests
    {
        private ConfigService configs;

        [SetUp]
        public void SetUp()
        {
            configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            validation.ThrowIfInvalid();
        }

        [Test]
        public void Build_RejectsMissingDomainDependencies()
        {
            var run = new RunSession(configs, 8101);
            Assert.Throws<ArgumentNullException>(() =>
                RunScreenStateBuilder.Build(null, configs, string.Empty));
            Assert.Throws<ArgumentNullException>(() =>
                RunScreenStateBuilder.Build(run, null, string.Empty));
        }

        [Test]
        public void Build_MapsAllNodesEdgesAndInitialReachability()
        {
            var run = new RunSession(configs, 8102);

            var state = RunScreenStateBuilder.Build(run, configs, "等待选择");
            var expectedEdges = run.State.CurrentMap.Nodes.Sum(node =>
                node.NextNodeIds.Count);
            var expectedConnections = run.State.CurrentMap.Nodes
                .SelectMany(node => node.NextNodeIds.Select(next =>
                    node.Id + "->" + next))
                .OrderBy(value => value)
                .ToArray();
            var renderedConnections = state.Edges.Select(edge =>
                    edge.FromNodeId + "->" + edge.ToNodeId)
                .OrderBy(value => value)
                .ToArray();

            Assert.That(state.Title, Is.EqualTo("荒野 · 第 1 章"));
            Assert.That(state.Status, Is.EqualTo("等待选择"));
            Assert.That(state.ResourceSummary, Does.Contain("护甲 10"));
            Assert.That(state.RouteHint, Does.Contain("亡语召唤链"));
            Assert.That(state.RouteHint, Does.Contain("万籁母巢"));
            var opening = state.Nodes.Single(node =>
                node.NodeId == "f1_opening_normal");
            Assert.That(opening.ThreatLevel, Is.EqualTo(1));
            Assert.That(opening.RouteText, Is.EqualTo("威胁 ★"));
            Assert.That(opening.FormationText, Is.EqualTo("敌阵 1/1"));
            Assert.That(opening.MechanicText, Is.EqualTo("低面板亡语入门"));
            Assert.That(opening.LossPressureText, Is.Empty);
            Assert.That(opening.RewardText, Is.EqualTo("下个商店 +1 金币"));
            Assert.That(state.Nodes, Has.Count.EqualTo(19));
            Assert.That(state.Edges, Has.Count.EqualTo(expectedEdges));
            Assert.That(renderedConnections, Is.EqualTo(expectedConnections));
            Assert.That(state.MaximumColumn,
                Is.EqualTo(run.State.CurrentMap.Nodes.Max(node => node.Column)));
            Assert.That(state.Nodes.Single(node => node.NodeId == "f1_shop_start")
                .IsInteractable, Is.True);
            Assert.That(state.Nodes.Count(node => node.IsInteractable), Is.EqualTo(1));
            Assert.That(state.Relics, Is.Empty);
            Assert.That(state.Choice, Is.Null);
            Assert.That(state.Summary.Text, Does.Contain("可达节点预览"));
            Assert.That(state.Summary.Text, Does.Contain("商店｜补给与整备"));
            Assert.That(state.Summary.IsActionVisible, Is.False);
        }

        [Test]
        public void Build_RouteChoiceSummarizesThreatMechanicLossAndReward()
        {
            var run = new RunSession(configs, 8107);
            var statuses = GetMutableMapStatuses(run.State.MapProgress);
            foreach (var nodeId in statuses.Keys.ToArray())
            {
                statuses[nodeId] = RunNodeStatus.Locked;
            }
            statuses["f1_elite_wall"] = RunNodeStatus.Reachable;
            statuses["f1_route_normal"] = RunNodeStatus.Reachable;
            statuses["f1_route_safe"] = RunNodeStatus.Reachable;

            var state = RunScreenStateBuilder.Build(
                run,
                configs,
                string.Empty);

            Assert.That(state.Summary.Text, Does.Contain(
                "强攻 · 威胁 ★★★★｜第 4 战 · 精英战斗｜百根围猎队"));
            Assert.That(state.Summary.Text, Does.Contain(
                "奇遇 · 威胁 ★★★｜第 4 战 · 普通战斗｜狐影繁生队"));
            Assert.That(state.Summary.Text, Does.Contain(
                "保守 · 威胁 ★★｜第 4 战 · 普通战斗｜盘根守林队"));
            Assert.That(state.Summary.Text, Does.Contain("敌阵 11/16"));
            Assert.That(state.Summary.Text, Does.Contain("失败修正 +1"));
            Assert.That(state.Summary.Text, Does.Contain("奖励：高价值三选一"));
            Assert.That(state.Summary.Text, Does.Contain("奖励：1 次免费刷新"));
        }

        [Test]
        public void Build_AfterShopUpdatesProgressAndReachableCombat()
        {
            var run = new RunSession(configs, 8103);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            ClaimAllRewards(run);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);

            var state = RunScreenStateBuilder.Build(run, configs, "商店完成");

            Assert.That(state.ProgressSummary, Does.Contain("本章商店 1/6"));
            Assert.That(state.ProgressSummary, Does.Contain("地图步数 1"));
            Assert.That(state.Nodes.Single(node => node.NodeId == "f1_shop_start").Status,
                Is.EqualTo(RunNodeStatus.Resolved));
            Assert.That(state.Nodes.Single(node => node.NodeId == "f1_opening_normal").Status,
                Is.EqualTo(RunNodeStatus.Reachable));
            Assert.That(state.Nodes.Single(node => node.NodeId == "f1_opening_normal")
                .IsInteractable, Is.True);
            Assert.That(state.Summary.Text, Does.Contain("敌阵 1/1"));
            Assert.That(state.Summary.Text, Does.Contain("机制：低面板亡语入门"));
            Assert.That(state.Summary.Text, Does.Contain("奖励：下个商店 +1 金币"));
        }

        [Test]
        public void Build_PropagatesConfiguredRelicIconIds()
        {
            var run = new RunSession(configs, 8104);
            var ownedConfig = configs.RelicsById["curio_refresh_gear"];
            AddOwnedRelic(run, ownedConfig);

            var state = RunScreenStateBuilder.Build(run, configs, string.Empty);

            Assert.That(state.Relics, Has.Count.EqualTo(1));
            Assert.That(state.Relics[0].IconId, Is.EqualTo(ownedConfig.UiIconId));

            var pending = run.Relics.CreateChoice(
                "Crown",
                "test-attempt",
                RelicCompletionMode.FloorComplete,
                0,
                false);
            SetInternal(run.State, nameof(RunState.PendingRelicChoice), pending);
            SetInternal(run.State, nameof(RunState.Phase), RunPhase.RelicChoice);

            state = RunScreenStateBuilder.Build(run, configs, string.Empty);

            Assert.That(state.Choice.Options, Has.Count.EqualTo(pending.Candidates.Count));
            foreach (var option in state.Choice.Options)
            {
                var candidate = pending.Candidates.Single(value =>
                    value.CandidateId == option.PrimaryId);
                Assert.That(
                    option.IconId,
                    Is.EqualTo(configs.RelicsById[candidate.RelicId].UiIconId),
                    candidate.RelicId);
            }
        }

        [Test]
        public void Build_PropagatesConfiguredEventArtworkId()
        {
            var run = new RunSession(configs, 8105);
            var eventConfig = configs.EventsById["tranquil_grove"];
            SetInternal(
                run.State,
                nameof(RunState.PendingEventChoice),
                new PendingEventChoice("test-attempt", eventConfig));
            SetInternal(run.State, nameof(RunState.Phase), RunPhase.EventChoice);

            var state = RunScreenStateBuilder.Build(
                run,
                configs,
                string.Empty);

            Assert.That(state.Choice, Is.Not.Null);
            Assert.That(
                state.Choice.ArtworkId,
                Is.EqualTo("event_tranquil_grove"));
            Assert.That(
                state.Choice.Options.Select(option => option.PrimaryId),
                Has.All.EqualTo("tranquil_grove"));
        }

        [Test]
        public void Build_DerivesAbandonedBranchesAndCompleteMapVisualContract()
        {
            var run = new RunSession(configs, 8106);
            var statuses = GetMutableMapStatuses(run.State.MapProgress);
            foreach (var nodeId in statuses.Keys.ToArray())
            {
                statuses[nodeId] = RunNodeStatus.Locked;
            }

            var resolvedPath = new[]
            {
                "f1_shop_start",
                "f1_opening_normal",
                "f1_shop_2",
                "f1_safe_normal",
                "f1_shop_3",
                "f1_mid_mechanic",
                "f1_shop_4"
            };
            foreach (var nodeId in resolvedPath)
            {
                statuses[nodeId] = RunNodeStatus.Resolved;
            }
            statuses["f1_elite_wall"] = RunNodeStatus.Current;
            statuses["f1_enhance"] = RunNodeStatus.Reachable;

            var state = RunScreenStateBuilder.Build(run, configs, string.Empty);

            Assert.That(
                state.Nodes.Single(node => node.NodeId == "f1_early_summon")
                    .PresentationStatus,
                Is.EqualTo(RunMapPresentationStatus.Abandoned));
            Assert.That(
                state.Nodes.Single(node => node.NodeId == "f1_route_normal")
                    .PresentationStatus,
                Is.EqualTo(RunMapPresentationStatus.Abandoned));
            Assert.That(
                state.Nodes.Single(node => node.NodeId == "f1_event")
                    .PresentationStatus,
                Is.EqualTo(RunMapPresentationStatus.Abandoned),
                "Abandoned branches should propagate through unique descendants.");
            Assert.That(
                state.Nodes.Single(node => node.NodeId == "f1_shop_5")
                    .PresentationStatus,
                Is.EqualTo(RunMapPresentationStatus.Locked),
                "Propagation must stop at a merge with a live predecessor.");
            Assert.That(
                run.State.MapProgress.GetStatus("f1_route_normal"),
                Is.EqualTo(RunNodeStatus.Locked),
                "Presentation derivation must not mutate domain progress.");

            Assert.That(
                state.Nodes.Select(node => node.PresentationStatus).Distinct(),
                Is.EquivalentTo(new[]
                {
                    RunMapPresentationStatus.Locked,
                    RunMapPresentationStatus.Reachable,
                    RunMapPresentationStatus.Current,
                    RunMapPresentationStatus.Resolved,
                    RunMapPresentationStatus.Abandoned
                }));
            Assert.That(
                state.Edges.Select(edge => edge.PresentationStatus).Distinct(),
                Is.EquivalentTo(new[]
                {
                    RunMapEdgePresentationStatus.Locked,
                    RunMapEdgePresentationStatus.Reachable,
                    RunMapEdgePresentationStatus.Resolved,
                    RunMapEdgePresentationStatus.Abandoned
                }));

            var iconsByType = state.Nodes
                .GroupBy(node => node.Type)
                .ToDictionary(group => group.Key, group => group.First().IconId);
            Assert.That(iconsByType, Has.Count.EqualTo(7));
            Assert.That(iconsByType.Values, Has.All.StartsWith("icon_map_"));
            Assert.That(
                iconsByType.Values.Distinct().ToArray(),
                Has.Length.EqualTo(7));
        }

        private static void ClaimAllRewards(RunSession run)
        {
            while (run.State.PendingCardRewards.Count > 0)
            {
                var result = run.ClaimNextCardReward();
                if (result.Success)
                {
                    continue;
                }
                Assert.That(result.Error, Is.EqualTo(RunOperationError.BenchFull));
                Assert.That(run.SkipNextCardReward().Success, Is.True);
            }
        }

        private static void AddOwnedRelic(RunSession run, RelicConfig relic)
        {
            var constructor = typeof(OwnedRelicState).GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var owned = (OwnedRelicState)constructor.Invoke(new object[]
            {
                relic,
                "Test",
                "test",
                run.State.Floor,
                run.State.ShopTurn
            });
            typeof(RunState).GetMethod(
                    "AddOwnedRelic",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(run.State, new object[] { owned });
        }

        private static void SetInternal<T>(RunState state, string propertyName, T value)
        {
            typeof(RunState).GetProperty(propertyName)
                .GetSetMethod(true)
                .Invoke(state, new object[] { value });
        }

        private static IDictionary<string, RunNodeStatus> GetMutableMapStatuses(
            MapProgressState progress)
        {
            return (IDictionary<string, RunNodeStatus>)typeof(MapProgressState)
                .GetField("statusById", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(progress);
        }
    }
}
