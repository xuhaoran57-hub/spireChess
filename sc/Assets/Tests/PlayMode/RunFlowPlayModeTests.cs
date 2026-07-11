using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SpireChess.App;
using SpireChess.Battle;
using SpireChess.Run;
using SpireChess.UI.Battle;
using SpireChess.UI.Run;
using SpireChess.UI.Shop;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpireChess.Tests
{
    public sealed class RunFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator StageFourB_ScenesCompleteSafeWinningPathAndClaimReward()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(101);
            var run = GameApp.Instance.Run;

            SceneManager.LoadScene("RunTest");
            yield return null;
            var map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map, Is.Not.Null);
            Assert.That(map.IsInitialized, Is.True);
            Assert.That(map.NodeButtonCount, Is.EqualTo(7));
            Assert.That(map.EnterNode("f1_opening_normal").Success, Is.True);

            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ShopTest"));
            var shop = Object.FindObjectOfType<ShopTestController>();
            Assert.That(shop.Session, Is.SameAs(run.Shop));
            Assert.That(run.Shop.Gold, Is.EqualTo(3));
            Assert.That(shop.EndShopAndEnterBattle().Success, Is.True);
            yield return null;
            CompletePendingBattleAsWin(run);
            SceneManager.LoadScene("RunTest");
            yield return null;

            map = Object.FindObjectOfType<RunTestController>();
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.BattleResult));
            Assert.That(map.ContinueAfterBattle().Success, Is.True);
            Assert.That(map.EnterNode("f1_safe_normal").Success, Is.True);
            yield return null;

            shop = Object.FindObjectOfType<ShopTestController>();
            Assert.That(run.Shop.Gold, Is.EqualTo(5));
            Assert.That(shop.EndShopAndEnterBattle().Success, Is.True);
            yield return null;
            CompletePendingBattleAsWin(run);
            SceneManager.LoadScene("RunTest");
            yield return null;

            map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map.ContinueAfterBattle().Success, Is.True);
            Assert.That(map.EnterNode("f1_rest").Success, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("RunTest"));
            Assert.That(map.ChoiceOverlayVisible, Is.True);
            Assert.That(map.SelectRest("leave").Success, Is.True);
            Assert.That(map.ChoiceOverlayVisible, Is.False);
            Assert.That(map.EnterNode("f1_boss").Success, Is.True);
            yield return null;

            shop = Object.FindObjectOfType<ShopTestController>();
            Assert.That(shop.RewardModalVisible, Is.True);
            Assert.That(shop.ClaimPendingReward().Success, Is.True);
            Assert.That(shop.RewardModalVisible, Is.False);
            Assert.That(shop.EndShopAndEnterBattle().Success, Is.True);
            yield return null;
            CompletePendingBattleAsWin(run);
            SceneManager.LoadScene("RunTest");
            yield return null;

            map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map, Is.Not.Null);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RewardChoice));
            Assert.That(map.ChoiceOverlayVisible, Is.True);
            Assert.That(map.SkipReward().Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.FloorComplete));
            Assert.That(run.State.Health, Is.EqualTo(20));
            Assert.That(run.State.RunTurn, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator ReloadingShopScene_PreservesAttemptBudgetAndRunTurn()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(202);
            var run = GameApp.Instance.Run;
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            var attemptId = run.State.CurrentAttempt.NodeAttemptId;

            SceneManager.LoadScene("ShopTest");
            yield return null;
            Assert.That(run.Shop.Gold, Is.EqualTo(3));
            Assert.That(run.State.RunTurn, Is.EqualTo(1));

            SceneManager.LoadScene("ShopTest");
            yield return null;
            var controllers = Object.FindObjectsOfType<ShopTestController>();
            Assert.That(controllers.Length, Is.EqualTo(1));
            Assert.That(run.Shop.Gold, Is.EqualTo(3));
            Assert.That(run.Shop.Round, Is.EqualTo(1));
            Assert.That(run.State.RunTurn, Is.EqualTo(1));
            Assert.That(run.State.CurrentAttempt.NodeAttemptId, Is.EqualTo(attemptId));
        }

        [UnityTest]
        public IEnumerator ReloadingBattleAfterSettlement_RestoresResultWithoutSettlingTwice()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(303);
            var run = GameApp.Instance.Run;
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            CompletePendingBattleAsWin(run);
            var attemptId = run.State.CurrentAttempt.NodeAttemptId;
            var health = run.State.Health;

            SceneManager.LoadScene("BattleTest");
            yield return null;
            var battle = Object.FindObjectOfType<BattleTestController>();
            Assert.That(battle, Is.Not.Null);
            Assert.That(battle.IsRunBattle, Is.True);
            Assert.That(battle.IsBattleLocked, Is.True);
            Assert.That(battle.LastResult, Is.SameAs(run.LastBattleResult));
            Assert.That(run.State.CurrentAttempt.NodeAttemptId, Is.EqualTo(attemptId));
            Assert.That(run.State.Health, Is.EqualTo(health));

            battle.ReturnToFlow();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("RunTest"));
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.BattleResult));
        }

        [UnityTest]
        public IEnumerator EliteRewardAndEnhance_UsePersistentRunChoiceOverlay()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(404);
            var run = GameApp.Instance.Run;
            CompleteCombatNodeAsWin(run, "f1_opening_normal");
            Assert.That(run.EnterNode("f1_elite_wall").Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            CompletePendingBattleAsWin(run);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RewardChoice));

            SceneManager.LoadScene("RunTest");
            yield return null;
            var map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map.ChoiceOverlayVisible, Is.True);
            Assert.That(map.SkipReward().Success, Is.True);
            Assert.That(map.ContinueAfterBattle().Success, Is.True);
            Assert.That(map.EnterNode("f1_enhance").Success, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("RunTest"));
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.EnhanceChoice));
            Assert.That(map.ChoiceOverlayVisible, Is.True);
            Assert.That(map.SkipEnhancement().Success, Is.True);
            Assert.That(run.State.MapProgress.GetStatus("f1_boss"), Is.EqualTo(RunNodeStatus.Reachable));
        }

        [UnityTest]
        public IEnumerator EventNode_StaysInRunSceneAndResolvesThroughOverlay()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(505);
            var run = GameApp.Instance.Run;
            CompleteCombatNodeAsWin(run, "f1_opening_normal");
            CompleteCombatNodeAsWin(run, "f1_safe_normal");

            SceneManager.LoadScene("RunTest");
            yield return null;
            var map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map.EnterNode("f1_event").Success, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("RunTest"));
            Assert.That(map.ChoiceOverlayVisible, Is.True);
            var pending = run.State.PendingEventChoice;
            var safeOption = pending.Config.Options[pending.Config.Options.Count - 1];
            Assert.That(map.SelectEvent(pending.Config.Id, safeOption.Id).Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.MapSelection));
            Assert.That(map.ChoiceOverlayVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator ReloadingRunScene_PreservesNonCombatChoice()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(606);
            var run = GameApp.Instance.Run;
            CompleteCombatNodeAsWin(run, "f1_opening_normal");
            CompleteCombatNodeAsWin(run, "f1_safe_normal");
            Assert.That(run.EnterNode("f1_rest").Success, Is.True);
            var attemptId = run.State.CurrentAttempt.NodeAttemptId;

            SceneManager.LoadScene("RunTest");
            yield return null;
            SceneManager.LoadScene("RunTest");
            yield return null;
            var controllers = Object.FindObjectsOfType<RunTestController>();
            Assert.That(controllers.Length, Is.EqualTo(1));
            Assert.That(controllers[0].ChoiceOverlayVisible, Is.True);
            Assert.That(run.State.CurrentAttempt.NodeAttemptId, Is.EqualTo(attemptId));
            Assert.That(run.State.RunTurn, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator FloorCompleteOverlay_AdvancesAndRebuildsSecondFloorMap()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(707);
            var run = GameApp.Instance.Run;
            CompleteSafeFloorToBossVictory(run, "f1_opening_normal", "f1_safe_normal", "f1_rest", "f1_boss");
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RewardChoice));

            SceneManager.LoadScene("RunTest");
            yield return null;
            var map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map.SkipReward().Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.FloorComplete));
            Assert.That(map.ContinueToNextFloor().Success, Is.True);
            Assert.That(run.State.Floor, Is.EqualTo(2));
            Assert.That(map.NodeButtonCount, Is.EqualTo(6));
            Assert.That(run.State.MapProgress.GetStatus("f2_normal"), Is.EqualTo(RunNodeStatus.Reachable));
        }

        [UnityTest]
        public IEnumerator ThreeFloorDomainFlow_RendersFinalRunWonResult()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(808);
            var run = GameApp.Instance.Run;
            CompleteSafeFloorToBossVictory(run, "f1_opening_normal", "f1_safe_normal", "f1_rest", "f1_boss");
            Assert.That(run.SkipRewardChoice().Success, Is.True);
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            CompleteSafeFloorToBossVictory(run, null, "f2_normal", "f2_rest", "f2_boss");
            Assert.That(run.SkipRewardChoice().Success, Is.True);
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            CompleteSafeFloorToBossVictory(run, null, "f3_normal", "f3_rest", "f3_boss");

            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RunWon));
            SceneManager.LoadScene("RunTest");
            yield return null;
            var map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map, Is.Not.Null);
            Assert.That(map.NodeButtonCount, Is.EqualTo(6));
            Assert.That(run.State.Statistics.BossesDefeated, Is.EqualTo(3));
            Assert.That(run.State.Statistics.CompletedAtUtc, Is.Not.Null);
        }

        private static void CompleteSafeFloorToBossVictory(
            RunSession run,
            string openingNode,
            string normalNode,
            string restNode,
            string bossNode)
        {
            if (!string.IsNullOrWhiteSpace(openingNode))
                CompleteCombatAndContinue(run, openingNode);
            CompleteCombatAndContinue(run, normalNode);
            Assert.That(run.EnterNode(restNode).Success, Is.True);
            Assert.That(run.SelectRestOption("leave").Success, Is.True);
            Assert.That(run.EnterNode(bossNode).Success, Is.True);
            ClaimAllRewards(run);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            CompletePendingBattleAsWin(run);
        }

        private static void CompleteCombatAndContinue(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True);
            ClaimAllRewards(run);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            CompletePendingBattleAsWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
        }

        private static void ClaimAllRewards(RunSession run)
        {
            while (run.State.PendingCardRewards.Count > 0)
                Assert.That(run.ClaimNextCardReward().Success, Is.True);
        }

        private static void CompleteCombatNodeAsWin(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            CompletePendingBattleAsWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
        }

        private static void CompletePendingBattleAsWin(RunSession run)
        {
            Assert.That(run.PendingBattle, Is.Not.Null);
            var result = new BattleSimulationResult(
                new BattleBoardState(),
                BattleSide.Player,
                BattleOutcomeReason.Victory,
                new List<string>(),
                new List<BattleStep>());
            Assert.That(run.TryCompleteBattle(result, out var returnScene), Is.True);
            Assert.That(returnScene, Is.EqualTo("RunTest"));
        }

        private static IEnumerator EnsureGameApp()
        {
            if (GameApp.Instance == null)
            {
                yield return null;
            }

            Assert.That(GameApp.Instance, Is.Not.Null);
            Assert.That(GameApp.Instance.Configs, Is.Not.Null);
        }
    }
}
