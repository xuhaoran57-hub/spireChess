using System.Collections;
using System.Linq;
using NUnit.Framework;
using SpireChess.App;
using SpireChess.Shop;
using SpireChess.UI.Battle;
using SpireChess.UI.Shop;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpireChess.Tests
{
    public sealed class ShopFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator ShopToBattleAndBack_PreservesRunSessionAndOwnedMinion()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(17);
            var run = GameApp.Instance.Run;

            SceneManager.LoadScene("ShopTest");
            yield return null;

            var shopController = Object.FindObjectOfType<ShopTestController>();
            Assert.That(shopController, Is.Not.Null);
            Assert.That(shopController.IsInitialized, Is.True);
            Assert.That(shopController.Session, Is.SameAs(run.Shop));
            Assert.That(run.Shop.Round, Is.EqualTo(1));

            var offerIndex = Enumerable.Range(0, run.Shop.MinionOffers.Count)
                .First(index => run.Shop.MinionOffers[index] != null);
            var purchase = shopController.BuyMinionAt(offerIndex);
            Assert.That(purchase.Success, Is.True);
            var purchased = run.Shop.Collection.Bench[purchase.BenchIndex];
            Assert.That(purchased, Is.Not.Null);

            var play = shopController.PlayBenchMinion(purchase.BenchIndex, 0);
            Assert.That(play.Success, Is.True);
            var sourceInstanceId = run.Shop.Collection.Battle[0].InstanceId;

            var end = shopController.EndShopAndEnterBattle();
            Assert.That(end.Success, Is.True);
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("BattleTest"));
            var battleController = Object.FindObjectOfType<BattleTestController>();
            Assert.That(battleController, Is.Not.Null);
            Assert.That(battleController.IsRunBattle, Is.True);
            Assert.That(battleController.SetupState.Player[0].SourceInstanceId,
                Is.EqualTo(sourceInstanceId));
            Assert.That(battleController.SetupState.Enemy.Any(minion => minion != null), Is.True);

            var battleResult = battleController.ResolveImmediately();
            Assert.That(battleResult, Is.Not.Null);
            Assert.That(run.LastBattleResult, Is.SameAs(battleResult));
            Assert.That(run.Shop.Collection.Battle[0].InstanceId, Is.EqualTo(sourceInstanceId));

            battleController.ReturnToFlow();
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ShopTest"));
            var returnedController = Object.FindObjectOfType<ShopTestController>();
            Assert.That(returnedController, Is.Not.Null);
            Assert.That(returnedController.Session, Is.SameAs(run.Shop));
            Assert.That(run.Shop.Round, Is.EqualTo(2));
            Assert.That(run.Shop.Collection.Battle[0].InstanceId, Is.EqualTo(sourceInstanceId));
        }

        [UnityTest]
        public IEnumerator TripleDiscover_ShowsBlockingModalAndResolvesSelection()
        {
            yield return EnsureGameApp();
            var configs = GameApp.Instance.Configs;
            var minion = configs.Minions.First(config =>
                config.Enabled && !config.IsToken && config.Tier == 1);
            var session = new ShopSession(
                new[] { minion },
                configs.Spells,
                new System.Random(3));

            var controllerObject = new GameObject("ShopControllerUnderTest");
            var controller = controllerObject.AddComponent<ShopTestController>();
            controller.InitializeForTests(session);
            session.GrantGold(20);
            yield return null;

            Assert.That(controller.BuyMinionAt(0).Success, Is.True);
            Assert.That(controller.BuyMinionAt(1).Success, Is.True);
            Assert.That(controller.RefreshShop().Success, Is.True);
            Assert.That(controller.BuyMinionAt(0).Success, Is.True);

            var goldenIndex = Enumerable.Range(0, session.Collection.Bench.Count)
                .First(index => session.Collection.Bench[index]?.IsGolden == true);
            Assert.That(controller.PlayBenchMinion(goldenIndex, 0).Success, Is.True);

            var rewardIndex = Enumerable.Range(0, session.Collection.Bench.Count)
                .First(index => session.Collection.Bench[index]?.ConfigId ==
                                ShopSession.TripleDiscoveryRewardSpellId);
            Assert.That(controller.UseBenchSpell(rewardIndex).Success, Is.True);
            Assert.That(session.PendingDiscover, Is.Not.Null);
            Assert.That(controller.DiscoverModalVisible, Is.True);

            var blockedRefresh = controller.RefreshShop();
            Assert.That(blockedRefresh.Success, Is.False);
            Assert.That(blockedRefresh.Error, Is.EqualTo(ShopOperationError.DiscoveryPending));

            Assert.That(controller.SelectDiscoverCandidate(0).Success, Is.True);
            Assert.That(session.PendingDiscover, Is.Null);
            Assert.That(controller.DiscoverModalVisible, Is.False);

            Object.Destroy(controllerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReloadingShopScene_DoesNotDuplicateControllerOrEventSubscription()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(29);
            var run = GameApp.Instance.Run;

            SceneManager.LoadScene("ShopTest");
            yield return null;
            var firstController = Object.FindObjectOfType<ShopTestController>();
            run.Shop.GrantGold(2);
            var firstEventCount = firstController.EventLogCount;
            Assert.That(firstController.RefreshShop().Success, Is.True);
            Assert.That(firstController.EventLogCount, Is.EqualTo(firstEventCount + 1));

            SceneManager.LoadScene("ShopTest");
            yield return null;
            var controllers = Object.FindObjectsOfType<ShopTestController>();
            Assert.That(controllers.Length, Is.EqualTo(1));
            run.Shop.GrantGold(2);
            var reloadedEventCount = controllers[0].EventLogCount;
            Assert.That(controllers[0].RefreshShop().Success, Is.True);
            Assert.That(controllers[0].EventLogCount, Is.EqualTo(reloadedEventCount + 1));
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
