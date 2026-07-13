using System;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Shop;
using SpireChess.Simulation;
using SpireChess.UI;
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
            Assert.That(configs.ContentRelease.MinionIds, Has.Count.EqualTo(67));
            Assert.That(configs.ContentRelease.SpellIds, Has.Count.EqualTo(16));
            Assert.That(configs.EventsById, Has.Count.EqualTo(10));
            Assert.That(configs.Encounters.Count(value => value.Category == "Normal"),
                Is.GreaterThanOrEqualTo(6));
            Assert.That(configs.Minions.Where(value => !value.IsToken),
                Has.All.Matches<MinionConfig>(value =>
                    value.Effects.Count > 0 && value.GoldenEffects.Count > 0));
        }

        [Test]
        public void CardTierPalette_UsesFiveDistinctReadableBackgrounds()
        {
            var colors = Enumerable.Range(1, 5)
                .Select(CardTierPalette.GetBackground)
                .ToList();

            Assert.That(colors.Distinct().Count(), Is.EqualTo(5));
            Assert.That(colors, Has.All.Matches<UnityEngine.Color>(color =>
                color.a == 1f && color.r >= 0.5f && color.g >= 0.5f));
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
        public void PrebattleBenediction_UsesFinalBattleLineupAtBattleStart()
        {
            var shop = CreateShop();
            Assert.That(shop.StartRound(1).Success, Is.True);
            var original = configs.MinionsById["stargazing_apprentice"];
            Assert.That(shop.ClaimRewardMinion(original).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, original.Id), 0).Success, Is.True);
            Assert.That(shop.ClaimRewardSpell(
                configs.SpellsById["prebattle_benediction"]).Success, Is.True);

            Assert.That(shop.UseSpell(
                FindBench(shop, "prebattle_benediction")).Success, Is.True);
            Assert.That(shop.Collection.Battle[0].HasPendingCombatShield, Is.False);
            Assert.That(shop.SellBattleMinion(0).Success, Is.True);

            var replacement = configs.MinionsById["young_deer_spirit"];
            Assert.That(shop.ClaimRewardMinion(replacement).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, replacement.Id), 0).Success, Is.True);
            var replacementCard = shop.Collection.Battle[0];
            var attackBeforeCombat = replacementCard.CurrentAttack;
            var healthBeforeCombat = replacementCard.CurrentHealth;

            var snapshot = shop.CreateBattleSnapshot();
            Assert.That(snapshot.Player[0].HasShield, Is.False);
            Assert.That(snapshot.BattleStartEffects, Has.Count.EqualTo(1));

            var result = new BattleSimulator(new Random(17), ResolveMinion)
                .SimulatePlayback(snapshot);
            Assert.That(result.FinalState.Player[0].HasShield, Is.True);
            Assert.That(result.Steps.Any(step =>
                step.BoardState.Player[0]?.HasShield == true &&
                step.Messages.Any(message => message.Contains("获得护盾"))), Is.True);

            shop.ApplyPostCombatSurvivorBuffs(result);
            Assert.That(replacementCard.CurrentAttack, Is.EqualTo(attackBeforeCombat + 1));
            Assert.That(replacementCard.CurrentHealth, Is.EqualTo(healthBeforeCombat + 1));
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
        public void NormalForgeAndWildFinishers_HaveLimitedPermanentGrowth()
        {
            var oathbroken = configs.MinionsById["oathbroken_blade_soul"];
            var oathbrokenGrowth = oathbroken.Effects.Single(
                effect => effect.Id == "oathbroken_blade_soul_lost_permanent");
            Assert.That(oathbrokenGrowth.Value.Attack, Is.EqualTo(1));
            Assert.That(oathbrokenGrowth.Value.Duration, Is.EqualTo("Permanent"));
            Assert.That(oathbrokenGrowth.Limit.PerCombat, Is.EqualTo(2));

            var tombGuardian = configs.MinionsById["thousand_ring_tomb_guardian"];
            var tombGrowth = tombGuardian.Effects.Single(
                effect => effect.Id == "thousand_ring_tomb_guardian_death_permanent");
            Assert.That(tombGrowth.Value.Attack, Is.EqualTo(1));
            Assert.That(tombGrowth.Value.Health, Is.EqualTo(1));
            Assert.That(tombGrowth.Target.MaxTargets, Is.EqualTo(2));

            var vinecrown = configs.MinionsById["vinecrown_priest"];
            var vinecrownGrowth = vinecrown.Effects.Single(
                effect => effect.Id == "vinecrown_priest_token_death_permanent");
            Assert.That(vinecrownGrowth.Trigger, Is.EqualTo("OnSummonedUnitDeath"));
            Assert.That(vinecrownGrowth.Value.Attack, Is.EqualTo(1));
            Assert.That(vinecrownGrowth.Value.Health, Is.EqualTo(1));

            var soulEater = configs.MinionsById["mountain_belly_soul_eater"];
            var soulEaterGrowth = soulEater.Effects.Single(
                effect => effect.Id == "mountain_belly_soul_eater_win");
            Assert.That(soulEaterGrowth.Condition.Type, Is.EqualTo("CombatWon"));
            Assert.That(soulEaterGrowth.Value.Health, Is.EqualTo(1));
            Assert.That(soulEaterGrowth.Limit.PerCombat, Is.EqualTo(1));
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
