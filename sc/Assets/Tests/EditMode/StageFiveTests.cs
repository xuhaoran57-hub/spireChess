using System;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Shop;
using SpireChess.Simulation;
using SpireChess.Utils;

namespace SpireChess.Tests.EditMode
{
    public sealed class StageFiveTests
    {
        private ConfigService configs;

        [SetUp]
        public void SetUp()
        {
            configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
        }

        [Test]
        public void ReleasedContent_IsCompleteAndPlayable()
        {
            Assert.That(configs.ContentRelease, Is.Not.Null);
            Assert.That(configs.ContentRelease.MinionIds, Has.Count.EqualTo(52));
            Assert.That(configs.ContentRelease.SpellIds, Has.Count.EqualTo(16));
            Assert.That(configs.EventsById, Has.Count.EqualTo(10));
            Assert.That(configs.Encounters.Count(value => value.Category == "Normal"),
                Is.GreaterThanOrEqualTo(6));
            Assert.That(configs.Minions.Where(value => !value.IsToken),
                Has.All.Matches<MinionConfig>(value =>
                    value.Effects.Count > 0 && value.GoldenEffects.Count > 0));
        }

        [Test]
        public void DelayedSupply_GrantsGoldAtNextShopStart()
        {
            var shop = CreateShop();
            Assert.That(shop.StartRound(1).Success, Is.True);
            Assert.That(shop.ClaimRewardSpell(configs.SpellsById["delayed_supply"]).Success, Is.True);
            var index = FindBench(shop, "delayed_supply");
            Assert.That(shop.UseSpell(index).Success, Is.True);
            Assert.That(shop.ScheduledGold, Is.EqualTo(2));
            Assert.That(shop.EndRound().Success, Is.True);
            Assert.That(shop.StartRound(2).Success, Is.True);
            Assert.That(shop.Gold, Is.EqualTo(ShopEconomyRules.GetRoundBudget(2) + 2));
        }

        [Test]
        public void TemporaryWard_IsConsumedByOneBattleSnapshot()
        {
            var shop = CreateShop();
            shop.StartRound(1);
            var minion = configs.MinionsById["stargazing_apprentice"];
            Assert.That(shop.ClaimRewardMinion(minion).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, minion.Id), 0).Success, Is.True);
            Assert.That(shop.ClaimRewardSpell(configs.SpellsById["temporary_ward"]).Success, Is.True);
            Assert.That(shop.UseSpell(FindBench(shop, "temporary_ward"), 0).Success, Is.True);

            Assert.That(shop.Collection.Battle[0].HasPermanentShield, Is.False);
            Assert.That(shop.Collection.Battle[0].HasPendingCombatShield, Is.True);
            Assert.That(shop.CreateBattleSnapshot().Player[0].HasShield, Is.True);
            Assert.That(shop.CreateBattleSnapshot().Player[0].HasShield, Is.False);
        }

        [Test]
        public void PrototypeCopy_CanCancelOrCreateBaseCopyAndConsumePool()
        {
            var shop = CreateShop();
            shop.StartRound(1);
            var minion = configs.MinionsById["stargazing_apprentice"];
            shop.ClaimRewardMinion(minion);
            shop.PlayMinion(FindBench(shop, minion.Id), 0);
            shop.ClaimRewardSpell(configs.SpellsById["prototype_copy"]);

            var remaining = shop.MinionPool.GetRemainingCopies(minion.Id);
            Assert.That(shop.UseSpell(FindBench(shop, "prototype_copy")).Success, Is.True);
            Assert.That(shop.PendingChoice, Is.Not.Null);
            Assert.That(shop.CancelEffectChoice().Success, Is.True);
            Assert.That(FindBench(shop, "prototype_copy"), Is.GreaterThanOrEqualTo(0));

            Assert.That(shop.UseSpell(FindBench(shop, "prototype_copy")).Success, Is.True);
            Assert.That(shop.SelectEffectChoice(0).Success, Is.True);
            Assert.That(shop.MinionPool.GetRemainingCopies(minion.Id), Is.EqualTo(remaining - 1));
            var copy = shop.Collection.Bench.First(card => card?.ConfigId == minion.Id);
            Assert.That(copy.IsGolden, Is.False);
            Assert.That(copy.CurrentAttack, Is.EqualTo(minion.Attack));
            Assert.That(copy.CurrentHealth, Is.EqualTo(minion.Health));
        }

        [Test]
        public void BattleStartAndPermanentDeathEffect_UseQueueAndProduceWriteback()
        {
            var state = new BattleBoardState();
            var bastion = configs.MinionsById["ringing_iron_bastion"];
            var ally = configs.MinionsById["stargazing_apprentice"];
            var enemy = configs.MinionsById["royal_bounty_hunter"];
            state.Player[0] = new BattleMinionRuntime(
                bastion, true, sourceInstanceId: "bastion", permanentKeywords: new[] { "Taunt" });
            state.Player[1] = new BattleMinionRuntime(ally, false, sourceInstanceId: "ally");
            state.Enemy[0] = new BattleMinionRuntime(enemy, false, initialAttack: 99, initialHealth: 20);

            var result = new BattleSimulator(new Random(7), ResolveMinion).Simulate(state);
            var delta = result.PermanentDeltas.Single(value => value.SourceInstanceId == "ally");
            Assert.That(delta.Attack, Is.EqualTo(3));
            Assert.That(delta.Health, Is.EqualTo(3));
        }

        [Test]
        public void BattleBatchRunner_IsDeterministicForSameSeedRange()
        {
            var state = BuildFixture.Create(
                new[] { new BattleMinionRuntime(configs.MinionsById["young_deer_spirit"]) },
                new[] { new BattleMinionRuntime(configs.MinionsById["hearth_core_spark"]) });
            var runner = new BattleBatchRunner(ResolveMinion);
            var first = runner.Run(state, 100, 20);
            var second = runner.Run(state, 100, 20);
            Assert.That(second.PlayerWins, Is.EqualTo(first.PlayerWins));
            Assert.That(second.EnemyWins, Is.EqualTo(first.EnemyWins));
            Assert.That(second.Draws, Is.EqualTo(first.Draws));
        }

        private ShopSession CreateShop()
        {
            return new ShopSession(configs.Minions, configs.Spells, new Random(123));
        }

        private MinionConfig ResolveMinion(string id)
        {
            return configs.MinionsById.TryGetValue(id, out var value) ? value : null;
        }

        private static int FindBench(ShopSession shop, string configId)
        {
            for (var i = 0; i < shop.Collection.Bench.Count; i++)
            {
                if (shop.Collection.Bench[i]?.ConfigId == configId) return i;
            }

            return -1;
        }
    }
}
