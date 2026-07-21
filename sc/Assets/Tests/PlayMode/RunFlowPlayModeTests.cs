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
        public IEnumerator ExplicitShopAndCombatNodes_SwitchScenesIndependently()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(101);
            var run = GameApp.Instance.Run;

            SceneManager.LoadScene("RunTest");
            yield return null;
            var map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map.NodeButtonCount, Is.EqualTo(19));
            Assert.That(map.EnterNode("f1_shop_start").Success, Is.True);
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ShopTest"));
            var shop = Object.FindObjectOfType<ShopTestController>();
            Assert.That(run.State.ShopTurn, Is.EqualTo(1));
            Assert.That(run.Shop.Gold, Is.EqualTo(3));
            Assert.That(shop.EndShopAndEnterBattle().Success, Is.True);
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("RunTest"));
            map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map.EnterNode("f1_opening_normal").Success, Is.True);
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("BattleTest"));
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(run.PendingBattle, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ReloadingShopScene_PreservesShopNodeBudgetAndAttempt()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(202);
            var run = GameApp.Instance.Run;
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            var attemptId = run.State.CurrentAttempt.NodeAttemptId;

            SceneManager.LoadScene("ShopTest");
            yield return null;
            SceneManager.LoadScene("ShopTest");
            yield return null;

            Assert.That(Object.FindObjectsOfType<ShopTestController>().Length, Is.EqualTo(1));
            Assert.That(run.Shop.Gold, Is.EqualTo(3));
            Assert.That(run.Shop.Round, Is.EqualTo(1));
            Assert.That(run.State.ShopTurn, Is.EqualTo(1));
            Assert.That(run.State.CurrentAttempt.NodeAttemptId, Is.EqualTo(attemptId));
        }

        [UnityTest]
        public IEnumerator ReloadingBattleAfterSettlement_RestoresResultWithoutSettlingTwice()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(303);
            var run = GameApp.Instance.Run;
            CompleteShop(run, "f1_shop_start");
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            CompletePendingBattleAsWin(run);
            var attemptId = run.State.CurrentAttempt.NodeAttemptId;
            var health = run.State.Health;

            SceneManager.LoadScene("BattleTest");
            yield return null;
            var battle = Object.FindObjectOfType<BattleTestController>();
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
            CompleteShop(run, "f1_shop_start");
            CompleteCombatAndContinue(run, "f1_opening_normal");
            CompleteShop(run, "f1_shop_2");
            CompleteCombatAndContinue(run, "f1_safe_normal");
            CompleteShop(run, "f1_shop_3");
            CompleteCombatAndContinue(run, "f1_mid_mechanic");
            CompleteShop(run, "f1_shop_4");
            Assert.That(run.EnterNode("f1_elite_wall").Success, Is.True);
            CompletePendingBattleAsWin(run);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RewardChoice));

            SceneManager.LoadScene("RunTest");
            yield return null;
            var map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map.ChoiceOverlayVisible, Is.True);
            Assert.That(map.SelectRelic(
                run.State.PendingRelicChoice.Candidates[0].CandidateId).Success, Is.True);
            Assert.That(map.ContinueAfterBattle().Success, Is.True);
            Assert.That(map.EnterNode("f1_enhance").Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.EnhanceChoice));
            Assert.That(map.ChoiceOverlayVisible, Is.True);
            Assert.That(map.SkipEnhancement().Success, Is.True);
            Assert.That(run.State.MapProgress.GetStatus("f1_shop_5"),
                Is.EqualTo(RunNodeStatus.Reachable));
        }

        [UnityTest]
        public IEnumerator EventNode_StaysInRunSceneAndResolvesThroughOverlay()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(505);
            var run = GameApp.Instance.Run;
            ReachFirstFloorUtility(run, "f1_safe_normal", "f1_route_normal");

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
        public IEnumerator ReloadingRunScene_PreservesNonCombatChoiceWithoutAdvancingShopTurn()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(606);
            var run = GameApp.Instance.Run;
            ReachFirstFloorUtility(run, "f1_safe_normal", "f1_route_safe");
            Assert.That(run.EnterNode("f1_rest").Success, Is.True);
            var attemptId = run.State.CurrentAttempt.NodeAttemptId;
            var shopTurn = run.State.ShopTurn;

            SceneManager.LoadScene("RunTest");
            yield return null;
            SceneManager.LoadScene("RunTest");
            yield return null;
            var controllers = Object.FindObjectsOfType<RunTestController>();
            Assert.That(controllers.Length, Is.EqualTo(1));
            Assert.That(controllers[0].ChoiceOverlayVisible, Is.True);
            Assert.That(run.State.CurrentAttempt.NodeAttemptId, Is.EqualTo(attemptId));
            Assert.That(run.State.ShopTurn, Is.EqualTo(shopTurn));
        }

        [UnityTest]
        public IEnumerator FloorCompleteOverlay_AdvancesToSecondFloorShopStart()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(707);
            var run = GameApp.Instance.Run;
            CompleteFloor(
                run, 1, "f1_safe_normal", "f1_route_safe", "f1_rest", "f1_late_shield");

            SceneManager.LoadScene("RunTest");
            yield return null;
            var map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map.SelectRelic(
                run.State.PendingRelicChoice.Candidates[0].CandidateId).Success, Is.True);
            Assert.That(map.ContinueToNextFloor().Success, Is.True);
            Assert.That(run.State.Floor, Is.EqualTo(2));
            Assert.That(map.NodeButtonCount, Is.EqualTo(19));
            Assert.That(run.State.MapProgress.GetStatus("f2_shop_start"),
                Is.EqualTo(RunNodeStatus.Reachable));
        }

        [UnityTest]
        public IEnumerator ThreeFloorDomainFlow_RendersEighteenBattleRunWonResult()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(808);
            var run = GameApp.Instance.Run;
            CompleteFloor(
                run, 1, "f1_safe_normal", "f1_route_safe", "f1_rest", "f1_late_shield");
            SelectFirstRelic(run);
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            CompleteFloor(
                run, 2, "f2_normal", "f2_route_safe", "f2_rest", "f2_late_break");
            SelectFirstRelic(run);
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            CompleteFloor(
                run, 3, "f3_normal", "f3_route_safe", "f3_rest", "f3_late_wild");

            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RunWon));
            Assert.That(run.State.ShopTurn, Is.EqualTo(18));
            Assert.That(run.State.Statistics.BattlesWon, Is.EqualTo(18));
            SceneManager.LoadScene("RunTest");
            yield return null;
            var map = Object.FindObjectOfType<RunTestController>();
            Assert.That(map.NodeButtonCount, Is.EqualTo(19));
            Assert.That(run.State.Statistics.BossesDefeated, Is.EqualTo(3));
            Assert.That(run.State.Statistics.CompletedAtUtc, Is.Not.Null);
        }

        private static void ReachFirstFloorUtility(
            RunSession run,
            string earlyCombat,
            string routeCombat)
        {
            CompleteShop(run, "f1_shop_start");
            CompleteCombatAndContinue(run, "f1_opening_normal");
            CompleteShop(run, "f1_shop_2");
            CompleteCombatAndContinue(run, earlyCombat);
            CompleteShop(run, "f1_shop_3");
            CompleteCombatAndContinue(run, "f1_mid_mechanic");
            CompleteShop(run, "f1_shop_4");
            CompleteCombatAndContinue(run, routeCombat);
        }

        private static void CompleteFloor(
            RunSession run,
            int floor,
            string earlyCombat,
            string routeCombat,
            string restNode,
            string lateCombat)
        {
            CompleteShop(run, $"f{floor}_shop_start");
            CompleteCombatAndContinue(run, $"f{floor}_opening_normal");
            CompleteShop(run, $"f{floor}_shop_2");
            CompleteCombatAndContinue(run, earlyCombat);
            CompleteShop(run, $"f{floor}_shop_3");
            CompleteCombatAndContinue(run, $"f{floor}_mid_mechanic");
            CompleteShop(run, $"f{floor}_shop_4");
            CompleteCombatAndContinue(run, routeCombat);
            Assert.That(run.EnterNode(restNode).Success, Is.True);
            Assert.That(run.SelectRestOption("leave").Success, Is.True);
            CompleteShop(run, $"f{floor}_shop_5");
            CompleteCombatAndContinue(run, lateCombat);
            CompleteShop(run, $"f{floor}_shop_boss");
            Assert.That(run.EnterNode($"f{floor}_boss").Success, Is.True);
            CompletePendingBattleAsWin(run);
        }

        private static void CompleteShop(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True, nodeId);
            ClaimAllRewards(run);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
        }

        private static void SelectFirstRelic(RunSession run)
        {
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RelicChoice));
            Assert.That(run.SelectRelicCandidate(
                run.State.PendingRelicChoice.Candidates[0].CandidateId).Success, Is.True);
        }

        private static void CompleteCombatAndContinue(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True, nodeId);
            CompletePendingBattleAsWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
        }

        private static void ClaimAllRewards(RunSession run)
        {
            while (run.State.PendingCardRewards.Count > 0)
            {
                var claim = run.ClaimNextCardReward();
                if (claim.Success)
                    continue;
                Assert.That(claim.Error, Is.EqualTo(RunOperationError.BenchFull));
                Assert.That(run.SkipNextCardReward().Success, Is.True);
            }
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
