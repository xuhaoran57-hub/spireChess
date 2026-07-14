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
            Assert.That(oathbrokenGrowth.Limit.PerCombat, Is.EqualTo(1));

            var tombGuardian = configs.MinionsById["thousand_ring_tomb_guardian"];
            var tombGrowth = tombGuardian.Effects.Single(
                effect => effect.Id == "thousand_ring_tomb_guardian_death_permanent");
            Assert.That(tombGrowth.Value.Attack, Is.EqualTo(1));
            Assert.That(tombGrowth.Value.Health, Is.EqualTo(1));
            Assert.That(tombGrowth.Target.MaxTargets, Is.EqualTo(2));

            var vinecrown = configs.MinionsById["vinecrown_priest"];
            var vinecrownGrowth = vinecrown.Effects.Single(
                effect => effect.Id == "vinecrown_priest_flourish");
            Assert.That(vinecrownGrowth.Trigger, Is.EqualTo("OnFriendlyDeath"));
            Assert.That(vinecrownGrowth.Action, Is.EqualTo("GainFlourish"));
            Assert.That(vinecrownGrowth.Condition.Type, Is.EqualTo("TriggerCountAtLeast"));
            Assert.That(vinecrownGrowth.Condition.Threshold, Is.EqualTo(2));
            Assert.That(vinecrownGrowth.Value.Count, Is.EqualTo(4));
            Assert.That(vinecrownGrowth.Limit.PerCombat, Is.EqualTo(1));
            var goldenVinecrownGrowth = vinecrown.GoldenEffects.Single(
                effect => effect.Id == "golden_vinecrown_priest_flourish");
            Assert.That(goldenVinecrownGrowth.Value.Count, Is.EqualTo(8));
            Assert.That(goldenVinecrownGrowth.Limit.PerCombat, Is.EqualTo(1));
            Assert.That(new BattleMinionRuntime(
                vinecrown, flourishStacks: 99).FlourishStacks, Is.EqualTo(4));
            Assert.That(new BattleMinionRuntime(
                vinecrown, true, flourishStacks: 99).FlourishStacks, Is.EqualTo(8));

            var soulEater = configs.MinionsById["mountain_belly_soul_eater"];
            var soulEaterGrowth = soulEater.Effects.Single(
                effect => effect.Id == "mountain_belly_soul_eater_win");
            Assert.That(soulEaterGrowth.Condition.Type, Is.EqualTo("CombatWon"));
            Assert.That(soulEaterGrowth.Value.Health, Is.EqualTo(1));
            Assert.That(soulEaterGrowth.Limit.PerCombat, Is.EqualTo(1));
        }

        [Test]
        public void SummonBuffs_IncludeTokensAndGoldenHoofCleaveTargetsToken()
        {
            var branch = configs.MinionsById["many_branch_invoker"];
            Assert.That(branch.Effects.Single(value =>
                value.Id == "many_branch_invoker_summon").Target.IncludeToken, Is.True);
            Assert.That(branch.GoldenEffects.Single(value =>
                value.Id == "golden_many_branch_invoker_summon").Target.IncludeToken, Is.True);

            var hoof = configs.MinionsById["ten_thousand_hoof_surge"];
            Assert.That(hoof.Effects.Single(value =>
                value.Id == "ten_thousand_hoof_surge_buff").Target.IncludeToken, Is.True);
            Assert.That(hoof.GoldenEffects.Single(value =>
                value.Id == "golden_ten_thousand_hoof_surge_buff").Target.IncludeToken, Is.True);
            Assert.That(hoof.GoldenEffects.Single(value =>
                value.Id == "golden_ten_thousand_hoof_surge_cleave").Target.IncludeToken, Is.True);
        }

        [Test]
        public void SummonedToken_ReceivesBranchHoofAndFlourishBeforeOneImmediateAttack()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["young_deer_spirit"], initialHealth: 1);
            state.Player[1] = new BattleMinionRuntime(configs.MinionsById["many_branch_invoker"]);
            state.Player[2] = new BattleMinionRuntime(configs.MinionsById["ten_thousand_hoof_surge"]);
            state.Player[3] = new BattleMinionRuntime(
                configs.MinionsById["vinecrown_priest"], flourishStacks: 3);
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["royal_bounty_hunter"],
                initialAttack: 0,
                initialHealth: 100);

            var result = new BattleSimulator(new Random(23), ResolveMinion).Simulate(state);
            var token = result.FinalState.Player.Single(value => value?.Config.IsToken == true);

            Assert.That(token.CurrentAttack, Is.EqualTo(7));
            Assert.That(token.CurrentHealth, Is.EqualTo(2));
            Assert.That(result.Diagnostics.Player.ImmediateAttacks, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Player.TemporaryAttackGained,
                Is.GreaterThanOrEqualTo(6));
        }

        [Test]
        public void GoldenHoof_GrantsSummonedTokenCleaveAndAttacksOnce()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["young_deer_spirit"], initialHealth: 1);
            state.Player[1] = new BattleMinionRuntime(
                configs.MinionsById["ten_thousand_hoof_surge"], true);
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["royal_bounty_hunter"],
                initialAttack: 0,
                initialHealth: 100);

            var result = new BattleSimulator(new Random(29), ResolveMinion).Simulate(state);
            var token = result.FinalState.Player.Single(value => value?.Config.IsToken == true);

            Assert.That(token.CurrentAttack, Is.EqualTo(5));
            Assert.That(token.HasCleave, Is.True);
            Assert.That(result.Diagnostics.Player.ImmediateAttacks, Is.EqualTo(1));
        }

        [Test]
        public void Vinecrown_GainsOneFlourishAfterTwoDeathsAndBuffsTokensWithNewStack()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["young_deer_spirit"], initialHealth: 1);
            state.Player[1] = new BattleMinionRuntime(
                configs.MinionsById["young_deer_spirit"], initialHealth: 1);
            state.Player[2] = new BattleMinionRuntime(
                configs.MinionsById["vinecrown_priest"],
                initialHealth: 100,
                sourceInstanceId: "vinecrown",
                flourishStacks: 3);
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["royal_bounty_hunter"],
                initialAttack: 0,
                initialHealth: 100);

            var result = new BattleSimulator(new Random(31), ResolveMinion).Simulate(state);
            var vinecrown = result.FinalState.Player.Single(value =>
                value?.SourceInstanceId == "vinecrown");
            var delta = result.PermanentDeltas.Single(value =>
                value.SourceInstanceId == "vinecrown");
            var tokens = result.FinalState.Player.Where(value =>
                value?.Config.IsToken == true).ToList();

            Assert.That(vinecrown.FlourishStacks, Is.EqualTo(4));
            Assert.That(delta.Flourish, Is.EqualTo(1));
            Assert.That(delta.Attack, Is.Zero);
            Assert.That(delta.Health, Is.Zero);
            Assert.That(result.Diagnostics.Player.FlourishGained, Is.EqualTo(1));
            Assert.That(tokens, Has.Count.EqualTo(2));
            Assert.That(tokens, Has.All.Matches<BattleMinionRuntime>(value =>
                value.CurrentAttack == 5));
        }

        [Test]
        public void VinecrownTriple_SumsFlourishAndCapsGoldenAtEight()
        {
            var shop = CreateShop();
            Assert.That(shop.StartRound(1).Success, Is.True);
            var vinecrown = configs.MinionsById["vinecrown_priest"];

            Assert.That(shop.ClaimRewardMinion(vinecrown).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, vinecrown.Id), 0).Success, Is.True);
            Assert.That(shop.ModifyOwnedBattleMinion(
                shop.Collection.Battle[0].InstanceId, 0, 0, flourish: 4).Success, Is.True);

            Assert.That(shop.ClaimRewardMinion(vinecrown).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, vinecrown.Id), 1).Success, Is.True);
            Assert.That(shop.ModifyOwnedBattleMinion(
                shop.Collection.Battle[1].InstanceId, 0, 0, flourish: 4).Success, Is.True);

            Assert.That(shop.ClaimRewardMinion(vinecrown).Success, Is.True);
            var golden = shop.Collection.Bench.Single(value =>
                value?.ConfigId == vinecrown.Id);
            Assert.That(golden.IsGolden, Is.True);
            Assert.That(golden.FlourishStacks, Is.EqualTo(8));
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
