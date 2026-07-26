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

            Assert.That(state.Title, Is.EqualTo("第 1 层 · 三层远征"));
            Assert.That(state.Status, Is.EqualTo("等待选择"));
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
            Assert.That(state.Summary.Text, Does.Contain("高亮节点"));
            Assert.That(state.Summary.IsActionVisible, Is.False);
        }

        [Test]
        public void Build_AfterShopUpdatesProgressAndReachableCombat()
        {
            var run = new RunSession(configs, 8103);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            ClaimAllRewards(run);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);

            var state = RunScreenStateBuilder.Build(run, configs, "商店完成");

            Assert.That(state.ProgressSummary, Does.Contain("本层商店 1/6"));
            Assert.That(state.ProgressSummary, Does.Contain("地图步数 1"));
            Assert.That(state.Nodes.Single(node => node.NodeId == "f1_shop_start").Status,
                Is.EqualTo(RunNodeStatus.Resolved));
            Assert.That(state.Nodes.Single(node => node.NodeId == "f1_opening_normal").Status,
                Is.EqualTo(RunNodeStatus.Reachable));
            Assert.That(state.Nodes.Single(node => node.NodeId == "f1_opening_normal")
                .IsInteractable, Is.True);
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
        public void Build_DerivesAbandonedBranchesAndCompleteMapVisualContract()
        {
            var run = new RunSession(configs, 8105);
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
