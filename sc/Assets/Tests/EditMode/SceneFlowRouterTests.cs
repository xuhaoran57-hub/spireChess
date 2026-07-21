using System.Collections.Generic;
using NUnit.Framework;
using SpireChess.App;
using SpireChess.Run;

namespace SpireChess.Tests.EditMode
{
    public sealed class SceneFlowRouterTests
    {
        [TestCase(RunPhase.MapSelection, GameSceneId.Run)]
        [TestCase(RunPhase.Shop, GameSceneId.Shop)]
        [TestCase(RunPhase.Battle, GameSceneId.Battle)]
        [TestCase(RunPhase.BattleResult, GameSceneId.Run)]
        [TestCase(RunPhase.RewardChoice, GameSceneId.Run)]
        [TestCase(RunPhase.RelicChoice, GameSceneId.Run)]
        [TestCase(RunPhase.EventChoice, GameSceneId.Run)]
        [TestCase(RunPhase.EnhanceChoice, GameSceneId.Run)]
        [TestCase(RunPhase.RestChoice, GameSceneId.Run)]
        [TestCase(RunPhase.FloorComplete, GameSceneId.Run)]
        [TestCase(RunPhase.RunWon, GameSceneId.Run)]
        [TestCase(RunPhase.RunLost, GameSceneId.Run)]
        public void Resolve_MapsEveryDurablePhase(RunPhase phase, GameSceneId expected)
        {
            var router = new SceneFlowRouter(() => "", _ => { });
            Assert.That(router.Resolve(phase), Is.EqualTo(expected));
        }

        [Test]
        public void Resolve_RejectsEnteringNode()
        {
            var router = new SceneFlowRouter(() => "", _ => { });
            Assert.That(
                () => router.Resolve(RunPhase.EnteringNode),
                Throws.InvalidOperationException);
        }

        [Test]
        public void GoTo_DoesNotReloadCurrentScene()
        {
            var loaded = new List<string>();
            var router = new SceneFlowRouter(() => GameSceneNames.Run, loaded.Add);
            router.GoTo(GameSceneId.Run);
            Assert.That(loaded, Is.Empty);
        }
    }
}
