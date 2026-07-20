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
        public void SpellEchoGrowth_HasPerShopCapsAndLimitedProphetTargets()
        {
            var glimmer = configs.MinionsById["glimmer_mage"];
            Assert.That(glimmer.Effects.Single(effect =>
                effect.Id == "glimmer_mage_spell_health").Limit.PerShop, Is.EqualTo(2));
            Assert.That(glimmer.GoldenEffects.Single(effect =>
                effect.Id == "golden_glimmer_mage_spell_health").Limit.PerShop, Is.EqualTo(2));

            var reader = configs.MinionsById["rune_ward_reader"];
            Assert.That(reader.Effects.Single(effect =>
                effect.Id == "rune_ward_reader_spell").Limit.PerShop, Is.EqualTo(2));
            Assert.That(reader.GoldenEffects.Single(effect =>
                effect.Id == "golden_rune_ward_reader_spell").Limit.PerShop, Is.EqualTo(2));

            var prophet = configs.MinionsById["falling_star_prophet"];
            var normalProphet = prophet.Effects.Single(effect =>
                effect.Id == "falling_star_prophet_spell");
            var goldenProphet = prophet.GoldenEffects.Single(effect =>
                effect.Id == "golden_falling_star_prophet_spell");
            Assert.That(normalProphet.Target.Scope, Is.EqualTo("Single"));
            Assert.That(normalProphet.Target.Race, Is.EqualTo("Starbound"));
            Assert.That(normalProphet.Target.Selector, Is.EqualTo("LowestAttack"));
            Assert.That(normalProphet.Target.MaxTargets, Is.EqualTo(2));
            Assert.That(normalProphet.Limit.PerShop, Is.EqualTo(2));
            Assert.That(goldenProphet.Target.Scope, Is.EqualTo("Single"));
            Assert.That(goldenProphet.Target.Race, Is.EqualTo("Starbound"));
            Assert.That(goldenProphet.Target.Selector, Is.EqualTo("LowestAttack"));
            Assert.That(goldenProphet.Target.MaxTargets, Is.EqualTo(4));
            Assert.That(goldenProphet.Limit.PerShop, Is.EqualTo(2));
        }

        [Test]
        public void UpdatedDiscoverMinions_HaveExpectedConditionsAndPickCounts()
        {
            var broker = configs.MinionsById["star_map_broker"];
            var brokerEffect = broker.Effects.Single();
            var goldenBrokerEffect = broker.GoldenEffects.Single();
            Assert.That(brokerEffect.Condition.Type, Is.EqualTo("PhaseStatAtLeast"));
            Assert.That(brokerEffect.Condition.PhaseStat, Is.EqualTo("RefreshCount"));
            Assert.That(brokerEffect.Condition.Threshold, Is.EqualTo(2));
            Assert.That(brokerEffect.Discover.Pick, Is.EqualTo(1));
            Assert.That(goldenBrokerEffect.Discover.Pick, Is.EqualTo(2));

            var guide = configs.MinionsById["old_tower_guide"];
            var guideEffect = guide.Effects.Single();
            Assert.That(guideEffect.Discover.Race, Is.EqualTo("MostCommonMainRace"));
            Assert.That(guideEffect.Discover.MaxTierMode,
                Is.EqualTo("CurrentTavernTierPlusOffset"));
            Assert.That(guideEffect.Discover.MaxTierOffset, Is.EqualTo(-1));
            Assert.That(guide.GoldenEffects.Single().Discover.Pick, Is.EqualTo(2));
            Assert.That(guide.GoldenEffects,
                Has.None.Matches<EffectConfig>(effect =>
                    effect.Action == "ModifyStats"));

            var lecturer = configs.MinionsById["stargate_lecturer"];
            var goldenLecturerDiscover = lecturer.GoldenEffects.Single(effect =>
                effect.Action == "DiscoverSpell").Discover;
            Assert.That(goldenLecturerDiscover.Count, Is.EqualTo(3));
            Assert.That(goldenLecturerDiscover.Pick, Is.EqualTo(2));
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
        public void TemperingMender_RepeatedNextCombatShieldPermanentlyAddsHealth()
        {
            var shop = CreateShop();
            Assert.That(shop.StartRound(1).Success, Is.True);
            var target = configs.MinionsById["stargazing_apprentice"];
            var mender = configs.MinionsById["tempering_mender"];
            Assert.That(shop.ClaimRewardMinion(target).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, target.Id), 0).Success, Is.True);

            Assert.That(shop.ClaimRewardMinion(mender).Success, Is.True);
            Assert.That(shop.PlayMinion(
                FindBench(shop, mender.Id), 1, 0).Success, Is.True);
            Assert.That(shop.Collection.Battle[0].HasPendingCombatShield, Is.True);
            Assert.That(shop.Collection.Battle[0].PermanentHealthBonus, Is.Zero);

            Assert.That(shop.ClaimRewardMinion(mender).Success, Is.True);
            Assert.That(shop.PlayMinion(
                FindBench(shop, mender.Id), 2, 0).Success, Is.True);
            Assert.That(shop.Collection.Battle[0].HasPendingCombatShield, Is.True);
            Assert.That(shop.Collection.Battle[0].PermanentHealthBonus, Is.EqualTo(2));
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
        public void RingingIronBastion_BattlecryPermanentlyBuffsAdjacentMinion()
        {
            var shop = CreateShop();
            Assert.That(shop.StartRound(1).Success, Is.True);
            var bastion = configs.MinionsById["ringing_iron_bastion"];
            var ally = configs.MinionsById["stargazing_apprentice"];
            Assert.That(shop.ClaimRewardMinion(ally).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, ally.Id), 0).Success, Is.True);
            Assert.That(shop.ClaimRewardMinion(bastion).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, bastion.Id), 1).Success, Is.True);

            Assert.That(shop.Collection.Battle[0].PermanentAttackBonus, Is.EqualTo(3));
            Assert.That(shop.Collection.Battle[0].PermanentHealthBonus, Is.EqualTo(3));
        }

        [Test]
        public void UpdatedForgeAndWildFinishers_HaveExpectedRules()
        {
            var oathbroken = configs.MinionsById["oathbroken_blade_soul"];
            Assert.That(oathbroken.Effects.Concat(oathbroken.GoldenEffects),
                Has.None.Matches<EffectConfig>(effect =>
                    effect.Trigger == "OnShieldLost" &&
                    effect.Value?.Duration == "Permanent"));
            var goldenKillGrowth = oathbroken.GoldenEffects.Single(effect =>
                effect.Id == "golden_oathbroken_blade_soul_kill_permanent");
            Assert.That(goldenKillGrowth.Trigger, Is.EqualTo("OnKill"));
            Assert.That(goldenKillGrowth.Action, Is.EqualTo("ModifyStats"));
            Assert.That(goldenKillGrowth.Value.Attack, Is.EqualTo(2));
            Assert.That(goldenKillGrowth.Value.Health, Is.Zero);
            Assert.That(goldenKillGrowth.Value.Duration, Is.EqualTo("Permanent"));
            Assert.That(goldenKillGrowth.Limit, Is.Null);

            var tombGuardian = configs.MinionsById["thousand_ring_tomb_guardian"];
            var tombGrowth = tombGuardian.Effects.Single(
                effect => effect.Id == "thousand_ring_tomb_guardian_death_permanent");
            Assert.That(tombGrowth.Value.Attack, Is.EqualTo(1));
            Assert.That(tombGrowth.Value.Health, Is.EqualTo(1));
            Assert.That(tombGrowth.Target.Scope, Is.EqualTo("All"));
            var tombShield = tombGuardian.Effects.Single(effect =>
                effect.Id == "thousand_ring_tomb_guardian_death_shield");
            Assert.That(tombShield.Action, Is.EqualTo("AddShield"));
            Assert.That(tombShield.Target.Scope, Is.EqualTo("Single"));
            Assert.That(tombShield.Target.MaxTargets, Is.EqualTo(2));
            Assert.That(tombShield.Target.Selector, Is.EqualTo("Random"));
            Assert.That(tombGuardian.GoldenEffects.Single(effect =>
                effect.Id == "golden_thousand_ring_tomb_guardian_death_shield")
                .Target.Scope, Is.EqualTo("All"));

            var astrolabe = configs.MinionsById["astrolabe_calibrator"];
            var normalCalibration = astrolabe.Effects.Single();
            var goldenCalibration = astrolabe.GoldenEffects.Single();
            Assert.That(normalCalibration.Target.MaxTargets, Is.EqualTo(1));
            Assert.That(normalCalibration.Value.Attack, Is.EqualTo(1));
            Assert.That(normalCalibration.Value.Health, Is.Zero);
            Assert.That(goldenCalibration.Target.MaxTargets, Is.EqualTo(1));
            Assert.That(goldenCalibration.Value.Attack, Is.EqualTo(2));
            Assert.That(goldenCalibration.Value.Health, Is.Zero);

            var vinecrown = configs.MinionsById["vinecrown_priest"];
            var vinecrownGrowth = vinecrown.Effects.Single(
                effect => effect.Id == "vinecrown_priest_flourish");
            Assert.That(vinecrownGrowth.Trigger, Is.EqualTo("OnFriendlyDeath"));
            Assert.That(vinecrownGrowth.Action, Is.EqualTo("GainFlourish"));
            Assert.That(vinecrownGrowth.Condition.Type, Is.EqualTo("TriggerCountMultipleOf"));
            Assert.That(vinecrownGrowth.Condition.Threshold, Is.EqualTo(4));
            Assert.That(vinecrownGrowth.Value.Amount, Is.EqualTo(1));
            Assert.That(vinecrownGrowth.Value.Count, Is.Zero);
            Assert.That(vinecrownGrowth.Limit, Is.Null);
            var goldenVinecrownGrowth = vinecrown.GoldenEffects.Single(
                effect => effect.Id == "golden_vinecrown_priest_flourish");
            Assert.That(goldenVinecrownGrowth.Condition.Type,
                Is.EqualTo("TriggerCountMultipleOf"));
            Assert.That(goldenVinecrownGrowth.Condition.Threshold, Is.EqualTo(3));
            Assert.That(goldenVinecrownGrowth.Value.Count, Is.Zero);
            Assert.That(goldenVinecrownGrowth.Limit, Is.Null);

            var soulEater = configs.MinionsById["mountain_belly_soul_eater"];
            var soulEaterHealth = soulEater.Effects.Single(
                effect => effect.Id == "mountain_belly_soul_eater_health");
            var soulEaterAttack = soulEater.Effects.Single(
                effect => effect.Id == "mountain_belly_soul_eater_attack");
            var soulEaterGrowth = soulEater.Effects.Single(
                effect => effect.Id == "mountain_belly_soul_eater_win");
            Assert.That(soulEaterHealth.Condition.Type, Is.EqualTo("SubjectRace"));
            Assert.That(soulEaterHealth.Condition.Race, Is.EqualTo("WildSpirit"));
            Assert.That(soulEaterAttack.Condition.Type,
                Is.EqualTo("SubjectRaceAndSourceAttackBelowHealth"));
            Assert.That(soulEaterAttack.Condition.Race, Is.EqualTo("WildSpirit"));
            Assert.That(soulEaterGrowth.Condition.Type, Is.EqualTo("CombatWon"));
            Assert.That(soulEaterGrowth.Value.Attack, Is.EqualTo(1));
            Assert.That(soulEaterGrowth.Value.Health, Is.EqualTo(2));
            Assert.That(soulEaterGrowth.Limit.PerCombat, Is.EqualTo(1));

            var goldenSoulEaterGrowth = soulEater.GoldenEffects.Single(
                effect => effect.Id == "golden_mountain_belly_soul_eater_win");
            Assert.That(goldenSoulEaterGrowth.Value.Attack, Is.EqualTo(2));
            Assert.That(goldenSoulEaterGrowth.Value.Health, Is.EqualTo(4));
            Assert.That(goldenSoulEaterGrowth.Limit.PerCombat, Is.EqualTo(1));
        }

        [Test]
        public void MountainBellySoulEater_TokenDeathAddsHealthBeforeAttackCheck()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["token_young_spirit"],
                initialAttack: 0,
                initialHealth: 1,
                sourceInstanceId: "wild-token");
            state.Player[1] = new BattleMinionRuntime(
                configs.MinionsById["mountain_belly_soul_eater"],
                initialAttack: 101,
                initialHealth: 100,
                sourceInstanceId: "soul-eater");
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["wandering_swordsman"],
                initialAttack: 1,
                initialHealth: 500);

            var result = new BattleSimulator(new Random(19), ResolveMinion)
                .Simulate(state);

            Assert.That(result.Diagnostics.Player.TokenDeaths, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Player.TemporaryHealthGained, Is.EqualTo(2));
            Assert.That(result.Diagnostics.Player.TemporaryAttackGained, Is.EqualTo(1),
                "101 attack is not below 100 health until the +2 health resolves.");
            var delta = result.PermanentDeltas.Single(value =>
                value.SourceInstanceId == "soul-eater");
            Assert.That(delta.Attack, Is.EqualTo(1));
            Assert.That(delta.Health, Is.EqualTo(2));
        }

        [Test]
        public void GoldenOathbroken_KillGrantsPermanentAttack()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["oathbroken_blade_soul"],
                true,
                sourceInstanceId: "golden-oathbroken");
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["wandering_swordsman"],
                initialAttack: 0,
                initialHealth: 1);

            var result = new BattleSimulator(new Random(21), ResolveMinion).Simulate(state);
            var delta = result.PermanentDeltas.Single(value =>
                value.SourceInstanceId == "golden-oathbroken");

            Assert.That(delta.Attack, Is.EqualTo(2));
            Assert.That(delta.Health, Is.Zero);
            Assert.That(result.FinalState.Player[0].CurrentAttack, Is.EqualTo(18));
        }

        [TestCase(false, 2)]
        [TestCase(true, 3)]
        public void ThousandRingTombGuardian_DeathShieldsDistinctConfiguredSurvivors(
            bool golden,
            int expectedShieldCount)
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["thousand_ring_tomb_guardian"],
                golden,
                initialAttack: 0,
                initialHealth: 1,
                sourceInstanceId: "tomb-guardian",
                permanentKeywords: new[] { "Taunt" });
            for (var slot = 1; slot <= 3; slot++)
            {
                state.Player[slot] = new BattleMinionRuntime(
                    configs.MinionsById["wandering_swordsman"],
                    initialAttack: 0,
                    initialHealth: 20,
                    sourceInstanceId: $"tomb-survivor-{slot}");
            }
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["wandering_swordsman"],
                initialAttack: 100,
                initialHealth: 500);

            var result = new BattleSimulator(new Random(27), ResolveMinion)
                .SimulatePlayback(state);
            var maximumSimultaneousShields = result.Steps.Max(step =>
                step.BoardState.Player.Count(card => card?.HasShield == true));

            Assert.That(result.Diagnostics.Player.ShieldsGranted,
                Is.EqualTo(expectedShieldCount));
            Assert.That(maximumSimultaneousShields, Is.EqualTo(expectedShieldCount),
                "The death effect must select distinct surviving allies.");
        }

        [Test]
        public void SummonBuffs_IncludeTokensAndGoldenHoofDoesNotGrantCleave()
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
            Assert.That(hoof.GoldenEffects.Any(value =>
                value.Action == "AddKeyword" && value.Value?.Keyword == "Cleave"), Is.False);
        }

        [Test]
        public void SummonedToken_ReceivesBranchHoofAndFlourishBeforeOneImmediateAttack()
        {
            var state = new BattleBoardState();
            state.PlayerFlourishStacks = 3;
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["young_deer_spirit"], initialHealth: 1);
            state.Player[1] = new BattleMinionRuntime(configs.MinionsById["many_branch_invoker"]);
            state.Player[2] = new BattleMinionRuntime(configs.MinionsById["ten_thousand_hoof_surge"]);
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
                Is.EqualTo(6));
        }

        [Test]
        public void GoldenHoof_BuffsSummonedTokenWithoutCleaveAndAttacksOnce()
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
            Assert.That(token.HasCleave, Is.False);
            Assert.That(result.Diagnostics.Player.ImmediateAttacks, Is.EqualTo(1));
        }

        [Test]
        public void NormalVinecrown_GainsGlobalFlourishAfterFourDeaths()
        {
            var state = new BattleBoardState();
            for (var slot = 0; slot < 4; slot++)
            {
                state.Player[slot] = new BattleMinionRuntime(
                    configs.MinionsById["wandering_swordsman"], initialHealth: 1);
            }
            state.Player[4] = new BattleMinionRuntime(
                configs.MinionsById["vinecrown_priest"],
                initialAttack: 3,
                initialHealth: 200,
                sourceInstanceId: "vinecrown");
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["royal_bounty_hunter"],
                initialAttack: 0,
                initialHealth: 5);

            var result = new BattleSimulator(new Random(31), ResolveMinion).Simulate(state);
            var vinecrown = result.FinalState.Player.Single(value =>
                value?.SourceInstanceId == "vinecrown");
            Assert.That(result.FinalState.PlayerFlourishStacks, Is.EqualTo(1));
            Assert.That(vinecrown.CurrentAttack, Is.EqualTo(4));
            Assert.That(result.Diagnostics.Player.FlourishGained, Is.EqualTo(1));
        }

        [Test]
        public void GoldenVinecrown_GainsOneFlourishAfterThreeFriendlyDeaths()
        {
            var state = new BattleBoardState();
            for (var slot = 0; slot < 3; slot++)
            {
                state.Player[slot] = new BattleMinionRuntime(
                    configs.MinionsById["wandering_swordsman"], initialHealth: 1);
            }
            state.Player[3] = new BattleMinionRuntime(
                configs.MinionsById["vinecrown_priest"],
                true,
                initialHealth: 200,
                sourceInstanceId: "vinecrown");
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["royal_bounty_hunter"],
                initialAttack: 0,
                initialHealth: 5);

            var result = new BattleSimulator(new Random(37), ResolveMinion).Simulate(state);
            Assert.That(result.FinalState.PlayerFlourishStacks, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Player.FlourishGained, Is.EqualTo(1));
        }

        [Test]
        public void HoofImmediateTokenDeaths_AdvanceVinecrownFlourish()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["hundred_song_herd"], initialHealth: 1);
            state.Player[1] = new BattleMinionRuntime(
                configs.MinionsById["ten_thousand_hoof_surge"]);
            state.Player[2] = new BattleMinionRuntime(
                configs.MinionsById["vinecrown_priest"],
                true,
                initialHealth: 100,
                sourceInstanceId: "vinecrown");
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["royal_bounty_hunter"],
                initialAttack: 100,
                initialHealth: 8);

            var result = new BattleSimulator(new Random(41), ResolveMinion).Simulate(state);
            Assert.That(result.Diagnostics.Player.ImmediateAttacks, Is.EqualTo(2));
            Assert.That(result.Diagnostics.Player.TokenDeaths, Is.EqualTo(2));
            Assert.That(result.FinalState.PlayerFlourishStacks, Is.EqualTo(2));
            Assert.That(result.Diagnostics.Player.FlourishGained, Is.EqualTo(2));
        }

        [Test]
        public void GlobalFlourish_IsUncappedAndDoesNotMultiplyWhenTripling()
        {
            var shop = CreateShop();
            Assert.That(shop.StartRound(1).Success, Is.True);
            var vinecrown = configs.MinionsById["vinecrown_priest"];
            shop.ApplyFlourish(12);

            Assert.That(shop.ClaimRewardMinion(vinecrown).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, vinecrown.Id), 0).Success, Is.True);

            Assert.That(shop.ClaimRewardMinion(vinecrown).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, vinecrown.Id), 1).Success, Is.True);

            Assert.That(shop.ClaimRewardMinion(vinecrown).Success, Is.True);
            var golden = shop.Collection.Bench.Single(value =>
                value?.ConfigId == vinecrown.Id);
            Assert.That(golden.IsGolden, Is.True);
            Assert.That(shop.FlourishStacks, Is.EqualTo(12));
            Assert.That(golden.FlourishAttackBonus, Is.EqualTo(12));
        }

        [Test]
        public void MoltenStandard_CountsPreparedShieldOnceAndIgnoresExistingShield()
        {
            var shop = CreateShop();
            Assert.That(shop.StartRound(1).Success, Is.True);
            var standard = configs.MinionsById["molten_core_standard"];
            var ally = configs.MinionsById["wandering_swordsman"];

            Assert.That(shop.ClaimRewardMinion(standard).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, standard.Id), 0).Success, Is.True);
            Assert.That(shop.ClaimRewardMinion(ally).Success, Is.True);
            Assert.That(shop.PlayMinion(FindBench(shop, ally.Id), 1).Success, Is.True);

            Assert.That(shop.ClaimRewardSpell(
                configs.SpellsById["temporary_ward"]).Success, Is.True);
            Assert.That(shop.UseSpell(
                FindBench(shop, "temporary_ward"), 1).Success, Is.True);
            Assert.That(shop.Collection.Battle[1].PermanentAttackBonus, Is.EqualTo(1));

            Assert.That(shop.ClaimRewardSpell(
                configs.SpellsById["temporary_ward"]).Success, Is.True);
            Assert.That(shop.UseSpell(
                FindBench(shop, "temporary_ward"), 1).Success, Is.True);
            Assert.That(shop.Collection.Battle[1].PermanentAttackBonus, Is.EqualTo(1));

            shop.CreateBattleSnapshot();
            Assert.That(shop.Collection.Battle[1].PermanentAttackBonus, Is.EqualTo(1));
        }

        [Test]
        public void MoltenStandard_WritesBackCombatShieldGrowthButNotForTokens()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["thousand_ring_tomb_guardian"],
                initialHealth: 1,
                sourceInstanceId: "guardian");
            state.Player[1] = new BattleMinionRuntime(
                configs.MinionsById["molten_core_standard"],
                initialHealth: 100,
                sourceInstanceId: "standard");
            state.Player[2] = new BattleMinionRuntime(
                configs.MinionsById["wandering_swordsman"],
                initialHealth: 100,
                sourceInstanceId: "ally");
            state.Player[3] = new BattleMinionRuntime(
                configs.MinionsById["token_swift_young_spirit"],
                initialAttack: 0,
                initialHealth: 100,
                sourceInstanceId: "token");
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["royal_bounty_hunter"],
                initialAttack: 100,
                initialHealth: 500);

            var result = new BattleSimulator(new Random(43), ResolveMinion).Simulate(state);
            var standardDelta = result.PermanentDeltas.Single(value =>
                value.SourceInstanceId == "standard");
            var allyDelta = result.PermanentDeltas.Single(value =>
                value.SourceInstanceId == "ally");

            Assert.That(standardDelta.Attack, Is.EqualTo(2));
            Assert.That(standardDelta.Health, Is.EqualTo(1));
            Assert.That(allyDelta.Attack, Is.EqualTo(2));
            Assert.That(allyDelta.Health, Is.EqualTo(1));
            Assert.That(result.PermanentDeltas.Any(value =>
                value.SourceInstanceId == "token"), Is.False);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void CrackedArmorAvenger_RequestsPostCombatForgeSoulRewards(
            bool isGolden,
            int expectedCount)
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["cracked_armor_avenger"],
                isGolden,
                initialHealth: 1);
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["royal_bounty_hunter"],
                initialAttack: 100,
                initialHealth: 100);

            var result = new BattleSimulator(new Random(47), ResolveMinion).Simulate(state);
            var request = result.PostCombatRewardRequests.Single();
            Assert.That(request.Side, Is.EqualTo(BattleSide.Player));
            Assert.That(request.Race, Is.EqualTo("ForgeSoul"));
            Assert.That(request.Count, Is.EqualTo(expectedCount));
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
