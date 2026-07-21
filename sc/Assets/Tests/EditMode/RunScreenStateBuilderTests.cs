using System;
using System.Linq;
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
    }
}
